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
  /// Concrete implementation to send content payloads to dev.to.
  /// </summary>
  /// <remarks>
  /// Contains the required implementation implementation of CreatePostAsync to send content to dev.to. Inherits from IThirdPartyBlogService.
  /// </remarks>
  public class DevToService : IThirdPartyBlogService<DevToPoco>
  {
    /// <value>Instance of the IHttpClientFactory to be used throughout the DevToService call. This is a common approach in .NET Core to generate consistent HttpClients. As an example in the context of ugoCrossPoster, named http clients may inherit retry, circuit breaker and other resilience patterns using the Polly Framework, as indicated in the program.cs startup routine.</value>
    private readonly IHttpClientFactory _clientFactory;
    /// <value>Instance of the ILogger to be used throughout the DevToService call. This is a common approach in .NET Core to generate consistent HttpClients. As an example in the context of HugoCrossPoster, named http clients may inherit retry, circuit breaker and other resilience patterns using the Polly Framework, as indicated in the program.cs startup routine.</value>
    private readonly ILogger<DevToService> _logger;

    /// <summary>
    /// The dev.to Service constructor.
    /// This is used as part of the .NET Dependency Injection functionality, binding the IHttpClientFactory interface to concrete types from the startup class.
    /// </summary>.
    /// <param name="clientFactory">Instance of the Client Factory which is passed to this service from the Program's startup class.</param>
    /// <param name="logger">Instance of the logger which is passed to this service from the Program's startup class.</param>
    public DevToService(IHttpClientFactory clientFactory, ILogger<DevToService> logger)
    {
      _clientFactory = clientFactory;
      _logger = logger;
    }

    /// <summary>
    /// CreatePostAsync is responsible for creating a post on Dev.To This is an asynchronous call, and used so that we do not block the caller's thread and can run in the background as there is potentially the need for retry and circuit breakers, which could make this a long-running operation.
    /// </summary>.
    /// <param name="articleObject">This is a poco which represents the article object which will be sent as the payload to dev.to</param>
    /// <param name="integrationToken">Integration Token which is used to authorize to the dev.to api. A user can obtain this through their user settings on dev.to.</param>
    /// <param name="authorId">This is defaulted to null and is unrequired for the dev.to service. It is not used within the implementation.</param>
    /// <param name="youtube">This is an optional parameter, representing a YouTube Video ID. If the article was originally a YouTube video (e.g.a podcast episode with a video on YouTube), then this should be populated. This is used to automatically append the appropriate liquid tag to the Dev.To article with the YouTube video ID.</param>
    public async Task<HttpResponseMessage> CreatePostAsync(DevToPoco articleObject, string integrationToken, CancellationTokenSource cts = default, string authorId = null, string youtube = null)
    {

      // If there is a youtube parameter, add it to the end of the content with a liquid tag.
      articleObject.article.body_markdown = await AppendYouTubeInformation(articleObject.article.body_markdown, youtube);

      // Replace any Tweet references in the content
      articleObject.article.body_markdown = await ReplaceEmbeddedTweets(articleObject.article.body_markdown);

      // Define the dev.to API URI, where we will be sending the articles.
      string uri = $"https://dev.to/api/articles";

      // Create a named client, so that it automatically inherits the circuit breaker and retry configuration from the Polly Framework through DI.
      var client = _clientFactory.CreateClient("devto");

      // Add the integration to the api-key header, per the dev.to API specification.
      client.DefaultRequestHeaders.Add("api-key", $"{integrationToken}");

      // Post the article object to the dev.to API by serializing the object to JSON.
      try
      {
        var postResponse = await client.PostAsJsonAsync(uri, articleObject, cts.Token);
        return postResponse.EnsureSuccessStatusCode();
      }
      catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
      {
        _logger.LogError("[DevTo] Unauthorized Response from Dev.To. Cancelling...");
        cts.Cancel();
        throw new UnauthorizedResponseException();
      }
      catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnprocessableEntity)
      {
        _logger.LogInformation(JsonSerializer.Serialize(articleObject));
        throw new UnprocessableEntityException(JsonSerializer.Serialize(articleObject));
      }
    }

    /// <summary>
    /// Method to take an ID of a YouTube video and convert it into the appropriate format for DevTo
    /// </summary>.
    /// <param name="originalBody">The original content body text</param>
    /// <param name="youtube">This is an optional parameter, representing a YouTube Video ID. If an ID is passed in, then it will add the appropriate representation into the content body for Devto.
    public async Task<string> AppendYouTubeInformation(string originalBody, string youtube)
    {
      if (!String.IsNullOrEmpty(youtube))
      {
        _logger.LogInformation("Youtube ID {youtube} added", youtube);
        return await Task.Run(() => originalBody += $"\n\n{{% youtube {youtube} %}}");
      }

      _logger.LogInformation("No YouTube ID provided, not embedding a YouTube Video.");
      return originalBody;
    }

    /// <summary>
    /// Method to replace the Hugo Tweet Embed shortcode with the appropriate code for DevTo.
    /// </summary>.
    /// <param name="fileContent">The original content body text</param>
    public async Task<string> ReplaceEmbeddedTweets(string fileContents)
    {
      // Find any strings that match the {{< tweet id >}} syntax
      string pattern = @"{{< tweet (.*) >}}";

      // We'll then replace it with the appropriate syntax for Medium, which is the full Twitter URL.
      string replacement = $"{{% twitter $1 %}}";

      string returnValue = Regex.Replace(fileContents, pattern, replacement);

      // Replace the contents throughout the doc and return the result.
      return await Task<string>.Run(() => returnValue);
    }
  }

  public class DevToPoco : IThirdPartyBlogPoco
  {
    public Article article { get; set; }
  }

  public class Article
  {
    public string title { get; set; }
    public bool published { get; set; } = false;
    public string body_markdown { get; set; }
    public List<string> tags { get; set; }
    public string series { get; set; }
    public string canonical_url { get; set; }
    public string description { get; set; }
    public int organization_id { get; set; } = 0;
  }
}