using Avalonia;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/predictive_back_event.dart

public enum SwipeEdge
{
    Left,
    Right,
}

public sealed record PredictiveBackEvent
{
    public PredictiveBackEvent(
        double progress,
        SwipeEdge swipeEdge,
        Point? touchOffset = null)
    {
        if (!double.IsFinite(progress) || progress is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(progress));
        }

        Progress = progress;
        SwipeEdge = swipeEdge;
        TouchOffset = touchOffset;
    }

    public Point? TouchOffset { get; }

    public double Progress { get; }

    public SwipeEdge SwipeEdge { get; }

    public bool IsButtonEvent => TouchOffset is null
                                 || (Progress == 0.0 && TouchOffset.Value == new Point(0.0, 0.0));

    public static PredictiveBackEvent FromMap(IReadOnlyDictionary<string, object?> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        double progress = Convert.ToDouble(map["progress"]);
        int edge = Convert.ToInt32(map["swipeEdge"]);
        Point? touchOffset = null;
        if (map.TryGetValue("touchOffset", out object? rawTouchOffset)
            && rawTouchOffset is IReadOnlyList<object?> values
            && values.Count >= 2)
        {
            touchOffset = new Point(Convert.ToDouble(values[0]), Convert.ToDouble(values[1]));
        }

        return new PredictiveBackEvent(
            progress,
            edge == 1 ? SwipeEdge.Right : SwipeEdge.Left,
            touchOffset);
    }
}
