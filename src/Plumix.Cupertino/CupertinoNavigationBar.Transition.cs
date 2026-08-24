using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using RelativeRect = Plumix.Rendering.RelativeRect;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/nav_bar.dart

/// <summary>Dart's `Tween&lt;Offset&gt;` instantiation used by the nav bar transition.</summary>
internal sealed class NavBarPointTween : Plumix.Tween<Point>
{
    public NavBarPointTween(Point? begin = null, Point? end = null)
    {
        if (begin.HasValue)
        {
            SetBeginValue(begin.Value);
        }

        if (end.HasValue)
        {
            SetEndValue(end.Value);
        }
    }

    public override Point Lerp(Point a, Point b, double t)
    {
        return new Point(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
    }
}

/// <summary>
/// Dart's `_FixedSizeSlidingTransition`: imposes a fixed size on its child and shifts it in the
/// parent stack, driven by `offsetAnimation`.
/// </summary>
internal sealed class FixedSizeSlidingTransition : AnimatedWidget
{
    public FixedSizeSlidingTransition(
        bool isLtr,
        Animation<Point> offsetAnimation,
        double width,
        double height,
        Widget child) : base(offsetAnimation)
    {
        IsLtr = isLtr;
        OffsetAnimation = offsetAnimation;
        Width = width;
        Height = height;
        Child = child;
    }

    /// <summary>Whether the writing direction used in the navigation bar transition is LTR.</summary>
    public bool IsLtr { get; }

    public double Width { get; }

    public double Height { get; }

    /// <summary>
    /// The animated offset from the top-leading corner of the stack. When <see cref="IsLtr"/> is
    /// false, the x-axis runs right to left starting at the stack's top right corner.
    /// </summary>
    public Animation<Point> OffsetAnimation { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new Positioned(
            top: OffsetAnimation.Value.Y,
            left: IsLtr ? OffsetAnimation.Value.X : null,
            right: IsLtr ? null : OffsetAnimation.Value.X,
            width: Width,
            height: Height,
            child: Child);
    }
}

/// <summary>
/// Dart's `_TransitionableNavigationBar`: the immediate child of the nav bar's Hero; carries the
/// component keys and paint-time metadata that hero flights read.
/// </summary>
internal sealed class TransitionableNavigationBar : StatelessWidget
{
    public TransitionableNavigationBar(
        NavigationBarStaticComponentsKeys componentsKeys,
        Color? backgroundColor,
        TextStyle backButtonTextStyle,
        TextStyle titleTextStyle,
        TextStyle? largeTitleTextStyle,
        Border? border,
        bool hasUserMiddle,
        bool largeExpanded,
        bool searchable,
        bool automaticBackgroundVisibility,
        Widget child) : base(componentsKeys.NavBarBoxKey)
    {
        if (largeExpanded && largeTitleTextStyle == null)
        {
            throw new ArgumentException(
                "largeTitleTextStyle cannot be null when largeExpanded is true.",
                nameof(largeTitleTextStyle));
        }

        ComponentsKeys = componentsKeys;
        BackgroundColor = backgroundColor;
        BackButtonTextStyle = backButtonTextStyle;
        TitleTextStyle = titleTextStyle;
        LargeTitleTextStyle = largeTitleTextStyle;
        Border = border;
        HasUserMiddle = hasUserMiddle;
        LargeExpanded = largeExpanded;
        Searchable = searchable;
        AutomaticBackgroundVisibility = automaticBackgroundVisibility;
        Child = child;
    }

    public NavigationBarStaticComponentsKeys ComponentsKeys { get; }

    public Color? BackgroundColor { get; }

    public TextStyle BackButtonTextStyle { get; }

    public TextStyle TitleTextStyle { get; }

    public TextStyle? LargeTitleTextStyle { get; }

    public Border? Border { get; }

    public bool HasUserMiddle { get; }

    public bool LargeExpanded { get; }

    public bool Searchable { get; }

    public bool AutomaticBackgroundVisibility { get; }

    public Widget Child { get; }

    public RenderBox RenderBox
    {
        get
        {
            var box = (RenderBox)ComponentsKeys.NavBarBoxKey.CurrentContext!.Value.FindRenderObject()!;
            if (!box.Attached)
            {
                throw new InvalidOperationException(
                    "TransitionableNavigationBar.RenderBox should be called when building hero "
                    + "flight shuttles when the from and the to nav bar boxes are already laid out "
                    + "and painted.");
            }

            return box;
        }
    }

    public bool UserGestureInProgress =>
        Navigator.Of(ComponentsKeys.NavBarBoxKey.CurrentContext!.Value).UserGestureInProgress;

    public override Widget Build(BuildContext context)
    {
        return Child;
    }
}

/// <summary>
/// Dart's `_NavigationBarTransition`: the widget in the Hero flight, built from the inner
/// components of both static navigation bars. `topNavBar` is the nav bar that was on top and
/// `bottomNavBar` the one at the bottom regardless of the push/pop direction.
/// </summary>
internal sealed class NavigationBarTransition : StatelessWidget
{
    public NavigationBarTransition(
        Animation<double> animation,
        TransitionableNavigationBar topNavBar,
        TransitionableNavigationBar bottomNavBar)
    {
        Animation = animation;
        TopNavBar = topNavBar;
        BottomNavBar = bottomNavBar;
        HeightTween = new DoubleTween(
            begin: bottomNavBar.RenderBox.Size.Height,
            end: topNavBar.RenderBox.Size.Height);
    }

    public Animation<double> Animation { get; }

    public TransitionableNavigationBar TopNavBar { get; }

    public TransitionableNavigationBar BottomNavBar { get; }

    public DoubleTween HeightTween { get; }

    public override Widget Build(BuildContext context)
    {
        var componentsTransition = new NavigationBarComponentsTransition(
            animation: Animation,
            bottomNavBar: BottomNavBar,
            topNavBar: TopNavBar,
            directionality: Directionality.Of(context));

        var candidates = new Widget?[]
        {
            componentsTransition.BottomNavBarBackground,
            componentsTransition.BottomBackChevron,
            componentsTransition.BottomBackLabel,
            componentsTransition.BottomLeading,
            componentsTransition.BottomMiddle,
            componentsTransition.BottomLargeTitle,
            componentsTransition.BottomTrailing,
            componentsTransition.BottomNavBarBottom,
            // Draw top components on top of the bottom components.
            componentsTransition.TopNavBarBackground,
            componentsTransition.TopLeading,
            componentsTransition.TopBackChevron,
            componentsTransition.TopBackLabel,
            componentsTransition.TopMiddle,
            componentsTransition.TopLargeTitle,
            componentsTransition.TopTrailing,
            componentsTransition.TopNavBarBottom,
        };
        var children = new List<Widget>();
        foreach (Widget? candidate in candidates)
        {
            if (candidate != null)
            {
                children.Add(candidate);
            }
        }

        // The text scaling is disabled to avoid odd transitions between pages.
        return new Builder(builder: builderContext => MediaQuery.WithNoTextScaling(
            builderContext,
            new SizedBox(
                height: Math.Max(HeightTween.GetBeginValue(), HeightTween.GetEndValue())
                        + MediaQuery.PaddingOf(builderContext).Top,
                width: double.PositiveInfinity,
                child: new Stack(children: children))));
    }
}

/// <summary>
/// Dart's `_NavigationBarComponentsTransition`: creates the transitional widgets from the static
/// components of the bottom and top navigation bars, replicating their existing layout using
/// `Positioned`/`PositionedTransition` wrappers. Never returns the `KeyedSubtree`s themselves —
/// only their children — to avoid global key duplication.
/// </summary>
internal sealed class NavigationBarComponentsTransition
{
    private static readonly Plumix.Animatable<double> FadeOutTween = new DoubleTween(begin: 1.0, end: 0.0);
    private static readonly Plumix.Animatable<double> FadeInTween = new DoubleTween(begin: 0.0, end: 1.0);

    public NavigationBarComponentsTransition(
        Animation<double> animation,
        TransitionableNavigationBar bottomNavBar,
        TransitionableNavigationBar topNavBar,
        TextDirection directionality)
    {
        Animation = animation;
        BottomComponents = bottomNavBar.ComponentsKeys;
        TopComponents = topNavBar.ComponentsKeys;
        BottomNavBarBox = bottomNavBar.RenderBox;
        TopNavBarBox = topNavBar.RenderBox;
        BottomBackButtonTextStyle = bottomNavBar.BackButtonTextStyle;
        TopBackButtonTextStyle = topNavBar.BackButtonTextStyle;
        BottomTitleTextStyle = bottomNavBar.TitleTextStyle;
        TopTitleTextStyle = topNavBar.TitleTextStyle;
        BottomLargeTitleTextStyle = bottomNavBar.LargeTitleTextStyle;
        TopLargeTitleTextStyle = topNavBar.LargeTitleTextStyle;
        BottomHasUserMiddle = bottomNavBar.HasUserMiddle;
        TopHasUserMiddle = topNavBar.HasUserMiddle;
        BottomLargeExpanded = bottomNavBar.LargeExpanded;
        TopLargeExpanded = topNavBar.LargeExpanded;
        BottomBackgroundColor = bottomNavBar.BackgroundColor;
        TopBackgroundColor = topNavBar.BackgroundColor;
        BottomBorder = bottomNavBar.Border;
        TopBorder = topNavBar.Border;
        BottomAutomaticBackgroundVisibility = bottomNavBar.AutomaticBackgroundVisibility;
        UserGestureInProgress = topNavBar.UserGestureInProgress || bottomNavBar.UserGestureInProgress;
        Searchable = topNavBar.Searchable && bottomNavBar.Searchable;
        // Paint bounds are based on offset zero so it's ok to expand the rects.
        TransitionBox = ExpandToInclude(BottomNavBarBox.PaintBounds, TopNavBarBox.PaintBounds);
        ForwardDirection = directionality == TextDirection.Ltr ? 1.0 : -1.0;
    }

    public Animation<double> Animation { get; }

    public NavigationBarStaticComponentsKeys BottomComponents { get; }

    public NavigationBarStaticComponentsKeys TopComponents { get; }

    // These render boxes that are the ancestors of all the bottom and top components are used to
    // determine the components' relative positions inside their respective navigation bars.
    public RenderBox BottomNavBarBox { get; }

    public RenderBox TopNavBarBox { get; }

    public TextStyle BottomBackButtonTextStyle { get; }

    public TextStyle TopBackButtonTextStyle { get; }

    public TextStyle BottomTitleTextStyle { get; }

    public TextStyle TopTitleTextStyle { get; }

    public TextStyle? BottomLargeTitleTextStyle { get; }

    public TextStyle? TopLargeTitleTextStyle { get; }

    public bool BottomHasUserMiddle { get; }

    public bool TopHasUserMiddle { get; }

    public bool BottomLargeExpanded { get; }

    public bool TopLargeExpanded { get; }

    public bool UserGestureInProgress { get; }

    public bool Searchable { get; }

    public bool BottomAutomaticBackgroundVisibility { get; }

    public Color? BottomBackgroundColor { get; }

    public Color? TopBackgroundColor { get; }

    public Border? BottomBorder { get; }

    public Border? TopBorder { get; }

    /// <summary>
    /// The outer box in which all the components will be fitted; the sizing component of
    /// `RelativeRect`s is based on this rect's size.
    /// </summary>
    public Rect TransitionBox { get; }

    /// <summary>x-axis unity number representing the direction of growth for text.</summary>
    public double ForwardDirection { get; }

    private static Rect ExpandToInclude(Rect a, Rect b)
    {
        double left = Math.Min(a.X, b.X);
        double top = Math.Min(a.Y, b.Y);
        double right = Math.Max(a.Right, b.Right);
        double bottom = Math.Max(a.Bottom, b.Bottom);
        return new Rect(left, top, right - left, bottom - top);
    }

    private static RelativeRect Shift(RelativeRect rect, Point offset)
    {
        return new RelativeRect(
            rect.Left + offset.X,
            rect.Top + offset.Y,
            rect.Right - offset.X,
            rect.Bottom - offset.Y);
    }

    /// <summary>
    /// Takes a widget in its original ancestor navigation bar render box and translates it into a
    /// `RelativeRect` in the transition navigation bar box.
    /// </summary>
    public RelativeRect PositionInTransitionBox(GlobalKey key, RenderBox from)
    {
        var componentBox = (RenderBox)key.CurrentContext!.Value.FindRenderObject()!;
        return RelativeRect.FromRect(
            new Rect(componentBox.LocalToGlobal(new Point(0.0, 0.0), ancestor: from), componentBox.Size),
            TransitionBox);
    }

    /// <summary>
    /// Creates an animated widget that moves the given child widget between its original position
    /// in its ancestor navigation bar to another widget's position in that widget's navigation bar,
    /// anchored on the vertical middle of their respective render boxes' leading edge.
    /// </summary>
    public FixedSizeSlidingTransition SlideFromLeadingEdge(
        GlobalKey fromKey,
        RenderBox fromNavBarBox,
        GlobalKey toKey,
        RenderBox toNavBarBox,
        Widget child,
        Curve? curve = null)
    {
        curve ??= Curves.Interval(0.0, 1.0);
        var fromBox = (RenderBox)fromKey.CurrentContext!.Value.FindRenderObject()!;
        var toBox = (RenderBox)toKey.CurrentContext!.Value.FindRenderObject()!;

        bool isLtr = ForwardDirection > 0;

        // The animation moves the fromBox so its anchor (left-center or right-center depending on
        // the writing direction) aligns with toBox's anchor.
        var fromAnchorLocal = new Point(isLtr ? 0.0 : fromBox.Size.Width, fromBox.Size.Height / 2);
        var toAnchorLocal = new Point(isLtr ? 0.0 : toBox.Size.Width, toBox.Size.Height / 2);
        Point fromAnchorInFromBox = fromBox.LocalToGlobal(fromAnchorLocal, ancestor: fromNavBarBox);
        Point toAnchorInToBox = toBox.LocalToGlobal(toAnchorLocal, ancestor: toNavBarBox);

        // The offset to move fromAnchor to toAnchor, in transitionBox's top-leading coordinates.
        Point translation = isLtr
            ? toAnchorInToBox - fromAnchorInFromBox
            : new Point(toNavBarBox.Size.Width - toAnchorInToBox.X, toAnchorInToBox.Y)
              - new Point(fromNavBarBox.Size.Width - fromAnchorInFromBox.X, fromAnchorInFromBox.Y);

        RelativeRect fromBoxMargin = PositionInTransitionBox(fromKey, from: fromNavBarBox);
        var fromOriginInTransitionBox = new Point(
            isLtr ? fromBoxMargin.Left : fromBoxMargin.Right,
            fromBoxMargin.Top);

        var anchorMovementInTransitionBox = new NavBarPointTween(
            begin: fromOriginInTransitionBox,
            end: fromOriginInTransitionBox + translation);

        return new FixedSizeSlidingTransition(
            isLtr: isLtr,
            offsetAnimation: Animation.Drive(new CurveTween(curve)).Drive(anchorMovementInTransitionBox),
            width: fromNavBarBox.Size.Width,
            height: fromBox.Size.Height,
            child: child);
    }

    public Animation<double> FadeInFrom(double t, Curve? curve = null)
    {
        return Animation.Drive(FadeInTween.Chain(new CurveTween(Curves.Interval(t, 1.0, curve ?? Curves.EaseIn))));
    }

    public Animation<double> FadeOutBy(double t, Curve? curve = null)
    {
        return Animation.Drive(FadeOutTween.Chain(new CurveTween(Curves.Interval(0.0, t, curve ?? Curves.EaseOut))));
    }

    /// <summary>The parent of the hero animation, which is the route animation.</summary>
    public Animation<double> RouteAnimation =>
        Animation is Plumix.CurvedAnimation curvedAnimation ? curvedAnimation.Parent : Animation;

    public Widget? BottomNavBarBackground
    {
        get
        {
            if (BottomBackgroundColor == null
                || (BottomLargeExpanded && BottomAutomaticBackgroundVisibility))
            {
                return null;
            }

            Curve animationCurve = Animation.Status == AnimationStatus.Forward
                ? Curves.FastEaseInToSlowEaseOut
                : Curves.Flipped(Curves.FastEaseInToSlowEaseOut);

            var pageTransitionAnimation = RouteAnimation.Drive(
                new CurveTween(UserGestureInProgress ? Curves.Linear : animationCurve));

            RelativeRect from = PositionInTransitionBox(BottomComponents.NavBarBoxKey, from: BottomNavBarBox);

            var positionTween = new RelativeRectTween(
                begin: from,
                end: Shift(from, new Point(ForwardDirection * -BottomNavBarBox.Size.Width, 0.0)));

            return new PositionedTransition(
                rect: pageTransitionAnimation.Drive(positionTween),
                child: NavBarStatics.WrapWithBackground(
                    // Don't update the system status bar color mid-flight.
                    updateSystemUiOverlay: false,
                    backgroundColor: BottomBackgroundColor.Value,
                    border: TopBorder,
                    child: new SizedBox(
                        height: BottomNavBarBox.Size.Height,
                        width: double.PositiveInfinity)));
        }
    }

    public Widget? BottomLeading
    {
        get
        {
            var bottomLeading = BottomComponents.LeadingKey.CurrentWidget as KeyedSubtree;
            if (bottomLeading == null)
            {
                return null;
            }

            return Positioned.FromRelativeRect(
                rect: PositionInTransitionBox(BottomComponents.LeadingKey, from: BottomNavBarBox),
                child: new FadeTransition(opacity: FadeOutBy(0.4), child: bottomLeading.Child));
        }
    }

    public Widget? BottomBackChevron
    {
        get
        {
            var bottomBackChevron = BottomComponents.BackChevronKey.CurrentWidget as KeyedSubtree;
            if (bottomBackChevron == null)
            {
                return null;
            }

            return Positioned.FromRelativeRect(
                rect: PositionInTransitionBox(BottomComponents.BackChevronKey, from: BottomNavBarBox),
                child: new FadeTransition(
                    opacity: FadeOutBy(0.6),
                    child: new DefaultTextStyle(
                        style: BottomBackButtonTextStyle,
                        child: bottomBackChevron.Child)));
        }
    }

    public Widget? BottomBackLabel
    {
        get
        {
            var bottomBackLabel = BottomComponents.BackLabelKey.CurrentWidget as KeyedSubtree;
            if (bottomBackLabel == null)
            {
                return null;
            }

            RelativeRect from = PositionInTransitionBox(BottomComponents.BackLabelKey, from: BottomNavBarBox);

            // Transition away by sliding horizontally to the leading edge off of the screen.
            var positionTween = new RelativeRectTween(
                begin: from,
                end: Shift(from, new Point(ForwardDirection * (-BottomNavBarBox.Size.Width / 2.0), 0.0)));

            return new PositionedTransition(
                rect: Animation.Drive(positionTween),
                child: new FadeTransition(
                    opacity: FadeOutBy(0.2),
                    child: new DefaultTextStyle(
                        style: BottomBackButtonTextStyle,
                        child: bottomBackLabel.Child)));
        }
    }

    public Widget? BottomMiddle
    {
        get
        {
            var bottomMiddle = BottomComponents.MiddleKey.CurrentWidget as KeyedSubtree;
            var topBackLabel = TopComponents.BackLabelKey.CurrentWidget as KeyedSubtree;
            var topLeading = TopComponents.LeadingKey.CurrentWidget as KeyedSubtree;

            // The middle component is non-null when the nav bar is a large title nav bar but would
            // be invisible when expanded, therefore don't show it here.
            if (!BottomHasUserMiddle && BottomLargeExpanded)
            {
                return null;
            }

            if (bottomMiddle != null && topBackLabel != null)
            {
                // Move from current position to the top page's back label position.
                return SlideFromLeadingEdge(
                    fromKey: BottomComponents.MiddleKey,
                    fromNavBarBox: BottomNavBarBox,
                    toKey: TopComponents.BackLabelKey,
                    toNavBarBox: TopNavBarBox,
                    child: new FadeTransition(
                        // A custom middle widget like a segmented control fades away faster.
                        opacity: FadeOutBy(BottomHasUserMiddle ? 0.4 : 0.7),
                        child: new Align(
                            // As the text shrinks, make sure it's still anchored to the leading
                            // edge of a constantly sized outer box.
                            alignment: AlignmentDirectional.CenterStart,
                            child: new DefaultTextStyleTransition(
                                style: Animation.Drive(new TextStyleTween(
                                    begin: BottomTitleTextStyle,
                                    end: TopBackButtonTextStyle)),
                                child: bottomMiddle.Child))));
            }

            // When the top page has a leading widget override (one of the few ways to not have a
            // top back label), don't move the bottom middle widget and just fade.
            if (bottomMiddle != null && topLeading != null)
            {
                return Positioned.FromRelativeRect(
                    rect: PositionInTransitionBox(BottomComponents.MiddleKey, from: BottomNavBarBox),
                    child: new FadeTransition(
                        opacity: FadeOutBy(BottomHasUserMiddle ? 0.4 : 0.7),
                        // Keep the font when transitioning into a non-back label leading.
                        child: new DefaultTextStyle(
                            style: BottomTitleTextStyle,
                            child: bottomMiddle.Child)));
            }

            return null;
        }
    }

    public Widget? BottomLargeTitle
    {
        get
        {
            var bottomLargeTitle = BottomComponents.LargeTitleKey.CurrentWidget as KeyedSubtree;
            var topBackLabel = TopComponents.BackLabelKey.CurrentWidget as KeyedSubtree;

            if (bottomLargeTitle == null || !BottomLargeExpanded)
            {
                return null;
            }

            if (topBackLabel != null)
            {
                // Move from current position to the top page's back label position.
                return SlideFromLeadingEdge(
                    fromKey: BottomComponents.LargeTitleKey,
                    fromNavBarBox: BottomNavBarBox,
                    toKey: TopComponents.BackLabelKey,
                    toNavBarBox: TopNavBarBox,
                    curve: Curves.Interval(0.0, Animation.Status == AnimationStatus.Forward ? 0.7 : 1.0),
                    child: new FadeTransition(
                        opacity: FadeOutBy(0.6),
                        child: new Align(
                            // As the text shrinks, make sure it's still anchored to the leading
                            // edge of a constantly sized outer box.
                            alignment: AlignmentDirectional.CenterStart,
                            child: new DefaultTextStyleTransition(
                                style: Animation.Drive(new TextStyleTween(
                                    begin: BottomLargeTitleTextStyle,
                                    end: TopBackButtonTextStyle)),
                                maxLines: 1,
                                overflow: TextOverflow.Ellipsis,
                                child: bottomLargeTitle.Child))));
            }

            // Unlike bottom middle, the bottom large title moves when it can't transition to the
            // top back label position.
            RelativeRect from = PositionInTransitionBox(BottomComponents.LargeTitleKey, from: BottomNavBarBox);

            var positionTween = new RelativeRectTween(
                begin: from,
                end: Shift(from, new Point(ForwardDirection * BottomNavBarBox.Size.Width / 4.0, 0.0)));

            // Just shift slightly towards the trailing edge instead of moving to the back label
            // position.
            return new PositionedTransition(
                rect: Animation.Drive(positionTween),
                child: new FadeTransition(
                    opacity: FadeOutBy(0.4),
                    // Keep the font when transitioning into a non-back-label leading.
                    child: new DefaultTextStyle(
                        style: BottomLargeTitleTextStyle!,
                        child: bottomLargeTitle.Child)));
        }
    }

    public Widget? BottomTrailing
    {
        get
        {
            var bottomTrailing = BottomComponents.TrailingKey.CurrentWidget as KeyedSubtree;
            if (bottomTrailing == null)
            {
                return null;
            }

            return Positioned.FromRelativeRect(
                rect: PositionInTransitionBox(BottomComponents.TrailingKey, from: BottomNavBarBox),
                child: new FadeTransition(opacity: FadeOutBy(0.6), child: bottomTrailing.Child));
        }
    }

    public Widget? BottomNavBarBottom
    {
        get
        {
            var bottomNavBarBottom = BottomComponents.NavBarBottomKey.CurrentWidget as KeyedSubtree;
            if (bottomNavBarBottom == null)
            {
                return null;
            }

            RelativeRect from = PositionInTransitionBox(BottomComponents.NavBarBottomKey, from: BottomNavBarBox);
            // Shift in from the leading edge of the screen.
            var positionTween = new RelativeRectTween(
                begin: from,
                end: Shift(from, new Point(ForwardDirection * -BottomNavBarBox.Size.Width, 0.0)));

            Widget child = bottomNavBarBottom.Child;
            Curve animationCurve = Animation.Status == AnimationStatus.Forward
                ? NavBarStatics.BottomNavBarHeaderTransitionCurve
                : Curves.Flipped(NavBarStatics.BottomNavBarHeaderTransitionCurve);

            // Fade out only if this is not a CupertinoSliverNavigationBar.search to
            // CupertinoSliverNavigationBar.search transition.
            if (!Searchable)
            {
                child = new FadeTransition(opacity: FadeOutBy(0.8, curve: animationCurve), child: child);
            }

            return new PositionedTransition(
                // The bottom widget animates linearly during a backswipe by a user gesture.
                rect: UserGestureInProgress
                    ? RouteAnimation.Drive(new CurveTween(Curves.Linear)).Drive(positionTween)
                    : Animation.Drive(new CurveTween(animationCurve)).Drive(positionTween),
                child: new ClipRect(child: child));
        }
    }

    public Widget? TopNavBarBackground
    {
        get
        {
            if (TopBackgroundColor == null)
            {
                return null;
            }

            Curve animationCurve = Animation.Status == AnimationStatus.Forward
                ? Curves.FastEaseInToSlowEaseOut
                : Curves.Flipped(Curves.FastEaseInToSlowEaseOut);

            var pageTransitionAnimation = RouteAnimation.Drive(
                new CurveTween(UserGestureInProgress ? Curves.Linear : animationCurve));

            RelativeRect to = PositionInTransitionBox(TopComponents.NavBarBoxKey, from: TopNavBarBox);

            var positionTween = new RelativeRectTween(
                begin: Shift(to, new Point(ForwardDirection * TopNavBarBox.Size.Width, 0.0)),
                end: to);

            return new PositionedTransition(
                rect: pageTransitionAnimation.Drive(positionTween),
                child: NavBarStatics.WrapWithBackground(
                    // Don't update the system status bar color mid-flight.
                    updateSystemUiOverlay: false,
                    backgroundColor: TopBackgroundColor.Value,
                    border: TopBorder,
                    child: new SizedBox(height: TopNavBarBox.Size.Height, width: double.PositiveInfinity)));
        }
    }

    public Widget? TopLeading
    {
        get
        {
            var topLeading = TopComponents.LeadingKey.CurrentWidget as KeyedSubtree;
            if (topLeading == null)
            {
                return null;
            }

            return Positioned.FromRelativeRect(
                rect: PositionInTransitionBox(TopComponents.LeadingKey, from: TopNavBarBox),
                child: new FadeTransition(opacity: FadeInFrom(0.6), child: topLeading.Child));
        }
    }

    public Widget? TopBackChevron
    {
        get
        {
            var topBackChevron = TopComponents.BackChevronKey.CurrentWidget as KeyedSubtree;
            var bottomBackChevron = BottomComponents.BackChevronKey.CurrentWidget as KeyedSubtree;

            if (topBackChevron == null)
            {
                return null;
            }

            RelativeRect to = PositionInTransitionBox(TopComponents.BackChevronKey, from: TopNavBarBox);
            RelativeRect from = to;

            Widget child = topBackChevron.Child;
            // Values eyeballed from an iPhone 15 simulator running iOS 17.5.
            Curve effectiveScaleCurve;
            Curve effectivePositionCurve;
            if (Animation.Status == AnimationStatus.Forward)
            {
                effectiveScaleCurve = Curves.Interval(0.0, 0.2);
                effectivePositionCurve = Curves.Interval(0.0, 0.5);
            }
            else
            {
                effectiveScaleCurve = Curves.Interval(0.8, 1.0);
                effectivePositionCurve = Curves.Interval(0.5, 1.0);
            }

            // If it's the first page with a back chevron, shrink and shift in slightly from the
            // right.
            if (bottomBackChevron == null)
            {
                var topBackChevronBox =
                    (RenderBox)TopComponents.BackChevronKey.CurrentContext!.Value.FindRenderObject()!;
                from = Shift(
                    to,
                    new Point(ForwardDirection * topBackChevronBox.Size.Width * 2.0, 0.0));
                child = new ScaleTransition(
                    scale: RouteAnimation.Drive(new CurveTween(effectiveScaleCurve)),
                    child: child);
            }

            var positionTween = new RelativeRectTween(begin: from, end: to);

            return new PositionedTransition(
                rect: RouteAnimation.Drive(new CurveTween(effectivePositionCurve)).Drive(positionTween),
                child: new FadeTransition(
                    opacity: RouteAnimation.Drive(new CurveTween(Curves.Interval(
                        // Fades faster going back from the first page with a back chevron.
                        bottomBackChevron == null && Animation.Status != AnimationStatus.Forward
                            ? 0.9
                            : 0.4,
                        1.0))),
                    child: new DefaultTextStyle(style: TopBackButtonTextStyle, child: child)));
        }
    }

    public Widget? TopBackLabel
    {
        get
        {
            var bottomMiddle = BottomComponents.MiddleKey.CurrentWidget as KeyedSubtree;
            var bottomLargeTitle = BottomComponents.LargeTitleKey.CurrentWidget as KeyedSubtree;
            var topBackLabel = TopComponents.BackLabelKey.CurrentWidget as KeyedSubtree;

            if (topBackLabel == null)
            {
                return null;
            }

            // Flutter looks up the `RenderAnimatedOpacity` driven by `AnimatedOpacity`; Plumix's
            // `AnimatedOpacity` drives a `RenderOpacity`, so the same lookup targets that type.
            var topBackLabelOpacity = TopComponents.BackLabelKey.CurrentContext
                ?.FindAncestorRenderObjectOfType<RenderOpacity>();

            Animation<double>? midClickOpacity = null;
            if (topBackLabelOpacity != null && topBackLabelOpacity.Opacity < 1.0)
            {
                midClickOpacity = Animation.Drive(new DoubleTween(
                    begin: 0.0,
                    end: topBackLabelOpacity.Opacity));
            }

            // Pick up from an incoming transition from the large title. This is duplicated here
            // from the bottomLargeTitle transition widget because the content text might be
            // different. For instance, if the bottomLargeTitle text is too long, the topBackLabel
            // will say 'Back' instead of the original text.
            if (bottomLargeTitle != null && BottomLargeExpanded)
            {
                return SlideFromLeadingEdge(
                    fromKey: BottomComponents.LargeTitleKey,
                    fromNavBarBox: BottomNavBarBox,
                    toKey: TopComponents.BackLabelKey,
                    toNavBarBox: TopNavBarBox,
                    curve: Curves.Interval(0.0, Animation.Status == AnimationStatus.Forward ? 0.7 : 1.0),
                    child: new FadeTransition(
                        opacity: midClickOpacity ?? FadeInFrom(0.4),
                        child: new DefaultTextStyleTransition(
                            style: Animation.Drive(new TextStyleTween(
                                begin: BottomLargeTitleTextStyle,
                                end: TopBackButtonTextStyle)),
                            maxLines: 1,
                            overflow: TextOverflow.Ellipsis,
                            child: topBackLabel.Child)));
            }

            // The topBackLabel always comes from the large title first if available and expanded
            // instead of middle.
            if (bottomMiddle != null)
            {
                return SlideFromLeadingEdge(
                    fromKey: BottomComponents.MiddleKey,
                    fromNavBarBox: BottomNavBarBox,
                    toKey: TopComponents.BackLabelKey,
                    toNavBarBox: TopNavBarBox,
                    child: new FadeTransition(
                        opacity: midClickOpacity ?? FadeInFrom(0.3),
                        child: new DefaultTextStyleTransition(
                            style: Animation.Drive(new TextStyleTween(
                                begin: BottomTitleTextStyle,
                                end: TopBackButtonTextStyle)),
                            child: topBackLabel.Child)));
            }

            return null;
        }
    }

    public Widget? TopMiddle
    {
        get
        {
            var topMiddle = TopComponents.MiddleKey.CurrentWidget as KeyedSubtree;
            if (topMiddle == null)
            {
                return null;
            }

            // The middle component is non-null when the nav bar is a large title nav bar but would
            // be invisible when expanded, therefore don't show it here.
            if (!TopHasUserMiddle && TopLargeExpanded)
            {
                return null;
            }

            RelativeRect to = PositionInTransitionBox(TopComponents.MiddleKey, from: TopNavBarBox);
            var toBox = (RenderBox)TopComponents.MiddleKey.CurrentContext!.Value.FindRenderObject()!;

            bool isLtr = ForwardDirection > 0;

            // Anchor is the top-leading point of toBox, in transition box's top-leading coordinate
            // space.
            var toAnchorInTransitionBox = new Point(isLtr ? to.Left : to.Right, to.Top);

            // Shift in from the trailing edge of the screen.
            var anchorMovementInTransitionBox = new NavBarPointTween(
                begin: new Point(
                    // The "width / 2" here makes the middle widget's horizontal center on the
                    // trailing edge of the top nav bar.
                    TopNavBarBox.Size.Width - (toBox.Size.Width / 2),
                    to.Top),
                end: toAnchorInTransitionBox);

            return new FixedSizeSlidingTransition(
                isLtr: isLtr,
                offsetAnimation: Animation.Drive(anchorMovementInTransitionBox),
                width: toBox.Size.Width,
                height: toBox.Size.Height,
                child: new FadeTransition(
                    opacity: FadeInFrom(0.25),
                    child: new DefaultTextStyle(style: TopTitleTextStyle, child: topMiddle.Child)));
        }
    }

    public Widget? TopTrailing
    {
        get
        {
            var topTrailing = TopComponents.TrailingKey.CurrentWidget as KeyedSubtree;
            if (topTrailing == null)
            {
                return null;
            }

            return Positioned.FromRelativeRect(
                rect: PositionInTransitionBox(TopComponents.TrailingKey, from: TopNavBarBox),
                child: new FadeTransition(opacity: FadeInFrom(0.4), child: topTrailing.Child));
        }
    }

    public Widget? TopLargeTitle
    {
        get
        {
            var topLargeTitle = TopComponents.LargeTitleKey.CurrentWidget as KeyedSubtree;
            if (topLargeTitle == null || !TopLargeExpanded)
            {
                return null;
            }

            RelativeRect to = PositionInTransitionBox(TopComponents.LargeTitleKey, from: TopNavBarBox);

            // Shift in from the trailing edge of the screen.
            var positionTween = new RelativeRectTween(
                begin: Shift(to, new Point(ForwardDirection * TopNavBarBox.Size.Width, 0.0)),
                end: to);

            Curve animationCurve = Animation.Status == AnimationStatus.Forward
                ? NavBarStatics.TopNavBarHeaderTransitionCurve
                : Curves.Flipped(NavBarStatics.TopNavBarHeaderTransitionCurve);

            return new PositionedTransition(
                // The large title animates linearly during a backswipe by a user gesture.
                rect: UserGestureInProgress
                    ? RouteAnimation.Drive(new CurveTween(Curves.Linear)).Drive(positionTween)
                    : Animation.Drive(new CurveTween(animationCurve)).Drive(positionTween),
                child: new FadeTransition(
                    opacity: FadeInFrom(0.0, curve: animationCurve),
                    child: new DefaultTextStyle(
                        style: TopLargeTitleTextStyle!,
                        maxLines: 1,
                        overflow: TextOverflow.Ellipsis,
                        child: topLargeTitle.Child)));
        }
    }

    public Widget? TopNavBarBottom
    {
        get
        {
            var topNavBarBottom = TopComponents.NavBarBottomKey.CurrentWidget as KeyedSubtree;
            if (topNavBarBottom == null)
            {
                return null;
            }

            RelativeRect to = PositionInTransitionBox(TopComponents.NavBarBottomKey, from: TopNavBarBox);
            // Shift in from the trailing edge of the screen.
            var positionTween = new RelativeRectTween(
                begin: Shift(to, new Point(ForwardDirection * TopNavBarBox.Size.Width, 0.0)),
                end: to);

            Widget child = topNavBarBottom.Child;

            Curve animationCurve = Animation.Status == AnimationStatus.Forward
                ? NavBarStatics.TopNavBarHeaderTransitionCurve
                : Curves.Flipped(NavBarStatics.TopNavBarHeaderTransitionCurve);

            // Fade in only if this is not a CupertinoSliverNavigationBar.search to
            // CupertinoSliverNavigationBar.search transition.
            if (!Searchable)
            {
                child = new FadeTransition(opacity: FadeInFrom(0.0, curve: animationCurve), child: child);
            }

            return new PositionedTransition(
                // The bottom widget animates linearly during a backswipe by a user gesture.
                rect: UserGestureInProgress
                    ? RouteAnimation.Drive(new CurveTween(Curves.Linear)).Drive(positionTween)
                    : Animation.Drive(new CurveTween(animationCurve)).Drive(positionTween),
                child: new ClipRect(child: child));
        }
    }
}

/// <summary>The nav bar hero builders (Dart's top-level `_navBarHero*` functions).</summary>
internal static class NavBarTransitions
{
    /// <summary>
    /// Dart's `_linearTranslateWithLargestRectSizeTween`: moves between the static bars but keeps a
    /// constant size that's the bigger of both navigation bars.
    /// </summary>
    public static Plumix.Tween<Rect> LinearTranslateWithLargestRectSizeTween(Rect begin, Rect end)
    {
        var largestSize = new Size(
            Math.Max(begin.Size.Width, end.Size.Width),
            Math.Max(begin.Size.Height, end.Size.Height));
        return new Plumix.RectTween(
            begin: new Rect(begin.TopLeft, largestSize),
            end: new Rect(end.TopLeft, largestSize));
    }

    /// <summary>Dart's `_navBarHeroLaunchPadBuilder`.</summary>
    public static Widget NavBarHeroLaunchPadBuilder(BuildContext context, Size heroSize, Widget child)
    {
        if (child is not TransitionableNavigationBar)
        {
            throw new InvalidOperationException(
                "The nav bar hero placeholder child must be a TransitionableNavigationBar.");
        }

        // Keeping the Hero subtree here is needed (instead of just swapping out the anchor nav bars
        // for fixed size boxes during flights) because the nav bar and their specific component
        // children may serve as anchor points again if another mid-transition flight diversion is
        // triggered.

        // This is ok performance-wise because static nav bars are generally cheap to build and
        // layout but expensive to GPU render (due to clips and blurs) which we're skipping here.
        return new Visibility(
            maintainSize: true,
            maintainAnimation: true,
            maintainState: true,
            visible: false,
            child: child);
    }

    /// <summary>Dart's `_navBarHeroFlightShuttleBuilder`.</summary>
    public static Widget NavBarHeroFlightShuttleBuilder(
        BuildContext flightContext,
        Animation<double> animation,
        HeroFlightDirection flightDirection,
        BuildContext fromHeroContext,
        BuildContext toHeroContext)
    {
        var fromHeroWidget = (Hero)fromHeroContext.Widget;
        var toHeroWidget = (Hero)toHeroContext.Widget;

        if (fromHeroWidget.Child is not TransitionableNavigationBar fromNavBar
            || toHeroWidget.Child is not TransitionableNavigationBar toNavBar)
        {
            throw new InvalidOperationException(
                "The nav bar hero children must be TransitionableNavigationBar widgets.");
        }

        return flightDirection switch
        {
            HeroFlightDirection.Push => new NavigationBarTransition(
                animation: animation,
                bottomNavBar: fromNavBar,
                topNavBar: toNavBar),
            HeroFlightDirection.Pop => new NavigationBarTransition(
                animation: animation,
                bottomNavBar: toNavBar,
                topNavBar: fromNavBar),
            _ => throw new ArgumentOutOfRangeException(nameof(flightDirection)),
        };
    }
}
