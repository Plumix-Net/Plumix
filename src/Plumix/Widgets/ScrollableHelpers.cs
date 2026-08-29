using Avalonia;
using Plumix.Rendering;

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
            return calculator(new ScrollIncrementDetails(type, state.Metrics));
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

        if (!state.EffectivePhysics.ShouldAcceptUserOffset(state.Position))
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
public sealed class EdgeDraggingAutoScroller : IDisposable
{
    private const double MaxOverDrag = 20.0;

    private readonly Scrollable.ScrollableState _scrollable;
    private readonly Func<Rect?> _viewportRectProvider;
    private readonly Action? _onScrollViewScrolled;
    private readonly Ticker _ticker;
    private TimeSpan _lastElapsed;
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
        _ticker = _scrollable.CreateTicker(HandleTick);
    }

    public double VelocityScalar { get; }

    public bool IsAutoScrolling => _ticker.IsActive;

    /// Starts the auto scroll if `dragTarget` is close to the edge.
    ///
    /// If the scrollable's resolved physics refuses user-driven scrolling (for example
    /// [NeverScrollableScrollPhysics]), no auto scroll is started and any in-flight auto
    /// scroll is stopped.
    public void StartAutoScrollIfNecessary(Rect dragTarget)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_scrollable.EffectivePhysics.ShouldAcceptUserOffset(_scrollable.Position))
        {
            StopAutoScroll();
            return;
        }

        _dragTarget = dragTarget;
        if (ResolveScrollVelocity() == 0.0)
        {
            StopAutoScroll();
            return;
        }

        if (!_ticker.IsActive)
        {
            _lastElapsed = TimeSpan.Zero;
            _ticker.Start();
        }
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
        _ticker.Dispose();
    }

    private void HandleTick(TimeSpan elapsed)
    {
        ScrollPosition position = _scrollable.Position;

        // The ticker reports time since it started; auto-scrolling advances by the frame delta.
        double frameSeconds = (elapsed - _lastElapsed).TotalSeconds;
        _lastElapsed = elapsed;

        double velocity = ResolveScrollVelocity();
        if (velocity == 0.0)
        {
            StopAutoScroll();
            return;
        }

        // The frame that gives the ticker its start timestamp reports no elapsed time; it must not be
        // read as "the scrollable cannot move any further".
        if (frameSeconds <= 0.0)
        {
            return;
        }

        double nextOffset = Math.Clamp(
            position.Pixels + (velocity * frameSeconds),
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
