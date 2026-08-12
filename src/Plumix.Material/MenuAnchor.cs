using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart.

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
            [new SingleActivator("GameButtonA")] = new ActivateIntent(),
            [new SingleActivator("Escape")] = new DismissIntent(),
            [new SingleActivator("Tab")] = new NextFocusIntent(),
            [new SingleActivator("Tab", shift: true)] = new PreviousFocusIntent(),
            [new SingleActivator("ArrowDown")] = new DirectionalFocusIntent(TraversalDirection.Down),
            [new SingleActivator("ArrowUp")] = new DirectionalFocusIntent(TraversalDirection.Up),
            [new SingleActivator("ArrowLeft")] = new DirectionalFocusIntent(TraversalDirection.Left),
            [new SingleActivator("ArrowRight")] = new DirectionalFocusIntent(TraversalDirection.Right),
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

        _animationController = new AnimationController(TimeSpan.FromMilliseconds(1), this);
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
            double? width = double.IsFinite(fixedSize.Value.Width) ? fixedSize.Value.Width : null;
            double? height = double.IsFinite(fixedSize.Value.Height) ? fixedSize.Value.Height : null;
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
        MenuStyle defaults = Anchor.AnchorParentOrientation == Axis.Vertical
            ? MenuStyleDefaults.Menu(theme)
            : MenuStyleDefaults.MenuBar(theme);
        MenuStyle? themeStyle = Anchor.AnchorParentOrientation == Axis.Vertical
            ? MenuTheme.Of(context).Style
            : MenuBarTheme.Of(context).Style;
        MenuStyle resolved = (MenuStyle ?? new MenuStyle()).Merge(themeStyle).Merge(defaults);
        VisualDensity visualDensity = resolved.VisualDensity ?? theme.VisualDensity;
        MouseCursor cursor = resolved.MouseCursor?.Resolve(MaterialState.None) ?? Plumix.Widgets.MouseCursor.Defer;

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
        Widget layout = new AnimatedBuilder(
            animation: HeightAnimation,
            builder: (_, child) => new CustomSingleChildLayout(
                layoutDelegate: new MenuOverlayLayoutDelegate(
                    anchorRect: MenuPosition.AnchorRect,
                    alignmentOffset: AlignmentOffset,
                    reservedPadding: ReservedPadding.Resolve(Directionality.Of(context)),
                    placementAxis: Anchor.AnchorParentOrientation == Axis.Horizontal
                        ? Axis.Vertical
                        : Axis.Horizontal,
                    textDirection: Directionality.Of(context),
                    viewInsets: mediaQuery?.ViewInsets ?? default,
                    position: MenuPosition.Position,
                    displayFeatures: mediaQuery?.DisplayFeatures,
                    heightFactor: HeightAnimation.Value),
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
        ThemeData theme = Theme.Of(context);
        MenuStyle defaults = Current.Orientation == Axis.Horizontal
            ? MenuStyleDefaults.MenuBar(theme)
            : MenuStyleDefaults.Menu(theme);
        MenuStyle? themeStyle = Current.Orientation == Axis.Horizontal
            ? MenuBarTheme.Of(context).Style
            : MenuTheme.Of(context).Style;
        MenuStyle style = (Current.MenuStyle ?? new MenuStyle()).Merge(themeStyle).Merge(defaults);
        MaterialState states = MaterialState.None;

        Color? backgroundColor = style.BackgroundColor?.Resolve(states);
        Color shadowColor = style.ShadowColor?.Resolve(states) ?? theme.ShadowColor;
        Color surfaceTint = style.SurfaceTintColor?.Resolve(states) ?? Colors.Transparent;
        double elevation = style.Elevation?.Resolve(states) ?? 0.0;
        ShapeBorder shape = style.Shape?.Resolve(states)
                            ?? new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(4.0));
        BorderSide? side = style.Side?.Resolve(states);
        if (side.HasValue && shape is OutlinedBorder outlined)
        {
            shape = outlined.CopyWith(side);
        }

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
            type: backgroundColor is null ? MaterialType.Transparency : MaterialType.Card,
            color: backgroundColor ?? Colors.Transparent,
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

/// <summary>Material 3 component defaults for menu bars and menu panels.</summary>
internal static class MenuStyleDefaults
{
    public static MenuStyle MenuBar(ThemeData theme) => new(
        BackgroundColor: MaterialStateProperty<Color?>.All(theme.SurfaceContainerColor),
        ShadowColor: MaterialStateProperty<Color?>.All(theme.ShadowColor),
        SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
        Elevation: MaterialStateProperty<double?>.All(3.0),
        Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(
            EdgeInsetsGeometry.DirectionalOnly(
                start: MenuConstants.TopLevelMenuHorizontalMinPadding,
                end: MenuConstants.TopLevelMenuHorizontalMinPadding)),
        Shape: MaterialStateProperty<ShapeBorder?>.All(
            new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(4.0))),
        Alignment: Plumix.Rendering.Alignment.BottomLeft,
        VisualDensity: theme.VisualDensity);

    public static MenuStyle Menu(ThemeData theme) => new(
        BackgroundColor: MaterialStateProperty<Color?>.All(theme.SurfaceContainerColor),
        ShadowColor: MaterialStateProperty<Color?>.All(theme.ShadowColor),
        SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
        Elevation: MaterialStateProperty<double?>.All(3.0),
        Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(
            EdgeInsetsGeometry.Symmetric(vertical: MenuConstants.MenuVerticalMinPadding)),
        Shape: MaterialStateProperty<ShapeBorder?>.All(
            new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(4.0))),
        Alignment: Plumix.Rendering.Alignment.TopRight,
        VisualDensity: theme.VisualDensity);
}

public sealed class MenuItemButton : StatelessWidget
{
    public MenuItemButton(
        Widget? child = null,
        Action? onPressed = null,
        Action<bool>? onHover = null,
        bool requestFocusOnHover = true,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
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
    public string? SemanticsLabel { get; }
    public ButtonStyle? Style { get; }
    public MaterialStatesController? StatesController { get; }
    public Clip ClipBehavior { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public bool CloseOnActivate { get; }
    public Axis OverflowAxis { get; }
    public bool Enabled => OnPressed is not null;

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        Widget label = Child ?? new SizedBox();
        var row = new List<Widget>();
        if (LeadingIcon is not null) row.Add(LeadingIcon);
        row.Add(new Flexible(child: label));
        if (TrailingIcon is not null) row.Add(TrailingIcon);
        Widget content = new Row(
            mainAxisSize: MainAxisSize.Max,
            spacing: MenuConstants.LabelItemDefaultSpacing,
            children: row,
            textDirection: Directionality.Of(context));
        ButtonStyle defaults = new(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled) ? theme.DisabledColor : theme.OnSurfaceColor),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(theme.OnSurfaceColor),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 0)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(64, 48)),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                BorderRadius.Zero)),
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap);
        Action? activate = OnPressed is null ? null : () =>
        {
            if (CloseOnActivate)
            {
                MenuAnchorScope.MaybeOf(context)?.State.RootAnchor.MenuController.Close();
            }

            Scheduler.AddPostFrameCallback(_ => OnPressed());
        };
        Widget result = new MaterialButtonCore(
            child: content,
            onPressed: activate,
            style: MaterialButtonCore.ComposeStyles(
                defaults,
                MenuButtonTheme.Of(context).Style,
                Style,
                null),
            onHoverChanged: value =>
            {
                OnHover?.Invoke(value);
                if (value && RequestFocusOnHover)
                {
                    FocusNode?.RequestFocus();
                }
            },
            onFocusChange: focused =>
            {
                OnFocusChange?.Invoke(focused);
                if (!focused)
                {
                    MenuController.MaybeOf(context)?.CloseChildren();
                }
            },
            focusNode: FocusNode,
            statesController: StatesController,
            autofocus: Autofocus,
            semanticLabel: SemanticsLabel,
            clipBehavior: ClipBehavior,
            enabled: Enabled);

        if (Enabled && MenuAcceleratorLabel.PlatformSupportsAccelerators(context))
        {
            result = new MenuAcceleratorCallbackBinding(
                child: result,
                onInvoke: activate);
        }

        return result;
    }
}

/// <summary>A menu item that combines a <see cref="Checkbox"/> with a <see cref="MenuItemButton"/>.</summary>
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart.</remarks>
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
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart.</remarks>
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
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart (MenuBar).</remarks>
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
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart (SubmenuButton).</remarks>
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

        Widget submenuIcon = Current.SubmenuIcon?.Resolve(states)
                             ?? MenuTheme.Of(context).SubmenuIcon?.Resolve(states)
                             ?? new Icon(Icons.ChevronRight, size: MenuConstants.DefaultSubmenuIconSize);
        var row = new List<Widget>();
        if (Current.LeadingIcon is not null) row.Add(Current.LeadingIcon);
        row.Add(new Flexible(child: Current.Child ?? new SizedBox()));
        if (Current.TrailingIcon is not null) row.Add(Current.TrailingIcon);
        row.Add(submenuIcon);

        ThemeData theme = Theme.Of(context);
        ButtonStyle defaults = new(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(value =>
                value.HasFlag(MaterialState.Disabled) ? theme.DisabledColor : theme.OnSurfaceColor),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(theme.OnSurfaceColor),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 0)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(64, 48)),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                BorderRadius.Zero)),
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap);
        Widget button = new MaterialButtonCore(
            child: new Row(
                mainAxisSize: MainAxisSize.Max,
                spacing: MenuConstants.LabelItemDefaultSpacing,
                children: row,
                textDirection: Directionality.Of(context)),
            onPressed: Current.Enabled ? ToggleShowMenu : null,
            style: MaterialButtonCore.ComposeStyles(
                defaults,
                MenuButtonTheme.Of(context).Style,
                Current.Style,
                null),
            onHoverChanged: HandleHover,
            onFocusChange: HandleFocusChange,
            focusNode: ButtonFocusNode,
            clipBehavior: Current.ClipBehavior,
            enabled: Current.Enabled);

        Vector menuPaddingOffset = ResolveMenuPaddingOffset(context, parentOrientation);

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

    private Vector ResolveMenuPaddingOffset(BuildContext context, Axis parentOrientation)
    {
        Vector offset = Current.AlignmentOffset ?? default;
        EdgeInsetsGeometry menuPaddingGeometry = Current.MenuStyle?.Padding?.Resolve(MaterialState.None)
                                                 ?? MenuTheme.Of(context).Style?.Padding
                                                     ?.Resolve(MaterialState.None)
                                                 ?? MenuStyleDefaults.Menu(Theme.Of(context)).Padding!
                                                     .Resolve(MaterialState.None)!.Value;
        Thickness menuPadding = menuPaddingGeometry.Resolve(Directionality.Of(context));
        TextDirection direction = Directionality.Of(context);
        Vector delta = (parentOrientation, direction) switch
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

internal sealed class MenuOverlayLayoutDelegate : SingleChildLayoutDelegate
{
    public MenuOverlayLayoutDelegate(
        Rect anchorRect,
        Vector alignmentOffset,
        Thickness reservedPadding,
        Axis placementAxis,
        TextDirection textDirection,
        Thickness viewInsets,
        Vector? position,
        IReadOnlyList<DisplayFeature>? displayFeatures = null,
        double heightFactor = 1.0)
    {
        AnchorRect = anchorRect;
        AlignmentOffset = alignmentOffset;
        ReservedPadding = reservedPadding;
        PlacementAxis = placementAxis;
        TextDirection = textDirection;
        ViewInsets = viewInsets;
        Position = position;
        DisplayFeatures = displayFeatures ?? [];
        HeightFactor = heightFactor;
    }

    public Rect AnchorRect { get; }

    public Vector AlignmentOffset { get; }

    public Thickness ReservedPadding { get; }

    public Axis PlacementAxis { get; }

    public TextDirection TextDirection { get; }

    public Thickness ViewInsets { get; }

    public Vector? Position { get; }

    public IReadOnlyList<DisplayFeature> DisplayFeatures { get; }

    /// <summary>The fraction of the panel's full height that is currently revealed.</summary>
    public double HeightFactor { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints)
    {
        Rect available = ResolveAvailableRegion(constraints.Biggest);
        return BoxConstraints.Loose(available.Size);
    }

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        // Flutter's `_MenuLayout` positions the menu using its *unfolded* height so a growing panel
        // does not slide across the screen while it opens.
        double unconstrainedHeight = HeightFactor > 0.01 ? childSize.Height / HeightFactor : 0.0;
        double childHeightEstimate = Math.Min(unconstrainedHeight, size.Height);
        var childSizeEstimate = new Size(childSize.Width, childHeightEstimate);
        Point finalPosition = PositionChild(size, childSizeEstimate);
        if (Position.HasValue)
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
        if (oldDelegate is not MenuOverlayLayoutDelegate oldLayout)
        {
            return true;
        }

        return AnchorRect != oldLayout.AnchorRect
               || AlignmentOffset != oldLayout.AlignmentOffset
               || ReservedPadding != oldLayout.ReservedPadding
               || PlacementAxis != oldLayout.PlacementAxis
               || TextDirection != oldLayout.TextDirection
               || ViewInsets != oldLayout.ViewInsets
               || Position != oldLayout.Position
               || HeightFactor != oldLayout.HeightFactor
               || !DisplayFeatures.SequenceEqual(oldLayout.DisplayFeatures);
    }

    private Point PositionChild(Size size, Size childSize)
    {
        Rect available = ResolveAvailableRegion(size);
        double leftLimit = available.Left;
        double topLimit = available.Top;
        double rightLimit = available.Right;
        double bottomLimit = available.Bottom;
        double x;
        double y;
        if (Position.HasValue)
        {
            x = AnchorRect.Left + Position.Value.X;
            y = AnchorRect.Top + Position.Value.Y;
        }
        else if (PlacementAxis == Axis.Horizontal)
        {
            bool placeAfter = TextDirection == TextDirection.Ltr;
            double after = placeAfter ? AnchorRect.Right : AnchorRect.Left - childSize.Width;
            double before = placeAfter ? AnchorRect.Left - childSize.Width : AnchorRect.Right;
            x = FitsHorizontally(after, childSize.Width, leftLimit, rightLimit) ? after : before;
            y = AnchorRect.Top;
        }
        else
        {
            x = TextDirection == TextDirection.Ltr
                ? AnchorRect.Left
                : AnchorRect.Right - childSize.Width;
            double below = AnchorRect.Bottom;
            double above = AnchorRect.Top - childSize.Height;
            y = below + childSize.Height <= bottomLimit ? below : above;
        }

        if (!Position.HasValue)
        {
            x += AlignmentOffset.X;
            y += AlignmentOffset.Y;
        }

        x = Math.Clamp(x, leftLimit, Math.Max(leftLimit, rightLimit - childSize.Width));
        y = Math.Clamp(y, topLimit, Math.Max(topLimit, bottomLimit - childSize.Height));
        return new Point(x, y);
    }

    private static bool FitsHorizontally(
        double x,
        double width,
        double leftLimit,
        double rightLimit)
    {
        return x >= leftLimit && x + width <= rightLimit;
    }

    private Rect ResolveAvailableRegion(Size size)
    {
        var regions = new List<Rect>
        {
            new(
                ReservedPadding.Left,
                ReservedPadding.Top,
                Math.Max(0.0, size.Width - ReservedPadding.Left - ReservedPadding.Right),
                Math.Max(
                    0.0,
                    size.Height - ReservedPadding.Top - ReservedPadding.Bottom - ViewInsets.Bottom)),
        };
        foreach (DisplayFeature feature in DisplayFeatures)
        {
            var next = new List<Rect>();
            foreach (Rect region in regions)
            {
                Rect bounds = feature.Bounds.Intersect(region);
                if (bounds.Width <= 0.0 || bounds.Height <= 0.0)
                {
                    next.Add(region);
                    continue;
                }

                if (bounds.Height >= region.Height && bounds.Width < region.Width)
                {
                    next.Add(new Rect(region.Left, region.Top, bounds.Left - region.Left, region.Height));
                    next.Add(new Rect(bounds.Right, region.Top, region.Right - bounds.Right, region.Height));
                }
                else if (bounds.Width >= region.Width && bounds.Height < region.Height)
                {
                    next.Add(new Rect(region.Left, region.Top, region.Width, bounds.Top - region.Top));
                    next.Add(new Rect(region.Left, bounds.Bottom, region.Width, region.Bottom - bounds.Bottom));
                }
                else
                {
                    next.Add(region);
                }
            }

            regions = next.Where(region => region.Width > 0.0 && region.Height > 0.0).ToList();
        }

        Point anchorCenter = AnchorRect.Center;
        return regions.MinBy(region =>
        {
            double dx = region.Center.X - anchorCenter.X;
            double dy = region.Center.Y - anchorCenter.Y;
            return (dx * dx) + (dy * dy);
        });
    }
}
