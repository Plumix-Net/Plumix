using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollbar.dart

public delegate bool ScrollNotificationPredicate(ScrollNotification notification);

public enum ScrollbarOrientation
{
    Left,
    Right,
    Top,
    Bottom,
}

public readonly record struct ScrollbarGeometry(
    Rect TrackRect,
    Rect ThumbRect,
    Axis Axis,
    bool IsReversed,
    double TrackMainAxisStart,
    double TrackMainAxisExtent,
    double ThumbMainAxisOffset,
    double ThumbMainAxisExtent)
{
    public double MaxThumbTravel => Math.Max(0, TrackMainAxisExtent - ThumbMainAxisExtent);
}

public sealed class ScrollbarPainter : CustomPainter
{
    /// <summary>`scrollbar.dart`'s `_kMinInteractiveSize`: the minimum touch target of a thumb.</summary>
    private const double KMinInteractiveSize = 48.0;

    private readonly ChangeNotifier _repaint;
    private readonly Animation<double> _fadeoutOpacityAnimation;
    private IScrollMetrics? _metrics;
    private AxisDirection? _axisDirection;
    private double? _lastPixels;
    private double? _lastMinScrollExtent;
    private double? _lastMaxScrollExtent;
    private double? _lastViewportDimension;
    private Size _size;
    private Color _color;
    private Color _trackColor;
    private Color _trackBorderColor;
    private TextDirection? _textDirection;
    private double _thickness;
    private Thickness _padding;
    private double _mainAxisMargin;
    private double _crossAxisMargin;
    private double? _radius;
    private double? _trackRadius;
    private ShapeBorder? _shape;
    private double _minLength;
    private double _minOverscrollLength;
    private ScrollbarOrientation? _scrollbarOrientation;
    private bool _ignorePointer;

    public ScrollbarPainter(
        Color color,
        Animation<double> fadeoutOpacityAnimation,
        Color? trackColor = null,
        Color? trackBorderColor = null,
        TextDirection? textDirection = null,
        double thickness = 6,
        Thickness? padding = null,
        double mainAxisMargin = 0,
        double crossAxisMargin = 0,
        double? radius = null,
        double? trackRadius = null,
        ShapeBorder? shape = null,
        double minLength = 18,
        double? minOverscrollLength = null,
        ScrollbarOrientation? scrollbarOrientation = null,
        bool ignorePointer = false) : this(
        new ChangeNotifier(),
        color,
        fadeoutOpacityAnimation,
        trackColor,
        trackBorderColor,
        textDirection,
        thickness,
        padding,
        mainAxisMargin,
        crossAxisMargin,
        radius,
        trackRadius,
        shape,
        minLength,
        minOverscrollLength,
        scrollbarOrientation,
        ignorePointer)
    {
    }

    private ScrollbarPainter(
        ChangeNotifier repaint,
        Color color,
        Animation<double> fadeoutOpacityAnimation,
        Color? trackColor,
        Color? trackBorderColor,
        TextDirection? textDirection,
        double thickness,
        Thickness? padding,
        double mainAxisMargin,
        double crossAxisMargin,
        double? radius,
        double? trackRadius,
        ShapeBorder? shape,
        double minLength,
        double? minOverscrollLength,
        ScrollbarOrientation? scrollbarOrientation,
        bool ignorePointer) : base(repaint)
    {
        ArgumentNullException.ThrowIfNull(fadeoutOpacityAnimation);
        if (shape is not null && radius.HasValue)
        {
            throw new ArgumentException("Only one of shape and radius may be provided.");
        }

        ValidateNonNegative(nameof(thickness), thickness);
        ValidateNonNegative(nameof(mainAxisMargin), mainAxisMargin);
        ValidateNonNegative(nameof(crossAxisMargin), crossAxisMargin);
        ValidateNonNegative(nameof(radius), radius);
        ValidateNonNegative(nameof(trackRadius), trackRadius);
        ValidateNonNegative(nameof(minLength), minLength);
        ValidateNonNegative(nameof(minOverscrollLength), minOverscrollLength);
        if (minOverscrollLength > minLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minOverscrollLength));
        }

        _repaint = repaint;
        _color = color;
        _fadeoutOpacityAnimation = fadeoutOpacityAnimation;
        _trackColor = trackColor ?? Colors.Transparent;
        _trackBorderColor = trackBorderColor ?? Colors.Transparent;
        _textDirection = textDirection;
        _thickness = thickness;
        _padding = padding ?? default;
        _mainAxisMargin = mainAxisMargin;
        _crossAxisMargin = crossAxisMargin;
        _radius = radius;
        _trackRadius = trackRadius;
        _shape = shape;
        _minLength = minLength;
        _minOverscrollLength = minOverscrollLength ?? minLength;
        _scrollbarOrientation = scrollbarOrientation;
        _ignorePointer = ignorePointer;
        _fadeoutOpacityAnimation.AddListener(NotifyListeners);
    }

    public Color Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    public Animation<double> FadeoutOpacityAnimation => _fadeoutOpacityAnimation;

    public Color TrackColor
    {
        get => _trackColor;
        set => SetField(ref _trackColor, value);
    }

    public Color TrackBorderColor
    {
        get => _trackBorderColor;
        set => SetField(ref _trackBorderColor, value);
    }

    public TextDirection? TextDirection
    {
        get => _textDirection;
        set => SetField(ref _textDirection, value);
    }

    public double Thickness
    {
        get => _thickness;
        set
        {
            ValidateNonNegative(nameof(value), value);
            SetField(ref _thickness, value);
        }
    }

    public Thickness Padding
    {
        get => _padding;
        set => SetField(ref _padding, value);
    }

    public double MainAxisMargin
    {
        get => _mainAxisMargin;
        set
        {
            ValidateNonNegative(nameof(value), value);
            SetField(ref _mainAxisMargin, value);
        }
    }

    public double CrossAxisMargin
    {
        get => _crossAxisMargin;
        set
        {
            ValidateNonNegative(nameof(value), value);
            SetField(ref _crossAxisMargin, value);
        }
    }

    public double? Radius
    {
        get => _radius;
        set
        {
            ValidateNonNegative(nameof(value), value);
            if (value.HasValue && Shape is not null)
            {
                throw new ArgumentException("Only one of shape and radius may be provided.");
            }
            SetField(ref _radius, value);
        }
    }

    public double? TrackRadius
    {
        get => _trackRadius;
        set
        {
            ValidateNonNegative(nameof(value), value);
            SetField(ref _trackRadius, value);
        }
    }

    public ShapeBorder? Shape
    {
        get => _shape;
        set
        {
            if (value is not null && Radius.HasValue)
            {
                throw new ArgumentException("Only one of shape and radius may be provided.");
            }
            SetField(ref _shape, value);
        }
    }

    public double MinLength
    {
        get => _minLength;
        set
        {
            ValidateNonNegative(nameof(value), value);
            if (value < MinOverscrollLength)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            SetField(ref _minLength, value);
        }
    }

    public double MinOverscrollLength
    {
        get => _minOverscrollLength;
        set
        {
            ValidateNonNegative(nameof(value), value);
            if (value > MinLength)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            SetField(ref _minOverscrollLength, value);
        }
    }

    public ScrollbarOrientation? ScrollbarOrientation
    {
        get => _scrollbarOrientation;
        set => SetField(ref _scrollbarOrientation, value);
    }

    public bool IgnorePointer
    {
        get => _ignorePointer;
        set => SetField(ref _ignorePointer, value);
    }

    public ScrollbarGeometry? Geometry => ComputeGeometry(_size);

    public void Update(IScrollMetrics metrics, AxisDirection axisDirection)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        if (_lastPixels == metrics.Pixels &&
            _lastMinScrollExtent == metrics.MinScrollExtent &&
            _lastMaxScrollExtent == metrics.MaxScrollExtent &&
            _lastViewportDimension == metrics.ViewportDimension &&
            _axisDirection == axisDirection)
        {
            return;
        }

        _metrics = metrics;
        _axisDirection = axisDirection;
        _lastPixels = metrics.Pixels;
        _lastMinScrollExtent = metrics.MinScrollExtent;
        _lastMaxScrollExtent = metrics.MaxScrollExtent;
        _lastViewportDimension = metrics.ViewportDimension;
        NotifyListeners();
    }

    public void UpdateThickness(double nextThickness, double? nextRadius)
    {
        ValidateNonNegative(nameof(nextThickness), nextThickness);
        ValidateNonNegative(nameof(nextRadius), nextRadius);
        _thickness = nextThickness;
        _radius = nextRadius;
        NotifyListeners();
    }

    public override void Paint(PaintingContext context, Size size)
    {
        _size = size;
        ScrollbarGeometry? geometry = ComputeGeometry(size);
        if (!geometry.HasValue || _fadeoutOpacityAnimation.Value <= 0)
        {
            return;
        }

        ScrollbarGeometry value = geometry.Value;
        double opacity = Math.Clamp(_fadeoutOpacityAnimation.Value, 0, 1);
        var trackBrush = new SolidColorBrush(ApplyOpacity(TrackColor, opacity));
        context.Canvas.DrawRectangle(trackBrush, null, value.TrackRect, TrackRadius ?? 0, TrackRadius ?? 0);

        Color borderColor = ApplyOpacity(TrackBorderColor, opacity);
        if (borderColor.A != 0)
        {
            var pen = new Pen(new SolidColorBrush(borderColor), 1);
            (Point start, Point end) = TrackBorderLine(value);
            context.Canvas.DrawLine(pen, start, end);
        }

        BorderSide? side = (Shape as OutlinedBorder)?.Side;
        IPen? thumbPen = side is null
            ? null
            : new Pen(
                new SolidColorBrush(ApplyOpacity(side.Value.Color, opacity)),
                side.Value.Width);
        context.Canvas.DrawRectangle(
            new SolidColorBrush(ApplyOpacity(Color, opacity)),
            thumbPen,
            value.ThumbRect,
            ScrollbarShapeGeometry.Radius(Shape) ?? Radius ?? 0,
            ScrollbarShapeGeometry.Radius(Shape) ?? Radius ?? 0);
    }

    /// <summary>
    /// Whether the metrics the painter last saw describe a scrollable view. Dart's
    /// <c>_lastMetricsAreScrollable</c>.
    /// </summary>
    private bool LastMetricsAreScrollable =>
        _metrics is not null && _metrics.MinScrollExtent != _metrics.MaxScrollExtent;

    public override bool? HitTest(Point position)
    {
        ScrollbarGeometry? geometry = Geometry;
        if (!geometry.HasValue)
        {
            // There is nothing painted to hit.
            return null;
        }

        // Interaction disabled, the thumb is transparent, or the view is not scrollable.
        if (IgnorePointer || _fadeoutOpacityAnimation.Value == 0.0 || !LastMetricsAreScrollable)
        {
            return false;
        }

        return geometry.Value.TrackRect.Contains(position);
    }

    public bool HitTestInteractive(Point position, PointerDeviceKind kind, bool forHover = false)
    {
        ScrollbarGeometry? geometry = Geometry;
        if (!geometry.HasValue || IgnorePointer || !LastMetricsAreScrollable)
        {
            return false;
        }

        Rect interactiveRect = geometry.Value.TrackRect;
        Rect paddedRect = ExpandToInclude(
            interactiveRect,
            FromCircle(geometry.Value.ThumbRect.Center, KMinInteractiveSize / 2));

        // The scrollbar is not able to be hit when transparent - except when hovering with a mouse,
        // so the bar can be brought back into view before it is interacted with.
        if (_fadeoutOpacityAnimation.Value == 0.0)
        {
            return forHover && kind == PointerDeviceKind.Mouse && paddedRect.Contains(position);
        }

        return kind is PointerDeviceKind.Touch or PointerDeviceKind.Trackpad
            ? paddedRect.Contains(position)
            : interactiveRect.Contains(position);
    }

    public bool HitTestOnlyThumbInteractive(Point position, PointerDeviceKind kind)
    {
        ScrollbarGeometry? geometry = Geometry;
        if (!geometry.HasValue ||
            IgnorePointer ||
            _fadeoutOpacityAnimation.Value == 0.0 ||
            !LastMetricsAreScrollable)
        {
            return false;
        }

        Rect thumbRect = geometry.Value.ThumbRect;
        if (kind is PointerDeviceKind.Touch or PointerDeviceKind.Trackpad)
        {
            return ExpandToInclude(thumbRect, FromCircle(thumbRect.Center, KMinInteractiveSize / 2))
                .Contains(position);
        }

        return thumbRect.Contains(position);
    }

    /// <summary>
    /// The main-axis offset of the thumb inside the painted track, including the leading track
    /// offset. Mirrors Dart's private <c>_thumbOffset</c>, which <c>handleTrackTapDown</c> compares
    /// the tap against to decide which way to page.
    /// </summary>
    public double ThumbOffset =>
        Geometry is { } geometry ? geometry.TrackMainAxisStart + geometry.ThumbMainAxisOffset : 0;

    /// <summary>
    /// Converts a distance travelled along the thumb track into the matching scroll distance.
    /// </summary>
    /// <remarks>
    /// The <paramref name="thumbOffsetLocal"/> argument is a *delta* in the thumb track, not an
    /// absolute position: Dart's `getTrackToScroll` scales it by
    /// `scrollableExtent / (traversableTrackExtent - thumbExtent)`.
    /// </remarks>
    public double GetTrackToScroll(double thumbOffsetLocal)
    {
        ScrollbarGeometry geometry = Geometry
            ?? throw new InvalidOperationException("Scrollbar geometry is not available before update and paint.");
        if (_metrics is null || geometry.MaxThumbTravel <= 0)
        {
            return 0;
        }

        double scrollableExtent = _metrics.MaxScrollExtent - _metrics.MinScrollExtent;
        return scrollableExtent * thumbOffsetLocal / geometry.MaxThumbTravel;
    }

    /// <summary>The thumb's corresponding scroll offset in the track.</summary>
    public double GetThumbScrollOffset()
    {
        ScrollbarGeometry geometry = Geometry
            ?? throw new InvalidOperationException("Scrollbar geometry is not available before update and paint.");
        if (_metrics is null)
        {
            return 0;
        }

        double scrollableExtent = _metrics.MaxScrollExtent - _metrics.MinScrollExtent;
        double maxFraction = _metrics.MaxScrollExtent / scrollableExtent;
        double minFraction = _metrics.MinScrollExtent / scrollableExtent;
        double fractionPast = scrollableExtent > 0
            ? Math.Clamp(_metrics.Pixels / scrollableExtent, minFraction, maxFraction)
            : 0;
        return fractionPast * geometry.MaxThumbTravel;
    }

    public double GetScrollToTrack(double scrollOffset)
    {
        ScrollbarGeometry geometry = Geometry
            ?? throw new InvalidOperationException("Scrollbar geometry is not available before update and paint.");
        if (_metrics is null || _metrics.MaxScrollExtent <= _metrics.MinScrollExtent)
        {
            return 0;
        }

        double fraction = Math.Clamp(
            (scrollOffset - _metrics.MinScrollExtent) /
            (_metrics.MaxScrollExtent - _metrics.MinScrollExtent),
            0,
            1);
        if (geometry.IsReversed)
        {
            fraction = 1 - fraction;
        }
        return fraction * geometry.MaxThumbTravel;
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        if (oldDelegate is not ScrollbarPainter oldPainter)
        {
            return true;
        }

        return Color != oldPainter.Color ||
               TrackColor != oldPainter.TrackColor ||
               TrackBorderColor != oldPainter.TrackBorderColor ||
               TextDirection != oldPainter.TextDirection ||
               Thickness != oldPainter.Thickness ||
               !ReferenceEquals(FadeoutOpacityAnimation, oldPainter.FadeoutOpacityAnimation) ||
               MainAxisMargin != oldPainter.MainAxisMargin ||
               CrossAxisMargin != oldPainter.CrossAxisMargin ||
               Radius != oldPainter.Radius ||
               TrackRadius != oldPainter.TrackRadius ||
               !Equals(Shape, oldPainter.Shape) ||
               Padding != oldPainter.Padding ||
               MinLength != oldPainter.MinLength ||
               MinOverscrollLength != oldPainter.MinOverscrollLength ||
               ScrollbarOrientation != oldPainter.ScrollbarOrientation ||
               IgnorePointer != oldPainter.IgnorePointer;
    }

    public override void Dispose()
    {
        _fadeoutOpacityAnimation.RemoveListener(NotifyListeners);
        _repaint.Dispose();
        base.Dispose();
    }

    private void NotifyListeners() => _repaint.NotifyListeners();

    private ScrollbarGeometry? ComputeGeometry(Size size)
    {
        if (_metrics is null || !_axisDirection.HasValue ||
            _metrics.ViewportDimension <= 0 ||
            !double.IsFinite(_metrics.MaxScrollExtent) ||
            _metrics.MaxScrollExtent - _metrics.MinScrollExtent <= Constants.PrecisionErrorTolerance)
        {
            return null;
        }

        AxisDirection axisDirection = _axisDirection.Value;
        Axis axis = axisDirection is AxisDirection.Up or AxisDirection.Down ? Axis.Vertical : Axis.Horizontal;
        ScrollbarOrientation orientation = ScrollbarOrientation ?? (axis == Axis.Vertical
            ? TextDirection == global::Plumix.UI.TextDirection.Rtl
                ? global::Plumix.Widgets.ScrollbarOrientation.Left
                : global::Plumix.Widgets.ScrollbarOrientation.Right
            : global::Plumix.Widgets.ScrollbarOrientation.Bottom);
        bool verticalOrientation = orientation is global::Plumix.Widgets.ScrollbarOrientation.Left
            or global::Plumix.Widgets.ScrollbarOrientation.Right;
        if (verticalOrientation != (axis == Axis.Vertical))
        {
            throw new InvalidOperationException(
                $"Scrollbar orientation {orientation} is incompatible with axis direction {axisDirection}.");
        }

        bool reversed = axisDirection is AxisDirection.Up or AxisDirection.Left;
        double leadingPadding = axis == Axis.Vertical ? Padding.Top : Padding.Left;
        double trailingPadding = axis == Axis.Vertical ? Padding.Bottom : Padding.Right;
        double mainExtent = axis == Axis.Vertical ? size.Height : size.Width;
        double trackStart = leadingPadding + MainAxisMargin;
        double trackExtent = mainExtent - leadingPadding - trailingPadding - (2 * MainAxisMargin);
        if (trackExtent <= 0)
        {
            return null;
        }

        // Flutter's track rect spans the padded viewport; `mainAxisMargin` insets only the thumb.
        double trackRectStart = leadingPadding;
        double trackRectExtent = Math.Max(0, mainExtent - leadingPadding - trailingPadding);

        double leadingOverscroll = Math.Max(_metrics.MinScrollExtent - _metrics.Pixels, 0);
        double trailingOverscroll = Math.Max(_metrics.Pixels - _metrics.MaxScrollExtent, 0);
        double extentInside = Math.Max(0, _metrics.ViewportDimension - leadingOverscroll - trailingOverscroll);
        double extentBefore = Math.Max(_metrics.Pixels - _metrics.MinScrollExtent, 0);
        double extentAfter = Math.Max(_metrics.MaxScrollExtent - _metrics.Pixels, 0);
        double totalContentExtent = extentBefore + extentInside + extentAfter;
        double totalPadding = leadingPadding + trailingPadding;
        double fractionVisible = Math.Clamp(
            (extentInside - totalPadding) /
            Math.Max(Constants.PrecisionErrorTolerance, totalContentExtent - totalPadding),
            0,
            1);
        double candidateExtent = Math.Max(Math.Min(trackExtent, MinOverscrollLength), trackExtent * fractionVisible);
        double safeMinLength = Math.Min(MinLength, trackExtent);
        double overscrollFraction = Math.Clamp(1 - (extentInside / _metrics.ViewportDimension), 0, 0.2);
        double minimumExtent = extentBefore > 0 && extentAfter > 0
            ? safeMinLength
            : safeMinLength * (1 - (overscrollFraction / 0.2));
        double thumbExtent = Math.Clamp(candidateExtent, minimumExtent, trackExtent);
        double fraction = Math.Clamp(
            (_metrics.Pixels - _metrics.MinScrollExtent) /
            (_metrics.MaxScrollExtent - _metrics.MinScrollExtent),
            0,
            1);
        if (reversed)
        {
            fraction = 1 - fraction;
        }

        double thumbOffset = fraction * Math.Max(0, trackExtent - thumbExtent);
        Rect trackRect;
        Rect thumbRect;
        if (axis == Axis.Vertical)
        {
            double thumbX = orientation == global::Plumix.Widgets.ScrollbarOrientation.Left
                ? Padding.Left + CrossAxisMargin
                : size.Width - Padding.Right - CrossAxisMargin - Thickness;
            double trackX = orientation == global::Plumix.Widgets.ScrollbarOrientation.Left
                ? Padding.Left
                : thumbX - CrossAxisMargin;
            trackRect = new Rect(trackX, trackRectStart, Thickness + (2 * CrossAxisMargin), trackRectExtent);
            thumbRect = new Rect(thumbX, trackStart + thumbOffset, Thickness, thumbExtent);
        }
        else
        {
            double thumbY = orientation == global::Plumix.Widgets.ScrollbarOrientation.Top
                ? Padding.Top + CrossAxisMargin
                : size.Height - Padding.Bottom - CrossAxisMargin - Thickness;
            double trackY = orientation == global::Plumix.Widgets.ScrollbarOrientation.Top
                ? Padding.Top
                : thumbY - CrossAxisMargin;
            trackRect = new Rect(trackRectStart, trackY, trackRectExtent, Thickness + (2 * CrossAxisMargin));
            thumbRect = new Rect(trackStart + thumbOffset, thumbY, thumbExtent, Thickness);
        }

        return new ScrollbarGeometry(
            trackRect,
            thumbRect,
            axis,
            reversed,
            trackStart,
            trackExtent,
            thumbOffset,
            thumbExtent);
    }

    private (Point Start, Point End) TrackBorderLine(ScrollbarGeometry geometry)
    {
        ScrollbarOrientation orientation = ScrollbarOrientation ?? (geometry.Axis == Axis.Vertical
            ? TextDirection == global::Plumix.UI.TextDirection.Rtl
                ? global::Plumix.Widgets.ScrollbarOrientation.Left
                : global::Plumix.Widgets.ScrollbarOrientation.Right
            : global::Plumix.Widgets.ScrollbarOrientation.Bottom);
        return orientation switch
        {
            global::Plumix.Widgets.ScrollbarOrientation.Left =>
                (geometry.TrackRect.TopRight, geometry.TrackRect.BottomRight),
            global::Plumix.Widgets.ScrollbarOrientation.Top =>
                (geometry.TrackRect.BottomLeft, geometry.TrackRect.BottomRight),
            global::Plumix.Widgets.ScrollbarOrientation.Bottom =>
                (geometry.TrackRect.TopLeft, geometry.TrackRect.TopRight),
            _ => (geometry.TrackRect.TopLeft, geometry.TrackRect.BottomLeft),
        };
    }

    /// <summary>`Rect.fromCircle(center: center, radius: radius)`.</summary>
    private static Rect FromCircle(Point center, double radius) => new(
        center.X - radius,
        center.Y - radius,
        radius * 2,
        radius * 2);

    /// <summary>`Rect.expandToInclude(other)`.</summary>
    private static Rect ExpandToInclude(Rect rect, Rect other)
    {
        double left = Math.Min(rect.Left, other.Left);
        double top = Math.Min(rect.Top, other.Top);
        return new Rect(
            left,
            top,
            Math.Max(rect.Right, other.Right) - left,
            Math.Max(rect.Bottom, other.Bottom) - top);
    }

    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        NotifyListeners();
    }

    private static void ValidateNonNegative(string name, double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue)
        {
            ValidateNonNegative(name, value.Value);
        }
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)(color.A * opacity), 0, 255),
        color.R,
        color.G,
        color.B);
}
/// <summary>
/// An extendable base class for building scrollbars that fade in and out.
/// </summary>
/// <remarks>
/// To add a scrollbar to a <see cref="ScrollView"/>, like a <see cref="ListView"/> or a
/// <see cref="CustomScrollView"/>, wrap the scroll view widget in a <see cref="RawScrollbar"/>
/// widget.
/// </remarks>
public class RawScrollbar : StatefulWidget
{
    /// <summary>`scrollbar.dart`'s `_kMinThumbExtent`.</summary>
    public const double KMinThumbExtent = 18.0;

    /// <summary>`scrollbar.dart`'s `_kScrollbarThickness`.</summary>
    public const double KScrollbarThickness = 6.0;

    /// <summary>`scrollbar.dart`'s `_kScrollbarFadeDuration`.</summary>
    public static readonly TimeSpan KScrollbarFadeDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>`scrollbar.dart`'s `_kScrollbarTimeToFade`.</summary>
    public static readonly TimeSpan KScrollbarTimeToFade = TimeSpan.FromMilliseconds(600);

    /// <summary>Creates a basic raw scrollbar that wraps the given <paramref name="child"/>.</summary>
    public RawScrollbar(
        Widget child,
        ScrollController? controller = null,
        bool? thumbVisibility = null,
        ShapeBorder? shape = null,
        double? radius = null,
        double? thickness = null,
        Color? thumbColor = null,
        double minThumbLength = KMinThumbExtent,
        double? minOverscrollLength = null,
        bool? trackVisibility = null,
        double? trackRadius = null,
        Color? trackColor = null,
        Color? trackBorderColor = null,
        TimeSpan? fadeDuration = null,
        TimeSpan? timeToFade = null,
        TimeSpan? pressDuration = null,
        ScrollNotificationPredicate? notificationPredicate = null,
        bool? interactive = null,
        ScrollbarOrientation? scrollbarOrientation = null,
        double mainAxisMargin = 0.0,
        double crossAxisMargin = 0.0,
        Thickness? padding = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (thumbVisibility == false && (trackVisibility ?? false))
        {
            throw new ArgumentException("A scrollbar track cannot be drawn without a scrollbar thumb.");
        }

        ValidateNonNegative(nameof(minThumbLength), minThumbLength);
        if (minOverscrollLength is { } overscroll && overscroll > minThumbLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minOverscrollLength),
                "minOverscrollLength cannot exceed minThumbLength.");
        }

        ValidateNonNegative(nameof(minOverscrollLength), minOverscrollLength);
        if (radius is not null && shape is not null)
        {
            throw new ArgumentException("A scrollbar cannot carry both a radius and a shape.");
        }

        ValidateNonNegative(nameof(mainAxisMargin), mainAxisMargin);
        ValidateNonNegative(nameof(crossAxisMargin), crossAxisMargin);
        ValidatePositive(nameof(thickness), thickness);
        ValidateNonNegative(nameof(radius), radius);
        ValidateNonNegative(nameof(trackRadius), trackRadius);
        ValidateDuration(nameof(fadeDuration), fadeDuration);
        ValidateDuration(nameof(timeToFade), timeToFade);
        ValidateDuration(nameof(pressDuration), pressDuration);

        Child = child;
        Controller = controller;
        ThumbVisibility = thumbVisibility;
        Shape = shape;
        Radius = radius;
        Thickness = thickness;
        ThumbColor = thumbColor;
        MinThumbLength = minThumbLength;
        MinOverscrollLength = minOverscrollLength;
        TrackVisibility = trackVisibility;
        TrackRadius = trackRadius;
        TrackColor = trackColor;
        TrackBorderColor = trackBorderColor;
        FadeDuration = fadeDuration ?? KScrollbarFadeDuration;
        TimeToFade = timeToFade ?? KScrollbarTimeToFade;
        PressDuration = pressDuration ?? TimeSpan.Zero;
        NotificationPredicate = notificationPredicate ?? DefaultScrollNotificationPredicate;
        Interactive = interactive;
        ScrollbarOrientation = scrollbarOrientation;
        MainAxisMargin = mainAxisMargin;
        CrossAxisMargin = crossAxisMargin;
        Padding = padding;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>The <see cref="ScrollController"/> used to implement scrollbar dragging.</summary>
    public ScrollController? Controller { get; }

    /// <summary>Indicates that the scrollbar thumb should be visible, even when a scroll is not underway.</summary>
    public bool? ThumbVisibility { get; }

    /// <summary>The <see cref="ShapeBorder"/> of the scrollbar's thumb.</summary>
    public ShapeBorder? Shape { get; }

    /// <summary>The radius of the scrollbar thumb's rounded rectangle corners.</summary>
    public double? Radius { get; }

    /// <summary>The thickness of the scrollbar in the cross axis of the scrollable.</summary>
    public double? Thickness { get; }

    /// <summary>The color of the scrollbar thumb.</summary>
    public Color? ThumbColor { get; }

    /// <summary>
    /// The preferred smallest size the scrollbar thumb can shrink to when the total scrollable
    /// extent is large.
    /// </summary>
    public double MinThumbLength { get; }

    /// <summary>The preferred smallest size the scrollbar thumb can shrink to when viewport is overscrolled.</summary>
    public double? MinOverscrollLength { get; }

    /// <summary>Indicates that the scrollbar track should be visible.</summary>
    public bool? TrackVisibility { get; }

    /// <summary>The radius of the scrollbar track's rounded rectangle corners.</summary>
    public double? TrackRadius { get; }

    /// <summary>The color of the scrollbar track.</summary>
    public Color? TrackColor { get; }

    /// <summary>The color of the scrollbar track's border.</summary>
    public Color? TrackBorderColor { get; }

    /// <summary>The <see cref="Duration"/> of the fade animation.</summary>
    public TimeSpan FadeDuration { get; }

    /// <summary>The time to wait before the scrollbar fades out.</summary>
    public TimeSpan TimeToFade { get; }

    /// <summary>
    /// The duration of time that a LongPress will trigger the drag gesture of the scrollbar thumb.
    /// </summary>
    /// <remarks>
    /// Kept for source compatibility with Flutter: at the pinned revision `pressDuration` is
    /// declared on `RawScrollbar` and passed by `Scrollbar`/`CupertinoScrollbar`, but nothing in the
    /// framework reads it — the thumb is driven by drag recognizers with a zero touch slop.
    /// </remarks>
    public TimeSpan PressDuration { get; }

    /// <summary>A check that specifies whether a <see cref="ScrollNotification"/> should be handled.</summary>
    public ScrollNotificationPredicate NotificationPredicate { get; }

    /// <summary>
    /// Whether the scrollbar should be interactive and respond to dragging on the thumb, or
    /// tapping in the track area.
    /// </summary>
    public bool? Interactive { get; }

    /// <summary>Dictates the orientation of the scrollbar.</summary>
    public ScrollbarOrientation? ScrollbarOrientation { get; }

    /// <summary>
    /// Distance from the scrollbar thumb's start and end to the edge of the viewport in logical
    /// pixels.
    /// </summary>
    public double MainAxisMargin { get; }

    /// <summary>Distance from the scrollbar thumb's side to the nearest cross axis edge in logical pixels.</summary>
    public double CrossAxisMargin { get; }

    /// <summary>The insets by which the scrollbar thumb and track should be padded.</summary>
    public Thickness? Padding { get; }

    /// <summary>A <see cref="ScrollNotificationPredicate"/> that checks whether `notification.depth == 0`.</summary>
    public static bool DefaultScrollNotificationPredicate(ScrollNotification notification) =>
        notification.Depth == 0;

    public override State CreateState() => new RawScrollbarState<RawScrollbar>();

    private static void ValidateNonNegative(string name, double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue) ValidateNonNegative(name, value.Value);
    }

    private static void ValidatePositive(string name, double? value)
    {
        if (!value.HasValue) return;
        if (!double.IsFinite(value.Value) || value.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private static void ValidateDuration(string name, TimeSpan? value)
    {
        if (value.HasValue && value.Value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(name);
    }
}

/// <summary>
/// The state for a <see cref="RawScrollbar"/> widget, also shared by the <c>Scrollbar</c> and
/// <c>CupertinoScrollbar</c> widgets.
/// </summary>
/// <remarks>
/// Controls the animation that fades a scrollbar's thumb in and out of view, and provides the
/// default gestures for dragging the scrollbar thumb and tapping on the scrollbar track.
/// </remarks>
public class RawScrollbarState<T> : State where T : RawScrollbar
{
    private readonly GlobalKey _scrollbarPainterKey =
        new LabeledGlobalKey<State>("RawScrollbar painter");

    private readonly GlobalKey<RawGestureDetectorState> _gestureDetectorKey =
        new LabeledGlobalKey<RawGestureDetectorState>("RawScrollbar gestures");

    private Point? _startDragScrollbarAxisOffset;
    private Point? _lastDragUpdateOffset;
    private double? _startDragThumbOffset;
    private ScrollController? _cachedController;
    private GestureTimer? _fadeoutTimer;
    private AnimationController _fadeoutAnimationController = null!;
    private CurvedAnimation _fadeoutOpacityAnimation = null!;
    private bool _hoverIsActive;
    private IDrag? _thumbDrag;
    private bool _maxScrollExtentPermitsScrolling;
    private IScrollHoldController? _thumbHold;
    private Axis? _axis;

    /// <summary>The widget this state is for, typed to the concrete scrollbar subclass.</summary>
    protected T CurrentWidget => (T)StateWidget;

    private ScrollController? EffectiveScrollController =>
        CurrentWidget.Controller ?? PrimaryScrollController.MaybeOf(Context);

    /// <summary>
    /// Used to paint the scrollbar. Can be customized by subclasses by overriding
    /// <see cref="UpdateScrollbarPainter"/>.
    /// </summary>
    protected ScrollbarPainter ScrollbarPainter { get; private set; } = null!;

    /// <summary>
    /// Overridable getter to indicate that the scrollbar should be visible, even when a scroll is
    /// not underway. Defaults to false when <see cref="RawScrollbar.ThumbVisibility"/> is null.
    /// </summary>
    protected virtual bool ShowScrollbar => CurrentWidget.ThumbVisibility ?? false;

    private bool ShowTrack => ShowScrollbar && (CurrentWidget.TrackVisibility ?? false);

    /// <summary>
    /// Overridable getter to indicate whether gestures should be enabled on the scrollbar. When
    /// false the scrollbar does not respond to gesture or hover events and allows clicks through.
    /// Defaults to true when <see cref="RawScrollbar.Interactive"/> is null.
    /// </summary>
    protected virtual bool EnableGestures => CurrentWidget.Interactive ?? true;

    public override void InitState()
    {
        base.InitState();
        _fadeoutAnimationController = new AnimationController(
            duration: CurrentWidget.FadeDuration,
            vsync: this);
        _fadeoutAnimationController.AddStatusListener(ValidateInteractions);
        _fadeoutOpacityAnimation = new CurvedAnimation(
            parent: _fadeoutAnimationController,
            curve: Curves.FastOutSlowIn);
        ScrollbarPainter = new ScrollbarPainter(
            color: CurrentWidget.ThumbColor ?? DefaultThumbColor,
            fadeoutOpacityAnimation: _fadeoutOpacityAnimation,
            thickness: CurrentWidget.Thickness ?? RawScrollbar.KScrollbarThickness,
            radius: CurrentWidget.Radius,
            trackRadius: CurrentWidget.TrackRadius,
            scrollbarOrientation: CurrentWidget.ScrollbarOrientation,
            mainAxisMargin: CurrentWidget.MainAxisMargin,
            shape: CurrentWidget.Shape,
            crossAxisMargin: CurrentWidget.CrossAxisMargin,
            minLength: CurrentWidget.MinThumbLength,
            minOverscrollLength: CurrentWidget.MinOverscrollLength ?? CurrentWidget.MinThumbLength);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        ScheduleCheckHasValidScrollPosition();
    }

    private void ScheduleCheckHasValidScrollPosition()
    {
        if (!ShowScrollbar)
        {
            return;
        }

        global::Plumix.Scheduler.AddPostFrameCallback(_ => CheckHasValidScrollPosition());
    }

    private void ValidateInteractions(AnimationStatus status)
    {
        if (status == AnimationStatus.Dismissed)
        {
            // We do not check for a valid scroll position if the scrollbar is not visible, because
            // it cannot be interacted with.
            return;
        }

        if (EffectiveScrollController is null || !EnableGestures)
        {
            return;
        }

        // Don't check immediately while the widget is still updating: the controller may not be
        // attached yet in that frame, and `ScheduleCheckHasValidScrollPosition` already covers it.
        if (_fadeoutAnimationController.Status == AnimationStatus.Forward &&
            (CurrentWidget.ThumbVisibility ?? false))
        {
            return;
        }

        CheckHasValidScrollPosition();
    }

    private void CheckHasValidScrollPosition()
    {
        if (!Mounted)
        {
            return;
        }

        ScrollController? scrollController = EffectiveScrollController;
        bool tryPrimary = CurrentWidget.Controller is null;
        string controllerForError = tryPrimary ? "PrimaryScrollController" : "provided ScrollController";
        string when = (CurrentWidget.ThumbVisibility ?? false)
            ? "Scrollbar.thumbVisibility is true"
            : EnableGestures
                ? "the scrollbar is interactive"
                : "using the Scrollbar";

        if (scrollController is null)
        {
            throw new InvalidOperationException($"A ScrollController is required when {when}.");
        }

        if (!scrollController.HasClients)
        {
            throw new InvalidOperationException(
                "The Scrollbar's ScrollController has no ScrollPosition attached. " +
                $"The Scrollbar attempted to use the {controllerForError}. This ScrollController " +
                "should be associated with the ScrollView that the Scrollbar is being applied to.");
        }

        if (scrollController.Positions.Count > 1)
        {
            throw new InvalidOperationException(
                $"The {controllerForError} is attached to more than one ScrollPosition. " +
                "The Scrollbar requires a single ScrollPosition in order to be painted.");
        }
    }

    /// <summary>
    /// Configures the <see cref="ScrollbarPainter"/> from the widget's properties and any inherited
    /// widgets the painter depends on, like <see cref="Directionality"/> and <see cref="MediaQuery"/>.
    /// Subclasses override this to configure the painter.
    /// </summary>
    protected virtual void UpdateScrollbarPainter()
    {
        TextDirection textDirection = Directionality.Of(Context);
        ScrollbarPainter.Color = CurrentWidget.ThumbColor ?? DefaultThumbColor;
        ScrollbarPainter.TrackRadius = CurrentWidget.TrackRadius;
        ScrollbarPainter.TrackColor = ShowTrack
            ? CurrentWidget.TrackColor ?? DefaultTrackColor
            : TransparentColor;
        ScrollbarPainter.TrackBorderColor = ShowTrack
            ? CurrentWidget.TrackBorderColor ?? DefaultTrackBorderColor
            : TransparentColor;
        ScrollbarPainter.TextDirection = textDirection;
        ScrollbarPainter.Thickness = CurrentWidget.Thickness ?? RawScrollbar.KScrollbarThickness;
        ScrollbarPainter.Radius = CurrentWidget.Radius;
        // Flutter reads `MediaQuery.paddingOf` unconditionally because every Flutter tree carries a
        // `View`-provided `MediaQuery`; Plumix trees need not, and `ScrollBehavior.BuildScrollbar`
        // may wrap any scrollable, so a missing one resolves to zero padding.
        ScrollbarPainter.Padding = CurrentWidget.Padding ?? MediaQuery.MaybePaddingOf(Context) ?? default;
        ScrollbarPainter.ScrollbarOrientation = CurrentWidget.ScrollbarOrientation;
        ScrollbarPainter.MainAxisMargin = CurrentWidget.MainAxisMargin;
        ScrollbarPainter.Shape = CurrentWidget.Shape;
        ScrollbarPainter.CrossAxisMargin = CurrentWidget.CrossAxisMargin;
        ScrollbarPainter.MinLength = CurrentWidget.MinThumbLength;
        ScrollbarPainter.MinOverscrollLength = CurrentWidget.MinOverscrollLength ?? CurrentWidget.MinThumbLength;
        ScrollbarPainter.IgnorePointer = !EnableGestures;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (RawScrollbar)oldWidget;
        if (CurrentWidget.ThumbVisibility != old.ThumbVisibility)
        {
            if (CurrentWidget.ThumbVisibility ?? false)
            {
                ScheduleCheckHasValidScrollPosition();
                _fadeoutTimer?.Cancel();
                _fadeoutAnimationController.AnimateTo(1.0);
            }
            else
            {
                _fadeoutAnimationController.Reverse();
            }
        }
    }

    private void MaybeStartFadeoutTimer()
    {
        if (ShowScrollbar)
        {
            return;
        }

        _fadeoutTimer?.Cancel();
        _fadeoutTimer = GestureTimer.Start(CurrentWidget.TimeToFade, () =>
        {
            _fadeoutAnimationController.Reverse();
            _fadeoutTimer = null;
        });
    }

    /// <summary>
    /// The <see cref="Axis"/> of the child scroll view, or null if no <see cref="ScrollMetrics"/>
    /// notification has been seen yet.
    /// </summary>
    protected Axis? GetScrollbarDirection() => _axis;

    private void DisposeThumbDrag() => _thumbDrag = null;

    private void DisposeThumbHold() => _thumbHold = null;

    // Given the drag's localPosition (see HandleThumbPressUpdate) compute the scroll position delta
    // in the scroll axis direction. Deals with the complications arising from scroll metrics changes
    // that have occurred since the last drag update and the need to prevent overscrolling on some
    // platforms.
    private double? GetPrimaryDelta(Point localPosition)
    {
        ScrollPosition position = _cachedController!.Position;
        double primaryDeltaFromDragStart;
        double primaryDeltaFromLastDragUpdate;
        switch (position.AxisDirection)
        {
            case AxisDirection.Up:
                primaryDeltaFromDragStart = _startDragScrollbarAxisOffset!.Value.Y - localPosition.Y;
                primaryDeltaFromLastDragUpdate = _lastDragUpdateOffset!.Value.Y - localPosition.Y;
                break;
            case AxisDirection.Right:
                primaryDeltaFromDragStart = localPosition.X - _startDragScrollbarAxisOffset!.Value.X;
                primaryDeltaFromLastDragUpdate = localPosition.X - _lastDragUpdateOffset!.Value.X;
                break;
            case AxisDirection.Down:
                primaryDeltaFromDragStart = localPosition.Y - _startDragScrollbarAxisOffset!.Value.Y;
                primaryDeltaFromLastDragUpdate = localPosition.Y - _lastDragUpdateOffset!.Value.Y;
                break;
            default:
                primaryDeltaFromDragStart = _startDragScrollbarAxisOffset!.Value.X - localPosition.X;
                primaryDeltaFromLastDragUpdate = _lastDragUpdateOffset!.Value.X - localPosition.X;
                break;
        }

        // Convert primaryDelta, the amount that the scrollbar moved since the last time when drag
        // started or last updated, into the coordinate space of the scroll position.
        double scrollOffsetGlobal = ScrollbarPainter.GetTrackToScroll(
            _startDragThumbOffset!.Value + primaryDeltaFromDragStart);

        if ((primaryDeltaFromDragStart > 0 && scrollOffsetGlobal < position.Pixels) ||
            (primaryDeltaFromDragStart < 0 && scrollOffsetGlobal > position.Pixels))
        {
            // Adjust the position value if the scrolling direction conflicts with the dragging
            // direction due to scroll metrics shrink.
            scrollOffsetGlobal =
                position.Pixels + ScrollbarPainter.GetTrackToScroll(primaryDeltaFromLastDragUpdate);
        }

        if (scrollOffsetGlobal == position.Pixels)
        {
            return null;
        }

        // Ensure we don't drag into overscroll if the physics do not allow it.
        double physicsAdjustment = position.Physics.ApplyBoundaryConditions(position, scrollOffsetGlobal);
        double newPosition = scrollOffsetGlobal - physicsAdjustment;

        // The physics may allow overscroll when actually *scrolling*, but dragging on the scrollbar
        // does not always allow us to enter overscroll.
        switch (ScrollConfiguration.Of(Context).GetPlatform(Context))
        {
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.MacOS:
            case TargetPlatform.Windows:
                newPosition = Math.Clamp(newPosition, position.MinScrollExtent, position.MaxScrollExtent);
                break;
            case TargetPlatform.IOS:
            case TargetPlatform.Android:
                // We can only drag the scrollbar into overscroll on mobile platforms, and only then
                // if the physics allow it.
                break;
        }

        bool isReversed = ScrollDirectionUtils.AxisDirectionIsReversed(position.AxisDirection);
        return isReversed ? newPosition - position.Pixels : position.Pixels - newPosition;
    }

    /// <summary>
    /// Handler called when a press on the scrollbar thumb has been recognized. Cancels the timer
    /// associated with the fade animation of the scrollbar.
    /// </summary>
    protected virtual void HandleThumbPress()
    {
        CheckHasValidScrollPosition();
        _cachedController = EffectiveScrollController;
        if (GetScrollbarDirection() is null)
        {
            return;
        }

        _fadeoutTimer?.Cancel();
        _thumbHold = _cachedController!.Position.Hold(DisposeThumbHold);
    }

    /// <summary>
    /// Handler called when the thumb drag has started. Begins the fade in animation and creates the
    /// thumb's <see cref="IDrag"/>.
    /// </summary>
    protected virtual void HandleThumbPressStart(Point localPosition)
    {
        CheckHasValidScrollPosition();
        Axis? direction = GetScrollbarDirection();
        if (direction is null)
        {
            return;
        }

        _fadeoutTimer?.Cancel();
        _fadeoutAnimationController.Forward();

        ScrollPosition position = _cachedController!.Position;
        var renderBox = (RenderBox)_scrollbarPainterKey.CurrentContext!.Value.FindRenderObject()!;
        var details = new DragStartDetails(
            GlobalPosition: renderBox.LocalToGlobal(localPosition),
            LocalPosition: localPosition);
        _thumbDrag = position.Drag(details, DisposeThumbDrag);

        _startDragScrollbarAxisOffset = localPosition;
        _lastDragUpdateOffset = localPosition;
        _startDragThumbOffset = ScrollbarPainter.GetThumbScrollOffset();
    }

    /// <summary>
    /// Handler called when a currently active thumb drag moves. Updates the position of the child
    /// scrollable through the thumb's <see cref="IDrag"/>.
    /// </summary>
    protected virtual void HandleThumbPressUpdate(Point localPosition)
    {
        CheckHasValidScrollPosition();
        if (_lastDragUpdateOffset == localPosition)
        {
            return;
        }

        ScrollPosition position = _cachedController!.Position;
        if (!position.Physics.ShouldAcceptUserOffset(position))
        {
            return;
        }

        Axis? direction = GetScrollbarDirection();
        if (direction is null)
        {
            return;
        }

        // The drag might be null if the drag activity ended and called DisposeThumbDrag.
        if (_thumbDrag is null)
        {
            return;
        }

        double? primaryDelta = GetPrimaryDelta(localPosition);
        if (primaryDelta is null)
        {
            return;
        }

        Point delta = direction == Axis.Horizontal
            ? new Point(primaryDelta.Value, 0)
            : new Point(0, primaryDelta.Value);
        var renderBox = (RenderBox)_scrollbarPainterKey.CurrentContext!.Value.FindRenderObject()!;

        // Triggers updates to the ScrollPosition and the ScrollbarPainter.
        _thumbDrag.Update(new DragUpdateDetails(
            GlobalPosition: renderBox.LocalToGlobal(localPosition),
            LocalPosition: localPosition,
            Delta: delta,
            PrimaryDelta: primaryDelta.Value));

        _lastDragUpdateOffset = localPosition;
    }

    /// <summary>Handler called when a thumb drag has ended.</summary>
    protected virtual void HandleThumbPressEnd(Point localPosition, Velocity velocity)
    {
        CheckHasValidScrollPosition();
        Axis? direction = GetScrollbarDirection();
        if (direction is null)
        {
            return;
        }

        MaybeStartFadeoutTimer();
        _cachedController = null;
        _lastDragUpdateOffset = null;

        // The drag might be null if the drag activity ended and called DisposeThumbDrag.
        if (_thumbDrag is null)
        {
            return;
        }

        // On mobile platforms flinging the scrollbar thumb causes a ballistic scroll, just like it
        // does via a touch drag. Likewise for desktops when dragging on the trackpad or with a stylus.
        TargetPlatform platform = ScrollConfiguration.Of(Context).GetPlatform(Context);
        Velocity adjustedVelocity = platform is TargetPlatform.IOS or TargetPlatform.Android
            ? -velocity
            : Velocity.Zero;
        var renderBox = (RenderBox)_scrollbarPainterKey.CurrentContext!.Value.FindRenderObject()!;
        var details = new DragEndDetails(
            velocity: adjustedVelocity,
            primaryVelocity: direction == Axis.Horizontal
                ? adjustedVelocity.PixelsPerSecond.X
                : adjustedVelocity.PixelsPerSecond.Y,
            globalPosition: renderBox.LocalToGlobal(localPosition),
            localPosition: localPosition);

        _thumbDrag?.End(details);

        _startDragScrollbarAxisOffset = null;
        _lastDragUpdateOffset = null;
        _startDragThumbOffset = null;
        _cachedController = null;
    }

    /// <summary>
    /// Handler called when the track is tapped in order to page in the tapped direction.
    /// </summary>
    protected virtual void HandleTrackTapDown(TapDownDetails details)
    {
        // The Scrollbar should page towards the position of the tap on the track.
        CheckHasValidScrollPosition();
        _cachedController = EffectiveScrollController;

        ScrollPosition position = _cachedController!.Position;
        if (!position.Physics.ShouldAcceptUserOffset(position))
        {
            return;
        }

        AxisDirection scrollDirection;
        if (ScrollDirectionUtils.AxisDirectionToAxis(position.AxisDirection) == Axis.Vertical)
        {
            scrollDirection = details.LocalPosition.Y > ScrollbarPainter.ThumbOffset
                ? AxisDirection.Down
                : AxisDirection.Up;
        }
        else
        {
            scrollDirection = details.LocalPosition.X > ScrollbarPainter.ThumbOffset
                ? AxisDirection.Right
                : AxisDirection.Left;
        }

        Scrollable.ScrollableState? state = Scrollable.MaybeOf(position.Context.NotificationContext!.Value);
        var intent = new ScrollIntent(direction: scrollDirection, type: ScrollIncrementType.Page);
        if (state is null)
        {
            return;
        }

        double scrollIncrement = ScrollAction.GetDirectionalIncrement(state, intent);
        position.MoveTo(
            position.Pixels + scrollIncrement,
            duration: TimeSpan.FromMilliseconds(100),
            curve: Curves.EaseInOut);
    }

    // ScrollController takes precedence over ScrollNotification.
    private bool ShouldUpdatePainter(Axis notificationAxis)
    {
        ScrollController? scrollController = EffectiveScrollController;

        // We do not have a scroll controller dictating axis.
        if (scrollController is null)
        {
            return true;
        }

        // Has more than one attached position.
        if (scrollController.Positions.Count > 1)
        {
            return false;
        }

        // The scroll controller is not attached to a position, or the notification matches the
        // scroll controller's axis.
        return !scrollController.HasClients ||
               scrollController.Position.Axis == notificationAxis;
    }

    private bool HandleScrollMetricsNotification(ScrollMetricsNotification notification)
    {
        if (!CurrentWidget.NotificationPredicate(notification.AsScrollUpdate()))
        {
            return false;
        }

        if (ShowScrollbar && !_fadeoutAnimationController.Status.IsForwardOrCompleted())
        {
            _fadeoutAnimationController.Forward();
        }

        IScrollMetrics metrics = notification.Metrics;
        if (ShouldUpdatePainter(metrics.Axis))
        {
            ScrollbarPainter.Update(metrics, metrics.AxisDirection);
        }

        if (metrics.Axis != _axis)
        {
            SetState(() => _axis = metrics.Axis);
        }

        if (_maxScrollExtentPermitsScrolling != metrics.MaxScrollExtent > 0.0)
        {
            SetState(() => _maxScrollExtentPermitsScrolling = !_maxScrollExtentPermitsScrolling);
        }

        return false;
    }

    private bool HandleScrollNotification(ScrollNotification notification)
    {
        if (!CurrentWidget.NotificationPredicate(notification))
        {
            return false;
        }

        IScrollMetrics metrics = notification.Metrics;
        if (metrics.MaxScrollExtent <= metrics.MinScrollExtent)
        {
            // Hide the bar when the Scrollable widget has no space to scroll.
            if (_fadeoutAnimationController.Status.IsForwardOrCompleted())
            {
                _fadeoutAnimationController.Reverse();
            }

            if (ShouldUpdatePainter(metrics.Axis))
            {
                ScrollbarPainter.Update(metrics, metrics.AxisDirection);
            }

            return false;
        }

        if (notification is ScrollUpdateNotification or OverscrollNotification)
        {
            // Any movement always makes the scrollbar start showing up.
            if (!_fadeoutAnimationController.Status.IsForwardOrCompleted())
            {
                _fadeoutAnimationController.Forward();
            }

            _fadeoutTimer?.Cancel();

            if (ShouldUpdatePainter(metrics.Axis))
            {
                ScrollbarPainter.Update(metrics, metrics.AxisDirection);
            }
        }
        else if (notification is ScrollEndNotification)
        {
            if (_thumbDrag is null)
            {
                MaybeStartFadeoutTimer();
            }
        }

        return false;
    }

    private void HandleThumbDragDown(DragDownDetails details) => HandleThumbPress();

    // The protected RawScrollbar API methods - HandleThumbPressStart, HandleThumbPressUpdate,
    // HandleThumbPressEnd - all depend on a localPosition parameter that defines the event's location
    // relative to the scrollbar. Ensure that the localPosition is reported consistently, even if the
    // source of the event is a trackpad or a stylus.
    private Point GlobalToScrollbar(Point offset)
    {
        var renderBox = (RenderBox)_scrollbarPainterKey.CurrentContext!.Value.FindRenderObject()!;
        return renderBox.GlobalToLocal(offset);
    }

    private void HandleThumbDragStart(DragStartDetails details) =>
        HandleThumbPressStart(GlobalToScrollbar(details.GlobalPosition));

    private void HandleThumbDragUpdate(DragUpdateDetails details) =>
        HandleThumbPressUpdate(GlobalToScrollbar(details.GlobalPosition));

    private void HandleThumbDragEnd(DragEndDetails details) =>
        HandleThumbPressEnd(GlobalToScrollbar(details.GlobalPosition), details.Velocity);

    private void HandleThumbDragCancel()
    {
        if (_gestureDetectorKey.CurrentContext is null)
        {
            // The cancel was caused by the gesture detector getting disposed, which means we will
            // get disposed momentarily as well and shouldn't do any work.
            return;
        }

        _thumbHold?.Cancel();
        _thumbDrag?.Cancel();
    }

    private void InitThumbDragGestureRecognizer(DragGestureRecognizer instance)
    {
        instance.OnDown = HandleThumbDragDown;
        instance.OnStart = HandleThumbDragStart;
        instance.OnUpdate = HandleThumbDragUpdate;
        instance.OnEnd = HandleThumbDragEnd;
        instance.OnCancel = HandleThumbDragCancel;
        instance.GestureSettings = new DeviceGestureSettings(TouchSlop: 0);
        instance.DragStartBehavior = DragStartBehavior.Down;
    }

    private bool CanHandleScrollGestures()
    {
        ScrollController? controller = EffectiveScrollController;
        return EnableGestures &&
               controller is not null &&
               controller.Positions.Count == 1 &&
               controller.Position.HasContentDimensions &&
               controller.Position.MaxScrollExtent - controller.Position.MinScrollExtent >
               Constants.PrecisionErrorTolerance;
    }

    /// <summary>The recognizers the scrollbar installs, keyed by recognizer type as in Flutter.</summary>
    internal IReadOnlyDictionary<Type, IGestureRecognizerFactory> Gestures
    {
        get
        {
            var gestures = new Dictionary<Type, IGestureRecognizerFactory>();
            if (!CanHandleScrollGestures())
            {
                return gestures;
            }

            if (EffectiveScrollController!.Position.Axis == Axis.Horizontal)
            {
                gestures[typeof(HorizontalThumbDragGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<HorizontalThumbDragGestureRecognizer>(
                        () => new HorizontalThumbDragGestureRecognizer(_scrollbarPainterKey),
                        InitThumbDragGestureRecognizer);
            }
            else
            {
                gestures[typeof(VerticalThumbDragGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<VerticalThumbDragGestureRecognizer>(
                        () => new VerticalThumbDragGestureRecognizer(_scrollbarPainterKey),
                        InitThumbDragGestureRecognizer);
            }

            gestures[typeof(TrackTapGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<TrackTapGestureRecognizer>(
                    () => new TrackTapGestureRecognizer(_scrollbarPainterKey),
                    instance => instance.OnTapDown = HandleTrackTapDown);

            return gestures;
        }
    }

    /// <summary>
    /// Whether the given offset is located over the track of the scrollbar, excluding its thumb.
    /// </summary>
    protected bool IsPointerOverTrack(Point position, PointerDeviceKind kind)
    {
        if (_scrollbarPainterKey.CurrentContext is null)
        {
            return false;
        }

        Point localOffset = GlobalToScrollbar(position);
        return ScrollbarPainter.HitTestInteractive(localOffset, kind) &&
               !ScrollbarPainter.HitTestOnlyThumbInteractive(localOffset, kind);
    }

    /// <summary>Whether the given offset is located over the thumb of the scrollbar.</summary>
    protected bool IsPointerOverThumb(Point position, PointerDeviceKind kind)
    {
        if (_scrollbarPainterKey.CurrentContext is null)
        {
            return false;
        }

        return ScrollbarPainter.HitTestOnlyThumbInteractive(GlobalToScrollbar(position), kind);
    }

    /// <summary>
    /// Whether the given offset is located over the track or thumb of the scrollbar. The hit test
    /// area for a hovering mouse is larger than regular hit testing, to make the scrollbar easier to
    /// reach; that larger area is always used here, exactly as in Flutter.
    /// </summary>
    protected bool IsPointerOverScrollbar(Point position, PointerDeviceKind kind, bool forHover = false)
    {
        if (_scrollbarPainterKey.CurrentContext is null)
        {
            return false;
        }

        return ScrollbarPainter.HitTestInteractive(GlobalToScrollbar(position), kind, forHover: true);
    }

    /// <summary>
    /// Cancels the fade out animation so the scrollbar will remain visible for interaction. Can be
    /// overridden by subclasses to respond to a <see cref="PointerHoverEvent"/>.
    /// </summary>
    protected virtual void HandleHover(PointerHoverEvent @event)
    {
        // Check if the position of the pointer falls over the painted scrollbar.
        if (IsPointerOverScrollbar(@event.Position, @event.Kind, forHover: true))
        {
            _hoverIsActive = true;
            // Bring the scrollbar back into view if it has faded or started to fade away.
            _fadeoutAnimationController.Forward();
            _fadeoutTimer?.Cancel();
        }
        else if (_hoverIsActive)
        {
            // Pointer is not over the painted scrollbar.
            _hoverIsActive = false;
            MaybeStartFadeoutTimer();
        }
    }

    /// <summary>
    /// Initiates the fade out animation. Can be overridden by subclasses to respond to a
    /// <see cref="PointerExitEvent"/>.
    /// </summary>
    protected virtual void HandleHoverExit(PointerExitEvent @event)
    {
        _hoverIsActive = false;
        MaybeStartFadeoutTimer();
    }

    // Returns the delta that should result from applying the event with axis and direction taken
    // into account.
    private double PointerSignalEventDelta(PointerScrollEvent @event)
    {
        ScrollPosition position = _cachedController!.Position;
        double delta = position.Axis == Axis.Horizontal
            ? @event.ScrollDelta.X
            : @event.ScrollDelta.Y;

        if (ScrollDirectionUtils.AxisDirectionIsReversed(position.AxisDirection))
        {
            delta *= -1;
        }

        return delta;
    }

    // Returns the offset that should result from applying the event to the current position, taking
    // min/max scroll extent into account.
    private double TargetScrollOffsetForPointerScroll(double delta)
    {
        ScrollPosition position = _cachedController!.Position;
        return Math.Min(
            Math.Max(position.Pixels + delta, position.MinScrollExtent),
            position.MaxScrollExtent);
    }

    private void HandlePointerScroll(PointerSignalEvent @event)
    {
        _cachedController = EffectiveScrollController;
        double delta = PointerSignalEventDelta((PointerScrollEvent)@event);
        double targetScrollOffset = TargetScrollOffsetForPointerScroll(delta);
        if (delta != 0.0 && targetScrollOffset != _cachedController!.Position.Pixels)
        {
            _cachedController.Position.ApplyPointerScrollDelta(delta);
        }
    }

    private void ReceivedPointerSignal(PointerSignalEvent @event)
    {
        _cachedController = EffectiveScrollController;

        // Only try to scroll if the bar absorbs the hit test.
        if ((ScrollbarPainter.HitTest(@event.LocalPosition) ?? false) &&
            _cachedController is not null &&
            _cachedController.HasClients &&
            (_thumbDrag is null || PlatformDefaults.IsWeb))
        {
            ScrollPosition position = _cachedController.Position;
            switch (@event)
            {
                case PointerScrollEvent scroll:
                {
                    if (!position.Physics.ShouldAcceptUserOffset(position))
                    {
                        return;
                    }

                    double delta = PointerSignalEventDelta(scroll);
                    double targetScrollOffset = TargetScrollOffsetForPointerScroll(delta);
                    if (delta != 0.0 && targetScrollOffset != position.Pixels)
                    {
                        GestureBinding.Instance.PointerSignalResolver.Register(scroll, HandlePointerScroll);
                    }

                    break;
                }
                case PointerScrollInertiaCancelEvent:
                    position.JumpTo(position.Pixels);
                    // Don't use the pointer signal resolver, all hit-tested scrollables should stop.
                    break;
            }
        }
    }

    public override void Dispose()
    {
        _fadeoutAnimationController.Dispose();
        _fadeoutTimer?.Cancel();
        ScrollbarPainter.Dispose();
        _fadeoutOpacityAnimation.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        UpdateScrollbarPainter();

        return new NotificationListener<ScrollMetricsNotification>(
            onNotification: HandleScrollMetricsNotification,
            child: new NotificationListener<ScrollNotification>(
                onNotification: HandleScrollNotification,
                child: new RepaintBoundary(
                    child: new Listener(
                        onPointerSignal: ReceivedPointerSignal,
                        child: new RawGestureDetector(
                            key: _gestureDetectorKey,
                            gestures: Gestures,
                            child: new MouseRegion(
                                onExit: @event =>
                                {
                                    if (@event.Kind is PointerDeviceKind.Mouse or PointerDeviceKind.Trackpad &&
                                        EnableGestures)
                                    {
                                        HandleHoverExit(@event);
                                    }
                                },
                                onHover: @event =>
                                {
                                    if (@event.Kind is PointerDeviceKind.Mouse or PointerDeviceKind.Trackpad &&
                                        EnableGestures)
                                    {
                                        HandleHover(@event);
                                    }
                                },
                                child: new CustomPaint(
                                    key: _scrollbarPainterKey,
                                    foregroundPainter: ScrollbarPainter,
                                    child: new RepaintBoundary(child: CurrentWidget.Child))))))));
    }

    private static Color DefaultThumbColor => Color.FromArgb(0x66, 0xBC, 0xBC, 0xBC);

    private static Color DefaultTrackColor => Color.FromArgb(0x08, 0x00, 0x00, 0x00);

    private static Color DefaultTrackBorderColor => Color.FromArgb(0x1A, 0x00, 0x00, 0x00);

    private static Color TransparentColor => Color.FromArgb(0x00, 0x00, 0x00, 0x00);
}

/// <summary>Maps a <see cref="ShapeBorder"/> onto the corner radius the thumb is painted with.</summary>
internal static class ScrollbarShapeGeometry
{
    public static double? Radius(ShapeBorder? shape)
    {
        return shape switch
        {
            RoundedRectangleBorder rounded => rounded.BorderRadius.Resolve(TextDirection.Ltr).Radius,
            _ => null,
        };
    }
}

/// <summary>
/// Reads the <see cref="ScrollbarPainter"/> off the <see cref="CustomPaint"/> the scrollbar's global
/// key is attached to, so a recognizer can hit-test against exactly what was painted.
/// </summary>
internal static class ScrollbarHitTest
{
    private static ScrollbarPainter? Painter(GlobalKey customPaintKey) =>
        (customPaintKey.CurrentWidget as CustomPaint)?.ForegroundPainter as ScrollbarPainter;

    private static Point? LocalOffset(GlobalKey customPaintKey, Point position) =>
        customPaintKey.CurrentContext?.FindRenderObject() is RenderBox box
            ? box.GlobalToLocal(position)
            : null;

    public static bool IsThumbEvent(GlobalKey customPaintKey, PointerEvent @event)
    {
        if (Painter(customPaintKey) is not { } painter ||
            LocalOffset(customPaintKey, @event.Position) is not { } localOffset)
        {
            return false;
        }

        return painter.HitTestOnlyThumbInteractive(localOffset, @event.Kind);
    }

    public static bool IsTrackEvent(GlobalKey customPaintKey, PointerEvent @event)
    {
        if (Painter(customPaintKey) is not { } painter ||
            LocalOffset(customPaintKey, @event.Position) is not { } localOffset)
        {
            return false;
        }

        return painter.HitTestInteractive(localOffset, @event.Kind) &&
               !painter.HitTestOnlyThumbInteractive(localOffset, @event.Kind);
    }
}

/// <summary>A tap recognizer that only claims pointers landing on the scrollbar track.</summary>
internal sealed class TrackTapGestureRecognizer(GlobalKey customPaintKey) : TapGestureRecognizer
{
    private readonly GlobalKey _customPaintKey = customPaintKey;

    protected override bool IsPointerAllowed(PointerDownEvent @event) =>
        ScrollbarHitTest.IsTrackEvent(_customPaintKey, @event) && base.IsPointerAllowed(@event);
}

/// <summary>A vertical drag recognizer that only claims pointers landing on the scrollbar thumb.</summary>
internal sealed class VerticalThumbDragGestureRecognizer(GlobalKey customPaintKey)
    : VerticalDragGestureRecognizer
{
    private readonly GlobalKey _customPaintKey = customPaintKey;

    protected override bool IsPointerAllowed(PointerDownEvent @event) =>
        ScrollbarHitTest.IsThumbEvent(_customPaintKey, @event) && base.IsPointerAllowed(@event);
}

/// <summary>A horizontal drag recognizer that only claims pointers landing on the scrollbar thumb.</summary>
internal sealed class HorizontalThumbDragGestureRecognizer(GlobalKey customPaintKey)
    : HorizontalDragGestureRecognizer
{
    private readonly GlobalKey _customPaintKey = customPaintKey;

    protected override bool IsPointerAllowed(PointerDownEvent @event) =>
        ScrollbarHitTest.IsThumbEvent(_customPaintKey, @event) && base.IsPointerAllowed(@event);
}
