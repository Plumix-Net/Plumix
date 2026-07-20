using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/flow.dart

namespace Plumix.Rendering;

/// <summary>Context exposed to <see cref="FlowDelegate.PaintChildren"/> during flow painting.</summary>
public abstract class FlowPaintingContext
{
    public abstract Size Size { get; }

    public abstract int ChildCount { get; }

    public abstract Size? GetChildSize(int index);

    public abstract void PaintChild(int index, Matrix? transform = null, double opacity = 1.0);
}

/// <summary>Controls the size, child constraints, and paint transforms of a <see cref="RenderFlow"/>.</summary>
public abstract class FlowDelegate
{
    protected FlowDelegate(IListenable? repaint = null)
    {
        Repaint = repaint;
    }

    internal IListenable? Repaint { get; }

    public virtual Size GetSize(BoxConstraints constraints) => constraints.Biggest;

    public virtual BoxConstraints GetConstraintsForChild(int index, BoxConstraints constraints) => constraints;

    public abstract void PaintChildren(FlowPaintingContext context);

    public virtual bool ShouldRelayout(FlowDelegate oldDelegate) => false;

    public abstract bool ShouldRepaint(FlowDelegate oldDelegate);
}

public sealed class FlowParentData : ContainerBoxParentData<RenderBox>
{
    internal Matrix? Transform { get; set; }
}

/// <summary>Delegate-driven multi-child layout whose children are positioned during paint.</summary>
public sealed class RenderFlow : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, FlowParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, FlowParentData> _container;
    private readonly List<RenderBox> _randomAccessChildren = [];
    private readonly List<int> _lastPaintOrder = [];
    private FlowDelegate _delegate;
    private Clip _clipBehavior;
    private PaintingContext? _paintingContext;
    private Point? _paintingOffset;

    public RenderFlow(
        FlowDelegate @delegate,
        List<RenderBox>? children = null,
        Clip clipBehavior = Clip.HardEdge)
    {
        _delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        _clipBehavior = clipBehavior;
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, FlowParentData>(this);
        if (children is not null)
        {
            AddAll(children);
        }
    }

    public FlowDelegate Delegate
    {
        get => _delegate;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_delegate, value))
            {
                return;
            }

            FlowDelegate oldDelegate = _delegate;
            _delegate = value;
            if (value.GetType() != oldDelegate.GetType() || value.ShouldRelayout(oldDelegate))
            {
                MarkNeedsLayout();
            }
            else if (value.ShouldRepaint(oldDelegate))
            {
                MarkNeedsPaint();
            }

            if (Attached)
            {
                oldDelegate.Repaint?.RemoveListener(MarkNeedsPaint);
                value.Repaint?.AddListener(MarkNeedsPaint);
            }
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool IsRepaintBoundary => true;

    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public int ChildCount => _container.ChildCount;

    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is FlowParentData parentData)
        {
            parentData.Transform = null;
            return;
        }

        child.parentData = new FlowParentData();
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _delegate.Repaint?.AddListener(MarkNeedsPaint);
    }

    protected override void OnDetach()
    {
        _delegate.Repaint?.RemoveListener(MarkNeedsPaint);
        base.OnDetach();
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_delegate.GetSize(Constraints));
        _randomAccessChildren.Clear();
        int index = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            _randomAccessChildren.Add(child);
            BoxConstraints childConstraints = _delegate.GetConstraintsForChild(index, Constraints);
            if (!childConstraints.IsNormalized)
            {
                throw new InvalidOperationException("FlowDelegate returned non-normalized child constraints.");
            }

            child.Layout(childConstraints, parentUsesSize: true);
            var parentData = (FlowParentData)child.parentData!;
            parentData.offset = default;
            index += 1;
        }
    }

    public Size? GetChildSize(int index)
    {
        return index < 0 || index >= _randomAccessChildren.Count
            ? null
            : _randomAccessChildren[index].Size;
    }

    public void PaintChild(int index, Matrix? transform = null, double opacity = 1.0)
    {
        if (index < 0 || index >= _randomAccessChildren.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (!double.IsFinite(opacity) || opacity < 0.0 || opacity > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        if (_paintingContext is null || !_paintingOffset.HasValue)
        {
            throw new InvalidOperationException("Flow children can only be painted from FlowDelegate.PaintChildren.");
        }

        RenderBox child = _randomAccessChildren[index];
        var parentData = (FlowParentData)child.parentData!;
        if (parentData.Transform.HasValue)
        {
            throw new InvalidOperationException("A FlowDelegate cannot paint the same child more than once.");
        }

        Matrix childTransform = transform ?? Matrix.Identity;
        parentData.Transform = childTransform;
        _lastPaintOrder.Add(index);
        if (opacity == 0.0)
        {
            return;
        }

        void PaintTransformed(PaintingContext context)
        {
            context.PushTransform(childTransform, transformed => transformed.PaintChild(child, default));
        }

        Point paintOffset = _paintingOffset.Value;
        _paintingContext.PushTransform(Matrix.CreateTranslation(paintOffset.X, paintOffset.Y), translated =>
        {
            if (opacity == 1.0)
            {
                PaintTransformed(translated);
                return;
            }

            translated.PushOpacity(opacity, PaintTransformed);
        });
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        void PaintWithDelegate(PaintingContext paintingContext)
        {
            _lastPaintOrder.Clear();
            _paintingContext = paintingContext;
            _paintingOffset = offset;
            foreach (RenderBox child in _randomAccessChildren)
            {
                ((FlowParentData)child.parentData!).Transform = null;
            }

            try
            {
                _delegate.PaintChildren(new RenderFlowPaintingContext(this));
            }
            finally
            {
                _paintingContext = null;
                _paintingOffset = null;
            }
        }

        if (ClipBehavior == Clip.None)
        {
            PaintWithDelegate(context);
            return;
        }

        context.PushClipRect(new Rect(offset, Size), PaintWithDelegate);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            Matrix transform = ((FlowParentData)child.parentData!).Transform ?? Matrix.Identity;
            visitor(child, default, transform);
        }
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return ClipBehavior == Clip.None ? null : new Rect(default, Size);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (int orderIndex = _lastPaintOrder.Count - 1; orderIndex >= 0; orderIndex--)
        {
            int childIndex = _lastPaintOrder[orderIndex];
            if (childIndex >= _randomAccessChildren.Count)
            {
                continue;
            }

            RenderBox child = _randomAccessChildren[childIndex];
            Matrix? transform = ((FlowParentData)child.parentData!).Transform;
            if (!transform.HasValue || !transform.Value.TryInvert(out Matrix inverse))
            {
                continue;
            }

            if (child.HitTest(result, inverse.Transform(position)))
            {
                return true;
            }
        }

        return false;
    }

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    private sealed class RenderFlowPaintingContext(RenderFlow owner) : FlowPaintingContext
    {
        public override Size Size => owner.Size;

        public override int ChildCount => owner.ChildCount;

        public override Size? GetChildSize(int index) => owner.GetChildSize(index);

        public override void PaintChild(int index, Matrix? transform = null, double opacity = 1.0)
        {
            owner.PaintChild(index, transform, opacity);
        }
    }
}
