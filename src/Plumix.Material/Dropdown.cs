using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dropdown.dart

internal static class DropdownConstants
{
    public static readonly TimeSpan MenuDuration = TimeSpan.FromMilliseconds(300);
    public const double MenuItemHeight = 48.0;
    public const double DenseButtonHeight = 24.0;
    public static readonly EdgeInsetsGeometry MenuItemPadding = EdgeInsetsGeometry.Symmetric(horizontal: 16.0);
    public static readonly EdgeInsetsGeometry AlignedButtonPadding =
        EdgeInsetsGeometry.DirectionalOnly(start: 16.0, end: 4.0);
    public static readonly EdgeInsetsGeometry UnalignedButtonPadding = EdgeInsetsGeometry.Zero;
    public static readonly EdgeInsetsGeometry AlignedMenuMargin = EdgeInsetsGeometry.Zero;
    public static readonly EdgeInsetsGeometry UnalignedMenuMargin =
        EdgeInsetsGeometry.DirectionalOnly(start: 16.0, end: 24.0);
}

public delegate IReadOnlyList<Widget> DropdownButtonBuilder(BuildContext context);

/// <summary>Dart's `_DropdownMenuPainter`: paints the menu background over the resize animation.</summary>
internal sealed class DropdownMenuPainter : CustomPainter
{
    private readonly BoxPainter _painter;

    public DropdownMenuPainter(
        Color? color,
        int? elevation,
        int? selectedIndex,
        BorderRadius? borderRadius,
        Animation<double> resize,
        Func<double> getSelectedItemOffset) : base(resize)
    {
        Color = color;
        Elevation = elevation;
        SelectedIndex = selectedIndex;
        BorderRadius = borderRadius;
        Resize = resize;
        GetSelectedItemOffset = getSelectedItemOffset;
        _painter = new BoxDecoration(
            Color: color,
            BorderRadius: borderRadius ?? Rendering.BorderRadius.Circular(2.0),
            BoxShadows: elevation.HasValue ? MaterialShadows.ForElevation(elevation.Value) : null)
            .CreateBoxPainter();
    }

    public Color? Color { get; }
    public int? Elevation { get; }
    public int? SelectedIndex { get; }
    public BorderRadius? BorderRadius { get; }
    public Animation<double> Resize { get; }
    public Func<double> GetSelectedItemOffset { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        Rect rect = ResolveRect(GetSelectedItemOffset(), size, Resize.Value);
        _painter.Paint(context, rect.TopLeft, new ImageConfiguration(Size: rect.Size));
    }

    /// <summary>
    /// The animated background rectangle: it starts around the selected row and grows to the full
    /// menu over the resize animation.
    /// </summary>
    internal static Rect ResolveRect(double selectedItemOffset, Size size, double resize)
    {
        var top = new DoubleTween(
            begin: Math.Clamp(selectedItemOffset, 0.0, Math.Max(size.Height - DropdownConstants.MenuItemHeight, 0.0)),
            end: 0.0);
        var bottom = new DoubleTween(
            begin: Math.Clamp(
                top.Begin!.Value + DropdownConstants.MenuItemHeight,
                Math.Min(DropdownConstants.MenuItemHeight, size.Height),
                size.Height),
            end: size.Height);
        double clamped = Math.Clamp(resize, 0.0, 1.0);
        double topValue = top.Transform(clamped);
        double bottomValue = bottom.Transform(clamped);
        return new Rect(0.0, topValue, size.Width, Math.Max(0.0, bottomValue - topValue));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        var old = (DropdownMenuPainter)oldDelegate;
        return old.Color != Color
               || old.Elevation != Elevation
               || old.SelectedIndex != SelectedIndex
               || old.BorderRadius != BorderRadius
               || !ReferenceEquals(old.Resize, Resize);
    }

    public override void Dispose()
    {
        _painter.Dispose();
        base.Dispose();
    }
}

/// <summary>Dart's `_DropdownMenuItemButton`: one staggered, focusable row of the open menu.</summary>
internal sealed class DropdownMenuItemButton<T> : StatefulWidget
{
    public DropdownMenuItemButton(
        DropdownRoute<T> route,
        Thickness? padding,
        Rect buttonRect,
        BoxConstraints constraints,
        int itemIndex,
        bool enableFeedback,
        ScrollController scrollController,
        MouseCursor? mouseCursor,
        Key? key = null) : base(key)
    {
        Route = route;
        Padding = padding;
        ButtonRect = buttonRect;
        Constraints = constraints;
        ItemIndex = itemIndex;
        EnableFeedback = enableFeedback;
        ScrollController = scrollController;
        MouseCursor = mouseCursor;
    }

    public DropdownRoute<T> Route { get; }
    public Thickness? Padding { get; }
    public Rect ButtonRect { get; }
    public BoxConstraints Constraints { get; }
    public int ItemIndex { get; }
    public bool EnableFeedback { get; }
    public ScrollController ScrollController { get; }
    public MouseCursor? MouseCursor { get; }

    public override State CreateState() => new DropdownMenuItemButtonState<T>();
}

internal sealed class DropdownMenuItemButtonState<T> : State
{
    private CurvedAnimation? _opacityAnimation;

    private DropdownMenuItemButton<T> CurrentWidget => (DropdownMenuItemButton<T>)StateWidget;

    public override void InitState()
    {
        base.InitState();
        SetOpacityAnimation();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (DropdownMenuItemButton<T>)oldWidget;
        if (old.ItemIndex != CurrentWidget.ItemIndex
            || !ReferenceEquals(old.Route.Animation, CurrentWidget.Route.Animation)
            || old.Route.SelectedIndex != CurrentWidget.Route.SelectedIndex
            || old.Route.Items.Count != CurrentWidget.Route.Items.Count)
        {
            SetOpacityAnimation();
        }
    }

    public override void Dispose()
    {
        _opacityAnimation?.Dispose();
        _opacityAnimation = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        DropdownRoute<T> route = widget.Route;
        DropdownMenuItem<T> dropdownMenuItem = route.Items[widget.ItemIndex].Item!;
        // The `MenuItem` wrapper stays in the tree: it is what reports each measured row height back
        // to the route, which the menu geometry then reads.
        Widget child = route.Items[widget.ItemIndex];
        if (widget.Padding.HasValue)
        {
            child = new Padding(widget.Padding.Value, child);
        }

        child = new SizedBox(height: route.ItemHeight, child: child);
        if (dropdownMenuItem.Enabled)
        {
            bool isSelected = widget.ItemIndex == route.SelectedIndex;
            child = new InkWell(
                autofocus: isSelected,
                enableFeedback: widget.EnableFeedback,
                onTap: HandleOnTap,
                onFocusChange: HandleFocusChange,
                mouseCursor: widget.MouseCursor,
                child: FocusManager.Instance.HighlightMode == FocusHighlightMode.Touch
                    ? new Ink(color: isSelected ? Theme.Of(context).FocusColor : null, child: child)
                    : child);
        }

        child = new FadeTransition(_opacityAnimation!, child);
        if (OperatingSystem.IsBrowser() && dropdownMenuItem.Enabled)
        {
            child = new Shortcuts(WebShortcuts, child);
        }

        return new Semantics(role: SemanticsRole.MenuItem, child: child);
    }

    private static IReadOnlyDictionary<ShortcutActivator, Intent> WebShortcuts { get; } =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] =
                new DirectionalFocusIntent(TraversalDirection.Down),
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] =
                new DirectionalFocusIntent(TraversalDirection.Up),
        };

    private void SetOpacityAnimation()
    {
        _opacityAnimation?.Dispose();
        var widget = CurrentWidget;
        DropdownRoute<T> route = widget.Route;
        double unit = 0.5 / (route.Items.Count + 1.5);
        if (widget.ItemIndex == route.SelectedIndex)
        {
            _opacityAnimation = new CurvedAnimation(route.Animation, Curves.Threshold(0.0));
            return;
        }

        double start = Math.Clamp(0.5 + ((widget.ItemIndex + 1) * unit), 0.0, 1.0);
        double end = Math.Clamp(start + (1.5 * unit), 0.0, 1.0);
        _opacityAnimation = new CurvedAnimation(route.Animation, Curves.Interval(start, end));
    }

    private void HandleFocusChange(bool focused)
    {
        bool inTraditionalMode = FocusManager.Instance.HighlightMode switch
        {
            FocusHighlightMode.Touch => false,
            _ => true,
        };
        if (!focused || !inTraditionalMode)
        {
            return;
        }

        var widget = CurrentWidget;
        DropdownMenuLimits menuLimits = widget.Route.GetMenuLimits(
            widget.ButtonRect,
            widget.Constraints.MaxHeight,
            widget.ItemIndex);
        widget.ScrollController.AnimateTo(
            menuLimits.ScrollOffset,
            duration: TimeSpan.FromMilliseconds(100),
            curve: Curves.EaseInOut);
    }

    private void HandleOnTap()
    {
        var widget = CurrentWidget;
        DropdownMenuItem<T> dropdownMenuItem = widget.Route.Items[widget.ItemIndex].Item!;
        dropdownMenuItem.OnTap?.Invoke();
        Navigator.Of(Context).Pop(new DropdownRouteResult<T>(dropdownMenuItem.Value));
    }
}

/// <summary>Dart's `_DropdownMenu`: the animated menu surface hosting the item buttons.</summary>
internal sealed class DropdownMenuPanel<T> : StatefulWidget
{
    public DropdownMenuPanel(
        DropdownRoute<T> route,
        Thickness? padding,
        Rect buttonRect,
        BoxConstraints constraints,
        Color? dropdownColor,
        bool enableFeedback,
        BorderRadius? borderRadius,
        ScrollController scrollController,
        MouseCursor? mouseCursor,
        Key? key = null) : base(key)
    {
        Route = route;
        Padding = padding;
        ButtonRect = buttonRect;
        Constraints = constraints;
        DropdownColor = dropdownColor;
        EnableFeedback = enableFeedback;
        BorderRadius = borderRadius;
        ScrollController = scrollController;
        MouseCursor = mouseCursor;
    }

    public DropdownRoute<T> Route { get; }
    public Thickness? Padding { get; }
    public Rect ButtonRect { get; }
    public BoxConstraints Constraints { get; }
    public Color? DropdownColor { get; }
    public bool EnableFeedback { get; }
    public BorderRadius? BorderRadius { get; }
    public ScrollController ScrollController { get; }
    public MouseCursor? MouseCursor { get; }

    public override State CreateState() => new DropdownMenuPanelState<T>();
}

internal sealed class DropdownMenuPanelState<T> : State
{
    private CurvedAnimation? _fadeOpacity;
    private CurvedAnimation? _resize;

    private DropdownMenuPanel<T> CurrentWidget => (DropdownMenuPanel<T>)StateWidget;

    public override void InitState()
    {
        base.InitState();
        // The `_fadeOpacity`/`_resize` animations are created here, not in `build`, so that their
        // curve direction survives a reversal (Dart keeps them in state for the same reason).
        Animation<double> animation = CurrentWidget.Route.Animation;
        _fadeOpacity = new CurvedAnimation(animation, Curves.Interval(0.0, 0.25), Curves.Interval(0.75, 1.0));
        _resize = new CurvedAnimation(animation, Curves.Interval(0.25, 0.5), Curves.Threshold(0.0));
    }

    public override void Dispose()
    {
        _fadeOpacity?.Dispose();
        _resize?.Dispose();
        _fadeOpacity = null;
        _resize = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        DropdownRoute<T> route = widget.Route;
        var children = new List<Widget>(route.Items.Count);
        for (int itemIndex = 0; itemIndex < route.Items.Count; itemIndex++)
        {
            children.Add(new DropdownMenuItemButton<T>(
                route: route,
                padding: widget.Padding,
                buttonRect: widget.ButtonRect,
                constraints: widget.Constraints,
                itemIndex: itemIndex,
                enableFeedback: widget.EnableFeedback,
                scrollController: widget.ScrollController,
                mouseCursor: widget.MouseCursor));
        }

        var theme = Theme.Of(context);
        Widget content = new ListView(
            children: children,
            primary: true,
            padding: MaterialConstants.MaterialListPadding.Resolve(TextDirection.Ltr),
            shrinkWrap: true);
        content = new Scrollbar(child: content, thumbVisibility: true);
        content = new PrimaryScrollController(widget.ScrollController, content);
        content = new ScrollConfiguration(
            ScrollConfiguration.Of(context).CopyWith(
                scrollbars: false,
                overscroll: false,
                physics: new ClampingScrollPhysics(),
                platform: theme.Platform),
            content);
        content = new Material(
            type: MaterialType.Transparency,
            textStyle: route.Style,
            child: content);
        // Dart clips with `Clip.antiAlias` only when a border radius is set and with `Clip.none`
        // otherwise, so the unrounded menu is left unclipped rather than clipped to its own bounds.
        if (widget.BorderRadius is not null)
        {
            content = new ClipRRect(widget.BorderRadius.Value, child: content);
        }
        content = new Semantics(
            role: SemanticsRole.Menu,
            scopesRoute: true,
            namesRoute: true,
            explicitChildNodes: true,
            label: MaterialLocalizations.Of(context).PopupMenuLabel,
            child: content);
        content = new CustomPaint(
            painter: new DropdownMenuPainter(
                color: widget.DropdownColor ?? theme.CanvasColor,
                elevation: route.Elevation,
                selectedIndex: route.SelectedIndex,
                borderRadius: widget.BorderRadius,
                resize: _resize!,
                getSelectedItemOffset: () => route.GetItemOffset(route.SelectedIndex)),
            child: content);
        return new FadeTransition(_fadeOpacity!, content);
    }
}

/// <summary>Dart's `_DropdownMenuRouteLayout`: sizes and positions the menu against the button.</summary>
internal sealed class DropdownMenuRouteLayout<T> : SingleChildLayoutDelegate
{
    public DropdownMenuRouteLayout(
        Rect buttonRect,
        DropdownRoute<T> route,
        TextDirection? textDirection,
        double? menuWidth)
    {
        ButtonRect = buttonRect;
        Route = route;
        TextDirection = textDirection;
        MenuWidth = menuWidth;
    }

    public Rect ButtonRect { get; }
    public DropdownRoute<T> Route { get; }
    public TextDirection? TextDirection { get; }
    public double? MenuWidth { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints)
    {
        // The maximum height of a simple menu should be one or more rows less than the view height.
        // This ensures a tappable area outside of the simple menu with which to dismiss the menu.
        double maxHeight = Math.Max(0.0, constraints.MaxHeight - (2 * DropdownConstants.MenuItemHeight));
        if (Route.MenuMaxHeight.HasValue && Route.MenuMaxHeight.Value <= maxHeight)
        {
            maxHeight = Route.MenuMaxHeight.Value;
        }

        // The width of a menu should be at most the view width. This ensures that
        // the menu does not extend past the left and right edges of the screen.
        double width = Math.Min(constraints.MaxWidth, MenuWidth ?? ButtonRect.Width);
        return new BoxConstraints(MinWidth: width, MaxWidth: width, MaxHeight: maxHeight);
    }

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        DropdownMenuLimits menuLimits = Route.GetMenuLimits(ButtonRect, size.Height, Route.SelectedIndex);
        double left = TextDirection == UI.TextDirection.Rtl
            ? Math.Clamp(ButtonRect.Right, 0.0, size.Width) - childSize.Width
            : Math.Clamp(ButtonRect.Left, 0.0, Math.Max(0.0, size.Width - childSize.Width));
        return new Point(left, menuLimits.Top);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        var old = (DropdownMenuRouteLayout<T>)oldDelegate;
        return ButtonRect != old.ButtonRect || TextDirection != old.TextDirection;
    }
}

internal sealed record DropdownRouteResult<T>(T? Result);

internal sealed record DropdownMenuLimits(double Top, double Bottom, double Height, double ScrollOffset);

internal sealed class DropdownRoute<T> : PopupRoute
{
    public DropdownRoute(
        IReadOnlyList<MenuItem<T>> items,
        EdgeInsetsGeometry padding,
        Rect buttonRect,
        int selectedIndex,
        CapturedThemes capturedThemes,
        TextStyle style,
        int elevation = 8,
        string? barrierLabel = null,
        double? itemHeight = null,
        double? menuWidth = null,
        Color? dropdownColor = null,
        double? menuMaxHeight = null,
        bool enableFeedback = false,
        BorderRadius? borderRadius = null,
        bool barrierDismissible = true,
        MouseCursor? dropdownMenuItemMouseCursor = null)
    {
        Items = items;
        Padding = padding;
        ButtonRect = buttonRect;
        SelectedIndex = selectedIndex;
        CapturedThemes = capturedThemes;
        Style = style;
        Elevation = elevation;
        BarrierLabel = barrierLabel;
        ItemHeight = itemHeight;
        MenuWidth = menuWidth;
        DropdownColor = dropdownColor;
        MenuMaxHeight = menuMaxHeight;
        EnableFeedback = enableFeedback;
        BorderRadius = borderRadius;
        BarrierDismissible = barrierDismissible;
        DropdownMenuItemMouseCursor = dropdownMenuItemMouseCursor;
        ItemHeights = [.. Enumerable.Repeat(itemHeight ?? DropdownConstants.MenuItemHeight, items.Count)];
    }

    public IReadOnlyList<MenuItem<T>> Items { get; }
    public EdgeInsetsGeometry Padding { get; }
    public Rect ButtonRect { get; }
    public int SelectedIndex { get; }
    public CapturedThemes CapturedThemes { get; }
    public TextStyle Style { get; }
    public int Elevation { get; }
    public double? ItemHeight { get; }
    public double? MenuWidth { get; }
    public Color? DropdownColor { get; }
    public double? MenuMaxHeight { get; }
    public bool EnableFeedback { get; }
    public BorderRadius? BorderRadius { get; }
    public MouseCursor? DropdownMenuItemMouseCursor { get; }
    public double[] ItemHeights { get; }

    public override TimeSpan TransitionDuration => DropdownConstants.MenuDuration;
    public override bool BarrierDismissible { get; }
    public override Color? BarrierColor => null;
    public override string? BarrierLabel { get; }

    public event Action<DropdownRoute<T>, DropdownRouteResult<T>?>? RouteCompleted;

    public override Widget BuildPage(BuildContext context) => new LayoutBuilder(
        (_, constraints) => new DropdownRoutePage<T>(
            route: this,
            constraints: constraints,
            buttonRect: ButtonRect,
            padding: Padding,
            selectedIndex: SelectedIndex,
            elevation: Elevation,
            capturedThemes: CapturedThemes,
            style: Style,
            dropdownColor: DropdownColor,
            enableFeedback: EnableFeedback,
            borderRadius: BorderRadius,
            menuWidth: MenuWidth,
            mouseCursor: DropdownMenuItemMouseCursor));

    public override void DidComplete(object? result)
    {
        base.DidComplete(result);
        Scheduler.ScheduleMicrotask(() => RouteCompleted?.Invoke(this, result as DropdownRouteResult<T>));
    }

    /// <summary>Dart's `_DropdownRoute._dismiss`: drops the route without waiting for a pop.</summary>
    public void Dismiss()
    {
        // The navigator is checked for `Mounted` as well: the button's `dispose` runs while the tree
        // is being torn down, and removing a route from a defunct navigator corrupts its history.
        if (IsActive && Navigator is { Mounted: true } navigator)
        {
            navigator.RemoveRoute(this);
        }
    }

    public double GetItemOffset(int index)
    {
        double offset = MaterialConstants.MaterialListPadding.Top;
        if (Items.Count > 0 && index > 0)
        {
            for (int i = 0; i < index && i < ItemHeights.Length; i++)
            {
                offset += ItemHeights[i];
            }
        }

        return offset;
    }

    /// <summary>
    /// Dart's `_DropdownRoute.getMenuLimits`: where the menu should be placed so the selected item
    /// lines up with the button, clamped into the available height.
    /// </summary>
    public DropdownMenuLimits GetMenuLimits(Rect buttonRect, double availableHeight, int index)
    {
        double computedMaxHeight = availableHeight - (2.0 * DropdownConstants.MenuItemHeight);
        if (MenuMaxHeight.HasValue)
        {
            computedMaxHeight = Math.Min(computedMaxHeight, MenuMaxHeight.Value);
        }

        double buttonTop = buttonRect.Top;
        double buttonBottom = Math.Min(buttonRect.Bottom, availableHeight);
        double selectedItemOffset = GetItemOffset(index);

        // If the button is placed on the bottom or top of the screen, its top or bottom may be
        // outside the bounds. Clamp the menu against a full item height from either edge.
        double topLimit = Math.Min(DropdownConstants.MenuItemHeight, buttonTop);
        double bottomLimit = Math.Max(availableHeight - DropdownConstants.MenuItemHeight, buttonBottom);

        double selectedItemHeight = SelectedItemHeight();
        double menuTop = buttonTop - selectedItemOffset - ((selectedItemHeight - buttonRect.Height) / 2.0);
        double preferredMenuHeight = MaterialConstants.MaterialListPadding.Vertical;
        if (Items.Count > 0)
        {
            for (int i = 0; i < ItemHeights.Length; i++)
            {
                preferredMenuHeight += ItemHeights[i];
            }
        }

        // If there are too many items to fit on the screen, then the menu is sized to fit.
        double menuHeight = Math.Min(computedMaxHeight, preferredMenuHeight);
        double menuBottom = menuTop + menuHeight;

        // If the computed top or bottom of the menu are outside of the range specified, we need to
        // bring them into range.
        if (menuTop < topLimit)
        {
            menuTop = Math.Min(buttonTop, topLimit);
            menuBottom = menuTop + menuHeight;
        }

        if (menuBottom > bottomLimit)
        {
            menuBottom = Math.Max(buttonBottom, bottomLimit);
            menuTop = menuBottom - menuHeight;
        }

        if (menuBottom - (selectedItemHeight / 2.0) < buttonBottom - (buttonRect.Height / 2.0))
        {
            menuBottom = buttonBottom - (buttonRect.Height / 2.0) + (selectedItemHeight / 2.0);
            menuTop = menuBottom - menuHeight;
        }

        double scrollOffset = 0.0;
        // If all of the menu items will not fit within availableHeight then compute the scroll offset
        // that will line the selected menu item up with the select item.
        if (preferredMenuHeight > computedMaxHeight)
        {
            scrollOffset = Math.Max(0.0, selectedItemOffset - (buttonTop - menuTop));
            scrollOffset = Math.Min(scrollOffset, preferredMenuHeight - menuHeight);
        }

        return new DropdownMenuLimits(menuTop, menuBottom, menuHeight, scrollOffset);
    }

    private double SelectedItemHeight()
    {
        if (ItemHeights.Length == 0)
        {
            return ItemHeight ?? DropdownConstants.MenuItemHeight;
        }

        return ItemHeights[Math.Clamp(SelectedIndex, 0, ItemHeights.Length - 1)];
    }
}

internal sealed class DropdownRoutePage<T> : StatefulWidget
{
    public DropdownRoutePage(
        DropdownRoute<T> route,
        BoxConstraints constraints,
        Rect buttonRect,
        EdgeInsetsGeometry padding,
        int selectedIndex,
        CapturedThemes capturedThemes,
        TextStyle style,
        Color? dropdownColor,
        bool enableFeedback,
        BorderRadius? borderRadius,
        double? menuWidth,
        MouseCursor? mouseCursor,
        int elevation = 8,
        Key? key = null) : base(key)
    {
        Route = route;
        Constraints = constraints;
        ButtonRect = buttonRect;
        Padding = padding;
        SelectedIndex = selectedIndex;
        CapturedThemes = capturedThemes;
        Style = style;
        DropdownColor = dropdownColor;
        EnableFeedback = enableFeedback;
        BorderRadius = borderRadius;
        MenuWidth = menuWidth;
        MouseCursor = mouseCursor;
        Elevation = elevation;
    }

    public DropdownRoute<T> Route { get; }
    public BoxConstraints Constraints { get; }
    public Rect ButtonRect { get; }
    public EdgeInsetsGeometry Padding { get; }
    public int SelectedIndex { get; }
    public CapturedThemes CapturedThemes { get; }
    public TextStyle Style { get; }
    public Color? DropdownColor { get; }
    public bool EnableFeedback { get; }
    public BorderRadius? BorderRadius { get; }
    public double? MenuWidth { get; }
    public MouseCursor? MouseCursor { get; }
    public int Elevation { get; }

    public override State CreateState() => new DropdownRoutePageState<T>();
}

internal sealed class DropdownRoutePageState<T> : State
{
    private ScrollController? _scrollController;

    private DropdownRoutePage<T> CurrentWidget => (DropdownRoutePage<T>)StateWidget;

    public override void InitState()
    {
        base.InitState();
        var widget = CurrentWidget;
        // Computing the initial scroll position lines the selected item up with the button, but it
        // can only be exact while the item heights are still the assumed ones.
        DropdownMenuLimits menuLimits = widget.Route.GetMenuLimits(
            widget.ButtonRect,
            widget.Constraints.MaxHeight,
            widget.SelectedIndex);
        _scrollController = new ScrollController(initialScrollOffset: menuLimits.ScrollOffset);
    }

    public override void Dispose()
    {
        _scrollController?.Dispose();
        _scrollController = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        TextDirection? textDirection = Directionality.MaybeOf(context);
        Widget menu = new DropdownMenuPanel<T>(
            route: widget.Route,
            padding: widget.Padding.Resolve(textDirection ?? TextDirection.Ltr),
            buttonRect: widget.ButtonRect,
            constraints: widget.Constraints,
            dropdownColor: widget.DropdownColor,
            enableFeedback: widget.EnableFeedback,
            borderRadius: widget.BorderRadius,
            scrollController: _scrollController!,
            mouseCursor: widget.MouseCursor);

        return MediaQuery.RemovePadding(
            context,
            new Builder(innerContext => new CustomSingleChildLayout(
                new DropdownMenuRouteLayout<T>(
                    buttonRect: widget.ButtonRect,
                    route: widget.Route,
                    textDirection: textDirection,
                    menuWidth: widget.MenuWidth),
                widget.CapturedThemes.Wrap(menu))),
            removeTop: true,
            removeBottom: true,
            removeLeft: true,
            removeRight: true);
    }
}

/// <summary>Dart's `_MenuItem`: reports the laid-out height of one menu row back to the route.</summary>
internal sealed class MenuItem<T> : SingleChildRenderObjectWidget
{
    public MenuItem(Action<Size> onLayout, DropdownMenuItem<T>? item, Key? key = null) : base(item, key)
    {
        OnLayout = onLayout;
        Item = item;
    }

    public Action<Size> OnLayout { get; }
    public DropdownMenuItem<T>? Item { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderMenuItem(OnLayout);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject) =>
        ((RenderMenuItem)renderObject).OnLayout = OnLayout;
}

internal sealed class RenderMenuItem : RenderProxyBox
{
    public RenderMenuItem(Action<Size> onLayout)
    {
        OnLayout = onLayout;
    }

    public Action<Size> OnLayout { get; set; }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        OnLayout(Size);
    }
}

/// <summary>Dart's `_DropdownMenuItemContainer`: the shared layout of a dropdown row.</summary>
public abstract class DropdownMenuItemContainer : StatelessWidget
{
    protected DropdownMenuItemContainer(
        Widget child,
        AlignmentGeometry? alignment = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Alignment = alignment ?? (AlignmentGeometry)AlignmentDirectional.CenterStart;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>Defines how the item is positioned within its container.</summary>
    public AlignmentGeometry Alignment { get; }

    public override Widget Build(BuildContext context) => new Semantics(
        flags: SemanticsFlags.IsButton,
        child: new ConstrainedBox(
            new BoxConstraints(MinHeight: DropdownConstants.MenuItemHeight),
            new Align(alignment: Alignment, child: Child)));
}

public sealed class DropdownMenuItem<T> : DropdownMenuItemContainer
{
    public DropdownMenuItem(
        Widget child,
        T? value = default,
        Action? onTap = null,
        bool enabled = true,
        AlignmentGeometry? alignment = null,
        Key? key = null) : base(child, alignment, key)
    {
        Value = value;
        OnTap = onTap;
        Enabled = enabled;
    }

    /// <summary>Called when the dropdown menu item is tapped.</summary>
    public Action? OnTap { get; }

    /// <summary>The value to return if the user selects this menu item.</summary>
    public T? Value { get; }

    /// <summary>Whether or not a user can select this menu item.</summary>
    public bool Enabled { get; }
}

public sealed class DropdownButtonHideUnderline : InheritedWidget
{
    public DropdownButtonHideUnderline(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;

    public static bool At(BuildContext context) =>
        context.DependOnInherited<DropdownButtonHideUnderline>() is not null;
}

public sealed class DropdownButton<T> : StatefulWidget
{
    public DropdownButton(
        IReadOnlyList<DropdownMenuItem<T>>? items,
        Action<T?>? onChanged,
        DropdownButtonBuilder? selectedItemBuilder = null,
        T? value = default,
        Widget? hint = null,
        Widget? disabledHint = null,
        Action? onTap = null,
        int elevation = 8,
        TextStyle? style = null,
        Widget? underline = null,
        Widget? icon = null,
        Color? iconDisabledColor = null,
        Color? iconEnabledColor = null,
        double iconSize = 24.0,
        bool isDense = false,
        bool isExpanded = false,
        double? itemHeight = DropdownConstants.MenuItemHeight,
        double? menuWidth = null,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Color? dropdownColor = null,
        double? menuMaxHeight = null,
        bool? enableFeedback = null,
        AlignmentGeometry? alignment = null,
        BorderRadius? borderRadius = null,
        EdgeInsetsGeometry? padding = null,
        bool barrierDismissible = true,
        MouseCursor? mouseCursor = null,
        MouseCursor? dropdownMenuItemMouseCursor = null,
        Key? key = null) : this(
            items: items,
            onChanged: onChanged,
            selectedItemBuilder: selectedItemBuilder,
            value: value,
            hint: hint,
            disabledHint: disabledHint,
            onTap: onTap,
            elevation: elevation,
            style: style,
            underline: underline,
            icon: icon,
            iconDisabledColor: iconDisabledColor,
            iconEnabledColor: iconEnabledColor,
            iconSize: iconSize,
            isDense: isDense,
            isExpanded: isExpanded,
            itemHeight: itemHeight,
            menuWidth: menuWidth,
            focusColor: focusColor,
            focusNode: focusNode,
            autofocus: autofocus,
            dropdownColor: dropdownColor,
            menuMaxHeight: menuMaxHeight,
            enableFeedback: enableFeedback,
            alignment: alignment,
            borderRadius: borderRadius,
            padding: padding,
            barrierDismissible: barrierDismissible,
            mouseCursor: mouseCursor,
            dropdownMenuItemMouseCursor: dropdownMenuItemMouseCursor,
            inputDecoration: null,
            isEmpty: false,
            valueLabel: "DropdownButton",
            key: key)
    {
    }

    /// <summary>Dart's `DropdownButton._formField`: the shape used inside a form field decorator.</summary>
    internal DropdownButton(
        IReadOnlyList<DropdownMenuItem<T>>? items,
        Action<T?>? onChanged,
        DropdownButtonBuilder? selectedItemBuilder,
        T? value,
        Widget? hint,
        Widget? disabledHint,
        Action? onTap,
        int elevation,
        TextStyle? style,
        Widget? underline,
        Widget? icon,
        Color? iconDisabledColor,
        Color? iconEnabledColor,
        double iconSize,
        bool isDense,
        bool isExpanded,
        double? itemHeight,
        double? menuWidth,
        Color? focusColor,
        FocusNode? focusNode,
        bool autofocus,
        Color? dropdownColor,
        double? menuMaxHeight,
        bool? enableFeedback,
        AlignmentGeometry? alignment,
        BorderRadius? borderRadius,
        EdgeInsetsGeometry? padding,
        bool barrierDismissible,
        MouseCursor? mouseCursor,
        MouseCursor? dropdownMenuItemMouseCursor,
        InputDecoration? inputDecoration,
        bool isEmpty,
        string valueLabel,
        Key? key) : base(key)
    {
        ValidateSelection(items, value, valueLabel);
        if (itemHeight.HasValue && itemHeight.Value < DropdownConstants.MenuItemHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(itemHeight),
                $"itemHeight must be greater than or equal to {DropdownConstants.MenuItemHeight}.");
        }

        Items = items;
        OnChanged = onChanged;
        SelectedItemBuilder = selectedItemBuilder;
        Value = value;
        Hint = hint;
        DisabledHint = disabledHint;
        OnTap = onTap;
        Elevation = elevation;
        Style = style;
        Underline = underline;
        Icon = icon;
        IconDisabledColor = iconDisabledColor;
        IconEnabledColor = iconEnabledColor;
        IconSize = iconSize;
        IsDense = isDense;
        IsExpanded = isExpanded;
        ItemHeight = itemHeight;
        MenuWidth = menuWidth;
        FocusColor = focusColor;
        FocusNode = focusNode;
        Autofocus = autofocus;
        DropdownColor = dropdownColor;
        MenuMaxHeight = menuMaxHeight;
        EnableFeedback = enableFeedback;
        Alignment = alignment ?? (AlignmentGeometry)AlignmentDirectional.CenterStart;
        BorderRadius = borderRadius;
        Padding = padding;
        BarrierDismissible = barrierDismissible;
        MouseCursor = mouseCursor;
        DropdownMenuItemMouseCursor = dropdownMenuItemMouseCursor;
        InputDecoration = inputDecoration;
        IsEmpty = isEmpty;
    }

    public IReadOnlyList<DropdownMenuItem<T>>? Items { get; }
    public DropdownButtonBuilder? SelectedItemBuilder { get; }
    public T? Value { get; }
    public Widget? Hint { get; }
    public Widget? DisabledHint { get; }
    public Action<T?>? OnChanged { get; }
    public Action? OnTap { get; }
    public int Elevation { get; }
    public TextStyle? Style { get; }
    public Widget? Underline { get; }
    public Widget? Icon { get; }
    public Color? IconDisabledColor { get; }
    public Color? IconEnabledColor { get; }
    public double IconSize { get; }
    public bool IsDense { get; }
    public bool IsExpanded { get; }
    public double? ItemHeight { get; }
    public double? MenuWidth { get; }
    public Color? FocusColor { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public Color? DropdownColor { get; }
    public double? MenuMaxHeight { get; }
    public bool? EnableFeedback { get; }
    public AlignmentGeometry Alignment { get; }
    public BorderRadius? BorderRadius { get; }
    public EdgeInsetsGeometry? Padding { get; }
    public bool BarrierDismissible { get; }
    public MouseCursor? MouseCursor { get; }
    public MouseCursor? DropdownMenuItemMouseCursor { get; }

    internal InputDecoration? InputDecoration { get; }
    internal bool IsEmpty { get; }

    public override State CreateState() => new DropdownButtonState<T>();

    internal static void ValidateSelection(
        IReadOnlyList<DropdownMenuItem<T>>? items,
        T? value,
        string valueLabel)
    {
        if (items is null || items.Count == 0 || value is null)
        {
            return;
        }

        int matching = items.Count(item => EqualityComparer<T?>.Default.Equals(item.Value, value));
        if (matching != 1)
        {
            throw new ArgumentException(
                $"There should be exactly one item with [{valueLabel}]'s value: {value}. "
                + "Either zero or 2 or more [DropdownMenuItem]s were detected with the same value",
                nameof(value));
        }
    }
}

internal sealed class DropdownButtonState<T> : State
{
    private int? _selectedIndex;
    private DropdownRoute<T>? _dropdownRoute;
    private Orientation? _lastOrientation;
    private FocusNode? _internalNode;
    private Dictionary<Type, FlutterAction> _actionMap = [];
    private bool _isHovering;
    private bool _hasPrimaryFocus;
    private bool _isMenuExpanded;

    private DropdownButton<T> CurrentWidget => (DropdownButton<T>)StateWidget;

    private FocusNode EffectiveFocusNode => CurrentWidget.FocusNode ?? _internalNode!;

    private bool Enabled => CurrentWidget.Items is { Count: > 0 } && CurrentWidget.OnChanged is not null;

    public override void InitState()
    {
        base.InitState();
        UpdateSelectedIndex();
        if (CurrentWidget.FocusNode is null)
        {
            _internalNode = new FocusNode();
        }

        _actionMap = new Dictionary<Type, FlutterAction>
        {
            [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(_ =>
            {
                HandleTap();
                return null;
            }),
            [typeof(ButtonActivateIntent)] = new CallbackAction<ButtonActivateIntent>(_ =>
            {
                HandleTap();
                return null;
            }),
        };
        EffectiveFocusNode.AddListener(HandleFocusChanged);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (DropdownButton<T>)oldWidget;
        if (!ReferenceEquals(old.FocusNode, CurrentWidget.FocusNode))
        {
            old.FocusNode?.RemoveListener(HandleFocusChanged);
            if (_internalNode is not null && CurrentWidget.FocusNode is not null)
            {
                _internalNode.RemoveListener(HandleFocusChanged);
                _internalNode.Dispose();
                _internalNode = null;
            }

            if (CurrentWidget.FocusNode is null)
            {
                _internalNode ??= new FocusNode();
            }

            _hasPrimaryFocus = EffectiveFocusNode.HasPrimaryFocus;
            EffectiveFocusNode.AddListener(HandleFocusChanged);
        }

        UpdateSelectedIndex();
    }

    public override void Dispose()
    {
        RemoveDropdownRoute();
        EffectiveFocusNode.RemoveListener(HandleFocusChanged);
        _internalNode?.Dispose();
        _internalNode = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        TextStyle textStyle = widget.Style ?? theme.TextTheme.TitleMedium;
        Orientation newOrientation = GetOrientation(context);
        _lastOrientation ??= newOrientation;
        if (newOrientation != _lastOrientation)
        {
            RemoveDropdownRoute();
            _lastOrientation = newOrientation;
        }

        List<Widget> items;
        if (widget.SelectedItemBuilder is null)
        {
            items = widget.Items is not null ? [.. widget.Items] : [];
        }
        else
        {
            IReadOnlyList<Widget> selectedItems = widget.SelectedItemBuilder(context);
            if (widget.Items is not null && selectedItems.Count != widget.Items.Count)
            {
                throw new InvalidOperationException(
                    "The selectedItemBuilder must return a list of widgets with the same length as the items list."
                    + $"\nCurrently, selectedItemBuilder returns a list of length {selectedItems.Count}, "
                    + $"but items has length {widget.Items.Count}.");
            }

            items = [.. selectedItems];
        }

        int? hintIndex = null;
        if (widget.Hint is not null || (!Enabled && widget.DisabledHint is not null))
        {
            Widget displayedHint = Enabled ? widget.Hint! : widget.DisabledHint ?? widget.Hint!;
            hintIndex = items.Count;
            items.Add(new DefaultTextStyle(
                textStyle.CopyWith(color: theme.HintColor),
                new IgnorePointer(child: new DropdownMenuItemHint(displayedHint, widget.Alignment))));
        }

        EdgeInsetsGeometry padding = ButtonTheme.Of(context).AlignedDropdown && widget.InputDecoration is null
            ? DropdownConstants.AlignedButtonPadding
            : DropdownConstants.UnalignedButtonPadding;

        // If value is null (then _selectedIndex is null) then we display the hint or nothing at all.
        Widget innerItemsWidget;
        if (items.Count == 0)
        {
            innerItemsWidget = new SizedBox();
        }
        else
        {
            innerItemsWidget = new IndexedStack(
                index: _selectedIndex ?? hintIndex,
                alignment: widget.Alignment,
                children: widget.IsDense
                    ? items
                    : [.. items.Select(item => widget.ItemHeight.HasValue
                        ? (Widget)new SizedBox(height: widget.ItemHeight.Value, child: item)
                        : new Column(mainAxisSize: MainAxisSize.Min, children: [item]))]);
        }

        Color iconColor = IconColor(theme);
        Widget effectiveSuffixIcon = new IconTheme(
            new IconThemeData(Color: iconColor, Size: widget.IconSize),
            widget.Icon ?? widget.InputDecoration?.SuffixIcon ?? new Icon(Icons.ArrowDropDown));

        var rowChildren = new List<Widget>
        {
            widget.IsExpanded ? new Expanded(child: innerItemsWidget) : innerItemsWidget,
        };
        if (widget.InputDecoration is null)
        {
            rowChildren.Add(effectiveSuffixIcon);
        }

        Widget result = new DefaultTextStyle(
            Enabled ? textStyle : textStyle.CopyWith(color: theme.DisabledColor),
            new SizedBox(
                height: widget.IsDense ? DenseButtonHeight(context, textStyle) : null,
                child: new Padding(
                    padding.Resolve(Directionality.Of(context)),
                    new Row(
                        mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                        mainAxisSize: MainAxisSize.Min,
                        children: rowChildren))));

        if (!DropdownButtonHideUnderline.At(context))
        {
            double bottom = widget.IsDense || widget.ItemHeight is null ? 0.0 : 8.0;
            result = new Stack(
                children:
                [
                    result,
                    new Positioned(
                        left: 0.0,
                        right: 0.0,
                        bottom: bottom,
                        child: widget.Underline ?? new Container(
                            height: 1.0,
                            decoration: new BoxDecoration(
                                Border: new Rendering.Border(
                                    bottom: new BorderSide(Color.Parse("#FFBDBDBD"), 0.0))))),
                ]);
        }

        MouseCursor effectiveMouseCursor = ResolveMouseCursor(widget.MouseCursor, Enabled);

        if (widget.InputDecoration is not null)
        {
            InputDecoration effectiveDecoration = BuildFormFieldDecoration(context, widget, effectiveSuffixIcon);
            return new Semantics(
                flags: ChildHasButtonSemantic(hintIndex) ? SemanticsFlags.None : SemanticsFlags.IsButton,
                expanded: _isMenuExpanded,
                child: new Actions(
                    _actionMap,
                    new Focus(
                        canRequestFocus: Enabled,
                        focusNode: EffectiveFocusNode,
                        autofocus: widget.Autofocus,
                        child: new MouseRegion(
                            onEnter: _ => HandleHover(true),
                            onExit: _ => HandleHover(false),
                            cursor: effectiveMouseCursor,
                            child: new GestureDetector(
                                onTap: Enabled ? HandleTap : null,
                                behavior: HitTestBehavior.Opaque,
                                child: new InputDecorator(
                                    decoration: effectiveDecoration,
                                    isEmpty: widget.IsEmpty,
                                    isFocused: _hasPrimaryFocus,
                                    isHovering: _isHovering,
                                    child: WithPadding(widget, result)))))));
        }

        return new Semantics(
            flags: ChildHasButtonSemantic(hintIndex) ? SemanticsFlags.None : SemanticsFlags.IsButton,
            expanded: _isMenuExpanded,
            child: new Actions(
                _actionMap,
                new InkWell(
                    mouseCursor: effectiveMouseCursor,
                    onTap: Enabled ? HandleTap : null,
                    canRequestFocus: Enabled,
                    borderRadius: widget.BorderRadius,
                    focusNode: EffectiveFocusNode,
                    autofocus: widget.Autofocus,
                    focusColor: widget.FocusColor ?? theme.FocusColor,
                    enableFeedback: false,
                    child: WithPadding(widget, result))));
    }

    private bool ChildHasButtonSemantic(int? hintIndex) =>
        hintIndex is not null || (_selectedIndex is not null && CurrentWidget.SelectedItemBuilder is null);

    private static Widget WithPadding(DropdownButton<T> widget, Widget child) =>
        widget.Padding.HasValue ? new Padding(widget.Padding.Value, child) : child;

    private Color IconColor(ThemeData theme)
    {
        var widget = CurrentWidget;
        if (Enabled)
        {
            return widget.IconEnabledColor
                   ?? (theme.Brightness == Brightness.Light
                       ? Color.Parse("#FF616161")
                       : Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF));
        }

        return widget.IconDisabledColor
               ?? (theme.Brightness == Brightness.Light
                   ? Color.Parse("#FFBDBDBD")
                   : Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
    }

    private static MouseCursor ResolveMouseCursor(MouseCursor? cursor, bool enabled)
    {
        MouseCursor source = cursor ?? WidgetStateMouseCursor.AdaptiveClickable;
        MaterialState states = enabled ? MaterialState.None : MaterialState.Disabled;
        return source is WidgetStateMouseCursor stateful
            ? stateful.Resolve(states) ?? SystemMouseCursors.Basic
            : source;
    }

    private InputDecoration BuildFormFieldDecoration(
        BuildContext context,
        DropdownButton<T> widget,
        Widget effectiveSuffixIcon)
    {
        InputDecorationThemeData decorationTheme = InputDecorationTheme.Of(context);
        bool filled = widget.InputDecoration!.Filled ?? decorationTheme.Filled;
        bool outlined = widget.InputDecoration.Border?.IsOutline ?? decorationTheme.Border?.IsOutline ?? false;
        double suffixIconEndMargin = filled || outlined ? 12.0 : 0.0;
        InputDecoration effectiveDecoration = widget.InputDecoration with
        {
            SuffixIconConstraints = new BoxConstraints(
                MinWidth: widget.IconSize + suffixIconEndMargin,
                MinHeight: widget.IconSize),
            SuffixIcon = new Padding(
                EdgeInsetsGeometry.DirectionalOnly(end: suffixIconEndMargin),
                effectiveSuffixIcon),
        };

        if (!_hasPrimaryFocus)
        {
            return effectiveDecoration;
        }

        Color? focusColor = widget.FocusColor ?? effectiveDecoration.FocusColor;
        return focusColor is null
            ? effectiveDecoration
            : effectiveDecoration with { FillColor = focusColor.Value };
    }

    private void HandleHover(bool hovering)
    {
        if (_isHovering == hovering)
        {
            return;
        }

        SetState(() => _isHovering = hovering);
    }

    private void HandleFocusChanged()
    {
        if (_hasPrimaryFocus != EffectiveFocusNode.HasPrimaryFocus)
        {
            SetState(() => _hasPrimaryFocus = EffectiveFocusNode.HasPrimaryFocus);
        }
    }

    private void RemoveDropdownRoute()
    {
        _dropdownRoute?.Dismiss();
        _dropdownRoute = null;
        _lastOrientation = null;
    }

    private void UpdateSelectedIndex()
    {
        var widget = CurrentWidget;
        if (widget.Items is null
            || widget.Items.Count == 0
            || (widget.Value is null
                && !widget.Items.Any(item =>
                    item.Enabled && EqualityComparer<T?>.Default.Equals(item.Value, widget.Value))))
        {
            _selectedIndex = null;
            return;
        }

        DropdownButton<T>.ValidateSelection(widget.Items, widget.Value, "DropdownButton");
        for (int itemIndex = 0; itemIndex < widget.Items.Count; itemIndex++)
        {
            if (EqualityComparer<T?>.Default.Equals(widget.Items[itemIndex].Value, widget.Value))
            {
                _selectedIndex = itemIndex;
                return;
            }
        }
    }

    private double DenseButtonHeight(BuildContext context, TextStyle textStyle)
    {
        var theme = Theme.Of(context);
        double fontSize = textStyle.FontSize ?? theme.TextTheme.TitleMedium.FontSize ?? 16.0;
        double lineHeight = textStyle.Height ?? theme.TextTheme.TitleMedium.Height ?? 1.0;
        double scaledFontSize = MediaQuery.TextScalerOf(context).Scale(fontSize * lineHeight);
        return Math.Max(scaledFontSize, Math.Max(CurrentWidget.IconSize, DropdownConstants.DenseButtonHeight));
    }

    private static Orientation GetOrientation(BuildContext context)
    {
        Orientation? result = MediaQuery.MaybeOrientationOf(context);
        if (result is not null)
        {
            return result.Value;
        }

        // If there's no MediaQuery, then use the view aspect to determine orientation. Without a view
        // either, orientation cannot change, so report a stable value instead of throwing.
        Size? size = View.MaybeOf(context)?.PhysicalSize;
        return size is { } viewSize && viewSize.Width > viewSize.Height
            ? Orientation.Landscape
            : Orientation.Portrait;
    }

    private void HandleTap()
    {
        if (!Enabled || _dropdownRoute is not null)
        {
            return;
        }

        var widget = CurrentWidget;
        TextDirection? textDirection = Directionality.MaybeOf(Context);
        EdgeInsetsGeometry menuMargin = ButtonTheme.Of(Context).AlignedDropdown
            ? DropdownConstants.AlignedMenuMargin
            : DropdownConstants.UnalignedMenuMargin;

        var menuItems = new MenuItem<T>[widget.Items!.Count];
        for (int index = 0; index < widget.Items.Count; index++)
        {
            int itemIndex = index;
            menuItems[itemIndex] = new MenuItem<T>(
                item: widget.Items[itemIndex],
                onLayout: size =>
                {
                    // If [_dropdownRoute] is null and onLayout is called, this means that performLayout
                    // was called on a _DropdownRoute that has not left the widget tree but is already
                    // on its way out. Since onLayout is used to determine the menu's initial scroll
                    // offset, it's safe to ignore the call.
                    if (_dropdownRoute is null)
                    {
                        return;
                    }

                    _dropdownRoute.ItemHeights[itemIndex] = size.Height;
                });
        }

        NavigatorState navigator = Navigator.Of(Context);
        if (Context.FindRenderObject() is not RenderBox itemBox || !itemBox.HasSize)
        {
            return;
        }

        Point itemOrigin = itemBox.LocalToGlobal(default, navigator.Context.FindRenderObject());
        var itemRect = new Rect(itemOrigin, itemBox.Size);
        var route = new DropdownRoute<T>(
            items: menuItems,
            buttonRect: menuMargin.Resolve(textDirection ?? TextDirection.Ltr).InflateRect(itemRect),
            padding: DropdownConstants.MenuItemPadding,
            selectedIndex: _selectedIndex ?? 0,
            elevation: widget.Elevation,
            capturedThemes: InheritedTheme.Capture(Context, navigator.Context),
            style: widget.Style ?? Theme.Of(Context).TextTheme.TitleMedium,
            barrierLabel: MaterialLocalizations.Of(Context).ModalBarrierDismissLabel,
            itemHeight: widget.ItemHeight,
            menuWidth: widget.MenuWidth,
            dropdownColor: widget.DropdownColor,
            menuMaxHeight: widget.MenuMaxHeight,
            enableFeedback: widget.EnableFeedback ?? true,
            borderRadius: widget.BorderRadius,
            barrierDismissible: widget.BarrierDismissible,
            dropdownMenuItemMouseCursor: widget.DropdownMenuItemMouseCursor);
        _dropdownRoute = route;
        route.RouteCompleted += HandleRouteCompleted;

        EffectiveFocusNode.RequestFocus();
        navigator.Push(route);
        widget.OnTap?.Invoke();
        SetState(() => _isMenuExpanded = true);
    }

    private void HandleRouteCompleted(DropdownRoute<T> route, DropdownRouteResult<T>? newValue)
    {
        route.RouteCompleted -= HandleRouteCompleted;
        RemoveDropdownRoute();
        if (!Mounted)
        {
            return;
        }

        SetState(() => _isMenuExpanded = false);
        if (newValue is null)
        {
            return;
        }

        CurrentWidget.OnChanged?.Invoke(newValue.Result);
    }
}

/// <summary>The hint slot of a <see cref="DropdownButton{T}"/>, laid out like a menu row.</summary>
internal sealed class DropdownMenuItemHint : DropdownMenuItemContainer
{
    public DropdownMenuItemHint(Widget child, AlignmentGeometry alignment) : base(child, alignment)
    {
    }
}
