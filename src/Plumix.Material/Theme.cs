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

public sealed class Theme : StatelessWidget
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

    public static ThemeData Of(BuildContext context)
    {
        InheritedMaterialTheme? inheritedTheme = context.DependOnInherited<InheritedMaterialTheme>();
        InheritedCupertinoTheme? inheritedCupertinoTheme =
            context.DependOnInherited<InheritedCupertinoTheme>();
        ThemeData data = inheritedTheme?.Theme.Data
                         ?? (inheritedCupertinoTheme is not null
                             ? new CupertinoBasedMaterialThemeData(
                                 inheritedCupertinoTheme.Theme.Data).MaterialTheme
                             : KFallbackTheme);
        return Localize(data, context);
    }

    // Dart's `Theme._inheritedCupertinoThemeData`.
    private CupertinoThemeData InheritedCupertinoThemeData(BuildContext context)
    {
        InheritedCupertinoTheme? inheritedTheme = context.DependOnInherited<InheritedCupertinoTheme>();
        return (inheritedTheme?.Theme.Data ?? new MaterialBasedCupertinoThemeData(Data))
            .ResolveFrom(context);
    }

    /// <summary>
    /// Dart's <c>Theme.brightnessOf</c>: the brightness descendant Material widgets should use,
    /// falling back to <see cref="MediaQuery.PlatformBrightnessOf"/> when no ancestor theme
    /// declares one.
    /// </summary>
    public static Brightness BrightnessOf(BuildContext context)
    {
        InheritedMaterialTheme? inheritedTheme = context.DependOnInherited<InheritedMaterialTheme>();
        return inheritedTheme?.Theme.Data.Brightness ?? ToBrightness(MediaQuery.PlatformBrightnessOf(context));
    }

    /// <summary>
    /// Dart's <c>Theme.maybeBrightnessOf</c>: like <see cref="BrightnessOf"/>, but null when
    /// neither an ancestor theme nor a <see cref="MediaQuery"/> declares a brightness.
    /// </summary>
    public static Brightness? MaybeBrightnessOf(BuildContext context)
    {
        InheritedMaterialTheme? inheritedTheme = context.DependOnInherited<InheritedMaterialTheme>();
        PlatformBrightness? platformBrightness = MediaQuery.MaybePlatformBrightnessOf(context);
        return inheritedTheme?.Theme.Data.Brightness
               ?? (platformBrightness is null ? null : ToBrightness(platformBrightness.Value));
    }

    // Dart has one `Brightness`; Plumix splits the platform value out as `PlatformBrightness`, so
    // the `MediaQuery` fallback has to be mapped back onto the Material enum.
    private static Brightness ToBrightness(PlatformBrightness brightness) =>
        brightness == PlatformBrightness.Dark ? Brightness.Dark : Brightness.Light;

    // Dart's `Theme._wrapsWidgetThemes`: the inherited themes in the widgets library cannot infer
    // their values from a Material `Theme`, so the subtree is wrapped in the widget-library themes
    // that carry them. The text style is not among them - `Material` installs it.
    private Widget WrapsWidgetThemes(BuildContext context, Widget child)
    {
        DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
        return new IconTheme(
            data: Data.IconTheme,
            child: new DefaultSelectionStyle(
                selectionColor: Data.TextSelectionTheme.SelectionColor ?? selectionStyle.SelectionColor,
                cursorColor: Data.TextSelectionTheme.CursorColor ?? selectionStyle.CursorColor,
                child: child));
    }

    public override Widget Build(BuildContext context)
    {
        return new InheritedMaterialTheme(
            theme: this,
            child: new CupertinoTheme(
                // If a CupertinoThemeData doesn't exist, we're using a MaterialBasedCupertinoThemeData
                // here instead of a CupertinoThemeData because it defers some properties to the
                // Material ThemeData.
                data: InheritedCupertinoThemeData(context),
                child: WrapsWidgetThemes(context, Child)));
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<ThemeData>("data", Data, showName: false));
    }

    private static ThemeData Localize(ThemeData data, BuildContext context)
    {
        ScriptCategory category = MaterialLocalizations.Of(context).ScriptCategory;
        return ThemeData.Localize(data, data.Typography.GeometryThemeFor(category));
    }
}

/// <summary>Provides a <see cref="Theme"/> to the widgets below it. Dart's <c>_InheritedTheme</c>.</summary>
public sealed class InheritedMaterialTheme : InheritedTheme
{
    public InheritedMaterialTheme(Theme theme, Widget child, Key? key = null) : base(child, key)
    {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    public Theme Theme { get; }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new Theme(Theme.Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((InheritedMaterialTheme)oldWidget).Theme.Data, Theme.Data);
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

            base.Dispose();
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
