using System.Text.RegularExpressions;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/assertions.dart

namespace Plumix.Foundation;

/// Signature for [FlutterError.OnError] handler.
public delegate void FlutterExceptionHandler(FlutterErrorDetails details);

/// Signature for [DiagnosticPropertiesBuilder] transformer.
public delegate IEnumerable<DiagnosticsNode> DiagnosticPropertiesTransformer(
    IEnumerable<DiagnosticsNode> properties);

/// Signature for [FlutterErrorDetails.InformationCollector] callback and other callbacks that
/// collect information describing an error.
public delegate IEnumerable<DiagnosticsNode> InformationCollector();

/// Signature for a function that demangles stack traces into a format that can be parsed by
/// [StackFrame].
public delegate string StackTraceDemangler(string details);

/// Dart's `IterableFilter<T>` from `foundation/basic_types.dart`.
public delegate IEnumerable<T> IterableFilter<T>(IEnumerable<T> input);

/// <summary>
/// The C# counterpart of Dart's `dart:core` `AssertionError`.
/// </summary>
/// <remarks>
/// .NET has no assertion error type whose message can carry an arbitrary object, which is what
/// Flutter relies on when an `assert` fails with a [FlutterError] as its message. This type is that
/// error, and it is also what [FlutterError] extends so that `exception is AssertionError` behaves
/// the way it does in Dart.
/// </remarks>
public class AssertionError : Exception
{
    /// Creates an assertion error with an optional `message` object.
    public AssertionError(object? message = null)
        : base(message?.ToString())
    {
        MessageObject = message;
    }

    /// Message describing the assertion error.
    ///
    /// Dart's `AssertionError.message` is an `Object?`; `Exception.Message` in .NET is a `string`,
    /// so the object is carried here and stringified for the inherited member.
    public virtual object? MessageObject { get; }

    /// <inheritdoc />
    public override string ToString() =>
        MessageObject is null ? "Assertion failed" : $"Assertion failed: {SafeToString(MessageObject)}";

    /// Dart's `Error.safeToString`: strings are quoted, everything else is stringified.
    private protected static string SafeToString(object? value) =>
        value is string text ? $"\"{text}\"" : value?.ToString() ?? "null";
}

/// <summary>
/// Partial information from a stack frame for stack filtering purposes.
/// </summary>
public class PartialStackFrame
{
    /// Creates a new [PartialStackFrame] instance.
    public PartialStackFrame(Regex package, string className, string method)
    {
        Package = package;
        ClassName = className;
        Method = method;
    }

    /// Creates a new [PartialStackFrame] whose package is matched as a literal substring.
    ///
    /// Dart's `package` is a `Pattern`, which a plain `String` implements; C#'s pattern type is
    /// [Regex] alone, so a literal is escaped into one here.
    public PartialStackFrame(string package, string className, string method)
        : this(new Regex(Regex.Escape(package)), className, method)
    {
    }

    /// An `<asynchronous suspension>` line in a stack trace.
    public static PartialStackFrame AsynchronousSuspension { get; } =
        new(string.Empty, string.Empty, "asynchronous suspension");

    /// The package to match, e.g. `package:flutter/src/foundation/assertions.dart`, or
    /// `dotnet:Plumix/Widgets/Element` for a CLR frame.
    public Regex Package { get; }

    /// The class name for the method.
    ///
    /// Top level methods should use the empty string.
    public string ClassName { get; }

    /// The method name for this frame line.
    public string Method { get; }

    /// Tests whether the [StackFrame] matches the information in this [PartialStackFrame].
    public bool Matches(StackFrame stackFrame)
    {
        ArgumentNullException.ThrowIfNull(stackFrame);

        string stackFramePackage =
            $"{stackFrame.PackageScheme}:{stackFrame.Package}/{stackFrame.PackagePath}";
        return Package.IsMatch(stackFramePackage)
            && string.Equals(stackFrame.Method, Method, StringComparison.Ordinal)
            && string.Equals(stackFrame.ClassName, ClassName, StringComparison.Ordinal);
    }
}

/// <summary>
/// A class that filters stack frames for additional filtering on
/// [FlutterError.DefaultStackFilter].
/// </summary>
public abstract class StackFilter
{
    /// Filters the list of [StackFrame]s by updating corresponding indices in `reasons`.
    ///
    /// To elide a frame or number of frames, set the string.
    public abstract void Filter(List<StackFrame> stackFrames, List<string?> reasons);
}

/// <summary>
/// A [StackFilter] that filters based on repeating lists of [PartialStackFrame]s.
/// </summary>
public sealed class RepetitiveStackFrameFilter : StackFilter
{
    /// Creates a new RepetitiveStackFrameFilter.
    public RepetitiveStackFrameFilter(IReadOnlyList<PartialStackFrame> frames, string replacement)
    {
        Frames = frames;
        Replacement = replacement;
    }

    /// The shape of this repetitive stack pattern.
    public IReadOnlyList<PartialStackFrame> Frames { get; }

    /// The number of frames in this pattern.
    public int NumFrames => Frames.Count;

    /// The string to replace the frames with.
    ///
    /// If the same replacement string is used multiple times in a row, the
    /// [FlutterError.DefaultStackFilter] will insert a repeat count after this line rather than
    /// repeating it.
    public string Replacement { get; }

    /// <inheritdoc />
    public override void Filter(List<StackFrame> stackFrames, List<string?> reasons)
    {
        ArgumentNullException.ThrowIfNull(stackFrames);
        ArgumentNullException.ThrowIfNull(reasons);

        for (int index = 0; index < stackFrames.Count - NumFrames; index += 1)
        {
            if (MatchesFrames(stackFrames.Skip(index).Take(NumFrames).ToList()))
            {
                for (int offset = 0; offset < NumFrames; offset += 1)
                {
                    reasons[index + offset] = Replacement;
                }

                index += NumFrames - 1;
            }
        }
    }

    private bool MatchesFrames(List<StackFrame> stackFrames)
    {
        if (stackFrames.Count < NumFrames)
        {
            return false;
        }

        for (int index = 0; index < stackFrames.Count; index++)
        {
            if (!Frames[index].Matches(stackFrames[index]))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// A [DiagnosticsProperty] carrying the parts of an error message.
/// </summary>
/// <remarks>
/// Dart's private `_ErrorDiagnostic`. The value is a `List&lt;Object&gt;` rather than a `String` so
/// that a debug kernel transformer can split an interpolated message into its parts and IDEs can
/// inspect the interpolated objects. Rendering is always the parts joined, so both shapes print
/// identically; C# has no such transformer, so the list always holds exactly the message.
/// </remarks>
public abstract class ErrorDiagnostic : DiagnosticsProperty<List<object>>
{
    /// Creates an error diagnostic from a plain message.
    protected ErrorDiagnostic(string message, DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            null,
            [message],
            showName: false,
            showSeparator: false,
            defaultValue: DiagnosticsDefaults.NullValue,
            style: DiagnosticsTreeStyle.Flat,
            level: level)
    {
    }

    /// Creates an error diagnostic from the parts of an interpolated message.
    protected ErrorDiagnostic(
        List<object> messageParts,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.Flat,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            null,
            messageParts,
            showName: false,
            showSeparator: false,
            defaultValue: DiagnosticsDefaults.NullValue,
            style: style,
            level: level)
    {
    }

    /// The parts this message was assembled from.
    public List<object> MessageParts => TypedValue!;

    /// <inheritdoc />
    public override string ToString(
        TextTreeConfiguration? parentConfiguration,
        DiagnosticLevel minLevel = DiagnosticLevel.Info)
        => ValueToString(parentConfiguration);

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
        => string.Concat(MessageParts);
}

/// <summary>
/// An explanation of the problem and its cause, any information that may help track down the
/// problem, background information, etc.
/// </summary>
public sealed class ErrorDescription : ErrorDiagnostic
{
    /// Creates an error description from a message.
    public ErrorDescription(string message)
        : base(message, DiagnosticLevel.Info)
    {
    }

    /// Creates an error description from the parts of an interpolated message.
    public ErrorDescription(List<object> messageParts)
        : base(messageParts, level: DiagnosticLevel.Info)
    {
    }
}

/// <summary>
/// A short (one line) description of the problem that was detected.
/// </summary>
/// <remarks>
/// A [FlutterError] must start with an [ErrorSummary] and may not contain multiple summaries.
/// </remarks>
public sealed class ErrorSummary : ErrorDiagnostic
{
    /// Creates an error summary from a message.
    public ErrorSummary(string message)
        : base(message, DiagnosticLevel.Summary)
    {
    }

    /// Creates an error summary from the parts of an interpolated message.
    public ErrorSummary(List<object> messageParts)
        : base(messageParts, level: DiagnosticLevel.Summary)
    {
    }
}

/// <summary>
/// An [ErrorHint] provides specific, non-obvious advice that may be applicable.
/// </summary>
public sealed class ErrorHint : ErrorDiagnostic
{
    /// Creates an error hint from a message.
    public ErrorHint(string message)
        : base(message, DiagnosticLevel.Hint)
    {
    }

    /// Creates an error hint from the parts of an interpolated message.
    public ErrorHint(List<object> messageParts)
        : base(messageParts, level: DiagnosticLevel.Hint)
    {
    }
}

/// <summary>
/// An [ErrorSpacer] creates an empty [DiagnosticsNode], that can be used to tune the spacing
/// between other DiagnosticsNode objects.
/// </summary>
public sealed class ErrorSpacer : DiagnosticsProperty<object>
{
    /// Creates an empty space to insert into a list of [DiagnosticsNode] objects.
    public ErrorSpacer()
        : base(string.Empty, (object?)null, description: string.Empty, showName: false)
    {
    }
}

/// <summary>
/// Class for information provided to [FlutterExceptionHandler] callbacks.
/// </summary>
public class FlutterErrorDetails : Diagnosticable
{
    /// Creates a [FlutterErrorDetails] object with the given arguments setting the object's
    /// properties.
    ///
    /// The `exception` must not be null.
    public FlutterErrorDetails(
        object exception,
        string? stack = null,
        string? library = "Plumix framework",
        DiagnosticsNode? context = null,
        IterableFilter<string>? stackFilter = null,
        InformationCollector? informationCollector = null,
        bool silent = false)
    {
        Exception = exception;
        Stack = stack;
        Library = library;
        Context = context;
        StackFilter = stackFilter;
        InformationCollector = informationCollector;
        Silent = silent;
    }

    /// Transformers to transform [DiagnosticsNode] in [DiagnosticPropertiesBuilder] into a more
    /// descriptive form.
    public static List<DiagnosticPropertiesTransformer> PropertiesTransformers { get; } = [];

    /// The exception. Often this will be an [AssertionError], maybe specifically a [FlutterError].
    public object Exception { get; }

    /// The stack trace from where the [Exception] was thrown (as opposed to where it was caught).
    ///
    /// Dart's `StackTrace` is an opaque object whose only contract is its `toString`; the C# port
    /// carries that string directly (see `docs/ai/DIVERGENCES.md`).
    public string? Stack { get; }

    /// A human-readable brief name describing the library that caught the error message.
    public string? Library { get; }

    /// A [DiagnosticsNode] that provides a human-readable description of where the error was caught.
    public DiagnosticsNode? Context { get; }

    /// A callback which filters the [Stack] trace.
    public IterableFilter<string>? StackFilter { get; }

    /// A callback which, if non-null, will be called to collect additional information when the
    /// error is reported to the console.
    public InformationCollector? InformationCollector { get; }

    /// Whether this error should be ignored by the default error reporting behavior in release mode.
    public bool Silent { get; }

    /// Returns a short (one line) description of the problem that was detected.
    ///
    /// If the exception contains an [ErrorSummary] that summary is used, otherwise the summary is
    /// inferred from the string representation of the exception.
    public DiagnosticsNode Summary
    {
        get
        {
            if (Constants.KReleaseMode)
            {
                return DiagnosticsNode.Message(FormatException());
            }

            IDiagnosticable? diagnosticable = ExceptionToDiagnosticable();
            DiagnosticsNode? summary = null;
            if (diagnosticable is not null)
            {
                var builder = new DiagnosticPropertiesBuilder();
                DebugFillProperties(builder);
                summary = builder.Properties.FirstOrDefault(node => node.Level == DiagnosticLevel.Summary);
            }

            return summary ?? new ErrorSummary(FormatException());
        }
    }

    /// Creates a copy of this [FlutterErrorDetails] but with the given fields replaced with the new
    /// values.
    public FlutterErrorDetails CopyWith(
        DiagnosticsNode? context = null,
        object? exception = null,
        InformationCollector? informationCollector = null,
        string? library = null,
        bool? silent = null,
        string? stack = null,
        IterableFilter<string>? stackFilter = null)
    {
        return new FlutterErrorDetails(
            context: context ?? Context,
            exception: exception ?? Exception,
            informationCollector: informationCollector ?? InformationCollector,
            library: library ?? Library,
            silent: silent ?? Silent,
            stack: stack ?? Stack,
            stackFilter: stackFilter ?? StackFilter);
    }

    /// Converts the [Exception] to a string.
    ///
    /// This applies some additional logic to make [AssertionError] exceptions prettier, to handle
    /// exceptions that stringify to empty strings, to handle objects that don't inherit from
    /// [System.Exception], and so forth.
    public string ExceptionAsString()
    {
        string? longMessage = null;
        if (Exception is AssertionError assertion)
        {
            // Regular assertion errors put the message last, after some code snippets. This leads to
            // ugly messages. To avoid this, we move the assertion message up to before the code
            // snippets, separated by a newline, if we recognize that format is being used.
            object? message = assertion.MessageObject;
            string fullMessage = assertion.ToString();
            if (message is string text && !string.Equals(text, fullMessage, StringComparison.Ordinal)
                && fullMessage.Length > text.Length)
            {
                int position = fullMessage.LastIndexOf(text, StringComparison.Ordinal);
                if (position == fullMessage.Length - text.Length
                    && position > 2
                    && fullMessage.Substring(position - 2, 2) == ": ")
                {
                    // Add a linebreak so that the filename at the start of the assertion message is
                    // always on its own line.
                    string body = fullMessage[..(position - 2)];
                    int splitPoint = body.IndexOf(" Failed assertion:", StringComparison.Ordinal);
                    if (splitPoint >= 0)
                    {
                        body = $"{body[..splitPoint]}\n{body[(splitPoint + 1)..]}";
                    }

                    longMessage = $"{text.TrimEnd()}\n{body}";
                }
            }

            longMessage ??= fullMessage;
        }
        else if (Exception is string plain)
        {
            longMessage = plain;
        }
        else if (Exception is Exception error)
        {
            longMessage = DescribeException(error);
        }
        else
        {
            longMessage = $"  {Exception}";
        }

        longMessage = longMessage.TrimEnd();
        if (longMessage.Length == 0)
        {
            longMessage = "  <no message available>";
        }

        return longMessage;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        DiagnosticsNode verb = new ErrorDescription(
            $"thrown{(Context is not null ? new ErrorDescription($" {Context}").ToString() : string.Empty)}");
        IDiagnosticable? diagnosticable = ExceptionToDiagnosticable();
        if (IsNumber(Exception))
        {
            properties.Add(new ErrorDescription($"The number {Exception} was {verb}."));
        }
        else
        {
            object thrown = Exception;
            DiagnosticsNode errorName = new ErrorDescription(thrown switch
            {
                AssertionError => "assertion",
                string => "message",
                System.Exception => Diagnostics.ObjectRuntimeType(thrown),
                _ => $"{Diagnostics.ObjectRuntimeType(thrown)} object",
            });
            properties.Add(new ErrorDescription($"The following {errorName} was {verb}:"));
            if (diagnosticable is not null)
            {
                diagnosticable.DebugFillProperties(properties);
            }
            else
            {
                // Many exception classes put their type at the head of their message. This is
                // redundant with the way we display exceptions, so attempt to strip out that header
                // when we see it.
                string prefix = $"{Diagnostics.ObjectRuntimeType(Exception)}: ";
                string message = ExceptionAsString();
                if (message.StartsWith(prefix, StringComparison.Ordinal))
                {
                    message = message[prefix.Length..];
                }

                properties.Add(new ErrorSummary(message));
            }
        }

        if (Stack is not null)
        {
            if (Exception is AssertionError && diagnosticable is null)
            {
                // After popping off any dart: stack frames, are there at least two more stack frames
                // coming from the framework?
                //
                // If not: Error is in user code (user violated assertion in framework).
                // If so:  Error is in the framework. We either need an assertion higher up in the
                //         stack, or we've violated our own assertions.
                List<StackFrame> stackFrames = [.. StackFrame
                    .FromStackString(FlutterError.DemangleStackTrace(Stack))
                    .SkipWhile(frame => frame.PackageScheme == "dart")];
                bool ourFault = stackFrames.Count >= 2
                    && stackFrames[0].Package == FrameworkPackage
                    && stackFrames[1].Package == FrameworkPackage;
                if (ourFault)
                {
                    properties.Add(new ErrorSpacer());
                    properties.Add(new ErrorHint(
                        "Either the assertion indicates an error in the framework itself, or we should "
                        + "provide substantially more information in this error message to help you determine "
                        + "and fix the underlying cause.\n"
                        + "In either case, please report this assertion by filing a bug on GitHub:\n"
                        + "  https://github.com/Plumix-Net/Plumix/issues/new"));
                }
            }

            properties.Add(new ErrorSpacer());
            properties.Add(new DiagnosticsStackTrace(
                "When the exception was thrown, this was the stack",
                Stack,
                stackFilter: StackFilter));
        }

        if (InformationCollector is not null)
        {
            properties.Add(new ErrorSpacer());
            foreach (DiagnosticsNode node in InformationCollector())
            {
                properties.Add(node);
            }
        }
    }

    /// <inheritdoc />
    public override string ToStringShort() =>
        Library is not null ? $"Exception caught by {Library}" : "Exception caught";

    /// <inheritdoc />
    public override string ToString(DiagnosticLevel minLevel) =>
        ToDiagnosticsNode(style: DiagnosticsTreeStyle.Error).ToStringDeep(minLevel: minLevel);

    /// <inheritdoc />
    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new FlutterErrorDetailsNode(name, this, style);

    /// The [StackFrame.Package] value the framework's own frames carry.
    private const string FrameworkPackage = "Plumix";

    /// Renders an exception the way Dart's `Error.toString`/`Exception.toString` do: the runtime
    /// type, a colon, and the message — never .NET's stack-trace-carrying `ToString`.
    private static string DescribeException(Exception error) =>
        $"{Diagnostics.ObjectRuntimeType(error)}: {error.Message}";

    private static bool IsNumber(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal;

    private string FormatException() => ExceptionAsString().Split('\n')[0].TrimStart();

    private IDiagnosticable? ExceptionToDiagnosticable()
    {
        if (Exception is FlutterError flutterError)
        {
            return flutterError;
        }

        if (Exception is AssertionError assertion && assertion.MessageObject is FlutterError inner)
        {
            return inner;
        }

        return null;
    }
}

/// <summary>
/// Error class used to report Plumix-specific assertion failures and contract violations.
/// </summary>
public class FlutterError : AssertionError, IDiagnosticableTree
{
    private static int _errorCount;

    private static readonly List<StackFilter> StackFilters = [];

    /// The width to which [DumpErrorToConsole] will wrap lines.
    public const int WrapWidth = 100;

    /// Create an error message from a list of [DiagnosticsNode]s.
    ///
    /// By convention, there should be exactly one [ErrorSummary] in the list, and it should be the
    /// first entry.
    ///
    /// Dart's `FlutterError.fromParts` named constructor; the message-taking factory is
    /// [FlutterError(string)].
    public FlutterError(List<DiagnosticsNode> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Diagnostics = diagnostics;
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (diagnostics.Count == 0)
        {
            throw new AssertionError(new FlutterError([new ErrorSummary("Empty FlutterError")]));
        }

        if (diagnostics[0].Level != DiagnosticLevel.Summary)
        {
            throw new AssertionError(new FlutterError([
                new ErrorSummary("FlutterError is missing a summary."),
                new ErrorDescription(
                    "All FlutterError objects should start with a short (one line) "
                    + "summary description of the problem that was detected."),
                new DiagnosticsProperty<FlutterError>(
                    "Malformed",
                    this,
                    expandableValue: true,
                    showSeparator: false,
                    style: DiagnosticsTreeStyle.Whitespace),
                new ErrorDescription(MalformedReportHint),
            ]));
        }

        List<DiagnosticsNode> summaries =
            [.. diagnostics.Where(node => node.Level == DiagnosticLevel.Summary)];
        if (summaries.Count > 1)
        {
            List<DiagnosticsNode> message =
            [
                new ErrorSummary("FlutterError contained multiple error summaries."),
                new ErrorDescription(
                    "All FlutterError objects should have only a single short "
                    + "(one line) summary description of the problem that was "
                    + "detected."),
                new DiagnosticsProperty<FlutterError>(
                    "Malformed",
                    this,
                    expandableValue: true,
                    showSeparator: false,
                    style: DiagnosticsTreeStyle.Whitespace),
                new ErrorDescription($"\nThe malformed error has {summaries.Count} summaries."),
            ];
            int i = 1;
            foreach (DiagnosticsNode summary in summaries)
            {
                message.Add(new DiagnosticsProperty<DiagnosticsNode>($"Summary {i}", summary, expandableValue: true));
                i += 1;
            }

            message.Add(new ErrorDescription(MalformedReportHint));
            throw new AssertionError(new FlutterError(message));
        }
    }

    /// Create an error message from a string.
    ///
    /// The message may have newlines in it. The first line should be a terse description of the
    /// error. Subsequent lines should contain substantial additional information.
    ///
    /// The first line is wrapped in an implied [ErrorSummary], and subsequent lines are wrapped in
    /// implied [ErrorDescription]s.
    public FlutterError(string message)
        : this(FromMessage(message))
    {
    }

    /// Called whenever the framework catches an error.
    ///
    /// The default behavior is to call [PresentError]. Set this to null to silently catch and
    /// ignore errors.
    public static FlutterExceptionHandler? OnError { get; set; } = DumpErrorToConsole;

    /// Called by the framework before attempting to parse a stack trace.
    ///
    /// The default behavior is to assume all stack traces are in the format the CLR generates.
    public static StackTraceDemangler DemangleStackTrace { get; set; } = DefaultStackTraceDemangler;

    /// Called whenever the framework wants to present an error to the users.
    ///
    /// The default behavior is to call [DumpErrorToConsole].
    public static FlutterExceptionHandler PresentError { get; set; } = DumpErrorToConsole;

    /// The information associated with this error, in structured form.
    public List<DiagnosticsNode> Diagnostics { get; }

    /// <inheritdoc />
    public override object? MessageObject => ToString();

    /// <inheritdoc />
    public override string Message => ToString();

    /// Resets the count of errors used by [DumpErrorToConsole] to decide whether to show a complete
    /// error message or an abbreviated one.
    public static void ResetErrorCount() => _errorCount = 0;

    /// Prints the given exception details to the console.
    ///
    /// The first time this is called, it dumps a very verbose message to the console. Subsequent
    /// calls only dump the first line of the exception, unless `forceReport` is set to true.
    public static void DumpErrorToConsole(FlutterErrorDetails details) => DumpErrorToConsole(details, false);

    /// Prints the given exception details to the console.
    public static void DumpErrorToConsole(FlutterErrorDetails details, bool forceReport)
    {
        ArgumentNullException.ThrowIfNull(details);

        // In debug mode, we ignore the "silent" flag.
        bool isInDebugMode = Constants.KDebugMode;
        bool reportError = isInDebugMode || !details.Silent;
        if (!reportError && !forceReport)
        {
            return;
        }

        if (_errorCount == 0 || forceReport)
        {
            // Diagnostics are only available in debug mode. In profile and release modes fall back
            // to a plain stack dump.
            if (isInDebugMode)
            {
                ErrorToConsoleDumper.Dump(
                    new TextTreeRenderer(wrapWidthProperties: WrapWidth, maxDescendentsTruncatableNode: 5)
                        .Render(details.ToDiagnosticsNode(style: DiagnosticsTreeStyle.Error))
                        .TrimEnd());
            }
            else
            {
                Assertions.DebugPrintStack(
                    stackTrace: details.Stack,
                    label: details.Exception.ToString(),
                    maxFrames: 100);
            }
        }
        else
        {
            ErrorToConsoleDumper.Dump($"Another exception was thrown: {details.Summary}");
        }

        _errorCount += 1;
    }

    /// Adds a stack filtering function to [DefaultStackFilter].
    public static void AddDefaultStackFilter(StackFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        StackFilters.Add(filter);
    }

    /// Removes every filter registered through [AddDefaultStackFilter].
    ///
    /// Plumix-only: Dart's `_stackFilters` is file-private and append-only, which leaves tests no
    /// way to undo a registration.
    internal static void ClearDefaultStackFilters() => StackFilters.Clear();

    /// Converts a stack to a string that is more readable by omitting stack frames that correspond
    /// to runtime internals.
    ///
    /// This is the default filter used by [DumpErrorToConsole] if the [FlutterErrorDetails] object
    /// has no [FlutterErrorDetails.StackFilter] callback.
    public static IEnumerable<string> DefaultStackFilter(IEnumerable<string> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        var removedPackagesAndClasses = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["dart:async-patch"] = 0,
            ["dart:async"] = 0,
            ["package:stack_trace"] = 0,
            ["class _AssertionError"] = 0,
            ["class _FakeAsync"] = 0,
            ["class _FrameCallbackEntry"] = 0,
            ["class _Timer"] = 0,
            ["class _RawReceivePortImpl"] = 0,
            // CLR frames keep the root namespace in Package and the remainder in PackagePath.
            ["dotnet:System/Runtime/CompilerServices"] = 0,
            ["dotnet:System/Threading/Tasks"] = 0,
        };
        int skipped = 0;

        List<StackFrame> parsedFrames = StackFrame.FromStackString(string.Join('\n', frames));

        for (int index = 0; index < parsedFrames.Count; index += 1)
        {
            StackFrame frame = parsedFrames[index];
            string className = $"class {frame.ClassName}";
            string package = $"{frame.PackageScheme}:{frame.Package}";
            string framePackagePath = $"{package}/{frame.PackagePath}";
            string? removalKey = removedPackagesAndClasses.ContainsKey(className)
                ? className
                : removedPackagesAndClasses.ContainsKey(package)
                    ? package
                    : removedPackagesAndClasses.Keys.FirstOrDefault(key =>
                        key.StartsWith("dotnet:", StringComparison.Ordinal)
                        && framePackagePath.StartsWith($"{key}/", StringComparison.Ordinal));
            if (removalKey is not null)
            {
                skipped += 1;
                removedPackagesAndClasses[removalKey] += 1;
                parsedFrames.RemoveAt(index);
                index -= 1;
            }
        }

        var reasons = new List<string?>(new string?[parsedFrames.Count]);
        foreach (StackFilter filter in StackFilters)
        {
            filter.Filter(parsedFrames, reasons);
        }

        var result = new List<string>();

        // Collapse duplicated reasons.
        for (int index = 0; index < parsedFrames.Count; index += 1)
        {
            int start = index;
            while (index < reasons.Count - 1
                && reasons[index] is not null
                && string.Equals(reasons[index + 1], reasons[index], StringComparison.Ordinal))
            {
                index++;
            }

            string suffix = string.Empty;
            if (reasons[index] is not null)
            {
                suffix = index != start ? $" ({index - start + 2} frames)" : " (1 frame)";
            }

            result.Add($"{reasons[index] ?? parsedFrames[index].Source}{suffix}");
        }

        // Only include packages we actually elided from.
        List<string> where = [.. removedPackagesAndClasses
            .Where(entry => entry.Value > 0)
            .Select(entry => entry.Key)
            .Order(StringComparer.Ordinal)];
        if (skipped == 1)
        {
            result.Add($"(elided one frame from {where.Single()})");
        }
        else if (skipped > 1)
        {
            if (where.Count > 1)
            {
                where[^1] = $"and {where[^1]}";
            }

            result.Add(where.Count > 2
                ? $"(elided {skipped} frames from {string.Join(", ", where)})"
                : $"(elided {skipped} frames from {string.Join(" ", where)})");
        }

        return result;
    }

    /// Calls [OnError] with the given details, unless it is null.
    public static void ReportError(FlutterErrorDetails details) => OnError?.Invoke(details);

    /// <inheritdoc />
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        foreach (DiagnosticsNode node in Diagnostics)
        {
            properties.Add(node);
        }
    }

    /// <inheritdoc />
    public string ToStringShort() => "FlutterError";

    /// <inheritdoc />
    public override string ToString() => ToString(DiagnosticLevel.Info);

    /// Returns a string representation of this error, showing every diagnostic whose level is at
    /// least `minLevel`.
    public string ToString(DiagnosticLevel minLevel)
    {
        if (Constants.KReleaseMode)
        {
            List<ErrorDiagnostic> errors = [.. Diagnostics.OfType<ErrorDiagnostic>()];
            return errors.Count > 0 ? errors[0].ValueToString() : ToStringShort();
        }

        // Avoid wrapping lines.
        var renderer = new TextTreeRenderer(wrapWidth: int.MaxValue);
        return string.Join('\n', Diagnostics.Select(node => renderer.Render(node).TrimEnd()));
    }

    /// Returns a string representation of this node and its descendants.
    public string ToStringDeep(
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 65)
    {
        return ToDiagnosticsNode().ToStringDeep(
            prefixLineOne: prefixLineOne,
            prefixOtherLines: prefixOtherLines,
            minLevel: minLevel,
            wrapWidth: wrapWidth);
    }

    /// <inheritdoc />
    public DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new DiagnosticableTreeNode(name, this, style);

    /// <inheritdoc />
    public List<DiagnosticsNode> DebugDescribeChildren() => [];

    private const string MalformedReportHint =
        "\nThis error should still help you solve your problem, "
        + "however please also report this malformed error in the "
        + "framework by filing a bug on GitHub:\n"
        + "  https://github.com/Plumix-Net/Plumix/issues/new";

    private static List<DiagnosticsNode> FromMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        string[] lines = message.Split('\n');
        List<DiagnosticsNode> diagnostics = [new ErrorSummary(lines[0])];
        diagnostics.AddRange(lines.Skip(1).Select(DiagnosticsNode (line) => new ErrorDescription(line)));
        return diagnostics;
    }

    private static string DefaultStackTraceDemangler(string stackTrace) => stackTrace;
}

/// <summary>
/// Diagnostic with a stack trace [Value] suitable for displaying stack traces as part of a
/// [FlutterError] object.
/// </summary>
public class DiagnosticsStackTrace : DiagnosticsBlock
{
    /// Creates a diagnostic for a stack trace.
    ///
    /// `name` describes a name the stack trace is given, e.g. `When the exception was thrown, this
    /// was the stack`. `stackFilter` provides an optional filter to use to filter which frames are
    /// included; if no filter is specified, [FlutterError.DefaultStackFilter] is used.
    /// `showSeparator` indicates whether to include a ':' after the `name`.
    public DiagnosticsStackTrace(
        string name,
        string? stack,
        IterableFilter<string>? stackFilter = null,
        bool showSeparator = true)
        : base(
            name: name,
            value: stack,
            properties: ApplyStackFilter(stack, stackFilter),
            style: DiagnosticsTreeStyle.Flat,
            showSeparator: showSeparator,
            allowTruncate: true)
    {
    }

    private DiagnosticsStackTrace(string name, string frame, bool showSeparator)
        : base(
            name: name,
            properties: [CreateStackFrame(frame)],
            style: DiagnosticsTreeStyle.Whitespace,
            showSeparator: showSeparator)
    {
    }

    /// <inheritdoc />
    public override bool AllowTruncate => false;

    /// Creates a diagnostic describing a single frame from a stack trace.
    ///
    /// Dart's `DiagnosticsStackTrace.singleFrame` named constructor.
    public static DiagnosticsStackTrace SingleFrame(string name, string frame, bool showSeparator = true)
        => new(name, frame, showSeparator);

    private static List<DiagnosticsNode> ApplyStackFilter(string? stack, IterableFilter<string>? stackFilter)
    {
        if (stack is null)
        {
            return [];
        }

        IterableFilter<string> filter = stackFilter ?? FlutterError.DefaultStackFilter;
        IEnumerable<string> frames = filter(FlutterError.DemangleStackTrace(stack).TrimEnd().Split('\n'));
        return [.. frames.Select(CreateStackFrame)];
    }

    private static DiagnosticsNode CreateStackFrame(string frame)
        => DiagnosticsNode.Message(frame, allowWrap: false);
}

/// <summary>
/// The `debugPrintStack` free function from `foundation/assertions.dart`.
/// </summary>
public static class Assertions
{
    /// Dump the stack to the console using [Print.DebugPrint] and
    /// [FlutterError.DefaultStackFilter].
    ///
    /// If the `stackTrace` parameter is null, the current stack is used.
    ///
    /// The `maxFrames` argument can be given to limit the stack to the given number of lines before
    /// filtering is applied. By default, all stack lines are included.
    ///
    /// The `label` argument, if present, will be printed before the stack.
    public static void DebugPrintStack(string? stackTrace = null, string? label = null, int? maxFrames = null)
    {
        if (label is not null)
        {
            ErrorToConsoleDumper.Dump(label);
        }

        stackTrace = stackTrace is null
            ? new System.Diagnostics.StackTrace(true).ToString()
            : FlutterError.DemangleStackTrace(stackTrace);

        IEnumerable<string> lines = stackTrace.TrimEnd().Split('\n');
        if (maxFrames is not null)
        {
            lines = lines.Take(maxFrames.Value);
        }

        ErrorToConsoleDumper.Dump(string.Join('\n', FlutterError.DefaultStackFilter(lines)));
    }
}

/// <summary>
/// The sink error messages are dumped to.
/// </summary>
/// <remarks>
/// Dart's `foundation/error_dumper.dart`. Its IO implementation is a `debugPrint` call and its web
/// implementation writes to `console.error`; Plumix has the one implementation, and tests capture
/// it by replacing [Print.PrintLine] the way Flutter's `capture_output.dart` replaces `print`.
/// </remarks>
public static class ErrorToConsoleDumper
{
    /// Dumps `message` to the console.
    public static void Dump(string message) => Print.DebugPrint(message);
}

/// <summary>
/// The [DiagnosticableNode] a [FlutterErrorDetails] describes itself with.
/// </summary>
/// <remarks>
/// Dart's private `_FlutterErrorDetailsNode`; C# has no file-private types, so it is internal.
/// </remarks>
internal sealed class FlutterErrorDetailsNode : DiagnosticableNode<FlutterErrorDetails>
{
    internal FlutterErrorDetailsNode(string? name, FlutterErrorDetails value, DiagnosticsTreeStyle? style)
        : base(name, value, style)
    {
    }

    protected override DiagnosticPropertiesBuilder Builder
    {
        get
        {
            DiagnosticPropertiesBuilder builder = base.Builder;
            IEnumerable<DiagnosticsNode> properties = builder.Properties;
            foreach (DiagnosticPropertiesTransformer transformer in FlutterErrorDetails.PropertiesTransformers)
            {
                properties = transformer(properties);
            }

            return DiagnosticPropertiesBuilder.FromProperties([.. properties]);
        }
    }
}
