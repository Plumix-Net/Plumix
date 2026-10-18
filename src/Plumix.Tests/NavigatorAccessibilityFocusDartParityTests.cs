using System.Collections;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports the accessibility-focus restoration cases of flutter/packages/flutter/test/widgets/navigator_test.dart:
// the navigator records ServicesBinding.accessibilityFocus on the top route and, after the route above it
// is popped and disposed, asks the platform to move accessibility focus back.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class NavigatorAccessibilityFocusDartParityTests : IDisposable
{
    private readonly BinaryMessenger _messenger = ServicesBinding.Instance.DefaultBinaryMessenger;

    public NavigatorAccessibilityFocusDartParityTests()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
    }

    public void Dispose()
    {
        _messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, null);
        PlatformDefaults.DebugTargetPlatformOverride = null;
        ServicesBinding.Instance.AccessibilityFocus.Value = null;
    }

    private static PageRouteBuilder Page(Widget child) => new(
        pageBuilder: (_, _, _) => child,
        transitionDuration: TimeSpan.Zero,
        reverseTransitionDuration: TimeSpan.Zero);

    // Flutter: 'Send semantic event to move a11y focus to the last focused item when pop next page'
    // (iOS variant; the Dart test opens and closes a modal bottom sheet over the route).
    [Fact]
    public void SendSemanticEventToMoveA11yFocusToTheLastFocusedItemWhenPopNextPage()
    {
        IDictionary? semanticEvent = null;
        _messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, message =>
        {
            semanticEvent = (IDictionary?)SystemChannels.Accessibility.Codec.DecodeMessage(message);
            return Task.FromResult<ByteData?>(null);
        });
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("navigator");
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(navigatorKey: navigatorKey, home: new Text("Home page")));
        Assert.Single(tester.OnstageElementsWithText("Home page"));

        // The focused node before opening the sheet.
        ByteData? fakeMessage = SystemChannels.Accessibility.Codec.EncodeMessage(
            new Dictionary<string, object?> { ["type"] = "didGainFocus", ["nodeId"] = 5 });
        _ = _messenger.HandlePlatformMessage(SystemChannels.Accessibility.Name, fakeMessage, static _ => { });
        tester.PumpAndSettle();
        Assert.Equal(5, ServicesBinding.Instance.AccessibilityFocus.Value);

        navigatorKey.CurrentState!.Push(Page(new Text("Close Sheet")));
        tester.PumpAndSettle();
        Assert.Single(tester.OnstageElementsWithText("Close Sheet"));
        navigatorKey.CurrentState!.Pop();
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(500));

        // The focused node before opening the sheet regains the focus.
        Assert.NotNull(semanticEvent);
        Assert.Equal("focus", semanticEvent!["type"]);
        Assert.Equal(5, Convert.ToInt32(semanticEvent["nodeId"]));
        Assert.Empty(Assert.IsAssignableFrom<IDictionary>(semanticEvent["data"]));
    }

    // Flutter: 'Navigator focus restoration reports error to FlutterError' (iOS).
    [Fact]
    public void NavigatorFocusRestorationReportsErrorToFlutterError()
    {
        var errorDetails = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? oldHandler = FlutterError.OnError;
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("navigator");
        using var tester = new FrameworkDartTester();
        FlutterError.OnError = errorDetails.Add;
        try
        {
            // Mock accessibility channel to throw error.
            _messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, message =>
            {
                var map = (IDictionary?)SystemChannels.Accessibility.Codec.DecodeMessage(message);
                if (Equals(map?["type"], "focus"))
                {
                    return Task.FromException<ByteData?>(new Exception("Focus restoration failed"));
                }

                return Task.FromResult<ByteData?>(null);
            });

            tester.PumpWidget(new TestWidgetsApp(navigatorKey: navigatorKey, home: new Text("Push")));

            // Record the last focus in route entry.
            ServicesBinding.Instance.AccessibilityFocus.Value = 123;
            tester.Pump();

            // Push second route.
            navigatorKey.CurrentState!.Push(Page(new Text("Pop")));
            tester.PumpAndSettle();

            // Now we are on the second route. Pop it.
            navigatorKey.CurrentState!.Pop();
            tester.PumpAndSettle();

            FlutterErrorDetails error = Assert.Single(errorDetails);
            Assert.Contains("Focus restoration failed", error.Exception.ToString());
            Assert.Equal("widgets library", error.Library);
            Assert.Contains("while restoring focus in the navigator", error.Context!.ToString());
        }
        finally
        {
            FlutterError.OnError = oldHandler;
        }
    }

    [Fact]
    public void NoFocusEventOutsideAndroidAndIOS()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        var events = new List<IDictionary>();
        _messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, message =>
        {
            events.Add((IDictionary)SystemChannels.Accessibility.Codec.DecodeMessage(message)!);
            return Task.FromResult<ByteData?>(null);
        });
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("navigator");
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(navigatorKey: navigatorKey, home: new Text("Home")));
        ServicesBinding.Instance.AccessibilityFocus.Value = 7;
        tester.Pump();

        navigatorKey.CurrentState!.Push(Page(new Text("Second")));
        tester.PumpAndSettle();
        navigatorKey.CurrentState!.Pop();
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(500));

        Assert.DoesNotContain(events, static e => Equals(e["type"], "focus"));
    }
}
