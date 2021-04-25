using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    public class ConvertFromMarkdownService : IConverter
    {
      public async Task<string> getCanonicalUrl(string protocol, string baseUrl, string fileName)
      {
          string fileNamewithoutExtension = fileName.Replace(".md", "");
          return new UriBuilder(protocol, baseUrl, -1, fileNamewithoutExtension).ToString();
      }

      public async Task<string> readFile(string fileName)
      {
          // Simply call out to the System.IO.File.ReadAllTextAsync method.
          return await System.IO.File.ReadAllTextAsync(fileName);
      }

      public async Task<string> getTitle (string fileContents)
      {
          // Find the title frontmatter in the YAML
          string pattern = @"title: (?<title>.+)";
          Match match = Regex.Match(fileContents, pattern);

          // Return the value of the <title> matched group
          return await Task<string>.Run(() => match.Groups["title"].Value);
      }

      public async Task<List<string>> getTags (string fileContents, bool humanize = false)
      {
          // Find the tags frontmatter in the YAML
          string pattern = @"tags:\n(?<tags>(\-\s[\w\s]+\n)*)";
          Match match = Regex.Match(fileContents, pattern);

          //Remove the "- " from the string
          var temp = await Task<string>.Run(() => match.Groups["tags"].Value);
          temp = temp.Replace("- ", "");

          if (humanize){
              temp = temp.Replace(" ","");
              temp = temp.ToLower();
          }

          //Delimit by the new line character
          return temp.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
      }

      public async Task<string> replaceLocalURLs(string fileContents, string baseUrl)
      {
          // Find any strings that match the []() syntax, and do not start with http. That means it's a 
          // local URL.
          string pattern = @"\[(.+)\]\(((?!http).+)\)";

          // We'll then replace it with the markdown []() syntax, but prepending the baseURL variable
          // in front of the URL.
          string replacement = $"[$1]({baseUrl}$2)";

          // Replace the contents throughout the doc and return the result.
          return await Task<string>.Run(() => Regex.Replace(fileContents, pattern, replacement)); 
      }

      public async Task<string> removeFrontMatter(string fileContents)
      {
          // Find the contents of the markdown frontmatter
          string pattern = @"^---[\s\S]+?---";

          // Replace it with an empty string
          string replacement = "";

          // Replace the contents throughout the doc and return the result.
          return await Task<string>.Run(() => Regex.Replace(fileContents, pattern, replacement)); 
      }

    }
}