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
public class MaterialPointArcTween : Tween<Offset>
{
    private bool _dirty = true;
    private Offset? _center;
    private double? _radius;
    private double? _beginAngle;
    private double? _endAngle;

    public MaterialPointArcTween(Offset? begin = null, Offset? end = null)
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
    public Offset? Center
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

    public new virtual Offset? Begin
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

    public new virtual Offset? End
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

    public override Offset Lerp(double t)
    {
        if (_dirty)
        {
            Initialize();
        }

        Offset begin = GetBeginValue();
        Offset end = GetEndValue();
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
            return new Offset(
                (begin.Dx * (1.0 - t)) + (end.Dx * t),
                (begin.Dy * (1.0 - t)) + (end.Dy * t));
        }

        double angle = (_beginAngle.Value * (1.0 - t)) + (_endAngle.Value * t);
        double x = Math.Cos(angle) * _radius!.Value;
        double y = Math.Sin(angle) * _radius!.Value;
        return _center!.Value + new Offset(x, y);
    }

    public override string ToString()
    {
        return $"MaterialPointArcTween({Begin} → {End}; center={Center}, radius={Radius}, "
            + $"beginAngle={BeginAngle}, endAngle={EndAngle})";
    }

    private void Initialize()
    {
        Offset begin = GetBeginValue();
        Offset end = GetEndValue();
        Offset delta = end - begin;
        double deltaX = Math.Abs(delta.Dx);
        double deltaY = Math.Abs(delta.Dy);
        double distanceFromAtoB = ArcGeometry.Distance(delta);
        var c = new Offset(end.Dx, begin.Dy);

        double SweepAngle() => 2.0 * Math.Asin(distanceFromAtoB / (2.0 * _radius!.Value));

        if (deltaX > ArcConstants.OnAxisDelta && deltaY > ArcConstants.OnAxisDelta)
        {
            if (deltaX < deltaY)
            {
                _radius = distanceFromAtoB * distanceFromAtoB / ArcGeometry.Distance(c - begin) / 2.0;
                _center = new Offset(end.Dx + (_radius.Value * ArcMath.Sign(begin.Dx - end.Dx)), end.Dy);
                if (begin.Dx < end.Dx)
                {
                    _beginAngle = SweepAngle() * ArcMath.Sign(begin.Dy - end.Dy);
                    _endAngle = 0.0;
                }
                else
                {
                    _beginAngle = Math.PI + (SweepAngle() * ArcMath.Sign(end.Dy - begin.Dy));
                    _endAngle = Math.PI;
                }
            }
            else
            {
                _radius = distanceFromAtoB * distanceFromAtoB / ArcGeometry.Distance(c - end) / 2.0;
                _center = new Offset(begin.Dx, begin.Dy + (ArcMath.Sign(end.Dy - begin.Dy) * _radius.Value));
                if (begin.Dy < end.Dy)
                {
                    _beginAngle = -Math.PI / 2.0;
                    _endAngle = _beginAngle.Value + (SweepAngle() * ArcMath.Sign(end.Dx - begin.Dx));
                }
                else
                {
                    _beginAngle = Math.PI / 2.0;
                    _endAngle = _beginAngle.Value + (SweepAngle() * ArcMath.Sign(begin.Dx - end.Dx));
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

    public override Rect? Lerp(double t)
    {
        if (_dirty)
        {
            Initialize();
        }

        Rect begin = Begin!.Value;
        Rect end = End!.Value;
        if (t == 0.0)
        {
            return begin;
        }

        if (t == 1.0)
        {
            return end;
        }

        return ArcGeometry.RectFromPoints(_beginArc.Lerp(t), _endArc.Lerp(t));
    }

    public override string ToString()
    {
        return $"MaterialRectArcTween({Begin} → {End}; beginArc={BeginArc}, endArc={EndArc})";
    }

    private static Offset CornerFor(Rect rect, CornerId id)
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
        Rect begin = Begin!.Value;
        Rect end = End!.Value;
        Offset centersVector = end.Center - begin.Center;
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

    private static double DiagonalSupport(Rect begin, Offset centersVector, Diagonal diagonal)
    {
        Offset delta = CornerFor(begin, diagonal.EndId) - CornerFor(begin, diagonal.BeginId);
        double length = ArcGeometry.Distance(delta);
        return (centersVector.Dx * delta.Dx / length) + (centersVector.Dy * delta.Dy / length);
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

    public override Rect? Lerp(double t)
    {
        if (_dirty)
        {
            Initialize();
        }

        Rect begin = Begin!.Value;
        Rect end = End!.Value;
        if (t == 0.0)
        {
            return begin;
        }

        if (t == 1.0)
        {
            return end;
        }

        Offset center = _centerArc.Lerp(t);
        double width = (begin.Width * (1.0 - t)) + (end.Width * t);
        double height = (begin.Height * (1.0 - t)) + (end.Height * t);
        return Rect.FromLTWH(center.Dx - (width / 2.0), center.Dy - (height / 2.0), width, height);
    }

    public override string ToString()
    {
        return $"MaterialRectCenterArcTween({Begin} → {End}; centerArc={CenterArc})";
    }

    private void Initialize()
    {
        _centerArc = new MaterialPointArcTween(begin: Begin!.Value.Center, end: End!.Value.Center);
        _dirty = false;
    }
}

/// <summary>Dart's `Rect.fromPoints`, which normalizes the two corners.</summary>
internal static class ArcGeometry
{
    public static double Distance(Offset delta) => Math.Sqrt((delta.Dx * delta.Dx) + (delta.Dy * delta.Dy));

    public static Rect RectFromPoints(Offset a, Offset b)
    {
        return Rect.FromLTWH(
            Math.Min(a.Dx, b.Dx),
            Math.Min(a.Dy, b.Dy),
            Math.Abs(a.Dx - b.Dx),
            Math.Abs(a.Dy - b.Dy));
    }
}
