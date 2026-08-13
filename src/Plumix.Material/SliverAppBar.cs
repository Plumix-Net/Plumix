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
        Widget? bottom = null,
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
        Widget? bottom,
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
        if (snap && !floating) throw new ArgumentException("snap requires floating=true.", nameof(snap));
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
        Widget? bottom = null,
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
        Widget? bottom = null,
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
    public Widget? Bottom { get; }
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

internal sealed class SliverAppBarState : State
{
    private SliverAppBar CurrentWidget => (SliverAppBar)StateWidget;

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        double bottomHeight = widget.Bottom is IPreferredSizeWidget preferred ? preferred.PreferredSize.Height : 0;
        double topPadding = widget.Primary ? MediaQuery.MaybePaddingOf(context)?.Top ?? 0 : 0;
        double collapsedHeight = widget.Variant switch
        {
            SliverAppBarVariant.Medium or SliverAppBarVariant.Large =>
                widget.CollapsedHeight ?? topPadding + 64 + bottomHeight,
            _ when widget.Pinned && widget.Floating && widget.Bottom is not null =>
                (widget.CollapsedHeight ?? 0) + bottomHeight + topPadding,
            _ => (widget.CollapsedHeight ?? widget.ToolbarHeight) + bottomHeight + topPadding,
        };
        double expandedBodyHeight = widget.Variant switch
        {
            SliverAppBarVariant.Medium => widget.ExpandedHeight ?? 112 + bottomHeight,
            SliverAppBarVariant.Large => widget.ExpandedHeight ?? 152 + bottomHeight,
            _ => widget.ExpandedHeight ?? widget.ToolbarHeight + bottomHeight,
        };
        double expandedHeight = topPadding + expandedBodyHeight;
        if (expandedHeight < collapsedHeight)
            throw new InvalidOperationException("SliverAppBar expandedHeight must be >= collapsed height.");

        var flexibleSpace = widget.FlexibleSpace ?? BuildVariantFlexibleSpace(context, widget);
        return MediaQuery.MaybeOf(context) is null
            ? BuildHeader(widget, flexibleSpace, collapsedHeight, expandedHeight)
            : MediaQuery.RemovePadding(
                context,
                BuildHeader(widget, flexibleSpace, collapsedHeight, expandedHeight),
                removeBottom: true);
    }

    private Widget BuildHeader(SliverAppBar widget, Widget? flexibleSpace, double minExtent, double maxExtent) =>
        new SliverPersistentHeader(
            pinned: widget.Pinned,
            floating: widget.Floating,
            @delegate: new SliverAppBarDelegate(widget, flexibleSpace, minExtent, maxExtent));

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

    public SliverAppBarDelegate(SliverAppBar widget, Widget? flexibleSpace, double minExtent, double maxExtent)
    {
        _widget = widget;
        _flexibleSpace = flexibleSpace;
        MinExtent = minExtent;
        MaxExtent = maxExtent;
    }

    public override double MinExtent { get; }
    public override double MaxExtent { get; }

    public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
    {
        var theme = Theme.Of(context);
        var appBarTheme = AppBarTheme.Of(context);
        double delta = Math.Max(0.0001, MaxExtent - MinExtent);
        double t = Math.Clamp(shrinkOffset / delta, 0, 1);
        double currentExtent = Math.Max(MinExtent, MaxExtent - shrinkOffset);
        var variant = _widget.Variant;
        double toolbarOpacity = variant == SliverAppBarVariant.Small ? 1 : t;
        double flexibleTitleOpacity = variant == SliverAppBarVariant.Small ? 1 : 1 - t;
        bool elevated = _widget.ForceElevated || overlapsContent;
        double baseElevation = _widget.Elevation ?? appBarTheme.Elevation ?? (theme.UseMaterial3 ? 0 : 4);
        double elevation = elevated
            ? _widget.ScrolledUnderElevation
              ?? appBarTheme.ScrolledUnderElevation
              ?? (theme.UseMaterial3 ? 3 : baseElevation)
            : baseElevation;
        var background = _widget.BackgroundColor
                         ?? appBarTheme.BackgroundColor
                         ?? (theme.UseMaterial3 || theme.Brightness == Brightness.Dark ? theme.CanvasColor : theme.PrimaryColor);
        var foreground = _widget.ForegroundColor
                         ?? appBarTheme.ForegroundColor
                         ?? (theme.UseMaterial3 || theme.Brightness == Brightness.Dark ? theme.OnSurfaceColor : theme.OnPrimaryColor);
        var surfaceTint = _widget.SurfaceTintColor ?? appBarTheme.SurfaceTintColor ?? Colors.Transparent;
        if (surfaceTint.A > 0 && elevation > 0)
            background = NavigationSurfaceUtilities.ApplySurfaceTint(background, surfaceTint, elevation);
        var shadow = _widget.ShadowColor
                     ?? appBarTheme.ShadowColor
                     ?? (theme.UseMaterial3 ? Colors.Transparent : theme.ShadowColor);
        var shape = _widget.Shape ?? appBarTheme.Shape ?? new RoundedRectangleBorder(borderRadius:
            Plumix.Rendering.BorderRadius.Circular(0));

        var children = new List<Widget>();
        if (_flexibleSpace is not null)
        {
            children.Add(new Positioned(
                left: 0,
                top: 0,
                right: 0,
                bottom: 0,
                child: new FlexibleSpaceBarSettings(
                    flexibleTitleOpacity,
                    MinExtent,
                    MaxExtent,
                    currentExtent,
                    overlapsContent,
                    _widget.Leading is not null || _widget.AutomaticallyImplyLeading,
                    _flexibleSpace)));
        }

        Widget? toolbarTitle = _widget.Title ?? (_widget.TitleText is null ? null : new Text(_widget.TitleText));
        Widget toolbar = new AppBar(
            title: toolbarTitle,
            leading: _widget.Leading,
            automaticallyImplyLeading: _widget.AutomaticallyImplyLeading,
            automaticallyImplyActions: _widget.AutomaticallyImplyActions,
            actions: _widget.Actions,
            centerTitle: _widget.CenterTitle,
            primary: _widget.Primary,
            titleSpacing: _widget.TitleSpacing,
            iconTheme: _widget.IconTheme,
            actionsIconTheme: _widget.ActionsIconTheme,
            toolbarTextStyle: _widget.ToolbarTextStyle,
            titleTextStyle: _widget.TitleTextStyle,
            actionsPadding: _widget.ActionsPadding,
            toolbarHeight: _widget.ToolbarHeight,
            backgroundColor: Colors.Transparent,
            foregroundColor: foreground,
            systemOverlayStyle: _widget.SystemOverlayStyle,
            bottom: _widget.Bottom);
        children.Add(new Positioned(
            left: 0,
            top: 0,
            right: 0,
            child: new Opacity(toolbarOpacity, toolbar)));

        Widget content = new DecoratedBox(
            new BoxDecoration(
                Color: _widget.ForceMaterialTransparency ? Colors.Transparent : background,
                Border: ShapeBorderGeometry.SideOrNull(
                    shape) is { } shapeSide ? Plumix.Rendering.Border.FromBorderSide(shapeSide) : null,
                BorderRadius: ShapeBorderGeometry.ResolveRadius(shape),
                BoxShadows: BuildShadows(shadow, elevation)),
            new Stack(fit: StackFit.Expand, children: children));
        if ((_widget.ClipBehavior ?? Clip.None) != Clip.None)
            content = new ClipRRect(ShapeBorderGeometry.ResolveRadius(shape), content);
        return content;
    }

    public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) =>
        oldDelegate is not SliverAppBarDelegate old
        || !ReferenceEquals(_widget, old._widget)
        || !ReferenceEquals(_flexibleSpace, old._flexibleSpace)
        || MinExtent != old.MinExtent
        || MaxExtent != old.MaxExtent;

    private static BoxShadows? BuildShadows(Color color, double elevation)
    {
        if (elevation <= 0 || color.A == 0) return null;
        var shadow = Color.FromArgb((byte)Math.Round(color.A * 0.20), color.R, color.G, color.B);
        return new BoxShadows(new BoxShadow
        {
            OffsetY = Math.Max(1, elevation * 0.5),
            Blur = Math.Max(2, elevation * 2.4),
            Color = shadow,
        });
    }
}
