using System.Drawing;
using System.Drawing.Text;

namespace AutomovilClub.Backend.Helpers
{
    public class FontsHelper: IFontsHelper
    {
        private PrivateFontCollection privateFontCollection;

        public FontsHelper()
        {
            privateFontCollection = new PrivateFontCollection();
            LoadFonts();
        }

        private void LoadFonts()
        {
            // Ruta base de la aplicación
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Ruta completa a la carpeta de fuentes
            string fontsPath = Path.Combine(baseDirectory, "wwwroot", "fonts");

            // Verifica si la carpeta de fuentes existe
            if (!Directory.Exists(fontsPath))
            {
                throw new DirectoryNotFoundException($"The fonts directory '{fontsPath}' does not exist.");
            }

            // Obtén todos los archivos .ttf y .otf
            string[] fontFiles = Directory.GetFiles(fontsPath, "*.*")
                                          .Where(file => file.EndsWith(".ttf") || file.EndsWith(".otf"))
                                          .ToArray();

            foreach (string fontFile in fontFiles)
            {
                privateFontCollection.AddFontFile(fontFile);
            }
        }

        public Font GetFont(string fontFamily, float emSize, FontStyle style = FontStyle.Regular)
        {
            var fontFamilyObject = privateFontCollection.Families.FirstOrDefault(f => f.Name == fontFamily);
            if (fontFamilyObject != null)
            {
                return new Font(fontFamilyObject, emSize, style);
            }
            else
            {
                throw new Exception($"Font '{fontFamily}' not found.");
            }
        }
    }
}
