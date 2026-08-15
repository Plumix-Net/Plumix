using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/debug_overflow_indicator.dart

internal static class DebugOverflowIndicator
{
    private const double IndicatorFraction = 0.1;
    private const double TileSize = 10.0;
    private const double LabelFontSize = 7.5;
    private const double LabelPadding = 1.0;
    private static readonly IBrush LabelBackgroundBrush = new SolidColorBrush(Colors.White);
    private static readonly IBrush LabelForegroundBrush = new SolidColorBrush(Color.Parse("#FF900000"));
    private static readonly IBrush IndicatorBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
        EndPoint = new RelativePoint(TileSize, TileSize, RelativeUnit.Absolute),
        SpreadMethod = GradientSpreadMethod.Repeat,
        GradientStops = new GradientStops
        {
            new GradientStop(Color.FromArgb(0xBF, 0x00, 0x00, 0x00), 0.25),
            new GradientStop(Color.FromArgb(0xBF, 0xFF, 0xFF, 0x00), 0.25),
            new GradientStop(Color.FromArgb(0xBF, 0xFF, 0xFF, 0x00), 0.75),
            new GradientStop(Color.FromArgb(0xBF, 0x00, 0x00, 0x00), 0.75),
        },
    };

    public static void Paint(
        PaintingContext context,
        Point paintOffset,
        Rect containerRect,
        Rect childRect)
    {
        Rect translatedContainer = containerRect.Translate(paintOffset);
        Rect translatedChild = childRect.Translate(paintOffset);
        double overflowLeft = Math.Max(0, translatedContainer.Left - translatedChild.Left);
        double overflowTop = Math.Max(0, translatedContainer.Top - translatedChild.Top);
        double overflowRight = Math.Max(0, translatedChild.Right - translatedContainer.Right);
        double overflowBottom = Math.Max(0, translatedChild.Bottom - translatedContainer.Bottom);

        if (overflowLeft > Constants.PrecisionErrorTolerance)
        {
            var marker = new Rect(
                translatedContainer.X,
                translatedContainer.Y,
                translatedContainer.Width * IndicatorFraction,
                translatedContainer.Height);
            PaintMarker(context, marker);
            PaintLabel(
                context,
                $"LEFT OVERFLOWED BY {FormatPixels(overflowLeft)} PIXELS",
                new Point(marker.Right - LabelFontSize - LabelPadding, marker.Center.Y),
                -Math.PI / 2.0);
        }

        if (overflowRight > Constants.PrecisionErrorTolerance)
        {
            var marker = new Rect(
                translatedContainer.Right - translatedContainer.Width * IndicatorFraction,
                translatedContainer.Y,
                translatedContainer.Width * IndicatorFraction,
                translatedContainer.Height);
            PaintMarker(context, marker);
            PaintLabel(
                context,
                $"RIGHT OVERFLOWED BY {FormatPixels(overflowRight)} PIXELS",
                new Point(marker.Right - LabelFontSize - LabelPadding, marker.Center.Y),
                -Math.PI / 2.0);
        }

        if (overflowTop > Constants.PrecisionErrorTolerance)
        {
            var marker = new Rect(
                translatedContainer.X,
                translatedContainer.Y,
                translatedContainer.Width,
                translatedContainer.Height * IndicatorFraction);
            PaintMarker(context, marker);
            PaintLabel(
                context,
                $"TOP OVERFLOWED BY {FormatPixels(overflowTop)} PIXELS",
                new Point(marker.Center.X, marker.Bottom - LabelFontSize - LabelPadding),
                0);
        }

        if (overflowBottom > Constants.PrecisionErrorTolerance)
        {
            var marker = new Rect(
                translatedContainer.X,
                translatedContainer.Bottom - translatedContainer.Height * IndicatorFraction,
                translatedContainer.Width,
                translatedContainer.Height * IndicatorFraction);
            PaintMarker(context, marker);
            PaintLabel(
                context,
                $"BOTTOM OVERFLOWED BY {FormatPixels(overflowBottom)} PIXELS",
                new Point(marker.Center.X, marker.Bottom - LabelFontSize - LabelPadding),
                0);
        }
    }

    private static void PaintMarker(PaintingContext context, Rect marker)
    {
        if (marker.Width > 0 && marker.Height > 0)
        {
            context.DrawRectangle(IndicatorBrush, null, marker);
        }
    }

    private static void PaintLabel(
        PaintingContext context,
        string label,
        Point labelOffset,
        double rotation)
    {
        try
        {
            var layout = new TextLayout(
                text: label,
                typeface: new Typeface(
                    FontFamily.Default,
                    FontStyle.Normal,
                    FontWeight.ExtraBold,
                    FontStretch.Normal),
                fontSize: LabelFontSize,
                foreground: LabelForegroundBrush);
            var labelOrigin = new Point(-layout.Width / 2.0, 0);
            var background = new Rect(labelOrigin, new Size(layout.Width, layout.Height));

            context.PushTransform(
                Matrix4.TranslationValues(labelOffset.X, labelOffset.Y, 0.0),
                translated =>
            {
                if (Math.Abs(rotation) <= Constants.PrecisionErrorTolerance)
                {
                    translated.DrawRectangle(LabelBackgroundBrush, null, background);
                    translated.DrawTextLayout(layout, labelOrigin);
                    return;
                }

                translated.PushTransform(CreateRotationMatrix(rotation), rotated =>
                {
                    rotated.DrawRectangle(LabelBackgroundBrush, null, background);
                    rotated.DrawTextLayout(layout, labelOrigin);
                });
            });
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            // Host-less tests may not have a font manager; the striped marker remains visible.
        }
    }

    private static string FormatPixels(double value)
    {
        return Math.Abs(value - Math.Round(value)) < Constants.PrecisionErrorTolerance
            ? Math.Round(value).ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Matrix4 CreateRotationMatrix(double radians) => Matrix4.RotationZ(radians);
}
