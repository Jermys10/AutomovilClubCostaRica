using System.Drawing;

namespace AutomovilClub.Backend.Extensions
{
    public static class StringExtensions
    {
        public static (string firstLine, string secondLine) SplitName(string fullName, int maxChars)
        {
            if (fullName.Length <= maxChars)
            {
                return (fullName, string.Empty);
            }

            var names = fullName.Split(' ');
            var firstLine = names[0];
            var secondLine = string.Join(" ", names.Skip(1));

            while (firstLine.Length + secondLine.Length > maxChars)
            {
                if (secondLine.Contains(' '))
                {
                    var splitIndex = secondLine.LastIndexOf(' ');
                    firstLine += " " + secondLine.Substring(0, splitIndex);
                    secondLine = secondLine.Substring(splitIndex + 1);
                }
                else
                {
                    break;
                }
            }

            return (firstLine, secondLine);
        }

        public static (string firstLine, string secondLine) SplitName(string fullName, int maxWidth, Graphics graphics, Font font)
        {
            if (graphics.MeasureString(fullName, font).Width <= maxWidth)
            {
                return (fullName, string.Empty);
            }

            var names = fullName.Split(' ');
            var firstLine = names[0];
            var secondLine = string.Join(" ", names.Skip(1));

            while (graphics.MeasureString(firstLine + " " + secondLine, font).Width > maxWidth && secondLine.Contains(' '))
            {
                var splitIndex = secondLine.LastIndexOf(' ');
                firstLine += " " + secondLine.Substring(0, splitIndex);
                secondLine = secondLine.Substring(splitIndex + 1);
            }

            return (firstLine, secondLine);
        }
    }
}
