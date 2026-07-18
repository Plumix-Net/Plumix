using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/transitions.dart

public sealed class FadeTransition : StatefulWidget
{
    public FadeTransition(
        Animation<double> opacity,
        Widget? child = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(key)
    {
        Opacity = opacity ?? throw new ArgumentNullException(nameof(opacity));
        Child = child;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public Animation<double> Opacity { get; }

    public Widget? Child { get; }

    public bool AlwaysIncludeSemantics { get; }

    public override State CreateState() => new FadeTransitionState();

    private sealed class FadeTransitionState : State
    {
        private FadeTransition CurrentWidget => (FadeTransition)StateWidget;

        public override void InitState()
        {
            CurrentWidget.Opacity.AddListener(HandleChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldTransition = (FadeTransition)oldWidget;
            if (ReferenceEquals(oldTransition.Opacity, CurrentWidget.Opacity))
            {
                return;
            }

            oldTransition.Opacity.RemoveListener(HandleChanged);
            CurrentWidget.Opacity.AddListener(HandleChanged);
        }

        public override Widget Build(BuildContext context)
        {
            return new Opacity(
                opacity: Math.Clamp(CurrentWidget.Opacity.Value, 0.0, 1.0),
                child: CurrentWidget.Child,
                alwaysIncludeSemantics: CurrentWidget.AlwaysIncludeSemantics);
        }

        public override void Dispose()
        {
            CurrentWidget.Opacity.RemoveListener(HandleChanged);
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
