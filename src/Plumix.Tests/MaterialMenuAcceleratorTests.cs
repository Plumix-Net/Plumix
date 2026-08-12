using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialMenuAcceleratorTests : IDisposable
{
    public MaterialMenuAcceleratorTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Theory]
    [InlineData("&File", "File", 0)]
    [InlineData("Save && E&xit", "Save & Exit", 8)]
    [InlineData("Trailing&", "Trailing", -1)]
    [InlineData("No & accelerator", "No  accelerator", -1)]
    [InlineData("&&Help", "&Help", -1)]
    public void StripAcceleratorMarkers_MatchesFlutterRules(
        string source,
        string expectedLabel,
        int expectedIndex)
    {
        int acceleratorIndex = int.MinValue;

        string displayLabel = MenuAcceleratorLabel.StripAcceleratorMarkers(
            source,
            index => acceleratorIndex = index);

        Assert.Equal(expectedLabel, displayLabel);
        Assert.Equal(expectedIndex, acceleratorIndex);
        var label = new MenuAcceleratorLabel(source);
        Assert.Equal(expectedLabel, label.DisplayLabel);
        Assert.Equal(expectedIndex >= 0, label.HasAccelerator);
    }

    [Fact]
    public void StripAcceleratorMarkers_UsesGraphemeClusterIndexes()
    {
        int acceleratorIndex = -1;

        string displayLabel = MenuAcceleratorLabel.StripAcceleratorMarkers(
            "A&👨‍👩‍👧‍👦B",
            index => acceleratorIndex = index);

        Assert.Equal("A👨‍👩‍👧‍👦B", displayLabel);
        Assert.Equal(1, acceleratorIndex);
    }

    [Fact]
    public void CallbackBinding_ProvidesDependentLookupAndValidatesRequiredAncestor()
    {
        MenuAcceleratorCallbackBinding? captured = null;
        Action callback = () => { };
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new Builder(context =>
                {
                    captured = MenuAcceleratorCallbackBinding.Of(context);
                    return new SizedBox();
                }),
                onInvoke: callback,
                hasSubmenu: true)));

        harness.Pump(new Size(300, 100));

        Assert.NotNull(captured);
        Assert.Same(callback, captured!.OnInvoke);
        Assert.True(captured.HasSubmenu);

        using var missing = new WidgetRenderHarness(Wrap(
            new Builder(context =>
            {
                Assert.Null(MenuAcceleratorCallbackBinding.MaybeOf(context));
                Assert.Throws<InvalidOperationException>(() => MenuAcceleratorCallbackBinding.Of(context));
                return new SizedBox();
            })));
        missing.Pump(new Size(300, 100));
    }

    [Fact]
    public void MenuItemButton_RegistersAltAcceleratorAndUsesUnderlinedDefaultBuilder()
    {
        int invoked = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuItemButton(
                child: new MenuAcceleratorLabel("&Open"),
                onPressed: () => invoked++)));
        harness.Pump(new Size(300, 100));
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));

        Assert.False(FocusManager.Instance.HandleKeyEvent(
            new KeyEvent("LeftAlt", true, isAltPressed: true)));
        harness.Pump(new Size(300, 100));

        RenderParagraph accelerator = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.Text == "O");
        Assert.NotNull(accelerator.TextDecorations);
        Assert.True(FocusManager.Instance.HandleKeyEvent(
            new KeyEvent("O", true, isAltPressed: true)));
        Scheduler.PumpFrameForTests();
        Assert.Equal(1, invoked);

        Assert.False(FocusManager.Instance.HandleKeyEvent(new KeyEvent("LeftAlt", false)));
        harness.Pump(new Size(300, 100));
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));
    }

    [Fact]
    public void CustomBuilder_ReceivesStrippedLabelAndVisibleAcceleratorIndex()
    {
        var builds = new List<(string Label, int Index)>();
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new MenuAcceleratorLabel(
                    "E&xit",
                    (_, label, index) =>
                    {
                        builds.Add((label, index));
                        return new Text(label);
                    }),
                onInvoke: () => { })));
        harness.Pump(new Size(300, 100));
        Assert.Equal(("Exit", -1), builds[^1]);

        FocusManager.Instance.HandleKeyEvent(new KeyEvent("RightAlt", true, isAltPressed: true));
        harness.Pump(new Size(300, 100));
        Assert.Equal(("Exit", 1), builds[^1]);

        FocusManager.Instance.HandleKeyEvent(new KeyEvent("RightAlt", false));
        harness.Pump(new Size(300, 100));
        Assert.Equal(("Exit", -1), builds[^1]);
    }

    [Fact]
    public void LabelMountedWhileAltIsHeld_ShowsItsAcceleratorImmediately()
    {
        int lastIndex = int.MinValue;
        FocusManager.Instance.HandleKeyEvent(new KeyEvent("LeftAlt", true, isAltPressed: true));

        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new MenuAcceleratorLabel(
                    "&Open",
                    (_, label, index) =>
                    {
                        lastIndex = index;
                        return new Text(label);
                    }),
                onInvoke: () => { })));
        harness.Pump(new Size(300, 100));

        Assert.Equal(0, lastIndex);
    }

    [Fact]
    public void DuplicateAccelerators_InvokeOnlyDeepestMostRecentlyMountedLabel()
    {
        var invocations = new List<string>();
        using var harness = new WidgetRenderHarness(Wrap(
            new Column(
                children:
                [
                    new MenuAcceleratorCallbackBinding(
                        new MenuAcceleratorLabel("&Open"),
                        () => invocations.Add("first")),
                    new MenuAcceleratorCallbackBinding(
                        new MenuAcceleratorLabel("&Other"),
                        () => invocations.Add("second")),
                ])));
        harness.Pump(new Size(300, 100));

        FocusManager.Instance.HandleKeyEvent(new KeyEvent("LeftAlt", true, isAltPressed: true));
        Assert.True(FocusManager.Instance.HandleKeyEvent(
            new KeyEvent("O", true, isAltPressed: true)));

        Assert.Equal(["second"], invocations);
    }

    [Fact]
    public void SubmenuAccelerator_OpensClosedMenuAndIsSuppressedWhileItIsOpen()
    {
        var controller = new MenuController();
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuBar(
                children:
                [
                    new SubmenuButton(
                        menuChildren:
                        [
                            new MenuItemButton(
                                child: new MenuAcceleratorLabel("&New"),
                                onPressed: () => { }),
                        ],
                        child: new MenuAcceleratorLabel("&File"),
                        controller: controller),
                ])));
        harness.Pump(new Size(500, 240));

        FocusManager.Instance.HandleKeyEvent(new KeyEvent("LeftAlt", true, isAltPressed: true));
        Assert.True(FocusManager.Instance.HandleKeyEvent(
            new KeyEvent("F", true, isAltPressed: true)));
        harness.Pump(new Size(500, 240));
        Assert.True(controller.IsOpen);

        Assert.False(FocusManager.Instance.HandleKeyEvent(
            new KeyEvent("F", true, isAltPressed: true)));
        Assert.True(controller.IsOpen);
    }

    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void CupertinoPlatforms_StripMarkersWithoutShowingOrInvokingAccelerators(
        TargetPlatform platform)
    {
        int lastIndex = int.MinValue;
        int invoked = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new MenuAcceleratorLabel(
                    "&Open",
                    (_, label, index) =>
                    {
                        lastIndex = index;
                        return new Text(label);
                    }),
                onInvoke: () => invoked++),
            platform));
        harness.Pump(new Size(300, 100));

        FocusManager.Instance.HandleKeyEvent(new KeyEvent("LeftAlt", true, isAltPressed: true));
        harness.Pump(new Size(300, 100));
        Assert.False(FocusManager.Instance.HandleKeyEvent(
            new KeyEvent("O", true, isAltPressed: true)));

        Assert.Equal(-1, lastIndex);
        Assert.Equal(0, invoked);
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));
    }

    private static Widget Wrap(
        Widget child,
        TargetPlatform platform = TargetPlatform.Windows)
    {
        return new Directionality(
            TextDirection.Ltr,
            new Theme(
                ThemeData.Light with { Platform = platform },
                new Overlay(initialEntries: [new OverlayEntry(_ => child)])));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root)
            .FirstOrDefault(paragraph => paragraph.Text == text);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T target)
        {
            result.Add(target);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
