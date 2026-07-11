using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart.

public delegate Widget MenuAnchorChildBuilder(BuildContext context, MenuController controller, Widget? child);

public sealed class MenuAnchor : StatefulWidget
{
    public MenuAnchor(
        IReadOnlyList<Widget> menuChildren,
        MenuAnchorChildBuilder? builder = null,
        Widget? child = null,
        MenuController? controller = null,
        FocusNode? childFocusNode = null,
        MenuStyle? style = null,
        Vector? alignmentOffset = null,
        Thickness? reservedPadding = null,
        Clip clipBehavior = Clip.HardEdge,
        bool anchorTapClosesMenu = false,
        bool consumeOutsideTap = false,
        Action? onOpen = null,
        Action? onClose = null,
        bool crossAxisUnconstrained = true,
        bool useRootOverlay = false,
        bool animated = false,
        Action<bool>? onAnimationStatusChanged = null,
        Key? key = null) : base(key)
    {
        MenuChildren = menuChildren ?? throw new ArgumentNullException(nameof(menuChildren));
        Builder = builder;
        Child = child;
        Controller = controller;
        ChildFocusNode = childFocusNode;
        Style = style;
        AlignmentOffset = alignmentOffset ?? default;
        ReservedPadding = reservedPadding;
        ClipBehavior = clipBehavior;
        AnchorTapClosesMenu = anchorTapClosesMenu;
        ConsumeOutsideTap = consumeOutsideTap;
        OnOpen = onOpen;
        OnClose = onClose;
        CrossAxisUnconstrained = crossAxisUnconstrained;
        UseRootOverlay = useRootOverlay;
        Animated = animated;
        OnAnimationStatusChanged = onAnimationStatusChanged;
    }

    public IReadOnlyList<Widget> MenuChildren { get; }
    public MenuAnchorChildBuilder? Builder { get; }
    public Widget? Child { get; }
    public MenuController? Controller { get; }
    public FocusNode? ChildFocusNode { get; }
    public MenuStyle? Style { get; }
    public Vector AlignmentOffset { get; }
    public Thickness? ReservedPadding { get; }
    public Clip ClipBehavior { get; }
    public bool AnchorTapClosesMenu { get; }
    public bool ConsumeOutsideTap { get; }
    public Action? OnOpen { get; }
    public Action? OnClose { get; }
    public bool CrossAxisUnconstrained { get; }
    public bool UseRootOverlay { get; }
    public bool Animated { get; }
    public Action<bool>? OnAnimationStatusChanged { get; }

    // These assembly-local fields mirror the orientation contracts supplied by
    // Flutter's private _MenuBarAnchor and submenu layout delegates.
    internal Axis Orientation { get; init; } = Axis.Vertical;
    internal Axis PanelOrientation { get; init; } = Axis.Vertical;

    public override State CreateState() => new MenuAnchorState();
}

public sealed class MenuAnchorState : State, IMenuControllerHost
{
    private readonly HashSet<MenuController> _childControllers = [];
    private MenuController? _internalController;
    private MenuController? _controller;
    private bool _isOpen;
    private MenuAnchor Current => (MenuAnchor)StateWidget;

    public override void InitState() => AttachController();

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldAnchor = (MenuAnchor)oldWidget;
        if (!ReferenceEquals(oldAnchor.Controller, Current.Controller))
        {
            DetachController();
            AttachController();
        }
    }

    public override void Dispose() => DetachController();

    public override Widget Build(BuildContext context)
    {
        MenuController controller = _controller!;
        Widget anchor = Current.Builder?.Invoke(context, controller, Current.Child) ?? Current.Child ?? new SizedBox();
        Widget? panel = _isOpen ? BuildPanel(context) : null;
        return new MenuControllerScope(
            controller,
            this,
            _isOpen,
            Current.Orientation,
            new MenuAnchorLayout(
                anchor,
                panel,
                Current.AlignmentOffset,
                Current.CrossAxisUnconstrained,
                Current.PanelOrientation,
                Directionality.Of(context)));
    }

    public void CloseChildren()
    {
        CloseChildrenExcept(null);
    }

    internal void RegisterChild(MenuController controller) => _childControllers.Add(controller);

    internal void UnregisterChild(MenuController controller) => _childControllers.Remove(controller);

    internal void CloseChildrenExcept(MenuController? except)
    {
        foreach (MenuController controller in _childControllers.ToArray())
        {
            if (!ReferenceEquals(controller, except))
            {
                controller.Close();
            }
        }
    }

    private void AttachController()
    {
        _internalController = Current.Controller is null ? new MenuController() : null;
        _controller = Current.Controller ?? _internalController!;
        _controller.Attach(this, Open, Close);
    }

    private void DetachController()
    {
        _controller?.Detach(this);
        _controller = null;
        _internalController = null;
    }

    private void Open()
    {
        if (_isOpen) return;
        MenuController.MaybeScopeOf(Context)?.Host.CloseChildrenExcept(_controller);
        SetState(() => _isOpen = true);
        _controller!.SetOpen(true);
        Current.OnOpen?.Invoke();
        Current.OnAnimationStatusChanged?.Invoke(true);
    }

    private void Close()
    {
        if (!_isOpen) return;
        SetState(() => _isOpen = false);
        _controller!.SetOpen(false);
        Current.OnClose?.Invoke();
        Current.OnAnimationStatusChanged?.Invoke(false);
        Current.ChildFocusNode?.RequestFocus();
    }

    private Widget BuildPanel(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        MenuStyle style = Current.Style ?? MenuTheme.Of(context).Style ?? new MenuStyle();
        MaterialState state = MaterialState.None;
        Color color = style.BackgroundColor?.Resolve(state) ?? theme.SurfaceContainerColor;
        Color shadowColor = style.ShadowColor?.Resolve(state) ?? theme.ShadowColor;
        Color surfaceTint = style.SurfaceTintColor?.Resolve(state) ?? Colors.Transparent;
        double elevation = style.Elevation?.Resolve(state) ?? 3;
        Thickness padding = style.Padding?.Resolve(state) ?? new Thickness(0, 8);
        ShapeBorder? shape = style.Shape?.Resolve(state);
        Widget content = new Padding(
            padding,
            new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: Current.MenuChildren));
        return new Material(
            type: MaterialType.Card,
            color: color,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTint,
            elevation: elevation,
            shape: shape,
            clipBehavior: Current.ClipBehavior,
            child: content);
    }
}

public sealed class MenuItemButton : StatelessWidget
{
    public MenuItemButton(
        Widget? child = null,
        Action? onPressed = null,
        Action<bool>? onHover = null,
        bool requestFocusOnHover = true,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        string? semanticsLabel = null,
        ButtonStyle? style = null,
        MaterialStatesController? statesController = null,
        Clip clipBehavior = Clip.None,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        bool closeOnActivate = true,
        Axis overflowAxis = Axis.Horizontal,
        Key? key = null) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        OnHover = onHover;
        RequestFocusOnHover = requestFocusOnHover;
        OnFocusChange = onFocusChange;
        FocusNode = focusNode;
        Autofocus = autofocus;
        SemanticsLabel = semanticsLabel;
        Style = style;
        StatesController = statesController;
        ClipBehavior = clipBehavior;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        CloseOnActivate = closeOnActivate;
        OverflowAxis = overflowAxis;
    }

    public Widget? Child { get; }
    public Action? OnPressed { get; }
    public Action<bool>? OnHover { get; }
    public bool RequestFocusOnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public string? SemanticsLabel { get; }
    public ButtonStyle? Style { get; }
    public MaterialStatesController? StatesController { get; }
    public Clip ClipBehavior { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public bool CloseOnActivate { get; }
    public Axis OverflowAxis { get; }
    public bool Enabled => OnPressed is not null;

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        Widget label = Child ?? new SizedBox();
        var row = new List<Widget>();
        if (LeadingIcon is not null) row.Add(LeadingIcon);
        row.Add(new Flexible(child: label));
        if (TrailingIcon is not null) row.Add(TrailingIcon);
        Widget content = new Row(
            mainAxisSize: MainAxisSize.Max,
            spacing: 12,
            children: row,
            textDirection: Directionality.Of(context));
        ButtonStyle defaults = new(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled) ? theme.DisabledColor : theme.OnSurfaceColor),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(theme.OnSurfaceColor),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 0)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(112, 48)),
            Shape: MaterialStateProperty<BorderRadius?>.All(BorderRadius.Zero),
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap);
        Action? activate = OnPressed is null ? null : () =>
        {
            OnPressed();
            if (CloseOnActivate) MenuController.MaybeOf(context)?.Close();
        };
        return new MaterialButtonCore(
            child: content,
            onPressed: activate,
            style: MaterialButtonCore.ComposeStyles(
                defaults,
                MenuButtonTheme.Of(context).Style,
                Style,
                null),
            onHoverChanged: value =>
            {
                OnHover?.Invoke(value);
                if (value && RequestFocusOnHover) FocusNode?.RequestFocus();
            },
            onFocusChange: OnFocusChange,
            focusNode: FocusNode,
            autofocus: Autofocus,
            semanticLabel: SemanticsLabel,
            clipBehavior: ClipBehavior,
            enabled: Enabled);
    }
}

/// <summary>A horizontal collection of top-level <see cref="SubmenuButton"/> controls.</summary>
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart (MenuBar).</remarks>
public sealed class MenuBar : StatelessWidget
{
    public MenuBar(
        IReadOnlyList<Widget> children,
        MenuStyle? style = null,
        Clip clipBehavior = Clip.None,
        MenuController? controller = null,
        Key? key = null) : base(key)
    {
        Children = children ?? throw new ArgumentNullException(nameof(children));
        Style = style;
        ClipBehavior = clipBehavior;
        Controller = controller;
    }

    public IReadOnlyList<Widget> Children { get; }
    public MenuStyle? Style { get; }
    public Clip ClipBehavior { get; }
    public MenuController? Controller { get; }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        MenuStyle style = Style ?? MenuBarTheme.Of(context).Style ?? new MenuStyle();
        MaterialState states = MaterialState.None;
        Color color = style.BackgroundColor?.Resolve(states) ?? theme.SurfaceContainerColor;
        Color shadowColor = style.ShadowColor?.Resolve(states) ?? theme.ShadowColor;
        Color surfaceTint = style.SurfaceTintColor?.Resolve(states) ?? Colors.Transparent;
        double elevation = style.Elevation?.Resolve(states) ?? 3;
        Thickness padding = style.Padding?.Resolve(states) ?? new Thickness(8, 0);
        ShapeBorder? shape = style.Shape?.Resolve(states);

        Widget content = new Material(
            type: MaterialType.Card,
            color: color,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTint,
            elevation: elevation,
            shape: shape,
            clipBehavior: ClipBehavior,
            child: new Padding(
                padding,
                new Row(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: Children,
                    textDirection: Directionality.Of(context))));

        return new MenuAnchor(
            menuChildren: [],
            controller: Controller,
            clipBehavior: ClipBehavior,
            builder: (_, _, _) => content)
        {
            Orientation = Axis.Horizontal,
        };
    }
}

/// <summary>A menu button that opens a nested vertical menu.</summary>
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart (SubmenuButton).</remarks>
public sealed class SubmenuButton : StatefulWidget
{
    public SubmenuButton(
        IReadOnlyList<Widget> menuChildren,
        Widget? child,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        Action? onOpen = null,
        Action? onClose = null,
        MenuController? controller = null,
        ButtonStyle? style = null,
        MenuStyle? menuStyle = null,
        Vector? alignmentOffset = null,
        Clip clipBehavior = Clip.HardEdge,
        FocusNode? focusNode = null,
        MaterialStatesController? statesController = null,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        MaterialStateProperty<Widget?>? submenuIcon = null,
        bool useRootOverlay = false,
        TimeSpan? hoverOpenDelay = null,
        bool animated = false,
        Action<bool>? onAnimationStatusChanged = null,
        Key? key = null) : base(key)
    {
        MenuChildren = menuChildren ?? throw new ArgumentNullException(nameof(menuChildren));
        Child = child;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        OnOpen = onOpen;
        OnClose = onClose;
        Controller = controller;
        Style = style;
        MenuStyle = menuStyle;
        AlignmentOffset = alignmentOffset ?? default;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        StatesController = statesController;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        SubmenuIcon = submenuIcon;
        UseRootOverlay = useRootOverlay;
        HoverOpenDelay = hoverOpenDelay ?? TimeSpan.Zero;
        if (HoverOpenDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(hoverOpenDelay));
        }
        Animated = animated;
        OnAnimationStatusChanged = onAnimationStatusChanged;
    }

    public IReadOnlyList<Widget> MenuChildren { get; }
    public Widget? Child { get; }
    public Action<bool>? OnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public Action? OnOpen { get; }
    public Action? OnClose { get; }
    public MenuController? Controller { get; }
    public ButtonStyle? Style { get; }
    public MenuStyle? MenuStyle { get; }
    public Vector AlignmentOffset { get; }
    public Clip ClipBehavior { get; }
    public FocusNode? FocusNode { get; }
    public MaterialStatesController? StatesController { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public MaterialStateProperty<Widget?>? SubmenuIcon { get; }
    public bool UseRootOverlay { get; }
    public TimeSpan HoverOpenDelay { get; }
    public bool Animated { get; }
    public Action<bool>? OnAnimationStatusChanged { get; }
    public bool Enabled => MenuChildren.Count > 0;

    public override State CreateState() => new SubmenuButtonState();
}

public sealed class SubmenuButtonState : State
{
    private MenuController? _internalController;
    private MenuAnchorState? _parentHost;
    private SubmenuButton Current => (SubmenuButton)StateWidget;
    private MenuController Controller => Current.Controller ?? _internalController!;

    public override void InitState()
    {
        if (Current.Controller is null)
        {
            _internalController = new MenuController();
        }
    }

    public override void DidChangeDependencies()
    {
        MenuAnchorState? nextParent = MenuController.MaybeScopeOf(Context)?.Host;
        if (ReferenceEquals(nextParent, _parentHost)) return;
        _parentHost?.UnregisterChild(Controller);
        _parentHost = nextParent;
        _parentHost?.RegisterChild(Controller);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldButton = (SubmenuButton)oldWidget;
        if (!ReferenceEquals(oldButton.Controller, Current.Controller))
        {
            _parentHost?.UnregisterChild(oldButton.Controller ?? _internalController!);
            _internalController = Current.Controller is null ? new MenuController() : null;
            _parentHost?.RegisterChild(Controller);
        }
    }

    public override void Dispose()
    {
        _parentHost?.UnregisterChild(Controller);
    }

    public override Widget Build(BuildContext context)
    {
        MenuControllerScope? parentScope = MenuController.MaybeScopeOf(context);
        Axis parentOrientation = parentScope?.Orientation ?? Axis.Vertical;
        MaterialState states = Current.StatesController?.Value ?? MaterialState.None;
        if (!Current.Enabled)
        {
            states |= MaterialState.Disabled;
        }
        Widget submenuIcon = Current.SubmenuIcon?.Resolve(states)
            ?? MenuTheme.Of(context).SubmenuIcon?.Resolve(states)
            ?? new Icon(parentOrientation == Axis.Horizontal ? Icons.ArrowDropDown : Icons.ChevronRight);
        var row = new List<Widget>();
        if (Current.LeadingIcon is not null) row.Add(Current.LeadingIcon);
        row.Add(new Flexible(child: Current.Child ?? new SizedBox()));
        if (Current.TrailingIcon is not null) row.Add(Current.TrailingIcon);
        row.Add(submenuIcon);

        ThemeData theme = Theme.Of(context);
        ButtonStyle defaults = new(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(value =>
                value.HasFlag(MaterialState.Disabled) ? theme.DisabledColor : theme.OnSurfaceColor),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(theme.OnSurfaceColor),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 0)),
            MinimumSize: MaterialStateProperty<Size?>.All(
                parentOrientation == Axis.Horizontal ? new Size(64, 48) : new Size(112, 48)),
            Shape: MaterialStateProperty<BorderRadius?>.All(BorderRadius.Zero),
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap);
        Widget button = new MaterialButtonCore(
            child: new Row(
                mainAxisSize: MainAxisSize.Max,
                spacing: 12,
                children: row,
                textDirection: Directionality.Of(context)),
            onPressed: Current.Enabled ? Toggle : null,
            style: MaterialButtonCore.ComposeStyles(
                defaults,
                MenuButtonTheme.Of(context).Style,
                Current.Style,
                null),
            onHoverChanged: HandleHover,
            onFocusChange: Current.OnFocusChange,
            focusNode: Current.FocusNode,
            clipBehavior: Current.ClipBehavior,
            enabled: Current.Enabled);

        return new MenuAnchor(
            menuChildren: Current.MenuChildren,
            controller: Controller,
            childFocusNode: Current.FocusNode,
            style: Current.MenuStyle,
            alignmentOffset: Current.AlignmentOffset,
            clipBehavior: Current.ClipBehavior,
            onOpen: () =>
            {
                SetState(() => { });
                Current.OnOpen?.Invoke();
            },
            onClose: () =>
            {
                SetState(() => { });
                Current.OnClose?.Invoke();
            },
            useRootOverlay: Current.UseRootOverlay,
            animated: Current.Animated,
            onAnimationStatusChanged: Current.OnAnimationStatusChanged,
            child: new Semantics(expanded: Controller.IsOpen, child: button))
        {
            PanelOrientation = parentOrientation == Axis.Horizontal ? Axis.Vertical : Axis.Horizontal,
        };
    }

    private void Toggle()
    {
        if (Controller.IsOpen)
        {
            Controller.Close();
        }
        else
        {
            Controller.Open();
        }
    }

    private void HandleHover(bool hovered)
    {
        Current.OnHover?.Invoke(hovered);
        if (!hovered || !Current.Enabled) return;
        if (Current.HoverOpenDelay == TimeSpan.Zero)
        {
            Controller.Open();
        }
        else
        {
            _ = DelayOpenAsync(Current.HoverOpenDelay);
        }
    }

    private async Task DelayOpenAsync(TimeSpan delay)
    {
        await Task.Delay(delay);
        if (Mounted && Current.Enabled)
        {
            Controller.Open();
        }
    }
}

internal sealed class MenuAnchorLayout : MultiChildRenderObjectWidget
{
    public MenuAnchorLayout(Widget anchor, Widget? panel, Vector offset, bool crossAxisUnconstrained)
        : this(
            anchor,
            panel,
            offset,
            crossAxisUnconstrained,
            Axis.Vertical,
            TextDirection.Ltr)
    {
    }

    public MenuAnchorLayout(
        Widget anchor,
        Widget? panel,
        Vector offset,
        bool crossAxisUnconstrained,
        Axis panelOrientation,
        TextDirection textDirection)
        : base(panel is null ? [anchor] : [anchor, panel])
    {
        Offset = offset;
        CrossAxisUnconstrained = crossAxisUnconstrained;
        PanelOrientation = panelOrientation;
        TextDirection = textDirection;
    }

    public Vector Offset { get; }
    public bool CrossAxisUnconstrained { get; }
    public Axis PanelOrientation { get; }
    public TextDirection TextDirection { get; }
    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderMenuAnchorLayout(Offset, CrossAxisUnconstrained, PanelOrientation, TextDirection);
    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderMenuAnchorLayout)renderObject;
        layout.Offset = Offset;
        layout.CrossAxisUnconstrained = CrossAxisUnconstrained;
        layout.PanelOrientation = PanelOrientation;
        layout.TextDirection = TextDirection;
    }
}

internal sealed class MenuAnchorParentData : ContainerBoxParentData<RenderBox> { }

internal sealed class RenderMenuAnchorLayout : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, MenuAnchorParentData>, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, MenuAnchorParentData> _container;
    private Vector _offset;
    private bool _crossAxisUnconstrained;
    private Axis _panelOrientation;
    private TextDirection _textDirection;
    public RenderMenuAnchorLayout(
        Vector offset,
        bool crossAxisUnconstrained,
        Axis panelOrientation,
        TextDirection textDirection)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, MenuAnchorParentData>(this);
        _offset = offset;
        _crossAxisUnconstrained = crossAxisUnconstrained;
        _panelOrientation = panelOrientation;
        _textDirection = textDirection;
    }
    public Vector Offset { get => _offset; set { if (_offset == value) return; _offset = value; MarkNeedsLayout(); } }
    public bool CrossAxisUnconstrained
    {
        get => _crossAxisUnconstrained;
        set
        {
            if (_crossAxisUnconstrained == value) return;
            _crossAxisUnconstrained = value;
            MarkNeedsLayout();
        }
    }
    public Axis PanelOrientation
    {
        get => _panelOrientation;
        set
        {
            if (_panelOrientation == value) return;
            _panelOrientation = value;
            MarkNeedsLayout();
        }
    }
    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value) return;
            _textDirection = value;
            MarkNeedsLayout();
        }
    }
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public int ChildCount => _container.ChildCount;
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);
    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not MenuAnchorParentData)
        {
            child.parentData = new MenuAnchorParentData();
        }
    }
    protected override void PerformLayout()
    {
        RenderBox? anchor = FirstChild;
        if (anchor is null) { Size = Constraints.Smallest; return; }
        anchor.Layout(Constraints, parentUsesSize: true);
        Size = Constraints.Constrain(anchor.Size);
        ((MenuAnchorParentData)anchor.parentData!).offset = default;
        RenderBox? panel = ChildAfter(anchor);
        if (panel is null) return;
        double maxWidth = CrossAxisUnconstrained ? Constraints.MaxWidth : Size.Width;
        if (double.IsPositiveInfinity(maxWidth)) maxWidth = 10000;
        double maxHeight = double.IsPositiveInfinity(Constraints.MaxHeight) ? 10000 : Constraints.MaxHeight;
        panel.Layout(BoxConstraints.Loose(new Size(maxWidth, maxHeight)), parentUsesSize: true);
        Point panelOffset = PanelOrientation == Axis.Horizontal
            ? new Point(
                TextDirection == TextDirection.Ltr ? Size.Width + Offset.X : -panel.Size.Width + Offset.X,
                Offset.Y)
            : new Point(Offset.X, Size.Height + Offset.Y);
        ((MenuAnchorParentData)panel.parentData!).offset = panelOffset;
    }
    public override void Paint(PaintingContext ctx, Point offset) => _container.DefaultPaint(ctx, offset);
    public void DefaultPaint(PaintingContext ctx, Point offset) => _container.DefaultPaint(ctx, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (!HasSize) return false;
        if (_container.DefaultHitTestChildren(result, position))
        {
            result.Add(new BoxHitTestEntry(this, position));
            return true;
        }
        return position.X >= 0 && position.Y >= 0 && position.X <= Size.Width && position.Y <= Size.Height;
    }
    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);
    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }
    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child, ((MenuAnchorParentData)child.parentData!).offset, Matrix.Identity);
        }
    }
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}
