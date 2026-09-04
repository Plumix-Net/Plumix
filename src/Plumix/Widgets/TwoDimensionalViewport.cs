using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/two_dimensional_viewport.dart

namespace Plumix.Widgets;

/// <summary>
/// Builds the child at <paramref name="vicinity"/>, or returns null when that position has no child.
/// </summary>
/// <remarks>Flutter's <c>TwoDimensionalIndexedWidgetBuilder</c>.</remarks>
public delegate Widget? TwoDimensionalIndexedWidgetBuilder(BuildContext context, ChildVicinity vicinity);

/// <summary>
/// A widget through which a portion of a larger, two-dimensional grid of children can be viewed.
/// </summary>
/// <remarks>
/// Flutter's <c>TwoDimensionalViewport</c>. Subclasses build a
/// <see cref="RenderTwoDimensionalViewport"/> and obtain its child manager with
/// <see cref="ChildManagerOf"/>.
/// </remarks>
public abstract class TwoDimensionalViewport : RenderObjectWidget
{
    protected TwoDimensionalViewport(
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        TwoDimensionalChildDelegate @delegate,
        Axis mainAxis,
        ScrollCacheExtent? scrollCacheExtent = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        if (verticalAxisDirection != AxisDirection.Down && verticalAxisDirection != AxisDirection.Up)
        {
            throw new AssertionError("TwoDimensionalViewport.verticalAxisDirection is not Axis.vertical.");
        }

        if (horizontalAxisDirection != AxisDirection.Left && horizontalAxisDirection != AxisDirection.Right)
        {
            throw new AssertionError("TwoDimensionalViewport.horizontalAxisDirection is not Axis.horizontal.");
        }

        ArgumentNullException.ThrowIfNull(verticalOffset);
        ArgumentNullException.ThrowIfNull(horizontalOffset);
        ArgumentNullException.ThrowIfNull(@delegate);

        VerticalOffset = verticalOffset;
        VerticalAxisDirection = verticalAxisDirection;
        HorizontalOffset = horizontalOffset;
        HorizontalAxisDirection = horizontalAxisDirection;
        MainAxis = mainAxis;
        ScrollCacheExtent = scrollCacheExtent;
        ClipBehavior = clipBehavior;
        Delegate = @delegate;
    }

    /// <summary>Which part of the content inside the viewport should be visible vertically.</summary>
    public ViewportOffset VerticalOffset { get; }

    /// <summary>The direction in which <see cref="VerticalOffset"/> increases.</summary>
    public AxisDirection VerticalAxisDirection { get; }

    /// <summary>Which part of the content inside the viewport should be visible horizontally.</summary>
    public ViewportOffset HorizontalOffset { get; }

    /// <summary>The direction in which <see cref="HorizontalOffset"/> increases.</summary>
    public AxisDirection HorizontalAxisDirection { get; }

    /// <summary>
    /// The major of the two axes, which decides the paint order of the viewport's children:
    /// <see cref="Axis.Vertical"/> paints row major, <see cref="Axis.Horizontal"/> column major.
    /// </summary>
    public Axis MainAxis { get; }

    /// <summary>How much content beyond the visible area is laid out.</summary>
    public ScrollCacheExtent? ScrollCacheExtent { get; }

    /// <summary>How the viewport clips content that overflows it.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>Supplies the children of the viewport.</summary>
    public TwoDimensionalChildDelegate Delegate { get; }

    /// <summary>
    /// The child manager the viewport's render object must be given, taken from the element behind
    /// <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Flutter writes <c>childManager: context as TwoDimensionalChildManager</c>, which C# cannot
    /// express because <see cref="BuildContext"/> is a struct that wraps the element rather than
    /// being it. Call this from <see cref="CreateRenderObject"/> instead.
    /// </remarks>
    public static ITwoDimensionalChildManager ChildManagerOf(BuildContext context)
    {
        return context.Owner as ITwoDimensionalChildManager
               ?? throw new InvalidOperationException(
                   "TwoDimensionalViewport.ChildManagerOf() was called with a context that is not a "
                   + "TwoDimensionalViewport's own element.");
    }

    internal override Element CreateElement() => new TwoDimensionalViewportElement(this);

    /// <inheritdoc />
    public abstract override RenderTwoDimensionalViewport CreateRenderObject(BuildContext context);

    /// <inheritdoc />
    public abstract override void UpdateRenderObject(BuildContext context, RenderObject renderObject);
}

/// <summary>
/// The element of a <see cref="TwoDimensionalViewport"/>, which is also the child manager its render
/// object drives during layout.
/// </summary>
/// <remarks>Flutter's private <c>_TwoDimensionalViewportElement</c>.</remarks>
internal sealed class TwoDimensionalViewportElement
    : RenderObjectElement, ITwoDimensionalChildManager, INotificationListener
{
    private Dictionary<ChildVicinity, Element> _vicinityToChild = [];
    private Dictionary<Key, Element> _keyToChild = [];

    // Used between StartLayout() and EndLayout() to compute the new values for the two maps above.
    private Dictionary<ChildVicinity, Element>? _newVicinityToChild;
    private Dictionary<Key, Element>? _newKeyToChild;

    public TwoDimensionalViewportElement(TwoDimensionalViewport widget) : base(widget)
    {
    }

    private TwoDimensionalViewport TypedWidget => (TwoDimensionalViewport)Widget;

    private RenderTwoDimensionalViewport TypedRenderObject => (RenderTwoDimensionalViewport)RenderObject!;

    private bool DebugIsDoingLayout => _newKeyToChild != null && _newVicinityToChild != null;

    /// <remarks>
    /// Flutter's <c>ViewportElementMixin.onNotification</c>: a viewport never handles a scroll
    /// notification, it only deepens it as it bubbles past.
    /// </remarks>
    bool INotificationListener.OnNotification(Notification notification)
    {
        if (notification is IViewportNotification viewportNotification)
        {
            viewportNotification.IncrementDepth();
        }

        return false;
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        // The child list is updated during layout, since only then is it known which children will
        // be visible.
        TypedRenderObject.MarkNeedsLayout(withDelegateRebuild: true);
    }

    /// <remarks>
    /// Flutter asserts <c>!_debugIsDoingLayout</c> here, because its <c>deactivateChild</c> never
    /// calls <c>forgetChild</c>. Plumix's <c>Element.DeactivateChild</c> does, and the child manager
    /// deactivates its leftovers from inside <see cref="EndLayout"/>, so the assert cannot hold.
    /// </remarks>
    internal override void ForgetChild(Element child)
    {
        base.ForgetChild(child);
        if (child.Slot is ChildVicinity vicinity)
        {
            _vicinityToChild.Remove(vicinity);
        }

        if (child.Widget.Key is { } key)
        {
            _keyToChild.Remove(key);
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        TypedRenderObject.InsertChild((RenderBox)child, (ChildVicinity)slot!);
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        TypedRenderObject.MoveChild((RenderBox)child, (ChildVicinity)oldSlot!, (ChildVicinity)newSlot!);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        TypedRenderObject.RemoveChild((RenderBox)child, (ChildVicinity)slot!);
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        foreach (Element child in _vicinityToChild.Values.ToList())
        {
            visitor(child);
        }
    }

    // Flutter also overrides `debugDescribeChildren` here, naming each node by its slot. Plumix's
    // `Element` is not `Diagnosticable`, so there is nothing to hook that onto; the equivalent dump
    // lives on `RenderTwoDimensionalViewport.DebugDescribeChildren`.

    /// <inheritdoc />
    public void StartLayout()
    {
        Debug.Assert(!DebugIsDoingLayout);
        _newVicinityToChild = [];
        _newKeyToChild = [];
    }

    /// <inheritdoc />
    public void BuildChild(ChildVicinity vicinity)
    {
        Debug.Assert(DebugIsDoingLayout);
        Owner!.BuildScope(this, () =>
        {
            Widget? newWidget = TypedWidget.Delegate.Build(new BuildContext(this), vicinity);
            if (newWidget is null)
            {
                return;
            }

            Element? oldElement = RetrieveOldElement(newWidget, vicinity);
            Element? newChild = UpdateChild(oldElement, newWidget, vicinity);
            Debug.Assert(newChild != null);
            // Ensure we are not overwriting an existing child.
            Debug.Assert(!_newVicinityToChild!.ContainsKey(vicinity));
            _newVicinityToChild![vicinity] = newChild!;
            if (newWidget.Key is { } key)
            {
                // Ensure we are not overwriting an existing key.
                Debug.Assert(!_newKeyToChild!.ContainsKey(key));
                _newKeyToChild![key] = newChild!;
            }
        });
    }

    private Element? RetrieveOldElement(Widget newWidget, ChildVicinity vicinity)
    {
        if (newWidget.Key is { } key)
        {
            if (_keyToChild.Remove(key, out Element? result))
            {
                if (result.Slot is ChildVicinity oldVicinity)
                {
                    _vicinityToChild.Remove(oldVicinity);
                }

                return result;
            }

            return null;
        }

        if (_vicinityToChild.TryGetValue(vicinity, out Element? potentialOldElement)
            && potentialOldElement.Widget.Key is null)
        {
            _vicinityToChild.Remove(vicinity);
            return potentialOldElement;
        }

        return null;
    }

    /// <inheritdoc />
    public void ReuseChild(ChildVicinity vicinity)
    {
        Debug.Assert(DebugIsDoingLayout);
        if (!_vicinityToChild.Remove(vicinity, out Element? elementToReuse))
        {
            throw new AssertionError(
                $"Expected to re-use an element at {vicinity}, but none was found.");
        }

        _newVicinityToChild![vicinity] = elementToReuse;
        if (elementToReuse.Widget.Key is { } key)
        {
            Debug.Assert(_keyToChild.ContainsKey(key));
            Debug.Assert(ReferenceEquals(_keyToChild[key], elementToReuse));
            _keyToChild.Remove(key);
            _newKeyToChild![key] = elementToReuse;
        }
    }

    /// <inheritdoc />
    public void EndLayout()
    {
        Debug.Assert(DebugIsDoingLayout);

        // Unmount all elements that have not been reused in this layout cycle.
        foreach (Element element in _vicinityToChild.Values.ToList())
        {
            if (element.Widget.Key is null)
            {
                // Keyed elements are handled by the loop below.
                UpdateChild(element, null, null);
            }
            else
            {
                Debug.Assert(_keyToChild.ContainsValue(element));
            }
        }

        foreach (Element element in _keyToChild.Values.ToList())
        {
            Debug.Assert(element.Widget.Key != null);
            UpdateChild(element, null, null);
        }

        _vicinityToChild = _newVicinityToChild!;
        _keyToChild = _newKeyToChild!;
        _newVicinityToChild = null;
        _newKeyToChild = null;
        Debug.Assert(!DebugIsDoingLayout);
    }
}
