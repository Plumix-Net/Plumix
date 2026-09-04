using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_fill.dart

public sealed class SliverFillViewport : StatelessWidget
{
    public SliverFillViewport(
        SliverChildDelegate @delegate,
        double viewportFraction = 1.0,
        bool padEnds = true,
        bool allowImplicitScrolling = true,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(@delegate);
        if (!double.IsFinite(viewportFraction) || viewportFraction <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportFraction),
                "viewportFraction must be positive and finite.");
        }

        Delegate = @delegate;
        ViewportFraction = viewportFraction;
        PadEnds = padEnds;
        AllowImplicitScrolling = allowImplicitScrolling;
    }

    public SliverChildDelegate Delegate { get; }

    public double ViewportFraction { get; }

    public bool PadEnds { get; }

    public bool AllowImplicitScrolling { get; }

    public override Widget Build(BuildContext context)
    {
        double paddingFraction = PadEnds
            ? Math.Clamp(1.0 - ViewportFraction, 0.0, 1.0) / 2.0
            : 0.0;
        return new SliverFractionalPadding(
            viewportFraction: paddingFraction,
            sliver: new SliverFillViewportRenderObjectWidget(
                Delegate,
                viewportFraction: ViewportFraction,
                allowImplicitScrolling: AllowImplicitScrolling));
    }
}

internal sealed class SliverFillViewportRenderObjectWidget : SliverMultiBoxAdaptorWidget
{
    public SliverFillViewportRenderObjectWidget(
        SliverChildDelegate @delegate,
        double viewportFraction = 1.0,
        bool allowImplicitScrolling = true,
        Key? key = null) : base(@delegate, key)
    {
        ViewportFraction = viewportFraction;
        AllowImplicitScrolling = allowImplicitScrolling;
    }

    public double ViewportFraction { get; }

    public bool AllowImplicitScrolling { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFillViewport(
            viewportFraction: ViewportFraction,
            allowImplicitScrolling: AllowImplicitScrolling);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var fillViewport = (RenderSliverFillViewport)renderObject;
        fillViewport.ViewportFraction = ViewportFraction;
        fillViewport.AllowImplicitScrolling = AllowImplicitScrolling;
    }
}

internal sealed class SliverFractionalPadding : SingleChildRenderObjectWidget
{
    public SliverFractionalPadding(
        double viewportFraction = 0.0,
        Widget? sliver = null,
        Key? key = null) : base(sliver, key)
    {
        ViewportFraction = viewportFraction;
    }

    public double ViewportFraction { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFractionalPadding(ViewportFraction);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverFractionalPadding)renderObject).ViewportFraction = ViewportFraction;
    }
}

public sealed class SliverFillRemaining : StatelessWidget
{
    public SliverFillRemaining(
        Widget? child = null,
        bool hasScrollBody = true,
        bool fillOverscroll = false,
        Key? key = null) : base(key)
    {
        Child = child;
        HasScrollBody = hasScrollBody;
        FillOverscroll = fillOverscroll;
    }

    public Widget? Child { get; }

    public bool HasScrollBody { get; }

    public bool FillOverscroll { get; }

    public override Widget Build(BuildContext context)
    {
        if (HasScrollBody)
        {
            return new SliverFillRemainingWithScrollable(Child);
        }

        return FillOverscroll
            ? new SliverFillRemainingAndOverscroll(Child)
            : new SliverFillRemainingWithoutScrollable(Child);
    }
}

internal sealed class SliverFillRemainingWithScrollable : SingleChildRenderObjectWidget
{
    public SliverFillRemainingWithScrollable(Widget? child = null) : base(child)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFillRemainingWithScrollable();
    }
}

internal sealed class SliverFillRemainingWithoutScrollable : SingleChildRenderObjectWidget
{
    public SliverFillRemainingWithoutScrollable(Widget? child = null) : base(child)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFillRemaining();
    }
}

internal sealed class SliverFillRemainingAndOverscroll : SingleChildRenderObjectWidget
{
    public SliverFillRemainingAndOverscroll(Widget? child = null) : base(child)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFillRemainingAndOverscroll();
    }
}
