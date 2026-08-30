using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/debug_overflow_indicator.dart

/// <summary>
/// An mixin indicator that is drawn when a [RenderObject] overflows its container.
/// </summary>
/// <remarks>
/// Dart's `DebugOverflowIndicatorMixin`. C# has no mixins, so the mixin's state and body live on
/// this helper and each render object that "mixes it in" owns one instance.
/// </remarks>
internal sealed class DebugOverflowIndicator
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

    private bool _overflowReportNeeded = true;

    /// <summary>
    /// Dart's <c>DebugOverflowIndicatorMixin.reassemble</c>: users expect the overflow error to be
    /// reported again after a hot reload, so the one-shot report flag is armed again. Dart wraps the
    /// reset in an `assert`, so it only happens in a debug build.
    /// </summary>
    public void Reassemble()
    {
        if (Constants.KDebugMode)
        {
            _overflowReportNeeded = true;
        }
    }

    /// To be called when the overflow indicators should be painted.
    ///
    /// Typically only called if there is an overflow, and only from within a debug build.
    public void PaintOverflowIndicator(
        RenderObject self,
        PaintingContext context,
        Point paintOffset,
        Rect containerRect,
        Rect childRect,
        List<DiagnosticsNode>? overflowHints = null)
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

        if (overflowLeft <= 0.0 && overflowRight <= 0.0 && overflowTop <= 0.0 && overflowBottom <= 0.0)
        {
            return;
        }

        if (_overflowReportNeeded)
        {
            _overflowReportNeeded = false;
            ReportOverflow(self, overflowLeft, overflowTop, overflowRight, overflowBottom, overflowHints);
        }
    }

    private static void ReportOverflow(
        RenderObject self,
        double overflowLeft,
        double overflowTop,
        double overflowRight,
        double overflowBottom,
        List<DiagnosticsNode>? overflowHints)
    {
        string runtimeType = Diagnostics.DescribeType(self.GetType());
        overflowHints ??= [];
        if (overflowHints.Count == 0)
        {
            overflowHints.Add(new ErrorDescription(
                $"The edge of the {runtimeType} that is "
                + "overflowing has been marked in the rendering with a yellow and black "
                + "striped pattern. This is usually caused by the contents being too big "
                + $"for the {runtimeType}."));
            overflowHints.Add(new ErrorHint(
                "This is considered an error condition because it indicates that there "
                + "is content that cannot be seen. If the content is legitimately bigger "
                + "than the available space, consider clipping it with a ClipRect widget "
                + $"before putting it in the {runtimeType}, or using a scrollable "
                + "container, like a ListView."));
        }

        List<string> overflows =
        [
            .. overflowLeft > 0.0 ? new[] { $"{FormatPixels(overflowLeft)} pixels on the left" } : [],
            .. overflowTop > 0.0 ? new[] { $"{FormatPixels(overflowTop)} pixels on the top" } : [],
            .. overflowBottom > 0.0 ? new[] { $"{FormatPixels(overflowBottom)} pixels on the bottom" } : [],
            .. overflowRight > 0.0 ? new[] { $"{FormatPixels(overflowRight)} pixels on the right" } : [],
        ];
        string overflowText;
        switch (overflows.Count)
        {
            case 1:
                overflowText = overflows[0];
                break;
            case 2:
                overflowText = $"{overflows[0]} and {overflows[^1]}";
                break;
            default:
                overflows[^1] = $"and {overflows[^1]}";
                overflowText = string.Join(", ", overflows);
                break;
        }

        List<DiagnosticsNode> hints = overflowHints;
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: new FlutterError($"A {runtimeType} overflowed by {overflowText}."),
            library: "rendering library",
            context: new ErrorDescription("during layout"),
            informationCollector: () =>
            [
                .. hints,
                self.DescribeForError($"The specific {runtimeType} in question is"),
                DiagnosticsNode.Message(
                    string.Concat(Enumerable.Repeat("\u25e2\u25e4", FlutterError.WrapWidth / 2)),
                    allowWrap: false),
            ]));
    }

    private static void PaintMarker(PaintingContext context, Rect marker)
    {
        if (marker.Width > 0 && marker.Height > 0)
        {
            context.Canvas.DrawRectangle(IndicatorBrush, null, marker);
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

            context.Canvas.Save();
            context.Canvas.Translate(labelOffset.X, labelOffset.Y);
            if (Math.Abs(rotation) > Constants.PrecisionErrorTolerance)
            {
                context.Canvas.Rotate(rotation);
            }

            context.Canvas.DrawRectangle(LabelBackgroundBrush, null, background);
            context.Canvas.DrawTextLayout(layout, labelOrigin);
            context.Canvas.Restore();
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            // Host-less tests may not have a font manager; the striped marker remains visible.
        }
    }

    private static string FormatPixels(double value)
    {
        return value switch
        {
            > 10.0 => value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture),
            > 1.0 => value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString("G3", System.Globalization.CultureInfo.InvariantCulture),
        };
    }

}
