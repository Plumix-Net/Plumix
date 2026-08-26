using System;
using Avalonia;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/arc.dart

/// <summary>Dart's `_kOnAxisDelta`.</summary>
internal static class ArcConstants
{
    public const double OnAxisDelta = 2.0;
}

/// <summary>
/// Dart's `MaterialPointArcTween`: a tween that interpolates an <see cref="Point"/> along a circular arc.
/// </summary>
public class MaterialPointArcTween : Tween<Point>
{
    private bool _dirty = true;
    private Point? _center;
    private double? _radius;
    private double? _beginAngle;
    private double? _endAngle;

    public MaterialPointArcTween(Point? begin = null, Point? end = null)
    {
        if (begin.HasValue)
        {
            SetBeginValue(begin.Value);
        }

        if (end.HasValue)
        {
            SetEndValue(end.Value);
        }
    }

    /// <summary>The center of the circular arc, null if <see cref="Begin"/> and <see cref="End"/> are on-axis.</summary>
    public Point? Center
    {
        get
        {
            if (Begin is null || End is null)
            {
                return null;
            }

            if (_dirty)
            {
                Initialize();
            }

            return _center;
        }
    }

    /// <summary>The radius of the circular arc, null if <see cref="Begin"/> and <see cref="End"/> are on-axis.</summary>
    public double? Radius
    {
        get
        {
            if (Begin is null || End is null)
            {
                return null;
            }

            if (_dirty)
            {
                Initialize();
            }

            return _radius;
        }
    }

    /// <summary>The beginning of the arc's sweep in radians, measured from the positive x axis.</summary>
    public double? BeginAngle
    {
        get
        {
            if (Begin is null || End is null)
            {
                return null;
            }

            if (_dirty)
            {
                Initialize();
            }

            return _beginAngle;
        }
    }

    /// <summary>The end of the arc's sweep in radians, measured from the positive x axis.</summary>
    // Dart parity: `MaterialPointArcTween.endAngle` returns `_beginAngle`, an upstream typo kept for
    // 1:1 behavior (nothing in `arc.dart` or Flutter's own tests reads it); `Lerp` uses `_endAngle`.
    public double? EndAngle
    {
        get
        {
            if (Begin is null || End is null)
            {
                return null;
            }

            if (_dirty)
            {
                Initialize();
            }

            return _beginAngle;
        }
    }

    public new virtual Point? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value != Begin)
            {
                if (value.HasValue)
                {
                    SetBeginValue(value.Value);
                }
                else
                {
                    ClearBeginValue();
                }

                _dirty = true;
            }
        }
    }

    public new virtual Point? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value != End)
            {
                if (value.HasValue)
                {
                    SetEndValue(value.Value);
                }
                else
                {
                    ClearEndValue();
                }

                _dirty = true;
            }
        }
    }

    public override Point Lerp(Point a, Point b, double t) => Evaluate(t);

    public override Point Evaluate(double t)
    {
        if (_dirty)
        {
            Initialize();
        }

        Point begin = GetBeginValue();
        Point end = GetEndValue();
        if (t == 0.0)
        {
            return begin;
        }

        if (t == 1.0)
        {
            return end;
        }

        if (_beginAngle is null || _endAngle is null)
        {
            return new Point(
                begin.X + ((end.X - begin.X) * t),
                begin.Y + ((end.Y - begin.Y) * t));
        }

        double angle = _beginAngle.Value + ((_endAngle.Value - _beginAngle.Value) * t);
        double x = Math.Cos(angle) * _radius!.Value;
        double y = Math.Sin(angle) * _radius!.Value;
        return _center!.Value + new Vector(x, y);
    }

    public override string ToString()
    {
        return $"MaterialPointArcTween({Begin} → {End}; center={Center}, radius={Radius}, "
            + $"beginAngle={BeginAngle}, endAngle={EndAngle})";
    }

    private void Initialize()
    {
        Point begin = GetBeginValue();
        Point end = GetEndValue();
        Point delta = end - begin;
        double deltaX = Math.Abs(delta.X);
        double deltaY = Math.Abs(delta.Y);
        double distanceFromAtoB = ArcGeometry.Distance(delta);
        var c = new Point(end.X, begin.Y);

        double SweepAngle() => 2.0 * Math.Asin(distanceFromAtoB / (2.0 * _radius!.Value));

        if (deltaX > ArcConstants.OnAxisDelta && deltaY > ArcConstants.OnAxisDelta)
        {
            if (deltaX < deltaY)
            {
                _radius = distanceFromAtoB * distanceFromAtoB / ArcGeometry.Distance(c - begin) / 2.0;
                _center = new Point(end.X + (_radius.Value * ArcMath.Sign(begin.X - end.X)), end.Y);
                if (begin.X < end.X)
                {
                    _beginAngle = SweepAngle() * ArcMath.Sign(begin.Y - end.Y);
                    _endAngle = 0.0;
                }
                else
                {
                    _beginAngle = Math.PI + (SweepAngle() * ArcMath.Sign(end.Y - begin.Y));
                    _endAngle = Math.PI;
                }
            }
            else
            {
                _radius = distanceFromAtoB * distanceFromAtoB / ArcGeometry.Distance(c - end) / 2.0;
                _center = new Point(begin.X, begin.Y + (ArcMath.Sign(end.Y - begin.Y) * _radius.Value));
                if (begin.Y < end.Y)
                {
                    _beginAngle = -Math.PI / 2.0;
                    _endAngle = _beginAngle.Value + (SweepAngle() * ArcMath.Sign(end.X - begin.X));
                }
                else
                {
                    _beginAngle = Math.PI / 2.0;
                    _endAngle = _beginAngle.Value + (SweepAngle() * ArcMath.Sign(begin.X - end.X));
                }
            }
        }
        else
        {
            _beginAngle = null;
            _endAngle = null;
        }

        _dirty = false;
    }
}

/// <summary>Dart's `_CornerId`.</summary>
internal enum CornerId
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>Dart's `_Diagonal`.</summary>
internal readonly struct Diagonal
{
    public Diagonal(CornerId beginId, CornerId endId)
    {
        BeginId = beginId;
        EndId = endId;
    }

    public CornerId BeginId { get; }

    public CornerId EndId { get; }
}

/// <summary>Dart's `double.sign`, which returns -1.0/0.0/1.0 (and NaN for NaN).</summary>
internal static class ArcMath
{
    public static double Sign(double value)
    {
        if (double.IsNaN(value))
        {
            return double.NaN;
        }

        if (value > 0.0)
        {
            return 1.0;
        }

        if (value < 0.0)
        {
            return -1.0;
        }

        return value;
    }
}

/// <summary>
/// Dart's `MaterialRectArcTween`: a tween that interpolates a <see cref="Rect"/> by moving the rect's
/// most-supported diagonal corners along circular arcs.
/// </summary>
public class MaterialRectArcTween : RectTween
{
    private static readonly Diagonal[] AllDiagonals =
    [
        new(CornerId.TopLeft, CornerId.BottomRight),
        new(CornerId.BottomRight, CornerId.TopLeft),
        new(CornerId.TopRight, CornerId.BottomLeft),
        new(CornerId.BottomLeft, CornerId.TopRight),
    ];

    private bool _dirty = true;
    private MaterialPointArcTween _beginArc = null!;
    private MaterialPointArcTween _endArc = null!;

    public MaterialRectArcTween(Rect? begin = null, Rect? end = null)
        : base(begin, end)
    {
    }

    /// <summary>The path of the corner of the rectangle that moves the least.</summary>
    public MaterialPointArcTween? BeginArc
    {
        get
        {
            if (Begin is null)
            {
                return null;
            }

            if (_dirty)
            {
                Initialize();
            }

            return _beginArc;
        }
    }

    /// <summary>The path of the corner of the rectangle that moves the most.</summary>
    public MaterialPointArcTween? EndArc
    {
        get
        {
            if (End is null)
            {
                return null;
            }

            if (_dirty)
            {
                Initialize();
            }

            return _endArc;
        }
    }

    public override Rect? Begin
    {
        get => base.Begin;
        set
        {
            if (value != base.Begin)
            {
                base.Begin = value;
                _dirty = true;
            }
        }
    }

    public override Rect? End
    {
        get => base.End;
        set
        {
            if (value != base.End)
            {
                base.End = value;
                _dirty = true;
            }
        }
    }

    public override Rect Evaluate(double t)
    {
        if (_dirty)
        {
            Initialize();
        }

        Rect begin = GetBeginValue();
        Rect end = GetEndValue();
        if (t == 0.0)
        {
            return begin;
        }

        if (t == 1.0)
        {
            return end;
        }

        return ArcGeometry.RectFromPoints(_beginArc.Evaluate(t), _endArc.Evaluate(t));
    }

    public override string ToString()
    {
        return $"MaterialRectArcTween({Begin} → {End}; beginArc={BeginArc}, endArc={EndArc})";
    }

    private static Point CornerFor(Rect rect, CornerId id)
    {
        return id switch
        {
            CornerId.TopLeft => rect.TopLeft,
            CornerId.TopRight => rect.TopRight,
            CornerId.BottomLeft => rect.BottomLeft,
            _ => rect.BottomRight,
        };
    }

    private void Initialize()
    {
        Rect begin = GetBeginValue();
        Rect end = GetEndValue();
        Point centersVector = end.Center - begin.Center;
        Diagonal diagonal = AllDiagonals[0];
        double? maxKey = null;
        foreach (Diagonal candidate in AllDiagonals)
        {
            double key = DiagonalSupport(begin, centersVector, candidate);
            if (maxKey is null || key > maxKey.Value)
            {
                diagonal = candidate;
                maxKey = key;
            }
        }

        _beginArc = new MaterialPointArcTween(
            begin: CornerFor(begin, diagonal.BeginId),
            end: CornerFor(end, diagonal.BeginId));
        _endArc = new MaterialPointArcTween(
            begin: CornerFor(begin, diagonal.EndId),
            end: CornerFor(end, diagonal.EndId));
        _dirty = false;
    }

    private static double DiagonalSupport(Rect begin, Point centersVector, Diagonal diagonal)
    {
        Point delta = CornerFor(begin, diagonal.EndId) - CornerFor(begin, diagonal.BeginId);
        double length = ArcGeometry.Distance(delta);
        return (centersVector.X * delta.X / length) + (centersVector.Y * delta.Y / length);
    }
}

/// <summary>
/// Dart's `MaterialRectCenterArcTween`: a tween that moves a rect's center along a circular arc while
/// linearly interpolating its size.
/// </summary>
public class MaterialRectCenterArcTween : RectTween
{
    private bool _dirty = true;
    private MaterialPointArcTween _centerArc = null!;

    public MaterialRectCenterArcTween(Rect? begin = null, Rect? end = null)
        : base(begin, end)
    {
    }

    /// <summary>The path of the rectangle's center.</summary>
    public MaterialPointArcTween? CenterArc
    {
        get
        {
            if (Begin is null || End is null)
            {
                return null;
            }

            if (_dirty)
            {
                Initialize();
            }

            return _centerArc;
        }
    }

    public override Rect? Begin
    {
        get => base.Begin;
        set
        {
            if (value != base.Begin)
            {
                base.Begin = value;
                _dirty = true;
            }
        }
    }

    public override Rect? End
    {
        get => base.End;
        set
        {
            if (value != base.End)
            {
                base.End = value;
                _dirty = true;
            }
        }
    }

    public override Rect Evaluate(double t)
    {
        if (_dirty)
        {
            Initialize();
        }

        Rect begin = GetBeginValue();
        Rect end = GetEndValue();
        if (t == 0.0)
        {
            return begin;
        }

        if (t == 1.0)
        {
            return end;
        }

        Point center = _centerArc.Evaluate(t);
        double width = begin.Width + ((end.Width - begin.Width) * t);
        double height = begin.Height + ((end.Height - begin.Height) * t);
        return new Rect(center.X - (width / 2.0), center.Y - (height / 2.0), width, height);
    }

    public override string ToString()
    {
        return $"MaterialRectCenterArcTween({Begin} → {End}; centerArc={CenterArc})";
    }

    private void Initialize()
    {
        _centerArc = new MaterialPointArcTween(begin: GetBeginValue().Center, end: GetEndValue().Center);
        _dirty = false;
    }
}

/// <summary>Dart's `Rect.fromPoints`, which normalizes the two corners.</summary>
internal static class ArcGeometry
{
    public static double Distance(Point delta) => Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));

    public static Rect RectFromPoints(Point a, Point b)
    {
        return new Rect(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X),
            Math.Abs(a.Y - b.Y));
    }
}
