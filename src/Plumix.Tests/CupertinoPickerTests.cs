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

// Dart parity source: cupertino_ui/test/picker_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoPickerTests : IDisposable
{
    private static readonly Size ViewSize = new(300.0, 300.0);

    public CupertinoPickerTests()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    [Fact]
    public void Constructors_ValidateAndUseFlutterDefaults()
    {
        IReadOnlyList<Widget> children = [new Text("0"), new Text("1")];
        Action<int> callback = _ => { };
        var picker = new CupertinoPicker(50.0, callback, children);

        Assert.Equal(CupertinoPicker.DefaultDiameterRatio, picker.DiameterRatio);
        Assert.Null(picker.BackgroundColor);
        Assert.Equal(0.0, picker.OffAxisFraction);
        Assert.False(picker.UseMagnifier);
        Assert.Equal(1.0, picker.Magnification);
        Assert.Null(picker.ScrollController);
        Assert.Equal(50.0, picker.ItemExtent);
        Assert.Equal(CupertinoPicker.DefaultSqueeze, picker.Squeeze);
        Assert.Equal(ChangeReportingBehavior.OnScrollUpdate, picker.ChangeReportingBehavior);
        Assert.Same(callback, picker.OnSelectedItemChanged);
        Assert.IsType<ListWheelChildListDelegate>(picker.ChildDelegate);
        Assert.IsType<CupertinoPickerDefaultSelectionOverlay>(picker.SelectionOverlay);

        var looping = new CupertinoPicker(50.0, callback, children, looping: true);
        Assert.IsType<ListWheelChildLoopingListDelegate>(looping.ChildDelegate);

        var builder = CupertinoPicker.Builder(
            50.0,
            callback,
            (_, index) => new Text(index.ToString()),
            childCount: 2);
        var builderDelegate = Assert.IsType<ListWheelChildBuilderDelegate>(builder.ChildDelegate);
        Assert.Equal(2, builderDelegate.ChildCount);
        Assert.IsType<CupertinoPickerDefaultSelectionOverlay>(builder.SelectionOverlay);

        var withoutOverlay = CupertinoPicker.Builder(
            50.0,
            callback,
            (_, index) => new Text(index.ToString()),
            selectionOverlay: null,
            childCount: 2);
        Assert.Null(withoutOverlay.SelectionOverlay);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoPicker(0.0, callback, children));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoPicker(
            50.0,
            callback,
            children,
            diameterRatio: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoPicker(
            50.0,
            callback,
            children,
            magnification: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoPicker(
            50.0,
            callback,
            children,
            squeeze: 0.0));
    }

    [Fact]
    public void Build_ComposesFlutterWheelDefaultsAndOverlayGeometry()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            itemExtent: 50.0,
            onSelectedItemChanged: _ => { },
            children: [new Text("0"), new Text("1"), new Text("2")])));

        harness.Pump(ViewSize);

        ListWheelScrollView wheel = Assert.Single(harness.FindWidgets<ListWheelScrollView>());
        Assert.IsType<FixedExtentScrollController>(wheel.Controller);
        Assert.IsType<FixedExtentScrollPhysics>(wheel.Physics);
        Assert.Equal(CupertinoPicker.DefaultDiameterRatio, wheel.DiameterRatio);
        Assert.Equal(RenderListWheelViewport.DefaultPerspective, wheel.Perspective);
        Assert.Equal(CupertinoPicker.OverAndUnderCenterOpacity, wheel.OverAndUnderCenterOpacity);
        Assert.Equal(CupertinoPicker.DefaultSqueeze, wheel.Squeeze);
        Assert.Equal(DragStartBehavior.Down, wheel.DragStartBehavior);
        Assert.IsType<CupertinoPickerListWheelChildDelegateWrapper>(wheel.ChildDelegate);

        DefaultTextStyle defaultTextStyle = Assert.Single(harness.FindWidgets<DefaultTextStyle>());
        Assert.Equal(21.0, defaultTextStyle.Style.FontSize);
        Assert.Equal(-0.6, defaultTextStyle.Style.LetterSpacing);
        Assert.Equal(CupertinoColors.Label.Color, defaultTextStyle.Style.Color);

        var overlay = Assert.Single(harness.FindWidgets<CupertinoPickerDefaultSelectionOverlay>());
        Assert.True(overlay.CapStartEdge);
        Assert.True(overlay.CapEndEdge);
        Container overlayContainer = Assert.Single(harness.FindWidgets<Container>());
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 9.0, end: 9.0),
            overlayContainer.Margin);
        var shape = Assert.IsType<ShapeDecoration>(overlayContainer.Decoration);
        var rounded = Assert.IsType<RoundedSuperellipseBorder>(shape.Shape);
        Assert.Equal(
            BorderRadius.Circular(8.0),
            rounded.BorderRadius.Resolve(TextDirection.Ltr));
    }

    [Fact]
    public void Build_ResolvesThemeTextAndDynamicBackgroundInLightAndDarkModes()
    {
        CupertinoDynamicColor background = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFF123456),
            Color.FromUInt32(0xFF654321));
        var picker = new CupertinoPicker(
            50.0,
            _ => { },
            [new Text("0"), new Text("1")],
            backgroundColor: background);

        using var light = new CupertinoThemeTestHarness(Wrap(picker, PlatformBrightness.Light));
        light.Pump(ViewSize);
        Assert.Contains(
            light.FindWidgets<DecoratedBox>(),
            box => box.Decoration is BoxDecoration { Color: { } color }
                && color == Color.FromUInt32(0xFF123456));
        Assert.Equal(
            CupertinoColors.Label.Color,
            Assert.Single(light.FindWidgets<DefaultTextStyle>()).Style.Color);

        using var dark = new CupertinoThemeTestHarness(Wrap(picker, PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Contains(
            dark.FindWidgets<DecoratedBox>(),
            box => box.Decoration is BoxDecoration { Color: { } color }
                && color == Color.FromUInt32(0xFF654321));
        Assert.Equal(
            CupertinoColors.Label.DarkColor,
            Assert.Single(dark.FindWidgets<DefaultTextStyle>()).Style.Color);
    }

    [Fact]
    public void SelectionOverlay_CanBeRemovedAndMagnificationControlsItsHeight()
    {
        using var removed = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            20.0,
            _ => { },
            [new Text("0"), new Text("1")],
            selectionOverlay: null)));
        removed.Pump(ViewSize);
        Assert.True(removed.FindWidgets<CupertinoPickerDefaultSelectionOverlay>().Count == 0);

        var customOverlay = new ColoredBox(Color.FromUInt32(0xFF123456));
        using var magnified = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            20.0,
            _ => { },
            [new Text("0"), new Text("1")],
            selectionOverlay: customOverlay,
            magnification: 1.5)));
        magnified.Pump(ViewSize);
        ConstrainedBox[] matchingConstraints = magnified.FindWidgets<ConstrainedBox>()
            .Where(box => box.Constraints.MinHeight == 30.0 && box.Constraints.MaxHeight == 30.0)
            .ToArray();
        Assert.True(matchingConstraints.Length == 1);
        ConstrainedBox constraint = matchingConstraints[0];
        Assert.Equal(30.0, constraint.Constraints.MinHeight);
        Assert.Equal(30.0, constraint.Constraints.MaxHeight);
    }

    [Fact]
    public void SelectionOverlay_UsesDirectionalMarginsAndCapsAndHandlesZeroArea()
    {
        var overlay = new CupertinoPickerDefaultSelectionOverlay(
            capStartEdge: false,
            background: Color.FromUInt32(0x12345678));
        using var harness = new CupertinoThemeTestHarness(Wrap(overlay));
        harness.Pump(ViewSize);

        Container container = Assert.Single(harness.FindWidgets<Container>());
        Assert.Equal(EdgeInsetsGeometry.DirectionalOnly(end: 9.0), container.Margin);
        var decoration = Assert.IsType<ShapeDecoration>(container.Decoration);
        Assert.Equal(Color.FromUInt32(0x12345678), decoration.Color);
        var border = Assert.IsType<RoundedSuperellipseBorder>(decoration.Shape);
        Assert.Equal(
            BorderRadius.Only(topRight: 8.0, bottomRight: 8.0),
            border.BorderRadius.Resolve(TextDirection.Ltr));

        using var zero = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoPickerDefaultSelectionOverlay())));
        zero.Pump(new Size());
        Assert.Equal(new Size(), FindRender<RenderDecoratedBox>(zero.RenderView).Size);
    }

    [Fact]
    public void Builder_RebuildExpandsItsChildCountAndZeroAreaDoesNotCrash()
    {
        static CupertinoPicker Build(int count) => CupertinoPicker.Builder(
            50.0,
            _ => { },
            (_, index) => new Text(index.ToString()),
            childCount: count);

        using var harness = new CupertinoThemeTestHarness(Wrap(Build(1)));
        harness.Pump(ViewSize);
        Assert.Single(harness.FindWidgets<Text>());

        harness.PumpWidget(Wrap(Build(2)));
        harness.Pump(ViewSize);
        Assert.Equal(2, harness.FindWidgets<Text>().Count);

        using var zero = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: Build(2))));
        zero.Pump(new Size());
        Assert.Equal(new Size(), FindRender<RenderListWheelViewport>(zero.RenderView).Size);
    }

    [Fact]
    public void Semantics_ExposeCurrentAndAdjacentLabelsAndActions()
    {
        var controller = new FixedExtentScrollController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            50.0,
            _ => { },
            [new Text("0"), new Text("1"), new Text("2")],
            scrollController: controller)));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode? initialPicker = FindSemantics(root, "0");
        Assert.True(initialPicker is not null, DescribeSemantics(root));
        SemanticsNode picker = initialPicker!;
        Assert.Equal("1", picker.IncreasedValue);
        Assert.True(picker.Actions.HasFlag(SemanticsActions.Increase));
        Assert.False(picker.Actions.HasFlag(SemanticsActions.Decrease));

        Assert.True(picker.PerformAction(SemanticsActions.Increase));
        root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        picker = Assert.IsType<SemanticsNode>(FindSemantics(root, "1"));
        Assert.Equal("2", picker.IncreasedValue);
        Assert.Equal("0", picker.DecreasedValue);
        Assert.True(picker.Actions.HasFlag(SemanticsActions.Increase));
        Assert.True(picker.Actions.HasFlag(SemanticsActions.Decrease));
        controller.Dispose();
    }

    [Fact]
    public void Semantics_ExcludeEmptyCurrentAndAdjacentLabels()
    {
        var controller = new FixedExtentScrollController(initialItem: 1);
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            50.0,
            _ => { },
            [
                new Text("0"),
                new ExcludeSemantics(child: new Text("1")),
                new Text("2"),
            ],
            scrollController: controller)));

        _ = harness.PumpAndGetSemantics(ViewSize);
        SemanticsNode picker = Assert.IsType<SemanticsNode>(
            FindRender<RenderCupertinoPickerSemantics>(harness.RenderView).SemanticsNode);
        Assert.True(string.IsNullOrEmpty(picker.Value));
        Assert.False(picker.Actions.HasFlag(SemanticsActions.Increase));
        Assert.False(picker.Actions.HasFlag(SemanticsActions.Decrease));

        controller.JumpToItem(0);
        _ = harness.PumpAndGetSemantics(ViewSize);
        picker = Assert.IsType<SemanticsNode>(
            FindRender<RenderCupertinoPickerSemantics>(harness.RenderView).SemanticsNode);
        Assert.Equal("0", picker.Value);
        Assert.False(picker.Actions.HasFlag(SemanticsActions.Increase));
        controller.Dispose();
    }

    [Fact]
    public void TappingAChildSelectsItWithTheCupertinoAnimation()
    {
        int selectedItem = 0;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        var controller = new FixedExtentScrollController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            50.0,
            index => selectedItem = index,
            [new Text("0"), new Text("1"), new Text("2"), new Text("3")],
            scrollController: controller)));
        harness.Pump(ViewSize);

        GestureDetector[] tappableChildren = harness.FindWidgets<GestureDetector>()
            .Where(detector => detector.Behavior == HitTestBehavior.Translucent
                               && detector.ExcludeFromSemantics
                               && detector.OnTap is not null)
            .ToArray();
        Assert.True(tappableChildren.Length >= 3);
        double clock = Scheduler.CurrentSeconds;
        tappableChildren[2].OnTap!.Invoke();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        harness.Pump(ViewSize);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(ViewSize);

        Assert.Equal(2, selectedItem);
        Assert.Equal(2, controller.SelectedItem);
        Assert.Empty(platform.Log);
        controller.Dispose();
    }

    [Fact]
    public void TappingAChildAndUnmountingBeforeTheAnimationSettlesDoesNotThrow()
    {
        // `_handleTap`'s continuation reads `SelectedItem`, which throws once the scroll view has
        // detached. Dart's continuation is protected by the widget lifetime; C#'s `async void` is not, so
        // the state guards on `Mounted` before touching the controller again.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        var controller = new FixedExtentScrollController();
        double clock;

        using (var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
                   50.0,
                   _ => { },
                   [new Text("0"), new Text("1"), new Text("2"), new Text("3")],
                   scrollController: controller))))
        {
            harness.Pump(ViewSize);
            GestureDetector[] tappableChildren = harness.FindWidgets<GestureDetector>()
                .Where(detector => detector.Behavior == HitTestBehavior.Translucent
                                   && detector.ExcludeFromSemantics
                                   && detector.OnTap is not null)
                .ToArray();
            Assert.True(tappableChildren.Length >= 3);

            clock = Scheduler.CurrentSeconds;
            tappableChildren[2].OnTap!.Invoke();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
            harness.Pump(ViewSize);
        }

        // The picker is unmounted mid-animation; draining the remaining frames must not fault.
        controller.Dispose();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 1.0));
    }

    [Fact]
    public void IOSScrollTriggersSelectionFeedbackButOtherPlatformsDoNot()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        var controller = new FixedExtentScrollController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            50.0,
            _ => { },
            [new Text("0"), new Text("1"), new Text("2")],
            scrollController: controller)));
        harness.Pump(ViewSize);

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        controller.JumpToItem(1);
        Assert.Equal(["HapticFeedback.vibrate", "SystemSound.play"], platform.Methods);
        Assert.Equal("HapticFeedbackType.selectionClick", platform.Log[0].Arguments);
        Assert.Equal("SystemSoundType.tick", platform.Log[1].Arguments);

        platform.Log.Clear();
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        controller.JumpToItem(2);
        Assert.Empty(platform.Log);
        controller.Dispose();
    }

    [Fact]
    public void ExternalControllerListenersAreRemovedWhenPickerIsDisposed()
    {
        var controller = new SpyFixedExtentScrollController();
        Assert.False(controller.HasAnyListeners);

        var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoPicker(
            50.0,
            _ => { },
            [new Text("0"), new Text("1")],
            scrollController: controller)));
        harness.Pump(ViewSize);
        Assert.True(controller.HasAnyListeners);
        Assert.Equal(2, controller.ListenerCount);

        harness.PumpWidget(new SizedBox());
        Assert.Equal(0, controller.ListenerCount);
        Assert.False(controller.HasAnyListeners);
        harness.Dispose();
        controller.Dispose();
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(PlatformBrightness: brightness),
                new CupertinoTheme(
                    new CupertinoThemeData(brightness: brightness),
                    new SizedBox(
                        width: ViewSize.Width,
                        height: ViewSize.Height,
                        child: child))));
    }

    private static T FindRender<T>(RenderObject root) where T : RenderObject
    {
        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindRenderOrNull<T>(child));
        return result ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");
    }

    private static T? FindRenderOrNull<T>(RenderObject root) where T : RenderObject
    {
        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindRenderOrNull<T>(child));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode node, string value)
    {
        if (node.Value == value)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? match = FindSemantics(child, value);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string DescribeSemantics(SemanticsNode node, int depth = 0)
    {
        string description = $"{new string(' ', depth)}value={node.Value}, label={node.Label}, "
                             + $"index={node.IndexInParent}, actions={node.Actions}\n";
        foreach (SemanticsNode child in node.Children)
        {
            description += DescribeSemantics(child, depth + 2);
        }

        return description;
    }

    private sealed class SpyFixedExtentScrollController : FixedExtentScrollController
    {
        public int ListenerCount { get; private set; }

        public bool HasAnyListeners => HasListeners;

        public override void AddListener(Action listener)
        {
            ListenerCount++;
            base.AddListener(listener);
        }

        public override void RemoveListener(Action listener)
        {
            ListenerCount--;
            base.RemoveListener(listener);
        }
    }
}
