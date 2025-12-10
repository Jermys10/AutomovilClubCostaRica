using System.Drawing;

namespace AutomovilClub.Backend.Helpers
{
    public interface IFontsHelper
    {
        Font GetFont(string fontFamily, float emSize, FontStyle style = FontStyle.Regular);
    }
}
