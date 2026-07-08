using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/dropdown.dart

public delegate IReadOnlyList<Widget> DropdownButtonBuilder(BuildContext context);

public sealed class DropdownMenuItem<T> : StatelessWidget
{
    public DropdownMenuItem(
        Widget child,
        T? value = default,
        Action? onTap = null,
        bool enabled = true,
        Alignment? alignment = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Value = value;
        OnTap = onTap;
        Enabled = enabled;
        Alignment = alignment;
    }

    public Widget Child { get; }
    public T? Value { get; }
    public Action? OnTap { get; }
    public bool Enabled { get; }
    public Alignment? Alignment { get; }

    public override Widget Build(BuildContext context) => DropdownMenuItemContainer.Build(
        context,
        Child,
        Alignment);
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
        double iconSize = 24,
        bool isDense = false,
        bool isExpanded = false,
        double? itemHeight = 48,
        double? menuWidth = null,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Color? dropdownColor = null,
        double? menuMaxHeight = null,
        bool? enableFeedback = null,
        Alignment? alignment = null,
        BorderRadius? borderRadius = null,
        Thickness? padding = null,
        bool barrierDismissible = true,
        MouseCursor? mouseCursor = null,
        MouseCursor? dropdownMenuItemMouseCursor = null,
        Key? key = null) : base(key)
    {
        if (elevation < 0) throw new ArgumentOutOfRangeException(nameof(elevation));
        ValidateFiniteNonNegative(iconSize, nameof(iconSize));
        if (itemHeight.HasValue && (!double.IsFinite(itemHeight.Value) || itemHeight.Value < 48))
            throw new ArgumentOutOfRangeException(nameof(itemHeight));
        ValidatePositive(menuWidth, nameof(menuWidth));
        ValidatePositive(menuMaxHeight, nameof(menuMaxHeight));
        ValidateInsets(padding, nameof(padding));
        ValidateSelection(items, value);

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
        Alignment = alignment;
        BorderRadius = borderRadius;
        Padding = padding;
        BarrierDismissible = barrierDismissible;
        MouseCursor = mouseCursor;
        DropdownMenuItemMouseCursor = dropdownMenuItemMouseCursor;
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
    public Alignment? Alignment { get; }
    public BorderRadius? BorderRadius { get; }
    public Thickness? Padding { get; }
    public bool BarrierDismissible { get; }
    public MouseCursor? MouseCursor { get; }
    public MouseCursor? DropdownMenuItemMouseCursor { get; }

    public override State CreateState() => new DropdownButtonState<T>();

    private static void ValidateSelection(IReadOnlyList<DropdownMenuItem<T>>? items, T? value)
    {
        if (items is null || items.Count == 0) return;
        var matching = items.Count(item => EqualityComparer<T?>.Default.Equals(item.Value, value));
        if (value is not null && matching != 1)
        {
            throw new ArgumentException("There must be exactly one dropdown item matching value.", nameof(value));
        }

    }

    private static void ValidateFiniteNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidatePositive(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
            throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateInsets(Thickness? value, string name)
    {
        if (!value.HasValue) return;
        var p = value.Value;
        if (!double.IsFinite(p.Left) || !double.IsFinite(p.Top)
            || !double.IsFinite(p.Right) || !double.IsFinite(p.Bottom)
            || p.Left < 0 || p.Top < 0 || p.Right < 0 || p.Bottom < 0)
            throw new ArgumentOutOfRangeException(name);
    }
}

internal sealed class DropdownButtonState<T> : State
{
    private DropdownRoute<T>? _route;
    private bool _isMenuExpanded;
    private int? _selectedIndex;
    private FocusNode? _focusNode;
    private bool _ownsFocusNode;

    private DropdownButton<T> CurrentWidget => (DropdownButton<T>)StateWidget;
    private bool Enabled => CurrentWidget.Items is { Count: > 0 } && CurrentWidget.OnChanged is not null;

    public override void InitState()
    {
        AttachFocusNode(CurrentWidget.FocusNode);
        UpdateSelectedIndex();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldDropdown = (DropdownButton<T>)oldWidget;
        if (!ReferenceEquals(oldDropdown.FocusNode, CurrentWidget.FocusNode))
        {
            DetachFocusNode();
            AttachFocusNode(CurrentWidget.FocusNode);
        }
        UpdateSelectedIndex();
    }

    public override void Dispose()
    {
        if (_route?.Navigator is { } navigator) navigator.RemoveRoute(_route);
        _route = null;
        DetachFocusNode();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        var direction = Directionality.Of(context);
        var alignment = ResolveAlignment(widget.Alignment, direction);
        var style = widget.Style ?? theme.TextTheme.TitleMedium;
        var children = widget.SelectedItemBuilder?.Invoke(context)?.ToList()
                       ?? widget.Items?.Cast<Widget>().ToList()
                       ?? [];
        if (widget.SelectedItemBuilder is not null && widget.Items is not null
            && children.Count != widget.Items.Count)
        {
            throw new InvalidOperationException("selectedItemBuilder must return the same number of widgets as items.");
        }

        int? hintIndex = null;
        if (widget.Hint is not null || (!Enabled && widget.DisabledHint is not null))
        {
            var displayedHint = Enabled ? widget.Hint! : widget.DisabledHint ?? widget.Hint!;
            hintIndex = children.Count;
            children.Add(new DefaultTextStyle(
                style.CopyWith(color: theme.HintColor),
                DropdownMenuItemContainer.Build(context, displayedHint, widget.Alignment)));
        }

        Widget inner;
        if (children.Count == 0)
        {
            inner = new SizedBox();
        }
        else
        {
            var sizedChildren = widget.IsDense
                ? children
                : children.Select(child => widget.ItemHeight.HasValue
                    ? (Widget)new SizedBox(height: widget.ItemHeight.Value, child: child)
                    : new Column(mainAxisSize: MainAxisSize.Min, children: [child])).ToList();
            inner = new IndexedStack(
                children: sizedChildren,
                index: _selectedIndex ?? hintIndex,
                alignment: alignment);
        }

        var iconColor = Enabled
            ? widget.IconEnabledColor ?? (theme.Brightness == Brightness.Light
                ? Color.Parse("#FF616161")
                : Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF))
            : widget.IconDisabledColor ?? (theme.Brightness == Brightness.Light
                ? Color.Parse("#FFBDBDBD")
                : Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
        var suffix = new IconTheme(
            new IconThemeData(Color: iconColor, Size: widget.IconSize),
            widget.Icon ?? new Icon(Icons.ArrowDropDown));

        var rowChildren = new List<Widget>();
        if (widget.IsExpanded) rowChildren.Add(new Expanded(child: inner));
        else rowChildren.Add(inner);
        rowChildren.Add(suffix);

        Widget result = new DefaultTextStyle(
            Enabled ? style : style.CopyWith(color: theme.DisabledColor),
            new SizedBox(
                height: widget.IsDense ? ResolveDenseHeight(context, style) : null,
                child: new Row(
                    mainAxisSize: widget.IsExpanded ? MainAxisSize.Max : MainAxisSize.Min,
                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                    children: rowChildren)));

        var buttonTheme = ButtonTheme.Of(context);
        if (buttonTheme.AlignedDropdown)
        {
            var alignedPadding = direction == TextDirection.Rtl
                ? new Thickness(4, 0, 16, 0)
                : new Thickness(16, 0, 4, 0);
            result = new Padding(alignedPadding, result);
        }

        if (!DropdownButtonHideUnderline.At(context))
        {
            var bottom = widget.IsDense || widget.ItemHeight is null ? 0 : 8;
            result = new Stack(
                children:
                [
                    result,
                    new Positioned(
                        left: 0,
                        right: 0,
                        bottom: bottom,
                        child: widget.Underline
                               ?? new SizedBox(
                                   height: 1,
                                   child: new ColoredBox(Color.Parse("#FFBDBDBD")))),
                ]);
        }

        if (widget.Padding.HasValue) result = new Padding(widget.Padding.Value, result);
        result = new InkWell(
            onTap: Enabled ? HandleTap : null,
            canRequestFocus: Enabled,
            focusNode: _focusNode,
            autofocus: widget.Autofocus,
            focusColor: widget.FocusColor ?? theme.FocusColor,
            mouseCursor: widget.MouseCursor
                         ?? (Enabled ? SystemMouseCursors.Click : SystemMouseCursors.Basic),
            enableFeedback: false,
            borderRadius: widget.BorderRadius,
            child: result);
        return new Semantics(
            flags: SemanticsFlags.IsButton | (Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None),
            onTap: Enabled ? HandleTap : null,
            expanded: _isMenuExpanded,
            child: result);
    }

    private void HandleTap()
    {
        if (!Enabled || _route is not null) return;
        var widget = CurrentWidget;
        var navigator = Navigator.Of(Context);
        if (Context.FindRenderObject() is not RenderBox button || !button.HasSize) return;
        var bounds = ResolveGlobalBounds(button);
        var direction = Directionality.Of(Context);
        if (!ButtonTheme.Of(Context).AlignedDropdown)
        {
            bounds = direction == TextDirection.Rtl
                ? new Rect(bounds.X - 24, bounds.Y, bounds.Width + 40, bounds.Height)
                : new Rect(bounds.X - 16, bounds.Y, bounds.Width + 40, bounds.Height);
        }

        var selectedIndex = _selectedIndex ?? 0;
        _route = new DropdownRoute<T>(
            context: Context,
            items: widget.Items!,
            buttonRect: bounds,
            selectedIndex: selectedIndex,
            elevation: widget.Elevation,
            style: widget.Style ?? Theme.Of(Context).TextTheme.TitleMedium,
            itemHeight: widget.ItemHeight,
            menuWidth: widget.MenuWidth,
            dropdownColor: widget.DropdownColor,
            menuMaxHeight: widget.MenuMaxHeight,
            enableFeedback: widget.EnableFeedback ?? true,
            borderRadius: widget.BorderRadius,
            barrierDismissible: widget.BarrierDismissible,
            mouseCursor: widget.DropdownMenuItemMouseCursor);
        navigator.Push(_route);
        _focusNode?.RequestFocus();
        widget.OnTap?.Invoke();
        SetState(() => _isMenuExpanded = true);
        _ = HandleRouteResult(_route);
    }

    private async Task HandleRouteResult(DropdownRoute<T> route)
    {
        var result = await route.Completed;
        if (!Mounted || !ReferenceEquals(_route, route)) return;
        SetState(() =>
        {
            _route = null;
            _isMenuExpanded = false;
        });
        if (result is not null) CurrentWidget.OnChanged?.Invoke(result.Value);
    }

    private void UpdateSelectedIndex()
    {
        _selectedIndex = null;
        var items = CurrentWidget.Items;
        if (items is null || items.Count == 0) return;
        if (CurrentWidget.Value is null && !items.Any(item => item.Enabled && item.Value is null)) return;
        for (var i = 0; i < items.Count; i++)
        {
            if (EqualityComparer<T?>.Default.Equals(items[i].Value, CurrentWidget.Value))
            {
                _selectedIndex = i;
                return;
            }
        }
    }

    private void AttachFocusNode(FocusNode? external)
    {
        _focusNode = external ?? new FocusNode();
        _ownsFocusNode = external is null;
    }

    private void DetachFocusNode()
    {
        if (_ownsFocusNode) _focusNode?.Dispose();
        _focusNode = null;
        _ownsFocusNode = false;
    }

    private double ResolveDenseHeight(BuildContext context, TextStyle style)
    {
        var fontSize = style.FontSize ?? Theme.Of(context).TextTheme.TitleMedium.FontSize ?? 14;
        var lineHeight = style.Height ?? Theme.Of(context).TextTheme.TitleMedium.Height ?? 1;
        var scale = MediaQuery.MaybeTextScaleFactorOf(context) ?? 1;
        return Math.Max(fontSize * lineHeight * scale, Math.Max(CurrentWidget.IconSize, 24));
    }

    private static Alignment ResolveAlignment(Alignment? alignment, TextDirection direction) =>
        alignment ?? (direction == TextDirection.Rtl ? Alignment.CenterRight : Alignment.CenterLeft);

    private static Rect ResolveGlobalBounds(RenderBox renderBox)
    {
        var transform = Matrix.Identity;
        RenderObject? child = renderBox;
        while (child?.Parent is not null)
        {
            var parent = child.Parent;
            var childOffset = child.parentData is BoxParentData data ? data.offset : default;
            var childTransform = Matrix.CreateTranslation(childOffset.X, childOffset.Y);
            if (parent is RenderTransform renderTransform) childTransform *= renderTransform.Transform;
            transform = childTransform * transform;
            child = parent;
        }

        var points = new[]
        {
            transform.Transform(new Point(0, 0)),
            transform.Transform(new Point(renderBox.Size.Width, 0)),
            transform.Transform(new Point(0, renderBox.Size.Height)),
            transform.Transform(new Point(renderBox.Size.Width, renderBox.Size.Height)),
        };
        var left = points.Min(point => point.X);
        var top = points.Min(point => point.Y);
        var right = points.Max(point => point.X);
        var bottom = points.Max(point => point.Y);
        return new Rect(left, top, right - left, bottom - top);
    }
}

internal sealed record DropdownRouteResult<T>(T? Value);

internal sealed class DropdownRoute<T> : PageRoute
{
    private readonly IReadOnlyList<DropdownMenuItem<T>> _items;
    private readonly ThemeData _theme;
    private readonly IconThemeData _iconTheme;
    private readonly MaterialLocalizations _localizations;
    private readonly MediaQueryData _mediaQuery;
    private readonly TextDirection _direction;
    private readonly AnimationController _animation;
    private readonly TaskCompletionSource<DropdownRouteResult<T>?> _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private object? _pendingResult;
    private bool _isExiting;
    private int _focusIndex;

    public DropdownRoute(
        BuildContext context,
        IReadOnlyList<DropdownMenuItem<T>> items,
        Rect buttonRect,
        int selectedIndex,
        int elevation,
        TextStyle style,
        double? itemHeight,
        double? menuWidth,
        Color? dropdownColor,
        double? menuMaxHeight,
        bool enableFeedback,
        BorderRadius? borderRadius,
        bool barrierDismissible,
        MouseCursor? mouseCursor) : base()
    {
        _items = items;
        _theme = Theme.Of(context);
        _iconTheme = IconTheme.Of(context);
        _localizations = MaterialLocalizations.Of(context);
        _mediaQuery = MediaQuery.Of(context);
        _direction = Directionality.Of(context);
        _animation = new AnimationController(TimeSpan.FromMilliseconds(300));
        _animation.Changed += HandleAnimationChanged;
        _animation.Dismissed += HandleDismissed;
        ButtonRect = buttonRect;
        SelectedIndex = selectedIndex;
        Elevation = elevation;
        Style = style;
        ItemHeight = itemHeight;
        MenuWidth = menuWidth;
        DropdownColor = dropdownColor;
        MenuMaxHeight = menuMaxHeight;
        EnableFeedback = enableFeedback;
        BorderRadius = borderRadius;
        BarrierDismissible = barrierDismissible;
        MouseCursor = mouseCursor;
        ItemHeights = Enumerable.Repeat(itemHeight ?? 48, items.Count).ToArray();
        _focusIndex = ResolveInitialFocusIndex(selectedIndex);
        ScrollController = new ScrollController(
            initialScrollOffset: GetMenuLimits(_mediaQuery.Size.Height).ScrollOffset);
    }

    public override bool Opaque => false;
    public Rect ButtonRect { get; }
    public int SelectedIndex { get; }
    public int Elevation { get; }
    public TextStyle Style { get; }
    public double? ItemHeight { get; }
    public double? MenuWidth { get; }
    public Color? DropdownColor { get; }
    public double? MenuMaxHeight { get; }
    public bool EnableFeedback { get; }
    public BorderRadius? BorderRadius { get; }
    public bool BarrierDismissible { get; }
    public MouseCursor? MouseCursor { get; }
    public double[] ItemHeights { get; }
    public ScrollController ScrollController { get; }
    public double Progress => Math.Clamp(_animation.Value, 0, 1);
    public bool IsExiting => _isExiting;
    public Task<DropdownRouteResult<T>?> Completed => _completed.Task;

    protected override void OnAttach() => _animation.Forward(0);

    public override bool WillPop(object? result)
    {
        if (_isExiting || _animation.Value <= 0) return base.WillPop(result);
        _pendingResult = result;
        _isExiting = true;
        _animation.Reverse();
        return false;
    }

    public override void DidComplete(object? result)
    {
        _completed.TrySetResult(result as DropdownRouteResult<T>);
    }

    public override Widget BuildPage(BuildContext context)
    {
        Widget menu = new DropdownMenuPanel<T>(this, _items, _focusIndex);
        menu = new IconTheme(_iconTheme, menu);
        menu = new MaterialLocalizationsScope(_localizations, menu);
        menu = new Theme(_theme, menu);
        menu = new DropdownMenuPositionLayout<T>(this, menu);
        var localizations = _localizations;
        Widget barrier = new SizedBox();
        if (BarrierDismissible)
        {
            barrier = new Semantics(
                label: localizations.ModalBarrierDismissLabel,
                onTap: () => Navigator?.MaybePop(),
                child: new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: () => Navigator?.MaybePop(),
                    child: barrier));
        }
        else
        {
            barrier = new GestureDetector(behavior: HitTestBehavior.Opaque, child: barrier);
        }

        Widget page = new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: barrier),
                new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: menu),
            ]);
        page = new Focus(autofocus: true, onKeyEvent: HandleKeyEvent, child: page);
        page = new MediaQuery(_mediaQuery, page);
        return new Directionality(_direction, page);
    }

    public override void Dispose()
    {
        _animation.Changed -= HandleAnimationChanged;
        _animation.Dismissed -= HandleDismissed;
        _animation.Dispose();
        ScrollController.Dispose();
        _completed.TrySetResult(null);
        base.Dispose();
    }

    public void UpdateItemHeight(int index, double height)
    {
        if (index < 0 || index >= ItemHeights.Length || Math.Abs(ItemHeights[index] - height) < 0.01) return;
        ItemHeights[index] = height;
        NotifyRouteChanged();
    }

    public DropdownMenuLimits GetMenuLimits(double availableHeight)
    {
        var computedMaxHeight = Math.Max(0, availableHeight - 96);
        if (MenuMaxHeight.HasValue) computedMaxHeight = Math.Min(computedMaxHeight, MenuMaxHeight.Value);
        var buttonTop = ButtonRect.Top;
        var buttonBottom = Math.Min(ButtonRect.Bottom, availableHeight);
        var selectedOffset = GetItemOffset(SelectedIndex);
        var topLimit = Math.Min(48, buttonTop);
        var bottomLimit = Math.Max(availableHeight - 48, buttonBottom);
        var selectedHeight = ItemHeights.Length == 0 ? 48 : ItemHeights[SelectedIndex];
        var menuTop = buttonTop - selectedOffset - ((selectedHeight - ButtonRect.Height) / 2);
        var preferredHeight = 16 + ItemHeights.Sum();
        var menuHeight = Math.Min(computedMaxHeight, preferredHeight);
        var menuBottom = menuTop + menuHeight;
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
        if (menuBottom - (selectedHeight / 2) < buttonBottom - (ButtonRect.Height / 2))
        {
            menuBottom = buttonBottom - (ButtonRect.Height / 2) + (selectedHeight / 2);
            menuTop = menuBottom - menuHeight;
        }
        var scrollOffset = preferredHeight > computedMaxHeight
            ? Math.Clamp(selectedOffset - (buttonTop - menuTop), 0, Math.Max(0, preferredHeight - menuHeight))
            : 0;
        return new DropdownMenuLimits(menuTop, menuBottom, menuHeight, scrollOffset);
    }

    public double GetItemOffset(int index) => 8 + ItemHeights.Take(index).Sum();

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
    {
        if (!@event.IsDown) return KeyEventResult.Ignored;
        if (@event.Key is "Escape" or "Esc")
        {
            Navigator?.MaybePop();
            return KeyEventResult.Handled;
        }
        if (@event.Key is "ArrowDown" or "Down")
        {
            MoveFocus(1);
            return KeyEventResult.Handled;
        }
        if (@event.Key is "ArrowUp" or "Up")
        {
            MoveFocus(-1);
            return KeyEventResult.Handled;
        }
        if (@event.Key is "Enter" or "Return" or "NumPadEnter" or "NumpadEnter" or "Space" or "Spacebar")
        {
            ActivateFocused();
            return KeyEventResult.Handled;
        }
        return KeyEventResult.Ignored;
    }

    private void MoveFocus(int delta)
    {
        var next = _focusIndex;
        for (var i = 0; i < _items.Count; i++)
        {
            next = (next + delta + _items.Count) % _items.Count;
            if (_items[next].Enabled)
            {
                _focusIndex = next;
                NotifyRouteChanged();
                return;
            }
        }
    }

    private void ActivateFocused()
    {
        if (_focusIndex < 0 || _focusIndex >= _items.Count || !_items[_focusIndex].Enabled) return;
        var item = _items[_focusIndex];
        item.OnTap?.Invoke();
        Navigator?.MaybePop(new DropdownRouteResult<T>(item.Value));
    }

    private int ResolveInitialFocusIndex(int selectedIndex)
    {
        if (selectedIndex >= 0 && selectedIndex < _items.Count && _items[selectedIndex].Enabled) return selectedIndex;
        for (var i = 0; i < _items.Count; i++) if (_items[i].Enabled) return i;
        return -1;
    }

    private void HandleAnimationChanged() => NotifyRouteChanged();
    private void HandleDismissed() { if (_isExiting) Navigator?.MaybePop(_pendingResult); }
}

internal sealed record DropdownMenuLimits(double Top, double Bottom, double Height, double ScrollOffset);

internal sealed class DropdownMenuPanel<T> : StatelessWidget
{
    private readonly DropdownRoute<T> _route;
    private readonly IReadOnlyList<DropdownMenuItem<T>> _items;
    private readonly int _focusIndex;

    public DropdownMenuPanel(DropdownRoute<T> route, IReadOnlyList<DropdownMenuItem<T>> items, int focusIndex)
    {
        _route = route;
        _items = items;
        _focusIndex = focusIndex;
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var children = new List<Widget>(_items.Count);
        var unit = 0.5 / (_items.Count + 1.5);
        for (var i = 0; i < _items.Count; i++)
        {
            var index = i;
            var item = _items[i];
            Widget child = item;
            if (_route.ItemHeight.HasValue) child = new SizedBox(height: _route.ItemHeight.Value, child: child);
            child = new Padding(new Thickness(16, 0), child);
            if (i == _focusIndex) child = new ColoredBox(theme.FocusColor, child);
            if (!item.Enabled)
            {
                child = new DefaultTextStyle(_route.Style.CopyWith(color: theme.DisabledColor), child);
            }
            child = new DropdownMeasuredItem(size => _route.UpdateItemHeight(index, size.Height), child);
            if (item.Enabled)
            {
                child = new InkWell(
                    onTap: () =>
                    {
                        item.OnTap?.Invoke();
                        _route.Navigator?.MaybePop(new DropdownRouteResult<T>(item.Value));
                    },
                    enableFeedback: _route.EnableFeedback,
                    mouseCursor: _route.MouseCursor ?? SystemMouseCursors.Click,
                    child: child);
            }
            else
            {
                child = new Listener(behavior: HitTestBehavior.Opaque, child: child);
            }
            var opacity = i == _route.SelectedIndex
                ? (_route.Progress > 0 ? 1 : 0)
                : Interval(_route.Progress, Math.Clamp(0.5 + ((i + 1) * unit), 0, 1),
                    Math.Clamp(0.5 + ((i + 2.5) * unit), 0, 1));
            child = new Opacity(opacity, new Semantics(role: SemanticsRole.MenuItem, child: child));
            children.Add(child);
        }

        Widget content = new SingleChildScrollView(
            controller: _route.ScrollController,
            padding: new Thickness(0, 8),
            child: new ListBody(children: children));
        content = new Scrollbar(child: content, controller: _route.ScrollController);
        content = new Semantics(
            role: SemanticsRole.Menu,
            label: MaterialLocalizations.Of(context).PopupMenuLabel,
            scopesRoute: true,
            namesRoute: true,
            explicitChildNodes: true,
            child: content);
        var radius = _route.BorderRadius ?? Rendering.BorderRadius.Circular(2);
        content = new DecoratedBox(
            new BoxDecoration(
                Color: _route.DropdownColor ?? theme.CanvasColor,
                BorderRadius: radius,
                BoxShadows: BuildShadow(theme.ShadowColor, _route.Elevation)),
            content);
        content = new ClipRRect(radius, content);
        content = new DropdownMenuReveal(
            progress: _route.IsExiting ? 1 : Interval(_route.Progress, 0.25, 0.5),
            selectedItemOffset: _route.GetItemOffset(_route.SelectedIndex),
            child: content);
        return new DefaultTextStyle(
            _route.Style,
            new Opacity(
                _route.IsExiting
                    ? Interval(_route.Progress, 0.75, 1)
                    : Interval(_route.Progress, 0, 0.25),
                content));
    }

    private static BoxShadows? BuildShadow(Color color, int elevation)
    {
        if (elevation <= 0 || color.A == 0) return null;
        return new BoxShadows(new BoxShadow
        {
            OffsetY = Math.Max(1, elevation * 0.5),
            Blur = Math.Max(2, elevation * 2.4),
            Color = MaterialButtonCore.ApplyOpacity(color, 0.20),
        });
    }

    private static double Interval(double value, double start, double end) =>
        end <= start ? (value >= end ? 1 : 0) : Math.Clamp((value - start) / (end - start), 0, 1);
}

internal sealed class DropdownMenuReveal : SingleChildRenderObjectWidget
{
    public DropdownMenuReveal(double progress, double selectedItemOffset, Widget child) : base(child)
    {
        Progress = Math.Clamp(progress, 0, 1);
        SelectedItemOffset = Math.Max(0, selectedItemOffset);
    }

    public double Progress { get; }
    public double SelectedItemOffset { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderDropdownMenuReveal(Progress, SelectedItemOffset);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var reveal = (RenderDropdownMenuReveal)renderObject;
        reveal.Progress = Progress;
        reveal.SelectedItemOffset = SelectedItemOffset;
    }
}

internal sealed class RenderDropdownMenuReveal : RenderProxyBox
{
    private double _progress;
    private double _selectedItemOffset;

    public RenderDropdownMenuReveal(double progress, double selectedItemOffset)
    {
        _progress = progress;
        _selectedItemOffset = selectedItemOffset;
    }

    public double Progress
    {
        get => _progress;
        set
        {
            var next = Math.Clamp(value, 0, 1);
            if (Math.Abs(_progress - next) < 0.0001) return;
            _progress = next;
            MarkNeedsPaint();
        }
    }

    public double SelectedItemOffset
    {
        get => _selectedItemOffset;
        set
        {
            var next = Math.Max(0, value);
            if (Math.Abs(_selectedItemOffset - next) < 0.0001) return;
            _selectedItemOffset = next;
            MarkNeedsPaint();
        }
    }

    public Rect RevealRect
    {
        get
        {
            var topStart = Math.Clamp(SelectedItemOffset, 0, Math.Max(0, Size.Height - 48));
            var bottomStart = Math.Clamp(topStart + 48, Math.Min(48, Size.Height), Size.Height);
            var top = Lerp(topStart, 0, Progress);
            var bottom = Lerp(bottomStart, Size.Height, Progress);
            return new Rect(0, top, Size.Width, Math.Max(0, bottom - top));
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null || RevealRect.Width <= 0 || RevealRect.Height <= 0) return;
        var clip = new Rect(RevealRect.Position + offset, RevealRect.Size);
        context.PushClipRect(clip, clipped => base.Paint(clipped, offset));
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return RevealRect.Contains(position) && base.HitTestChildren(result, position);
    }

    private static double Lerp(double from, double to, double t) => from + ((to - from) * t);
}

internal sealed class DropdownMenuPositionLayout<T> : SingleChildRenderObjectWidget
{
    public DropdownMenuPositionLayout(DropdownRoute<T> route, Widget child) : base(child)
    {
        Route = route;
    }

    public DropdownRoute<T> Route { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderDropdownMenuPositionLayout<T>(Route, Directionality.Of(context));

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderDropdownMenuPositionLayout<T>)renderObject;
        layout.Route = Route;
        layout.TextDirection = Directionality.Of(context);
    }
}

internal sealed class RenderDropdownMenuPositionLayout<T> : RenderProxyBox
{
    private DropdownRoute<T> _route;
    private TextDirection _textDirection;

    public RenderDropdownMenuPositionLayout(DropdownRoute<T> route, TextDirection textDirection)
    {
        _route = route;
        _textDirection = textDirection;
    }

    public DropdownRoute<T> Route
    {
        get => _route;
        set { if (!ReferenceEquals(_route, value)) { _route = value; MarkNeedsLayout(); } }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } }
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(Constraints.Biggest);
        if (Child is null) return;
        var width = Math.Min(Size.Width, Route.MenuWidth ?? Route.ButtonRect.Width);
        var limits = Route.GetMenuLimits(Size.Height);
        Child.Layout(new BoxConstraints(
            MinWidth: width,
            MaxWidth: width,
            MaxHeight: Math.Max(0, limits.Height)), parentUsesSize: true);
        var x = TextDirection == TextDirection.Rtl
            ? Math.Clamp(Route.ButtonRect.Right, 0, Size.Width) - Child.Size.Width
            : Math.Clamp(Route.ButtonRect.Left, 0, Math.Max(0, Size.Width - Child.Size.Width));
        ((BoxParentData)Child.parentData!).offset = new Point(x, limits.Top);
    }
}

internal sealed class DropdownMeasuredItem : SingleChildRenderObjectWidget
{
    public DropdownMeasuredItem(Action<Size> onLayout, Widget child) : base(child)
    {
        OnLayout = onLayout;
    }

    public Action<Size> OnLayout { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderDropdownMeasuredItem(OnLayout);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject) =>
        ((RenderDropdownMeasuredItem)renderObject).OnLayout = OnLayout;
}

internal sealed class RenderDropdownMeasuredItem : RenderProxyBox
{
    public RenderDropdownMeasuredItem(Action<Size> onLayout)
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

internal static class DropdownMenuItemContainer
{
    public static Widget Build(BuildContext context, Widget child, Alignment? alignment) => new Semantics(
        flags: SemanticsFlags.IsButton,
        child: new ConstrainedBox(
            new BoxConstraints(MinHeight: 48),
            new Align(
                alignment: alignment
                           ?? (Directionality.Of(context) == TextDirection.Rtl
                               ? Alignment.CenterRight
                               : Alignment.CenterLeft),
                child: child)));
}
