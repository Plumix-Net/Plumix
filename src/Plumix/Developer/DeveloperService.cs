using Plumix.Foundation;

// C#-only infrastructure: the `postEvent` and `inspect` functions of `dart:developer`. Dart hands
// both to the VM service, which forwards them to a connected tool (DevTools, an IDE). .NET has no
// VM service, so the calls surface as in-process events a tool bridge (or a test) can subscribe to.

namespace Plumix.Developer;

/// <summary>The <c>postEvent</c> and <c>inspect</c> functions of <c>dart:developer</c>.</summary>
public static class DeveloperService
{
    /// <summary>The stream <see cref="PostEvent"/> posts to by default.</summary>
    public const string ExtensionStream = "Extension";

    /// <summary>Raised with the stream, the event kind and the data of every posted event.</summary>
    public static event Action<string, string, IReadOnlyDictionary<string, object?>>? EventPosted;

    /// <summary>Raised with every object passed to <see cref="Inspect"/>.</summary>
    public static event Action<object?>? ObjectInspected;

    /// <summary>
    /// Posts an event of <paramref name="eventKind"/> with <paramref name="eventData"/> to
    /// <paramref name="stream"/>.
    /// </summary>
    /// <remarks>
    /// <c>dart:developer</c>'s <c>postEvent(eventKind, eventData, {stream = 'Extension'})</c>. Events on
    /// the <c>Extension</c> stream are also raised through <see cref="BindingBase.EventPosted"/>.
    /// </remarks>
    public static void PostEvent(
        string eventKind,
        IReadOnlyDictionary<string, object?> eventData,
        string stream = ExtensionStream)
    {
        ArgumentNullException.ThrowIfNull(eventKind);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(stream);
        EventPosted?.Invoke(stream, eventKind, eventData);
        if (stream == ExtensionStream)
        {
            BindingBase.RaiseEventPosted(eventKind, eventData);
        }
    }

    /// <summary>Sends <paramref name="object"/> to a connected tool's object inspector.</summary>
    /// <remarks><c>dart:developer</c>'s <c>inspect</c>. Returns its argument, as Dart does.</remarks>
    public static object? Inspect(object? @object)
    {
        ObjectInspected?.Invoke(@object);
        return @object;
    }

    /// <summary>Drops every listener, for a test that subscribes.</summary>
    internal static void ResetForTests()
    {
        EventPosted = null;
        ObjectInspected = null;
    }
}
