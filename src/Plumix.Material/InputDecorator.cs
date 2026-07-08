using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/input_border.dart;
// flutter/packages/flutter/lib/src/material/input_decorator.dart

public enum FloatingLabelBehavior { Never, Auto, Always }
public enum FloatingLabelAlignment { Start, Center }

public abstract class InputBorder
{
    protected InputBorder(BorderSide? borderSide = null) => BorderSide = borderSide ?? default;
    public BorderSide BorderSide { get; }
    public abstract bool IsOutline { get; }
    public abstract BorderRadius BorderRadius { get; }
    public abstract InputBorder CopyWith(BorderSide borderSide);
    public static InputBorder None { get; } = new NoInputBorder();
}

public sealed class UnderlineInputBorder : InputBorder
{
    public UnderlineInputBorder(BorderSide? borderSide = null, BorderRadius? borderRadius = null)
        : base(borderSide ?? new BorderSide(Colors.Black, 1)) =>
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Circular(4);
    public override bool IsOutline => false;
    public override BorderRadius BorderRadius { get; }
    public override InputBorder CopyWith(BorderSide borderSide) => new UnderlineInputBorder(borderSide, BorderRadius);
}

public sealed class OutlineInputBorder : InputBorder
{
    public OutlineInputBorder(BorderSide? borderSide = null, BorderRadius? borderRadius = null, double gapPadding = 4)
        : base(borderSide ?? new BorderSide(Colors.Black, 1))
    {
        if (!double.IsFinite(gapPadding) || gapPadding < 0) throw new ArgumentOutOfRangeException(nameof(gapPadding));
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Circular(4);
        GapPadding = gapPadding;
    }
    public override bool IsOutline => true;
    public override BorderRadius BorderRadius { get; }
    public double GapPadding { get; }
    public override InputBorder CopyWith(BorderSide borderSide) => new OutlineInputBorder(borderSide, BorderRadius, GapPadding);
}

internal sealed class NoInputBorder : InputBorder
{
    public override bool IsOutline => false;
    public override BorderRadius BorderRadius => Plumix.Rendering.BorderRadius.Zero;
    public override InputBorder CopyWith(BorderSide borderSide) => this;
}

public sealed class InputDecoration
{
    public InputDecoration(
        Widget? icon = null,
        Color? iconColor = null,
        Widget? label = null,
        string? labelText = null,
        TextStyle? labelStyle = null,
        TextStyle? floatingLabelStyle = null,
        Widget? helper = null,
        string? helperText = null,
        TextStyle? helperStyle = null,
        int? helperMaxLines = null,
        string? hintText = null,
        Widget? hint = null,
        TextStyle? hintStyle = null,
        int? hintMaxLines = null,
        Widget? error = null,
        string? errorText = null,
        TextStyle? errorStyle = null,
        int? errorMaxLines = null,
        FloatingLabelBehavior? floatingLabelBehavior = null,
        FloatingLabelAlignment? floatingLabelAlignment = null,
        bool? isCollapsed = null,
        bool? isDense = null,
        Thickness? contentPadding = null,
        Widget? prefixIcon = null,
        BoxConstraints? prefixIconConstraints = null,
        Widget? prefix = null,
        string? prefixText = null,
        TextStyle? prefixStyle = null,
        Color? prefixIconColor = null,
        Widget? suffixIcon = null,
        Widget? suffix = null,
        string? suffixText = null,
        TextStyle? suffixStyle = null,
        Color? suffixIconColor = null,
        BoxConstraints? suffixIconConstraints = null,
        Widget? counter = null,
        string? counterText = null,
        TextStyle? counterStyle = null,
        bool? filled = null,
        Color? fillColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        InputBorder? errorBorder = null,
        InputBorder? focusedBorder = null,
        InputBorder? focusedErrorBorder = null,
        InputBorder? disabledBorder = null,
        InputBorder? enabledBorder = null,
        InputBorder? border = null,
        bool enabled = true,
        string? semanticCounterText = null,
        bool? alignLabelWithHint = null,
        BoxConstraints? constraints = null)
    {
        if (label is not null && labelText is not null) throw new ArgumentException("label and labelText cannot both be specified.");
        if (hint is not null && hintText is not null) throw new ArgumentException("hint and hintText cannot both be specified.");
        if (helper is not null && helperText is not null) throw new ArgumentException("helper and helperText cannot both be specified.");
        if (prefix is not null && prefixText is not null) throw new ArgumentException("prefix and prefixText cannot both be specified.");
        if (suffix is not null && suffixText is not null) throw new ArgumentException("suffix and suffixText cannot both be specified.");
        if (error is not null && errorText is not null) throw new ArgumentException("error and errorText cannot both be specified.");
        ValidateLines(helperMaxLines, nameof(helperMaxLines)); ValidateLines(hintMaxLines, nameof(hintMaxLines)); ValidateLines(errorMaxLines, nameof(errorMaxLines));
        Icon = icon; IconColor = iconColor; Label = label; LabelText = labelText; LabelStyle = labelStyle;
        FloatingLabelStyle = floatingLabelStyle; Helper = helper; HelperText = helperText; HelperStyle = helperStyle;
        HelperMaxLines = helperMaxLines; HintText = hintText; Hint = hint; HintStyle = hintStyle; HintMaxLines = hintMaxLines;
        Error = error; ErrorText = errorText; ErrorStyle = errorStyle; ErrorMaxLines = errorMaxLines;
        FloatingLabelBehavior = floatingLabelBehavior; FloatingLabelAlignment = floatingLabelAlignment;
        IsCollapsed = isCollapsed; IsDense = isDense; ContentPadding = contentPadding; PrefixIcon = prefixIcon;
        PrefixIconConstraints = prefixIconConstraints; Prefix = prefix; PrefixText = prefixText; PrefixStyle = prefixStyle;
        PrefixIconColor = prefixIconColor; SuffixIcon = suffixIcon; Suffix = suffix; SuffixText = suffixText;
        SuffixStyle = suffixStyle; SuffixIconColor = suffixIconColor; SuffixIconConstraints = suffixIconConstraints;
        Counter = counter; CounterText = counterText; CounterStyle = counterStyle; Filled = filled; FillColor = fillColor;
        FocusColor = focusColor; HoverColor = hoverColor; ErrorBorder = errorBorder; FocusedBorder = focusedBorder;
        FocusedErrorBorder = focusedErrorBorder; DisabledBorder = disabledBorder; EnabledBorder = enabledBorder;
        Border = border; Enabled = enabled; SemanticCounterText = semanticCounterText; AlignLabelWithHint = alignLabelWithHint;
        Constraints = constraints;
    }

    public Widget? Icon { get; } public Color? IconColor { get; } public Widget? Label { get; } public string? LabelText { get; }
    public TextStyle? LabelStyle { get; } public TextStyle? FloatingLabelStyle { get; } public Widget? Helper { get; }
    public string? HelperText { get; } public TextStyle? HelperStyle { get; } public int? HelperMaxLines { get; }
    public string? HintText { get; } public Widget? Hint { get; } public TextStyle? HintStyle { get; } public int? HintMaxLines { get; }
    public Widget? Error { get; } public string? ErrorText { get; } public TextStyle? ErrorStyle { get; } public int? ErrorMaxLines { get; }
    public FloatingLabelBehavior? FloatingLabelBehavior { get; } public FloatingLabelAlignment? FloatingLabelAlignment { get; }
    public bool? IsCollapsed { get; } public bool? IsDense { get; } public Thickness? ContentPadding { get; }
    public Widget? PrefixIcon { get; } public BoxConstraints? PrefixIconConstraints { get; } public Widget? Prefix { get; }
    public string? PrefixText { get; } public TextStyle? PrefixStyle { get; } public Color? PrefixIconColor { get; }
    public Widget? SuffixIcon { get; } public Widget? Suffix { get; } public string? SuffixText { get; }
    public TextStyle? SuffixStyle { get; } public Color? SuffixIconColor { get; } public BoxConstraints? SuffixIconConstraints { get; }
    public Widget? Counter { get; } public string? CounterText { get; } public TextStyle? CounterStyle { get; }
    public bool? Filled { get; } public Color? FillColor { get; } public Color? FocusColor { get; } public Color? HoverColor { get; }
    public InputBorder? ErrorBorder { get; } public InputBorder? FocusedBorder { get; } public InputBorder? FocusedErrorBorder { get; }
    public InputBorder? DisabledBorder { get; } public InputBorder? EnabledBorder { get; } public InputBorder? Border { get; }
    public bool Enabled { get; } public string? SemanticCounterText { get; } public bool? AlignLabelWithHint { get; }
    public BoxConstraints? Constraints { get; }

    public static InputDecoration Collapsed(string? hintText = null, TextStyle? hintStyle = null, Widget? hint = null, bool enabled = true) =>
        new(hintText: hintText, hintStyle: hintStyle, hint: hint, isCollapsed: true, isDense: false,
            contentPadding: new Thickness(0), filled: false, border: InputBorder.None, enabled: enabled);

    public InputDecoration ApplyDefaults(InputDecorationThemeData theme) => new(
        icon: Icon, iconColor: IconColor ?? theme.IconColor, label: Label, labelText: LabelText,
        labelStyle: LabelStyle ?? theme.LabelStyle, floatingLabelStyle: FloatingLabelStyle ?? theme.FloatingLabelStyle,
        helper: Helper, helperText: HelperText, helperStyle: HelperStyle ?? theme.HelperStyle,
        helperMaxLines: HelperMaxLines ?? theme.HelperMaxLines, hintText: HintText, hint: Hint,
        hintStyle: HintStyle ?? theme.HintStyle, hintMaxLines: HintMaxLines ?? theme.HintMaxLines,
        error: Error, errorText: ErrorText, errorStyle: ErrorStyle ?? theme.ErrorStyle,
        errorMaxLines: ErrorMaxLines ?? theme.ErrorMaxLines, floatingLabelBehavior: FloatingLabelBehavior ?? theme.FloatingLabelBehavior,
        floatingLabelAlignment: FloatingLabelAlignment ?? theme.FloatingLabelAlignment,
        isCollapsed: IsCollapsed ?? theme.IsCollapsed, isDense: IsDense ?? theme.IsDense,
        contentPadding: ContentPadding ?? theme.ContentPadding, prefixIcon: PrefixIcon,
        prefixIconConstraints: PrefixIconConstraints, prefix: Prefix, prefixText: PrefixText,
        prefixStyle: PrefixStyle ?? theme.PrefixStyle, prefixIconColor: PrefixIconColor ?? theme.PrefixIconColor,
        suffixIcon: SuffixIcon, suffix: Suffix, suffixText: SuffixText, suffixStyle: SuffixStyle ?? theme.SuffixStyle,
        suffixIconColor: SuffixIconColor ?? theme.SuffixIconColor, suffixIconConstraints: SuffixIconConstraints,
        counter: Counter, counterText: CounterText, counterStyle: CounterStyle ?? theme.CounterStyle,
        filled: Filled ?? theme.Filled, fillColor: FillColor ?? theme.FillColor, focusColor: FocusColor ?? theme.FocusColor,
        hoverColor: HoverColor ?? theme.HoverColor, errorBorder: ErrorBorder ?? theme.ErrorBorder,
        focusedBorder: FocusedBorder ?? theme.FocusedBorder, focusedErrorBorder: FocusedErrorBorder ?? theme.FocusedErrorBorder,
        disabledBorder: DisabledBorder ?? theme.DisabledBorder, enabledBorder: EnabledBorder ?? theme.EnabledBorder,
        border: Border ?? theme.Border, enabled: Enabled, semanticCounterText: SemanticCounterText,
        alignLabelWithHint: AlignLabelWithHint, constraints: Constraints ?? theme.Constraints);

    internal InputDecoration WithRuntime(bool enabled, string? generatedCounterText) => new(
        icon: Icon, iconColor: IconColor, label: Label, labelText: LabelText, labelStyle: LabelStyle,
        floatingLabelStyle: FloatingLabelStyle, helper: Helper, helperText: HelperText, helperStyle: HelperStyle,
        helperMaxLines: HelperMaxLines, hintText: HintText, hint: Hint, hintStyle: HintStyle, hintMaxLines: HintMaxLines,
        error: Error, errorText: ErrorText, errorStyle: ErrorStyle, errorMaxLines: ErrorMaxLines,
        floatingLabelBehavior: FloatingLabelBehavior, floatingLabelAlignment: FloatingLabelAlignment,
        isCollapsed: IsCollapsed, isDense: IsDense, contentPadding: ContentPadding, prefixIcon: PrefixIcon,
        prefixIconConstraints: PrefixIconConstraints, prefix: Prefix, prefixText: PrefixText, prefixStyle: PrefixStyle,
        prefixIconColor: PrefixIconColor, suffixIcon: SuffixIcon, suffix: Suffix, suffixText: SuffixText,
        suffixStyle: SuffixStyle, suffixIconColor: SuffixIconColor, suffixIconConstraints: SuffixIconConstraints,
        counter: Counter, counterText: CounterText ?? generatedCounterText, counterStyle: CounterStyle,
        filled: Filled, fillColor: FillColor, focusColor: FocusColor, hoverColor: HoverColor,
        errorBorder: ErrorBorder, focusedBorder: FocusedBorder, focusedErrorBorder: FocusedErrorBorder,
        disabledBorder: DisabledBorder, enabledBorder: EnabledBorder, border: Border, enabled: enabled,
        semanticCounterText: SemanticCounterText, alignLabelWithHint: AlignLabelWithHint, constraints: Constraints);

    internal InputDecoration WithCounter(Widget counter) => new(
        icon: Icon, iconColor: IconColor, label: Label, labelText: LabelText, labelStyle: LabelStyle,
        floatingLabelStyle: FloatingLabelStyle, helper: Helper, helperText: HelperText, helperStyle: HelperStyle,
        helperMaxLines: HelperMaxLines, hintText: HintText, hint: Hint, hintStyle: HintStyle, hintMaxLines: HintMaxLines,
        error: Error, errorText: ErrorText, errorStyle: ErrorStyle, errorMaxLines: ErrorMaxLines,
        floatingLabelBehavior: FloatingLabelBehavior, floatingLabelAlignment: FloatingLabelAlignment,
        isCollapsed: IsCollapsed, isDense: IsDense, contentPadding: ContentPadding, prefixIcon: PrefixIcon,
        prefixIconConstraints: PrefixIconConstraints, prefix: Prefix, prefixText: PrefixText, prefixStyle: PrefixStyle,
        prefixIconColor: PrefixIconColor, suffixIcon: SuffixIcon, suffix: Suffix, suffixText: SuffixText,
        suffixStyle: SuffixStyle, suffixIconColor: SuffixIconColor, suffixIconConstraints: SuffixIconConstraints,
        counter: counter, counterStyle: CounterStyle, filled: Filled, fillColor: FillColor, focusColor: FocusColor,
        hoverColor: HoverColor, errorBorder: ErrorBorder, focusedBorder: FocusedBorder,
        focusedErrorBorder: FocusedErrorBorder, disabledBorder: DisabledBorder, enabledBorder: EnabledBorder,
        border: Border, enabled: Enabled, semanticCounterText: SemanticCounterText,
        alignLabelWithHint: AlignLabelWithHint, constraints: Constraints);

    internal InputDecoration WithFormError(string? errorText, Widget? error = null, bool clearHintText = false) => new(
        icon: Icon, iconColor: IconColor, label: Label, labelText: LabelText, labelStyle: LabelStyle,
        floatingLabelStyle: FloatingLabelStyle, helper: Helper, helperText: HelperText, helperStyle: HelperStyle,
        helperMaxLines: HelperMaxLines, hintText: clearHintText && HintText is not null ? string.Empty : HintText,
        hint: Hint, hintStyle: HintStyle, hintMaxLines: HintMaxLines,
        error: error, errorText: error is null ? errorText : null, errorStyle: ErrorStyle, errorMaxLines: ErrorMaxLines,
        floatingLabelBehavior: FloatingLabelBehavior, floatingLabelAlignment: FloatingLabelAlignment,
        isCollapsed: IsCollapsed, isDense: IsDense, contentPadding: ContentPadding, prefixIcon: PrefixIcon,
        prefixIconConstraints: PrefixIconConstraints, prefix: Prefix, prefixText: PrefixText, prefixStyle: PrefixStyle,
        prefixIconColor: PrefixIconColor, suffixIcon: SuffixIcon, suffix: Suffix, suffixText: SuffixText,
        suffixStyle: SuffixStyle, suffixIconColor: SuffixIconColor, suffixIconConstraints: SuffixIconConstraints,
        counter: Counter, counterText: CounterText, counterStyle: CounterStyle,
        filled: Filled, fillColor: FillColor, focusColor: FocusColor, hoverColor: HoverColor,
        errorBorder: ErrorBorder, focusedBorder: FocusedBorder, focusedErrorBorder: FocusedErrorBorder,
        disabledBorder: DisabledBorder, enabledBorder: EnabledBorder, border: Border, enabled: Enabled,
        semanticCounterText: SemanticCounterText, alignLabelWithHint: AlignLabelWithHint, constraints: Constraints);

    private static void ValidateLines(int? value, string name) { if (value.HasValue && value.Value <= 0) throw new ArgumentOutOfRangeException(name); }
}

public sealed class InputDecorator : StatefulWidget
{
    public InputDecorator(InputDecoration decoration, TextStyle? baseStyle = null, TextAlign? textAlign = null,
        bool isFocused = false, bool isHovering = false, bool expands = false, bool isEmpty = false,
        Widget? child = null, Key? key = null) : base(key)
    {
        Decoration = decoration ?? throw new ArgumentNullException(nameof(decoration)); BaseStyle = baseStyle;
        TextAlign = textAlign; IsFocused = isFocused; IsHovering = isHovering; Expands = expands; IsEmpty = isEmpty; Child = child;
    }
    public InputDecoration Decoration { get; } public TextStyle? BaseStyle { get; } public TextAlign? TextAlign { get; }
    public bool IsFocused { get; } public bool IsHovering { get; } public bool Expands { get; } public bool IsEmpty { get; }
    public Widget? Child { get; }
    public override State CreateState() => new InputDecoratorState();

    private sealed class InputDecoratorState : State
    {
        private AnimationController? _labelController;
        private InputDecorator Current => (InputDecorator)StateWidget;

        public override void InitState()
        {
            _labelController = new AnimationController(TimeSpan.FromMilliseconds(200)) { Curve = Curves.FastOutSlowIn };
            _labelController.Changed += Changed;
            _labelController.Forward(from: 0); _labelController.Stop();
        }
        public override void DidChangeDependencies()
        {
            var shouldFloat = ShouldFloat(Current, InputDecorationTheme.Of(Context));
            _labelController!.Forward(from: shouldFloat ? 1 : 0); _labelController.Stop();
        }
        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (InputDecorator)oldWidget;
            var inputTheme = InputDecorationTheme.Of(Context);
            if (ShouldFloat(old, inputTheme) == ShouldFloat(Current, inputTheme)) return;
            if (ShouldFloat(Current, inputTheme)) _labelController!.Forward(); else _labelController!.Reverse();
        }
        public override void Dispose() { _labelController!.Changed -= Changed; _labelController.Dispose(); }
        public override Widget Build(BuildContext context) => BuildDecorator(context, _labelController!.Evaluate());
        private void Changed() => SetState(() => { });

        private Widget BuildDecorator(BuildContext context, double floatProgress)
        {
            var theme = Theme.Of(context);
            var decoration = Current.Decoration.ApplyDefaults(InputDecorationTheme.Of(context));
            var hasError = decoration.Error is not null || decoration.ErrorText is not null;
            var dense = decoration.IsDense ?? false;
            var collapsed = decoration.IsCollapsed ?? false;
            var filled = decoration.Filled ?? false;
            var border = ResolveBorder(decoration, theme, hasError);
            var fillColor = ResolveFill(decoration, theme, filled);
            var baseStyle = Merge(theme.UseMaterial3 ? theme.TextTheme.BodyLarge : theme.TextTheme.TitleMedium, Current.BaseStyle);
            var labelStyle = Merge(baseStyle, decoration.LabelStyle).CopyWith(
                color: decoration.LabelStyle?.Color ?? (hasError ? theme.ErrorColor : theme.OnSurfaceVariantColor));
            var floatingStyle = Merge(labelStyle, decoration.FloatingLabelStyle).CopyWith(
                fontSize: decoration.FloatingLabelStyle?.FontSize ?? 12,
                color: decoration.FloatingLabelStyle?.Color ?? (hasError ? theme.ErrorColor : Current.IsFocused ? theme.PrimaryColor : theme.OnSurfaceVariantColor));
            var hintStyle = Merge(baseStyle.CopyWith(color: ApplyOpacity(theme.OnSurfaceColor, 0.60)), decoration.HintStyle);
            var helperStyle = Merge(theme.TextTheme.BodySmall.CopyWith(color: theme.OnSurfaceVariantColor), decoration.HelperStyle);
            var errorStyle = Merge(theme.TextTheme.BodySmall.CopyWith(color: theme.ErrorColor), decoration.ErrorStyle);
            var counterStyle = Merge(theme.TextTheme.BodySmall.CopyWith(color: theme.OnSurfaceVariantColor), decoration.CounterStyle);
            var contentPadding = decoration.ContentPadding ?? DefaultPadding(border, dense, filled, collapsed);
            var containerHeight = collapsed ? 24 : dense ? 48 : 56;
            if (Current.Expands) containerHeight = 80;

            Widget input = Current.Child ?? new SizedBox();
            var hintVisible = Current.IsEmpty && (decoration.Label is null && decoration.LabelText is null || floatProgress > 0.5);
            var centerChildren = new List<Widget>
            {
                new Padding(new Thickness(0, 12 * floatProgress, 0, 0), new Align(alignment: Alignment.CenterLeft, child: input)),
            };
            if (hintVisible)
                centerChildren.Add(new Align(alignment: Alignment.CenterLeft, child: Styled(decoration.Hint ?? TextOrNull(decoration.HintText, hintStyle, decoration.HintMaxLines), hintStyle)));
            if (decoration.Label is not null || decoration.LabelText is not null)
            {
                centerChildren.Add(new Opacity(1 - floatProgress, new Align(alignment: Alignment.CenterLeft, child: Styled(decoration.Label ?? TextOrNull(decoration.LabelText, labelStyle, 1), labelStyle))));
                Widget floatingLabel = Styled(decoration.Label ?? TextOrNull(decoration.LabelText, floatingStyle, 1), floatingStyle);
                if (border.IsOutline)
                    floatingLabel = new ColoredBox(fillColor.A > 0 ? fillColor : theme.CanvasColor,
                        new Padding(new Thickness((border as OutlineInputBorder)?.GapPadding ?? 4, 0), floatingLabel));
                centerChildren.Add(new Positioned(
                    left: decoration.FloatingLabelAlignment == Plumix.Material.FloatingLabelAlignment.Center ? null : 0,
                    top: border.IsOutline ? -1 : 1,
                    child: new Opacity(floatProgress, floatingLabel)));
            }
            Widget center = new Stack(fit: StackFit.Expand, children: centerChildren);

            var middleChildren = new List<Widget>();
            if (decoration.Prefix is not null || decoration.PrefixText is not null)
                middleChildren.Add(Styled(decoration.Prefix ?? TextOrNull(decoration.PrefixText, decoration.PrefixStyle ?? baseStyle, 1), decoration.PrefixStyle ?? baseStyle));
            middleChildren.Add(new Expanded(center));
            if (decoration.Suffix is not null || decoration.SuffixText is not null)
                middleChildren.Add(Styled(decoration.Suffix ?? TextOrNull(decoration.SuffixText, decoration.SuffixStyle ?? baseStyle, 1), decoration.SuffixStyle ?? baseStyle));

            var fieldChildren = new List<Widget>();
            if (decoration.PrefixIcon is not null) fieldChildren.Add(IconSlot(decoration.PrefixIcon, decoration.PrefixIconConstraints, decoration.PrefixIconColor));
            fieldChildren.Add(new Expanded(new Padding(contentPadding, new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: middleChildren))));
            if (decoration.SuffixIcon is not null) fieldChildren.Add(IconSlot(decoration.SuffixIcon, decoration.SuffixIconConstraints, decoration.SuffixIconColor));
            var fieldRow = new Row(crossAxisAlignment: CrossAxisAlignment.Center, children: fieldChildren);
            Widget container = new CustomPaint(
                painter: new InputBorderPainter(border, fillColor),
                child: new SizedBox(height: containerHeight, child: fieldRow));
            if (decoration.Constraints.HasValue) container = new ConstrainedBox(decoration.Constraints.Value, container);

            var bottom = BuildBottom(decoration, hasError ? errorStyle : helperStyle, counterStyle, hasError);
            Widget decorated = new Column(mainAxisSize: MainAxisSize.Min, crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: bottom is null ? [container] : [container, bottom]);
            if (decoration.Icon is not null)
                decorated = new Row(crossAxisAlignment: CrossAxisAlignment.Center,
                    children: [new Padding(new Thickness(0, 0, 16, 0), Styled(decoration.Icon, new TextStyle(Color: decoration.IconColor))), new Expanded(decorated)]);
            return decorated;
        }

        private InputBorder ResolveBorder(InputDecoration d, ThemeData theme, bool error)
        {
            var selected = !d.Enabled ? d.DisabledBorder
                : error && Current.IsFocused ? d.FocusedErrorBorder ?? d.ErrorBorder
                : error ? d.ErrorBorder
                : Current.IsFocused ? d.FocusedBorder
                : d.EnabledBorder;
            var border = selected ?? d.Border ?? new UnderlineInputBorder();
            if (ReferenceEquals(border, InputBorder.None)) return border;
            if (selected is not null && selected.BorderSide.Width > 0) return selected;
            var color = !d.Enabled ? ApplyOpacity(theme.OnSurfaceColor, 0.12)
                : error ? theme.ErrorColor : Current.IsFocused ? theme.PrimaryColor
                : border.IsOutline ? theme.OutlineColor : theme.OnSurfaceVariantColor;
            var width = Current.IsFocused ? 2.0 : 1.0;
            return border.CopyWith(new BorderSide(color, width));
        }

        private Color ResolveFill(InputDecoration d, ThemeData theme, bool filled)
        {
            if (!filled) return Colors.Transparent;
            var color = d.FillColor ?? (theme.UseMaterial3 ? theme.SurfaceContainerHighestColor : ApplyOpacity(theme.OnSurfaceColor, 0.04));
            if (Current.IsFocused && d.FocusColor.HasValue) color = Blend(color, d.FocusColor.Value);
            if (Current.IsHovering && d.Enabled) color = Blend(color, d.HoverColor ?? theme.HoverColor);
            return color;
        }

        private static Widget? BuildBottom(InputDecoration d, TextStyle supportStyle, TextStyle counterStyle, bool hasError)
        {
            Widget? support = hasError ? d.Error ?? TextOrNull(d.ErrorText, supportStyle, d.ErrorMaxLines)
                : d.Helper ?? TextOrNull(d.HelperText, supportStyle, d.HelperMaxLines);
            Widget? counter = d.Counter ?? TextOrNull(d.CounterText, counterStyle, 1);
            if (support is null && counter is null) return null;
            var children = new List<Widget> { new Expanded(support ?? new SizedBox()) };
            if (counter is not null) children.Add(counter);
            return new Padding(new Thickness(16, 4, 16, 0), new Row(
                crossAxisAlignment: CrossAxisAlignment.Start,
                children: children));
        }

        private static Widget IconSlot(Widget icon, BoxConstraints? constraints, Color? color) =>
            new ConstrainedBox(constraints ?? new BoxConstraints(MinWidth: 48, MinHeight: 48),
                new Center(child: color.HasValue ? new IconTheme(new IconThemeData(Color: color), icon) : icon));
        private static Widget Styled(Widget? child, TextStyle style) => child is null ? new SizedBox() : new DefaultTextStyle(style, child);
        private static Widget? TextOrNull(string? text, TextStyle style, int? maxLines) => text is null ? null : new Text(text, maxLines: maxLines, overflow: TextOverflow.Ellipsis,
            fontFamily: style.FontFamily, fontSize: style.FontSize, color: style.Color, fontWeight: style.FontWeight, fontStyle: style.FontStyle, height: style.Height, letterSpacing: style.LetterSpacing);
        private static bool ShouldFloat(InputDecorator value, InputDecorationThemeData theme) =>
            value.Decoration.ApplyDefaults(theme).FloatingLabelBehavior switch
        {
            Plumix.Material.FloatingLabelBehavior.Always => true,
            Plumix.Material.FloatingLabelBehavior.Never => false,
            _ => !value.IsEmpty || value.IsFocused && value.Decoration.Enabled,
        };
        private static Thickness DefaultPadding(InputBorder border, bool dense, bool filled, bool collapsed) => collapsed ? new Thickness(0)
            : border.IsOutline ? new Thickness(12, dense ? 8 : 12)
            : filled ? new Thickness(12, dense ? 8 : 12)
            : new Thickness(0, dense ? 8 : 12);
        private static TextStyle Merge(TextStyle a, TextStyle? b) => b is null ? a : new TextStyle(
            b.FontFamily ?? a.FontFamily, b.FontSize ?? a.FontSize, b.Color ?? a.Color, b.FontWeight ?? a.FontWeight,
            b.FontStyle ?? a.FontStyle, b.Height ?? a.Height, b.LetterSpacing ?? a.LetterSpacing);
        private static Color ApplyOpacity(Color c, double opacity) => Color.FromArgb((byte)Math.Round(c.A * Math.Clamp(opacity, 0, 1)), c.R, c.G, c.B);
        private static Color Blend(Color a, Color b) { var alpha = b.A / 255.0; return Color.FromArgb(a.A,
            (byte)Math.Round(a.R + (b.R - a.R) * alpha), (byte)Math.Round(a.G + (b.G - a.G) * alpha), (byte)Math.Round(a.B + (b.B - a.B) * alpha)); }
    }
}

internal sealed class InputBorderPainter : CustomPainter
{
    private readonly InputBorder _border;
    private readonly Color _fillColor;

    public InputBorderPainter(InputBorder border, Color fillColor)
    {
        _border = border;
        _fillColor = fillColor;
    }

    internal InputBorder Border => _border;
    internal Color FillColor => _fillColor;

    public override void Paint(PaintingContext context, Size size)
    {
        var rect = new Rect(size);
        var radius = _border.BorderRadius.Radius;
        if (_fillColor.A > 0) context.DrawRectangle(new SolidColorBrush(_fillColor), null, rect, radius, radius);
        if (ReferenceEquals(_border, InputBorder.None) || _border.BorderSide.Width <= 0) return;
        var pen = new Pen(new SolidColorBrush(_border.BorderSide.Color), _border.BorderSide.Width);
        if (_border.IsOutline) context.DrawRectangle(Brushes.Transparent, pen, rect, radius, radius);
        else context.DrawLine(pen, new Point(0, size.Height - _border.BorderSide.Width / 2), new Point(size.Width, size.Height - _border.BorderSide.Width / 2));
    }
    public override bool ShouldRepaint(CustomPainter oldDelegate) => oldDelegate is not InputBorderPainter old
        || old._fillColor != _fillColor || old._border.GetType() != _border.GetType()
        || old._border.BorderSide != _border.BorderSide || old._border.BorderRadius != _border.BorderRadius;
}
