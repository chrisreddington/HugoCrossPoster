using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace markdown_converter
{
    class Program
    {
        static string title;
        static List<string> tags;
        static async Task Main(string[] args)
        {
            string baseUrl = "https://www.cloudwithchris.com";
            string fileName = "contributing-to-a-hugo-theme.md";
            string sourceFile = await readFile(fileName);

            string replaceUrls = await replaceLocalURLs(sourceFile, baseUrl);
            title = await getTitle(sourceFile);

            Console.WriteLine($"Title is {title}");
            Console.WriteLine($"{replaceUrls}");

            MediumPayload mediumPayload = new MediumPayload(){
                title = title,
                content = replaceUrls,
                canonicalUrl = $"{baseUrl}{fileName}"
            };

            sendToMedium(mediumPayload);
        }

        async static Task<string> readFile(string fileName)
        {
            // Simply call out to the System.IO.File.ReadAllTextAsync method.
            return await System.IO.File.ReadAllTextAsync(fileName);
        }

        async static Task<string> getTitle (string fileContents)
        {
            // Find any strings that match the []() syntax, and do not start with http. That means it's a 
            // local URL.
            string pattern = "title: (?<title>.+)";

            Match match = Regex.Match(fileContents, pattern);
            return await Task<string>.Run(() => match.Groups["title"].Value);
        }

        async static Task<string> replaceLocalURLs(string fileContents, string baseUrl)
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

        async static Task sendToMedium(MediumPayload mediumPayload){
            Console.WriteLine("Implement logic here...");
            Console.WriteLine(mediumPayload.canonicalUrl);
        }
    }
}
