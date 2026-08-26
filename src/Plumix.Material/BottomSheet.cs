using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/bottom_sheet.dart
// material_ui/lib/src/bottom_sheet_theme.dart

public delegate void BottomSheetDragStartHandler(DragStartDetails details);

public delegate void BottomSheetDragEndHandler(DragEndDetails details, bool isClosing);

public sealed class BottomSheet : StatefulWidget
{
    public static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(200);

    internal static readonly Curve ModalBottomSheetCurve = Curves.LegacyDecelerate;
    internal const double MinFlingVelocity = 700.0;
    internal const double CloseProgressThreshold = 0.5;

    public BottomSheet(
        Action onClosing,
        WidgetBuilder builder,
        AnimationController? animationController = null,
        bool enableDrag = true,
        bool? showDragHandle = null,
        WidgetStateColor? dragHandleColor = null,
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
    public WidgetStateColor? DragHandleColor { get; }
    public Size? DragHandleSize { get; }
    public Action<DragStartDetails>? OnDragStart { get; }
    public BottomSheetDragEndHandler? OnDragEnd { get; }
    public Color? BackgroundColor { get; }
    public Color? ShadowColor { get; }
    public double? Elevation { get; }
    public ShapeBorder? Shape { get; }
    public Clip? ClipBehavior { get; }
    public BoxConstraints? Constraints { get; }

    public static AnimationController CreateAnimationController(
        ITickerProvider? vsync = null,
        AnimationStyle? sheetAnimationStyle = null) =>
        new(duration: sheetAnimationStyle?.Duration ?? EnterDuration, vsync: vsync)
        {
            ReverseDuration = sheetAnimationStyle?.ReverseDuration ?? ExitDuration,
        };

    public override State CreateState() => new BottomSheetState();

    /// <summary>The Material 3 token defaults, used regardless of <see cref="ThemeData.UseMaterial3"/>.</summary>
    internal static BottomSheetThemeData Material3Defaults(ThemeData theme) => new(
        BackgroundColor: theme.ColorScheme.SurfaceContainerLow,
        SurfaceTintColor: Colors.Transparent,
        Elevation: 1,
        ModalElevation: 1,
        ShadowColor: Colors.Transparent,
        Shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Only(topLeft: 28.0, topRight: 28.0)),
        DragHandleColor: theme.ColorScheme.OnSurfaceVariant,
        DragHandleSize: new Size(32, 4),
        Constraints: new BoxConstraints(MaxWidth: 640));

    internal static BottomSheetThemeData ResolveDefaults(ThemeData theme) =>
        theme.UseMaterial3 ? Material3Defaults(theme) : new BottomSheetThemeData();

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

    private sealed record BottomSheetChildKey(Guid Id) : GlobalKey;

    private sealed class BottomSheetState : State
    {
        private readonly GlobalKey _childKey = new BottomSheetChildKey(Guid.NewGuid());
        private readonly HashSet<WidgetState> _dragHandleStates = [];

        private BottomSheet CurrentWidget => (BottomSheet)StateWidget;

        private double ChildHeight =>
            _childKey.CurrentContext?.FindRenderObject() is RenderBox { HasSize: true } box ? box.Size.Height : 0.0;

        private bool DismissUnderway => CurrentWidget.AnimationController?.Status == AnimationStatus.Reverse;

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var bottomSheetTheme = BottomSheetTheme.Of(context);
            var defaults = ResolveDefaults(theme);
            BoxConstraints? constraints = widget.Constraints ?? bottomSheetTheme.Constraints ?? defaults.Constraints;
            Color? color = widget.BackgroundColor ?? bottomSheetTheme.BackgroundColor ?? defaults.BackgroundColor;
            Color? surfaceTintColor = bottomSheetTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
            Color? shadowColor = widget.ShadowColor ?? bottomSheetTheme.ShadowColor ?? defaults.ShadowColor;
            double elevation = widget.Elevation ?? bottomSheetTheme.Elevation ?? defaults.Elevation ?? 0;
            ShapeBorder? shape = widget.Shape ?? bottomSheetTheme.Shape ?? defaults.Shape;
            Clip clipBehavior = widget.ClipBehavior ?? bottomSheetTheme.ClipBehavior ?? Clip.None;
            bool showDragHandle = widget.ShowDragHandle
                                  ?? (widget.EnableDrag && (bottomSheetTheme.ShowDragHandle ?? false));

            Widget? dragHandle = null;
            if (showDragHandle)
            {
                dragHandle = new DragHandle(
                    onSemanticsTap: widget.OnClosing,
                    handleHover: HandleDragHandleHover,
                    states: _dragHandleStates,
                    dragHandleColor: widget.DragHandleColor,
                    dragHandleSize: widget.DragHandleSize);

                // A drag handle is draggable on its own only when the whole sheet is not.
                if (!widget.EnableDrag)
                {
                    dragHandle = new BottomSheetGestureDetector(
                        onVerticalDragStart: HandleDragStart,
                        onVerticalDragUpdate: HandleDragUpdate,
                        onVerticalDragEnd: HandleDragEnd,
                        child: dragHandle);
                }
            }

            Widget content = widget.Builder(context);
            Widget child = showDragHandle
                ? new Stack(
                    alignment: Alignment.TopCenter,
                    children:
                    [
                        dragHandle!,
                        new Padding(new Thickness(0, WidgetConstants.MinInteractiveDimension, 0, 0), content),
                    ])
                : content;

            child = new NotificationListener<DraggableScrollableNotification>(
                onNotification: ExtentChanged,
                child: child);

            Widget bottomSheet = new Material(
                color: color,
                elevation: elevation,
                shadowColor: shadowColor,
                surfaceTintColor: surfaceTintColor,
                shape: shape,
                clipBehavior: clipBehavior,
                child: child,
                key: _childKey);

            if (constraints is not null)
            {
                bottomSheet = new Align(
                    alignment: Alignment.BottomCenter,
                    heightFactor: 1.0,
                    child: new ConstrainedBox(constraints.Value, bottomSheet));
            }

            return !widget.EnableDrag
                ? bottomSheet
                : new BottomSheetGestureDetector(
                    onVerticalDragStart: HandleDragStart,
                    onVerticalDragUpdate: HandleDragUpdate,
                    onVerticalDragEnd: HandleDragEnd,
                    child: bottomSheet);
        }

        private bool ExtentChanged(DraggableScrollableNotification notification)
        {
            if (notification.Extent == notification.MinExtent && notification.ShouldCloseOnMinExtent)
            {
                CurrentWidget.OnClosing();
            }

            return false;
        }

        private AnimationController RequireController()
        {
            return CurrentWidget.AnimationController
                   ?? throw new InvalidOperationException(
                       "BottomSheet.animationController cannot be null when BottomSheet.enableDrag or "
                       + "BottomSheet.showDragHandle is true. Use BottomSheet.CreateAnimationController to create "
                       + "one, or provide another AnimationController.");
        }

        private void HandleDragHandleHover(bool hovering)
        {
            if (hovering == _dragHandleStates.Contains(WidgetState.Hovered)) return;
            SetState(() =>
            {
                if (hovering)
                {
                    _dragHandleStates.Add(WidgetState.Hovered);
                }
                else
                {
                    _dragHandleStates.Remove(WidgetState.Hovered);
                }
            });
        }

        private void HandleDragStart(DragStartDetails details)
        {
            RequireController();
            SetState(() => _dragHandleStates.Add(WidgetState.Dragged));
            CurrentWidget.OnDragStart?.Invoke(details);
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            var controller = RequireController();
            if (DismissUnderway) return;
            double childHeight = ChildHeight;
            if (childHeight <= 0.0) return;
            controller.Stop();
            controller.SetValue(controller.Value - (details.PrimaryDelta / childHeight));
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            var controller = RequireController();
            if (DismissUnderway) return;
            SetState(() => _dragHandleStates.Remove(WidgetState.Dragged));
            bool isClosing = false;
            double childHeight = ChildHeight;
            double verticalVelocity = details.Velocity.PixelsPerSecond.Y;
            if (verticalVelocity > MinFlingVelocity)
            {
                double flingVelocity = childHeight > 0.0 ? -verticalVelocity / childHeight : -1.0;
                if (controller.Value > 0.0)
                {
                    controller.Fling(flingVelocity);
                }

                if (flingVelocity < 0.0)
                {
                    isClosing = true;
                }
            }
            else if (controller.Value < CloseProgressThreshold)
            {
                if (controller.Value > 0.0)
                {
                    controller.Fling(-1.0);
                }

                isClosing = true;
            }
            else
            {
                controller.Forward();
            }

            CurrentWidget.OnDragEnd?.Invoke(details, isClosing);
            if (isClosing)
            {
                CurrentWidget.OnClosing();
            }
        }
    }
}

internal sealed class DragHandle : StatelessWidget
{
    public DragHandle(
        Action? onSemanticsTap,
        Action<bool> handleHover,
        IReadOnlySet<WidgetState> states,
        WidgetStateColor? dragHandleColor = null,
        Size? dragHandleSize = null,
        Key? key = null) : base(key)
    {
        OnSemanticsTap = onSemanticsTap;
        HandleHover = handleHover ?? throw new ArgumentNullException(nameof(handleHover));
        States = states;
        DragHandleColor = dragHandleColor;
        DragHandleSize = dragHandleSize;
    }

    public Action? OnSemanticsTap { get; }
    public Action<bool> HandleHover { get; }
    public IReadOnlySet<WidgetState> States { get; }
    public WidgetStateColor? DragHandleColor { get; }
    public Size? DragHandleSize { get; }

    public override Widget Build(BuildContext context)
    {
        var bottomSheetTheme = BottomSheetTheme.Of(context);
        var defaults = BottomSheet.Material3Defaults(Theme.Of(context));
        Size handleSize = DragHandleSize ?? bottomSheetTheme.DragHandleSize ?? defaults.DragHandleSize!.Value;
        Color color = DragHandleColor?.Resolve(States)
                      ?? bottomSheetTheme.DragHandleColor?.Resolve(States)
                      ?? defaults.DragHandleColor!.Resolve(States);

        return new MouseRegion(
            onEnter: _ => HandleHover(true),
            onExit: _ => HandleHover(false),
            child: new Semantics(
                label: MaterialLocalizations.Of(context).ModalBarrierDismissLabel,
                flags: SemanticsFlags.IsButton,
                container: true,
                onTap: OnSemanticsTap,
                child: new SizedBox(
                    width: Math.Max(handleSize.Width, WidgetConstants.MinInteractiveDimension),
                    height: Math.Max(handleSize.Height, WidgetConstants.MinInteractiveDimension),
                    child: new Center(
                        child: new Container(
                            width: handleSize.Width,
                            height: handleSize.Height,
                            decoration: new BoxDecoration(
                                Color: color,
                                BorderRadius: BorderRadius.Circular(handleSize.Height / 2)))))));
    }
}

internal sealed class BottomSheetGestureDetector : StatelessWidget
{
    public BottomSheetGestureDetector(
        Widget child,
        Action<DragStartDetails> onVerticalDragStart,
        Action<DragUpdateDetails> onVerticalDragUpdate,
        Action<DragEndDetails> onVerticalDragEnd,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnVerticalDragStart = onVerticalDragStart;
        OnVerticalDragUpdate = onVerticalDragUpdate;
        OnVerticalDragEnd = onVerticalDragEnd;
    }

    public Widget Child { get; }
    public Action<DragStartDetails> OnVerticalDragStart { get; }
    public Action<DragUpdateDetails> OnVerticalDragUpdate { get; }
    public Action<DragEndDetails> OnVerticalDragEnd { get; }

    public override Widget Build(BuildContext context) => new RawGestureDetector(
        excludeFromSemantics: true,
        gestures: new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(VerticalDragGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<VerticalDragGestureRecognizer>(
                    () => new VerticalDragGestureRecognizer(),
                    recognizer =>
                    {
                        recognizer.OnStart = OnVerticalDragStart;
                        recognizer.OnUpdate = OnVerticalDragUpdate;
                        recognizer.OnEnd = OnVerticalDragEnd;
                        recognizer.OnlyAcceptDragOnThreshold = true;
                    }),
        },
        child: Child);
}

internal sealed class BottomSheetLayoutWithSizeListener : SingleChildRenderObjectWidget
{
    public BottomSheetLayoutWithSizeListener(
        Action<Size> onChildSizeChanged,
        double animationValue,
        bool isScrollControlled,
        double scrollControlDisabledMaxHeightRatio,
        Widget child) : base(child)
    {
        OnChildSizeChanged = onChildSizeChanged ?? throw new ArgumentNullException(nameof(onChildSizeChanged));
        AnimationValue = animationValue;
        IsScrollControlled = isScrollControlled;
        ScrollControlDisabledMaxHeightRatio = scrollControlDisabledMaxHeightRatio;
    }

    public Action<Size> OnChildSizeChanged { get; }
    public double AnimationValue { get; }
    public bool IsScrollControlled { get; }
    public double ScrollControlDisabledMaxHeightRatio { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderBottomSheetLayoutWithSizeListener(
            OnChildSizeChanged,
            AnimationValue,
            IsScrollControlled,
            ScrollControlDisabledMaxHeightRatio);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderBottomSheetLayoutWithSizeListener)renderObject;
        layout.OnChildSizeChanged = OnChildSizeChanged;
        layout.AnimationValue = AnimationValue;
        layout.IsScrollControlled = IsScrollControlled;
        layout.ScrollControlDisabledMaxHeightRatio = ScrollControlDisabledMaxHeightRatio;
    }
}

internal sealed class RenderBottomSheetLayoutWithSizeListener : RenderProxyBox
{
    private Size _lastSize;
    private double _animationValue;
    private bool _isScrollControlled;
    private double _scrollControlDisabledMaxHeightRatio;

    public RenderBottomSheetLayoutWithSizeListener(
        Action<Size> onChildSizeChanged,
        double animationValue,
        bool isScrollControlled,
        double scrollControlDisabledMaxHeightRatio)
    {
        OnChildSizeChanged = onChildSizeChanged;
        _animationValue = animationValue;
        _isScrollControlled = isScrollControlled;
        _scrollControlDisabledMaxHeightRatio = scrollControlDisabledMaxHeightRatio;
    }

    public Action<Size> OnChildSizeChanged { get; set; }

    public double AnimationValue
    {
        get => _animationValue;
        set
        {
            if (Math.Abs(_animationValue - value) <= double.Epsilon) return;
            _animationValue = value;
            MarkNeedsLayout();
        }
    }

    public bool IsScrollControlled
    {
        get => _isScrollControlled;
        set
        {
            if (_isScrollControlled == value) return;
            _isScrollControlled = value;
            MarkNeedsLayout();
        }
    }

    public double ScrollControlDisabledMaxHeightRatio
    {
        get => _scrollControlDisabledMaxHeightRatio;
        set
        {
            if (Math.Abs(_scrollControlDisabledMaxHeightRatio - value) <= double.Epsilon) return;
            _scrollControlDisabledMaxHeightRatio = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) => 0.0;

    protected override double ComputeMaxIntrinsicWidth(double height) => 0.0;

    protected override double ComputeMinIntrinsicHeight(double width) => 0.0;

    protected override double ComputeMaxIntrinsicHeight(double width) => 0.0;

    protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Biggest;

    protected override void PerformLayout()
    {
        Size = Constraints.Biggest;
        if (Child is null) return;
        BoxConstraints childConstraints = GetConstraintsForChild(Constraints);
        Child.Layout(childConstraints, parentUsesSize: !childConstraints.IsTight);
        Size childSize = childConstraints.IsTight ? childConstraints.Smallest : Child.Size;
        ((BoxParentData)Child.parentData!).offset = GetPositionForChild(Size, childSize);
        if (_lastSize == childSize) return;
        _lastSize = childSize;
        OnChildSizeChanged(_lastSize);
    }

    private BoxConstraints GetConstraintsForChild(BoxConstraints constraints) => new(
        MinWidth: constraints.MaxWidth,
        MaxWidth: constraints.MaxWidth,
        MaxHeight: IsScrollControlled
            ? constraints.MaxHeight
            : constraints.MaxHeight * ScrollControlDisabledMaxHeightRatio);

    private Point GetPositionForChild(Size size, Size childSize) =>
        new(0.0, size.Height - (childSize.Height * AnimationValue));
}

internal sealed class ModalBottomSheetWidget<T> : StatefulWidget
{
    public ModalBottomSheetWidget(
        ModalBottomSheetRoute<T> route,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        bool isScrollControlled = false,
        double scrollControlDisabledMaxHeightRatio =
            ModalBottomSheetRoute<T>.DefaultScrollControlDisabledMaxHeightRatio,
        bool enableDrag = true,
        bool showDragHandle = false,
        AnimationStyle? animationStyle = null,
        Key? key = null) : base(key)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        Shape = shape;
        ClipBehavior = clipBehavior;
        Constraints = constraints;
        IsScrollControlled = isScrollControlled;
        ScrollControlDisabledMaxHeightRatio = scrollControlDisabledMaxHeightRatio;
        EnableDrag = enableDrag;
        ShowDragHandle = showDragHandle;
        AnimationStyle = animationStyle;
    }

    public ModalBottomSheetRoute<T> Route { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public ShapeBorder? Shape { get; }
    public Clip? ClipBehavior { get; }
    public BoxConstraints? Constraints { get; }
    public bool IsScrollControlled { get; }
    public double ScrollControlDisabledMaxHeightRatio { get; }
    public bool EnableDrag { get; }
    public bool ShowDragHandle { get; }
    public AnimationStyle? AnimationStyle { get; }

    public override State CreateState() => new ModalBottomSheetWidgetState<T>();
}

internal sealed class ModalBottomSheetWidgetState<T> : State
{
    private static readonly Animation<double> AlwaysDismissedAnimation =
        new ConstantAnimation<double>(0.0, AnimationStatus.Dismissed);

    private CurvedAnimation _curvedSheetAnimation = null!;
    private ProxyAnimation _sheetAnimation = null!;

    private ModalBottomSheetWidget<T> CurrentWidget => (ModalBottomSheetWidget<T>)StateWidget;

    public override void InitState()
    {
        base.InitState();
        var style = CurrentWidget.AnimationStyle;
        _curvedSheetAnimation = new CurvedAnimation(
            CurrentWidget.Route.Animation,
            curve: style?.Curve ?? BottomSheet.ModalBottomSheetCurve,
            reverseCurve: style?.ReverseCurve ?? BottomSheet.ModalBottomSheetCurve);
        _sheetAnimation = new ProxyAnimation(_curvedSheetAnimation);
    }

    public override void Dispose()
    {
        _sheetAnimation.Parent = AlwaysDismissedAnimation;
        _curvedSheetAnimation.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var route = widget.Route;
        string routeLabel = GetRouteLabel(MaterialLocalizations.Of(context));

        return new AnimatedBuilder(
            animation: _sheetAnimation,
            child: new BottomSheet(
                animationController: route.SheetAnimationController,
                onClosing: () =>
                {
                    if (route.IsCurrent)
                    {
                        Navigator.Pop(context);
                    }
                },
                builder: route.Builder,
                backgroundColor: widget.BackgroundColor,
                elevation: widget.Elevation,
                shape: widget.Shape,
                clipBehavior: widget.ClipBehavior,
                constraints: widget.Constraints,
                enableDrag: widget.EnableDrag,
                showDragHandle: widget.ShowDragHandle,
                onDragStart: HandleDragStart,
                onDragEnd: HandleDragEnd),
            builder: (_, child) => new Semantics(
                label: routeLabel,
                scopesRoute: true,
                namesRoute: true,
                explicitChildNodes: true,
                child: new ClipRect(
                    child: new BottomSheetLayoutWithSizeListener(
                        onChildSizeChanged: size =>
                            route.DidChangeBarrierSemanticsClip(new Thickness(0, 0, 0, size.Height)),
                        animationValue: _sheetAnimation.Value,
                        isScrollControlled: widget.IsScrollControlled,
                        scrollControlDisabledMaxHeightRatio: widget.ScrollControlDisabledMaxHeightRatio,
                        child: child!))));
    }

    private static string GetRouteLabel(MaterialLocalizations localizations) =>
        PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? string.Empty
            : localizations.DialogLabel;

    private void HandleDragStart(DragStartDetails details)
    {
        // Allow the sheet to track the user's finger accurately.
        _sheetAnimation.Parent = CurrentWidget.Route.Animation;
    }

    private void HandleDragEnd(DragEndDetails details, bool isClosing)
    {
        // Smooth out the animation from the current position of the sheet.
        var style = CurrentWidget.AnimationStyle;
        double currentProgress = CurrentWidget.Route.Animation.Value;
        _sheetAnimation.Parent = new CurvedAnimation(
            CurrentWidget.Route.Animation,
            curve: Curves.Split(
                currentProgress,
                endCurve: style?.Curve ?? BottomSheet.ModalBottomSheetCurve),
            reverseCurve: Curves.Split(
                currentProgress,
                endCurve: style?.ReverseCurve ?? BottomSheet.ModalBottomSheetCurve));
    }
}

public sealed class ModalBottomSheetRoute<T> : PopupRoute
{
    public const double DefaultScrollControlDisabledMaxHeightRatio = 9.0 / 16.0;

    private readonly ValueNotifier<Thickness> _clipDetailsNotifier = new(default);
    private readonly CapturedThemes _capturedThemes;
    private readonly BottomSheetThemeData _capturedBottomSheetTheme;
    private readonly AnimationController? _transitionAnimationController;
    private readonly AnimationStyle? _sheetAnimationStyle;
    private readonly TaskCompletionSource<T?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private AnimationController? _animationController;

    public ModalBottomSheetRoute(
        BuildContext context,
        WidgetBuilder builder,
        bool isScrollControlled,
        double scrollControlDisabledMaxHeightRatio = DefaultScrollControlDisabledMaxHeightRatio,
        string? barrierLabel = null,
        string? barrierOnTapHint = null,
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
        Point? anchorPoint = null,
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
        AnchorPoint = anchorPoint;
        var localizations = MaterialLocalizations.Of(context);
        BarrierLabel = barrierLabel ?? localizations.ScrimLabel;
        BarrierOnTapHint = barrierOnTapHint ?? localizations.ScrimOnTapHint(localizations.BottomSheetLabel);
        _capturedBottomSheetTheme = BottomSheetTheme.Of(context);
        _capturedThemes = InheritedTheme.Capture(context, Widgets.Navigator.Of(context).Context);
        _transitionAnimationController = transitionAnimationController;
        _sheetAnimationStyle = sheetAnimationStyle;
    }

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
    public Point? AnchorPoint { get; }
    public override string? BarrierLabel { get; }
    public string BarrierOnTapHint { get; }
    public Task<T?> Completed => _completed.Task;

    public override bool BarrierDismissible => IsDismissible;

    public override Color? BarrierColor => ModalBarrierColor
                                           ?? _capturedBottomSheetTheme.ModalBarrierColor
                                           ?? Color.FromArgb(0x8A, 0, 0, 0);

    public override TimeSpan TransitionDuration => _transitionAnimationController?.Duration
                                                   ?? _sheetAnimationStyle?.Duration
                                                   ?? BottomSheet.EnterDuration;

    public override TimeSpan ReverseTransitionDuration => _transitionAnimationController?.ReverseDuration
                                                          ?? _transitionAnimationController?.Duration
                                                          ?? _sheetAnimationStyle?.ReverseDuration
                                                          ?? BottomSheet.ExitDuration;

    internal AnimationController SheetAnimationController => _animationController
        ?? throw new InvalidOperationException("The bottom sheet animation is unavailable before it is installed.");

    protected override bool WillDisposeAnimationController => _transitionAnimationController is null;

    protected override AnimationController CreateAnimationController()
    {
        _animationController = _transitionAnimationController
                               ?? BottomSheet.CreateAnimationController(Navigator, _sheetAnimationStyle);
        return _animationController;
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
        Widget content = new DisplayFeatureSubScreen(
            anchorPoint: AnchorPoint,
            child: new Builder(routeContext =>
            {
                var sheetTheme = BottomSheetTheme.Of(routeContext);
                var defaults = BottomSheet.ResolveDefaults(Theme.Of(routeContext));
                return new ModalBottomSheetWidget<T>(
                    route: this,
                    backgroundColor: BackgroundColor
                                     ?? sheetTheme.ModalBackgroundColor
                                     ?? sheetTheme.BackgroundColor
                                     ?? defaults.BackgroundColor,
                    elevation: Elevation
                               ?? sheetTheme.ModalElevation
                               ?? sheetTheme.Elevation
                               ?? defaults.ModalElevation,
                    shape: Shape,
                    clipBehavior: ClipBehavior,
                    constraints: Constraints,
                    isScrollControlled: IsScrollControlled,
                    scrollControlDisabledMaxHeightRatio: ScrollControlDisabledMaxHeightRatio,
                    enableDrag: EnableDrag,
                    showDragHandle: ShowDragHandle ?? (EnableDrag && (sheetTheme.ShowDragHandle ?? false)),
                    animationStyle: _sheetAnimationStyle);
            }));

        Widget bottomSheet = UseSafeArea
            ? new SafeArea(bottom: false, child: content)
            : MediaQuery.RemovePadding(context, content, removeTop: true);

        // Prevent clicks inside the bottom sheet from passing through to the barrier.
        bottomSheet = new Semantics(hitTestBehavior: SemanticsHitTestBehavior.Opaque, child: bottomSheet);

        return _capturedThemes.Wrap(bottomSheet);
    }

    public override void Dispose()
    {
        _clipDetailsNotifier.Dispose();
        if (!_completed.Task.IsCompleted) _completed.TrySetResult(default);
        base.Dispose();
    }

    internal void DidChangeBarrierSemanticsClip(Thickness clipDetails)
    {
        if (_clipDetailsNotifier.Value == clipDetails) return;
        _clipDetailsNotifier.Value = clipDetails;
    }

    public override Widget BuildModalBarrier()
    {
        if (BarrierColor is { A: not 0 } barrierColor)
        {
            return new AnimatedModalBarrier(
                color: CreateBarrierColorAnimation(barrierColor),
                dismissible: BarrierDismissible,
                semanticsLabel: BarrierLabel,
                barrierSemanticsDismissible: SemanticsDismissible,
                clipDetailsNotifier: _clipDetailsNotifier,
                semanticsOnTapHint: BarrierOnTapHint);
        }

        return new ModalBarrier(
            dismissible: BarrierDismissible,
            semanticsLabel: BarrierLabel,
            barrierSemanticsDismissible: SemanticsDismissible,
            clipDetailsNotifier: _clipDetailsNotifier,
            semanticsOnTapHint: BarrierOnTapHint);
    }
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
        Point? anchorPoint = null,
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
            barrierOnTapHint: null,
            backgroundColor,
            elevation,
            shape,
            clipBehavior,
            constraints,
            barrierColor ?? Theme.Of(context).BottomSheetTheme.ModalBarrierColor,
            isDismissible,
            enableDrag,
            showDragHandle,
            useSafeArea,
            anchorPoint,
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
