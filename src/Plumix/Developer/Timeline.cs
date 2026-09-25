using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Plumix.Foundation;

// C#-only infrastructure: the subset of `dart:developer` the framework records timeline events with
// (`Timeline`, `TimelineTask`, `Flow`). Dart hands these events to the VM's timeline recorder; .NET's
// counterpart is an `EventSource`, so the events go to `TimelineEventSource` ("Plumix-Timeline"),
// which `dotnet-trace`, PerfView or an in-process `EventListener` can record. As in Dart, nothing is
// recorded in a release build, and a sync block that starts while no listener is attached is not
// reported even if one attaches before it finishes.

namespace Plumix.Developer;

/// <summary>
/// A flow event: links timeline events that are logically related but run at different times, for
/// example the scheduling and the running of a task.
/// </summary>
/// <remarks><c>dart:developer</c>'s <c>Flow</c>.</remarks>
public sealed class Flow
{
    // These values must be kept in sync with the enum "EventType" in runtime/vm/timeline.h.
    internal const int BeginType = 9;
    internal const int StepType = 10;
    internal const int EndType = 11;

    private Flow(int type, long id)
    {
        Type = type;
        Id = id;
    }

    /// <summary>The flow id; pass it to <see cref="Step"/> and <see cref="End"/>.</summary>
    public long Id { get; }

    internal int Type { get; }

    /// <summary>
    /// A "begin" flow event. When <paramref name="id"/> is null a fresh id is allocated.
    /// </summary>
    public static Flow Begin(long? id = null) => new(BeginType, id ?? Timeline.GetNextTaskId());

    /// <summary>A "step" flow event, continuing the flow with the given id.</summary>
    public static Flow Step(long id) => new(StepType, id);

    /// <summary>An "end" flow event, closing the flow with the given id.</summary>
    public static Flow End(long id) => new(EndType, id);
}

/// <summary>Records synchronous timeline events.</summary>
/// <remarks>
/// <c>dart:developer</c>'s <c>Timeline</c>. Dart keeps one block stack per isolate; a Plumix process
/// may drive the framework from more than one thread (tests, hosts), so the stack is per thread.
/// </remarks>
public static class Timeline
{
    [ThreadStatic]
    private static List<SyncBlock?>? _stack;

    private static long _nextTaskId;

    private static List<SyncBlock?> Stack => _stack ??= [];

    /// <summary>The current time on the timeline clock, in microseconds.</summary>
    public static long Now => (long)(Stopwatch.GetTimestamp() * (1000000.0 / Stopwatch.Frequency));

    /// <summary>
    /// Starts a synchronous operation labeled <paramref name="name"/>. It lasts until the matching
    /// <see cref="FinishSync"/>; operations nest.
    /// </summary>
    public static void StartSync(string name, IReadOnlyDictionary<string, object?>? arguments = null, Flow? flow = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!HasTimeline)
        {
            return;
        }

        if (!TimelineEventSource.Log.IsEnabled())
        {
            // Push a null onto the stack so FinishSync stays balanced.
            Stack.Add(null);
            return;
        }

        var block = new SyncBlock(name, GetNextTaskId(), arguments, flow);
        Stack.Add(block);
        TimelineEventSource.Log.SyncBegin(block.TaskId, name, TimelineEventSource.ArgumentsAsJson(arguments));
    }

    /// <summary>Finishes the innermost synchronous operation started with <see cref="StartSync"/>.</summary>
    /// <exception cref="InvalidOperationException">No operation is pending.</exception>
    public static void FinishSync()
    {
        if (!HasTimeline)
        {
            return;
        }

        List<SyncBlock?> stack = Stack;
        if (stack.Count == 0)
        {
            throw new InvalidOperationException("Uneven calls to startSync and finishSync");
        }

        SyncBlock? block = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        if (block is null)
        {
            return;
        }

        TimelineEventSource.Log.SyncEnd(block.TaskId, block.Name);
        if (block.Flow is not null)
        {
            TimelineEventSource.Log.FlowEvent(block.Flow.Id, block.Flow.Type, block.TaskId, block.Name);
        }
    }

    /// <summary>Emits an instant event labeled <paramref name="name"/>.</summary>
    public static void InstantSync(string name, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!HasTimeline || !TimelineEventSource.Log.IsEnabled())
        {
            return;
        }

        TimelineEventSource.Log.Instant(name, TimelineEventSource.ArgumentsAsJson(arguments));
    }

    /// <summary>
    /// Runs <paramref name="function"/> inside a <see cref="StartSync"/>/<see cref="FinishSync"/>
    /// pair and returns its result.
    /// </summary>
    public static T TimeSync<T>(
        string name,
        Func<T> function,
        IReadOnlyDictionary<string, object?>? arguments = null,
        Flow? flow = null)
    {
        ArgumentNullException.ThrowIfNull(function);
        StartSync(name, arguments, flow);
        try
        {
            return function();
        }
        finally
        {
            FinishSync();
        }
    }

    /// <summary>
    /// <see cref="TimeSync{T}"/> for a function without a result (Dart's <c>TimelineSyncFunction&lt;void&gt;</c>).
    /// </summary>
    public static void TimeSync(
        string name,
        Action function,
        IReadOnlyDictionary<string, object?>? arguments = null,
        Flow? flow = null)
    {
        ArgumentNullException.ThrowIfNull(function);
        StartSync(name, arguments, flow);
        try
        {
            function();
        }
        finally
        {
            FinishSync();
        }
    }

    // C#-only test hook: a Dart test file runs in a fresh isolate; here a test that threw inside a
    // block leaves it on this thread's stack.
    internal static void ResetForTests() => _stack?.Clear();

    // Dart's `_hasTimeline`: the timeline is compiled out of a product (release) build.
    internal static bool HasTimeline => !Constants.KReleaseMode;

    internal static long GetNextTaskId() => Interlocked.Increment(ref _nextTaskId);

    private sealed class SyncBlock(
        string name,
        long taskId,
        IReadOnlyDictionary<string, object?>? arguments,
        Flow? flow)
    {
        public string Name { get; } = name;

        public long TaskId { get; } = taskId;

        public IReadOnlyDictionary<string, object?>? Arguments { get; } = arguments;

        public Flow? Flow { get; } = flow;
    }
}

/// <summary>
/// An asynchronous task on the timeline: its operations may start and finish in different stack
/// frames (and different frames of the app).
/// </summary>
/// <remarks><c>dart:developer</c>'s <c>TimelineTask</c>.</remarks>
public sealed class TimelineTask
{
    /// <summary>
    /// The argument key <see cref="TimelineTask(TimelineTask?, string?)"/> reports its filter key under.
    /// </summary>
    public const string FilterKey = "filterKey";

    private readonly TimelineTask? _parent;
    private readonly string? _filterKey;
    private readonly long _taskId;
    private readonly List<AsyncBlock> _stack = [];

    /// <summary>
    /// Creates a task. A <paramref name="parent"/> links this task under another one; a
    /// <paramref name="filterKey"/> is reported with every event so tools can filter on it.
    /// </summary>
    public TimelineTask(TimelineTask? parent = null, string? filterKey = null)
    {
        _parent = parent;
        _filterKey = filterKey;
        _taskId = Timeline.GetNextTaskId();
    }

    private TimelineTask(long taskId, string? filterKey)
    {
        _filterKey = filterKey;
        _taskId = taskId;
    }

    /// <summary>
    /// Recreates a task from an id handed out by <see cref="Pass"/>, typically in another isolate
    /// (here: another thread).
    /// </summary>
    public static TimelineTask WithTaskId(long taskId, string? filterKey = null) => new(taskId, filterKey);

    /// <summary>Starts an operation labeled <paramref name="name"/> in this task; operations nest.</summary>
    public void Start(string name, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Timeline.HasTimeline)
        {
            return;
        }

        var block = new AsyncBlock(name, _taskId);
        _stack.Add(block);
        var instantArguments = new Dictionary<string, object?>();
        if (arguments is not null)
        {
            foreach ((string key, object? value) in arguments)
            {
                instantArguments[key] = value;
            }
        }

        if (_filterKey is not null)
        {
            instantArguments[FilterKey] = _filterKey;
        }

        if (_parent is not null)
        {
            instantArguments["parentId"] = _parent._taskId.ToString("x", CultureInfo.InvariantCulture);
        }

        if (TimelineEventSource.Log.IsEnabled())
        {
            TimelineEventSource.Log.AsyncBegin(_taskId, name, TimelineEventSource.ArgumentsAsJson(instantArguments));
        }
    }

    /// <summary>Emits an instant event labeled <paramref name="name"/> in this task.</summary>
    public void Instant(string name, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Timeline.HasTimeline || !TimelineEventSource.Log.IsEnabled())
        {
            return;
        }

        Dictionary<string, object?>? instantArguments = null;
        if (arguments is not null || _filterKey is not null)
        {
            instantArguments = [];
            if (arguments is not null)
            {
                foreach ((string key, object? value) in arguments)
                {
                    instantArguments[key] = value;
                }
            }

            if (_filterKey is not null)
            {
                instantArguments[FilterKey] = _filterKey;
            }
        }

        TimelineEventSource.Log.AsyncInstant(_taskId, name, TimelineEventSource.ArgumentsAsJson(instantArguments));
    }

    /// <summary>Finishes the innermost operation started with <see cref="Start"/>.</summary>
    /// <exception cref="InvalidOperationException">No operation is pending.</exception>
    public void Finish(IReadOnlyDictionary<string, object?>? arguments = null)
    {
        if (!Timeline.HasTimeline)
        {
            return;
        }

        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Uneven calls to start and finish");
        }

        Dictionary<string, object?>? finishArguments = null;
        if (_filterKey is not null)
        {
            finishArguments = new Dictionary<string, object?> { [FilterKey] = _filterKey };
        }

        if (arguments is not null)
        {
            finishArguments ??= [];
            foreach ((string key, object? value) in arguments)
            {
                finishArguments[key] = value;
            }
        }

        AsyncBlock block = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        if (TimelineEventSource.Log.IsEnabled())
        {
            TimelineEventSource.Log.AsyncEnd(
                block.TaskId,
                block.Name,
                TimelineEventSource.ArgumentsAsJson(finishArguments));
        }
    }

    /// <summary>
    /// Returns this task's id so another thread can continue it through <see cref="WithTaskId"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">An operation of this task is still pending.</exception>
    public long Pass()
    {
        if (_stack.Count > 0)
        {
            throw new InvalidOperationException(
                "You cannot pass a TimelineTask without finishing all started operations");
        }

        return _taskId;
    }

    private sealed record AsyncBlock(string Name, long TaskId);
}

/// <summary>
/// The .NET sink for <see cref="Timeline"/> and <see cref="TimelineTask"/> events — the counterpart
/// of the Dart VM's timeline recorder.
/// </summary>
[EventSource(Name = "Plumix-Timeline")]
public sealed class TimelineEventSource : EventSource
{
    /// <summary>The event ids, for listeners that switch on <see cref="EventWrittenEventArgs.EventId"/>.</summary>
    public const int SyncBeginId = 1, SyncEndId = 2, InstantId = 3, AsyncBeginId = 4, AsyncInstantId = 5,
        AsyncEndId = 6, FlowEventId = 7;

    /// <summary>The single instance every timeline event is written to.</summary>
    public static readonly TimelineEventSource Log = new();

    private TimelineEventSource()
    {
    }

    /// <summary>A <see cref="Timeline.StartSync"/> block began.</summary>
    [Event(SyncBeginId, Level = EventLevel.Informational)]
    public void SyncBegin(long taskId, string name, string arguments) =>
        WriteEvent(SyncBeginId, taskId, name, arguments);

    /// <summary>A <see cref="Timeline.StartSync"/> block finished.</summary>
    [Event(SyncEndId, Level = EventLevel.Informational)]
    public void SyncEnd(long taskId, string name) => WriteEvent(SyncEndId, taskId, name);

    /// <summary>A <see cref="Timeline.InstantSync"/> event.</summary>
    [Event(InstantId, Level = EventLevel.Informational)]
    public void Instant(string name, string arguments) => WriteEvent(InstantId, name, arguments);

    /// <summary>A <see cref="TimelineTask.Start"/> operation began.</summary>
    [Event(AsyncBeginId, Level = EventLevel.Informational)]
    public void AsyncBegin(long taskId, string name, string arguments) =>
        WriteEvent(AsyncBeginId, taskId, name, arguments);

    /// <summary>A <see cref="TimelineTask.Instant"/> event.</summary>
    [Event(AsyncInstantId, Level = EventLevel.Informational)]
    public void AsyncInstant(long taskId, string name, string arguments) =>
        WriteEvent(AsyncInstantId, taskId, name, arguments);

    /// <summary>A <see cref="TimelineTask.Start"/> operation finished.</summary>
    [Event(AsyncEndId, Level = EventLevel.Informational)]
    public void AsyncEnd(long taskId, string name, string arguments) =>
        WriteEvent(AsyncEndId, taskId, name, arguments);

    /// <summary>
    /// A <see cref="Flow"/> event attached to the sync block <paramref name="taskId"/>;
    /// <paramref name="flowType"/> is 9 (begin), 10 (step) or 11 (end), as in the Dart VM.
    /// </summary>
    [Event(FlowEventId, Level = EventLevel.Informational)]
    public void FlowEvent(long flowId, int flowType, long taskId, string name) =>
        WriteEvent(FlowEventId, flowId, flowType, taskId, name);

    // Dart's `_argumentsAsJson`: a null map is the empty object; values are reported as strings.
    [NonEvent]
    internal static string ArgumentsAsJson(IReadOnlyDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "{}";
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach ((string key, object? value) in arguments)
            {
                if (value is null)
                {
                    writer.WriteNull(key);
                }
                else
                {
                    writer.WriteString(key, Convert.ToString(value, CultureInfo.InvariantCulture));
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
