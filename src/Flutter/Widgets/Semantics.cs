using Flutter.Foundation;
using Flutter.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (Semantics subset)

namespace Flutter.Widgets;

public sealed class Semantics : SingleChildRenderObjectWidget
{
    public Semantics(
        Widget? child = null,
        string? label = null,
        SemanticsFlags flags = SemanticsFlags.None,
        Action? onTap = null,
        Key? key = null) : base(child, key)
    {
        Label = label;
        Flags = flags;
        OnTap = onTap;
    }

    public string? Label { get; }

    public SemanticsFlags Flags { get; }

    public Action? OnTap { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSemanticsAnnotations(
            label: Label,
            flags: Flags,
            onTap: OnTap);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderSemanticsAnnotations)renderObject;
        semantics.Label = Label;
        semantics.Flags = Flags;
        semantics.OnTap = OnTap;
    }
}
