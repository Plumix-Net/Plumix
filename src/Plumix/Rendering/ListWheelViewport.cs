using System.Diagnostics;
using Avalonia;
using Plumix.Gestures;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/list_wheel_viewport.dart

namespace Plumix.Rendering;

/// <summary>
/// A delegate used by <see cref="RenderListWheelViewport"/> to manage its children.
/// </summary>
/// <remarks>
/// Dart's abstract <c>ListWheelChildManager</c>. <see cref="RenderListWheelViewport"/> during layout
/// will ask the manager to create children that are visible in the viewport and remove those that
/// are not.
/// </remarks>
public interface IListWheelChildManager
{
    /// <summary>
    /// The maximum number of children that can be provided to <see cref="RenderListWheelViewport"/>.
    /// If non-null, the children will have index in the range <c>[0, childCount - 1]</c>. If null,
    /// then there's no explicit limits to the range of the children except that it has to be
    /// contiguous. If <see cref="ChildExistsAt"/> for a certain index returns false, that index is
    /// already past the limit.
    /// </summary>
    int? ChildCount { get; }

    /// <summary>
    /// Checks whether the delegate is able to provide a child widget at the given index. This
    /// function is not about whether the child at the given index is attached to the
    /// <see cref="RenderListWheelViewport"/> or not.
    /// </summary>
    bool ChildExistsAt(int index);

    /// <summary>
    /// Creates a new child at the given index and updates it to the
    /// <see cref="RenderListWheelViewport"/>. If no child corresponds to <paramref name="index"/>,
    /// then does nothing. It is possible to create children with negative indices.
    /// </summary>
    void CreateChild(int index, RenderBox? after);

    /// <summary>Removes the child element corresponding with the given RenderBox.</summary>
    void RemoveChild(RenderBox child);
}

/// <summary><see cref="IParentData"/> for use with <see cref="RenderListWheelViewport"/>.</summary>
public sealed class ListWheelParentData : ContainerBoxParentData<RenderBox>
{
    /// <summary>Index of this child in its parent's child list. This must be maintained by the
    /// <see cref="IListWheelChildManager"/>.</summary>
    public int? Index { get; set; }

    /// <summary>
    /// Transform applied to this child during painting. Can be used to find the local bounds of this
    /// child in the viewport, and then use it, for example, in hit testing. May be null if child was
    /// laid out, but not painted by the viewport, but may also be null if the child was not painted
    /// because it was not visible.
    /// </summary>
    public Matrix4? Transform { get; set; }
}

/// <summary>
/// Render, onto a wheel, a bigger sequential set of objects inside this viewport.
/// </summary>
/// <remarks>
/// Takes a scrollable set of fixed sized <see cref="RenderBox"/>es and renders them sequentially from
/// top down on a vertical scrolling axis. It starts with the first scrollable item in the center of
/// the main axis and ends with the last scrollable item in the center of the main axis. This is in
/// contrast to typical lists that start with the first scrollable item at the start of the main axis
/// and ends with the last scrollable item at the end of the main axis.
///
/// Instead of rendering its children on a flat plane, it renders them as if each child is broken
/// into its own plane and that plane is perpendicularly fixed onto a cylinder which rotates along the
/// scrolling axis. This class works in 3 coordinate systems: the <em>scrollable layout coordinates</em>
/// (an infinite one-dimensional axis where the first child is at 0 and each following child at
/// <c>itemExtent</c> further), the <em>untransformed plane's viewport painting coordinates</em>
/// (the flat plane after applying the scroll offset and the top margin that centers item 0), and the
/// <em>transformed cylindrical space viewport painting coordinates</em> (after projecting each child
/// onto the cylinder).
/// </remarks>
public class RenderListWheelViewport : RenderBox, IContainerRenderObjectMixin<RenderBox, ListWheelParentData>,
    IRenderObjectContainer, IRenderAbstractViewport
{
    private readonly ContainerRenderObjectMixin<RenderBox, ListWheelParentData> _mixin1;

    /// <summary>
    /// Creates a <see cref="RenderListWheelViewport"/> instance with only compulsory arguments and
    /// Dart's defaults for the rest.
    /// </summary>
    public RenderListWheelViewport(
        IListWheelChildManager childManager,
        ViewportOffset offset,
        double itemExtent,
        double diameterRatio = DefaultDiameterRatio,
        double perspective = DefaultPerspective,
        double offAxisFraction = 0,
        bool useMagnifier = false,
        double magnification = 1,
        double overAndUnderCenterOpacity = 1,
        double squeeze = 1,
        bool renderChildrenOutsideViewport = false,
        Clip clipBehavior = Clip.None,
        List<RenderBox>? children = null)
    {
        ArgumentNullException.ThrowIfNull(childManager);
        ArgumentNullException.ThrowIfNull(offset);
        if (!(diameterRatio > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(diameterRatio), DiameterRatioZeroMessage);
        }

        if (!(perspective > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(perspective));
        }

        if (!(perspective <= 0.01))
        {
            throw new ArgumentOutOfRangeException(nameof(perspective), PerspectiveTooHighMessage);
        }

        if (!(magnification > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(magnification));
        }

        if (!(overAndUnderCenterOpacity >= 0 && overAndUnderCenterOpacity <= 1))
        {
            throw new ArgumentOutOfRangeException(nameof(overAndUnderCenterOpacity));
        }

        if (!(squeeze > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(squeeze));
        }

        if (!(itemExtent > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (renderChildrenOutsideViewport && clipBehavior != Clip.None)
        {
            throw new ArgumentException(
                ClipBehaviorAndRenderChildrenOutsideViewportConflict,
                nameof(renderChildrenOutsideViewport));
        }

        _mixin1 = new ContainerRenderObjectMixin<RenderBox, ListWheelParentData>(this);
        ChildManager = childManager;
        _offset = offset;
        _diameterRatio = diameterRatio;
        _perspective = perspective;
        _offAxisFraction = offAxisFraction;
        _useMagnifier = useMagnifier;
        _magnification = magnification;
        _overAndUnderCenterOpacity = overAndUnderCenterOpacity;
        _itemExtent = itemExtent;
        _squeeze = squeeze;
        _renderChildrenOutsideViewport = renderChildrenOutsideViewport;
        _clipBehavior = clipBehavior;
        if (children != null)
        {
            AddAll(children);
        }
    }

    /// <summary>An arbitrary but aesthetically reasonable default value for
    /// <see cref="DiameterRatio"/>.</summary>
    public const double DefaultDiameterRatio = 2.0;

    /// <summary>An arbitrary but aesthetically reasonable default value for
    /// <see cref="Perspective"/>.</summary>
    public const double DefaultPerspective = 0.003;

    /// <summary>An error message to show when the provided <see cref="DiameterRatio"/> is zero.</summary>
    public const string DiameterRatioZeroMessage =
        "You can't set a diameterRatio of 0 or of a negative number. It would imply "
        + "a cylinder of 0 in diameter in which case nothing will be drawn.";

    /// <summary>An error message to show when the <see cref="Perspective"/> value is too high.</summary>
    public const string PerspectiveTooHighMessage =
        "A perspective too high will be clipped in the z-axis and therefore "
        + "not renderable. Value must be between 0 and 0.01.";

    /// <summary>
    /// An error message to show when <see cref="ClipBehavior"/> and
    /// <see cref="RenderChildrenOutsideViewport"/> are set to conflicting values.
    /// </summary>
    public const string ClipBehaviorAndRenderChildrenOutsideViewportConflict =
        "Cannot renderChildrenOutsideViewport and clip since children "
        + "rendered outside will be clipped anyway.";

    /// <summary>The delegate that manages the children of this object.</summary>
    public IListWheelChildManager ChildManager { get; }

    private ViewportOffset _offset;

    /// <summary>The associated ViewportOffset object for the viewport describing the part of the
    /// content inside that's visible.</summary>
    public ViewportOffset Offset
    {
        get => _offset;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, _offset))
            {
                return;
            }

            if (Attached)
            {
                _offset.RemoveListener(HasScrolled);
            }

            _offset = value;
            if (Attached)
            {
                _offset.AddListener(HasScrolled);
            }

            MarkNeedsLayout();
        }
    }

    private double _diameterRatio;

    /// <summary>
    /// A ratio between the diameter of the cylinder and the viewport's size in the main axis.
    /// A value of 1 means the cylinder has the same diameter as the viewport's size. A value smaller
    /// than 1 means items at the edges of the cylinder are entirely contained inside the viewport. A
    /// value larger than 1 means angles less than ±π/2 from the center of the cylinder are visible.
    /// Must be a positive number.
    /// </summary>
    public double DiameterRatio
    {
        get => _diameterRatio;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), DiameterRatioZeroMessage);
            }

            if (value == _diameterRatio)
            {
                return;
            }

            _diameterRatio = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    private double _perspective;

    /// <summary>
    /// Perspective of the cylindrical projection. A number between 0 and 0.01 where 0 means looking
    /// at the cylinder from infinitely far with an infinitely small field of view and 1 means
    /// looking at the cylinder from infinitely close with an infinitely large field of view.
    /// </summary>
    public double Perspective
    {
        get => _perspective;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (!(value <= 0.01))
            {
                throw new ArgumentOutOfRangeException(nameof(value), PerspectiveTooHighMessage);
            }

            if (value == _perspective)
            {
                return;
            }

            _perspective = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    private double _offAxisFraction;

    /// <summary>
    /// How much the wheel is horizontally off-center, as a fraction of its width. This property
    /// creates the visual effect of looking at a vertical wheel from its side where its vanishing
    /// points at the edge curves to one side instead of looking at the wheel head-on.
    /// </summary>
    public double OffAxisFraction
    {
        get => _offAxisFraction;
        set
        {
            if (value == _offAxisFraction)
            {
                return;
            }

            _offAxisFraction = value;
            MarkNeedsPaint();
        }
    }

    private bool _useMagnifier;

    /// <summary>Whether to use the magnifier for the center item of the wheel.</summary>
    public bool UseMagnifier
    {
        get => _useMagnifier;
        set
        {
            if (value == _useMagnifier)
            {
                return;
            }

            _useMagnifier = value;
            MarkNeedsPaint();
        }
    }

    private double _magnification;

    /// <summary>
    /// The zoomed-in rate of the magnifier, if it is used. The default value is 1.0, which will not
    /// change anything. If the value is greater than 1.0, the center item will be zoomed in by that
    /// rate, and it will also be rendered as flat, not cylindrical like the rest of the list. The
    /// item will be zoomed-out if magnification is less than 1.0. Must be positive.
    /// </summary>
    public double Magnification
    {
        get => _magnification;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == _magnification)
            {
                return;
            }

            _magnification = value;
            MarkNeedsPaint();
        }
    }

    private double _overAndUnderCenterOpacity;

    /// <summary>
    /// The opacity value that will be applied to the wheel that appears below and above the
    /// magnifier. The default value is 1.0, which will not change anything. Must be greater than or
    /// equal to 0, and less than or equal to 1.
    /// </summary>
    public double OverAndUnderCenterOpacity
    {
        get => _overAndUnderCenterOpacity;
        set
        {
            if (!(value >= 0 && value <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == _overAndUnderCenterOpacity)
            {
                return;
            }

            _overAndUnderCenterOpacity = value;
            MarkNeedsPaint();
        }
    }

    private double _itemExtent;

    /// <summary>The size of the children along the main axis. Children
    /// <see cref="RenderBox"/>es will be given the <see cref="BoxConstraints"/> of this exact size.
    /// Must be a positive number.</summary>
    public double ItemExtent
    {
        get => _itemExtent;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == _itemExtent)
            {
                return;
            }

            _itemExtent = value;
            MarkNeedsLayout();
        }
    }

    private double _squeeze;

    /// <summary>
    /// The angular compactness of the children on the wheel. This denotes a ratio of the number of
    /// children on the wheel vs the number of children that would fit on a flat list of equivalent
    /// size, assuming <see cref="DiameterRatio"/> of 1. For instance, if this RenderListWheelViewport
    /// has a height of 100px and <see cref="ItemExtent"/> is 20px, 5 items would fit on an equivalent
    /// flat list. With a <see cref="Squeeze"/> of 1, 5 items would also be shown in the
    /// RenderListWheelViewport. With a <see cref="Squeeze"/> of 2, 10 items would be shown in the
    /// RenderListWheelViewport. Changing this value will change the number of children built and
    /// shown inside the wheel. Must be a positive number.
    /// </summary>
    public double Squeeze
    {
        get => _squeeze;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == _squeeze)
            {
                return;
            }

            _squeeze = value;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    private bool _renderChildrenOutsideViewport;

    /// <summary>
    /// Whether to paint children inside the viewport only. If false, every child will be painted.
    /// However the <see cref="Scrollable"/> is still the size of the viewport and detects gestures
    /// inside only. Defaults to false. Cannot be true if <see cref="ClipBehavior"/> is not
    /// <see cref="Clip.None"/> since children outside the viewport will be clipped, and therefore
    /// cannot render children outside the viewport.
    /// </summary>
    public bool RenderChildrenOutsideViewport
    {
        get => _renderChildrenOutsideViewport;
        set
        {
            if (RenderChildrenOutsideViewport && ClipBehavior != Clip.None)
            {
                throw new InvalidOperationException(ClipBehaviorAndRenderChildrenOutsideViewportConflict);
            }

            if (value == _renderChildrenOutsideViewport)
            {
                return;
            }

            _renderChildrenOutsideViewport = value;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    private Clip _clipBehavior;

    /// <summary>The content will be clipped (or not) according to this option. Defaults to
    /// <see cref="Clip.HardEdge"/> in the widget layer.</summary>
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (value != _clipBehavior)
            {
                _clipBehavior = value;
                MarkNeedsPaint();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    private void HasScrolled()
    {
        MarkNeedsLayout();
        MarkNeedsSemanticsUpdate();
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not ListWheelParentData)
        {
            child.parentData = new ListWheelParentData();
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _offset.AddListener(HasScrolled);
    }

    protected override void OnDetach()
    {
        _offset.RemoveListener(HasScrolled);
        base.OnDetach();
    }

    public override bool IsRepaintBoundary => true;

    /// <summary>Main axis length in the untransformed plane.</summary>
    private double ViewportExtent
    {
        get
        {
            Debug.Assert(HasSize);
            return Size.Height;
        }
    }

    /// <summary>Main axis scroll extent in the <b>scrollable layout coordinates</b> that puts the
    /// first item in the center.</summary>
    private double MinEstimatedScrollExtent
    {
        get
        {
            Debug.Assert(HasSize);
            if (ChildManager.ChildCount == null)
            {
                return double.NegativeInfinity;
            }

            return 0.0;
        }
    }

    /// <summary>Main axis scroll extent in the <b>scrollable layout coordinates</b> that puts the
    /// last item in the center.</summary>
    private double MaxEstimatedScrollExtent
    {
        get
        {
            Debug.Assert(HasSize);
            if (ChildManager.ChildCount == null)
            {
                return double.PositiveInfinity;
            }

            return Math.Max(0.0, (ChildManager.ChildCount!.Value - 1) * _itemExtent);
        }
    }

    /// <summary>
    /// Scroll extent distance in the untransformed plane between the center position in the
    /// viewport and the top position in the viewport. It's also the distance in the untransformed
    /// plane that children's painting is offset by with respect to those children's
    /// <see cref="BoxParentData.offset"/>.
    /// </summary>
    private double TopScrollMarginExtent
    {
        get
        {
            Debug.Assert(HasSize);
            // Consider adding alignment options other than center.
            return (-Size.Height / 2.0) + (_itemExtent / 2.0);
        }
    }

    /// <summary>Transforms a <b>scrollable layout coordinates</b>' y position to the
    /// <b>untransformed plane's viewport painting coordinates</b>' y position given the current
    /// scroll offset.</summary>
    private double GetUntransformedPaintingCoordinateY(double layoutCoordinateY)
    {
        return layoutCoordinateY - TopScrollMarginExtent - Offset.Pixels;
    }

    /// <summary>
    /// Given the _diameterRatio, return the largest absolute angle of the item at the edge of the
    /// portion of the visible cylinder. For a _diameterRatio of 1 or less than 1 (i.e. the viewport
    /// is bigger than the cylinder diameter), this value reaches and clips at pi / 2. When the
    /// center of the cylinder is at 0.0 and the viewport is at [-1.0, 1.0] in cross axis units,
    /// this angle is asin(1.0 / _diameterRatio).
    /// </summary>
    private double MaxVisibleRadian
    {
        get
        {
            if (_diameterRatio < 1.0)
            {
                return Math.PI / 2.0;
            }

            return Math.Asin(1.0 / _diameterRatio);
        }
    }

    private double GetIntrinsicCrossAxis(Func<RenderBox, double> childSize)
    {
        double extent = 0.0;
        RenderBox? child = FirstChild;
        while (child != null)
        {
            extent = Math.Max(extent, childSize(child));
            child = ChildAfter(child);
        }

        return extent;
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return GetIntrinsicCrossAxis(child => child.GetMinIntrinsicWidth(height));
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return GetIntrinsicCrossAxis(child => child.GetMaxIntrinsicWidth(height));
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (ChildManager.ChildCount == null)
        {
            return 0.0;
        }

        return ChildManager.ChildCount!.Value * _itemExtent;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        if (ChildManager.ChildCount == null)
        {
            return 0.0;
        }

        return ChildManager.ChildCount!.Value * _itemExtent;
    }

    /// <summary>Dart's <c>sizedByParent</c>: this viewport always takes its constraints' biggest
    /// size, which is what its <see cref="ComputeDryLayout"/> reports.</summary>
    protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Biggest;

    /// <summary>Gets the index of a child by looking at its <see cref="ListWheelParentData"/>.</summary>
    public int IndexOf(RenderBox child)
    {
        var childParentData = (ListWheelParentData)child.parentData!;
        Debug.Assert(childParentData.Index != null);
        return childParentData.Index!.Value;
    }

    /// <summary>Returns the index of the child at the given offset.</summary>
    public int ScrollOffsetToIndex(double scrollOffset) => (int)Math.Floor(scrollOffset / ItemExtent);

    /// <summary>Returns the scroll offset of the child with the given index.</summary>
    public double IndexToScrollOffset(int index) => index * ItemExtent;

    private void CreateChild(int index, RenderBox? after = null)
    {
        InvokeLayoutCallback<BoxConstraints>(constraints =>
        {
            Debug.Assert(constraints.Equals(Constraints));
            ChildManager.CreateChild(index, after);
        }, Constraints);
    }

    private void DestroyChild(RenderBox child)
    {
        InvokeLayoutCallback<BoxConstraints>(constraints =>
        {
            Debug.Assert(constraints.Equals(Constraints));
            ChildManager.RemoveChild(child);
        }, Constraints);
    }

    private void LayoutChild(RenderBox child, BoxConstraints constraints, int index)
    {
        child.Layout(constraints, parentUsesSize: true);
        var childParentData = (ListWheelParentData)child.parentData!;
        // Centers the child horizontally.
        double crossPosition = (Size.Width / 2.0) - (child.Size.Width / 2.0);
        childParentData.offset = new Point(crossPosition, IndexToScrollOffset(index));
    }

    /// <summary>
    /// Performs layout based on how <see cref="ChildManager"/> provides children. From the current
    /// scroll offset, the minimum index and maximum index that is visible in the viewport can be
    /// calculated. The index range of the currently active children can also be acquired by looking
    /// directly at the current child list. This function has to modify the current index range to
    /// match the target index range by removing children that are no longer visible and creating
    /// those that are visible but not yet provided by <see cref="ChildManager"/>.
    /// </summary>
    protected override void PerformLayout()
    {
        // Dart's sizedByParent: performResize sets the size from the constraints alone.
        Size = Constraints.Biggest;
        Offset.ApplyViewportDimension(ViewportExtent);
        // Apply the content dimensions first if it has a known number of children (i.e. childCount
        // is not null). This is because a computed maxScrollExtent may be less than the current
        // pixel offset, which would cause the offset to be corrected before deciding which children
        // to show. See flutter/flutter#42462.
        if (ChildManager.ChildCount != null)
        {
            Offset.ApplyContentDimensions(MinEstimatedScrollExtent, MaxEstimatedScrollExtent);
        }

        double visibleHeight = Size.Height * _squeeze;
        // If renderChildrenOutsideViewport is true, we spawn extra children by doubling the
        // visibility range, those that are in the backside of the cylinder won't be painted anyway.
        if (RenderChildrenOutsideViewport)
        {
            visibleHeight *= 2;
        }

        double firstVisibleOffset = Offset.Pixels + (_itemExtent / 2) - (visibleHeight / 2);
        double lastVisibleOffset = firstVisibleOffset + visibleHeight;

        // The index range that we want to spawn children. We find indexes that are in the interval
        // [firstVisibleOffset, lastVisibleOffset).
        int targetFirstIndex = ScrollOffsetToIndex(firstVisibleOffset);
        int targetLastIndex = ScrollOffsetToIndex(lastVisibleOffset);
        // Because we exclude lastVisibleOffset, if there's a new child starting at that offset, it
        // is removed.
        if (targetLastIndex * _itemExtent == lastVisibleOffset)
        {
            targetLastIndex--;
        }

        // Validates the target index range.
        while (!ChildManager.ChildExistsAt(targetFirstIndex) && targetFirstIndex <= targetLastIndex)
        {
            targetFirstIndex++;
        }

        while (!ChildManager.ChildExistsAt(targetLastIndex) && targetFirstIndex <= targetLastIndex)
        {
            targetLastIndex--;
        }

        // If it turns out there's no children to layout, we remove old children and return.
        if (targetFirstIndex > targetLastIndex)
        {
            while (FirstChild != null)
            {
                DestroyChild(FirstChild);
            }

            return;
        }

        // Now there are 2 cases:
        //  - The target index range and our current index range have intersection: We shorten and
        //    extend our current child list so that the two lists match. Most of the time we are in
        //    this case.
        //  - The target list and our current child list have no intersection: We first remove all
        //    children and then add one child from the target list => this case becomes the other case.

        // Case when there is no intersection.
        if (ChildCount > 0
            && (IndexOf(FirstChild!) > targetLastIndex || IndexOf(LastChild!) < targetFirstIndex))
        {
            while (FirstChild != null)
            {
                DestroyChild(FirstChild);
            }
        }

        BoxConstraints childConstraints = Constraints with
        {
            MinHeight = _itemExtent,
            MaxHeight = _itemExtent,
            MinWidth = 0.0,
        };

        // If there is no child at this stage, we add the first one that is in target range.
        if (ChildCount == 0)
        {
            CreateChild(targetFirstIndex);
            LayoutChild(FirstChild!, childConstraints, targetFirstIndex);
        }

        int currentFirstIndex = IndexOf(FirstChild!);
        int currentLastIndex = IndexOf(LastChild!);

        // Remove all unnecessary children by shortening the current child list, in both directions.
        while (currentFirstIndex < targetFirstIndex)
        {
            DestroyChild(FirstChild!);
            currentFirstIndex++;
        }

        while (currentLastIndex > targetLastIndex)
        {
            DestroyChild(LastChild!);
            currentLastIndex--;
        }

        // Relayout all active children.
        RenderBox? child = FirstChild;
        int index = currentFirstIndex;
        while (child != null)
        {
            LayoutChild(child, childConstraints, index++);
            child = ChildAfter(child);
        }

        // Spawning new children that are actually visible but not in child list yet.
        while (currentFirstIndex > targetFirstIndex)
        {
            CreateChild(currentFirstIndex - 1);
            LayoutChild(FirstChild!, childConstraints, --currentFirstIndex);
        }

        while (currentLastIndex < targetLastIndex)
        {
            CreateChild(currentLastIndex + 1, after: LastChild);
            LayoutChild(LastChild!, childConstraints, ++currentLastIndex);
        }

        // Applying content dimensions bases on how the childManager builds widgets: if it is
        // available to provide a child just out of target range, then we don't know whether there's
        // a limit yet, and set the dimension to the max. Otherwise, we set the dimension limited to
        // our target range.
        double minScrollExtent = ChildManager.ChildExistsAt(targetFirstIndex - 1)
            ? MinEstimatedScrollExtent
            : IndexToScrollOffset(targetFirstIndex);
        double maxScrollExtent = ChildManager.ChildExistsAt(targetLastIndex + 1)
            ? MaxEstimatedScrollExtent
            : IndexToScrollOffset(targetLastIndex);
        Offset.ApplyContentDimensions(minScrollExtent, maxScrollExtent);
    }

    private bool ShouldClipAtCurrentOffset()
    {
        double highestUntransformedPaintY = GetUntransformedPaintingCoordinateY(0.0);
        return highestUntransformedPaintY < 0.0
               || Size.Height < highestUntransformedPaintY + MaxEstimatedScrollExtent + _itemExtent;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (ChildCount > 0)
        {
            if (ShouldClipAtCurrentOffset() && ClipBehavior != Clip.None)
            {
                context.PushClipRect(
                    new Rect(offset, Size),
                    clippedContext => PaintVisibleChildren(clippedContext, offset),
                    ClipBehavior);
            }
            else
            {
                PaintVisibleChildren(context, offset);
            }
        }
    }

    /// <summary>Paints all children visible in the current viewport.</summary>
    private void PaintVisibleChildren(PaintingContext context, Point offset)
    {
        // The magnifier cannot be turned off if the opacity is less than 1.0.
        if (OverAndUnderCenterOpacity >= 1)
        {
            PaintAllChildren(context, offset);
            return;
        }

        // In order to reduce the number of opacity layers, we first paint all partially opaque
        // children, then finally paint the fully opaque children.
        double alpha = Math.Round(OverAndUnderCenterOpacity * 255, MidpointRounding.AwayFromZero);
        context.PushOpacity(alpha / 255.0, opacityContext => PaintAllChildren(opacityContext, offset, center: false));
        PaintAllChildren(context, offset, center: true);
    }

    private void PaintAllChildren(PaintingContext context, Point offset, bool? center = null)
    {
        RenderBox? childToPaint = FirstChild;
        while (childToPaint != null)
        {
            var childParentData = (ListWheelParentData)childToPaint.parentData!;
            PaintTransformedChild(childToPaint, context, offset, childParentData.offset, center: center);
            childToPaint = childParentData.nextSibling;
        }
    }

    /// <summary>
    /// Takes in a child with a <b>scrollable layout offset</b> and paints it in the <b>transformed
    /// cylindrical space viewport painting coordinates</b>. The value of <paramref name="center"/> is
    /// passed through to <see cref="PaintChildWithMagnifier"/> only if the magnifier is enabled
    /// and/or opacity is less than 1.0.
    /// </summary>
    private void PaintTransformedChild(
        RenderBox child,
        PaintingContext context,
        Point offset,
        Point layoutOffset,
        bool? center)
    {
        Point untransformedPaintingCoordinates =
            offset + new Point(layoutOffset.X, GetUntransformedPaintingCoordinateY(layoutOffset.Y));

        // Get child's center as a fraction of the viewport's height.
        double fractionalY = (untransformedPaintingCoordinates.Y + (_itemExtent / 2.0)) / Size.Height;
        double angle = -(fractionalY - 0.5) * 2.0 * MaxVisibleRadian / Squeeze;
        // Don't paint the backside of the cylinder when renderChildrenOutsideViewport is true. Otherwise,
        // only children within a 180° visible cylinder are visible.
        if (angle > Math.PI / 2.0 || angle < -Math.PI / 2.0 || double.IsNaN(angle))
        {
            return;
        }

        Matrix4 transform = MatrixUtils.CreateCylindricalProjectionTransform(
            radius: Size.Height * _diameterRatio / 2.0,
            angle: angle,
            perspective: _perspective);

        // Offset that helps painting everything in the center (e.g. angle = 0).
        var offsetToCenter = new Point(untransformedPaintingCoordinates.X, -TopScrollMarginExtent);

        bool shouldApplyOffCenterDim = OverAndUnderCenterOpacity < 1;
        if (UseMagnifier || shouldApplyOffCenterDim)
        {
            PaintChildWithMagnifier(
                context,
                offset,
                child,
                transform,
                offsetToCenter,
                untransformedPaintingCoordinates,
                center: center);
        }
        else
        {
            Debug.Assert(center == null);
            PaintChildCylindrically(context, offset, child, transform, offsetToCenter);
        }
    }

    /// <summary>
    /// Paint child with the magnifier active - the child will be rendered differently if it
    /// intersects with the magnifier. `center` controls how items that partially intersect the center
    /// magnifier are rendered. If `center` is false, items are only painted cylindrically. If
    /// `center` is true, only the clipped magnifier items are painted. If `center` is null, partially
    /// intersecting items are painted both as the magnifier and cylindrical.
    /// </summary>
    private void PaintChildWithMagnifier(
        PaintingContext context,
        Point offset,
        RenderBox child,
        Matrix4 cylindricalTransform,
        Point offsetToCenter,
        Point untransformedPaintingCoordinates,
        bool? center)
    {
        double magnifierTopLinePosition = (Size.Height / 2) - (_itemExtent * _magnification / 2);
        double magnifierBottomLinePosition = (Size.Height / 2) + (_itemExtent * _magnification / 2);

        bool isAfterMagnifierTopLine = untransformedPaintingCoordinates.Y
                                       >= magnifierTopLinePosition - (_itemExtent * _magnification);
        bool isBeforeMagnifierBottomLine = untransformedPaintingCoordinates.Y <= magnifierBottomLinePosition;

        var centerRect = new Rect(0.0, magnifierTopLinePosition, Size.Width, _itemExtent * _magnification);
        var topHalfRect = new Rect(0.0, 0.0, Size.Width, magnifierTopLinePosition);
        var bottomHalfRect = new Rect(0.0, magnifierBottomLinePosition, Size.Width, magnifierTopLinePosition);
        // Some part of the child is in the center magnifier.
        bool inCenter = isAfterMagnifierTopLine && isBeforeMagnifierBottomLine;

        if ((center == null || center == true) && inCenter)
        {
            // Clipping the part in the center.
            context.PushClipRect(centerRect.Translate(new Vector(offset.X, offset.Y)), clippedContext =>
            {
                // Paint the ordinary child in the middle of the magnifier at its magnified size.
                clippedContext.PushTransform(WithOffset(MagnifyTransform(), offset), magnifiedContext =>
                {
                    magnifiedContext.PaintChild(child, offset + untransformedPaintingCoordinates);
                });
            });
        }

        // Clipping the part in either the top-half or bottom-half of the wheel.
        if ((center == null || center == false) && inCenter)
        {
            Rect halfRect = untransformedPaintingCoordinates.Y <= magnifierTopLinePosition
                ? topHalfRect
                : bottomHalfRect;
            context.PushClipRect(halfRect.Translate(new Vector(offset.X, offset.Y)), clippedContext =>
            {
                PaintChildCylindrically(clippedContext, offset, child, cylindricalTransform, offsetToCenter);
            });
        }

        if ((center == null || center == false) && !inCenter)
        {
            PaintChildCylindrically(context, offset, child, cylindricalTransform, offsetToCenter);
        }
    }

    /// <summary>
    /// Paint the child cylindrically at given offset. `offset` is the offset of the viewport,
    /// `child` is the child to be painted, `cylindricalTransform` is the transform of the cylinder,
    /// `offsetToCenter` is the offset of the child to the center of the wheel.
    /// </summary>
    private void PaintChildCylindrically(
        PaintingContext context,
        Point offset,
        RenderBox child,
        Matrix4 cylindricalTransform,
        Point offsetToCenter)
    {
        Point paintOriginOffset = offset + offsetToCenter;

        // Paint child cylindrically, without [overAndUnderCenterOpacity].
        context.PushTransform(
            WithOffset(CenterOriginTransform(cylindricalTransform), offset),
            transformedContext => transformedContext.PaintChild(child, paintOriginOffset));

        // Save the final transform that accounts both for the offset and cylindrical transform.
        Matrix4 transform = CenterOriginTransform(cylindricalTransform);
        transform.TranslateByDouble(paintOriginOffset.X, paintOriginOffset.Y, 0, 1);
        ((ListWheelParentData)child.parentData!).Transform = transform;
    }

    /// <summary>Return the Matrix4 transformation that would zoom in content in the magnified area.</summary>
    private Matrix4 MagnifyTransform()
    {
        Matrix4 magnify = Matrix4.Identity();
        magnify.TranslateByDouble(Size.Width * (-_offAxisFraction + 0.5), Size.Height / 2, 0, 1);
        magnify.ScaleByDouble(_magnification, _magnification, _magnification, 1.0);
        magnify.TranslateByDouble(-Size.Width * (-_offAxisFraction + 0.5), -Size.Height / 2, 0, 1);
        return magnify;
    }

    /// <summary>Apply incoming transformation with the transformation's origin at the viewport's
    /// center or horizontally off to the side based on offAxisFraction.</summary>
    private Matrix4 CenterOriginTransform(Matrix4 originalMatrix)
    {
        Matrix4 result = Matrix4.Identity();
        Point centerOriginTranslation = Alignment.Center.AlongSize(Size);
        result.TranslateByDouble(
            centerOriginTranslation.X * ((-_offAxisFraction * 2) + 1),
            centerOriginTranslation.Y,
            0,
            1);
        result.Multiply(originalMatrix);
        result.TranslateByDouble(
            -centerOriginTranslation.X * ((-_offAxisFraction * 2) + 1),
            -centerOriginTranslation.Y,
            0,
            1);
        return result;
    }

    /// <summary>
    /// Flutter's <c>PaintingContext.pushTransform(needsCompositing, offset, transform, painter)</c>
    /// applies <paramref name="transform"/> around <paramref name="offset"/>; Plumix's context takes
    /// the effective matrix, so the translation sandwich is folded in here.
    /// </summary>
    private static Matrix4 WithOffset(Matrix4 transform, Point offset)
    {
        Matrix4 effectiveTransform = Matrix4.TranslationValues(offset.X, offset.Y, 0.0);
        effectiveTransform.Multiply(transform);
        effectiveTransform.TranslateByDouble(-offset.X, -offset.Y, 0.0, 1.0);
        return effectiveTransform;
    }

    private static bool DebugAssertValidHitTestOffsets(string context, Point offset1, Point offset2)
    {
        if (offset1 != offset2)
        {
            throw new InvalidOperationException(
                $"{context} - hit test expected values didn't match: {offset1} != {offset2}");
        }

        return true;
    }

    /// <summary>
    /// This returns the matrices relative to the <b>untransformed plane's viewport painting
    /// coordinates</b> system.
    /// </summary>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        var parentData = (ListWheelParentData)child.parentData!;
        Matrix4? paintTransform = parentData.Transform;
        if (paintTransform != null)
        {
            transform.Multiply(paintTransform);
        }
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return ShouldClipAtCurrentOffset() ? new Rect(default(Point), Size) : null;
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        RenderBox? child = LastChild;
        while (child != null)
        {
            var childParentData = (ListWheelParentData)child.parentData!;
            Matrix4? transform = childParentData.Transform;
            // Skip not painted children
            if (transform != null)
            {
                RenderBox current = child;
                bool isHit = result.AddWithPaintTransform(
                    transform: transform,
                    position: position,
                    hitTest: (hitResult, transformed) =>
                    {
                        Debug.Assert(DebugValidateHitTestOffsets(transform, transformed, position));
                        return current.HitTest(hitResult, transformed);
                    });
                if (isHit)
                {
                    return true;
                }
            }

            child = childParentData.previousSibling;
        }

        return false;
    }

    private static bool DebugValidateHitTestOffsets(Matrix4 transform, Point transformed, Point position)
    {
        Matrix4? inverted = Matrix4.TryInvert(PointerEventUtils.RemovePerspectiveTransform(transform));
        if (inverted == null)
        {
            return DebugAssertValidHitTestOffsets("Null inverted transform", transformed, position);
        }

        return DebugAssertValidHitTestOffsets(
            "MatrixUtils.transformPoint",
            transformed,
            MatrixUtils.TransformPoint(inverted, position));
    }

    /// <inheritdoc />
    /// <remarks>`alignment` is ignored: the wheel always centers the revealed child. Only vertical
    /// scrolling is supported, so `axis` is not consulted either.</remarks>
    public RevealedOffset GetOffsetToReveal(
        RenderObject target,
        double alignment,
        Rect? rect = null,
        Axis? axis = null)
    {
        // `target` is only fully revealed when in the selected/center position. Therefore, this
        // method always returns the offset that shows `target` in the center position, which is the
        // same offset for all `alignment` values.
        rect ??= target.PaintBounds;

        // `child` will be the last RenderObject before the viewport when walking up from `target`.
        RenderObject child = target;
        while (!ReferenceEquals(child.Parent, this))
        {
            child = child.Parent!;
        }

        var parentData = (ListWheelParentData)child.parentData!;
        double targetOffset = parentData.offset.Y; // the so-called "centerPosition"

        Matrix4 transform = target.GetTransformTo(child);
        Rect bounds = MatrixUtils.TransformRect(transform, rect.Value);
        Rect targetRect = bounds.Translate(new Vector(0.0, (Size.Height - ItemExtent) / 2));

        return new RevealedOffset(targetOffset, targetRect);
    }

    public override void ShowOnScreen(
        RenderObject? descendant = null,
        Rect? rect = null,
        TimeSpan duration = default,
        Curve? curve = null)
    {
        if (descendant != null)
        {
            // Shows the descendant in the selected/center position.
            RevealedOffset revealedOffset = GetOffsetToReveal(descendant, 0.5, rect: rect);
            if (duration == TimeSpan.Zero)
            {
                Offset.JumpTo(revealedOffset.Offset);
            }
            else
            {
                _ = Offset.AnimateTo(revealedOffset.Offset, duration, curve ?? Curves.Ease);
            }

            rect = revealedOffset.Rect;
        }

        base.ShowOnScreen(rect: rect, duration: duration, curve: curve);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    #region Mixins

    public int ChildCount => _mixin1.ChildCount;

    public RenderBox? FirstChild => _mixin1.FirstChild;

    public RenderBox? LastChild => _mixin1.LastChild;

    public void Insert(RenderBox child, RenderBox? after = null) => _mixin1.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _mixin1.Move(child, after);

    public void Remove(RenderBox child) => _mixin1.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    public void AddAll(List<RenderBox> children) => _mixin1.AddAll(children);

    public RenderBox? ChildBefore(RenderBox child) => _mixin1.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _mixin1.ChildAfter(child);

    #endregion
}
