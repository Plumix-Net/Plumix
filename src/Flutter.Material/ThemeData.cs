using Avalonia;
using Avalonia.Media;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

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
        TextStyle? labelLarge = null)
    {
        BodyMedium = bodyMedium ?? DefaultBodyMedium;
        TitleLarge = titleLarge ?? DefaultTitleLarge;
        LabelLarge = labelLarge ?? DefaultLabelLarge;
    }

    public TextStyle BodyMedium { get; init; }

    public TextStyle TitleLarge { get; init; }

    public TextStyle LabelLarge { get; init; }

    public static TextStyle DefaultBodyMedium { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 14,
        Color: Color.Parse("#FF1D1B20"),
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.43,
        LetterSpacing: 0.25);

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

    public static MaterialTextTheme Fallback { get; } = new();

    private static FontFamily ResolveDefaultBodyFontFamily()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsMacOS())
        {
            return new FontFamily(".AppleSystemUIFont");
        }

        if (OperatingSystem.IsAndroid())
        {
            // Flutter Material typography on Android resolves through Roboto.
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
    private static readonly Color LightPrimaryContainerColor = Color.Parse("#FFEADDFF");
    private static readonly Color LightOnPrimaryContainerColor = Color.Parse("#FF21005D");
    private static readonly Color LightOnSurfaceColor = Color.Parse("#FF1D1B20");
    private static readonly Color LightOnSurfaceVariantColor = Color.Parse("#FF49454F");
    private static readonly Color LightOutlineColor = Color.Parse("#FF79747E");
    private static readonly Color LightShadowColor = Colors.Black;
    private static readonly Color LightSurfaceContainerLowColor = Color.Parse("#FFF7F2FA");
    private static readonly Color LightSurfaceContainerHighestColor = Color.Parse("#FFE6E0E9");
    private static readonly Color LightSecondaryContainerColor = Color.Parse("#FFE8DEF8");
    private static readonly Color LightOnSecondaryContainerColor = Color.Parse("#FF4A4458");
    private static readonly Color LightInverseSurfaceColor = Color.Parse("#FF322F35");
    private static readonly Color LightOnInverseSurfaceColor = Color.Parse("#FFF5EFF7");
    private static readonly Color LightErrorColor = Color.Parse("#FFB3261E");
    private static readonly Color LightOnErrorColor = Colors.White;

    private AppBarThemeData? _appBarTheme;
    private TextButtonThemeData? _textButtonTheme;
    private ElevatedButtonThemeData? _elevatedButtonTheme;
    private OutlinedButtonThemeData? _outlinedButtonTheme;
    private FilledButtonThemeData? _filledButtonTheme;
    private IconButtonThemeData? _iconButtonTheme;
    private ListTileThemeData? _listTileTheme;
    private DrawerThemeData? _drawerTheme;
    private FloatingActionButtonThemeData? _floatingActionButtonTheme;
    private BottomNavigationBarThemeData? _bottomNavigationBarTheme;
    private CheckboxThemeData? _checkboxTheme;
    private SwitchThemeData? _switchTheme;
    private RadioThemeData? _radioTheme;

    public ThemeData(
        TargetPlatform? platform = null,
        Brightness? brightness = null,
        MaterialTextTheme? textTheme = null,
        Color? scaffoldBackgroundColor = null,
        Color? canvasColor = null,
        Color? primaryColor = null,
        Color? onPrimaryColor = null,
        Color? primaryContainerColor = null,
        Color? onPrimaryContainerColor = null,
        bool? useMaterial3 = null,
        AppBarThemeData? appBarTheme = null,
        Color? shadowColor = null,
        Color? onSurfaceColor = null,
        Color? onSurfaceVariantColor = null,
        Color? outlineColor = null,
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
        ListTileThemeData? listTileTheme = null,
        DrawerThemeData? drawerTheme = null,
        FloatingActionButtonThemeData? floatingActionButtonTheme = null,
        BottomNavigationBarThemeData? bottomNavigationBarTheme = null,
        CheckboxThemeData? checkboxTheme = null,
        SwitchThemeData? switchTheme = null,
        RadioThemeData? radioTheme = null)
    {
        Platform = platform ?? ResolveDefaultPlatform();
        Brightness = brightness ?? Brightness.Light;
        TextTheme = textTheme ?? MaterialTextTheme.Fallback;
        ScaffoldBackgroundColor = scaffoldBackgroundColor ?? LightScaffoldAndCanvasColor;
        CanvasColor = canvasColor ?? LightScaffoldAndCanvasColor;
        PrimaryColor = primaryColor ?? LightPrimaryColor;
        OnPrimaryColor = onPrimaryColor ?? Colors.White;
        PrimaryContainerColor = primaryContainerColor ?? LightPrimaryContainerColor;
        OnPrimaryContainerColor = onPrimaryContainerColor ?? LightOnPrimaryContainerColor;
        UseMaterial3 = useMaterial3 ?? true;
        _appBarTheme = appBarTheme;
        ShadowColor = shadowColor ?? LightShadowColor;
        OnSurfaceColor = onSurfaceColor ?? LightOnSurfaceColor;
        OnSurfaceVariantColor = onSurfaceVariantColor ?? LightOnSurfaceVariantColor;
        OutlineColor = outlineColor ?? LightOutlineColor;
        SurfaceContainerLowColor = surfaceContainerLowColor ?? LightSurfaceContainerLowColor;
        SurfaceContainerHighestColor = surfaceContainerHighestColor ?? LightSurfaceContainerHighestColor;
        SecondaryContainerColor = secondaryContainerColor ?? LightSecondaryContainerColor;
        OnSecondaryContainerColor = onSecondaryContainerColor ?? LightOnSecondaryContainerColor;
        InverseSurfaceColor = inverseSurfaceColor ?? LightInverseSurfaceColor;
        OnInverseSurfaceColor = onInverseSurfaceColor ?? LightOnInverseSurfaceColor;
        ErrorColor = errorColor ?? LightErrorColor;
        OnErrorColor = onErrorColor ?? LightOnErrorColor;
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
        _listTileTheme = listTileTheme;
        _drawerTheme = drawerTheme;
        _floatingActionButtonTheme = floatingActionButtonTheme;
        _bottomNavigationBarTheme = bottomNavigationBarTheme;
        _checkboxTheme = checkboxTheme;
        _switchTheme = switchTheme;
        _radioTheme = radioTheme;
    }

    public TargetPlatform Platform { get; init; }

    public Brightness Brightness { get; init; }

    public MaterialTextTheme TextTheme { get; init; }

    public Color ScaffoldBackgroundColor { get; init; }

    public Color CanvasColor { get; init; }

    public Color PrimaryColor { get; init; }

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

    public Color OnSurfaceColor { get; init; }

    public Color OnSurfaceVariantColor { get; init; }

    public Color OutlineColor { get; init; }

    public Color SurfaceContainerLowColor { get; init; }

    public Color SurfaceContainerHighestColor { get; init; }

    public Color SecondaryContainerColor { get; init; }

    public Color OnSecondaryContainerColor { get; init; }

    public Color InverseSurfaceColor { get; init; }

    public Color OnInverseSurfaceColor { get; init; }

    public Color ErrorColor { get; init; }

    public Color OnErrorColor { get; init; }

    public MaterialTapTargetSize MaterialTapTargetSize { get; init; }

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

    public static ThemeData Light { get; } = new();

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
