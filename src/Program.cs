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
    
/// <summary>
/// The main Program class.
/// Contains the execution calls for the Content Converter and Third Party API Services.
/// </summary>
/// <remarks>
/// This is the entry point of the application. 
/// </remarks>
  class Program
    {
        /// <value>Instance of the Medium Service to be used throughout the program execution.</value>
        private readonly IThirdPartyBlogService<MediumPoco> _mediumService;
        /// <value>Instance of the DevTo Service to be used throughout the program execution.</value>
        private readonly IThirdPartyBlogService<DevToPoco> _devToService;
        /// <value>Instance of the Markdown Converter Service to be used throughout the program execution.</value>
        private readonly IConverter _markdownService;

        /// <summary>
        /// The main Program class' constructor.
        /// This is used as part of the .NET Dependency Injection functionality, binding the interfaces to concrete types from the startup class.
        /// </summary>.
         /// <param name="mediumService">Instance of the Medium Service being passed in during the Program's Startup</param>
         /// <param name="devtoService">Instance of the DevTo Service being passed in during the Program's Startup</param>
         /// <param name="markdownService">Instance of the Markdown Converter Service being passed in during the Program's Startup</param>
        public Program(IThirdPartyBlogService<MediumPoco> mediumService, IThirdPartyBlogService<DevToPoco> devToService, IConverter markdownService)
        {
            _mediumService = mediumService;
            _devToService = devToService;
            _markdownService = markdownService;
        }
        
        /// <summary>
        /// A Policy which has been built using the .NET Polly Framework.
        /// This is used to add retry (Maybe it's just a blip, let's try again) and circuit breaker (Stop doing it if it hurts, let's give the system a break) functionality.e.<
        /// </summary>
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicyAsync()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .WrapAsync(Policy.Handle<AggregateException>(x =>
                        {
                        var result = x.InnerException is HttpRequestException;
                        return result;
                        })
                        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
        }

        /// <summary>
        /// The main program entry point. This contains the Command Line's startup routine and calls the OnExecute logic (allowing for the passing in of command line flags etc. which are defined lower in this file).
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            return await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {

                    services.AddHttpClient("devto")
                    .SetHandlerLifetime(TimeSpan.FromMinutes(10))  //Set lifetime to ten minutes
                    .AddPolicyHandler(GetRetryPolicyAsync());

                    services.AddHttpClient()
                    .AddTransient<IThirdPartyBlogService<MediumPoco>, MediumService>()
                    .AddTransient<IThirdPartyBlogService<DevToPoco>, DevToService>()
                    .AddTransient<IConverter, ConvertFromMarkdownService>();
                }).UseConsoleLifetime()
                .RunCommandLineApplicationAsync<Program>(args);
        }

        
        /// <value>Directory path of the content to be converted and crossposted.</value>
        [Option(ShortName = "f", Description = "Directory path of the content to be converted and crossposted.")]
        public string directoryPath { get; } = "./testcases";

        /// <value>Boolean (True/False) on whether Recursive Subdirectories should be used for file access</value>
        [Option(ShortName = "r", Description = "Boolean (True/False) on whether Recursive Subdirectories should be used for file access")]
        public bool recursiveSubdirectories { get; } = false;

        /// <value>The search string to match against the names of files in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions. Defaults to *.md.</value>
        [Option(ShortName = "s", Description = "The search string to match against the names of files in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions. Defaults to *.md.")]
        public string searchPattern { get; } = "*.md";

        /// <value>Base URL of the website, not including protocol. e.g. www.cloudwithchris.com. This is used for converting any relative links to the original source, including the canonical URL.</value>
        [Option(ShortName = "u", Description = "Base URL of the website, not including protocol. e.g. www.cloudwithchris.com. This is used for converting any relative links to the original source, including the canonical URL.")]
        public string baseUrl { get; } = "www.cloudwithchris.com";

        /// <value>DevTo Integration Token. This is required if crossposting to DevTo, as it forms part of the URL for the API Call.</value>
        [Option(ShortName = "d", Description = "DevTo Integration Token. This is required if crossposting to DevTo, as it forms part of the URL for the API Call.")]
        public string devtoToken { get; }
        
        /// <value>Medium Author ID. This is required if crossposting to medium, as it forms part of the URL for the API Call.</value>
        [Option(ShortName = "a", Description = "Medium Author ID. This is required if crossposting to medium, as it forms part of the URL for the API Call.")]
        public string mediumAuthorId { get; }

        /// <value>Medium Integration Token. This is required to authorize to the Medium API</value>
        [Option(ShortName = "i", Description = "Medium Integration Token. This is required to authorize to the Medium API")]
        public string mediumToken { get; }

        /// <value>Protocol used on the website. Options are either HTTP or HTTPS. This is used for converting any relative links to the original source, including the canonical URL.</value>
        [Option(ShortName = "p", Description = "Protocol used on the website. Options are either HTTP or HTTPS. This is used for converting any relative links to the original source, including the canonical URL.")]
        public string protocol { get; } = "https";

        /// <summary>
        /// The OnExecute method contains the primary program logic. It gathers a list of files (based upon the input parameters), and then adds to them to a List of tasks to be processed asynchronously.
        /// </summary>
        async Task<int> OnExecute()
        {
            List<string> matchedFiles = (await _markdownService.listFiles(directoryPath, searchPattern, recursiveSubdirectories)).ToList();

            List<Task> listOfTasks = new List<Task>();
            for (int i = 0; i  < matchedFiles.Count; i++)
            {
                listOfTasks.Add(ConvertAndPostAsync(matchedFiles[i]));
            }

            await Task.WhenAll(listOfTasks);

            return await Task.Run(() => 0);
        }

        /// <summary>
        /// The ConvertAndPostAsync is executed on an individual file. It reads the file, processes it by removing localURLs and pulling the required frontmatter out of the document. This is then added to an appropriate POCO, either for Medium or for DevTo. As a future exercise, could investigate making this POCO agnostic of the third party service.
        /// </summary>
         /// <param name="filePath">File Path of the file to be processed.</param>
        async Task ConvertAndPostAsync(string filePath)
        {
            // Obtain the filename without the directoryPath, so that it can be used for the canonical URL details later on.
            string canonicalPath = filePath.Replace($"{directoryPath}\\", "");
            Console.WriteLine($"[Loop] Processing ${filePath}");

            // Read the file contents out to a string
            string sourceFile = await _markdownService.readFile(filePath);

            // Process the file contents, by replacing any localURL within the markdown to a full URL.
            string contentWithFrontMatter = await _markdownService.replaceLocalURLs(sourceFile, baseUrl);

            // Also take a copy of the file contents, but without the frontmatter. This may be needed dependant upon the third party service.
            string contentWithoutFrontMatter = await _markdownService.removeFrontMatter(contentWithFrontMatter);            

            // If either the mediumAuthorId or mediumToken are not completed, skip this step, as we don't have all of the needed details to call to the API.
            if (!(String.IsNullOrEmpty(mediumAuthorId) || String.IsNullOrEmpty(mediumToken)))
            {
                // If we were successful, it means we have both pieces of information and should be able to authenticate to Medium.
                Console.WriteLine($"[Medium] Crossposting {filePath}...");

                // Initialise the MediumPOCO by using several MarkDown Service methods, including getCanonicalURL, getFrontMatterProperty and getFrontMatterPropertyList.
                MediumPoco mediumPayload = new MediumPoco()
                {
                    title = await _markdownService.getFrontmatterProperty(sourceFile, "title"),
                    content = contentWithoutFrontMatter,
                    canonicalUrl = await _markdownService.getCanonicalUrl(protocol, baseUrl, canonicalPath),
                    tags = await _markdownService.getFrontMatterPropertyList(contentWithFrontMatter, "tags")
                };
                //TODO: Add some logic to handle bad authorization. If we detect one, we should cancel the loop as all will fail.
                await _mediumService.CreatePostAsync(mediumPayload, mediumToken, mediumAuthorId, await _markdownService.getFrontmatterProperty(contentWithFrontMatter, "youtube"));
                Console.WriteLine($"[Medium] Crossposting of {filePath} complete.");
            } else {
                Console.WriteLine($"[Medium] Missing required parameters to crosspost {filePath}. Skipping.");
            }

            // If the devtoToken is not available, skip this step, as we don't have the needed details to call to the API.
            if (!String.IsNullOrEmpty(devtoToken))
            {
                // If we were successful, it means we have both pieces of information and should be able to authenticate to DevTo if the credentials are correct.
                Console.WriteLine($"[DevTo] Crossposting {filePath}...");
                
                // Initialise the DevToPOCO by using several MarkDown Service methods, including getCanonicalURL, getFrontMatterProperty and getFrontMatterPropertyList.
                DevToPoco devToPayload = new DevToPoco()
                {
                    article = new Article()
                    {
                        title = await _markdownService.getFrontmatterProperty(sourceFile, "title"),
                        body_markdown = contentWithoutFrontMatter,
                        canonical_url = await _markdownService.getCanonicalUrl(protocol, baseUrl, canonicalPath),
                        tags = await _markdownService.getFrontMatterPropertyList(contentWithFrontMatter, "tags", 4, true),
                        description = await _markdownService.getFrontmatterProperty(contentWithFrontMatter, "description"),
                        series = (await _markdownService.getFrontMatterPropertyList(contentWithFrontMatter, "series", 1))[0]
                    }
                };
                //TODO: Add some logic to handle bad authorization. If we detect one, we should cancel the loop as all will fail.
                await _devToService.CreatePostAsync(devToPayload, devtoToken, null, await _markdownService.getFrontmatterProperty(contentWithFrontMatter, "youtube"));
                Console.WriteLine($"[DevTo] Crosspost of {filePath} complete.");
            } else {
                Console.WriteLine($"[DevTo] Missing required parameter to crosspost {filePath}. Skipping.");
            }
        }
    }
}