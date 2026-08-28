using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/theme_data.dart

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

/// <summary>
/// Defines the visual density of user interface components, as an offset in logical pixels per
/// axis from the base component size.
/// </summary>
/// <remarks>
/// Density, in the context of a UI, is the vertical and horizontal "compactness" of the
/// components in the UI. It is unitless, since it means different things to different UI
/// components. Density values must lie between <see cref="MinimumDensity"/> and
/// <see cref="MaximumDensity"/>, inclusive.
/// </remarks>
public readonly record struct VisualDensity
{
    /// <summary>The minimum allowed density.</summary>
    public const double MinimumDensity = -4.0;

    /// <summary>The maximum allowed density.</summary>
    public const double MaximumDensity = 4.0;

    /// <summary>
    /// Creates a visual density that is adjusted by the given horizontal and vertical density
    /// values, each of which must lie within [<see cref="MinimumDensity"/>,
    /// <see cref="MaximumDensity"/>].
    /// </summary>
    public VisualDensity(double horizontal = 0.0, double vertical = 0.0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertical, MaximumDensity);
        ArgumentOutOfRangeException.ThrowIfLessThan(vertical, MinimumDensity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(horizontal, MaximumDensity);
        ArgumentOutOfRangeException.ThrowIfLessThan(horizontal, MinimumDensity);
        Horizontal = horizontal;
        Vertical = vertical;
    }

    /// <summary>The default profile: no adjustment on either axis.</summary>
    public static VisualDensity Standard => new();

    /// <summary>A profile that is slightly denser than <see cref="Standard"/>.</summary>
    public static VisualDensity Comfortable => new(horizontal: -1.0, vertical: -1.0);

    /// <summary>The densest profile, appropriate for desktop.</summary>
    public static VisualDensity Compact => new(horizontal: -2.0, vertical: -2.0);

    /// <summary>
    /// Dart's `VisualDensity.adaptivePlatformDensity`: the density for the host platform.
    /// </summary>
    public static VisualDensity AdaptivePlatformDensity =>
        DefaultDensityForPlatform(PlatformDefaults.TargetPlatform);

    /// <summary>Returns the default visual density for <paramref name="platform"/>.</summary>
    public static VisualDensity DefaultDensityForPlatform(TargetPlatform platform)
    {
        return platform switch
        {
            TargetPlatform.Android or TargetPlatform.IOS or TargetPlatform.Fuchsia => Standard,
            TargetPlatform.Linux or TargetPlatform.MacOS or TargetPlatform.Windows => Compact,
            _ => Standard,
        };
    }

    /// <summary>Copies this density, replacing the given axes.</summary>
    public VisualDensity CopyWith(double? horizontal = null, double? vertical = null)
    {
        return new VisualDensity(
            horizontal: horizontal ?? Horizontal,
            vertical: vertical ?? Vertical);
    }

    /// <summary>The horizontal density adjustment.</summary>
    public double Horizontal { get; }

    /// <summary>The vertical density adjustment.</summary>
    public double Vertical { get; }

    /// <summary>
    /// The number of logical pixels this density adds to (or removes from) a component's base
    /// size on each axis: one density unit is four logical pixels.
    /// </summary>
    public Vector BaseSizeAdjustment
    {
        get
        {
            const double interval = 4.0;
            return new Vector(Horizontal * interval, Vertical * interval);
        }
    }

    /// <summary>Linearly interpolates between two densities. <paramref name="t"/> is unclamped.</summary>
    public static VisualDensity Lerp(VisualDensity a, VisualDensity b, double t)
    {
        if (a == b)
        {
            return a;
        }

        return new VisualDensity(
            horizontal: a.Horizontal + ((b.Horizontal - a.Horizontal) * t),
            vertical: a.Vertical + ((b.Vertical - a.Vertical) * t));
    }

    /// <summary>
    /// Applies this density to <paramref name="constraints"/>, adjusting only the minimums and
    /// clamping each into [0, corresponding maximum].
    /// </summary>
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

    /// <summary>Dart's `toStringShort`.</summary>
    public override string ToString() =>
        $"VisualDensity(h: {DoubleProperty.FormatDouble(Horizontal)}, " +
        $"v: {DoubleProperty.FormatDouble(Vertical)})";
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

public sealed record ThemeData : IDiagnosticable
{
    private static readonly Color LightPrimaryColor = Color.Parse("#FF6750A4");
    private static readonly IReadOnlyDictionary<Type, ThemeExtension> EmptyExtensions =
        new ThemeExtensionMap([]);
    private static readonly IReadOnlyDictionary<Type, Adaptation> EmptyAdaptations =
        new Dictionary<Type, Adaptation>();
    private const int LocalizedThemeDataCacheSize = 5;
    private static readonly FifoCache<IdentityThemeDataCacheKey, ThemeData> LocalizedThemeCache =
        new(LocalizedThemeDataCacheSize);


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
        Color? secondaryHeaderColor = null,
        Color? canvasColor = null,
        Color? primaryColor = null,
        MaterialColor? primarySwatch = null,
        bool? useMaterial3 = null,
        AppBarThemeData? appBarTheme = null,
        Color? shadowColor = null,
        Color? dividerColor = null,
        Color? cardColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
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
        DialogThemeData? dialogTheme = null,
        PopupMenuThemeData? popupMenuTheme = null,
        ButtonThemeData? buttonTheme = null,
        Color? dialogBackgroundColor = null,
        Color? indicatorColor = null,
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
        NoDefaultCupertinoThemeData? cupertinoOverrideTheme = null,
        IEnumerable<ThemeExtension>? extensions = null,
        IEnumerable<Adaptation>? adaptations = null)
    {
        Platform = platform ?? PlatformDefaults.TargetPlatform;
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
        if (colorSchemeSeed.HasValue && primarySwatch is not null)
        {
            throw new ArgumentException(
                "Only one of colorSchemeSeed and primarySwatch may be specified.");
        }

        Brightness effectiveBrightness = brightness ?? colorScheme?.Brightness ?? Brightness.Light;
        bool isDark = effectiveBrightness == Brightness.Dark;
        UseMaterial3 = useMaterial3 ?? true;

        // Mirrors Flutter's derivation order: the Material 3 branch resolves the scheme-backed
        // colors first, then the shared Material 2 fallbacks fill in whatever is still unset, and
        // the Material 2 scheme itself is derived from `primarySwatch` last.
        ColorScheme? scheme = colorSchemeSeed.HasValue
            ? ColorScheme.FromSeed(colorSchemeSeed.Value, effectiveBrightness)
            : colorScheme;
        Color? resolvedPrimaryColor = primaryColor;
        Color? resolvedCanvasColor = canvasColor;
        Color? resolvedScaffoldBackgroundColor = scaffoldBackgroundColor;
        Color? resolvedCardColor = cardColor;
        Color? resolvedDividerColor = dividerColor;
        Color? resolvedDialogBackgroundColor = dialogBackgroundColor;
        Color? resolvedIndicatorColor = indicatorColor;
        if (colorSchemeSeed.HasValue || UseMaterial3)
        {
            scheme ??= isDark ? ColorScheme.Material3Dark : ColorScheme.Material3Light;
            resolvedPrimaryColor ??= isDark ? scheme.Surface : scheme.Primary;
            resolvedCanvasColor ??= scheme.Surface;
            resolvedScaffoldBackgroundColor ??= scheme.Surface;
            resolvedCardColor ??= scheme.Surface;
            resolvedDividerColor ??= scheme.Outline;
            resolvedDialogBackgroundColor ??= scheme.Surface;
            resolvedIndicatorColor ??= isDark ? scheme.OnSurface : scheme.OnPrimary;
        }

        MaterialColor swatch = primarySwatch ?? Colors.Blue;
        resolvedPrimaryColor ??= isDark ? Colors.Grey.Shade900 : swatch.Primary;
        PrimaryColorLight = primaryColorLight ?? (isDark ? Colors.Grey.Shade500 : swatch.Shade100);
        PrimaryColorDark = primaryColorDark ?? (isDark ? Colors.Black : swatch.Shade700);
        resolvedCanvasColor ??= isDark ? Colors.Grey[850]!.Value : Colors.Grey.Shade50;
        resolvedScaffoldBackgroundColor ??= resolvedCanvasColor;
        resolvedCardColor ??= isDark ? Colors.Grey.Shade800 : Colors.White;
        resolvedDividerColor ??= isDark
            ? Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x1F, 0x00, 0x00, 0x00);
        ColorScheme = scheme ?? ColorScheme.FromSwatch(
            primarySwatch: swatch,
            accentColor: isDark ? Colors.TealAccent.Shade200 : swatch.Shade500,
            cardColor: resolvedCardColor,
            backgroundColor: isDark ? Colors.Grey.Shade700 : swatch.Shade200,
            errorColor: Colors.Red.Shade700,
            brightness: effectiveBrightness);
        SecondaryHeaderColor = secondaryHeaderColor
                               ?? (isDark ? Colors.Grey.Shade700 : swatch.Shade50);
        resolvedDialogBackgroundColor ??= isDark ? Colors.Grey.Shade800 : Colors.White;
        CanvasColor = resolvedCanvasColor.Value;
        ScaffoldBackgroundColor = resolvedScaffoldBackgroundColor.Value;
        PrimaryColor = resolvedPrimaryColor.Value;
        CardColor = resolvedCardColor.Value;
        DividerColor = resolvedDividerColor.Value;
        DialogBackgroundColor = resolvedDialogBackgroundColor.Value;
        IndicatorColor = resolvedIndicatorColor
                         ?? (ColorScheme.Secondary == PrimaryColor
                             ? Colors.White
                             : ColorScheme.Secondary);

        Typography = typography
                     ?? (UseMaterial3
                         ? Plumix.Material.Typography.Material2021(
                             platform: Platform,
                             colorScheme: ColorScheme)
                         : Plumix.Material.Typography.Material2014(platform: Platform));
        ApplyElevationOverlayColor = applyElevationOverlayColor
                                     ?? ((colorSchemeSeed.HasValue || UseMaterial3)
                                         && brightness == Brightness.Dark);
        TextTheme defaultTextTheme = effectiveBrightness == Brightness.Dark
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
                        Color: effectiveBrightness == Brightness.Dark
                            ? Colors.White
                            : Color.FromArgb(0xDD, 0x00, 0x00, 0x00));
        PrimaryIconTheme = primaryIconTheme
                           ?? new IconThemeData(
                               Color: EstimateBrightnessForColor(PrimaryColor) == Brightness.Dark
                                   ? Colors.White
                                   : Colors.Black);
        AppBarTheme = appBarTheme ?? new AppBarThemeData();
        ShadowColor = shadowColor ?? Colors.Black;
        DisabledColor = disabledColor ?? (isDark ? Colors.White38 : Colors.Black38);
        UnselectedWidgetColor = unselectedWidgetColor
                                ?? (isDark ? Colors.White70 : Colors.Black54);
        HintColor = hintColor
                    ?? (isDark ? Colors.White60 : ApplyOpacity(Colors.Black, 0.60));
        FocusColor = focusColor ?? ApplyOpacity(
            effectiveBrightness == Brightness.Dark ? Colors.White : Colors.Black,
            0.12);
        HoverColor = hoverColor ?? ApplyOpacity(
            effectiveBrightness == Brightness.Dark ? Colors.White : Colors.Black,
            0.04);
        HighlightColor = highlightColor ?? (effectiveBrightness == Brightness.Dark
            ? Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)
            : Color.FromArgb(0x66, 0xBC, 0xBC, 0xBC));
        SplashColor = splashColor ?? (effectiveBrightness == Brightness.Dark
            ? Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)
            : Color.FromArgb(0x66, 0xC8, 0xC8, 0xC8));
        SplashFactory = splashFactory ?? ResolveDefaultSplashFactory(UseMaterial3, Platform);
        MaterialTapTargetSize = materialTapTargetSize ?? Platform switch
        {
            TargetPlatform.Android or TargetPlatform.Fuchsia or TargetPlatform.IOS =>
                MaterialTapTargetSize.Padded,
            _ => MaterialTapTargetSize.ShrinkWrap,
        };
        TextButtonTheme = textButtonTheme ?? new TextButtonThemeData();
        ElevatedButtonTheme = elevatedButtonTheme ?? new ElevatedButtonThemeData();
        OutlinedButtonTheme = outlinedButtonTheme ?? new OutlinedButtonThemeData();
        FilledButtonTheme = filledButtonTheme ?? new FilledButtonThemeData();
        IconButtonTheme = iconButtonTheme ?? new IconButtonThemeData();
        CardTheme = cardTheme ?? new CardThemeData();
        ListTileTheme = listTileTheme ?? new ListTileThemeData();
        DrawerTheme = drawerTheme ?? new DrawerThemeData();
        FloatingActionButtonTheme = floatingActionButtonTheme ?? new FloatingActionButtonThemeData();
        BottomNavigationBarTheme = bottomNavigationBarTheme ?? new BottomNavigationBarThemeData();
        DividerTheme = dividerTheme ?? new DividerThemeData();
        ProgressIndicatorTheme = progressIndicatorTheme ?? new ProgressIndicatorThemeData();
        CheckboxTheme = checkboxTheme ?? new CheckboxThemeData();
        SwitchTheme = switchTheme ?? new SwitchThemeData();
        RadioTheme = radioTheme ?? new RadioThemeData();
        SliderTheme = sliderTheme ?? new SliderThemeData();
        ExpansionTileTheme = expansionTileTheme ?? new ExpansionTileThemeData();
        BadgeTheme = badgeTheme ?? new BadgeThemeData();
        TooltipTheme = tooltipTheme ?? new TooltipThemeData();
        NavigationBarTheme = navigationBarTheme ?? new NavigationBarThemeData();
        NavigationRailTheme = navigationRailTheme ?? new NavigationRailThemeData();
        NavigationDrawerTheme = navigationDrawerTheme ?? new NavigationDrawerThemeData();
        ToggleButtonsTheme = toggleButtonsTheme ?? new ToggleButtonsThemeData();
        SegmentedButtonTheme = segmentedButtonTheme ?? new SegmentedButtonThemeData();
        ChipTheme = chipTheme ?? new ChipThemeData();
        ActionIconTheme = actionIconTheme;
        BannerTheme = bannerTheme ?? new MaterialBannerThemeData();
        SnackBarTheme = snackBarTheme ?? new SnackBarThemeData();
        DialogTheme = dialogTheme ?? new DialogThemeData();
        PopupMenuTheme = popupMenuTheme ?? new PopupMenuThemeData();
        ButtonTheme = buttonTheme ?? new ButtonThemeData(
            ButtonColor: isDark ? swatch.Shade600 : Colors.Grey.Shade300,
            DisabledColor: disabledColor,
            FocusColor: focusColor,
            HoverColor: hoverColor,
            HighlightColor: highlightColor,
            SplashColor: splashColor,
            MaterialTapTargetSize: MaterialTapTargetSize);
        ButtonBarTheme = buttonBarTheme ?? new ButtonBarThemeData();
        BottomAppBarTheme = bottomAppBarTheme ?? new BottomAppBarThemeData();
        DataTableTheme = dataTableTheme ?? new DataTableThemeData();
        ScrollbarTheme = scrollbarTheme ?? new ScrollbarThemeData();
        TabBarTheme = tabBarTheme ?? new TabBarThemeData();
        BottomSheetTheme = bottomSheetTheme ?? new BottomSheetThemeData();
        InputDecorationTheme = inputDecorationTheme ?? new InputDecorationThemeData();
        DatePickerTheme = datePickerTheme ?? new DatePickerThemeData();
        TimePickerTheme = timePickerTheme ?? new TimePickerThemeData();
        DropdownMenuTheme = dropdownMenuTheme ?? new DropdownMenuThemeData();
        SearchBarTheme = searchBarTheme ?? new SearchBarThemeData();
        SearchViewTheme = searchViewTheme ?? new SearchViewThemeData();
        CarouselViewTheme = carouselViewTheme ?? new CarouselViewThemeData();
        MenuBarTheme = menuBarTheme ?? new MenuBarThemeData();
        MenuButtonTheme = menuButtonTheme ?? new MenuButtonThemeData();
        MenuTheme = menuTheme ?? new MenuThemeData();
        TextSelectionTheme = textSelectionTheme ?? new TextSelectionThemeData();
        PageTransitionsTheme = pageTransitionsTheme ?? new PageTransitionsTheme();
        CupertinoOverrideTheme = cupertinoOverrideTheme?.NoDefault();
        Extensions = CreateExtensionMap(extensions);
        Adaptations = CreateAdaptationMap(adaptations);
        VisualDensity = visualDensity ?? VisualDensity.DefaultDensityForPlatform(Platform);
    }

    public TargetPlatform Platform { get; init; }

    /// <summary>
    /// The overall theme brightness. Dart derives this from <see cref="ColorScheme"/> rather than
    /// storing it, so it is not part of equality or `CopyWith` except through the scheme.
    /// </summary>
    public Brightness Brightness => ColorScheme.Brightness;

    public ColorScheme ColorScheme { get; init; }

    public Typography Typography { get; init; }

    public bool ApplyElevationOverlayColor { get; init; }

    /// <summary>
    /// Components of the <see cref="CupertinoThemeData"/> to override from the Material theme
    /// adaptation. Null (the default) lets every Cupertino attribute cascade from this theme.
    /// </summary>
    public NoDefaultCupertinoThemeData? CupertinoOverrideTheme { get; init; }

    public TextTheme TextTheme { get; init; }

    public Color ScaffoldBackgroundColor { get; init; }

    /// <summary>
    /// The color of the header of a data table, and of `PaginatedDataTable`'s selected-row
    /// overlay. Dart's `secondaryHeaderColor`.
    /// </summary>
    public Color SecondaryHeaderColor { get; init; }

    public Color CanvasColor { get; init; }

    public Color PrimaryColor { get; init; }

    public Color PrimaryColorLight { get; init; }

    public Color PrimaryColorDark { get; init; }

    public TextTheme PrimaryTextTheme { get; init; }

    public IconThemeData IconTheme { get; init; }

    public IconThemeData PrimaryIconTheme { get; init; }


    public bool UseMaterial3 { get; init; }

    public AppBarThemeData AppBarTheme { get; init; }

    public Color ShadowColor { get; init; }


    public Color DividerColor { get; init; }

    /// <summary>The background color of `Dialog` elements.</summary>
    [Obsolete("Use DialogThemeData.BackgroundColor instead. Deprecated in Flutter after v3.27.0-0.1.pre.")]
    public Color DialogBackgroundColor { get; init; }

    /// <summary>The color of the selected tab indicator in a tab bar.</summary>
    [Obsolete("Use TabBarThemeData.IndicatorColor instead. Deprecated in Flutter after v3.28.0-1.0.pre.")]
    public Color IndicatorColor { get; init; }

    public Color CardColor { get; init; }


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

    public DataTableThemeData DataTableTheme { get; init; }


    public TextButtonThemeData TextButtonTheme { get; init; }

    public ElevatedButtonThemeData ElevatedButtonTheme { get; init; }

    public OutlinedButtonThemeData OutlinedButtonTheme { get; init; }

    public FilledButtonThemeData FilledButtonTheme { get; init; }

    public IconButtonThemeData IconButtonTheme { get; init; }

    public CardThemeData CardTheme { get; init; }

    public ListTileThemeData ListTileTheme { get; init; }

    public DrawerThemeData DrawerTheme { get; init; }

    public FloatingActionButtonThemeData FloatingActionButtonTheme { get; init; }

    public BottomNavigationBarThemeData BottomNavigationBarTheme { get; init; }

    public DividerThemeData DividerTheme { get; init; }

    public ProgressIndicatorThemeData ProgressIndicatorTheme { get; init; }

    public CheckboxThemeData CheckboxTheme { get; init; }

    public SwitchThemeData SwitchTheme { get; init; }

    public RadioThemeData RadioTheme { get; init; }

    public SliderThemeData SliderTheme { get; init; }

    public ExpansionTileThemeData ExpansionTileTheme { get; init; }

    public BadgeThemeData BadgeTheme { get; init; }

    public TooltipThemeData TooltipTheme { get; init; }

    public NavigationBarThemeData NavigationBarTheme { get; init; }

    public NavigationRailThemeData NavigationRailTheme { get; init; }

    public NavigationDrawerThemeData NavigationDrawerTheme { get; init; }

    public ToggleButtonsThemeData ToggleButtonsTheme { get; init; }

    public SegmentedButtonThemeData SegmentedButtonTheme { get; init; }

    public ChipThemeData ChipTheme { get; init; }

    public ActionIconThemeData? ActionIconTheme { get; init; }

    public MaterialBannerThemeData BannerTheme { get; init; }

    public SnackBarThemeData SnackBarTheme { get; init; }

    public DialogThemeData DialogTheme { get; init; }

    public PopupMenuThemeData PopupMenuTheme { get; init; }

    public ButtonThemeData ButtonTheme { get; init; }

    public ScrollbarThemeData ScrollbarTheme { get; init; }

    public TabBarThemeData TabBarTheme { get; init; }

    public BottomSheetThemeData BottomSheetTheme { get; init; }

    public InputDecorationThemeData InputDecorationTheme { get; init; }

    public DatePickerThemeData DatePickerTheme { get; init; }

    public TimePickerThemeData TimePickerTheme { get; init; }

    public DropdownMenuThemeData DropdownMenuTheme { get; init; }

    public SearchBarThemeData SearchBarTheme { get; init; }

    public SearchViewThemeData SearchViewTheme { get; init; }

    public CarouselViewThemeData CarouselViewTheme { get; init; }

    public MenuBarThemeData MenuBarTheme { get; init; }

    public MenuButtonThemeData MenuButtonTheme { get; init; }

    public MenuThemeData MenuTheme { get; init; }

    public TextSelectionThemeData TextSelectionTheme { get; init; }

    public PageTransitionsTheme PageTransitionsTheme { get; init; }

    public ButtonBarThemeData ButtonBarTheme { get; init; }

    public BottomAppBarThemeData BottomAppBarTheme { get; init; }

    public IReadOnlyDictionary<Type, ThemeExtension> Extensions { get; init; }

    public T? Extension<T>() where T : ThemeExtension<T>
    {
        return Extensions.TryGetValue(typeof(T), out ThemeExtension? extension)
            ? (T)extension
            : null;
    }

    public IReadOnlyDictionary<Type, Adaptation> Adaptations { get; init; }

    /// <summary>Dart's `Diagnosticable.toStringShort`.</summary>
    public string ToStringShort() => Diagnostics.DescribeIdentity(this);

    /// <summary>Returns this theme as a diagnostics node.</summary>
    public DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
    {
        return new DiagnosticableNode<IDiagnosticable>(name, this, style);
    }

    /// <summary>Dart's compact `Diagnosticable.toString` output.</summary>
    public override string ToString() => ToString(DiagnosticLevel.Info);

    /// <summary>Returns diagnostics at or above <paramref name="minLevel"/>.</summary>
    public string ToString(DiagnosticLevel minLevel)
    {
        return ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, minLevel);
    }

    /// <summary>Adds every ThemeData property in Dart declaration order.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var defaultData = new ThemeData();
        object cupertinoOverrideDefault = defaultData.CupertinoOverrideTheme
            ?? DiagnosticsDefaults.NullValue;

        properties.Add(new IterableProperty<Adaptation>(
            "adaptations",
            Adaptations.Values,
            defaultValue: defaultData.Adaptations.Values,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<bool>(
            "applyElevationOverlayColor",
            ApplyElevationOverlayColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<NoDefaultCupertinoThemeData>(
            "cupertinoOverrideTheme",
            CupertinoOverrideTheme,
            defaultValue: cupertinoOverrideDefault,
            level: DiagnosticLevel.Debug));
        properties.Add(new IterableProperty<ThemeExtension>(
            "extensions",
            Extensions.Values,
            defaultValue: defaultData.Extensions.Values,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<InputDecorationThemeData>(
            "inputDecorationTheme",
            InputDecorationTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<MaterialTapTargetSize>(
            "materialTapTargetSize",
            MaterialTapTargetSize,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<PageTransitionsTheme>(
            "pageTransitionsTheme",
            PageTransitionsTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new EnumProperty<TargetPlatform>(
            "platform",
            Platform,
            defaultValue: PlatformDefaults.TargetPlatform,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ScrollbarThemeData>(
            "scrollbarTheme",
            ScrollbarTheme,
            defaultValue: defaultData.ScrollbarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<InteractiveInkFeatureFactory>(
            "splashFactory",
            SplashFactory,
            defaultValue: defaultData.SplashFactory,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<bool>(
            "useMaterial3",
            UseMaterial3,
            defaultValue: defaultData.UseMaterial3,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<VisualDensity>(
            "visualDensity",
            VisualDensity,
            defaultValue: defaultData.VisualDensity,
            level: DiagnosticLevel.Debug));

        AddColorProperties(properties, defaultData);
        AddTypographyProperties(properties, defaultData);
        AddComponentThemeProperties(properties, defaultData);
    }

    private void AddColorProperties(DiagnosticPropertiesBuilder properties, ThemeData defaultData)
    {
        properties.Add(new ColorProperty(
            "canvasColor",
            CanvasColor,
            defaultValue: defaultData.CanvasColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "cardColor",
            CardColor,
            defaultValue: defaultData.CardColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ColorScheme>(
            "colorScheme",
            ColorScheme,
            defaultValue: defaultData.ColorScheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "disabledColor",
            DisabledColor,
            defaultValue: defaultData.DisabledColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "dividerColor",
            DividerColor,
            defaultValue: defaultData.DividerColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "focusColor",
            FocusColor,
            defaultValue: defaultData.FocusColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "highlightColor",
            HighlightColor,
            defaultValue: defaultData.HighlightColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "hintColor",
            HintColor,
            defaultValue: defaultData.HintColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "hoverColor",
            HoverColor,
            defaultValue: defaultData.HoverColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "primaryColorDark",
            PrimaryColorDark,
            defaultValue: defaultData.PrimaryColorDark,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "primaryColorLight",
            PrimaryColorLight,
            defaultValue: defaultData.PrimaryColorLight,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "primaryColor",
            PrimaryColor,
            defaultValue: defaultData.PrimaryColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "scaffoldBackgroundColor",
            ScaffoldBackgroundColor,
            defaultValue: defaultData.ScaffoldBackgroundColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "secondaryHeaderColor",
            SecondaryHeaderColor,
            defaultValue: defaultData.SecondaryHeaderColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "shadowColor",
            ShadowColor,
            defaultValue: defaultData.ShadowColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "splashColor",
            SplashColor,
            defaultValue: defaultData.SplashColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "unselectedWidgetColor",
            UnselectedWidgetColor,
            defaultValue: defaultData.UnselectedWidgetColor,
            level: DiagnosticLevel.Debug));
    }

    private void AddTypographyProperties(DiagnosticPropertiesBuilder properties, ThemeData defaultData)
    {
        properties.Add(new DiagnosticsProperty<IconThemeData>(
            "iconTheme",
            IconTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<IconThemeData>(
            "primaryIconTheme",
            PrimaryIconTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<TextTheme>(
            "primaryTextTheme",
            PrimaryTextTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<TextTheme>(
            "textTheme",
            TextTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<Typography>(
            "typography",
            Typography,
            defaultValue: defaultData.Typography,
            level: DiagnosticLevel.Debug));
    }

    private void AddComponentThemeProperties(
        DiagnosticPropertiesBuilder properties,
        ThemeData defaultData)
    {
        properties.Add(new DiagnosticsProperty<ActionIconThemeData>(
            "actionIconTheme",
            ActionIconTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<AppBarThemeData>(
            "appBarTheme",
            AppBarTheme,
            defaultValue: defaultData.AppBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<BadgeThemeData>(
            "badgeTheme",
            BadgeTheme,
            defaultValue: defaultData.BadgeTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<MaterialBannerThemeData>(
            "bannerTheme",
            BannerTheme,
            defaultValue: defaultData.BannerTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<BottomAppBarThemeData>(
            "bottomAppBarTheme",
            BottomAppBarTheme,
            defaultValue: defaultData.BottomAppBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<BottomNavigationBarThemeData>(
            "bottomNavigationBarTheme",
            BottomNavigationBarTheme,
            defaultValue: defaultData.BottomNavigationBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<BottomSheetThemeData>(
            "bottomSheetTheme",
            BottomSheetTheme,
            defaultValue: defaultData.BottomSheetTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ButtonThemeData>(
            "buttonTheme",
            ButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<CardThemeData>(
            "cardTheme",
            CardTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<CarouselViewThemeData>(
            "carouselViewTheme",
            CarouselViewTheme,
            defaultValue: defaultData.CarouselViewTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<CheckboxThemeData>(
            "checkboxTheme",
            CheckboxTheme,
            defaultValue: defaultData.CheckboxTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ChipThemeData>(
            "chipTheme",
            ChipTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<DataTableThemeData>(
            "dataTableTheme",
            DataTableTheme,
            defaultValue: defaultData.DataTableTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<DatePickerThemeData>(
            "datePickerTheme",
            DatePickerTheme,
            defaultValue: defaultData.DatePickerTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<DialogThemeData>(
            "dialogTheme",
            DialogTheme,
            defaultValue: defaultData.DialogTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<DividerThemeData>(
            "dividerTheme",
            DividerTheme,
            defaultValue: defaultData.DividerTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<DrawerThemeData>(
            "drawerTheme",
            DrawerTheme,
            defaultValue: defaultData.DrawerTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<DropdownMenuThemeData>(
            "dropdownMenuTheme",
            DropdownMenuTheme,
            defaultValue: defaultData.DropdownMenuTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ElevatedButtonThemeData>(
            "elevatedButtonTheme",
            ElevatedButtonTheme,
            defaultValue: defaultData.ElevatedButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ExpansionTileThemeData>(
            "expansionTileTheme",
            ExpansionTileTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<FilledButtonThemeData>(
            "filledButtonTheme",
            FilledButtonTheme,
            defaultValue: defaultData.FilledButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<FloatingActionButtonThemeData>(
            "floatingActionButtonTheme",
            FloatingActionButtonTheme,
            defaultValue: defaultData.FloatingActionButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<IconButtonThemeData>(
            "iconButtonTheme",
            IconButtonTheme,
            defaultValue: defaultData.IconButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ListTileThemeData>(
            "listTileTheme",
            ListTileTheme,
            defaultValue: defaultData.ListTileTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<MenuBarThemeData>(
            "menuBarTheme",
            MenuBarTheme,
            defaultValue: defaultData.MenuBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<MenuButtonThemeData>(
            "menuButtonTheme",
            MenuButtonTheme,
            defaultValue: defaultData.MenuButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<MenuThemeData>(
            "menuTheme",
            MenuTheme,
            defaultValue: defaultData.MenuTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<NavigationBarThemeData>(
            "navigationBarTheme",
            NavigationBarTheme,
            defaultValue: defaultData.NavigationBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<NavigationDrawerThemeData>(
            "navigationDrawerTheme",
            NavigationDrawerTheme,
            defaultValue: defaultData.NavigationDrawerTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<NavigationRailThemeData>(
            "navigationRailTheme",
            NavigationRailTheme,
            defaultValue: defaultData.NavigationRailTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<OutlinedButtonThemeData>(
            "outlinedButtonTheme",
            OutlinedButtonTheme,
            defaultValue: defaultData.OutlinedButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<PopupMenuThemeData>(
            "popupMenuTheme",
            PopupMenuTheme,
            defaultValue: defaultData.PopupMenuTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ProgressIndicatorThemeData>(
            "progressIndicatorTheme",
            ProgressIndicatorTheme,
            defaultValue: defaultData.ProgressIndicatorTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<RadioThemeData>(
            "radioTheme",
            RadioTheme,
            defaultValue: defaultData.RadioTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<SearchBarThemeData>(
            "searchBarTheme",
            SearchBarTheme,
            defaultValue: defaultData.SearchBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<SearchViewThemeData>(
            "searchViewTheme",
            SearchViewTheme,
            defaultValue: defaultData.SearchViewTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<SegmentedButtonThemeData>(
            "segmentedButtonTheme",
            SegmentedButtonTheme,
            defaultValue: defaultData.SegmentedButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<SliderThemeData>(
            "sliderTheme",
            SliderTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<SnackBarThemeData>(
            "snackBarTheme",
            SnackBarTheme,
            defaultValue: defaultData.SnackBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<SwitchThemeData>(
            "switchTheme",
            SwitchTheme,
            defaultValue: defaultData.SwitchTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<TabBarThemeData>(
            "tabBarTheme",
            TabBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<TextButtonThemeData>(
            "textButtonTheme",
            TextButtonTheme,
            defaultValue: defaultData.TextButtonTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<TextSelectionThemeData>(
            "textSelectionTheme",
            TextSelectionTheme,
            defaultValue: defaultData.TextSelectionTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<TimePickerThemeData>(
            "timePickerTheme",
            TimePickerTheme,
            defaultValue: defaultData.TimePickerTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ToggleButtonsThemeData>(
            "toggleButtonsTheme",
            ToggleButtonsTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<TooltipThemeData>(
            "tooltipTheme",
            TooltipTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<ButtonBarThemeData>(
            "buttonBarTheme",
            ButtonBarTheme,
            defaultValue: defaultData.ButtonBarTheme,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "dialogBackgroundColor",
            DialogBackgroundColor,
            defaultValue: defaultData.DialogBackgroundColor,
            level: DiagnosticLevel.Debug));
        properties.Add(new ColorProperty(
            "indicatorColor",
            IndicatorColor,
            defaultValue: defaultData.IndicatorColor,
            level: DiagnosticLevel.Debug));
    }

    /// Dart's `ThemeData.getAdaptation<T>()`.
    public Adaptation<T>? GetAdaptation<T>()
    {
        return Adaptations.TryGetValue(typeof(T), out Adaptation? adaptation)
            ? (Adaptation<T>)adaptation
            : null;
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
        color.R,
        color.G,
        color.B);

    /// <summary>
    /// Dart's `ThemeData.copyWith` for the parameters whose semantics a C# `with` expression
    /// cannot express: <paramref name="brightness"/> is applied to <see cref="ColorScheme"/>
    /// rather than stored, the extension/adaptation iterables are re-keyed into their maps, and
    /// <paramref name="cupertinoOverrideTheme"/> is stripped of its defaults. Every plain field
    /// is replaced with a `with` expression, as elsewhere in this library.
    /// </summary>
    public ThemeData CopyWith(
        Brightness? brightness = null,
        ColorScheme? colorScheme = null,
        NoDefaultCupertinoThemeData? cupertinoOverrideTheme = null,
        IEnumerable<ThemeExtension>? extensions = null,
        IEnumerable<Adaptation>? adaptations = null)
    {
        ColorScheme resolvedScheme = colorScheme ?? ColorScheme;
        if (brightness is Brightness value)
        {
            resolvedScheme = resolvedScheme.CopyWith(brightness: value);
        }

        return this with
        {
            ColorScheme = resolvedScheme,
            CupertinoOverrideTheme = cupertinoOverrideTheme is null
                ? CupertinoOverrideTheme
                : cupertinoOverrideTheme.NoDefault(),
            Extensions = extensions is null ? Extensions : CreateExtensionMap(extensions),
            Adaptations = adaptations is null ? Adaptations : CreateAdaptationMap(adaptations),
        };
    }

    public static ThemeData Light { get; } = new();

    public static ThemeData Dark { get; } = new(brightness: Brightness.Dark);

    /// <summary>
    /// A default theme without text geometry, expected to be localized through
    /// <see cref="Localize"/>. Dart's `ThemeData.fallback`, which is `ThemeData.light`.
    /// </summary>
    public static ThemeData Fallback { get; } = new();

    /// <summary>
    /// Creates a theme from <paramref name="colorScheme"/>. Dart's `ThemeData.from`: the scheme
    /// drives brightness and the surface-backed colors, and dark schemes turn on
    /// <see cref="ApplyElevationOverlayColor"/>.
    /// </summary>
    public static ThemeData From(
        ColorScheme colorScheme,
        TextTheme? textTheme = null,
        bool? useMaterial3 = null)
    {
        ArgumentNullException.ThrowIfNull(colorScheme);
        bool isDark = colorScheme.Brightness == Brightness.Dark;
        Color primarySurfaceColor = isDark ? colorScheme.Surface : colorScheme.Primary;
        Color onPrimarySurfaceColor = isDark ? colorScheme.OnSurface : colorScheme.OnPrimary;
        return new ThemeData(
            colorScheme: colorScheme,
            brightness: colorScheme.Brightness,
            primaryColor: primarySurfaceColor,
            canvasColor: colorScheme.Surface,
            scaffoldBackgroundColor: colorScheme.Surface,
            cardColor: colorScheme.Surface,
            dividerColor: ApplyOpacity(colorScheme.OnSurface, 0.12),
            dialogBackgroundColor: colorScheme.Surface,
            indicatorColor: onPrimarySurfaceColor,
            textTheme: textTheme,
            applyElevationOverlayColor: isDark,
            useMaterial3: useMaterial3);
    }

    public static ThemeData Localize(ThemeData baseTheme, TextTheme localTextGeometry)
    {
        ArgumentNullException.ThrowIfNull(baseTheme);
        ArgumentNullException.ThrowIfNull(localTextGeometry);
        return LocalizedThemeCache.PutIfAbsent(
            new IdentityThemeDataCacheKey(baseTheme, localTextGeometry),
            () => baseTheme with
            {
                PrimaryTextTheme = localTextGeometry.Merge(baseTheme.PrimaryTextTheme),
                TextTheme = localTextGeometry.Merge(baseTheme.TextTheme),
            });
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
            ShadowColor = LerpColor(a.ShadowColor, b.ShadowColor, t),
            DividerColor = LerpColor(a.DividerColor, b.DividerColor, t),
            SecondaryHeaderColor = LerpColor(a.SecondaryHeaderColor, b.SecondaryHeaderColor, t),
            DialogBackgroundColor = LerpColor(a.DialogBackgroundColor, b.DialogBackgroundColor, t),
            IndicatorColor = LerpColor(a.IndicatorColor, b.IndicatorColor, t),
            CardColor = LerpColor(a.CardColor, b.CardColor, t),
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
                t),
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
            Adaptations = t < 0.5 ? a.Adaptations : b.Adaptations,
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

    private static IReadOnlyDictionary<Type, Adaptation> CreateAdaptationMap(
        IEnumerable<Adaptation>? adaptations)
    {
        if (adaptations is null)
        {
            return EmptyAdaptations;
        }

        var result = new Dictionary<Type, Adaptation>();
        foreach (Adaptation adaptation in adaptations)
        {
            ArgumentNullException.ThrowIfNull(adaptation);
            if (!result.TryAdd(adaptation.Type, adaptation))
            {
                throw new ArgumentException(
                    $"Only one Adaptation with type {adaptation.Type.Name} may be provided.",
                    nameof(adaptations));
            }
        }

        return result.Count == 0 ? EmptyAdaptations : result;
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

    /// <summary>
    /// Dart's `_IdentityThemeDataCacheKey`: keys the localized-theme cache on the *identity* of
    /// its two inputs, so a rebuilt-but-equal theme is a cache miss rather than a stale hit.
    /// </summary>
    private sealed class IdentityThemeDataCacheKey(ThemeData baseTheme, TextTheme localTextGeometry)
    {
        private readonly ThemeData _baseTheme = baseTheme;
        private readonly TextTheme _localTextGeometry = localTextGeometry;

        public override int GetHashCode() =>
            RuntimeHelpers.GetHashCode(_baseTheme) ^ RuntimeHelpers.GetHashCode(_localTextGeometry);

        public override bool Equals(object? obj) =>
            obj is IdentityThemeDataCacheKey other
            && ReferenceEquals(other._baseTheme, _baseTheme)
            && ReferenceEquals(other._localTextGeometry, _localTextGeometry);
    }

    /// <summary>Dart's `_FifoCache`: bounded, evicting the least recently *inserted* entry.</summary>
    private sealed class FifoCache<TKey, TValue>(int maximumSize)
        where TKey : notnull
        where TValue : class
    {
        private readonly Dictionary<TKey, TValue> _cache = [];
        private readonly Queue<TKey> _order = new();
        private readonly Lock _gate = new();

        public TValue PutIfAbsent(TKey key, Func<TValue> loader)
        {
            lock (_gate)
            {
                if (_cache.TryGetValue(key, out TValue? existing))
                {
                    return existing;
                }

                if (_cache.Count == maximumSize)
                {
                    _cache.Remove(_order.Dequeue());
                }

                TValue created = loader();
                _cache[key] = created;
                _order.Enqueue(key);
                return created;
            }
        }
    }
}

/// <summary>
/// A <see cref="CupertinoThemeData"/> that defers unspecified theme attributes to an upstream
/// Material <see cref="ThemeData"/>. Used by the Material <see cref="Theme"/> to harmonize the
/// <see cref="CupertinoTheme"/> with the Material theme's colors and text styles.
/// </summary>
// This class subclasses CupertinoThemeData rather than composes one because it _is_ a
// CupertinoThemeData with partially altered behavior. e.g. its textTheme is from the superclass and
// based on the primaryColor but the primaryColor comes from the Material theme unless overridden.
public class MaterialBasedCupertinoThemeData : CupertinoThemeData
{
    private readonly ThemeData _materialTheme;
    private readonly NoDefaultCupertinoThemeData _cupertinoOverrideTheme;

    /// <summary>
    /// Creates a <see cref="MaterialBasedCupertinoThemeData"/> based on a Material
    /// <see cref="ThemeData"/> and its <see cref="ThemeData.CupertinoOverrideTheme"/>.
    /// </summary>
    public MaterialBasedCupertinoThemeData(ThemeData materialTheme)
        : this(
            materialTheme ?? throw new ArgumentNullException(nameof(materialTheme)),
            (materialTheme.CupertinoOverrideTheme ?? new CupertinoThemeData()).NoDefault())
    {
    }

    // Pass all values to the superclass so Material-agnostic properties like barBackgroundColor can
    // still behave like a normal CupertinoThemeData.
    private MaterialBasedCupertinoThemeData(
        ThemeData materialTheme,
        NoDefaultCupertinoThemeData cupertinoOverrideTheme)
        : base(
            cupertinoOverrideTheme.Brightness,
            cupertinoOverrideTheme.PrimaryColor,
            cupertinoOverrideTheme.PrimaryContrastingColor,
            cupertinoOverrideTheme.TextTheme,
            cupertinoOverrideTheme.BarBackgroundColor,
            cupertinoOverrideTheme.ScaffoldBackgroundColor,
            cupertinoOverrideTheme.SelectionHandleColor
            ?? AsDynamic(materialTheme.TextSelectionTheme.SelectionHandleColor),
            cupertinoOverrideTheme.ApplyThemeToAll)
    {
        _materialTheme = materialTheme;
        _cupertinoOverrideTheme = cupertinoOverrideTheme;
    }

    public override PlatformBrightness? Brightness =>
        _cupertinoOverrideTheme.Brightness ?? ToPlatformBrightness(_materialTheme.Brightness);

    public override CupertinoDynamicColor PrimaryColor =>
        _cupertinoOverrideTheme.PrimaryColor ?? _materialTheme.ColorScheme.Primary;

    public override CupertinoDynamicColor PrimaryContrastingColor =>
        _cupertinoOverrideTheme.PrimaryContrastingColor ?? _materialTheme.ColorScheme.OnPrimary;

    public override CupertinoDynamicColor ScaffoldBackgroundColor =>
        _cupertinoOverrideTheme.ScaffoldBackgroundColor ?? _materialTheme.ScaffoldBackgroundColor;

    /// <summary>
    /// Copies the <see cref="ThemeData.CupertinoOverrideTheme"/>. Only its specified override
    /// attributes and the newly specified parameters are in the returned theme; no attribute derived
    /// from iOS defaults or cascaded from the Material theme is copied. This cannot change the base
    /// Material theme — create a new Material <see cref="Theme"/> for that.
    /// </summary>
    public override MaterialBasedCupertinoThemeData CopyWith(
        PlatformBrightness? brightness = null,
        CupertinoDynamicColor? primaryColor = null,
        CupertinoDynamicColor? primaryContrastingColor = null,
        CupertinoTextThemeData? textTheme = null,
        CupertinoDynamicColor? barBackgroundColor = null,
        CupertinoDynamicColor? scaffoldBackgroundColor = null,
        CupertinoDynamicColor? selectionHandleColor = null,
        bool? applyThemeToAll = null)
    {
        return new MaterialBasedCupertinoThemeData(
            _materialTheme,
            _cupertinoOverrideTheme.CopyWith(
                brightness: brightness,
                primaryColor: primaryColor,
                primaryContrastingColor: primaryContrastingColor,
                textTheme: textTheme,
                barBackgroundColor: barBackgroundColor,
                scaffoldBackgroundColor: scaffoldBackgroundColor,
                selectionHandleColor: selectionHandleColor,
                applyThemeToAll: applyThemeToAll));
    }

    public override CupertinoThemeData ResolveFrom(BuildContext context)
    {
        // Only the Cupertino override theme is resolved, as well as the default text theme. A color
        // that comes from the Material theme is not resolved.
        NoDefaultCupertinoThemeData cupertinoOverrideThemeWithTextTheme =
            _cupertinoOverrideTheme.CopyWith(textTheme: TextTheme);
        return new MaterialBasedCupertinoThemeData(
            _materialTheme,
            cupertinoOverrideThemeWithTextTheme.ResolveFrom(context));
    }

    private static CupertinoDynamicColor? AsDynamic(Color? color) =>
        color is { } value ? (CupertinoDynamicColor)value : null;

    // `Brightness` names this type's own property here, so the Material enum needs qualifying.
    private static PlatformBrightness ToPlatformBrightness(Plumix.Material.Brightness brightness) =>
        brightness == Plumix.Material.Brightness.Dark
            ? PlatformBrightness.Dark
            : PlatformBrightness.Light;
}

/// <summary>
/// Creates a Material theme whose color scheme is based off the colors from a
/// <see cref="CupertinoThemeData"/>. Intended only for the case where a Material widget cannot find
/// a Material theme in the tree but can find a Cupertino one — most often a Material widget used
/// inside a <c>CupertinoApp</c>. Besides the colors, every default comes from <see cref="ThemeData"/>,
/// so further customization is best done by adding a Material <see cref="Theme"/> above the app.
/// </summary>
public class CupertinoBasedMaterialThemeData
{
    /// <summary>
    /// Creates a Material theme with a color scheme based off of the colors from a
    /// <see cref="CupertinoThemeData"/>.
    /// </summary>
    public CupertinoBasedMaterialThemeData(CupertinoThemeData themeData)
    {
        ArgumentNullException.ThrowIfNull(themeData);
        MaterialTheme = new ThemeData(
            colorScheme: ColorScheme.FromSeed(
                seedColor: themeData.PrimaryColor,
                brightness: themeData.Brightness == PlatformBrightness.Dark
                    ? Brightness.Dark
                    : Brightness.Light,
                primary: themeData.PrimaryColor,
                onPrimary: themeData.PrimaryContrastingColor));
    }

    /// <summary>The Material theme data with colors based on an existing Cupertino theme.</summary>
    public ThemeData MaterialTheme { get; }
}
