namespace AutomovilClub.Backend.Helpers
{
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;

    public interface IFilesHelper
    {
        Task<string> UploadPhoto(IFormFile file, string folder);
    }
}
