using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/icon_theme_data.dart; flutter/packages/flutter/lib/src/widgets/icon_theme.dart (approximate)

public sealed record IconThemeData(
    Color? Color,
    double? Size,
    double? Opacity)
{
    public IconThemeData(Color? Color = null, double? Size = null) : this(Color, Size, null)
    {
    }

    public IconThemeData CopyWith(Color? color = null, double? size = null, double? opacity = null)
    {
        return new IconThemeData(color ?? Color, size ?? Size, opacity ?? Opacity);
    }

    public IconThemeData Merge(IconThemeData? other)
    {
        return other is null
            ? this
            : CopyWith(
                color: other.Color,
                size: other.Size,
                opacity: other.Opacity);
    }

    public static IconThemeData Lerp(IconThemeData? a, IconThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new IconThemeData(
            Color: LerpColor(a?.Color, b?.Color, clampedT),
            Size: LerpDouble(a?.Size, b?.Size, clampedT),
            Opacity: LerpDouble(a?.Opacity, b?.Opacity, clampedT));
    }

    public void Deconstruct(out Color? color, out double? size)
    {
        color = Color;
        size = Size;
    }

    internal static IconThemeData Fallback { get; } = new();

    private static double? LerpDouble(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        double from = a ?? 0.0;
        double to = b ?? 0.0;
        return from + ((to - from) * t);
    }

    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        Color from = a ?? Avalonia.Media.Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        Color to = b ?? Avalonia.Media.Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return new ColorTween().Evaluate(t, from, to);
    }
}

public sealed class IconTheme : InheritedTheme
{
    public IconTheme(
        IconThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data;
        Child = child;
    }

    public IconThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new IconTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((IconTheme)oldWidget).Data, Data);
    }

    public static IconThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<IconTheme>()?.Data ?? IconThemeData.Fallback;
    }

    public static Widget Merge(
        IconThemeData data,
        Widget child,
        Key? key = null)
    {
        return new Builder(context => new IconTheme(
            key: key,
            data: Of(context).Merge(data),
            child: child));
    }
}

public sealed class AnimatedIconTheme : StatefulWidget
{
    public AnimatedIconTheme(
        IconThemeData data,
        Widget child,
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Duration = duration;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    public IconThemeData Data { get; }

    public Widget Child { get; }

    public TimeSpan Duration { get; }

    public Curve Curve { get; }

    public Action? OnEnd { get; }

    public override State CreateState()
    {
        return new AnimatedIconThemeState();
    }

    private sealed class AnimatedIconThemeState : State
    {
        private AnimationController? _controller;
        private IconThemeData _begin = null!;
        private IconThemeData _end = null!;

        private AnimatedIconTheme CurrentWidget => (AnimatedIconTheme)StateWidget;

        public override void InitState()
        {
            _begin = _end = CurrentWidget.Data;
            _controller = new AnimationController(CurrentWidget.Duration, this)
            {
                Curve = CurrentWidget.Curve
            };
            _controller.Changed += HandleChanged;
            _controller.Completed += HandleCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _controller!.Duration = CurrentWidget.Duration;
            _controller.Curve = CurrentWidget.Curve;
            IconThemeData current = IconThemeData.Lerp(_begin, _end, _controller.Evaluate());
            if (!Equals(CurrentWidget.Data, _end))
            {
                _begin = current;
                _end = CurrentWidget.Data;
                _controller.Forward(from: 0.0);
            }
        }

        public override Widget Build(BuildContext context)
        {
            IconThemeData data = IconThemeData.Lerp(_begin, _end, _controller!.Evaluate());
            return new IconTheme(data, CurrentWidget.Child);
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
