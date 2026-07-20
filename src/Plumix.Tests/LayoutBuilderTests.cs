using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/layout_builder.dart
// flutter/packages/flutter/lib/src/widgets/orientation_builder.dart

public sealed class LayoutBuilderTests
{
    [Fact]
    public void LayoutBuilder_ExposesSourceContractAndValidatesBuilder()
    {
        LayoutWidgetBuilder builder = (_, _) => new SizedBox();
        var widget = new LayoutBuilder(builder);

        Assert.Same(builder, widget.Builder);
        Assert.Throws<ArgumentNullException>(() => new LayoutBuilder(null!));
    }

    [Fact]
    public void LayoutBuilder_DefersBuildUntilLayoutAndForwardsConstraints()
    {
        int builderCalls = 0;
        BoxConstraints? receivedConstraints = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, constraints) =>
        {
            builderCalls++;
            receivedConstraints = constraints;
            return new SizedBox(width: 36, height: 18);
        }));
        Mount(root, owner);

        Assert.Equal(0, builderCalls);
        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 120, MaxHeight: 80);

        renderObject.Layout(constraints);

        Assert.Equal(1, builderCalls);
        Assert.Equal(constraints, receivedConstraints);
        Assert.Equal(new Size(36, 18), renderObject.Size);
        Assert.Equal(new Size(36, 18), renderObject.Child!.Size);
    }

    [Fact]
    public void LayoutBuilder_RebuildsForChangedConstraintsButSkipsEquivalentLayoutInfo()
    {
        int builderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, constraints) =>
        {
            builderCalls++;
            return new SizedBox(width: constraints.MaxWidth / 2.0, height: 20);
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var firstConstraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 60);
        renderObject.Layout(firstConstraints);
        Assert.Equal(1, builderCalls);
        Assert.Equal(new Size(50, 20), renderObject.Size);

        renderObject.ScheduleLayoutCallback();
        renderObject.Layout(firstConstraints);
        Assert.Equal(1, builderCalls);

        var secondConstraints = new BoxConstraints(MaxWidth: 160, MaxHeight: 60);
        renderObject.Layout(secondConstraints);
        Assert.Equal(2, builderCalls);
        Assert.Equal(new Size(80, 20), renderObject.Size);
    }

    [Fact]
    public void LayoutBuilder_WidgetUpdateUsesNewBuilderAtNextLayout()
    {
        int firstBuilderCalls = 0;
        int secondBuilderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, _) =>
        {
            firstBuilderCalls++;
            return new SizedBox(width: 20, height: 10);
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 100);
        renderObject.Layout(constraints);

        root.Update(new LayoutBuilder((_, _) =>
        {
            secondBuilderCalls++;
            return new SizedBox(width: 40, height: 30);
        }));

        Assert.Equal(1, firstBuilderCalls);
        Assert.Equal(0, secondBuilderCalls);
        Assert.Same(renderObject, root.ChildElement!.RenderObject);

        renderObject.Layout(constraints);

        Assert.Equal(1, firstBuilderCalls);
        Assert.Equal(1, secondBuilderCalls);
        Assert.Equal(new Size(40, 30), renderObject.Size);
    }

    [Fact]
    public void LayoutBuilder_MarkNeedsBuildRebuildsWithLastConstraintsDuringNextLayout()
    {
        int builderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, _) =>
        {
            builderCalls++;
            return new SizedBox(width: 20, height: 10);
        }));
        Mount(root, owner);

        var element = Assert.IsType<LayoutBuilderElement>(root.ChildElement);
        var renderObject = Assert.IsType<RenderLayoutBuilder>(element.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 100);
        renderObject.Layout(constraints);

        element.MarkNeedsBuild();
        owner.FlushBuild();
        Assert.Equal(1, builderCalls);

        renderObject.Layout(constraints);
        Assert.Equal(2, builderCalls);
    }

    [Fact]
    public void LayoutBuilder_RebuildsAtLayoutWhenInheritedDependencyChanges()
    {
        int builderCalls = 0;
        var values = new List<int>();
        var layoutBuilder = new LayoutBuilder((context, _) =>
        {
            builderCalls++;
            values.Add(context.DependOnInherited<TestInheritedValue>()!.Value);
            return new SizedBox(width: 20, height: 10);
        });
        var owner = new BuildOwner();
        var root = new TestRootElement(new TestInheritedValue(1, layoutBuilder));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 100);
        renderObject.Layout(constraints);
        Assert.Equal([1], values);

        root.Update(new TestInheritedValue(2, layoutBuilder));
        owner.FlushBuild();
        Assert.Equal(1, builderCalls);

        renderObject.Layout(constraints);
        Assert.Equal(2, builderCalls);
        Assert.Equal([1, 2], values);
    }

    [Theory]
    [InlineData(120, 80, Orientation.Landscape)]
    [InlineData(80, 120, Orientation.Portrait)]
    [InlineData(100, 100, Orientation.Portrait)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, Orientation.Portrait)]
    public void OrientationBuilder_UsesConstraintOrientation(
        double maxWidth,
        double maxHeight,
        Orientation expected)
    {
        Orientation? received = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new OrientationBuilder((_, orientation) =>
        {
            received = orientation;
            return new SizedBox(width: 10, height: 10);
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        renderObject.Layout(new BoxConstraints(MaxWidth: maxWidth, MaxHeight: maxHeight));

        Assert.Equal(expected, received);
        Assert.Equal(new Size(10, 10), renderObject.Size);
    }

    [Fact]
    public void OrientationBuilder_ExposesSourceContractAndValidatesBuilder()
    {
        OrientationWidgetBuilder builder = (_, _) => new SizedBox();
        var widget = new OrientationBuilder(builder);

        Assert.Same(builder, widget.Builder);
        Assert.Throws<ArgumentNullException>(() => new OrientationBuilder(null!));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TestInheritedValue : InheritedWidget
    {
        private readonly Widget _child;

        public TestInheritedValue(int value, Widget child)
        {
            Value = value;
            _child = child;
        }

        public int Value { get; }

        public override Widget Build(BuildContext context) => _child;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
            Value != ((TestInheritedValue)oldWidget).Value;
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support child moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }
}
