using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dropdown_menu.dart

public delegate IReadOnlyList<DropdownMenuEntry<T>> DropdownMenuFilterCallback<T>(
    IReadOnlyList<DropdownMenuEntry<T>> entries,
    string filter);

public delegate int? DropdownMenuSearchCallback<T>(
    IReadOnlyList<DropdownMenuEntry<T>> entries,
    string query);

public delegate InputDecoration DropdownMenuDecorationBuilder(
    BuildContext context,
    MenuController controller);

/// <summary>Defines a <see cref="DropdownMenu{T}"/> menu entry.</summary>
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

/// <summary>Defines which menus a <see cref="DropdownMenu{T}"/> selection closes.</summary>
public enum DropdownMenuCloseBehavior
{
    /// <summary>Closes every open menu in the widget tree.</summary>
    All,

    /// <summary>Closes only this dropdown menu.</summary>
    Self,

    /// <summary>Closes nothing.</summary>
    None,
}

internal sealed class DropdownMenuArrowUpIntent : Intent;

internal sealed class DropdownMenuArrowDownIntent : Intent;

internal sealed class DropdownMenuEnterIntent : Intent;

/// <summary>
/// A dropdown menu that can be opened from a <see cref="TextField"/>. The selected menu item is
/// displayed in that field, which allows the user to filter and search the entries.
/// </summary>
public sealed class DropdownMenu<T> : StatefulWidget
{
    internal const double MinimumWidth = 112.0;
    internal const double DefaultHorizontalPadding = 12.0;
    internal const double InputStartGap = 4.0;

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
        TextInputType? keyboardType = null,
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
        EdgeInsetsGeometry? expandedInsets = null,
        DropdownMenuFilterCallback<T>? filterCallback = null,
        DropdownMenuSearchCallback<T>? searchCallback = null,
        Vector? alignmentOffset = null,
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        DropdownMenuCloseBehavior closeBehavior = DropdownMenuCloseBehavior.All,
        int? maxLines = 1,
        TextInputAction? textInputAction = null,
        double? cursorHeight = null,
        string? restorationId = null,
        MenuController? menuController = null,
        Thickness? scrollPadding = null,
        Key? key = null) : base(key)
    {
        if (dropdownMenuEntries is null) throw new ArgumentNullException(nameof(dropdownMenuEntries));
        if (filterCallback is not null && !enableFilter)
            throw new ArgumentException("filterCallback requires enableFilter=true.", nameof(filterCallback));
        if (trailingIconFocusNode is not null && !showTrailingIcon)
        {
            throw new ArgumentException(
                "trailingIconFocusNode requires showTrailingIcon=true.",
                nameof(trailingIconFocusNode));
        }
        if (decorationBuilder is not null
            && (label is not null || hintText is not null || helperText is not null || errorText is not null))
        {
            throw new ArgumentException(
                "label/hintText/helperText/errorText must be supplied by decorationBuilder when it is set.",
                nameof(decorationBuilder));
        }

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
        KeyboardType = keyboardType;
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
        InputFormatters = inputFormatters;
        CloseBehavior = closeBehavior;
        MaxLines = maxLines;
        TextInputAction = textInputAction;
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
    public TextInputType? KeyboardType { get; }
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
    public EdgeInsetsGeometry? ExpandedInsets { get; }
    public DropdownMenuFilterCallback<T>? FilterCallback { get; }
    public DropdownMenuSearchCallback<T>? SearchCallback { get; }
    public Vector AlignmentOffset { get; }
    public IReadOnlyList<TextInputFormatter>? InputFormatters { get; }
    public DropdownMenuCloseBehavior CloseBehavior { get; }
    public int? MaxLines { get; }
    public TextInputAction? TextInputAction { get; }
    public double? CursorHeight { get; }
    public string? RestorationId { get; }
    public MenuController? MenuController { get; }
    public Thickness ScrollPadding { get; }

    public override State CreateState() => new DropdownMenuState<T>();
}

public sealed class DropdownMenuState<T> : State
{
    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> EditableShortcuts =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] =
                new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: true),
            [new SingleActivator(LogicalKeyboardKey.ArrowRight)] =
                new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true),
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DropdownMenuArrowUpIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new DropdownMenuArrowDownIntent(),
        };

    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> SelectOnlyShortcuts =
        new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DropdownMenuArrowUpIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new DropdownMenuArrowDownIntent(),
            [new SingleActivator(LogicalKeyboardKey.Enter)] = new DropdownMenuEnterIntent(),
        };

    private readonly GlobalKey _anchorKey = new GlobalObjectKey<State>(new object());
    private readonly GlobalKey _leadingKey = new GlobalObjectKey<State>(new object());
    private readonly FocusNode _internalFocusNode = new();
    private List<GlobalKey> _buttonItemKeys = [];
    private MenuController _controller = null!;
    private bool _enableFilter;
    private bool _enableSearch;
    private IReadOnlyList<DropdownMenuEntry<T>> _filteredEntries = [];
    private IReadOnlyList<Widget>? _initialMenu;
    private int? _currentHighlight;
    private double? _leadingPadding;
    private bool _menuHasEnabledItem;
    private TextEditingController? _localTextEditingController;
    private MaterialStatesController? _highlightedItemStatesController;
    private FocusNode? _localTrailingIconButtonFocusNode;

    private DropdownMenu<T> Current => (DropdownMenu<T>)StateWidget;

    private TextEditingController EffectiveTextEditingController =>
        Current.Controller ?? (_localTextEditingController ??= new TextEditingController());

    private FocusNode TrailingIconButtonFocusNode =>
        Current.TrailingIconFocusNode ?? (_localTrailingIconButtonFocusNode ??= new FocusNode());

    /// <summary>The highlighted entry index; exposed for parity tests only.</summary>
    internal int? DebugCurrentHighlight => _currentHighlight;

    /// <summary>The entries the menu is currently showing; exposed for parity tests only.</summary>
    internal IReadOnlyList<DropdownMenuEntry<T>> DebugFilteredEntries => _filteredEntries;

    /// <summary>Flutter's `_DropdownMenuState.selectOnly`.</summary>
    private bool SelectOnly => Current.SelectOnly;

    /// <summary>Flutter's `_DropdownMenuState.isButton`.</summary>
    private bool IsButton => !CanRequestFocus() || SelectOnly;

    public override void InitState()
    {
        _enableSearch = Current.EnableSearch;
        _filteredEntries = Current.DropdownMenuEntries;
        _buttonItemKeys = CreateButtonItemKeys(_filteredEntries.Count);
        _menuHasEnabledItem = _filteredEntries.Any(entry => entry.Enabled);

        int index = IndexOfValue(_filteredEntries, Current.InitialSelection);
        if (index != -1)
        {
            string label = _filteredEntries[index].Label;
            EffectiveTextEditingController.Value = new TextEditingValue(label, TextSelection.Collapsed(label.Length));
        }

        RefreshLeadingPadding();
        _controller = Current.MenuController ?? new MenuController();
    }

    public override void Dispose()
    {
        _localTextEditingController?.Dispose();
        _localTextEditingController = null;
        _internalFocusNode.Dispose();
        _localTrailingIconButtonFocusNode?.Dispose();
        _localTrailingIconButtonFocusNode = null;
        _highlightedItemStatesController?.Dispose();
        base.Dispose();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (DropdownMenu<T>)oldWidget;
        if (!ReferenceEquals(old.Controller, Current.Controller))
        {
            _localTextEditingController?.Dispose();
            _localTextEditingController = null;
        }

        if (old.EnableFilter != Current.EnableFilter && !Current.EnableFilter)
        {
            _enableFilter = false;
        }

        if (old.EnableSearch != Current.EnableSearch && !Current.EnableSearch)
        {
            _enableSearch = Current.EnableSearch;
            _currentHighlight = null;
        }

        if (!ReferenceEquals(old.DropdownMenuEntries, Current.DropdownMenuEntries))
        {
            _currentHighlight = null;
            _filteredEntries = Current.DropdownMenuEntries;
            _buttonItemKeys = CreateButtonItemKeys(_filteredEntries.Count);
            _menuHasEnabledItem = _filteredEntries.Any(entry => entry.Enabled);
        }

        if (!ReferenceEquals(old.LeadingIcon, Current.LeadingIcon))
        {
            RefreshLeadingPadding();
        }

        if (!EqualityComparer<T?>.Default.Equals(old.InitialSelection, Current.InitialSelection))
        {
            int index = IndexOfValue(_filteredEntries, Current.InitialSelection);
            if (index != -1)
            {
                string label = _filteredEntries[index].Label;
                EffectiveTextEditingController.Value =
                    new TextEditingValue(label, TextSelection.Collapsed(label.Length));
            }
        }

        if (!ReferenceEquals(old.MenuController, Current.MenuController))
        {
            _controller = Current.MenuController ?? new MenuController();
        }
    }

    /// <summary>Flutter's `_DropdownMenuState.canRequestFocus`.</summary>
    private bool CanRequestFocus()
    {
        if (Current.FocusNode is { } node) return node.CanRequestFocus;
        if (Current.RequestFocusOnTap is { } requested) return requested;
        return Theme.Of(Context).Platform switch
        {
            TargetPlatform.IOS or TargetPlatform.Android or TargetPlatform.Fuchsia => false,
            _ => true,
        };
    }

    private static List<GlobalKey> CreateButtonItemKeys(int count)
    {
        var keys = new List<GlobalKey>(count);
        for (int i = 0; i < count; i++) keys.Add(new GlobalObjectKey<State>(new object()));
        return keys;
    }

    private static int IndexOfValue(IReadOnlyList<DropdownMenuEntry<T>> entries, T? value)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (EqualityComparer<T?>.Default.Equals(entries[i].Value, value)) return i;
        }

        return -1;
    }

    private static double? GetWidth(GlobalKey key)
    {
        if (key.CurrentContext is not { } context) return null;
        return context.FindRenderObject() is RenderBox { HasSize: true } box ? box.Size.Width : null;
    }

    private void RefreshLeadingPadding()
    {
        Scheduler.AddPostFrameCallback(timestamp =>
        {
            if (!Mounted) return;
            SetState(() => _leadingPadding = GetWidth(_leadingKey));
        });
    }

    private void ScrollToHighlight()
    {
        Scheduler.AddPostFrameCallback(timestamp =>
        {
            if (_currentHighlight is not { } highlight
                || highlight < 0
                || highlight >= _buttonItemKeys.Count)
            {
                return;
            }

            if (_buttonItemKeys[highlight].CurrentContext is not { } highlightContext) return;
            if (highlightContext.FindRenderObject() is not { } renderObject) return;
            // Dart uses `Scrollable.of(context).position.ensureVisible(...)`, i.e. the *nearest*
            // scrollable only, so an ancestor list never scrolls along with the menu.
            _ = Scrollable.MaybeOf(highlightContext)?.Position.EnsureVisible(renderObject);
        });
    }

    private static IReadOnlyList<DropdownMenuEntry<T>> Filter(
        IReadOnlyList<DropdownMenuEntry<T>> entries,
        TextEditingController controller)
    {
        string filterText = controller.Text.ToLowerInvariant();
        return entries.Where(entry => entry.Label.ToLowerInvariant().Contains(filterText, StringComparison.Ordinal))
            .ToList();
    }

    private bool ShouldUpdateCurrentHighlight(IReadOnlyList<DropdownMenuEntry<T>> entries)
    {
        string searchText = EffectiveTextEditingController.Value.Text.ToLowerInvariant();
        if (searchText.Length == 0) return true;
        if (_currentHighlight is not { } highlight || highlight >= entries.Count) return true;
        // Keep the current highlight when it still matches the search text.
        return !entries[highlight].Label.ToLowerInvariant().Contains(searchText, StringComparison.Ordinal);
    }

    private static int? Search(IReadOnlyList<DropdownMenuEntry<T>> entries, TextEditingController controller)
    {
        string searchText = controller.Value.Text.ToLowerInvariant();
        if (searchText.Length == 0) return null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Label.ToLowerInvariant().Contains(searchText, StringComparison.Ordinal)) return i;
        }

        return null;
    }

    private List<Widget> BuildButtons(
        IReadOnlyList<DropdownMenuEntry<T>> filteredEntries,
        TextDirection textDirection,
        int? focusedIndex = null,
        bool enableScrollToHighlight = true,
        bool excludeSemantics = false,
        bool useMaterial3 = true)
    {
        var result = new List<Widget>(filteredEntries.Count);
        double effectiveInputStartGap = useMaterial3 ? DropdownMenu<T>.InputStartGap : 0.0;
        for (int i = 0; i < filteredEntries.Count; i++)
        {
            var entry = filteredEntries[i];
            int index = i;

            // The leading padding is the width of the field's leading icon, so the entry labels line
            // up with the field text; entries with their own leading icon keep the default padding.
            double padding = entry.LeadingIcon is null
                ? _leadingPadding ?? DropdownMenu<T>.DefaultHorizontalPadding
                : DropdownMenu<T>.DefaultHorizontalPadding;
            // `ButtonStyle.Padding` is a resolved `Thickness`, so the directional inset Dart writes
            // as `EdgeInsetsDirectional.only(start: padding, end: 12)` is resolved here instead.
            var itemPadding = EdgeInsetsGeometry
                .DirectionalOnly(start: padding, end: DropdownMenu<T>.DefaultHorizontalPadding)
                .Resolve(textDirection);
            var effectiveStyle = entry.Style
                                 ?? new ButtonStyle(Padding: MaterialStateProperty<Thickness?>.All(itemPadding));

            var themeStyle = MenuButtonTheme.Of(Context).Style;
            var effectiveForegroundColor = entry.Style?.ForegroundColor ?? themeStyle?.ForegroundColor;
            var effectiveIconColor = entry.Style?.IconColor ?? themeStyle?.IconColor;
            var effectiveOverlayColor = entry.Style?.OverlayColor ?? themeStyle?.OverlayColor;
            var effectiveBackgroundColor = entry.Style?.BackgroundColor ?? themeStyle?.BackgroundColor;

            bool entryIsSelected = entry.Enabled && index == focusedIndex;
            if (entryIsSelected)
            {
                // Dart recreates the controller so the highlighted item reports `focused` without
                // owning real focus (focus stays on the text field).
                _highlightedItemStatesController?.Dispose();
                _highlightedItemStatesController = new MaterialStatesController(MaterialState.Focused);

                var defaultStyle = new MenuItemButton().DefaultStyleOf(Context);
                Color focusedForegroundColor =
                    (effectiveForegroundColor ?? defaultStyle.ForegroundColor!).Resolve(MaterialState.Focused)!.Value;
                Color focusedIconColor =
                    (effectiveIconColor ?? defaultStyle.IconColor!).Resolve(MaterialState.Focused)!.Value;
                Color focusedOverlayColor =
                    (effectiveOverlayColor ?? defaultStyle.OverlayColor!).Resolve(MaterialState.Focused)!.Value;
                Color focusedBackgroundColor = effectiveBackgroundColor?.Resolve(MaterialState.Focused)
                                               ?? MaterialButtonCore.ApplyOpacity(
                                                   Theme.Of(Context).ColorScheme.OnSurface,
                                                   0.12);
                effectiveStyle = effectiveStyle with
                {
                    BackgroundColor = MaterialStateProperty<Color?>.All(focusedBackgroundColor),
                    ForegroundColor = MaterialStateProperty<Color?>.All(focusedForegroundColor),
                    IconColor = MaterialStateProperty<Color?>.All(focusedIconColor),
                    OverlayColor = MaterialStateProperty<Color?>.All(focusedOverlayColor),
                };
            }
            else
            {
                effectiveStyle = effectiveStyle with
                {
                    BackgroundColor = effectiveBackgroundColor ?? effectiveStyle.BackgroundColor,
                    ForegroundColor = effectiveForegroundColor ?? effectiveStyle.ForegroundColor,
                    IconColor = effectiveIconColor ?? effectiveStyle.IconColor,
                    OverlayColor = effectiveOverlayColor ?? effectiveStyle.OverlayColor,
                };
            }

            Widget label = entry.LabelWidget ?? new Text(entry.Label);
            if (Current.Width is { } menuWidth)
            {
                double horizontalPadding = padding + DropdownMenu<T>.DefaultHorizontalPadding + effectiveInputStartGap;
                label = new ConstrainedBox(
                    constraints: new BoxConstraints(MaxWidth: Math.Max(0.0, menuWidth - horizontalPadding)),
                    child: label);
            }

            Widget menuItemButton = new MenuItemButton(
                // A custom `filterCallback` may return more entries than the source list, so the key
                // list is only used while it covers the index.
                key: enableScrollToHighlight && index < _buttonItemKeys.Count ? _buttonItemKeys[index] : null,
                statesController: entryIsSelected ? _highlightedItemStatesController : null,
                style: effectiveStyle,
                leadingIcon: entry.LeadingIcon,
                trailingIcon: entry.TrailingIcon,
                closeOnActivate: Current.CloseBehavior == DropdownMenuCloseBehavior.All,
                onPressed: entry.Enabled && Current.Enabled ? () => HandleEntryPressed(entry, index) : null,
                requestFocusOnHover: false,
                child: new Padding(
                    insets: EdgeInsetsGeometry.DirectionalOnly(start: effectiveInputStartGap).Resolve(textDirection),
                    child: label));

            result.Add(new ExcludeFocus(
                child: new ExcludeSemantics(excluding: excludeSemantics, child: menuItemButton)));
        }

        return result;
    }

    private void HandleEntryPressed(DropdownMenuEntry<T> entry, int index)
    {
        if (!Mounted)
        {
            if (Current.Controller is { } external)
            {
                external.Value = new TextEditingValue(entry.Label, TextSelection.Collapsed(entry.Label.Length));
            }

            Current.OnSelected?.Invoke(entry.Value);
            return;
        }

        EffectiveTextEditingController.Value =
            new TextEditingValue(entry.Label, TextSelection.Collapsed(entry.Label.Length));
        _currentHighlight = Current.EnableSearch ? index : null;
        Current.OnSelected?.Invoke(entry.Value);
        _enableFilter = false;
        if (Current.CloseBehavior == DropdownMenuCloseBehavior.Self)
        {
            _controller.Close();
        }
    }

    private void HandleUpKey()
    {
        SetState(() =>
        {
            if (!Current.Enabled || !_menuHasEnabledItem || !_controller.IsOpen) return;
            MoveHighlight(-1);
        });
    }

    private void HandleDownKey()
    {
        SetState(() =>
        {
            if (!Current.Enabled || !_menuHasEnabledItem || !_controller.IsOpen) return;
            MoveHighlight(1);
        });
    }

    private void MoveHighlight(int delta)
    {
        _enableFilter = false;
        _enableSearch = false;
        int count = _filteredEntries.Count;
        if (count == 0) return;
        int highlight = _currentHighlight ?? (delta < 0 ? 0 : -1);
        highlight = Modulo(highlight + delta, count);
        int guard = 0;
        while (!_filteredEntries[highlight].Enabled && guard++ < count)
        {
            highlight = Modulo(highlight + delta, count);
        }

        _currentHighlight = highlight;
        string currentLabel = _filteredEntries[highlight].Label;
        EffectiveTextEditingController.Value =
            new TextEditingValue(currentLabel, TextSelection.Collapsed(currentLabel.Length));
    }

    /// <summary>Dart's `%` never returns a negative result; C#'s does, so it is normalized here.</summary>
    private static int Modulo(int value, int count) => ((value % count) + count) % count;

    private void HandleEnterKey()
    {
        if (SelectOnly && !_controller.IsOpen)
        {
            _controller.Open();
            return;
        }

        HandleSubmitted();
    }

    private void HandlePressed(MenuController controller, bool focusForKeyboard = true)
    {
        if (controller.IsOpen)
        {
            _currentHighlight = null;
            controller.Close();
        }
        else
        {
            _filteredEntries = Current.DropdownMenuEntries;
            if (EffectiveTextEditingController.Text.Length > 0) _enableFilter = false;
            controller.Open();
            if (focusForKeyboard) _internalFocusNode.RequestFocus();
        }

        SetState(() => { });
    }

    private void HandleSubmitted()
    {
        if (_currentHighlight is { } highlight && highlight >= 0 && highlight < _filteredEntries.Count)
        {
            var entry = _filteredEntries[highlight];
            if (entry.Enabled)
            {
                EffectiveTextEditingController.Value =
                    new TextEditingValue(entry.Label, TextSelection.Collapsed(entry.Label.Length));
                Current.OnSelected?.Invoke(entry.Value);
            }
        }
        else if (_controller.IsOpen)
        {
            Current.OnSelected?.Invoke(default);
        }

        if (!Current.EnableSearch) _currentHighlight = null;
        _controller.Close();
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        TextDirection textDirection = Directionality.Of(context);
        var dropdownMenuTheme = DropdownMenuTheme.Of(context);
        var defaults = DropdownMenuTheme.Defaults(context);
        double? anchorWidth = GetWidth(_anchorKey);

        if (_enableFilter)
        {
            _filteredEntries = Current.FilterCallback?.Invoke(_filteredEntries, EffectiveTextEditingController.Text)
                               ?? Filter(Current.DropdownMenuEntries, EffectiveTextEditingController);
        }

        _menuHasEnabledItem = _filteredEntries.Any(entry => entry.Enabled);

        if (_enableSearch)
        {
            if (Current.SearchCallback is { } searchCallback)
            {
                _currentHighlight = searchCallback(_filteredEntries, EffectiveTextEditingController.Text);
            }
            else if (ShouldUpdateCurrentHighlight(_filteredEntries))
            {
                _currentHighlight = Search(_filteredEntries, EffectiveTextEditingController);
            }

            if (_currentHighlight is not null) ScrollToHighlight();
        }

        List<Widget> menu = BuildButtons(
            _filteredEntries,
            textDirection,
            focusedIndex: _currentHighlight,
            useMaterial3: theme.UseMaterial3);
        _initialMenu ??= BuildButtons(
            Current.DropdownMenuEntries,
            textDirection,
            enableScrollToHighlight: false,
            excludeSemantics: true,
            useMaterial3: theme.UseMaterial3);

        var effectiveMenuStyle = Current.MenuStyle ?? dropdownMenuTheme.MenuStyle ?? defaults.MenuStyle!;
        if (Current.Width is { } requestedWidth)
        {
            effectiveMenuStyle = WithMinimumWidth(effectiveMenuStyle, requestedWidth);
        }
        else if (anchorWidth is { } resolvedAnchorWidth)
        {
            effectiveMenuStyle = WithMinimumWidth(effectiveMenuStyle, resolvedAnchorWidth);
        }

        if (Current.MenuHeight is { } menuHeight)
        {
            effectiveMenuStyle = effectiveMenuStyle.CopyWith(
                maximumSize: MaterialStateProperty<Size?>.All(new Size(double.PositiveInfinity, menuHeight)));
        }

        var baseTextStyle = Current.TextStyle ?? dropdownMenuTheme.TextStyle ?? defaults.TextStyle;
        Color? disabledColor = dropdownMenuTheme.DisabledColor ?? defaults.DisabledColor;
        var effectiveTextStyle = Current.Enabled
            ? baseTextStyle
            : baseTextStyle?.CopyWith(color: disabledColor) ?? new TextStyle(Color: disabledColor);
        var effectiveInputDecorationTheme = Current.InputDecorationTheme
                                            ?? dropdownMenuTheme.InputDecorationTheme
                                            ?? defaults.InputDecorationTheme!;

        Widget menuAnchor = new MenuAnchor(
            style: effectiveMenuStyle,
            alignmentOffset: Current.AlignmentOffset,
            reservedPadding: EdgeInsetsGeometry.Zero,
            controller: _controller,
            menuChildren: menu,
            crossAxisUnconstrained: false,
            builder: (anchorContext, controller, _) =>
                BuildAnchorChild(anchorContext, controller, effectiveTextStyle, effectiveInputDecorationTheme));

        if (Current.ExpandedInsets is { } expandedInsets)
        {
            // Clamping to zero vertically is what makes `expandedInsets`' top/bottom no-ops.
            var clamped = expandedInsets.Clamp(
                EdgeInsetsGeometry.Zero,
                EdgeInsetsGeometry.Only(left: double.PositiveInfinity, right: double.PositiveInfinity)
                    .Add(EdgeInsetsGeometry.DirectionalOnly(
                        start: double.PositiveInfinity,
                        end: double.PositiveInfinity)));
            menuAnchor = new Padding(insets: clamped, child: menuAnchor);
        }

        menuAnchor = new Align(
            alignment: AlignmentDirectional.TopStart,
            widthFactor: 1.0,
            heightFactor: 1.0,
            child: menuAnchor);

        return new Actions(
            actions: new Dictionary<Type, FlutterAction>
            {
                [typeof(DropdownMenuArrowUpIntent)] =
                    new CallbackAction<DropdownMenuArrowUpIntent>(_ => { HandleUpKey(); return null; }),
                [typeof(DropdownMenuArrowDownIntent)] =
                    new CallbackAction<DropdownMenuArrowDownIntent>(_ => { HandleDownKey(); return null; }),
                [typeof(DropdownMenuEnterIntent)] =
                    new CallbackAction<DropdownMenuEnterIntent>(_ => { HandleEnterKey(); return null; }),
                [typeof(DismissIntent)] = new DismissMenuAction(_controller),
            },
            child: new Stack(children:
            [
                new Shortcuts(
                    shortcuts: new Dictionary<ShortcutActivator, Intent>
                    {
                        [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DropdownMenuArrowUpIntent(),
                        [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new DropdownMenuArrowDownIntent(),
                        [new SingleActivator(LogicalKeyboardKey.Enter)] = new DropdownMenuEnterIntent(),
                        [new SingleActivator(LogicalKeyboardKey.Escape)] = new DismissIntent(),
                    },
                    child: new Focus(
                        focusNode: _internalFocusNode,
                        skipTraversal: true,
                        child: new SizedBox(width: 0, height: 0))),
                menuAnchor,
            ]));
    }

    private static MenuStyle WithMinimumWidth(MenuStyle style, double width)
    {
        MenuStyle? mutated = null;
        mutated = style.CopyWith(minimumSize: MaterialStateProperty<Size?>.ResolveWith(states =>
        {
            double? maxWidth = mutated!.MaximumSize?.Resolve(states)?.Width;
            return new Size(Math.Min(width, maxWidth ?? width), 0.0);
        }));
        return mutated;
    }

    private Widget BuildAnchorChild(
        BuildContext context,
        MenuController controller,
        TextStyle? effectiveTextStyle,
        InputDecorationThemeData effectiveInputDecorationTheme)
    {
        bool isButton = IsButton;
        var decorationBuilder = Current.DecorationBuilder ?? BuildDefaultDecoration;
        var decoration = decorationBuilder(context, controller);
        if (decoration.SuffixIcon is null)
        {
            decoration = decoration with { SuffixIcon = BuildDefaultSuffixIcon(controller) };
        }

        var effectiveDecoration = decoration.ApplyDefaults(effectiveInputDecorationTheme);
        var textFieldDecoration = effectiveDecoration.PrefixIcon is null
            ? effectiveDecoration
            : effectiveDecoration with
            {
                PrefixIcon = new SizedBox(key: _leadingKey, child: effectiveDecoration.PrefixIcon),
            };
        var localizations = MaterialLocalizations.Of(context);

        Widget textField = new Semantics(
            flags: isButton ? SemanticsFlags.IsButton : SemanticsFlags.None,
            hint: Theme.Of(context).Platform == TargetPlatform.IOS
                ? controller.IsOpen ? localizations.CollapsedHint : localizations.ExpandedHint
                : null,
            expanded: controller.IsOpen,
            onExpand: controller.IsOpen ? null : () => controller.Open(),
            onCollapse: !controller.IsOpen ? null : () => controller.Close(),
            child: new ExcludeSemantics(
                excluding: isButton && PlatformDefaults.IsWeb,
                child: new TextField(
                    key: _anchorKey,
                    enabled: Current.Enabled,
                    mouseCursor: Current.Enabled
                        ? isButton ? SystemMouseCursors.Click : SystemMouseCursors.Text
                        : null,
                    focusNode: Current.FocusNode,
                    canRequestFocus: CanRequestFocus(),
                    enableInteractiveSelection: !isButton,
                    readOnly: isButton,
                    keyboardType: Current.KeyboardType,
                    textAlign: Current.TextAlign,
                    textAlignVertical: Plumix.Rendering.TextAlignVertical.Center,
                    maxLines: Current.MaxLines,
                    textInputAction: Current.TextInputAction,
                    cursorHeight: Current.CursorHeight,
                    style: effectiveTextStyle,
                    controller: EffectiveTextEditingController,
                    // Dart routes Enter through `_EnterIntent`; Plumix's `EditableText` consumes the
                    // key itself and reports it as a submission, so the same handler runs from here.
                    onSubmitted: _ => HandleEnterKey(),
                    onTap: !Current.Enabled
                        ? null
                        : () => HandlePressed(controller, focusForKeyboard: !CanRequestFocus()),
                    onChanged: _ =>
                    {
                        controller.Open();
                        SetState(() =>
                        {
                            _filteredEntries = Current.DropdownMenuEntries;
                            _enableFilter = Current.EnableFilter;
                            _enableSearch = Current.EnableSearch;
                        });
                    },
                    inputFormatters: Current.InputFormatters,
                    decoration: textFieldDecoration,
                    restorationId: Current.RestorationId,
                    scrollPadding: Current.ScrollPadding)));

        Widget? effectiveLabel = effectiveDecoration.Label
                                 ?? (effectiveDecoration.LabelText is { } labelText ? new Text(labelText) : null);

        Widget body;
        if (Current.ExpandedInsets is not null)
        {
            body = textField;
        }
        else
        {
            var children = new List<Widget> { textField };
            children.AddRange(_initialMenu!);
            if (effectiveLabel is not null)
            {
                children.Add(new ExcludeSemantics(child: new Padding(
                    insets: new Thickness(4.0, 0.0),
                    child: new DefaultTextStyle(effectiveTextStyle ?? new TextStyle(), effectiveLabel))));
            }

            children.Add(effectiveDecoration.SuffixIcon ?? new SizedBox(width: 0, height: 0));
            children.Add(new Padding(
                insets: new Thickness(8.0),
                child: effectiveDecoration.PrefixIcon ?? new SizedBox(width: 0, height: 0)));
            body = new DropdownMenuBody(Current.Width, children);
        }

        return new Shortcuts(
            shortcuts: SelectOnly ? SelectOnlyShortcuts : EditableShortcuts,
            child: body);
    }

    private InputDecoration BuildDefaultDecoration(BuildContext context, MenuController controller)
    {
        return new InputDecoration
        {
            Label = Current.Label,
            HintText = Current.HintText,
            HelperText = Current.HelperText,
            ErrorText = Current.ErrorText,
            PrefixIcon = Current.LeadingIcon,
            SuffixIcon = BuildDefaultSuffixIcon(controller),
        };
    }

    private Widget? BuildDefaultSuffixIcon(MenuController controller)
    {
        if (!Current.ShowTrailingIcon) return null;
        bool isCollapsed = Current.InputDecorationTheme?.IsCollapsed ?? false;
        return new Padding(
            insets: isCollapsed ? new Thickness(0) : new Thickness(4.0),
            child: new ExcludeSemantics(
                excluding: IsButton,
                child: new IconButton(
                    focusNode: TrailingIconButtonFocusNode,
                    isSelected: controller.IsOpen,
                    constraints: Current.InputDecorationTheme?.SuffixIconConstraints,
                    padding: isCollapsed ? EdgeInsetsGeometry.Zero : null,
                    icon: Current.TrailingIcon ?? new Icon(Icons.ArrowDropDown),
                    selectedIcon: Current.SelectedTrailingIcon ?? new Icon(Icons.ArrowDropUp),
                    onPressed: !Current.Enabled ? null : () => HandlePressed(controller))));
    }
}

/// <summary>
/// Flutter's `_DropdownMenuBody`: lays out every child so the field can be sized from the widest
/// menu entry, but paints and hit-tests only the text field.
/// </summary>
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
        BoxConstraints constraints = Constraints;
        double maxWidth = 0.0;
        double? maxHeight = null;
        RenderBox? child = FirstChild;

        double intrinsicWidth = Width ?? GetMaxIntrinsicWidth(constraints.MaxHeight);
        double widthConstraint = Math.Min(intrinsicWidth, constraints.MaxWidth);
        var innerConstraints = new BoxConstraints(
            MaxWidth: widthConstraint,
            MaxHeight: GetMaxIntrinsicHeight(widthConstraint));

        while (child is not null)
        {
            var childParentData = (DropdownMenuBodyParentData)child.parentData!;
            if (ReferenceEquals(child, FirstChild))
            {
                // The text field's offset stays at its default; only its height feeds the size.
                child.Layout(innerConstraints, parentUsesSize: true);
                maxHeight ??= child.Size.Height;
                child = childParentData.nextSibling as RenderBox;
                continue;
            }

            child.Layout(innerConstraints, parentUsesSize: true);
            childParentData.offset = default;
            maxWidth = Math.Max(maxWidth, child.Size.Width);
            maxHeight ??= child.Size.Height;
            child = childParentData.nextSibling as RenderBox;
        }

        maxWidth = Math.Max(DropdownMenuBodyMetrics.MinimumWidth, maxWidth);
        Size = constraints.Constrain(new Size(Width ?? maxWidth, maxHeight ?? 0.0));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (FirstChild is not { } child) return;
        var parentData = (DropdownMenuBodyParentData)child.parentData!;
        context.PaintChild(child, offset + parentData.offset);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        double maxWidth = 0.0;
        double? maxHeight = null;
        RenderBox? child = FirstChild;

        double intrinsicWidth = Width ?? GetMaxIntrinsicWidth(constraints.MaxHeight);
        double widthConstraint = Math.Min(intrinsicWidth, constraints.MaxWidth);
        var innerConstraints = new BoxConstraints(
            MaxWidth: widthConstraint,
            MaxHeight: GetMaxIntrinsicHeight(widthConstraint));

        while (child is not null)
        {
            if (ReferenceEquals(child, FirstChild))
            {
                Size firstChildSize = child.GetDryLayout(innerConstraints);
                maxHeight ??= firstChildSize.Height;
                child = ChildAfter(child);
                continue;
            }

            Size childSize = child.GetDryLayout(innerConstraints);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight ??= childSize.Height;
            child = ChildAfter(child);
        }

        maxWidth = Math.Max(DropdownMenuBodyMetrics.MinimumWidth, maxWidth);
        return constraints.Constrain(new Size(Width ?? maxWidth, maxHeight ?? 0.0));
    }

    protected override double ComputeMinIntrinsicWidth(double height) => ComputeIntrinsicWidth(height, max: false);

    protected override double ComputeMaxIntrinsicWidth(double height) => ComputeIntrinsicWidth(height, max: true);

    private double ComputeIntrinsicWidth(double height, bool max)
    {
        RenderBox? child = FirstChild;
        double width = 0.0;
        while (child is not null)
        {
            if (ReferenceEquals(child, FirstChild))
            {
                child = ChildAfter(child);
                continue;
            }

            double childWidth = max ? child.GetMaxIntrinsicWidth(height) : child.GetMinIntrinsicWidth(height);
            // Dart accumulates the trailing suffix icon and the leading icon block, then takes the
            // max against the widest measurement child.
            if (ReferenceEquals(child, LastChild)) width += childWidth;
            if (LastChild is { } last && ReferenceEquals(child, ChildBefore(last))) width += childWidth;
            width = Math.Max(width, childWidth);
            child = ChildAfter(child);
        }

        return Math.Max(width, DropdownMenuBodyMetrics.MinimumWidth);
    }

    protected override double ComputeMinIntrinsicHeight(double width) => ComputeIntrinsicHeight(max: false);

    protected override double ComputeMaxIntrinsicHeight(double width) => ComputeIntrinsicHeight(max: true);

    // Dart shadows the `width` parameter with a local `0.0` here, so the first child is measured
    // against a zero width; ported literally.
    private double ComputeIntrinsicHeight(bool max)
    {
        double width = 0.0;
        if (FirstChild is { } child)
        {
            width = Math.Max(width, max ? child.GetMaxIntrinsicHeight(width) : child.GetMinIntrinsicHeight(width));
        }

        return width;
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (FirstChild is not { } child) return false;
        var parentData = (DropdownMenuBodyParentData)child.parentData!;
        return result.AddWithPaintOffset(
            offset: parentData.offset,
            position: position,
            hitTest: (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        // Only the text field contributes semantics; the measurement-only copies never do.
        if (FirstChild is not null) visitor(FirstChild);
    }

    public void DefaultPaint(PaintingContext ctx, Point offset) => _children.DefaultPaint(ctx, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _children.DefaultHitTestChildren(result, position);

    public void Insert(RenderBox child, RenderBox? after = null) => _children.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _children.Move(child, after);
    public void Remove(RenderBox child) => _children.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}

internal static class DropdownMenuBodyMetrics
{
    /// <summary>Dart's `_kMinimumWidth`.</summary>
    public const double MinimumWidth = 112.0;
}
