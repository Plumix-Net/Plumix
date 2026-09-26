using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Canvas = Plumix.UI.Canvas;
using Path = Plumix.UI.Path;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/slider_value_indicator_shape.dart

/// <summary>
/// Base class for slider thumb, thumb overlay, and value indicator shapes.
/// </summary>
/// <remarks>
/// Create a subclass of this if you would like a custom shape. All shapes are painted to the same
/// canvas and ordering is important. The overlay is painted first, then the value indicator, then
/// the thumb.
/// </remarks>
public abstract class SliderComponentShape
{
    /// This abstract const constructor enables subclasses to provide const constructors so that
    /// they can be used in const expressions.
    protected SliderComponentShape()
    {
    }

    /// Returns the preferred size of the shape, based on the given conditions.
    public abstract Size GetPreferredSize(bool isEnabled, bool isDiscrete);

    /// Paints the shape, taking into account the state passed to it.
    ///
    /// [center] is the offset for where this shape's center should be painted. This offset is
    /// relative to the origin of the [context] canvas. [activationAnimation] runs from 0 to 1
    /// when the thumb is pressed; [enableAnimation] runs from 0 to 1 when the slider is enabled.
    /// [sizeWithOverflow] is the size of the parent box plus the value indicator overflow.
    public abstract void Paint(
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
        Size sizeWithOverflow);

    /// Special instance of [SliderComponentShape] to skip the thumb drawing.
    public static SliderComponentShape NoThumb { get; } = new EmptySliderComponentShape();

    /// Special instance of [SliderComponentShape] to skip the overlay drawing.
    public static SliderComponentShape NoOverlay { get; } = new EmptySliderComponentShape();

    /// Dart's `Object.toString`, which the slider theme's diagnostics print for shapes.
    public override string ToString() => $"Instance of '{Diagnostics.DescribeType(GetType())}'";
}

// The following shapes are the material defaults.

/// Dart's `_EmptySliderComponentShape`.
internal sealed class EmptySliderComponentShape : SliderComponentShape
{
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => default;

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
        // no-op.
    }
}

/// <summary>
/// The default shape of a [Slider]'s thumb overlay.
/// </summary>
/// <remarks>
/// The shape of the overlay is a circle with the same center as the thumb, but with a larger
/// radius. It animates to full size when the thumb is pressed, and animates back down to size 0
/// when it is released. It is painted behind the thumb, and is expected to extend beyond the
/// bounds of the thumb so that it is visible.
/// </remarks>
public class RoundSliderOverlayShape : SliderComponentShape
{
    /// Create a slider thumb overlay that draws a circle.
    public RoundSliderOverlayShape(double overlayRadius = 24.0)
    {
        OverlayRadius = overlayRadius;
    }

    /// The preferred radius of the round thumb shape when enabled.
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
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        Canvas canvas = context.Canvas;
        var radiusTween = new DoubleTween(begin: 0.0, end: OverlayRadius);

        canvas.DrawCircle(
            new SolidColorBrush(sliderTheme.OverlayColor!),
            null,
            center,
            radiusTween.Evaluate(activationAnimation.Value));
    }
}

/// <summary>
/// The default shape of a Material 3 [Slider]'s value indicator.
/// </summary>
public class RectangularSliderValueIndicatorShape : SliderComponentShape
{
    /// Create a slider value indicator that resembles a rectangular tooltip.
    public RectangularSliderValueIndicatorShape()
    {
    }

    private static readonly RectangularSliderValueIndicatorPathPainter _pathPainter = new();

    /// Dart's two-argument override cannot carry the named `labelPainter`/`textScaleFactor`
    /// parameters in C#; it forwards to the widened overload, which asserts they are given.
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) =>
        GetPreferredSize(isEnabled, isDiscrete, labelPainter: null, textScaleFactor: null);

    public Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter? labelPainter,
        double? textScaleFactor)
    {
        if (Constants.KDebugMode && labelPainter == null)
        {
            throw new AssertionError("labelPainter != null");
        }

        if (Constants.KDebugMode && !(textScaleFactor != null && textScaleFactor >= 0))
        {
            throw new AssertionError("textScaleFactor != null && textScaleFactor >= 0");
        }

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

/// <summary>
/// The default shape of a Material 3 [RangeSlider]'s value indicators.
/// </summary>
public class RectangularRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    /// Create a range slider value indicator that resembles a rectangular tooltip.
    public RectangularRangeSliderValueIndicatorShape()
    {
    }

    private static readonly RectangularSliderValueIndicatorPathPainter _pathPainter = new();

    public override Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter labelPainter,
        double textScaleFactor)
    {
        if (Constants.KDebugMode && !(textScaleFactor >= 0))
        {
            throw new AssertionError("textScaleFactor >= 0");
        }

        return _pathPainter.GetPreferredSize(labelPainter, textScaleFactor);
    }

    public override double GetHorizontalShift(
        RenderBox? parentBox = null,
        Point? center = null,
        TextPainter? labelPainter = null,
        Animation<double>? activationAnimation = null,
        double? textScaleFactor = null,
        Size? sizeWithOverflow = null)
    {
        return _pathPainter.GetHorizontalShift(
            parentBox: parentBox!,
            center: center!.Value,
            labelPainter: labelPainter!,
            textScaleFactor: textScaleFactor!.Value,
            sizeWithOverflow: sizeWithOverflow!.Value,
            scale: activationAnimation!.Value);
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

/// Dart's `_RectangularSliderValueIndicatorPathPainter`.
internal sealed class RectangularSliderValueIndicatorPathPainter
{
    private const double _triangleHeight = 8.0;
    private const double _labelPadding = 16.0;
    private const double _preferredHeight = 32.0;
    private const double _minLabelWidth = 16.0;
    private const double _bottomTipYOffset = 14.0;
    private const double _preferredHalfHeight = _preferredHeight / 2;
    private const double _upperRectRadius = 4;

    public Size GetPreferredSize(TextPainter labelPainter, double textScaleFactor)
    {
        return new Size(
            UpperRectangleWidth(labelPainter, 1, textScaleFactor),
            labelPainter.Height + _labelPadding);
    }

    public double GetHorizontalShift(
        RenderBox parentBox,
        Point center,
        TextPainter labelPainter,
        double textScaleFactor,
        Size sizeWithOverflow,
        double scale)
    {
        if (Constants.KDebugMode && sizeWithOverflow.IsEmpty)
        {
            throw new AssertionError("!sizeWithOverflow.isEmpty");
        }

        const double edgePadding = 8.0;
        double rectangleWidth = UpperRectangleWidth(labelPainter, scale, textScaleFactor);

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

    private static double UpperRectangleWidth(TextPainter labelPainter, double scale, double textScaleFactor)
    {
        double unscaledWidth =
            Math.Max(_minLabelWidth * textScaleFactor, labelPainter.Width) + (_labelPadding * 2);
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

        if (Constants.KDebugMode && sizeWithOverflow.IsEmpty)
        {
            throw new AssertionError("!sizeWithOverflow.isEmpty");
        }

        double rectangleWidth = UpperRectangleWidth(labelPainter, scale, textScaleFactor);
        double horizontalShift = GetHorizontalShift(
            parentBox: parentBox,
            center: center,
            labelPainter: labelPainter,
            textScaleFactor: textScaleFactor,
            sizeWithOverflow: sizeWithOverflow,
            scale: scale);

        double rectHeight = labelPainter.Height + _labelPadding;
        var upperRect = new Rect(
            (-rectangleWidth / 2) + horizontalShift,
            -_triangleHeight - rectHeight,
            rectangleWidth,
            rectHeight);

        var trianglePath = new Path();
        trianglePath.LineTo(-_triangleHeight, -_triangleHeight);
        trianglePath.LineTo(_triangleHeight, -_triangleHeight);
        trianglePath.Close();
        var fillPaint = new SolidColorBrush(backgroundPaintColor);
        RRect upperRRect = RRect.FromRectAndRadius(upperRect, Radius.Circular(_upperRectRadius));
        trianglePath.AddRRect(upperRRect);

        canvas.Save();
        // Prepare the canvas for the base of the tooltip, which is relative to the
        // center of the thumb.
        canvas.Translate(center.X, center.Y - _bottomTipYOffset);
        canvas.Scale(scale, scale);
        if (strokePaintColor != null)
        {
            var strokePaint = new Pen(new SolidColorBrush(strokePaintColor), 1.0);
            canvas.DrawPath(trianglePath, null, strokePaint);
        }

        canvas.DrawPath(trianglePath, fillPaint, null);

        // The label text is centered within the value indicator.
        double bottomTipToUpperRectTranslateY = (-_preferredHalfHeight / 2) - upperRect.Height;
        canvas.Translate(0, bottomTipToUpperRectTranslateY);
        var boxCenter = new Point(horizontalShift, upperRect.Height / 2);
        var halfLabelPainterOffset = new Point(labelPainter.Width / 2, labelPainter.Height / 2);
        Point labelOffset = boxCenter - halfLabelPainterOffset;
        labelPainter.Paint(canvas, labelOffset);
        canvas.Restore();
    }
}

/// <summary>
/// A variant shape of a [Slider]'s value indicator. The value indicator is in the shape of an
/// upside-down pear.
/// </summary>
public class PaddleSliderValueIndicatorShape : SliderComponentShape
{
    /// Create a slider value indicator in the shape of an upside-down pear.
    public PaddleSliderValueIndicatorShape()
    {
    }

    private static readonly PaddleSliderValueIndicatorPathPainter _pathPainter = new();

    /// Dart's two-argument override cannot carry the named `labelPainter`/`textScaleFactor`
    /// parameters in C#; it forwards to the widened overload, which asserts they are given.
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) =>
        GetPreferredSize(isEnabled, isDiscrete, labelPainter: null, textScaleFactor: null);

    public Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter? labelPainter,
        double? textScaleFactor)
    {
        if (Constants.KDebugMode && labelPainter == null)
        {
            throw new AssertionError("labelPainter != null");
        }

        if (Constants.KDebugMode && !(textScaleFactor != null && textScaleFactor >= 0))
        {
            throw new AssertionError("textScaleFactor != null && textScaleFactor >= 0");
        }

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
        if (Constants.KDebugMode && sizeWithOverflow.IsEmpty)
        {
            throw new AssertionError("!sizeWithOverflow.isEmpty");
        }

        var enableColor = new ColorTween(
            begin: sliderTheme.DisabledThumbColor,
            end: sliderTheme.ValueIndicatorColor);
        _pathPainter.Paint(
            context.Canvas,
            center,
            new Plumix.UI.Paint { Color = enableColor.Evaluate(enableAnimation.Value) },
            activationAnimation.Value,
            labelPainter,
            textScaleFactor,
            sizeWithOverflow,
            sliderTheme.ValueIndicatorStrokeColor);
    }
}

/// <summary>
/// A variant shape of a [RangeSlider]'s value indicators. The value indicator is in the shape of
/// an upside-down pear.
/// </summary>
public class PaddleRangeSliderValueIndicatorShape : RangeSliderValueIndicatorShape
{
    /// Create a slider value indicator in the shape of an upside-down pear.
    public PaddleRangeSliderValueIndicatorShape()
    {
    }

    private static readonly PaddleSliderValueIndicatorPathPainter _pathPainter = new();

    public override Size GetPreferredSize(
        bool isEnabled,
        bool isDiscrete,
        TextPainter labelPainter,
        double textScaleFactor)
    {
        if (Constants.KDebugMode && !(textScaleFactor >= 0))
        {
            throw new AssertionError("textScaleFactor >= 0");
        }

        return _pathPainter.GetPreferredSize(labelPainter, textScaleFactor);
    }

    public override double GetHorizontalShift(
        RenderBox? parentBox = null,
        Point? center = null,
        TextPainter? labelPainter = null,
        Animation<double>? activationAnimation = null,
        double? textScaleFactor = null,
        Size? sizeWithOverflow = null)
    {
        return _pathPainter.GetHorizontalShift(
            center: center!.Value,
            labelPainter: labelPainter!,
            scale: activationAnimation!.Value,
            textScaleFactor: textScaleFactor!.Value,
            sizeWithOverflow: sizeWithOverflow!.Value);
    }

    /// Dart declares `bool isOnTop = false`; the shared abstract signature makes it `bool?`, so a
    /// null is read as that default.
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
        if (Constants.KDebugMode && sizeWithOverflow!.Value.IsEmpty)
        {
            throw new AssertionError("!sizeWithOverflow!.isEmpty");
        }

        var enableColor = new ColorTween(
            begin: sliderTheme.DisabledThumbColor,
            end: sliderTheme.ValueIndicatorColor);
        // Add a stroke of 1dp around the top paddle.
        _pathPainter.Paint(
            context.Canvas,
            center,
            new Plumix.UI.Paint { Color = enableColor.Evaluate(enableAnimation.Value) },
            activationAnimation.Value,
            labelPainter,
            textScaleFactor!.Value,
            sizeWithOverflow!.Value,
            (isOnTop ?? false) ? sliderTheme.OverlappingShapeStrokeColor : sliderTheme.ValueIndicatorStrokeColor);
    }
}

/// Dart's `_PaddleSliderValueIndicatorPathPainter`.
internal sealed class PaddleSliderValueIndicatorPathPainter
{
    // These constants define the shape of the default value indicator.
    // The value indicator changes shape based on the size of
    // the label: The top lobe spreads horizontally, and the
    // top arc on the neck moves down to keep it merging smoothly
    // with the top lobe as it expands.

    // Radius of the top lobe of the value indicator.
    private const double _topLobeRadius = 16.0;
    private const double _minLabelWidth = 16.0;

    // Radius of the bottom lobe of the value indicator.
    private const double _bottomLobeRadius = 10.0;
    private const double _labelPadding = 8.0;
    private const double _distanceBetweenTopBottomCenters = 40.0;
    private const double _middleNeckWidth = 3.0;
    private const double _bottomNeckRadius = 4.5;

    // The base of the triangle between the top lobe center and the centers of
    // the two top neck arcs.
    private const double _neckTriangleBase = _topNeckRadius + (_middleNeckWidth / 2);
    private const double _rightBottomNeckCenterX = (_middleNeckWidth / 2) + _bottomNeckRadius;
    private const double _rightBottomNeckAngleStart = Math.PI;
    private static readonly Point _topLobeCenter = new(0.0, -_distanceBetweenTopBottomCenters);
    private const double _topNeckRadius = 13.0;

    // The length of the hypotenuse of the triangle formed by the center
    // of the left top lobe arc and the center of the top left neck arc.
    // Used to calculate the position of the center of the arc.
    private const double _neckTriangleHypotenuse = _topLobeRadius + _topNeckRadius;

    // Some convenience values to help readability.
    private const double _twoSeventyDegrees = 3.0 * Math.PI / 2.0;
    private const double _ninetyDegrees = Math.PI / 2.0;
    private const double _thirtyDegrees = Math.PI / 6.0;
    private const double _preferredHeight =
        _distanceBetweenTopBottomCenters + _topLobeRadius + _bottomLobeRadius;

    // Set to true if you want a rectangle to be drawn around the label bubble.
    // This helps with building tests that check that the label draws in the right
    // place (because it prints the rect in the failed test output). It should not
    // be checked in while set to "true". (Dart's `static const`; readonly here so the
    // debug-only branch below does not trip the unreachable-code warning.)
    private static readonly bool _debuggingLabelLocation = false;

    public Size GetPreferredSize(TextPainter labelPainter, double textScaleFactor)
    {
        if (Constants.KDebugMode && !(textScaleFactor >= 0))
        {
            throw new AssertionError("textScaleFactor >= 0");
        }

        double width =
            Math.Max(_minLabelWidth * textScaleFactor, labelPainter.Width)
            + (_labelPadding * 2 * textScaleFactor);
        return new Size(width, _preferredHeight * textScaleFactor);
    }

    // Adds an arc to the path that has the attributes passed in. This is
    // a convenience to make adding arcs have less boilerplate.
    private static void AddArc(Path path, Point center, double radius, double startAngle, double endAngle)
    {
        if (Constants.KDebugMode && !(double.IsFinite(center.X) && double.IsFinite(center.Y)))
        {
            throw new AssertionError("center.isFinite");
        }

        var arcRect = new Rect(center.X - radius, center.Y - radius, radius * 2.0, radius * 2.0);
        path.ArcTo(arcRect, startAngle, endAngle - startAngle, false);
    }

    public double GetHorizontalShift(
        Point center,
        TextPainter labelPainter,
        double scale,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        if (Constants.KDebugMode && sizeWithOverflow.IsEmpty)
        {
            throw new AssertionError("!sizeWithOverflow.isEmpty");
        }

        double inverseTextScale = textScaleFactor != 0 ? 1.0 / textScaleFactor : 0.0;
        double labelHalfWidth = labelPainter.Width / 2.0;
        double halfWidthNeeded = Math.Max(
            0.0,
            (inverseTextScale * labelHalfWidth) - (_topLobeRadius - _labelPadding));
        double shift = GetIdealOffset(
            halfWidthNeeded,
            textScaleFactor * scale,
            center,
            sizeWithOverflow.Width);
        return shift * textScaleFactor;
    }

    // Determines the "best" offset to keep the bubble within the slider. The
    // calling code will bound that with the available movement in the paddle shape.
    private static double GetIdealOffset(
        double halfWidthNeeded,
        double scale,
        Point center,
        double widthWithOverflow)
    {
        const double edgeMargin = 8.0;
        var topLobeRect = new Rect(
            -_topLobeRadius - halfWidthNeeded,
            -_topLobeRadius - _distanceBetweenTopBottomCenters,
            2.0 * (_topLobeRadius + halfWidthNeeded),
            2.0 * _topLobeRadius);
        // We can just multiply by scale instead of a transform, since we're scaling
        // around (0, 0).
        Point topLeft = (topLobeRect.TopLeft * scale) + center;
        Point bottomRight = (topLobeRect.BottomRight * scale) + center;
        double shift = 0.0;

        if (topLeft.X < edgeMargin)
        {
            shift = edgeMargin - topLeft.X;
        }

        double endGlobal = widthWithOverflow;
        if (bottomRight.X > endGlobal - edgeMargin)
        {
            shift = endGlobal - edgeMargin - bottomRight.X;
        }

        shift = scale == 0.0 ? 0.0 : shift / scale;
        if (shift < 0.0)
        {
            // Shifting to the left.
            shift = Math.Max(shift, -halfWidthNeeded);
        }
        else
        {
            // Shifting to the right.
            shift = Math.Min(shift, halfWidthNeeded);
        }

        return shift;
    }

    public void Paint(
        Canvas canvas,
        Point center,
        Plumix.UI.Paint paint,
        double scale,
        TextPainter labelPainter,
        double textScaleFactor,
        Size sizeWithOverflow,
        Color? strokePaintColor)
    {
        if (scale == 0.0)
        {
            // Zero scale essentially means "do not draw anything", so it's safe to just return. Otherwise,
            // our math below will attempt to divide by zero and send needless NaNs to the engine.
            return;
        }

        if (Constants.KDebugMode && sizeWithOverflow.IsEmpty)
        {
            throw new AssertionError("!sizeWithOverflow.isEmpty");
        }

        // The entire value indicator should scale with the size of the label,
        // to keep it large enough to encompass the label text.
        double overallScale = scale * textScaleFactor;
        double inverseTextScale = textScaleFactor != 0 ? 1.0 / textScaleFactor : 0.0;
        double labelHalfWidth = labelPainter.Width / 2.0;

        canvas.Save();
        canvas.Translate(center.X, center.Y);
        canvas.Scale(overallScale, overallScale);

        double bottomNeckTriangleHypotenuse =
            _bottomNeckRadius + (_bottomLobeRadius / overallScale);
        double rightBottomNeckCenterY = -Math.Sqrt(
            Math.Pow(bottomNeckTriangleHypotenuse, 2) - Math.Pow(_rightBottomNeckCenterX, 2));
        double rightBottomNeckAngleEnd =
            Math.PI + Math.Atan(rightBottomNeckCenterY / _rightBottomNeckCenterX);
        var path = new Path();
        path.MoveTo(_middleNeckWidth / 2, rightBottomNeckCenterY);
        AddArc(
            path,
            new Point(_rightBottomNeckCenterX, rightBottomNeckCenterY),
            _bottomNeckRadius,
            _rightBottomNeckAngleStart,
            rightBottomNeckAngleEnd);
        AddArc(
            path,
            new Point(0.0, 0.0),
            _bottomLobeRadius / overallScale,
            rightBottomNeckAngleEnd - Math.PI,
            (2 * Math.PI) - rightBottomNeckAngleEnd);
        AddArc(
            path,
            new Point(-_rightBottomNeckCenterX, rightBottomNeckCenterY),
            _bottomNeckRadius,
            Math.PI - rightBottomNeckAngleEnd,
            0);

        // This is the needed extra width for the label. It is only positive when
        // the label exceeds the minimum size contained by the round top lobe.
        double halfWidthNeeded = Math.Max(
            0.0,
            (inverseTextScale * labelHalfWidth) - (_topLobeRadius - _labelPadding));

        double shift = GetIdealOffset(
            halfWidthNeeded,
            overallScale,
            center,
            sizeWithOverflow.Width);
        double leftWidthNeeded = halfWidthNeeded - shift;
        double rightWidthNeeded = halfWidthNeeded + shift;

        // The parameter that describes how far along the transition from round to
        // stretched we are.
        double leftAmount = Math.Max(0.0, Math.Min(1.0, leftWidthNeeded / _neckTriangleBase));
        double rightAmount = Math.Max(0.0, Math.Min(1.0, rightWidthNeeded / _neckTriangleBase));
        // The angle between the top neck arc's center and the top lobe's center
        // and vertical. The base amount is chosen so that the neck is smooth,
        // even when the lobe is shifted due to its size.
        double leftTheta = (1.0 - leftAmount) * _thirtyDegrees;
        double rightTheta = (1.0 - rightAmount) * _thirtyDegrees;
        // The center of the top left neck arc.
        var leftTopNeckCenter = new Point(
            -_neckTriangleBase,
            _topLobeCenter.Y + (Math.Cos(leftTheta) * _neckTriangleHypotenuse));
        var neckRightCenter = new Point(
            _neckTriangleBase,
            _topLobeCenter.Y + (Math.Cos(rightTheta) * _neckTriangleHypotenuse));
        double leftNeckArcAngle = _ninetyDegrees - leftTheta;
        double rightNeckArcAngle = Math.PI + _ninetyDegrees - rightTheta;
        // The distance between the end of the bottom neck arc and the beginning of
        // the top neck arc. We use this to shrink/expand it based on the scale
        // factor of the value indicator.
        double neckStretchBaseline = Math.Max(
            0.0,
            rightBottomNeckCenterY - Math.Max(leftTopNeckCenter.Y, neckRightCenter.Y));
        double t = Math.Pow(inverseTextScale, 3.0);
        double stretch = Math.Clamp(neckStretchBaseline * t, 0.0, 10.0 * neckStretchBaseline);
        var neckStretch = new Point(0.0, neckStretchBaseline - stretch);

        if (Constants.KDebugMode && _debuggingLabelLocation)
        {
            Point leftCenter = _topLobeCenter - new Point(leftWidthNeeded, 0.0) + neckStretch;
            Point rightCenter = _topLobeCenter + new Point(rightWidthNeeded, 0.0) + neckStretch;
            var valueRect = new Rect(
                new Point(leftCenter.X - _topLobeRadius, leftCenter.Y - _topLobeRadius),
                new Point(rightCenter.X + _topLobeRadius, rightCenter.Y + _topLobeRadius));
            var outlinePaint = new Pen(new SolidColorBrush(new Color(0xffff0000)), 1.0);
            canvas.DrawRectangle(null, outlinePaint, valueRect);
        }

        AddArc(path, leftTopNeckCenter + neckStretch, _topNeckRadius, 0.0, -leftNeckArcAngle);
        AddArc(
            path,
            _topLobeCenter - new Point(leftWidthNeeded, 0.0) + neckStretch,
            _topLobeRadius,
            _ninetyDegrees + leftTheta,
            _twoSeventyDegrees);
        AddArc(
            path,
            _topLobeCenter + new Point(rightWidthNeeded, 0.0) + neckStretch,
            _topLobeRadius,
            _twoSeventyDegrees,
            _twoSeventyDegrees + Math.PI - rightTheta);
        AddArc(path, neckRightCenter + neckStretch, _topNeckRadius, rightNeckArcAngle, Math.PI);

        if (strokePaintColor != null)
        {
            var strokePaint = new Pen(new SolidColorBrush(strokePaintColor), 1.0);
            canvas.DrawPath(path, null, strokePaint);
        }

        canvas.DrawPath(path, new SolidColorBrush(paint.Color), null);

        // Draw the label.
        canvas.Save();
        canvas.Translate(shift, -_distanceBetweenTopBottomCenters + neckStretch.Y);
        canvas.Scale(inverseTextScale, inverseTextScale);
        labelPainter.Paint(canvas, new Point(0.0, 0.0) - new Point(labelHalfWidth, labelPainter.Height / 2.0));
        canvas.Restore();
        canvas.Restore();
    }
}
