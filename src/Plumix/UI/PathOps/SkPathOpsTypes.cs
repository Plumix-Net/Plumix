namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsTypes.h and SkPathOpsTypes.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept.
//
// Conventions for every file under UI/PathOps (later layers follow them exactly):
//
//  1. Files and types. Namespace Plumix.UI.PathOps, every type internal. One C# file per Skia translation unit,
//     named after it (SkPathOpsCubic.h + .cpp -> SkPathOpsCubic.cs). Each file starts with the "C#-only
//     infrastructure: port of ..." header. A type whose members are spread over several Skia .cpp files is a
//     `partial` struct/class, one part per file (SkIntersections, SkDQuad, SkDCubic).
//  2. Names. Types keep Skia's names (SkDPoint, SkDCubic, SkIntersections, SkTCurve...). Methods: Skia's name in
//     PascalCase (ptAtT -> PtAtT). A static/instance pair that differs only in case (approximatelyEqual /
//     ApproximatelyEqual, subDivide / SubDivide) becomes overloads of one PascalCase name.
//     Fields: public Skia field fFoo -> public field Foo (fPts -> Pts, fX -> X, fWeight -> Weight); private or
//     protected fFoo -> private field _foo (fUsed -> _used). When a public field would collide with a method,
//     the field keeps Skia's f as a capital F (SkReduceOrder.fQuad -> FQuad, next to SkReduceOrder.Quad()).
//     Constants: kFoo -> KFoo and gFoo -> GFoo (kPointCount -> KPointCount, so it never collides with the
//     static pointCount() -> PointCount()); SCREAMING_CASE macros and consts -> PascalCase (FLT_EPSILON_HALF ->
//     FltEpsilonHalf, SK_ScalarMax -> SkScalarMax). Enum members drop the k and a `_EnumName` suffix that
//     repeats the enum (kWinding_PathOpsMask -> SkPathOpsMask.Winding, kXAxis -> SearchAxis.XAxis).
//  3. Free functions. Global functions and inline helpers from headers (approximately_zero, AlmostEqualUlps,
//     SkDInterp, other_two...) live in `internal static partial class SkPathOps`, mechanically PascalCased:
//     snake_case words are capitalized and underscores dropped (approximately_zero -> ApproximatelyZero,
//     AlmostEqualUlps_Pin -> AlmostEqualUlpsPin, other_two -> OtherTwo); names already in PascalCase stay
//     (SkDInterp, SkPinT, SKDSide). File-local `static` functions of a .cpp go into a `file static class
//     <FileName>Statics` in that file (SkPathOpsQuadStatics.InterpQuadCoords), never into SkPathOps.
//  4. Value types. SkDPoint, SkDVector, SkDLine, SkDQuad, SkDConic, SkDCubic, SkDRect, SkDQuadPair, SkDCubicPair,
//     SkLineParameters, SkPathOpsBounds and the SkReduceOrder union are structs, so assignment copies as in C++.
//     Fixed arrays (SkDPoint fPts[4]) are [InlineArray] structs (SkDPoint4, SkPathOpsPoint.cs). Curve structs
//     expose `[UnscopedRef] ref SkDPoint this[int n]` for Skia's operator[]; inside their own readonly methods
//     and on `in` parameters prefer `.Pts[n]`, which avoids a defensive copy. Non-mutating methods are
//     `readonly`. C++ aggregate initializers become constructors (SkDLine b0 = {{a, b}} -> new SkDLine(a, b)).
//     A C++ union (SkReduceOrder) is [StructLayout(LayoutKind.Explicit)] with every member at offset 0.
//     Reinterpreting casts of point runs ((const SkDLine&) cubic[0], SkDQuadPair::first()) copy the points into
//     the target struct; that is only done where Skia reads through the cast.
//  5. Parameters. const T& of a struct -> `in T`; T& -> `ref T`. A pointer out-parameter that is always
//     written -> `out`; one written only on some paths (bool* isLinear, double* ratio) -> `ref`. An optional
//     pointer output that callers pass as nullptr (SkDLine::nearPoint's bool* unequal) -> two overloads, with
//     and without the `ref` parameter. Arrays (const SkDPoint pts[], double t[2]) -> ReadOnlySpan<T>/Span<T>;
//     pointer arithmetic (&tValues[roots]) -> slicing (tValues[roots..]); a nullable array pointer -> a span
//     that is empty for nullptr. The double-stride views Skia takes with &pts[0].fX / &pts[0].fY are
//     SkPathOps.Doubles(Pts) / SkPathOps.Doubles(Pts)[1..] (MutableDoubles for writes). Arrays of const point
//     pointers that are only read (otherPts' const SkDPoint* endPt[2]) receive copies in a Span<SkDPoint>.
//     Local C arrays are `stackalloc` spans declared outside loops. Methods that return *this for chaining:
//     a const T& return (SkDCubic::set) returns a copy (only const methods are chained on it in Skia);
//     a non-const T& return (SkDVector::normalize) returns void.
//  6. Identity types. Classes with identity (SkArenaAlloc-allocated, linked by pointers, or always used through
//     a pointer: SkTCurve and its wrappers, SkIntersections) are C# classes; SkArenaAlloc::make<T>() is `new T()`
//     and the allocator parameter is dropped. SkTDArray/SkSTArray -> List<T> (CollectionsMarshal.AsSpan to sort).
//     Skia's SkPoint is Plumix.UI.PathPoint, SkPath::Verb is Plumix.UI.PathVerb (same numbering, no kDone),
//     SkConic arguments are (points, weight).
//  7. Debug code. SkASSERT, SkDEBUGCODE, SkDEBUGPARAMS, DEBUG_* blocks, SkDebugf and debug*/dump* methods are
//     omitted; release-build logic never is. Methods that release code calls but whose bodies are debug-only
//     (SkIntersections::upDepth) stay, with release behaviour (empty / returning 0). Comments that explain math
//     are kept.
//  8. Numerics. float stays float and double stays double (SkScalar is float); every narrowing conversion is an
//     explicit cast; SkDoubleToScalar(x) is (float)x. Expressions are not simplified or reordered, and float
//     subexpressions stay float (2 * (w - 1) with float w is computed in float, as in C++). FLT_EPSILON and
//     DBL_EPSILON are SkPathOps.FltEpsilon / DblEpsilon (2^-23 / 2^-52), never float.Epsilon/double.Epsilon.
//     std::min/std::max/SkTAbs/SkTPin -> SkPathOps.StdMin/StdMax/SkTAbs/SkTPin (Skia's comparison order;
//     Math.Min/Max/Abs differ on NaN and signed zero). fabs/sqrt/cbrt/acos/cos/copysign -> Math.*. Bit tricks
//     use BitConverter.SingleToInt32Bits / DoubleToInt64Bits. Integer arithmetic stays unchecked (wraps as the
//     C++ does); uint16_t/unsigned char fields are ushort/byte with explicit truncating casts after arithmetic.
//     `if (!x)` on a double is `x == 0`; bool-to-int (`1 + !a`) is `(a ? 0 : 1)`. goto is kept where Skia uses it.
//  9. Templates. SkTSect<TCurve>-style templates become generics or work through SkTCurve, the abstract base of
//     SkTQuad/SkTConic/SkTCubic. SkTQSort and friends are SkPathOps.SkTQSort (SkTSort.cs).
// 10. Skia code from outside src/pathops that pathops needs is ported here too, one file per source, into
//     SkPathOps: SkPathOpsBase.cs (include/private/base helpers, std::min/max, the Doubles views), SkTSort.cs
//     (src/base/SkTSort.h), SkGeometry.cs (SkClassifyCubic, which Plumix.UI.PathGeometry lacks). Reuse
//     Plumix.UI.PathGeometry for SkGeometry functions it already ports instead of duplicating them.
// 11. Calls into code a later layer ports are declared in a `<Type>.Pending.cs` placeholder that throws
//     NotImplementedException (as SkIntersections.Pending.cs did until the intersection layer replaced it); the
//     layer that ports the real code deletes it.
//
// SkOpGlobalState arrived with the graph-core layer (SkOpSpan/SkOpSegment/SkOpAngle/SkOpContour): its SkArenaAlloc
// (allocator(), whose make<T>() is `new T()` per convention 6) and debug members are dropped; fAllocatedOpSpan,
// which Skia leaves uninitialized, starts false.

internal enum SkPathOpsMask
{
    Winding = -1,
    No = 0,
    EvenOdd = 1,
}

internal enum SkOpPhase : byte
{
    NoChange,
    Intersecting,
    Walking,
    FixWinding,
}

internal sealed class SkOpGlobalState
{
    public const int KMaxWindingTries = 10;

    private SkOpCoincidence? _coincidence;
    private SkOpContourHead _contourHead;
    private int _nested;
    private bool _allocatedOpSpan;
    private bool _windingFailed;
    private SkOpPhase _phase;

    // SkPathOpsTypes.cpp
    public SkOpGlobalState(SkOpContourHead head)
    {
        _coincidence = null;
        _contourHead = head;
        _nested = 0;
        _windingFailed = false;
        _phase = SkOpPhase.Intersecting;
    }

    public bool AllocatedOpSpan()
    {
        return _allocatedOpSpan;
    }

    public void BumpNested()
    {
        ++_nested;
    }

    public void ClearNested()
    {
        _nested = 0;
    }

    public SkOpCoincidence? Coincidence()
    {
        return _coincidence;
    }

    public SkOpContourHead ContourHead()
    {
        return _contourHead;
    }

    public int Nested()
    {
        return _nested;
    }

    public SkOpPhase Phase()
    {
        return _phase;
    }

    public void ResetAllocatedOpSpan()
    {
        _allocatedOpSpan = false;
    }

    public void SetAllocatedOpSpan()
    {
        _allocatedOpSpan = true;
    }

    public void SetCoincidence(SkOpCoincidence? coincidence)
    {
        _coincidence = coincidence;
    }

    public void SetContourHead(SkOpContourHead contourHead)
    {
        _contourHead = contourHead;
    }

    public void SetPhase(SkOpPhase phase)
    {
        if (SkOpPhase.NoChange == phase)
        {
            return;
        }

        _phase = phase;
    }

    // called in very rare cases where angles are sorted incorrectly -- signfies op will fail
    public void SetWindingFailed()
    {
        _windingFailed = true;
    }

    public bool WindingFailed()
    {
        return _windingFailed;
    }
}

internal static partial class SkPathOps
{
    // Use Almost Equal when comparing coordinates. Use epsilon to compare T values.
    public static bool AlmostEqualUlps(double a, double b)
    {
        return AlmostEqualUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool AlmostEqualUlpsNoNormalCheck(double a, double b)
    {
        return AlmostEqualUlpsNoNormalCheck(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool AlmostEqualUlpsPin(double a, double b)
    {
        return AlmostEqualUlpsPin(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool NotAlmostEqualUlps(double a, double b)
    {
        return NotAlmostEqualUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool NotAlmostEqualUlpsPin(double a, double b)
    {
        return NotAlmostEqualUlpsPin(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool NotAlmostDequalUlps(double a, double b)
    {
        return NotAlmostDequalUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    // Use Almost Bequal when comparing coordinates in conjunction with between.
    public static bool AlmostBequalUlps(double a, double b)
    {
        return AlmostBequalUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool AlmostPequalUlps(double a, double b)
    {
        return AlmostPequalUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool RoughlyEqualUlps(double a, double b)
    {
        return RoughlyEqualUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool AlmostLessUlps(double a, double b)
    {
        return AlmostLessUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool AlmostLessOrEqualUlps(double a, double b)
    {
        return AlmostLessOrEqualUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    public static bool AlmostBetweenUlps(double a, double b, double c)
    {
        return AlmostBetweenUlps(SkDoubleToScalar(a), SkDoubleToScalar(b), SkDoubleToScalar(c));
    }

    public static int UlpsDistance(double a, double b)
    {
        return UlpsDistance(SkDoubleToScalar(a), SkDoubleToScalar(b));
    }

    // FLT_EPSILON == 1.19209290E-07 == 1 / (2 ^ 23)
    // DBL_EPSILON == 2.22045e-16
    public const float FltEpsilon = 1.0f / 8388608.0f;
    public const double DblEpsilon = 1.0 / 4503599627370496.0;

    // The products below are float arithmetic in Skia (FLT_EPSILON is a float); all are exact powers of two.
    public const double FltEpsilonCubed = FltEpsilon * FltEpsilon * FltEpsilon;
    public const double FltEpsilonHalf = FltEpsilon / 2;
    public const double FltEpsilonDouble = FltEpsilon * 2;
    public const double FltEpsilonOrderableErr = FltEpsilon * 16;
    public const double FltEpsilonSquared = FltEpsilon * FltEpsilon;

    // Use a compile-time constant for FLT_EPSILON_SQRT to avoid initializers.
    // A 17 digit constant guarantees exact results.
    public const double FltEpsilonSqrt = 0.00034526697709225118; // sqrt(FLT_EPSILON);
    public const double FltEpsilonInverse = 1 / FltEpsilon;
    public const double DblEpsilonErr = DblEpsilon * 4; // FIXME: tune -- allow a few bits of error
    public const double DblEpsilonSubdivideErr = DblEpsilon * 16;
    public const double RoughEpsilon = FltEpsilon * 64;
    public const double MoreRoughEpsilon = FltEpsilon * 256;
    public const double WayRoughEpsilon = FltEpsilon * 2048;
    public const double BumpEpsilon = FltEpsilon * 4096;

    public const float InverseNumberRange = (float)FltEpsilonOrderableErr;

    public static bool ZeroOrOne(double x)
    {
        return x == 0 || x == 1;
    }

    public static bool ApproximatelyZero(double x)
    {
        return Math.Abs(x) < FltEpsilon;
    }

    public static bool PreciselyZero(double x)
    {
        return Math.Abs(x) < DblEpsilonErr;
    }

    public static bool PreciselySubdivideZero(double x)
    {
        return Math.Abs(x) < DblEpsilonSubdivideErr;
    }

    public static bool ApproximatelyZero(float x)
    {
        return MathF.Abs(x) < FltEpsilon;
    }

    public static bool ApproximatelyZeroHalf(double x)
    {
        return Math.Abs(x) < FltEpsilonHalf;
    }

    public static bool ApproximatelyZeroDouble(double x)
    {
        return Math.Abs(x) < FltEpsilonDouble;
    }

    public static bool ApproximatelyZeroOrderable(double x)
    {
        return Math.Abs(x) < FltEpsilonOrderableErr;
    }

    public static bool ApproximatelyZeroSquared(double x)
    {
        return Math.Abs(x) < FltEpsilonSquared;
    }

    public static bool ApproximatelyZeroSqrt(double x)
    {
        return Math.Abs(x) < FltEpsilonSqrt;
    }

    public static bool RoughlyZero(double x)
    {
        return Math.Abs(x) < RoughEpsilon;
    }

    public static bool ApproximatelyZeroInverse(double x)
    {
        return Math.Abs(x) > FltEpsilonInverse;
    }

    public static bool ApproximatelyZeroWhenComparedTo(double x, double y)
    {
        return x == 0 || Math.Abs(x) < Math.Abs(y * FltEpsilon);
    }

    public static bool PreciselyZeroWhenComparedTo(double x, double y)
    {
        return x == 0 || Math.Abs(x) < Math.Abs(y * DblEpsilon);
    }

    public static bool RoughlyZeroWhenComparedTo(double x, double y)
    {
        return x == 0 || Math.Abs(x) < Math.Abs(y * RoughEpsilon);
    }

    // Use this for comparing Ts in the range of 0 to 1. For general numbers (larger and smaller) use
    // AlmostEqualUlps instead.
    public static bool ApproximatelyEqual(double x, double y)
    {
        return ApproximatelyZero(x - y);
    }

    public static bool PreciselyEqual(double x, double y)
    {
        return PreciselyZero(x - y);
    }

    public static bool PreciselySubdivideEqual(double x, double y)
    {
        return PreciselySubdivideZero(x - y);
    }

    public static bool ApproximatelyEqualHalf(double x, double y)
    {
        return ApproximatelyZeroHalf(x - y);
    }

    public static bool ApproximatelyEqualDouble(double x, double y)
    {
        return ApproximatelyZeroDouble(x - y);
    }

    public static bool ApproximatelyEqualOrderable(double x, double y)
    {
        return ApproximatelyZeroOrderable(x - y);
    }

    public static bool ApproximatelyEqualSquared(double x, double y)
    {
        return ApproximatelyEqual(x, y);
    }

    public static bool ApproximatelyGreater(double x, double y)
    {
        return x - FltEpsilon >= y;
    }

    public static bool ApproximatelyGreaterDouble(double x, double y)
    {
        return x - FltEpsilonDouble >= y;
    }

    public static bool ApproximatelyGreaterOrderable(double x, double y)
    {
        return x - FltEpsilonOrderableErr >= y;
    }

    public static bool ApproximatelyGreaterOrEqual(double x, double y)
    {
        return x + FltEpsilon > y;
    }

    public static bool ApproximatelyGreaterOrEqualDouble(double x, double y)
    {
        return x + FltEpsilonDouble > y;
    }

    public static bool ApproximatelyGreaterOrEqualOrderable(double x, double y)
    {
        return x + FltEpsilonOrderableErr > y;
    }

    public static bool ApproximatelyLesser(double x, double y)
    {
        return x + FltEpsilon <= y;
    }

    public static bool ApproximatelyLesserDouble(double x, double y)
    {
        return x + FltEpsilonDouble <= y;
    }

    public static bool ApproximatelyLesserOrderable(double x, double y)
    {
        return x + FltEpsilonOrderableErr <= y;
    }

    public static bool ApproximatelyLesserOrEqual(double x, double y)
    {
        return x - FltEpsilon < y;
    }

    public static bool ApproximatelyLesserOrEqualDouble(double x, double y)
    {
        return x - FltEpsilonDouble < y;
    }

    public static bool ApproximatelyLesserOrEqualOrderable(double x, double y)
    {
        return x - FltEpsilonOrderableErr < y;
    }

    public static bool ApproximatelyGreaterThanOne(double x)
    {
        return x > 1 - FltEpsilon;
    }

    public static bool PreciselyGreaterThanOne(double x)
    {
        return x > 1 - DblEpsilonErr;
    }

    public static bool ApproximatelyLessThanZero(double x)
    {
        return x < FltEpsilon;
    }

    public static bool PreciselyLessThanZero(double x)
    {
        return x < DblEpsilonErr;
    }

    public static bool ApproximatelyNegative(double x)
    {
        return x < FltEpsilon;
    }

    public static bool ApproximatelyNegativeOrderable(double x)
    {
        return x < FltEpsilonOrderableErr;
    }

    public static bool PreciselyNegative(double x)
    {
        return x < DblEpsilonErr;
    }

    public static bool ApproximatelyOneOrLess(double x)
    {
        return x < 1 + FltEpsilon;
    }

    public static bool ApproximatelyOneOrLessDouble(double x)
    {
        return x < 1 + FltEpsilonDouble;
    }

    public static bool ApproximatelyPositive(double x)
    {
        return x > -FltEpsilon;
    }

    public static bool ApproximatelyPositiveSquared(double x)
    {
        return x > -(FltEpsilonSquared);
    }

    public static bool ApproximatelyZeroOrMore(double x)
    {
        return x > -FltEpsilon;
    }

    public static bool ApproximatelyZeroOrMoreDouble(double x)
    {
        return x > -FltEpsilonDouble;
    }

    public static bool ApproximatelyBetweenOrderable(double a, double b, double c)
    {
        return a <= c
            ? ApproximatelyNegativeOrderable(a - b) && ApproximatelyNegativeOrderable(b - c)
            : ApproximatelyNegativeOrderable(b - a) && ApproximatelyNegativeOrderable(c - b);
    }

    public static bool ApproximatelyBetween(double a, double b, double c)
    {
        return a <= c
            ? ApproximatelyNegative(a - b) && ApproximatelyNegative(b - c)
            : ApproximatelyNegative(b - a) && ApproximatelyNegative(c - b);
    }

    public static bool PreciselyBetween(double a, double b, double c)
    {
        return a <= c
            ? PreciselyNegative(a - b) && PreciselyNegative(b - c)
            : PreciselyNegative(b - a) && PreciselyNegative(c - b);
    }

    // returns true if (a <= b <= c) || (a >= b >= c)
    public static bool Between(double a, double b, double c)
    {
        return (a - b) * (c - b) <= 0;
    }

    public static bool RoughlyEqual(double x, double y)
    {
        return Math.Abs(x - y) < RoughEpsilon;
    }

    public static bool RoughlyNegative(double x)
    {
        return x < RoughEpsilon;
    }

    public static bool RoughlyBetween(double a, double b, double c)
    {
        return a <= c
            ? RoughlyNegative(a - b) && RoughlyNegative(b - c)
            : RoughlyNegative(b - a) && RoughlyNegative(c - b);
    }

    public static bool MoreRoughlyEqual(double x, double y)
    {
        return Math.Abs(x - y) < MoreRoughEpsilon;
    }

    public static PathVerb SkPathOpsPointsToVerb(int points)
    {
        int verb = (1 << points) >> 1;
        return (PathVerb)verb;
    }

    public static int SkPathOpsVerbToPoints(PathVerb verb)
    {
        int points = (int)verb - (((int)verb + 1) >> 2);
        return points;
    }

    public static double SkDInterp(double A, double B, double t)
    {
        return A + (B - A) * t;
    }

    /* Returns -1 if negative, 0 if zero, 1 if positive
    */
    public static int SkDSign(double x)
    {
        return (x > 0 ? 1 : 0) - (x < 0 ? 1 : 0);
    }

    /* Returns 0 if negative, 1 if zero, 2 if positive
    */
    public static int SKDSide(double x)
    {
        return (x > 0 ? 1 : 0) + (x >= 0 ? 1 : 0);
    }

    /* Returns 1 if negative, 2 if zero, 4 if positive
    */
    public static int SkDSideBit(double x)
    {
        return 1 << SKDSide(x);
    }

    public static double SkPinT(double t)
    {
        return PreciselyLessThanZero(t) ? 0 : PreciselyGreaterThanOne(t) ? 1 : t;
    }

    // SkPathOpsTypes.cpp

    // equality using the same error term as between
    public static bool AlmostBequalUlps(float a, float b)
    {
        const int UlpsEpsilon = 2;
        return SkPathOpsTypesStatics.EqualUlps(a, b, UlpsEpsilon, UlpsEpsilon);
    }

    public static bool AlmostPequalUlps(float a, float b)
    {
        const int UlpsEpsilon = 8;
        return SkPathOpsTypesStatics.EqualUlps(a, b, UlpsEpsilon, UlpsEpsilon);
    }

    // Use Almost Dequal when comparing should not special case denormalized values.
    public static bool AlmostDequalUlps(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.DEqualUlps(a, b, UlpsEpsilon);
    }

    public static bool AlmostDequalUlps(double a, double b)
    {
        if (Math.Abs(a) < SkScalarMax && Math.Abs(b) < SkScalarMax)
        {
            return AlmostDequalUlps(SkDoubleToScalar(a), SkDoubleToScalar(b));
        }

        // We allow divide-by-zero here. It only happens if one of a,b is zero, and the other is NaN.
        // (Otherwise, we'd hit the condition above). Thus, if std::max returns 0, we compute NaN / 0,
        // which will produce NaN. The comparison will return false, which is the correct answer.
        return SkIeeeDoubleDivide(Math.Abs(a - b), StdMax(Math.Abs(a), Math.Abs(b))) < FltEpsilon * 16;
    }

    public static bool AlmostEqualUlps(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.EqualUlps(a, b, UlpsEpsilon, UlpsEpsilon);
    }

    public static bool AlmostEqualUlpsNoNormalCheck(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.EqualUlpsNoNormalCheck(a, b, UlpsEpsilon, UlpsEpsilon);
    }

    public static bool AlmostEqualUlpsPin(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.EqualUlpsPin(a, b, UlpsEpsilon, UlpsEpsilon);
    }

    public static bool NotAlmostEqualUlps(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.NotEqualUlps(a, b, UlpsEpsilon);
    }

    public static bool NotAlmostEqualUlpsPin(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.NotEqualUlpsPin(a, b, UlpsEpsilon);
    }

    public static bool NotAlmostDequalUlps(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.DNotEqualUlps(a, b, UlpsEpsilon);
    }

    public static bool RoughlyEqualUlps(float a, float b)
    {
        const int UlpsEpsilon = 256;
        const int DUlpsEpsilon = 1024;
        return SkPathOpsTypesStatics.EqualUlps(a, b, UlpsEpsilon, DUlpsEpsilon);
    }

    public static bool AlmostBetweenUlps(float a, float b, float c)
    {
        const int UlpsEpsilon = 2;
        return a <= c
            ? SkPathOpsTypesStatics.LessOrEqualUlps(a, b, UlpsEpsilon)
                && SkPathOpsTypesStatics.LessOrEqualUlps(b, c, UlpsEpsilon)
            : SkPathOpsTypesStatics.LessOrEqualUlps(b, a, UlpsEpsilon)
                && SkPathOpsTypesStatics.LessOrEqualUlps(c, b, UlpsEpsilon);
    }

    public static bool AlmostLessUlps(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.LessUlps(a, b, UlpsEpsilon);
    }

    public static bool AlmostLessOrEqualUlps(float a, float b)
    {
        const int UlpsEpsilon = 16;
        return SkPathOpsTypesStatics.LessOrEqualUlps(a, b, UlpsEpsilon);
    }

    public static int UlpsDistance(float a, float b)
    {
        int floatIntA = SkFloat2Bits(a);
        int floatIntB = SkFloat2Bits(b);
        // Different signs means they do not match.
        if ((floatIntA < 0) != (floatIntB < 0))
        {
            // Check for equality to make sure +0 == -0
            return a == b ? 0 : SkMaxS32;
        }

        // Find the difference in ULPs.
        return SkTAbs(floatIntA - floatIntB);
    }
}

file static class SkPathOpsTypesStatics
{
    public static bool ArgumentsDenormalized(float a, float b, int epsilon)
    {
        float denormalizedCheck = SkPathOps.FltEpsilon * epsilon / 2;
        return MathF.Abs(a) <= denormalizedCheck && MathF.Abs(b) <= denormalizedCheck;
    }

    // from http://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
    // FIXME: move to SkFloatBits.h
    public static bool EqualUlps(float a, float b, int epsilon, int depsilon)
    {
        if (ArgumentsDenormalized(a, b, depsilon))
        {
            return true;
        }

        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits < bBits + epsilon && bBits < aBits + epsilon;
    }

    public static bool EqualUlpsNoNormalCheck(float a, float b, int epsilon, int depsilon)
    {
        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits < bBits + epsilon && bBits < aBits + epsilon;
    }

    public static bool EqualUlpsPin(float a, float b, int epsilon, int depsilon)
    {
        if (!SkPathOps.SkScalarIsFinite(a) || !SkPathOps.SkScalarIsFinite(b))
        {
            return false;
        }

        if (ArgumentsDenormalized(a, b, depsilon))
        {
            return true;
        }

        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits < bBits + epsilon && bBits < aBits + epsilon;
    }

    public static bool DEqualUlps(float a, float b, int epsilon)
    {
        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits < bBits + epsilon && bBits < aBits + epsilon;
    }

    public static bool NotEqualUlps(float a, float b, int epsilon)
    {
        if (ArgumentsDenormalized(a, b, epsilon))
        {
            return false;
        }

        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits >= bBits + epsilon || bBits >= aBits + epsilon;
    }

    public static bool NotEqualUlpsPin(float a, float b, int epsilon)
    {
        if (!SkPathOps.SkScalarIsFinite(a) || !SkPathOps.SkScalarIsFinite(b))
        {
            return false;
        }

        if (ArgumentsDenormalized(a, b, epsilon))
        {
            return false;
        }

        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits >= bBits + epsilon || bBits >= aBits + epsilon;
    }

    public static bool DNotEqualUlps(float a, float b, int epsilon)
    {
        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits >= bBits + epsilon || bBits >= aBits + epsilon;
    }

    public static bool LessUlps(float a, float b, int epsilon)
    {
        if (ArgumentsDenormalized(a, b, epsilon))
        {
            return a <= b - SkPathOps.FltEpsilon * epsilon;
        }

        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits <= bBits - epsilon;
    }

    public static bool LessOrEqualUlps(float a, float b, int epsilon)
    {
        if (ArgumentsDenormalized(a, b, epsilon))
        {
            return a < b + SkPathOps.FltEpsilon * epsilon;
        }

        int aBits = SkPathOps.SkFloatAs2sCompliment(a);
        int bBits = SkPathOps.SkFloatAs2sCompliment(b);
        // Find the difference in ULPs.
        return aBits < bBits + epsilon;
    }
}
