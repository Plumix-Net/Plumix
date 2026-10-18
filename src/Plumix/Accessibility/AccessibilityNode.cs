using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

// C#-only infrastructure: one node of the engine's platform accessibility tree. The geometry and the
// platform-neutral classification follow engine/src/flutter/shell/platform/common/accessibility_bridge.cc
// (`ConvertFlutterUpdate`, `SetRoleFromFlutterUpdate`, `SetStateFromFlutterUpdate`) and
// flutter_platform_node_delegate.cc; the platform bridges read everything else off `Data`.

namespace Plumix.Accessibility;

/// <summary>The role the desktop accessibility bridges give a semantics node.</summary>
/// <remarks>The <c>ax::mojom::Role</c> values the engine's <c>SetRoleFromFlutterUpdate</c> produces.</remarks>
public enum AccessibilityRole
{
    /// <summary>A node with children and no more specific role.</summary>
    Group,

    /// <summary>A childless node with no more specific role.</summary>
    StaticText,

    /// <summary>A node with the <see cref="SemanticsFlags.IsButton"/> flag.</summary>
    Button,

    /// <summary>An editable text field.</summary>
    TextField,

    /// <summary>A node with the <see cref="SemanticsFlags.IsHeader"/> flag.</summary>
    Header,

    /// <summary>A node with the <see cref="SemanticsFlags.IsImage"/> flag.</summary>
    Image,

    /// <summary>A node with the <see cref="SemanticsFlags.IsLink"/> flag.</summary>
    Link,

    /// <summary>A checkable node in a mutually exclusive group.</summary>
    RadioButton,

    /// <summary>A checkable node outside a mutually exclusive group.</summary>
    CheckBox,

    /// <summary>A toggleable node.</summary>
    Switch,

    /// <summary>A node with the <see cref="SemanticsFlags.IsSlider"/> flag.</summary>
    Slider,
}

/// <summary>The checked state the accessibility bridges report for a node.</summary>
/// <remarks>The engine's <c>ax::mojom::CheckedState</c>.</remarks>
public enum AccessibilityCheckedState
{
    /// <summary>The node is neither checkable nor toggleable.</summary>
    None,

    /// <summary>The node is unchecked or toggled off.</summary>
    False,

    /// <summary>The node is checked or toggled on.</summary>
    True,

    /// <summary>The node is in the mixed (indeterminate) checked state.</summary>
    Mixed,
}

/// <summary>
/// One node of an <see cref="AccessibilityBridge"/>'s tree: the last semantics update the framework sent
/// for it, plus its place in the platform tree.
/// </summary>
/// <remarks>
/// The engine's <c>FlutterPlatformNodeDelegate</c> over its <c>ui::AXNode</c>. Children are the node's
/// <see cref="SemanticsNodeUpdate.ChildrenInTraversalOrder"/>, as in every engine bridge; geometry is
/// the update's rect under its (traversal) transform, in the view's physical pixels.
/// </remarks>
public sealed class AccessibilityNode
{
    private static readonly IReadOnlyList<AccessibilityNode> NoChildren = [];
    private static readonly IReadOnlyList<int> NoIds = [];
    private IReadOnlyList<AccessibilityNode> _children = NoChildren;
    private IReadOnlyList<AccessibilityNode> _childrenInHitTestOrder = NoChildren;

    internal AccessibilityNode(AccessibilityBridge bridge, int id)
    {
        Bridge = bridge;
        Id = id;
    }

    /// <summary>The bridge that owns this node.</summary>
    public AccessibilityBridge Bridge { get; }

    /// <summary>The framework's id of the semantics node.</summary>
    public int Id { get; }

    /// <summary>The node whose traversal children include this one, or <c>null</c> for the root.</summary>
    public AccessibilityNode? Parent { get; internal set; }

    /// <summary>The children, in traversal (reading) order.</summary>
    public IReadOnlyList<AccessibilityNode> Children => _children;

    /// <summary>The children, in hit-test order: the topmost first.</summary>
    public IReadOnlyList<AccessibilityNode> ChildrenInHitTestOrder => _childrenInHitTestOrder;

    /// <summary>The flattened annotations of the node's last update.</summary>
    public SemanticsData Data { get; private set; } = null!;

    /// <summary>The node's transform into its parent's coordinate space, or <c>null</c> for identity.</summary>
    public Matrix4? Transform { get; private set; }

    /// <summary>Ids of the custom actions the node handles, ascending.</summary>
    public IReadOnlyList<int> CustomActionIds { get; private set; } = NoIds;

    /// <summary>Whether the node is still part of its bridge's tree.</summary>
    public bool IsAttached { get; internal set; } = true;

    internal IReadOnlyList<int> ChildIds { get; private set; } = NoIds;

    internal IReadOnlyList<int> HitTestChildIds { get; private set; } = NoIds;

    /// <summary>The node's own rect, in its coordinate space.</summary>
    public Rect Rect => Data.Rect;

    /// <summary>The node's label.</summary>
    public string Label => Data.Label;

    /// <summary>The node's value.</summary>
    public string Value => Data.Value;

    /// <summary>The node's hint.</summary>
    public string Hint => Data.Hint;

    /// <summary>The node's tooltip.</summary>
    public string Tooltip => Data.Tooltip;

    /// <summary>The node's identifier.</summary>
    public string Identifier => Data.Identifier;

    /// <summary>The node's flags.</summary>
    public SemanticsFlags Flags => Data.Flags;

    /// <summary>The actions the node handles.</summary>
    public SemanticsActions Actions => Data.Actions;

    /// <summary>Whether the node has any of <paramref name="flag"/>.</summary>
    public bool HasFlag(SemanticsFlags flag) => Data.HasFlag(flag);

    /// <summary>Whether the node handles any of <paramref name="action"/>.</summary>
    public bool HasAction(SemanticsActions action) => Data.HasAction(action);

    /// <summary>Whether the node has traversal children.</summary>
    public bool HasChildren => ChildIds.Count > 0;

    /// <summary>The role the desktop bridges give the node.</summary>
    /// <remarks>
    /// The engine's <c>SetRoleFromFlutterUpdate</c>, first match wins: button, editable text field,
    /// header, image, link, radio button, checkbox, switch, slider, then static text for a childless
    /// node and group otherwise. A read-only text field falls through.
    /// </remarks>
    public AccessibilityRole Role
    {
        get
        {
            if (HasFlag(SemanticsFlags.IsButton))
            {
                return AccessibilityRole.Button;
            }

            if (IsEditableTextField)
            {
                return AccessibilityRole.TextField;
            }

            if (HasFlag(SemanticsFlags.IsHeader))
            {
                return AccessibilityRole.Header;
            }

            if (HasFlag(SemanticsFlags.IsImage))
            {
                return AccessibilityRole.Image;
            }

            if (HasFlag(SemanticsFlags.IsLink))
            {
                return AccessibilityRole.Link;
            }

            if (HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup) && HasFlag(SemanticsFlags.HasCheckedState))
            {
                return AccessibilityRole.RadioButton;
            }

            if (HasFlag(SemanticsFlags.HasCheckedState))
            {
                return AccessibilityRole.CheckBox;
            }

            if (HasFlag(SemanticsFlags.HasToggledState))
            {
                return AccessibilityRole.Switch;
            }

            if (HasFlag(SemanticsFlags.IsSlider))
            {
                return AccessibilityRole.Slider;
            }

            return HasChildren ? AccessibilityRole.Group : AccessibilityRole.StaticText;
        }
    }

    /// <summary>Whether the node is a text field that accepts input.</summary>
    /// <remarks>The engine's <c>kEditable</c> state: a text field without the read-only flag.</remarks>
    public bool IsEditableTextField =>
        HasFlag(SemanticsFlags.IsTextField) && !HasFlag(SemanticsFlags.IsReadOnly);

    /// <summary>Whether the desktop bridges leave the node out of the platform tree.</summary>
    /// <remarks>
    /// The engine's <c>kIgnored</c> state: a static-text node without scroll actions whose label, value
    /// and hint are all empty. Every other node is focusable for the platform.
    /// </remarks>
    public bool IsIgnored =>
        Role == AccessibilityRole.StaticText
        && !HasAction(ScrollActions)
        && Label.Length == 0
        && Value.Length == 0
        && Hint.Length == 0;

    /// <summary>The node's checked state, for radio buttons, checkboxes and switches.</summary>
    /// <remarks>
    /// The engine's <c>kCheckedState</c>: a radio button or checkbox reports mixed, true or false; a
    /// switch reports its toggled state; every other role reports none.
    /// </remarks>
    public AccessibilityCheckedState CheckedState
    {
        get
        {
            switch (Role)
            {
                case AccessibilityRole.RadioButton:
                case AccessibilityRole.CheckBox:
                    if (HasFlag(SemanticsFlags.IsCheckStateMixed))
                    {
                        return AccessibilityCheckedState.Mixed;
                    }

                    return HasFlag(SemanticsFlags.IsChecked)
                        ? AccessibilityCheckedState.True
                        : AccessibilityCheckedState.False;
                case AccessibilityRole.Switch:
                    return HasFlag(SemanticsFlags.IsToggled)
                        ? AccessibilityCheckedState.True
                        : AccessibilityCheckedState.False;
                default:
                    return AccessibilityCheckedState.None;
            }
        }
    }

    /// <summary>Whether the node is selected.</summary>
    /// <remarks>The engine's <c>kSelected</c>: only an explicit selected state counts.</remarks>
    public bool IsSelected => HasFlag(SemanticsFlags.HasSelectedState) && HasFlag(SemanticsFlags.IsSelected);

    /// <summary>The node's expanded state: <c>null</c> when it is not expandable.</summary>
    /// <remarks>The engine's <c>kExpanded</c>/<c>kCollapsed</c> states.</remarks>
    public bool? IsExpanded =>
        HasFlag(SemanticsFlags.HasExpandedState) ? HasFlag(SemanticsFlags.IsExpanded) : null;

    /// <summary>Whether the node is enabled: only an explicit disabled state disables it.</summary>
    public bool IsEnabled => !HasFlag(SemanticsFlags.HasEnabledState) || HasFlag(SemanticsFlags.IsEnabled);

    /// <summary>Whether the node has input focus.</summary>
    public bool IsFocused => HasFlag(SemanticsFlags.IsFocused);

    /// <summary>Whether the node handles any scroll action.</summary>
    /// <remarks>The engine's <c>kScrollable</c>.</remarks>
    public bool IsScrollable => HasAction(ScrollActions);

    /// <summary>The scroll actions every engine bridge groups together.</summary>
    internal const SemanticsActions ScrollActions =
        SemanticsActions.ScrollLeft
        | SemanticsActions.ScrollRight
        | SemanticsActions.ScrollUp
        | SemanticsActions.ScrollDown;

    /// <summary>The node's rect in the view's coordinate space, in physical pixels.</summary>
    /// <remarks>
    /// The engine's <c>RelativeToGlobalBounds</c>: an empty rect is replaced by the union of the
    /// children's rects, then the rect is carried up through every ancestor's transform. The root's
    /// transform scales logical to physical pixels, as the framework's root semantics node does.
    /// </remarks>
    public Rect GetGlobalRect()
    {
        Rect rect = Rect;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            Rect? union = null;
            foreach (AccessibilityNode child in _children)
            {
                Rect childRect = child.Transform is { } childTransform
                    ? MatrixUtils.TransformRect(childTransform, child.Rect)
                    : child.Rect;
                union = union is { } current ? current.Union(childRect) : childRect;
            }

            if (union is { Width: > 0, Height: > 0 } children)
            {
                rect = children;
            }
        }

        for (AccessibilityNode? node = this; node is not null; node = node.Parent)
        {
            if (node.Transform is { } transform)
            {
                rect = MatrixUtils.TransformRect(transform, rect);
            }
        }

        return rect;
    }

    /// <summary>The node's rect in the view's coordinate space, in logical pixels.</summary>
    /// <remarks>
    /// <see cref="GetGlobalRect"/> divided by the view's device pixel ratio, where the macOS and iOS
    /// bridges undo it.
    /// </remarks>
    public Rect GetBoundsInView()
    {
        Rect rect = GetGlobalRect();
        double scale = Bridge.DevicePixelRatio;
        if (scale <= 0 || scale == 1.0)
        {
            return rect;
        }

        return new Rect(rect.X / scale, rect.Y / scale, rect.Width / scale, rect.Height / scale);
    }

    /// <summary>Asks the framework to perform <paramref name="action"/> on this node.</summary>
    public void PerformAction(SemanticsActions action, object? arguments = null) =>
        Bridge.DispatchAction(Id, action, arguments);

    internal void Update(SemanticsNodeUpdate update)
    {
        Data = update.Data;
        Transform = update.Transform;
        CustomActionIds = update.AdditionalActions;
        ChildIds = update.ChildrenInTraversalOrder;
        HitTestChildIds = update.ChildrenInHitTestOrder;
    }

    internal void SetChildren(
        IReadOnlyList<AccessibilityNode> children,
        IReadOnlyList<AccessibilityNode> childrenInHitTestOrder)
    {
        _children = children;
        _childrenInHitTestOrder = childrenInHitTestOrder;
    }

    /// <inheritdoc />
    public override string ToString() => $"AccessibilityNode#{Id}({Role}, \"{Label}\")";
}
