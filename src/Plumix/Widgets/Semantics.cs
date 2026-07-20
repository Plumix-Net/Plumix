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
    TabBar,
    Tab,
    TabPanel,
    Form,
}

public sealed class Semantics : SingleChildRenderObjectWidget
{
    public Semantics(
        Widget? child = null,
        string? label = null,
        string? hint = null,
        SemanticsFlags flags = SemanticsFlags.None,
        Action? onTap = null,
        Action? onLongPress = null,
        Action? onDismiss = null,
        bool liveRegion = false,
        bool container = false,
        bool explicitChildNodes = false,
        SemanticsRole role = SemanticsRole.None,
        bool scopesRoute = false,
        bool namesRoute = false,
        bool? expanded = null,
        bool? @checked = null,
        bool? selected = null,
        Key? key = null) : base(child, key)
    {
        Label = label;
        Hint = hint;
        Flags = flags
                | RoleFlags(role)
                | (scopesRoute ? SemanticsFlags.ScopesRoute : SemanticsFlags.None)
                | (namesRoute ? SemanticsFlags.NamesRoute : SemanticsFlags.None)
                | (expanded.HasValue ? SemanticsFlags.HasExpandedState : SemanticsFlags.None)
                | (expanded == true ? SemanticsFlags.IsExpanded : SemanticsFlags.None)
                | (@checked.HasValue ? SemanticsFlags.HasCheckedState : SemanticsFlags.None)
                | (@checked == true ? SemanticsFlags.IsChecked : SemanticsFlags.None)
                | (selected == true ? SemanticsFlags.IsSelected : SemanticsFlags.None);
        OnTap = onTap;
        OnLongPress = onLongPress;
        OnDismiss = onDismiss;
        LiveRegion = liveRegion;
        Container = container;
        ExplicitChildNodes = explicitChildNodes;
        Role = role;
        ScopesRoute = scopesRoute;
        NamesRoute = namesRoute;
        Expanded = expanded;
        Checked = @checked;
        Selected = selected;
    }

    public string? Label { get; }

    public string? Hint { get; }

    public SemanticsFlags Flags { get; }

    public Action? OnTap { get; }

    public Action? OnLongPress { get; }

    public Action? OnDismiss { get; }

    public Action? OnFocus { get; init; }

    public bool LiveRegion { get; }

    public bool Container { get; }

    public bool ExplicitChildNodes { get; }

    public SemanticsRole Role { get; }

    public bool ScopesRoute { get; }

    public bool NamesRoute { get; }

    public bool? Expanded { get; }

    public bool? Checked { get; }

    public bool? Selected { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var semantics = new RenderSemanticsAnnotations(
            label: Label,
            hint: Hint,
            role: Role,
            flags: Flags,
            onTap: OnTap,
            onLongPress: OnLongPress,
            onDismiss: OnDismiss,
            liveRegion: LiveRegion,
            container: Container,
            explicitChildNodes: ExplicitChildNodes);
        semantics.OnFocus = OnFocus;
        return semantics;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderSemanticsAnnotations)renderObject;
        semantics.Label = Label;
        semantics.Hint = Hint;
        semantics.Role = Role;
        semantics.Flags = Flags;
        semantics.OnTap = OnTap;
        semantics.OnLongPress = OnLongPress;
        semantics.OnDismiss = OnDismiss;
        semantics.OnFocus = OnFocus;
        semantics.LiveRegion = LiveRegion;
        semantics.Container = Container;
        semantics.ExplicitChildNodes = ExplicitChildNodes;
    }

    private static SemanticsFlags RoleFlags(SemanticsRole role) => role switch
    {
        SemanticsRole.Dialog => SemanticsFlags.IsDialog,
        SemanticsRole.AlertDialog => SemanticsFlags.IsAlertDialog,
        SemanticsRole.MenuItem or SemanticsRole.MenuItemCheckbox or SemanticsRole.Tab => SemanticsFlags.IsButton,
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

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (ExcludeSemantics)
public sealed class ExcludeSemantics : SingleChildRenderObjectWidget
{
    public ExcludeSemantics(
        Widget? child = null,
        bool excluding = true,
        Key? key = null) : base(child, key)
    {
        Excluding = excluding;
    }

    public bool Excluding { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderExcludeSemantics(Excluding);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderExcludeSemantics)renderObject).Excluding = Excluding;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (BlockSemantics)
public sealed class BlockSemantics : SingleChildRenderObjectWidget
{
    public BlockSemantics(
        Widget? child = null,
        bool blocking = true,
        Key? key = null) : base(child, key)
    {
        Blocking = blocking;
    }

    public bool Blocking { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderBlockSemantics(Blocking);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderBlockSemantics)renderObject).Blocking = Blocking;
    }
}
