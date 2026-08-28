using Plumix.Foundation;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity tests for the ported `foundation/stack_frame.dart`. The Dart VM cases come from Flutter's
/// own `test/foundation/stack_frame_test.dart`; the CLR cases cover the grammar that replaces
/// Dart's dart2js/DDC web-frame parsers (see `docs/ai/DIVERGENCES.md`).
/// </summary>
public class StackFrameTests
{
    [Fact]
    public void ParsesAVmLine()
    {
        const string Line = "#1      _AssertionError._throwNew (dart:core-patch/errors_patch.dart:40:5)";

        StackFrame? frame = StackFrame.FromStackTraceLine(Line);

        Assert.NotNull(frame);
        Assert.Equal(1, frame.Number);
        Assert.Equal("dart", frame.PackageScheme);
        Assert.Equal("core-patch", frame.Package);
        Assert.Equal("errors_patch.dart", frame.PackagePath);
        Assert.Equal(40, frame.Line);
        Assert.Equal(5, frame.Column);
        Assert.Equal("_AssertionError", frame.ClassName);
        Assert.Equal("_throwNew", frame.Method);
        Assert.Equal(Line, frame.Source);
    }

    [Fact]
    public void ParsesAConstructorFrame()
    {
        StackFrame frame = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("#2      new Text (package:flutter/src/widgets/text.dart:287:10)"));

        Assert.Equal("Text", frame.ClassName);
        Assert.Equal(string.Empty, frame.Method);
        Assert.True(frame.IsConstructor);
        Assert.Equal("flutter", frame.Package);
        Assert.Equal("src/widgets/text.dart", frame.PackagePath);
    }

    [Fact]
    public void ParsesAFrameWithoutLineOrColumn()
    {
        StackFrame frame = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("#3      Element.updateChild (package:flutter/src/widgets/framework.dart)"));

        Assert.Equal(-1, frame.Line);
        Assert.Equal(-1, frame.Column);
        Assert.Equal("Element", frame.ClassName);
        Assert.Equal("updateChild", frame.Method);
    }

    [Fact]
    public void ParsesAFrameWithoutAColumn()
    {
        StackFrame frame = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("#4      main (package:test/test.dart:12)"));

        Assert.Equal(12, frame.Line);
        Assert.Equal(-1, frame.Column);
        Assert.Equal(string.Empty, frame.ClassName);
        Assert.Equal("main", frame.Method);
    }

    [Fact]
    public void StripsAnonymousClosureMarkers()
    {
        StackFrame frame = Assert.IsType<StackFrame>(StackFrame.FromStackTraceLine(
            "#0      getSampleStack.<anonymous closure> (package:test/test.dart:1:1)"));

        Assert.Equal(string.Empty, frame.ClassName);
        Assert.Equal("getSampleStack", frame.Method);
    }

    [Fact]
    public void ParsesAConstructorFrameWithAnUnknownClassName()
    {
        const string Line = "#32     new (http://localhost:42191/dart-sdk/lib/async/stream_controller.dart:880:9)";

        StackFrame frame = Assert.IsType<StackFrame>(StackFrame.FromStackTraceLine(Line));

        Assert.Equal(32, frame.Number);
        Assert.Equal("<unknown>", frame.ClassName);
        Assert.Equal(string.Empty, frame.Method);
        Assert.Equal("http", frame.PackageScheme);
        Assert.Equal("<unknown>", frame.Package);
        Assert.Equal(880, frame.Line);
        Assert.Equal(9, frame.Column);
    }

    [Fact]
    public void ParsesTheWellKnownMarkers()
    {
        Assert.Same(StackFrame.AsynchronousSuspension, StackFrame.FromStackTraceLine("<asynchronous suspension>"));
        Assert.Same(StackFrame.StackOverFlowElision, StackFrame.FromStackTraceLine("..."));
    }

    [Fact]
    public void ReturnsNullForTheWrongFormat()
    {
        Assert.Null(StackFrame.FromStackTraceLine("wrong stack trace format"));
    }

    [Fact]
    public void ParsesAWholeVmStack()
    {
        const string Stack = """
            #0      _AssertionError._doThrowNew (dart:core-patch/errors_patch.dart:42:39)
            #1      _AssertionError._throwNew (dart:core-patch/errors_patch.dart:38:5)
            <asynchronous suspension>
            #2      main (package:test/test.dart:1:1)
            """;

        List<StackFrame> frames = StackFrame.FromStackString(Stack);

        Assert.Equal(4, frames.Count);
        Assert.Equal("_doThrowNew", frames[0].Method);
        Assert.Same(StackFrame.AsynchronousSuspension, frames[2]);
        Assert.Equal("main", frames[3].Method);
    }

    [Fact]
    public void ParsesAClrFrame()
    {
        const string Line =
            "   at Plumix.Widgets.Element.UpdateChild(Element child, Widget newWidget) "
            + "in /src/Plumix/Widgets/Framework.Element.cs:line 3070";

        StackFrame frame = Assert.IsType<StackFrame>(StackFrame.FromStackTraceLine(Line));

        Assert.Equal(-1, frame.Number);
        Assert.Equal("dotnet", frame.PackageScheme);
        Assert.Equal("Plumix", frame.Package);
        Assert.Equal("Widgets/Element", frame.PackagePath);
        Assert.Equal("Element", frame.ClassName);
        Assert.Equal("UpdateChild", frame.Method);
        Assert.Equal(3070, frame.Line);
        Assert.Equal(-1, frame.Column);
        Assert.Equal(Line, frame.Source);
    }

    [Fact]
    public void ParsesAClrFrameWithoutFileInformation()
    {
        StackFrame frame = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("   at System.Threading.Tasks.Task.Execute()"));

        Assert.Equal("System", frame.Package);
        Assert.Equal("Threading/Tasks/Task", frame.PackagePath);
        Assert.Equal("Task", frame.ClassName);
        Assert.Equal("Execute", frame.Method);
        Assert.Equal(-1, frame.Line);
    }

    [Fact]
    public void ParsesAClrConstructorAndGenericFrame()
    {
        StackFrame constructor = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("   at Plumix.Widgets.Text..ctor(String data)"));
        Assert.True(constructor.IsConstructor);
        Assert.Equal("Text", constructor.ClassName);
        Assert.Equal(string.Empty, constructor.Method);

        StackFrame generic = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("   at Plumix.Widgets.Inherited.Of[T](BuildContext context)"));
        Assert.Equal("Inherited", generic.ClassName);
        Assert.Equal("Of", generic.Method);
    }

    [Fact]
    public void ParsesTheClrAsynchronousGap()
    {
        StackFrame frame = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("--- End of stack trace from previous location ---"));

        Assert.Equal("asynchronous suspension", frame.Method);
        Assert.Equal("--- End of stack trace from previous location ---", frame.Source);
    }

    [Fact]
    public void ParsesARealClrStackTrace()
    {
        List<StackFrame> frames = StackFrame.FromStackString(new System.Diagnostics.StackTrace(true).ToString());

        Assert.NotEmpty(frames);
        Assert.Equal("ParsesARealClrStackTrace", frames[0].Method);
        Assert.Equal("StackFrameTests", frames[0].ClassName);
        Assert.Equal("Plumix", frames[0].Package);
    }

    [Fact]
    public void EqualityIgnoresSchemePathAndConstructorFlag()
    {
        var left = new StackFrame(
            number: 1, column: 2, line: 3, packageScheme: "package", package: "test",
            packagePath: "a.dart", className: "C", method: "m", source: "src");
        var right = new StackFrame(
            number: 1, column: 2, line: 3, packageScheme: "dart", package: "test",
            packagePath: "b.dart", className: "C", method: "m", source: "src", isConstructor: true);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void ToStringDescribesTheFrame()
    {
        var frame = new StackFrame(
            number: 1, column: 2, line: 3, packageScheme: "package", package: "test",
            packagePath: "a.dart", className: "C", method: "m", source: "src");

        Assert.Equal("StackFrame(#1, package:test/a.dart:3:2, className: C, method: m)", frame.ToString());
    }

    [Fact]
    public void PartialStackFrameMatchesOnPackageClassAndMethod()
    {
        var partial = new PartialStackFrame("dotnet:Plumix/Widgets/Element", "Element", "UpdateChild");
        StackFrame frame = Assert.IsType<StackFrame>(
            StackFrame.FromStackTraceLine("   at Plumix.Widgets.Element.UpdateChild(Element child)"));

        Assert.True(partial.Matches(frame));
        Assert.False(new PartialStackFrame("dotnet:Plumix/Widgets/Element", "Element", "Mount").Matches(frame));
        Assert.False(new PartialStackFrame("dotnet:Other/Element", "Element", "UpdateChild").Matches(frame));
    }
}
