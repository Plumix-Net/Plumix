using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources (reference): material_ui/lib/src/scaffold.dart;
// material_ui/lib/src/app_bar.dart

public sealed class Scaffold : StatefulWidget
{
    public Scaffold(
        Widget body,
        AppBar? appBar = null,
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

    public Widget Body { get; }

    public AppBar? AppBar { get; }

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
    private LocalHistoryEntry? _persistentSheetHistoryEntry;

    private Scaffold CurrentWidget => (Scaffold)StateWidget;

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
        _bottomSheetScrimAnimationController = new AnimationController(TimeSpan.Zero, this);
        _floatingActionButtonLocation = CurrentWidget.FloatingActionButtonLocation;
        _floatingActionButtonAnimator = CurrentWidget.FloatingActionButtonAnimator;
        _previousFloatingActionButtonLocation = _floatingActionButtonLocation;
        _floatingActionButtonMoveController = new AnimationController(
            FloatingActionButtonConstants.Segue * 2,
            this);
        _floatingActionButtonMoveController.SetValue(1.0);
        _floatingActionButtonVisibilityController =
            new AnimationController(FloatingActionButtonConstants.Segue, this);
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
            new BodyBuilder(
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
            double topPadding = appBar.Primary ? mediaQuery.Padding.Top : 0.0;
            double appBarMaxHeight = AppBar.PreferredHeightFor(context, appBar.PreferredSize) + topPadding;
            AddIfNonNull(
                children,
                mediaQuery,
                new ConstrainedBox(
                    constraints: new BoxConstraints(MaxHeight: appBarMaxHeight),
                    child: FlexibleSpaceBar.CreateSettings(currentExtent: appBarMaxHeight, child: appBar)),
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
