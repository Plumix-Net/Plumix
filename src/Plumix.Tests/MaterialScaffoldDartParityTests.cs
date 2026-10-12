using Avalonia;
using Plumix;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: material_ui/test/scaffold_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialScaffoldDartParityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingScaffoldLookupReportsFiveOrderedDiagnostics(bool geometry)
    {
        using var tester = new FrameworkDartTester();
        BuildContext? context = null;
        tester.PumpWidget(new Builder(value =>
        {
            context = value;
            return new SizedBox();
        }));

        var error = Assert.Throws<FlutterError>(() =>
        {
            if (geometry)
            {
                Scaffold.GeometryOf(context!);
            }
            else
            {
                Scaffold.Of(context!);
            }
        });
        string method = geometry ? "geometryOf" : "of";
        Assert.Equal(5, error.Diagnostics.Count);
        Assert.IsType<ErrorSummary>(error.Diagnostics[0]);
        Assert.Equal(
            $"Scaffold.{method}() called with a context that does not contain a Scaffold.",
            error.Diagnostics[0].ToDescription());
        Assert.IsType<ErrorDescription>(error.Diagnostics[1]);
        Assert.IsType<ErrorHint>(error.Diagnostics[2]);
        Assert.Contains("https://api.flutter.dev/flutter/material/Scaffold/of.html",
            error.Diagnostics[2].ToDescription());
        Assert.IsType<ErrorHint>(error.Diagnostics[3]);
        Assert.Equal(!geometry, error.Diagnostics[3].ToDescription().Contains("GlobalKey"));
        Assert.IsType<DiagnosticsProperty<Element>>(error.Diagnostics[4]);
        Assert.Equal("The context used was", error.Diagnostics[4].Name);
        Assert.Null(Scaffold.MaybeOf(context!));
        Assert.False(Scaffold.HasDrawerOf(context!));
    }

    [DebugOnlyFact]
    public void ScaffoldWithoutMediaQueryReportsAncestorErrorBeforeDirectionality()
    {
        var owner = TestBuildOwner.Create();
        RootElement? root = null;
        try
        {
            var error = BuildErrors.Throws<FlutterError>(() =>
                root = new RootWidget(child: new Scaffold()).Attach(owner));
            Assert.Equal("No MediaQuery widget ancestor found.", error.Diagnostics[0].ToDescription());
            Assert.Equal(5, error.Diagnostics.Count);
        }
        finally
        {
            root?.UnmountRoot();
        }
    }

    [DebugOnlyFact]
    public void ScaffoldWithMediaQueryRequiresDirectionality()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Scaffold());
        var error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal("No Directionality widget found.", error.Diagnostics[0].ToDescription());
    }

    [Fact]
    public void ScaffoldWidgetRetainsNullableFabConfigurationAndStateResolvesDefaults()
    {
        var scaffold = new Scaffold(floatingActionButton: new SizedBox(width: 56, height: 56));
        Assert.Null(scaffold.FloatingActionButtonLocation);
        Assert.Null(scaffold.FloatingActionButtonAnimator);
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Shell(scaffold));
        var fab = Assert.Single(tester.AllElements(), e => e.Widget == scaffold.FloatingActionButton);
        var render = Assert.IsAssignableFrom<RenderBox>(fab.RenderObject);
        Assert.Equal(new Point(728, 528), render.LocalToGlobal(default));
        tester.PumpWidget(Shell(new Scaffold(
            floatingActionButton: scaffold.FloatingActionButton,
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            floatingActionButtonAnimator: FloatingActionButtonAnimator.NoAnimation)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.PumpWidget(Shell(scaffold));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new Point(728, 528), render.LocalToGlobal(default));
        Assert.Null(tester.TakeException());
    }

    [DebugOnlyFact]
    public void FloatingSnackBarOffScreenReportsThreeDiagnosticsAfterPositioning()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ScaffoldMessengerState>("messenger");
        tester.PumpWidget(Shell(new ScaffoldMessenger(
            key: key,
            child: new Scaffold(bottomNavigationBar: new SizedBox(height: 600)))));
        var controller = key.CurrentState!.ShowSnackBar(new SnackBar(
            content: new Text("Off screen"),
            behavior: SnackBarBehavior.Floating));
        ((AnimationController)controller.Feature.Animation!).SetValue(1.0);
        tester.Pump();
        var error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal(3, error.Diagnostics.Count);
        Assert.Equal("Floating SnackBar presented off screen.", error.Diagnostics[0].ToDescription());
        Assert.IsType<ErrorDescription>(error.Diagnostics[1]);
        Assert.IsType<ErrorHint>(error.Diagnostics[2]);
        key.CurrentState.RemoveCurrentSnackBar();
        tester.Pump();
        Assert.Null(tester.TakeException());
    }

    [DebugOnlyFact]
    public void ShowingSnackBarDuringBuildRewritesErrorWithFourDiagnostics()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Shell(new ScaffoldMessenger(child: new Scaffold(body: new Builder(context =>
        {
            ScaffoldMessenger.Of(context).ShowSnackBar(new SnackBar(content: new Text("Build")));
            return new SizedBox();
        })))));
        var error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal(4, error.Diagnostics.Count);
        Assert.Equal("The showSnackBar() method cannot be called during build.",
            error.Diagnostics[0].ToDescription());
        Assert.IsType<ErrorDescription>(error.Diagnostics[1]);
        Assert.IsType<ErrorHint>(error.Diagnostics[2]);
        Assert.Contains("addPostFrameCallback", error.Diagnostics[2].ToDescription());
        Assert.Equal("The ownership chain for the particular ScaffoldMessenger is", error.Diagnostics[3].Name);
    }

    [Fact]
    public void RemovingBannerStartsSuccessorAtZeroAndDoesNotNotifyMessengerDependents()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ScaffoldMessengerState>("messenger");
        int dependencyBuilds = 0;
        var dependent = new Builder(context =>
        {
            ScaffoldMessenger.Of(context);
            dependencyBuilds++;
            return new SizedBox();
        });
        tester.PumpWidget(Shell(new ScaffoldMessenger(
            key: key,
            child: new Scaffold(body: dependent))));
        var messenger = key.CurrentState!;
        var first = messenger.ShowMaterialBanner(Banner("First"));
        var second = messenger.ShowMaterialBanner(Banner("Second"));
        tester.Pump();
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(1.0, first.Feature.Animation!.Value);
        Assert.Equal(1, dependencyBuilds);
        int beforeRemove = dependencyBuilds;
        messenger.RemoveCurrentMaterialBanner();
        Assert.Equal(MaterialBannerClosedReason.Remove, first.Closed.Result);
        Assert.Equal(0.0, second.Feature.Animation!.Value);
        tester.Pump();
        Assert.Equal(beforeRemove, dependencyBuilds);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.InRange(second.Feature.Animation.Value, 0.001, 0.999);
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(1.0, second.Feature.Animation.Value);
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void MessengerDisposalLeavesFeatureFuturesUnresolved()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ScaffoldMessengerState>("messenger");
        tester.PumpWidget(Shell(new ScaffoldMessenger(key: key, child: new Scaffold())));
        var snackBar = key.CurrentState!.ShowSnackBar(new SnackBar(content: new Text("Snack")));
        var banner = key.CurrentState.ShowMaterialBanner(Banner("Banner"));
        tester.PumpWidget(new SizedBox());
        Assert.False(snackBar.Closed.IsCompleted);
        Assert.False(banner.Closed.IsCompleted);
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void CancelingSnackBarReverseDoesNotCompleteItsClosedFuture()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ScaffoldMessengerState>("messenger");
        tester.PumpWidget(Shell(new ScaffoldMessenger(key: key, child: new Scaffold())));
        var snackBar = key.CurrentState!.ShowSnackBar(new SnackBar(content: new Text("Snack")));
        tester.Pump();
        tester.Pump(TimeSpan.FromSeconds(1));
        key.CurrentState.HideCurrentSnackBar();
        ((AnimationController)snackBar.Feature.Animation!).Forward();
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.False(snackBar.Closed.IsCompleted);
        key.CurrentState.RemoveCurrentSnackBar();
        Assert.Equal(SnackBarClosedReason.Remove, snackBar.Closed.Result);
    }

    [Fact]
    public void RemovingBannerDuringReverseKeepsRemoveReasonAndQueuedFutureUnresolved()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ScaffoldMessengerState>("messenger");
        tester.PumpWidget(Shell(new ScaffoldMessenger(key: key, child: new Scaffold())));
        var banner = key.CurrentState!.ShowMaterialBanner(Banner("First"));
        var queued = key.CurrentState.ShowMaterialBanner(Banner("Second"));
        tester.Pump();
        tester.Pump(TimeSpan.FromSeconds(1));
        key.CurrentState.ClearMaterialBanners();
        key.CurrentState.RemoveCurrentMaterialBanner();
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(MaterialBannerClosedReason.Remove, banner.Closed.Result);
        Assert.False(queued.Closed.IsCompleted);
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void BodyMetricsValidationMatchesBuildMode()
    {
        if (Constants.KDebugMode)
        {
            Assert.Throws<AssertionError>(() => new BodyBoxMetrics(-1, 0, 0));
            Assert.Throws<AssertionError>(() => new BodyBoxMetrics(0, -1, 0));
            Assert.Throws<AssertionError>(() => new BodyBoxMetrics(0, 0, -1));
        }
        else
        {
            Assert.Equal(-1, new BodyBoxMetrics(-1, -1, -1).BottomWidgetsHeight);
        }
    }

    [Fact]
    public void ScaffoldSlotsDemoSupportsKeyboardAndResizeControlsAtNarrowWidth()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(350, 700));
        tester.PumpWidget(Shell(new ScaffoldSlotsDemoPage()));
        Assert.Null(tester.TakeException());
        tester.Tap(Find.Text("Keyboard off"));
        tester.Pump();
        Element scaffold = Assert.Single(tester.AllElements(), e => e.Widget is Scaffold);
        Assert.Equal(120, MediaQuery.Of(scaffold).ViewInsets.Bottom);
        Assert.Equal(true, ((Scaffold)scaffold.Widget).ResizeToAvoidBottomInset);
        tester.Tap(Find.Text("Resize on"));
        tester.Pump();
        Assert.Equal(false, ((Scaffold)scaffold.Widget).ResizeToAvoidBottomInset);
        Assert.Equal(120, MediaQuery.Of(scaffold).ViewInsets.Bottom);
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void AccessibilityChangeDoesNotPrematurelyDismissSnackBar()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ScaffoldMessengerState>("messenger");
        Widget child = Shell(new ScaffoldMessenger(key: key, child: new Scaffold()));
        tester.PumpWidget(new MediaQuery(new MediaQueryData(AccessibleNavigation: true), child));
        var snackBar = key.CurrentState!.ShowSnackBar(new SnackBar(content: new Text("Snack")));
        tester.PumpWidget(new MediaQuery(new MediaQueryData(AccessibleNavigation: false), child));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.False(snackBar.Closed.IsCompleted);
        Assert.Equal(1.0, snackBar.Feature.Animation!.Value);
        Assert.Null(tester.TakeException());
    }

    [DebugOnlyFact]
    public void StaleFeatureCloseAssertsQueueIdentity()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ScaffoldMessengerState>("messenger");
        tester.PumpWidget(Shell(new ScaffoldMessenger(key: key, child: new Scaffold())));
        var first = key.CurrentState!.ShowSnackBar(new SnackBar(content: new Text("First")));
        var second = key.CurrentState.ShowSnackBar(new SnackBar(content: new Text("Second")));
        Assert.Throws<AssertionError>(() => second.Close());
        var banner = key.CurrentState.ShowMaterialBanner(Banner("First"));
        var queuedBanner = key.CurrentState.ShowMaterialBanner(Banner("Second"));
        Assert.Throws<AssertionError>(() => queuedBanner.Close());
        Assert.False(first.Closed.IsCompleted);
        Assert.False(banner.Closed.IsCompleted);
    }

    [Fact]
    public void GeometryPaintPhaseGuardMatchesBuildMode()
    {
        using var tester = new FrameworkDartTester();
        BuildContext? context = null;
        tester.PumpWidget(Shell(new Scaffold(body: new Builder(value =>
        {
            context = value;
            return new SizedBox();
        }))));
        var geometry = Scaffold.GeometryOf(context!);
        if (Constants.KDebugMode)
        {
            var error = Assert.Throws<FlutterError>(() => geometry.Value);
            Assert.StartsWith("Scaffold.geometryOf() must only be accessed during the paint phase.", error.Message);
        }
        else
        {
            Assert.Null(geometry.Value.FloatingActionButtonArea);
        }
    }

    private static Widget Shell(Widget child) => new Directionality(
        TextDirection.Ltr,
        new Theme(data: ThemeData.Light, child: child));

    private static MaterialBanner Banner(string text) => new(
        content: new Text(text),
        actions: [new SizedBox(width: 10, height: 10)]);
}
