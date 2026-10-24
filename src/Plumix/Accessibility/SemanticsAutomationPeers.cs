using Avalonia;
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Plumix.Rendering;
using Plumix.Widgets;

// C#-only infrastructure: the desktop and Android accessibility bridge over Avalonia's automation peers.
// Avalonia hands these peers to UI Automation (Windows), the macOS accessibility API, AT-SPI (Linux)
// and Android's accessibility node provider. The tree, roles, states, the focus-is-an-action rule
// and the activation-on-first-query rule follow the engine's common bridge
// (engine/src/flutter/shell/platform/common/accessibility_bridge.cc, flutter_platform_node_delegate.cc)
// and its Windows embedder (shell/platform/windows/flutter_window.cc `OnGetObject`,
// accessibility_bridge_windows.cc); the deliberate departures are listed in docs/ai/DIVERGENCES.md.

namespace Plumix.Accessibility;

/// <summary>The automation peer of a <see cref="PlumixHost"/>: the root of its semantics peers.</summary>
/// <remarks>
/// The engine's Windows fragment root. The first time an automation client asks for the host's
/// children the host's <see cref="AccessibilityBridge"/> is activated, which turns semantics on — the
/// Windows embedder's <c>WM_GETOBJECT</c> rule — and from then on every commit is turned into the
/// matching Avalonia peer notifications.
/// </remarks>
internal sealed class PlumixHostAutomationPeer : ControlAutomationPeer
{
    private readonly Dictionary<int, SemanticsNodeAutomationPeer> _peers = [];
    private AnnouncementAutomationPeer? _announcement;

    public PlumixHostAutomationPeer(PlumixHost owner)
        : base(owner)
    {
        Bridge = owner.AccessibilityBridge;
        Bridge.Committed += HandleCommitted;
        Bridge.Cleared += HandleCleared;
        Bridge.AnnouncementRequested += HandleAnnouncementRequested;
    }

    public AccessibilityBridge Bridge { get; }

    /// <summary>The live peer of the node <paramref name="id"/>, if one was created.</summary>
    internal SemanticsNodeAutomationPeer? FindPeer(int id) => _peers.GetValueOrDefault(id);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override string GetClassNameCore() => "FlutterView";

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        Bridge.Activate();
        var children = new List<AutomationPeer>(2);
        if (Bridge.Root is { IsIgnored: false } root)
        {
            children.Add(GetOrCreatePeer(root));
        }

        if (_announcement is not null)
        {
            children.Add(_announcement);
        }

        return children;
    }

    internal SemanticsNodeAutomationPeer GetOrCreatePeer(AccessibilityNode node)
    {
        SemanticsPeerShape shape = SemanticsPeerShapes.For(node);
        if (_peers.TryGetValue(node.Id, out SemanticsNodeAutomationPeer? peer)
            && ReferenceEquals(peer.Node, node)
            && peer.Shape == shape)
        {
            return peer;
        }

        peer = SemanticsNodeAutomationPeer.Create(this, node, shape);
        _peers[node.Id] = peer;
        return peer;
    }

    internal IReadOnlyList<AutomationPeer> GetChildPeers(AccessibilityNode node)
    {
        var children = new List<AutomationPeer>(node.Children.Count);
        foreach (AccessibilityNode child in node.Children)
        {
            if (!child.IsIgnored)
            {
                children.Add(GetOrCreatePeer(child));
            }
        }

        return children;
    }

    internal AutomationPeer GetParentPeer(AccessibilityNode node) =>
        node.Parent is { } parent && _peers.TryGetValue(parent.Id, out SemanticsNodeAutomationPeer? peer)
            ? peer
            : node.Parent is { } liveParent
                ? GetOrCreatePeer(liveParent)
                : this;

    /// <summary>Maps a rect in the view's logical coordinates to the host's visual root.</summary>
    /// <remarks>
    /// Avalonia's backends expect a peer's bounding rectangle in the coordinates of its visual root, as
    /// <c>ControlAutomationPeer</c> reports it, and convert it to screen coordinates themselves.
    /// </remarks>
    internal Rect ToVisualRoot(Rect viewRect)
    {
        if (TopLevel.GetTopLevel(Owner) is { } root && Owner.TransformToVisual(root) is { } transform)
        {
            return viewRect.TransformToAABB(transform);
        }

        return viewRect;
    }

    /// <summary>The logical size of the host's view.</summary>
    internal Size ViewSize
    {
        get
        {
            FlutterView view = ((PlumixHost)Owner).RootFlutterView;
            double scale = view.DevicePixelRatio > 0 ? view.DevicePixelRatio : 1.0;
            return new Size(view.PhysicalSize.Width / scale, view.PhysicalSize.Height / scale);
        }
    }

    private void HandleCommitted(AccessibilityTreeUpdate update)
    {
        if (update.RootChanged)
        {
            InvalidateChildren();
        }

        foreach (AccessibilityNode node in update.RemovedNodes)
        {
            if (_peers.Remove(node.Id, out SemanticsNodeAutomationPeer? peer))
            {
                peer.TrySetParentInternal(null);
            }
        }

        var replacedParents = new HashSet<AccessibilityNode?>();
        foreach (AccessibilityNode node in update.UpdatedNodes)
        {
            if (_peers.TryGetValue(node.Id, out SemanticsNodeAutomationPeer? peer)
                && peer.Shape != SemanticsPeerShapes.For(node))
            {
                // A node that gained or lost a pattern gets a peer of the new shape; its parent re-reads
                // its children so the platform sees the replacement.
                _peers.Remove(node.Id);
                replacedParents.Add(node.Parent);
            }
        }

        foreach (AccessibilityEvent change in update.Events)
        {
            AccessibilityNode node = change.Node;
            if (change.Type is AccessibilityEventType.IgnoredChanged)
            {
                replacedParents.Add(node.Parent);
                continue;
            }

            if (_peers.TryGetValue(node.Id, out SemanticsNodeAutomationPeer? peer))
            {
                peer.HandleEvent(change.Type);
            }
        }

        foreach (AccessibilityNode? parent in replacedParents)
        {
            if (parent is not null && _peers.TryGetValue(parent.Id, out SemanticsNodeAutomationPeer? parentPeer))
            {
                parentPeer.InvalidateChildPeers();
            }
            else
            {
                InvalidateChildren();
            }
        }
    }

    private void HandleCleared()
    {
        foreach (SemanticsNodeAutomationPeer peer in _peers.Values)
        {
            peer.TrySetParentInternal(null);
        }

        _peers.Clear();
        InvalidateChildren();
    }

    // The Windows embedder announces through a separate alert node whose name is the message.
    private void HandleAnnouncementRequested(AccessibilityAnnouncement announcement)
    {
        bool isNew = _announcement is null;
        _announcement ??= new AnnouncementAutomationPeer(this);
        string? oldMessage = _announcement.Message;
        _announcement.Announce(announcement);
        if (isNew)
        {
            InvalidateChildren();
        }

        _announcement.RaisePropertyChangedEvent(
            AutomationElementIdentifiers.NameProperty,
            oldMessage,
            announcement.Message);
    }
}

/// <summary>Which provider interfaces a semantics peer implements.</summary>
[Flags]
internal enum SemanticsPeerShape
{
    None = 0,
    Invoke = 1 << 0,
    Toggle = 1 << 1,
    SelectionItem = 1 << 2,
    ExpandCollapse = 1 << 3,
    RangeValue = 1 << 4,
    Value = 1 << 5,
    Scroll = 1 << 6,
}

/// <summary>Chooses the provider interfaces of a node's peer.</summary>
/// <remarks>
/// Avalonia's Android backend registers a peer's patterns by its type once, so a peer's provider set
/// is fixed by its class and a node whose patterns change gets a new peer. The shapes below are the
/// combinations a Flutter node produces; any other combination drops patterns in the order scroll,
/// invoke, expand/collapse until a shape remains.
/// </remarks>
internal static class SemanticsPeerShapes
{
    private static readonly SemanticsPeerShape[] Supported =
    [
        SemanticsPeerShape.None,
        SemanticsPeerShape.Invoke,
        SemanticsPeerShape.Toggle,
        SemanticsPeerShape.SelectionItem,
        SemanticsPeerShape.Invoke | SemanticsPeerShape.SelectionItem,
        SemanticsPeerShape.ExpandCollapse,
        SemanticsPeerShape.Invoke | SemanticsPeerShape.ExpandCollapse,
        SemanticsPeerShape.RangeValue,
        SemanticsPeerShape.Value,
        SemanticsPeerShape.Invoke | SemanticsPeerShape.Value,
        SemanticsPeerShape.Scroll,
        SemanticsPeerShape.Invoke | SemanticsPeerShape.Scroll,
    ];

    private static readonly SemanticsPeerShape[] DropOrder =
    [
        SemanticsPeerShape.Scroll,
        SemanticsPeerShape.Invoke,
        SemanticsPeerShape.ExpandCollapse,
        SemanticsPeerShape.Value,
        SemanticsPeerShape.RangeValue,
        SemanticsPeerShape.SelectionItem,
    ];

    public static SemanticsPeerShape For(AccessibilityNode node)
    {
        SemanticsPeerShape shape = SemanticsPeerShape.None;
        AccessibilityRole role = node.Role;
        if (role is AccessibilityRole.CheckBox or AccessibilityRole.Switch)
        {
            shape |= SemanticsPeerShape.Toggle;
        }
        else if (role is AccessibilityRole.RadioButton || node.HasFlag(SemanticsFlags.HasSelectedState))
        {
            // The engine reports isSelected whatever the node's actions, and a tappable item stays
            // clickable (Android's SelectionItem provider only adds ACTION_SELECT).
            shape |= SemanticsPeerShape.SelectionItem;
        }

        if ((shape & SemanticsPeerShape.Toggle) == 0 && node.HasAction(SemanticsActions.Tap))
        {
            shape |= SemanticsPeerShape.Invoke;
        }

        if (node.IsExpanded is not null)
        {
            shape |= SemanticsPeerShape.ExpandCollapse;
        }

        if (role is AccessibilityRole.Slider || node.HasAction(SemanticsActions.Increase | SemanticsActions.Decrease))
        {
            shape |= SemanticsPeerShape.RangeValue;
        }
        else if (node.HasFlag(SemanticsFlags.IsTextField))
        {
            shape |= SemanticsPeerShape.Value;
        }

        if (node.IsScrollable)
        {
            shape |= SemanticsPeerShape.Scroll;
        }

        foreach (SemanticsPeerShape dropped in DropOrder)
        {
            if (Array.IndexOf(Supported, shape) >= 0)
            {
                break;
            }

            shape &= ~dropped;
        }

        return Array.IndexOf(Supported, shape) >= 0 ? shape : SemanticsPeerShape.None;
    }
}

/// <summary>The automation peer of one semantics node.</summary>
/// <remarks>
/// The engine's <c>FlutterPlatformNodeDelegate</c>. Provider members are public here and become the
/// provider implementations of the sealed shape classes below, which only list the interfaces.
/// </remarks>
internal abstract class SemanticsNodeAutomationPeer : AutomationPeer
{
    private IReadOnlyList<AutomationPeer>? _children;
    private AutomationPeer? _parent;

    protected SemanticsNodeAutomationPeer(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape)
    {
        Host = host;
        Node = node;
        Shape = shape;
    }

    public PlumixHostAutomationPeer Host { get; }

    public AccessibilityNode Node { get; }

    public SemanticsPeerShape Shape { get; }

    public static SemanticsNodeAutomationPeer Create(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape) => shape switch
    {
        SemanticsPeerShape.Invoke => new InvokePeer(host, node, shape),
        SemanticsPeerShape.Toggle => new TogglePeer(host, node, shape),
        SemanticsPeerShape.SelectionItem => new SelectionItemPeer(host, node, shape),
        SemanticsPeerShape.Invoke | SemanticsPeerShape.SelectionItem =>
            new InvokeSelectionItemPeer(host, node, shape),
        SemanticsPeerShape.ExpandCollapse => new ExpandCollapsePeer(host, node, shape),
        SemanticsPeerShape.Invoke | SemanticsPeerShape.ExpandCollapse =>
            new InvokeExpandCollapsePeer(host, node, shape),
        SemanticsPeerShape.RangeValue => new RangeValuePeer(host, node, shape),
        SemanticsPeerShape.Value => new ValuePeer(host, node, shape),
        SemanticsPeerShape.Invoke | SemanticsPeerShape.Value => new InvokeValuePeer(host, node, shape),
        SemanticsPeerShape.Scroll => new ScrollPeer(host, node, shape),
        SemanticsPeerShape.Invoke | SemanticsPeerShape.Scroll => new InvokeScrollPeer(host, node, shape),
        _ => new PlainPeer(host, node, shape),
    };

    // ---- AutomationPeer -------------------------------------------------------------------------

    // The engine's `kScrollToMakeVisible`.
    protected override void BringIntoViewCore() => Node.PerformAction(SemanticsActions.ShowOnScreen);

    protected override string? GetAcceleratorKeyCore() => null;

    protected override string? GetAccessKeyCore() => null;

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        switch (Node.Role)
        {
            case AccessibilityRole.Button:
            case AccessibilityRole.Switch:
                return AutomationControlType.Button;
            case AccessibilityRole.TextField:
                return AutomationControlType.Edit;
            case AccessibilityRole.Header:
                return AutomationControlType.Text;
            case AccessibilityRole.Image:
                return AutomationControlType.Image;
            case AccessibilityRole.Link:
                return AutomationControlType.Hyperlink;
            case AccessibilityRole.RadioButton:
                return AutomationControlType.RadioButton;
            case AccessibilityRole.CheckBox:
                return AutomationControlType.CheckBox;
            case AccessibilityRole.Slider:
                return AutomationControlType.Slider;
        }

        // The common bridge stops at group and static text; a framework role refines those.
        AutomationControlType? fromRole = Node.Data.Role switch
        {
            SemanticsRole.Tab => AutomationControlType.TabItem,
            SemanticsRole.TabBar => AutomationControlType.Tab,
            SemanticsRole.Dialog or SemanticsRole.AlertDialog => AutomationControlType.Window,
            SemanticsRole.Table => AutomationControlType.Table,
            SemanticsRole.Row => AutomationControlType.DataItem,
            SemanticsRole.Cell => AutomationControlType.Text,
            SemanticsRole.ColumnHeader => AutomationControlType.HeaderItem,
            SemanticsRole.ComboBox => AutomationControlType.ComboBox,
            SemanticsRole.MenuBar => AutomationControlType.MenuBar,
            SemanticsRole.Menu => AutomationControlType.Menu,
            SemanticsRole.MenuItem or SemanticsRole.MenuItemCheckbox or SemanticsRole.MenuItemRadio =>
                AutomationControlType.MenuItem,
            SemanticsRole.List or SemanticsRole.RadioGroup => AutomationControlType.List,
            SemanticsRole.ListItem => AutomationControlType.ListItem,
            SemanticsRole.Tooltip => AutomationControlType.ToolTip,
            SemanticsRole.ProgressBar or SemanticsRole.LoadingSpinner => AutomationControlType.ProgressBar,
            SemanticsRole.SpinButton => AutomationControlType.Spinner,
            SemanticsRole.Status => AutomationControlType.StatusBar,
            _ => null,
        };
        if (fromRole is { } type)
        {
            return type;
        }

        return Node.Role == AccessibilityRole.Group ? AutomationControlType.Group : AutomationControlType.Text;
    }

    // The engine's `kIdentifier`, exposed as the author unique id.
    protected override string? GetAutomationIdCore() => Node.Identifier.Length == 0 ? null : Node.Identifier;

    protected override Rect GetBoundingRectangleCore() =>
        Node.IsAttached ? Host.ToVisualRoot(Node.GetBoundsInView()) : default;

    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
    {
        if (_children is not null)
        {
            return _children;
        }

        IReadOnlyList<AutomationPeer> children = Node.IsAttached ? Host.GetChildPeers(Node) : [];
        foreach (AutomationPeer child in children)
        {
            (child as SemanticsNodeAutomationPeer)?.TrySetParentInternal(this);
        }

        return _children = children;
    }

    // Avalonia's Android backend reads the class name as the node's Android widget class, which is
    // how TalkBack describes a node's role; the engine's Android bridge names the same classes.
    protected override string GetClassNameCore()
    {
        if (!OperatingSystem.IsAndroid())
        {
            return "FlutterSemanticsNode";
        }

        return Node.Role switch
        {
            AccessibilityRole.Button => "android.widget.Button",
            AccessibilityRole.TextField => "android.widget.EditText",
            AccessibilityRole.Image => "android.widget.ImageView",
            AccessibilityRole.RadioButton => "android.widget.RadioButton",
            AccessibilityRole.CheckBox => "android.widget.CheckBox",
            AccessibilityRole.Switch => "android.widget.Switch",
            AccessibilityRole.Slider => "android.widget.SeekBar",
            _ when Node.IsScrollable => Node.HasAction(SemanticsActions.ScrollLeft | SemanticsActions.ScrollRight)
                ? "android.widget.HorizontalScrollView"
                : "android.widget.ScrollView",
            _ => "android.view.View",
        };
    }

    protected override AutomationPeer? GetLabeledByCore() => null;

    // The label, with the tooltip on its own line as the iOS and web bridges compose it.
    protected override string? GetNameCore()
    {
        string label = Node.Label;
        string tooltip = Node.Tooltip;
        if (tooltip.Length == 0)
        {
            return label;
        }

        return label.Length == 0 ? tooltip : label + "\n" + tooltip;
    }

    protected override string? GetHelpTextCore() => Node.Hint.Length == 0 ? null : Node.Hint;

    // The web bridge's heading rule: an explicit level, else level 2 for a header.
    protected override int GetHeadingLevelCore()
    {
        if (Node.Data.HeadingLevel > 0)
        {
            return Node.Data.HeadingLevel;
        }

        return Node.HasFlag(SemanticsFlags.IsHeader) ? 2 : 0;
    }

    protected override AutomationLandmarkType? GetLandmarkTypeCore() => Node.Data.Role switch
    {
        SemanticsRole.Complementary => AutomationLandmarkType.Complementary,
        SemanticsRole.ContentInfo => AutomationLandmarkType.ContentInfo,
        SemanticsRole.Main => AutomationLandmarkType.Main,
        SemanticsRole.Navigation => AutomationLandmarkType.Navigation,
        SemanticsRole.Region => AutomationLandmarkType.Region,
        SemanticsRole.Form => AutomationLandmarkType.Form,
        _ => null,
    };

    protected override AutomationPeer? GetParentCore() =>
        _parent ?? (Node.IsAttached ? Host.GetParentPeer(Node) : null);

    // The engine's tree focus: the node the framework reports focused.
    protected override bool HasKeyboardFocusCore() => Host.Bridge.FocusedNodeId == Node.Id;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override bool IsEnabledCore() => Node.IsEnabled;

    protected override bool IsKeyboardFocusableCore() => Node.HasFlag(SemanticsFlags.IsFocusable);

    protected override bool IsOffscreenCore()
    {
        if (!Node.IsAttached || Node.HasFlag(SemanticsFlags.IsHidden))
        {
            return true;
        }

        Rect bounds = Node.GetBoundsInView();
        return bounds.Width <= 0
               || bounds.Height <= 0
               || !new Rect(Host.ViewSize).Intersects(bounds);
    }

    // The engine's `kFocus`: accessibility focus moves to this node.
    protected override void SetFocusCore() => Host.Bridge.SetLastFocusedId(Node.Id);

    protected override bool ShowContextMenuCore()
    {
        if (!Node.HasAction(SemanticsActions.LongPress))
        {
            return false;
        }

        Node.PerformAction(SemanticsActions.LongPress);
        return true;
    }

    protected override AutomationLiveSetting GetLiveSettingCore() =>
        Node.HasFlag(SemanticsFlags.IsLiveRegion) ? AutomationLiveSetting.Polite : AutomationLiveSetting.Off;

    protected override object? GetProviderCore(Type providerType) =>
        providerType.IsInstanceOfType(this) ? this : null;

    protected override bool TrySetParent(AutomationPeer? parent)
    {
        _parent = parent;
        return true;
    }

    internal void TrySetParentInternal(AutomationPeer? parent) => _parent = parent;

    internal void InvalidateChildPeers()
    {
        _children = null;
        RaiseChildrenChangedEvent();
    }

    internal void HandleEvent(AccessibilityEventType type)
    {
        switch (type)
        {
            case AccessibilityEventType.ChildrenChanged:
                InvalidateChildPeers();
                break;
            case AccessibilityEventType.NameChanged:
            case AccessibilityEventType.LiveRegionChanged:
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, null, GetName());
                break;
            case AccessibilityEventType.HintChanged:
                RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, null, GetHelpText());
                break;
            case AccessibilityEventType.BoundsChanged:
                RaisePropertyChangedEvent(
                    AutomationElementIdentifiers.BoundingRectangleProperty,
                    null,
                    GetBoundingRectangle());
                break;
            case AccessibilityEventType.CheckedStateChanged:
            case AccessibilityEventType.SelectedChanged:
                if ((Shape & SemanticsPeerShape.Toggle) != 0)
                {
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, null, ToggleState);
                }

                if ((Shape & SemanticsPeerShape.SelectionItem) != 0)
                {
                    RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, null, IsSelected);
                }

                break;
            case AccessibilityEventType.ExpandedChanged:
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    null,
                    ExpandCollapseState);
                break;
            case AccessibilityEventType.ValueChanged:
                if ((Shape & SemanticsPeerShape.RangeValue) != 0)
                {
                    RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, null, RangeValue);
                }
                else if ((Shape & SemanticsPeerShape.Value) != 0)
                {
                    RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, null, Node.Value);
                }

                break;
            case AccessibilityEventType.ScrollPositionChanged:
                if ((Shape & SemanticsPeerShape.Scroll) != 0)
                {
                    RaisePropertyChangedEvent(
                        VerticallyScrollable
                            ? ScrollPatternIdentifiers.VerticalScrollPercentProperty
                            : ScrollPatternIdentifiers.HorizontalScrollPercentProperty,
                        null,
                        VerticallyScrollable ? VerticalScrollPercent : HorizontalScrollPercent);
                }

                break;
        }
    }

    // ---- IInvokeProvider ------------------------------------------------------------------------

    // The engine's `kDoDefault`.
    public void Invoke()
    {
        EnsureEnabled();
        Node.PerformAction(SemanticsActions.Tap);
    }

    // ---- IToggleProvider ------------------------------------------------------------------------

    public ToggleState ToggleState => Node.CheckedState switch
    {
        AccessibilityCheckedState.True => ToggleState.On,
        AccessibilityCheckedState.Mixed => ToggleState.Indeterminate,
        _ => ToggleState.Off,
    };

    public void Toggle()
    {
        EnsureEnabled();
        Node.PerformAction(SemanticsActions.Tap);
    }

    // ---- ISelectionItemProvider ----------------------------------------------------------------

    public bool IsSelected => Node.Role == AccessibilityRole.RadioButton
        ? Node.CheckedState == AccessibilityCheckedState.True
        : Node.IsSelected;

    public ISelectionProvider? SelectionContainer => null;

    public void Select()
    {
        EnsureEnabled();
        if (!IsSelected)
        {
            Node.PerformAction(SemanticsActions.Tap);
        }
    }

    public void AddToSelection() => Select();

    public void RemoveFromSelection()
    {
        EnsureEnabled();
        if (IsSelected && Node.Role != AccessibilityRole.RadioButton)
        {
            Node.PerformAction(SemanticsActions.Tap);
        }
    }

    // ---- IExpandCollapseProvider ---------------------------------------------------------------

    public ExpandCollapseState ExpandCollapseState => Node.IsExpanded switch
    {
        true => ExpandCollapseState.Expanded,
        false => ExpandCollapseState.Collapsed,
        null => ExpandCollapseState.LeafNode,
    };

    public bool ShowsMenu => Node.Data.Role is SemanticsRole.Menu or SemanticsRole.MenuItem;

    public void Expand() => ChangeExpansion(expand: true);

    public void Collapse() => ChangeExpansion(expand: false);

    private void ChangeExpansion(bool expand)
    {
        EnsureEnabled();
        if (Node.IsExpanded == expand)
        {
            return;
        }

        SemanticsActions action = expand ? SemanticsActions.Expand : SemanticsActions.Collapse;
        Node.PerformAction(Node.HasAction(action) ? action : SemanticsActions.Tap);
    }

    // ---- IRangeValueProvider -------------------------------------------------------------------

    public bool IsReadOnly => (Shape & SemanticsPeerShape.RangeValue) != 0
        ? !Node.HasAction(SemanticsActions.Increase | SemanticsActions.Decrease)
        : !Node.IsEditableTextField || !Node.HasAction(SemanticsActions.SetText);

    public double Minimum => ParseRange().Minimum;

    public double Maximum => ParseRange().Maximum;

    public double RangeValue => ParseRange().Value;

    public double SmallChange => ParseRange().Step;

    public double LargeChange => ParseRange().Step;

    // One increase or decrease per request, toward the requested value, as the web bridge's range
    // input surrogate moves.
    public void SetValue(double value)
    {
        EnsureEnabled();
        double current = RangeValue;
        if (value > current && Node.HasAction(SemanticsActions.Increase))
        {
            Node.PerformAction(SemanticsActions.Increase);
        }
        else if (value < current && Node.HasAction(SemanticsActions.Decrease))
        {
            Node.PerformAction(SemanticsActions.Decrease);
        }
    }

    private (double Minimum, double Maximum, double Value, double Step) ParseRange()
    {
        bool isPercent = Node.Value.TrimEnd().EndsWith('%');
        double minimum = ParseNumber(Node.Data.MinValue) ?? 0;
        double maximum = ParseNumber(Node.Data.MaxValue) ?? (isPercent ? 100 : 0);
        double value = ParseNumber(Node.Value) ?? minimum;
        double step = ParseNumber(Node.Data.IncreasedValue) is { } increased && increased != value
            ? Math.Abs(increased - value)
            : ParseNumber(Node.Data.DecreasedValue) is { } decreased && decreased != value
                ? Math.Abs(value - decreased)
                : 1;
        return (minimum, Math.Max(minimum, maximum), value, step);
    }

    private static double? ParseNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string trimmed = text.Trim().TrimEnd('%').Trim();
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
               || double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out number)
            ? number
            : null;
    }

    // ---- IValueProvider ------------------------------------------------------------------------

    public string? Value => Node.Value;

    // Android's `ACTION_SET_TEXT`, which its engine bridge maps to `SemanticsAction.setText`.
    public void SetValue(string? value)
    {
        EnsureEnabled();
        if (IsReadOnly)
        {
            throw new InvalidOperationException("The text field is read-only.");
        }

        Node.PerformAction(SemanticsActions.SetText, value ?? string.Empty);
    }

    // ---- IScrollProvider -----------------------------------------------------------------------

    public bool HorizontallyScrollable => Node.HasAction(SemanticsActions.ScrollLeft | SemanticsActions.ScrollRight);

    public bool VerticallyScrollable => Node.HasAction(SemanticsActions.ScrollUp | SemanticsActions.ScrollDown);

    public double HorizontalScrollPercent => HorizontallyScrollable ? ScrollPercent() : -1;

    public double VerticalScrollPercent => VerticallyScrollable ? ScrollPercent() : -1;

    public double HorizontalViewSize => HorizontallyScrollable ? ViewSize(Node.Rect.Width) : 100;

    public double VerticalViewSize => VerticallyScrollable ? ViewSize(Node.Rect.Height) : 100;

    // Scrolling toward the end of the content is Flutter's scroll up (vertical) or scroll left
    // (horizontal), as the Android bridge maps `ACTION_SCROLL_FORWARD`.
    public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
    {
        EnsureEnabled();
        PerformScroll(horizontalAmount, SemanticsActions.ScrollLeft, SemanticsActions.ScrollRight);
        PerformScroll(verticalAmount, SemanticsActions.ScrollUp, SemanticsActions.ScrollDown);
    }

    public void SetScrollPercent(double horizontalPercent, double verticalPercent)
    {
        EnsureEnabled();
        if (!Node.HasAction(SemanticsActions.ScrollToOffset)
            || Node.Data.ScrollExtentMin is not { } min
            || Node.Data.ScrollExtentMax is not { } max
            || !double.IsFinite(min)
            || !double.IsFinite(max))
        {
            throw new InvalidOperationException("The node cannot scroll to an offset.");
        }

        double horizontal = horizontalPercent >= 0 && HorizontallyScrollable
            ? min + ((max - min) * horizontalPercent / 100)
            : 0;
        double vertical = verticalPercent >= 0 && VerticallyScrollable
            ? min + ((max - min) * verticalPercent / 100)
            : 0;
        Node.PerformAction(SemanticsActions.ScrollToOffset, new Point(horizontal, vertical));
    }

    private void PerformScroll(ScrollAmount amount, SemanticsActions forward, SemanticsActions backward)
    {
        SemanticsActions action = amount switch
        {
            ScrollAmount.SmallIncrement or ScrollAmount.LargeIncrement => forward,
            ScrollAmount.SmallDecrement or ScrollAmount.LargeDecrement => backward,
            _ => SemanticsActions.None,
        };
        if (action != SemanticsActions.None && Node.HasAction(action))
        {
            Node.PerformAction(action);
        }
    }

    private double ScrollPercent()
    {
        if (Node.Data.ScrollPosition is not { } position
            || Node.Data.ScrollExtentMin is not { } min
            || Node.Data.ScrollExtentMax is not { } max
            || !double.IsFinite(position)
            || !double.IsFinite(min)
            || !double.IsFinite(max)
            || max <= min)
        {
            return 0;
        }

        return Math.Clamp((position - min) / (max - min) * 100, 0, 100);
    }

    private double ViewSize(double viewport)
    {
        if (Node.Data.ScrollExtentMin is not { } min
            || Node.Data.ScrollExtentMax is not { } max
            || !double.IsFinite(min)
            || !double.IsFinite(max)
            || viewport <= 0)
        {
            return 100;
        }

        return Math.Clamp(viewport / (max - min + viewport) * 100, 0, 100);
    }

    private sealed class PlainPeer(PlumixHostAutomationPeer host, AccessibilityNode node, SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape);

    private sealed class InvokePeer(PlumixHostAutomationPeer host, AccessibilityNode node, SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IInvokeProvider;

    private sealed class TogglePeer(PlumixHostAutomationPeer host, AccessibilityNode node, SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IToggleProvider;

    private sealed class SelectionItemPeer(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), ISelectionItemProvider;

    private sealed class InvokeSelectionItemPeer(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IInvokeProvider, ISelectionItemProvider;

    private sealed class ExpandCollapsePeer(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IExpandCollapseProvider;

    private sealed class InvokeExpandCollapsePeer(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IInvokeProvider, IExpandCollapseProvider;

    private sealed class RangeValuePeer(PlumixHostAutomationPeer host, AccessibilityNode node, SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IRangeValueProvider
    {
        double IRangeValueProvider.Value => RangeValue;
    }

    private sealed class ValuePeer(PlumixHostAutomationPeer host, AccessibilityNode node, SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IValueProvider;

    private sealed class InvokeValuePeer(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IInvokeProvider, IValueProvider;

    private sealed class ScrollPeer(PlumixHostAutomationPeer host, AccessibilityNode node, SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IScrollProvider;

    private sealed class InvokeScrollPeer(
        PlumixHostAutomationPeer host,
        AccessibilityNode node,
        SemanticsPeerShape shape)
        : SemanticsNodeAutomationPeer(host, node, shape), IInvokeProvider, IScrollProvider;
}

/// <summary>The hidden live region the host peer announces messages through.</summary>
/// <remarks>The Windows embedder's alert node (<c>AccessibilityBridgeWindows::AnnounceAlert</c>).</remarks>
internal sealed class AnnouncementAutomationPeer(PlumixHostAutomationPeer host) : AutomationPeer
{
    private AutomationLiveSetting _liveSetting = AutomationLiveSetting.Polite;

    public string? Message { get; private set; }

    public void Announce(AccessibilityAnnouncement announcement)
    {
        Message = announcement.Message;
        _liveSetting = announcement.Assertiveness == UI.Assertiveness.Assertive
            ? AutomationLiveSetting.Assertive
            : AutomationLiveSetting.Polite;
    }

    protected override void BringIntoViewCore()
    {
    }

    protected override string? GetAcceleratorKeyCore() => null;

    protected override string? GetAccessKeyCore() => null;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    protected override string? GetAutomationIdCore() => null;

    protected override Rect GetBoundingRectangleCore() => default;

    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => [];

    protected override string GetClassNameCore() => "FlutterAnnouncement";

    protected override AutomationPeer? GetLabeledByCore() => null;

    protected override string? GetNameCore() => Message;

    protected override AutomationPeer? GetParentCore() => host;

    protected override bool HasKeyboardFocusCore() => false;

    protected override bool IsContentElementCore() => false;

    protected override bool IsControlElementCore() => false;

    protected override bool IsEnabledCore() => true;

    protected override bool IsKeyboardFocusableCore() => false;

    protected override bool IsOffscreenCore() => true;

    protected override void SetFocusCore()
    {
    }

    protected override bool ShowContextMenuCore() => false;

    protected override AutomationLiveSetting GetLiveSettingCore() => _liveSetting;

    protected override bool TrySetParent(AutomationPeer? parent) => true;
}
