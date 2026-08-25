using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Tests;

/// <summary>
/// Dart parity source: `cupertino_ui/test/magnifier_test.dart` plus the defaults declared by
/// `cupertino_ui/lib/src/magnifier.dart`.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoMagnifierTests : IDisposable
{
    private static readonly Rect ReasonableTextField = new(0, 100, 200, 100);
    private static readonly Size ScreenSize = new(400, 300);

    public CupertinoMagnifierTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void CupertinoMagnifier_UsesSourceConstantsAndDefaults()
    {
        var magnifier = new CupertinoMagnifier();

        Assert.Equal(-26.0, CupertinoMagnifier.MagnifierAboveFocalPoint);
        Assert.Equal(new Size(80, 47.5), CupertinoMagnifier.DefaultSize);
        Assert.Equal(CupertinoMagnifier.DefaultSize, magnifier.Size);
        Assert.Equal(Radius.Elliptical(60, 50), magnifier.BorderRadius.TopLeftRadius);
        Assert.True(magnifier.BorderRadius.IsUniform);
        Assert.Equal(default, magnifier.AdditionalFocalPointOffset);
        Assert.Equal(Clip.None, magnifier.ClipBehavior);
        Assert.Equal(Color.FromArgb(255, 0, 124, 255), magnifier.BorderSide.Color);
        Assert.Equal(2.0, magnifier.BorderSide.Width);
        Assert.Null(magnifier.InOutAnimation);
        Assert.Equal(1.0, magnifier.MagnificationScale);

        BoxShadow shadow = Assert.Single(magnifier.Shadows);
        Assert.Equal(Color.FromArgb(25, 0, 0, 0), shadow.Color);
        Assert.Equal(11.0, shadow.BlurRadius);
        Assert.Equal(0.2, shadow.SpreadRadius);
        Assert.Equal(BlurStyle.Outer, shadow.BlurStyle);
        Assert.Equal(default, shadow.Offset);
    }

    [Fact]
    public void CupertinoMagnifier_RejectsNonPositiveMagnificationScale()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoMagnifier(magnificationScale: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoMagnifier(magnificationScale: -1));
    }

    [Fact]
    public void CupertinoMagnifier_PassesMagnificationScaleAndEllipticalRimToRawMagnifier()
    {
        using var harness = new CupertinoThemeTestHarness(Host(new CupertinoMagnifier(magnificationScale: 2)));
        harness.Pump(ScreenSize);

        RawMagnifier raw = Assert.Single(harness.FindWidgets<RawMagnifier>());
        Assert.Equal(2.0, raw.MagnificationScale);
        Assert.Equal(CupertinoMagnifier.DefaultSize, raw.Size);
        Assert.Equal(Clip.None, raw.ClipBehavior);
        Assert.Equal(1.0, raw.Decoration.Opacity);

        var shape = Assert.IsType<RoundedRectangleBorder>(raw.Decoration.Shape);
        BorderRadius radius = shape.BorderRadius.Resolve(TextDirection.Ltr);
        Assert.Equal(60.0, radius.TopLeftRadius.X);
        Assert.Equal(50.0, radius.TopLeftRadius.Y);
        Assert.Equal(2.0, shape.Side.Width);
    }

    [Fact]
    public void CupertinoMagnifier_FocalPointDoesNotScaleWithTheInOutAnimation()
    {
        // `magnifier.dart` discards the result of `focalPointOffset.scale(...)`, so the focal point
        // is the static above-focal-point shift plus the caller's adjustment at every animation value.
        using var animation = new AnimationController(duration: TimeSpan.FromMilliseconds(150));
        animation.SetValue(0.5);
        using var harness = new CupertinoThemeTestHarness(Host(new CupertinoMagnifier(
            inOutAnimation: animation,
            additionalFocalPointOffset: new Point(2, 3))));
        harness.Pump(ScreenSize);

        double expectedY = (CupertinoMagnifier.DefaultSize.Height / 2.0)
                           - CupertinoMagnifier.MagnifierAboveFocalPoint
                           + 3;
        RawMagnifier raw = Assert.Single(harness.FindWidgets<RawMagnifier>());
        Assert.Equal(2.0, raw.FocalPointOffset.X, 3);
        Assert.Equal(expectedY, raw.FocalPointOffset.Y, 3);
        Assert.Equal(0.5, raw.Decoration.Opacity, 3);

        var render = (RenderTransform)harness.FindRenderObject<Plumix.Widgets.Transform>();
        Assert.Equal(
            -CupertinoMagnifier.MagnifierAboveFocalPoint * 0.5,
            render.Transform.GetTranslation().Y,
            3);
    }

    [Fact]
    public void CupertinoMagnifier_DoesNotCrashAtZeroArea()
    {
        using var harness = new CupertinoThemeTestHarness(Host(
            new Center(child: new SizedBox(width: 0, height: 0, child: new CupertinoMagnifier(
                magnificationScale: 2)))));
        harness.Pump(ScreenSize);

        var render = (RenderBox)harness.FindRenderObject<CupertinoMagnifier>();
        Assert.Equal(default, render.Size);
    }

    [Fact]
    public void CupertinoTextMagnifier_UsesSourceDefaults()
    {
        var controller = new MagnifierController();
        var info = new ValueNotifier<MagnifierInfo>(MagnifierInfo.Empty);
        var magnifier = new CupertinoTextMagnifier(controller, info);

        Assert.Same(controller, magnifier.Controller);
        Assert.Same(info, magnifier.MagnifierInfo);
        Assert.Equal(Curves.EaseOut(0.25), magnifier.AnimationCurve(0.25), 9);
        Assert.Equal(10.0, magnifier.DragResistance);
        Assert.Equal(48.0, magnifier.HideBelowThreshold);
        Assert.Equal(10.0, magnifier.HorizontalScreenEdgePadding);
    }

    [Fact]
    public void CupertinoTextMagnifier_BorderColorInheritsFromParentCupertinoTheme()
    {
        var info = new ValueNotifier<MagnifierInfo>(AtCenterOf(ReasonableTextField));
        using var harness = new CupertinoThemeTestHarness(Host(
            new CupertinoTheme(
                new CupertinoThemeData(primaryColor: CupertinoColors.ActiveGreen),
                new Stack(children: [new CupertinoTextMagnifier(new MagnifierController(), info)]))));
        harness.Pump(ScreenSize);

        CupertinoMagnifier magnifier = Assert.Single(harness.FindWidgets<CupertinoMagnifier>());
        Assert.Equal(
            CupertinoColors.ActiveGreen.Color,
            magnifier.BorderSide.Color);
        Assert.Equal(2.0, magnifier.BorderSide.Width);
    }

    [Fact]
    public void CupertinoTextMagnifier_TracksGesturePositionWhenNoRuleIsViolated()
    {
        var info = new ValueNotifier<MagnifierInfo>(AtCenterOf(ReasonableTextField));
        using var harness = new CupertinoThemeTestHarness(Host(
            new Stack(children: [new CupertinoTextMagnifier(new MagnifierController(), info)])));
        harness.Pump(ScreenSize);

        AnimatedPositioned positioned = Assert.Single(harness.FindWidgets<AnimatedPositioned>());
        Assert.Equal(TimeSpan.FromMilliseconds(45), positioned.Duration);
        Assert.Equal(Curves.EaseOut(0.25), positioned.Curve(0.25), 9);
        Assert.Equal(
            ReasonableTextField.Center.X - (CupertinoMagnifier.DefaultSize.Width / 2.0),
            positioned.Left!.Value,
            3);
        Assert.Equal(
            ReasonableTextField.Center.Y
            - (CupertinoMagnifier.DefaultSize.Height - CupertinoMagnifier.MagnifierAboveFocalPoint),
            positioned.Top!.Value,
            3);
    }

    [Fact]
    public void CupertinoTextMagnifier_NeverLeavesTheHorizontalScreenPadding()
    {
        var info = new ValueNotifier<MagnifierInfo>(new MagnifierInfo(
            GlobalGesturePosition: new Point(ScreenSize.Width + 100, ReasonableTextField.Center.Y),
            CaretRect: ReasonableTextField,
            FieldBounds: ReasonableTextField,
            CurrentLineBoundaries: ReasonableTextField));
        using var harness = new CupertinoThemeTestHarness(Host(
            new Stack(children: [new CupertinoTextMagnifier(new MagnifierController(), info)])));
        harness.Pump(ScreenSize);

        AnimatedPositioned positioned = Assert.Single(harness.FindWidgets<AnimatedPositioned>());
        Assert.Equal(
            ScreenSize.Width - 10 - CupertinoMagnifier.DefaultSize.Width,
            positioned.Left!.Value,
            3);
        Assert.True(positioned.Left!.Value < ScreenSize.Width);

        info.Value = info.Value with { GlobalGesturePosition = new Point(-100, ReasonableTextField.Center.Y) };
        harness.Pump(ScreenSize);

        positioned = Assert.Single(harness.FindWidgets<AnimatedPositioned>());
        Assert.Equal(10.0, positioned.Left!.Value, 3);
    }

    [Fact]
    public void CupertinoTextMagnifier_ResistsVerticalDragAndKeepsTheLensOnTheLine()
    {
        double dragPositionBelowTextField = ReasonableTextField.Center.Y + 30;
        var info = new ValueNotifier<MagnifierInfo>(new MagnifierInfo(
            GlobalGesturePosition: new Point(ScreenSize.Width / 2.0, dragPositionBelowTextField),
            CaretRect: ReasonableTextField,
            FieldBounds: ReasonableTextField,
            CurrentLineBoundaries: ReasonableTextField));
        using var harness = new CupertinoThemeTestHarness(Host(
            new Stack(children: [new CupertinoTextMagnifier(new MagnifierController(), info)])));
        harness.Pump(ScreenSize);

        double basicOffsetY = CupertinoMagnifier.DefaultSize.Height - CupertinoMagnifier.MagnifierAboveFocalPoint;
        AnimatedPositioned positioned = Assert.Single(harness.FindWidgets<AnimatedPositioned>());
        double lensCenterLine = positioned.Top!.Value + basicOffsetY;

        Assert.True(lensCenterLine > ReasonableTextField.Center.Y);
        Assert.True(lensCenterLine < dragPositionBelowTextField);

        // The lens still points at the center of the line: 30 units of drag / 10 of resistance = 3.
        var state = harness.FindState<CupertinoTextMagnifier.CupertinoTextMagnifierState>();
        Assert.Equal(-3.0, state.VerticalFocalPointAdjustment, 3);

        // Dragging upward is not resisted at all: the lens never goes above the line center.
        info.Value = info.Value with
        {
            GlobalGesturePosition = new Point(ScreenSize.Width / 2.0, ReasonableTextField.Center.Y - 40),
        };
        harness.Pump(ScreenSize);

        positioned = Assert.Single(harness.FindWidgets<AnimatedPositioned>());
        Assert.Equal(ReasonableTextField.Center.Y - basicOffsetY, positioned.Top!.Value, 3);
        Assert.Equal(0.0, state.VerticalFocalPointAdjustment, 3);
    }

    [Fact]
    public async Task CupertinoTextMagnifier_HidesBelowThresholdAndReshowsWhenTheGestureMovesBackUp()
    {
        var info = new ValueNotifier<MagnifierInfo>(new MagnifierInfo(
            GlobalGesturePosition: new Point(ScreenSize.Width / 2.0, ReasonableTextField.Top),
            CaretRect: ReasonableTextField,
            FieldBounds: ReasonableTextField,
            CurrentLineBoundaries: ReasonableTextField));
        var overlayEntry = new OverlayEntry(_ => new ContextProbe());
        using var harness = new CupertinoThemeTestHarness(Host(new Overlay(initialEntries: [overlayEntry])));
        harness.Pump(ScreenSize);
        var probe = harness.FindState<ContextProbeState>();
        var controller = new MagnifierController();

        Task shown = controller.Show(
            probe.Context,
            _ => new CupertinoTextMagnifier(controller, info));
        harness.Pump(ScreenSize);
        PumpAnimation();
        await shown;

        Assert.True(controller.Shown);
        Assert.NotNull(controller.OverlayEntry);

        // Move the gesture 100 units further down: past the 48 unit hide threshold.
        info.Value = info.Value with
        {
            GlobalGesturePosition = info.Value.GlobalGesturePosition + new Point(0, 100),
        };
        harness.Pump(ScreenSize);
        PumpAnimation();

        Assert.False(controller.Shown);
        Assert.NotNull(controller.OverlayEntry);

        // Return the gesture to one that shows it.
        info.Value = info.Value with
        {
            GlobalGesturePosition = new Point(ScreenSize.Width / 2.0, ReasonableTextField.Top),
        };
        harness.Pump(ScreenSize);
        PumpAnimation();

        Assert.True(controller.Shown);
        Assert.NotNull(controller.OverlayEntry);
        controller.RemoveFromOverlay();
    }

    [Fact]
    public void CupertinoTextMagnifier_DrivesTheLensThroughACurvedInOutAnimation()
    {
        var info = new ValueNotifier<MagnifierInfo>(AtCenterOf(ReasonableTextField));
        var controller = new MagnifierController();
        using var harness = new CupertinoThemeTestHarness(Host(
            new Stack(children: [new CupertinoTextMagnifier(controller, info)])));
        harness.Pump(ScreenSize);

        AnimationController animation = controller.AnimationController!;
        Assert.Equal(TimeSpan.FromMilliseconds(150), animation.Duration);

        animation.SetValue(0.5);
        harness.Pump(ScreenSize);

        CupertinoMagnifier magnifier = Assert.Single(harness.FindWidgets<CupertinoMagnifier>());
        Assert.Equal(Curves.EaseOut(0.5), magnifier.InOutAnimation!.Value, 6);

        RawMagnifier raw = Assert.Single(harness.FindWidgets<RawMagnifier>());
        Assert.Equal(Curves.EaseOut(0.5), raw.Decoration.Opacity, 6);
    }

    [Fact]
    public void CupertinoTextMagnifier_ReleasesTheControllerAnimationOnDispose()
    {
        var info = new ValueNotifier<MagnifierInfo>(AtCenterOf(ReasonableTextField));
        var controller = new MagnifierController();
        using var harness = new CupertinoThemeTestHarness(Host(
            new Stack(children: [new CupertinoTextMagnifier(controller, info)])));
        harness.Pump(ScreenSize);

        Assert.NotNull(controller.AnimationController);

        harness.PumpWidget(Host(new SizedBox()));
        harness.Pump(ScreenSize);

        Assert.Null(controller.AnimationController);

        // The notifier no longer feeds the disposed state.
        info.Value = info.Value with { GlobalGesturePosition = new Point(1, 1) };
        harness.Pump(ScreenSize);
    }

    private static MagnifierInfo AtCenterOf(Rect field)
    {
        return new MagnifierInfo(
            GlobalGesturePosition: field.Center,
            CaretRect: field,
            FieldBounds: field,
            CurrentLineBoundaries: field);
    }

    private static Widget Host(Widget child)
    {
        return new MediaQuery(
            new MediaQueryData(Size: ScreenSize),
            new Directionality(TextDirection.Ltr, child));
    }

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.5));
    }

    private sealed class ContextProbe : StatefulWidget
    {
        public override State CreateState() => new ContextProbeState();
    }

    private sealed class ContextProbeState : State
    {
        public override Widget Build(BuildContext context) => new SizedBox();
    }
}
