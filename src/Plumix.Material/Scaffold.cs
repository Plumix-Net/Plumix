using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources (reference): flutter/packages/flutter/lib/src/material/scaffold.dart;
// flutter/packages/flutter/lib/src/material/app_bar.dart

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
        FloatingActionButtonLocation? floatingActionButtonLocation = null,
        FloatingActionButtonAnimator? floatingActionButtonAnimator = null,
        Widget? bottomNavigationBar = null,
        Color? backgroundColor = null,
        Key? key = null,
        Widget? bottomSheet = null,
        BottomSheetScrimBuilder? bottomSheetScrimBuilder = null) : base(key)
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
        FloatingActionButtonLocation = floatingActionButtonLocation
                                      ?? Plumix.Material.FloatingActionButtonLocation.EndFloat;
        FloatingActionButtonAnimator = floatingActionButtonAnimator
                                      ?? Plumix.Material.FloatingActionButtonAnimator.Scaling;
        BottomNavigationBar = bottomNavigationBar;
        BottomSheet = bottomSheet;
        BottomSheetScrimBuilder = bottomSheetScrimBuilder ?? DefaultBottomSheetScrimBuilder;
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

    public FloatingActionButtonLocation FloatingActionButtonLocation { get; }

    public FloatingActionButtonAnimator FloatingActionButtonAnimator { get; }

    public Widget? BottomNavigationBar { get; }

    public Widget? BottomSheet { get; }

    /// <summary>
    /// Builds the scrim shown over the body while a draggable bottom sheet dominates the screen. The animation
    /// runs from 0.0 (the sheet covers 70% of the screen) to 1.0 (the sheet covers the screen); returning
    /// <see langword="null"/> suppresses the scrim.
    /// </summary>
    public BottomSheetScrimBuilder BottomSheetScrimBuilder { get; }

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

    internal static ScaffoldGeometryData? GeometryMaybeOf(BuildContext context)
    {
        return context.DependOnInherited<ScaffoldScope>()?.Geometry;
    }

    internal const double BottomSheetDominatesPercentage = 0.3;
    internal const double MinBottomSheetScrimOpacity = 0.1;
    internal const double MaxBottomSheetScrimOpacity = 0.6;

    private static Widget? DefaultBottomSheetScrimBuilder(BuildContext context, Animation<double> animation)
    {
        return new AnimatedBuilder(
            animation: animation,
            builder: (_, _) =>
            {
                double extentRemaining = BottomSheetDominatesPercentage * (1.0 - animation.Value);
                double floatingButtonVisibilityValue = extentRemaining * BottomSheetDominatesPercentage * 10;
                double opacity = Math.Max(
                    MinBottomSheetScrimOpacity,
                    MaxBottomSheetScrimOpacity - floatingButtonVisibilityValue);
                return new ModalBarrier(
                    dismissible: false,
                    color: Color.FromArgb((byte)Math.Round(opacity * 255), 0, 0, 0));
            });
    }
}

/// <summary>Builds the scrim painted over the scaffold body while a bottom sheet dominates the screen.</summary>
public delegate Widget? BottomSheetScrimBuilder(BuildContext context, Animation<double> animation);

internal sealed record ScaffoldGeometryData(
    Rect? FloatingActionButtonArea,
    double? BottomNavigationBarTop);

internal sealed class ScaffoldScope : InheritedWidget
{
    public ScaffoldScope(
        ScaffoldState scaffold,
        bool hasDrawer,
        bool hasEndDrawer,
        bool isDrawerOpen,
        bool isEndDrawerOpen,
        ScaffoldGeometryData geometry,
        Widget child,
        Key? key = null) : base(key)
    {
        Scaffold = scaffold ?? throw new ArgumentNullException(nameof(scaffold));
        HasDrawer = hasDrawer;
        HasEndDrawer = hasEndDrawer;
        IsDrawerOpen = isDrawerOpen;
        IsEndDrawerOpen = isEndDrawerOpen;
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScaffoldState Scaffold { get; }

    public bool HasDrawer { get; }

    public bool HasEndDrawer { get; }

    public bool IsDrawerOpen { get; }

    public bool IsEndDrawerOpen { get; }

    public ScaffoldGeometryData Geometry { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (ScaffoldScope)oldWidget;
        return !ReferenceEquals(Scaffold, oldScope.Scaffold)
               || HasDrawer != oldScope.HasDrawer
               || HasEndDrawer != oldScope.HasEndDrawer
               || IsDrawerOpen != oldScope.IsDrawerOpen
               || IsEndDrawerOpen != oldScope.IsEndDrawerOpen
               || Geometry != oldScope.Geometry;
    }
}

public sealed class ScaffoldState : State
{
    private const double DefaultDrawerWidth = 304.0;
    private const double DefaultOpenThreshold = 0.5;
    private const double MinFlingVelocityPixelsPerSecond = 365.0;
    private static readonly TimeSpan BaseSettleDuration = TimeSpan.FromMilliseconds(246);
    private bool _isDrawerOpen;
    private bool _isEndDrawerOpen;
    private double _drawerProgress;
    private double _endDrawerProgress;
    private DrawerSide? _activeDragSide;
    private double _activeDragProgress;
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
    private PersistentBottomSheetPresentation? _persistentBottomSheet;
    private AnimationController? _staticBottomSheetAnimation;
    private ScaffoldMessengerState? _scaffoldMessenger;
    private AnimationController _bottomSheetScrimAnimationController = null!;
    private AnimationController _floatingActionButtonVisibilityController = null!;
    private CurvedAnimation _floatingActionButtonScaleAnimation = null!;
    private bool _showBodyScrim;
    private LocalHistoryEntry? _persistentSheetHistoryEntry;

    // Flutter identifies the scaffold's children by `_ScaffoldSlot`; the overlay stack keys them so that a
    // slot appearing or disappearing never shifts another slot's element onto a different widget.
    private static readonly Key SnackBarSlotKey = new ValueKey<string>("scaffold.snackBar");
    private static readonly Key BodyScrimSlotKey = new ValueKey<string>("scaffold.bodyScrim");
    private static readonly Key BottomSheetSlotKey = new ValueKey<string>("scaffold.bottomSheet");
    private static readonly Key MaterialBannerSlotKey = new ValueKey<string>("scaffold.materialBanner");

    private Scaffold CurrentWidget => (Scaffold)StateWidget;

    public bool HasDrawer => CurrentWidget.Drawer != null;

    public bool HasEndDrawer => CurrentWidget.EndDrawer != null;

    public bool HasFloatingActionButton => CurrentWidget.FloatingActionButton != null;

    public Size FloatingActionButtonSize => CurrentWidget.FloatingActionButton is FloatingActionButton button
        ? button.ResolveNominalSizeForScaffold(Context)
        : new Size(56, 56);

    public bool IsDrawerOpen => _isDrawerOpen;

    public bool IsEndDrawerOpen => _isEndDrawerOpen;

    public override void InitState()
    {
        _isDisposed = false;
        _drawerProgress = 0;
        _endDrawerProgress = 0;
        _bottomSheetScrimAnimationController = new AnimationController(TimeSpan.Zero, this);
        _floatingActionButtonVisibilityController =
            new AnimationController(FloatingActionButtonConstants.Segue, this);
        _floatingActionButtonVisibilityController.SetValue(1.0);
        _floatingActionButtonVisibilityController.Changed += HandleFloatingActionButtonVisibilityChanged;
        _floatingActionButtonScaleAnimation = new CurvedAnimation(
            _floatingActionButtonVisibilityController,
            curve: Curves.EaseIn);
        SyncStaticBottomSheetAnimation();
    }

    public override void Dispose()
    {
        _isDisposed = true;
        _scaffoldMessenger?.Unregister(this);
        _scaffoldMessenger = null;
        RemoveDrawerHistoryEntry();
        RemovePersistentSheetHistoryEntry();
        StopSettleAnimation(DrawerSide.Start);
        StopSettleAnimation(DrawerSide.End);
        DisposeStaticBottomSheetAnimation();
        DisposePersistentBottomSheet(complete: true);
        _floatingActionButtonVisibilityController.Changed -= HandleFloatingActionButtonVisibilityChanged;
        _floatingActionButtonScaleAnimation.Dispose();
        _floatingActionButtonVisibilityController.Dispose();
        _bottomSheetScrimAnimationController.Dispose();
    }

    /// <summary>
    /// Shows or hides the scrim painted over the body while a draggable bottom sheet dominates the screen.
    /// </summary>
    public void ShowBodyScrim(bool value, double animationValue)
    {
        if (_showBodyScrim != value)
        {
            SetState(() => _showBodyScrim = value);
        }

        if (_bottomSheetScrimAnimationController.Value != animationValue)
        {
            _bottomSheetScrimAnimationController.SetValue(animationValue);
        }
    }

    /// <summary>The controller a dominating bottom sheet drives to shrink the floating action button.</summary>
    internal AnimationController FloatingActionButtonVisibilityController =>
        _floatingActionButtonVisibilityController;

    /// <summary>Whether the scaffold hosts a <see cref="Scaffold.BottomSheet"/> rather than a shown sheet.</summary>
    internal bool HasStaticBottomSheet => CurrentWidget.BottomSheet is not null;

    private void HandleFloatingActionButtonVisibilityChanged()
    {
        if (!_isDisposed) SetState(() => { });
    }

    private void ShowFloatingActionButton() => _floatingActionButtonVisibilityController.Forward();

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        ScaffoldMessengerState? messenger = ScaffoldMessenger.MaybeOf(Context);
        if (ReferenceEquals(messenger, _scaffoldMessenger))
        {
            return;
        }

        _scaffoldMessenger?.Unregister(this);
        _scaffoldMessenger = messenger;
        _scaffoldMessenger?.Register(this);
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

    public PersistentBottomSheetController ShowBottomSheet(
        WidgetBuilder builder,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        bool? enableDrag = null,
        bool? showDragHandle = null,
        AnimationController? transitionAnimationController = null,
        AnimationStyle? sheetAnimationStyle = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (CurrentWidget.BottomSheet is not null)
        {
            throw new InvalidOperationException(
                "Scaffold.showBottomSheet cannot be used while Scaffold.bottomSheet is non-null.");
        }
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));

        ClosePersistentBottomSheet(immediate: true);
        var animation = transitionAnimationController
                        ?? BottomSheet.CreateAnimationController(vsync: this, sheetAnimationStyle: sheetAnimationStyle);
        var presentation = new PersistentBottomSheetPresentation
        {
            Builder = builder,
            Animation = animation,
            OwnsAnimation = transitionAnimationController is null,
            ExitDuration = transitionAnimationController?.Duration
                           ?? sheetAnimationStyle?.ReverseDuration
                           ?? BottomSheet.ExitDuration,
            EnableDrag = enableDrag ?? true,
            ShowDragHandle = showDragHandle,
            BackgroundColor = backgroundColor,
            Elevation = elevation,
            Shape = shape,
            ClipBehavior = clipBehavior,
            Constraints = constraints,
        };
        animation.Changed += HandlePersistentBottomSheetAnimationChanged;
        animation.Dismissed += HandlePersistentBottomSheetDismissed;
        var route = ModalRoute.MaybeOf(Context);
        if (route is not null)
        {
            presentation.HistoryEntry = new LocalHistoryEntry(
                onRemove: () => ClosePersistentBottomSheet(),
                impliesAppBarDismissal: true);
            route.AddLocalHistoryEntry(presentation.HistoryEntry);
        }
        _persistentBottomSheet = presentation;
        SetState(() => { });
        animation.Forward(from: 0);

        return new PersistentBottomSheetController(
            close: ClosePersistentBottomSheet,
            setState: callback =>
            {
                if (!ReferenceEquals(_persistentBottomSheet, presentation)) return;
                SetState(callback);
            },
            closed: presentation.Closed.Task);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldScaffold = (Scaffold)oldWidget;
        if (!ReferenceEquals(oldScaffold.BottomSheet, CurrentWidget.BottomSheet))
        {
            SyncStaticBottomSheetAnimation();
        }
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
        ScaffoldMessengerState? messenger = ScaffoldMessenger.MaybeOf(context);
        var presentedSnackBar = messenger?.SnackBarFor(this);
        MaterialBanner? presentedMaterialBanner = messenger?.MaterialBannerFor(this);
        double materialBannerElevation = presentedMaterialBanner?.Elevation
                                         ?? MaterialBannerTheme.Of(context).Elevation
                                         ?? 0.0;
        var presentedSnackBarBehavior = presentedSnackBar?.Behavior
                                        ?? SnackBarTheme.Of(context).Behavior
                                        ?? SnackBarBehavior.Fixed;

        var columnChildren = new List<Widget>();
        if (CurrentWidget.AppBar != null)
        {
            columnChildren.Add(CurrentWidget.AppBar);
        }

        if (presentedMaterialBanner is not null && materialBannerElevation == 0.0)
        {
            columnChildren.Add(MediaQuery.RemovePadding(
                context,
                presentedMaterialBanner,
                removeTop: CurrentWidget.AppBar is not null,
                removeBottom: true));
        }

        columnChildren.Add(new Expanded(child: CurrentWidget.Body));

        if (presentedSnackBar is not null && presentedSnackBarBehavior == SnackBarBehavior.Fixed)
        {
            columnChildren.Add(presentedSnackBar);
        }

        if (CurrentWidget.BottomNavigationBar != null)
        {
            columnChildren.Add(CurrentWidget.BottomNavigationBar);
        }

        Widget content = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children: columnChildren);

        var textDirection = Directionality.Of(context);
        var mediaQuery = MediaQuery.MaybeOf(context) ?? new MediaQueryData();
        double? bottomNavigationBarTop = null;
        if (CurrentWidget.BottomNavigationBar is BottomAppBar bottomAppBar)
        {
            bottomNavigationBarTop = Math.Max(
                0.0,
                mediaQuery.Size.Height - bottomAppBar.ResolveHeightForScaffold(context));
        }

        Rect? floatingActionButtonArea = null;

        if (CurrentWidget.FloatingActionButton != null)
        {
            double appBarHeight = CurrentWidget.AppBar?.PreferredSize.Height ?? 0.0;
            double bottomNavigationHeight = bottomNavigationBarTop.HasValue
                ? mediaQuery.Size.Height - bottomNavigationBarTop.Value
                : 0.0;
            var geometry = new ScaffoldPrelayoutGeometry(
                ScaffoldSize: mediaQuery.Size,
                ContentTop: appBarHeight,
                ContentBottom: Math.Max(0.0, mediaQuery.Size.Height - bottomNavigationHeight),
                FloatingActionButtonSize: FloatingActionButtonSize,
                BottomSheetSize: new Size(),
                SnackBarSize: new Size(),
                MinInsets: mediaQuery.ViewInsets,
                MinViewPadding: mediaQuery.ViewPadding,
                TextDirection: textDirection,
                MaterialBannerSize: new Size());
            Point floatingActionButtonOffset = CurrentWidget.FloatingActionButtonLocation.GetOffset(geometry);
            floatingActionButtonArea = new Rect(
                floatingActionButtonOffset,
                geometry.FloatingActionButtonSize);
            content = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    content,
                    new Positioned(
                        left: 0,
                        top: 0,
                        right: 0,
                        bottom: 0,
                        child: new FloatingActionButtonPosition(
                            geometry,
                            CurrentWidget.FloatingActionButtonLocation,
                            new ScaleTransition(
                                scale: _floatingActionButtonScaleAnimation,
                                child: CurrentWidget.FloatingActionButton))),
                ]);
        }

        double drawerProgress = ResolveDrawerProgress(DrawerSide.Start);
        double endDrawerProgress = ResolveDrawerProgress(DrawerSide.End);
        bool isStartDrawerVisible = IsDrawerVisible(DrawerSide.Start, drawerProgress);
        bool isEndDrawerVisible = IsDrawerVisible(DrawerSide.End, endDrawerProgress);
        bool isAnyDrawerVisible = isStartDrawerVisible || isEndDrawerVisible;
        var overlayChildren = new List<Widget> { content };
        if (presentedSnackBar is not null && presentedSnackBarBehavior == SnackBarBehavior.Floating)
        {
            overlayChildren.Add(new Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                key: SnackBarSlotKey,
                child: presentedSnackBar));
        }

        if (_showBodyScrim
            && CurrentWidget.BottomSheetScrimBuilder(context, _bottomSheetScrimAnimationController) is { } bodyScrim)
        {
            double scrimBottom = bottomNavigationBarTop.HasValue
                ? Math.Max(0.0, mediaQuery.Size.Height - bottomNavigationBarTop.Value)
                : 0.0;
            overlayChildren.Add(new Positioned(
                left: 0,
                top: 0,
                right: 0,
                bottom: scrimBottom,
                key: BodyScrimSlotKey,
                child: MediaQuery.RemovePadding(
                    context,
                    bodyScrim,
                    removeLeft: true,
                    removeTop: true,
                    removeRight: true,
                    removeBottom: true)));
        }

        var bottomSheet = BuildPresentedBottomSheet(context);
        if (bottomSheet is not null)
        {
            overlayChildren.Add(new Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                key: BottomSheetSlotKey,
                child: bottomSheet));
        }

        if (presentedMaterialBanner is not null && materialBannerElevation != 0.0)
        {
            double appBarHeight = CurrentWidget.AppBar?.PreferredSize.Height ?? 0.0;
            overlayChildren.Add(new Positioned(
                left: 0,
                top: appBarHeight,
                right: 0,
                key: MaterialBannerSlotKey,
                child: MediaQuery.RemovePadding(
                    context,
                    presentedMaterialBanner,
                    removeTop: CurrentWidget.AppBar is not null,
                    removeBottom: true)));
        }

        if (!isAnyDrawerVisible)
        {
            if (ShouldEnableOpenDragGesture(DrawerSide.Start, theme))
            {
                overlayChildren.Add(BuildEdgeDragArea(context, DrawerSide.Start, textDirection));
            }

            if (ShouldEnableOpenDragGesture(DrawerSide.End, theme))
            {
                overlayChildren.Add(BuildEdgeDragArea(context, DrawerSide.End, textDirection));
            }
        }

        if (isAnyDrawerVisible)
        {
            overlayChildren.Add(BuildScrim(context, Math.Max(drawerProgress, endDrawerProgress)));
        }

        if (isStartDrawerVisible && CurrentWidget.Drawer != null)
        {
            overlayChildren.Add(BuildDrawerPanel(
                context: context,
                side: DrawerSide.Start,
                textDirection: textDirection,
                progress: drawerProgress,
                child: CurrentWidget.Drawer));
        }

        if (isEndDrawerVisible && CurrentWidget.EndDrawer != null)
        {
            overlayChildren.Add(BuildDrawerPanel(
                context: context,
                side: DrawerSide.End,
                textDirection: textDirection,
                progress: endDrawerProgress,
                child: CurrentWidget.EndDrawer));
        }

        // The overlay stack is unconditional: adding the first sheet/drawer/scrim child must not restructure
        // the body's subtree, which would rebuild its elements (and re-register its heroes) from scratch.
        content = new Stack(
            fit: StackFit.Expand,
            children: overlayChildren);

        return new ScaffoldScope(
            scaffold: this,
            hasDrawer: HasDrawer,
            hasEndDrawer: HasEndDrawer,
            isDrawerOpen: _isDrawerOpen,
            isEndDrawerOpen: _isEndDrawerOpen,
            geometry: new ScaffoldGeometryData(
                FloatingActionButtonArea: floatingActionButtonArea,
                BottomNavigationBarTop: bottomNavigationBarTop),
            child: new ScrollNotificationObserver(
                child: new Container(
                    color: effectiveBackground,
                    child: content)));
    }

    private Widget? BuildPresentedBottomSheet(BuildContext context)
    {
        if (_persistentBottomSheet is { } presentation)
        {
            return new StandardBottomSheet(
                animationController: presentation.Animation,
                builder: presentation.Builder,
                onClosing: ClosePersistentBottomSheet,
                enableDrag: presentation.EnableDrag,
                showDragHandle: presentation.ShowDragHandle,
                isPersistent: false,
                backgroundColor: presentation.BackgroundColor,
                elevation: presentation.Elevation,
                shape: presentation.Shape,
                clipBehavior: presentation.ClipBehavior,
                constraints: presentation.Constraints);
        }

        if (CurrentWidget.BottomSheet is null) return null;
        _staticBottomSheetAnimation ??= CreateCompletedBottomSheetAnimation();
        return new StandardBottomSheet(
            animationController: _staticBottomSheetAnimation,
            builder: _ => new NotificationListener<DraggableScrollableNotification>(
                onNotification: PersistentBottomSheetExtentChanged,
                child: new DraggableScrollableActuator(child: CurrentWidget.BottomSheet)),
            onClosing: () => _staticBottomSheetAnimation.Reverse(),
            isPersistent: true);
    }

    private bool PersistentBottomSheetExtentChanged(DraggableScrollableNotification notification)
    {
        if (notification.Extent - notification.InitialExtent > Constants.PrecisionErrorTolerance)
        {
            if (_persistentSheetHistoryEntry is null && ModalRoute.MaybeOf(Context) is { } route)
            {
                _persistentSheetHistoryEntry = new LocalHistoryEntry(onRemove: () =>
                {
                    _persistentSheetHistoryEntry = null;
                    if (_isDisposed) return;
                    DraggableScrollableActuator.Reset(notification.SourceContext);
                    ShowBodyScrim(false, 0.0);
                    _floatingActionButtonVisibilityController.SetValue(1.0);
                    _persistentSheetHistoryEntry = null;
                });
                route.AddLocalHistoryEntry(_persistentSheetHistoryEntry);
            }
        }
        else
        {
            _persistentSheetHistoryEntry?.Remove();
        }

        return false;
    }

    private void RemovePersistentSheetHistoryEntry()
    {
        if (_persistentSheetHistoryEntry is null) return;
        _persistentSheetHistoryEntry.Remove();
        _persistentSheetHistoryEntry = null;
    }

    private void ClosePersistentBottomSheet() => ClosePersistentBottomSheet(immediate: false);

    private void ClosePersistentBottomSheet(bool immediate)
    {
        var presentation = _persistentBottomSheet;
        if (presentation is null || presentation.Closing) return;
        presentation.Closing = true;
        ShowFloatingActionButton();
        ShowBodyScrim(false, 0.0);
        if (immediate)
        {
            DisposePersistentBottomSheet(complete: true);
            if (!_isDisposed) SetState(() => { });
            return;
        }
        presentation.Animation.Duration = presentation.ExitDuration;
        presentation.Animation.Reverse();
    }

    private void HandlePersistentBottomSheetAnimationChanged()
    {
        if (!_isDisposed) SetState(() => { });
    }

    private void HandlePersistentBottomSheetDismissed()
    {
        DisposePersistentBottomSheet(complete: true);
        if (!_isDisposed) SetState(() => { });
    }

    private void DisposePersistentBottomSheet(bool complete)
    {
        var presentation = _persistentBottomSheet;
        if (presentation is null) return;
        _persistentBottomSheet = null;
        presentation.Animation.Changed -= HandlePersistentBottomSheetAnimationChanged;
        presentation.Animation.Dismissed -= HandlePersistentBottomSheetDismissed;
        presentation.HistoryEntry?.Remove();
        presentation.HistoryEntry = null;
        if (presentation.OwnsAnimation) presentation.Animation.Dispose();
        if (complete) presentation.Closed.TrySetResult(null);
    }

    private void SyncStaticBottomSheetAnimation()
    {
        DisposeStaticBottomSheetAnimation();
        if (CurrentWidget.BottomSheet is null) return;
        _staticBottomSheetAnimation = CreateCompletedBottomSheetAnimation();
        _staticBottomSheetAnimation.Changed += HandleStaticBottomSheetAnimationChanged;
    }

    private AnimationController CreateCompletedBottomSheetAnimation()
    {
        var animation = BottomSheet.CreateAnimationController(vsync: this);
        animation.SetValue(1);
        return animation;
    }

    private void HandleStaticBottomSheetAnimationChanged()
    {
        if (!_isDisposed) SetState(() => { });
    }

    private void DisposeStaticBottomSheetAnimation()
    {
        if (_staticBottomSheetAnimation is null) return;
        _staticBottomSheetAnimation.Changed -= HandleStaticBottomSheetAnimationChanged;
        _staticBottomSheetAnimation.Dispose();
        _staticBottomSheetAnimation = null;
    }

    private Widget BuildEdgeDragArea(BuildContext context, DrawerSide side, TextDirection textDirection)
    {
        double edgeWidth = ResolveEdgeDragWidth(context, side, textDirection);
        bool isOnLeft = IsDrawerOnLeft(side, textDirection);
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
                onHorizontalDragEnd: details => EndDrag(side, details, textDirection),
                onHorizontalDragCancel: () => CancelDragGesture(side)));
    }

    private Widget BuildScrim(BuildContext context, double progress)
    {
        var drawerTheme = DrawerTheme.Of(context);
        var baseColor = CurrentWidget.DrawerScrimColor
                        ?? drawerTheme.ScrimColor
                        ?? Scaffold.ResolveDrawerScrimColor(null);
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

    private Widget BuildDrawerPanel(BuildContext context, DrawerSide side, TextDirection textDirection, double progress, Widget child)
    {
        double drawerWidth = ResolveDrawerWidth(context, child);
        bool isOnLeft = IsDrawerOnLeft(side, textDirection);
        double offset = -(1 - progress) * drawerWidth;
        var alignment = side == DrawerSide.Start
            ? DrawerAlignment.Start
            : DrawerAlignment.End;
        var controller = new DrawerController(
            child: child,
            alignment: alignment,
            isDrawerOpen: progress >= DefaultOpenThreshold,
            drawerCallback: isOpen => CommitDrawerVisibility(side, isOpen),
            scrimColor: CurrentWidget.DrawerScrimColor,
            edgeDragWidth: CurrentWidget.DrawerEdgeDragWidth,
            enableOpenDragGesture: side == DrawerSide.Start
                ? CurrentWidget.DrawerEnableOpenDragGesture
                : CurrentWidget.EndDrawerEnableOpenDragGesture,
            drawerBarrierDismissible: CurrentWidget.DrawerBarrierDismissible);

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
                onHorizontalDragCancel: () => CancelDragGesture(side),
                child: new DrawerControllerScope(
                    controller: controller,
                    child: child)));
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

        double drawerWidth = ResolveDrawerWidth(Context, drawer);
        if (drawerWidth <= 0)
        {
            return;
        }

        double deltaProgress = primaryDelta * ResolveOpenDirectionMultiplier(side, textDirection) / drawerWidth;
        double nextProgress = Math.Clamp(_activeDragProgress + deltaProgress, 0, 1);
        if (Math.Abs(nextProgress - _activeDragProgress) <= 0.0001)
        {
            return;
        }

        SetState(() =>
        {
            _activeDragProgress = nextProgress;
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

        double drawerWidth = ResolveDrawerWidth(Context, drawer);
        if (drawerWidth <= 0)
        {
            return;
        }

        double releaseVelocity = details.PrimaryVelocity * ResolveOpenDirectionMultiplier(side, textDirection);

        bool shouldOpen;
        if (releaseVelocity >= MinFlingVelocityPixelsPerSecond)
        {
            shouldOpen = true;
        }
        else if (releaseVelocity <= -MinFlingVelocityPixelsPerSecond)
        {
            shouldOpen = false;
        }
        else
        {
            shouldOpen = _activeDragProgress >= DefaultOpenThreshold;
        }

        SetState(() =>
        {
            CommitProgress(side, _activeDragProgress);
            CommitDrawerVisibility(side, shouldOpen);
            CancelDrag();
            double normalizedVelocity = Math.Abs(releaseVelocity) / drawerWidth;
            StartSettleAnimation(side, shouldOpen ? 1.0 : 0.0, normalizedVelocity);
            if (shouldOpen)
            {
                StartSettleAnimation(OppositeOf(side), targetProgress: 0.0, normalizedVelocityHint: null);
            }
        });
    }

    private void CancelDragGesture(DrawerSide side)
    {
        if (_activeDragSide != side)
        {
            return;
        }

        bool shouldOpen = _activeDragProgress >= DefaultOpenThreshold;

        SetState(() =>
        {
            CommitProgress(side, _activeDragProgress);
            CommitDrawerVisibility(side, shouldOpen);
            CancelDrag();
            StartSettleAnimation(side, shouldOpen ? 1.0 : 0.0, normalizedVelocityHint: null);
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

    private double ResolveEdgeDragWidth(BuildContext context, DrawerSide side, TextDirection textDirection)
    {
        if (CurrentWidget.DrawerEdgeDragWidth.HasValue)
        {
            return CurrentWidget.DrawerEdgeDragWidth.Value;
        }

        var padding = MediaQuery.MaybePaddingOf(context) ?? default;
        double safePadding = IsDrawerOnLeft(side, textDirection) ? padding.Left : padding.Right;
        return Scaffold.ResolveDrawerEdgeDragWidth(null) + safePadding;
    }

    private void StartSettleAnimation(DrawerSide side, double targetProgress, double? normalizedVelocityHint)
    {
        targetProgress = Math.Clamp(targetProgress, 0, 1);
        if (targetProgress > 0 && !HasDrawerForSide(side))
        {
            return;
        }

        double currentProgress = ResolveDrawerProgress(side);
        currentProgress = Math.Clamp(currentProgress, 0, 1);

        if (Math.Abs(currentProgress - targetProgress) <= 0.0001)
        {
            CommitProgress(side, targetProgress);
            return;
        }

        StopSettleAnimation(side);
        var duration = ResolveSettleDuration(currentProgress, targetProgress, normalizedVelocityHint);
        var controller = new AnimationController(duration, this)
        {
            Curve = Curves.Linear
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
        double distance = Math.Abs(targetProgress - currentProgress);
        if (distance <= 0)
        {
            return TimeSpan.FromMilliseconds(1);
        }

        double durationMs = BaseSettleDuration.TotalMilliseconds * distance;
        double velocity = Math.Abs(normalizedVelocityHint ?? 0);
        if (velocity > double.Epsilon)
        {
            durationMs /= velocity;
        }

        durationMs = Math.Clamp(durationMs, 1.0, BaseSettleDuration.TotalMilliseconds);
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

        double value = _drawerAnimationController.Evaluate();
        double progress = Math.Clamp(_drawerAnimationFrom + (_drawerAnimationTo - _drawerAnimationFrom) * value, 0, 1);
        SetState(() => _drawerProgress = progress);
    }

    private void HandleEndDrawerAnimationTick()
    {
        if (_endDrawerAnimationController == null)
        {
            return;
        }

        double value = _endDrawerAnimationController.Evaluate();
        double progress = Math.Clamp(_endDrawerAnimationFrom + (_endDrawerAnimationTo - _endDrawerAnimationFrom) * value, 0, 1);
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
        bool isOpen = progress >= DefaultOpenThreshold;
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

    private static double ResolveDrawerWidth(BuildContext context, Widget drawer)
    {
        if (drawer is Drawer typedDrawer)
        {
            return typedDrawer.ResolveEffectiveWidthForScaffold(context);
        }

        return DefaultDrawerWidth;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        double effectiveOpacity = Math.Clamp(opacity, 0, 1);
        byte alpha = (byte)Math.Clamp((int)Math.Round(color.A * effectiveOpacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

public sealed class AppBar : StatefulWidget, IPreferredSizeWidget
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
        WidgetStateColor? backgroundColor = null,
        Color? foregroundColor = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        Widget? bottom = null,
        Widget? flexibleSpace = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        ScrollNotificationPredicate? notificationPredicate = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        bool excludeHeaderSemantics = false,
        double toolbarOpacity = 1.0,
        double bottomOpacity = 1.0,
        bool forceMaterialTransparency = false,
        bool useDefaultSemanticsOrder = true,
        Clip? clipBehavior = null,
        bool animateColor = false,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (double.IsNaN(elevation.Value) || elevation.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be non-negative.");
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
        Bottom = bottom;
        FlexibleSpace = flexibleSpace;
        Elevation = elevation;
        ScrolledUnderElevation = scrolledUnderElevation;
        NotificationPredicate = notificationPredicate ?? RawScrollbar.DefaultScrollNotificationPredicate;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Shape = shape;
        ExcludeHeaderSemantics = excludeHeaderSemantics;
        ToolbarOpacity = toolbarOpacity;
        BottomOpacity = bottomOpacity;
        ForceMaterialTransparency = forceMaterialTransparency;
        UseDefaultSemanticsOrder = useDefaultSemanticsOrder;
        ClipBehavior = clipBehavior;
        AnimateColor = animateColor;
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

    public WidgetStateColor? BackgroundColor { get; }

    public Color? ForegroundColor { get; }

    public SystemUiOverlayStyle? SystemOverlayStyle { get; }

    public Widget? Bottom { get; }

    public Widget? FlexibleSpace { get; }

    public double? Elevation { get; }

    public double? ScrolledUnderElevation { get; }

    public ScrollNotificationPredicate NotificationPredicate { get; }

    public Color? ShadowColor { get; }

    public Color? SurfaceTintColor { get; }

    public ShapeBorder? Shape { get; }

    public bool ExcludeHeaderSemantics { get; }

    public double ToolbarOpacity { get; }

    public double BottomOpacity { get; }

    public bool ForceMaterialTransparency { get; }

    public bool UseDefaultSemanticsOrder { get; }

    public Clip? ClipBehavior { get; }

    public bool AnimateColor { get; }

    public Size PreferredSize => new(
        0,
        (ToolbarHeight ?? 56) + (Bottom is IPreferredSizeWidget preferred ? preferred.PreferredSize.Height : 0));

    public static double PreferredHeightFor(BuildContext context, Size preferredSize)
    {
        return preferredSize.Height;
    }

    public override State CreateState()
    {
        return new AppBarState();
    }

    private Widget BuildAppBar(BuildContext context, bool scrolledUnder)
    {
        var theme = Theme.Of(context);
        var appBarTheme = AppBarTheme.Of(context);
        IReadOnlySet<WidgetState> states = scrolledUnder
            ? new HashSet<WidgetState> { WidgetState.ScrolledUnder }
            : new HashSet<WidgetState>();
        WidgetStateColor defaultBackground = new(ResolveDefaultBackgroundColor(theme));
        WidgetStateColor? backgroundSource = BackgroundColor
                                             ?? appBarTheme.BackgroundColorState
                                             ?? (appBarTheme.BackgroundColor.HasValue
                                                 ? new WidgetStateColor(appBarTheme.BackgroundColor.Value)
                                                 : null);
        Color effectiveBackground = backgroundSource?.Resolve(states)
                                    ?? (theme.UseMaterial3 && scrolledUnder
                                        ? theme.ColorScheme.SurfaceContainer
                                        : defaultBackground.Resolve(states));
        Color effectiveForeground = ForegroundColor
                                    ?? appBarTheme.ForegroundColor
                                    ?? ResolveDefaultForegroundColor(theme);
        bool effectiveCenterTitle = ResolveEffectiveCenterTitle(theme, appBarTheme);
        double effectiveTitleSpacing = TitleSpacing
                                       ?? appBarTheme.TitleSpacing
                                       ?? NavigationToolbar.KMiddleSpacing;
        var effectiveIconTheme = ResolveEffectiveIconTheme(theme, appBarTheme, effectiveForeground);
        var effectiveActionsIconTheme = ResolveEffectiveActionsIconTheme(
            theme,
            appBarTheme,
            effectiveForeground,
            effectiveIconTheme);
        double effectiveToolbarOpacity = ResolveToolbarOpacity(ToolbarOpacity);
        effectiveIconTheme = effectiveIconTheme with
        {
            Opacity = (effectiveIconTheme.Opacity ?? 1.0) * effectiveToolbarOpacity,
        };
        effectiveActionsIconTheme = effectiveActionsIconTheme with
        {
            Opacity = (effectiveActionsIconTheme.Opacity ?? 1.0) * effectiveToolbarOpacity,
        };
        var effectiveLeading = ResolveEffectiveLeading(context);
        var effectiveActions = ResolveEffectiveActions(context);
        double effectiveLeadingWidth = ResolveEffectiveLeadingWidth(appBarTheme);
        var effectiveActionsPadding = ActionsPadding ?? appBarTheme.ActionsPadding ?? new Thickness();
        double effectiveToolbarHeight = ResolveEffectiveToolbarHeight(appBarTheme);
        var effectiveToolbarTextStyle = ResolveToolbarTextStyle(theme, appBarTheme, effectiveForeground);
        var effectiveTitleTextStyle = ResolveTitleTextStyle(theme, appBarTheme, effectiveForeground);
        effectiveToolbarTextStyle = ApplyTextOpacity(effectiveToolbarTextStyle, effectiveToolbarOpacity);
        effectiveTitleTextStyle = ApplyTextOpacity(effectiveTitleTextStyle, effectiveToolbarOpacity);
        var effectiveSystemOverlayStyle = ResolveEffectiveSystemOverlayStyle(
            theme,
            appBarTheme,
            effectiveBackground);
        double effectiveElevation = Elevation
                                    ?? appBarTheme.Elevation
                                    ?? (theme.UseMaterial3 ? 0.0 : 4.0);
        double effectiveScrolledUnderElevation = ScrolledUnderElevation
                                                 ?? appBarTheme.ScrolledUnderElevation
                                                 ?? (theme.UseMaterial3 ? 3.0 : effectiveElevation);
        double materialElevation = scrolledUnder ? effectiveScrolledUnderElevation : effectiveElevation;
        Color? effectiveShadowColor = ShadowColor
                                      ?? appBarTheme.ShadowColor
                                      ?? (theme.UseMaterial3 ? Colors.Transparent : Colors.Black);
        Color? effectiveSurfaceTintColor = SurfaceTintColor
                                           ?? appBarTheme.SurfaceTintColor
                                           ?? (theme.UseMaterial3 ? theme.ColorScheme.SurfaceTint : null);
        ShapeBorder? effectiveShape = Shape ?? appBarTheme.Shape;

        Widget? titleWidget = Title ?? (TitleText is null ? null : BuildDefaultTitle());
        if (titleWidget is not null && !ExcludeHeaderSemantics)
        {
            bool namesRoute = PlatformDefaults.TargetPlatform is not (TargetPlatform.IOS or TargetPlatform.MacOS);
            titleWidget = new Semantics(
                flags: SemanticsFlags.IsHeader,
                namesRoute: namesRoute,
                child: titleWidget);
        }
        if (titleWidget is not null)
        {
            titleWidget = new DefaultTextStyle(
                style: effectiveTitleTextStyle,
                child: titleWidget);
            if (MediaQuery.MaybeOf(context) is not null)
            {
                titleWidget = MediaQuery.WithClampedTextScaling(
                    context,
                    titleWidget,
                    maxScaleFactor: 1.34);
            }
        }

        Widget? toolbarLeading = null;
        if (effectiveLeading != null)
        {
            Widget leadingChild = theme.UseMaterial3 && effectiveLeading is IconButton
                ? new Center(child: effectiveLeading)
                : effectiveLeading;
            toolbarLeading = new SizedBox(
                width: effectiveLeadingWidth,
                height: effectiveToolbarHeight,
                child: leadingChild);
        }

        Widget? toolbarTrailing = null;
        if (effectiveActions.Count > 0)
        {
            Widget actions = new Padding(
                insets: effectiveActionsPadding,
                child: new Plumix.Widgets.IconTheme(
                    data: effectiveActionsIconTheme,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: theme.UseMaterial3
                            ? CrossAxisAlignment.Center
                            : CrossAxisAlignment.Stretch,
                        spacing: 0,
                        children: effectiveActions)));
            toolbarTrailing = new IconButtonTheme(
                data: new IconButtonThemeData(
                    IconButton.StyleFrom(iconSize: effectiveActionsIconTheme.Size ?? 24.0)),
                child: actions);
        }

        Widget toolbar = new SizedBox(
            height: effectiveToolbarHeight,
            child: new DefaultTextStyle(
                style: effectiveToolbarTextStyle,
                child: new Plumix.Widgets.IconTheme(
                    data: effectiveIconTheme,
                    child: new NavigationToolbar(
                        leading: toolbarLeading,
                        middle: titleWidget,
                        trailing: toolbarTrailing,
                        centerMiddle: effectiveCenterTitle,
                        middleSpacing: effectiveTitleSpacing))));
        toolbar = new ClipRect(
            clipBehavior: ClipBehavior ?? Clip.HardEdge,
            child: toolbar);

        Widget toolbarAndBottom = toolbar;

        if (Bottom is not null)
        {
            Widget effectiveBottom = BottomOpacity == 1.0
                ? Bottom
                : new Opacity(
                    opacity: ResolveToolbarOpacity(BottomOpacity),
                    child: Bottom);
            toolbarAndBottom = new Column(
                mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [new Flexible(child: toolbar), effectiveBottom]);
        }

        Widget appBarContent = new Padding(Padding ?? new Thickness(), toolbarAndBottom);

        if (Primary && MediaQuery.MaybeOf(context) != null)
        {
            appBarContent = new SafeArea(bottom: false, child: appBarContent);
        }

        appBarContent = new Align(alignment: Alignment.TopCenter, child: appBarContent);

        if (FlexibleSpace is not null)
        {
            appBarContent = new Stack(
                fit: StackFit.Passthrough,
                children:
                [
                    new Semantics(
                        sortKey: UseDefaultSemanticsOrder ? new OrdinalSortKey(1.0) : null,
                        explicitChildNodes: true,
                        child: FlexibleSpace),
                    new Semantics(
                        sortKey: UseDefaultSemanticsOrder ? new OrdinalSortKey(0.0) : null,
                        explicitChildNodes: true,
                        child: new Material(
                            type: MaterialType.Transparency,
                            child: appBarContent)),
                ]);
        }

        SystemChrome.SetSystemUiOverlayStyle(effectiveSystemOverlayStyle);

        Widget material = new Material(
            type: ForceMaterialTransparency ? MaterialType.Transparency : MaterialType.Canvas,
            color: effectiveBackground,
            elevation: materialElevation,
            shadowColor: effectiveShadowColor,
            surfaceTintColor: effectiveSurfaceTintColor,
            shape: effectiveShape,
            animateColor: AnimateColor,
            child: new Semantics(
                explicitChildNodes: true,
                child: appBarContent));
        return new Semantics(
            container: true,
            child: new AnnotatedRegion<SystemUiOverlayStyle>(
                value: effectiveSystemOverlayStyle,
                child: material));
    }

    private static double ResolveToolbarOpacity(double opacity)
    {
        double intervalValue = Math.Clamp((opacity - 0.25) / 0.75, 0.0, 1.0);
        return Curves.FastOutSlowIn(intervalValue);
    }

    private static TextStyle ApplyTextOpacity(TextStyle style, double opacity)
    {
        if (!style.Color.HasValue || opacity >= 1.0)
        {
            return style;
        }

        return style with
        {
            Color = ApplyOpacity(style.Color.Value, opacity),
        };
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(color.A * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private sealed class AppBarState : State
    {
        private ScrollNotificationObserverState? _scrollNotificationObserver;
        private bool _scrolledUnder;

        private AppBar CurrentWidget => (AppBar)StateWidget;

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();

            var scaffold = Scaffold.MaybeOf(Context);
            if (scaffold?.IsDrawerOpen == true || scaffold?.IsEndDrawerOpen == true)
            {
                return;
            }

            ScrollNotificationObserverState? observer = ScrollNotificationObserver.MaybeOf(Context);
            if (ReferenceEquals(observer, _scrollNotificationObserver))
            {
                return;
            }

            _scrollNotificationObserver?.RemoveListener(HandleScrollNotification);
            observer?.AddListener(HandleScrollNotification);
            _scrollNotificationObserver = observer;
        }

        public override Widget Build(BuildContext context)
        {
            return CurrentWidget.BuildAppBar(context, _scrolledUnder);
        }

        public override void Dispose()
        {
            _scrollNotificationObserver?.RemoveListener(HandleScrollNotification);
            _scrollNotificationObserver = null;
            base.Dispose();
        }

        private void HandleScrollNotification(ScrollNotification notification)
        {
            if (notification is not ScrollUpdateNotification
                || !CurrentWidget.NotificationPredicate(notification))
            {
                return;
            }

            bool? next = notification.Metrics.AxisDirection switch
            {
                AxisDirection.Down => notification.Metrics.ExtentBefore > 0.0,
                AxisDirection.Up => notification.Metrics.ExtentAfter > 0.0,
                _ => null,
            };
            if (!next.HasValue || next.Value == _scrolledUnder)
            {
                return;
            }

            SetState(() => _scrolledUnder = next.Value);
        }
    }

    private bool ResolveEffectiveCenterTitle(ThemeData theme, AppBarThemeData appBarTheme)
    {
        if (CenterTitle.HasValue)
        {
            return CenterTitle.Value;
        }

        if (appBarTheme.CenterTitle.HasValue)
        {
            return appBarTheme.CenterTitle.Value;
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
        bool impliesAppBarDismissal = route?.ImpliesAppBarDismissal ?? Navigator.CanPop(context);
        if (!impliesAppBarDismissal)
        {
            return null;
        }

        bool useCloseButton = route is PageRoute pageRoute && pageRoute.FullscreenDialog;
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
        return new DrawerButton();
    }

    private static Widget BuildDefaultEndDrawerAction(BuildContext context)
    {
        return new EndDrawerButton();
    }

    private static Widget BuildDefaultLeading(BuildContext context, bool useCloseButton)
    {
        return useCloseButton
            ? new CloseButton()
            : new BackButton();
    }

    private double ResolveEffectiveLeadingWidth(AppBarThemeData appBarTheme)
    {
        return LeadingWidth ?? appBarTheme.LeadingWidth ?? 56;
    }

    private IconThemeData ResolveEffectiveIconTheme(
        ThemeData theme,
        AppBarThemeData appBarTheme,
        Color effectiveForeground)
    {
        var baseTheme = IconTheme
                        ?? appBarTheme.IconTheme
                        ?? ResolveDefaultIconTheme(theme, effectiveForeground);
        return baseTheme with
        {
            Color = baseTheme.Color ?? effectiveForeground,
        };
    }

    private IconThemeData ResolveEffectiveActionsIconTheme(
        ThemeData theme,
        AppBarThemeData appBarTheme,
        Color effectiveForeground,
        IconThemeData effectiveIconTheme)
    {
        var actionForeground = ForegroundColor ?? appBarTheme.ForegroundColor;
        var baseTheme = ActionsIconTheme
                        ?? appBarTheme.ActionsIconTheme
                        ?? IconTheme
                        ?? appBarTheme.IconTheme
                        ?? ResolveDefaultActionsIconTheme(theme, actionForeground, effectiveIconTheme);

        return baseTheme with
        {
            Color = baseTheme.Color ?? actionForeground ?? effectiveForeground,
        };
    }

    private double ResolveEffectiveToolbarHeight(AppBarThemeData appBarTheme)
    {
        return ToolbarHeight
               ?? appBarTheme.ToolbarHeight
               ?? ResolveDefaultToolbarHeight();
    }

    private static double ResolveDefaultToolbarHeight()
    {
        return 56;
    }

    private static Color ResolveDefaultBackgroundColor(ThemeData theme)
    {
        if (theme.UseMaterial3)
        {
            return theme.ColorScheme.Surface;
        }

        return theme.ColorScheme.Brightness == Brightness.Dark
            ? theme.ColorScheme.Surface
            : theme.ColorScheme.Primary;
    }

    private static Color ResolveDefaultForegroundColor(ThemeData theme)
    {
        if (theme.UseMaterial3)
        {
            return theme.ColorScheme.OnSurface;
        }

        return theme.ColorScheme.Brightness == Brightness.Dark
            ? theme.ColorScheme.OnSurface
            : theme.ColorScheme.OnPrimary;
    }

    private static IconThemeData ResolveDefaultIconTheme(ThemeData theme, Color effectiveForeground)
    {
        return theme.UseMaterial3
            ? new IconThemeData(Color: effectiveForeground, Size: 24)
            : theme.IconTheme with { Color = effectiveForeground };
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
            Color: actionForeground ?? theme.ColorScheme.OnSurfaceVariant,
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

    private TextStyle ResolveToolbarTextStyle(
        ThemeData theme,
        AppBarThemeData appBarTheme,
        Color effectiveForeground)
    {
        var baseStyle = theme.TextTheme.BodyMedium with
        {
            Color = effectiveForeground,
        };

        var overrideStyle = ToolbarTextStyle ?? appBarTheme.ToolbarTextStyle;
        return ComposeTextStyle(baseStyle, overrideStyle);
    }

    private TextStyle ResolveTitleTextStyle(
        ThemeData theme,
        AppBarThemeData appBarTheme,
        Color effectiveForeground)
    {
        var baseStyle = theme.TextTheme.TitleLarge with
        {
            Color = effectiveForeground,
        };

        var overrideStyle = TitleTextStyle ?? appBarTheme.TitleTextStyle;
        return ComposeTextStyle(baseStyle, overrideStyle);
    }

    private SystemUiOverlayStyle ResolveEffectiveSystemOverlayStyle(
        ThemeData theme,
        AppBarThemeData appBarTheme,
        Color effectiveBackground)
    {
        return SystemOverlayStyle
               ?? appBarTheme.SystemOverlayStyle
               ?? ResolveDefaultSystemOverlayStyle(theme, effectiveBackground);
    }

    private static SystemUiOverlayStyle ResolveDefaultSystemOverlayStyle(ThemeData theme, Color effectiveBackground)
    {
        var iconBrightness = EstimateIconBrightnessForColor(effectiveBackground);
        return new SystemUiOverlayStyle(
            StatusBarColor: theme.UseMaterial3 ? Colors.Transparent : null,
            StatusBarIconBrightness: iconBrightness);
    }

    private static SystemUiIconBrightness EstimateIconBrightnessForColor(Color color)
    {
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
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
        return new Text(
            TitleText!,
            softWrap: false,
            maxLines: 1,
            overflow: TextOverflow.Ellipsis);
    }
}
