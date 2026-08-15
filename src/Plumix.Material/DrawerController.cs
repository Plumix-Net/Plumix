using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/drawer.dart

public enum DrawerAlignment
{
    Start,
    End,
}

public sealed class DrawerController : StatefulWidget
{
    public DrawerController(
        Widget child,
        DrawerAlignment alignment,
        bool isDrawerOpen = false,
        Action<bool>? drawerCallback = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        Color? scrimColor = null,
        double? edgeDragWidth = null,
        bool enableOpenDragGesture = true,
        bool drawerBarrierDismissible = true,
        Key? key = null) : base(key)
    {
        if (edgeDragWidth.HasValue
            && (!double.IsFinite(edgeDragWidth.Value) || edgeDragWidth.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(edgeDragWidth),
                "Drawer edge drag width must be positive and finite.");
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
        Alignment = alignment;
        IsDrawerOpen = isDrawerOpen;
        DrawerCallback = drawerCallback;
        DragStartBehavior = dragStartBehavior;
        ScrimColor = scrimColor;
        EdgeDragWidth = edgeDragWidth;
        EnableOpenDragGesture = enableOpenDragGesture;
        DrawerBarrierDismissible = drawerBarrierDismissible;
    }

    public Widget Child { get; }

    public DrawerAlignment Alignment { get; }

    public bool IsDrawerOpen { get; }

    public Action<bool>? DrawerCallback { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public Color? ScrimColor { get; }

    public double? EdgeDragWidth { get; }

    public bool EnableOpenDragGesture { get; }

    public bool DrawerBarrierDismissible { get; }

    public static DrawerController? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<DrawerControllerScope>()?.Controller;
    }

    public static DrawerController Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "DrawerController.Of() was called with a context that does not contain a DrawerController.");
    }

    public override State CreateState() => new DrawerControllerState();
}

public sealed class DrawerControllerState : State
{
    private const double DefaultWidth = 304.0;
    private const double DefaultEdgeDragWidth = 20.0;
    private const double MinFlingVelocityPixelsPerSecond = 365.0;
    private static readonly TimeSpan BaseSettleDuration = TimeSpan.FromMilliseconds(246);
    private readonly LabeledGlobalKey<State> _drawerKey = new("drawer");
    private readonly FocusScopeNode _focusScopeNode = new();
    private AnimationController _controller = null!;
    private LocalHistoryEntry? _historyEntry;
    private ModalRoute? _historyRoute;
    private bool _isDragging;
    private bool _previouslyOpened;

    private DrawerController CurrentWidget => (DrawerController)StateWidget;

    public double Progress => _controller.Value;

    public bool IsOpen => _controller.Value >= 0.5;

    public override void InitState()
    {
        _controller = new AnimationController(duration: BaseSettleDuration, vsync: this);
        _controller.Changed += HandleAnimationChanged;
        _controller.AddStatusListener(HandleAnimationStatusChanged);
        _controller.SetValue(CurrentWidget.IsDrawerOpen ? 1.0 : 0.0);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldController = (DrawerController)oldWidget;
        if (_controller.Status.IsAnimating() || _isDragging)
        {
            // Don't snap the drawer open or shut while the user is dragging.
            return;
        }

        if (CurrentWidget.IsDrawerOpen != oldController.IsDrawerOpen)
        {
            _controller.SetValue(CurrentWidget.IsDrawerOpen ? 1.0 : 0.0);
        }
    }

    public override void Dispose()
    {
        RemoveHistoryEntry();
        _controller.Changed -= HandleAnimationChanged;
        _controller.RemoveStatusListener(HandleAnimationStatusChanged);
        _controller.Dispose();
        _focusScopeNode.Dispose();
    }

    public void Open()
    {
        _controller.Fling();
        CurrentWidget.DrawerCallback?.Invoke(true);
    }

    public void Close()
    {
        _controller.Fling(velocity: -1.0);
        CurrentWidget.DrawerCallback?.Invoke(false);
    }

    public override Widget Build(BuildContext context)
    {
        Widget result = _controller.Value <= 0
            ? BuildClosedDrawer(context)
            : BuildOpenDrawer(context);
        return new ListTileTheme(
            data: ListTileTheme.Of(context) with { Style = ListTileStyle.Drawer },
            child: result);
    }

    private Widget BuildClosedDrawer(BuildContext context)
    {
        if (!CurrentWidget.EnableOpenDragGesture || IsDesktopPlatform(Theme.Of(context).Platform))
        {
            return new SizedBox();
        }

        TextDirection textDirection = Directionality.Of(context);
        bool isOnLeft = IsOnLeft(CurrentWidget.Alignment, textDirection);
        double dragAreaWidth = ResolveEdgeDragWidth(context, textDirection);
        return new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Positioned(
                    left: isOnLeft ? 0 : null,
                    top: 0,
                    right: isOnLeft ? null : 0,
                    bottom: 0,
                    width: dragAreaWidth,
                    child: new GestureDetector(
                        excludeFromSemantics: true,
                        behavior: HitTestBehavior.Translucent,
                        onHorizontalDragStart: _ => HandleDragStart(),
                        onHorizontalDragUpdate: HandleDragUpdate,
                        onHorizontalDragEnd: HandleDragEnd,
                        onHorizontalDragCancel: HandleDragCancel,
                        dragStartBehavior: CurrentWidget.DragStartBehavior,
                        child: new SizedBox())),
            ]);
    }

    private Widget BuildOpenDrawer(BuildContext context)
    {
        TextDirection textDirection = Directionality.Of(context);
        bool isOnLeft = IsOnLeft(CurrentWidget.Alignment, textDirection);
        double drawerWidth = ResolveDrawerWidth(context);
        double offset = -(1.0 - _controller.Value) * drawerWidth;
        var drawerTheme = DrawerTheme.Of(context);
        Color baseScrimColor = CurrentWidget.ScrimColor
                               ?? drawerTheme.ScrimColor
                               ?? Color.FromArgb(0x8A, 0, 0, 0);
        Color scrimColor = ApplyOpacity(baseScrimColor, _controller.Value);
        bool platformHasBackButton = PlatformDefaults.TargetPlatform == TargetPlatform.Android;
        var localizations = MaterialLocalizations.Of(context);

        Widget child = new DrawerControllerScope(
            controller: CurrentWidget,
            child: new RepaintBoundary(
                child: new Stack(
                    fit: StackFit.Expand,
                    children:
                    [
                        new BlockSemantics(
                            child: new ExcludeSemantics(
                                excluding: platformHasBackButton,
                                child: new GestureDetector(
                                    excludeFromSemantics: true,
                                    behavior: HitTestBehavior.Opaque,
                                    onTap: CurrentWidget.DrawerBarrierDismissible ? Close : null,
                                    child: new Semantics(
                                        label: localizations.ModalBarrierDismissLabel,
                                        child: new ColoredBox(
                                            color: scrimColor,
                                            child: new SizedBox()))))),
                        new Positioned(
                            left: isOnLeft ? offset : null,
                            top: 0,
                            right: isOnLeft ? null : offset,
                            bottom: 0,
                                child: new RepaintBoundary(
                                    child: new FocusScope(
                                        key: _drawerKey,
                                        focusScopeNode: _focusScopeNode,
                                        child: CurrentWidget.Child))),
                    ])));

        if (IsDesktopPlatform(Theme.Of(context).Platform))
        {
            return child;
        }

        return new GestureDetector(
            excludeFromSemantics: true,
            behavior: HitTestBehavior.Opaque,
            onHorizontalDragStart: _ => HandleDragStart(),
            onHorizontalDragUpdate: HandleDragUpdate,
            onHorizontalDragEnd: HandleDragEnd,
            onHorizontalDragCancel: HandleDragCancel,
            dragStartBehavior: CurrentWidget.DragStartBehavior,
            child: child);
    }

    private void HandleDragStart()
    {
        _controller.Stop();
        _isDragging = true;
        EnsureHistoryEntry();
    }

    private void HandleDragUpdate(DragUpdateDetails details)
    {
        if (!_isDragging)
        {
            return;
        }

        double drawerWidth = ResolveDrawerWidth(Context);
        TextDirection textDirection = Directionality.Of(Context);
        double directionFactor = IsOnLeft(CurrentWidget.Alignment, textDirection) ? 1.0 : -1.0;
        _controller.SetValue(_controller.Value + (details.PrimaryDelta / drawerWidth * directionFactor));

        bool opened = _controller.Value > 0.5;
        if (opened != _previouslyOpened)
        {
            CurrentWidget.DrawerCallback?.Invoke(opened);
        }

        _previouslyOpened = opened;
    }

    private void HandleDragEnd(DragEndDetails details)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        double drawerWidth = ResolveDrawerWidth(Context);
        TextDirection textDirection = Directionality.Of(Context);
        double directionFactor = IsOnLeft(CurrentWidget.Alignment, textDirection) ? 1.0 : -1.0;
        double velocity = details.PrimaryVelocity * directionFactor;
        if (Math.Abs(velocity) >= MinFlingVelocityPixelsPerSecond)
        {
            double visualVelocity = velocity / drawerWidth;
            Settle(visualVelocity > 0, Math.Abs(visualVelocity));
            return;
        }

        Settle(_controller.Value >= 0.5);
    }

    private void HandleDragCancel()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        Settle(_controller.Value >= 0.5);
    }

    private void Settle(bool open, double? normalizedVelocity = null)
    {
        if (open)
        {
            EnsureHistoryEntry();
            _controller.Fling(Math.Max(1.0, normalizedVelocity ?? 1.0));
        }
        else
        {
            RemoveHistoryEntry();
            _controller.Fling(-Math.Max(1.0, normalizedVelocity ?? 1.0));
        }

        CurrentWidget.DrawerCallback?.Invoke(open);
    }

    private void EnsureHistoryEntry()
    {
        var route = ModalRoute.MaybeOf(Context);
        if (route is null)
        {
            return;
        }

        if (_historyEntry is not null && ReferenceEquals(_historyRoute, route))
        {
            return;
        }

        RemoveHistoryEntry();
        var entry = new LocalHistoryEntry(
            onRemove: HandleHistoryEntryRemoved,
            impliesAppBarDismissal: false);
        route.AddLocalHistoryEntry(entry);
        _historyEntry = entry;
        _historyRoute = route;
        _focusScopeNode.RequestFocus();
    }

    private void RemoveHistoryEntry()
    {
        var entry = _historyEntry;
        _historyEntry = null;
        _historyRoute = null;
        entry?.Remove();
    }

    private void HandleHistoryEntryRemoved()
    {
        _historyEntry = null;
        _historyRoute = null;
        Close();
    }

    private void HandleAnimationChanged()
    {
        if (Mounted)
        {
            SetState(() => { });
        }
    }

    // The history entry follows the animation's direction: an opening drawer owns a pop target, a
    // closing one gives it up. The terminal statuses leave it alone.
    private void HandleAnimationStatusChanged(AnimationStatus status)
    {
        switch (status)
        {
            case AnimationStatus.Forward:
                EnsureHistoryEntry();
                break;
            case AnimationStatus.Reverse:
                RemoveHistoryEntry();
                break;
            default:
                break;
        }
    }

    private double ResolveDrawerWidth(BuildContext context)
    {
        if (_drawerKey.CurrentContext?.FindRenderObject() is RenderBox { HasSize: true } renderBox)
        {
            return renderBox.Size.Width;
        }

        return CurrentWidget.Child is Drawer drawer
            ? drawer.ResolveEffectiveWidthForScaffold(context)
            : DefaultWidth;
    }

    private double ResolveEdgeDragWidth(BuildContext context, TextDirection textDirection)
    {
        if (CurrentWidget.EdgeDragWidth.HasValue)
        {
            return CurrentWidget.EdgeDragWidth.Value;
        }

        Thickness padding = MediaQuery.MaybePaddingOf(context) ?? default;
        bool isOnLeft = IsOnLeft(CurrentWidget.Alignment, textDirection);
        return DefaultEdgeDragWidth + (isOnLeft ? padding.Left : padding.Right);
    }

    private static bool IsOnLeft(DrawerAlignment alignment, TextDirection textDirection)
    {
        return alignment switch
        {
            DrawerAlignment.Start => textDirection == TextDirection.Ltr,
            DrawerAlignment.End => textDirection == TextDirection.Rtl,
            _ => true,
        };
    }

    private static bool IsDesktopPlatform(TargetPlatform platform)
    {
        return platform is TargetPlatform.Windows or TargetPlatform.Linux or TargetPlatform.MacOS;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(color.A * Math.Clamp(opacity, 0.0, 1.0)), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

internal sealed class DrawerControllerScope : InheritedWidget
{
    public DrawerControllerScope(
        DrawerController controller,
        Widget child,
        Key? key = null) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DrawerController Controller { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((DrawerControllerScope)oldWidget).Controller, Controller);
    }
}
