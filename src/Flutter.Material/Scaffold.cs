using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/scaffold.dart; flutter/packages/flutter/lib/src/material/app_bar.dart (approximate)

public sealed class Scaffold : StatelessWidget
{
    public Scaffold(
        Widget body,
        AppBar? appBar = null,
        Widget? floatingActionButton = null,
        Widget? bottomNavigationBar = null,
        Color? backgroundColor = null,
        Key? key = null) : base(key)
    {
        Body = body;
        AppBar = appBar;
        FloatingActionButton = floatingActionButton;
        BottomNavigationBar = bottomNavigationBar;
        BackgroundColor = backgroundColor;
    }

    public Widget Body { get; }

    public AppBar? AppBar { get; }

    public Widget? FloatingActionButton { get; }

    public Widget? BottomNavigationBar { get; }

    public Color? BackgroundColor { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var effectiveBackground = BackgroundColor ?? theme.ScaffoldBackgroundColor;

        var children = new List<Widget>();
        if (AppBar != null)
        {
            children.Add(AppBar);
        }

        children.Add(new Expanded(child: Body));

        if (BottomNavigationBar != null)
        {
            children.Add(BottomNavigationBar);
        }

        Widget content = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children: children);

        if (FloatingActionButton != null)
        {
            content = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    content,
                    new Positioned(
                        right: 16,
                        bottom: 16,
                        child: FloatingActionButton),
                ]);
        }

        return new Container(
            color: effectiveBackground,
            child: content);
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

        if (!AutomaticallyImplyLeading || !Navigator.CanPop(context))
        {
            return null;
        }

        return BuildDefaultLeading(context);
    }

    private static Widget BuildDefaultLeading(BuildContext context)
    {
        return new IconButton(
            icon: new Icon(Icons.ArrowBack),
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
