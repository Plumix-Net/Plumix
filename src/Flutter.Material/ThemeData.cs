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
        TextStyle? titleLarge = null)
    {
        BodyMedium = bodyMedium ?? DefaultBodyMedium;
        TitleLarge = titleLarge ?? DefaultTitleLarge;
    }

    public TextStyle BodyMedium { get; init; }

    public TextStyle TitleLarge { get; init; }

    public static TextStyle DefaultBodyMedium { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 14,
        Color: Colors.Black,
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        Height: 1.43,
        LetterSpacing: 0.25);

    public static TextStyle DefaultTitleLarge { get; } = new(
        FontFamily: DefaultBodyFontFamily,
        FontSize: 20,
        Color: Colors.Black,
        FontWeight: FontWeight.SemiBold,
        FontStyle: FontStyle.Normal,
        Height: 1.20,
        LetterSpacing: 0.0);

    public static MaterialTextTheme Fallback { get; } = new();

    private static FontFamily ResolveDefaultBodyFontFamily()
    {
        if (OperatingSystem.IsMacOS())
        {
            return new FontFamily(".AppleSystemUIFont");
        }

        return Avalonia.Media.FontFamily.Default;
    }
}

public sealed record ThemeData
{
    private AppBarThemeData? _appBarTheme;
    private TextButtonThemeData? _textButtonTheme;
    private ElevatedButtonThemeData? _elevatedButtonTheme;
    private OutlinedButtonThemeData? _outlinedButtonTheme;
    private FilledButtonThemeData? _filledButtonTheme;

    public ThemeData(
        TargetPlatform? platform = null,
        Brightness? brightness = null,
        MaterialTextTheme? textTheme = null,
        Color? scaffoldBackgroundColor = null,
        Color? canvasColor = null,
        Color? primaryColor = null,
        Color? onPrimaryColor = null,
        bool? useMaterial3 = null,
        AppBarThemeData? appBarTheme = null,
        Color? onSurfaceColor = null,
        Color? onSurfaceVariantColor = null,
        Color? outlineColor = null,
        Color? surfaceContainerLowColor = null,
        Color? secondaryContainerColor = null,
        Color? onSecondaryContainerColor = null,
        ButtonStyle? textButtonStyle = null,
        ButtonStyle? elevatedButtonStyle = null,
        ButtonStyle? outlinedButtonStyle = null,
        ButtonStyle? filledButtonStyle = null,
        TextButtonThemeData? textButtonTheme = null,
        ElevatedButtonThemeData? elevatedButtonTheme = null,
        OutlinedButtonThemeData? outlinedButtonTheme = null,
        FilledButtonThemeData? filledButtonTheme = null)
    {
        Platform = platform ?? ResolveDefaultPlatform();
        Brightness = brightness ?? Brightness.Light;
        TextTheme = textTheme ?? MaterialTextTheme.Fallback;
        ScaffoldBackgroundColor = scaffoldBackgroundColor ?? Colors.White;
        CanvasColor = canvasColor ?? Colors.White;
        PrimaryColor = primaryColor ?? Colors.Blue;
        OnPrimaryColor = onPrimaryColor ?? Colors.White;
        UseMaterial3 = useMaterial3 ?? true;
        _appBarTheme = appBarTheme;
        OnSurfaceColor = onSurfaceColor ?? Colors.Black;
        OnSurfaceVariantColor = onSurfaceVariantColor ?? Color.Parse("#FF49454F");
        OutlineColor = outlineColor ?? Color.Parse("#FF79747E");
        SurfaceContainerLowColor = surfaceContainerLowColor ?? Color.Parse("#FFF7F2FA");
        SecondaryContainerColor = secondaryContainerColor ?? Color.Parse("#FFE8DEF8");
        OnSecondaryContainerColor = onSecondaryContainerColor ?? Color.Parse("#FF1D192B");
        TextButtonStyle = textButtonStyle;
        ElevatedButtonStyle = elevatedButtonStyle;
        OutlinedButtonStyle = outlinedButtonStyle;
        FilledButtonStyle = filledButtonStyle;
        _textButtonTheme = textButtonTheme;
        _elevatedButtonTheme = elevatedButtonTheme;
        _outlinedButtonTheme = outlinedButtonTheme;
        _filledButtonTheme = filledButtonTheme;
    }

    public TargetPlatform Platform { get; init; }

    public Brightness Brightness { get; init; }

    public MaterialTextTheme TextTheme { get; init; }

    public Color ScaffoldBackgroundColor { get; init; }

    public Color CanvasColor { get; init; }

    public Color PrimaryColor { get; init; }

    public Color OnPrimaryColor { get; init; }

    public bool UseMaterial3 { get; init; }

    public AppBarThemeData AppBarTheme
    {
        get => _appBarTheme ?? new AppBarThemeData();
        init => _appBarTheme = value;
    }

    public Color OnSurfaceColor { get; init; }

    public Color OnSurfaceVariantColor { get; init; }

    public Color OutlineColor { get; init; }

    public Color SurfaceContainerLowColor { get; init; }

    public Color SecondaryContainerColor { get; init; }

    public Color OnSecondaryContainerColor { get; init; }

    public ButtonStyle? TextButtonStyle { get; init; }

    public ButtonStyle? ElevatedButtonStyle { get; init; }

    public ButtonStyle? OutlinedButtonStyle { get; init; }

    public ButtonStyle? FilledButtonStyle { get; init; }

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
