using System.Collections;
using Avalonia;
using Plumix.Foundation;
using SkiaSharp;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Tangent, PathMetrics, PathMetric)
// Engine adapter: engine/src/flutter/lib/ui/painting/path_measure.cc.
public sealed class Tangent(Point position, Point vector)
{
    public Point Position { get; } = position;
    public Point Vector { get; } = vector;
    public double Angle => -Math.Atan2(Vector.Y, Vector.X);
    public static Tangent FromAngle(Point position, double angle) =>
        new(position, new Point(Math.Cos(angle), Math.Sin(angle)));
}

/// <summary>A single-use iterable over a snapshot of a path's nonzero-length contours.</summary>
public sealed class PathMetrics : IEnumerable<PathMetric>
{
    private readonly PathMetricIterator _iterator;
    internal PathMetrics(SKPath path, bool forceClosed) => _iterator = new PathMetricIterator(path, forceClosed);
    public PathMetricIterator GetEnumerator() => _iterator;
    IEnumerator<PathMetric> IEnumerable<PathMetric>.GetEnumerator() => _iterator;
    IEnumerator IEnumerable.GetEnumerator() => _iterator;
}

public sealed class PathMetricIterator : IEnumerator<PathMetric>
{
    private readonly SKPath _snapshot;
    private readonly SKPathMeasure _measure;
    private readonly PathMeasure _retainedMeasure;
    private PathMetric? _current;
    private int _contourIndex = -1;
    private bool _started;
    private bool _finished;

    internal PathMetricIterator(SKPath path, bool forceClosed)
    {
        _snapshot = new SKPath(path);
        _measure = new SKPathMeasure(_snapshot, forceClosed, 1.0f);
        _retainedMeasure = new PathMeasure(_snapshot, forceClosed);
    }

    public PathMetric Current => _current ?? throw new InvalidOperationException(
        "PathMetricIterator is not pointing to a PathMetric. This can happen in two situations:\n"
        + "- The iteration has not started yet. If so, call \"moveNext\" to start iteration.\n"
        + "- The iterator ran out of elements. If so, check that \"moveNext\" returns true "
        + "prior to calling \"current\".");
    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_finished)
        {
            return false;
        }

        bool hasContour = !_started || _measure.NextContour();
        _started = true;
        while (hasContour && _measure.Length <= 0.0f)
        {
            hasContour = _measure.NextContour();
        }

        if (!hasContour)
        {
            _current = null;
            _finished = true;
            return false;
        }

        _contourIndex++;
        _current = new PathMetric(_retainedMeasure, _contourIndex, _measure.Length, _measure.IsClosed);
        return true;
    }

    public void Reset() => throw new NotSupportedException("PathMetrics does not restart iteration.");

    // Dart returns the same iterator on subsequent enumerations, including after First/Take. Its
    // native cursor must survive foreach disposal until the shared stream is exhausted or collected.
    public void Dispose() { }
}

public sealed class PathMetric
{
    private readonly PathMeasure _measure;

    internal PathMetric(PathMeasure measure, int contourIndex, double length, bool isClosed)
    {
        _measure = measure;
        Length = length;
        IsClosed = isClosed;
        ContourIndex = contourIndex;
    }

    public double Length { get; }
    public bool IsClosed { get; }
    public int ContourIndex { get; }

    public Tangent? GetTangentForOffset(double distance) =>
        _measure.At(ContourIndex).GetPositionAndTangent(
            Path.Narrow(distance), out SKPoint position, out SKPoint tangent)
            ? new Tangent(new Point(position.X, position.Y), new Point(tangent.X, tangent.Y)) : null;

    public Path ExtractPath(double start, double end, bool startWithMoveTo = true)
    {
        var result = new SKPath();
        _measure.At(ContourIndex).GetSegment(Path.Narrow(start), Path.Narrow(end), result, startWithMoveTo);
        return new Path(result);
    }

    public override string ToString()
    {
        string length = BindingBase.DartDoubleToString(Length);
        return $"PathMetric(length: {length}, isClosed: {IsClosed.ToString().ToLowerInvariant()}, "
               + $"contourIndex: {ContourIndex})";
    }
}

// SkiaSharp exposes a forward SkPathMeasure cursor rather than retained SkContourMeasures. Keep a
// second cursor for metric queries: enumeration and queries in contour order remain linear, while
// revisiting an earlier contour resets that cursor over the immutable snapshot.
internal sealed class PathMeasure(SKPath snapshot, bool forceClosed)
{
    private readonly SKPathMeasure _measure = new(snapshot, forceClosed, 1.0f);
    private readonly SKPath _snapshot = snapshot;
    private int _index;

    internal SKPathMeasure At(int index)
    {
        if (index < _index)
        {
            _measure.SetPath(_snapshot, forceClosed);
            _index = 0;
        }

        while (_index < index)
        {
            _measure.NextContour();
            _index++;
        }

        return _measure;
    }
}
