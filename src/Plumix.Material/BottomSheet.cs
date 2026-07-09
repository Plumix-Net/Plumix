using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/bottom_sheet.dart
// flutter/packages/flutter/lib/src/material/bottom_sheet_theme.dart

public delegate void BottomSheetDragEndHandler(DragEndDetails details, bool isClosing);

public sealed class BottomSheet : StatefulWidget
{
    public static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(200);

    public BottomSheet(
        Action onClosing,
        WidgetBuilder builder,
        AnimationController? animationController = null,
        bool enableDrag = true,
        bool? showDragHandle = null,
        Color? dragHandleColor = null,
        Size? dragHandleSize = null,
        Action<DragStartDetails>? onDragStart = null,
        BottomSheetDragEndHandler? onDragEnd = null,
        Color? backgroundColor = null,
        Color? shadowColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(onClosing);
        ArgumentNullException.ThrowIfNull(builder);
        ValidateElevation(elevation, nameof(elevation));
        ValidateHandleSize(dragHandleSize);
        OnClosing = onClosing;
        Builder = builder;
        AnimationController = animationController;
        EnableDrag = enableDrag;
        ShowDragHandle = showDragHandle;
        DragHandleColor = dragHandleColor;
        DragHandleSize = dragHandleSize;
        OnDragStart = onDragStart;
        OnDragEnd = onDragEnd;
        BackgroundColor = backgroundColor;
        ShadowColor = shadowColor;
        Elevation = elevation;
        Shape = shape;
        ClipBehavior = clipBehavior;
        Constraints = constraints;
    }

    public AnimationController? AnimationController { get; }
    public Action OnClosing { get; }
    public WidgetBuilder Builder { get; }
    public bool EnableDrag { get; }
    public bool? ShowDragHandle { get; }
    public Color? DragHandleColor { get; }
    public Size? DragHandleSize { get; }
    public Action<DragStartDetails>? OnDragStart { get; }
    public BottomSheetDragEndHandler? OnDragEnd { get; }
    public Color? BackgroundColor { get; }
    public Color? ShadowColor { get; }
    public double? Elevation { get; }
    public ShapeBorder? Shape { get; }
    public Clip? ClipBehavior { get; }
    public BoxConstraints? Constraints { get; }

    public static AnimationController CreateAnimationController(AnimationStyle? sheetAnimationStyle = null) =>
        new(sheetAnimationStyle?.Duration ?? EnterDuration)
        {
            Curve = sheetAnimationStyle?.Curve ?? Curves.EaseOut,
        };

    public override State CreateState() => new BottomSheetState();

    internal static BottomSheetThemeData ResolveDefaults(ThemeData theme)
    {
        if (!theme.UseMaterial3) return new BottomSheetThemeData();
        return new BottomSheetThemeData(
            BackgroundColor: theme.SurfaceContainerLowColor,
            SurfaceTintColor: Colors.Transparent,
            Elevation: 1,
            ModalElevation: 1,
            ShadowColor: Colors.Transparent,
            Shape: ShapeBorder.RoundedRectangle(28),
            DragHandleColor: MaterialStateProperty<Color?>.All(theme.OnSurfaceVariantColor),
            DragHandleSize: new Size(32, 4),
            Constraints: new BoxConstraints(MaxWidth: 640));
    }

    private static void ValidateElevation(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateHandleSize(Size? value)
    {
        if (value.HasValue
            && (!double.IsFinite(value.Value.Width) || !double.IsFinite(value.Value.Height)
                || value.Value.Width < 0 || value.Value.Height < 0))
            throw new ArgumentOutOfRangeException(nameof(value));
    }

    private sealed class BottomSheetState : State
    {
        private const double MinFlingVelocity = 700;
        private const double CloseProgressThreshold = 0.5;
        private double _childHeight = 1;
        private bool _dragged;
        private BottomSheet CurrentWidget => (BottomSheet)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var sheetTheme = BottomSheetTheme.Of(context);
            var defaults = ResolveDefaults(theme);
            var constraints = widget.Constraints ?? sheetTheme.Constraints ?? defaults.Constraints;
            var color = widget.BackgroundColor ?? sheetTheme.BackgroundColor ?? defaults.BackgroundColor ?? theme.CanvasColor;
            var surfaceTint = sheetTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
            double elevation = widget.Elevation ?? sheetTheme.Elevation ?? defaults.Elevation ?? 0;
            var shadow = widget.ShadowColor ?? sheetTheme.ShadowColor ?? defaults.ShadowColor ?? Colors.Transparent;
            var shape = widget.Shape ?? sheetTheme.Shape ?? defaults.Shape ?? ShapeBorder.RoundedRectangle(0);
            var clip = widget.ClipBehavior ?? sheetTheme.ClipBehavior ?? Clip.None;
            bool showHandle = widget.ShowDragHandle ?? (widget.EnableDrag && (sheetTheme.ShowDragHandle ?? false));

            if (surfaceTint.HasValue && surfaceTint.Value.A > 0 && elevation > 0)
            {
                color = NavigationSurfaceUtilities.ApplySurfaceTint(color, surfaceTint.Value, elevation);
            }

            Widget child = widget.Builder(context);
            if (showHandle)
            {
                child = new Stack(
                    alignment: Alignment.TopCenter,
                    children:
                    [
                        new Padding(new Thickness(0, 48, 0, 0), child),
                        BuildDragHandle(context, sheetTheme, defaults, widget.EnableDrag),
                    ]);
            }

            Widget surface = new DecoratedBox(
                new BoxDecoration(
                    Color: color,
                    Border: shape.Side,
                    BorderRadius: shape.BorderRadius,
                    BoxShadows: BuildBoxShadows(shadow, elevation)),
                child);
            if (clip != Clip.None) surface = new ClipRRect(shape.BorderRadius, surface);
            surface = new BottomSheetMeasure(size => _childHeight = Math.Max(1, size.Height), surface);

            if (constraints is not null)
            {
                surface = new Align(
                    alignment: Alignment.BottomCenter,
                    heightFactor: 1,
                    child: new ConstrainedBox(constraints.Value, surface));
            }

            return widget.EnableDrag ? WrapDrag(surface) : surface;
        }

        private Widget BuildDragHandle(
            BuildContext context,
            BottomSheetThemeData sheetTheme,
            BottomSheetThemeData defaults,
            bool wholeSheetDraggable)
        {
            var size = CurrentWidget.DragHandleSize ?? sheetTheme.DragHandleSize ?? defaults.DragHandleSize ?? new Size(32, 4);
            var states = _dragged ? MaterialState.Dragged : MaterialState.None;
            var color = CurrentWidget.DragHandleColor
                        ?? sheetTheme.DragHandleColor?.Resolve(states)
                        ?? defaults.DragHandleColor?.Resolve(states)
                        ?? Theme.Of(context).OnSurfaceVariantColor;
            Widget handle = new Semantics(
                label: MaterialLocalizations.Of(context).ModalBarrierDismissLabel,
                flags: SemanticsFlags.IsButton,
                container: true,
                onTap: CurrentWidget.OnClosing,
                child: new SizedBox(
                    width: Math.Max(size.Width, 48),
                    height: Math.Max(size.Height, 48),
                    child: new Center(
                        child: new Container(
                            width: size.Width,
                            height: size.Height,
                            decoration: new BoxDecoration(
                                Color: color,
                                BorderRadius: BorderRadius.Circular(size.Height / 2))))));
            return wholeSheetDraggable ? handle : WrapDrag(handle);
        }

        private Widget WrapDrag(Widget child) => new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            onVerticalDragStart: HandleDragStart,
            onVerticalDragUpdate: HandleDragUpdate,
            onVerticalDragEnd: HandleDragEnd,
            onVerticalDragCancel: HandleDragCancel,
            child: child);

        private AnimationController RequireController()
        {
            return CurrentWidget.AnimationController
                   ?? throw new InvalidOperationException(
                       "BottomSheet.animationController cannot be null when dragging is enabled or a drag handle is shown.");
        }

        private void HandleDragStart(DragStartDetails details)
        {
            RequireController();
            SetState(() => _dragged = true);
            CurrentWidget.OnDragStart?.Invoke(details);
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            var controller = RequireController();
            controller.Stop();
            controller.SetValue(controller.Value - (details.PrimaryDelta / _childHeight));
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            var controller = RequireController();
            bool isClosing = details.PrimaryVelocity > MinFlingVelocity
                             || (Math.Abs(details.PrimaryVelocity) <= MinFlingVelocity
                                 && controller.Value < CloseProgressThreshold);
            SetState(() => _dragged = false);
            if (isClosing) controller.Reverse();
            else controller.Forward();
            CurrentWidget.OnDragEnd?.Invoke(details, isClosing);
            if (isClosing) CurrentWidget.OnClosing();
        }

        private void HandleDragCancel()
        {
            SetState(() => _dragged = false);
            RequireController().Forward();
        }

        private static BoxShadows? BuildBoxShadows(Color color, double elevation)
        {
            if (color.A == 0 || elevation <= 0) return null;
            Color WithOpacity(double opacity) => Color.FromArgb(
                (byte)Math.Round(color.A * opacity), color.R, color.G, color.B);
            return new BoxShadows(
                new BoxShadow { OffsetY = elevation * 0.5, Blur = elevation * 2.4, Color = WithOpacity(0.20) },
                [new BoxShadow { OffsetY = elevation * 0.25, Blur = elevation * 3.2, Color = WithOpacity(0.14) }]);
        }
    }
}

internal sealed class BottomSheetMeasure : SingleChildRenderObjectWidget
{
    public BottomSheetMeasure(Action<Size> onSizeChanged, Widget child) : base(child) => OnSizeChanged = onSizeChanged;
    public Action<Size> OnSizeChanged { get; }
    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderBottomSheetMeasure(OnSizeChanged);
    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject) =>
        ((RenderBottomSheetMeasure)renderObject).OnSizeChanged = OnSizeChanged;
}

internal sealed class RenderBottomSheetMeasure : RenderProxyBox
{
    private Size _lastSize = new(double.NaN, double.NaN);
    public RenderBottomSheetMeasure(Action<Size> onSizeChanged) => OnSizeChanged = onSizeChanged;
    public Action<Size> OnSizeChanged { get; set; }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        if (_lastSize == Size) return;
        _lastSize = Size;
        OnSizeChanged(Size);
    }
}

public sealed class ModalBottomSheetRoute<T> : PageRoute
{
    public const double DefaultScrollControlDisabledMaxHeightRatio = 9.0 / 16.0;
    private readonly ThemeData _capturedTheme;
    private readonly BottomSheetThemeData _capturedBottomSheetTheme;
    private readonly MediaQueryData _capturedMediaQuery;
    private readonly TextDirection _capturedDirection;
    private readonly AnimationController _animation;
    private readonly bool _ownsAnimation;
    private readonly TaskCompletionSource<T?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TimeSpan _enterDuration;
    private readonly TimeSpan _exitDuration;
    private readonly Curve _forwardCurve;
    private readonly Curve _reverseCurve;
    private object? _pendingResult;
    private bool _isExiting;

    public ModalBottomSheetRoute(
        BuildContext context,
        WidgetBuilder builder,
        bool isScrollControlled,
        double scrollControlDisabledMaxHeightRatio = DefaultScrollControlDisabledMaxHeightRatio,
        string? barrierLabel = null,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        Color? modalBarrierColor = null,
        bool isDismissible = true,
        bool enableDrag = true,
        bool? showDragHandle = null,
        bool useSafeArea = false,
        RouteSettings? settings = null,
        AnimationController? transitionAnimationController = null,
        AnimationStyle? sheetAnimationStyle = null) : base(settings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (!double.IsFinite(scrollControlDisabledMaxHeightRatio) || scrollControlDisabledMaxHeightRatio <= 0)
            throw new ArgumentOutOfRangeException(nameof(scrollControlDisabledMaxHeightRatio));
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        Builder = builder;
        IsScrollControlled = isScrollControlled;
        ScrollControlDisabledMaxHeightRatio = scrollControlDisabledMaxHeightRatio;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        Shape = shape;
        ClipBehavior = clipBehavior;
        Constraints = constraints;
        ModalBarrierColor = modalBarrierColor;
        IsDismissible = isDismissible;
        EnableDrag = enableDrag;
        ShowDragHandle = showDragHandle;
        UseSafeArea = useSafeArea;
        BarrierLabel = barrierLabel ?? MaterialLocalizations.Of(context).ModalBarrierDismissLabel;
        _capturedTheme = Theme.Of(context);
        _capturedBottomSheetTheme = BottomSheetTheme.Of(context);
        _capturedMediaQuery = MediaQuery.Of(context);
        _capturedDirection = Directionality.Of(context);
        _enterDuration = transitionAnimationController?.Duration
                         ?? sheetAnimationStyle?.Duration
                         ?? BottomSheet.EnterDuration;
        _exitDuration = transitionAnimationController?.Duration
                        ?? sheetAnimationStyle?.ReverseDuration
                        ?? BottomSheet.ExitDuration;
        _forwardCurve = sheetAnimationStyle?.Curve ?? Curves.EaseOut;
        _reverseCurve = sheetAnimationStyle?.ReverseCurve ?? Curves.EaseOut;
        _animation = transitionAnimationController ?? BottomSheet.CreateAnimationController(sheetAnimationStyle);
        _ownsAnimation = transitionAnimationController is null;
        _animation.Curve = _forwardCurve;
        _animation.Changed += HandleAnimationChanged;
        _animation.Dismissed += HandleDismissed;
    }

    public override bool Opaque => false;
    public WidgetBuilder Builder { get; }
    public bool IsScrollControlled { get; }
    public double ScrollControlDisabledMaxHeightRatio { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public ShapeBorder? Shape { get; }
    public Clip? ClipBehavior { get; }
    public BoxConstraints? Constraints { get; }
    public Color? ModalBarrierColor { get; }
    public bool IsDismissible { get; }
    public bool EnableDrag { get; }
    public bool? ShowDragHandle { get; }
    public bool UseSafeArea { get; }
    public string BarrierLabel { get; }
    public Task<T?> Completed => _completed.Task;
    internal AnimationController Animation => _animation;

    protected override void OnAttach()
    {
        _animation.Duration = _enterDuration;
        _animation.Curve = _forwardCurve;
        _animation.Forward(from: 0);
    }

    public override bool WillPop(object? result)
    {
        if (_isExiting || _animation.Value <= 0) return base.WillPop(result);
        _pendingResult = result;
        _isExiting = true;
        _animation.Duration = _exitDuration;
        _animation.Curve = _reverseCurve;
        _animation.Reverse();
        return false;
    }

    public override void DidComplete(object? result)
    {
        if (result is null) _completed.TrySetResult(default);
        else if (result is T typed) _completed.TrySetResult(typed);
        else _completed.TrySetException(new InvalidCastException(
            $"Bottom sheet result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}."));
    }

    public override Widget BuildPage(BuildContext context)
    {
        double progress = Math.Clamp(_animation.Evaluate(), 0, 1);
        var barrierColor = ModalBarrierColor ?? _capturedBottomSheetTheme.ModalBarrierColor ?? Color.FromArgb(0x8A, 0, 0, 0);
        Widget barrier = new Semantics(
            label: BarrierLabel,
            container: true,
            onTap: IsDismissible ? () => Navigator?.MaybePop() : null,
            child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: IsDismissible ? () => Navigator?.MaybePop() : null,
                child: new ColoredBox(ApplyOpacity(barrierColor, progress))));

        var modalBackground = BackgroundColor ?? _capturedBottomSheetTheme.ModalBackgroundColor;
        double? modalElevation = Elevation ?? _capturedBottomSheetTheme.ModalElevation;
        Widget sheet = new BottomSheet(
            animationController: _animation,
            onClosing: () => Navigator?.MaybePop(),
            builder: Builder,
            backgroundColor: modalBackground,
            elevation: modalElevation,
            shape: Shape,
            clipBehavior: ClipBehavior,
            constraints: Constraints,
            enableDrag: EnableDrag,
            showDragHandle: ShowDragHandle);
        if (UseSafeArea)
        {
            sheet = new SafeArea(left: true, top: true, right: true, bottom: false, child: sheet);
        }
        else
        {
            sheet = MediaQuery.RemovePadding(context, sheet, removeTop: true);
        }

        sheet = new Semantics(
            label: _capturedTheme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? null
                : MaterialLocalizations.Of(context).DialogLabel,
            scopesRoute: true,
            namesRoute: true,
            explicitChildNodes: true,
            child: new ModalBottomSheetLayout(
                progress,
                IsScrollControlled,
                ScrollControlDisabledMaxHeightRatio,
                sheet));
        sheet = new BottomSheetTheme(_capturedBottomSheetTheme, sheet);
        sheet = new Theme(_capturedTheme, sheet);
        sheet = new MediaQuery(_capturedMediaQuery, sheet);
        sheet = new Directionality(_capturedDirection, sheet);
        return new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: barrier),
                sheet,
            ]);
    }

    public override void Dispose()
    {
        _animation.Changed -= HandleAnimationChanged;
        _animation.Dismissed -= HandleDismissed;
        if (_ownsAnimation) _animation.Dispose();
        if (!_completed.Task.IsCompleted) _completed.TrySetResult(default);
        base.Dispose();
    }

    private void HandleAnimationChanged() => NotifyRouteChanged();
    private void HandleDismissed()
    {
        if (_isExiting) Navigator?.MaybePop(_pendingResult);
    }
    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * opacity), color.R, color.G, color.B);
}

internal sealed class ModalBottomSheetLayout : SingleChildRenderObjectWidget
{
    public ModalBottomSheetLayout(double animationValue, bool isScrollControlled, double maxHeightRatio, Widget child)
        : base(child)
    {
        AnimationValue = animationValue;
        IsScrollControlled = isScrollControlled;
        MaxHeightRatio = maxHeightRatio;
    }
    public double AnimationValue { get; }
    public bool IsScrollControlled { get; }
    public double MaxHeightRatio { get; }
    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderModalBottomSheetLayout(AnimationValue, IsScrollControlled, MaxHeightRatio);
    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderModalBottomSheetLayout)renderObject;
        layout.AnimationValue = AnimationValue;
        layout.IsScrollControlled = IsScrollControlled;
        layout.MaxHeightRatio = MaxHeightRatio;
    }
}

internal sealed class RenderModalBottomSheetLayout : RenderProxyBox
{
    private double _animationValue;
    private bool _isScrollControlled;
    private double _maxHeightRatio;
    public RenderModalBottomSheetLayout(double animationValue, bool isScrollControlled, double maxHeightRatio)
    {
        _animationValue = animationValue;
        _isScrollControlled = isScrollControlled;
        _maxHeightRatio = maxHeightRatio;
    }
    public double AnimationValue { get => _animationValue; set { if (Math.Abs(_animationValue - value) > 0.000001) { _animationValue = value; MarkNeedsLayout(); } } }
    public bool IsScrollControlled { get => _isScrollControlled; set { if (_isScrollControlled != value) { _isScrollControlled = value; MarkNeedsLayout(); } } }
    public double MaxHeightRatio { get => _maxHeightRatio; set { if (Math.Abs(_maxHeightRatio - value) > 0.000001) { _maxHeightRatio = value; MarkNeedsLayout(); } } }

    protected override void PerformLayout()
    {
        Size = Constraints.Biggest;
        if (Child is null) return;
        double maxHeight = IsScrollControlled ? Constraints.MaxHeight : Constraints.MaxHeight * MaxHeightRatio;
        Child.Layout(new BoxConstraints(
            MinWidth: Constraints.MaxWidth,
            MaxWidth: Constraints.MaxWidth,
            MaxHeight: maxHeight), parentUsesSize: true);
        ((BoxParentData)Child.parentData!).offset = new Point(0, Size.Height - (Child.Size.Height * AnimationValue));
    }
}

public sealed class PersistentBottomSheetController
{
    private readonly Action _close;
    private readonly Action<Action> _setState;

    internal PersistentBottomSheetController(Action close, Action<Action> setState, Task closed)
    {
        _close = close;
        _setState = setState;
        Closed = closed;
    }

    public Task Closed { get; }

    public void Close() => _close();

    public void SetState(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _setState(callback);
    }
}

internal sealed class PersistentBottomSheetPresentation
{
    public required WidgetBuilder Builder { get; init; }
    public required AnimationController Animation { get; init; }
    public required bool OwnsAnimation { get; init; }
    public required TimeSpan ExitDuration { get; init; }
    public required bool EnableDrag { get; init; }
    public bool? ShowDragHandle { get; init; }
    public Color? BackgroundColor { get; init; }
    public double? Elevation { get; init; }
    public ShapeBorder? Shape { get; init; }
    public Clip? ClipBehavior { get; init; }
    public BoxConstraints? Constraints { get; init; }
    public LocalHistoryEntry? HistoryEntry { get; set; }
    public TaskCompletionSource<object?> Closed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Closing { get; set; }
}

public static class MaterialBottomSheets
{
    public static Task<T?> ShowModalBottomSheet<T>(
        BuildContext context,
        WidgetBuilder builder,
        Color? backgroundColor = null,
        string? barrierLabel = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        Color? barrierColor = null,
        bool isScrollControlled = false,
        double scrollControlDisabledMaxHeightRatio = ModalBottomSheetRoute<T>.DefaultScrollControlDisabledMaxHeightRatio,
        bool useRootNavigator = false,
        bool isDismissible = true,
        bool enableDrag = true,
        bool? showDragHandle = null,
        bool useSafeArea = false,
        RouteSettings? routeSettings = null,
        AnimationController? transitionAnimationController = null,
        AnimationStyle? sheetAnimationStyle = null)
    {
        var route = new ModalBottomSheetRoute<T>(
            context,
            builder,
            isScrollControlled,
            scrollControlDisabledMaxHeightRatio,
            barrierLabel,
            backgroundColor,
            elevation,
            shape,
            clipBehavior,
            constraints,
            barrierColor,
            isDismissible,
            enableDrag,
            showDragHandle,
            useSafeArea,
            routeSettings,
            transitionAnimationController,
            sheetAnimationStyle);
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(route);
        return route.Completed;
    }

    public static PersistentBottomSheetController ShowBottomSheet(
        BuildContext context,
        WidgetBuilder builder,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        bool? enableDrag = null,
        bool? showDragHandle = null,
        AnimationController? transitionAnimationController = null,
        AnimationStyle? sheetAnimationStyle = null) =>
        Scaffold.Of(context).ShowBottomSheet(
            builder,
            backgroundColor,
            elevation,
            shape,
            clipBehavior,
            constraints,
            enableDrag,
            showDragHandle,
            transitionAnimationController,
            sheetAnimationStyle);
}
