using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: material_ui/test/snack_bar_test.dart — the cases that wait for a snack bar's
// display timer, which expires on the tester's pump clock (flutter_test's FakeAsync).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SnackBarDartParityTests
{
    private static readonly Key TapTarget = new ValueKey<string>("tap-target");

    [Fact]
    public void SnackBarControlTest()
    {
        const string helloSnackBar = "Hello SnackBar";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(TapTargetApp(context => ScaffoldMessenger.Of(context).ShowSnackBar(
            new SnackBar(content: new Text(helloSnackBar), duration: TimeSpan.FromSeconds(2)))));

        Finds.Nothing(Find.Text(helloSnackBar));
        tester.Tap(Find.ByKey(TapTarget));
        Finds.Nothing(Find.Text(helloSnackBar));
        tester.Pump(); // schedule animation
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(); // begin animation
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 0.75s // animation last frame; two second timer starts here
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 1.50s
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 2.25s
        Finds.OneWidget(Find.Text(helloSnackBar));
        // 3.00s // timer triggers to dismiss snackbar, reverse animation is scheduled
        tester.Pump(TimeSpan.FromMilliseconds(750));
        tester.Pump(); // begin animation
        Finds.OneWidget(Find.Text(helloSnackBar)); // frame 0 of dismiss animation
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 3.75s // last frame of animation, snackbar removed from build
        Finds.Nothing(Find.Text(helloSnackBar));
    }

    [Fact]
    public void SnackBarTwiceTest()
    {
        int snackBarCount = 0;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(TapTargetApp(context =>
        {
            snackBarCount += 1;
            ScaffoldMessenger.Of(context).ShowSnackBar(new SnackBar(
                content: new Text($"bar{snackBarCount}"),
                duration: TimeSpan.FromSeconds(2)));
        }));

        void Expect(bool bar1, bool bar2)
        {
            if (bar1) Finds.OneWidget(Find.Text("bar1")); else Finds.Nothing(Find.Text("bar1"));
            if (bar2) Finds.OneWidget(Find.Text("bar2")); else Finds.Nothing(Find.Text("bar2"));
        }

        Expect(false, false);
        tester.Tap(Find.ByKey(TapTarget)); // queue bar1
        tester.Tap(Find.ByKey(TapTarget)); // queue bar2
        Expect(false, false);
        tester.Pump(); // schedule animation for bar1
        Expect(true, false);
        tester.Pump(); // begin animation
        Expect(true, false);
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 0.75s // animation last frame; two second timer starts here
        Expect(true, false);
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 1.50s
        Expect(true, false);
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 2.25s
        Expect(true, false);
        // 3.00s // timer triggers to dismiss snackbar, reverse animation is scheduled
        tester.Pump(TimeSpan.FromMilliseconds(750));
        tester.Pump(); // begin animation
        Expect(true, false);
        // 3.75s // last frame of animation, snackbar removed from build, new snack bar put in its place
        tester.Pump(TimeSpan.FromMilliseconds(750));
        Expect(false, true);
        tester.Pump(); // begin animation
        Expect(false, true);
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 4.50s // animation last frame; two second timer starts here
        Expect(false, true);
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 5.25s
        Expect(false, true);
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 6.00s
        Expect(false, true);
        // 6.75s // timer triggers to dismiss snackbar, reverse animation is scheduled
        tester.Pump(TimeSpan.FromMilliseconds(750));
        tester.Pump(); // begin animation
        Expect(false, true);
        // 7.50s // last frame of animation, snackbar removed from build, new snack bar put in its place
        tester.Pump(TimeSpan.FromMilliseconds(750));
        Expect(false, false);
    }

    [Fact]
    public async Task SnackBarClosedReason_TimesOutWithoutAnAction()
    {
        // The timeout part of Dart's 'SnackBarClosedReason': a snack bar with an action never times out,
        // so the test re-pumps the app without one.
        Task<SnackBarClosedReason>? closed = null;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new Scaffold(body: new Builder(context => new GestureDetector(
            onTap: () => closed = ScaffoldMessenger.Of(context).ShowSnackBar(new SnackBar(
                content: new Text("snack"),
                duration: TimeSpan.FromSeconds(2))).Closed,
            child: new Text("X"))))));

        // Pop up the snack bar and then let it time out.
        tester.Tap(Find.Text("X"));
        tester.Pump(TimeSpan.FromMilliseconds(750));
        tester.Pump(TimeSpan.FromMilliseconds(750));
        Assert.False(closed!.IsCompleted);
        tester.Pump(TimeSpan.FromMilliseconds(1500));
        tester.Pump(); // begin animation
        tester.PumpAndSettle(TimeSpan.FromSeconds(1));
        Assert.True(closed.IsCompletedSuccessfully);
        Assert.Equal(SnackBarClosedReason.Timeout, await closed);
    }

    [Fact]
    public void AccessibleNavigationBehaviorWithAction()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new MediaQuery(
            data: new MediaQueryData(AccessibleNavigation: true),
            child: new ScaffoldMessenger(new Builder(context => new Scaffold(body: new GestureDetector(
                onTap: () => ScaffoldMessenger.Of(context).ShowSnackBar(new SnackBar(
                    content: new Text("snack"),
                    duration: TimeSpan.FromSeconds(1),
                    action: new SnackBarAction(label: "ACTION", onPressed: () => { }))),
                child: new Text("X"))))))));

        tester.Tap(Find.Text("X"));
        tester.Pump();
        // Find action immediately
        Finds.OneWidget(Find.Text("ACTION"));
        // Snackbar doesn't close
        tester.Pump(TimeSpan.FromSeconds(10));
        Finds.OneWidget(Find.Text("ACTION"));
        tester.Tap(Find.Text("ACTION"));
        tester.Pump();
        // Snackbar closes immediately
        Finds.Nothing(Find.Text("ACTION"));
    }

    [Fact]
    public void SnackBarDefaultDisplayDurationTest()
    {
        const string helloSnackBar = "Hello SnackBar";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(TapTargetApp(context => ScaffoldMessenger.Of(context).ShowSnackBar(
            new SnackBar(content: new Text(helloSnackBar)))));

        Finds.Nothing(Find.Text(helloSnackBar));
        tester.Tap(Find.ByKey(TapTarget));
        Finds.Nothing(Find.Text(helloSnackBar));
        tester.Pump(); // schedule animation
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(); // begin animation
        Finds.OneWidget(Find.Text(helloSnackBar));
        // 0.75s // animation last frame; four second timer starts here
        tester.Pump(TimeSpan.FromMilliseconds(750));
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 1.50s
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 2.25s
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 3.00s
        Finds.OneWidget(Find.Text(helloSnackBar));
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 3.75s
        Finds.OneWidget(Find.Text(helloSnackBar));
        // 4.75s // timer triggers to dismiss snackbar, reverse animation is scheduled
        tester.Pump(TimeSpan.FromMilliseconds(1000));
        tester.Pump(); // begin animation
        Finds.OneWidget(Find.Text(helloSnackBar)); // frame 0 of dismiss animation
        tester.Pump(TimeSpan.FromMilliseconds(750)); // 5.50s // last frame of animation, snackbar removed from build
        Finds.Nothing(Find.Text(helloSnackBar));
    }

    private static Widget TapTargetApp(Action<BuildContext> onTap) => new MaterialApp(
        home: new Scaffold(body: new Builder(context => new GestureDetector(
            key: TapTarget,
            onTap: () => onTap(context),
            behavior: HitTestBehavior.Opaque,
            child: new SizedBox(height: 100.0, width: 100.0)))));
}
