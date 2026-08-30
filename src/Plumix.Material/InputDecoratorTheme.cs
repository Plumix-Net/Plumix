using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/input_decorator.dart
// (InputDecorationThemeData, InputDecorationTheme).

/// Defines the default appearance of [InputDecorator]s.
///
/// Flutter declares this as an ordinary class so `_InputDecoratorDefaultsM2`/`_InputDecoratorDefaultsM3`
/// can extend it and override individual getters with state-resolving values; Plumix keeps that shape,
/// which is why the members are `virtual` rather than record properties.
public class InputDecorationThemeData : IDiagnosticable
{
    public InputDecorationThemeData(
        WidgetStateTextStyle? labelStyle = null,
        WidgetStateTextStyle? floatingLabelStyle = null,
        WidgetStateTextStyle? helperStyle = null,
        int? helperMaxLines = null,
        WidgetStateTextStyle? hintStyle = null,
        TimeSpan? hintFadeDuration = null,
        int? hintMaxLines = null,
        WidgetStateTextStyle? errorStyle = null,
        int? errorMaxLines = null,
        FloatingLabelBehavior floatingLabelBehavior = FloatingLabelBehavior.Auto,
        FloatingLabelAlignment floatingLabelAlignment = FloatingLabelAlignment.Start,
        bool isDense = false,
        EdgeInsetsGeometry? contentPadding = null,
        bool isCollapsed = false,
        WidgetStateColor? iconColor = null,
        WidgetStateTextStyle? prefixStyle = null,
        WidgetStateColor? prefixIconColor = null,
        BoxConstraints? prefixIconConstraints = null,
        WidgetStateTextStyle? suffixStyle = null,
        WidgetStateColor? suffixIconColor = null,
        BoxConstraints? suffixIconConstraints = null,
        WidgetStateTextStyle? counterStyle = null,
        bool filled = false,
        WidgetStateColor? fillColor = null,
        WidgetStateBorderSide? activeIndicatorBorder = null,
        WidgetStateBorderSide? outlineBorder = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        InputBorder? errorBorder = null,
        InputBorder? focusedBorder = null,
        InputBorder? focusedErrorBorder = null,
        InputBorder? disabledBorder = null,
        InputBorder? enabledBorder = null,
        InputBorder? border = null,
        bool alignLabelWithHint = false,
        BoxConstraints? constraints = null,
        VisualDensity? visualDensity = null)
    {
        LabelStyle = labelStyle;
        FloatingLabelStyle = floatingLabelStyle;
        HelperStyle = helperStyle;
        HelperMaxLines = helperMaxLines;
        HintStyle = hintStyle;
        HintFadeDuration = hintFadeDuration;
        HintMaxLines = hintMaxLines;
        ErrorStyle = errorStyle;
        ErrorMaxLines = errorMaxLines;
        FloatingLabelBehavior = floatingLabelBehavior;
        FloatingLabelAlignment = floatingLabelAlignment;
        IsDense = isDense;
        ContentPadding = contentPadding;
        IsCollapsed = isCollapsed;
        IconColor = iconColor;
        PrefixStyle = prefixStyle;
        PrefixIconColor = prefixIconColor;
        PrefixIconConstraints = prefixIconConstraints;
        SuffixStyle = suffixStyle;
        SuffixIconColor = suffixIconColor;
        SuffixIconConstraints = suffixIconConstraints;
        CounterStyle = counterStyle;
        Filled = filled;
        FillColor = fillColor;
        ActiveIndicatorBorder = activeIndicatorBorder;
        OutlineBorder = outlineBorder;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        ErrorBorder = errorBorder;
        FocusedBorder = focusedBorder;
        FocusedErrorBorder = focusedErrorBorder;
        DisabledBorder = disabledBorder;
        EnabledBorder = enabledBorder;
        Border = border;
        AlignLabelWithHint = alignLabelWithHint;
        Constraints = constraints;
        VisualDensity = visualDensity;
    }

    public virtual WidgetStateTextStyle? LabelStyle { get; }
    public virtual WidgetStateTextStyle? FloatingLabelStyle { get; }
    public virtual WidgetStateTextStyle? HelperStyle { get; }
    public virtual int? HelperMaxLines { get; }
    public virtual WidgetStateTextStyle? HintStyle { get; }
    public virtual TimeSpan? HintFadeDuration { get; }
    public virtual int? HintMaxLines { get; }
    public virtual WidgetStateTextStyle? ErrorStyle { get; }
    public virtual int? ErrorMaxLines { get; }
    public virtual FloatingLabelBehavior FloatingLabelBehavior { get; }
    public virtual FloatingLabelAlignment FloatingLabelAlignment { get; }
    public virtual bool IsDense { get; }
    public virtual EdgeInsetsGeometry? ContentPadding { get; }
    public virtual bool IsCollapsed { get; }
    public virtual WidgetStateColor? IconColor { get; }
    public virtual WidgetStateTextStyle? PrefixStyle { get; }
    public virtual WidgetStateColor? PrefixIconColor { get; }
    public virtual BoxConstraints? PrefixIconConstraints { get; }
    public virtual WidgetStateTextStyle? SuffixStyle { get; }
    public virtual WidgetStateColor? SuffixIconColor { get; }
    public virtual BoxConstraints? SuffixIconConstraints { get; }
    public virtual WidgetStateTextStyle? CounterStyle { get; }
    public virtual bool Filled { get; }
    public virtual WidgetStateColor? FillColor { get; }
    public virtual WidgetStateBorderSide? ActiveIndicatorBorder { get; }
    public virtual WidgetStateBorderSide? OutlineBorder { get; }
    public virtual Color? FocusColor { get; }
    public virtual Color? HoverColor { get; }
    public virtual InputBorder? ErrorBorder { get; }
    public virtual InputBorder? FocusedBorder { get; }
    public virtual InputBorder? FocusedErrorBorder { get; }
    public virtual InputBorder? DisabledBorder { get; }
    public virtual InputBorder? EnabledBorder { get; }
    public virtual InputBorder? Border { get; }
    public virtual bool AlignLabelWithHint { get; }
    public virtual BoxConstraints? Constraints { get; }
    public virtual VisualDensity? VisualDensity { get; }

    public InputDecorationThemeData CopyWith(
        WidgetStateTextStyle? labelStyle = null,
        WidgetStateTextStyle? floatingLabelStyle = null,
        WidgetStateTextStyle? helperStyle = null,
        int? helperMaxLines = null,
        WidgetStateTextStyle? hintStyle = null,
        TimeSpan? hintFadeDuration = null,
        int? hintMaxLines = null,
        WidgetStateTextStyle? errorStyle = null,
        int? errorMaxLines = null,
        FloatingLabelBehavior? floatingLabelBehavior = null,
        FloatingLabelAlignment? floatingLabelAlignment = null,
        bool? isDense = null,
        EdgeInsetsGeometry? contentPadding = null,
        bool? isCollapsed = null,
        WidgetStateColor? iconColor = null,
        WidgetStateTextStyle? prefixStyle = null,
        WidgetStateColor? prefixIconColor = null,
        BoxConstraints? prefixIconConstraints = null,
        WidgetStateTextStyle? suffixStyle = null,
        WidgetStateColor? suffixIconColor = null,
        BoxConstraints? suffixIconConstraints = null,
        WidgetStateTextStyle? counterStyle = null,
        bool? filled = null,
        WidgetStateColor? fillColor = null,
        WidgetStateBorderSide? activeIndicatorBorder = null,
        WidgetStateBorderSide? outlineBorder = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        InputBorder? errorBorder = null,
        InputBorder? focusedBorder = null,
        InputBorder? focusedErrorBorder = null,
        InputBorder? disabledBorder = null,
        InputBorder? enabledBorder = null,
        InputBorder? border = null,
        bool? alignLabelWithHint = null,
        BoxConstraints? constraints = null,
        VisualDensity? visualDensity = null) => new(
        labelStyle: labelStyle ?? LabelStyle,
        floatingLabelStyle: floatingLabelStyle ?? FloatingLabelStyle,
        helperStyle: helperStyle ?? HelperStyle,
        helperMaxLines: helperMaxLines ?? HelperMaxLines,
        hintStyle: hintStyle ?? HintStyle,
        hintFadeDuration: hintFadeDuration ?? HintFadeDuration,
        hintMaxLines: hintMaxLines ?? HintMaxLines,
        errorStyle: errorStyle ?? ErrorStyle,
        errorMaxLines: errorMaxLines ?? ErrorMaxLines,
        floatingLabelBehavior: floatingLabelBehavior ?? FloatingLabelBehavior,
        floatingLabelAlignment: floatingLabelAlignment ?? FloatingLabelAlignment,
        isDense: isDense ?? IsDense,
        contentPadding: contentPadding ?? ContentPadding,
        isCollapsed: isCollapsed ?? IsCollapsed,
        iconColor: iconColor ?? IconColor,
        prefixStyle: prefixStyle ?? PrefixStyle,
        prefixIconColor: prefixIconColor ?? PrefixIconColor,
        prefixIconConstraints: prefixIconConstraints ?? PrefixIconConstraints,
        suffixStyle: suffixStyle ?? SuffixStyle,
        suffixIconColor: suffixIconColor ?? SuffixIconColor,
        suffixIconConstraints: suffixIconConstraints ?? SuffixIconConstraints,
        counterStyle: counterStyle ?? CounterStyle,
        filled: filled ?? Filled,
        fillColor: fillColor ?? FillColor,
        activeIndicatorBorder: activeIndicatorBorder ?? ActiveIndicatorBorder,
        outlineBorder: outlineBorder ?? OutlineBorder,
        focusColor: focusColor ?? FocusColor,
        hoverColor: hoverColor ?? HoverColor,
        errorBorder: errorBorder ?? ErrorBorder,
        focusedBorder: focusedBorder ?? FocusedBorder,
        focusedErrorBorder: focusedErrorBorder ?? FocusedErrorBorder,
        disabledBorder: disabledBorder ?? DisabledBorder,
        enabledBorder: enabledBorder ?? EnabledBorder,
        border: border ?? Border,
        alignLabelWithHint: alignLabelWithHint ?? AlignLabelWithHint,
        constraints: constraints ?? Constraints,
        visualDensity: visualDensity ?? VisualDensity);

    /// Fills only this theme's null fields from <paramref name="other"/>. Flutter deliberately omits
    /// the six non-nullable fields (floating-label behavior/alignment, `isDense`, `isCollapsed`,
    /// `filled`, `alignLabelWithHint`), so `other` can never override them.
    public InputDecorationThemeData Merge(InputDecorationThemeData? other)
    {
        if (other is null)
        {
            return this;
        }

        return CopyWith(
            labelStyle: LabelStyle ?? other.LabelStyle,
            floatingLabelStyle: FloatingLabelStyle ?? other.FloatingLabelStyle,
            helperStyle: HelperStyle ?? other.HelperStyle,
            helperMaxLines: HelperMaxLines ?? other.HelperMaxLines,
            hintStyle: HintStyle ?? other.HintStyle,
            hintFadeDuration: HintFadeDuration ?? other.HintFadeDuration,
            hintMaxLines: HintMaxLines ?? other.HintMaxLines,
            errorStyle: ErrorStyle ?? other.ErrorStyle,
            errorMaxLines: ErrorMaxLines ?? other.ErrorMaxLines,
            contentPadding: ContentPadding ?? other.ContentPadding,
            iconColor: IconColor ?? other.IconColor,
            prefixStyle: PrefixStyle ?? other.PrefixStyle,
            prefixIconColor: PrefixIconColor ?? other.PrefixIconColor,
            prefixIconConstraints: PrefixIconConstraints ?? other.PrefixIconConstraints,
            suffixStyle: SuffixStyle ?? other.SuffixStyle,
            suffixIconColor: SuffixIconColor ?? other.SuffixIconColor,
            suffixIconConstraints: SuffixIconConstraints ?? other.SuffixIconConstraints,
            counterStyle: CounterStyle ?? other.CounterStyle,
            fillColor: FillColor ?? other.FillColor,
            activeIndicatorBorder: ActiveIndicatorBorder ?? other.ActiveIndicatorBorder,
            outlineBorder: OutlineBorder ?? other.OutlineBorder,
            focusColor: FocusColor ?? other.FocusColor,
            hoverColor: HoverColor ?? other.HoverColor,
            errorBorder: ErrorBorder ?? other.ErrorBorder,
            focusedBorder: FocusedBorder ?? other.FocusedBorder,
            focusedErrorBorder: FocusedErrorBorder ?? other.FocusedErrorBorder,
            disabledBorder: DisabledBorder ?? other.DisabledBorder,
            enabledBorder: EnabledBorder ?? other.EnabledBorder,
            border: Border ?? other.Border,
            constraints: Constraints ?? other.Constraints,
            visualDensity: VisualDensity ?? other.VisualDensity);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        // Dart's `operator ==` starts with a runtimeType check, so a defaults subclass is never equal
        // to a plain InputDecorationThemeData carrying the same values.
        if (obj is not InputDecorationThemeData other || other.GetType() != GetType())
        {
            return false;
        }

        return Equals(LabelStyle, other.LabelStyle)
               && Equals(FloatingLabelStyle, other.FloatingLabelStyle)
               && Equals(HelperStyle, other.HelperStyle)
               && HelperMaxLines == other.HelperMaxLines
               && Equals(HintStyle, other.HintStyle)
               && HintFadeDuration == other.HintFadeDuration
               && HintMaxLines == other.HintMaxLines
               && Equals(ErrorStyle, other.ErrorStyle)
               && ErrorMaxLines == other.ErrorMaxLines
               && FloatingLabelBehavior == other.FloatingLabelBehavior
               && FloatingLabelAlignment == other.FloatingLabelAlignment
               && IsDense == other.IsDense
               && Equals(ContentPadding, other.ContentPadding)
               && IsCollapsed == other.IsCollapsed
               && Equals(IconColor, other.IconColor)
               && Equals(PrefixStyle, other.PrefixStyle)
               && Equals(PrefixIconColor, other.PrefixIconColor)
               && Nullable.Equals(PrefixIconConstraints, other.PrefixIconConstraints)
               && Equals(SuffixStyle, other.SuffixStyle)
               && Equals(SuffixIconColor, other.SuffixIconColor)
               && Nullable.Equals(SuffixIconConstraints, other.SuffixIconConstraints)
               && Equals(CounterStyle, other.CounterStyle)
               && Filled == other.Filled
               && Equals(FillColor, other.FillColor)
               && Equals(ActiveIndicatorBorder, other.ActiveIndicatorBorder)
               && Equals(OutlineBorder, other.OutlineBorder)
               && Nullable.Equals(FocusColor, other.FocusColor)
               && Nullable.Equals(HoverColor, other.HoverColor)
               && Equals(ErrorBorder, other.ErrorBorder)
               && Equals(FocusedBorder, other.FocusedBorder)
               && Equals(FocusedErrorBorder, other.FocusedErrorBorder)
               && Equals(DisabledBorder, other.DisabledBorder)
               && Equals(EnabledBorder, other.EnabledBorder)
               && Equals(Border, other.Border)
               && AlignLabelWithHint == other.AlignLabelWithHint
               && Nullable.Equals(Constraints, other.Constraints)
               && Nullable.Equals(VisualDensity, other.VisualDensity);
    }

    /// <summary>Dart's `InputDecorationThemeData.debugFillProperties`.</summary>
    /// <remarks>
    /// The state-resolving members are typed `WidgetState*` here rather than the plain `TextStyle`,
    /// `Color` and `BorderSide` Dart declares (see the `WidgetState` row in
    /// `docs/ai/DIVERGENCES.md`), so they are dumped through `DiagnosticsProperty` of that type;
    /// name, order, level and default-value elision match Dart.
    /// </remarks>
    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var defaultTheme = new InputDecorationThemeData();
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "labelStyle", LabelStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "floatingLabelStyle", FloatingLabelStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "helperStyle", HelperStyle, defaultValue: nullDefault));
        properties.Add(new IntProperty("helperMaxLines", HelperMaxLines, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "hintStyle", HintStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TimeSpan?>(
            "hintFadeDuration", HintFadeDuration, defaultValue: nullDefault));
        properties.Add(new IntProperty("hintMaxLines", HintMaxLines, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "errorStyle", ErrorStyle, defaultValue: nullDefault));
        properties.Add(new IntProperty("errorMaxLines", ErrorMaxLines, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<FloatingLabelBehavior>(
            "floatingLabelBehavior", FloatingLabelBehavior, defaultValue: defaultTheme.FloatingLabelBehavior));
        properties.Add(new DiagnosticsProperty<FloatingLabelAlignment>(
            "floatingLabelAlignment", FloatingLabelAlignment, defaultValue: defaultTheme.FloatingLabelAlignment));
        properties.Add(new DiagnosticsProperty<bool>("isDense", IsDense, defaultValue: defaultTheme.IsDense));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>(
            "contentPadding", ContentPadding, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool>(
            "isCollapsed", IsCollapsed, defaultValue: defaultTheme.IsCollapsed));
        properties.Add(new DiagnosticsProperty<WidgetStateColor?>(
            "iconColor", IconColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateColor?>(
            "prefixIconColor", PrefixIconColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>(
            "prefixIconConstraints", PrefixIconConstraints, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "prefixStyle", PrefixStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateColor?>(
            "suffixIconColor", SuffixIconColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>(
            "suffixIconConstraints", SuffixIconConstraints, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "suffixStyle", SuffixStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateTextStyle?>(
            "counterStyle", CounterStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool>("filled", Filled, defaultValue: defaultTheme.Filled));
        properties.Add(new DiagnosticsProperty<WidgetStateColor?>(
            "fillColor", FillColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateBorderSide?>(
            "activeIndicatorBorder", ActiveIndicatorBorder, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateBorderSide?>(
            "outlineBorder", OutlineBorder, defaultValue: nullDefault));
        properties.Add(new ColorProperty("focusColor", FocusColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty("hoverColor", HoverColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<InputBorder?>(
            "errorBorder", ErrorBorder, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<InputBorder?>(
            "focusedBorder", FocusedBorder, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<InputBorder?>(
            "focusedErrorBorder", FocusedErrorBorder, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<InputBorder?>(
            "disabledBorder", DisabledBorder, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<InputBorder?>(
            "enabledBorder", EnabledBorder, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<InputBorder?>("border", Border, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool>(
            "alignLabelWithHint", AlignLabelWithHint, defaultValue: defaultTheme.AlignLabelWithHint));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>(
            "constraints", Constraints, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<VisualDensity?>(
            "visualDensity", VisualDensity, defaultValue: nullDefault));
    }

    public override int GetHashCode()
    {
        var first = new HashCode();
        first.Add(LabelStyle);
        first.Add(FloatingLabelStyle);
        first.Add(HelperStyle);
        first.Add(HelperMaxLines);
        first.Add(HintStyle);
        first.Add(HintMaxLines);
        first.Add(ErrorStyle);
        first.Add(ErrorMaxLines);
        first.Add(FloatingLabelBehavior);
        first.Add(FloatingLabelAlignment);
        first.Add(IsDense);
        first.Add(ContentPadding);
        first.Add(IsCollapsed);
        first.Add(IconColor);
        first.Add(PrefixStyle);
        first.Add(PrefixIconColor);
        first.Add(PrefixIconConstraints);
        first.Add(SuffixStyle);
        first.Add(SuffixIconColor);

        var second = new HashCode();
        second.Add(SuffixIconConstraints);
        second.Add(CounterStyle);
        second.Add(Filled);
        second.Add(FillColor);
        second.Add(ActiveIndicatorBorder);
        second.Add(OutlineBorder);
        second.Add(FocusColor);
        second.Add(HoverColor);
        second.Add(ErrorBorder);
        second.Add(FocusedBorder);
        second.Add(FocusedErrorBorder);
        second.Add(DisabledBorder);
        second.Add(EnabledBorder);
        second.Add(Border);
        second.Add(AlignLabelWithHint);
        second.Add(Constraints);
        second.Add(HintFadeDuration);
        second.Add(VisualDensity);

        first.Add(second.ToHashCode());
        return first.ToHashCode();
    }

    public static bool operator ==(InputDecorationThemeData? left, InputDecorationThemeData? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(InputDecorationThemeData? left, InputDecorationThemeData? right) =>
        !(left == right);
}

/// An [InheritedTheme] that overrides the [InputDecorator] defaults below it.
///
/// Flutter keeps the obsolete per-field constructor alongside the `data`-based one; both are ported,
/// including the field-based [CopyWith]/[Merge] pair and the forwarding getters.
public sealed class InputDecorationTheme : InheritedTheme
{
    private readonly InputDecorationThemeData? _data;
    private readonly WidgetStateTextStyle? _labelStyle;
    private readonly WidgetStateTextStyle? _floatingLabelStyle;
    private readonly WidgetStateTextStyle? _helperStyle;
    private readonly int? _helperMaxLines;
    private readonly WidgetStateTextStyle? _hintStyle;
    private readonly TimeSpan? _hintFadeDuration;
    private readonly int? _hintMaxLines;
    private readonly WidgetStateTextStyle? _errorStyle;
    private readonly int? _errorMaxLines;
    private readonly FloatingLabelBehavior _floatingLabelBehavior;
    private readonly FloatingLabelAlignment _floatingLabelAlignment;
    private readonly bool _isDense;
    private readonly EdgeInsetsGeometry? _contentPadding;
    private readonly bool _isCollapsed;
    private readonly WidgetStateColor? _iconColor;
    private readonly WidgetStateTextStyle? _prefixStyle;
    private readonly WidgetStateColor? _prefixIconColor;
    private readonly BoxConstraints? _prefixIconConstraints;
    private readonly WidgetStateTextStyle? _suffixStyle;
    private readonly WidgetStateColor? _suffixIconColor;
    private readonly BoxConstraints? _suffixIconConstraints;
    private readonly WidgetStateTextStyle? _counterStyle;
    private readonly bool _filled;
    private readonly WidgetStateColor? _fillColor;
    private readonly WidgetStateBorderSide? _activeIndicatorBorder;
    private readonly WidgetStateBorderSide? _outlineBorder;
    private readonly Color? _focusColor;
    private readonly Color? _hoverColor;
    private readonly InputBorder? _errorBorder;
    private readonly InputBorder? _focusedBorder;
    private readonly InputBorder? _focusedErrorBorder;
    private readonly InputBorder? _disabledBorder;
    private readonly InputBorder? _enabledBorder;
    private readonly InputBorder? _border;
    private readonly bool _alignLabelWithHint;
    private readonly BoxConstraints? _constraints;
    private readonly VisualDensity? _visualDensity;

    public InputDecorationTheme(
        InputDecorationThemeData? data = null,
        Widget? child = null,
        WidgetStateTextStyle? labelStyle = null,
        WidgetStateTextStyle? floatingLabelStyle = null,
        WidgetStateTextStyle? helperStyle = null,
        int? helperMaxLines = null,
        WidgetStateTextStyle? hintStyle = null,
        TimeSpan? hintFadeDuration = null,
        int? hintMaxLines = null,
        WidgetStateTextStyle? errorStyle = null,
        int? errorMaxLines = null,
        FloatingLabelBehavior? floatingLabelBehavior = null,
        FloatingLabelAlignment? floatingLabelAlignment = null,
        bool? isDense = null,
        EdgeInsetsGeometry? contentPadding = null,
        bool? isCollapsed = null,
        WidgetStateColor? iconColor = null,
        WidgetStateTextStyle? prefixStyle = null,
        WidgetStateColor? prefixIconColor = null,
        BoxConstraints? prefixIconConstraints = null,
        WidgetStateTextStyle? suffixStyle = null,
        WidgetStateColor? suffixIconColor = null,
        BoxConstraints? suffixIconConstraints = null,
        WidgetStateTextStyle? counterStyle = null,
        bool? filled = null,
        WidgetStateColor? fillColor = null,
        WidgetStateBorderSide? activeIndicatorBorder = null,
        WidgetStateBorderSide? outlineBorder = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        InputBorder? errorBorder = null,
        InputBorder? focusedBorder = null,
        InputBorder? focusedErrorBorder = null,
        InputBorder? disabledBorder = null,
        InputBorder? enabledBorder = null,
        InputBorder? border = null,
        bool? alignLabelWithHint = null,
        BoxConstraints? constraints = null,
        VisualDensity? visualDensity = null,
        Key? key = null) : base(key)
    {
        bool hasFieldArgument = labelStyle is not null || floatingLabelStyle is not null
            || helperStyle is not null || helperMaxLines is not null || hintStyle is not null
            || hintFadeDuration is not null || hintMaxLines is not null || errorStyle is not null
            || errorMaxLines is not null || floatingLabelBehavior is not null
            || floatingLabelAlignment is not null || isDense is not null || contentPadding is not null
            || isCollapsed is not null || iconColor is not null || prefixStyle is not null
            || prefixIconColor is not null || prefixIconConstraints is not null || suffixStyle is not null
            || suffixIconColor is not null || suffixIconConstraints is not null || counterStyle is not null
            || filled is not null || fillColor is not null || activeIndicatorBorder is not null
            || outlineBorder is not null || focusColor is not null || hoverColor is not null
            || errorBorder is not null || focusedBorder is not null || focusedErrorBorder is not null
            || disabledBorder is not null || enabledBorder is not null || border is not null
            || alignLabelWithHint is not null || constraints is not null || visualDensity is not null;
        if (data is not null && hasFieldArgument)
        {
            throw new ArgumentException(
                "InputDecorationTheme accepts either a data argument or the obsolete per-field "
                + "arguments, never both.",
                nameof(data));
        }

        _data = data;
        _labelStyle = labelStyle;
        _floatingLabelStyle = floatingLabelStyle;
        _helperStyle = helperStyle;
        _helperMaxLines = helperMaxLines;
        _hintStyle = hintStyle;
        _hintFadeDuration = hintFadeDuration;
        _hintMaxLines = hintMaxLines;
        _errorStyle = errorStyle;
        _errorMaxLines = errorMaxLines;
        _floatingLabelBehavior = floatingLabelBehavior ?? FloatingLabelBehavior.Auto;
        _floatingLabelAlignment = floatingLabelAlignment ?? FloatingLabelAlignment.Start;
        _isDense = isDense ?? false;
        _contentPadding = contentPadding;
        _isCollapsed = isCollapsed ?? false;
        _iconColor = iconColor;
        _prefixStyle = prefixStyle;
        _prefixIconColor = prefixIconColor;
        _prefixIconConstraints = prefixIconConstraints;
        _suffixStyle = suffixStyle;
        _suffixIconColor = suffixIconColor;
        _suffixIconConstraints = suffixIconConstraints;
        _counterStyle = counterStyle;
        _filled = filled ?? false;
        _fillColor = fillColor;
        _activeIndicatorBorder = activeIndicatorBorder;
        _outlineBorder = outlineBorder;
        _focusColor = focusColor;
        _hoverColor = hoverColor;
        _errorBorder = errorBorder;
        _focusedBorder = focusedBorder;
        _focusedErrorBorder = focusedErrorBorder;
        _disabledBorder = disabledBorder;
        _enabledBorder = enabledBorder;
        _border = border;
        _alignLabelWithHint = alignLabelWithHint ?? false;
        _constraints = constraints;
        _visualDensity = visualDensity;
        Child = child ?? new SizedBox();
    }

    public Widget Child { get; }

    public InputDecorationThemeData Data => _data ?? new InputDecorationThemeData(
        labelStyle: _labelStyle,
        floatingLabelStyle: _floatingLabelStyle,
        helperStyle: _helperStyle,
        helperMaxLines: _helperMaxLines,
        hintStyle: _hintStyle,
        hintFadeDuration: _hintFadeDuration,
        hintMaxLines: _hintMaxLines,
        errorStyle: _errorStyle,
        errorMaxLines: _errorMaxLines,
        floatingLabelBehavior: _floatingLabelBehavior,
        floatingLabelAlignment: _floatingLabelAlignment,
        isDense: _isDense,
        contentPadding: _contentPadding,
        isCollapsed: _isCollapsed,
        iconColor: _iconColor,
        prefixStyle: _prefixStyle,
        prefixIconColor: _prefixIconColor,
        prefixIconConstraints: _prefixIconConstraints,
        suffixStyle: _suffixStyle,
        suffixIconColor: _suffixIconColor,
        suffixIconConstraints: _suffixIconConstraints,
        counterStyle: _counterStyle,
        filled: _filled,
        fillColor: _fillColor,
        activeIndicatorBorder: _activeIndicatorBorder,
        outlineBorder: _outlineBorder,
        focusColor: _focusColor,
        hoverColor: _hoverColor,
        errorBorder: _errorBorder,
        focusedBorder: _focusedBorder,
        focusedErrorBorder: _focusedErrorBorder,
        disabledBorder: _disabledBorder,
        enabledBorder: _enabledBorder,
        border: _border,
        alignLabelWithHint: _alignLabelWithHint,
        constraints: _constraints,
        visualDensity: _visualDensity);

    public WidgetStateTextStyle? LabelStyle => _data is not null ? _data.LabelStyle : _labelStyle;
    public WidgetStateTextStyle? FloatingLabelStyle =>
        _data is not null ? _data.FloatingLabelStyle : _floatingLabelStyle;
    public WidgetStateTextStyle? HelperStyle => _data is not null ? _data.HelperStyle : _helperStyle;
    public int? HelperMaxLines => _data is not null ? _data.HelperMaxLines : _helperMaxLines;
    public WidgetStateTextStyle? HintStyle => _data is not null ? _data.HintStyle : _hintStyle;
    public TimeSpan? HintFadeDuration => _data is not null ? _data.HintFadeDuration : _hintFadeDuration;
    public int? HintMaxLines => _data is not null ? _data.HintMaxLines : _hintMaxLines;
    public WidgetStateTextStyle? ErrorStyle => _data is not null ? _data.ErrorStyle : _errorStyle;
    public int? ErrorMaxLines => _data is not null ? _data.ErrorMaxLines : _errorMaxLines;
    public FloatingLabelBehavior FloatingLabelBehavior =>
        _data is not null ? _data.FloatingLabelBehavior : _floatingLabelBehavior;
    public FloatingLabelAlignment FloatingLabelAlignment =>
        _data is not null ? _data.FloatingLabelAlignment : _floatingLabelAlignment;
    public bool IsDense => _data is not null ? _data.IsDense : _isDense;
    public EdgeInsetsGeometry? ContentPadding => _data is not null ? _data.ContentPadding : _contentPadding;
    public bool IsCollapsed => _data is not null ? _data.IsCollapsed : _isCollapsed;
    public WidgetStateColor? IconColor => _data is not null ? _data.IconColor : _iconColor;
    public WidgetStateTextStyle? PrefixStyle => _data is not null ? _data.PrefixStyle : _prefixStyle;
    public WidgetStateColor? PrefixIconColor => _data is not null ? _data.PrefixIconColor : _prefixIconColor;
    public BoxConstraints? PrefixIconConstraints =>
        _data is not null ? _data.PrefixIconConstraints : _prefixIconConstraints;
    public WidgetStateTextStyle? SuffixStyle => _data is not null ? _data.SuffixStyle : _suffixStyle;
    public WidgetStateColor? SuffixIconColor => _data is not null ? _data.SuffixIconColor : _suffixIconColor;
    public BoxConstraints? SuffixIconConstraints =>
        _data is not null ? _data.SuffixIconConstraints : _suffixIconConstraints;
    public WidgetStateTextStyle? CounterStyle => _data is not null ? _data.CounterStyle : _counterStyle;
    public bool Filled => _data is not null ? _data.Filled : _filled;
    public WidgetStateColor? FillColor => _data is not null ? _data.FillColor : _fillColor;
    public WidgetStateBorderSide? ActiveIndicatorBorder =>
        _data is not null ? _data.ActiveIndicatorBorder : _activeIndicatorBorder;
    public WidgetStateBorderSide? OutlineBorder => _data is not null ? _data.OutlineBorder : _outlineBorder;
    public Color? FocusColor => _data is not null ? _data.FocusColor : _focusColor;
    public Color? HoverColor => _data is not null ? _data.HoverColor : _hoverColor;
    public InputBorder? ErrorBorder => _data is not null ? _data.ErrorBorder : _errorBorder;
    public InputBorder? FocusedBorder => _data is not null ? _data.FocusedBorder : _focusedBorder;
    public InputBorder? FocusedErrorBorder =>
        _data is not null ? _data.FocusedErrorBorder : _focusedErrorBorder;
    public InputBorder? DisabledBorder => _data is not null ? _data.DisabledBorder : _disabledBorder;
    public InputBorder? EnabledBorder => _data is not null ? _data.EnabledBorder : _enabledBorder;
    public InputBorder? Border => _data is not null ? _data.Border : _border;
    public bool AlignLabelWithHint => _data is not null ? _data.AlignLabelWithHint : _alignLabelWithHint;
    public BoxConstraints? Constraints => _data is not null ? _data.Constraints : _constraints;
    public VisualDensity? VisualDensity => _data is not null ? _data.VisualDensity : _visualDensity;

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new InputDecorationTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        Data != ((InputDecorationTheme)oldWidget).Data;

    public static InputDecorationThemeData Of(BuildContext context) =>
        context.DependOnInherited<InputDecorationTheme>()?.Data
        ?? Theme.Of(context).InputDecorationTheme;

    /// Obsolete field-based copy. Like Flutter's, the result is field-backed — it keeps neither the
    /// `data` argument nor the `child` of the theme it was copied from.
    public InputDecorationTheme CopyWith(
        WidgetStateTextStyle? labelStyle = null,
        WidgetStateTextStyle? floatingLabelStyle = null,
        WidgetStateTextStyle? helperStyle = null,
        int? helperMaxLines = null,
        WidgetStateTextStyle? hintStyle = null,
        TimeSpan? hintFadeDuration = null,
        int? hintMaxLines = null,
        WidgetStateTextStyle? errorStyle = null,
        int? errorMaxLines = null,
        FloatingLabelBehavior? floatingLabelBehavior = null,
        FloatingLabelAlignment? floatingLabelAlignment = null,
        bool? isDense = null,
        EdgeInsetsGeometry? contentPadding = null,
        bool? isCollapsed = null,
        WidgetStateColor? iconColor = null,
        WidgetStateTextStyle? prefixStyle = null,
        WidgetStateColor? prefixIconColor = null,
        BoxConstraints? prefixIconConstraints = null,
        WidgetStateTextStyle? suffixStyle = null,
        WidgetStateColor? suffixIconColor = null,
        BoxConstraints? suffixIconConstraints = null,
        WidgetStateTextStyle? counterStyle = null,
        bool? filled = null,
        WidgetStateColor? fillColor = null,
        WidgetStateBorderSide? activeIndicatorBorder = null,
        WidgetStateBorderSide? outlineBorder = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        InputBorder? errorBorder = null,
        InputBorder? focusedBorder = null,
        InputBorder? focusedErrorBorder = null,
        InputBorder? disabledBorder = null,
        InputBorder? enabledBorder = null,
        InputBorder? border = null,
        bool? alignLabelWithHint = null,
        BoxConstraints? constraints = null,
        VisualDensity? visualDensity = null) => new(
        labelStyle: labelStyle ?? LabelStyle,
        floatingLabelStyle: floatingLabelStyle ?? FloatingLabelStyle,
        helperStyle: helperStyle ?? HelperStyle,
        helperMaxLines: helperMaxLines ?? HelperMaxLines,
        hintStyle: hintStyle ?? HintStyle,
        hintFadeDuration: hintFadeDuration ?? HintFadeDuration,
        hintMaxLines: hintMaxLines ?? HintMaxLines,
        errorStyle: errorStyle ?? ErrorStyle,
        errorMaxLines: errorMaxLines ?? ErrorMaxLines,
        floatingLabelBehavior: floatingLabelBehavior ?? FloatingLabelBehavior,
        floatingLabelAlignment: floatingLabelAlignment ?? FloatingLabelAlignment,
        isDense: isDense ?? IsDense,
        contentPadding: contentPadding ?? ContentPadding,
        isCollapsed: isCollapsed ?? IsCollapsed,
        iconColor: iconColor ?? IconColor,
        prefixStyle: prefixStyle ?? PrefixStyle,
        prefixIconColor: prefixIconColor ?? PrefixIconColor,
        prefixIconConstraints: prefixIconConstraints ?? PrefixIconConstraints,
        suffixStyle: suffixStyle ?? SuffixStyle,
        suffixIconColor: suffixIconColor ?? SuffixIconColor,
        suffixIconConstraints: suffixIconConstraints ?? SuffixIconConstraints,
        counterStyle: counterStyle ?? CounterStyle,
        filled: filled ?? Filled,
        fillColor: fillColor ?? FillColor,
        activeIndicatorBorder: activeIndicatorBorder ?? ActiveIndicatorBorder,
        outlineBorder: outlineBorder ?? OutlineBorder,
        focusColor: focusColor ?? FocusColor,
        hoverColor: hoverColor ?? HoverColor,
        errorBorder: errorBorder ?? ErrorBorder,
        focusedBorder: focusedBorder ?? FocusedBorder,
        focusedErrorBorder: focusedErrorBorder ?? FocusedErrorBorder,
        disabledBorder: disabledBorder ?? DisabledBorder,
        enabledBorder: enabledBorder ?? EnabledBorder,
        border: border ?? Border,
        alignLabelWithHint: alignLabelWithHint ?? AlignLabelWithHint,
        constraints: constraints ?? Constraints,
        visualDensity: visualDensity ?? VisualDensity);

    /// Obsolete field-based merge; the six non-nullable fields are never taken from `other`.
    public InputDecorationTheme Merge(InputDecorationTheme? other)
    {
        if (other is null)
        {
            return this;
        }

        return CopyWith(
            labelStyle: LabelStyle ?? other.LabelStyle,
            floatingLabelStyle: FloatingLabelStyle ?? other.FloatingLabelStyle,
            helperStyle: HelperStyle ?? other.HelperStyle,
            helperMaxLines: HelperMaxLines ?? other.HelperMaxLines,
            hintStyle: HintStyle ?? other.HintStyle,
            hintFadeDuration: HintFadeDuration ?? other.HintFadeDuration,
            hintMaxLines: HintMaxLines ?? other.HintMaxLines,
            errorStyle: ErrorStyle ?? other.ErrorStyle,
            errorMaxLines: ErrorMaxLines ?? other.ErrorMaxLines,
            contentPadding: ContentPadding ?? other.ContentPadding,
            iconColor: IconColor ?? other.IconColor,
            prefixStyle: PrefixStyle ?? other.PrefixStyle,
            prefixIconColor: PrefixIconColor ?? other.PrefixIconColor,
            prefixIconConstraints: PrefixIconConstraints ?? other.PrefixIconConstraints,
            suffixStyle: SuffixStyle ?? other.SuffixStyle,
            suffixIconColor: SuffixIconColor ?? other.SuffixIconColor,
            suffixIconConstraints: SuffixIconConstraints ?? other.SuffixIconConstraints,
            counterStyle: CounterStyle ?? other.CounterStyle,
            fillColor: FillColor ?? other.FillColor,
            activeIndicatorBorder: ActiveIndicatorBorder ?? other.ActiveIndicatorBorder,
            outlineBorder: OutlineBorder ?? other.OutlineBorder,
            focusColor: FocusColor ?? other.FocusColor,
            hoverColor: HoverColor ?? other.HoverColor,
            errorBorder: ErrorBorder ?? other.ErrorBorder,
            focusedBorder: FocusedBorder ?? other.FocusedBorder,
            focusedErrorBorder: FocusedErrorBorder ?? other.FocusedErrorBorder,
            disabledBorder: DisabledBorder ?? other.DisabledBorder,
            enabledBorder: EnabledBorder ?? other.EnabledBorder,
            border: Border ?? other.Border,
            constraints: Constraints ?? other.Constraints,
            visualDensity: VisualDensity ?? other.VisualDensity);
    }
}
