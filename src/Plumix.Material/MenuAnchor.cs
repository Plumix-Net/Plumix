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
        LayerLink? layerLink = null,
        Clip clipBehavior = Clip.HardEdge,
        bool anchorTapClosesMenu = false,
        bool consumeOutsideTap = false,
        Action? onOpen = null,
        Action? onClose = null,
        bool crossAxisUnconstrained = true,
        bool useRootOverlay = false,
        bool animated = false,
        Action<AnimationStatus>? onAnimationStatusChanged = null,
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
        LayerLink = layerLink;
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
    public LayerLink? LayerLink { get; }
    public Clip ClipBehavior { get; }
    public bool AnchorTapClosesMenu { get; }
    public bool ConsumeOutsideTap { get; }
    public Action? OnOpen { get; }
    public Action? OnClose { get; }
    public bool CrossAxisUnconstrained { get; }
    public bool UseRootOverlay { get; }
    public bool Animated { get; }
    public Action<AnimationStatus>? OnAnimationStatusChanged { get; }

    // These assembly-local fields mirror the orientation contracts supplied by
    // Flutter's private _MenuBarAnchor and submenu layout delegates.
    internal Axis Orientation { get; init; } = Axis.Vertical;
    internal Axis PanelOrientation { get; init; } = Axis.Vertical;

    public override State CreateState() => new MenuAnchorState();
}

public sealed class MenuAnchorState : State, IMenuControllerHost
{
    private readonly HashSet<MenuController> _childControllers = [];
    private readonly RawMenuAnchorController _rawController = new();
    private MenuController? _internalController;
    private MenuController? _controller;
    private AnimationController? _animationController;
    private CurvedAnimation? _heightAnimation;
    private CurvedAnimation? _opacityAnimation;
    private Size? _mediaSize;
    private bool _isOpen;
    private bool _isClosing;
    private MenuAnchor Current => (MenuAnchor)StateWidget;

    public override void InitState()
    {
        AttachController();
        _animationController = new AnimationController(
            TimeSpan.FromMilliseconds(500),
            this)
        {
            ReverseDuration = TimeSpan.FromMilliseconds(150),
        };
        _animationController.AddStatusListener(HandleAnimationStatusChanged);
        _heightAnimation = new CurvedAnimation(
            _animationController,
            Curves.Cubic(0.3, 0.0, 0.0, 1.0),
            Curves.Interval(0.35, 1.0, Curves.Flipped(Curves.EmphasizedAccelerate)));
        _opacityAnimation = new CurvedAnimation(
            _animationController,
            Curves.Interval(0.0, 0.1),
            Curves.Interval(0.0, 1.0 / 3.0));
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldAnchor = (MenuAnchor)oldWidget;
        if (!ReferenceEquals(oldAnchor.Controller, Current.Controller))
        {
            DetachController();
            AttachController();
        }

        if (oldAnchor.Animated && !Current.Animated && _isOpen)
        {
            _animationController!.Stop();
            _animationController.SetValue(_isClosing ? 0.0 : 1.0);
        }
    }

    public override void DidChangeDependencies()
    {
        Size? nextSize = MediaQuery.MaybeOf(Context)?.Size;
        bool changedWhileOpen = _mediaSize.HasValue
                                && nextSize.HasValue
                                && _mediaSize.Value != nextSize.Value
                                && _isOpen;
        _mediaSize = nextSize;
        if (changedWhileOpen)
        {
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (Mounted)
                {
                    Close();
                }
            });
        }
    }

    public override void Dispose()
    {
        DetachController();
        _heightAnimation?.Dispose();
        _opacityAnimation?.Dispose();
        _animationController?.RemoveStatusListener(HandleAnimationStatusChanged);
        _animationController?.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        MenuController controller = _controller!;
        Widget anchor = Current.Builder?.Invoke(context, controller, Current.Child) ?? Current.Child ?? new SizedBox();
        Widget rawAnchor = new RawMenuAnchor(
            controller: _rawController,
            overlayBuilder: BuildOverlay,
            child: anchor,
            onTapOutside: Close,
            consumeOutsideTaps: Current.ConsumeOutsideTap,
            useRootOverlay: Current.UseRootOverlay);
        if (Current.LayerLink is not null)
        {
            rawAnchor = new CompositedTransformTarget(Current.LayerLink, rawAnchor);
        }
        return new MenuControllerScope(
            controller,
            this,
            _isOpen,
            Current.Orientation,
            rawAnchor);
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
        if (_animationController!.Status.IsForwardOrCompleted()) return;
        MenuController.MaybeScopeOf(Context)?.Host.CloseChildrenExcept(_controller);
        _rawController.Open(_controller?.Position);
        Current.ChildFocusNode?.RequestFocus();
        SetState(() =>
        {
            _isOpen = true;
            _isClosing = false;
        });
        _controller!.SetOpen(true);
        Current.OnOpen?.Invoke();
        if (Current.Animated)
        {
            _animationController.Forward();
        }
        else
        {
            _animationController.Stop();
            _animationController.SetValue(1.0);
        }
    }

    private void Close()
    {
        if (!_isOpen || _isClosing) return;
        CloseChildren();
        SetState(() => _isClosing = true);
        Current.ChildFocusNode?.RequestFocus();
        if (Current.Animated)
        {
            _animationController!.Reverse();
        }
        else
        {
            _animationController!.Stop();
            _animationController.SetValue(0.0);
        }
    }

    private Widget BuildOverlay(BuildContext context, RawMenuOverlayInfo info)
    {
        Widget panel = new AnimatedBuilder(
            animation: _animationController!,
            builder: (_, child) => new FadeTransition(
                opacity: _opacityAnimation!,
                alwaysIncludeSemantics: true,
                child: new Align(
                    alignment: AlignmentDirectional.TopStart,
                    heightFactor: _heightAnimation!.Value,
                    widthFactor: 1.0,
                    child: child)),
            child: BuildPanel(context));
        panel = new ExcludeSemantics(
            excluding: _isClosing,
            child: new IgnorePointer(
                ignoring: _isClosing,
                child: new ExcludeFocus(
                    excluding: _isClosing,
                    child: panel)));
        Thickness reservedPadding = Current.ReservedPadding ?? new Thickness(8.0);
        MediaQueryData? mediaQuery = MediaQuery.MaybeOf(context);
        Widget layout = new CustomSingleChildLayout(
            layoutDelegate: new MenuOverlayLayoutDelegate(
                info.AnchorRect,
                Current.AlignmentOffset,
                reservedPadding,
                Current.PanelOrientation,
                Directionality.Of(context),
                mediaQuery?.ViewInsets ?? default,
                info.Position,
                mediaQuery?.DisplayFeatures),
            child: panel);
        return new TapRegion(
            child: layout,
            groupId: info.TapRegionGroupId,
            consumeOutsideTaps: Current.ConsumeOutsideTap,
            onTapOutside: _ => Close(),
            debugLabel: "MenuAnchor panel");
    }

    private Widget BuildPanel(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        MenuStyle style = (Current.Style ?? new MenuStyle()).Merge(MenuTheme.Of(context).Style);
        MaterialState state = MaterialState.None;
        Color color = style.BackgroundColor?.Resolve(state) ?? theme.SurfaceContainerColor;
        Color shadowColor = style.ShadowColor?.Resolve(state) ?? theme.ShadowColor;
        Color surfaceTint = style.SurfaceTintColor?.Resolve(state) ?? Colors.Transparent;
        double elevation = style.Elevation?.Resolve(state) ?? 3;
        Thickness padding = style.Padding?.Resolve(state) ?? new Thickness(0, 8);
        VisualDensity density = style.VisualDensity ?? theme.VisualDensity;
        double horizontalDensityPadding = Math.Max(0.0, density.BaseSizeAdjustment.X);
        padding = new Thickness(
            padding.Left + horizontalDensityPadding,
            padding.Top,
            padding.Right + horizontalDensityPadding,
            padding.Bottom);
        ShapeBorder? shape = style.Shape?.Resolve(state) ?? ShapeBorder.RoundedRectangle(4.0);
        BorderSide? side = style.Side?.Resolve(state);
        if (side.HasValue)
        {
            shape = shape with { Side = side };
        }
        Widget content = new Padding(
            padding,
            new IntrinsicWidth(
                child: new SingleChildScrollView(
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children: Current.MenuChildren),
                    physics: new ClampingScrollPhysics())));
        Widget panel = new Material(
            type: MaterialType.Card,
            color: color,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTint,
            elevation: elevation,
            shape: shape,
            clipBehavior: Current.ClipBehavior,
            child: content);
        return new ConstrainedBox(ResolveMenuConstraints(style, state, density), panel);
    }

    internal static BoxConstraints ResolveMenuConstraints(
        MenuStyle style,
        MaterialState state,
        VisualDensity density)
    {
        Size minimum = style.MinimumSize?.Resolve(state) ?? default;
        Size maximum = style.MaximumSize?.Resolve(state)
                       ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
        var constraints = new BoxConstraints(
            MinWidth: minimum.Width,
            MaxWidth: maximum.Width,
            MinHeight: minimum.Height,
            MaxHeight: maximum.Height);
        constraints = density.EffectiveConstraints(constraints);
        Size? fixedSize = style.FixedSize?.Resolve(state);
        if (fixedSize.HasValue)
        {
            double? width = double.IsFinite(fixedSize.Value.Width) ? fixedSize.Value.Width : null;
            double? height = double.IsFinite(fixedSize.Value.Height) ? fixedSize.Value.Height : null;
            constraints = constraints.Tighten(width, height);
        }
        return constraints;
    }

    private void HandleAnimationStatusChanged(AnimationStatus status)
    {
        Current.OnAnimationStatusChanged?.Invoke(status);
        if (status != AnimationStatus.Dismissed || !_isClosing)
        {
            if (Mounted)
            {
                SetState(static () => { });
            }
            return;
        }

        _rawController.Close();
        _controller!.SetOpen(false);
        SetState(() =>
        {
            _isOpen = false;
            _isClosing = false;
        });
        Current.OnClose?.Invoke();
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
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(64, 48)),
            Shape: MaterialStateProperty<BorderRadius?>.All(BorderRadius.Zero),
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap);
        Action? activate = OnPressed is null ? null : () =>
        {
            OnPressed();
            if (CloseOnActivate) MenuController.MaybeOf(context)?.Close();
        };
        Widget result = new MaterialButtonCore(
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
            statesController: StatesController,
            autofocus: Autofocus,
            semanticLabel: SemanticsLabel,
            clipBehavior: ClipBehavior,
            enabled: Enabled);

        if (Enabled && MenuAcceleratorLabel.PlatformSupportsAccelerators(context))
        {
            result = new MenuAcceleratorCallbackBinding(
                child: result,
                onInvoke: activate);
        }

        return result;
    }
}

/// <summary>A menu item that combines a <see cref="Checkbox"/> with a <see cref="MenuItemButton"/>.</summary>
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart.</remarks>
public sealed class CheckboxMenuButton : StatelessWidget
{
    public CheckboxMenuButton(
        bool? value,
        Action<bool?>? onChanged,
        Widget? child,
        bool tristate = false,
        bool isError = false,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        ButtonStyle? style = null,
        MaterialStatesController? statesController = null,
        Clip clipBehavior = Clip.None,
        Widget? trailingIcon = null,
        bool closeOnActivate = true,
        Key? key = null) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException(
                "CheckboxMenuButton value cannot be null when tristate is false.",
                nameof(value));
        }

        Value = value;
        OnChanged = onChanged;
        Child = child;
        Tristate = tristate;
        IsError = isError;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        FocusNode = focusNode;
        Style = style;
        StatesController = statesController;
        ClipBehavior = clipBehavior;
        TrailingIcon = trailingIcon;
        CloseOnActivate = closeOnActivate;
    }

    public bool? Value { get; }
    public Action<bool?>? OnChanged { get; }
    public Widget? Child { get; }
    public bool Tristate { get; }
    public bool IsError { get; }
    public Action<bool>? OnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public FocusNode? FocusNode { get; }
    public ButtonStyle? Style { get; }
    public MaterialStatesController? StatesController { get; }
    public Clip ClipBehavior { get; }
    public Widget? TrailingIcon { get; }
    public bool CloseOnActivate { get; }
    public bool Enabled => OnChanged is not null;

    public override Widget Build(BuildContext context)
    {
        return new MenuItemButton(
            child: Child,
            onPressed: OnChanged is null ? null : HandleChanged,
            onHover: OnHover,
            onFocusChange: OnFocusChange,
            focusNode: FocusNode,
            style: Style,
            statesController: StatesController,
            clipBehavior: ClipBehavior,
            leadingIcon: new ExcludeFocus(
                new IgnorePointer(
                    child: new ConstrainedBox(
                        new BoxConstraints(MaxWidth: Checkbox.Width, MaxHeight: Checkbox.Width),
                        new Checkbox(
                            value: Value,
                            onChanged: OnChanged,
                            tristate: Tristate,
                            isError: IsError)))),
            trailingIcon: TrailingIcon,
            closeOnActivate: CloseOnActivate,
            key: Key);
    }

    private void HandleChanged()
    {
        bool? nextValue = Value switch
        {
            false => true,
            true => Tristate ? null : false,
            null => false,
        };
        OnChanged!(nextValue);
    }
}

/// <summary>A menu item that combines a <see cref="Radio{T}"/> with a <see cref="MenuItemButton"/>.</summary>
/// <remarks>Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart.</remarks>
public sealed class RadioMenuButton<T> : StatelessWidget
{
    public RadioMenuButton(
        T value,
        T? groupValue,
        Action<T?>? onChanged,
        Widget? child,
        bool toggleable = false,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        ButtonStyle? style = null,
        MaterialStatesController? statesController = null,
        Clip clipBehavior = Clip.None,
        Widget? trailingIcon = null,
        bool closeOnActivate = true,
        Key? key = null) : base(key)
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        Child = child;
        Toggleable = toggleable;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        FocusNode = focusNode;
        Style = style;
        StatesController = statesController;
        ClipBehavior = clipBehavior;
        TrailingIcon = trailingIcon;
        CloseOnActivate = closeOnActivate;
    }

    public T Value { get; }
    public T? GroupValue { get; }
    public Action<T?>? OnChanged { get; }
    public Widget? Child { get; }
    public bool Toggleable { get; }
    public Action<bool>? OnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public FocusNode? FocusNode { get; }
    public ButtonStyle? Style { get; }
    public MaterialStatesController? StatesController { get; }
    public Clip ClipBehavior { get; }
    public Widget? TrailingIcon { get; }
    public bool CloseOnActivate { get; }
    public bool Enabled => OnChanged is not null;

    public override Widget Build(BuildContext context)
    {
        return new MenuItemButton(
            child: Child,
            onPressed: OnChanged is null ? null : HandleChanged,
            onHover: OnHover,
            onFocusChange: OnFocusChange,
            focusNode: FocusNode,
            style: Style,
            statesController: StatesController,
            clipBehavior: ClipBehavior,
            leadingIcon: new ExcludeFocus(
                new IgnorePointer(
                    child: new ConstrainedBox(
                        new BoxConstraints(MaxWidth: Checkbox.Width, MaxHeight: Checkbox.Width),
                        new Radio<T>(
                            value: Value,
                            groupValue: GroupValue,
                            onChanged: OnChanged,
                            toggleable: Toggleable)))),
            trailingIcon: TrailingIcon,
            closeOnActivate: CloseOnActivate,
            key: Key);
    }

    private void HandleChanged()
    {
        bool selected = EqualityComparer<T?>.Default.Equals(GroupValue, Value);
        OnChanged!(Toggleable && selected ? default : Value);
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
        MenuStyle style = (Style ?? new MenuStyle()).Merge(MenuBarTheme.Of(context).Style);
        MaterialState states = MaterialState.None;
        Color color = style.BackgroundColor?.Resolve(states) ?? theme.SurfaceContainerColor;
        Color shadowColor = style.ShadowColor?.Resolve(states) ?? theme.ShadowColor;
        Color surfaceTint = style.SurfaceTintColor?.Resolve(states) ?? Colors.Transparent;
        double elevation = style.Elevation?.Resolve(states) ?? 3;
        Thickness padding = style.Padding?.Resolve(states) ?? new Thickness(4, 0);
        VisualDensity density = style.VisualDensity ?? theme.VisualDensity;
        double horizontalDensityPadding = Math.Max(0.0, density.BaseSizeAdjustment.X);
        padding = new Thickness(
            padding.Left + horizontalDensityPadding,
            padding.Top,
            padding.Right + horizontalDensityPadding,
            padding.Bottom);
        ShapeBorder? shape = style.Shape?.Resolve(states) ?? ShapeBorder.RoundedRectangle(4.0);
        BorderSide? side = style.Side?.Resolve(states);
        if (side.HasValue)
        {
            shape = shape with { Side = side };
        }

        Widget content = new ConstrainedBox(
            MenuAnchorState.ResolveMenuConstraints(style, states, density),
            new Material(
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
                    textDirection: Directionality.Of(context)))));

        return new RawMenuAnchorGroup(
            new MenuAnchor(
                menuChildren: [],
                controller: Controller,
                clipBehavior: ClipBehavior,
                builder: (_, _, _) => content)
            {
                Orientation = Axis.Horizontal,
            });
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
        Action<AnimationStatus>? onAnimationStatusChanged = null,
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
    public Action<AnimationStatus>? OnAnimationStatusChanged { get; }
    public bool Enabled => MenuChildren.Count > 0;

    public override State CreateState() => new SubmenuButtonState();
}

public sealed class SubmenuButtonState : State
{
    private MenuController? _internalController;
    private MenuAnchorState? _parentHost;
    private CancellationTokenSource? _hoverOpenCancellation;
    private AnimationStatus _animationStatus = AnimationStatus.Dismissed;
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
        Axis parentOrientation = MenuController.MaybeScopeOf(Context)?.Orientation ?? Axis.Vertical;
        if (parentOrientation == Axis.Horizontal && Current.HoverOpenDelay > TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "A direct MenuBar SubmenuButton must use a zero hoverOpenDelay.");
        }
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
        CancelDelayedOpen();
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
            ?? new Icon(Icons.ChevronRight, size: 24.0);
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
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(64, 48)),
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
            onFocusChange: HandleFocusChange,
            focusNode: Current.FocusNode,
            clipBehavior: Current.ClipBehavior,
            enabled: Current.Enabled);

        Widget result = new MenuAnchor(
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
            onAnimationStatusChanged: HandleAnimationStatusChanged,
            child: new MergeSemantics(
                new Semantics(
                    expanded: Current.Enabled && _animationStatus.IsForwardOrCompleted(),
                    child: button)))
        {
            PanelOrientation = parentOrientation == Axis.Horizontal ? Axis.Vertical : Axis.Horizontal,
        };

        if (Current.Enabled && MenuAcceleratorLabel.PlatformSupportsAccelerators(context))
        {
            result = new MenuAcceleratorCallbackBinding(
                child: result,
                onInvoke: Toggle,
                hasSubmenu: true);
        }

        return result;
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
        CancelDelayedOpen();
        if (!hovered || !Current.Enabled) return;
        BeginOpen();
    }

    private void HandleFocusChange(bool focused)
    {
        Current.OnFocusChange?.Invoke(focused);
        CancelDelayedOpen();
        if (focused && Current.Enabled)
        {
            BeginOpen();
        }
    }

    private void BeginOpen()
    {
        if (Current.HoverOpenDelay == TimeSpan.Zero)
        {
            Controller.Open();
            return;
        }

        _hoverOpenCancellation = new CancellationTokenSource();
        _ = DelayOpenAsync(Current.HoverOpenDelay, _hoverOpenCancellation.Token);
    }

    private async Task DelayOpenAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            if (Mounted && Current.Enabled && !cancellationToken.IsCancellationRequested)
            {
                Controller.Open();
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void CancelDelayedOpen()
    {
        _hoverOpenCancellation?.Cancel();
        _hoverOpenCancellation?.Dispose();
        _hoverOpenCancellation = null;
    }

    private void HandleAnimationStatusChanged(AnimationStatus status)
    {
        if (_animationStatus != status && Mounted)
        {
            SetState(() => _animationStatus = status);
        }
        else
        {
            _animationStatus = status;
        }
        Current.OnAnimationStatusChanged?.Invoke(status);
    }
}

internal sealed class MenuOverlayLayoutDelegate : SingleChildLayoutDelegate
{
    public MenuOverlayLayoutDelegate(
        Rect anchorRect,
        Vector alignmentOffset,
        Thickness reservedPadding,
        Axis placementAxis,
        TextDirection textDirection,
        Thickness viewInsets,
        Vector? position,
        IReadOnlyList<DisplayFeature>? displayFeatures = null)
    {
        AnchorRect = anchorRect;
        AlignmentOffset = alignmentOffset;
        ReservedPadding = reservedPadding;
        PlacementAxis = placementAxis;
        TextDirection = textDirection;
        ViewInsets = viewInsets;
        Position = position;
        DisplayFeatures = displayFeatures ?? [];
    }

    public Rect AnchorRect { get; }

    public Vector AlignmentOffset { get; }

    public Thickness ReservedPadding { get; }

    public Axis PlacementAxis { get; }

    public TextDirection TextDirection { get; }

    public Thickness ViewInsets { get; }

    public Vector? Position { get; }

    public IReadOnlyList<DisplayFeature> DisplayFeatures { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints)
    {
        Rect available = ResolveAvailableRegion(constraints.Biggest);
        return BoxConstraints.Loose(available.Size);
    }

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        Rect available = ResolveAvailableRegion(size);
        double leftLimit = available.Left;
        double topLimit = available.Top;
        double rightLimit = available.Right;
        double bottomLimit = available.Bottom;
        double x;
        double y;
        if (Position.HasValue)
        {
            x = AnchorRect.Left + Position.Value.X;
            y = AnchorRect.Top + Position.Value.Y;
        }
        else if (PlacementAxis == Axis.Horizontal)
        {
            bool placeAfter = TextDirection == TextDirection.Ltr;
            double after = placeAfter ? AnchorRect.Right : AnchorRect.Left - childSize.Width;
            double before = placeAfter ? AnchorRect.Left - childSize.Width : AnchorRect.Right;
            x = FitsHorizontally(after, childSize.Width, leftLimit, rightLimit) ? after : before;
            y = AnchorRect.Top;
        }
        else
        {
            x = TextDirection == TextDirection.Ltr
                ? AnchorRect.Left
                : AnchorRect.Right - childSize.Width;
            double below = AnchorRect.Bottom;
            double above = AnchorRect.Top - childSize.Height;
            y = below + childSize.Height <= bottomLimit ? below : above;
        }

        if (!Position.HasValue)
        {
            x += AlignmentOffset.X;
            y += AlignmentOffset.Y;
        }
        x = Math.Clamp(x, leftLimit, Math.Max(leftLimit, rightLimit - childSize.Width));
        y = Math.Clamp(y, topLimit, Math.Max(topLimit, bottomLimit - childSize.Height));
        return new Point(x, y);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        if (oldDelegate is not MenuOverlayLayoutDelegate oldLayout)
        {
            return true;
        }

        return AnchorRect != oldLayout.AnchorRect
               || AlignmentOffset != oldLayout.AlignmentOffset
               || ReservedPadding != oldLayout.ReservedPadding
               || PlacementAxis != oldLayout.PlacementAxis
               || TextDirection != oldLayout.TextDirection
               || ViewInsets != oldLayout.ViewInsets
               || Position != oldLayout.Position
               || !DisplayFeatures.SequenceEqual(oldLayout.DisplayFeatures);
    }

    private static bool FitsHorizontally(
        double x,
        double width,
        double leftLimit,
        double rightLimit)
    {
        return x >= leftLimit && x + width <= rightLimit;
    }

    private Rect ResolveAvailableRegion(Size size)
    {
        var regions = new List<Rect>
        {
            new(
                ReservedPadding.Left,
                ReservedPadding.Top,
                Math.Max(0.0, size.Width - ReservedPadding.Left - ReservedPadding.Right),
                Math.Max(
                    0.0,
                    size.Height - ReservedPadding.Top - ReservedPadding.Bottom - ViewInsets.Bottom)),
        };
        foreach (DisplayFeature feature in DisplayFeatures)
        {
            var next = new List<Rect>();
            foreach (Rect region in regions)
            {
                Rect bounds = feature.Bounds.Intersect(region);
                if (bounds.Width <= 0.0 || bounds.Height <= 0.0)
                {
                    next.Add(region);
                    continue;
                }

                if (bounds.Height >= region.Height && bounds.Width < region.Width)
                {
                    next.Add(new Rect(region.Left, region.Top, bounds.Left - region.Left, region.Height));
                    next.Add(new Rect(bounds.Right, region.Top, region.Right - bounds.Right, region.Height));
                }
                else if (bounds.Width >= region.Width && bounds.Height < region.Height)
                {
                    next.Add(new Rect(region.Left, region.Top, region.Width, bounds.Top - region.Top));
                    next.Add(new Rect(region.Left, bounds.Bottom, region.Width, region.Bottom - bounds.Bottom));
                }
                else
                {
                    next.Add(region);
                }
            }
            regions = next.Where(region => region.Width > 0.0 && region.Height > 0.0).ToList();
        }

        Point anchorCenter = AnchorRect.Center;
        return regions.MinBy(region =>
        {
            double dx = region.Center.X - anchorCenter.X;
            double dy = region.Center.Y - anchorCenter.Y;
            return (dx * dx) + (dy * dy);
        });
    }
}
