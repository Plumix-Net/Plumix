namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/system_sound.dart

public enum SystemSoundType
{
    Click,
    Alert,
}

public static class SystemSound
{
    private static Action<SystemSoundType>? _soundRequested;

    public static event Action<SystemSoundType>? SoundRequested
    {
        add => _soundRequested += value;
        remove => _soundRequested -= value;
    }

    public static void Play(SystemSoundType type)
    {
        _soundRequested?.Invoke(type);
    }

    internal static void ResetForTests()
    {
        _soundRequested = null;
    }
}
