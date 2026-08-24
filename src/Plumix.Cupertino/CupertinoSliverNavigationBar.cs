using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/nav_bar.dart

/// <summary>
/// An iOS-styled navigation bar with iOS-11-style large titles using slivers.
/// </summary>
public sealed class CupertinoSliverNavigationBar : StatefulWidget
{
    public CupertinoSliverNavigationBar(
        Widget? largeTitle = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyTitle = true,
        bool alwaysShowMiddle = true,
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
        bool stretch = false,
        IPreferredSizeWidget? bottom = null,
        NavigationBarBottomMode? bottomMode = null,
        Key? key = null) : this(
        NavBarStatics.DefaultNavBarBorder,
        largeTitle,
        leading,
        automaticallyImplyLeading,
        automaticallyImplyTitle,
        alwaysShowMiddle,
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
        stretch,
        bottom,
        bottomMode,
        key)
    {
    }

    public CupertinoSliverNavigationBar(
        Border? border,
        Widget? largeTitle = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyTitle = true,
        bool alwaysShowMiddle = true,
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
        bool stretch = false,
        IPreferredSizeWidget? bottom = null,
        NavigationBarBottomMode? bottomMode = null,
        Key? key = null) : this(
        searchable: false,
        searchField: null,
        onSearchableBottomTap: null,
        border: border,
        largeTitle: largeTitle,
        leading: leading,
        automaticallyImplyLeading: automaticallyImplyLeading,
        automaticallyImplyTitle: automaticallyImplyTitle,
        alwaysShowMiddle: alwaysShowMiddle,
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
        stretch: stretch,
        bottom: bottom,
        bottomMode: bottomMode,
        key: key)
    {
    }

    /// <summary>Dart's `CupertinoSliverNavigationBar.search` with the default border.</summary>
    public static CupertinoSliverNavigationBar Search(
        Widget searchField,
        Widget? largeTitle = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyTitle = true,
        bool alwaysShowMiddle = true,
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
        bool stretch = false,
        NavigationBarBottomMode bottomMode = NavigationBarBottomMode.Automatic,
        Action<bool>? onSearchableBottomTap = null,
        Key? key = null)
    {
        return Search(
            NavBarStatics.DefaultNavBarBorder,
            searchField,
            largeTitle,
            leading,
            automaticallyImplyLeading,
            automaticallyImplyTitle,
            alwaysShowMiddle,
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
            stretch,
            bottomMode,
            onSearchableBottomTap,
            key);
    }

    /// <summary>Dart's `CupertinoSliverNavigationBar.search` with an explicit (possibly null) border.</summary>
    public static CupertinoSliverNavigationBar Search(
        Border? border,
        Widget searchField,
        Widget? largeTitle = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        bool automaticallyImplyTitle = true,
        bool alwaysShowMiddle = true,
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
        bool stretch = false,
        NavigationBarBottomMode bottomMode = NavigationBarBottomMode.Automatic,
        Action<bool>? onSearchableBottomTap = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(searchField);
        return new CupertinoSliverNavigationBar(
            searchable: true,
            searchField: searchField,
            onSearchableBottomTap: onSearchableBottomTap,
            border: border,
            largeTitle: largeTitle,
            leading: leading,
            automaticallyImplyLeading: automaticallyImplyLeading,
            automaticallyImplyTitle: automaticallyImplyTitle,
            alwaysShowMiddle: alwaysShowMiddle,
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
            stretch: stretch,
            bottom: null,
            bottomMode: bottomMode,
            key: key);
    }

    private CupertinoSliverNavigationBar(
        bool searchable,
        Widget? searchField,
        Action<bool>? onSearchableBottomTap,
        Border? border,
        Widget? largeTitle,
        Widget? leading,
        bool automaticallyImplyLeading,
        bool automaticallyImplyTitle,
        bool alwaysShowMiddle,
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
        bool stretch,
        IPreferredSizeWidget? bottom,
        NavigationBarBottomMode? bottomMode,
        Key? key) : base(key)
    {
        if (!automaticallyImplyTitle && largeTitle == null)
        {
            throw new ArgumentException(
                "No largeTitle has been provided but automaticallyImplyTitle is also false. Either "
                + "provide a largeTitle or set automaticallyImplyTitle to true.",
                nameof(largeTitle));
        }

        if (bottomMode != null && bottom == null && !searchable)
        {
            throw new ArgumentException(
                "A bottomMode was provided without a corresponding bottom.",
                nameof(bottomMode));
        }

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

        Searchable = searchable;
        SearchField = searchField;
        OnSearchableBottomTap = onSearchableBottomTap;
        LargeTitle = largeTitle;
        Leading = leading;
        AutomaticallyImplyLeading = automaticallyImplyLeading;
        AutomaticallyImplyTitle = automaticallyImplyTitle;
        AlwaysShowMiddle = alwaysShowMiddle;
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
        Stretch = stretch;
        Bottom = bottom;
        BottomMode = bottomMode;
    }

    public Widget? LargeTitle { get; }

    public Widget? Leading { get; }

    public bool AutomaticallyImplyLeading { get; }

    public bool AutomaticallyImplyTitle { get; }

    public bool AlwaysShowMiddle { get; }

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

    public bool Stretch { get; }

    public IPreferredSizeWidget? Bottom { get; }

    public NavigationBarBottomMode? BottomMode { get; }

    public Widget? SearchField { get; }

    public Action<bool>? OnSearchableBottomTap { get; }

    /// <summary>Dart's `_searchable`.</summary>
    internal bool Searchable { get; }

    /// <summary>True if the navigation bar's background color has no transparency.</summary>
    public bool Opaque => BackgroundColor != null && ((Color)BackgroundColor).A == 0xFF;

    public override State CreateState() => new CupertinoSliverNavigationBarState();
}

internal sealed class CupertinoSliverNavigationBarState : State
{
    private NavigationBarStaticComponentsKeys _keys = null!;
    private Scrollable.ScrollableState? _scrollableState;
    private Widget? _effectiveMiddle;
    private Plumix.AnimationController _animationController = null!;
    private Plumix.CurvedAnimation _searchAnimation = null!;
    private Animation<double> _persistentHeightAnimation = null!;
    private Animation<double> _largeTitleHeightAnimation = null!;
    private double _scaledSearchFieldHeight;
    private double _scaledLargeTitleHeight;
    private bool _searchIsActive;
    private bool _isPortrait = true;

    private CupertinoSliverNavigationBar CurrentWidget => (CupertinoSliverNavigationBar)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _keys = new NavigationBarStaticComponentsKeys();
        _animationController = new Plumix.AnimationController(
            vsync: this,
            duration: NavBarStatics.NavBarSearchDuration);
        _searchAnimation = new Plumix.CurvedAnimation(_animationController, NavBarStatics.NavBarSearchCurve);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (!ReferenceEquals(CurrentWidget.Middle, ((CupertinoSliverNavigationBar)oldWidget).Middle))
        {
            UpdateEffectiveMiddle();
        }
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _isPortrait = MediaQuery.OrientationOf(Context) == Orientation.Portrait;
        UpdateEffectiveMiddle();
        ComputeScaledHeights();
        SetupSearchableAnimation();
        _scrollableState?.Position?.IsScrollingNotifier.RemoveListener(HandleScrollChange);
        _scrollableState = Scrollable.MaybeOf(Context);
        _scrollableState?.Position?.IsScrollingNotifier.AddListener(HandleScrollChange);
    }

    public override void Dispose()
    {
        _scrollableState?.Position?.IsScrollingNotifier.RemoveListener(HandleScrollChange);
        _persistentHeightAnimation.RemoveStatusListener(HandleSearchFieldStatusChanged);
        _searchAnimation.Dispose();
        _animationController.Dispose();
        base.Dispose();
    }

    private double BottomHeight
    {
        get
        {
            if (CurrentWidget.Searchable)
            {
                return _scaledSearchFieldHeight + NavBarStatics.NavBarBottomPadding;
            }

            if (CurrentWidget.Bottom != null)
            {
                return CurrentWidget.Bottom.PreferredSize.Height;
            }

            return 0.0;
        }
    }

    private void UpdateEffectiveMiddle()
    {
        _effectiveMiddle = CurrentWidget.Middle ?? (_isPortrait ? null : CurrentWidget.LargeTitle);
    }

    private void ComputeScaledHeights()
    {
        var textScaler = MediaQuery.TextScalerOf(Context);
        _scaledSearchFieldHeight = NavBarStatics.SearchFieldHeight
                                   * NavBarStatics.DampScaleFactor(
                                       textScaler.Scale(NavBarStatics.SearchFieldHeight),
                                       NavBarStatics.SearchFieldHeight,
                                       NavBarStatics.MaxScaleFactor);
        _scaledLargeTitleHeight = _isPortrait
            ? NavBarStatics.NavBarLargeTitleHeightExtension
              * NavBarStatics.DampScaleFactor(
                  textScaler.Scale(NavBarStatics.NavBarLargeTitleHeightExtension),
                  NavBarStatics.NavBarLargeTitleHeightExtension,
                  NavBarStatics.LargeTitleScaleDampingRatio)
            : 0.0;
    }

    private void SetupSearchableAnimation()
    {
        _persistentHeightAnimation?.RemoveStatusListener(HandleSearchFieldStatusChanged);
        var persistentHeightTween = new DoubleTween(
            begin: NavBarStatics.NavBarPersistentHeight,
            end: 0.0);
        _persistentHeightAnimation = persistentHeightTween.Animate(_animationController);
        _persistentHeightAnimation.AddStatusListener(HandleSearchFieldStatusChanged);
        var largeTitleHeightTween = new DoubleTween(begin: _scaledLargeTitleHeight, end: 0.0);
        _largeTitleHeightAnimation = largeTitleHeightTween.Animate(_animationController);
    }

    private void HandleScrollChange()
    {
        ScrollPosition? position = _scrollableState?.Position;
        if (position == null || !position.HasPixels || position.Pixels <= 0.0)
        {
            return;
        }

        double? target = null;
        double bottomScrollOffset = CurrentWidget.BottomMode == NavigationBarBottomMode.Always
            ? 0.0
            : BottomHeight;
        bool canScrollBottom = (CurrentWidget.Searchable || CurrentWidget.Bottom != null)
                               && bottomScrollOffset > 0.0;

        // Snap the scroll view to a target determined by the navigation bar's position.
        if (canScrollBottom && position.Pixels < bottomScrollOffset)
        {
            target = position.Pixels > bottomScrollOffset / 2 ? bottomScrollOffset : 0.0;
        }
        else if (position.Pixels > bottomScrollOffset
                 && position.Pixels < bottomScrollOffset + _scaledLargeTitleHeight)
        {
            target = position.Pixels > bottomScrollOffset + (_scaledLargeTitleHeight / 2)
                ? bottomScrollOffset + _scaledLargeTitleHeight
                : bottomScrollOffset;
        }

        // If the target is not null and within the scrollable range, animate to it.
        if (target != null && target.Value <= position.MaxScrollExtent)
        {
            _ = position.AnimateTo(
                target.Value,
                // Eyeballed on an iPhone 16 simulator running iOS 18.
                duration: TimeSpan.FromMilliseconds(300),
                curve: Curves.FastEaseInToSlowEaseOut);
        }
    }

    private void HandleSearchFieldStatusChanged(AnimationStatus status)
    {
        // If the search animation is stopped, rebuild so that the leading, middle, and trailing
        // widgets that were collapsed while the search field was active are re-expanded. Otherwise,
        // rebuild to update this widget with the animation controller's values.
        SetState(() =>
        {
            switch (status)
            {
                case AnimationStatus.Forward:
                    _searchIsActive = true;
                    break;
                case AnimationStatus.Reverse:
                    _searchIsActive = false;
                    break;
            }
        });
    }

    private void OnSearchFieldTap()
    {
        CurrentWidget.OnSearchableBottomTap?.Invoke(!_searchIsActive);
        _ = _animationController.Toggle();
    }

    public override Widget Build(BuildContext context)
    {
        Widget? userBottom;
        if (CurrentWidget.Searchable)
        {
            userBottom = _searchIsActive
                ? new ActiveSearchableBottom(
                    animationController: _animationController,
                    animation: _persistentHeightAnimation,
                    searchField: CurrentWidget.SearchField,
                    searchFieldHeight: _scaledSearchFieldHeight,
                    onSearchFieldTap: OnSearchFieldTap)
                : new InactiveSearchableBottom(
                    animationController: _animationController,
                    animation: _persistentHeightAnimation,
                    searchField: CurrentWidget.SearchField,
                    searchFieldHeight: _scaledSearchFieldHeight,
                    onSearchFieldTap: OnSearchFieldTap);
        }
        else
        {
            userBottom = CurrentWidget.Bottom as Widget;
        }

        var components = new NavigationBarStaticComponents(
            keys: _keys,
            route: ModalRoute.MaybeOf(context),
            userLeading: CurrentWidget.Leading != null
                ? new Visibility(visible: !_searchIsActive, child: CurrentWidget.Leading)
                : null,
            automaticallyImplyLeading: CurrentWidget.AutomaticallyImplyLeading,
            automaticallyImplyTitle: CurrentWidget.AutomaticallyImplyTitle,
            previousPageTitle: CurrentWidget.PreviousPageTitle,
            userMiddle: _animationController.IsAnimating ? new Text("") : _effectiveMiddle,
            userTrailing: CurrentWidget.Trailing != null
                ? new Visibility(visible: !_searchIsActive, child: CurrentWidget.Trailing)
                : null,
            userLargeTitle: CurrentWidget.LargeTitle,
            userBottom: userBottom ?? new SizedBox(width: 0.0, height: 0.0),
            padding: CurrentWidget.Padding,
            large: _isPortrait,
            staticBar: false, // This one scrolls.
            context: context);

        return new Builder(builder: outerContext => MediaQuery.WithNoTextScaling(
            outerContext,
            new AnimatedBuilder(
                animation: _searchAnimation,
                builder: (builderContext, _) => new SliverPersistentHeader(
                    pinned: true, // iOS navigation bars are always pinned.
                    @delegate: new LargeTitleNavigationBarSliverDelegate(
                        keys: _keys,
                        components: components,
                        userMiddle: _effectiveMiddle,
                        backgroundColor:
                            CupertinoDynamicColor.MaybeResolve(CurrentWidget.BackgroundColor, builderContext)
                            ?? CupertinoDynamicColor.Resolve(
                                CupertinoTheme.Of(builderContext).BarBackgroundColor,
                                builderContext),
                        automaticBackgroundVisibility: CurrentWidget.AutomaticBackgroundVisibility,
                        brightness: CurrentWidget.Brightness,
                        border: CurrentWidget.Border,
                        padding: CurrentWidget.Padding,
                        actionsForegroundColor: CupertinoDynamicColor.Resolve(
                            CupertinoTheme.Of(builderContext).PrimaryColor,
                            builderContext),
                        transitionBetweenRoutes: CurrentWidget.TransitionBetweenRoutes,
                        heroTag: CurrentWidget.HeroTag,
                        persistentHeight:
                            _persistentHeightAnimation.Value + MediaQuery.PaddingOf(builderContext).Top,
                        largeTitleHeight: _largeTitleHeightAnimation.Value,
                        alwaysShowMiddle: CurrentWidget.AlwaysShowMiddle && _effectiveMiddle != null,
                        stretchConfiguration: CurrentWidget.Stretch && !_searchIsActive
                            ? new OverScrollHeaderStretchConfiguration()
                            : null,
                        enableBackgroundFilterBlur: CurrentWidget.EnableBackgroundFilterBlur,
                        bottomMode: _searchIsActive
                            ? NavigationBarBottomMode.Always
                            : CurrentWidget.BottomMode ?? NavigationBarBottomMode.Automatic,
                        bottomHeight: BottomHeight,
                        controller: _animationController,
                        searchable: CurrentWidget.Searchable)))));
    }
}
