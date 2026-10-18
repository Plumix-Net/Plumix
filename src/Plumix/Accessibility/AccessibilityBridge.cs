using System.Collections;
using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only infrastructure: the engine's platform-independent accessibility bridge
// (engine/src/flutter/shell/platform/common/accessibility_bridge.cc and its AXEventGenerator diff),
// plus the embedder plumbing every engine bridge shares — turning semantics on when an assistive
// technology asks (`UpdateSemanticsEnabled`), dispatching actions back to the framework
// (`DispatchSemanticsAction`) and the platform side of `flutter/accessibility`. The per-platform
// bridges (the Avalonia automation peers, the iOS and browser hosts) read the tree it mirrors.

namespace Plumix.Accessibility;

/// <summary>The kind of change an <see cref="AccessibilityEvent"/> reports.</summary>
/// <remarks>
/// The engine's <c>ui::AXEventGenerator::Event</c> values its bridges act on, plus the bounds, scroll,
/// hint, enabled and live-region changes the Avalonia, iOS and browser bridges need.
/// </remarks>
public enum AccessibilityEventType
{
    /// <summary>The node is the top of a subtree that joined the tree.</summary>
    SubtreeCreated,

    /// <summary>The node's children, or their order, changed.</summary>
    ChildrenChanged,

    /// <summary>The node's <see cref="AccessibilityNode.Role"/> changed.</summary>
    RoleChanged,

    /// <summary>The node's label or tooltip changed (never raised for the root).</summary>
    NameChanged,

    /// <summary>The node's value changed.</summary>
    ValueChanged,

    /// <summary>The node's hint changed.</summary>
    HintChanged,

    /// <summary>The node's <see cref="AccessibilityNode.CheckedState"/> changed.</summary>
    CheckedStateChanged,

    /// <summary>The node's <see cref="AccessibilityNode.IsSelected"/> changed.</summary>
    SelectedChanged,

    /// <summary>The node's <see cref="AccessibilityNode.IsExpanded"/> changed.</summary>
    ExpandedChanged,

    /// <summary>The node's <see cref="AccessibilityNode.IsEnabled"/> changed.</summary>
    EnabledChanged,

    /// <summary>The node's <see cref="AccessibilityNode.IsIgnored"/> changed.</summary>
    IgnoredChanged,

    /// <summary>The node gained input focus.</summary>
    FocusChanged,

    /// <summary>A live region appeared or its label changed, so its label should be announced.</summary>
    LiveRegionChanged,

    /// <summary>The node's rect or transform changed.</summary>
    BoundsChanged,

    /// <summary>The node's scroll position changed.</summary>
    ScrollPositionChanged,

    /// <summary>The node's actions or custom actions changed.</summary>
    ActionsChanged,
}

/// <summary>A change to one node, produced by an <see cref="AccessibilityBridge"/> commit.</summary>
public readonly record struct AccessibilityEvent(AccessibilityEventType Type, AccessibilityNode Node);

/// <summary>What one <see cref="AccessibilityBridge.AddSemanticsUpdate"/> commit changed.</summary>
public sealed class AccessibilityTreeUpdate
{
    internal AccessibilityTreeUpdate(
        IReadOnlyList<AccessibilityEvent> events,
        IReadOnlyList<AccessibilityNode> updatedNodes,
        IReadOnlyList<AccessibilityNode> createdNodes,
        IReadOnlyList<AccessibilityNode> removedNodes,
        bool rootChanged)
    {
        Events = events;
        UpdatedNodes = updatedNodes;
        CreatedNodes = createdNodes;
        RemovedNodes = removedNodes;
        RootChanged = rootChanged;
    }

    /// <summary>The changes, in the order the commit found them.</summary>
    public IReadOnlyList<AccessibilityEvent> Events { get; }

    /// <summary>The nodes the update carried that are still in the tree.</summary>
    public IReadOnlyList<AccessibilityNode> UpdatedNodes { get; }

    /// <summary>The nodes among <see cref="UpdatedNodes"/> that joined the tree in this commit.</summary>
    public IReadOnlyList<AccessibilityNode> CreatedNodes { get; }

    /// <summary>The nodes that left the tree.</summary>
    public IReadOnlyList<AccessibilityNode> RemovedNodes { get; }

    /// <summary>Whether the tree gained or lost its root.</summary>
    public bool RootChanged { get; }

    /// <summary>Whether any node joined, moved, resized or left the tree.</summary>
    /// <remarks>
    /// The iOS bridge's <c>layoutChanged</c>: a node whose rect or transform differs from its previous
    /// update (a new node differs from the empty one), or any removal.
    /// </remarks>
    public bool LayoutChanged =>
        RemovedNodes.Count > 0
        || CreatedNodes.Count > 0
        || Events.Any(static e => e.Type == AccessibilityEventType.BoundsChanged);
}

/// <summary>An announcement the framework asked the platform to make.</summary>
/// <remarks>The <c>announce</c> message of <c>flutter/accessibility</c>.</remarks>
public sealed record AccessibilityAnnouncement(
    string Message,
    TextDirection TextDirection,
    Assertiveness Assertiveness);

/// <summary>
/// The host-side mirror of one view's semantics tree, which the platform accessibility bridges read.
/// </summary>
/// <remarks>
/// <para>
/// The engine's <c>AccessibilityBridge</c>: <see cref="AddSemanticsUpdate"/> applies a framework
/// <see cref="SemanticsUpdate"/> and commits it, wiring children in traversal order, dropping every node
/// the root no longer reaches, and diffing each surviving node against its previous update the way the
/// engine's <c>AXEventGenerator</c> does.
/// </para>
/// <para>
/// A platform bridge calls <see cref="Activate"/> when an assistive technology first queries the view
/// (the engine embedders' <c>UpdateSemanticsEnabled(true)</c>), and sends actions back through
/// <see cref="DispatchAction"/>.
/// </para>
/// </remarks>
public sealed class AccessibilityBridge
{
    private const string ChannelName = "flutter/accessibility";

    private static readonly List<WeakReference<AccessibilityBridge>> s_activeBridges = [];
    private static BinaryMessenger? s_channelMessenger;
    private static MessageHandler? s_channelHandler;

    private readonly Dictionary<int, AccessibilityNode> _nodes = [];
    private readonly Dictionary<int, CustomSemanticsActionUpdate> _customActions = [];
    private readonly Func<double> _devicePixelRatio;
    private int _lastFocusedId = -1;

    /// <summary>Creates a bridge for the view <paramref name="viewId"/>.</summary>
    /// <param name="viewId">The id of the view whose semantics updates this bridge receives.</param>
    /// <param name="devicePixelRatio">The view's current device pixel ratio.</param>
    public AccessibilityBridge(int viewId, Func<double>? devicePixelRatio = null)
    {
        ViewId = viewId;
        _devicePixelRatio = devicePixelRatio ?? (static () => 1.0);
    }

    /// <summary>The id of the view this bridge mirrors.</summary>
    public int ViewId { get; }

    /// <summary>The view's device pixel ratio, which the tree's physical pixels are divided by.</summary>
    public double DevicePixelRatio => _devicePixelRatio();

    /// <summary>The root of the tree, or <c>null</c> before the first update.</summary>
    public AccessibilityNode? Root { get; private set; }

    /// <summary>The number of nodes in the tree.</summary>
    public int NodeCount => _nodes.Count;

    /// <summary>Whether a platform bridge has activated this bridge.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The id of the node with input focus, or <c>-1</c>.</summary>
    /// <remarks>The engine's tree <c>focus_id</c>, set from the <see cref="SemanticsFlags.IsFocused"/> flag.</remarks>
    public int FocusedNodeId { get; private set; } = -1;

    /// <summary>The id of the node the platform last moved accessibility focus to, or <c>-1</c>.</summary>
    /// <remarks>The engine's <c>last_focused_id_</c>, set by <see cref="SetLastFocusedId"/>.</remarks>
    public int LastFocusedId => _lastFocusedId;

    /// <summary>The custom actions the framework has described, by id.</summary>
    public IReadOnlyDictionary<int, CustomSemanticsActionUpdate> CustomActions => _customActions;

    /// <summary>Raised after every commit, with what it changed.</summary>
    public event Action<AccessibilityTreeUpdate>? Committed;

    /// <summary>Raised when the tree is cleared because semantics were turned off.</summary>
    public event Action? Cleared;

    /// <summary>Raised when the framework asks the platform to announce a message for this view.</summary>
    public event Action<AccessibilityAnnouncement>? AnnouncementRequested;

    /// <summary>Raised when the framework asks the platform to move accessibility focus to a node.</summary>
    /// <remarks>The <c>focus</c> message of <c>flutter/accessibility</c>, which only Android and iOS act on.</remarks>
    public event Action<AccessibilityNode>? FocusRequested;

    /// <summary>The node with id <paramref name="id"/>, or <c>null</c>.</summary>
    public AccessibilityNode? GetNode(int id) => _nodes.GetValueOrDefault(id);

    /// <summary>
    /// Turns semantics on for the application and makes this bridge the platform side of
    /// <c>flutter/accessibility</c> for its view.
    /// </summary>
    /// <remarks>
    /// The engine embedders' <c>UpdateSemanticsEnabled(true)</c>: Windows calls it on the first
    /// <c>WM_GETOBJECT</c>, iOS when VoiceOver runs, the web engine when the placeholder is activated.
    /// Activation is never undone, as on Windows.
    /// </remarks>
    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            lock (s_activeBridges)
            {
                s_activeBridges.Add(new WeakReference<AccessibilityBridge>(this));
            }
        }

        EnsureChannelHandler();
        PlatformDispatcher.Instance.UpdateSemanticsEnabled(true);
    }

    /// <summary>Turns semantics off again, as the iOS and macOS embedders do when VoiceOver stops.</summary>
    /// <remarks>
    /// The framework then disposes its semantics owner, which <see cref="PlumixHost"/> answers with
    /// <see cref="Clear"/>. The bridge stays the platform side of <c>flutter/accessibility</c>.
    /// </remarks>
    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        lock (s_activeBridges)
        {
            s_activeBridges.RemoveAll(reference =>
                !reference.TryGetTarget(out AccessibilityBridge? bridge) || ReferenceEquals(bridge, this));
        }

        if (ActiveBridges().Count == 0)
        {
            PlatformDispatcher.Instance.UpdateSemanticsEnabled(false);
        }
    }

    /// <summary>Applies one framework semantics update and commits it.</summary>
    /// <remarks>The engine's <c>AddFlutterSemanticsNodeUpdate</c> for every node, then <c>CommitUpdates</c>.</remarks>
    public void AddSemanticsUpdate(SemanticsUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        foreach (CustomSemanticsActionUpdate action in update.CustomActions)
        {
            _customActions[action.Id] = action;
        }

        if (update.Nodes.Count == 0)
        {
            return;
        }

        // Snapshot every node the update touches before any of it is applied.
        var previous = new Dictionary<int, NodeSnapshot>(update.Nodes.Count);
        var created = new HashSet<int>();
        foreach (SemanticsNodeUpdate nodeUpdate in update.Nodes)
        {
            if (_nodes.TryGetValue(nodeUpdate.Id, out AccessibilityNode? existing))
            {
                previous[nodeUpdate.Id] = new NodeSnapshot(existing);
            }
            else
            {
                created.Add(nodeUpdate.Id);
                _nodes[nodeUpdate.Id] = new AccessibilityNode(this, nodeUpdate.Id);
            }

            _nodes[nodeUpdate.Id].Update(nodeUpdate);
        }

        bool rootChanged = false;
        if (Root is null || !Root.IsAttached)
        {
            // The engine keeps the root of the first commit; the framework's root is always the
            // shallowest node of its first update.
            Root = _nodes[update.Nodes[0].Id];
            rootChanged = true;
        }

        foreach (SemanticsNodeUpdate nodeUpdate in update.Nodes)
        {
            AccessibilityNode node = _nodes[nodeUpdate.Id];
            node.SetChildren(ResolveChildren(node.ChildIds), ResolveChildren(node.HitTestChildIds));
        }

        // AXTree::Unserialize: parent links follow the traversal children, and every node the root no
        // longer reaches leaves the tree with its subtree.
        var reachable = new HashSet<int>();
        LinkReachable(Root, parent: null, reachable);
        var removed = new List<AccessibilityNode>();
        foreach (KeyValuePair<int, AccessibilityNode> entry in _nodes.ToArray())
        {
            if (reachable.Contains(entry.Key))
            {
                continue;
            }

            _nodes.Remove(entry.Key);
            entry.Value.IsAttached = false;
            entry.Value.Parent = null;
            if (!created.Contains(entry.Key))
            {
                removed.Add(entry.Value);
            }
        }

        var events = new List<AccessibilityEvent>();
        var updatedNodes = new List<AccessibilityNode>(update.Nodes.Count);
        var createdNodes = new List<AccessibilityNode>();
        foreach (SemanticsNodeUpdate nodeUpdate in update.Nodes)
        {
            if (!_nodes.TryGetValue(nodeUpdate.Id, out AccessibilityNode? node))
            {
                continue;
            }

            updatedNodes.Add(node);
            if (previous.TryGetValue(node.Id, out NodeSnapshot snapshot))
            {
                AddChangeEvents(node, snapshot, events);
            }
            else
            {
                createdNodes.Add(node);
                if (node.Parent is null || !created.Contains(node.Parent.Id))
                {
                    events.Add(new AccessibilityEvent(AccessibilityEventType.SubtreeCreated, node));
                }

                if (node.HasFlag(SemanticsFlags.IsLiveRegion))
                {
                    events.Add(new AccessibilityEvent(AccessibilityEventType.LiveRegionChanged, node));
                }
            }
        }

        UpdateFocus(updatedNodes, events);

        Committed?.Invoke(new AccessibilityTreeUpdate(events, updatedNodes, createdNodes, removed, rootChanged));
    }

    /// <summary>Drops the whole tree, as the embedders do when semantics are turned off.</summary>
    public void Clear()
    {
        if (_nodes.Count == 0 && Root is null)
        {
            return;
        }

        foreach (AccessibilityNode node in _nodes.Values)
        {
            node.IsAttached = false;
            node.Parent = null;
        }

        _nodes.Clear();
        _customActions.Clear();
        Root = null;
        FocusedNodeId = -1;
        _lastFocusedId = -1;
        Cleared?.Invoke();
    }

    /// <summary>
    /// Asks the framework to perform <paramref name="action"/> on the node <paramref name="nodeId"/>.
    /// </summary>
    /// <remarks>
    /// The engine's <c>DispatchAccessibilityAction</c>: the action reaches the framework as a
    /// <see cref="PlatformDispatcher.OnSemanticsActionEvent"/> for this bridge's view.
    /// </remarks>
    public void DispatchAction(int nodeId, SemanticsActions action, object? arguments = null)
    {
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        PlatformDispatcher.Instance.DispatchSemanticsActionEvent(
            new SemanticsActionEvent(action, ViewId, nodeId, arguments));
    }

    /// <summary>Moves the platform's accessibility focus to the node <paramref name="nodeId"/>.</summary>
    /// <remarks>
    /// The engine's <c>kFocus</c> action: the previously focused node, if it still exists, is told it
    /// lost accessibility focus, then the new node is told it gained it.
    /// </remarks>
    public void SetLastFocusedId(int nodeId)
    {
        if (_lastFocusedId != nodeId)
        {
            if (_lastFocusedId != -1 && _nodes.ContainsKey(_lastFocusedId))
            {
                DispatchAction(_lastFocusedId, SemanticsActions.DidLoseAccessibilityFocus);
            }

            _lastFocusedId = nodeId;
        }

        DispatchAction(nodeId, SemanticsActions.DidGainAccessibilityFocus);
    }

    /// <summary>Forgets the platform's accessibility focus if it is on <paramref name="nodeId"/>.</summary>
    /// <remarks>The iOS bridge's <c>AccessibilityObjectDidLoseFocus</c>.</remarks>
    public void ClearLastFocusedId(int nodeId)
    {
        if (_lastFocusedId == nodeId)
        {
            _lastFocusedId = -1;
        }
    }

    /// <summary>
    /// Tells the framework the platform moved accessibility focus to the node <paramref name="nodeId"/>.
    /// </summary>
    /// <remarks>
    /// The iOS bridge's <c>AccessibilityObjectDidBecomeFocused</c>: records the node as the last focused
    /// one and sends <c>{type: didGainFocus, nodeId}</c> on <c>flutter/accessibility</c>.
    /// </remarks>
    public void ReportAccessibilityFocus(int nodeId)
    {
        _lastFocusedId = nodeId;
        using EventLoop.FrameworkThreadScope scope = EventLoop.EnterFrameworkThread();
        BinaryMessenger messenger = ServicesBinding.Instance.DefaultBinaryMessenger;
        ByteData? message = SystemChannels.Accessibility.Codec.EncodeMessage(
            new Dictionary<string, object?> { ["type"] = "didGainFocus", ["nodeId"] = nodeId });
        _ = messenger.HandlePlatformMessage(ChannelName, message, callback: null);
    }

    /// <summary>The custom actions <paramref name="node"/> handles, in id order.</summary>
    /// <remarks>Overriding descriptions of standard actions are left out, as on iOS.</remarks>
    public IReadOnlyList<CustomSemanticsActionUpdate> GetCustomActions(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var actions = new List<CustomSemanticsActionUpdate>(node.CustomActionIds.Count);
        foreach (int id in node.CustomActionIds)
        {
            if (_customActions.TryGetValue(id, out CustomSemanticsActionUpdate? action) && action.OverrideId == -1)
            {
                actions.Add(action);
            }
        }

        return actions;
    }

    internal static void ResetForTests()
    {
        lock (s_activeBridges)
        {
            s_activeBridges.Clear();
        }

        if (s_channelMessenger is { } messenger && s_channelHandler is not null)
        {
            messenger.SetPlatformMessageHandler(ChannelName, null);
        }

        s_channelMessenger = null;
        s_channelHandler = null;
    }

    private IReadOnlyList<AccessibilityNode> ResolveChildren(IReadOnlyList<int> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var children = new List<AccessibilityNode>(ids.Count);
        foreach (int id in ids)
        {
            if (_nodes.TryGetValue(id, out AccessibilityNode? child))
            {
                children.Add(child);
            }
            else
            {
                Debug.Fail($"Semantics node {id} is a child before the framework described it.");
            }
        }

        return children;
    }

    private static void LinkReachable(AccessibilityNode node, AccessibilityNode? parent, HashSet<int> reachable)
    {
        if (!reachable.Add(node.Id))
        {
            return;
        }

        node.Parent = parent;
        foreach (AccessibilityNode child in node.Children)
        {
            LinkReachable(child, node, reachable);
        }
    }

    private void AddChangeEvents(AccessibilityNode node, NodeSnapshot old, List<AccessibilityEvent> events)
    {
        void Add(AccessibilityEventType type) => events.Add(new AccessibilityEvent(type, node));

        if (!old.ChildIds.SequenceEqual(node.ChildIds))
        {
            Add(AccessibilityEventType.ChildrenChanged);
        }

        if (old.Role != node.Role)
        {
            Add(AccessibilityEventType.RoleChanged);
        }

        if (old.IsIgnored != node.IsIgnored)
        {
            Add(AccessibilityEventType.IgnoredChanged);
        }

        if (!ReferenceEquals(node, Root) && (old.Data.Label != node.Label || old.Data.Tooltip != node.Tooltip))
        {
            Add(AccessibilityEventType.NameChanged);
        }

        if (old.Data.Value != node.Value)
        {
            Add(AccessibilityEventType.ValueChanged);
        }

        if (old.Data.Hint != node.Hint)
        {
            Add(AccessibilityEventType.HintChanged);
        }

        if (old.CheckedState != node.CheckedState)
        {
            Add(AccessibilityEventType.CheckedStateChanged);
        }

        if (old.IsSelected != node.IsSelected)
        {
            Add(AccessibilityEventType.SelectedChanged);
        }

        if (old.IsExpanded != node.IsExpanded)
        {
            Add(AccessibilityEventType.ExpandedChanged);
        }

        if (old.IsEnabled != node.IsEnabled)
        {
            Add(AccessibilityEventType.EnabledChanged);
        }

        if (old.Data.Rect != node.Rect || !Equals(old.Transform, node.Transform))
        {
            Add(AccessibilityEventType.BoundsChanged);
        }

        if (old.Data.ScrollPosition is { } oldPosition
            && node.Data.ScrollPosition is { } newPosition
            && !double.IsNaN(oldPosition)
            && !double.IsNaN(newPosition)
            && oldPosition != newPosition)
        {
            Add(AccessibilityEventType.ScrollPositionChanged);
        }

        if (old.Data.Actions != node.Actions || !old.CustomActionIds.SequenceEqual(node.CustomActionIds))
        {
            Add(AccessibilityEventType.ActionsChanged);
        }

        // The iOS bridge's `nodeShouldTriggerAnnouncement`.
        if (node.HasFlag(SemanticsFlags.IsLiveRegion)
            && (!old.Data.HasFlag(SemanticsFlags.IsLiveRegion) || old.Data.Label != node.Label))
        {
            Add(AccessibilityEventType.LiveRegionChanged);
        }
    }

    // The engine's `SetTreeData`: a node that reports focus becomes the tree's focus; the focus is
    // dropped when that node stops reporting it or leaves the tree.
    private void UpdateFocus(IReadOnlyList<AccessibilityNode> updatedNodes, List<AccessibilityEvent> events)
    {
        int focusedId = FocusedNodeId;
        foreach (AccessibilityNode node in updatedNodes)
        {
            if (node.IsFocused)
            {
                focusedId = node.Id;
            }
            else if (focusedId == node.Id)
            {
                focusedId = -1;
            }
        }

        if (focusedId != -1 && !_nodes.ContainsKey(focusedId))
        {
            focusedId = -1;
        }

        if (focusedId == FocusedNodeId)
        {
            return;
        }

        FocusedNodeId = focusedId;
        if (focusedId != -1)
        {
            events.Add(new AccessibilityEvent(AccessibilityEventType.FocusChanged, _nodes[focusedId]));
        }
    }

    // The platform side of `flutter/accessibility`, installed once per messenger by the first bridge
    // that is activated, as the engine installs its accessibility channel handler.
    private static void EnsureChannelHandler()
    {
        BinaryMessenger messenger = ServicesBinding.Instance.DefaultBinaryMessenger;
        if (ReferenceEquals(s_channelMessenger, messenger))
        {
            return;
        }

        s_channelMessenger = messenger;
        s_channelHandler = HandleChannelMessage;
        messenger.SetPlatformMessageHandler(ChannelName, s_channelHandler);
    }

    private static Task<ByteData?>? HandleChannelMessage(ByteData? data)
    {
        if (SystemChannels.Accessibility.Codec.DecodeMessage(data) is not IDictionary message
            || message["type"] is not string type)
        {
            return Task.FromResult<ByteData?>(null);
        }

        IDictionary? payload = message.Contains("data") ? message["data"] as IDictionary : null;
        switch (type)
        {
            case "announce":
                if (payload?["message"] is string text)
                {
                    int? viewId = payload.Contains("viewId") ? Convert.ToInt32(payload["viewId"]) : null;
                    var direction = payload.Contains("textDirection")
                        ? (TextDirection)Convert.ToInt32(payload["textDirection"])
                        : TextDirection.Ltr;
                    Assertiveness assertiveness = payload.Contains("assertiveness")
                                                  && Convert.ToInt32(payload["assertiveness"]) == 1
                        ? Assertiveness.Assertive
                        : Assertiveness.Polite;
                    var announcement = new AccessibilityAnnouncement(text, direction, assertiveness);
                    foreach (AccessibilityBridge bridge in ActiveBridges())
                    {
                        if (viewId is null || bridge.ViewId == viewId)
                        {
                            bridge.AnnouncementRequested?.Invoke(announcement);
                            break;
                        }
                    }
                }

                break;
            case "focus":
                if (message.Contains("nodeId") && message["nodeId"] is { } rawNodeId)
                {
                    int nodeId = Convert.ToInt32(rawNodeId);
                    foreach (AccessibilityBridge bridge in ActiveBridges())
                    {
                        if (bridge.GetNode(nodeId) is { } node)
                        {
                            bridge.FocusRequested?.Invoke(node);
                            break;
                        }
                    }
                }

                break;
        }

        return Task.FromResult<ByteData?>(null);
    }

    private static List<AccessibilityBridge> ActiveBridges()
    {
        var bridges = new List<AccessibilityBridge>();
        lock (s_activeBridges)
        {
            s_activeBridges.RemoveAll(reference => !reference.TryGetTarget(out _));
            foreach (WeakReference<AccessibilityBridge> reference in s_activeBridges)
            {
                if (reference.TryGetTarget(out AccessibilityBridge? bridge))
                {
                    bridges.Add(bridge);
                }
            }
        }

        return bridges;
    }

    private readonly struct NodeSnapshot
    {
        public NodeSnapshot(AccessibilityNode node)
        {
            Data = node.Data;
            Transform = node.Transform;
            ChildIds = node.ChildIds;
            CustomActionIds = node.CustomActionIds;
            Role = node.Role;
            IsIgnored = node.IsIgnored;
            CheckedState = node.CheckedState;
            IsSelected = node.IsSelected;
            IsExpanded = node.IsExpanded;
            IsEnabled = node.IsEnabled;
        }

        public SemanticsData Data { get; }

        public Matrix4? Transform { get; }

        public IReadOnlyList<int> ChildIds { get; }

        public IReadOnlyList<int> CustomActionIds { get; }

        public AccessibilityRole Role { get; }

        public bool IsIgnored { get; }

        public AccessibilityCheckedState CheckedState { get; }

        public bool IsSelected { get; }

        public bool? IsExpanded { get; }

        public bool IsEnabled { get; }
    }
}
