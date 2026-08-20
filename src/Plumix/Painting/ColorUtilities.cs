using Avalonia.Media;

namespace Plumix.Painting;

// C#-only infrastructure: shared equivalents of dart:ui Color.alphaBlend and Color.lerp.

public static class ColorUtilities
{
    public static Color AlphaBlend(Color foreground, Color background)
    {
        double foregroundAlpha = foreground.A / 255.0;
        double backgroundAlpha = background.A / 255.0;
        double outputAlpha = foregroundAlpha + (backgroundAlpha * (1.0 - foregroundAlpha));
        if (outputAlpha <= 0.0)
        {
            return Colors.Transparent;
        }

        byte Blend(byte foregroundChannel, byte backgroundChannel)
        {
            double channel = ((foregroundChannel * foregroundAlpha)
                              + (backgroundChannel * backgroundAlpha * (1.0 - foregroundAlpha)))
                             / outputAlpha;
            return (byte)Math.Clamp(
                (int)Math.Round(channel, MidpointRounding.AwayFromZero),
                byte.MinValue,
                byte.MaxValue);
        }

        return Color.FromArgb(
            (byte)Math.Clamp(
                (int)Math.Round(outputAlpha * 255.0, MidpointRounding.AwayFromZero),
                byte.MinValue,
                byte.MaxValue),
            Blend(foreground.R, background.R),
            Blend(foreground.G, background.G),
            Blend(foreground.B, background.B));
    }

    public static Color Lerp(Color from, Color to, double t)
    {
        byte LerpChannel(byte start, byte end) =>
            (byte)Math.Clamp(
                (int)Math.Round(start + ((end - start) * t), MidpointRounding.AwayFromZero),
                byte.MinValue,
                byte.MaxValue);

        return Color.FromArgb(
            LerpChannel(from.A, to.A),
            LerpChannel(from.R, to.R),
            LerpChannel(from.G, to.G),
            LerpChannel(from.B, to.B));
    }

    public static Color? Lerp(Color? from, Color? to, double t)
    {
        Color start = from ?? Colors.Transparent;
        Color end = to ?? Colors.Transparent;
        if (!from.HasValue)
        {
            start = Color.FromArgb(0, end.R, end.G, end.B);
        }
        if (!to.HasValue)
        {
            end = Color.FromArgb(0, start.R, start.G, start.B);
        }
        return from.HasValue || to.HasValue ? Lerp(start, end, t) : null;
    }

    public static double? LerpDouble(double? from, double? to, double t)
    {
        if (!from.HasValue && !to.HasValue)
        {
            return null;
        }

        double start = from ?? 0.0;
        double end = to ?? 0.0;
        return start + ((end - start) * t);
    }
}
