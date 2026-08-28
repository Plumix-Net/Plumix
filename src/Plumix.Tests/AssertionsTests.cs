using Plumix.Foundation;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity tests for the ported `foundation/assertions.dart`, `foundation/print.dart` and
/// `foundation/stack_frame.dart`. Goldens come from Flutter's own
/// `test/foundation/assertions_test.dart`, `error_reporting_test.dart`, `print_test.dart` and
/// `stack_frame_test.dart`, with Flutter's self-identifying strings (the library label and the
/// issue URL) replaced by Plumix's.
/// </summary>
public class AssertionsTests
{
    /// The Dart VM trace Flutter's "Identifies user fault" test uses, rewritten as the CLR trace
    /// the same situation produces in Plumix: the framework's `Text` constructor is reached from
    /// application code, so only one framework frame sits on top.
    private const string UserFaultStack = """
           at Plumix.Widgets.Text..ctor(String data) in /src/Plumix/Widgets/Text.cs:line 287
           at HelloPlumix.MyHomePageState.Build(BuildContext context) in /app/main.cs:line 72
           at Plumix.Widgets.StatefulElement.Build() in /src/Plumix/Widgets/Framework.Element.cs:line 4414
           at Plumix.Widgets.ComponentElement.PerformRebuild() in /src/Plumix/Widgets/Framework.Element.cs:line 4303
           at Plumix.Widgets.Element.Rebuild() in /src/Plumix/Widgets/Framework.Element.cs:line 4027
        """;

    /// The same trace with a second framework frame on top, which is Flutter's "Identifies our
    /// fault" case.
    private const string OurFaultStack = """
           at Plumix.Widgets.Text..ctor(String data) in /src/Plumix/Widgets/Text.cs:line 287
           at Plumix.Widgets.SomeWidgetUsingText..ctor() in /src/Plumix/Widgets/TextHelper.cs:line 287
           at HelloPlumix.MyHomePageState.Build(BuildContext context) in /app/main.cs:line 72
           at Plumix.Widgets.StatefulElement.Build() in /src/Plumix/Widgets/Framework.Element.cs:line 4414
           at Plumix.Widgets.ComponentElement.PerformRebuild() in /src/Plumix/Widgets/Framework.Element.cs:line 4303
        """;

    private const string FrameworkBugHint =
        "Either the assertion indicates an error in the framework itself, or we should "
        + "provide substantially more information in this error message to help you determine "
        + "and fix the underlying cause.\n"
        + "In either case, please report this assertion by filing a bug on GitHub:\n"
        + "  https://github.com/Plumix-Net/Plumix/issues/new";

    [Fact]
    public void DebugPrintStackPrintsLabelThenFrames()
    {
        List<string> log = CaptureOutput(() => Assertions.DebugPrintStack(label: "Example label", maxFrames: 7));

        Assert.True(log.Count >= 2);
        Assert.Contains("Example label", log[0], StringComparison.Ordinal);
        Assert.Contains("DebugPrintStack", log[1], StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorDescriptionShowsItsMessage()
    {
        const string DescriptionMessage = "This is the message";
        var errorDescription = new ErrorDescription(DescriptionMessage);

        Assert.Equal(DescriptionMessage, errorDescription.ToString());
    }

    [Fact]
    public void DumpErrorToConsoleRendersLibraryContextExceptionAndInformation()
    {
        List<string> log = CaptureOutput(() =>
        {
            var details = new FlutterErrorDetails(
                exception: "Example exception",
                // Only the first frames: the xUnit runner's own stack is deep enough to exceed
                // `DebugPrintThrottled`'s 12 KiB window, which would defer the tail of the block.
                stack: string.Join(
                    '\n',
                    new System.Diagnostics.StackTrace(true).ToString().Split('\n').Take(5)),
                library: "Example library",
                context: new ErrorDescription("Example context"),
                informationCollector: () => [new ErrorDescription("Example information")]);

            FlutterError.DumpErrorToConsole(details);
        });

        Assert.True(log.Count > 3);
        Assert.Contains("EXAMPLE LIBRARY", log[0], StringComparison.Ordinal);
        Assert.Contains("Example context", log[1], StringComparison.Ordinal);
        Assert.Contains("Example exception", log[2], StringComparison.Ordinal);

        string joined = string.Join('\n', log);
        Assert.Contains("DumpErrorToConsoleRendersLibraryContextExceptionAndInformation", joined, StringComparison.Ordinal);
        Assert.Contains("\nExample information\n", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void FlutterErrorDetailsToStringRendersTheErrorBlock()
    {
        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY LIBRARY ╞════════════════════════════════\n"
            + "The following message was thrown CONTEXTING:\n"
            + "MESSAGE\n"
            + "\n"
            + "INFO\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(
                exception: "MESSAGE",
                library: "LIBRARY",
                context: new ErrorDescription("CONTEXTING"),
                informationCollector: () => [new ErrorDescription("INFO")]).ToString());

        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The following message was thrown CONTEXTING:\n"
            + "MESSAGE\n"
            + "\n"
            + "INFO\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(
                exception: "MESSAGE",
                context: new ErrorDescription("CONTEXTING"),
                informationCollector: () => [new ErrorDescription("INFO")]).ToString());

        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The following message was thrown CONTEXTING SomeContext(BlaBla):\n"
            + "MESSAGE\n"
            + "\n"
            + "INFO\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(
                exception: "MESSAGE",
                context: new ErrorDescription($"CONTEXTING {"SomeContext(BlaBla)"}"),
                informationCollector: () => [new ErrorDescription("INFO")]).ToString());

        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The following message was thrown:\n"
            + "MESSAGE\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(exception: "MESSAGE").ToString());
    }

    [Fact]
    public void FlutterErrorDetailsToStringShortNamesTheLibrary()
    {
        Assert.Equal(
            "Exception caught by library",
            new FlutterErrorDetails(
                exception: "MESSAGE",
                library: "library",
                context: new ErrorDescription("CONTEXTING"),
                informationCollector: () => [new ErrorDescription("INFO")]).ToStringShort());

        Assert.Equal("Exception caught", new FlutterErrorDetails("MESSAGE", library: null).ToStringShort());
    }

    [Fact]
    public void FlutterErrorMessageConstructorSplitsLines()
    {
        var error = new FlutterError(
            "My Error Summary.\n"
            + "My first description.\n"
            + "My second description.");
        Assert.Equal(3, error.Diagnostics.Count);
        Assert.Equal(DiagnosticLevel.Summary, error.Diagnostics[0].Level);
        Assert.Equal(DiagnosticLevel.Info, error.Diagnostics[1].Level);
        Assert.Equal(DiagnosticLevel.Info, error.Diagnostics[2].Level);
        Assert.Equal("My Error Summary.", error.Diagnostics[0].ToString());
        Assert.Equal("My first description.", error.Diagnostics[1].ToString());
        Assert.Equal("My second description.", error.Diagnostics[2].ToString());
        Assert.Equal(
            "FlutterError\n"
            + "   My Error Summary.\n"
            + "   My first description.\n"
            + "   My second description.\n",
            error.ToStringDeep());

        error = new FlutterError(
            "My Error Summary.\n"
            + "My first description.\n"
            + "My second description.\n"
            + "\n");
        Assert.Equal(5, error.Diagnostics.Count);
        Assert.Equal(string.Empty, error.Diagnostics[3].ToString());
        Assert.Equal(string.Empty, error.Diagnostics[4].ToString());
        Assert.Equal(
            "FlutterError\n"
            + "   My Error Summary.\n"
            + "   My first description.\n"
            + "   My second description.\n"
            + "\n"
            + "\n",
            error.ToStringDeep());

        error = new FlutterError(
            "My Error Summary.\n"
            + "My first description.\n"
            + "\n"
            + "My second description.");
        Assert.Equal(4, error.Diagnostics.Count);
        Assert.Equal(string.Empty, error.Diagnostics[2].ToString());
        Assert.Equal(
            "FlutterError\n"
            + "   My Error Summary.\n"
            + "   My first description.\n"
            + "\n"
            + "   My second description.\n",
            error.ToStringDeep());

        error = new FlutterError("My Error Summary.");
        Assert.Single(error.Diagnostics);
        Assert.Equal(DiagnosticLevel.Summary, error.Diagnostics[0].Level);
        Assert.Equal("My Error Summary.", error.Diagnostics[0].ToString());
        Assert.Equal(
            "FlutterError\n"
            + "   My Error Summary.\n",
            error.ToStringDeep());
    }

    [Fact]
    public void EmptyFlutterErrorIsReportedAsAnAssertion()
    {
        AssertionError error = Assert.Throws<AssertionError>(() => new FlutterError([]));

        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The following assertion was thrown:\n"
            + "Empty FlutterError\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(error).ToString());
    }

    [Fact]
    public void FlutterErrorWithoutASummaryIsReportedAsMalformed()
    {
        AssertionError error = Assert.Throws<AssertionError>(
            () => new FlutterError([new ErrorDescription("Error description without a summary")]));

        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The following assertion was thrown:\n"
            + "FlutterError is missing a summary.\n"
            + "All FlutterError objects should start with a short (one line)\n"
            + "summary description of the problem that was detected.\n"
            + "Malformed FlutterError:\n"
            + "  Error description without a summary\n"
            + "\n"
            + "This error should still help you solve your problem, however\n"
            + "please also report this malformed error in the framework by\n"
            + "filing a bug on GitHub:\n"
            + "  https://github.com/Plumix-Net/Plumix/issues/new\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(error).ToString());
    }

    [Fact]
    public void FlutterErrorWithMultipleSummariesIsReportedAsMalformed()
    {
        AssertionError error = Assert.Throws<AssertionError>(() => new FlutterError([
            new ErrorSummary("Error Summary A"),
            new ErrorDescription("Some descriptionA"),
            new ErrorSummary("Error Summary B"),
            new ErrorDescription("Some descriptionB"),
        ]));

        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The following assertion was thrown:\n"
            + "FlutterError contained multiple error summaries.\n"
            + "All FlutterError objects should have only a single short (one\n"
            + "line) summary description of the problem that was detected.\n"
            + "Malformed FlutterError:\n"
            + "  Error Summary A\n"
            + "  Some descriptionA\n"
            + "  Error Summary B\n"
            + "  Some descriptionB\n"
            + "\n"
            + "The malformed error has 2 summaries.\n"
            + "Summary 1: Error Summary A\n"
            + "Summary 2: Error Summary B\n"
            + "\n"
            + "This error should still help you solve your problem, however\n"
            + "please also report this malformed error in the framework by\n"
            + "filing a bug on GitHub:\n"
            + "  https://github.com/Plumix-Net/Plumix/issues/new\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(error).ToString());
    }

    [Fact]
    public void FlutterErrorWhoseSummaryIsNotFirstIsReportedAsMissingASummary()
    {
        AssertionError error = Assert.Throws<AssertionError>(() => new FlutterError([
            new ErrorDescription("Some description"),
            new ErrorSummary("Error summary"),
        ]));

        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The following assertion was thrown:\n"
            + "FlutterError is missing a summary.\n"
            + "All FlutterError objects should start with a short (one line)\n"
            + "summary description of the problem that was detected.\n"
            + "Malformed FlutterError:\n"
            + "  Some description\n"
            + "  Error summary\n"
            + "\n"
            + "This error should still help you solve your problem, however\n"
            + "please also report this malformed error in the framework by\n"
            + "filing a bug on GitHub:\n"
            + "  https://github.com/Plumix-Net/Plumix/issues/new\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(error).ToString());
    }

    [Fact]
    public void UserThrownExceptionsHaveErrorSummaryProperties()
    {
        DiagnosticsNode node = new FlutterErrorDetails("User thrown string").ToDiagnosticsNode();
        ErrorSummary summary = node.GetProperties().OfType<ErrorSummary>().Single();
        Assert.Equal<object>(["User thrown string"], summary.MessageParts);

        node = new FlutterErrorDetails(new ArgumentNullException("myArgument")).ToDiagnosticsNode();
        summary = node.GetProperties().OfType<ErrorSummary>().Single();
        Assert.Equal<object>(["Value cannot be null. (Parameter 'myArgument')"], summary.MessageParts);
    }

    [Fact]
    public void IdentifiesUserFault()
    {
        var details = new FlutterErrorDetails(new AssertionError("Test assertion"), stack: UserFaultStack);

        var builder = new DiagnosticPropertiesBuilder();
        details.DebugFillProperties(builder);

        Assert.Equal(4, builder.Properties.Count);
        Assert.Equal("The following assertion was thrown:", builder.Properties[0].ToString());
        Assert.Contains("Assertion failed", builder.Properties[1].ToString(), StringComparison.Ordinal);
        Assert.IsType<ErrorSpacer>(builder.Properties[2]);
        var trace = Assert.IsType<DiagnosticsStackTrace>(builder.Properties[3]);
        Assert.Equal(UserFaultStack, trace.Value);
    }

    [Fact]
    public void IdentifiesOurFault()
    {
        var details = new FlutterErrorDetails(new AssertionError("Test assertion"), stack: OurFaultStack);

        var builder = new DiagnosticPropertiesBuilder();
        details.DebugFillProperties(builder);

        Assert.Equal(6, builder.Properties.Count);
        Assert.Equal("The following assertion was thrown:", builder.Properties[0].ToString());
        Assert.Contains("Assertion failed", builder.Properties[1].ToString(), StringComparison.Ordinal);
        Assert.IsType<ErrorSpacer>(builder.Properties[2]);
        Assert.Equal(FrameworkBugHint, builder.Properties[3].ToString());
        Assert.IsType<ErrorSpacer>(builder.Properties[4]);
        var trace = Assert.IsType<DiagnosticsStackTrace>(builder.Properties[5]);
        Assert.Equal(OurFaultStack, trace.Value);
    }

    [Fact]
    public void AssertionErrorRendersDartsForm()
    {
        Assert.Equal("Assertion failed", new AssertionError().ToString());
        Assert.Equal("Assertion failed: \"Test assertion\"", new AssertionError("Test assertion").ToString());
    }

    [Fact]
    public void RepetitiveStackFrameFilterDoesNotGoOutOfRange()
    {
        var filter = new RepetitiveStackFrameFilter(
            [
                new PartialStackFrame("package:test/blah.dart", "TestClass", "test1"),
                new PartialStackFrame("package:test/blah.dart", "TestClass", "test2"),
                new PartialStackFrame("package:test/blah.dart", "TestClass", "test3"),
            ],
            "test");
        var reasons = new List<string?>([null, null]);
        filter.Filter(
            [
                new StackFrame(
                    number: 0, column: 1, line: 1, packageScheme: "package", package: "test",
                    packagePath: "blah.dart", className: "TestClass", method: "test1", source: string.Empty),
                new StackFrame(
                    number: 0, column: 1, line: 1, packageScheme: "package", package: "test",
                    packagePath: "blah.dart", className: "TestClass", method: "test2", source: string.Empty),
            ],
            reasons);

        Assert.Equal([null, null], reasons);
    }

    [Fact]
    public void RepetitiveStackFrameFilterCollapsesMatchedRuns()
    {
        var filter = new RepetitiveStackFrameFilter(
            [new PartialStackFrame("dotnet:Plumix/Widgets/Element", "Element", "UpdateChild")],
            "...     Normal element mounting");
        FlutterError.AddDefaultStackFilter(filter);
        try
        {
            string frame = "   at Plumix.Widgets.Element.UpdateChild(Element child)";
            List<string> filtered = [.. FlutterError.DefaultStackFilter([frame, frame, frame, "   at App.Main()"])];

            // Dart's collapse suffix is literally `index - start + 2`, so a run of three matched
            // frames reports "(4 frames)"; the quirk is ported as-is.
            Assert.Equal(
                ["...     Normal element mounting (4 frames)", "   at App.Main()"],
                filtered);
        }
        finally
        {
            FlutterError.ClearDefaultStackFilters();
        }
    }

    [Fact]
    public void DefaultStackFilterElidesRuntimeFrames()
    {
        List<string> filtered = [.. FlutterError.DefaultStackFilter([
            "#0      _AssertionError._doThrowNew (dart:core-patch/errors_patch.dart:42:39)",
            "#1      main (package:test/test.dart:1:1)",
        ])];

        Assert.Equal(
            [
                "#1      main (package:test/test.dart:1:1)",
                "(elided one frame from class _AssertionError)",
            ],
            filtered);
    }

    [Fact]
    public void DefaultStackFilterNamesEveryElidedSource()
    {
        List<string> filtered = [.. FlutterError.DefaultStackFilter([
            "#0      _AssertionError._doThrowNew (dart:core-patch/errors_patch.dart:42:39)",
            "#1      _Timer._runTimers (dart:isolate-patch/timer_impl.dart:398:19)",
            "#2      Future.sync (dart:async/future.dart:224:31)",
            "#3      main (package:test/test.dart:1:1)",
        ])];

        Assert.Equal(
            [
                "#3      main (package:test/test.dart:1:1)",
                "(elided 3 frames from class _AssertionError, class _Timer, and dart:async)",
            ],
            filtered);
    }

    [Fact]
    public void DumpErrorToConsoleAbbreviatesRepeatErrors()
    {
        FlutterError.ResetErrorCount();
        try
        {
            List<string> log = CaptureOutput(() =>
            {
                FlutterError.DumpErrorToConsole(new FlutterErrorDetails("Message goes here."));
                FlutterError.DumpErrorToConsole(new FlutterErrorDetails("Message goes here."));
            });

            Assert.Equal("Another exception was thrown: Message goes here.", log[^1]);
            Assert.Contains("══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞", log[0], StringComparison.Ordinal);
        }
        finally
        {
            FlutterError.ResetErrorCount();
        }
    }

    [Fact]
    public void DumpErrorToConsoleForceReportRendersTheFullBlockAgain()
    {
        FlutterError.ResetErrorCount();
        try
        {
            List<string> log = CaptureOutput(() =>
            {
                FlutterError.DumpErrorToConsole(new FlutterErrorDetails("Message goes here."));
                FlutterError.DumpErrorToConsole(new FlutterErrorDetails("Message goes here."), forceReport: true);
            });

            Assert.Equal(2, log.Count(line => line.Contains("Message goes here.", StringComparison.Ordinal)));
            Assert.DoesNotContain(log, line => line.StartsWith("Another exception", StringComparison.Ordinal));
        }
        finally
        {
            FlutterError.ResetErrorCount();
        }
    }

    [Fact]
    public void EmptyStackRendersWithoutATrailingColon()
    {
        FlutterError.ResetErrorCount();
        try
        {
            List<string> log = CaptureOutput(() => FlutterError.DumpErrorToConsole(
                new FlutterErrorDetails("Message goes here.", stack: string.Empty)));

            Assert.Contains("When the exception was thrown, this was the stack", log);
        }
        finally
        {
            FlutterError.ResetErrorCount();
        }
    }

    [Fact]
    public void StackTracesAreNotTruncated()
    {
        FlutterError.ResetErrorCount();
        try
        {
            string stack = string.Join(
                '\n',
                Enumerable.Range(0, 11).Select(i => $"   at Plumix.Test.Frame{i}.Run()"));
            List<string> log = CaptureOutput(() => FlutterError.DumpErrorToConsole(
                new FlutterErrorDetails("Message goes here.", stack: stack)));

            for (int i = 0; i < 11; i++)
            {
                Assert.Contains(log, line => line.EndsWith($"at Plumix.Test.Frame{i}.Run()", StringComparison.Ordinal));
            }
        }
        finally
        {
            FlutterError.ResetErrorCount();
        }
    }

    [Fact]
    public void ReportErrorRoutesThroughOnErrorAndCanBeSilenced()
    {
        FlutterExceptionHandler? previous = FlutterError.OnError;
        try
        {
            var seen = new List<FlutterErrorDetails>();
            FlutterError.OnError = seen.Add;
            var details = new FlutterErrorDetails("boom");
            FlutterError.ReportError(details);
            Assert.Same(details, Assert.Single(seen));

            FlutterError.OnError = null;
            FlutterError.ReportError(details);
            Assert.Single(seen);
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void PropertiesTransformersRewriteTheReportedProperties()
    {
        IEnumerable<DiagnosticsNode> Transformer(IEnumerable<DiagnosticsNode> properties)
            => [.. properties, new ErrorDescription("EXTRA")];

        FlutterErrorDetails.PropertiesTransformers.Add(Transformer);
        try
        {
            Assert.Contains(
                "EXTRA",
                new FlutterErrorDetails("MESSAGE").ToString(),
                StringComparison.Ordinal);
        }
        finally
        {
            FlutterErrorDetails.PropertiesTransformers.Remove(Transformer);
        }
    }

    [Fact]
    public void CopyWithReplacesOnlyTheGivenFields()
    {
        var original = new FlutterErrorDetails("MESSAGE", library: "one", silent: true);
        FlutterErrorDetails copy = original.CopyWith(library: "two");

        Assert.Equal("two", copy.Library);
        Assert.True(copy.Silent);
        Assert.Equal("MESSAGE", copy.Exception);
        Assert.Equal("one", original.Library);
    }

    [Fact]
    public void ExceptionAsStringFallsBackToANoMessagePlaceholder()
    {
        Assert.Equal("  <no message available>", new FlutterErrorDetails(string.Empty).ExceptionAsString());
        Assert.Equal("  42", new FlutterErrorDetails(42).ExceptionAsString());
        Assert.Equal(
            "InvalidOperationException: boom",
            new FlutterErrorDetails(new InvalidOperationException("boom")).ExceptionAsString());
    }

    [Fact]
    public void NumericExceptionsGetTheNumberPhrasing()
    {
        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY PLUMIX FRAMEWORK ╞═══════════════════════\n"
            + "The number 42 was thrown.\n"
            + "═════════════════════════════════════════════════════════════════\n",
            new FlutterErrorDetails(42).ToString());
    }

    [Fact]
    public void NonExceptionObjectsAreDescribedAsObjects()
    {
        Assert.Contains(
            "The following Object object was thrown:",
            new FlutterErrorDetails(new object()).ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryUnwrapsAFlutterErrorCarriedByAnAssertion()
    {
        var error = new FlutterError("Summary line.\nDescription line.");
        Assert.Equal("Summary line.", new FlutterErrorDetails(error).Summary.ToString());
        Assert.Equal(
            "Summary line.",
            new FlutterErrorDetails(new AssertionError(error)).Summary.ToString());
        Assert.Equal("plain", new FlutterErrorDetails("plain").Summary.ToString());
    }

    [Fact]
    public void DiagnosticsStackTraceSingleFrameShowsOneFrame()
    {
        DiagnosticsStackTrace trace = DiagnosticsStackTrace.SingleFrame("Origin", "   at App.Main()");

        Assert.Equal("Origin", trace.Name);
        Assert.Equal("   at App.Main()", Assert.Single(trace.GetProperties()).ToString());
        Assert.False(trace.AllowTruncate);
    }

    /// Runs `body` with the console sink captured, the way Flutter's `capture_output.dart` does by
    /// overriding the zone's `print`. [Print.DebugPrintThrottled] is the default sink and splits the
    /// dumped block into one entry per line, which is what makes the indexed assertions above line
    /// up with Flutter's.
    internal static List<string> CaptureOutput(Action body)
    {
        var log = new List<string>();
        Action<string> previousSink = Print.PrintLine;
        DebugPrintCallback previousPrint = Print.DebugPrint;
        Print.ResetThrottleForTesting();
        Print.DebugPrint = Print.DebugPrintThrottled;
        Print.PrintLine = log.Add;
        FlutterError.ResetErrorCount();
        try
        {
            body();
        }
        finally
        {
            Print.PrintLine = previousSink;
            Print.DebugPrint = previousPrint;
            Print.ResetThrottleForTesting();
            FlutterError.ResetErrorCount();
        }

        return log;
    }
}
