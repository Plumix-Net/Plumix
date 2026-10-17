// Dart parity source: flutter/engine/src/flutter/lib/ui/key.dart

using System.Globalization;
using System.Text;

namespace Plumix.UI;

/// <summary>The type of a key event.</summary>
public enum KeyEventType
{
    /// <summary>The key is pressed.</summary>
    Down,

    /// <summary>The key is released.</summary>
    Up,

    /// <summary>The key is held, causing a repeated key input.</summary>
    Repeat,
}

/// <summary>The source device for the key event.</summary>
/// <remarks>
/// Not all platforms supply an accurate type. Defaults to <see cref="Keyboard"/>.
/// </remarks>
public enum KeyEventDeviceType
{
    /// <summary>The device is a keyboard.</summary>
    Keyboard,

    /// <summary>The device is a directional pad on something like a television remote control or similar.</summary>
    DirectionalPad,

    /// <summary>The device is a gamepad button.</summary>
    Gamepad,

    /// <summary>The device is a joystick button.</summary>
    Joystick,

    /// <summary>The device is a device connected to an HDMI bus.</summary>
    Hdmi,
}

/// <summary>Dart's enhanced-enum <c>label</c> getters of <see cref="KeyEventType"/> and
/// <see cref="KeyEventDeviceType"/>.</summary>
public static class KeyEventTypeLabels
{
    /// <summary>Dart's <c>KeyEventType.label</c>.</summary>
    public static string Label(this KeyEventType type) => type switch
    {
        KeyEventType.Down => "Key Down",
        KeyEventType.Up => "Key Up",
        KeyEventType.Repeat => "Key Repeat",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>Dart's <c>KeyEventDeviceType.label</c>.</summary>
    public static string Label(this KeyEventDeviceType deviceType) => deviceType switch
    {
        KeyEventDeviceType.Keyboard => "Keyboard",
        KeyEventDeviceType.DirectionalPad => "Directional Pad",
        KeyEventDeviceType.Gamepad => "Gamepad",
        KeyEventDeviceType.Joystick => "Joystick",
        KeyEventDeviceType.Hdmi => "HDMI",
        _ => throw new ArgumentOutOfRangeException(nameof(deviceType)),
    };
}

/// <summary>Signature for <see cref="PlatformDispatcher.OnKeyData"/>.</summary>
/// <remarks>The callback should return true if the key event has been handled by the framework.</remarks>
public delegate bool KeyDataCallback(KeyData data);

/// <summary>Information about a key event.</summary>
public sealed class KeyData
{
    /// <summary>Creates an object that represents a key event.</summary>
    public KeyData(
        TimeSpan timeStamp,
        KeyEventType type,
        long physical,
        long logical,
        string? character,
        bool synthesized,
        KeyEventDeviceType deviceType = KeyEventDeviceType.Keyboard)
    {
        TimeStamp = timeStamp;
        Type = type;
        Physical = physical;
        Logical = logical;
        Character = character;
        Synthesized = synthesized;
        DeviceType = deviceType;
    }

    /// <summary>Time of event dispatch, relative to an arbitrary timeline.</summary>
    public TimeSpan TimeStamp { get; }

    /// <summary>The type of the event.</summary>
    public KeyEventType Type { get; }

    /// <summary>
    /// Describes what type of device (such as a keyboard or directional pad) this event originated from.
    /// </summary>
    public KeyEventDeviceType DeviceType { get; }

    /// <summary>The key code for the physical key that has changed.</summary>
    public long Physical { get; }

    /// <summary>The key code for the logical key that has changed.</summary>
    public long Logical { get; }

    /// <summary>Character input from the event. Ignored for up events.</summary>
    public string? Character { get; }

    /// <summary>
    /// If <see cref="Synthesized"/> is true, this event does not correspond to a native event.
    /// </summary>
    public bool Synthesized { get; }

    // Returns the bits that are not included in [valueMask], shifted to the
    // right.
    //
    // For example, if the input is 0x12abcdabcd, then the result is 0x12.
    //
    // This is mostly equivalent to a right shift, resolving the problem that
    // JavaScript only support 32-bit bitwise operations and needs to use
    // division instead.
    private static long NonValueBits(long n)
    {
        // `n >> valueMaskWidth` is equivalent to `n / divisorForValueMask`.
        const int valueMaskWidth = 32;

        // Equivalent to assuming web platform for the native case.
        const int maxSafeIntegerWidth = 52;
        const long nonValueMask = (1L << (maxSafeIntegerWidth - valueMaskWidth)) - 1;

        return (n >> valueMaskWidth) & nonValueMask;
    }

    private string LogicalToString()
    {
        string result = $"0x{Logical.ToString("x", CultureInfo.InvariantCulture)}";
        // Find the highest-priority planes that the logical key belongs to.
        long planeNum = NonValueBits(Logical) & 0x0FF;
        string planeDescription = planeNum switch
        {
            0x000 => " (Unicode)",
            0x001 => " (Unprintable)",
            0x002 => " (Flutter)",
            0x011 => " (Android)",
            0x012 => " (Fuchsia)",
            0x013 => " (iOS)",
            0x014 => " (macOS)",
            0x015 => " (GTK)",
            0x016 => " (Windows)",
            0x017 => " (Web)",
            0x018 => " (GLFW)",
            _ => string.Empty,
        };
        return result + planeDescription;
    }

    private string EscapeCharacter()
    {
        if (Character == null)
        {
            return "<none>";
        }

        return Character switch
        {
            "\n" => "\"\\n\"",
            "\t" => "\"\\t\"",
            "\r" => "\"\\r\"",
            "\b" => "\"\\b\"",
            "\f" => "\"\\f\"",
            _ => $"\"{Character}\"",
        };
    }

    private string QuotedCharCode()
    {
        if (Character == null)
        {
            return string.Empty;
        }

        IEnumerable<string> hexChars = Character.Select(code =>
            ((int)code).ToString("x", CultureInfo.InvariantCulture).PadLeft(2, '0'));
        return $" (0x{string.Join(' ', hexChars)})";
    }

    /// <summary>
    /// Dart's <c>KeyData.toString</c>, including the source's missing closing parenthesis.
    /// </summary>
    public override string ToString()
    {
        string physical = Physical.ToString("x", CultureInfo.InvariantCulture);
        return $"KeyData({Type.Label()}, physical: 0x{physical}, logical: {LogicalToString()}, "
               + $"character: {EscapeCharacter()}{QuotedCharCode()}{(Synthesized ? ", synthesized" : string.Empty)}";
    }

    /// <summary>Returns a complete textual description of the information in this object.</summary>
    public string ToStringFull()
    {
        string physical = Physical.ToString("x", CultureInfo.InvariantCulture);
        string logical = Logical.ToString("x", CultureInfo.InvariantCulture);
        var builder = new StringBuilder();
        builder.Append($"{nameof(KeyData)}(type: {Type.Label()}, deviceType: {DeviceType.Label()}, ");
        builder.Append($"timeStamp: {DartDuration(TimeStamp)}, physical: 0x{physical}, logical: 0x{logical}, ");
        builder.Append($"character: {EscapeCharacter()}, synthesized: {(Synthesized ? "true" : "false")})");
        return builder.ToString();
    }

    // Dart's `Duration.toString`: total hours and six microsecond digits.
    private static string DartDuration(TimeSpan duration)
    {
        long microseconds = duration.Ticks / TimeSpan.TicksPerMicrosecond;
        string sign = microseconds < 0 ? "-" : string.Empty;
        microseconds = Math.Abs(microseconds);
        long hours = microseconds / 3_600_000_000;
        long minutes = microseconds / 60_000_000 % 60;
        long seconds = microseconds / 1_000_000 % 60;
        long fraction = microseconds % 1_000_000;
        return string.Create(
            CultureInfo.InvariantCulture, $"{sign}{hours}:{minutes:00}:{seconds:00}.{fraction:000000}");
    }
}
