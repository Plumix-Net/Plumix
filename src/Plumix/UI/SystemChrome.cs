using Avalonia.Media;

namespace Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/services/system_chrome.dart (approximate)

public enum SystemUiIconBrightness
{
    Light,
    Dark,
}

public sealed record SystemUiOverlayStyle(
    Color? StatusBarColor = null,
    Color? NavigationBarColor = null,
    SystemUiIconBrightness? StatusBarIconBrightness = null,
    SystemUiIconBrightness? NavigationBarIconBrightness = null,
    SystemUiIconBrightness? StatusBarBrightness = null)
{
    /// <summary>
    /// System overlays drawn in a light color, for applications with a dark background. Dart types
    /// <c>statusBarBrightness</c> as <c>Brightness</c>; the two enums carry the same two values, and
    /// <c>Brightness</c> lives in <c>Plumix.Material</c>, which core may not reference.
    /// </summary>
    public static SystemUiOverlayStyle Light { get; } = new(
        StatusBarIconBrightness: SystemUiIconBrightness.Light,
        NavigationBarIconBrightness: SystemUiIconBrightness.Light,
        StatusBarBrightness: SystemUiIconBrightness.Dark);

    /// <summary>System overlays drawn in a dark color, for applications with a light background.</summary>
    public static SystemUiOverlayStyle Dark { get; } = new(
        StatusBarIconBrightness: SystemUiIconBrightness.Dark,
        NavigationBarIconBrightness: SystemUiIconBrightness.Dark,
        StatusBarBrightness: SystemUiIconBrightness.Light);
}

public sealed record ApplicationSwitcherDescription(
    string Label,
    uint PrimaryColor);

public static class SystemChrome
{
    private static SystemUiOverlayStyle _currentSystemUiOverlayStyle = new(
        StatusBarColor: Colors.Transparent,
        NavigationBarColor: Colors.Transparent,
        StatusBarIconBrightness: SystemUiIconBrightness.Dark,
        NavigationBarIconBrightness: SystemUiIconBrightness.Dark);

    public static event Action<SystemUiOverlayStyle>? SystemUiOverlayStyleChanged;

    public static event Action<ApplicationSwitcherDescription>? ApplicationSwitcherDescriptionChanged;

    public static SystemUiOverlayStyle CurrentSystemUiOverlayStyle => _currentSystemUiOverlayStyle;

    public static ApplicationSwitcherDescription? CurrentApplicationSwitcherDescription { get; private set; }

    public static void SetSystemUiOverlayStyle(SystemUiOverlayStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        if (Equals(_currentSystemUiOverlayStyle, style))
        {
            return;
        }

        _currentSystemUiOverlayStyle = style;
        SystemUiOverlayStyleChanged?.Invoke(style);
    }

    public static void SetApplicationSwitcherDescription(ApplicationSwitcherDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(description.Label);

        if (Equals(CurrentApplicationSwitcherDescription, description))
        {
            return;
        }

        CurrentApplicationSwitcherDescription = description;
        ApplicationSwitcherDescriptionChanged?.Invoke(description);
    }

    internal static void ResetSystemUiOverlayStyleForTests(SystemUiOverlayStyle? style = null)
    {
        _currentSystemUiOverlayStyle = style ?? new SystemUiOverlayStyle(
            StatusBarColor: Colors.Transparent,
            NavigationBarColor: Colors.Transparent,
            StatusBarIconBrightness: SystemUiIconBrightness.Dark,
            NavigationBarIconBrightness: SystemUiIconBrightness.Dark);
    }

    internal static void ResetApplicationSwitcherDescriptionForTests()
    {
        CurrentApplicationSwitcherDescription = null;
        ApplicationSwitcherDescriptionChanged = null;
    }
}
