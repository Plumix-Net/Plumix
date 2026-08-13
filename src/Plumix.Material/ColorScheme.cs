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
            primary: primary ?? Color.Parse("#FF6200EE"),
            onPrimary: onPrimary ?? Colors.White,
            secondary: secondary ?? Color.Parse("#FF03DAC6"),
            onSecondary: onSecondary ?? Colors.Black,
            error: error ?? Color.Parse("#FFB00020"),
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
            primary: primary ?? Color.Parse("#FFBB86FC"),
            onPrimary: onPrimary ?? Colors.Black,
            secondary: secondary ?? Color.Parse("#FF03DAC6"),
            onSecondary: onSecondary ?? Colors.Black,
            error: error ?? Color.Parse("#FFCF6679"),
            onError: onError ?? Colors.Black,
            surface: surface ?? Color.Parse("#FF121212"),
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
            background: background ?? Color.Parse("#FF121212"),
            onBackground: onBackground ?? Colors.White,
            surfaceVariant: surfaceVariant);
    }

    public static ColorScheme HighContrastLight() => Light(
        primary: Color.Parse("#FF0000BA"),
        secondary: Color.Parse("#FF66FFF9"),
        error: Color.Parse("#FF790000"));

    public static ColorScheme HighContrastDark() => Dark(
        primary: Color.Parse("#FFEFB7FF"),
        secondary: Color.Parse("#FF66FFF9"),
        error: Color.Parse("#FF9B374D"));

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
            .CreateFromSourceColor(RgbColor.FromRgb(seedColor.R, seedColor.G, seedColor.B))
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
        primary: C("#FF6750A4"),
        onPrimary: Colors.White,
        primaryContainer: C("#FFEADDFF"),
        onPrimaryContainer: C("#FF4F378B"),
        primaryFixed: C("#FFEADDFF"),
        primaryFixedDim: C("#FFD0BCFF"),
        onPrimaryFixed: C("#FF21005D"),
        onPrimaryFixedVariant: C("#FF4F378B"),
        secondary: C("#FF625B71"),
        onSecondary: Colors.White,
        secondaryContainer: C("#FFE8DEF8"),
        onSecondaryContainer: C("#FF4A4458"),
        secondaryFixed: C("#FFE8DEF8"),
        secondaryFixedDim: C("#FFCCC2DC"),
        onSecondaryFixed: C("#FF1D192B"),
        onSecondaryFixedVariant: C("#FF4A4458"),
        tertiary: C("#FF7D5260"),
        onTertiary: Colors.White,
        tertiaryContainer: C("#FFFFD8E4"),
        onTertiaryContainer: C("#FF633B48"),
        tertiaryFixed: C("#FFFFD8E4"),
        tertiaryFixedDim: C("#FFEFB8C8"),
        onTertiaryFixed: C("#FF31111D"),
        onTertiaryFixedVariant: C("#FF633B48"),
        error: C("#FFB3261E"),
        onError: Colors.White,
        errorContainer: C("#FFF9DEDC"),
        onErrorContainer: C("#FF8C1D18"),
        surface: C("#FFFEF7FF"),
        onSurface: C("#FF1D1B20"),
        surfaceBright: C("#FFFEF7FF"),
        surfaceContainerLowest: Colors.White,
        surfaceContainerLow: C("#FFF7F2FA"),
        surfaceContainer: C("#FFF3EDF7"),
        surfaceContainerHigh: C("#FFECE6F0"),
        surfaceContainerHighest: C("#FFE6E0E9"),
        surfaceDim: C("#FFDED8E1"),
        onSurfaceVariant: C("#FF49454F"),
        outline: C("#FF79747E"),
        outlineVariant: C("#FFCAC4D0"),
        shadow: Colors.Black,
        scrim: Colors.Black,
        inverseSurface: C("#FF322F35"),
        onInverseSurface: C("#FFF5EFF7"),
        inversePrimary: C("#FFD0BCFF"),
        surfaceTint: C("#FF6750A4"),
        background: C("#FFFEF7FF"),
        onBackground: C("#FF1D1B20"),
        surfaceVariant: C("#FFE7E0EC"));

    private static ColorScheme CreateMaterial3Dark() => new(
        brightness: Brightness.Dark,
        primary: C("#FFD0BCFF"),
        onPrimary: C("#FF381E72"),
        primaryContainer: C("#FF4F378B"),
        onPrimaryContainer: C("#FFEADDFF"),
        primaryFixed: C("#FFEADDFF"),
        primaryFixedDim: C("#FFD0BCFF"),
        onPrimaryFixed: C("#FF21005D"),
        onPrimaryFixedVariant: C("#FF4F378B"),
        secondary: C("#FFCCC2DC"),
        onSecondary: C("#FF332D41"),
        secondaryContainer: C("#FF4A4458"),
        onSecondaryContainer: C("#FFE8DEF8"),
        secondaryFixed: C("#FFE8DEF8"),
        secondaryFixedDim: C("#FFCCC2DC"),
        onSecondaryFixed: C("#FF1D192B"),
        onSecondaryFixedVariant: C("#FF4A4458"),
        tertiary: C("#FFEFB8C8"),
        onTertiary: C("#FF492532"),
        tertiaryContainer: C("#FF633B48"),
        onTertiaryContainer: C("#FFFFD8E4"),
        tertiaryFixed: C("#FFFFD8E4"),
        tertiaryFixedDim: C("#FFEFB8C8"),
        onTertiaryFixed: C("#FF31111D"),
        onTertiaryFixedVariant: C("#FF633B48"),
        error: C("#FFF2B8B5"),
        onError: C("#FF601410"),
        errorContainer: C("#FF8C1D18"),
        onErrorContainer: C("#FFF9DEDC"),
        surface: C("#FF141218"),
        onSurface: C("#FFE6E0E9"),
        surfaceBright: C("#FF3B383E"),
        surfaceContainerLowest: C("#FF0F0D13"),
        surfaceContainerLow: C("#FF1D1B20"),
        surfaceContainer: C("#FF211F26"),
        surfaceContainerHigh: C("#FF2B2930"),
        surfaceContainerHighest: C("#FF36343B"),
        surfaceDim: C("#FF141218"),
        onSurfaceVariant: C("#FFCAC4D0"),
        outline: C("#FF938F99"),
        outlineVariant: C("#FF49454F"),
        shadow: Colors.Black,
        scrim: Colors.Black,
        inverseSurface: C("#FFE6E0E9"),
        onInverseSurface: C("#FF322F35"),
        inversePrimary: C("#FF6750A4"),
        surfaceTint: C("#FFD0BCFF"),
        background: C("#FF141218"),
        onBackground: C("#FFE6E0E9"),
        surfaceVariant: C("#FF49454F"));

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

    private static Color ToColor(RgbColor color) => Color.FromRgb(color.Red, color.Green, color.Blue);

    private static Color LerpColor(Color a, Color b, double t) => new ColorTween().Evaluate(t, a, b);

    private static Color C(string value) => Color.Parse(value);
}
