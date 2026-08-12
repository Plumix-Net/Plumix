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
    EdgeInsetsGeometry? ContentPadding = null,
    Color? IconColor = null,
    TextStyle? PrefixStyle = null,
    Color? PrefixIconColor = null,
    BoxConstraints? PrefixIconConstraints = null,
    TextStyle? SuffixStyle = null,
    Color? SuffixIconColor = null,
    BoxConstraints? SuffixIconConstraints = null,
    TextStyle? CounterStyle = null,
    bool? Filled = null,
    Color? FillColor = null,
    BorderSide? ActiveIndicatorBorder = null,
    BorderSide? OutlineBorder = null,
    Color? FocusColor = null,
    Color? HoverColor = null,
    InputBorder? ErrorBorder = null,
    InputBorder? FocusedBorder = null,
    InputBorder? FocusedErrorBorder = null,
    InputBorder? DisabledBorder = null,
    InputBorder? EnabledBorder = null,
    InputBorder? Border = null,
    bool? AlignLabelWithHint = null,
    BoxConstraints? Constraints = null,
    VisualDensity? VisualDensity = null,
    TimeSpan? HintFadeDuration = null)
{
    /// Fills only this theme's null fields from <paramref name="other"/>; the fields Flutter treats as
    /// non-nullable (floating-label behavior/alignment, density flags, filled, alignLabelWithHint)
    /// cannot be overridden once set.
    public InputDecorationThemeData Merge(InputDecorationThemeData? other)
    {
        if (other is null)
        {
            return this;
        }

        return this with
        {
            LabelStyle = LabelStyle ?? other.LabelStyle,
            FloatingLabelStyle = FloatingLabelStyle ?? other.FloatingLabelStyle,
            HelperStyle = HelperStyle ?? other.HelperStyle,
            HelperMaxLines = HelperMaxLines ?? other.HelperMaxLines,
            HintStyle = HintStyle ?? other.HintStyle,
            HintFadeDuration = HintFadeDuration ?? other.HintFadeDuration,
            HintMaxLines = HintMaxLines ?? other.HintMaxLines,
            ErrorStyle = ErrorStyle ?? other.ErrorStyle,
            ErrorMaxLines = ErrorMaxLines ?? other.ErrorMaxLines,
            ContentPadding = ContentPadding ?? other.ContentPadding,
            IconColor = IconColor ?? other.IconColor,
            PrefixStyle = PrefixStyle ?? other.PrefixStyle,
            PrefixIconColor = PrefixIconColor ?? other.PrefixIconColor,
            PrefixIconConstraints = PrefixIconConstraints ?? other.PrefixIconConstraints,
            SuffixStyle = SuffixStyle ?? other.SuffixStyle,
            SuffixIconColor = SuffixIconColor ?? other.SuffixIconColor,
            SuffixIconConstraints = SuffixIconConstraints ?? other.SuffixIconConstraints,
            CounterStyle = CounterStyle ?? other.CounterStyle,
            FillColor = FillColor ?? other.FillColor,
            ActiveIndicatorBorder = ActiveIndicatorBorder ?? other.ActiveIndicatorBorder,
            OutlineBorder = OutlineBorder ?? other.OutlineBorder,
            FocusColor = FocusColor ?? other.FocusColor,
            HoverColor = HoverColor ?? other.HoverColor,
            ErrorBorder = ErrorBorder ?? other.ErrorBorder,
            FocusedBorder = FocusedBorder ?? other.FocusedBorder,
            FocusedErrorBorder = FocusedErrorBorder ?? other.FocusedErrorBorder,
            DisabledBorder = DisabledBorder ?? other.DisabledBorder,
            EnabledBorder = EnabledBorder ?? other.EnabledBorder,
            Border = Border ?? other.Border,
            Constraints = Constraints ?? other.Constraints,
            VisualDensity = VisualDensity ?? other.VisualDensity,
        };
    }
}

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
