using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/scaffold.dart

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
        bool? resizeToAvoidBottomInset = null,
        string? restorationId = null) : base(key)
    {
        // Dart's Scaffold constructor asserts nothing; `DrawerController` is where a non-positive
        // `edgeDragWidth` is rejected.
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
        RestorationId = restorationId;
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

    /// <summary>
    /// Restoration ID to save and restore the state of the <see cref="Scaffold"/>: whether
    /// <see cref="Drawer"/> and <see cref="EndDrawer"/> were open is restored under it. When null,
    /// state restoration is disabled for this scaffold.
    /// </summary>
    public string? RestorationId { get; }

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
        return context.FindAncestorStateOfType<ScaffoldState>();
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
        ScaffoldState scaffold = Scaffold.Of(_context);
        if (IsEnabled(intent))
        {
            scaffold.CloseDrawer();
            scaffold.CloseEndDrawer();
        }

        return null;
    }
}

internal sealed class ScaffoldScope : InheritedWidget
{
    public ScaffoldScope(
        bool hasDrawer,
        ScaffoldGeometryNotifier geometryNotifier,
        Widget child,
        Key? key = null) : base(key)
    {
        HasDrawer = hasDrawer;
        GeometryNotifier = geometryNotifier ?? throw new ArgumentNullException(nameof(geometryNotifier));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public bool HasDrawer { get; }

    public ScaffoldGeometryNotifier GeometryNotifier { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (ScaffoldScope)oldWidget;
        return HasDrawer != oldScope.HasDrawer;
    }
}

public sealed class ScaffoldState : RestorationState, WidgetsBindingObserver
{
    private static readonly TimeSpan StatusBarTapScrollDuration = TimeSpan.FromMilliseconds(1000);
    private readonly LabeledGlobalKey<DrawerControllerState> _drawerKey = new("Scaffold drawer");
    private readonly LabeledGlobalKey<DrawerControllerState> _endDrawerKey = new("Scaffold end drawer");
    private readonly LabeledGlobalKey<State> _statusBarKey = new("Scaffold status bar");
    private readonly LabeledGlobalKey<State> _bodyKey = new("Scaffold body");
    private readonly RestorableBool _drawerOpened = new(false);
    private readonly RestorableBool _endDrawerOpened = new(false);

    // Contains bottom sheets that may still be animating out of view. Important if the app or the user
    // takes an action that could repeatedly show a bottom sheet.
    private readonly List<StandardBottomSheet> _dismissedBottomSheets = [];
    private readonly LabeledGlobalKey<State> _currentBottomSheetKey = new("Scaffold bottom sheet");
    private PersistentBottomSheetController? _currentBottomSheet;
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

    protected override string? RestorationId => CurrentWidget.RestorationId;

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RegisterForRestoration(_drawerOpened, "drawer_open");
        RegisterForRestoration(_endDrawerOpened, "end_drawer_open");
    }

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

    public bool IsDrawerOpen => _drawerOpened.Value;

    public bool IsEndDrawerOpen => _endDrawerOpened.Value;

    /// <summary>Whether tapping the drawer scrim closes the open drawer.</summary>
    public bool IsDrawerBarrierDismissible => CurrentWidget.DrawerBarrierDismissible;

    public override void InitState()
    {
        _geometryNotifier = new ScaffoldGeometryNotifier(new ScaffoldGeometry(), Context);
        _floatingActionButtonLocation = CurrentWidget.FloatingActionButtonLocation;
        _floatingActionButtonAnimator = CurrentWidget.FloatingActionButtonAnimator;
        _previousFloatingActionButtonLocation = _floatingActionButtonLocation;
        _floatingActionButtonMoveController = new AnimationController(
            value: 1.0,
            duration: FloatingActionButtonConstants.Segue * 2,
            vsync: this);
        _floatingActionButtonVisibilityController =
            new AnimationController(duration: FloatingActionButtonConstants.Segue, vsync: this);
        _bottomSheetScrimAnimationController = new AnimationController(vsync: this);
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
        _geometryNotifier.Dispose();
        _floatingActionButtonMoveController.Dispose();
        _floatingActionButtonVisibilityController.Dispose();
        _scaffoldMessenger?.Unregister(this);
        _drawerOpened.Dispose();
        _endDrawerOpened.Dispose();
        _bottomSheetScrimAnimationController.Dispose();
        base.Dispose();
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
        // Using MaybeOf is valid here since both the Scaffold and the ScaffoldMessenger are currently
        // available for managing SnackBars.
        ScaffoldMessengerState? currentScaffoldMessenger = ScaffoldMessenger.MaybeOf(Context);

        // If our ScaffoldMessenger has changed, unregister with the old one first.
        if (_scaffoldMessenger is not null
            && (currentScaffoldMessenger is null || !ReferenceEquals(_scaffoldMessenger, currentScaffoldMessenger)))
        {
            _scaffoldMessenger?.Unregister(this);
        }

        // Register with the current ScaffoldMessenger, if there is one.
        _scaffoldMessenger = currentScaffoldMessenger;
        _scaffoldMessenger?.Register(this);

        MaybeBuildPersistentBottomSheet();
        base.DidChangeDependencies();
    }

    private void DrawerOpenedCallback(bool isOpened)
    {
        if (_drawerOpened.Value != isOpened && _drawerKey.CurrentState is not null)
        {
            SetState(() => _drawerOpened.Value = isOpened);
            CurrentWidget.OnDrawerChanged?.Invoke(isOpened);
        }
    }

    private void EndDrawerOpenedCallback(bool isOpened)
    {
        if (_endDrawerOpened.Value != isOpened && _endDrawerKey.CurrentState is not null)
        {
            SetState(() => _endDrawerOpened.Value = isOpened);
            CurrentWidget.OnEndDrawerChanged?.Invoke(isOpened);
        }
    }

    /// <summary>
    /// Opens the <see cref="Scaffold.Drawer"/>, closing <see cref="Scaffold.EndDrawer"/> first when it is
    /// open. Has no effect if the scaffold has no drawer.
    /// </summary>
    public void OpenDrawer()
    {
        if (_endDrawerKey.CurrentState is not null && _endDrawerOpened.Value)
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
        if (_drawerKey.CurrentState is not null && _drawerOpened.Value)
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

    // PERSISTENT BOTTOM SHEET API

    /// <summary>Ports Flutter's private <c>_maybeBuildPersistentBottomSheet</c>.</summary>
    private void MaybeBuildPersistentBottomSheet()
    {
        if (CurrentWidget.BottomSheet is null || _currentBottomSheet is not null)
        {
            return;
        }

        // The new _currentBottomSheet is not a local history entry so a "back" button will not be added
        // to the Scaffold's app bar and the bottom sheet will not support drag or swipe to dismiss.
        AnimationController animationController = BottomSheet.CreateAnimationController(this);
        animationController.SetValue(1.0);

        bool PersistentBottomSheetExtentChanged(DraggableScrollableNotification notification)
        {
            if (notification.Extent - notification.InitialExtent > Constants.PrecisionErrorTolerance)
            {
                if (_persistentSheetHistoryEntry is null)
                {
                    _persistentSheetHistoryEntry = new LocalHistoryEntry(onRemove: () =>
                    {
                        DraggableScrollableActuator.Reset(notification.SourceContext);
                        ShowBodyScrim(false, 0.0);
                        _floatingActionButtonVisibilityController.SetValue(1.0);
                        _persistentSheetHistoryEntry = null;
                    });
                    ModalRoute.Of(Context).AddLocalHistoryEntry(_persistentSheetHistoryEntry);
                }
            }
            else
            {
                _persistentSheetHistoryEntry?.Remove();
            }

            return false;
        }

        // Stop the animation and unmount the dismissed sheets from the tree immediately, otherwise this
        // may cause a duplicate GlobalKey assertion if the sheet sub-tree contains GlobalKey widgets.
        if (_dismissedBottomSheets.Count > 0)
        {
            StandardBottomSheet[] sheets = [.. _dismissedBottomSheets];
            foreach (StandardBottomSheet sheet in sheets)
            {
                sheet.AnimationController.Reset();
            }
        }

        _currentBottomSheet = BuildBottomSheet(
            _ => new NotificationListener<DraggableScrollableNotification>(
                onNotification: PersistentBottomSheetExtentChanged,
                child: new DraggableScrollableActuator(
                    child: new StatefulBuilder(
                        key: _currentBottomSheetKey,
                        builder: (_, _) => CurrentWidget.BottomSheet ?? new SizedBox(width: 0, height: 0)))),
            isPersistent: true,
            animationController: animationController);
    }

    /// <summary>Ports Flutter's private <c>_closeCurrentBottomSheet</c>.</summary>
    private void CloseCurrentBottomSheet()
    {
        if (_currentBottomSheet is { IsLocalHistoryEntry: false } sheet)
        {
            sheet.Close();
        }
    }

    /// <summary>Ports Flutter's private <c>_updatePersistentBottomSheet</c>.</summary>
    private void UpdatePersistentBottomSheet()
    {
        _currentBottomSheetKey.CurrentState!.InvokeSetState(() => { });
    }

    /// <summary>Ports Flutter's private <c>_buildBottomSheet</c>.</summary>
    private PersistentBottomSheetController BuildBottomSheet(
        WidgetBuilder builder,
        bool isPersistent,
        AnimationController animationController,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        bool? enableDrag = null,
        bool? showDragHandle = null,
        bool shouldDisposeAnimationController = true)
    {
        if (CurrentWidget.BottomSheet is not null && isPersistent && _currentBottomSheet is not null)
        {
            throw new InvalidOperationException(
                "Scaffold.bottomSheet cannot be specified while a bottom sheet displayed with "
                + "showBottomSheet() is still visible.\n"
                + "Rebuild the Scaffold with a null bottomSheet before calling showBottomSheet().");
        }

        var bottomSheetKey = new LabeledGlobalKey<StandardBottomSheetState>("Scaffold standard bottom sheet");
        StandardBottomSheet bottomSheet = null!;
        PersistentBottomSheetController controller = null!;

        bool removedEntry = false;
        bool doingDispose = false;

        void RemovePersistentSheetHistoryEntryIfNeeded()
        {
            if (_persistentSheetHistoryEntry is not null)
            {
                _persistentSheetHistoryEntry.Remove();
                _persistentSheetHistoryEntry = null;
            }
        }

        void RemoveCurrentBottomSheet()
        {
            removedEntry = true;
            if (_currentBottomSheet is null)
            {
                return;
            }

            ShowFloatingActionButton();

            if (isPersistent)
            {
                RemovePersistentSheetHistoryEntryIfNeeded();
            }

            bottomSheetKey.CurrentState!.Close();
            SetState(() =>
            {
                _showBodyScrim = false;
                _bottomSheetScrimAnimationController.SetValue(0.0);
                _currentBottomSheet = null;
            });

            if (animationController.Status != AnimationStatus.Dismissed)
            {
                _dismissedBottomSheets.Add(bottomSheet);
            }

            controller.Complete(null);
        }

        LocalHistoryEntry? entry = isPersistent
            ? null
            : new LocalHistoryEntry(onRemove: () =>
            {
                if (!removedEntry && ReferenceEquals(_currentBottomSheet?.Feature, bottomSheet) && !doingDispose)
                {
                    RemoveCurrentBottomSheet();
                }
            });

        void RemoveEntryIfNeeded()
        {
            if (!isPersistent && !removedEntry)
            {
                entry!.Remove();
                removedEntry = true;
            }
        }

        bottomSheet = new StandardBottomSheet(
            key: bottomSheetKey,
            animationController: animationController,
            enableDrag: enableDrag ?? !isPersistent,
            showDragHandle: showDragHandle,
            onClosing: () =>
            {
                if (_currentBottomSheet is null)
                {
                    return;
                }

                RemoveEntryIfNeeded();
            },
            onDismissed: () =>
            {
                if (_dismissedBottomSheets.Contains(bottomSheet))
                {
                    SetState(() => _dismissedBottomSheets.Remove(bottomSheet));
                }
            },
            onDispose: () =>
            {
                doingDispose = true;
                RemoveEntryIfNeeded();
                if (shouldDisposeAnimationController)
                {
                    animationController.Dispose();
                }
            },
            builder: builder,
            isPersistent: isPersistent,
            backgroundColor: backgroundColor,
            elevation: elevation,
            shape: shape,
            clipBehavior: clipBehavior,
            constraints: constraints);

        if (!isPersistent)
        {
            ModalRoute.Of(Context).AddLocalHistoryEntry(entry!);
        }

        controller = new PersistentBottomSheetController(
            bottomSheet,
            entry is not null ? entry.Remove : RemoveCurrentBottomSheet,
            fn => bottomSheetKey.CurrentState?.InvokeSetState(fn),
            !isPersistent);
        return controller;
    }

    /// <summary>
    /// Shows a Material Design bottom sheet in the nearest <see cref="Scaffold"/>. To show a persistent
    /// bottom sheet, use <see cref="Scaffold.BottomSheet"/>.
    /// </summary>
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
                "Scaffold.bottomSheet cannot be specified while a bottom sheet displayed with "
                + "showBottomSheet() is still visible.\n"
                + "Rebuild the Scaffold with a null bottomSheet before calling showBottomSheet().");
        }

        CloseCurrentBottomSheet();
        AnimationController controller = transitionAnimationController
                                         ?? BottomSheet.CreateAnimationController(
                                             vsync: this,
                                             sheetAnimationStyle: sheetAnimationStyle);
        controller.Forward();
        SetState(() =>
        {
            _currentBottomSheet = BuildBottomSheet(
                builder,
                isPersistent: false,
                animationController: controller,
                backgroundColor: backgroundColor,
                elevation: elevation,
                shape: shape,
                clipBehavior: clipBehavior,
                constraints: constraints,
                enableDrag: enableDrag,
                showDragHandle: showDragHandle,
                shouldDisposeAnimationController: transitionAnimationController is null);
        });
        return _currentBottomSheet!;
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
            if (CurrentWidget.BottomSheet is not null && (_currentBottomSheet?.IsLocalHistoryEntry ?? false))
            {
                throw new InvalidOperationException(
                    "Scaffold.bottomSheet cannot be specified while a bottom sheet displayed with "
                    + "showBottomSheet() is still visible.\n"
                    + "Use the PersistentBottomSheetController returned by showBottomSheet() to close the "
                    + "old bottom sheet before creating a Scaffold with a (non null) bottomSheet.");
            }

            if (CurrentWidget.BottomSheet is null)
            {
                CloseCurrentBottomSheet();
            }
            else if (oldScaffold.BottomSheet is null)
            {
                MaybeBuildPersistentBottomSheet();
            }
            else
            {
                UpdatePersistentBottomSheet();
            }
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

        bool isSnackBarFloating = false;
        double? snackBarWidth = null;

        if (_currentBottomSheet is not null || _dismissedBottomSheets.Count > 0)
        {
            var sheets = new List<Widget>(_dismissedBottomSheets);
            if (_currentBottomSheet is not null)
            {
                sheets.Add(_currentBottomSheet.Feature);
            }

            AddIfNonNull(
                children,
                mediaQuery,
                new Stack(alignment: Alignment.BottomCenter, children: sheets),
                ScaffoldSlot.BottomSheet,
                removeTopPadding: true,
                removeBottomPadding: resizeToAvoidBottomInset);
        }
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

        if (_endDrawerOpened.Value)
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
            hasDrawer: HasDrawer,
            geometryNotifier: _geometryNotifier,
            child: new ScrollNotificationObserver(
                child: new Material(
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
                isDrawerOpen: _endDrawerOpened.Value,
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
                isDrawerOpen: _drawerOpened.Value,
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
}

/// <summary>
/// The standard bottom sheet hosted by a <see cref="Scaffold"/>: the sheet grows and shrinks with its
/// controller value instead of translating like the modal variant.
/// </summary>
/// <remarks>
/// Dart declares this as the private <c>_StandardBottomSheet</c>. C# cannot use an internal type as the
/// type argument of a public base class, and <see cref="PersistentBottomSheetController"/> derives from
/// <c>ScaffoldFeatureController&lt;_StandardBottomSheet, void&gt;</c>, so the type is public with an
/// internal constructor.
/// </remarks>
public sealed class StandardBottomSheet : StatefulWidget
{
    internal StandardBottomSheet(
        AnimationController animationController,
        WidgetBuilder builder,
        Action? onClosing,
        Action? onDismissed,
        bool enableDrag = true,
        bool? showDragHandle = null,
        bool isPersistent = false,
        Color? backgroundColor = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null,
        Action? onDispose = null,
        Key? key = null) : base(key)
    {
        AnimationController = animationController ?? throw new ArgumentNullException(nameof(animationController));
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        OnClosing = onClosing;
        OnDismissed = onDismissed;
        EnableDrag = enableDrag;
        ShowDragHandle = showDragHandle;
        IsPersistent = isPersistent;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        Shape = shape;
        ClipBehavior = clipBehavior;
        Constraints = constraints;
        OnDispose = onDispose;
    }

    /// <summary>The controller this sheet drives; whoever created it disposes it.</summary>
    public AnimationController AnimationController { get; }

    public bool EnableDrag { get; }

    public bool? ShowDragHandle { get; }

    public Action? OnClosing { get; }

    public Action? OnDismissed { get; }

    public Action? OnDispose { get; }

    public WidgetBuilder Builder { get; }

    public bool IsPersistent { get; }

    public Color? BackgroundColor { get; }

    public double? Elevation { get; }

    public ShapeBorder? Shape { get; }

    public Clip? ClipBehavior { get; }

    public BoxConstraints? Constraints { get; }

    public override State CreateState() => new StandardBottomSheetState();
}

/// <summary>Ports Flutter's private <c>_StandardBottomSheetState</c>.</summary>
public sealed class StandardBottomSheetState : State
{
    // Dart parity source: the file-level `_standardBottomSheetCurve = standardEasing` in scaffold.dart.
    private static readonly Curve StandardBottomSheetCurve = Curves.FastOutSlowIn;

    private Curve _animationCurve = StandardBottomSheetCurve;

    private StandardBottomSheet CurrentWidget => (StandardBottomSheet)StateWidget;

    public override void InitState()
    {
        base.InitState();
        if (!CurrentWidget.AnimationController.Status.IsForwardOrCompleted())
        {
            throw new InvalidOperationException(
                "A standard bottom sheet must be shown with a forward or completed animation controller.");
        }

        CurrentWidget.AnimationController.AddStatusListener(HandleStatusChange);
    }

    public override void Dispose()
    {
        CurrentWidget.AnimationController.RemoveStatusListener(HandleStatusChange);
        CurrentWidget.OnDispose?.Invoke();
        base.Dispose();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (!ReferenceEquals(((StandardBottomSheet)oldWidget).AnimationController, CurrentWidget.AnimationController))
        {
            throw new InvalidOperationException(
                "A standard bottom sheet cannot change its animation controller after it has been shown.");
        }
    }

    internal void Close()
    {
        CurrentWidget.AnimationController.Reverse();
        CurrentWidget.OnClosing?.Invoke();
    }

    private void HandleDragStart(DragStartDetails details)
    {
        // Allow the bottom sheet to track the user's finger accurately.
        _animationCurve = Curves.Linear;
    }

    private void HandleDragEnd(DragEndDetails details, bool isClosing)
    {
        // Allow the bottom sheet to animate smoothly from its current position.
        _animationCurve = Curves.Split(
            CurrentWidget.AnimationController.Value,
            endCurve: StandardBottomSheetCurve);
    }

    private void HandleStatusChange(AnimationStatus status)
    {
        if (status == AnimationStatus.Dismissed)
        {
            CurrentWidget.OnDismissed?.Invoke();
        }
    }

    private bool ExtentChanged(DraggableScrollableNotification notification)
    {
        double extentRemaining = 1.0 - notification.Extent;
        ScaffoldState scaffold = Scaffold.Of(Context);
        if (extentRemaining < Scaffold.BottomSheetDominatesPercentage)
        {
            scaffold.FloatingActionButtonVisibilityController.SetValue(
                extentRemaining * Scaffold.BottomSheetDominatesPercentage * 10);
            scaffold.ShowBodyScrim(true, 1 - (extentRemaining / Scaffold.BottomSheetDominatesPercentage));
        }
        else
        {
            scaffold.FloatingActionButtonVisibilityController.SetValue(1.0);
            scaffold.ShowBodyScrim(false, 0.0);
        }

        // If the Scaffold.bottomSheet is non-null we are a persistent bottom sheet.
        if (notification.Extent == notification.MinExtent
            && !scaffold.HasStaticBottomSheet
            && notification.ShouldCloseOnMinExtent)
        {
            Close();
        }

        return false;
    }

    public override Widget Build(BuildContext context)
    {
        StandardBottomSheet widget = CurrentWidget;
        return new AnimatedBuilder(
            animation: widget.AnimationController,
            builder: (_, child) => new Align(
                alignment: AlignmentDirectional.TopStart,
                heightFactor: _animationCurve(widget.AnimationController.Value),
                child: child),
            child: new Semantics(
                container: true,
                onDismiss: widget.IsPersistent ? null : Close,
                child: new NotificationListener<DraggableScrollableNotification>(
                    onNotification: ExtentChanged,
                    child: new BottomSheet(
                        animationController: widget.AnimationController,
                        enableDrag: widget.EnableDrag,
                        showDragHandle: widget.ShowDragHandle,
                        onDragStart: HandleDragStart,
                        onDragEnd: HandleDragEnd,
                        onClosing: widget.OnClosing!,
                        builder: widget.Builder,
                        backgroundColor: widget.BackgroundColor,
                        elevation: widget.Elevation,
                        shape: widget.Shape,
                        clipBehavior: widget.ClipBehavior,
                        constraints: widget.Constraints))));
    }
}

/// <summary>
/// A <see cref="ScaffoldFeatureController{TFeature,TClosedReason}"/> for standard bottom sheets: the type
/// returned by <see cref="ScaffoldState.ShowBottomSheet"/>. A bottom sheet is only persistent when it is
/// set as <see cref="Scaffold.BottomSheet"/>.
/// </summary>
public sealed class PersistentBottomSheetController : ScaffoldFeatureController<StandardBottomSheet, object?>
{
    internal PersistentBottomSheetController(
        StandardBottomSheet feature,
        Action close,
        StateSetter setState,
        bool isLocalHistoryEntry) : base(feature, close, setState)
    {
        SetState = setState;
        IsLocalHistoryEntry = isLocalHistoryEntry;
    }

    /// <summary>
    /// Marks the bottom sheet as needing to rebuild. Dart promotes the base class's nullable
    /// <c>setState</c> to non-null for this subtype (<c>StateSetter super.setState</c>); C# expresses that
    /// by shadowing the property with a non-nullable one that holds the same delegate.
    /// </summary>
    public new StateSetter SetState { get; }

    internal bool IsLocalHistoryEntry { get; }
}
