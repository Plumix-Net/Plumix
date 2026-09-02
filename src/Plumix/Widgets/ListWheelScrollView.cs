using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/list_wheel_scroll_view.dart

namespace Plumix.Widgets;

/// <summary>Signature for a function that creates a widget for a given index, or null.</summary>
/// <remarks>Dart's <c>NullableIndexedWidgetBuilder</c> (widgets/framework.dart).</remarks>
public delegate Widget? NullableIndexedWidgetBuilder(BuildContext context, int index);

/// <summary>Determines when a <see cref="ListWheelScrollView"/> reports a change in its selected
/// item through <see cref="ListWheelScrollView.OnSelectedItemChanged"/>.</summary>
public enum ChangeReportingBehavior
{
    /// <summary>Report the selected item only when scrolling stops.</summary>
    OnScrollEnd,

    /// <summary>Report the selected item on every scroll update.</summary>
    OnScrollUpdate,
}

/// <summary>
/// A delegate that supplies children for <see cref="ListWheelScrollView"/>.
/// </summary>
/// <remarks>
/// <see cref="ListWheelScrollView"/> lazily constructs its children during layout to avoid creating
/// more children than are visible through the <see cref="Viewport"/>. This delegate is responsible
/// for providing children to <see cref="ListWheelScrollView"/> during that stage.
/// </remarks>
public abstract class ListWheelChildDelegate
{
    /// <summary>Return the child at the given index. If the child at the given index does not
    /// exist, return null.</summary>
    public abstract Widget? Build(BuildContext context, int index);

    /// <summary>Returns an estimate of the number of children this delegate will build.</summary>
    public abstract int? EstimatedChildCount { get; }

    /// <summary>Returns the true index for a child built at a given index. Defaults to
    /// <paramref name="index"/>.</summary>
    public virtual int TrueIndexOf(int index) => index;

    /// <summary>Called to check whether this and the old delegate are actually 'different', so
    /// that the caller can decide to rebuild or not.</summary>
    public abstract bool ShouldRebuild(ListWheelChildDelegate oldDelegate);
}

/// <summary>
/// A delegate that supplies children for <see cref="ListWheelScrollView"/> using an explicit list.
/// </summary>
public class ListWheelChildListDelegate : ListWheelChildDelegate
{
    public ListWheelChildListDelegate(IReadOnlyList<Widget> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        Children = children;
    }

    /// <summary>The list containing all children that can be supplied.</summary>
    public IReadOnlyList<Widget> Children { get; }

    public override int? EstimatedChildCount => Children.Count;

    public override Widget? Build(BuildContext context, int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            return null;
        }

        return new IndexedSemantics(index: index, child: Children[index]);
    }

    public override bool ShouldRebuild(ListWheelChildDelegate oldDelegate) =>
        !ReferenceEquals(Children, ((ListWheelChildListDelegate)oldDelegate).Children);
}

/// <summary>
/// A delegate that supplies infinite children by looping an explicit list for
/// <see cref="ListWheelScrollView"/>.
/// </summary>
public class ListWheelChildLoopingListDelegate : ListWheelChildDelegate
{
    public ListWheelChildLoopingListDelegate(IReadOnlyList<Widget> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        Children = children;
    }

    /// <summary>The list containing all children that can be supplied.</summary>
    public IReadOnlyList<Widget> Children { get; }

    public override int? EstimatedChildCount => null;

    public override int TrueIndexOf(int index) => DartModulo(index, Children.Count);

    public override Widget? Build(BuildContext context, int index)
    {
        if (Children.Count == 0)
        {
            return null;
        }

        return new IndexedSemantics(index: index, child: Children[DartModulo(index, Children.Count)]);
    }

    public override bool ShouldRebuild(ListWheelChildDelegate oldDelegate) =>
        !ReferenceEquals(Children, ((ListWheelChildLoopingListDelegate)oldDelegate).Children);

    /// <summary>Dart's <c>%</c> is never negative for a positive divisor; C#'s takes the dividend's
    /// sign.</summary>
    private static int DartModulo(int value, int divisor) => ((value % divisor) + divisor) % divisor;
}

/// <summary>
/// A delegate that supplies children for <see cref="ListWheelScrollView"/> using a builder callback.
/// </summary>
public class ListWheelChildBuilderDelegate : ListWheelChildDelegate
{
    public ListWheelChildBuilderDelegate(NullableIndexedWidgetBuilder builder, int? childCount = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Builder = builder;
        ChildCount = childCount;
    }

    /// <summary>Called lazily to build children.</summary>
    public NullableIndexedWidgetBuilder Builder { get; }

    /// <summary>
    /// The maximum number of children that can be provided to <see cref="ListWheelScrollView"/>. If
    /// non-null, the children will have index in the range <c>[0, childCount - 1]</c>. If null,
    /// then there's no explicit limits to the range of the children except that it has to be
    /// contiguous. If <see cref="Build"/> returns null for a certain index, that index is already
    /// past the limit.
    /// </summary>
    public int? ChildCount { get; }

    public override int? EstimatedChildCount => ChildCount;

    public override Widget? Build(BuildContext context, int index)
    {
        if (ChildCount == null)
        {
            Widget? child = Builder(context, index);
            return child == null ? null : new IndexedSemantics(index: index, child: child);
        }

        if (index < 0 || index >= ChildCount.Value)
        {
            return null;
        }

        return new IndexedSemantics(index: index, child: Builder(context, index));
    }

    public override bool ShouldRebuild(ListWheelChildDelegate oldDelegate)
    {
        var old = (ListWheelChildBuilderDelegate)oldDelegate;
        return Builder != old.Builder || ChildCount != old.ChildCount;
    }
}

/// <summary>
/// A controller for scroll views whose items have the same size.
/// </summary>
/// <remarks>
/// Similar to a standard <see cref="ScrollController"/> but with the added convenience mechanisms to
/// read and go to item indices rather than a raw pixel scroll offset. Only used with
/// <see cref="ListWheelScrollView"/>.
/// </remarks>
public class FixedExtentScrollController : ScrollController
{
    public FixedExtentScrollController(
        int initialItem = 0,
        bool keepScrollOffset = true,
        string? debugLabel = null,
        Action<ScrollPosition>? onAttach = null,
        Action<ScrollPosition>? onDetach = null)
        : base(keepScrollOffset: keepScrollOffset, debugLabel: debugLabel, onAttach: onAttach, onDetach: onDetach)
    {
        InitialItem = initialItem;
    }

    /// <summary>
    /// The page to show when first creating the scroll view. Defaults to 0.
    /// </summary>
    public int InitialItem { get; }

    /// <summary>
    /// The currently selected item index that's closest to the center of the viewport. There are
    /// circumstances that this <see cref="FixedExtentScrollController"/> can't know the current
    /// item. Reading <see cref="SelectedItem"/> will throw in the following cases: no scroll view is
    /// currently using this <see cref="FixedExtentScrollController"/>, or more than one scroll views
    /// using this <see cref="FixedExtentScrollController"/>.
    /// </summary>
    public int SelectedItem
    {
        get
        {
            if (Positions.Count == 0)
            {
                throw new InvalidOperationException(
                    "FixedExtentScrollController.selectedItem cannot be accessed before a "
                    + "scroll view is built with it.");
            }

            if (Positions.Count > 1)
            {
                throw new InvalidOperationException(
                    "The selectedItem property cannot be read when multiple scroll views are "
                    + "attached to the same FixedExtentScrollController.");
            }

            return ((FixedExtentScrollPosition)Position).ItemIndex;
        }
    }

    /// <summary>
    /// Animates the controlled scroll view to the given item index. The animation lasts for the
    /// given duration and follows the given curve. The returned task resolves when the animation
    /// completes.
    /// </summary>
    public Task AnimateToItem(int itemIndex, TimeSpan duration, Curve curve)
    {
        if (!HasClients)
        {
            return Task.CompletedTask;
        }

        var futures = new List<Task>();
        foreach (ScrollPosition position in Positions.ToArray())
        {
            var fixedExtent = (FixedExtentScrollPosition)position;
            futures.Add(fixedExtent.AnimateTo(itemIndex * fixedExtent.ItemExtent, duration, curve));
        }

        return Task.WhenAll(futures);
    }

    /// <summary>
    /// Changes which item index is centered in the controlled scroll view. Jumps the item index
    /// position from its current value to the given value, without animation, and without checking
    /// if the new value is in range.
    /// </summary>
    public void JumpToItem(int itemIndex)
    {
        foreach (ScrollPosition position in Positions.ToArray())
        {
            var fixedExtent = (FixedExtentScrollPosition)position;
            fixedExtent.JumpTo(itemIndex * fixedExtent.ItemExtent);
        }
    }

    public override ScrollPosition CreateScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition)
    {
        return new FixedExtentScrollPosition(
            physics: physics,
            context: context,
            initialItem: InitialItem,
            keepScrollOffset: KeepScrollOffset,
            oldPosition: oldPosition);
    }
}

/// <summary>
/// Metrics for a <see cref="ScrollPosition"/> to a scroll view with fixed item sizes. The metrics
/// are available on <see cref="ScrollNotification"/>s generated from a scroll views such as
/// <see cref="ListWheelScrollView"/> and exposes the current <see cref="ItemIndex"/> and the scroll
/// view's extents.
/// </summary>
public class FixedExtentMetrics : FixedScrollMetrics
{
    public FixedExtentMetrics(
        double? minScrollExtent,
        double? maxScrollExtent,
        double? pixels,
        double? viewportDimension,
        AxisDirection axisDirection,
        int itemIndex,
        double devicePixelRatio)
        : base(minScrollExtent, maxScrollExtent, pixels, viewportDimension, axisDirection, devicePixelRatio)
    {
        ItemIndex = itemIndex;
    }

    /// <summary>The scroll view's currently selected item index.</summary>
    public int ItemIndex { get; }

    public override FixedExtentMetrics CopyWith(
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return CopyWith(
            itemIndex: null,
            minScrollExtent: minScrollExtent,
            maxScrollExtent: maxScrollExtent,
            pixels: pixels,
            viewportDimension: viewportDimension,
            axisDirection: axisDirection,
            devicePixelRatio: devicePixelRatio);
    }

    /// <summary>
    /// Dart adds <c>itemIndex</c> to <c>copyWith</c>'s named arguments. C# forbids widening an
    /// override's parameter list, so the extra value moves to the front of a separate overload whose
    /// <paramref name="itemIndex"/> has no default.
    /// </summary>
    public FixedExtentMetrics CopyWith(
        int? itemIndex,
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return new FixedExtentMetrics(
            minScrollExtent: minScrollExtent ?? (HasContentDimensions ? MinScrollExtent : null),
            maxScrollExtent: maxScrollExtent ?? (HasContentDimensions ? MaxScrollExtent : null),
            pixels: pixels ?? (HasPixels ? Pixels : null),
            viewportDimension: viewportDimension ?? (HasViewportDimension ? ViewportDimension : null),
            axisDirection: axisDirection ?? AxisDirection,
            itemIndex: itemIndex ?? ItemIndex,
            devicePixelRatio: devicePixelRatio ?? DevicePixelRatio);
    }
}

/// <summary>Dart's private top-level helpers of list_wheel_scroll_view.dart.</summary>
internal static class FixedExtentMath
{
    /// <summary>Dart's <c>_getItemFromOffset</c>: the item whose center is closest to
    /// <paramref name="offset"/> once it is clamped into the scrollable range.</summary>
    public static int GetItemFromOffset(
        double offset,
        double itemExtent,
        double minScrollExtent,
        double maxScrollExtent)
    {
        return (int)Math.Round(
            ClipOffsetToScrollableRange(offset, minScrollExtent, maxScrollExtent) / itemExtent,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>Dart's <c>_clipOffsetToScrollableRange</c>.</summary>
    public static double ClipOffsetToScrollableRange(double offset, double minScrollExtent, double maxScrollExtent)
    {
        return Math.Min(Math.Max(offset, minScrollExtent), maxScrollExtent);
    }
}

/// <summary>
/// A <see cref="ScrollPosition"/> that is created by <see cref="FixedExtentScrollController"/> and
/// which reports its offset in items as well as pixels (Dart's
/// <c>_FixedExtentScrollPosition</c>, which <c>implements FixedExtentMetrics</c>).
/// </summary>
internal sealed class FixedExtentScrollPosition : ScrollPosition
{
    public FixedExtentScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        int initialItem,
        bool keepScrollOffset = true,
        ScrollPosition? oldPosition = null)
        : base(
            physics: physics,
            context: context,
            initialPixels: GetItemExtentFromScrollContext(context) * initialItem,
            keepScrollOffset: keepScrollOffset,
            oldPosition: oldPosition)
    {
    }

    /// <summary>Dart's <c>_getItemExtentFromScrollContext</c>: the item extent of the
    /// <see cref="ListWheelScrollView"/> this position belongs to.</summary>
    private static double GetItemExtentFromScrollContext(IScrollContext context)
    {
        return context is FixedExtentScrollableState scrollable
            ? scrollable.ItemExtent
            : throw new InvalidOperationException(
                "FixedExtentScrollController can only be used with ListWheelScrollViews");
    }

    public double ItemExtent => GetItemExtentFromScrollContext(Context);

    /// <summary>The scroll view's currently selected item index.</summary>
    public int ItemIndex => FixedExtentMath.GetItemFromOffset(
        offset: Pixels,
        itemExtent: ItemExtent,
        minScrollExtent: MinScrollExtent,
        maxScrollExtent: MaxScrollExtent);

    public override FixedExtentMetrics CopyWith(
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return CopyWith(
            itemIndex: null,
            minScrollExtent: minScrollExtent,
            maxScrollExtent: maxScrollExtent,
            pixels: pixels,
            viewportDimension: viewportDimension,
            axisDirection: axisDirection,
            devicePixelRatio: devicePixelRatio);
    }

    /// <summary>
    /// The item-index-carrying form of <see cref="CopyWith(double?, double?, double?, double?,
    /// AxisDirection?, double?)"/>, split out because C# forbids widening an override's parameter
    /// list.
    /// </summary>
    public FixedExtentMetrics CopyWith(
        int? itemIndex,
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return new FixedExtentMetrics(
            minScrollExtent: minScrollExtent ?? (HasContentDimensions ? MinScrollExtent : null),
            maxScrollExtent: maxScrollExtent ?? (HasContentDimensions ? MaxScrollExtent : null),
            pixels: pixels ?? (HasPixels ? Pixels : null),
            viewportDimension: viewportDimension ?? (HasViewportDimension ? ViewportDimension : null),
            axisDirection: axisDirection ?? AxisDirection,
            itemIndex: itemIndex ?? ItemIndex,
            devicePixelRatio: devicePixelRatio ?? DevicePixelRatio);
    }
}

/// <summary>
/// A snapping physics that always lands directly on items instead of anywhere within the scroll
/// extent. Behaves similarly to a slot machine wheel except the ballistics simulation never
/// overshoots and rolls back within a single item if it's to settle on that item. Must be used with
/// a scrollable that uses a <see cref="FixedExtentScrollController"/>. Defers back to the parent
/// physics' ballistics again if the parent physics is on top of the scroll extent.
/// </summary>
public class FixedExtentScrollPhysics : ScrollPhysics
{
    public FixedExtentScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor) =>
        new FixedExtentScrollPhysics(BuildParent(ancestor));

    public override Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        if (position is not FixedExtentScrollPosition metrics)
        {
            throw new InvalidOperationException(
                "FixedExtentScrollPhysics can only be used with Scrollables that uses "
                + "the FixedExtentScrollController");
        }

        // Scenario 1:
        // If we're out of range and not headed back in range, defer to the parent ballistics, which
        // should put us back in range at the scrollable's boundary.
        if ((velocity <= 0.0 && metrics.Pixels <= metrics.MinScrollExtent)
            || (velocity >= 0.0 && metrics.Pixels >= metrics.MaxScrollExtent))
        {
            return base.CreateBallisticSimulation(metrics, velocity);
        }

        // Create a test simulation to see where it would have ballistically fallen naturally without
        // settling onto items.
        Simulation? testFrictionSimulation = base.CreateBallisticSimulation(metrics, velocity);

        // Scenario 2:
        // If it was going to end up past the scroll extent, defer back to the parent physics'
        // ballistics again which should put us on the scrollable's boundary.
        if (testFrictionSimulation != null
            && (testFrictionSimulation.X(double.PositiveInfinity) == metrics.MinScrollExtent
                || testFrictionSimulation.X(double.PositiveInfinity) == metrics.MaxScrollExtent))
        {
            return base.CreateBallisticSimulation(metrics, velocity);
        }

        // From the natural final position, find the nearest item it should have settled to.
        int settlingItemIndex = FixedExtentMath.GetItemFromOffset(
            offset: testFrictionSimulation?.X(double.PositiveInfinity) ?? metrics.Pixels,
            itemExtent: metrics.ItemExtent,
            minScrollExtent: metrics.MinScrollExtent,
            maxScrollExtent: metrics.MaxScrollExtent);

        double settlingPixels = settlingItemIndex * metrics.ItemExtent;

        // Scenario 3:
        // If there's no velocity and we're already at where we intend to land, do nothing.
        Tolerance tolerance = ToleranceFor(position);
        if (Math.Abs(velocity) < tolerance.Velocity
            && Math.Abs(settlingPixels - metrics.Pixels) < tolerance.Distance)
        {
            return null;
        }

        // Scenario 4:
        // If we're going to end back at the same item because initial velocity is too low to break
        // past it, use a spring simulation to get back.
        if (settlingItemIndex == metrics.ItemIndex)
        {
            return new SpringSimulation(Spring, metrics.Pixels, settlingPixels, velocity, tolerance: tolerance);
        }

        // Scenario 5:
        // Create a new friction simulation except the drag will be tweaked to ensure that it lands
        // exactly on the item closest to the natural stopping point.
        return FrictionSimulation.Through(
            metrics.Pixels,
            settlingPixels,
            velocity,
            tolerance.Velocity * Math.Sign(velocity));
    }
}

/// <summary>
/// A box in which children on a wheel can be scrolled.
/// </summary>
/// <remarks>
/// This widget is similar to a <see cref="ListView"/> but with the restriction that all children
/// must be the same size along the scrolling axis. When the list is at the zero scroll offset, the
/// first child is aligned with the middle of the viewport. When the list is at the final scroll
/// offset, the last child is aligned with the middle of the viewport. The children are rendered as
/// if rotating on a wheel instead of scrolling on a plane.
/// </remarks>
public sealed class ListWheelScrollView : StatefulWidget
{
    /// <summary>Constructs a list in which children are scrolled a wheel. Its children are passed
    /// to a delegate and lazily built during layout.</summary>
    public ListWheelScrollView(
        double itemExtent,
        IReadOnlyList<Widget> children,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double diameterRatio = RenderListWheelViewport.DefaultDiameterRatio,
        double perspective = RenderListWheelViewport.DefaultPerspective,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0,
        double overAndUnderCenterOpacity = 1.0,
        double squeeze = 1.0,
        Action<int>? onSelectedItemChanged = null,
        bool renderChildrenOutsideViewport = false,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        string? restorationId = null,
        ScrollBehavior? scrollBehavior = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        Key? key = null) : this(
        itemExtent: itemExtent,
        childDelegate: new ListWheelChildListDelegate(children ?? throw new ArgumentNullException(nameof(children))),
        controller: controller,
        physics: physics,
        diameterRatio: diameterRatio,
        perspective: perspective,
        offAxisFraction: offAxisFraction,
        useMagnifier: useMagnifier,
        magnification: magnification,
        overAndUnderCenterOpacity: overAndUnderCenterOpacity,
        squeeze: squeeze,
        onSelectedItemChanged: onSelectedItemChanged,
        renderChildrenOutsideViewport: renderChildrenOutsideViewport,
        clipBehavior: clipBehavior,
        hitTestBehavior: hitTestBehavior,
        restorationId: restorationId,
        scrollBehavior: scrollBehavior,
        dragStartBehavior: dragStartBehavior,
        changeReportingBehavior: changeReportingBehavior,
        key: key)
    {
    }

    /// <summary>Constructs a list in which children are scrolled a wheel. Its children are managed
    /// by a delegate and are lazily built during layout (Dart's
    /// <c>ListWheelScrollView.useDelegate</c>).</summary>
    public ListWheelScrollView(
        double itemExtent,
        ListWheelChildDelegate childDelegate,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double diameterRatio = RenderListWheelViewport.DefaultDiameterRatio,
        double perspective = RenderListWheelViewport.DefaultPerspective,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0,
        double overAndUnderCenterOpacity = 1.0,
        double squeeze = 1.0,
        Action<int>? onSelectedItemChanged = null,
        bool renderChildrenOutsideViewport = false,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        string? restorationId = null,
        ScrollBehavior? scrollBehavior = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(childDelegate);
        if (!(diameterRatio > 0.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(diameterRatio),
                RenderListWheelViewport.DiameterRatioZeroMessage);
        }

        if (!(perspective > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(perspective));
        }

        if (!(perspective <= 0.01))
        {
            throw new ArgumentOutOfRangeException(
                nameof(perspective),
                RenderListWheelViewport.PerspectiveTooHighMessage);
        }

        if (!(magnification > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(magnification));
        }

        if (!(overAndUnderCenterOpacity >= 0 && overAndUnderCenterOpacity <= 1))
        {
            throw new ArgumentOutOfRangeException(nameof(overAndUnderCenterOpacity));
        }

        if (!(itemExtent > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (!(squeeze > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(squeeze));
        }

        if (renderChildrenOutsideViewport && clipBehavior != Clip.None)
        {
            throw new ArgumentException(
                RenderListWheelViewport.ClipBehaviorAndRenderChildrenOutsideViewportConflict,
                nameof(renderChildrenOutsideViewport));
        }

        Controller = controller;
        Physics = physics;
        DiameterRatio = diameterRatio;
        Perspective = perspective;
        OffAxisFraction = offAxisFraction;
        UseMagnifier = useMagnifier;
        Magnification = magnification;
        OverAndUnderCenterOpacity = overAndUnderCenterOpacity;
        ItemExtent = itemExtent;
        Squeeze = squeeze;
        OnSelectedItemChanged = onSelectedItemChanged;
        RenderChildrenOutsideViewport = renderChildrenOutsideViewport;
        ClipBehavior = clipBehavior;
        HitTestBehavior = hitTestBehavior;
        RestorationId = restorationId;
        ScrollBehavior = scrollBehavior;
        DragStartBehavior = dragStartBehavior;
        ChangeReportingBehavior = changeReportingBehavior;
        ChildDelegate = childDelegate;
    }

    /// <summary>Typically a <see cref="FixedExtentScrollController"/> used to control the current
    /// item. A <see cref="FixedExtentScrollController"/> can be used to read the currently selected
    /// or centered child item and can be used to change the current item. If none is provided, a
    /// new <see cref="FixedExtentScrollController"/> is implicitly created. If a
    /// <see cref="ScrollController"/> is used instead of <see cref="FixedExtentScrollController"/>,
    /// <see cref="ScrollNotification.Metrics"/> will no longer provide <see cref="FixedExtentMetrics"/>
    /// to indicate the current item index and <see cref="OnSelectedItemChanged"/> will not work.</summary>
    public ScrollController? Controller { get; }

    /// <summary>How the scroll view should respond to user input. Defaults to matching platform
    /// conventions.</summary>
    public ScrollPhysics? Physics { get; }

    /// <summary>See <see cref="RenderListWheelViewport.DiameterRatio"/>.</summary>
    public double DiameterRatio { get; }

    /// <summary>See <see cref="RenderListWheelViewport.Perspective"/>.</summary>
    public double Perspective { get; }

    /// <summary>See <see cref="RenderListWheelViewport.OffAxisFraction"/>.</summary>
    public double OffAxisFraction { get; }

    /// <summary>See <see cref="RenderListWheelViewport.UseMagnifier"/>.</summary>
    public bool UseMagnifier { get; }

    /// <summary>See <see cref="RenderListWheelViewport.Magnification"/>.</summary>
    public double Magnification { get; }

    /// <summary>See <see cref="RenderListWheelViewport.OverAndUnderCenterOpacity"/>.</summary>
    public double OverAndUnderCenterOpacity { get; }

    /// <summary>Size of each child in the main axis. Must not be null and must be positive.</summary>
    public double ItemExtent { get; }

    /// <summary>See <see cref="RenderListWheelViewport.Squeeze"/>.</summary>
    public double Squeeze { get; }

    /// <summary>On optional listener that's called when the centered item changes.</summary>
    public Action<int>? OnSelectedItemChanged { get; }

    /// <summary>See <see cref="RenderListWheelViewport.RenderChildrenOutsideViewport"/>.</summary>
    public bool RenderChildrenOutsideViewport { get; }

    /// <summary>A delegate that helps lazily instantiating child.</summary>
    public ListWheelChildDelegate ChildDelegate { get; }

    /// <summary>The content will be clipped (or not) according to this option. Defaults to
    /// <see cref="Clip.HardEdge"/>.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>Defaults to <see cref="HitTestBehavior.Opaque"/>.</summary>
    public HitTestBehavior HitTestBehavior { get; }

    /// <summary>Restoration ID to save and restore the scroll offset of the scrollable.</summary>
    public string? RestorationId { get; }

    /// <summary>A <see cref="ScrollBehavior"/> that will be applied to this widget individually.
    /// Defaults to null, wherein the inherited <see cref="ScrollBehavior"/> is copied and modified
    /// to alter the viewport decoration, like <see cref="Scrollbar"/>s.</summary>
    public ScrollBehavior? ScrollBehavior { get; }

    /// <summary>Defaults to <see cref="DragStartBehavior.Start"/>.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>Determines when <see cref="OnSelectedItemChanged"/> is invoked. Defaults to
    /// <see cref="ChangeReportingBehavior.OnScrollUpdate"/>.</summary>
    public ChangeReportingBehavior ChangeReportingBehavior { get; }

    public override State CreateState() => new ListWheelScrollViewState();

    private sealed class ListWheelScrollViewState : State
    {
        private int _lastReportedItemIndex;
        private ScrollController? _backupController;

        private ListWheelScrollView CurrentWidget => (ListWheelScrollView)StateWidget;

        private ScrollController EffectiveController =>
            CurrentWidget.Controller ?? (_backupController ??= new FixedExtentScrollController());

        public override void InitState()
        {
            base.InitState();
            if (CurrentWidget.Controller is FixedExtentScrollController controller)
            {
                _lastReportedItemIndex = controller.InitialItem;
            }
        }

        public override void Dispose()
        {
            _backupController?.Dispose();
            base.Dispose();
        }

        private void ReportSelectedItemChanged(ScrollNotification notification)
        {
            var metrics = (FixedExtentMetrics)notification.Metrics;
            int currentItemIndex = metrics.ItemIndex;
            if (currentItemIndex != _lastReportedItemIndex)
            {
                _lastReportedItemIndex = currentItemIndex;
                int trueIndex = CurrentWidget.ChildDelegate.TrueIndexOf(currentItemIndex);
                CurrentWidget.OnSelectedItemChanged!(trueIndex);
            }
        }

        private bool HandleScrollNotification(ScrollNotification notification)
        {
            if (CurrentWidget.OnSelectedItemChanged == null
                || notification.Depth != 0
                || notification.Metrics is not FixedExtentMetrics)
            {
                return false;
            }

            switch (CurrentWidget.ChangeReportingBehavior)
            {
                case ChangeReportingBehavior.OnScrollEnd:
                    if (notification is ScrollEndNotification)
                    {
                        ReportSelectedItemChanged(notification);
                    }

                    break;
                case ChangeReportingBehavior.OnScrollUpdate:
                    if (notification is ScrollUpdateNotification)
                    {
                        ReportSelectedItemChanged(notification);
                    }

                    break;
            }

            return false;
        }

        public override Widget Build(BuildContext context)
        {
            ListWheelScrollView widget = CurrentWidget;
            ScrollBehavior scrollBehavior = widget.ScrollBehavior
                                            ?? ScrollConfiguration.Of(context).CopyWith(scrollbars: false);
            // Flutter leaves the physics null and lets Scrollable append the ambient physics; the
            // chain is composed here instead (the PageView precedent) so the whole chain reaches
            // the position.
            ScrollPhysics ambient = scrollBehavior.GetScrollPhysics(context);
            ScrollPhysics physics = widget.Physics?.ApplyTo(ambient) ?? ambient;
            return new NotificationListener<ScrollNotification>(
                onNotification: HandleScrollNotification,
                child: new FixedExtentScrollable(
                    controller: EffectiveController,
                    physics: physics,
                    itemExtent: widget.ItemExtent,
                    restorationId: widget.RestorationId,
                    hitTestBehavior: widget.HitTestBehavior,
                    scrollBehavior: scrollBehavior,
                    dragStartBehavior: widget.DragStartBehavior)
                {
                    ViewportBuilder = (viewportContext, offset) => new ListWheelViewport(
                        itemExtent: widget.ItemExtent,
                        offset: offset,
                        childDelegate: widget.ChildDelegate,
                        diameterRatio: widget.DiameterRatio,
                        perspective: widget.Perspective,
                        offAxisFraction: widget.OffAxisFraction,
                        useMagnifier: widget.UseMagnifier,
                        magnification: widget.Magnification,
                        overAndUnderCenterOpacity: widget.OverAndUnderCenterOpacity,
                        squeeze: widget.Squeeze,
                        renderChildrenOutsideViewport: widget.RenderChildrenOutsideViewport,
                        clipBehavior: widget.ClipBehavior),
                });
        }
    }
}

/// <summary>
/// A <see cref="Scrollable"/> that carries the fixed main-axis extent of every item, so that the
/// <see cref="FixedExtentScrollPosition"/> its controller creates can read it through its
/// <see cref="IScrollContext"/> (Dart's private <c>_FixedExtentScrollable</c>).
/// </summary>
internal sealed class FixedExtentScrollable : Scrollable
{
    public FixedExtentScrollable(
        double itemExtent,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        string? restorationId = null,
        ScrollBehavior? scrollBehavior = null,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start)
        : base(
            controller: controller,
            physics: physics,
            restorationId: restorationId,
            scrollBehavior: scrollBehavior,
            hitTestBehavior: hitTestBehavior,
            dragStartBehavior: dragStartBehavior)
    {
        ItemExtent = itemExtent;
    }

    /// <summary>The main-axis extent of every item.</summary>
    public double ItemExtent { get; }

    public override State CreateState()
    {
        return new FixedExtentScrollableState();
    }
}

/// <summary>Dart's private <c>_FixedExtentScrollableState</c>.</summary>
internal sealed class FixedExtentScrollableState : Scrollable.ScrollableState
{
    /// <summary>The item extent of the <see cref="FixedExtentScrollable"/> this state belongs to.</summary>
    public double ItemExtent => ((FixedExtentScrollable)Element.Widget).ItemExtent;
}

/// <summary>
/// Element that supports building children lazily for <see cref="ListWheelViewport"/>.
/// </summary>
public sealed class ListWheelElement : RenderObjectElement, IListWheelChildManager
{
    /// <summary>
    /// A cache of widgets so that we don't have to rebuild every time. Values may be null when
    /// the delegate declined to build an index.
    /// </summary>
    private readonly Dictionary<int, Widget?> _childWidgets = [];

    /// <summary>The map containing all active child elements. SortedDictionary is used so that we
    /// have all elements ordered and iterable by their keys.</summary>
    private readonly SortedDictionary<int, Element> _childElements = [];

    public ListWheelElement(ListWheelViewport widget) : base(widget)
    {
    }

    private ListWheelViewport TypedWidget => (ListWheelViewport)Widget;

    private RenderListWheelViewport TypedRenderObject => (RenderListWheelViewport)RequireRenderObject();

    internal override void Update(Widget newWidget)
    {
        var oldWidget = (ListWheelViewport)Widget;
        base.Update(newWidget);
        ListWheelChildDelegate newDelegate = ((ListWheelViewport)newWidget).ChildDelegate;
        ListWheelChildDelegate oldDelegate = oldWidget.ChildDelegate;
        if (!ReferenceEquals(newDelegate, oldDelegate)
            && (newDelegate.GetType() != oldDelegate.GetType() || newDelegate.ShouldRebuild(oldDelegate)))
        {
            Rebuild();
            TypedRenderObject.MarkNeedsLayout();
        }
    }

    public int? ChildCount => TypedWidget.ChildDelegate.EstimatedChildCount;

    /// <summary>Dart's <c>performRebuild</c>: drops the widget cache and updates every active child
    /// against a freshly built widget.</summary>
    internal override void Rebuild()
    {
        _childWidgets.Clear();
        base.Rebuild();
        if (_childElements.Count == 0)
        {
            return;
        }

        int firstIndex = _childElements.Keys.First();
        int lastIndex = _childElements.Keys.Last();

        for (int index = firstIndex; index <= lastIndex; ++index)
        {
            _childElements.TryGetValue(index, out Element? current);
            Element? newChild = UpdateChild(current, RetrieveWidget(index), index);
            if (newChild != null)
            {
                _childElements[index] = newChild;
            }
            else
            {
                _childElements.Remove(index);
            }
        }
    }

    /// <summary>Asks the underlying delegate for a widget at the given index. Normally the builder
    /// is only called once for each index and the result will be cached. However when the element
    /// is rebuilt, the cache will be cleared.</summary>
    public Widget? RetrieveWidget(int index)
    {
        if (!_childWidgets.TryGetValue(index, out Widget? widget))
        {
            widget = TypedWidget.ChildDelegate.Build(new BuildContext(this), index);
            _childWidgets[index] = widget;
        }

        return widget;
    }

    public bool ChildExistsAt(int index) => RetrieveWidget(index) != null;

    public void CreateChild(int index, RenderBox? after)
    {
        BuildOwner owner = Owner ?? throw new InvalidOperationException("ListWheelElement is not attached.");
        owner.BuildScope(this, () =>
        {
            bool insertFirst = after == null;
            Debug.Assert(insertFirst || _childElements.ContainsKey(index - 1));
            _childElements.TryGetValue(index, out Element? current);
            Element? newChild = UpdateChild(current, RetrieveWidget(index), index);
            if (newChild != null)
            {
                _childElements[index] = newChild;
            }
            else
            {
                _childElements.Remove(index);
            }
        });
    }

    public void RemoveChild(RenderBox child)
    {
        BuildOwner owner = Owner ?? throw new InvalidOperationException("ListWheelElement is not attached.");
        owner.BuildScope(this, () =>
        {
            int index = TypedRenderObject.IndexOf(child);
            Debug.Assert(_childElements.ContainsKey(index));
            Element? result = UpdateChild(_childElements[index], null, index);
            Debug.Assert(result == null);
            _childElements.Remove(index);
            Debug.Assert(!_childElements.ContainsKey(index));
        });
    }

    /// <summary>Dart's <c>ListWheelElement.updateChild</c> override: keeps the child's previous
    /// layout offset and stamps its index into the parent data.</summary>
    internal override Element? UpdateChild(Element? child, Widget? newWidget, object? newSlot)
    {
        var oldParentData = child?.RenderObject?.parentData as ListWheelParentData;
        Element? newChild = base.UpdateChild(child, newWidget, newSlot);
        var newParentData = newChild?.RenderObject?.parentData as ListWheelParentData;
        if (newParentData != null)
        {
            newParentData.Index = (int)newSlot!;
            if (oldParentData != null)
            {
                newParentData.offset = oldParentData.offset;
            }
        }

        return newChild;
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        int index = (int)slot!;
        _childElements.TryGetValue(index - 1, out Element? previous);
        TypedRenderObject.Insert((RenderBox)child, after: previous?.RenderObject as RenderBox);
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        const string moveChildRenderObjectErrorMessage =
            "Currently we maintain the list in contiguous increasing order, so "
            + "moving children around is not allowed.";
        throw new InvalidOperationException(moveChildRenderObjectErrorMessage);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        Debug.Assert(ReferenceEquals(child.Parent, TypedRenderObject));
        TypedRenderObject.Remove((RenderBox)child);
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        foreach (Element child in _childElements.Values.ToArray())
        {
            visitor(child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (child.Slot is int slot && _childElements.TryGetValue(slot, out Element? current)
            && ReferenceEquals(current, child))
        {
            _childElements.Remove(slot);
        }

        base.ForgetChild(child);
    }

    internal override void Unmount()
    {
        foreach (Element child in _childElements.Values.ToArray())
        {
            UnmountChild(child);
        }

        _childElements.Clear();
        _childWidgets.Clear();
        base.Unmount();
    }
}

/// <summary>
/// A viewport showing a subset of children on a wheel.
/// </summary>
/// <remarks>
/// Typically used with <see cref="ListWheelScrollView"/>, this viewport is similar to
/// <see cref="Viewport"/> in that it shows a subset of children in a scrollable based on the
/// scrolling offset and the children's dimensions. But uses <see cref="RenderListWheelViewport"/>
/// to display the children on a wheel.
/// </remarks>
public sealed class ListWheelViewport : RenderObjectWidget
{
    /// <summary>Creates a viewport where children are rendered onto a wheel.</summary>
    public ListWheelViewport(
        double itemExtent,
        ViewportOffset offset,
        ListWheelChildDelegate childDelegate,
        double diameterRatio = RenderListWheelViewport.DefaultDiameterRatio,
        double perspective = RenderListWheelViewport.DefaultPerspective,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0,
        double overAndUnderCenterOpacity = 1.0,
        double squeeze = 1.0,
        bool renderChildrenOutsideViewport = false,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(offset);
        ArgumentNullException.ThrowIfNull(childDelegate);
        if (!(diameterRatio > 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(diameterRatio),
                RenderListWheelViewport.DiameterRatioZeroMessage);
        }

        if (!(perspective > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(perspective));
        }

        if (!(perspective <= 0.01))
        {
            throw new ArgumentOutOfRangeException(
                nameof(perspective),
                RenderListWheelViewport.PerspectiveTooHighMessage);
        }

        if (!(overAndUnderCenterOpacity >= 0 && overAndUnderCenterOpacity <= 1))
        {
            throw new ArgumentOutOfRangeException(nameof(overAndUnderCenterOpacity));
        }

        if (!(itemExtent > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (!(squeeze > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(squeeze));
        }

        if (renderChildrenOutsideViewport && clipBehavior != Clip.None)
        {
            throw new ArgumentException(
                RenderListWheelViewport.ClipBehaviorAndRenderChildrenOutsideViewportConflict,
                nameof(renderChildrenOutsideViewport));
        }

        DiameterRatio = diameterRatio;
        Perspective = perspective;
        OffAxisFraction = offAxisFraction;
        UseMagnifier = useMagnifier;
        Magnification = magnification;
        OverAndUnderCenterOpacity = overAndUnderCenterOpacity;
        ItemExtent = itemExtent;
        Squeeze = squeeze;
        RenderChildrenOutsideViewport = renderChildrenOutsideViewport;
        Offset = offset;
        ChildDelegate = childDelegate;
        ClipBehavior = clipBehavior;
    }

    /// <summary>See <see cref="RenderListWheelViewport.DiameterRatio"/>.</summary>
    public double DiameterRatio { get; }

    /// <summary>See <see cref="RenderListWheelViewport.Perspective"/>.</summary>
    public double Perspective { get; }

    /// <summary>See <see cref="RenderListWheelViewport.OffAxisFraction"/>.</summary>
    public double OffAxisFraction { get; }

    /// <summary>See <see cref="RenderListWheelViewport.UseMagnifier"/>.</summary>
    public bool UseMagnifier { get; }

    /// <summary>See <see cref="RenderListWheelViewport.Magnification"/>.</summary>
    public double Magnification { get; }

    /// <summary>See <see cref="RenderListWheelViewport.OverAndUnderCenterOpacity"/>.</summary>
    public double OverAndUnderCenterOpacity { get; }

    /// <summary>See <see cref="RenderListWheelViewport.ItemExtent"/>.</summary>
    public double ItemExtent { get; }

    /// <summary>See <see cref="RenderListWheelViewport.Squeeze"/>.</summary>
    public double Squeeze { get; }

    /// <summary>See <see cref="RenderListWheelViewport.RenderChildrenOutsideViewport"/>.</summary>
    public bool RenderChildrenOutsideViewport { get; }

    /// <summary>See <see cref="RenderListWheelViewport.Offset"/>.</summary>
    public ViewportOffset Offset { get; }

    /// <summary>A delegate that lazily instantiates children.</summary>
    public ListWheelChildDelegate ChildDelegate { get; }

    /// <summary>The content will be clipped (or not) according to this option. Defaults to
    /// <see cref="Clip.HardEdge"/>.</summary>
    public Clip ClipBehavior { get; }

    internal override Element CreateElement() => new ListWheelElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var childManager = (ListWheelElement)context.Owner;
        return new RenderListWheelViewport(
            childManager: childManager,
            offset: Offset,
            itemExtent: ItemExtent,
            diameterRatio: DiameterRatio,
            perspective: Perspective,
            offAxisFraction: OffAxisFraction,
            useMagnifier: UseMagnifier,
            magnification: Magnification,
            overAndUnderCenterOpacity: OverAndUnderCenterOpacity,
            squeeze: Squeeze,
            renderChildrenOutsideViewport: RenderChildrenOutsideViewport,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderListWheelViewport)renderObject;
        viewport.Offset = Offset;
        viewport.DiameterRatio = DiameterRatio;
        viewport.Perspective = Perspective;
        viewport.OffAxisFraction = OffAxisFraction;
        viewport.UseMagnifier = UseMagnifier;
        viewport.Magnification = Magnification;
        viewport.OverAndUnderCenterOpacity = OverAndUnderCenterOpacity;
        viewport.ItemExtent = ItemExtent;
        viewport.Squeeze = Squeeze;
        viewport.RenderChildrenOutsideViewport = RenderChildrenOutsideViewport;
        viewport.ClipBehavior = ClipBehavior;
    }
}
