using HugoCrossPoster.Services;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.Hosting.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
                    services.AddTransient<IConverter, ConvertFromMarkdownService>();
                }).UseConsoleLifetime()
                .RunCommandLineApplicationAsync<Program>(args);
        }
            

        [Option(ShortName = "u", Description = "Base URL of the website, not including protocol. e.g. www.cloudwithchris.com")]
        public string baseUrl { get; } = "www.cloudwithchris.com";

        [Option(ShortName = "p", Description = "Protocol used, either HTTP or HTTPS")]
        public string protocol { get; } = "https";

        

        private IThirdPartyBlogService<MediumPoco> _mediumService;
        private IThirdPartyBlogService<DevToPoco> _devToService;
        private IConverter _markdownService;

        public Program(IThirdPartyBlogService<MediumPoco> mediumService, IThirdPartyBlogService<DevToPoco> devToService, IConverter markdownService)
        {
            _mediumService = mediumService;
            _devToService = devToService;
            _markdownService = markdownService;
        }

        async Task<int> OnExecute()
        {
            string fileName = "contributing-to-a-hugo-theme.md";
            string sourceFile = await _markdownService.readFile(fileName);

            string contentWithFrontMatter = await _markdownService.replaceLocalURLs(sourceFile, baseUrl);
            string contentWithoutFrontMatter = await _markdownService.removeFrontMatter(contentWithFrontMatter);
        
            Console.WriteLine($"{contentWithoutFrontMatter}");

            /*MediumPoco mediumPayload = new MediumPoco(){
                title = await _markdownService.getTitle(sourceFile),
                content = contentWithoutFrontMatter,
                canonicalUrl = await _markdownService.getCanonicalUrl(protocol, baseUrl, fileName),
                tags = await _markdownService.getTags(contentWithFrontMatter)
            };

            await _mediumService.CreatePostAsync(mediumPayload, "", "");*/

            DevToPoco devToPayload = new DevToPoco(){
                article = new Article(){
                    title = await _markdownService.getTitle(sourceFile),
                    body_markdown = contentWithoutFrontMatter,
                    canonical_url = await _markdownService.getCanonicalUrl(protocol, baseUrl, fileName),
                    tags = await _markdownService.getTags(contentWithFrontMatter, true)
                }
            };

            await _devToService.CreatePostAsync(devToPayload, "", null);

            return await Task.Run(() => 0);
        }
    }
}