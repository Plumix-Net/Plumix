using Avalonia;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RadioGroupRawRadioTests : IDisposable
{
    public RadioGroupRawRadioTests()
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

    [Fact]
    public void RawRadio_Constructor_RequiresRegistryWhenEnabled()
    {
        Assert.Throws<ArgumentException>(() => new RawRadio<string>(
            value: "a",
            mouseCursor: WidgetStateProperty<MouseCursor>.All(SystemMouseCursors.Click),
            toggleable: false,
            focusNode: new FocusNode(),
            autofocus: false,
            groupRegistry: null,
            enabled: true,
            builder: (_, _) => new SizedBox(width: 20, height: 20)));
    }

    [Fact]
    public void RawRadio_RegistersAnimatesAndUnregisters_WithSourceStateSurface()
    {
        var registry = new TestRegistry<string>("a");
        RawRadioState<string>? capturedState = null;
        using (var harness = new WidgetRenderHarness(
                   new RawRadio<string>(
                       value: "a",
                       mouseCursor: WidgetStateProperty<MouseCursor>.ResolveWith(states =>
                           states.Contains(WidgetState.Disabled)
                               ? SystemMouseCursors.Basic
                               : SystemMouseCursors.Click),
                       toggleable: true,
                       focusNode: new FocusNode(),
                       autofocus: false,
                       groupRegistry: registry,
                       enabled: true,
                       builder: (_, state) =>
                       {
                           capturedState = state;
                           return new SizedBox(width: 20, height: 20);
                       })))
        {
            harness.Pump(new Size(80, 40));

            RawRadioState<string> state = Assert.IsType<RawRadioState<string>>(capturedState);
            Assert.Single(registry.Clients);
            Assert.True(state.Selected);
            Assert.Equal(1.0, state.Position);
            Assert.Contains(WidgetState.Selected, state.States);

            registry.SetGroupValue("b");
            state.AnimateToValue();
            Scheduler.PumpFrameForTests(
                TimeSpan.FromSeconds(Scheduler.CurrentSeconds) + TimeSpan.FromMilliseconds(250));
            harness.Pump(new Size(80, 40));

            Assert.False(state.Selected);
            Assert.Equal(0.0, state.Position);
        }

        Assert.Empty(registry.Clients);
    }

    [Fact]
    public void RadioGroup_ArrowKeysSelectNextEnabledAndWrap()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroupProbe(disableMiddle: true))));
        harness.Pump(new Size(240, 120));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        Assert.True(state.FirstFocus.RequestFocus());
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowRight", isDown: true)));
        harness.Pump(new Size(240, 120));

        Assert.Equal("third", state.GroupValue);
        Assert.True(state.ThirdFocus.HasFocus);

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowRight", isDown: true)));
        harness.Pump(new Size(240, 120));

        Assert.Equal("first", state.GroupValue);
        Assert.True(state.FirstFocus.HasFocus);
    }

    [Fact]
    public void RadioGroup_SpaceTogglesOnlyToggleableSelectedRadio()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroupProbe(toggleable: true))));
        harness.Pump(new Size(240, 120));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        state.FirstFocus.RequestFocus();
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Space", isDown: true)));
        harness.Pump(new Size(240, 120));

        Assert.Null(state.GroupValue);
    }

    [Fact]
    public void RadioGroup_TabTraversalExposesOnlySelectedRadio()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Column(
                        children:
                        [
                            new RadioGroupProbe(),
                            new Focus(
                                focusNode: new FocusNode(),
                                child: new SizedBox(width: 20, height: 20)),
                        ]))));
        harness.Pump(new Size(240, 160));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        Assert.True(FocusManager.Instance.FocusNext());
        Assert.True(state.FirstFocus.HasFocus);
        Assert.True(FocusManager.Instance.FocusNext());
        Assert.False(state.SecondFocus.HasFocus);
        Assert.False(state.ThirdFocus.HasFocus);
    }

    [Fact]
    public void RadioGroupAndRawRadio_ExposeGroupAndCheckedSemantics()
    {
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new RadioGroup<string>(
                    groupValue: "a",
                    onChanged: _ => { },
                    child: new RawRadioFromGroup(value: "a"))));

        SemanticsNode root = Assert.IsType<SemanticsNode>(
            harness.PumpAndGetSemantics(new Size(80, 40)));
        SemanticsNode group = Assert.Single(FindNodes(root, node => node.Role == SemanticsRole.RadioGroup));
        SemanticsNode radio = Assert.Single(FindNodes(group, node =>
            node.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)));

        Assert.True(radio.Flags.HasFlag(SemanticsFlags.HasCheckedState));
        Assert.True(radio.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.True(radio.Flags.HasFlag(SemanticsFlags.IsEnabled));
    }

    [Fact]
    public void RadioGroup_RejectsMultipleClientsWithSelectedValue()
    {
        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroup<string>(
                        groupValue: "same",
                        onChanged: _ => { },
                        child: new Column(
                            children:
                            [
                                new Radio<string>(value: "same"),
                                new Radio<string>(value: "same"),
                            ]))))));
    }

    private static List<SemanticsNode> FindNodes(
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
            result.AddRange(FindNodes(child, predicate));
        }

        return result;
    }

    private sealed class TestRegistry<T> : RadioGroupRegistry<T>
    {
        private T? _groupValue;

        public TestRegistry(T? groupValue)
        {
            _groupValue = groupValue;
        }

        public List<RadioClient<T>> Clients { get; } = [];

        public override T? GroupValue => _groupValue;

        public override Action<T?> OnChanged => value => _groupValue = value;

        public void SetGroupValue(T? value)
        {
            _groupValue = value;
        }

        public override void RegisterClient(RadioClient<T> radio)
        {
            Clients.Add(radio);
        }

        public override void UnregisterClient(RadioClient<T> radio)
        {
            Clients.Remove(radio);
        }
    }

    private sealed class RawRadioFromGroup : StatelessWidget
    {
        public RawRadioFromGroup(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override Widget Build(BuildContext context)
        {
            RadioGroupRegistry<string> registry = RadioGroup<string>.MaybeOf(context)
                                                  ?? throw new InvalidOperationException();
            return new RawRadio<string>(
                value: Value,
                mouseCursor: WidgetStateProperty<MouseCursor>.All(SystemMouseCursors.Click),
                toggleable: false,
                focusNode: new FocusNode(),
                autofocus: false,
                groupRegistry: registry,
                enabled: true,
                builder: (_, _) => new SizedBox(width: 20, height: 20));
        }
    }

    private sealed class RadioGroupProbe : StatefulWidget
    {
        public RadioGroupProbe(bool disableMiddle = false, bool toggleable = false)
        {
            DisableMiddle = disableMiddle;
            Toggleable = toggleable;
        }

        public bool DisableMiddle { get; }

        public bool Toggleable { get; }

        public override State CreateState()
        {
            return new RadioGroupProbeState();
        }
    }

    private sealed class RadioGroupProbeState : State
    {
        public string? GroupValue { get; private set; } = "first";

        public FocusNode FirstFocus { get; } = new();

        public FocusNode SecondFocus { get; } = new();

        public FocusNode ThirdFocus { get; } = new();

        private RadioGroupProbe CurrentWidget => (RadioGroupProbe)StateWidget;

        public override Widget Build(BuildContext context)
        {
            return new RadioGroup<string>(
                groupValue: GroupValue,
                onChanged: value => SetState(() => GroupValue = value),
                child: new Column(
                    children:
                    [
                        new Radio<string>(
                            value: "first",
                            toggleable: CurrentWidget.Toggleable,
                            focusNode: FirstFocus),
                        new Radio<string>(
                            value: "second",
                            enabled: !CurrentWidget.DisableMiddle,
                            focusNode: SecondFocus),
                        new Radio<string>(
                            value: "third",
                            focusNode: ThirdFocus),
                    ]));
        }

        public override void Dispose()
        {
            FirstFocus.Dispose();
            SecondFocus.Dispose();
            ThirdFocus.Dispose();
        }
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

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public T FindState<T>() where T : State
        {
            return FindState<T>(_rootElement)
                   ?? throw new InvalidOperationException($"State {typeof(T).Name} not found.");
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private static T? FindState<T>(Element element) where T : State
        {
            if (element is StatefulElement { State: T state })
            {
                return state;
            }

            T? result = null;
            element.VisitChildren(child => result ??= FindState<T>(child));
            return result;
        }

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

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
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
        }
    }
}
