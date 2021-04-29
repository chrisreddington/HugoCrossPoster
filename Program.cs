using HugoCrossPoster.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HugoCrossPoster
{
  class Program
    {
        private IThirdPartyBlogService<MediumPoco> _mediumService;
        private IThirdPartyBlogService<DevToPoco> _devToService;
        private IConverter _markdownService;

        public Program(IThirdPartyBlogService<MediumPoco> mediumService, IThirdPartyBlogService<DevToPoco> devToService, IConverter markdownService)
        {
            _mediumService = mediumService;
            _devToService = devToService;
            _markdownService = markdownService;
        }
        
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                            retryAttempt)));
        }

        public static async Task<int> Main(string[] args)
        {
            return await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {

                    services.AddHttpClient("devto")
                    .SetHandlerLifetime(TimeSpan.FromMinutes(10))  //Set lifetime to ten minutes
                    .AddPolicyHandler(GetRetryPolicy());

                    services.AddHttpClient()
                    .AddTransient<IThirdPartyBlogService<MediumPoco>, MediumService>()
                    .AddTransient<IThirdPartyBlogService<DevToPoco>, DevToService>()
                    .AddTransient<IConverter, ConvertFromMarkdownService>();
                }).UseConsoleLifetime()
                .RunCommandLineApplicationAsync<Program>(args);
        }

        
        [Option(ShortName = "f", Description = "Directory path of the content to be converted and crossposted.")]
        public string directoryPath { get; } = "./testcases";

        [Option(ShortName = "r", Description = "Boolean (True/False) on whether Recursive Subdirectories should be used for file access")]
        public bool recursiveSubdirectories { get; } = false;

        [Option(ShortName = "s", Description = "The search string to match against the names of files in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions. Defaults to *.md.")]
        public string searchPattern { get; } = "*.md";

        [Option(ShortName = "u", Description = "Base URL of the website, not including protocol. e.g. www.cloudwithchris.com. This is used for converting any relative links to the original source, including the canonical URL.")]
        public string baseUrl { get; } = "www.cloudwithchris.com";

        [Option(ShortName = "d", Description = "DevTo Integration Token. This is required if crossposting to DevTo, as it forms part of the URL for the API Call.")]
        public string devtoToken { get; }

        [Option(ShortName = "a", Description = "Medium Author ID. This is required if crossposting to medium, as it forms part of the URL for the API Call.")]
        public string mediumAuthorId { get; }
        
        [Option(ShortName = "i", Description = "Medium Integration Token. This is required to authorize to the Medium API")]
        public string mediumToken { get; }

        [Option(ShortName = "p", Description = "Protocol used on the website. Options are either HTTP or HTTPS. This is used for converting any relative links to the original source, including the canonical URL.")]
        public string protocol { get; } = "https";

               

        async Task<int> OnExecute()
        {
            List<string> matchedFiles = (await _markdownService.listFiles(directoryPath, searchPattern, recursiveSubdirectories)).ToList();

            List<Task> listOfTasks = new List<Task>();
            for (int i = 0; i  < matchedFiles.Count(); i++)
            {
                listOfTasks.Add(ConvertAndPostAsync(matchedFiles[i]));
            }

            await Task.WhenAll(listOfTasks);

            return await Task.Run(() => 0);
        }

        async Task ConvertAndPostAsync(string filePath){
            string canonicalPath = filePath.Replace($"{directoryPath}\\", "");
            Console.WriteLine($"[Loop] Processing ${filePath}");
            string sourceFile = await _markdownService.readFile(filePath);

            string contentWithFrontMatter = await _markdownService.replaceLocalURLs(sourceFile, baseUrl);
            string contentWithoutFrontMatter = await _markdownService.removeFrontMatter(contentWithFrontMatter);            

            // If either the authorId or mediumToken are not completed, skip this step
            if (!(String.IsNullOrEmpty(mediumAuthorId) || String.IsNullOrEmpty(mediumToken))){
                Console.WriteLine($"[Medium] Crossposting {filePath}...");
                MediumPoco mediumPayload = new MediumPoco(){
                    title = await _markdownService.getTitle(sourceFile),
                    content = contentWithoutFrontMatter,
                    canonicalUrl = await _markdownService.getCanonicalUrl(protocol, baseUrl, canonicalPath),
                    tags = await _markdownService.getTags(contentWithFrontMatter)
                };
                await _mediumService.CreatePostAsync(mediumPayload, mediumToken, mediumAuthorId, await _markdownService.getYoutube(contentWithFrontMatter));
                Console.WriteLine($"[Medium] Crossposting of {filePath} complete.");
            } else {
                Console.WriteLine($"[Medium] Missing required parameters to crosspost {filePath}. Skipping.");
            }

            if (!String.IsNullOrEmpty(devtoToken)){
                Console.WriteLine($"[DevTo] Crossposting {filePath}...");
                DevToPoco devToPayload = new DevToPoco(){
                    article = new Article(){
                        title = await _markdownService.getTitle(sourceFile),
                        body_markdown = contentWithoutFrontMatter,
                        canonical_url = await _markdownService.getCanonicalUrl(protocol, baseUrl, canonicalPath),
                        tags = await _markdownService.getTags(contentWithFrontMatter, 4, true),
                        description = await _markdownService.getDescription(contentWithFrontMatter),
                        series = await _markdownService.getSeries(contentWithFrontMatter)
                    }
                };
                await _devToService.CreatePostAsync(devToPayload, devtoToken, null, await _markdownService.getYoutube(contentWithFrontMatter));
                Console.WriteLine($"[DevTo] Crosspost of {filePath} complete.");
            } else {
                Console.WriteLine($"[DevTo] Missing required parameter to crosspost {filePath}. Skipping.");
            }
        }
    }
}