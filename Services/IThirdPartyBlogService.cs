using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    /// <summary>
    /// Interface to provide consistency across Third Party Blog Service implementations.
    /// </summary>
    /// <remarks>
    /// Contains the required methods for any concrete third party blog service to be implemented. 
    /// </remarks>
    public interface IThirdPartyBlogService<T>
    {
        Task CreatePostAsync(T mediumPoco, string integrationToken, string authorId = null, string youtube = null);
    }
}