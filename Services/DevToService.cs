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
    public class DevToService : IThirdPartyBlogService<DevToPoco>
    {
      private readonly IHttpClientFactory _clientFactory;

      public DevToService(IHttpClientFactory clientFactory)
      {
          _clientFactory = clientFactory;
      }

      public async Task CreatePostAsync(DevToPoco articleObject, string integrationToken, string authorId = null)
      {
        string uri = $"https://dev.to/api/articles";
        string json = JsonSerializer.Serialize<DevToPoco>(articleObject);

        var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("api-key", $"{integrationToken}");
        var postResponse = await client.PostAsJsonAsync(uri, articleObject);

        postResponse.EnsureSuccessStatusCode();
      }

    }

    public class DevToPoco {
      public Article article {get; set;}

    }

    public class Article {
      public string title {get; set;}
      public bool published {get; set;} = false;
      public string body_markdown {get; set;}
      public List<string> tags {get; set;}
      public string series {get; set;}
      public string canonical_url {get; set;}
      public string description {get; set;}

    }
}