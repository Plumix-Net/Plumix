using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/error_widget_builder_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ErrorWidgetBuilderDartParityTests
{
    // Flutter: error_widget_builder_test.dart: "ErrorWidget.builder" (the first one)
    [Fact]
    public void ErrorWidgetBuilder()
    {
        ErrorWidgetBuilder oldBuilder = ErrorWidget.Builder;
        try
        {
            using var tester = new FrameworkDartTester();
            ErrorWidget.Builder = _ => new Text("oopsie!", textDirection: TextDirection.Ltr);
            tester.PumpWidget(new SizedBox(child: new Builder(_ => throw new ThrownString("test"))));
            Assert.Equal("test", tester.TakeException()!.ToString());
            Assert.Single(tester.ElementsWithText("oopsie!"));
        }
        finally
        {
            ErrorWidget.Builder = oldBuilder;
        }
    }

    // Flutter: error_widget_builder_test.dart: "ErrorWidget.builder" (the second one)
    [Fact]
    public void ErrorWidgetBuilderWithAnEmptyMessagePaintsNoParagraph()
    {
        ErrorWidgetBuilder oldBuilder = ErrorWidget.Builder;
        try
        {
            using var tester = new FrameworkDartTester();
            ErrorWidget.Builder = _ => new ErrorWidget(string.Empty);
            tester.PumpWidget(new SizedBox(child: new Builder(_ => throw new ThrownString("test"))));
            Assert.Equal("test", tester.TakeException()!.ToString());

            // `isNot(paints..paragraph())`: flutter_test paints the render object through a
            // TestRecordingPaintingContext; the matcher fails when nothing is painted or when no
            // drawParagraph call is recorded, so the negation holds when no paragraph is drawn.
            RenderObject renderObject = tester.ElementOfType<ErrorWidget>().FindRenderObject()!;
            var context = new TestRecordingPaintingContext();
            renderObject.Paint(context, default);
            Assert.DoesNotContain(context.Calls, call => call.Method == "drawParagraph");
        }
        finally
        {
            ErrorWidget.Builder = oldBuilder;
        }
    }

    /// <summary>Dart's <c>throw 'test'</c>: C# can only throw exceptions, so this one prints as the string.</summary>
    private sealed class ThrownString(string value) : Exception(value)
    {
        public override string ToString() => Message;
    }

    /// <summary>flutter_test's <c>TestRecordingPaintingContext</c>: children paint inline.</summary>
    private sealed class TestRecordingPaintingContext() : PaintingContext(new OffsetLayer(), default)
    {
        private readonly Canvas _canvas = new(new PictureRecorder());

        public override Canvas Canvas => _canvas;

        public IReadOnlyList<CanvasCall> Calls => _canvas.DebugCalls;

        public override void PaintChild(RenderObject child, Point offset) => child.Paint(this, offset);
    }
}
