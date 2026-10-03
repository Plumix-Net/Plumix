using System.Reflection;
using Avalonia;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.SemanticsMatchers;
using MaterialWidget = Plumix.Material.Material;
using TextDirection = Plumix.UI.TextDirection;

// C#-only test infrastructure; no Dart parity source. Self-tests of the shared flutter_test-like
// surface of FrameworkDartTester (finders, keys, text, semantics, announcements, restoration, view,
// gestures, haptics, platform variants, paint).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FrameworkDartTesterHarnessTests : IDisposable
{
    public FrameworkDartTesterHarnessTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void Finders_FindByTextTypeKeyIconAndRelations()
    {
        using var tester = new FrameworkDartTester();
        var key = new ValueKey<string>("box");
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Column(children:
            [
                new SizedBox(key: key, width: 10, height: 10, child: new Text("A")),
                new Text("B"),
                new Offstage(child: new Text("Hidden")),
            ])));

        Finds.OneWidget(Find.Text("A"));
        Finds.NWidgets(Find.ByType<Text>(), 2);
        Finds.Nothing(Find.Text("Hidden"));
        Finds.OneWidget(Find.Text("Hidden", skipOffstage: false));
        Finds.OneWidget(Find.ByKey(key));
        Finds.OneWidget(Find.WidgetWithText(typeof(SizedBox), "A"));
        Finds.OneWidget(Find.Descendant(of: Find.ByKey(key), matching: Find.ByType<Text>()));
        Finds.OneWidget(Find.Ancestor(of: Find.Text("A"), matching: Find.ByKey(key)));
        Finds.Widgets(Find.ByWidgetPredicate(widget => widget is Text));
        Finds.AtLeastNWidgets(Find.ByElementPredicate(element => element.Widget is Text), 2);
        Finds.OneWidget(Find.Text("A", findRichText: true));
        Finds.OneWidget(Find.TextContaining("Hid", skipOffstage: false));

        Assert.Equal("A", tester.Widget<Text>(Find.ByType<Text>().First).Data);
        Assert.Equal("B", tester.Widget<Text>(Find.ByType<Text>().Last).Data);
        Assert.Equal("B", tester.Widget<Text>(Find.ByType<Text>().At(1)).Data);
        Assert.Equal("A", tester.FirstWidget<Text>(Find.ByType<Text>()).Data);
        Assert.Equal(2, tester.WidgetList<Text>(Find.ByType<Text>()).Count);
        Assert.Same(tester.ElementsWithKey(key).Single(), tester.Element(Find.ByKey(key)));
        Assert.Equal(new Size(10, 10), tester.RenderObject<RenderBox>(Find.ByKey(key)).Size);
        Assert.Contains(tester.RenderObject<RenderBox>(Find.ByKey(key)), tester.AllRenderObjects);
        Assert.Throws<InvalidOperationException>(() => tester.Widget<Text>(Find.ByType<Text>()));
    }

    [Fact]
    public void Finders_StateTooltipAndSemanticsLabel()
    {
        using var tester = new FrameworkDartTester(semanticsEnabled: true);
        tester.PumpWidget(new MaterialApp(home: new MaterialWidget(child: new Column(children:
        [
            new Tooltip(message: "Tip", child: new Text("Target")),
            new Semantics(label: "Labelled", child: new SizedBox(width: 10, height: 10)),
            new Checkbox(value: false, onChanged: _ => { }),
            new IconButton(icon: new Icon(Icons.Add), onPressed: () => { }),
        ]))));

        Finds.OneWidget(Find.ByIcon(Icons.Add));
        Finds.OneWidget(Find.WidgetWithIcon<IconButton>(Icons.Add));

        Finds.OneWidget(Find.ByTooltip("Tip"));
        Finds.OneWidget(Find.BySemanticsLabel("Labelled"));
        Assert.NotNull(tester.State<State>(Find.ByType<Checkbox>()));
    }

    [Fact]
    public void Geometry_AndTapLongPressDrag()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        int taps = 0;
        int longPresses = 0;
        double dragged = 0;
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Align(
                alignment: Alignment.TopLeft,
                child: new Padding(
                    insets: new EdgeInsets(10, 20, 0, 0),
                    child: new GestureDetector(
                        onTap: () => taps++,
                        onLongPress: () => longPresses++,
                        onPanUpdate: details => dragged += details.Delta.X,
                        behavior: HitTestBehavior.Opaque,
                        child: new SizedBox(width: 100, height: 50))))));
        Finder box = Find.ByType<SizedBox>();

        Assert.Equal(new Point(10, 20), tester.GetTopLeft(box));
        Assert.Equal(new Point(110, 20), tester.GetTopRight(box));
        Assert.Equal(new Point(10, 70), tester.GetBottomLeft(box));
        Assert.Equal(new Point(110, 70), tester.GetBottomRight(box));
        Assert.Equal(new Point(60, 45), tester.GetCenter(box));
        Assert.Equal(new Rect(10, 20, 100, 50), tester.GetRect(box));
        Assert.Equal(new Size(100, 50), tester.GetSize(box));

        tester.Tap(box);
        tester.PumpAndSettle();
        tester.TapAt(new Point(20, 30));
        tester.PumpAndSettle();
        Assert.Equal(2, taps);

        tester.LongPress(box);
        tester.PumpAndSettle();
        Assert.Equal(1, longPresses);

        tester.Drag(box, new Vector(40, 0));
        tester.PumpAndSettle();
        Assert.True(dragged > 0);
    }

    [Fact]
    public void Keys_SendKeyEventsThroughTheFocusTree()
    {
        using var tester = new FrameworkDartTester();
        var events = new List<(LogicalKeyboardKey Key, bool Shift)>();
        var node = new FocusNode();
        tester.PumpWidget(new Focus(
            focusNode: node,
            autofocus: true,
            onKeyEvent: (_, @event) =>
            {
                if (@event is KeyDownEvent)
                {
                    events.Add((@event.LogicalKey, HardwareKeyboard.Instance.IsShiftPressed));
                }

                return KeyEventResult.Handled;
            },
            child: new SizedBox()));
        tester.Pump();

        Assert.True(tester.SendKeyEvent(LogicalKeyboardKey.Space));
        tester.SendKeyDownEvent(LogicalKeyboardKey.ShiftLeft);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyUpEvent(LogicalKeyboardKey.ShiftLeft);

        Assert.Equal(
            [(LogicalKeyboardKey.Space, false), (LogicalKeyboardKey.ShiftLeft, true), (LogicalKeyboardKey.Tab, true)],
            events);
        Assert.False(HardwareKeyboard.Instance.IsShiftPressed);
    }

    [Fact]
    public void Text_EnterTextAndReceiveAction()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController();
        string? submitted = null;
        tester.PumpWidget(new MaterialApp(home: new MaterialWidget(child: new TextField(
            controller: controller,
            onSubmitted: value => submitted = value))));

        tester.EnterText(Find.ByType<TextField>(), "hello");
        tester.Pump();
        Assert.Equal("hello", controller.Text);
        Finds.OneWidget(Find.Text("hello"));

        tester.TestTextInput.ReceiveAction(TextInputActionType.Done);
        tester.Pump();
        Assert.Equal("hello", submitted);
    }

    [Fact]
    public void Semantics_GetSemanticsMatchersAndSemanticsTester()
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(child: new Semantics(
                label: "Button",
                button: true,
                enabled: true,
                onTap: () => { },
                child: new SizedBox(width: 10, height: 10)))));

        ExpectSemantics(
            tester.GetSemantics(Find.ByType<SizedBox>()),
            MatchesSemantics(
                label: "Button",
                isButton: true,
                hasEnabledState: true,
                isEnabled: true,
                hasTapAction: true,
                textDirection: TextDirection.Ltr));
        ExpectSemantics(Find.ByType<SizedBox>(), ContainsSemantics(label: "Button", isButton: true));
        ExpectNotSemantics(tester.Semantics.Find(Find.ByType<SizedBox>()), MatchesSemantics(label: "Button"));
        Assert.True(semantics.IncludesNodeWith(label: "Button", actions: SemanticsActions.Tap));
        Assert.False(semantics.IncludesNodeWith(label: "Other"));
    }

    [Fact]
    public void Announcements_CapturesWhatTheFrameworkAnnounces()
    {
        using var tester = new FrameworkDartTester();
        Announce(tester, "Hello");

        CapturedAccessibilityAnnouncement announcement = Assert.Single(tester.TakeAnnouncements());
        ExpectAnnouncement(announcement, "Hello", textDirection: TextDirection.Ltr);
        Assert.Empty(tester.TakeAnnouncements());
    }

    [Fact]
    public void Announcements_CapturesAccessibilityChannelMessages()
    {
        using var tester = new FrameworkDartTester();
        _ = SystemChannels.Accessibility.Send(new Dictionary<string, object?>
        {
            ["type"] = "announce",
            ["data"] = new Dictionary<string, object?>
            {
                ["message"] = "From channel",
                ["textDirection"] = 0,
                ["viewId"] = tester.View.ViewId,
                ["assertiveness"] = 1,
            },
        });

        CapturedAccessibilityAnnouncement announcement = Assert.Single(tester.TakeAnnouncements());
        Assert.Equal(
            new CapturedAccessibilityAnnouncement(
                "From channel",
                tester.View.ViewId,
                TextDirection.Rtl,
                Assertiveness.Assertive),
            announcement);
    }

    [Fact]
    public void Restoration_RestartAndRestoreAndRestoreFrom()
    {
        using var tester = new FrameworkDartTester();
        // WidgetsApp does not give its Navigator Dart's `restorationScopeId: 'nav'` yet, so a route's content
        // is not restorable under MaterialApp(home:); a root scope is (the tester's manager serves both).
        tester.PumpWidget(new RootRestorationScope(
            restorationId: "root",
            child: new Directionality(
                TextDirection.Ltr,
                new ListView(
                    restorationId: "list",
                    children: Enumerable.Range(0, 50)
                        .Select(index => (Widget)new SizedBox(height: 50, child: new Text($"Tile {index}")))
                        .ToList()))));
        ScrollableState Scrollable() => tester.State<ScrollableState>(Find.ByType<Scrollable>());

        Scrollable().Position.JumpTo(100);
        tester.Pump();
        tester.RestartAndRestore();
        Assert.Equal(100, Scrollable().Position.Pixels);

        TestRestorationData data = tester.GetRestorationData();
        Scrollable().Position.JumpTo(0);
        tester.Pump();
        tester.RestoreFrom(data);
        Assert.Equal(100, Scrollable().Position.Pixels);
    }

    [Fact]
    public void View_MetricsAreSettableAndResettable()
    {
        using var tester = new FrameworkDartTester();
        MediaQueryData? data = null;
        tester.PumpWidget(new Builder(context =>
        {
            data = MediaQuery.Of(context);
            return new SizedBox();
        }));
        Assert.Equal(new Size(800, 600), data!.Size);

        tester.View.PhysicalSize = new Size(800, 800);
        tester.View.DevicePixelRatio = 2.0;
        tester.View.ViewInsets = new Thickness(0, 0, 0, 100);
        tester.View.Padding = new Thickness(0, 40, 0, 0);
        tester.Pump();
        Assert.Equal(new Size(400, 400), data!.Size);
        Assert.Equal(new Size(400, 400), tester.RenderView.Size);
        Assert.Equal(50, data.ViewInsets.Bottom);
        Assert.Equal(20, data.Padding.Top);

        tester.View.Reset();
        tester.Pump();
        Assert.Equal(new Size(800, 600), data!.Size);
        Assert.Equal(0, data.ViewInsets.Bottom);

        tester.Binding.SetSurfaceSize(new Size(200, 100));
        tester.Pump();
        Assert.Equal(new Size(200, 100), tester.RenderView.Size);
        Assert.Equal(new Size(800, 600), data!.Size);
        tester.Binding.SetSurfaceSize(null);
        tester.Pump();
        Assert.Equal(new Size(800, 600), tester.RenderView.Size);
    }

    [Fact]
    public void Feedback_CountsTheClickSoundOfATap()
    {
        using var feedback = new FeedbackTester();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new MaterialWidget(child: new Center(child: new InkWell(
            onTap: () => { },
            child: new SizedBox(width: 50, height: 50))))));

        tester.Tap(Find.ByType<InkWell>());
        tester.PumpAndSettle();

        Assert.Equal(1, feedback.ClickSoundCount);
        Assert.Equal(0, feedback.HapticCount);
    }

    [Fact]
    public void TargetPlatformVariant_RunsEachPlatformAndRestores()
    {
        var seen = new List<TargetPlatform>();
        TargetPlatformVariant.Desktop().Run(_ => seen.Add(PlatformDefaults.TargetPlatform));

        Assert.Equal([TargetPlatform.Linux, TargetPlatform.MacOS, TargetPlatform.Windows], seen);
        Assert.Equal(TargetPlatform.Android, PlatformDefaults.TargetPlatform);
        Assert.Equal(6, TargetPlatformVariant.All().Values.Count);
        Assert.Equal([TargetPlatform.IOS], TargetPlatformVariant.Only(TargetPlatform.IOS).Values);
    }

    [Theory]
    [MemberData(nameof(TargetPlatformVariant.MobileData), MemberType = typeof(TargetPlatformVariant))]
    public void TargetPlatformVariant_OverridesForATheoryCase(TargetPlatform platform)
    {
        using (TargetPlatformVariant.Override(platform))
        {
            Assert.Equal(platform, PlatformDefaults.TargetPlatform);
        }

        Assert.Equal(TargetPlatform.Android, PlatformDefaults.TargetPlatform);
    }

    [Fact]
    public void Paint_RecordsTheRenderObjectAFinderFinds()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new SizedBox(
            width: 10,
            height: 10,
            child: new ColoredBox(new Color(0xFF00FF00)))));

        PaintAssert.Paints(Find.ByType<ColoredBox>(), PaintPattern.Paints.Rect(color: new Color(0xFF00FF00)));
        Assert.Equal(1, tester.RecordPaint(Find.ByType<ColoredBox>()).CountCalls("drawRect"));
    }

    // Calls `SemanticsService.sendAnnouncement` whatever its C# shape (a view or a view id first).
    private static void Announce(FrameworkDartTester tester, string message)
    {
        MethodInfo method = typeof(SemanticsService).GetMethod("SendAnnouncement")!;
        object?[] arguments = method.GetParameters()
            .Select(parameter => parameter.ParameterType switch
            {
                Type type when type == typeof(FlutterView) => tester.View.FlutterView,
                Type type when type == typeof(int) => tester.View.ViewId,
                Type type when type == typeof(string) => message,
                Type type when type == typeof(TextDirection) => TextDirection.Ltr,
                _ => parameter.HasDefaultValue ? parameter.DefaultValue : null,
            })
            .ToArray();
        ((Task)method.Invoke(null, arguments)!).GetAwaiter().GetResult();
    }
}
