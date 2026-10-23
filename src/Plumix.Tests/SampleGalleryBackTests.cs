using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: dart_sample/lib/sample_gallery_screen.dart (system back in the nested gallery navigator)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SampleGalleryBackTests : IDisposable
{
    public SampleGalleryBackTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    [Fact]
    public void SystemBack_PopsTheOpenDemoPage_ThenHandsTheGalleryRootToThePlatform()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new CounterApp());
        tester.PumpAndSettle();
        tester.State<NavigatorState>(Find.ByType<Navigator>().Last).RestorablePushNamed("/restoration");
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.ByType<RestorationDemoPage>()));

        // WidgetsApp pops only the root navigator; the gallery's NavigatorPopHandler hands the press to the
        // nested one that holds the demo page.
        Task<bool> popped = WidgetsBinding.Instance.HandlePopRoute();
        tester.PumpAndSettle();
        Assert.True(popped.IsCompletedSuccessfully && popped.Result);
        Assert.False(tester.Any(Find.ByType<RestorationDemoPage>()));
        Assert.DoesNotContain("SystemNavigator.pop", platform.Methods);

        // At the gallery root nothing pops, so the framework asks the platform to pop the app.
        Task<bool> atRoot = WidgetsBinding.Instance.HandlePopRoute();
        tester.PumpAndSettle();
        Assert.True(atRoot.IsCompletedSuccessfully);
        Assert.False(atRoot.Result);
        Assert.Contains("SystemNavigator.pop", platform.Methods);
        Assert.Null(tester.TakeException());
    }
}
