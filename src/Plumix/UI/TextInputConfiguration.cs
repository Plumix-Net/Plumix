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

public readonly record struct TextInputConfiguration(
    TextInputKeyboardType KeyboardType,
    TextInputActionType InputAction,
    bool Autocorrect,
    bool EnableSuggestions,
    bool ObscureText,
    bool Multiline);
