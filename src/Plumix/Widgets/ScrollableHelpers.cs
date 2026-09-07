using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollable_helpers.dart

/// <summary>
/// Describes the type of scroll increment being requested of a <see cref="ScrollIncrementCalculator"/>.
/// </summary>
public enum ScrollIncrementType
{
    /// The distance to move when the user requests to scroll by a "line".
    Line,

    /// The distance to move when the user requests to scroll by a "page".
    Page,
}

/// <summary>
/// A details object that describes the type of scroll increment being requested, plus the current
/// metrics of the scrollable being scrolled.
/// </summary>
public sealed class ScrollIncrementDetails
{
    public ScrollIncrementDetails(ScrollIncrementType type, IScrollMetrics metrics)
    {
        Type = type;
        Metrics = metrics;
    }

    /// The type of scroll this is (e.g. line, page).
    public ScrollIncrementType Type { get; }

    /// The current metrics of the scrollable that is being scrolled.
    public IScrollMetrics Metrics { get; }
}

/// <summary>Computes the scroll distance for one keyboard-driven scroll request.</summary>
public delegate double ScrollIncrementCalculator(ScrollIncrementDetails details);

/// <summary>
/// An [Intent] that represents scrolling the nearest scrollable by an amount appropriate for the
/// [Type] specified.
/// </summary>
public sealed class ScrollIntent : Intent
{
    public ScrollIntent(AxisDirection direction, ScrollIncrementType type = ScrollIncrementType.Line)
    {
        Direction = direction;
        Type = type;
    }

    /// The direction in which to scroll the scrollable containing the focused widget.
    public AxisDirection Direction { get; }

    /// The type of scrolling that is intended.
    public ScrollIncrementType Type { get; }
}

/// <summary>
/// An action that scrolls the relevant [Scrollable] by the amount configured in the
/// <see cref="ScrollIntent"/> given to it.
/// </summary>
public sealed class ScrollAction : ContextAction<ScrollIntent>
{
    /// The duration of the animation a keyboard-driven scroll runs.
    private static readonly TimeSpan ScrollDuration = TimeSpan.FromMilliseconds(100);

    public override bool IsEnabled(ScrollIntent intent, BuildContext? context)
    {
        if (context is not { } buildContext)
        {
            return false;
        }

        if (Scrollable.MaybeOf(buildContext) is not null)
        {
            return true;
        }

        ScrollController? primaryScrollController = PrimaryScrollController.MaybeOf(buildContext);
        return primaryScrollController is not null && primaryScrollController.HasClients;
    }

    /// <summary>
    /// The scroll increment for a single scroll request, taking the scrollable's own calculator into
    /// account. Defaults are 80% of the viewport for a page and 50 logical pixels for a line.
    /// </summary>
    public static double CalculateScrollIncrement(
        Scrollable.ScrollableState state,
        ScrollIncrementType type = ScrollIncrementType.Line)
    {
        if (state.IncrementCalculator is { } calculator)
        {
            return calculator(new ScrollIncrementDetails(type, state.Position.CopyWith()));
        }

        return type switch
        {
            ScrollIncrementType.Line => 50.0,
            _ => 0.8 * state.Position.ViewportDimension,
        };
    }

    /// <summary>The signed increment for the intent, accounting for the scrollable's axis.</summary>
    public static double GetDirectionalIncrement(Scrollable.ScrollableState state, ScrollIntent intent)
    {
        if (ScrollDirectionUtils.AxisDirectionToAxis(intent.Direction)
            != ScrollDirectionUtils.AxisDirectionToAxis(state.AxisDirection))
        {
            return 0.0;
        }

        double increment = CalculateScrollIncrement(state, intent.Type);
        return intent.Direction == state.AxisDirection ? increment : -increment;
    }

    public override object? Invoke(ScrollIntent intent, BuildContext? context)
    {
        if (context is not { } buildContext)
        {
            return null;
        }

        Scrollable.ScrollableState? state = Scrollable.MaybeOf(buildContext);
        if (state is null)
        {
            ScrollController? primary = PrimaryScrollController.MaybeOf(buildContext);
            if (primary is null || !primary.HasClients)
            {
                return null;
            }

            state = primary.Position.Context.NotificationContext is { } notificationContext
                ? Scrollable.MaybeOf(notificationContext)
                : null;
            if (state is null)
            {
                return null;
            }
        }

        if (state.ResolvedPhysics?.ShouldAcceptUserOffset(state.Position) != true)
        {
            return null;
        }

        double increment = GetDirectionalIncrement(state, intent);
        if (increment == 0.0)
        {
            return null;
        }

        state.Position.MoveTo(state.Position.Pixels + increment, ScrollDuration, Curves.EaseInOut);
        return null;
    }
}

/// <summary>Continuously scrolls a viewport while a dragged rectangle extends beyond an edge.</summary>
/// <remarks>
/// Dart drives the scroll from an <c>async</c> loop that awaits a 20-pixel
/// <c>position.animateTo</c> hop per iteration; Plumix drives the same geometry from a
/// <see cref="Ticker"/> at the equivalent velocity (see <c>docs/ai/DIVERGENCES.md</c>). The drag
/// target is anchored to the scroll origin exactly as Dart anchors it, so an auto scroll that is
/// no longer being fed a fresh target stops once the content has scrolled far enough to bring the
/// anchored target back inside the viewport.
/// </remarks>
public sealed class EdgeDraggingAutoScroller : IDisposable
{
    private const double OverDragMax = 20.0;

    private readonly Scrollable.ScrollableState _scrollable;
    private readonly Action? _onScrollViewScrolled;
    private readonly Ticker _ticker;
    private Rect _dragTargetRelatedToScrollOrigin;
    private bool _disposed;

    // Dart's `_scroll` loop advances in discrete hops: each iteration picks an offset at most
    // `OverDragMax` away and animates to it linearly over `1000 / velocityScalar` milliseconds,
    // then re-resolves. The ticker reproduces one hop at a time so the scroll lands exactly on the
    // resolved offset (and therefore exactly on a scroll extent) instead of approaching it.
    private bool _hopInFlight;
    private double _hopStartPixels;
    private double _hopTargetPixels;
    private TimeSpan _hopStartElapsed;

    public EdgeDraggingAutoScroller(
        Scrollable.ScrollableState scrollable,
        double velocityScalar,
        Action? onScrollViewScrolled = null)
    {
        _scrollable = scrollable ?? throw new ArgumentNullException(nameof(scrollable));
        if (!double.IsFinite(velocityScalar) || velocityScalar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(velocityScalar));
        }

        VelocityScalar = velocityScalar;
        _onScrollViewScrolled = onScrollViewScrolled;
        _ticker = _scrollable.CreateTicker(HandleTick);
    }

    /// <summary>The [Scrollable] this auto scroller drives.</summary>
    public Scrollable.ScrollableState Scrollable => _scrollable;

    /// <summary>The velocity scalar per pixel over scroll, in logical pixels per second.</summary>
    public double VelocityScalar { get; }

    /// <summary>Whether the auto scroll is in progress.</summary>
    public bool IsAutoScrolling => _ticker.IsActive;

    /// Starts the auto scroll if `dragTarget` is close to the edge.
    ///
    /// The `dragTarget` is given in global coordinates and stored relative to the scroll origin,
    /// so it stays anchored to the content while the viewport moves under it.
    ///
    /// If the scrollable's resolved physics refuses user-driven scrolling (for example
    /// [NeverScrollableScrollPhysics]), no auto scroll is started and any in-flight auto
    /// scroll is stopped.
    public void StartAutoScrollIfNecessary(Rect dragTarget)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ScrollPhysics? physics = _scrollable.ResolvedPhysics;
        if (physics is not null && !physics.ShouldAcceptUserOffset(_scrollable.Position))
        {
            StopAutoScroll();
            return;
        }

        Point deltaToOrigin = _scrollable.DeltaToScrollOrigin;
        _dragTargetRelatedToScrollOrigin =
            dragTarget.Translate(new Vector(deltaToOrigin.X, deltaToOrigin.Y));
        if (!TryResolveTargetOffset(out double _))
        {
            StopAutoScroll();
            return;
        }

        // A hop already in flight keeps its target, exactly as Dart's in-flight `animateTo` does;
        // the fresh drag target is picked up by the next iteration.
        if (!_ticker.IsActive)
        {
            _hopInFlight = false;
            _ticker.Start();
        }
    }

    public void StopAutoScroll()
    {
        _hopInFlight = false;
        _ticker.Stop();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ticker.Dispose();
    }

    private void HandleTick(TimeSpan elapsed)
    {
        if (!_hopInFlight)
        {
            if (!TryResolveTargetOffset(out double resolved))
            {
                StopAutoScroll();
                return;
            }

            _hopStartPixels = _scrollable.Position.Pixels;
            _hopTargetPixels = resolved;
            _hopStartElapsed = elapsed;
            _hopInFlight = true;
        }

        double hopMilliseconds = 1000.0 / VelocityScalar;
        double t = Math.Clamp((elapsed - _hopStartElapsed).TotalMilliseconds / hopMilliseconds, 0.0, 1.0);
        _scrollable.Position.JumpTo(_hopStartPixels + ((_hopTargetPixels - _hopStartPixels) * t));
        if (t < 1.0)
        {
            return;
        }

        _hopInFlight = false;
        _onScrollViewScrolled?.Invoke();
    }

    /// Dart's `_scroll` body: the offset the scrollable should move toward, or none when the
    /// anchored drag target no longer sits outside the viewport.
    private bool TryResolveTargetOffset(out double newOffset)
    {
        newOffset = 0.0;
        if (_scrollable.Context.FindRenderObject() is not RenderBox scrollRenderBox
            || !scrollRenderBox.HasSize)
        {
            return false;
        }

        Matrix4 transform = scrollRenderBox.GetTransformTo(null);
        Rect globalRect = MatrixUtils.TransformRect(
            transform,
            new Rect(0, 0, scrollRenderBox.Size.Width, scrollRenderBox.Size.Height));
        Debug.Assert(
            globalRect.Size.Width + Constants.PrecisionErrorTolerance >= _dragTargetRelatedToScrollOrigin.Width
            && globalRect.Size.Height + Constants.PrecisionErrorTolerance >= _dragTargetRelatedToScrollOrigin.Height,
            "Drag target size is larger than scrollable size, which may cause bouncing");

        ScrollPosition position = _scrollable.Position;
        AxisDirection direction = _scrollable.AxisDirection;
        Axis scrollDirection = ScrollDirectionUtils.AxisDirectionToAxis(direction);
        Point deltaToOrigin = _scrollable.DeltaToScrollOrigin;
        Point viewportOrigin = globalRect.TopLeft + new Vector(deltaToOrigin.X, deltaToOrigin.Y);
        double viewportStart = OffsetExtent(viewportOrigin, scrollDirection);
        double viewportEnd = viewportStart + SizeExtent(globalRect.Size, scrollDirection);

        double proxyStart = OffsetExtent(_dragTargetRelatedToScrollOrigin.TopLeft, scrollDirection);
        double proxyEnd = OffsetExtent(_dragTargetRelatedToScrollOrigin.BottomRight, scrollDirection);
        bool resolved = false;
        switch (direction)
        {
            case AxisDirection.Up:
            case AxisDirection.Left:
                if (proxyEnd > viewportEnd && position.Pixels > position.MinScrollExtent)
                {
                    double overDrag = Math.Min(proxyEnd - viewportEnd, OverDragMax);
                    newOffset = Math.Max(position.MinScrollExtent, position.Pixels - overDrag);
                    resolved = true;
                }
                else if (proxyStart < viewportStart && position.Pixels < position.MaxScrollExtent)
                {
                    double overDrag = Math.Min(viewportStart - proxyStart, OverDragMax);
                    newOffset = Math.Min(position.MaxScrollExtent, position.Pixels + overDrag);
                    resolved = true;
                }

                break;
            case AxisDirection.Right:
            case AxisDirection.Down:
                if (proxyStart < viewportStart && position.Pixels > position.MinScrollExtent)
                {
                    double overDrag = Math.Min(viewportStart - proxyStart, OverDragMax);
                    newOffset = Math.Max(position.MinScrollExtent, position.Pixels - overDrag);
                    resolved = true;
                }
                else if (proxyEnd > viewportEnd && position.Pixels < position.MaxScrollExtent)
                {
                    double overDrag = Math.Min(proxyEnd - viewportEnd, OverDragMax);
                    newOffset = Math.Min(position.MaxScrollExtent, position.Pixels + overDrag);
                    resolved = true;
                }

                break;
        }

        if (!resolved || Math.Abs(newOffset - position.Pixels) < 1.0)
        {
            newOffset = 0.0;
            return false;
        }

        return true;
    }

    private static double OffsetExtent(Point offset, Axis scrollDirection) =>
        scrollDirection == Axis.Horizontal ? offset.X : offset.Y;

    private static double SizeExtent(Size size, Axis scrollDirection) =>
        scrollDirection == Axis.Horizontal ? size.Width : size.Height;
}
