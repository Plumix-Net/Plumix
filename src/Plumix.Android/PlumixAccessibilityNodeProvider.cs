using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Plumix.Accessibility;
using Plumix.Rendering;
using Plumix.UI;
using AndroidRect = Android.Graphics.Rect;
using AccessibilityAction = Android.Views.Accessibility.AccessibilityNodeInfo.AccessibilityAction;
using AccessibilityEvent = Android.Views.Accessibility.AccessibilityEvent;

namespace Plumix.Android;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/view/AccessibilityBridge.java and its role
// configurators (BaseRoleConfigurator.java, RoleConfiguratorFactory.java) at the pinned Flutter 3.47: the virtual
// node tree TalkBack reads, its actions, accessibility focus, touch-exploration hover and the events each semantics
// commit sends. The tree itself is the core's platform-agnostic AccessibilityBridge (the engine's common bridge);
// this class is only its Android face. Platform views are out of scope, as are the Java bridge's quirks that read
// as bugs (`systemAction` always 0, input focus never cleared when the flag drops): those follow the intent.

/// <summary>Exposes the view's semantics tree to Android accessibility services as virtual views.</summary>
internal sealed class PlumixAccessibilityNodeProvider : AccessibilityNodeProvider
{
    private const int HostViewId = View.NoId;
    private const int RootNodeId = 0;

    // AccessibilityBridge.FIRST_RESOURCE_ID: a custom action's Android id is its framework id plus this.
    private const int FirstResourceId = 267386881;

    // android.R.id.accessibilityActionShowOnScreen.
    private const int ActionShowOnScreen = 16908342;
    private const float ScrollExtentForInfinity = 100000f;
    private const float ScrollPositionCapForInfinity = 70000f;

    private const SemanticsActions ScrollActions = SemanticsActions.ScrollLeft | SemanticsActions.ScrollRight
                                                   | SemanticsActions.ScrollUp | SemanticsActions.ScrollDown;

    private const SemanticsFlags FocusableFlags = SemanticsFlags.HasCheckedState | SemanticsFlags.IsChecked
        | SemanticsFlags.IsSelected | SemanticsFlags.IsTextField | SemanticsFlags.IsFocused
        | SemanticsFlags.HasEnabledState | SemanticsFlags.IsEnabled | SemanticsFlags.IsInMutuallyExclusiveGroup
        | SemanticsFlags.HasToggledState | SemanticsFlags.IsToggled | SemanticsFlags.IsFocusable
        | SemanticsFlags.IsSlider;

    private readonly View _view;
    private readonly AccessibilityBridge _bridge;
    private readonly AccessibilityManager? _manager;
    private readonly Dictionary<int, NodeState> _previous = [];
    private int _accessibilityFocusedId = -1;
    private int _inputFocusedId = -1;
    private int _lastInputFocusedId = -1;
    private int _hoveredId = -1;
    private List<int> _routes = [];
    private int _previousRouteId;

    public PlumixAccessibilityNodeProvider(View view, AccessibilityBridge bridge)
    {
        _view = view;
        _bridge = bridge;
        _manager = view.Context?.GetSystemService(global::Android.Content.Context.AccessibilityService)
            as AccessibilityManager;
        _bridge.Committed += HandleCommitted;
        _bridge.Cleared += HandleCleared;
        _bridge.AnnouncementRequested += announcement => _view.AnnounceForAccessibility(announcement.Message);
        _bridge.FocusRequested += node => SendEvent(node.Id, EventTypes.ViewFocused);
    }

    /// <summary>Whether an accessibility service is on: the view hands out this provider only then.</summary>
    public bool IsAccessibilityEnabled => _manager?.IsEnabled == true;

    public override AccessibilityNodeInfo? CreateAccessibilityNodeInfo(int virtualViewId)
    {
        // The engine sets accessible navigation when a service first asks for a node; activation turns semantics on.
        _bridge.Activate();
        if (virtualViewId == HostViewId)
        {
            AccessibilityNodeInfo host = AccessibilityNodeInfo.Obtain(_view)!;
            _view.OnInitializeAccessibilityNodeInfo(host);
            if (_bridge.GetNode(RootNodeId) is not null)
            {
                host.AddChild(_view, RootNodeId);
            }

            host.ImportantForAccessibility = false;
            return host;
        }

        return _bridge.GetNode(virtualViewId) is { } node ? CreateNodeInfo(node) : null;
    }

    public override bool PerformAction(int virtualViewId, global::Android.Views.Accessibility.Action action,
        Bundle? arguments)
    {
        if (_bridge.GetNode(virtualViewId) is not { } node)
        {
            return false;
        }

        switch ((int)action)
        {
            case (int)global::Android.Views.Accessibility.Action.Click:
                _bridge.DispatchAction(node.Id, SemanticsActions.Tap);
                return true;
            case (int)global::Android.Views.Accessibility.Action.LongClick:
                _bridge.DispatchAction(node.Id, SemanticsActions.LongPress);
                return true;
            case (int)global::Android.Views.Accessibility.Action.ScrollForward:
                return Scroll(node, SemanticsActions.ScrollUp, SemanticsActions.ScrollLeft, SemanticsActions.Increase);
            case (int)global::Android.Views.Accessibility.Action.ScrollBackward:
                return Scroll(node, SemanticsActions.ScrollDown, SemanticsActions.ScrollRight,
                    SemanticsActions.Decrease);
            case (int)global::Android.Views.Accessibility.Action.NextAtMovementGranularity:
                return MoveCursor(node, arguments, forward: true);
            case (int)global::Android.Views.Accessibility.Action.PreviousAtMovementGranularity:
                return MoveCursor(node, arguments, forward: false);
            case (int)global::Android.Views.Accessibility.Action.ClearAccessibilityFocus:
                if (_accessibilityFocusedId == node.Id)
                {
                    _accessibilityFocusedId = -1;
                }

                _bridge.DispatchAction(node.Id, SemanticsActions.DidLoseAccessibilityFocus);
                SendEvent(node.Id, EventTypes.ViewAccessibilityFocusCleared);
                return true;
            case (int)global::Android.Views.Accessibility.Action.AccessibilityFocus:
                if (_accessibilityFocusedId == -1)
                {
                    _view.Invalidate();
                }

                _accessibilityFocusedId = node.Id;
                _bridge.SetLastFocusedId(node.Id);
                _bridge.ReportAccessibilityFocus(node.Id);
                SendEvent(node.Id, EventTypes.ViewAccessibilityFocused);
                if (node.HasAction(SemanticsActions.Increase) || node.HasAction(SemanticsActions.Decrease))
                {
                    // A SeekBar only announces its value after a selection event.
                    SendEvent(node.Id, EventTypes.ViewSelected);
                }

                return true;
            case ActionShowOnScreen:
                _bridge.DispatchAction(node.Id, SemanticsActions.ShowOnScreen);
                return true;
            case (int)global::Android.Views.Accessibility.Action.SetSelection:
            {
                int extent = node.Data.TextSelection?.ExtentOffset ?? 0;
                bool hasRange = arguments?.ContainsKey(AccessibilityNodeInfo.ActionArgumentSelectionStartInt) == true
                    && arguments.ContainsKey(AccessibilityNodeInfo.ActionArgumentSelectionEndInt);
                int start = hasRange
                    ? arguments!.GetInt(AccessibilityNodeInfo.ActionArgumentSelectionStartInt)
                    : extent;
                int end = hasRange ? arguments!.GetInt(AccessibilityNodeInfo.ActionArgumentSelectionEndInt) : extent;
                _bridge.DispatchAction(node.Id, SemanticsActions.SetSelection,
                    new Dictionary<string, object?> { ["base"] = start, ["extent"] = end });
                return true;
            }
            case (int)global::Android.Views.Accessibility.Action.Copy:
                _bridge.DispatchAction(node.Id, SemanticsActions.Copy);
                return true;
            case (int)global::Android.Views.Accessibility.Action.Cut:
                _bridge.DispatchAction(node.Id, SemanticsActions.Cut);
                return true;
            case (int)global::Android.Views.Accessibility.Action.Paste:
                _bridge.DispatchAction(node.Id, SemanticsActions.Paste);
                return true;
            case (int)global::Android.Views.Accessibility.Action.Dismiss:
                _bridge.DispatchAction(node.Id, SemanticsActions.Dismiss);
                return true;
            case (int)global::Android.Views.Accessibility.Action.SetText:
            {
                string text = arguments?.GetCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence)
                    ?.ToString() ?? string.Empty;
                _bridge.DispatchAction(node.Id, SemanticsActions.SetText, text);
                return true;
            }
            case (int)global::Android.Views.Accessibility.Action.Expand:
                _bridge.DispatchAction(node.Id, SemanticsActions.Expand);
                return true;
            case (int)global::Android.Views.Accessibility.Action.Collapse:
                _bridge.DispatchAction(node.Id, SemanticsActions.Collapse);
                return true;
            default:
                int customId = (int)action - FirstResourceId;
                if (_bridge.CustomActions.ContainsKey(customId))
                {
                    _bridge.DispatchAction(node.Id, SemanticsActions.CustomAction, customId);
                    return true;
                }

                return false;
        }
    }

    public override AccessibilityNodeInfo? FindFocus(NodeFocus focus)
    {
        if (focus == NodeFocus.Input && _inputFocusedId != -1)
        {
            return CreateAccessibilityNodeInfo(_inputFocusedId);
        }

        return focus is NodeFocus.Input or NodeFocus.Accessibility && _accessibilityFocusedId != -1
            ? CreateAccessibilityNodeInfo(_accessibilityFocusedId)
            : null;
    }

    /// <summary>Touch exploration: the view's hover events while a service explores by touch.</summary>
    public bool OnHoverEvent(MotionEvent motion)
    {
        if (_manager?.IsTouchExplorationEnabled != true || _bridge.Root is null)
        {
            return false;
        }

        switch (motion.ActionMasked)
        {
            case MotionEventActions.HoverEnter:
            case MotionEventActions.HoverMove:
                int hit = HitTest(motion.GetX(), motion.GetY());
                if (hit != _hoveredId)
                {
                    if (hit != -1)
                    {
                        SendEvent(hit, EventTypes.ViewHoverEnter);
                    }

                    if (_hoveredId != -1)
                    {
                        SendEvent(_hoveredId, EventTypes.ViewHoverExit);
                    }

                    _hoveredId = hit;
                }

                return true;
            case MotionEventActions.HoverExit:
                if (_hoveredId != -1)
                {
                    SendEvent(_hoveredId, EventTypes.ViewHoverExit);
                    _hoveredId = -1;
                }

                return true;
            default:
                return false;
        }
    }

    private AccessibilityNodeInfo CreateNodeInfo(AccessibilityNode node)
    {
        AccessibilityNodeInfo info = AccessibilityNodeInfo.Obtain(_view, node.Id)!;
        info.ImportantForAccessibility = IsImportant(node);
        info.ViewIdResourceName = node.Identifier;
        info.PackageName = _view.Context?.PackageName;
        info.ClassName = "android.view.View";
        info.SetSource(_view, node.Id);
        Configure(info, node);

        if (node.Parent is { } parent)
        {
            info.SetParent(_view, parent.Id);
            int index = IndexOf(parent.Children, node);
            if (index > 0)
            {
                info.SetTraversalAfter(_view, parent.Children[index - 1].Id);
            }
        }
        else
        {
            info.SetParent(_view);
        }

        AndroidRect bounds = GlobalBounds(node);
        int[] location = new int[2];
        _view.GetLocationOnScreen(location);
        bounds.Offset(location[0], location[1]);
        info.SetBoundsInScreen(bounds);
        info.VisibleToUser = true;
        info.Enabled = node.IsEnabled;

        foreach (AccessibilityNode child in node.Children)
        {
            if (!child.HasFlag(SemanticsFlags.IsHidden))
            {
                info.AddChild(_view, child.Id);
            }
        }

        return info;
    }

    // BaseRoleConfigurator.configure, in its order: the last class name set wins.
    private void Configure(AccessibilityNodeInfo info, AccessibilityNode node)
    {
        info.Focusable = IsFocusable(node);
        if (_inputFocusedId != -1)
        {
            info.Focused = _inputFocusedId == node.Id;
        }

        if (_accessibilityFocusedId != -1)
        {
            info.AccessibilityFocused = _accessibilityFocusedId == node.Id;
        }

        info.AddAction(_accessibilityFocusedId == node.Id
            ? AccessibilityAction.ActionClearAccessibilityFocus
            : AccessibilityAction.ActionAccessibilityFocus);

        AddActionIf(info, node, SemanticsActions.SetSelection, AccessibilityAction.ActionSetSelection);
        AddActionIf(info, node, SemanticsActions.Copy, AccessibilityAction.ActionCopy);
        AddActionIf(info, node, SemanticsActions.Cut, AccessibilityAction.ActionCut);
        AddActionIf(info, node, SemanticsActions.Paste, AccessibilityAction.ActionPaste);
        AddActionIf(info, node, SemanticsActions.SetText, AccessibilityAction.ActionSetText);
        if (node.HasAction(SemanticsActions.Dismiss))
        {
            info.Dismissable = true;
            info.AddAction(AccessibilityAction.ActionDismiss);
        }

        IReadOnlyList<CustomSemanticsActionUpdate> customActions = _bridge.GetCustomActions(node);
        if (node.HasAction(SemanticsActions.Tap))
        {
            info.Clickable = true;
            info.AddAction(OverrideHint(node, SemanticsActions.Tap) is { } tapHint
                ? new AccessibilityAction((int)global::Android.Views.Accessibility.Action.Click, tapHint)
                : AccessibilityAction.ActionClick);
        }

        if (node.HasAction(SemanticsActions.LongPress))
        {
            info.LongClickable = true;
            info.AddAction(OverrideHint(node, SemanticsActions.LongPress) is { } longPressHint
                ? new AccessibilityAction((int)global::Android.Views.Accessibility.Action.LongClick, longPressHint)
                : AccessibilityAction.ActionLongClick);
        }

        if (node.HasFlag(SemanticsFlags.IsLiveRegion))
        {
            info.LiveRegion = AccessibilityLiveRegion.Polite;
        }

        info.Selected = node.HasFlag(SemanticsFlags.IsSelected);
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            info.Heading = node.Data.HeadingLevel > 0 || node.HasFlag(SemanticsFlags.IsHeader);
        }
        foreach (CustomSemanticsActionUpdate custom in customActions)
        {
            info.AddAction(new AccessibilityAction(FirstResourceId + custom.Id, custom.Label));
        }

        if (node.HasFlag(SemanticsFlags.IsTextField))
        {
            ConfigureTextField(info, node);
        }

        if (node.HasFlag(SemanticsFlags.IsButton)
            || (node.HasFlag(SemanticsFlags.IsLink) && node.Data.LinkUrl is null))
        {
            info.ClassName = "android.widget.Button";
        }

        if (node.HasFlag(SemanticsFlags.IsImage))
        {
            info.ClassName = "android.widget.ImageView";
        }

        if (node.HasFlag(SemanticsFlags.IsSlider) && !node.HasAction(SemanticsActions.Tap))
        {
            info.Clickable = true;
            info.AddAction(AccessibilityAction.ActionClick);
        }

        if (node.HasAction(SemanticsActions.Increase) || node.HasAction(SemanticsActions.Decrease))
        {
            info.ClassName = "android.widget.SeekBar";
            AddActionIf(info, node, SemanticsActions.Increase, AccessibilityAction.ActionScrollForward);
            AddActionIf(info, node, SemanticsActions.Decrease, AccessibilityAction.ActionScrollBackward);
        }

        if ((node.Actions & ScrollActions) != 0)
        {
            ConfigureScrollable(info, node);
        }

        ConfigureCollectionItem(info, node);
        ConfigureText(info, node);
        ConfigureCheckable(info, node);
    }

    private static void ConfigureTextField(AccessibilityNodeInfo info, AccessibilityNode node)
    {
        info.Password = node.HasFlag(SemanticsFlags.IsObscured);
        bool readOnly = node.HasFlag(SemanticsFlags.IsReadOnly);
        if (!readOnly)
        {
            info.ClassName = "android.widget.EditText";
        }

        info.Editable = !readOnly;
        info.MultiLine = node.HasFlag(SemanticsFlags.IsMultiline);
        if (node.Data.TextSelection is { BaseOffset: >= 0, ExtentOffset: >= 0 } selection)
        {
            info.SetTextSelection(selection.BaseOffset, selection.ExtentOffset);
        }

        MovementGranularity granularities = 0;
        foreach ((SemanticsActions action, AccessibilityAction? androidAction, MovementGranularity granularity)
                 in CursorActions)
        {
            if (node.HasAction(action))
            {
                AddActionIf(info, node, action, androidAction);
                granularities |= granularity;
            }
        }

        info.MovementGranularities = granularities;
        if (node.Data.MaxValueLength is >= 0 and int max)
        {
            info.MaxTextLength = node.Value.Length - (node.Data.CurrentValueLength ?? 0) + max;
        }
    }

    private void ConfigureScrollable(AccessibilityNodeInfo info, AccessibilityNode node)
    {
        info.Scrollable = true;
        bool horizontal = node.HasAction(SemanticsActions.ScrollLeft) || node.HasAction(SemanticsActions.ScrollRight);
        if (node.HasFlag(SemanticsFlags.HasImplicitScrolling))
        {
            info.ClassName = horizontal ? "android.widget.HorizontalScrollView" : "android.widget.ScrollView";
        }

        if (node.HasAction(SemanticsActions.ScrollLeft) || node.HasAction(SemanticsActions.ScrollUp))
        {
            info.AddAction(AccessibilityAction.ActionScrollForward);
        }

        if (node.HasAction(SemanticsActions.ScrollRight) || node.HasAction(SemanticsActions.ScrollDown))
        {
            info.AddAction(AccessibilityAction.ActionScrollBackward);
        }

        if (ShouldSetCollectionInfo(node))
        {
            int count = node.Data.ScrollChildCount ?? 0;
            info.SetCollectionInfo(horizontal
                ? AccessibilityNodeInfo.CollectionInfo.Obtain(1, count, false)
                : AccessibilityNodeInfo.CollectionInfo.Obtain(count, 1, false));
        }
    }

    private void ConfigureCollectionItem(AccessibilityNodeInfo info, AccessibilityNode node)
    {
        if (node.Parent is not { } parent || !parent.HasFlag(SemanticsFlags.HasImplicitScrolling)
            || !ShouldSetCollectionInfo(parent))
        {
            return;
        }

        int index = IndexOf(parent.Children, node);
        bool horizontal = parent.HasAction(SemanticsActions.ScrollLeft)
                          || parent.HasAction(SemanticsActions.ScrollRight);
        bool heading = node.HasFlag(SemanticsFlags.IsHeader);
        info.SetCollectionItemInfo(horizontal
            ? AccessibilityNodeInfo.CollectionItemInfo.Obtain(0, 1, index, 1, heading)
            : AccessibilityNodeInfo.CollectionItemInfo.Obtain(index, 1, 0, 1, heading));
    }

    // Collection info only around the focused node, or anywhere when the focus is outside any implicit scroller.
    private bool ShouldSetCollectionInfo(AccessibilityNode node)
    {
        if ((node.Data.ScrollChildCount ?? 0) <= 1)
        {
            return false;
        }

        AccessibilityNode? focused = _bridge.GetNode(_accessibilityFocusedId);
        if (focused is null)
        {
            return true;
        }

        bool insideImplicitScroller = false;
        for (AccessibilityNode? ancestor = focused.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, node))
            {
                return true;
            }

            insideImplicitScroller |= ancestor.HasFlag(SemanticsFlags.HasImplicitScrolling);
        }

        return !insideImplicitScroller;
    }

    // BaseRoleConfigurator.configureLabelAndValue: hint and tooltip texts from Android 9; before it the tooltip
    // ends the content description.
    private static void ConfigureText(AccessibilityNodeInfo info, AccessibilityNode node)
    {
        bool api28 = OperatingSystem.IsAndroidVersionAtLeast(28);
        if (node.HasFlag(SemanticsFlags.IsTextField))
        {
            info.Text = node.Value;
            if (api28)
            {
                info.HintText = Join(node.Label, node.Hint);
            }
        }
        else if (!node.HasFlag(SemanticsFlags.ScopesRoute))
        {
            string? content = Join(node.Value, node.Label, node.Hint);
            if (!api28 && node.Tooltip.Length > 0)
            {
                content = (content ?? string.Empty) + "\n" + node.Tooltip;
            }

            if (content is not null)
            {
                info.ContentDescription = content;
            }
        }

        if (api28 && node.Tooltip.Length > 0)
        {
            info.TooltipText = node.Tooltip;
            if (Join(node.Value, node.Label, node.Hint) is null)
            {
                info.ContentDescription = node.Tooltip;
            }
        }
    }

    private static void ConfigureCheckable(AccessibilityNodeInfo info, AccessibilityNode node)
    {
        bool hasChecked = node.HasFlag(SemanticsFlags.HasCheckedState);
        bool hasToggled = node.HasFlag(SemanticsFlags.HasToggledState);
        info.Checkable = hasChecked || hasToggled;
        if (hasChecked)
        {
            info.ClassName = node.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)
                ? "android.widget.RadioButton"
                : "android.widget.CheckBox";
            info.Checked = node.HasFlag(SemanticsFlags.IsChecked);
        }
        else if (hasToggled)
        {
            info.ClassName = "android.widget.Switch";
            info.Checked = node.HasFlag(SemanticsFlags.IsToggled);
        }
    }

    private bool Scroll(AccessibilityNode node, SemanticsActions primary, SemanticsActions secondary,
        SemanticsActions slider)
    {
        foreach (SemanticsActions action in new[] { primary, secondary })
        {
            if (node.HasAction(action))
            {
                _bridge.DispatchAction(node.Id, action);
                return true;
            }
        }

        if (!node.HasAction(slider))
        {
            return false;
        }

        // The engine reads the new value out before the framework answers: TalkBack speaks it right away.
        SendEvent(node.Id, EventTypes.ViewSelected);
        _bridge.DispatchAction(node.Id, slider);
        return true;
    }

    private bool MoveCursor(AccessibilityNode node, Bundle? arguments, bool forward)
    {
        if (arguments is null)
        {
            return false;
        }

        var granularity =
            (MovementGranularity)arguments.GetInt(AccessibilityNodeInfo.ActionArgumentMovementGranularityInt);
        bool extend = arguments.GetBoolean(AccessibilityNodeInfo.ActionArgumentExtendSelectionBoolean);
        SemanticsActions? action = granularity switch
        {
            MovementGranularity.Character => forward
                ? SemanticsActions.MoveCursorForwardByCharacter
                : SemanticsActions.MoveCursorBackwardByCharacter,
            MovementGranularity.Word => forward
                ? SemanticsActions.MoveCursorForwardByWord
                : SemanticsActions.MoveCursorBackwardByWord,
            MovementGranularity.Line or MovementGranularity.Paragraph or MovementGranularity.Page => null,
            _ => (SemanticsActions?)SemanticsActions.None,
        };
        if (action == SemanticsActions.None)
        {
            return false;
        }

        if (action is { } move && node.HasAction(move))
        {
            _bridge.DispatchAction(node.Id, move, extend);
        }

        return true;
    }

    private void HandleCommitted(AccessibilityTreeUpdate update)
    {
        foreach (AccessibilityNode removed in update.RemovedNodes)
        {
            _previous.Remove(removed.Id);
            if (_accessibilityFocusedId == removed.Id)
            {
                SendEvent(removed.Id, EventTypes.ViewAccessibilityFocusCleared);
                _accessibilityFocusedId = -1;
            }

            if (_inputFocusedId == removed.Id)
            {
                _inputFocusedId = -1;
            }

            if (_hoveredId == removed.Id)
            {
                _hoveredId = -1;
            }
        }

        UpdateRoutes();
        SendContentChanged(RootNodeId, ContentChangeTypes.Subtree);
        _inputFocusedId = FindInputFocus(_bridge.Root);

        foreach (AccessibilityNode node in update.UpdatedNodes.Concat(update.CreatedNodes).DistinctBy(n => n.Id))
        {
            NodeState now = NodeState.Of(node);
            NodeState? before = _previous.GetValueOrDefault(node.Id);
            _previous[node.Id] = now;
            if (before is null)
            {
                continue;
            }

            if (before.ScrollPosition is { } oldPosition && now.ScrollPosition is { } newPosition
                && oldPosition != newPosition)
            {
                SendScrolled(node, before);
            }

            if (node.HasFlag(SemanticsFlags.IsLiveRegion) && before.Label != now.Label)
            {
                SendContentChanged(node.Id, ContentChangeTypes.Subtree);
            }

            if (_accessibilityFocusedId == node.Id && !before.Selected && now.Selected)
            {
                SendEvent(node.Id, EventTypes.ViewSelected, node.Label);
            }

            if (_inputFocusedId == node.Id && before.IsTextField && now.IsTextField
                && (_accessibilityFocusedId == -1 || _accessibilityFocusedId == node.Id))
            {
                SendTextChanges(node, before, now);
            }
        }

        if (_inputFocusedId != -1 && _inputFocusedId != _lastInputFocusedId)
        {
            _lastInputFocusedId = _inputFocusedId;
            SendEvent(_inputFocusedId, EventTypes.ViewFocused);
        }
        else if (_inputFocusedId == -1)
        {
            _lastInputFocusedId = -1;
        }
    }

    private void HandleCleared()
    {
        if (_accessibilityFocusedId != -1)
        {
            SendEvent(_accessibilityFocusedId, EventTypes.ViewAccessibilityFocusCleared);
            _accessibilityFocusedId = -1;
        }

        _hoveredId = -1;
        _previous.Clear();
        SendContentChanged(RootNodeId, ContentChangeTypes.Subtree);
    }

    // The engine's route change: the newest route's name becomes the pane title, so TalkBack announces it.
    private void UpdateRoutes()
    {
        var routes = new List<AccessibilityNode>();
        CollectRoutes(_bridge.Root, routes);
        AccessibilityNode? lastAdded =
            routes.LastOrDefault(route => !_routes.Contains(route.Id)) ?? routes.LastOrDefault();
        if (lastAdded is not null && (lastAdded.Id != _previousRouteId || routes.Count != _routes.Count))
        {
            _previousRouteId = lastAdded.Id;
            string routeName = RouteName(lastAdded) ?? " ";
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                _view.AccessibilityPaneTitle = routeName;
            }
            else
            {
                // Before Android 9 the engine announces the route with a window state change.
                SendEvent(lastAdded.Id, EventTypes.WindowStateChanged, routeName);
            }
        }

        _routes = [.. routes.Select(route => route.Id)];
    }

    private static void CollectRoutes(AccessibilityNode? node, List<AccessibilityNode> routes)
    {
        if (node is null)
        {
            return;
        }

        if (node.HasFlag(SemanticsFlags.ScopesRoute))
        {
            routes.Add(node);
        }

        foreach (AccessibilityNode child in node.Children)
        {
            CollectRoutes(child, routes);
        }
    }

    private static string? RouteName(AccessibilityNode node)
    {
        if (node.HasFlag(SemanticsFlags.NamesRoute) && node.Label.Length > 0)
        {
            return node.Label;
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

    private static int FindInputFocus(AccessibilityNode? node)
    {
        if (node is null || node.HasFlag(SemanticsFlags.IsHidden))
        {
            return -1;
        }

        int focused = node.HasFlag(SemanticsFlags.IsFocused) ? node.Id : -1;
        foreach (AccessibilityNode child in node.Children)
        {
            int childFocus = FindInputFocus(child);
            if (childFocus != -1)
            {
                focused = childFocus;
            }
        }

        return focused;
    }

    private void SendScrolled(AccessibilityNode node, NodeState before)
    {
        if (CreateEvent(node.Id, EventTypes.ViewScrolled) is not { } scrolled)
        {
            return;
        }

        double position = node.Data.ScrollPosition ?? 0;
        double max = node.Data.ScrollExtentMax ?? 0;
        double min = node.Data.ScrollExtentMin ?? 0;
        if (double.IsInfinity(max))
        {
            max = ScrollExtentForInfinity;
            position = Math.Min(position, ScrollPositionCapForInfinity);
        }

        if (double.IsInfinity(min))
        {
            max += ScrollExtentForInfinity;
            position = Math.Max(position, -ScrollPositionCapForInfinity) + ScrollExtentForInfinity;
        }
        else
        {
            max -= min;
            position -= min;
        }

        if ((before.Actions & (SemanticsActions.ScrollUp | SemanticsActions.ScrollDown)) != 0)
        {
            scrolled.ScrollY = (int)position;
            scrolled.MaxScrollY = (int)max;
        }
        else if ((before.Actions & (SemanticsActions.ScrollLeft | SemanticsActions.ScrollRight)) != 0)
        {
            scrolled.ScrollX = (int)position;
            scrolled.MaxScrollX = (int)max;
        }

        if (node.Data.ScrollChildCount is > 0 and int count)
        {
            int index = node.Data.ScrollIndex ?? 0;
            int visible = node.ChildrenInHitTestOrder.Count(child => !child.HasFlag(SemanticsFlags.IsHidden));
            scrolled.ItemCount = count;
            scrolled.FromIndex = index;
            scrolled.ToIndex = index + visible - 1;
        }

        Send(scrolled);
    }

    private void SendTextChanges(AccessibilityNode node, NodeState before, NodeState now)
    {
        if (before.Value != now.Value && CreateEvent(node.Id, EventTypes.ViewTextChanged) is { } changed)
        {
            string oldText = before.Value;
            string newText = now.Value;
            int from = 0;
            while (from < oldText.Length && from < newText.Length && oldText[from] == newText[from])
            {
                from++;
            }

            int oldEnd = oldText.Length - 1;
            int newEnd = newText.Length - 1;
            while (oldEnd >= from && newEnd >= from && oldText[oldEnd] == newText[newEnd])
            {
                oldEnd--;
                newEnd--;
            }

            changed.BeforeText = oldText;
            changed.Text?.Add(new Java.Lang.String(newText));
            changed.FromIndex = from;
            changed.RemovedCount = oldEnd - from + 1;
            changed.AddedCount = newEnd - from + 1;
            Send(changed);
        }

        if ((before.SelectionBase != now.SelectionBase || before.SelectionExtent != now.SelectionExtent)
            && CreateEvent(node.Id, EventTypes.ViewTextSelectionChanged) is { } selection)
        {
            selection.Text?.Add(new Java.Lang.String(now.Value));
            selection.FromIndex = now.SelectionBase;
            selection.ToIndex = now.SelectionExtent;
            selection.ItemCount = now.Value.Length;
            Send(selection);
        }
    }

    private void SendContentChanged(int id, ContentChangeTypes types)
    {
        if (CreateEvent(id, EventTypes.WindowContentChanged) is { } changed)
        {
            changed.ContentChangeTypes = types;
            Send(changed);
        }
    }

    private void SendEvent(int id, EventTypes type, string? text = null)
    {
        if (CreateEvent(id, type) is { } accessibilityEvent)
        {
            if (text is not null)
            {
                accessibilityEvent.Text?.Add(new Java.Lang.String(text));
            }

            Send(accessibilityEvent);
        }
    }

    private AccessibilityEvent? CreateEvent(int id, EventTypes type)
    {
        if (!IsAccessibilityEnabled)
        {
            return null;
        }

        AccessibilityEvent accessibilityEvent = AccessibilityEvent.Obtain(type)!;
        accessibilityEvent.PackageName = _view.Context?.PackageName;
        accessibilityEvent.SetSource(_view, id);
        return accessibilityEvent;
    }

    private void Send(AccessibilityEvent accessibilityEvent) =>
        _view.Parent?.RequestSendAccessibilityEvent(_view, accessibilityEvent);

    // The engine's root.hitTest: the deepest focusable node under the point, front-most child first.
    private int HitTest(float x, float y) => _bridge.Root is { } root ? HitTest(root, x, y) : -1;

    private static int HitTest(AccessibilityNode node, float x, float y)
    {
        AndroidRect bounds = GlobalBounds(node);
        if (x < bounds.Left || x >= bounds.Right || y < bounds.Top || y >= bounds.Bottom)
        {
            return -1;
        }

        foreach (AccessibilityNode child in node.ChildrenInHitTestOrder)
        {
            if (!child.HasFlag(SemanticsFlags.IsHidden) && HitTest(child, x, y) is not -1 and int hit)
            {
                return hit;
            }
        }

        return IsFocusable(node) ? node.Id : -1;
    }

    // The node's global rect, in the view's pixels (the root transform carries the device pixel ratio).
    private static AndroidRect GlobalBounds(AccessibilityNode node)
    {
        Rect rect = node.GetGlobalRect();
        return new AndroidRect((int)Math.Round(rect.Left), (int)Math.Round(rect.Top), (int)Math.Round(rect.Right),
            (int)Math.Round(rect.Bottom));
    }

    // AccessibilityBridge.SemanticsNode.isFocusable.
    private static bool IsFocusable(AccessibilityNode node)
    {
        if (node.HasFlag(SemanticsFlags.ScopesRoute))
        {
            return false;
        }

        if (node.HasFlag(SemanticsFlags.IsFocusable))
        {
            return true;
        }

        if (node.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked))
        {
            return false;
        }

        return (node.Actions & ~ScrollActions) != 0 || (node.Flags & FocusableFlags) != 0
            || node.Label.Length > 0 || node.Value.Length > 0 || node.Hint.Length > 0;
    }

    // AccessibilityBridge.isImportant, with the intended `systemAction` mask (the Java bridge's is always 0).
    private static bool IsImportant(AccessibilityNode node)
    {
        if (node.HasFlag(SemanticsFlags.ScopesRoute))
        {
            return false;
        }

        if (Join(node.Value, node.Label, node.Hint) is not null)
        {
            return true;
        }

        const SemanticsActions systemActions = SemanticsActions.DidGainAccessibilityFocus
                                               | SemanticsActions.DidLoseAccessibilityFocus
                                               | SemanticsActions.ShowOnScreen;
        return (node.Actions & ~systemActions) != 0;
    }

    private string? OverrideHint(AccessibilityNode node, SemanticsActions action)
    {
        int standard = action == SemanticsActions.Tap ? 0 : 1;
        foreach (int id in node.CustomActionIds)
        {
            if (_bridge.CustomActions.TryGetValue(id, out CustomSemanticsActionUpdate? custom)
                && custom.OverrideId == standard && custom.Hint is { Length: > 0 } hint)
            {
                return hint;
            }
        }

        return null;
    }

    private static void AddActionIf(AccessibilityNodeInfo info, AccessibilityNode node, SemanticsActions action,
        AccessibilityAction? androidAction)
    {
        if (androidAction is not null && node.HasAction(action))
        {
            info.AddAction(androidAction);
        }
    }

    private static int IndexOf(IReadOnlyList<AccessibilityNode> nodes, AccessibilityNode node)
    {
        for (int index = 0; index < nodes.Count; index++)
        {
            if (ReferenceEquals(nodes[index], node))
            {
                return index;
            }
        }

        return -1;
    }

    // AccessibilityBridge.getValueLabelHint: the non-empty parts, joined with ", ".
    private static string? Join(params string?[] parts)
    {
        string joined = string.Join(", ", parts.Where(part => !string.IsNullOrEmpty(part)));
        return joined.Length > 0 ? joined : null;
    }

    private static readonly (SemanticsActions Action, AccessibilityAction? Android, MovementGranularity Granularity)[]
        CursorActions =
        [
            (SemanticsActions.MoveCursorForwardByCharacter, AccessibilityAction.ActionNextAtMovementGranularity,
                MovementGranularity.Character),
            (SemanticsActions.MoveCursorBackwardByCharacter, AccessibilityAction.ActionPreviousAtMovementGranularity,
                MovementGranularity.Character),
            (SemanticsActions.MoveCursorForwardByWord, AccessibilityAction.ActionNextAtMovementGranularity,
                MovementGranularity.Word),
            (SemanticsActions.MoveCursorBackwardByWord, AccessibilityAction.ActionPreviousAtMovementGranularity,
                MovementGranularity.Word),
        ];

    /// <summary>What the previous commit showed of a node, for the change events of the next one.</summary>
    private sealed record NodeState(
        string Label,
        string Value,
        bool Selected,
        bool IsTextField,
        int SelectionBase,
        int SelectionExtent,
        double? ScrollPosition,
        SemanticsActions Actions)
    {
        public static NodeState Of(AccessibilityNode node) => new(
            node.Label,
            node.Value,
            node.HasFlag(SemanticsFlags.IsSelected),
            node.HasFlag(SemanticsFlags.IsTextField),
            node.Data.TextSelection?.BaseOffset ?? -1,
            node.Data.TextSelection?.ExtentOffset ?? -1,
            node.Data.ScrollPosition is { } position && !double.IsNaN(position) ? position : null,
            node.Actions);
    }
}
