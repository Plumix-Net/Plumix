using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/theme_data.dart (approximate)

public enum Brightness
{
    Light,
    Dark,
}

public enum MaterialTapTargetSize
{
    Padded,
    ShrinkWrap,
}

public readonly record struct VisualDensity(double Horizontal = 0, double Vertical = 0)
{
    public static VisualDensity Standard => new();

    public static VisualDensity Comfortable => new(0, -1);

    public static VisualDensity Compact => new(-2, -2);

    public Vector BaseSizeAdjustment => new(Horizontal * 4, Vertical * 4);

    public BoxConstraints EffectiveConstraints(BoxConstraints constraints)
    {
        Vector adjustment = BaseSizeAdjustment;
        return constraints with
        {
            MinWidth = Math.Clamp(
                constraints.MinWidth + adjustment.X,
                0.0,
                constraints.MaxWidth),
            MinHeight = Math.Clamp(
                constraints.MinHeight + adjustment.Y,
                0.0,
                constraints.MaxHeight),
        };
    }

    public static VisualDensity Lerp(VisualDensity a, VisualDensity b, double t)
    {
        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new VisualDensity(
            Horizontal: a.Horizontal + ((b.Horizontal - a.Horizontal) * clampedT),
            Vertical: a.Vertical + ((b.Vertical - a.Vertical) * clampedT));
    }
}

public sealed record AppBarThemeData(
    Color? BackgroundColor = null,
    Color? ForegroundColor = null,
    IconThemeData? IconTheme = null,
    IconThemeData? ActionsIconTheme = null,
    bool? CenterTitle = null,
    double? TitleSpacing = null,
    double? LeadingWidth = null,
    double? ToolbarHeight = null,
    TextStyle? ToolbarTextStyle = null,
    TextStyle? TitleTextStyle = null,
    Thickness? ActionsPadding = null,
    SystemUiOverlayStyle? SystemOverlayStyle = null,
    double? Elevation = null,
    double? ScrolledUnderElevation = null,
    Color? ShadowColor = null,
    Color? SurfaceTintColor = null,
    ShapeBorder? Shape = null,
    WidgetStateColor? BackgroundColorState = null)
{
    public AppBarThemeData CopyWith(
        Color? color = null,
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        bool? centerTitle = null,
        double? titleSpacing = null,
        double? leadingWidth = null,
        double? toolbarHeight = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        Thickness? actionsPadding = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        WidgetStateColor? backgroundColorState = null)
    {
        if (color.HasValue && backgroundColor.HasValue)
        {
            throw new ArgumentException(
                "color and backgroundColor mean the same thing. Only specify one.");
        }

        return new AppBarThemeData(
            BackgroundColor: backgroundColor
                             ?? color
                             ?? backgroundColorState?.DefaultValue
                             ?? BackgroundColor,
            ForegroundColor: foregroundColor ?? ForegroundColor,
            IconTheme: iconTheme ?? IconTheme,
            ActionsIconTheme: actionsIconTheme ?? ActionsIconTheme,
            CenterTitle: centerTitle ?? CenterTitle,
            TitleSpacing: titleSpacing ?? TitleSpacing,
            LeadingWidth: leadingWidth ?? LeadingWidth,
            ToolbarHeight: toolbarHeight ?? ToolbarHeight,
            ToolbarTextStyle: toolbarTextStyle ?? ToolbarTextStyle,
            TitleTextStyle: titleTextStyle ?? TitleTextStyle,
            ActionsPadding: actionsPadding ?? ActionsPadding,
            SystemOverlayStyle: systemOverlayStyle ?? SystemOverlayStyle,
            Elevation: elevation ?? Elevation,
            ScrolledUnderElevation: scrolledUnderElevation ?? ScrolledUnderElevation,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            Shape: shape ?? Shape,
            BackgroundColorState: backgroundColorState
                                  ?? (backgroundColor.HasValue || color.HasValue
                                      ? new WidgetStateColor((backgroundColor ?? color)!.Value)
                                      : BackgroundColorState));
    }

    public static AppBarThemeData Lerp(AppBarThemeData? a, AppBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new AppBarThemeData(
            BackgroundColor: LerpColor(a?.BackgroundColor, b?.BackgroundColor, clampedT),
            ForegroundColor: LerpColor(a?.ForegroundColor, b?.ForegroundColor, clampedT),
            IconTheme: LerpIconTheme(a?.IconTheme, b?.IconTheme, clampedT),
            ActionsIconTheme: LerpIconTheme(a?.ActionsIconTheme, b?.ActionsIconTheme, clampedT),
            CenterTitle: clampedT < 0.5 ? a?.CenterTitle : b?.CenterTitle,
            TitleSpacing: LerpDouble(a?.TitleSpacing, b?.TitleSpacing, clampedT),
            LeadingWidth: LerpDouble(a?.LeadingWidth, b?.LeadingWidth, clampedT),
            ToolbarHeight: LerpDouble(a?.ToolbarHeight, b?.ToolbarHeight, clampedT),
            ToolbarTextStyle: LerpTextStyle(a?.ToolbarTextStyle, b?.ToolbarTextStyle, clampedT),
            TitleTextStyle: LerpTextStyle(a?.TitleTextStyle, b?.TitleTextStyle, clampedT),
            ActionsPadding: LerpThickness(a?.ActionsPadding, b?.ActionsPadding, clampedT),
            SystemOverlayStyle: clampedT < 0.5 ? a?.SystemOverlayStyle : b?.SystemOverlayStyle,
            Elevation: LerpDouble(a?.Elevation, b?.Elevation, clampedT),
            ScrolledUnderElevation: LerpDouble(
                a?.ScrolledUnderElevation,
                b?.ScrolledUnderElevation,
                clampedT),
            ShadowColor: LerpColor(a?.ShadowColor, b?.ShadowColor, clampedT),
            SurfaceTintColor: LerpColor(a?.SurfaceTintColor, b?.SurfaceTintColor, clampedT),
            Shape: clampedT < 0.5 ? a?.Shape : b?.Shape);
    }

    private static double? LerpDouble(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        double from = a ?? 0.0;
        double to = b ?? 0.0;
        return from + ((to - from) * t);
    }

    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        var from = a ?? Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        var to = b ?? Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return new ColorTween().Evaluate(t, from, to);
    }
    private static IconThemeData? LerpIconTheme(IconThemeData? a, IconThemeData? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return new IconThemeData(
            Color: LerpColor(a?.Color, b?.Color, t),
            Size: LerpDouble(a?.Size, b?.Size, t),
            Opacity: LerpDouble(a?.Opacity, b?.Opacity, t));
    }

    private static TextStyle? LerpTextStyle(TextStyle? a, TextStyle? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return TextStyle.Lerp(a ?? new TextStyle(), b ?? new TextStyle(), t);
    }

    private static Thickness? LerpThickness(Thickness? a, Thickness? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        var from = a ?? default;
        var to = b ?? default;
        return new Thickness(
            from.Left + ((to.Left - from.Left) * t),
            from.Top + ((to.Top - from.Top) * t),
            from.Right + ((to.Right - from.Right) * t),
            from.Bottom + ((to.Bottom - from.Bottom) * t));
    }
}

public sealed record ThemeData
{
    private static readonly Color Material2LightCanvasColor = Color.Parse("#FFFAFAFA");
    private static readonly Color Material2DarkCanvasColor = Color.Parse("#FF303030");
    private static readonly Color Material2DarkCardColor = Color.Parse("#FF424242");
    private static readonly Color LightPrimaryColor = Color.Parse("#FF6750A4");
    private static readonly Color DefaultPrimaryColorLight = Color.Parse("#FFBBDEFB");
    private static readonly Color DefaultPrimaryColorDark = Color.Parse("#FF1976D2");
    private static readonly Color LightSecondaryColor = Color.Parse("#FF625B71");
    private static readonly Color LightPrimaryContainerColor = Color.Parse("#FFEADDFF");
    private static readonly Color LightOnPrimaryContainerColor = Color.Parse("#FF21005D");
    private static readonly Color LightSurfaceColor = Color.Parse("#FFFEF7FF");
    private static readonly Color LightOnSurfaceColor = Color.Parse("#FF1D1B20");
    private static readonly Color LightOnSurfaceVariantColor = Color.Parse("#FF49454F");
    private static readonly Color LightOutlineColor = Color.Parse("#FF79747E");
    private static readonly Color LightOutlineVariantColor = Color.Parse("#FFCAC4D0");
    private static readonly Color LightSurfaceContainerLowColor = Color.Parse("#FFF7F2FA");
    private static readonly Color LightSurfaceContainerColor = Color.Parse("#FFF3EDF7");
    private static readonly Color LightSurfaceContainerHighColor = Color.Parse("#FFECE6F0");
    private static readonly Color LightSurfaceContainerHighestColor = Color.Parse("#FFE6E0E9");
    private static readonly Color LightSecondaryContainerColor = Color.Parse("#FFE8DEF8");
    private static readonly Color LightOnSecondaryContainerColor = Color.Parse("#FF4A4458");
    private static readonly Color LightInverseSurfaceColor = Color.Parse("#FF322F35");
    private static readonly Color LightOnInverseSurfaceColor = Color.Parse("#FFF5EFF7");
    private static readonly Color LightInversePrimaryColor = Color.Parse("#FFD0BCFF");
    private static readonly Color LightErrorColor = Color.Parse("#FFB3261E");
    private static readonly Color LightOnErrorColor = Colors.White;
    private static readonly IReadOnlyDictionary<Type, ThemeExtension> EmptyExtensions =
        new ThemeExtensionMap([]);
    private static readonly object LocalizedThemeCacheLock = new();
    private static readonly List<LocalizedThemeEntry> LocalizedThemeCache = [];

    private AppBarThemeData? _appBarTheme;
    private TextButtonThemeData? _textButtonTheme;
    private ElevatedButtonThemeData? _elevatedButtonTheme;
    private OutlinedButtonThemeData? _outlinedButtonTheme;
    private FilledButtonThemeData? _filledButtonTheme;
    private IconButtonThemeData? _iconButtonTheme;
    private CardThemeData? _cardTheme;
    private ListTileThemeData? _listTileTheme;
    private DrawerThemeData? _drawerTheme;
    private FloatingActionButtonThemeData? _floatingActionButtonTheme;
    private BottomNavigationBarThemeData? _bottomNavigationBarTheme;
    private DividerThemeData? _dividerTheme;
    private ProgressIndicatorThemeData? _progressIndicatorTheme;
    private CheckboxThemeData? _checkboxTheme;
    private SwitchThemeData? _switchTheme;
    private RadioThemeData? _radioTheme;
    private SliderThemeData? _sliderTheme;
    private ExpansionTileThemeData? _expansionTileTheme;
    private BadgeThemeData? _badgeTheme;
    private TooltipThemeData? _tooltipTheme;
    private NavigationBarThemeData? _navigationBarTheme;
    private NavigationRailThemeData? _navigationRailTheme;
    private NavigationDrawerThemeData? _navigationDrawerTheme;
    private ToggleButtonsThemeData? _toggleButtonsTheme;
    private SegmentedButtonThemeData? _segmentedButtonTheme;
    private ChipThemeData? _chipTheme;
    private ActionIconThemeData? _actionIconTheme;
    private MaterialBannerThemeData? _bannerTheme;
    private SnackBarThemeData? _snackBarTheme;
    private DialogThemeData? _dialogTheme;
    private PopupMenuThemeData? _popupMenuTheme;
    private ButtonThemeData? _buttonTheme;
    private ButtonBarThemeData? _buttonBarTheme;
    private BottomAppBarThemeData? _bottomAppBarTheme;
    private DataTableThemeData? _dataTableTheme;
    private ScrollbarThemeData? _scrollbarTheme;
    private TabBarThemeData? _tabBarTheme;
    private BottomSheetThemeData? _bottomSheetTheme;
    private InputDecorationThemeData? _inputDecorationTheme;
    private DatePickerThemeData? _datePickerTheme;
    private TimePickerThemeData? _timePickerTheme;
    private DropdownMenuThemeData? _dropdownMenuTheme;
    private SearchBarThemeData? _searchBarTheme;
    private SearchViewThemeData? _searchViewTheme;
    private CarouselViewThemeData? _carouselViewTheme;
    private MenuBarThemeData? _menuBarTheme;
    private MenuButtonThemeData? _menuButtonTheme;
    private MenuThemeData? _menuTheme;
    private TextSelectionThemeData? _textSelectionTheme;
    private PageTransitionsTheme? _pageTransitionsTheme;

    public ThemeData(
        TargetPlatform? platform = null,
        Brightness? brightness = null,
        ColorScheme? colorScheme = null,
        Color? colorSchemeSeed = null,
        Typography? typography = null,
        TextTheme? textTheme = null,
        FontFamily? fontFamily = null,
        IReadOnlyList<string>? fontFamilyFallback = null,
        string? package = null,
        Color? scaffoldBackgroundColor = null,
        Color? canvasColor = null,
        Color? primaryColor = null,
        Color? secondaryColor = null,
        Color? onPrimaryColor = null,
        Color? primaryContainerColor = null,
        Color? onPrimaryContainerColor = null,
        bool? useMaterial3 = null,
        AppBarThemeData? appBarTheme = null,
        Color? shadowColor = null,
        Color? surfaceColor = null,
        Color? onSurfaceColor = null,
        Color? onSurfaceVariantColor = null,
        Color? outlineColor = null,
        Color? outlineVariantColor = null,
        Color? dividerColor = null,
        Color? cardColor = null,
        Color? surfaceContainerLowColor = null,
        Color? surfaceContainerHighestColor = null,
        Color? secondaryContainerColor = null,
        Color? onSecondaryContainerColor = null,
        Color? inverseSurfaceColor = null,
        Color? onInverseSurfaceColor = null,
        Color? errorColor = null,
        Color? onErrorColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        ButtonStyle? textButtonStyle = null,
        ButtonStyle? elevatedButtonStyle = null,
        ButtonStyle? outlinedButtonStyle = null,
        ButtonStyle? filledButtonStyle = null,
        ButtonStyle? iconButtonStyle = null,
        TextButtonThemeData? textButtonTheme = null,
        ElevatedButtonThemeData? elevatedButtonTheme = null,
        OutlinedButtonThemeData? outlinedButtonTheme = null,
        FilledButtonThemeData? filledButtonTheme = null,
        IconButtonThemeData? iconButtonTheme = null,
        CardThemeData? cardTheme = null,
        ListTileThemeData? listTileTheme = null,
        DrawerThemeData? drawerTheme = null,
        FloatingActionButtonThemeData? floatingActionButtonTheme = null,
        BottomNavigationBarThemeData? bottomNavigationBarTheme = null,
        DividerThemeData? dividerTheme = null,
        ProgressIndicatorThemeData? progressIndicatorTheme = null,
        CheckboxThemeData? checkboxTheme = null,
        SwitchThemeData? switchTheme = null,
        RadioThemeData? radioTheme = null,
        SliderThemeData? sliderTheme = null,
        ExpansionTileThemeData? expansionTileTheme = null,
        BadgeThemeData? badgeTheme = null,
        TooltipThemeData? tooltipTheme = null,
        Color? primaryColorLight = null,
        Color? primaryColorDark = null,
        TextTheme? primaryTextTheme = null,
        IconThemeData? iconTheme = null,
        IconThemeData? primaryIconTheme = null,
        Color? surfaceContainerColor = null,
        NavigationBarThemeData? navigationBarTheme = null,
        NavigationRailThemeData? navigationRailTheme = null,
        ChipThemeData? chipTheme = null,
        VisualDensity? visualDensity = null,
        ActionIconThemeData? actionIconTheme = null,
        NavigationDrawerThemeData? navigationDrawerTheme = null,
        ToggleButtonsThemeData? toggleButtonsTheme = null,
        SegmentedButtonThemeData? segmentedButtonTheme = null,
        MaterialBannerThemeData? bannerTheme = null,
        SnackBarThemeData? snackBarTheme = null,
        Color? inversePrimaryColor = null,
        DialogThemeData? dialogTheme = null,
        Color? surfaceContainerHighColor = null,
        PopupMenuThemeData? popupMenuTheme = null,
        ButtonThemeData? buttonTheme = null,
        ButtonBarThemeData? buttonBarTheme = null,
        BottomAppBarThemeData? bottomAppBarTheme = null,
        DataTableThemeData? dataTableTheme = null,
        ScrollbarThemeData? scrollbarTheme = null,
        TabBarThemeData? tabBarTheme = null,
        Color? disabledColor = null,
        Color? unselectedWidgetColor = null,
        Color? hintColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        BottomSheetThemeData? bottomSheetTheme = null,
        InputDecorationThemeData? inputDecorationTheme = null,
        DatePickerThemeData? datePickerTheme = null,
        TimePickerThemeData? timePickerTheme = null,
        DropdownMenuThemeData? dropdownMenuTheme = null,
        SearchBarThemeData? searchBarTheme = null,
        SearchViewThemeData? searchViewTheme = null,
        CarouselViewThemeData? carouselViewTheme = null,
        MenuBarThemeData? menuBarTheme = null,
        MenuButtonThemeData? menuButtonTheme = null,
        MenuThemeData? menuTheme = null,
        TextSelectionThemeData? textSelectionTheme = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        PageTransitionsTheme? pageTransitionsTheme = null,
        bool? applyElevationOverlayColor = null,
        IEnumerable<ThemeExtension>? extensions = null)
    {
        Platform = platform ?? ResolveDefaultPlatform();
        if (brightness.HasValue
            && colorScheme is not null
            && brightness.Value != colorScheme.Brightness)
        {
            throw new ArgumentException(
                "ThemeData brightness must match ColorScheme brightness.");
        }
        if (colorSchemeSeed.HasValue && colorScheme is not null)
        {
            throw new ArgumentException(
                "Only one of colorSchemeSeed and colorScheme may be specified.");
        }
        if (colorSchemeSeed.HasValue && primaryColor.HasValue)
        {
            throw new ArgumentException(
                "Only one of colorSchemeSeed and primaryColor may be specified.");
        }

        Brightness = brightness ?? colorScheme?.Brightness ?? Brightness.Light;
        UseMaterial3 = useMaterial3 ?? true;
        ColorScheme = colorSchemeSeed.HasValue
            ? ColorScheme.FromSeed(colorSchemeSeed.Value, Brightness)
            : colorScheme
              ?? (UseMaterial3
                  ? Brightness == Brightness.Dark
                      ? ColorScheme.Material3Dark
                      : ColorScheme.Material3Light
                  : Brightness == Brightness.Dark
                      ? ColorScheme.Dark()
                      : ColorScheme.Light());
        Typography = typography
                     ?? (UseMaterial3
                         ? Plumix.Material.Typography.Material2021(
                             platform: Platform,
                             colorScheme: ColorScheme)
                         : Plumix.Material.Typography.Material2014(platform: Platform));
        ApplyElevationOverlayColor = applyElevationOverlayColor
                                     ?? (UseMaterial3 && Brightness == Brightness.Dark);
        TextTheme defaultTextTheme = Brightness == Brightness.Dark
            ? Typography.White
            : Typography.Black;
        if (fontFamily is not null)
        {
            defaultTextTheme = defaultTextTheme.Apply(fontFamily: fontFamily);
        }
        if (fontFamilyFallback is not null)
        {
            defaultTextTheme = defaultTextTheme.Apply(fontFamilyFallback: fontFamilyFallback);
        }
        if (package is not null)
        {
            defaultTextTheme = defaultTextTheme.Apply(package: package);
        }
        TextTheme = defaultTextTheme.Merge(textTheme);
        CanvasColor = canvasColor
                      ?? (UseMaterial3
                          ? ColorScheme.Surface
                          : Brightness == Brightness.Dark
                              ? Material2DarkCanvasColor
                              : Material2LightCanvasColor);
        ScaffoldBackgroundColor = scaffoldBackgroundColor ?? CanvasColor;
        PrimaryColor = primaryColor
                       ?? (Brightness == Brightness.Dark
                           ? ColorScheme.Surface
                           : ColorScheme.Primary);
        PrimaryColorLight = primaryColorLight ?? DefaultPrimaryColorLight;
        PrimaryColorDark = primaryColorDark ?? DefaultPrimaryColorDark;
        TextTheme defaultPrimaryTextTheme = EstimateBrightnessForColor(PrimaryColor) == Brightness.Dark
            ? Typography.White
            : Typography.Black;
        if (fontFamily is not null)
        {
            defaultPrimaryTextTheme = defaultPrimaryTextTheme.Apply(fontFamily: fontFamily);
        }
        if (fontFamilyFallback is not null)
        {
            defaultPrimaryTextTheme = defaultPrimaryTextTheme.Apply(fontFamilyFallback: fontFamilyFallback);
        }
        if (package is not null)
        {
            defaultPrimaryTextTheme = defaultPrimaryTextTheme.Apply(package: package);
        }
        PrimaryTextTheme = defaultPrimaryTextTheme.Merge(primaryTextTheme);
        IconTheme = iconTheme
                    ?? new IconThemeData(
                        Color: Brightness == Brightness.Dark
                            ? Colors.White
                            : Color.FromArgb(0xDD, 0x00, 0x00, 0x00));
        PrimaryIconTheme = primaryIconTheme
                           ?? new IconThemeData(
                               Color: EstimateBrightnessForColor(PrimaryColor) == Brightness.Dark
                                   ? Colors.White
                                   : Colors.Black);
        SecondaryColor = secondaryColor ?? ColorScheme.Secondary;
        OnPrimaryColor = onPrimaryColor ?? ColorScheme.OnPrimary;
        PrimaryContainerColor = primaryContainerColor ?? ColorScheme.PrimaryContainer;
        OnPrimaryContainerColor = onPrimaryContainerColor ?? ColorScheme.OnPrimaryContainer;
        _appBarTheme = appBarTheme;
        ShadowColor = shadowColor ?? Colors.Black;
        SurfaceColor = surfaceColor ?? ColorScheme.Surface;
        OnSurfaceColor = onSurfaceColor ?? ColorScheme.OnSurface;
        OnSurfaceVariantColor = onSurfaceVariantColor ?? ColorScheme.OnSurfaceVariant;
        OutlineColor = outlineColor ?? ColorScheme.Outline;
        OutlineVariantColor = outlineVariantColor ?? ColorScheme.OutlineVariant;
        DividerColor = dividerColor
                       ?? (UseMaterial3
                           ? ColorScheme.Outline
                           : Brightness == Brightness.Dark
                               ? Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)
                               : Color.FromArgb(0x1F, 0x00, 0x00, 0x00));
        CardColor = cardColor
                    ?? (UseMaterial3
                        ? ColorScheme.Surface
                        : Brightness == Brightness.Dark
                            ? Material2DarkCardColor
                            : Colors.White);
        SurfaceContainerLowColor = surfaceContainerLowColor ?? ColorScheme.SurfaceContainerLow;
        SurfaceContainerColor = surfaceContainerColor ?? ColorScheme.SurfaceContainer;
        SurfaceContainerHighColor = surfaceContainerHighColor ?? ColorScheme.SurfaceContainerHigh;
        SurfaceContainerHighestColor = surfaceContainerHighestColor ?? ColorScheme.SurfaceContainerHighest;
        SecondaryContainerColor = secondaryContainerColor ?? ColorScheme.SecondaryContainer;
        OnSecondaryContainerColor = onSecondaryContainerColor ?? ColorScheme.OnSecondaryContainer;
        InverseSurfaceColor = inverseSurfaceColor ?? ColorScheme.InverseSurface;
        OnInverseSurfaceColor = onInverseSurfaceColor ?? ColorScheme.OnInverseSurface;
        InversePrimaryColor = inversePrimaryColor ?? ColorScheme.InversePrimary;
        ErrorColor = errorColor ?? ColorScheme.Error;
        OnErrorColor = onErrorColor ?? ColorScheme.OnError;
        DisabledColor = disabledColor ?? ApplyOpacity(OnSurfaceColor, 0.38);
        UnselectedWidgetColor = unselectedWidgetColor
                                ?? (Brightness == Brightness.Dark
                                    ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF)
                                    : Color.FromArgb(0x8A, 0x00, 0x00, 0x00));
        HintColor = hintColor ?? ApplyOpacity(OnSurfaceColor, 0.60);
        FocusColor = focusColor ?? ApplyOpacity(
            Brightness == Brightness.Dark ? Colors.White : Colors.Black,
            0.12);
        HoverColor = hoverColor ?? ApplyOpacity(
            Brightness == Brightness.Dark ? Colors.White : Colors.Black,
            0.04);
        HighlightColor = highlightColor ?? (Brightness == Brightness.Dark
            ? Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)
            : Color.FromArgb(0x66, 0xBC, 0xBC, 0xBC));
        SplashColor = splashColor ?? (Brightness == Brightness.Dark
            ? Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)
            : Color.FromArgb(0x66, 0xC8, 0xC8, 0xC8));
        SplashFactory = splashFactory ?? ResolveDefaultSplashFactory(UseMaterial3, Platform);
        MaterialTapTargetSize = materialTapTargetSize ?? MaterialTapTargetSize.Padded;
        TextButtonStyle = textButtonStyle;
        ElevatedButtonStyle = elevatedButtonStyle;
        OutlinedButtonStyle = outlinedButtonStyle;
        FilledButtonStyle = filledButtonStyle;
        IconButtonStyle = iconButtonStyle;
        _textButtonTheme = textButtonTheme;
        _elevatedButtonTheme = elevatedButtonTheme;
        _outlinedButtonTheme = outlinedButtonTheme;
        _filledButtonTheme = filledButtonTheme;
        _iconButtonTheme = iconButtonTheme;
        _cardTheme = cardTheme;
        _listTileTheme = listTileTheme;
        _drawerTheme = drawerTheme;
        _floatingActionButtonTheme = floatingActionButtonTheme;
        _bottomNavigationBarTheme = bottomNavigationBarTheme;
        _dividerTheme = dividerTheme;
        _progressIndicatorTheme = progressIndicatorTheme;
        _checkboxTheme = checkboxTheme;
        _switchTheme = switchTheme;
        _radioTheme = radioTheme;
        _sliderTheme = sliderTheme;
        _expansionTileTheme = expansionTileTheme;
        _badgeTheme = badgeTheme;
        _tooltipTheme = tooltipTheme;
        _navigationBarTheme = navigationBarTheme;
        _navigationRailTheme = navigationRailTheme;
        _navigationDrawerTheme = navigationDrawerTheme;
        _toggleButtonsTheme = toggleButtonsTheme;
        _segmentedButtonTheme = segmentedButtonTheme;
        _chipTheme = chipTheme;
        _actionIconTheme = actionIconTheme;
        _bannerTheme = bannerTheme;
        _snackBarTheme = snackBarTheme;
        _dialogTheme = dialogTheme;
        _popupMenuTheme = popupMenuTheme;
        _buttonTheme = buttonTheme;
        _buttonBarTheme = buttonBarTheme;
        _bottomAppBarTheme = bottomAppBarTheme;
        _dataTableTheme = dataTableTheme;
        _scrollbarTheme = scrollbarTheme;
        _tabBarTheme = tabBarTheme;
        _bottomSheetTheme = bottomSheetTheme;
        _inputDecorationTheme = inputDecorationTheme;
        _datePickerTheme = datePickerTheme;
        _timePickerTheme = timePickerTheme;
        _dropdownMenuTheme = dropdownMenuTheme;
        _searchBarTheme = searchBarTheme;
        _searchViewTheme = searchViewTheme;
        _carouselViewTheme = carouselViewTheme;
        _menuBarTheme = menuBarTheme;
        _menuButtonTheme = menuButtonTheme;
        _menuTheme = menuTheme;
        _textSelectionTheme = textSelectionTheme;
        _pageTransitionsTheme = pageTransitionsTheme;
        Extensions = CreateExtensionMap(extensions);
        VisualDensity = visualDensity ?? VisualDensity.Standard;
    }

    public TargetPlatform Platform { get; init; }

    public Brightness Brightness { get; init; }

    public ColorScheme ColorScheme { get; init; }

    public Typography Typography { get; init; }

    public bool ApplyElevationOverlayColor { get; init; }

    public TextTheme TextTheme { get; init; }

    public Color ScaffoldBackgroundColor { get; init; }

    public Color CanvasColor { get; init; }

    public Color PrimaryColor { get; init; }

    public Color PrimaryColorLight { get; init; }

    public Color PrimaryColorDark { get; init; }

    public TextTheme PrimaryTextTheme { get; init; }

    public IconThemeData IconTheme { get; init; }

    public IconThemeData PrimaryIconTheme { get; init; }

    public Color SecondaryColor { get; init; }

    public Color OnPrimaryColor { get; init; }

    public Color PrimaryContainerColor { get; init; }

    public Color OnPrimaryContainerColor { get; init; }

    public bool UseMaterial3 { get; init; }

    public AppBarThemeData AppBarTheme
    {
        get => _appBarTheme ?? new AppBarThemeData();
        init => _appBarTheme = value;
    }

    public Color ShadowColor { get; init; }

    public Color SurfaceColor { get; init; }

    public Color OnSurfaceColor { get; init; }

    public Color OnSurfaceVariantColor { get; init; }

    public Color OutlineColor { get; init; }

    public Color OutlineVariantColor { get; init; }

    public Color DividerColor { get; init; }

    public Color CardColor { get; init; }

    public Color SurfaceContainerLowColor { get; init; }

    public Color SurfaceContainerColor { get; init; }

    public Color SurfaceContainerHighColor { get; init; }

    public Color SurfaceContainerHighestColor { get; init; }

    public Color SecondaryContainerColor { get; init; }

    public Color OnSecondaryContainerColor { get; init; }

    public Color InverseSurfaceColor { get; init; }

    public Color OnInverseSurfaceColor { get; init; }

    public Color InversePrimaryColor { get; init; }

    public Color ErrorColor { get; init; }

    public Color OnErrorColor { get; init; }

    public Color DisabledColor { get; init; }

    public Color UnselectedWidgetColor { get; init; }

    public Color HintColor { get; init; }

    public Color FocusColor { get; init; }

    public Color HoverColor { get; init; }

    public Color HighlightColor { get; init; }

    public Color SplashColor { get; init; }

    public InteractiveInkFeatureFactory SplashFactory { get; init; }

    public MaterialTapTargetSize MaterialTapTargetSize { get; init; }

    public VisualDensity VisualDensity { get; init; }

    public DataTableThemeData DataTableTheme
    {
        get => _dataTableTheme ?? new DataTableThemeData();
        init => _dataTableTheme = value;
    }

    public ButtonStyle? TextButtonStyle { get; init; }

    public ButtonStyle? ElevatedButtonStyle { get; init; }

    public ButtonStyle? OutlinedButtonStyle { get; init; }

    public ButtonStyle? FilledButtonStyle { get; init; }

    public ButtonStyle? IconButtonStyle { get; init; }

    public TextButtonThemeData TextButtonTheme
    {
        get => _textButtonTheme ?? new TextButtonThemeData(style: TextButtonStyle);
        init => _textButtonTheme = value;
    }

    public ElevatedButtonThemeData ElevatedButtonTheme
    {
        get => _elevatedButtonTheme ?? new ElevatedButtonThemeData(style: ElevatedButtonStyle);
        init => _elevatedButtonTheme = value;
    }

    public OutlinedButtonThemeData OutlinedButtonTheme
    {
        get => _outlinedButtonTheme ?? new OutlinedButtonThemeData(style: OutlinedButtonStyle);
        init => _outlinedButtonTheme = value;
    }

    public FilledButtonThemeData FilledButtonTheme
    {
        get => _filledButtonTheme ?? new FilledButtonThemeData(style: FilledButtonStyle);
        init => _filledButtonTheme = value;
    }

    public IconButtonThemeData IconButtonTheme
    {
        get => _iconButtonTheme ?? new IconButtonThemeData(style: IconButtonStyle);
        init => _iconButtonTheme = value;
    }

    public CardThemeData CardTheme
    {
        get => _cardTheme ?? new CardThemeData();
        init => _cardTheme = value;
    }

    public ListTileThemeData ListTileTheme
    {
        get => _listTileTheme ?? new ListTileThemeData();
        init => _listTileTheme = value;
    }

    public DrawerThemeData DrawerTheme
    {
        get => _drawerTheme ?? new DrawerThemeData();
        init => _drawerTheme = value;
    }

    public FloatingActionButtonThemeData FloatingActionButtonTheme
    {
        get => _floatingActionButtonTheme ?? new FloatingActionButtonThemeData();
        init => _floatingActionButtonTheme = value;
    }

    public BottomNavigationBarThemeData BottomNavigationBarTheme
    {
        get => _bottomNavigationBarTheme ?? new BottomNavigationBarThemeData();
        init => _bottomNavigationBarTheme = value;
    }

    public DividerThemeData DividerTheme
    {
        get => _dividerTheme ?? new DividerThemeData();
        init => _dividerTheme = value;
    }

    public ProgressIndicatorThemeData ProgressIndicatorTheme
    {
        get => _progressIndicatorTheme ?? new ProgressIndicatorThemeData();
        init => _progressIndicatorTheme = value;
    }

    public CheckboxThemeData CheckboxTheme
    {
        get => _checkboxTheme ?? new CheckboxThemeData();
        init => _checkboxTheme = value;
    }

    public SwitchThemeData SwitchTheme
    {
        get => _switchTheme ?? new SwitchThemeData();
        init => _switchTheme = value;
    }

    public RadioThemeData RadioTheme
    {
        get => _radioTheme ?? new RadioThemeData();
        init => _radioTheme = value;
    }

    public SliderThemeData SliderTheme
    {
        get => _sliderTheme ?? new SliderThemeData();
        init => _sliderTheme = value;
    }

    public ExpansionTileThemeData ExpansionTileTheme
    {
        get => _expansionTileTheme ?? new ExpansionTileThemeData();
        init => _expansionTileTheme = value;
    }

    public BadgeThemeData BadgeTheme
    {
        get => _badgeTheme ?? new BadgeThemeData();
        init => _badgeTheme = value;
    }

    public TooltipThemeData TooltipTheme
    {
        get => _tooltipTheme ?? new TooltipThemeData();
        init => _tooltipTheme = value;
    }

    public NavigationBarThemeData NavigationBarTheme
    {
        get => _navigationBarTheme ?? new NavigationBarThemeData();
        init => _navigationBarTheme = value;
    }

    public NavigationRailThemeData NavigationRailTheme
    {
        get => _navigationRailTheme ?? new NavigationRailThemeData();
        init => _navigationRailTheme = value;
    }

    public NavigationDrawerThemeData NavigationDrawerTheme
    {
        get => _navigationDrawerTheme ?? new NavigationDrawerThemeData();
        init => _navigationDrawerTheme = value;
    }

    public ToggleButtonsThemeData ToggleButtonsTheme
    {
        get => _toggleButtonsTheme ?? new ToggleButtonsThemeData();
        init => _toggleButtonsTheme = value;
    }

    public SegmentedButtonThemeData SegmentedButtonTheme
    {
        get => _segmentedButtonTheme ?? new SegmentedButtonThemeData();
        init => _segmentedButtonTheme = value;
    }

    public ChipThemeData ChipTheme
    {
        get => _chipTheme ?? new ChipThemeData();
        init => _chipTheme = value;
    }

    public ActionIconThemeData? ActionIconTheme
    {
        get => _actionIconTheme;
        init => _actionIconTheme = value;
    }

    public MaterialBannerThemeData BannerTheme
    {
        get => _bannerTheme ?? new MaterialBannerThemeData();
        init => _bannerTheme = value;
    }

    public SnackBarThemeData SnackBarTheme
    {
        get => _snackBarTheme ?? new SnackBarThemeData();
        init => _snackBarTheme = value;
    }

    public DialogThemeData DialogTheme
    {
        get => _dialogTheme ?? new DialogThemeData();
        init => _dialogTheme = value;
    }

    public PopupMenuThemeData PopupMenuTheme
    {
        get => _popupMenuTheme ?? new PopupMenuThemeData();
        init => _popupMenuTheme = value;
    }

    public ButtonThemeData ButtonTheme
    {
        get => _buttonTheme ?? new ButtonThemeData();
        init => _buttonTheme = value;
    }

    public ScrollbarThemeData ScrollbarTheme
    {
        get => _scrollbarTheme ?? new ScrollbarThemeData();
        init => _scrollbarTheme = value;
    }

    public TabBarThemeData TabBarTheme
    {
        get => _tabBarTheme ?? new TabBarThemeData();
        init => _tabBarTheme = value;
    }

    public BottomSheetThemeData BottomSheetTheme
    {
        get => _bottomSheetTheme ?? new BottomSheetThemeData();
        init => _bottomSheetTheme = value;
    }

    public InputDecorationThemeData InputDecorationTheme
    {
        get => _inputDecorationTheme ?? new InputDecorationThemeData();
        init => _inputDecorationTheme = value;
    }

    public DatePickerThemeData DatePickerTheme
    {
        get => _datePickerTheme ?? new DatePickerThemeData();
        init => _datePickerTheme = value;
    }

    public TimePickerThemeData TimePickerTheme
    {
        get => _timePickerTheme ?? new TimePickerThemeData();
        init => _timePickerTheme = value;
    }

    public DropdownMenuThemeData DropdownMenuTheme
    {
        get => _dropdownMenuTheme ?? new DropdownMenuThemeData();
        init => _dropdownMenuTheme = value;
    }

    public SearchBarThemeData SearchBarTheme
    {
        get => _searchBarTheme ?? new SearchBarThemeData();
        init => _searchBarTheme = value;
    }

    public SearchViewThemeData SearchViewTheme
    {
        get => _searchViewTheme ?? new SearchViewThemeData();
        init => _searchViewTheme = value;
    }

    public CarouselViewThemeData CarouselViewTheme
    {
        get => _carouselViewTheme ?? new CarouselViewThemeData();
        init => _carouselViewTheme = value;
    }

    public MenuBarThemeData MenuBarTheme
    {
        get => _menuBarTheme ?? new MenuBarThemeData();
        init => _menuBarTheme = value;
    }

    public MenuButtonThemeData MenuButtonTheme
    {
        get => _menuButtonTheme ?? new MenuButtonThemeData();
        init => _menuButtonTheme = value;
    }

    public MenuThemeData MenuTheme
    {
        get => _menuTheme ?? new MenuThemeData();
        init => _menuTheme = value;
    }

    public TextSelectionThemeData TextSelectionTheme
    {
        get => _textSelectionTheme ?? new TextSelectionThemeData();
        init => _textSelectionTheme = value;
    }

    public PageTransitionsTheme PageTransitionsTheme
    {
        get => _pageTransitionsTheme ?? new PageTransitionsTheme();
        init => _pageTransitionsTheme = value;
    }

    public ButtonBarThemeData ButtonBarTheme
    {
        get => _buttonBarTheme ?? new ButtonBarThemeData();
        init => _buttonBarTheme = value;
    }

    public BottomAppBarThemeData BottomAppBarTheme
    {
        get => _bottomAppBarTheme ?? new BottomAppBarThemeData();
        init => _bottomAppBarTheme = value;
    }

    public IReadOnlyDictionary<Type, ThemeExtension> Extensions { get; init; }

    public T? Extension<T>() where T : ThemeExtension<T>
    {
        return Extensions.TryGetValue(typeof(T), out ThemeExtension? extension)
            ? (T)extension
            : null;
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
        color.R,
        color.G,
        color.B);

    public static ThemeData Light { get; } = new();

    public static ThemeData Dark { get; } = new(brightness: Brightness.Dark);

    public static ThemeData Localize(ThemeData baseTheme, TextTheme localTextGeometry)
    {
        ArgumentNullException.ThrowIfNull(baseTheme);
        ArgumentNullException.ThrowIfNull(localTextGeometry);
        lock (LocalizedThemeCacheLock)
        {
            LocalizedThemeEntry? cached = LocalizedThemeCache.FirstOrDefault(
                entry => ReferenceEquals(entry.BaseTheme, baseTheme)
                         && ReferenceEquals(entry.LocalTextGeometry, localTextGeometry));
            if (cached is not null)
            {
                return cached.Theme;
            }

            ThemeData localized = baseTheme with
            {
                PrimaryTextTheme = localTextGeometry.Merge(baseTheme.PrimaryTextTheme),
                TextTheme = localTextGeometry.Merge(baseTheme.TextTheme),
            };
            if (LocalizedThemeCache.Count == 5)
            {
                LocalizedThemeCache.RemoveAt(0);
            }
            LocalizedThemeCache.Add(new LocalizedThemeEntry(baseTheme, localTextGeometry, localized));
            return localized;
        }
    }

    public static ThemeData Lerp(ThemeData a, ThemeData b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        ThemeData selected = t < 0.5 ? a : b;
        return selected with
        {
            ColorScheme = ColorScheme.Lerp(a.ColorScheme, b.ColorScheme, t),
            Typography = Typography.Lerp(a.Typography, b.Typography, t),
            TextTheme = TextTheme.Lerp(a.TextTheme, b.TextTheme, t),
            PrimaryTextTheme = TextTheme.Lerp(a.PrimaryTextTheme, b.PrimaryTextTheme, t),
            IconTheme = IconThemeData.Lerp(a.IconTheme, b.IconTheme, t),
            PrimaryIconTheme = IconThemeData.Lerp(a.PrimaryIconTheme, b.PrimaryIconTheme, t),
            VisualDensity = VisualDensity.Lerp(a.VisualDensity, b.VisualDensity, t),
            ScaffoldBackgroundColor = LerpColor(a.ScaffoldBackgroundColor, b.ScaffoldBackgroundColor, t),
            CanvasColor = LerpColor(a.CanvasColor, b.CanvasColor, t),
            PrimaryColor = LerpColor(a.PrimaryColor, b.PrimaryColor, t),
            PrimaryColorLight = LerpColor(a.PrimaryColorLight, b.PrimaryColorLight, t),
            PrimaryColorDark = LerpColor(a.PrimaryColorDark, b.PrimaryColorDark, t),
            SecondaryColor = LerpColor(a.SecondaryColor, b.SecondaryColor, t),
            OnPrimaryColor = LerpColor(a.OnPrimaryColor, b.OnPrimaryColor, t),
            PrimaryContainerColor = LerpColor(a.PrimaryContainerColor, b.PrimaryContainerColor, t),
            OnPrimaryContainerColor = LerpColor(
                a.OnPrimaryContainerColor,
                b.OnPrimaryContainerColor,
                t),
            ShadowColor = LerpColor(a.ShadowColor, b.ShadowColor, t),
            SurfaceColor = LerpColor(a.SurfaceColor, b.SurfaceColor, t),
            OnSurfaceColor = LerpColor(a.OnSurfaceColor, b.OnSurfaceColor, t),
            OnSurfaceVariantColor = LerpColor(a.OnSurfaceVariantColor, b.OnSurfaceVariantColor, t),
            OutlineColor = LerpColor(a.OutlineColor, b.OutlineColor, t),
            OutlineVariantColor = LerpColor(a.OutlineVariantColor, b.OutlineVariantColor, t),
            DividerColor = LerpColor(a.DividerColor, b.DividerColor, t),
            CardColor = LerpColor(a.CardColor, b.CardColor, t),
            SurfaceContainerLowColor = LerpColor(
                a.SurfaceContainerLowColor,
                b.SurfaceContainerLowColor,
                t),
            SurfaceContainerColor = LerpColor(a.SurfaceContainerColor, b.SurfaceContainerColor, t),
            SurfaceContainerHighColor = LerpColor(
                a.SurfaceContainerHighColor,
                b.SurfaceContainerHighColor,
                t),
            SurfaceContainerHighestColor = LerpColor(
                a.SurfaceContainerHighestColor,
                b.SurfaceContainerHighestColor,
                t),
            SecondaryContainerColor = LerpColor(
                a.SecondaryContainerColor,
                b.SecondaryContainerColor,
                t),
            OnSecondaryContainerColor = LerpColor(
                a.OnSecondaryContainerColor,
                b.OnSecondaryContainerColor,
                t),
            InverseSurfaceColor = LerpColor(a.InverseSurfaceColor, b.InverseSurfaceColor, t),
            OnInverseSurfaceColor = LerpColor(a.OnInverseSurfaceColor, b.OnInverseSurfaceColor, t),
            InversePrimaryColor = LerpColor(a.InversePrimaryColor, b.InversePrimaryColor, t),
            ErrorColor = LerpColor(a.ErrorColor, b.ErrorColor, t),
            OnErrorColor = LerpColor(a.OnErrorColor, b.OnErrorColor, t),
            DisabledColor = LerpColor(a.DisabledColor, b.DisabledColor, t),
            UnselectedWidgetColor = LerpColor(
                a.UnselectedWidgetColor,
                b.UnselectedWidgetColor,
                t),
            HintColor = LerpColor(a.HintColor, b.HintColor, t),
            FocusColor = LerpColor(a.FocusColor, b.FocusColor, t),
            HoverColor = LerpColor(a.HoverColor, b.HoverColor, t),
            HighlightColor = LerpColor(a.HighlightColor, b.HighlightColor, t),
            SplashColor = LerpColor(a.SplashColor, b.SplashColor, t),
            TextButtonStyle = ButtonStyle.Lerp(a.TextButtonStyle, b.TextButtonStyle, t),
            ElevatedButtonStyle = ButtonStyle.Lerp(a.ElevatedButtonStyle, b.ElevatedButtonStyle, t),
            OutlinedButtonStyle = ButtonStyle.Lerp(a.OutlinedButtonStyle, b.OutlinedButtonStyle, t),
            FilledButtonStyle = ButtonStyle.Lerp(a.FilledButtonStyle, b.FilledButtonStyle, t),
            IconButtonStyle = ButtonStyle.Lerp(a.IconButtonStyle, b.IconButtonStyle, t),
            ActionIconTheme = ActionIconThemeData.Lerp(a.ActionIconTheme, b.ActionIconTheme, t),
            AppBarTheme = AppBarThemeData.Lerp(a.AppBarTheme, b.AppBarTheme, t),
            BadgeTheme = BadgeThemeData.Lerp(a.BadgeTheme, b.BadgeTheme, t),
            BannerTheme = MaterialBannerThemeData.Lerp(a.BannerTheme, b.BannerTheme, t),
            BottomAppBarTheme = BottomAppBarThemeData.Lerp(a.BottomAppBarTheme, b.BottomAppBarTheme, t)
                ?? new BottomAppBarThemeData(),
            BottomNavigationBarTheme = BottomNavigationBarThemeData.Lerp(
                a.BottomNavigationBarTheme,
                b.BottomNavigationBarTheme,
                t),
            BottomSheetTheme = BottomSheetThemeData.Lerp(a.BottomSheetTheme, b.BottomSheetTheme, t)
                ?? new BottomSheetThemeData(),
            CardTheme = CardThemeData.Lerp(a.CardTheme, b.CardTheme, t),
            CarouselViewTheme = CarouselViewThemeData.Lerp(a.CarouselViewTheme, b.CarouselViewTheme, t),
            CheckboxTheme = CheckboxThemeData.Lerp(a.CheckboxTheme, b.CheckboxTheme, t),
            ChipTheme = ChipThemeData.Lerp(a.ChipTheme, b.ChipTheme, t) ?? new ChipThemeData(),
            DataTableTheme = DataTableThemeData.Lerp(a.DataTableTheme, b.DataTableTheme, t),
            DatePickerTheme = DatePickerThemeData.Lerp(a.DatePickerTheme, b.DatePickerTheme, t),
            DialogTheme = DialogThemeData.Lerp(a.DialogTheme, b.DialogTheme, t),
            DividerTheme = DividerThemeData.Lerp(a.DividerTheme, b.DividerTheme, t),
            DrawerTheme = DrawerThemeData.Lerp(a.DrawerTheme, b.DrawerTheme, t) ?? new DrawerThemeData(),
            DropdownMenuTheme = DropdownMenuThemeData.Lerp(
                a.DropdownMenuTheme,
                b.DropdownMenuTheme,
                t),
            ElevatedButtonTheme = ElevatedButtonThemeData.Lerp(
                a.ElevatedButtonTheme,
                b.ElevatedButtonTheme,
                t) ?? new ElevatedButtonThemeData(),
            ExpansionTileTheme = ExpansionTileThemeData.Lerp(
                a.ExpansionTileTheme,
                b.ExpansionTileTheme,
                t) ?? new ExpansionTileThemeData(),
            FilledButtonTheme = FilledButtonThemeData.Lerp(a.FilledButtonTheme, b.FilledButtonTheme, t)
                ?? new FilledButtonThemeData(),
            FloatingActionButtonTheme = FloatingActionButtonThemeData.Lerp(
                a.FloatingActionButtonTheme,
                b.FloatingActionButtonTheme,
                t) ?? new FloatingActionButtonThemeData(),
            IconButtonTheme = IconButtonThemeData.Lerp(a.IconButtonTheme, b.IconButtonTheme, t)
                ?? new IconButtonThemeData(),
            ListTileTheme = ListTileThemeData.Lerp(a.ListTileTheme, b.ListTileTheme, t)
                ?? new ListTileThemeData(),
            MenuBarTheme = MenuBarThemeData.Lerp(a.MenuBarTheme, b.MenuBarTheme, t)
                ?? new MenuBarThemeData(),
            MenuButtonTheme = MenuButtonThemeData.Lerp(a.MenuButtonTheme, b.MenuButtonTheme, t)
                ?? new MenuButtonThemeData(),
            MenuTheme = MenuThemeData.Lerp(a.MenuTheme, b.MenuTheme, t) ?? new MenuThemeData(),
            NavigationBarTheme = NavigationBarThemeData.Lerp(
                a.NavigationBarTheme,
                b.NavigationBarTheme,
                t) ?? new NavigationBarThemeData(),
            NavigationRailTheme = NavigationRailThemeData.Lerp(
                a.NavigationRailTheme,
                b.NavigationRailTheme,
                t) ?? new NavigationRailThemeData(),
            NavigationDrawerTheme = NavigationDrawerThemeData.Lerp(
                a.NavigationDrawerTheme,
                b.NavigationDrawerTheme,
                t) ?? new NavigationDrawerThemeData(),
            OutlinedButtonTheme = OutlinedButtonThemeData.Lerp(
                a.OutlinedButtonTheme,
                b.OutlinedButtonTheme,
                t) ?? new OutlinedButtonThemeData(),
            PopupMenuTheme = PopupMenuThemeData.Lerp(a.PopupMenuTheme, b.PopupMenuTheme, t)
                ?? new PopupMenuThemeData(),
            ProgressIndicatorTheme = ProgressIndicatorThemeData.Lerp(
                a.ProgressIndicatorTheme,
                b.ProgressIndicatorTheme,
                t) ?? new ProgressIndicatorThemeData(),
            RadioTheme = RadioThemeData.Lerp(a.RadioTheme, b.RadioTheme, t),
            ScrollbarTheme = ScrollbarThemeData.Lerp(a.ScrollbarTheme, b.ScrollbarTheme, t),
            SearchBarTheme = SearchBarThemeData.Lerp(a.SearchBarTheme, b.SearchBarTheme, t)
                ?? new SearchBarThemeData(),
            SearchViewTheme = SearchViewThemeData.Lerp(a.SearchViewTheme, b.SearchViewTheme, t)
                ?? new SearchViewThemeData(),
            SegmentedButtonTheme = SegmentedButtonThemeData.Lerp(
                a.SegmentedButtonTheme,
                b.SegmentedButtonTheme,
                t) ?? new SegmentedButtonThemeData(),
            SliderTheme = SliderThemeData.Lerp(a.SliderTheme, b.SliderTheme, t),
            SnackBarTheme = SnackBarThemeData.Lerp(a.SnackBarTheme, b.SnackBarTheme, t),
            SwitchTheme = SwitchThemeData.Lerp(a.SwitchTheme, b.SwitchTheme, t),
            TabBarTheme = TabBarThemeData.Lerp(a.TabBarTheme, b.TabBarTheme, t),
            TextButtonTheme = TextButtonThemeData.Lerp(a.TextButtonTheme, b.TextButtonTheme, t)
                ?? new TextButtonThemeData(),
            TextSelectionTheme = TextSelectionThemeData.Lerp(
                a.TextSelectionTheme,
                b.TextSelectionTheme,
                t) ?? new TextSelectionThemeData(),
            TimePickerTheme = TimePickerThemeData.Lerp(a.TimePickerTheme, b.TimePickerTheme, t),
            ToggleButtonsTheme = ToggleButtonsThemeData.Lerp(
                a.ToggleButtonsTheme,
                b.ToggleButtonsTheme,
                t) ?? new ToggleButtonsThemeData(),
            TooltipTheme = TooltipThemeData.Lerp(a.TooltipTheme, b.TooltipTheme, t)
                ?? new TooltipThemeData(),
            ButtonBarTheme = ButtonBarThemeData.Lerp(a.ButtonBarTheme, b.ButtonBarTheme, t)
                ?? new ButtonBarThemeData(),
            Extensions = LerpExtensions(a.Extensions, b.Extensions, t),
        };
    }

    private static IReadOnlyDictionary<Type, ThemeExtension> LerpExtensions(
        IReadOnlyDictionary<Type, ThemeExtension> a,
        IReadOnlyDictionary<Type, ThemeExtension> b,
        double t)
    {
        var result = new Dictionary<Type, ThemeExtension>();
        foreach ((Type type, ThemeExtension extension) in a)
        {
            b.TryGetValue(type, out ThemeExtension? other);
            result[type] = extension.LerpUntyped(other, t);
        }

        foreach ((Type type, ThemeExtension extension) in b)
        {
            result.TryAdd(type, extension);
        }

        return CreateExtensionMap(result.Values);
    }

    public static Brightness EstimateBrightnessForColor(Color color)
    {
        static double Linearize(byte component)
        {
            double value = component / 255.0;
            return value <= 0.03928
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        double luminance = (0.2126 * Linearize(color.R))
                           + (0.7152 * Linearize(color.G))
                           + (0.0722 * Linearize(color.B));
        return (luminance + 0.05) * (luminance + 0.05) > 0.15
            ? Brightness.Light
            : Brightness.Dark;
    }

    private static TargetPlatform ResolveDefaultPlatform()
    {
        if (OperatingSystem.IsIOS())
        {
            return TargetPlatform.IOS;
        }

        if (OperatingSystem.IsMacOS())
        {
            return TargetPlatform.MacOS;
        }

        if (OperatingSystem.IsAndroid())
        {
            return TargetPlatform.Android;
        }

        if (OperatingSystem.IsWindows())
        {
            return TargetPlatform.Windows;
        }

        if (OperatingSystem.IsLinux())
        {
            return TargetPlatform.Linux;
        }

        return TargetPlatform.Android;
    }

    private static IReadOnlyDictionary<Type, ThemeExtension> CreateExtensionMap(
        IEnumerable<ThemeExtension>? extensions)
    {
        if (extensions is null)
        {
            return EmptyExtensions;
        }

        var result = new Dictionary<Type, ThemeExtension>();
        foreach (ThemeExtension extension in extensions)
        {
            ArgumentNullException.ThrowIfNull(extension);
            if (!result.TryAdd(extension.Type, extension))
            {
                throw new ArgumentException(
                    $"Only one ThemeExtension with type {extension.Type.Name} may be provided.",
                    nameof(extensions));
            }
        }

        return result.Count == 0 ? EmptyExtensions : new ThemeExtensionMap(result.Values);
    }

    private sealed class ThemeExtensionMap : IReadOnlyDictionary<Type, ThemeExtension>
    {
        private readonly IReadOnlyDictionary<Type, ThemeExtension> _values;

        public ThemeExtensionMap(IEnumerable<ThemeExtension> extensions)
        {
            _values = extensions.ToDictionary(extension => extension.Type);
        }

        public IEnumerable<Type> Keys => _values.Keys;

        public IEnumerable<ThemeExtension> Values => _values.Values;

        public int Count => _values.Count;

        public ThemeExtension this[Type key] => _values[key];

        public bool ContainsKey(Type key) => _values.ContainsKey(key);

        public bool TryGetValue(Type key, out ThemeExtension value)
        {
            return _values.TryGetValue(key, out value!);
        }

        public IEnumerator<KeyValuePair<Type, ThemeExtension>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not IReadOnlyDictionary<Type, ThemeExtension> other || Count != other.Count)
            {
                return false;
            }

            foreach ((Type type, ThemeExtension extension) in _values)
            {
                if (!other.TryGetValue(type, out ThemeExtension? otherExtension)
                    || !Equals(extension, otherExtension))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach ((Type type, ThemeExtension extension) in _values.OrderBy(pair => pair.Key.FullName))
            {
                hash.Add(type);
                hash.Add(extension);
            }

            return hash.ToHashCode();
        }
    }

    private static Color LerpColor(Color a, Color b, double t)
    {
        return new ColorTween().Evaluate(t, a, b);
    }

    private static InteractiveInkFeatureFactory ResolveDefaultSplashFactory(
        bool useMaterial3,
        TargetPlatform platform)
    {
        if (!useMaterial3)
        {
            return InkSplash.SplashFactory;
        }

        return platform == TargetPlatform.Android
            ? InkSparkle.SplashFactory
            : InkRipple.SplashFactory;
    }

    private sealed record LocalizedThemeEntry(
        ThemeData BaseTheme,
        TextTheme LocalTextGeometry,
        ThemeData Theme);
}
