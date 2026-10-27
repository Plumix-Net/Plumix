using System.Collections;
using System.Reflection;
using Avalonia;
using Plumix.Cupertino;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: dart_sample/lib/sample_gallery_screen.dart (the gallery's Cupertino tab and its
// CupertinoApp shell demo, mounted through the framework host on desktop)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SampleCupertinoTabTests : IDisposable
{
    public SampleCupertinoTabTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    public static TheoryData<string> CupertinoTabRoutes()
    {
        Type gallery = typeof(CounterApp).Assembly.GetType("Plumix.SampleGalleryScreen", throwOnError: true)!;
        var pages = (IEnumerable)gallery
            .GetField("CupertinoDemoPages", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        var data = new TheoryData<string>();
        foreach (object page in pages)
        {
            data.Add((string)page.GetType().GetProperty("RouteName")!.GetValue(page)!);
        }

        return data;
    }

    [Fact]
    public void CupertinoTab_OpensTheCupertinoAppShell_AndNavigatesItsRoutes()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new CounterApp());
        tester.PumpAndSettle();

        tester.Tap(Find.Text("Cupertino"));
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.Text("Adaptive Cupertino behavior probes for controls.")));

        tester.Tap(Find.TextContaining("Application shell"));
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.ByType<CupertinoApp>()));
        Assert.True(tester.Any(Find.Text("CupertinoApp")));

        tester.Tap(Find.Text("Push Cupertino route"));
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.Text("Details route")));
        Assert.False(tester.Any(Find.Text("Push Cupertino route")));

        tester.Tap(Find.Text("Pop route"));
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.Text("Push Cupertino route")));
        Assert.False(tester.Any(Find.Text("Details route")));
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [MemberData(nameof(CupertinoTabRoutes))]
    public void CupertinoTabRoute_OpensWithoutErrors(string route)
    {
        // Same pump sequence as SampleScreenCaptureTests; the tester fails on any untaken error. The
        // demo pages are laid out for a desktop window, so a tall view keeps their columns from
        // overflowing the 800x600 test default.
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.View.DevicePixelRatio = 1.0;
        tester.View.PhysicalSize = new Size(1280, 2400);
        tester.PumpWidget(new CounterApp());
        tester.Pump();
        tester.State<NavigatorState>(Find.ByType<Navigator>().Last).RestorablePushNamed(route);
        tester.Pump();
        tester.Pump(TimeSpan.FromSeconds(1));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.False(tester.Any(Find.TextContaining("Unknown route")));
        Assert.Null(tester.TakeException());
        tester.PumpWidget(new SizedBox());
        tester.Pump(TimeSpan.FromMinutes(1));
    }
}
