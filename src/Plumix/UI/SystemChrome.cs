using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/system_chrome.dart

/// <summary>Specifies a particular device orientation.</summary>
/// <remarks>
/// To determine which values correspond to which orientations, first position the device in its
/// default orientation (this is the orientation that the system first uses for its boot logo, or
/// the orientation in which the hardware logos or markings are upright, or the orientation in which
/// the cameras are at the top). If this is a portrait orientation, then this is
/// <see cref="PortraitUp"/>. Otherwise, it's <see cref="LandscapeLeft"/>. As you rotate the device
/// by 90 degrees in a counter-clockwise direction around the axis that pierces the screen, you step
/// through each value in this enum in the order given.
/// </remarks>
public enum DeviceOrientation
{
    /// <summary>If the device shows its boot logo in portrait, then the boot logo is shown in this
    /// orientation.</summary>
    PortraitUp,

    /// <summary>The orientation that is 90 degrees counterclockwise from <see cref="PortraitUp"/>.</summary>
    LandscapeLeft,

    /// <summary>The orientation that is 180 degrees from <see cref="PortraitUp"/>.</summary>
    PortraitDown,

    /// <summary>The orientation that is 90 degrees clockwise from <see cref="PortraitUp"/>.</summary>
    LandscapeRight,
}

/// <summary>
/// A description of the current application used to describe this application in the operating
/// system's task switcher.
/// </summary>
public sealed class ApplicationSwitcherDescription
{
    /// <summary>Creates an application switcher description.</summary>
    public ApplicationSwitcherDescription(string? label = null, long? primaryColor = null)
    {
        Label = label;
        PrimaryColor = primaryColor;
    }

    /// <summary>A label and description of the current state of the application.</summary>
    public string? Label { get; }

    /// <summary>The application's primary color, as an ARGB integer.</summary>
    /// <remarks>
    /// Dart's <c>int</c> is 64-bit, and an opaque ARGB value does not fit in a C# <c>int</c>, so
    /// this is a <c>long</c>.
    /// </remarks>
    public long? PrimaryColor { get; }
}

/// <summary>Specifies a system overlay at a particular location.</summary>
/// <remarks>Used by <see cref="SystemChrome.SetEnabledSystemUIMode"/>.</remarks>
public enum SystemUiOverlay
{
    /// <summary>The status bar provided by the embedder on the top of the application surface, if
    /// any.</summary>
    Top,

    /// <summary>The status bar provided by the embedder on the bottom of the application surface,
    /// if any.</summary>
    Bottom,
}

/// <summary>Describes different display configurations for both Android and iOS.</summary>
/// <remarks>
/// These modes mimic Android-specific display setups. Used by
/// <see cref="SystemChrome.SetEnabledSystemUIMode"/>.
/// </remarks>
public enum SystemUiMode
{
    /// <summary>Fullscreen display with status and navigation bars presentable by tapping anywhere
    /// on the display.</summary>
    LeanBack,

    /// <summary>Fullscreen display with status and navigation bars presentable through a swipe
    /// gesture at the edges of the display.</summary>
    Immersive,

    /// <summary>Fullscreen display with status and navigation elements rendered over the
    /// application.</summary>
    ImmersiveSticky,

    /// <summary>Displays the status and navigation bars, laid out over the application
    /// (Android SDK 29+).</summary>
    EdgeToEdge,

    /// <summary>Declares manually configured <see cref="SystemUiOverlay"/>s.</summary>
    Manual,
}

/// <summary>Specifies a preference for the style of the system overlays.</summary>
/// <remarks>
/// Used by <see cref="AnnotatedRegion{T}"/> and <see cref="SystemChrome.SetSystemUIOverlayStyle"/>.
/// dart:ui's <c>Brightness</c> is <see cref="PlatformBrightness"/> in Plumix (see
/// <c>docs/ai/DIVERGENCES.md</c>); the wire format and the diagnostics still spell it
/// <c>Brightness.light</c>/<c>Brightness.dark</c>, as Dart does.
/// </remarks>
public sealed class SystemUiOverlayStyle : Diagnosticable, IEquatable<SystemUiOverlayStyle>
{
    /// <summary>
    /// System overlays should be drawn with a light color. Intended for applications with a dark
    /// background.
    /// </summary>
    public static readonly SystemUiOverlayStyle Light = new(
        systemNavigationBarColor: new Color(0xFF000000),
        systemNavigationBarIconBrightness: PlatformBrightness.Light,
        statusBarIconBrightness: PlatformBrightness.Light,
        statusBarBrightness: PlatformBrightness.Dark);

    /// <summary>
    /// System overlays should be drawn with a dark color. Intended for applications with a light
    /// background.
    /// </summary>
    public static readonly SystemUiOverlayStyle Dark = new(
        systemNavigationBarColor: new Color(0xFF000000),
        systemNavigationBarIconBrightness: PlatformBrightness.Light,
        statusBarIconBrightness: PlatformBrightness.Dark,
        statusBarBrightness: PlatformBrightness.Light);

    /// <summary>Creates a new <see cref="SystemUiOverlayStyle"/>.</summary>
    public SystemUiOverlayStyle(
        Color? systemNavigationBarColor = null,
        Color? systemNavigationBarDividerColor = null,
        PlatformBrightness? systemNavigationBarIconBrightness = null,
        bool? systemNavigationBarContrastEnforced = null,
        Color? statusBarColor = null,
        PlatformBrightness? statusBarBrightness = null,
        PlatformBrightness? statusBarIconBrightness = null,
        bool? systemStatusBarContrastEnforced = null)
    {
        SystemNavigationBarColor = systemNavigationBarColor;
        SystemNavigationBarDividerColor = systemNavigationBarDividerColor;
        SystemNavigationBarIconBrightness = systemNavigationBarIconBrightness;
        SystemNavigationBarContrastEnforced = systemNavigationBarContrastEnforced;
        StatusBarColor = statusBarColor;
        StatusBarBrightness = statusBarBrightness;
        StatusBarIconBrightness = statusBarIconBrightness;
        SystemStatusBarContrastEnforced = systemStatusBarContrastEnforced;
    }

    /// <summary>The color of the system bottom navigation bar. Only honored in Android versions O
    /// and greater.</summary>
    public Color? SystemNavigationBarColor { get; }

    /// <summary>The color of the divider between the system's bottom navigation bar and the app's
    /// content. Only honored in Android versions P and greater.</summary>
    public Color? SystemNavigationBarDividerColor { get; }

    /// <summary>The brightness of the system navigation bar icons. Only honored in Android versions
    /// O and greater. When set to <see cref="PlatformBrightness.Light"/>, the icons are light.</summary>
    public PlatformBrightness? SystemNavigationBarIconBrightness { get; }

    /// <summary>
    /// Overrides the contrast enforcement when setting a transparent navigation bar. Only honored
    /// in Android versions Q and greater.
    /// </summary>
    public bool? SystemNavigationBarContrastEnforced { get; }

    /// <summary>The color of top status bar. Only honored in Android version M and greater.</summary>
    public Color? StatusBarColor { get; }

    /// <summary>The brightness of top status bar. Only honored in iOS.</summary>
    public PlatformBrightness? StatusBarBrightness { get; }

    /// <summary>The brightness of the top status bar icons. Only honored in Android version M and
    /// greater.</summary>
    public PlatformBrightness? StatusBarIconBrightness { get; }

    /// <summary>
    /// Overrides the contrast enforcement when setting a transparent status bar. Only honored in
    /// Android versions Q and greater.
    /// </summary>
    public bool? SystemStatusBarContrastEnforced { get; }

    /// <summary>Converts this object to a map to send over the platform channel.</summary>
    /// <remarks>Dart's private <c>_toMap</c>; the host decodes it with <see cref="FromMap"/>.</remarks>
    internal Dictionary<string, object?> ToMap()
    {
        return new Dictionary<string, object?>
        {
            ["systemNavigationBarColor"] = ColorValue(SystemNavigationBarColor),
            ["systemNavigationBarDividerColor"] = ColorValue(SystemNavigationBarDividerColor),
            ["systemStatusBarContrastEnforced"] = SystemStatusBarContrastEnforced,
            ["statusBarColor"] = ColorValue(StatusBarColor),
            ["statusBarBrightness"] = BrightnessToString(StatusBarBrightness),
            ["statusBarIconBrightness"] = BrightnessToString(StatusBarIconBrightness),
            ["systemNavigationBarIconBrightness"] = BrightnessToString(SystemNavigationBarIconBrightness),
            ["systemNavigationBarContrastEnforced"] = SystemNavigationBarContrastEnforced,
        };
    }

    /// <summary>
    /// Decodes the map <see cref="ToMap"/> produced. C#-only: Dart's platform side is the engine, and
    /// a Plumix host is the platform side of <c>flutter/platform</c>.
    /// </summary>
    internal static SystemUiOverlayStyle FromMap(System.Collections.IDictionary map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return new SystemUiOverlayStyle(
            systemNavigationBarColor: ColorFromValue(map["systemNavigationBarColor"]),
            systemNavigationBarDividerColor: ColorFromValue(map["systemNavigationBarDividerColor"]),
            systemNavigationBarIconBrightness: BrightnessFromString(map["systemNavigationBarIconBrightness"]),
            systemNavigationBarContrastEnforced: map["systemNavigationBarContrastEnforced"] as bool?,
            statusBarColor: ColorFromValue(map["statusBarColor"]),
            statusBarBrightness: BrightnessFromString(map["statusBarBrightness"]),
            statusBarIconBrightness: BrightnessFromString(map["statusBarIconBrightness"]),
            systemStatusBarContrastEnforced: map["systemStatusBarContrastEnforced"] as bool?);
    }

    /// <summary>
    /// Creates a copy of this theme with the given fields replaced with new values.
    /// </summary>
    public SystemUiOverlayStyle CopyWith(
        Color? systemNavigationBarColor = null,
        Color? systemNavigationBarDividerColor = null,
        bool? systemNavigationBarContrastEnforced = null,
        Color? statusBarColor = null,
        PlatformBrightness? statusBarBrightness = null,
        PlatformBrightness? statusBarIconBrightness = null,
        bool? systemStatusBarContrastEnforced = null,
        PlatformBrightness? systemNavigationBarIconBrightness = null)
    {
        return new SystemUiOverlayStyle(
            systemNavigationBarColor: systemNavigationBarColor ?? SystemNavigationBarColor,
            systemNavigationBarDividerColor: systemNavigationBarDividerColor ?? SystemNavigationBarDividerColor,
            systemNavigationBarContrastEnforced:
                systemNavigationBarContrastEnforced ?? SystemNavigationBarContrastEnforced,
            statusBarColor: statusBarColor ?? StatusBarColor,
            statusBarIconBrightness: statusBarIconBrightness ?? StatusBarIconBrightness,
            statusBarBrightness: statusBarBrightness ?? StatusBarBrightness,
            systemStatusBarContrastEnforced: systemStatusBarContrastEnforced ?? SystemStatusBarContrastEnforced,
            systemNavigationBarIconBrightness:
                systemNavigationBarIconBrightness ?? SystemNavigationBarIconBrightness);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            SystemNavigationBarColor,
            SystemNavigationBarDividerColor,
            SystemNavigationBarContrastEnforced,
            StatusBarColor,
            StatusBarBrightness,
            StatusBarIconBrightness,
            SystemStatusBarContrastEnforced,
            SystemNavigationBarIconBrightness);
    }

    /// <inheritdoc />
    public bool Equals(SystemUiOverlayStyle? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return Equals(other.SystemNavigationBarColor, SystemNavigationBarColor)
               && Equals(other.SystemNavigationBarDividerColor, SystemNavigationBarDividerColor)
               && other.SystemNavigationBarContrastEnforced == SystemNavigationBarContrastEnforced
               && Equals(other.StatusBarColor, StatusBarColor)
               && other.StatusBarIconBrightness == StatusBarIconBrightness
               && other.StatusBarBrightness == StatusBarBrightness
               && other.SystemStatusBarContrastEnforced == SystemStatusBarContrastEnforced
               && other.SystemNavigationBarIconBrightness == SystemNavigationBarIconBrightness;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SystemUiOverlayStyle);

    public static bool operator ==(SystemUiOverlayStyle? left, SystemUiOverlayStyle? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(SystemUiOverlayStyle? left, SystemUiOverlayStyle? right) => !(left == right);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Color>("systemNavigationBarColor", SystemNavigationBarColor));
        properties.Add(new DiagnosticsProperty<Color>(
            "systemNavigationBarDividerColor",
            SystemNavigationBarDividerColor));
        properties.Add(BrightnessProperty("systemNavigationBarIconBrightness", SystemNavigationBarIconBrightness));
        properties.Add(new DiagnosticsProperty<bool?>(
            "systemNavigationBarContrastEnforced",
            SystemNavigationBarContrastEnforced));
        properties.Add(new DiagnosticsProperty<Color>("statusBarColor", StatusBarColor));
        properties.Add(BrightnessProperty("statusBarBrightness", StatusBarBrightness));
        properties.Add(BrightnessProperty("statusBarIconBrightness", StatusBarIconBrightness));
        properties.Add(new DiagnosticsProperty<bool?>(
            "systemStatusBarContrastEnforced",
            SystemStatusBarContrastEnforced));
    }

    /// <summary>
    /// A <c>DiagnosticsProperty&lt;Brightness&gt;</c>: the value prints as dart:ui's
    /// <c>Brightness.light</c>/<c>Brightness.dark</c> rather than the C# enum's type name.
    /// </summary>
    private static DiagnosticsProperty<PlatformBrightness?> BrightnessProperty(
        string name,
        PlatformBrightness? value)
    {
        return new DiagnosticsProperty<PlatformBrightness?>(name, value, description: BrightnessToString(value));
    }

    /// <summary>Dart's <c>Color.value</c>, carried as a JSON integer.</summary>
    private static long? ColorValue(Color? color) => color is null ? null : color.ToARGB32();

    private static Color? ColorFromValue(object? value) => value switch
    {
        int intValue => new Color(unchecked((uint)intValue)),
        long longValue => new Color(unchecked((uint)longValue)),
        _ => null,
    };

    /// <summary>dart:ui's <c>Brightness.toString</c>.</summary>
    private static string? BrightnessToString(PlatformBrightness? brightness) => brightness switch
    {
        PlatformBrightness.Light => "Brightness.light",
        PlatformBrightness.Dark => "Brightness.dark",
        _ => null,
    };

    private static PlatformBrightness? BrightnessFromString(object? value) => value switch
    {
        "Brightness.light" => PlatformBrightness.Light,
        "Brightness.dark" => PlatformBrightness.Dark,
        _ => null,
    };
}

/// <summary>Controls specific aspects of the operating system's graphical interface and how it
/// interacts with the application.</summary>
public static class SystemChrome
{
    private static SystemUiOverlayStyle? _pendingStyle;
    private static SystemUiOverlayStyle? _latestStyle;

    /// <summary>
    /// Specifies the set of orientations the application interface can be displayed in.
    /// </summary>
    /// <remarks>
    /// The <paramref name="orientations"/> argument is a list of <see cref="DeviceOrientation"/>
    /// enum values. The empty list causes the application to defer to the operating system default.
    /// </remarks>
    public static async Task SetPreferredOrientations(IReadOnlyList<DeviceOrientation> orientations)
    {
        ArgumentNullException.ThrowIfNull(orientations);
        await SystemChannels.Platform.InvokeMethod<object>(
            "SystemChrome.setPreferredOrientations",
            Stringify(orientations));
    }

    /// <summary>
    /// Specifies the description of the current state of the application as it pertains to the
    /// application switcher (also known as "recent tasks").
    /// </summary>
    public static async Task SetApplicationSwitcherDescription(ApplicationSwitcherDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        await SystemChannels.Platform.InvokeMethod<object>(
            "SystemChrome.setApplicationSwitcherDescription",
            new Dictionary<string, object?>
            {
                ["label"] = description.Label,
                ["primaryColor"] = description.PrimaryColor,
            });
    }

    /// <summary>
    /// Specifies the <see cref="SystemUiMode"/> to have visible when the application is running.
    /// </summary>
    /// <remarks>
    /// The <paramref name="overlays"/> argument is a list of <see cref="SystemUiOverlay"/> enum
    /// values denoting the overlays to show when configured with <see cref="SystemUiMode.Manual"/>,
    /// and is ignored for every other mode.
    /// </remarks>
    public static async Task SetEnabledSystemUIMode(
        SystemUiMode mode,
        IReadOnlyList<SystemUiOverlay>? overlays = null)
    {
        if (mode != SystemUiMode.Manual)
        {
            await SystemChannels.Platform.InvokeMethod<object>(
                "SystemChrome.setEnabledSystemUIMode",
                Diagnostics.DescribeValue(mode));
        }
        else
        {
            if (Constants.KDebugMode && !(mode == SystemUiMode.Manual && overlays is not null))
            {
                throw new AssertionError("mode == SystemUiMode.manual && overlays != null");
            }

            // Dart's `overlays!`: outside debug mode a null list fails here.
            await SystemChannels.Platform.InvokeMethod<object>(
                "SystemChrome.setEnabledSystemUIOverlays",
                Stringify(overlays!));
        }
    }

    /// <summary>
    /// Sets the callback method for responding to changes in the system UI.
    /// </summary>
    /// <remarks>
    /// This is relevant when using <see cref="SystemUiMode.LeanBack"/> and
    /// <see cref="SystemUiMode.Immersive"/> on Android, where the system UI can appear based on user
    /// interaction. The callback receives whether the system overlays are visible.
    /// </remarks>
    public static async Task SetSystemUIChangeCallback(SystemUiChangeCallback? callback)
    {
        ServicesBinding.Instance.SetSystemUiChangeCallback(callback);
        // Skip setting up the listener if there is no callback.
        if (callback is not null)
        {
            await SystemChannels.Platform.InvokeMethod<object>("SystemChrome.setSystemUIChangeListener");
        }
    }

    /// <summary>
    /// Restores the system overlays to the last settings provided via
    /// <see cref="SetEnabledSystemUIMode"/>.
    /// </summary>
    public static async Task RestoreSystemUIOverlays()
    {
        await SystemChannels.Platform.InvokeMethod<object>("SystemChrome.restoreSystemUIOverlays");
    }

    /// <summary>Specifies the style to use for the system overlays (e.g. the status bar on Android
    /// or iOS, the system navigation bar on Android) that are visible (if any).</summary>
    /// <remarks>
    /// Calls are asynchronous: the style is sent in a microtask, so several calls in one event-loop
    /// turn result in a single platform message carrying the last one. A call whose style equals the
    /// last one sent schedules nothing.
    /// </remarks>
    public static void SetSystemUIOverlayStyle(SystemUiOverlayStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        if (_pendingStyle is not null)
        {
            // The microtask has already been queued; just update the pending value.
            _pendingStyle = style;
            return;
        }

        if (style == _latestStyle)
        {
            // Trivial success: no microtask has been queued and we're already set up the way the
            // caller wants.
            return;
        }

        _pendingStyle = style;
        Scheduler.ScheduleMicrotask(() =>
        {
            Debug.Assert(_pendingStyle is not null);
            if (_pendingStyle != _latestStyle)
            {
                Task<object?> invocation = SystemChannels.Platform.InvokeMethod<object>(
                    "SystemChrome.setSystemUIOverlayStyle",
                    _pendingStyle!.ToMap());
                Scheduler.RunAsync(() => ReportOverlayStyleError(invocation));
                _latestStyle = _pendingStyle;
            }

            _pendingStyle = null;
        });
    }

    /// <summary>
    /// Called by the binding during a transition to a new app lifecycle state.
    /// </summary>
    public static void HandleAppLifecycleStateChanged(AppLifecycleState state)
    {
        // When the app is detached, clear the record of the style sent to the host so that it will
        // be sent again when the app is reattached.
        if (state == AppLifecycleState.Detached)
        {
            Scheduler.ScheduleMicrotask(() => _latestStyle = null);
        }
    }

    /// <summary>The last style passed to <see cref="SetSystemUIOverlayStyle"/>.</summary>
    /// <remarks>
    /// This variable is used to determine whether to send the style to the host, and is exposed for
    /// tests (Dart's <c>@visibleForTesting latestStyle</c>).
    /// </remarks>
    public static SystemUiOverlayStyle? LatestStyle => _latestStyle;

    /// <summary>Forgets the pending and latest style, as a fresh test isolate starts without them.</summary>
    internal static void ResetForTests()
    {
        _pendingStyle = null;
        _latestStyle = null;
    }

    /// <summary>The <c>onError</c> half of Dart's <c>invokeMethod(...).then(..., onError: ...)</c>.</summary>
    private static async Task ReportOverlayStyleError(Task<object?> invocation)
    {
        try
        {
            await invocation;
        }
        catch (Exception error)
        {
            ServicesDebug.ReportError(error, "while setting the system UI overlay style");
        }
    }

    /// <summary>Dart's private <c>_stringify</c>: each value's <c>toString</c>.</summary>
    private static List<string> Stringify<T>(IEnumerable<T> list) where T : struct, Enum
    {
        var result = new List<string>();
        foreach (T item in list)
        {
            result.Add(Diagnostics.DescribeValue(item));
        }

        return result;
    }
}
