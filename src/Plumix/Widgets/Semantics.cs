using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (Semantics subset)

namespace Plumix.Widgets;

public enum SemanticsRole
{
    None,
    Dialog,
    AlertDialog,
    Menu,
    MenuItem,
    MenuItemCheckbox,
}

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
        SemanticsRole role = SemanticsRole.None,
        bool scopesRoute = false,
        bool namesRoute = false,
        bool? expanded = null,
        bool? @checked = null,
        Key? key = null) : base(child, key)
    {
        Label = label;
        Flags = flags
                | RoleFlags(role)
                | (scopesRoute ? SemanticsFlags.ScopesRoute : SemanticsFlags.None)
                | (namesRoute ? SemanticsFlags.NamesRoute : SemanticsFlags.None)
                | (expanded.HasValue ? SemanticsFlags.HasExpandedState : SemanticsFlags.None)
                | (expanded == true ? SemanticsFlags.IsExpanded : SemanticsFlags.None)
                | (@checked.HasValue ? SemanticsFlags.HasCheckedState : SemanticsFlags.None)
                | (@checked == true ? SemanticsFlags.IsChecked : SemanticsFlags.None);
        OnTap = onTap;
        OnDismiss = onDismiss;
        LiveRegion = liveRegion;
        Container = container;
        ExplicitChildNodes = explicitChildNodes;
        Role = role;
        ScopesRoute = scopesRoute;
        NamesRoute = namesRoute;
        Expanded = expanded;
        Checked = @checked;
    }

    public string? Label { get; }

    public SemanticsFlags Flags { get; }

    public Action? OnTap { get; }

    public Action? OnDismiss { get; }

    public bool LiveRegion { get; }

    public bool Container { get; }

    public bool ExplicitChildNodes { get; }

    public SemanticsRole Role { get; }

    public bool ScopesRoute { get; }

    public bool NamesRoute { get; }

    public bool? Expanded { get; }

    public bool? Checked { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSemanticsAnnotations(
            label: Label,
            role: Role,
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
        semantics.Role = Role;
        semantics.Flags = Flags;
        semantics.OnTap = OnTap;
        semantics.OnDismiss = OnDismiss;
        semantics.LiveRegion = LiveRegion;
        semantics.Container = Container;
        semantics.ExplicitChildNodes = ExplicitChildNodes;
    }

    private static SemanticsFlags RoleFlags(SemanticsRole role) => role switch
    {
        SemanticsRole.Dialog => SemanticsFlags.IsDialog,
        SemanticsRole.AlertDialog => SemanticsFlags.IsAlertDialog,
        SemanticsRole.MenuItem or SemanticsRole.MenuItemCheckbox => SemanticsFlags.IsButton,
        _ => SemanticsFlags.None,
    };
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
