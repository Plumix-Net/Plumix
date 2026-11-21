using System.Runtime.CompilerServices;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathOpsPoint.h at chrome/m119 (the Skia inside SkiaSharp
// 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types are kept.
// Also holds the [InlineArray] structs that stand for Skia's fixed SkDPoint arrays (SkDPoint fPts[N]).

internal static partial class SkPathOps
{
    public static bool AlmostEqualUlps(PathPoint pt1, PathPoint pt2)
    {
        return AlmostEqualUlps(pt1.X, pt2.X) && AlmostEqualUlps(pt1.Y, pt2.Y);
    }
}

internal struct SkDVector
{
    public double X;
    public double Y;

    public SkDVector(double x, double y)
    {
        X = x;
        Y = y;
    }

    public void Set(PathPoint pt)
    {
        X = pt.X;
        Y = pt.Y;
    }

    // Skia's operator+= / -= / /= / *= ("only used by testing"); C# derives the compound forms from these.
    public static SkDVector operator +(SkDVector a, SkDVector v)
    {
        return new SkDVector(a.X + v.X, a.Y + v.Y);
    }

    public static SkDVector operator -(SkDVector a, SkDVector v)
    {
        return new SkDVector(a.X - v.X, a.Y - v.Y);
    }

    public static SkDVector operator /(SkDVector a, double s)
    {
        return new SkDVector(a.X / s, a.Y / s);
    }

    public static SkDVector operator *(SkDVector a, double s)
    {
        return new SkDVector(a.X * s, a.Y * s);
    }

    public readonly PathPoint AsSkVector()
    {
        return new PathPoint(SkPathOps.SkDoubleToScalar(X), SkPathOps.SkDoubleToScalar(Y));
    }

    // only used by testing
    public readonly double Cross(in SkDVector a)
    {
        return X * a.Y - Y * a.X;
    }

    // similar to cross, this bastardization considers nearly coincident to be zero
    // uses ulps epsilon == 16
    public readonly double CrossCheck(in SkDVector a)
    {
        double xy = X * a.Y;
        double yx = Y * a.X;
        return SkPathOps.AlmostEqualUlps(xy, yx) ? 0 : xy - yx;
    }

    // allow tinier numbers
    public readonly double CrossNoNormalCheck(in SkDVector a)
    {
        double xy = X * a.Y;
        double yx = Y * a.X;
        return SkPathOps.AlmostEqualUlpsNoNormalCheck(xy, yx) ? 0 : xy - yx;
    }

    public readonly double Dot(in SkDVector a)
    {
        return X * a.X + Y * a.Y;
    }

    public readonly double Length()
    {
        return Math.Sqrt(LengthSquared());
    }

    public readonly double LengthSquared()
    {
        return X * X + Y * Y;
    }

    public void Normalize()
    {
        double inverseLength = SkPathOps.SkIeeeDoubleDivide(1, Length());
        X *= inverseLength;
        Y *= inverseLength;
    }

    public readonly bool IsFinite()
    {
        return double.IsFinite(X) && double.IsFinite(Y);
    }
}

internal struct SkDPoint : IEquatable<SkDPoint>
{
    public double X;
    public double Y;

    public SkDPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    // Skia's set(const SkPoint&) and operator=(const SkPoint&).
    public void Set(PathPoint pt)
    {
        X = pt.X;
        Y = pt.Y;
    }

    public static SkDVector operator -(SkDPoint a, SkDPoint b)
    {
        return new SkDVector(a.X - b.X, a.Y - b.Y);
    }

    public static bool operator ==(SkDPoint a, SkDPoint b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool operator !=(SkDPoint a, SkDPoint b)
    {
        return a.X != b.X || a.Y != b.Y;
    }

    // Skia's operator+(const SkDVector&) / operator-(const SkDVector&); += and -= derive from them.
    public static SkDPoint operator +(SkDPoint a, SkDVector v)
    {
        SkDPoint result = a;
        result.X += v.X;
        result.Y += v.Y;
        return result;
    }

    public static SkDPoint operator -(SkDPoint a, SkDVector v)
    {
        SkDPoint result = a;
        result.X -= v.X;
        result.Y -= v.Y;
        return result;
    }

    public readonly bool Equals(SkDPoint other)
    {
        return this == other;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is SkDPoint other && this == other;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    // note: this can not be implemented with
    // return approximately_equal(a.fY, fY) && approximately_equal(a.fX, fX);
    // because that will not take the magnitude of the values into account
    public readonly bool ApproximatelyDEqual(in SkDPoint a)
    {
        if (SkPathOps.ApproximatelyEqual(X, a.X) && SkPathOps.ApproximatelyEqual(Y, a.Y))
        {
            return true;
        }

        if (!SkPathOps.RoughlyEqualUlps(X, a.X) || !SkPathOps.RoughlyEqualUlps(Y, a.Y))
        {
            return false;
        }

        double dist = Distance(a); // OPTIMIZATION: can we compare against distSq instead ?
        double tiniest = SkPathOps.StdMin(SkPathOps.StdMin(SkPathOps.StdMin(X, a.X), Y), a.Y);
        double largest = SkPathOps.StdMax(SkPathOps.StdMax(SkPathOps.StdMax(X, a.X), Y), a.Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        return SkPathOps.AlmostDequalUlps(largest, largest + dist); // is the dist within ULPS tolerance?
    }

    public readonly bool ApproximatelyDEqual(PathPoint a)
    {
        SkDPoint dA = default;
        dA.Set(a);
        return ApproximatelyDEqual(dA);
    }

    public readonly bool ApproximatelyEqual(in SkDPoint a)
    {
        if (SkPathOps.ApproximatelyEqual(X, a.X) && SkPathOps.ApproximatelyEqual(Y, a.Y))
        {
            return true;
        }

        if (!SkPathOps.RoughlyEqualUlps(X, a.X) || !SkPathOps.RoughlyEqualUlps(Y, a.Y))
        {
            return false;
        }

        double dist = Distance(a); // OPTIMIZATION: can we compare against distSq instead ?
        double tiniest = SkPathOps.StdMin(SkPathOps.StdMin(SkPathOps.StdMin(X, a.X), Y), a.Y);
        double largest = SkPathOps.StdMax(SkPathOps.StdMax(SkPathOps.StdMax(X, a.X), Y), a.Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        return SkPathOps.AlmostPequalUlps(largest, largest + dist); // is the dist within ULPS tolerance?
    }

    public readonly bool ApproximatelyEqual(PathPoint a)
    {
        SkDPoint dA = default;
        dA.Set(a);
        return ApproximatelyEqual(dA);
    }

    public static bool ApproximatelyEqual(PathPoint a, PathPoint b)
    {
        if (SkPathOps.ApproximatelyEqual(a.X, b.X) && SkPathOps.ApproximatelyEqual(a.Y, b.Y))
        {
            return true;
        }

        if (!SkPathOps.RoughlyEqualUlps(a.X, b.X) || !SkPathOps.RoughlyEqualUlps(a.Y, b.Y))
        {
            return false;
        }

        SkDPoint dA = default;
        SkDPoint dB = default;
        dA.Set(a);
        dB.Set(b);
        double dist = dA.Distance(dB); // OPTIMIZATION: can we compare against distSq instead ?
        float tiniest = SkPathOps.StdMin(SkPathOps.StdMin(SkPathOps.StdMin(a.X, b.X), a.Y), b.Y);
        float largest = SkPathOps.StdMax(SkPathOps.StdMax(SkPathOps.StdMax(a.X, b.X), a.Y), b.Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        return SkPathOps.AlmostDequalUlps((double)largest, largest + dist); // is dist within ULPS tolerance?
    }

    // only used by testing
    public readonly bool ApproximatelyZero()
    {
        return SkPathOps.ApproximatelyZero(X) && SkPathOps.ApproximatelyZero(Y);
    }

    public readonly PathPoint AsSkPoint()
    {
        return new PathPoint(SkPathOps.SkDoubleToScalar(X), SkPathOps.SkDoubleToScalar(Y));
    }

    public readonly double Distance(in SkDPoint a)
    {
        SkDVector temp = this - a;
        return temp.Length();
    }

    public readonly double DistanceSquared(in SkDPoint a)
    {
        SkDVector temp = this - a;
        return temp.LengthSquared();
    }

    public static SkDPoint Mid(in SkDPoint a, in SkDPoint b)
    {
        SkDPoint result;
        result.X = (a.X + b.X) / 2;
        result.Y = (a.Y + b.Y) / 2;
        return result;
    }

    public readonly bool RoughlyEqual(in SkDPoint a)
    {
        if (SkPathOps.RoughlyEqual(X, a.X) && SkPathOps.RoughlyEqual(Y, a.Y))
        {
            return true;
        }

        double dist = Distance(a); // OPTIMIZATION: can we compare against distSq instead ?
        double tiniest = SkPathOps.StdMin(SkPathOps.StdMin(SkPathOps.StdMin(X, a.X), Y), a.Y);
        double largest = SkPathOps.StdMax(SkPathOps.StdMax(SkPathOps.StdMax(X, a.X), Y), a.Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        return SkPathOps.RoughlyEqualUlps(largest, largest + dist); // is the dist within ULPS tolerance?
    }

    public static bool RoughlyEqual(PathPoint a, PathPoint b)
    {
        if (!SkPathOps.RoughlyEqualUlps(a.X, b.X) && !SkPathOps.RoughlyEqualUlps(a.Y, b.Y))
        {
            return false;
        }

        SkDPoint dA = default;
        SkDPoint dB = default;
        dA.Set(a);
        dB.Set(b);
        double dist = dA.Distance(dB); // OPTIMIZATION: can we compare against distSq instead ?
        float tiniest = SkPathOps.StdMin(SkPathOps.StdMin(SkPathOps.StdMin(a.X, b.X), a.Y), b.Y);
        float largest = SkPathOps.StdMax(SkPathOps.StdMax(SkPathOps.StdMax(a.X, b.X), a.Y), b.Y);
        largest = SkPathOps.StdMax(largest, -tiniest);
        return SkPathOps.RoughlyEqualUlps((double)largest, largest + dist); // is dist within ULPS tolerance?
    }

    // very light weight check, should only be used for inequality check
    public static bool WayRoughlyEqual(PathPoint a, PathPoint b)
    {
        float largestNumber = SkPathOps.StdMax(
            SkPathOps.SkTAbs(a.X),
            SkPathOps.StdMax(SkPathOps.SkTAbs(a.Y), SkPathOps.StdMax(SkPathOps.SkTAbs(b.X), SkPathOps.SkTAbs(b.Y))));
        PathPoint diffs = a - b;
        float largestDiff = SkPathOps.StdMax(diffs.X, diffs.Y);
        return SkPathOps.RoughlyZeroWhenComparedTo(largestDiff, largestNumber);
    }
}

// Skia's fixed SkDPoint arrays. Copying the owning struct copies the points, as in C++.

[InlineArray(2)]
internal struct SkDPoint2
{
    private SkDPoint _element0;
}

[InlineArray(3)]
internal struct SkDPoint3
{
    private SkDPoint _element0;
}

[InlineArray(4)]
internal struct SkDPoint4
{
    private SkDPoint _element0;
}

[InlineArray(5)]
internal struct SkDPoint5
{
    private SkDPoint _element0;
}

[InlineArray(7)]
internal struct SkDPoint7
{
    private SkDPoint _element0;
}
