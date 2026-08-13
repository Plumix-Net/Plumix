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

public readonly record struct TextInputConfiguration(
    TextInputKeyboardType KeyboardType,
    TextInputActionType InputAction,
    bool Autocorrect,
    bool EnableSuggestions,
    bool ObscureText,
    bool Multiline,
    TextCapitalization TextCapitalization = TextCapitalization.None,
    SmartDashesType SmartDashesType = SmartDashesType.Enabled,
    SmartQuotesType SmartQuotesType = SmartQuotesType.Enabled);
