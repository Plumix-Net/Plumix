using System.Diagnostics;
using System.Text.RegularExpressions;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/print.dart

namespace Plumix.Foundation;

/// <summary>
/// Signature for [Print.DebugPrint] implementations.
/// </summary>
/// <remarks>
/// If a `wrapWidth` is provided, each line of the `message` is word-wrapped to that width. (Lines
/// may be separated by newline characters, as in `\n`.)
///
/// By default, this function very crudely attempts to throttle the rate at which messages are sent
/// to avoid data loss on Android. This means that interleaving calls to this function and to the
/// raw line sink can result in out-of-order messages in the logs.
///
/// The implementation can be replaced by setting [Print.DebugPrint] to a new implementation that
/// matches this signature.
/// </remarks>
public delegate void DebugPrintCallback(string? message, int? wrapWidth = null);

/// <summary>
/// The `debugPrint` family from Flutter's `foundation/print.dart`.
/// </summary>
/// <remarks>
/// Dart's free functions live on this static class because C# has no top-level functions, and
/// Dart's `print` — which flutter_test replaces through a `Zone` — is the replaceable
/// [PrintLine] sink.
/// </remarks>
public static class Print
{
    private const int KDebugPrintCapacity = 12 * 1024;

    private static readonly TimeSpan KDebugPrintPauseTime = TimeSpan.FromSeconds(1);

    private static readonly Regex IndentPattern = new("^ *(?:[-+*] |[0-9]+[.):] )?", RegexOptions.Compiled);

    private static readonly Queue<string> DebugPrintBuffer = new();

    private static readonly Stopwatch DebugPrintStopwatch = new();

    private static int _debugPrintedCharacters;

    private static TaskCompletionSource? _debugPrintCompleter;

    private static bool _debugPrintScheduled;

    /// Prints a message to the console.
    ///
    /// The default value is [DebugPrintThrottled]. For a version that acts identically but does not
    /// throttle, use [DebugPrintSynchronously].
    public static DebugPrintCallback DebugPrint { get; set; } = DebugPrintThrottled;

    /// The raw line sink every implementation in this class writes through.
    ///
    /// Dart calls `print`, which flutter_test replaces by running the framework inside a `Zone`
    /// that overrides it. .NET has no zones, so the sink itself is replaceable.
    public static Action<string> PrintLine { get; set; } = Console.Out.WriteLine;

    /// A task that resolves when there is no longer any buffered content being printed by
    /// [DebugPrintThrottled] (which is the default implementation for [DebugPrint], which is used
    /// to report errors to the console).
    public static Task DebugPrintDone => _debugPrintCompleter?.Task ?? Task.CompletedTask;

    /// Schedules `callback` to run after `delay`.
    ///
    /// Dart creates a `Timer`; the seam exists so tests can drive the throttle deterministically
    /// the way Flutter's own tests drive it with `FakeAsync`.
    internal static Action<TimeSpan, Action> ScheduleTimer { get; set; } = DefaultScheduleTimer;

    /// Alternative implementation of [DebugPrint] that does not throttle.
    ///
    /// Used by tests.
    public static void DebugPrintSynchronously(string? message, int? wrapWidth = null)
    {
        if (message is not null && wrapWidth is not null)
        {
            PrintLine(string.Join(
                '\n',
                message.Split('\n').SelectMany(line => DebugWordWrap(line, wrapWidth.Value))));
        }
        else
        {
            PrintLine(message ?? "null");
        }
    }

    /// Implementation of [DebugPrint] that throttles messages.
    ///
    /// This avoids dropping messages on platforms that rate-limit their logging (for example,
    /// Android).
    ///
    /// If `wrapWidth` is not null, the message is wrapped using [DebugWordWrap].
    public static void DebugPrintThrottled(string? message, int? wrapWidth = null)
    {
        string[] messageLines = message?.Split('\n') ?? ["null"];
        if (wrapWidth is not null)
        {
            foreach (string line in messageLines.SelectMany(line => DebugWordWrap(line, wrapWidth.Value)))
            {
                DebugPrintBuffer.Enqueue(line);
            }
        }
        else
        {
            foreach (string line in messageLines)
            {
                DebugPrintBuffer.Enqueue(line);
            }
        }

        if (!_debugPrintScheduled)
        {
            DebugPrintTask();
        }
    }

    /// Wraps the given string at the given width.
    ///
    /// The `message` should not contain newlines (`\n`, U+000A). Strings that may contain newlines
    /// should be split before being wrapped.
    ///
    /// Wrapping occurs at space characters (U+0020). Lines that start with an octothorpe ("#",
    /// U+0023) are not wrapped (so for example, Dart stack traces won't be wrapped).
    ///
    /// Subsequent lines attempt to duplicate the indentation of the first line, for example if the
    /// first line starts with multiple spaces. In addition, if a `wrapIndent` argument is provided,
    /// each line after the first is prefixed by that string.
    ///
    /// This is not suitable for use with arbitrary Unicode text. It is only intended for formatting
    /// error messages.
    public static IEnumerable<string> DebugWordWrap(string message, int width, string wrapIndent = "")
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Length < width || message.TrimStart()[0] == '#')
        {
            return [message];
        }

        var wrapped = new List<string>();
        Match prefixMatch = IndentPattern.Match(message);
        string prefix = wrapIndent + new string(' ', prefixMatch.Length);
        int start = 0;
        int startForLengthCalculations = 0;
        bool addPrefix = false;
        int index = prefix.Length;
        WordWrapParseMode mode = WordWrapParseMode.InSpace;
        int lastWordStart = 0;
        int? lastWordEnd = null;
        while (true)
        {
            switch (mode)
            {
                // At start of break point (or start of line); can't break until next break.
                case WordWrapParseMode.InSpace:
                    while (index < message.Length && message[index] == ' ')
                    {
                        index += 1;
                    }

                    lastWordStart = index;
                    mode = WordWrapParseMode.InWord;
                    break;

                // Looking for a good break point.
                case WordWrapParseMode.InWord:
                    while (index < message.Length && message[index] != ' ')
                    {
                        index += 1;
                    }

                    mode = WordWrapParseMode.AtBreak;
                    break;

                // At start of break point.
                case WordWrapParseMode.AtBreak:
                    if (index - startForLengthCalculations > width || index == message.Length)
                    {
                        // We are over the width line, so break.
                        if (index - startForLengthCalculations <= width || lastWordEnd is null)
                        {
                            // We should use this point, because either it doesn't actually go over
                            // the end (last line), or it does, but there was no earlier break point.
                            lastWordEnd = index;
                        }

                        if (addPrefix)
                        {
                            wrapped.Add(prefix + message[start..lastWordEnd.Value]);
                        }
                        else
                        {
                            wrapped.Add(message[start..lastWordEnd.Value]);
                            addPrefix = true;
                        }

                        if (lastWordEnd.Value >= message.Length)
                        {
                            return wrapped;
                        }

                        // Just yielded a line.
                        if (lastWordEnd.Value == index)
                        {
                            // We broke at the current position; eat all the spaces, then set our
                            // start point.
                            while (index < message.Length && message[index] == ' ')
                            {
                                index += 1;
                            }

                            start = index;
                            mode = WordWrapParseMode.InWord;
                        }
                        else
                        {
                            // We broke at the previous break point, and we're at the start of a new
                            // one.
                            start = lastWordStart;
                            mode = WordWrapParseMode.AtBreak;
                        }

                        startForLengthCalculations = start - prefix.Length;
                        lastWordEnd = null;
                    }
                    else
                    {
                        // Save this break point, we're not yet over the line width, then skip to the
                        // end of this break point.
                        lastWordEnd = index;
                        mode = WordWrapParseMode.InSpace;
                    }

                    break;
            }
        }
    }

    /// Drops every buffered line and resets the throttle window.
    ///
    /// Plumix-only: Dart's throttle state is file-private and its tests reach it through
    /// `FakeAsync`; Plumix tests need a way to leave the globals clean.
    internal static void ResetThrottleForTesting()
    {
        DebugPrintBuffer.Clear();
        DebugPrintStopwatch.Reset();
        _debugPrintedCharacters = 0;
        _debugPrintScheduled = false;
        _debugPrintCompleter = null;
    }

    private static void DefaultScheduleTimer(TimeSpan delay, Action callback)
    {
        _ = Task.Delay(delay).ContinueWith(_ => callback(), TaskScheduler.Default);
    }

    private static void DebugPrintTask()
    {
        _debugPrintScheduled = false;
        if (DebugPrintStopwatch.Elapsed > KDebugPrintPauseTime)
        {
            DebugPrintStopwatch.Stop();
            DebugPrintStopwatch.Reset();
            _debugPrintedCharacters = 0;
        }

        while (_debugPrintedCharacters < KDebugPrintCapacity && DebugPrintBuffer.Count > 0)
        {
            string line = DebugPrintBuffer.Dequeue();
            _debugPrintedCharacters += line.Length;
            PrintLine(line);
        }

        if (DebugPrintBuffer.Count > 0)
        {
            _debugPrintScheduled = true;
            _debugPrintedCharacters = 0;
            ScheduleTimer(KDebugPrintPauseTime, DebugPrintTask);
            _debugPrintCompleter ??= new TaskCompletionSource();
        }
        else
        {
            DebugPrintStopwatch.Start();
            _debugPrintCompleter?.TrySetResult();
            _debugPrintCompleter = null;
        }
    }

    private enum WordWrapParseMode
    {
        InSpace,
        InWord,
        AtBreak,
    }
}
