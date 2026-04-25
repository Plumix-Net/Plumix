using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (Semantics subset)

namespace Plumix.Widgets;

public sealed class Semantics : SingleChildRenderObjectWidget
{
    public Semantics(
        Widget? child = null,
        string? label = null,
        SemanticsFlags flags = SemanticsFlags.None,
        Action? onTap = null,
        bool container = false,
        bool explicitChildNodes = false,
        Key? key = null) : base(child, key)
    {
        Label = label;
        Flags = flags;
        OnTap = onTap;
        Container = container;
        ExplicitChildNodes = explicitChildNodes;
    }

    public string? Label { get; }

    public SemanticsFlags Flags { get; }

    public Action? OnTap { get; }

    public bool Container { get; }

    public bool ExplicitChildNodes { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSemanticsAnnotations(
            label: Label,
            flags: Flags,
            onTap: OnTap,
            container: Container,
            explicitChildNodes: ExplicitChildNodes);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderSemanticsAnnotations)renderObject;
        semantics.Label = Label;
        semantics.Flags = Flags;
        semantics.OnTap = OnTap;
        semantics.Container = Container;
        semantics.ExplicitChildNodes = ExplicitChildNodes;
    }
}
