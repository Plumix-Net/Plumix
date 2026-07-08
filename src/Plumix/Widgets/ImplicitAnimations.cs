using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/implicit_animations.dart

public sealed class AnimatedContainer : StatefulWidget
{
    public AnimatedContainer(
        TimeSpan duration,
        Widget? child = null,
        Alignment? alignment = null,
        Thickness? padding = null,
        Color? color = null,
        BoxDecoration? decoration = null,
        BoxDecoration? foregroundDecoration = null,
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
    public BoxDecoration? Decoration { get; }
    public BoxDecoration? ForegroundDecoration { get; }
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
            _controller = new AnimationController(duration) { Curve = CurrentWidget.Curve };
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
                Decoration: BoxDecoration.Lerp(_begin.Decoration, _end.Decoration, t),
                ForegroundDecoration: BoxDecoration.Lerp(
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
        BoxDecoration? Decoration,
        BoxDecoration? ForegroundDecoration,
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
