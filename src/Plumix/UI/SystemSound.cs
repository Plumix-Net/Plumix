namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/system_sound.dart

/// <summary>A sound provided by the system.</summary>
public enum SystemSoundType
{
    /// <summary>A short indication that a button was pressed.</summary>
    Click,

    /// <summary>A short indication that a scroll wheel or picker moved.</summary>
    Tick,

    /// <summary>A sound indicating that something went wrong.</summary>
    Alert,
}

/// <summary>Provides access to the library of short system-specific sounds for common tasks.</summary>
public static class SystemSound
{
    /// <summary>Play the specified system sound. If that sound is not present on the system, this
    /// method is a no-op.</summary>
    public static Task Play(SystemSoundType type)
    {
        return SystemChannels.Platform.InvokeMethod<object>("SystemSound.play", DartName(type));
    }

    /// <summary>Dart's <c>SystemSoundType.toString()</c>, which the platform side matches on.</summary>
    internal static string DartName(SystemSoundType type) => type switch
    {
        SystemSoundType.Click => "SystemSoundType.click",
        SystemSoundType.Tick => "SystemSoundType.tick",
        SystemSoundType.Alert => "SystemSoundType.alert",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
