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
        Action? onDismiss = null,
        bool liveRegion = false,
        bool container = false,
        bool explicitChildNodes = false,
        Key? key = null) : base(child, key)
    {
        Label = label;
        Flags = flags;
        OnTap = onTap;
        OnDismiss = onDismiss;
        LiveRegion = liveRegion;
        Container = container;
        ExplicitChildNodes = explicitChildNodes;
    }

    public string? Label { get; }

    public SemanticsFlags Flags { get; }

    public Action? OnTap { get; }

    public Action? OnDismiss { get; }

    public bool LiveRegion { get; }

    public bool Container { get; }

    public bool ExplicitChildNodes { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSemanticsAnnotations(
            label: Label,
            flags: Flags,
            onTap: OnTap,
            onDismiss: OnDismiss,
            liveRegion: LiveRegion,
            container: Container,
            explicitChildNodes: ExplicitChildNodes);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderSemanticsAnnotations)renderObject;
        semantics.Label = Label;
        semantics.Flags = Flags;
        semantics.OnTap = OnTap;
        semantics.OnDismiss = OnDismiss;
        semantics.LiveRegion = LiveRegion;
        semantics.Container = Container;
        semantics.ExplicitChildNodes = ExplicitChildNodes;
    }
}

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (MergeSemantics)
public sealed class MergeSemantics : StatelessWidget
{
    public MergeSemantics(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new Semantics(
            child: Child,
            container: true);
    }
}
