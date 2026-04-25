using Avalonia;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/paragraph.dart (adapted fallback for host-less test environments)

namespace Plumix.UI;

internal static class TextLayoutFallback
{
    internal static bool IsMissingFontManager(Exception exception)
    {
        return exception is InvalidOperationException
               && exception.Message.Contains("Avalonia.Platform.IFontManagerImpl", StringComparison.Ordinal);
    }

    internal static Size EstimateTextSize(
        string text,
        double fontSize,
        double maxWidth,
        double? height = null,
        double letterSpacing = 0)
    {
        var effectiveText = text ?? string.Empty;
        var effectiveFontSize = Math.Max(1.0, fontSize);
        var charWidth = Math.Max(0.1, effectiveFontSize * 0.6 + letterSpacing);
        var lineHeight = effectiveFontSize * (height is > 0 ? height.Value : 1.2);
        var lines = effectiveText.Split('\n');

        if (!double.IsInfinity(maxWidth) && maxWidth <= 0)
        {
            return new Size(0, lineHeight * Math.Max(1, lines.Length));
        }

        var lineCount = 0;
        var measuredWidth = 0.0;

        foreach (var line in lines)
        {
            var lineWidth = line.Length * charWidth;

            if (double.IsInfinity(maxWidth))
            {
                measuredWidth = Math.Max(measuredWidth, lineWidth);
                lineCount += 1;
                continue;
            }

            var wraps = Math.Max(1, (int)Math.Ceiling(lineWidth / maxWidth));
            measuredWidth = Math.Max(measuredWidth, Math.Min(lineWidth, maxWidth));
            lineCount += wraps;
        }

        return new Size(measuredWidth, Math.Max(1, lineCount) * lineHeight);
    }
}
