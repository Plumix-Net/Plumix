using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/dropdown_menu.dart

public delegate IReadOnlyList<DropdownMenuEntry<T>> DropdownMenuFilterCallback<T>(
    IReadOnlyList<DropdownMenuEntry<T>> entries,
    string filter);

public delegate int? DropdownMenuSearchCallback<T>(
    IReadOnlyList<DropdownMenuEntry<T>> entries,
    string query);

public delegate InputDecoration DropdownMenuDecorationBuilder(
    BuildContext context,
    MenuController controller);

public sealed class DropdownMenuEntry<T>
{
    public DropdownMenuEntry(
        T value,
        string label,
        Widget? labelWidget = null,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        bool enabled = true,
        ButtonStyle? style = null)
    {
        Value = value;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        LabelWidget = labelWidget;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        Enabled = enabled;
        Style = style;
    }

    public T Value { get; }
    public string Label { get; }
    public Widget? LabelWidget { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public bool Enabled { get; }
    public ButtonStyle? Style { get; }
}

public enum DropdownMenuCloseBehavior
{
    All,
    Self,
    None,
}

public sealed class MenuController : ChangeNotifier
{
    private Action? _open;
    private Action? _close;
    private object? _owner;

    public bool IsOpen { get; private set; }

    public void Open()
    {
        if (_open is null) throw new InvalidOperationException("The MenuController is not attached to a menu anchor.");
        _open();
    }

    public void Close() => _close?.Invoke();

    public void CloseChildren()
    {
        if (_owner is null) throw new InvalidOperationException("The MenuController is not attached to a menu anchor.");
        (_owner as IMenuControllerHost)?.CloseChildren();
    }

    internal void Attach(object owner, Action open, Action close)
    {
        if (_owner is not null && !ReferenceEquals(_owner, owner))
            throw new InvalidOperationException("A MenuController cannot be attached to more than one menu anchor.");
        _owner = owner;
        _open = open;
        _close = close;
    }

    internal void Detach(object owner)
    {
        if (!ReferenceEquals(_owner, owner)) return;
        _owner = null;
        _open = null;
        _close = null;
        SetOpen(false);
    }

    internal void SetOpen(bool value)
    {
        if (IsOpen == value) return;
        IsOpen = value;
        NotifyListeners();
    }

    /// <summary>Returns the nearest menu-anchor controller without creating an inherited dependency.</summary>
    public static MenuController? MaybeOf(BuildContext context)
    {
        return context.GetInherited<MenuControllerScope>()?.Controller;
    }

    internal static MenuControllerScope? MaybeScopeOf(BuildContext context)
    {
        return context.GetInherited<MenuControllerScope>();
    }
}

internal interface IMenuControllerHost
{
    void CloseChildren();
}

internal sealed class MenuControllerScope : InheritedWidget
{
    public MenuControllerScope(
        MenuController controller,
        MenuAnchorState host,
        bool isOpen,
        Axis orientation,
        Widget child) : base()
    {
        Controller = controller;
        Host = host;
        IsOpen = isOpen;
        Orientation = orientation;
        Child = child;
    }

    public MenuController Controller { get; }
    public MenuAnchorState Host { get; }
    public bool IsOpen { get; }
    public Axis Orientation { get; }
    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;
    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        ((MenuControllerScope)oldWidget).IsOpen != IsOpen
        || ((MenuControllerScope)oldWidget).Orientation != Orientation;
}

public sealed class DropdownMenu<T> : StatefulWidget
{
    public DropdownMenu(
        IReadOnlyList<DropdownMenuEntry<T>> dropdownMenuEntries,
        bool enabled = true,
        double? width = null,
        double? menuHeight = null,
        Widget? leadingIcon = null,
        Widget? trailingIcon = null,
        bool showTrailingIcon = true,
        FocusNode? trailingIconFocusNode = null,
        Widget? label = null,
        string? hintText = null,
        string? helperText = null,
        string? errorText = null,
        Widget? selectedTrailingIcon = null,
        bool enableFilter = false,
        bool enableSearch = true,
        TextStyle? textStyle = null,
        TextAlign textAlign = TextAlign.Start,
        InputDecorationThemeData? inputDecorationTheme = null,
        DropdownMenuDecorationBuilder? decorationBuilder = null,
        MenuStyle? menuStyle = null,
        TextEditingController? controller = null,
        T? initialSelection = default,
        Action<T?>? onSelected = null,
        FocusNode? focusNode = null,
        bool? requestFocusOnTap = null,
        bool selectOnly = false,
        Thickness? expandedInsets = null,
        DropdownMenuFilterCallback<T>? filterCallback = null,
        DropdownMenuSearchCallback<T>? searchCallback = null,
        Vector? alignmentOffset = null,
        DropdownMenuCloseBehavior closeBehavior = DropdownMenuCloseBehavior.All,
        int? maxLines = 1,
        double? cursorHeight = null,
        string? restorationId = null,
        MenuController? menuController = null,
        Thickness? scrollPadding = null,
        Key? key = null) : base(key)
    {
        if (dropdownMenuEntries is null) throw new ArgumentNullException(nameof(dropdownMenuEntries));
        ValidatePositive(width, nameof(width));
        ValidatePositive(menuHeight, nameof(menuHeight));
        ValidatePositive(cursorHeight, nameof(cursorHeight));
        if (maxLines.HasValue && maxLines.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));
        if (filterCallback is not null && !enableFilter)
            throw new ArgumentException("filterCallback requires enableFilter=true.", nameof(filterCallback));
        if (trailingIconFocusNode is not null && !showTrailingIcon)
            throw new ArgumentException("trailingIconFocusNode requires showTrailingIcon=true.", nameof(trailingIconFocusNode));
        if (decorationBuilder is not null && (label is not null || hintText is not null || helperText is not null || errorText is not null))
            throw new ArgumentException("label/hint/helper/error must be supplied by decorationBuilder when it is set.", nameof(decorationBuilder));
        ValidateFiniteInsets(expandedInsets, nameof(expandedInsets));
        ValidateFiniteInsets(scrollPadding, nameof(scrollPadding));

        DropdownMenuEntries = dropdownMenuEntries;
        Enabled = enabled;
        Width = width;
        MenuHeight = menuHeight;
        LeadingIcon = leadingIcon;
        TrailingIcon = trailingIcon;
        ShowTrailingIcon = showTrailingIcon;
        TrailingIconFocusNode = trailingIconFocusNode;
        Label = label;
        HintText = hintText;
        HelperText = helperText;
        ErrorText = errorText;
        SelectedTrailingIcon = selectedTrailingIcon;
        EnableFilter = enableFilter;
        EnableSearch = enableSearch;
        TextStyle = textStyle;
        TextAlign = textAlign;
        InputDecorationTheme = inputDecorationTheme;
        DecorationBuilder = decorationBuilder;
        MenuStyle = menuStyle;
        Controller = controller;
        InitialSelection = initialSelection;
        OnSelected = onSelected;
        FocusNode = focusNode;
        RequestFocusOnTap = requestFocusOnTap;
        SelectOnly = selectOnly;
        ExpandedInsets = expandedInsets;
        FilterCallback = filterCallback;
        SearchCallback = searchCallback;
        AlignmentOffset = alignmentOffset ?? default;
        CloseBehavior = closeBehavior;
        MaxLines = maxLines;
        CursorHeight = cursorHeight;
        RestorationId = restorationId;
        MenuController = menuController;
        ScrollPadding = scrollPadding ?? new Thickness(20);
    }

    public IReadOnlyList<DropdownMenuEntry<T>> DropdownMenuEntries { get; }
    public bool Enabled { get; }
    public double? Width { get; }
    public double? MenuHeight { get; }
    public Widget? LeadingIcon { get; }
    public Widget? TrailingIcon { get; }
    public bool ShowTrailingIcon { get; }
    public FocusNode? TrailingIconFocusNode { get; }
    public Widget? Label { get; }
    public string? HintText { get; }
    public string? HelperText { get; }
    public string? ErrorText { get; }
    public Widget? SelectedTrailingIcon { get; }
    public bool EnableFilter { get; }
    public bool EnableSearch { get; }
    public TextStyle? TextStyle { get; }
    public TextAlign TextAlign { get; }
    public InputDecorationThemeData? InputDecorationTheme { get; }
    public DropdownMenuDecorationBuilder? DecorationBuilder { get; }
    public MenuStyle? MenuStyle { get; }
    public TextEditingController? Controller { get; }
    public T? InitialSelection { get; }
    public Action<T?>? OnSelected { get; }
    public FocusNode? FocusNode { get; }
    public bool? RequestFocusOnTap { get; }
    public bool SelectOnly { get; }
    public Thickness? ExpandedInsets { get; }
    public DropdownMenuFilterCallback<T>? FilterCallback { get; }
    public DropdownMenuSearchCallback<T>? SearchCallback { get; }
    public Vector AlignmentOffset { get; }
    public DropdownMenuCloseBehavior CloseBehavior { get; }
    public int? MaxLines { get; }
    public double? CursorHeight { get; }
    public string? RestorationId { get; }
    public MenuController? MenuController { get; }
    public Thickness ScrollPadding { get; }

    public override State CreateState() => new DropdownMenuState<T>();

    private static void ValidatePositive(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
            throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateFiniteInsets(Thickness? value, string name)
    {
        if (!value.HasValue) return;
        var p = value.Value;
        if (!double.IsFinite(p.Left) || !double.IsFinite(p.Top)
            || !double.IsFinite(p.Right) || !double.IsFinite(p.Bottom))
            throw new ArgumentOutOfRangeException(name);
    }
}

internal sealed class DropdownMenuState<T> : State
{
    private TextEditingController? _controller;
    private FocusNode? _focusNode;
    private FocusNode? _trailingFocusNode;
    private MenuController? _menuController;
    private bool _ownsController;
    private bool _ownsFocusNode;
    private bool _ownsTrailingFocusNode;
    private bool _ownsMenuController;
    private bool _suppressControllerChange;
    private bool _filterActive;
    private bool _searchActive;
    private int? _currentHighlight;
    private IReadOnlyList<DropdownMenuEntry<T>> _filteredEntries = [];
    private DropdownRoute<T>? _route;

    private DropdownMenu<T> Current => (DropdownMenu<T>)StateWidget;

    public override void InitState()
    {
        AttachController(Current.Controller);
        AttachFocusNode(Current.FocusNode);
        AttachTrailingFocusNode(Current.TrailingIconFocusNode);
        AttachMenuController(Current.MenuController);
        _filteredEntries = Current.DropdownMenuEntries;
        _searchActive = Current.EnableSearch;
        ApplyInitialSelection();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (DropdownMenu<T>)oldWidget;
        if (!ReferenceEquals(old.Controller, Current.Controller))
        {
            DetachController();
            AttachController(Current.Controller);
        }
        if (!ReferenceEquals(old.FocusNode, Current.FocusNode))
        {
            DetachFocusNode();
            AttachFocusNode(Current.FocusNode);
        }
        if (!ReferenceEquals(old.TrailingIconFocusNode, Current.TrailingIconFocusNode))
        {
            DetachTrailingFocusNode();
            AttachTrailingFocusNode(Current.TrailingIconFocusNode);
        }
        if (!ReferenceEquals(old.MenuController, Current.MenuController))
        {
            DetachMenuController();
            AttachMenuController(Current.MenuController);
        }
        if (!ReferenceEquals(old.DropdownMenuEntries, Current.DropdownMenuEntries))
        {
            _filteredEntries = Current.DropdownMenuEntries;
            _currentHighlight = null;
            RecomputeEntries();
            UpdateRouteItems(notify: false);
        }
        if (old.EnableFilter != Current.EnableFilter && !Current.EnableFilter)
            _filterActive = false;
        if (old.EnableSearch != Current.EnableSearch && !Current.EnableSearch)
        {
            _searchActive = false;
            _currentHighlight = null;
        }
        if (!EqualityComparer<T?>.Default.Equals(old.InitialSelection, Current.InitialSelection))
            ApplyInitialSelection();
    }

    public override void Dispose()
    {
        if (_route is not null) _route.Navigator?.MaybePop();
        DetachMenuController();
        DetachTrailingFocusNode();
        DetachFocusNode();
        DetachController();
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dropdownTheme = DropdownMenuTheme.Of(context);
        var defaults = DropdownMenuTheme.Defaults(context);
        bool canRequestFocus = CanRequestFocus(theme);
        bool isButton = !canRequestFocus || Current.SelectOnly;
        var baseStyle = Current.TextStyle ?? dropdownTheme.TextStyle ?? defaults.TextStyle ?? theme.TextTheme.BodyLarge;
        var effectiveStyle = Current.Enabled
            ? baseStyle
            : baseStyle.CopyWith(color: dropdownTheme.DisabledColor ?? defaults.DisabledColor);
        var inputTheme = Current.InputDecorationTheme
                         ?? dropdownTheme.InputDecorationTheme
                         ?? defaults.InputDecorationTheme
                         ?? new InputDecorationThemeData(Border: new OutlineInputBorder());

        var decoration = Current.DecorationBuilder?.Invoke(context, _menuController!)
                         ?? new InputDecoration(
                             label: Current.Label,
                             hintText: Current.HintText,
                             helperText: Current.HelperText,
                             errorText: Current.ErrorText,
                             prefixIcon: Current.LeadingIcon,
                             suffixIcon: BuildSuffixIcon());
        if (decoration.SuffixIcon is null && Current.ShowTrailingIcon)
            decoration = decoration.WithSuffixIcon(BuildSuffixIcon());
        decoration = decoration.ApplyDefaults(inputTheme);

        Widget textField = new Semantics(
            flags: isButton ? SemanticsFlags.IsButton : SemanticsFlags.None,
            expanded: _menuController!.IsOpen,
            onTap: Current.Enabled ? ToggleMenu : null,
            child: new TextField(
                controller: _controller,
                focusNode: _focusNode,
                decoration: decoration,
                style: effectiveStyle,
                textAlign: Current.TextAlign,
                readOnly: isButton,
                maxLines: Current.MaxLines,
                enabled: Current.Enabled,
                onTap: Current.Enabled ? ToggleMenu : null,
                onChanged: HandleTextChanged,
                onSubmitted: _ => HandleSubmitted(),
                canRequestFocus: canRequestFocus,
                mouseCursor: Current.Enabled
                    ? isButton ? SystemMouseCursors.Click : SystemMouseCursors.Text
                    : SystemMouseCursors.Basic,
                onKeyEvent: HandleKeyEvent));

        Widget body;
        if (Current.ExpandedInsets.HasValue)
        {
            var p = Current.ExpandedInsets.Value;
            body = new Padding(new Thickness(p.Left, 0, p.Right, 0), textField);
        }
        else
        {
            var measureChildren = new List<Widget> { textField };
            measureChildren.AddRange(Current.DropdownMenuEntries.Select(entry =>
                new Padding(new Thickness(16, 0), BuildEntryContent(entry, false))));
            if (Current.Label is not null) measureChildren.Add(new Padding(new Thickness(4, 0), Current.Label));
            body = new DropdownMenuBody(Current.Width, measureChildren);
        }

        return new Align(
            alignment: Alignment.TopLeft,
            widthFactor: 1,
            heightFactor: 1,
            child: body);
    }

    private Widget? BuildSuffixIcon()
    {
        if (!Current.ShowTrailingIcon) return null;
        return new IconButton(
            focusNode: _trailingFocusNode,
            isSelected: _menuController?.IsOpen == true,
            icon: Current.TrailingIcon ?? new Icon(Icons.ArrowDropDown),
            selectedIcon: Current.SelectedTrailingIcon ?? new Icon(Icons.ArrowDropUp),
            onPressed: Current.Enabled ? ToggleMenu : null);
    }

    private void ToggleMenu()
    {
        if (_route is null) OpenMenu();
        else CloseMenu();
    }

    private void OpenMenu()
    {
        if (!Mounted || !Current.Enabled || _route is not null) return;
        if (Context.FindRenderObject() is not RenderBox anchor || !anchor.HasSize) return;

        _filteredEntries = Current.DropdownMenuEntries;
        if (!string.IsNullOrEmpty(_controller?.Text)) _filterActive = false;
        RecomputeEntries();

        var bounds = ResolveGlobalBounds(anchor);
        bounds = new Rect(
            bounds.X + Current.AlignmentOffset.X,
            bounds.Y + Current.AlignmentOffset.Y,
            bounds.Width,
            bounds.Height);
        var style = ResolveMenuStyle();
        var states = MaterialState.None;
        var min = style.MinimumSize?.Resolve(states);
        var fixedSize = style.FixedSize?.Resolve(states);
        var max = style.MaximumSize?.Resolve(states);
        double desiredWidth = Current.Width ?? bounds.Width;
        if (fixedSize is { } fixedValue && double.IsFinite(fixedValue.Width)) desiredWidth = fixedValue.Width;
        desiredWidth = Math.Max(min?.Width ?? 112, desiredWidth);
        if (max is { } maximum && double.IsFinite(maximum.Width)) desiredWidth = Math.Min(desiredWidth, maximum.Width);
        double? maxHeight = Current.MenuHeight;
        if (!maxHeight.HasValue && max is { } maximumSize && double.IsFinite(maximumSize.Height))
            maxHeight = maximumSize.Height;
        var shape = style.Shape?.Resolve(states);
        var side = style.Side?.Resolve(states);
        var menuPadding = style.Padding?.Resolve(states);
        var routeItems = BuildRouteItems();
        int selectedIndex = _currentHighlight ?? FirstEnabledIndex(_filteredEntries);
        _route = new DropdownRoute<T>(
            context: Context,
            items: routeItems,
            buttonRect: bounds,
            selectedIndex: selectedIndex < 0 ? 0 : selectedIndex,
            elevation: (int)Math.Round(style.Elevation?.Resolve(states) ?? 3),
            style: Current.TextStyle ?? DropdownMenuTheme.Of(Context).TextStyle ?? Theme.Of(Context).TextTheme.BodyLarge,
            itemHeight: null,
            menuWidth: desiredWidth,
            dropdownColor: style.BackgroundColor?.Resolve(states),
            menuMaxHeight: maxHeight,
            enableFeedback: true,
            borderRadius: shape?.BorderRadius,
            barrierDismissible: true,
            mouseCursor: style.MouseCursor?.Resolve(states),
            requestFocus: false,
            closeOnSelect: Current.CloseBehavior != DropdownMenuCloseBehavior.None,
            menuBelowAnchor: true,
            shadowColor: style.ShadowColor?.Resolve(states),
            side: side,
            menuPadding: menuPadding);
        Navigator.Of(Context).Push(_route);
        _menuController!.SetOpen(true);
        if (CanRequestFocus(Theme.Of(Context))) _focusNode?.RequestFocus();
        SetState(() => { });
        _ = AwaitRoute(_route);
    }

    private async Task AwaitRoute(DropdownRoute<T> route)
    {
        await route.Completed;
        if (!Mounted || !ReferenceEquals(route, _route)) return;
        SetState(() => _route = null);
        _menuController?.SetOpen(false);
    }

    private void CloseMenu()
    {
        _menuController?.SetOpen(false);
        _route?.Navigator?.MaybePop();
    }

    private void HandleTextChanged(string text)
    {
        _filteredEntries = Current.DropdownMenuEntries;
        _filterActive = Current.EnableFilter;
        _searchActive = Current.EnableSearch;
        RecomputeEntries();
        if (_route is null) OpenMenu();
        else UpdateRouteItems();
    }

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
    {
        if (!@event.IsDown || !Current.Enabled) return KeyEventResult.Ignored;
        if (@event.Key is "Escape" or "Esc")
        {
            if (_route is null) return KeyEventResult.Ignored;
            CloseMenu();
            return KeyEventResult.Handled;
        }
        if (@event.Key is "ArrowDown" or "Down")
        {
            if (_route is null) return KeyEventResult.Ignored;
            MoveHighlight(1);
            return KeyEventResult.Handled;
        }
        if (@event.Key is "ArrowUp" or "Up")
        {
            if (_route is null) return KeyEventResult.Ignored;
            MoveHighlight(-1);
            return KeyEventResult.Handled;
        }
        if (@event.Key is "Enter" or "Return" or "NumPadEnter" or "NumpadEnter")
        {
            if (Current.SelectOnly && _route is null) OpenMenu();
            else HandleSubmitted();
            return KeyEventResult.Handled;
        }
        return KeyEventResult.Ignored;
    }

    private void MoveHighlight(int delta)
    {
        if (!_filteredEntries.Any(entry => entry.Enabled)) return;
        _filterActive = false;
        _searchActive = false;
        int next = _currentHighlight ?? (delta > 0 ? -1 : 0);
        for (int count = 0; count < _filteredEntries.Count; count++)
        {
            next = (next + delta + _filteredEntries.Count) % _filteredEntries.Count;
            if (!_filteredEntries[next].Enabled) continue;
            _currentHighlight = next;
            SetControllerText(_filteredEntries[next].Label);
            SetState(() => { });
            UpdateRouteItems();
            return;
        }
    }

    private void HandleSubmitted()
    {
        if (_route is null) return;
        if (_currentHighlight is { } index && index >= 0 && index < _filteredEntries.Count
            && _filteredEntries[index].Enabled)
        {
            SelectEntry(_filteredEntries[index], index);
        }
        else
        {
            Current.OnSelected?.Invoke(default);
        }
        if (Current.CloseBehavior != DropdownMenuCloseBehavior.None) CloseMenu();
    }

    private void SelectEntry(DropdownMenuEntry<T> entry, int index)
    {
        if (!entry.Enabled) return;
        SetControllerText(entry.Label);
        _currentHighlight = Current.EnableSearch ? index : null;
        _filterActive = false;
        Current.OnSelected?.Invoke(entry.Value);
        if (Mounted) SetState(() => { });
    }

    private void RecomputeEntries()
    {
        var entries = Current.DropdownMenuEntries;
        if (_filterActive)
        {
            _filteredEntries = Current.FilterCallback?.Invoke(entries, _controller?.Text ?? string.Empty)
                               ?? entries.Where(entry => entry.Label.Contains(
                                   _controller?.Text ?? string.Empty,
                                   StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        else
        {
            _filteredEntries = entries;
        }

        if (_searchActive)
        {
            _currentHighlight = Current.SearchCallback?.Invoke(_filteredEntries, _controller?.Text ?? string.Empty)
                                ?? DefaultSearch(_filteredEntries, _controller?.Text ?? string.Empty);
        }
        if (_currentHighlight.HasValue
            && (_currentHighlight.Value < 0 || _currentHighlight.Value >= _filteredEntries.Count))
            _currentHighlight = null;
    }

    private static int? DefaultSearch(IReadOnlyList<DropdownMenuEntry<T>> entries, string query)
    {
        if (string.IsNullOrEmpty(query)) return null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Label.Contains(query, StringComparison.OrdinalIgnoreCase)) return i;
        return null;
    }

    private IReadOnlyList<DropdownMenuItem<T>> BuildRouteItems()
    {
        var result = new List<DropdownMenuItem<T>>(_filteredEntries.Count);
        for (int i = 0; i < _filteredEntries.Count; i++)
        {
            int index = i;
            var entry = _filteredEntries[index];
            result.Add(new DropdownMenuItem<T>(
                child: BuildEntryContent(entry, _currentHighlight == index),
                value: entry.Value,
                enabled: entry.Enabled && Current.Enabled,
                onTap: () => SelectEntry(entry, index)));
        }
        return result;
    }

    private Widget BuildEntryContent(DropdownMenuEntry<T> entry, bool highlighted)
    {
        var theme = Theme.Of(Context);
        var states = entry.Enabled ? MaterialState.None : MaterialState.Disabled;
        if (highlighted) states |= MaterialState.Focused;
        var textStyle = entry.Style?.TextStyle?.Resolve(states)
                        ?? (Current.TextStyle ?? DropdownMenuTheme.Of(Context).TextStyle ?? theme.TextTheme.LabelLarge);
        var foreground = entry.Style?.ForegroundColor?.Resolve(states)
                         ?? (entry.Enabled ? theme.OnSurfaceColor : theme.DisabledColor);
        var iconColor = entry.Style?.IconColor?.Resolve(states) ?? foreground;
        var background = entry.Style?.BackgroundColor?.Resolve(states)
                         ?? (highlighted ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12) : null);
        var children = new List<Widget>();
        if (entry.LeadingIcon is not null)
        {
            children.Add(new IconTheme(new IconThemeData(Color: iconColor), entry.LeadingIcon));
            children.Add(new SizedBox(width: 12));
        }
        children.Add(entry.LabelWidget ?? new Text(entry.Label, maxLines: 1, overflow: TextOverflow.Ellipsis));
        if (entry.TrailingIcon is not null)
        {
            children.Add(new SizedBox(width: 12));
            children.Add(new IconTheme(new IconThemeData(Color: iconColor), entry.TrailingIcon));
        }
        Widget content = new DefaultTextStyle(
            textStyle.CopyWith(color: foreground),
            new Row(mainAxisSize: MainAxisSize.Min, children: children));
        if (background.HasValue) content = new ColoredBox(background.Value, content);
        return content;
    }

    private void UpdateRouteItems(bool notify = true)
    {
        if (_route is null) return;
        int selected = _currentHighlight ?? FirstEnabledIndex(_filteredEntries);
        _route.UpdateItems(BuildRouteItems(), selected < 0 ? 0 : selected, notify);
    }

    private MenuStyle ResolveMenuStyle()
    {
        var local = DropdownMenuTheme.Of(Context).MenuStyle;
        var defaults = DropdownMenuTheme.Defaults(Context).MenuStyle!;
        var widget = Current.MenuStyle;
        return new MenuStyle(
            BackgroundColor: widget?.BackgroundColor ?? local?.BackgroundColor ?? defaults.BackgroundColor,
            ShadowColor: widget?.ShadowColor ?? local?.ShadowColor ?? defaults.ShadowColor,
            SurfaceTintColor: widget?.SurfaceTintColor ?? local?.SurfaceTintColor ?? defaults.SurfaceTintColor,
            Elevation: widget?.Elevation ?? local?.Elevation ?? defaults.Elevation,
            Padding: widget?.Padding ?? local?.Padding ?? defaults.Padding,
            MinimumSize: widget?.MinimumSize ?? local?.MinimumSize ?? defaults.MinimumSize,
            FixedSize: widget?.FixedSize ?? local?.FixedSize ?? defaults.FixedSize,
            MaximumSize: widget?.MaximumSize ?? local?.MaximumSize ?? defaults.MaximumSize,
            Side: widget?.Side ?? local?.Side ?? defaults.Side,
            Shape: widget?.Shape ?? local?.Shape ?? defaults.Shape,
            MouseCursor: widget?.MouseCursor ?? local?.MouseCursor ?? defaults.MouseCursor,
            Alignment: widget?.Alignment ?? local?.Alignment ?? defaults.Alignment,
            VisualDensity: widget?.VisualDensity ?? local?.VisualDensity ?? defaults.VisualDensity);
    }

    private bool CanRequestFocus(ThemeData theme) => Current.FocusNode?.CanRequestFocus
        ?? Current.RequestFocusOnTap
        ?? theme.Platform is not (TargetPlatform.Android or TargetPlatform.Fuchsia or TargetPlatform.IOS);

    private void ApplyInitialSelection()
    {
        var entry = Current.DropdownMenuEntries.FirstOrDefault(item =>
            EqualityComparer<T?>.Default.Equals(item.Value, Current.InitialSelection));
        if (entry is not null) SetControllerText(entry.Label);
    }

    private void SetControllerText(string text)
    {
        if (_controller is null || string.Equals(_controller.Text, text, StringComparison.Ordinal)) return;
        _suppressControllerChange = true;
        _controller.Text = text;
        _suppressControllerChange = false;
    }

    private void AttachController(TextEditingController? external, string initialText = "")
    {
        _controller = external ?? new TextEditingController(initialText);
        _ownsController = external is null;
        _controller.AddListener(HandleControllerChanged);
    }

    private void DetachController()
    {
        if (_controller is null) return;
        _controller.RemoveListener(HandleControllerChanged);
        if (_ownsController) _controller.Dispose();
        _controller = null;
        _ownsController = false;
    }

    private void HandleControllerChanged()
    {
        if (_suppressControllerChange || !Mounted) return;
        SetState(() =>
        {
            _filteredEntries = Current.DropdownMenuEntries;
            _searchActive = Current.EnableSearch;
            RecomputeEntries();
        });
        UpdateRouteItems();
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

    private void AttachTrailingFocusNode(FocusNode? external)
    {
        _trailingFocusNode = external ?? new FocusNode();
        _ownsTrailingFocusNode = external is null;
    }

    private void DetachTrailingFocusNode()
    {
        if (_ownsTrailingFocusNode) _trailingFocusNode?.Dispose();
        _trailingFocusNode = null;
        _ownsTrailingFocusNode = false;
    }

    private void AttachMenuController(MenuController? external)
    {
        _menuController = external ?? new MenuController();
        _ownsMenuController = external is null;
        _menuController.Attach(this, OpenMenu, CloseMenu);
    }

    private void DetachMenuController()
    {
        _menuController?.Detach(this);
        if (_ownsMenuController) _menuController?.Dispose();
        _menuController = null;
        _ownsMenuController = false;
    }

    private static int FirstEnabledIndex(IReadOnlyList<DropdownMenuEntry<T>> entries)
    {
        for (int i = 0; i < entries.Count; i++) if (entries[i].Enabled) return i;
        return -1;
    }

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
        double left = points.Min(point => point.X);
        double top = points.Min(point => point.Y);
        double right = points.Max(point => point.X);
        double bottom = points.Max(point => point.Y);
        return new Rect(left, top, right - left, bottom - top);
    }
}

internal sealed class DropdownMenuBody : MultiChildRenderObjectWidget
{
    public DropdownMenuBody(double? width, IReadOnlyList<Widget> children) : base(children)
    {
        Width = width;
    }

    public double? Width { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderDropdownMenuBody(Width);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject) =>
        ((RenderDropdownMenuBody)renderObject).Width = Width;
}

internal sealed class DropdownMenuBodyParentData : ContainerBoxParentData<RenderBox>
{
}

internal sealed class RenderDropdownMenuBody : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, DropdownMenuBodyParentData>, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, DropdownMenuBodyParentData> _children;
    private double? _width;

    public RenderDropdownMenuBody(double? width)
    {
        _width = width;
        _children = new RenderBoxContainerDefaultsMixin<RenderBox, DropdownMenuBodyParentData>(this);
    }

    public double? Width
    {
        get => _width;
        set { if (_width != value) { _width = value; MarkNeedsLayout(); } }
    }

    public int ChildCount => _children.ChildCount;
    public RenderBox? FirstChild => _children.FirstChild;
    public RenderBox? LastChild => _children.LastChild;
    public RenderBox? ChildBefore(RenderBox child) => _children.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _children.ChildAfter(child);
    public void AddAll(List<RenderBox> children) => _children.AddAll(children);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not DropdownMenuBodyParentData) child.parentData = new DropdownMenuBodyParentData();
    }

    protected override void PerformLayout()
    {
        if (FirstChild is null)
        {
            Size = Constraints.Constrain(new Size(Math.Max(112, Width ?? 112), 0));
            return;
        }
        var loose = new BoxConstraints(
            MaxWidth: Constraints.HasBoundedWidth ? Constraints.MaxWidth : double.PositiveInfinity,
            MaxHeight: Constraints.HasBoundedHeight ? Constraints.MaxHeight : double.PositiveInfinity);
        double measuredWidth = 112.0;
        for (var child = ChildAfter(FirstChild); child is not null; child = ChildAfter(child))
        {
            child.Layout(loose, parentUsesSize: true);
            measuredWidth = Math.Max(measuredWidth, child.Size.Width);
            ((DropdownMenuBodyParentData)child.parentData!).offset = default;
        }
        double width = Width ?? measuredWidth;
        if (Constraints.HasBoundedWidth) width = Math.Min(width, Constraints.MaxWidth);
        width = Math.Max(0, width);
        FirstChild.Layout(new BoxConstraints(MinWidth: width, MaxWidth: width, MaxHeight: loose.MaxHeight), parentUsesSize: true);
        ((DropdownMenuBodyParentData)FirstChild.parentData!).offset = default;
        Size = Constraints.Constrain(new Size(width, FirstChild.Size.Height));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (FirstChild is not null) context.PaintChild(FirstChild, offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        FirstChild?.HitTest(result, position) == true;

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (FirstChild is not null) visitor(FirstChild, default, Matrix.Identity);
    }

    public void DefaultPaint(PaintingContext ctx, Point offset) => _children.DefaultPaint(ctx, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) => _children.DefaultHitTestChildren(result, position);
    public void Insert(RenderBox child, RenderBox? after = null) => _children.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _children.Move(child, after);
    public void Remove(RenderBox child) => _children.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}
