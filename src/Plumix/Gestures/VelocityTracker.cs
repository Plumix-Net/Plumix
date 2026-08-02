using System.Diagnostics;
using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/velocity_tracker.dart

namespace Plumix.Gestures;

public delegate VelocityTracker GestureVelocityTrackerBuilder(PointerEvent @event);

public sealed record VelocityEstimate(
    Vector PixelsPerSecond,
    double Confidence,
    TimeSpan Duration,
    Vector Offset);

public class VelocityTracker
{
    private const int AssumePointerMoveStoppedMilliseconds = 40;
    private const int HistorySize = 20;
    private const int HorizonMilliseconds = 100;
    private const int MinimumSampleSize = 3;
    private readonly PointAtTime?[] _samples = new PointAtTime?[HistorySize];
    private readonly Stopwatch _sinceLastSample = new();
    private int _index;

    public VelocityTracker(PointerDeviceKind kind)
    {
        Kind = kind;
    }

    public PointerDeviceKind Kind { get; }

    public virtual void AddPosition(DateTime timestampUtc, Point position)
    {
        RestartSampleClock();
        _index = (_index + 1) % HistorySize;
        _samples[_index] = new PointAtTime(position, timestampUtc);
    }

    public virtual VelocityEstimate? GetVelocityEstimate()
    {
        if (_sinceLastSample.ElapsedMilliseconds > AssumePointerMoveStoppedMilliseconds)
        {
            return StoppedEstimate();
        }

        PointAtTime? newestSample = _samples[_index];
        if (newestSample == null)
        {
            return null;
        }

        var x = new List<double>();
        var y = new List<double>();
        var weights = new List<double>();
        var time = new List<double>();
        int sampleCount = 0;
        int index = _index;
        PointAtTime previousSample = newestSample.Value;
        PointAtTime oldestSample = newestSample.Value;
        do
        {
            PointAtTime? sample = _samples[index];
            if (sample == null)
            {
                break;
            }

            double age = (newestSample.Value.TimestampUtc - sample.Value.TimestampUtc).TotalMilliseconds;
            double delta = Math.Abs((sample.Value.TimestampUtc - previousSample.TimestampUtc).TotalMilliseconds);
            previousSample = sample.Value;
            if (age > HorizonMilliseconds || delta > AssumePointerMoveStoppedMilliseconds)
            {
                break;
            }

            oldestSample = sample.Value;
            x.Add(sample.Value.Position.X);
            y.Add(sample.Value.Position.Y);
            weights.Add(1.0);
            time.Add(-age);
            index = index == 0 ? HistorySize - 1 : index - 1;
            sampleCount++;
        }
        while (sampleCount < HistorySize);

        if (sampleCount >= MinimumSampleSize)
        {
            PolynomialFit? xFit = new LeastSquaresSolver(time, x, weights).Solve(2);
            PolynomialFit? yFit = new LeastSquaresSolver(time, y, weights).Solve(2);
            if (xFit != null && yFit != null)
            {
                return new VelocityEstimate(
                    new Vector(xFit.Coefficients[1] * 1000.0, yFit.Coefficients[1] * 1000.0),
                    xFit.Confidence * yFit.Confidence,
                    newestSample.Value.TimestampUtc - oldestSample.TimestampUtc,
                    newestSample.Value.Position - oldestSample.Position);
            }
        }

        return new VelocityEstimate(
            default,
            1.0,
            newestSample.Value.TimestampUtc - oldestSample.TimestampUtc,
            newestSample.Value.Position - oldestSample.Position);
    }

    public Velocity GetVelocity()
    {
        VelocityEstimate? estimate = GetVelocityEstimate();
        return estimate == null || estimate.PixelsPerSecond == default
            ? Velocity.Zero
            : new Velocity(estimate.PixelsPerSecond);
    }

    protected bool HasStopped => _sinceLastSample.ElapsedMilliseconds > AssumePointerMoveStoppedMilliseconds;

    protected void RestartSampleClock() => _sinceLastSample.Restart();

    protected static VelocityEstimate StoppedEstimate()
    {
        return new VelocityEstimate(default, 1.0, TimeSpan.Zero, default);
    }

    protected readonly record struct PointAtTime(Point Position, DateTime TimestampUtc);
}

public class IOSScrollViewFlingVelocityTracker : VelocityTracker
{
    private const int SampleSize = 20;
    private readonly PointAtTime?[] _touchSamples = new PointAtTime?[SampleSize];
    private int _index;

    public IOSScrollViewFlingVelocityTracker(PointerDeviceKind kind) : base(kind)
    {
    }

    public override void AddPosition(DateTime timestampUtc, Point position)
    {
        PointAtTime? previousPoint = _touchSamples[_index];
        if (previousPoint != null && previousPoint.Value.TimestampUtc > timestampUtc)
        {
            throw new ArgumentException(
                $"The position being added ({position}) has a smaller timestamp ({timestampUtc:O}) "
                + $"than its predecessor: {previousPoint.Value}.",
                nameof(timestampUtc));
        }

        RestartSampleClock();
        _index = (_index + 1) % SampleSize;
        _touchSamples[_index] = new PointAtTime(position, timestampUtc);
    }

    public override VelocityEstimate GetVelocityEstimate()
    {
        if (HasStopped)
        {
            return StoppedEstimate();
        }

        return CreateEstimate(
            (PreviousVelocityAt(-2) * 0.6)
            + (PreviousVelocityAt(-1) * 0.35)
            + (PreviousVelocityAt(0) * 0.05));
    }

    protected Vector PreviousVelocityAt(int offset)
    {
        int endIndex = PositiveModulo(_index + offset, SampleSize);
        int startIndex = PositiveModulo(_index + offset - 1, SampleSize);
        PointAtTime? end = _touchSamples[endIndex];
        PointAtTime? start = _touchSamples[startIndex];
        if (end == null || start == null)
        {
            return default;
        }

        double seconds = (end.Value.TimestampUtc - start.Value.TimestampUtc).TotalSeconds;
        return seconds > 0.0
            ? (end.Value.Position - start.Value.Position) / seconds
            : default;
    }

    protected VelocityEstimate CreateEstimate(Vector estimatedVelocity)
    {
        PointAtTime? newestSample = _touchSamples[_index];
        PointAtTime? oldestSample = null;
        for (int sample = 1; sample <= SampleSize; sample++)
        {
            oldestSample = _touchSamples[(_index + sample) % SampleSize];
            if (oldestSample != null)
            {
                break;
            }
        }

        if (oldestSample == null || newestSample == null)
        {
            throw new InvalidOperationException("There must be at least 1 point in the velocity samples.");
        }

        return new VelocityEstimate(
            estimatedVelocity,
            1.0,
            newestSample.Value.TimestampUtc - oldestSample.Value.TimestampUtc,
            newestSample.Value.Position - oldestSample.Value.Position);
    }

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}

public sealed class MacOSScrollViewFlingVelocityTracker : IOSScrollViewFlingVelocityTracker
{
    public MacOSScrollViewFlingVelocityTracker(PointerDeviceKind kind) : base(kind)
    {
    }

    public override VelocityEstimate GetVelocityEstimate()
    {
        if (HasStopped)
        {
            return StoppedEstimate();
        }

        return CreateEstimate(
            (PreviousVelocityAt(-2) * 0.15)
            + (PreviousVelocityAt(-1) * 0.65)
            + (PreviousVelocityAt(0) * 0.2));
    }
}
