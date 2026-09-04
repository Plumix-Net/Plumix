using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/input_decorator.dart
// (_DecorationSlot, _Decorator, _Decoration, _InputBorderGap, _RenderDecoration).

internal enum DecorationSlot
{
    Icon,
    Input,
    Label,
    Hint,
    Prefix,
    Suffix,
    PrefixIcon,
    SuffixIcon,
    HelperError,
    Counter,
    Container,
}

/// Mutable gap geometry shared between the render object (writer) and the border painter (reader).
internal sealed class InputBorderGap : ChangeNotifier
{
    private double? _start;
    private double _extent;

    public double? Start
    {
        get => _start;
        set
        {
            if (_start == value)
            {
                return;
            }

            _start = value;
            NotifyListeners();
        }
    }

    public double Extent
    {
        get => _extent;
        set
        {
            if (_extent.Equals(value))
            {
                return;
            }

            _extent = value;
            NotifyListeners();
        }
    }

    public override bool Equals(object? obj) =>
        obj is InputBorderGap other && other._start == _start && other._extent.Equals(_extent);

    public override int GetHashCode() => HashCode.Combine(_start, _extent);
}

internal sealed record DecorationSpec(
    EdgeInsetsGeometry ContentPadding,
    bool IsCollapsed,
    double FloatingLabelHeight,
    double FloatingLabelProgress,
    FloatingLabelAlignment FloatingLabelAlignment,
    InputBorder Border,
    InputBorderGap BorderGap,
    bool AlignLabelWithHint,
    bool IsDense,
    bool IsEmpty,
    VisualDensity VisualDensity,
    double InputGap,
    bool MaintainHintSize,
    bool MaintainLabelSize,
    Widget? Icon = null,
    Widget? Input = null,
    Widget? Label = null,
    Widget? Hint = null,
    Widget? Prefix = null,
    Widget? Suffix = null,
    Widget? PrefixIcon = null,
    Widget? SuffixIcon = null,
    Widget? HelperError = null,
    Widget? Counter = null,
    Widget? Container = null);

internal sealed class DecoratorRenderWidget : SlottedMultiChildRenderObjectWidget<DecorationSlot>
{
    private static readonly IReadOnlyList<DecorationSlot> AllSlots = Enum.GetValues<DecorationSlot>();

    public DecoratorRenderWidget(
        DecorationSpec decoration,
        TextDirection textDirection,
        TextBaseline textBaseline,
        TextAlignVertical? textAlignVertical,
        bool isFocused,
        bool expands,
        bool material3,
        Key? key = null) : base(key)
    {
        Decoration = decoration;
        TextDirection = textDirection;
        TextBaseline = textBaseline;
        TextAlignVertical = textAlignVertical;
        IsFocused = isFocused;
        Expands = expands;
        Material3 = material3;
    }

    public DecorationSpec Decoration { get; }
    public TextDirection TextDirection { get; }
    public TextBaseline TextBaseline { get; }
    public TextAlignVertical? TextAlignVertical { get; }
    public bool IsFocused { get; }
    public bool Expands { get; }
    public bool Material3 { get; }

    public override IReadOnlyList<DecorationSlot> Slots => AllSlots;

    public override Widget? ChildForSlot(DecorationSlot slot) => slot switch
    {
        DecorationSlot.Icon => Decoration.Icon,
        DecorationSlot.Input => Decoration.Input,
        DecorationSlot.Label => Decoration.Label,
        DecorationSlot.Hint => Decoration.Hint,
        DecorationSlot.Prefix => Decoration.Prefix,
        DecorationSlot.Suffix => Decoration.Suffix,
        DecorationSlot.PrefixIcon => Decoration.PrefixIcon,
        DecorationSlot.SuffixIcon => Decoration.SuffixIcon,
        DecorationSlot.HelperError => Decoration.HelperError,
        DecorationSlot.Counter => Decoration.Counter,
        DecorationSlot.Container => Decoration.Container,
        _ => null,
    };

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderDecoration(
        decoration: Decoration,
        textDirection: TextDirection,
        textBaseline: TextBaseline,
        textAlignVertical: TextAlignVertical,
        isFocused: IsFocused,
        expands: Expands,
        material3: Material3);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var decorator = (RenderDecoration)renderObject;
        decorator.Decoration = Decoration;
        decorator.Expands = Expands;
        decorator.IsFocused = IsFocused;
        decorator.TextAlignVertical = TextAlignVertical;
        decorator.TextBaseline = TextBaseline;
        decorator.TextDirection = TextDirection;
    }
}

internal sealed class RenderDecoration : RenderBox, ISlottedRenderObjectContainer
{
    private const double FinalLabelScale = 0.75;
    private const double SubtextCounterPadding = 16.0;
    private const double MinInteractiveDimension = 48.0;

    private RenderBox? _icon;
    private RenderBox? _input;
    private RenderBox? _label;
    private RenderBox? _hint;
    private RenderBox? _prefix;
    private RenderBox? _suffix;
    private RenderBox? _prefixIcon;
    private RenderBox? _suffixIcon;
    private RenderBox? _helperError;
    private RenderBox? _counter;
    private RenderBox? _container;

    private DecorationSpec _decoration;
    private TextDirection _textDirection;
    private TextBaseline _textBaseline;
    private TextAlignVertical? _textAlignVertical;
    private bool _isFocused;
    private bool _expands;
    private readonly bool _material3;
    private Matrix4? _labelTransform;

    public RenderDecoration(
        DecorationSpec decoration,
        TextDirection textDirection,
        TextBaseline textBaseline,
        TextAlignVertical? textAlignVertical,
        bool isFocused,
        bool expands,
        bool material3)
    {
        _decoration = decoration;
        _textDirection = textDirection;
        _textBaseline = textBaseline;
        _textAlignVertical = textAlignVertical;
        _isFocused = isFocused;
        _expands = expands;
        _material3 = material3;
    }

    public DecorationSpec Decoration
    {
        get => _decoration;
        set
        {
            if (_decoration == value)
            {
                return;
            }

            _decoration = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set => SetLayoutValue(ref _textDirection, value);
    }

    public TextBaseline TextBaseline
    {
        get => _textBaseline;
        set => SetLayoutValue(ref _textBaseline, value);
    }

    public TextAlignVertical? TextAlignVertical
    {
        get => _textAlignVertical ?? DefaultTextAlignVertical;
        set
        {
            if (_textAlignVertical == value)
            {
                return;
            }

            // A different instance with the same effective y needs no relayout.
            if (TextAlignVertical!.Value.Y == (value?.Y ?? DefaultTextAlignVertical.Y))
            {
                _textAlignVertical = value;
                return;
            }

            _textAlignVertical = value;
            MarkNeedsLayout();
        }
    }

    public bool IsFocused
    {
        get => _isFocused;
        set
        {
            if (_isFocused == value)
            {
                return;
            }

            _isFocused = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool Expands
    {
        get => _expands;
        set => SetLayoutValue(ref _expands, value);
    }

    internal RenderBox? LabelBox => _label;

    internal RenderBox? ContainerBox => _container;

    internal RenderBox? InputBox => _input;

    internal RenderBox? HintBox => _hint;

    internal RenderBox? PrefixIconBox => _prefixIcon;

    internal RenderBox? SuffixIconBox => _suffixIcon;

    internal RenderBox? HelperErrorBox => _helperError;

    internal RenderBox? CounterBox => _counter;

    internal Matrix4? LabelTransform => _labelTransform;

    private TextAlignVertical DefaultTextAlignVertical =>
        IsOutlineAligned ? Plumix.Rendering.TextAlignVertical.Center : Plumix.Rendering.TextAlignVertical.Top;

    private bool IsOutlineAligned => !_decoration.IsCollapsed && _decoration.Border.IsOutline;

    private Vector DensityOffset => _decoration.VisualDensity.BaseSizeAdjustment;

    private EdgeInsetsGeometry ContentPadding => _decoration.ContentPadding;

    private double SubtextGap => _material3 ? 4.0 : 8.0;

    private double PrefixToInputGap => _material3 ? 4.0 : 0.0;

    private double InputToSuffixGap => _material3 ? 4.0 : 0.0;

    private bool IsRtl => _textDirection == Plumix.UI.TextDirection.Rtl;

    private double PaddingStart => IsRtl ? ContentPadding.Right + ContentPadding.End
        : ContentPadding.Left + ContentPadding.Start;

    private double PaddingEnd => IsRtl ? ContentPadding.Left + ContentPadding.Start
        : ContentPadding.Right + ContentPadding.End;

    private double PaddingTop => ContentPadding.Top;

    private double PaddingBottom => ContentPadding.Bottom;

    private double PaddingHorizontal => PaddingStart + PaddingEnd;

    private double PaddingVertical => PaddingTop + PaddingBottom;

    public void SetChild(RenderObject? child, object slot)
    {
        RenderBox? box = child switch
        {
            null => null,
            RenderBox renderBox => renderBox,
            _ => throw new InvalidOperationException("InputDecorator slots require RenderBox children."),
        };

        switch ((DecorationSlot)slot)
        {
            case DecorationSlot.Icon: SetSlotChild(ref _icon, box); break;
            case DecorationSlot.Input: SetSlotChild(ref _input, box); break;
            case DecorationSlot.Label: SetSlotChild(ref _label, box); break;
            case DecorationSlot.Hint: SetSlotChild(ref _hint, box); break;
            case DecorationSlot.Prefix: SetSlotChild(ref _prefix, box); break;
            case DecorationSlot.Suffix: SetSlotChild(ref _suffix, box); break;
            case DecorationSlot.PrefixIcon: SetSlotChild(ref _prefixIcon, box); break;
            case DecorationSlot.SuffixIcon: SetSlotChild(ref _suffixIcon, box); break;
            case DecorationSlot.HelperError: SetSlotChild(ref _helperError, box); break;
            case DecorationSlot.Counter: SetSlotChild(ref _counter, box); break;
            case DecorationSlot.Container: SetSlotChild(ref _container, box); break;
        }
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        foreach (RenderBox child in Children())
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        void Visit(RenderBox? child)
        {
            if (child is not null)
            {
                visitor(child);
            }
        }

        Visit(_icon);
        Visit(_prefix);
        Visit(_prefixIcon);
        Visit(_label);

        // The hint is not visible when the label is not floating, so it is not exposed then either.
        if (IsFocused || _label is null)
        {
            Visit(_hint);
        }

        Visit(_input);
        Visit(_suffixIcon);
        Visit(_suffix);
        Visit(_container);
        Visit(_helperError);
        Visit(_counter);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.ChildConfigurationsDelegate = ChildSemanticsConfigurationDelegate;
    }

    /// Groups each tagged affix into its own sibling semantics node and merges everything else up,
    /// so a prefix, the input and a suffix are three siblings rather than one concatenated label.
    private static ChildSemanticsConfigurationsResult ChildSemanticsConfigurationDelegate(
        List<SemanticsConfiguration> childConfigs)
    {
        var builder = new ChildSemanticsConfigurationsResultBuilder();
        var mergeGroups = new Dictionary<SemanticsTag, List<SemanticsConfiguration>>();
        foreach (SemanticsConfiguration childConfig in childConfigs)
        {
            SemanticsTag? tag = Array.Find(
                InputDecorator.AffixSemanticsTags,
                candidate => childConfig.TagsChildrenWith(candidate));
            if (tag is null)
            {
                builder.MarkAsMergeUp(childConfig);
                continue;
            }

            if (!mergeGroups.TryGetValue(tag, out List<SemanticsConfiguration>? group))
            {
                group = [];
                mergeGroups[tag] = group;
            }

            group.Add(childConfig);
        }

        foreach (SemanticsTag tag in InputDecorator.AffixSemanticsTags)
        {
            if (mergeGroups.TryGetValue(tag, out List<SemanticsConfiguration>? group))
            {
                builder.MarkAsSiblingMergeGroup(group);
            }
        }

        return builder.Build();
    }

    private IEnumerable<RenderBox> Children()
    {
        if (_icon is not null) yield return _icon;
        if (_input is not null) yield return _input;
        if (_prefixIcon is not null) yield return _prefixIcon;
        if (_suffixIcon is not null) yield return _suffixIcon;
        if (_prefix is not null) yield return _prefix;
        if (_suffix is not null) yield return _suffix;
        if (_label is not null) yield return _label;
        if (_hint is not null) yield return _hint;
        if (_helperError is not null) yield return _helperError;
        if (_counter is not null) yield return _counter;
        if (_container is not null) yield return _container;
    }

    // ---- intrinsics -------------------------------------------------------------------------

    private static double MinWidth(RenderBox? box, double height) => box?.GetMinIntrinsicWidth(height) ?? 0.0;

    private static double MaxWidth(RenderBox? box, double height) => box?.GetMaxIntrinsicWidth(height) ?? 0.0;

    private static double MinHeight(RenderBox? box, double width) => box?.GetMinIntrinsicHeight(width) ?? 0.0;

    private static Size BoxSize(RenderBox? box) => box?.Size ?? default;

    private static BoxParentData ParentDataOf(RenderBox child) => (BoxParentData)child.parentData!;

    protected override double ComputeMinIntrinsicWidth(double height) =>
        ComputeIntrinsicWidth(height, MinWidth);

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        ComputeIntrinsicWidth(height, MaxWidth);

    private double ComputeIntrinsicWidth(double height, Func<RenderBox?, double, double> measure)
    {
        double inputWidth = _decoration.IsEmpty || _decoration.MaintainHintSize
            ? Math.Max(measure(_input, height), measure(_hint, height))
            : measure(_input, height);
        double contentWidth = _decoration.MaintainLabelSize
            ? Math.Max(inputWidth, measure(_label, height))
            : inputWidth;
        return measure(_icon, height)
               + (_prefixIcon is not null ? PrefixToInputGap : PaddingStart + _decoration.InputGap)
               + measure(_prefixIcon, height)
               + measure(_prefix, height)
               + contentWidth
               + measure(_suffix, height)
               + measure(_suffixIcon, height)
               + (_suffixIcon is not null ? InputToSuffixGap : PaddingEnd + _decoration.InputGap);
    }

    private static double LineHeight(double width, IEnumerable<RenderBox?> boxes)
    {
        double height = 0.0;
        foreach (RenderBox? box in boxes)
        {
            if (box is not null)
            {
                height = Math.Max(height, MinHeight(box, width));
            }
        }

        return height;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        double iconHeight = MinHeight(_icon, width);
        double iconWidth = MinWidth(_icon, iconHeight);
        width = Math.Max(width - iconWidth, 0.0);

        double prefixIconHeight = MinHeight(_prefixIcon, width);
        double prefixIconWidth = MinWidth(_prefixIcon, prefixIconHeight);
        double suffixIconHeight = MinHeight(_suffixIcon, width);
        double suffixIconWidth = MinWidth(_suffixIcon, suffixIconHeight);
        width = Math.Max(width - PaddingHorizontal - (_decoration.InputGap * 2.0), 0.0);

        double counterHeight = MinHeight(_counter, width);
        double counterWidth = MinWidth(_counter, counterHeight);
        double counterPadding = _counter is not null ? SubtextCounterPadding : 0.0;
        double helperErrorAvailableWidth = Math.Max(width - counterWidth - counterPadding, 0.0);
        double helperErrorHeight = MinHeight(_helperError, helperErrorAvailableWidth);
        double subtextHeight = Math.Max(counterHeight, helperErrorHeight);
        if (subtextHeight > 0.0)
        {
            subtextHeight += SubtextGap;
        }

        double prefixHeight = MinHeight(_prefix, width);
        double prefixWidth = MinWidth(_prefix, prefixHeight);
        double suffixHeight = MinHeight(_suffix, width);
        double suffixWidth = MinWidth(_suffix, suffixHeight);
        double availableInputWidth = Math.Max(
            width - prefixWidth - suffixWidth - prefixIconWidth - suffixIconWidth,
            0.0);
        double inputHeight = LineHeight(
            availableInputWidth,
            _decoration.IsEmpty ? [_input, _hint] : [_input]);
        double inputMaxHeight = Math.Max(inputHeight, Math.Max(prefixHeight, suffixHeight));

        double contentHeight = PaddingTop
                               + (_label is null ? 0.0 : _decoration.FloatingLabelHeight)
                               + inputMaxHeight
                               + PaddingBottom
                               + DensityOffset.Y;
        double containerHeight = Math.Max(
            Math.Max(iconHeight, contentHeight),
            Math.Max(prefixIconHeight, suffixIconHeight));
        double minContainerHeight = _decoration.IsDense || _expands ? 0.0 : MinInteractiveDimension;
        return Math.Max(containerHeight, minContainerHeight) + subtextHeight;
    }

    protected override double ComputeMaxIntrinsicHeight(double width) => GetMinIntrinsicHeight(width);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        if (_input is null)
        {
            return 0.0;
        }

        return ParentDataOf(_input).offset.Y
               + (_input.GetDistanceToBaseline(baseline, onlyReal: true) ?? _input.Size.Height);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        constraints.Constrain(Layout(constraints, DryLayoutChild, DryBaseline).Size);

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (_input is null)
        {
            return 0.0;
        }

        DecorationLayout layout = Layout(constraints, DryLayoutChild, DryBaseline);
        double delta = baseline == Plumix.UI.TextBaseline.Alphabetic
            ? 0.0
            : (_input.GetDryBaseline(layout.InputConstraints, baseline)
               ?? _input.GetDryLayout(layout.InputConstraints).Height)
              - (_input.GetDryBaseline(layout.InputConstraints, Plumix.UI.TextBaseline.Alphabetic)
                 ?? _input.GetDryLayout(layout.InputConstraints).Height);
        return delta + layout.Baseline;
    }

    // ---- layout -----------------------------------------------------------------------------

    private static Size LayoutChild(RenderBox box, BoxConstraints constraints)
    {
        box.Layout(constraints, parentUsesSize: true);
        return box.Size;
    }

    private static Size DryLayoutChild(RenderBox box, BoxConstraints constraints) =>
        box.GetDryLayout(constraints);

    private double RealBaseline(RenderBox box, BoxConstraints constraints) =>
        box.GetDistanceToBaseline(Plumix.UI.TextBaseline.Alphabetic, onlyReal: true) ?? box.Size.Height;

    private double DryBaseline(RenderBox box, BoxConstraints constraints) =>
        box.GetDryBaseline(constraints, Plumix.UI.TextBaseline.Alphabetic)
        ?? box.GetDryLayout(constraints).Height;

    private readonly record struct SubtextSize(double Ascent, double BottomHeight, double SubtextHeight);

    private readonly record struct DecorationLayout(
        BoxConstraints InputConstraints,
        double Baseline,
        double ContainerHeight,
        SubtextSize? Subtext,
        Size Size);

    private SubtextSize? ComputeSubtextSizes(
        BoxConstraints constraints,
        Func<RenderBox, BoxConstraints, Size> layoutChild,
        Func<RenderBox, BoxConstraints, double> getBaseline)
    {
        Size counterSize = default;
        double counterAscent = 0.0;
        if (_counter is not null)
        {
            counterSize = layoutChild(_counter, constraints);
            counterAscent = getBaseline(_counter, constraints);
        }

        double counterPadding = _counter is not null ? SubtextCounterPadding : 0.0;
        BoxConstraints helperErrorConstraints = constraints.Deflate(
            new Thickness(counterSize.Width + counterPadding, 0, 0, 0));
        double helperErrorHeight = _helperError is null
            ? 0.0
            : layoutChild(_helperError, helperErrorConstraints).Height;

        if (helperErrorHeight == 0.0 && counterSize.Height == 0.0)
        {
            return null;
        }

        double helperErrorBaseline = _helperError is null
            ? 0.0
            : getBaseline(_helperError, helperErrorConstraints);
        return new SubtextSize(
            Ascent: Math.Max(counterAscent, helperErrorBaseline) + SubtextGap,
            BottomHeight: Math.Max(counterAscent, helperErrorHeight) + SubtextGap,
            SubtextHeight: Math.Max(counterSize.Height, helperErrorHeight) + SubtextGap);
    }

    private DecorationLayout Layout(
        BoxConstraints constraints,
        Func<RenderBox, BoxConstraints, Size> layoutChild,
        Func<RenderBox, BoxConstraints, double> getBaseline)
    {
        if (double.IsInfinity(constraints.MaxWidth))
        {
            throw new InvalidOperationException("An InputDecorator cannot have an unbounded width.");
        }

        BoxConstraints boxConstraints = constraints.Loosen();

        double iconWidth = _icon is null ? 0.0 : layoutChild(_icon, boxConstraints).Width;
        BoxConstraints containerConstraints = boxConstraints.Deflate(new Thickness(iconWidth, 0, 0, 0));
        BoxConstraints contentConstraints = containerConstraints.Deflate(new Thickness(
            PaddingStart + _decoration.InputGap,
            0,
            PaddingEnd + _decoration.InputGap,
            0));

        SubtextSize? subtextSize = ComputeSubtextSizes(contentConstraints, layoutChild, getBaseline);

        Size prefixIconSize = _prefixIcon is null ? default : layoutChild(_prefixIcon, containerConstraints);
        Size suffixIconSize = _suffixIcon is null ? default : layoutChild(_suffixIcon, containerConstraints);
        Size prefixSize = _prefix is null ? default : layoutChild(_prefix, contentConstraints);
        Size suffixSize = _suffix is null ? default : layoutChild(_suffix, contentConstraints);

        double accessoryStart = iconWidth + prefixSize.Width
                                + (_prefixIcon is null
                                    ? PaddingStart + _decoration.InputGap
                                    : prefixIconSize.Width + PrefixToInputGap);
        double accessoryEnd = suffixSize.Width
                              + (_suffixIcon is null
                                  ? PaddingEnd + _decoration.InputGap
                                  : suffixIconSize.Width + InputToSuffixGap);
        double inputWidth = Math.Max(0.0, constraints.MaxWidth - (accessoryStart + accessoryEnd));

        double topHeight = 0.0;
        if (_label is not null)
        {
            double suffixIconSpace = _decoration.Border.IsOutline
                ? Lerp(suffixIconSize.Width, PaddingEnd, _decoration.FloatingLabelProgress)
                : suffixIconSize.Width;
            double labelWidth = Math.Max(
                0.0,
                constraints.MaxWidth - ((_decoration.InputGap * 2.0)
                                        + iconWidth
                                        + (_prefixIcon is null ? PaddingStart : prefixIconSize.Width)
                                        + (_suffixIcon is null ? PaddingEnd : suffixIconSpace)));
            double invertedLabelScale = Lerp(1.0, 1.0 / FinalLabelScale, _decoration.FloatingLabelProgress);
            BoxConstraints labelConstraints = boxConstraints with
            {
                MaxWidth = Math.Min(labelWidth * invertedLabelScale, boxConstraints.MaxWidth),
            };
            layoutChild(_label, labelConstraints);
            double labelHeight = _decoration.FloatingLabelHeight;
            topHeight = _decoration.Border.IsOutline
                ? Math.Max(labelHeight - getBaseline(_label, labelConstraints), 0.0)
                : labelHeight;
        }

        double bottomHeight = subtextSize?.BottomHeight ?? 0.0;
        BoxConstraints inputConstraints = boxConstraints
            .Deflate(new Thickness(0, PaddingVertical + topHeight + bottomHeight + DensityOffset.Y, 0, 0))
            .Tighten(width: inputWidth);

        Size inputSize = _input is null ? default : layoutChild(_input, inputConstraints);
        BoxConstraints hintConstraints = boxConstraints.Tighten(width: inputWidth);
        Size hintSize = _hint is null ? default : layoutChild(_hint, hintConstraints);
        double inputBaseline = _input is null ? 0.0 : getBaseline(_input, inputConstraints);
        double hintBaseline = _hint is null ? 0.0 : getBaseline(_hint, hintConstraints);

        double inputHeight = Math.Max(
            _decoration.IsEmpty || _decoration.MaintainHintSize ? hintSize.Height : 0.0,
            inputSize.Height);
        double inputInternalBaseline = Math.Max(inputBaseline, hintBaseline);

        double prefixBaseline = _prefix is null ? 0.0 : getBaseline(_prefix, contentConstraints);
        double suffixBaseline = _suffix is null ? 0.0 : getBaseline(_suffix, contentConstraints);
        double fixHeight = Math.Max(prefixBaseline, suffixBaseline);
        double fixAboveInput = Math.Max(0.0, fixHeight - inputInternalBaseline);
        double fixBelowBaseline = Math.Max(
            prefixSize.Height - prefixBaseline,
            suffixSize.Height - suffixBaseline);
        double fixBelowInput = Math.Max(0.0, fixBelowBaseline - (inputHeight - inputInternalBaseline));

        double fixIconHeight = Math.Max(prefixIconSize.Height, suffixIconSize.Height);
        double contentHeight = Math.Max(
            fixIconHeight,
            topHeight + PaddingTop + fixAboveInput + inputHeight + fixBelowInput + PaddingBottom
            + DensityOffset.Y);
        double minContainerHeight = _decoration.IsDense || _decoration.IsCollapsed || _expands
            ? inputHeight
            : MinInteractiveDimension;
        double maxContainerHeight = Math.Max(0.0, boxConstraints.MaxHeight - bottomHeight);
        double containerHeight = _expands
            ? maxContainerHeight
            : Math.Min(Math.Max(contentHeight, minContainerHeight), maxContainerHeight);

        double interactiveAdjustment = minContainerHeight > contentHeight
            ? (minContainerHeight - contentHeight) / 2.0
            : 0.0;

        double overflow = Math.Max(0.0, contentHeight - maxContainerHeight);
        double textAlignVerticalFactor = (TextAlignVertical!.Value.Y + 1.0) / 2.0;
        double baselineAdjustment = fixAboveInput - (overflow * (1.0 - textAlignVerticalFactor));

        double topInputBaseline = PaddingTop + topHeight + inputInternalBaseline
                                  + baselineAdjustment + interactiveAdjustment + (DensityOffset.Y / 2.0);
        double maxContentHeight = containerHeight - PaddingVertical - topHeight - DensityOffset.Y;
        double alignableHeight = fixAboveInput + inputHeight + fixBelowInput;
        double maxVerticalOffset = maxContentHeight - alignableHeight;

        double baseline;
        if (IsOutlineAligned)
        {
            double outlineCenterBaseline = inputInternalBaseline
                                           + (baselineAdjustment / 2.0)
                                           + ((containerHeight - inputHeight) / 2.0);
            double outlineTopBaseline = topInputBaseline;
            double outlineBottomBaseline = topInputBaseline + maxVerticalOffset;
            baseline = InterpolateThree(
                outlineTopBaseline,
                outlineCenterBaseline,
                outlineBottomBaseline,
                TextAlignVertical.Value);
        }
        else
        {
            baseline = topInputBaseline + (maxVerticalOffset * textAlignVerticalFactor);
        }

        return new DecorationLayout(
            InputConstraints: inputConstraints,
            Baseline: baseline,
            ContainerHeight: containerHeight,
            Subtext: subtextSize,
            Size: new Size(constraints.MaxWidth, containerHeight + (subtextSize?.SubtextHeight ?? 0.0)));
    }

    private static double InterpolateThree(
        double begin,
        double middle,
        double end,
        TextAlignVertical textAlignVertical)
    {
        double basis = textAlignVertical.Y <= 0.0
            ? Math.Max(middle - begin, 0.0)
            : Math.Max(end - middle, 0.0);
        return middle + (basis * textAlignVertical.Y);
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        _labelTransform = null;
        DecorationLayout layout = Layout(constraints, LayoutChild, RealBaseline);
        Size = constraints.Constrain(layout.Size);
        double overallWidth = layout.Size.Width;

        if (_container is not null)
        {
            _container.Layout(
                BoxConstraints.TightFor(
                    width: overallWidth - BoxSize(_icon).Width,
                    height: layout.ContainerHeight),
                parentUsesSize: true);
            double containerX = IsRtl ? 0.0 : BoxSize(_icon).Width;
            ParentDataOf(_container).offset = new Point(containerX, 0.0);
        }

        double height = layout.ContainerHeight;

        double CenterLayout(RenderBox box, double x)
        {
            ParentDataOf(box).offset = new Point(x, (height - box.Size.Height) / 2.0);
            return box.Size.Width;
        }

        if (_icon is not null)
        {
            CenterLayout(_icon, IsRtl ? overallWidth - _icon.Size.Width : 0.0);
        }

        double subtextBaseline = (layout.Subtext?.Ascent ?? 0.0) + layout.ContainerHeight;
        double helperErrorBaseline = _helperError is null
            ? 0.0
            : _helperError.GetDistanceToBaseline(Plumix.UI.TextBaseline.Alphabetic, onlyReal: true)
              ?? _helperError.Size.Height;
        double counterBaseline = _counter is null
            ? 0.0
            : _counter.GetDistanceToBaseline(Plumix.UI.TextBaseline.Alphabetic, onlyReal: true)
              ?? _counter.Size.Height;

        double start;
        double end;
        if (IsRtl)
        {
            start = overallWidth - PaddingStart - BoxSize(_icon).Width;
            end = PaddingEnd;
            if (_helperError is not null)
            {
                ParentDataOf(_helperError).offset = new Point(
                    start - _helperError.Size.Width - _decoration.InputGap,
                    subtextBaseline - helperErrorBaseline);
            }

            if (_counter is not null)
            {
                ParentDataOf(_counter).offset = new Point(
                    end + _decoration.InputGap,
                    subtextBaseline - counterBaseline);
            }
        }
        else
        {
            start = PaddingStart + BoxSize(_icon).Width;
            end = overallWidth - PaddingEnd;
            if (_helperError is not null)
            {
                ParentDataOf(_helperError).offset = new Point(
                    start + _decoration.InputGap,
                    subtextBaseline - helperErrorBaseline);
            }

            if (_counter is not null)
            {
                ParentDataOf(_counter).offset = new Point(
                    end - _counter.Size.Width - _decoration.InputGap,
                    subtextBaseline - counterBaseline);
            }
        }

        double baseline = layout.Baseline;

        double BaselineLayout(RenderBox box, double x)
        {
            double childBaseline =
                box.GetDistanceToBaseline(Plumix.UI.TextBaseline.Alphabetic, onlyReal: true)
                ?? box.Size.Height;
            ParentDataOf(box).offset = new Point(x, baseline - childBaseline);
            return box.Size.Width;
        }

        if (IsRtl)
        {
            if (_prefixIcon is not null)
            {
                start += PaddingStart;
                start -= CenterLayout(_prefixIcon, start - _prefixIcon.Size.Width);
                start -= PrefixToInputGap;
            }
            else
            {
                start -= _decoration.InputGap;
            }

            if (_label is not null)
            {
                if (_decoration.AlignLabelWithHint)
                {
                    BaselineLayout(_label, start - _label.Size.Width);
                }
                else
                {
                    CenterLayout(_label, start - _label.Size.Width);
                }
            }

            if (_prefix is not null)
            {
                start -= BaselineLayout(_prefix, start - _prefix.Size.Width);
            }

            if (_input is not null)
            {
                BaselineLayout(_input, start - _input.Size.Width);
            }

            if (_hint is not null)
            {
                BaselineLayout(_hint, start - _hint.Size.Width);
            }

            if (_suffixIcon is not null)
            {
                end -= PaddingEnd;
                end += CenterLayout(_suffixIcon, end);
                end += InputToSuffixGap;
            }
            else
            {
                end += _decoration.InputGap;
            }

            if (_suffix is not null)
            {
                end += BaselineLayout(_suffix, end);
            }
        }
        else
        {
            if (_prefixIcon is not null)
            {
                start -= PaddingStart;
                start += CenterLayout(_prefixIcon, start);
                start += PrefixToInputGap;
            }
            else
            {
                start += _decoration.InputGap;
            }

            if (_label is not null)
            {
                if (_decoration.AlignLabelWithHint)
                {
                    BaselineLayout(_label, start);
                }
                else
                {
                    CenterLayout(_label, start);
                }
            }

            if (_prefix is not null)
            {
                start += BaselineLayout(_prefix, start);
            }

            if (_input is not null)
            {
                BaselineLayout(_input, start);
            }

            if (_hint is not null)
            {
                BaselineLayout(_hint, start);
            }

            if (_suffixIcon is not null)
            {
                end += PaddingEnd;
                end -= CenterLayout(_suffixIcon, end - _suffixIcon.Size.Width);
                end -= InputToSuffixGap;
            }
            else
            {
                end -= _decoration.InputGap;
            }

            if (_suffix is not null)
            {
                end -= BaselineLayout(_suffix, end - _suffix.Size.Width);
            }
        }

        if (_label is not null)
        {
            double labelX = ParentDataOf(_label).offset.X;
            double floatAlign = _decoration.FloatingLabelAlignment == FloatingLabelAlignment.Center ? 1.0 : 0.0;
            double floatWidth = BoxSize(_label).Width * FinalLabelScale;
            double offsetToPrefixIcon = 0.0;
            if (IsRtl)
            {
                if (_prefixIcon is not null && !_decoration.AlignLabelWithHint && _material3)
                {
                    offsetToPrefixIcon = BoxSize(_prefixIcon).Width - PaddingEnd;
                }

                _decoration.BorderGap.Start = Lerp(
                    labelX + BoxSize(_label).Width + offsetToPrefixIcon,
                    (BoxSize(_container).Width / 2.0) + (floatWidth / 2.0),
                    floatAlign);
            }
            else
            {
                if (_prefixIcon is not null && !_decoration.AlignLabelWithHint && _material3)
                {
                    offsetToPrefixIcon = -BoxSize(_prefixIcon).Width + PaddingStart;
                }

                _decoration.BorderGap.Start = Lerp(
                    labelX - BoxSize(_icon).Width + offsetToPrefixIcon,
                    (BoxSize(_container).Width / 2.0) - (floatWidth / 2.0),
                    floatAlign);
            }

            _decoration.BorderGap.Extent = _label.Size.Width * FinalLabelScale;
        }
        else
        {
            _decoration.BorderGap.Start = null;
            _decoration.BorderGap.Extent = 0.0;
        }
    }

    // ---- paint ------------------------------------------------------------------------------

    public override void Paint(PaintingContext context, Point offset)
    {
        PaintSlot(context, _container, offset);

        if (_label is not null)
        {
            Point labelOffset = ParentDataOf(_label).offset;
            double labelHeight = BoxSize(_label).Height;
            double labelWidth = BoxSize(_label).Width;
            double floatAlign = _decoration.FloatingLabelAlignment == FloatingLabelAlignment.Center ? 1.0 : 0.0;
            double floatWidth = labelWidth * FinalLabelScale;
            BorderSide borderSide = _decoration.Border.BorderSide;
            double t = _decoration.FloatingLabelProgress;
            bool isOutlineBorder = _decoration.Border.IsOutline;
            double outlinedFloatingY = (-labelHeight * FinalLabelScale / 2.0) - (borderSide.StrokeOffset / 2.0);
            double floatingY = isOutlineBorder
                ? outlinedFloatingY
                : PaddingTop + (DensityOffset.Y / 2.0);
            double scale = Lerp(1.0, FinalLabelScale, t);
            double centeredFloatX = ParentDataOf(_container!).offset.X
                                    + (BoxSize(_container).Width / 2.0)
                                    - (floatWidth / 2.0);

            double startX;
            double floatStartX;
            if (IsRtl)
            {
                startX = labelOffset.X + (labelWidth * (1.0 - scale));
                floatStartX = startX;
                if (_prefixIcon is not null && !_decoration.AlignLabelWithHint && isOutlineBorder && _material3)
                {
                    floatStartX += BoxSize(_prefixIcon).Width - PaddingEnd;
                }
            }
            else
            {
                startX = labelOffset.X;
                floatStartX = startX;
                if (_prefixIcon is not null && !_decoration.AlignLabelWithHint && isOutlineBorder && _material3)
                {
                    floatStartX += -BoxSize(_prefixIcon).Width + PaddingStart;
                }
            }

            double floatEndX = Lerp(floatStartX, centeredFloatX, floatAlign);
            double dx = Lerp(startX, floatEndX, t);
            double dy = Lerp(0.0, floatingY - labelOffset.Y, t);

            // Records where the label was painted, in this render object's own coordinate space so
            // that ApplyPaintTransform can hand the same matrix to the geometry protocol.
            Matrix4 labelPaintTransform = Matrix4.TranslationValues(dx, labelOffset.Y + dy, 0.0);
            labelPaintTransform.ScaleByDouble(scale, scale, 1.0, 1);
            _labelTransform = labelPaintTransform;
            context.PushTransform(
                NeedsCompositing,
                offset,
                labelPaintTransform,
                (childContext, childOffset) => childContext.PaintChild(_label, childOffset));
        }
        else
        {
            _labelTransform = null;
        }

        PaintSlot(context, _icon, offset);
        PaintSlot(context, _prefix, offset);
        PaintSlot(context, _suffix, offset);
        PaintSlot(context, _prefixIcon, offset);
        PaintSlot(context, _suffixIcon, offset);
        if (_decoration.IsEmpty)
        {
            PaintSlot(context, _hint, offset);
        }

        PaintSlot(context, _input, offset);
        PaintSlot(context, _helperError, offset);
        PaintSlot(context, _counter, offset);
    }

    private static void PaintSlot(PaintingContext context, RenderBox? child, Point offset)
    {
        if (child is not null)
        {
            context.PaintChild(child, ParentDataOf(child).offset + offset);
        }
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        base.ApplyPaintTransform(child, transform);

        if (ReferenceEquals(child, _label) && _labelTransform is { } labelTransform)
        {
            // The label is painted through _labelTransform, which already carries its absolute
            // position, so the offset the base implementation appended is cancelled out first.
            Point labelOffset = ParentDataOf(_label!).offset;
            transform.TranslateByDouble(-labelOffset.X, -labelOffset.Y, 0, 1);
            transform.Multiply(labelTransform);
        }
    }

    protected override bool HitTestSelf(Point position) => true;

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        foreach (RenderBox child in Children())
        {
            RenderBox localChild = child;
            bool isHit = result.AddWithPaintOffset(
                ParentDataOf(child).offset,
                position,
                (hitResult, transformed) => localChild.HitTest(hitResult, transformed));
            if (isHit)
            {
                return true;
            }
        }

        return false;
    }

    private void SetSlotChild(ref RenderBox? field, RenderBox? value)
    {
        if (ReferenceEquals(field, value))
        {
            return;
        }

        if (field is not null)
        {
            DropChild(field);
        }

        field = value;
        if (field is not null)
        {
            AdoptChild(field);
        }
    }

    private void SetLayoutValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        MarkNeedsLayout();
    }
}
