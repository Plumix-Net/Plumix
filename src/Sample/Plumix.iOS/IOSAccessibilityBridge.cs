using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using Plumix.Accessibility;
using Plumix.UI;
using UIKit;

// C#-only infrastructure: the UIKit half of the iOS accessibility bridge, the engine's
// engine/src/flutter/shell/platform/darwin/ios/framework/Source/accessibility_bridge.mm and
// SemanticsObject.mm over a PlumixHost's AccessibilityBridge. What each element reads and does is
// decided by Plumix.Accessibility.IOSSemanticsObject; this file only hands it to UIKit. The deliberate
// departures (no native scroll view or UISwitch forwarding, no text-input surrogate) are listed in
// docs/ai/DIVERGENCES.md.

namespace Plumix.iOS;

/// <summary>
/// Exposes a <see cref="PlumixHost"/>'s semantics tree to VoiceOver and the other iOS assistive technologies.
/// </summary>
internal sealed class IOSAccessibilityBridge : IDisposable
{
    private readonly PlumixHost _host;
    private readonly UIView _view;
    private readonly Dictionary<int, SemanticsElement> _objects = [];
    private readonly IOSAccessibilityNotifier _notifier = new();
    private readonly List<NSObject> _observers = [];

    private IOSAccessibilityBridge(PlumixHost host, UIView view)
    {
        _host = host;
        _view = view;
        Bridge.Committed += HandleCommitted;
        Bridge.Cleared += HandleCleared;
        Bridge.AnnouncementRequested += HandleAnnouncementRequested;
        Bridge.FocusRequested += HandleFocusRequested;

        NSNotificationCenter center = NSNotificationCenter.DefaultCenter;
        foreach (NSString name in ObservedNotificationNames())
        {
            _observers.Add(center.AddObserver(name, _ => OnAccessibilityStatusChanged()));
        }

        OnAccessibilityStatusChanged();
    }

    public AccessibilityBridge Bridge => _host.AccessibilityBridge;

    internal UIView View => _view;

    /// <summary>Creates the bridge once <paramref name="host"/> is shown in an Avalonia iOS view.</summary>
    public static void Attach(PlumixHost host)
    {
        if (TryCreate(host) is not null)
        {
            return;
        }

        void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (TryCreate(host) is not null)
            {
                host.AttachedToVisualTree -= OnAttached;
            }
        }

        host.AttachedToVisualTree += OnAttached;
    }

    private static IOSAccessibilityBridge? TryCreate(PlumixHost host)
    {
        if (TopLevel.GetTopLevel(host)?.TryGetPlatformHandle() is not { } handle
            || Runtime.GetNSObject<UIView>(handle.Handle) is not { } view)
        {
            return null;
        }

        return new IOSAccessibilityBridge(host, view);
    }

    public void Dispose()
    {
        foreach (NSObject observer in _observers)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
        }

        _observers.Clear();
        Bridge.Committed -= HandleCommitted;
        Bridge.Cleared -= HandleCleared;
        Bridge.AnnouncementRequested -= HandleAnnouncementRequested;
        Bridge.FocusRequested -= HandleFocusRequested;
    }

    internal SemanticsElement? ObjectFor(AccessibilityNode? node) =>
        node is not null && _objects.TryGetValue(node.Id, out SemanticsElement? element) ? element : null;

    /// <summary>The node's frame in screen coordinates, as <c>SemanticsObject.globalRect</c> computes it.</summary>
    internal CGRect ScreenFrameOf(AccessibilityNode node)
    {
        Rect bounds = node.GetBoundsInView();
        if (TopLevel.GetTopLevel(_host) is { } topLevel && _host.TranslatePoint(default, topLevel) is { } origin)
        {
            bounds = bounds.Translate(new Vector(origin.X, origin.Y));
        }

        var frame = new CGRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        return UIAccessibility.ConvertFrameToScreenCoordinates(frame, _view);
    }

    // `onAccessibilityStatusChanged:`: semantics follow VoiceOver, Switch Control and Speak Screen (always
    // on in the simulator), and the platform's accessibility features are reported with them.
    private void OnAccessibilityStatusChanged()
    {
        bool voiceOver = UIAccessibility.IsVoiceOverRunning;
        bool switchControl = UIAccessibility.IsSwitchControlRunning;
        bool enabled = Runtime.Arch == Arch.SIMULATOR
                       || voiceOver
                       || switchControl
                       || UIAccessibility.IsSpeakScreenEnabled;

        int flags = 0;
        if (voiceOver || switchControl)
        {
            flags |= 1 << 0;
        }

        if (UIAccessibility.IsInvertColorsEnabled)
        {
            flags |= 1 << 1;
        }

        if (UIAccessibility.IsBoldTextEnabled)
        {
            flags |= 1 << 3;
        }

        if (UIAccessibility.IsReduceMotionEnabled)
        {
            flags |= 1 << 4;
        }

        if (UIAccessibility.IsOnOffSwitchLabelsEnabled)
        {
            flags |= 1 << 6;
        }

        if (!UIAccessibility.IsVideoAutoplayEnabled)
        {
            flags |= 1 << 9;
        }

        // The high-contrast bit (darker system colors) belongs to PlumixHost, which reads it from Avalonia.
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        if (dispatcher.AccessibilityFeatures.HighContrast)
        {
            flags |= 1 << 5;
        }

        dispatcher.UpdateAccessibilityFeatures(flags);
        if (enabled)
        {
            Bridge.Activate();
        }
        else
        {
            Bridge.Deactivate();
        }
    }

    private static IEnumerable<NSString> ObservedNotificationNames()
    {
        yield return UIView.VoiceOverStatusDidChangeNotification;
        yield return UIView.SwitchControlStatusDidChangeNotification;
        yield return UIView.SpeakScreenStatusDidChangeNotification;
        yield return UIView.InvertColorsStatusDidChangeNotification;
        yield return UIView.ReduceMotionStatusDidChangeNotification;
        yield return UIView.BoldTextStatusDidChangeNotification;
        yield return UIView.OnOffSwitchLabelsDidChangeNotification;
        yield return UIView.VideoAutoplayStatusDidChangeNotification;
    }

    // `AccessibilityBridge::UpdateSemantics`: objects are created for new nodes and replaced when a
    // node's kind changes, removed nodes drop their objects, the root container becomes the view's
    // accessibility elements, and the notifications the update calls for are posted.
    private void HandleCommitted(AccessibilityTreeUpdate update)
    {
        foreach (AccessibilityNode node in update.RemovedNodes)
        {
            _objects.Remove(node.Id);
        }

        foreach (AccessibilityNode node in update.UpdatedNodes)
        {
            IOSSemanticsObjectKind kind = IOSSemanticsObject.KindOf(node);
            if (!_objects.TryGetValue(node.Id, out SemanticsElement? element) || element.Kind != kind)
            {
                _objects[node.Id] = new SemanticsElement(this, node.Id, kind);
            }
        }

        if (Bridge.Root is { } root && ObjectFor(root) is { } rootObject)
        {
            if (_view.ValueForKey(new NSString("accessibilityElements")) is null)
            {
                SetViewElements(NSArray.FromNSObjects(rootObject.Container));
            }
        }
        else
        {
            SetViewElements(null);
        }

        IOSAccessibilityNotifications plan = _notifier.Plan(Bridge, update);
        foreach (string announcement in plan.Announcements)
        {
            Post(UIAccessibilityPostNotification.Announcement, new NSString(announcement));
        }

        if (IsPresentingModalViewController())
        {
            return;
        }

        if (plan.ScreenChanged)
        {
            Post(UIAccessibilityPostNotification.ScreenChanged, plan.RouteName is { } name ? new NSString(name) : null);
        }

        if (plan.LayoutChanged)
        {
            Post(UIAccessibilityPostNotification.LayoutChanged, ObjectFor(plan.FocusTarget));
        }
        else if (plan.PageScrolled)
        {
            Post(UIAccessibilityPostNotification.PageScrolled, ObjectFor(plan.FocusTarget));
        }
    }

    // The engine's `clearState`.
    private void HandleCleared()
    {
        _objects.Clear();
        _notifier.Reset();
        SetViewElements(null);
    }

    private void HandleAnnouncementRequested(AccessibilityAnnouncement announcement) =>
        Post(UIAccessibilityPostNotification.Announcement, new NSString(announcement.Message));

    private void HandleFocusRequested(AccessibilityNode node) =>
        Post(UIAccessibilityPostNotification.LayoutChanged, ObjectFor(node));

    private void SetViewElements(NSArray? elements) =>
        _view.SetValueForKey(elements!, new NSString("accessibilityElements"));

    private bool IsPresentingModalViewController()
    {
        UIViewController? controller = _view.Window?.RootViewController;
        return controller is not null
               && (controller.PresentedViewController is not null || controller.IsBeingPresented);
    }

    private static void Post(UIAccessibilityPostNotification notification, NSObject? argument) =>
        UIAccessibility.PostNotification(notification, argument!);
}

/// <summary>The accessibility element of one semantics node: the engine's <c>SemanticsObject</c>.</summary>
internal sealed class SemanticsElement : UIAccessibilityElement
{
    private readonly IOSAccessibilityBridge _owner;
    private SemanticsContainer? _container;
    private UISwitch? _nativeSwitch;

    public SemanticsElement(IOSAccessibilityBridge owner, int id, IOSSemanticsObjectKind kind)
        : base(owner.View)
    {
        _owner = owner;
        Id = id;
        Kind = kind;
    }

    public int Id { get; }

    public IOSSemanticsObjectKind Kind { get; }

    private AccessibilityNode? Node => _owner.Bridge.GetNode(Id);

    /// <summary>The container that lists this node and its children, created on first use.</summary>
    public SemanticsContainer Container => _container ??= new SemanticsContainer(_owner, this);

    public override bool IsAccessibilityElement
    {
        get => Node is { } node && IOSSemanticsObject.IsAccessibilityElement(node);
        set { }
    }

    public override string? AccessibilityLabel
    {
        get => Node is { } node ? IOSSemanticsObject.Label(node) : null;
        set { }
    }

    public override string? AccessibilityHint
    {
        get => Node is { } node ? IOSSemanticsObject.Hint(node) : null;
        set { }
    }

    public override string? AccessibilityValue
    {
        get
        {
            if (Node is not { } node)
            {
                return null;
            }

            if (Kind == IOSSemanticsObjectKind.Switch)
            {
                NativeSwitch(node).On = IOSSemanticsObject.Value(node) == "1";
                return NativeSwitch(node).AccessibilityValue;
            }

            return IOSSemanticsObject.Value(node);
        }

        set { }
    }

    public override ulong AccessibilityTraits
    {
        get
        {
            if (Node is not { } node)
            {
                return 0;
            }

            return Kind == IOSSemanticsObjectKind.Switch
                ? (ulong)NativeSwitch(node).AccessibilityTraits
                : (ulong)IOSSemanticsObject.Traits(node);
        }

        set { }
    }

    public override string? AccessibilityIdentifier
    {
        get => Node is { Identifier.Length: > 0 } node ? node.Identifier : null;
        set { }
    }

    public override CGRect AccessibilityFrame
    {
        get => Node is { } node ? _owner.ScreenFrameOf(node) : CGRect.Empty;
        set { }
    }

    // `SemanticsObject.accessibilityContainer`: a node with children (and the root) lives in its own
    // container; any other node lives in its parent's.
    public override NSObject? AccessibilityContainer
    {
        get
        {
            if (Node is not { } node)
            {
                return null;
            }

            if (node.HasChildren || ReferenceEquals(node, _owner.Bridge.Root))
            {
                return Container;
            }

            return _owner.ObjectFor(node.Parent)?.AccessibilityContainer;
        }

        set { }
    }

    [Export("accessibilityLanguage")]
    public string? AccessibilityLanguageValue =>
        Node?.Data.Locale is { } locale ? locale.ToLanguageTag() : null;

    [Export("accessibilityCustomActions")]
    public NSArray? AccessibilityCustomActionsValue
    {
        get
        {
            if (Node is not { } node || node.CustomActionIds.Count == 0)
            {
                return null;
            }

            var actions = new List<NSObject>();
            foreach (Rendering.CustomSemanticsActionUpdate custom in node.Bridge.GetCustomActions(node))
            {
                int actionId = custom.Id;
                Func<UIAccessibilityCustomAction, bool> handler =
                    _ => Node is { } current && IOSSemanticsObject.PerformCustomAction(current, actionId);
                actions.Add(new UIAccessibilityCustomAction(custom.Label ?? string.Empty, handler));
            }

            return NSArray.FromNSObjects(actions.ToArray());
        }
    }

    [Export("accessibilityRespondsToUserInteraction")]
    public bool AccessibilityRespondsToUserInteraction =>
        Node is { } node && IOSSemanticsObject.RespondsToUserInteraction(node);

    [Export("accessibilityActivate")]
    public bool AccessibilityActivate() => Node is { } node && IOSSemanticsObject.Activate(node);

    [Export("accessibilityIncrement")]
    public void AccessibilityIncrement()
    {
        if (Node is { } node)
        {
            IOSSemanticsObject.Adjust(node, increment: true);
        }
    }

    [Export("accessibilityDecrement")]
    public void AccessibilityDecrement()
    {
        if (Node is { } node)
        {
            IOSSemanticsObject.Adjust(node, increment: false);
        }
    }

    [Export("accessibilityScroll:")]
    public bool AccessibilityScroll(UIAccessibilityScrollDirection direction) =>
        Node is { } node && IOSSemanticsObject.Scroll(node, (IOSAccessibilityScrollDirection)(int)direction);

    [Export("accessibilityPerformEscape")]
    public bool AccessibilityPerformEscape() => Node is { } node && IOSSemanticsObject.PerformEscape(node);

    [Export("accessibilityScrollToVisible")]
    public bool AccessibilityScrollToVisible()
    {
        Node?.PerformAction(Rendering.SemanticsActions.ShowOnScreen);
        return Node is not null;
    }

    [Export("accessibilityElementDidBecomeFocused")]
    public void AccessibilityElementDidBecomeFocused()
    {
        if (Node is { } node)
        {
            IOSSemanticsObject.DidBecomeFocused(node);
        }
    }

    [Export("accessibilityElementDidLoseFocus")]
    public void AccessibilityElementDidLoseFocus()
    {
        if (Node is { } node)
        {
            IOSSemanticsObject.DidLoseFocus(node);
        }
    }

    // `FlutterSwitchSemanticsObject`: the traits and value of a real UISwitch in the node's state.
    private UISwitch NativeSwitch(AccessibilityNode node)
    {
        _nativeSwitch ??= new UISwitch();
        _nativeSwitch.Enabled = node.HasFlag(Rendering.SemanticsFlags.HasEnabledState)
                                && node.HasFlag(Rendering.SemanticsFlags.IsEnabled);
        return _nativeSwitch;
    }
}

/// <summary>Lists a node and then its children: the engine's <c>SemanticsObjectContainer</c>.</summary>
internal sealed class SemanticsContainer : UIAccessibilityElement
{
    private readonly IOSAccessibilityBridge _owner;
    private readonly SemanticsElement _element;

    public SemanticsContainer(IOSAccessibilityBridge owner, SemanticsElement element)
        : base(owner.View)
    {
        _owner = owner;
        _element = element;
    }

    public override bool IsAccessibilityElement
    {
        get => false;
        set { }
    }

    public override CGRect AccessibilityFrame
    {
        get => UIScreen.MainScreen.Bounds;
        set { }
    }

    public override NSObject? AccessibilityContainer
    {
        get
        {
            AccessibilityNode? node = _owner.Bridge.GetNode(_element.Id);
            if (node is null)
            {
                return null;
            }

            return node.Parent is null ? _owner.View : _owner.ObjectFor(node.Parent)?.AccessibilityContainer;
        }

        set { }
    }

    private IReadOnlyList<AccessibilityNode> Children =>
        _owner.Bridge.GetNode(_element.Id)?.Children ?? [];

    [Export("accessibilityElementCount")]
    public nint AccessibilityElementCount() => Children.Count + 1;

    [Export("accessibilityElementAtIndex:")]
    public NSObject? GetAccessibilityElementAt(nint index)
    {
        if (index == 0)
        {
            return _element;
        }

        IReadOnlyList<AccessibilityNode> children = Children;
        if (index < 0 || index > children.Count)
        {
            return null;
        }

        AccessibilityNode child = children[(int)index - 1];
        SemanticsElement? element = _owner.ObjectFor(child);
        return child.HasChildren ? element?.Container : element;
    }

    [Export("indexOfAccessibilityElement:")]
    public nint GetIndexOfAccessibilityElement(NSObject element)
    {
        if (ReferenceEquals(element, _element))
        {
            return 0;
        }

        IReadOnlyList<AccessibilityNode> children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            SemanticsElement? child = _owner.ObjectFor(children[i]);
            if (child is null)
            {
                continue;
            }

            if ((!children[i].HasChildren && ReferenceEquals(child, element))
                || (children[i].HasChildren && ReferenceEquals(child.Container, element)))
            {
                return i + 1;
            }
        }

        // NSNotFound.
        return nint.MaxValue;
    }
}
