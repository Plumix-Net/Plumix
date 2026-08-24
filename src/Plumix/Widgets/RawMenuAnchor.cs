using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/raw_menu_anchor.dart

/// <summary>The anchor geometry handed to a <see cref="RawMenuAnchor"/> overlay builder.</summary>
public sealed record RawMenuOverlayInfo(
    Rect AnchorRect,
    Size OverlaySize,
    object TapRegionGroupId,
    Vector? Position = null);

public delegate Widget RawMenuAnchorOverlayBuilder(BuildContext context, RawMenuOverlayInfo info);

public delegate Widget RawMenuAnchorChildBuilder(
    BuildContext context,
    MenuController controller,
    Widget? child);

public delegate void RawMenuAnchorOpenRequestedCallback(Vector? position, Action showOverlay);

public delegate void RawMenuAnchorCloseRequestedCallback(Action hideOverlay);

/// <summary>Controls the menu owned by a <see cref="RawMenuAnchor"/> or <see cref="RawMenuAnchorGroup"/>.</summary>
public class MenuController
{
    internal RawMenuAnchorBaseState? Anchor { get; private set; }

    /// <summary>Whether the attached menu is open.</summary>
    public bool IsOpen => Anchor?.IsOpen ?? false;

    /// <summary>Opens the attached menu, optionally offset from the anchor's top-left corner.</summary>
    public virtual void Open(Vector? position = null)
    {
        if (Anchor is null)
        {
            throw new InvalidOperationException(
                "MenuController.Open requires the controller to be attached to a menu anchor.");
        }

        Anchor.HandleOpenRequest(position);
    }

    /// <summary>Closes the attached menu. Does nothing when the controller is not attached.</summary>
    public virtual void Close()
    {
        Anchor?.HandleCloseRequest();
    }

    /// <summary>Requests that every child menu of the attached anchor close.</summary>
    public virtual void CloseChildren()
    {
        if (Anchor is null)
        {
            throw new InvalidOperationException(
                "MenuController.CloseChildren requires the controller to be attached to a menu anchor.");
        }

        Anchor.RequestChildrenClose();
    }

    /// <summary>Returns the nearest ancestor menu controller without creating a dependency.</summary>
    public static MenuController? MaybeOf(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetInherited<MenuControllerScope>()?.Controller;
    }

    /// <summary>Returns whether the nearest ancestor menu is open, creating a dependency on that state.</summary>
    public static bool? MaybeIsOpenOf(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.DependOnInherited<MenuControllerScope>()?.IsOpen;
    }

    internal void Attach(RawMenuAnchorBaseState anchor)
    {
        Anchor = anchor;
    }

    internal void Detach(RawMenuAnchorBaseState anchor)
    {
        if (ReferenceEquals(Anchor, anchor))
        {
            Anchor = null;
        }
    }
}

/// <summary>Closes the whole menu tree that owns <see cref="Controller"/>.</summary>
public sealed class DismissMenuAction : DismissAction
{
    public DismissMenuAction(MenuController controller)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public MenuController Controller { get; }

    public override bool IsEnabled(DismissIntent intent) => Controller.Anchor is not null;

    public override object? Invoke(DismissIntent intent)
    {
        Controller.Anchor!.Root.HandleCloseRequest();
        return null;
    }
}

internal sealed class MenuControllerScope : InheritedWidget
{
    public MenuControllerScope(MenuController controller, bool isOpen, Widget child) : base()
    {
        Controller = controller;
        IsOpen = isOpen;
        Child = child;
    }

    public MenuController Controller { get; }

    public bool IsOpen { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        ((MenuControllerScope)oldWidget).IsOpen != IsOpen;
}

/// <summary>Shared anchor-tree behavior for <see cref="RawMenuAnchor"/> and <see cref="RawMenuAnchorGroup"/>.</summary>
/// <remarks>Ports Flutter's private `_RawMenuAnchorBaseMixin`; C# has no mixins, so it is a base class.</remarks>
public abstract class RawMenuAnchorBaseState : State
{
    private readonly List<RawMenuAnchorBaseState> _anchorChildren = [];
    private RawMenuAnchorBaseState? _parent;
    private ScrollPosition? _scrollPosition;
    private Size? _viewSize;

    internal bool IsRoot => _parent is null;

    internal RawMenuAnchorBaseState? AnchorParent => _parent;

    internal IReadOnlyList<RawMenuAnchorBaseState> AnchorChildren => _anchorChildren;

    internal RawMenuAnchorBaseState Root
    {
        get
        {
            RawMenuAnchorBaseState anchor = this;
            while (anchor._parent is not null)
            {
                anchor = anchor._parent;
            }

            return anchor;
        }
    }

    internal abstract MenuController MenuController { get; }

    internal abstract bool IsOpen { get; }

    internal abstract void Open(Vector? position = null);

    internal abstract void Close(bool inDispose = false);

    internal abstract void HandleOpenRequest(Vector? position = null);

    internal abstract void HandleCloseRequest();

    protected abstract Widget BuildAnchor(BuildContext context);

    public override void InitState()
    {
        MenuController.Attach(this);
    }

    public override void DidChangeDependencies()
    {
        RawMenuAnchorBaseState? newParent = MenuController.MaybeOf(Context)?.Anchor;
        if (!ReferenceEquals(newParent, _parent))
        {
            if (ReferenceEquals(newParent, this))
            {
                throw new InvalidOperationException(
                    "A MenuController should only be attached to one anchor at a time.");
            }

            _parent?.RemoveChild(this);
            _parent = newParent;
            _parent?.AddChild(this);
        }

        if (!IsRoot)
        {
            return;
        }

        if (_scrollPosition is not null)
        {
            _scrollPosition.IsScrollingNotifier.RemoveListener(HandleScroll);
        }

        _scrollPosition = Scrollable.MaybeOf(Context)?.Position;
        _scrollPosition?.IsScrollingNotifier.AddListener(HandleScroll);

        Size? newSize = MediaQuery.MaybeSizeOf(Context);
        if (_viewSize.HasValue && newSize.HasValue && newSize.Value != _viewSize.Value && IsOpen)
        {
            HandleCloseRequest();
        }

        _viewSize = newSize;
    }

    public override void Dispose()
    {
        if (IsOpen)
        {
            Close(inDispose: true);
        }

        if (_scrollPosition is not null)
        {
            _scrollPosition.IsScrollingNotifier.RemoveListener(HandleScroll);
            _scrollPosition = null;
        }

        _parent?.RemoveChild(this);
        _parent = null;
        _anchorChildren.Clear();
        MenuController.Detach(this);
    }

    public sealed override Widget Build(BuildContext context)
    {
        var actions = new Dictionary<Type, FlutterAction>();
        if (IsOpen)
        {
            actions[typeof(DismissIntent)] = new DismissMenuAction(MenuController);
        }

        return new MenuControllerScope(
            controller: MenuController,
            isOpen: IsOpen,
            child: new Actions(
                actions: actions,
                child: new Builder(BuildAnchor)));
    }

    /// <summary>Immediately closes every child anchor, bypassing their close-request interception.</summary>
    internal void CloseChildren(bool inDispose = false)
    {
        foreach (RawMenuAnchorBaseState child in _anchorChildren.ToArray())
        {
            child.Close(inDispose);
        }
    }

    /// <summary>Asks every child anchor to close, running each child's close-request interception.</summary>
    internal void RequestChildrenClose()
    {
        foreach (RawMenuAnchorBaseState child in _anchorChildren.ToArray())
        {
            child.HandleCloseRequest();
        }
    }

    internal void HandleOutsideTap(PointerDownEvent pointerDownEvent)
    {
        if (IsOpen)
        {
            RequestChildrenClose();
        }
    }

    internal void ChildChangedOpenState()
    {
        _parent?.ChildChangedOpenState();
        if (Scheduler.Phase != SchedulerPhase.PersistentCallbacks)
        {
            SetState(static () => { });
        }
        else
        {
            Scheduler.AddPostFrameCallback(_ => SetState(static () => { }));
        }
    }

    private void AddChild(RawMenuAnchorBaseState child)
    {
        if (!_anchorChildren.Contains(child))
        {
            _anchorChildren.Add(child);
        }
    }

    private void RemoveChild(RawMenuAnchorBaseState child)
    {
        _anchorChildren.Remove(child);
    }

    private void HandleScroll()
    {
        if (IsOpen)
        {
            HandleCloseRequest();
        }
    }
}

/// <summary>A menu anchor that shows an overlay built by <see cref="OverlayBuilder"/>.</summary>
public sealed class RawMenuAnchor : StatefulWidget
{
    public RawMenuAnchor(
        MenuController controller,
        RawMenuAnchorOverlayBuilder overlayBuilder,
        FocusNode? childFocusNode = null,
        bool consumeOutsideTaps = false,
        Action? onOpen = null,
        Action? onClose = null,
        RawMenuAnchorOpenRequestedCallback? onOpenRequested = null,
        RawMenuAnchorCloseRequestedCallback? onCloseRequested = null,
        bool useRootOverlay = false,
        RawMenuAnchorChildBuilder? builder = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        OverlayBuilder = overlayBuilder ?? throw new ArgumentNullException(nameof(overlayBuilder));
        ChildFocusNode = childFocusNode;
        ConsumeOutsideTaps = consumeOutsideTaps;
        OnOpen = onOpen;
        OnClose = onClose;
        OnOpenRequested = onOpenRequested ?? DefaultOnOpenRequested;
        OnCloseRequested = onCloseRequested ?? DefaultOnCloseRequested;
        UseRootOverlay = useRootOverlay;
        Builder = builder;
        Child = child;
    }

    public MenuController Controller { get; }

    public RawMenuAnchorOverlayBuilder OverlayBuilder { get; }

    public FocusNode? ChildFocusNode { get; }

    public bool ConsumeOutsideTaps { get; }

    public Action? OnOpen { get; }

    public Action? OnClose { get; }

    public RawMenuAnchorOpenRequestedCallback OnOpenRequested { get; }

    public RawMenuAnchorCloseRequestedCallback OnCloseRequested { get; }

    public bool UseRootOverlay { get; }

    public RawMenuAnchorChildBuilder? Builder { get; }

    public Widget? Child { get; }

    /// <summary>The shortcut map installed on every raw menu anchor child.</summary>
    public static IReadOnlyDictionary<ShortcutActivator, Intent> MenuTraversalShortcuts { get; } =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.GameButtonA)] = new ActivateIntent(),
            [new SingleActivator(LogicalKeyboardKey.Escape)] = new DismissIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new DirectionalFocusIntent(TraversalDirection.Down),
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DirectionalFocusIntent(TraversalDirection.Up),
            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = new DirectionalFocusIntent(TraversalDirection.Left),
            [new SingleActivator(LogicalKeyboardKey.ArrowRight)] = new DirectionalFocusIntent(TraversalDirection.Right),
        };

    public override State CreateState() => new RawMenuAnchorState();

    private static void DefaultOnOpenRequested(Vector? position, Action showOverlay) => showOverlay();

    private static void DefaultOnCloseRequested(Action hideOverlay) => hideOverlay();
}

public sealed class RawMenuAnchorState : RawMenuAnchorBaseState
{
    private readonly OverlayPortalController _overlayController = new("MenuAnchor controller");
    private Vector? _menuPosition;

    private RawMenuAnchor Current => (RawMenuAnchor)StateWidget;

    internal override MenuController MenuController => Current.Controller;

    internal override bool IsOpen => _overlayController.IsShowing;

    /// <summary>Whether this anchor owns the overlay location, i.e. no ancestor anchor supplies one.</summary>
    internal bool IsRootOverlayAnchor => AnchorParent is not RawMenuAnchorState;

    internal bool UseRootOverlay => AnchorParent is RawMenuAnchorState parentAnchor
        ? parentAnchor.UseRootOverlay
        : Current.UseRootOverlay;

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (RawMenuAnchor)oldWidget;
        if (!ReferenceEquals(previous.Controller, Current.Controller))
        {
            previous.Controller.Detach(this);
            Current.Controller.Attach(this);
        }
    }

    internal override void HandleOpenRequest(Vector? position = null)
    {
        Current.OnOpenRequested(position, () => Open(position));
    }

    internal override void Open(Vector? position = null)
    {
        if (!Mounted)
        {
            return;
        }

        if (IsOpen)
        {
            Close();
        }

        AnchorParent?.RequestChildrenClose();
        _menuPosition = position;
        AnchorParent?.ChildChangedOpenState();
        _overlayController.Show();
        if (IsRootOverlayAnchor)
        {
            Current.ChildFocusNode?.RequestFocus();
        }

        Current.OnOpen?.Invoke();
        SetState(static () => { });
    }

    internal override void Close(bool inDispose = false)
    {
        if (!IsOpen)
        {
            return;
        }

        CloseChildren(inDispose);
        if (Scheduler.Phase != SchedulerPhase.PersistentCallbacks)
        {
            _overlayController.Hide();
        }
        else if (!inDispose)
        {
            Scheduler.AddPostFrameCallback(_ => _overlayController.Hide());
        }

        if (inDispose)
        {
            return;
        }

        AnchorParent?.ChildChangedOpenState();
        Current.OnClose?.Invoke();
        if (Mounted && Scheduler.Phase != SchedulerPhase.PersistentCallbacks)
        {
            SetState(static () => { });
        }
    }

    internal override void HandleCloseRequest()
    {
        if (Scheduler.Phase != SchedulerPhase.PersistentCallbacks)
        {
            Current.OnCloseRequested(HideOverlay);
        }
        else
        {
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (Mounted)
                {
                    Current.OnCloseRequested(HideOverlay);
                }
            });
        }

        RequestChildrenClose();
    }

    protected override Widget BuildAnchor(BuildContext context)
    {
        Widget anchorChild = new Shortcuts(
            shortcuts: RawMenuAnchor.MenuTraversalShortcuts,
            includeSemantics: false,
            child: new TapRegion(
                child: new Builder(anchorContext =>
                    Current.Builder?.Invoke(anchorContext, MenuController, Current.Child)
                    ?? Current.Child
                    ?? new SizedBox()),
                groupId: Root.MenuController,
                consumeOutsideTaps: Root.IsOpen && Current.ConsumeOutsideTaps,
                onTapOutside: HandleOutsideTap,
                debugLabel: "RawMenuAnchor anchor"));

        return OverlayPortal.WithLayoutBuilder(
            controller: _overlayController,
            overlayLocation: UseRootOverlay
                ? OverlayChildLocation.RootOverlay
                : OverlayChildLocation.NearestOverlay,
            child: anchorChild,
            overlayChildBuilder: BuildOverlay);
    }

    private void HideOverlay() => Close();

    private Widget BuildOverlay(BuildContext context, OverlayChildLayoutInfo layoutInfo)
    {
        Rect anchorRect = RenderObject.TransformRect(
            layoutInfo.ChildPaintTransform,
            new Rect(default, layoutInfo.ChildSize));
        return Current.OverlayBuilder(
            context,
            new RawMenuOverlayInfo(
                anchorRect,
                layoutInfo.OverlaySize,
                Root.MenuController,
                _menuPosition));
    }
}

/// <summary>Groups sibling menu anchors so that only one of them is open at a time.</summary>
public sealed class RawMenuAnchorGroup : StatefulWidget
{
    public RawMenuAnchorGroup(Widget child, MenuController controller, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public Widget Child { get; }

    public MenuController Controller { get; }

    public override State CreateState() => new RawMenuAnchorGroupState();
}

public sealed class RawMenuAnchorGroupState : RawMenuAnchorBaseState
{
    private RawMenuAnchorGroup Current => (RawMenuAnchorGroup)StateWidget;

    internal override MenuController MenuController => Current.Controller;

    internal override bool IsOpen => AnchorChildren.Any(child => child.IsOpen);

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (RawMenuAnchorGroup)oldWidget;
        if (!ReferenceEquals(previous.Controller, Current.Controller))
        {
            previous.Controller.Detach(this);
            Current.Controller.Attach(this);
        }
    }

    internal override void Open(Vector? position = null)
    {
    }

    internal override void HandleOpenRequest(Vector? position = null)
    {
        Open(position);
    }

    internal override void HandleCloseRequest()
    {
        RequestChildrenClose();
    }

    internal override void Close(bool inDispose = false)
    {
        if (!IsOpen)
        {
            return;
        }

        CloseChildren(inDispose);
        if (inDispose)
        {
            return;
        }

        if (Scheduler.Phase != SchedulerPhase.PersistentCallbacks)
        {
            SetState(static () => { });
        }
        else
        {
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (Mounted)
                {
                    SetState(static () => { });
                }
            });
        }
    }

    protected override Widget BuildAnchor(BuildContext context)
    {
        return new TapRegion(
            child: Current.Child,
            groupId: Root.MenuController,
            onTapOutside: HandleOutsideTap,
            debugLabel: "RawMenuAnchorGroup");
    }
}
