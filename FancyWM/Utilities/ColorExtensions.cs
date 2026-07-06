using System;
using System.Windows.Media;

namespace FancyWM.Utilities
{
    internal static class ColorExtensions
    {
        public static Color WithOpacity(this Color color, double opacity)
        {
            return Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
        }

        public static (double Hue, double Saturation, double Value) ToHsv(this Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double hue = 0;
            if (delta > 0.0)
            {
                if (max == r)
                {
                    hue = 60 * (((g - b) / delta) % 6);
                }
                else if (max == g)
                {
                    hue = 60 * (((b - r) / delta) + 2);
                }
                else
                {
                    hue = 60 * (((r - g) / delta) + 4);
                }
            }
            if (hue < 0)
            {
                hue += 360;
            }

            double saturation = max <= 0.0 ? 0.0 : delta / max;
            double value = max;
            return (hue, saturation, value);
        }

        public static Color FromHsv(double hue, double saturation, double value, byte alpha = 255)
        {
            hue = ((hue % 360) + 360) % 360;
            saturation = Math.Clamp(saturation, 0, 1);
            value = Math.Clamp(value, 0, 1);

            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = value - c;

            var (r, g, b) = hue switch
            {
                < 60 => (c, x, 0.0),
                < 120 => (x, c, 0.0),
                < 180 => (0.0, c, x),
                < 240 => (0.0, x, c),
                < 300 => (x, 0.0, c),
                _ => (c, 0.0, x),
            };

            return Color.FromArgb(
                alpha,
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        public static string ToCss(this Color color)
        {
            if (color == Colors.White)
            {
                return "white";
            }
            if (color == Colors.Black)
            {
                return "black";
            }
            if (color.A == 255)
            {
                return $"rgb({color.R}, {color.G}, {color.B})";
            }
            return $"rgba({color.R}, {color.G}, {color.B}, {color.A / 255.0:F})";
        }
    }
}
