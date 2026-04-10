using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/scaffold.dart; flutter/packages/flutter/lib/src/material/app_bar.dart (approximate)

public sealed class Drawer : StatelessWidget
{
    private const double DefaultWidth = 304.0;
    private const double DefaultM2Elevation = 16.0;
    private const double DefaultM3Elevation = 1.0;

    public Drawer(
        Widget? child = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        double? width = null,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (double.IsNaN(elevation.Value) || double.IsInfinity(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Drawer elevation must be non-negative and finite.");
        }

        if (width.HasValue && (double.IsNaN(width.Value) || double.IsInfinity(width.Value) || width.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Drawer width must be positive and finite.");
        }

        Child = child;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        Width = width;
    }

    public Widget? Child { get; }

    public Color? BackgroundColor { get; }

    public double? Elevation { get; }

    public Color? ShadowColor { get; }

    public double? Width { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var useMaterial3 = theme.UseMaterial3;
        var effectiveBackground = BackgroundColor ?? (useMaterial3
            ? theme.SurfaceContainerLowColor
            : theme.CanvasColor);
        var effectiveElevation = Elevation ?? (useMaterial3
            ? DefaultM3Elevation
            : DefaultM2Elevation);
        var effectiveShadowColor = ShadowColor ?? (useMaterial3
            ? Colors.Transparent
            : theme.ShadowColor);
        var effectiveBoxShadows = BuildBoxShadows(effectiveShadowColor, effectiveElevation);

        return new Container(
            width: Width ?? DefaultWidth,
            decoration: new BoxDecoration(
                Color: effectiveBackground,
                BoxShadows: effectiveBoxShadows),
            child: Child ?? new SizedBox());
    }

    private static BoxShadows? BuildBoxShadows(Color shadowColor, double elevation)
    {
        if (elevation <= 0 || shadowColor.A == 0)
        {
            return null;
        }

        var keyShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(2, elevation * 2.4),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.20),
            IsInset = false,
        };

        var ambientShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(3, elevation * 3.2),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.14),
            IsInset = false,
        };

        return new BoxShadows(keyShadow, [ambientShadow]);
    }

    private static Color ApplyOpacity(Color color, double opacityMultiplier)
    {
        var baseOpacity = color.A / 255.0;
        var effectiveOpacity = Math.Clamp(baseOpacity * opacityMultiplier, 0, 1);
        var alpha = (byte)Math.Clamp((int)(effectiveOpacity * 255), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

public sealed class Scaffold : StatefulWidget
{
    private static readonly Color DefaultDrawerScrimColor = Color.FromArgb(0x8A, 0x00, 0x00, 0x00);

    public Scaffold(
        Widget body,
        AppBar? appBar = null,
        Widget? drawer = null,
        bool drawerBarrierDismissible = true,
        Color? drawerScrimColor = null,
        Widget? floatingActionButton = null,
        Widget? bottomNavigationBar = null,
        Color? backgroundColor = null,
        Key? key = null) : base(key)
    {
        Body = body;
        AppBar = appBar;
        Drawer = drawer;
        DrawerBarrierDismissible = drawerBarrierDismissible;
        DrawerScrimColor = drawerScrimColor;
        FloatingActionButton = floatingActionButton;
        BottomNavigationBar = bottomNavigationBar;
        BackgroundColor = backgroundColor;
    }

    public Widget Body { get; }

    public AppBar? AppBar { get; }

    public Widget? Drawer { get; }

    public bool DrawerBarrierDismissible { get; }

    public Color? DrawerScrimColor { get; }

    public Widget? FloatingActionButton { get; }

    public Widget? BottomNavigationBar { get; }

    public Color? BackgroundColor { get; }

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

    internal static Color ResolveDrawerScrimColor(Color? drawerScrimColor)
    {
        return drawerScrimColor ?? DefaultDrawerScrimColor;
    }
}

internal sealed class ScaffoldScope : InheritedWidget
{
    public ScaffoldScope(
        ScaffoldState scaffold,
        bool hasDrawer,
        bool isDrawerOpen,
        Widget child,
        Key? key = null) : base(key)
    {
        Scaffold = scaffold ?? throw new ArgumentNullException(nameof(scaffold));
        HasDrawer = hasDrawer;
        IsDrawerOpen = isDrawerOpen;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScaffoldState Scaffold { get; }

    public bool HasDrawer { get; }

    public bool IsDrawerOpen { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (ScaffoldScope)oldWidget;
        return !ReferenceEquals(Scaffold, oldScope.Scaffold)
               || HasDrawer != oldScope.HasDrawer
               || IsDrawerOpen != oldScope.IsDrawerOpen;
    }
}

public sealed class ScaffoldState : State
{
    private bool _isDrawerOpen;

    private Scaffold CurrentWidget => (Scaffold)StateWidget;

    public bool HasDrawer => CurrentWidget.Drawer != null;

    public bool IsDrawerOpen => _isDrawerOpen;

    public void OpenDrawer()
    {
        if (!HasDrawer || _isDrawerOpen)
        {
            return;
        }

        SetState(() => _isDrawerOpen = true);
    }

    public void CloseDrawer()
    {
        if (!_isDrawerOpen)
        {
            return;
        }

        SetState(() => _isDrawerOpen = false);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        if (!HasDrawer && _isDrawerOpen)
        {
            _isDrawerOpen = false;
        }
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var effectiveBackground = CurrentWidget.BackgroundColor ?? theme.ScaffoldBackgroundColor;

        var columnChildren = new List<Widget>();
        if (CurrentWidget.AppBar != null)
        {
            columnChildren.Add(CurrentWidget.AppBar);
        }

        columnChildren.Add(new Expanded(child: CurrentWidget.Body));

        if (CurrentWidget.BottomNavigationBar != null)
        {
            columnChildren.Add(CurrentWidget.BottomNavigationBar);
        }

        Widget content = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children: columnChildren);

        if (CurrentWidget.FloatingActionButton != null)
        {
            content = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    content,
                    new Positioned(
                        right: 16,
                        bottom: 16,
                        child: CurrentWidget.FloatingActionButton),
                ]);
        }

        if (_isDrawerOpen && CurrentWidget.Drawer != null)
        {
            var scrim = new Positioned(
                left: 0,
                top: 0,
                right: 0,
                bottom: 0,
                child: new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: CurrentWidget.DrawerBarrierDismissible ? CloseDrawer : null,
                    child: new Container(
                        color: Scaffold.ResolveDrawerScrimColor(CurrentWidget.DrawerScrimColor))));

            content = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    content,
                    scrim,
                    new Positioned(
                        left: 0,
                        top: 0,
                        bottom: 0,
                        child: CurrentWidget.Drawer),
                ]);
        }

        return new ScaffoldScope(
            scaffold: this,
            hasDrawer: HasDrawer,
            isDrawerOpen: _isDrawerOpen,
            child: new Container(
                color: effectiveBackground,
                child: content));
    }
}

public sealed class AppBar : StatelessWidget
{
    public AppBar(
        string? titleText = null,
        Widget? title = null,
        Widget? leading = null,
        bool automaticallyImplyLeading = true,
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
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        Key? key = null) : base(key)
    {
        if (toolbarHeight.HasValue && (double.IsNaN(toolbarHeight.Value) || double.IsInfinity(toolbarHeight.Value) || toolbarHeight.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(toolbarHeight), "Toolbar height must be positive and finite.");
        }

        if (leadingWidth.HasValue && (double.IsNaN(leadingWidth.Value) || double.IsInfinity(leadingWidth.Value) || leadingWidth.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(leadingWidth), "Leading width must be positive and finite.");
        }

        if (titleSpacing.HasValue && (double.IsNaN(titleSpacing.Value) || double.IsInfinity(titleSpacing.Value) || titleSpacing.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(titleSpacing), "Title spacing must be non-negative and finite.");
        }

        TitleText = titleText;
        Title = title;
        Leading = leading;
        AutomaticallyImplyLeading = automaticallyImplyLeading;
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
    }

    public string? TitleText { get; }

    public Widget? Title { get; }

    public Widget? Leading { get; }

    public bool AutomaticallyImplyLeading { get; }

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

    public Color? BackgroundColor { get; }

    public Color? ForegroundColor { get; }

    public SystemUiOverlayStyle? SystemOverlayStyle { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var effectiveBackground = BackgroundColor ?? theme.AppBarTheme.BackgroundColor ?? ResolveDefaultBackgroundColor(theme);
        var effectiveForeground = ForegroundColor ?? theme.AppBarTheme.ForegroundColor ?? ResolveDefaultForegroundColor(theme);
        var effectiveCenterTitle = ResolveEffectiveCenterTitle(theme);
        var effectiveTitleSpacing = TitleSpacing ?? theme.AppBarTheme.TitleSpacing ?? 16;
        var effectiveIconTheme = ResolveEffectiveIconTheme(theme, effectiveForeground);
        var effectiveActionsIconTheme = ResolveEffectiveActionsIconTheme(theme, effectiveForeground, effectiveIconTheme);
        var effectiveLeading = ResolveEffectiveLeading(context);
        var effectiveLeadingWidth = ResolveEffectiveLeadingWidth(theme);
        var effectiveActionsPadding = ActionsPadding ?? theme.AppBarTheme.ActionsPadding ?? new Thickness();
        var effectiveToolbarHeight = ResolveEffectiveToolbarHeight(theme);
        var effectiveToolbarTextStyle = ResolveToolbarTextStyle(theme, effectiveForeground);
        var effectiveTitleTextStyle = ResolveTitleTextStyle(theme, effectiveForeground);
        var effectiveSystemOverlayStyle = ResolveEffectiveSystemOverlayStyle(theme, effectiveBackground);

        var titleWidget = (Widget)new DefaultTextStyle(
            style: effectiveTitleTextStyle,
            child: Title ?? BuildDefaultTitle());
        var middle = (Widget)new Padding(
            insets: new Thickness(effectiveTitleSpacing, 0, effectiveTitleSpacing, 0),
            child: effectiveCenterTitle
                ? new Center(child: titleWidget)
                : titleWidget);

        var rowChildren = new List<Widget>();
        if (effectiveLeading != null)
        {
            rowChildren.Add(
                new SizedBox(
                    width: effectiveLeadingWidth,
                    height: effectiveToolbarHeight,
                    child: new Center(
                        child: new Flutter.Widgets.IconTheme(
                            data: effectiveIconTheme,
                            child: effectiveLeading))));
        }

        rowChildren.Add(new Expanded(child: middle));

        if (Actions.Count > 0)
        {
            rowChildren.Add(new Padding(
                insets: effectiveActionsPadding,
                child: new Flutter.Widgets.IconTheme(
                    data: effectiveActionsIconTheme,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: theme.UseMaterial3
                            ? CrossAxisAlignment.Center
                            : CrossAxisAlignment.Stretch,
                        spacing: 0,
                        children: Actions))));
        }
        else if (effectiveCenterTitle && effectiveLeading != null)
        {
            // Reserve symmetric trailing space when centering title without explicit actions.
            rowChildren.Add(new SizedBox(width: effectiveLeadingWidth));
        }

        Widget appBarContent = new SizedBox(
            height: effectiveToolbarHeight,
            child: new DefaultTextStyle(
                style: effectiveToolbarTextStyle,
                child: new Row(
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    spacing: 0,
                    children: rowChildren)));

        if (Primary && MediaQuery.MaybeOf(context) != null)
        {
            appBarContent = new SafeArea(bottom: false, child: appBarContent);
        }

        SystemChrome.SetSystemUiOverlayStyle(effectiveSystemOverlayStyle);

        return new Container(
            color: effectiveBackground,
            padding: Padding ?? new Thickness(),
            child: appBarContent);
    }

    private bool ResolveEffectiveCenterTitle(ThemeData theme)
    {
        if (CenterTitle.HasValue)
        {
            return CenterTitle.Value;
        }

        if (theme.AppBarTheme.CenterTitle.HasValue)
        {
            return theme.AppBarTheme.CenterTitle.Value;
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

        if (!Navigator.CanPop(context))
        {
            return null;
        }

        var useCloseButton = ModalRoute.MaybeOf(context) is PageRoute pageRoute && pageRoute.FullscreenDialog;
        return BuildDefaultLeading(context, useCloseButton);
    }

    private static Widget BuildDefaultDrawerLeading(BuildContext context)
    {
        return new IconButton(
            icon: new Icon(Icons.Menu),
            onPressed: () => Scaffold.Of(context).OpenDrawer());
    }

    private static Widget BuildDefaultLeading(BuildContext context, bool useCloseButton)
    {
        return new IconButton(
            icon: new Icon(useCloseButton ? Icons.Close : Icons.ArrowBack),
            onPressed: () => Navigator.MaybePop(context));
    }

    private double ResolveEffectiveLeadingWidth(ThemeData theme)
    {
        var effectiveLeadingWidth = LeadingWidth ?? theme.AppBarTheme.LeadingWidth ?? 56;
        if (double.IsNaN(effectiveLeadingWidth)
            || double.IsInfinity(effectiveLeadingWidth)
            || effectiveLeadingWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AppBarThemeData.LeadingWidth),
                "Leading width must be positive and finite.");
        }

        return effectiveLeadingWidth;
    }

    private IconThemeData ResolveEffectiveIconTheme(ThemeData theme, Color effectiveForeground)
    {
        var baseTheme = IconTheme
                        ?? theme.AppBarTheme.IconTheme
                        ?? ResolveDefaultIconTheme(theme, effectiveForeground);
        return baseTheme with
        {
            Color = baseTheme.Color ?? effectiveForeground,
        };
    }

    private IconThemeData ResolveEffectiveActionsIconTheme(
        ThemeData theme,
        Color effectiveForeground,
        IconThemeData effectiveIconTheme)
    {
        var actionForeground = ForegroundColor ?? theme.AppBarTheme.ForegroundColor;
        var baseTheme = ActionsIconTheme
                        ?? theme.AppBarTheme.ActionsIconTheme
                        ?? IconTheme
                        ?? theme.AppBarTheme.IconTheme
                        ?? ResolveDefaultActionsIconTheme(theme, actionForeground, effectiveIconTheme);

        return baseTheme with
        {
            Color = baseTheme.Color ?? actionForeground ?? effectiveForeground,
        };
    }

    private double ResolveEffectiveToolbarHeight(ThemeData theme)
    {
        var effectiveToolbarHeight = ToolbarHeight
                                     ?? theme.AppBarTheme.ToolbarHeight
                                     ?? ResolveDefaultToolbarHeight();
        if (double.IsNaN(effectiveToolbarHeight)
            || double.IsInfinity(effectiveToolbarHeight)
            || effectiveToolbarHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AppBarThemeData.ToolbarHeight),
                "Toolbar height must be positive and finite.");
        }

        return effectiveToolbarHeight;
    }

    private static double ResolveDefaultToolbarHeight()
    {
        return 56;
    }

    private static Color ResolveDefaultBackgroundColor(ThemeData theme)
    {
        if (theme.UseMaterial3)
        {
            return theme.CanvasColor;
        }

        return theme.Brightness == Brightness.Dark
            ? theme.CanvasColor
            : theme.PrimaryColor;
    }

    private static Color ResolveDefaultForegroundColor(ThemeData theme)
    {
        if (theme.UseMaterial3)
        {
            return theme.OnSurfaceColor;
        }

        return theme.Brightness == Brightness.Dark
            ? theme.OnSurfaceColor
            : theme.OnPrimaryColor;
    }

    private static IconThemeData ResolveDefaultIconTheme(ThemeData theme, Color effectiveForeground)
    {
        return theme.UseMaterial3
            ? new IconThemeData(Color: effectiveForeground, Size: 24)
            : new IconThemeData(Color: effectiveForeground);
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
            Color: actionForeground ?? theme.OnSurfaceVariantColor,
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

    private TextStyle ResolveToolbarTextStyle(ThemeData theme, Color effectiveForeground)
    {
        var baseStyle = theme.TextTheme.BodyMedium with
        {
            Color = effectiveForeground,
        };

        var overrideStyle = ToolbarTextStyle ?? theme.AppBarTheme.ToolbarTextStyle;
        return ComposeTextStyle(baseStyle, overrideStyle);
    }

    private TextStyle ResolveTitleTextStyle(ThemeData theme, Color effectiveForeground)
    {
        var baseStyle = theme.TextTheme.TitleLarge with
        {
            Color = effectiveForeground,
        };

        var overrideStyle = TitleTextStyle ?? theme.AppBarTheme.TitleTextStyle;
        return ComposeTextStyle(baseStyle, overrideStyle);
    }

    private SystemUiOverlayStyle ResolveEffectiveSystemOverlayStyle(ThemeData theme, Color effectiveBackground)
    {
        return SystemOverlayStyle
               ?? theme.AppBarTheme.SystemOverlayStyle
               ?? ResolveDefaultSystemOverlayStyle(theme, effectiveBackground);
    }

    private static SystemUiOverlayStyle ResolveDefaultSystemOverlayStyle(ThemeData theme, Color effectiveBackground)
    {
        var iconBrightness = EstimateIconBrightnessForColor(effectiveBackground);
        var systemBarColor = effectiveBackground;
        return new SystemUiOverlayStyle(
            StatusBarColor: systemBarColor,
            NavigationBarColor: systemBarColor,
            StatusBarIconBrightness: iconBrightness,
            NavigationBarIconBrightness: iconBrightness);
    }

    private static SystemUiIconBrightness EstimateIconBrightnessForColor(Color color)
    {
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
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
        if (TitleText is null)
        {
            return new SizedBox();
        }

        return new Text(
            TitleText,
            softWrap: false,
            maxLines: 1,
            overflow: TextOverflow.Ellipsis);
    }
}
