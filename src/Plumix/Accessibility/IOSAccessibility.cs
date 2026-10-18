using Plumix.Rendering;

// C#-only infrastructure: the UIKit-free half of the iOS accessibility bridge — which object kind a
// node gets, what VoiceOver reads for it, whether it is an element, how its actions map, and which
// notifications an update posts — so that it can be tested off-device. It follows the engine's
// engine/src/flutter/shell/platform/darwin/ios/framework/Source/SemanticsObject.mm and
// accessibility_bridge.mm; the iOS host turns it into `UIAccessibilityElement`s.

namespace Plumix.Accessibility;

/// <summary>The kind of accessibility object the iOS bridge creates for a node.</summary>
/// <remarks>
/// The engine's <c>AccessibilityBridge::CreateObject</c> choice; a change of kind replaces the object.
/// </remarks>
public enum IOSSemanticsObjectKind
{
    /// <summary>A <c>FlutterSemanticsObject</c>.</summary>
    Plain,

    /// <summary>A <c>TextInputSemanticsObject</c>: an editable text field.</summary>
    TextInput,

    /// <summary>A <c>FlutterSwitchSemanticsObject</c>, which reports a real <c>UISwitch</c>'s traits.</summary>
    Switch,

    /// <summary>A <c>FlutterScrollableSemanticsObject</c>: a node with implicit scrolling.</summary>
    Scrollable,
}

/// <summary>UIKit's <c>UIAccessibilityTraits</c> bits the iOS bridge sets.</summary>
[Flags]
public enum IOSAccessibilityTraits : long
{
    /// <summary><c>UIAccessibilityTraitNone</c>.</summary>
    None = 0,

    /// <summary><c>UIAccessibilityTraitButton</c>.</summary>
    Button = 1 << 0,

    /// <summary><c>UIAccessibilityTraitLink</c>.</summary>
    Link = 1 << 1,

    /// <summary><c>UIAccessibilityTraitImage</c>.</summary>
    Image = 1 << 2,

    /// <summary><c>UIAccessibilityTraitSelected</c>.</summary>
    Selected = 1 << 3,

    /// <summary><c>UIAccessibilityTraitKeyboardKey</c>.</summary>
    KeyboardKey = 1 << 5,

    /// <summary><c>UIAccessibilityTraitStaticText</c>.</summary>
    StaticText = 1 << 6,

    /// <summary><c>UIAccessibilityTraitNotEnabled</c>.</summary>
    NotEnabled = 1 << 8,

    /// <summary><c>UIAccessibilityTraitUpdatesFrequently</c>.</summary>
    UpdatesFrequently = 1 << 9,

    /// <summary><c>UIAccessibilityTraitAdjustable</c>.</summary>
    Adjustable = 1 << 12,

    /// <summary><c>UIAccessibilityTraitHeader</c>.</summary>
    Header = 1 << 16,
}

/// <summary><c>UIAccessibilityScrollDirection</c>, in UIKit's numbering.</summary>
public enum IOSAccessibilityScrollDirection
{
    /// <summary><c>UIAccessibilityScrollDirectionRight</c>.</summary>
    Right = 1,

    /// <summary><c>UIAccessibilityScrollDirectionLeft</c>.</summary>
    Left = 2,

    /// <summary><c>UIAccessibilityScrollDirectionUp</c>.</summary>
    Up = 3,

    /// <summary><c>UIAccessibilityScrollDirectionDown</c>.</summary>
    Down = 4,

    /// <summary><c>UIAccessibilityScrollDirectionNext</c>.</summary>
    Next = 5,

    /// <summary><c>UIAccessibilityScrollDirectionPrevious</c>.</summary>
    Previous = 6,
}

/// <summary>What the iOS bridge reads and does for one semantics node.</summary>
/// <remarks>The engine's <c>SemanticsObject</c>/<c>FlutterSemanticsObject</c> property and action overrides.</remarks>
public static class IOSSemanticsObject
{
    private const SemanticsActions SystemActions =
        SemanticsActions.DidGainAccessibilityFocus | SemanticsActions.DidLoseAccessibilityFocus;

    /// <summary>The object kind the engine creates for <paramref name="node"/>, first match wins.</summary>
    public static IOSSemanticsObjectKind KindOf(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsEditableTextField)
        {
            return IOSSemanticsObjectKind.TextInput;
        }

        if (!node.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)
            && node.HasFlag(SemanticsFlags.HasToggledState | SemanticsFlags.HasCheckedState))
        {
            return IOSSemanticsObjectKind.Switch;
        }

        return node.HasFlag(SemanticsFlags.HasImplicitScrolling)
            ? IOSSemanticsObjectKind.Scrollable
            : IOSSemanticsObjectKind.Plain;
    }

    /// <summary>Whether VoiceOver should treat the node as an element: <c>isAccessibilityElement</c>.</summary>
    /// <remarks>A route scope never is; otherwise the node is when it is focusable.</remarks>
    public static bool IsAccessibilityElement(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return !node.HasFlag(SemanticsFlags.ScopesRoute) && IsFocusable(node);
    }

    /// <summary>
    /// The engine's <c>isFocusable</c>: a hidden implicitly scrolling node, a node with a label, value or
    /// hint, or a node with any action other than scrolling.
    /// </summary>
    public static bool IsFocusable(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return (node.HasFlag(SemanticsFlags.HasImplicitScrolling) && node.HasFlag(SemanticsFlags.IsHidden))
               || node.Label.Length > 0
               || node.Value.Length > 0
               || node.Hint.Length > 0
               || (node.Actions & ~AccessibilityNode.ScrollActions) != SemanticsActions.None;
    }

    /// <summary><c>accessibilityLabel</c>: the label, then the tooltip on its own line.</summary>
    public static string? Label(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        string label = node.Label;
        string tooltip = node.Tooltip;
        if (tooltip.Length == 0)
        {
            return label.Length == 0 ? null : label;
        }

        return label.Length == 0 ? tooltip : label + "\n" + tooltip;
    }

    /// <summary><c>accessibilityHint</c>.</summary>
    public static string? Hint(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Hint.Length == 0 ? null : node.Hint;
    }

    /// <summary>
    /// <c>accessibilityValue</c>: the value, else nothing for a radio button, else <c>"1"</c> or
    /// <c>"0"</c> for a toggleable or checkable node.
    /// </summary>
    public static string? Value(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Value.Length > 0)
        {
            return node.Value;
        }

        if (node.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup))
        {
            return null;
        }

        bool toggleable = node.HasFlag(SemanticsFlags.HasToggledState);
        bool checkable = node.HasFlag(SemanticsFlags.HasCheckedState);
        if ((toggleable && node.HasFlag(SemanticsFlags.IsToggled))
            || (checkable && node.HasFlag(SemanticsFlags.IsChecked)))
        {
            return "1";
        }

        return toggleable || checkable ? "0" : null;
    }

    /// <summary>The <c>accessibilityTraits</c> of a <see cref="IOSSemanticsObjectKind.Plain"/> object.</summary>
    /// <remarks>
    /// <c>FlutterSemanticsObject.accessibilityTraits</c>. A heading level, not the header flag, gives the
    /// header trait; a childless labelled node with no other trait is static text.
    /// </remarks>
    public static IOSAccessibilityTraits Traits(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        IOSAccessibilityTraits traits = IOSAccessibilityTraits.None;
        if (node.HasAction(SemanticsActions.Increase | SemanticsActions.Decrease))
        {
            traits |= IOSAccessibilityTraits.Adjustable;
        }

        if (node.HasFlag(SemanticsFlags.HasToggledState | SemanticsFlags.HasCheckedState))
        {
            traits |= IOSAccessibilityTraits.Button;
        }

        if (node.IsSelected)
        {
            traits |= IOSAccessibilityTraits.Selected;
        }

        if (node.HasFlag(SemanticsFlags.IsButton))
        {
            traits |= IOSAccessibilityTraits.Button;
        }

        if (node.HasFlag(SemanticsFlags.HasEnabledState) && !node.HasFlag(SemanticsFlags.IsEnabled))
        {
            traits |= IOSAccessibilityTraits.NotEnabled;
        }

        if (node.Data.HeadingLevel > 0)
        {
            traits |= IOSAccessibilityTraits.Header;
        }

        if (node.HasFlag(SemanticsFlags.IsImage))
        {
            traits |= IOSAccessibilityTraits.Image;
        }

        if (node.HasFlag(SemanticsFlags.IsLiveRegion))
        {
            traits |= IOSAccessibilityTraits.UpdatesFrequently;
        }

        if (node.HasFlag(SemanticsFlags.IsLink))
        {
            traits |= IOSAccessibilityTraits.Link;
        }

        if (traits == IOSAccessibilityTraits.None
            && !node.HasChildren
            && node.Label.Length > 0
            && !node.HasFlag(SemanticsFlags.IsTextField))
        {
            traits = IOSAccessibilityTraits.StaticText;
        }

        return traits;
    }

    /// <summary><c>accessibilityRespondsToUserInteraction</c>.</summary>
    public static bool RespondsToUserInteraction(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked))
        {
            return false;
        }

        return (node.Actions & ~SystemActions) != SemanticsActions.None
               || node.Bridge.GetCustomActions(node).Count > 0;
    }

    /// <summary><c>accessibilityActivate</c>: taps the node; a slider swallows the activation.</summary>
    public static bool Activate(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.HasAction(SemanticsActions.Tap))
        {
            node.PerformAction(SemanticsActions.Tap);
            return true;
        }

        return node.HasFlag(SemanticsFlags.IsSlider);
    }

    /// <summary><c>accessibilityIncrement</c>/<c>accessibilityDecrement</c>.</summary>
    public static void Adjust(AccessibilityNode node, bool increment)
    {
        ArgumentNullException.ThrowIfNull(node);
        SemanticsActions action = increment ? SemanticsActions.Increase : SemanticsActions.Decrease;
        if (node.HasAction(action))
        {
            node.PerformAction(action);
        }
    }

    /// <summary>The Flutter action for a VoiceOver scroll in <paramref name="direction"/>.</summary>
    public static SemanticsActions ScrollActionFor(IOSAccessibilityScrollDirection direction) => direction switch
    {
        IOSAccessibilityScrollDirection.Right or IOSAccessibilityScrollDirection.Previous =>
            SemanticsActions.ScrollRight,
        IOSAccessibilityScrollDirection.Left or IOSAccessibilityScrollDirection.Next => SemanticsActions.ScrollLeft,
        IOSAccessibilityScrollDirection.Up => SemanticsActions.ScrollDown,
        IOSAccessibilityScrollDirection.Down => SemanticsActions.ScrollUp,
        _ => SemanticsActions.None,
    };

    /// <summary><c>accessibilityScroll:</c>: whether the node scrolled.</summary>
    public static bool Scroll(AccessibilityNode node, IOSAccessibilityScrollDirection direction)
    {
        ArgumentNullException.ThrowIfNull(node);
        SemanticsActions action = ScrollActionFor(direction);
        if (action == SemanticsActions.None || !node.HasAction(action))
        {
            return false;
        }

        node.PerformAction(action);
        return true;
    }

    /// <summary><c>accessibilityPerformEscape</c>: dismisses the node when it can be dismissed.</summary>
    public static bool PerformEscape(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!node.HasAction(SemanticsActions.Dismiss))
        {
            return false;
        }

        node.PerformAction(SemanticsActions.Dismiss);
        return true;
    }

    /// <summary><c>onCustomAccessibilityAction:</c>.</summary>
    public static bool PerformCustomAction(AccessibilityNode node, int customActionId)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!node.HasAction(SemanticsActions.CustomAction))
        {
            return false;
        }

        node.PerformAction(SemanticsActions.CustomAction, customActionId);
        return true;
    }

    /// <summary><c>accessibilityElementDidBecomeFocused</c>.</summary>
    /// <remarks>
    /// Records the focus and reports it to the framework, scrolls a hidden node or a header into view,
    /// and sends the node its gain-focus action.
    /// </remarks>
    public static void DidBecomeFocused(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Bridge.ReportAccessibilityFocus(node.Id);
        if (node.HasFlag(SemanticsFlags.IsHidden | SemanticsFlags.IsHeader))
        {
            node.PerformAction(SemanticsActions.ShowOnScreen);
        }

        if (node.HasAction(SemanticsActions.DidGainAccessibilityFocus))
        {
            node.PerformAction(SemanticsActions.DidGainAccessibilityFocus);
        }
    }

    /// <summary><c>accessibilityElementDidLoseFocus</c>.</summary>
    public static void DidLoseFocus(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Bridge.ClearLastFocusedId(node.Id);
        if (node.HasAction(SemanticsActions.DidLoseAccessibilityFocus))
        {
            node.PerformAction(SemanticsActions.DidLoseAccessibilityFocus);
        }
    }

    /// <summary>
    /// <c>routeName</c>: the node's own label when it names a route, else the first route name found
    /// among its descendants in traversal order.
    /// </summary>
    public static string? RouteName(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.HasFlag(SemanticsFlags.NamesRoute) && Label(node) is { Length: > 0 } label)
        {
            return label;
        }

        foreach (AccessibilityNode child in node.Children)
        {
            if (RouteName(child) is { } name)
            {
                return name;
            }
        }

        return null;
    }
}

/// <summary>The notifications one commit asks the iOS bridge to post.</summary>
/// <param name="ScreenChanged">Whether to post <c>UIAccessibilityScreenChangedNotification</c>.</param>
/// <param name="RouteName">The screen-changed argument: the new route's name.</param>
/// <param name="LayoutChanged">Whether to post <c>UIAccessibilityLayoutChangedNotification</c>.</param>
/// <param name="PageScrolled">Whether to post <c>UIAccessibilityPageScrolledNotification</c>.</param>
/// <param name="FocusTarget">The layout-changed or page-scrolled argument: the element to focus, or none.</param>
/// <param name="Announcements">Live-region labels to announce, queued.</param>
public sealed record IOSAccessibilityNotifications(
    bool ScreenChanged,
    string? RouteName,
    bool LayoutChanged,
    bool PageScrolled,
    AccessibilityNode? FocusTarget,
    IReadOnlyList<string> Announcements);

/// <summary>The route and focus bookkeeping the iOS bridge keeps across commits.</summary>
/// <remarks>The notification half of the engine's <c>AccessibilityBridge::UpdateSemantics</c>.</remarks>
public sealed class IOSAccessibilityNotifier
{
    private List<int> _previousRoutes = [];
    private int _previousRouteId;

    /// <summary>Works out what <paramref name="update"/> should announce.</summary>
    /// <remarks>
    /// A route change is the last newly added route scope (or the last route scope, when none is new)
    /// differing from the previous one; a layout change moves focus to the first element at or below
    /// the last focused node, and is reported without an element when focus would not move.
    /// </remarks>
    public IOSAccessibilityNotifications Plan(AccessibilityBridge bridge, AccessibilityTreeUpdate update)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        ArgumentNullException.ThrowIfNull(update);
        var announcements = new List<string>();
        foreach (AccessibilityEvent change in update.Events)
        {
            if (change.Type == AccessibilityEventType.LiveRegionChanged)
            {
                announcements.Add(change.Node.Label);
            }
        }

        bool routeChanged = false;
        AccessibilityNode? lastAdded = null;
        if (bridge.Root is { } root)
        {
            var newRoutes = new List<AccessibilityNode>();
            CollectRoutes(root, newRoutes);
            var previous = new HashSet<int>(_previousRoutes);
            lastAdded = newRoutes.LastOrDefault(route => !previous.Contains(route.Id)) ?? newRoutes.LastOrDefault();
            routeChanged = lastAdded is not null
                           && (lastAdded.Id != _previousRouteId || newRoutes.Count != _previousRoutes.Count);
            if (routeChanged)
            {
                _previousRouteId = lastAdded!.Id;
            }

            _previousRoutes = newRoutes.Select(static route => route.Id).ToList();
        }

        bool layoutChanged = update.LayoutChanged;
        bool scrolled = update.Events.Any(static e => e.Type == AccessibilityEventType.ScrollPositionChanged);
        AccessibilityNode? next = layoutChanged || scrolled ? FindNextFocusableIfNecessary(bridge) : null;
        AccessibilityNode? target = null;
        if (layoutChanged)
        {
            AccessibilityNode? lastFocused = bridge.GetNode(bridge.LastFocusedId);
            target = routeChanged || !ReferenceEquals(next, lastFocused) ? next : null;
        }
        else if (scrolled)
        {
            target = next;
        }

        return new IOSAccessibilityNotifications(
            ScreenChanged: routeChanged,
            RouteName: routeChanged ? IOSSemanticsObject.RouteName(lastAdded!) : null,
            LayoutChanged: layoutChanged,
            PageScrolled: !layoutChanged && scrolled,
            FocusTarget: target,
            Announcements: announcements);
    }

    /// <summary>Forgets every route, as the engine's <c>clearState</c> does.</summary>
    public void Reset()
    {
        _previousRoutes = [];
        _previousRouteId = 0;
    }

    private static void CollectRoutes(AccessibilityNode node, List<AccessibilityNode> routes)
    {
        if (node.HasFlag(SemanticsFlags.ScopesRoute))
        {
            routes.Add(node);
        }

        foreach (AccessibilityNode child in node.Children)
        {
            CollectRoutes(child, routes);
        }
    }

    // `FindNextFocusableIfNecessary`: nothing while nothing has accessibility focus; otherwise the first
    // element at or below the last focused node, or below the root once that node is gone.
    private static AccessibilityNode? FindNextFocusableIfNecessary(AccessibilityBridge bridge)
    {
        if (bridge.LastFocusedId == -1)
        {
            return null;
        }

        AccessibilityNode? start = bridge.GetNode(bridge.LastFocusedId) ?? bridge.Root;
        return start is null ? null : FindFirstFocusable(start);
    }

    private static AccessibilityNode? FindFirstFocusable(AccessibilityNode node)
    {
        if (IOSSemanticsObject.IsAccessibilityElement(node))
        {
            return node;
        }

        foreach (AccessibilityNode child in node.Children)
        {
            if (FindFirstFocusable(child) is { } focusable)
            {
                return focusable;
            }
        }

        return null;
    }
}
