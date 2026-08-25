using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): material_ui/lib/src/scaffold.dart

public sealed class Scaffold : StatefulWidget
{
    public Scaffold(
        Widget? body = null,
        IPreferredSizeWidget? appBar = null,
        Widget? drawer = null,
        Widget? endDrawer = null,
        Action<bool>? onDrawerChanged = null,
        Action<bool>? onEndDrawerChanged = null,
        DragStartBehavior drawerDragStartBehavior = DragStartBehavior.Start,
        bool drawerBarrierDismissible = true,
        Color? drawerScrimColor = null,
        double? drawerEdgeDragWidth = null,
        bool drawerEnableOpenDragGesture = true,
        bool endDrawerEnableOpenDragGesture = true,
        Widget? floatingActionButton = null,
        FloatingActionButtonLocation? floatingActionButtonLocation = null,
        FloatingActionButtonAnimator? floatingActionButtonAnimator = null,
        IReadOnlyList<Widget>? persistentFooterButtons = null,
        AlignmentDirectional? persistentFooterAlignment = null,
        BoxDecoration? persistentFooterDecoration = null,
        Widget? bottomNavigationBar = null,
        Color? backgroundColor = null,
        Key? key = null,
        Widget? bottomSheet = null,
        BottomSheetScrimBuilder? bottomSheetScrimBuilder = null,
        bool extendBody = false,
        bool extendBodyBehindAppBar = false,
        bool primary = true,
        bool? resizeToAvoidBottomInset = null) : base(key)
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
        OnDrawerChanged = onDrawerChanged;
        OnEndDrawerChanged = onEndDrawerChanged;
        DrawerDragStartBehavior = drawerDragStartBehavior;
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
        PersistentFooterButtons = persistentFooterButtons;
        PersistentFooterAlignment = persistentFooterAlignment ?? AlignmentDirectional.CenterEnd;
        PersistentFooterDecoration = persistentFooterDecoration;
        BottomNavigationBar = bottomNavigationBar;
        BottomSheet = bottomSheet;
        BottomSheetScrimBuilder = bottomSheetScrimBuilder ?? DefaultBottomSheetScrimBuilder;
        BackgroundColor = backgroundColor;
        ExtendBody = extendBody;
        ExtendBodyBehindAppBar = extendBodyBehindAppBar;
        Primary = primary;
        ResizeToAvoidBottomInset = resizeToAvoidBottomInset;
    }

    public Widget? Body { get; }

    public IPreferredSizeWidget? AppBar { get; }

    public Widget? Drawer { get; }

    public Widget? EndDrawer { get; }

    /// <summary>Called when <see cref="Drawer"/> changes between opened and closed.</summary>
    public Action<bool>? OnDrawerChanged { get; }

    /// <summary>Called when <see cref="EndDrawer"/> changes between opened and closed.</summary>
    public Action<bool>? OnEndDrawerChanged { get; }

    /// <summary>How the drawers' drag gestures determine their start offset.</summary>
    public DragStartBehavior DrawerDragStartBehavior { get; }

    public bool DrawerBarrierDismissible { get; }

    public Color? DrawerScrimColor { get; }

    public double? DrawerEdgeDragWidth { get; }

    public bool DrawerEnableOpenDragGesture { get; }

    public bool EndDrawerEnableOpenDragGesture { get; }

    public Widget? FloatingActionButton { get; }

    public FloatingActionButtonLocation FloatingActionButtonLocation { get; }

    public FloatingActionButtonAnimator FloatingActionButtonAnimator { get; }

    /// <summary>
    /// A set of buttons displayed at the bottom of the scaffold, above <see cref="BottomNavigationBar"/> and
    /// below <see cref="Body"/>. They stay visible while the body scrolls.
    /// </summary>
    public IReadOnlyList<Widget>? PersistentFooterButtons { get; }

    /// <summary>
    /// How <see cref="PersistentFooterButtons"/> are aligned in their row. Defaults to
    /// <see cref="AlignmentDirectional.CenterEnd"/>.
    /// </summary>
    public AlignmentDirectional PersistentFooterAlignment { get; }

    /// <summary>
    /// The decoration painted behind <see cref="PersistentFooterButtons"/>. Defaults to a single top divider
    /// border resolved from the ambient <see cref="DividerTheme"/>.
    /// </summary>
    public BoxDecoration? PersistentFooterDecoration { get; }

    public Widget? BottomNavigationBar { get; }

    public Widget? BottomSheet { get; }

    /// <summary>
    /// Builds the scrim shown over the body while a draggable bottom sheet dominates the screen. The animation
    /// runs from 0.0 (the sheet covers 70% of the screen) to 1.0 (the sheet covers the screen); returning
    /// <see langword="null"/> suppresses the scrim.
    /// </summary>
    public BottomSheetScrimBuilder BottomSheetScrimBuilder { get; }

    public Color? BackgroundColor { get; }

    /// <summary>
    /// Whether <see cref="Body"/> should extend behind <see cref="BottomNavigationBar"/>.
    /// </summary>
    public bool ExtendBody { get; }

    /// <summary>Whether <see cref="Body"/> should extend behind <see cref="AppBar"/>.</summary>
    public bool ExtendBodyBehindAppBar { get; }

    /// <summary>
    /// Whether this scaffold is being displayed at the top of the screen. On iOS and macOS this installs the
    /// status-bar tap target that scrolls the primary scrollable back to the top.
    /// </summary>
    public bool Primary { get; }

    /// <summary>
    /// Whether the scaffold's layout should keep its body above the on-screen keyboard. Defaults to
    /// <see langword="true"/>.
    /// </summary>
    public bool? ResizeToAvoidBottomInset { get; }

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

    /// <summary>
    /// Returns a <see cref="IValueListenable{T}"/> for the <see cref="ScaffoldGeometry"/> of the closest
    /// <see cref="Scaffold"/> ancestor. The listenable may only be read during the paint phase.
    /// </summary>
    public static IValueListenable<ScaffoldGeometry> GeometryOf(BuildContext context)
    {
        ScaffoldScope? scope = context.DependOnInherited<ScaffoldScope>();
        if (scope is null)
        {
            throw new InvalidOperationException(
                "Scaffold.geometryOf() called with a context that does not contain a Scaffold.\n"
                + "This usually happens when the context provided is from the same StatefulWidget as that "
                + "whose build function actually creates the Scaffold widget being sought.");
        }

        return scope.GeometryNotifier;
    }

    /// <summary>
    /// Whether the closest <see cref="Scaffold"/> ancestor has a <see cref="Drawer"/>. When
    /// <paramref name="registerForUpdates"/> is <see langword="true"/> the caller rebuilds whenever the
    /// answer changes.
    /// </summary>
    public static bool HasDrawerOf(BuildContext context, bool registerForUpdates = true)
    {
        if (registerForUpdates)
        {
            return context.DependOnInherited<ScaffoldScope>()?.HasDrawer ?? false;
        }

        return context.FindAncestorStateOfType<ScaffoldState>()?.HasDrawer ?? false;
    }

    /// <summary>
    /// Ports the height computation `_ScaffoldState.build` performs for the app-bar slot: the widget's
    /// own preferred height, with `AppBarThemeData.ToolbarHeight` substituted when the app bar left its
    /// toolbar height unset.
    /// </summary>
    internal static double PreferredAppBarHeight(BuildContext context, IPreferredSizeWidget appBar)
    {
        return appBar is Plumix.Material.AppBar bar
            ? Plumix.Material.AppBar.PreferredHeightFor(context, bar.PreferredAppBarSize)
            : Plumix.Material.AppBar.PreferredHeightFor(context, appBar.PreferredSize);
    }

    internal static ScaffoldGeometryNotifier? GeometryNotifierMaybeOf(BuildContext context)
    {
        return context.DependOnInherited<ScaffoldScope>()?.GeometryNotifier;
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

/// <summary>
/// Ports Flutter's private <c>_DismissDrawerAction</c>: closes an open drawer when the dismiss intent
/// (Escape) reaches the scaffold.
/// </summary>
internal sealed class DismissDrawerAction : DismissAction
{
    private readonly BuildContext _context;

    public DismissDrawerAction(BuildContext context)
    {
        _context = context;
    }

    public override bool IsEnabled(DismissIntent intent)
    {
        ScaffoldState scaffold = Scaffold.Of(_context);
        return (scaffold.IsDrawerOpen || scaffold.IsEndDrawerOpen) && scaffold.IsDrawerBarrierDismissible;
    }

    public override object? Invoke(DismissIntent intent)
    {
        Scaffold.Of(_context).CloseDrawer();
        Scaffold.Of(_context).CloseEndDrawer();
        return null;
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
        ScaffoldGeometryNotifier geometryNotifier,
        Widget child,
        Key? key = null) : base(key)
    {
        Scaffold = scaffold ?? throw new ArgumentNullException(nameof(scaffold));
        HasDrawer = hasDrawer;
        HasEndDrawer = hasEndDrawer;
        IsDrawerOpen = isDrawerOpen;
        IsEndDrawerOpen = isEndDrawerOpen;
        GeometryNotifier = geometryNotifier ?? throw new ArgumentNullException(nameof(geometryNotifier));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScaffoldState Scaffold { get; }

    public bool HasDrawer { get; }

    public bool HasEndDrawer { get; }

    public bool IsDrawerOpen { get; }

    public bool IsEndDrawerOpen { get; }

    public ScaffoldGeometryNotifier GeometryNotifier { get; }

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
               || !ReferenceEquals(GeometryNotifier, oldScope.GeometryNotifier);
    }
}

public sealed class ScaffoldState : State, WidgetsBindingObserver
{
    private static readonly TimeSpan StatusBarTapScrollDuration = TimeSpan.FromMilliseconds(1000);
    private readonly LabeledGlobalKey<DrawerControllerState> _drawerKey = new("Scaffold drawer");
    private readonly LabeledGlobalKey<DrawerControllerState> _endDrawerKey = new("Scaffold end drawer");
    private readonly LabeledGlobalKey<State> _statusBarKey = new("Scaffold status bar");
    private readonly LabeledGlobalKey<State> _bodyKey = new("Scaffold body");
    private bool _drawerOpened;
    private bool _endDrawerOpened;
    private bool _isDisposed;
    private PersistentBottomSheetPresentation? _persistentBottomSheet;
    private AnimationController? _staticBottomSheetAnimation;
    private ScaffoldMessengerState? _scaffoldMessenger;
    private AnimationController _bottomSheetScrimAnimationController = null!;
    private AnimationController _floatingActionButtonVisibilityController = null!;
    private AnimationController _floatingActionButtonMoveController = null!;
    private FloatingActionButtonAnimator _floatingActionButtonAnimator = null!;
    private FloatingActionButtonLocation? _previousFloatingActionButtonLocation;
    private FloatingActionButtonLocation? _floatingActionButtonLocation;
    private ScaffoldGeometryNotifier _geometryNotifier = null!;
    private bool _showBodyScrim;
    private double? _appBarMaxHeight;
    private LocalHistoryEntry? _persistentSheetHistoryEntry;

    private Scaffold CurrentWidget => (Scaffold)StateWidget;

    /// <summary>Whether this scaffold has a non-null <see cref="Scaffold.AppBar"/>.</summary>
    public bool HasAppBar => CurrentWidget.AppBar != null;

    /// <summary>
    /// The height of the <see cref="Scaffold.AppBar"/> slot, including the status-bar padding a primary
    /// scaffold adds. Null until the first build, and whenever there is no app bar.
    /// </summary>
    public double? AppBarMaxHeight => _appBarMaxHeight;

    public bool HasDrawer => CurrentWidget.Drawer != null;

    public bool HasEndDrawer => CurrentWidget.EndDrawer != null;

    public bool HasFloatingActionButton => CurrentWidget.FloatingActionButton != null;

    public bool IsDrawerOpen => _drawerOpened;

    public bool IsEndDrawerOpen => _endDrawerOpened;

    /// <summary>Whether tapping the drawer scrim closes the open drawer.</summary>
    public bool IsDrawerBarrierDismissible => CurrentWidget.DrawerBarrierDismissible;

    public override void InitState()
    {
        _isDisposed = false;
        _geometryNotifier = new ScaffoldGeometryNotifier(new ScaffoldGeometry(), Context);
        _bottomSheetScrimAnimationController = new AnimationController(duration: TimeSpan.Zero, vsync: this);
        _floatingActionButtonLocation = CurrentWidget.FloatingActionButtonLocation;
        _floatingActionButtonAnimator = CurrentWidget.FloatingActionButtonAnimator;
        _previousFloatingActionButtonLocation = _floatingActionButtonLocation;
        _floatingActionButtonMoveController = new AnimationController(
            duration: FloatingActionButtonConstants.Segue * 2,
            vsync: this);
        _floatingActionButtonMoveController.SetValue(1.0);
        _floatingActionButtonVisibilityController =
            new AnimationController(duration: FloatingActionButtonConstants.Segue, vsync: this);
        SyncStaticBottomSheetAnimation();
        if (CurrentWidget.Primary)
        {
            WidgetsBinding.Instance.AddObserver(this);
        }
    }

    public override void Activate()
    {
        base.Activate();

        // A scaffold moved through a global key is deactivated and reactivated without being disposed, so
        // the status-bar observer removed by Deactivate has to be registered again here.
        if (CurrentWidget.Primary)
        {
            WidgetsBinding.Instance.AddObserver(this);
        }
    }

    public override void Deactivate()
    {
        WidgetsBinding.Instance.RemoveObserver(this);
        base.Deactivate();
    }

    public override void Dispose()
    {
        _isDisposed = true;
        WidgetsBinding.Instance.RemoveObserver(this);
        _scaffoldMessenger?.Unregister(this);
        _scaffoldMessenger = null;
        RemovePersistentSheetHistoryEntry();
        DisposeStaticBottomSheetAnimation();
        DisposePersistentBottomSheet(complete: true);
        _geometryNotifier.Dispose();
        _floatingActionButtonMoveController.Dispose();
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

    /// <summary>The progress of the floating action button's location change animation.</summary>
    internal double FloatingActionButtonMoveProgressForTests => _floatingActionButtonMoveController.Value;

    /// <summary>The controller a dominating bottom sheet drives to shrink the floating action button.</summary>
    internal AnimationController FloatingActionButtonVisibilityController =>
        _floatingActionButtonVisibilityController;

    /// <summary>Whether the scaffold hosts a <see cref="Scaffold.BottomSheet"/> rather than a shown sheet.</summary>
    internal bool HasStaticBottomSheet => CurrentWidget.BottomSheet is not null;

    /// <summary>
    /// Moves the floating action button to a new <see cref="FloatingActionButtonLocation"/>. Ports Flutter's
    /// private <c>_moveFloatingActionButton</c>, including the interrupt-restart behavior.
    /// </summary>
    private void MoveFloatingActionButton(FloatingActionButtonLocation newLocation)
    {
        FloatingActionButtonLocation? previousLocation = _floatingActionButtonLocation;
        double restartAnimationFrom = 0.0;

        // If the animation is currently running, then we want to start from the current relative value so
        // the animation does not jump.
        if (_floatingActionButtonMoveController.IsAnimating)
        {
            previousLocation = new TransitionSnapshotFabLocation(
                _previousFloatingActionButtonLocation!,
                _floatingActionButtonLocation!,
                _floatingActionButtonAnimator,
                _floatingActionButtonMoveController.Value);
            restartAnimationFrom =
                _floatingActionButtonAnimator.GetAnimationRestart(_floatingActionButtonMoveController.Value);
        }

        SetState(() =>
        {
            _previousFloatingActionButtonLocation = previousLocation;
            _floatingActionButtonLocation = newLocation;
        });

        // Animate the motion even when the fab is null so that if the exit animation is running, the fab
        // will go to the right place.
        _floatingActionButtonMoveController.Forward(from: restartAnimationFrom);
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

    private void DrawerOpenedCallback(bool isOpened)
    {
        if (_drawerOpened != isOpened && _drawerKey.CurrentState is not null)
        {
            SetState(() => _drawerOpened = isOpened);
            CurrentWidget.OnDrawerChanged?.Invoke(isOpened);
        }
    }

    private void EndDrawerOpenedCallback(bool isOpened)
    {
        if (_endDrawerOpened != isOpened && _endDrawerKey.CurrentState is not null)
        {
            SetState(() => _endDrawerOpened = isOpened);
            CurrentWidget.OnEndDrawerChanged?.Invoke(isOpened);
        }
    }

    /// <summary>
    /// Opens the <see cref="Scaffold.Drawer"/>, closing <see cref="Scaffold.EndDrawer"/> first when it is
    /// open. Has no effect if the scaffold has no drawer.
    /// </summary>
    public void OpenDrawer()
    {
        if (_endDrawerKey.CurrentState is not null && _endDrawerOpened)
        {
            _endDrawerKey.CurrentState.Close();
        }

        _drawerKey.CurrentState?.Open();
    }

    /// <summary>
    /// Opens the <see cref="Scaffold.EndDrawer"/>, closing <see cref="Scaffold.Drawer"/> first when it is
    /// open. Has no effect if the scaffold has no end drawer.
    /// </summary>
    public void OpenEndDrawer()
    {
        if (_drawerKey.CurrentState is not null && _drawerOpened)
        {
            _drawerKey.CurrentState.Close();
        }

        _endDrawerKey.CurrentState?.Open();
    }

    /// <summary>Closes the <see cref="Scaffold.Drawer"/> if it is currently open.</summary>
    public void CloseDrawer()
    {
        if (HasDrawer && IsDrawerOpen)
        {
            _drawerKey.CurrentState!.Close();
        }
    }

    /// <summary>Closes the <see cref="Scaffold.EndDrawer"/> if it is currently open.</summary>
    public void CloseEndDrawer()
    {
        if (HasEndDrawer && IsEndDrawerOpen)
        {
            _endDrawerKey.CurrentState!.Close();
        }
    }

    /// <summary>
    /// Ports the iOS/macOS status-bar tap: scrolls the primary scrollable of the foreground scaffold back to
    /// the top. Dispatched by <see cref="WidgetsBinding.HandleStatusBarTap"/>, never by a gesture recognizer.
    /// </summary>
    public void HandleStatusBarTap()
    {
        ScrollController? primaryScrollController = PrimaryScrollController.MaybeOf(Context);
        if (primaryScrollController is { HasClients: true }
            && HitTestableAtOrigin.IsHitTestableAtOrigin(_statusBarKey))
        {
            primaryScrollController.AnimateTo(
                0.0,
                duration: StatusBarTapScrollDuration,
                curve: Curves.EaseOutCirc);
        }
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
        if (!ReferenceEquals(
                oldScaffold.FloatingActionButtonAnimator,
                CurrentWidget.FloatingActionButtonAnimator))
        {
            _floatingActionButtonAnimator = CurrentWidget.FloatingActionButtonAnimator;
        }

        if (!ReferenceEquals(
                oldScaffold.FloatingActionButtonLocation,
                CurrentWidget.FloatingActionButtonLocation))
        {
            MoveFloatingActionButton(CurrentWidget.FloatingActionButtonLocation);
        }

        if (!ReferenceEquals(oldScaffold.BottomSheet, CurrentWidget.BottomSheet))
        {
            SyncStaticBottomSheetAnimation();
        }

        switch (oldScaffold.Primary, CurrentWidget.Primary)
        {
            case (true, false):
                WidgetsBinding.Instance.RemoveObserver(this);
                break;
            case (false, true):
                WidgetsBinding.Instance.AddObserver(this);
                break;
            case (true, true):
            case (false, false):
                break;
        }
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var effectiveBackground = CurrentWidget.BackgroundColor ?? theme.ScaffoldBackgroundColor;
        var textDirection = Directionality.Of(context);
        var mediaQuery = MediaQuery.MaybeOf(context) ?? new MediaQueryData();
        ScaffoldMessengerState? messenger = ScaffoldMessenger.MaybeOf(context);
        var presentedSnackBar = messenger?.SnackBarFor(this);
        MaterialBanner? presentedMaterialBanner = messenger?.MaterialBannerFor(this);
        bool resizeToAvoidBottomInset = CurrentWidget.ResizeToAvoidBottomInset ?? true;

        var children = new List<Widget>();
        AddIfNonNull(
            children,
            mediaQuery,
            CurrentWidget.Body is null
                ? null
                : new BodyBuilder(
                    extendBody: CurrentWidget.ExtendBody,
                    extendBodyBehindAppBar: CurrentWidget.ExtendBodyBehindAppBar,
                    body: new KeyedSubtree(key: _bodyKey, child: CurrentWidget.Body)),
            ScaffoldSlot.Body,
            removeTopPadding: CurrentWidget.AppBar is not null,
            removeBottomPadding: CurrentWidget.BottomNavigationBar is not null
                                 || CurrentWidget.PersistentFooterButtons is not null,
            removeBottomInset: resizeToAvoidBottomInset);

        if (_showBodyScrim
            && CurrentWidget.BottomSheetScrimBuilder(context, _bottomSheetScrimAnimationController) is { } bodyScrim)
        {
            AddIfNonNull(
                children,
                mediaQuery,
                bodyScrim,
                ScaffoldSlot.BodyScrim,
                removeLeftPadding: true,
                removeTopPadding: true,
                removeRightPadding: true,
                removeBottomPadding: true);
        }

        if (CurrentWidget.AppBar is { } appBar)
        {
            _appBarMaxHeight = Scaffold.PreferredAppBarHeight(context, appBar)
                               + (CurrentWidget.Primary ? mediaQuery.Padding.Top : 0.0);
            if (double.IsNaN(_appBarMaxHeight.Value)
                || double.IsInfinity(_appBarMaxHeight.Value)
                || _appBarMaxHeight.Value < 0.0)
            {
                throw new InvalidOperationException("AppBar preferred height must be finite and non-negative.");
            }

            AddIfNonNull(
                children,
                mediaQuery,
                new ConstrainedBox(
                    constraints: new BoxConstraints(MaxHeight: _appBarMaxHeight.Value),
                    child: FlexibleSpaceBar.CreateSettings(
                        currentExtent: _appBarMaxHeight.Value,
                        child: (Widget)appBar)),
                ScaffoldSlot.AppBar,
                removeBottomPadding: true);
        }

        var presentedBottomSheet = BuildPresentedBottomSheet(context);
        if (presentedBottomSheet is not null)
        {
            AddIfNonNull(
                children,
                mediaQuery,
                presentedBottomSheet,
                ScaffoldSlot.BottomSheet,
                removeTopPadding: true,
                removeBottomPadding: resizeToAvoidBottomInset);
        }

        bool isSnackBarFloating = false;
        double? snackBarWidth = null;
        if (presentedSnackBar is not null)
        {
            var snackBarTheme = SnackBarTheme.Of(context);
            SnackBarBehavior snackBarBehavior =
                presentedSnackBar.Behavior ?? snackBarTheme.Behavior ?? SnackBarBehavior.Fixed;
            isSnackBarFloating = snackBarBehavior == SnackBarBehavior.Floating;
            snackBarWidth = presentedSnackBar.Width ?? snackBarTheme.Width;
            AddIfNonNull(
                children,
                mediaQuery,
                presentedSnackBar,
                ScaffoldSlot.SnackBar,
                removeTopPadding: true,
                removeBottomPadding: CurrentWidget.BottomNavigationBar is not null
                                     || CurrentWidget.PersistentFooterButtons is not null,
                maintainBottomViewPadding: !resizeToAvoidBottomInset);
        }

        bool extendBodyBehindMaterialBanner = false;
        if (presentedMaterialBanner is not null)
        {
            double materialBannerElevation = presentedMaterialBanner.Elevation
                                             ?? MaterialBannerTheme.Of(context).Elevation
                                             ?? 0.0;
            extendBodyBehindMaterialBanner = materialBannerElevation != 0.0;
            AddIfNonNull(
                children,
                mediaQuery,
                presentedMaterialBanner,
                ScaffoldSlot.MaterialBanner,
                removeTopPadding: CurrentWidget.AppBar is not null,
                removeBottomPadding: true,
                maintainBottomViewPadding: !resizeToAvoidBottomInset);
        }

        if (CurrentWidget.PersistentFooterButtons is { } persistentFooterButtons)
        {
            AddIfNonNull(
                children,
                mediaQuery,
                new Container(
                    decoration: CurrentWidget.PersistentFooterDecoration
                                ?? new BoxDecoration(
                                    Border: new Border(top: Divider.CreateBorderSide(context, width: 1.0))),
                    child: new SafeArea(
                        top: false,
                        child: new IntrinsicHeight(
                            child: new Padding(
                                insets: EdgeInsets.All(8),
                                child: new Align(
                                    alignment: CurrentWidget.PersistentFooterAlignment,
                                    child: new OverflowBar(
                                        spacing: 8,
                                        overflowAlignment: OverflowBarAlignment.End,
                                        children: persistentFooterButtons)))))),
                ScaffoldSlot.PersistentFooter,
                removeTopPadding: true,
                removeBottomPadding: CurrentWidget.BottomNavigationBar is not null,
                maintainBottomViewPadding: !resizeToAvoidBottomInset);
        }

        if (CurrentWidget.BottomNavigationBar is not null)
        {
            AddIfNonNull(
                children,
                mediaQuery,
                CurrentWidget.BottomNavigationBar,
                ScaffoldSlot.BottomNavigationBar,
                removeTopPadding: true,
                maintainBottomViewPadding: !resizeToAvoidBottomInset);
        }

        AddIfNonNull(
            children,
            mediaQuery,
            new FloatingActionButtonTransition(
                child: CurrentWidget.FloatingActionButton,
                fabMoveAnimation: _floatingActionButtonMoveController,
                fabMotionAnimator: _floatingActionButtonAnimator,
                geometryNotifier: _geometryNotifier,
                currentController: _floatingActionButtonVisibilityController),
            ScaffoldSlot.FloatingActionButton,
            removeLeftPadding: true,
            removeTopPadding: true,
            removeRightPadding: true,
            removeBottomPadding: true);

        Widget? statusBar = theme.Platform switch
        {
            TargetPlatform.IOS or TargetPlatform.MacOS => CurrentWidget.Primary
                ? new HitTestableAtOrigin(_statusBarKey)
                : null,
            _ => null,
        };
        AddIfNonNull(
            children,
            mediaQuery,
            statusBar,
            ScaffoldSlot.StatusBar,
            removeTopPadding: true,
            removeBottomPadding: true);

        if (_endDrawerOpened)
        {
            BuildDrawer(children, mediaQuery, textDirection);
            BuildEndDrawer(children, mediaQuery, textDirection);
        }
        else
        {
            BuildEndDrawer(children, mediaQuery, textDirection);
            BuildDrawer(children, mediaQuery, textDirection);
        }

        // The minimum insets for contents of the Scaffold to keep visible.
        Thickness minInsets = CopyBottomInset(
            mediaQuery.Padding,
            resizeToAvoidBottomInset ? mediaQuery.ViewInsets.Bottom : 0.0);

        // The minimum viewPadding for interactive elements positioned by the Scaffold to keep within safe
        // interactive areas.
        Thickness minViewPadding = resizeToAvoidBottomInset && mediaQuery.ViewInsets.Bottom != 0.0
            ? CopyBottomInset(mediaQuery.ViewPadding, 0.0)
            : mediaQuery.ViewPadding;

        Widget content = new CustomMultiChildLayout(
            @delegate: new ScaffoldLayout(
                minInsets: minInsets,
                minViewPadding: minViewPadding,
                geometryNotifier: _geometryNotifier,
                previousFloatingActionButtonLocation: _previousFloatingActionButtonLocation!,
                currentFloatingActionButtonLocation: _floatingActionButtonLocation!,
                floatingActionButtonMoveAnimation: _floatingActionButtonMoveController,
                floatingActionButtonMotionAnimator: _floatingActionButtonAnimator,
                isSnackBarFloating: isSnackBarFloating,
                snackBarWidth: snackBarWidth,
                extendBody: CurrentWidget.ExtendBody,
                extendBodyBehindAppBar: CurrentWidget.ExtendBodyBehindAppBar,
                extendBodyBehindMaterialBanner: extendBodyBehindMaterialBanner,
                textDirection: textDirection),
            children: children);

        return new ScaffoldScope(
            scaffold: this,
            hasDrawer: HasDrawer,
            hasEndDrawer: HasEndDrawer,
            isDrawerOpen: _drawerOpened,
            isEndDrawerOpen: _endDrawerOpened,
            geometryNotifier: _geometryNotifier,
            child: new ScrollNotificationObserver(
                child: new Container(
                    color: effectiveBackground,
                    child: new Builder(builder: actionsContext => new Actions(
                        actions: new Dictionary<Type, FlutterAction>
                        {
                            [typeof(DismissIntent)] = new DismissDrawerAction(actionsContext),
                        },
                        child: content)))));
    }

    /// <summary>Ports Flutter's private <c>_buildEndDrawer</c>.</summary>
    private void BuildEndDrawer(List<Widget> children, MediaQueryData mediaQuery, TextDirection textDirection)
    {
        if (CurrentWidget.EndDrawer is not { } endDrawer)
        {
            return;
        }

        AddIfNonNull(
            children,
            mediaQuery,
            new DrawerController(
                key: _endDrawerKey,
                alignment: DrawerAlignment.End,
                drawerCallback: EndDrawerOpenedCallback,
                dragStartBehavior: CurrentWidget.DrawerDragStartBehavior,
                scrimColor: CurrentWidget.DrawerScrimColor,
                edgeDragWidth: CurrentWidget.DrawerEdgeDragWidth,
                enableOpenDragGesture: CurrentWidget.EndDrawerEnableOpenDragGesture,
                isDrawerOpen: _endDrawerOpened,
                drawerBarrierDismissible: CurrentWidget.DrawerBarrierDismissible,
                child: endDrawer),
            ScaffoldSlot.EndDrawer,

            // Remove the side padding from the side we're not touching.
            removeLeftPadding: textDirection == TextDirection.Ltr,
            removeRightPadding: textDirection == TextDirection.Rtl);
    }

    /// <summary>Ports Flutter's private <c>_buildDrawer</c>.</summary>
    private void BuildDrawer(List<Widget> children, MediaQueryData mediaQuery, TextDirection textDirection)
    {
        if (CurrentWidget.Drawer is not { } drawer)
        {
            return;
        }

        AddIfNonNull(
            children,
            mediaQuery,
            new DrawerController(
                key: _drawerKey,
                alignment: DrawerAlignment.Start,
                drawerCallback: DrawerOpenedCallback,
                dragStartBehavior: CurrentWidget.DrawerDragStartBehavior,
                scrimColor: CurrentWidget.DrawerScrimColor,
                edgeDragWidth: CurrentWidget.DrawerEdgeDragWidth,
                enableOpenDragGesture: CurrentWidget.DrawerEnableOpenDragGesture,
                isDrawerOpen: _drawerOpened,
                drawerBarrierDismissible: CurrentWidget.DrawerBarrierDismissible,
                child: drawer),
            ScaffoldSlot.Drawer,

            // Remove the side padding from the side we're not touching.
            removeLeftPadding: textDirection == TextDirection.Rtl,
            removeRightPadding: textDirection == TextDirection.Ltr);
    }

    /// <summary>
    /// Ports Flutter's private <c>_addIfNonNull</c>: wraps a scaffold slot in the <see cref="MediaQuery"/>
    /// the slot expects and gives it its <see cref="LayoutId"/>.
    /// </summary>
    private static void AddIfNonNull(
        List<Widget> children,
        MediaQueryData mediaQuery,
        Widget? child,
        ScaffoldSlot childId,
        bool removeLeftPadding = false,
        bool removeTopPadding = false,
        bool removeRightPadding = false,
        bool removeBottomPadding = false,
        bool removeBottomInset = false,
        bool maintainBottomViewPadding = false)
    {
        MediaQueryData data = mediaQuery.RemovePadding(
            removeLeft: removeLeftPadding,
            removeTop: removeTopPadding,
            removeRight: removeRightPadding,
            removeBottom: removeBottomPadding);
        if (removeBottomInset)
        {
            data = data.RemoveViewInsets(removeBottom: true);
        }

        if (maintainBottomViewPadding && data.ViewInsets.Bottom != 0.0)
        {
            data = data.CopyWith(padding: CopyBottomInset(data.Padding, data.ViewPadding.Bottom));
        }

        if (child is null)
        {
            return;
        }

        children.Add(new LayoutId(id: childId, child: new MediaQuery(data: data, child: child)));
    }

    private static Thickness CopyBottomInset(Thickness insets, double bottom) =>
        new(insets.Left, insets.Top, insets.Right, bottom);

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
}
