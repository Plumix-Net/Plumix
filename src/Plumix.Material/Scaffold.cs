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
// flutter/packages/flutter/lib/src/material/app_bar.dart; flutter/packages/flutter/lib/src/material/drawer.dart

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
        Key? key = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        string? semanticLabel = null,
        Clip? clipBehavior = null) : base(key)
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
        SurfaceTintColor = surfaceTintColor;
        Width = width;
        Shape = shape;
        SemanticLabel = semanticLabel;
        ClipBehavior = clipBehavior;
    }

    public Widget? Child { get; }

    public Color? BackgroundColor { get; }

    public double? Elevation { get; }

    public Color? ShadowColor { get; }

    public Color? SurfaceTintColor { get; }

    public double? Width { get; }

    public ShapeBorder? Shape { get; }

    public string? SemanticLabel { get; }

    public Clip? ClipBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var drawerTheme = DrawerTheme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        bool isDrawerStart = DrawerController.MaybeOf(context)?.Alignment != DrawerAlignment.End;
        var effectiveBackground = BackgroundColor ?? drawerTheme.BackgroundColor ?? (useMaterial3
            ? theme.ColorScheme.SurfaceContainerLow
            : theme.CanvasColor);
        double effectiveElevation = ResolveEffectiveElevation(drawerTheme, useMaterial3);
        double effectiveWidth = ResolveEffectiveWidth(drawerTheme);
        var effectiveShadowColor = ShadowColor ?? drawerTheme.ShadowColor ?? (useMaterial3
            ? Colors.Transparent
            : theme.ShadowColor);
        var effectiveSurfaceTintColor = SurfaceTintColor ?? drawerTheme.SurfaceTintColor
            ?? (useMaterial3 ? Colors.Transparent : null);
        ShapeBorder? effectiveShape = Shape
                                      ?? (isDrawerStart ? drawerTheme.Shape : drawerTheme.EndShape)
                                      ?? ResolveDefaultShape(useMaterial3);
        Clip effectiveClip = effectiveShape is null
            ? Clip.None
            : ClipBehavior ?? drawerTheme.ClipBehavior ?? Clip.HardEdge;
        string? label = theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? SemanticLabel
            : SemanticLabel ?? MaterialLocalizations.Of(context).DrawerLabel;

        return new Semantics(
            scopesRoute: true,
            namesRoute: true,
            explicitChildNodes: true,
            label: label,
            child: new ConstrainedBox(
                constraints: BoxConstraints.Expand(width: effectiveWidth),
                child: new Material(
                    color: effectiveBackground,
                    elevation: effectiveElevation,
                    shadowColor: effectiveShadowColor,
                    surfaceTintColor: effectiveSurfaceTintColor,
                    shape: effectiveShape,
                    clipBehavior: effectiveClip,
                    child: Child ?? new SizedBox())));
    }

    internal double ResolveEffectiveWidthForScaffold(BuildContext context)
    {
        return ResolveEffectiveWidth(DrawerTheme.Of(context));
    }

    private double ResolveEffectiveElevation(DrawerThemeData drawerTheme, bool useMaterial3)
    {
        double effectiveElevation = Elevation ?? drawerTheme.Elevation ?? (useMaterial3
            ? DefaultM3Elevation
            : DefaultM2Elevation);
        if (double.IsNaN(effectiveElevation) || double.IsInfinity(effectiveElevation) || effectiveElevation < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DrawerThemeData.Elevation),
                "Drawer theme elevation must be non-negative and finite.");
        }

        return effectiveElevation;
    }

    private double ResolveEffectiveWidth(DrawerThemeData drawerTheme)
    {
        double effectiveWidth = Width ?? drawerTheme.Width ?? DefaultWidth;
        if (double.IsNaN(effectiveWidth) || double.IsInfinity(effectiveWidth) || effectiveWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DrawerThemeData.Width),
                "Drawer theme width must be positive and finite.");
        }

        return effectiveWidth;
    }

    private static ShapeBorder? ResolveDefaultShape(bool useMaterial3)
    {
        if (!useMaterial3)
        {
            return null;
        }

        return ShapeBorder.RoundedRectangle(16);
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
        FloatingActionButtonLocation? floatingActionButtonLocation = null,
        FloatingActionButtonAnimator? floatingActionButtonAnimator = null,
        Widget? bottomNavigationBar = null,
        Color? backgroundColor = null,
        Key? key = null,
        Widget? bottomSheet = null) : base(key)
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
}

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
        SyncStaticBottomSheetAnimation();
    }

    public override void Dispose()
    {
        _isDisposed = true;
        _scaffoldMessenger?.Unregister(this);
        _scaffoldMessenger = null;
        RemoveDrawerHistoryEntry();
        StopSettleAnimation(DrawerSide.Start);
        StopSettleAnimation(DrawerSide.End);
        DisposeStaticBottomSheetAnimation();
        DisposePersistentBottomSheet(complete: true);
    }

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
        var animation = transitionAnimationController ?? BottomSheet.CreateAnimationController(sheetAnimationStyle);
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
                            CurrentWidget.FloatingActionButton)),
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
                child: presentedSnackBar));
        }

        var bottomSheet = BuildPresentedBottomSheet(context);
        if (bottomSheet is not null)
        {
            overlayChildren.Add(new Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                child: bottomSheet));
        }

        if (presentedMaterialBanner is not null && materialBannerElevation != 0.0)
        {
            double appBarHeight = CurrentWidget.AppBar?.PreferredSize.Height ?? 0.0;
            overlayChildren.Add(new Positioned(
                left: 0,
                top: appBarHeight,
                right: 0,
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
            geometry: new ScaffoldGeometryData(
                FloatingActionButtonArea: floatingActionButtonArea,
                BottomNavigationBarTop: bottomNavigationBarTop),
            child: new Container(
                color: effectiveBackground,
                child: content));
    }

    private Widget? BuildPresentedBottomSheet(BuildContext context)
    {
        if (_persistentBottomSheet is { } presentation)
        {
            return new FractionalTranslation(
                new Vector(0, 1 - presentation.Animation.Evaluate()),
                transformHitTests: true,
                child: new BottomSheet(
                    animationController: presentation.Animation,
                    onClosing: ClosePersistentBottomSheet,
                    builder: presentation.Builder,
                    enableDrag: presentation.EnableDrag,
                    showDragHandle: presentation.ShowDragHandle,
                    backgroundColor: presentation.BackgroundColor,
                    elevation: presentation.Elevation,
                    shape: presentation.Shape,
                    clipBehavior: presentation.ClipBehavior,
                    constraints: presentation.Constraints));
        }

        if (CurrentWidget.BottomSheet is null) return null;
        _staticBottomSheetAnimation ??= CreateCompletedBottomSheetAnimation();
        return new FractionalTranslation(
            new Vector(0, 1 - _staticBottomSheetAnimation.Evaluate()),
            transformHitTests: true,
            child: new BottomSheet(
                animationController: _staticBottomSheetAnimation,
                onClosing: () => _staticBottomSheetAnimation.Reverse(),
                builder: _ => CurrentWidget.BottomSheet,
                enableDrag: true));
    }

    private void ClosePersistentBottomSheet() => ClosePersistentBottomSheet(immediate: false);

    private void ClosePersistentBottomSheet(bool immediate)
    {
        var presentation = _persistentBottomSheet;
        if (presentation is null || presentation.Closing) return;
        presentation.Closing = true;
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

    private static AnimationController CreateCompletedBottomSheetAnimation()
    {
        var animation = BottomSheet.CreateAnimationController();
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

public sealed class AppBar : StatelessWidget, IPreferredSizeWidget
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
        Widget? bottom = null,
        Widget? flexibleSpace = null,
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
        Bottom = bottom;
        FlexibleSpace = flexibleSpace;
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

    public Widget? Bottom { get; }

    public Widget? FlexibleSpace { get; }

    public Size PreferredSize => new(
        0,
        (ToolbarHeight ?? 56) + (Bottom is IPreferredSizeWidget preferred ? preferred.PreferredSize.Height : 0));

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var appBarTheme = AppBarTheme.Of(context);
        var effectiveBackground = BackgroundColor
                                  ?? appBarTheme.BackgroundColor
                                  ?? ResolveDefaultBackgroundColor(theme);
        var effectiveForeground = ForegroundColor
                                  ?? appBarTheme.ForegroundColor
                                  ?? ResolveDefaultForegroundColor(theme);
        bool effectiveCenterTitle = ResolveEffectiveCenterTitle(theme, appBarTheme);
        double effectiveTitleSpacing = TitleSpacing ?? appBarTheme.TitleSpacing ?? 16;
        var effectiveIconTheme = ResolveEffectiveIconTheme(theme, appBarTheme, effectiveForeground);
        var effectiveActionsIconTheme = ResolveEffectiveActionsIconTheme(
            theme,
            appBarTheme,
            effectiveForeground,
            effectiveIconTheme);
        var effectiveLeading = ResolveEffectiveLeading(context);
        var effectiveActions = ResolveEffectiveActions(context);
        double effectiveLeadingWidth = ResolveEffectiveLeadingWidth(appBarTheme);
        var effectiveActionsPadding = ActionsPadding ?? appBarTheme.ActionsPadding ?? new Thickness();
        double effectiveToolbarHeight = ResolveEffectiveToolbarHeight(appBarTheme);
        var effectiveToolbarTextStyle = ResolveToolbarTextStyle(theme, appBarTheme, effectiveForeground);
        var effectiveTitleTextStyle = ResolveTitleTextStyle(theme, appBarTheme, effectiveForeground);
        var effectiveSystemOverlayStyle = ResolveEffectiveSystemOverlayStyle(
            theme,
            appBarTheme,
            effectiveBackground);

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
                        child: new Plumix.Widgets.IconTheme(
                            data: effectiveIconTheme,
                            child: effectiveLeading))));
        }

        rowChildren.Add(new Expanded(child: middle));

        if (effectiveActions.Count > 0)
        {
            rowChildren.Add(new Padding(
                insets: effectiveActionsPadding,
                child: new Plumix.Widgets.IconTheme(
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

        if (Bottom is not null)
        {
            appBarContent = new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [appBarContent, Bottom]);
        }

        if (FlexibleSpace is not null)
        {
            appBarContent = new Stack(
                fit: StackFit.Passthrough,
                children:
                [
                    new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: FlexibleSpace),
                    appBarContent,
                ]);
        }

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
        double effectiveLeadingWidth = LeadingWidth ?? appBarTheme.LeadingWidth ?? 56;
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
        double effectiveToolbarHeight = ToolbarHeight
                                        ?? appBarTheme.ToolbarHeight
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
        var systemBarColor = effectiveBackground;
        return new SystemUiOverlayStyle(
            StatusBarColor: systemBarColor,
            NavigationBarColor: systemBarColor,
            StatusBarIconBrightness: iconBrightness,
            NavigationBarIconBrightness: iconBrightness);
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
