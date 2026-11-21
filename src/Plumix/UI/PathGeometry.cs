namespace Plumix.UI;

// Engine adapter: skia/src/core/SkGeometry.cpp and SkCubicClipper.cpp — the curve math SkPath's
// containment, measurement and boolean operations build on, with Skia's float operations in Skia's order.

/// <summary>Skia's quad, conic and cubic helpers (<c>SkGeometry.h</c>).</summary>
internal static class PathGeometry
{
    // SkScalarInterp.
    internal static float Interp(float a, float b, float t) => a + ((b - a) * t);

    private static PathPoint Mix(PathPoint a, PathPoint b, float t) =>
        new(((b.X - a.X) * t) + a.X, ((b.Y - a.Y) * t) + a.Y);

    private static int ValidUnitDivide(float numerator, float denominator, out float ratio)
    {
        ratio = 0.0f;
        if (numerator < 0.0f)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        if (denominator == 0.0f || numerator == 0.0f || numerator >= denominator)
        {
            return 0;
        }

        float r = numerator / denominator;
        if (float.IsNaN(r) || r == 0.0f)
        {
            return 0;
        }

        ratio = r;
        return 1;
    }

    // SkFindUnitQuadRoots: the roots of At^2 + Bt + C in (0, 1), ascending (Numerical Recipes'
    // Q = -1/2 (B + sign(B) sqrt(B^2 - 4AC)), x1 = Q / A, x2 = C / Q).
    internal static int FindUnitQuadRoots(float a, float b, float c, Span<float> roots)
    {
        if (a == 0.0f)
        {
            int count = ValidUnitDivide(-c, b, out float root);
            roots[0] = root;
            return count;
        }

        // Doubles, so R does not overflow on the way.
        double discriminant = ((double)b * b) - (4 * (double)a * c);
        if (discriminant < 0.0)
        {
            return 0;
        }

        float r = (float)Math.Sqrt(discriminant);
        if (!float.IsFinite(r))
        {
            return 0;
        }

        float q = b < 0.0f ? -(b - r) / 2 : -(b + r) / 2;
        int found = 0;
        if (ValidUnitDivide(q, a, out float first) == 1)
        {
            roots[found++] = first;
        }

        if (ValidUnitDivide(c, q, out float second) == 1)
        {
            roots[found++] = second;
        }

        if (found == 2)
        {
            if (roots[0] > roots[1])
            {
                (roots[0], roots[1]) = (roots[1], roots[0]);
            }
            else if (roots[0] == roots[1])
            {
                // A double root.
                found = 1;
            }
        }

        return found;
    }

    // SkChopQuadAt.
    internal static void ChopQuadAt(ReadOnlySpan<PathPoint> source, Span<PathPoint> destination, float t)
    {
        PathPoint p01 = Lerp(source[0], source[1], t);
        PathPoint p12 = Lerp(source[1], source[2], t);
        destination[0] = source[0];
        destination[1] = p01;
        destination[2] = Lerp(p01, p12, t);
        destination[3] = p12;
        destination[4] = source[2];
    }

    // SkGeometry's interp: a + (b - a) * t.
    private static PathPoint Lerp(PathPoint a, PathPoint b, float t) =>
        new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));

    private static bool IsNotMonotonic(float a, float b, float c)
    {
        float ab = a - b;
        float bc = b - c;
        if (ab < 0.0f)
        {
            bc = -bc;
        }

        return ab == 0.0f || bc < 0.0f;
    }

    // SkChopQuadAtYExtrema: 0 for one monotonic quad, 1 for two, in destination.
    internal static int ChopQuadAtYExtrema(ReadOnlySpan<PathPoint> source, Span<PathPoint> destination)
    {
        float a = source[0].Y;
        float b = source[1].Y;
        float c = source[2].Y;
        if (IsNotMonotonic(a, b, c))
        {
            if (ValidUnitDivide(a - b, a - b - b + c, out float t) == 1)
            {
                ChopQuadAt(source, destination, t);
                // Flatten the double extrema.
                float y = destination[2].Y;
                destination[1] = destination[1] with { Y = y };
                destination[3] = destination[3] with { Y = y };
                return 1;
            }

            // Force monotonic even without a unit divide (probably underflow).
            b = Math.Abs(a - b) < Math.Abs(b - c) ? a : c;
        }

        destination[0] = new PathPoint(source[0].X, a);
        destination[1] = new PathPoint(source[1].X, b);
        destination[2] = new PathPoint(source[2].X, c);
        return 0;
    }

    // SkChopCubicAt(src, dst, t).
    internal static void ChopCubicAt(ReadOnlySpan<PathPoint> source, Span<PathPoint> destination, float t)
    {
        if (t == 1.0f)
        {
            source[..4].CopyTo(destination);
            destination[4] = destination[5] = destination[6] = source[3];
            return;
        }

        PathPoint ab = Mix(source[0], source[1], t);
        PathPoint bc = Mix(source[1], source[2], t);
        PathPoint cd = Mix(source[2], source[3], t);
        PathPoint abc = Mix(ab, bc, t);
        PathPoint bcd = Mix(bc, cd, t);
        PathPoint abcd = Mix(abc, bcd, t);
        PathPoint p0 = source[0];
        PathPoint p3 = source[3];
        destination[0] = p0;
        destination[1] = ab;
        destination[2] = abc;
        destination[3] = abcd;
        destination[4] = bcd;
        destination[5] = cd;
        destination[6] = p3;
    }

    // SkChopCubicAt(src, dst, t0, t1): both chops at once, the middle cubic from the two de Casteljau
    // ladders.
    private static void ChopCubicAt(ReadOnlySpan<PathPoint> source, Span<PathPoint> destination, float t0, float t1)
    {
        if (t1 == 1.0f)
        {
            ChopCubicAt(source, destination, t0);
            destination[7] = destination[8] = destination[9] = source[3];
            return;
        }

        PathPoint p0 = source[0];
        PathPoint p1 = source[1];
        PathPoint p2 = source[2];
        PathPoint p3 = source[3];
        PathPoint ab0 = Mix(p0, p1, t0);
        PathPoint bc0 = Mix(p1, p2, t0);
        PathPoint cd0 = Mix(p2, p3, t0);
        PathPoint abc0 = Mix(ab0, bc0, t0);
        PathPoint bcd0 = Mix(bc0, cd0, t0);
        PathPoint abcd0 = Mix(abc0, bcd0, t0);
        PathPoint ab1 = Mix(p0, p1, t1);
        PathPoint bc1 = Mix(p1, p2, t1);
        PathPoint cd1 = Mix(p2, p3, t1);
        PathPoint abc1 = Mix(ab1, bc1, t1);
        PathPoint bcd1 = Mix(bc1, cd1, t1);
        PathPoint abcd1 = Mix(abc1, bcd1, t1);
        destination[0] = p0;
        destination[1] = ab0;
        destination[2] = abc0;
        destination[3] = abcd0;
        destination[4] = Mix(abc0, bcd0, t1);
        destination[5] = Mix(abc1, bcd1, t0);
        destination[6] = abcd1;
        destination[7] = bcd1;
        destination[8] = cd1;
        destination[9] = p3;
    }

    // SkChopCubicAt(src, dst, tValues, tCount), tValues ascending in [0, 1].
    internal static void ChopCubicAt(
        ReadOnlySpan<PathPoint> source,
        Span<PathPoint> destination,
        ReadOnlySpan<float> tValues)
    {
        if (tValues.Length == 0)
        {
            source[..4].CopyTo(destination);
            return;
        }

        Span<PathPoint> buffer = stackalloc PathPoint[4];
        int offset = 0;
        int index = 0;
        for (; index < tValues.Length - 1; index += 2)
        {
            float t0 = tValues[index];
            float t1 = tValues[index + 1];
            if (index != 0)
            {
                float lastT = tValues[index - 1];
                t0 = Math.Clamp((t0 - lastT) / (1 - lastT), 0.0f, 1.0f);
                t1 = Math.Clamp((t1 - lastT) / (1 - lastT), 0.0f, 1.0f);
            }

            ReadOnlySpan<PathPoint> chopSource = index == 0 ? source : CopyTo(buffer, destination.Slice(offset, 4));
            ChopCubicAt(chopSource, destination[offset..], t0, t1);
            offset += 6;
        }

        if (index < tValues.Length)
        {
            float t = tValues[index];
            if (index != 0)
            {
                float lastT = tValues[index - 1];
                t = Math.Clamp((t - lastT) / (1 - lastT), 0.0f, 1.0f);
            }

            ReadOnlySpan<PathPoint> chopSource = index == 0 ? source : CopyTo(buffer, destination.Slice(offset, 4));
            ChopCubicAt(chopSource, destination[offset..], t);
        }
    }

    private static ReadOnlySpan<PathPoint> CopyTo(Span<PathPoint> buffer, ReadOnlySpan<PathPoint> points)
    {
        points.CopyTo(buffer);
        return buffer;
    }

    // SkFindCubicExtrema: A, B, C divided by 3.
    internal static int FindCubicExtrema(float a, float b, float c, float d, Span<float> tValues)
    {
        float coefficientA = d - a + (3 * (b - c));
        float coefficientB = 2 * (a - b - b + c);
        float coefficientC = b - a;
        return FindUnitQuadRoots(coefficientA, coefficientB, coefficientC, tValues);
    }

    // SkChopCubicAtYExtrema: the number of chops (0..2); destination holds 3 * chops + 4 points.
    internal static int ChopCubicAtYExtrema(ReadOnlySpan<PathPoint> source, Span<PathPoint> destination)
    {
        Span<float> tValues = stackalloc float[2];
        int roots = FindCubicExtrema(source[0].Y, source[1].Y, source[2].Y, source[3].Y, tValues);
        ChopCubicAt(source, destination, tValues[..roots]);
        if (roots > 0)
        {
            // Make the Y extrema flat.
            FlattenCubicExtrema(destination, 0);
            if (roots == 2)
            {
                FlattenCubicExtrema(destination, 3);
            }
        }

        return roots;
    }

    private static void FlattenCubicExtrema(Span<PathPoint> points, int start)
    {
        float y = points[start + 3].Y;
        points[start + 2] = points[start + 2] with { Y = y };
        points[start + 4] = points[start + 4] with { Y = y };
    }

    // SkCubicClipper::ChopMonoAtY: bisection to 1/65536 in t.
    internal static bool ChopMonoCubicAtY(ReadOnlySpan<PathPoint> points, float y, out float t)
    {
        float y0 = points[0].Y - y;
        float y1 = points[1].Y - y;
        float y2 = points[2].Y - y;
        float y3 = points[3].Y - y;
        float tNegative;
        float tPositive;
        if (y0 < 0.0f)
        {
            if (y3 < 0.0f)
            {
                t = 0.0f;
                return false;
            }

            tNegative = 0.0f;
            tPositive = 1.0f;
        }
        else if (y0 > 0.0f)
        {
            if (y3 > 0.0f)
            {
                t = 0.0f;
                return false;
            }

            tNegative = 1.0f;
            tPositive = 0.0f;
        }
        else
        {
            t = 0.0f;
            return true;
        }

        const float tolerance = 1.0f / 65536;
        do
        {
            float tMid = (tPositive + tNegative) / 2;
            float y01 = Interp(y0, y1, tMid);
            float y12 = Interp(y1, y2, tMid);
            float y23 = Interp(y2, y3, tMid);
            float y012 = Interp(y01, y12, tMid);
            float y123 = Interp(y12, y23, tMid);
            float y0123 = Interp(y012, y123, tMid);
            if (y0123 == 0.0f)
            {
                t = tMid;
                return true;
            }

            if (y0123 < 0.0f)
            {
                tNegative = tMid;
            }
            else
            {
                tPositive = tMid;
            }
        }
        while (!(Math.Abs(tPositive - tNegative) <= tolerance));

        t = (tNegative + tPositive) / 2;
        return true;
    }

    // SkConic::chopAt: false when the halves are not finite.
    internal static bool ChopConicAt(
        ReadOnlySpan<PathPoint> points,
        float weight,
        float t,
        Span<PathPoint> destination,
        out float weight0,
        out float weight1)
    {
        // Map to homogeneous coordinates and interpolate each of x, y, z.
        (float x0, float y0, float z0) = (points[0].X * 1, points[0].Y * 1, 1.0f);
        (float x1, float y1, float z1) = (points[1].X * weight, points[1].Y * weight, weight);
        (float x2, float y2, float z2) = (points[2].X * 1, points[2].Y * 1, 1.0f);
        (float ax, float ay, float az) = (Interp(x0, x1, t), Interp(y0, y1, t), Interp(z0, z1, t));
        (float cx, float cy, float cz) = (Interp(x1, x2, t), Interp(y1, y2, t), Interp(z1, z2, t));
        (float bx, float by, float bz) = (Interp(ax, cx, t), Interp(ay, cy, t), Interp(az, cz, t));
        destination[0] = points[0];
        destination[1] = new PathPoint(ax / az, ay / az);
        destination[2] = new PathPoint(bx / bz, by / bz);
        destination[3] = new PathPoint(cx / cz, cy / cz);
        destination[4] = points[2];
        // In standard form w0 == w2 == 1: w1 /= sqrt(w0 * w2), where w0 (first half) and w2 (second) are 1.
        float root = MathF.Sqrt(bz);
        weight0 = az / root;
        weight1 = cz / root;
        for (int index = 0; index < 5; index++)
        {
            if (!float.IsFinite(destination[index].X) || !float.IsFinite(destination[index].Y))
            {
                return false;
            }
        }

        return float.IsFinite(weight0) && float.IsFinite(weight1);
    }

    // SkConic::chopAtYExtrema.
    internal static bool ChopConicAtYExtrema(
        ReadOnlySpan<PathPoint> points,
        float weight,
        Span<PathPoint> destination,
        out float weight0,
        out float weight1)
    {
        weight0 = weight1 = 0.0f;
        // conic_find_extrema over y: the derivative's coefficients.
        float p20 = points[2].Y - points[0].Y;
        float p10 = points[1].Y - points[0].Y;
        float wP10 = weight * p10;
        Span<float> roots = stackalloc float[2];
        int count = FindUnitQuadRoots((weight * p20) - p20, p20 - (2 * wP10), wP10, roots);
        if (count != 1 || !ChopConicAt(points, weight, roots[0], destination, out weight0, out weight1))
        {
            return false;
        }

        // Clean up the middle, since t was meant to be a y extremum.
        float y = destination[2].Y;
        destination[1] = destination[1] with { Y = y };
        destination[3] = destination[3] with { Y = y };
        return true;
    }

    // SkEvalQuadTangentAt.
    internal static PathPoint QuadTangentAt(ReadOnlySpan<PathPoint> points, float t)
    {
        // The derivative is zero at an end whose control point coincides with it; use the chord.
        if ((t == 0.0f && points[0] == points[1]) || (t == 1.0f && points[1] == points[2]))
        {
            return points[2] - points[0];
        }

        PathPoint b = points[1] - points[0];
        PathPoint a = points[2] - points[1] - b;
        var tangent = new PathPoint((a.X * t) + b.X, (a.Y * t) + b.Y);
        return tangent + tangent;
    }

    // SkConic::evalTangentAt.
    internal static PathPoint ConicTangentAt(ReadOnlySpan<PathPoint> points, float weight, float t)
    {
        if ((t == 0.0f && points[0] == points[1]) || (t == 1.0f && points[1] == points[2]))
        {
            return points[2] - points[0];
        }

        PathPoint p20 = points[2] - points[0];
        PathPoint p10 = points[1] - points[0];
        PathPoint c = new(weight * p10.X, weight * p10.Y);
        PathPoint a = new((weight * p20.X) - p20.X, (weight * p20.Y) - p20.Y);
        PathPoint b = p20 - c - c;
        return new PathPoint((((a.X * t) + b.X) * t) + c.X, (((a.Y * t) + b.Y) * t) + c.Y);
    }

    // SkEvalCubicAt's tangent.
    internal static PathPoint CubicTangentAt(ReadOnlySpan<PathPoint> points, float t)
    {
        // A zero derivative at an end whose neighbouring control point coincides with it: use the next
        // control point, or the end points.
        if ((t == 0.0f && points[0] == points[1]) || (t == 1.0f && points[2] == points[3]))
        {
            PathPoint tangent = t == 0.0f ? points[2] - points[0] : points[3] - points[1];
            return tangent.X == 0.0f && tangent.Y == 0.0f ? points[3] - points[0] : tangent;
        }

        PathPoint p0 = points[0];
        PathPoint p1 = points[1];
        PathPoint p2 = points[2];
        PathPoint p3 = points[3];
        var a = new PathPoint(p3.X + (3 * (p1.X - p2.X)) - p0.X, p3.Y + (3 * (p1.Y - p2.Y)) - p0.Y);
        PathPoint twiceP1 = p1 + p1;
        PathPoint inner = p2 - twiceP1 + p0;
        PathPoint b = inner + inner;
        PathPoint c = p1 - p0;
        return new PathPoint((((a.X * t) + b.X) * t) + c.X, (((a.Y * t) + b.Y) * t) + c.Y);
    }

    // SkPath.cpp's eval_cubic_pts: one coordinate of a cubic at t.
    internal static float EvalCubic(float c0, float c1, float c2, float c3, float t)
    {
        float a = c3 + (3 * (c1 - c2)) - c0;
        float b = 3 * (c2 - c1 - c1 + c0);
        float c = 3 * (c1 - c0);
        float d = c0;
        return (((((a * t) + b) * t) + c) * t) + d;
    }

    // SkPath.cpp's poly_eval: At^2 + Bt + C, in double.
    internal static double PolyEval(double a, double b, double c, double t) => (((a * t) + b) * t) + c;

    // SkPath.cpp's conic_eval_numerator for one coordinate (src[0], src[2], src[4] are its three values).
    internal static double ConicEvalNumerator(float value0, float value1, float value2, float weight, float t)
    {
        float src2W = value1 * weight;
        float c = value0;
        float a = value2 - (2 * src2W) + c;
        float b = 2 * (src2W - c);
        return PolyEval(a, b, c, t);
    }

    internal static double ConicEvalDenominator(float weight, float t)
    {
        float b = 2 * (weight - 1);
        const float c = 1;
        float a = -b;
        return PolyEval(a, b, c, t);
    }
}
