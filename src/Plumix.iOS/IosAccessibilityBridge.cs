using CoreGraphics;
using Foundation;
using ObjCRuntime;
using Plumix.Accessibility;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using UIKit;
using Rect = Plumix.UI.Rect;

namespace Plumix.iOS;

// C#-only host infrastructure parity sources, all under
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/: accessibility_bridge.mm (objects per node,
// replaced when the kind changes; the root container as the view's accessibility elements; route, layout, scroll and
// live-region notifications), SemanticsObject.mm (FlutterSemanticsObject, FlutterSwitchSemanticsObject,
// FlutterScrollableSemanticsObject, SemanticsObjectContainer, global rects through the node transforms, hit testing),
// FlutterSemanticsScrollView.mm and TextInputSemanticsObject.mm. What a node reads and does is shared with the
// Avalonia iOS host in the core (Accessibility/IOSAccessibility.cs). Not ported: platform view containers, the
// switch's full selector forwarding to UISwitch (its traits and value are borrowed), and SemanticsObject+UIFocusSystem
// (Full Keyboard Access focus items).

/// <summary>Exposes the host's semantics tree to VoiceOver and the other iOS assistive technologies.</summary>
internal sealed class IosAccessibilityBridge : IDisposable
{
    private readonly Dictionary<int, SemanticsElement> _objects = [];
    private readonly IOSAccessibilityNotifier _notifier = new();
    private readonly WeakReference<PlumixViewController> _viewController;
    private readonly Func<IosTextInputView?> _textInputView;

    public IosAccessibilityBridge(
        AccessibilityBridge bridge, PlumixViewController viewController, UIView view,
        Func<IosTextInputView?> textInputView)
    {
        Bridge = bridge;
        View = view;
        _viewController = new WeakReference<PlumixViewController>(viewController);
        _textInputView = textInputView;
        bridge.Committed += HandleCommitted;
        bridge.Cleared += HandleCleared;
        bridge.AnnouncementRequested += HandleAnnouncementRequested;
        bridge.FocusRequested += HandleFocusRequested;
    }

    public AccessibilityBridge Bridge { get; }

    public UIView View { get; }

    /// <summary>The engine bridge's textInputView: the focused field's input view.</summary>
    public IosTextInputView? TextInputView => _textInputView();

    public void Dispose()
    {
        Bridge.Committed -= HandleCommitted;
        Bridge.Cleared -= HandleCleared;
        Bridge.AnnouncementRequested -= HandleAnnouncementRequested;
        Bridge.FocusRequested -= HandleFocusRequested;
        HandleCleared();
    }

    public SemanticsElement? ObjectFor(AccessibilityNode? node) =>
        node is not null && _objects.TryGetValue(node.Id, out SemanticsElement? element) ? element : null;

    /// <summary>ConvertRectToGlobal: a rect in the node's space, through every transform, in screen points.</summary>
    public CGRect ConvertRectToGlobal(AccessibilityNode node, Rect localRect)
    {
        Rect rect = localRect;
        for (AccessibilityNode? current = node; current is not null; current = current.Parent)
        {
            if (current.Transform is { } transform)
            {
                rect = MatrixUtils.TransformRect(transform, rect);
            }
        }

        // The rect is in physical pixels; iOS expects points.
        nfloat scale = ScreenScale;
        var result = new CGRect(rect.Left / scale, rect.Top / scale, rect.Width / scale, rect.Height / scale);
        return UIAccessibility.ConvertFrameToScreenCoordinates(result, View);
    }

    /// <summary>ConvertPointToGlobal: a point in the node's space in window coordinates.</summary>
    public CGPoint ConvertPointToGlobal(AccessibilityNode node, Offset localPoint)
    {
        Offset point = localPoint;
        for (AccessibilityNode? current = node; current is not null; current = current.Parent)
        {
            if (current.Transform is { } transform)
            {
                point = MatrixUtils.TransformPoint(transform, point);
            }
        }

        nfloat scale = ScreenScale;
        return View.ConvertPointToView(new CGPoint(point.Dx / scale, point.Dy / scale), null);
    }

    private nfloat ScreenScale => (View.Window?.Screen ?? UIScreen.MainScreen).Scale;

    // AccessibilityBridge::UpdateSemantics.
    private void HandleCommitted(AccessibilityTreeUpdate update)
    {
        foreach (AccessibilityNode node in update.RemovedNodes)
        {
            if (_objects.Remove(node.Id, out SemanticsElement? removed))
            {
                removed.Detach();
            }
        }

        foreach (AccessibilityNode node in update.UpdatedNodes)
        {
            IOSSemanticsObjectKind kind = IOSSemanticsObject.KindOf(node);
            if (!_objects.TryGetValue(node.Id, out SemanticsElement? element) || element.Kind != kind)
            {
                element?.Detach();
                element = SemanticsElement.Create(this, node.Id, kind);
                _objects[node.Id] = element;
            }

            element.NodeUpdated(node);
        }

        if (Bridge.Root is { } root && ObjectFor(root) is { } rootObject)
        {
            if (GetAccessibilityElements(View) is null)
            {
                SetAccessibilityElements(View, NSArray.FromNSObjects(rootObject.Container));
            }
        }
        else
        {
            SetAccessibilityElements(View, null);
        }

        foreach (SemanticsElement element in _objects.Values)
        {
            element.AccessibilityBridgeDidFinishUpdate();
        }

        IOSAccessibilityNotifications plan = _notifier.Plan(Bridge, update);
        // A live region announces its label, queued after what VoiceOver is saying.
        foreach (string announcement in plan.Announcements)
        {
            Post(UIAccessibilityPostNotification.Announcement, new NSAttributedString(
                announcement,
                NSDictionary.FromObjectAndKey(
                    NSNumber.FromBoolean(true), UIView.SpeechAttributeQueueAnnouncement)));
        }

        if (IsPresentingViewController())
        {
            return;
        }

        if (plan.ScreenChanged)
        {
            Post(UIAccessibilityPostNotification.ScreenChanged,
                plan.RouteName is { } name ? new NSString(name) : null);
        }

        if (plan.LayoutChanged)
        {
            Post(UIAccessibilityPostNotification.LayoutChanged, ObjectFor(plan.FocusTarget)?.NativeAccessibility);
        }
        else if (plan.PageScrolled)
        {
            Post(UIAccessibilityPostNotification.PageScrolled, ObjectFor(plan.FocusTarget)?.NativeAccessibility);
        }
    }

    // clearState.
    private void HandleCleared()
    {
        foreach (SemanticsElement element in _objects.Values)
        {
            element.Detach();
        }

        _objects.Clear();
        _notifier.Reset();
        SetAccessibilityElements(View, null);
    }

    // HandleEvent: `announce` and `focus` on flutter/accessibility.
    private void HandleAnnouncementRequested(AccessibilityAnnouncement announcement) =>
        Post(UIAccessibilityPostNotification.Announcement, new NSString(announcement.Message));

    private void HandleFocusRequested(AccessibilityNode node) =>
        Post(UIAccessibilityPostNotification.LayoutChanged, ObjectFor(node));

    // isPresentingViewController: a presented controller, or one being presented.
    private bool IsPresentingViewController() =>
        _viewController.TryGetTarget(out PlumixViewController? controller)
        && (controller.PresentedViewController is not null || controller.IsPresentingViewControllerAnimating);

    private static void Post(UIAccessibilityPostNotification notification, NSObject? argument) =>
        UIAccessibility.PostNotification(notification, argument!);

    /// <summary>The view's accessibilityElements (UIAccessibilityContainer has no binding on UIView).</summary>
    internal static NSObject? GetAccessibilityElements(UIView view) =>
        view.ValueForKey(new NSString("accessibilityElements"));

    private static void SetAccessibilityElements(UIView view, NSArray? elements) =>
        view.SetValueForKey(elements!, new NSString("accessibilityElements"));
}

/// <summary>The accessibility element of one semantics node: the engine's SemanticsObject.</summary>
internal class SemanticsElement : UIAccessibilityElement
{
    private SemanticsContainer? _container;
    private string? _valueOverride;

    protected SemanticsElement(IosAccessibilityBridge owner, int id, IOSSemanticsObjectKind kind)
        : base(owner.View)
    {
        Owner = owner;
        Id = id;
        Kind = kind;
    }

    public int Id { get; }

    public IOSSemanticsObjectKind Kind { get; }

    /// <summary>The element UIKit should see: the object itself, or the scroll view of a scrollable.</summary>
    public virtual NSObject NativeAccessibility => this;

    /// <summary>The container that lists this node and its children, created on first use.</summary>
    public SemanticsContainer Container => _container ??= new SemanticsContainer(Owner, this);

    protected IosAccessibilityBridge Owner { get; }

    protected AccessibilityNode? Node => Owner.Bridge.GetNode(Id);

    public static SemanticsElement Create(IosAccessibilityBridge owner, int id, IOSSemanticsObjectKind kind) =>
        kind switch
        {
            IOSSemanticsObjectKind.TextInput => new TextInputSemanticsElement(owner, id),
            IOSSemanticsObjectKind.Switch => new SwitchSemanticsElement(owner, id),
            IOSSemanticsObjectKind.Scrollable => new ScrollableSemanticsElement(owner, id),
            _ => new SemanticsElement(owner, id, kind),
        };

    /// <summary>setSemanticsNode: a new node clears the value an increment or decrement predicted.</summary>
    public virtual void NodeUpdated(AccessibilityNode node) => _valueOverride = null;

    public virtual void AccessibilityBridgeDidFinishUpdate()
    {
    }

    public virtual void Detach()
    {
    }

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

    [Export("accessibilityAttributedLabel")]
    public NSAttributedString? AccessibilityAttributedLabelValue
    {
        get => AccessibilityLabel is { Length: > 0 } label && Node is { } node
            ? CreateAttributedString(label, node.Data.AttributedLabel.Attributes)
            : null;
    }

    public override string? AccessibilityHint
    {
        get => Node is { } node ? IOSSemanticsObject.Hint(node) : null;
        set { }
    }

    [Export("accessibilityAttributedHint")]
    public NSAttributedString? AccessibilityAttributedHintValue
    {
        get => AccessibilityHint is { Length: > 0 } hint && Node is { } node
            ? CreateAttributedString(hint, node.Data.AttributedHint.Attributes)
            : null;
    }

    public override string? AccessibilityValue
    {
        get => _valueOverride ?? (Node is { } node ? IOSSemanticsObject.Value(node) : null);
        set { }
    }

    [Export("accessibilityAttributedValue")]
    public NSAttributedString? AccessibilityAttributedValueValue
    {
        get => AccessibilityValue is { Length: > 0 } value && Node is { } node
            ? CreateAttributedString(value, _valueOverride is null ? node.Data.AttributedValue.Attributes : [])
            : null;
    }

    public override ulong AccessibilityTraits
    {
        get => Node is { } node ? (ulong)IOSSemanticsObject.Traits(node) : 0;
        set { }
    }

    public override string? AccessibilityIdentifier
    {
        get => Node is { Identifier.Length: > 0 } node ? node.Identifier : null;
        set { }
    }

    // A hidden node keeps UIKit's own frame; any other the node rect through the transforms.
    public override CGRect AccessibilityFrame
    {
        get
        {
            if (Node is not { } node)
            {
                return CGRect.Empty;
            }

            return node.HasFlag(SemanticsFlags.IsHidden) ? base.AccessibilityFrame : GlobalRect(node);
        }

        set => base.AccessibilityFrame = value;
    }

    // A node with children (and the root) lives in its own container; any other node in its parent's.
    public override NSObject? AccessibilityContainer
    {
        get
        {
            if (Node is not { } node)
            {
                return null;
            }

            if (node.HasChildren || ReferenceEquals(node, Owner.Bridge.Root))
            {
                return Container;
            }

            return Owner.ObjectFor(node.Parent)?.AccessibilityContainer;
        }

        set { }
    }

    [Export("accessibilityLanguage")]
    public string? AccessibilityLanguageValue =>
        Node is not { } node
            ? null
            : node.Data.Locale?.ToLanguageTag() ?? PlatformDispatcher.Instance.ApplicationLocale?.ToLanguageTag();

    [Export("accessibilityCustomActions")]
    public NSArray? AccessibilityCustomActionsValue
    {
        get
        {
            if (Node is not { } node || node.CustomActionIds.Count == 0)
            {
                return null;
            }

            List<NSObject> actions = [];
            foreach (CustomSemanticsActionUpdate custom in node.Bridge.GetCustomActions(node))
            {
                int actionId = custom.Id;
                Func<UIAccessibilityCustomAction, bool> handler =
                    _ => Node is { } current && IOSSemanticsObject.PerformCustomAction(current, actionId);
                actions.Add(new UIAccessibilityCustomAction(custom.Label ?? string.Empty, handler));
            }

            return NSArray.FromNSObjects(actions.ToArray());
        }
    }

    public override bool AccessibilityRespondsToUserInteraction
    {
        get => Node is { } node && IOSSemanticsObject.RespondsToUserInteraction(node);
        set { }
    }

    [Export("accessibilityActivate")]
    public virtual bool AccessibilityActivate() => Node is { } node && IOSSemanticsObject.Activate(node);

    // accessibilityIncrement: the node's increased value is shown at once, before the framework answers.
    public override void AccessibilityIncrement()
    {
        if (Node is { } node && node.HasAction(SemanticsActions.Increase))
        {
            _valueOverride = node.Data.AttributedIncreasedValue.String;
            IOSSemanticsObject.Adjust(node, increment: true);
        }
    }

    public override void AccessibilityDecrement()
    {
        if (Node is { } node && node.HasAction(SemanticsActions.Decrease))
        {
            _valueOverride = node.Data.AttributedDecreasedValue.String;
            IOSSemanticsObject.Adjust(node, increment: false);
        }
    }

    public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction) =>
        Node is { } node && IOSSemanticsObject.Scroll(node, (IOSAccessibilityScrollDirection)(int)direction);

    public override bool AccessibilityPerformEscape() => Node is { } node && IOSSemanticsObject.PerformEscape(node);

    [Export("accessibilityScrollToVisible")]
    public bool AccessibilityScrollToVisible()
    {
        Node?.PerformAction(SemanticsActions.ShowOnScreen);
        return true;
    }

    [Export("accessibilityScrollToVisibleWithChild:")]
    public bool AccessibilityScrollToVisibleWithChild(NSObject child)
    {
        if (child is not SemanticsElement element)
        {
            return false;
        }

        element.AccessibilityScrollToVisible();
        return true;
    }

    public override void AccessibilityElementDidBecomeFocused()
    {
        if (Node is { } node)
        {
            IOSSemanticsObject.DidBecomeFocused(node);
        }
    }

    public override void AccessibilityElementDidLoseFocus()
    {
        if (Node is { } node)
        {
            IOSSemanticsObject.DidLoseFocus(node);
        }
    }

    // _accessibilityHitTest:withEvent: (search:): the deepest focusable node under the point, children in hit-test
    // order first.
    [Export("_accessibilityHitTest:withEvent:")]
    public NSObject? AccessibilityHitTest(CGPoint point, UIEvent? evt) => Node is { } node ? Search(node, point) : null;

    protected CGRect GlobalRect(AccessibilityNode node) => Owner.ConvertRectToGlobal(node, node.Rect);

    private NSObject? Search(AccessibilityNode node, CGPoint point)
    {
        foreach (AccessibilityNode child in node.ChildrenInHitTestOrder)
        {
            if (GlobalRect(child).Contains(point) && Owner.ObjectFor(child) is { } childObject
                && childObject.Search(child, point) is { } result)
            {
                return result;
            }
        }

        return GlobalRect(node).Contains(point) && IOSSemanticsObject.IsFocusable(node) ? NativeAccessibility : null;
    }

    // createAttributedStringFromString:withAttributes: locale and spell-out ranges as speech attributes.
    private static NSAttributedString CreateAttributedString(string text, IReadOnlyList<StringAttribute> attributes)
    {
        var attributed = new NSMutableAttributedString(text);
        foreach (StringAttribute attribute in attributes)
        {
            int start = Math.Clamp(attribute.Range.Start, 0, text.Length);
            int end = Math.Clamp(attribute.Range.End, start, text.Length);
            var range = new NSRange(start, end - start);
            switch (attribute)
            {
                case LocaleStringAttribute locale:
                    attributed.SetAttributes(
                        NSDictionary.FromObjectAndKey(
                            new NSString(locale.Locale), UIView.SpeechAttributeLanguage),
                        range);
                    break;
                case SpellOutStringAttribute:
                    attributed.SetAttributes(
                        NSDictionary.FromObjectAndKey(
                            NSNumber.FromBoolean(true), UIView.SpeechAttributeSpellOut),
                        range);
                    break;
            }
        }

        return attributed;
    }
}

/// <summary>FlutterSwitchSemanticsObject: the traits and value of a real UISwitch in the node's state.</summary>
internal sealed class SwitchSemanticsElement(IosAccessibilityBridge owner, int id)
    : SemanticsElement(owner, id, IOSSemanticsObjectKind.Switch)
{
    private readonly UISwitch _nativeSwitch = new();

    public override string? AccessibilityValue
    {
        get
        {
            if (Node is not { } node)
            {
                return null;
            }

            _nativeSwitch.On = (node.HasFlag(SemanticsFlags.HasToggledState) && node.HasFlag(SemanticsFlags.IsToggled))
                               || (node.HasFlag(SemanticsFlags.HasCheckedState)
                                   && node.HasFlag(SemanticsFlags.IsChecked));
            return _nativeSwitch.AccessibilityValue;
        }

        set { }
    }

    public override ulong AccessibilityTraits
    {
        get
        {
            if (Node is { } node)
            {
                _nativeSwitch.Enabled = node.HasFlag(SemanticsFlags.HasEnabledState)
                                        && node.HasFlag(SemanticsFlags.IsEnabled);
            }

            return (ulong)_nativeSwitch.AccessibilityTraits;
        }

        set { }
    }
}

/// <summary>
/// FlutterScrollableSemanticsObject: an implicitly scrolling node shown to UIKit as a UIScrollView whose content size
/// and offset follow the node's scroll extent and position, so VoiceOver's three-finger scrolls and the scroll
/// position announcements work as in native lists.
/// </summary>
internal sealed class ScrollableSemanticsElement : SemanticsElement
{
    // kScrollExtentMaxForInf.
    private const double ScrollExtentMaxForInf = 1000;

    private const SemanticsActions VerticalScrollActions = SemanticsActions.ScrollUp | SemanticsActions.ScrollDown;
    private const SemanticsActions HorizontalScrollActions = SemanticsActions.ScrollLeft | SemanticsActions.ScrollRight;

    private readonly SemanticsScrollView _scrollView;

    public ScrollableSemanticsElement(IosAccessibilityBridge owner, int id)
        : base(owner, id, IOSSemanticsObjectKind.Scrollable)
    {
        _scrollView = new SemanticsScrollView(this)
        {
            ShowsHorizontalScrollIndicator = false,
            ShowsVerticalScrollIndicator = false,
            ContentInset = UIEdgeInsets.Zero,
            ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never,
        };
        owner.View.AddSubview(_scrollView);
    }

    public override NSObject NativeAccessibility => _scrollView;

    internal int ChildCount => Node?.Children.Count ?? 0;

    internal bool IsVoiceOverRunning => UIAccessibility.IsVoiceOverRunning;

    // UIKit treats the scroll view as scrollable when its content is bigger than its frame and it is an element; the
    // content offset then decides which scroll directions are offered.
    public override void AccessibilityBridgeDidFinishUpdate()
    {
        if (Node is not { } node)
        {
            return;
        }

        _scrollView.Frame = AccessibilityFrame;
        _scrollView.ContentSize = ContentSize(node);
        // The system's own deceleration owns the offset until it ends.
        if (!_scrollView.IsDoingSystemScrolling)
        {
            _scrollView.SetContentOffset(ContentOffset(node), false);
        }
    }

    public override void Detach() => _scrollView.RemoveFromSuperview();

    private double ScrollExtentMax(AccessibilityNode node)
    {
        double scrollExtentMax = node.Data.ScrollExtentMax ?? double.NaN;
        if (double.IsNaN(scrollExtentMax))
        {
            return 0;
        }

        return double.IsFinite(scrollExtentMax) ? scrollExtentMax : ScrollExtentMaxForInf + ScrollPosition(node);
    }

    private static double ScrollPosition(AccessibilityNode node)
    {
        double scrollPosition = node.Data.ScrollPosition ?? double.NaN;
        return double.IsNaN(scrollPosition) ? 0 : scrollPosition;
    }

    private CGSize ContentSize(AccessibilityNode node)
    {
        Rect rect = node.Rect;
        Rect content = (node.Actions & VerticalScrollActions) != 0
            ? Rect.FromLTWH(rect.Left, rect.Top, rect.Width, rect.Height + ScrollExtentMax(node))
            : (node.Actions & HorizontalScrollActions) != 0
                ? Rect.FromLTWH(rect.Left, rect.Top, rect.Width + ScrollExtentMax(node), rect.Height)
                : rect;
        return Owner.ConvertRectToGlobal(node, content).Size;
    }

    private CGPoint ContentOffset(AccessibilityNode node)
    {
        CGPoint origin = _scrollView.Frame.Location;
        Rect rect = node.Rect;
        CGPoint result = (node.Actions & VerticalScrollActions) != 0
            ? Owner.ConvertPointToGlobal(node, new Offset(rect.Left, rect.Top + ScrollPosition(node)))
            : (node.Actions & HorizontalScrollActions) != 0
                ? Owner.ConvertPointToGlobal(node, new Offset(rect.Left + ScrollPosition(node), rect.Top))
                : origin;
        return new CGPoint(result.X - origin.X, result.Y - origin.Y);
    }
}

/// <summary>
/// FlutterSemanticsScrollView: a scroll view that only exists for accessibility; touches pass through.
/// </summary>
internal sealed class SemanticsScrollView : UIScrollView
{
    private readonly WeakReference<ScrollableSemanticsElement> _semanticsObject;
    private readonly ScrollDelegate _delegate;

    public SemanticsScrollView(ScrollableSemanticsElement semanticsObject)
        : base(CGRect.Empty)
    {
        _semanticsObject = new WeakReference<ScrollableSemanticsElement>(semanticsObject);
        _delegate = new ScrollDelegate(this);
        Delegate = _delegate;
    }

    /// <summary>
    /// isDoingSystemScrolling: between the end of a drag and the end of deceleration, UIKit (a VoiceOver scroll)
    /// moves the offset, which the bridge must not reset.
    /// </summary>
    public bool IsDoingSystemScrolling { get; private set; }

    private ScrollableSemanticsElement? SemanticsObject =>
        _semanticsObject.TryGetTarget(out ScrollableSemanticsElement? element) ? element : null;

    public override UIView? HitTest(CGPoint point, UIEvent? uievent) => null;

    // While VoiceOver runs, the scroll view is an element only when it has a label; otherwise when the object is one,
    // or when its content can scroll.
    public override bool IsAccessibilityElement
    {
        get
        {
            if (SemanticsObject is not { } semanticsObject)
            {
                return false;
            }

            if (semanticsObject.IsVoiceOverRunning)
            {
                return semanticsObject.AccessibilityLabel is { Length: > 0 };
            }

            return semanticsObject.IsAccessibilityElement
                   || ContentSize.Width > Frame.Width || ContentSize.Height > Frame.Height;
        }

        set { }
    }

    public override string? AccessibilityLabel
    {
        get => SemanticsObject?.AccessibilityLabel;
        set { }
    }

    [Export("accessibilityAttributedLabel")]
    public NSAttributedString? AccessibilityAttributedLabelValue
    {
        get => SemanticsObject?.AccessibilityAttributedLabelValue;
    }

    public override string? AccessibilityValue
    {
        get => SemanticsObject?.AccessibilityValue;
        set { }
    }

    [Export("accessibilityAttributedValue")]
    public NSAttributedString? AccessibilityAttributedValueValue
    {
        get => SemanticsObject?.AccessibilityAttributedValueValue;
    }

    public override string? AccessibilityHint
    {
        get => SemanticsObject?.AccessibilityHint;
        set { }
    }

    [Export("accessibilityAttributedHint")]
    public NSAttributedString? AccessibilityAttributedHintValue
    {
        get => SemanticsObject?.AccessibilityAttributedHintValue;
    }

    public override bool AccessibilityActivate() => SemanticsObject?.AccessibilityActivate() == true;

    public override void AccessibilityIncrement() => SemanticsObject?.AccessibilityIncrement();

    public override void AccessibilityDecrement() => SemanticsObject?.AccessibilityDecrement();

    public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction) =>
        SemanticsObject?.AccessibilityScroll(direction) == true;

    public override bool AccessibilityPerformEscape() => SemanticsObject?.AccessibilityPerformEscape() == true;

    public override void AccessibilityElementDidBecomeFocused() =>
        SemanticsObject?.AccessibilityElementDidBecomeFocused();

    public override void AccessibilityElementDidLoseFocus() => SemanticsObject?.AccessibilityElementDidLoseFocus();

    [Export("accessibilityContainer")]
    public NSObject? AccessibilityContainerValue => SemanticsObject?.AccessibilityContainer;

    [Export("accessibilityElementCount")]
    public nint AccessibilityElementCount() => SemanticsObject?.ChildCount ?? 0;

    private sealed class ScrollDelegate(SemanticsScrollView owner) : UIScrollViewDelegate
    {
        public override void WillEndDragging(
            UIScrollView scrollView, CGPoint velocity, ref CGPoint targetContentOffset) =>
            owner.IsDoingSystemScrolling = true;

        public override void DecelerationEnded(UIScrollView scrollView) => owner.IsDoingSystemScrolling = false;
    }
}

/// <summary>Lists a node and then its children: the engine's SemanticsObjectContainer.</summary>
internal sealed class SemanticsContainer : UIAccessibilityElement
{
    private readonly IosAccessibilityBridge _owner;
    private readonly SemanticsElement _element;

    public SemanticsContainer(IosAccessibilityBridge owner, SemanticsElement element)
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

    // The container's frame is never used; the engine reports the screen's bounds.
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

            return ReferenceEquals(node, _owner.Bridge.Root)
                ? _owner.View
                : _owner.ObjectFor(node.Parent)?.AccessibilityContainer;
        }

        set { }
    }

    private IReadOnlyList<AccessibilityNode> Children => _owner.Bridge.GetNode(_element.Id)?.Children ?? [];

    [Export("accessibilityElementCount")]
    public nint AccessibilityElementCount() => Children.Count + 1;

    [Export("accessibilityElementAtIndex:")]
    public NSObject? GetAccessibilityElementAt(nint index)
    {
        IReadOnlyList<AccessibilityNode> children = Children;
        if (index < 0 || index > children.Count)
        {
            return null;
        }

        if (index == 0)
        {
            return _element.NativeAccessibility;
        }

        AccessibilityNode child = children[(int)index - 1];
        SemanticsElement? element = _owner.ObjectFor(child);
        return child.HasChildren ? element?.AccessibilityContainer : element?.NativeAccessibility;
    }

    [Export("indexOfAccessibilityElement:")]
    public nint GetIndexOfAccessibilityElement(NSObject element)
    {
        if (ReferenceEquals(element, _element.NativeAccessibility))
        {
            return 0;
        }

        IReadOnlyList<AccessibilityNode> children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (_owner.ObjectFor(children[i]) is not { } child)
            {
                continue;
            }

            if ((!children[i].HasChildren && ReferenceEquals(child.NativeAccessibility, element))
                || (children[i].HasChildren
                    && ReferenceEquals(ContainerOf(child.NativeAccessibility), element)))
            {
                return i + 1;
            }
        }

        return nint.MaxValue; // NSNotFound.
    }

    public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction) =>
        _element.AccessibilityScroll(direction);

    private static NSObject? ContainerOf(NSObject nativeAccessibility) =>
        nativeAccessibility switch
        {
            SemanticsElement element => element.AccessibilityContainer,
            SemanticsScrollView scrollView => scrollView.AccessibilityContainerValue,
            _ => null,
        };
}
