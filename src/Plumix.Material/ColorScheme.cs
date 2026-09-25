using Avalonia.Media;
using MaterialTheming;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity source:
// material_ui/lib/src/color_scheme.dart

public enum DynamicSchemeVariant
{
    TonalSpot,
    Fidelity,
    Monochrome,
    Neutral,
    Vibrant,
    Expressive,
    Content,
    Rainbow,
    FruitSalad,
}

public sealed record ColorScheme
{
    public ColorScheme(
        Brightness brightness,
        Color primary,
        Color onPrimary,
        Color secondary,
        Color onSecondary,
        Color error,
        Color onError,
        Color surface,
        Color onSurface,
        Color? primaryContainer = null,
        Color? onPrimaryContainer = null,
        Color? primaryFixed = null,
        Color? primaryFixedDim = null,
        Color? onPrimaryFixed = null,
        Color? onPrimaryFixedVariant = null,
        Color? secondaryContainer = null,
        Color? onSecondaryContainer = null,
        Color? secondaryFixed = null,
        Color? secondaryFixedDim = null,
        Color? onSecondaryFixed = null,
        Color? onSecondaryFixedVariant = null,
        Color? tertiary = null,
        Color? onTertiary = null,
        Color? tertiaryContainer = null,
        Color? onTertiaryContainer = null,
        Color? tertiaryFixed = null,
        Color? tertiaryFixedDim = null,
        Color? onTertiaryFixed = null,
        Color? onTertiaryFixedVariant = null,
        Color? errorContainer = null,
        Color? onErrorContainer = null,
        Color? surfaceDim = null,
        Color? surfaceBright = null,
        Color? surfaceContainerLowest = null,
        Color? surfaceContainerLow = null,
        Color? surfaceContainer = null,
        Color? surfaceContainerHigh = null,
        Color? surfaceContainerHighest = null,
        Color? onSurfaceVariant = null,
        Color? outline = null,
        Color? outlineVariant = null,
        Color? shadow = null,
        Color? scrim = null,
        Color? inverseSurface = null,
        Color? onInverseSurface = null,
        Color? inversePrimary = null,
        Color? surfaceTint = null,
        Color? background = null,
        Color? onBackground = null,
        Color? surfaceVariant = null)
    {
        Brightness = brightness;
        Primary = primary;
        OnPrimary = onPrimary;
        PrimaryContainer = primaryContainer ?? primary;
        OnPrimaryContainer = onPrimaryContainer ?? onPrimary;
        PrimaryFixed = primaryFixed ?? primary;
        PrimaryFixedDim = primaryFixedDim ?? primary;
        OnPrimaryFixed = onPrimaryFixed ?? onPrimary;
        OnPrimaryFixedVariant = onPrimaryFixedVariant ?? onPrimary;
        Secondary = secondary;
        OnSecondary = onSecondary;
        SecondaryContainer = secondaryContainer ?? secondary;
        OnSecondaryContainer = onSecondaryContainer ?? onSecondary;
        SecondaryFixed = secondaryFixed ?? secondary;
        SecondaryFixedDim = secondaryFixedDim ?? secondary;
        OnSecondaryFixed = onSecondaryFixed ?? onSecondary;
        OnSecondaryFixedVariant = onSecondaryFixedVariant ?? onSecondary;
        Tertiary = tertiary ?? secondary;
        OnTertiary = onTertiary ?? onSecondary;
        TertiaryContainer = tertiaryContainer ?? Tertiary;
        OnTertiaryContainer = onTertiaryContainer ?? OnTertiary;
        TertiaryFixed = tertiaryFixed ?? Tertiary;
        TertiaryFixedDim = tertiaryFixedDim ?? Tertiary;
        OnTertiaryFixed = onTertiaryFixed ?? OnTertiary;
        OnTertiaryFixedVariant = onTertiaryFixedVariant ?? OnTertiary;
        Error = error;
        OnError = onError;
        ErrorContainer = errorContainer ?? error;
        OnErrorContainer = onErrorContainer ?? onError;
        Surface = surface;
        OnSurface = onSurface;
        SurfaceVariant = surfaceVariant ?? surface;
        SurfaceDim = surfaceDim ?? surface;
        SurfaceBright = surfaceBright ?? surface;
        SurfaceContainerLowest = surfaceContainerLowest ?? surface;
        SurfaceContainerLow = surfaceContainerLow ?? surface;
        SurfaceContainer = surfaceContainer ?? surface;
        SurfaceContainerHigh = surfaceContainerHigh ?? surface;
        SurfaceContainerHighest = surfaceContainerHighest ?? surface;
        OnSurfaceVariant = onSurfaceVariant ?? onSurface;
        Background = background ?? surface;
        OnBackground = onBackground ?? onSurface;
        Outline = outline ?? OnBackground;
        OutlineVariant = outlineVariant ?? OnBackground;
        Shadow = shadow ?? Colors.Black;
        Scrim = scrim ?? Colors.Black;
        InverseSurface = inverseSurface ?? onSurface;
        OnInverseSurface = onInverseSurface ?? surface;
        InversePrimary = inversePrimary ?? onPrimary;
        SurfaceTint = surfaceTint ?? primary;
    }

    public Brightness Brightness { get; init; }

    public Color Primary { get; init; }

    public Color OnPrimary { get; init; }

    public Color PrimaryContainer { get; init; }

    public Color OnPrimaryContainer { get; init; }

    public Color PrimaryFixed { get; init; }

    public Color PrimaryFixedDim { get; init; }

    public Color OnPrimaryFixed { get; init; }

    public Color OnPrimaryFixedVariant { get; init; }

    public Color Secondary { get; init; }

    public Color OnSecondary { get; init; }

    public Color SecondaryContainer { get; init; }

    public Color OnSecondaryContainer { get; init; }

    public Color SecondaryFixed { get; init; }

    public Color SecondaryFixedDim { get; init; }

    public Color OnSecondaryFixed { get; init; }

    public Color OnSecondaryFixedVariant { get; init; }

    public Color Tertiary { get; init; }

    public Color OnTertiary { get; init; }

    public Color TertiaryContainer { get; init; }

    public Color OnTertiaryContainer { get; init; }

    public Color TertiaryFixed { get; init; }

    public Color TertiaryFixedDim { get; init; }

    public Color OnTertiaryFixed { get; init; }

    public Color OnTertiaryFixedVariant { get; init; }

    public Color Error { get; init; }

    public Color OnError { get; init; }

    public Color ErrorContainer { get; init; }

    public Color OnErrorContainer { get; init; }

    public Color Surface { get; init; }

    public Color OnSurface { get; init; }

    public Color Background { get; init; }

    public Color OnBackground { get; init; }

    public Color SurfaceVariant { get; init; }

    public Color SurfaceDim { get; init; }

    public Color SurfaceBright { get; init; }

    public Color SurfaceContainerLowest { get; init; }

    public Color SurfaceContainerLow { get; init; }

    public Color SurfaceContainer { get; init; }

    public Color SurfaceContainerHigh { get; init; }

    public Color SurfaceContainerHighest { get; init; }

    public Color OnSurfaceVariant { get; init; }

    public Color Outline { get; init; }

    public Color OutlineVariant { get; init; }

    public Color Shadow { get; init; }

    public Color Scrim { get; init; }

    public Color InverseSurface { get; init; }

    public Color OnInverseSurface { get; init; }

    public Color InversePrimary { get; init; }

    public Color SurfaceTint { get; init; }

    public static ColorScheme Light(
        Brightness brightness = Brightness.Light,
        Color? primary = null,
        Color? onPrimary = null,
        Color? primaryContainer = null,
        Color? onPrimaryContainer = null,
        Color? primaryFixed = null,
        Color? primaryFixedDim = null,
        Color? onPrimaryFixed = null,
        Color? onPrimaryFixedVariant = null,
        Color? secondary = null,
        Color? onSecondary = null,
        Color? secondaryContainer = null,
        Color? onSecondaryContainer = null,
        Color? secondaryFixed = null,
        Color? secondaryFixedDim = null,
        Color? onSecondaryFixed = null,
        Color? onSecondaryFixedVariant = null,
        Color? tertiary = null,
        Color? onTertiary = null,
        Color? tertiaryContainer = null,
        Color? onTertiaryContainer = null,
        Color? tertiaryFixed = null,
        Color? tertiaryFixedDim = null,
        Color? onTertiaryFixed = null,
        Color? onTertiaryFixedVariant = null,
        Color? error = null,
        Color? onError = null,
        Color? errorContainer = null,
        Color? onErrorContainer = null,
        Color? surface = null,
        Color? onSurface = null,
        Color? surfaceDim = null,
        Color? surfaceBright = null,
        Color? surfaceContainerLowest = null,
        Color? surfaceContainerLow = null,
        Color? surfaceContainer = null,
        Color? surfaceContainerHigh = null,
        Color? surfaceContainerHighest = null,
        Color? onSurfaceVariant = null,
        Color? outline = null,
        Color? outlineVariant = null,
        Color? shadow = null,
        Color? scrim = null,
        Color? inverseSurface = null,
        Color? onInverseSurface = null,
        Color? inversePrimary = null,
        Color? surfaceTint = null,
        Color? background = null,
        Color? onBackground = null,
        Color? surfaceVariant = null)
    {
        return new ColorScheme(
            brightness: brightness,
            primary: primary ?? new Color(0xFF6200EE),
            onPrimary: onPrimary ?? Colors.White,
            secondary: secondary ?? new Color(0xFF03DAC6),
            onSecondary: onSecondary ?? Colors.Black,
            error: error ?? new Color(0xFFB00020),
            onError: onError ?? Colors.White,
            surface: surface ?? Colors.White,
            onSurface: onSurface ?? Colors.Black,
            primaryContainer: primaryContainer,
            onPrimaryContainer: onPrimaryContainer,
            primaryFixed: primaryFixed,
            primaryFixedDim: primaryFixedDim,
            onPrimaryFixed: onPrimaryFixed,
            onPrimaryFixedVariant: onPrimaryFixedVariant,
            secondaryContainer: secondaryContainer,
            onSecondaryContainer: onSecondaryContainer,
            secondaryFixed: secondaryFixed,
            secondaryFixedDim: secondaryFixedDim,
            onSecondaryFixed: onSecondaryFixed,
            onSecondaryFixedVariant: onSecondaryFixedVariant,
            tertiary: tertiary,
            onTertiary: onTertiary,
            tertiaryContainer: tertiaryContainer,
            onTertiaryContainer: onTertiaryContainer,
            tertiaryFixed: tertiaryFixed,
            tertiaryFixedDim: tertiaryFixedDim,
            onTertiaryFixed: onTertiaryFixed,
            onTertiaryFixedVariant: onTertiaryFixedVariant,
            errorContainer: errorContainer,
            onErrorContainer: onErrorContainer,
            surfaceDim: surfaceDim,
            surfaceBright: surfaceBright,
            surfaceContainerLowest: surfaceContainerLowest,
            surfaceContainerLow: surfaceContainerLow,
            surfaceContainer: surfaceContainer,
            surfaceContainerHigh: surfaceContainerHigh,
            surfaceContainerHighest: surfaceContainerHighest,
            onSurfaceVariant: onSurfaceVariant,
            outline: outline,
            outlineVariant: outlineVariant,
            shadow: shadow,
            scrim: scrim,
            inverseSurface: inverseSurface,
            onInverseSurface: onInverseSurface,
            inversePrimary: inversePrimary,
            surfaceTint: surfaceTint,
            background: background ?? Colors.White,
            onBackground: onBackground ?? Colors.Black,
            surfaceVariant: surfaceVariant);
    }

    public static ColorScheme Dark(
        Brightness brightness = Brightness.Dark,
        Color? primary = null,
        Color? onPrimary = null,
        Color? primaryContainer = null,
        Color? onPrimaryContainer = null,
        Color? primaryFixed = null,
        Color? primaryFixedDim = null,
        Color? onPrimaryFixed = null,
        Color? onPrimaryFixedVariant = null,
        Color? secondary = null,
        Color? onSecondary = null,
        Color? secondaryContainer = null,
        Color? onSecondaryContainer = null,
        Color? secondaryFixed = null,
        Color? secondaryFixedDim = null,
        Color? onSecondaryFixed = null,
        Color? onSecondaryFixedVariant = null,
        Color? tertiary = null,
        Color? onTertiary = null,
        Color? tertiaryContainer = null,
        Color? onTertiaryContainer = null,
        Color? tertiaryFixed = null,
        Color? tertiaryFixedDim = null,
        Color? onTertiaryFixed = null,
        Color? onTertiaryFixedVariant = null,
        Color? error = null,
        Color? onError = null,
        Color? errorContainer = null,
        Color? onErrorContainer = null,
        Color? surface = null,
        Color? onSurface = null,
        Color? surfaceDim = null,
        Color? surfaceBright = null,
        Color? surfaceContainerLowest = null,
        Color? surfaceContainerLow = null,
        Color? surfaceContainer = null,
        Color? surfaceContainerHigh = null,
        Color? surfaceContainerHighest = null,
        Color? onSurfaceVariant = null,
        Color? outline = null,
        Color? outlineVariant = null,
        Color? shadow = null,
        Color? scrim = null,
        Color? inverseSurface = null,
        Color? onInverseSurface = null,
        Color? inversePrimary = null,
        Color? surfaceTint = null,
        Color? background = null,
        Color? onBackground = null,
        Color? surfaceVariant = null)
    {
        return new ColorScheme(
            brightness: brightness,
            primary: primary ?? new Color(0xFFBB86FC),
            onPrimary: onPrimary ?? Colors.Black,
            secondary: secondary ?? new Color(0xFF03DAC6),
            onSecondary: onSecondary ?? Colors.Black,
            error: error ?? new Color(0xFFCF6679),
            onError: onError ?? Colors.Black,
            surface: surface ?? new Color(0xFF121212),
            onSurface: onSurface ?? Colors.White,
            primaryContainer: primaryContainer,
            onPrimaryContainer: onPrimaryContainer,
            primaryFixed: primaryFixed,
            primaryFixedDim: primaryFixedDim,
            onPrimaryFixed: onPrimaryFixed,
            onPrimaryFixedVariant: onPrimaryFixedVariant,
            secondaryContainer: secondaryContainer,
            onSecondaryContainer: onSecondaryContainer,
            secondaryFixed: secondaryFixed,
            secondaryFixedDim: secondaryFixedDim,
            onSecondaryFixed: onSecondaryFixed,
            onSecondaryFixedVariant: onSecondaryFixedVariant,
            tertiary: tertiary,
            onTertiary: onTertiary,
            tertiaryContainer: tertiaryContainer,
            onTertiaryContainer: onTertiaryContainer,
            tertiaryFixed: tertiaryFixed,
            tertiaryFixedDim: tertiaryFixedDim,
            onTertiaryFixed: onTertiaryFixed,
            onTertiaryFixedVariant: onTertiaryFixedVariant,
            errorContainer: errorContainer,
            onErrorContainer: onErrorContainer,
            surfaceDim: surfaceDim,
            surfaceBright: surfaceBright,
            surfaceContainerLowest: surfaceContainerLowest,
            surfaceContainerLow: surfaceContainerLow,
            surfaceContainer: surfaceContainer,
            surfaceContainerHigh: surfaceContainerHigh,
            surfaceContainerHighest: surfaceContainerHighest,
            onSurfaceVariant: onSurfaceVariant,
            outline: outline,
            outlineVariant: outlineVariant,
            shadow: shadow,
            scrim: scrim,
            inverseSurface: inverseSurface,
            onInverseSurface: onInverseSurface,
            inversePrimary: inversePrimary,
            surfaceTint: surfaceTint,
            background: background ?? new Color(0xFF121212),
            onBackground: onBackground ?? Colors.White,
            surfaceVariant: surfaceVariant);
    }

    public static ColorScheme HighContrastLight() => Light(
        primary: new Color(0xFF0000BA),
        secondary: new Color(0xFF66FFF9),
        error: new Color(0xFF790000));

    public static ColorScheme HighContrastDark() => Dark(
        primary: new Color(0xFFEFB7FF),
        secondary: new Color(0xFF66FFF9),
        error: new Color(0xFF9B374D));

    /// <summary>
    /// Creates a Material 2 color scheme from a <see cref="MaterialColor"/> swatch.
    /// </summary>
    /// <remarks>
    /// In Material 3 this factory is ignored by <see cref="ThemeData"/> when it creates its default
    /// color scheme. If <see cref="ThemeData.UseMaterial3"/> is false, then this factory is used by
    /// <see cref="ThemeData"/> to create its default color scheme.
    /// </remarks>
    public static ColorScheme FromSwatch(
        MaterialColor? primarySwatch = null,
        Color? accentColor = null,
        Color? cardColor = null,
        Color? backgroundColor = null,
        Color? errorColor = null,
        Brightness brightness = Brightness.Light)
    {
        primarySwatch ??= Colors.Blue;
        bool isDark = brightness == Brightness.Dark;
        bool primaryIsDark = BrightnessFor(primarySwatch) == Brightness.Dark;
        Color secondary = accentColor
                          ?? (isDark ? Colors.TealAccent.Shade200 : primarySwatch);
        bool secondaryIsDark = BrightnessFor(secondary) == Brightness.Dark;

        return new ColorScheme(
            brightness: brightness,
            primary: primarySwatch,
            secondary: secondary,
            surface: cardColor ?? (isDark ? Colors.Grey.Shade800 : Colors.White),
            error: errorColor ?? Colors.Red.Shade700,
            onPrimary: primaryIsDark ? Colors.White : Colors.Black,
            onSecondary: secondaryIsDark ? Colors.White : Colors.Black,
            onSurface: isDark ? Colors.White : Colors.Black,
            onError: isDark ? Colors.Black : Colors.White,
            background: backgroundColor
                        ?? (isDark ? Colors.Grey.Shade700 : primarySwatch.Shade200),
            onBackground: primaryIsDark ? Colors.White : Colors.Black);
    }

    private static Brightness BrightnessFor(Color color) =>
        ThemeData.EstimateBrightnessForColor(color);

    public static ColorScheme FromSeed(
        Color seedColor,
        Brightness brightness = Brightness.Light,
        DynamicSchemeVariant dynamicSchemeVariant = DynamicSchemeVariant.TonalSpot,
        double contrastLevel = 0.0,
        Color? primary = null,
        Color? onPrimary = null,
        Color? primaryContainer = null,
        Color? onPrimaryContainer = null,
        Color? primaryFixed = null,
        Color? primaryFixedDim = null,
        Color? onPrimaryFixed = null,
        Color? onPrimaryFixedVariant = null,
        Color? secondary = null,
        Color? onSecondary = null,
        Color? secondaryContainer = null,
        Color? onSecondaryContainer = null,
        Color? secondaryFixed = null,
        Color? secondaryFixedDim = null,
        Color? onSecondaryFixed = null,
        Color? onSecondaryFixedVariant = null,
        Color? tertiary = null,
        Color? onTertiary = null,
        Color? tertiaryContainer = null,
        Color? onTertiaryContainer = null,
        Color? tertiaryFixed = null,
        Color? tertiaryFixedDim = null,
        Color? onTertiaryFixed = null,
        Color? onTertiaryFixedVariant = null,
        Color? error = null,
        Color? onError = null,
        Color? errorContainer = null,
        Color? onErrorContainer = null,
        Color? outline = null,
        Color? outlineVariant = null,
        Color? surface = null,
        Color? onSurface = null,
        Color? surfaceDim = null,
        Color? surfaceBright = null,
        Color? surfaceContainerLowest = null,
        Color? surfaceContainerLow = null,
        Color? surfaceContainer = null,
        Color? surfaceContainerHigh = null,
        Color? surfaceContainerHighest = null,
        Color? onSurfaceVariant = null,
        Color? inverseSurface = null,
        Color? onInverseSurface = null,
        Color? inversePrimary = null,
        Color? shadow = null,
        Color? scrim = null,
        Color? surfaceTint = null,
        Color? background = null,
        Color? onBackground = null,
        Color? surfaceVariant = null)
    {
        if (!double.IsFinite(contrastLevel) || contrastLevel is < -1.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contrastLevel),
                "Contrast level must be finite and between -1.0 and 1.0.");
        }

        var builder = ThemeBuilder
            .CreateFromSourceColor(RgbColor.FromRgb((byte)seedColor.Red, (byte)seedColor.Green, (byte)seedColor.Blue))
            .WithMode(
                brightness == Brightness.Dark
                    ? MaterialTheming.ThemeMode.Dark
                    : MaterialTheming.ThemeMode.Light)
            .WithVariant(ToMaterialVariant(dynamicSchemeVariant))
            .WithContrastLevel(contrastLevel)
            .WithSpecVersion(SpecVersion.Spec2021);
        ThemeColors colors = builder.Build();

        var generated = new ColorScheme(
            brightness: brightness,
            primary: ToColor(colors.Primary),
            onPrimary: ToColor(colors.OnPrimary),
            primaryContainer: ToColor(colors.PrimaryContainer),
            onPrimaryContainer: ToColor(colors.OnPrimaryContainer),
            primaryFixed: ToColor(colors.PrimaryFixed),
            primaryFixedDim: ToColor(colors.PrimaryFixedDim),
            onPrimaryFixed: ToColor(colors.OnPrimaryFixed),
            onPrimaryFixedVariant: ToColor(colors.OnPrimaryFixedVariant),
            secondary: ToColor(colors.Secondary),
            onSecondary: ToColor(colors.OnSecondary),
            secondaryContainer: ToColor(colors.SecondaryContainer),
            onSecondaryContainer: ToColor(colors.OnSecondaryContainer),
            secondaryFixed: ToColor(colors.SecondaryFixed),
            secondaryFixedDim: ToColor(colors.SecondaryFixedDim),
            onSecondaryFixed: ToColor(colors.OnSecondaryFixed),
            onSecondaryFixedVariant: ToColor(colors.OnSecondaryFixedVariant),
            tertiary: ToColor(colors.Tertiary),
            onTertiary: ToColor(colors.OnTertiary),
            tertiaryContainer: ToColor(colors.TertiaryContainer),
            onTertiaryContainer: ToColor(colors.OnTertiaryContainer),
            tertiaryFixed: ToColor(colors.TertiaryFixed),
            tertiaryFixedDim: ToColor(colors.TertiaryFixedDim),
            onTertiaryFixed: ToColor(colors.OnTertiaryFixed),
            onTertiaryFixedVariant: ToColor(colors.OnTertiaryFixedVariant),
            error: ToColor(colors.Error),
            onError: ToColor(colors.OnError),
            errorContainer: ToColor(colors.ErrorContainer),
            onErrorContainer: ToColor(colors.OnErrorContainer),
            surface: ToColor(colors.Surface),
            onSurface: ToColor(colors.OnSurface),
            surfaceDim: ToColor(colors.SurfaceDim),
            surfaceBright: ToColor(colors.SurfaceBright),
            surfaceContainerLowest: ToColor(colors.SurfaceContainerLowest),
            surfaceContainerLow: ToColor(colors.SurfaceContainerLow),
            surfaceContainer: ToColor(colors.SurfaceContainer),
            surfaceContainerHigh: ToColor(colors.SurfaceContainerHigh),
            surfaceContainerHighest: ToColor(colors.SurfaceContainerHighest),
            onSurfaceVariant: ToColor(colors.OnSurfaceVariant),
            outline: ToColor(colors.Outline),
            outlineVariant: ToColor(colors.OutlineVariant),
            shadow: ToColor(colors.Shadow),
            scrim: ToColor(colors.Scrim),
            inverseSurface: ToColor(colors.InverseSurface),
            onInverseSurface: ToColor(colors.InverseOnSurface),
            inversePrimary: ToColor(colors.InversePrimary),
            surfaceTint: ToColor(colors.SurfaceTint),
            background: ToColor(colors.Background),
            onBackground: ToColor(colors.OnBackground),
            surfaceVariant: ToColor(colors.SurfaceVariant));
        return generated.CopyWith(
            primary: primary,
            onPrimary: onPrimary,
            primaryContainer: primaryContainer,
            onPrimaryContainer: onPrimaryContainer,
            primaryFixed: primaryFixed,
            primaryFixedDim: primaryFixedDim,
            onPrimaryFixed: onPrimaryFixed,
            onPrimaryFixedVariant: onPrimaryFixedVariant,
            secondary: secondary,
            onSecondary: onSecondary,
            secondaryContainer: secondaryContainer,
            onSecondaryContainer: onSecondaryContainer,
            secondaryFixed: secondaryFixed,
            secondaryFixedDim: secondaryFixedDim,
            onSecondaryFixed: onSecondaryFixed,
            onSecondaryFixedVariant: onSecondaryFixedVariant,
            tertiary: tertiary,
            onTertiary: onTertiary,
            tertiaryContainer: tertiaryContainer,
            onTertiaryContainer: onTertiaryContainer,
            tertiaryFixed: tertiaryFixed,
            tertiaryFixedDim: tertiaryFixedDim,
            onTertiaryFixed: onTertiaryFixed,
            onTertiaryFixedVariant: onTertiaryFixedVariant,
            error: error,
            onError: onError,
            errorContainer: errorContainer,
            onErrorContainer: onErrorContainer,
            outline: outline,
            outlineVariant: outlineVariant,
            surface: surface,
            onSurface: onSurface,
            surfaceDim: surfaceDim,
            surfaceBright: surfaceBright,
            surfaceContainerLowest: surfaceContainerLowest,
            surfaceContainerLow: surfaceContainerLow,
            surfaceContainer: surfaceContainer,
            surfaceContainerHigh: surfaceContainerHigh,
            surfaceContainerHighest: surfaceContainerHighest,
            onSurfaceVariant: onSurfaceVariant,
            inverseSurface: inverseSurface,
            onInverseSurface: onInverseSurface,
            inversePrimary: inversePrimary,
            shadow: shadow,
            scrim: scrim,
            surfaceTint: surfaceTint,
            background: background,
            onBackground: onBackground,
            surfaceVariant: surfaceVariant);
    }

    public ColorScheme CopyWith(
        Brightness? brightness = null,
        Color? primary = null,
        Color? onPrimary = null,
        Color? primaryContainer = null,
        Color? onPrimaryContainer = null,
        Color? primaryFixed = null,
        Color? primaryFixedDim = null,
        Color? onPrimaryFixed = null,
        Color? onPrimaryFixedVariant = null,
        Color? secondary = null,
        Color? onSecondary = null,
        Color? secondaryContainer = null,
        Color? onSecondaryContainer = null,
        Color? secondaryFixed = null,
        Color? secondaryFixedDim = null,
        Color? onSecondaryFixed = null,
        Color? onSecondaryFixedVariant = null,
        Color? tertiary = null,
        Color? onTertiary = null,
        Color? tertiaryContainer = null,
        Color? onTertiaryContainer = null,
        Color? tertiaryFixed = null,
        Color? tertiaryFixedDim = null,
        Color? onTertiaryFixed = null,
        Color? onTertiaryFixedVariant = null,
        Color? error = null,
        Color? onError = null,
        Color? errorContainer = null,
        Color? onErrorContainer = null,
        Color? surface = null,
        Color? onSurface = null,
        Color? surfaceDim = null,
        Color? surfaceBright = null,
        Color? surfaceContainerLowest = null,
        Color? surfaceContainerLow = null,
        Color? surfaceContainer = null,
        Color? surfaceContainerHigh = null,
        Color? surfaceContainerHighest = null,
        Color? onSurfaceVariant = null,
        Color? outline = null,
        Color? outlineVariant = null,
        Color? shadow = null,
        Color? scrim = null,
        Color? inverseSurface = null,
        Color? onInverseSurface = null,
        Color? inversePrimary = null,
        Color? surfaceTint = null,
        Color? background = null,
        Color? onBackground = null,
        Color? surfaceVariant = null)
    {
        return new ColorScheme(
            brightness: brightness ?? Brightness,
            primary: primary ?? Primary,
            onPrimary: onPrimary ?? OnPrimary,
            primaryContainer: primaryContainer ?? PrimaryContainer,
            onPrimaryContainer: onPrimaryContainer ?? OnPrimaryContainer,
            primaryFixed: primaryFixed ?? PrimaryFixed,
            primaryFixedDim: primaryFixedDim ?? PrimaryFixedDim,
            onPrimaryFixed: onPrimaryFixed ?? OnPrimaryFixed,
            onPrimaryFixedVariant: onPrimaryFixedVariant ?? OnPrimaryFixedVariant,
            secondary: secondary ?? Secondary,
            onSecondary: onSecondary ?? OnSecondary,
            secondaryContainer: secondaryContainer ?? SecondaryContainer,
            onSecondaryContainer: onSecondaryContainer ?? OnSecondaryContainer,
            secondaryFixed: secondaryFixed ?? SecondaryFixed,
            secondaryFixedDim: secondaryFixedDim ?? SecondaryFixedDim,
            onSecondaryFixed: onSecondaryFixed ?? OnSecondaryFixed,
            onSecondaryFixedVariant: onSecondaryFixedVariant ?? OnSecondaryFixedVariant,
            tertiary: tertiary ?? Tertiary,
            onTertiary: onTertiary ?? OnTertiary,
            tertiaryContainer: tertiaryContainer ?? TertiaryContainer,
            onTertiaryContainer: onTertiaryContainer ?? OnTertiaryContainer,
            tertiaryFixed: tertiaryFixed ?? TertiaryFixed,
            tertiaryFixedDim: tertiaryFixedDim ?? TertiaryFixedDim,
            onTertiaryFixed: onTertiaryFixed ?? OnTertiaryFixed,
            onTertiaryFixedVariant: onTertiaryFixedVariant ?? OnTertiaryFixedVariant,
            error: error ?? Error,
            onError: onError ?? OnError,
            errorContainer: errorContainer ?? ErrorContainer,
            onErrorContainer: onErrorContainer ?? OnErrorContainer,
            surface: surface ?? Surface,
            onSurface: onSurface ?? OnSurface,
            surfaceDim: surfaceDim ?? SurfaceDim,
            surfaceBright: surfaceBright ?? SurfaceBright,
            surfaceContainerLowest: surfaceContainerLowest ?? SurfaceContainerLowest,
            surfaceContainerLow: surfaceContainerLow ?? SurfaceContainerLow,
            surfaceContainer: surfaceContainer ?? SurfaceContainer,
            surfaceContainerHigh: surfaceContainerHigh ?? SurfaceContainerHigh,
            surfaceContainerHighest: surfaceContainerHighest ?? SurfaceContainerHighest,
            onSurfaceVariant: onSurfaceVariant ?? OnSurfaceVariant,
            outline: outline ?? Outline,
            outlineVariant: outlineVariant ?? OutlineVariant,
            shadow: shadow ?? Shadow,
            scrim: scrim ?? Scrim,
            inverseSurface: inverseSurface ?? InverseSurface,
            onInverseSurface: onInverseSurface ?? OnInverseSurface,
            inversePrimary: inversePrimary ?? InversePrimary,
            surfaceTint: surfaceTint ?? SurfaceTint,
            background: background ?? Background,
            onBackground: onBackground ?? OnBackground,
            surfaceVariant: surfaceVariant ?? SurfaceVariant);
    }

    public static ColorScheme Lerp(ColorScheme a, ColorScheme b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new ColorScheme(
            brightness: clampedT < 0.5 ? a.Brightness : b.Brightness,
            primary: LerpColor(a.Primary, b.Primary, clampedT),
            onPrimary: LerpColor(a.OnPrimary, b.OnPrimary, clampedT),
            primaryContainer: LerpColor(a.PrimaryContainer, b.PrimaryContainer, clampedT),
            onPrimaryContainer: LerpColor(a.OnPrimaryContainer, b.OnPrimaryContainer, clampedT),
            primaryFixed: LerpColor(a.PrimaryFixed, b.PrimaryFixed, clampedT),
            primaryFixedDim: LerpColor(a.PrimaryFixedDim, b.PrimaryFixedDim, clampedT),
            onPrimaryFixed: LerpColor(a.OnPrimaryFixed, b.OnPrimaryFixed, clampedT),
            onPrimaryFixedVariant: LerpColor(
                a.OnPrimaryFixedVariant,
                b.OnPrimaryFixedVariant,
                clampedT),
            secondary: LerpColor(a.Secondary, b.Secondary, clampedT),
            onSecondary: LerpColor(a.OnSecondary, b.OnSecondary, clampedT),
            secondaryContainer: LerpColor(a.SecondaryContainer, b.SecondaryContainer, clampedT),
            onSecondaryContainer: LerpColor(
                a.OnSecondaryContainer,
                b.OnSecondaryContainer,
                clampedT),
            secondaryFixed: LerpColor(a.SecondaryFixed, b.SecondaryFixed, clampedT),
            secondaryFixedDim: LerpColor(a.SecondaryFixedDim, b.SecondaryFixedDim, clampedT),
            onSecondaryFixed: LerpColor(a.OnSecondaryFixed, b.OnSecondaryFixed, clampedT),
            onSecondaryFixedVariant: LerpColor(
                a.OnSecondaryFixedVariant,
                b.OnSecondaryFixedVariant,
                clampedT),
            tertiary: LerpColor(a.Tertiary, b.Tertiary, clampedT),
            onTertiary: LerpColor(a.OnTertiary, b.OnTertiary, clampedT),
            tertiaryContainer: LerpColor(a.TertiaryContainer, b.TertiaryContainer, clampedT),
            onTertiaryContainer: LerpColor(
                a.OnTertiaryContainer,
                b.OnTertiaryContainer,
                clampedT),
            tertiaryFixed: LerpColor(a.TertiaryFixed, b.TertiaryFixed, clampedT),
            tertiaryFixedDim: LerpColor(a.TertiaryFixedDim, b.TertiaryFixedDim, clampedT),
            onTertiaryFixed: LerpColor(a.OnTertiaryFixed, b.OnTertiaryFixed, clampedT),
            onTertiaryFixedVariant: LerpColor(
                a.OnTertiaryFixedVariant,
                b.OnTertiaryFixedVariant,
                clampedT),
            error: LerpColor(a.Error, b.Error, clampedT),
            onError: LerpColor(a.OnError, b.OnError, clampedT),
            errorContainer: LerpColor(a.ErrorContainer, b.ErrorContainer, clampedT),
            onErrorContainer: LerpColor(a.OnErrorContainer, b.OnErrorContainer, clampedT),
            surface: LerpColor(a.Surface, b.Surface, clampedT),
            onSurface: LerpColor(a.OnSurface, b.OnSurface, clampedT),
            surfaceDim: LerpColor(a.SurfaceDim, b.SurfaceDim, clampedT),
            surfaceBright: LerpColor(a.SurfaceBright, b.SurfaceBright, clampedT),
            surfaceContainerLowest: LerpColor(
                a.SurfaceContainerLowest,
                b.SurfaceContainerLowest,
                clampedT),
            surfaceContainerLow: LerpColor(a.SurfaceContainerLow, b.SurfaceContainerLow, clampedT),
            surfaceContainer: LerpColor(a.SurfaceContainer, b.SurfaceContainer, clampedT),
            surfaceContainerHigh: LerpColor(
                a.SurfaceContainerHigh,
                b.SurfaceContainerHigh,
                clampedT),
            surfaceContainerHighest: LerpColor(
                a.SurfaceContainerHighest,
                b.SurfaceContainerHighest,
                clampedT),
            onSurfaceVariant: LerpColor(a.OnSurfaceVariant, b.OnSurfaceVariant, clampedT),
            outline: LerpColor(a.Outline, b.Outline, clampedT),
            outlineVariant: LerpColor(a.OutlineVariant, b.OutlineVariant, clampedT),
            shadow: LerpColor(a.Shadow, b.Shadow, clampedT),
            scrim: LerpColor(a.Scrim, b.Scrim, clampedT),
            inverseSurface: LerpColor(a.InverseSurface, b.InverseSurface, clampedT),
            onInverseSurface: LerpColor(a.OnInverseSurface, b.OnInverseSurface, clampedT),
            inversePrimary: LerpColor(a.InversePrimary, b.InversePrimary, clampedT),
            surfaceTint: LerpColor(a.SurfaceTint, b.SurfaceTint, clampedT),
            background: LerpColor(a.Background, b.Background, clampedT),
            onBackground: LerpColor(a.OnBackground, b.OnBackground, clampedT),
            surfaceVariant: LerpColor(a.SurfaceVariant, b.SurfaceVariant, clampedT));
    }

    internal static ColorScheme Material3Light { get; } = CreateMaterial3Light();

    internal static ColorScheme Material3Dark { get; } = CreateMaterial3Dark();

    private static ColorScheme CreateMaterial3Light() => new(
        brightness: Brightness.Light,
        primary: new Color(0xFF6750A4),
        onPrimary: Colors.White,
        primaryContainer: new Color(0xFFEADDFF),
        onPrimaryContainer: new Color(0xFF4F378B),
        primaryFixed: new Color(0xFFEADDFF),
        primaryFixedDim: new Color(0xFFD0BCFF),
        onPrimaryFixed: new Color(0xFF21005D),
        onPrimaryFixedVariant: new Color(0xFF4F378B),
        secondary: new Color(0xFF625B71),
        onSecondary: Colors.White,
        secondaryContainer: new Color(0xFFE8DEF8),
        onSecondaryContainer: new Color(0xFF4A4458),
        secondaryFixed: new Color(0xFFE8DEF8),
        secondaryFixedDim: new Color(0xFFCCC2DC),
        onSecondaryFixed: new Color(0xFF1D192B),
        onSecondaryFixedVariant: new Color(0xFF4A4458),
        tertiary: new Color(0xFF7D5260),
        onTertiary: Colors.White,
        tertiaryContainer: new Color(0xFFFFD8E4),
        onTertiaryContainer: new Color(0xFF633B48),
        tertiaryFixed: new Color(0xFFFFD8E4),
        tertiaryFixedDim: new Color(0xFFEFB8C8),
        onTertiaryFixed: new Color(0xFF31111D),
        onTertiaryFixedVariant: new Color(0xFF633B48),
        error: new Color(0xFFB3261E),
        onError: Colors.White,
        errorContainer: new Color(0xFFF9DEDC),
        onErrorContainer: new Color(0xFF8C1D18),
        surface: new Color(0xFFFEF7FF),
        onSurface: new Color(0xFF1D1B20),
        surfaceBright: new Color(0xFFFEF7FF),
        surfaceContainerLowest: Colors.White,
        surfaceContainerLow: new Color(0xFFF7F2FA),
        surfaceContainer: new Color(0xFFF3EDF7),
        surfaceContainerHigh: new Color(0xFFECE6F0),
        surfaceContainerHighest: new Color(0xFFE6E0E9),
        surfaceDim: new Color(0xFFDED8E1),
        onSurfaceVariant: new Color(0xFF49454F),
        outline: new Color(0xFF79747E),
        outlineVariant: new Color(0xFFCAC4D0),
        shadow: Colors.Black,
        scrim: Colors.Black,
        inverseSurface: new Color(0xFF322F35),
        onInverseSurface: new Color(0xFFF5EFF7),
        inversePrimary: new Color(0xFFD0BCFF),
        surfaceTint: new Color(0xFF6750A4),
        background: new Color(0xFFFEF7FF),
        onBackground: new Color(0xFF1D1B20),
        surfaceVariant: new Color(0xFFE7E0EC));

    private static ColorScheme CreateMaterial3Dark() => new(
        brightness: Brightness.Dark,
        primary: new Color(0xFFD0BCFF),
        onPrimary: new Color(0xFF381E72),
        primaryContainer: new Color(0xFF4F378B),
        onPrimaryContainer: new Color(0xFFEADDFF),
        primaryFixed: new Color(0xFFEADDFF),
        primaryFixedDim: new Color(0xFFD0BCFF),
        onPrimaryFixed: new Color(0xFF21005D),
        onPrimaryFixedVariant: new Color(0xFF4F378B),
        secondary: new Color(0xFFCCC2DC),
        onSecondary: new Color(0xFF332D41),
        secondaryContainer: new Color(0xFF4A4458),
        onSecondaryContainer: new Color(0xFFE8DEF8),
        secondaryFixed: new Color(0xFFE8DEF8),
        secondaryFixedDim: new Color(0xFFCCC2DC),
        onSecondaryFixed: new Color(0xFF1D192B),
        onSecondaryFixedVariant: new Color(0xFF4A4458),
        tertiary: new Color(0xFFEFB8C8),
        onTertiary: new Color(0xFF492532),
        tertiaryContainer: new Color(0xFF633B48),
        onTertiaryContainer: new Color(0xFFFFD8E4),
        tertiaryFixed: new Color(0xFFFFD8E4),
        tertiaryFixedDim: new Color(0xFFEFB8C8),
        onTertiaryFixed: new Color(0xFF31111D),
        onTertiaryFixedVariant: new Color(0xFF633B48),
        error: new Color(0xFFF2B8B5),
        onError: new Color(0xFF601410),
        errorContainer: new Color(0xFF8C1D18),
        onErrorContainer: new Color(0xFFF9DEDC),
        surface: new Color(0xFF141218),
        onSurface: new Color(0xFFE6E0E9),
        surfaceBright: new Color(0xFF3B383E),
        surfaceContainerLowest: new Color(0xFF0F0D13),
        surfaceContainerLow: new Color(0xFF1D1B20),
        surfaceContainer: new Color(0xFF211F26),
        surfaceContainerHigh: new Color(0xFF2B2930),
        surfaceContainerHighest: new Color(0xFF36343B),
        surfaceDim: new Color(0xFF141218),
        onSurfaceVariant: new Color(0xFFCAC4D0),
        outline: new Color(0xFF938F99),
        outlineVariant: new Color(0xFF49454F),
        shadow: Colors.Black,
        scrim: Colors.Black,
        inverseSurface: new Color(0xFFE6E0E9),
        onInverseSurface: new Color(0xFF322F35),
        inversePrimary: new Color(0xFF6750A4),
        surfaceTint: new Color(0xFFD0BCFF),
        background: new Color(0xFF141218),
        onBackground: new Color(0xFFE6E0E9),
        surfaceVariant: new Color(0xFF49454F));

    private static Variant ToMaterialVariant(DynamicSchemeVariant variant) => variant switch
    {
        DynamicSchemeVariant.TonalSpot => Variant.TonalSpot,
        DynamicSchemeVariant.Fidelity => Variant.Fidelity,
        DynamicSchemeVariant.Monochrome => Variant.Monochrome,
        DynamicSchemeVariant.Neutral => Variant.Neutral,
        DynamicSchemeVariant.Vibrant => Variant.Vibrant,
        DynamicSchemeVariant.Expressive => Variant.Expressive,
        DynamicSchemeVariant.Content => Variant.Content,
        DynamicSchemeVariant.Rainbow => Variant.Rainbow,
        DynamicSchemeVariant.FruitSalad => Variant.FruitSalad,
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };

    private static Color ToColor(RgbColor color) => Color.FromARGB(0xFF, color.Red, color.Green, color.Blue);

    private static Color LerpColor(Color a, Color b, double t) => new ColorTween().Evaluate(t, a, b);

}
