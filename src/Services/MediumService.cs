using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
      /// <value>Instance of the IHttpClientFactory to be used throughout the MediumService call. This is a common approach in .NET Core to generate consistent HttpClients. As an example in the context of HugoCrossPoster, named http clients may inherit retry, circuit breaker and other resilience patterns using the Polly Framework, as indicated in the program.cs startup routine.</value>
      private readonly IHttpClientFactory _clientFactory;

      /// <summary>
      /// The Medium.com Service constructor.
      /// This is used as part of the .NET Dependency Injection functionality, binding the IHttpClientFactory interface to concrete types from the startup class.
      /// </summary>.
      /// <param name="clientFactory">Instance of the Client Factory which is passed to this service from the Program's startup class.</param>
      public MediumService(IHttpClientFactory clientFactory)
      {
          _clientFactory = clientFactory;
      }

      /// <summary>
      /// CreatePostAsync is responsible for creating a post on medium.com This is an asynchronous call, and used so that we do not block the caller's thread and can run in the background as there is potentially the need for retry and circuit breakers, which could make this a long-running operation.
      /// </summary>.
      /// <param name="articleObject">This is a poco which represents the article object which will be sent as the payload to medium.com.</param>
      /// <param name="integrationToken">Integration Token which is used to authorize to the medium.com api. A user can obtain this through their user settings on dev.to.</param>
      /// <param name="authorId">This is required for the medium.com service. Tjhe authorId forms part of the Uri where the articleObject should be POSTed.</param>
      /// <param name="youtube">This is an optional parameter, representing a YouTube Video ID. If the article was originally a YouTube video (e.g.a podcast episode with a video on YouTube), then this should be populated. This is used to automatically append the appropriate liquid tag to the Dev.To article with the YouTube video ID.</param>      
      public async Task<HttpResponseMessage> CreatePostAsync(MediumPoco articleObject, string integrationToken, string authorId = null, string youtube = null)
      {
        //Prepend the title, as medium doesn't automatically add the title to the page.
        articleObject.content = $"# {articleObject.title}{articleObject.content}";

        // If there is a youtube parameter, add it to the end of the content.
        if (!String.IsNullOrEmpty(youtube))
        {
          articleObject.content += $"\n\nhttps://youtu.be/{youtube}";
        }

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
        var postResponse = await client.PostAsJsonAsync(uri, articleObject);
        return await Task.Run(() => postResponse.EnsureSuccessStatusCode());
      }
    }

    public class MediumPoco
    {
      public string title {get; set;}
      public string contentFormat {get; set;} = "markdown";
      public string content {get; set;}
      public string canonicalUrl {get; set;}
      public List<string> tags {get; set;}
      public string publishStatus {get; set;} = "draft";
    }
}