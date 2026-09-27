using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/animation/tween_sequence.dart

namespace Plumix;

/// <summary>
/// Enables creating an <see cref="Animation{T}"/> whose value is defined by a sequence of
/// <see cref="Tween{T}"/>s.
/// </summary>
public class TweenSequence<T> : Animatable<T>
{
    private readonly List<TweenSequenceItem<T>> _items = [];
    private readonly List<TweenSequenceInterval> _intervals = [];

    /// <summary>Construct a TweenSequence. The <paramref name="items"/> list must not be empty.</summary>
    public TweenSequence(IReadOnlyList<TweenSequenceItem<T>> items)
    {
        DebugAssertions.Assert(items.Count > 0);
        _items.AddRange(items);

        double totalWeight = 0.0;
        foreach (TweenSequenceItem<T> item in _items)
        {
            totalWeight += item.Weight;
        }

        DebugAssertions.Assert(totalWeight > 0.0);

        double start = 0.0;
        for (int i = 0; i < _items.Count; i += 1)
        {
            double end = i == _items.Count - 1 ? 1.0 : start + (_items[i].Weight / totalWeight);
            _intervals.Add(new TweenSequenceInterval(start, end));
            start = end;
        }
    }

    private T EvaluateAt(double t, int index)
    {
        TweenSequenceItem<T> element = _items[index];
        double tInterval = _intervals[index].Value(t);
        return element.Tween.Transform(tInterval);
    }

    public override T Transform(double t)
    {
        DebugAssertions.Assert(t >= 0.0 && t <= 1.0);
        if (t == 1.0)
        {
            return EvaluateAt(t, _items.Count - 1);
        }

        for (int index = 0; index < _items.Count; index++)
        {
            if (_intervals[index].Contains(t))
            {
                return EvaluateAt(t, index);
            }
        }

        // Should be unreachable.
        throw new InvalidOperationException($"TweenSequence.evaluate() could not find an interval for {t}");
    }

    public override string ToString() => $"TweenSequence({_items.Count} items)";
}

/// <summary>
/// Enables creating a flipped <see cref="Animation{T}"/> whose value is defined by a sequence of
/// <see cref="Tween{T}"/>s.
/// </summary>
public class FlippedTweenSequence : TweenSequence<double>
{
    public FlippedTweenSequence(IReadOnlyList<TweenSequenceItem<double>> items)
        : base(items)
    {
    }

    public override double Transform(double t) => 1 - base.Transform(1 - t);
}

/// <summary>A simple holder for one element of a <see cref="TweenSequence{T}"/>.</summary>
public class TweenSequenceItem<T>
{
    public TweenSequenceItem(Animatable<T> tween, double weight)
    {
        DebugAssertions.Assert(weight > 0.0);
        Tween = tween;
        Weight = weight;
    }

    /// <summary>Defines the value of the <see cref="TweenSequence{T}"/> for the interval of this item.</summary>
    public Animatable<T> Tween { get; }

    /// <summary>An arbitrary value that indicates the relative percentage of the sequence this item covers.</summary>
    public double Weight { get; }
}

/// <summary>Dart's private <c>_Interval</c>.</summary>
internal readonly struct TweenSequenceInterval
{
    public TweenSequenceInterval(double start, double end)
    {
        DebugAssertions.Assert(end > start);
        Start = start;
        End = end;
    }

    public double Start { get; }

    public double End { get; }

    public bool Contains(double t) => t >= Start && t < End;

    public double Value(double t) => (t - Start) / (End - Start);

    public override string ToString() => $"<{Start}, {End}>";
}
