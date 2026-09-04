using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/segmented_control.dart

/// <summary>An iOS-style, mutually exclusive segmented control.</summary>
public sealed class CupertinoSegmentedControl<T> : StatefulWidget where T : notnull
{
    public CupertinoSegmentedControl(
        IReadOnlyDictionary<T, Widget> children,
        Action<T> onValueChanged,
        T? groupValue = default,
        Color? unselectedColor = null,
        Color? selectedColor = null,
        Color? borderColor = null,
        Color? pressedColor = null,
        Color? disabledColor = null,
        Color? disabledTextColor = null,
        EdgeInsetsGeometry? padding = null,
        IReadOnlySet<T>? disabledChildren = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(onValueChanged);
        if (children.Count < 2)
        {
            throw new ArgumentException("CupertinoSegmentedControl requires at least two children.", nameof(children));
        }

        if (groupValue is not null && !children.Keys.Contains(groupValue))
        {
            throw new ArgumentException(
                "The groupValue must be either null or one of the keys in the children map.",
                nameof(groupValue));
        }

        Children = children;
        OnValueChanged = onValueChanged;
        GroupValue = groupValue;
        UnselectedColor = unselectedColor;
        SelectedColor = selectedColor;
        BorderColor = borderColor;
        PressedColor = pressedColor;
        DisabledColor = disabledColor;
        DisabledTextColor = disabledTextColor;
        Padding = padding;
        DisabledChildren = disabledChildren ?? EmptyReadOnlySet<T>.Instance;
    }

    public IReadOnlyDictionary<T, Widget> Children { get; }

    public Action<T> OnValueChanged { get; }

    public T? GroupValue { get; }

    public Color? UnselectedColor { get; }

    public Color? SelectedColor { get; }

    public Color? BorderColor { get; }

    public Color? PressedColor { get; }

    public Color? DisabledColor { get; }

    public Color? DisabledTextColor { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public IReadOnlySet<T> DisabledChildren { get; }

    public override State CreateState() => new CupertinoSegmentedControlState<T>();
}

internal sealed class CupertinoSegmentedControlState<T> : State where T : notnull
{
    private static readonly TimeSpan SelectionAnimationDuration = TimeSpan.FromMilliseconds(165.0);
    private static readonly Color DefaultDisabledTextColor = Color.FromArgb(115, 122, 122, 122);

    private readonly Dictionary<T, LabeledGlobalKey<CupertinoSegmentButtonState<T>>> _segmentKeys = [];
    private readonly List<AnimationController> _selectionControllers = [];
    private readonly List<ColorTween> _childTweens = [];
    private Color _selectedColor;
    private Color _unselectedColor;
    private Color _selectedDisabledColor;
    private Color _unselectedDisabledColor;
    private Color _borderColor;
    private Color _pressedColor;
    private Color _disabledTextColor;
    private bool _colorsInitialized;
    private T? _pressedKey;
    private bool _hasPressedKey;

    private CupertinoSegmentedControl<T> CurrentWidget =>
        (CupertinoSegmentedControl<T>)StateWidget;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        UpdateColors();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldControl = (CupertinoSegmentedControl<T>)oldWidget;
        bool childCountChanged = oldControl.Children.Count != CurrentWidget.Children.Count;
        UpdateColors();
        if (childCountChanged)
        {
            ResetSelectionControllers();
            return;
        }

        if (!EqualityComparer<T?>.Default.Equals(oldControl.GroupValue, CurrentWidget.GroupValue))
        {
            AnimateSelectionChange();
        }
    }

    public override Widget Build(BuildContext context)
    {
        EnsureSelectionControllers();
        IReadOnlyList<T> keys = CurrentWidget.Children.Keys.ToList();
        int? selectedIndex = IndexOf(keys, CurrentWidget.GroupValue);
        int? pressedIndex = _hasPressedKey ? IndexOf(keys, _pressedKey) : null;
        var backgroundColors = new List<Color>(keys.Count);
        var children = new List<Widget>(keys.Count);
        for (int index = 0; index < keys.Count; index++)
        {
            T value = keys[index];
            bool enabled = !CurrentWidget.DisabledChildren.Contains(value);
            Color textColor = GetTextColor(index, value, enabled);
            backgroundColors.Add(GetBackgroundColor(index, value, enabled));
            if (!_segmentKeys.TryGetValue(value, out LabeledGlobalKey<CupertinoSegmentButtonState<T>>? segmentKey))
            {
                segmentKey = new LabeledGlobalKey<CupertinoSegmentButtonState<T>>("Segmented control segment");
                _segmentKeys[value] = segmentKey;
            }

            Widget child = new Semantics(
                flags: SemanticsFlags.IsButton | SemanticsFlags.IsInMutuallyExclusiveGroup,
                selected: EqualityComparer<T?>.Default.Equals(CurrentWidget.GroupValue, value),
                child: new Center(child: CurrentWidget.Children[value]));
            child = new DefaultTextStyle(
                style: DefaultTextStyle.Of(context).CopyWith(color: textColor),
                child: child);
            child = new IconTheme(
                data: new IconThemeData(Color: textColor),
                child: child);
            child = new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTapDown: enabled ? _ => HandleTapDown(value) : null,
                onTapCancel: enabled ? HandleTapCancel : null,
                onTap: () =>
                {
                    if (enabled)
                    {
                        segmentKey.CurrentState?.FocusNode.RequestFocus();
                    }
                    HandleTap(value);
                },
                child: child);
            child = new MouseRegion(
                cursor: PlatformDefaults.IsWeb ? SystemMouseCursors.Click : MouseCursor.Defer,
                child: child);
            children.Add(new CupertinoSegmentButton<T>(
                value: value,
                enabled: enabled,
                child: child,
                key: segmentKey));
        }

        Widget result = new CupertinoSegmentedControlRenderWidget(
            selectedIndex: selectedIndex,
            pressedIndex: pressedIndex,
            backgroundColors: backgroundColors,
            borderColor: _borderColor,
            textDirection: Directionality.Of(context),
            children: children);
        result = new UnconstrainedBox(
            constrainedAxis: Axis.Horizontal,
            child: result);
        result = new Padding(
            CurrentWidget.Padding ?? EdgeInsetsGeometry.Symmetric(horizontal: 16.0),
            result);
        result = new RadioGroup<T>(
            groupValue: CurrentWidget.GroupValue,
            onChanged: value =>
            {
                if (value is not null && !CurrentWidget.DisabledChildren.Contains(value))
                {
                    CurrentWidget.OnValueChanged(value);
                }
            },
            child: result);
        return new Actions(
            actions: new Dictionary<Type, FlutterAction>
            {
                [typeof(VoidCallbackIntent)] = new VoidCallbackAction(),
            },
            child: result);
    }

    public override void Dispose()
    {
        DisposeSelectionControllers();
        base.Dispose();
    }

    private void UpdateColors()
    {
        CupertinoThemeData theme = CupertinoTheme.Of(Context);
        Color selected = CurrentWidget.SelectedColor ?? theme.PrimaryColor;
        Color unselected = CurrentWidget.UnselectedColor ?? theme.PrimaryContrastingColor;
        Color selectedDisabled = CurrentWidget.DisabledColor ?? WithOpacity(selected, 0.5);
        Color unselectedDisabled = CurrentWidget.DisabledColor ?? unselected;
        Color border = CurrentWidget.BorderColor ?? theme.PrimaryColor;
        Color pressed = CurrentWidget.PressedColor ?? WithOpacity(theme.PrimaryColor, 0.2);
        Color disabledText = CurrentWidget.DisabledTextColor ?? DefaultDisabledTextColor;
        bool changed = !_colorsInitialized
                       || _selectedColor != selected
                       || _unselectedColor != unselected
                       || _selectedDisabledColor != selectedDisabled
                       || _unselectedDisabledColor != unselectedDisabled
                       || _borderColor != border
                       || _pressedColor != pressed
                       || _disabledTextColor != disabledText;
        _selectedColor = selected;
        _unselectedColor = unselected;
        _selectedDisabledColor = selectedDisabled;
        _unselectedDisabledColor = unselectedDisabled;
        _borderColor = border;
        _pressedColor = pressed;
        _disabledTextColor = disabledText;
        _colorsInitialized = true;
        if (changed)
        {
            ResetSelectionControllers();
        }
    }

    private void EnsureSelectionControllers()
    {
        if (_selectionControllers.Count != CurrentWidget.Children.Count)
        {
            ResetSelectionControllers();
        }
    }

    private void ResetSelectionControllers()
    {
        DisposeSelectionControllers();
        foreach (T key in CurrentWidget.Children.Keys)
        {
            bool selected = EqualityComparer<T?>.Default.Equals(CurrentWidget.GroupValue, key);
            var controller = new AnimationController(
                value: selected ? 1.0 : 0.0,
                duration: SelectionAnimationDuration,
                vsync: this);
            controller.Changed += HandleAnimationTick;
            _selectionControllers.Add(controller);
            _childTweens.Add(selected ? ReverseBackgroundTween() : ForwardBackgroundTween());
        }
    }

    private void DisposeSelectionControllers()
    {
        foreach (AnimationController controller in _selectionControllers)
        {
            controller.Changed -= HandleAnimationTick;
            controller.Dispose();
        }
        _selectionControllers.Clear();
        _childTweens.Clear();
    }

    private void AnimateSelectionChange()
    {
        IReadOnlyList<T> keys = CurrentWidget.Children.Keys.ToList();
        for (int index = 0; index < keys.Count; index++)
        {
            if (EqualityComparer<T?>.Default.Equals(CurrentWidget.GroupValue, keys[index]))
            {
                _childTweens[index] = ForwardBackgroundTween();
                _ = _selectionControllers[index].Forward();
            }
            else
            {
                _childTweens[index] = ReverseBackgroundTween();
                _ = _selectionControllers[index].Reverse();
            }
        }
    }

    private void HandleTapDown(T key)
    {
        if (_hasPressedKey || EqualityComparer<T?>.Default.Equals(CurrentWidget.GroupValue, key))
        {
            return;
        }

        SetState(() =>
        {
            _pressedKey = key;
            _hasPressedKey = true;
        });
    }

    private void HandleTapCancel()
    {
        SetState(() =>
        {
            _pressedKey = default;
            _hasPressedKey = false;
        });
    }

    private void HandleTap(T key)
    {
        if (!_hasPressedKey || !EqualityComparer<T?>.Default.Equals(key, _pressedKey))
        {
            return;
        }

        _segmentKeys[key].CurrentState?.FocusNode.RequestFocus();
        if (!EqualityComparer<T?>.Default.Equals(CurrentWidget.GroupValue, key))
        {
            CurrentWidget.OnValueChanged(key);
        }
        HandleTapCancel();
    }

    private Color GetTextColor(int index, T key, bool enabled)
    {
        if (!enabled)
        {
            return _disabledTextColor;
        }

        AnimationController controller = _selectionControllers[index];
        if (controller.IsAnimating)
        {
            return TextColorTween().Evaluate(controller.Value);
        }

        return EqualityComparer<T?>.Default.Equals(CurrentWidget.GroupValue, key)
            ? _unselectedColor
            : _selectedColor;
    }

    private Color GetBackgroundColor(int index, T key, bool enabled)
    {
        bool selected = EqualityComparer<T?>.Default.Equals(CurrentWidget.GroupValue, key);
        if (!enabled)
        {
            return selected ? _selectedDisabledColor : _unselectedDisabledColor;
        }

        AnimationController controller = _selectionControllers[index];
        if (controller.IsAnimating)
        {
            return _childTweens[index].Evaluate(controller.Value);
        }

        if (selected)
        {
            return _selectedColor;
        }

        return _hasPressedKey && EqualityComparer<T?>.Default.Equals(_pressedKey, key)
            ? _pressedColor
            : _unselectedColor;
    }

    private ColorTween ForwardBackgroundTween() => new(_pressedColor, _selectedColor);

    private ColorTween ReverseBackgroundTween() => new(_unselectedColor, _selectedColor);

    private ColorTween TextColorTween() => new(_selectedColor, _unselectedColor);

    private void HandleAnimationTick()
    {
        if (Mounted)
        {
            SetState(() => { });
        }
    }

    private static int? IndexOf(IReadOnlyList<T> keys, T? value)
    {
        if (value is null)
        {
            return null;
        }

        for (int index = 0; index < keys.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(keys[index], value))
            {
                return index;
            }
        }
        return null;
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0.0, 1.0));
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

internal sealed class CupertinoSegmentButton<T> : StatefulWidget where T : notnull
{
    public CupertinoSegmentButton(T value, bool enabled, Widget child, Key? key = null) : base(key)
    {
        Value = value;
        Enabled = enabled;
        Child = child;
    }

    public T Value { get; }

    public bool Enabled { get; }

    public Widget Child { get; }

    public override State CreateState() => new CupertinoSegmentButtonState<T>();
}

internal sealed class CupertinoSegmentButtonState<T> : State, RadioClient<T> where T : notnull
{
    private readonly FocusNode _focusNode = new();
    private RadioGroupRegistry<T>? _registry;

    private CupertinoSegmentButton<T> CurrentWidget => (CupertinoSegmentButton<T>)StateWidget;

    public bool Tristate => false;

    public T RadioValue => CurrentWidget.Value;

    public bool Enabled => CurrentWidget.Enabled;

    public FocusNode FocusNode => _focusNode;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        SetRegistry(RadioGroup<T>.MaybeOf(Context));
    }

    public override Widget Build(BuildContext context)
    {
        return new Focus(
            focusNode: _focusNode,
            canRequestFocus: CurrentWidget.Enabled,
            onKeyEvent: (_, _) => KeyEventResult.Ignored,
            child: CurrentWidget.Child);
    }

    public override void Dispose()
    {
        SetRegistry(null);
        _focusNode.Dispose();
        base.Dispose();
    }

    private void SetRegistry(RadioGroupRegistry<T>? registry)
    {
        if (ReferenceEquals(_registry, registry))
        {
            return;
        }

        _registry?.UnregisterClient(this);
        _registry = registry;
        _registry?.RegisterClient(this);
    }
}

internal sealed class CupertinoSegmentedControlParentData : ContainerBoxParentData<RenderBox>
{
    public RSuperellipse SurroundingRect { get; set; }
}

internal sealed class CupertinoSegmentedControlRenderWidget : MultiChildRenderObjectWidget
{
    public CupertinoSegmentedControlRenderWidget(
        int? selectedIndex,
        int? pressedIndex,
        IReadOnlyList<Color> backgroundColors,
        Color borderColor,
        TextDirection textDirection,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(children, key)
    {
        SelectedIndex = selectedIndex;
        PressedIndex = pressedIndex;
        BackgroundColors = backgroundColors;
        BorderColor = borderColor;
        TextDirection = textDirection;
    }

    public int? SelectedIndex { get; }

    public int? PressedIndex { get; }

    public IReadOnlyList<Color> BackgroundColors { get; }

    public Color BorderColor { get; }

    public TextDirection TextDirection { get; }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderCupertinoSegmentedControl(
        selectedIndex: SelectedIndex,
        pressedIndex: PressedIndex,
        backgroundColors: BackgroundColors,
        borderColor: BorderColor,
        textDirection: TextDirection);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var segmentedControl = (RenderCupertinoSegmentedControl)renderObject;
        segmentedControl.SelectedIndex = SelectedIndex;
        segmentedControl.PressedIndex = PressedIndex;
        segmentedControl.BackgroundColors = BackgroundColors;
        segmentedControl.BorderColor = BorderColor;
        segmentedControl.TextDirection = TextDirection;
    }
}

internal sealed class RenderCupertinoSegmentedControl : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, CupertinoSegmentedControlParentData>,
    IRenderObjectContainer
{
    private const double MinimumHeight = 28.0;
    private const double CornerRadius = 3.0;

    private readonly RenderBoxContainerDefaultsMixin<RenderBox, CupertinoSegmentedControlParentData> _children;
    private int? _selectedIndex;
    private int? _pressedIndex;
    private IReadOnlyList<Color> _backgroundColors;
    private Color _borderColor;
    private TextDirection _textDirection;

    public RenderCupertinoSegmentedControl(
        int? selectedIndex,
        int? pressedIndex,
        IReadOnlyList<Color> backgroundColors,
        Color borderColor,
        TextDirection textDirection)
    {
        _children = new RenderBoxContainerDefaultsMixin<RenderBox, CupertinoSegmentedControlParentData>(this);
        _selectedIndex = selectedIndex;
        _pressedIndex = pressedIndex;
        _backgroundColors = backgroundColors;
        _borderColor = borderColor;
        _textDirection = textDirection;
    }

    public int? SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex == value)
            {
                return;
            }
            _selectedIndex = value;
            MarkNeedsPaint();
        }
    }

    public int? PressedIndex
    {
        get => _pressedIndex;
        set
        {
            if (_pressedIndex == value)
            {
                return;
            }
            _pressedIndex = value;
            MarkNeedsPaint();
        }
    }

    public IReadOnlyList<Color> BackgroundColors
    {
        get => _backgroundColors;
        set
        {
            if (_backgroundColors.SequenceEqual(value))
            {
                return;
            }
            _backgroundColors = value;
            MarkNeedsPaint();
        }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor == value)
            {
                return;
            }
            _borderColor = value;
            MarkNeedsPaint();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }
            _textDirection = value;
            MarkNeedsLayout();
        }
    }

    public int ChildCount => _children.ChildCount;

    public RenderBox? FirstChild => _children.FirstChild;

    public RenderBox? LastChild => _children.LastChild;

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not CupertinoSegmentedControlParentData)
        {
            child.parentData = new CupertinoSegmentedControlParentData();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        ChildCount * MaximumIntrinsic(static (child, extent) => child.GetMinIntrinsicWidth(extent), height);

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        ChildCount * MaximumIntrinsic(static (child, extent) => child.GetMaxIntrinsicWidth(extent), height);

    protected override double ComputeMinIntrinsicHeight(double width) =>
        MaximumIntrinsic(static (child, extent) => child.GetMinIntrinsicHeight(extent), width);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        MaximumIntrinsic(static (child, extent) => child.GetMaxIntrinsicHeight(extent), width);

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        Size childSize = CalculateChildSize(constraints);
        return constraints.Constrain(new Size(childSize.Width * ChildCount, childSize.Height));
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        Size childSize = CalculateChildSize(constraints);
        BoxConstraints childConstraints = BoxConstraints.Tight(childSize);
        double? result = null;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
            if (childBaseline.HasValue)
            {
                result = !result.HasValue ? childBaseline : Math.Min(result.Value, childBaseline.Value);
            }
        }
        return result;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        double? result = null;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            double? childBaseline = child.GetDistanceToBaseline(baseline, onlyReal: true);
            if (!childBaseline.HasValue)
            {
                continue;
            }

            var data = (CupertinoSegmentedControlParentData)child.parentData!;
            double positionedBaseline = data.offset.Y + childBaseline.Value;
            result = !result.HasValue ? positionedBaseline : Math.Min(result.Value, positionedBaseline);
        }
        return result;
    }

    protected override void PerformLayout()
    {
        Size childSize = CalculateChildSize(Constraints);
        BoxConstraints childConstraints = BoxConstraints.TightFor(
            width: childSize.Width,
            height: childSize.Height);
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(childConstraints, parentUsesSize: true);
        }

        Size = Constraints.Constrain(new Size(childSize.Width * ChildCount, childSize.Height));
        double position = 0.0;
        RenderBox? positionedChild = TextDirection == TextDirection.Ltr ? FirstChild : LastChild;
        while (positionedChild is not null)
        {
            var data = (CupertinoSegmentedControlParentData)positionedChild.parentData!;
            data.offset = new Point(position, 0.0);
            bool firstPhysical = position == 0.0;
            bool lastPhysical = position + childSize.Width == Size.Width;
            data.SurroundingRect = RSuperellipse.FromRectAndCorners(
                new Rect(data.offset, childSize),
                topLeft: firstPhysical ? Radius.Circular(CornerRadius) : Radius.Zero,
                topRight: lastPhysical ? Radius.Circular(CornerRadius) : Radius.Zero,
                bottomRight: lastPhysical ? Radius.Circular(CornerRadius) : Radius.Zero,
                bottomLeft: firstPhysical ? Radius.Circular(CornerRadius) : Radius.Zero);
            position += childSize.Width;
            positionedChild = TextDirection == TextDirection.Ltr
                ? ChildAfter(positionedChild)
                : ChildBefore(positionedChild);
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        int childIndex = 0;
        var borderPen = new Pen(new SolidColorBrush(BorderColor), 1.0);
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var data = (CupertinoSegmentedControlParentData)child.parentData!;
            RSuperellipse shape = data.SurroundingRect.Shift(offset);
            context.Canvas.DrawRSuperellipse(shape, new SolidColorBrush(BackgroundColors[childIndex]), null);
            context.Canvas.DrawRSuperellipse(shape, null, borderPen);
            context.PaintChild(child, offset + (Vector)data.offset);
            childIndex++;
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (RenderBox? child = LastChild; child is not null; child = ChildBefore(child))
        {
            var data = (CupertinoSegmentedControlParentData)child.parentData!;
            if (!data.SurroundingRect.OuterRect.Contains(position))
            {
                continue;
            }

            return result.AddWithPaintOffset(
                data.offset,
                position,
                (boxResult, localPosition) => child.HitTest(boxResult, localPosition));
        }
        return false;
    }

    public void AddAll(List<RenderBox>? children) => _children.AddAll(children);

    public void RemoveAll() => _children.RemoveAll();

    public RenderBox? ChildBefore(RenderBox child) => _children.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _children.ChildAfter(child);

    public void DefaultPaint(PaintingContext context, Point offset) => _children.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _children.DefaultHitTestChildren(result, position);

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor) => VisitChildren(visitor);

    public void Insert(RenderBox child, RenderBox? after = null) => _children.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _children.Move(child, after);

    public void Remove(RenderBox child) => _children.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private Size CalculateChildSize(BoxConstraints constraints)
    {
        if (ChildCount == 0)
        {
            return constraints.Smallest;
        }

        double childWidth = constraints.MinWidth / ChildCount;
        double maxHeight = MinimumHeight;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            childWidth = Math.Max(childWidth, child.GetMaxIntrinsicWidth(double.PositiveInfinity));
        }
        childWidth = Math.Min(childWidth, constraints.MaxWidth / ChildCount);
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            maxHeight = Math.Max(maxHeight, child.GetMaxIntrinsicHeight(childWidth));
        }
        return new Size(childWidth, maxHeight);
    }

    private double MaximumIntrinsic(Func<RenderBox, double, double> query, double extent)
    {
        double maximum = 0.0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            maximum = Math.Max(maximum, query(child, extent));
        }
        return maximum;
    }
}

internal sealed class EmptyReadOnlySet<T> : IReadOnlySet<T>
{
    public static EmptyReadOnlySet<T> Instance { get; } = new();

    public int Count => 0;

    public bool Contains(T item) => false;

    public bool IsProperSubsetOf(IEnumerable<T> other) => other.Any();

    public bool IsProperSupersetOf(IEnumerable<T> other) => false;

    public bool IsSubsetOf(IEnumerable<T> other) => true;

    public bool IsSupersetOf(IEnumerable<T> other) => !other.Any();

    public bool Overlaps(IEnumerable<T> other) => false;

    public bool SetEquals(IEnumerable<T> other) => !other.Any();

    public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
