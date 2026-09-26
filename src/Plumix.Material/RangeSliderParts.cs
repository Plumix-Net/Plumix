using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/range_slider_parts.dart

/// <summary>
/// Base class for <see cref="RangeSlider"/> thumb shapes.
/// </summary>
/// <remarks>
/// <see cref="RoundRangeSliderThumbShape"/> is the default M2 thumb shape (a solid circle).
/// </remarks>
public abstract class RangeSliderThumbShape
{
    /// <summary>
    /// This abstract constructor enables subclasses to provide const-like constructors.
    /// </summary>
    protected RangeSliderThumbShape()
    {
    }

    /// <summary>
    /// Returns the preferred size of the shape, based on the given conditions.
    /// </summary>
    /// <remarks>
    /// <c>isDiscrete</c> is true if <see cref="RangeSlider.Divisions"/> is non-null. <c>isEnabled</c> is false
    /// when <see cref="RangeSlider.OnChanged"/> is null.
    /// </remarks>
    public abstract Size GetPreferredSize(bool isEnabled, bool isDiscrete);

    /// <summary>
    /// Paints the thumb shape based on the state passed to it.
    /// </summary>
    /// <remarks>
    /// <c>isOnTop</c> is true when this thumb is painted on top of the other thumb because it was the most
    /// recently selected. <c>thumb</c> specifies which of the two thumbs is painted; <c>isPressed</c> can
    /// give the selected thumb additional pressed-state feedback, such as a larger shadow.
    /// </remarks>
    public abstract void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        bool isDiscrete = false,
        bool isEnabled = false,
        bool? isOnTop = null,
        TextDirection? textDirection = null,
        Thumb? thumb = null,
        bool? isPressed = null);

    /// <summary>Dart's <c>Object.toString</c>, which the slider theme's diagnostics print for shapes.</summary>
    public override string ToString() => $"Instance of '{Diagnostics.DescribeType(GetType())}'";
}

/// <summary>
/// Base class for <see cref="RangeSlider"/> value indicator shapes.
/// </summary>
public abstract class RangeSliderValueIndicatorShape
{
    /// <summary>
    /// This abstract constructor enables subclasses to provide const-like constructors.
    /// </summary>
    protected RangeSliderValueIndicatorShape()
    {
    }

    /// <summary>
    /// Returns the preferred size of the shape, based on the given conditions.
    /// </summary>
    /// <remarks>
    /// The <c>labelPainter</c> determines the width of the shape: it is variable width because it is derived
    /// from a formatted string.
    /// </remarks>
    public abstract Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter labelPainter,
        double textScaleFactor);

    /// <summary>
    /// Determines the best offset to keep this shape on the screen.
    /// </summary>
    /// <remarks>
    /// Override this method when the center of the value indicator should be shifted from the vertical
    /// center of the thumb.
    /// </remarks>
    public virtual double GetHorizontalShift(
        RenderBox? parentBox = null,
        Point? center = null,
        TextPainter? labelPainter = null,
        Animation<double>? activationAnimation = null,
        double? textScaleFactor = null,
        Size? sizeWithOverflow = null)
    {
        return 0;
    }

    /// <summary>
    /// Paints the value indicator shape based on the state passed to it.
    /// </summary>
    /// <remarks>
    /// <c>isOnTop</c> marks the top-most value indicator, which is always the indicator of the most recently
    /// selected thumb; the default shapes stroke it for better visibility between the two indicators.
    /// <c>value</c> is the current parametric value (from 0.0 to 1.0) of the slider.
    /// </remarks>
    public abstract void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        bool? isDiscrete = null,
        bool? isOnTop = null,
        double? textScaleFactor = null,
        Size? sizeWithOverflow = null,
        TextDirection? textDirection = null,
        double? value = null,
        Thumb? thumb = null);

    /// <summary>Dart's <c>Object.toString</c>, which the slider theme's diagnostics print for shapes.</summary>
    public override string ToString() => $"Instance of '{Diagnostics.DescribeType(GetType())}'";
}

/// <summary>
/// Base class for <see cref="RangeSlider"/> tick mark shapes.
/// </summary>
/// <remarks>
/// This is a simplified version of <see cref="SliderComponentShape"/> with a <see cref="SliderThemeData"/>
/// passed when getting the preferred size.
/// </remarks>
public abstract class RangeSliderTickMarkShape
{
    /// <summary>
    /// This abstract constructor enables subclasses to provide const-like constructors.
    /// </summary>
    protected RangeSliderTickMarkShape()
    {
    }

    /// <summary>
    /// Returns the preferred size of the shape. It is used to help position the tick marks within the slider.
    /// </summary>
    public abstract Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled = false);

    /// <summary>
    /// Paints the slider track.
    /// </summary>
    /// <remarks>
    /// The track segment between the two thumbs is the active track segment. The track segments between
    /// the thumb and each end of the slider are the inactive track segments. In <see cref="TextDirection.Ltr"/>
    /// the start of the slider is on the left, and in <see cref="TextDirection.Rtl"/> it is on the right.
    /// </remarks>
    public abstract void Paint(
        PaintingContext context,
        Point center,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        bool isEnabled = false);

    /// <summary>Dart's <c>Object.toString</c>, which the slider theme's diagnostics print for shapes.</summary>
    public override string ToString() => $"Instance of '{Diagnostics.DescribeType(GetType())}'";
}

/// <summary>
/// Base class for <see cref="RangeSlider"/> track shapes.
/// </summary>
/// <remarks>
/// The slider's thumbs move along the track. A discrete slider's tick marks are drawn after the track, but
/// before the thumb, and are aligned with the track. <see cref="GetPreferredRect"/> helps position the
/// slider thumbs and tick marks relative to the track.
/// </remarks>
public abstract class RangeSliderTrackShape
{
    /// <summary>
    /// This abstract constructor enables subclasses to provide const-like constructors.
    /// </summary>
    protected RangeSliderTrackShape()
    {
    }

    /// <summary>
    /// Returns the preferred bounds of the shape.
    /// </summary>
    /// <remarks>
    /// It provides horizontal boundaries for the position of the thumbs. The <c>offset</c> is relative to
    /// the caller's bounding box.
    /// </remarks>
    public abstract Rect GetPreferredRect(
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Point offset = default,
        bool isEnabled = false,
        bool isDiscrete = false);

    /// <summary>
    /// Paints the track shape based on the state passed to it.
    /// </summary>
    /// <remarks>
    /// <c>offset</c> is the offset of the origin of the <c>parentBox</c> to the origin of its <c>context</c>
    /// canvas. <c>startThumbCenter</c>/<c>endThumbCenter</c> are relative to the canvas origin and divide
    /// the track between inactive and active.
    /// </remarks>
    public abstract void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        bool isEnabled = false,
        bool isDiscrete = false);

    /// <summary>
    /// Whether the track shape is rounded. This is used to determine the correct position of the thumbs in
    /// relation to the track. Defaults to false.
    /// </summary>
    public virtual bool IsRounded => false;

    /// <summary>Dart's <c>Object.toString</c>, which the slider theme's diagnostics print for shapes.</summary>
    public override string ToString() => $"Instance of '{Diagnostics.DescribeType(GetType())}'";
}

/// <summary>
/// Base range slider track shape that provides an implementation of <see cref="GetPreferredRect"/> for
/// default sizing.
/// </summary>
/// <remarks>
/// Dart's <c>mixin BaseRangeSliderTrackShape</c>, applied here as an intermediate base class. The height is
/// <see cref="SliderThemeData.TrackHeight"/> and the width is the parent box's width less the larger of the
/// widths of <see cref="SliderThemeData.RangeThumbShape"/> and <see cref="SliderThemeData.OverlayShape"/>.
/// </remarks>
public abstract class BaseRangeSliderTrackShape : RangeSliderTrackShape
{
    /// <summary>
    /// Returns a rect that represents the track bounds that fits within the <see cref="RangeSlider"/>.
    /// </summary>
    /// <remarks>
    /// The width is the width of the <see cref="RangeSlider"/>, padded by the max of the overlay and thumb
    /// radius. The height is <see cref="SliderThemeData.TrackHeight"/>. The rect is centered both
    /// horizontally and vertically within the slider bounds.
    /// </remarks>
    public override Rect GetPreferredRect(
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Point offset = default,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        DebugAssertions.Assert(sliderTheme.RangeThumbShape != null);
        DebugAssertions.Assert(sliderTheme.OverlayShape != null);
        DebugAssertions.Assert(sliderTheme.TrackHeight != null);
        Size thumbSize = sliderTheme.RangeThumbShape!.GetPreferredSize(isEnabled, isDiscrete);
        double overlayWidth = sliderTheme.OverlayShape!
            .GetPreferredSize(isEnabled, isDiscrete)
            .Width;
        double trackHeight = sliderTheme.TrackHeight!.Value;
        DebugAssertions.Assert(overlayWidth >= 0);
        DebugAssertions.Assert(trackHeight >= 0);

        // If the track colors are transparent, then override only the track height
        // to maintain overall Slider width.
        if (sliderTheme.ActiveTrackColor == Colors.Transparent
            && sliderTheme.InactiveTrackColor == Colors.Transparent)
        {
            trackHeight = 0;
        }

        double trackLeft =
            offset.X
            + (sliderTheme.Padding == null
                ? Math.Max(overlayWidth / 2, thumbSize.Width / 2)
                : (thumbSize.Width / 2));
        double trackTop = offset.Y + (parentBox.Size.Height - trackHeight) / 2;
        double trackRight =
            trackLeft
            + parentBox.Size.Width
            - (sliderTheme.Padding == null ? Math.Max(thumbSize.Width, overlayWidth) : thumbSize.Width);
        double trackBottom = trackTop + trackHeight;
        // If the parentBox's size less than slider's size the trackRight will be less than trackLeft, so switch them.
        return DartGeometry.RectFromLTRB(
            Math.Min(trackLeft, trackRight),
            trackTop,
            Math.Max(trackLeft, trackRight),
            trackBottom);
    }
}

/// <summary>
/// A <see cref="RangeSlider"/> track that's a simple rectangle.
/// </summary>
/// <remarks>
/// It paints a solid colored rectangle, vertically centered in the <c>parentBox</c>, padded by the
/// <see cref="RoundSliderOverlayShape"/> radius. The color is determined by the slider's enabled state and
/// the track segment's active state, which are defined by <see cref="SliderThemeData.ActiveTrackColor"/>,
/// <see cref="SliderThemeData.InactiveTrackColor"/>, <see cref="SliderThemeData.DisabledActiveTrackColor"/>
/// and <see cref="SliderThemeData.DisabledInactiveTrackColor"/>.
/// </remarks>
public class RectangularRangeSliderTrackShape : BaseRangeSliderTrackShape
{
    /// <summary>
    /// Create a slider track with rectangular outer edges.
    /// </summary>
    /// <remarks>
    /// The middle track segment is the selected range and is active, and the two outer track segments are
    /// inactive.
    /// </remarks>
    public RectangularRangeSliderTrackShape()
    {
    }

    public override void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        DebugAssertions.Assert(sliderTheme.DisabledActiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.DisabledInactiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.ActiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.InactiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.RangeThumbShape != null);
        DebugAssertions.Assert(enableAnimation != null);
        // Assign the track segment paints, which are left: active, right: inactive,
        // but reversed for right to left text.
        var activeTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledActiveTrackColor,
            end: sliderTheme.ActiveTrackColor);
        var inactiveTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledInactiveTrackColor,
            end: sliderTheme.InactiveTrackColor);
        var activePaint = new SolidColorBrush(activeTrackColorTween.Evaluate(enableAnimation!.Value));
        var inactivePaint = new SolidColorBrush(inactiveTrackColorTween.Evaluate(enableAnimation.Value));

        (Point leftThumbOffset, Point rightThumbOffset) = textDirection switch
        {
            TextDirection.Ltr => (startThumbCenter, endThumbCenter),
            _ => (endThumbCenter, startThumbCenter),
        };

        Rect trackRect = GetPreferredRect(
            parentBox: parentBox,
            offset: offset,
            sliderTheme: sliderTheme,
            isEnabled: isEnabled,
            isDiscrete: isDiscrete);
        Rect leftTrackSegment = DartGeometry.RectFromLTRB(
            trackRect.Left,
            trackRect.Top,
            leftThumbOffset.X,
            trackRect.Bottom);
        if (!DartGeometry.RectIsEmpty(leftTrackSegment))
        {
            context.Canvas.DrawRectangle(inactivePaint, null, leftTrackSegment);
        }

        Rect middleTrackSegment = DartGeometry.RectFromLTRB(
            leftThumbOffset.X,
            trackRect.Top,
            rightThumbOffset.X,
            trackRect.Bottom);
        if (!DartGeometry.RectIsEmpty(middleTrackSegment))
        {
            context.Canvas.DrawRectangle(activePaint, null, middleTrackSegment);
        }

        Rect rightTrackSegment = DartGeometry.RectFromLTRB(
            rightThumbOffset.X,
            trackRect.Top,
            trackRect.Right,
            trackRect.Bottom);
        if (!DartGeometry.RectIsEmpty(rightTrackSegment))
        {
            context.Canvas.DrawRectangle(inactivePaint, null, rightTrackSegment);
        }
    }
}

/// <summary>
/// The default shape of a <see cref="RangeSlider"/>'s track.
/// </summary>
/// <remarks>
/// It paints a solid colored rectangle with rounded edges, vertically centered in the <c>parentBox</c>,
/// padded by the larger of <see cref="RoundSliderOverlayShape"/>'s radius and
/// <see cref="RoundRangeSliderThumbShape"/>'s radius. The active segment is
/// <c>additionalActiveTrackHeight</c> (2) taller than the inactive segments.
/// </remarks>
public class RoundedRectRangeSliderTrackShape : BaseRangeSliderTrackShape
{
    /// <summary>
    /// Create a slider track with rounded outer edges.
    /// </summary>
    /// <remarks>
    /// The middle track segment is the selected range and is active, and the two outer track segments are
    /// inactive.
    /// </remarks>
    public RoundedRectRangeSliderTrackShape()
    {
    }

    /// <remarks>
    /// Dart's <c>paint</c> widens the base signature with <c>double additionalActiveTrackHeight = 2</c>; C#
    /// overrides cannot add parameters, so this override forwards to the overload that takes it.
    /// </remarks>
    public override void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        Paint(
            context,
            offset,
            parentBox,
            sliderTheme,
            enableAnimation,
            startThumbCenter,
            endThumbCenter,
            textDirection,
            additionalActiveTrackHeight: 2,
            isEnabled: isEnabled,
            isDiscrete: isDiscrete);
    }

    /// <summary>
    /// Dart's <c>paint</c> with its extra <c>additionalActiveTrackHeight</c> parameter.
    /// </summary>
    public void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        double additionalActiveTrackHeight,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        DebugAssertions.Assert(sliderTheme.DisabledActiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.DisabledInactiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.ActiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.InactiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.RangeThumbShape != null);

        if (sliderTheme.TrackHeight == null || sliderTheme.TrackHeight!.Value <= 0)
        {
            return;
        }

        // Assign the track segment paints, which are left: active, right: inactive,
        // but reversed for right to left text.
        var activeTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledActiveTrackColor,
            end: sliderTheme.ActiveTrackColor);
        var inactiveTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledInactiveTrackColor,
            end: sliderTheme.InactiveTrackColor);
        var activePaint = new SolidColorBrush(activeTrackColorTween.Evaluate(enableAnimation.Value));
        var inactivePaint = new SolidColorBrush(inactiveTrackColorTween.Evaluate(enableAnimation.Value));

        (Point leftThumbOffset, Point rightThumbOffset) = textDirection switch
        {
            TextDirection.Ltr => (startThumbCenter, endThumbCenter),
            _ => (endThumbCenter, startThumbCenter),
        };
        Size thumbSize = sliderTheme.RangeThumbShape!.GetPreferredSize(isEnabled, isDiscrete);
        double thumbRadius = thumbSize.Width / 2;
        DebugAssertions.Assert(thumbRadius > 0);

        Rect trackRect = GetPreferredRect(
            parentBox: parentBox,
            offset: offset,
            sliderTheme: sliderTheme,
            isEnabled: isEnabled,
            isDiscrete: isDiscrete);

        Radius trackRadius = Radius.Circular(trackRect.Height / 2);

        context.Canvas.DrawRRect(
            RRect.FromLTRBAndCorners(
                trackRect.Left,
                trackRect.Top,
                leftThumbOffset.X,
                trackRect.Bottom,
                topLeft: trackRadius,
                bottomLeft: trackRadius),
            inactivePaint,
            null);
        context.Canvas.DrawRRect(
            RRect.FromLTRBAndCorners(
                rightThumbOffset.X,
                trackRect.Top,
                trackRect.Right,
                trackRect.Bottom,
                topRight: trackRadius,
                bottomRight: trackRadius),
            inactivePaint,
            null);
        context.Canvas.DrawRRect(
            RRect.FromLTRBR(
                leftThumbOffset.X - (sliderTheme.TrackHeight!.Value / 2),
                trackRect.Top - (additionalActiveTrackHeight / 2),
                rightThumbOffset.X + (sliderTheme.TrackHeight!.Value / 2),
                trackRect.Bottom + (additionalActiveTrackHeight / 2),
                trackRadius),
            activePaint,
            null);
    }

    public override bool IsRounded => true;
}

/// <summary>
/// The default shape of each <see cref="RangeSlider"/> tick mark.
/// </summary>
/// <remarks>
/// Tick marks are only displayed if the slider is discrete. It paints a solid circle, centered on the
/// track, colored from <see cref="SliderThemeData.ActiveTickMarkColor"/>,
/// <see cref="SliderThemeData.InactiveTickMarkColor"/>,
/// <see cref="SliderThemeData.DisabledActiveTickMarkColor"/> and
/// <see cref="SliderThemeData.DisabledInactiveTickMarkColor"/>.
/// </remarks>
public class RoundRangeSliderTickMarkShape : RangeSliderTickMarkShape
{
    /// <summary>
    /// Create a range slider tick mark that draws a circle.
    /// </summary>
    public RoundRangeSliderTickMarkShape(double? tickMarkRadius = null)
    {
        TickMarkRadius = tickMarkRadius;
    }

    /// <summary>
    /// The preferred radius of the round tick mark.
    /// </summary>
    /// <remarks>
    /// If it is not provided, then 1/4 of the <see cref="SliderThemeData.TrackHeight"/> is used.
    /// </remarks>
    public double? TickMarkRadius { get; }

    public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled = false)
    {
        DebugAssertions.Assert(sliderTheme.TrackHeight != null);
        return DartGeometry.SizeFromRadius(TickMarkRadius ?? sliderTheme.TrackHeight!.Value / 4);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        bool isEnabled = false)
    {
        DebugAssertions.Assert(sliderTheme.DisabledActiveTickMarkColor != null);
        DebugAssertions.Assert(sliderTheme.DisabledInactiveTickMarkColor != null);
        DebugAssertions.Assert(sliderTheme.ActiveTickMarkColor != null);
        DebugAssertions.Assert(sliderTheme.InactiveTickMarkColor != null);

        bool hasGap = sliderTheme.TrackGap != null && sliderTheme.TrackGap!.Value > 0;
        bool underThumb = startThumbCenter.X == center.X || endThumbCenter.X == center.X;
        if (hasGap && underThumb)
        {
            return;
        }

        bool isBetweenThumbs = textDirection switch
        {
            TextDirection.Ltr => startThumbCenter.X < center.X && center.X < endThumbCenter.X,
            _ => endThumbCenter.X < center.X && center.X < startThumbCenter.X,
        };
        Color? begin = isBetweenThumbs
            ? sliderTheme.DisabledActiveTickMarkColor
            : sliderTheme.DisabledInactiveTickMarkColor;
        Color? end = isBetweenThumbs
            ? sliderTheme.ActiveTickMarkColor
            : sliderTheme.InactiveTickMarkColor;
        var paint = new SolidColorBrush(new ColorTween(begin: begin, end: end).Evaluate(enableAnimation.Value));

        // The tick marks are tiny circles that are the same height as the track.
        double tickMarkRadius =
            GetPreferredSize(isEnabled: isEnabled, sliderTheme: sliderTheme).Width / 2;
        if (tickMarkRadius > 0)
        {
            context.Canvas.DrawCircle(paint, null, center, tickMarkRadius);
        }
    }
}

/// <summary>
/// The default shape of a <see cref="RangeSlider"/>'s thumbs.
/// </summary>
/// <remarks>
/// There is a shadow for the resting and pressed state.
/// </remarks>
public class RoundRangeSliderThumbShape : RangeSliderThumbShape
{
    /// <summary>
    /// Create a slider thumb that draws a circle.
    /// </summary>
    public RoundRangeSliderThumbShape(
        double enabledThumbRadius = 10.0,
        double? disabledThumbRadius = null,
        double elevation = 1.0,
        double pressedElevation = 6.0)
    {
        EnabledThumbRadius = enabledThumbRadius;
        DisabledThumbRadius = disabledThumbRadius;
        Elevation = elevation;
        PressedElevation = pressedElevation;
    }

    /// <summary>
    /// The preferred radius of the round thumb shape when the slider is enabled.
    /// </summary>
    /// <remarks>If it is not provided, then the Material Design default of 10 is used.</remarks>
    public double EnabledThumbRadius { get; }

    /// <summary>
    /// The preferred radius of the round thumb shape when the slider is disabled.
    /// </summary>
    /// <remarks>If no disabledRadius is provided, then it is equal to the <see cref="EnabledThumbRadius"/>.</remarks>
    public double? DisabledThumbRadius { get; }

    private double _disabledThumbRadius => DisabledThumbRadius ?? EnabledThumbRadius;

    /// <summary>
    /// The resting elevation adds shadow to the unpressed thumb. The default is 1.
    /// </summary>
    public double Elevation { get; }

    /// <summary>
    /// The pressed elevation adds shadow to the pressed thumb. The default is 6.
    /// </summary>
    public double PressedElevation { get; }

    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        return DartGeometry.SizeFromRadius(isEnabled ? EnabledThumbRadius : _disabledThumbRadius);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        bool isDiscrete = false,
        bool isEnabled = false,
        bool? isOnTop = null,
        TextDirection? textDirection = null,
        Thumb? thumb = null,
        bool? isPressed = null)
    {
        DebugAssertions.Assert(sliderTheme.ShowValueIndicator != null);
        DebugAssertions.Assert(sliderTheme.OverlappingShapeStrokeColor != null);
        Canvas canvas = context.Canvas;
        var radiusTween = new DoubleTween(begin: _disabledThumbRadius, end: EnabledThumbRadius);
        var colorTween = new ColorTween(
            begin: sliderTheme.DisabledThumbColor,
            end: sliderTheme.ThumbColor);
        double radius = radiusTween.Evaluate(enableAnimation.Value);
        var elevationTween = new DoubleTween(begin: Elevation, end: PressedElevation);

        // Add a stroke of 1dp around the circle if this thumb would overlap
        // the other thumb.
        if (isOnTop ?? false)
        {
            var strokePaint = new Pen(new SolidColorBrush(sliderTheme.OverlappingShapeStrokeColor!), 1.0);
            canvas.DrawCircle(null, strokePaint, center, radius);
        }

        Color color = colorTween.Evaluate(enableAnimation.Value);

        double evaluatedElevation = isPressed!.Value
            ? elevationTween.Evaluate(activationAnimation.Value)
            : Elevation;
        var shadowPath = new Path();
        shadowPath.AddArc(
            DartGeometry.RectFromCenter(center: center, width: 2 * radius, height: 2 * radius),
            0,
            Math.PI * 2);

        bool paintShadows = true;
        if (Constants.KDebugMode)
        {
            if (RenderingDebug.DisableShadows)
            {
                RangeSliderPartsDebug.DebugDrawShadow(canvas, shadowPath, evaluatedElevation);
                paintShadows = false;
            }
        }

        if (paintShadows)
        {
            canvas.DrawShadow(shadowPath, Colors.Black, evaluatedElevation, true);
        }

        canvas.DrawCircle(new SolidColorBrush(color), null, center, radius);
    }
}

/// <summary>
/// Decides which thumbs (if any) should be selected.
/// </summary>
/// <remarks>
/// The default finds the closest thumb, but if the thumbs are close to each other, it waits for movement
/// defined by <c>dx</c> to determine the selected thumb. Override <see cref="SliderThemeData.ThumbSelector"/>
/// for custom thumb selection.
/// </remarks>
public delegate Thumb? RangeThumbSelector(
    TextDirection textDirection,
    RangeValues values,
    double tapValue,
    Size thumbSize,
    Size trackSize,
    double dx);

/// <summary>
/// Object for specifying a pair of values that represent the start and end of a range, used by
/// <see cref="RangeSlider"/>.
/// </summary>
public class RangeValues
{
    /// <summary>
    /// Creates pair of start and end values.
    /// </summary>
    public RangeValues(double start, double end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// The value of the start thumb.
    /// </summary>
    /// <remarks>
    /// For LTR text direction, the start is the left thumb, and for RTL text direction, the start is the
    /// right thumb.
    /// </remarks>
    public double Start { get; }

    /// <summary>
    /// The value of the end thumb.
    /// </summary>
    /// <remarks>
    /// For LTR text direction, the end is the right thumb, and for RTL text direction, the end is the left
    /// thumb.
    /// </remarks>
    public double End { get; }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return obj is RangeValues other && other.Start == Start && other.End == End;
    }

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(RangeValues? left, RangeValues? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(RangeValues? left, RangeValues? right) => !(left == right);

    public override string ToString()
    {
        return $"{Diagnostics.ObjectRuntimeType(this, "RangeValues")}("
            + $"{BindingBase.DartDoubleToString(Start)}, {BindingBase.DartDoubleToString(End)})";
    }
}

/// <summary>
/// Object for setting range slider label values that appear in the value indicator for each thumb.
/// </summary>
/// <remarks>
/// Used in combination with <see cref="SliderThemeData.ShowValueIndicator"/> to display labels above the
/// thumbs.
/// </remarks>
public class RangeLabels
{
    /// <summary>
    /// Creates pair of start and end labels.
    /// </summary>
    public RangeLabels(string start, string end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// The label of the start thumb.
    /// </summary>
    /// <remarks>
    /// For LTR text direction, the start is the left thumb, and for RTL text direction, the start is the
    /// right thumb.
    /// </remarks>
    public string Start { get; }

    /// <summary>
    /// The label of the end thumb.
    /// </summary>
    /// <remarks>
    /// For LTR text direction, the end is the right thumb, and for RTL text direction, the end is the left
    /// thumb.
    /// </remarks>
    public string End { get; }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return obj is RangeLabels other && other.Start == Start && other.End == End;
    }

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(RangeLabels? left, RangeLabels? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(RangeLabels? left, RangeLabels? right) => !(left == right);

    public override string ToString()
    {
        return $"{Diagnostics.ObjectRuntimeType(this, "RangeLabels")}({Start}, {End})";
    }
}

/// <summary>
/// Dart's library-private top-level <c>_debugDrawShadow</c> of range_slider_parts.dart.
/// </summary>
file static class RangeSliderPartsDebug
{
    public static void DebugDrawShadow(Canvas canvas, Path path, double elevation)
    {
        if (elevation > 0.0)
        {
            canvas.DrawPath(
                path,
                null,
                new Pen(new SolidColorBrush(Colors.Black), elevation * 2.0));
        }
    }
}

/// <summary>
/// The gapped shape of a <see cref="RangeSlider"/>'s track.
/// </summary>
/// <remarks>
/// It consists of active and inactive tracks: the active track uses
/// <see cref="SliderThemeData.ActiveTrackColor"/> and the inactive tracks use
/// <see cref="SliderThemeData.InactiveTrackColor"/>. The edge corners use a circular radius, the inside
/// corners a radius of 2 pixels. The gap between the thumb and the track is
/// <see cref="SliderThemeData.TrackGap"/>; stop indicators are painted at each end of a continuous slider.
/// </remarks>
public class GappedRangeSliderTrackShape : BaseRangeSliderTrackShape
{
    /// <summary>
    /// Create a range slider track that draws 3 rounded rectangles with rounded outer edges.
    /// </summary>
    public GappedRangeSliderTrackShape()
    {
    }

    /// <remarks>
    /// Dart's <c>paint</c> widens the base signature with <c>double additionalActiveTrackHeight = 2</c>; C#
    /// overrides cannot add parameters, so this override forwards to the overload that takes it.
    /// </remarks>
    public override void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        Paint(
            context,
            offset,
            parentBox,
            sliderTheme,
            enableAnimation,
            startThumbCenter,
            endThumbCenter,
            textDirection,
            additionalActiveTrackHeight: 2,
            isEnabled: isEnabled,
            isDiscrete: isDiscrete);
    }

    /// <summary>
    /// Dart's <c>paint</c> with its extra (unused) <c>additionalActiveTrackHeight</c> parameter.
    /// </summary>
    public void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point startThumbCenter,
        Point endThumbCenter,
        TextDirection textDirection,
        double additionalActiveTrackHeight,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        DebugAssertions.Assert(sliderTheme.DisabledActiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.DisabledInactiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.ActiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.InactiveTrackColor != null);
        DebugAssertions.Assert(sliderTheme.RangeThumbShape != null);

        if (sliderTheme.TrackHeight == null || sliderTheme.TrackHeight!.Value <= 0)
        {
            return;
        }

        var activeTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledActiveTrackColor,
            end: sliderTheme.ActiveTrackColor);
        var inactiveTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledInactiveTrackColor,
            end: sliderTheme.InactiveTrackColor);

        var activePaint = new SolidColorBrush(activeTrackColorTween.Evaluate(enableAnimation.Value));
        var inactivePaint = new SolidColorBrush(inactiveTrackColorTween.Evaluate(enableAnimation.Value));

        Rect trackRect = GetPreferredRect(
            parentBox: parentBox,
            offset: offset,
            sliderTheme: sliderTheme,
            isEnabled: isEnabled,
            isDiscrete: isDiscrete);

        Radius trackCornerRadius = Radius.Circular(DartGeometry.ShortestSide(trackRect.Size) / 2);
        Radius trackInsideCornerRadius = Radius.Circular(2.0);

        (Point leftThumbOffset, Point rightThumbOffset) = textDirection switch
        {
            TextDirection.Ltr => (startThumbCenter, endThumbCenter),
            _ => (endThumbCenter, startThumbCenter),
        };

        Size thumbSize = sliderTheme.RangeThumbShape!.GetPreferredSize(isEnabled, isDiscrete);
        double thumbRadius = thumbSize.Width / 2;
        DebugAssertions.Assert(thumbRadius > 0);
        double trackGap = sliderTheme.TrackGap!.Value;

        var trackRRect = new RRect(
            trackRect,
            topLeft: trackCornerRadius,
            topRight: trackCornerRadius,
            bottomRight: trackCornerRadius,
            bottomLeft: trackCornerRadius);

        RRect leftRRect = RRect.FromLTRBAndCorners(
            trackRect.Left,
            trackRect.Top,
            leftThumbOffset.X - trackGap,
            trackRect.Bottom,
            topLeft: trackCornerRadius,
            bottomLeft: trackCornerRadius,
            topRight: trackInsideCornerRadius,
            bottomRight: trackInsideCornerRadius);

        RRect rightRRect = RRect.FromLTRBAndCorners(
            rightThumbOffset.X + trackGap,
            trackRect.Top,
            trackRect.Right,
            trackRect.Bottom,
            topLeft: trackInsideCornerRadius,
            bottomLeft: trackInsideCornerRadius,
            topRight: trackCornerRadius,
            bottomRight: trackCornerRadius);

        context.Canvas.Save();
        context.Canvas.ClipRRect(trackRRect);
        bool drawLeftTrack =
            startThumbCenter.X > (leftRRect.Left + (sliderTheme.TrackHeight!.Value / 2));
        bool drawRightTrack =
            endThumbCenter.X < (rightRRect.Right - (sliderTheme.TrackHeight!.Value / 2));

        if (drawLeftTrack)
        {
            context.Canvas.DrawRRect(leftRRect, inactivePaint, null);
        }

        if (drawRightTrack)
        {
            context.Canvas.DrawRRect(rightRRect, inactivePaint, null);
        }

        if (leftThumbOffset.X + trackGap < rightThumbOffset.X - trackGap)
        {
            context.Canvas.DrawRRect(
                RRect.FromLTRBR(
                    leftThumbOffset.X + trackGap,
                    trackRect.Top,
                    rightThumbOffset.X - trackGap,
                    trackRect.Bottom,
                    trackInsideCornerRadius),
                activePaint,
                null);
        }

        context.Canvas.Restore();

        const double stopIndicatorRadius = 2.0;
        double stopIndicatorTrailingSpace = sliderTheme.TrackHeight!.Value / 2;
        // trackRect.centerLeft.dx / trackRect.centerRight.dx / trackRect.center.dy.
        var startStopIndicatorOffset = new Point(
            trackRect.Left + stopIndicatorTrailingSpace,
            trackRect.Center.Y);
        var endStopIndicatorOffset = new Point(
            trackRect.Right - stopIndicatorTrailingSpace,
            trackRect.Center.Y);

        bool showStartStopIndicator = startThumbCenter.X > startStopIndicatorOffset.X;
        if (showStartStopIndicator && !isDiscrete)
        {
            Rect stopIndicatorRect = DartGeometry.RectFromCircle(
                center: startStopIndicatorOffset,
                radius: stopIndicatorRadius);
            context.Canvas.DrawCircle(activePaint, null, stopIndicatorRect.Center, stopIndicatorRadius);
        }

        bool showEndStopIndicator = endThumbCenter.X < endStopIndicatorOffset.X;
        if (showEndStopIndicator && !isDiscrete)
        {
            Rect stopIndicatorRect = DartGeometry.RectFromCircle(
                center: endStopIndicatorOffset,
                radius: stopIndicatorRadius);
            context.Canvas.DrawCircle(activePaint, null, stopIndicatorRect.Center, stopIndicatorRadius);
        }
    }

    public override bool IsRounded => true;
}

/// <summary>
/// The bar shape of <see cref="RangeSlider"/>'s thumbs.
/// </summary>
/// <remarks>
/// When the range slider is enabled, <see cref="ColorScheme.Primary"/> is used for the thumb; when it is
/// disabled, <see cref="ColorScheme.OnSurface"/> with an opacity of 0.38. The thumb size is resolved from
/// <see cref="SliderThemeData.ThumbSize"/>.
/// </remarks>
public class HandleRangeSliderThumbShape : RangeSliderThumbShape
{
    /// <summary>
    /// Create a range slider thumb that draws a bar.
    /// </summary>
    public HandleRangeSliderThumbShape()
    {
    }

    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        return new Size(4.0, 44.0);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        SliderThemeData sliderTheme,
        bool isDiscrete = false,
        bool isEnabled = false,
        bool? isOnTop = null,
        TextDirection? textDirection = null,
        Thumb? thumb = null,
        bool? isPressed = null)
    {
        DebugAssertions.Assert(sliderTheme.ShowValueIndicator != null);
        DebugAssertions.Assert(sliderTheme.OverlappingShapeStrokeColor != null);
        DebugAssertions.Assert(sliderTheme.DisabledThumbColor != null);
        DebugAssertions.Assert(sliderTheme.ThumbColor != null);
        DebugAssertions.Assert(sliderTheme.ThumbSize != null);

        var colorTween = new ColorTween(
            begin: sliderTheme.DisabledThumbColor,
            end: sliderTheme.ThumbColor);
        Color color = colorTween.Evaluate(enableAnimation.Value);
        Canvas canvas = context.Canvas;

        Size thumbSize = sliderTheme.ThumbSize!.Resolve(
            new HashSet<WidgetState>())!.Value; // This is resolved in the paint method.
        RRect rrect = RRect.FromRectAndRadius(
            DartGeometry.RectFromCenter(center: center, width: thumbSize.Width, height: thumbSize.Height),
            Radius.Circular(DartGeometry.ShortestSide(thumbSize) / 2));

        canvas.DrawRRect(rrect, new SolidColorBrush(color), null);
    }
}

/// <summary>
/// The rounded rectangle shape of a <see cref="RangeSlider"/>'s value indicators.
/// </summary>
/// <remarks>
/// If <see cref="SliderThemeData.ValueIndicatorColor"/> is null, the shape uses
/// <see cref="ColorScheme.InverseSurface"/>; the label defaults to <see cref="TextTheme.LabelMedium"/> in
/// <see cref="ColorScheme.OnInverseSurface"/>. The value indicator is only painted when the label is
/// non-null.
/// </remarks>
public class RoundedRectRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    /// <summary>
    /// Create range slider value indicators that resembles a rounded rectangle.
    /// </summary>
    public RoundedRectRangeSliderValueIndicatorShape()
    {
    }

    // Dart declares a byte-identical `_RoundedRectSliderValueIndicatorPathPainter` in both slider_parts.dart
    // and range_slider_parts.dart; the single C# copy lives in SliderParts.cs and is reused here.
    private static readonly RoundedRectSliderValueIndicatorPathPainter _pathPainter = new();

    public override Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter labelPainter,
        double textScaleFactor)
    {
        DebugAssertions.Assert(labelPainter != null);
        DebugAssertions.Assert(textScaleFactor >= 0);
        return _pathPainter.GetPreferredSize(labelPainter!, textScaleFactor);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        bool? isDiscrete = null,
        bool? isOnTop = null,
        double? textScaleFactor = null,
        Size? sizeWithOverflow = null,
        TextDirection? textDirection = null,
        double? value = null,
        Thumb? thumb = null)
    {
        DebugAssertions.Assert(textScaleFactor != null);
        DebugAssertions.Assert(sizeWithOverflow != null);
        DebugAssertions.Assert(sliderTheme.ValueIndicatorColor != null);

        Canvas canvas = context.Canvas;
        double scale = activationAnimation.Value;
        _pathPainter.Paint(
            parentBox: parentBox,
            canvas: canvas,
            center: center,
            scale: scale,
            labelPainter: labelPainter,
            textScaleFactor: textScaleFactor!.Value,
            sizeWithOverflow: sizeWithOverflow!.Value,
            backgroundPaintColor: sliderTheme.ValueIndicatorColor!,
            strokePaintColor: isOnTop!.Value
                ? sliderTheme.OverlappingShapeStrokeColor
                : sliderTheme.ValueIndicatorStrokeColor);
    }
}

/// <summary>
/// The shape of a Material 3 <see cref="RangeSlider"/>'s value indicators.
/// </summary>
/// <remarks>
/// If <see cref="SliderThemeData.ValueIndicatorColor"/> is null, the shape uses
/// <see cref="ColorScheme.Primary"/>; the label defaults to <see cref="TextTheme.LabelMedium"/> in
/// <see cref="ColorScheme.OnPrimary"/>. The value indicator is only painted when the label is non-null.
/// </remarks>
public class DropRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    /// <summary>
    /// Create a range slider value indicator that resembles a drop shape.
    /// </summary>
    public DropRangeSliderValueIndicatorShape()
    {
    }

    // Dart declares a byte-identical `_DropSliderValueIndicatorPathPainter` in both slider_parts.dart and
    // range_slider_parts.dart; the single C# copy lives in SliderParts.cs and is reused here.
    private static readonly DropSliderValueIndicatorPathPainter _pathPainter = new();

    public override Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter labelPainter,
        double textScaleFactor)
    {
        DebugAssertions.Assert(labelPainter != null);
        DebugAssertions.Assert(textScaleFactor >= 0);
        return _pathPainter.GetPreferredSize(labelPainter!, textScaleFactor);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        bool? isDiscrete = null,
        bool? isOnTop = null,
        double? textScaleFactor = null,
        Size? sizeWithOverflow = null,
        TextDirection? textDirection = null,
        double? value = null,
        Thumb? thumb = null)
    {
        Canvas canvas = context.Canvas;
        double scale = activationAnimation.Value;
        _pathPainter.Paint(
            parentBox: parentBox,
            canvas: canvas,
            center: center,
            scale: scale,
            labelPainter: labelPainter,
            textScaleFactor: textScaleFactor!.Value,
            sizeWithOverflow: sizeWithOverflow!.Value,
            backgroundPaintColor: sliderTheme.ValueIndicatorColor!,
            strokePaintColor: isOnTop!.Value
                ? sliderTheme.OverlappingShapeStrokeColor
                : sliderTheme.ValueIndicatorStrokeColor);
    }
}
