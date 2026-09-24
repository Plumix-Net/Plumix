using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: cupertino_ui/test/dialog_test.dart

namespace Plumix.Tests;

public sealed class CupertinoDialogGestureTests : IDisposable
{
    private static readonly Size ViewSize = new(600, 600);
    private static readonly DateTime EventTime = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    public CupertinoDialogGestureTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScrollWinsAfterHorizontalMotionAndClearsPressedFill(bool sheet)
    {
        int presses = 0;
        using var controller = new ScrollController();
        using var harness = Harness(Control(sheet, () => presses++, controller, 20));
        harness.Pump(ViewSize);
        Point start = Center(Paragraph(harness, "Action 0"));
        harness.HandlePointerEvent(Down(1, start));
        harness.Pump(ViewSize);
        Assert.True(HasPressedFill(harness, sheet));

        Point sideways = start + new Vector(100, 0);
        harness.HandlePointerEvent(Move(1, sideways));
        harness.Pump(ViewSize);
        Assert.True(HasPressedFill(harness, sheet));
        harness.HandlePointerEvent(Move(1, sideways + new Vector(0, -30)));
        harness.HandlePointerEvent(Move(1, sideways + new Vector(0, -80)));
        harness.Pump(ViewSize);
        Assert.True(controller.Offset > 0);
        Assert.False(HasPressedFill(harness, sheet));
        harness.HandlePointerEvent(Up(1, sideways + new Vector(0, -80)));
        Assert.Equal(0, presses);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondPointerCanScrollAndCancelPrimarySelection(bool sheet)
    {
        int presses = 0;
        using var controller = new ScrollController();
        using var harness = Harness(Control(sheet, () => presses++, controller, 20));
        harness.Pump(ViewSize);
        Point start = Center(Paragraph(harness, "Action 0"));
        harness.HandlePointerEvent(Down(1, start));
        harness.HandlePointerEvent(Down(2, start));
        harness.HandlePointerEvent(Move(2, start + new Vector(0, -30)));
        harness.HandlePointerEvent(Move(2, start + new Vector(0, -80)));
        harness.Pump(ViewSize);
        Assert.True(controller.Offset > 0);
        Assert.False(HasPressedFill(harness, sheet));
        harness.HandlePointerEvent(Up(2, start + new Vector(0, -80)));
        harness.HandlePointerEvent(Up(1, start));
        Assert.Equal(0, presses);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShortSlideAcrossBoundarySelectsBeforeDragThreshold(bool sheet)
    {
        string? result = null;
        Widget[] actions =
        [
            Action(sheet, "First", () => result = "First"),
            Action(sheet, "Second", () => result = "Second"),
        ];
        Widget control = sheet
            ? new CupertinoActionSheet(actions: actions)
            : new CupertinoAlertDialog(actions: actions);
        using var harness = Harness(control);
        harness.Pump(ViewSize);
        RenderBox second = ActionBox(Paragraph(harness, "Second"));
        Point origin = second.GetPaintOffsetToRoot();
        Point start = sheet
            ? new Point(origin.X + 20, origin.Y - 5)
            : new Point(origin.X - 5, origin.Y + 20);
        Point end = start + (sheet ? new Vector(0, 10) : new Vector(10, 0));
        harness.HandlePointerEvent(Down(1, start));
        harness.HandlePointerEvent(Move(1, end));
        harness.Pump(ViewSize);
        Assert.True(HasPressedFill(harness, sheet));
        harness.HandlePointerEvent(Up(1, end));
        Assert.Equal("Second", result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReleaseRehitTestsPositionEvenWithoutAMove(bool sheet)
    {
        string? result = null;
        Widget[] actions =
        [
            Action(sheet, "First", () => result = "First"),
            Action(sheet, "Second", () => result = "Second"),
        ];
        using var harness = Harness(sheet
            ? new CupertinoActionSheet(actions: actions)
            : new CupertinoAlertDialog(actions: actions));
        harness.Pump(ViewSize);
        harness.HandlePointerEvent(Down(1, Center(Paragraph(harness, "First"))));
        harness.HandlePointerEvent(Up(1, Center(Paragraph(harness, "Second"))));
        Assert.Equal("Second", result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelAndSecondaryButtonNeverConfirm(bool sheet)
    {
        int presses = 0;
        using var harness = Harness(Control(sheet, () => presses++, count: 20));
        harness.Pump(ViewSize);
        Point position = Center(Paragraph(harness, "Action 0"));
        harness.HandlePointerEvent(Down(1, position, PointerButtons.Secondary));
        harness.Pump(ViewSize);
        Assert.False(HasPressedFill(harness, sheet));
        harness.HandlePointerEvent(Up(1, position));
        harness.HandlePointerEvent(Down(2, position));
        harness.HandlePointerEvent(new PointerCancelEvent(
            2, PointerDeviceKind.Touch, position, PointerButtons.None, EventTime));
        harness.Pump(ViewSize);
        Assert.False(HasPressedFill(harness, sheet));
        Assert.Equal(0, presses);
        // A fresh sequence can still confirm after cancellation.
        harness.HandlePointerEvent(Down(3, position));
        harness.HandlePointerEvent(Up(3, position));
        Assert.Equal(1, presses);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CoveringOverlayRemovesTargetsFromGlobalHitTest(bool sheet)
    {
        int presses = 0;
        Widget control = Control(sheet, () => presses++);
        using var harness = Harness(new Stack(children: [control, new SizedBox(width: 0, height: 0)]));
        harness.Pump(ViewSize);
        Point start = Center(Paragraph(harness, "Action 0"));
        harness.HandlePointerEvent(Down(1, start));
        harness.PumpWidget(Wrap(new Stack(children:
        [
            control,
            new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: new ColoredBox(Colors.Black)),
        ])));
        harness.Pump(ViewSize);
        harness.HandlePointerEvent(Up(1, start));
        Assert.Equal(0, presses);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyGestureDetectorTakesPriorityOverSlidingTap(bool sheet)
    {
        int legacyTaps = 0;
        Widget legacy = new GestureDetector(
            onTap: () => legacyTaps++,
            behavior: HitTestBehavior.Opaque,
            child: new ConstrainedBox(
                new BoxConstraints(MinHeight: sheet ? 57.0 : 45.0),
                child: new Padding(new Thickness(10, 16), child: new Center(child: new Text("Legacy")))));
        using var harness = Harness(sheet
            ? new CupertinoActionSheet(actions: [legacy])
            : new CupertinoAlertDialog(actions: [legacy]));
        harness.Pump(ViewSize);
        Point start = Center(Paragraph(harness, "Legacy"));
        harness.HandlePointerEvent(Down(1, start));
        harness.Pump(ViewSize);
        Assert.True(HasPressedFill(harness, sheet));
        harness.HandlePointerEvent(Up(1, start));
        harness.Pump(ViewSize);
        Assert.Equal(1, legacyTaps);
        Assert.False(HasPressedFill(harness, sheet));
    }

    [Fact]
    public void TargetCallbacksFollowInnerToOuterOrderAndEnabledChain()
    {
        var log = new List<string>();
        var inner = new SlideTarget("inner", log, enabled: false);
        var outer = new SlideTarget("outer", log);
        using var recognizer = new TargetSelectionGestureRecognizer(_ => Path(inner, outer));
        recognizer.AddPointer(Down(1, default));
        GestureBinding.Instance.GestureArena.Close(1);
        GestureBinding.Instance.GestureArena.FlushDefaultResolutions();
        GestureBinding.Instance.PointerRouter.Route(Up(1, default));
        Assert.Equal(["inner:enter:True:True", "outer:enter:True:False", "inner:confirm", "outer:confirm"], log);
    }

    [Fact]
    public void UnchangedInnermostTargetSkipsOuterChangesAndDisposeDoesNotLeave()
    {
        var log = new List<string>();
        var inner = new SlideTarget("inner", log);
        var outer = new SlideTarget("outer", log);
        bool includeOuter = true;
        var recognizer = new TargetSelectionGestureRecognizer(_ => includeOuter ? Path(inner, outer) : Path(inner));
        recognizer.AddPointer(Down(1, default));
        GestureBinding.Instance.GestureArena.Close(1);
        GestureBinding.Instance.GestureArena.FlushDefaultResolutions();
        includeOuter = false;
        GestureBinding.Instance.PointerRouter.Route(Move(1, new Point(1, 0)));
        recognizer.Dispose();
        Assert.Equal(["inner:enter:True:True", "outer:enter:True:True"], log);
    }

    [Fact]
    public void PrimaryEndsThenNewPointerSelectsWhileOldSecondaryIsStillTracked()
    {
        var log = new List<string>();
        var targets = Enumerable.Range(0, 5).Select(index => new SlideTarget($"target{index}", log)).ToArray();
        using var recognizer = new TargetSelectionGestureRecognizer(position => Path(targets[(int)position.X]));
        void Begin(int pointer, int target)
        {
            recognizer.AddPointer(Down(pointer, new Point(target, 0)));
            GestureBinding.Instance.GestureArena.Close(pointer);
            GestureBinding.Instance.GestureArena.FlushDefaultResolutions();
        }

        Begin(1, 0);
        Begin(2, 1);
        GestureBinding.Instance.PointerRouter.Route(Move(1, new Point(2, 0)));
        GestureBinding.Instance.PointerRouter.Route(Up(1, new Point(2, 0)));
        Begin(3, 3);
        GestureBinding.Instance.PointerRouter.Route(Move(2, new Point(4, 0)));
        GestureBinding.Instance.PointerRouter.Route(Up(3, new Point(3, 0)));
        GestureBinding.Instance.PointerRouter.Route(Up(2, new Point(4, 0)));
        Assert.Equal(["target2:confirm", "target3:confirm"], log.Where(entry => entry.EndsWith(":confirm")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartingOutsideDetectorCannotSlideIntoActions(bool sheet)
    {
        int presses = 0;
        using var harness = Harness(new Stack(children:
        [
            new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: new ColoredBox(Colors.Black)),
            new Align(alignment: Alignment.BottomCenter, child: Control(sheet, () => presses++)),
        ]));
        harness.Pump(ViewSize);
        Point end = Center(Paragraph(harness, "Action 0"));
        harness.HandlePointerEvent(Down(1, new Point(1, 1)));
        harness.HandlePointerEvent(Move(1, end));
        harness.HandlePointerEvent(Up(1, end));
        Assert.Equal(0, presses);
    }

    private static HitTestResult Path(params ISlideTarget[] targets)
    {
        var result = new HitTestResult();
        foreach (ISlideTarget target in targets)
        {
            result.Add(new HitTestEntry(new RenderMetaData(metaData: target)));
        }

        return result;
    }

    private sealed class SlideTarget(string name, List<string> log, bool enabled = true) : ISlideTarget
    {
        public bool DidEnter(bool fromPointerDown, bool innerEnabled)
        {
            log.Add($"{name}:enter:{fromPointerDown}:{innerEnabled}");
            return enabled && innerEnabled;
        }

        public void DidLeave() => log.Add($"{name}:leave");

        public void DidConfirm() => log.Add($"{name}:confirm");
    }

    private static Widget Action(bool sheet, string label, System.Action callback) => sheet
        ? new CupertinoActionSheetAction(new Text(label), callback)
        : new CupertinoDialogAction(new Text(label), onPressed: callback);

    private static Widget Control(
        bool sheet,
        System.Action callback,
        ScrollController? controller = null,
        int count = 1)
    {
        var actions = Enumerable.Range(0, count).Select(index => Action(sheet, $"Action {index}", callback)).ToArray();
        return sheet
            ? new CupertinoActionSheet(actions: actions, actionScrollController: controller)
            : new CupertinoAlertDialog(actions: actions, actionScrollController: controller);
    }

    private static CupertinoThemeTestHarness Harness(Widget control) => new(Wrap(control));

    private static Widget Wrap(Widget control) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(new MediaQueryData(Size: ViewSize),
            new CupertinoTheme(new CupertinoThemeData(),
                new Localizations(new Locale("en"),
                    [DefaultWidgetsLocalizations.Delegate, DefaultCupertinoLocalizations.Delegate],
                    child: control))));

    private static RenderParagraph Paragraph(CupertinoThemeTestHarness harness, string label) =>
        Descendants<RenderParagraph>(harness.RenderView).First(paragraph => paragraph.PlainText == label);

    private static RenderBox ActionBox(RenderParagraph paragraph)
    {
        RenderObject? node = paragraph;
        while (node is not RenderMetaData { MetaData: CupertinoDialogActionState or CupertinoActionSheetActionState })
        {
            node = node!.Parent;
        }

        return (RenderBox)node;
    }

    private static bool HasPressedFill(CupertinoThemeTestHarness harness, bool sheet) =>
        Descendants<RenderColoredBox>(harness.RenderView)
            .Any(box => box.Color == Color.FromUInt32(sheet ? 0xCAE0E0E0u : 0xFFE1E1E1u));

    private static IEnumerable<T> Descendants<T>(RenderObject root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(Descendants<T>(child)));
        return result;
    }

    private static Point Center(RenderBox box) => box.GetPaintOffsetToRoot()
        + new Vector(box.Size.Width / 2, box.Size.Height / 2);

    private static PointerDownEvent Down(
        int pointer,
        Point position,
        PointerButtons buttons = PointerButtons.Primary) =>
        new(pointer, PointerDeviceKind.Touch, position, buttons, EventTime);

    private static PointerMoveEvent Move(int pointer, Point position) =>
        new(pointer, PointerDeviceKind.Touch, position, PointerButtons.Primary,
            EventTime.AddMilliseconds(20));

    private static PointerUpEvent Up(int pointer, Point position) =>
        new(pointer, PointerDeviceKind.Touch, position, PointerButtons.None, EventTime.AddMilliseconds(40));
}
