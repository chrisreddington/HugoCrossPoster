using HugoCrossPoster.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
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
        public static async Task<int> Main(string[] args)
        {
            return await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddTransient<IThirdPartyBlogService<MediumPoco>, MediumService>();
                    services.AddTransient<IThirdPartyBlogService<DevToPoco>, DevToService>();
                    services.AddTransient<IConverter, ConvertFromMarkdownService>();
                }).UseConsoleLifetime()
                .RunCommandLineApplicationAsync<Program>(args);
        }

        
        [Option(ShortName = "f", Description = "File path of the content to be converted and crossposted.")]
        public string filePath { get; } 

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
            string sourceFile = await _markdownService.readFile(filePath);

            string contentWithFrontMatter = await _markdownService.replaceLocalURLs(sourceFile, baseUrl);
            string contentWithoutFrontMatter = await _markdownService.removeFrontMatter(contentWithFrontMatter);
        
            Console.WriteLine($"{contentWithoutFrontMatter}");

            // If either the authorId or mediumToken are not completed, skip this step
            if (!(String.IsNullOrEmpty(mediumAuthorId) || String.IsNullOrEmpty(mediumToken))){
                Console.WriteLine("[Medium] Crossposting...");
                MediumPoco mediumPayload = new MediumPoco(){
                    title = await _markdownService.getTitle(sourceFile),
                    content = contentWithoutFrontMatter,
                    canonicalUrl = await _markdownService.getCanonicalUrl(protocol, baseUrl, filePath),
                    tags = await _markdownService.getTags(contentWithFrontMatter)
                };
                await _mediumService.CreatePostAsync(mediumPayload, mediumToken, mediumAuthorId);
                Console.WriteLine("[Medium] Crosspost complete.");
            } else {
                Console.WriteLine("[Medium] Missing required parameters to Crosspost. Skipping.");
            }

            if (!String.IsNullOrEmpty(devtoToken)){
                Console.WriteLine("[DevTo] Crossposting...");
                DevToPoco devToPayload = new DevToPoco(){
                    article = new Article(){
                        title = await _markdownService.getTitle(sourceFile),
                        body_markdown = contentWithoutFrontMatter,
                        canonical_url = await _markdownService.getCanonicalUrl(protocol, baseUrl, filePath),
                        tags = await _markdownService.getTags(contentWithFrontMatter, true)
                    }
                };
                await _devToService.CreatePostAsync(devToPayload, devtoToken, null);
                Console.WriteLine("[DevTo] Crosspost complete.");
            } else {
                Console.WriteLine("[DevTo] Missing required parameter to Crosspost. Skipping.");
            }

            return await Task.Run(() => 0);
        }
    }
}