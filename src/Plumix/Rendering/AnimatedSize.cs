using Avalonia;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/animated_size.dart

internal enum RenderAnimatedSizeState
{
    Start,
    Stable,
    Changed,
    Unstable,
}

internal sealed class RenderAnimatedSize : RenderProxyBox
{
    private AnimationController _controller;
    private TimeSpan _duration;
    private TimeSpan? _reverseDuration;
    private Alignment _alignment;
    private Clip _clipBehavior;
    private RenderAnimatedSizeState _state = RenderAnimatedSizeState.Start;
    private Size _beginSize;
    private Size _endSize;
    private bool _hasVisualOverflow;

    public RenderAnimatedSize(
        AnimationController controller,
        TimeSpan duration,
        TimeSpan? reverseDuration,
        Alignment alignment,
        Clip clipBehavior)
    {
        _controller = controller;
        _duration = duration;
        _reverseDuration = reverseDuration;
        _alignment = alignment;
        _clipBehavior = clipBehavior;
    }

    public AnimationController Controller
    {
        get => _controller;
        set
        {
            if (ReferenceEquals(_controller, value))
            {
                return;
            }

            if (Attached)
            {
                _controller.Changed -= HandleControllerChanged;
                value.Changed += HandleControllerChanged;
            }

            _controller = value;
            MarkNeedsLayout();
        }
    }

    public TimeSpan Duration
    {
        get => _duration;
        set => _duration = value;
    }

    public TimeSpan? ReverseDuration
    {
        get => _reverseDuration;
        set => _reverseDuration = value;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    internal RenderAnimatedSizeState State => _state;

    protected override void OnAttach()
    {
        base.OnAttach();
        _controller.Changed += HandleControllerChanged;
        if (_state is RenderAnimatedSizeState.Changed or RenderAnimatedSizeState.Unstable)
        {
            MarkNeedsLayout();
        }
    }

    protected override void OnDetach()
    {
        _controller.Changed -= HandleControllerChanged;
        _controller.Stop();
        base.OnDetach();
    }

    protected override void PerformLayout()
    {
        _hasVisualOverflow = false;
        if (Child is null || Constraints.IsTight)
        {
            _controller.Stop();
            Size = Constraints.Smallest;
            _beginSize = Size;
            _endSize = Size;
            _state = RenderAnimatedSizeState.Start;
            if (Child is not null)
            {
                Child.Layout(Constraints);
                ((BoxParentData)Child.parentData!).offset = default;
            }
            return;
        }

        Child.Layout(Constraints, parentUsesSize: true);
        switch (_state)
        {
            case RenderAnimatedSizeState.Start:
                _beginSize = Child.Size;
                _endSize = Child.Size;
                _state = RenderAnimatedSizeState.Stable;
                break;
            case RenderAnimatedSizeState.Stable:
                LayoutStable();
                break;
            case RenderAnimatedSizeState.Changed:
                LayoutChanged();
                break;
            case RenderAnimatedSizeState.Unstable:
                LayoutUnstable();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Size = Constraints.Constrain(EvaluateSize());
        ((BoxParentData)Child.parentData!).offset = Alignment.AlongOffset(Size, Child.Size);
        _hasVisualOverflow = Size.Width < _endSize.Width || Size.Height < _endSize.Height;
    }

    private void LayoutStable()
    {
        if (_endSize != Child!.Size)
        {
            _beginSize = Size;
            Size targetSize = Child.Size;
            bool shrinking = targetSize.Width < _endSize.Width || targetSize.Height < _endSize.Height;
            _endSize = targetSize;
            _controller.Duration = shrinking ? ReverseDuration ?? Duration : Duration;
            RestartAnimation();
            _state = RenderAnimatedSizeState.Changed;
        }
        else if (_controller.Value >= 1.0)
        {
            _beginSize = Child.Size;
            _endSize = Child.Size;
        }
        else if (!_controller.IsAnimating)
        {
            _controller.Forward();
        }
    }

    private void LayoutChanged()
    {
        if (_endSize != Child!.Size)
        {
            _beginSize = Child.Size;
            _endSize = Child.Size;
            RestartAnimation();
            _state = RenderAnimatedSizeState.Unstable;
        }
        else
        {
            _state = RenderAnimatedSizeState.Stable;
            if (!_controller.IsAnimating)
            {
                _controller.Forward();
            }
        }
    }

    private void LayoutUnstable()
    {
        if (_endSize != Child!.Size)
        {
            _beginSize = Child.Size;
            _endSize = Child.Size;
            RestartAnimation();
        }
        else
        {
            _controller.Stop();
            _state = RenderAnimatedSizeState.Stable;
        }
    }

    private void RestartAnimation() => _controller.Forward(from: 0.0);

    private Size EvaluateSize()
    {
        double t = _controller.Evaluate();
        return new Size(
            _beginSize.Width + ((_endSize.Width - _beginSize.Width) * t),
            _beginSize.Height + ((_endSize.Height - _beginSize.Height) * t));
    }

    private void HandleControllerChanged() => MarkNeedsLayout();

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        if (_hasVisualOverflow && ClipBehavior != Clip.None)
        {
            context.PushClipRect(new Rect(offset, Size), clippedContext => base.Paint(clippedContext, offset));
            return;
        }

        base.Paint(context, offset);
    }
}
