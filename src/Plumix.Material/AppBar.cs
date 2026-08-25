using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/app_bar.dart

/// <summary>
/// Ports Flutter's private <c>_ToolbarContainerLayout</c>: gives the toolbar a fixed height and
/// bottom-justifies it inside a container that may be shorter, so the overflow is clipped at the top.
/// </summary>
internal sealed class ToolbarContainerLayout : SingleChildLayoutDelegate
{
    public ToolbarContainerLayout(double toolbarHeight)
    {
        ToolbarHeight = toolbarHeight;
    }

    public double ToolbarHeight { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints)
    {
        return constraints.Tighten(height: ToolbarHeight);
    }

    public override Size GetSize(BoxConstraints constraints)
    {
        return new Size(constraints.MaxWidth, ToolbarHeight);
    }

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        return new Point(0.0, size.Height - childSize.Height);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not ToolbarContainerLayout old || old.ToolbarHeight != ToolbarHeight;
    }
}

/// <summary>Ports Flutter's private <c>_AppBarTitleBox</c>.</summary>
internal sealed class AppBarTitleBox : SingleChildRenderObjectWidget
{
    public AppBarTitleBox(Widget child, Key? key = null) : base(child, key)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAppBarTitleBox(Directionality.Of(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderAppBarTitleBox)renderObject).TextDirection = Directionality.Of(context);
    }
}

/// <summary>
/// Ports Flutter's private <c>_RenderAppBarTitleBox</c>: lays the title out with unbounded height,
/// reports the constrained size, and centers the child so an over-tall title overflows symmetrically.
/// </summary>
internal sealed class RenderAppBarTitleBox : RenderProxyBox
{
    private TextDirection _textDirection;

    public RenderAppBarTitleBox(TextDirection textDirection, RenderBox? child = null)
    {
        _textDirection = textDirection;
        Child = child;
    }

    /// <summary>
    /// Only meaningful for the resolved alignment, which <c>_RenderAppBarTitleBox</c> pins to
    /// <see cref="Alignment.Center"/> — a direction-agnostic value. Kept for parity with Dart.
    /// </summary>
    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        BoxConstraints innerConstraints = Constraints with { MaxHeight = double.PositiveInfinity };
        if (Child is null)
        {
            Size = Constraints.Constrain(default);
            return;
        }

        Child.Layout(innerConstraints, parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);
        ((BoxParentData)Child.parentData!).offset = Alignment.Center.AlongOffset(Size, Child.Size);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        BoxConstraints innerConstraints = constraints with { MaxHeight = double.PositiveInfinity };
        Size childSize = Child?.GetDryLayout(innerConstraints) ?? default;
        return constraints.Constrain(childSize);
    }
}

/// <summary>
/// Ports Flutter's private <c>_PreferredAppBarSize</c>. Avalonia's <see cref="Size"/> is a sealed value
/// type, so the toolbar/bottom metadata Dart keeps on its <c>Size</c> subclass rides on this record
/// instead; <see cref="AppBar.PreferredHeightFor(BuildContext, PreferredAppBarSize)"/> is the overload
/// that can still substitute <c>AppBarThemeData.ToolbarHeight</c>.
/// </summary>
public sealed record PreferredAppBarSize(double? ToolbarHeight, double? BottomHeight)
{
    public double Height => (ToolbarHeight ?? MaterialConstants.ToolbarHeight) + (BottomHeight ?? 0.0);

    /// <summary>Dart's <c>Size.fromHeight</c>: an unbounded width with the computed height.</summary>
    public Size ToSize() => new(double.PositiveInfinity, Height);
}

/// <summary>Ports Flutter's private <c>_AppBarDefaultsM2</c>/<c>_AppBarDefaultsM3</c>.</summary>
internal static class AppBarDefaults
{
    public static AppBarThemeData Of(BuildContext context, ThemeData theme)
    {
        return theme.UseMaterial3 ? M3(theme) : M2(theme);
    }

    public static AppBarThemeData M2(ThemeData theme)
    {
        ColorScheme colors = theme.ColorScheme;
        return new AppBarThemeData(
            BackgroundColor: colors.Brightness == Brightness.Dark ? colors.Surface : colors.Primary,
            ForegroundColor: colors.Brightness == Brightness.Dark ? colors.OnSurface : colors.OnPrimary,
            IconTheme: theme.IconTheme,
            CenterTitle: null,
            TitleSpacing: NavigationToolbar.KMiddleSpacing,
            ToolbarHeight: MaterialConstants.ToolbarHeight,
            ToolbarTextStyle: theme.TextTheme.BodyMedium,
            TitleTextStyle: theme.TextTheme.TitleLarge,
            ActionsPadding: new Thickness(),
            Elevation: 4.0,
            ShadowColor: Colors.Black);
    }

    public static AppBarThemeData M3(ThemeData theme)
    {
        ColorScheme colors = theme.ColorScheme;
        return new AppBarThemeData(
            BackgroundColor: colors.Surface,
            ForegroundColor: colors.OnSurface,
            IconTheme: new IconThemeData(Color: colors.OnSurface, Size: 24.0),
            ActionsIconTheme: new IconThemeData(Color: colors.OnSurfaceVariant, Size: 24.0),
            CenterTitle: null,
            TitleSpacing: NavigationToolbar.KMiddleSpacing,
            ToolbarHeight: 64.0,
            ToolbarTextStyle: theme.TextTheme.BodyMedium,
            TitleTextStyle: theme.TextTheme.TitleLarge,
            ActionsPadding: new Thickness(),
            Elevation: 0.0,
            ScrolledUnderElevation: 3.0,
            ShadowColor: Colors.Transparent,
            SurfaceTintColor: Colors.Transparent);
    }
}

/// <summary>A Material Design app bar.</summary>
public sealed class AppBar : StatefulWidget, IPreferredSizeWidget
{
    // Mirrors Flutter's `_kLeadingWidth` — "so the leading button is square".
    private const double LeadingWidthDefault = MaterialConstants.ToolbarHeight;

    // Mirrors Flutter's `_kMaxTitleTextScaleFactor`.
    private const double MaxTitleTextScaleFactor = 1.34;

    public AppBar(
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
        Widget? title = null,
        IReadOnlyList<Widget>? actions = null,
        bool automaticallyImplyActions = true,
        Widget? flexibleSpace = null,
        IPreferredSizeWidget? bottom = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        ScrollNotificationPredicate? notificationPredicate = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        WidgetStateColor? backgroundColor = null,
        Color? foregroundColor = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        bool primary = true,
        bool? centerTitle = null,
        bool excludeHeaderSemantics = false,
        double? titleSpacing = null,
        double toolbarOpacity = 1.0,
        double bottomOpacity = 1.0,
        double? toolbarHeight = null,
        double? leadingWidth = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        bool forceMaterialTransparency = false,
        bool useDefaultSemanticsOrder = true,
        Clip? clipBehavior = null,
        Thickness? actionsPadding = null,
        bool animateColor = false,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (double.IsNaN(elevation.Value) || elevation.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be non-negative.");
        }

        Leading = leading;
        AutomaticallyImplyLeading = automaticallyImplyLeading;
        Title = title;
        Actions = actions;
        AutomaticallyImplyActions = automaticallyImplyActions;
        FlexibleSpace = flexibleSpace;
        Bottom = bottom;
        Elevation = elevation;
        ScrolledUnderElevation = scrolledUnderElevation;
        NotificationPredicate = notificationPredicate ?? RawScrollbar.DefaultScrollNotificationPredicate;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Shape = shape;
        BackgroundColor = backgroundColor;
        ForegroundColor = foregroundColor;
        IconTheme = iconTheme;
        ActionsIconTheme = actionsIconTheme;
        Primary = primary;
        CenterTitle = centerTitle;
        ExcludeHeaderSemantics = excludeHeaderSemantics;
        TitleSpacing = titleSpacing;
        ToolbarOpacity = toolbarOpacity;
        BottomOpacity = bottomOpacity;
        ToolbarHeight = toolbarHeight;
        LeadingWidth = leadingWidth;
        ToolbarTextStyle = toolbarTextStyle;
        TitleTextStyle = titleTextStyle;
        SystemOverlayStyle = systemOverlayStyle;
        ForceMaterialTransparency = forceMaterialTransparency;
        UseDefaultSemanticsOrder = useDefaultSemanticsOrder;
        ClipBehavior = clipBehavior;
        ActionsPadding = actionsPadding;
        AnimateColor = animateColor;
        PreferredAppBarSize = new PreferredAppBarSize(toolbarHeight, bottom?.PreferredSize.Height);
    }

    public Widget? Leading { get; }

    public bool AutomaticallyImplyLeading { get; }

    public Widget? Title { get; }

    public IReadOnlyList<Widget>? Actions { get; }

    public bool AutomaticallyImplyActions { get; }

    public Widget? FlexibleSpace { get; }

    public IPreferredSizeWidget? Bottom { get; }

    public double? Elevation { get; }

    public double? ScrolledUnderElevation { get; }

    public ScrollNotificationPredicate NotificationPredicate { get; }

    public Color? ShadowColor { get; }

    public Color? SurfaceTintColor { get; }

    public ShapeBorder? Shape { get; }

    public WidgetStateColor? BackgroundColor { get; }

    public Color? ForegroundColor { get; }

    public IconThemeData? IconTheme { get; }

    public IconThemeData? ActionsIconTheme { get; }

    public bool Primary { get; }

    public bool? CenterTitle { get; }

    public bool ExcludeHeaderSemantics { get; }

    public double? TitleSpacing { get; }

    public double ToolbarOpacity { get; }

    public double BottomOpacity { get; }

    public double? ToolbarHeight { get; }

    public double? LeadingWidth { get; }

    public TextStyle? ToolbarTextStyle { get; }

    public TextStyle? TitleTextStyle { get; }

    public SystemUiOverlayStyle? SystemOverlayStyle { get; }

    public bool ForceMaterialTransparency { get; }

    public bool UseDefaultSemanticsOrder { get; }

    public Clip? ClipBehavior { get; }

    public Thickness? ActionsPadding { get; }

    public bool AnimateColor { get; }

    /// <summary>The toolbar/bottom metadata Dart keeps on its <c>_PreferredAppBarSize</c>.</summary>
    public PreferredAppBarSize PreferredAppBarSize { get; }

    public Size PreferredSize => PreferredAppBarSize.ToSize();

    /// <summary>
    /// Ports Dart's <c>AppBar.preferredHeightFor</c> for a plain <see cref="Size"/>, which carries no
    /// toolbar-height metadata and therefore always reports its own height.
    /// </summary>
    public static double PreferredHeightFor(BuildContext context, Size preferredSize)
    {
        return preferredSize.Height;
    }

    /// <summary>
    /// Ports Dart's <c>AppBar.preferredHeightFor</c>: when the widget did not set a toolbar height, the
    /// ambient <see cref="AppBarThemeData.ToolbarHeight"/> substitutes for it.
    /// </summary>
    public static double PreferredHeightFor(BuildContext context, PreferredAppBarSize preferredSize)
    {
        if (preferredSize.ToolbarHeight is null)
        {
            return (AppBarTheme.Of(context).ToolbarHeight ?? MaterialConstants.ToolbarHeight)
                   + (preferredSize.BottomHeight ?? 0.0);
        }

        return preferredSize.Height;
    }

    public override State CreateState()
    {
        return new AppBarState();
    }

    /// <summary>Ports Dart's private <c>AppBar._getEffectiveCenterTitle</c>.</summary>
    internal bool GetEffectiveCenterTitle(ThemeData theme, AppBarThemeData appBarTheme)
    {
        return CenterTitle
               ?? appBarTheme.CenterTitle
               ?? theme.Platform switch
               {
                   TargetPlatform.IOS or TargetPlatform.MacOS => Actions is null || Actions.Count < 2,
                   _ => false,
               };
    }

    private sealed class AppBarState : State
    {
        private ScrollNotificationObserverState? _scrollNotificationObserver;
        private bool _scrolledUnder;

        private AppBar CurrentWidget => (AppBar)StateWidget;

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            _scrollNotificationObserver?.RemoveListener(HandleScrollNotification);

            // While a drawer is open the app bar stops listening entirely, so scrolls inside the drawer
            // cannot flip `_scrolledUnder`; the last resolved value survives until the drawer closes.
            ScaffoldState? scaffold = Scaffold.MaybeOf(Context);
            if (scaffold is not null && (scaffold.IsDrawerOpen || scaffold.IsEndDrawerOpen))
            {
                return;
            }

            _scrollNotificationObserver = ScrollNotificationObserver.MaybeOf(Context);
            _scrollNotificationObserver?.AddListener(HandleScrollNotification);
        }

        public override void Dispose()
        {
            if (_scrollNotificationObserver is not null)
            {
                _scrollNotificationObserver.RemoveListener(HandleScrollNotification);
                _scrollNotificationObserver = null;
            }

            base.Dispose();
        }

        private void HandleScrollNotification(ScrollNotification notification)
        {
            if (notification is not ScrollUpdateNotification
                || !CurrentWidget.NotificationPredicate(notification))
            {
                return;
            }

            bool oldScrolledUnder = _scrolledUnder;
            IScrollMetrics metrics = notification.Metrics;
            _scrolledUnder = metrics.AxisDirection switch
            {
                AxisDirection.Up => metrics.ExtentAfter > 0.0,
                AxisDirection.Down => metrics.ExtentBefore > 0.0,

                // Horizontal scrollers never change the scrolled-under state.
                _ => _scrolledUnder,
            };

            if (_scrolledUnder != oldScrolledUnder)
            {
                SetState(() => { });
            }
        }

        public override Widget Build(BuildContext context)
        {
            AppBar widget = CurrentWidget;
            var theme = Theme.Of(context);
            IconButtonThemeData iconButtonTheme = IconButtonTheme.Of(context);
            AppBarThemeData appBarTheme = AppBarTheme.Of(context);
            AppBarThemeData defaults = AppBarDefaults.Of(context, theme);
            ScaffoldState? scaffold = Scaffold.MaybeOf(context);
            ModalRoute? parentRoute = ModalRoute.MaybeOf(context);

            // A SliverAppBar computes "scrolled under" from the persistent header's shrink offset and
            // publishes it through the settings; that wins over this bar's own notification tracking.
            var settings = context.DependOnInherited<FlexibleSpaceBarSettings>();

            bool hasDrawer = scaffold?.HasDrawer ?? false;
            bool hasEndDrawer = scaffold?.HasEndDrawer ?? false;
            bool useCloseButton = parentRoute is PageRoute { FullscreenDialog: true };

            var states = new HashSet<WidgetState>();
            bool isScrolledUnder = settings?.IsScrolledUnder ?? _scrolledUnder;
            if (isScrolledUnder)
            {
                states.Add(WidgetState.ScrolledUnder);
            }

            double toolbarHeight = widget.ToolbarHeight
                                   ?? appBarTheme.ToolbarHeight
                                   ?? MaterialConstants.ToolbarHeight;

            WidgetStateColor? themeBackgroundColor = ThemeBackgroundColor(appBarTheme);
            Color backgroundColor = ResolveColor(
                states,
                widget.BackgroundColor,
                themeBackgroundColor,
                defaults.BackgroundColor!.Value);
            Color scrolledUnderBackground = ResolveColor(
                states,
                widget.BackgroundColor,
                themeBackgroundColor,
                theme.ColorScheme.SurfaceContainer);
            Color effectiveBackgroundColor = isScrolledUnder ? scrolledUnderBackground : backgroundColor;

            Color foregroundColor = widget.ForegroundColor
                                    ?? appBarTheme.ForegroundColor
                                    ?? defaults.ForegroundColor!.Value;

            double elevation = widget.Elevation ?? appBarTheme.Elevation ?? defaults.Elevation!.Value;
            double effectiveElevation = isScrolledUnder
                ? widget.ScrolledUnderElevation
                  ?? appBarTheme.ScrolledUnderElevation
                  ?? defaults.ScrolledUnderElevation
                  ?? elevation
                : elevation;

            IconThemeData overallIconTheme = widget.IconTheme
                                             ?? appBarTheme.IconTheme
                                             ?? defaults.IconTheme!.CopyWith(color: foregroundColor);
            Color? actionForegroundColor = widget.ForegroundColor ?? appBarTheme.ForegroundColor;
            IconThemeData actionsIconTheme = widget.ActionsIconTheme
                                             ?? appBarTheme.ActionsIconTheme
                                             ?? widget.IconTheme
                                             ?? appBarTheme.IconTheme
                                             ?? defaults.ActionsIconTheme?.CopyWith(color: actionForegroundColor)
                                             ?? overallIconTheme;

            Thickness actionsPadding = widget.ActionsPadding
                                       ?? appBarTheme.ActionsPadding
                                       ?? defaults.ActionsPadding!.Value;

            TextStyle toolbarTextStyle = widget.ToolbarTextStyle
                                         ?? appBarTheme.ToolbarTextStyle
                                         ?? defaults.ToolbarTextStyle! with { Color = foregroundColor };
            TextStyle titleTextStyle = widget.TitleTextStyle
                                       ?? appBarTheme.TitleTextStyle
                                       ?? defaults.TitleTextStyle! with { Color = foregroundColor };

            if (widget.ToolbarOpacity != 1.0)
            {
                double opacity = Curves.Interval(0.25, 1.0, Curves.FastOutSlowIn)(widget.ToolbarOpacity);
                if (titleTextStyle.Color.HasValue)
                {
                    titleTextStyle = titleTextStyle with
                    {
                        Color = WithOpacity(titleTextStyle.Color.Value, opacity),
                    };
                }

                if (toolbarTextStyle.Color.HasValue)
                {
                    toolbarTextStyle = toolbarTextStyle with
                    {
                        Color = WithOpacity(toolbarTextStyle.Color.Value, opacity),
                    };
                }

                overallIconTheme = overallIconTheme.CopyWith(
                    opacity: opacity * (overallIconTheme.Opacity ?? 1.0));
                actionsIconTheme = actionsIconTheme.CopyWith(
                    opacity: opacity * (actionsIconTheme.Opacity ?? 1.0));
            }

            Widget? leading = widget.Leading;
            if (leading is null && widget.AutomaticallyImplyLeading)
            {
                if (hasDrawer)
                {
                    leading = new DrawerButton(
                        style: IconButton.StyleFrom(iconSize: overallIconTheme.Size ?? 24.0));
                }
                else if (parentRoute?.ImpliesAppBarDismissal ?? false)
                {
                    leading = useCloseButton ? new CloseButton() : new BackButton();
                }
            }

            if (leading is not null)
            {
                if (theme.UseMaterial3)
                {
                    IconButtonThemeData effectiveIconButtonTheme = EffectiveIconButtonTheme(
                        iconButtonTheme,
                        overallIconTheme,
                        defaults.IconTheme);
                    leading = new IconButtonTheme(
                        data: effectiveIconButtonTheme,
                        child: leading is IconButton ? new Center(child: leading) : leading);
                }

                leading = new ConstrainedBox(
                    constraints: BoxConstraints.TightFor(
                        width: widget.LeadingWidth ?? appBarTheme.LeadingWidth ?? LeadingWidthDefault),
                    child: leading);
            }

            Widget? title = widget.Title;
            if (title is not null)
            {
                title = new AppBarTitleBox(child: title);
                if (!widget.ExcludeHeaderSemantics)
                {
                    title = new Semantics(
                        flags: SemanticsFlags.IsHeader,
                        namesRoute: PlatformDefaults.TargetPlatform
                            is not (TargetPlatform.IOS or TargetPlatform.MacOS),
                        child: title);
                }

                title = new DefaultTextStyle(
                    style: titleTextStyle,
                    softWrap: false,
                    overflow: TextOverflow.Ellipsis,
                    child: title);
                if (MediaQuery.MaybeOf(context) is not null)
                {
                    title = MediaQuery.WithClampedTextScaling(
                        context,
                        title,
                        maxScaleFactor: MaxTitleTextScaleFactor);
                }
            }

            Widget? actions = null;
            if (widget.Actions is { Count: > 0 })
            {
                actions = new Padding(
                    insets: actionsPadding,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: theme.UseMaterial3
                            ? CrossAxisAlignment.Center
                            : CrossAxisAlignment.Stretch,
                        spacing: 0,
                        children: widget.Actions));
            }
            else if (hasEndDrawer && widget.AutomaticallyImplyActions)
            {
                actions = new EndDrawerButton(
                    style: IconButton.StyleFrom(iconSize: overallIconTheme.Size ?? 24.0));
            }

            if (actions is not null)
            {
                IconButtonThemeData effectiveActionsIconButtonTheme = EffectiveIconButtonTheme(
                    iconButtonTheme,
                    actionsIconTheme,
                    defaults.ActionsIconTheme);
                actions = new IconButtonTheme(
                    data: effectiveActionsIconButtonTheme,
                    child: Plumix.Widgets.IconTheme.Merge(data: actionsIconTheme, child: actions));
            }

            Widget toolbar = new ClipRect(
                clipBehavior: widget.ClipBehavior ?? Clip.HardEdge,
                child: new CustomSingleChildLayout(
                    layoutDelegate: new ToolbarContainerLayout(toolbarHeight),
                    child: Plumix.Widgets.IconTheme.Merge(
                        data: overallIconTheme,
                        child: new DefaultTextStyle(
                            style: toolbarTextStyle,
                            child: new NavigationToolbar(
                                leading: leading,
                                middle: title,
                                trailing: actions,
                                centerMiddle: widget.GetEffectiveCenterTitle(theme, appBarTheme),
                                middleSpacing: widget.TitleSpacing
                                               ?? appBarTheme.TitleSpacing
                                               ?? NavigationToolbar.KMiddleSpacing)))));

            Widget appBar = toolbar;
            if (widget.Bottom is { } bottom)
            {
                appBar = new Column(
                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                    children:
                    [
                        new Flexible(
                            child: new ConstrainedBox(
                                constraints: new BoxConstraints(MaxHeight: toolbarHeight),
                                child: toolbar)),
                        widget.BottomOpacity == 1.0
                            ? (Widget)bottom
                            : new Opacity(
                                opacity: Curves.Interval(0.25, 1.0, Curves.FastOutSlowIn)(widget.BottomOpacity),
                                child: (Widget)bottom),
                    ]);
            }

            if (widget.Primary && MediaQuery.MaybeOf(context) is not null)
            {
                appBar = new SafeArea(bottom: false, child: appBar);
            }

            appBar = new Align(alignment: Alignment.TopCenter, child: appBar);

            if (widget.FlexibleSpace is { } flexibleSpace)
            {
                appBar = new Stack(
                    fit: StackFit.Passthrough,
                    children:
                    [
                        new Semantics(
                            sortKey: widget.UseDefaultSemanticsOrder ? new OrdinalSortKey(1.0) : null,
                            explicitChildNodes: true,
                            child: flexibleSpace),
                        new Semantics(
                            sortKey: widget.UseDefaultSemanticsOrder ? new OrdinalSortKey(0.0) : null,
                            explicitChildNodes: true,

                            // Creates a material widget to prevent the flexibleSpace from obscuring the
                            // ink splashes produced by appBar children.
                            child: new Material(
                                type: MaterialType.Transparency,
                                child: appBar)),
                    ]);
            }

            SystemUiOverlayStyle overlayStyle = widget.SystemOverlayStyle
                                                ?? appBarTheme.SystemOverlayStyle
                                                ?? defaults.SystemOverlayStyle
                                                ?? SystemOverlayStyleForBrightness(
                                                    ThemeData.EstimateBrightnessForColor(effectiveBackgroundColor),
                                                    theme.UseMaterial3 ? Colors.Transparent : null);

            return new Semantics(
                container: true,
                child: new AnnotatedRegion<SystemUiOverlayStyle>(
                    value: overlayStyle,
                    child: new Material(
                        color: theme.UseMaterial3 ? effectiveBackgroundColor : backgroundColor,
                        elevation: effectiveElevation,
                        type: widget.ForceMaterialTransparency
                            ? MaterialType.Transparency
                            : MaterialType.Canvas,
                        shadowColor: widget.ShadowColor ?? appBarTheme.ShadowColor ?? defaults.ShadowColor,

                        // `defaults.SurfaceTintColor` is deliberately skipped: the M3 default is
                        // transparent, which would defeat `ScrolledUnderElevation`.
                        surfaceTintColor: widget.SurfaceTintColor
                                          ?? appBarTheme.SurfaceTintColor
                                          ?? (theme.UseMaterial3 ? theme.ColorScheme.SurfaceTint : null),
                        shape: widget.Shape ?? appBarTheme.Shape ?? defaults.Shape,
                        animateColor: widget.AnimateColor,
                        child: new Semantics(explicitChildNodes: true, child: appBar))));
        }

        private static WidgetStateColor? ThemeBackgroundColor(AppBarThemeData appBarTheme)
        {
            return appBarTheme.BackgroundColorState
                   ?? (appBarTheme.BackgroundColor.HasValue
                       ? new WidgetStateColor(appBarTheme.BackgroundColor.Value)
                       : null);
        }

        /// <summary>Ports Dart's private <c>_AppBarState._resolveColor</c>.</summary>
        private static Color ResolveColor(
            IReadOnlySet<WidgetState> states,
            WidgetStateColor? widgetColor,
            WidgetStateColor? themeColor,
            Color defaultColor)
        {
            return widgetColor?.Resolve(states) ?? themeColor?.Resolve(states) ?? defaultColor;
        }

        /// <summary>
        /// Ports the `effectiveIconButtonTheme`/`effectiveActionsIconButtonTheme` locals: the ambient
        /// icon-button theme is kept untouched while the icon theme is still the resolved default, and
        /// otherwise has its foreground/overlay/size overridden from the icon theme.
        /// </summary>
        private static IconButtonThemeData EffectiveIconButtonTheme(
            IconButtonThemeData iconButtonTheme,
            IconThemeData iconTheme,
            IconThemeData? defaultIconTheme)
        {
            if (Equals(iconTheme, defaultIconTheme))
            {
                return iconButtonTheme;
            }

            ButtonStyle overriddenStyle = IconButton.StyleFrom(
                foregroundColor: iconTheme.Color,
                iconSize: iconTheme.Size);
            return new IconButtonThemeData(
                iconButtonTheme.Style is { } style
                    ? style with
                    {
                        ForegroundColor = overriddenStyle.ForegroundColor,
                        OverlayColor = overriddenStyle.OverlayColor,
                        IconSize = overriddenStyle.IconSize,
                    }
                    : null);
        }

        /// <summary>Ports Dart's private <c>_systemOverlayStyleForBrightness</c>.</summary>
        private static SystemUiOverlayStyle SystemOverlayStyleForBrightness(
            Brightness brightness,
            Color? backgroundColor)
        {
            SystemUiOverlayStyle style = brightness == Brightness.Dark
                ? SystemUiOverlayStyle.Light
                : SystemUiOverlayStyle.Dark;

            // Leave the system navigation bar untouched, for backwards compatibility with Dart.
            return new SystemUiOverlayStyle(
                StatusBarColor: backgroundColor,
                StatusBarBrightness: style.StatusBarBrightness,
                StatusBarIconBrightness: style.StatusBarIconBrightness);
        }

        private static Color WithOpacity(Color color, double opacity)
        {
            byte alpha = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}
