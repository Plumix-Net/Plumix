using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/implicit_animations.dart

public sealed class DecorationTween : Tween<Decoration>
{
    private sealed record NullDecoration : Decoration
    {
        public override BoxPainter CreateBoxPainter(Action? onChanged = null)
        {
            throw new InvalidOperationException("The null decoration sentinel cannot be painted.");
        }
    }

    private static readonly Decoration BeginNull = new NullDecoration();
    private static readonly Decoration EndNull = new NullDecoration();

    public DecorationTween(Decoration? begin = null, Decoration? end = null)
    {
        Begin = begin;
        End = end;
    }

    public new Decoration? Begin
    {
        get => ReferenceEquals(GetBeginValue(), BeginNull) ? null : GetBeginValue();
        set => SetBeginValue(value ?? BeginNull);
    }

    public new Decoration? End
    {
        get => ReferenceEquals(GetEndValue(), EndNull) ? null : GetEndValue();
        set => SetEndValue(value ?? EndNull);
    }

    public override Decoration Lerp(Decoration a, Decoration b, double t)
    {
        Decoration? begin = ReferenceEquals(a, BeginNull) ? null : a;
        Decoration? end = ReferenceEquals(b, EndNull) ? null : b;
        return Decoration.Lerp(begin, end, t)
               ?? throw new InvalidOperationException("DecorationTween cannot interpolate two null decorations.");
    }
}

public sealed class AnimatedOpacity : StatefulWidget
{
    public AnimatedOpacity(
        double opacity,
        TimeSpan duration,
        Widget? child = null,
        Curve? curve = null,
        Action? onEnd = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(opacity) || opacity < 0.0 || opacity > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between zero and one.");
        }
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Opacity = opacity;
        Duration = duration;
        Child = child;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public double Opacity { get; }

    public TimeSpan Duration { get; }

    public Widget? Child { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public bool AlwaysIncludeSemantics { get; }

    public override State CreateState() => new AnimatedOpacityState();

    private sealed class AnimatedOpacityState : State
    {
        private AnimationController? _controller;
        private double _begin;
        private double _end;

        private AnimatedOpacity CurrentWidget => (AnimatedOpacity)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Opacity;
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            double current = Evaluate(_controller.Evaluate());
            if (CurrentWidget.Opacity != _end)
            {
                _begin = current;
                _end = CurrentWidget.Opacity;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new Opacity(
                opacity: Evaluate(_controller!.Evaluate()),
                alwaysIncludeSemantics: CurrentWidget.AlwaysIncludeSemantics,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private double Evaluate(double t) => _begin + ((_end - _begin) * t);

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class SliverAnimatedOpacity : StatefulWidget
{
    public SliverAnimatedOpacity(
        double opacity,
        TimeSpan duration,
        Widget? sliver = null,
        Curve? curve = null,
        Action? onEnd = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(opacity) || opacity < 0.0 || opacity > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between zero and one.");
        }
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Opacity = opacity;
        Duration = duration;
        Sliver = sliver;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public double Opacity { get; }

    public TimeSpan Duration { get; }

    public Widget? Sliver { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public bool AlwaysIncludeSemantics { get; }

    public override State CreateState() => new SliverAnimatedOpacityState();

    private sealed class SliverAnimatedOpacityState : State
    {
        private AnimationController? _controller;
        private CurvedAnimation? _animation;
        private MappedDoubleAnimation? _opacityAnimation;
        private double _begin;
        private double _end;

        private SliverAnimatedOpacity CurrentWidget => (SliverAnimatedOpacity)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Opacity;
            _controller = new AnimationController(CurrentWidget.Duration, this);
            _animation = new CurvedAnimation(_controller, CurrentWidget.Curve);
            _opacityAnimation = new MappedDoubleAnimation(_animation, Evaluate);
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _animation!.Curve = CurrentWidget.Curve;
            double current = Evaluate(_animation.Value);
            if (CurrentWidget.Opacity != _end)
            {
                _begin = current;
                _end = CurrentWidget.Opacity;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new SliverFadeTransition(
                opacity: _opacityAnimation!,
                sliver: CurrentWidget.Sliver,
                alwaysIncludeSemantics: CurrentWidget.AlwaysIncludeSemantics);
        }

        public override void Dispose()
        {
            _controller!.Completed -= HandleCompleted;
            _opacityAnimation!.Dispose();
            _animation!.Dispose();
            _controller.Dispose();
            _opacityAnimation = null;
            _animation = null;
            _controller = null;
        }

        private double Evaluate(double t) => _begin + ((_end - _begin) * t);

        private void HandleCompleted()
        {
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedSlide : StatefulWidget
{
    public AnimatedSlide(
        Vector offset,
        TimeSpan duration,
        Widget? child = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Offset = offset;
        Duration = duration;
        Child = child;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Vector Offset { get; }

    public TimeSpan Duration { get; }

    public Widget? Child { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedSlideState();

    private sealed class AnimatedSlideState : State
    {
        private AnimationController? _controller;
        private Vector _begin;
        private Vector _end;

        private AnimatedSlide CurrentWidget => (AnimatedSlide)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Offset;
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            Vector current = Evaluate(_controller.Evaluate());
            if (CurrentWidget.Offset != _end)
            {
                _begin = current;
                _end = CurrentWidget.Offset;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new FractionalTranslation(
                translation: Evaluate(_controller!.Evaluate()),
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private Vector Evaluate(double t)
        {
            return new Vector(
                _begin.X + ((_end.X - _begin.X) * t),
                _begin.Y + ((_end.Y - _begin.Y) * t));
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedScale : StatefulWidget
{
    public AnimatedScale(
        double scale,
        TimeSpan duration,
        Widget? child = null,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Scale = scale;
        Duration = duration;
        Child = child;
        Alignment = alignment;
        FilterQuality = filterQuality;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public double Scale { get; }

    public TimeSpan Duration { get; }

    public Widget? Child { get; }

    public Alignment Alignment { get; }

    public FilterQuality? FilterQuality { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedScaleState();

    private sealed class AnimatedScaleState : State
    {
        private AnimationController? _controller;
        private double _begin;
        private double _end;

        private AnimatedScale CurrentWidget => (AnimatedScale)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Scale;
            CreateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            double current = Evaluate(_controller.Evaluate());
            if (CurrentWidget.Scale != _end)
            {
                _begin = current;
                _end = CurrentWidget.Scale;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            double scale = Evaluate(_controller!.Evaluate());
            return new Transform(
                transform: Matrix.CreateScale(scale, scale),
                alignment: CurrentWidget.Alignment,
                filterQuality: CurrentWidget.FilterQuality,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private void CreateController()
        {
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        private void DisposeController()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private double Evaluate(double t) => _begin + ((_end - _begin) * t);

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedRotation : StatefulWidget
{
    public AnimatedRotation(
        double turns,
        TimeSpan duration,
        Widget? child = null,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if (!double.IsFinite(turns))
        {
            throw new ArgumentOutOfRangeException(nameof(turns));
        }

        Turns = turns;
        Duration = duration;
        Child = child;
        Alignment = alignment;
        FilterQuality = filterQuality;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public double Turns { get; }

    public TimeSpan Duration { get; }

    public Widget? Child { get; }

    public Alignment Alignment { get; }

    public FilterQuality? FilterQuality { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedRotationState();

    private sealed class AnimatedRotationState : State
    {
        private AnimationController? _controller;
        private double _begin;
        private double _end;

        private AnimatedRotation CurrentWidget => (AnimatedRotation)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Turns;
            CreateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            double current = Evaluate(_controller.Evaluate());
            if (CurrentWidget.Turns != _end)
            {
                _begin = current;
                _end = CurrentWidget.Turns;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            double radians = Evaluate(_controller!.Evaluate()) * Math.PI * 2.0;
            return new Transform(
                transform: CreateRotationMatrix(radians),
                alignment: CurrentWidget.Alignment,
                filterQuality: CurrentWidget.FilterQuality,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private void CreateController()
        {
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        private void DisposeController()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private double Evaluate(double t) => _begin + ((_end - _begin) * t);

        private static Matrix CreateRotationMatrix(double radians)
        {
            if (radians == 0.0) return Matrix.Identity;

            double sine = Math.Sin(radians);
            if (sine == 1.0) return new Matrix(0, 1, -1, 0, 0, 0);
            if (sine == -1.0) return new Matrix(0, -1, 1, 0, 0, 0);

            double cosine = Math.Cos(radians);
            if (cosine == -1.0) return new Matrix(-1, 0, 0, -1, 0, 0);
            return new Matrix(cosine, sine, -sine, cosine, 0, 0);
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedContainer : StatefulWidget
{
    public AnimatedContainer(
        TimeSpan duration,
        Widget? child = null,
        Alignment? alignment = null,
        Thickness? padding = null,
        Color? color = null,
        Decoration? decoration = null,
        Decoration? foregroundDecoration = null,
        double? width = null,
        double? height = null,
        BoxConstraints? constraints = null,
        Thickness? margin = null,
        Matrix? transform = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (color.HasValue && decoration is not null)
        {
            throw new ArgumentException("color and decoration cannot both be specified.");
        }
        ValidateThickness(padding, nameof(padding));
        ValidateThickness(margin, nameof(margin));

        Duration = duration;
        Child = child;
        Alignment = alignment;
        Padding = padding;
        Decoration = decoration ?? (color.HasValue ? new BoxDecoration(Color: color) : null);
        ForegroundDecoration = foregroundDecoration;
        Constraints = width.HasValue || height.HasValue
            ? constraints?.Tighten(width: width, height: height)
              ?? BoxConstraints.TightFor(width: width, height: height)
            : constraints;
        Margin = margin;
        Transform = transform;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Widget? Child { get; }
    public Alignment? Alignment { get; }
    public Thickness? Padding { get; }
    public Decoration? Decoration { get; }
    public Decoration? ForegroundDecoration { get; }
    public BoxConstraints? Constraints { get; }
    public Thickness? Margin { get; }
    public Matrix? Transform { get; }
    public TimeSpan Duration { get; }
    public Curve Curve { get; }
    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedContainerState();

    private static void ValidateThickness(Thickness? value, string parameterName)
    {
        if (value is not { } thickness) return;
        if (thickness.Left < 0 || thickness.Top < 0 || thickness.Right < 0 || thickness.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Insets must be non-negative.");
        }
    }

    private sealed class AnimatedContainerState : State
    {
        private AnimationController? _controller;
        private AnimatedContainerValues _begin = null!;
        private AnimatedContainerValues _end = null!;

        private AnimatedContainer CurrentWidget => (AnimatedContainer)StateWidget;

        public override void InitState()
        {
            _begin = _end = AnimatedContainerValues.From(CurrentWidget);
            CreateController(CurrentWidget.Duration);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldContainer = (AnimatedContainer)oldWidget;
            var current = Evaluate(_controller!.Evaluate());
            var target = AnimatedContainerValues.From(CurrentWidget);
            bool targetChanged = target != _end;
            bool durationChanged = oldContainer.Duration != CurrentWidget.Duration;
            bool wasAnimating = _controller.IsAnimating;

            if (durationChanged)
            {
                DisposeController();
                CreateController(CurrentWidget.Duration);
            }
            else
            {
                _controller.Curve = CurrentWidget.Curve;
            }

            if (targetChanged)
            {
                _begin = current;
                _end = target;
                _controller.Forward(from: 0);
            }
            else if (durationChanged)
            {
                if (wasAnimating && current != target)
                {
                    _begin = current;
                    _end = target;
                    _controller.Forward(from: 0);
                }
                else
                {
                    _begin = _end = target;
                }
            }
        }

        public override Widget Build(BuildContext context)
        {
            var values = Evaluate(_controller!.Evaluate());
            return new Container(
                child: CurrentWidget.Child,
                alignment: values.Alignment,
                padding: values.Padding,
                decoration: values.Decoration,
                constraints: values.Constraints,
                margin: values.Margin,
                transform: values.Transform,
                foregroundDecoration: values.ForegroundDecoration);
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private void CreateController(TimeSpan duration)
        {
            _controller = new AnimationController(duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        private void DisposeController()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private AnimatedContainerValues Evaluate(double t)
        {
            return new AnimatedContainerValues(
                Alignment: LerpAlignment(_begin.Alignment, _end.Alignment, t),
                Padding: LerpThickness(_begin.Padding, _end.Padding, t),
                Decoration: Decoration.Lerp(_begin.Decoration, _end.Decoration, t),
                ForegroundDecoration: Decoration.Lerp(
                    _begin.ForegroundDecoration,
                    _end.ForegroundDecoration,
                    t),
                Constraints: LerpConstraints(_begin.Constraints, _end.Constraints, t),
                Margin: LerpThickness(_begin.Margin, _end.Margin, t),
                Transform: LerpMatrix(_begin.Transform, _end.Transform, t));
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }

        private static Alignment? LerpAlignment(Alignment? a, Alignment? b, double t)
        {
            if (!a.HasValue || !b.HasValue) return t < 1 ? a : b;
            return new Alignment(
                LerpDouble(a.Value.X, b.Value.X, t),
                LerpDouble(a.Value.Y, b.Value.Y, t));
        }

        private static Thickness? LerpThickness(Thickness? a, Thickness? b, double t)
        {
            if (!a.HasValue && !b.HasValue) return null;
            var from = a ?? default;
            var to = b ?? default;
            return new Thickness(
                LerpDouble(from.Left, to.Left, t),
                LerpDouble(from.Top, to.Top, t),
                LerpDouble(from.Right, to.Right, t),
                LerpDouble(from.Bottom, to.Bottom, t));
        }

        private static BoxConstraints? LerpConstraints(BoxConstraints? a, BoxConstraints? b, double t)
        {
            if (!a.HasValue && !b.HasValue) return null;
            var from = a ?? new BoxConstraints(0, 0, 0, 0);
            var to = b ?? new BoxConstraints(0, 0, 0, 0);
            return new BoxConstraints(
                MinWidth: LerpConstraint(from.MinWidth, to.MinWidth, t),
                MaxWidth: LerpConstraint(from.MaxWidth, to.MaxWidth, t),
                MinHeight: LerpConstraint(from.MinHeight, to.MinHeight, t),
                MaxHeight: LerpConstraint(from.MaxHeight, to.MaxHeight, t));
        }

        private static Matrix? LerpMatrix(Matrix? a, Matrix? b, double t)
        {
            if (!a.HasValue || !b.HasValue) return t < 1 ? a : b;
            var from = a.Value;
            var to = b.Value;
            return new Matrix(
                LerpDouble(from.M11, to.M11, t),
                LerpDouble(from.M12, to.M12, t),
                LerpDouble(from.M21, to.M21, t),
                LerpDouble(from.M22, to.M22, t),
                LerpDouble(from.M31, to.M31, t),
                LerpDouble(from.M32, to.M32, t));
        }

        private static double LerpConstraint(double a, double b, double t)
        {
            if (double.IsPositiveInfinity(a) && double.IsPositiveInfinity(b)) return double.PositiveInfinity;
            if (double.IsPositiveInfinity(a) || double.IsPositiveInfinity(b)) return t < 0.5 ? a : b;
            return LerpDouble(a, b, t);
        }

        private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);
    }

    private sealed record AnimatedContainerValues(
        Alignment? Alignment,
        Thickness? Padding,
        Decoration? Decoration,
        Decoration? ForegroundDecoration,
        BoxConstraints? Constraints,
        Thickness? Margin,
        Matrix? Transform)
    {
        public static AnimatedContainerValues From(AnimatedContainer widget)
        {
            return new AnimatedContainerValues(
                widget.Alignment,
                widget.Padding,
                widget.Decoration,
                widget.ForegroundDecoration,
                widget.Constraints,
                widget.Margin,
                widget.Transform);
        }
    }
}

public sealed class AnimatedPadding : StatefulWidget
{
    public AnimatedPadding(
        Thickness padding,
        TimeSpan duration,
        Widget? child = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        ValidatePadding(padding);

        Padding = padding;
        Duration = duration;
        Child = child;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Thickness Padding { get; }

    public TimeSpan Duration { get; }

    public Widget? Child { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedPaddingState();

    private static void ValidatePadding(Thickness padding)
    {
        if (!(padding.Left >= 0) || !(padding.Top >= 0) || !(padding.Right >= 0) || !(padding.Bottom >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(padding), "Insets must be non-negative.");
        }
    }

    private sealed class AnimatedPaddingState : State
    {
        private AnimationController? _controller;
        private Thickness _begin;
        private Thickness _end;

        private AnimatedPadding CurrentWidget => (AnimatedPadding)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Padding;
            CreateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            Thickness current = Evaluate(_controller!.Evaluate());
            bool targetChanged = CurrentWidget.Padding != _end;

            if (targetChanged)
            {
                _begin = current;
                _end = CurrentWidget.Padding;
                _controller.Forward(from: 0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new Padding(Evaluate(_controller!.Evaluate()), CurrentWidget.Child);
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private void CreateController()
        {
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        private void DisposeController()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private Thickness Evaluate(double t)
        {
            return new Thickness(
                Math.Max(0, LerpDouble(_begin.Left, _end.Left, t)),
                Math.Max(0, LerpDouble(_begin.Top, _end.Top, t)),
                Math.Max(0, LerpDouble(_begin.Right, _end.Right, t)),
                Math.Max(0, LerpDouble(_begin.Bottom, _end.Bottom, t)));
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);
}

public sealed class AnimatedAlign : StatefulWidget
{
    public AnimatedAlign(
        Alignment alignment,
        TimeSpan duration,
        Widget? child = null,
        double? heightFactor = null,
        double? widthFactor = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (widthFactor is double width && !(width >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(widthFactor));
        }
        if (heightFactor is double height && !(height >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(heightFactor));
        }

        Alignment = alignment;
        Duration = duration;
        Child = child;
        HeightFactor = heightFactor;
        WidthFactor = widthFactor;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Alignment Alignment { get; }

    public TimeSpan Duration { get; }

    public Widget? Child { get; }

    public double? HeightFactor { get; }

    public double? WidthFactor { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedAlignState();

    private sealed class AnimatedAlignState : State
    {
        private AnimationController? _controller;
        private AnimatedAlignValues _begin = null!;
        private AnimatedAlignValues _end = null!;

        private AnimatedAlign CurrentWidget => (AnimatedAlign)StateWidget;

        public override void InitState()
        {
            _begin = _end = AnimatedAlignValues.From(CurrentWidget);
            CreateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            AnimatedAlignValues current = Evaluate(_controller!.Evaluate());
            AnimatedAlignValues target = AnimatedAlignValues.From(CurrentWidget);
            bool shouldStart = target.Alignment != _end.Alignment
                               || IsTweenTargetChanged(_end.HeightFactor, target.HeightFactor)
                               || IsTweenTargetChanged(_end.WidthFactor, target.WidthFactor);

            if (shouldStart)
            {
                _begin = new AnimatedAlignValues(
                    Alignment: current.Alignment,
                    HeightFactor: ResolveTweenBegin(
                        current.HeightFactor,
                        _end.HeightFactor,
                        target.HeightFactor),
                    WidthFactor: ResolveTweenBegin(
                        current.WidthFactor,
                        _end.WidthFactor,
                        target.WidthFactor));
                _end = target;
                _controller.Forward(from: 0);
            }
            else
            {
                _begin = _begin with
                {
                    HeightFactor = ResolveUnanimatedTarget(_begin.HeightFactor, target.HeightFactor),
                    WidthFactor = ResolveUnanimatedTarget(_begin.WidthFactor, target.WidthFactor)
                };
                _end = _end with
                {
                    HeightFactor = target.HeightFactor,
                    WidthFactor = target.WidthFactor
                };
            }
        }

        public override Widget Build(BuildContext context)
        {
            AnimatedAlignValues values = Evaluate(_controller!.Evaluate());
            return new Align(
                alignment: values.Alignment,
                heightFactor: values.HeightFactor,
                widthFactor: values.WidthFactor,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private void CreateController()
        {
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        private void DisposeController()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private AnimatedAlignValues Evaluate(double t)
        {
            return new AnimatedAlignValues(
                Alignment: new Alignment(
                    LerpDouble(_begin.Alignment.X, _end.Alignment.X, t),
                    LerpDouble(_begin.Alignment.Y, _end.Alignment.Y, t)),
                HeightFactor: LerpNullableDouble(_begin.HeightFactor, _end.HeightFactor, t),
                WidthFactor: LerpNullableDouble(_begin.WidthFactor, _end.WidthFactor, t));
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }

        private static double? LerpNullableDouble(double? a, double? b, double t)
        {
            if (!b.HasValue) return null;
            if (!a.HasValue) return b;
            return LerpDouble(a.Value, b.Value, t);
        }

        private static bool IsTweenTargetChanged(double? previousTarget, double? target)
        {
            return previousTarget.HasValue && target.HasValue && previousTarget != target;
        }

        private static double? ResolveTweenBegin(double? current, double? previousTarget, double? target)
        {
            if (!target.HasValue) return null;
            return previousTarget.HasValue ? current : target;
        }

        private static double? ResolveUnanimatedTarget(double? begin, double? target)
        {
            return target.HasValue ? begin ?? target : null;
        }
    }

    private sealed record AnimatedAlignValues(
        Alignment Alignment,
        double? HeightFactor,
        double? WidthFactor)
    {
        public static AnimatedAlignValues From(AnimatedAlign widget)
        {
            return new AnimatedAlignValues(widget.Alignment, widget.HeightFactor, widget.WidthFactor);
        }
    }

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);
}

public sealed class AnimatedPositioned : StatefulWidget
{
    public AnimatedPositioned(
        Widget child,
        TimeSpan duration,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        ValidatePosition(left, top, right, bottom, width, height);
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));

        Child = child;
        Duration = duration;
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Width = width;
        Height = height;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Widget Child { get; }

    public TimeSpan Duration { get; }

    public double? Left { get; }

    public double? Top { get; }

    public double? Right { get; }

    public double? Bottom { get; }

    public double? Width { get; }

    public double? Height { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public static AnimatedPositioned FromRect(
        Rect rect,
        Widget child,
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null)
    {
        return new AnimatedPositioned(
            child: child,
            duration: duration,
            left: rect.Left,
            top: rect.Top,
            width: rect.Width,
            height: rect.Height,
            curve: curve,
            onEnd: onEnd,
            key: key);
    }

    public override State CreateState() => new AnimatedPositionedState();

    private static void ValidatePosition(
        double? left,
        double? top,
        double? right,
        double? bottom,
        double? width,
        double? height)
    {
        if (left.HasValue && right.HasValue && width.HasValue)
        {
            throw new ArgumentException("Cannot provide left, right, and width simultaneously.");
        }

        if (top.HasValue && bottom.HasValue && height.HasValue)
        {
            throw new ArgumentException("Cannot provide top, bottom, and height simultaneously.");
        }
    }

    private sealed class AnimatedPositionedState : State
    {
        private AnimationController? _controller;
        private AnimatedPositionedValues _begin = null!;
        private AnimatedPositionedValues _end = null!;

        private AnimatedPositioned CurrentWidget => (AnimatedPositioned)StateWidget;

        public override void InitState()
        {
            _begin = _end = AnimatedPositionedValues.From(CurrentWidget);
            CreateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            AnimatedPositionedValues current = Evaluate(_controller.Evaluate());
            AnimatedPositionedValues target = AnimatedPositionedValues.From(CurrentWidget);
            UpdateTweens(current, target);
        }

        public override Widget Build(BuildContext context)
        {
            AnimatedPositionedValues values = Evaluate(_controller!.Evaluate());
            return new Positioned(
                child: CurrentWidget.Child,
                left: values.Left,
                top: values.Top,
                right: values.Right,
                bottom: values.Bottom,
                width: values.Width,
                height: values.Height);
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private void UpdateTweens(AnimatedPositionedValues current, AnimatedPositionedValues target)
        {
            bool shouldStart = AnimatedPositionedValues.HasAnimatedChange(_end, target);
            if (shouldStart)
            {
                _begin = AnimatedPositionedValues.TweenBegins(current, _end, target);
                _end = target;
                _controller!.Forward(from: 0);
                return;
            }

            _begin = AnimatedPositionedValues.ApplyNullTransitions(_begin, _end, target);
            _end = AnimatedPositionedValues.ApplyNullTransitions(_end, _end, target);
        }

        private AnimatedPositionedValues Evaluate(double t)
        {
            return AnimatedPositionedValues.Lerp(_begin, _end, t);
        }

        private void CreateController()
        {
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        private void DisposeController()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedPositionedDirectional : StatefulWidget
{
    public AnimatedPositionedDirectional(
        Widget child,
        TimeSpan duration,
        double? start = null,
        double? top = null,
        double? end = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        ValidatePosition(start, top, end, bottom, width, height);
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));

        Child = child;
        Duration = duration;
        Start = start;
        Top = top;
        End = end;
        Bottom = bottom;
        Width = width;
        Height = height;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Widget Child { get; }

    public TimeSpan Duration { get; }

    public double? Start { get; }

    public double? Top { get; }

    public double? End { get; }

    public double? Bottom { get; }

    public double? Width { get; }

    public double? Height { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedPositionedDirectionalState();

    private static void ValidatePosition(
        double? start,
        double? top,
        double? end,
        double? bottom,
        double? width,
        double? height)
    {
        if (start.HasValue && end.HasValue && width.HasValue)
        {
            throw new ArgumentException("Cannot provide start, end, and width simultaneously.");
        }

        if (top.HasValue && bottom.HasValue && height.HasValue)
        {
            throw new ArgumentException("Cannot provide top, bottom, and height simultaneously.");
        }
    }

    private sealed class AnimatedPositionedDirectionalState : State
    {
        private AnimationController? _controller;
        private AnimatedPositionedValues _begin = null!;
        private AnimatedPositionedValues _end = null!;

        private AnimatedPositionedDirectional CurrentWidget =>
            (AnimatedPositionedDirectional)StateWidget;

        public override void InitState()
        {
            _begin = _end = AnimatedPositionedValues.From(CurrentWidget);
            CreateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            AnimatedPositionedValues current = Evaluate(_controller.Evaluate());
            AnimatedPositionedValues target = AnimatedPositionedValues.From(CurrentWidget);
            UpdateTweens(current, target);
        }

        public override Widget Build(BuildContext context)
        {
            AnimatedPositionedValues values = Evaluate(_controller!.Evaluate());
            TextDirection textDirection = Directionality.MaybeOf(context)
                ?? throw new InvalidOperationException(
                    "AnimatedPositionedDirectional requires a Directionality ancestor.");
            return Positioned.Directional(
                textDirection: textDirection,
                child: CurrentWidget.Child,
                start: values.Left,
                top: values.Top,
                end: values.Right,
                bottom: values.Bottom,
                width: values.Width,
                height: values.Height);
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private void UpdateTweens(AnimatedPositionedValues current, AnimatedPositionedValues target)
        {
            bool shouldStart = AnimatedPositionedValues.HasAnimatedChange(_end, target);
            if (shouldStart)
            {
                _begin = AnimatedPositionedValues.TweenBegins(current, _end, target);
                _end = target;
                _controller!.Forward(from: 0);
                return;
            }

            _begin = AnimatedPositionedValues.ApplyNullTransitions(_begin, _end, target);
            _end = AnimatedPositionedValues.ApplyNullTransitions(_end, _end, target);
        }

        private AnimatedPositionedValues Evaluate(double t)
        {
            return AnimatedPositionedValues.Lerp(_begin, _end, t);
        }

        private void CreateController()
        {
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        private void DisposeController()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedDefaultTextStyle : StatefulWidget
{
    public AnimatedDefaultTextStyle(
        Widget child,
        TextStyle style,
        TimeSpan duration,
        TextAlign? textAlign = null,
        bool softWrap = true,
        TextOverflow overflow = TextOverflow.Clip,
        int? maxLines = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(style);
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }

        Child = child;
        Style = style;
        Duration = duration;
        TextAlign = textAlign;
        SoftWrap = softWrap;
        Overflow = overflow;
        MaxLines = maxLines;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Widget Child { get; }

    public TextStyle Style { get; }

    public TextAlign? TextAlign { get; }

    public bool SoftWrap { get; }

    public TextOverflow Overflow { get; }

    public int? MaxLines { get; }

    public TextWidthBasis TextWidthBasis { get; }

    public TextHeightBehavior? TextHeightBehavior { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedDefaultTextStyleState();

    private sealed class AnimatedDefaultTextStyleState : State
    {
        private AnimationController? _controller;
        private TextStyle _begin = null!;
        private TextStyle _end = null!;

        private AnimatedDefaultTextStyle CurrentWidget => (AnimatedDefaultTextStyle)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Style;
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            TextStyle current = Evaluate(_controller.Evaluate());
            if (!Equals(CurrentWidget.Style, _end))
            {
                _begin = current;
                _end = CurrentWidget.Style;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new DefaultTextStyle(
                style: Evaluate(_controller!.Evaluate()),
                textAlign: CurrentWidget.TextAlign,
                softWrap: CurrentWidget.SoftWrap,
                overflow: CurrentWidget.Overflow,
                maxLines: CurrentWidget.MaxLines,
                textWidthBasis: CurrentWidget.TextWidthBasis,
                textHeightBehavior: CurrentWidget.TextHeightBehavior,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private TextStyle Evaluate(double t) => TextStyle.Lerp(_begin, _end, t);

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedPhysicalModel : StatefulWidget
{
    public AnimatedPhysicalModel(
        Widget child,
        Color color,
        Color shadowColor,
        TimeSpan duration,
        BoxShape shape = BoxShape.Rectangle,
        Clip clipBehavior = Clip.None,
        BorderRadius? borderRadius = null,
        double elevation = 0.0,
        bool animateColor = true,
        bool animateShadowColor = true,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if (!double.IsFinite(elevation) || elevation < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }

        Child = child;
        Shape = shape;
        ClipBehavior = clipBehavior;
        BorderRadius = borderRadius;
        Elevation = elevation;
        Color = color;
        AnimateColor = animateColor;
        ShadowColor = shadowColor;
        AnimateShadowColor = animateShadowColor;
        Duration = duration;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Widget Child { get; }

    public BoxShape Shape { get; }

    public Clip ClipBehavior { get; }

    public BorderRadius? BorderRadius { get; }

    public double Elevation { get; }

    public Color Color { get; }

    public bool AnimateColor { get; }

    public Color ShadowColor { get; }

    public bool AnimateShadowColor { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedPhysicalModelState();

    private sealed class AnimatedPhysicalModelState : State
    {
        private readonly ColorTween _colorTween = new();
        private AnimationController? _controller;
        private AnimatedPhysicalModelValues _begin;
        private AnimatedPhysicalModelValues _end;

        private AnimatedPhysicalModel CurrentWidget => (AnimatedPhysicalModel)StateWidget;

        public override void InitState()
        {
            _begin = _end = ValuesFromWidget();
            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            AnimatedPhysicalModelValues current = Evaluate(_controller.Evaluate());
            AnimatedPhysicalModelValues target = ValuesFromWidget();
            if (target != _end)
            {
                _begin = current;
                _end = target;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            AnimatedPhysicalModelValues values = Evaluate(_controller!.Evaluate());
            return new PhysicalModel(
                shape: CurrentWidget.Shape,
                clipBehavior: CurrentWidget.ClipBehavior,
                borderRadius: values.BorderRadius,
                elevation: values.Elevation,
                color: CurrentWidget.AnimateColor ? values.Color : CurrentWidget.Color,
                shadowColor: CurrentWidget.AnimateShadowColor ? values.ShadowColor : CurrentWidget.ShadowColor,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private AnimatedPhysicalModelValues ValuesFromWidget()
        {
            return new AnimatedPhysicalModelValues(
                CurrentWidget.BorderRadius ?? Plumix.Rendering.BorderRadius.Zero,
                CurrentWidget.Elevation,
                CurrentWidget.Color,
                CurrentWidget.ShadowColor);
        }

        private AnimatedPhysicalModelValues Evaluate(double t)
        {
            double radius = _begin.BorderRadius.Radius
                + ((_end.BorderRadius.Radius - _begin.BorderRadius.Radius) * t);
            double elevation = _begin.Elevation + ((_end.Elevation - _begin.Elevation) * t);
            return new AnimatedPhysicalModelValues(
                Plumix.Rendering.BorderRadius.Circular(radius),
                elevation,
                _colorTween.Evaluate(t, _begin.Color, _end.Color),
                _colorTween.Evaluate(t, _begin.ShadowColor, _end.ShadowColor));
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }
}

public sealed class AnimatedFractionallySizedBox : StatefulWidget
{
    public AnimatedFractionallySizedBox(
        TimeSpan duration,
        Widget? child = null,
        Alignment alignment = default,
        double? heightFactor = null,
        double? widthFactor = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Child = child;
        Alignment = alignment;
        HeightFactor = ValidateFactor(heightFactor, nameof(heightFactor));
        WidthFactor = ValidateFactor(widthFactor, nameof(widthFactor));
        Duration = duration;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public Widget? Child { get; }

    public Alignment Alignment { get; }

    public double? HeightFactor { get; }

    public double? WidthFactor { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedFractionallySizedBoxState();

    private sealed class AnimatedFractionallySizedBoxState : State
    {
        private AnimationController? _controller;
        private Alignment _alignmentBegin;
        private Alignment _alignmentEnd;
        private bool _hasHeightFactorTween;
        private double _heightFactorBegin;
        private double _heightFactorEnd;
        private bool _hasWidthFactorTween;
        private double _widthFactorBegin;
        private double _widthFactorEnd;

        private AnimatedFractionallySizedBox CurrentWidget =>
            (AnimatedFractionallySizedBox)StateWidget;

        public override void InitState()
        {
            _alignmentBegin = _alignmentEnd = CurrentWidget.Alignment;
            if (CurrentWidget.HeightFactor is double heightFactor)
            {
                _hasHeightFactorTween = true;
                _heightFactorBegin = _heightFactorEnd = heightFactor;
            }
            if (CurrentWidget.WidthFactor is double widthFactor)
            {
                _hasWidthFactorTween = true;
                _widthFactorBegin = _widthFactorEnd = widthFactor;
            }

            _controller = new AnimationController(CurrentWidget.Duration, this) { Curve = CurrentWidget.Curve };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            double t = _controller.Evaluate();
            Alignment currentAlignment = EvaluateAlignment(t);
            double? currentHeightFactor = EvaluateHeightFactor(t);
            double? currentWidthFactor = EvaluateWidthFactor(t);
            bool shouldStart = CurrentWidget.Alignment != _alignmentEnd;

            if (CurrentWidget.HeightFactor is double heightFactor)
            {
                if (_hasHeightFactorTween)
                {
                    shouldStart |= heightFactor != _heightFactorEnd;
                }
                else
                {
                    _hasHeightFactorTween = true;
                    _heightFactorBegin = _heightFactorEnd = heightFactor;
                    currentHeightFactor = heightFactor;
                }
            }
            if (CurrentWidget.WidthFactor is double widthFactor)
            {
                if (_hasWidthFactorTween)
                {
                    shouldStart |= widthFactor != _widthFactorEnd;
                }
                else
                {
                    _hasWidthFactorTween = true;
                    _widthFactorBegin = _widthFactorEnd = widthFactor;
                    currentWidthFactor = widthFactor;
                }
            }

            if (shouldStart)
            {
                _alignmentBegin = currentAlignment;
                _alignmentEnd = CurrentWidget.Alignment;
                if (CurrentWidget.HeightFactor is double targetHeightFactor)
                {
                    _heightFactorBegin = currentHeightFactor ?? targetHeightFactor;
                    _heightFactorEnd = targetHeightFactor;
                }
                if (CurrentWidget.WidthFactor is double targetWidthFactor)
                {
                    _widthFactorBegin = currentWidthFactor ?? targetWidthFactor;
                    _widthFactorEnd = targetWidthFactor;
                }

                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            double t = _controller!.Evaluate();
            return new FractionallySizedBox(
                alignment: EvaluateAlignment(t),
                heightFactor: EvaluateHeightFactor(t),
                widthFactor: EvaluateWidthFactor(t),
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private Alignment EvaluateAlignment(double t)
        {
            return new Alignment(
                LerpDouble(_alignmentBegin.X, _alignmentEnd.X, t),
                LerpDouble(_alignmentBegin.Y, _alignmentEnd.Y, t));
        }

        private double? EvaluateHeightFactor(double t)
        {
            return _hasHeightFactorTween
                ? LerpDouble(_heightFactorBegin, _heightFactorEnd, t)
                : null;
        }

        private double? EvaluateWidthFactor(double t)
        {
            return _hasWidthFactorTween
                ? LerpDouble(_widthFactorBegin, _widthFactorEnd, t)
                : null;
        }

        private void HandleChanged() => SetState(() => { });

        private void HandleCompleted()
        {
            SetState(() => { });
            CurrentWidget.OnEnd?.Invoke();
        }
    }

    private static double? ValidateFactor(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }
        if (!double.IsFinite(value.Value) || value.Value < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Factor must be finite and non-negative.");
        }

        return value;
    }

    private static double LerpDouble(double from, double to, double t) => from + ((to - from) * t);
}

internal readonly record struct AnimatedPhysicalModelValues(
    BorderRadius BorderRadius,
    double Elevation,
    Color Color,
    Color ShadowColor);

internal sealed record AnimatedPositionedValues(
    double? Left,
    double? Top,
    double? Right,
    double? Bottom,
    double? Width,
    double? Height)
{
    public static AnimatedPositionedValues From(AnimatedPositioned widget)
    {
        return new AnimatedPositionedValues(
            widget.Left,
            widget.Top,
            widget.Right,
            widget.Bottom,
            widget.Width,
            widget.Height);
    }

    public static AnimatedPositionedValues From(AnimatedPositionedDirectional widget)
    {
        return new AnimatedPositionedValues(
            widget.Start,
            widget.Top,
            widget.End,
            widget.Bottom,
            widget.Width,
            widget.Height);
    }

    public static bool HasAnimatedChange(AnimatedPositionedValues previous, AnimatedPositionedValues target)
    {
        return IsAnimatedChange(previous.Left, target.Left)
               || IsAnimatedChange(previous.Top, target.Top)
               || IsAnimatedChange(previous.Right, target.Right)
               || IsAnimatedChange(previous.Bottom, target.Bottom)
               || IsAnimatedChange(previous.Width, target.Width)
               || IsAnimatedChange(previous.Height, target.Height);
    }

    public static AnimatedPositionedValues TweenBegins(
        AnimatedPositionedValues current,
        AnimatedPositionedValues previous,
        AnimatedPositionedValues target)
    {
        return new AnimatedPositionedValues(
            TweenBegin(current.Left, previous.Left, target.Left),
            TweenBegin(current.Top, previous.Top, target.Top),
            TweenBegin(current.Right, previous.Right, target.Right),
            TweenBegin(current.Bottom, previous.Bottom, target.Bottom),
            TweenBegin(current.Width, previous.Width, target.Width),
            TweenBegin(current.Height, previous.Height, target.Height));
    }

    public static AnimatedPositionedValues ApplyNullTransitions(
        AnimatedPositionedValues values,
        AnimatedPositionedValues previous,
        AnimatedPositionedValues target)
    {
        return new AnimatedPositionedValues(
            ApplyNullTransition(values.Left, previous.Left, target.Left),
            ApplyNullTransition(values.Top, previous.Top, target.Top),
            ApplyNullTransition(values.Right, previous.Right, target.Right),
            ApplyNullTransition(values.Bottom, previous.Bottom, target.Bottom),
            ApplyNullTransition(values.Width, previous.Width, target.Width),
            ApplyNullTransition(values.Height, previous.Height, target.Height));
    }

    public static AnimatedPositionedValues Lerp(
        AnimatedPositionedValues begin,
        AnimatedPositionedValues end,
        double t)
    {
        return new AnimatedPositionedValues(
            LerpNullable(begin.Left, end.Left, t),
            LerpNullable(begin.Top, end.Top, t),
            LerpNullable(begin.Right, end.Right, t),
            LerpNullable(begin.Bottom, end.Bottom, t),
            LerpNullable(begin.Width, end.Width, t),
            LerpNullable(begin.Height, end.Height, t));
    }

    private static bool IsAnimatedChange(double? previous, double? target)
    {
        return previous.HasValue && target.HasValue && previous.Value != target.Value;
    }

    private static double? TweenBegin(double? current, double? previous, double? target)
    {
        if (!target.HasValue) return null;
        return previous.HasValue ? current : target;
    }

    private static double? ApplyNullTransition(double? value, double? previous, double? target)
    {
        if (!target.HasValue) return null;
        return previous.HasValue ? value : target;
    }

    private static double? LerpNullable(double? begin, double? end, double t)
    {
        if (!end.HasValue) return null;
        if (!begin.HasValue) return end;
        return begin.Value + ((end.Value - begin.Value) * t);
    }
}
