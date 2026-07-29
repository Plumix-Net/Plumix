using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/theme.dart

public sealed class ThemeDataTween : Tween<ThemeData>
{
    public ThemeDataTween(ThemeData? begin = null, ThemeData? end = null)
    {
        Begin = begin;
        End = end;
    }

    public override ThemeData Lerp(ThemeData a, ThemeData b, double t)
    {
        return ThemeData.Lerp(a, b, t);
    }
}

public sealed class Theme : InheritedTheme
{
    public Theme(
        ThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data;
        Child = child;
    }

    public ThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new DefaultTextStyle(
            style: Data.TextTheme.BodyMedium,
            child: Child);
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new Theme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((Theme)oldWidget).Data, Data);
    }

    public static ThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<Theme>()?.Data ?? ThemeData.Light;
    }
}

public sealed class AnimatedTheme : StatefulWidget
{
    public static TimeSpan DefaultDuration { get; } = TimeSpan.FromMilliseconds(200);

    public AnimatedTheme(
        ThemeData data,
        Widget child,
        TimeSpan? duration = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        TimeSpan effectiveDuration = duration ?? DefaultDuration;
        if (effectiveDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Duration = effectiveDuration;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public ThemeData Data { get; }

    public Widget Child { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState() => new AnimatedThemeState();

    private sealed class AnimatedThemeState : State
    {
        private AnimationController? _controller;
        private ThemeData _begin = null!;
        private ThemeData _end = null!;

        private AnimatedTheme CurrentWidget => (AnimatedTheme)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Data;
            _controller = new AnimationController(CurrentWidget.Duration)
            {
                Curve = CurrentWidget.Curve,
            };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            ThemeData current = ThemeData.Lerp(_begin, _end, _controller.Evaluate());
            if (!Equals(CurrentWidget.Data, _end))
            {
                _begin = current;
                _end = CurrentWidget.Data;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            ThemeData data = ThemeData.Lerp(_begin, _end, _controller!.Evaluate());
            return new Theme(data, CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _controller!.Changed -= HandleChanged;
            _controller.Completed -= HandleCompleted;
            _controller.Dispose();
            _controller = null;
        }

        private void HandleChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private void HandleCompleted()
        {
            if (Mounted)
            {
                SetState(() => { });
                CurrentWidget.OnEnd?.Invoke();
            }
        }
    }
}
