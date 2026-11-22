namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of the SkPointPriv helpers of skia/src/core/SkPointPriv.h that pathops uses, at
// chrome/m119 (the Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's operation order and
// float types are kept (the distance is float arithmetic, as SkScalar is).

internal static partial class SkPathOps
{
    // SkPointPriv::DistanceToSqd.
    public static float SkPointPrivDistanceToSqd(PathPoint pt, PathPoint a)
    {
        float dx = pt.X - a.X;
        float dy = pt.Y - a.Y;
        return dx * dx + dy * dy;
    }
}
