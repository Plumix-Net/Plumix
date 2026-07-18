using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/visibility.dart

public sealed class Visibility : StatelessWidget
{
    public Visibility(
        Widget child,
        Widget? replacement = null,
        bool visible = true,
        bool maintainState = false,
        bool maintainAnimation = false,
        bool maintainSize = false,
        bool maintainSemantics = false,
        bool maintainInteractivity = false,
        bool maintainFocusability = false,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        ValidateMaintainedFeatures(
            maintainState,
            maintainAnimation,
            maintainSize,
            maintainSemantics,
            maintainInteractivity,
            maintainFocusability);

        Child = child;
        Replacement = replacement ?? new SizedBox(width: 0, height: 0);
        Visible = visible;
        MaintainState = maintainState;
        MaintainAnimation = maintainAnimation;
        MaintainSize = maintainSize;
        MaintainSemantics = maintainSemantics;
        MaintainInteractivity = maintainInteractivity;
        MaintainFocusability = maintainFocusability;
    }

    private Visibility(Widget child, bool visible, Key? key) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        Child = child;
        Replacement = new SizedBox(width: 0, height: 0);
        Visible = visible;
        MaintainState = true;
        MaintainAnimation = true;
        MaintainSize = true;
        MaintainSemantics = true;
        MaintainInteractivity = true;
        MaintainFocusability = true;
    }

    public Widget Child { get; }

    public Widget Replacement { get; }

    public bool Visible { get; }

    public bool MaintainState { get; }

    public bool MaintainAnimation { get; }

    public bool MaintainSize { get; }

    public bool MaintainSemantics { get; }

    public bool MaintainInteractivity { get; }

    public bool MaintainFocusability { get; }

    public static Visibility Maintain(Widget child, bool visible = true, Key? key = null)
    {
        return new Visibility(child, visible, key);
    }

    public static bool Of(BuildContext context)
    {
        return context.DependOnInheritedAncestors<VisibilityScope>().All(scope => scope.IsVisible);
    }

    public override Widget Build(BuildContext context)
    {
        Widget result = new ExcludeFocus(
            excluding: !Visible && !MaintainFocusability,
            child: Child);

        if (MaintainSize)
        {
            result = new VisibilityRenderWidget(
                visible: Visible,
                maintainSemantics: MaintainSemantics,
                child: new IgnorePointer(
                    ignoring: !Visible && !MaintainInteractivity,
                    child: result));
        }
        else if (MaintainState)
        {
            if (!MaintainAnimation)
            {
                result = new TickerMode(enabled: Visible, child: result);
            }

            result = new Offstage(offstage: !Visible, child: result);
        }
        else
        {
            result = Visible ? Child : Replacement;
        }

        return new VisibilityScope(Visible, result);
    }

    private static void ValidateMaintainedFeatures(
        bool maintainState,
        bool maintainAnimation,
        bool maintainSize,
        bool maintainSemantics,
        bool maintainInteractivity,
        bool maintainFocusability)
    {
        if (maintainAnimation && !maintainState)
        {
            throw new ArgumentException("Cannot maintain animations if the state is not also maintained.");
        }

        if (maintainSize && !maintainAnimation)
        {
            throw new ArgumentException("Cannot maintain size if animations are not maintained.");
        }

        if (maintainSemantics && !maintainSize)
        {
            throw new ArgumentException("Cannot maintain semantics if size is not maintained.");
        }

        if (maintainInteractivity && !maintainSize)
        {
            throw new ArgumentException("Cannot maintain interactivity if size is not maintained.");
        }

        if (maintainFocusability && !maintainState)
        {
            throw new ArgumentException("Cannot maintain focusability if the state is not also maintained.");
        }
    }
}

public sealed class SliverVisibility : StatelessWidget
{
    public SliverVisibility(
        Widget sliver,
        Widget? replacementSliver = null,
        bool visible = true,
        bool maintainState = false,
        bool maintainAnimation = false,
        bool maintainSize = false,
        bool maintainSemantics = false,
        bool maintainInteractivity = false,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(sliver);
        ValidateMaintainedFeatures(
            maintainState,
            maintainAnimation,
            maintainSize,
            maintainSemantics,
            maintainInteractivity);

        Sliver = sliver;
        ReplacementSliver = replacementSliver ?? new SliverToBoxAdapter();
        Visible = visible;
        MaintainState = maintainState;
        MaintainAnimation = maintainAnimation;
        MaintainSize = maintainSize;
        MaintainSemantics = maintainSemantics;
        MaintainInteractivity = maintainInteractivity;
    }

    private SliverVisibility(Widget sliver, Widget? replacementSliver, bool visible, Key? key) : base(key)
    {
        ArgumentNullException.ThrowIfNull(sliver);
        Sliver = sliver;
        ReplacementSliver = replacementSliver ?? new SliverToBoxAdapter();
        Visible = visible;
        MaintainState = true;
        MaintainAnimation = true;
        MaintainSize = true;
        MaintainSemantics = true;
        MaintainInteractivity = true;
    }

    public Widget Sliver { get; }

    public Widget ReplacementSliver { get; }

    public bool Visible { get; }

    public bool MaintainState { get; }

    public bool MaintainAnimation { get; }

    public bool MaintainSize { get; }

    public bool MaintainSemantics { get; }

    public bool MaintainInteractivity { get; }

    public static SliverVisibility Maintain(
        Widget sliver,
        Widget? replacementSliver = null,
        bool visible = true,
        Key? key = null)
    {
        return new SliverVisibility(sliver, replacementSliver, visible, key);
    }

    public override Widget Build(BuildContext context)
    {
        if (MaintainSize)
        {
            return new SliverVisibilityRenderWidget(
                visible: Visible,
                maintainSemantics: MaintainSemantics,
                sliver: new SliverIgnorePointer(
                    ignoring: !Visible && !MaintainInteractivity,
                    sliver: Sliver));
        }

        if (MaintainState)
        {
            Widget result = Sliver;
            if (!MaintainAnimation)
            {
                result = new TickerMode(enabled: Visible, child: Sliver);
            }

            return new SliverOffstage(offstage: !Visible, sliver: result);
        }

        return Visible ? Sliver : ReplacementSliver;
    }

    private static void ValidateMaintainedFeatures(
        bool maintainState,
        bool maintainAnimation,
        bool maintainSize,
        bool maintainSemantics,
        bool maintainInteractivity)
    {
        if (maintainAnimation && !maintainState)
        {
            throw new ArgumentException("Cannot maintain animations if the state is not also maintained.");
        }

        if (maintainSize && !maintainAnimation)
        {
            throw new ArgumentException("Cannot maintain size if animations are not maintained.");
        }

        if (maintainSemantics && !maintainSize)
        {
            throw new ArgumentException("Cannot maintain semantics if size is not maintained.");
        }

        if (maintainInteractivity && !maintainSize)
        {
            throw new ArgumentException("Cannot maintain interactivity if size is not maintained.");
        }
    }
}

internal sealed class VisibilityScope : InheritedWidget
{
    public VisibilityScope(bool isVisible, Widget child) : base(key: null)
    {
        IsVisible = isVisible;
        Child = child;
    }

    public bool IsVisible { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return ((VisibilityScope)oldWidget).IsVisible != IsVisible;
    }
}

internal sealed class VisibilityRenderWidget : SingleChildRenderObjectWidget
{
    public VisibilityRenderWidget(
        bool visible,
        bool maintainSemantics,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Visible = visible;
        MaintainSemantics = maintainSemantics;
    }

    public bool Visible { get; }

    public bool MaintainSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderVisibility(Visible, MaintainSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var visibility = (RenderVisibility)renderObject;
        visibility.Visible = Visible;
        visibility.MaintainSemantics = MaintainSemantics;
    }
}

internal sealed class SliverVisibilityRenderWidget : SingleChildRenderObjectWidget
{
    public SliverVisibilityRenderWidget(
        bool visible,
        bool maintainSemantics,
        Widget? sliver = null,
        Key? key = null) : base(sliver, key)
    {
        Visible = visible;
        MaintainSemantics = maintainSemantics;
    }

    public bool Visible { get; }

    public bool MaintainSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverVisibility(Visible, MaintainSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var visibility = (RenderSliverVisibility)renderObject;
        visibility.Visible = Visible;
        visibility.MaintainSemantics = MaintainSemantics;
    }
}
