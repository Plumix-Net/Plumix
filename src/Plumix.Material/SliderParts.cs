using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;
using Radius = Plumix.Rendering.Radius;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/slider_parts.dart

/// <summary>Base class for <see cref="Slider"/> tick mark shapes.</summary>
/// <remarks>
/// The tick mark painting can be skipped by specifying <see cref="NoTickMark"/> for
/// <see cref="SliderThemeData.TickMarkShape"/>.
/// </remarks>
public abstract class SliderTickMarkShape
{
    /// <summary>Returns the preferred size of the shape, used to position the tick marks within the
    /// slider.</summary>
    public abstract Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled);

    /// <summary>Paints the tick mark. The track segment between the start of the slider and the thumb is
    /// the active segment; the start is on the left in LTR and on the right in RTL.</summary>
    public abstract void Paint(
        PaintingContext context,
        Point center,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        bool isEnabled,
        TextDirection textDirection);

    /// <summary>Special instance of <see cref="SliderTickMarkShape"/> to skip the tick mark painting.</summary>
    public static SliderTickMarkShape NoTickMark { get; } = new EmptySliderTickMarkShape();

    /// <summary>Dart's <c>Object.toString</c>, which the slider theme's diagnostics print for shapes.</summary>
    public override string ToString() => $"Instance of '{Diagnostics.DescribeType(GetType())}'";
}

/// <summary>Base class for slider track shapes.</summary>
/// <remarks>
/// The slider's thumb moves along the track. A discrete slider's tick marks are drawn after the
/// track, but before the thumb, and are aligned with the track. <see cref="GetPreferredRect"/> helps
/// position the slider thumb and tick marks relative to the track.
/// </remarks>
public abstract class SliderTrackShape
{
    /// <summary>Returns the preferred bounds of the shape: the horizontal boundaries for the thumb's
    /// position. <paramref name="offset"/> is relative to the caller's bounding box.</summary>
    public abstract Rect GetPreferredRect(
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Point offset = default,
        bool isEnabled = false,
        bool isDiscrete = false);

    /// <summary>Paints the track shape. <paramref name="offset"/> is the offset of the origin of
    /// <paramref name="parentBox"/> to the origin of the context canvas; <paramref name="thumbCenter"/>
    /// divides the track into 2 segments, and a non-null <paramref name="secondaryOffset"/> into 3.</summary>
    public abstract void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        TextDirection textDirection,
        Point? secondaryOffset = null,
        bool isEnabled = false,
        bool isDiscrete = false);

    /// <summary>Whether the track shape is rounded. This is used to determine the correct position of the
    /// thumb in relation to the track.</summary>
    public virtual bool IsRounded => false;

    /// <summary>Dart's <c>Object.toString</c>, which the slider theme's diagnostics print for shapes.</summary>
    public override string ToString() => $"Instance of '{Diagnostics.DescribeType(GetType())}'";
}

/// <summary>Base track shape that provides an implementation of <see cref="GetPreferredRect"/> for default
/// sizing.</summary>
/// <remarks>
/// Dart's <c>mixin BaseSliderTrackShape</c>; C# has no mixins, so the Dart classes declared
/// <c>extends SliderTrackShape with BaseSliderTrackShape</c> derive from this class. The height is set
/// from <see cref="SliderThemeData.TrackHeight"/> and the width of the parent box less the larger of the
/// widths of <see cref="SliderThemeData.ThumbShape"/> and <see cref="SliderThemeData.OverlayShape"/>.
/// </remarks>
public abstract class BaseSliderTrackShape : SliderTrackShape
{
    /// <summary>Returns a rect that represents the track bounds that fits within the slider.</summary>
    /// <remarks>
    /// The width is the width of the slider, but padded by the max of the overlay and thumb radius. The
    /// height is defined by <see cref="SliderThemeData.TrackHeight"/>. The rect is centered both
    /// horizontally and vertically within the slider bounds.
    /// </remarks>
    public override Rect GetPreferredRect(
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Point offset = default,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        double thumbWidth = sliderTheme.ThumbShape!.GetPreferredSize(isEnabled, isDiscrete).Width;
        double overlayWidth = sliderTheme.OverlayShape!
            .GetPreferredSize(isEnabled, isDiscrete)
            .Width;
        double trackHeight = sliderTheme.TrackHeight!.Value;
        DebugAssertions.Assert(overlayWidth >= 0, "overlayWidth >= 0");
        DebugAssertions.Assert(trackHeight >= 0, "trackHeight >= 0");

        // If the track colors are transparent, then override only the track height
        // to maintain overall Slider width.
        if (sliderTheme.ActiveTrackColor == Colors.Transparent
            && sliderTheme.InactiveTrackColor == Colors.Transparent)
        {
            trackHeight = 0;
        }

        double trackLeft =
            offset.X + (sliderTheme.Padding == null ? Math.Max(overlayWidth / 2, thumbWidth / 2) : 0);
        double trackTop = offset.Y + ((parentBox.Size.Height - trackHeight) / 2);
        double trackRight =
            trackLeft
            + parentBox.Size.Width
            - (sliderTheme.Padding == null ? Math.Max(thumbWidth, overlayWidth) : 0);
        double trackBottom = trackTop + trackHeight;
        // If the parentBox's size less than slider's size the trackRight will be less than trackLeft, so
        // switch them.
        return DartGeometry.RectFromLTRB(
            Math.Min(trackLeft, trackRight),
            trackTop,
            Math.Max(trackLeft, trackRight),
            trackBottom);
    }

    /// <summary>Whether the track shape is rounded. Defaults to false.</summary>
    public override bool IsRounded => false;
}

/// <summary>A <see cref="Slider"/> track that's a simple rectangle.</summary>
/// <remarks>
/// It paints a solid colored rectangle, vertically centered in the <c>parentBox</c>, padded by the
/// overlay radius. The color is determined by the slider's enabled state and the track segment's active
/// state: <see cref="SliderThemeData.ActiveTrackColor"/>, <see cref="SliderThemeData.InactiveTrackColor"/>,
/// <see cref="SliderThemeData.DisabledActiveTrackColor"/>,
/// <see cref="SliderThemeData.DisabledInactiveTrackColor"/>.
/// </remarks>
public class RectangularSliderTrackShape : BaseSliderTrackShape
{
    /// <summary>Creates a slider track that draws 2 rectangles.</summary>
    public RectangularSliderTrackShape()
    {
    }

    public override void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        TextDirection textDirection,
        Point? secondaryOffset = null,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        DebugAssertions.Assert(
            sliderTheme.DisabledActiveTrackColor != null,
            "sliderTheme.disabledActiveTrackColor != null");
        DebugAssertions.Assert(
            sliderTheme.DisabledInactiveTrackColor != null,
            "sliderTheme.disabledInactiveTrackColor != null");
        DebugAssertions.Assert(sliderTheme.ActiveTrackColor != null, "sliderTheme.activeTrackColor != null");
        DebugAssertions.Assert(sliderTheme.InactiveTrackColor != null, "sliderTheme.inactiveTrackColor != null");
        DebugAssertions.Assert(sliderTheme.ThumbShape != null, "sliderTheme.thumbShape != null");
        // If the slider [SliderThemeData.trackHeight] is less than or equal to 0,
        // then it makes no difference whether the track is painted or not,
        // therefore the painting can be a no-op.
        if (sliderTheme.TrackHeight!.Value <= 0)
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
        var activePaint = new SolidColorBrush(activeTrackColorTween.Transform(enableAnimation.Value));
        var inactivePaint = new SolidColorBrush(inactiveTrackColorTween.Transform(enableAnimation.Value));
        (IBrush leftTrackPaint, IBrush rightTrackPaint) = textDirection switch
        {
            TextDirection.Ltr => (activePaint, inactivePaint),
            _ => (inactivePaint, activePaint),
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
            thumbCenter.X,
            trackRect.Bottom);
        if (!DartGeometry.RectIsEmpty(leftTrackSegment))
        {
            context.Canvas.DrawRectangle(leftTrackPaint, null, leftTrackSegment);
        }

        Rect rightTrackSegment = DartGeometry.RectFromLTRB(
            thumbCenter.X,
            trackRect.Top,
            trackRect.Right,
            trackRect.Bottom);
        if (!DartGeometry.RectIsEmpty(rightTrackSegment))
        {
            context.Canvas.DrawRectangle(rightTrackPaint, null, rightTrackSegment);
        }

        bool showSecondaryTrack =
            secondaryOffset != null
            && textDirection switch
            {
                TextDirection.Rtl => secondaryOffset.Value.X < thumbCenter.X,
                _ => secondaryOffset.Value.X > thumbCenter.X,
            };

        if (showSecondaryTrack)
        {
            var secondaryTrackColorTween = new ColorTween(
                begin: sliderTheme.DisabledSecondaryActiveTrackColor,
                end: sliderTheme.SecondaryActiveTrackColor);
            var secondaryTrackPaint =
                new SolidColorBrush(secondaryTrackColorTween.Transform(enableAnimation.Value));
            Rect secondaryTrackSegment = textDirection switch
            {
                TextDirection.Rtl => DartGeometry.RectFromLTRB(
                    secondaryOffset!.Value.X,
                    trackRect.Top,
                    thumbCenter.X,
                    trackRect.Bottom),
                _ => DartGeometry.RectFromLTRB(
                    thumbCenter.X,
                    trackRect.Top,
                    secondaryOffset!.Value.X,
                    trackRect.Bottom),
            };
            if (!DartGeometry.RectIsEmpty(secondaryTrackSegment))
            {
                context.Canvas.DrawRectangle(secondaryTrackPaint, null, secondaryTrackSegment);
            }
        }
    }
}

/// <summary>The default shape of a <see cref="Slider"/>'s track (Material 2).</summary>
/// <remarks>
/// It paints a solid colored rectangle with rounded edges, vertically centered in the
/// <c>parentBox</c>, padded by the larger of the overlay radius and the thumb radius. The height is
/// defined by <see cref="SliderThemeData.TrackHeight"/>; the active segment is
/// <c>additionalActiveTrackHeight</c> taller.
/// </remarks>
public class RoundedRectSliderTrackShape : BaseSliderTrackShape
{
    /// <summary>Create a slider track that draws two rectangles with rounded outer edges.</summary>
    public RoundedRectSliderTrackShape()
    {
    }

    /// <summary>Paints the track with Dart's default <c>additionalActiveTrackHeight</c> of 2.</summary>
    public override void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        TextDirection textDirection,
        Point? secondaryOffset = null,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        Paint(
            context,
            offset,
            parentBox,
            sliderTheme,
            enableAnimation,
            thumbCenter,
            textDirection,
            secondaryOffset,
            isEnabled,
            isDiscrete,
            additionalActiveTrackHeight: 2);
    }

    /// <summary>Dart's <c>paint</c> override, which widens the base signature with the optional
    /// <paramref name="additionalActiveTrackHeight"/>; C# overrides cannot add parameters.</summary>
    public void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        TextDirection textDirection,
        Point? secondaryOffset,
        bool isEnabled,
        bool isDiscrete,
        double additionalActiveTrackHeight)
    {
        DebugAssertions.Assert(
            sliderTheme.DisabledActiveTrackColor != null,
            "sliderTheme.disabledActiveTrackColor != null");
        DebugAssertions.Assert(
            sliderTheme.DisabledInactiveTrackColor != null,
            "sliderTheme.disabledInactiveTrackColor != null");
        DebugAssertions.Assert(sliderTheme.ActiveTrackColor != null, "sliderTheme.activeTrackColor != null");
        DebugAssertions.Assert(sliderTheme.InactiveTrackColor != null, "sliderTheme.inactiveTrackColor != null");
        DebugAssertions.Assert(sliderTheme.ThumbShape != null, "sliderTheme.thumbShape != null");
        // If the slider [SliderThemeData.trackHeight] is less than or equal to 0,
        // then it makes no difference whether the track is painted or not,
        // therefore the painting can be a no-op.
        if (sliderTheme.TrackHeight == null || sliderTheme.TrackHeight.Value <= 0)
        {
            return;
        }

        // Assign the track segment paints, which are leading: active and
        // trailing: inactive.
        var activeTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledActiveTrackColor,
            end: sliderTheme.ActiveTrackColor);
        var inactiveTrackColorTween = new ColorTween(
            begin: sliderTheme.DisabledInactiveTrackColor,
            end: sliderTheme.InactiveTrackColor);
        var activePaint = new SolidColorBrush(activeTrackColorTween.Transform(enableAnimation.Value));
        var inactivePaint = new SolidColorBrush(inactiveTrackColorTween.Transform(enableAnimation.Value));
        (IBrush leftTrackPaint, IBrush rightTrackPaint) = textDirection switch
        {
            TextDirection.Ltr => (activePaint, inactivePaint),
            _ => (inactivePaint, activePaint),
        };

        Rect trackRect = GetPreferredRect(
            parentBox: parentBox,
            offset: offset,
            sliderTheme: sliderTheme,
            isEnabled: isEnabled,
            isDiscrete: isDiscrete);
        Radius trackRadius = Radius.Circular(trackRect.Height / 2);
        Radius activeTrackRadius = Radius.Circular((trackRect.Height + additionalActiveTrackHeight) / 2);
        bool isLTR = textDirection == TextDirection.Ltr;
        bool isRTL = textDirection == TextDirection.Rtl;
        double trackHeight = sliderTheme.TrackHeight.Value;

        bool drawInactiveTrack = thumbCenter.X < (trackRect.Right - (trackHeight / 2));
        if (drawInactiveTrack)
        {
            // Draw the inactive track segment.
            context.Canvas.DrawRRect(
                RRect.FromLTRBR(
                    thumbCenter.X - (trackHeight / 2),
                    isRTL ? trackRect.Top - (additionalActiveTrackHeight / 2) : trackRect.Top,
                    trackRect.Right,
                    isRTL ? trackRect.Bottom + (additionalActiveTrackHeight / 2) : trackRect.Bottom,
                    isLTR ? trackRadius : activeTrackRadius),
                rightTrackPaint,
                null);
        }

        bool drawActiveTrack = thumbCenter.X > (trackRect.Left + (trackHeight / 2));
        if (drawActiveTrack)
        {
            // Draw the active track segment.
            context.Canvas.DrawRRect(
                RRect.FromLTRBR(
                    trackRect.Left,
                    isLTR ? trackRect.Top - (additionalActiveTrackHeight / 2) : trackRect.Top,
                    thumbCenter.X + (trackHeight / 2),
                    isLTR ? trackRect.Bottom + (additionalActiveTrackHeight / 2) : trackRect.Bottom,
                    isLTR ? activeTrackRadius : trackRadius),
                leftTrackPaint,
                null);
        }

        bool showSecondaryTrack =
            secondaryOffset != null
            && (isLTR ? (secondaryOffset.Value.X > thumbCenter.X) : (secondaryOffset.Value.X < thumbCenter.X));

        if (showSecondaryTrack)
        {
            var secondaryTrackColorTween = new ColorTween(
                begin: sliderTheme.DisabledSecondaryActiveTrackColor,
                end: sliderTheme.SecondaryActiveTrackColor);
            var secondaryTrackPaint =
                new SolidColorBrush(secondaryTrackColorTween.Transform(enableAnimation.Value));
            if (isLTR)
            {
                context.Canvas.DrawRRect(
                    RRect.FromLTRBAndCorners(
                        thumbCenter.X,
                        trackRect.Top,
                        secondaryOffset!.Value.X,
                        trackRect.Bottom,
                        topRight: trackRadius,
                        bottomRight: trackRadius),
                    secondaryTrackPaint,
                    null);
            }
            else
            {
                context.Canvas.DrawRRect(
                    RRect.FromLTRBAndCorners(
                        secondaryOffset!.Value.X,
                        trackRect.Top,
                        thumbCenter.X,
                        trackRect.Bottom,
                        topLeft: trackRadius,
                        bottomLeft: trackRadius),
                    secondaryTrackPaint,
                    null);
            }
        }
    }

    public override bool IsRounded => true;
}

/// <summary>The default shape of each <see cref="Slider"/> tick mark.</summary>
/// <remarks>
/// Tick marks are only displayed if the slider is discrete. It paints a solid circle, centered on the
/// track, colored by the slider's enabled state and the track's active state.
/// </remarks>
public class RoundSliderTickMarkShape : SliderTickMarkShape
{
    /// <summary>Create a slider tick mark that draws a circle.</summary>
    public RoundSliderTickMarkShape(double? tickMarkRadius = null)
    {
        TickMarkRadius = tickMarkRadius;
    }

    /// <summary>The preferred radius of the round tick mark. If it is not provided, then 1/4 of the
    /// <see cref="SliderThemeData.TrackHeight"/> is used.</summary>
    public double? TickMarkRadius { get; }

    public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled)
    {
        DebugAssertions.Assert(sliderTheme.TrackHeight != null, "sliderTheme.trackHeight != null");
        // The tick marks are tiny circles. If no radius is provided, then the
        // radius is defaulted to be a fraction of the
        // [SliderThemeData.trackHeight]. The fraction is 1/4.
        return DartGeometry.SizeFromRadius(TickMarkRadius ?? sliderTheme.TrackHeight!.Value / 4);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        bool isEnabled,
        TextDirection textDirection)
    {
        DebugAssertions.Assert(
            sliderTheme.DisabledActiveTickMarkColor != null,
            "sliderTheme.disabledActiveTickMarkColor != null");
        DebugAssertions.Assert(
            sliderTheme.DisabledInactiveTickMarkColor != null,
            "sliderTheme.disabledInactiveTickMarkColor != null");
        DebugAssertions.Assert(
            sliderTheme.ActiveTickMarkColor != null,
            "sliderTheme.activeTickMarkColor != null");
        DebugAssertions.Assert(
            sliderTheme.InactiveTickMarkColor != null,
            "sliderTheme.inactiveTickMarkColor != null");
        // The paint color of the tick mark depends on its position relative
        // to the thumb and the text direction.
        double xOffset = center.X - thumbCenter.X;
        (Color? begin, Color? end) = textDirection switch
        {
            TextDirection.Ltr when xOffset > 0 => (
                sliderTheme.DisabledInactiveTickMarkColor,
                sliderTheme.InactiveTickMarkColor),
            TextDirection.Rtl when xOffset < 0 => (
                sliderTheme.DisabledInactiveTickMarkColor,
                sliderTheme.InactiveTickMarkColor),
            _ => (
                sliderTheme.DisabledActiveTickMarkColor,
                sliderTheme.ActiveTickMarkColor),
        };
        var paint = new SolidColorBrush(new ColorTween(begin: begin, end: end).Transform(enableAnimation.Value));

        // The tick marks are tiny circles that are the same height as the track.
        double tickMarkRadius =
            GetPreferredSize(isEnabled: isEnabled, sliderTheme: sliderTheme).Width / 2;
        if (tickMarkRadius > 0)
        {
            context.Canvas.DrawCircle(paint, null, center, tickMarkRadius);
        }
    }
}

/// <summary>A special version of <see cref="SliderTickMarkShape"/> that has a zero size and paints
/// nothing.</summary>
/// <remarks>Dart's <c>_EmptySliderTickMarkShape</c>, stored in
/// <see cref="SliderTickMarkShape.NoTickMark"/>.</remarks>
internal sealed class EmptySliderTickMarkShape : SliderTickMarkShape
{
    public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled)
    {
        return default;
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        bool isEnabled,
        TextDirection textDirection)
    {
        // no-op.
    }
}

/// <summary>The default shape of a Material 2 <see cref="Slider"/>'s thumb.</summary>
/// <remarks>There is a shadow for the resting, pressed, hovered, and focused state.</remarks>
public class RoundSliderThumbShape : SliderComponentShape
{
    /// <summary>Create a slider thumb that draws a circle.</summary>
    public RoundSliderThumbShape(
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

    /// <summary>The preferred radius of the round thumb shape when the slider is enabled. Defaults to the
    /// Material Design default of 10.</summary>
    public double EnabledThumbRadius { get; }

    /// <summary>The preferred radius of the round thumb shape when the slider is disabled. If null, it is
    /// equal to <see cref="EnabledThumbRadius"/>.</summary>
    public double? DisabledThumbRadius { get; }

    // Dart's `_disabledThumbRadius`.
    private double EffectiveDisabledThumbRadius => DisabledThumbRadius ?? EnabledThumbRadius;

    /// <summary>The resting elevation adds shadow to the unpressed thumb. The default is 1; use 0 for no
    /// shadow.</summary>
    public double Elevation { get; }

    /// <summary>The pressed elevation adds shadow to the pressed thumb. The default is 6; use 0 for no
    /// shadow.</summary>
    public double PressedElevation { get; }

    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        return DartGeometry.SizeFromRadius(isEnabled ? EnabledThumbRadius : EffectiveDisabledThumbRadius);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        DebugAssertions.Assert(sliderTheme.DisabledThumbColor != null, "sliderTheme.disabledThumbColor != null");
        DebugAssertions.Assert(sliderTheme.ThumbColor != null, "sliderTheme.thumbColor != null");

        Canvas canvas = context.Canvas;
        var radiusTween = new DoubleTween(begin: EffectiveDisabledThumbRadius, end: EnabledThumbRadius);
        var colorTween = new ColorTween(
            begin: sliderTheme.DisabledThumbColor,
            end: sliderTheme.ThumbColor);

        Color color = colorTween.Transform(enableAnimation.Value);
        double radius = radiusTween.Transform(enableAnimation.Value);

        var elevationTween = new DoubleTween(begin: Elevation, end: PressedElevation);

        double evaluatedElevation = elevationTween.Transform(activationAnimation.Value);
        var path = new Path();
        path.AddArc(
            DartGeometry.RectFromCenter(center, 2 * radius, 2 * radius),
            0,
            Math.PI * 2);

        bool paintShadows = true;
        if (Constants.KDebugMode)
        {
            if (RenderingDebug.DisableShadows)
            {
                SliderPartsDebug.DebugDrawShadow(canvas, path, evaluatedElevation);
                paintShadows = false;
            }
        }

        if (paintShadows)
        {
            canvas.DrawShadow(path, Colors.Black, evaluatedElevation, true);
        }

        canvas.DrawCircle(new SolidColorBrush(color), null, center, radius);
    }
}

/// <summary>The default shape of a Material 3 <see cref="Slider"/>'s value indicator.</summary>
public class DropSliderValueIndicatorShape : SliderComponentShape
{
    /// <summary>Create a slider value indicator that resembles a drop shape.</summary>
    public DropSliderValueIndicatorShape()
    {
    }

    private static readonly DropSliderValueIndicatorPathPainter _pathPainter = new();

    /// <summary>Forwards to the widened overload with no label painter, which asserts like Dart does when
    /// the named arguments are omitted.</summary>
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        return GetPreferredSize(isEnabled, isDiscrete, labelPainter: null, textScaleFactor: null);
    }

    /// <summary>Dart's <c>getPreferredSize</c> override with the optional named <c>labelPainter</c> and
    /// <c>textScaleFactor</c>; C# overrides cannot add parameters.</summary>
    public Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter? labelPainter,
        double? textScaleFactor)
    {
        DebugAssertions.Assert(labelPainter != null, "labelPainter != null");
        DebugAssertions.Assert(
            textScaleFactor != null && textScaleFactor >= 0,
            "textScaleFactor != null && textScaleFactor >= 0");
        return _pathPainter.GetPreferredSize(labelPainter!, textScaleFactor!.Value);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        Canvas canvas = context.Canvas;
        double scale = activationAnimation.Value;
        _pathPainter.Paint(
            parentBox: parentBox,
            canvas: canvas,
            center: center,
            scale: scale,
            labelPainter: labelPainter,
            textScaleFactor: textScaleFactor,
            sizeWithOverflow: sizeWithOverflow,
            backgroundPaintColor: sliderTheme.ValueIndicatorColor!,
            strokePaintColor: sliderTheme.ValueIndicatorStrokeColor);
    }
}

/// <summary>Dart's <c>_DropSliderValueIndicatorPathPainter</c>.</summary>
/// <remarks>range_slider_parts.dart declares a byte-identical private copy; both the slider and the
/// range slider value indicator shapes use this single C# class.</remarks>
internal sealed class DropSliderValueIndicatorPathPainter
{
    private const double TriangleHeight = 10.0;
    private const double LabelPadding = 8.0;
    private const double PreferredHeight = 32.0;
    private const double MinLabelWidth = 20.0;
    private const double MinRectHeight = 28.0;
    private const double RectYOffset = 6.0;
    private const double BottomTipYOffset = 16.0;
    private const double PreferredHalfHeight = PreferredHeight / 2;
    private const double UpperRectRadius = 4;

    public Size GetPreferredSize(TextPainter labelPainter, double textScaleFactor)
    {
        double width =
            Math.Max(MinLabelWidth, labelPainter.Width) + (LabelPadding * 2 * textScaleFactor);
        return new Size(width, PreferredHeight * textScaleFactor);
    }

    public double GetHorizontalShift(
        RenderBox parentBox,
        Point center,
        TextPainter labelPainter,
        double textScaleFactor,
        Size sizeWithOverflow,
        double scale)
    {
        DebugAssertions.Assert(!sizeWithOverflow.IsEmpty, "!sizeWithOverflow.isEmpty");

        const double edgePadding = 8.0;
        double rectangleWidth = UpperRectangleWidth(labelPainter, scale);

        // Value indicator draws on the Overlay and by using the global Offset
        // we are making sure we use the bounds of the Overlay instead of the Slider.
        Point globalCenter = parentBox.LocalToGlobal(center);

        // The rectangle must be shifted towards the center so that it minimizes the
        // chance of it rendering outside the bounds of the render box. If the shift
        // is negative, then the lobe is shifted from right to left, and if it is
        // positive, then the lobe is shifted from left to right.
        double overflowLeft = Math.Max(0, (rectangleWidth / 2) - globalCenter.X + edgePadding);
        double overflowRight = Math.Max(
            0,
            (rectangleWidth / 2) - (sizeWithOverflow.Width - globalCenter.X - edgePadding));

        if (rectangleWidth < sizeWithOverflow.Width)
        {
            return overflowLeft - overflowRight;
        }
        else if (overflowLeft - overflowRight > 0)
        {
            return overflowLeft - (edgePadding * textScaleFactor);
        }
        else
        {
            return -overflowRight + (edgePadding * textScaleFactor);
        }
    }

    private static double UpperRectangleWidth(TextPainter labelPainter, double scale)
    {
        double unscaledWidth = Math.Max(MinLabelWidth, labelPainter.Width) + LabelPadding;
        return unscaledWidth * scale;
    }

    private static BorderRadius AdjustBorderRadius(Rect rect)
    {
        const double rectness = 0.0;
        return BorderRadius.Lerp(
            BorderRadius.All(Radius.Circular(UpperRectRadius)),
            BorderRadius.All(Radius.Circular(DartGeometry.ShortestSide(rect) / 2.0)),
            1.0 - rectness)!.Value;
    }

    public void Paint(
        RenderBox parentBox,
        Canvas canvas,
        Point center,
        double scale,
        TextPainter labelPainter,
        double textScaleFactor,
        Size sizeWithOverflow,
        Color backgroundPaintColor,
        Color? strokePaintColor = null)
    {
        if (scale == 0.0)
        {
            // Zero scale essentially means "do not draw anything", so it's safe to just return.
            return;
        }

        DebugAssertions.Assert(!sizeWithOverflow.IsEmpty, "!sizeWithOverflow.isEmpty");
        double rectangleWidth = UpperRectangleWidth(labelPainter, scale);
        double horizontalShift = GetHorizontalShift(
            parentBox: parentBox,
            center: center,
            labelPainter: labelPainter,
            textScaleFactor: textScaleFactor,
            sizeWithOverflow: sizeWithOverflow,
            scale: scale);
        var upperRect = new Rect(
            (-rectangleWidth / 2) + horizontalShift,
            -RectYOffset - MinRectHeight,
            rectangleWidth,
            MinRectHeight);

        var fillPaint = new SolidColorBrush(backgroundPaintColor);

        canvas.Save();
        canvas.Translate(center.X, center.Y - BottomTipYOffset);
        canvas.Scale(scale, scale);

        BorderRadius adjustedBorderRadius = AdjustBorderRadius(upperRect);
        RRect borderRect = ((BorderRadiusGeometry)adjustedBorderRadius)
            .Resolve(labelPainter.TextDirection)
            .ToRRect(upperRect);
        var trianglePath = new Path();
        trianglePath.LineTo(-TriangleHeight, -TriangleHeight);
        trianglePath.LineTo(TriangleHeight, -TriangleHeight);
        trianglePath.Close();
        trianglePath.AddRRect(borderRect);

        if (strokePaintColor != null)
        {
            var strokePaint = new Pen(new SolidColorBrush(strokePaintColor), 1.0);
            canvas.DrawPath(trianglePath, null, strokePaint);
        }

        canvas.DrawPath(trianglePath, fillPaint, null);

        // The label text is centered within the value indicator.
        double bottomTipToUpperRectTranslateY = (-PreferredHalfHeight / 2) - upperRect.Height;
        canvas.Translate(0, bottomTipToUpperRectTranslateY);
        var boxCenter = new Point(horizontalShift, upperRect.Height / 1.75);
        var halfLabelPainterOffset = new Point(labelPainter.Width / 2, labelPainter.Height / 2);
        Point labelOffset = boxCenter - halfLabelPainterOffset;
        labelPainter.Paint(canvas, labelOffset);
        canvas.Restore();
    }
}

/// <summary>The bar shape of a Material 3 <see cref="Slider"/>'s thumb.</summary>
/// <remarks>
/// The thumb bar shape width is reduced when the thumb is pressed. The thumb size comes from
/// <see cref="SliderThemeData.ThumbSize"/>, resolved with no states; the preferred size is 4x44.
/// </remarks>
public class HandleThumbShape : SliderComponentShape
{
    /// <summary>Create a slider thumb that draws a bar.</summary>
    public HandleThumbShape()
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
        bool isDiscrete,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        DebugAssertions.Assert(sliderTheme.DisabledThumbColor != null, "sliderTheme.disabledThumbColor != null");
        DebugAssertions.Assert(sliderTheme.ThumbColor != null, "sliderTheme.thumbColor != null");
        DebugAssertions.Assert(sliderTheme.ThumbSize != null, "sliderTheme.thumbSize != null");

        var colorTween = new ColorTween(
            begin: sliderTheme.DisabledThumbColor,
            end: sliderTheme.ThumbColor);
        Color color = colorTween.Transform(enableAnimation.Value);

        Canvas canvas = context.Canvas;
        Size thumbSize = sliderTheme.ThumbSize!.Resolve(
            new HashSet<WidgetState>())!.Value; // This is resolved in the paint method.
        RRect rrect = RRect.FromRectAndRadius(
            DartGeometry.RectFromCenter(center, thumbSize.Width, thumbSize.Height),
            Radius.Circular(Math.Min(Math.Abs(thumbSize.Width), Math.Abs(thumbSize.Height)) / 2));
        canvas.DrawRRect(rrect, new SolidColorBrush(color), null);
    }
}

/// <summary>The gapped shape of a Material 3 <see cref="Slider"/>'s track.</summary>
/// <remarks>
/// Active and inactive tracks with circular outer corners and 2px inside corners, separated by a gap
/// of <see cref="SliderThemeData.TrackGap"/> on each side of the thumb center. A stop indicator dot is
/// painted at the track end for continuous sliders.
/// </remarks>
public class GappedSliderTrackShape : BaseSliderTrackShape
{
    /// <summary>Create a slider track that draws two rectangles with rounded outer edges.</summary>
    public GappedSliderTrackShape()
    {
    }

    /// <summary>Paints the track with Dart's default <c>additionalActiveTrackHeight</c> of 2.</summary>
    public override void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        TextDirection textDirection,
        Point? secondaryOffset = null,
        bool isEnabled = false,
        bool isDiscrete = false)
    {
        Paint(
            context,
            offset,
            parentBox,
            sliderTheme,
            enableAnimation,
            thumbCenter,
            textDirection,
            secondaryOffset,
            isEnabled,
            isDiscrete,
            additionalActiveTrackHeight: 2);
    }

    /// <summary>Dart's <c>paint</c> override, which widens the base signature with the optional (and
    /// unused) <paramref name="additionalActiveTrackHeight"/>; C# overrides cannot add parameters.</summary>
    public void Paint(
        PaintingContext context,
        Point offset,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        Animation<double> enableAnimation,
        Point thumbCenter,
        TextDirection textDirection,
        Point? secondaryOffset,
        bool isEnabled,
        bool isDiscrete,
        double additionalActiveTrackHeight)
    {
        DebugAssertions.Assert(
            sliderTheme.DisabledActiveTrackColor != null,
            "sliderTheme.disabledActiveTrackColor != null");
        DebugAssertions.Assert(
            sliderTheme.DisabledInactiveTrackColor != null,
            "sliderTheme.disabledInactiveTrackColor != null");
        DebugAssertions.Assert(sliderTheme.ActiveTrackColor != null, "sliderTheme.activeTrackColor != null");
        DebugAssertions.Assert(sliderTheme.InactiveTrackColor != null, "sliderTheme.inactiveTrackColor != null");
        DebugAssertions.Assert(sliderTheme.ThumbShape != null, "sliderTheme.thumbShape != null");
        DebugAssertions.Assert(sliderTheme.TrackGap != null, "sliderTheme.trackGap != null");
        DebugAssertions.Assert(
            !double.IsNegative(sliderTheme.TrackGap!.Value),
            "!sliderTheme.trackGap!.isNegative");
        // If the slider [SliderThemeData.trackHeight] is less than or equal to 0,
        // then it makes no difference whether the track is painted or not,
        // therefore the painting can be a no-op.
        if (sliderTheme.TrackHeight == null || sliderTheme.TrackHeight.Value <= 0)
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
        var activePaint = new SolidColorBrush(activeTrackColorTween.Transform(enableAnimation.Value));
        var inactivePaint = new SolidColorBrush(inactiveTrackColorTween.Transform(enableAnimation.Value));
        IBrush leftTrackPaint;
        IBrush rightTrackPaint;
        switch (textDirection)
        {
            case TextDirection.Ltr:
                leftTrackPaint = activePaint;
                rightTrackPaint = inactivePaint;
                break;
            default:
                leftTrackPaint = inactivePaint;
                rightTrackPaint = activePaint;
                break;
        }

        // Gap, starting from the middle of the thumb.
        double trackGap = sliderTheme.TrackGap!.Value;
        double trackHeight = sliderTheme.TrackHeight.Value;

        Rect trackRect = GetPreferredRect(
            parentBox: parentBox,
            offset: offset,
            sliderTheme: sliderTheme,
            isEnabled: isEnabled,
            isDiscrete: isDiscrete);

        Radius trackCornerRadius = Radius.Circular(DartGeometry.ShortestSide(trackRect) / 2);
        Radius trackInsideCornerRadius = Radius.Circular(2.0);

        var trackRRect = new RRect(
            trackRect,
            topLeft: trackCornerRadius,
            topRight: trackCornerRadius,
            bottomRight: trackCornerRadius,
            bottomLeft: trackCornerRadius);

        RRect leftRRect = RRect.FromLTRBAndCorners(
            trackRect.Left,
            trackRect.Top,
            Math.Max(trackRect.Left, thumbCenter.X - trackGap),
            trackRect.Bottom,
            topLeft: trackCornerRadius,
            bottomLeft: trackCornerRadius,
            topRight: trackInsideCornerRadius,
            bottomRight: trackInsideCornerRadius);

        RRect rightRRect = RRect.FromLTRBAndCorners(
            thumbCenter.X + trackGap,
            trackRect.Top,
            trackRect.Right,
            trackRect.Bottom,
            topRight: trackCornerRadius,
            bottomRight: trackCornerRadius,
            topLeft: trackInsideCornerRadius,
            bottomLeft: trackInsideCornerRadius);

        context.Canvas.Save();
        context.Canvas.ClipRRect(trackRRect);
        bool drawLeftTrack = thumbCenter.X > (leftRRect.Left + (trackHeight / 2));
        bool drawRightTrack = thumbCenter.X < (rightRRect.Right - (trackHeight / 2));
        if (drawLeftTrack)
        {
            context.Canvas.DrawRRect(leftRRect, leftTrackPaint, null);
        }

        if (drawRightTrack)
        {
            context.Canvas.DrawRRect(rightRRect, rightTrackPaint, null);
        }

        bool isLTR = textDirection == TextDirection.Ltr;
        bool showSecondaryTrack =
            secondaryOffset != null
            && (isLTR
                ? secondaryOffset.Value.X > thumbCenter.X + trackGap
                : secondaryOffset.Value.X < thumbCenter.X - trackGap);

        if (showSecondaryTrack)
        {
            var secondaryTrackColorTween = new ColorTween(
                begin: sliderTheme.DisabledSecondaryActiveTrackColor,
                end: sliderTheme.SecondaryActiveTrackColor);
            var secondaryTrackPaint =
                new SolidColorBrush(secondaryTrackColorTween.Transform(enableAnimation.Value));
            if (isLTR)
            {
                context.Canvas.DrawRRect(
                    RRect.FromLTRBAndCorners(
                        thumbCenter.X + trackGap,
                        trackRect.Top,
                        secondaryOffset!.Value.X,
                        trackRect.Bottom,
                        topLeft: trackInsideCornerRadius,
                        bottomLeft: trackInsideCornerRadius,
                        topRight: trackCornerRadius,
                        bottomRight: trackCornerRadius),
                    secondaryTrackPaint,
                    null);
            }
            else
            {
                context.Canvas.DrawRRect(
                    RRect.FromLTRBAndCorners(
                        secondaryOffset!.Value.X - trackGap,
                        trackRect.Top,
                        thumbCenter.X,
                        trackRect.Bottom,
                        topLeft: trackInsideCornerRadius,
                        bottomLeft: trackInsideCornerRadius,
                        topRight: trackCornerRadius,
                        bottomRight: trackCornerRadius),
                    secondaryTrackPaint,
                    null);
            }
        }

        context.Canvas.Restore();

        const double stopIndicatorRadius = 2.0;
        double stopIndicatorTrailingSpace = trackHeight / 2;
        var stopIndicatorOffset = new Point(
            (textDirection == TextDirection.Ltr)
                ? trackRect.Right - stopIndicatorTrailingSpace
                : trackRect.Left + stopIndicatorTrailingSpace,
            trackRect.Center.Y);

        bool showStopIndicator = (textDirection == TextDirection.Ltr)
            ? thumbCenter.X < stopIndicatorOffset.X
            : thumbCenter.X > stopIndicatorOffset.X;
        if (showStopIndicator && !isDiscrete)
        {
            Rect stopIndicatorRect = DartGeometry.RectFromCenter(
                stopIndicatorOffset,
                stopIndicatorRadius * 2,
                stopIndicatorRadius * 2);
            context.Canvas.DrawCircle(activePaint, null, stopIndicatorRect.Center, stopIndicatorRadius);
        }
    }

    public override bool IsRounded => true;
}

/// <summary>The rounded rectangle shape of a Material 3 <see cref="Slider"/>'s value indicator.</summary>
/// <remarks>
/// Filled with <see cref="SliderThemeData.ValueIndicatorColor"/>; when
/// <see cref="SliderThemeData.ValueIndicatorStrokeColor"/> is provided, the indicator is drawn with a
/// stroke border of that color.
/// </remarks>
public class RoundedRectSliderValueIndicatorShape : SliderComponentShape
{
    /// <summary>Create a slider value indicator that resembles a rounded rectangle.</summary>
    public RoundedRectSliderValueIndicatorShape()
    {
    }

    private static readonly RoundedRectSliderValueIndicatorPathPainter _pathPainter = new();

    /// <summary>Forwards to the widened overload with no label painter, which asserts like Dart does when
    /// the named arguments are omitted.</summary>
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete)
    {
        return GetPreferredSize(isEnabled, isDiscrete, labelPainter: null, textScaleFactor: null);
    }

    /// <summary>Dart's <c>getPreferredSize</c> override with the optional named <c>labelPainter</c> and
    /// <c>textScaleFactor</c>; C# overrides cannot add parameters.</summary>
    public Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter? labelPainter,
        double? textScaleFactor)
    {
        DebugAssertions.Assert(labelPainter != null, "labelPainter != null");
        DebugAssertions.Assert(
            textScaleFactor != null && textScaleFactor >= 0,
            "textScaleFactor != null && textScaleFactor >= 0");
        return _pathPainter.GetPreferredSize(labelPainter!, textScaleFactor!.Value);
    }

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        Canvas canvas = context.Canvas;
        double scale = activationAnimation.Value;
        _pathPainter.Paint(
            parentBox: parentBox,
            canvas: canvas,
            center: center,
            scale: scale,
            labelPainter: labelPainter,
            textScaleFactor: textScaleFactor,
            sizeWithOverflow: sizeWithOverflow,
            backgroundPaintColor: sliderTheme.ValueIndicatorColor!,
            strokePaintColor: sliderTheme.ValueIndicatorStrokeColor);
    }
}

/// <summary>Dart's <c>_RoundedRectSliderValueIndicatorPathPainter</c>.</summary>
/// <remarks>range_slider_parts.dart declares a byte-identical private copy; both the slider and the
/// range slider value indicator shapes use this single C# class.</remarks>
internal sealed class RoundedRectSliderValueIndicatorPathPainter
{
    private const double LabelPadding = 10.0;
    private const double PreferredHeight = 32.0;
    private const double MinLabelWidth = 16.0;
    private const double RectYOffset = 10.0;
    private const double BottomTipYOffset = 16.0;
    private const double PreferredHalfHeight = PreferredHeight / 2;

    public Size GetPreferredSize(TextPainter labelPainter, double textScaleFactor)
    {
        double width =
            Math.Max(MinLabelWidth, labelPainter.Width) + ((LabelPadding * 2) * textScaleFactor);
        return new Size(width, PreferredHeight * textScaleFactor);
    }

    public double GetHorizontalShift(
        RenderBox parentBox,
        Point center,
        TextPainter labelPainter,
        double textScaleFactor,
        Size sizeWithOverflow,
        double scale)
    {
        DebugAssertions.Assert(!sizeWithOverflow.IsEmpty, "!sizeWithOverflow.isEmpty");

        const double edgePadding = 8.0;
        double rectangleWidth = UpperRectangleWidth(labelPainter, scale);

        // Value indicator draws on the Overlay and by using the global Offset
        // we are making sure we use the bounds of the Overlay instead of the Slider.
        Point globalCenter = parentBox.LocalToGlobal(center);

        // The rectangle must be shifted towards the center so that it minimizes the
        // chance of it rendering outside the bounds of the render box. If the shift
        // is negative, then the lobe is shifted from right to left, and if it is
        // positive, then the lobe is shifted from left to right.
        double overflowLeft = Math.Max(0, (rectangleWidth / 2) - globalCenter.X + edgePadding);
        double overflowRight = Math.Max(
            0,
            (rectangleWidth / 2) - (sizeWithOverflow.Width - globalCenter.X - edgePadding));

        if (rectangleWidth < sizeWithOverflow.Width)
        {
            return overflowLeft - overflowRight;
        }
        else if (overflowLeft - overflowRight > 0)
        {
            return overflowLeft - (edgePadding * textScaleFactor);
        }
        else
        {
            return -overflowRight + (edgePadding * textScaleFactor);
        }
    }

    private static double UpperRectangleWidth(TextPainter labelPainter, double scale)
    {
        double unscaledWidth = Math.Max(MinLabelWidth, labelPainter.Width) + (LabelPadding * 2);
        return unscaledWidth * scale;
    }

    public void Paint(
        RenderBox parentBox,
        Canvas canvas,
        Point center,
        double scale,
        TextPainter labelPainter,
        double textScaleFactor,
        Size sizeWithOverflow,
        Color backgroundPaintColor,
        Color? strokePaintColor = null)
    {
        if (scale == 0.0)
        {
            // Zero scale essentially means "do not draw anything", so it's safe to just return.
            return;
        }

        DebugAssertions.Assert(!sizeWithOverflow.IsEmpty, "!sizeWithOverflow.isEmpty");

        double rectangleWidth = UpperRectangleWidth(labelPainter, scale);
        double horizontalShift = GetHorizontalShift(
            parentBox: parentBox,
            center: center,
            labelPainter: labelPainter,
            textScaleFactor: textScaleFactor,
            sizeWithOverflow: sizeWithOverflow,
            scale: scale);

        var upperRect = new Rect(
            (-rectangleWidth / 2) + horizontalShift,
            -RectYOffset - PreferredHeight,
            rectangleWidth,
            PreferredHeight);

        var fillPaint = new SolidColorBrush(backgroundPaintColor);

        canvas.Save();
        // Prepare the canvas for the base of the tooltip, which is relative to the
        // center of the thumb.
        canvas.Translate(center.X, center.Y - BottomTipYOffset);
        canvas.Scale(scale, scale);

        RRect rrect = RRect.FromRectAndRadius(upperRect, Radius.Circular(upperRect.Height / 2));
        if (strokePaintColor != null)
        {
            var strokePaint = new Pen(new SolidColorBrush(strokePaintColor), 1.0);
            canvas.DrawRRect(rrect, null, strokePaint);
        }

        canvas.DrawRRect(rrect, fillPaint, null);

        // The label text is centered within the value indicator.
        double bottomTipToUpperRectTranslateY = (-PreferredHalfHeight / 2) - upperRect.Height;
        canvas.Translate(0, bottomTipToUpperRectTranslateY);
        var boxCenter = new Point(horizontalShift, upperRect.Height / 2.3);
        var halfLabelPainterOffset = new Point(labelPainter.Width / 2, labelPainter.Height / 2);
        Point labelOffset = boxCenter - halfLabelPainterOffset;
        labelPainter.Paint(canvas, labelOffset);
        canvas.Restore();
    }
}

/// <summary>Dart's top-level private <c>_debugDrawShadow</c> of slider_parts.dart.</summary>
file static class SliderPartsDebug
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
