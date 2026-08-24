using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/nav_bar.dart

/// <summary>Modes that determine when the bottom widget of a navigation bar collapses.</summary>
public enum NavigationBarBottomMode
{
    Automatic,
    Always,
}

/// <summary>Constants and helpers shared by the navigation bar family (Dart's `nav_bar.dart` privates).</summary>
internal static class NavBarStatics
{
    /// <summary>`_kNavBarPersistentHeight` (`kMinInteractiveDimensionCupertino`).</summary>
    public const double NavBarPersistentHeight = 44.0;

    /// <summary>`_kNavBarLargeTitleHeightExtension`.</summary>
    public const double NavBarLargeTitleHeightExtension = 52.0;

    /// <summary>`_kNavBarShowLargeTitleThreshold`.</summary>
    public const double NavBarShowLargeTitleThreshold = 10.0;

    /// <summary>`_kNavBarScrollUnderAnimationExtent`.</summary>
    public const double NavBarScrollUnderAnimationExtent = 10.0;

    /// <summary>`_kNavBarEdgePadding`.</summary>
    public const double NavBarEdgePadding = 16.0;

    /// <summary>`_kNavBarBottomPadding`.</summary>
    public const double NavBarBottomPadding = 8.0;

    /// <summary>`_kNavBarBackButtonTapWidth`.</summary>
    public const double NavBarBackButtonTapWidth = 50.0;

    /// <summary>`_kMinScaleFactor`.</summary>
    public const double MinScaleFactor = 0.9;

    /// <summary>`_kMaxScaleFactor`.</summary>
    public const double MaxScaleFactor = 1.235;

    /// <summary>`_kLargeTitleScaleDampingRatio`.</summary>
    public const double LargeTitleScaleDampingRatio = 3.0;

    /// <summary>`_kSearchFieldCancelButtonWidth`.</summary>
    public const double SearchFieldCancelButtonWidth = 67.0;

    /// <summary>`_kSearchFieldHeight`.</summary>
    public const double SearchFieldHeight = 36.0;

    /// <summary>`_kNavBarSearchDuration`.</summary>
    public static readonly TimeSpan NavBarSearchDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>`_kNavBarSearchCurve`.</summary>
    public static Curve NavBarSearchCurve => Curves.EaseInOut;

    /// <summary>`_kNavBarTitleFadeDuration`.</summary>
    public static readonly TimeSpan NavBarTitleFadeDuration = TimeSpan.FromMilliseconds(150);

    /// <summary>`_kDefaultNavBarBorderColor`.</summary>
    public static readonly Color DefaultNavBarBorderColor = Color.FromUInt32(0x4D000000);

    /// <summary>`_kDefaultNavBarBorder`.</summary>
    public static readonly Border DefaultNavBarBorder = new(
        bottom: new BorderSide(DefaultNavBarBorderColor, width: 0.0));

    /// <summary>`_kTransparentNavBarBorder`.</summary>
    public static readonly Border TransparentNavBarBorder = new(
        bottom: new BorderSide(Color.FromUInt32(0x00000000), width: 0.0));

    /// <summary>`_kTopNavBarHeaderTransitionCurve`.</summary>
    public static Curve TopNavBarHeaderTransitionCurve { get; } = Curves.Cubic(0.0, 0.45, 0.45, 0.98);

    /// <summary>`_kBottomNavBarHeaderTransitionCurve`.</summary>
    public static Curve BottomNavBarHeaderTransitionCurve { get; } = Curves.Cubic(0.05, 0.90, 0.90, 0.95);

    /// <summary>`_defaultHeroTag`.</summary>
    public static readonly object DefaultHeroTag = new NavigationBarHeroTag(null);

    /// <summary>Dart's `clampDouble`: like <see cref="Math.Clamp(double,double,double)"/> but NaN clamps to the upper limit.</summary>
    public static double ClampDouble(double value, double lowerLimit, double upperLimit)
    {
        if (double.IsNaN(value))
        {
            return upperLimit;
        }

        return Math.Clamp(value, lowerLimit, upperLimit);
    }

    /// <summary>`_dampScaleFactor`: damps large-title/search-field growth beyond the unscaled size.</summary>
    public static double DampScaleFactor(double scaledSize, double unscaledSize, double dampingRatio)
    {
        double scaleFactor = scaledSize / unscaledSize;
        if (scaleFactor < 1.0)
        {
            return Math.Max(MinScaleFactor, scaleFactor);
        }

        return 1.0 + ((scaleFactor - 1.0) / dampingRatio);
    }

    /// <summary>`_isTransitionable`.</summary>
    public static bool IsTransitionable(BuildContext context)
    {
        var route = ModalRoute.MaybeOf(context);
        return route is PageRoute pageRoute
               && !pageRoute.FullscreenDialog
               && !CupertinoSheetRoute<dynamic>.HasParentSheet(context);
    }

    /// <summary>`_wrapWithBackground`: background, border, blur and status-bar annotation.</summary>
    public static Widget WrapWithBackground(
        Border? border,
        Color backgroundColor,
        Widget child,
        PlatformBrightness? brightness = null,
        bool updateSystemUiOverlay = true,
        bool enableBackgroundFilterBlur = true)
    {
        Widget result = child;
        if (updateSystemUiOverlay)
        {
            bool isDark = backgroundColor.ComputeLuminance() < 0.179;
            PlatformBrightness newBrightness =
                brightness ?? (isDark ? PlatformBrightness.Dark : PlatformBrightness.Light);
            SystemUiOverlayStyle overlayStyle = newBrightness == PlatformBrightness.Dark
                ? SystemUiOverlayStyle.Light
                : SystemUiOverlayStyle.Dark;
            // Flutter copies only the statusBar* fields so system navigation bar properties stay
            // untouched.
            result = new AnnotatedRegion<SystemUiOverlayStyle>(
                value: new SystemUiOverlayStyle(
                    StatusBarColor: overlayStyle.StatusBarColor,
                    StatusBarIconBrightness: overlayStyle.StatusBarIconBrightness),
                child: result);
        }

        var childWithBackground = new DecoratedBox(
            decoration: new BoxDecoration(
                Color: backgroundColor,
                Border: border),
            child: result);

        return new ClipRect(
            child: new BackdropFilter(
                filter: new ImageFilter.Blur(sigmaX: 10.0, sigmaY: 10.0),
                child: childWithBackground,
                enabled: backgroundColor.A != 0xFF && enableBackgroundFilterBlur));
    }

    /// <summary>`MediaQuery.withClampedTextScaling(minScaleFactor: 0.9, maxScaleFactor: 1.235)`.</summary>
    public static Widget WrapWithClampedTextScaling(Widget child)
    {
        return new Builder(builder: context => MediaQuery.WithClampedTextScaling(
            context,
            child,
            maxScaleFactor: MaxScaleFactor,
            minScaleFactor: MinScaleFactor));
    }
}

/// <summary>Dart's `_HeroTag`: the default nav-bar hero tag, scoped per navigator.</summary>
internal sealed class NavigationBarHeroTag
{
    public NavigationBarHeroTag(NavigatorState? navigator)
    {
        Navigator = navigator;
    }

    public NavigatorState? Navigator { get; }

    public override string ToString() =>
        "Default Hero tag for Cupertino navigation bars with navigator " + (Navigator?.ToString() ?? "null");

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is NavigationBarHeroTag other && ReferenceEquals(Navigator, other.Navigator);
    }

    public override int GetHashCode()
    {
        return Navigator is null
            ? 0
            : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Navigator);
    }
}

/// <summary>An iOS-styled navigation bar, drawn at the top of the screen.</summary>
public sealed class CupertinoNavigationBar : StatefulWidget, IObstructingPreferredSizeWidget
{
    public CupertinoNavigationBar(
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyMiddle = true,
        string? previousPageTitle = null,
        Widget? middle = null,
        Widget? trailing = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool automaticBackgroundVisibility = true,
        bool enableBackgroundFilterBlur = true,
        PlatformBrightness? brightness = null,
        EdgeInsetsDirectional? padding = null,
        bool transitionBetweenRoutes = true,
        object? heroTag = null,
        IPreferredSizeWidget? bottom = null,
        Key? key = null) : this(
        NavBarStatics.DefaultNavBarBorder,
        leading,
        automaticallyImplyLeading,
        automaticallyImplyMiddle,
        previousPageTitle,
        middle,
        trailing,
        backgroundColor,
        automaticBackgroundVisibility,
        enableBackgroundFilterBlur,
        brightness,
        padding,
        transitionBetweenRoutes,
        heroTag,
        bottom,
        key)
    {
    }

    public CupertinoNavigationBar(
        Border? border,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyMiddle = true,
        string? previousPageTitle = null,
        Widget? middle = null,
        Widget? trailing = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool automaticBackgroundVisibility = true,
        bool enableBackgroundFilterBlur = true,
        PlatformBrightness? brightness = null,
        EdgeInsetsDirectional? padding = null,
        bool transitionBetweenRoutes = true,
        object? heroTag = null,
        IPreferredSizeWidget? bottom = null,
        Key? key = null) : this(
        largeTitle: null,
        border: border,
        leading: leading,
        automaticallyImplyLeading: automaticallyImplyLeading,
        automaticallyImplyMiddle: automaticallyImplyMiddle,
        previousPageTitle: previousPageTitle,
        middle: middle,
        trailing: trailing,
        backgroundColor: backgroundColor,
        automaticBackgroundVisibility: automaticBackgroundVisibility,
        enableBackgroundFilterBlur: enableBackgroundFilterBlur,
        brightness: brightness,
        padding: padding,
        transitionBetweenRoutes: transitionBetweenRoutes,
        heroTag: heroTag,
        bottom: bottom,
        key: key)
    {
    }

    /// <summary>Dart's `CupertinoNavigationBar.large` with the default border.</summary>
    public static CupertinoNavigationBar Large(
        Widget? largeTitle = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyTitle = true,
        string? previousPageTitle = null,
        Widget? trailing = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool automaticBackgroundVisibility = true,
        bool enableBackgroundFilterBlur = true,
        PlatformBrightness? brightness = null,
        EdgeInsetsDirectional? padding = null,
        bool transitionBetweenRoutes = true,
        object? heroTag = null,
        IPreferredSizeWidget? bottom = null,
        Key? key = null)
    {
        return Large(
            NavBarStatics.DefaultNavBarBorder,
            largeTitle,
            leading,
            automaticallyImplyLeading,
            automaticallyImplyTitle,
            previousPageTitle,
            trailing,
            backgroundColor,
            automaticBackgroundVisibility,
            enableBackgroundFilterBlur,
            brightness,
            padding,
            transitionBetweenRoutes,
            heroTag,
            bottom,
            key);
    }

    /// <summary>Dart's `CupertinoNavigationBar.large` with an explicit (possibly null) border.</summary>
    public static CupertinoNavigationBar Large(
        Border? border,
        Widget? largeTitle = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyTitle = true,
        string? previousPageTitle = null,
        Widget? trailing = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool automaticBackgroundVisibility = true,
        bool enableBackgroundFilterBlur = true,
        PlatformBrightness? brightness = null,
        EdgeInsetsDirectional? padding = null,
        bool transitionBetweenRoutes = true,
        object? heroTag = null,
        IPreferredSizeWidget? bottom = null,
        Key? key = null)
    {
        return new CupertinoNavigationBar(
            largeTitle: largeTitle,
            border: border,
            leading: leading,
            automaticallyImplyLeading: automaticallyImplyLeading,
            automaticallyImplyMiddle: automaticallyImplyTitle,
            previousPageTitle: previousPageTitle,
            middle: null,
            trailing: trailing,
            backgroundColor: backgroundColor,
            automaticBackgroundVisibility: automaticBackgroundVisibility,
            enableBackgroundFilterBlur: enableBackgroundFilterBlur,
            brightness: brightness,
            padding: padding,
            transitionBetweenRoutes: transitionBetweenRoutes,
            heroTag: heroTag,
            bottom: bottom,
            key: key);
    }

    private CupertinoNavigationBar(
        Widget? largeTitle,
        Border? border,
        Widget? leading,
        bool automaticallyImplyLeading,
        bool automaticallyImplyMiddle,
        string? previousPageTitle,
        Widget? middle,
        Widget? trailing,
        CupertinoDynamicColor? backgroundColor,
        bool automaticBackgroundVisibility,
        bool enableBackgroundFilterBlur,
        PlatformBrightness? brightness,
        EdgeInsetsDirectional? padding,
        bool transitionBetweenRoutes,
        object? heroTag,
        IPreferredSizeWidget? bottom,
        Key? key) : base(key)
    {
        if (bottom is not null and not Widget)
        {
            throw new ArgumentException("The bottom widget must be a Widget.", nameof(bottom));
        }

        HeroTag = heroTag ?? NavBarStatics.DefaultHeroTag;
        if (transitionBetweenRoutes && !ReferenceEquals(HeroTag, NavBarStatics.DefaultHeroTag))
        {
            throw new ArgumentException(
                "Cannot specify a heroTag override if this navigation bar does not transition due to "
                + "transitionBetweenRoutes = false.",
                nameof(heroTag));
        }

        LargeTitle = largeTitle;
        Leading = leading;
        AutomaticallyImplyLeading = automaticallyImplyLeading;
        AutomaticallyImplyMiddle = automaticallyImplyMiddle;
        PreviousPageTitle = previousPageTitle;
        Middle = middle;
        Trailing = trailing;
        Border = border;
        BackgroundColor = backgroundColor;
        AutomaticBackgroundVisibility = automaticBackgroundVisibility;
        EnableBackgroundFilterBlur = enableBackgroundFilterBlur;
        Brightness = brightness;
        Padding = padding;
        TransitionBetweenRoutes = transitionBetweenRoutes;
        Bottom = bottom;
    }

    public Widget? LargeTitle { get; }

    public Widget? Leading { get; }

    public bool AutomaticallyImplyLeading { get; }

    public bool AutomaticallyImplyMiddle { get; }

    public string? PreviousPageTitle { get; }

    public Widget? Middle { get; }

    public Widget? Trailing { get; }

    public Border? Border { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public bool AutomaticBackgroundVisibility { get; }

    public bool EnableBackgroundFilterBlur { get; }

    public PlatformBrightness? Brightness { get; }

    public EdgeInsetsDirectional? Padding { get; }

    public bool TransitionBetweenRoutes { get; }

    public object HeroTag { get; }

    public IPreferredSizeWidget? Bottom { get; }

    public Size PreferredSize
    {
        get
        {
            double heightForLargeTitle = LargeTitle != null ? NavBarStatics.NavBarLargeTitleHeightExtension : 0.0;
            double bottomHeight = Bottom?.PreferredSize.Height ?? 0.0;
            return new Size(
                double.PositiveInfinity,
                NavBarStatics.NavBarPersistentHeight + heightForLargeTitle + bottomHeight);
        }
    }

    public bool ShouldFullyObstruct(BuildContext context)
    {
        Color backgroundColor = CupertinoDynamicColor.MaybeResolve(BackgroundColor, context)
                                ?? CupertinoTheme.Of(context).BarBackgroundColor;
        return backgroundColor.A == 0xFF;
    }

    public override State CreateState() => new CupertinoNavigationBarState();
}

internal sealed class CupertinoNavigationBarState : State
{
    private NavigationBarStaticComponentsKeys _keys = null!;
    private ScrollNotificationObserverState? _scrollNotificationObserver;
    private double _scrollAnimationValue;

    private CupertinoNavigationBar CurrentWidget => (CupertinoNavigationBar)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _keys = new NavigationBarStaticComponentsKeys();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _scrollNotificationObserver?.RemoveListener(HandleScrollNotification);
        _scrollNotificationObserver = ScrollNotificationObserver.MaybeOf(Context);
        _scrollNotificationObserver?.AddListener(HandleScrollNotification);
    }

    public override void Dispose()
    {
        if (_scrollNotificationObserver != null)
        {
            _scrollNotificationObserver.RemoveListener(HandleScrollNotification);
            _scrollNotificationObserver = null;
        }

        base.Dispose();
    }

    private void HandleScrollNotification(ScrollNotification notification)
    {
        if (notification is not ScrollUpdateNotification { Depth: 0 } update)
        {
            return;
        }

        IScrollMetrics metrics = update.Metrics;
        double scrollExtent;
        switch (metrics.AxisDirection)
        {
            case AxisDirection.Up:
                // Scroll view is reversed.
                scrollExtent = metrics.ExtentAfter;
                break;
            case AxisDirection.Down:
                scrollExtent = metrics.ExtentBefore;
                break;
            default:
                // Scrolled under is only supported in the vertical axis.
                return;
        }

        if (scrollExtent >= 0.0 && scrollExtent < NavBarStatics.NavBarScrollUnderAnimationExtent)
        {
            SetState(() => _scrollAnimationValue =
                Math.Clamp(scrollExtent / NavBarStatics.NavBarScrollUnderAnimationExtent, 0.0, 1.0));
        }
        else if (scrollExtent > NavBarStatics.NavBarScrollUnderAnimationExtent && _scrollAnimationValue != 1.0)
        {
            SetState(() => _scrollAnimationValue = 1.0);
        }
        else if (scrollExtent <= 0.0 && _scrollAnimationValue != 0.0)
        {
            SetState(() => _scrollAnimationValue = 0.0);
        }
    }

    public override Widget Build(BuildContext context)
    {
        if (CurrentWidget.Middle != null && CurrentWidget.LargeTitle != null)
        {
            throw new InvalidOperationException(
                "Cannot provide both a middle and a largeTitle to CupertinoNavigationBar.");
        }

        var theme = CupertinoTheme.Of(context);
        Color backgroundColor = CupertinoDynamicColor.MaybeResolve(CurrentWidget.BackgroundColor, context)
                                ?? CupertinoDynamicColor.Resolve(theme.BarBackgroundColor, context);
        Color? parentPageScaffoldBackgroundColor = CupertinoPageScaffoldBackgroundColor.MaybeOf(context);
        bool automaticallyTransparent = CurrentWidget.AutomaticBackgroundVisibility
                                        && parentPageScaffoldBackgroundColor != null;

        Border? initialBorder = automaticallyTransparent
            ? NavBarStatics.TransparentNavBarBorder
            : CurrentWidget.Border;
        Border? effectiveBorder = CurrentWidget.Border == null
            ? null
            : Border.Lerp(initialBorder, CurrentWidget.Border, _scrollAnimationValue);
        Color effectiveBackgroundColor = automaticallyTransparent
            ? ColorUtilities.Lerp(parentPageScaffoldBackgroundColor!.Value, backgroundColor, _scrollAnimationValue)
            : backgroundColor;

        var components = new NavigationBarStaticComponents(
            keys: _keys,
            route: ModalRoute.MaybeOf(context),
            userLeading: CurrentWidget.Leading,
            automaticallyImplyLeading: CurrentWidget.AutomaticallyImplyLeading,
            automaticallyImplyTitle: CurrentWidget.AutomaticallyImplyMiddle,
            previousPageTitle: CurrentWidget.PreviousPageTitle,
            userMiddle: CurrentWidget.Middle,
            userTrailing: CurrentWidget.Trailing,
            padding: CurrentWidget.Padding,
            userLargeTitle: CurrentWidget.LargeTitle,
            userBottom: CurrentWidget.Bottom as Widget,
            large: CurrentWidget.LargeTitle != null,
            staticBar: true,
            context: context);

        double bottomHeight = CurrentWidget.Bottom?.PreferredSize.Height ?? 0.0;
        double persistentHeight =
            NavBarStatics.NavBarPersistentHeight + bottomHeight + MediaQuery.PaddingOf(context).Top;
        double largeHeight = persistentHeight + NavBarStatics.NavBarLargeTitleHeightExtension;

        Widget persistentNavigationBar = new PersistentNavigationBar(
            components: components,
            padding: CurrentWidget.Padding,
            middleVisible: CurrentWidget.LargeTitle == null);

        Widget navBar;
        if (CurrentWidget.LargeTitle != null)
        {
            var children = new List<Widget>
            {
                persistentNavigationBar,
                new Expanded(
                    child: new Padding(
                        new EdgeInsetsDirectional(
                            NavBarStatics.NavBarEdgePadding,
                            0.0,
                            0.0,
                            NavBarStatics.NavBarBottomPadding),
                        child: new Semantics(
                            flags: SemanticsFlags.IsHeader,
                            child: new DefaultTextStyle(
                                style: theme.TextTheme.NavLargeTitleTextStyle,
                                maxLines: 1,
                                overflow: TextOverflow.Ellipsis,
                                child: new LargeTitleWidget(
                                    height: NavBarStatics.NavBarLargeTitleHeightExtension,
                                    child: components.LargeTitle!))))),
            };
            if (CurrentWidget.Bottom != null)
            {
                children.Add(new SizedBox(height: bottomHeight, child: components.NavBarBottom));
            }

            navBar = new ConstrainedBox(
                constraints: new BoxConstraints(MaxHeight: largeHeight),
                child: new Column(children: children));
        }
        else
        {
            var children = new List<Widget> { persistentNavigationBar };
            if (CurrentWidget.Bottom != null)
            {
                children.Add(new SizedBox(height: bottomHeight, child: components.NavBarBottom));
            }

            navBar = new ConstrainedBox(
                constraints: new BoxConstraints(MaxHeight: persistentHeight),
                child: new Column(children: children));
        }

        navBar = NavBarStatics.WrapWithBackground(
            border: effectiveBorder,
            backgroundColor: effectiveBackgroundColor,
            brightness: CurrentWidget.Brightness,
            enableBackgroundFilterBlur: CurrentWidget.EnableBackgroundFilterBlur,
            child: new DefaultTextStyle(
                style: theme.TextTheme.TextStyle,
                child: navBar));

        if (!CurrentWidget.TransitionBetweenRoutes || !NavBarStatics.IsTransitionable(context))
        {
            // Lint ignore: https://github.com/flutter/flutter/issues/29341
            return navBar;
        }

        return new Builder(
            // Get the context that might have a possibly changed CupertinoTheme.
            builder: builderContext => new Hero(
                tag: ReferenceEquals(CurrentWidget.HeroTag, NavBarStatics.DefaultHeroTag)
                    ? new NavigationBarHeroTag(Navigator.Of(builderContext))
                    : CurrentWidget.HeroTag,
                createRectTween: NavBarTransitions.LinearTranslateWithLargestRectSizeTween,
                placeholderBuilder: NavBarTransitions.NavBarHeroLaunchPadBuilder,
                flightShuttleBuilder: NavBarTransitions.NavBarHeroFlightShuttleBuilder,
                transitionOnUserGestures: true,
                child: new TransitionableNavigationBar(
                    componentsKeys: _keys,
                    backgroundColor: effectiveBackgroundColor,
                    backButtonTextStyle: theme.TextTheme.NavActionTextStyle,
                    titleTextStyle: theme.TextTheme.NavTitleTextStyle,
                    largeTitleTextStyle: theme.TextTheme.NavLargeTitleTextStyle,
                    border: effectiveBorder,
                    hasUserMiddle: CurrentWidget.Middle != null,
                    largeExpanded: CurrentWidget.LargeTitle != null,
                    searchable: false,
                    automaticBackgroundVisibility: CurrentWidget.AutomaticBackgroundVisibility,
                    child: navBar)));
    }
}

/// <summary>A nav bar back button typically used in <see cref="CupertinoNavigationBar"/>.</summary>
public sealed class CupertinoNavigationBarBackButton : StatelessWidget
{
    public CupertinoNavigationBarBackButton(
        CupertinoDynamicColor? color = null,
        string? previousPageTitle = null,
        Action? onPressed = null,
        Key? key = null) : base(key)
    {
        Color = color;
        PreviousPageTitle = previousPageTitle;
        OnPressed = onPressed;
    }

    private CupertinoNavigationBarBackButton(Widget backChevron, Widget backLabel)
    {
        BackChevronWidget = backChevron;
        BackLabelWidget = backLabel;
    }

    /// <summary>Dart's `CupertinoNavigationBarBackButton._assemble`.</summary>
    internal static CupertinoNavigationBarBackButton Assemble(Widget backChevron, Widget backLabel)
    {
        return new CupertinoNavigationBarBackButton(backChevron, backLabel);
    }

    public CupertinoDynamicColor? Color { get; }

    public string? PreviousPageTitle { get; }

    public Action? OnPressed { get; }

    internal Widget? BackChevronWidget { get; }

    internal Widget? BackLabelWidget { get; }

    public override Widget Build(BuildContext context)
    {
        var currentRoute = ModalRoute.MaybeOf(context);
        if (BackChevronWidget == null && OnPressed == null && !(currentRoute?.CanPop ?? false))
        {
            throw new InvalidOperationException(
                "CupertinoNavigationBarBackButton should only be used in routes that can be popped.");
        }

        TextStyle actionTextStyle = CupertinoTheme.Of(context).TextTheme.NavActionTextStyle;
        if (Color != null)
        {
            actionTextStyle = actionTextStyle.CopyWith(
                color: CupertinoDynamicColor.MaybeResolve(Color, context));
        }

        return new CupertinoButton(
            padding: EdgeInsetsDirectional.Zero,
            onPressed: () =>
            {
                if (OnPressed != null)
                {
                    OnPressed();
                }
                else
                {
                    Navigator.MaybePop(context);
                }
            },
            child: new Semantics(
                container: true,
                label: CupertinoLocalizations.Of(context).BackButtonLabel,
                flags: SemanticsFlags.IsButton,
                child: new ExcludeSemantics(
                    child: new DefaultTextStyle(
                        style: actionTextStyle,
                        child: new ConstrainedBox(
                            constraints: new BoxConstraints(MinWidth: NavBarStatics.NavBarBackButtonTapWidth),
                            child: new Row(
                                mainAxisSize: MainAxisSize.Min,
                                mainAxisAlignment: MainAxisAlignment.Start,
                                children:
                                [
                                    new Padding(new EdgeInsetsDirectional(8.0, 0.0, 0.0, 0.0)),
                                    BackChevronWidget ?? new BackChevron(),
                                    new Padding(new EdgeInsetsDirectional(6.0, 0.0, 0.0, 0.0)),
                                    new Flexible(
                                        child: BackLabelWidget ?? new BackLabel(
                                            specifiedPreviousTitle: PreviousPageTitle,
                                            route: currentRoute)),
                                ]))))));
    }
}

/// <summary>Dart's `_BackChevron`.</summary>
internal sealed class BackChevron : StatelessWidget
{
    public BackChevron() : base(StandardComponentType.BackButton.Key())
    {
    }

    public override Widget Build(BuildContext context)
    {
        TextDirection textDirection = Directionality.Of(context);
        TextStyle textStyle = DefaultTextStyle.Of(context);

        // Replicate the Icon logic here to get a tightly sized icon and add custom non-square padding.
        var backChevron = CupertinoIcons.Back;
        Widget iconWidget = new Padding(
            new EdgeInsetsDirectional(6.0, 0.0, 2.0, 0.0),
            child: Text.Rich(
                new TextSpan(
                    text: char.ConvertFromUtf32(backChevron.CodePoint),
                    style: new TextStyle(
                        Inherit: false,
                        Color: textStyle.Color,
                        FontSize: 30.0,
                        FontFamily: IconFontRegistry.Resolve(backChevron)))));
        if (textDirection == TextDirection.Rtl)
        {
            iconWidget = new Plumix.Widgets.Transform(
                transform: Matrix4.Diagonal3Values(-1.0, 1.0, 1.0),
                alignment: Alignment.Center,
                transformHitTests: false,
                child: iconWidget);
        }

        return iconWidget;
    }
}

/// <summary>
/// Dart's `_BackLabel`: the previous route's title, or "Back" when the title is over 12 characters.
/// </summary>
internal sealed class BackLabel : StatelessWidget
{
    public BackLabel(string? specifiedPreviousTitle, ModalRoute? route)
    {
        SpecifiedPreviousTitle = specifiedPreviousTitle;
        Route = route;
    }

    public string? SpecifiedPreviousTitle { get; }

    public ModalRoute? Route { get; }

    private static Widget BuildPreviousTitleWidget(BuildContext context, string? previousTitle)
    {
        if (previousTitle == null)
        {
            return new SizedBox(width: 0.0, height: 0.0);
        }

        Widget textWidget = new Text(
            previousTitle,
            maxLines: 1,
            overflow: TextOverflow.Ellipsis);

        if (previousTitle.Length > 12)
        {
            textWidget = new Text(CupertinoLocalizations.Of(context).BackButtonLabel);
        }

        return new Align(
            alignment: AlignmentDirectional.CenterStart,
            widthFactor: 1.0,
            child: textWidget);
    }

    public override Widget Build(BuildContext context)
    {
        if (SpecifiedPreviousTitle != null)
        {
            return BuildPreviousTitleWidget(context, SpecifiedPreviousTitle);
        }

        if (Route is ICupertinoRouteTransition cupertinoRoute && !Route.IsFirst)
        {
            // There is no timing issue because the previous title property is updated synchronously.
            return new ValueListenableBuilder<string?>(
                valueListenable: cupertinoRoute.PreviousTitle,
                builder: (builderContext, previousTitle, _) =>
                    BuildPreviousTitleWidget(builderContext, previousTitle));
        }

        return new SizedBox(width: 0.0, height: 0.0);
    }
}
