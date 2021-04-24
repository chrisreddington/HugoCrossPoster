using HugoCrossPoster.Services;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.Hosting.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HugoCrossPoster
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddTransient<IThirdPartyBlogService<MediumPoco>, MediumService>();
                    services.AddTransient<IThirdPartyBlogService<DevToPoco>, DevToService>();
                }).UseConsoleLifetime()
                .RunCommandLineApplicationAsync<Program>(args);
        }
            

        [Option(ShortName = "u", Description = "Base URL of the website, not including protocol. e.g. www.cloudwithchris.com")]
        public string baseUrl { get; } = "www.cloudwithchris.com";

        [Option(ShortName = "p", Description = "Protocol used, either HTTP or HTTPS")]
        public string protocol { get; } = "https";

        

        private IThirdPartyBlogService<MediumPoco> _mediumService;
        private IThirdPartyBlogService<DevToPoco> _devToService;

        public Program(IThirdPartyBlogService<MediumPoco> mediumService, IThirdPartyBlogService<DevToPoco> devToService)
        {
            _mediumService = mediumService;
            _devToService = devToService;
        }

        async Task<int> OnExecute()
        {
            string title;
            List<string> tags;
            string fileName = "contributing-to-a-hugo-theme.md";
            string sourceFile = await readFile(fileName);

            string contentWithFrontMatter = await replaceLocalURLs(sourceFile, baseUrl);
            string contentWithoutFrontMatter = await removeFrontMatter(contentWithFrontMatter);
            title = await getTitle(sourceFile);
            tags = await getTags(sourceFile);

            Console.WriteLine($"Title is {title}");
            Console.WriteLine($"{contentWithoutFrontMatter}");

            /*MediumPoco mediumPayload = new MediumPoco(){
                title = title,
                content = contentWithoutFrontMatter,
                canonicalUrl = await getCanonicalUrl(protocol, baseUrl, fileName),
                tags = await getTags(contentWithFrontMatter)
            };

            await _mediumService.CreatePostAsync(mediumPayload, "", "");*/

            DevToPoco devToPayload = new DevToPoco(){
                article = new Article(){
                    title = title,
                    body_markdown = contentWithoutFrontMatter,
                    canonical_url = await getCanonicalUrl(protocol, baseUrl, fileName),
                    tags = await getTags(contentWithFrontMatter, true)
                }
            };

            await _devToService.CreatePostAsync(devToPayload, "", null);

            return await Task.Run(() => 0);
        }

        static async Task<string> getCanonicalUrl(string protocol, string baseUrl, string fileName){
            string fileNamewithoutExtension = fileName.Replace(".md", "");
            return new UriBuilder(protocol, baseUrl, -1, fileNamewithoutExtension).ToString();
        }

        static async Task<string> readFile(string fileName)
        {
            // Simply call out to the System.IO.File.ReadAllTextAsync method.
            return await System.IO.File.ReadAllTextAsync(fileName);
        }

        static async Task<string> getTitle (string fileContents)
        {
            // Find the title frontmatter in the YAML
            string pattern = @"title: (?<title>.+)";
            Match match = Regex.Match(fileContents, pattern);

            // Return the value of the <title> matched group
            return await Task<string>.Run(() => match.Groups["title"].Value);
        }

        static async Task<List<string>> getTags (string fileContents, bool humanize = false)
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

        static async Task<string> replaceLocalURLs(string fileContents, string baseUrl)
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

        static async Task<string> removeFrontMatter(string fileContents)
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