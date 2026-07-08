using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/input_decorator.dart

public sealed record InputDecorationThemeData(
    TextStyle? LabelStyle = null,
    TextStyle? FloatingLabelStyle = null,
    TextStyle? HelperStyle = null,
    int? HelperMaxLines = null,
    TextStyle? HintStyle = null,
    int? HintMaxLines = null,
    TextStyle? ErrorStyle = null,
    int? ErrorMaxLines = null,
    FloatingLabelBehavior? FloatingLabelBehavior = null,
    FloatingLabelAlignment? FloatingLabelAlignment = null,
    bool? IsDense = null,
    bool? IsCollapsed = null,
    Thickness? ContentPadding = null,
    Color? IconColor = null,
    TextStyle? PrefixStyle = null,
    Color? PrefixIconColor = null,
    TextStyle? SuffixStyle = null,
    Color? SuffixIconColor = null,
    TextStyle? CounterStyle = null,
    bool? Filled = null,
    Color? FillColor = null,
    Color? FocusColor = null,
    Color? HoverColor = null,
    InputBorder? ErrorBorder = null,
    InputBorder? FocusedBorder = null,
    InputBorder? FocusedErrorBorder = null,
    InputBorder? DisabledBorder = null,
    InputBorder? EnabledBorder = null,
    InputBorder? Border = null,
    BoxConstraints? Constraints = null);

public sealed class InputDecorationTheme : InheritedWidget
{
    public InputDecorationTheme(InputDecorationThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public InputDecorationThemeData Data { get; }
    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;
    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((InputDecorationTheme)oldWidget).Data);

    public static InputDecorationThemeData Of(BuildContext context) =>
        context.DependOnInherited<InputDecorationTheme>()?.Data
        ?? Theme.Of(context).InputDecorationTheme;
}
