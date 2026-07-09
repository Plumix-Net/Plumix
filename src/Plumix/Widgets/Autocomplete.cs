using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/autocomplete.dart

public delegate Task<IEnumerable<T>> AutocompleteOptionsBuilder<T>(TextEditingValue textEditingValue);

public delegate void AutocompleteOnSelected<in T>(T option);

public delegate Widget AutocompleteOptionsViewBuilder<T>(
    BuildContext context,
    AutocompleteOnSelected<T> onSelected,
    IEnumerable<T> options);

public delegate Widget AutocompleteFieldViewBuilder(
    BuildContext context,
    TextEditingController textEditingController,
    FocusNode focusNode,
    Action onFieldSubmitted);

public delegate string AutocompleteOptionToString<in T>(T option);

public enum OptionsViewOpenDirection
{
    Up,
    Down,
    MostSpace,
}

public sealed class RawAutocomplete<T> : StatefulWidget
{
    public RawAutocomplete(
        AutocompleteOptionsViewBuilder<T> optionsViewBuilder,
        Func<TextEditingValue, IEnumerable<T>> optionsBuilder,
        OptionsViewOpenDirection optionsViewOpenDirection = OptionsViewOpenDirection.Down,
        AutocompleteOptionToString<T>? displayStringForOption = null,
        AutocompleteFieldViewBuilder? fieldViewBuilder = null,
        FocusNode? focusNode = null,
        AutocompleteOnSelected<T>? onSelected = null,
        TextEditingController? textEditingController = null,
        TextEditingValue? initialValue = null,
        Key? key = null)
        : this(
            optionsViewBuilder,
            WrapSynchronousBuilder(optionsBuilder),
            optionsViewOpenDirection,
            displayStringForOption,
            fieldViewBuilder,
            focusNode,
            onSelected,
            textEditingController,
            initialValue,
            key)
    {
    }

    public RawAutocomplete(
        AutocompleteOptionsViewBuilder<T> optionsViewBuilder,
        AutocompleteOptionsBuilder<T> optionsBuilder,
        OptionsViewOpenDirection optionsViewOpenDirection = OptionsViewOpenDirection.Down,
        AutocompleteOptionToString<T>? displayStringForOption = null,
        AutocompleteFieldViewBuilder? fieldViewBuilder = null,
        FocusNode? focusNode = null,
        AutocompleteOnSelected<T>? onSelected = null,
        TextEditingController? textEditingController = null,
        TextEditingValue? initialValue = null,
        Key? key = null) : base(key)
    {
        OptionsViewBuilder = optionsViewBuilder ?? throw new ArgumentNullException(nameof(optionsViewBuilder));
        OptionsBuilder = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));
        if (fieldViewBuilder is null
            && (key is not GlobalKey || focusNode is null || textEditingController is null))
        {
            throw new ArgumentException(
                "Pass a fieldViewBuilder, or provide a GlobalKey, FocusNode, and TextEditingController for a separate field.",
                nameof(fieldViewBuilder));
        }

        if ((focusNode is null) != (textEditingController is null))
        {
            throw new ArgumentException("focusNode and textEditingController must either both be provided or both be null.");
        }

        if (textEditingController is not null && initialValue.HasValue)
        {
            throw new ArgumentException("textEditingController and initialValue cannot both be provided.", nameof(initialValue));
        }

        OptionsViewOpenDirection = optionsViewOpenDirection;
        DisplayStringForOption = displayStringForOption ?? DefaultStringForOption;
        FieldViewBuilder = fieldViewBuilder;
        FocusNode = focusNode;
        OnSelected = onSelected;
        TextEditingController = textEditingController;
        InitialValue = initialValue;
    }

    public AutocompleteFieldViewBuilder? FieldViewBuilder { get; }

    public FocusNode? FocusNode { get; }

    public AutocompleteOptionsViewBuilder<T> OptionsViewBuilder { get; }

    public OptionsViewOpenDirection OptionsViewOpenDirection { get; }

    public AutocompleteOptionToString<T> DisplayStringForOption { get; }

    public AutocompleteOnSelected<T>? OnSelected { get; }

    public AutocompleteOptionsBuilder<T> OptionsBuilder { get; }

    public TextEditingController? TextEditingController { get; }

    public TextEditingValue? InitialValue { get; }

    public static void OnFieldSubmitted(GlobalKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.CurrentElement is not StatefulElement element
            || element.State is not RawAutocompleteState<T> state)
        {
            throw new InvalidOperationException("The GlobalKey is not attached to a matching RawAutocomplete.");
        }

        state.OnFieldSubmitted();
    }

    public static string DefaultStringForOption(T option) => option?.ToString() ?? string.Empty;

    public override State CreateState() => new RawAutocompleteState<T>();

    private static AutocompleteOptionsBuilder<T> WrapSynchronousBuilder(
        Func<TextEditingValue, IEnumerable<T>> optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        return value => Task.FromResult(optionsBuilder(value));
    }
}

internal sealed class RawAutocompleteState<T> : State
{
    private const int PageSize = 4;
    private readonly ValueNotifier<int> _highlightedOptionIndex = new(0);
    private TextEditingController? _textEditingController;
    private FocusNode? _focusNode;
    private bool _ownsTextEditingController;
    private bool _ownsFocusNode;
    private bool _hasFocus;
    private bool _selecting;
    private IReadOnlyList<T> _options = [];
    private T? _selection;
    private string? _lastFieldText;
    private int _onChangedCallId;
    private AutocompleteRoute<T>? _optionsRoute;

    private RawAutocomplete<T> Current => (RawAutocomplete<T>)StateWidget;

    internal IReadOnlyList<T> Options => _options;

    internal ValueNotifier<int> HighlightedOptionIndex => _highlightedOptionIndex;

    public override void InitState()
    {
        AttachTextEditingController(Current.TextEditingController, Current.InitialValue);
        AttachFocusNode(Current.FocusNode);
        _hasFocus = _focusNode!.HasFocus;
        if (_hasFocus)
        {
            Scheduler.AddPostFrameCallback(timestamp =>
            {
                _ = UpdateOptionsAsync();
            });
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (RawAutocomplete<T>)oldWidget;
        if (!ReferenceEquals(old.TextEditingController, Current.TextEditingController))
        {
            DetachTextEditingController();
            AttachTextEditingController(Current.TextEditingController, Current.InitialValue);
            _lastFieldText = null;
        }

        if (!ReferenceEquals(old.FocusNode, Current.FocusNode))
        {
            DetachFocusNode();
            AttachFocusNode(Current.FocusNode);
            _hasFocus = _focusNode!.HasFocus;
        }

    }

    public override void Dispose()
    {
        HideOptions();
        DetachFocusNode();
        DetachTextEditingController();
        _highlightedOptionIndex.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return Current.FieldViewBuilder?.Invoke(
                   context,
                   _textEditingController!,
                   _focusNode!,
                   OnFieldSubmitted)
               ?? new SizedBox(height: 0);
    }

    internal Widget BuildOptionsView(BuildContext context)
    {
        return Current.OptionsViewBuilder(context, Select, _options);
    }

    internal void OnFieldSubmitted()
    {
        if (_optionsRoute is null || _options.Count == 0)
        {
            return;
        }

        Select(_options[_highlightedOptionIndex.Value]);
    }

    private void HandleFocusChange()
    {
        bool hasFocus = _focusNode?.HasFocus == true;
        if (_hasFocus == hasFocus)
        {
            return;
        }

        _hasFocus = hasFocus;
        if (hasFocus && _lastFieldText is null)
        {
            _ = UpdateOptionsAsync();
            return;
        }

        UpdateOptionsViewVisibility();
    }

    private void HandleControllerChanged()
    {
        _ = UpdateOptionsAsync();
    }

    private async Task UpdateOptionsAsync()
    {
        if (_selecting || _textEditingController is null)
        {
            return;
        }

        TextEditingValue value = _textEditingController.Value;
        bool shouldUpdateOptions = !string.Equals(value.Text, _lastFieldText, StringComparison.Ordinal);
        if (shouldUpdateOptions)
        {
            _onChangedCallId += 1;
        }

        _lastFieldText = value.Text;
        int callId = _onChangedCallId;
        IEnumerable<T> result = await Current.OptionsBuilder(value);
        if (!Mounted || callId != _onChangedCallId || !shouldUpdateOptions)
        {
            return;
        }

        _options = result?.ToArray() ?? [];
        UpdateHighlight(_highlightedOptionIndex.Value);
        if (_selection is not null
            && !string.Equals(value.Text, Current.DisplayStringForOption(_selection), StringComparison.Ordinal))
        {
            _selection = default;
        }

        UpdateOptionsViewVisibility();
    }

    private void UpdateOptionsViewVisibility()
    {
        if (_focusNode?.HasFocus == true && _options.Count > 0)
        {
            ShowOptions();
        }
        else
        {
            HideOptions();
        }
    }

    private void ShowOptions()
    {
        if (_optionsRoute is not null)
        {
            _optionsRoute.Refresh();
            return;
        }

        if (Context.FindRenderObject() is not RenderBox anchor || !anchor.HasSize)
        {
            return;
        }

        var route = new AutocompleteRoute<T>(
            this,
            ResolveGlobalBounds(anchor),
            Current.OptionsViewOpenDirection,
            MediaQuery.Of(Context),
            Directionality.Of(Context));
        _optionsRoute = route;
        Navigator.Of(Context).Push(route);
    }

    private void HideOptions()
    {
        var route = _optionsRoute;
        _optionsRoute = null;
        route?.Navigator?.MaybePop();
    }

    private void Select(T nextSelection)
    {
        if (EqualityComparer<T?>.Default.Equals(nextSelection, _selection))
        {
            return;
        }

        _selecting = true;
        _selection = nextSelection;
        string selectionString = Current.DisplayStringForOption(nextSelection);
        _textEditingController!.Value = new TextEditingValue(
            selectionString,
            TextSelection.Collapsed(selectionString.Length));
        _lastFieldText = selectionString;
        Current.OnSelected?.Invoke(nextSelection);
        HideOptions();
        _selecting = false;
    }

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
    {
        if (!@event.IsDown)
        {
            return KeyEventResult.Ignored;
        }

        bool canNavigate = _optionsRoute is not null && _options.Count > 0;
        string key = @event.Key;
        if ((key is "Escape" or "Esc") && _optionsRoute is not null)
        {
            HideOptions();
            return KeyEventResult.Handled;
        }

        if (!canNavigate)
        {
            return KeyEventResult.Ignored;
        }

        if (key is "ArrowUp" or "Up")
        {
            HighlightOption((@event.IsMetaPressed || @event.IsControlPressed)
                ? 0
                : _highlightedOptionIndex.Value - 1);
            return KeyEventResult.Handled;
        }

        if (key is "ArrowDown" or "Down")
        {
            HighlightOption((@event.IsMetaPressed || @event.IsControlPressed)
                ? _options.Count - 1
                : _highlightedOptionIndex.Value + 1);
            return KeyEventResult.Handled;
        }

        if (key is "PageUp" or "Prior")
        {
            HighlightOption(_highlightedOptionIndex.Value - PageSize);
            return KeyEventResult.Handled;
        }

        if (key is "PageDown" or "Next")
        {
            HighlightOption(_highlightedOptionIndex.Value + PageSize);
            return KeyEventResult.Handled;
        }

        if (key is "Enter" or "Return" or "NumPadEnter" or "NumpadEnter")
        {
            OnFieldSubmitted();
            return KeyEventResult.Handled;
        }

        return KeyEventResult.Ignored;
    }

    private void HighlightOption(int index)
    {
        UpdateHighlight(index);
        _optionsRoute?.Refresh();
    }

    private void UpdateHighlight(int index)
    {
        _highlightedOptionIndex.Value = _options.Count == 0
            ? 0
            : Math.Clamp(index, 0, _options.Count - 1);
    }

    private void AttachTextEditingController(TextEditingController? external, TextEditingValue? initialValue)
    {
        _textEditingController = external ?? TextEditingController.FromValue(initialValue);
        _ownsTextEditingController = external is null;
        _textEditingController.AddListener(HandleControllerChanged);
    }

    private void DetachTextEditingController()
    {
        if (_textEditingController is null)
        {
            return;
        }

        _textEditingController.RemoveListener(HandleControllerChanged);
        if (_ownsTextEditingController)
        {
            _textEditingController.Dispose();
        }

        _textEditingController = null;
        _ownsTextEditingController = false;
    }

    private void AttachFocusNode(FocusNode? external)
    {
        _focusNode = external ?? new FocusNode();
        _ownsFocusNode = external is null;
        _focusNode.AddListener(HandleFocusChange);
        _focusNode.AddKeyEventHandler(HandleKeyEvent);
    }

    private void DetachFocusNode()
    {
        if (_focusNode is null)
        {
            return;
        }

        _focusNode.RemoveListener(HandleFocusChange);
        _focusNode.RemoveKeyEventHandler(HandleKeyEvent);
        if (_ownsFocusNode)
        {
            _focusNode.Dispose();
        }

        _focusNode = null;
        _ownsFocusNode = false;
    }

    private static Rect ResolveGlobalBounds(RenderBox renderBox)
    {
        var transform = Matrix.Identity;
        RenderObject? child = renderBox;
        while (child?.Parent is not null)
        {
            RenderObject parent = child.Parent;
            Point childOffset = child.parentData is BoxParentData data ? data.offset : default;
            Matrix childTransform = Matrix.CreateTranslation(childOffset.X, childOffset.Y);
            if (parent is RenderTransform renderTransform)
            {
                childTransform *= renderTransform.EffectiveTransform;
            }

            transform = childTransform * transform;
            child = parent;
        }

        Point[] points =
        [
            transform.Transform(default),
            transform.Transform(new Point(renderBox.Size.Width, 0)),
            transform.Transform(new Point(0, renderBox.Size.Height)),
            transform.Transform(new Point(renderBox.Size.Width, renderBox.Size.Height)),
        ];
        double left = points.Min(point => point.X);
        double top = points.Min(point => point.Y);
        double right = points.Max(point => point.X);
        double bottom = points.Max(point => point.Y);
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}

public sealed class AutocompleteHighlightedOption : InheritedNotifier<ValueNotifier<int>>
{
    public AutocompleteHighlightedOption(
        ValueNotifier<int> highlightIndexNotifier,
        Widget child,
        Key? key = null) : base(highlightIndexNotifier, child, key)
    {
    }

    public static int Of(BuildContext context)
    {
        return context.DependOnInherited<AutocompleteHighlightedOption>()?.Notifier?.Value ?? 0;
    }
}

internal sealed class AutocompleteRoute<T> : PageRoute
{
    private readonly RawAutocompleteState<T> _owner;
    private readonly MediaQueryData _mediaQuery;
    private readonly TextDirection _textDirection;

    public AutocompleteRoute(
        RawAutocompleteState<T> owner,
        Rect fieldRect,
        OptionsViewOpenDirection openDirection,
        MediaQueryData mediaQuery,
        TextDirection textDirection)
    {
        _owner = owner;
        FieldRect = fieldRect;
        OpenDirection = openDirection;
        _mediaQuery = mediaQuery;
        _textDirection = textDirection;
    }

    public override bool Opaque => false;

    public Rect FieldRect { get; }

    public OptionsViewOpenDirection OpenDirection { get; }

    public override Widget BuildPage(BuildContext context)
    {
        Widget options = new AutocompleteHighlightedOption(
            _owner.HighlightedOptionIndex,
            new Builder(_owner.BuildOptionsView));
        options = new AutocompleteOptionsPosition(
            FieldRect,
            OpenDirection,
            _mediaQuery.Padding,
            _mediaQuery.ViewInsets,
            options);
        return new Directionality(_textDirection, new MediaQuery(_mediaQuery, options));
    }

    public void Refresh() => NotifyRouteChanged();
}

internal sealed class AutocompleteOptionsPosition : SingleChildRenderObjectWidget
{
    public AutocompleteOptionsPosition(
        Rect fieldRect,
        OptionsViewOpenDirection openDirection,
        Thickness padding,
        Thickness viewInsets,
        Widget child) : base(child)
    {
        FieldRect = fieldRect;
        OpenDirection = openDirection;
        Padding = padding;
        ViewInsets = viewInsets;
    }

    public Rect FieldRect { get; }

    public OptionsViewOpenDirection OpenDirection { get; }

    public Thickness Padding { get; }

    public Thickness ViewInsets { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAutocompleteOptionsPosition(FieldRect, OpenDirection, Padding, ViewInsets);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var position = (RenderAutocompleteOptionsPosition)renderObject;
        position.FieldRect = FieldRect;
        position.OpenDirection = OpenDirection;
        position.Padding = Padding;
        position.ViewInsets = ViewInsets;
    }
}

internal sealed class RenderAutocompleteOptionsPosition : RenderProxyBox
{
    private Rect _fieldRect;
    private OptionsViewOpenDirection _openDirection;
    private Thickness _padding;
    private Thickness _viewInsets;

    public RenderAutocompleteOptionsPosition(
        Rect fieldRect,
        OptionsViewOpenDirection openDirection,
        Thickness padding,
        Thickness viewInsets)
    {
        _fieldRect = fieldRect;
        _openDirection = openDirection;
        _padding = padding;
        _viewInsets = viewInsets;
    }

    public Rect FieldRect
    {
        get => _fieldRect;
        set
        {
            if (_fieldRect == value) return;
            _fieldRect = value;
            MarkNeedsLayout();
        }
    }

    public OptionsViewOpenDirection OpenDirection
    {
        get => _openDirection;
        set
        {
            if (_openDirection == value) return;
            _openDirection = value;
            MarkNeedsLayout();
        }
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding == value) return;
            _padding = value;
            MarkNeedsLayout();
        }
    }

    public Thickness ViewInsets
    {
        get => _viewInsets;
        set
        {
            if (_viewInsets == value) return;
            _viewInsets = value;
            MarkNeedsLayout();
        }
    }

    internal bool OpensUp { get; private set; }

    protected override void PerformLayout()
    {
        Size = Constraints.Biggest;
        if (Child is null)
        {
            return;
        }

        double safeLeft = Math.Max(_padding.Left, _viewInsets.Left);
        double safeTop = Math.Max(_padding.Top, _viewInsets.Top);
        double safeRight = Math.Max(safeLeft, Size.Width - Math.Max(_padding.Right, _viewInsets.Right));
        double safeBottom = Math.Max(safeTop, Size.Height - Math.Max(_padding.Bottom, _viewInsets.Bottom));
        double spaceAbove = Math.Max(0, _fieldRect.Top - safeTop);
        double spaceBelow = Math.Max(0, safeBottom - _fieldRect.Bottom);
        OpensUp = _openDirection switch
        {
            OptionsViewOpenDirection.Up => true,
            OptionsViewOpenDirection.MostSpace => spaceAbove > spaceBelow,
            _ => false,
        };

        double availableHeight = Math.Max(48, OpensUp ? spaceAbove : spaceBelow);
        double width = Math.Clamp(_fieldRect.Width, 0, Math.Max(0, safeRight - safeLeft));
        Child.Layout(
            new BoxConstraints(
                MinWidth: width,
                MaxWidth: width,
                MaxHeight: availableHeight),
            parentUsesSize: true);
        double x = Math.Clamp(_fieldRect.Left, safeLeft, Math.Max(safeLeft, safeRight - Child.Size.Width));
        double y = OpensUp ? _fieldRect.Top - Child.Size.Height : _fieldRect.Bottom;
        ((BoxParentData)Child.parentData!).offset = new Point(x, y);
    }
}
