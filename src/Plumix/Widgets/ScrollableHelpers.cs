using Avalonia;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollable_helpers.dart

/// <summary>Continuously scrolls a viewport while a dragged rectangle extends beyond an edge.</summary>
public sealed class EdgeDraggingAutoScroller : IDisposable
{
    private const double MaxOverDrag = 20.0;

    private readonly Scrollable.ScrollableState _scrollable;
    private readonly Func<Rect?> _viewportRectProvider;
    private readonly Action? _onScrollViewScrolled;
    private readonly Ticker _ticker;
    private Rect _dragTarget;
    private bool _disposed;

    public EdgeDraggingAutoScroller(
        Scrollable.ScrollableState scrollable,
        Func<Rect?> viewportRectProvider,
        double velocityScalar = 50.0,
        Action? onScrollViewScrolled = null)
    {
        _scrollable = scrollable ?? throw new ArgumentNullException(nameof(scrollable));
        _viewportRectProvider = viewportRectProvider
                                ?? throw new ArgumentNullException(nameof(viewportRectProvider));
        if (!double.IsFinite(velocityScalar) || velocityScalar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(velocityScalar));
        }

        VelocityScalar = velocityScalar;
        _onScrollViewScrolled = onScrollViewScrolled;
        _ticker = new Ticker(HandleTick);
    }

    public double VelocityScalar { get; }

    public bool IsAutoScrolling => _ticker.Active;

    public void StartAutoScrollIfNecessary(Rect dragTarget)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dragTarget = dragTarget;
        if (ResolveScrollVelocity() == 0.0)
        {
            StopAutoScroll();
            return;
        }

        _ticker.Start();
    }

    public void StopAutoScroll()
    {
        _ticker.Stop();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ticker.Stop();
    }

    private void HandleTick(TimeSpan elapsed)
    {
        ScrollPosition position = _scrollable.Position;

        double velocity = ResolveScrollVelocity();
        if (velocity == 0.0)
        {
            StopAutoScroll();
            return;
        }

        double nextOffset = Math.Clamp(
            position.Pixels + (velocity * elapsed.TotalSeconds),
            position.MinScrollExtent,
            position.MaxScrollExtent);
        if (Math.Abs(nextOffset - position.Pixels) < 0.0001)
        {
            StopAutoScroll();
            return;
        }

        position.JumpTo(nextOffset);
        _onScrollViewScrolled?.Invoke();
    }

    private double ResolveScrollVelocity()
    {
        Rect? viewportValue = _viewportRectProvider();
        if (!viewportValue.HasValue)
        {
            return 0.0;
        }

        ScrollPosition position = _scrollable.Position;
        Rect viewport = viewportValue.Value;
        AxisDirection direction = _scrollable.AxisDirection;
        bool vertical = direction is AxisDirection.Up or AxisDirection.Down;
        double viewportStart = vertical ? viewport.Top : viewport.Left;
        double viewportEnd = vertical ? viewport.Bottom : viewport.Right;
        double targetStart = vertical ? _dragTarget.Top : _dragTarget.Left;
        double targetEnd = vertical ? _dragTarget.Bottom : _dragTarget.Right;
        double leadingOverDrag = Math.Min(Math.Max(viewportStart - targetStart, 0.0), MaxOverDrag);
        double trailingOverDrag = Math.Min(Math.Max(targetEnd - viewportEnd, 0.0), MaxOverDrag);
        double overDrag = leadingOverDrag > 0.0 ? -leadingOverDrag : trailingOverDrag;
        if (Math.Abs(overDrag) < 1.0)
        {
            return 0.0;
        }

        if (direction is AxisDirection.Up or AxisDirection.Left)
        {
            overDrag = -overDrag;
        }

        double target = Math.Clamp(
            position.Pixels + overDrag,
            position.MinScrollExtent,
            position.MaxScrollExtent);
        return Math.Abs(target - position.Pixels) < 1.0 ? 0.0 : overDrag * VelocityScalar;
    }
}
