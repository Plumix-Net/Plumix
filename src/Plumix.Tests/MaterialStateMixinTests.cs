using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: material_ui/lib/src/material_state_mixin.dart
// Mirrors `material_ui/test/material_state_mixin_test.dart`: each `WidgetState` value is tracked
// through the callback `updateMaterialState` hands to a child, and the state's `is*` getter flips
// with it.
public sealed class MaterialStateMixinTests
{
    [Theory]
    [InlineData(WidgetState.Pressed)]
    [InlineData(WidgetState.Focused)]
    [InlineData(WidgetState.Hovered)]
    [InlineData(WidgetState.Disabled)]
    [InlineData(WidgetState.Selected)]
    [InlineData(WidgetState.ScrolledUnder)]
    [InlineData(WidgetState.Dragged)]
    [InlineData(WidgetState.Error)]
    public void UpdateMaterialState_TracksEveryWidgetStateValue(WidgetState state)
    {
        var owner = new BuildOwner();
        var widget = new TrackingWidget(state);
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        TrackingState trackingState = widget.LastState!;
        Assert.False(trackingState.Evaluate());

        trackingState.Notify(true);
        owner.FlushBuild();
        Assert.True(trackingState.Evaluate());
        Assert.Contains(state, trackingState.States);

        trackingState.Notify(false);
        owner.FlushBuild();
        Assert.False(trackingState.Evaluate());
        Assert.DoesNotContain(state, trackingState.States);
    }

    [Fact]
    public void UpdateMaterialState_InvokesOnChangedOnlyOnARealChange()
    {
        var owner = new BuildOwner();
        var widget = new TrackingWidget(WidgetState.Hovered);
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        TrackingState trackingState = widget.LastState!;
        var changes = new List<bool>();
        trackingState.OnChanged = changes.Add;

        trackingState.Notify(true);
        owner.FlushBuild();
        trackingState.Notify(true);
        owner.FlushBuild();
        trackingState.Notify(false);
        owner.FlushBuild();
        trackingState.Notify(false);
        owner.FlushBuild();

        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void SetMaterialState_AddsAndRemovesWithoutGoingThroughACallback()
    {
        var owner = new BuildOwner();
        var widget = new TrackingWidget(WidgetState.Selected);
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        TrackingState trackingState = widget.LastState!;
        trackingState.Set(WidgetState.Disabled, isSet: true);
        owner.FlushBuild();
        Assert.True(trackingState.IsDisabledState);
        Assert.Equal(new HashSet<WidgetState> { WidgetState.Disabled }, trackingState.States);

        trackingState.Set(WidgetState.Disabled, isSet: false);
        owner.FlushBuild();
        Assert.False(trackingState.IsDisabledState);
        Assert.Empty(trackingState.States);
    }

    [Fact]
    public void MaterialStates_ResolvesACoreWidgetStateProperty()
    {
        var owner = new BuildOwner();
        var widget = new TrackingWidget(WidgetState.Hovered);
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        WidgetStateProperty<string> property = WidgetStateProperty<string>.ResolveWith(
            states => states.Contains(WidgetState.Hovered) ? "hovered" : "rest");
        TrackingState trackingState = widget.LastState!;

        Assert.Equal("rest", property.Resolve(trackingState.States));

        trackingState.Notify(true);
        owner.FlushBuild();
        Assert.Equal("hovered", property.Resolve(trackingState.States));
    }

    [DebugOnlyFact]
    public void DebugFillProperties_DumpsTheManagedSet()
    {
        var owner = new BuildOwner();
        var widget = new TrackingWidget(WidgetState.Focused);
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        TrackingState trackingState = widget.LastState!;
        trackingState.Notify(true);
        owner.FlushBuild();

        var properties = new DiagnosticPropertiesBuilder();
        trackingState.DebugFillProperties(properties);

        DiagnosticsNode dumped = Assert.Single(
            properties.Properties.Where(property => property.Name == "materialStates"));
        var typed = Assert.IsType<DiagnosticsProperty<IReadOnlySet<WidgetState>>>(dumped);
        Assert.Equal(new HashSet<WidgetState> { WidgetState.Focused }, typed.TypedValue);
    }

    private sealed class TrackingWidget : StatefulWidget
    {
        public TrackingWidget(WidgetState state)
        {
            State = state;
        }

        public WidgetState State { get; }

        public TrackingState? LastState { get; set; }

        public override State CreateState() => new TrackingState();
    }

    private sealed class TrackingState : MaterialStateMixin
    {
        private Action<bool>? _notify;

        public Action<bool>? OnChanged { get; set; }

        public IReadOnlySet<WidgetState> States => MaterialStates;

        public bool IsDisabledState => IsDisabled;

        public void Notify(bool value) => _notify!(value);

        public void Set(WidgetState state, bool isSet) => SetMaterialState(state, isSet);

        public bool Evaluate() => ((TrackingWidget)StateWidget).State switch
        {
            WidgetState.Pressed => IsPressed,
            WidgetState.Focused => IsFocused,
            WidgetState.Hovered => IsHovered,
            WidgetState.Disabled => IsDisabled,
            WidgetState.Selected => IsSelected,
            WidgetState.ScrolledUnder => IsScrolledUnder,
            WidgetState.Dragged => IsDragged,
            _ => IsErrored,
        };

        public override Widget Build(BuildContext context)
        {
            var widget = (TrackingWidget)StateWidget;
            widget.LastState = this;
            _notify = UpdateMaterialState(widget.State, value => OnChanged?.Invoke(value));
            return new Text(Evaluate() ? "true" : "false");
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
