using System.Runtime.CompilerServices;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/widget_inspector_test.dart
//
// The tests of widget_inspector_test.dart that run without `--track-widget-creation`: Plumix has no
// creation-location transform, so the tracked tests (skipped by Dart in that configuration) are not
// ported, and the tests that branch on tracking take the untracked branch. Golden comparisons are
// replaced by the non-image assertions of the same tests.

namespace Plumix.Tests;

public sealed class WidgetInspectorDartParityTests : IDisposable
{
    private static TestWidgetInspectorService Service => TestWidgetInspectorService.Shared;

    private readonly WidgetInspectorService _previousService = WidgetInspectorService.Instance;
    private readonly List<FrameworkDartTester> _testers = [];

    public WidgetInspectorDartParityTests()
    {
        TestWidgetInspectorService.Restore(Service);
    }

    public void Dispose()
    {
        foreach (FrameworkDartTester tester in _testers)
        {
            tester.Dispose();
        }

        WidgetsBinding.Instance.DebugOverrideRootForTests(null, null);
        Service.ResetAllState();
        Service.SelectionChangedCallback = null;
        WidgetsBinding.Instance.DebugShowWidgetInspectorOverride = false;
        WidgetsBinding.Instance.DebugExcludeRootWidgetInspector = false;
        WidgetsBinding.Instance.DebugWidgetInspectorSelectionOnTapEnabled.Value = true;
        TestWidgetInspectorService.Restore(_previousService);
    }

    private FrameworkDartTester CreateTester(Size? logicalSize = null)
    {
        var tester = new FrameworkDartTester(fakeGestureTimers: true, logicalSize: logicalSize);
        _testers.Add(tester);
        WidgetsBinding.Instance.DebugOverrideRootForTests(tester.Root, tester.Owner);
        return tester;
    }

    private static Widget Ltr(Widget child) => new Directionality(TextDirection.Ltr, child);

    private static Text LtrText(string data, Key? key = null) =>
        new(data, textDirection: TextDirection.Ltr, key: key);

    private static string ParagraphText(RenderObject renderObject) =>
        ((TextSpan)((RenderParagraph)renderObject).Text).Text!;

    private static void PumpWidgetTreeWithABC(FrameworkDartTester tester)
    {
        tester.PumpWidget(Ltr(new Stack(
            children:
            [
                LtrText("a"),
                LtrText("b"),
                LtrText("c"),
                new DisableWidgetInspectorScope(
                    new Column(
                        children:
                        [
                            new InspectorTestHiddenWidget(),
                            new EnableWidgetInspectorScope(new InspectorTestVisibleWidget()),
                        ])),
            ])));
    }

    private static Element FindElementABC(FrameworkDartTester tester, string letter)
    {
        Assert.Contains(letter, new[] { "a", "b", "c" });
        return tester.FirstElement(Find.Text(letter));
    }

    private static DiagnosticsNode? GetFirstVisibleNode(DiagnosticsNode node, bool inDisable = false)
    {
        object? value = node.Value;
        if ((value is Element { Widget: DisableWidgetInspectorScope }) || inDisable)
        {
            foreach (DiagnosticsNode child in node.GetChildren())
            {
                DiagnosticsNode? visible = GetFirstVisibleNode(
                    child,
                    ((Element)value!).Widget is not EnableWidgetInspectorScope);
                if (visible is not null)
                {
                    return visible;
                }
            }

            return null;
        }

        return node;
    }

    public static TheoryData<object> WeakValueTests() => new()
    {
        1,
        1.0,
        "hello",
        true,
        false,
        new object(),
        new List<int> { 3, 4 },
        new DateTime(2023, 1, 1),
    };

    [Theory]
    [MemberData(nameof(WeakValueTests))]
    public void InspectorReferenceData_CanBeCreatedForAnyTypeButRecord(object item)
    {
        var weakValue = new InspectorReferenceData(item, "id");
        Assert.Equal(item, weakValue.Value);
    }

    [Fact]
    public void InspectorReferenceData_ThrowsForRecord()
    {
        Assert.Throws<ArgumentException>(() => new InspectorReferenceData((1, 2), "id"));
    }

    [Theory]
    [MemberData(nameof(WeakValueTests))]
    public void WeakMap_AssignsAndRemovesValue(object item)
    {
        var weakMap = new WeakMap<object, object>();
        weakMap[item] = 1;
        Assert.Equal(1, weakMap[item]);
        Assert.Equal(1, weakMap.Remove(item));
        Assert.Null(weakMap[item]);
    }

    [Theory]
    [MemberData(nameof(WeakValueTests))]
    public void WeakMap_ReturnsNullForAbsentValue(object item)
    {
        var weakMap = new WeakMap<object, object>();
        Assert.Null(weakMap[item]);
    }

    [Fact]
    public void ObjectToDiagnosticsNode_ReturnsNullForNonDiagnosticable()
    {
        Assert.Null(WidgetInspectorService.ObjectToDiagnosticsNode(Alignment.BottomCenter));
    }

    [Fact]
    public void WidgetInspector_DoesNotHoldObjectsFromGC()
    {
        WeakReference reference = CreateAndRegister();

        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndRegister()
    {
        var someObject = new List<DateTime> { DateTime.Now, DateTime.Now };
        string? id = Service.ToId(someObject, "group_name");
        Assert.NotNull(id);
        return new WeakReference(someObject);
    }

    [Fact]
    public void WidgetInspector_SmokeTest()
    {
        FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(Ltr(new Stack(children: [LtrText("a"), LtrText("b"), LtrText("c")])));
        tester.PumpWidget(Ltr(new WidgetInspector(
            exitWidgetSelectionButtonBuilder: null,
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new Stack(children: [LtrText("a"), LtrText("b"), LtrText("c")]))));

        Assert.True(true); // Expect that we don't crash.
    }

    private static Widget ExitButton(
        BuildContext context,
        Action onPressed,
        string semanticsLabel,
        GlobalKey key,
        Action<GlobalKey>? captureKey = null)
    {
        captureKey?.Invoke(key);
        return new InspectorTestButton(
            onPressed: onPressed,
            key: key,
            behavior: HitTestBehavior.Opaque,
            child: new SizedBox(width: 48.0, height: 48.0));
    }

    [Fact]
    public void WidgetInspector_InteractionTest()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;
        var log = new List<string>();
        var inspectorKey = new LabeledGlobalKey<State>(null);
        var topButtonKey = new LabeledGlobalKey<State>(null);
        var bottomButtonKey = new LabeledGlobalKey<State>(null);
        GlobalKey? exitWidgetSelectionButtonKey = null;

        void TapAndVerifyWidgetSelection(Finder finder, GlobalKey widgetKey)
        {
            tester.Tap(finder);
            tester.Pump();
            Assert.Contains(
                tester.FirstElement(Find.ByKey(widgetKey)).RenderObject,
                WidgetInspectorService.Instance.Selection.Candidates);
        }

        void PanAndVerifyWidgetSelection(Finder startAt, Finder endAt, GlobalKey widgetKey)
        {
            Point start = tester.GetCenter(startAt);
            Point end = tester.GetCenter(endAt);
            tester.DragFrom(start, new Vector(end.X - start.X, end.Y - start.Y));
            tester.Pump();
            Assert.Contains(
                tester.FirstElement(Find.ByKey(widgetKey)).RenderObject,
                WidgetInspectorService.Instance.Selection.Candidates);
        }

        tester.PumpWidget(Ltr(new WidgetInspector(
            key: inspectorKey,
            exitWidgetSelectionButtonBuilder: (context, onPressed, semanticsLabel, key) =>
                ExitButton(
                    context,
                    onPressed,
                    semanticsLabel,
                    key,
                    captured => exitWidgetSelectionButtonKey = captured),
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new ListView(
                children:
                [
                    new InspectorTestButton(
                        onPressed: () => log.Add("top"),
                        behavior: HitTestBehavior.Opaque,
                        child: new SizedBox(key: topButtonKey, height: 48.0, child: new Text("TOP"))),
                    new InspectorTestButton(
                        onPressed: () => log.Add("bottom"),
                        behavior: HitTestBehavior.Opaque,
                        child: new SizedBox(key: bottomButtonKey, height: 48.0, child: new Text("BOTTOM"))),
                ]))));

        InspectorSelection selection = WidgetInspectorService.Instance.Selection;
        Assert.Null(selection.Current);
        TapAndVerifyWidgetSelection(Find.Text("TOP"), topButtonKey);
        // Tap intercepted by the inspector
        Assert.Empty(log);
        Assert.Equal("TOP", ParagraphText(selection.Current!));

        TapAndVerifyWidgetSelection(Find.Text("BOTTOM"), bottomButtonKey);
        Assert.Equal("BOTTOM", ParagraphText(selection.Current!));
        Assert.Empty(log);

        PanAndVerifyWidgetSelection(Find.Text("BOTTOM"), Find.Text("TOP"), topButtonKey);
        Assert.Equal("TOP", ParagraphText(selection.Current!));
        Assert.Empty(log);

        PanAndVerifyWidgetSelection(Find.Text("TOP"), Find.Text("BOTTOM"), bottomButtonKey);
        Assert.Equal("BOTTOM", ParagraphText(selection.Current!));
        Assert.Empty(log);

        // Tap on the exit selection mode button to exit select mode.
        tester.Tap(Find.ByKey(exitWidgetSelectionButtonKey!));
        tester.Pump();
        Assert.Null(selection.Current);
    }

    [Fact]
    public void WidgetInspector_NonInvertibleTransformRegressionTest()
    {
        FrameworkDartTester tester = CreateTester();
        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(0.0, 0.0, 0.0, 1.0);
        tester.PumpWidget(Ltr(new WidgetInspector(
            exitWidgetSelectionButtonBuilder: null,
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new Transform(
                transform: transform,
                child: new Stack(children: [LtrText("a"), LtrText("b"), LtrText("c")])))));

        tester.Tap(Find.ByType(typeof(Transform)));

        Assert.True(true); // Expect that we don't crash.
    }

    [Fact]
    public void WidgetInspector_ScrollTest()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;
        var childKey = new UniqueKey();
        var inspectorKey = new LabeledGlobalKey<State>(null);
        GlobalKey? exitWidgetSelectionButtonKey = null;

        tester.PumpWidget(Ltr(new WidgetInspector(
            key: inspectorKey,
            exitWidgetSelectionButtonBuilder: (context, onPressed, semanticsLabel, key) =>
                ExitButton(
                    context,
                    onPressed,
                    semanticsLabel,
                    key,
                    captured => exitWidgetSelectionButtonKey = captured),
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new ListView(
                dragStartBehavior: DragStartBehavior.Down,
                children: [new Container(key: childKey, height: 5000.0)]))));
        Assert.Equal(0.0, tester.GetTopLeft(Find.ByKey(childKey)).Y);

        tester.Fling(Find.ByType(typeof(ListView)), new Vector(0.0, -200.0), 200.0);
        tester.Pump();

        // Fling does nothing as are in inspect mode.
        Assert.Equal(0.0, tester.GetTopLeft(Find.ByKey(childKey)).Y);

        tester.Fling(Find.ByType(typeof(ListView)), new Vector(200.0, 0.0), 200.0);
        tester.Pump();

        // Fling still does nothing as are in inspect mode.
        Assert.Equal(0.0, tester.GetTopLeft(Find.ByKey(childKey)).Y);

        tester.Tap(Find.ByType(typeof(ListView)));
        tester.Pump();
        Assert.NotNull(WidgetInspectorService.Instance.Selection.Current);

        // Now out of inspect mode due to the click.
        tester.Tap(Find.ByKey(exitWidgetSelectionButtonKey!));
        tester.Pump();

        tester.Fling(Find.ByType(typeof(ListView)), new Vector(0.0, -200.0), 200.0);
        tester.Pump();

        Assert.Equal(-200.0, tester.GetTopLeft(Find.ByKey(childKey)).Y);

        tester.Fling(Find.ByType(typeof(ListView)), new Vector(0.0, 200.0), 200.0);
        tester.Pump();

        Assert.Equal(0.0, tester.GetTopLeft(Find.ByKey(childKey)).Y);
    }

    [Fact]
    public void WidgetInspector_LongPress()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;
        bool didLongPress = false;

        tester.PumpWidget(Ltr(new WidgetInspector(
            exitWidgetSelectionButtonBuilder: null,
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new GestureDetector(
                onLongPress: () =>
                {
                    Assert.False(didLongPress);
                    didLongPress = true;
                },
                child: LtrText("target")))));

        tester.LongPress(Find.Text("target"));
        // The inspector will swallow the long press.
        Assert.False(didLongPress);
    }

    [Fact]
    public void WidgetInspector_Offstage()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;
        var inspectorKey = new LabeledGlobalKey<State>(null);
        var clickTarget = new LabeledGlobalKey<State>(null);

        Widget CreateSubtree(double? width = null, Key? key = null)
        {
            return new Stack(
                children:
                [
                    new Positioned(
                        key: key,
                        left: 0.0,
                        top: 0.0,
                        width: width,
                        height: 100.0,
                        child: LtrText(width?.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                            ?? "null")),
                ]);
        }

        var entry1 = new OverlayEntry(maintainState: true, builder: _ => CreateSubtree(width: 94.0));
        var entry2 = new OverlayEntry(opaque: true, maintainState: true, builder: _ => CreateSubtree(width: 95.0));
        var entry3 = new OverlayEntry(
            maintainState: true,
            builder: _ => CreateSubtree(width: 96.0, key: clickTarget));
        tester.PumpWidget(Ltr(new WidgetInspector(
            key: inspectorKey,
            exitWidgetSelectionButtonBuilder: null,
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new Overlay(initialEntries: [entry1, entry2, entry3]))));

        tester.LongPress(Find.ByKey(clickTarget));
        // The object with width 95.0 wins over the object with width 94.0 because
        // the subtree with width 94.0 is offstage.
        Assert.Equal(95.0, WidgetInspectorService.Instance.Selection.Current?.SemanticBoundsForSemantics.Width);

        // Exactly 2 out of the 3 text elements should be in the candidate list of
        // objects to select as only 2 are onstage.
        Assert.Equal(2, WidgetInspectorService.Instance.Selection.Candidates.OfType<RenderParagraph>().Count());

        foreach (OverlayEntry entry in new[] { entry1, entry2, entry3 })
        {
            entry.Remove();
            entry.Dispose();
        }
    }

    [Fact]
    public void WidgetInspector_WithTransformAbove_PaintsTheOverlayInTheInspectorSpace()
    {
        // Dart compares a golden of the overlay (inspector.overlay_positioning_with_transform.png);
        // this checks the overlay picture instead: the selected box is outlined in its own space,
        // transformed into the inspector's.
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;
        var childKey = new LabeledGlobalKey<State>(null);
        var repaintBoundaryKey = new LabeledGlobalKey<State>(null);
        Matrix4 mainTransform = Matrix4.Identity();
        mainTransform.TranslateByDouble(50.0, 30.0, 0.0, 1.0);
        mainTransform.ScaleByDouble(0.8, 0.8, 1.0, 1.0);
        mainTransform.TranslateByDouble(100.0, 50.0, 0.0, 1.0);

        tester.PumpWidget(new RepaintBoundary(
            key: repaintBoundaryKey,
            child: new ColoredBox(
                color: new Color(0xFF9E9E9E),
                child: new Transform(
                    transform: mainTransform,
                    child: Ltr(new WidgetInspector(
                        exitWidgetSelectionButtonBuilder: null,
                        moveExitWidgetSelectionButtonBuilder: null,
                        tapBehaviorButtonBuilder: null,
                        child: new ColoredBox(
                            color: new Color(0xFFFFFFFF),
                            child: new Center(
                                child: new Container(
                                    key: childKey,
                                    height: 100.0,
                                    width: 50.0,
                                    color: new Color(0xFFF44336))))))))));

        tester.Tap(Find.ByKey(childKey));
        tester.Pump();

        // The smallest candidate wins; the container's coloured box and constrained box tie on area,
        // and the descendant was hit first.
        RenderObject selected = WidgetInspectorService.Instance.Selection.Current!;
        Assert.IsType<RenderColoredBox>(selected);
        var overlay = (RenderInspectorOverlay)tester
            .FirstElement(Find.ByType(typeof(InspectorOverlay)))
            .FindRenderObject()!;
        var layer = new InspectorOverlayLayer(
            new Rect(overlay.Size),
            WidgetInspectorService.Instance.Selection,
            overlay.Parent);
        layer.AddToScene(new SceneBuilder());
        IReadOnlyList<CanvasCall> calls = layer.DebugPicture!.DebugCalls;
        CanvasCall[] rects = calls.Where(call => call.Method == "drawRect").ToArray();
        // Selected fill, selected border, a border per candidate, then the tooltip background.
        Assert.Equal(new Color(0x808080FF), rects[0].Color);
        Assert.Equal(new Color(0x80404080), rects[1].Color);
        Assert.All(rects[2..^1], rect => Assert.Equal(new Color(0x80404080), rect.Color));
        Assert.Equal(new Color(0xE63C3C3C), rects[^1].Color);
        Assert.Equal(new Rect(0.5, 0.5, 49.0, 99.0), rects[0].Rect);
        layer.Dispose();
    }

    [Fact]
    public void MultipleWidgetInspectors()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;
        // This test verifies that interacting with different inspectors
        // works correctly. This use case may be an app that displays multiple
        // apps inside (i.e. a storyboard).
        var selectButton1Key = new LabeledGlobalKey<State>(null);
        var selectButton2Key = new LabeledGlobalKey<State>(null);
        var inspector1Key = new LabeledGlobalKey<State>(null);
        var inspector2Key = new LabeledGlobalKey<State>(null);
        var child1Key = new LabeledGlobalKey<State>(null);
        var child2Key = new LabeledGlobalKey<State>(null);

        ExitWidgetSelectionButtonBuilder ExitWidgetSelectionButtonBuilder(GlobalKey key) =>
            (context, onPressed, semanticsLabel, buttonKey) => new InspectorTestButton(
                onPressed: onPressed,
                key: buttonKey,
                behavior: HitTestBehavior.Opaque,
                child: new SizedBox(width: 48.0, height: 48.0));

        tester.PumpWidget(Ltr(new Row(
            children:
            [
                new Flexible(new WidgetInspector(
                    key: inspector1Key,
                    exitWidgetSelectionButtonBuilder: ExitWidgetSelectionButtonBuilder(selectButton1Key),
                    moveExitWidgetSelectionButtonBuilder: null,
                    tapBehaviorButtonBuilder: null,
                    child: new Container(key: child1Key, child: new Text("Child 1")))),
                new Flexible(new WidgetInspector(
                    key: inspector2Key,
                    exitWidgetSelectionButtonBuilder: ExitWidgetSelectionButtonBuilder(selectButton2Key),
                    moveExitWidgetSelectionButtonBuilder: null,
                    tapBehaviorButtonBuilder: null,
                    child: new Container(key: child2Key, child: new Text("Child 2")))),
            ])));

        tester.Tap(Find.Text("Child 1"));
        tester.Pump();
        Assert.Equal("Child 1", ParagraphText(WidgetInspectorService.Instance.Selection.Current!));

        tester.Tap(Find.Text("Child 2"));
        tester.Pump();
        Assert.Equal("Child 2", ParagraphText(WidgetInspectorService.Instance.Selection.Current!));
    }

    [Fact]
    public void WidgetInspector_IsNotInsertedWhenDebugExcludeRootWidgetInspectorIsSet()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetsBinding binding = WidgetsBinding.Instance;
        binding.DebugExcludeRootWidgetInspector = true;

        tester.PumpWidget(new WidgetsApp(
            color: new Color(0xFFFF0000),
            builder: (_, _) => new Text("Foo")));

        Assert.False(binding.DebugShowWidgetInspectorOverride);
        Assert.Empty(tester.ElementList(Find.ByType(typeof(WidgetInspector))));

        binding.DebugShowWidgetInspectorOverride = true;
        tester.Pump();

        Assert.Empty(tester.ElementList(Find.ByType(typeof(WidgetInspector))));
    }

    [Fact]
    public void WidgetsApp_InsertsAWidgetInspectorWhileTheOverrideIsSet()
    {
        FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new WidgetsApp(
            color: new Color(0xFFFF0000),
            builder: (_, _) => new Text("Foo")));
        Assert.Empty(tester.ElementList(Find.ByType(typeof(WidgetInspector))));

        WidgetsBinding.Instance.DebugShowWidgetInspectorOverride = true;
        tester.Pump();
        Assert.Single(tester.ElementList(Find.ByType(typeof(WidgetInspector))));

        WidgetsBinding.Instance.DebugShowWidgetInspectorOverride = false;
        tester.Pump();
        Assert.Empty(tester.ElementList(Find.ByType(typeof(WidgetInspector))));
    }

    [Fact]
    public void InspectorButtons_RespectBottomViewPadding()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;

        tester.PumpWidget(Ltr(new MediaQuery(
            data: new MediaQueryData(ViewPadding: new Thickness(0, 0, 0, 50.0)),
            child: new WidgetInspector(
                exitWidgetSelectionButtonBuilder: (_, _, _, _) => new Text("exit"),
                moveExitWidgetSelectionButtonBuilder: (_, _, _, _) => new Text("move"),
                tapBehaviorButtonBuilder: (_, _, _, _) => new Text("tap"),
                child: new SizedBox()))));

        foreach (string name in new[] { "exit", "move", "tap" })
        {
            Finder finder = Find.Text(name);
            Assert.Single(tester.ElementList(finder));
            var positioned = (Positioned)tester.Element(Find.Ancestor(finder, Find.ByType(typeof(Positioned)))).Widget;
            Assert.Equal(50.0, positioned.Bottom);
        }

        WidgetInspectorService.Instance.IsSelectMode = false;
    }

    [Fact]
    public void InspectorButtons_MoveAndTapBehaviorButtonsUpdateTheGroup()
    {
        // The untracked half of Dart's on-device button tests: the move button flips the group
        // to the other edge and back, and the tap-behavior button toggles selection on tap.
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;

        tester.PumpWidget(Ltr(new WidgetInspector(
            exitWidgetSelectionButtonBuilder: (_, onPressed, _, key) =>
                new InspectorTestButton(onPressed: onPressed, key: key, child: new Text("EXIT SELECT MODE")),
            moveExitWidgetSelectionButtonBuilder: (_, onPressed, _, usesDefaultAlignment) =>
                new InspectorTestButton(
                    onPressed: onPressed,
                    child: new Text(usesDefaultAlignment ? "MOVE RIGHT" : "MOVE LEFT")),
            tapBehaviorButtonBuilder: (_, onPressed, _, selectionOnTapEnabled) =>
                new InspectorTestButton(
                    onPressed: onPressed,
                    child: new Text(selectionOnTapEnabled ? "SELECTION ON TAP" : "APP INTERACTION ON TAP")),
            child: new Row(children: [new Text("Child 1"), new Text("Child 2")]))));

        double initialX = tester.GetCenter(Find.Text("EXIT SELECT MODE")).X;
        tester.Tap(Find.Text("MOVE RIGHT"));
        tester.Pump();
        Assert.Single(tester.ElementList(Find.Text("MOVE LEFT")));
        Assert.True(initialX < tester.GetCenter(Find.Text("EXIT SELECT MODE")).X);
        tester.Tap(Find.Text("MOVE LEFT"));
        tester.Pump();
        Assert.Equal(initialX, tester.GetCenter(Find.Text("EXIT SELECT MODE")).X);

        tester.Tap(Find.Text("Child 1"));
        tester.Pump();
        Assert.Same(
            tester.FirstElement(Find.Text("Child 1")).RenderObject,
            Service.Selection.Current);

        tester.Tap(Find.Text("SELECTION ON TAP"));
        tester.Pump();
        Assert.Empty(tester.ElementList(Find.Text("SELECTION ON TAP")));
        Assert.Single(tester.ElementList(Find.Text("APP INTERACTION ON TAP")));
        tester.Tap(Find.Text("Child 2"));
        tester.Pump();
        Assert.Null(Service.Selection.Current);

        tester.Tap(Find.Text("APP INTERACTION ON TAP"));
        tester.Pump();
        Assert.Single(tester.ElementList(Find.Text("SELECTION ON TAP")));
        tester.Tap(Find.Text("Child 2"));
        tester.Pump();
        Assert.Same(
            tester.FirstElement(Find.Text("Child 2")).RenderObject,
            Service.Selection.Current);

        tester.Tap(Find.Text("EXIT SELECT MODE"));
        tester.Pump();
        Assert.False(WidgetsBinding.Instance.DebugShowWidgetInspectorOverride);
        Assert.Null(Service.Selection.Current);
    }

    [Fact]
    public void InspectorButtons_RtlMoveButtonMovesTheGroupLeft()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.IsSelectMode = true;

        tester.PumpWidget(new Directionality(
            TextDirection.Rtl,
            new WidgetInspector(
                exitWidgetSelectionButtonBuilder: (_, onPressed, _, key) =>
                    new InspectorTestButton(onPressed: onPressed, key: key, child: new Text("EXIT SELECT MODE")),
                moveExitWidgetSelectionButtonBuilder: (_, onPressed, _, usesDefaultAlignment) =>
                    new InspectorTestButton(
                        onPressed: onPressed,
                        child: new Text(usesDefaultAlignment ? "MOVE RIGHT" : "MOVE LEFT")),
                tapBehaviorButtonBuilder: null,
                child: new Text("APP"))));

        double initialX = tester.GetCenter(Find.Text("EXIT SELECT MODE")).X;
        tester.Tap(Find.Text("MOVE RIGHT"));
        tester.Pump();
        Assert.True(initialX > tester.GetCenter(Find.Text("EXIT SELECT MODE")).X);
        tester.Tap(Find.Text("MOVE LEFT"));
        tester.Pump();
        Assert.Equal(initialX, tester.GetCenter(Find.Text("EXIT SELECT MODE")).X);
    }

    [Fact]
    public void TransformDebugCreator_WillReorderIfAfterStackTrace()
    {
        FrameworkDartTester tester = CreateTester();
        Assert.False(WidgetInspectorService.Instance.IsWidgetCreationTracked());
        tester.PumpWidget(Ltr(new Stack(children: [new Text("a"), LtrText("b"), LtrText("c")])));
        Element elementA = tester.FirstElement(Find.Text("a"));
        Service.SetSelection(elementA, "my-group");

        var builder = new DiagnosticPropertiesBuilder();
        builder.Add(new StringProperty("dummy1", "value"));
        builder.Add(new StringProperty("dummy2", "value"));
        builder.Add(new DiagnosticsStackTrace("When the exception was thrown, this was the stack", null));
        builder.Add(new DiagnosticsDebugCreator(new DebugCreator(elementA)));

        List<DiagnosticsNode> nodes = WidgetInspectorDebug.DebugTransformDebugCreator(builder.Properties).ToList();
        Assert.Equal(5, nodes.Count);
        Assert.IsType<StringProperty>(nodes[0]);
        Assert.Equal("dummy1", nodes[0].Name);
        Assert.IsType<StringProperty>(nodes[1]);
        Assert.Equal("dummy2", nodes[1].Name);
        // transformed node should come in front of stack trace.
        Assert.IsType<ErrorDescription>(nodes[2]);
        Assert.StartsWith(
            "Widget creation tracking is currently disabled.",
            ((ErrorDescription)nodes[2]).ValueToString());
        Assert.IsType<ErrorSpacer>(nodes[3]);
        Assert.IsType<DiagnosticsStackTrace>(nodes[4]);
    }

    [Fact]
    public void TransformDebugCreator_WillNotReorderIfBeforeStackTrace()
    {
        FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(Ltr(new Stack(children: [new Text("a"), LtrText("b"), LtrText("c")])));
        Element elementA = tester.FirstElement(Find.Text("a"));
        var builder = new DiagnosticPropertiesBuilder();
        builder.Add(new StringProperty("dummy1", "value"));
        builder.Add(new DiagnosticsDebugCreator(new DebugCreator(elementA)));
        builder.Add(new StringProperty("dummy2", "value"));
        builder.Add(new DiagnosticsStackTrace("When the exception was thrown, this was the stack", null));

        List<DiagnosticsNode> nodes = WidgetInspectorDebug.DebugTransformDebugCreator(builder.Properties).ToList();
        Assert.Equal(5, nodes.Count);
        Assert.IsType<StringProperty>(nodes[0]);
        Assert.Equal("dummy1", nodes[0].Name);
        Assert.IsType<ErrorDescription>(nodes[1]);
        Assert.StartsWith(
            "Widget creation tracking is currently disabled.",
            ((ErrorDescription)nodes[1]).ValueToString());
        Assert.IsType<ErrorSpacer>(nodes[2]);
        Assert.IsType<StringProperty>(nodes[3]);
        Assert.Equal("dummy2", nodes[3].Name);
        Assert.IsType<DiagnosticsStackTrace>(nodes[4]);
    }

    [Fact]
    public void TransformDebugCreator_IsAnErrorDetailsPropertiesTransformer()
    {
        Assert.Contains(
            FlutterErrorDetails.PropertiesTransformers,
            transformer => transformer.Method.Name == nameof(WidgetInspectorDebug.DebugTransformDebugCreator));
    }

    private const string DirectoryA = "/a/b/c";
    private const string DirectoryB = "/d/e/f";
    private const string DirectoryC = "/g/h/i";

    [Fact]
    public void PubRootDirectory_AddCanAddMultipleDirectories()
    {
        Service.ResetPubRootDirectories();
        Service.AddPubRootDirectories([DirectoryA, DirectoryB]);
        Assert.Equal(new[] { DirectoryA, DirectoryB }.Order(), Service.CurrentPubRootDirectories.Order());
    }

    [Fact]
    public void PubRootDirectory_AddCanAddMultipleDirectoriesSeparately()
    {
        Service.ResetPubRootDirectories();
        Service.AddPubRootDirectories([DirectoryA]);
        Service.AddPubRootDirectories([DirectoryB]);
        Service.AddPubRootDirectories([]);
        Assert.Equal(new[] { DirectoryA, DirectoryB }.Order(), Service.CurrentPubRootDirectories.Order());
    }

    [Fact]
    public void PubRootDirectory_AddHandlesDuplicates()
    {
        Service.ResetPubRootDirectories();
        Service.AddPubRootDirectories(["/a/b/c", "file:///a/b/c", "/d/e/f", "/d/e/f"]);
        Assert.Equal(new[] { "/a/b/c", "/d/e/f" }.Order(), Service.CurrentPubRootDirectories.Order());
    }

    private static void ResetWithThreeDirectories()
    {
        Service.ResetPubRootDirectories();
        Service.AddPubRootDirectories([DirectoryA, DirectoryB, DirectoryC]);
    }

    [Fact]
    public void PubRootDirectory_RemoveRemovesMultipleDirectories()
    {
        ResetWithThreeDirectories();
        Service.RemovePubRootDirectories([DirectoryA, DirectoryB]);
        Assert.Equal(new[] { DirectoryC }, Service.CurrentPubRootDirectories);
    }

    [Fact]
    public void PubRootDirectory_RemoveRemovesMultipleDirectoriesSeparately()
    {
        ResetWithThreeDirectories();
        Service.RemovePubRootDirectories([DirectoryA]);
        Service.RemovePubRootDirectories([DirectoryB]);
        Service.RemovePubRootDirectories([]);
        Assert.Equal(new[] { DirectoryC }, Service.CurrentPubRootDirectories);
    }

    [Fact]
    public void PubRootDirectory_RemoveHandlesDuplicates()
    {
        ResetWithThreeDirectories();
        Service.RemovePubRootDirectories(["file:///a/b/c", "/a/b/c", "/d/e/f", "/d/e/f"]);
        Assert.Equal(new[] { DirectoryC }, Service.CurrentPubRootDirectories);
    }

    [Fact]
    public void PubRootDirectory_RemoveDoesNothingIfTheDirectoriesDoNotExist()
    {
        ResetWithThreeDirectories();
        Service.RemovePubRootDirectories(["/x/y/z"]);
        Assert.Equal(
            new[] { DirectoryA, DirectoryB, DirectoryC }.Order(),
            Service.CurrentPubRootDirectories.Order());
    }

    [Fact]
    public void ServiceApi_NullId()
    {
        Service.DisposeAllGroups();
        Assert.Null(Service.ToObject(null));
        Assert.Null(Service.ToId(null, "test-group"));
    }

    [Fact]
    public void ServiceApi_DisposeGroup()
    {
        Service.DisposeAllGroups();
        object a = new object();
        const string group1 = "group-1";
        const string group2 = "group-2";
        const string group3 = "group-3";
        string? aId = Service.ToId(a, group1);
        Assert.Equal(aId, Service.ToId(a, group2));
        Assert.Equal(aId, Service.ToId(a, group3));
        Service.DisposeGroup(group1);
        Service.DisposeGroup(group2);
        Assert.Same(a, Service.ToObject(aId));
        Service.DisposeGroup(group3);
        Assert.Throws<FlutterError>(() => Service.ToObject(aId));
    }

    [Fact]
    public void ServiceApi_DisposeId()
    {
        Service.DisposeAllGroups();
        object a = new object();
        object b = new object();
        const string group1 = "group-1";
        const string group2 = "group-2";
        string? aId = Service.ToId(a, group1);
        string? bId = Service.ToId(b, group1);
        Assert.Equal(aId, Service.ToId(a, group2));
        Service.DisposeId(bId, group1);
        Assert.Throws<FlutterError>(() => Service.ToObject(bId));
        Service.DisposeId(aId, group1);
        Assert.Same(a, Service.ToObject(aId));
        Service.DisposeId(aId, group2);
        Assert.Throws<FlutterError>(() => Service.ToObject(aId));
    }

    [Fact]
    public void ServiceApi_ToObjectForSourceLocation()
    {
        Widget widget = LtrText("a");
        Service.DisposeAllGroups();
        const string group = "test-group";
        string id = Service.ToId(widget, group)!;
        Assert.Same(widget, Service.ToObjectForSourceLocation(id));
        Element element = widget.CreateElement();
        string elementId = Service.ToId(element, group)!;
        Assert.Same(widget, Service.ToObjectForSourceLocation(elementId));
        Assert.NotSame(widget, element);
        Service.DisposeGroup(group);
        Assert.Throws<FlutterError>(() => Service.ToObjectForSourceLocation(elementId));
    }

    [Fact]
    public void ServiceApi_ObjectIdTest()
    {
        Widget a = LtrText("a");
        Widget b = LtrText("b");
        Widget c = LtrText("c");
        Widget d = LtrText("d");

        const string group1 = "group-1";
        const string group2 = "group-2";
        const string group3 = "group-3";
        Service.DisposeAllGroups();

        string? aId = Service.ToId(a, group1);
        string? bId = Service.ToId(b, group2);
        string? cId = Service.ToId(c, group3);
        string? dId = Service.ToId(d, group1);
        // Make sure we get a consistent id if we add the object to a group multiple
        // times.
        Assert.Equal(aId, Service.ToId(a, group1));
        Assert.Same(a, Service.ToObject(aId));
        Assert.NotSame(b, Service.ToObject(aId));
        Assert.Same(b, Service.ToObject(bId));
        Assert.Same(c, Service.ToObject(cId));
        Assert.Same(d, Service.ToObject(dId));
        // Make sure we get a consistent id even if we add the object to a different
        // group.
        Assert.Equal(aId, Service.ToId(a, group3));
        Assert.NotEqual(aId, bId);
        Assert.NotEqual(aId, cId);

        Service.DisposeGroup(group3);
    }

    [Fact]
    public void ServiceApi_MaybeSetSelection()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        Element elementA = FindElementABC(tester, "a");
        Element elementB = FindElementABC(tester, "b");

        Service.DisposeAllGroups();
        Service.Selection.Clear();
        int selectionChangedCount = 0;
        Service.SelectionChangedCallback = () => selectionChangedCount++;
        Service.SetSelection("invalid selection");
        Assert.Equal(0, selectionChangedCount);
        Assert.Null(Service.Selection.CurrentElement);
        Service.SetSelection(elementA);
        Assert.Equal(1, selectionChangedCount);
        Assert.Same(elementA, Service.Selection.CurrentElement);
        Assert.Same(elementA.RenderObject, Service.Selection.Current);

        Service.SetSelection(elementB.RenderObject);
        Assert.Equal(2, selectionChangedCount);
        Assert.Same(elementB.RenderObject, Service.Selection.Current);
        Assert.Same(
            ((DebugCreator)elementB.RenderObject!.DebugCreator!).Element,
            Service.Selection.CurrentElement);

        Service.SetSelection("invalid selection");
        Assert.Equal(2, selectionChangedCount);
        Assert.Same(elementB.RenderObject, Service.Selection.Current);

        Service.SetSelectionById(Service.ToId(elementA, "my-group"));
        Assert.Equal(3, selectionChangedCount);
        Assert.Same(elementA, Service.Selection.CurrentElement);
        Assert.Same(elementA.RenderObject, Service.Selection.Current);

        Service.SetSelectionById(Service.ToId(elementA, "my-group"));
        Assert.Equal(3, selectionChangedCount);
        Assert.Same(elementA, Service.Selection.CurrentElement);
    }

    [Fact]
    public void ServiceApi_DefunctSelectionRegressionTest()
    {
        FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(Ltr(new Stack(children: [LtrText("a")])));
        Element elementA = tester.FirstElement(Find.Text("a"));

        Service.SetSelection(elementA);
        Assert.Same(elementA, Service.Selection.CurrentElement);
        Assert.Same(elementA.RenderObject, Service.Selection.Current);

        tester.PumpWidget(new SizedBox(child: LtrText("b")));
        // Selection is now empty as the element is defunct.
        Assert.Null(Service.Selection.CurrentElement);
        Assert.Null(Service.Selection.Current);

        // Verify that getting the debug creation location of the defunct element
        // does not crash.
        Assert.False(WidgetInspectorDebug.DebugIsLocalCreationLocation(elementA));

        // Verify that generating json for a defunct element does not crash.
        Assert.NotNull(elementA.ToDiagnosticsNode().ToJsonMap(new InspectorSerializationDelegate(
            service: Service,
            includeProperties: true)));

        Element elementB = tester.FirstElement(Find.Text("b"));
        Service.SetSelection(elementB);
        Assert.Same(elementB, Service.Selection.CurrentElement);
        Assert.Same(elementB.RenderObject, Service.Selection.Current);

        // Set selection back to a defunct element.
        Service.SetSelection(elementA);

        Assert.Null(Service.Selection.CurrentElement);
        Assert.Null(Service.Selection.Current);
    }

    [Fact]
    public void ServiceApi_GetParentChain()
    {
        FrameworkDartTester tester = CreateTester();
        const string group = "test-group";
        PumpWidgetTreeWithABC(tester);
        Element elementB = FindElementABC(tester, "b");
        Service.DisposeAllGroups();
        string bId = Service.ToId(elementB, group)!;
        object? jsonList = JsonRoundTrip.Decode(Service.GetParentChain(bId, group));
        VerifyParentChain(tester, elementB, jsonList);
    }

    internal static void VerifyParentChain(FrameworkDartTester tester, Element elementB, object? jsonList)
    {
        Assert.IsType<List<object?>>(jsonList);
        var chainElements = (List<object?>)jsonList!;
        List<Element> expectedChain = elementB.DebugGetDiagnosticChain();
        expectedChain.Reverse();
        // Sanity check that the chain goes back to the root.
        Assert.Same(tester.Root, expectedChain[0]);

        Assert.Equal(expectedChain.Count, chainElements.Count);
        for (int i = 0; i < expectedChain.Count; i += 1)
        {
            var chainNode = Assert.IsType<Dictionary<string, object?>>(chainElements[i]);
            Element element = expectedChain[i];
            var jsonNode = Assert.IsType<Dictionary<string, object?>>(chainNode["node"]);
            Assert.Same(element, Service.ToObject((string?)jsonNode["valueId"]));

            var childrenElements = new List<Element>();
            element.VisitChildren(childrenElements.Add);
            var jsonChildren = Assert.IsType<List<object?>>(chainNode["children"]);
            Assert.Equal(childrenElements.Count, jsonChildren.Count);
            if (i + 1 == expectedChain.Count)
            {
                Assert.Null(chainNode["childIndex"]);
            }
            else
            {
                Assert.Equal(childrenElements.IndexOf(expectedChain[i + 1]), chainNode["childIndex"]);
            }

            for (int j = 0; j < childrenElements.Count; j += 1)
            {
                var childJson = Assert.IsType<Dictionary<string, object?>>(jsonChildren[j]);
                Assert.Same(childrenElements[j], Service.ToObject((string?)childJson["valueId"]));
            }
        }
    }

    [Fact]
    public void ServiceApi_GetProperties()
    {
        IDiagnosticable diagnosticable = LtrText("a");
        const string group = "group";
        Service.DisposeAllGroups();
        string id = Service.ToId(diagnosticable, group)!;
        var propertiesJson = (List<object?>)JsonRoundTrip.Decode(Service.GetProperties(id, group))!;
        VerifyProperties(diagnosticable, propertiesJson);
    }

    internal static void VerifyProperties(IDiagnosticable diagnosticable, List<object?> propertiesJson)
    {
        List<DiagnosticsNode> properties = diagnosticable.ToDiagnosticsNode().GetProperties();
        Assert.NotEmpty(properties);
        Assert.Equal(properties.Count, propertiesJson.Count);
        for (int i = 0; i < propertiesJson.Count; ++i)
        {
            var propertyJson = (Dictionary<string, object?>)propertiesJson[i]!;
            object? expected = properties[i].Value;
            object? actual = Service.ToObject((string?)propertyJson["valueId"]);
            if (expected is null || expected.GetType().IsValueType || expected is string)
            {
                Assert.Equal(expected, actual);
            }
            else
            {
                Assert.Same(expected, actual);
            }
        }
    }

    [Fact]
    public void ServiceApi_GetChildren()
    {
        FrameworkDartTester tester = CreateTester();
        const string group = "test-group";
        PumpWidgetTreeWithABC(tester);
        DiagnosticsNode diagnostic = tester.FirstElement(Find.ByType(typeof(Stack))).ToDiagnosticsNode();
        Service.DisposeAllGroups();
        string id = Service.ToId(diagnostic, group)!;
        var propertiesJson = (List<object?>)JsonRoundTrip.Decode(Service.GetChildren(id, group))!;
        VerifyChildren(diagnostic, propertiesJson);
    }

    internal static void VerifyChildren(DiagnosticsNode diagnostic, List<object?> childrenJson)
    {
        List<DiagnosticsNode> children = diagnostic.GetChildren();
        Assert.Equal(4, children.Count);
        Assert.Equal(children.Count, childrenJson.Count);
        for (int i = 0; i < childrenJson.Count; ++i)
        {
            var childJson = (Dictionary<string, object?>)childrenJson[i]!;
            Assert.Same(GetFirstVisibleNode(children[i])?.Value, Service.ToObject((string?)childJson["valueId"]));
        }
    }

    [Fact]
    public void InspectorSelection_ReceivesNotificationsWhenSelectionChanges()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        var selection = new InspectorSelection();
        int count = 0;
        selection.AddListener(() => count++);
        RenderObject renderObjectA = tester.FirstElement(Find.Text("a")).RenderObject!;
        RenderObject renderObjectB = tester.FirstElement(Find.Text("b")).RenderObject!;
        Element elementA = tester.FirstElement(Find.Text("a"));

        selection.Candidates = [renderObjectA, renderObjectB];
        tester.Pump();
        Assert.Equal(1, count);

        selection.Index = 1;
        tester.Pump();
        Assert.Equal(2, count);

        selection.Clear();
        tester.Pump();
        Assert.Equal(3, count);

        selection.Current = renderObjectA;
        tester.Pump();
        Assert.Equal(4, count);

        selection.CurrentElement = elementA;
        Assert.Equal(5, count);
        selection.Dispose();
    }

    [Fact]
    public void InspectorSelection_ClearCandidatesPreservesCurrentSelection()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        var selection = new InspectorSelection();
        RenderObject renderObjectA = tester.FirstElement(Find.Text("a")).RenderObject!;
        RenderObject renderObjectB = tester.FirstElement(Find.Text("b")).RenderObject!;

        selection.Candidates = [renderObjectA, renderObjectB];
        Assert.Same(renderObjectA, selection.Current);

        selection.ClearCandidates();
        Assert.Empty(selection.Candidates);
        Assert.Same(renderObjectA, selection.Current);
        selection.Dispose();
    }

    [Fact]
    public void InspectorSelectionCandidates_AreScopedToTheActiveModalRoute()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.Selection.Clear();
        var behindKey = new LabeledGlobalKey<State>(null);
        var sheetTextKey = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(Ltr(new WidgetInspector(
            exitWidgetSelectionButtonBuilder: null,
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new Navigator(
                initialRoute: new PageRouteBuilder(
                    pageBuilder: (context, _, _) => new Column(
                        children:
                        [
                            LtrText("behind", key: behindKey),
                            new GestureDetector(
                                onTap: () => Navigator.Of(context).Push(new PageRouteBuilder(
                                    pageBuilder: (_, _, _) => new Center(
                                        child: LtrText("in sheet", key: sheetTextKey)))),
                                child: LtrText("open sheet")),
                        ]))))));

        RenderObject behindRender = tester.FirstElement(Find.ByKey(behindKey)).FindRenderObject()!;
        tester.Tap(Find.Text("open sheet"));
        tester.PumpAndSettle();

        WidgetInspectorService.Instance.IsSelectMode = true;
        tester.Tap(Find.ByKey(sheetTextKey));
        tester.Pump();

        List<RenderObject> candidates = WidgetInspectorService.Instance.Selection.Candidates;
        Assert.DoesNotContain(behindRender, candidates);
        Assert.Contains(tester.FirstElement(Find.ByKey(sheetTextKey)).FindRenderObject(), candidates);
    }

    [Fact]
    public void InspectorSelection_ScopesToNestedNavigatorInsideOverlayRoute()
    {
        FrameworkDartTester tester = CreateTester();
        WidgetInspectorService.Instance.Selection.Clear();
        var innerTextKey = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(Ltr(new WidgetInspector(
            exitWidgetSelectionButtonBuilder: null,
            moveExitWidgetSelectionButtonBuilder: null,
            tapBehaviorButtonBuilder: null,
            child: new Navigator(
                initialRoute: new PageRouteBuilder(
                    pageBuilder: (context, _, _) => new Column(
                        children:
                        [
                            new GestureDetector(
                                onTap: () => Navigator.Of(context).Push(new PageRouteBuilder(
                                    pageBuilder: (_, _, _) => new SizedBox(
                                        width: 300,
                                        height: 300,
                                        child: new Navigator(
                                            initialRoute: new PageRouteBuilder(
                                                pageBuilder: (_, _, _) => new Center(
                                                    child: LtrText("nested inner", key: innerTextKey))))))),
                                child: LtrText("open overlay")),
                        ]))))));

        tester.Tap(Find.Text("open overlay"));
        tester.PumpAndSettle();

        WidgetInspectorService.Instance.IsSelectMode = true;
        tester.Tap(Find.ByKey(innerTextKey));
        tester.Pump();

        Assert.Contains(
            tester.FirstElement(Find.ByKey(innerTextKey)).FindRenderObject(),
            WidgetInspectorService.Instance.Selection.Candidates);
    }

    [Fact]
    public void SetSelection_ClearsStaleOverlayCandidates()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        Element elementA = FindElementABC(tester, "a");
        Element elementB = FindElementABC(tester, "b");

        WidgetInspectorService.Instance.Selection.Candidates = [elementA.RenderObject!, elementB.RenderObject!];
        Assert.Equal(2, WidgetInspectorService.Instance.Selection.Candidates.Count);

        Service.SetSelection(elementA);

        Assert.Empty(WidgetInspectorService.Instance.Selection.Candidates);
        Assert.Same(elementA, WidgetInspectorService.Instance.Selection.CurrentElement);
    }

    [Fact]
    public void WidgetInspector_DoesNotCrashAtZeroArea()
    {
        FrameworkDartTester tester = CreateTester(logicalSize: new Size(0, 0));
        tester.PumpWidget(new WidgetsApp(
            color: new Color(0xFFFFFFFF),
            pageRouteBuilder: (settings, builder) => new PageRouteBuilder(
                settings: settings,
                pageBuilder: (context, _, _) => builder(context)),
            home: new Center(
                child: new WidgetInspector(
                    tapBehaviorButtonBuilder: null,
                    exitWidgetSelectionButtonBuilder: null,
                    moveExitWidgetSelectionButtonBuilder: null,
                    child: new Placeholder()))));
        Assert.Equal(new Size(0, 0), tester.GetSize(Find.ByType(typeof(WidgetInspector))));
    }

    [Fact]
    public void DebugIsWidgetLocalCreation_IsFalseWithoutCreationTracking()
    {
        FrameworkDartTester tester = CreateTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(Ltr(new Container(
            padding: new EdgeInsets(8, 8, 8, 8),
            child: LtrText("target", key: key))));
        Element element = (Element)key.CurrentContext!;
        Assert.False(WidgetInspectorDebug.DebugIsWidgetLocalCreation(element.Widget));
        Assert.False(WidgetsDebug.DebugIsWidgetLocalCreation(element.Widget));
    }

    [Fact]
    public void DevToolsInspectorUri()
    {
        string? previousServer = FoundationDebug.ActiveDevToolsServerAddress;
        string? previousUri = FoundationDebug.ConnectedVmServiceUri;
        FoundationDebug.ActiveDevToolsServerAddress = "http://127.0.0.1:9100";
        FoundationDebug.ConnectedVmServiceUri = "http://127.0.0.1:55269/798ay5al_FM=/";
        try
        {
            Assert.Equal(
                "http://127.0.0.1:9100/#/inspector?uri=http%3A%2F%2F127.0.0.1%3A55269%2F798ay5al_FM%3D%2F"
                + "&inspectorRef=inspector-0",
                WidgetInspectorService.Instance.DevToolsInspectorUri("inspector-0"));
        }
        finally
        {
            FoundationDebug.ActiveDevToolsServerAddress = previousServer;
            FoundationDebug.ConnectedVmServiceUri = previousUri;
        }
    }

    [Fact]
    public void DevToolsDeepLinkPropertyTest()
    {
        var node = new DevToolsDeepLinkProperty("description of the deep link", "http://the-deeplink/");
        Assert.Equal("description of the deep link", node.ToString());
        Assert.Equal(string.Empty, node.Name);
        Assert.Equal("http://the-deeplink/", node.Value);
        Assert.Equal(
            new Dictionary<string, object?>
            {
                ["description"] = "description of the deep link",
                ["type"] = "DevToolsDeepLinkProperty",
                ["name"] = string.Empty,
                ["style"] = "singleLine",
                ["allowNameWrap"] = true,
                ["missingIfNull"] = false,
                ["propertyType"] = "String",
                ["defaultLevel"] = "info",
                ["value"] = "http://the-deeplink/",
            },
            node.ToJsonMap(DiagnosticsSerializationDelegate.Create()));
    }

    [Fact]
    public void InspectorSerializationDelegate_AddAdditionalPropertiesCallback()
    {
        FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(Ltr(new Center(child: new Column(children: [new Text("Hello World!")]))));
        Assert.Single(tester.ElementList(Find.ByType(typeof(Column))));
        DiagnosticsNode node = tester.FirstElement(Find.ByType(typeof(Column))).ToDiagnosticsNode();
        var serializationDelegate = new InspectorSerializationDelegate(
            service: Service,
            includeProperties: true,
            addAdditionalPropertiesCallback: (diagnosticsNode, @delegate) =>
            {
                var additionalJson = new Dictionary<string, object?>();
                object? value = diagnosticsNode.Value;
                if (value is Element element && element.RenderObject is { } renderObject)
                {
                    additionalJson["renderObject"] = renderObject.ToDiagnosticsNode().ToJsonMap(
                        @delegate.CopyWith(subtreeDepth: 0));
                }

                additionalJson["callbackExecuted"] = true;
                return additionalJson;
            });
        Dictionary<string, object?> json = node.ToJsonMap(serializationDelegate);
        Assert.Equal(true, json["callbackExecuted"]);
        Assert.True(json.ContainsKey("renderObject"));
        var renderObjectJson = Assert.IsType<Dictionary<string, object?>>(json["renderObject"]);
        Assert.StartsWith("RenderFlex", (string)renderObjectJson["description"]!);

        var emptyDelegate = new InspectorSerializationDelegate(
            service: Service,
            includeProperties: true,
            addAdditionalPropertiesCallback: (_, _) => null);
        var defaultDelegate = new InspectorSerializationDelegate(service: Service, includeProperties: true);
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(node.ToJsonMap(defaultDelegate)),
            System.Text.Json.JsonSerializer.Serialize(node.ToJsonMap(emptyDelegate)));
    }
}
