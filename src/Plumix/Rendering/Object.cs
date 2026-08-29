using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

/// Base class for data associated with a [RenderObject] by its parent.
///
/// Some render objects wish to store data on their children, such as the
/// children's input parameters to the parent's layout algorithm or the
/// children's position relative to other children.
///
/// See also:
///
///  * [RenderObject.setupParentData], which [RenderObject] subclasses may
///    override to attach specific types of parent data to children.
public class ParentData : IParentData
{
    /// Called when the RenderObject is removed from the tree.
    ///
    /// Overrides must call the base implementation last.
    public virtual void Detach()
    {
    }

    public override string ToString() => "<none>";
}

public interface IParentData
{
    void Detach();
}

/// <summary>
/// An abstract set of layout constraints.
/// </summary>
public interface IConstraints
{
    /// <summary>
    /// Whether there is exactly one size possible given these constraints.
    /// </summary>
    bool IsTight { get; }

    /// <summary>
    /// Whether the constraint is expressed in a consistent manner.
    /// </summary>
    bool IsNormalized { get; }

    /// <summary>
    /// Asserts that these constraints are valid.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>Constraints.debugAssertIsValid</c>. <paramref name="isAppliedConstraint"/> selects
    /// the stricter rules that apply to constraints that are about to be given to
    /// <c>RenderObject.layout</c>; <paramref name="informationCollector"/> is appended to the error.
    /// The base contract is "return whether the constraints are normalized, throwing when asserts are
    /// enabled", so the default implementation is exactly <see cref="IsNormalized"/>.
    /// </remarks>
    bool DebugAssertIsValid(
        bool isAppliedConstraint = false,
        InformationCollector? informationCollector = null)
    {
        if (!IsNormalized)
        {
            throw new AssertionError($"{GetType().Name} is not normalized.");
        }

        return IsNormalized;
    }
}

/// <summary>
/// A <c>DiagnosticsProperty</c> that carries a <see cref="RenderObject.DebugCreator"/> so error
/// reporters can transform it into the widget that created the render object.
/// </summary>
/// <remarks>Flutter's <c>DiagnosticsDebugCreator</c>.</remarks>
public sealed class DiagnosticsDebugCreator : DiagnosticsProperty<object>
{
    public DiagnosticsDebugCreator(object value)
        : base("debugCreator", value, level: DiagnosticLevel.Hidden)
    {
    }
}

/// <summary>
/// A render object that runs a callback during its own layout, before laying out its children.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderObjectWithLayoutCallbackMixin</c>. C# has no mixins, so only
/// <see cref="LayoutCallback"/> lives on the implementer; the shared body Dart puts in the mixin -
/// the <c>_needsRebuild</c> flag, <c>RenderObject.ScheduleLayoutCallback</c> and
/// <c>RenderObject.RunLayoutCallback</c> - lives on <see cref="RenderObject"/> behind this interface.
/// </remarks>
public interface IRenderObjectWithLayoutCallback : IRenderObject
{
    /// <summary>Runs the layout callback. Do not call directly; call <c>RunLayoutCallback</c>.</summary>
    /// <remarks>Flutter's <c>RenderObjectWithLayoutCallbackMixin.layoutCallback</c>.</remarks>
    void LayoutCallback();
}

public interface IContainerParentDataMixin<TChild> : IParentData
    where TChild : IRenderObject
{
    TChild? previousSibling { get; set; }

    /// The next sibling in the parent's child list.
    TChild? nextSibling { get; set; }

}

public class ContainerParentDataMixin<TChild>(IParentData owner) : IContainerParentDataMixin<TChild>
    where TChild : RenderObject
{
    /// The previous sibling in the parent's child list.
    public TChild? previousSibling { get; set; }

    /// The next sibling in the parent's child list.
    public TChild? nextSibling { get; set; }

    /// Clear the sibling pointers.
    public void Detach()
    {
        Debug.Assert(
            previousSibling == null,
            "Pointers to siblings must be nulled before detaching ParentData."
        );

        Debug.Assert(nextSibling == null, "Pointers to siblings must be nulled before detaching ParentData.");

        owner.Detach();
    }
}

public interface IContainerRenderObjectMixin<TChild, TParentData> : IRenderObject
    where TChild : IRenderObject
    where TParentData : IContainerParentDataMixin<TChild>
{
    int ChildCount { get; }

    /// The first child in the child list.
    TChild? FirstChild { get; }


    /// The last child in the child list.
    TChild? LastChild { get; }

    void Insert(TChild child, TChild? after = default);

    void Move(TChild child, TChild? after = default);

    void Remove(TChild child);

    void AddAll(List<TChild>? children);

    /// <summary>Removes every child from this render object's child list.</summary>
    /// <remarks>Flutter's <c>ContainerRenderObjectMixin.removeAll</c>.</remarks>
    void RemoveAll();

    /// The previous child before the given child in the child list.
    TChild? ChildBefore(TChild child);

    /// The next child after the given child in the child list.
    TChild? ChildAfter(TChild child);
}

public interface IRenderObjectContainer
{
    void Insert(RenderObject child, RenderObject? after = null);
    void Move(RenderObject child, RenderObject? after = null);
    void Remove(RenderObject child);
}

/// <summary>
/// Generic mixin for render objects with a list of children.
/// </summary>
public class ContainerRenderObjectMixin<TChild, TParentData>(RenderObject owner)
    : IContainerRenderObjectMixin<TChild, TParentData>
    where TChild : RenderObject
    where TParentData : IContainerParentDataMixin<TChild>
{
    private int _childCount = 0;

    /// The number of children.
    public int ChildCount => _childCount;

    public TChild? FirstChild => _firstChild;

    public TChild? LastChild => _lastChild;

    public void Insert(TChild child, TChild? after = null)
    {
        Debug.Assert(!ReferenceEquals(child, owner), "A RenderObject cannot be inserted into itself.");
        Debug.Assert(
            !ReferenceEquals(after, owner),
            "A RenderObject cannot simultaneously be both the parent and the sibling of another "
            + "RenderObject.");
        Debug.Assert(!ReferenceEquals(child, after), "A RenderObject cannot be inserted after itself.");
        Debug.Assert(!ReferenceEquals(child, _firstChild));
        Debug.Assert(!ReferenceEquals(child, _lastChild));
        owner.AdoptChild(child);
        Debug.Assert(
            child.parentData is TParentData,
            $"A child of {owner.GetType().Name} has parentData of type "
            + $"{child.parentData?.GetType().Name}, which does not conform to {typeof(TParentData).Name}. "
            + "A class using ContainerRenderObjectMixin should override SetupParentData to set "
            + $"parentData to type {typeof(TParentData).Name}.");

        _insertIntoChildList(child, after: after);
    }

    /// <summary>Validates that <paramref name="child"/> has the child type this container expects.</summary>
    /// <remarks>Flutter's <c>ContainerRenderObjectMixin.debugValidateChild</c>.</remarks>
    public bool DebugValidateChild(RenderObject child)
    {
        return RenderObject.DebugValidateChildType<TChild>(owner, child);
    }

    /// Append child to the end of this render object's child list.
    public void Add(TChild child)
    {
        Insert(child, after: _lastChild);
    }

    /// Add all the children to the end of this render object's child list.
    public void AddAll(List<TChild>? children)
    {
        if (children is null)
        {
            return;
        }

        foreach (TChild child in children)
        {
            Add(child);
        }
    }

    /// <summary>Removes every child from this render object's child list.</summary>
    /// <remarks>
    /// Flutter's <c>ContainerRenderObjectMixin.removeAll</c>. More efficient than removing the
    /// children individually, because the sibling pointers are dropped in one sweep.
    /// </remarks>
    public void RemoveAll()
    {
        TChild? child = _firstChild;
        while (child is not null)
        {
            var childParentData = (TParentData)child.parentData!;
            TChild? next = childParentData.nextSibling;
            childParentData.previousSibling = default;
            childParentData.nextSibling = default;
            owner.DropChild(child);
            child = next;
        }

        _firstChild = null;
        _lastChild = null;
        _childCount = 0;
    }

    /// <remarks>Flutter's <c>ContainerRenderObjectMixin.attach</c>.</remarks>
    public void Attach(PipelineOwner pipelineOwner)
    {
        TChild? child = _firstChild;
        while (child is not null)
        {
            child.Attach(pipelineOwner);
            child = ((TParentData)child.parentData!).nextSibling;
        }
    }

    /// <remarks>Flutter's <c>ContainerRenderObjectMixin.detach</c>.</remarks>
    public void Detach()
    {
        TChild? child = _firstChild;
        while (child is not null)
        {
            child.Detach();
            child = ((TParentData)child.parentData!).nextSibling;
        }
    }

    /// <remarks>Flutter's <c>ContainerRenderObjectMixin.visitChildren</c>.</remarks>
    public void VisitChildren(Action<RenderObject> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        TChild? child = _firstChild;
        while (child is not null)
        {
            TChild? next = ((TParentData)child.parentData!).nextSibling;
            visitor(child);
            child = next;
        }
    }

    /// <remarks>Flutter's <c>ContainerRenderObjectMixin._debugUltimatePreviousSiblingOf</c>.</remarks>
    private bool DebugUltimatePreviousSiblingOf(TChild child, TChild? equals)
    {
        var childParentData = (TParentData)child.parentData!;
        while (childParentData.previousSibling is not null)
        {
            Debug.Assert(!ReferenceEquals(childParentData.previousSibling, child));
            child = childParentData.previousSibling!;
            childParentData = (TParentData)child.parentData!;
        }

        return ReferenceEquals(child, equals);
    }

    /// <remarks>Flutter's <c>ContainerRenderObjectMixin._debugUltimateNextSiblingOf</c>.</remarks>
    private bool DebugUltimateNextSiblingOf(TChild child, TChild? equals)
    {
        var childParentData = (TParentData)child.parentData!;
        while (childParentData.nextSibling is not null)
        {
            Debug.Assert(!ReferenceEquals(childParentData.nextSibling, child));
            child = childParentData.nextSibling!;
            childParentData = (TParentData)child.parentData!;
        }

        return ReferenceEquals(child, equals);
    }

    public void Move(TChild child, TChild? after = null)
    {
        Debug.Assert(!ReferenceEquals(child, owner));
        Debug.Assert(!ReferenceEquals(after, owner));
        Debug.Assert(!ReferenceEquals(child, after));
        Debug.Assert(ReferenceEquals(child.Parent, owner));
        if (ReferenceEquals(ChildBefore(child), after))
        {
            return;
        }

        _removeFromChildList(child);
        _insertIntoChildList(child, after);
        owner.MarkNeedsLayout();
    }

    public void Remove(TChild child)
    {
        _removeFromChildList(child);
        owner.DropChild(child);
    }

    public TChild? ChildAfter(TChild child)
    {
        Debug.Assert(child.Parent == owner);

        TParentData childParentData = (TParentData)child.parentData!;

        return childParentData.nextSibling;
    }

    public TChild? ChildBefore(TChild child)
    {
        Debug.Assert(child.Parent == owner);
        TParentData childParentData = (TParentData)child.parentData!;
        return childParentData.previousSibling;
    }

    private TChild? _firstChild;
    private TChild? _lastChild;

    private void _insertIntoChildList(TChild child, TChild? after = null)
    {
        TParentData childParentData = (TParentData)child.parentData!;
        Debug.Assert(childParentData.nextSibling is null);
        Debug.Assert(childParentData.previousSibling is null);

        _childCount += 1;
        Debug.Assert(_childCount > 0);

        if (after == null)
        {
            // insert at the start (_firstChild)
            childParentData.nextSibling = _firstChild;
            if (_firstChild != null)
            {
                TParentData firstChildParentData = (TParentData)_firstChild!.parentData!;
                firstChildParentData.previousSibling = child;
            }

            _firstChild = child;
            _lastChild ??= child;
        }
        else
        {
            Debug.Assert(_firstChild is not null);
            Debug.Assert(_lastChild is not null);
            Debug.Assert(DebugUltimatePreviousSiblingOf(after, equals: _firstChild));
            Debug.Assert(DebugUltimateNextSiblingOf(after, equals: _lastChild));
            var afterParentData = (TParentData)after.parentData!;

            if (afterParentData.nextSibling == null)
            {
                // insert at the end (_lastChild); we'll end up with two or more children
                Debug.Assert(after == _lastChild);
                childParentData.previousSibling = after;
                afterParentData.nextSibling = child;
                _lastChild = child;
            }
            else
            {
                // insert in the middle; we'll end up with three or more children
                // set up links from child to siblings
                childParentData.nextSibling = afterParentData.nextSibling;
                childParentData.previousSibling = after;

                // set up links from siblings to child
                TParentData childPreviousSiblingParentData =
                    (TParentData)childParentData.previousSibling!.parentData!;
                TParentData childNextSiblingParentData =
                    (TParentData)childParentData.nextSibling!.parentData!;

                childPreviousSiblingParentData.nextSibling = child;
                childNextSiblingParentData.previousSibling = child;

                Debug.Assert(afterParentData.nextSibling == child);
            }
        }
    }

    private void _removeFromChildList(TChild child)
    {
        Debug.Assert(DebugUltimatePreviousSiblingOf(child, equals: _firstChild));
        Debug.Assert(DebugUltimateNextSiblingOf(child, equals: _lastChild));
        Debug.Assert(_childCount >= 0);
        var childParentData = (TParentData)child.parentData!;

        if (childParentData.previousSibling == null)
        {
            Debug.Assert(ReferenceEquals(_firstChild, child));
            _firstChild = childParentData.nextSibling;
        }
        else
        {
            var prevParentData = (TParentData)childParentData.previousSibling.parentData!;
            prevParentData.nextSibling = childParentData.nextSibling;
        }

        if (childParentData.nextSibling == null)
        {
            Debug.Assert(ReferenceEquals(_lastChild, child));
            _lastChild = childParentData.previousSibling;
        }
        else
        {
            var nextParentData = (TParentData)childParentData.nextSibling.parentData!;
            nextParentData.previousSibling = childParentData.previousSibling;
        }

        childParentData.previousSibling = default;
        childParentData.nextSibling = default;
        _childCount -= 1;
    }

    public void AdoptChild(RenderObject child) => owner.AdoptChild(child);

    /// Returns a list of [DiagnosticsNode] objects describing this node's children, named
    /// `child 1`, `child 2` and so on.
    ///
    /// Dart's `ContainerRenderObjectMixin.debugDescribeChildren`; the render object holding this
    /// mixin forwards its own <see cref="RenderObject.DebugDescribeChildren"/> here.
    public List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        if (_firstChild is not null)
        {
            TChild child = _firstChild;
            int count = 1;
            while (true)
            {
                children.Add(child.ToDiagnosticsNode(name: $"child {count}"));
                if (ReferenceEquals(child, _lastChild))
                {
                    break;
                }

                count += 1;
                TParentData childParentData = (TParentData)child.parentData!;
                child = childParentData.nextSibling!;
            }
        }

        return children;
    }
}

public interface IRenderBoxContainerDefaultsMixin<TChild, TParentData>
    : IContainerRenderObjectMixin<TChild, TParentData>
    where TChild : RenderBox
    where TParentData : ContainerBoxParentData<TChild>
{
    void DefaultPaint(PaintingContext ctx, Point offset);

    bool DefaultHitTestChildren(BoxHitTestResult result, Point position);
}

public class RenderBoxContainerDefaultsMixin<TChild, TParentData>(RenderObject owner)
    : ContainerRenderObjectMixin<TChild, TParentData>(owner),
        IRenderBoxContainerDefaultsMixin<TChild, TParentData>
    where TChild : RenderBox
    where TParentData : ContainerBoxParentData<TChild>
{
    /// Paints each child by walking the child list forwards.
    ///
    /// See also:
    ///
    ///  * [defaultHitTestChildren], which implements hit-testing of the children
    ///    in a manner appropriate for this painting strategy.
    public void DefaultPaint(PaintingContext ctx, Point offset)
    {
        var child = FirstChild;

        while (child != null)
        {
            var childParentData = (TParentData)child.parentData!;

            ctx.PaintChild(child, childParentData.offset + offset);

            child = childParentData.nextSibling;
        }
    }

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        var child = LastChild;
        while (child != null)
        {
            var childParentData = (TParentData)child.parentData!;
            RenderBox hitChild = child;
            bool isHit = result.AddWithPaintOffset(
                childParentData.offset,
                position,
                (hitResult, transformed) => hitChild.HitTest(hitResult, transformed));
            if (isHit)
            {
                return true;
            }

            child = childParentData.previousSibling;
        }

        return false;
    }

    /// Returns the baseline of the first child that has one, offset by that
    /// child's position along the vertical axis.
    public double? DefaultComputeDistanceToFirstActualBaseline(TextBaseline baseline)
    {
        var child = FirstChild;
        while (child != null)
        {
            var childParentData = (TParentData)child.parentData!;
            double? result = child.GetDistanceToBaseline(baseline, onlyReal: true);
            if (result != null)
            {
                return result.Value + childParentData.offset.Y;
            }

            child = childParentData.nextSibling;
        }

        return null;
    }

    /// Returns the smallest offset baseline among the children that have one.
    public double? DefaultComputeDistanceToHighestActualBaseline(TextBaseline baseline)
    {
        double? minBaseline = null;
        var child = FirstChild;
        while (child != null)
        {
            var childParentData = (TParentData)child.parentData!;
            double? candidate = child.GetDistanceToBaseline(baseline, onlyReal: true);
            if (candidate != null)
            {
                double offsetCandidate = candidate.Value + childParentData.offset.Y;
                minBaseline = minBaseline == null
                    ? offsetCandidate
                    : Math.Min(minBaseline.Value, offsetCandidate);
            }

            child = childParentData.nextSibling;
        }

        return minBaseline;
    }
}
