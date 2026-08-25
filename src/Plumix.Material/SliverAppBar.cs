using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/app_bar.dart

internal enum SliverAppBarVariant
{
    Small,
    Medium,
    Large,
}

public sealed class SliverAppBar : StatefulWidget
{
    public SliverAppBar(
        string? titleText = null,
        Widget? title = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        IReadOnlyList<Widget>? actions = null,
        bool automaticallyImplyActions = true,
        Widget? flexibleSpace = null,
        IPreferredSizeWidget? bottom = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        bool forceElevated = false,
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        bool primary = true,
        bool? centerTitle = null,
        bool excludeHeaderSemantics = false,
        double? titleSpacing = null,
        double? collapsedHeight = null,
        double? expandedHeight = null,
        bool floating = false,
        bool pinned = false,
        bool snap = false,
        bool stretch = false,
        double stretchTriggerOffset = 100,
        Func<Task>? onStretchTrigger = null,
        ShapeBorder? shape = null,
        double toolbarHeight = 56,
        double? leadingWidth = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        bool forceMaterialTransparency = false,
        bool useDefaultSemanticsOrder = true,
        Clip? clipBehavior = null,
        Thickness? actionsPadding = null,
        Key? key = null) : this(
        SliverAppBarVariant.Small, titleText, title, leading, automaticallyImplyLeading, actions,
        automaticallyImplyActions, flexibleSpace, bottom, elevation, scrolledUnderElevation,
        shadowColor, surfaceTintColor, forceElevated, backgroundColor, foregroundColor, iconTheme,
        actionsIconTheme, primary, centerTitle, excludeHeaderSemantics, titleSpacing, collapsedHeight,
        expandedHeight, floating, pinned, snap, stretch, stretchTriggerOffset, onStretchTrigger,
        shape, toolbarHeight, leadingWidth, toolbarTextStyle, titleTextStyle, systemOverlayStyle,
        forceMaterialTransparency, useDefaultSemanticsOrder, clipBehavior, actionsPadding, key)
    {
    }

    private SliverAppBar(
        SliverAppBarVariant variant,
        string? titleText,
        Widget? title,
        Widget? leading,
        bool automaticallyImplyLeading,
        IReadOnlyList<Widget>? actions,
        bool automaticallyImplyActions,
        Widget? flexibleSpace,
        IPreferredSizeWidget? bottom,
        double? elevation,
        double? scrolledUnderElevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        bool forceElevated,
        Color? backgroundColor,
        Color? foregroundColor,
        IconThemeData? iconTheme,
        IconThemeData? actionsIconTheme,
        bool primary,
        bool? centerTitle,
        bool excludeHeaderSemantics,
        double? titleSpacing,
        double? collapsedHeight,
        double? expandedHeight,
        bool floating,
        bool pinned,
        bool snap,
        bool stretch,
        double stretchTriggerOffset,
        Func<Task>? onStretchTrigger,
        ShapeBorder? shape,
        double toolbarHeight,
        double? leadingWidth,
        TextStyle? toolbarTextStyle,
        TextStyle? titleTextStyle,
        SystemUiOverlayStyle? systemOverlayStyle,
        bool forceMaterialTransparency,
        bool useDefaultSemanticsOrder,
        Clip? clipBehavior,
        Thickness? actionsPadding,
        Key? key) : base(key)
    {
        if (!double.IsFinite(toolbarHeight) || toolbarHeight <= 0) throw new ArgumentOutOfRangeException(nameof(toolbarHeight));
        if (collapsedHeight.HasValue && (!double.IsFinite(collapsedHeight.Value) || collapsedHeight.Value < toolbarHeight))
            throw new ArgumentOutOfRangeException(nameof(collapsedHeight));
        if (expandedHeight.HasValue && (!double.IsFinite(expandedHeight.Value) || expandedHeight.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(expandedHeight));
        if (snap && !floating)
            throw new ArgumentException("The \"snap\" argument only makes sense for floating app bars.", nameof(snap));
        if (!double.IsFinite(stretchTriggerOffset) || stretchTriggerOffset <= 0)
            throw new ArgumentOutOfRangeException(nameof(stretchTriggerOffset));
        ValidateElevation(elevation, nameof(elevation));
        ValidateElevation(scrolledUnderElevation, nameof(scrolledUnderElevation));

        Variant = variant;
        TitleText = titleText;
        Title = title;
        Leading = leading;
        AutomaticallyImplyLeading = automaticallyImplyLeading;
        Actions = actions ?? [];
        AutomaticallyImplyActions = automaticallyImplyActions;
        FlexibleSpace = flexibleSpace;
        Bottom = bottom;
        Elevation = elevation;
        ScrolledUnderElevation = scrolledUnderElevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        ForceElevated = forceElevated;
        BackgroundColor = backgroundColor;
        ForegroundColor = foregroundColor;
        IconTheme = iconTheme;
        ActionsIconTheme = actionsIconTheme;
        Primary = primary;
        CenterTitle = centerTitle;
        ExcludeHeaderSemantics = excludeHeaderSemantics;
        TitleSpacing = titleSpacing;
        CollapsedHeight = collapsedHeight;
        ExpandedHeight = expandedHeight;
        Floating = floating;
        Pinned = pinned;
        Snap = snap;
        Stretch = stretch;
        StretchTriggerOffset = stretchTriggerOffset;
        OnStretchTrigger = onStretchTrigger;
        Shape = shape;
        ToolbarHeight = toolbarHeight;
        LeadingWidth = leadingWidth;
        ToolbarTextStyle = toolbarTextStyle;
        TitleTextStyle = titleTextStyle;
        SystemOverlayStyle = systemOverlayStyle;
        ForceMaterialTransparency = forceMaterialTransparency;
        UseDefaultSemanticsOrder = useDefaultSemanticsOrder;
        ClipBehavior = clipBehavior;
        ActionsPadding = actionsPadding;
    }

    public static SliverAppBar Medium(
        Widget? title = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        IReadOnlyList<Widget>? actions = null,
        bool automaticallyImplyActions = true,
        Widget? flexibleSpace = null,
        IPreferredSizeWidget? bottom = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        bool forceElevated = false,
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        bool primary = true,
        bool? centerTitle = null,
        bool excludeHeaderSemantics = false,
        double? titleSpacing = null,
        double? collapsedHeight = null,
        double? expandedHeight = null,
        bool floating = false,
        bool pinned = true,
        bool snap = false,
        bool stretch = false,
        double stretchTriggerOffset = 100,
        Func<Task>? onStretchTrigger = null,
        ShapeBorder? shape = null,
        double toolbarHeight = 64,
        double? leadingWidth = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        bool forceMaterialTransparency = false,
        bool useDefaultSemanticsOrder = true,
        Clip? clipBehavior = null,
        Thickness? actionsPadding = null,
        Key? key = null) => new(
        SliverAppBarVariant.Medium, null, title, leading, automaticallyImplyLeading, actions,
        automaticallyImplyActions, flexibleSpace, bottom, elevation, scrolledUnderElevation,
        shadowColor, surfaceTintColor, forceElevated, backgroundColor, foregroundColor, iconTheme,
        actionsIconTheme, primary, centerTitle, excludeHeaderSemantics, titleSpacing, collapsedHeight,
        expandedHeight, floating, pinned, snap, stretch, stretchTriggerOffset, onStretchTrigger,
        shape, toolbarHeight, leadingWidth, toolbarTextStyle, titleTextStyle, systemOverlayStyle,
        forceMaterialTransparency, useDefaultSemanticsOrder, clipBehavior, actionsPadding, key);

    public static SliverAppBar Large(
        Widget? title = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        IReadOnlyList<Widget>? actions = null,
        bool automaticallyImplyActions = true,
        Widget? flexibleSpace = null,
        IPreferredSizeWidget? bottom = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        bool forceElevated = false,
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        bool primary = true,
        bool? centerTitle = null,
        bool excludeHeaderSemantics = false,
        double? titleSpacing = null,
        double? collapsedHeight = null,
        double? expandedHeight = null,
        bool floating = false,
        bool pinned = true,
        bool snap = false,
        bool stretch = false,
        double stretchTriggerOffset = 100,
        Func<Task>? onStretchTrigger = null,
        ShapeBorder? shape = null,
        double toolbarHeight = 64,
        double? leadingWidth = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        bool forceMaterialTransparency = false,
        bool useDefaultSemanticsOrder = true,
        Clip? clipBehavior = null,
        Thickness? actionsPadding = null,
        Key? key = null) => new(
        SliverAppBarVariant.Large, null, title, leading, automaticallyImplyLeading, actions,
        automaticallyImplyActions, flexibleSpace, bottom, elevation, scrolledUnderElevation,
        shadowColor, surfaceTintColor, forceElevated, backgroundColor, foregroundColor, iconTheme,
        actionsIconTheme, primary, centerTitle, excludeHeaderSemantics, titleSpacing, collapsedHeight,
        expandedHeight, floating, pinned, snap, stretch, stretchTriggerOffset, onStretchTrigger,
        shape, toolbarHeight, leadingWidth, toolbarTextStyle, titleTextStyle, systemOverlayStyle,
        forceMaterialTransparency, useDefaultSemanticsOrder, clipBehavior, actionsPadding, key);

    internal SliverAppBarVariant Variant { get; }
    public string? TitleText { get; }
    public Widget? Title { get; }
    public Widget? Leading { get; }
    public bool AutomaticallyImplyLeading { get; }
    public IReadOnlyList<Widget> Actions { get; }
    public bool AutomaticallyImplyActions { get; }
    public Widget? FlexibleSpace { get; }
    public IPreferredSizeWidget? Bottom { get; }
    public double? Elevation { get; }
    public double? ScrolledUnderElevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public bool ForceElevated { get; }
    public Color? BackgroundColor { get; }
    public Color? ForegroundColor { get; }
    public IconThemeData? IconTheme { get; }
    public IconThemeData? ActionsIconTheme { get; }
    public bool Primary { get; }
    public bool? CenterTitle { get; }
    public bool ExcludeHeaderSemantics { get; }
    public double? TitleSpacing { get; }
    public double? CollapsedHeight { get; }
    public double? ExpandedHeight { get; }
    public bool Floating { get; }
    public bool Pinned { get; }
    public bool Snap { get; }
    public bool Stretch { get; }
    public double StretchTriggerOffset { get; }
    public Func<Task>? OnStretchTrigger { get; }
    public ShapeBorder? Shape { get; }
    public double ToolbarHeight { get; }
    public double? LeadingWidth { get; }
    public TextStyle? ToolbarTextStyle { get; }
    public TextStyle? TitleTextStyle { get; }
    public SystemUiOverlayStyle? SystemOverlayStyle { get; }
    public bool ForceMaterialTransparency { get; }
    public bool UseDefaultSemanticsOrder { get; }
    public Clip? ClipBehavior { get; }
    public Thickness? ActionsPadding { get; }

    public override State CreateState() => new SliverAppBarState();

    private static void ValidateElevation(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(name);
    }
}

/// <summary>
/// This class is only stateful because it owns the ticker provider used by the floating app bar's
/// snap animation (through <see cref="FloatingHeaderSnapConfiguration"/>).
/// </summary>
internal sealed class SliverAppBarState : State
{
    private FloatingHeaderSnapConfiguration? _snapConfiguration;
    private OverScrollHeaderStretchConfiguration? _stretchConfiguration;
    private PersistentHeaderShowOnScreenConfiguration? _showOnScreenConfiguration;

    private SliverAppBar CurrentWidget => (SliverAppBar)StateWidget;

    public override void InitState()
    {
        base.InitState();
        UpdateSnapConfiguration();
        UpdateStretchConfiguration();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (SliverAppBar)oldWidget;
        if (CurrentWidget.Snap != old.Snap || CurrentWidget.Floating != old.Floating) UpdateSnapConfiguration();
        if (CurrentWidget.Stretch != old.Stretch) UpdateStretchConfiguration();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        double bottomHeight = widget.Bottom?.PreferredSize.Height ?? 0.0;
        double topPadding = widget.Primary ? MediaQuery.MaybePaddingOf(context)?.Top ?? 0 : 0;
        double collapsedHeight = widget.Pinned && widget.Floating && widget.Bottom is not null
            ? (widget.CollapsedHeight ?? 0) + bottomHeight + topPadding
            : (widget.CollapsedHeight ?? widget.ToolbarHeight) + bottomHeight + topPadding;
        double? expandedHeight = widget.ExpandedHeight;
        var flexibleSpace = widget.FlexibleSpace;
        switch (widget.Variant)
        {
            case SliverAppBarVariant.Medium:
                expandedHeight = widget.ExpandedHeight ?? 112 + bottomHeight;
                collapsedHeight = widget.CollapsedHeight ?? topPadding + 64 + bottomHeight;
                flexibleSpace ??= BuildVariantFlexibleSpace(context, widget);
                break;
            case SliverAppBarVariant.Large:
                expandedHeight = widget.ExpandedHeight ?? 152 + bottomHeight;
                collapsedHeight = widget.CollapsedHeight ?? topPadding + 64 + bottomHeight;
                flexibleSpace ??= BuildVariantFlexibleSpace(context, widget);
                break;
        }

        Widget header = new SliverPersistentHeader(
            pinned: widget.Pinned,
            floating: widget.Floating,
            @delegate: new SliverAppBarDelegate(
                widget,
                flexibleSpace,
                collapsedHeight,
                expandedHeight,
                topPadding,
                bottomHeight,
                this,
                _snapConfiguration,
                _stretchConfiguration,
                _showOnScreenConfiguration));
        return MediaQuery.MaybeOf(context) is null
            ? header
            : MediaQuery.RemovePadding(context, header, removeBottom: true);
    }

    private void UpdateSnapConfiguration()
    {
        _snapConfiguration = CurrentWidget.Snap && CurrentWidget.Floating
            ? new FloatingHeaderSnapConfiguration(
                curve: Curves.EaseOut,
                duration: TimeSpan.FromMilliseconds(200))
            : null;
        _showOnScreenConfiguration = CurrentWidget.Floating && CurrentWidget.Snap
            ? new PersistentHeaderShowOnScreenConfiguration(minShowOnScreenExtent: double.PositiveInfinity)
            : null;
    }

    private void UpdateStretchConfiguration()
    {
        _stretchConfiguration = CurrentWidget.Stretch
            ? new OverScrollHeaderStretchConfiguration(
                stretchTriggerOffset: CurrentWidget.StretchTriggerOffset,
                onStretchTrigger: CurrentWidget.OnStretchTrigger)
            : null;
    }

    private static Widget? BuildVariantFlexibleSpace(BuildContext context, SliverAppBar widget)
    {
        if (widget.Variant == SliverAppBarVariant.Small || widget.Title is null) return null;
        var theme = Theme.Of(context);
        return new FlexibleSpaceBar(
            title: new DefaultTextStyle(
                widget.Variant == SliverAppBarVariant.Large
                    ? theme.TextTheme.HeadlineMedium.CopyWith(color: widget.ForegroundColor ?? theme.OnSurfaceColor)
                    : theme.TextTheme.HeadlineSmall.CopyWith(color: widget.ForegroundColor ?? theme.OnSurfaceColor),
                widget.Title),
            titlePadding: widget.Variant == SliverAppBarVariant.Large
                ? new Thickness(16, 0, 16, 28)
                : new Thickness(16, 0, 16, 20),
            expandedTitleScale: 1);
    }
}

internal sealed class SliverAppBarDelegate : SliverPersistentHeaderDelegate
{
    private readonly SliverAppBar _widget;
    private readonly Widget? _flexibleSpace;
    private readonly double? _expandedHeight;
    private readonly double _topPadding;
    private readonly double _bottomHeight;
    private readonly ITickerProvider _vsync;
    private readonly FloatingHeaderSnapConfiguration? _snapConfiguration;
    private readonly OverScrollHeaderStretchConfiguration? _stretchConfiguration;
    private readonly PersistentHeaderShowOnScreenConfiguration? _showOnScreenConfiguration;

    public SliverAppBarDelegate(
        SliverAppBar widget,
        Widget? flexibleSpace,
        double collapsedHeight,
        double? expandedHeight,
        double topPadding,
        double bottomHeight,
        ITickerProvider vsync,
        FloatingHeaderSnapConfiguration? snapConfiguration,
        OverScrollHeaderStretchConfiguration? stretchConfiguration,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration)
    {
        if (!widget.Primary && topPadding != 0.0)
            throw new ArgumentOutOfRangeException(nameof(topPadding), "A non-primary app bar has no top padding.");

        _widget = widget;
        _flexibleSpace = flexibleSpace;
        _expandedHeight = expandedHeight;
        _topPadding = topPadding;
        _bottomHeight = bottomHeight;
        _vsync = vsync;
        _snapConfiguration = snapConfiguration;
        _stretchConfiguration = stretchConfiguration;
        _showOnScreenConfiguration = showOnScreenConfiguration;
        MinExtent = collapsedHeight;
        MaxExtent = Math.Max(topPadding + (expandedHeight ?? widget.ToolbarHeight + bottomHeight), collapsedHeight);
    }

    public override double MinExtent { get; }
    public override double MaxExtent { get; }
    public override ITickerProvider? Vsync => _vsync;
    public override FloatingHeaderSnapConfiguration? SnapConfiguration => _snapConfiguration;
    public override OverScrollHeaderStretchConfiguration? StretchConfiguration => _stretchConfiguration;
    public override PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration => _showOnScreenConfiguration;

    public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
    {
        double visibleMainHeight = MaxExtent - shrinkOffset - _topPadding;
        double extraToolbarHeight = Math.Max(MinExtent - _bottomHeight - _topPadding - _widget.ToolbarHeight, 0.0);
        double visibleToolbarHeight = visibleMainHeight - _bottomHeight - extraToolbarHeight;
        bool isScrolledUnder = overlapsContent
                               || _widget.ForceElevated
                               || (_widget.Pinned && shrinkOffset > MaxExtent - MinExtent);
        bool isPinnedWithOpacityFade =
            _widget.Pinned && _widget.Floating && _widget.Bottom is not null && extraToolbarHeight == 0.0;
        bool accessibleNavigation = MediaQuery.MaybeOf(context)?.AccessibleNavigation ?? false;
        double toolbarOpacity = !accessibleNavigation && (!_widget.Pinned || isPinnedWithOpacityFade)
            ? Math.Clamp(visibleToolbarHeight / _widget.ToolbarHeight, 0.0, 1.0)
            : 1.0;

        Widget? toolbarTitle = _widget.Title ?? (_widget.TitleText is null ? null : new Text(_widget.TitleText));
        Widget? effectiveTitle = _widget.Variant == SliverAppBarVariant.Small || toolbarTitle is null
            ? toolbarTitle
            : new AnimatedOpacity(
                opacity: isScrolledUnder ? 1.0 : 0.0,
                duration: TimeSpan.FromMilliseconds(500),
                curve: Curves.Cubic(0.2, 0.0, 0.0, 1.0),
                child: toolbarTitle);

        Widget? flexibleSpace = _flexibleSpace;
        if (toolbarTitle is null && flexibleSpace is not null && !_widget.ExcludeHeaderSemantics)
            flexibleSpace = new Semantics(flags: SemanticsFlags.IsHeader, child: flexibleSpace);

        return new FlexibleSpaceBarSettings(
            toolbarOpacity: toolbarOpacity,
            minExtent: MinExtent,
            maxExtent: MaxExtent,
            currentExtent: Math.Max(MinExtent, MaxExtent - shrinkOffset),
            isScrolledUnder: isScrolledUnder,
            hasLeading: _widget.Leading is not null || _widget.AutomaticallyImplyLeading,
            child: new AppBar(
                clipBehavior: _widget.ClipBehavior,
                leading: _widget.Leading,
                automaticallyImplyLeading: _widget.AutomaticallyImplyLeading,
                title: effectiveTitle,
                actions: _widget.Actions,
                automaticallyImplyActions: _widget.AutomaticallyImplyActions,
                flexibleSpace: flexibleSpace,
                bottom: _widget.Bottom,
                elevation: isScrolledUnder ? _widget.Elevation : 0.0,
                scrolledUnderElevation: _widget.ScrolledUnderElevation,
                shadowColor: _widget.ShadowColor,
                surfaceTintColor: _widget.SurfaceTintColor,
                backgroundColor: _widget.BackgroundColor,
                foregroundColor: _widget.ForegroundColor,
                iconTheme: _widget.IconTheme,
                actionsIconTheme: _widget.ActionsIconTheme,
                primary: _widget.Primary,
                centerTitle: _widget.CenterTitle,
                excludeHeaderSemantics: _widget.ExcludeHeaderSemantics,
                titleSpacing: _widget.TitleSpacing,
                shape: _widget.Shape,
                toolbarOpacity: toolbarOpacity,
                bottomOpacity: _widget.Pinned ? 1.0 : Math.Clamp(visibleMainHeight / _bottomHeight, 0.0, 1.0),
                toolbarHeight: _widget.ToolbarHeight,
                leadingWidth: _widget.LeadingWidth,
                toolbarTextStyle: _widget.ToolbarTextStyle,
                titleTextStyle: _widget.TitleTextStyle,
                systemOverlayStyle: _widget.SystemOverlayStyle,
                forceMaterialTransparency: _widget.ForceMaterialTransparency,
                useDefaultSemanticsOrder: _widget.UseDefaultSemanticsOrder,
                actionsPadding: _widget.ActionsPadding));
    }

    public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) =>
        oldDelegate is not SliverAppBarDelegate old
        || !ReferenceEquals(_widget, old._widget)
        || !ReferenceEquals(_flexibleSpace, old._flexibleSpace)
        || _bottomHeight != old._bottomHeight
        || _expandedHeight != old._expandedHeight
        || _topPadding != old._topPadding
        || !ReferenceEquals(_vsync, old._vsync)
        || !ReferenceEquals(_snapConfiguration, old._snapConfiguration)
        || !ReferenceEquals(_stretchConfiguration, old._stretchConfiguration)
        || !ReferenceEquals(_showOnScreenConfiguration, old._showOnScreenConfiguration)
        || MinExtent != old.MinExtent
        || MaxExtent != old.MaxExtent;
}
