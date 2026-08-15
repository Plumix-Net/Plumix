using System.Collections;
using System.Text;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/restoration.dart

namespace Plumix.UI;

/// <summary>
/// Manages the restoration data in the framework and synchronizes it with the host.
/// </summary>
/// <remarks>
/// Like Flutter, the manager talks to the host over the <c>flutter/restoration</c>
/// <see cref="OptionalMethodChannel"/> and encodes the data with <see cref="StandardMessageCodec"/>.
/// A host that ships no restoration handler answers nothing, which is exactly Flutter's behavior on a
/// platform without the plugin: restoration is reported as disabled.
/// </remarks>
public class RestorationManager : ChangeNotifier
{
    private static readonly StandardMessageCodec RestorationCodec = new StandardMessageCodec();

    private readonly HashSet<RestorationBucket> _bucketsNeedingSerialization = [];

    private RestorationBucket? _rootBucket;
    private List<Action<RestorationBucket?>>? _pendingRootBucketCallbacks;
    private bool _rootBucketIsValid;
    private bool _isReplacing;
    private bool _debugDoingUpdate;
    private bool _serializationScheduled;

    public RestorationManager()
    {
        InitChannels();
    }

    /// <summary>
    /// The ambient manager used by <see cref="Plumix.Widgets.RootRestorationScope"/>.
    /// </summary>
    /// <remarks>
    /// Dart parity source: <c>ServicesBinding.instance.restorationManager</c>. Plumix has no binding
    /// singleton, so hosts and tests substitute their own manager here.
    /// </remarks>
    public static RestorationManager Instance { get; set; } = new RestorationManager();

    /// <summary>
    /// Whether the framework is currently restoring into a new set of data provided by the host.
    /// </summary>
    public bool IsReplacing => _isReplacing;

    /// <summary>Installs the <c>flutter/restoration</c> method-call handler.</summary>
    protected virtual void InitChannels()
    {
        SystemChannels.Restoration.SetMethodCallHandler(HandleMethodCall);
    }

    private Task<object?> HandleMethodCall(MethodCall call)
    {
        switch (call.Method)
        {
            case "push":
                ParseAndHandleRestorationUpdateFromEngine(call.Arguments);
                return Task.FromResult<object?>(null);
            default:
                throw new NotImplementedException(
                    $"{call.Method} was invoked but isn't implemented by {GetType().Name}.");
        }
    }

    private void ParseAndHandleRestorationUpdateFromEngine(object? update)
    {
        var config = update as IDictionary<object, object?>;
        bool enabled = config is not null
            && config.TryGetValue("enabled", out object? enabledValue)
            && enabledValue is true;
        byte[]? data = null;
        if (config is not null && config.TryGetValue("data", out object? rawData))
        {
            data = rawData as byte[];
        }

        HandleRestorationUpdateFromEngine(enabled, data);
    }

    /// <summary>
    /// Delivers the root bucket. Dart parity source: <c>Future&lt;RestorationBucket?&gt; get rootBucket</c>.
    /// C# has no synchronous future, so the bucket is handed to <paramref name="callback"/>, which runs
    /// synchronously whenever the data is already available.
    /// </summary>
    public virtual void GetRootBucket(Action<RestorationBucket?> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_rootBucketIsValid)
        {
            callback(_rootBucket);
            return;
        }

        bool requestPending = _pendingRootBucketCallbacks is not null;
        (_pendingRootBucketCallbacks ??= []).Add(callback);
        if (!requestPending)
        {
            GetRootBucketFromEngine();
        }
    }

    /// <summary>
    /// Asks the host for the restoration data over the <c>flutter/restoration</c> channel.
    /// Implementations must eventually call <see cref="HandleRestorationUpdateFromEngine"/>; a host
    /// without a restoration channel answers <c>null</c>, which reports restoration as disabled.
    /// </summary>
    protected virtual void GetRootBucketFromEngine()
    {
        Task<object?> pending = SystemChannels.Restoration.InvokeMethod<object>("get");
        if (pending.IsCompletedSuccessfully)
        {
            // Dart's `SynchronousFuture` equivalent: a host that answers inline keeps the root bucket
            // available in the same turn, which is what every restoration test relies on.
            ParseAndHandleRestorationUpdateFromEngine(pending.Result);
            return;
        }

        _ = AwaitRootBucketFromEngine(pending);
    }

    private async Task AwaitRootBucketFromEngine(Task<object?> pending)
    {
        object? update = await pending.ConfigureAwait(false);
        ParseAndHandleRestorationUpdateFromEngine(update);
    }

    /// <summary>Called by the host when it has new restoration data for the framework.</summary>
    /// <param name="enabled">Whether the host supports state restoration.</param>
    /// <param name="data">The restoration data, encoded with <see cref="StandardMessageCodec"/>.</param>
    public void HandleRestorationUpdateFromEngine(bool enabled, byte[]? data)
    {
        if (!enabled && data is not null)
        {
            throw new ArgumentException("Restoration data cannot be provided while restoration is disabled.");
        }

        _isReplacing = _rootBucketIsValid && enabled;
        if (_isReplacing)
        {
            Scheduler.AddPostFrameCallback(_ => _isReplacing = false, scheduleFrame: false);
        }

        RestorationBucket? oldRoot = _rootBucket;
        _rootBucket = enabled
            ? RestorationBucket.Root(this, DecodeRestorationData(data))
            : null;
        _rootBucketIsValid = true;

        List<Action<RestorationBucket?>>? pending = _pendingRootBucketCallbacks;
        _pendingRootBucketCallbacks = null;

        if (!ReferenceEquals(_rootBucket, oldRoot))
        {
            NotifyListeners();
            oldRoot?.Dispose();
        }

        // Dart completes the pending `rootBucket` future here; its continuations run in a microtask,
        // after the listeners above, so the callbacks are drained last.
        if (pending is not null)
        {
            foreach (Action<RestorationBucket?> callback in pending)
            {
                callback(_rootBucket);
            }
        }
    }

    /// <summary>Hands the serialized restoration data to the host over the
    /// <c>flutter/restoration</c> channel.</summary>
    protected virtual void SendToEngine(byte[] encodedData)
    {
        _ = SystemChannels.Restoration.InvokeMethod<object>("put", encodedData);
    }

    /// <summary>Decodes restoration data received from the host.</summary>
    protected static IDictionary<object, object?>? DecodeRestorationData(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        return RestorationCodec.DecodeMessage(ByteData.SublistView(data)) as IDictionary<object, object?>;
    }

    /// <summary>Encodes restoration data for the host.</summary>
    protected static byte[] EncodeRestorationData(IDictionary<object, object?> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return RestorationCodec.EncodeMessage(data)!.ToUint8List();
    }

    /// <summary>Schedules <paramref name="bucket"/> to be finalized before the next data send.</summary>
    public virtual void ScheduleSerializationFor(RestorationBucket bucket)
    {
        ArgumentNullException.ThrowIfNull(bucket);
        if (!ReferenceEquals(bucket.Manager, this))
        {
            throw new InvalidOperationException("The bucket is not managed by this RestorationManager.");
        }

        AssertNotDoingUpdate();
        _bucketsNeedingSerialization.Add(bucket);
        if (_serializationScheduled)
        {
            return;
        }

        _serializationScheduled = true;
        Scheduler.AddPostFrameCallback(_ => DoSerialization(), scheduleFrame: false);
    }

    /// <summary>Cancels a pending serialization scheduled for <paramref name="bucket"/>.</summary>
    public virtual void UnscheduleSerializationFor(RestorationBucket bucket)
    {
        ArgumentNullException.ThrowIfNull(bucket);
        if (!ReferenceEquals(bucket.Manager, this))
        {
            throw new InvalidOperationException("The bucket is not managed by this RestorationManager.");
        }

        AssertNotDoingUpdate();
        _bucketsNeedingSerialization.Remove(bucket);
    }

    /// <summary>Sends any pending restoration data to the host immediately, unless a frame is pending.</summary>
    public void FlushData()
    {
        AssertNotDoingUpdate();
        if (Scheduler.HasScheduledFrame)
        {
            return;
        }

        DoSerialization();
    }

    public override void Dispose()
    {
        _rootBucket?.Dispose();
        base.Dispose();
    }

    private void DoSerialization()
    {
        if (!_serializationScheduled)
        {
            return;
        }

        _debugDoingUpdate = true;
        _serializationScheduled = false;
        foreach (RestorationBucket bucket in _bucketsNeedingSerialization)
        {
            bucket.FinalizeBucket();
        }

        _bucketsNeedingSerialization.Clear();
        SendToEngine(EncodeRestorationData(_rootBucket!.RawData));
        _debugDoingUpdate = false;
    }

    private void AssertNotDoingUpdate()
    {
        if (_debugDoingUpdate)
        {
            throw new InvalidOperationException(
                "The RestorationManager is currently serializing its data and cannot be modified.");
        }
    }
}

/// <summary>
/// A piece of restoration data that a <see cref="Plumix.Widgets.RestorableProperty{T}"/> or a child
/// bucket can be stored in.
/// </summary>
public class RestorationBucket
{
    private const string ChildrenMapKey = "c";
    private const string ValuesMapKey = "v";

    private readonly Dictionary<object, object?> _rawData;
    private readonly Dictionary<string, RestorationBucket> _claimedChildren = [];
    private readonly Dictionary<string, List<RestorationBucket>> _childrenToAdd = [];

    private object? _debugOwner;
    private RestorationManager? _manager;
    private RestorationBucket? _parent;
    private string _restorationId;
    private bool _needsSerialization;
    private bool _disposed;

    private RestorationBucket(
        string restorationId,
        Dictionary<object, object?> rawData,
        RestorationManager? manager,
        RestorationBucket? parent,
        object? debugOwner)
    {
        _restorationId = restorationId;
        _rawData = rawData;
        _manager = manager;
        _parent = parent;
        _debugOwner = debugOwner;
    }

    /// <summary>Creates an empty bucket that is not attached to any parent yet.</summary>
    public static RestorationBucket Empty(string restorationId, object? debugOwner)
    {
        ArgumentNullException.ThrowIfNull(restorationId);
        return new RestorationBucket(restorationId, [], manager: null, parent: null, debugOwner: debugOwner);
    }

    /// <summary>Creates the root bucket owned by <paramref name="manager"/>.</summary>
    public static RestorationBucket Root(RestorationManager manager, IDictionary<object, object?>? rawData)
    {
        ArgumentNullException.ThrowIfNull(manager);
        Dictionary<object, object?> data = rawData as Dictionary<object, object?>
            ?? (rawData is null ? [] : new Dictionary<object, object?>(rawData));
        return new RestorationBucket("root", data, manager, parent: null, debugOwner: manager);
    }

    /// <summary>Creates a bucket backed by the raw child data already stored in <paramref name="parent"/>.</summary>
    public static RestorationBucket Child(string restorationId, RestorationBucket parent, object? debugOwner)
    {
        ArgumentNullException.ThrowIfNull(restorationId);
        ArgumentNullException.ThrowIfNull(parent);
        if (parent.RawChildren.GetValueOrDefault(restorationId) is not Dictionary<object, object?> rawData)
        {
            throw new InvalidOperationException(
                $"The parent bucket has no raw child data stored under \"{restorationId}\".");
        }

        return new RestorationBucket(restorationId, rawData, parent._manager, parent, debugOwner);
    }

    /// <summary>The object currently owning this bucket, used for debug output only.</summary>
    public object? DebugOwner
    {
        get
        {
            AssertNotDisposed();
            return _debugOwner;
        }
    }

    /// <summary>Whether the data in this bucket is being replaced by new data from the host.</summary>
    public bool IsReplacing => _manager?.IsReplacing ?? false;

    /// <summary>The id under which this bucket is stored in its parent.</summary>
    public string RestorationId
    {
        get
        {
            AssertNotDisposed();
            return _restorationId;
        }
    }

    internal RestorationManager? Manager => _manager;

    internal Dictionary<object, object?> RawData => _rawData;

    private Dictionary<object, object?> RawChildren
    {
        get
        {
            if (_rawData.GetValueOrDefault(ChildrenMapKey) is not Dictionary<object, object?> children)
            {
                children = [];
                _rawData[ChildrenMapKey] = children;
            }

            return children;
        }
    }

    private Dictionary<object, object?> RawValues
    {
        get
        {
            if (_rawData.GetValueOrDefault(ValuesMapKey) is not Dictionary<object, object?> values)
            {
                values = [];
                _rawData[ValuesMapKey] = values;
            }

            return values;
        }
    }

    /// <summary>Reads the value stored under <paramref name="restorationId"/>.</summary>
    public P? Read<P>(string restorationId)
    {
        AssertNotDisposed();
        object? value = RawValues.GetValueOrDefault(restorationId);
        return value is null ? default : (P)value;
    }

    /// <summary>Stores <paramref name="value"/> under <paramref name="restorationId"/>.</summary>
    public void Write<P>(string restorationId, P value)
    {
        AssertNotDisposed();
        RestorationSerialization.AssertSerializable(value);
        Dictionary<object, object?> values = RawValues;
        if (!values.TryGetValue(restorationId, out object? existing) || !Equals(existing, value))
        {
            values[restorationId] = value;
            MarkNeedsSerialization();
        }
    }

    /// <summary>Removes and returns the value stored under <paramref name="restorationId"/>.</summary>
    public P? Remove<P>(string restorationId)
    {
        AssertNotDisposed();
        Dictionary<object, object?> values = RawValues;
        bool needsUpdate = values.ContainsKey(restorationId);
        object? result = values.GetValueOrDefault(restorationId);
        values.Remove(restorationId);
        if (values.Count == 0)
        {
            _rawData.Remove(ValuesMapKey);
        }

        if (needsUpdate)
        {
            MarkNeedsSerialization();
        }

        return result is null ? default : (P)result;
    }

    /// <summary>Whether this bucket contains a value stored under <paramref name="restorationId"/>.</summary>
    public bool Contains(string restorationId)
    {
        AssertNotDisposed();
        return RawValues.ContainsKey(restorationId);
    }

    /// <summary>Claims ownership of the child bucket stored under <paramref name="restorationId"/>.</summary>
    public RestorationBucket ClaimChild(string restorationId, object? debugOwner)
    {
        AssertNotDisposed();
        ArgumentNullException.ThrowIfNull(restorationId);

        // Ensure that a child bucket is only claimed once: a second claimant gets a fresh bucket that
        // is added to the tree only when the current owner gives the id up.
        if (_claimedChildren.ContainsKey(restorationId) || !RawChildren.ContainsKey(restorationId))
        {
            var child = Empty(restorationId, debugOwner);
            AdoptChild(child);
            return child;
        }

        var existing = Child(restorationId, this, debugOwner);
        _claimedChildren[restorationId] = existing;
        return existing;
    }

    /// <summary>Makes this bucket the parent of <paramref name="child"/>.</summary>
    public void AdoptChild(RestorationBucket child)
    {
        AssertNotDisposed();
        ArgumentNullException.ThrowIfNull(child);

        if (!ReferenceEquals(child._parent, this))
        {
            child._parent?.RemoveChildData(child);
            child._parent = this;
            AddChildData(child);
            if (!ReferenceEquals(child._manager, _manager))
            {
                RecursivelyUpdateManager(child);
            }
        }
    }

    /// <summary>
    /// Called by the manager once the data of this bucket has been sent to the host.
    /// </summary>
    /// <remarks>Dart names this <c>finalize</c>; C# reserves <c>Finalize</c> for destructors.</remarks>
    public void FinalizeBucket()
    {
        AssertNotDisposed();
        if (!_needsSerialization)
        {
            throw new InvalidOperationException("The bucket does not need to be serialized.");
        }

        _needsSerialization = false;
        AssertIntegrity();
    }

    /// <summary>Changes the id under which this bucket is stored in its parent.</summary>
    public void Rename(string newRestorationId)
    {
        AssertNotDisposed();
        ArgumentNullException.ThrowIfNull(newRestorationId);
        if (string.Equals(newRestorationId, _restorationId, StringComparison.Ordinal))
        {
            return;
        }

        _parent?.RemoveChildData(this);
        _restorationId = newRestorationId;
        _parent?.AddChildData(this);
    }

    /// <summary>Deletes the bucket and its children from the restoration data.</summary>
    public void Dispose()
    {
        AssertNotDisposed();
        VisitChildren(DropChild, concurrentModification: true);
        _claimedChildren.Clear();
        _childrenToAdd.Clear();
        _parent?.RemoveChildData(this);
        _parent = null;
        UpdateManager(null);
        _disposed = true;
    }

    public override string ToString() => $"RestorationBucket(restorationId: {_restorationId}, owner: {_debugOwner})";

    private void DropChild(RestorationBucket child)
    {
        RemoveChildData(child);
        child._parent = null;
        if (child._manager is not null)
        {
            child.UpdateManager(null);
            child.VisitChildren(RecursivelyUpdateManager);
        }
    }

    private void MarkNeedsSerialization()
    {
        if (_needsSerialization)
        {
            return;
        }

        _needsSerialization = true;
        _manager?.ScheduleSerializationFor(this);
    }

    private void RecursivelyUpdateManager(RestorationBucket bucket)
    {
        bucket.UpdateManager(_manager);
        bucket.VisitChildren(RecursivelyUpdateManager);
    }

    private void UpdateManager(RestorationManager? newManager)
    {
        if (ReferenceEquals(_manager, newManager))
        {
            return;
        }

        if (_needsSerialization)
        {
            _manager?.UnscheduleSerializationFor(this);
        }

        _manager = newManager;
        if (_needsSerialization && _manager is not null)
        {
            _needsSerialization = false;
            MarkNeedsSerialization();
        }
    }

    private void AssertIntegrity()
    {
        if (_childrenToAdd.Count == 0)
        {
            return;
        }

        var message = new StringBuilder();
        message.AppendLine("Multiple owners claimed child RestorationBuckets with the same IDs.");
        message.AppendLine($"The following IDs were claimed multiple times from the parent {this}:");
        foreach ((string id, List<RestorationBucket> buckets) in _childrenToAdd)
        {
            message.AppendLine($" * \"{id}\" was claimed by:");
            foreach (RestorationBucket bucket in buckets)
            {
                message.AppendLine($"   * {bucket._debugOwner}");
            }

            message.AppendLine($"   * {_claimedChildren[id]._debugOwner} (current owner)");
        }

        throw new InvalidOperationException(message.ToString().TrimEnd());
    }

    private void RemoveChildData(RestorationBucket child)
    {
        if (_claimedChildren.TryGetValue(child._restorationId, out RestorationBucket? claimed)
            && ReferenceEquals(claimed, child))
        {
            _claimedChildren.Remove(child._restorationId);
            RawChildren.Remove(child._restorationId);
            if (_childrenToAdd.TryGetValue(child._restorationId, out List<RestorationBucket>? pendingChildren))
            {
                RestorationBucket toAdd = pendingChildren[^1];
                pendingChildren.RemoveAt(pendingChildren.Count - 1);
                FinalizeAddChildData(toAdd);
                if (pendingChildren.Count == 0)
                {
                    _childrenToAdd.Remove(child._restorationId);
                }
            }

            if (RawChildren.Count == 0)
            {
                _rawData.Remove(ChildrenMapKey);
            }

            MarkNeedsSerialization();
            return;
        }

        if (_childrenToAdd.TryGetValue(child._restorationId, out List<RestorationBucket>? pending))
        {
            pending.Remove(child);
            if (pending.Count == 0)
            {
                _childrenToAdd.Remove(child._restorationId);
            }
        }
    }

    private void AddChildData(RestorationBucket child)
    {
        if (_claimedChildren.ContainsKey(child._restorationId))
        {
            // The id is already claimed: the child waits until the current owner gives it up.
            if (!_childrenToAdd.TryGetValue(child._restorationId, out List<RestorationBucket>? pending))
            {
                pending = [];
                _childrenToAdd[child._restorationId] = pending;
            }

            pending.Add(child);
            MarkNeedsSerialization();
            return;
        }

        FinalizeAddChildData(child);
        MarkNeedsSerialization();
    }

    private void FinalizeAddChildData(RestorationBucket child)
    {
        _claimedChildren[child._restorationId] = child;
        RawChildren[child._restorationId] = child._rawData;
    }

    private void VisitChildren(Action<RestorationBucket> visitor, bool concurrentModification = false)
    {
        IEnumerable<RestorationBucket> children = _claimedChildren.Values
            .Concat(_childrenToAdd.Values.SelectMany(buckets => buckets));
        if (concurrentModification)
        {
            children = children.ToList();
        }

        foreach (RestorationBucket child in children)
        {
            visitor(child);
        }
    }

    private void AssertNotDisposed()
    {
        if (_disposed)
        {
            throw new InvalidOperationException(
                "A RestorationBucket was used after being disposed.\n"
                + "Once you have called dispose() on a RestorationBucket, it can no longer be used.");
        }
    }
}

/// <summary>
/// Helpers for the restoration data format.
/// </summary>
/// <remarks>
/// Flutter encodes restoration data with <c>StandardMessageCodec</c>. Plumix keeps the data as a
/// live object graph, so these helpers copy it across the host boundary and check the same value
/// domain the codec supports.
/// </remarks>
public static class RestorationSerialization
{
    /// <summary>
    /// Whether <paramref name="value"/> can be stored in a <see cref="RestorationBucket"/>.
    /// Dart parity source: <c>debugIsSerializableForRestoration</c>.
    /// </summary>
    public static bool DebugIsSerializableForRestoration(object? value)
    {
        switch (value)
        {
            case null:
            case bool:
            case int:
            case long:
            case double:
            case string:
            case byte[]:
            case int[]:
            case long[]:
            case double[]:
                return true;
            case IDictionary map:
                foreach (DictionaryEntry entry in map)
                {
                    if (!DebugIsSerializableForRestoration(entry.Key)
                        || !DebugIsSerializableForRestoration(entry.Value))
                    {
                        return false;
                    }
                }

                return true;
            case IEnumerable list:
                foreach (object? item in list)
                {
                    if (!DebugIsSerializableForRestoration(item))
                    {
                        return false;
                    }
                }

                return true;
            default:
                return false;
        }
    }

    /// <summary>Throws when <paramref name="value"/> cannot be stored in a bucket.</summary>
    public static void AssertSerializable(object? value)
    {
        if (!DebugIsSerializableForRestoration(value))
        {
            throw new ArgumentException(
                $"A value of type {value?.GetType().Name} cannot be stored in restoration data.",
                nameof(value));
        }
    }

    /// <summary>
    /// Deep-copies a restoration data map. Flutter gets a fresh tree out of the codec on every
    /// exchange with the host; Plumix copies instead so neither side aliases the other's maps.
    /// </summary>
    public static Dictionary<object, object?>? CopyRestorationData(IDictionary<object, object?>? data)
    {
        if (data is null)
        {
            return null;
        }

        var copy = new Dictionary<object, object?>(data.Count);
        foreach ((object key, object? value) in data)
        {
            copy[key] = value is IDictionary<object, object?> nested ? CopyRestorationData(nested) : value;
        }

        return copy;
    }

    /// <summary>Dart parity source: <c>DateTime.millisecondsSinceEpoch</c>.</summary>
    public static long MillisecondsSinceEpoch(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime())
            .ToUnixTimeMilliseconds();
    }

    /// <summary>Dart parity source: <c>DateTime.fromMillisecondsSinceEpoch</c> (local time).</summary>
    public static DateTime DateTimeFromMillisecondsSinceEpoch(long milliseconds)
    {
        return DateTime.SpecifyKind(
            DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime,
            DateTimeKind.Unspecified);
    }
}
