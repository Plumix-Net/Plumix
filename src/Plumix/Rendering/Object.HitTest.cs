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

public class HitTestEntry(RenderObject target)
{
    public RenderObject Target { get; } = target;

    public virtual PointerEvent TransformEvent(PointerEvent @event)
    {
        return @event;
    }
}

public sealed class BoxHitTestEntry(RenderBox target, Point localPosition) : HitTestEntry(target)
{
    public Point LocalPosition { get; } = localPosition;

    public override PointerEvent TransformEvent(PointerEvent @event)
    {
        return @event.WithLocalCoordinates(LocalPosition, @event.Delta);
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
