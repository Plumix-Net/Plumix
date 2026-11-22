using System.Numerics;
using System.Runtime.InteropServices;

namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of the skia/include/private/base and skia/include/core helpers the pathops sources
// use (SkFloatBits.h, SkFloatingPoint.h, SkMath.h, SkScalar.h, SkTPin.h, SkTemplates.h' SkTAbs, src/base/SkMathPriv.h'
// SkNextLog2) and of std::min/std::max, at chrome/m119 (the Skia inside SkiaSharp 3.119), behind Path.Combine.
// Line for line: Skia's names, operation order and float/double types are kept.

internal static partial class SkPathOps
{
    public const float SkScalarMax = 3.402823466e+38f;
    public const float SkScalarInfinity = float.PositiveInfinity;
    public const int SkMaxS32 = int.MaxValue;
    public const int SkMinS32 = -SkMaxS32; // not int.MinValue: SK_MinS32 is -SK_MaxS32
    public const int SkNaN32 = int.MinValue;

    public static float SkDoubleToScalar(double x)
    {
        return (float)x;
    }

    // SkPoint's operator== (include/core/SkPoint.h). PathPoint's record equality uses float.Equals, which calls
    // NaN equal to NaN; Skia compares with ==.
    public static bool SkPointEquals(PathPoint a, PathPoint b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool SkScalarIsFinite(float x)
    {
        return float.IsFinite(x);
    }

    // sk_floats_are_finite: the product of every value with 0 stays 0 only if all are finite.
    public static bool SkScalarsAreFinite(ReadOnlySpan<float> array, int count)
    {
        float prod = 0;
        for (int i = 0; i < count; ++i)
        {
            prod *= array[i];
        }

        return prod == 0;
    }

    public static float SkScalarAbs(float x)
    {
        return MathF.Abs(x);
    }

    public static double SkIeeeDoubleDivide(double numer, double denom)
    {
        return numer / denom;
    }

    // std::min returns (b < a) ? b : a; std::max returns (a < b) ? b : a. Math.Min/Max differ on NaN and +-0.
    public static double StdMin(double a, double b)
    {
        return (b < a) ? b : a;
    }

    public static double StdMax(double a, double b)
    {
        return (a < b) ? b : a;
    }

    public static float StdMin(float a, float b)
    {
        return (b < a) ? b : a;
    }

    public static float StdMax(float a, float b)
    {
        return (a < b) ? b : a;
    }

    public static int StdMin(int a, int b)
    {
        return (b < a) ? b : a;
    }

    public static int StdMax(int a, int b)
    {
        return (a < b) ? b : a;
    }

    public static int SkTAbs(int value)
    {
        if (value < 0)
        {
            value = -value;
        }

        return value;
    }

    public static float SkTAbs(float value)
    {
        if (value < 0)
        {
            value = -value;
        }

        return value;
    }

    public static double SkTAbs(double value)
    {
        if (value < 0)
        {
            value = -value;
        }

        return value;
    }

    // Unlike std::clamp(), SkTPin() always returns a value between lo and hi.
    // If x is NaN, SkTPin() returns lo but std::clamp() returns NaN.
    public static double SkTPin(double x, double lo, double hi)
    {
        return StdMax(lo, StdMin(x, hi));
    }

    public static float SkTPin(float x, float lo, float hi)
    {
        return StdMax(lo, StdMin(x, hi));
    }

    public static int SkSignBitTo2sCompliment(int x)
    {
        if (x < 0)
        {
            x &= 0x7FFFFFFF;
            x = -x;
        }

        return x;
    }

    // Helper to see a float as its bit pattern (w/o aliasing warnings)
    public static int SkFloat2Bits(float x)
    {
        return BitConverter.SingleToInt32Bits(x);
    }

    public static int SkFloatAs2sCompliment(float x)
    {
        return SkSignBitTo2sCompliment(SkFloat2Bits(x));
    }

    // SkNextLog2(1) -> 0, SkNextLog2(2) -> 1, SkNextLog2(3) -> 2, SkNextLog2(4) -> 2, SkNextLog2(5) -> 3
    public static int SkNextLog2(uint value)
    {
        return 32 - BitOperations.LeadingZeroCount(value - 1);
    }

    // Skia's &pts[0].fX: the points as a run of doubles, x at even and y at odd indices. &pts[0].fY is
    // Doubles(pts)[1..].
    public static ReadOnlySpan<double> Doubles(ReadOnlySpan<SkDPoint> pts)
    {
        return MemoryMarshal.Cast<SkDPoint, double>(pts);
    }

    // The writable form of Doubles, for Skia's double* destinations.
    public static Span<double> MutableDoubles(Span<SkDPoint> pts)
    {
        return MemoryMarshal.Cast<SkDPoint, double>(pts);
    }
}
