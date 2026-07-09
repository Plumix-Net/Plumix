using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source (reference): flutter/packages/flutter/lib/src/cupertino/activity_indicator.dart (adapted)

public sealed class CupertinoActivityIndicator : StatefulWidget
{
    private const double DefaultIndicatorRadius = 10.0;
    private const int TickCount = 8;
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromSeconds(1);
    private static readonly Color DefaultActiveTickLightColor = Avalonia.Media.Color.FromArgb(0xFF, 0x3C, 0x3C, 0x44);
    private static readonly Color DefaultActiveTickDarkColor = Avalonia.Media.Color.FromArgb(0xFF, 0xEB, 0xEB, 0xF5);

    public CupertinoActivityIndicator(
        Color? color = null,
        bool animating = true,
        double radius = DefaultIndicatorRadius,
        bool isDark = false,
        Key? key = null)
        : this(
            color: color,
            animating: animating,
            radius: radius,
            progress: 1.0,
            isDark: isDark,
            key: key)
    {
    }

    private CupertinoActivityIndicator(
        Color? color,
        bool animating,
        double radius,
        double progress,
        bool isDark,
        Key? key = null)
        : base(key)
    {
        if (double.IsNaN(radius) || double.IsInfinity(radius) || radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "CupertinoActivityIndicator radius must be finite and greater than zero.");
        }

        if (double.IsNaN(progress) || double.IsInfinity(progress) || progress < 0 || progress > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(progress), "CupertinoActivityIndicator progress must be finite and between 0 and 1.");
        }

        Color = color;
        Animating = animating;
        Radius = radius;
        Progress = progress;
        IsDark = isDark;
    }

    public Color? Color { get; }

    public bool Animating { get; }

    public double Radius { get; }

    public double Progress { get; }

    public bool IsDark { get; }

    public static CupertinoActivityIndicator PartiallyRevealed(
        Color? color = null,
        double radius = DefaultIndicatorRadius,
        double progress = 1.0,
        bool isDark = false,
        Key? key = null)
    {
        return new CupertinoActivityIndicator(
            color: color,
            animating: false,
            radius: radius,
            progress: progress,
            isDark: isDark,
            key: key);
    }

    public override State CreateState()
    {
        return new CupertinoActivityIndicatorState();
    }

    private sealed class CupertinoActivityIndicatorState : State
    {
        private AnimationController? _controller;
        private bool _isMounted;

        private CupertinoActivityIndicator CurrentWidget => (CupertinoActivityIndicator)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(DefaultAnimationDuration);
            _isMounted = true;
            UpdateAnimatingStatus();
            _controller.Changed += HandleControllerTick;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldIndicator = (CupertinoActivityIndicator)oldWidget;
            if (oldIndicator.Animating != CurrentWidget.Animating)
            {
                UpdateAnimatingStatus();
            }
        }

        public override void Dispose()
        {
            _isMounted = false;
            if (_controller is null)
            {
                return;
            }

            _controller.Changed -= HandleControllerTick;
            _controller.Dispose();
            _controller = null;
        }

        public override Widget Build(BuildContext context)
        {
            var resolvedColor = CurrentWidget.Color
                                ?? (CurrentWidget.IsDark
                                    ? DefaultActiveTickDarkColor
                                    : DefaultActiveTickLightColor);
            double position = _controller?.Evaluate() ?? 0.0;

            return new SizedBox(
                width: CurrentWidget.Radius * 2.0,
                height: CurrentWidget.Radius * 2.0,
                child: new CupertinoActivityIndicatorRenderWidget(
                    position: position,
                    activeColor: resolvedColor,
                    radius: CurrentWidget.Radius,
                    progress: CurrentWidget.Progress));
        }

        private void UpdateAnimatingStatus()
        {
            if (_controller is null)
            {
                return;
            }

            if (CurrentWidget.Animating)
            {
                if (!_controller.IsAnimating)
                {
                    _controller.Repeat();
                }
                return;
            }

            if (_controller.IsAnimating)
            {
                _controller.Stop();
            }
        }

        private void HandleControllerTick()
        {
            if (!_isMounted || !CurrentWidget.Animating)
            {
                return;
            }

            SetState(() => { });
        }
    }
}

internal sealed class CupertinoActivityIndicatorRenderWidget : LeafRenderObjectWidget
{
    public CupertinoActivityIndicatorRenderWidget(
        double position,
        Color activeColor,
        double radius,
        double progress,
        Key? key = null) : base(key)
    {
        Position = position;
        ActiveColor = activeColor;
        Radius = radius;
        Progress = progress;
    }

    public double Position { get; }

    public Color ActiveColor { get; }

    public double Radius { get; }

    public double Progress { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoActivityIndicator(
            position: Position,
            activeColor: ActiveColor,
            radius: Radius,
            progress: Progress);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var indicator = (RenderCupertinoActivityIndicator)renderObject;
        indicator.Position = Position;
        indicator.ActiveColor = ActiveColor;
        indicator.Radius = Radius;
        indicator.Progress = Progress;
    }
}

internal sealed class RenderCupertinoActivityIndicator : RenderBox
{
    private const double TwoPi = Math.PI * 2.0;
    private const double DefaultIndicatorRadius = 10.0;
    private const int TickCount = 8;
    private const byte PartiallyRevealedAlpha = 147;
    private static readonly byte[] TickAlphaValues = [47, 47, 47, 47, 72, 97, 122, 147];

    private double _position;
    private Color _activeColor;
    private double _radius;
    private double _progress;

    public RenderCupertinoActivityIndicator(
        double position,
        Color activeColor,
        double radius,
        double progress)
    {
        _position = position;
        _activeColor = activeColor;
        _radius = radius;
        _progress = progress;
    }

    public double Position
    {
        get => _position;
        set
        {
            if (Math.Abs(_position - value) <= 0.0001)
            {
                return;
            }

            _position = value;
            MarkNeedsPaint();
        }
    }

    public Color ActiveColor
    {
        get => _activeColor;
        set
        {
            if (_activeColor == value)
            {
                return;
            }

            _activeColor = value;
            MarkNeedsPaint();
        }
    }

    public double Radius
    {
        get => _radius;
        set
        {
            if (Math.Abs(_radius - value) <= 0.0001)
            {
                return;
            }

            _radius = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            if (Math.Abs(_progress - value) <= 0.0001)
            {
                return;
            }

            _progress = value;
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        double side = Math.Max(0, _radius * 2.0);
        Size = Constraints.Constrain(new Size(side, side));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0 || _radius <= 0)
        {
            return;
        }

        double progress = Math.Clamp(_progress, 0.0, 1.0);
        if (progress <= 0.0)
        {
            return;
        }

        var center = new Point(offset.X + (Size.Width / 2.0), offset.Y + (Size.Height / 2.0));
        double tickRadius = _radius / DefaultIndicatorRadius;
        var tickRect = new Rect(
            x: -tickRadius,
            y: -_radius,
            width: tickRadius * 2.0,
            height: _radius - (_radius / 3.0));
        int activeTick = PositiveModulo((int)Math.Floor(TickCount * Math.Clamp(_position, 0.0, 1.0)), TickCount);

        ctx.PushTransform(Matrix.CreateTranslation(center.X, center.Y), centeredContext =>
        {
            for (int i = 0; i < TickCount * progress; i++)
            {
                int tickIndex = PositiveModulo(i - activeTick, TickCount);
                byte alpha = progress < 1.0 ? PartiallyRevealedAlpha : TickAlphaValues[tickIndex];
                var tickColor = Color.FromArgb(alpha, _activeColor.R, _activeColor.G, _activeColor.B);
                double angle = i * TwoPi / TickCount;
                centeredContext.PushTransform(CreateRotationMatrix(angle), rotatedContext =>
                {
                    rotatedContext.DrawRectangle(
                        new SolidColorBrush(tickColor),
                        pen: null,
                        rect: tickRect,
                        radiusX: tickRadius,
                        radiusY: tickRadius);
                });
            }
        });
    }

    private static Matrix CreateRotationMatrix(double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return new Matrix(cos, sin, -sin, cos, 0, 0);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }
}
