using System.Diagnostics;
using System.Globalization;
using Plumix.Developer;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/timeline.dart

namespace Plumix.Foundation;

/// <summary>
/// Measures how long blocks of code take to run.
/// </summary>
/// <remarks>
/// <para>
/// Dart's <c>FlutterTimeline</c>. This class's methods proxy to <see cref="Timeline"/>, so the
/// blocks show up on the timeline (<see cref="TimelineEventSource"/>). In addition, when
/// <see cref="DebugCollectionEnabled"/> is true, the timings are collected in memory and can be read
/// back with <see cref="DebugCollect"/>, which is what benchmarks use to aggregate them.
/// </para>
/// <para>
/// All methods in this class are no-ops in release mode, except <see cref="DebugCollectionEnabled"/>,
/// <see cref="DebugCollect"/> and <see cref="DebugReset"/>, which throw.
/// </para>
/// </remarks>
public static class FlutterTimeline
{
    private static BlockBuffer _buffer = new();
    private static bool _collectionEnabled;

    /// <summary>
    /// Whether block timings are collected and can be retrieved with <see cref="DebugCollect"/>.
    /// </summary>
    /// <remarks>
    /// Changing the value resets the collected timings (see <see cref="DebugReset"/>). This is a
    /// debug and profile feature; setting it in release mode throws.
    /// </remarks>
    public static bool DebugCollectionEnabled
    {
        get => _collectionEnabled;
        set
        {
            if (Constants.KReleaseMode)
            {
                throw CreateReleaseModeNotSupportedError();
            }

            if (value == _collectionEnabled)
            {
                return;
            }

            _collectionEnabled = value;
            DebugReset();
        }
    }

    /// <summary>
    /// The current time on the timeline clock, in microseconds; the clock
    /// <see cref="TimedBlock.Start"/> and <see cref="TimedBlock.End"/> are measured with.
    /// </summary>
    public static long Now => (long)PerformanceTimestamp;

    // Dart's `_timeline_io.dart` `performanceTimestamp`: `Timeline.now` as a double.
    internal static double PerformanceTimestamp => Timeline.Now;

    /// <summary>
    /// Starts a synchronous operation labeled <paramref name="name"/>; see <see cref="Timeline.StartSync"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="DebugCollectionEnabled"/> is true the block is also timed, and the timing is
    /// recorded when the matching <see cref="FinishSync"/> runs.
    /// </remarks>
    public static void StartSync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments = null,
        Flow? flow = null)
    {
        Timeline.StartSync(name, arguments, flow);
        if (!Constants.KReleaseMode && _collectionEnabled)
        {
            _buffer.StartSync(name, arguments, flow);
        }
    }

    /// <summary>Finishes the innermost operation started with <see cref="StartSync"/>.</summary>
    public static void FinishSync()
    {
        Timeline.FinishSync();
        if (!Constants.KReleaseMode && _collectionEnabled)
        {
            _buffer.FinishSync();
        }
    }

    /// <summary>
    /// Emits an instant event labeled <paramref name="name"/>; instants are not collected by
    /// <see cref="DebugCollect"/>.
    /// </summary>
    public static void InstantSync(string name, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        Timeline.InstantSync(name, arguments);
    }

    /// <summary>
    /// Runs <paramref name="function"/> between <see cref="StartSync"/> and <see cref="FinishSync"/>
    /// and returns its result.
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

    /// <summary><see cref="TimeSync{T}"/> for a function without a result.</summary>
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

    /// <summary>
    /// Returns the timings collected since collection was enabled or last reset, and resets them.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// In release mode, or when <see cref="DebugCollectionEnabled"/> is false.
    /// </exception>
    public static AggregatedTimings DebugCollect()
    {
        if (Constants.KReleaseMode)
        {
            throw CreateReleaseModeNotSupportedError();
        }

        if (!_collectionEnabled)
        {
            throw new InvalidOperationException("Timeline metric collection not enabled.");
        }

        var result = new AggregatedTimings(_buffer.ComputeTimings());
        DebugReset();
        return result;
    }

    /// <summary>Forgets all collected timings.</summary>
    /// <exception cref="InvalidOperationException">In release mode.</exception>
    public static void DebugReset()
    {
        if (Constants.KReleaseMode)
        {
            throw CreateReleaseModeNotSupportedError();
        }

        _buffer = new BlockBuffer();
    }

    // Dart's `_createReleaseModeNotSupportedError`, a `StateError`.
    private static InvalidOperationException CreateReleaseModeNotSupportedError()
    {
        return new InvalidOperationException(
            "FlutterTimeline metric collection not supported in release mode.");
    }

    // The number of elements in each list of a list chain.
    private const int SliceSize = 500;

    // A growable list of doubles made of fixed-size slices, so adding never copies.
    private sealed class DoubleListChain
    {
        private readonly List<double[]> _chain = [];
        private double[] _slice = new double[SliceSize];
        private int _pointer;

        public int Length { get; private set; }

        public void Add(double element)
        {
            _slice[_pointer] = element;
            _pointer += 1;
            Length += 1;
            if (_pointer >= SliceSize)
            {
                _chain.Add(_slice);
                _slice = new double[SliceSize];
                _pointer = 0;
            }
        }

        public List<double> ExtractElements()
        {
            var result = new List<double>(Length);
            foreach (double[] list in _chain)
            {
                result.AddRange(list);
            }

            for (int i = 0; i < _pointer; i++)
            {
                result.Add(_slice[i]);
            }

            return result;
        }
    }

    // A growable list of strings made of fixed-size slices, so adding never copies.
    private sealed class StringListChain
    {
        private readonly List<string?[]> _chain = [];
        private string?[] _slice = new string?[SliceSize];
        private int _pointer;

        public int Length { get; private set; }

        public void Add(string element)
        {
            _slice[_pointer] = element;
            _pointer += 1;
            Length += 1;
            if (_pointer >= SliceSize)
            {
                _chain.Add(_slice);
                _slice = new string?[SliceSize];
                _pointer = 0;
            }
        }

        public List<string> ExtractElements()
        {
            var result = new List<string>(Length);
            foreach (string?[] slice in _chain)
            {
                foreach (string? value in slice)
                {
                    result.Add(value!);
                }
            }

            for (int i = 0; i < _pointer; i++)
            {
                result.Add(_slice[i]!);
            }

            return result;
        }
    }

    // Keeps track of the start and finish times of timed blocks.
    private sealed class BlockBuffer
    {
        // Start-finish blocks can be nested. Track this nestedness by stacking the start timestamps.
        // Finish timestamps will pop timings from the stack and add the (start, finish) tuple to the
        // block.
        private const int StackDepth = 1000;
        private static readonly double[] _startStack = new double[StackDepth];
        private static readonly string?[] _nameStack = new string?[StackDepth];
        private static int _stackPointer;

        private readonly DoubleListChain _starts = new();
        private readonly DoubleListChain _finishes = new();
        private readonly StringListChain _names = new();

        public List<TimedBlock> ComputeTimings()
        {
            if (Constants.KDebugMode && _stackPointer != 0)
            {
                throw new AssertionError(
                    "Invalid sequence of `startSync` and `finishSync`.\n"
                    + "The operation stack was not empty. The following operations are still "
                    + "waiting to be finished via the `finishSync` method:\n"
                    + string.Join(", ", Enumerable.Range(0, _stackPointer).Select(i => _nameStack[i]!)));
            }

            var result = new List<TimedBlock>();
            int length = _finishes.Length;
            List<double> starts = _starts.ExtractElements();
            List<double> finishes = _finishes.ExtractElements();
            List<string> names = _names.ExtractElements();
            FlutterTimeline.DebugAssert(starts.Count == length);
            FlutterTimeline.DebugAssert(finishes.Count == length);
            FlutterTimeline.DebugAssert(names.Count == length);
            for (int i = 0; i < length; i++)
            {
                result.Add(new TimedBlock(start: starts[i], end: finishes[i], name: names[i]));
            }

            return result;
        }

        public void StartSync(string name, IReadOnlyDictionary<string, object?>? arguments, Flow? flow)
        {
            _startStack[_stackPointer] = PerformanceTimestamp;
            _nameStack[_stackPointer] = name;
            _stackPointer += 1;
        }

        public void FinishSync()
        {
            FlutterTimeline.DebugAssert(
                _stackPointer > 0,
                "Invalid sequence of `startSync` and `finishSync`.\n"
                + "Attempted to finish timing a block of code, but there are no pending "
                + "`startSync` calls.");
            double finishTime = PerformanceTimestamp;
            double startTime = _startStack[_stackPointer - 1];
            string name = _nameStack[_stackPointer - 1]!;
            _stackPointer -= 1;
            _starts.Add(startTime);
            _finishes.Add(finishTime);
            _names.Add(name);
        }

        // C#-only: Dart's stacks are static and a test can leave them unbalanced; the harness resets
        // them between tests.
        internal static void ResetStack() => _stackPointer = 0;
    }

    // Dart passes the `Map<String, String>` of `DiagnosticsNode.toTimelineArguments` straight to the
    // `Map<String, Object?>` parameter; C# dictionaries are invariant, so the call sites convert.
    internal static Dictionary<string, object?>? ToTimelineArguments(IReadOnlyDictionary<string, string>? arguments)
    {
        return arguments?.ToDictionary(static entry => entry.Key, static entry => (object?)entry.Value);
    }

    // Dart's `assert` for this file (the framework's `DebugAssertions` lives in the widgets layer).
    internal static void DebugAssert(bool condition, string? message = null)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }

    // C#-only test hook: restores the collection state a fresh isolate starts with.
    internal static void ResetForTests()
    {
        _collectionEnabled = false;
        _buffer = new BlockBuffer();
        BlockBuffer.ResetStack();
    }
}

/// <summary>
/// Provides the start, end, and duration of a block of code that was timed by
/// <see cref="FlutterTimeline"/>.
/// </summary>
/// <remarks>Dart's <c>TimedBlock</c>.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TimedBlock
{
    /// <summary>Creates a timed block of code from start and end timestamps (microseconds).</summary>
    public TimedBlock(string name, double start, double end)
    {
        FlutterTimeline.DebugAssert(end >= start, "The start timestamp must not be greater than the end timestamp.");
        Name = name;
        Start = start;
        End = end;
    }

    /// <summary>The name of the timeline event.</summary>
    public string Name { get; }

    /// <summary>The timestamp in microseconds that marks the beginning of the timed block.</summary>
    public double Start { get; }

    /// <summary>The timestamp in microseconds that marks the end of the timed block.</summary>
    public double End { get; }

    /// <summary>How long the block took, in microseconds.</summary>
    public double Duration => End - Start;

    /// <inheritdoc />
    public override string ToString() =>
        $"TimedBlock({Name}, {Dart(Start)}, {Dart(End)}, {Dart(Duration)})";

    internal static string Dart(double value) => BindingBase.DartDoubleToString(value);
}

/// <summary>Provides aggregated results for timings collected by <see cref="FlutterTimeline"/>.</summary>
/// <remarks>Dart's <c>AggregatedTimings</c>.</remarks>
public sealed class AggregatedTimings
{
    private List<AggregatedTimedBlock>? _aggregatedBlocks;

    /// <summary>Creates aggregated timings from the given timed blocks.</summary>
    public AggregatedTimings(IReadOnlyList<TimedBlock> timedBlocks)
    {
        TimedBlocks = timedBlocks;
    }

    /// <summary>All timed blocks collected between the last reset and the collection.</summary>
    public IReadOnlyList<TimedBlock> TimedBlocks { get; }

    /// <summary>Aggregated timed blocks collected between the last reset and the collection.</summary>
    public IReadOnlyList<AggregatedTimedBlock> AggregatedBlocks => _aggregatedBlocks ??= ComputeAggregatedBlocks();

    private List<AggregatedTimedBlock> ComputeAggregatedBlocks()
    {
        // Dart's LinkedHashMap keeps insertion order; the ordered key list does the same here.
        var order = new List<string>();
        var aggregate = new Dictionary<string, (double Duration, int Count)>();
        foreach (TimedBlock block in TimedBlocks)
        {
            if (!aggregate.TryGetValue(block.Name, out (double Duration, int Count) previousValue))
            {
                previousValue = (0, 0);
                order.Add(block.Name);
            }

            aggregate[block.Name] = (previousValue.Duration + block.Duration, previousValue.Count + 1);
        }

        return order
            .Select(name => new AggregatedTimedBlock(name, aggregate[name].Duration, aggregate[name].Count))
            .ToList();
    }

    /// <summary>
    /// Returns the aggregated block for <paramref name="name"/>; a name that was never recorded
    /// aggregates to a zero duration and count.
    /// </summary>
    public AggregatedTimedBlock GetAggregated(string name)
    {
        AggregatedTimedBlock? match = null;
        foreach (AggregatedTimedBlock block in AggregatedBlocks)
        {
            if (block.Name != name)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidOperationException("Too many elements");
            }

            match = block;
        }

        // Handle the case where there are no recorded blocks of the specified type. In this case,
        // the aggregated duration is simply zero, and so is the number of occurrences (i.e. count).
        return match ?? new AggregatedTimedBlock(name, 0, 0);
    }
}

/// <summary>Aggregates multiple <see cref="TimedBlock"/> objects that share a name.</summary>
/// <remarks>Dart's <c>AggregatedTimedBlock</c>.</remarks>
public sealed class AggregatedTimedBlock
{
    /// <summary>Creates a timed block of code from a name, total duration and count.</summary>
    public AggregatedTimedBlock(string name, double duration, int count)
    {
        FlutterTimeline.DebugAssert(duration >= 0);
        Name = name;
        Duration = duration;
        Count = count;
    }

    /// <summary>The name of the timed blocks.</summary>
    public string Name { get; }

    /// <summary>The sum of the durations of the timed blocks, in microseconds.</summary>
    public double Duration { get; }

    /// <summary>The number of timed blocks aggregated.</summary>
    public int Count { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"AggregatedTimedBlock({Name}, {TimedBlock.Dart(Duration)}, "
        + $"{Count.ToString(CultureInfo.InvariantCulture)})";
}
