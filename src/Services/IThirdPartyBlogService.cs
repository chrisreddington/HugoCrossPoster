using System.Net.Http;
using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    /// <summary>
    /// Interface to provide consistency across Third Party Blog Service implementations.
    /// </summary>
    /// <remarks>
    /// Contains the required methods for any concrete third party blog service to be implemented. 
    /// </remarks>
    public interface IThirdPartyBlogService<in T>
    {
        Task<HttpResponseMessage> CreatePostAsync(T articleObject, string integrationToken, string authorId = null, string youtube = null);
        Task<string> AppendYouTubeInformation(string originalBody, string youtube);
        Task<string> ReplaceEmbeddedTweets(string fileContents);
    }
}