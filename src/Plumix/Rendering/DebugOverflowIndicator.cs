using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/debug_overflow_indicator.dart

/// <summary>
/// An mixin indicator that is drawn when a [RenderObject] overflows its container.
/// </summary>
/// <remarks>
/// Dart's `DebugOverflowIndicatorMixin`. C# has no mixins, so the mixin's state and body live on
/// this helper and each render object that "mixes it in" owns one instance.
/// </remarks>
internal sealed class DebugOverflowIndicator : IDisposable
{
    private static readonly Color Black = new(0xBF000000);
    private static readonly Color Yellow = new(0xBFFFFF00);

    // The fraction of the container that the indicator covers.
    private const double IndicatorFraction = 0.1;
    private const double IndicatorFontSizePixels = 7.5;
    private const double IndicatorLabelPaddingPixels = 1.0;

    private static readonly TextStyle IndicatorTextStyle = new(
        Color: new Color(0xFF900000),
        FontSize: IndicatorFontSizePixels,
        FontWeight: FontWeight.ExtraBold);

    private static readonly IBrush IndicatorPaint = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0.0, 0.0, RelativeUnit.Absolute),
        EndPoint = new RelativePoint(10.0, 10.0, RelativeUnit.Absolute),
        SpreadMethod = GradientSpreadMethod.Repeat,
        GradientStops = new GradientStops
        {
            new GradientStop(Black, 0.25),
            new GradientStop(Yellow, 0.25),
            new GradientStop(Yellow, 0.75),
            new GradientStop(Black, 0.75),
        },
    }.ToImmutable();

    private static readonly IBrush LabelBackgroundPaint = new SolidColorBrush(new Color(0xFFFFFFFF)).ToImmutable();

    // One label painter per `_OverflowSide`; the labels are in English.
    private readonly TextPainter[] _indicatorLabel =
    [
        .. Enumerable.Range(0, 4).Select(_ => new TextPainter(textDirection: TextDirection.Ltr)),
    ];

    // Set to true to trigger a debug message in the console upon the next paint call. Will be reset
    // after each paint.
    private bool _overflowReportNeeded = true;

    /// <summary>Dart's mixin <c>dispose</c>: releases the four label painters.</summary>
    public void Dispose()
    {
        foreach (TextPainter painter in _indicatorLabel)
        {
            painter.Dispose();
        }
    }

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

    private static string FormatPixels(double value)
    {
        DebugAssert(value > 0.0, "value > 0.0");
        return value switch
        {
            > 10.0 => Diagnostics.ToStringAsFixed(value, 0),
            > 1.0 => Diagnostics.ToStringAsFixed(value, 1),
            _ => Diagnostics.ToStringAsPrecision(value, 3),
        };
    }

    private static List<OverflowRegionData> CalculateOverflowRegions(RelativeRect overflow, Rect containerRect)
    {
        var regions = new List<OverflowRegionData>();
        if (overflow.Left > 0.0)
        {
            var markerRect = new Rect(0.0, 0.0, containerRect.Width * IndicatorFraction, containerRect.Height);
            regions.Add(new OverflowRegionData(
                Rect: markerRect,
                Label: $"LEFT OVERFLOWED BY {FormatPixels(overflow.Left)} PIXELS",
                LabelOffset: new Point(markerRect.Left, markerRect.Center.Y)
                             + new Point(IndicatorFontSizePixels + IndicatorLabelPaddingPixels, 0.0),
                Rotation: Math.PI / 2.0,
                Side: OverflowSide.Left));
        }

        if (overflow.Right > 0.0)
        {
            var markerRect = new Rect(
                containerRect.Width * (1.0 - IndicatorFraction),
                0.0,
                containerRect.Width * IndicatorFraction,
                containerRect.Height);
            regions.Add(new OverflowRegionData(
                Rect: markerRect,
                Label: $"RIGHT OVERFLOWED BY {FormatPixels(overflow.Right)} PIXELS",
                LabelOffset: new Point(markerRect.Right, markerRect.Center.Y)
                             - new Point(IndicatorFontSizePixels + IndicatorLabelPaddingPixels, 0.0),
                Rotation: -Math.PI / 2.0,
                Side: OverflowSide.Right));
        }

        if (overflow.Top > 0.0)
        {
            var markerRect = new Rect(0.0, 0.0, containerRect.Width, containerRect.Height * IndicatorFraction);
            regions.Add(new OverflowRegionData(
                Rect: markerRect,
                Label: $"TOP OVERFLOWED BY {FormatPixels(overflow.Top)} PIXELS",
                LabelOffset: new Point(markerRect.Center.X, markerRect.Top)
                             + new Point(0.0, IndicatorLabelPaddingPixels),
                Rotation: 0.0,
                Side: OverflowSide.Top));
        }

        if (overflow.Bottom > 0.0)
        {
            var markerRect = new Rect(
                0.0,
                containerRect.Height * (1.0 - IndicatorFraction),
                containerRect.Width,
                containerRect.Height * IndicatorFraction);
            regions.Add(new OverflowRegionData(
                Rect: markerRect,
                Label: $"BOTTOM OVERFLOWED BY {FormatPixels(overflow.Bottom)} PIXELS",
                LabelOffset: new Point(markerRect.Center.X, markerRect.Bottom)
                             - new Point(0.0, IndicatorFontSizePixels + IndicatorLabelPaddingPixels),
                Rotation: 0.0,
                Side: OverflowSide.Bottom));
        }

        return regions;
    }

    private static void ReportOverflow(
        RenderObject self,
        RelativeRect overflow,
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
            .. overflow.Left > 0.0 ? new[] { $"{FormatPixels(overflow.Left)} pixels on the left" } : [],
            .. overflow.Top > 0.0 ? new[] { $"{FormatPixels(overflow.Top)} pixels on the top" } : [],
            .. overflow.Bottom > 0.0 ? new[] { $"{FormatPixels(overflow.Bottom)} pixels on the bottom" } : [],
            .. overflow.Right > 0.0 ? new[] { $"{FormatPixels(overflow.Right)} pixels on the right" } : [],
        ];
        DebugAssert(
            overflows.Count > 0,
            $"Somehow {runtimeType} didn't actually overflow like it thought it did.");
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

    /// To be called when the overflow indicators should be painted.
    ///
    /// Typically only called if there is an overflow, and only from within a debug build.
    ///
    /// See example code in [DebugOverflowIndicatorMixin] documentation.
    public void PaintOverflowIndicator(
        RenderObject self,
        PaintingContext context,
        Point offset,
        Rect containerRect,
        Rect childRect,
        List<DiagnosticsNode>? overflowHints = null)
    {
        RelativeRect overflow = RelativeRect.FromRect(containerRect, childRect);

        if (overflow.Left <= 0.0 && overflow.Right <= 0.0 && overflow.Top <= 0.0 && overflow.Bottom <= 0.0)
        {
            return;
        }

        List<OverflowRegionData> overflowRegions = CalculateOverflowRegions(overflow, containerRect);
        foreach (OverflowRegionData region in overflowRegions)
        {
            context.Canvas.DrawRectangle(IndicatorPaint, null, region.Rect.Translate((Vector)offset));
            TextPainter label = _indicatorLabel[(int)region.Side];
            var textSpan = label.Text as TextSpan;
            if (textSpan?.Text != region.Label)
            {
                label.Text = new TextSpan(text: region.Label, style: IndicatorTextStyle);
                label.Layout();
            }

            Point labelOffset = region.LabelOffset + offset;
            var centerOffset = new Point(-label.Width / 2.0, 0.0);
            var textBackgroundRect = new Rect(centerOffset, label.Size);
            context.Canvas.Save();
            context.Canvas.Translate(labelOffset.X, labelOffset.Y);
            context.Canvas.Rotate(region.Rotation);
            context.Canvas.DrawRectangle(LabelBackgroundPaint, null, textBackgroundRect);
            label.Paint(context.Canvas, centerOffset);
            context.Canvas.Restore();
        }

        if (_overflowReportNeeded)
        {
            _overflowReportNeeded = false;
            ReportOverflow(self, overflow, overflowHints);
        }
    }

    // Dart's `assert` for this file.
    private static void DebugAssert(bool condition, string message)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }

    // Describes which side the region data overflows on.
    private enum OverflowSide
    {
        Left,
        Top,
        Bottom,
        Right,
    }

    // Data used by the DebugOverflowIndicator to manage the regions and labels for the indicators.
    private readonly record struct OverflowRegionData(
        Rect Rect,
        string Label,
        Point LabelOffset,
        double Rotation,
        OverflowSide Side);
}
