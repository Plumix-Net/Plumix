namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of the SkPathOp enum of skia/include/pathops/SkPathOps.h at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Same numbering as Skia; SkOpSegment's gActiveEdge table is indexed
// by it.

// The logical operations that can be performed when combining two paths.
internal enum SkPathOp
{
    Difference, //!< subtract the op path from the first path
    Intersect, //!< intersect the two paths
    Union, //!< union (inclusive-or) the two paths
    XOR, //!< exclusive-or the two paths
    ReverseDifference, //!< subtract the first path from the op path
}
