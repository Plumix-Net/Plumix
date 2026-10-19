using Plumix.Material;
using Plumix.Widgets;
using Xunit;

// Dart parity source: dart_sample/lib/demos/general/restoration_demo_page.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationSampleTests : IDisposable
{
    public RestorationSampleTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    [Fact]
    public void Gallery_RestoresTheOpenDemoCounterNoteAndScrollAfterRestart()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new CounterApp());
        tester.PumpAndSettle();
        tester.State<NavigatorState>(Find.ByType<Navigator>().Last).RestorablePushNamed("/restoration");
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.Text("State restoration: enabled")));

        tester.Tap(Find.Text("Increment"));
        tester.Pump();
        tester.Tap(Find.Text("Increment"));
        tester.Pump();
        tester.EnterText(Find.ByType<TextField>(), "Saved through the native channel");
        tester.Widget<TextField>(Find.ByType<TextField>()).FocusNode?.Unfocus();
        tester.Pump();
        Finder scroll = Find.ByWidgetPredicate(widget => widget is Scrollable { RestorationId: "scroll" });
        tester.State<ScrollableState>(scroll).Position.JumpTo(400);
        tester.Pump();

        tester.RestartAndRestore();
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());

        Assert.True(tester.Any(Find.ByType<RestorationDemoPage>()));
        Assert.True(tester.Any(Find.Text("Restorable counter: 2")));
        Assert.Equal(
            "Saved through the native channel",
            tester.Widget<TextField>(Find.ByType<TextField>()).Controller!.Text);
        Assert.Equal(400, tester.State<ScrollableState>(scroll).Position.Pixels, precision: 3);
    }
}
