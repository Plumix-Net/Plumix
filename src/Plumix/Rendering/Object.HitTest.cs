using Avalonia;
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

public sealed class BoxHitTestResult : HitTestResult
{
}
