using AutomovilClub.Backend.Enums;

namespace AutomovilClub.Backend.Extensions
{
    public static class EnumExtensions
    {
        public static string ToFriendlyString(this Rol rol)
        {
            return rol.ToString().Replace('_', ' ');
        }
    }
}
