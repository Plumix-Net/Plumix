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

public sealed class AutocompletePreviousOptionIntent : Intent
{
}

public sealed class AutocompleteNextOptionIntent : Intent
{
}

public sealed class AutocompleteFirstOptionIntent : Intent
{
}

public sealed class AutocompleteLastOptionIntent : Intent
{
}

public sealed class AutocompleteNextPageOptionIntent : Intent
{
}

public sealed class AutocompletePreviousPageOptionIntent : Intent
{
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
    private const double MinimumOptionsHeight = 48.0;
    private readonly OverlayPortalController _optionsViewController = new("RawAutocomplete");
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
    private FlutterAction? _previousDismissAction;
    private BuildContext _previousDismissContext;

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
        Widget field = Current.FieldViewBuilder?.Invoke(
                           context,
                           _textEditingController!,
                           _focusNode!,
                           OnFieldSubmitted)
                       ?? new SizedBox(width: double.PositiveInfinity, height: 0.0);
        field = new Actions(BuildActions(context), field);
        field = new Shortcuts(BuildShortcuts(), field);
        field = new TextFieldTapRegion(field);
        return OverlayPortal.WithLayoutBuilder(
            controller: _optionsViewController,
            overlayChildBuilder: BuildOptionsView,
            child: field);
    }

    internal void OnFieldSubmitted()
    {
        if (!_optionsViewController.IsShowing || _options.Count == 0)
        {
            return;
        }

        Select(_options[_highlightedOptionIndex.Value]);
    }

    private Widget BuildOptionsView(
        BuildContext context,
        OverlayChildLayoutInfo layoutInfo)
    {
        if (Matrix4.TryInvert(layoutInfo.ChildPaintTransform) is not { } overlayToField)
        {
            return new SizedBox();
        }

        MediaQueryData mediaQuery = MediaQuery.Of(context);
        Rect usableOverlayRect = DeflateRect(
            DeflateRect(
                new Rect(layoutInfo.OverlaySize),
                mediaQuery.ViewInsets),
            mediaQuery.Padding);
        Rect overlayRectInField = TransformRect(overlayToField, usableOverlayRect);
        double spaceAbove = -overlayRectInField.Top;
        double spaceBelow = overlayRectInField.Bottom - layoutInfo.ChildSize.Height;
        bool opensUp = Current.OptionsViewOpenDirection switch
        {
            OptionsViewOpenDirection.Up => true,
            OptionsViewOpenDirection.MostSpace => spaceAbove > spaceBelow,
            _ => false,
        };
        double availableHeight = opensUp ? spaceAbove : spaceBelow;
        double boundingHeight = Math.Max(availableHeight, MinimumOptionsHeight);
        double originY = opensUp
            ? overlayRectInField.Top
            : overlayRectInField.Bottom - boundingHeight;

        // Exclude the options overlay from the ambient focus traversal tree. Options are navigated
        // by arrow keys (via the widget's own shortcuts) and selected via Enter or tap, so they do
        // not participate in TAB traversal; without this, TAB from the field would detour into
        // focusable items in the overlay instead of advancing to the next form field.
        Widget options = new AutocompleteHighlightedOption(
            _highlightedOptionIndex,
            new ExcludeFocus(new Builder(optionsContext => Current.OptionsViewBuilder(
                optionsContext,
                Select,
                _options))));
        options = new TextFieldTapRegion(options);
        options = new Align(
            child: options,
            alignment: opensUp
                ? AlignmentDirectional.BottomStart
                : AlignmentDirectional.TopStart);
        options = new ConstrainedBox(
            new BoxConstraints(
                MinWidth: layoutInfo.ChildSize.Width,
                MaxWidth: layoutInfo.ChildSize.Width,
                MinHeight: boundingHeight,
                MaxHeight: boundingHeight),
            options);
        options = new Align(
            child: options,
            alignment: Alignment.TopLeft,
            widthFactor: 1.0,
            heightFactor: 1.0);
        options = new Transform(
            Matrix4.TranslationValues(0.0, originY, 0.0),
            options);
        return new Transform(layoutInfo.ChildPaintTransform, options);
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

        IReadOnlyList<T> options = result?.ToArray() ?? [];
        if (_options.Count == 0 != (options.Count == 0))
        {
            _ = AnnounceSemanticsAsync(options.Count > 0);
        }

        _options = options;
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
        _optionsViewController.Show();
    }

    private void HideOptions()
    {
        if (_optionsViewController.IsShowing)
        {
            _optionsViewController.Hide();
        }
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

    private IReadOnlyDictionary<ShortcutActivator, Intent> BuildShortcuts()
    {
        bool apple = PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS;
        return new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new AutocompletePreviousOptionIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new AutocompleteNextOptionIntent(),
            [new SingleActivator(LogicalKeyboardKey.PageUp)] = new AutocompletePreviousPageOptionIntent(),
            [new SingleActivator(LogicalKeyboardKey.PageDown)] = new AutocompleteNextPageOptionIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowUp, control: !apple, meta: apple)] =
                new AutocompleteFirstOptionIntent(),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown, control: !apple, meta: apple)] =
                new AutocompleteLastOptionIntent(),
            [new SingleActivator(LogicalKeyboardKey.Escape)] = new DismissIntent(),
        };
    }

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
    {
        if (@event is not KeyDownEvent)
        {
            return KeyEventResult.Ignored;
        }

        LogicalKeyboardKey key = @event.LogicalKey;
        if (key.Equals(LogicalKeyboardKey.Escape))
        {
            if (!_optionsViewController.IsShowing)
            {
                var intent = new DismissIntent();
                if (_previousDismissAction?.IsEnabledObject(intent, _previousDismissContext) != true)
                {
                    return KeyEventResult.Ignored;
                }

                object? result = _previousDismissAction.InvokeObject(intent, _previousDismissContext);
                return _previousDismissAction.ToKeyEventResultObject(intent, result);
            }

            HideOptions();
            return KeyEventResult.Handled;
        }

        if (_focusNode?.HasFocus != true || _options.Count == 0)
        {
            return KeyEventResult.Ignored;
        }

        if (key.Equals(LogicalKeyboardKey.ArrowUp))
        {
            HighlightOption(HardwareKeyboard.Instance.IsMetaPressed || HardwareKeyboard.Instance.IsControlPressed
                ? 0
                : _highlightedOptionIndex.Value - 1);
            return KeyEventResult.Handled;
        }

        if (key.Equals(LogicalKeyboardKey.ArrowDown))
        {
            HighlightOption(HardwareKeyboard.Instance.IsMetaPressed || HardwareKeyboard.Instance.IsControlPressed
                ? _options.Count - 1
                : _highlightedOptionIndex.Value + 1);
            return KeyEventResult.Handled;
        }

        if (key.Equals(LogicalKeyboardKey.PageUp))
        {
            HighlightOption(_highlightedOptionIndex.Value - PageSize);
            return KeyEventResult.Handled;
        }

        if (key.Equals(LogicalKeyboardKey.PageDown))
        {
            HighlightOption(_highlightedOptionIndex.Value + PageSize);
            return KeyEventResult.Handled;
        }

        return KeyEventResult.Ignored;
    }

    private IReadOnlyDictionary<Type, FlutterAction> BuildActions(BuildContext context)
    {
        // Skip disabled handlers (a non-dismissible ModalRoute maps DismissIntent but keeps it disabled) so
        // Escape reaches the handler that would have received it without the autocomplete portal.
        FlutterAction? previousDismissAction = Actions.MaybeFindEnabled(context, new DismissIntent());
        _previousDismissAction = previousDismissAction;
        _previousDismissContext = context;
        return new Dictionary<Type, FlutterAction>
        {
            [typeof(AutocompletePreviousOptionIntent)] =
                NavigationAction<AutocompletePreviousOptionIntent>(-1),
            [typeof(AutocompleteNextOptionIntent)] =
                NavigationAction<AutocompleteNextOptionIntent>(1),
            [typeof(AutocompleteFirstOptionIntent)] =
                NavigationAction<AutocompleteFirstOptionIntent>(int.MinValue),
            [typeof(AutocompleteLastOptionIntent)] =
                NavigationAction<AutocompleteLastOptionIntent>(int.MaxValue),
            [typeof(AutocompletePreviousPageOptionIntent)] =
                NavigationAction<AutocompletePreviousPageOptionIntent>(-PageSize),
            [typeof(AutocompleteNextPageOptionIntent)] =
                NavigationAction<AutocompleteNextPageOptionIntent>(PageSize),
            [typeof(DismissIntent)] = new AutocompleteDismissAction(
                this,
                previousDismissAction,
                context),
        };
    }

    private FlutterAction NavigationAction<TIntent>(int delta) where TIntent : Intent
    {
        return new AutocompleteNavigationAction<TIntent>(
            () => _focusNode?.HasFocus == true && _options.Count > 0,
            () =>
            {
                int index = delta switch
                {
                    int.MinValue => 0,
                    int.MaxValue => _options.Count - 1,
                    _ => _highlightedOptionIndex.Value + delta,
                };
                HighlightOption(index);
            });
    }

    private void HighlightOption(int index)
    {
        UpdateHighlight(index);
        UpdateOptionsViewVisibility();
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

    private async Task AnnounceSemanticsAsync(bool hasOptions)
    {
        if (!MediaQuery.SupportsAnnounceOf(Context))
        {
            return;
        }

        WidgetsLocalizations localizations = WidgetsLocalizations.Of(Context);
        string message = hasOptions
            ? localizations.SearchResultsFound
            : localizations.NoResultsFound;
        await SemanticsService.SendAnnouncement(
            MediaQuery.ViewIdOf(Context),
            message,
            localizations.TextDirection);
    }

    private static Rect DeflateRect(Rect rect, Thickness insets)
    {
        double left = rect.Left + insets.Left;
        double top = rect.Top + insets.Top;
        double right = Math.Max(left, rect.Right - insets.Right);
        double bottom = Math.Max(top, rect.Bottom - insets.Bottom);
        return new Rect(left, top, right - left, bottom - top);
    }

    private static Rect TransformRect(Matrix4 transform, Rect rect)
    {
        Rect transformed = MatrixUtils.TransformRect(transform, rect);
        return new Rect(
            transformed.X,
            transformed.Y,
            Math.Max(0, transformed.Width),
            Math.Max(0, transformed.Height));
    }

    private sealed class AutocompleteNavigationAction<TIntent> : FlutterAction<TIntent> where TIntent : Intent
    {
        private readonly Func<bool> _isEnabled;
        private readonly Action _invoke;

        public AutocompleteNavigationAction(Func<bool> isEnabled, Action invoke)
        {
            _isEnabled = isEnabled;
            _invoke = invoke;
        }

        public override bool IsEnabled(TIntent intent) => _isEnabled();

        public override object? Invoke(TIntent intent)
        {
            _invoke();
            return null;
        }
    }

    private sealed class AutocompleteDismissAction : FlutterAction<DismissIntent>
    {
        private readonly RawAutocompleteState<T> _owner;
        private readonly FlutterAction? _previousAction;
        private readonly BuildContext _previousActionContext;

        public AutocompleteDismissAction(
            RawAutocompleteState<T> owner,
            FlutterAction? previousAction,
            BuildContext previousActionContext)
        {
            _owner = owner;
            _previousAction = previousAction;
            _previousActionContext = previousActionContext;
        }

        public override bool IsEnabled(DismissIntent intent)
        {
            return _owner._optionsViewController.IsShowing
                   || _previousAction?.IsEnabledObject(intent, _previousActionContext) == true;
        }

        public override bool ConsumesKey(DismissIntent intent)
        {
            return _owner._optionsViewController.IsShowing
                   || _previousAction?.ConsumesKeyObject(intent) == true;
        }

        public override object? Invoke(DismissIntent intent)
        {
            if (_owner._optionsViewController.IsShowing)
            {
                _owner.HideOptions();
                return null;
            }

            return _previousAction?.InvokeObject(intent, _previousActionContext);
        }
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
