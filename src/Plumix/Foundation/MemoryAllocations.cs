// Dart parity source: flutter/packages/flutter/lib/src/foundation/memory_allocations.dart

namespace Plumix.Foundation;

/// <summary>A lifecycle event of an object tracked by <see cref="FlutterMemoryAllocations"/>.</summary>
/// <remarks>Dart's <c>ObjectEvent</c>.</remarks>
public abstract class ObjectEvent
{
    /// <summary>Creates an event for <paramref name="object"/>.</summary>
    protected ObjectEvent(object @object)
    {
        ArgumentNullException.ThrowIfNull(@object);
        Object = @object;
    }

    /// <summary>The object the event is about.</summary>
    public object Object { get; }

    /// <summary>The event as the map a memory tool expects.</summary>
    public abstract Dictionary<object, Dictionary<string, object>> ToMap();
}

/// <summary>A callback invoked for every <see cref="ObjectEvent"/>.</summary>
/// <remarks>Dart's <c>ObjectEventListener</c>.</remarks>
public delegate void ObjectEventListener(ObjectEvent @event);

/// <summary>An event reporting that an object was created.</summary>
/// <remarks>Dart's <c>ObjectCreated</c>.</remarks>
public sealed class ObjectCreated : ObjectEvent
{
    /// <summary>Creates the event.</summary>
    public ObjectCreated(string library, string className, object @object) : base(@object)
    {
        Library = library;
        ClassName = className;
    }

    /// <summary>The name of the library the object's class is defined in.</summary>
    public string Library { get; }

    /// <summary>The name of the object's class.</summary>
    public string ClassName { get; }

    /// <inheritdoc />
    public override Dictionary<object, Dictionary<string, object>> ToMap()
    {
        return new Dictionary<object, Dictionary<string, object>>
        {
            [Object] = new()
            {
                [FieldNames.LibraryName] = Library,
                [FieldNames.ClassName] = ClassName,
                [FieldNames.EventType] = "created",
            },
        };
    }
}

/// <summary>An event reporting that an object was disposed.</summary>
/// <remarks>Dart's <c>ObjectDisposed</c>.</remarks>
public sealed class ObjectDisposed : ObjectEvent
{
    /// <summary>Creates the event.</summary>
    public ObjectDisposed(object @object) : base(@object)
    {
    }

    /// <inheritdoc />
    public override Dictionary<object, Dictionary<string, object>> ToMap()
    {
        return new Dictionary<object, Dictionary<string, object>>
        {
            [Object] = new() { [FieldNames.EventType] = "disposed" },
        };
    }
}

/// <summary>Dart's private <c>_FieldNames</c>.</summary>
internal static class FieldNames
{
    public const string EventType = "eventType";
    public const string LibraryName = "libraryName";
    public const string ClassName = "className";
}

/// <summary>
/// An interface for listening to object lifecycle events, used by memory tooling to detect leaks.
/// </summary>
/// <remarks>
/// Dart's <c>FlutterMemoryAllocations</c>. The <c>dart:ui</c> <c>Image</c>/<c>Picture</c>
/// subscription (<c>_subscribeToSdkObjects</c>) has no counterpart: the drawing backend exposes no
/// creation or disposal hooks for its images and pictures.
/// </remarks>
public sealed class FlutterMemoryAllocations
{
    /// <summary>
    /// Whether object lifecycle events are dispatched. Dart's <c>kFlutterMemoryAllocationsEnabled</c>,
    /// which is <c>true</c> in debug builds (the <c>flutter.memory_allocations</c> environment flag has
    /// no .NET counterpart).
    /// </summary>
    public static bool KFlutterMemoryAllocationsEnabled => Constants.KDebugMode;

    private List<ObjectEventListener?>? _listeners;
    private int _activeDispatchLoops;
    private bool _listenersContainNulls;

    private FlutterMemoryAllocations()
    {
    }

    /// <summary>The shared instance.</summary>
    public static FlutterMemoryAllocations Instance { get; } = new();

    /// <summary>Registers <paramref name="listener"/> for object events.</summary>
    public void AddListener(ObjectEventListener listener)
    {
        if (!KFlutterMemoryAllocationsEnabled)
        {
            return;
        }

        _listeners ??= [];
        _listeners.Add(listener);
    }

    /// <summary>
    /// Stops <paramref name="listener"/> from receiving events. Safe to call during a dispatch, which
    /// keeps iterating the list it started with.
    /// </summary>
    public void RemoveListener(ObjectEventListener listener)
    {
        if (!KFlutterMemoryAllocationsEnabled)
        {
            return;
        }

        List<ObjectEventListener?>? listeners = _listeners;
        if (listeners is null)
        {
            return;
        }

        if (_activeDispatchLoops > 0)
        {
            for (int i = 0; i < listeners.Count; i++)
            {
                if (listeners[i] == listener)
                {
                    listeners[i] = null;
                    _listenersContainNulls = true;
                }
            }
        }
        else
        {
            listeners.RemoveAll(l => l == listener);
            CheckListenersForEmptiness();
        }
    }

    private void TryDefragmentListeners()
    {
        if (_activeDispatchLoops > 0 || !_listenersContainNulls)
        {
            return;
        }

        _listeners?.RemoveAll(l => l is null);
        _listenersContainNulls = false;
        CheckListenersForEmptiness();
    }

    private void CheckListenersForEmptiness()
    {
        if (_listeners is { Count: 0 })
        {
            _listeners = null;
        }
    }

    /// <summary>Whether any listener is registered.</summary>
    public bool HasListeners
    {
        get
        {
            if (!KFlutterMemoryAllocationsEnabled)
            {
                return false;
            }

            if (_listenersContainNulls)
            {
                return _listeners?.Find(l => l is not null) is not null;
            }

            return _listeners is { Count: > 0 };
        }
    }

    /// <summary>
    /// Sends <paramref name="event"/> to every listener. A throwing listener is reported and does not
    /// stop the others.
    /// </summary>
    public void DispatchObjectEvent(ObjectEvent @event)
    {
        if (!KFlutterMemoryAllocationsEnabled)
        {
            return;
        }

        List<ObjectEventListener?>? listeners = _listeners;
        if (listeners is null || listeners.Count == 0)
        {
            return;
        }

        _activeDispatchLoops++;
        int end = listeners.Count;
        for (int i = 0; i < end; i++)
        {
            try
            {
                listeners[i]?.Invoke(@event);
            }
            catch (Exception exception)
            {
                string type = Diagnostics.DescribeType(@event.Object.GetType());
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "foundation library",
                    context: new ErrorDescription($"MemoryAllocations while dispatching notifications for {type}"),
                    informationCollector: () =>
                    [
                        new DiagnosticsProperty<object>(
                            $"The {type} sending notification was",
                            @event.Object,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                    ]));
            }
        }

        _activeDispatchLoops--;
        TryDefragmentListeners();
    }

    /// <summary>Dispatches an <see cref="ObjectCreated"/> event, if anyone is listening.</summary>
    public void DispatchObjectCreated(string library, string className, object @object)
    {
        if (!HasListeners)
        {
            return;
        }

        DispatchObjectEvent(new ObjectCreated(library, className, @object));
    }

    /// <summary>Dispatches an <see cref="ObjectDisposed"/> event, if anyone is listening.</summary>
    public void DispatchObjectDisposed(object @object)
    {
        if (!HasListeners)
        {
            return;
        }

        DispatchObjectEvent(new ObjectDisposed(@object));
    }
}
