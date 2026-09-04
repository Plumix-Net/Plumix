using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class UnconstrainedLimitedBoxTests
{
    [Fact]
    public void RenderUnconstrainedBox_UnconstrainsBothAxes_AndAlignsChild()
    {
        var child = new FixedSizeRenderBox(new Size(120, 40));
        var unconstrained = new RenderUnconstrainedBox(
            alignment: Alignment.Center,
            child: child);
        var root = new RenderView
        {
            Child = unconstrained
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(80, 80));

        Assert.Equal(new Size(80, 40), unconstrained.Size);
        Assert.Equal(new Size(120, 40), child.Size);
        Assert.Equal(new Point(-20, 0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderUnconstrainedBox_WithHorizontalAxis_RetainsHorizontalConstraints()
    {
        var child = new FixedSizeRenderBox(new Size(120, 200));
        var unconstrained = new RenderUnconstrainedBox(
            alignment: Alignment.TopLeft,
            constrainedAxis: Axis.Horizontal,
            child: child);
        var root = new RenderView
        {
            Child = unconstrained
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(80, 80));

        Assert.Equal(new Size(80, 80), unconstrained.Size);
        Assert.Equal(new Size(80, 200), child.Size);
        Assert.Equal(new Point(0, 0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void UnconstrainedBoxWidget_ComposesConstraintsTransformBox_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Directionality(
                TextDirection.Rtl,
                new UnconstrainedBox(
                    alignment: AlignmentDirectional.TopStart,
                    constrainedAxis: Axis.Vertical,
                    clipBehavior: Clip.HardEdge,
                    child: new SizedBox(width: 10, height: 10))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderConstraintsTransformBox>(root.ChildElement);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopStart, renderBox.Alignment);
        Assert.Equal(TextDirection.Rtl, renderBox.TextDirection);
        Assert.Equal(Clip.HardEdge, renderBox.ClipBehavior);
        Assert.Equal(
            new BoxConstraints(MinHeight: 5, MaxHeight: 15),
            renderBox.ConstraintsTransform(new BoxConstraints(
                MinWidth: 4,
                MaxWidth: 14,
                MinHeight: 5,
                MaxHeight: 15)));

        root.Update(new Directionality(
            TextDirection.Ltr,
            new UnconstrainedBox(
                alignment: Alignment.BottomRight,
                constrainedAxis: null,
                clipBehavior: Clip.AntiAlias,
                child: new SizedBox(width: 10, height: 10))));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderConstraintsTransformBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal((AlignmentGeometry)Alignment.BottomRight, updated.Alignment);
        Assert.Equal(TextDirection.Ltr, updated.TextDirection);
        Assert.Equal(Clip.AntiAlias, updated.ClipBehavior);
        Assert.Equal(
            new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: double.PositiveInfinity),
            updated.ConstraintsTransform(new BoxConstraints(
                MinWidth: 4,
                MaxWidth: 14,
                MinHeight: 5,
                MaxHeight: 15)));
    }

    [Fact]
    public void ConstraintsTransformBoxWidget_ExplicitDirectionResolvesDirectionalAlignment()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new ConstraintsTransformBox(
                constraintsTransform: ConstraintsTransformBox.Unconstrained,
                textDirection: TextDirection.Ltr,
                alignment: AlignmentDirectional.CenterStart,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderConstraintsTransformBox>(root.ChildElement);
        Assert.Equal(TextDirection.Ltr, renderBox.TextDirection);

        renderBox.Layout(BoxConstraints.Tight(new Size(40, 20)));

        Assert.Equal(new Point(0, 5), ((BoxParentData)renderBox.Child!.parentData!).offset);
    }

    [Fact]
    public void ConstraintsTransformBox_PredefinedTransformsMatchFlutterContracts()
    {
        var constraints = new BoxConstraints(
            MinWidth: 10,
            MaxWidth: 100,
            MinHeight: 20,
            MaxHeight: 200);

        Assert.Equal(constraints, ConstraintsTransformBox.Unmodified(constraints));
        Assert.Equal(
            new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: double.PositiveInfinity),
            ConstraintsTransformBox.Unconstrained(constraints));
        Assert.Equal(
            new BoxConstraints(
                MaxWidth: double.PositiveInfinity,
                MinHeight: 20,
                MaxHeight: 200),
            ConstraintsTransformBox.WidthUnconstrained(constraints));
        Assert.Equal(
            new BoxConstraints(
                MinWidth: 10,
                MaxWidth: 100,
                MaxHeight: double.PositiveInfinity),
            ConstraintsTransformBox.HeightUnconstrained(constraints));
        Assert.Equal(
            constraints with { MaxWidth = double.PositiveInfinity },
            ConstraintsTransformBox.MaxWidthUnconstrained(constraints));
        Assert.Equal(
            constraints with { MaxHeight = double.PositiveInfinity },
            ConstraintsTransformBox.MaxHeightUnconstrained(constraints));
        Assert.Equal(
            constraints with
            {
                MaxWidth = double.PositiveInfinity,
                MaxHeight = double.PositiveInfinity,
            },
            ConstraintsTransformBox.MaxUnconstrained(constraints));
    }

    [Fact]
    public void RenderConstraintsTransformBox_TransformsAlignsAndTracksOverflow()
    {
        var child = new FixedSizeRenderBox(new Size(120, 40));
        var transform = new RenderConstraintsTransformBox(
            alignment: Alignment.Center,
            textDirection: TextDirection.Ltr,
            constraintsTransform: ConstraintsTransformBox.Unconstrained,
            child: child);

        transform.Layout(BoxConstraints.Tight(new Size(80, 80)));

        Assert.Equal(
            new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: double.PositiveInfinity),
            transform.ChildConstraints);
        Assert.Equal(new Size(120, 40), child.Size);
        Assert.Equal(new Size(80, 80), transform.Size);
        Assert.Equal(new Point(-20, 20), ((BoxParentData)child.parentData!).offset);
        Assert.True(transform.IsOverflowing);
    }

    [Fact]
    public void RenderConstraintsTransformBox_ClipsOnlyOverflowAndReportsPaintClip()
    {
        var child = new FixedSizeRenderBox(new Size(120, 40), paints: true);
        var transform = new RenderConstraintsTransformBox(
            alignment: Alignment.Center,
            textDirection: TextDirection.Ltr,
            constraintsTransform: ConstraintsTransformBox.Unconstrained,
            child: child,
            clipBehavior: Clip.AntiAlias);
        var root = new RenderView
        {
            Child = transform,
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(80, 80));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        // Nothing below needs compositing, so the overflow clip is recorded onto the canvas.
        Assert.Empty(FindLayers<ClipRectLayer>(pipeline.RootLayer));
        Assert.NotEmpty(FindLayers<PictureLayer>(pipeline.RootLayer));
        Assert.Equal(
            new Rect(0, 0, 80, 40),
            transform.InvokeDescribeApproximatePaintClip(child));

        transform.ClipBehavior = Clip.None;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        Assert.Empty(FindLayers<ClipRectLayer>(pipeline.RootLayer));
        Assert.NotEmpty(FindLayers<PictureLayer>(pipeline.RootLayer));
        Assert.Null(transform.InvokeDescribeApproximatePaintClip(child));
    }

    [Fact]
    public void RenderConstraintsTransformBox_RejectsNonNormalizedChildConstraints()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var transform = new RenderConstraintsTransformBox(
            alignment: Alignment.Center,
            textDirection: TextDirection.Ltr,
            constraintsTransform: _ => new BoxConstraints(MinWidth: 20, MaxWidth: 10),
            child: new FixedSizeRenderBox(new Size(10, 10)));

        var error = Assert.Throws<InvalidOperationException>(() =>
            transform.Layout(BoxConstraints.Loose(new Size(100, 100))));

        Assert.Contains("non-normalized", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderConstraintsTransformBox_RequiresDirectionOnlyForDirectionalAlignment()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var physical = new RenderConstraintsTransformBox(
            alignment: Alignment.CenterRight,
            textDirection: null,
            constraintsTransform: ConstraintsTransformBox.Unconstrained,
            child: new FixedSizeRenderBox(new Size(10, 10)));
        physical.Layout(BoxConstraints.Tight(new Size(40, 20)));
        Assert.Equal(new Point(30, 5), ((BoxParentData)physical.Child!.parentData!).offset);

        var directional = new RenderConstraintsTransformBox(
            alignment: AlignmentDirectional.CenterEnd,
            textDirection: null,
            constraintsTransform: ConstraintsTransformBox.Unmodified,
            child: new FixedSizeRenderBox(new Size(10, 10)));
        Assert.Throws<InvalidOperationException>(() =>
            directional.Layout(BoxConstraints.Tight(new Size(40, 20))));
    }

    [Fact]
    public void RenderLimitedBox_AppliesLimitsWhenParentIsUnbounded()
    {
        var child = new FixedSizeRenderBox(new Size(300, 200));
        var limited = new RenderLimitedBox(
            maxWidth: 100,
            maxHeight: 80,
            child: child);

        limited.Layout(new BoxConstraints(
            MinWidth: 0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: 0,
            MaxHeight: double.PositiveInfinity));

        Assert.Equal(new Size(100, 80), child.Size);
        Assert.Equal(new Size(100, 80), limited.Size);
    }

    [Fact]
    public void RenderLimitedBox_IgnoresOwnLimitsWhenParentIsBounded()
    {
        var child = new FixedSizeRenderBox(new Size(300, 200));
        var limited = new RenderLimitedBox(
            maxWidth: 100,
            maxHeight: 80,
            child: child);

        limited.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 60, MinHeight: 0, MaxHeight: 50));

        Assert.Equal(new Size(60, 50), child.Size);
        Assert.Equal(new Size(60, 50), limited.Size);
    }

    [Fact]
    public void LimitedBoxWidget_CreatesRenderObject_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new LimitedBox(
                maxWidth: 120,
                maxHeight: 70,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderLimitedBox>(root.ChildElement);
        Assert.Equal(120, renderBox.MaxWidth);
        Assert.Equal(70, renderBox.MaxHeight);

        root.Update(new LimitedBox(
            maxWidth: 90,
            maxHeight: 44,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderLimitedBox>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.Equal(90, updated.MaxWidth);
        Assert.Equal(44, updated.MaxHeight);
    }

    [Fact]
    public void LimitedBox_HasDebugOnlyMaximumAssertions()
    {
        double[] invalidValues = [-1.0, double.NaN];
        foreach (double value in invalidValues)
        {
            LimitedBox? widget = null;
            RenderLimitedBox? renderObject = null;
            Exception? widgetError = Record.Exception(() => widget = new LimitedBox(maxWidth: value));
            Exception? renderError = Record.Exception(() => renderObject = new RenderLimitedBox(maxHeight: value));

            if (Constants.KDebugMode)
            {
                Assert.IsType<AssertionError>(widgetError);
                Assert.IsType<AssertionError>(renderError);
            }
            else
            {
                Assert.Null(widgetError);
                Assert.Null(renderError);
                Assert.Equal(value, widget!.MaxWidth);
                Assert.Equal(value, renderObject!.MaxHeight);
            }
        }

        var updated = new RenderLimitedBox();
        Exception? updateError = Record.Exception(() => updated.MaxWidth = -1.0);
        if (Constants.KDebugMode)
        {
            Assert.IsType<AssertionError>(updateError);
        }
        else
        {
            Assert.Null(updateError);
            Assert.Equal(-1.0, updated.MaxWidth);
        }

        Assert.Equal(double.PositiveInfinity, new LimitedBox().MaxWidth);
        Assert.Equal(double.PositiveInfinity, new RenderLimitedBox().MaxHeight);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private static List<T> FindLayers<T>(Layer layer) where T : Layer
    {
        var result = new List<T>();
        if (layer is T target)
        {
            result.Add(target);
        }

        if (layer is ContainerLayer container)
        {
            foreach (Layer child in container.Children)
            {
                result.AddRange(FindLayers<T>(child));
            }
        }

        return result;
    }

    private sealed class FixedSizeRenderBox : RenderBox
    {
        private readonly Size _size;
        private readonly bool _paints;

        public FixedSizeRenderBox(Size size, bool paints = false)
        {
            _size = size;
            _paints = paints;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            if (_paints)
            {
                ctx.Canvas.DrawRectangle(Brushes.Red, pen: null, new Rect(offset, Size));
            }
        }
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

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
