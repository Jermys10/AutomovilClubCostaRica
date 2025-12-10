namespace AutomovilClub.Backend.Helpers
{
    public class FilesHelper : IFilesHelper
    {
        private readonly IWebHostEnvironment _env;

        public FilesHelper(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadPhoto(IFormFile file, string folder)
        {
            string path = string.Empty;
            string pic = string.Empty;

            if (file != null)
            {
                pic = Path.GetFileName(file.FileName);
                string raiz = _env.WebRootPath.ToString();

                path = $"{raiz}{folder}{pic}";

                using (Stream fileStream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }

            return pic;
        }
    }
}
