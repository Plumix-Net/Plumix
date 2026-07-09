using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/autocomplete.dart

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
        return new TextFormField(
            controller: textEditingController,
            focusNode: focusNode,
            onFieldSubmitted: value => onFieldSubmitted());
    }

    private Widget BuildDefaultOptions(
        BuildContext context,
        AutocompleteOnSelected<T> onSelected,
        IEnumerable<T> options)
    {
        return new AutocompleteOptions<T>(
            DisplayStringForOption,
            onSelected,
            options.ToArray(),
            OptionsViewOpenDirection,
            OptionsMaxHeight);
    }

    private static AutocompleteOptionsBuilder<T> WrapSynchronousBuilder(
        Func<TextEditingValue, IEnumerable<T>> optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        return value => Task.FromResult(optionsBuilder(value));
    }
}

internal sealed class AutocompleteOptions<T> : StatelessWidget
{
    public AutocompleteOptions(
        AutocompleteOptionToString<T> displayStringForOption,
        AutocompleteOnSelected<T> onSelected,
        IReadOnlyList<T> options,
        OptionsViewOpenDirection openDirection,
        double optionsMaxHeight)
    {
        DisplayStringForOption = displayStringForOption;
        OnSelected = onSelected;
        Options = options;
        OpenDirection = openDirection;
        OptionsMaxHeight = optionsMaxHeight;
    }

    public AutocompleteOptionToString<T> DisplayStringForOption { get; }

    public AutocompleteOnSelected<T> OnSelected { get; }

    public IReadOnlyList<T> Options { get; }

    public OptionsViewOpenDirection OpenDirection { get; }

    public double OptionsMaxHeight { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        int highlightedIndex = AutocompleteHighlightedOption.Of(context);
        var shadow = new BoxShadows(new BoxShadow
        {
            OffsetY = 2,
            Blur = 8,
            Color = MaterialButtonCore.ApplyOpacity(theme.ShadowColor, 0.24),
        });
        Widget list = new AutocompleteOptionsList<T>(
            DisplayStringForOption,
            highlightedIndex,
            OnSelected,
            Options);
        Widget surface = new DecoratedBox(
            new BoxDecoration(
                Color: theme.CanvasColor,
                BoxShadows: shadow),
            list);
        return new ConstrainedBox(
            new BoxConstraints(MaxHeight: OptionsMaxHeight),
            surface);
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

        Scheduler.AddPostFrameCallback(timestamp =>
        {
            if (!Mounted || !_scrollController.HasClients)
            {
                return;
            }

            var position = _scrollController.PrimaryPosition;
            if (position is null)
            {
                return;
            }

            double target = Current.HighlightedIndex == 0
                ? 0
                : Current.HighlightedIndex >= Current.Options.Count - 1
                    ? position.MaxScrollExtent
                    : Math.Clamp(Current.HighlightedIndex * 48.0, 0, position.MaxScrollExtent);
            _scrollController.JumpTo(target);
        });
    }

    public override void Dispose()
    {
        _scrollController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        int highlightedIndex = AutocompleteHighlightedOption.Of(context);
        return ListView.Builder(
            itemCount: Current.Options.Count,
            controller: _scrollController,
            padding: default,
            shrinkWrap: true,
            itemBuilder: (itemContext, index) =>
            {
                T option = Current.Options[index];
                Widget optionContent = new Padding(
                    new Thickness(16),
                    new Text(Current.DisplayStringForOption(option)));
                if (highlightedIndex == index)
                {
                    optionContent = new ColoredBox(theme.FocusColor, optionContent);
                }

                return new Semantics(
                    flags: SemanticsFlags.IsButton,
                    onTap: () => Current.OnSelected(option),
                    child: new InkWell(
                        onTap: () => Current.OnSelected(option),
                        child: optionContent));
            });
    }
}
