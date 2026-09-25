using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/scrollable_selection_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollableSelectionDartParityTests : IDisposable
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    // Dart's `mockClipboard` with the `setUp` that stores 'empty' before every test.
    private readonly MockClipboardPlatform _clipboard = new("empty");

    public ScrollableSelectionDartParityTests()
    {
        // A flutter_test binding starts with no lifecycle state, so a SelectableRegion keeps its
        // selection when it loses focus; do not inherit a state an earlier test left behind.
        Scheduler.ResetInternalState();
    }

    public void Dispose()
    {
        _clipboard.Dispose();
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    // Dart's top-level `textOffsetToPosition`.
    private static Point TextOffsetToPosition(RenderParagraph paragraph, int offset)
    {
        var caret = new Rect(0.0, 0.0, 2.0, 20.0);
        Point localOffset = paragraph.GetOffsetForCaret(new TextPosition(offset), caret);
        return paragraph.LocalToGlobal(localOffset);
    }

    // Dart's top-level `globalize`.
    private static Point Globalize(Point point, RenderBox box) => box.LocalToGlobal(point);

    // flutter_test's onstage element walk (`skipOffstage: true`).
    private static List<Element> OnstageElements(FrameworkDartTester tester)
    {
        var elements = new List<Element>();
        void Visit(Element element)
        {
            elements.Add(element);
            element.DebugVisitOnstageChildren(Visit);
        }

        tester.Root.DebugVisitOnstageChildren(Visit);
        return elements;
    }

    // `find.descendant(of: find.text(text), matching: find.byType(RichText))`, onstage only.
    private static List<RenderParagraph> FindParagraphs(FrameworkDartTester tester, string text)
    {
        var result = new List<RenderParagraph>();
        foreach (Element element in OnstageElements(tester))
        {
            if (element.Widget is not Text { Data: var data } || data != text)
            {
                continue;
            }

            void Visit(Element child)
            {
                if (child.Widget is RichText && child.RenderObject is RenderParagraph paragraph)
                {
                    result.Add(paragraph);
                }

                child.DebugVisitOnstageChildren(Visit);
            }

            element.DebugVisitOnstageChildren(Visit);
        }

        return result;
    }

    // `tester.renderObject<RenderParagraph>(find.descendant(of: find.text(text), ...))`.
    private static RenderParagraph Paragraph(FrameworkDartTester tester, string text)
        => FindParagraphs(tester, text).Single();

    // `find.text(text)` with the default `skipOffstage: true`.
    private static int CountText(FrameworkDartTester tester, string text)
        => OnstageElements(tester).Count(element => element.Widget is Text { Data: var data } && data == text);

    private static Point GetBottomLeft(FrameworkDartTester tester, Element element)
    {
        Rect rect = tester.GetRect(element);
        return rect.BottomLeft;
    }

    // Dart's `tester.pumpAndSettle(duration)`.
    private static void PumpAndSettle(FrameworkDartTester tester, TimeSpan duration)
    {
        int count = 0;
        do
        {
            Assert.True(count <= 1000, "pumpAndSettle timed out");
            tester.Pump(duration);
            count += 1;
        }
        while (Scheduler.HasScheduledFrame || Scheduler.TransientCallbackCount > 0);
    }

    // keyboard_utils.dart: `sendKeyCombination`.
    private static void SendKeyCombination(
        FrameworkDartTester tester,
        LogicalKeyboardKey trigger,
        bool control = false,
        bool shift = false,
        bool meta = false)
    {
        var modifiers = new List<LogicalKeyboardKey>();
        if (control)
        {
            modifiers.Add(LogicalKeyboardKey.ControlLeft);
        }

        if (shift)
        {
            modifiers.Add(LogicalKeyboardKey.ShiftLeft);
        }

        if (meta)
        {
            modifiers.Add(LogicalKeyboardKey.MetaLeft);
        }

        bool controlDown = false;
        bool shiftDown = false;
        bool metaDown = false;
        void SetModifier(LogicalKeyboardKey modifier, bool down)
        {
            if (modifier == LogicalKeyboardKey.ControlLeft)
            {
                controlDown = down;
            }
            else if (modifier == LogicalKeyboardKey.ShiftLeft)
            {
                shiftDown = down;
            }
            else
            {
                metaDown = down;
            }
        }

        // Every `await tester.sendKey*Event` in Dart drains the microtask queue once the event is
        // handled, so a focus change requested before the combination lands before the trigger.
        void Send(LogicalKeyboardKey key, bool down)
        {
            KeySim.DispatchRaw(key, down: down, control: controlDown, shift: shiftDown, meta: metaDown);
            Scheduler.FlushMicrotasks();
        }

        foreach (LogicalKeyboardKey modifier in modifiers)
        {
            SetModifier(modifier, true);
            Send(modifier, down: true);
        }

        Send(trigger, down: true);
        Send(trigger, down: false);
        tester.Pump();
        for (int i = modifiers.Count - 1; i >= 0; i -= 1)
        {
            SetModifier(modifiers[i], false);
            Send(modifiers[i], down: false);
        }
    }

    // The `MaterialApp(home: SelectionArea(child: ListView.builder(... Text('Item $index'))))` tree.
    private static Widget ItemList(
        ScrollController? controller = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        int itemCount = 100,
        FocusNode? focusNode = null)
    {
        return new MaterialApp(
            home: new SelectionArea(
                focusNode: focusNode,
                selectionControls: MaterialTextSelectionControls.Instance,
                child: ListView.Builder(
                    controller: controller,
                    scrollDirection: scrollDirection,
                    reverse: reverse,
                    itemCount: itemCount,
                    itemBuilder: (_, index) => new Text($"Item {index}"))));
    }

    private static void AssertSelection(RenderParagraph paragraph, int baseOffset, int extentOffset)
    {
        Assert.Equal(new TextSelection(baseOffset, extentOffset), paragraph.Selections[0]);
    }

    // Flutter: 'mouse can select multiple widgets'
    [Fact]
    public void MouseCanSelectMultipleWidgets()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(ItemList());
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();

        gesture.MoveTo(TextOffsetToPosition(paragraph1, 4));
        tester.Pump();
        AssertSelection(paragraph1, 2, 4);

        RenderParagraph paragraph2 = Paragraph(tester, "Item 1");
        gesture.MoveTo(TextOffsetToPosition(paragraph2, 5));
        // Should select the rest of paragraph 1.
        AssertSelection(paragraph1, 2, 6);
        AssertSelection(paragraph2, 0, 5);

        RenderParagraph paragraph3 = Paragraph(tester, "Item 3");
        gesture.MoveTo(TextOffsetToPosition(paragraph3, 3));
        AssertSelection(paragraph1, 2, 6);
        AssertSelection(paragraph2, 0, 6);
        AssertSelection(paragraph3, 0, 3);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'mouse can select multiple widgets - horizontal'
    [Fact]
    public void MouseCanSelectMultipleWidgetsHorizontal()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(ItemList(scrollDirection: Axis.Horizontal));
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();

        gesture.MoveTo(TextOffsetToPosition(paragraph1, 4));
        tester.Pump();
        AssertSelection(paragraph1, 2, 4);

        RenderParagraph paragraph2 = Paragraph(tester, "Item 1");
        gesture.MoveTo(TextOffsetToPosition(paragraph2, 5) + new Vector(0, 5));
        // Should select the rest of paragraph 1.
        AssertSelection(paragraph1, 2, 6);
        AssertSelection(paragraph2, 0, 5);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'mouse can select multiple widgets on double-click drag'
    [Fact]
    public void MouseCanSelectMultipleWidgetsOnDoubleClickDrag()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(ItemList());
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();

        gesture.Up();
        tester.Pump();
        gesture.Down(TextOffsetToPosition(paragraph1, 2));
        tester.PumpAndSettle();
        AssertSelection(paragraph1, 0, 4);

        gesture.MoveTo(TextOffsetToPosition(paragraph1, 4));
        tester.Pump();
        AssertSelection(paragraph1, 0, 5);

        RenderParagraph paragraph2 = Paragraph(tester, "Item 1");
        gesture.MoveTo(TextOffsetToPosition(paragraph2, 4));
        // Should select the rest of paragraph 1.
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 5);

        RenderParagraph paragraph3 = Paragraph(tester, "Item 3");
        gesture.MoveTo(TextOffsetToPosition(paragraph3, 3));
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 6);
        AssertSelection(paragraph3, 0, 4);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'mouse can select multiple widgets on double-click drag - horizontal'
    [Fact]
    public void MouseCanSelectMultipleWidgetsOnDoubleClickDragHorizontal()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(ItemList(scrollDirection: Axis.Horizontal));
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.Up();
        tester.Pump();

        gesture.Down(TextOffsetToPosition(paragraph1, 2));
        tester.PumpAndSettle();
        AssertSelection(paragraph1, 0, 4);

        gesture.MoveTo(TextOffsetToPosition(paragraph1, 4));
        tester.Pump();
        AssertSelection(paragraph1, 0, 5);

        RenderParagraph paragraph2 = Paragraph(tester, "Item 1");
        gesture.MoveTo(TextOffsetToPosition(paragraph2, 5) + new Vector(0, 5));
        // Should select the rest of paragraph 1.
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 6);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'mouse can select multiple widgets on triple-click drag'
    [Fact]
    public void MouseCanSelectMultipleWidgetsOnTripleClickDrag()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(ItemList());
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.Up();
        tester.Pump();

        gesture.Down(TextOffsetToPosition(paragraph1, 2));
        tester.Pump();
        gesture.Up();
        tester.Pump();

        gesture.Down(TextOffsetToPosition(paragraph1, 2));
        tester.PumpAndSettle();
        AssertSelection(paragraph1, 0, 6);

        RenderParagraph paragraph2 = Paragraph(tester, "Item 1");
        Assert.Empty(paragraph2.Selections);
        gesture.MoveTo(TextOffsetToPosition(paragraph2, 4));
        // Should select paragraph 2.
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 6);

        RenderParagraph paragraph3 = Paragraph(tester, "Item 3");
        Assert.Empty(paragraph3.Selections);
        gesture.MoveTo(TextOffsetToPosition(paragraph3, 3));
        // Should select paragraph 3.
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 6);
        AssertSelection(paragraph3, 0, 6);

        RenderParagraph paragraph4 = Paragraph(tester, "Item 4");
        Assert.Empty(paragraph4.Selections);
        gesture.MoveTo(TextOffsetToPosition(paragraph4, 3));
        // Should select paragraph 4.
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 6);
        AssertSelection(paragraph3, 0, 6);
        AssertSelection(paragraph4, 0, 6);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'mouse can select multiple widgets on triple-click drag - horizontal'
    [Fact]
    public void MouseCanSelectMultipleWidgetsOnTripleClickDragHorizontal()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(ItemList(scrollDirection: Axis.Horizontal));
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.Up();
        tester.Pump();

        gesture.Down(TextOffsetToPosition(paragraph1, 2));
        tester.Pump();
        gesture.Up();
        tester.Pump();

        gesture.Down(TextOffsetToPosition(paragraph1, 2));
        tester.PumpAndSettle();
        AssertSelection(paragraph1, 0, 6);

        RenderParagraph paragraph2 = Paragraph(tester, "Item 1");
        Assert.Empty(paragraph2.Selections);
        gesture.MoveTo(TextOffsetToPosition(paragraph2, 5) + new Vector(0, 50));
        // Should select paragraph 2.
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 6);

        RenderParagraph paragraph3 = Paragraph(tester, "Item 2");
        Assert.Empty(paragraph3.Selections);
        gesture.MoveTo(TextOffsetToPosition(paragraph3, 5) + new Vector(0, 50));
        // Should select paragraph 3.
        AssertSelection(paragraph1, 0, 6);
        AssertSelection(paragraph2, 0, 6);
        AssertSelection(paragraph3, 0, 6);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'select to scroll forward'
    [Fact]
    public void SelectToScrollForward()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller));
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(0.0, controller.Offset);
        double previousOffset = controller.Offset;

        // Scrollable only auto scroll if the drag passes the boundary.
        gesture.MoveTo(tester.GetBottomRight(tester.ElementOfType<ListView>()) + new Vector(0, 20));
        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);
        previousOffset = controller.Offset;

        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);

        // Scroll to the end.
        PumpAndSettle(tester, OneSecond);
        Assert.Equal(4200.0, controller.Offset);
        RenderParagraph paragraph99 = Paragraph(tester, "Item 99");
        RenderParagraph paragraph98 = Paragraph(tester, "Item 98");
        RenderParagraph paragraph97 = Paragraph(tester, "Item 97");
        RenderParagraph paragraph96 = Paragraph(tester, "Item 96");
        AssertSelection(paragraph99, 0, 7);
        AssertSelection(paragraph98, 0, 7);
        AssertSelection(paragraph97, 0, 7);
        AssertSelection(paragraph96, 0, 7);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'select to scroll works for small scrollable'
    [Fact]
    public void SelectToScrollWorksForSmallScrollable()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: false),
            home: new SelectionArea(
                selectionControls: MaterialTextSelectionControls.Instance,
                child: new Scaffold(
                    body: new SizedBox(
                        height: 10,
                        child: ListView.Builder(
                            controller: controller,
                            itemCount: 100,
                            itemBuilder: (_, index) => new Text($"Item {index}")))))));
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(0.0, controller.Offset);
        double previousOffset = controller.Offset;

        // Scrollable only auto scroll if the drag passes the boundary
        gesture.MoveTo(tester.GetBottomRight(tester.ElementOfType<ListView>()) + new Vector(0, 20));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.True(controller.Offset > previousOffset);
        previousOffset = controller.Offset;

        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.True(controller.Offset > previousOffset);
        gesture.Up();

        // Shouldn't be stuck if gesture is up.
        PumpAndSettle(tester, OneSecond);
        Assert.Null(tester.TakeException());
        gesture.RemovePointer();
    }

    // Flutter: 'select to scroll backward'
    [Fact]
    public void SelectToScrollBackward()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller));
        tester.PumpAndSettle();

        controller.JumpTo(4000);
        tester.PumpAndSettle();

        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<ListView>()),
            PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(4000, controller.Offset);
        double previousOffset = controller.Offset;

        gesture.MoveTo(tester.GetTopLeft(tester.ElementOfType<ListView>()) + new Vector(0, -20));
        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset < previousOffset);
        previousOffset = controller.Offset;

        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset < previousOffset);

        // Scroll to the beginning.
        PumpAndSettle(tester, OneSecond);
        Assert.Equal(0.0, controller.Offset);
        RenderParagraph paragraph0 = Paragraph(tester, "Item 0");
        RenderParagraph paragraph1 = Paragraph(tester, "Item 1");
        RenderParagraph paragraph2 = Paragraph(tester, "Item 2");
        RenderParagraph paragraph3 = Paragraph(tester, "Item 3");
        AssertSelection(paragraph0, 6, 0);
        AssertSelection(paragraph1, 6, 0);
        AssertSelection(paragraph2, 6, 0);
        AssertSelection(paragraph3, 6, 0);
        gesture.RemovePointer();
    }

    // Flutter: 'select to scroll forward - horizontal'
    [Fact]
    public void SelectToScrollForwardHorizontal()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller, scrollDirection: Axis.Horizontal, itemCount: 10));
        tester.PumpAndSettle();

        RenderParagraph paragraph1 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(0.0, controller.Offset);
        double previousOffset = controller.Offset;

        // Scrollable only auto scroll if the drag passes the boundary
        gesture.MoveTo(tester.GetBottomRight(tester.ElementOfType<ListView>()) + new Vector(20, 0));
        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);
        previousOffset = controller.Offset;

        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);

        // Scroll to the end.
        PumpAndSettle(tester, OneSecond);
        Assert.Equal(2080.0, controller.Offset);
        RenderParagraph paragraph9 = Paragraph(tester, "Item 9");
        RenderParagraph paragraph8 = Paragraph(tester, "Item 8");
        RenderParagraph paragraph7 = Paragraph(tester, "Item 7");
        AssertSelection(paragraph9, 0, 6);
        AssertSelection(paragraph8, 0, 6);
        AssertSelection(paragraph7, 0, 6);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'select to scroll backward - horizontal'
    [Fact]
    public void SelectToScrollBackwardHorizontal()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller, scrollDirection: Axis.Horizontal, itemCount: 10));
        tester.PumpAndSettle();

        controller.JumpTo(2080);
        tester.PumpAndSettle();

        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementOfType<ListView>()),
            PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(2080, controller.Offset);
        double previousOffset = controller.Offset;

        gesture.MoveTo(tester.GetTopLeft(tester.ElementOfType<ListView>()) + new Vector(-10, 0));
        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset < previousOffset);
        previousOffset = controller.Offset;

        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset < previousOffset);

        // Scroll to the beginning.
        PumpAndSettle(tester, OneSecond);
        Assert.Equal(0.0, controller.Offset);
        RenderParagraph paragraph0 = Paragraph(tester, "Item 0");
        RenderParagraph paragraph1 = Paragraph(tester, "Item 1");
        RenderParagraph paragraph2 = Paragraph(tester, "Item 2");
        AssertSelection(paragraph0, 6, 0);
        AssertSelection(paragraph1, 6, 0);
        AssertSelection(paragraph2, 6, 0);

        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'preserve selection when out of view.'
    [Fact]
    public void PreserveSelectionWhenOutOfView()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller));

        controller.JumpTo(2000);
        tester.PumpAndSettle();
        Assert.Equal(1, CountText(tester, "Item 50"));
        RenderParagraph paragraph50 = Paragraph(tester, "Item 50");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph50, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.MoveTo(TextOffsetToPosition(paragraph50, 4));
        gesture.Up();
        AssertSelection(paragraph50, 2, 4);

        controller.JumpTo(0);
        tester.PumpAndSettle();
        Assert.Equal(0, CountText(tester, "Item 50"));

        controller.JumpTo(2000);
        tester.PumpAndSettle();
        Assert.Equal(1, CountText(tester, "Item 50"));
        paragraph50 = Paragraph(tester, "Item 50");
        AssertSelection(paragraph50, 2, 4);

        controller.JumpTo(4000);
        tester.PumpAndSettle();
        Assert.Equal(0, CountText(tester, "Item 50"));

        controller.JumpTo(2000);
        tester.PumpAndSettle();
        Assert.Equal(1, CountText(tester, "Item 50"));
        paragraph50 = Paragraph(tester, "Item 50");
        AssertSelection(paragraph50, 2, 4);
        gesture.RemovePointer();
    }

    // Flutter: 'can select all non-Apple'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Windows)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.Fuchsia)]
    public void CanSelectAllNonApple(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var node = new FocusNode();
        tester.PumpWidget(ItemList(focusNode: node));
        tester.PumpAndSettle();
        node.RequestFocus();
        SendKeyCombination(tester, LogicalKeyboardKey.KeyA, control: true);
        tester.Pump();

        for (int i = 0; i < 13; i += 1)
        {
            RenderParagraph paragraph = Paragraph(tester, $"Item {i}");
            AssertSelection(paragraph, 0, $"Item {i}".Length);
        }

        Assert.Equal(0, CountText(tester, "Item 13"));
    }

    // Flutter: 'can select all - Apple'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void CanSelectAllApple(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var node = new FocusNode();
        tester.PumpWidget(ItemList(focusNode: node));
        tester.PumpAndSettle();
        node.RequestFocus();
        SendKeyCombination(tester, LogicalKeyboardKey.KeyA, meta: true);
        tester.Pump();

        for (int i = 0; i < 13; i += 1)
        {
            RenderParagraph paragraph = Paragraph(tester, $"Item {i}");
            AssertSelection(paragraph, 0, $"Item {i}".Length);
        }

        Assert.Equal(0, CountText(tester, "Item 13"));
    }

    // Long press on 'Item 0' to bring up the selection handles; returns the paragraph and the gesture.
    private static (RenderParagraph Paragraph, TestGesture Gesture) LongPressItem0(FrameworkDartTester tester)
    {
        RenderParagraph paragraph0 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph0, 2), PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.LongPressTimeout);
        gesture.Up();
        tester.PumpAndSettle();
        AssertSelection(paragraph0, 0, 4);
        return (paragraph0, gesture);
    }

    // Flutter: 'select to scroll by dragging selection handles forward'
    [Fact]
    public void SelectToScrollByDraggingSelectionHandlesForward()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller));
        tester.PumpAndSettle();

        // Long press to bring up the selection handles.
        (RenderParagraph paragraph0, TestGesture gesture) = LongPressItem0(tester);

        IReadOnlyList<TextBox> boxes = paragraph0.GetBoxesForSelection(paragraph0.Selections[0]);
        Assert.Single(boxes);
        // Find end handle.
        Point handlePos = Globalize(boxes[0].ToRect().BottomRight, paragraph0);
        gesture.Down(handlePos);

        Assert.Equal(0.0, controller.Offset);
        double previousOffset = controller.Offset;
        // Scrollable only auto scroll if the drag passes the boundary
        gesture.MoveTo(tester.GetBottomRight(tester.ElementOfType<ListView>()) + new Vector(0, 40));
        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);
        previousOffset = controller.Offset;

        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);

        // Scroll to the end.
        PumpAndSettle(tester, OneSecond);
        Assert.Equal(4200.0, controller.Offset);
        RenderParagraph paragraph99 = Paragraph(tester, "Item 99");
        RenderParagraph paragraph98 = Paragraph(tester, "Item 98");
        RenderParagraph paragraph97 = Paragraph(tester, "Item 97");
        RenderParagraph paragraph96 = Paragraph(tester, "Item 96");
        AssertSelection(paragraph99, 0, 7);
        AssertSelection(paragraph98, 0, 7);
        AssertSelection(paragraph97, 0, 7);
        AssertSelection(paragraph96, 0, 7);
        gesture.Up();
        gesture.RemovePointer();
    }

    private static void DragHandleAndRelease(FrameworkDartTester tester, bool startHandle)
    {
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller));
        tester.PumpAndSettle();

        // Long press to bring up the selection handles.
        (RenderParagraph paragraph0, TestGesture gesture) = LongPressItem0(tester);

        IReadOnlyList<TextBox> boxes = paragraph0.GetBoxesForSelection(paragraph0.Selections[0]);
        Assert.Single(boxes);
        Rect box = boxes[0].ToRect();
        Point handlePos = Globalize(startHandle ? box.BottomLeft : box.BottomRight, paragraph0);
        gesture.Down(handlePos);

        Assert.Equal(0.0, controller.Offset);
        double previousOffset = controller.Offset;
        // Scrollable only auto scroll if the drag passes the boundary.
        gesture.MoveTo(tester.GetBottomRight(tester.ElementOfType<ListView>()) + new Vector(0, 40));
        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);
        previousOffset = controller.Offset;

        tester.Pump();
        tester.Pump(OneSecond);
        Assert.True(controller.Offset > previousOffset);

        // Release handle should stop scrolling.
        gesture.Up();
        // Last scheduled scroll.
        tester.Pump();
        tester.Pump(OneSecond);
        previousOffset = controller.Offset;
        tester.PumpAndSettle();
        Assert.Equal(previousOffset, controller.Offset);
        gesture.RemovePointer();
    }

    // Flutter: 'select to scroll by dragging start selection handle stops scroll when released'
    [Fact]
    public void SelectToScrollByDraggingStartSelectionHandleStopsScrollWhenReleased()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        DragHandleAndRelease(tester, startHandle: true);
    }

    // Flutter: 'select to scroll by dragging end selection handle stops scroll when released'
    [Fact]
    public void SelectToScrollByDraggingEndSelectionHandleStopsScrollWhenReleased()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        DragHandleAndRelease(tester, startHandle: false);
    }

    private static void AssertRange(RenderParagraph paragraph, int start, int end)
    {
        Assert.Single(paragraph.Selections);
        Assert.Equal(start, paragraph.Selections[0].Start);
        Assert.Equal(end, paragraph.Selections[0].End);
    }

    // Flutter: 'keyboard selection should auto scroll - vertical'
    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void KeyboardSelectionShouldAutoScrollVertical(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var node = new FocusNode();
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller, focusNode: node));
        tester.PumpAndSettle();
        RenderParagraph paragraph9 = Paragraph(tester, "Item 9");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph9, 2), PointerDeviceKind.Mouse);
        gesture.MoveTo(TextOffsetToPosition(paragraph9, 4) + new Vector(0, 5));
        tester.PumpAndSettle();
        gesture.Up();
        tester.Pump();
        AssertRange(paragraph9, 2, 4);
        Assert.Equal(0.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowDown, shift: true);
        tester.Pump();
        RenderParagraph paragraph10 = Paragraph(tester, "Item 10");
        AssertRange(paragraph10, 0, 4);
        Assert.Equal(0.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowDown, shift: true);
        tester.Pump();
        RenderParagraph paragraph11 = Paragraph(tester, "Item 11");
        AssertRange(paragraph11, 0, 4);
        Assert.Equal(0.0, controller.Offset);

        // Should start scrolling.
        SendKeyCombination(tester, LogicalKeyboardKey.ArrowDown, shift: true);
        tester.Pump();
        RenderParagraph paragraph12 = Paragraph(tester, "Item 12");
        AssertRange(paragraph12, 0, 4);
        Assert.Equal(24.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowDown, shift: true);
        tester.Pump();
        RenderParagraph paragraph13 = Paragraph(tester, "Item 13");
        AssertRange(paragraph13, 0, 4);
        Assert.Equal(72.0, controller.Offset);
        gesture.RemovePointer();
    }

    // Flutter: 'keyboard selection should auto scroll - vertical reversed'
    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void KeyboardSelectionShouldAutoScrollVerticalReversed(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var node = new FocusNode();
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller, reverse: true, focusNode: node));
        tester.PumpAndSettle();
        RenderParagraph paragraph9 = Paragraph(tester, "Item 9");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph9, 2), PointerDeviceKind.Mouse);
        gesture.MoveTo(TextOffsetToPosition(paragraph9, 4) + new Vector(0, 5));
        tester.PumpAndSettle();
        gesture.Up();
        tester.Pump();
        AssertRange(paragraph9, 2, 4);
        Assert.Equal(0.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowUp, shift: true);
        tester.Pump();
        RenderParagraph paragraph10 = Paragraph(tester, "Item 10");
        AssertRange(paragraph10, 2, 7);
        Assert.Equal(0.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowUp, shift: true);
        tester.Pump();
        RenderParagraph paragraph11 = Paragraph(tester, "Item 11");
        AssertRange(paragraph11, 2, 7);
        Assert.Equal(0.0, controller.Offset);

        // Should start scrolling.
        SendKeyCombination(tester, LogicalKeyboardKey.ArrowUp, shift: true);
        tester.Pump();
        RenderParagraph paragraph12 = Paragraph(tester, "Item 12");
        AssertRange(paragraph12, 2, 7);
        Assert.Equal(24.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowUp, shift: true);
        tester.Pump();
        RenderParagraph paragraph13 = Paragraph(tester, "Item 13");
        AssertRange(paragraph13, 2, 7);
        Assert.Equal(72.0, controller.Offset);
        gesture.RemovePointer();
    }

    // Flutter: 'keyboard selection should auto scroll - horizontal'
    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void KeyboardSelectionShouldAutoScrollHorizontal(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var node = new FocusNode();
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(controller: controller, scrollDirection: Axis.Horizontal, focusNode: node));
        tester.PumpAndSettle();
        RenderParagraph paragraph2 = Paragraph(tester, "Item 2");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph2, 0), PointerDeviceKind.Mouse);
        gesture.MoveTo(TextOffsetToPosition(paragraph2, 1) + new Vector(0, 5));
        tester.PumpAndSettle();
        gesture.Up();
        tester.Pump();
        AssertRange(paragraph2, 0, 1);
        Assert.Equal(0.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowDown, shift: true);
        tester.Pump();
        AssertRange(paragraph2, 0, 6);
        Assert.Equal(64.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowDown, shift: true);
        tester.Pump();
        RenderParagraph paragraph3 = Paragraph(tester, "Item 3");
        AssertRange(paragraph3, 0, 6);
        Assert.Equal(352.0, controller.Offset);
        gesture.RemovePointer();
    }

    // Flutter: 'keyboard selection should auto scroll - horizontal reversed'
    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void KeyboardSelectionShouldAutoScrollHorizontalReversed(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var node = new FocusNode();
        using var controller = new ScrollController();
        tester.PumpWidget(ItemList(
            controller: controller,
            scrollDirection: Axis.Horizontal,
            reverse: true,
            focusNode: node));
        tester.PumpAndSettle();
        RenderParagraph paragraph1 = Paragraph(tester, "Item 1");
        TestGesture gesture = tester.StartGesture(
            TextOffsetToPosition(paragraph1, 5) + new Vector(0, 5),
            PointerDeviceKind.Mouse);
        gesture.MoveTo(TextOffsetToPosition(paragraph1, 4) + new Vector(0, 5));
        tester.PumpAndSettle();
        gesture.Up();
        tester.PumpAndSettle();
        AssertRange(paragraph1, 4, 5);
        Assert.Equal(0.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowUp, shift: true);
        tester.Pump();
        AssertRange(paragraph1, 0, 5);
        Assert.Equal(0.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowUp, shift: true);
        tester.Pump();
        RenderParagraph paragraph2 = Paragraph(tester, "Item 2");
        AssertRange(paragraph2, 0, 6);
        Assert.Equal(64.0, controller.Offset);

        SendKeyCombination(tester, LogicalKeyboardKey.ArrowUp, shift: true);
        tester.Pump();
        RenderParagraph paragraph3 = Paragraph(tester, "Item 3");
        AssertRange(paragraph3, 0, 6);
        Assert.Equal(352.0, controller.Offset);
        gesture.RemovePointer();
    }

    // TargetPlatformVariant.all().
    public static TheoryData<TargetPlatform> AllPlatforms() => new()
    {
        TargetPlatform.Android,
        TargetPlatform.Fuchsia,
        TargetPlatform.IOS,
        TargetPlatform.Linux,
        TargetPlatform.MacOS,
        TargetPlatform.Windows,
    };

    // Flutter: 'Starting selection in empty padding of scrollable should not crash'
    [Fact]
    public void StartingSelectionInEmptyPaddingOfScrollableShouldNotCrash()
    {
        // Regression test for https://github.com/flutter/flutter/issues/115787
        const string text = "Some selectable text children";

        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(new TestWidgetsApp(
            home: new SelectableRegion(
                selectionControls: EmptyTextSelectionControls.Instance,
                child: new SingleChildScrollView(padding: EdgeInsets.All(50.0), child: new Text(text)))));
        tester.PumpAndSettle();

        Point paddingOffset =
            tester.GetTopLeft(tester.ElementOfType<SingleChildScrollView>()) + new Vector(20.0, 20.0);
        Point textCenter = tester.GetCenter(tester.ElementsWithText(text).Single());

        TestGesture gesture = tester.StartGesture(paddingOffset, PointerDeviceKind.Touch);

        // Simulate long press.
        tester.Pump(GestureConstants.LongPressTimeout);

        // Drag into the text content.
        gesture.MoveTo(textCenter);
        tester.Pump();

        gesture.Up();
        tester.PumpAndSettle();

        Assert.Null(tester.TakeException());
        gesture.RemovePointer();
    }

    // Flutter: 'Fast drag starting in padding correctly triggers auto-scroll'
    [Fact]
    public void FastDragStartingInPaddingCorrectlyTriggersAutoScroll()
    {
        string text = string.Concat(Enumerable.Repeat(
            "Some selectable text children that is long enough to make it scrollable \n",
            20));

        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(new TestWidgetsApp(
            home: new SelectableRegion(
                selectionControls: EmptyTextSelectionControls.Instance,
                child: new SizedBox(
                    height: 200.0,
                    child: new SingleChildScrollView(
                        padding: EdgeInsets.Only(top: 50.0),
                        child: new Text(text))))));
        tester.PumpAndSettle();

        ScrollPosition position = tester.State<ScrollableState>().Position;
        Assert.Equal(0.0, position.Pixels);

        // Get a point inside the padding (which is inside the scrollable).
        Point scrollableTopLeft = tester.GetTopLeft(tester.ElementOfType<SingleChildScrollView>());
        Point paddingStartOffset = scrollableTopLeft + new Vector(50.0, 20.0); // Inside padding.

        // Get a point outside the bottom of the scrollable to trigger downward auto-scrolling.
        Point dragEndOffset =
            GetBottomLeft(tester, tester.ElementOfType<SingleChildScrollView>()) + new Vector(50.0, 100.0);

        // Start gesture perfectly on padding.
        TestGesture gesture = tester.StartGesture(paddingStartOffset, PointerDeviceKind.Touch);

        // Simulate long press.
        tester.Pump(GestureConstants.LongPressTimeout);

        // First drag update is far ALREADY OUTSIDE the scrollable.
        // Emulates a very fast drag movement (so the first EdgeUpdate frame is processed outside).
        gesture.MoveTo(dragEndOffset);
        tester.Pump();

        // Let auto-scroller run for a few frames.
        tester.Pump(TimeSpan.FromMilliseconds(50));
        tester.Pump(TimeSpan.FromMilliseconds(50));
        tester.Pump(TimeSpan.FromMilliseconds(50));

        // If _selectionStartsInScrollable was correctly preserved as TRUE,
        // the scrollable will have started auto-scrolling downwards.
        Assert.True(position.Pixels > 0.0);
        gesture.Up();
        gesture.RemovePointer();
    }

    // Flutter: 'automatic edge scrolling respects NeverScrollableScrollPhysics'
    [Fact]
    public void AutomaticEdgeScrollingRespectsNeverScrollableScrollPhysics()
    {
        // Regression test for https://github.com/flutter/flutter/issues/140654.
        // When a scrollable view with non-scrollable physics (e.g.,
        // NeverScrollableScrollPhysics) is wrapped in a SelectableRegion, dragging
        // a selection past the viewport boundary must not advance the scroll offset
        // or throw exceptions.
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new SelectableRegion(
                selectionControls: TestTextSelectionHandleControls.Instance,
                child: ListView.Builder(
                    controller: controller,
                    physics: new NeverScrollableScrollPhysics(),
                    itemCount: 100,
                    itemBuilder: (_, index) => new Text($"Item {index}")))));
        tester.PumpAndSettle();

        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithText("Item 0").Single()),
            PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(0.0, controller.Offset);

        // Drag past the bottom of the scrollable; this would normally trigger
        // edge auto-scroll.
        gesture.MoveTo(tester.GetBottomRight(tester.ElementOfType<ListView>()) + new Vector(0.0, 40.0));
        tester.Pump();
        tester.Pump(OneSecond);

        // The scroll position must not have advanced, and no exception must have
        // been thrown.
        Assert.Equal(0.0, controller.Offset);
        Assert.Null(tester.TakeException());

        tester.Pump(OneSecond);
        Assert.Equal(0.0, controller.Offset);
        Assert.Null(tester.TakeException());

        gesture.Up();
        tester.PumpAndSettle();
        Assert.Equal(0.0, controller.Offset);
        Assert.Null(tester.TakeException());
        gesture.RemovePointer();
    }

    // Flutter: 'Complex cases' / 'selection starts outside of the scrollable'
    [Fact]
    public void ComplexCasesSelectionStartsOutsideOfTheScrollable()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        tester.PumpWidget(new MaterialApp(
            home: new SelectionArea(
                selectionControls: MaterialTextSelectionControls.Instance,
                child: new Column(
                    children:
                    [
                        new Text("Item 0"),
                        new SizedBox(
                            height: 400,
                            child: ListView.Builder(
                                controller: controller,
                                itemCount: 100,
                                itemBuilder: (_, index) => new Text($"Inner item {index}"))),
                        new Text("Item 1"),
                    ]))));
        tester.PumpAndSettle();

        controller.JumpTo(1000);
        tester.PumpAndSettle();
        RenderParagraph paragraph0 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph0, 2), PointerDeviceKind.Mouse);
        RenderParagraph paragraph1 = Paragraph(tester, "Item 1");
        gesture.MoveTo(TextOffsetToPosition(paragraph1, 2) + new Vector(0, 5));
        tester.PumpAndSettle();
        gesture.Up();

        // The entire scrollable should be selected.
        AssertSelection(paragraph0, 2, 6);
        AssertSelection(paragraph1, 0, 2);
        RenderParagraph innerParagraph = Paragraph(tester, "Inner item 20");
        AssertSelection(innerParagraph, 0, 13);
        // Should not scroll the inner scrollable.
        Assert.Equal(1000.0, controller.Offset);
        gesture.RemovePointer();
    }

    // Flutter: 'Complex cases' / 'nested scrollables keep selection alive'
    [Fact]
    public void ComplexCasesNestedScrollablesKeepSelectionAlive()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var outerController = new ScrollController();
        using var innerController = new ScrollController();
        tester.PumpWidget(new MaterialApp(
            home: new SelectionArea(
                selectionControls: MaterialTextSelectionControls.Instance,
                child: ListView.Builder(
                    controller: outerController,
                    itemCount: 100,
                    itemBuilder: (_, index) =>
                    {
                        if (index == 2)
                        {
                            return new SizedBox(
                                height: 700,
                                child: ListView.Builder(
                                    controller: innerController,
                                    itemCount: 100,
                                    itemBuilder: (_, innerIndex) => new Text($"Iteminner {innerIndex}")));
                        }

                        return new Text($"Item {index}");
                    }))));
        tester.PumpAndSettle();

        innerController.JumpTo(1000);
        tester.PumpAndSettle();
        RenderParagraph innerParagraph23 = Paragraph(tester, "Iteminner 23");
        TestGesture gesture = tester.StartGesture(
            TextOffsetToPosition(innerParagraph23, 2) + new Vector(0, 5),
            PointerDeviceKind.Mouse);
        RenderParagraph innerParagraph24 = Paragraph(tester, "Iteminner 24");
        gesture.MoveTo(TextOffsetToPosition(innerParagraph24, 2) + new Vector(0, 5));
        tester.PumpAndSettle();
        gesture.Up();
        AssertSelection(innerParagraph23, 2, 12);
        AssertSelection(innerParagraph24, 0, 2);

        innerController.JumpTo(2000);
        tester.PumpAndSettle();
        Assert.Empty(FindParagraphs(tester, "Iteminner 23"));

        outerController.JumpTo(2000);
        tester.PumpAndSettle();
        Assert.Empty(FindParagraphs(tester, "Iteminner 23"));

        // Selected item is still kept alive.
        Assert.Empty(AllParagraphs(tester, "Iteminner 23"));

        // Selection stays the same after scrolling back.
        outerController.JumpTo(0);
        tester.PumpAndSettle();
        Assert.Equal(2000.0, innerController.Offset);
        innerController.JumpTo(1000);
        tester.PumpAndSettle();
        innerParagraph23 = Paragraph(tester, "Iteminner 23");
        innerParagraph24 = Paragraph(tester, "Iteminner 24");
        AssertSelection(innerParagraph23, 2, 12);
        AssertSelection(innerParagraph24, 0, 2);
        gesture.RemovePointer();
    }

    // `find.descendant(of: find.text(text), matching: find.byType(RichText), skipOffstage: false)`:
    // `skipOffstage` only widens the descendant walk; the `of:` finder keeps its default onstage walk.
    private static List<RenderParagraph> AllParagraphs(FrameworkDartTester tester, string text)
    {
        var result = new List<RenderParagraph>();
        foreach (Element element in OnstageElements(tester)
                     .Where(element => element.Widget is Text { Data: var data } && data == text))
        {
            void Visit(Element child)
            {
                if (child.Widget is RichText && child.RenderObject is RenderParagraph paragraph)
                {
                    result.Add(paragraph);
                }

                child.VisitChildren(Visit);
            }

            element.VisitChildren(Visit);
        }

        return result;
    }

    private void CopyOffScreenSelection(bool control, bool meta)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        using var controller = new ScrollController();
        using var focusNode = new FocusNode();
        tester.PumpWidget(ItemList(controller: controller, focusNode: focusNode));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        RenderParagraph paragraph0 = Paragraph(tester, "Item 0");
        TestGesture gesture = tester.StartGesture(
            TextOffsetToPosition(paragraph0, 2) + new Vector(0, 5),
            PointerDeviceKind.Mouse);
        RenderParagraph paragraph1 = Paragraph(tester, "Item 1");
        gesture.MoveTo(TextOffsetToPosition(paragraph1, 2) + new Vector(0, 5));
        tester.PumpAndSettle();
        gesture.Up();
        AssertSelection(paragraph0, 2, 6);
        AssertSelection(paragraph1, 0, 2);

        // Scroll the selected text out off the screen.
        controller.JumpTo(1000);
        tester.PumpAndSettle();
        Assert.Empty(FindParagraphs(tester, "Item 0"));
        Assert.Empty(FindParagraphs(tester, "Item 1"));

        // Start copying.
        SendKeyCombination(tester, LogicalKeyboardKey.KeyC, control: control, meta: meta);

        Assert.Equal("em 0It", _clipboard.Text);
        gesture.RemovePointer();
    }

    // Flutter: 'Complex cases' / 'can copy off screen selection - Apple'
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void ComplexCasesCanCopyOffScreenSelectionApple(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        CopyOffScreenSelection(control: false, meta: true);
    }

    // Flutter: 'Complex cases' / 'can copy off screen selection - non-Apple'
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Windows)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.Fuchsia)]
    public void ComplexCasesCanCopyOffScreenSelectionNonApple(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        CopyOffScreenSelection(control: true, meta: false);
    }
}
