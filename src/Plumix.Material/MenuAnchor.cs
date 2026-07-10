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

    public override State CreateState() => new MenuAnchorState();
}

public sealed class MenuAnchorState : State, IMenuControllerHost
{
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
            _isOpen,
            new MenuAnchorLayout(
                anchor,
                panel,
                Current.AlignmentOffset,
                Current.CrossAxisUnconstrained));
    }

    public void CloseChildren()
    {
        // SubmenuButton will register child controllers here. A leaf MenuAnchor has none.
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
        MenuStyle style = Current.Style ?? DropdownMenuTheme.Of(context).MenuStyle ?? new MenuStyle();
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
            style: MaterialButtonCore.ComposeStyles(defaults, null, Style, null),
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

internal sealed class MenuAnchorLayout : MultiChildRenderObjectWidget
{
    public MenuAnchorLayout(Widget anchor, Widget? panel, Vector offset, bool crossAxisUnconstrained)
        : base(panel is null ? [anchor] : [anchor, panel])
    {
        Offset = offset;
        CrossAxisUnconstrained = crossAxisUnconstrained;
    }

    public Vector Offset { get; }
    public bool CrossAxisUnconstrained { get; }
    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderMenuAnchorLayout(Offset, CrossAxisUnconstrained);
    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderMenuAnchorLayout)renderObject;
        layout.Offset = Offset;
        layout.CrossAxisUnconstrained = CrossAxisUnconstrained;
    }
}

internal sealed class MenuAnchorParentData : ContainerBoxParentData<RenderBox> { }

internal sealed class RenderMenuAnchorLayout : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, MenuAnchorParentData>, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, MenuAnchorParentData> _container;
    private Vector _offset;
    private bool _crossAxisUnconstrained;
    public RenderMenuAnchorLayout(Vector offset, bool crossAxisUnconstrained)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, MenuAnchorParentData>(this);
        _offset = offset;
        _crossAxisUnconstrained = crossAxisUnconstrained;
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
        ((MenuAnchorParentData)panel.parentData!).offset = new Point(Offset.X, Size.Height + Offset.Y);
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
