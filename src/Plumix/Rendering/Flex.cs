// Dart parity source: flutter/packages/flutter/lib/src/rendering/flex.dart

namespace Plumix.Rendering;

/// How the child is inscribed into the available space.
///
/// See also:
///
///  * [RenderFlex], the flex render object.
///  * [Column], [Row], and [Flex], the flex widgets.
///  * [Expanded], the widget equivalent of [Tight].
///  * [Flexible], the widget equivalent of [Loose].
public enum FlexFit
{
    /// The child is forced to fill the available space.
    Tight,

    /// The child can be at most as large as the available space (but is
    /// allowed to be smaller).
    Loose,
}

/// How much space should be occupied in the main axis.
///
/// During a flex layout, available space along the main axis is allocated to
/// children. After allocating space, there might be some remaining free space.
/// This value controls whether to maximize or minimize the amount of free
/// space, subject to the incoming layout constraints.
public enum MainAxisSize
{
    /// Minimize the amount of free space along the main axis, subject to the
    /// incoming layout constraints.
    Min,

    /// Maximize the amount of free space along the main axis, subject to the
    /// incoming layout constraints.
    Max,
}

/// How the children should be placed along the main axis in a flex layout.
public enum MainAxisAlignment
{
    /// Place the children as close to the start of the main axis as possible.
    Start,

    /// Place the children as close to the end of the main axis as possible.
    End,

    /// Place the children as close to the middle of the main axis as possible.
    Center,

    /// Place the free space evenly between the children.
    SpaceBetween,

    /// Place the free space evenly between the children as well as half of that
    /// space before and after the first and last child.
    SpaceAround,

    /// Place the free space evenly between the children as well as before and
    /// after the first and last child.
    SpaceEvenly,
}

/// How the children should be placed along the cross axis in a flex layout.
public enum CrossAxisAlignment
{
    /// Place the children with their start edge aligned with the start side of
    /// the cross axis.
    Start,

    /// Place the children as close to the end of the cross axis as possible.
    End,

    /// Place the children so that their centers align with the middle of the
    /// cross axis.
    ///
    /// This is the default cross-axis alignment.
    Center,

    /// Require the children to fill the cross axis.
    ///
    /// This causes the constraints passed to the children to be tight in the
    /// cross axis.
    Stretch,

    /// Place the children along the cross axis such that their baselines match.
    ///
    /// Because baselines are always horizontal, this alignment is intended for
    /// horizontal main axes. If the main axis is vertical, then this value is
    /// treated like [Start].
    ///
    /// For horizontal main axes, if the minimum height constraint passed to the
    /// flex layout is non-zero, the baseline of the tallest child is used, and
    /// children who report no baseline will be top-aligned.
    Baseline,
}

/// The two cardinal directions in two dimensions.
///
/// Lives in `painting/basic_types.dart` upstream; kept next to the flex layout
/// types here because `Plumix.Painting` cannot be referenced unqualified from
/// `Plumix.Rendering` call sites.
public enum Axis
{
    /// Left and right.
    Horizontal,

    /// Up and down.
    Vertical,
}

/// Parent data for use with [RenderFlex].
public sealed class FlexParentData : ContainerBoxParentData<RenderBox>
{
    /// The flex factor to use for this child.
    ///
    /// If null or zero, the child is inflexible and determines its own size. If
    /// non-zero, the amount of space the child's can occupy in the main axis is
    /// determined by dividing the free space (after placing the inflexible
    /// children) according to the flex factors of the flexible children.
    public int? flex;

    /// How a flexible child is inscribed into the available space.
    ///
    /// If [flex] is non-zero, the [fit] determines whether the child fills the
    /// space the parent makes available during layout. If the fit is
    /// [FlexFit.Tight], the child is required to fill the available space. If the
    /// fit is [FlexFit.Loose], the child can be at most as large as the available
    /// space (but is allowed to be smaller).
    public FlexFit? fit;

    public override string ToString() => $"{base.ToString()}; flex={flex}; fit={fit}";
}
