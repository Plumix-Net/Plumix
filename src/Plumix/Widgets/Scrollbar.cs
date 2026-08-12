using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
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

[Flags]
public enum ScrollbarInteractionState
{
    None = 0,
    Hovered = 1 << 0,
    Dragged = 1 << 1,
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
        context.DrawRectangle(trackBrush, null, value.TrackRect, TrackRadius ?? 0, TrackRadius ?? 0);

        Color borderColor = ApplyOpacity(TrackBorderColor, opacity);
        if (borderColor.A != 0)
        {
            var pen = new Pen(new SolidColorBrush(borderColor), 1);
            (Point start, Point end) = TrackBorderLine(value);
            context.DrawLine(pen, start, end);
        }

        BorderSide? side = (Shape as OutlinedBorder)?.Side;
        IPen? thumbPen = side is null
            ? null
            : new Pen(
                new SolidColorBrush(ApplyOpacity(side.Value.Color, opacity)),
                side.Value.Width);
        context.DrawRectangle(
            new SolidColorBrush(ApplyOpacity(Color, opacity)),
            thumbPen,
            value.ThumbRect,
            ScrollbarShapeGeometry.Radius(Shape) ?? Radius ?? 0,
            ScrollbarShapeGeometry.Radius(Shape) ?? Radius ?? 0);
    }

    public override bool? HitTest(Point position)
    {
        ScrollbarGeometry? geometry = Geometry;
        if (IgnorePointer ||
            _fadeoutOpacityAnimation.Value <= 0 ||
            !geometry.HasValue)
        {
            return false;
        }

        return geometry.Value.TrackRect.Contains(position);
    }

    public bool HitTestInteractive(Point position, PointerDeviceKind kind, bool forHover = false)
    {
        ScrollbarGeometry? geometry = Geometry;
        if (!geometry.HasValue || IgnorePointer || (!forHover && _fadeoutOpacityAnimation.Value <= 0))
        {
            return false;
        }

        Rect hitRect = kind is PointerDeviceKind.Touch or PointerDeviceKind.Trackpad || forHover
            ? ExpandToMinimumInteractiveSize(geometry.Value.ThumbRect)
            : geometry.Value.TrackRect;
        return hitRect.Contains(position) || geometry.Value.TrackRect.Contains(position);
    }

    public bool HitTestOnlyThumbInteractive(Point position, PointerDeviceKind kind, bool forHover = false)
    {
        ScrollbarGeometry? geometry = Geometry;
        if (!geometry.HasValue || IgnorePointer || (!forHover && _fadeoutOpacityAnimation.Value <= 0))
        {
            return false;
        }

        Rect hitRect = kind is PointerDeviceKind.Touch or PointerDeviceKind.Trackpad || forHover
            ? ExpandToMinimumInteractiveSize(geometry.Value.ThumbRect)
            : geometry.Value.ThumbRect;
        return hitRect.Contains(position);
    }

    public double GetTrackToScroll(double thumbOffset)
    {
        ScrollbarGeometry geometry = Geometry
            ?? throw new InvalidOperationException("Scrollbar geometry is not available before update and paint.");
        if (_metrics is null || geometry.MaxThumbTravel <= 0)
        {
            return 0;
        }

        double fraction = Math.Clamp(thumbOffset / geometry.MaxThumbTravel, 0, 1);
        if (geometry.IsReversed)
        {
            fraction = 1 - fraction;
        }
        return _metrics.MinScrollExtent +
               (fraction * (_metrics.MaxScrollExtent - _metrics.MinScrollExtent));
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
            trackRect = new Rect(trackX, trackStart, Thickness + (2 * CrossAxisMargin), trackExtent);
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
            trackRect = new Rect(trackStart, trackY, trackExtent, Thickness + (2 * CrossAxisMargin));
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

    private static Rect ExpandToMinimumInteractiveSize(Rect rect)
    {
        const double minimumSize = 48;
        double width = Math.Max(minimumSize, rect.Width);
        double height = Math.Max(minimumSize, rect.Height);
        return new Rect(
            rect.Center.X - (width / 2),
            rect.Center.Y - (height / 2),
            width,
            height);
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

public class RawScrollbar : StatefulWidget
{
    public RawScrollbar(
        Widget child,
        ScrollController? controller = null,
        bool? thumbVisibility = null,
        ShapeBorder? shape = null,
        double? radius = null,
        double? thickness = null,
        Color? thumbColor = null,
        double minThumbLength = 18,
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
        double mainAxisMargin = 0,
        double crossAxisMargin = 0,
        Thickness? padding = null,
        Key? key = null) : this(
        child,
        controller,
        thumbVisibility,
        shape,
        radius,
        thickness,
        thumbColor,
        minThumbLength,
        minOverscrollLength,
        trackVisibility,
        trackRadius,
        trackColor,
        trackBorderColor,
        fadeDuration,
        timeToFade,
        pressDuration,
        notificationPredicate,
        interactive,
        scrollbarOrientation,
        mainAxisMargin,
        crossAxisMargin,
        padding,
        thumbColorResolver: null,
        trackColorResolver: null,
        trackBorderColorResolver: null,
        thicknessResolver: null,
        radiusResolver: null,
        thumbVisibilityResolver: null,
        trackVisibilityResolver: null,
        trackTapEnabled: true,
        interactionChanged: null,
        key)
    {
    }

    internal RawScrollbar(
        Widget child,
        ScrollController? controller,
        bool? thumbVisibility,
        ShapeBorder? shape,
        double? radius,
        double? thickness,
        Color? thumbColor,
        double minThumbLength,
        double? minOverscrollLength,
        bool? trackVisibility,
        double? trackRadius,
        Color? trackColor,
        Color? trackBorderColor,
        TimeSpan? fadeDuration,
        TimeSpan? timeToFade,
        TimeSpan? pressDuration,
        ScrollNotificationPredicate? notificationPredicate,
        bool? interactive,
        ScrollbarOrientation? scrollbarOrientation,
        double mainAxisMargin,
        double crossAxisMargin,
        Thickness? padding,
        Func<ScrollbarInteractionState, Color?>? thumbColorResolver,
        Func<ScrollbarInteractionState, Color?>? trackColorResolver,
        Func<ScrollbarInteractionState, Color?>? trackBorderColorResolver,
        Func<ScrollbarInteractionState, double?>? thicknessResolver,
        Func<ScrollbarInteractionState, double?>? radiusResolver,
        Func<ScrollbarInteractionState, bool?>? thumbVisibilityResolver,
        Func<ScrollbarInteractionState, bool?>? trackVisibilityResolver,
        bool trackTapEnabled,
        Action<ScrollbarInteractionState>? interactionChanged,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (thumbVisibility == false && trackVisibility == true)
        {
            throw new ArgumentException("A scrollbar track cannot be visible without its thumb.");
        }

        ValidateNonNegative(nameof(minThumbLength), minThumbLength);
        ValidateNonNegative(nameof(minOverscrollLength), minOverscrollLength);
        ValidateNonNegative(nameof(mainAxisMargin), mainAxisMargin);
        ValidateNonNegative(nameof(crossAxisMargin), crossAxisMargin);
        ValidatePositive(nameof(thickness), thickness);
        ValidateNonNegative(nameof(radius), radius);
        ValidateNonNegative(nameof(trackRadius), trackRadius);
        ValidateDuration(nameof(fadeDuration), fadeDuration);
        ValidateDuration(nameof(timeToFade), timeToFade);
        ValidateDuration(nameof(pressDuration), pressDuration);
        if (minOverscrollLength > minThumbLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minOverscrollLength));
        }

        if (shape is not null && radius.HasValue)
        {
            throw new ArgumentException("Only one of shape and radius may be provided.");
        }

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
        FadeDuration = fadeDuration ?? TimeSpan.FromMilliseconds(300);
        TimeToFade = timeToFade ?? TimeSpan.FromMilliseconds(600);
        PressDuration = pressDuration ?? TimeSpan.Zero;
        NotificationPredicate = notificationPredicate ?? DefaultScrollNotificationPredicate;
        Interactive = interactive;
        ScrollbarOrientation = scrollbarOrientation;
        MainAxisMargin = mainAxisMargin;
        CrossAxisMargin = crossAxisMargin;
        Padding = padding;
        ThumbColorResolver = thumbColorResolver;
        TrackColorResolver = trackColorResolver;
        TrackBorderColorResolver = trackBorderColorResolver;
        ThicknessResolver = thicknessResolver;
        RadiusResolver = radiusResolver;
        ThumbVisibilityResolver = thumbVisibilityResolver;
        TrackVisibilityResolver = trackVisibilityResolver;
        TrackTapEnabled = trackTapEnabled;
        InteractionChanged = interactionChanged;
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public bool? ThumbVisibility { get; }
    public ShapeBorder? Shape { get; }
    public double? Radius { get; }
    public double? Thickness { get; }
    public Color? ThumbColor { get; }
    public double MinThumbLength { get; }
    public double? MinOverscrollLength { get; }
    public bool? TrackVisibility { get; }
    public double? TrackRadius { get; }
    public Color? TrackColor { get; }
    public Color? TrackBorderColor { get; }
    public TimeSpan FadeDuration { get; }
    public TimeSpan TimeToFade { get; }
    public TimeSpan PressDuration { get; }
    public ScrollNotificationPredicate NotificationPredicate { get; }
    public bool? Interactive { get; }
    public ScrollbarOrientation? ScrollbarOrientation { get; }
    public double MainAxisMargin { get; }
    public double CrossAxisMargin { get; }
    public Thickness? Padding { get; }

    internal Func<ScrollbarInteractionState, Color?>? ThumbColorResolver { get; }
    internal Func<ScrollbarInteractionState, Color?>? TrackColorResolver { get; }
    internal Func<ScrollbarInteractionState, Color?>? TrackBorderColorResolver { get; }
    internal Func<ScrollbarInteractionState, double?>? ThicknessResolver { get; }
    internal Func<ScrollbarInteractionState, double?>? RadiusResolver { get; }
    internal Func<ScrollbarInteractionState, bool?>? ThumbVisibilityResolver { get; }
    internal Func<ScrollbarInteractionState, bool?>? TrackVisibilityResolver { get; }
    internal bool TrackTapEnabled { get; }
    internal Action<ScrollbarInteractionState>? InteractionChanged { get; }

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
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private static void ValidateDuration(string name, TimeSpan? value)
    {
        if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(name);
    }
}

public class RawScrollbarState<T> : State where T : RawScrollbar
    {
        private ScrollController? _controller;
        private AnimationController? _fadeController;
        private AnimationController? _fadeDelayController;
        private AnimationController? _pressController;
        private AxisDirection _axisDirection = AxisDirection.Down;
        private ScrollbarInteractionState _interactionState;
        private int _paintRevision;
        private int? _activePointer;
        private bool _draggingThumb;
        private bool _pendingThumbPress;
        private double _dragOffsetWithinThumb;
        private double _lastPointerAxisOffset;
        private DateTime _lastPointerTimestamp;
        private double _lastDragVelocity;
        private bool _didDragThumb;
        private double _pendingThumbStart;
        private double _pendingThumbExtent;

        protected T CurrentWidget => (T)StateWidget;

        protected bool IsHovered => _interactionState.HasFlag(ScrollbarInteractionState.Hovered);

        protected bool IsDragged => _interactionState.HasFlag(ScrollbarInteractionState.Dragged);

        protected IReadOnlySet<WidgetState> WidgetStates
        {
            get
            {
                var states = new HashSet<WidgetState>();
                if (IsHovered) states.Add(WidgetState.Hovered);
                if (IsDragged) states.Add(WidgetState.Dragged);
                return states;
            }
        }

        public override void InitState()
        {
            CreateFadeControllers();
            CreatePressController();
            AttachController(CurrentWidget.Controller);
        }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            if (CurrentWidget.Controller is null)
            {
                AttachController(PrimaryScrollController.MaybeOf(Context));
            }

            ScheduleControllerValidation();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (RawScrollbar)oldWidget;
            if (!ReferenceEquals(old.Controller, CurrentWidget.Controller))
            {
                AttachController(CurrentWidget.Controller ?? PrimaryScrollController.MaybeOf(Context));
            }

            if (old.FadeDuration != CurrentWidget.FadeDuration || old.TimeToFade != CurrentWidget.TimeToFade)
            {
                DisposeFadeControllers();
                CreateFadeControllers();
            }

            if (old.PressDuration != CurrentWidget.PressDuration)
            {
                DisposePressController();
                CreatePressController();
            }

            if (CurrentWidget.ThumbVisibility == true)
            {
                CancelFade();
            }

            else if (old.ThumbVisibility == true)
            {
                _fadeController?.Forward(0);
            }

            ScheduleControllerValidation();
        }

        public override void Dispose()
        {
            AttachController(null);
            DisposeFadeControllers();
            DisposePressController();
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var states = _interactionState;
            bool forcedVisible = ResolveThumbVisibility(states);
            bool trackVisible = ResolveTrackVisibility(states);
            double opacity = forcedVisible ? 1 : 1 - (_fadeController?.Evaluate() ?? 1);
            double thickness = ResolveThickness(states);
            double radius = ResolveRadius(states);
            var padding = widget.Padding ?? MediaQuery.MaybePaddingOf(context) ?? default;
            bool interactive = widget.Interactive ?? true;
            var effectiveOrientation = widget.ScrollbarOrientation;
            if (!effectiveOrientation.HasValue)
            {
                effectiveOrientation = _axisDirection is AxisDirection.Left or AxisDirection.Right
                    ? global::Plumix.Widgets.ScrollbarOrientation.Bottom
                    : Directionality.Of(context) == TextDirection.Rtl
                        ? global::Plumix.Widgets.ScrollbarOrientation.Left
                        : global::Plumix.Widgets.ScrollbarOrientation.Right;
            }

            Widget result = new RawScrollbarOverlay(
                positionProvider: () => _controller?.PrimaryPosition,
                axisDirection: _axisDirection,
                orientation: effectiveOrientation,
                thickness: thickness,
                thumbColor: ResolveThumbColor(states),
                radius: radius,
                thumbBorder: (widget.Shape as OutlinedBorder)?.Side,
                minThumbLength: widget.MinThumbLength,
                minOverscrollLength: widget.MinOverscrollLength ?? widget.MinThumbLength,
                trackVisible: trackVisible && (forcedVisible || opacity > 0.001),
                trackRadius: widget.TrackRadius ?? 0,
                trackColor: ResolveTrackColor(states),
                trackBorderColor: ResolveTrackBorderColor(states),
                mainAxisMargin: widget.MainAxisMargin,
                crossAxisMargin: widget.CrossAxisMargin,
                padding: padding,
                opacity: Math.Clamp(opacity, 0, 1),
                interactive: interactive,
                paintRevision: _paintRevision,
                onPointerDown: HandlePointerDown,
                onPointerMove: HandlePointerMove,
                onPointerUp: HandlePointerUp,
                onPointerCancel: HandlePointerCancel,
                onPointerHover: HandlePointerHover,
                onPointerExit: HandlePointerExit,
                child: new NotificationListener<ScrollNotification>(
                    onNotification: HandleScrollNotification,
                    child: widget.Child));

            return result;
        }

        protected virtual bool ResolveThumbVisibility(ScrollbarInteractionState states) =>
            CurrentWidget.ThumbVisibility
            ?? CurrentWidget.ThumbVisibilityResolver?.Invoke(states)
            ?? false;

        protected virtual bool ResolveTrackVisibility(ScrollbarInteractionState states) =>
            CurrentWidget.TrackVisibility
            ?? CurrentWidget.TrackVisibilityResolver?.Invoke(states)
            ?? false;

        protected virtual double ResolveThickness(ScrollbarInteractionState states) =>
            CurrentWidget.Thickness
            ?? CurrentWidget.ThicknessResolver?.Invoke(states)
            ?? 6;

        protected virtual double ResolveRadius(ScrollbarInteractionState states) =>
            ScrollbarShapeGeometry.Radius(CurrentWidget.Shape)
            ?? CurrentWidget.Radius
            ?? CurrentWidget.RadiusResolver?.Invoke(states)
            ?? 0;

        protected virtual Color ResolveThumbColor(ScrollbarInteractionState states) =>
            CurrentWidget.ThumbColor
            ?? CurrentWidget.ThumbColorResolver?.Invoke(states)
            ?? Color.FromArgb(0x66, 0xBC, 0xBC, 0xBC);

        protected virtual Color ResolveTrackColor(ScrollbarInteractionState states) =>
            CurrentWidget.TrackColor
            ?? CurrentWidget.TrackColorResolver?.Invoke(states)
            ?? Color.FromArgb(0x08, 0, 0, 0);

        protected virtual Color ResolveTrackBorderColor(ScrollbarInteractionState states) =>
            CurrentWidget.TrackBorderColor
            ?? CurrentWidget.TrackBorderColorResolver?.Invoke(states)
            ?? Color.FromArgb(0x1A, 0, 0, 0);

        private bool HandleScrollNotification(ScrollNotification notification)
        {
            if (!CurrentWidget.NotificationPredicate(notification)) return false;
            _axisDirection = notification.Metrics.AxisDirection;
            if (notification.Metrics.MaxScrollExtent <= notification.Metrics.MinScrollExtent)
            {
                _fadeDelayController?.Stop();
                _fadeController?.Forward(0);
            }
            else if (notification is ScrollEndNotification)
            {
                if (!IsDragged && !ResolveThumbVisibility(_interactionState))
                {
                    _fadeDelayController?.Forward(0);
                }
            }
            else
            {
                ShowTemporarily(scheduleFade: false);
            }
            return false;
        }

        private void ScheduleControllerValidation()
        {
            global::Plumix.Scheduler.AddPostFrameCallback(_ =>
            {
                if (!Mounted)
                {
                    return;
                }

                bool interactive = CurrentWidget.Interactive ?? true;
                if (!ResolveThumbVisibility(_interactionState) && !interactive)
                {
                    return;
                }

                if (_controller is null)
                {
                    throw new InvalidOperationException(
                        "A visible or interactive RawScrollbar requires a ScrollController or " +
                        "PrimaryScrollController.");
                }

                if (_controller.Positions.Count != 1)
                {
                    throw new InvalidOperationException(
                        "A visible or interactive RawScrollbar requires exactly one attached ScrollPosition; " +
                        $"found {_controller.Positions.Count}.");
                }
            });
        }

        private void AttachController(ScrollController? controller)
        {
            if (ReferenceEquals(_controller, controller)) return;
            _controller?.RemoveListener(HandleControllerChanged);
            _controller = controller;
            _controller?.AddListener(HandleControllerChanged);
            if (_controller?.PrimaryPosition is { } position)
            {
                _axisDirection = position.AxisDirection;
            }
            if (Mounted) SetState(() => _paintRevision++);
        }

        private void HandleControllerChanged()
        {
            ShowTemporarily();
        }

        private void ShowTemporarily(bool scheduleFade = true)
        {
            if (!Mounted) return;
            SetState(() => _paintRevision++);
            if (ResolveThumbVisibility(_interactionState)) return;
            _fadeController?.Stop();
            SetFadeValue(0);
            if (scheduleFade)
            {
                _fadeDelayController?.Forward(0);
            }
        }

        private void CancelFade()
        {
            _fadeDelayController?.Stop();
            _fadeController?.Stop();
            SetFadeValue(0);
        }

        private void CreateFadeControllers()
        {
            _fadeController = new AnimationController(CurrentWidget.FadeDuration, this)
            {
                Curve = Curves.FastOutSlowIn,
            };
            SetFadeValue(1);
            _fadeController.Changed += HandleFadeTick;
            _fadeDelayController = new AnimationController(CurrentWidget.TimeToFade, this);
            _fadeDelayController.Completed += HandleFadeDelayCompleted;
        }

        private void DisposeFadeControllers()
        {
            if (_fadeController is not null)
            {
                _fadeController.Changed -= HandleFadeTick;
                _fadeController.Dispose();
                _fadeController = null;
            }

            if (_fadeDelayController is not null)
            {
                _fadeDelayController.Completed -= HandleFadeDelayCompleted;
                _fadeDelayController.Dispose();
                _fadeDelayController = null;
            }
        }

        private void HandleFadeTick()
        {
            if (Mounted) SetState(() => _paintRevision++);
        }

        private void HandleFadeDelayCompleted()
        {
            if (_interactionState.HasFlag(ScrollbarInteractionState.Dragged)) return;
            _fadeController?.Forward(0);
        }

        private void CreatePressController()
        {
            _pressController = new AnimationController(CurrentWidget.PressDuration, this);
            _pressController.Completed += HandlePressDurationCompleted;
        }

        private void DisposePressController()
        {
            if (_pressController is null) return;
            _pressController.Completed -= HandlePressDurationCompleted;
            _pressController.Dispose();
            _pressController = null;
        }

        private void HandlePressDurationCompleted()
        {
            if (!_pendingThumbPress || !_activePointer.HasValue) return;
            BeginThumbDrag(_lastPointerAxisOffset, _pendingThumbStart, _pendingThumbExtent);
        }

        private void SetFadeValue(double value)
        {
            if (_fadeController is null) return;
            _fadeController.Forward(value);
            _fadeController.Stop();
        }

        private void HandlePointerDown(PointerDownEvent @event, ScrollbarGeometry geometry)
        {
            if (_activePointer.HasValue || _controller?.PrimaryPosition is not { } position) return;
            _activePointer = @event.Pointer;
            double axisOffset = AxisOffset(@event.LocalPosition, geometry.Axis);
            _lastPointerAxisOffset = axisOffset;
            _lastPointerTimestamp = @event.TimestampUtc;
            double thumbStart = geometry.TrackMainAxisStart + geometry.ThumbMainAxisOffset;
            double thumbEnd = thumbStart + geometry.ThumbMainAxisExtent;
            if (axisOffset >= thumbStart && axisOffset <= thumbEnd)
            {
                CancelFade();
                if (CurrentWidget.PressDuration <= TimeSpan.Zero)
                {
                    BeginThumbDrag(axisOffset, thumbStart, geometry.ThumbMainAxisExtent);
                }
                else
                {
                    _pendingThumbPress = true;
                    _pendingThumbStart = thumbStart;
                    _pendingThumbExtent = geometry.ThumbMainAxisExtent;
                    _pressController?.Forward(0);
                }
                return;
            }

            if (!CurrentWidget.TrackTapEnabled) return;

            int direction = axisOffset < thumbStart ? -1 : 1;
            position.AnimateTo(
                position.Pixels + (direction * position.ViewportDimension),
                TimeSpan.FromMilliseconds(100),
                Curves.EaseInOut);
            ShowTemporarily();
        }

        private void HandlePointerMove(PointerMoveEvent @event, ScrollbarGeometry geometry)
        {
            if (_activePointer != @event.Pointer) return;
            double previousAxisOffset = _lastPointerAxisOffset;
            DateTime previousTimestamp = _lastPointerTimestamp;
            _lastPointerAxisOffset = AxisOffset(@event.LocalPosition, geometry.Axis);
            _lastPointerTimestamp = @event.TimestampUtc;
            if (!_draggingThumb || _controller?.PrimaryPosition is not { } position)
            {
                return;
            }

            double delta = _lastPointerAxisOffset - previousAxisOffset;
            double elapsedSeconds = (@event.TimestampUtc - previousTimestamp).TotalSeconds;
            _didDragThumb |= Math.Abs(delta) > Constants.PrecisionErrorTolerance;
            _lastDragVelocity = elapsedSeconds > 0 ? delta / elapsedSeconds : 0;

            double axisOffset = _lastPointerAxisOffset;
            double thumbOffset = Math.Clamp(
                axisOffset - _dragOffsetWithinThumb - geometry.TrackMainAxisStart,
                0,
                geometry.MaxThumbTravel);
            double fraction = geometry.MaxThumbTravel <= 0 ? 0 : thumbOffset / geometry.MaxThumbTravel;
            if (geometry.IsReversed) fraction = 1 - fraction;
            double target = position.MinScrollExtent +
                            (fraction * (position.MaxScrollExtent - position.MinScrollExtent));
            position.UpdateDragTo(target);
        }

        private void HandlePointerUp(PointerUpEvent @event, ScrollbarGeometry geometry) => EndPointer(@event.Pointer);

        private void HandlePointerCancel(PointerCancelEvent @event, ScrollbarGeometry geometry) => EndPointer(@event.Pointer);

        private void EndPointer(int pointer)
        {
            if (_activePointer != pointer) return;
            _activePointer = null;
            _pressController?.Stop();
            _pendingThumbPress = false;
            if (_draggingThumb)
            {
                _controller?.PrimaryPosition?.EndDrag(0);
                _draggingThumb = false;
                SetInteractionState(_interactionState & ~ScrollbarInteractionState.Dragged);
                ThumbDragEnded(_didDragThumb, _lastDragVelocity);
            }

            ShowTemporarily();
        }

        private void BeginThumbDrag(double axisOffset, double thumbStart, double thumbExtent)
        {
            _pendingThumbPress = false;
            _pressController?.Stop();
            _draggingThumb = true;
            _controller?.PrimaryPosition?.BeginDrag();
            _didDragThumb = false;
            _lastDragVelocity = 0;
            _dragOffsetWithinThumb = Math.Clamp(axisOffset - thumbStart, 0, thumbExtent);
            SetInteractionState(_interactionState | ScrollbarInteractionState.Dragged);
        }

        private void HandlePointerHover(PointerHoverEvent @event, ScrollbarGeometry geometry)
        {
            if (@event.Kind is not (PointerDeviceKind.Mouse or PointerDeviceKind.Trackpad) ||
                !(CurrentWidget.Interactive ?? true) ||
                !IsPointerOverScrollbar(
                    @event.LocalPosition,
                    geometry,
                    includeHoverPadding: IsScrollbarTransparent()))
            {
                EndHover();
                return;
            }

            _fadeDelayController?.Stop();
            if (_fadeController is { Value: > 0 } fadeController)
            {
                fadeController.Reverse();
            }
            SetInteractionState(_interactionState | ScrollbarInteractionState.Hovered);
        }

        private void HandlePointerExit(PointerExitEvent @event, ScrollbarGeometry geometry)
        {
            EndHover();
        }

        private void EndHover()
        {
            if (!_interactionState.HasFlag(ScrollbarInteractionState.Hovered)) return;
            SetInteractionState(_interactionState & ~ScrollbarInteractionState.Hovered);
            if (CurrentWidget.ThumbVisibility != true)
            {
                _fadeDelayController?.Forward(0);
            }
        }

        private bool IsScrollbarTransparent()
        {
            bool forcedVisible = CurrentWidget.ThumbVisibility
                                 ?? CurrentWidget.ThumbVisibilityResolver?.Invoke(_interactionState)
                                 ?? false;
            return !forcedVisible && 1 - (_fadeController?.Evaluate() ?? 1) <= 0.001;
        }

        private static bool IsPointerOverScrollbar(
            Point position,
            ScrollbarGeometry geometry,
            bool includeHoverPadding)
        {
            if (!includeHoverPadding) return geometry.TrackRect.Contains(position);

            const double minInteractiveSize = 48;
            var center = geometry.ThumbRect.Center;
            var paddedThumb = new Rect(
                center.X - (minInteractiveSize / 2),
                center.Y - (minInteractiveSize / 2),
                minInteractiveSize,
                minInteractiveSize);
            var track = geometry.TrackRect;
            var hoverRect = new Rect(
                Math.Min(track.Left, paddedThumb.Left),
                Math.Min(track.Top, paddedThumb.Top),
                Math.Max(track.Right, paddedThumb.Right) - Math.Min(track.Left, paddedThumb.Left),
                Math.Max(track.Bottom, paddedThumb.Bottom) - Math.Min(track.Top, paddedThumb.Top));
            return hoverRect.Contains(position);
        }

        protected virtual void InteractionStateChanged(
            ScrollbarInteractionState oldValue,
            ScrollbarInteractionState newValue)
        {
        }

        protected virtual void ThumbDragEnded(bool didDrag, double primaryVelocity)
        {
        }

        private void SetInteractionState(ScrollbarInteractionState value)
        {
            if (_interactionState == value) return;
            ScrollbarInteractionState oldValue = _interactionState;
            SetState(() =>
            {
                _interactionState = value;
                _paintRevision++;
            });
            InteractionStateChanged(oldValue, value);
            CurrentWidget.InteractionChanged?.Invoke(value);
        }

        private static double AxisOffset(Point point, Axis axis) => axis == Axis.Vertical ? point.Y : point.X;
}

// Compatibility wrapper retained for existing Plumix.Widgets call sites. Material applications
// should use Plumix.Material.Scrollbar, which supplies Flutter Material defaults and theming.
public sealed class Scrollbar : StatelessWidget
{
    public Scrollbar(
        Widget child,
        ScrollController? controller = null,
        double thickness = 4,
        Color? thumbColor = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Controller = controller;
        Thickness = thickness;
        ThumbColor = thumbColor ?? Color.Parse("#AA5A6B82");
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public double Thickness { get; }
    public Color ThumbColor { get; }

    public override Widget Build(BuildContext context) => new RawScrollbar(
        child: Child,
        controller: Controller,
        thickness: Thickness,
        thumbColor: ThumbColor);
}

internal sealed class RawScrollbarOverlay : SingleChildRenderObjectWidget
{
    public RawScrollbarOverlay(
        Func<ScrollPosition?> positionProvider,
        AxisDirection axisDirection,
        ScrollbarOrientation? orientation,
        double thickness,
        Color thumbColor,
        double radius,
        BorderSide? thumbBorder,
        double minThumbLength,
        double minOverscrollLength,
        bool trackVisible,
        double trackRadius,
        Color trackColor,
        Color trackBorderColor,
        double mainAxisMargin,
        double crossAxisMargin,
        Thickness padding,
        double opacity,
        bool interactive,
        int paintRevision,
        Action<PointerDownEvent, ScrollbarGeometry> onPointerDown,
        Action<PointerMoveEvent, ScrollbarGeometry> onPointerMove,
        Action<PointerUpEvent, ScrollbarGeometry> onPointerUp,
        Action<PointerCancelEvent, ScrollbarGeometry> onPointerCancel,
        Action<PointerHoverEvent, ScrollbarGeometry> onPointerHover,
        Action<PointerExitEvent, ScrollbarGeometry> onPointerExit,
        Widget child) : base(child)
    {
        PositionProvider = positionProvider;
        AxisDirection = axisDirection;
        Orientation = orientation;
        Thickness = thickness;
        ThumbColor = thumbColor;
        Radius = radius;
        ThumbBorder = thumbBorder;
        MinThumbLength = minThumbLength;
        MinOverscrollLength = minOverscrollLength;
        TrackVisible = trackVisible;
        TrackRadius = trackRadius;
        TrackColor = trackColor;
        TrackBorderColor = trackBorderColor;
        MainAxisMargin = mainAxisMargin;
        CrossAxisMargin = crossAxisMargin;
        Padding = padding;
        Opacity = opacity;
        Interactive = interactive;
        PaintRevision = paintRevision;
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnPointerHover = onPointerHover;
        OnPointerExit = onPointerExit;
    }

    public Func<ScrollPosition?> PositionProvider { get; }
    public AxisDirection AxisDirection { get; }
    public ScrollbarOrientation? Orientation { get; }
    public double Thickness { get; }
    public Color ThumbColor { get; }
    public double Radius { get; }
    public BorderSide? ThumbBorder { get; }
    public double MinThumbLength { get; }
    public double MinOverscrollLength { get; }
    public bool TrackVisible { get; }
    public double TrackRadius { get; }
    public Color TrackColor { get; }
    public Color TrackBorderColor { get; }
    public double MainAxisMargin { get; }
    public double CrossAxisMargin { get; }
    public Thickness Padding { get; }
    public double Opacity { get; }
    public bool Interactive { get; }
    public int PaintRevision { get; }
    public Action<PointerDownEvent, ScrollbarGeometry> OnPointerDown { get; }
    public Action<PointerMoveEvent, ScrollbarGeometry> OnPointerMove { get; }
    public Action<PointerUpEvent, ScrollbarGeometry> OnPointerUp { get; }
    public Action<PointerCancelEvent, ScrollbarGeometry> OnPointerCancel { get; }
    public Action<PointerHoverEvent, ScrollbarGeometry> OnPointerHover { get; }
    public Action<PointerExitEvent, ScrollbarGeometry> OnPointerExit { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderRawScrollbarOverlay(
        PositionProvider, AxisDirection, Orientation, Thickness, ThumbColor, Radius, ThumbBorder, MinThumbLength,
        MinOverscrollLength, TrackVisible, TrackRadius, TrackColor, TrackBorderColor, MainAxisMargin,
        CrossAxisMargin, Padding, Opacity, Interactive, PaintRevision, OnPointerDown, OnPointerMove,
        OnPointerUp, OnPointerCancel, OnPointerHover, OnPointerExit);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var overlay = (RenderRawScrollbarOverlay)renderObject;
        overlay.Update(
            PositionProvider, AxisDirection, Orientation, Thickness, ThumbColor, Radius, ThumbBorder, MinThumbLength,
            MinOverscrollLength, TrackVisible, TrackRadius, TrackColor, TrackBorderColor, MainAxisMargin,
            CrossAxisMargin, Padding, Opacity, Interactive, PaintRevision, OnPointerDown, OnPointerMove,
            OnPointerUp, OnPointerCancel, OnPointerHover, OnPointerExit);
    }
}

internal sealed class RenderRawScrollbarOverlay : RenderProxyBox
{
    private Func<ScrollPosition?> _positionProvider;
    private AxisDirection _axisDirection;
    private ScrollbarOrientation? _orientation;
    private double _thickness;
    private Color _thumbColor;
    private double _radius;
    private BorderSide? _thumbBorder;
    private double _minThumbLength;
    private double _minOverscrollLength;
    private bool _trackVisible;
    private double _trackRadius;
    private Color _trackColor;
    private Color _trackBorderColor;
    private double _mainAxisMargin;
    private double _crossAxisMargin;
    private Thickness _padding;
    private double _opacity;
    private bool _interactive;
    private int _paintRevision;
    private Action<PointerDownEvent, ScrollbarGeometry> _onPointerDown;
    private Action<PointerMoveEvent, ScrollbarGeometry> _onPointerMove;
    private Action<PointerUpEvent, ScrollbarGeometry> _onPointerUp;
    private Action<PointerCancelEvent, ScrollbarGeometry> _onPointerCancel;
    private Action<PointerHoverEvent, ScrollbarGeometry> _onPointerHover;
    private Action<PointerExitEvent, ScrollbarGeometry> _onPointerExit;

    public RenderRawScrollbarOverlay(
        Func<ScrollPosition?> positionProvider, AxisDirection axisDirection, ScrollbarOrientation? orientation,
        double thickness, Color thumbColor, double radius, BorderSide? thumbBorder, double minThumbLength, double minOverscrollLength,
        bool trackVisible, double trackRadius, Color trackColor, Color trackBorderColor, double mainAxisMargin,
        double crossAxisMargin, Thickness padding, double opacity, bool interactive, int paintRevision,
        Action<PointerDownEvent, ScrollbarGeometry> onPointerDown,
        Action<PointerMoveEvent, ScrollbarGeometry> onPointerMove,
        Action<PointerUpEvent, ScrollbarGeometry> onPointerUp,
        Action<PointerCancelEvent, ScrollbarGeometry> onPointerCancel,
        Action<PointerHoverEvent, ScrollbarGeometry> onPointerHover,
        Action<PointerExitEvent, ScrollbarGeometry> onPointerExit)
    {
        _positionProvider = positionProvider;
        _axisDirection = axisDirection;
        _orientation = orientation;
        _thickness = thickness;
        _thumbColor = thumbColor;
        _radius = radius;
        _thumbBorder = thumbBorder;
        _minThumbLength = minThumbLength;
        _minOverscrollLength = minOverscrollLength;
        _trackVisible = trackVisible;
        _trackRadius = trackRadius;
        _trackColor = trackColor;
        _trackBorderColor = trackBorderColor;
        _mainAxisMargin = mainAxisMargin;
        _crossAxisMargin = crossAxisMargin;
        _padding = padding;
        _opacity = opacity;
        _interactive = interactive;
        _paintRevision = paintRevision;
        _onPointerDown = onPointerDown;
        _onPointerMove = onPointerMove;
        _onPointerUp = onPointerUp;
        _onPointerCancel = onPointerCancel;
        _onPointerHover = onPointerHover;
        _onPointerExit = onPointerExit;
    }

    internal ScrollbarGeometry? Geometry => ComputeGeometry();
    internal Color ThumbColor => _thumbColor;
    internal Color TrackColor => _trackColor;
    internal Color TrackBorderColor => _trackBorderColor;
    internal double Thickness => _thickness;
    internal double Opacity => _opacity;
    internal bool TrackVisible => _trackVisible;

    public void Update(
        Func<ScrollPosition?> positionProvider, AxisDirection axisDirection, ScrollbarOrientation? orientation,
        double thickness, Color thumbColor, double radius, BorderSide? thumbBorder, double minThumbLength, double minOverscrollLength,
        bool trackVisible, double trackRadius, Color trackColor, Color trackBorderColor, double mainAxisMargin,
        double crossAxisMargin, Thickness padding, double opacity, bool interactive, int paintRevision,
        Action<PointerDownEvent, ScrollbarGeometry> onPointerDown,
        Action<PointerMoveEvent, ScrollbarGeometry> onPointerMove,
        Action<PointerUpEvent, ScrollbarGeometry> onPointerUp,
        Action<PointerCancelEvent, ScrollbarGeometry> onPointerCancel,
        Action<PointerHoverEvent, ScrollbarGeometry> onPointerHover,
        Action<PointerExitEvent, ScrollbarGeometry> onPointerExit)
    {
        _positionProvider = positionProvider;
        _axisDirection = axisDirection;
        _orientation = orientation;
        _thickness = thickness;
        _thumbColor = thumbColor;
        _radius = radius;
        _thumbBorder = thumbBorder;
        _minThumbLength = minThumbLength;
        _minOverscrollLength = minOverscrollLength;
        _trackVisible = trackVisible;
        _trackRadius = trackRadius;
        _trackColor = trackColor;
        _trackBorderColor = trackBorderColor;
        _mainAxisMargin = mainAxisMargin;
        _crossAxisMargin = crossAxisMargin;
        _padding = padding;
        _opacity = opacity;
        _interactive = interactive;
        _paintRevision = paintRevision;
        _onPointerDown = onPointerDown;
        _onPointerMove = onPointerMove;
        _onPointerUp = onPointerUp;
        _onPointerCancel = onPointerCancel;
        _onPointerHover = onPointerHover;
        _onPointerExit = onPointerExit;
        MarkNeedsPaint();
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        base.Paint(context, offset);
        var geometry = ComputeGeometry();
        if (!geometry.HasValue || _opacity <= 0.001) return;

        var value = geometry.Value;
        Color trackColor = _trackVisible ? _trackColor : Colors.Transparent;
        Color trackBorderColor = _trackVisible ? _trackBorderColor : Colors.Transparent;
        var trackBrush = new SolidColorBrush(ApplyOpacity(trackColor, _opacity));
        context.DrawRectangle(
            trackBrush,
            null,
            Translate(value.TrackRect, offset),
            _trackRadius,
            _trackRadius);
        Color borderColor = ApplyOpacity(trackBorderColor, _opacity);
        if (borderColor.A != 0)
        {
            var pen = new Pen(new SolidColorBrush(borderColor), 1);
            (Point start, Point end) = TrackBorderLine(value, offset);
            context.DrawLine(pen, start, end);
        }

        context.DrawRectangle(
            new SolidColorBrush(ApplyOpacity(_thumbColor, _opacity)),
            _thumbBorder is { } thumbBorder
                ? new Pen(
                    new SolidColorBrush(ApplyOpacity(thumbBorder.Color, _opacity)),
                    thumbBorder.Width)
                : null,
            Translate(value.ThumbRect, offset),
            _radius,
            _radius);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        var geometry = ComputeGeometry();
        if (_interactive && _opacity > 0.001 && geometry is { } value && value.TrackRect.Contains(position))
        {
            result.Add(new BoxHitTestEntry(this, position));
            return true;
        }

        return base.HitTest(result, position);
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (entry is not BoxHitTestEntry || ComputeGeometry() is not { } geometry) return;
        var localEvent = @event;
        switch (localEvent)
        {
            case PointerDownEvent down when IsInteractivePointerDown(down, geometry):
                _onPointerDown(down, geometry);
                break;
            case PointerMoveEvent move: _onPointerMove(move, geometry); break;
            case PointerUpEvent up: _onPointerUp(up, geometry); break;
            case PointerCancelEvent cancel: _onPointerCancel(cancel, geometry); break;
            case PointerHoverEvent hover: _onPointerHover(hover, geometry); break;
            case PointerExitEvent exit: _onPointerExit(exit, geometry); break;
        }
    }

    private bool IsInteractivePointerDown(PointerDownEvent @event, ScrollbarGeometry geometry)
    {
        return _interactive &&
               _opacity > 0.001 &&
               geometry.TrackRect.Contains(@event.LocalPosition);
    }

    private ScrollbarGeometry? ComputeGeometry()
    {
        var position = _positionProvider();
        if (position is null ||
            position.ViewportDimension <= 0 ||
            !double.IsFinite(position.MaxScrollExtent) ||
            position.MaxScrollExtent - position.MinScrollExtent <= Constants.PrecisionErrorTolerance)
        {
            return null;
        }

        var orientation = _orientation ?? (_axisDirection is AxisDirection.Left or AxisDirection.Right
            ? ScrollbarOrientation.Bottom
            : ScrollbarOrientation.Right);
        var axis = orientation is ScrollbarOrientation.Left or ScrollbarOrientation.Right
            ? Axis.Vertical
            : Axis.Horizontal;
        AxisDirection axisDirection = position.AxisDirection;
        Axis scrollAxis = axisDirection is AxisDirection.Up or AxisDirection.Down
            ? Axis.Vertical
            : Axis.Horizontal;
        if (axis != scrollAxis)
        {
            throw new InvalidOperationException(
                $"Scrollbar orientation {orientation} is incompatible with axis direction {axisDirection}.");
        }

        bool reversed = axisDirection is AxisDirection.Up or AxisDirection.Left;
        double leadingPadding = axis == Axis.Vertical ? _padding.Top : _padding.Left;
        double trailingPadding = axis == Axis.Vertical ? _padding.Bottom : _padding.Right;
        double mainExtent = axis == Axis.Vertical ? Size.Height : Size.Width;
        double trackStart = leadingPadding + _mainAxisMargin;
        double trackExtent = Math.Max(0, mainExtent - leadingPadding - trailingPadding - (2 * _mainAxisMargin));
        if (trackExtent <= 0) return null;

        double leadingOverscroll = Math.Max(position.MinScrollExtent - position.Pixels, 0);
        double trailingOverscroll = Math.Max(position.Pixels - position.MaxScrollExtent, 0);
        double extentInside = Math.Max(0, position.ViewportDimension - leadingOverscroll - trailingOverscroll);
        double extentBefore = Math.Max(position.Pixels - position.MinScrollExtent, 0);
        double extentAfter = Math.Max(position.MaxScrollExtent - position.Pixels, 0);
        double totalContentExtent = extentBefore + extentInside + extentAfter;
        double totalMainAxisPadding = leadingPadding + trailingPadding;
        double fractionVisible = Math.Clamp(
            (extentInside - totalMainAxisPadding) /
            Math.Max(Constants.PrecisionErrorTolerance, totalContentExtent - totalMainAxisPadding),
            0,
            1);
        double candidateThumbExtent = Math.Max(
            Math.Min(trackExtent, _minOverscrollLength),
            trackExtent * fractionVisible);
        double safeMinLength = Math.Min(_minThumbLength, trackExtent);
        double overscrollFraction = Math.Clamp(
            1 - (extentInside / position.ViewportDimension),
            0,
            0.2);
        double thumbMinLength = extentBefore > 0 && extentAfter > 0
            ? safeMinLength
            : safeMinLength * (1 - (overscrollFraction / 0.2));
        double thumbExtent = Math.Clamp(candidateThumbExtent, thumbMinLength, trackExtent);
        double fraction = Math.Clamp(
            (position.Pixels - position.MinScrollExtent) / (position.MaxScrollExtent - position.MinScrollExtent),
            0,
            1);
        if (reversed) fraction = 1 - fraction;
        double thumbOffset = fraction * Math.Max(0, trackExtent - thumbExtent);

        Rect trackRect;
        Rect thumbRect;
        if (axis == Axis.Vertical)
        {
            double thumbX = orientation == ScrollbarOrientation.Left
                ? _padding.Left + _crossAxisMargin
                : Size.Width - _padding.Right - _crossAxisMargin - _thickness;
            double trackX = orientation == ScrollbarOrientation.Left
                ? _padding.Left
                : thumbX - _crossAxisMargin;
            trackRect = new Rect(trackX, trackStart, _thickness + (2 * _crossAxisMargin), trackExtent);
            thumbRect = new Rect(thumbX, trackStart + thumbOffset, _thickness, thumbExtent);
        }
        else
        {
            double thumbY = orientation == ScrollbarOrientation.Top
                ? _padding.Top + _crossAxisMargin
                : Size.Height - _padding.Bottom - _crossAxisMargin - _thickness;
            double trackY = orientation == ScrollbarOrientation.Top
                ? _padding.Top
                : thumbY - _crossAxisMargin;
            trackRect = new Rect(trackStart, trackY, trackExtent, _thickness + (2 * _crossAxisMargin));
            thumbRect = new Rect(trackStart + thumbOffset, thumbY, thumbExtent, _thickness);
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

    private static Rect Translate(Rect rect, Point offset) => new(rect.Position + offset, rect.Size);

    private (Point Start, Point End) TrackBorderLine(ScrollbarGeometry geometry, Point offset)
    {
        Rect track = Translate(geometry.TrackRect, offset);
        return _orientation switch
        {
            ScrollbarOrientation.Left => (track.TopRight, track.BottomRight),
            ScrollbarOrientation.Top => (track.BottomLeft, track.BottomRight),
            ScrollbarOrientation.Bottom => (track.TopLeft, track.TopRight),
            _ => (track.TopLeft, track.BottomLeft),
        };
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)(color.A * opacity), 0, 255), color.R, color.G, color.B);
}

/// Resolves the corner radius a scrollbar thumb shape paints with.
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
