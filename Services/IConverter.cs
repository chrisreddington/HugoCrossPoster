using System.Collections.Generic;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    public interface IConverter
    {
        Task<string> getCanonicalUrl(string protocol, string baseUrl, string fileName);
        Task<List<string>> getTags (string fileContents, int count = 10, bool humanize = false);
        Task<string> getDescription (string fileContents);
        Task<string> getTitle (string fileContents);
        Task<string> getSeries (string fileContents);
        Task<string> getYoutube (string fileContents);
        Task<IEnumerable<string>> listFiles(string directoryPath, string searchPattern, bool recursiveSubdirectories);
        Task<string> readFile(string fileName);
        Task<string> removeFrontMatter(string fileContents);
        Task<string> replaceLocalURLs(string fileContents, string baseUrl);
    }
}