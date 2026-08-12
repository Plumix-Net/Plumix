using Avalonia;
using Avalonia.Media;
using System.Globalization;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/input_decorator.dart

public enum FloatingLabelBehavior { Never, Auto, Always }

public enum FloatingLabelAlignment { Start, Center }

public sealed record InputDecoration
{
    internal const double FinalLabelScale = 0.75;
    internal const double InputExtraPadding = 4.0;
    internal static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(167);
    internal static readonly TimeSpan HintFadeTransitionDuration = TimeSpan.FromMilliseconds(20);

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
        TextDirection? hintTextDirection = null,
        int? hintMaxLines = null,
        TimeSpan? hintFadeDuration = null,
        bool maintainHintSize = true,
        bool maintainLabelSize = false,
        Widget? error = null,
        string? errorText = null,
        TextStyle? errorStyle = null,
        int? errorMaxLines = null,
        FloatingLabelBehavior? floatingLabelBehavior = null,
        FloatingLabelAlignment? floatingLabelAlignment = null,
        bool? isCollapsed = null,
        bool? isDense = null,
        EdgeInsetsGeometry? contentPadding = null,
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
        BoxConstraints? constraints = null,
        VisualDensity? visualDensity = null)
    {
        if (label is not null && labelText is not null)
        {
            throw new ArgumentException("Declaring both label and labelText is not supported.");
        }

        if (hint is not null && hintText is not null)
        {
            throw new ArgumentException("Declaring both hint and hintText is not supported.");
        }

        if (helper is not null && helperText is not null)
        {
            throw new ArgumentException("Declaring both helper and helperText is not supported.");
        }

        if (prefix is not null && prefixText is not null)
        {
            throw new ArgumentException("Declaring both prefix and prefixText is not supported.");
        }

        if (suffix is not null && suffixText is not null)
        {
            throw new ArgumentException("Declaring both suffix and suffixText is not supported.");
        }

        if (error is not null && errorText is not null)
        {
            throw new ArgumentException("Declaring both error and errorText is not supported.");
        }

        ValidateLines(helperMaxLines, nameof(helperMaxLines));
        ValidateLines(hintMaxLines, nameof(hintMaxLines));
        ValidateLines(errorMaxLines, nameof(errorMaxLines));

        Icon = icon;
        IconColor = iconColor;
        Label = label;
        LabelText = labelText;
        LabelStyle = labelStyle;
        FloatingLabelStyle = floatingLabelStyle;
        Helper = helper;
        HelperText = helperText;
        HelperStyle = helperStyle;
        HelperMaxLines = helperMaxLines;
        HintText = hintText;
        Hint = hint;
        HintStyle = hintStyle;
        HintTextDirection = hintTextDirection;
        HintMaxLines = hintMaxLines;
        HintFadeDuration = hintFadeDuration;
        MaintainHintSize = maintainHintSize;
        MaintainLabelSize = maintainLabelSize;
        Error = error;
        ErrorText = errorText;
        ErrorStyle = errorStyle;
        ErrorMaxLines = errorMaxLines;
        FloatingLabelBehavior = floatingLabelBehavior;
        FloatingLabelAlignment = floatingLabelAlignment;
        IsCollapsed = isCollapsed;
        IsDense = isDense;
        ContentPadding = contentPadding;
        PrefixIcon = prefixIcon;
        PrefixIconConstraints = prefixIconConstraints;
        Prefix = prefix;
        PrefixText = prefixText;
        PrefixStyle = prefixStyle;
        PrefixIconColor = prefixIconColor;
        SuffixIcon = suffixIcon;
        Suffix = suffix;
        SuffixText = suffixText;
        SuffixStyle = suffixStyle;
        SuffixIconColor = suffixIconColor;
        SuffixIconConstraints = suffixIconConstraints;
        Counter = counter;
        CounterText = counterText;
        CounterStyle = counterStyle;
        Filled = filled;
        FillColor = fillColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        ErrorBorder = errorBorder;
        FocusedBorder = focusedBorder;
        FocusedErrorBorder = focusedErrorBorder;
        DisabledBorder = disabledBorder;
        EnabledBorder = enabledBorder;
        Border = border;
        Enabled = enabled;
        SemanticCounterText = semanticCounterText;
        AlignLabelWithHint = alignLabelWithHint;
        Constraints = constraints;
        VisualDensity = visualDensity;
    }

    public Widget? Icon { get; init; }
    public Color? IconColor { get; init; }
    public Widget? Label { get; init; }
    public string? LabelText { get; init; }
    public TextStyle? LabelStyle { get; init; }
    public TextStyle? FloatingLabelStyle { get; init; }
    public Widget? Helper { get; init; }
    public string? HelperText { get; init; }
    public TextStyle? HelperStyle { get; init; }
    public int? HelperMaxLines { get; init; }
    public string? HintText { get; init; }
    public Widget? Hint { get; init; }
    public TextStyle? HintStyle { get; init; }
    public TextDirection? HintTextDirection { get; init; }
    public int? HintMaxLines { get; init; }
    public TimeSpan? HintFadeDuration { get; init; }
    public bool MaintainHintSize { get; init; }
    public bool MaintainLabelSize { get; init; }
    public Widget? Error { get; init; }
    public string? ErrorText { get; init; }
    public TextStyle? ErrorStyle { get; init; }
    public int? ErrorMaxLines { get; init; }
    public FloatingLabelBehavior? FloatingLabelBehavior { get; init; }
    public FloatingLabelAlignment? FloatingLabelAlignment { get; init; }
    public bool? IsCollapsed { get; init; }
    public bool? IsDense { get; init; }
    public EdgeInsetsGeometry? ContentPadding { get; init; }
    public Widget? PrefixIcon { get; init; }
    public BoxConstraints? PrefixIconConstraints { get; init; }
    public Widget? Prefix { get; init; }
    public string? PrefixText { get; init; }
    public TextStyle? PrefixStyle { get; init; }
    public Color? PrefixIconColor { get; init; }
    public Widget? SuffixIcon { get; init; }
    public Widget? Suffix { get; init; }
    public string? SuffixText { get; init; }
    public TextStyle? SuffixStyle { get; init; }
    public Color? SuffixIconColor { get; init; }
    public BoxConstraints? SuffixIconConstraints { get; init; }
    public Widget? Counter { get; init; }
    public string? CounterText { get; init; }
    public TextStyle? CounterStyle { get; init; }
    public bool? Filled { get; init; }
    public Color? FillColor { get; init; }
    public Color? FocusColor { get; init; }
    public Color? HoverColor { get; init; }
    public InputBorder? ErrorBorder { get; init; }
    public InputBorder? FocusedBorder { get; init; }
    public InputBorder? FocusedErrorBorder { get; init; }
    public InputBorder? DisabledBorder { get; init; }
    public InputBorder? EnabledBorder { get; init; }
    public InputBorder? Border { get; init; }
    public bool Enabled { get; init; }
    public string? SemanticCounterText { get; init; }
    public bool? AlignLabelWithHint { get; init; }
    public BoxConstraints? Constraints { get; init; }
    public VisualDensity? VisualDensity { get; init; }

    public static InputDecoration Collapsed(
        string? hintText = null,
        TextStyle? hintStyle = null,
        Widget? hint = null,
        TextDirection? hintTextDirection = null,
        int? hintMaxLines = null,
        TimeSpan? hintFadeDuration = null,
        bool maintainHintSize = true,
        bool? filled = null,
        Color? fillColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        bool enabled = true,
        BoxConstraints? constraints = null) => new(
        hintText: hintText,
        hintStyle: hintStyle,
        hint: hint,
        hintTextDirection: hintTextDirection,
        hintMaxLines: hintMaxLines,
        hintFadeDuration: hintFadeDuration,
        maintainHintSize: maintainHintSize,
        isCollapsed: true,
        isDense: false,
        contentPadding: EdgeInsetsGeometry.Zero,
        filled: filled ?? false,
        fillColor: fillColor,
        focusColor: focusColor,
        hoverColor: hoverColor,
        border: InputBorder.None,
        enabled: enabled,
        alignLabelWithHint: false,
        constraints: constraints);

    public InputDecoration ApplyDefaults(InputDecorationThemeData theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return this with
        {
            LabelStyle = LabelStyle ?? theme.LabelStyle,
            FloatingLabelStyle = FloatingLabelStyle ?? theme.FloatingLabelStyle,
            HelperStyle = HelperStyle ?? theme.HelperStyle,
            HelperMaxLines = HelperMaxLines ?? theme.HelperMaxLines,
            HintStyle = HintStyle ?? theme.HintStyle,
            HintFadeDuration = HintFadeDuration ?? theme.HintFadeDuration,
            HintMaxLines = HintMaxLines ?? theme.HintMaxLines,
            ErrorStyle = ErrorStyle ?? theme.ErrorStyle,
            ErrorMaxLines = ErrorMaxLines ?? theme.ErrorMaxLines,
            FloatingLabelBehavior = FloatingLabelBehavior ?? theme.FloatingLabelBehavior,
            FloatingLabelAlignment = FloatingLabelAlignment ?? theme.FloatingLabelAlignment,
            IsDense = IsDense ?? theme.IsDense,
            ContentPadding = ContentPadding ?? theme.ContentPadding,
            IsCollapsed = IsCollapsed ?? theme.IsCollapsed,
            IconColor = IconColor ?? theme.IconColor,
            PrefixStyle = PrefixStyle ?? theme.PrefixStyle,
            PrefixIconColor = PrefixIconColor ?? theme.PrefixIconColor,
            PrefixIconConstraints = PrefixIconConstraints ?? theme.PrefixIconConstraints,
            SuffixStyle = SuffixStyle ?? theme.SuffixStyle,
            SuffixIconColor = SuffixIconColor ?? theme.SuffixIconColor,
            SuffixIconConstraints = SuffixIconConstraints ?? theme.SuffixIconConstraints,
            CounterStyle = CounterStyle ?? theme.CounterStyle,
            Filled = Filled ?? theme.Filled,
            FillColor = FillColor ?? theme.FillColor,
            FocusColor = FocusColor ?? theme.FocusColor,
            HoverColor = HoverColor ?? theme.HoverColor,
            ErrorBorder = ErrorBorder ?? theme.ErrorBorder,
            FocusedBorder = FocusedBorder ?? theme.FocusedBorder,
            FocusedErrorBorder = FocusedErrorBorder ?? theme.FocusedErrorBorder,
            DisabledBorder = DisabledBorder ?? theme.DisabledBorder,
            EnabledBorder = EnabledBorder ?? theme.EnabledBorder,
            Border = Border ?? theme.Border,
            AlignLabelWithHint = AlignLabelWithHint ?? theme.AlignLabelWithHint,
            Constraints = Constraints ?? theme.Constraints,
            VisualDensity = VisualDensity ?? theme.VisualDensity,
        };
    }

    internal InputDecoration WithRuntime(bool enabled, string? generatedCounterText) => this with
    {
        Enabled = enabled,
        CounterText = CounterText ?? generatedCounterText,
    };

    internal InputDecoration WithCounter(Widget counter) => this with { Counter = counter, CounterText = null };

    internal InputDecoration WithFormError(string? errorText, Widget? error = null, bool clearHintText = false) =>
        this with
        {
            HintText = clearHintText && HintText is not null ? string.Empty : HintText,
            Error = error,
            ErrorText = error is null ? errorText : null,
        };

    internal InputDecoration WithSuffixIcon(Widget? suffixIcon) => this with { SuffixIcon = suffixIcon };

    internal InputDecoration WithLabels(Widget? label, string? hintText, string? helperText) => this with
    {
        Label = label ?? Label,
        HintText = hintText ?? HintText,
        HelperText = helperText ?? HelperText,
    };

    private static void ValidateLines(int? value, string name)
    {
        if (value.HasValue && value.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

public sealed class InputDecorator : StatefulWidget
{
    public InputDecorator(
        InputDecoration decoration,
        TextStyle? baseStyle = null,
        TextAlign? textAlign = null,
        TextAlignVertical? textAlignVertical = null,
        bool isFocused = false,
        bool isHovering = false,
        bool expands = false,
        bool isEmpty = false,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        Decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        BaseStyle = baseStyle;
        TextAlign = textAlign;
        TextAlignVertical = textAlignVertical;
        IsFocused = isFocused;
        IsHovering = isHovering;
        Expands = expands;
        IsEmpty = isEmpty;
        Child = child;
    }

    public InputDecoration Decoration { get; }
    public TextStyle? BaseStyle { get; }
    public TextAlign? TextAlign { get; }
    public TextAlignVertical? TextAlignVertical { get; }
    public bool IsFocused { get; }
    public bool IsHovering { get; }
    public bool Expands { get; }
    public bool IsEmpty { get; }
    public Widget? Child { get; }

    internal bool LabelShouldWithdraw => !IsEmpty || (IsFocused && Decoration.Enabled);

    internal static readonly SemanticsTag PrefixSemanticsTag = new("_InputDecoratorState.prefix");
    internal static readonly SemanticsTag PrefixIconSemanticsTag = new("_InputDecoratorState.prefixIcon");
    internal static readonly SemanticsTag SuffixSemanticsTag = new("_InputDecoratorState.suffix");
    internal static readonly SemanticsTag SuffixIconSemanticsTag = new("_InputDecoratorState.suffixIcon");

    /// The affix tags in the order the render object groups them into sibling semantics nodes.
    internal static readonly SemanticsTag[] AffixSemanticsTags =
    [
        PrefixSemanticsTag,
        PrefixIconSemanticsTag,
        SuffixSemanticsTag,
        SuffixIconSemanticsTag,
    ];

    public override State CreateState() => new InputDecoratorState();

    private sealed class InputDecoratorState : State
    {
        private AnimationController _floatingLabelController = null!;
        private CurvedAnimation _floatingLabelAnimation = null!;
        private AnimationController _shakingLabelController = null!;
        private readonly InputBorderGap _borderGap = new();
        private InputDecoration? _effectiveDecoration;

        // Provide a unique name to avoid mixing up sort order with sibling input decorators.
        private OrdinalSortKey _prefixSemanticsSortOrder = null!;
        private OrdinalSortKey _inputSemanticsSortOrder = null!;
        private OrdinalSortKey _suffixSemanticsSortOrder = null!;

        private InputDecorator Current => (InputDecorator)StateWidget;

        public override void InitState()
        {
            bool labelIsInitiallyFloating =
                Current.Decoration.FloatingLabelBehavior != Plumix.Material.FloatingLabelBehavior.Never
                && Current.LabelShouldWithdraw;
            _floatingLabelController = new AnimationController(InputDecoration.TransitionDuration, this);
            _floatingLabelController.SetValue(labelIsInitiallyFloating ? 1.0 : 0.0);
            _floatingLabelController.AddListener(HandleChange);
            _floatingLabelAnimation = new CurvedAnimation(
                _floatingLabelController,
                Curves.FastOutSlowIn,
                Curves.Flipped(Curves.FastOutSlowIn));
            _shakingLabelController = new AnimationController(InputDecoration.TransitionDuration, this);

            string group = GetHashCode().ToString(CultureInfo.InvariantCulture);
            _prefixSemanticsSortOrder = new OrdinalSortKey(0, group);
            _inputSemanticsSortOrder = new OrdinalSortKey(1, group);
            _suffixSemanticsSortOrder = new OrdinalSortKey(2, group);
        }

        public override void DidChangeDependencies()
        {
            _effectiveDecoration = null;
            bool labelIsFloating =
                Decoration.FloatingLabelBehavior != Plumix.Material.FloatingLabelBehavior.Never
                && LabelShouldWithdraw;
            _floatingLabelController.SetValue(labelIsFloating ? 1.0 : 0.0);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (InputDecorator)oldWidget;
            if (!Equals(Current.Decoration, old.Decoration))
            {
                _effectiveDecoration = null;
            }

            bool floatBehaviorChanged =
                Current.Decoration.FloatingLabelBehavior != old.Decoration.FloatingLabelBehavior;
            if (Current.LabelShouldWithdraw != old.LabelShouldWithdraw || floatBehaviorChanged)
            {
                if (FloatingLabelEnabled && LabelShouldWithdraw)
                {
                    _floatingLabelController.Forward();
                }
                else
                {
                    _floatingLabelController.Reverse();
                }
            }

            string? errorText = Current.Decoration.ErrorText;
            string? oldErrorText = old.Decoration.ErrorText;
            if (_floatingLabelController.Status == AnimationStatus.Completed
                && errorText is not null
                && errorText != oldErrorText)
            {
                _shakingLabelController.Forward(from: 0.0);
            }
        }

        public override void Dispose()
        {
            _floatingLabelController.RemoveListener(HandleChange);
            _floatingLabelController.Dispose();
            _floatingLabelAnimation.Dispose();
            _shakingLabelController.Dispose();
            _borderGap.Dispose();
        }

        private void HandleChange() => SetState(() => { });

        private InputDecoration Decoration =>
            _effectiveDecoration ??= Current.Decoration.ApplyDefaults(InputDecorationTheme.Of(Context));

        private bool LabelShouldWithdraw => Current.LabelShouldWithdraw
                                            || Decoration.FloatingLabelBehavior
                                            == Plumix.Material.FloatingLabelBehavior.Always;

        private bool FloatingLabelEnabled =>
            Decoration.FloatingLabelBehavior != Plumix.Material.FloatingLabelBehavior.Never;

        private bool HasInlineLabel =>
            !LabelShouldWithdraw && (Decoration.LabelText is not null || Decoration.Label is not null);

        private bool ShouldShowLabel => HasInlineLabel || FloatingLabelEnabled;

        private bool HasError => Decoration.ErrorText is not null || Decoration.Error is not null;

        private bool IsHovering => Current.IsHovering && Decoration.Enabled;

        private MaterialState WidgetState
        {
            get
            {
                MaterialState states = MaterialState.None;
                if (!Decoration.Enabled) states |= MaterialState.Disabled;
                if (Current.IsFocused) states |= MaterialState.Focused;
                if (IsHovering) states |= MaterialState.Hovered;
                if (HasError) states |= MaterialState.Error;
                return states;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var themeData = Theme.Of(context);
            InputDecoration decoration = Decoration;
            MaterialState states = WidgetState;
            InputDecoratorDefaults defaults = InputDecoratorDefaults.Resolve(themeData, states, HasError);
            var textDirection = Directionality.Of(context);
            bool flipHorizontal = textDirection == TextDirection.Rtl;

            InputBorder border = ResolveBorder(themeData, defaults, states, context);
            bool isDense = decoration.IsDense ?? false;
            bool isCollapsed = decoration.IsCollapsed ?? false;
            bool filled = decoration.Filled ?? false;
            VisualDensity visualDensity = decoration.VisualDensity ?? themeData.VisualDensity;

            TextStyle labelStyle = themeData.TextTheme.TitleMedium
                .Merge(Current.BaseStyle)
                .Merge(defaults.LabelStyle)
                .Merge(decoration.LabelStyle)
                .CopyWith(height: 1.0);
            TextStyle hintStyle = (themeData.UseMaterial3
                    ? themeData.TextTheme.BodyLarge
                    : themeData.TextTheme.TitleMedium)
                .Merge(Current.BaseStyle)
                .Merge(defaults.HintStyle)
                .Merge(decoration.HintStyle);
            TextStyle floatingLabelStyle = BuildFloatingLabelStyle(themeData, defaults, decoration);
            TextStyle helperStyle = defaults.HelperStyle.Merge(decoration.HelperStyle);
            TextStyle errorStyle = defaults.ErrorStyle.Merge(decoration.ErrorStyle);
            TextStyle counterStyle = helperStyle.Merge(decoration.CounterStyle);

            double inputGap = 0.0;
            if (themeData.UseMaterial3)
            {
                inputGap = border is OutlineInputBorder outline
                    ? outline.GapPadding
                    : border.IsOutline || filled ? InputDecoration.InputExtraPadding : 0.0;
            }

            double floatingLabelHeight = isCollapsed || border.IsOutline
                ? 0.0
                : MediaQuery.TextScaleFactorOf(context) * (4.0 + (0.75 * (labelStyle.FontSize ?? 16.0)));

            EdgeInsetsGeometry contentPadding = ResolveContentPadding(
                decoration,
                themeData.UseMaterial3,
                border.IsOutline,
                isCollapsed,
                isDense,
                filled,
                textDirection,
                flipHorizontal);

            double iconSize = isDense ? 18.0 : 24.0;
            Color fillColor = filled
                ? decoration.FillColor ?? defaults.FillColor
                : Colors.Transparent;
            Color hoverColor = !filled || !decoration.Enabled
                ? Colors.Transparent
                : decoration.HoverColor ?? themeData.HoverColor;

            Widget container = new BorderContainer(
                border: border,
                gap: _borderGap,
                gapAnimation: _floatingLabelAnimation,
                fillColor: fillColor,
                hoverColor: hoverColor,
                isHovering: IsHovering,
                textDirection: textDirection);

            Widget? icon = decoration.Icon is null
                ? null
                : new MouseRegion(
                    cursor: SystemMouseCursors.Basic,
                    child: new Padding(
                        EdgeInsetsGeometry.DirectionalOnly(end: 16.0).Resolve(textDirection),
                        new IconTheme(
                            new IconThemeData(
                                Color: decoration.IconColor ?? defaults.IconColor,
                                Size: iconSize),
                            decoration.Icon)));

            Widget? prefixIcon = BuildIconSlot(
                decoration.PrefixIcon,
                decoration.PrefixIconConstraints,
                decoration.PrefixIconColor ?? defaults.PrefixIconColor,
                iconSize,
                visualDensity,
                InputDecorator.PrefixIconSemanticsTag);
            Widget? suffixIcon = BuildIconSlot(
                decoration.SuffixIcon,
                decoration.SuffixIconConstraints,
                decoration.SuffixIconColor ?? defaults.SuffixIconColor,
                iconSize,
                visualDensity,
                InputDecorator.SuffixIconSemanticsTag);

            bool hasPrefix = decoration.Prefix is not null || decoration.PrefixText is not null;
            bool hasSuffix = decoration.Suffix is not null || decoration.SuffixText is not null;
            Widget? input = Current.Child;

            // If at least two out of the three are visible, it needs semantics sort order.
            bool needsSemanticsSortOrder = LabelShouldWithdraw
                                           && (input is not null
                                               ? hasPrefix || hasSuffix
                                               : hasPrefix && hasSuffix);

            Widget? prefix = BuildAffix(
                decoration.Prefix,
                decoration.PrefixText,
                decoration.PrefixStyle ?? hintStyle,
                needsSemanticsSortOrder ? _prefixSemanticsSortOrder : null,
                InputDecorator.PrefixSemanticsTag);
            Widget? suffix = BuildAffix(
                decoration.Suffix,
                decoration.SuffixText,
                decoration.SuffixStyle ?? hintStyle,
                needsSemanticsSortOrder ? _suffixSemanticsSortOrder : null,
                InputDecorator.SuffixSemanticsTag);

            if (input is not null && needsSemanticsSortOrder)
            {
                input = new Semantics(container: true, sortKey: _inputSemanticsSortOrder, child: input);
            }

            Widget? label = BuildLabel(decoration, labelStyle, floatingLabelStyle);
            Widget? hint = BuildHint(decoration, hintStyle);
            Widget helperError = new HelperError(
                textAlign: Current.TextAlign,
                helper: decoration.Helper,
                helperText: decoration.HelperText,
                helperStyle: helperStyle,
                helperMaxLines: decoration.HelperMaxLines,
                error: decoration.Error,
                errorText: decoration.ErrorText,
                errorStyle: errorStyle,
                errorMaxLines: decoration.ErrorMaxLines);

            Widget? counter = decoration.Counter;
            if (counter is null && !string.IsNullOrEmpty(decoration.CounterText))
            {
                counter = new Semantics(
                    container: true,
                    liveRegion: Current.IsFocused,
                    label: decoration.SemanticCounterText,
                    child: Styled(
                        new Text(decoration.CounterText!, overflow: TextOverflow.Ellipsis, maxLines: 1),
                        counterStyle));
            }

            var spec = new DecorationSpec(
                ContentPadding: contentPadding,
                IsCollapsed: isCollapsed,
                FloatingLabelHeight: floatingLabelHeight,
                FloatingLabelProgress: _floatingLabelAnimation.Value,
                FloatingLabelAlignment: decoration.FloatingLabelAlignment
                                        ?? Plumix.Material.FloatingLabelAlignment.Start,
                Border: border,
                BorderGap: _borderGap,
                AlignLabelWithHint: decoration.AlignLabelWithHint ?? false,
                IsDense: isDense,
                IsEmpty: Current.IsEmpty,
                VisualDensity: visualDensity,
                InputGap: inputGap,
                MaintainHintSize: decoration.MaintainHintSize,
                MaintainLabelSize: decoration.MaintainLabelSize,
                Icon: icon,
                Input: input,
                Label: label,
                Hint: hint,
                Prefix: prefix,
                Suffix: suffix,
                PrefixIcon: prefixIcon,
                SuffixIcon: suffixIcon,
                HelperError: helperError,
                Counter: counter,
                Container: container);

            Widget decorator = new DecoratorRenderWidget(
                decoration: spec,
                textDirection: textDirection,
                textBaseline: labelStyle.TextBaseline ?? Plumix.UI.TextBaseline.Alphabetic,
                textAlignVertical: Current.TextAlignVertical,
                isFocused: Current.IsFocused,
                expands: Current.Expands,
                material3: themeData.UseMaterial3);

            Widget result = new Semantics(hint: decoration.ErrorText, child: decorator);
            if (decoration.Constraints.HasValue)
            {
                result = new ConstrainedBox(decoration.Constraints.Value, result);
            }

            return result;
        }

        private static TextStyle BuildFloatingLabelStyle(
            ThemeData themeData,
            InputDecoratorDefaults defaults,
            InputDecoration decoration)
        {
            TextStyle defaultTextStyle = defaults.FloatingLabelStyle;
            if ((decoration.ErrorText is not null || decoration.Error is not null)
                && decoration.ErrorStyle?.Color is { } errorColor)
            {
                defaultTextStyle = defaultTextStyle.CopyWith(color: errorColor);
            }

            defaultTextStyle = defaultTextStyle.Merge(decoration.FloatingLabelStyle ?? decoration.LabelStyle);
            return themeData.TextTheme.TitleMedium
                .Merge(defaultTextStyle)
                .Merge(decoration.FloatingLabelStyle)
                .CopyWith(height: 1.0);
        }

        private Widget? BuildLabel(
            InputDecoration decoration,
            TextStyle labelStyle,
            TextStyle floatingLabelStyle)
        {
            if (decoration.Label is null && decoration.LabelText is null)
            {
                return null;
            }

            Widget content = decoration.Label
                             ?? new Text(
                                 decoration.LabelText!,
                                 overflow: TextOverflow.Ellipsis,
                                 textAlign: Current.TextAlign);
            return new MatrixTransition(
                animation: _shakingLabelController,
                onTransform: ShakeTransform,
                child: new AnimatedOpacity(
                    opacity: ShouldShowLabel ? 1.0 : 0.0,
                    duration: InputDecoration.TransitionDuration,
                    curve: Curves.FastOutSlowIn,
                    child: new AnimatedDefaultTextStyle(
                        child: content,
                        style: LabelShouldWithdraw ? floatingLabelStyle : labelStyle,
                        duration: InputDecoration.TransitionDuration,
                        curve: Curves.FastOutSlowIn)));
        }

        private static Matrix ShakeTransform(double value)
        {
            double shakeOffset = value <= 0.25
                ? -value
                : value < 0.75 ? value - 0.5 : (1.0 - value) * 4.0;
            return Matrix.CreateTranslation(shakeOffset * 4.0, 0.0);
        }

        private Widget? BuildHint(InputDecoration decoration, TextStyle hintStyle)
        {
            if (decoration.Hint is null && decoration.HintText is null)
            {
                return null;
            }

            bool showHint = Current.IsEmpty && !HasInlineLabel;
            Widget hintWidget = Styled(
                decoration.Hint
                ?? new Text(
                    decoration.HintText!,
                    textDirection: decoration.HintTextDirection,
                    overflow: decoration.HintMaxLines is null ? null : TextOverflow.Ellipsis,
                    textAlign: Current.TextAlign,
                    maxLines: decoration.HintMaxLines),
                hintStyle);
            TimeSpan fadeDuration = decoration.HintFadeDuration ?? InputDecoration.HintFadeTransitionDuration;

            if (decoration.MaintainHintSize)
            {
                return new AnimatedOpacity(
                    opacity: showHint ? 1.0 : 0.0,
                    duration: fadeDuration,
                    curve: Curves.FastOutSlowIn,
                    child: hintWidget);
            }

            return new AnimatedSwitcher(
                duration: fadeDuration,
                child: showHint ? hintWidget : new SizedBox());
        }

        private Widget? BuildAffix(
            Widget? affix,
            string? affixText,
            TextStyle style,
            SemanticsSortKey? semanticsSortKey,
            SemanticsTag semanticsTag)
        {
            if (affix is null && affixText is null)
            {
                return null;
            }

            // Flutter's affix Semantics is not a container: its descendants merge up as fragments and
            // the decorator's delegate sees the tagging configuration itself. Plumix hands the delegate
            // the child semantics nodes instead, so the affix forms one node carrying its merged label.
            Widget content = new Semantics(
                container: true,
                sortKey: semanticsSortKey,
                tagForChildren: semanticsTag,
                child: affix ?? new Text(affixText!));
            return Styled(
                new IgnorePointer(
                    ignoring: !LabelShouldWithdraw,
                    child: new AnimatedOpacity(
                        opacity: LabelShouldWithdraw ? 1.0 : 0.0,
                        duration: InputDecoration.TransitionDuration,
                        curve: Curves.FastOutSlowIn,
                        child: content)),
                style);
        }

        private static Widget? BuildIconSlot(
            Widget? icon,
            BoxConstraints? constraints,
            Color color,
            double iconSize,
            VisualDensity visualDensity,
            SemanticsTag semanticsTag)
        {
            if (icon is null)
            {
                return null;
            }

            BoxConstraints effective = constraints ?? visualDensity.EffectiveConstraints(
                new BoxConstraints(MinWidth: 48.0, MinHeight: 48.0));
            return new Center(
                widthFactor: 1.0,
                heightFactor: 1.0,
                child: new MouseRegion(
                    cursor: SystemMouseCursors.Basic,
                    child: new ConstrainedBox(
                        effective,
                        new IconTheme(
                            new IconThemeData(Color: color, Size: iconSize),
                            new Semantics(container: true, tagForChildren: semanticsTag, child: icon)))));
        }

        private InputBorder ResolveBorder(
            ThemeData themeData,
            InputDecoratorDefaults defaults,
            MaterialState states,
            BuildContext context)
        {
            InputDecoration decoration = Decoration;
            InputBorder? border;
            if (!decoration.Enabled)
            {
                border = HasError ? decoration.ErrorBorder : decoration.DisabledBorder;
            }
            else if (Current.IsFocused)
            {
                border = HasError ? decoration.FocusedErrorBorder : decoration.FocusedBorder;
            }
            else
            {
                border = HasError ? decoration.ErrorBorder : decoration.EnabledBorder;
            }

            return border ?? GetDefaultBorder(themeData, defaults, states, context);
        }

        private InputBorder GetDefaultBorder(
            ThemeData themeData,
            InputDecoratorDefaults defaults,
            MaterialState states,
            BuildContext context)
        {
            InputDecoration decoration = Decoration;
            InputBorder declared = decoration.Border ?? new UnderlineInputBorder();
            InputBorder border = declared is IStateInputBorder stateBorder
                ? stateBorder.Resolve(states)
                : declared;

            if (declared is IStateInputBorder)
            {
                return border;
            }

            if (border.BorderSide == BorderSide.None)
            {
                return border;
            }

            if (themeData.UseMaterial3)
            {
                if (decoration.Filled ?? false)
                {
                    BorderSide? themeSide = InputDecorationTheme.Of(context).ActiveIndicatorBorder;
                    return border.CopyWith(themeSide ?? defaults.ActiveIndicatorBorder);
                }

                return border.CopyWith(defaults.OutlineBorder);
            }

            double width = (decoration.IsCollapsed ?? false)
                           || ReferenceEquals(decoration.Border, InputBorder.None)
                           || !decoration.Enabled
                ? 0.0
                : Current.IsFocused ? 2.0 : 1.0;
            return border.CopyWith(new BorderSide(GetDefaultMaterial2BorderColor(themeData), width));
        }

        private Color GetDefaultMaterial2BorderColor(ThemeData themeData)
        {
            InputDecoration decoration = Decoration;
            if (!decoration.Enabled && !Current.IsFocused)
            {
                return (decoration.Filled ?? false) && !(decoration.Border?.IsOutline ?? false)
                    ? Colors.Transparent
                    : themeData.DisabledColor;
            }

            if (HasError)
            {
                return themeData.ErrorColor;
            }

            if (Current.IsFocused)
            {
                return themeData.PrimaryColor;
            }

            if (decoration.Filled ?? false)
            {
                return themeData.HintColor;
            }

            Color enabledColor = InputDecoratorDefaults.WithOpacity(themeData.OnSurfaceColor, 0.38);
            if (IsHovering)
            {
                Color hover = InputDecoratorDefaults.WithOpacity(
                    decoration.HoverColor ?? themeData.HoverColor,
                    0.12);
                return InputDecoratorDefaults.AlphaBlend(hover, enabledColor);
            }

            return enabledColor;
        }

        private static EdgeInsetsGeometry ResolveContentPadding(
            InputDecoration decoration,
            bool useMaterial3,
            bool isOutline,
            bool isCollapsed,
            bool isDense,
            bool filled,
            TextDirection textDirection,
            bool flipHorizontal)
        {
            if (decoration.ContentPadding is { } declared)
            {
                Thickness resolved = declared.Resolve(textDirection);
                return EdgeInsetsGeometry.DirectionalOnly(
                    start: flipHorizontal ? resolved.Right : resolved.Left,
                    top: resolved.Top,
                    end: flipHorizontal ? resolved.Left : resolved.Right,
                    bottom: resolved.Bottom);
            }

            if (isCollapsed)
            {
                return EdgeInsetsGeometry.Zero;
            }

            if (!isOutline)
            {
                double horizontal = filled ? 12.0 : 0.0;
                double vertical = useMaterial3
                    ? isDense ? 4.0 : 8.0
                    : isDense ? 8.0 : 12.0;
                return EdgeInsetsGeometry.DirectionalOnly(
                    start: horizontal,
                    top: vertical,
                    end: horizontal,
                    bottom: vertical);
            }

            return useMaterial3
                ? isDense
                    ? EdgeInsetsGeometry.DirectionalOnly(start: 12.0, top: 16.0, end: 12.0, bottom: 8.0)
                    : EdgeInsetsGeometry.DirectionalOnly(start: 12.0, top: 20.0, end: 12.0, bottom: 12.0)
                : isDense
                    ? EdgeInsetsGeometry.DirectionalOnly(start: 12.0, top: 20.0, end: 12.0, bottom: 12.0)
                    : EdgeInsetsGeometry.DirectionalOnly(start: 12.0, top: 24.0, end: 12.0, bottom: 16.0);
        }

        private static Widget Styled(Widget child, TextStyle style) => new DefaultTextStyle(style, child);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/material/input_decorator.dart (_HelperError).
internal sealed class HelperError : StatefulWidget
{
    public HelperError(
        TextAlign? textAlign = null,
        Widget? helper = null,
        string? helperText = null,
        TextStyle? helperStyle = null,
        int? helperMaxLines = null,
        Widget? error = null,
        string? errorText = null,
        TextStyle? errorStyle = null,
        int? errorMaxLines = null,
        Key? key = null) : base(key)
    {
        TextAlign = textAlign;
        Helper = helper;
        HelperText = helperText;
        HelperStyle = helperStyle;
        HelperMaxLines = helperMaxLines;
        Error = error;
        ErrorText = errorText;
        ErrorStyle = errorStyle;
        ErrorMaxLines = errorMaxLines;
    }

    public TextAlign? TextAlign { get; }
    public Widget? Helper { get; }
    public string? HelperText { get; }
    public TextStyle? HelperStyle { get; }
    public int? HelperMaxLines { get; }
    public Widget? Error { get; }
    public string? ErrorText { get; }
    public TextStyle? ErrorStyle { get; }
    public int? ErrorMaxLines { get; }

    public override State CreateState() => new HelperErrorState();

    private sealed class HelperErrorState : State
    {
        private AnimationController _controller = null!;
        private Widget? _helper;
        private Widget? _error;

        private HelperError Current => (HelperError)StateWidget;

        private static readonly Widget Empty = new SizedBox(width: 0.0, height: 0.0);

        public override void InitState()
        {
            _controller = new AnimationController(InputDecoration.TransitionDuration, this);
            if (Current.Error is not null || Current.ErrorText is not null)
            {
                _error = BuildError();
                _controller.SetValue(1.0);
            }
            else if (Current.Helper is not null || Current.HelperText is not null)
            {
                _helper = BuildHelper();
            }

            _controller.AddListener(HandleChange);
        }

        public override void Dispose()
        {
            _controller.RemoveListener(HandleChange);
            _controller.Dispose();
        }

        private void HandleChange() => SetState(() => { });

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (HelperError)oldWidget;
            bool errorStateChanged = (Current.Error is not null) != (old.Error is not null);
            bool errorTextStateChanged = (Current.ErrorText is not null) != (old.ErrorText is not null);
            bool helperStateChanged = (Current.Helper is not null) != (old.Helper is not null);
            bool helperTextStateChanged = Current.ErrorText is null
                                          && (Current.HelperText is not null) != (old.HelperText is not null);

            if (errorStateChanged || errorTextStateChanged || helperStateChanged || helperTextStateChanged)
            {
                if (Current.Error is not null || Current.ErrorText is not null)
                {
                    _error = BuildError();
                    _controller.Forward();
                }
                else if (Current.Helper is not null || Current.HelperText is not null)
                {
                    _helper = BuildHelper();
                    _controller.Reverse();
                }
                else
                {
                    _controller.Reverse();
                }
            }
        }

        private Widget BuildHelper() => new Opacity(
            1.0,
            Styled(
                Current.Helper ?? new Text(
                    Current.HelperText ?? string.Empty,
                    textAlign: Current.TextAlign,
                    overflow: TextOverflow.Ellipsis,
                    maxLines: Current.HelperMaxLines),
                Current.HelperStyle));

        private Widget BuildError() => new Opacity(
            1.0,
            Styled(
                Current.Error ?? new Text(
                    Current.ErrorText ?? string.Empty,
                    textAlign: Current.TextAlign,
                    overflow: TextOverflow.Ellipsis,
                    maxLines: Current.ErrorMaxLines),
                Current.ErrorStyle));

        public override Widget Build(BuildContext context)
        {
            if (_controller.Status == AnimationStatus.Dismissed)
            {
                _error = null;
                if (Current.Helper is not null || Current.HelperText is not null)
                {
                    return _helper = BuildHelper();
                }

                _helper = null;
                return Empty;
            }

            if (_controller.Status == AnimationStatus.Completed)
            {
                _helper = null;
                if (Current.Error is not null || Current.ErrorText is not null)
                {
                    return _error = BuildError();
                }

                _error = null;
                return Empty;
            }

            if (_helper is null && (Current.Error is not null || Current.ErrorText is not null))
            {
                return new Opacity(Math.Clamp(_controller.Value, 0.0, 1.0), BuildError());
            }

            if (_error is null && (Current.Helper is not null || Current.HelperText is not null))
            {
                return new Opacity(Math.Clamp(1.0 - _controller.Value, 0.0, 1.0), BuildHelper());
            }

            if (Current.Error is not null || Current.ErrorText is not null)
            {
                return new Stack(
                    children:
                    [
                        new Opacity(Math.Clamp(1.0 - _controller.Value, 0.0, 1.0), _helper ?? Empty),
                        new Opacity(Math.Clamp(_controller.Value, 0.0, 1.0), BuildError()),
                    ]);
            }

            return _helper ?? Empty;
        }

        private static Widget Styled(Widget child, TextStyle? style) =>
            style is null ? child : new DefaultTextStyle(style, child);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/material/input_decorator.dart (_BorderContainer).
internal sealed class BorderContainer : StatefulWidget
{
    public BorderContainer(
        InputBorder border,
        InputBorderGap gap,
        Animation<double> gapAnimation,
        Color fillColor,
        Color hoverColor,
        bool isHovering,
        TextDirection textDirection,
        Key? key = null) : base(key)
    {
        Border = border;
        Gap = gap;
        GapAnimation = gapAnimation;
        FillColor = fillColor;
        HoverColor = hoverColor;
        IsHovering = isHovering;
        TextDirection = textDirection;
    }

    public InputBorder Border { get; }
    public InputBorderGap Gap { get; }
    public Animation<double> GapAnimation { get; }
    public Color FillColor { get; }
    public Color HoverColor { get; }
    public bool IsHovering { get; }
    public TextDirection TextDirection { get; }

    public override State CreateState() => new BorderContainerState();

    private sealed class BorderContainerState : State
    {
        private static readonly TimeSpan HoverDuration = TimeSpan.FromMilliseconds(15);

        private AnimationController _controller = null!;
        private CurvedAnimation _borderAnimation = null!;
        private AnimationController _hoverColorController = null!;
        private CurvedAnimation _hoverAnimation = null!;
        private InputBorder _begin = null!;
        private InputBorder _end = null!;

        private BorderContainer Current => (BorderContainer)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(InputDecoration.TransitionDuration, this);
            _controller.AddListener(HandleChange);
            _borderAnimation = new CurvedAnimation(
                _controller,
                Curves.FastOutSlowIn,
                Curves.Flipped(Curves.FastOutSlowIn));
            _hoverColorController = new AnimationController(HoverDuration, this);
            _hoverColorController.SetValue(Current.IsHovering ? 1.0 : 0.0);
            _hoverColorController.AddListener(HandleChange);
            _hoverAnimation = new CurvedAnimation(_hoverColorController, Curves.Linear);
            _begin = Current.Border;
            _end = Current.Border;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (BorderContainer)oldWidget;
            if (!Equals(Current.Border, old.Border))
            {
                _begin = old.Border;
                _end = Current.Border;
                _controller.Forward(from: 0.0);
            }

            if (Current.IsHovering != old.IsHovering)
            {
                if (Current.IsHovering)
                {
                    _hoverColorController.Forward();
                }
                else
                {
                    _hoverColorController.Reverse();
                }
            }
        }

        public override void Dispose()
        {
            _controller.RemoveListener(HandleChange);
            _hoverColorController.RemoveListener(HandleChange);
            _controller.Dispose();
            _borderAnimation.Dispose();
            _hoverColorController.Dispose();
            _hoverAnimation.Dispose();
        }

        private void HandleChange() => SetState(() => { });

        public override Widget Build(BuildContext context) => new CustomPaint(
            painter: new InputBorderPainter(
                // Flutter's _InputBorderTween is `ShapeBorder.lerp(begin, end, t)! as InputBorder`.
                border: (InputBorder)ShapeBorder.Lerp(_begin, _end, _borderAnimation.Value)!,
                gap: Current.Gap,
                gapPercentage: Current.GapAnimation.Value,
                fillColor: Current.FillColor,
                hoverColor: Current.HoverColor,
                hoverProgress: _hoverAnimation.Value,
                textDirection: Current.TextDirection));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/material/input_decorator.dart (_InputBorderPainter).
internal sealed class InputBorderPainter : CustomPainter
{
    public InputBorderPainter(
        InputBorder border,
        InputBorderGap gap,
        double gapPercentage,
        Color fillColor,
        Color hoverColor,
        double hoverProgress,
        TextDirection textDirection) : base(Listenable.Merge(gap))
    {
        Border = border;
        Gap = gap;
        GapPercentage = gapPercentage;
        FillColor = fillColor;
        HoverColor = hoverColor;
        HoverProgress = hoverProgress;
        TextDirection = textDirection;
    }

    internal InputBorder Border { get; }
    internal InputBorderGap Gap { get; }
    internal double? GapStart => Gap.Start;
    internal double GapExtent => Gap.Extent;
    internal double GapPercentage { get; }
    internal Color FillColor { get; }
    internal Color HoverColor { get; }
    internal double HoverProgress { get; }
    internal TextDirection TextDirection { get; }

    internal Color BlendedColor => InputDecoratorDefaults.AlphaBlend(
        Color.FromArgb(
            (byte)Math.Clamp(Math.Round(HoverColor.A * HoverProgress), 0, 255),
            HoverColor.R,
            HoverColor.G,
            HoverColor.B),
        FillColor);

    public override void Paint(PaintingContext context, Size size)
    {
        var canvasRect = new Rect(size);
        Color blended = BlendedColor;
        if (blended.A > 0)
        {
            var brush = new SolidColorBrush(blended);
            if (Border.PreferPaintInterior)
            {
                Border.PaintInterior(context, canvasRect, brush, TextDirection);
            }
            else
            {
                context.DrawGeometry(brush, null, Border.GetOuterPath(canvasRect, TextDirection).ToGeometry());
            }
        }

        Border.Paint(
            context,
            canvasRect,
            gapStart: GapStart,
            gapExtent: GapExtent,
            gapPercentage: GapPercentage,
            textDirection: TextDirection);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => oldDelegate is not InputBorderPainter old
                                                                     || !Equals(old.Border, Border)
                                                                     || !ReferenceEquals(old.Gap, Gap)
                                                                     || !old.GapPercentage.Equals(GapPercentage)
                                                                     || old.FillColor != FillColor
                                                                     || old.HoverColor != HoverColor
                                                                     || !old.HoverProgress.Equals(HoverProgress)
                                                                     || old.TextDirection != TextDirection;
}

// Dart parity source: flutter/packages/flutter/lib/src/material/input_decorator.dart
// (_InputDecoratorDefaultsM2 / _InputDecoratorDefaultsM3).
internal sealed record InputDecoratorDefaults(
    TextStyle HintStyle,
    TextStyle LabelStyle,
    TextStyle FloatingLabelStyle,
    TextStyle HelperStyle,
    TextStyle ErrorStyle,
    Color FillColor,
    Color IconColor,
    Color PrefixIconColor,
    Color SuffixIconColor,
    BorderSide ActiveIndicatorBorder,
    BorderSide OutlineBorder)
{
    public static InputDecoratorDefaults Resolve(ThemeData theme, MaterialState states, bool hasError)
    {
        return theme.UseMaterial3 ? ResolveMaterial3(theme, states) : ResolveMaterial2(theme, states, hasError);
    }

    private static InputDecoratorDefaults ResolveMaterial3(ThemeData theme, MaterialState states)
    {
        bool disabled = states.HasFlag(MaterialState.Disabled);
        bool error = states.HasFlag(MaterialState.Error);
        bool focused = states.HasFlag(MaterialState.Focused);
        bool hovered = states.HasFlag(MaterialState.Hovered);

        Color LabelColor()
        {
            if (disabled) return WithOpacity(theme.OnSurfaceColor, 0.38);
            if (error && focused) return theme.ErrorColor;
            if (error && hovered) return theme.ColorScheme.OnErrorContainer;
            if (error) return theme.ErrorColor;
            if (focused) return theme.PrimaryColor;
            return theme.OnSurfaceVariantColor;
        }

        BorderSide IndicatorSide(Color enabledColor)
        {
            if (disabled) return new BorderSide(WithOpacity(theme.OnSurfaceColor, 0.38));
            if (error && focused) return new BorderSide(theme.ErrorColor, 2.0);
            if (error && hovered) return new BorderSide(theme.ColorScheme.OnErrorContainer);
            if (error) return new BorderSide(theme.ErrorColor);
            if (focused) return new BorderSide(theme.PrimaryColor, 2.0);
            if (hovered) return new BorderSide(theme.OnSurfaceColor);
            return new BorderSide(enabledColor);
        }

        BorderSide OutlineSide()
        {
            if (disabled) return new BorderSide(WithOpacity(theme.OnSurfaceColor, 0.12));
            return IndicatorSide(theme.OutlineColor);
        }

        TextStyle labelStyle = theme.TextTheme.BodyLarge.CopyWith(color: LabelColor());
        return new InputDecoratorDefaults(
            HintStyle: new TextStyle(
                Color: disabled ? WithOpacity(theme.OnSurfaceColor, 0.38) : theme.OnSurfaceVariantColor),
            LabelStyle: labelStyle,
            FloatingLabelStyle: labelStyle,
            HelperStyle: theme.TextTheme.BodySmall.CopyWith(
                color: disabled ? WithOpacity(theme.OnSurfaceColor, 0.38) : theme.OnSurfaceVariantColor),
            ErrorStyle: theme.TextTheme.BodySmall.CopyWith(color: theme.ErrorColor),
            FillColor: disabled
                ? WithOpacity(theme.OnSurfaceColor, 0.04)
                : theme.SurfaceContainerHighestColor,
            IconColor: theme.OnSurfaceVariantColor,
            PrefixIconColor: disabled
                ? WithOpacity(theme.OnSurfaceColor, 0.38)
                : theme.OnSurfaceVariantColor,
            SuffixIconColor: disabled ? WithOpacity(theme.OnSurfaceColor, 0.38)
                : error && hovered ? theme.ColorScheme.OnErrorContainer
                : error ? theme.ErrorColor
                : theme.OnSurfaceVariantColor,
            ActiveIndicatorBorder: IndicatorSide(theme.OnSurfaceVariantColor),
            OutlineBorder: OutlineSide());
    }

    private static InputDecoratorDefaults ResolveMaterial2(
        ThemeData theme,
        MaterialState states,
        bool hasError)
    {
        bool disabled = states.HasFlag(MaterialState.Disabled);
        bool focused = states.HasFlag(MaterialState.Focused);
        bool dark = theme.Brightness == Brightness.Dark;

        Color IconColorValue()
        {
            if (disabled && !focused) return theme.DisabledColor;
            if (focused) return theme.PrimaryColor;
            return dark ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x73, 0x00, 0x00, 0x00);
        }

        var hintStyle = new TextStyle(Color: disabled ? theme.DisabledColor : theme.HintColor);
        return new InputDecoratorDefaults(
            HintStyle: hintStyle,
            LabelStyle: hintStyle,
            FloatingLabelStyle: new TextStyle(
                Color: disabled ? theme.DisabledColor
                    : hasError ? theme.ErrorColor
                    : focused ? theme.PrimaryColor
                    : theme.HintColor),
            HelperStyle: theme.TextTheme.BodySmall.CopyWith(
                color: disabled ? Colors.Transparent : theme.HintColor),
            ErrorStyle: theme.TextTheme.BodySmall.CopyWith(
                color: disabled ? Colors.Transparent : theme.ErrorColor),
            FillColor: dark
                ? disabled ? Color.FromArgb(0x0D, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)
                : disabled ? Color.FromArgb(0x05, 0x00, 0x00, 0x00) : Color.FromArgb(0x0A, 0x00, 0x00, 0x00),
            IconColor: IconColorValue(),
            PrefixIconColor: IconColorValue(),
            SuffixIconColor: disabled && !focused ? theme.DisabledColor
                : hasError ? theme.ErrorColor
                : focused ? theme.PrimaryColor
                : dark ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x73, 0x00, 0x00, 0x00),
            ActiveIndicatorBorder: BorderSide.None,
            OutlineBorder: BorderSide.None);
    }

    internal static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * Math.Clamp(opacity, 0.0, 1.0)),
        color.R,
        color.G,
        color.B);

    internal static Color AlphaBlend(Color foreground, Color background)
    {
        double alpha = foreground.A / 255.0;
        if (alpha == 0.0)
        {
            return background;
        }

        if (alpha == 1.0)
        {
            return foreground;
        }

        double invAlpha = 1.0 - alpha;
        double backAlpha = background.A / 255.0;
        double outAlpha = alpha + (backAlpha * invAlpha);
        if (outAlpha == 0.0)
        {
            return Colors.Transparent;
        }

        return Color.FromArgb(
            (byte)Math.Round(outAlpha * 255.0),
            (byte)Math.Round((((foreground.R * alpha) + (background.R * backAlpha * invAlpha)) / outAlpha)),
            (byte)Math.Round((((foreground.G * alpha) + (background.G * backAlpha * invAlpha)) / outAlpha)),
            (byte)Math.Round((((foreground.B * alpha) + (background.B * backAlpha * invAlpha)) / outAlpha)));
    }
}
