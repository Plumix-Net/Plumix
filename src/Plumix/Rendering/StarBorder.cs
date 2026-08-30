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
        public Point Valley;
        public Point Point;
        public Point ValleyArc1;
        public Point PointArc1;
        public Point PointArc2;
        public Point ValleyArc2;
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
            Point center = rect.Center;

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

            var scale = new Point(rect.Width / maxDiameter, rect.Height / maxDiameter);
            scale = BoxBorder.ShortestSide(rect) == rect.Width
                ? new Point(scale.X, (squash * scale.Y) + ((1 - squash) * scale.X))
                : new Point((squash * scale.X) + ((1 - squash) * scale.Y), scale.Y);

            Matrix squashMatrix = Matrix.CreateTranslation(-center.X, -center.Y)
                                  * Matrix.CreateRotation(rotation)
                                  * Matrix.CreateScale(scale.X, scale.Y)
                                  * Matrix.CreateTranslation(center.X, center.Y);
            return path.Transform(squashMatrix);
        }

        private double GeneratePoints(List<PointInfo> pointList, Point center, double radius, double innerRadius)
        {
            double step = Math.PI / points;

            // Start at the top of the star, one step before the first point.
            double angle = (-Math.PI / 2.0) - step;
            var valley = new Point(
                center.X + (Math.Cos(angle) * innerRadius),
                center.Y + (Math.Sin(angle) * innerRadius));

            double AddPoint(double pointAngle, double pointStep, double pointRadius, double pointInnerRadius)
            {
                pointAngle += pointStep;
                var point = new Point(
                    center.X + (Math.Cos(pointAngle) * pointRadius),
                    center.Y + (Math.Sin(pointAngle) * pointRadius));
                pointAngle += pointStep;
                var nextValley = new Point(
                    center.X + (Math.Cos(pointAngle) * pointInnerRadius),
                    center.Y + (Math.Sin(pointAngle) * pointInnerRadius));
                Point valleyArc1 = Along(valley, point, valleyRounding);
                Point pointArc1 = Along(point, valley, pointRounding);
                Point pointArc2 = Along(point, nextValley, pointRounding);
                Point valleyArc2 = Along(nextValley, point, valleyRounding);
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
            Point pointMidpoint = GetCurveMidpoint(
                thisPoint.Valley,
                thisPoint.Point,
                nextPoint.Valley,
                thisPoint.PointArc1,
                thisPoint.PointArc2);
            Point valleyMidpoint = GetCurveMidpoint(
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
            Point startingPoint = pointList[0].PointArc1;
            path.MoveTo(startingPoint.X, startingPoint.Y);
            double pointAngle = GetAngle(pointList[0].Valley, pointList[0].Point, pointList[1].Valley);
            double pointWeight = GetWeight(pointAngle);
            double valleyAngle = GetAngle(pointList[1].Point, pointList[1].Valley, pointList[0].Point);
            double valleyWeight = GetWeight(valleyAngle);

            for (int i = 0; i < pointList.Count; i++)
            {
                PointInfo point = pointList[i];
                PointInfo nextPoint = pointList[(i + 1) % pointList.Count];
                path.LineTo(point.PointArc1.X, point.PointArc1.Y);
                if (pointAngle != 180 && pointAngle != 0)
                {
                    path.ConicTo(point.Point.X, point.Point.Y, point.PointArc2.X, point.PointArc2.Y, pointWeight);
                }
                else
                {
                    path.LineTo(point.PointArc2.X, point.PointArc2.Y);
                }

                path.LineTo(point.ValleyArc2.X, point.ValleyArc2.Y);
                if (valleyAngle != 180 && valleyAngle != 0)
                {
                    path.ConicTo(
                        nextPoint.Valley.X,
                        nextPoint.Valley.Y,
                        nextPoint.ValleyArc1.X,
                        nextPoint.ValleyArc1.Y,
                        valleyWeight);
                }
                else
                {
                    path.LineTo(nextPoint.ValleyArc1.X, nextPoint.ValleyArc1.Y);
                }
            }

            path.Close();
        }

        private static Point GetCurveMidpoint(Point a, Point b, Point c, Point a1, Point c1)
        {
            double angle = GetAngle(a, b, c);
            double w = GetWeight(angle) / 2.0;
            return new Point(
                ((a1.X / 4.0) + (b.X * w) + (c1.X / 4.0)) / (0.5 + w),
                ((a1.Y / 4.0) + (b.Y * w) + (c1.Y / 4.0)) / (0.5 + w));
        }

        private static double GetWeight(double angle)
        {
            double half = angle / 2.0;
            double divisor = Math.PI / 2.0;
            double remainder = half - (Math.Floor(half / divisor) * divisor);
            return Math.Cos(remainder);
        }

        /// Returns the included angle between the vectors BA and BC, in radians.
        private static double GetAngle(Point a, Point b, Point c)
        {
            if (a == c || b == c || b == a)
            {
                return 0;
            }

            var u = new Point(a.X - b.X, a.Y - b.Y);
            var v = new Point(c.X - b.X, c.Y - b.Y);
            double dot = (u.X * v.X) + (u.Y * v.Y);
            double m1 = b.X == a.X ? double.PositiveInfinity : -u.Y / -u.X;
            double m2 = b.X == c.X ? double.PositiveInfinity : -v.Y / -v.X;
            double angle = Math.Abs(Math.Atan2(m1 - m2, 1 + (m1 * m2)));
            if (dot < 0)
            {
                angle += Math.PI;
            }

            return angle;
        }

        private static Point Along(Point from, Point to, double fraction)
        {
            return new Point(
                from.X + ((to.X - from.X) * fraction),
                from.Y + ((to.Y - from.Y) * fraction));
        }

        private static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }
}
