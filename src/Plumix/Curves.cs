using Avalonia;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/animation/curves.dart

namespace Plumix;

/// <summary>
/// An abstract class providing an interface for evaluating a parametric curve. A parametric curve
/// transforms a parameter (hence the name) <c>t</c> along a curve to the value of the curve at that value
/// of <c>t</c>.
/// </summary>
public abstract class ParametricCurve<T>
{
    protected ParametricCurve()
    {
    }

    /// <summary>Returns the value of the curve at point <paramref name="t"/>.</summary>
    public virtual T Transform(double t)
    {
        DebugAssertions.Assert(
            t >= 0.0 && t <= 1.0,
            $"parametric value {CurveFormat.D(t)} is outside of [0, 1] range.");
        return TransformInternal(t);
    }

    /// <summary>Returns the value of the curve at point <paramref name="t"/>.</summary>
    /// <remarks>Dart's <c>@protected transformInternal</c>; public because Flutter's tests call it directly.</remarks>
    public virtual T TransformInternal(double t) => throw new NotImplementedException();

    public override string ToString() => Diagnostics.ObjectRuntimeType(this, "ParametricCurve");
}

/// <summary>
/// An parametric animation easing curve, i.e. a mapping of the unit interval to the unit interval.
/// </summary>
public abstract class Curve : ParametricCurve<double>
{
    protected Curve()
    {
    }

    /// <summary>
    /// Returns the value of the curve at point <paramref name="t"/>. It must satisfy <c>transform(0) == 0</c>
    /// and <c>transform(1) == 1</c>, which this method enforces by returning <paramref name="t"/> at 0.0 and 1.0.
    /// </summary>
    public override double Transform(double t)
    {
        if (t == 0.0 || t == 1.0)
        {
            return t;
        }

        return base.Transform(t);
    }

    /// <summary>Returns a new curve that is the reversed inversion of this one.</summary>
    public Curve Flipped => new FlippedCurve(this);
}

/// <summary>Dart's private <c>_Linear</c>: the identity map over the unit interval.</summary>
internal sealed class LinearCurve : Curve
{
    public override double TransformInternal(double t) => t;
}

/// <summary>A sawtooth curve that repeats a given number of times over the unit interval.</summary>
public class SawTooth : Curve
{
    public SawTooth(int count)
    {
        Count = count;
    }

    /// <summary>The number of repetitions of the sawtooth pattern in the unit interval.</summary>
    public int Count { get; }

    public override double TransformInternal(double t)
    {
        t *= Count;
        return t - Math.Truncate(t);
    }

    public override string ToString() => $"{Diagnostics.ObjectRuntimeType(this, "SawTooth")}({Count})";
}

/// <summary>
/// A curve that is 0.0 until <see cref="Begin"/>, then curved (according to <see cref="Curve"/>) from 0.0
/// at <see cref="Begin"/> to 1.0 at <see cref="End"/>, then remains 1.0 past <see cref="End"/>.
/// </summary>
public class Interval : Curve
{
    public Interval(double begin, double end, Curve? curve = null)
    {
        Begin = begin;
        End = end;
        Curve = curve ?? Curves.Linear;
    }

    /// <summary>The largest value for which this interval is 0.0.</summary>
    public double Begin { get; }

    /// <summary>The smallest value for which this interval is 1.0.</summary>
    public double End { get; }

    /// <summary>The curve to apply between <see cref="Begin"/> and <see cref="End"/>.</summary>
    public Curve Curve { get; }

    public override double TransformInternal(double t)
    {
        DebugAssertions.Assert(Begin >= 0.0);
        DebugAssertions.Assert(Begin <= 1.0);
        DebugAssertions.Assert(End >= 0.0);
        DebugAssertions.Assert(End <= 1.0);
        DebugAssertions.Assert(End >= Begin);
        t = CurveFormat.ClampDouble((t - Begin) / (End - Begin), 0.0, 1.0);
        if (t == 0.0 || t == 1.0)
        {
            return t;
        }

        return Curve.Transform(t);
    }

    public override string ToString()
    {
        string type = Diagnostics.ObjectRuntimeType(this, "Interval");
        if (Curve is not LinearCurve)
        {
            return $"{type}({CurveFormat.D(Begin)}⋯{CurveFormat.D(End)})➩{Curve}";
        }

        return $"{type}({CurveFormat.D(Begin)}⋯{CurveFormat.D(End)})";
    }
}

/// <summary>
/// A curve that progresses according to <see cref="BeginCurve"/> until <see cref="SplitPoint"/>, then
/// according to <see cref="EndCurve"/>.
/// </summary>
/// <remarks>Dart's field is <c>split</c>; C# cannot name a member after its enclosing type.</remarks>
public class Split : Curve
{
    public Split(double split, Curve? beginCurve = null, Curve? endCurve = null)
    {
        SplitPoint = split;
        BeginCurve = beginCurve ?? Curves.Linear;
        EndCurve = endCurve ?? Curves.EaseOutCubic;
    }

    /// <summary>The progress value separating <see cref="BeginCurve"/> from <see cref="EndCurve"/>.</summary>
    public double SplitPoint { get; }

    /// <summary>The curve to use before <see cref="SplitPoint"/> is reached.</summary>
    public Curve BeginCurve { get; }

    /// <summary>The curve to use after <see cref="SplitPoint"/> is reached.</summary>
    public Curve EndCurve { get; }

    public override double Transform(double t)
    {
        DebugAssertions.Assert(t >= 0.0 && t <= 1.0);
        DebugAssertions.Assert(SplitPoint >= 0.0 && SplitPoint <= 1.0);

        if (t == 0.0 || t == 1.0)
        {
            return t;
        }

        if (t == SplitPoint)
        {
            return SplitPoint;
        }

        if (t < SplitPoint)
        {
            double curveProgress = t / SplitPoint;
            double transformed = BeginCurve.Transform(curveProgress);
            return CurveFormat.LerpDouble(0, SplitPoint, transformed);
        }
        else
        {
            double curveProgress = (t - SplitPoint) / (1 - SplitPoint);
            double transformed = EndCurve.Transform(curveProgress);
            return CurveFormat.LerpDouble(SplitPoint, 1, transformed);
        }
    }

    public override string ToString()
    {
        return $"{Diagnostics.DescribeIdentity(this)}({CurveFormat.D(SplitPoint)}, {BeginCurve}, {EndCurve})";
    }
}

/// <summary>A curve that is 0.0 until it hits the threshold, then it jumps to 1.0.</summary>
/// <remarks>Dart's field is <c>threshold</c>; C# cannot name a member after its enclosing type.</remarks>
public class Threshold : Curve
{
    public Threshold(double threshold)
    {
        ThresholdValue = threshold;
    }

    /// <summary>The value before which the curve is 0.0 and after which the curve is 1.0.</summary>
    public double ThresholdValue { get; }

    public override double TransformInternal(double t)
    {
        DebugAssertions.Assert(ThresholdValue >= 0.0);
        DebugAssertions.Assert(ThresholdValue <= 1.0);
        return t < ThresholdValue ? 0.0 : 1.0;
    }
}

/// <summary>
/// A cubic polynomial mapping of the unit interval: the curve through (0, 0) and (1, 1) with control
/// points (<see cref="A"/>, <see cref="B"/>) and (<see cref="C"/>, <see cref="D"/>).
/// </summary>
public class Cubic : Curve
{
    private const double CubicErrorBound = 0.001;

    public Cubic(double a, double b, double c, double d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    /// <summary>The x coordinate of the first control point.</summary>
    public double A { get; }

    /// <summary>The y coordinate of the first control point.</summary>
    public double B { get; }

    /// <summary>The x coordinate of the second control point.</summary>
    public double C { get; }

    /// <summary>The y coordinate of the second control point.</summary>
    public double D { get; }

    private static double EvaluateCubic(double a, double b, double m)
    {
        return (3 * a * (1 - m) * (1 - m) * m) + (3 * b * (1 - m) * m * m) + (m * m * m);
    }

    public override double TransformInternal(double t)
    {
        if (double.IsNaN(t))
        {
            throw new ArgumentException("must not be NaN", nameof(t));
        }

        if (t <= 0.0)
        {
            return 0.0;
        }

        if (t >= 1.0)
        {
            return 1.0;
        }

        double start = 0.0;
        double end = 1.0;
        while (true)
        {
            double midpoint = (start + end) / 2;
            double estimate = EvaluateCubic(A, C, midpoint);
            if (Math.Abs(t - estimate) < CubicErrorBound)
            {
                return EvaluateCubic(B, D, midpoint);
            }

            if (estimate < t)
            {
                start = midpoint;
            }
            else
            {
                end = midpoint;
            }
        }
    }

    public override string ToString()
    {
        return $"{Diagnostics.ObjectRuntimeType(this, "Cubic")}({Fixed2(A)}, {Fixed2(B)}, {Fixed2(C)}, {Fixed2(D)})";
    }

    private static string Fixed2(double value) => Diagnostics.ToStringAsFixed(value, 2);
}

/// <summary>
/// A cubic polynomial composed of two curves that share a common center point, joined at
/// <see cref="Midpoint"/> (Dart's <c>Offset</c>s are Avalonia <see cref="Point"/>s).
/// </summary>
public class ThreePointCubic : Curve
{
    public ThreePointCubic(Point a1, Point b1, Point midpoint, Point a2, Point b2)
    {
        A1 = a1;
        B1 = b1;
        Midpoint = midpoint;
        A2 = a2;
        B2 = b2;
    }

    /// <summary>The coordinates of the first control point of the first curve.</summary>
    public Point A1 { get; }

    /// <summary>The coordinates of the second control point of the first curve.</summary>
    public Point B1 { get; }

    /// <summary>The coordinates of the middle shared point.</summary>
    public Point Midpoint { get; }

    /// <summary>The coordinates of the first control point of the second curve.</summary>
    public Point A2 { get; }

    /// <summary>The coordinates of the second control point of the second curve.</summary>
    public Point B2 { get; }

    public override double TransformInternal(double t)
    {
        bool firstCurve = t < Midpoint.X;
        double scaleX = firstCurve ? Midpoint.X : 1.0 - Midpoint.X;
        double scaleY = firstCurve ? Midpoint.Y : 1.0 - Midpoint.Y;
        double scaledT = (t - (firstCurve ? 0.0 : Midpoint.X)) / scaleX;
        if (firstCurve)
        {
            return new Cubic(A1.X / scaleX, A1.Y / scaleY, B1.X / scaleX, B1.Y / scaleY).Transform(scaledT)
                   * scaleY;
        }
        else
        {
            return (new Cubic(
                        (A2.X - Midpoint.X) / scaleX,
                        (A2.Y - Midpoint.Y) / scaleY,
                        (B2.X - Midpoint.X) / scaleX,
                        (B2.Y - Midpoint.Y) / scaleY).Transform(scaledT)
                    * scaleY)
                   + Midpoint.Y;
        }
    }

    public override string ToString()
    {
        string described = $"ThreePointCubic({CurveFormat.O(A1)}, {CurveFormat.O(B1)}, {CurveFormat.O(Midpoint)}, "
                           + $"{CurveFormat.O(A2)}, {CurveFormat.O(B2)})";
        return $"{Diagnostics.ObjectRuntimeType(this, described)} ";
    }
}

/// <summary>
/// Abstract class that defines an API for evaluating 2D parametric curves, whose values are Dart's
/// <c>Offset</c> (Avalonia's <see cref="Point"/>).
/// </summary>
public abstract class Curve2D : ParametricCurve<Point>
{
    protected Curve2D()
    {
    }

    /// <summary>
    /// Generates a list of samples with a recursive subdivision until a tolerance of flatness is reached.
    /// </summary>
    public IEnumerable<Curve2DSample> GenerateSamples(double start = 0.0, double end = 1.0, double tolerance = 1e-10)
    {
        DebugAssertions.Assert(end > start);
        var rand = new DartRandom(SamplingSeed);

        bool IsFlat(Point p, Point q, Point r)
        {
            Point pr = CurveFormat.Sub(p, r);
            Point qr = CurveFormat.Sub(q, r);
            double z = (pr.X * qr.Y) - (qr.X * pr.Y);
            return (z * z) < tolerance;
        }

        var first = new Curve2DSample(start, Transform(start));
        var last = new Curve2DSample(end, Transform(end));
        var samples = new List<Curve2DSample> { first };

        void Sample(Curve2DSample p, Curve2DSample q, bool forceSubdivide = false)
        {
            double t = p.T + ((0.45 + (0.1 * rand.NextDouble())) * (q.T - p.T));
            var r = new Curve2DSample(t, Transform(t));

            if (!forceSubdivide && IsFlat(p.Value, q.Value, r.Value))
            {
                samples.Add(q);
            }
            else
            {
                Sample(p, r);
                Sample(r, q);
            }
        }

        Sample(
            first,
            last,
            forceSubdivide: Math.Abs(first.Value.X - last.Value.X) < tolerance
                            && Math.Abs(first.Value.Y - last.Value.Y) < tolerance);
        return samples;
    }

    /// <summary>Returns a seed value used by <see cref="GenerateSamples"/> to seed a random number generator.</summary>
    protected virtual long SamplingSeed => 0;

    /// <summary>Returns the parameter <c>t</c> that corresponds to the given x value of the spline.</summary>
    public double FindInverse(double x)
    {
        double start = 0.0;
        double end = 1.0;
        double mid = double.NaN;
        double OffsetToOrigin(double pos) => x - Transform(pos).X;

        const double errorLimit = 1e-6;
        int count = 100;
        double startValue = OffsetToOrigin(start);
        while ((end - start) / 2.0 > errorLimit && count > 0)
        {
            mid = (end + start) / 2.0;
            double value = OffsetToOrigin(mid);
            if (CurveFormat.Sign(value) == CurveFormat.Sign(startValue))
            {
                start = mid;
            }
            else
            {
                end = mid;
            }

            count--;
        }

        return mid;
    }
}

/// <summary>A class that holds a sample of a 2D parametric curve, containing the value and the parameter.</summary>
public class Curve2DSample
{
    public Curve2DSample(double t, Point value)
    {
        T = t;
        Value = value;
    }

    /// <summary>The parametric location of this sample point along the curve.</summary>
    public double T { get; }

    /// <summary>The value (the output of the curve function) at <see cref="T"/>.</summary>
    public Point Value { get; }

    public override string ToString()
    {
        return $"[({Diagnostics.ToStringAsFixed(Value.X, 2)}, {Diagnostics.ToStringAsFixed(Value.Y, 2)}), "
               + $"{Diagnostics.ToStringAsFixed(T, 2)}]";
    }
}

/// <summary>
/// A 2D spline that passes smoothly through the given control points using a centripetal Catmull-Rom
/// spline.
/// </summary>
public class CatmullRomSpline : Curve2D
{
    private readonly List<List<Point>> _cubicSegments;
    private readonly IReadOnlyList<Point>? _controlPoints;
    private readonly Point? _startHandle;
    private readonly Point? _endHandle;
    private readonly double? _tension;

    /// <summary>Constructs a centripetal Catmull-Rom spline curve; segments are computed on first use.</summary>
    public CatmullRomSpline(
        IReadOnlyList<Point> controlPoints,
        double tension = 0.0,
        Point? startHandle = null,
        Point? endHandle = null)
    {
        AssertArguments(controlPoints, tension);
        _controlPoints = controlPoints;
        _startHandle = startHandle;
        _endHandle = endHandle;
        _tension = tension;
        _cubicSegments = [];
    }

    private CatmullRomSpline(List<List<Point>> cubicSegments)
    {
        _cubicSegments = cubicSegments;
    }

    /// <summary>Constructs a centripetal Catmull-Rom spline curve with its segments precomputed.</summary>
    public static CatmullRomSpline Precompute(
        IReadOnlyList<Point> controlPoints,
        double tension = 0.0,
        Point? startHandle = null,
        Point? endHandle = null)
    {
        AssertArguments(controlPoints, tension);
        return new CatmullRomSpline(
            ComputeSegments(controlPoints, tension, startHandle: startHandle, endHandle: endHandle));
    }

    // The initializer-list asserts shared by both Dart constructors.
    private static void AssertArguments(IReadOnlyList<Point> controlPoints, double tension)
    {
        DebugAssertions.Assert(tension <= 1.0, $"tension {CurveFormat.D(tension)} must not be greater than 1.0.");
        DebugAssertions.Assert(tension >= 0.0, $"tension {CurveFormat.D(tension)} must not be negative.");
        DebugAssertions.Assert(
            controlPoints.Count > 3,
            "There must be at least four control points to create a CatmullRomSpline.");
    }

    private static List<List<Point>> ComputeSegments(
        IReadOnlyList<Point> controlPoints,
        double tension,
        Point? startHandle = null,
        Point? endHandle = null)
    {
        DebugAssertions.Assert(
            startHandle is null || CurveFormat.IsFinite(startHandle.Value),
            "The provided startHandle of CatmullRomSpline must be finite. The startHandle given was "
            + $"{CurveFormat.O(startHandle)}.");
        DebugAssertions.Assert(
            endHandle is null || CurveFormat.IsFinite(endHandle.Value),
            "The provided endHandle of CatmullRomSpline must be finite. The endHandle given was "
            + $"{CurveFormat.O(endHandle)}.");
        if (Constants.KDebugMode)
        {
            for (int index = 0; index < controlPoints.Count; index++)
            {
                if (!CurveFormat.IsFinite(controlPoints[index]))
                {
                    throw new FlutterError(
                        $"The provided CatmullRomSpline control point at index {index} is not finite. "
                        + $"The control point given was {CurveFormat.O(controlPoints[index])}.");
                }
            }
        }

        // If not specified, select the first and last control points (which are
        // handles: they are not intersected by the resulting curve) so that they
        // extend the first and last segments, respectively.
        Point start = startHandle
                      ?? CurveFormat.Sub(CurveFormat.Mul(controlPoints[0], 2.0), controlPoints[1]);
        Point end = endHandle
                    ?? CurveFormat.Sub(
                        CurveFormat.Mul(controlPoints[^1], 2.0),
                        controlPoints[controlPoints.Count - 2]);
        var allPoints = new List<Point>(controlPoints.Count + 2) { start };
        allPoints.AddRange(controlPoints);
        allPoints.Add(end);

        // An alpha of 0.5 is what makes it a centripetal Catmull-Rom spline. A
        // value of 0.0 would make it a uniform Catmull-Rom spline, and a value of
        // 1.0 would make it a chordal Catmull-Rom spline. Non-centripetal values
        // for alpha can give self-intersecting behavior or looping within a
        // segment.
        const double alpha = 0.5;
        double reverseTension = 1.0 - tension;
        var result = new List<List<Point>>();
        for (int i = 0; i < allPoints.Count - 3; ++i)
        {
            Point[] curve = [allPoints[i], allPoints[i + 1], allPoints[i + 2], allPoints[i + 3]];
            Point diffCurve10 = CurveFormat.Sub(curve[1], curve[0]);
            Point diffCurve21 = CurveFormat.Sub(curve[2], curve[1]);
            Point diffCurve32 = CurveFormat.Sub(curve[3], curve[2]);
            double t01 = Math.Pow(CurveFormat.Distance(diffCurve10), alpha);
            double t12 = Math.Pow(CurveFormat.Distance(diffCurve21), alpha);
            double t23 = Math.Pow(CurveFormat.Distance(diffCurve32), alpha);

            Point m1 = CurveFormat.Mul(
                CurveFormat.Add(
                    diffCurve21,
                    CurveFormat.Mul(
                        CurveFormat.Sub(
                            CurveFormat.Div(diffCurve10, t01),
                            CurveFormat.Div(CurveFormat.Sub(curve[2], curve[0]), t01 + t12)),
                        t12)),
                reverseTension);
            Point m2 = CurveFormat.Mul(
                CurveFormat.Add(
                    diffCurve21,
                    CurveFormat.Mul(
                        CurveFormat.Sub(
                            CurveFormat.Div(diffCurve32, t23),
                            CurveFormat.Div(CurveFormat.Sub(curve[3], curve[1]), t12 + t23)),
                        t12)),
                reverseTension);
            Point sumM12 = CurveFormat.Add(m1, m2);

            var segment = new List<Point>
            {
                CurveFormat.Add(CurveFormat.Mul(diffCurve21, -2.0), sumM12),
                CurveFormat.Sub(CurveFormat.Sub(CurveFormat.Mul(diffCurve21, 3.0), m1), sumM12),
                m1,
                curve[1],
            };
            result.Add(segment);
        }

        return result;
    }

    private void InitializeIfNeeded()
    {
        if (_cubicSegments.Count > 0)
        {
            return;
        }

        _cubicSegments.AddRange(
            ComputeSegments(_controlPoints!, _tension!.Value, startHandle: _startHandle, endHandle: _endHandle));
    }

    protected override long SamplingSeed
    {
        get
        {
            InitializeIfNeeded();
            Point seedPoint = _cubicSegments[0][1];
            return (long)Math.Round((seedPoint.X + seedPoint.Y) * 10000, MidpointRounding.AwayFromZero);
        }
    }

    public override Point TransformInternal(double t)
    {
        InitializeIfNeeded();
        double length = _cubicSegments.Count;
        double position;
        double localT;
        int index;
        if (t < 1.0)
        {
            position = t * length;
            localT = CurveFormat.Mod1(position);
            index = (int)Math.Floor(position);
        }
        else
        {
            position = length;
            localT = 1.0;
            index = _cubicSegments.Count - 1;
        }

        List<Point> cubicControlPoints = _cubicSegments[index];
        double localT2 = localT * localT;
        return CurveFormat.Add(
            CurveFormat.Add(
                CurveFormat.Add(
                    CurveFormat.Mul(CurveFormat.Mul(cubicControlPoints[0], localT2), localT),
                    CurveFormat.Mul(cubicControlPoints[1], localT2)),
                CurveFormat.Mul(cubicControlPoints[2], localT)),
            cubicControlPoints[3]);
    }
}

/// <summary>
/// An animation easing curve that passes smoothly through the given control points using a centripetal
/// Catmull-Rom spline.
/// </summary>
public class CatmullRomCurve : Curve
{
    // Dart's static `_debugAssertReasons`, shared by every instance.
    private static readonly List<string> DebugAssertReasons = [];

    private readonly List<Curve2DSample> _precomputedSamples;

    /// <summary>Constructs a centripetal <see cref="CatmullRomCurve"/>; samples are computed on first use.</summary>
    public CatmullRomCurve(IReadOnlyList<Point> controlPoints, double tension = 0.0)
    {
        AssertValid(controlPoints, tension);
        ControlPoints = controlPoints;
        Tension = tension;
        _precomputedSamples = [];
    }

    private CatmullRomCurve(IReadOnlyList<Point> controlPoints, double tension, List<Curve2DSample> samples)
    {
        ControlPoints = controlPoints;
        Tension = tension;
        _precomputedSamples = samples;
    }

    /// <summary>Constructs a centripetal <see cref="CatmullRomCurve"/> with its samples precomputed.</summary>
    public static CatmullRomCurve Precompute(IReadOnlyList<Point> controlPoints, double tension = 0.0)
    {
        AssertValid(controlPoints, tension);
        return new CatmullRomCurve(controlPoints, tension, ComputeSamples(controlPoints, tension));
    }

    /// <summary>The control points used to create this curve.</summary>
    public IReadOnlyList<Point> ControlPoints { get; }

    /// <summary>The "tension" of the curve.</summary>
    public double Tension { get; }

    private static void AssertValid(IReadOnlyList<Point> controlPoints, double tension)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        DebugAssertReasons.Clear();
        if (!ValidateControlPoints(controlPoints, tension: tension, reasons: DebugAssertReasons))
        {
            string points = "[" + string.Join(", ", controlPoints.Select(CurveFormat.O)) + "]";
            throw new AssertionError(
                $"control points {points} could not be validated:\n  {string.Join("\n  ", DebugAssertReasons)}");
        }
    }

    private static List<Curve2DSample> ComputeSamples(IReadOnlyList<Point> controlPoints, double tension)
    {
        var points = new List<Point>(controlPoints.Count + 2) { new Point(0.0, 0.0) };
        points.AddRange(controlPoints);
        points.Add(new Point(1.0, 1.0));
        return CatmullRomSpline.Precompute(points, tension: tension).GenerateSamples(tolerance: 1e-12).ToList();
    }

    /// <summary>
    /// Validates that a given set of control points for a <see cref="CatmullRomCurve"/> is well-formed and
    /// will not produce a spline that self-intersects. Reasons are appended to <paramref name="reasons"/> in
    /// debug builds.
    /// </summary>
    public static bool ValidateControlPoints(
        IReadOnlyList<Point>? controlPoints,
        double tension = 0.0,
        List<string>? reasons = null)
    {
        if (controlPoints is null)
        {
            if (Constants.KDebugMode)
            {
                reasons?.Add("Supplied control points cannot be null");
            }

            return false;
        }

        if (controlPoints.Count < 2)
        {
            if (Constants.KDebugMode)
            {
                reasons?.Add("There must be at least two points supplied to create a valid curve.");
            }

            return false;
        }

        var withEnds = new List<Point>(controlPoints.Count + 2) { new Point(0.0, 0.0) };
        withEnds.AddRange(controlPoints);
        withEnds.Add(new Point(1.0, 1.0));
        Point startHandle = CurveFormat.Sub(CurveFormat.Mul(withEnds[0], 2.0), withEnds[1]);
        Point endHandle = CurveFormat.Sub(CurveFormat.Mul(withEnds[^1], 2.0), withEnds[withEnds.Count - 2]);
        var points = new List<Point>(withEnds.Count + 2) { startHandle };
        points.AddRange(withEnds);
        points.Add(endHandle);
        double lastX = double.NegativeInfinity;
        for (int i = 0; i < points.Count; ++i)
        {
            if (i > 1 && i < points.Count - 2 && (points[i].X <= 0.0 || points[i].X >= 1.0))
            {
                if (Constants.KDebugMode)
                {
                    reasons?.Add(
                        "Control points must have X values between 0.0 and 1.0, exclusive. "
                        + $"Point {i} has an x value ({CurveFormat.D(points[i].X)}) which is outside the range.");
                }

                return false;
            }

            if (points[i].X <= lastX)
            {
                if (Constants.KDebugMode)
                {
                    reasons?.Add(
                        "Each X coordinate must be greater than the preceding X coordinate "
                        + "(i.e. must be monotonically increasing in X). Point "
                        + $"{i} has an x value of {CurveFormat.D(points[i].X)}, which is not greater than "
                        + CurveFormat.D(lastX));
                }

                return false;
            }

            lastX = points[i].X;
        }

        bool success = true;

        // An empty control point list would pass the tests above, but we want to
        // be able to spot a self-intersecting spline before it causes problems.
        lastX = double.NegativeInfinity;
        const double tolerance = 1e-3;
        var testSpline = new CatmullRomSpline(points, tension: tension);
        double start = testSpline.FindInverse(0.0);
        double end = testSpline.FindInverse(1.0);
        List<Curve2DSample> samplePoints = testSpline.GenerateSamples(start: start, end: end).ToList();

        // If the first and last points in the samples aren't at (0,0) or (1,1)
        // respectively, then the curve is multi-valued at the ends.
        if (Math.Abs(samplePoints[0].Value.Y) > tolerance || Math.Abs(1.0 - samplePoints[^1].Value.Y) > tolerance)
        {
            bool bail = true;
            success = false;
            if (Constants.KDebugMode)
            {
                reasons?.Add(
                    $"The curve has more than one Y value at X = {CurveFormat.D(samplePoints[0].Value.X)}. "
                    + "Try moving some control points further away from this value of X, or increasing "
                    + "the tension.");
                bail = reasons is null;
            }

            if (bail)
            {
                return false;
            }
        }

        foreach (Curve2DSample sample in samplePoints)
        {
            Point point = sample.Value;
            double t = sample.T;
            double x = point.X;
            if (t >= start && t <= end && (x < -1e-3 || x > 1.0 + 1e-3))
            {
                bool bail = true;
                success = false;
                if (Constants.KDebugMode)
                {
                    reasons?.Add(
                        $"The resulting curve has an X value ({CurveFormat.D(x)}) which is outside "
                        + "the range [0.0, 1.0], inclusive.");
                    bail = reasons is null;
                }

                if (bail)
                {
                    return false;
                }
            }

            if (x < lastX)
            {
                bool bail = true;
                success = false;
                if (Constants.KDebugMode)
                {
                    reasons?.Add(
                        $"The curve has more than one Y value at x = {CurveFormat.D(x)}. Try moving "
                        + "some control points further apart in X, or increasing the tension.");
                    bail = reasons is null;
                }

                if (bail)
                {
                    return false;
                }
            }

            lastX = x;
        }

        return success;
    }

    public override double TransformInternal(double t)
    {
        // Linearly interpolate between the two closest samples generated when the
        // curve was created.
        if (_precomputedSamples.Count == 0)
        {
            // Compute the samples now if we were constructed lazily.
            _precomputedSamples.AddRange(ComputeSamples(ControlPoints, Tension));
        }

        int start = 0;
        int end = _precomputedSamples.Count - 1;
        int mid;
        Point value;
        Point startValue = _precomputedSamples[start].Value;
        Point endValue = _precomputedSamples[end].Value;
        // Use a binary search to find the index of the sample point that is just
        // before t.
        while (end - start > 1)
        {
            mid = (end + start) / 2;
            value = _precomputedSamples[mid].Value;
            if (t >= value.X)
            {
                start = mid;
                startValue = value;
            }
            else
            {
                end = mid;
                endValue = value;
            }
        }

        // Now interpolate between the found sample and the next one.
        double t2 = (t - startValue.X) / (endValue.X - startValue.X);
        return CurveFormat.LerpDouble(startValue.Y, endValue.Y, t2);
    }
}

/// <summary>A curve that is the reversed inversion of its given curve.</summary>
public class FlippedCurve : Curve
{
    public FlippedCurve(Curve curve)
    {
        Curve = curve;
    }

    /// <summary>The curve that is being flipped.</summary>
    public Curve Curve { get; }

    public override double TransformInternal(double t) => 1.0 - Curve.Transform(1.0 - t);

    public override string ToString() => $"{Diagnostics.ObjectRuntimeType(this, "FlippedCurve")}({Curve})";
}

/// <summary>
/// Dart's private <c>_DecelerateCurve</c>: a curve where the rate of change starts out quickly and then
/// decelerates.
/// </summary>
internal sealed class DecelerateCurve : Curve
{
    public override double TransformInternal(double t)
    {
        // Intended to match the behavior of:
        // https://android.googlesource.com/platform/frameworks/base/+/main/core/java/android/view/animation/
        // DecelerateInterpolator.java
        // ...as of December 2016.
        t = 1.0 - t;
        return 1.0 - (t * t);
    }
}

/// <summary>Dart's private <c>_BounceInCurve</c>.</summary>
internal sealed class BounceInCurve : Curve
{
    public override double TransformInternal(double t) => 1.0 - BounceMath.Bounce(1.0 - t);
}

/// <summary>Dart's private <c>_BounceOutCurve</c>.</summary>
internal sealed class BounceOutCurve : Curve
{
    public override double TransformInternal(double t) => BounceMath.Bounce(t);
}

/// <summary>Dart's private <c>_BounceInOutCurve</c>.</summary>
internal sealed class BounceInOutCurve : Curve
{
    public override double TransformInternal(double t)
    {
        if (t < 0.5)
        {
            return (1.0 - BounceMath.Bounce(1.0 - (t * 2.0))) * 0.5;
        }
        else
        {
            return (BounceMath.Bounce((t * 2.0) - 1.0) * 0.5) + 0.5;
        }
    }
}

/// <summary>Dart's top-level private <c>_bounce</c>.</summary>
internal static class BounceMath
{
    public static double Bounce(double t)
    {
        if (t < 1.0 / 2.75)
        {
            return 7.5625 * t * t;
        }
        else if (t < 2 / 2.75)
        {
            t -= 1.5 / 2.75;
            return (7.5625 * t * t) + 0.75;
        }
        else if (t < 2.5 / 2.75)
        {
            t -= 2.25 / 2.75;
            return (7.5625 * t * t) + 0.9375;
        }

        t -= 2.625 / 2.75;
        return (7.5625 * t * t) + 0.984375;
    }
}

/// <summary>An oscillating curve that grows in magnitude while overshooting its bounds.</summary>
public class ElasticInCurve : Curve
{
    public ElasticInCurve(double period = 0.4)
    {
        Period = period;
    }

    /// <summary>The duration of the oscillation.</summary>
    public double Period { get; }

    public override double TransformInternal(double t)
    {
        double s = Period / 4.0;
        t -= 1.0;
        return -Math.Pow(2.0, 10.0 * t) * Math.Sin((t - s) * (Math.PI * 2.0) / Period);
    }

    public override string ToString()
    {
        return $"{Diagnostics.ObjectRuntimeType(this, "ElasticInCurve")}({CurveFormat.D(Period)})";
    }
}

/// <summary>An oscillating curve that shrinks in magnitude while overshooting its bounds.</summary>
public class ElasticOutCurve : Curve
{
    public ElasticOutCurve(double period = 0.4)
    {
        Period = period;
    }

    /// <summary>The duration of the oscillation.</summary>
    public double Period { get; }

    public override double TransformInternal(double t)
    {
        double s = Period / 4.0;
        return (Math.Pow(2.0, -10 * t) * Math.Sin((t - s) * (Math.PI * 2.0) / Period)) + 1.0;
    }

    public override string ToString()
    {
        return $"{Diagnostics.ObjectRuntimeType(this, "ElasticOutCurve")}({CurveFormat.D(Period)})";
    }
}

/// <summary>
/// An oscillating curve that grows and then shrinks in magnitude while overshooting its bounds.
/// </summary>
public class ElasticInOutCurve : Curve
{
    public ElasticInOutCurve(double period = 0.4)
    {
        Period = period;
    }

    /// <summary>The duration of the oscillation.</summary>
    public double Period { get; }

    public override double TransformInternal(double t)
    {
        double s = Period / 4.0;
        t = (2.0 * t) - 1.0;
        if (t < 0.0)
        {
            return -0.5 * Math.Pow(2.0, 10.0 * t) * Math.Sin((t - s) * (Math.PI * 2.0) / Period);
        }
        else
        {
            return (Math.Pow(2.0, -10.0 * t) * Math.Sin((t - s) * (Math.PI * 2.0) / Period) * 0.5) + 1.0;
        }
    }

    public override string ToString()
    {
        return $"{Diagnostics.ObjectRuntimeType(this, "ElasticInOutCurve")}({CurveFormat.D(Period)})";
    }
}

/// <summary>A collection of common animation curves.</summary>
/// <remarks>
/// Dart's <c>static const</c> members are <c>static readonly</c> fields, so each is one shared instance.
/// </remarks>
public static class Curves
{
    /// <summary>A linear animation curve.</summary>
    public static readonly Curve Linear = new LinearCurve();

    /// <summary>A curve where the rate of change starts out quickly and then decelerates.</summary>
    public static readonly Curve Decelerate = new DecelerateCurve();

    /// <summary>A curve that is very steep and linear at the beginning, but quickly flattens out.</summary>
    public static readonly Cubic FastLinearToSlowEaseIn = new(0.18, 1.0, 0.04, 1.0);

    /// <summary>A curve that starts slowly, speeds up very quickly, and then ends slowly.</summary>
    public static readonly ThreePointCubic FastEaseInToSlowEaseOut = new(
        new Point(0.056, 0.024),
        new Point(0.108, 0.3085),
        new Point(0.198, 0.541),
        new Point(0.3655, 1.0),
        new Point(0.5465, 0.989));

    /// <summary>A cubic animation curve that speeds up quickly and ends slowly (CSS <c>ease</c>).</summary>
    public static readonly Cubic Ease = new(0.25, 0.1, 0.25, 1.0);

    /// <summary>A cubic animation curve that starts slowly and ends quickly (CSS <c>ease-in</c>).</summary>
    public static readonly Cubic EaseIn = new(0.42, 0.0, 1.0, 1.0);

    /// <summary>A cubic animation curve that starts starts slowly and ends linearly.</summary>
    public static readonly Cubic EaseInToLinear = new(0.67, 0.03, 0.65, 0.09);

    /// <summary>A cubic animation curve that starts slowly and ends quickly (Penner's sine).</summary>
    public static readonly Cubic EaseInSine = new(0.47, 0.0, 0.745, 0.715);

    /// <summary>A cubic animation curve (Penner's quadratic ease-in).</summary>
    public static readonly Cubic EaseInQuad = new(0.55, 0.085, 0.68, 0.53);

    /// <summary>A cubic animation curve (Penner's cubic ease-in).</summary>
    public static readonly Cubic EaseInCubic = new(0.55, 0.055, 0.675, 0.19);

    /// <summary>A cubic animation curve (Penner's quartic ease-in).</summary>
    public static readonly Cubic EaseInQuart = new(0.895, 0.03, 0.685, 0.22);

    /// <summary>A cubic animation curve (Penner's quintic ease-in).</summary>
    public static readonly Cubic EaseInQuint = new(0.755, 0.05, 0.855, 0.06);

    /// <summary>A cubic animation curve (Penner's exponential ease-in).</summary>
    public static readonly Cubic EaseInExpo = new(0.95, 0.05, 0.795, 0.035);

    /// <summary>A cubic animation curve (Penner's circular ease-in).</summary>
    public static readonly Cubic EaseInCirc = new(0.6, 0.04, 0.98, 0.335);

    /// <summary>A cubic animation curve that starts slowly and ends quickly, first backing up.</summary>
    public static readonly Cubic EaseInBack = new(0.6, -0.28, 0.735, 0.045);

    /// <summary>A cubic animation curve that starts quickly and ends slowly (CSS <c>ease-out</c>).</summary>
    public static readonly Cubic EaseOut = new(0.0, 0.0, 0.58, 1.0);

    /// <summary>A cubic animation curve that starts linearly and ends slowly.</summary>
    public static readonly Cubic LinearToEaseOut = new(0.35, 0.91, 0.33, 0.97);

    /// <summary>A cubic animation curve (Penner's sine ease-out).</summary>
    public static readonly Cubic EaseOutSine = new(0.39, 0.575, 0.565, 1.0);

    /// <summary>A cubic animation curve (Penner's quadratic ease-out).</summary>
    public static readonly Cubic EaseOutQuad = new(0.25, 0.46, 0.45, 0.94);

    /// <summary>A cubic animation curve (Penner's cubic ease-out).</summary>
    public static readonly Cubic EaseOutCubic = new(0.215, 0.61, 0.355, 1.0);

    /// <summary>A cubic animation curve (Penner's quartic ease-out).</summary>
    public static readonly Cubic EaseOutQuart = new(0.165, 0.84, 0.44, 1.0);

    /// <summary>A cubic animation curve (Penner's quintic ease-out).</summary>
    public static readonly Cubic EaseOutQuint = new(0.23, 1.0, 0.32, 1.0);

    /// <summary>A cubic animation curve (Penner's exponential ease-out).</summary>
    public static readonly Cubic EaseOutExpo = new(0.19, 1.0, 0.22, 1.0);

    /// <summary>A cubic animation curve (Penner's circular ease-out).</summary>
    public static readonly Cubic EaseOutCirc = new(0.075, 0.82, 0.165, 1.0);

    /// <summary>A cubic animation curve that starts quickly and ends slowly, overshooting first.</summary>
    public static readonly Cubic EaseOutBack = new(0.175, 0.885, 0.32, 1.275);

    /// <summary>
    /// A cubic animation curve that starts slowly, speeds up, and then ends slowly (CSS <c>ease-in-out</c>).
    /// </summary>
    public static readonly Cubic EaseInOut = new(0.42, 0.0, 0.58, 1.0);

    /// <summary>A cubic animation curve (Penner's sine ease-in-out).</summary>
    public static readonly Cubic EaseInOutSine = new(0.445, 0.05, 0.55, 0.95);

    /// <summary>A cubic animation curve (Penner's quadratic ease-in-out).</summary>
    public static readonly Cubic EaseInOutQuad = new(0.455, 0.03, 0.515, 0.955);

    /// <summary>A cubic animation curve (Penner's cubic ease-in-out).</summary>
    public static readonly Cubic EaseInOutCubic = new(0.645, 0.045, 0.355, 1.0);

    /// <summary>
    /// A cubic animation curve that starts slowly, speeds up shortly thereafter, and then ends slowly.
    /// </summary>
    public static readonly ThreePointCubic EaseInOutCubicEmphasized = new(
        new Point(0.05, 0),
        new Point(0.133333, 0.06),
        new Point(0.166666, 0.4),
        new Point(0.208333, 0.82),
        new Point(0.25, 1));

    /// <summary>A cubic animation curve (Penner's quartic ease-in-out).</summary>
    public static readonly Cubic EaseInOutQuart = new(0.77, 0.0, 0.175, 1.0);

    /// <summary>A cubic animation curve (Penner's quintic ease-in-out).</summary>
    public static readonly Cubic EaseInOutQuint = new(0.86, 0.0, 0.07, 1.0);

    /// <summary>A cubic animation curve (Penner's exponential ease-in-out).</summary>
    public static readonly Cubic EaseInOutExpo = new(1.0, 0.0, 0.0, 1.0);

    /// <summary>A cubic animation curve (Penner's circular ease-in-out).</summary>
    public static readonly Cubic EaseInOutCirc = new(0.785, 0.135, 0.15, 0.86);

    /// <summary>
    /// A cubic animation curve that starts slowly, speeds up, and ends slowly, overshooting both ends.
    /// </summary>
    public static readonly Cubic EaseInOutBack = new(0.68, -0.55, 0.265, 1.55);

    /// <summary>
    /// A curve that starts quickly and eases into its final position (Material's <c>Easing.legacy</c>).
    /// </summary>
    public static readonly Cubic FastOutSlowIn = new(0.4, 0.0, 0.2, 1.0);

    /// <summary>A cubic animation curve that starts quickly, slows down, and then ends quickly.</summary>
    public static readonly Cubic SlowMiddle = new(0.15, 0.85, 0.85, 0.15);

    /// <summary>An oscillating curve that grows in magnitude.</summary>
    public static readonly Curve BounceIn = new BounceInCurve();

    /// <summary>An oscillating curve that first grows and then shrink in magnitude.</summary>
    public static readonly Curve BounceOut = new BounceOutCurve();

    /// <summary>An oscillating curve that first grows and then shrink in magnitude.</summary>
    public static readonly Curve BounceInOut = new BounceInOutCurve();

    /// <summary>An oscillating curve that grows in magnitude while overshooting its bounds.</summary>
    public static readonly ElasticInCurve ElasticIn = new();

    /// <summary>An oscillating curve that shrinks in magnitude while overshooting its bounds.</summary>
    public static readonly ElasticOutCurve ElasticOut = new();

    /// <summary>An oscillating curve that grows and then shrinks in magnitude while overshooting its bounds.</summary>
    public static readonly ElasticInOutCurve ElasticInOut = new();
}

/// <summary>C#-only helpers for the dart:ui arithmetic and formatting the curve ports need.</summary>
internal static class CurveFormat
{
    // Dart's `double.toString()`.
    public static string D(double value) => BindingBase.DartDoubleToString(value);

    // dart:ui `Offset.toString()`.
    public static string O(Point? value) => Diagnostics.DescribeValue(value);

    public static string O(Point value) => Diagnostics.DescribeValue(value);

    // dart:ui `clampDouble`.
    public static double ClampDouble(double x, double min, double max)
    {
        DebugAssertions.Assert(min <= max && !double.IsNaN(max) && !double.IsNaN(min));
        if (x < min)
        {
            return min;
        }

        if (x > max)
        {
            return max;
        }

        if (double.IsNaN(x))
        {
            return max;
        }

        return x;
    }

    // dart:ui `lerpDouble` over non-null operands.
    public static double LerpDouble(double a, double b, double t)
    {
        if (a == b || (double.IsNaN(a) && double.IsNaN(b)))
        {
            return a;
        }

        DebugAssertions.Assert(double.IsFinite(a), "Cannot interpolate between finite and non-finite values");
        DebugAssertions.Assert(double.IsFinite(b), "Cannot interpolate between finite and non-finite values");
        DebugAssertions.Assert(double.IsFinite(t), "t must be finite when interpolating between values");
        return (a * (1.0 - t)) + (b * t);
    }

    // Dart's `double.sign`: -1.0, 1.0, or the value itself for zeros and NaN.
    public static double Sign(double value) => value > 0.0 ? 1.0 : value < 0.0 ? -1.0 : value;

    // Dart's Euclidean `double % 1.0`.
    public static double Mod1(double value)
    {
        double result = value % 1.0;
        return result < 0.0 ? result + 1.0 : result;
    }

    public static Point Add(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);

    public static Point Sub(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);

    public static Point Mul(Point a, double k) => new(a.X * k, a.Y * k);

    public static Point Div(Point a, double k) => new(a.X / k, a.Y / k);

    public static double Distance(Point a) => Math.Sqrt((a.X * a.X) + (a.Y * a.Y));

    public static bool IsFinite(Point a) => double.IsFinite(a.X) && double.IsFinite(a.Y);
}
