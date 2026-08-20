using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/icon_theme_data.dart;
// flutter/packages/flutter/lib/src/widgets/icon_theme.dart

/// <summary>Defines the size, font variations, color, opacity, and shadows of icons.</summary>
public class IconThemeData : Diagnosticable, IEquatable<IconThemeData>
{
    private readonly double? _opacity;

    public IconThemeData(
        Color? Color = null,
        double? Size = null,
        double? Opacity = null,
        double? Fill = null,
        double? Weight = null,
        double? Grade = null,
        double? OpticalSize = null,
        IReadOnlyList<Shadow>? Shadows = null,
        bool? ApplyTextScaling = null)
    {
        if (Fill.HasValue && !(Fill.Value >= 0.0 && Fill.Value <= 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(Fill), "Icon fill must be between 0 and 1.");
        }

        if (Weight.HasValue && !(Weight.Value > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(Weight), "Icon weight must be positive.");
        }

        if (OpticalSize.HasValue && !(OpticalSize.Value > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(OpticalSize), "Icon optical size must be positive.");
        }

        this.Size = Size;
        this.Fill = Fill;
        this.Weight = Weight;
        this.Grade = Grade;
        this.OpticalSize = OpticalSize;
        this.Color = Color;
        _opacity = Opacity;
        this.Shadows = Shadows;
        this.ApplyTextScaling = ApplyTextScaling;
    }

    /// <summary>An icon theme with Flutter's concrete fallback values.</summary>
    public static IconThemeData Fallback { get; } = new(
        Color: Avalonia.Media.Colors.Black,
        Size: 24.0,
        Opacity: 1.0,
        Fill: 0.0,
        Weight: 400.0,
        Grade: 0.0,
        OpticalSize: 48.0,
        ApplyTextScaling: false);

    public double? Size { get; }

    public double? Fill { get; }

    public double? Weight { get; }

    public double? Grade { get; }

    public double? OpticalSize { get; }

    public Color? Color { get; }

    public double? Opacity => _opacity is double opacity
        ? double.IsNaN(opacity) ? 1.0 : Math.Clamp(opacity, 0.0, 1.0)
        : null;

    public IReadOnlyList<Shadow>? Shadows { get; }

    public bool? ApplyTextScaling { get; }

    public bool IsConcrete =>
        Size.HasValue
        && Fill.HasValue
        && Weight.HasValue
        && Grade.HasValue
        && OpticalSize.HasValue
        && Color.HasValue
        && Opacity.HasValue
        && ApplyTextScaling.HasValue;

    public virtual IconThemeData CopyWith(
        Color? color = null,
        double? size = null,
        double? opacity = null,
        double? fill = null,
        double? weight = null,
        double? grade = null,
        double? opticalSize = null,
        IReadOnlyList<Shadow>? shadows = null,
        bool? applyTextScaling = null)
    {
        return new IconThemeData(
            Color: color ?? Color,
            Size: size ?? Size,
            Opacity: opacity ?? Opacity,
            Fill: fill ?? Fill,
            Weight: weight ?? Weight,
            Grade: grade ?? Grade,
            OpticalSize: opticalSize ?? OpticalSize,
            Shadows: shadows ?? Shadows,
            ApplyTextScaling: applyTextScaling ?? ApplyTextScaling);
    }

    public IconThemeData Merge(IconThemeData? other)
    {
        return other is null
            ? this
            : CopyWith(
                color: other.Color,
                size: other.Size,
                opacity: other.Opacity,
                fill: other.Fill,
                weight: other.Weight,
                grade: other.Grade,
                opticalSize: other.OpticalSize,
                shadows: other.Shadows,
                applyTextScaling: other.ApplyTextScaling);
    }

    /// <summary>Resolves context-dependent values after the theme is retrieved.</summary>
    public virtual IconThemeData Resolve(BuildContext context) => this;

    public static IconThemeData Lerp(IconThemeData? a, IconThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new IconThemeData(
            Color: LerpColor(a?.Color, b?.Color, t),
            Size: LerpDouble(a?.Size, b?.Size, t),
            Opacity: LerpDouble(a?.Opacity, b?.Opacity, t),
            Fill: LerpDouble(a?.Fill, b?.Fill, t),
            Weight: LerpDouble(a?.Weight, b?.Weight, t),
            Grade: LerpDouble(a?.Grade, b?.Grade, t),
            OpticalSize: LerpDouble(a?.OpticalSize, b?.OpticalSize, t),
            Shadows: Shadow.LerpList(a?.Shadows, b?.Shadows, t),
            ApplyTextScaling: t < 0.5 ? a?.ApplyTextScaling : b?.ApplyTextScaling);
    }

    public void Deconstruct(out Color? color, out double? size)
    {
        color = Color;
        size = Size;
    }

    public void Deconstruct(out Color? color, out double? size, out double? opacity)
    {
        color = Color;
        size = Size;
        opacity = Opacity;
    }

    public virtual bool Equals(IconThemeData? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && other.Size == Size
               && other.Fill == Fill
               && other.Weight == Weight
               && other.Grade == Grade
               && other.OpticalSize == OpticalSize
               && other.Color == Color
               && other.Opacity == Opacity
               && ShadowListsEqual(other.Shadows, Shadows)
               && other.ApplyTextScaling == ApplyTextScaling;
    }

    public override bool Equals(object? obj) => Equals(obj as IconThemeData);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GetType());
        hash.Add(Size);
        hash.Add(Fill);
        hash.Add(Weight);
        hash.Add(Grade);
        hash.Add(OpticalSize);
        hash.Add(Color);
        hash.Add(Opacity);
        if (Shadows is not null)
        {
            foreach (Shadow shadow in Shadows)
            {
                hash.Add(shadow);
            }
        }

        hash.Add(ApplyTextScaling);
        return hash.ToHashCode();
    }

    public static bool operator ==(IconThemeData? left, IconThemeData? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(IconThemeData? left, IconThemeData? right) => !(left == right);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new DoubleProperty("size", Size, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("fill", Fill, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("weight", Weight, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("grade", Grade, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("opticalSize", OpticalSize, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Color?>("color", Color, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("opacity", Opacity, defaultValue: nullDefault));
        properties.Add(new IterableProperty<Shadow>("shadows", Shadows, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>(
            "applyTextScaling",
            ApplyTextScaling,
            defaultValue: nullDefault));
    }

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

        if (!a.HasValue)
        {
            Color value = b!.Value;
            return Avalonia.Media.Color.FromArgb(ScaleAlpha(value.A, t), value.R, value.G, value.B);
        }

        if (!b.HasValue)
        {
            Color value = a.Value;
            return Avalonia.Media.Color.FromArgb(ScaleAlpha(value.A, 1.0 - t), value.R, value.G, value.B);
        }

        return new ColorTween().Evaluate(t, a.Value, b.Value);
    }

    private static byte ScaleAlpha(byte alpha, double factor)
    {
        return (byte)Math.Clamp((int)Math.Round(alpha * factor, MidpointRounding.AwayFromZero), 0, 255);
    }

    private static bool ShadowListsEqual(IReadOnlyList<Shadow>? a, IReadOnlyList<Shadow>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        return a is not null && b is not null && a.SequenceEqual(b);
    }
}

public sealed class IconTheme : InheritedTheme
{
    public IconTheme(
        IconThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
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
        IconThemeData iconThemeData = GetInheritedIconThemeData(context).Resolve(context);
        return iconThemeData.IsConcrete
            ? iconThemeData
            : iconThemeData.CopyWith(
                size: iconThemeData.Size ?? IconThemeData.Fallback.Size,
                fill: iconThemeData.Fill ?? IconThemeData.Fallback.Fill,
                weight: iconThemeData.Weight ?? IconThemeData.Fallback.Weight,
                grade: iconThemeData.Grade ?? IconThemeData.Fallback.Grade,
                opticalSize: iconThemeData.OpticalSize ?? IconThemeData.Fallback.OpticalSize,
                color: iconThemeData.Color ?? IconThemeData.Fallback.Color,
                opacity: iconThemeData.Opacity ?? IconThemeData.Fallback.Opacity,
                shadows: iconThemeData.Shadows ?? IconThemeData.Fallback.Shadows,
                applyTextScaling: iconThemeData.ApplyTextScaling ?? IconThemeData.Fallback.ApplyTextScaling);
    }

    public static Widget Merge(
        IconThemeData data,
        Widget child,
        Key? key = null)
    {
        return new Builder(context => new IconTheme(
            key: key,
            data: GetInheritedIconThemeData(context).Merge(data),
            child: child));
    }

    private static IconThemeData GetInheritedIconThemeData(BuildContext context)
    {
        return context.DependOnInherited<IconTheme>()?.Data ?? IconThemeData.Fallback;
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
            _controller = new AnimationController(duration: CurrentWidget.Duration, vsync: this)
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
