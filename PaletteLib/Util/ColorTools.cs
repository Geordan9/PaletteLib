using System;
using System.Drawing;

namespace PaletteLib.Util;

public static class ColorTools
{
    public static Color FromArgb(double r, double g, double b)
    {
        return Color.FromArgb(255, (byte) Math.Round(r * 255), (byte) Math.Round(g * 255),
            (byte) Math.Round(b * 255));
    }

    public static Color ConvertHsvToRgb(double h, double s, double v)
    {
        double r, g, b;

        if (s == 0)
        {
            r = v;
            g = v;
            b = v;
        }
        else
        {
            int i;
            double f, p, q, t;

            if (h == 360)
                h = 0;
            else
                h /= 60;

            i = (int) Math.Truncate(h);
            f = h - i;

            p = v * (1.0 - s);
            q = v * (1.0 - s * f);
            t = v * (1.0 - s * (1.0 - f));

            switch (i)
            {
                case 0:
                {
                    r = v;
                    g = t;
                    b = p;
                    break;
                }

                case 1:
                {
                    r = q;
                    g = v;
                    b = p;
                    break;
                }

                case 2:
                {
                    r = p;
                    g = v;
                    b = t;
                    break;
                }

                case 3:
                {
                    r = p;
                    g = q;
                    b = v;
                    break;
                }

                case 4:
                {
                    r = t;
                    g = p;
                    b = v;
                    break;
                }

                default:
                {
                    r = v;
                    g = p;
                    b = q;
                    break;
                }
            }
        }

        return Color.FromArgb(255, (byte) Math.Round(r * 255), (byte) Math.Round(g * 255),
            (byte) Math.Round(b * 255));
    }

    public static Color ConvertCMYKToRgb(double c, double m, double y, double k, double scale = 1)
    {
        c /= scale;
        m /= scale;
        y /= scale;
        k /= scale;

        var r = Convert.ToInt32(255.0 * (1 - c) * (1 - k));
        var g = Convert.ToInt32(255.0 * (1 - m) * (1 - k));
        var b = Convert.ToInt32(255.0 * (1 - y) * (1 - k));

        return Color.FromArgb(r, g, b);
    }

    public static Color ConvertLabToRgb(double L, double A, double B, double scale = 1)
    {
        L /= scale;
        A /= scale;
        B /= scale;


        double y = (L + 16) / 116,
            x = A / 500 + y,
            z = y - B / 200,
            r,
            g,
            b;

        x = 0.95047 * (x * x * x > 0.008856 ? x * x * x : (x - 16 / 116) / 7.787);
        y = 1.00000 * (y * y * y > 0.008856 ? y * y * y : (y - 16 / 116) / 7.787);
        z = 1.08883 * (z * z * z > 0.008856 ? z * z * z : (z - 16 / 116) / 7.787);

        r = x * 3.2406 + y * -1.5372 + z * -0.4986;
        g = x * -0.9689 + y * 1.8758 + z * 0.0415;
        b = x * 0.0557 + y * -0.2040 + z * 1.0570;

        r = r > 0.0031308 ? 1.055 * Math.Pow(r, 1 / 2.4) - 0.055 : 12.92 * r;
        g = g > 0.0031308 ? 1.055 * Math.Pow(g, 1 / 2.4) - 0.055 : 12.92 * g;
        b = b > 0.0031308 ? 1.055 * Math.Pow(b, 1 / 2.4) - 0.055 : 12.92 * b;

        return Color.FromArgb((byte) Math.Round(Math.Max(0, Math.Min(1, r)) * 255),
            (byte) Math.Round(Math.Max(0, Math.Min(1, g)) * 255),
            (byte) Math.Round(Math.Max(0, Math.Min(1, b)) * 255));
    }
}