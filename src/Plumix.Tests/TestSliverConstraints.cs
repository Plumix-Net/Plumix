using Plumix.Rendering;

// C#-only test infrastructure: no Dart counterpart.

namespace Plumix.Tests;

/// <summary>
/// Builds <see cref="SliverConstraints"/> for render-object tests that lay a sliver out directly.
/// </summary>
/// <remarks>
/// Dart's <c>SliverConstraints</c> constructor requires all twelve fields; Flutter's own tests spell
/// them out every time. This helper fills the ones a test does not care about with the values a
/// down-scrolling viewport at rest would pass, and derives <see cref="SliverConstraints.AxisDirection"/>
/// and <see cref="SliverConstraints.CrossAxisDirection"/> from <paramref name="Axis"/> so the two
/// always lie on different axes.
/// </remarks>
internal static class TestSliverConstraints
{
    public static SliverConstraints Create(
        Axis Axis = Axis.Vertical,
        double ScrollOffset = 0.0,
        double RemainingPaintExtent = 0.0,
        double CrossAxisExtent = 0.0,
        double ViewportMainAxisExtent = 0.0,
        double CacheOrigin = 0.0,
        double RemainingCacheExtent = 0.0,
        AxisDirection? AxisDirection = null,
        GrowthDirection GrowthDirection = GrowthDirection.Forward,
        double Overlap = 0.0,
        double PrecedingScrollExtent = 0.0,
        ScrollDirection UserScrollDirection = ScrollDirection.Idle,
        AxisDirection? CrossAxisDirection = null)
    {
        AxisDirection axisDirection = AxisDirection
            ?? (Axis == Axis.Vertical ? Rendering.AxisDirection.Down : Rendering.AxisDirection.Right);
        AxisDirection crossAxisDirection = CrossAxisDirection
            ?? (ScrollDirectionUtils.AxisDirectionToAxis(axisDirection) == Axis.Vertical
                ? Rendering.AxisDirection.Right
                : Rendering.AxisDirection.Down);
        return new SliverConstraints(
            AxisDirection: axisDirection,
            GrowthDirection: GrowthDirection,
            UserScrollDirection: UserScrollDirection,
            ScrollOffset: ScrollOffset,
            PrecedingScrollExtent: PrecedingScrollExtent,
            Overlap: Overlap,
            RemainingPaintExtent: RemainingPaintExtent,
            CrossAxisExtent: CrossAxisExtent,
            CrossAxisDirection: crossAxisDirection,
            ViewportMainAxisExtent: ViewportMainAxisExtent,
            RemainingCacheExtent: RemainingCacheExtent,
            CacheOrigin: CacheOrigin);
    }
}
