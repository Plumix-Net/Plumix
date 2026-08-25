using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/theme.dart

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
    // Dart's `Theme._kFallbackTheme`: `ThemeData.fallback()`, which is `ThemeData.light()`.
    private static readonly ThemeData KFallbackTheme = ThemeData.Light;

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
        ThemeData localized = Localize(Data, context);
        return new CupertinoTheme(
            // If a CupertinoThemeData doesn't exist, we're using a MaterialBasedCupertinoThemeData
            // here instead of a CupertinoThemeData because it defers some properties to the
            // Material ThemeData.
            data: InheritedCupertinoThemeData(context, localized),
            child: WrapsWidgetThemes(context, localized, Child));
    }

    // Dart's `Theme._inheritedCupertinoThemeData`.
    private static CupertinoThemeData InheritedCupertinoThemeData(BuildContext context, ThemeData data)
    {
        InheritedCupertinoTheme? inheritedTheme = context.DependOnInherited<InheritedCupertinoTheme>();
        return (inheritedTheme?.Theme.Data ?? new MaterialBasedCupertinoThemeData(data))
            .ResolveFrom(context);
    }

    // Dart's `Theme._wrapsWidgetThemes`: the inherited themes in the widgets library cannot infer
    // their values from a Material `Theme`, so the subtree is wrapped in the widget-library themes
    // that carry them. `DefaultTextStyle` is Plumix's own addition (see `DIVERGENCES.md`).
    private static Widget WrapsWidgetThemes(BuildContext context, ThemeData data, Widget child)
    {
        DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
        return new IconTheme(
            data: data.IconTheme,
            child: new DefaultSelectionStyle(
                selectionColor: data.TextSelectionTheme.SelectionColor ?? selectionStyle.SelectionColor,
                cursorColor: data.TextSelectionTheme.CursorColor ?? selectionStyle.CursorColor,
                child: new DefaultTextStyle(
                    style: data.TextTheme.BodyMedium,
                    child: child)));
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
        Theme? inheritedTheme = context.DependOnInherited<Theme>();
        InheritedCupertinoTheme? inheritedCupertinoTheme =
            context.DependOnInherited<InheritedCupertinoTheme>();
        ThemeData data = inheritedTheme?.Data
                         ?? (inheritedCupertinoTheme is not null
                             ? new CupertinoBasedMaterialThemeData(
                                 inheritedCupertinoTheme.Theme.Data).MaterialTheme
                             : KFallbackTheme);
        return Localize(data, context);
    }

    private static ThemeData Localize(ThemeData data, BuildContext context)
    {
        ScriptCategory category = MaterialLocalizations.Of(context).ScriptCategory;
        return ThemeData.Localize(data, data.Typography.GeometryThemeFor(category));
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
            _controller = new AnimationController(duration: CurrentWidget.Duration, vsync: this)
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
