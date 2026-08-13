using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/slider_theme.dart
// material_ui/lib/src/slider_parts.dart
// material_ui/lib/src/range_slider_parts.dart
// material_ui/lib/src/slider_value_indicator_shape.dart

public abstract class SliderComponentShape
{
    public static SliderComponentShape NoThumb { get; } = new NoSliderComponentShape();

    public static SliderComponentShape NoOverlay { get; } = new NoSliderComponentShape();

    public abstract Size GetPreferredSize(bool isEnabled, bool isDiscrete);

    public abstract void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextLayout? labelLayout,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow);

    private sealed class NoSliderComponentShape : SliderComponentShape
    {
        public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => default;

        public override void Paint(
            PaintingContext context,
            Point center,
            Animation<double> activationAnimation,
            Animation<double> enableAnimation,
            bool isDiscrete,
            TextLayout? labelLayout,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            TextDirection textDirection,
            double value,
            double textScaleFactor,
            Size sizeWithOverflow)
        {
        }
    }
}

public abstract class SliderTickMarkShape
{
    public static SliderTickMarkShape NoTickMark { get; } = new NoSliderTickMarkShape();

    public abstract Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled);

    public abstract void Paint(
        PaintingContext context,
        Point center,
        Point thumbCenter,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        TextDirection textDirection);

    private sealed class NoSliderTickMarkShape : SliderTickMarkShape
    {
        public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled) => default;

        public override void Paint(
            PaintingContext context,
            Point center,
            Point thumbCenter,
            Animation<double> enableAnimation,
            SliderThemeData sliderTheme,
            TextDirection textDirection)
        {
        }
    }
}

public abstract class SliderTrackShape
{
    public virtual bool IsRounded => false;

    public virtual Rect GetPreferredRect(
        RenderBox parentBox,
        Point offset,
        SliderThemeData sliderTheme,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        double trackHeight = sliderTheme.TrackHeight ?? 0.0;
        double overlayWidth = sliderTheme.OverlayShape?.GetPreferredSize(isEnabled, isDiscrete).Width ?? 0.0;
        double thumbWidth = sliderTheme.ThumbShape?.GetPreferredSize(isEnabled, isDiscrete).Width ?? 0.0;
        Thickness padding = SliderShapePaint.ResolvePadding(parentBox);
        double horizontalInset = sliderTheme.Padding.HasValue ? 0.0 : Math.Max(overlayWidth, thumbWidth) / 2.0;
        double left = offset.X + padding.Left + horizontalInset;
        double contentWidth = parentBox.Size.Width - padding.Left - padding.Right;
        double width = Math.Max(0.0, contentWidth - (horizontalInset * 2.0));
        double contentHeight = parentBox.Size.Height - padding.Top - padding.Bottom;
        double top = offset.Y + padding.Top + ((contentHeight - trackHeight) / 2.0);
        return new Rect(left, top, width, trackHeight);
    }

    public abstract void Paint(
        PaintingContext context,
        Point offset,
        Point thumbCenter,
        Point? secondaryOffset,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection);
}

public sealed class RectangularSliderTrackShape : SliderTrackShape
{
    public override void Paint(
        PaintingContext context,
        Point offset,
        Point thumbCenter,
        Point? secondaryOffset,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        SliderShapePaint.PaintSliderTrack(
            context,
            GetPreferredRect(parentBox, offset, sliderTheme, isEnabled, isDiscrete),
            thumbCenter,
            secondaryOffset,
            sliderTheme,
            textDirection,
            rounded: false,
            gapped: false);
    }
}

public sealed class RoundedRectSliderTrackShape : SliderTrackShape
{
    public RoundedRectSliderTrackShape(double additionalActiveTrackHeight = 2.0)
    {
        AdditionalActiveTrackHeight = additionalActiveTrackHeight;
    }

    public double AdditionalActiveTrackHeight { get; }

    public override bool IsRounded => true;

    public override void Paint(
        PaintingContext context,
        Point offset,
        Point thumbCenter,
        Point? secondaryOffset,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        SliderShapePaint.PaintSliderTrack(
            context,
            GetPreferredRect(parentBox, offset, sliderTheme, isEnabled, isDiscrete),
            thumbCenter,
            secondaryOffset,
            sliderTheme,
            textDirection,
            rounded: true,
            gapped: false,
            AdditionalActiveTrackHeight);
    }
}

public sealed class GappedSliderTrackShape : SliderTrackShape
{
    public GappedSliderTrackShape(double additionalActiveTrackHeight = 2.0)
    {
        AdditionalActiveTrackHeight = additionalActiveTrackHeight;
    }

    public double AdditionalActiveTrackHeight { get; }

    public override bool IsRounded => true;

    public override void Paint(
        PaintingContext context,
        Point offset,
        Point thumbCenter,
        Point? secondaryOffset,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        SliderShapePaint.PaintSliderTrack(
            context,
            GetPreferredRect(parentBox, offset, sliderTheme, isEnabled, isDiscrete),
            thumbCenter,
            secondaryOffset,
            sliderTheme,
            textDirection,
            rounded: true,
            gapped: true,
            AdditionalActiveTrackHeight);
    }
}

public sealed class RoundSliderTickMarkShape : SliderTickMarkShape
{
    public RoundSliderTickMarkShape(double? tickMarkRadius = null)
    {
        TickMarkRadius = tickMarkRadius;
    }

    public double? TickMarkRadius { get; }

    public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled)
    {
        double radius = TickMarkRadius ?? (sliderTheme.TrackHeight ?? 0.0) / 4.0;
        return new Size(radius * 2.0, radius * 2.0);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Point thumbCenter,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        bool active = textDirection == TextDirection.Ltr
            ? center.X <= thumbCenter.X
            : center.X >= thumbCenter.X;
        Color color = active
            ? sliderTheme.ActiveTickMarkColor ?? Colors.Transparent
            : sliderTheme.InactiveTickMarkColor ?? Colors.Transparent;
        context.DrawCircle(
            new SolidColorBrush(color),
            null,
            center,
            GetPreferredSize(sliderTheme, true).Width / 2.0);
    }
}

public sealed class RoundSliderThumbShape : SliderComponentShape
{
    public RoundSliderThumbShape(
        double enabledThumbRadius = 10.0,
        double? disabledThumbRadius = null,
        double elevation = 1.0,
        double pressedElevation = 6.0)
    {
        EnabledThumbRadius = enabledThumbRadius;
        DisabledThumbRadius = disabledThumbRadius ?? enabledThumbRadius;
        Elevation = elevation;
        PressedElevation = pressedElevation;
    }

    public double EnabledThumbRadius { get; }
    public double DisabledThumbRadius { get; }
    public double Elevation { get; }
    public double PressedElevation { get; }

    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        double radius = isEnabled ? EnabledThumbRadius : DisabledThumbRadius;
        return new Size(radius * 2.0, radius * 2.0);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextLayout? labelLayout,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        Size size = GetPreferredSize(enableAnimation.Value >= 0.5, isDiscrete);
        Color color = enableAnimation.Value >= 0.5
            ? sliderTheme.ThumbColor ?? Colors.Transparent
            : sliderTheme.DisabledThumbColor ?? Colors.Transparent;
        context.DrawCircle(new SolidColorBrush(color), null, center, size.Width / 2.0);
    }
}

public sealed class HandleThumbShape : SliderComponentShape
{
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => new(4.0, 44.0);

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextLayout? labelLayout,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        Size size = SliderShapePaint.ResolveThumbSize(sliderTheme, activationAnimation.Value >= 0.5)
                    ?? GetPreferredSize(enableAnimation.Value >= 0.5, isDiscrete);
        Color color = enableAnimation.Value >= 0.5
            ? sliderTheme.ThumbColor ?? Colors.Transparent
            : sliderTheme.DisabledThumbColor ?? Colors.Transparent;
        SliderShapePaint.DrawRoundedBar(context, center, size, color);
    }
}

public sealed class RoundSliderOverlayShape : SliderComponentShape
{
    public RoundSliderOverlayShape(double overlayRadius = 24.0)
    {
        OverlayRadius = overlayRadius;
    }

    public double OverlayRadius { get; }

    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        return new Size(OverlayRadius * 2.0, OverlayRadius * 2.0);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextLayout? labelLayout,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        Color color = SliderShapePaint.ResolveStateColor(sliderTheme.OverlayColor)
                      ?? Colors.Transparent;
        context.DrawCircle(
            new SolidColorBrush(color),
            null,
            center,
            OverlayRadius * Math.Clamp(activationAnimation.Value, 0.0, 1.0));
    }
}

public abstract class SliderValueIndicatorShape : SliderComponentShape
{
    protected SliderValueIndicatorShape(double minimumWidth, double radius)
    {
        MinimumWidth = minimumWidth;
        Radius = radius;
    }

    protected double MinimumWidth { get; }
    protected double Radius { get; }

    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => new(MinimumWidth, 32.0);

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextLayout? labelLayout,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        if (activationAnimation.Value <= 0.0 || labelLayout is null)
        {
            return;
        }

        SliderShapePaint.PaintValueIndicator(
            context,
            center,
            labelLayout,
            sliderTheme,
            MinimumWidth,
            Radius);
    }
}

public sealed class RectangularSliderValueIndicatorShape : SliderValueIndicatorShape
{
    public RectangularSliderValueIndicatorShape() : base(32.0, 4.0)
    {
    }
}

public sealed class PaddleSliderValueIndicatorShape : SliderValueIndicatorShape
{
    public PaddleSliderValueIndicatorShape() : base(32.0, 16.0)
    {
    }
}

public sealed class DropSliderValueIndicatorShape : SliderValueIndicatorShape
{
    public DropSliderValueIndicatorShape() : base(32.0, 16.0)
    {
    }
}

public sealed class RoundedRectSliderValueIndicatorShape : SliderValueIndicatorShape
{
    public RoundedRectSliderValueIndicatorShape() : base(32.0, 16.0)
    {
    }
}

public abstract class RangeSliderTrackShape
{
    public virtual bool IsRounded => false;

    public virtual Rect GetPreferredRect(
        RenderBox parentBox,
        Point offset,
        SliderThemeData sliderTheme,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        double trackHeight = sliderTheme.TrackHeight ?? 0.0;
        double overlayWidth = sliderTheme.OverlayShape?.GetPreferredSize(isEnabled, isDiscrete).Width ?? 0.0;
        double thumbWidth = sliderTheme.RangeThumbShape?.GetPreferredSize(isEnabled, isDiscrete).Width ?? 0.0;
        Thickness padding = SliderShapePaint.ResolvePadding(parentBox);
        double horizontalInset = sliderTheme.Padding.HasValue
            ? thumbWidth / 2.0
            : Math.Max(overlayWidth, thumbWidth) / 2.0;
        double contentWidth = parentBox.Size.Width - padding.Left - padding.Right;
        double contentHeight = parentBox.Size.Height - padding.Top - padding.Bottom;
        return new Rect(
            offset.X + padding.Left + horizontalInset,
            offset.Y + padding.Top + ((contentHeight - trackHeight) / 2.0),
            Math.Max(0.0, contentWidth - (horizontalInset * 2.0)),
            trackHeight);
    }

    public abstract void Paint(
        PaintingContext context,
        Point offset,
        Point startThumbCenter,
        Point endThumbCenter,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection);
}

public sealed class RectangularRangeSliderTrackShape : RangeSliderTrackShape
{
    public override void Paint(
        PaintingContext context,
        Point offset,
        Point startThumbCenter,
        Point endThumbCenter,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        SliderShapePaint.PaintRangeTrack(
            context,
            GetPreferredRect(parentBox, offset, sliderTheme, isEnabled, isDiscrete),
            startThumbCenter,
            endThumbCenter,
            sliderTheme,
            rounded: false,
            gapped: false);
    }
}

public sealed class RoundedRectRangeSliderTrackShape : RangeSliderTrackShape
{
    public override bool IsRounded => true;

    public override void Paint(
        PaintingContext context,
        Point offset,
        Point startThumbCenter,
        Point endThumbCenter,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        SliderShapePaint.PaintRangeTrack(
            context,
            GetPreferredRect(parentBox, offset, sliderTheme, isEnabled, isDiscrete),
            startThumbCenter,
            endThumbCenter,
            sliderTheme,
            rounded: true,
            gapped: false);
    }
}

public sealed class GappedRangeSliderTrackShape : RangeSliderTrackShape
{
    public override bool IsRounded => true;

    public override void Paint(
        PaintingContext context,
        Point offset,
        Point startThumbCenter,
        Point endThumbCenter,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isEnabled,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        SliderShapePaint.PaintRangeTrack(
            context,
            GetPreferredRect(parentBox, offset, sliderTheme, isEnabled, isDiscrete),
            startThumbCenter,
            endThumbCenter,
            sliderTheme,
            rounded: true,
            gapped: true);
    }
}

public abstract class RangeSliderTickMarkShape
{
    public abstract Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled = false);

    public abstract void Paint(
        PaintingContext context,
        Point center,
        Point startThumbCenter,
        Point endThumbCenter,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        TextDirection textDirection);
}

public sealed class RoundRangeSliderTickMarkShape : RangeSliderTickMarkShape
{
    public RoundRangeSliderTickMarkShape(double? tickMarkRadius = null)
    {
        TickMarkRadius = tickMarkRadius;
    }

    public double? TickMarkRadius { get; }

    public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled = false)
    {
        double radius = TickMarkRadius ?? (sliderTheme.TrackHeight ?? 0.0) / 4.0;
        return new Size(radius * 2.0, radius * 2.0);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Point startThumbCenter,
        Point endThumbCenter,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        TextDirection textDirection)
    {
        double left = Math.Min(startThumbCenter.X, endThumbCenter.X);
        double right = Math.Max(startThumbCenter.X, endThumbCenter.X);
        bool active = center.X >= left && center.X <= right;
        Color color = active
            ? sliderTheme.ActiveTickMarkColor ?? Colors.Transparent
            : sliderTheme.InactiveTickMarkColor ?? Colors.Transparent;
        context.DrawCircle(
            new SolidColorBrush(color),
            null,
            center,
            GetPreferredSize(sliderTheme).Width / 2.0);
    }
}

public abstract class RangeSliderThumbShape
{
    public abstract Size GetPreferredSize(bool isEnabled, bool isDiscrete);

    public abstract void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isOnTop,
        bool isPressed,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        Thumb thumb);
}

public sealed class RoundRangeSliderThumbShape : RangeSliderThumbShape
{
    public RoundRangeSliderThumbShape(
        double enabledThumbRadius = 10.0,
        double? disabledThumbRadius = null,
        double elevation = 1.0,
        double pressedElevation = 6.0)
    {
        EnabledThumbRadius = enabledThumbRadius;
        DisabledThumbRadius = disabledThumbRadius ?? enabledThumbRadius;
        Elevation = elevation;
        PressedElevation = pressedElevation;
    }

    public double EnabledThumbRadius { get; }
    public double DisabledThumbRadius { get; }
    public double Elevation { get; }
    public double PressedElevation { get; }

    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        double radius = isEnabled ? EnabledThumbRadius : DisabledThumbRadius;
        return new Size(radius * 2.0, radius * 2.0);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isOnTop,
        bool isPressed,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        Thumb thumb)
    {
        double radius = GetPreferredSize(enableAnimation.Value >= 0.5, isDiscrete).Width / 2.0;
        Color color = enableAnimation.Value >= 0.5
            ? sliderTheme.ThumbColor ?? Colors.Transparent
            : sliderTheme.DisabledThumbColor ?? Colors.Transparent;
        IPen? pen = isOnTop && sliderTheme.OverlappingShapeStrokeColor.HasValue
            ? new Pen(new SolidColorBrush(sliderTheme.OverlappingShapeStrokeColor.Value), 1.0)
            : null;
        context.DrawCircle(new SolidColorBrush(color), pen, center, radius);
    }
}

public sealed class HandleRangeSliderThumbShape : RangeSliderThumbShape
{
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => new(4.0, 44.0);

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isOnTop,
        bool isPressed,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        Thumb thumb)
    {
        Size size = SliderShapePaint.ResolveThumbSize(sliderTheme, isPressed)
                    ?? GetPreferredSize(enableAnimation.Value >= 0.5, isDiscrete);
        Color color = enableAnimation.Value >= 0.5
            ? sliderTheme.ThumbColor ?? Colors.Transparent
            : sliderTheme.DisabledThumbColor ?? Colors.Transparent;
        SliderShapePaint.DrawRoundedBar(context, center, size, color);
    }
}

public abstract class RangeSliderValueIndicatorShape
{
    protected RangeSliderValueIndicatorShape(double minimumWidth, double radius)
    {
        MinimumWidth = minimumWidth;
        Radius = radius;
    }

    protected double MinimumWidth { get; }
    protected double Radius { get; }

    public virtual Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextLayout labelLayout,
        double textScaleFactor)
    {
        return new Size(Math.Max(MinimumWidth, labelLayout.Width + 16.0), labelLayout.Height + 8.0);
    }

    public virtual double GetHorizontalShift(
        RenderBox parentBox,
        Point center,
        TextLayout labelLayout,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        return 0.0;
    }

    public virtual void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        bool isOnTop,
        TextLayout labelLayout,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        Thumb thumb,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        if (activationAnimation.Value <= 0.0)
        {
            return;
        }

        SliderShapePaint.PaintValueIndicator(
            context,
            center,
            labelLayout,
            sliderTheme,
            MinimumWidth,
            Radius,
            isOnTop ? sliderTheme.OverlappingShapeStrokeColor : null);
    }
}

public sealed class RectangularRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    public RectangularRangeSliderValueIndicatorShape() : base(32.0, 4.0)
    {
    }
}

public sealed class PaddleRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    public PaddleRangeSliderValueIndicatorShape() : base(32.0, 16.0)
    {
    }
}

public sealed class DropRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    public DropRangeSliderValueIndicatorShape() : base(32.0, 16.0)
    {
    }
}

public sealed class RoundedRectRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    public RoundedRectRangeSliderValueIndicatorShape() : base(32.0, 16.0)
    {
    }
}

internal static class SliderShapePaint
{
    public static Thickness ResolvePadding(RenderBox parentBox)
    {
        return parentBox switch
        {
            RenderSlider slider => slider.Padding,
            RenderRangeSlider rangeSlider => rangeSlider.Padding,
            _ => default,
        };
    }

    public static void PaintSliderTrack(
        PaintingContext context,
        Rect rect,
        Point thumbCenter,
        Point? secondaryOffset,
        SliderThemeData theme,
        TextDirection direction,
        bool rounded,
        bool gapped,
        double additionalActiveTrackHeight = 0.0)
    {
        double gap = gapped ? (theme.TrackGap ?? 0.0) : 0.0;
        double activeHeight = rect.Height + additionalActiveTrackHeight;
        bool ltr = direction == TextDirection.Ltr;
        Color active = theme.ActiveTrackColor ?? Colors.Transparent;
        Color inactive = theme.InactiveTrackColor ?? Colors.Transparent;
        DrawTrackSegment(context, rect.Left, thumbCenter.X - gap, rect.Center.Y, activeHeight, ltr ? active : inactive, rounded);
        DrawTrackSegment(context, thumbCenter.X + gap, rect.Right, rect.Center.Y, rect.Height, ltr ? inactive : active, rounded);
        if (secondaryOffset.HasValue)
        {
            double start = ltr ? thumbCenter.X + gap : secondaryOffset.Value.X;
            double end = ltr ? secondaryOffset.Value.X : thumbCenter.X - gap;
            DrawTrackSegment(
                context,
                start,
                end,
                rect.Center.Y,
                rect.Height,
                theme.SecondaryActiveTrackColor ?? Colors.Transparent,
                rounded);
        }
    }

    public static void PaintRangeTrack(
        PaintingContext context,
        Rect rect,
        Point startThumbCenter,
        Point endThumbCenter,
        SliderThemeData theme,
        bool rounded,
        bool gapped)
    {
        double gap = gapped ? theme.TrackGap ?? 0.0 : 0.0;
        double leftThumb = Math.Min(startThumbCenter.X, endThumbCenter.X);
        double rightThumb = Math.Max(startThumbCenter.X, endThumbCenter.X);
        Color active = theme.ActiveTrackColor ?? Colors.Transparent;
        Color inactive = theme.InactiveTrackColor ?? Colors.Transparent;
        DrawTrackSegment(context, rect.Left, leftThumb - gap, rect.Center.Y, rect.Height, inactive, rounded);
        DrawTrackSegment(context, leftThumb + gap, rightThumb - gap, rect.Center.Y, rect.Height, active, rounded);
        DrawTrackSegment(context, rightThumb + gap, rect.Right, rect.Center.Y, rect.Height, inactive, rounded);
    }

    public static void DrawRoundedBar(PaintingContext context, Point center, Size size, Color color)
    {
        double radius = Math.Min(size.Width, size.Height) / 2.0;
        context.DrawRectangle(
            new SolidColorBrush(color),
            null,
            new Rect(
                center.X - (size.Width / 2.0),
                center.Y - (size.Height / 2.0),
                size.Width,
                size.Height),
            radius,
            radius);
    }

    public static void PaintValueIndicator(
        PaintingContext context,
        Point center,
        TextLayout labelLayout,
        SliderThemeData theme,
        double minimumWidth,
        double radius,
        Color? overrideStrokeColor = null)
    {
        double width = Math.Max(minimumWidth, labelLayout.Width + 16.0);
        double height = labelLayout.Height + 8.0;
        var rect = new Rect(center.X - (width / 2.0), center.Y - height - 8.0, width, height);
        Color? stroke = overrideStrokeColor ?? theme.ValueIndicatorStrokeColor;
        IPen? pen = stroke.HasValue ? new Pen(new SolidColorBrush(stroke.Value), 1.0) : null;
        context.DrawRectangle(
            new SolidColorBrush(theme.ValueIndicatorColor ?? Colors.Transparent),
            pen,
            rect,
            radius,
            radius);
        context.DrawTextLayout(
            labelLayout,
            new Point(rect.X + ((width - labelLayout.Width) / 2.0), rect.Y + 4.0));
    }

    public static Size? ResolveThumbSize(SliderThemeData theme, bool pressed)
    {
        if (theme.ThumbSize is null)
        {
            return null;
        }

        var states = pressed
            ? new HashSet<WidgetState> { WidgetState.Pressed }
            : new HashSet<WidgetState>();
        return theme.ThumbSize.Resolve(states);
    }

    public static Color? ResolveStateColor(WidgetStateProperty<Color?>? property)
    {
        return property?.Resolve(new HashSet<WidgetState>());
    }

    private static void DrawTrackSegment(
        PaintingContext context,
        double start,
        double end,
        double centerY,
        double height,
        Color color,
        bool rounded)
    {
        if (end <= start || height <= 0.0)
        {
            return;
        }

        double radius = rounded ? height / 2.0 : 0.0;
        context.DrawRectangle(
            new SolidColorBrush(color),
            null,
            new Rect(start, centerY - (height / 2.0), end - start, height),
            radius,
            radius);
    }
}
