using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/theme_data.dart; flutter/packages/flutter/lib/src/material/app_bar_theme.dart (approximate)

public enum TargetPlatform
{
    Android,
    Fuchsia,
    IOS,
    Linux,
    MacOS,
    Windows,
}

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
    SystemUiOverlayStyle? SystemOverlayStyle = null);

public sealed record MaterialTextTheme
{
    private static readonly FontFamily DefaultBodyFontFamily = ResolveDefaultBodyFontFamily();

    public MaterialTextTheme(
        TextStyle? bodyMedium = null,
        TextStyle? titleLarge = null,
        TextStyle? labelLarge = null,
        TextStyle? labelSmall = null,
        TextStyle? titleMedium = null,
        TextStyle? bodyLarge = null,
        TextStyle? labelMedium = null,
        TextStyle? bodySmall = null,
        TextStyle? headlineSmall = null)
    {
        BodyMedium = bodyMedium ?? DefaultBodyMedium;
        BodyLarge = bodyLarge ?? DefaultBodyLarge;
        BodySmall = bodySmall ?? DefaultBodySmall;
        TitleLarge = titleLarge ?? DefaultTitleLarge;
        LabelLarge = labelLarge ?? DefaultLabelLarge;
        LabelMedium = labelMedium ?? DefaultLabelMedium;
        LabelSmall = labelSmall ?? DefaultLabelSmall;
        TitleMedium = titleMedium ?? DefaultTitleMedium;
        HeadlineSmall = headlineSmall ?? DefaultHeadlineSmall;
    }

    public TextStyle BodyMedium { get; init; }

    public TextStyle BodyLarge { get; init; }

    public TextStyle BodySmall { get; init; }

    public TextStyle TitleLarge { get; init; }

    public TextStyle LabelLarge { get; init; }

    public TextStyle LabelMedium { get; init; }

    public TextStyle LabelSmall { get; init; }

    public TextStyle TitleMedium { get; init; }

    public TextStyle HeadlineSmall { get; init; }

    public static TextStyle DefaultBodyMedium { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 14,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.43,
        LetterSpacing: 0.25);

    public static TextStyle DefaultBodyLarge { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 16,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.5,
        LetterSpacing: 0.5);

    public static TextStyle DefaultBodySmall { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 12,
        Color: Color.Parse("#FF49454F"),
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.33,
        LetterSpacing: 0.4);

    public static TextStyle DefaultTitleLarge { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 22,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.27,
        LetterSpacing: 0.0);

    public static TextStyle DefaultLabelLarge { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 14,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Medium,
        FontStyle: FontStyle.Normal,
        Height: 1.43,
        LetterSpacing: 0.1);

    public static TextStyle DefaultLabelMedium { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 12,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Medium,
        FontStyle: FontStyle.Normal,
        Height: 1.33,
        LetterSpacing: 0.5);

    public static TextStyle DefaultLabelSmall { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 11,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Medium,
        FontStyle: FontStyle.Normal,
        Height: 1.45,
        LetterSpacing: 0.5);

    public static TextStyle DefaultTitleMedium { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 16,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.5,
        LetterSpacing: 0.15);

    public static TextStyle DefaultHeadlineSmall { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 24,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.33,
        LetterSpacing: 0.0);

    public static MaterialTextTheme Fallback { get; } = new();

    private static FontFamily ResolveDefaultBodyFontFamily()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsMacOS())
        {
            return new FontFamily(".AppleSystemUIFont");
        }

        if (OperatingSystem.IsAndroid())
        {
            // Plumix.Sample Material typography on Android resolves through Roboto.
            return new FontFamily("Roboto");
        }

        if (OperatingSystem.IsWindows())
        {
            return new FontFamily("Segoe UI");
        }

        if (OperatingSystem.IsLinux())
        {
            return new FontFamily("Noto Sans");
        }

        return Avalonia.Media.FontFamily.Default;
    }
}

public sealed record ThemeData
{
    private static readonly Color LightScaffoldAndCanvasColor = Color.Parse("#FFFEF7FF");
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
    private static readonly Color LightDividerColor = Color.FromArgb(0x1F, 0x00, 0x00, 0x00);
    private static readonly Color LightShadowColor = Colors.Black;
    private static readonly Color LightCardColor = Colors.White;
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

    public ThemeData(
        TargetPlatform? platform = null,
        Brightness? brightness = null,
        MaterialTextTheme? textTheme = null,
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
        MaterialTextTheme? primaryTextTheme = null,
        IconThemeData? iconTheme = null,
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
        Color? disabledColor = null,
        Color? hintColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null)
    {
        Platform = platform ?? ResolveDefaultPlatform();
        Brightness = brightness ?? Brightness.Light;
        TextTheme = textTheme ?? MaterialTextTheme.Fallback;
        ScaffoldBackgroundColor = scaffoldBackgroundColor ?? LightScaffoldAndCanvasColor;
        CanvasColor = canvasColor ?? LightScaffoldAndCanvasColor;
        PrimaryColor = primaryColor ?? LightPrimaryColor;
        PrimaryColorLight = primaryColorLight ?? DefaultPrimaryColorLight;
        PrimaryColorDark = primaryColorDark ?? DefaultPrimaryColorDark;
        PrimaryTextTheme = primaryTextTheme ?? new MaterialTextTheme(
            titleMedium: MaterialTextTheme.DefaultTitleMedium.CopyWith(color: Colors.White));
        IconTheme = iconTheme ?? new IconThemeData(Color: LightOnSurfaceColor, Size: 24);
        SecondaryColor = secondaryColor ?? LightSecondaryColor;
        OnPrimaryColor = onPrimaryColor ?? Colors.White;
        PrimaryContainerColor = primaryContainerColor ?? LightPrimaryContainerColor;
        OnPrimaryContainerColor = onPrimaryContainerColor ?? LightOnPrimaryContainerColor;
        UseMaterial3 = useMaterial3 ?? true;
        _appBarTheme = appBarTheme;
        ShadowColor = shadowColor ?? LightShadowColor;
        SurfaceColor = surfaceColor ?? LightSurfaceColor;
        OnSurfaceColor = onSurfaceColor ?? LightOnSurfaceColor;
        OnSurfaceVariantColor = onSurfaceVariantColor ?? LightOnSurfaceVariantColor;
        OutlineColor = outlineColor ?? LightOutlineColor;
        OutlineVariantColor = outlineVariantColor ?? LightOutlineVariantColor;
        DividerColor = dividerColor ?? LightDividerColor;
        CardColor = cardColor ?? LightCardColor;
        SurfaceContainerLowColor = surfaceContainerLowColor ?? LightSurfaceContainerLowColor;
        SurfaceContainerColor = surfaceContainerColor ?? LightSurfaceContainerColor;
        SurfaceContainerHighColor = surfaceContainerHighColor ?? LightSurfaceContainerHighColor;
        SurfaceContainerHighestColor = surfaceContainerHighestColor ?? LightSurfaceContainerHighestColor;
        SecondaryContainerColor = secondaryContainerColor ?? LightSecondaryContainerColor;
        OnSecondaryContainerColor = onSecondaryContainerColor ?? LightOnSecondaryContainerColor;
        InverseSurfaceColor = inverseSurfaceColor ?? LightInverseSurfaceColor;
        OnInverseSurfaceColor = onInverseSurfaceColor ?? LightOnInverseSurfaceColor;
        InversePrimaryColor = inversePrimaryColor ?? LightInversePrimaryColor;
        ErrorColor = errorColor ?? LightErrorColor;
        OnErrorColor = onErrorColor ?? LightOnErrorColor;
        DisabledColor = disabledColor ?? ApplyOpacity(OnSurfaceColor, 0.38);
        HintColor = hintColor ?? ApplyOpacity(OnSurfaceColor, 0.60);
        FocusColor = focusColor ?? ApplyOpacity(OnSurfaceColor, 0.12);
        HoverColor = hoverColor ?? ApplyOpacity(
            Brightness == Brightness.Dark ? Colors.White : Colors.Black,
            0.04);
        HighlightColor = highlightColor ?? (Brightness == Brightness.Dark
            ? Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)
            : Color.FromArgb(0x66, 0xBC, 0xBC, 0xBC));
        SplashColor = splashColor ?? (Brightness == Brightness.Dark
            ? Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)
            : Color.FromArgb(0x66, 0xC8, 0xC8, 0xC8));
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
        VisualDensity = visualDensity ?? VisualDensity.Standard;
    }

    public TargetPlatform Platform { get; init; }

    public Brightness Brightness { get; init; }

    public MaterialTextTheme TextTheme { get; init; }

    public Color ScaffoldBackgroundColor { get; init; }

    public Color CanvasColor { get; init; }

    public Color PrimaryColor { get; init; }

    public Color PrimaryColorLight { get; init; }

    public Color PrimaryColorDark { get; init; }

    public MaterialTextTheme PrimaryTextTheme { get; init; }

    public IconThemeData IconTheme { get; init; }

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

    public Color HintColor { get; init; }

    public Color FocusColor { get; init; }

    public Color HoverColor { get; init; }

    public Color HighlightColor { get; init; }

    public Color SplashColor { get; init; }

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

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
        color.R,
        color.G,
        color.B);

    public static ThemeData Light { get; } = new();

    public static ThemeData Dark { get; } = new(
        brightness: Brightness.Dark,
        textTheme: new MaterialTextTheme(
            bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(color: Colors.White),
            bodyLarge: MaterialTextTheme.DefaultBodyLarge.CopyWith(color: Colors.White),
            bodySmall: MaterialTextTheme.DefaultBodySmall.CopyWith(color: Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF)),
            titleLarge: MaterialTextTheme.DefaultTitleLarge.CopyWith(color: Colors.White),
            titleMedium: MaterialTextTheme.DefaultTitleMedium.CopyWith(color: Colors.White),
            headlineSmall: MaterialTextTheme.DefaultHeadlineSmall.CopyWith(color: Colors.White),
            labelLarge: MaterialTextTheme.DefaultLabelLarge.CopyWith(color: Colors.White),
            labelMedium: MaterialTextTheme.DefaultLabelMedium.CopyWith(color: Colors.White),
            labelSmall: MaterialTextTheme.DefaultLabelSmall.CopyWith(color: Colors.White)),
        scaffoldBackgroundColor: Color.Parse("#FF121212"),
        canvasColor: Color.Parse("#FF121212"),
        primaryColor: Color.Parse("#FFBB86FC"),
        secondaryColor: Color.Parse("#FF03DAC6"),
        onPrimaryColor: Colors.Black,
        surfaceColor: Color.Parse("#FF121212"),
        onSurfaceColor: Colors.White,
        onSurfaceVariantColor: Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF),
        inverseSurfaceColor: Color.Parse("#FFE6E1E5"),
        onInverseSurfaceColor: Color.Parse("#FF322F35"),
        inversePrimaryColor: Color.Parse("#FF6750A4"),
        surfaceContainerHighColor: Color.Parse("#FF211F26"),
        cardColor: Color.Parse("#FF1E1E1E"),
        iconTheme: new IconThemeData(Color: Colors.White, Size: 24));

    public static Brightness EstimateBrightnessForColor(Color color)
    {
        static double Linearize(byte component)
        {
            var value = component / 255.0;
            return value <= 0.03928
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        var luminance = (0.2126 * Linearize(color.R))
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
}
