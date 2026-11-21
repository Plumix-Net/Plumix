namespace Plumix.UI;

// Engine adapter: skia/src/core/SkMatrix.cpp (the 3x3 matrix Skia maps path points with). Path builds its
// arcs and transforms its points with the same float operations, in the same order, as SkPath does, so a
// path built here is point for point the path the engine's Skia builds (PathSkiaParityTests).

/// <summary>Skia's <c>SkMatrix</c>: a row-major 3x3 float matrix with its point-mapping rules.</summary>
internal struct PathMatrix
{
    // SkMatrix's fMat order.
    private const int ScaleX = 0;
    private const int SkewX = 1;
    private const int TransX = 2;
    private const int SkewY = 3;
    private const int ScaleY = 4;
    private const int TransY = 5;
    private const int Persp0 = 6;
    private const int Persp1 = 7;
    private const int Persp2 = 8;

    private float _m0, _m1, _m2, _m3, _m4, _m5, _m6, _m7, _m8;

    internal static PathMatrix Identity => new() { _m0 = 1.0f, _m4 = 1.0f, _m8 = 1.0f };

    internal static PathMatrix FromValues(
        float scaleX, float skewX, float transX,
        float skewY, float scaleY, float transY,
        float persp0, float persp1, float persp2) => new()
    {
        _m0 = scaleX, _m1 = skewX, _m2 = transX,
        _m3 = skewY, _m4 = scaleY, _m5 = transY,
        _m6 = persp0, _m7 = persp1, _m8 = persp2,
    };

    internal static PathMatrix Translate(float dx, float dy) =>
        FromValues(1.0f, 0.0f, dx, 0.0f, 1.0f, dy, 0.0f, 0.0f, 1.0f);

    internal static PathMatrix Scale(float sx, float sy) =>
        FromValues(sx, 0.0f, 0.0f, 0.0f, sy, 0.0f, 0.0f, 0.0f, 1.0f);

    // SkMatrix::setSinCos.
    internal static PathMatrix SinCos(float sin, float cos) =>
        FromValues(cos, -sin, 0.0f, sin, cos, 0.0f, 0.0f, 0.0f, 1.0f);

    // SkMatrix::setRotate.
    internal static PathMatrix Rotate(float degrees)
    {
        float radians = PathMath.DegreesToRadians(degrees);
        return SinCos(PathMath.SinSnapToZero(radians), PathMath.CosSnapToZero(radians));
    }

    internal readonly float this[int index] => index switch
    {
        ScaleX => _m0,
        SkewX => _m1,
        TransX => _m2,
        SkewY => _m3,
        ScaleY => _m4,
        TransY => _m5,
        Persp0 => _m6,
        Persp1 => _m7,
        _ => _m8,
    };

    internal readonly bool HasPerspective => _m6 != 0.0f || _m7 != 0.0f || _m8 != 1.0f;

    private readonly bool HasTranslate => _m2 != 0.0f || _m5 != 0.0f;

    private readonly bool IsAffine => _m1 != 0.0f || _m3 != 0.0f;

    private readonly bool HasScale => _m0 != 1.0f || _m4 != 1.0f;

    internal readonly bool IsIdentity => !HasPerspective && !HasTranslate && !IsAffine && !HasScale;

    // SkMatrix::rectStaysRect: no perspective, and either the primary diagonal is all non-zero with no
    // skew, or the primary diagonal is all zero and the secondary all non-zero.
    internal readonly bool RectStaysRect => !HasPerspective && (IsAffine
        ? _m0 == 0.0f && _m4 == 0.0f && _m1 != 0.0f && _m3 != 0.0f
        : _m0 != 0.0f && _m4 != 0.0f);

    // SkMatrix::setConcat(a, b): a * b, b applied first.
    internal static PathMatrix Concat(PathMatrix a, PathMatrix b)
    {
        if (a.IsIdentity)
        {
            return b;
        }

        if (b.IsIdentity)
        {
            return a;
        }

        if (!a.HasPerspective && !b.HasPerspective && !a.IsAffine && !b.IsAffine)
        {
            return FromValues(
                a._m0 * b._m0, 0.0f, (a._m0 * b._m2) + a._m2,
                0.0f, a._m4 * b._m4, (a._m4 * b._m5) + a._m5,
                0.0f, 0.0f, 1.0f);
        }

        if (a.HasPerspective || b.HasPerspective)
        {
            return FromValues(
                RowColumn3(a._m0, a._m1, a._m2, b._m0, b._m3, b._m6),
                RowColumn3(a._m0, a._m1, a._m2, b._m1, b._m4, b._m7),
                RowColumn3(a._m0, a._m1, a._m2, b._m2, b._m5, b._m8),
                RowColumn3(a._m3, a._m4, a._m5, b._m0, b._m3, b._m6),
                RowColumn3(a._m3, a._m4, a._m5, b._m1, b._m4, b._m7),
                RowColumn3(a._m3, a._m4, a._m5, b._m2, b._m5, b._m8),
                RowColumn3(a._m6, a._m7, a._m8, b._m0, b._m3, b._m6),
                RowColumn3(a._m6, a._m7, a._m8, b._m1, b._m4, b._m7),
                RowColumn3(a._m6, a._m7, a._m8, b._m2, b._m5, b._m8));
        }

        return FromValues(
            MulAddMul(a._m0, b._m0, a._m1, b._m3),
            MulAddMul(a._m0, b._m1, a._m1, b._m4),
            MulAddMul(a._m0, b._m2, a._m1, b._m5) + a._m2,
            MulAddMul(a._m3, b._m0, a._m4, b._m3),
            MulAddMul(a._m3, b._m1, a._m4, b._m4),
            MulAddMul(a._m3, b._m2, a._m4, b._m5) + a._m5,
            0.0f, 0.0f, 1.0f);
    }

    internal readonly PathMatrix PreConcat(PathMatrix other) => Concat(this, other);

    internal readonly PathMatrix PostConcat(PathMatrix other) => Concat(other, this);

    internal readonly PathMatrix PreRotate(float degrees) => PreConcat(Rotate(degrees));

    // SkMatrix::preScale.
    internal readonly PathMatrix PreScale(float sx, float sy)
    {
        if (sx == 1.0f && sy == 1.0f)
        {
            return this;
        }

        PathMatrix result = this;
        result._m0 *= sx;
        result._m3 *= sx;
        result._m6 *= sx;
        result._m1 *= sy;
        result._m4 *= sy;
        result._m7 *= sy;
        return result;
    }

    // SkMatrix::postScale.
    internal readonly PathMatrix PostScale(float sx, float sy) =>
        sx == 1.0f && sy == 1.0f ? this : PostConcat(Scale(sx, sy));

    // SkMatrix::postTranslate.
    internal readonly PathMatrix PostTranslate(float dx, float dy)
    {
        if (HasPerspective)
        {
            return PostConcat(Translate(dx, dy));
        }

        PathMatrix result = this;
        result._m2 += dx;
        result._m5 += dy;
        return result;
    }

    // SkMatrix::mapPoints, through the proc its type mask selects (Identity/Trans/Scale/Affine/Persp_pts).
    internal readonly PathPoint Map(PathPoint point)
    {
        float x = point.X;
        float y = point.Y;
        if (HasPerspective)
        {
            float mappedX = (x * _m0) + ((y * _m1) + _m2);
            float mappedY = (x * _m3) + ((y * _m4) + _m5);
            float z = (x * _m6) + ((y * _m7) + _m8);
            if (z != 0.0f)
            {
                z = 1.0f / z;
            }

            return new PathPoint(mappedX * z, mappedY * z);
        }

        if (IsAffine)
        {
            return new PathPoint((x * _m0) + (y * _m1) + _m2, (y * _m4) + (x * _m3) + _m5);
        }

        if (HasScale)
        {
            return new PathPoint((x * _m0) + _m2, (y * _m4) + _m5);
        }

        return HasTranslate ? new PathPoint(x + _m2, y + _m5) : point;
    }

    // SkMatrix::setConcat's helpers.
    private static float MulAddMul(float a, float b, float c, float d) => (float)(((double)a * b) + ((double)c * d));

    private static float RowColumn3(float r0, float r1, float r2, float c0, float c1, float c2) =>
        (float)(((double)r0 * c0) + ((double)r1 * c1) + ((double)r2 * c2));
}

/// <summary>A path point: Skia's <c>SkPoint</c>, two floats compared with float equality.</summary>
internal readonly record struct PathPoint(float X, float Y)
{
    // SkPoint::operator==: float comparison, so a NaN coordinate is unequal to everything (the record's
    // default equality would call NaN equal to NaN).
    public bool Equals(PathPoint other) => X == other.X && Y == other.Y;

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static PathPoint operator +(PathPoint a, PathPoint b) => new(a.X + b.X, a.Y + b.Y);

    public static PathPoint operator -(PathPoint a, PathPoint b) => new(a.X - b.X, a.Y - b.Y);

    internal PathPoint Scale(float scale) => new(X * scale, Y * scale);

    // SkPoint::setLength (set_point_length<false>): the length is computed in double.
    internal PathPoint WithLength(float length)
    {
        double magnitude = Math.Sqrt(((double)X * X) + ((double)Y * Y));
        double scale = length / magnitude;
        float x = (float)(X * scale);
        float y = (float)(Y * scale);
        return !float.IsFinite(x) || !float.IsFinite(y) || (x == 0.0f && y == 0.0f) ? default : new PathPoint(x, y);
    }
}

/// <summary>The scalar helpers SkPath's builders use (include/core/SkScalar.h).</summary>
internal static class PathMath
{
    internal const float Pi = 3.14159265f;
    internal const float Root2Over2 = 0.707106781f;
    internal const float NearlyZero = 1.0f / (1 << 12);

    // SK_ScalarSinCosNearlyZero: the snap tolerance of SkScalarSinSnapToZero/CosSnapToZero.
    private const float SinCosNearlyZero = 1.0f / (1 << 16);

    internal static float DegreesToRadians(float degrees) => degrees * (Pi / 180);

    internal static bool IsNearlyZero(float value) => Math.Abs(value) <= NearlyZero;

    internal static bool NearlyEqual(float a, float b) => Math.Abs(a - b) <= NearlyZero;

    internal static float SinSnapToZero(float radians)
    {
        float value = MathF.Sin(radians);
        return Math.Abs(value) <= SinCosNearlyZero ? 0.0f : value;
    }

    internal static float CosSnapToZero(float radians)
    {
        float value = MathF.Cos(radians);
        return Math.Abs(value) <= SinCosNearlyZero ? 0.0f : value;
    }

    // SkScalarRoundToScalar.
    internal static float Round(float value) => MathF.Floor(value + 0.5f);

    // sk_float_midpoint, as SkRect::centerX/centerY compute it.
    internal static float Midpoint(float a, float b) => (float)(((double)a + b) * 0.5);
}
