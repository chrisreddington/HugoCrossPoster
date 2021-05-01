using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
  public class ConvertFromMarkdownService : IConverter
    {
        public async Task<IEnumerable<string>> listFiles(string directoryPath, string searchPattern, bool recursiveSubdirectories){
            IEnumerable<string> fileList = new List<string>();
            try {
                fileList = await Task.Run(() => System.IO.Directory.EnumerateFiles(directoryPath, searchPattern, new System.IO.EnumerationOptions(){
                    RecurseSubdirectories = recursiveSubdirectories
                }));
            } catch(Exception ex) {
                Console.WriteLine($"[Files] {ex.Message}");
            }

            return fileList;
        }
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

      public async Task<string> getFrontmatterProperty (string fileContents, string key)
      {
            // Find the frontmatter with the Key 'key' in the YAML
            string pattern = $@"{key}:\s(?<{key}>[\-\s[\w\s]+\s?]*)";
            //string pattern = $@"{key}: ('?""?)*(?<{key}>[\w\-]+)('?""?)*";
          Match match = Regex.Match(fileContents, pattern, RegexOptions.IgnoreCase);

          // Return the value of the <key> matched group
          return await Task<string>.Run(() => match.Groups[key].Value);
      }

      
      public async Task<List<string>> getFrontMatterPropertyList (string fileContents, string key, int count = 10, bool urlize = false)
      {
          // Find the frontmatter for the key in the YAML
          string pattern = $@"{key}:\s(?<{key}>[\-\s[\w\s]+\s?]*)";
            Match match = Regex.Match(fileContents, pattern, RegexOptions.IgnoreCase);

          //Remove the "- " from the string
          var temp = await Task<string>.Run(() => match.Groups[key].Value);
          temp = temp.Replace("- ", "");

          if (urlize){
              temp = temp.Replace(" ","");
              temp = temp.ToLower();
          }

        //Delimit by the new line character
        return temp.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Take(count).ToList();
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