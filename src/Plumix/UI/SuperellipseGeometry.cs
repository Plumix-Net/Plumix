using Avalonia;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (_NativeRSuperellipse.contains)
// Engine parity source: flutter/engine/src/flutter/impeller/geometry/round_superellipse_param.cc
// The native containment path uses float32 parameters and raw radii, independently of path flattening.
internal static class SuperellipseGeometry
{
    public static bool Contains(RSuperellipse shape, Point point)
    {
        float left = Narrow(shape.Left);
        float top = Narrow(shape.Top);
        float right = Narrow(shape.Right);
        float bottom = Narrow(shape.Bottom);
        (left, right) = (MathF.Min(left, right), MathF.Max(left, right));
        (top, bottom) = (MathF.Min(top, bottom), MathF.Max(top, bottom));
        V p = new(Narrow(point.X), Narrow(point.Y));
        if (!(p.X >= left && p.X < right && p.Y >= top && p.Y < bottom)) { return false; }

        V tl = FromRadius(shape.TopLeft);
        V tr = FromRadius(shape.TopRight);
        V br = FromRadius(shape.BottomRight);
        V bl = FromRadius(shape.BottomLeft);
        if (tl == tr && tr == br && br == bl && tl.X > 0 && tl.Y > 0)
        {
            return CornerContains(ComputeQuadrant(
                new V(left * 0.5f + right * 0.5f, top * 0.5f + bottom * 0.5f), new V(right, top), tr, new V(-1, 1)),
                p, checkQuadrant: false);
        }

        float topSplit = Split(left, right, tl.X, tr.X);
        float rightSplit = Split(top, bottom, tr.Y, br.Y);
        float bottomSplit = Split(left, right, bl.X, br.X);
        float leftSplit = Split(top, bottom, tl.Y, bl.Y);
        return CornerContains(ComputeQuadrant(new V(topSplit, rightSplit), new V(right, top), tr, new V(1, -1)), p)
               && CornerContains(ComputeQuadrant(
                   new V(bottomSplit, rightSplit), new V(right, bottom), br, new V(1, 1)), p)
               && CornerContains(ComputeQuadrant(
                   new V(bottomSplit, leftSplit), new V(left, bottom), bl, new V(-1, 1)), p)
               && CornerContains(ComputeQuadrant(new V(topSplit, leftSplit), new V(left, top), tl, new V(-1, -1)), p);
    }

    private static float Narrow(double value) => double.IsFinite(value)
        ? Math.Clamp((float)value, -float.MaxValue, float.MaxValue) : (float)value;
    private static V FromRadius(Radius radius) => new(Narrow(radius.X), Narrow(radius.Y));
    private static float Split(float left, float right, float leftRadius, float rightRadius) =>
        leftRadius == 0 && rightRadius == 0 ? (left + right) * 0.5f
            : (left * rightRadius + right * leftRadius) / (leftRadius + rightRadius);

    private static Quadrant ComputeQuadrant(V center, V corner, V inputRadii, V fallbackSign)
    {
        V cornerVector = corner - center;
        V radii = new(MathF.Min(MathF.Abs(inputRadii.X), MathF.Abs(cornerVector.X)),
            MathF.Min(MathF.Abs(inputRadii.Y), MathF.Abs(cornerVector.Y)));
        float radius = MathF.Min(radii.X, radii.Y);
        V forwardScale = radius == 0 ? new V(1, 1) : radii / radius;
        V halfSize = cornerVector.Abs() / forwardScale;
        V signedScale = (cornerVector / halfSize).ReplaceNaN(fallbackSign);
        float offset = halfSize.X - halfSize.Y;
        return new Quadrant(center, signedScale,
            ComputeOctant(new V(0, -offset), halfSize.X, radius),
            ComputeOctant(new V(offset, 0), halfSize.Y, radius));
    }

    private static Octant ComputeOctant(V center, float a, float radius)
    {
        if (radius <= 1e-3f) { return new Octant(center, a, 0, new V(a, a), default); }
        float ratio = a * 2 / radius;
        float gap = 0.29289321881f * radius;
        (float n, float xRatio) = ComputeNAndXj(ratio);
        float xJ = xRatio * a;
        float yJ = MathF.Pow(1 - MathF.Pow(xRatio, n), 1 / n) * a;
        float tangent = MathF.Pow(xJ / yJ, n - 1);
        float diagonal = (xJ - tangent * yJ) / (1 - tangent);
        float circleRadius = (float)((a - diagonal - gap) * Math.Sqrt(2));
        V midpoint = new(a - gap, a - gap);
        V join = new(xJ, yJ);
        V delta = midpoint - join;
        V middle = (join + midpoint) / 2;
        V perpendicular = new(-delta.Y, delta.X);
        float halfDistance = delta.Length / 2;
        float centerDistance = MathF.Sqrt(circleRadius * circleRadius - halfDistance * halfDistance);
        V circleCenter = middle - perpendicular.Normalized() * centerDistance;
        return new Octant(center, a, n, join, circleCenter);
    }

    private static (float N, float XRatio) ComputeNAndXj(float ratio)
    {
        var table = RoundSuperellipsePathBuilder.NAndXjTable;
        if (ratio > 5)
        {
            float n = 1.559599389f * (ratio - 5) + (float)table[^1].N;
            float k = 0.522807185f * (ratio - 5) + (float)table[^1].Kxj;
            return (n, 1 - 1 / k);
        }

        ratio = Math.Clamp(ratio, 2, 5);
        float steps = ratio < 2.5f ? (ratio - 2) * 10 : (ratio - 2.5f) * 2 + 5;
        int left = Math.Clamp((int)MathF.Floor(steps), 0, table.Length - 2);
        float fraction = steps - left;
        float exponent = (1 - fraction) * (float)table[left].N + fraction * (float)table[left + 1].N;
        float kxj = (1 - fraction) * (float)table[left].Kxj + fraction * (float)table[left + 1].Kxj;
        return (exponent, 1 - 1 / kxj);
    }

    private static bool CornerContains(Quadrant param, V p, bool checkQuadrant = true)
    {
        V normalized = (p - param.Offset) / param.SignedScale;
        if (checkQuadrant)
        {
            if (normalized.X < 0 || normalized.Y < 0) { return true; }
        }
        else { normalized = normalized.Abs(); }
        if (param.Top.N < 2 || param.Right.N < 2)
        {
            float dx = param.Right.Offset.X + param.Right.A - normalized.X;
            float dy = param.Top.Offset.Y + param.Top.A - normalized.Y;
            return (dx > 0 || (dx == 0 && param.SignedScale.X < 0))
                   && (dy > 0 || (dy == 0 && param.SignedScale.Y < 0));
        }
        return OctantContains(param.Top, normalized - param.Top.Offset)
               && OctantContains(param.Right, (normalized - param.Right.Offset).Flipped());
    }

    private static bool OctantContains(Octant param, V p)
    {
        if (p.X < 0 || p.Y < 0 || p.Y < p.X) { return true; }
        if (p.X <= param.CircleStart.X)
        {
            V normalized = p / param.A;
            return MathF.Pow(normalized.X, param.N) + MathF.Pow(normalized.Y, param.N) <= 1;
        }
        V radius = param.CircleStart - param.CircleCenter;
        V point = p - param.CircleCenter;
        return point.LengthSquared < radius.LengthSquared;
    }

    private readonly record struct Quadrant(V Offset, V SignedScale, Octant Top, Octant Right);
    private readonly record struct Octant(V Offset, float A, float N, V CircleStart, V CircleCenter);
    private readonly record struct V(float X, float Y)
    {
        public float LengthSquared => X * X + Y * Y;
        public float Length => MathF.Sqrt(LengthSquared);
        public V Abs() => new(MathF.Abs(X), MathF.Abs(Y));
        public V Flipped() => new(Y, X);
        public V Normalized() => Length == 0 ? default : this / Length;
        public V ReplaceNaN(V fallback) => new(float.IsNaN(X) ? fallback.X : X, float.IsNaN(Y) ? fallback.Y : Y);
        public static V operator +(V a, V b) => new(a.X + b.X, a.Y + b.Y);
        public static V operator -(V a, V b) => new(a.X - b.X, a.Y - b.Y);
        public static V operator *(V a, float b) => new(a.X * b, a.Y * b);
        public static V operator /(V a, float b) => new(a.X / b, a.Y / b);
        public static V operator /(V a, V b) => new(a.X / b.X, a.Y / b.Y);
    }
}
