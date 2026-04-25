using System.Diagnostics;
using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart (approximate)

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
    public void Detach()
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

    void AddAll(List<TChild> children);

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
        owner.AdoptChild(child);

        _insertIntoChildList(child, after: after);
    }

    /// Append child to the end of this render object's child list.
    public void Add(TChild child)
    {
        Insert(child, after: _lastChild);
    }

    /// Add all the children to the end of this render object's child list.
    public void AddAll(List<TChild> children)
    {
        foreach (var child in children)
            Add(child);
    }

    public void Move(TChild child, TChild? after = null)
    {
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

        _childCount += 1;

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
        var childParentData = (TParentData)child.parentData!;

        if (childParentData.previousSibling == null)
        {
            _firstChild = childParentData.nextSibling;
        }
        else
        {
            var prevParentData = (TParentData)childParentData.previousSibling.parentData!;
            prevParentData.nextSibling = childParentData.nextSibling;
        }

        if (childParentData.nextSibling == null)
        {
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
            var transformedPosition = position - childParentData.offset;
            if (child.HitTest(result, transformedPosition))
            {
                return true;
            }

            child = childParentData.previousSibling;
        }

        return false;
    }
}
