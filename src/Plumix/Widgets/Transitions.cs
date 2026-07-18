using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/transitions.dart

public abstract class AnimatedWidget : StatefulWidget
{
    protected AnimatedWidget(IListenable listenable, Key? key = null) : base(key)
    {
        Listenable = listenable ?? throw new ArgumentNullException(nameof(listenable));
    }

    public IListenable Listenable { get; }

    public abstract Widget Build(BuildContext context);

    public sealed override State CreateState() => new AnimatedWidgetState();

    private sealed class AnimatedWidgetState : State
    {
        private AnimatedWidget CurrentWidget => (AnimatedWidget)StateWidget;

        public override void InitState()
        {
            CurrentWidget.Listenable.AddListener(HandleChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldAnimatedWidget = (AnimatedWidget)oldWidget;
            if (ReferenceEquals(oldAnimatedWidget.Listenable, CurrentWidget.Listenable))
            {
                return;
            }

            oldAnimatedWidget.Listenable.RemoveListener(HandleChanged);
            CurrentWidget.Listenable.AddListener(HandleChanged);
        }

        public override Widget Build(BuildContext context) => CurrentWidget.Build(context);

        public override void Dispose()
        {
            CurrentWidget.Listenable.RemoveListener(HandleChanged);
        }

        private void HandleChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }
    }
}

public sealed class FadeTransition : AnimatedWidget
{
    public FadeTransition(
        Animation<double> opacity,
        Widget? child = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(opacity ?? throw new ArgumentNullException(nameof(opacity)), key)
    {
        Child = child;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public Animation<double> Opacity => (Animation<double>)Listenable;

    public Widget? Child { get; }

    public bool AlwaysIncludeSemantics { get; }

    public override Widget Build(BuildContext context)
    {
        return new Opacity(
            opacity: Math.Clamp(Opacity.Value, 0.0, 1.0),
            child: Child,
            alwaysIncludeSemantics: AlwaysIncludeSemantics);
    }
}

public delegate Matrix TransformCallback(double animationValue);

public class MatrixTransition : AnimatedWidget
{
    public MatrixTransition(
        Animation<double> animation,
        TransformCallback onTransform,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Widget? child = null,
        Key? key = null) : base(animation ?? throw new ArgumentNullException(nameof(animation)), key)
    {
        OnTransform = onTransform ?? throw new ArgumentNullException(nameof(onTransform));
        Alignment = alignment;
        FilterQuality = filterQuality;
        Child = child;
    }

    public TransformCallback OnTransform { get; }

    public Animation<double> Animation => (Animation<double>)Listenable;

    public Alignment Alignment { get; }

    public FilterQuality? FilterQuality { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new Transform(
            transform: OnTransform(Animation.Value),
            alignment: Alignment,
            filterQuality: Animation.Status.IsAnimating() ? FilterQuality : null,
            child: Child);
    }
}

public sealed class ScaleTransition : MatrixTransition
{
    public ScaleTransition(
        Animation<double> scale,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Widget? child = null,
        Key? key = null) : base(
            animation: scale,
            onTransform: HandleScaleMatrix,
            alignment: alignment,
            filterQuality: filterQuality,
            child: child,
            key: key)
    {
    }

    public Animation<double> Scale => Animation;

    private static Matrix HandleScaleMatrix(double value) => Matrix.CreateScale(value, value);
}

public sealed class RotationTransition : MatrixTransition
{
    public RotationTransition(
        Animation<double> turns,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Widget? child = null,
        Key? key = null) : base(
            animation: turns,
            onTransform: HandleTurnsMatrix,
            alignment: alignment,
            filterQuality: filterQuality,
            child: child,
            key: key)
    {
    }

    public Animation<double> Turns => Animation;

    private static Matrix HandleTurnsMatrix(double value)
    {
        double radians = value * Math.PI * 2.0;
        if (radians == 0.0)
        {
            return Matrix.Identity;
        }

        double sine = Math.Sin(radians);
        if (sine == 1.0)
        {
            return new Matrix(0, 1, -1, 0, 0, 0);
        }
        if (sine == -1.0)
        {
            return new Matrix(0, -1, 1, 0, 0, 0);
        }

        double cosine = Math.Cos(radians);
        if (cosine == -1.0)
        {
            return new Matrix(-1, 0, 0, -1, 0, 0);
        }

        return new Matrix(cosine, sine, -sine, cosine, 0, 0);
    }
}

public sealed class SliverFadeTransition : SingleChildRenderObjectWidget
{
    public SliverFadeTransition(
        Animation<double> opacity,
        Widget? sliver = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(sliver, key)
    {
        Opacity = opacity ?? throw new ArgumentNullException(nameof(opacity));
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public Animation<double> Opacity { get; }

    public bool AlwaysIncludeSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverAnimatedOpacity(
            opacity: Opacity,
            alwaysIncludeSemantics: AlwaysIncludeSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var opacity = (RenderSliverAnimatedOpacity)renderObject;
        opacity.Opacity = Opacity;
        opacity.AlwaysIncludeSemantics = AlwaysIncludeSemantics;
    }
}

// The inherited surface matches Flutter's composition. Ticker muting is intentionally kept
// in one shared primitive so descendant ticker registration can be wired without changing callers.
public sealed class TickerMode : InheritedWidget
{
    public TickerMode(
        Widget child,
        bool enabled = true,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Enabled = enabled;
    }

    public Widget Child { get; }

    public bool Enabled { get; }

    public override Widget Build(BuildContext context) => Child;

    public static bool Of(BuildContext context)
    {
        return context.DependOnInherited<TickerMode>()?.Enabled ?? true;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return ((TickerMode)oldWidget).Enabled != Enabled;
    }
}
