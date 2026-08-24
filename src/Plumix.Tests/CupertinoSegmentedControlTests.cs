using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/segmented_control_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoSegmentedControlTests : IDisposable
{
    private static readonly Size ViewSize = new(320.0, 160.0);

    public CupertinoSegmentedControlTests()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugIsWebOverride = null;
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugIsWebOverride = null;
    }

    [Fact]
    public void Constructor_RequiresTwoChildrenAndAValidGroupValue()
    {
        var empty = new Dictionary<string, Widget>();
        var one = new Dictionary<string, Widget> { ["one"] = new Text("One") };
        IReadOnlyDictionary<string, Widget> two = Children();

        Assert.Throws<ArgumentException>(() => new CupertinoSegmentedControl<string>(empty, _ => { }));
        Assert.Throws<ArgumentException>(() => new CupertinoSegmentedControl<string>(one, _ => { }));
        Assert.Throws<ArgumentException>(() => new CupertinoSegmentedControl<string>(
            two,
            _ => { },
            groupValue: "missing"));

        var control = new CupertinoSegmentedControl<string>(two, _ => { });
        Assert.Same(two, control.Children);
        Assert.Null(control.GroupValue);
        Assert.Null(control.Padding);
        Assert.Empty(control.DisabledChildren);
    }

    [Fact]
    public void DefaultsResolveLightDarkSelectedUnselectedPressedAndDisabledColors()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(Control(groupValue: "one")));
        light.Pump(ViewSize);
        RenderCupertinoSegmentedControl lightRender = FindRender(light);
        Assert.Equal(0xFF007AFFu, lightRender.BorderColor.ToUInt32());
        Assert.Equal([0xFF007AFFu, 0xFFFFFFFFu], ColorsOf(lightRender));

        using var dark = new CupertinoThemeTestHarness(Wrap(
            Control(groupValue: "one"),
            brightness: PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        RenderCupertinoSegmentedControl darkRender = FindRender(dark);
        Assert.Equal(0xFF0A84FFu, darkRender.BorderColor.ToUInt32());
        Assert.Equal([0xFF0A84FFu, 0xFFFFFFFFu], ColorsOf(darkRender));

        using var disabled = new CupertinoThemeTestHarness(Wrap(new CupertinoSegmentedControl<string>(
            Children(),
            _ => { },
            groupValue: "one",
            disabledChildren: new HashSet<string> { "one", "two" })));
        disabled.Pump(ViewSize);
        Assert.Equal([0x80007AFFu, 0xFFFFFFFFu], ColorsOf(FindRender(disabled)));
        Assert.All(
            disabled.FindWidgets<DefaultTextStyle>(),
            style => Assert.Equal(0x737A7A7Au, style.Style.Color!.Value.ToUInt32()));
    }

    [Fact]
    public void CustomColorsAndPaddingUseWidgetPrecedence()
    {
        var control = new CupertinoSegmentedControl<string>(
            Children(),
            _ => { },
            groupValue: "one",
            selectedColor: Colors.DarkGreen,
            unselectedColor: Colors.Beige,
            borderColor: Colors.Purple,
            pressedColor: Colors.Orange,
            disabledColor: Colors.Gray,
            disabledTextColor: Colors.Pink,
            padding: EdgeInsetsGeometry.FromLTRB(1.0, 3.0, 5.0, 7.0),
            disabledChildren: new HashSet<string> { "one" });
        using var harness = new CupertinoThemeTestHarness(Wrap(control));

        harness.Pump(ViewSize);

        RenderCupertinoSegmentedControl render = FindRender(harness);
        Assert.Equal(Colors.Purple, render.BorderColor);
        Assert.Equal([Colors.Gray.ToUInt32(), Colors.Beige.ToUInt32()], ColorsOf(render));
        Padding padding = Assert.Single(harness.FindWidgets<Padding>());
        Assert.Equal(new Thickness(1.0, 3.0, 5.0, 7.0), padding.Insets);
        Assert.Single(
            harness.FindWidgets<DefaultTextStyle>(),
            style => style.Style.Color == Colors.Pink);
    }

    [Fact]
    public void TapReportsValueWithoutChangingControlledSelectionAndSelectedRetapIsIgnored()
    {
        string? reported = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(Control(
            groupValue: "one",
            onValueChanged: value => reported = value)));
        harness.Pump(ViewSize);

        Tap(harness.RenderView, new Point(91.0, 14.0), pointer: 10);
        harness.Pump(ViewSize);

        Assert.Equal("two", reported);
        Assert.Equal([0xFF007AFFu, 0xFFFFFFFFu], ColorsOf(FindRender(harness)));

        reported = null;
        Tap(harness.RenderView, new Point(41.0, 14.0), pointer: 11);
        Assert.Null(reported);
    }

    [Fact]
    public void PressedStateIsExclusiveAndSelectedOrDisabledSegmentsDoNotPress()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Control(groupValue: "one")));
        harness.Pump(ViewSize);

        PointerDown(harness.RenderView, new Point(91.0, 14.0), pointer: 20);
        harness.Pump(ViewSize);
        Assert.Equal([0xFF007AFFu, 0x33007AFFu], ColorsOf(FindRender(harness)));

        PointerDown(harness.RenderView, new Point(41.0, 14.0), pointer: 21);
        harness.Pump(ViewSize);
        Assert.Equal([0xFF007AFFu, 0x33007AFFu], ColorsOf(FindRender(harness)));
        PointerCancel(harness.RenderView, new Point(91.0, 14.0), pointer: 20);

        using var disabled = new CupertinoThemeTestHarness(Wrap(new CupertinoSegmentedControl<string>(
            Children(),
            _ => { },
            groupValue: "one",
            disabledChildren: new HashSet<string> { "two" })));
        disabled.Pump(ViewSize);
        PointerDown(disabled.RenderView, new Point(91.0, 14.0), pointer: 22);
        disabled.Pump(ViewSize);
        Assert.Equal([0xFF007AFFu, 0xFFFFFFFFu], ColorsOf(FindRender(disabled)));
    }

    [Fact]
    public void SelectionChangeUsesFlutterLinear165MillisecondTweenAndSurvivesRebuilds()
    {
        IReadOnlyDictionary<string, Widget> children = Children();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "one")));
        harness.Pump(ViewSize);
        harness.PumpWidget(Wrap(new CupertinoSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "two")));
        harness.Pump(ViewSize);

        Assert.Equal([0xFF007AFFu, 0x33007AFFu], ColorsOf(FindRender(harness)));
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.08));
        harness.PumpWidget(Wrap(new CupertinoSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "two")));
        harness.Pump(ViewSize);

        Assert.Equal([0xFF7BBAFFu, 0x95007AFFu], ColorsOf(FindRender(harness)));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.20));
        harness.Pump(ViewSize);
        Assert.Equal([0xFFFFFFFFu, 0xFF007AFFu], ColorsOf(FindRender(harness)));
    }

    [Fact]
    public void LayoutEqualizesWidthHeightCentersChildrenAndReversesPhysicalOrderInRtl()
    {
        IReadOnlyDictionary<string, Widget> children = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 50.0, height: 40.0, child: new Text("One")),
            ["two"] = new SizedBox(width: 100.0, height: 80.0, child: new Text("Two")),
            ["three"] = new SizedBox(width: 70.0, height: 60.0, child: new Text("Three")),
        };
        using var ltr = new CupertinoThemeTestHarness(Wrap(new CupertinoSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "one")));
        ltr.Pump(new Size(640.0, 160.0));
        RenderCupertinoSegmentedControl ltrRender = FindRender(ltr);

        Assert.Equal(new Size(300.0, 80.0), ltrRender.Size);
        IReadOnlyList<RenderBox> ltrChildren = DirectChildren(ltrRender);
        Assert.All(ltrChildren, child => Assert.Equal(new Size(100.0, 80.0), child.Size));
        Assert.Equal(0.0, ParentData(ltrChildren[0]).offset.X);
        Assert.Equal(200.0, ParentData(ltrChildren[2]).offset.X);
        Assert.Equal(Radius.Circular(3.0), ParentData(ltrChildren[0]).SurroundingRect.TopLeft);
        Assert.Equal(Radius.Circular(3.0), ParentData(ltrChildren[2]).SurroundingRect.TopRight);

        using var rtl = new CupertinoThemeTestHarness(Wrap(new CupertinoSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "one"),
            direction: TextDirection.Rtl));
        rtl.Pump(new Size(640.0, 160.0));
        IReadOnlyList<RenderBox> rtlChildren = DirectChildren(FindRender(rtl));
        Assert.Equal(200.0, ParentData(rtlChildren[0]).offset.X);
        Assert.Equal(0.0, ParentData(rtlChildren[2]).offset.X);
    }

    [Fact]
    public void SemanticsExposeRadioGroupButtonsSelectionFocusAndTap()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Control(groupValue: "one")));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode group = Assert.Single(FindSemantics(root, node => node.Role == SemanticsRole.RadioGroup));
        IReadOnlyList<SemanticsNode> buttons = FindSemantics(
            group,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.Equal(2, buttons.Count);
        Assert.All(buttons, button =>
        {
            Assert.True(button.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup));
            Assert.True(button.Flags.HasFlag(SemanticsFlags.HasSelectedState));
            Assert.True(button.Actions.HasFlag(SemanticsActions.Tap));
        });
        Assert.Single(buttons, button => button.Flags.HasFlag(SemanticsFlags.IsSelected));
    }

    [Fact]
    public void ArrowKeysWrapAndSkipDisabledSegments()
    {
        string? reported = null;
        IReadOnlyDictionary<string, Widget> children = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 50.0, height: 28.0, child: new Text("One")),
            ["two"] = new SizedBox(width: 50.0, height: 28.0, child: new Text("Two")),
            ["three"] = new SizedBox(width: 50.0, height: 28.0, child: new Text("Three")),
        };
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSegmentedControl<string>(
            children,
            value => reported = value,
            groupValue: "one",
            disabledChildren: new HashSet<string> { "two" })));
        harness.Pump(ViewSize);
        Tap(harness.RenderView, new Point(41.0, 14.0), pointer: 30);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        Assert.Equal("three", reported);
        reported = null;
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        Assert.Equal("one", reported);
        reported = null;
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Equal("three", reported);
    }

    [Fact]
    public void WebUsesClickCursorAndZeroAreaDoesNotCrash()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        using var web = new CupertinoThemeTestHarness(Wrap(Control(groupValue: "one")));
        web.Pump(ViewSize);
        Assert.All(web.FindWidgets<MouseRegion>(), region => Assert.Equal(SystemMouseCursors.Click, region.Cursor));

        using var zero = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: Control(groupValue: "one"))));
        zero.Pump(default);
        Assert.Equal(0.0, FindRender(zero).Size.Width);
    }

    [Fact]
    public void RSuperellipsePrimitivePreservesBoundsShiftRadiiAndExactPath()
    {
        var shape = RSuperellipse.FromRectAndCorners(
            new Rect(10.0, 20.0, 100.0, 40.0),
            topLeft: Radius.Circular(12.0),
            bottomRight: Radius.Elliptical(8.0, 6.0));

        Assert.Equal(shape.Rect, shape.OuterRect);
        Assert.Equal(new Rect(15.0, 27.0, 100.0, 40.0), shape.Shift(new Point(5.0, 7.0)).OuterRect);
        Assert.Equal(shape.OuterRect, shape.ToPath().GetBounds());
        Assert.True(shape.Contains(new Point(60.0, 40.0)));
        Assert.False(shape.Contains(new Point(10.0, 20.0)));
    }

    private static CupertinoSegmentedControl<string> Control(
        string? groupValue,
        Action<string>? onValueChanged = null) => new(
        Children(),
        onValueChanged ?? (_ => { }),
        groupValue: groupValue);

    private static IReadOnlyDictionary<string, Widget> Children() => new Dictionary<string, Widget>
    {
        ["one"] = new SizedBox(width: 50.0, height: 28.0, child: new Text("One")),
        ["two"] = new SizedBox(width: 50.0, height: 28.0, child: new Text("Two")),
    };

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        TextDirection direction = TextDirection.Ltr)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: brightness),
            child: new Directionality(
                direction,
                new CupertinoTheme(
                    new CupertinoThemeData(brightness: brightness),
                    new Align(alignment: Alignment.TopLeft, child: child))));
    }

    private static RenderCupertinoSegmentedControl FindRender(CupertinoThemeTestHarness harness)
    {
        return Assert.Single(FindAll<RenderCupertinoSegmentedControl>(harness.RenderView));
    }

    private static uint[] ColorsOf(RenderCupertinoSegmentedControl render) =>
        render.BackgroundColors.Select(color => color.ToUInt32()).ToArray();

    private static IReadOnlyList<RenderBox> DirectChildren(RenderCupertinoSegmentedControl render)
    {
        var result = new List<RenderBox>();
        render.VisitChildren(child => result.Add((RenderBox)child));
        return result;
    }

    private static CupertinoSegmentedControlParentData ParentData(RenderBox child) =>
        Assert.IsType<CupertinoSegmentedControlParentData>(child.parentData);

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }
        if (root is T typed)
        {
            result.Add(typed);
        }
        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }

    private static IReadOnlyList<SemanticsNode> FindSemantics(
        SemanticsNode root,
        Func<SemanticsNode, bool> predicate)
    {
        var result = new List<SemanticsNode>();
        if (predicate(root))
        {
            result.Add(root);
        }
        foreach (SemanticsNode child in root.Children)
        {
            result.AddRange(FindSemantics(child, predicate));
        }
        return result;
    }

    private static void Tap(RenderView view, Point position, int pointer)
    {
        PointerDown(view, position, pointer);
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                DateTime.UtcNow.AddMilliseconds(16.0)));
    }

    private static void PointerDown(RenderView view, Point position, int pointer)
    {
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                DateTime.UtcNow));
    }

    private static void PointerCancel(RenderView view, Point position, int pointer)
    {
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerCancelEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                DateTime.UtcNow));
    }
}
