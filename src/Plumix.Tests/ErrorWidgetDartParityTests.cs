using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/error_widget_test.dart (ErrorWidgetTests pins the
// RenderErrorBox/ErrorWidget contracts at unit level; these are the widget-tree tests).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ErrorWidgetDartParityTests
{
    private static readonly Color Red = Color.FromUInt32(0xffff0000);

    // Flutter: error_widget_test.dart: "ErrorWidget displays actual error when throwing during build"
    [Fact]
    public void ErrorWidgetDisplaysActualErrorWhenThrowingDuringBuild()
    {
        using var tester = new FrameworkDartTester();
        Key container = new UniqueKey();
        const string errorText = "Oh no, there was a crash!!1";

        tester.PumpWidget(new Container(
            key: container,
            color: Red,
            padding: EdgeInsets.All(10),
            child: new Builder(_ => throw new NotSupportedException(errorText))));

        NotSupportedException error = Assert.IsType<NotSupportedException>(tester.TakeException());
        Assert.Contains(errorText, error.Message);

        var errorWidget = (ErrorWidget)tester.ElementOfType<ErrorWidget>().Widget;
        Assert.Contains(errorText, errorWidget.Message);

        // Failure in one widget shouldn't ripple through the entire tree and effect
        // ancestors. Those should still be in the tree.
        Assert.Single(tester.ElementsWithKey(container));
    }

    // Flutter: error_widget_test.dart:
    // "when constructing an ErrorWidget due to a build failure throws an error, fail gracefully"
    [DebugOnlyFact]
    public void WhenConstructingAnErrorWidgetDueToABuildFailureThrowsAnErrorFailGracefully()
    {
        using var tester = new FrameworkDartTester();
        Key container = new UniqueKey();
        tester.PumpWidget(new Container(
            key: container,
            color: Red,
            padding: EdgeInsets.All(10),
            // This widget throws during build, which causes the construction of an
            // ErrorWidget with the build error. However, during construction of
            // that ErrorWidget, another error is thrown.
            child: new MyDoubleThrowingWidget()));

        NotSupportedException error = Assert.IsType<NotSupportedException>(tester.TakeException());
        Assert.Contains(MyThrowingElement.DebugFillPropertiesErrorMessage, error.Message);

        var errorWidget = (ErrorWidget)tester.ElementOfType<ErrorWidget>().Widget;
        Assert.Contains(MyThrowingElement.DebugFillPropertiesErrorMessage, errorWidget.Message);

        // Failure in one widget shouldn't ripple through the entire tree and effect
        // ancestors. Those should still be in the tree.
        Assert.Single(tester.ElementsWithKey(container));
    }

    // Flutter: error_widget_test.dart: "ErrorWidget does not crash at zero area"
    [Fact]
    public void ErrorWidgetDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: SizedBox.Shrink(child: new ErrorWidget("Exception")))));
        Assert.Equal(new Size(0, 0), tester.GetSize(tester.ElementOfType<ErrorWidget>()));
    }

    // This widget throws during its regular build and then again when the
    // ErrorWidget is constructed, which calls MyThrowingElement.debugFillProperties.
    private sealed class MyDoubleThrowingWidget(Key? key = null) : StatelessWidget(key)
    {
        public override Element CreateElement() => new MyThrowingElement(this);

        public override Widget Build(BuildContext context)
            => throw new NotSupportedException("You cannot build me!");
    }

    private sealed class MyThrowingElement(StatelessWidget widget) : StatelessElement(widget)
    {
        public const string DebugFillPropertiesErrorMessage = "Crash during debugFillProperties";

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            throw new NotSupportedException(DebugFillPropertiesErrorMessage);
        }
    }
}
