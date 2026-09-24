using System.Collections;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/clipboard.dart

/// <summary>Plain text data stored on the system clipboard.</summary>
public sealed class ClipboardData
{
    public ClipboardData(string text)
    {
        Text = text;
    }

    public string? Text { get; }
}

/// <summary>Methods for reading and writing the system clipboard through the platform channel.</summary>
public static class Clipboard
{
    public const string KTextPlain = "text/plain";

    public static async Task SetData(ClipboardData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        await SystemChannels.Platform.InvokeMethod<object>(
            "Clipboard.setData",
            new Dictionary<string, object?> { ["text"] = data.Text });
    }

    public static async Task<ClipboardData?> GetData(string format)
    {
        object? result = await SystemChannels.Platform.InvokeMethod<object>("Clipboard.getData", format);
        if (result is null)
        {
            return null;
        }

        IDictionary map = (IDictionary)result;
        return map["text"] is string text
            ? new ClipboardData(text)
            : throw new InvalidCastException("Clipboard data must contain text.");
    }

    public static async Task<bool> HasStrings()
    {
        object? result = await SystemChannels.Platform.InvokeMethod<object>("Clipboard.hasStrings", KTextPlain);
        if (result is null)
        {
            return false;
        }

        IDictionary map = (IDictionary)result;
        return map["value"] is bool value
            ? value
            : throw new InvalidCastException("Clipboard.hasStrings must return a boolean value.");
    }
}
