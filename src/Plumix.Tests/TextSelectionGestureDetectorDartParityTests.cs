using Avalonia;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources: flutter/packages/flutter/test/widgets/text_selection_test.dart (the
// `TextSelectionGestureDetector` group) and the gesture tests of
// material_ui/test/text_field_test.dart, which exercise `TextSelectionGestureDetectorBuilder`
// through `TextField`.
public sealed class TextSelectionGestureDetectorDartParityTests : IDisposable
{
    private int _tapCount;
    private int _singleTapUpCount;
    private int _singleTapCancelCount;
    private int _singleLongTapStartCount;
    private int _doubleTapDownCount;
    private int _tripleTapDownCount;
    private int _dragStartCount;
    private int _dragUpdateCount;
    private int _dragEndCount;

    public TextSelectionGestureDetectorDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private FrameworkDartTester PumpGestureDetector()
    {
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new TextSelectionGestureDetector(
            behavior: HitTestBehavior.Opaque,
            onTapDown: _ => _tapCount++,
            onSingleTapUp: _ => _singleTapUpCount++,
            onSingleTapCancel: () => _singleTapCancelCount++,
            onSingleLongTapStart: _ => _singleLongTapStartCount++,
            onDoubleTapDown: _ => _doubleTapDownCount++,
            onTripleTapDown: _ => _tripleTapDownCount++,
            onDragSelectionStart: _ => _dragStartCount++,
            onDragSelectionUpdate: _ => _dragUpdateCount++,
            onDragSelectionEnd: _ => _dragEndCount++,
            child: new Container()));
        return tester;
    }

    private static void TapAt(FrameworkDartTester tester, Point location, PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        TestGesture gesture = tester.StartGesture(location, kind);
        gesture.Up();
    }

    [Fact]
    public void ASeriesOfTapsAllCallOnTaps()
    {
        using FrameworkDartTester tester = PumpGestureDetector();
        for (int i = 0; i < 6; i++)
        {
            TapAt(tester, new Point(200, 200));
            tester.Pump(TimeSpan.FromMilliseconds(150));
        }

        Assert.Equal(6, _tapCount);
    }

    [Fact]
    public void InASeriesOfRapidTapsOnTapDownOnDoubleTapDownAndOnTripleTapDownAlternate()
    {
        using FrameworkDartTester tester = PumpGestureDetector();
        int[] singleTapUp = [1, 1, 1, 2, 2, 2, 3];
        int[] doubleTapDown = [0, 1, 1, 1, 2, 2, 2];
        int[] tripleTapDown = [0, 0, 1, 1, 1, 2, 2];
        for (int i = 0; i < 7; i++)
        {
            TapAt(tester, new Point(200, 200));
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.Equal(singleTapUp[i], _singleTapUpCount);
            Assert.Equal(doubleTapDown[i], _doubleTapDownCount);
            Assert.Equal(tripleTapDown[i], _tripleTapDownCount);
        }

        Assert.Equal(7, _tapCount);
    }

    [Fact]
    public void QuickTapTapHoldIsADoubleTapDown()
    {
        using FrameworkDartTester tester = PumpGestureDetector();
        TapAt(tester, new Point(200, 200));
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(1, _singleTapUpCount);

        TestGesture gesture = tester.StartGesture(new Point(200, 200), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(1, _singleTapUpCount);
        // The double tap down is sent right away.
        Assert.Equal(2, _tapCount);
        Assert.Equal(0, _singleTapCancelCount);
        Assert.Equal(1, _doubleTapDownCount);
        // The double tap down hold supersedes the single tap down.
        Assert.Equal(0, _singleLongTapStartCount);

        gesture.Up();
        // Nothing else happens on up.
        Assert.Equal(1, _singleTapUpCount);
        Assert.Equal(2, _tapCount);
        Assert.Equal(1, _doubleTapDownCount);
        Assert.Equal(0, _singleLongTapStartCount);
    }

    [Fact]
    public void ALongPressFromATouchDeviceIsRecognizedAsALongSingleTap()
    {
        using FrameworkDartTester tester = PumpGestureDetector();
        TestGesture gesture = tester.StartGesture(new Point(200, 200), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromSeconds(2));
        gesture.Up();
        tester.Pump();

        Assert.Equal(1, _tapCount);
        Assert.Equal(0, _singleTapUpCount);
        Assert.Equal(1, _singleLongTapStartCount);
    }

    [Fact]
    public void ALongPressFromAMouseIsJustATap()
    {
        using FrameworkDartTester tester = PumpGestureDetector();
        TestGesture gesture = tester.StartGesture(new Point(200, 200), PointerDeviceKind.Mouse);
        tester.Pump(TimeSpan.FromSeconds(2));
        gesture.Up();
        tester.Pump();

        Assert.Equal(1, _tapCount);
        Assert.Equal(1, _singleTapUpCount);
        Assert.Equal(0, _singleLongTapStartCount);
    }

    [Theory]
    [InlineData(PointerDeviceKind.Touch)]
    [InlineData(PointerDeviceKind.Mouse)]
    public void ADragIsRecognizedForTextSelection(PointerDeviceKind kind)
    {
        using FrameworkDartTester tester = PumpGestureDetector();
        TestGesture gesture = tester.StartGesture(new Point(200, 200), kind);
        tester.Pump();
        gesture.MoveBy(new Vector(210.0, 200.0));
        tester.Pump();
        gesture.Up();
        tester.Pump();

        Assert.Equal(1, _tapCount);
        Assert.Equal(0, _singleTapUpCount);
        Assert.Equal(0, _singleTapCancelCount);
        Assert.Equal(1, _dragStartCount);
        Assert.Equal(1, _dragUpdateCount);
        Assert.Equal(1, _dragEndCount);
    }

    // ---------------------------------------------------------------- TextField

    private readonly TextEditingController _controller = new();

    private FrameworkDartTester PumpTextField(
        TargetPlatform platform,
        int? maxLines = 1,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Material.Material(
                child: new Center(
                    child: new TextField(
                        controller: _controller,
                        maxLines: maxLines,
                        dragStartBehavior: dragStartBehavior)))));
        tester.Pump();
        return tester;
    }

    private static RenderEditable FindRenderEditable(FrameworkDartTester tester) =>
        tester.AllElements().Select(element => element.RenderObject).OfType<RenderEditable>().Distinct().Single();

    // Dart's `textOffsetToPosition`.
    private static Point TextOffsetToPosition(FrameworkDartTester tester, int offset)
    {
        RenderEditable editable = FindRenderEditable(tester);
        IReadOnlyList<TextSelectionPoint> endpoints =
            editable.GetEndpointsForSelection(TextSelection.Collapsed(offset));
        Assert.Single(endpoints);
        Point global = editable.LocalToGlobal(endpoints[0].Point);
        return new Point(global.X, global.Y - 2.0);
    }

    private static void Tap(
        FrameworkDartTester tester,
        Point location,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        bool shift = false)
    {
        if (shift)
        {
            _ = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ShiftLeft, shift: true));
        }

        TapAt(tester, location, kind);
        if (shift)
        {
            _ = FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.ShiftLeft));
        }
    }

    [Fact]
    public void CaretPositionIsUpdatedOnTap()
    {
        _controller.Text = "abc def ghi";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Android);

        Tap(tester, TextOffsetToPosition(tester, _controller.Text.IndexOf('e', StringComparison.Ordinal)));
        tester.Pump();

        Assert.Equal(TextSelection.Collapsed(5), _controller.Selection);
    }

    [Fact]
    public void SelectionUpdatesOnTapDownOnDesktopPlatforms()
    {
        _controller.Text = "abc def ghi";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Windows);

        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(tester, 5), PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(5), _controller.Selection);
        gesture.Up();
        tester.Pump(TimeSpan.FromMilliseconds(500));

        gesture = tester.StartGesture(TextOffsetToPosition(tester, 8), PointerDeviceKind.Mouse);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);
        gesture.Up();
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);
    }

    [Fact]
    public void SelectionUpdatesOnTapUpOnMobilePlatforms()
    {
        _controller.Text = "abc def ghi";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Android);

        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(tester, 5), PointerDeviceKind.Touch);
        tester.Pump();
        TextSelection beforeUp = _controller.Selection;
        Assert.NotEqual(TextSelection.Collapsed(5), beforeUp);

        gesture.Up();
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(5), _controller.Selection);
    }

    [Fact]
    public void TapMovesTheCursorToTheEdgeOfTheWordItTappedOnIOS()
    {
        _controller.Text = "Atwater Peel Sherbrooke Bonaventure";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.IOS);

        // Tap inside "Atwater": the caret goes to the end of the word.
        Tap(tester, TextOffsetToPosition(tester, 3));
        tester.Pump();

        Assert.Equal(TextSelection.Collapsed(7, TextAffinity.Upstream), _controller.Selection);
    }

    [Fact]
    public void DoubleTapSelectsWordAndFirstTapMovesCursor()
    {
        _controller.Text = "Atwater Peel Sherbrooke Bonaventure";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Android);
        Point pPos = TextOffsetToPosition(tester, 9); // Index of 'P|eel'

        Tap(tester, pPos);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(TextSelection.Collapsed(9), _controller.Selection);

        Tap(tester, pPos);
        tester.PumpAndSettle();
        Assert.Equal(new TextSelection(8, 12), _controller.Selection);
    }

    [Fact]
    public void DoubleTapSelectsWordAndFirstTapMovesCursorToTheWordEdgeOnIOS()
    {
        _controller.Text = "Atwater Peel Sherbrooke Bonaventure";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.IOS);
        Point pPos = TextOffsetToPosition(tester, 9); // Index of 'P|eel'

        Tap(tester, pPos);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(TextSelection.Collapsed(12, TextAffinity.Upstream), _controller.Selection);

        Tap(tester, pPos);
        tester.PumpAndSettle();
        Assert.Equal(new TextSelection(8, 12), _controller.Selection);
    }

    [Fact]
    public void CanLongPressToSelect()
    {
        _controller.Text = "abc def ghi";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Android);

        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(tester, 5), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromSeconds(1));
        gesture.Up();
        tester.PumpAndSettle();

        Assert.Equal(new TextSelection(4, 7), _controller.Selection);
    }

    [Fact]
    public void CanSelectTextByDraggingWithAMouse()
    {
        _controller.Text = "abc def ghi";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Android);
        int eIndex = _controller.Text.IndexOf('e', StringComparison.Ordinal);
        int gIndex = _controller.Text.IndexOf('g', StringComparison.Ordinal);

        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(tester, eIndex), PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.MoveTo(TextOffsetToPosition(tester, gIndex));
        tester.Pump();
        gesture.Up();
        tester.PumpAndSettle();

        Assert.Equal(new TextSelection(eIndex, gIndex), _controller.Selection);
    }

    // Dart: 'Can move cursor when dragging (Android)'.
    [Fact]
    public void TouchDragMovesTheCursorOnAndroid()
    {
        const string testValue = "abc def ghi";
        _controller.Text = testValue;
        // Dart pumps this TextField with `dragStartBehavior: DragStartBehavior.down`: the drag starts
        // on the collapsed handle, and only a down-behavior drag reports the move as an update.
        using FrameworkDartTester tester = PumpTextField(
            TargetPlatform.Android,
            dragStartBehavior: DragStartBehavior.Down);
        Point ePos = TextOffsetToPosition(tester, testValue.IndexOf('e', StringComparison.Ordinal));
        Point gPos = TextOffsetToPosition(tester, testValue.IndexOf('g', StringComparison.Ordinal));

        // Tap on text field to gain focus, and set selection to '|e'. Wait for the double tap
        // timeout after the up event, so the next down event does not register as a double tap.
        TestGesture gesture = tester.StartGesture(ePos, PointerDeviceKind.Touch);
        tester.Pump();
        gesture.Up();
        tester.Pump(TimeSpan.FromMilliseconds(300));
        tester.PumpAndSettle();
        Assert.True(_controller.Selection.IsCollapsed);
        Assert.Equal(testValue.IndexOf('e', StringComparison.Ordinal), _controller.Selection.BaseOffset);

        // Here we tap on '|d', and move to '|g'.
        gesture.Down(TextOffsetToPosition(tester, testValue.IndexOf('d', StringComparison.Ordinal)));
        tester.Pump();
        gesture.MoveTo(gPos);
        tester.PumpAndSettle();

        Assert.True(_controller.Selection.IsCollapsed);
        Assert.Equal(testValue.IndexOf('g', StringComparison.Ordinal), _controller.Selection.BaseOffset);
    }

    [Fact]
    public void CanShiftTapToExtendTheSelectionOnNonApplePlatforms()
    {
        _controller.Text = "Atwater Peel Sherbrooke Bonaventure";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Windows);

        Tap(tester, TextOffsetToPosition(tester, 13), PointerDeviceKind.Mouse);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(TextSelection.Collapsed(13), _controller.Selection);

        Tap(tester, TextOffsetToPosition(tester, 20), PointerDeviceKind.Mouse, shift: true);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new TextSelection(13, 20), _controller.Selection);

        // Shift + tap before the base keeps the base: the selection is extended, not expanded.
        Tap(tester, TextOffsetToPosition(tester, 4), PointerDeviceKind.Mouse, shift: true);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new TextSelection(13, 4), _controller.Selection);
    }

    [Fact]
    public void CanShiftTapToExpandTheSelectionOnApplePlatforms()
    {
        _controller.Text = "Atwater Peel Sherbrooke Bonaventure";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.MacOS);

        Tap(tester, TextOffsetToPosition(tester, 13), PointerDeviceKind.Mouse);
        tester.Pump(TimeSpan.FromMilliseconds(500));

        Tap(tester, TextOffsetToPosition(tester, 20), PointerDeviceKind.Mouse, shift: true);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new TextSelection(13, 20), _controller.Selection);

        // Shift + tap before the base moves the closer end: the selection grows and pivots.
        Tap(tester, TextOffsetToPosition(tester, 4), PointerDeviceKind.Mouse, shift: true);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new TextSelection(20, 4), _controller.Selection);
    }

    [Fact]
    public void CanTripleTapToSelectAParagraphOnMobilePlatforms()
    {
        _controller.Text = "Now is the time for\nall good people";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Android, maxLines: null);
        // Inside "time", clear of the handles the double tap shows at the word's edges.
        Point position = TextOffsetToPosition(tester, 13);

        Tap(tester, position);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Tap(tester, position);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Tap(tester, position);
        tester.PumpAndSettle();

        Assert.Equal(new TextSelection(0, 20), _controller.Selection);
    }

    [Fact]
    public void CanTripleTapToSelectAllOnASingleLineTextField()
    {
        _controller.Text = "Atwater Peel Sherbrooke Bonaventure";
        using FrameworkDartTester tester = PumpTextField(TargetPlatform.Android);
        Point position = TextOffsetToPosition(tester, 4);

        Tap(tester, position);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Tap(tester, position);
        tester.Pump(TimeSpan.FromMilliseconds(50));
        Tap(tester, position);
        tester.PumpAndSettle();

        Assert.Equal(new TextSelection(0, _controller.Text.Length), _controller.Selection);
    }
}
