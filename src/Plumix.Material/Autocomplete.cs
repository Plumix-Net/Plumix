using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/autocomplete.dart

public sealed class Autocomplete<T> : StatelessWidget
{
    public Autocomplete(
        Func<TextEditingValue, IEnumerable<T>> optionsBuilder,
        AutocompleteOptionToString<T>? displayStringForOption = null,
        AutocompleteFieldViewBuilder? fieldViewBuilder = null,
        FocusNode? focusNode = null,
        AutocompleteOnSelected<T>? onSelected = null,
        double optionsMaxHeight = 200,
        AutocompleteOptionsViewBuilder<T>? optionsViewBuilder = null,
        OptionsViewOpenDirection optionsViewOpenDirection = OptionsViewOpenDirection.Down,
        TextEditingController? textEditingController = null,
        TextEditingValue? initialValue = null,
        Key? key = null)
        : this(
            WrapSynchronousBuilder(optionsBuilder),
            displayStringForOption,
            fieldViewBuilder,
            focusNode,
            onSelected,
            optionsMaxHeight,
            optionsViewBuilder,
            optionsViewOpenDirection,
            textEditingController,
            initialValue,
            key)
    {
    }

    public Autocomplete(
        AutocompleteOptionsBuilder<T> optionsBuilder,
        AutocompleteOptionToString<T>? displayStringForOption = null,
        AutocompleteFieldViewBuilder? fieldViewBuilder = null,
        FocusNode? focusNode = null,
        AutocompleteOnSelected<T>? onSelected = null,
        double optionsMaxHeight = 200,
        AutocompleteOptionsViewBuilder<T>? optionsViewBuilder = null,
        OptionsViewOpenDirection optionsViewOpenDirection = OptionsViewOpenDirection.Down,
        TextEditingController? textEditingController = null,
        TextEditingValue? initialValue = null,
        Key? key = null) : base(key)
    {
        OptionsBuilder = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));
        DisplayStringForOption = displayStringForOption ?? RawAutocomplete<T>.DefaultStringForOption;
        FieldViewBuilder = fieldViewBuilder ?? BuildDefaultField;
        FocusNode = focusNode;
        OnSelected = onSelected;
        OptionsMaxHeight = optionsMaxHeight;
        OptionsViewBuilder = optionsViewBuilder;
        OptionsViewOpenDirection = optionsViewOpenDirection;
        TextEditingController = textEditingController;
        InitialValue = initialValue;
    }

    public AutocompleteOptionToString<T> DisplayStringForOption { get; }

    public AutocompleteFieldViewBuilder FieldViewBuilder { get; }

    public FocusNode? FocusNode { get; }

    public AutocompleteOnSelected<T>? OnSelected { get; }

    public AutocompleteOptionsBuilder<T> OptionsBuilder { get; }

    public AutocompleteOptionsViewBuilder<T>? OptionsViewBuilder { get; }

    public OptionsViewOpenDirection OptionsViewOpenDirection { get; }

    public double OptionsMaxHeight { get; }

    public TextEditingController? TextEditingController { get; }

    public TextEditingValue? InitialValue { get; }

    public override Widget Build(BuildContext context)
    {
        return new RawAutocomplete<T>(
            displayStringForOption: DisplayStringForOption,
            fieldViewBuilder: FieldViewBuilder,
            focusNode: FocusNode,
            textEditingController: TextEditingController,
            initialValue: InitialValue,
            optionsBuilder: OptionsBuilder,
            optionsViewOpenDirection: OptionsViewOpenDirection,
            optionsViewBuilder: OptionsViewBuilder ?? BuildDefaultOptions,
            onSelected: OnSelected);
    }

    private static Widget BuildDefaultField(
        BuildContext context,
        TextEditingController textEditingController,
        FocusNode focusNode,
        Action onFieldSubmitted)
    {
        return new AutocompleteField(
            focusNode,
            textEditingController,
            onFieldSubmitted);
    }

    private Widget BuildDefaultOptions(
        BuildContext context,
        AutocompleteOnSelected<T> onSelected,
        IEnumerable<T> options)
    {
        return new AutocompleteOptions<T>(
            DisplayStringForOption,
            onSelected,
            OptionsViewOpenDirection,
            options.ToArray(),
            OptionsMaxHeight);
    }

    private static AutocompleteOptionsBuilder<T> WrapSynchronousBuilder(
        Func<TextEditingValue, IEnumerable<T>> optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        return value => Task.FromResult(optionsBuilder(value));
    }
}

internal sealed class AutocompleteField : StatelessWidget
{
    public AutocompleteField(
        FocusNode focusNode,
        TextEditingController textEditingController,
        Action onFieldSubmitted)
    {
        FocusNode = focusNode;
        TextEditingController = textEditingController;
        OnFieldSubmitted = onFieldSubmitted;
    }

    public FocusNode FocusNode { get; }

    public Action OnFieldSubmitted { get; }

    public TextEditingController TextEditingController { get; }

    public override Widget Build(BuildContext context)
    {
        return new TextFormField(
            controller: TextEditingController,
            focusNode: FocusNode,
            onFieldSubmitted: value => OnFieldSubmitted());
    }
}

internal sealed class AutocompleteOptions<T> : StatelessWidget
{
    public AutocompleteOptions(
        AutocompleteOptionToString<T> displayStringForOption,
        AutocompleteOnSelected<T> onSelected,
        OptionsViewOpenDirection openDirection,
        IReadOnlyList<T> options,
        double optionsMaxHeight)
    {
        DisplayStringForOption = displayStringForOption;
        OnSelected = onSelected;
        OpenDirection = openDirection;
        Options = options;
        OptionsMaxHeight = optionsMaxHeight;
    }

    public AutocompleteOptionToString<T> DisplayStringForOption { get; }

    public AutocompleteOnSelected<T> OnSelected { get; }

    public OptionsViewOpenDirection OpenDirection { get; }

    public IReadOnlyList<T> Options { get; }

    public double OptionsMaxHeight { get; }

    public override Widget Build(BuildContext context)
    {
        int highlightedIndex = AutocompleteHighlightedOption.Of(context);
        Widget list = new AutocompleteOptionsList<T>(
            DisplayStringForOption,
            highlightedIndex,
            OnSelected,
            Options);
        return new Material(
            elevation: 4.0,
            child: new ConstrainedBox(
                new BoxConstraints(MaxHeight: OptionsMaxHeight),
                list));
    }
}

internal sealed class AutocompleteOptionsList<T> : StatefulWidget
{
    public AutocompleteOptionsList(
        AutocompleteOptionToString<T> displayStringForOption,
        int highlightedIndex,
        AutocompleteOnSelected<T> onSelected,
        IReadOnlyList<T> options)
    {
        DisplayStringForOption = displayStringForOption;
        HighlightedIndex = highlightedIndex;
        OnSelected = onSelected;
        Options = options;
    }

    public AutocompleteOptionToString<T> DisplayStringForOption { get; }

    public int HighlightedIndex { get; }

    public AutocompleteOnSelected<T> OnSelected { get; }

    public IReadOnlyList<T> Options { get; }

    public override State CreateState() => new AutocompleteOptionsListState<T>();
}

internal sealed class AutocompleteOptionsListState<T> : State
{
    private readonly ScrollController _scrollController = new();

    private AutocompleteOptionsList<T> Current => (AutocompleteOptionsList<T>)StateWidget;

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (AutocompleteOptionsList<T>)oldWidget;
        if (old.HighlightedIndex == Current.HighlightedIndex)
        {
            return;
        }

        ScheduleEnsureHighlightedVisible();
    }

    public override void Dispose()
    {
        _scrollController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        int highlightedIndex = AutocompleteHighlightedOption.Of(context);
        return ListView.Builder(
            itemCount: Current.Options.Count,
            controller: _scrollController,
            padding: default,
            shrinkWrap: true,
            itemBuilder: (itemContext, index) =>
            {
                T option = Current.Options[index];
                return new Semantics(
                    flags: SemanticsFlags.IsButton,
                    child: new InkWell(
                        key: KeyForOption(option),
                        onTap: () => Current.OnSelected(option),
                        child: new Builder(builderContext => new Container(
                            color: highlightedIndex == index
                                ? Theme.Of(builderContext).FocusColor
                                : null,
                            padding: new Thickness(16),
                            child: new Text(Current.DisplayStringForOption(option))))));
            });
    }

    private static GlobalObjectKey<State> KeyForOption(T option)
    {
        if (option is null)
        {
            throw new InvalidOperationException("Autocomplete options must not be null.");
        }

        return new GlobalObjectKey<State>(option);
    }

    private void ScheduleEnsureHighlightedVisible()
    {
        Scheduler.AddPostFrameCallback(timestamp =>
        {
            if (!Mounted || !_scrollController.HasClients)
            {
                return;
            }

            ScrollPosition? position = _scrollController.PrimaryPosition;
            if (position is null)
            {
                return;
            }

            T option = Current.Options[Current.HighlightedIndex];
            BuildContext? highlightedContext = KeyForOption(option).CurrentContext;
            if (highlightedContext is not null)
            {
                _ = Scrollable.EnsureVisible(highlightedContext, alignment: 0.5);
                return;
            }

            _scrollController.JumpTo(Current.HighlightedIndex == 0
                ? 0.0
                : position.MaxScrollExtent);
        });
    }
}
