using Avalonia;
using Plumix.Gestures;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart; flutter/packages/flutter/lib/src/rendering/box.dart (approximate)

namespace Plumix.Rendering;

public enum HitTestBehavior
{
    DeferToChild,
    Opaque,
    Translucent
}

public class HitTestEntry(IHitTestTarget target)
{
    public IHitTestTarget Target { get; } = target;

    public virtual PointerEvent TransformEvent(PointerEvent @event)
    {
        return @event;
    }
}

public sealed class BoxHitTestEntry : HitTestEntry
{
    private Point _currentLocalPosition;

    public BoxHitTestEntry(RenderBox target, Point localPosition) : base(target)
    {
        LocalPosition = localPosition;
        _currentLocalPosition = localPosition;
    }

    public Point LocalPosition { get; }

    public override PointerEvent TransformEvent(PointerEvent @event)
    {
        if (@event is PointerMoveEvent or PointerUpEvent or PointerCancelEvent)
        {
            _currentLocalPosition += new Vector(@event.Delta.X, @event.Delta.Y);
        }
        else if (@event is PointerDownEvent)
        {
            _currentLocalPosition = LocalPosition;
        }

        return @event.WithLocalCoordinates(_currentLocalPosition, @event.Delta);
    }
}

public class HitTestResult
{
    private readonly List<HitTestEntry> _path = [];

    public IReadOnlyList<HitTestEntry> Path => _path;

    public void Add(HitTestEntry entry)
    {
        _path.Add(entry);
    }
}

/// <summary>Signature for the nested hit test that <c>BoxHitTestResult.AddWith*</c> runs.</summary>
public delegate bool BoxHitTest(BoxHitTestResult result, Point position);

public sealed class BoxHitTestResult : HitTestResult
{
    /// <summary>
    /// Runs <paramref name="hitTest"/> with <paramref name="position"/> mapped through the inverse of
    /// a paint transform, returning <c>false</c> without testing when the transform is not invertible.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>BoxHitTestResult.addWithPaintTransform</c>. Plumix does not carry a per-event
    /// transform on <see cref="PointerEvent"/> (see `docs/ai/DIVERGENCES.md`), so the matrix is applied
    /// to the position only and no transform stack is pushed onto the result.
    /// </remarks>
    public bool AddWithPaintTransform(Matrix4? transform, Point position, BoxHitTest hitTest)
    {
        if (transform is not null)
        {
            transform = Matrix4.TryInvert(PointerEventUtils.RemovePerspectiveTransform(transform));
            if (transform is null)
            {
                return false;
            }
        }

        return AddWithRawTransform(transform, position, hitTest);
    }

    /// <summary>Runs <paramref name="hitTest"/> with <paramref name="position"/> shifted by an offset.</summary>
    public bool AddWithPaintOffset(Point? offset, Point position, BoxHitTest hitTest)
    {
        Point transformedPosition = offset is { } value ? position - value : position;
        return hitTest(this, transformedPosition);
    }

    /// <summary>Runs <paramref name="hitTest"/> with <paramref name="position"/> mapped through a matrix.</summary>
    public bool AddWithRawTransform(Matrix4? transform, Point position, BoxHitTest hitTest)
    {
        Point transformedPosition = transform is null
            ? position
            : MatrixUtils.TransformPoint(transform, position);
        return hitTest(this, transformedPosition);
    }
}
