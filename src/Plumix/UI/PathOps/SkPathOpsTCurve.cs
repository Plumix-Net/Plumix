namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsTCurve.h at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept.
// The implementations are SkTQuad (SkPathOpsQuad.cs), SkTConic (SkPathOpsConic.cs) and SkTCubic
// (SkPathOpsCubic.cs). make(SkArenaAlloc&) is Make(); otherPts copies the points instead of handing out pointers.

internal abstract class SkTCurve
{
    public abstract ref SkDPoint this[int n] { get; }

    public abstract bool Collapsed();

    public abstract bool ControlsInside();

    public abstract SkDVector DxdyAtT(double t);

    public abstract bool HullIntersects(in SkDQuad quad, ref bool isLinear);

    public abstract bool HullIntersects(in SkDConic conic, ref bool isLinear);

    public abstract bool HullIntersects(in SkDCubic cubic, ref bool isLinear);

    public abstract bool HullIntersects(SkTCurve curve, ref bool isLinear);

    public abstract int IntersectRay(SkIntersections i, in SkDLine line);

    public abstract bool IsConic();

    public abstract SkTCurve Make();

    public abstract int MaxIntersections();

    public abstract void OtherPts(int oddMan, Span<SkDPoint> endPt);

    public abstract int PointCount();

    public abstract int PointLast();

    public abstract SkDPoint PtAtT(double t);

    public abstract void SetBounds(ref SkDRect rect);

    public abstract void SubDivide(double t1, double t2, SkTCurve curve);
}
