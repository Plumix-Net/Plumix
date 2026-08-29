using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/text_field.dart

public enum OverlayVisibilityMode
{
    Never,
    Editing,
    NotEditing,
    Always,
}

/// <summary>An iOS-style editable text field with optional prefix, suffix, placeholder, and clear button.</summary>
public sealed class CupertinoTextField : StatefulWidget
{
    internal static readonly BoxDecoration DefaultRoundedBorderDecoration = new(
        Color: CupertinoColors.White,
        Border: Border.All(Color.FromUInt32(0x33000000), width: 0.0),
        BorderRadius: BorderRadius.Circular(5.0));

    internal static readonly TextStyle DefaultPlaceholderStyle = new(
        Color: CupertinoColors.PlaceholderText.Color,
        FontWeight: FontWeight.Normal);

    public CupertinoTextField(
        object? groupId = null,
        TextEditingController? controller = null,
        FocusNode? focusNode = null,
        UndoHistoryController? undoController = null,
        BoxDecoration? decoration = null,
        EdgeInsetsGeometry? padding = null,
        string? placeholder = null,
        TextStyle? placeholderStyle = null,
        Widget? prefix = null,
        OverlayVisibilityMode prefixMode = OverlayVisibilityMode.Always,
        Widget? suffix = null,
        OverlayVisibilityMode suffixMode = OverlayVisibilityMode.Always,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Center,
        OverlayVisibilityMode clearButtonMode = OverlayVisibilityMode.Never,
        string? clearButtonSemanticLabel = null,
        TextInputType? keyboardType = null,
        TextInputActionType? textInputAction = null,
        TextCapitalization textCapitalization = TextCapitalization.None,
        TextStyle? style = null,
        StrutStyle? strutStyle = null,
        TextAlign textAlign = TextAlign.Start,
        TextAlignVertical? textAlignVertical = null,
        TextDirection? textDirection = null,
        bool readOnly = false,
        ToolbarOptions? toolbarOptions = null,
        bool? showCursor = null,
        bool autofocus = false,
        string obscuringCharacter = "•",
        bool obscureText = false,
        bool? autocorrect = true,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        bool enableSuggestions = true,
        int? maxLines = 1,
        int? minLines = null,
        bool expands = false,
        int? maxLength = null,
        MaxLengthEnforcement? maxLengthEnforcement = null,
        Action<string>? onChanged = null,
        Action? onEditingComplete = null,
        Action<string>? onSubmitted = null,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<PointerUpEvent>? onTapUpOutside = null,
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        bool enabled = true,
        double cursorWidth = 2.0,
        double? cursorHeight = null,
        Radius? cursorRadius = null,
        bool cursorOpacityAnimates = true,
        CupertinoDynamicColor? cursorColor = null,
        BoxHeightStyle? selectionHeightStyle = null,
        BoxWidthStyle? selectionWidthStyle = null,
        PlatformBrightness? keyboardAppearance = null,
        EdgeInsetsGeometry? scrollPadding = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool? enableInteractiveSelection = null,
        bool? selectAllOnFocus = null,
        TextSelectionControls? selectionControls = null,
        Action? onTap = null,
        ScrollController? scrollController = null,
        ScrollPhysics? scrollPhysics = null,
        IReadOnlyList<string>? autofillHints = null,
        ContentInsertionConfiguration? contentInsertionConfiguration = null,
        Clip clipBehavior = Clip.HardEdge,
        string? restorationId = null,
        bool scribbleEnabled = true,
        bool stylusHandwritingEnabled = true,
        bool enableIMEPersonalizedLearning = true,
        bool? enableInlinePrediction = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        SpellCheckConfiguration? spellCheckConfiguration = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Key? key = null) : this(
            borderless: false,
            groupId,
            controller,
            focusNode,
            undoController,
            decoration,
            padding,
            placeholder,
            placeholderStyle,
            prefix,
            prefixMode,
            suffix,
            suffixMode,
            crossAxisAlignment,
            clearButtonMode,
            clearButtonSemanticLabel,
            keyboardType,
            textInputAction,
            textCapitalization,
            style,
            strutStyle,
            textAlign,
            textAlignVertical,
            textDirection,
            readOnly,
            toolbarOptions,
            showCursor,
            autofocus,
            obscuringCharacter,
            obscureText,
            autocorrect,
            smartDashesType,
            smartQuotesType,
            enableSuggestions,
            maxLines,
            minLines,
            expands,
            maxLength,
            maxLengthEnforcement,
            onChanged,
            onEditingComplete,
            onSubmitted,
            onTapOutside,
            onTapUpOutside,
            inputFormatters,
            enabled,
            cursorWidth,
            cursorHeight,
            cursorRadius,
            cursorOpacityAnimates,
            cursorColor,
            selectionHeightStyle,
            selectionWidthStyle,
            keyboardAppearance,
            scrollPadding,
            dragStartBehavior,
            enableInteractiveSelection,
            selectAllOnFocus,
            selectionControls,
            onTap,
            scrollController,
            scrollPhysics,
            autofillHints,
            contentInsertionConfiguration,
            clipBehavior,
            restorationId,
            scribbleEnabled,
            stylusHandwritingEnabled,
            enableIMEPersonalizedLearning,
            enableInlinePrediction,
            contextMenuBuilder,
            spellCheckConfiguration,
            magnifierConfiguration,
            key)
    {
    }

    public static CupertinoTextField Borderless(
        object? groupId = null,
        TextEditingController? controller = null,
        FocusNode? focusNode = null,
        UndoHistoryController? undoController = null,
        BoxDecoration? decoration = null,
        EdgeInsetsGeometry? padding = null,
        string? placeholder = null,
        TextStyle? placeholderStyle = null,
        Widget? prefix = null,
        OverlayVisibilityMode prefixMode = OverlayVisibilityMode.Always,
        Widget? suffix = null,
        OverlayVisibilityMode suffixMode = OverlayVisibilityMode.Always,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Center,
        OverlayVisibilityMode clearButtonMode = OverlayVisibilityMode.Never,
        string? clearButtonSemanticLabel = null,
        TextInputType? keyboardType = null,
        TextInputActionType? textInputAction = null,
        TextCapitalization textCapitalization = TextCapitalization.None,
        TextStyle? style = null,
        StrutStyle? strutStyle = null,
        TextAlign textAlign = TextAlign.Start,
        TextAlignVertical? textAlignVertical = null,
        TextDirection? textDirection = null,
        bool readOnly = false,
        ToolbarOptions? toolbarOptions = null,
        bool? showCursor = null,
        bool autofocus = false,
        string obscuringCharacter = "•",
        bool obscureText = false,
        bool? autocorrect = null,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        bool enableSuggestions = true,
        int? maxLines = 1,
        int? minLines = null,
        bool expands = false,
        int? maxLength = null,
        MaxLengthEnforcement? maxLengthEnforcement = null,
        Action<string>? onChanged = null,
        Action? onEditingComplete = null,
        Action<string>? onSubmitted = null,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<PointerUpEvent>? onTapUpOutside = null,
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        bool enabled = true,
        double cursorWidth = 2.0,
        double? cursorHeight = null,
        Radius? cursorRadius = null,
        bool cursorOpacityAnimates = true,
        CupertinoDynamicColor? cursorColor = null,
        BoxHeightStyle? selectionHeightStyle = null,
        BoxWidthStyle? selectionWidthStyle = null,
        PlatformBrightness? keyboardAppearance = null,
        EdgeInsetsGeometry? scrollPadding = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool? enableInteractiveSelection = null,
        bool? selectAllOnFocus = null,
        TextSelectionControls? selectionControls = null,
        Action? onTap = null,
        ScrollController? scrollController = null,
        ScrollPhysics? scrollPhysics = null,
        IReadOnlyList<string>? autofillHints = null,
        ContentInsertionConfiguration? contentInsertionConfiguration = null,
        Clip clipBehavior = Clip.HardEdge,
        string? restorationId = null,
        bool scribbleEnabled = true,
        bool stylusHandwritingEnabled = true,
        bool enableIMEPersonalizedLearning = true,
        bool? enableInlinePrediction = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        SpellCheckConfiguration? spellCheckConfiguration = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Key? key = null)
    {
        return new CupertinoTextField(
            borderless: true,
            groupId,
            controller,
            focusNode,
            undoController,
            decoration,
            padding,
            placeholder,
            placeholderStyle,
            prefix,
            prefixMode,
            suffix,
            suffixMode,
            crossAxisAlignment,
            clearButtonMode,
            clearButtonSemanticLabel,
            keyboardType,
            textInputAction,
            textCapitalization,
            style,
            strutStyle,
            textAlign,
            textAlignVertical,
            textDirection,
            readOnly,
            toolbarOptions,
            showCursor,
            autofocus,
            obscuringCharacter,
            obscureText,
            autocorrect,
            smartDashesType,
            smartQuotesType,
            enableSuggestions,
            maxLines,
            minLines,
            expands,
            maxLength,
            maxLengthEnforcement,
            onChanged,
            onEditingComplete,
            onSubmitted,
            onTapOutside,
            onTapUpOutside,
            inputFormatters,
            enabled,
            cursorWidth,
            cursorHeight,
            cursorRadius,
            cursorOpacityAnimates,
            cursorColor,
            selectionHeightStyle,
            selectionWidthStyle,
            keyboardAppearance,
            scrollPadding,
            dragStartBehavior,
            enableInteractiveSelection,
            selectAllOnFocus,
            selectionControls,
            onTap,
            scrollController,
            scrollPhysics,
            autofillHints,
            contentInsertionConfiguration,
            clipBehavior,
            restorationId,
            scribbleEnabled,
            stylusHandwritingEnabled,
            enableIMEPersonalizedLearning,
            enableInlinePrediction,
            contextMenuBuilder,
            spellCheckConfiguration,
            magnifierConfiguration,
            key);
    }

    private CupertinoTextField(
        bool borderless,
        object? groupId,
        TextEditingController? controller,
        FocusNode? focusNode,
        UndoHistoryController? undoController,
        BoxDecoration? decoration,
        EdgeInsetsGeometry? padding,
        string? placeholder,
        TextStyle? placeholderStyle,
        Widget? prefix,
        OverlayVisibilityMode prefixMode,
        Widget? suffix,
        OverlayVisibilityMode suffixMode,
        CrossAxisAlignment crossAxisAlignment,
        OverlayVisibilityMode clearButtonMode,
        string? clearButtonSemanticLabel,
        TextInputType? keyboardType,
        TextInputActionType? textInputAction,
        TextCapitalization textCapitalization,
        TextStyle? style,
        StrutStyle? strutStyle,
        TextAlign textAlign,
        TextAlignVertical? textAlignVertical,
        TextDirection? textDirection,
        bool readOnly,
        ToolbarOptions? toolbarOptions,
        bool? showCursor,
        bool autofocus,
        string obscuringCharacter,
        bool obscureText,
        bool? autocorrect,
        SmartDashesType? smartDashesType,
        SmartQuotesType? smartQuotesType,
        bool enableSuggestions,
        int? maxLines,
        int? minLines,
        bool expands,
        int? maxLength,
        MaxLengthEnforcement? maxLengthEnforcement,
        Action<string>? onChanged,
        Action? onEditingComplete,
        Action<string>? onSubmitted,
        Action<PointerDownEvent>? onTapOutside,
        Action<PointerUpEvent>? onTapUpOutside,
        IReadOnlyList<TextInputFormatter>? inputFormatters,
        bool enabled,
        double cursorWidth,
        double? cursorHeight,
        Radius? cursorRadius,
        bool cursorOpacityAnimates,
        CupertinoDynamicColor? cursorColor,
        BoxHeightStyle? selectionHeightStyle,
        BoxWidthStyle? selectionWidthStyle,
        PlatformBrightness? keyboardAppearance,
        EdgeInsetsGeometry? scrollPadding,
        DragStartBehavior dragStartBehavior,
        bool? enableInteractiveSelection,
        bool? selectAllOnFocus,
        TextSelectionControls? selectionControls,
        Action? onTap,
        ScrollController? scrollController,
        ScrollPhysics? scrollPhysics,
        IReadOnlyList<string>? autofillHints,
        ContentInsertionConfiguration? contentInsertionConfiguration,
        Clip clipBehavior,
        string? restorationId,
        bool scribbleEnabled,
        bool stylusHandwritingEnabled,
        bool enableIMEPersonalizedLearning,
        bool? enableInlinePrediction,
        EditableTextContextMenuBuilder? contextMenuBuilder,
        SpellCheckConfiguration? spellCheckConfiguration,
        TextMagnifierConfiguration? magnifierConfiguration,
        Key? key) : base(key)
    {
        Validate(
            obscuringCharacter,
            maxLines,
            minLines,
            expands,
            obscureText,
            maxLength,
            textInputAction,
            keyboardType);
        GroupId = groupId ?? typeof(EditableText);
        Controller = controller;
        FocusNode = focusNode;
        UndoController = undoController;
        Decoration = borderless ? decoration : decoration ?? DefaultRoundedBorderDecoration;
        Padding = padding ?? EdgeInsetsGeometry.All(7.0);
        Placeholder = placeholder;
        PlaceholderStyle = placeholderStyle ?? DefaultPlaceholderStyle;
        Prefix = prefix;
        PrefixMode = prefixMode;
        Suffix = suffix;
        SuffixMode = suffixMode;
        CrossAxisAlignment = crossAxisAlignment;
        ClearButtonMode = clearButtonMode;
        ClearButtonSemanticLabel = clearButtonSemanticLabel;
        KeyboardType = keyboardType ?? (maxLines == 1 ? TextInputType.Text : TextInputType.Multiline);
        TextInputAction = textInputAction;
        TextCapitalization = textCapitalization;
        Style = style;
        StrutStyle = strutStyle;
        TextAlign = textAlign;
        TextAlignVertical = textAlignVertical;
        TextDirection = textDirection;
        ReadOnly = readOnly;
        ToolbarOptions = toolbarOptions;
        ShowCursor = showCursor;
        Autofocus = autofocus;
        ObscuringCharacter = obscuringCharacter;
        ObscureText = obscureText;
        Autocorrect = autocorrect;
        SmartDashesType = smartDashesType ?? (obscureText
            ? global::Plumix.UI.SmartDashesType.Disabled
            : global::Plumix.UI.SmartDashesType.Enabled);
        SmartQuotesType = smartQuotesType ?? (obscureText
            ? global::Plumix.UI.SmartQuotesType.Disabled
            : global::Plumix.UI.SmartQuotesType.Enabled);
        EnableSuggestions = enableSuggestions;
        MaxLines = maxLines;
        MinLines = minLines;
        Expands = expands;
        MaxLength = maxLength;
        MaxLengthEnforcement = maxLengthEnforcement;
        OnChanged = onChanged;
        OnEditingComplete = onEditingComplete;
        OnSubmitted = onSubmitted;
        OnTapOutside = onTapOutside;
        OnTapUpOutside = onTapUpOutside;
        InputFormatters = inputFormatters;
        Enabled = enabled;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        CursorRadius = cursorRadius ?? Radius.Circular(2.0);
        CursorOpacityAnimates = cursorOpacityAnimates;
        CursorColor = cursorColor;
        SelectionHeightStyle = selectionHeightStyle;
        SelectionWidthStyle = selectionWidthStyle;
        KeyboardAppearance = keyboardAppearance;
        ScrollPadding = scrollPadding ?? EdgeInsetsGeometry.All(20.0);
        DragStartBehavior = dragStartBehavior;
        EnableInteractiveSelection = enableInteractiveSelection ?? (!readOnly || !obscureText);
        SelectAllOnFocus = selectAllOnFocus;
        SelectionControls = selectionControls;
        OnTap = onTap;
        ScrollController = scrollController;
        ScrollPhysics = scrollPhysics;
        AutofillHints = autofillHints ?? [];
        ContentInsertionConfiguration = contentInsertionConfiguration;
        ClipBehavior = clipBehavior;
        RestorationId = restorationId;
        ScribbleEnabled = scribbleEnabled;
        StylusHandwritingEnabled = stylusHandwritingEnabled;
        EnableIMEPersonalizedLearning = enableIMEPersonalizedLearning;
        EnableInlinePrediction = enableInlinePrediction;
        ContextMenuBuilder = contextMenuBuilder ?? DefaultContextMenuBuilder;
        SpellCheckConfiguration = spellCheckConfiguration;
        MagnifierConfiguration = magnifierConfiguration ?? IosMagnifierConfiguration;
        IsBorderless = borderless;
    }

    public object GroupId { get; }
    public TextEditingController? Controller { get; }
    public FocusNode? FocusNode { get; }
    public UndoHistoryController? UndoController { get; }
    public BoxDecoration? Decoration { get; }
    public EdgeInsetsGeometry Padding { get; }
    public string? Placeholder { get; }
    public TextStyle PlaceholderStyle { get; }
    public Widget? Prefix { get; }
    public OverlayVisibilityMode PrefixMode { get; }
    public Widget? Suffix { get; }
    public OverlayVisibilityMode SuffixMode { get; }
    public CrossAxisAlignment CrossAxisAlignment { get; }
    public OverlayVisibilityMode ClearButtonMode { get; }
    public string? ClearButtonSemanticLabel { get; }
    public TextInputType KeyboardType { get; }
    public TextInputActionType? TextInputAction { get; }
    public TextCapitalization TextCapitalization { get; }
    public TextStyle? Style { get; }
    public StrutStyle? StrutStyle { get; }
    public TextAlign TextAlign { get; }
    public TextAlignVertical? TextAlignVertical { get; }
    public TextDirection? TextDirection { get; }
    public bool ReadOnly { get; }
    public ToolbarOptions? ToolbarOptions { get; }
    public bool? ShowCursor { get; }
    public bool Autofocus { get; }
    public string ObscuringCharacter { get; }
    public bool ObscureText { get; }
    public bool? Autocorrect { get; }
    public SmartDashesType SmartDashesType { get; }
    public SmartQuotesType SmartQuotesType { get; }
    public bool EnableSuggestions { get; }
    public int? MaxLines { get; }
    public int? MinLines { get; }
    public bool Expands { get; }
    public int? MaxLength { get; }
    public MaxLengthEnforcement? MaxLengthEnforcement { get; }
    public Action<string>? OnChanged { get; }
    public Action? OnEditingComplete { get; }
    public Action<string>? OnSubmitted { get; }
    public Action<PointerDownEvent>? OnTapOutside { get; }
    public Action<PointerUpEvent>? OnTapUpOutside { get; }
    public IReadOnlyList<TextInputFormatter>? InputFormatters { get; }
    public bool Enabled { get; }
    public double CursorWidth { get; }
    public double? CursorHeight { get; }
    public Radius CursorRadius { get; }
    public bool CursorOpacityAnimates { get; }
    public CupertinoDynamicColor? CursorColor { get; }
    public BoxHeightStyle? SelectionHeightStyle { get; }
    public BoxWidthStyle? SelectionWidthStyle { get; }
    public PlatformBrightness? KeyboardAppearance { get; }
    public EdgeInsetsGeometry ScrollPadding { get; }
    public DragStartBehavior DragStartBehavior { get; }
    public bool EnableInteractiveSelection { get; }
    public bool SelectionEnabled => EnableInteractiveSelection;
    public bool? SelectAllOnFocus { get; }
    public TextSelectionControls? SelectionControls { get; }
    public Action? OnTap { get; }
    public ScrollController? ScrollController { get; }
    public ScrollPhysics? ScrollPhysics { get; }
    public IReadOnlyList<string>? AutofillHints { get; }
    public ContentInsertionConfiguration? ContentInsertionConfiguration { get; }
    public Clip ClipBehavior { get; }
    public string? RestorationId { get; }
    public bool ScribbleEnabled { get; }
    public bool StylusHandwritingEnabled { get; }
    public bool EnableIMEPersonalizedLearning { get; }
    public bool? EnableInlinePrediction { get; }
    public EditableTextContextMenuBuilder? ContextMenuBuilder { get; }
    public SpellCheckConfiguration? SpellCheckConfiguration { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    internal bool IsBorderless { get; }

    public static TextStyle CupertinoMisspelledTextStyle { get; } = new(
        Decoration: global::Plumix.UI.TextDecoration.Underline,
        DecorationColor: CupertinoColors.SystemRed.Color,
        DecorationStyle: global::Plumix.UI.TextDecorationStyle.Dotted);

    public static Color KMisspelledSelectionColor { get; } = Color.FromUInt32(0x62FF9699);

    public static Widget DefaultSpellCheckSuggestionsToolbarBuilder(
        BuildContext context,
        EditableText.EditableTextState editableTextState)
    {
        return CupertinoSpellCheckSuggestionsToolbar.EditableText(editableTextState);
    }

    public static SpellCheckConfiguration InferIOSSpellCheckConfiguration(SpellCheckConfiguration? configuration)
    {
        if (configuration is null || !configuration.SpellCheckEnabled)
        {
            return SpellCheckConfiguration.Disabled;
        }

        return configuration.CopyWith(
            misspelledTextStyle: configuration.MisspelledTextStyle ?? CupertinoMisspelledTextStyle,
            misspelledSelectionColor: configuration.MisspelledSelectionColor ?? KMisspelledSelectionColor,
            spellCheckSuggestionsToolbarBuilder: configuration.SpellCheckSuggestionsToolbarBuilder
                                                    ?? DefaultSpellCheckSuggestionsToolbarBuilder);
    }

    public override State CreateState() => new CupertinoTextFieldState();

    private static Widget DefaultContextMenuBuilder(
        BuildContext context,
        EditableText.EditableTextState editableTextState)
    {
        return CupertinoAdaptiveTextSelectionToolbar.EditableText(editableTextState);
    }

    private static TextMagnifierConfiguration IosMagnifierConfiguration { get; } = new(
        (context, controller, info) => PlatformDefaults.TargetPlatform is TargetPlatform.Android or TargetPlatform.IOS
            ? new CupertinoTextMagnifier(controller, info)
            : null);

    private static void Validate(
        string obscuringCharacter,
        int? maxLines,
        int? minLines,
        bool expands,
        bool obscureText,
        int? maxLength,
        TextInputActionType? textInputAction,
        TextInputType? keyboardType)
    {
        if (string.IsNullOrEmpty(obscuringCharacter) || obscuringCharacter.Length != 1)
        {
            throw new ArgumentException("obscuringCharacter must contain exactly one UTF-16 character.",
                nameof(obscuringCharacter));
        }
        if (maxLines.HasValue && maxLines.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));
        if (minLines.HasValue && minLines.Value <= 0) throw new ArgumentOutOfRangeException(nameof(minLines));
        if (maxLines.HasValue && minLines.HasValue && maxLines.Value < minLines.Value)
        {
            throw new ArgumentException("minLines can't be greater than maxLines.", nameof(minLines));
        }
        if (expands && (maxLines.HasValue || minLines.HasValue))
        {
            throw new ArgumentException("minLines and maxLines must be null when expands is true.", nameof(expands));
        }
        if (obscureText && maxLines != 1)
        {
            throw new ArgumentException("Obscured fields cannot be multiline.", nameof(obscureText));
        }
        if (maxLength.HasValue && maxLength.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        if (textInputAction == TextInputActionType.Newline
            && maxLines != 1
            && keyboardType == TextInputType.Text)
        {
            throw new ArgumentException(
                "Use keyboardType TextInputType.multiline when using TextInputAction.newline on a multiline field.",
                nameof(textInputAction));
        }
    }
}

internal sealed class CupertinoTextFieldState : RestorationState
{
    private readonly GlobalKey<EditableText.EditableTextState> _editableTextKey =
        new GlobalObjectKey<EditableText.EditableTextState>(new object());
    private TextEditingController? _controller;
    private RestorableTextEditingController? _restorableController;
    private FocusNode? _focusNode;
    private bool _ownsController;
    private bool _ownsFocusNode;
    private bool _showSelectionHandles;

    private CupertinoTextField Current => (CupertinoTextField)StateWidget;

    protected override string? RestorationId => Current.RestorationId;

    public override void InitState()
    {
        AttachController(Current.Controller);
        AttachFocusNode(Current.FocusNode);
    }

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        if (_restorableController is not null)
        {
            RegisterForRestoration(_restorableController, "controller");
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (CupertinoTextField)oldWidget;
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
        _focusNode!.CanRequestFocus = Current.Enabled;
    }

    public override Widget Build(BuildContext context)
    {
        EnsureController();
        CupertinoThemeData theme = CupertinoTheme.Of(context);
        DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
        TextStyle textStyle = ResolveTextStyle(theme);
        TextStyle placeholderStyle = ResolvePlaceholderStyle(context, textStyle);
        Color primaryColor = theme.PrimaryColor.Value;
        Color cursorColor = CupertinoDynamicColor.MaybeResolve(Current.CursorColor, context)
                            ?? selectionStyle.CursorColor
                            ?? primaryColor;
        Color selectionColor = selectionStyle.SelectionColor ?? ApplyOpacity(primaryColor, 0.2);
        IReadOnlyList<TextInputFormatter>? inputFormatters = ResolveInputFormatters();
        TextSelectionControls selectionControls = Current.SelectionControls ?? PlatformSelectionControls();
        SpellCheckConfiguration spellCheckConfiguration = CupertinoTextField.InferIOSSpellCheckConfiguration(
            Current.SpellCheckConfiguration);
        bool hasAttachments = Current.Placeholder is not null
                              || Current.ClearButtonMode != OverlayVisibilityMode.Never
                              || Current.Prefix is not null
                              || Current.Suffix is not null;
        TextAlignVertical textAlignVertical = Current.TextAlignVertical
                                              ?? (hasAttachments
                                                  ? Plumix.Rendering.TextAlignVertical.Center
                                                  : Plumix.Rendering.TextAlignVertical.Top);

        Widget editable = new EditableText(
            controller: _controller!,
            focusNode: _focusNode,
            undoController: Current.UndoController,
            onChanged: Current.OnChanged,
            autofocus: Current.Autofocus,
            enabled: Current.Enabled,
            multiline: Current.MaxLines != 1,
            minLines: Current.MinLines,
            maxLines: Current.MaxLines,
            expands: Current.Expands,
            fontSize: textStyle.FontSize ?? 17.0,
            textColor: textStyle.Color ?? CupertinoColors.Label.Value,
            backgroundColor: CupertinoColors.Transparent,
            focusedBackgroundColor: CupertinoColors.Transparent,
            cursorColor: cursorColor,
            selectionColor: _focusNode!.HasFocus ? selectionColor : null,
            padding: new Thickness(0.0),
            style: textStyle,
            strutStyle: Current.StrutStyle,
            readOnly: Current.ReadOnly || !Current.Enabled,
            obscureText: Current.ObscureText,
            obscuringCharacter: Current.ObscuringCharacter,
            onEditingComplete: Current.OnEditingComplete,
            onSubmitted: Current.OnSubmitted,
            textAlign: Current.TextAlign,
            textDirection: Current.TextDirection,
            keyboardType: Current.KeyboardType,
            textInputAction: Current.TextInputAction ?? TextInputActionType.Unspecified,
            textCapitalization: Current.TextCapitalization,
            smartDashesType: Current.SmartDashesType,
            smartQuotesType: Current.SmartQuotesType,
            scrollPadding: Current.ScrollPadding.Resolve(Directionality.Of(context)),
            scrollController: Current.ScrollController,
            scrollPhysics: Current.ScrollPhysics,
            autocorrect: Current.Autocorrect,
            enableSuggestions: Current.EnableSuggestions,
            canRequestFocus: Current.Enabled,
            inputFormatters: inputFormatters,
            showCursor: Current.ShowCursor,
            cursorWidth: Current.CursorWidth,
            cursorHeight: Current.CursorHeight,
            cursorRadius: Current.CursorRadius,
            selectionHeightStyle: Current.SelectionHeightStyle ?? BoxHeightStyle.Tight,
            selectionWidthStyle: Current.SelectionWidthStyle ?? BoxWidthStyle.Tight,
            cursorOpacityAnimates: Current.CursorOpacityAnimates,
            cursorOffset: new Point(-2.0 / (MediaQuery.MaybeOf(context)?.DevicePixelRatio ?? 1.0), 0.0),
            paintCursorAboveText: true,
            enableInteractiveSelection: Current.EnableInteractiveSelection,
            selectAllOnFocus: Current.SelectAllOnFocus,
            toolbarOptions: Current.ToolbarOptions,
            contextMenuBuilder: Current.ContextMenuBuilder,
            magnifierConfiguration: Current.MagnifierConfiguration,
            selectionControls: Current.EnableInteractiveSelection ? selectionControls : null,
            showSelectionHandles: _showSelectionHandles,
            spellCheckConfiguration: spellCheckConfiguration,
            onSelectionChanged: HandleSelectionChanged,
            rendererIgnoresPointer: true,
            autofillHints: Current.AutofillHints,
            autofillHintText: Current.Placeholder,
            keyboardAppearance: Current.KeyboardAppearance ?? CupertinoTheme.BrightnessOf(context),
            enableIMEPersonalizedLearning: Current.EnableIMEPersonalizedLearning,
            enableInlinePrediction: Current.EnableInlinePrediction,
            contentInsertionConfiguration: Current.ContentInsertionConfiguration,
            scribbleEnabled: Current.ScribbleEnabled,
            stylusHandwritingEnabled: Current.StylusHandwritingEnabled,
            clipBehavior: Current.ClipBehavior,
            key: _editableTextKey);

        Widget content = hasAttachments
            ? BuildDecoratedContent(context, editable, placeholderStyle, textAlignVertical)
            : new Padding(Current.Padding, new RepaintBoundary(editable));
        content = new Align(
            alignment: new Alignment(-1.0, textAlignVertical.Y),
            widthFactor: 1.0,
            heightFactor: 1.0,
            child: content);
        content = BuildGestureDetector(content);
        content = new Container(
            decoration: ResolveDecoration(context),
            color: !Current.Enabled && Current.Decoration is null ? DisabledBackground(context) : null,
            child: content);
        content = new IgnorePointer(ignoring: !Current.Enabled, child: content);
        content = new TextFieldTapRegion(
            groupId: Current.GroupId,
            onTapOutside: Current.OnTapOutside,
            onTapUpOutside: Current.OnTapUpOutside,
            child: content);
        return new Semantics(
            enabled: Current.Enabled,
            onTap: Current.Enabled && !Current.ReadOnly ? HandleSemanticTap : null,
            child: content)
        {
            OnFocus = Current.Enabled ? HandleSemanticFocus : null,
        };
    }

    public override void Dispose()
    {
        DetachController();
        DetachFocusNode();
        base.Dispose();
    }

    private void HandleSelectionChanged(TextSelection selection, SelectionChangedCause? cause)
    {
        bool show = Current.EnableInteractiveSelection
                    && !selection.IsCollapsed
                    && cause != SelectionChangedCause.Keyboard
                    && (!string.IsNullOrEmpty(_controller?.Text)
                        || cause == SelectionChangedCause.StylusHandwriting);
        if (show == _showSelectionHandles)
        {
            return;
        }

        SetState(() => _showSelectionHandles = show);
    }

    private Widget BuildDecoratedContent(
        BuildContext context,
        Widget editable,
        TextStyle placeholderStyle,
        TextAlignVertical textAlignVertical)
    {
        Widget paddedEditable = new Padding(Current.Padding, new RepaintBoundary(editable));
        return new ValueListenableBuilder<TextEditingValue>(
            valueListenable: _controller!,
            child: paddedEditable,
            builder: (_, value, child) =>
            {
                bool hasText = !string.IsNullOrEmpty(value.Text);
                var children = new List<Widget>();
                if (Current.Prefix is not null && IsVisible(Current.PrefixMode, hasText))
                {
                    children.Add(Current.Prefix);
                }

                var middle = new List<Widget>();
                if (Current.Placeholder is not null)
                {
                    middle.Add(new Visibility(
                        visible: !hasText,
                        maintainAnimation: true,
                        maintainSize: true,
                        maintainState: true,
                        child: new SizedBox(
                            width: double.PositiveInfinity,
                            child: new Padding(
                                Current.Padding,
                                new Text(
                                    Current.Placeholder,
                                    style: placeholderStyle,
                                    maxLines: hasText ? 1 : Current.MaxLines,
                                    overflow: TextOverflow.Clip,
                                    textAlign: Current.TextAlign)))));
                }
                middle.Add(child!);
                children.Add(new Expanded(
                    new Directionality(
                        Current.TextDirection ?? Directionality.Of(context),
                        new CupertinoBaselineAlignedStack(
                            textAlignVertical,
                            middle))));

                Widget? suffix = ResolveSuffix(context, hasText);
                if (suffix is not null)
                {
                    children.Add(suffix);
                }

                return new Row(
                    crossAxisAlignment: Current.CrossAxisAlignment,
                    children: children);
            });
    }

    private Widget BuildGestureDetector(Widget child)
    {
        Widget result = new Listener(
            behavior: HitTestBehavior.Translucent,
            onPointerDown: @event => _editableTextKey.CurrentState?.HandlePointerDown(@event),
            onPointerMove: @event => _editableTextKey.CurrentState?.HandlePointerMove(@event),
            onPointerUp: @event => _editableTextKey.CurrentState?.HandlePointerUp(@event),
            onPointerCancel: @event => _editableTextKey.CurrentState?.HandlePointerCancel(@event),
            child: child);
        return new GestureDetector(
            behavior: HitTestBehavior.Translucent,
            excludeFromSemantics: true,
            dragStartBehavior: Current.DragStartBehavior,
            onTap: Current.OnTap,
            onDoubleTap: () => _editableTextKey.CurrentState?.HandleDoubleTap(),
            onLongPress: () => _editableTextKey.CurrentState?.HandleLongPress(),
            onSecondaryTap: () => _editableTextKey.CurrentState?.ShowToolbar(),
            child: result);
    }

    private Widget? ResolveSuffix(BuildContext context, bool hasText)
    {
        if (Current.Suffix is not null && IsVisible(Current.SuffixMode, hasText))
        {
            return Current.Suffix;
        }
        if (!IsVisible(Current.ClearButtonMode, hasText))
        {
            return null;
        }

        string label = Current.ClearButtonSemanticLabel ?? CupertinoLocalizations.Of(context).ClearButtonLabel;
        Color color = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark
            ? Color.FromUInt32(0x33FFFFFF)
            : Color.FromUInt32(0x33000000);
        return new Semantics(
            flags: SemanticsFlags.IsButton,
            label: label,
            onTap: Current.Enabled ? HandleClear : null,
            child: new GestureDetector(
                onTap: Current.Enabled ? HandleClear : null,
                child: new Padding(
                    EdgeInsetsGeometry.Symmetric(horizontal: 6.0),
                    new Icon(CupertinoIcons.ClearThickCircled, size: 18.0, color: color))));
    }

    private TextStyle ResolveTextStyle(CupertinoThemeData theme)
    {
        TextStyle style = theme.TextTheme.TextStyle.Merge(Current.Style);
        if (Current.StrutStyle is { } strut)
        {
            style = style.CopyWith(
                fontFamily: strut.FontFamily,
                fontSize: strut.FontSize,
                fontWeight: strut.FontWeight,
                fontStyle: strut.FontStyle,
                height: strut.Height);
        }
        return style;
    }

    private TextStyle ResolvePlaceholderStyle(BuildContext context, TextStyle textStyle)
    {
        TextStyle placeholder = Current.PlaceholderStyle;
        if (Equals(placeholder, CupertinoTextField.DefaultPlaceholderStyle))
        {
            placeholder = placeholder.CopyWith(
                color: CupertinoDynamicColor.Resolve(CupertinoColors.PlaceholderText, context));
        }
        return textStyle.Merge(placeholder);
    }

    private Decoration? ResolveDecoration(BuildContext context)
    {
        if (Current.Decoration is null)
        {
            return null;
        }
        if (!Equals(Current.Decoration, CupertinoTextField.DefaultRoundedBorderDecoration))
        {
            return Current.Decoration;
        }

        bool dark = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark;
        return new BoxDecoration(
            Color: Current.Enabled
                ? dark ? CupertinoColors.Black : CupertinoColors.White
                : DisabledBackground(context),
            Border: Border.All(dark ? Color.FromUInt32(0x33FFFFFF) : Color.FromUInt32(0x33000000), 0.0),
            BorderRadius: BorderRadius.Circular(5.0));
    }

    private IReadOnlyList<TextInputFormatter>? ResolveInputFormatters()
    {
        if (Current.MaxLength is null)
        {
            return Current.InputFormatters;
        }

        var formatters = Current.InputFormatters?.ToList() ?? [];
        formatters.Add(new LengthLimitingTextInputFormatter(
            Current.MaxLength,
            Current.MaxLengthEnforcement));
        return formatters;
    }

    private void HandleClear()
    {
        bool hadText = !string.IsNullOrEmpty(_controller!.Text);
        _controller.Clear();
        if (hadText)
        {
            Current.OnChanged?.Invoke(string.Empty);
        }
    }

    private void HandleSemanticTap()
    {
        TextSelection selection = _controller!.Selection;
        if (!selection.IsValid)
        {
            _controller.Selection = TextSelection.Collapsed(_controller.Text.Length);
        }
        _focusNode!.RequestFocus();
        _editableTextKey.CurrentState?.RequestKeyboard();
    }

    private void HandleSemanticFocus()
    {
        if (!_focusNode!.HasFocus)
        {
            _focusNode.RequestFocus();
        }
        else if (!Current.ReadOnly)
        {
            _editableTextKey.CurrentState?.RequestKeyboard();
        }
    }

    private void AttachController(TextEditingController? external)
    {
        if (external is null)
        {
            _restorableController = new RestorableTextEditingController();
            if (!RestorePending)
            {
                RegisterForRestoration(_restorableController, "controller");
            }
            _ownsController = true;
            return;
        }

        _controller = external;
        _controller.AddListener(HandleControllerChanged);
    }

    private void EnsureController()
    {
        if (_restorableController is null)
        {
            return;
        }
        TextEditingController restored = _restorableController.Value;
        if (ReferenceEquals(_controller, restored))
        {
            return;
        }
        _controller?.RemoveListener(HandleControllerChanged);
        _controller = restored;
        _controller.AddListener(HandleControllerChanged);
    }

    private void DetachController()
    {
        _controller?.RemoveListener(HandleControllerChanged);
        if (_ownsController && _restorableController is not null)
        {
            if (_restorableController.RegisteredRestorationId is not null)
            {
                UnregisterFromRestoration(_restorableController);
            }
            _restorableController.Dispose();
        }
        _controller = null;
        _restorableController = null;
        _ownsController = false;
    }

    private void AttachFocusNode(FocusNode? external)
    {
        _focusNode = external ?? new FocusNode();
        _ownsFocusNode = external is null;
        _focusNode.CanRequestFocus = Current.Enabled;
        _focusNode.AddListener(HandleFocusChanged);
    }

    private void DetachFocusNode()
    {
        if (_focusNode is null)
        {
            return;
        }
        _focusNode.RemoveListener(HandleFocusChanged);
        if (_ownsFocusNode)
        {
            _focusNode.Dispose();
        }
        _focusNode = null;
        _ownsFocusNode = false;
    }

    private void HandleControllerChanged()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    private void HandleFocusChanged()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    private static bool IsVisible(OverlayVisibilityMode mode, bool hasText)
    {
        return mode switch
        {
            OverlayVisibilityMode.Never => false,
            OverlayVisibilityMode.Always => true,
            OverlayVisibilityMode.Editing => hasText,
            OverlayVisibilityMode.NotEditing => !hasText,
            _ => false,
        };
    }

    private static Color DisabledBackground(BuildContext context)
    {
        return CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark
            ? Color.FromUInt32(0xFF050505)
            : Color.FromUInt32(0xFFFAFAFA);
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        return Color.FromArgb(
            (byte)Math.Round(color.A * Math.Clamp(opacity, 0.0, 1.0)),
            color.R,
            color.G,
            color.B);
    }

    private static TextSelectionControls PlatformSelectionControls()
    {
#pragma warning disable CS0618
        return PlatformDefaults.TargetPlatform switch
        {
            TargetPlatform.Linux or TargetPlatform.MacOS or TargetPlatform.Windows =>
                CupertinoDesktopTextSelectionControls.HandleControls,
            _ => CupertinoTextSelectionHandleControls.Instance,
        };
#pragma warning restore CS0618
    }
}

internal sealed class CupertinoBaselineAlignedStackParentData : ContainerBoxParentData<RenderBox>
{
}

internal sealed class CupertinoBaselineAlignedStack : MultiChildRenderObjectWidget
{
    public CupertinoBaselineAlignedStack(
        TextAlignVertical textAlignVertical,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(children, key)
    {
        TextAlignVertical = textAlignVertical;
    }

    public TextAlignVertical TextAlignVertical { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoBaselineAlignedStack(TextAlignVertical);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderCupertinoBaselineAlignedStack)renderObject).TextAlignVertical = TextAlignVertical;
    }
}

internal sealed class RenderCupertinoBaselineAlignedStack : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, CupertinoBaselineAlignedStackParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, CupertinoBaselineAlignedStackParentData> _children;
    private TextAlignVertical _textAlignVertical;

    public RenderCupertinoBaselineAlignedStack(TextAlignVertical textAlignVertical)
    {
        _textAlignVertical = textAlignVertical;
        _children = new RenderBoxContainerDefaultsMixin<RenderBox, CupertinoBaselineAlignedStackParentData>(this);
    }

    public TextAlignVertical TextAlignVertical
    {
        get => _textAlignVertical;
        set
        {
            if (_textAlignVertical == value)
            {
                return;
            }
            _textAlignVertical = value;
            MarkNeedsLayout();
        }
    }

    public RenderBox? FirstChild => _children.FirstChild;
    public RenderBox? LastChild => _children.LastChild;
    public int ChildCount => _children.ChildCount;
    public RenderBox? ChildBefore(RenderBox child) => _children.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _children.ChildAfter(child);
    public void AddAll(List<RenderBox> children) => _children.AddAll(children);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not CupertinoBaselineAlignedStackParentData)
        {
            child.parentData = new CupertinoBaselineAlignedStackParentData();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) => MaxIntrinsic(
        child => child.GetMinIntrinsicWidth(height));

    protected override double ComputeMaxIntrinsicWidth(double height) => MaxIntrinsic(
        child => child.GetMaxIntrinsicWidth(height));

    protected override double ComputeMinIntrinsicHeight(double width) => MaxIntrinsic(
        child => child.GetMinIntrinsicHeight(width));

    protected override double ComputeMaxIntrinsicHeight(double width) => MaxIntrinsic(
        child => child.GetMaxIntrinsicHeight(width));

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return ComputeSize(constraints, dry: true);
    }

    protected override void PerformLayout()
    {
        Size = ComputeSize(Constraints, dry: false);
        PositionChildren();
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        _children.DefaultPaint(context, offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        RenderBox? editable = LastChild;
        if (editable is null)
        {
            return false;
        }
        var parentData = (CupertinoBaselineAlignedStackParentData)editable.parentData!;
        RenderBox child = editable;
        return result.AddWithPaintOffset(
            parentData.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _children.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _children.Move(child, after);
    public void Remove(RenderBox child) => _children.Remove(child);
    public void DefaultPaint(PaintingContext context, Point offset) => _children.DefaultPaint(context, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _children.DefaultHitTestChildren(result, position);

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private Size ComputeSize(BoxConstraints constraints, bool dry)
    {
        double width = constraints.MinWidth;
        double height = constraints.MinHeight;
        double maxAscent = 0.0;
        double maxDescent = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            Size childSize = dry ? child.GetDryLayout(constraints) : LayoutChild(child, constraints);
            double baseline = dry
                ? child.GetDryBaseline(constraints, TextBaseline.Alphabetic) ?? childSize.Height
                : child.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true) ?? childSize.Height;
            width = Math.Max(width, childSize.Width);
            height = Math.Max(height, childSize.Height);
            maxAscent = Math.Max(maxAscent, baseline);
            maxDescent = Math.Max(maxDescent, childSize.Height - baseline);
        }
        height = Math.Max(height, maxAscent + maxDescent);
        return constraints.Constrain(new Size(width, height));
    }

    private void PositionChildren()
    {
        RenderBox? editable = LastChild;
        if (editable is null)
        {
            return;
        }
        double editableBaseline = editable.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true)
                                  ?? editable.Size.Height;
        Point editableOffset = new Alignment(0.0, TextAlignVertical.Y).AlongOffset(Size, editable.Size);
        ((CupertinoBaselineAlignedStackParentData)editable.parentData!).offset = editableOffset;
        for (RenderBox? child = FirstChild; child is not null && !ReferenceEquals(child, editable);
             child = ChildAfter(child))
        {
            double childBaseline = child.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true)
                                   ?? child.Size.Height;
            ((CupertinoBaselineAlignedStackParentData)child.parentData!).offset =
                editableOffset + new Vector(0.0, editableBaseline - childBaseline);
        }
    }

    private double MaxIntrinsic(Func<RenderBox, double> measure)
    {
        double result = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            result = Math.Max(result, measure(child));
        }
        return result;
    }

    private static Size LayoutChild(RenderBox child, BoxConstraints constraints)
    {
        child.Layout(constraints, parentUsesSize: true);
        return child.Size;
    }
}
