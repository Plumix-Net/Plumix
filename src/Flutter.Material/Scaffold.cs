using Avalonia;
using Avalonia.Media;
using Flutter;
using Flutter.Foundation;
using Flutter.Gestures;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/scaffold.dart; flutter/packages/flutter/lib/src/material/app_bar.dart (approximate)

public sealed class Drawer : StatelessWidget
{
    private const double DefaultWidth = 304.0;
    private const double DefaultM2Elevation = 16.0;
    private const double DefaultM3Elevation = 1.0;

    public Drawer(
        Widget? child = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        double? width = null,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (double.IsNaN(elevation.Value) || double.IsInfinity(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Drawer elevation must be non-negative and finite.");
        }

        if (width.HasValue && (double.IsNaN(width.Value) || double.IsInfinity(width.Value) || width.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Drawer width must be positive and finite.");
        }

        Child = child;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        Width = width;
    }

    public Widget? Child { get; }

    public Color? BackgroundColor { get; }

    public double? Elevation { get; }

    public Color? ShadowColor { get; }

    public double? Width { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var useMaterial3 = theme.UseMaterial3;
        var effectiveBackground = BackgroundColor ?? (useMaterial3
            ? theme.SurfaceContainerLowColor
            : theme.CanvasColor);
        var effectiveElevation = Elevation ?? (useMaterial3
            ? DefaultM3Elevation
            : DefaultM2Elevation);
        var effectiveShadowColor = ShadowColor ?? (useMaterial3
            ? Colors.Transparent
            : theme.ShadowColor);
        var effectiveBoxShadows = BuildBoxShadows(effectiveShadowColor, effectiveElevation);

        return new Container(
            width: Width ?? DefaultWidth,
            decoration: new BoxDecoration(
                Color: effectiveBackground,
                BoxShadows: effectiveBoxShadows),
            child: Child ?? new SizedBox());
    }

    private static BoxShadows? BuildBoxShadows(Color shadowColor, double elevation)
    {
        if (elevation <= 0 || shadowColor.A == 0)
        {
            return null;
        }

        var keyShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(2, elevation * 2.4),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.20),
            IsInset = false,
        };

        var ambientShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(3, elevation * 3.2),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.14),
            IsInset = false,
        };

        return new BoxShadows(keyShadow, [ambientShadow]);
    }

    private static Color ApplyOpacity(Color color, double opacityMultiplier)
    {
        var baseOpacity = color.A / 255.0;
        var effectiveOpacity = Math.Clamp(baseOpacity * opacityMultiplier, 0, 1);
        var alpha = (byte)Math.Clamp((int)(effectiveOpacity * 255), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

internal enum DrawerSide
{
    Start,
    End
}

public sealed class Scaffold : StatefulWidget
{
    private const double DefaultDrawerEdgeDragWidth = 20.0;
    private static readonly Color DefaultDrawerScrimColor = Color.FromArgb(0x8A, 0x00, 0x00, 0x00);

    public Scaffold(
        Widget body,
        AppBar? appBar = null,
        Widget? drawer = null,
        Widget? endDrawer = null,
        bool drawerBarrierDismissible = true,
        Color? drawerScrimColor = null,
        double? drawerEdgeDragWidth = null,
        bool drawerEnableOpenDragGesture = true,
        bool endDrawerEnableOpenDragGesture = true,
        Widget? floatingActionButton = null,
        Widget? bottomNavigationBar = null,
        Color? backgroundColor = null,
        Key? key = null) : base(key)
    {
        if (drawerEdgeDragWidth.HasValue
            && (double.IsNaN(drawerEdgeDragWidth.Value)
                || double.IsInfinity(drawerEdgeDragWidth.Value)
                || drawerEdgeDragWidth.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(drawerEdgeDragWidth), "Drawer edge drag width must be positive and finite.");
        }

        Body = body;
        AppBar = appBar;
        Drawer = drawer;
        EndDrawer = endDrawer;
        DrawerBarrierDismissible = drawerBarrierDismissible;
        DrawerScrimColor = drawerScrimColor;
        DrawerEdgeDragWidth = drawerEdgeDragWidth;
        DrawerEnableOpenDragGesture = drawerEnableOpenDragGesture;
        EndDrawerEnableOpenDragGesture = endDrawerEnableOpenDragGesture;
        FloatingActionButton = floatingActionButton;
        BottomNavigationBar = bottomNavigationBar;
        BackgroundColor = backgroundColor;
    }

    public Widget Body { get; }

    public AppBar? AppBar { get; }

    public Widget? Drawer { get; }

    public Widget? EndDrawer { get; }

    public bool DrawerBarrierDismissible { get; }

    public Color? DrawerScrimColor { get; }

    public double? DrawerEdgeDragWidth { get; }

    public bool DrawerEnableOpenDragGesture { get; }

    public bool EndDrawerEnableOpenDragGesture { get; }

    public Widget? FloatingActionButton { get; }

    public Widget? BottomNavigationBar { get; }

    public Color? BackgroundColor { get; }

    public override State CreateState()
    {
        return new ScaffoldState();
    }

    public static ScaffoldState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("Scaffold not found in context.");
    }

    public static ScaffoldState? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<ScaffoldScope>()?.Scaffold;
    }

    internal static Color ResolveDrawerScrimColor(Color? drawerScrimColor)
    {
        return drawerScrimColor ?? DefaultDrawerScrimColor;
    }

    internal static double ResolveDrawerEdgeDragWidth(double? drawerEdgeDragWidth)
    {
        return drawerEdgeDragWidth ?? DefaultDrawerEdgeDragWidth;
    }
}

internal sealed class ScaffoldScope : InheritedWidget
{
    public ScaffoldScope(
        ScaffoldState scaffold,
        bool hasDrawer,
        bool hasEndDrawer,
        bool isDrawerOpen,
        bool isEndDrawerOpen,
        Widget child,
        Key? key = null) : base(key)
    {
        Scaffold = scaffold ?? throw new ArgumentNullException(nameof(scaffold));
        HasDrawer = hasDrawer;
        HasEndDrawer = hasEndDrawer;
        IsDrawerOpen = isDrawerOpen;
        IsEndDrawerOpen = isEndDrawerOpen;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScaffoldState Scaffold { get; }

    public bool HasDrawer { get; }

    public bool HasEndDrawer { get; }

    public bool IsDrawerOpen { get; }

    public bool IsEndDrawerOpen { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (ScaffoldScope)oldWidget;
        return !ReferenceEquals(Scaffold, oldScope.Scaffold)
               || HasDrawer != oldScope.HasDrawer
               || HasEndDrawer != oldScope.HasEndDrawer
               || IsDrawerOpen != oldScope.IsDrawerOpen
               || IsEndDrawerOpen != oldScope.IsEndDrawerOpen;
    }
}

public sealed class ScaffoldState : State
{
    private const double DefaultDrawerWidth = 304.0;
    private const double OpenThreshold = 0.5;
    private const double FlingVelocityThreshold = 0.35;
    private const double VelocityHintPerDeltaFactor = 60.0;
    private const double MinSettleDurationMs = 80.0;
    private const double MaxSettleDurationMs = 246.0;
    private bool _isDrawerOpen;
    private bool _isEndDrawerOpen;
    private double _drawerProgress;
    private double _endDrawerProgress;
    private DrawerSide? _activeDragSide;
    private double _activeDragProgress;
    private double _lastDragVelocityHint;
    private AnimationController? _drawerAnimationController;
    private AnimationController? _endDrawerAnimationController;
    private double _drawerAnimationFrom;
    private double _drawerAnimationTo;
    private double _endDrawerAnimationFrom;
    private double _endDrawerAnimationTo;
    private LocalHistoryEntry? _drawerHistoryEntry;
    private ModalRoute? _drawerHistoryRoute;
    private bool _isRemovingDrawerHistoryEntry;
    private bool _isDisposed;

    private Scaffold CurrentWidget => (Scaffold)StateWidget;

    public bool HasDrawer => CurrentWidget.Drawer != null;

    public bool HasEndDrawer => CurrentWidget.EndDrawer != null;

    public bool IsDrawerOpen => _isDrawerOpen;

    public bool IsEndDrawerOpen => _isEndDrawerOpen;

    public override void InitState()
    {
        _isDisposed = false;
        _drawerProgress = 0;
        _endDrawerProgress = 0;
    }

    public override void Dispose()
    {
        _isDisposed = true;
        RemoveDrawerHistoryEntry();
        StopSettleAnimation(DrawerSide.Start);
        StopSettleAnimation(DrawerSide.End);
    }

    public void OpenDrawer()
    {
        if (!HasDrawer)
        {
            return;
        }

        SetState(() =>
        {
            StopSettleAnimation(DrawerSide.Start);
            StopSettleAnimation(DrawerSide.End);
            CancelDrag();
            _isDrawerOpen = true;
            _isEndDrawerOpen = false;
            StartSettleAnimation(DrawerSide.Start, targetProgress: 1.0, normalizedVelocityHint: null);
            StartSettleAnimation(DrawerSide.End, targetProgress: 0.0, normalizedVelocityHint: null);
        });
    }

    public void OpenEndDrawer()
    {
        if (!HasEndDrawer)
        {
            return;
        }

        SetState(() =>
        {
            StopSettleAnimation(DrawerSide.Start);
            StopSettleAnimation(DrawerSide.End);
            CancelDrag();
            _isEndDrawerOpen = true;
            _isDrawerOpen = false;
            StartSettleAnimation(DrawerSide.End, targetProgress: 1.0, normalizedVelocityHint: null);
            StartSettleAnimation(DrawerSide.Start, targetProgress: 0.0, normalizedVelocityHint: null);
        });
    }

    public void CloseDrawer()
    {
        if (!HasDrawer && ResolveDrawerProgress(DrawerSide.Start) <= 0)
        {
            return;
        }

        if (!_isDrawerOpen && ResolveDrawerProgress(DrawerSide.Start) <= 0)
        {
            return;
        }

        SetState(() =>
        {
            StopSettleAnimation(DrawerSide.Start);
            _isDrawerOpen = false;
            if (_activeDragSide == DrawerSide.Start)
            {
                CancelDrag();
            }

            StartSettleAnimation(DrawerSide.Start, targetProgress: 0.0, normalizedVelocityHint: null);
        });
    }

    public void CloseEndDrawer()
    {
        if (!HasEndDrawer && ResolveDrawerProgress(DrawerSide.End) <= 0)
        {
            return;
        }

        if (!_isEndDrawerOpen && ResolveDrawerProgress(DrawerSide.End) <= 0)
        {
            return;
        }

        SetState(() =>
        {
            StopSettleAnimation(DrawerSide.End);
            _isEndDrawerOpen = false;
            if (_activeDragSide == DrawerSide.End)
            {
                CancelDrag();
            }

            StartSettleAnimation(DrawerSide.End, targetProgress: 0.0, normalizedVelocityHint: null);
        });
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        if (!HasDrawer)
        {
            _isDrawerOpen = false;
            _drawerProgress = 0;
            StopSettleAnimation(DrawerSide.Start);
            if (_activeDragSide == DrawerSide.Start)
            {
                CancelDrag();
            }
        }

        if (!HasEndDrawer)
        {
            _isEndDrawerOpen = false;
            _endDrawerProgress = 0;
            StopSettleAnimation(DrawerSide.End);
            if (_activeDragSide == DrawerSide.End)
            {
                CancelDrag();
            }
        }
    }

    public override Widget Build(BuildContext context)
    {
        SyncDrawerHistoryEntry(context);

        var theme = Theme.Of(context);
        var effectiveBackground = CurrentWidget.BackgroundColor ?? theme.ScaffoldBackgroundColor;

        var columnChildren = new List<Widget>();
        if (CurrentWidget.AppBar != null)
        {
            columnChildren.Add(CurrentWidget.AppBar);
        }

        columnChildren.Add(new Expanded(child: CurrentWidget.Body));

        if (CurrentWidget.BottomNavigationBar != null)
        {
            columnChildren.Add(CurrentWidget.BottomNavigationBar);
        }

        Widget content = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children: columnChildren);

        if (CurrentWidget.FloatingActionButton != null)
        {
            content = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    content,
                    new Positioned(
                        right: 16,
                        bottom: 16,
                        child: CurrentWidget.FloatingActionButton),
                ]);
        }

        var textDirection = Directionality.Of(context);
        var drawerProgress = ResolveDrawerProgress(DrawerSide.Start);
        var endDrawerProgress = ResolveDrawerProgress(DrawerSide.End);
        var isStartDrawerVisible = IsDrawerVisible(DrawerSide.Start, drawerProgress);
        var isEndDrawerVisible = IsDrawerVisible(DrawerSide.End, endDrawerProgress);
        var isAnyDrawerVisible = isStartDrawerVisible || isEndDrawerVisible;
        var overlayChildren = new List<Widget> { content };

        if (!isAnyDrawerVisible)
        {
            if (ShouldEnableOpenDragGesture(DrawerSide.Start, theme))
            {
                overlayChildren.Add(BuildEdgeDragArea(DrawerSide.Start, textDirection));
            }

            if (ShouldEnableOpenDragGesture(DrawerSide.End, theme))
            {
                overlayChildren.Add(BuildEdgeDragArea(DrawerSide.End, textDirection));
            }
        }

        if (isAnyDrawerVisible)
        {
            overlayChildren.Add(BuildScrim(Math.Max(drawerProgress, endDrawerProgress)));
        }

        if (isStartDrawerVisible && CurrentWidget.Drawer != null)
        {
            overlayChildren.Add(BuildDrawerPanel(
                side: DrawerSide.Start,
                textDirection: textDirection,
                progress: drawerProgress,
                child: CurrentWidget.Drawer));
        }

        if (isEndDrawerVisible && CurrentWidget.EndDrawer != null)
        {
            overlayChildren.Add(BuildDrawerPanel(
                side: DrawerSide.End,
                textDirection: textDirection,
                progress: endDrawerProgress,
                child: CurrentWidget.EndDrawer));
        }

        if (overlayChildren.Count > 1)
        {
            content = new Stack(
                fit: StackFit.Expand,
                children: overlayChildren);
        }

        return new ScaffoldScope(
            scaffold: this,
            hasDrawer: HasDrawer,
            hasEndDrawer: HasEndDrawer,
            isDrawerOpen: _isDrawerOpen,
            isEndDrawerOpen: _isEndDrawerOpen,
            child: new Container(
                color: effectiveBackground,
                child: content));
    }

    private Widget BuildEdgeDragArea(DrawerSide side, TextDirection textDirection)
    {
        var edgeWidth = Scaffold.ResolveDrawerEdgeDragWidth(CurrentWidget.DrawerEdgeDragWidth);
        var isOnLeft = IsDrawerOnLeft(side, textDirection);
        return new Positioned(
            left: isOnLeft ? 0 : null,
            top: 0,
            right: isOnLeft ? null : 0,
            bottom: 0,
            width: edgeWidth,
            child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onHorizontalDragStart: _ => BeginDrag(side),
                onHorizontalDragUpdate: details => UpdateDrag(side, details.PrimaryDelta, textDirection),
                onHorizontalDragEnd: details => EndDrag(side, details, textDirection)));
    }

    private Widget BuildScrim(double progress)
    {
        var baseColor = Scaffold.ResolveDrawerScrimColor(CurrentWidget.DrawerScrimColor);
        var scrimColor = ApplyOpacity(baseColor, progress);
        return new Positioned(
            left: 0,
            top: 0,
            right: 0,
            bottom: 0,
            child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: CurrentWidget.DrawerBarrierDismissible ? CloseOpenDrawers : null,
                child: new Container(color: scrimColor)));
    }

    private Widget BuildDrawerPanel(DrawerSide side, TextDirection textDirection, double progress, Widget child)
    {
        var drawerWidth = ResolveDrawerWidth(child);
        var isOnLeft = IsDrawerOnLeft(side, textDirection);
        var offset = -(1 - progress) * drawerWidth;

        return new Positioned(
            left: isOnLeft ? offset : null,
            top: 0,
            right: isOnLeft ? null : offset,
            bottom: 0,
            child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onHorizontalDragStart: _ => BeginDrag(side),
                onHorizontalDragUpdate: details => UpdateDrag(side, details.PrimaryDelta, textDirection),
                onHorizontalDragEnd: details => EndDrag(side, details, textDirection),
                child: child));
    }

    private bool ShouldEnableOpenDragGesture(DrawerSide side, ThemeData theme)
    {
        if (!HasDrawerForSide(side) || IsDesktopPlatform(theme.Platform))
        {
            return false;
        }

        return side == DrawerSide.Start
            ? CurrentWidget.DrawerEnableOpenDragGesture
            : CurrentWidget.EndDrawerEnableOpenDragGesture;
    }

    private void BeginDrag(DrawerSide side)
    {
        if (!HasDrawerForSide(side))
        {
            return;
        }

        SetState(() =>
        {
            StopSettleAnimation(side);
            _activeDragSide = side;
            _activeDragProgress = ResolveDrawerProgress(side);
            _lastDragVelocityHint = 0;

            if (side == DrawerSide.Start)
            {
                _isEndDrawerOpen = false;
                _endDrawerProgress = 0;
                StopSettleAnimation(DrawerSide.End);
            }
            else
            {
                _isDrawerOpen = false;
                _drawerProgress = 0;
                StopSettleAnimation(DrawerSide.Start);
            }
        });
    }

    private void UpdateDrag(DrawerSide side, double primaryDelta, TextDirection textDirection)
    {
        if (_activeDragSide != side)
        {
            return;
        }

        var drawer = ResolveDrawerWidget(side);
        if (drawer == null)
        {
            return;
        }

        var drawerWidth = ResolveDrawerWidth(drawer);
        if (drawerWidth <= 0)
        {
            return;
        }

        var deltaProgress = primaryDelta * ResolveOpenDirectionMultiplier(side, textDirection) / drawerWidth;
        var nextProgress = Math.Clamp(_activeDragProgress + deltaProgress, 0, 1);
        if (Math.Abs(nextProgress - _activeDragProgress) <= 0.0001)
        {
            return;
        }

        SetState(() =>
        {
            _activeDragProgress = nextProgress;
            _lastDragVelocityHint = deltaProgress * VelocityHintPerDeltaFactor;
            UpdateOpenFlagsFromProgress(side, nextProgress);
        });
    }

    private void EndDrag(DrawerSide side, DragEndDetails details, TextDirection textDirection)
    {
        if (_activeDragSide != side)
        {
            return;
        }

        var drawer = ResolveDrawerWidget(side);
        if (drawer == null)
        {
            return;
        }

        var drawerWidth = ResolveDrawerWidth(drawer);
        if (drawerWidth <= 0)
        {
            return;
        }

        var releaseVelocity = details.PrimaryVelocity * ResolveOpenDirectionMultiplier(side, textDirection) / drawerWidth;
        if (Math.Abs(releaseVelocity) < double.Epsilon)
        {
            releaseVelocity = _lastDragVelocityHint;
        }

        bool shouldOpen;
        if (releaseVelocity >= FlingVelocityThreshold)
        {
            shouldOpen = true;
        }
        else if (releaseVelocity <= -FlingVelocityThreshold)
        {
            shouldOpen = false;
        }
        else
        {
            shouldOpen = _activeDragProgress >= OpenThreshold;
        }

        SetState(() =>
        {
            CommitProgress(side, _activeDragProgress);
            CommitDrawerVisibility(side, shouldOpen);
            CancelDrag();
            StartSettleAnimation(side, shouldOpen ? 1.0 : 0.0, releaseVelocity);
            if (shouldOpen)
            {
                StartSettleAnimation(OppositeOf(side), targetProgress: 0.0, normalizedVelocityHint: null);
            }
        });
    }

    private void CommitDrawerVisibility(DrawerSide side, bool isOpen)
    {
        if (side == DrawerSide.Start)
        {
            _isDrawerOpen = isOpen && HasDrawer;
            if (isOpen)
            {
                _isEndDrawerOpen = false;
            }

            return;
        }

        _isEndDrawerOpen = isOpen && HasEndDrawer;
        if (isOpen)
        {
            _isDrawerOpen = false;
        }
    }

    private void CloseOpenDrawers()
    {
        if (!_isDrawerOpen && !_isEndDrawerOpen && _activeDragSide is null
            && ResolveDrawerProgress(DrawerSide.Start) <= 0
            && ResolveDrawerProgress(DrawerSide.End) <= 0)
        {
            return;
        }

        SetState(() =>
        {
            StopSettleAnimation(DrawerSide.Start);
            StopSettleAnimation(DrawerSide.End);
            _isDrawerOpen = false;
            _isEndDrawerOpen = false;
            CancelDrag();
            StartSettleAnimation(DrawerSide.Start, targetProgress: 0.0, normalizedVelocityHint: null);
            StartSettleAnimation(DrawerSide.End, targetProgress: 0.0, normalizedVelocityHint: null);
        });
    }

    private void CancelDrag()
    {
        if (_activeDragSide.HasValue)
        {
            CommitProgress(_activeDragSide.Value, _activeDragProgress);
        }

        _activeDragSide = null;
        _activeDragProgress = 0;
        _lastDragVelocityHint = 0;
    }

    private bool HasDrawerForSide(DrawerSide side)
    {
        return side == DrawerSide.Start ? HasDrawer : HasEndDrawer;
    }

    private Widget? ResolveDrawerWidget(DrawerSide side)
    {
        return side == DrawerSide.Start ? CurrentWidget.Drawer : CurrentWidget.EndDrawer;
    }

    private double ResolveDrawerProgress(DrawerSide side)
    {
        if (_activeDragSide == side)
        {
            return _activeDragProgress;
        }

        return side == DrawerSide.Start
            ? _drawerProgress
            : _endDrawerProgress;
    }

    private bool IsDrawerVisible(DrawerSide side, double progress)
    {
        if (progress > 0)
        {
            return true;
        }

        if (_activeDragSide == side)
        {
            return true;
        }

        return side == DrawerSide.Start
            ? _isDrawerOpen
            : _isEndDrawerOpen;
    }

    private static bool IsDrawerOnLeft(DrawerSide side, TextDirection textDirection)
    {
        return side switch
        {
            DrawerSide.Start => textDirection == TextDirection.Ltr,
            DrawerSide.End => textDirection == TextDirection.Rtl,
            _ => true,
        };
    }

    private static double ResolveOpenDirectionMultiplier(DrawerSide side, TextDirection textDirection)
    {
        return IsDrawerOnLeft(side, textDirection) ? 1.0 : -1.0;
    }

    private static bool IsDesktopPlatform(TargetPlatform platform)
    {
        return platform is TargetPlatform.Windows or TargetPlatform.Linux or TargetPlatform.MacOS;
    }

    private void StartSettleAnimation(DrawerSide side, double targetProgress, double? normalizedVelocityHint)
    {
        targetProgress = Math.Clamp(targetProgress, 0, 1);
        if (targetProgress > 0 && !HasDrawerForSide(side))
        {
            return;
        }

        var currentProgress = ResolveDrawerProgress(side);
        currentProgress = Math.Clamp(currentProgress, 0, 1);

        if (Math.Abs(currentProgress - targetProgress) <= 0.0001)
        {
            CommitProgress(side, targetProgress);
            return;
        }

        StopSettleAnimation(side);
        var duration = ResolveSettleDuration(currentProgress, targetProgress, normalizedVelocityHint);
        var controller = new AnimationController(duration)
        {
            Curve = Curves.EaseOut
        };

        if (side == DrawerSide.Start)
        {
            _drawerAnimationController = controller;
            _drawerAnimationFrom = currentProgress;
            _drawerAnimationTo = targetProgress;
            controller.Changed += HandleDrawerAnimationTick;
            controller.Completed += HandleDrawerAnimationCompleted;
            controller.Dismissed += HandleDrawerAnimationCompleted;
        }
        else
        {
            _endDrawerAnimationController = controller;
            _endDrawerAnimationFrom = currentProgress;
            _endDrawerAnimationTo = targetProgress;
            controller.Changed += HandleEndDrawerAnimationTick;
            controller.Completed += HandleEndDrawerAnimationCompleted;
            controller.Dismissed += HandleEndDrawerAnimationCompleted;
        }

        controller.Forward(0);
    }

    private static TimeSpan ResolveSettleDuration(double currentProgress, double targetProgress, double? normalizedVelocityHint)
    {
        var distance = Math.Abs(targetProgress - currentProgress);
        if (distance <= 0)
        {
            return TimeSpan.FromMilliseconds(1);
        }

        double durationMs;
        var velocity = Math.Abs(normalizedVelocityHint ?? 0);
        if (velocity > double.Epsilon)
        {
            durationMs = distance / velocity * 1000;
        }
        else
        {
            durationMs = MaxSettleDurationMs * distance;
        }

        durationMs = Math.Clamp(durationMs, MinSettleDurationMs, MaxSettleDurationMs);
        return TimeSpan.FromMilliseconds(durationMs);
    }

    private void StopSettleAnimation(DrawerSide side)
    {
        if (side == DrawerSide.Start)
        {
            if (_drawerAnimationController == null)
            {
                return;
            }

            _drawerAnimationController.Changed -= HandleDrawerAnimationTick;
            _drawerAnimationController.Completed -= HandleDrawerAnimationCompleted;
            _drawerAnimationController.Dismissed -= HandleDrawerAnimationCompleted;
            _drawerAnimationController.Dispose();
            _drawerAnimationController = null;
            return;
        }

        if (_endDrawerAnimationController == null)
        {
            return;
        }

        _endDrawerAnimationController.Changed -= HandleEndDrawerAnimationTick;
        _endDrawerAnimationController.Completed -= HandleEndDrawerAnimationCompleted;
        _endDrawerAnimationController.Dismissed -= HandleEndDrawerAnimationCompleted;
        _endDrawerAnimationController.Dispose();
        _endDrawerAnimationController = null;
    }

    private void HandleDrawerAnimationTick()
    {
        if (_drawerAnimationController == null)
        {
            return;
        }

        var value = _drawerAnimationController.Evaluate();
        var progress = Math.Clamp(_drawerAnimationFrom + (_drawerAnimationTo - _drawerAnimationFrom) * value, 0, 1);
        SetState(() => _drawerProgress = progress);
    }

    private void HandleEndDrawerAnimationTick()
    {
        if (_endDrawerAnimationController == null)
        {
            return;
        }

        var value = _endDrawerAnimationController.Evaluate();
        var progress = Math.Clamp(_endDrawerAnimationFrom + (_endDrawerAnimationTo - _endDrawerAnimationFrom) * value, 0, 1);
        SetState(() => _endDrawerProgress = progress);
    }

    private void HandleDrawerAnimationCompleted()
    {
        SetState(() =>
        {
            CommitProgress(DrawerSide.Start, _drawerAnimationTo);
            StopSettleAnimation(DrawerSide.Start);
        });
    }

    private void HandleEndDrawerAnimationCompleted()
    {
        SetState(() =>
        {
            CommitProgress(DrawerSide.End, _endDrawerAnimationTo);
            StopSettleAnimation(DrawerSide.End);
        });
    }

    private void CommitProgress(DrawerSide side, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        if (side == DrawerSide.Start)
        {
            _drawerProgress = progress;
            return;
        }

        _endDrawerProgress = progress;
    }

    private void UpdateOpenFlagsFromProgress(DrawerSide side, double progress)
    {
        var isOpen = progress >= OpenThreshold;
        if (side == DrawerSide.Start)
        {
            _isDrawerOpen = isOpen && HasDrawer;
            _isEndDrawerOpen = false;
            return;
        }

        _isEndDrawerOpen = isOpen && HasEndDrawer;
        _isDrawerOpen = false;
    }

    private void SyncDrawerHistoryEntry(BuildContext context)
    {
        if (_isDisposed)
        {
            return;
        }

        if (!ShouldMaintainDrawerHistoryEntry())
        {
            RemoveDrawerHistoryEntry();
            return;
        }

        var route = ModalRoute.MaybeOf(context);
        if (route == null)
        {
            RemoveDrawerHistoryEntry();
            return;
        }

        if (_drawerHistoryEntry != null && ReferenceEquals(_drawerHistoryRoute, route))
        {
            return;
        }

        RemoveDrawerHistoryEntry();

        var entry = new LocalHistoryEntry(onRemove: HandleDrawerHistoryEntryRemoved);
        route.AddLocalHistoryEntry(entry);
        _drawerHistoryEntry = entry;
        _drawerHistoryRoute = route;
    }

    private bool ShouldMaintainDrawerHistoryEntry()
    {
        return _activeDragSide != null
               || _isDrawerOpen
               || _isEndDrawerOpen;
    }

    private void RemoveDrawerHistoryEntry()
    {
        var entry = _drawerHistoryEntry;
        if (entry == null)
        {
            _drawerHistoryRoute = null;
            return;
        }

        _drawerHistoryEntry = null;
        _drawerHistoryRoute = null;
        _isRemovingDrawerHistoryEntry = true;

        try
        {
            entry.Remove();
        }
        finally
        {
            _isRemovingDrawerHistoryEntry = false;
        }
    }

    private void HandleDrawerHistoryEntryRemoved()
    {
        _drawerHistoryEntry = null;
        _drawerHistoryRoute = null;

        if (_isRemovingDrawerHistoryEntry || _isDisposed)
        {
            return;
        }

        CloseOpenDrawers();
    }

    private static DrawerSide OppositeOf(DrawerSide side)
    {
        return side == DrawerSide.Start ? DrawerSide.End : DrawerSide.Start;
    }

    private static double ResolveDrawerWidth(Widget drawer)
    {
        if (drawer is Drawer typedDrawer)
        {
            return typedDrawer.Width ?? DefaultDrawerWidth;
        }

        return DefaultDrawerWidth;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        var effectiveOpacity = Math.Clamp(opacity, 0, 1);
        var alpha = (byte)Math.Clamp((int)Math.Round(color.A * effectiveOpacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

public sealed class AppBar : StatelessWidget
{
    public AppBar(
        string? titleText = null,
        Widget? title = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyActions = true,
        double? leadingWidth = null,
        IReadOnlyList<Widget>? actions = null,
        bool? centerTitle = null,
        bool primary = true,
        double? titleSpacing = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        Thickness? actionsPadding = null,
        double? toolbarHeight = null,
        Thickness? padding = null,
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        Key? key = null) : base(key)
    {
        if (toolbarHeight.HasValue && (double.IsNaN(toolbarHeight.Value) || double.IsInfinity(toolbarHeight.Value) || toolbarHeight.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(toolbarHeight), "Toolbar height must be positive and finite.");
        }

        if (leadingWidth.HasValue && (double.IsNaN(leadingWidth.Value) || double.IsInfinity(leadingWidth.Value) || leadingWidth.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(leadingWidth), "Leading width must be positive and finite.");
        }

        if (titleSpacing.HasValue && (double.IsNaN(titleSpacing.Value) || double.IsInfinity(titleSpacing.Value) || titleSpacing.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(titleSpacing), "Title spacing must be non-negative and finite.");
        }

        TitleText = titleText;
        Title = title;
        Leading = leading;
        AutomaticallyImplyLeading = automaticallyImplyLeading;
        AutomaticallyImplyActions = automaticallyImplyActions;
        LeadingWidth = leadingWidth;
        Actions = actions ?? Array.Empty<Widget>();
        CenterTitle = centerTitle;
        Primary = primary;
        TitleSpacing = titleSpacing;
        IconTheme = iconTheme;
        ActionsIconTheme = actionsIconTheme;
        ToolbarTextStyle = toolbarTextStyle;
        TitleTextStyle = titleTextStyle;
        ActionsPadding = actionsPadding;
        ToolbarHeight = toolbarHeight;
        Padding = padding;
        BackgroundColor = backgroundColor;
        ForegroundColor = foregroundColor;
        SystemOverlayStyle = systemOverlayStyle;
    }

    public string? TitleText { get; }

    public Widget? Title { get; }

    public Widget? Leading { get; }

    public bool AutomaticallyImplyLeading { get; }

    public bool AutomaticallyImplyActions { get; }

    public double? LeadingWidth { get; }

    public IReadOnlyList<Widget> Actions { get; }

    public bool? CenterTitle { get; }

    public bool Primary { get; }

    public double? TitleSpacing { get; }

    public IconThemeData? IconTheme { get; }

    public IconThemeData? ActionsIconTheme { get; }

    public TextStyle? ToolbarTextStyle { get; }

    public TextStyle? TitleTextStyle { get; }

    public Thickness? ActionsPadding { get; }

    public double? ToolbarHeight { get; }

    public Thickness? Padding { get; }

    public Color? BackgroundColor { get; }

    public Color? ForegroundColor { get; }

    public SystemUiOverlayStyle? SystemOverlayStyle { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var effectiveBackground = BackgroundColor ?? theme.AppBarTheme.BackgroundColor ?? ResolveDefaultBackgroundColor(theme);
        var effectiveForeground = ForegroundColor ?? theme.AppBarTheme.ForegroundColor ?? ResolveDefaultForegroundColor(theme);
        var effectiveCenterTitle = ResolveEffectiveCenterTitle(theme);
        var effectiveTitleSpacing = TitleSpacing ?? theme.AppBarTheme.TitleSpacing ?? 16;
        var effectiveIconTheme = ResolveEffectiveIconTheme(theme, effectiveForeground);
        var effectiveActionsIconTheme = ResolveEffectiveActionsIconTheme(theme, effectiveForeground, effectiveIconTheme);
        var effectiveLeading = ResolveEffectiveLeading(context);
        var effectiveActions = ResolveEffectiveActions(context);
        var effectiveLeadingWidth = ResolveEffectiveLeadingWidth(theme);
        var effectiveActionsPadding = ActionsPadding ?? theme.AppBarTheme.ActionsPadding ?? new Thickness();
        var effectiveToolbarHeight = ResolveEffectiveToolbarHeight(theme);
        var effectiveToolbarTextStyle = ResolveToolbarTextStyle(theme, effectiveForeground);
        var effectiveTitleTextStyle = ResolveTitleTextStyle(theme, effectiveForeground);
        var effectiveSystemOverlayStyle = ResolveEffectiveSystemOverlayStyle(theme, effectiveBackground);

        var titleWidget = (Widget)new DefaultTextStyle(
            style: effectiveTitleTextStyle,
            child: Title ?? BuildDefaultTitle());
        var middle = (Widget)new Padding(
            insets: new Thickness(effectiveTitleSpacing, 0, effectiveTitleSpacing, 0),
            child: effectiveCenterTitle
                ? new Center(child: titleWidget)
                : titleWidget);

        var rowChildren = new List<Widget>();
        if (effectiveLeading != null)
        {
            rowChildren.Add(
                new SizedBox(
                    width: effectiveLeadingWidth,
                    height: effectiveToolbarHeight,
                    child: new Center(
                        child: new Flutter.Widgets.IconTheme(
                            data: effectiveIconTheme,
                            child: effectiveLeading))));
        }

        rowChildren.Add(new Expanded(child: middle));

        if (effectiveActions.Count > 0)
        {
            rowChildren.Add(new Padding(
                insets: effectiveActionsPadding,
                child: new Flutter.Widgets.IconTheme(
                    data: effectiveActionsIconTheme,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: theme.UseMaterial3
                            ? CrossAxisAlignment.Center
                            : CrossAxisAlignment.Stretch,
                        spacing: 0,
                        children: effectiveActions))));
        }
        else if (effectiveCenterTitle && effectiveLeading != null)
        {
            // Reserve symmetric trailing space when centering title without explicit actions.
            rowChildren.Add(new SizedBox(width: effectiveLeadingWidth));
        }

        Widget appBarContent = new SizedBox(
            height: effectiveToolbarHeight,
            child: new DefaultTextStyle(
                style: effectiveToolbarTextStyle,
                child: new Row(
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    spacing: 0,
                    children: rowChildren)));

        if (Primary && MediaQuery.MaybeOf(context) != null)
        {
            appBarContent = new SafeArea(bottom: false, child: appBarContent);
        }

        SystemChrome.SetSystemUiOverlayStyle(effectiveSystemOverlayStyle);

        return new Container(
            color: effectiveBackground,
            padding: Padding ?? new Thickness(),
            child: appBarContent);
    }

    private bool ResolveEffectiveCenterTitle(ThemeData theme)
    {
        if (CenterTitle.HasValue)
        {
            return CenterTitle.Value;
        }

        if (theme.AppBarTheme.CenterTitle.HasValue)
        {
            return theme.AppBarTheme.CenterTitle.Value;
        }

        return ResolvePlatformDefaultCenterTitle(theme.Platform);
    }

    private Widget? ResolveEffectiveLeading(BuildContext context)
    {
        if (Leading != null)
        {
            return Leading;
        }

        if (!AutomaticallyImplyLeading)
        {
            return null;
        }

        var scaffold = Scaffold.MaybeOf(context);
        if (scaffold?.HasDrawer == true)
        {
            return BuildDefaultDrawerLeading(context);
        }

        var route = ModalRoute.MaybeOf(context);
        var impliesAppBarDismissal = route?.ImpliesAppBarDismissal ?? Navigator.CanPop(context);
        if (!impliesAppBarDismissal)
        {
            return null;
        }

        var useCloseButton = route is PageRoute pageRoute && pageRoute.FullscreenDialog;
        return BuildDefaultLeading(context, useCloseButton);
    }

    private IReadOnlyList<Widget> ResolveEffectiveActions(BuildContext context)
    {
        if (Actions.Count > 0)
        {
            return Actions;
        }

        if (!AutomaticallyImplyActions)
        {
            return Array.Empty<Widget>();
        }

        var scaffold = Scaffold.MaybeOf(context);
        if (scaffold?.HasEndDrawer == true)
        {
            return
            [
                BuildDefaultEndDrawerAction(context),
            ];
        }

        return Array.Empty<Widget>();
    }

    private static Widget BuildDefaultDrawerLeading(BuildContext context)
    {
        return new IconButton(
            icon: new Icon(Icons.Menu),
            onPressed: () => Scaffold.Of(context).OpenDrawer());
    }

    private static Widget BuildDefaultEndDrawerAction(BuildContext context)
    {
        return new IconButton(
            icon: new Icon(Icons.Menu),
            onPressed: () => Scaffold.Of(context).OpenEndDrawer());
    }

    private static Widget BuildDefaultLeading(BuildContext context, bool useCloseButton)
    {
        return new IconButton(
            icon: new Icon(useCloseButton ? Icons.Close : Icons.ArrowBack),
            onPressed: () => Navigator.MaybePop(context));
    }

    private double ResolveEffectiveLeadingWidth(ThemeData theme)
    {
        var effectiveLeadingWidth = LeadingWidth ?? theme.AppBarTheme.LeadingWidth ?? 56;
        if (double.IsNaN(effectiveLeadingWidth)
            || double.IsInfinity(effectiveLeadingWidth)
            || effectiveLeadingWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AppBarThemeData.LeadingWidth),
                "Leading width must be positive and finite.");
        }

        return effectiveLeadingWidth;
    }

    private IconThemeData ResolveEffectiveIconTheme(ThemeData theme, Color effectiveForeground)
    {
        var baseTheme = IconTheme
                        ?? theme.AppBarTheme.IconTheme
                        ?? ResolveDefaultIconTheme(theme, effectiveForeground);
        return baseTheme with
        {
            Color = baseTheme.Color ?? effectiveForeground,
        };
    }

    private IconThemeData ResolveEffectiveActionsIconTheme(
        ThemeData theme,
        Color effectiveForeground,
        IconThemeData effectiveIconTheme)
    {
        var actionForeground = ForegroundColor ?? theme.AppBarTheme.ForegroundColor;
        var baseTheme = ActionsIconTheme
                        ?? theme.AppBarTheme.ActionsIconTheme
                        ?? IconTheme
                        ?? theme.AppBarTheme.IconTheme
                        ?? ResolveDefaultActionsIconTheme(theme, actionForeground, effectiveIconTheme);

        return baseTheme with
        {
            Color = baseTheme.Color ?? actionForeground ?? effectiveForeground,
        };
    }

    private double ResolveEffectiveToolbarHeight(ThemeData theme)
    {
        var effectiveToolbarHeight = ToolbarHeight
                                     ?? theme.AppBarTheme.ToolbarHeight
                                     ?? ResolveDefaultToolbarHeight();
        if (double.IsNaN(effectiveToolbarHeight)
            || double.IsInfinity(effectiveToolbarHeight)
            || effectiveToolbarHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AppBarThemeData.ToolbarHeight),
                "Toolbar height must be positive and finite.");
        }

        return effectiveToolbarHeight;
    }

    private static double ResolveDefaultToolbarHeight()
    {
        return 56;
    }

    private static Color ResolveDefaultBackgroundColor(ThemeData theme)
    {
        if (theme.UseMaterial3)
        {
            return theme.CanvasColor;
        }

        return theme.Brightness == Brightness.Dark
            ? theme.CanvasColor
            : theme.PrimaryColor;
    }

    private static Color ResolveDefaultForegroundColor(ThemeData theme)
    {
        if (theme.UseMaterial3)
        {
            return theme.OnSurfaceColor;
        }

        return theme.Brightness == Brightness.Dark
            ? theme.OnSurfaceColor
            : theme.OnPrimaryColor;
    }

    private static IconThemeData ResolveDefaultIconTheme(ThemeData theme, Color effectiveForeground)
    {
        return theme.UseMaterial3
            ? new IconThemeData(Color: effectiveForeground, Size: 24)
            : new IconThemeData(Color: effectiveForeground);
    }

    private static IconThemeData ResolveDefaultActionsIconTheme(
        ThemeData theme,
        Color? actionForeground,
        IconThemeData effectiveIconTheme)
    {
        if (!theme.UseMaterial3)
        {
            return effectiveIconTheme;
        }

        return new IconThemeData(
            Color: actionForeground ?? theme.OnSurfaceVariantColor,
            Size: effectiveIconTheme.Size ?? 24);
    }

    private bool ResolvePlatformDefaultCenterTitle(TargetPlatform platform)
    {
        if (platform is TargetPlatform.IOS or TargetPlatform.MacOS)
        {
            return Actions.Count < 2;
        }

        return false;
    }

    private TextStyle ResolveToolbarTextStyle(ThemeData theme, Color effectiveForeground)
    {
        var baseStyle = theme.TextTheme.BodyMedium with
        {
            Color = effectiveForeground,
        };

        var overrideStyle = ToolbarTextStyle ?? theme.AppBarTheme.ToolbarTextStyle;
        return ComposeTextStyle(baseStyle, overrideStyle);
    }

    private TextStyle ResolveTitleTextStyle(ThemeData theme, Color effectiveForeground)
    {
        var baseStyle = theme.TextTheme.TitleLarge with
        {
            Color = effectiveForeground,
        };

        var overrideStyle = TitleTextStyle ?? theme.AppBarTheme.TitleTextStyle;
        return ComposeTextStyle(baseStyle, overrideStyle);
    }

    private SystemUiOverlayStyle ResolveEffectiveSystemOverlayStyle(ThemeData theme, Color effectiveBackground)
    {
        return SystemOverlayStyle
               ?? theme.AppBarTheme.SystemOverlayStyle
               ?? ResolveDefaultSystemOverlayStyle(theme, effectiveBackground);
    }

    private static SystemUiOverlayStyle ResolveDefaultSystemOverlayStyle(ThemeData theme, Color effectiveBackground)
    {
        var iconBrightness = EstimateIconBrightnessForColor(effectiveBackground);
        var systemBarColor = effectiveBackground;
        return new SystemUiOverlayStyle(
            StatusBarColor: systemBarColor,
            NavigationBarColor: systemBarColor,
            StatusBarIconBrightness: iconBrightness,
            NavigationBarIconBrightness: iconBrightness);
    }

    private static SystemUiIconBrightness EstimateIconBrightnessForColor(Color color)
    {
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        return luminance > 0.5 ? SystemUiIconBrightness.Dark : SystemUiIconBrightness.Light;
    }

    private static TextStyle ComposeTextStyle(TextStyle baseStyle, TextStyle? overrideStyle)
    {
        if (overrideStyle is null)
        {
            return baseStyle;
        }

        return baseStyle with
        {
            FontFamily = overrideStyle.FontFamily ?? baseStyle.FontFamily,
            FontSize = overrideStyle.FontSize ?? baseStyle.FontSize,
            Color = overrideStyle.Color ?? baseStyle.Color,
            FontWeight = overrideStyle.FontWeight ?? baseStyle.FontWeight,
            FontStyle = overrideStyle.FontStyle ?? baseStyle.FontStyle,
            Height = overrideStyle.Height ?? baseStyle.Height,
            LetterSpacing = overrideStyle.LetterSpacing ?? baseStyle.LetterSpacing,
        };
    }

    private Widget BuildDefaultTitle()
    {
        if (TitleText is null)
        {
            return new SizedBox();
        }

        return new Text(
            TitleText,
            softWrap: false,
            maxLines: 1,
            overflow: TextOverflow.Ellipsis);
    }
}
