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

// Dart parity source: cupertino_ui/test/sliding_segmented_control_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoSlidingSegmentedControlTests : IDisposable
{
    private static readonly Size ViewSize = new(800.0, 500.0);

    public CupertinoSlidingSegmentedControlTests()
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
    public void ConstructorRequiresTwoChildrenAndValidGroupValueAndExposesExactDefaults()
    {
        var empty = new Dictionary<string, Widget>();
        var one = new Dictionary<string, Widget> { ["one"] = new Text("One") };
        IReadOnlyDictionary<string, Widget> children = Children();

        Assert.Throws<ArgumentException>(() => new CupertinoSlidingSegmentedControl<string>(empty, _ => { }));
        Assert.Throws<ArgumentException>(() => new CupertinoSlidingSegmentedControl<string>(one, _ => { }));
        Assert.Throws<ArgumentException>(() => new CupertinoSlidingSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "missing"));

        var control = new CupertinoSlidingSegmentedControl<string>(children, _ => { });
        Assert.Same(children, control.Children);
        Assert.Null(control.GroupValue);
        Assert.Empty(control.DisabledChildren);
        Assert.Equal(EdgeInsetsGeometry.Symmetric(horizontal: 3.0, vertical: 2.0), control.Padding);
        Assert.Same(CupertinoColors.TertiarySystemFill, control.BackgroundColor);
        Assert.False(control.ProportionalWidth);
        Assert.False(control.IsMomentary);
        Assert.Equal(0xFFFFFFFFu, control.ThumbColor.Color.ToUInt32());
        Assert.Equal(0xFF636366u, control.ThumbColor.DarkColor.ToUInt32());

        var disabled = new HashSet<string> { "two" };
        CupertinoDynamicColor thumb = Color.FromUInt32(0xFF123456);
        CupertinoDynamicColor background = Color.FromUInt32(0xFF654321);
        var custom = new CupertinoSlidingSegmentedControl<string>(
            children,
            _ => { },
            disabledChildren: disabled,
            thumbColor: thumb,
            padding: EdgeInsetsGeometry.All(7.0),
            backgroundColor: background,
            proportionalWidth: true,
            isMomentary: true);
        Assert.Same(disabled, custom.DisabledChildren);
        Assert.Same(thumb, custom.ThumbColor);
        Assert.Same(background, custom.BackgroundColor);
        Assert.Equal(EdgeInsetsGeometry.All(7.0), custom.Padding);
        Assert.True(custom.ProportionalWidth);
        Assert.True(custom.IsMomentary);
    }

    [Fact]
    public void DynamicDefaultsAndCustomColorsResolveThroughAmbientBrightness()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(Control(groupValue: "one")));
        light.Pump(ViewSize);
        RenderCupertinoSlidingSegmentedControl<string> lightRender = FindRender(light);
        Assert.Equal(0xFFFFFFFFu, lightRender.ThumbColor.ToUInt32());
        Assert.Equal(0x1E767680u, BackgroundColor(light).ToUInt32());

        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSlidingSegmentedControl<string>(
                Children(),
                _ => { },
                groupValue: "one",
                thumbColor: CupertinoColors.SystemGreen,
                backgroundColor: CupertinoColors.SystemRed),
            brightness: PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Equal(CupertinoColors.SystemGreen.DarkColor, FindRender(dark).ThumbColor);
        Assert.Equal(CupertinoColors.SystemRed.DarkColor, BackgroundColor(dark));
    }

    [Fact]
    public void EqualAndProportionalLayoutMatchFlutterGeometryAndRelayoutOnUpdate()
    {
        IReadOnlyDictionary<string, Widget> children = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 50.0, height: 100.0),
            ["two"] = new SizedBox(width: 100.0, height: 400.0),
            ["three"] = new SizedBox(width: 200.0, height: 200.0),
        };
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlidingSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "one")));
        harness.Pump(ViewSize);

        RenderCupertinoSlidingSegmentedControl<string> equal = FindRender(harness);
        Assert.Equal(new Size(662.0, 400.0), equal.Size);
        Assert.Equal([220.0, 220.0, 220.0], SegmentChildren(equal).Select(child => child.Size.Width));

        harness.PumpWidget(Wrap(new CupertinoSlidingSegmentedControl<string>(
            children,
            _ => { },
            groupValue: "one",
            proportionalWidth: true)));
        harness.Pump(ViewSize);

        RenderCupertinoSlidingSegmentedControl<string> proportional = FindRender(harness);
        Assert.Equal([70.0, 120.0, 220.0], SegmentChildren(proportional).Select(child => child.Size.Width));
        Assert.Equal(412.0, proportional.Size.Width);
        Assert.Equal(400.0, proportional.Size.Height);
    }

    [Fact]
    public void ProportionalWidthsScaleUniformlyToMinAndMaxConstraintsAndUpdateWithChildren()
    {
        IReadOnlyDictionary<string, Widget> children = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 50.0, height: 28.0),
            ["two"] = new SizedBox(width: 100.0, height: 28.0),
            ["three"] = new SizedBox(width: 200.0, height: 28.0),
        };
        Widget constrained = new ConstrainedBox(
            new BoxConstraints(MaxWidth: 206.0),
            new CupertinoSlidingSegmentedControl<string>(
                children,
                _ => { },
                proportionalWidth: true));
        using var harness = new CupertinoThemeTestHarness(Wrap(constrained));
        harness.Pump(ViewSize);

        IReadOnlyList<double> widths = SegmentChildren(FindRender(harness)).Select(child => child.Size.Width).ToList();
        double scale = 198.0 / 410.0;
        Assert.Equal(70.0 * scale, widths[0], 6);
        Assert.Equal(120.0 * scale, widths[1], 6);
        Assert.Equal(220.0 * scale, widths[2], 6);

        IReadOnlyDictionary<string, Widget> updated = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 0.0, height: 28.0),
            ["two"] = new SizedBox(width: 220.0, height: 28.0),
            ["three"] = new SizedBox(width: 170.0, height: 28.0),
        };
        harness.PumpWidget(Wrap(new CupertinoSlidingSegmentedControl<string>(
            updated,
            _ => { },
            proportionalWidth: true)));
        harness.Pump(ViewSize);
        Assert.Equal(
            [20.0, 240.0, 190.0],
            SegmentChildren(FindRender(harness)).Select(child => child.Size.Width));

        using var minimum = new CupertinoThemeTestHarness(Wrap(new ConstrainedBox(
            new BoxConstraints(MinWidth: 206.0),
            new CupertinoSlidingSegmentedControl<string>(
                new Dictionary<string, Widget>
                {
                    ["one"] = new SizedBox(width: 20.0),
                    ["two"] = new SizedBox(width: 30.0),
                    ["three"] = new SizedBox(width: 50.0),
                },
                _ => { },
                proportionalWidth: true))));
        minimum.Pump(ViewSize);
        Assert.Equal(198.0, SegmentChildren(FindRender(minimum)).Sum(child => child.Size.Width), 6);
    }

    [Fact]
    public void DryLayoutIsPureSizeIsSelectionIndependentAndRtlReversesSelection()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Control(groupValue: null)));
        harness.Pump(ViewSize);
        RenderCupertinoSlidingSegmentedControl<string> render = FindRender(harness);
        Size unselectedSize = render.Size;
        Size drySize = render.GetDryLayout(new BoxConstraints(
            MaxWidth: double.PositiveInfinity,
            MaxHeight: double.PositiveInfinity));
        Assert.True(drySize.Width > 10.0, $"Dry size was {drySize}; laid-out size was {render.Size}.");
        Assert.Null(render.HighlightedIndex);

        harness.PumpWidget(Wrap(Control(groupValue: "two")));
        harness.Pump(ViewSize);
        Assert.Equal(unselectedSize, FindRender(harness).Size);

        string? rtlReport = null;
        using var rtl = new CupertinoThemeTestHarness(Wrap(
            Control(groupValue: "one", onValueChanged: value => rtlReport = value),
            direction: TextDirection.Rtl));
        rtl.Pump(ViewSize);
        RenderCupertinoSlidingSegmentedControl<string> rtlRender = FindRender(rtl);
        Assert.Equal(1, rtlRender.HighlightedIndex);
        Assert.True(
            ParentData(SegmentChildren(rtlRender)[0]).offset.X
            < ParentData(SegmentChildren(rtlRender)[1]).offset.X);
        Tap(rtl.RenderView, new Point(30.0, 14.0), pointer: 5);
        Assert.Equal("two", rtlReport);
    }

    [Fact]
    public void TapIsControlledIgnoresSelectedAndDisabledSegmentsAndUsesAllocatedHitArea()
    {
        var reports = new List<string?>();
        IReadOnlyDictionary<string, Widget> children = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 0.0, height: 28.0),
            ["two"] = new SizedBox(width: 50.0, height: 28.0),
            ["three"] = new SizedBox(width: 0.0, height: 28.0),
        };
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlidingSegmentedControl<string>(
            children,
            reports.Add,
            groupValue: "one",
            disabledChildren: new HashSet<string> { "two" },
            proportionalWidth: true)));
        harness.Pump(ViewSize);

        Tap(harness.RenderView, new Point(13.0, 14.0), pointer: 10);
        Tap(harness.RenderView, new Point(45.0, 14.0), pointer: 11);
        Tap(harness.RenderView, new Point(101.0, 14.0), pointer: 12);

        Assert.Equal(["three"], reports);
        Assert.Equal(0, FindRender(harness).HighlightedIndex);
    }

    [Fact]
    public void DisabledSegmentMayRemainProgrammaticallySelectedAndAllDisabledSuppressInput()
    {
        int reports = 0;
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlidingSegmentedControl<string>(
            Children(),
            _ => reports++,
            groupValue: "two",
            disabledChildren: new HashSet<string> { "one", "two" })));
        harness.Pump(ViewSize);

        Assert.Equal(1, FindRender(harness).HighlightedIndex);
        Assert.All(
            harness.FindWidgets<AnimatedDefaultTextStyle>(),
            style => Assert.Equal(Color.FromArgb(115, 122, 122, 122), style.Style.Color));
        Tap(harness.RenderView, new Point(30.0, 14.0), pointer: 20);
        Tap(harness.RenderView, new Point(110.0, 14.0), pointer: 21);
        Assert.Equal(0, reports);
    }

    [Fact]
    public void PressOpacityAndHighlightedWeightFollowPersistentInteractionState()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Control(groupValue: "one")));
        harness.Pump(ViewSize);
        Assert.Contains(
            harness.FindWidgets<AnimatedDefaultTextStyle>(),
            style => style.Style.FontWeight == FontWeight.DemiBold);

        PointerDown(harness.RenderView, new Point(110.0, 14.0), pointer: 30);
        harness.Pump(ViewSize);
        AnimatedOpacity fading = Assert.Single(
            harness.FindWidgets<AnimatedOpacity>(),
            opacity => opacity.Opacity == 0.2);
        Assert.Equal(TimeSpan.FromMilliseconds(470.0), fading.Duration);

        PointerCancel(harness.RenderView, new Point(110.0, 14.0), pointer: 30);
        harness.Pump(ViewSize);
        Assert.DoesNotContain(harness.FindWidgets<AnimatedOpacity>(), opacity => opacity.Opacity == 0.2);

        PointerDown(harness.RenderView, new Point(30.0, 14.0), pointer: 31);
        harness.Pump(ViewSize);
        Assert.DoesNotContain(harness.FindWidgets<AnimatedOpacity>(), opacity => opacity.Opacity == 0.2);
        PointerCancel(harness.RenderView, new Point(30.0, 14.0), pointer: 31);
    }

    [Fact]
    public void SelectedThumbDragMovesHighlightCallsBackOnReleaseAndAnchorsEdgeScale()
    {
        string? reported = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(Control(
            groupValue: "one",
            onValueChanged: value => reported = value)));
        harness.Pump(ViewSize);
        RenderCupertinoSlidingSegmentedControl<string> render = FindRender(harness);
        Rect initial = Assert.IsType<Rect>(render.CurrentThumbRect);

        PointerDown(harness.RenderView, new Point(30.0, 14.0), pointer: 40);
        // The move that carries the drag past the touch slop only wins the arena and reports
        // `onStart`; with `DragStartBehavior.Start` the first `onUpdate` arrives on the next move.
        Move(harness.RenderView, new Point(110.0, 14.0), pointer: 40);
        harness.Pump(ViewSize);
        Assert.Equal(0, render.HighlightedIndex);
        Move(harness.RenderView, new Point(112.0, 14.0), pointer: 40);
        harness.Pump(ViewSize);
        Assert.Equal(1, render.HighlightedIndex);
        Assert.Null(reported);
        Move(harness.RenderView, new Point(110.0, 200.0), pointer: 40);
        harness.Pump(ViewSize);
        Assert.Equal("two", render.ControlState.Pressed);

        PumpAnimation(harness, 500.0);
        Rect shrunken = Assert.IsType<Rect>(render.CurrentThumbRect);
        Assert.True(
            shrunken.Center.X > initial.Center.X,
            $"Thumb stayed at {shrunken} from {initial}; animation value was "
            + $"{render.ControlState.ThumbController.Value}.");

        PointerUp(harness.RenderView, new Point(110.0, 14.0), pointer: 40);
        harness.Pump(ViewSize);
        Assert.Equal("two", reported);
        PumpAnimation(harness, 500.0);
        Assert.Equal(1.0, render.ThumbScale, 2);
    }

    [Fact]
    public void ExternalSelectionRetargetsAnInFlightThumbAndKeepsItInBoundsAfterChildChange()
    {
        IReadOnlyDictionary<string, Widget> three = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 60.0, height: 80.0),
            ["two"] = new SizedBox(width: 100.0, height: 80.0),
            ["three"] = new SizedBox(width: 140.0, height: 80.0),
        };
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlidingSegmentedControl<string>(
            three,
            _ => { },
            groupValue: "one",
            proportionalWidth: true)));
        harness.Pump(ViewSize);

        harness.PumpWidget(Wrap(new CupertinoSlidingSegmentedControl<string>(
            three,
            _ => { },
            groupValue: "two",
            proportionalWidth: true)));
        PumpAnimation(harness, 40.0);
        Rect inFlight = Assert.IsType<Rect>(FindRender(harness).CurrentThumbRect);

        harness.PumpWidget(Wrap(new CupertinoSlidingSegmentedControl<string>(
            three,
            _ => { },
            groupValue: "three",
            proportionalWidth: true)));
        PumpAnimation(harness, 40.0);
        Rect retargeted = Assert.IsType<Rect>(FindRender(harness).CurrentThumbRect);
        Assert.True(retargeted.Left >= inFlight.Left);

        IReadOnlyDictionary<string, Widget> smaller = new Dictionary<string, Widget>
        {
            ["one"] = new SizedBox(width: 20.0, height: 28.0),
            ["three"] = new SizedBox(width: 30.0, height: 28.0),
        };
        harness.PumpWidget(Wrap(new CupertinoSlidingSegmentedControl<string>(
            smaller,
            _ => { },
            groupValue: "three",
            proportionalWidth: true)));
        PumpAnimation(harness, 1000.0);
        RenderCupertinoSlidingSegmentedControl<string> render = FindRender(harness);
        Rect settled = Assert.IsType<Rect>(render.CurrentThumbRect);
        Assert.True(settled.Left >= -1.0);
        Assert.True(
            settled.Right <= render.Size.Width + 1.0,
            $"Thumb {settled} exceeded render size {render.Size}.");
        Assert.Equal(28.0, settled.Height);
    }

    [Fact]
    public void MomentaryModeSuppressesThumbAndUsesSpringScaleUpThenReturnsToOne()
    {
        string? reported = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlidingSegmentedControl<string>(
            Children(),
            value => reported = value,
            groupValue: "one",
            isMomentary: true)));
        harness.Pump(ViewSize);
        Assert.Null(FindRender(harness).HighlightedIndex);
        Assert.Null(FindRender(harness).CurrentThumbRect);

        PointerDown(harness.RenderView, new Point(30.0, 14.0), pointer: 50);
        harness.Pump(ViewSize);
        PumpAnimation(harness, 80.0);
        Assert.Contains(harness.FindWidgets<ScaleTransition>(), scale => scale.Scale.Value > 1.0);
        PointerUp(harness.RenderView, new Point(30.0, 14.0), pointer: 50);
        PumpAnimation(harness, 500.0);
        Assert.All(harness.FindWidgets<ScaleTransition>(), scale => Assert.Equal(1.0, scale.Scale.Value, 2));
        Tap(harness.RenderView, new Point(110.0, 14.0), pointer: 51);
        Assert.Equal("two", reported);
    }

    [Fact]
    public void SemanticsExposeRadioGroupButtonsAndKeyboardWrapsWhileSkippingDisabled()
    {
        string? reported = null;
        IReadOnlyDictionary<string, Widget> children = new Dictionary<string, Widget>
        {
            ["one"] = new Text("One"),
            ["two"] = new Text("Two"),
            ["three"] = new Text("Three"),
        };
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSlidingSegmentedControl<string>(
            children,
            value => reported = value,
            groupValue: "one",
            disabledChildren: new HashSet<string> { "two" })));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode group = Assert.Single(FindSemantics(root, node => node.Role == SemanticsRole.RadioGroup));
        IReadOnlyList<SemanticsNode> buttons = FindSemantics(
            group,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.Equal(3, buttons.Count);
        Assert.Single(buttons, button => button.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.False(buttons[1].Actions.HasFlag(SemanticsActions.Tap));

        Tap(harness.RenderView, new Point(30.0, 14.0), pointer: 60);
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        Assert.Equal("three", reported);
        harness.PumpWidget(Wrap(new CupertinoSlidingSegmentedControl<string>(
            children,
            value => reported = value,
            groupValue: "three",
            disabledChildren: new HashSet<string> { "two" })));
        harness.Pump(ViewSize);
        reported = null;
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Equal("one", reported);
    }

    [Fact]
    public void WebSegmentsUseClickCursorAndRemovingControlDuringGestureIsSafe()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        using var harness = new CupertinoThemeTestHarness(Wrap(Control(groupValue: "one")));
        harness.Pump(ViewSize);
        Assert.All(harness.FindWidgets<MouseRegion>(), region => Assert.Equal(SystemMouseCursors.Click, region.Cursor));

        PointerDown(harness.RenderView, new Point(30.0, 14.0), pointer: 70);
        harness.PumpWidget(Wrap(new SizedBox(width: 10.0, height: 10.0)));
        harness.Pump(ViewSize);
        Move(harness.RenderView, new Point(110.0, 14.0), pointer: 70);
        PointerUp(harness.RenderView, new Point(110.0, 14.0), pointer: 70);
    }

    private static CupertinoSlidingSegmentedControl<string> Control(
        string? groupValue,
        Action<string?>? onValueChanged = null) => new(
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

    private static RenderCupertinoSlidingSegmentedControl<string> FindRender(
        CupertinoThemeTestHarness harness)
    {
        return Assert.Single(FindAll<RenderCupertinoSlidingSegmentedControl<string>>(harness.RenderView));
    }

    private static IReadOnlyList<RenderBox> SegmentChildren(RenderCupertinoSlidingSegmentedControl<string> render)
    {
        var all = new List<RenderBox>();
        render.VisitChildren(child => all.Add((RenderBox)child));
        return all.Where((_, index) => index % 2 == 0).ToList();
    }

    private static CupertinoSlidingSegmentedControlParentData ParentData(RenderBox child) =>
        Assert.IsType<CupertinoSlidingSegmentedControlParentData>(child.parentData);

    private static Color BackgroundColor(CupertinoThemeTestHarness harness)
    {
        Container container = Assert.Single(
            harness.FindWidgets<Container>(),
            candidate => candidate.Decoration is ShapeDecoration
            {
                Shape: RoundedSuperellipseBorder,
            });
        return Assert.IsType<ShapeDecoration>(container.Decoration).Color!.Value;
    }

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

    private static void PumpAnimation(CupertinoThemeTestHarness harness, double milliseconds)
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + (milliseconds / 1000.0)));
        harness.Pump(ViewSize);
    }

    private static void Tap(RenderView view, Point position, int pointer)
    {
        PointerDown(view, position, pointer);
        PointerUp(view, position, pointer);
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

    private static void Move(RenderView view, Point position, int pointer)
    {
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerMoveEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                true,
                DateTime.UtcNow.AddMilliseconds(16.0)));
    }

    private static void PointerUp(RenderView view, Point position, int pointer)
    {
        GestureBinding.Instance.HandlePointerEvent(
            view,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                DateTime.UtcNow.AddMilliseconds(32.0)));
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
                DateTime.UtcNow.AddMilliseconds(32.0)));
    }
}
