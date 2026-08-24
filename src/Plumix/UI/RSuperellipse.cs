using Avalonia;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (RSuperellipse)

/// <summary>A rectangle whose corners use Flutter's continuous rounded-superellipse contour.</summary>
public readonly record struct RSuperellipse
{
    public RSuperellipse(
        Rect rect,
        Radius topLeft,
        Radius topRight,
        Radius bottomRight,
        Radius bottomLeft)
    {
        Rect = rect;
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public Rect Rect { get; }

    public Radius TopLeft { get; }

    public Radius TopRight { get; }

    public Radius BottomRight { get; }

    public Radius BottomLeft { get; }

    public Rect OuterRect => Rect;

    public double Width => Rect.Width;

    public double Height => Rect.Height;

    public static RSuperellipse Zero { get; } = new(default, Radius.Zero, Radius.Zero, Radius.Zero, Radius.Zero);

    public static RSuperellipse FromRectAndRadius(Rect rect, Radius radius) =>
        new(rect, radius, radius, radius, radius);

    public static RSuperellipse FromRectAndCorners(
        Rect rect,
        Radius? topLeft = null,
        Radius? topRight = null,
        Radius? bottomRight = null,
        Radius? bottomLeft = null) => new(
        rect,
        topLeft ?? Radius.Zero,
        topRight ?? Radius.Zero,
        bottomRight ?? Radius.Zero,
        bottomLeft ?? Radius.Zero);

    public RSuperellipse Shift(Point offset) => new(
        new Rect(Rect.Position + (Vector)offset, Rect.Size),
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft);

    public RSuperellipse Inflate(double delta) => new(
        Rect.Inflate(delta),
        InflateRadius(TopLeft, delta),
        InflateRadius(TopRight, delta),
        InflateRadius(BottomRight, delta),
        InflateRadius(BottomLeft, delta));

    public RSuperellipse Deflate(double delta) => Inflate(-delta);

    public RSuperellipse ScaleRadii()
    {
        double scale = 1.0;
        scale = MinScale(scale, BottomLeft.Y + TopLeft.Y, Height);
        scale = MinScale(scale, TopLeft.X + TopRight.X, Width);
        scale = MinScale(scale, TopRight.Y + BottomRight.Y, Height);
        scale = MinScale(scale, BottomRight.X + BottomLeft.X, Width);
        if (scale >= 1.0)
        {
            return this;
        }

        return new RSuperellipse(
            Rect,
            Radius.Elliptical(TopLeft.X * scale, TopLeft.Y * scale),
            Radius.Elliptical(TopRight.X * scale, TopRight.Y * scale),
            Radius.Elliptical(BottomRight.X * scale, BottomRight.Y * scale),
            Radius.Elliptical(BottomLeft.X * scale, BottomLeft.Y * scale));
    }

    public bool Contains(Point point) => Rect.Contains(point) && ToPath().Contains(point);

    public Path ToPath()
    {
        var path = new Path();
        RoundSuperellipsePathBuilder.AddToPath(path, ScaleRadii());
        return path;
    }

    private static Radius InflateRadius(Radius radius, double delta) =>
        Radius.Elliptical(Math.Max(0.0, radius.X + delta), Math.Max(0.0, radius.Y + delta));

    private static double MinScale(double scale, double sum, double limit)
    {
        return sum > limit && sum != 0.0
            ? Math.Min(scale, limit / sum)
            : scale;
    }
}

// Port of Flutter engine's impeller::RoundSuperellipseParam path dispatch. The engine deliberately
// joins a superellipse segment to a circular corner arc; a simple RRect or fixed-exponent curve is not equivalent.
internal static class RoundSuperellipsePathBuilder
{
    private const double GapFactor = 0.29289321881;
    private const double CloseEnough = 1e-5;

    private static readonly (double N, double Kxj)[] NAndXjTable =
    [
        (2.00000000, 1.13276676),
        (2.18349805, 1.20311921),
        (2.33888662, 1.28698796),
        (2.48660575, 1.36351941),
        (2.62226596, 1.44717976),
        (2.75148990, 1.53385819),
        (3.36298265, 1.98288283),
        (4.08649929, 2.23811846),
        (4.85481134, 2.47563463),
        (5.62945551, 2.72948597),
        (6.43023796, 2.98020421),
    ];

    private static readonly (double First, double Second)[] BezierFactorTable =
    [
        (0.7078, 8.3194),
        (0.7895, 2.4523),
        (0.8379, 1.8528),
        (0.8701, 1.6891),
        (0.8932, 1.5806),
        (0.9107, 1.5043),
        (0.9244, 1.4470),
        (0.9355, 1.4037),
        (0.9448, 1.3701),
        (0.9526, 1.3431),
        (0.9594, 1.3212),
        (0.9653, 1.3032),
        (0.9705, 1.2880),
    ];

    public static void AddToPath(Path path, RSuperellipse shape)
    {
        Rect bounds = shape.Rect;
        bool allCornersSame = shape.TopLeft == shape.TopRight
                              && shape.TopRight == shape.BottomRight
                              && shape.BottomRight == shape.BottomLeft
                              && shape.TopLeft != Radius.Zero;
        if (allCornersSame)
        {
            Quadrant quadrant = ComputeQuadrant(
                Vec.FromPoint(bounds.Center),
                new Vec(bounds.Right, bounds.Top),
                Vec.FromRadius(shape.TopRight),
                new Vec(-1.0, 1.0));
            Vec start = Transform(quadrant, quadrant.Top.Offset + new Vec(0.0, quadrant.Top.A));
            path.MoveTo(start.X, start.Y);
            AddQuadrant(path, quadrant, reverse: false, new Vec(1.0, 1.0));
            AddQuadrant(path, quadrant, reverse: true, new Vec(1.0, -1.0));
            AddQuadrant(path, quadrant, reverse: false, new Vec(-1.0, -1.0));
            AddQuadrant(path, quadrant, reverse: true, new Vec(-1.0, 1.0));
            path.LineTo(start.X, start.Y);
            path.Close();
            return;
        }

        double topSplit = Split(bounds.Left, bounds.Right, shape.TopLeft.X, shape.TopRight.X);
        double rightSplit = Split(bounds.Top, bounds.Bottom, shape.TopRight.Y, shape.BottomRight.Y);
        double bottomSplit = Split(bounds.Left, bounds.Right, shape.BottomLeft.X, shape.BottomRight.X);
        double leftSplit = Split(bounds.Top, bounds.Bottom, shape.TopLeft.Y, shape.BottomLeft.Y);
        Quadrant topRight = ComputeQuadrant(
            new Vec(topSplit, rightSplit),
            new Vec(bounds.Right, bounds.Top),
            Vec.FromRadius(shape.TopRight),
            new Vec(1.0, -1.0));
        Quadrant bottomRight = ComputeQuadrant(
            new Vec(bottomSplit, rightSplit),
            new Vec(bounds.Right, bounds.Bottom),
            Vec.FromRadius(shape.BottomRight),
            new Vec(1.0, 1.0));
        Quadrant bottomLeft = ComputeQuadrant(
            new Vec(bottomSplit, leftSplit),
            new Vec(bounds.Left, bounds.Bottom),
            Vec.FromRadius(shape.BottomLeft),
            new Vec(-1.0, 1.0));
        Quadrant topLeft = ComputeQuadrant(
            new Vec(topSplit, leftSplit),
            new Vec(bounds.Left, bounds.Top),
            Vec.FromRadius(shape.TopLeft),
            new Vec(-1.0, -1.0));

        Vec first = Transform(topRight, topRight.Top.Offset + new Vec(0.0, topRight.Top.A));
        path.MoveTo(first.X, first.Y);
        AddQuadrant(path, topRight, reverse: false, Vec.One);
        AddQuadrant(path, bottomRight, reverse: true, Vec.One);
        AddQuadrant(path, bottomLeft, reverse: false, Vec.One);
        AddQuadrant(path, topLeft, reverse: true, Vec.One);
        path.LineTo(first.X, first.Y);
        path.Close();
    }

    private static double Split(double left, double right, double leftRatio, double rightRatio)
    {
        return leftRatio == 0.0 && rightRatio == 0.0
            ? (left + right) / 2.0
            : ((left * rightRatio) + (right * leftRatio)) / (leftRatio + rightRatio);
    }

    private static Quadrant ComputeQuadrant(Vec center, Vec corner, Vec inputRadii, Vec fallbackSign)
    {
        Vec cornerVector = corner - center;
        var radii = new Vec(
            Math.Min(Math.Abs(inputRadii.X), Math.Abs(cornerVector.X)),
            Math.Min(Math.Abs(inputRadii.Y), Math.Abs(cornerVector.Y)));
        double normalizedRadius = Math.Min(radii.X, radii.Y);
        Vec forwardScale = normalizedRadius == 0.0 ? Vec.One : radii / normalizedRadius;
        Vec normalizedHalfSize = cornerVector.Abs() / forwardScale;
        Vec signedScale = (cornerVector / normalizedHalfSize).ReplaceNaN(fallbackSign);
        double offset = normalizedHalfSize.X - normalizedHalfSize.Y;
        return new Quadrant(
            center,
            signedScale,
            ComputeOctant(new Vec(0.0, -offset), normalizedHalfSize.X, normalizedRadius),
            ComputeOctant(new Vec(offset, 0.0), normalizedHalfSize.Y, normalizedRadius));
    }

    private static Octant ComputeOctant(Vec center, double semiAxis, double radius)
    {
        if (radius <= CloseEnough)
        {
            return new Octant(center, semiAxis, 0.0, 0.0, new Vec(semiAxis, semiAxis), default, 0.0);
        }

        double ratio = semiAxis * 2.0 / radius;
        double gap = GapFactor * radius;
        (double n, double xJRatio) = ComputeNAndXj(ratio);
        double xJ = xJRatio * semiAxis;
        double yJ = Math.Pow(1.0 - Math.Pow(xJRatio, n), 1.0 / n) * semiAxis;
        double tangent = Math.Pow(xJ / yJ, n - 1.0);
        double diagonal = (xJ - (tangent * yJ)) / (1.0 - tangent);
        double circleRadius = (semiAxis - diagonal - gap) * Math.Sqrt(2.0);
        Vec midpoint = new(semiAxis - gap, semiAxis - gap);
        Vec join = new(xJ, yJ);
        Vec circleCenter = FindCircleCenter(join, midpoint, circleRadius);
        double circleAngle = (join - circleCenter).AngleFrom(midpoint - circleCenter);
        return new Octant(center, semiAxis, n, 0.0, join, circleCenter, circleAngle);
    }

    private static (double N, double XjRatio) ComputeNAndXj(double ratio)
    {
        const double secondMaxRatio = 5.0;
        if (ratio > secondMaxRatio)
        {
            double n = (1.559599389 * (ratio - secondMaxRatio)) + NAndXjTable[^1].N;
            double kxj = (0.522807185 * (ratio - secondMaxRatio)) + NAndXjTable[^1].Kxj;
            return (n, 1.0 - (1.0 / kxj));
        }

        double clamped = Math.Clamp(ratio, 2.0, secondMaxRatio);
        double steps = clamped < 2.5
            ? (clamped - 2.0) * 10.0
            : ((clamped - 2.5) * 2.0) + 5.0;
        int left = Math.Clamp((int)Math.Floor(steps), 0, NAndXjTable.Length - 2);
        double fraction = steps - left;
        double nValue = Lerp(NAndXjTable[left].N, NAndXjTable[left + 1].N, fraction);
        double kxjValue = Lerp(NAndXjTable[left].Kxj, NAndXjTable[left + 1].Kxj, fraction);
        return (nValue, 1.0 - (1.0 / kxjValue));
    }

    private static Vec FindCircleCenter(Vec first, Vec second, double radius)
    {
        Vec delta = second - first;
        Vec midpoint = (first + second) / 2.0;
        Vec perpendicular = new(-delta.Y, delta.X);
        double halfDistance = delta.Length / 2.0;
        double centerDistance = Math.Sqrt(Math.Max(0.0, (radius * radius) - (halfDistance * halfDistance)));
        return midpoint - (perpendicular.Normalized() * centerDistance);
    }

    private static void AddQuadrant(Path path, Quadrant quadrant, bool reverse, Vec scaleSign)
    {
        if (quadrant.Top.N < 2.0 || quadrant.Right.N < 2.0)
        {
            Vec corner = quadrant.Top.Offset + new Vec(quadrant.Top.A, quadrant.Top.A);
            AddLine(path, Transform(quadrant, corner, scaleSign));
            Vec end = reverse
                ? quadrant.Top.Offset + new Vec(0.0, quadrant.Top.A)
                : quadrant.Right.Offset + new Vec(quadrant.Right.A, 0.0);
            AddLine(path, Transform(quadrant, end, scaleSign));
            return;
        }

        if (!reverse)
        {
            AddOctant(path, quadrant, quadrant.Top, reverse: false, flip: false, scaleSign);
            AddOctant(path, quadrant, quadrant.Right, reverse: true, flip: true, scaleSign);
        }
        else
        {
            AddOctant(path, quadrant, quadrant.Right, reverse: false, flip: true, scaleSign);
            AddOctant(path, quadrant, quadrant.Top, reverse: true, flip: false, scaleSign);
        }
    }

    private static void AddOctant(
        Path path,
        Quadrant quadrant,
        Octant octant,
        bool reverse,
        bool flip,
        Vec scaleSign)
    {
        (Conic first, Conic second) = SuperellipseArcPoints(octant);
        (Vec start, Vec firstControl, Vec secondControl, Vec end) = CircularArcPoints(octant);
        Vec Convert(Vec point) => Transform(quadrant, octant.Offset + (flip ? point.Flipped() : point), scaleSign);

        if (!reverse)
        {
            AddConic(path, Convert(first.Control), Convert(first.Second), first.Weight);
            AddConic(path, Convert(second.Control), Convert(second.Second), second.Weight);
            AddCubic(path, Convert(firstControl), Convert(secondControl), Convert(end));
            return;
        }

        AddCubic(path, Convert(secondControl), Convert(firstControl), Convert(start));
        AddConic(path, Convert(second.Control), Convert(second.First), second.Weight);
        AddConic(path, Convert(first.Control), Convert(first.First), first.Weight);
    }

    private static (Conic First, Conic Second) SuperellipseArcPoints(Octant octant)
    {
        Vec start = new(0.0, octant.A);
        Vec join = octant.CircleStart;
        (double firstWeight, double secondWeight, double yRatio) = BezierFactors(
            octant.N,
            join.X / octant.A,
            join.Y / octant.A);
        var split = new Vec(
            Math.Pow(1.0 - Math.Pow(yRatio, octant.N), 1.0 / octant.N) * octant.A,
            yRatio * octant.A);
        double joinSlope = -Math.Pow(join.X / join.Y, octant.N - 1.0);
        double splitSlope = -Math.Pow(split.X / split.Y, octant.N - 1.0);
        return (
            new Conic(start, Intersection(start, 0.0, split, splitSlope), split, firstWeight),
            new Conic(split, Intersection(split, splitSlope, join, joinSlope), join, secondWeight));
    }

    private static (Vec Start, Vec FirstControl, Vec SecondControl, Vec End) CircularArcPoints(Octant octant)
    {
        Vec startVector = octant.CircleStart - octant.CircleCenter;
        Vec endVector = startVector.Rotate(-octant.CircleAngle);
        Vec end = octant.CircleCenter + endVector;
        Vec startTangent = new Vec(startVector.Y, -startVector.X).Normalized();
        Vec endTangent = new Vec(-endVector.Y, endVector.X).Normalized();
        double bezierFactor = Math.Tan(octant.CircleAngle / 4.0) * 4.0 / 3.0;
        double radius = startVector.Length;
        return (
            octant.CircleStart,
            octant.CircleStart + (startTangent * bezierFactor * radius),
            end + (endTangent * bezierFactor * radius),
            end);
    }

    private static (double FirstWeight, double SecondWeight, double YRatio) BezierFactors(
        double n,
        double xRatio,
        double yRatio)
    {
        n = Math.Min(n, 14.0);
        double steps = Math.Clamp(n - 2.0, 0.0, BezierFactorTable.Length - 1.0);
        int left = Math.Clamp((int)Math.Floor(steps), 0, BezierFactorTable.Length - 2);
        double fraction = steps - left;
        double firstWeight = ((1.0 - fraction) * BezierFactorTable[left].First)
                             + (fraction * BezierFactorTable[left + 1].First * Math.Sqrt(n));
        double secondWeight = ((1.0 - fraction) * BezierFactorTable[left].Second)
                              + (fraction * BezierFactorTable[left + 1].Second * xRatio);
        double squareRoot = Math.Sqrt(n);
        double splitYRatio = (squareRoot + yRatio) / (squareRoot + 1.0);
        return (firstWeight, secondWeight, splitYRatio);
    }

    private static Vec Intersection(Vec first, double firstSlope, Vec second, double secondSlope)
    {
        if (Math.Abs(firstSlope - secondSlope) < CloseEnough)
        {
            return (first + second) / 2.0;
        }

        double x = ((firstSlope * first.X) - (secondSlope * second.X) + second.Y - first.Y)
                   / (firstSlope - secondSlope);
        return new Vec(x, (firstSlope * (x - first.X)) + first.Y);
    }

    private static Vec Transform(Quadrant quadrant, Vec point, Vec? scaleSign = null)
    {
        Vec scale = quadrant.SignedScale * (scaleSign ?? Vec.One);
        return quadrant.Offset + (point * scale);
    }

    private static void AddLine(Path path, Vec point) => path.LineTo(point.X, point.Y);

    private static void AddConic(Path path, Vec control, Vec end, double weight) =>
        path.ConicTo(control.X, control.Y, end.X, end.Y, weight);

    private static void AddCubic(Path path, Vec first, Vec second, Vec end) =>
        path.CubicTo(first.X, first.Y, second.X, second.Y, end.X, end.Y);

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private readonly record struct Octant(
        Vec Offset,
        double A,
        double N,
        double MaxTheta,
        Vec CircleStart,
        Vec CircleCenter,
        double CircleAngle);

    private readonly record struct Quadrant(Vec Offset, Vec SignedScale, Octant Top, Octant Right);

    private readonly record struct Conic(Vec First, Vec Control, Vec Second, double Weight);

    private readonly record struct Vec(double X, double Y)
    {
        public static Vec One { get; } = new(1.0, 1.0);

        public double Length => Math.Sqrt((X * X) + (Y * Y));

        public static Vec FromPoint(Point point) => new(point.X, point.Y);

        public static Vec FromRadius(Radius radius) => new(radius.X, radius.Y);

        public Vec Abs() => new(Math.Abs(X), Math.Abs(Y));

        public Vec Flipped() => new(Y, X);

        public Vec ReplaceNaN(Vec fallback) => new(
            double.IsNaN(X) ? fallback.X : X,
            double.IsNaN(Y) ? fallback.Y : Y);

        public Vec Normalized() => Length <= CloseEnough ? default : this / Length;

        public Vec Rotate(double angle) => new(
            (X * Math.Cos(angle)) - (Y * Math.Sin(angle)),
            (X * Math.Sin(angle)) + (Y * Math.Cos(angle)));

        public double AngleFrom(Vec other) => Math.Atan2((other.X * Y) - (other.Y * X), other.Dot(this));

        public double Dot(Vec other) => (X * other.X) + (Y * other.Y);

        public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);

        public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);

        public static Vec operator *(Vec a, Vec b) => new(a.X * b.X, a.Y * b.Y);

        public static Vec operator *(Vec value, double scale) => new(value.X * scale, value.Y * scale);

        public static Vec operator /(Vec a, Vec b) => new(a.X / b.X, a.Y / b.Y);

        public static Vec operator /(Vec value, double scale) => new(value.X / scale, value.Y / scale);
    }
}
