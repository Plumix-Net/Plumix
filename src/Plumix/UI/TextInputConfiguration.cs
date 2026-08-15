using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart

public enum TextInputKeyboardType
{
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
}

public enum TextInputActionType
{
    Unspecified,
    None,
    Search,
    Done,
    Go,
    Next,
    Send,
}

public enum TextCapitalization
{
    Words,
    Sentences,
    Characters,
    None,
}

public enum SmartDashesType
{
    Disabled,
    Enabled,
}

public enum SmartQuotesType
{
    Disabled,
    Enabled,
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
        int viewId = 0,
        TextInputKeyboardType keyboardType = TextInputKeyboardType.Text,
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
        bool enableIMEPersonalizedLearning = true,
        IReadOnlyList<string>? allowedMimeTypes = null,
        AutofillConfiguration? autofillConfiguration = null,
        bool enableDeltaModel = false,
        IReadOnlyList<Locale>? hintLocales = null,
        bool? enableInlinePrediction = null,
        bool multiline = false)
    {
        ViewId = viewId;
        KeyboardType = keyboardType;
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
        EnableIMEPersonalizedLearning = enableIMEPersonalizedLearning;
        AllowedMimeTypes = allowedMimeTypes ?? [];
        AutofillConfiguration = autofillConfiguration ?? AutofillConfiguration.Disabled;
        EnableDeltaModel = enableDeltaModel;
        HintLocales = hintLocales;
        EnableInlinePrediction = enableInlinePrediction;
        Multiline = multiline;
    }

    /// <summary>The ID of the view this configuration belongs to.</summary>
    public int ViewId { get; }

    /// <summary>The type of information for which to optimize the text input control.</summary>
    public TextInputKeyboardType KeyboardType { get; }

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

    /// <summary>Whether the platform may personalize its models from this input.</summary>
    public bool EnableIMEPersonalizedLearning { get; }

    /// <summary>The content MIME types the input control accepts.</summary>
    public IReadOnlyList<string> AllowedMimeTypes { get; }

    /// <summary>The autofill configuration of the client. Never <c>null</c>; defaults to
    /// <see cref="Plumix.UI.AutofillConfiguration.Disabled"/>.</summary>
    public AutofillConfiguration AutofillConfiguration { get; }

    /// <summary>Whether the client requests editing deltas rather than whole values.</summary>
    public bool EnableDeltaModel { get; }

    /// <summary>The language hints for the input control.</summary>
    public IReadOnlyList<Locale>? HintLocales { get; }

    /// <summary>Whether the platform should show inline predictions.</summary>
    public bool? EnableInlinePrediction { get; }

    /// <summary>Whether the field accepts more than one line.</summary>
    /// <remarks>Dart derives this from <c>TextInputType.multiline</c>; Plumix's keyboard type is a
    /// flat enum, so the flag is carried separately.</remarks>
    public bool Multiline { get; }

    /// <summary>Creates a copy of this configuration with the given fields replaced.</summary>
    public TextInputConfiguration CopyWith(
        int? viewId = null,
        TextInputKeyboardType? keyboardType = null,
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
        bool? enableInlinePrediction = null,
        bool? multiline = null)
    {
        return new TextInputConfiguration(
            viewId: viewId ?? ViewId,
            keyboardType: keyboardType ?? KeyboardType,
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
            enableIMEPersonalizedLearning: enableIMEPersonalizedLearning ?? EnableIMEPersonalizedLearning,
            allowedMimeTypes: allowedMimeTypes ?? AllowedMimeTypes,
            autofillConfiguration: autofillConfiguration ?? AutofillConfiguration,
            enableDeltaModel: enableDeltaModel ?? EnableDeltaModel,
            hintLocales: hintLocales ?? HintLocales,
            enableInlinePrediction: enableInlinePrediction ?? EnableInlinePrediction,
            multiline: multiline ?? Multiline);
    }

    /// <summary>The JSON payload the host receives with <c>TextInput.setClient</c>.</summary>
    public virtual Dictionary<string, object?> ToJson()
    {
        Dictionary<string, object?>? autofill = AutofillConfiguration.ToJson();
        var json = new Dictionary<string, object?>
        {
            ["viewId"] = ViewId,
            ["inputType"] = KeyboardTypeToJson(KeyboardType, Multiline),
            ["readOnly"] = ReadOnly,
            ["obscureText"] = ObscureText,
            ["autocorrect"] = Autocorrect,
            ["smartDashesType"] = ((int)SmartDashesType).ToString(),
            ["smartQuotesType"] = ((int)SmartQuotesType).ToString(),
            ["enableSuggestions"] = EnableSuggestions,
            ["enableInteractiveSelection"] = EnableInteractiveSelection,
            ["actionLabel"] = ActionLabel,
            ["inputAction"] = $"TextInputAction.{DartName(InputAction)}",
            ["textCapitalization"] = $"TextCapitalization.{DartName(TextCapitalization)}",
            ["keyboardAppearance"] = $"Brightness.{DartName(KeyboardAppearance)}",
            ["enableIMEPersonalizedLearning"] = EnableIMEPersonalizedLearning,
            ["contentCommitMimeTypes"] = AllowedMimeTypes,
        };
        if (autofill != null)
        {
            json["autofill"] = autofill;
        }

        json["enableDeltaModel"] = EnableDeltaModel;
        json["hintLocales"] = HintLocales?.Select(locale => locale.Name).ToList();
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
               && other.KeyboardType == KeyboardType
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
               && other.EnableIMEPersonalizedLearning == EnableIMEPersonalizedLearning
               && other.AllowedMimeTypes.SequenceEqual(AllowedMimeTypes)
               && other.AutofillConfiguration.Equals(AutofillConfiguration)
               && other.EnableDeltaModel == EnableDeltaModel
               && (other.HintLocales is null
                   ? HintLocales is null
                   : HintLocales is not null && other.HintLocales.SequenceEqual(HintLocales))
               && other.EnableInlinePrediction == EnableInlinePrediction
               && other.Multiline == Multiline;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as TextInputConfiguration);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ViewId);
        hash.Add(KeyboardType);
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
        hash.Add(EnableIMEPersonalizedLearning);
        hash.Add(AutofillConfiguration);
        hash.Add(EnableDeltaModel);
        hash.Add(EnableInlinePrediction);
        hash.Add(Multiline);
        return hash.ToHashCode();
    }

    /// <summary>Whether two configurations carry the same values.</summary>
    public static bool operator ==(TextInputConfiguration? left, TextInputConfiguration? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two configurations carry different values.</summary>
    public static bool operator !=(TextInputConfiguration? left, TextInputConfiguration? right) =>
        !(left == right);

    private static Dictionary<string, object?> KeyboardTypeToJson(
        TextInputKeyboardType keyboardType,
        bool multiline)
    {
        return new Dictionary<string, object?>
        {
            ["name"] = $"TextInputType.{DartName(keyboardType)}",
            ["signed"] = null,
            ["decimal"] = null,
            ["isMultiline"] = multiline,
        };
    }

    private static string DartName<T>(T value)
        where T : struct, Enum
    {
        string name = value.ToString()!;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
