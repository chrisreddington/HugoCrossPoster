using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    public class MediumService : IThirdPartyBlogService<MediumPoco>
    {
      private readonly IHttpClientFactory _clientFactory;

      public MediumService(IHttpClientFactory clientFactory)
      {
          _clientFactory = clientFactory;
      }

      public async Task CreatePostAsync(MediumPoco mediumPoco, string integrationToken, string authorId)
      {
        string uri = $"https://api.medium.com/v1/users/{authorId}/posts";
        string json = JsonSerializer.Serialize<MediumPoco>(mediumPoco);

        var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {integrationToken}");
        var postResponse = await client.PostAsJsonAsync(uri, mediumPoco);

        postResponse.EnsureSuccessStatusCode();
      }

    }

    public class MediumPoco {
      public string title {get; set;}
      public string contentFormat {get; set;} = "markdown";
      public string content {get; set;}
      public string canonicalUrl {get; set;}
      public List<string> tags {get; set;}
      public string publishStatus {get; set;} = "draft";

    }
}