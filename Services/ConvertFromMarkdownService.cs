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

      public async Task<string> getTitle (string fileContents)
      {
          // Find the title frontmatter in the YAML
          string pattern = @"title: (?<title>.+)";
          Match match = Regex.Match(fileContents, pattern, RegexOptions.IgnoreCase);

          // Return the value of the <title> matched group
          return await Task<string>.Run(() => match.Groups["title"].Value);
      }

      public async Task<string> getYoutube (string fileContents)
      {
          // Find the youtube frontmatter in the YAML
          string pattern = @"youtube: (?<youtube>.+)";
          Match match = Regex.Match(fileContents, pattern, RegexOptions.IgnoreCase);

          // Return the value of the <youtube> matched group
          return await Task<string>.Run(() => match.Groups["youtube"].Value);
      }

      public async Task<string> getDescription (string fileContents)
      {
          // Find the youtube frontmatter in the YAML
          string pattern = @"description: (?<description>.+)";
          Match match = Regex.Match(fileContents, pattern, RegexOptions.IgnoreCase);

          // Return the value of the <youtube> matched group
          return await Task<string>.Run(() => match.Groups["description"].Value);
      }

      public async Task<List<string>> getTags (string fileContents, int count = 10, bool humanize = false)
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
          return temp.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(count).ToList();
      }

      public async Task<string> getSeries (string fileContents)
      {
          // Find the tags frontmatter in the YAML
          string pattern = @"series:\n(?<series>(\-\s[\w\s]+\n)*)";
          Match match = Regex.Match(fileContents, pattern);

          //Remove the "- " from the string
          var temp = await Task<string>.Run(() => match.Groups["series"].Value);
          temp = temp.Replace("- ", "");

            List<string> listOfSeries = temp.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (listOfSeries.Count() > 0){
                return listOfSeries[0];
            } else {
                return "";
            }
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