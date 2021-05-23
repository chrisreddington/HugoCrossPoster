using HugoCrossPoster.Classes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{

    /// <summary>
    /// Concrete implementation to send content payloads to medium.com.
    /// </summary>
    /// <remarks>
    /// Contains the required implementation implementation of CreatePostAsync to send content to medium.com. Inherits from IThirdPartyBlogService.
    /// </remarks>
    public class MediumService : IThirdPartyBlogService<MediumPoco>
    {
        /// <value>Instance of the IHttpClientFactory to be used throughout the Medium Service call. This is a common approach in .NET Core to generate consistent HttpClients. As an example in the context of HugoCrossPoster, named http clients may inherit retry, circuit breaker and other resilience patterns using the Polly Framework, as indicated in the program.cs startup routine.</value>
        private readonly IHttpClientFactory _clientFactory;
        /// <value>Instance of the ILogger to be used throughout the Medium Service call. This is a common approach in .NET Core to generate consistent HttpClients. As an example in the context of HugoCrossPoster, named http clients may inherit retry, circuit breaker and other resilience patterns using the Polly Framework, as indicated in the program.cs startup routine.</value>
        private readonly ILogger<MediumService> _logger;

        /// <summary>
        /// The Medium.com Service constructor.
        /// This is used as part of the .NET Dependency Injection functionality, binding the IHttpClientFactory interface to concrete types from the startup class.
        /// </summary>.
        /// <param name="clientFactory">Instance of the Client Factory which is passed to this service from the Program's startup class.</param>
        /// <param name="logger">Instance of the logger which is passed to this service from the Program's startup class.</param>
        public MediumService(IHttpClientFactory clientFactory, ILogger<MediumService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        /// <summary>
        /// CreatePostAsync is responsible for creating a post on medium.com This is an asynchronous call, and used so that we do not block the caller's thread and can run in the background as there is potentially the need for retry and circuit breakers, which could make this a long-running operation.
        /// </summary>.
        /// <param name="articleObject">This is a poco which represents the article object which will be sent as the payload to medium.com.</param>
        /// <param name="integrationToken">Integration Token which is used to authorize to the medium.com api. A user can obtain this through their user settings on dev.to.</param>
        /// <param name="authorId">This is required for the medium.com service. Tjhe authorId forms part of the Uri where the articleObject should be POSTed.</param>
        /// <param name="youtube">This is an optional parameter, representing a YouTube Video ID. If the article was originally a YouTube video (e.g.a podcast episode with a video on YouTube), then this should be populated. This is used to automatically append the appropriate liquid tag to the Dev.To article with the YouTube video ID.</param>      
        public async Task<HttpResponseMessage> CreatePostAsync(MediumPoco articleObject, string integrationToken, CancellationTokenSource cts = default, string authorId = null, string youtube = null)
        {
            //Prepend the title, as medium doesn't automatically add the title to the page.
            articleObject.content = $"# {articleObject.title}{articleObject.content}";

            // If there is a youtube parameter, add it to the end of the content.
            articleObject.content = await AppendYouTubeInformation(articleObject.content, youtube);

            // Replace any Tweet references in the content
            articleObject.content = await ReplaceEmbeddedTweets(articleObject.content);

            // Define the dev.to API URI, where we will be sending the articles.
            // Note that authorId is required as it forms part of the URI.
            string uri = $"https://api.medium.com/v1/users/{authorId}/posts";

            // Create a named client, so that it automatically inherits the circuit breaker and retry configuration from the Polly Framework through DI.
            var client = _clientFactory.CreateClient("devto");

            // Add the integration to the Authorization header in the form of a bearer token, per the medium.com API specification.
            if (client.DefaultRequestHeaders.Authorization == null)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {integrationToken}");
            }

            // Post the article object to the medium.com API by serializing the object to JSON.
            // TODO: Review approach to logging out success/failure, particularly for unprocessable_entity items.
            try
            {
                var postResponse = await client.PostAsJsonAsync(uri, articleObject, cts.Token);
                return postResponse.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("[Medium] Unauthorized Response from Dev.To. Cancelling all further requests to this Third Party Service...");
                cts.Cancel();
                throw new UnauthorizedResponseException();
            }
        }

        // <summary>
        /// Method to take an ID of a YouTube video and convert it into the appropriate format for medium.com
        /// </summary>.
        /// <param name="originalBody">The original content body text</param>
        /// <param name="youtube">This is an optional parameter, representing a YouTube Video ID. If an ID is passed in, then it will add the appropriate representation into the content body for medium.com.

        public async Task<string> AppendYouTubeInformation(string originalBody, string youtube)
        {
            if (!String.IsNullOrEmpty(youtube))
            {
                _logger.LogInformation("Youtube ID {youtube} added", youtube);
                return await Task.Run(() => originalBody += $"\n\nhttps://youtu.be/{youtube}");
            }

            _logger.LogInformation("No YouTube ID provided, not embedding a YouTube Video.");
            return originalBody;
        }

        /// <summary>
        /// Method to replace the Hugo Tweet Embed shortcode with the appropriate code for medium.com.
        /// </summary>.
        /// <param name="fileContent">The original content body text</param>
        public async Task<string> ReplaceEmbeddedTweets(string fileContents)
        {
            // Find any strings that match the {{< tweet id >}} syntax
            string pattern = @"{{< tweet (.*) >}}";

            // We'll then replace it with the appropriate syntax for Medium, which is the full Twitter URL.
            string replacement = $"https://twitter.com/username/status/$1";

            string returnValue = Regex.Replace(fileContents, pattern, replacement);

            // Replace the contents throughout the doc and return the result.
            return await Task<string>.Run(() => returnValue);
        }
    }

    public class MediumPoco : IThirdPartyBlogPoco
    {
      public string title {get; set;}
      public string contentFormat {get; set;} = "markdown";
      public string content {get; set;}
      public string canonicalUrl {get; set;}
      public List<string> tags {get; set;}
      public string publishStatus {get; set;} = "draft";
    }
}