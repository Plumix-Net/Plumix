using Avalonia;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/star_border.dart

/// A border that fits a star or polygon-shaped border within the rectangle of the widget it is applied to.
public sealed record StarBorder : OutlinedBorder
{
    private const double RadiansToDegrees = 180.0 / Math.PI;
    private const double DegreesToRadians = Math.PI / 180.0;

    public StarBorder(
        BorderSide? side = null,
        double points = 5,
        double innerRadiusRatio = 0.4,
        double pointRounding = 0,
        double valleyRounding = 0,
        double rotation = 0,
        double squash = 0)
        : base(side)
    {
        ValidateRange(squash, nameof(squash));
        ValidateRange(pointRounding, nameof(pointRounding));
        ValidateRange(valleyRounding, nameof(valleyRounding));
        if (valleyRounding + pointRounding > 1.0)
        {
            throw new ArgumentException(
                $"The sum of valleyRounding ({valleyRounding}) and pointRounding ({pointRounding}) must not "
                + "exceed one.");
        }

        ValidateRange(innerRadiusRatio, nameof(innerRadiusRatio));
        if (points < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "A StarBorder must have at least two points.");
        }

        Points = points;
        StoredInnerRadiusRatio = innerRadiusRatio;
        PointRounding = pointRounding;
        ValleyRounding = valleyRounding;
        RotationRadians = rotation * DegreesToRadians;
        Squash = squash;
    }

    private StarBorder(
        BorderSide? side,
        double points,
        double? storedInnerRadiusRatio,
        double pointRounding,
        double valleyRounding,
        double rotationRadians,
        double squash)
        : base(side)
    {
        Points = points;
        StoredInnerRadiusRatio = storedInnerRadiusRatio;
        PointRounding = pointRounding;
        ValleyRounding = valleyRounding;
        RotationRadians = rotationRadians;
        Squash = squash;
    }

    /// Creates a regular polygon with the given number of sides.
    public static StarBorder Polygon(
        BorderSide? side = null,
        double sides = 5,
        double pointRounding = 0,
        double rotation = 0,
        double squash = 0)
    {
        ValidateRange(squash, nameof(squash));
        ValidateRange(pointRounding, nameof(pointRounding));
        if (sides < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(sides), "A polygon must have at least two sides.");
        }

        return new StarBorder(
            side,
            sides,
            storedInnerRadiusRatio: null,
            pointRounding,
            valleyRounding: 0,
            rotation * DegreesToRadians,
            squash);
    }

    /// The number of points in the star, or sides in the polygon.
    public double Points { get; init; }

    internal double? StoredInnerRadiusRatio { get; init; }

    /// The ratio of the inner radius of a star with the outer radius.
    public double InnerRadiusRatio => StoredInnerRadiusRatio ?? Math.Cos(Math.PI / Points);

    /// The amount of rounding on the points of a star, or the corners of a polygon.
    public double PointRounding { get; init; }

    /// The amount of rounding of the interior corners of a star.
    public double ValleyRounding { get; init; }

    internal double RotationRadians { get; init; }

    /// The rotation in clockwise degrees around the center of the shape.
    public double Rotation => RotationRadians * RadiansToDegrees;

    /// How much the shape is "squashed" towards the shorter of its two axes.
    public double Squash { get; init; }

    public override ShapeBorder Scale(double t)
    {
        return new StarBorder(
            Side.Scale(t),
            Points,
            InnerRadiusRatio,
            PointRounding,
            ValleyRounding,
            Rotation,
            Squash);
    }

    public override OutlinedBorder CopyWith(BorderSide? side = null)
    {
        return CopyWith(side, null, null, null, null, null, null);
    }

    public StarBorder CopyWith(
        BorderSide? side,
        double? points,
        double? innerRadiusRatio,
        double? pointRounding,
        double? valleyRounding,
        double? rotation,
        double? squash)
    {
        return new StarBorder(
            side ?? Side,
            points ?? Points,
            innerRadiusRatio ?? InnerRadiusRatio,
            pointRounding ?? PointRounding,
            valleyRounding ?? ValleyRounding,
            rotation ?? Rotation,
            squash ?? Squash);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        if (t == 0.0)
        {
            return a;
        }

        if (t == 1.0)
        {
            return this;
        }

        switch (a)
        {
            case StarBorder star:
                return new StarBorder(
                    BorderSide.Lerp(star.Side, Side, t),
                    LerpDouble(star.Points, Points, t),
                    LerpDouble(star.InnerRadiusRatio, InnerRadiusRatio, t),
                    LerpDouble(star.PointRounding, PointRounding, t),
                    LerpDouble(star.ValleyRounding, ValleyRounding, t),
                    LerpDouble(star.RotationRadians, RotationRadians, t) * RadiansToDegrees,
                    LerpDouble(star.Squash, Squash, t));
            case CircleBorder circle when Points >= 2.5:
            {
                double lerpedPoints = LerpDouble(Math.Round(Points, MidpointRounding.AwayFromZero), Points, t);
                return new StarBorder(
                    BorderSide.Lerp(circle.Side, Side, t),
                    lerpedPoints,
                    LerpDouble(Math.Cos(Math.PI / lerpedPoints), InnerRadiusRatio, t),
                    LerpDouble(1.0, PointRounding, t),
                    LerpDouble(0.0, ValleyRounding, t),
                    Rotation,
                    LerpDouble(circle.Eccentricity, Squash, t));
            }

            case CircleBorder circle:
            {
                double lerpedPoints = LerpDouble(Points, 2, t);
                return new StarBorder(
                    BorderSide.Lerp(circle.Side, Side, t),
                    lerpedPoints,
                    LerpDouble(1, InnerRadiusRatio, t),
                    LerpDouble(0.5, PointRounding, t),
                    LerpDouble(0.5, ValleyRounding, t),
                    Rotation,
                    LerpDouble(circle.Eccentricity, Squash, t));
            }

            case StadiumBorder stadium:
            {
                BorderSide lerpedSide = BorderSide.Lerp(stadium.Side, Side, t);
                return TwoPhaseLerp(
                    t,
                    0.5,
                    phase => stadium.LerpTo(new CircleBorder(lerpedSide), phase),
                    phase => LerpFrom(new CircleBorder(lerpedSide), phase));
            }

            case RoundedRectangleBorder rounded:
            {
                BorderSide lerpedSide = BorderSide.Lerp(rounded.Side, Side, t);
                return TwoPhaseLerp(
                    t,
                    1.0 / 3.0,
                    phase => new StadiumBorder(lerpedSide).LerpFrom(rounded, phase),
                    phase => TwoPhaseLerp(
                        phase,
                        0.5,
                        inner => new StadiumBorder(lerpedSide).LerpTo(new CircleBorder(lerpedSide), inner),
                        inner => LerpFrom(new CircleBorder(lerpedSide), inner)));
            }

            default:
                return base.LerpFrom(a, t);
        }
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        if (t == 0.0)
        {
            return this;
        }

        if (t == 1.0)
        {
            return b;
        }

        switch (b)
        {
            case StarBorder star:
                return new StarBorder(
                    BorderSide.Lerp(Side, star.Side, t),
                    LerpDouble(Points, star.Points, t),
                    LerpDouble(InnerRadiusRatio, star.InnerRadiusRatio, t),
                    LerpDouble(PointRounding, star.PointRounding, t),
                    LerpDouble(ValleyRounding, star.ValleyRounding, t),
                    LerpDouble(RotationRadians, star.RotationRadians, t) * RadiansToDegrees,
                    LerpDouble(Squash, star.Squash, t));
            case CircleBorder circle when Points >= 2.5:
            {
                double lerpedPoints = LerpDouble(Points, Math.Round(Points, MidpointRounding.AwayFromZero), t);
                return new StarBorder(
                    BorderSide.Lerp(Side, circle.Side, t),
                    lerpedPoints,
                    LerpDouble(InnerRadiusRatio, Math.Cos(Math.PI / lerpedPoints), t),
                    LerpDouble(PointRounding, 1.0, t),
                    LerpDouble(ValleyRounding, 0.0, t),
                    Rotation,
                    LerpDouble(Squash, circle.Eccentricity, t));
            }

            case CircleBorder circle:
            {
                double lerpedPoints = LerpDouble(Points, 2, t);
                return new StarBorder(
                    BorderSide.Lerp(Side, circle.Side, t),
                    lerpedPoints,
                    LerpDouble(InnerRadiusRatio, 1, t),
                    LerpDouble(PointRounding, 0.5, t),
                    LerpDouble(ValleyRounding, 0.5, t),
                    Rotation,
                    LerpDouble(Squash, circle.Eccentricity, t));
            }

            case StadiumBorder stadium:
            {
                BorderSide lerpedSide = BorderSide.Lerp(Side, stadium.Side, t);
                return TwoPhaseLerp(
                    t,
                    0.5,
                    phase => LerpTo(new CircleBorder(lerpedSide), phase),
                    phase => stadium.LerpFrom(new CircleBorder(lerpedSide), phase));
            }

            case RoundedRectangleBorder rounded:
            {
                BorderSide lerpedSide = BorderSide.Lerp(Side, rounded.Side, t);
                return TwoPhaseLerp(
                    t,
                    2.0 / 3.0,
                    phase => TwoPhaseLerp(
                        phase,
                        0.5,
                        inner => LerpTo(new CircleBorder(lerpedSide), inner),
                        inner => new StadiumBorder(lerpedSide).LerpFrom(new CircleBorder(lerpedSide), inner)),
                    phase => new StadiumBorder(lerpedSide).LerpTo(rounded, phase));
            }

            default:
                return base.LerpTo(b, t);
        }
    }

    public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
    {
        return CreateGenerator().Generate(rect.Deflate(Side.StrokeInset));
    }

    public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
    {
        return CreateGenerator().Generate(rect);
    }

    public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
    {
        if (Side.Style == BorderStyle.None)
        {
            return;
        }

        Rect adjustedRect = rect.Inflate(Side.StrokeOffset / 2.0);
        Path path = CreateGenerator().Generate(adjustedRect);
        context.Canvas.DrawPath(path, null, Side.ToPen());
    }

    public override string ToString()
    {
        return $"StarBorder({Side}, points: {Points}, innerRadiusRatio: {InnerRadiusRatio})";
    }

    public bool Equals(StarBorder? other)
    {
        return other is not null
               && other.Side == Side
               && other.Points == Points
               && other.StoredInnerRadiusRatio == StoredInnerRadiusRatio
               && other.PointRounding == PointRounding
               && other.ValleyRounding == ValleyRounding
               && other.RotationRadians == RotationRadians
               && other.Squash == Squash;
    }

    public override int GetHashCode()
    {
        return Side.GetHashCode();
    }

    private StarGenerator CreateGenerator()
    {
        return new StarGenerator(
            Points,
            InnerRadiusRatio,
            PointRounding,
            ValleyRounding,
            RotationRadians,
            Squash);
    }

    private static ShapeBorder? TwoPhaseLerp(
        double t,
        double split,
        Func<double, ShapeBorder?> first,
        Func<double, ShapeBorder?> second)
    {
        if (t < split)
        {
            return first(t * (1.0 / split));
        }

        return second((1.0 / (1.0 - split)) * (t - split));
    }

    private static void ValidateRange(double value, string name)
    {
        if (value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(name, $"The {name} argument must be between 0.0 and 1.0.");
        }
    }

    private static double LerpDouble(double a, double b, double t)
    {
        return (a * (1.0 - t)) + (b * t);
    }

    private sealed class PointInfo
    {
        public Offset Valley;
        public Offset Point;
        public Offset ValleyArc1;
        public Offset PointArc1;
        public Offset PointArc2;
        public Offset ValleyArc2;
    }

    private sealed class StarGenerator(
        double points,
        double innerRadiusRatio,
        double pointRounding,
        double valleyRounding,
        double rotation,
        double squash)
    {
        public Path Generate(Rect rect)
        {
            double radius = BoxBorder.ShortestSide(rect) / 2.0;
            Offset center = rect.Center;

            // Add a tiny fudge factor so that the shape is never degenerate.
            const double minInnerRadiusRatio = .002;
            double mappedInnerRadiusRatio = (innerRadiusRatio * (1.0 - minInnerRadiusRatio)) + minInnerRadiusRatio;

            var pointList = new List<PointInfo>();
            double maxDiameter = 2.0 * GeneratePoints(
                pointList,
                center,
                radius,
                radius * mappedInnerRadiusRatio);

            var path = new Path();
            DrawPoints(path, pointList);

            var scale = new Offset(rect.Width / maxDiameter, rect.Height / maxDiameter);
            scale = BoxBorder.ShortestSide(rect) == rect.Width
                ? new Offset(scale.Dx, (squash * scale.Dy) + ((1 - squash) * scale.Dx))
                : new Offset((squash * scale.Dx) + ((1 - squash) * scale.Dy), scale.Dy);

            Matrix squashMatrix = Matrix.CreateTranslation(-center.Dx, -center.Dy)
                                  * Matrix.CreateRotation(rotation)
                                  * Matrix.CreateScale(scale.Dx, scale.Dy)
                                  * Matrix.CreateTranslation(center.Dx, center.Dy);
            return path.Transform(squashMatrix);
        }

        private double GeneratePoints(List<PointInfo> pointList, Offset center, double radius, double innerRadius)
        {
            double step = Math.PI / points;

            // Start at the top of the star, one step before the first point.
            double angle = (-Math.PI / 2.0) - step;
            var valley = new Offset(
                center.Dx + (Math.Cos(angle) * innerRadius),
                center.Dy + (Math.Sin(angle) * innerRadius));

            double AddPoint(double pointAngle, double pointStep, double pointRadius, double pointInnerRadius)
            {
                pointAngle += pointStep;
                var point = new Offset(
                    center.Dx + (Math.Cos(pointAngle) * pointRadius),
                    center.Dy + (Math.Sin(pointAngle) * pointRadius));
                pointAngle += pointStep;
                var nextValley = new Offset(
                    center.Dx + (Math.Cos(pointAngle) * pointInnerRadius),
                    center.Dy + (Math.Sin(pointAngle) * pointInnerRadius));
                Offset valleyArc1 = Along(valley, point, valleyRounding);
                Offset pointArc1 = Along(point, valley, pointRounding);
                Offset pointArc2 = Along(point, nextValley, pointRounding);
                Offset valleyArc2 = Along(nextValley, point, valleyRounding);
                pointList.Add(new PointInfo
                {
                    Valley = valley,
                    Point = point,
                    ValleyArc1 = valleyArc1,
                    PointArc1 = pointArc1,
                    PointArc2 = pointArc2,
                    ValleyArc2 = valleyArc2,
                });
                valley = nextValley;
                return pointAngle;
            }

            double remainder = points - Math.Truncate(points);
            bool hasIntegerSides = remainder < 1e-6;
            double wholeSides = points - (hasIntegerSides ? 0 : 1);
            for (int i = 0; i < wholeSides; i++)
            {
                angle = AddPoint(angle, step, radius, innerRadius);
            }

            PointInfo thisPoint = pointList[0];
            PointInfo nextPoint = pointList[1];
            Offset pointMidpoint = GetCurveMidpoint(
                thisPoint.Valley,
                thisPoint.Point,
                nextPoint.Valley,
                thisPoint.PointArc1,
                thisPoint.PointArc2);
            Offset valleyMidpoint = GetCurveMidpoint(
                thisPoint.Point,
                nextPoint.Valley,
                nextPoint.Point,
                thisPoint.ValleyArc2,
                nextPoint.ValleyArc1);
            double valleyRadius = Distance(valleyMidpoint, center);
            double pointRadius = Distance(pointMidpoint, center);

            // Add an extra point (which is shorter) for a fractional number of points.
            if (!hasIntegerSides)
            {
                double effectiveInnerRadius = Math.Max(valleyRadius, innerRadius);
                double endingRadius = effectiveInnerRadius + (remainder * (radius - effectiveInnerRadius));
                AddPoint(angle, step * remainder, endingRadius, innerRadius);
            }

            return Math.Clamp(Math.Max(valleyRadius, pointRadius), double.Epsilon, double.MaxValue);
        }

        private void DrawPoints(Path path, List<PointInfo> pointList)
        {
            Offset startingPoint = pointList[0].PointArc1;
            path.MoveTo(startingPoint.Dx, startingPoint.Dy);
            double pointAngle = GetAngle(pointList[0].Valley, pointList[0].Point, pointList[1].Valley);
            double pointWeight = GetWeight(pointAngle);
            double valleyAngle = GetAngle(pointList[1].Point, pointList[1].Valley, pointList[0].Point);
            double valleyWeight = GetWeight(valleyAngle);

            for (int i = 0; i < pointList.Count; i++)
            {
                PointInfo point = pointList[i];
                PointInfo nextPoint = pointList[(i + 1) % pointList.Count];
                path.LineTo(point.PointArc1.Dx, point.PointArc1.Dy);
                if (pointAngle != 180 && pointAngle != 0)
                {
                    path.ConicTo(point.Point.Dx, point.Point.Dy, point.PointArc2.Dx, point.PointArc2.Dy, pointWeight);
                }
                else
                {
                    path.LineTo(point.PointArc2.Dx, point.PointArc2.Dy);
                }

                path.LineTo(point.ValleyArc2.Dx, point.ValleyArc2.Dy);
                if (valleyAngle != 180 && valleyAngle != 0)
                {
                    path.ConicTo(
                        nextPoint.Valley.Dx,
                        nextPoint.Valley.Dy,
                        nextPoint.ValleyArc1.Dx,
                        nextPoint.ValleyArc1.Dy,
                        valleyWeight);
                }
                else
                {
                    path.LineTo(nextPoint.ValleyArc1.Dx, nextPoint.ValleyArc1.Dy);
                }
            }

            path.Close();
        }

        private static Offset GetCurveMidpoint(Offset a, Offset b, Offset c, Offset a1, Offset c1)
        {
            double angle = GetAngle(a, b, c);
            double w = GetWeight(angle) / 2.0;
            return new Offset(
                ((a1.Dx / 4.0) + (b.Dx * w) + (c1.Dx / 4.0)) / (0.5 + w),
                ((a1.Dy / 4.0) + (b.Dy * w) + (c1.Dy / 4.0)) / (0.5 + w));
        }

        private static double GetWeight(double angle)
        {
            double half = angle / 2.0;
            double divisor = Math.PI / 2.0;
            double remainder = half - (Math.Floor(half / divisor) * divisor);
            return Math.Cos(remainder);
        }

        /// Returns the included angle between the vectors BA and BC, in radians.
        private static double GetAngle(Offset a, Offset b, Offset c)
        {
            if (a == c || b == c || b == a)
            {
                return 0;
            }

            var u = new Offset(a.Dx - b.Dx, a.Dy - b.Dy);
            var v = new Offset(c.Dx - b.Dx, c.Dy - b.Dy);
            double dot = (u.Dx * v.Dx) + (u.Dy * v.Dy);
            double m1 = b.Dx == a.Dx ? double.PositiveInfinity : -u.Dy / -u.Dx;
            double m2 = b.Dx == c.Dx ? double.PositiveInfinity : -v.Dy / -v.Dx;
            double angle = Math.Abs(Math.Atan2(m1 - m2, 1 + (m1 * m2)));
            if (dot < 0)
            {
                angle += Math.PI;
            }

            return angle;
        }

        private static Offset Along(Offset from, Offset to, double fraction)
        {
            return new Offset(
                from.Dx + ((to.Dx - from.Dx) * fraction),
                from.Dy + ((to.Dy - from.Dy) * fraction));
        }

        private static double Distance(Offset a, Offset b)
        {
            double dx = a.Dx - b.Dx;
            double dy = a.Dy - b.Dy;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }
}
