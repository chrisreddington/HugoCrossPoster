using System.Collections.Generic;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    /// <summary>
    /// Interface to provide consistency across Content Conversion implementations (e.g. markdown, HTML, etc.).
    /// </summary>
    /// <remarks>
    /// Contains the required methods for any concrete content converter to be implemented. 
    /// </remarks>
    public interface IConverter
    {
        Task<string> getCanonicalUrl(string protocol, string baseUrl, string fileName);
        Task<List<string>> getTags (string fileContents, int count = 10, bool humanize = false);
        Task<string> getFrontmatterProperty (string fileContents, string key);
        Task<List<string>> getFrontMatterPropertyList (string fileContents, string key, int count = 10, bool urlize = false);
        Task<string> getSeries (string fileContents);
        Task<IEnumerable<string>> listFiles(string directoryPath, string searchPattern, bool recursiveSubdirectories);
        Task<string> readFile(string fileName);
        Task<string> removeFrontMatter(string fileContents);
        Task<string> replaceLocalURLs(string fileContents, string baseUrl);
    }
}