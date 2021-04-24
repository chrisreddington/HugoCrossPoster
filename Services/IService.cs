using System.Threading.Tasks;

namespace HugoCrossPoster.Services
{
    public interface IThirdPartyBlogService<T>
    {
        Task CreatePostAsync(T mediumPoco, string integrationToken, string authorId = null);
    }
}