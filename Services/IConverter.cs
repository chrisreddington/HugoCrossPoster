using System.Collections.Generic;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    public interface IConverter
    {
        Task<string> getCanonicalUrl(string protocol, string baseUrl, string fileName);
        Task<List<string>> getTags (string fileContents, bool humanize = false);
        Task<string> getTitle (string fileContents);
        Task<string> readFile(string fileName);
        Task<string> removeFrontMatter(string fileContents);
        Task<string> replaceLocalURLs(string fileContents, string baseUrl);
    }
}