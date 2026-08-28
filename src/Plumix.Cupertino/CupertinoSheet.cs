using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/sheet.dart

/// <summary>The iOS sheet transition used by <see cref="CupertinoSheetRoute{T}"/>.</summary>
public sealed class CupertinoSheetTransition : StatefulWidget
{
    public const double DefaultTopGap = 0.08;
    internal const double StretchedTopGap = 0.072;
    internal const double SheetScaleFactor = 0.0835;

    public CupertinoSheetTransition(
        Animation<double> primaryRouteAnimation,
        Animation<double> secondaryRouteAnimation,
        Widget child,
        bool linearTransition,
        double topGap = DefaultTopGap,
        Key? key = null) : base(key)
    {
        PrimaryRouteAnimation = primaryRouteAnimation
                                ?? throw new ArgumentNullException(nameof(primaryRouteAnimation));
        SecondaryRouteAnimation = secondaryRouteAnimation
                                  ?? throw new ArgumentNullException(nameof(secondaryRouteAnimation));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        LinearTransition = linearTransition;
        TopGap = topGap;
    }

    public Animation<double> PrimaryRouteAnimation { get; }

    public Animation<double> SecondaryRouteAnimation { get; }

    public Widget Child { get; }

    public bool LinearTransition { get; }

    public double TopGap { get; }

    public static Widget? DelegateTransition(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        bool allowSnapshotting,
        Widget? child)
    {
        _ = animation;
        _ = allowSnapshotting;
        if (child is null)
        {
            return null;
        }

        var curved = new CurvedAnimation(
            secondaryAnimation,
            Navigator.Of(context).UserGestureInProgress ? Curves.Linear : Curves.LinearToEaseOut,
            Navigator.Of(context).UserGestureInProgress ? Curves.Linear : Curves.EaseInToLinear);
        Animation<Vector> position = curved.Drive(new VectorTween(
            begin: Vector.Zero,
            end: CupertinoSheetRoute<dynamic>.HasParentSheet(context)
                ? new Vector(0.0, -0.005)
                : new Vector(0.0, 0.07)));
        Animation<double> scale = curved.Drive(new DoubleTween(begin: 1.0, end: 1.0 - SheetScaleFactor));

        if (CupertinoSheetRoute<dynamic>.HasParentSheet(context))
        {
            return new SlideTransition(
                position: position,
                transformHitTests: false,
                child: new ScaleTransition(
                    scale: scale,
                    alignment: Alignment.TopCenter,
                    filterQuality: FilterQuality.Medium,
                    child: new ClipRSuperellipse(
                        borderRadius: BorderRadius.Vertical(top: Radius.Circular(12.0)),
                        child: child)));
        }

        return new Stack(children:
        [
            new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(
                    StatusBarIconBrightness: SystemUiIconBrightness.Light),
                child: new SizedBox(
                    width: double.PositiveInfinity,
                    height: MediaQuery.SizeOf(context).Height * DefaultTopGap)),
            new SlideTransition(
                position: position,
                child: new ScaleTransition(
                    scale: scale,
                    alignment: Alignment.TopCenter,
                    filterQuality: FilterQuality.Medium,
                    child: new AnimatedBuilder(
                        animation: curved,
                        child: child,
                        builder: (builderContext, builtChild) => BuildCoveredRoute(
                            builderContext,
                            secondaryAnimation,
                            curved,
                            builtChild!)))),
        ]);
    }

    public override State CreateState() => new CupertinoSheetTransitionState();

    private static Widget BuildCoveredRoute(
        BuildContext context,
        Animation<double> secondaryAnimation,
        Animation<double> curved,
        Widget child)
    {
        Thickness viewPadding = MediaQuery.MaybeViewPaddingOf(context) ?? default;
        double deviceCornerRadius = viewPadding.Top * 0.9;
        double startingRadius = deviceCornerRadius > 20.0 ? deviceCornerRadius : 0.0;
        BorderRadius radius = secondaryAnimation.Status == AnimationStatus.Dismissed
            ? BorderRadius.Zero
            : BorderRadius.Lerp(
                BorderRadius.Vertical(top: Radius.Circular(startingRadius)),
                BorderRadius.Circular(12.0),
                curved.Value) ?? BorderRadius.Zero;
        Widget paintedChild = child;
        if (secondaryAnimation.Status != AnimationStatus.Dismissed)
        {
            bool dark = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark;
            paintedChild = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    child,
                    new Opacity(
                        opacity: 0.10 * curved.Value,
                        child: new ColoredBox(
                            color: dark ? Color.FromUInt32(0xFFC8C8C8) : Color.FromUInt32(0xFF000000))),
                ]);
        }

        return new ClipRSuperellipse(borderRadius: radius, child: paintedChild);
    }
}

internal sealed class CupertinoSheetTransitionState : State
{
    private AnimationController _stretchDragController = null!;
    private CurvedAnimation _primaryCurve = null!;
    private CurvedAnimation _secondaryCurve = null!;

    private CupertinoSheetTransition CurrentWidget => (CupertinoSheetTransition)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _stretchDragController = new AnimationController(
            duration: TimeSpan.FromMicroseconds(1),
            vsync: this);
        UpdateAnimations();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldTransition = (CupertinoSheetTransition)oldWidget;
        if (ReferenceEquals(oldTransition.PrimaryRouteAnimation, CurrentWidget.PrimaryRouteAnimation)
            && ReferenceEquals(oldTransition.SecondaryRouteAnimation, CurrentWidget.SecondaryRouteAnimation))
        {
            return;
        }

        _primaryCurve.Dispose();
        _secondaryCurve.Dispose();
        UpdateAnimations();
    }

    public override Widget Build(BuildContext context)
    {
        Animation<Vector> secondaryPosition = _secondaryCurve.Drive(new VectorTween(
            begin: Vector.Zero,
            end: new Vector(0.0, -0.005)));
        Animation<double> secondaryScale = _secondaryCurve.Drive(new DoubleTween(
            begin: 1.0,
            end: 1.0 - CupertinoSheetTransition.SheetScaleFactor));
        Animation<Vector> primaryPosition = _primaryCurve.Drive(new VectorTween(
            begin: new Vector(0.0, 1.0),
            end: CupertinoSheetRoute<dynamic>.HasParentSheet(context)
                ? new Vector(0.0, -0.02)
                : Vector.Zero));

        return new CupertinoSheetStretchControllerProvider(
            controller: _stretchDragController,
            child: new SizedBox(
                width: double.PositiveInfinity,
                height: double.PositiveInfinity,
                child: new AnimatedBuilder(
                    animation: _stretchDragController,
                    child: new SlideTransition(
                        position: secondaryPosition,
                        transformHitTests: false,
                        child: new ScaleTransition(
                            scale: secondaryScale,
                            alignment: Alignment.TopCenter,
                            filterQuality: FilterQuality.Medium,
                            child: new SlideTransition(
                                position: primaryPosition,
                                child: CurrentWidget.Child))),
                    builder: (_, child) => new Padding(
                        EdgeInsetsGeometry.Only(
                            top: MediaQuery.HeightOf(context) * StretchTopGap(_stretchDragController.Value)),
                        child: child))));
    }

    public override void Dispose()
    {
        _primaryCurve.Dispose();
        _secondaryCurve.Dispose();
        _stretchDragController.Dispose();
        base.Dispose();
    }

    private double StretchTopGap(double value)
    {
        double distance = CupertinoSheetTransition.DefaultTopGap - CupertinoSheetTransition.StretchedTopGap;
        return CurrentWidget.TopGap - (distance * value);
    }

    private void UpdateAnimations()
    {
        Curve primaryCurve = CurrentWidget.LinearTransition ? Curves.Linear : Curves.FastEaseInToSlowEaseOut;
        Curve primaryReverseCurve = CurrentWidget.LinearTransition
            ? Curves.Linear
            : Curves.Flipped(Curves.FastEaseInToSlowEaseOut);
        _primaryCurve = new CurvedAnimation(
            CurrentWidget.PrimaryRouteAnimation,
            primaryCurve,
            primaryReverseCurve);
        _secondaryCurve = new CurvedAnimation(
            CurrentWidget.SecondaryRouteAnimation,
            Curves.LinearToEaseOut,
            Curves.EaseInToLinear);
    }
}

internal sealed class CupertinoSheetStretchControllerProvider : InheritedWidget
{
    public CupertinoSheetStretchControllerProvider(
        AnimationController controller,
        Widget child,
        Key? key = null) : base(key)
    {
        Controller = controller;
        Child = child;
    }

    public AnimationController Controller { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;
}

/// <summary>A non-opaque page route that presents content as an iOS sheet.</summary>
public sealed class CupertinoSheetRoute<T> : PageRoute, ICupertinoSheetRoute
{
    private readonly ScrollableWidgetBuilder _effectiveBuilder;
    private readonly WidgetBuilder? _builder;
    private readonly double? _topGap;
    private readonly TaskCompletionSource<T?> _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CupertinoSheetRoute(
        WidgetBuilder? builder = null,
        ScrollableWidgetBuilder? scrollableBuilder = null,
        bool enableDrag = true,
        bool showDragHandle = false,
        double? topGap = null,
        RouteSettings? settings = null) : base(settings)
    {
        if (topGap is < 0.0 or > 0.9)
        {
            throw new ArgumentOutOfRangeException(nameof(topGap), "topGap must be between 0.0 and 0.9.");
        }
        if (builder is null && scrollableBuilder is null)
        {
            throw new ArgumentException("Either scrollableBuilder or builder must not be null.");
        }

        _builder = builder;
        ScrollableBuilder = scrollableBuilder;
        EnableDrag = enableDrag;
        ShowDragHandle = showDragHandle;
        _topGap = topGap;
        _effectiveBuilder = scrollableBuilder ?? ((context, _) => builder!(context));
    }

    [Obsolete("Use ScrollableBuilder instead.")]
    public WidgetBuilder? Builder => _builder;

    public ScrollableWidgetBuilder? ScrollableBuilder { get; }

    public bool EnableDrag { get; }

    public bool ShowDragHandle { get; }

    public double TopGap => _topGap ?? CupertinoSheetTransition.DefaultTopGap;

    public Task<T?> Completed => _completed.Task;

    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(500);

    public override Color? BarrierColor => CupertinoColors.Transparent;

    public override bool BarrierDismissible => false;

    public override string? BarrierLabel => null;

    public override bool MaintainState => true;

    public override bool Opaque => false;

    public override DelegatedTransitionBuilder? DelegatedTransition => _topGap is null
        ? CupertinoSheetTransition.DelegateTransition
        : null;

    public static bool HasParentSheet(BuildContext context)
    {
        return context.DependOnInherited<CupertinoSheetScope>() is not null;
    }

    public static void PopSheet(BuildContext context)
    {
        if (context.DependOnInherited<CupertinoSheetScope>() is not null)
        {
            Plumix.Widgets.Navigator.Of(context, rootNavigator: true).Pop();
        }
    }

    public override bool CanTransitionFrom(TransitionRoute previousRoute) => _topGap is null;

    public override bool CanTransitionTo(TransitionRoute nextRoute)
    {
        if (_topGap is not null)
        {
            return false;
        }

        return nextRoute is CupertinoSheetRoute<T>
               || nextRoute.GetType().IsGenericType
               && nextRoute.GetType().GetGenericTypeDefinition() == typeof(CupertinoSheetRoute<>);
    }

    public override Widget BuildPage(BuildContext context) => BuildContent(context);

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return new CupertinoSheetTransition(
            primaryRouteAnimation: animation,
            secondaryRouteAnimation: secondaryAnimation,
            linearTransition: PopGestureInProgress,
            topGap: TopGap,
            child: child);
    }

    public override void DidComplete(object? result)
    {
        base.DidComplete(result);
        CompleteResult(result);
    }

    public override void Dispose()
    {
        _completed.TrySetResult(default);
        base.Dispose();
    }

    internal SheetPopGestureController StartPopGesture(BuildContext context)
    {
        AnimationController stretchController = context
            .DependOnInherited<CupertinoSheetStretchControllerProvider>()?.Controller
            ?? throw new InvalidOperationException("CupertinoSheetTransition was not found above sheet content.");
        NavigatorState navigator = Plumix.Widgets.Navigator.Of(context);
        navigator.StartUserGesture();
        return new SheetPopGestureController(
            Controller,
            stretchController,
            navigator,
            () => IsCurrent,
            () => IsActive);
    }

    SheetPopGestureController ICupertinoSheetRoute.StartPopGesture(BuildContext context) =>
        StartPopGesture(context);

    private Widget BuildContent(BuildContext context)
    {
        Widget content = new CupertinoSheetScrollable(
            route: this,
            builder: SheetWithDragHandle);
        content = new CupertinoSheetScope(child: content);
        content = new CupertinoUserInterfaceLevel(
            data: CupertinoUserInterfaceLevelData.Elevated,
            child: content);
        content = new ClipRSuperellipse(
            borderRadius: BorderRadius.Vertical(top: Radius.Circular(12.0)),
            child: content);
        return MediaQuery.RemovePadding(context, content, removeTop: true);
    }

    private Widget SheetWithDragHandle(BuildContext context, ScrollController controller)
    {
        Widget body = _effectiveBuilder(context, controller);
        if (!ShowDragHandle)
        {
            return body;
        }

        MediaQueryData media = MediaQuery.Of(context).CopyWith(padding: new Thickness(0.0, 15.0, 0.0, 0.0));
        Color handleColor = CupertinoColors.TertiaryLabel.ResolveFrom(context);
        return new Stack(
            fit: StackFit.Expand,
            children:
            [
                new MediaQuery(media, body),
                new Align(
                    alignment: Alignment.TopCenter,
                    child: new Padding(
                        EdgeInsetsGeometry.Only(top: 5.0),
                        child: new DecoratedBox(
                            decoration: new ShapeDecoration(
                                Shape: new RoundedSuperellipseBorder(
                                    borderRadius: BorderRadius.Circular(18.0)),
                                Color: handleColor),
                            child: new SizedBox(width: 36.0, height: 5.0)))),
            ]);
    }

    private void CompleteResult(object? result)
    {
        if (result is null)
        {
            _completed.TrySetResult(default);
        }
        else if (result is T value)
        {
            _completed.TrySetResult(value);
        }
        else
        {
            _completed.TrySetException(new InvalidCastException(
                $"Route result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}."));
        }
    }
}

internal sealed class CupertinoSheetScope : InheritedWidget
{
    public CupertinoSheetScope(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;
}

internal sealed class SheetPopGestureController
{
    private const double MinFlingVelocity = 2.0;
    private static readonly TimeSpan DroppedSheetDuration = TimeSpan.FromMilliseconds(300);
    private readonly AnimationController _popController;
    private readonly AnimationController _stretchController;
    private readonly NavigatorState _navigator;
    private readonly Func<bool> _getIsCurrent;
    private readonly Func<bool> _getIsActive;
    private bool _ended;

    public SheetPopGestureController(
        AnimationController popController,
        AnimationController stretchController,
        NavigatorState navigator,
        Func<bool> getIsCurrent,
        Func<bool> getIsActive)
    {
        _popController = popController;
        _stretchController = stretchController;
        _navigator = navigator;
        _getIsCurrent = getIsCurrent;
        _getIsActive = getIsActive;
    }

    public bool IsSheetDraggedDown => _popController.Value < 1.0;

    public void DragUpdate(double delta)
    {
        if (_popController.Value == 1.0 && (_stretchController.Value != 0.0 || delta < 0.0))
        {
            double stretchDistance = CupertinoSheetTransition.DefaultTopGap
                                     - CupertinoSheetTransition.StretchedTopGap;
            _stretchController.SetValue(_stretchController.Value - (delta / stretchDistance));
            return;
        }

        _popController.SetValue(_popController.Value - delta);
    }

    public void DragEnd(double velocity)
    {
        if (_ended)
        {
            return;
        }
        _ended = true;

        if (_stretchController.Value > 0.0)
        {
            _ = _stretchController.AnimateBack(
                0.0,
                TimeSpan.FromMilliseconds(180),
                Curves.EaseOut);
            _navigator.StopUserGesture();
            return;
        }

        bool isCurrent = _getIsCurrent();
        bool animateForward = !isCurrent
            ? _getIsActive()
            : Math.Abs(velocity) >= MinFlingVelocity
                ? velocity <= 0.0
                : _popController.Value > 0.52;

        if (animateForward)
        {
            _ = _popController.AnimateTo(1.0, DroppedSheetDuration, Curves.EaseOut);
        }
        else
        {
            if (isCurrent)
            {
                _navigator.Pop();
            }
            if (_popController.IsAnimating || _popController.Value != 0.0)
            {
                _ = _popController.AnimateBack(0.0, DroppedSheetDuration, Curves.EaseOut);
            }
        }

        _navigator.StopUserGesture();
    }
}

internal interface ICupertinoSheetRoute
{
    bool EnableDrag { get; }

    SheetPopGestureController StartPopGesture(BuildContext context);
}

internal sealed class CupertinoSheetScrollable : StatefulWidget
{
    public CupertinoSheetScrollable(
        ICupertinoSheetRoute route,
        ScrollableWidgetBuilder builder,
        Key? key = null) : base(key)
    {
        Route = route;
        Builder = builder;
    }

    public ICupertinoSheetRoute Route { get; }

    public ScrollableWidgetBuilder Builder { get; }

    public override State CreateState() => new CupertinoSheetScrollableState();
}

internal sealed class CupertinoSheetScrollableState : State
{
    private CupertinoSheetScrollController _scrollController = null!;
    private SheetPopGestureController? _dragController;

    private CupertinoSheetScrollable CurrentWidget => (CupertinoSheetScrollable)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _scrollController = new CupertinoSheetScrollController(
            onDragStart: StartDrag,
            onDragUpdate: UpdateDrag,
            onDragEnd: EndDrag,
            sheetIsDraggedDown: () => _dragController?.IsSheetDraggedDown == true);
    }

    public override Widget Build(BuildContext context)
    {
        Widget child = CurrentWidget.Builder(context, _scrollController);
        if (!CurrentWidget.Route.EnableDrag)
        {
            return child;
        }

        return new RawGestureDetector(
            behavior: HitTestBehavior.Translucent,
            onVerticalDragStart: _ => StartDrag(),
            onVerticalDragUpdate: details => UpdateDrag((details.PrimaryDelta ?? 0.0) / SheetHeight),
            onVerticalDragEnd: details => EndDrag((details.PrimaryVelocity ?? 0.0) / SheetHeight),
            onVerticalDragCancel: () => EndDrag(0.0),
            velocityTrackerBuilder: @event => new IOSScrollViewFlingVelocityTracker(@event.Kind),
            child: child);
    }

    public override void Dispose()
    {
        _dragController?.DragEnd(0.0);
        _scrollController.Dispose();
        base.Dispose();
    }

    private double SheetHeight
    {
        get
        {
            double height = Context.FindRenderObject() is RenderBox { HasSize: true } renderBox
                ? renderBox.Size.Height
                : MediaQuery.HeightOf(Context);
            return Math.Max(height, 1.0);
        }
    }

    private void StartDrag()
    {
        _dragController ??= CurrentWidget.Route.StartPopGesture(Context);
    }

    private void UpdateDrag(double delta)
    {
        StartDrag();
        _dragController!.DragUpdate(delta);
    }

    private void EndDrag(double velocity)
    {
        _dragController?.DragEnd(velocity);
        _dragController = null;
    }
}

internal sealed class CupertinoSheetScrollController : ScrollController
{
    private readonly Action _onDragStart;
    private readonly Action<double> _onDragUpdate;
    private readonly Action<double> _onDragEnd;
    private readonly Func<bool> _sheetIsDraggedDown;

    public CupertinoSheetScrollController(
        Action onDragStart,
        Action<double> onDragUpdate,
        Action<double> onDragEnd,
        Func<bool> sheetIsDraggedDown) : base(physics: new BouncingScrollPhysics(
        parent: new AlwaysScrollableScrollPhysics()))
    {
        _onDragStart = onDragStart;
        _onDragUpdate = onDragUpdate;
        _onDragEnd = onDragEnd;
        _sheetIsDraggedDown = sheetIsDraggedDown;
    }

    public override ScrollPosition CreateScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition)
    {
        return new CupertinoSheetScrollPosition(
            physics.ApplyTo(new AlwaysScrollableScrollPhysics()),
            context,
            oldPosition,
            _onDragStart,
            _onDragUpdate,
            _onDragEnd,
            _sheetIsDraggedDown);
    }
}

internal sealed class CupertinoSheetScrollPosition : ScrollPosition
{
    private readonly Action _onDragStart;
    private readonly Action<double> _onDragUpdate;
    private readonly Action<double> _onDragEnd;
    private readonly Func<bool> _sheetIsDraggedDown;

    public CupertinoSheetScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition,
        Action onDragStart,
        Action<double> onDragUpdate,
        Action<double> onDragEnd,
        Func<bool> sheetIsDraggedDown) : base(
        physics,
        context,
        initialPixels: 0.0,
        oldPosition: oldPosition)
    {
        _onDragStart = onDragStart;
        _onDragUpdate = onDragUpdate;
        _onDragEnd = onDragEnd;
        _sheetIsDraggedDown = sheetIsDraggedDown;
    }

    private bool ListShouldScroll => Pixels > 0.0;

    public override void ApplyUserOffset(double delta)
    {
        _onDragStart();
        if (!ListShouldScroll && (delta > 0.0 || _sheetIsDraggedDown()))
        {
            double height = Math.Max(NotificationContextHeight(), 1.0);
            double usableHeight = height * (1.0 - CupertinoSheetTransition.DefaultTopGap);
            _onDragUpdate(delta / usableHeight);
            return;
        }

        base.ApplyUserOffset(delta);
    }

    public override void GoBallistic(double velocity)
    {
        if (velocity < 0.0 && !ListShouldScroll)
        {
            double height = Math.Max(NotificationContextHeight(), 1.0);
            _onDragEnd(-velocity / height);
            base.GoBallistic(0.0);
            return;
        }

        _onDragEnd(0.0);
        base.GoBallistic(velocity);
    }

    private double NotificationContextHeight()
    {
        return Context.NotificationContext?.FindRenderObject() is RenderBox { HasSize: true } renderBox
            ? renderBox.Size.Height
            : ViewportDimension;
    }
}

/// <summary>Helpers corresponding to Dart's top-level <c>showCupertinoSheet</c> function.</summary>
public static class CupertinoSheets
{
    public static Task<T?> ShowCupertinoSheet<T>(
        BuildContext context,
        WidgetBuilder? pageBuilder = null,
        WidgetBuilder? builder = null,
        ScrollableWidgetBuilder? scrollableBuilder = null,
        bool useNestedNavigation = false,
        bool enableDrag = true,
        RouteSettings? settings = null,
        double? topGap = null,
        bool showDragHandle = false)
    {
        ArgumentNullException.ThrowIfNull(context.Owner);
        if (topGap is < 0.0 or > 0.9)
        {
            throw new ArgumentOutOfRangeException(nameof(topGap), "topGap must be between 0.0 and 0.9.");
        }
        if (pageBuilder is null && builder is null && scrollableBuilder is null)
        {
            throw new ArgumentException("A pageBuilder, builder, or scrollableBuilder is required.");
        }
        if (scrollableBuilder is not null && (pageBuilder is not null || builder is not null))
        {
            throw new ArgumentException("scrollableBuilder must be the only builder when provided.");
        }

        WidgetBuilder? effectiveBuilder = builder ?? pageBuilder;
        ScrollableWidgetBuilder? routeScrollableBuilder = scrollableBuilder;
        if (useNestedNavigation)
        {
            var nestedNavigatorKey = new LabeledGlobalKey<NavigatorState>("CupertinoSheet nested navigator");
            WidgetBuilder? nestedBuilder = effectiveBuilder;
            ScrollableWidgetBuilder? nestedScrollableBuilder = scrollableBuilder;
            routeScrollableBuilder = (sheetContext, controller) => new NavigatorPopHandler<object?>(
                onPopWithResult: result => nestedNavigatorKey.CurrentState?.MaybePop(result),
                child: new Navigator(
                    key: nestedNavigatorKey,
                    initialRoute: new CupertinoPageRoute<object?>(
                        _ => new PopScope<object?>(
                            canPop: false,
                            onPopInvokedWithResult: (didPop, result) =>
                            {
                                if (!didPop)
                                {
                                    Navigator.Of(sheetContext, rootNavigator: true).Pop(result);
                                }
                            },
                            child: nestedScrollableBuilder is not null
                                ? nestedScrollableBuilder(sheetContext, controller)
                                : nestedBuilder!(sheetContext)))));
            effectiveBuilder = null;
        }

        // The pinned Dart implementation accepts showDragHandle here but does not forward it.
        _ = showDragHandle;
        var route = new CupertinoSheetRoute<T>(
            builder: effectiveBuilder,
            scrollableBuilder: routeScrollableBuilder,
            enableDrag: enableDrag,
            settings: settings,
            topGap: topGap);
        Navigator.Of(context, rootNavigator: true).Push(route);
        return route.Completed;
    }
}
