using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart

/// <summary>
/// The type of information for which to optimize the text input control.
/// </summary>
/// <remarks>
/// Dart models this as a hand-rolled immutable value rather than an enum, because
/// <see cref="NumberWithOptions"/> carries two extra flags.
/// </remarks>
public sealed class TextInputType : IEquatable<TextInputType>
{
    private static readonly string[] Names =
    [
        "text",
        "multiline",
        "number",
        "phone",
        "datetime",
        "emailAddress",
        "url",
        "visiblePassword",
        "name",
        "address",
        "none",
        "webSearch",
        "twitter",
    ];

    private TextInputType(int index, bool? signed = null, bool? isDecimal = null)
    {
        Index = index;
        Signed = signed;
        Decimal = isDecimal;
    }

    /// <summary>Optimize for textual information.</summary>
    public static TextInputType Text { get; } = new(0);

    /// <summary>Optimize for multiline textual information.</summary>
    public static TextInputType Multiline { get; } = new(1);

    /// <summary>Optimize for unsigned numerical information without a decimal point.</summary>
    public static TextInputType Number { get; } = NumberWithOptions();

    /// <summary>Optimize for telephone numbers.</summary>
    public static TextInputType Phone { get; } = new(3);

    /// <summary>Optimize for date and time information.</summary>
    public static TextInputType Datetime { get; } = new(4);

    /// <summary>Optimize for email addresses.</summary>
    public static TextInputType EmailAddress { get; } = new(5);

    /// <summary>Optimize for URLs.</summary>
    public static TextInputType Url { get; } = new(6);

    /// <summary>Optimize for passwords that are visible to the user.</summary>
    public static TextInputType VisiblePassword { get; } = new(7);

    /// <summary>Optimize for a person's name.</summary>
    public static TextInputType Name { get; } = new(8);

    /// <summary>Optimize for postal mailing addresses.</summary>
    public static TextInputType StreetAddress { get; } = new(9);

    /// <summary>Prevent the OS from showing the on-screen virtual keyboard.</summary>
    public static TextInputType None { get; } = new(10);

    /// <summary>Optimize for web searches.</summary>
    public static TextInputType WebSearch { get; } = new(11);

    /// <summary>Optimize for social media handles.</summary>
    public static TextInputType Twitter { get; } = new(12);

    /// <summary>All the predefined input types, in Dart's declaration order.</summary>
    public static IReadOnlyList<TextInputType> Values { get; } =
    [
        Text,
        Multiline,
        Number,
        Phone,
        Datetime,
        EmailAddress,
        Url,
        VisiblePassword,
        Name,
        StreetAddress,
        None,
        WebSearch,
        Twitter,
    ];

    /// <summary>Optimize for numerical information, with optional sign and decimal point.</summary>
    public static TextInputType NumberWithOptions(bool signed = false, bool isDecimal = false) =>
        new(2, signed, isDecimal);

    /// <summary>The index of this input type within <see cref="Values"/>.</summary>
    public int Index { get; }

    /// <summary>Whether the number format includes a sign; <c>null</c> outside number types.
    /// </summary>
    public bool? Signed { get; }

    /// <summary>Whether the number format includes a decimal point; <c>null</c> outside number
    /// types.</summary>
    public bool? Decimal { get; }

    /// <summary>The wire name of this input type, for example <c>TextInputType.emailAddress</c>.
    /// </summary>
    public string DartName => $"TextInputType.{Names[Index]}";

    /// <summary>The JSON payload the host receives inside a text input configuration.</summary>
    public Dictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>
        {
            ["name"] = DartName,
            ["signed"] = Signed,
            ["decimal"] = Decimal,
        };
    }

    /// <inheritdoc/>
    public bool Equals(TextInputType? other) =>
        other is not null
        && other.Index == Index
        && other.Signed == Signed
        && other.Decimal == Decimal;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as TextInputType);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Index, Signed, Decimal);

    /// <inheritdoc/>
    public override string ToString() =>
        $"TextInputType(name: {DartName}, signed: {DartBool(Signed)}, decimal: {DartBool(Decimal)})";

    /// <summary>Whether two input types carry the same values.</summary>
    public static bool operator ==(TextInputType? left, TextInputType? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two input types carry different values.</summary>
    public static bool operator !=(TextInputType? left, TextInputType? right) => !(left == right);

    private static string DartBool(bool? value) => value is null ? "null" : value.Value ? "true" : "false";
}

/// <summary>
/// An action the user has requested the text input control to perform.
/// </summary>
/// <remarks>Dart calls this <c>TextInputAction</c>; the C# name keeps the <c>Type</c> suffix so it
/// does not collide with the <c>TextInputAction</c> property that carries it.</remarks>
public enum TextInputActionType
{
    /// <summary>Logical meaning: there is no relevant input action for this control.</summary>
    None,

    /// <summary>Logical meaning: let the OS decide which action is most appropriate.</summary>
    Unspecified,

    /// <summary>Logical meaning: the user is done providing input.</summary>
    Done,

    /// <summary>Logical meaning: the user has entered some text representing a destination.</summary>
    Go,

    /// <summary>Logical meaning: execute a search query.</summary>
    Search,

    /// <summary>Logical meaning: sends something that the user has composed.</summary>
    Send,

    /// <summary>Logical meaning: the user is done with the current input source and wants the next.
    /// </summary>
    Next,

    /// <summary>Logical meaning: the user wishes to return to the previous input source.</summary>
    Previous,

    /// <summary>Logical meaning: the user is done with the current input source and wants to move to
    /// the next one.</summary>
    ContinueAction,

    /// <summary>Logical meaning: the user wants to join something.</summary>
    Join,

    /// <summary>Logical meaning: the user is routing to another destination.</summary>
    Route,

    /// <summary>Logical meaning: initiate a call.</summary>
    EmergencyCall,

    /// <summary>Logical meaning: insert a newline character in the focused text input.</summary>
    Newline,
}

/// <summary>Configures how the platform keyboard capitalizes text entry.</summary>
public enum TextCapitalization
{
    /// <summary>Capitalize the first letter of every word.</summary>
    Words,

    /// <summary>Capitalize the first letter of every sentence.</summary>
    Sentences,

    /// <summary>Capitalize every letter.</summary>
    Characters,

    /// <summary>Do not change the capitalization of the text.</summary>
    None,
}

/// <summary>Indicates how the platform may replace dashes with typographic ones.</summary>
public enum SmartDashesType
{
    /// <summary>Smart dashes are disabled.</summary>
    Disabled,

    /// <summary>Smart dashes are enabled.</summary>
    Enabled,
}

/// <summary>Indicates how the platform may replace quotes with typographic ones.</summary>
public enum SmartQuotesType
{
    /// <summary>Smart quotes are disabled.</summary>
    Disabled,

    /// <summary>Smart quotes are enabled.</summary>
    Enabled,
}

/// <summary>Translates between <see cref="TextInputActionType"/> and its wire encoding.</summary>
public static class TextInputActions
{
    private static readonly Dictionary<string, TextInputActionType> ByName = new(StringComparer.Ordinal)
    {
        ["TextInputAction.none"] = TextInputActionType.None,
        ["TextInputAction.unspecified"] = TextInputActionType.Unspecified,
        ["TextInputAction.go"] = TextInputActionType.Go,
        ["TextInputAction.search"] = TextInputActionType.Search,
        ["TextInputAction.send"] = TextInputActionType.Send,
        ["TextInputAction.next"] = TextInputActionType.Next,
        ["TextInputAction.previous"] = TextInputActionType.Previous,
        ["TextInputAction.continueAction"] = TextInputActionType.ContinueAction,
        ["TextInputAction.join"] = TextInputActionType.Join,
        ["TextInputAction.route"] = TextInputActionType.Route,
        ["TextInputAction.emergencyCall"] = TextInputActionType.EmergencyCall,
        ["TextInputAction.done"] = TextInputActionType.Done,
        ["TextInputAction.newline"] = TextInputActionType.Newline,
    };

    /// <summary>The wire name of an action, for example <c>TextInputAction.emergencyCall</c>.
    /// </summary>
    public static string ToDartName(this TextInputActionType action) =>
        $"TextInputAction.{TextInputEncoding.CamelCase(action.ToString())}";

    /// <summary>Parses the wire name of an action.</summary>
    /// <exception cref="ArgumentException">The name is not a known action.</exception>
    public static TextInputActionType Parse(string action)
    {
        if (ByName.TryGetValue(action, out TextInputActionType parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Unknown text input action: {action}", nameof(action));
    }
}

/// <summary>Shared wire-encoding helpers for the text input service.</summary>
internal static class TextInputEncoding
{
    internal static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}

/// <summary>
/// Controls the visual appearance of the text input control.
/// </summary>
/// <remarks>
/// Dart's <c>TextInputConfiguration</c> is an <c>@immutable class</c> that
/// <see cref="AutofillScopeTextInputConfiguration"/> subclasses to append the scope's
/// <c>fields</c> entry, so this is a class rather than a value type.
/// </remarks>
public class TextInputConfiguration : IEquatable<TextInputConfiguration>
{
    /// <summary>Creates configuration information for a text input control.</summary>
    public TextInputConfiguration(
        int? viewId = null,
        TextInputType? inputType = null,
        bool readOnly = false,
        bool obscureText = false,
        bool autocorrect = true,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        bool enableSuggestions = true,
        bool enableInteractiveSelection = true,
        string? actionLabel = null,
        TextInputActionType inputAction = TextInputActionType.Done,
        PlatformBrightness keyboardAppearance = PlatformBrightness.Light,
        TextCapitalization textCapitalization = TextCapitalization.None,
        AutofillConfiguration? autofillConfiguration = null,
        bool enableIMEPersonalizedLearning = true,
        IReadOnlyList<string>? allowedMimeTypes = null,
        bool enableDeltaModel = false,
        IReadOnlyList<Locale>? hintLocales = null,
        bool? enableInlinePrediction = null)
    {
        ViewId = viewId;
        InputType = inputType ?? TextInputType.Text;
        ReadOnly = readOnly;
        ObscureText = obscureText;
        Autocorrect = autocorrect;
        SmartDashesType = smartDashesType ?? (obscureText ? SmartDashesType.Disabled : SmartDashesType.Enabled);
        SmartQuotesType = smartQuotesType ?? (obscureText ? SmartQuotesType.Disabled : SmartQuotesType.Enabled);
        EnableSuggestions = enableSuggestions;
        EnableInteractiveSelection = enableInteractiveSelection;
        ActionLabel = actionLabel;
        InputAction = inputAction;
        KeyboardAppearance = keyboardAppearance;
        TextCapitalization = textCapitalization;
        AutofillConfiguration = autofillConfiguration ?? AutofillConfiguration.Disabled;
        EnableIMEPersonalizedLearning = enableIMEPersonalizedLearning;
        AllowedMimeTypes = allowedMimeTypes ?? [];
        EnableDeltaModel = enableDeltaModel;
        HintLocales = hintLocales ?? [];
        EnableInlinePrediction = enableInlinePrediction;
    }

    /// <summary>The ID of the view this configuration belongs to.</summary>
    public int? ViewId { get; }

    /// <summary>The type of information for which to optimize the text input control.</summary>
    public TextInputType InputType { get; }

    /// <summary>Whether the text field can be edited.</summary>
    public bool ReadOnly { get; }

    /// <summary>Whether to hide the text being edited.</summary>
    public bool ObscureText { get; }

    /// <summary>Whether to enable autocorrection.</summary>
    public bool Autocorrect { get; }

    /// <summary>Whether to allow the platform to automatically format dashes.</summary>
    public SmartDashesType SmartDashesType { get; }

    /// <summary>Whether to allow the platform to automatically format quotes.</summary>
    public SmartQuotesType SmartQuotesType { get; }

    /// <summary>Whether to show input suggestions as the user types.</summary>
    public bool EnableSuggestions { get; }

    /// <summary>Whether the user can change the text selection.</summary>
    public bool EnableInteractiveSelection { get; }

    /// <summary>What text to display in the text input control's action button.</summary>
    public string? ActionLabel { get; }

    /// <summary>What kind of action to request for the action button.</summary>
    public TextInputActionType InputAction { get; }

    /// <summary>The appearance of the keyboard.</summary>
    public PlatformBrightness KeyboardAppearance { get; }

    /// <summary>How to capitalize the text the user enters.</summary>
    public TextCapitalization TextCapitalization { get; }

    /// <summary>The autofill configuration of the client. Never <c>null</c>; defaults to
    /// <see cref="Plumix.UI.AutofillConfiguration.Disabled"/>.</summary>
    public AutofillConfiguration AutofillConfiguration { get; }

    /// <summary>Whether the platform may personalize its models from this input.</summary>
    public bool EnableIMEPersonalizedLearning { get; }

    /// <summary>The content MIME types the input control accepts.</summary>
    public IReadOnlyList<string> AllowedMimeTypes { get; }

    /// <summary>Whether the client requests editing deltas rather than whole values.</summary>
    public bool EnableDeltaModel { get; }

    /// <summary>The language hints for the input control.</summary>
    public IReadOnlyList<Locale> HintLocales { get; }

    /// <summary>Whether the platform should show inline predictions.</summary>
    public bool? EnableInlinePrediction { get; }

    /// <summary>Whether the field accepts more than one line.</summary>
    /// <remarks>Dart derives this the same way, from <c>inputType == TextInputType.multiline</c>.
    /// </remarks>
    public bool IsMultiline => InputType == TextInputType.Multiline;

    /// <summary>Creates a copy of this configuration with the given fields replaced.</summary>
    public TextInputConfiguration CopyWith(
        int? viewId = null,
        TextInputType? inputType = null,
        bool? readOnly = null,
        bool? obscureText = null,
        bool? autocorrect = null,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        bool? enableSuggestions = null,
        bool? enableInteractiveSelection = null,
        string? actionLabel = null,
        TextInputActionType? inputAction = null,
        PlatformBrightness? keyboardAppearance = null,
        TextCapitalization? textCapitalization = null,
        bool? enableIMEPersonalizedLearning = null,
        IReadOnlyList<string>? allowedMimeTypes = null,
        AutofillConfiguration? autofillConfiguration = null,
        bool? enableDeltaModel = null,
        IReadOnlyList<Locale>? hintLocales = null,
        bool? enableInlinePrediction = null)
    {
        return new TextInputConfiguration(
            viewId: viewId ?? ViewId,
            inputType: inputType ?? InputType,
            readOnly: readOnly ?? ReadOnly,
            obscureText: obscureText ?? ObscureText,
            autocorrect: autocorrect ?? Autocorrect,
            smartDashesType: smartDashesType ?? SmartDashesType,
            smartQuotesType: smartQuotesType ?? SmartQuotesType,
            enableSuggestions: enableSuggestions ?? EnableSuggestions,
            enableInteractiveSelection: enableInteractiveSelection ?? EnableInteractiveSelection,
            actionLabel: actionLabel ?? ActionLabel,
            inputAction: inputAction ?? InputAction,
            keyboardAppearance: keyboardAppearance ?? KeyboardAppearance,
            textCapitalization: textCapitalization ?? TextCapitalization,
            autofillConfiguration: autofillConfiguration ?? AutofillConfiguration,
            enableIMEPersonalizedLearning: enableIMEPersonalizedLearning ?? EnableIMEPersonalizedLearning,
            allowedMimeTypes: allowedMimeTypes ?? AllowedMimeTypes,
            enableDeltaModel: enableDeltaModel ?? EnableDeltaModel,
            hintLocales: hintLocales ?? HintLocales,
            enableInlinePrediction: enableInlinePrediction ?? EnableInlinePrediction);
    }

    /// <summary>The JSON payload the host receives with <c>TextInput.setClient</c>.</summary>
    public virtual Dictionary<string, object?> ToJson()
    {
        Dictionary<string, object?>? autofill = AutofillConfiguration.ToJson();
        var json = new Dictionary<string, object?>
        {
            ["viewId"] = ViewId,
            ["inputType"] = InputType.ToJson(),
            ["readOnly"] = ReadOnly,
            ["obscureText"] = ObscureText,
            ["autocorrect"] = Autocorrect,
            ["smartDashesType"] = ((int)SmartDashesType).ToString(),
            ["smartQuotesType"] = ((int)SmartQuotesType).ToString(),
            ["enableSuggestions"] = EnableSuggestions,
            ["enableInteractiveSelection"] = EnableInteractiveSelection,
            ["actionLabel"] = ActionLabel,
            ["inputAction"] = InputAction.ToDartName(),
            ["textCapitalization"] = $"TextCapitalization.{TextInputEncoding.CamelCase(TextCapitalization.ToString())}",
            ["keyboardAppearance"] =
                $"Brightness.{TextInputEncoding.CamelCase(KeyboardAppearance.ToString())}",
            ["enableIMEPersonalizedLearning"] = EnableIMEPersonalizedLearning,
            ["contentCommitMimeTypes"] = AllowedMimeTypes,
        };
        if (autofill != null)
        {
            json["autofill"] = autofill;
        }

        json["enableDeltaModel"] = EnableDeltaModel;
        json["hintLocales"] = HintLocales.Select(locale => locale.ToLanguageTag()).ToList();
        json["enableInlinePrediction"] = EnableInlinePrediction;
        return json;
    }

    /// <inheritdoc/>
    public bool Equals(TextInputConfiguration? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && other.GetType() == GetType()
               && other.ViewId == ViewId
               && other.InputType == InputType
               && other.ReadOnly == ReadOnly
               && other.ObscureText == ObscureText
               && other.Autocorrect == Autocorrect
               && other.SmartDashesType == SmartDashesType
               && other.SmartQuotesType == SmartQuotesType
               && other.EnableSuggestions == EnableSuggestions
               && other.EnableInteractiveSelection == EnableInteractiveSelection
               && other.ActionLabel == ActionLabel
               && other.InputAction == InputAction
               && other.KeyboardAppearance == KeyboardAppearance
               && other.TextCapitalization == TextCapitalization
               && other.AutofillConfiguration.Equals(AutofillConfiguration)
               && other.EnableIMEPersonalizedLearning == EnableIMEPersonalizedLearning
               && other.AllowedMimeTypes.SequenceEqual(AllowedMimeTypes)
               && other.EnableDeltaModel == EnableDeltaModel
               && other.HintLocales.SequenceEqual(HintLocales)
               && other.EnableInlinePrediction == EnableInlinePrediction;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as TextInputConfiguration);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ViewId);
        hash.Add(InputType);
        hash.Add(ReadOnly);
        hash.Add(ObscureText);
        hash.Add(Autocorrect);
        hash.Add(SmartDashesType);
        hash.Add(SmartQuotesType);
        hash.Add(EnableSuggestions);
        hash.Add(EnableInteractiveSelection);
        hash.Add(ActionLabel);
        hash.Add(InputAction);
        hash.Add(KeyboardAppearance);
        hash.Add(TextCapitalization);
        hash.Add(AutofillConfiguration);
        hash.Add(EnableIMEPersonalizedLearning);
        foreach (string mimeType in AllowedMimeTypes)
        {
            hash.Add(mimeType);
        }

        hash.Add(EnableDeltaModel);
        hash.Add(EnableInlinePrediction);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var description = new List<string>();
        if (ViewId is not null)
        {
            description.Add($"viewId: {ViewId}");
        }

        description.Add($"inputType: {InputType}");
        description.Add($"readOnly: {ReadOnly}");
        description.Add($"obscureText: {ObscureText}");
        description.Add($"autocorrect: {Autocorrect}");
        description.Add($"smartDashesType: {SmartDashesType}");
        description.Add($"smartQuotesType: {SmartQuotesType}");
        description.Add($"enableSuggestions: {EnableSuggestions}");
        description.Add($"enableInteractiveSelection: {EnableInteractiveSelection}");
        if (ActionLabel is not null)
        {
            description.Add($"actionLabel: {ActionLabel}");
        }

        description.Add($"inputAction: {InputAction}");
        description.Add($"keyboardAppearance: {KeyboardAppearance}");
        description.Add($"textCapitalization: {TextCapitalization}");
        description.Add($"autofillConfiguration: {AutofillConfiguration}");
        description.Add($"enableIMEPersonalizedLearning: {EnableIMEPersonalizedLearning}");
        description.Add($"allowedMimeTypes: [{string.Join(", ", AllowedMimeTypes)}]");
        description.Add($"enableDeltaModel: {EnableDeltaModel}");
        description.Add($"hintLocales: [{string.Join(", ", HintLocales.Select(locale => locale.Name))}]");
        if (EnableInlinePrediction is not null)
        {
            description.Add($"enableInlinePrediction: {EnableInlinePrediction}");
        }

        return $"TextInputConfiguration({string.Join(", ", description)})";
    }

    /// <summary>Whether two configurations carry the same values.</summary>
    public static bool operator ==(TextInputConfiguration? left, TextInputConfiguration? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two configurations carry different values.</summary>
    public static bool operator !=(TextInputConfiguration? left, TextInputConfiguration? right) =>
        !(left == right);
}
