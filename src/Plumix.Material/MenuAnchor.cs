using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/menu_anchor.dart.

public delegate Widget MenuAnchorChildBuilder(BuildContext context, MenuController controller, Widget? child);

/// <summary>Shared metrics, durations and curves for the Material menu family.</summary>
internal static class MenuConstants
{
    public const double DefaultSubmenuIconSize = 24.0;
    public const double LabelItemDefaultSpacing = 12.0;
    public const double LabelItemMinSpacing = 4.0;
    public const double MenuVerticalMinPadding = 8.0;
    public const double MenuViewPadding = 8.0;
    public const double TopLevelMenuHorizontalMinPadding = 4.0;

    public static readonly TimeSpan OpeningDuration = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan ClosingDuration = TimeSpan.FromMilliseconds(150);

    /// <summary>Flutter's `kThemeChangeDuration`, used by the menu button defaults.</summary>
    public static readonly TimeSpan ThemeChangeDuration = TimeSpan.FromMilliseconds(200);

    public const double ItemRelativeFadeInDuration = 1.0 / 2.0;
    public const double ItemRelativeFadeOutDuration = 1.0 / 3.0;
    public const double ItemRelativeFadeOutDelay = 1.0 / 3.0;

    public static Curve PanelHeightForwardCurve { get; } = Curves.Cubic(0.3, 0.0, 0.0, 1.0);

    public static Curve PanelHeightReverseCurve { get; } =
        Curves.TweenCurve(0.35, 1.0, Curves.Flipped(Curves.EmphasizedAccelerate));

    public static Curve PanelOpacityForwardCurve { get; } = Curves.Interval(0.0, 50.0 / 500.0);

    public static Curve PanelOpacityReverseCurve { get; } =
        Curves.Flipped(Curves.Interval(100.0 / 150.0, 150.0 / 150.0));

    /// <summary>
    /// The staggered fade curves for menu item <paramref name="index"/> of <paramref name="itemCount"/>.
    /// </summary>
    public static (Curve Forward, Curve Reverse) ItemFadeCurves(int index, int itemCount)
    {
        const double forwardFinalItemOffset = 1.0 - ItemRelativeFadeInDuration;
        const double reverseFinalItemOffset =
            1.0 - ItemRelativeFadeOutDuration - ItemRelativeFadeOutDelay;
        double itemFadeInGap = itemCount > 1 ? forwardFinalItemOffset / (itemCount - 1) : 0.0;
        double itemFadeOutGap = itemCount > 1 ? reverseFinalItemOffset / (itemCount - 1) : 0.0;
        double forwardProgress = itemFadeInGap * index;
        double reverseProgress = itemFadeOutGap * index;
        return (
            Curves.Interval(forwardProgress, Math.Min(1.0, forwardProgress + ItemRelativeFadeInDuration)),
            Curves.Interval(reverseProgress, Math.Min(1.0, reverseProgress + ItemRelativeFadeOutDuration)));
    }

    /// <summary>The shortcut map installed on every Material menu panel and menu bar.</summary>
    public static IReadOnlyDictionary<ShortcutActivator, Intent> TraversalShortcuts { get; } =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.GameButtonA)] = new ActivateIntent(),
            [new SingleActivator(LogicalKeyboardKey.Escape)] = new DismissIntent(),
            [new SingleActivator(LogicalKeyboardKey.Tab)] = new NextFocusIntent(),
            [new SingleActivator(LogicalKeyboardKey.Tab, shift: true)] = new PreviousFocusIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new DirectionalFocusIntent(TraversalDirection.Down),
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DirectionalFocusIntent(TraversalDirection.Up),
            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = new DirectionalFocusIntent(TraversalDirection.Left),
            [new SingleActivator(LogicalKeyboardKey.ArrowRight)] = new DirectionalFocusIntent(TraversalDirection.Right),
        };
}

public class MenuAnchor : StatefulWidget
{
    public MenuAnchor(
        IReadOnlyList<Widget> menuChildren,
        MenuAnchorChildBuilder? builder = null,
        Widget? child = null,
        MenuController? controller = null,
        FocusNode? childFocusNode = null,
        MenuStyle? style = null,
        Vector? alignmentOffset = null,
        EdgeInsetsGeometry? reservedPadding = null,
        LayerLink? layerLink = null,
        Clip clipBehavior = Clip.HardEdge,
        bool consumeOutsideTap = false,
        Action? onOpen = null,
        Action? onClose = null,
        bool crossAxisUnconstrained = true,
        bool useRootOverlay = false,
        bool animated = false,
        Action<AnimationStatus>? onAnimationStatusChanged = null,
        Key? key = null) : base(key)
    {
        MenuChildren = menuChildren ?? throw new ArgumentNullException(nameof(menuChildren));
        Builder = builder;
        Child = child;
        Controller = controller;
        ChildFocusNode = childFocusNode;
        Style = style;
        AlignmentOffset = alignmentOffset ?? default;
        ReservedPadding = reservedPadding;
        LayerLink = layerLink;
        ClipBehavior = clipBehavior;
        ConsumeOutsideTap = consumeOutsideTap;
        OnOpen = onOpen;
        OnClose = onClose;
        CrossAxisUnconstrained = crossAxisUnconstrained;
        UseRootOverlay = useRootOverlay;
        Animated = animated;
        OnAnimationStatusChanged = onAnimationStatusChanged;
    }

    public IReadOnlyList<Widget> MenuChildren { get; }
    public MenuAnchorChildBuilder? Builder { get; }
    public Widget? Child { get; }
    public MenuController? Controller { get; }
    public FocusNode? ChildFocusNode { get; }
    public MenuStyle? Style { get; }
    public Vector AlignmentOffset { get; }
    public EdgeInsetsGeometry? ReservedPadding { get; }
    public LayerLink? LayerLink { get; }
    public Clip ClipBehavior { get; }
    public bool ConsumeOutsideTap { get; }
    public Action? OnOpen { get; }
    public Action? OnClose { get; }
    public bool CrossAxisUnconstrained { get; }
    public bool UseRootOverlay { get; }
    public bool Animated { get; }
    public Action<AnimationStatus>? OnAnimationStatusChanged { get; }

    public override State CreateState() => new MenuAnchorState();
}

public class MenuAnchorState : State
{
    private readonly List<Widget> _menuChildren = [];
    private readonly List<CurvedAnimation> _cachedAnimations = [];
    private AnimationController _animationController = null!;
    private MenuController? _internalMenuController;
    private MenuAnchorState? _anchorParent;

    internal FocusScopeNode MenuScopeNode { get; } = new();

    internal CurvedAnimation HeightAnimation { get; private set; } = null!;

    internal CurvedAnimation OpacityAnimation { get; private set; } = null!;

    internal MenuController MenuController => CurrentAnchor.Controller ?? _internalMenuController!;

    internal MenuAnchorState? AnchorParent => _anchorParent;

    internal IReadOnlyList<Widget> ResolvedMenuChildren => _menuChildren;

    internal AnimationStatus AnimationStatus => _animationController.Status;

    /// <summary>The axis the panel of this anchor lays its children out along.</summary>
    internal virtual Axis Orientation => Axis.Vertical;

    internal Axis AnchorParentOrientation => _anchorParent?.Orientation ?? Axis.Horizontal;

    internal bool IsClosing => _animationController.Status == Plumix.AnimationStatus.Reverse;

    internal bool IsClosingOrClosed =>
        _animationController.Status is Plumix.AnimationStatus.Reverse or Plumix.AnimationStatus.Dismissed;

    internal MenuAnchorState RootAnchor
    {
        get
        {
            MenuAnchorState anchor = this;
            while (anchor._anchorParent is not null)
            {
                anchor = anchor._anchorParent;
            }

            return anchor;
        }
    }

    internal MenuAnchor CurrentAnchor => (MenuAnchor)StateWidget;

    public override void InitState()
    {
        if (CurrentAnchor.Controller is null)
        {
            _internalMenuController = new MenuController();
        }

        _animationController = new AnimationController(duration: TimeSpan.FromMilliseconds(1), vsync: this);
        ResolveAnimationController();
        _animationController.AddStatusListener(HandleAnimationStatusChanged);
        HeightAnimation = new CurvedAnimation(
            _animationController,
            MenuConstants.PanelHeightForwardCurve,
            MenuConstants.PanelHeightReverseCurve);
        OpacityAnimation = new CurvedAnimation(
            _animationController,
            MenuConstants.PanelOpacityForwardCurve,
            MenuConstants.PanelOpacityReverseCurve);
        ResolveMenuItems();
    }

    public override void DidChangeDependencies()
    {
        _anchorParent = MenuAnchorScope.MaybeOf(Context)?.State;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (MenuAnchor)oldWidget;
        if (!ReferenceEquals(previous.Controller, CurrentAnchor.Controller))
        {
            _internalMenuController = CurrentAnchor.Controller is null ? new MenuController() : null;
        }

        if (previous.Animated != CurrentAnchor.Animated)
        {
            ResolveAnimationController();
        }

        if (previous.Animated != CurrentAnchor.Animated
            || !ReferenceEquals(previous.MenuChildren, CurrentAnchor.MenuChildren))
        {
            ResolveMenuItems();
        }
    }

    public override void Dispose()
    {
        _menuChildren.Clear();
        foreach (CurvedAnimation animation in _cachedAnimations)
        {
            animation.Dispose();
        }

        _cachedAnimations.Clear();
        _internalMenuController = null;
        MenuScopeNode.Dispose();
        HeightAnimation.Dispose();
        OpacityAnimation.Dispose();
        _animationController.RemoveStatusListener(HandleAnimationStatusChanged);
        _animationController.Stop();
        _animationController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        Widget result = new MenuAnchorScope(
            state: this,
            animationStatus: _animationController.Status,
            child: new RawMenuAnchor(
                controller: MenuController,
                overlayBuilder: BuildOverlay,
                childFocusNode: CurrentAnchor.ChildFocusNode,
                consumeOutsideTaps: CurrentAnchor.ConsumeOutsideTap,
                onOpen: CurrentAnchor.OnOpen,
                onClose: CurrentAnchor.OnClose,
                onOpenRequested: HandleMenuOpenRequest,
                onCloseRequested: HandleMenuCloseRequest,
                useRootOverlay: CurrentAnchor.UseRootOverlay,
                builder: CurrentAnchor.Builder is null
                    ? null
                    : (anchorContext, controller, child) =>
                        CurrentAnchor.Builder(anchorContext, controller, child),
                child: CurrentAnchor.Child));

        if (CurrentAnchor.LayerLink is not null)
        {
            result = new CompositedTransformTarget(CurrentAnchor.LayerLink, result);
        }

        return result;
    }

    /// <summary>Focuses the widget the menu is anchored to, when one was supplied.</summary>
    internal void FocusButton()
    {
        CurrentAnchor.ChildFocusNode?.RequestFocus();
    }

    internal void FocusFirstMenuItem()
    {
        FocusTraversalPolicy policy = ResolveTraversalPolicy();
        policy.FindFirstFocus(MenuScopeNode, ignoreCurrentFocus: true)?.RequestFocus();
    }

    internal void FocusLastMenuItem()
    {
        FocusTraversalPolicy policy = ResolveTraversalPolicy();
        policy.FindLastFocus(MenuScopeNode, ignoreCurrentFocus: true)?.RequestFocus();
    }

    internal static BoxConstraints ResolveMenuConstraints(
        MenuStyle style,
        MaterialState state,
        VisualDensity density)
    {
        Size minimum = style.MinimumSize?.Resolve(state) ?? default;
        Size maximum = style.MaximumSize?.Resolve(state)
                       ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
        var constraints = new BoxConstraints(
            MinWidth: minimum.Width,
            MaxWidth: maximum.Width,
            MinHeight: minimum.Height,
            MaxHeight: maximum.Height);
        constraints = density.EffectiveConstraints(constraints);
        Size? fixedSize = style.FixedSize?.Resolve(state);
        if (fixedSize.HasValue)
        {
            // Flutter constrains the fixed size first, so a fixed size outside the min/max window is
            // clamped rather than winning outright.
            Size size = constraints.Constrain(fixedSize.Value);
            double? width = double.IsFinite(size.Width) ? size.Width : null;
            double? height = double.IsFinite(size.Height) ? size.Height : null;
            constraints = constraints.Tighten(width, height);
        }

        return constraints;
    }

    private FocusTraversalPolicy ResolveTraversalPolicy()
    {
        return Mounted
            ? FocusTraversalGroup.MaybeOf(Context) ?? new ReadingOrderTraversalPolicy()
            : new ReadingOrderTraversalPolicy();
    }

    private void ResolveAnimationController()
    {
        _animationController.Duration = CurrentAnchor.Animated
            ? MenuConstants.OpeningDuration
            : TimeSpan.Zero;
        _animationController.ReverseDuration = CurrentAnchor.Animated
            ? MenuConstants.ClosingDuration
            : TimeSpan.Zero;
    }

    private void ResolveMenuItems()
    {
        _menuChildren.Clear();
        foreach (CurvedAnimation animation in _cachedAnimations)
        {
            animation.Dispose();
        }

        _cachedAnimations.Clear();
        int itemCount = CurrentAnchor.MenuChildren.Count;
        if (itemCount == 0)
        {
            return;
        }

        if (!CurrentAnchor.Animated)
        {
            _menuChildren.AddRange(CurrentAnchor.MenuChildren);
            return;
        }

        for (int index = 0; index < itemCount; index++)
        {
            (Curve forwardCurve, Curve reverseCurve) = MenuConstants.ItemFadeCurves(index, itemCount);
            var animation = new CurvedAnimation(_animationController, forwardCurve, reverseCurve);
            _cachedAnimations.Add(animation);
            _menuChildren.Add(new FadeTransition(
                opacity: animation,
                alwaysIncludeSemantics: true,
                child: CurrentAnchor.MenuChildren[index]));
        }
    }

    private void HandleMenuOpenRequest(Vector? position, Action showOverlay)
    {
        if (_anchorParent?.IsClosing ?? false)
        {
            return;
        }

        showOverlay();
        if (_animationController.Status.IsForwardOrCompleted())
        {
            return;
        }

        RunOpenAnimation();
    }

    private void HandleMenuCloseRequest(Action hideOverlay)
    {
        if (!_animationController.Status.IsForwardOrCompleted())
        {
            return;
        }

        _pendingHideOverlay = hideOverlay;
        RunCloseAnimation();
    }

    private Action? _pendingHideOverlay;

    private void RunOpenAnimation()
    {
        _pendingHideOverlay = null;
        if (CurrentAnchor.Animated)
        {
            _animationController.Forward();
            return;
        }

        _animationController.Stop();
        _animationController.SetValue(1.0);
        HandleAnimationStatusChanged(Plumix.AnimationStatus.Completed);
    }

    private void RunCloseAnimation()
    {
        if (CurrentAnchor.Animated)
        {
            _animationController.Reverse();
            return;
        }

        _animationController.Stop();
        _animationController.SetValue(0.0);
        HandleAnimationStatusChanged(Plumix.AnimationStatus.Dismissed);
    }

    private void HandleAnimationStatusChanged(AnimationStatus status)
    {
        if (Mounted)
        {
            SetState(static () => { });
        }

        if (status == Plumix.AnimationStatus.Dismissed && _pendingHideOverlay is { } hideOverlay)
        {
            _pendingHideOverlay = null;
            hideOverlay();
        }

        CurrentAnchor.OnAnimationStatusChanged?.Invoke(status);
    }

    private Widget BuildOverlay(BuildContext context, RawMenuOverlayInfo info)
    {
        bool closing = IsClosingOrClosed;
        return new ExcludeSemantics(
            excluding: closing,
            child: new IgnorePointer(
                ignoring: closing,
                child: new ExcludeFocus(
                    excluding: closing,
                    child: new Submenu(
                        anchor: this,
                        fadeAnimation: OpacityAnimation,
                        heightAnimation: HeightAnimation,
                        layerLink: CurrentAnchor.LayerLink,
                        menuStyle: CurrentAnchor.Style,
                        clipBehavior: CurrentAnchor.ClipBehavior,
                        menuChildren: _menuChildren,
                        crossAxisUnconstrained: CurrentAnchor.CrossAxisUnconstrained,
                        menuPosition: info,
                        alignmentOffset: CurrentAnchor.AlignmentOffset,
                        reservedPadding: CurrentAnchor.ReservedPadding
                                         ?? EdgeInsetsGeometry.All(MenuConstants.MenuViewPadding)))));
    }
}

/// <summary>Publishes the nearest Material menu anchor and its animation status to descendants.</summary>
internal sealed class MenuAnchorScope : InheritedWidget
{
    public MenuAnchorScope(MenuAnchorState state, AnimationStatus animationStatus, Widget child) : base()
    {
        State = state;
        AnimationStatus = animationStatus;
        Child = child;
    }

    public MenuAnchorState State { get; }

    public AnimationStatus AnimationStatus { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        ((MenuAnchorScope)oldWidget).AnimationStatus != AnimationStatus;

    /// <summary>Returns the nearest anchor scope without creating a dependency.</summary>
    public static MenuAnchorScope? MaybeOf(BuildContext context) =>
        context.GetInherited<MenuAnchorScope>();

    /// <summary>Returns the nearest anchor's animation status, creating a dependency on it.</summary>
    public static AnimationStatus? MaybeAnimationStatusOf(BuildContext context) =>
        context.DependOnInherited<MenuAnchorScope>()?.AnimationStatus;
}

/// <summary>The overlay content of an open <see cref="MenuAnchor"/>.</summary>
internal sealed class Submenu : StatelessWidget
{
    public Submenu(
        MenuAnchorState anchor,
        Animation<double> fadeAnimation,
        Animation<double> heightAnimation,
        LayerLink? layerLink,
        MenuStyle? menuStyle,
        Clip clipBehavior,
        IReadOnlyList<Widget> menuChildren,
        bool crossAxisUnconstrained,
        RawMenuOverlayInfo menuPosition,
        Vector alignmentOffset,
        EdgeInsetsGeometry reservedPadding,
        Key? key = null) : base(key)
    {
        Anchor = anchor;
        FadeAnimation = fadeAnimation;
        HeightAnimation = heightAnimation;
        LayerLink = layerLink;
        MenuStyle = menuStyle;
        ClipBehavior = clipBehavior;
        MenuChildren = menuChildren;
        CrossAxisUnconstrained = crossAxisUnconstrained;
        MenuPosition = menuPosition;
        AlignmentOffset = alignmentOffset;
        ReservedPadding = reservedPadding;
    }

    public MenuAnchorState Anchor { get; }
    public Animation<double> FadeAnimation { get; }
    public Animation<double> HeightAnimation { get; }
    public LayerLink? LayerLink { get; }
    public MenuStyle? MenuStyle { get; }
    public Clip ClipBehavior { get; }
    public IReadOnlyList<Widget> MenuChildren { get; }
    public bool CrossAxisUnconstrained { get; }
    public RawMenuOverlayInfo MenuPosition { get; }
    public Vector AlignmentOffset { get; }
    public EdgeInsetsGeometry ReservedPadding { get; }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        TextDirection textDirection = Directionality.Of(context);
        MenuStyle defaults = Anchor.AnchorParentOrientation == Axis.Vertical
            ? new MenuDefaultsM3(context)
            : new MenuBarDefaultsM3(context);
        MenuStyle? themeStyle = Anchor.AnchorParentOrientation == Axis.Vertical
            ? MenuTheme.Of(context).Style
            : MenuBarTheme.Of(context).Style;
        MenuStyle resolved = (MenuStyle ?? new MenuStyle()).Merge(themeStyle).Merge(defaults);
        VisualDensity visualDensity = resolved.VisualDensity ?? theme.VisualDensity;
        AlignmentGeometry alignment = resolved.Alignment ?? AlignmentDirectional.BottomStart;
        var cursor = new MenuMouseCursor(states => resolved.MouseCursor?.Resolve(states));
        EdgeInsetsGeometry menuPadding = resolved.Padding?.Resolve(MaterialState.None)
                                         ?? EdgeInsetsGeometry.Zero;
        double densityDx = Math.Max(0.0, visualDensity.BaseSizeAdjustment.X);
        EdgeInsetsGeometry resolvedMenuPadding = menuPadding
            .Add(EdgeInsetsGeometry.Symmetric(horizontal: densityDx))
            .Clamp(EdgeInsetsGeometry.Zero, EdgeInsetsGeometry.Infinity);
        Rect layoutAnchorRect = LayerLink is null
            ? new Rect(
                MenuPosition.AnchorRect.Left + densityDx,
                MenuPosition.AnchorRect.Top,
                Math.Max(0.0, MenuPosition.AnchorRect.Right - MenuPosition.AnchorRect.Left - densityDx),
                MenuPosition.AnchorRect.Height)
            : default;

        Widget panel = new MenuPanel(
            menuStyle: MenuStyle,
            clipBehavior: ClipBehavior,
            orientation: Anchor.Orientation,
            crossAxisUnconstrained: CrossAxisUnconstrained,
            heightAnimation: HeightAnimation,
            children: MenuChildren);

        Widget content = new TapRegion(
            child: new MouseRegion(
                cursor: cursor,
                child: new FocusScope(
                    focusScopeNode: Anchor.MenuScopeNode,
                    skipTraversal: true,
                    child: new Actions(
                        actions: new Dictionary<Type, FlutterAction>
                        {
                            [typeof(DismissIntent)] = new DismissMenuAction(Anchor.MenuController),
                        },
                        child: new Shortcuts(
                            shortcuts: MenuConstants.TraversalShortcuts,
                            child: new FadeTransition(
                                opacity: FadeAnimation,
                                alwaysIncludeSemantics: true,
                                child: panel))))),
            groupId: MenuPosition.TapRegionGroupId,
            consumeOutsideTaps: Anchor.RootAnchor.MenuController.IsOpen
                                && Anchor.CurrentAnchor.ConsumeOutsideTap,
            onTapOutside: _ => Anchor.MenuController.Close(),
            debugLabel: "MenuAnchor panel");

        MediaQueryData? mediaQuery = MediaQuery.MaybeOf(context);
        List<Rect> avoidBounds = mediaQuery is null
            ? []
            : DisplayFeatureSubScreen.AvoidBounds(mediaQuery);
        Widget layout = new AnimatedBuilder(
            animation: HeightAnimation,
            builder: (_, child) => new CustomSingleChildLayout(
                layoutDelegate: new MenuLayout(
                    anchorRect: layoutAnchorRect,
                    textDirection: textDirection,
                    alignment: alignment,
                    alignmentOffset: AlignmentOffset,
                    menuPosition: MenuPosition.Position,
                    menuPadding: resolvedMenuPadding,
                    orientation: Anchor.Orientation,
                    parentOrientation: Anchor.AnchorParentOrientation,
                    reservedPadding: ReservedPadding,
                    avoidBounds: avoidBounds,
                    heightFactor: HeightAnimation.Value,
                    viewPadding: mediaQuery?.Padding ?? default,
                    viewInsets: mediaQuery?.ViewInsets ?? default),
                child: child!),
            child: content);

        Widget result = new ConstrainedBox(
            BoxConstraints.Loose(MenuPosition.OverlaySize),
            new Theme(theme with { VisualDensity = visualDensity }, layout));

        if (LayerLink is not null)
        {
            result = new CompositedTransformFollower(
                link: LayerLink,
                targetAnchor: Alignment.BottomLeft,
                child: result);
        }

        return result;
    }
}

/// <summary>
/// Dart's `_MouseCursor`: wraps the menu's state-resolving cursor so that it falls back to
/// <see cref="MouseCursor.Uncontrolled"/> when no style in the chain supplies one.
/// </summary>
internal sealed record MenuMouseCursor(
    Func<IReadOnlySet<WidgetState>, MouseCursor?> ResolveCallback) : WidgetStateMouseCursor
{
    public override MouseCursor Resolve(IReadOnlySet<WidgetState> states) =>
        ResolveCallback(states) ?? MouseCursor.Uncontrolled;
}

/// <summary>The Material surface that hosts a menu's children.</summary>
internal sealed class MenuPanel : StatefulWidget
{
    public MenuPanel(
        MenuStyle? menuStyle,
        Clip clipBehavior,
        Axis orientation,
        IReadOnlyList<Widget> children,
        bool crossAxisUnconstrained = true,
        Animation<double>? heightAnimation = null,
        Key? key = null) : base(key)
    {
        MenuStyle = menuStyle;
        ClipBehavior = clipBehavior;
        Orientation = orientation;
        Children = children;
        CrossAxisUnconstrained = crossAxisUnconstrained;
        HeightAnimation = heightAnimation;
    }

    public MenuStyle? MenuStyle { get; }
    public Clip ClipBehavior { get; }
    public Axis Orientation { get; }
    public IReadOnlyList<Widget> Children { get; }
    public bool CrossAxisUnconstrained { get; }
    public Animation<double>? HeightAnimation { get; }

    public override State CreateState() => new MenuPanelState();
}

internal sealed class MenuPanelState : State
{
    private readonly ScrollController _scrollController = new();

    private MenuPanel Current => (MenuPanel)StateWidget;

    public override void Dispose()
    {
        _scrollController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        MenuStyle defaults = Current.Orientation == Axis.Horizontal
            ? new MenuBarDefaultsM3(context)
            : new MenuDefaultsM3(context);
        MenuStyle? themeStyle = Current.Orientation == Axis.Horizontal
            ? MenuBarTheme.Of(context).Style
            : MenuTheme.Of(context).Style;
        MenuStyle style = (Current.MenuStyle ?? new MenuStyle()).Merge(themeStyle).Merge(defaults);
        MaterialState states = MaterialState.None;

        Color? backgroundColor = style.BackgroundColor?.Resolve(states);
        Color? shadowColor = style.ShadowColor?.Resolve(states);
        Color? surfaceTint = style.SurfaceTintColor?.Resolve(states);
        double elevation = style.Elevation?.Resolve(states) ?? 0.0;
        BorderSide? side = style.Side?.Resolve(states);

        // Flutter force-unwraps the resolved shape and always folds `side` into it, so a theme that
        // sets only `side` still decorates the default border.
        OutlinedBorder shape = (style.Shape?.Resolve(states) ?? MenuBarDefaultsM3.DefaultMenuBorder)
            .CopyWith(side);

        VisualDensity density = style.VisualDensity ?? VisualDensity.Standard;
        EdgeInsetsGeometry padding = style.Padding?.Resolve(states) ?? EdgeInsetsGeometry.Zero;
        double horizontalDensityPadding = Math.Max(0.0, density.BaseSizeAdjustment.X);
        EdgeInsetsGeometry resolvedPadding = padding
            .Add(EdgeInsetsGeometry.Symmetric(horizontal: horizontalDensityPadding))
            .Clamp(EdgeInsetsGeometry.Zero, EdgeInsetsGeometry.Infinity);

        IReadOnlyList<Widget> children = Current.Orientation == Axis.Horizontal
            ? Current.Children.Select(child => (Widget)new IntrinsicWidth(child: child)).ToList()
            : Current.Children;

        bool displayScrollbar = MenuAnchorScope.MaybeAnimationStatusOf(context)
                                == Plumix.AnimationStatus.Completed;

        Widget content = new ScrollConfiguration(
            behavior: ScrollConfiguration.Of(context).CopyWith(
                scrollbars: false,
                overscroll: false,
                physics: new ClampingScrollPhysics()),
            child: new PrimaryScrollController(
                controller: _scrollController,
                child: new Scrollbar(
                    thumbVisibility: displayScrollbar,
                    child: new SingleChildScrollView(
                        controller: _scrollController,
                        scrollDirection: Current.Orientation,
                        child: new Flex(
                            direction: Current.Orientation,
                            mainAxisSize: MainAxisSize.Min,
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            textDirection: Directionality.Of(context),
                            children: children)))));

        Widget padded = new Padding(resolvedPadding.Resolve(Directionality.Of(context)), content);
        Widget surfaceChild = Current.HeightAnimation is null
            ? padded
            : new AnimatedBuilder(
                animation: Current.HeightAnimation,
                builder: (_, child) => new Align(
                    alignment: AlignmentDirectional.TopStart,
                    heightFactor: Current.HeightAnimation.Value,
                    widthFactor: 1.0,
                    child: child),
                child: padded);

        Widget surface = new Material(
            type: backgroundColor is null ? MaterialType.Transparency : MaterialType.Canvas,
            color: backgroundColor,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTint,
            elevation: elevation,
            shape: shape,
            clipBehavior: Current.ClipBehavior,
            child: surfaceChild);

        Widget intrinsic = Current.Orientation == Axis.Horizontal
            ? new IntrinsicHeight(child: surface)
            : new IntrinsicWidth(child: surface);

        if (Current.CrossAxisUnconstrained)
        {
            intrinsic = new UnconstrainedBox(
                child: intrinsic,
                constrainedAxis: Current.Orientation,
                clipBehavior: Clip.HardEdge,
                alignment: AlignmentDirectional.CenterStart);
        }

        return new ConstrainedBox(
            MenuAnchorState.ResolveMenuConstraints(style, states, density),
            intrinsic);
    }
}

/// <summary>Flutter's `_MenuBarDefaultsM3`: the Material 3 defaults for a <see cref="MenuBar"/> strip.</summary>
internal sealed class MenuBarDefaultsM3 : MenuStyle
{
    internal static readonly RoundedRectangleBorder DefaultMenuBorder =
        new(borderRadius: BorderRadius.Circular(4.0));

    private readonly BuildContext _context;
    private ColorScheme? _colorScheme;

    public MenuBarDefaultsM3(BuildContext context) : base(
        elevation: MaterialStateProperty<double?>.All(3.0),
        shape: MaterialStateProperty<OutlinedBorder?>.All(DefaultMenuBorder),
        alignment: AlignmentDirectional.BottomStart)
    {
        _context = context;
    }

    private ColorScheme ColorRoles => _colorScheme ??= Theme.Of(_context).ColorScheme;

    public override MaterialStateProperty<Color?>? BackgroundColor =>
        MaterialStateProperty<Color?>.All(ColorRoles.SurfaceContainer);

    public override MaterialStateProperty<Color?>? ShadowColor =>
        MaterialStateProperty<Color?>.All(ColorRoles.Shadow);

    public override MaterialStateProperty<Color?>? SurfaceTintColor =>
        MaterialStateProperty<Color?>.All(Colors.Transparent);

    public override MaterialStateProperty<EdgeInsetsGeometry?>? Padding =>
        MaterialStateProperty<EdgeInsetsGeometry?>.All(
            EdgeInsetsGeometry.DirectionalSymmetric(
                horizontal: MenuConstants.TopLevelMenuHorizontalMinPadding));

    public override VisualDensity? VisualDensity => Theme.Of(_context).VisualDensity;
}

/// <summary>Flutter's `_MenuDefaultsM3`: the Material 3 defaults for a dropped-down menu panel.</summary>
internal sealed class MenuDefaultsM3 : MenuStyle
{
    private readonly BuildContext _context;
    private ColorScheme? _colorScheme;

    public MenuDefaultsM3(BuildContext context) : base(
        elevation: MaterialStateProperty<double?>.All(3.0),
        shape: MaterialStateProperty<OutlinedBorder?>.All(MenuBarDefaultsM3.DefaultMenuBorder),
        alignment: AlignmentDirectional.TopEnd)
    {
        _context = context;
    }

    private ColorScheme ColorRoles => _colorScheme ??= Theme.Of(_context).ColorScheme;

    public override MaterialStateProperty<Color?>? BackgroundColor =>
        MaterialStateProperty<Color?>.All(ColorRoles.SurfaceContainer);

    public override MaterialStateProperty<Color?>? SurfaceTintColor =>
        MaterialStateProperty<Color?>.All(Colors.Transparent);

    public override MaterialStateProperty<Color?>? ShadowColor =>
        MaterialStateProperty<Color?>.All(ColorRoles.Shadow);

    public override MaterialStateProperty<EdgeInsetsGeometry?>? Padding =>
        MaterialStateProperty<EdgeInsetsGeometry?>.All(
            EdgeInsetsGeometry.DirectionalSymmetric(
                vertical: MenuConstants.MenuVerticalMinPadding));

    public override VisualDensity? VisualDensity => Theme.Of(_context).VisualDensity;
}

/// <summary>Flutter's `_LocalizedShortcutLabeler`: renders a menu item's shortcut as label text.</summary>
internal sealed class LocalizedShortcutLabeler
{
    /// <summary>Flutter's `_shortcutGraphicEquivalents`.</summary>
    private static readonly IReadOnlyDictionary<LogicalKeyboardKey, string> GraphicEquivalents =
        new Dictionary<LogicalKeyboardKey, string>
        {
            [LogicalKeyboardKey.ArrowLeft] = "←",
            [LogicalKeyboardKey.ArrowRight] = "→",
            [LogicalKeyboardKey.ArrowUp] = "↑",
            [LogicalKeyboardKey.ArrowDown] = "↓",
            [LogicalKeyboardKey.Enter] = "↵",
        };

    private static LocalizedShortcutLabeler? _instance;

    private readonly Dictionary<MaterialLocalizations, Dictionary<LogicalKeyboardKey, string>>
        _cachedShortcutKeys = [];

    private LocalizedShortcutLabeler()
    {
    }

    public static LocalizedShortcutLabeler Instance => _instance ??= new LocalizedShortcutLabeler();

    /// <summary>Flutter's `_usesSymbolicModifiers`: Apple platforms use ⌃⌥⇧⌘ joined by a space.</summary>
    internal static bool UsesSymbolicModifiers(TargetPlatform platform) =>
        platform is TargetPlatform.IOS or TargetPlatform.MacOS;

    public string GetShortcutLabel(
        IMenuSerializableShortcut shortcut,
        MaterialLocalizations localizations,
        TargetPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        ShortcutSerialization serialized = shortcut.SerializeForMenu();
        bool symbolic = UsesSymbolicModifiers(platform);
        string keySeparator = symbolic ? " " : "+";
        var parts = new List<string>();

        if (serialized.Trigger is not null)
        {
            AddModifiers(parts, serialized, localizations, platform, symbolic, includeShift: true);
            LogicalKeyboardKey trigger = serialized.Trigger;
            string? shortcutTrigger = GraphicEquivalents.GetValueOrDefault(trigger);
            if (shortcutTrigger == null)
            {
                shortcutTrigger = GetLocalizedName(trigger, localizations);
                if (shortcutTrigger == null && (trigger.KeyId & LogicalKeyboardKey.PlaneMask) == 0x0)
                {
                    // A Unicode-character-producing key is labelled with the character itself.
                    shortcutTrigger = char
                        .ConvertFromUtf32((int)(trigger.KeyId & LogicalKeyboardKey.ValueMask))
                        .ToUpperInvariant();
                }

                shortcutTrigger ??= trigger.KeyLabel;
            }
            if (shortcutTrigger.Length > 0)
            {
                parts.Add(shortcutTrigger);
            }

            return string.Join(keySeparator, parts);
        }

        if (serialized.Character is not null)
        {
            // A character encodes its own shift state, so shift is never emitted here.
            AddModifiers(parts, serialized, localizations, platform, symbolic, includeShift: false);
            parts.Add(serialized.Character);
            return string.Join(keySeparator, parts);
        }

        throw new NotSupportedException(
            "Shortcut labels for shortcut activators that do not implement IMenuSerializableShortcut "
            + "(that is, activators other than SingleActivator or CharacterActivator) are not supported.");
    }

    private static void AddModifiers(
        List<string> parts,
        ShortcutSerialization serialized,
        MaterialLocalizations localizations,
        TargetPlatform platform,
        bool symbolic,
        bool includeShift)
    {
        if (symbolic)
        {
            // Apple ordering: control, alt, shift, meta — ⌘ always last.
            AddModifier(parts, serialized.Control, "Control", localizations, platform);
            AddModifier(parts, serialized.Alt, "Alt", localizations, platform);
            if (includeShift)
            {
                AddModifier(parts, serialized.Shift, "Shift", localizations, platform);
            }

            AddModifier(parts, serialized.Meta, "Meta", localizations, platform);
            return;
        }

        // Non-Apple ordering, matching Flutter's LogicalKeySet order.
        AddModifier(parts, serialized.Alt, "Alt", localizations, platform);
        AddModifier(parts, serialized.Control, "Control", localizations, platform);
        AddModifier(parts, serialized.Meta, "Meta", localizations, platform);
        if (includeShift)
        {
            AddModifier(parts, serialized.Shift, "Shift", localizations, platform);
        }
    }

    private static void AddModifier(
        List<string> parts,
        bool? pressed,
        string modifier,
        MaterialLocalizations localizations,
        TargetPlatform platform)
    {
        if (pressed == true)
        {
            parts.Add(GetModifierLabel(modifier, localizations, platform));
        }
    }

    /// <summary>Flutter's `_getModifierLabel`.</summary>
    internal static string GetModifierLabel(
        string modifier,
        MaterialLocalizations localizations,
        TargetPlatform platform)
    {
        bool apple = UsesSymbolicModifiers(platform);
        return modifier switch
        {
            "Meta" => apple
                ? "⌘"
                : platform == TargetPlatform.Windows
                    ? localizations.KeyboardKeyMetaWindows
                    : localizations.KeyboardKeyMeta,
            "Alt" => apple ? "⌥" : localizations.KeyboardKeyAlt,
            "Control" => apple ? "⌃" : localizations.KeyboardKeyControl,
            "Shift" => apple ? "⇧" : localizations.KeyboardKeyShift,
            _ => throw new ArgumentException($"Keyboard key {modifier} is not a modifier.", nameof(modifier))
        };
    }

    /// <summary>Flutter's `_getLocalizedName`; returns null for keys without a localized name.</summary>
    private string? GetLocalizedName(LogicalKeyboardKey key, MaterialLocalizations localizations)
    {
        if (!_cachedShortcutKeys.TryGetValue(localizations, out Dictionary<LogicalKeyboardKey, string>? names))
        {
            names = new Dictionary<LogicalKeyboardKey, string>
            {
                [LogicalKeyboardKey.AltGraph] = localizations.KeyboardKeyAltGraph,
                [LogicalKeyboardKey.Backspace] = localizations.KeyboardKeyBackspace,
                [LogicalKeyboardKey.CapsLock] = localizations.KeyboardKeyCapsLock,
                [LogicalKeyboardKey.ChannelDown] = localizations.KeyboardKeyChannelDown,
                [LogicalKeyboardKey.ChannelUp] = localizations.KeyboardKeyChannelUp,
                [LogicalKeyboardKey.Delete] = localizations.KeyboardKeyDelete,
                [LogicalKeyboardKey.Eject] = localizations.KeyboardKeyEject,
                [LogicalKeyboardKey.End] = localizations.KeyboardKeyEnd,
                [LogicalKeyboardKey.Escape] = localizations.KeyboardKeyEscape,
                [LogicalKeyboardKey.Fn] = localizations.KeyboardKeyFn,
                [LogicalKeyboardKey.Home] = localizations.KeyboardKeyHome,
                [LogicalKeyboardKey.Insert] = localizations.KeyboardKeyInsert,
                [LogicalKeyboardKey.NumLock] = localizations.KeyboardKeyNumLock,
                [LogicalKeyboardKey.Numpad1] = localizations.KeyboardKeyNumpad1,
                [LogicalKeyboardKey.Numpad2] = localizations.KeyboardKeyNumpad2,
                [LogicalKeyboardKey.Numpad3] = localizations.KeyboardKeyNumpad3,
                [LogicalKeyboardKey.Numpad4] = localizations.KeyboardKeyNumpad4,
                [LogicalKeyboardKey.Numpad5] = localizations.KeyboardKeyNumpad5,
                [LogicalKeyboardKey.Numpad6] = localizations.KeyboardKeyNumpad6,
                [LogicalKeyboardKey.Numpad7] = localizations.KeyboardKeyNumpad7,
                [LogicalKeyboardKey.Numpad8] = localizations.KeyboardKeyNumpad8,
                [LogicalKeyboardKey.Numpad9] = localizations.KeyboardKeyNumpad9,
                [LogicalKeyboardKey.Numpad0] = localizations.KeyboardKeyNumpad0,
                [LogicalKeyboardKey.NumpadAdd] = localizations.KeyboardKeyNumpadAdd,
                [LogicalKeyboardKey.NumpadComma] = localizations.KeyboardKeyNumpadComma,
                [LogicalKeyboardKey.NumpadDecimal] = localizations.KeyboardKeyNumpadDecimal,
                [LogicalKeyboardKey.NumpadDivide] = localizations.KeyboardKeyNumpadDivide,
                [LogicalKeyboardKey.NumpadEnter] = localizations.KeyboardKeyNumpadEnter,
                [LogicalKeyboardKey.NumpadEqual] = localizations.KeyboardKeyNumpadEqual,
                [LogicalKeyboardKey.NumpadMultiply] = localizations.KeyboardKeyNumpadMultiply,
                [LogicalKeyboardKey.NumpadParenLeft] = localizations.KeyboardKeyNumpadParenLeft,
                [LogicalKeyboardKey.NumpadParenRight] = localizations.KeyboardKeyNumpadParenRight,
                [LogicalKeyboardKey.NumpadSubtract] = localizations.KeyboardKeyNumpadSubtract,
                [LogicalKeyboardKey.PageDown] = localizations.KeyboardKeyPageDown,
                [LogicalKeyboardKey.PageUp] = localizations.KeyboardKeyPageUp,
                [LogicalKeyboardKey.Power] = localizations.KeyboardKeyPower,
                [LogicalKeyboardKey.PowerOff] = localizations.KeyboardKeyPowerOff,
                [LogicalKeyboardKey.PrintScreen] = localizations.KeyboardKeyPrintScreen,
                [LogicalKeyboardKey.ScrollLock] = localizations.KeyboardKeyScrollLock,
                [LogicalKeyboardKey.Select] = localizations.KeyboardKeySelect,
                [LogicalKeyboardKey.Space] = localizations.KeyboardKeySpace,
            };
            _cachedShortcutKeys[localizations] = names;
        }

        return names.GetValueOrDefault(key);
    }
}

/// <summary>Flutter's `_MenuItemLabel`: the leading/label/shortcut/submenu row of a menu button.</summary>
internal sealed class MenuItemLabel : StatelessWidget
{
    public MenuItemLabel(
        bool hasSubmenu,
        bool showDecoration = true,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        IMenuSerializableShortcut? shortcut = null,
        Widget? submenuIcon = null,
        string? semanticsLabel = null,
        Axis overflowAxis = Axis.Vertical,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        HasSubmenu = hasSubmenu;
        ShowDecoration = showDecoration;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        Shortcut = shortcut;
        SubmenuIcon = submenuIcon;
        SemanticsLabel = semanticsLabel;
        OverflowAxis = overflowAxis;
        Child = child;
    }

    public bool HasSubmenu { get; }
    public bool ShowDecoration { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public IMenuSerializableShortcut? Shortcut { get; }
    public Widget? SubmenuIcon { get; }
    public string? SemanticsLabel { get; }
    public Axis OverflowAxis { get; }
    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        VisualDensity density = Theme.Of(context).VisualDensity;
        TextDirection textDirection = Directionality.Of(context);
        double horizontalPadding = Math.Max(
            MenuConstants.LabelItemMinSpacing,
            MenuConstants.LabelItemDefaultSpacing + (density.Horizontal * 2));
        Thickness leadingPadding = EdgeInsetsGeometry
            .DirectionalOnly(start: horizontalPadding)
            .Resolve(textDirection);

        Widget leadings = BuildLeadings(leadingPadding);
        var children = new List<Widget> { leadings };
        if (TrailingIcon is not null)
        {
            children.Add(new Padding(leadingPadding, TrailingIcon));
        }

        if (ShowDecoration && Shortcut is not null)
        {
            children.Add(new Padding(
                leadingPadding,
                new Text(LocalizedShortcutLabeler.Instance.GetShortcutLabel(
                    Shortcut,
                    MaterialLocalizations.Of(context),
                    Theme.Of(context).Platform))));
        }

        if (ShowDecoration && HasSubmenu && SubmenuIcon is not null)
        {
            children.Add(new Padding(leadingPadding, SubmenuIcon));
        }

        Widget result = new Row(
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            textDirection: textDirection,
            children: children);

        if (SemanticsLabel is not null)
        {
            result = new Semantics(
                label: SemanticsLabel,
                child: new ExcludeSemantics(result));
        }

        return result;
    }

    private Widget BuildLeadings(Thickness leadingPadding)
    {
        var inner = new List<Widget>();
        if (LeadingIcon is not null)
        {
            inner.Add(LeadingIcon);
        }

        if (OverflowAxis == Axis.Vertical)
        {
            if (Child is not null)
            {
                inner.Add(new Expanded(child: new ClipRect(
                    child: new Padding(LeadingIcon is not null ? leadingPadding : default, Child))));
            }

            return new Expanded(child: new ClipRect(
                child: new Row(mainAxisSize: MainAxisSize.Min, children: inner)));
        }

        if (Child is not null)
        {
            inner.Add(new Padding(LeadingIcon is not null ? leadingPadding : default, Child));
        }

        return new Row(mainAxisSize: MainAxisSize.Min, children: inner);
    }
}

/// <summary>Flutter's `_MenuButtonDefaultsM3`: the Material 3 defaults for menu buttons.</summary>
internal static class MenuButtonDefaults
{
    public static ButtonStyle M3(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        ColorScheme colors = theme.ColorScheme;
        return new ButtonStyle(
            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            Elevation: MaterialStateProperty<double?>.All(0.0),
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return colors.OnSurface.WithOpacity(0.38);
                }

                if (states.HasFlag(MaterialState.Pressed))
                {
                    return colors.OnSurface;
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return colors.OnSurface;
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return colors.OnSurface;
                }

                return colors.OnSurface;
            }),
            IconColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return colors.OnSurface.WithOpacity(0.38);
                }

                if (states.HasFlag(MaterialState.Pressed))
                {
                    return colors.OnSurfaceVariant;
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return colors.OnSurfaceVariant;
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return colors.OnSurfaceVariant;
                }

                return colors.OnSurfaceVariant;
            }),
            IconSize: MaterialStateProperty<double?>.All(24.0),
            MaximumSize: MaterialStateProperty<Size?>.All(
                new Size(double.PositiveInfinity, double.PositiveInfinity)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(64.0, 48.0)),
            MouseCursor: MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
                WidgetStateMouseCursor.AdaptiveClickable.Resolve(states)),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Pressed))
                {
                    return colors.OnSurface.WithOpacity(0.1);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return colors.OnSurface.WithOpacity(0.08);
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return colors.OnSurface.WithOpacity(0.1);
                }

                return Colors.Transparent;
            }),
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(ScaledPadding(context)),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder()),
            SplashFactory: theme.SplashFactory,
            TapTargetSize: theme.MaterialTapTargetSize,
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            VisualDensity: theme.VisualDensity,
            Alignment: AlignmentDirectional.CenterStart,
            AnimationDuration: MenuConstants.ThemeChangeDuration,
            EnableFeedback: true);
    }

    /// <summary>Flutter's `_scaledPadding`: horizontal-only padding that shrinks with text scale.</summary>
    internal static Thickness ScaledPadding(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        VisualDensity density = theme.VisualDensity;
        if (density.Horizontal > 0)
        {
            density = new VisualDensity(vertical: density.Vertical);
        }

        double fontSizeRatio = ButtonStyleButton.EffectiveTextScale(
            context,
            theme.TextTheme.LabelLarge.FontSize);
        double densityDx = density.BaseSizeAdjustment.X;
        return ButtonStyleButton.ScaledPadding(
            new Thickness(
                Math.Max(MenuConstants.MenuViewPadding, MenuConstants.LabelItemDefaultSpacing + densityDx),
                0.0),
            new Thickness(Math.Max(MenuConstants.MenuViewPadding, 8.0 + densityDx), 0.0),
            new Thickness(MenuConstants.MenuViewPadding, 0.0),
            fontSizeRatio).Resolve(Directionality.Of(context));
    }
}

public sealed class MenuItemButton : StatefulWidget
{
    public MenuItemButton(
        Widget? child = null,
        Action? onPressed = null,
        Action<bool>? onHover = null,
        bool requestFocusOnHover = true,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        IMenuSerializableShortcut? shortcut = null,
        string? semanticsLabel = null,
        ButtonStyle? style = null,
        MaterialStatesController? statesController = null,
        Clip clipBehavior = Clip.None,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        bool closeOnActivate = true,
        Axis overflowAxis = Axis.Horizontal,
        Key? key = null) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        OnHover = onHover;
        RequestFocusOnHover = requestFocusOnHover;
        OnFocusChange = onFocusChange;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Shortcut = shortcut;
        SemanticsLabel = semanticsLabel;
        Style = style;
        StatesController = statesController;
        ClipBehavior = clipBehavior;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        CloseOnActivate = closeOnActivate;
        OverflowAxis = overflowAxis;
    }

    public Widget? Child { get; }
    public Action? OnPressed { get; }
    public Action<bool>? OnHover { get; }
    public bool RequestFocusOnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }

    /// <summary>
    /// The shortcut label shown at the end of the item. As in Flutter, this is display-only: the
    /// activator is never registered, so the owner must handle the key binding itself.
    /// </summary>
    public IMenuSerializableShortcut? Shortcut { get; }

    public string? SemanticsLabel { get; }
    public ButtonStyle? Style { get; }
    public MaterialStatesController? StatesController { get; }
    public Clip ClipBehavior { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public bool CloseOnActivate { get; }
    public Axis OverflowAxis { get; }
    public bool Enabled => OnPressed is not null;

    /// <summary>Flutter's `MenuItemButton.defaultStyleOf`.</summary>
    public ButtonStyle DefaultStyleOf(BuildContext context) => MenuButtonDefaults.M3(context);

    /// <summary>Flutter's `MenuItemButton.themeStyleOf`.</summary>
    public ButtonStyle? ThemeStyleOf(BuildContext context) => MenuButtonTheme.Of(context).Style;

    public override State CreateState() => new MenuItemButtonState();
}

public sealed class MenuItemButtonState : State
{
    private readonly FocusNode _internalFocusNode = new();
    private MenuAnchorState? _anchor;
    private bool _isHovered;

    private MenuItemButton Current => (MenuItemButton)StateWidget;

    internal FocusNode ButtonFocusNode => Current.FocusNode ?? _internalFocusNode;

    public override void InitState()
    {
        ButtonFocusNode.AddListener(HandleFocusChanged);
    }

    public override void DidChangeDependencies()
    {
        _anchor = MenuAnchorScope.MaybeOf(Context)?.State;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (MenuItemButton)oldWidget;
        if (!ReferenceEquals(previous.FocusNode, Current.FocusNode))
        {
            (previous.FocusNode ?? _internalFocusNode).RemoveListener(HandleFocusChanged);
            ButtonFocusNode.AddListener(HandleFocusChanged);
        }
    }

    public override void Dispose()
    {
        ButtonFocusNode.RemoveListener(HandleFocusChanged);
        _internalFocusNode.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        ButtonStyle mergedStyle = MenuButtonStyleComposer.ComposeStyles(
            defaults: Current.DefaultStyleOf(context),
            themeStyle: Current.ThemeStyleOf(context),
            widgetStyle: Current.Style,
            legacyOverrides: null);

        Widget result = new TextButton(
            child: new MenuItemLabel(
                hasSubmenu: false,
                leadingIcon: Current.LeadingIcon,
                trailingIcon: Current.TrailingIcon,
                shortcut: Current.Shortcut,
                semanticsLabel: Current.SemanticsLabel,
                overflowAxis: _anchor?.Orientation ?? Current.OverflowAxis,
                child: Current.Child),
            onPressed: Current.Enabled ? HandleSelect : null,
            onFocusChange: Current.Enabled ? Current.OnFocusChange : null,
            focusNode: ButtonFocusNode,
            style: mergedStyle,
            autofocus: Current.Enabled && Current.Autofocus,
            statesController: Current.StatesController,
            clipBehavior: Current.ClipBehavior,
            isSemanticButton: OperatingSystem.IsBrowser() ? true : null);

        if (Current.OnHover is not null || Current.RequestFocusOnHover)
        {
            // Flutter deliberately uses onHover rather than onEnter here: onEnter also fires when a
            // button scrolls under a stationary pointer, which would steal focus during scrolling.
            result = new MouseRegion(
                onHover: _ => HandlePointerHover(),
                onExit: _ => HandlePointerExit(),
                child: result);
        }

        if (Current.Enabled && MenuAcceleratorLabel.PlatformSupportsAccelerators(context))
        {
            result = new MenuAcceleratorCallbackBinding(
                child: result,
                onInvoke: HandleSelect);
        }

        return new MergeSemantics(result);
    }

    private void HandleSelect()
    {
        if (Current.CloseOnActivate)
        {
            _anchor?.RootAnchor.MenuController.Close();
        }

        // Delay the callback until the menu has finished closing, as Flutter does.
        Scheduler.AddPostFrameCallback(_ => Current.OnPressed?.Invoke());
    }

    private void HandlePointerHover()
    {
        if (_isHovered)
        {
            return;
        }

        _isHovered = true;
        Current.OnHover?.Invoke(true);

        // Flutter requests focus without testing `enabled`, because a disabled button builds no
        // `Focus` and its unregistered node cannot take primary focus. Plumix's `FocusNode` can
        // focus while unattached, so the disabled case is excluded explicitly to keep the same
        // observable behavior.
        if (!Current.RequestFocusOnHover || !Current.Enabled)
        {
            return;
        }

        ButtonFocusNode.RequestFocus();

        // Without invalidating the focus policy, switching to directional focus may not originate
        // at this node.
        FocusTraversalGroup.MaybeOf(Context)?.InvalidateScopeData(
            FocusScope.MaybeOf(Context) ?? FocusManager.Instance.RootScope);
    }

    private void HandlePointerExit()
    {
        if (!_isHovered)
        {
            return;
        }

        _isHovered = false;
        Current.OnHover?.Invoke(false);
    }

    private void HandleFocusChanged()
    {
        if (!ButtonFocusNode.HasPrimaryFocus && Mounted)
        {
            // Close any child menus of this button's menu.
            MenuController.MaybeOf(Context)?.CloseChildren();
        }
    }
}

/// <summary>A menu item that combines a <see cref="Checkbox"/> with a <see cref="MenuItemButton"/>.</summary>
/// <remarks>Dart parity source: material_ui/lib/src/menu_anchor.dart.</remarks>
public sealed class CheckboxMenuButton : StatelessWidget
{
    public CheckboxMenuButton(
        bool? value,
        Action<bool?>? onChanged,
        Widget? child,
        bool tristate = false,
        bool isError = false,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        IMenuSerializableShortcut? shortcut = null,
        ButtonStyle? style = null,
        MaterialStatesController? statesController = null,
        Clip clipBehavior = Clip.None,
        Widget? trailingIcon = null,
        bool closeOnActivate = true,
        Key? key = null) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException(
                "CheckboxMenuButton value cannot be null when tristate is false.",
                nameof(value));
        }

        Value = value;
        OnChanged = onChanged;
        Child = child;
        Tristate = tristate;
        IsError = isError;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        FocusNode = focusNode;
        Shortcut = shortcut;
        Style = style;
        StatesController = statesController;
        ClipBehavior = clipBehavior;
        TrailingIcon = trailingIcon;
        CloseOnActivate = closeOnActivate;
    }

    public bool? Value { get; }
    public Action<bool?>? OnChanged { get; }
    public Widget? Child { get; }
    public bool Tristate { get; }
    public bool IsError { get; }
    public Action<bool>? OnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public FocusNode? FocusNode { get; }
    public IMenuSerializableShortcut? Shortcut { get; }
    public ButtonStyle? Style { get; }
    public MaterialStatesController? StatesController { get; }
    public Clip ClipBehavior { get; }
    public Widget? TrailingIcon { get; }
    public bool CloseOnActivate { get; }
    public bool Enabled => OnChanged is not null;

    public override Widget Build(BuildContext context)
    {
        return new MenuItemButton(
            child: Child,
            onPressed: OnChanged is null ? null : HandleChanged,
            onHover: OnHover,
            onFocusChange: OnFocusChange,
            focusNode: FocusNode,
            shortcut: Shortcut,
            style: Style,
            statesController: StatesController,
            clipBehavior: ClipBehavior,
            leadingIcon: new ExcludeFocus(
                new IgnorePointer(
                    child: new ConstrainedBox(
                        new BoxConstraints(MaxWidth: Checkbox.Width, MaxHeight: Checkbox.Width),
                        new Checkbox(
                            value: Value,
                            onChanged: OnChanged,
                            tristate: Tristate,
                            isError: IsError)))),
            trailingIcon: TrailingIcon,
            closeOnActivate: CloseOnActivate,
            key: Key);
    }

    private void HandleChanged()
    {
        bool? nextValue = Value switch
        {
            false => true,
            true => Tristate ? null : false,
            null => false,
        };
        OnChanged!(nextValue);
    }
}

/// <summary>A menu item that combines a <see cref="Radio{T}"/> with a <see cref="MenuItemButton"/>.</summary>
/// <remarks>Dart parity source: material_ui/lib/src/menu_anchor.dart.</remarks>
public sealed class RadioMenuButton<T> : StatelessWidget
{
    public RadioMenuButton(
        T value,
        T? groupValue,
        Action<T?>? onChanged,
        Widget? child,
        bool toggleable = false,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        IMenuSerializableShortcut? shortcut = null,
        ButtonStyle? style = null,
        MaterialStatesController? statesController = null,
        Clip clipBehavior = Clip.None,
        Widget? trailingIcon = null,
        bool closeOnActivate = true,
        Key? key = null) : base(key)
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        Child = child;
        Toggleable = toggleable;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        FocusNode = focusNode;
        Shortcut = shortcut;
        Style = style;
        StatesController = statesController;
        ClipBehavior = clipBehavior;
        TrailingIcon = trailingIcon;
        CloseOnActivate = closeOnActivate;
    }

    public T Value { get; }
    public T? GroupValue { get; }
    public Action<T?>? OnChanged { get; }
    public Widget? Child { get; }
    public bool Toggleable { get; }
    public Action<bool>? OnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public FocusNode? FocusNode { get; }
    public IMenuSerializableShortcut? Shortcut { get; }
    public ButtonStyle? Style { get; }
    public MaterialStatesController? StatesController { get; }
    public Clip ClipBehavior { get; }
    public Widget? TrailingIcon { get; }
    public bool CloseOnActivate { get; }
    public bool Enabled => OnChanged is not null;

    public override Widget Build(BuildContext context)
    {
        return new MenuItemButton(
            child: Child,
            onPressed: OnChanged is null ? null : HandleChanged,
            onHover: OnHover,
            onFocusChange: OnFocusChange,
            focusNode: FocusNode,
            shortcut: Shortcut,
            style: Style,
            statesController: StatesController,
            clipBehavior: ClipBehavior,
            leadingIcon: new ExcludeFocus(
                new IgnorePointer(
                    child: new ConstrainedBox(
                        new BoxConstraints(MaxWidth: Checkbox.Width, MaxHeight: Checkbox.Width),
                        new Radio<T>(
                            value: Value,
                            groupValue: GroupValue,
                            onChanged: OnChanged,
                            toggleable: Toggleable)))),
            trailingIcon: TrailingIcon,
            closeOnActivate: CloseOnActivate,
            key: Key);
    }

    private void HandleChanged()
    {
        bool selected = EqualityComparer<T?>.Default.Equals(GroupValue, Value);
        OnChanged!(Toggleable && selected ? default : Value);
    }
}

/// <summary>A horizontal collection of top-level <see cref="SubmenuButton"/> controls.</summary>
/// <remarks>Dart parity source: material_ui/lib/src/menu_anchor.dart (MenuBar).</remarks>
public sealed class MenuBar : StatelessWidget
{
    public MenuBar(
        IReadOnlyList<Widget> children,
        MenuStyle? style = null,
        Clip clipBehavior = Clip.None,
        MenuController? controller = null,
        Key? key = null) : base(key)
    {
        Children = children ?? throw new ArgumentNullException(nameof(children));
        Style = style;
        ClipBehavior = clipBehavior;
        Controller = controller;
    }

    public IReadOnlyList<Widget> Children { get; }
    public MenuStyle? Style { get; }
    public Clip ClipBehavior { get; }
    public MenuController? Controller { get; }

    public override Widget Build(BuildContext context) => new MenuBarAnchor(
        menuChildren: Children,
        controller: Controller,
        clipBehavior: ClipBehavior,
        style: Style);
}

/// <summary>The anchor a <see cref="MenuBar"/> builds; its panel is the bar itself.</summary>
internal sealed class MenuBarAnchor : MenuAnchor
{
    public MenuBarAnchor(
        IReadOnlyList<Widget> menuChildren,
        MenuController? controller = null,
        Clip clipBehavior = Clip.None,
        MenuStyle? style = null,
        Key? key = null) : base(
        menuChildren: menuChildren,
        controller: controller,
        clipBehavior: clipBehavior,
        style: style,
        key: key)
    {
    }

    public override State CreateState() => new MenuBarAnchorState();
}

internal sealed class MenuBarAnchorState : MenuAnchorState
{
    internal override Axis Orientation => Axis.Horizontal;

    public override Widget Build(BuildContext context)
    {
        return new MenuAnchorScope(
            state: this,
            animationStatus: AnimationStatus,
            child: new RawMenuAnchorGroup(
                controller: MenuController,
                child: new Builder(groupContext =>
                {
                    bool isOpen = MenuController.MaybeIsOpenOf(groupContext) ?? false;
                    return new FocusScope(
                        focusScopeNode: MenuScopeNode,
                        skipTraversal: !isOpen,
                        canRequestFocus: isOpen,
                        child: new ExcludeFocus(
                            excluding: !isOpen,
                            child: new Actions(
                                actions: new Dictionary<Type, FlutterAction>
                                {
                                    [typeof(DismissIntent)] = new DismissMenuAction(MenuController),
                                },
                                child: new Shortcuts(
                                    shortcuts: MenuConstants.TraversalShortcuts,
                                    child: new MenuPanel(
                                        menuStyle: CurrentAnchor.Style,
                                        clipBehavior: CurrentAnchor.ClipBehavior,
                                        orientation: Axis.Horizontal,
                                        children: CurrentAnchor.MenuChildren)))));
                })));
    }
}

/// <summary>A menu button that opens a nested vertical menu.</summary>
/// <remarks>Dart parity source: material_ui/lib/src/menu_anchor.dart (SubmenuButton).</remarks>
public sealed class SubmenuButton : StatefulWidget
{
    public SubmenuButton(
        IReadOnlyList<Widget> menuChildren,
        Widget? child,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        Action? onOpen = null,
        Action? onClose = null,
        MenuController? controller = null,
        ButtonStyle? style = null,
        MenuStyle? menuStyle = null,
        Vector? alignmentOffset = null,
        Clip clipBehavior = Clip.HardEdge,
        FocusNode? focusNode = null,
        MaterialStatesController? statesController = null,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        MaterialStateProperty<Widget?>? submenuIcon = null,
        bool useRootOverlay = false,
        TimeSpan? hoverOpenDelay = null,
        bool animated = false,
        Action<AnimationStatus>? onAnimationStatusChanged = null,
        Key? key = null) : base(key)
    {
        MenuChildren = menuChildren ?? throw new ArgumentNullException(nameof(menuChildren));
        Child = child;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        OnOpen = onOpen;
        OnClose = onClose;
        Controller = controller;
        Style = style;
        MenuStyle = menuStyle;
        AlignmentOffset = alignmentOffset;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        StatesController = statesController;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        SubmenuIcon = submenuIcon;
        UseRootOverlay = useRootOverlay;
        HoverOpenDelay = hoverOpenDelay ?? TimeSpan.Zero;
        if (HoverOpenDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(hoverOpenDelay));
        }

        Animated = animated;
        OnAnimationStatusChanged = onAnimationStatusChanged;
    }

    public IReadOnlyList<Widget> MenuChildren { get; }
    public Widget? Child { get; }
    public Action<bool>? OnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public Action? OnOpen { get; }
    public Action? OnClose { get; }
    public MenuController? Controller { get; }
    public ButtonStyle? Style { get; }
    public MenuStyle? MenuStyle { get; }
    public Vector? AlignmentOffset { get; }
    public Clip ClipBehavior { get; }
    public FocusNode? FocusNode { get; }
    public MaterialStatesController? StatesController { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public MaterialStateProperty<Widget?>? SubmenuIcon { get; }
    public bool UseRootOverlay { get; }
    public TimeSpan HoverOpenDelay { get; }
    public bool Animated { get; }
    public Action<AnimationStatus>? OnAnimationStatusChanged { get; }
    public bool Enabled => MenuChildren.Count > 0;

    /// <summary>Flutter's `SubmenuButton.defaultStyleOf`.</summary>
    public ButtonStyle DefaultStyleOf(BuildContext context) => MenuButtonDefaults.M3(context);

    /// <summary>Flutter's `SubmenuButton.themeStyleOf`.</summary>
    public ButtonStyle? ThemeStyleOf(BuildContext context) => MenuButtonTheme.Of(context).Style;

    public override State CreateState() => new SubmenuButtonState();
}

public sealed class SubmenuButtonState : State
{
    private readonly FocusNode _internalFocusNode = new();
    private MenuController? _internalController;
    private MenuAnchorState? _parentAnchor;
    private CancellationTokenSource? _hoverOpenCancellation;
    private AnimationStatus _animationStatus = AnimationStatus.Dismissed;
    private bool _isHovered;
    private bool _isOpenOnFocusEnabled = true;
    private bool _waitingToFocusMenu;

    private SubmenuButton Current => (SubmenuButton)StateWidget;

    internal MenuController Controller => Current.Controller ?? _internalController!;

    internal FocusNode ButtonFocusNode => Current.FocusNode ?? _internalFocusNode;

    internal MenuAnchorState? ParentAnchor => _parentAnchor;

    internal MenuAnchorState? AnchorState { get; private set; }

    public override void InitState()
    {
        if (Current.Controller is null)
        {
            _internalController = new MenuController();
        }
    }

    public override void DidChangeDependencies()
    {
        _parentAnchor = MenuAnchorScope.MaybeOf(Context)?.State;
        ValidateHoverOpenDelay();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (SubmenuButton)oldWidget;
        if (!ReferenceEquals(previous.Controller, Current.Controller))
        {
            _internalController = Current.Controller is null ? new MenuController() : null;
        }

        ValidateHoverOpenDelay();
    }

    public override void Dispose()
    {
        CancelDelayedOpen();
        _internalFocusNode.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        Axis parentOrientation = _parentAnchor?.Orientation ?? Axis.Horizontal;
        MaterialState states = Current.StatesController?.Value ?? MaterialState.None;
        if (!Current.Enabled)
        {
            states |= MaterialState.Disabled;
        }

        if (_isHovered)
        {
            states |= MaterialState.Hovered;
        }

        if (ButtonFocusNode.HasFocus)
        {
            states |= MaterialState.Focused;
        }

        Widget submenuIcon = Current.SubmenuIcon?.Resolve(states)
                             ?? MenuTheme.Of(context).SubmenuIcon?.Resolve(states)
                             ?? new Icon(Icons.ArrowRight, size: MenuConstants.DefaultSubmenuIconSize);

        ButtonStyle mergedStyle = MenuButtonStyleComposer.ComposeStyles(
            defaults: Current.DefaultStyleOf(context),
            themeStyle: Current.ThemeStyleOf(context),
            widgetStyle: Current.Style,
            legacyOverrides: null);
        Widget button = new TextButton(
            child: new MenuItemLabel(
                hasSubmenu: true,
                showDecoration: parentOrientation == Axis.Vertical,
                leadingIcon: Current.LeadingIcon,
                trailingIcon: Current.TrailingIcon,
                submenuIcon: submenuIcon,
                child: Current.Child),
            onPressed: Current.Enabled ? ToggleShowMenu : null,
            onFocusChange: HandleFocusChange,
            focusNode: ButtonFocusNode,
            style: mergedStyle,
            statesController: Current.StatesController,
            clipBehavior: Current.ClipBehavior,
            isSemanticButton: OperatingSystem.IsBrowser() ? true : null);

        if (Current.Enabled)
        {
            // As in Flutter, hover is read from MouseRegion.onHover rather than TextButton.onHover:
            // onEnter/onHover-on-enter also fire when a button scrolls under a stationary pointer,
            // which interferes with focus traversal and the scroll position.
            button = new MouseRegion(
                onHover: _ => HandleHover(true),
                onExit: _ => HandleHover(false),
                child: button);
        }

        Vector menuPaddingOffset = ResolveMenuPaddingOffset(context);

        Widget anchor = new MenuAnchor(
            menuChildren: Current.MenuChildren,
            controller: Controller,
            childFocusNode: ButtonFocusNode,
            style: Current.MenuStyle,
            alignmentOffset: menuPaddingOffset,
            clipBehavior: Current.ClipBehavior,
            onOpen: HandleOpen,
            onClose: HandleClose,
            useRootOverlay: Current.UseRootOverlay,
            animated: Current.Animated,
            onAnimationStatusChanged: HandleAnimationStatusChanged,
            child: new MergeSemantics(
                new Semantics(
                    expanded: Current.Enabled && _animationStatus.IsForwardOrCompleted(),
                    child: button)),
            builder: (anchorContext, _, child) =>
            {
                AnchorState = MenuAnchorScope.MaybeOf(anchorContext)?.State;
                return child ?? new SizedBox();
            });

        Widget result = new Actions(
            actions: new Dictionary<Type, FlutterAction>
            {
                [typeof(DirectionalFocusIntent)] = new SubmenuDirectionalFocusAction(this),
            },
            child: anchor);

        if (Current.Enabled && MenuAcceleratorLabel.PlatformSupportsAccelerators(context))
        {
            result = new MenuAcceleratorCallbackBinding(
                child: result,
                onInvoke: ToggleShowMenu,
                hasSubmenu: true);
        }

        return result;
    }

    internal void ToggleShowMenu()
    {
        if (!Mounted)
        {
            return;
        }

        if (_animationStatus.IsForwardOrCompleted())
        {
            Controller.Close();
        }
        else
        {
            Controller.Open();
        }
    }

    private Vector ResolveMenuPaddingOffset(BuildContext context)
    {
        Vector offset = Current.AlignmentOffset ?? default;
        MaterialState states = Current.StatesController?.Value ?? MaterialState.None;
        EdgeInsetsGeometry menuPaddingGeometry = Current.MenuStyle?.Padding?.Resolve(states)
                                                 ?? MenuTheme.Of(context).Style?.Padding?.Resolve(states)
                                                 ?? new MenuDefaultsM3(context).Padding!
                                                     .Resolve(states)!.Value;
        TextDirection direction = Directionality.Of(context);
        Thickness menuPadding = menuPaddingGeometry.Resolve(direction);

        // Flutter's `_SubmenuButtonState.build` falls back to `Axis.vertical` here, unlike the
        // horizontal fallback the label decoration and the overlay theme switch use.
        Axis orientation = _parentAnchor?.Orientation ?? Axis.Vertical;
        Vector delta = (orientation, direction) switch
        {
            (Axis.Horizontal, TextDirection.Rtl) => new Vector(menuPadding.Right, 0),
            (Axis.Horizontal, TextDirection.Ltr) => new Vector(-menuPadding.Left, 0),
            _ => new Vector(0, -menuPadding.Top),
        };
        return offset + delta;
    }

    private void ValidateHoverOpenDelay()
    {
        if (_parentAnchor?.Orientation == Axis.Horizontal && Current.HoverOpenDelay > TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "A non-zero hoverOpenDelay was used in a top-level SubmenuButton situated in a MenuBar.");
        }
    }

    private void HandleOpen()
    {
        if (!_waitingToFocusMenu)
        {
            _waitingToFocusMenu = true;
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (Mounted)
                {
                    ButtonFocusNode.RequestFocus();
                }

                _waitingToFocusMenu = false;
            });
        }

        SetState(static () => { });
        Current.OnOpen?.Invoke();
    }

    private void HandleClose()
    {
        if (!ButtonFocusNode.HasFocus)
        {
            _isOpenOnFocusEnabled = false;
            Scheduler.AddPostFrameCallback(_ => _isOpenOnFocusEnabled = true);
        }

        SetState(static () => { });
        Current.OnClose?.Invoke();
    }

    private void HandleHover(bool hovered)
    {
        if (!Current.Enabled)
        {
            return;
        }

        if (!hovered)
        {
            if (_isHovered)
            {
                Current.OnHover?.Invoke(false);
                _isHovered = false;
                CancelDelayedOpen();
            }

            return;
        }

        if (_isHovered)
        {
            return;
        }

        _isHovered = true;
        Current.OnHover?.Invoke(true);
        MenuAnchorState? root = _parentAnchor?.RootAnchor ?? AnchorState?.RootAnchor;
        if (_parentAnchor?.Orientation == Axis.Horizontal && root?.MenuController.IsOpen != true)
        {
            return;
        }

        if (ButtonFocusNode.HasPrimaryFocus)
        {
            CancelDelayedOpen();
            MaybeOpenMenuOnHoverOrFocus();
        }
        else
        {
            ButtonFocusNode.RequestFocus();
        }
    }

    private void HandleFocusChange(bool focused)
    {
        Current.OnFocusChange?.Invoke(focused);
        CancelDelayedOpen();
        if (!focused)
        {
            if (AnchorState?.MenuScopeNode.HasFocusInScope != true && _animationStatus.IsForwardOrCompleted())
            {
                Controller.Close();
            }

            return;
        }

        MaybeOpenMenuOnHoverOrFocus();
    }

    private void MaybeOpenMenuOnHoverOrFocus()
    {
        if (!_isOpenOnFocusEnabled || !Current.Enabled)
        {
            return;
        }

        if (Controller.IsOpen)
        {
            if (_animationStatus != AnimationStatus.Reverse) return;
            if (_isHovered) return;
            if (_parentAnchor?.Orientation == Axis.Horizontal) return;
        }

        if (Current.HoverOpenDelay == TimeSpan.Zero)
        {
            Controller.Open();
            return;
        }

        _hoverOpenCancellation = new CancellationTokenSource();
        _ = DelayOpenAsync(Current.HoverOpenDelay, _hoverOpenCancellation.Token);
    }

    private async Task DelayOpenAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            if (Mounted && Current.Enabled && !cancellationToken.IsCancellationRequested)
            {
                Controller.Open();
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void CancelDelayedOpen()
    {
        _hoverOpenCancellation?.Cancel();
        _hoverOpenCancellation?.Dispose();
        _hoverOpenCancellation = null;
    }

    private void HandleAnimationStatusChanged(AnimationStatus status)
    {
        if (_animationStatus != status && Mounted)
        {
            SetState(() => _animationStatus = status);
        }
        else
        {
            _animationStatus = status;
        }

        Current.OnAnimationStatusChanged?.Invoke(status);
    }
}

/// <summary>Flutter's `_MenuDirectionalFocusAction`: arrow keys traverse, open and close submenus.</summary>
internal sealed class SubmenuDirectionalFocusAction : FlutterAction<DirectionalFocusIntent>
{
    private readonly SubmenuButtonState _submenu;

    public SubmenuDirectionalFocusAction(SubmenuButtonState submenu)
    {
        _submenu = submenu;
    }

    public override object? Invoke(DirectionalFocusIntent intent)
    {
        MenuAnchorState? parent = _submenu.ParentAnchor;
        Axis? orientation = parent?.Orientation;
        MenuAnchorState? anchorState = _submenu.AnchorState;
        MenuController controller = _submenu.Controller;
        FocusNode button = _submenu.ButtonFocusNode;
        bool isSubmenu = button.HasPrimaryFocus;
        TextDirection direction = _submenu.Mounted
            ? Directionality.Of(_submenu.Context)
            : TextDirection.Ltr;

        switch (orientation, direction, intent.Direction)
        {
            case (Axis.Horizontal, TextDirection.Ltr, TraversalDirection.Left):
            case (Axis.Horizontal, TextDirection.Rtl, TraversalDirection.Right):
                button.RequestFocus();
                button.PreviousFocus();
                return null;
            case (Axis.Horizontal, TextDirection.Ltr, TraversalDirection.Right):
            case (Axis.Horizontal, TextDirection.Rtl, TraversalDirection.Left):
                button.RequestFocus();
                button.NextFocus();
                return null;
            case (Axis.Horizontal, _, TraversalDirection.Down):
                if (isSubmenu)
                {
                    anchorState?.FocusFirstMenuItem();
                    return null;
                }

                break;
            case (Axis.Horizontal, _, TraversalDirection.Up):
                if (isSubmenu)
                {
                    anchorState?.FocusLastMenuItem();
                    return null;
                }

                break;
            case (Axis.Vertical, TextDirection.Ltr, TraversalDirection.Left):
            case (Axis.Vertical, TextDirection.Rtl, TraversalDirection.Right):
                if (parent?.AnchorParent?.Orientation == Axis.Horizontal)
                {
                    if (isSubmenu)
                    {
                        FocusNode? parentButton = ResolveParentChildFocusNode(parent);
                        parentButton?.RequestFocus();
                        parentButton?.PreviousFocus();
                    }
                    else
                    {
                        anchorState?.FocusButton();
                    }

                    return null;
                }

                if (isSubmenu)
                {
                    if (parent?.AnchorParent is null)
                    {
                        return null;
                    }

                    parent.FocusButton();
                    parent.MenuController.Close();
                }
                else
                {
                    controller.Close();
                }

                return null;
            case (Axis.Vertical, TextDirection.Ltr, TraversalDirection.Right):
            case (Axis.Vertical, TextDirection.Rtl, TraversalDirection.Left):
                if (!isSubmenu)
                {
                    break;
                }

                if (controller.IsOpen)
                {
                    anchorState?.FocusFirstMenuItem();
                }
                else
                {
                    controller.Open();
                    Scheduler.AddPostFrameCallback(_ =>
                    {
                        if (controller.IsOpen)
                        {
                            _submenu.AnchorState?.FocusFirstMenuItem();
                        }
                    });
                }

                return null;
        }

        if (_submenu.Mounted)
        {
            Actions.MaybeInvoke(_submenu.Context, intent);
        }

        return null;
    }

    private static FocusNode? ResolveParentChildFocusNode(MenuAnchorState? parent)
    {
        return parent?.CurrentAnchor.ChildFocusNode;
    }
}

/// <summary>Flutter's `_MenuLayout`: places an open menu panel relative to its anchor.</summary>
internal sealed class MenuLayout : SingleChildLayoutDelegate
{
    public MenuLayout(
        Rect anchorRect,
        TextDirection textDirection,
        AlignmentGeometry alignment,
        Vector alignmentOffset,
        Vector? menuPosition,
        EdgeInsetsGeometry menuPadding,
        Axis orientation,
        Axis parentOrientation,
        EdgeInsetsGeometry reservedPadding,
        IReadOnlyList<Rect>? avoidBounds = null,
        double heightFactor = 1.0,
        Thickness viewPadding = default,
        Thickness viewInsets = default)
    {
        AnchorRect = anchorRect;
        TextDirection = textDirection;
        Alignment = alignment;
        AlignmentOffset = alignmentOffset;
        MenuPosition = menuPosition;
        MenuPadding = menuPadding;
        Orientation = orientation;
        ParentOrientation = parentOrientation;
        ReservedPadding = reservedPadding;
        AvoidBounds = avoidBounds ?? [];
        HeightFactor = heightFactor;
        ViewPadding = viewPadding;
        ViewInsets = viewInsets;
    }

    /// <summary>The anchor's rect, relative to the overlay the menu is placed in.</summary>
    public Rect AnchorRect { get; }

    public TextDirection TextDirection { get; }

    public AlignmentGeometry Alignment { get; }

    public Vector AlignmentOffset { get; }

    /// <summary>The position passed to <see cref="MenuController.Open"/>, if any.</summary>
    public Vector? MenuPosition { get; }

    /// <summary>The menu panel's own padding. Only <see cref="ShouldRelayout"/> reads it, as in Dart.</summary>
    public EdgeInsetsGeometry MenuPadding { get; }

    public Axis Orientation { get; }

    public Axis ParentOrientation { get; }

    public EdgeInsetsGeometry ReservedPadding { get; }

    public IReadOnlyList<Rect> AvoidBounds { get; }

    /// <summary>The fraction of the panel's full height that is currently revealed.</summary>
    public double HeightFactor { get; }

    /// <summary>`MediaQueryData.padding` of the overlay's media query.</summary>
    public Thickness ViewPadding { get; }

    /// <summary>`MediaQueryData.viewInsets` of the overlay's media query.</summary>
    public Thickness ViewInsets { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints)
    {
        return BoxConstraints.Loose(constraints.Biggest).Deflate(ReservedPadding.Resolve(TextDirection));
    }

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        Rect overlayRect = DeflateRect(ViewPadding, DeflateRect(ViewInsets, new Rect(default, size)));

        // Position the menu using its *unfolded* height so a growing panel does not slide across
        // the screen while it opens.
        double unconstrainedHeight = HeightFactor > 0.01 ? childSize.Height / HeightFactor : 0.0;
        double childHeightEstimate = Math.Min(unconstrainedHeight, size.Height);
        var childSizeEstimate = new Size(childSize.Width, childHeightEstimate);
        Point finalPosition = PositionChild(childSizeEstimate, overlayRect);

        if (MenuPosition.HasValue)
        {
            return finalPosition;
        }

        bool growsUp = finalPosition.Y + childSizeEstimate.Height <= AnchorRect.Center.Y;
        if (growsUp)
        {
            return new Point(finalPosition.X, finalPosition.Y + (childHeightEstimate - childSize.Height));
        }

        var initialPosition = new Point(finalPosition.X, AnchorRect.Bottom);
        return new Point(
            initialPosition.X + ((finalPosition.X - initialPosition.X) * HeightFactor),
            initialPosition.Y + ((finalPosition.Y - initialPosition.Y) * HeightFactor));
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        if (oldDelegate is not MenuLayout oldLayout)
        {
            return true;
        }

        return AnchorRect != oldLayout.AnchorRect
               || TextDirection != oldLayout.TextDirection
               || Alignment != oldLayout.Alignment
               || AlignmentOffset != oldLayout.AlignmentOffset
               || MenuPosition != oldLayout.MenuPosition
               || MenuPadding != oldLayout.MenuPadding
               || Orientation != oldLayout.Orientation
               || ParentOrientation != oldLayout.ParentOrientation
               || ReservedPadding != oldLayout.ReservedPadding
               || HeightFactor != oldLayout.HeightFactor
               || !AvoidBounds.SequenceEqual(oldLayout.AvoidBounds);
    }

    private Point PositionChild(Size childSize, Rect overlayRect)
    {
        double x;
        double y;
        if (!MenuPosition.HasValue)
        {
            Alignment resolved = Alignment.Resolve(TextDirection);
            Point desiredPosition = resolved.WithinRect(AnchorRect);
            Vector directionalOffset = Alignment.IsDirectional && TextDirection == TextDirection.Rtl
                ? new Vector(-AlignmentOffset.X, AlignmentOffset.Y)
                : AlignmentOffset;
            x = desiredPosition.X + directionalOffset.X;
            y = desiredPosition.Y + directionalOffset.Y;
            if (TextDirection == TextDirection.Rtl)
            {
                x -= childSize.Width;
            }
        }
        else
        {
            x = MenuPosition.Value.X + AnchorRect.Left;
            y = MenuPosition.Value.Y + AnchorRect.Top;
        }

        List<Rect> subScreens = DisplayFeatureSubScreen.SubScreensInBounds(overlayRect, AvoidBounds);
        Rect allowedRect = ClosestScreen(subScreens, AnchorRect.Center);

        bool OffLeftSide(double value) => value < allowedRect.Left;
        bool OffRightSide(double value) => value + childSize.Width > allowedRect.Right;
        bool OffTop(double value) => value < allowedRect.Top;
        bool OffBottom(double value) => value + childSize.Height > allowedRect.Bottom;

        if (childSize.Width >= allowedRect.Width)
        {
            x = allowedRect.Left;
        }
        else if (OffLeftSide(x))
        {
            if (ParentOrientation != Orientation)
            {
                x = allowedRect.Left;
            }
            else
            {
                double newX = AnchorRect.Right + AlignmentOffset.X;
                x = !OffRightSide(newX) ? newX : allowedRect.Left;
            }
        }
        else if (OffRightSide(x))
        {
            if (ParentOrientation != Orientation)
            {
                x = allowedRect.Right - childSize.Width;
            }
            else
            {
                double newX = AnchorRect.Left - childSize.Width - AlignmentOffset.X;
                x = !OffLeftSide(newX) ? newX : allowedRect.Right - childSize.Width;
            }
        }

        if (childSize.Height >= allowedRect.Height)
        {
            y = allowedRect.Top;
        }
        else if (OffTop(y))
        {
            double newY = AnchorRect.Bottom;
            y = !OffBottom(newY) ? newY : allowedRect.Top;
        }
        else if (OffBottom(y))
        {
            double newY = AnchorRect.Top - childSize.Height;
            if (!OffTop(newY))
            {
                y = ParentOrientation == Axis.Horizontal ? newY - AlignmentOffset.Y : newY;
            }
            else
            {
                y = allowedRect.Bottom - childSize.Height;
            }
        }

        return new Point(x, y);
    }

    private static Rect ClosestScreen(IReadOnlyList<Rect> screens, Point point)
    {
        Rect closest = screens[0];
        foreach (Rect screen in screens)
        {
            if (Distance(screen.Center, point) < Distance(closest.Center, point))
            {
                closest = screen;
            }
        }

        return closest;
    }

    private static double Distance(Point from, Point to)
    {
        double dx = from.X - to.X;
        double dy = from.Y - to.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static Rect DeflateRect(Thickness insets, Rect rect)
    {
        double left = rect.Left + insets.Left;
        double top = rect.Top + insets.Top;
        return new Rect(
            left,
            top,
            Math.Max(0.0, rect.Right - insets.Right - left),
            Math.Max(0.0, rect.Bottom - insets.Bottom - top));
    }
}

/// <summary>
/// The per-state style layering `ButtonStyleButton` does internally. Plumix's `MenuItemButton` and
/// `SubmenuButton` compose a `TextButton` instead of extending `ButtonStyleButton` the way Dart's do
/// (`docs/ai/DIVERGENCES.md`), so they layer the three style sources themselves.
/// </summary>
internal static class MenuButtonStyleComposer
{
    public static ButtonStyle ComposeStyles(
        ButtonStyle? defaults,
        ButtonStyle? themeStyle,
        ButtonStyle? widgetStyle,
        ButtonStyle? legacyOverrides)
    {
        return new ButtonStyle(
            ForegroundColor: ComposeStateProperty<Color?>(
                legacyOverrides?.ForegroundColor,
                widgetStyle?.ForegroundColor,
                themeStyle?.ForegroundColor,
                defaults?.ForegroundColor),
            BackgroundColor: ComposeStateProperty<Color?>(
                legacyOverrides?.BackgroundColor,
                widgetStyle?.BackgroundColor,
                themeStyle?.BackgroundColor,
                defaults?.BackgroundColor),
            ShadowColor: ComposeStateProperty<Color?>(
                legacyOverrides?.ShadowColor,
                widgetStyle?.ShadowColor,
                themeStyle?.ShadowColor,
                defaults?.ShadowColor),
            SurfaceTintColor: ComposeStateProperty<Color?>(
                legacyOverrides?.SurfaceTintColor,
                widgetStyle?.SurfaceTintColor,
                themeStyle?.SurfaceTintColor,
                defaults?.SurfaceTintColor),
            OverlayColor: ComposeStateProperty<Color?>(
                legacyOverrides?.OverlayColor,
                widgetStyle?.OverlayColor,
                themeStyle?.OverlayColor,
                defaults?.OverlayColor),
            IconColor: ComposeIconColorProperty(
                legacyOverrides,
                widgetStyle,
                themeStyle,
                defaults),
            IconSize: ComposeStateProperty<double?>(
                legacyOverrides?.IconSize,
                widgetStyle?.IconSize,
                themeStyle?.IconSize,
                defaults?.IconSize),
            Elevation: ComposeStateProperty<double?>(
                legacyOverrides?.Elevation,
                widgetStyle?.Elevation,
                themeStyle?.Elevation,
                defaults?.Elevation),
            Side: ComposeStateProperty<BorderSide?>(
                legacyOverrides?.Side,
                widgetStyle?.Side,
                themeStyle?.Side,
                defaults?.Side),
            Padding: ComposeStateProperty<EdgeInsetsGeometry?>(
                legacyOverrides?.Padding,
                widgetStyle?.Padding,
                themeStyle?.Padding,
                defaults?.Padding),
            Shape: ComposeStateProperty<OutlinedBorder?>(
                legacyOverrides?.Shape,
                widgetStyle?.Shape,
                themeStyle?.Shape,
                defaults?.Shape),
            MinimumSize: ComposeStateProperty<Size?>(
                legacyOverrides?.MinimumSize,
                widgetStyle?.MinimumSize,
                themeStyle?.MinimumSize,
                defaults?.MinimumSize),
            FixedSize: ComposeStateProperty<Size?>(
                legacyOverrides?.FixedSize,
                widgetStyle?.FixedSize,
                themeStyle?.FixedSize,
                defaults?.FixedSize),
            MaximumSize: ComposeStateProperty<Size?>(
                legacyOverrides?.MaximumSize,
                widgetStyle?.MaximumSize,
                themeStyle?.MaximumSize,
                defaults?.MaximumSize),
            Alignment: legacyOverrides?.Alignment
                       ?? widgetStyle?.Alignment
                       ?? themeStyle?.Alignment
                       ?? defaults?.Alignment,
            IconAlignment: legacyOverrides?.IconAlignment
                           ?? widgetStyle?.IconAlignment
                           ?? themeStyle?.IconAlignment
                           ?? defaults?.IconAlignment,
            TapTargetSize: legacyOverrides?.TapTargetSize
                           ?? widgetStyle?.TapTargetSize
                           ?? themeStyle?.TapTargetSize
                           ?? defaults?.TapTargetSize,
            TextStyle: ComposeStateProperty<TextStyle?>(
                legacyOverrides?.TextStyle,
                widgetStyle?.TextStyle,
                themeStyle?.TextStyle,
                defaults?.TextStyle),
            MouseCursor: ComposeStateProperty<MouseCursor?>(
                legacyOverrides?.MouseCursor,
                widgetStyle?.MouseCursor,
                themeStyle?.MouseCursor,
                defaults?.MouseCursor),
            VisualDensity: legacyOverrides?.VisualDensity
                           ?? widgetStyle?.VisualDensity
                           ?? themeStyle?.VisualDensity
                           ?? defaults?.VisualDensity,
            AnimationDuration: legacyOverrides?.AnimationDuration
                               ?? widgetStyle?.AnimationDuration
                               ?? themeStyle?.AnimationDuration
                               ?? defaults?.AnimationDuration,
            EnableFeedback: legacyOverrides?.EnableFeedback
                            ?? widgetStyle?.EnableFeedback
                            ?? themeStyle?.EnableFeedback
                            ?? defaults?.EnableFeedback,
            SplashFactory: legacyOverrides?.SplashFactory
                           ?? widgetStyle?.SplashFactory
                           ?? themeStyle?.SplashFactory
                           ?? defaults?.SplashFactory,
            BackgroundBuilder: legacyOverrides?.BackgroundBuilder
                               ?? widgetStyle?.BackgroundBuilder
                               ?? themeStyle?.BackgroundBuilder
                               ?? defaults?.BackgroundBuilder,
            ForegroundBuilder: legacyOverrides?.ForegroundBuilder
                               ?? widgetStyle?.ForegroundBuilder
                               ?? themeStyle?.ForegroundBuilder
                               ?? defaults?.ForegroundBuilder);
    }

    private static MaterialStateProperty<T>? ComposeStateProperty<T>(
        params MaterialStateProperty<T>?[] layers)
    {
        bool hasAny = false;
        foreach (var layer in layers)
        {
            if (layer is not null)
            {
                hasAny = true;
                break;
            }
        }

        if (!hasAny)
        {
            return null;
        }

        return MaterialStateProperty<T>.ResolveWith(states =>
        {
            foreach (var layer in layers)
            {
                if (layer is null)
                {
                    continue;
                }

                var resolved = layer.Resolve(states);
                if (resolved is not null)
                {
                    return resolved;
                }
            }

            return default!;
        });
    }

    private static MaterialStateProperty<Color?>? ComposeIconColorProperty(params ButtonStyle?[] layers)
    {
        bool hasAny = layers.Any(style => style?.IconColor is not null || style?.ForegroundColor is not null);
        if (!hasAny)
        {
            return null;
        }

        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            foreach (ButtonStyle? style in layers)
            {
                Color? resolved = style?.IconColor?.Resolve(states)
                                  ?? style?.ForegroundColor?.Resolve(states);
                if (resolved.HasValue)
                {
                    return resolved;
                }
            }

            return null;
        });
    }
}
