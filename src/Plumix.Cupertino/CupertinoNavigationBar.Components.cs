using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/nav_bar.dart

/// <summary>
/// Dart's `_NavigationBarStaticComponentsKeys`: keys used when building static routes' nav bar
/// components, read in the hero flight to reference the components' render boxes.
/// </summary>
internal sealed class NavigationBarStaticComponentsKeys
{
    public GlobalKey NavBarBoxKey { get; } = new GlobalObjectKey<State>(new object());

    public GlobalKey LeadingKey { get; } = new GlobalObjectKey<State>(new object());

    public GlobalKey BackChevronKey { get; } = new GlobalObjectKey<State>(new object());

    public GlobalKey BackLabelKey { get; } = new GlobalObjectKey<State>(new object());

    public GlobalKey MiddleKey { get; } = new GlobalObjectKey<State>(new object());

    public GlobalKey TrailingKey { get; } = new GlobalObjectKey<State>(new object());

    public GlobalKey LargeTitleKey { get; } = new GlobalObjectKey<State>(new object());

    public GlobalKey NavBarBottomKey { get; } = new GlobalObjectKey<State>(new object());
}

/// <summary>
/// Dart's `_NavigationBarStaticComponents`: `KeyedSubtree` components shared by
/// <see cref="CupertinoNavigationBar"/> and <see cref="CupertinoSliverNavigationBar"/>, whose
/// children are reused in hero flights.
/// </summary>
internal sealed class NavigationBarStaticComponents
{
    public NavigationBarStaticComponents(
        NavigationBarStaticComponentsKeys keys,
        ModalRoute? route,
        Widget? userLeading,
        bool automaticallyImplyLeading,
        bool automaticallyImplyTitle,
        string? previousPageTitle,
        Widget? userMiddle,
        Widget? userTrailing,
        Widget? userLargeTitle,
        Widget? userBottom,
        EdgeInsetsDirectional? padding,
        bool large,
        bool staticBar,
        BuildContext context)
    {
        Leading = CreateLeading(
            leadingKey: keys.LeadingKey,
            userLeading: userLeading,
            route: route,
            automaticallyImplyLeading: automaticallyImplyLeading,
            padding: padding,
            context: context);
        BackChevron = CreateBackChevron(
            backChevronKey: keys.BackChevronKey,
            userLeading: userLeading,
            route: route,
            automaticallyImplyLeading: automaticallyImplyLeading,
            context: context);
        BackLabel = CreateBackLabel(
            backLabelKey: keys.BackLabelKey,
            userLeading: userLeading,
            route: route,
            previousPageTitle: previousPageTitle,
            automaticallyImplyLeading: automaticallyImplyLeading,
            context: context);
        Middle = CreateMiddle(
            middleKey: keys.MiddleKey,
            userMiddle: userMiddle,
            userLargeTitle: userLargeTitle,
            route: route,
            automaticallyImplyTitle: automaticallyImplyTitle,
            large: large,
            staticBar: staticBar,
            context: context);
        Trailing = CreateTrailing(
            trailingKey: keys.TrailingKey,
            userTrailing: userTrailing,
            padding: padding,
            context: context);
        LargeTitle = CreateLargeTitle(
            largeTitleKey: keys.LargeTitleKey,
            userLargeTitle: userLargeTitle,
            route: route,
            automaticImplyTitle: automaticallyImplyTitle,
            large: large,
            context: context);
        NavBarBottom = CreateNavBarBottom(
            navBarBottomKey: keys.NavBarBottomKey,
            userBottom: userBottom,
            context: context);
    }

    public KeyedSubtree? Leading { get; }

    public KeyedSubtree? BackChevron { get; }

    public KeyedSubtree? BackLabel { get; }

    public KeyedSubtree? Middle { get; }

    public KeyedSubtree? Trailing { get; }

    public KeyedSubtree? LargeTitle { get; }

    public KeyedSubtree? NavBarBottom { get; }

    private static Widget? DerivedTitle(bool automaticallyImplyTitle, ModalRoute? currentRoute)
    {
        // Auto use the CupertinoPageRoute's title if middle not provided.
        if (automaticallyImplyTitle
            && currentRoute is ICupertinoRouteTransition { Title: not null } cupertinoRoute)
        {
            return new Text(cupertinoRoute.Title!);
        }

        return null;
    }

    private static KeyedSubtree? CreateLeading(
        GlobalKey leadingKey,
        Widget? userLeading,
        ModalRoute? route,
        bool automaticallyImplyLeading,
        EdgeInsetsDirectional? padding,
        BuildContext context)
    {
        Widget? leadingContent = null;

        if (userLeading != null)
        {
            leadingContent = userLeading;
        }
        else if (automaticallyImplyLeading
                 && route is PageRoute { FullscreenDialog: true } pageRoute
                 && pageRoute.CanPop)
        {
            leadingContent = new CupertinoButton(
                padding: EdgeInsetsDirectional.Zero,
                onPressed: () => pageRoute.Navigator!.MaybePop(),
                child: new Text(CupertinoLocalizations.Of(context).CancelButtonLabel));
        }

        if (leadingContent == null)
        {
            return null;
        }

        return new KeyedSubtree(
            key: leadingKey,
            child: new Padding(
                new EdgeInsetsDirectional(padding?.Start ?? NavBarStatics.NavBarEdgePadding, 0.0, 0.0, 0.0),
                child: new MediaQuery(
                    MediaQuery.Of(context).CopyWith(textScaler: ClampedTextScaler(context)),
                    IconTheme.Merge(
                        data: new IconThemeData(Size: 32.0),
                        child: leadingContent))));
    }

    private static KeyedSubtree? CreateBackChevron(
        GlobalKey backChevronKey,
        Widget? userLeading,
        ModalRoute? route,
        bool automaticallyImplyLeading,
        BuildContext context)
    {
        if (userLeading != null
            || !automaticallyImplyLeading
            || route == null
            || !route.CanPop
            || (route is PageRoute { FullscreenDialog: true }))
        {
            return null;
        }

        return new KeyedSubtree(
            key: backChevronKey,
            child: new MediaQuery(
                MediaQuery.Of(context).CopyWith(textScaler: ClampedTextScaler(context)),
                new BackChevron()));
    }

    // This widget is not decorated with a font since the font style could animate during transitions.
    private static KeyedSubtree? CreateBackLabel(
        GlobalKey backLabelKey,
        Widget? userLeading,
        ModalRoute? route,
        bool automaticallyImplyLeading,
        string? previousPageTitle,
        BuildContext context)
    {
        if (userLeading != null
            || !automaticallyImplyLeading
            || route == null
            || !route.CanPop
            || (route is PageRoute { FullscreenDialog: true }))
        {
            return null;
        }

        return new KeyedSubtree(
            key: backLabelKey,
            child: new MediaQuery(
                MediaQuery.Of(context).CopyWith(textScaler: ClampedTextScaler(context)),
                new BackLabel(specifiedPreviousTitle: previousPageTitle, route: route)));
    }

    // This widget is not decorated with a font since the font style could animate during transitions.
    private static KeyedSubtree? CreateMiddle(
        GlobalKey middleKey,
        Widget? userMiddle,
        Widget? userLargeTitle,
        bool large,
        bool staticBar,
        bool automaticallyImplyTitle,
        ModalRoute? route,
        BuildContext context)
    {
        Widget? middleContent = userMiddle;

        if (large && staticBar)
        {
            // Static bar only displays the middle, or the large, not both.
            // A scrolling bar creates both middle and large to transition between.
            return null;
        }

        if (large)
        {
            middleContent ??= userLargeTitle;
        }

        middleContent ??= DerivedTitle(
            automaticallyImplyTitle: automaticallyImplyTitle,
            currentRoute: route);

        if (middleContent == null)
        {
            return null;
        }

        return new KeyedSubtree(
            key: middleKey,
            child: new MediaQuery(
                MediaQuery.Of(context).CopyWith(textScaler: ClampedTextScaler(context)),
                middleContent));
    }

    private static KeyedSubtree? CreateTrailing(
        GlobalKey trailingKey,
        Widget? userTrailing,
        EdgeInsetsDirectional? padding,
        BuildContext context)
    {
        if (userTrailing == null)
        {
            return null;
        }

        return new KeyedSubtree(
            key: trailingKey,
            child: new Padding(
                new EdgeInsetsDirectional(0.0, 0.0, padding?.End ?? NavBarStatics.NavBarEdgePadding, 0.0),
                child: new MediaQuery(
                    MediaQuery.Of(context).CopyWith(textScaler: ClampedTextScaler(context)),
                    IconTheme.Merge(
                        data: new IconThemeData(Size: 32.0),
                        child: userTrailing))));
    }

    // This widget is not decorated with a font since the font style could animate during transitions.
    private static KeyedSubtree? CreateLargeTitle(
        GlobalKey largeTitleKey,
        Widget? userLargeTitle,
        bool large,
        bool automaticImplyTitle,
        ModalRoute? route,
        BuildContext context)
    {
        if (!large)
        {
            return null;
        }

        Widget? largeTitleContent = userLargeTitle ?? DerivedTitle(
            automaticallyImplyTitle: automaticImplyTitle,
            currentRoute: route);

        if (largeTitleContent == null)
        {
            throw new InvalidOperationException(
                "largeTitle was not provided and there was no title from the route.");
        }

        return new KeyedSubtree(
            key: largeTitleKey,
            child: new MediaQuery(
                MediaQuery.Of(context).CopyWith(
                    textScaler: TextScaler.Linear(
                        NavBarStatics.DampScaleFactor(
                            MediaQuery.TextScalerOf(context).Scale(NavBarStatics.NavBarLargeTitleHeightExtension),
                            NavBarStatics.NavBarLargeTitleHeightExtension,
                            NavBarStatics.LargeTitleScaleDampingRatio))),
                largeTitleContent));
    }

    private static KeyedSubtree CreateNavBarBottom(
        GlobalKey navBarBottomKey,
        Widget? userBottom,
        BuildContext context)
    {
        return new KeyedSubtree(
            key: navBarBottomKey,
            child: new MediaQuery(
                MediaQuery.Of(context).CopyWith(textScaler: MediaQuery.TextScalerOf(context)),
                userBottom ?? new SizedBox(width: 0.0, height: 0.0)));
    }

    private static TextScaler ClampedTextScaler(BuildContext context)
    {
        return MediaQuery.TextScalerOf(context)
            .Clamp(minScaleFactor: 1.0, maxScaleFactor: NavBarStatics.MaxScaleFactor);
    }
}

/// <summary>
/// Dart's `_PersistentNavigationBar`: the top part of the navigation bar that's never scrolled away.
/// </summary>
internal sealed class PersistentNavigationBar : StatelessWidget
{
    public PersistentNavigationBar(
        NavigationBarStaticComponents components,
        EdgeInsetsDirectional? padding = null,
        bool? middleVisible = null)
    {
        Components = components;
        Padding = padding;
        MiddleVisible = middleVisible;
    }

    public NavigationBarStaticComponents Components { get; }

    public EdgeInsetsDirectional? Padding { get; }

    /// <summary>
    /// Whether the middle widget has a visible animated opacity. A null value means the middle
    /// opacity will not be animated.
    /// </summary>
    public bool? MiddleVisible { get; }

    public override Widget Build(BuildContext context)
    {
        Widget? middle = Components.Middle;

        if (middle != null)
        {
            middle = new DefaultTextStyle(
                style: CupertinoTheme.Of(context).TextTheme.NavTitleTextStyle,
                child: new Semantics(flags: SemanticsFlags.IsHeader, child: middle));
            // When the middle's visibility can change on the fly like with large title slivers,
            // wrap with animated opacity.
            middle = MiddleVisible == null
                ? middle
                : new AnimatedOpacity(
                    opacity: MiddleVisible.Value ? 1.0 : 0.0,
                    duration: NavBarStatics.NavBarTitleFadeDuration,
                    child: middle);
        }

        Widget? leading = Components.Leading;
        Widget? backChevron = Components.BackChevron;
        Widget? backLabel = Components.BackLabel;

        if (leading == null
            && backChevron != null
            && backLabel != null
            && !CupertinoSheetRoute<dynamic>.HasParentSheet(context))
        {
            leading = CupertinoNavigationBarBackButton.Assemble(backChevron, backLabel);
        }
        else
        {
            leading = new Align(widthFactor: 1.0, child: leading);
        }

        Widget paddedToolbar = new NavigationToolbar(
            leading: leading,
            middle: middle,
            trailing: Components.Trailing,
            middleSpacing: 6.0);

        if (Padding != null)
        {
            paddedToolbar = new Padding(
                new Thickness(0.0, Padding.Value.Top, 0.0, Padding.Value.Bottom),
                child: paddedToolbar);
        }

        return new SizedBox(
            height: NavBarStatics.NavBarPersistentHeight + MediaQuery.PaddingOf(context).Top,
            child: new SafeArea(
                top: !CupertinoSheetRoute<dynamic>.HasParentSheet(context),
                bottom: false,
                child: paddedToolbar));
    }
}

/// <summary>
/// Dart's `_LargeTitle`: the large title of the navigation bar. Magnifies on over-scroll when
/// <see cref="CupertinoSliverNavigationBar"/>'s stretch parameter is true.
/// </summary>
internal sealed class LargeTitleWidget : SingleChildRenderObjectWidget
{
    public LargeTitleWidget(double height, Widget? child = null) : base(child)
    {
        Height = height;
    }

    public double Height { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderLargeTitle(
            alignment: AlignmentDirectional.BottomStart.Resolve(Directionality.Of(context)),
            height: Height);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var renderLargeTitle = (RenderLargeTitle)renderObject;
        renderLargeTitle.Alignment = AlignmentDirectional.BottomStart.Resolve(Directionality.Of(context));
        renderLargeTitle.Height = Height;
    }
}

/// <summary>Dart's `_RenderLargeTitle` (a `RenderShiftedBox` in Flutter).</summary>
internal sealed class RenderLargeTitle : RenderProxyBox
{
    private Alignment _alignment;
    private double _height;
    private double _scale = 1.0;

    public RenderLargeTitle(Alignment alignment, double height)
    {
        _alignment = alignment;
        _height = height;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public double Height
    {
        get => _height;
        set
        {
            if (_height == value)
            {
                return;
            }

            _height = value;
            MarkNeedsLayout();
        }
    }

    internal double Scale => _scale;

    private static double ComputeTitleScale(Size childSize, BoxConstraints constraints, double height)
    {
        double maxHeight = height - NavBarStatics.NavBarBottomPadding;
        double scale = 1.0 + (0.03 * (constraints.MaxHeight - maxHeight) / maxHeight);
        double maxScale = childSize.Width != 0.0
            ? Math.Clamp(constraints.MaxWidth / childSize.Width, 1.0, 1.1)
            : 1.1;
        return Math.Clamp(scale, 1.0, maxScale);
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        double? distance = Child?.GetDistanceToBaseline(baseline, onlyReal: true);
        if (distance == null)
        {
            return null;
        }

        var childParentData = (BoxParentData)Child!.parentData!;
        return childParentData.offset.Y + (distance.Value * _scale);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        RenderBox? child = Child;
        if (child == null)
        {
            return null;
        }

        BoxConstraints childConstraints = constraints.WidthConstraints().Loosen();
        double? result = child.GetDryBaseline(childConstraints, baseline);
        if (result == null)
        {
            return null;
        }

        Size childSize = child.GetDryLayout(childConstraints);
        double scale = ComputeTitleScale(childSize, constraints, _height);
        var scaledChildSize = new Size(childSize.Width * scale, childSize.Height * scale);
        return (result.Value * scale) + _alignment.AlongOffset(constraints.Biggest, scaledChildSize).Y;
    }

    protected override void PerformLayout()
    {
        RenderBox? child = Child;
        Size = Constraints.Biggest;

        if (child == null)
        {
            return;
        }

        BoxConstraints childConstraints = Constraints.WidthConstraints().Loosen();
        child.Layout(childConstraints, parentUsesSize: true);
        _scale = ComputeTitleScale(child.Size, Constraints, _height);
        var childParentData = (BoxParentData)child.parentData!;
        var scaledChildSize = new Size(child.Size.Width * _scale, child.Size.Height * _scale);
        childParentData.offset = _alignment.AlongOffset(Size, scaledChildSize);
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        base.ApplyPaintTransform(child, transform);
        transform.ScaleByDouble(_scale, _scale, _scale, 1.0);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        RenderBox? child = Child;
        if (child == null)
        {
            return;
        }

        var childParentData = (BoxParentData)child.parentData!;
        var transform = Matrix4.TranslationValues(
            offset.X + childParentData.offset.X,
            offset.Y + childParentData.offset.Y,
            0.0);
        transform.ScaleByDouble(_scale, _scale, 1.0, 1.0);
        ctx.PushTransform(transform, innerContext => innerContext.PaintChild(child, new Point(0.0, 0.0)));
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        RenderBox? child = Child;
        if (child == null)
        {
            return false;
        }

        Point childOffset = ((BoxParentData)child.parentData!).offset;

        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(1.0 / _scale, 1.0 / _scale, 1.0, 1.0);
        transform.TranslateByDouble(-childOffset.X, -childOffset.Y, 0.0, 1.0);

        return result.AddWithRawTransform(
            transform: transform,
            position: position,
            hitTest: (innerResult, transformed) => child.HitTest(innerResult, transformed));
    }
}

/// <summary>Dart's `_LargeTitleNavigationBarSliverDelegate`.</summary>
internal sealed class LargeTitleNavigationBarSliverDelegate : SliverPersistentHeaderDelegate
{
    public LargeTitleNavigationBarSliverDelegate(
        NavigationBarStaticComponentsKeys keys,
        NavigationBarStaticComponents components,
        Widget? userMiddle,
        Color backgroundColor,
        bool automaticBackgroundVisibility,
        PlatformBrightness? brightness,
        Border? border,
        EdgeInsetsDirectional? padding,
        Color actionsForegroundColor,
        bool transitionBetweenRoutes,
        object heroTag,
        double persistentHeight,
        double largeTitleHeight,
        bool alwaysShowMiddle,
        OverScrollHeaderStretchConfiguration? stretchConfiguration,
        bool enableBackgroundFilterBlur,
        NavigationBarBottomMode bottomMode,
        double bottomHeight,
        Plumix.AnimationController controller,
        bool searchable)
    {
        Keys = keys;
        Components = components;
        UserMiddle = userMiddle;
        BackgroundColor = backgroundColor;
        AutomaticBackgroundVisibility = automaticBackgroundVisibility;
        Brightness = brightness;
        Border = border;
        Padding = padding;
        ActionsForegroundColor = actionsForegroundColor;
        TransitionBetweenRoutes = transitionBetweenRoutes;
        HeroTag = heroTag;
        PersistentHeight = persistentHeight;
        LargeTitleHeight = largeTitleHeight;
        AlwaysShowMiddle = alwaysShowMiddle;
        _stretchConfiguration = stretchConfiguration;
        EnableBackgroundFilterBlur = enableBackgroundFilterBlur;
        BottomMode = bottomMode;
        BottomHeight = bottomHeight;
        Controller = controller;
        Searchable = searchable;
    }

    private readonly OverScrollHeaderStretchConfiguration? _stretchConfiguration;

    public NavigationBarStaticComponentsKeys Keys { get; }

    public NavigationBarStaticComponents Components { get; }

    public Widget? UserMiddle { get; }

    public Color BackgroundColor { get; }

    public bool AutomaticBackgroundVisibility { get; }

    public PlatformBrightness? Brightness { get; }

    public Border? Border { get; }

    public EdgeInsetsDirectional? Padding { get; }

    public Color ActionsForegroundColor { get; }

    public bool TransitionBetweenRoutes { get; }

    public object HeroTag { get; }

    public double PersistentHeight { get; }

    public double LargeTitleHeight { get; }

    public bool AlwaysShowMiddle { get; }

    public bool EnableBackgroundFilterBlur { get; }

    public NavigationBarBottomMode BottomMode { get; }

    public double BottomHeight { get; }

    public Plumix.AnimationController Controller { get; }

    public bool Searchable { get; }

    public override double MinExtent =>
        PersistentHeight + (BottomMode == NavigationBarBottomMode.Always ? BottomHeight : 0.0);

    public override double MaxExtent => PersistentHeight + LargeTitleHeight + BottomHeight;

    public override OverScrollHeaderStretchConfiguration? StretchConfiguration => _stretchConfiguration;

    public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
    {
        double largeTitleThreshold = MaxExtent - MinExtent - NavBarStatics.NavBarShowLargeTitleThreshold;
        bool showLargeTitle = shrinkOffset < largeTitleThreshold;

        // Calculate how much the bottom should shrink.
        double bottomShrinkFactor = NavBarStatics.ClampDouble(shrinkOffset / BottomHeight, 0.0, 1.0);

        double shrinkAnimationValue = Math.Clamp(
            (shrinkOffset - largeTitleThreshold - NavBarStatics.NavBarScrollUnderAnimationExtent)
            / NavBarStatics.NavBarScrollUnderAnimationExtent,
            0.0,
            1.0);

        var persistentNavigationBar = new PersistentNavigationBar(
            components: Components,
            padding: Padding,
            // If a user specified middle exists, always show it. Otherwise, show title when sliver
            // is collapsed.
            middleVisible: AlwaysShowMiddle ? null : !showLargeTitle);

        Color? parentPageScaffoldBackgroundColor = CupertinoPageScaffoldBackgroundColor.MaybeOf(context);

        bool automaticallyTransparent = AutomaticBackgroundVisibility
                                        && parentPageScaffoldBackgroundColor != null;
        Border? initialBorder = automaticallyTransparent ? NavBarStatics.TransparentNavBarBorder : Border;
        Border? effectiveBorder = Border == null
            ? null
            : Plumix.Rendering.Border.Lerp(initialBorder, Border, shrinkAnimationValue);

        Color effectiveBackgroundColor = automaticallyTransparent
            ? ColorUtilities.Lerp(parentPageScaffoldBackgroundColor!.Value, BackgroundColor, shrinkAnimationValue)
            : BackgroundColor;

        var stackChildren = new List<Widget>
        {
            new Positioned(
                top: PersistentHeight,
                left: 0.0,
                right: 0.0,
                bottom: BottomMode == NavigationBarBottomMode.Automatic
                    ? BottomHeight * (1.0 - bottomShrinkFactor)
                    : 0.0,
                child: new ClipRect(
                    child: new Padding(
                        new EdgeInsetsDirectional(
                            NavBarStatics.NavBarEdgePadding,
                            0.0,
                            0.0,
                            NavBarStatics.NavBarBottomPadding),
                        child: new SafeArea(
                            top: false,
                            bottom: false,
                            child: new AnimatedOpacity(
                                // Fade the large title as the search field animates from its
                                // expanded to its collapsed state.
                                opacity: showLargeTitle && !Controller.Status.IsForwardOrCompleted()
                                    ? 1.0
                                    : 0.0,
                                duration: NavBarStatics.NavBarTitleFadeDuration,
                                child: new Semantics(
                                    flags: SemanticsFlags.IsHeader,
                                    child: new DefaultTextStyle(
                                        style: CupertinoTheme.Of(context).TextTheme.NavLargeTitleTextStyle,
                                        maxLines: 1,
                                        overflow: TextOverflow.Ellipsis,
                                        child: new LargeTitleWidget(
                                            height: LargeTitleHeight,
                                            child: Components.LargeTitle)))))))),
            new Positioned(left: 0.0, right: 0.0, top: 0.0, child: persistentNavigationBar),
        };
        if (BottomMode == NavigationBarBottomMode.Automatic)
        {
            stackChildren.Add(new Positioned(
                left: 0.0,
                right: 0.0,
                bottom: 0.0,
                child: new SizedBox(
                    height: BottomHeight * (1.0 - bottomShrinkFactor),
                    child: new ClipRect(child: Components.NavBarBottom))));
        }

        var columnChildren = new List<Widget> { new Expanded(child: new Stack(children: stackChildren)) };
        if (BottomMode == NavigationBarBottomMode.Always)
        {
            columnChildren.Add(new SizedBox(height: BottomHeight, child: Components.NavBarBottom));
        }

        Widget navBar = NavBarStatics.WrapWithBackground(
            border: effectiveBorder,
            backgroundColor: effectiveBackgroundColor,
            brightness: Brightness,
            enableBackgroundFilterBlur: EnableBackgroundFilterBlur,
            child: new DefaultTextStyle(
                style: CupertinoTheme.Of(context).TextTheme.TextStyle,
                child: new Column(children: columnChildren)));

        if (!TransitionBetweenRoutes || !NavBarStatics.IsTransitionable(context))
        {
            return navBar;
        }

        return new Hero(
            tag: ReferenceEquals(HeroTag, NavBarStatics.DefaultHeroTag)
                ? new NavigationBarHeroTag(Navigator.Of(context))
                : HeroTag,
            createRectTween: NavBarTransitions.LinearTranslateWithLargestRectSizeTween,
            flightShuttleBuilder: NavBarTransitions.NavBarHeroFlightShuttleBuilder,
            placeholderBuilder: NavBarTransitions.NavBarHeroLaunchPadBuilder,
            transitionOnUserGestures: true,
            // This is all the way down here instead of being at the top level of
            // CupertinoSliverNavigationBar like CupertinoNavigationBar because it needs to wrap the
            // top level RenderBox rather than a RenderSliver.
            child: new TransitionableNavigationBar(
                componentsKeys: Keys,
                backgroundColor: effectiveBackgroundColor,
                backButtonTextStyle: CupertinoTheme.Of(context).TextTheme.NavActionTextStyle,
                titleTextStyle: CupertinoTheme.Of(context).TextTheme.NavTitleTextStyle,
                largeTitleTextStyle: CupertinoTheme.Of(context).TextTheme.NavLargeTitleTextStyle,
                border: effectiveBorder,
                hasUserMiddle: UserMiddle != null && (AlwaysShowMiddle || !showLargeTitle),
                largeExpanded: showLargeTitle,
                searchable: Searchable,
                automaticBackgroundVisibility: AutomaticBackgroundVisibility,
                child: navBar));
    }

    public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate)
    {
        if (oldDelegate is not LargeTitleNavigationBarSliverDelegate old)
        {
            return true;
        }

        return !ReferenceEquals(Components, old.Components)
               || !ReferenceEquals(UserMiddle, old.UserMiddle)
               || BackgroundColor != old.BackgroundColor
               || AutomaticBackgroundVisibility != old.AutomaticBackgroundVisibility
               || !Equals(Border, old.Border)
               || !Nullable.Equals(Padding, old.Padding)
               || ActionsForegroundColor != old.ActionsForegroundColor
               || TransitionBetweenRoutes != old.TransitionBetweenRoutes
               || PersistentHeight != old.PersistentHeight
               || LargeTitleHeight != old.LargeTitleHeight
               || AlwaysShowMiddle != old.AlwaysShowMiddle
               || !Equals(HeroTag, old.HeroTag)
               || EnableBackgroundFilterBlur != old.EnableBackgroundFilterBlur
               || BottomMode != old.BottomMode
               || BottomHeight != old.BottomHeight
               || !ReferenceEquals(Controller, old.Controller)
               || Searchable != old.Searchable;
    }
}

/// <summary>Dart's `_CancelButton`: the Cancel button of a searchable navigation bar.</summary>
internal sealed class NavBarCancelButton : StatelessWidget
{
    public NavBarCancelButton(Action? onPressed, double opacity = 1.0)
    {
        OnPressed = onPressed;
        Opacity = opacity;
    }

    public Action? OnPressed { get; }

    public double Opacity { get; }

    public override Widget Build(BuildContext context)
    {
        var localizations = CupertinoLocalizations.Of(context);
        return new Builder(builder: builderContext => MediaQuery.WithNoTextScaling(
            builderContext,
            new Align(
                alignment: Alignment.CenterLeft,
                child: new Opacity(
                    opacity: Opacity,
                    child: new CupertinoButton(
                        padding: EdgeInsetsDirectional.Zero,
                        onPressed: OnPressed,
                        child: new Text(
                            localizations.CancelButtonLabel,
                            maxLines: 1,
                            overflow: TextOverflow.Clip))))));
    }
}

/// <summary>
/// Dart's `_InactiveSearchableBottom`: the bottom of a searchable navigation bar when the search
/// field is inactive.
/// </summary>
internal sealed class InactiveSearchableBottom : StatelessWidget
{
    public InactiveSearchableBottom(
        Plumix.AnimationController animationController,
        Widget? searchField,
        Animation<double> animation,
        double searchFieldHeight,
        Action? onSearchFieldTap)
    {
        AnimationController = animationController;
        SearchField = searchField;
        Animation = animation;
        SearchFieldHeight = searchFieldHeight;
        OnSearchFieldTap = onSearchFieldTap;
    }

    public Plumix.AnimationController AnimationController { get; }

    public Widget? SearchField { get; }

    public Animation<double> Animation { get; }

    public double SearchFieldHeight { get; }

    public Action? OnSearchFieldTap { get; }

    public override Widget Build(BuildContext context)
    {
        return new AnimatedBuilder(
            animation: Animation,
            child: new GestureDetector(
                onTap: OnSearchFieldTap,
                child: new AbsorbPointer(
                    child: new FocusableActionDetector(
                        descendantsAreFocusable: false,
                        child: new Padding(
                            new EdgeInsetsDirectional(
                                NavBarStatics.NavBarEdgePadding,
                                0.0,
                                NavBarStatics.NavBarEdgePadding,
                                NavBarStatics.NavBarBottomPadding),
                            child: new SizedBox(height: SearchFieldHeight, child: SearchField))))),
            builder: (builderContext, child) => new LayoutBuilder(
                builder: (layoutContext, constraints) => new Row(
                    children:
                    [
                        new SizedBox(
                            width: constraints.MaxWidth
                                   - (NavBarStatics.SearchFieldCancelButtonWidth * AnimationController.Value),
                            child: child),
                        // A decoy 'Cancel' button used in the collapsed-to-expanded animation.
                        new SizedBox(
                            width: AnimationController.Value * NavBarStatics.SearchFieldCancelButtonWidth,
                            child: new Padding(
                                new Thickness(0.0, 0.0, 0.0, NavBarStatics.NavBarBottomPadding),
                                child: new NavBarCancelButton(opacity: 0.4, onPressed: () => { }))),
                    ])));
    }
}

/// <summary>
/// Dart's `_ActiveSearchableBottom`: the bottom of a searchable navigation bar when the search
/// field is active.
/// </summary>
internal sealed class ActiveSearchableBottom : StatelessWidget
{
    public ActiveSearchableBottom(
        Plumix.AnimationController animationController,
        Widget? searchField,
        Animation<double> animation,
        double searchFieldHeight,
        Action? onSearchFieldTap)
    {
        AnimationController = animationController;
        SearchField = searchField;
        Animation = animation;
        SearchFieldHeight = searchFieldHeight;
        OnSearchFieldTap = onSearchFieldTap;
    }

    public Plumix.AnimationController AnimationController { get; }

    public Widget? SearchField { get; }

    public Animation<double> Animation { get; }

    public double SearchFieldHeight { get; }

    public Action? OnSearchFieldTap { get; }

    public override Widget Build(BuildContext context)
    {
        return new Padding(
            new EdgeInsetsDirectional(
                NavBarStatics.NavBarEdgePadding,
                0.0,
                0.0,
                NavBarStatics.NavBarBottomPadding),
            child: new Row(
                spacing: 12.0, // Eyeballed on an iPhone 15 simulator running iOS 17.5.
                children:
                [
                    new Expanded(
                        child: new SizedBox(
                            height: SearchFieldHeight,
                            child: SearchField ?? new SizedBox(width: 0.0, height: 0.0))),
                    new AnimatedBuilder(
                        animation: Animation,
                        child: new FadeTransition(
                            opacity: new DoubleTween(begin: 0.0, end: 1.0).Animate(AnimationController),
                            child: new NavBarCancelButton(onPressed: OnSearchFieldTap)),
                        builder: (builderContext, child) => new SizedBox(
                            width: AnimationController.Value * NavBarStatics.SearchFieldCancelButtonWidth,
                            child: child)),
                ]));
    }
}
