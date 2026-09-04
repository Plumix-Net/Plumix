using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/sliding_segmented_control.dart

/// <summary>An iOS sliding segmented control with a spring-animated selection thumb.</summary>
public sealed class CupertinoSlidingSegmentedControl<T> : StatefulWidget where T : notnull
{
    internal static readonly EdgeInsetsGeometry DefaultPadding =
        EdgeInsetsGeometry.Symmetric(horizontal: 3.0, vertical: 2.0);

    internal static readonly CupertinoDynamicColor DefaultThumbColor = CupertinoDynamicColor.WithBrightness(
        Color.FromUInt32(0xFFFFFFFF),
        Color.FromUInt32(0xFF636366),
        debugLabel: "CupertinoSlidingSegmentedControl.thumbColor");

    public CupertinoSlidingSegmentedControl(
        IReadOnlyDictionary<T, Widget> children,
        Action<T?> onValueChanged,
        IReadOnlySet<T>? disabledChildren = null,
        T? groupValue = default,
        CupertinoDynamicColor? thumbColor = null,
        EdgeInsetsGeometry? padding = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool proportionalWidth = false,
        bool isMomentary = false,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(onValueChanged);
        if (children.Count < 2)
        {
            throw new ArgumentException(
                "CupertinoSlidingSegmentedControl requires at least two children.",
                nameof(children));
        }

        if (groupValue is not null && !children.Keys.Contains(groupValue))
        {
            throw new ArgumentException(
                "The groupValue must be either null or one of the keys in the children map.",
                nameof(groupValue));
        }

        Children = children;
        OnValueChanged = onValueChanged;
        DisabledChildren = disabledChildren ?? EmptyReadOnlySet<T>.Instance;
        GroupValue = groupValue;
        ThumbColor = thumbColor ?? DefaultThumbColor;
        Padding = padding ?? DefaultPadding;
        BackgroundColor = backgroundColor ?? CupertinoColors.TertiarySystemFill;
        ProportionalWidth = proportionalWidth;
        IsMomentary = isMomentary;
    }

    public IReadOnlyDictionary<T, Widget> Children { get; }

    public IReadOnlySet<T> DisabledChildren { get; }

    public T? GroupValue { get; }

    public Action<T?> OnValueChanged { get; }

    public CupertinoDynamicColor BackgroundColor { get; }

    public bool ProportionalWidth { get; }

    public CupertinoDynamicColor ThumbColor { get; }

    public EdgeInsetsGeometry Padding { get; }

    public bool IsMomentary { get; }

    public override State CreateState() => new CupertinoSlidingSegmentedControlState<T>();
}

internal sealed class CupertinoSlidingSegmentedControlState<T> : State where T : notnull
{
    internal static readonly TimeSpan SpringAnimationDuration = TimeSpan.FromMilliseconds(412.0);
    private static readonly SpringDescription ThumbSpring = new(
        mass: 1.0,
        stiffness: 503.551,
        damping: 44.8799);

    private readonly Dictionary<T, LabeledGlobalKey<CupertinoSlidingSegmentButtonState<T>>> _segmentKeys = [];
    private readonly GlobalKey _renderKey = new SlidingRenderKey(Guid.NewGuid());
    private AnimationController? _thumbScaleController;
    private Animation<double>? _thumbScaleAnimation;
    private AnimationController? _thumbController;
    private HorizontalDragGestureRecognizer? _drag;
    private TapGestureRecognizer? _tap;
    private LongPressGestureRecognizer? _longPress;
    private T? _highlighted;
    private T? _pressed;
    private bool? _startedOnSelectedSegment;
    private bool _startedOnDisabledSegment;

    private CupertinoSlidingSegmentedControl<T> Current =>
        (CupertinoSlidingSegmentedControl<T>)StateWidget;

    internal AnimationController ThumbController => _thumbController!;

    internal bool IsThumbDragging =>
        (_startedOnSelectedSegment ?? false) && !_startedOnDisabledSegment;

    internal T? Highlighted => _highlighted;

    internal T? Pressed => _pressed;

    public override void InitState()
    {
        base.InitState();
        _highlighted = Current.GroupValue;
        _thumbScaleController = new AnimationController(
            value: 0.0,
            duration: SpringAnimationDuration,
            vsync: this);
        _thumbScaleAnimation = new DoubleTween(begin: 1.0, end: 0.95)
            .Animate(_thumbScaleController);
        _thumbController = new AnimationController(
            value: 0.0,
            duration: SpringAnimationDuration,
            vsync: this);

        _drag = new HorizontalDragGestureRecognizer
        {
            OnDown = HandleDragDown,
            OnUpdate = HandleDragUpdate,
            OnEnd = _ => HandleDragEnd(),
            OnCancel = HandleDragCancel,
        };
        _tap = new TapGestureRecognizer
        {
            OnTapUp = HandleTapUp,
        };
        _longPress = new LongPressGestureRecognizer
        {
            OnLongPress = () => { },
        };
        var team = new GestureArenaTeam { Captain = _drag };
        _drag.Team = team;
        _longPress.Team = team;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (IsThumbDragging || ValuesEqual(_highlighted, Current.GroupValue))
        {
            return;
        }

        StartThumbAnimation();
        _highlighted = Current.GroupValue;
    }

    public override Widget Build(BuildContext context)
    {
        IReadOnlyList<KeyValuePair<T, Widget>> entries = Current.Children.ToList();
        TextDirection direction = Directionality.Of(context);
        var physicalChildren = new List<Widget>((entries.Count * 2) - 1);
        for (int index = 0; index < entries.Count; index++)
        {
            KeyValuePair<T, Widget> entry = entries[index];
            bool highlighted = ValuesEqual(_highlighted, entry.Key);
            if (index > 0)
            {
                bool previousHighlighted = ValuesEqual(_highlighted, entries[index - 1].Key);
                physicalChildren.Add(new SlidingSegmentSeparator(
                    highlighted: previousHighlighted || highlighted,
                    key: new ValueKey<int>(index)));
            }

            if (!_segmentKeys.TryGetValue(
                    entry.Key,
                    out LabeledGlobalKey<CupertinoSlidingSegmentButtonState<T>>? segmentKey))
            {
                segmentKey = new LabeledGlobalKey<CupertinoSlidingSegmentButtonState<T>>(
                    "CupertinoSlidingSegmentedControl segment");
                _segmentKeys[entry.Key] = segmentKey;
            }

            int physicalSegmentIndex = direction == TextDirection.Ltr
                ? index
                : entries.Count - 1 - index;
            SlidingSegmentLocation location = physicalSegmentIndex switch
            {
                0 => SlidingSegmentLocation.Leftmost,
                _ when physicalSegmentIndex == entries.Count - 1 => SlidingSegmentLocation.Rightmost,
                _ => SlidingSegmentLocation.InBetween,
            };
            bool enabled = !Current.DisabledChildren.Contains(entry.Key);
            Widget child = new SlidingSegment<T>(
                child: entry.Value,
                pressed: ValuesEqual(_pressed, entry.Key),
                highlighted: highlighted,
                isDragging: IsThumbDragging,
                enabled: enabled,
                location: location,
                isMomentary: Current.IsMomentary,
                key: new ValueKey<T>(entry.Key));
            child = new MouseRegion(
                cursor: PlatformDefaults.IsWeb ? SystemMouseCursors.Click : MouseCursor.Defer,
                child: child);
            var semantics = new Semantics(
                flags: SemanticsFlags.IsButton | SemanticsFlags.IsInMutuallyExclusiveGroup,
                selected: ValuesEqual(Current.GroupValue, entry.Key),
                onTap: () =>
                {
                    if (!enabled)
                    {
                        return;
                    }

                    segmentKey.CurrentState?.FocusNode.RequestFocus();
                    Current.OnValueChanged(entry.Key);
                },
                child: child);
            physicalChildren.Add(new CupertinoSlidingSegmentButton<T>(
                value: entry.Key,
                enabled: enabled,
                child: semantics,
                key: segmentKey));
        }

        if (direction == TextDirection.Rtl)
        {
            physicalChildren.Reverse();
        }

        int? highlightedIndex = IndexOf(entries, _highlighted);
        if (direction == TextDirection.Rtl && highlightedIndex.HasValue)
        {
            highlightedIndex = entries.Count - 1 - highlightedIndex.Value;
        }

        Color thumbColor = CupertinoDynamicColor.Resolve(Current.ThumbColor, context);
        Color backgroundColor = CupertinoDynamicColor.Resolve(Current.BackgroundColor, context);
        Widget rendered = new AnimatedBuilder(
            animation: _thumbScaleAnimation!,
            builder: (_, _) => new CupertinoSlidingSegmentedControlRenderWidget<T>(
                highlightedIndex: Current.IsMomentary ? null : highlightedIndex,
                thumbColor: thumbColor,
                thumbScale: _thumbScaleAnimation!.Value,
                proportionalWidth: Current.ProportionalWidth,
                state: this,
                children: physicalChildren,
                key: _renderKey));
        Widget decorated = new Container(
            padding: Current.Padding,
            decoration: new ShapeDecoration(
                Shape: new RoundedSuperellipseBorder(
                    borderRadius: new BorderRadius(9.0)),
                Color: backgroundColor),
            child: rendered);
        decorated = new ClipRSuperellipse(
            borderRadius: new BorderRadius(9.0),
            clipBehavior: Clip.AntiAlias,
            child: decorated);
        Widget result = new UnconstrainedBox(
            constrainedAxis: Axis.Horizontal,
            child: decorated);
        result = new RadioGroup<T>(
            groupValue: Current.GroupValue,
            onChanged: value =>
            {
                if (value is not null && !Current.DisabledChildren.Contains(value))
                {
                    Current.OnValueChanged(value);
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
        _drag?.Dispose();
        _tap?.Dispose();
        _longPress?.Dispose();
        _thumbScaleController?.Dispose();
        _thumbController?.Dispose();
        _thumbScaleController = null;
        _thumbController = null;
        _drag = null;
        _tap = null;
        _longPress = null;
        base.Dispose();
    }

    internal void AddPointer(PointerDownEvent @event)
    {
        _tap!.AddPointer(@event);
        _drag!.AddPointer(@event);
        _longPress!.AddPointer(@event);
    }

    private void HandleTapUp(TapUpDetails details)
    {
        if (IsThumbDragging)
        {
            return;
        }

        T segment = SegmentForXPosition(details.LocalPosition.X);
        SetState(() => _pressed = default);
        if (Current.DisabledChildren.Contains(segment))
        {
            return;
        }

        RequestFocus(segment);
        if (!ValuesEqual(segment, Current.GroupValue))
        {
            Current.OnValueChanged(segment);
        }
    }

    private void HandleDragDown(DragDownDetails details)
    {
        T segment = SegmentForXPosition(details.LocalPosition.X);
        _startedOnSelectedSegment = ValuesEqual(segment, _highlighted);
        _startedOnDisabledSegment = Current.DisabledChildren.Contains(segment);
        if (_startedOnDisabledSegment)
        {
            return;
        }

        SetState(() => _pressed = segment);
        if (_startedOnSelectedSegment == true)
        {
            AnimateThumbScale(0.95);
        }
    }

    private void HandleDragUpdate(DragUpdateDetails details)
    {
        if (_startedOnDisabledSegment)
        {
            return;
        }

        T segment = SegmentForXPosition(details.LocalPosition.X);
        if (Current.DisabledChildren.Contains(segment))
        {
            return;
        }

        SetState(() =>
        {
            if (IsThumbDragging)
            {
                _pressed = segment;
                if (!ValuesEqual(_highlighted, segment))
                {
                    _highlighted = segment;
                    StartThumbAnimation();
                }
            }
            else
            {
                _pressed = HasDraggedTooFar(details.LocalPosition) ? default : segment;
            }
        });
    }

    private void HandleDragEnd()
    {
        if (IsThumbDragging)
        {
            AnimateThumbScale(1.0);
            if (!ValuesEqual(_highlighted, Current.GroupValue))
            {
                RequestFocus(_highlighted);
                Current.OnValueChanged(_highlighted);
            }
        }
        else if (_pressed is not null)
        {
            T value = _pressed;
            SetState(() => _highlighted = value);
            StartThumbAnimation();
            if (!ValuesEqual(value, Current.GroupValue))
            {
                RequestFocus(value);
                Current.OnValueChanged(value);
            }
        }

        SetState(() =>
        {
            _pressed = default;
            _startedOnSelectedSegment = null;
        });
    }

    private void HandleDragCancel()
    {
        if (IsThumbDragging)
        {
            AnimateThumbScale(1.0);
        }

        SetState(() =>
        {
            _pressed = default;
            _startedOnSelectedSegment = null;
        });
    }

    private void AnimateThumbScale(double target)
    {
        double currentScale = _thumbScaleAnimation!.Value;
        _thumbScaleAnimation = new DoubleTween(begin: currentScale, end: target)
            .Animate(_thumbScaleController!);
        _thumbScaleController!.AnimateWith(CreateSpringSimulation());
    }

    private void StartThumbAnimation()
    {
        _thumbController!.AnimateWith(CreateSpringSimulation());
        CurrentRenderObject?.ClearThumbAnimatable();
    }

    private bool HasDraggedTooFar(Point position)
    {
        RenderCupertinoSlidingSegmentedControl<T>? render = CurrentRenderObject;
        if (render is null || !render.HasSize)
        {
            return false;
        }

        double xDistance = Math.Max(0.0, Math.Abs(position.X - (render.Size.Width / 2.0)) - (render.Size.Width / 2.0));
        double yDistance = Math.Max(
            0.0,
            Math.Abs(position.Y - (render.Size.Height / 2.0)) - (render.Size.Height / 2.0));
        return (xDistance * xDistance) + (yDistance * yDistance) > 2500.0;
    }

    private T SegmentForXPosition(double x)
    {
        int physicalIndex = CurrentRenderObject?.GetClosestSegmentIndex(x) ?? 0;
        int logicalIndex = Directionality.Of(Context) == TextDirection.Rtl
            ? Current.Children.Count - 1 - physicalIndex
            : physicalIndex;
        return Current.Children.Keys.ElementAt(logicalIndex);
    }

    private void RequestFocus(T? value)
    {
        if (value is not null && _segmentKeys.TryGetValue(value, out var key))
        {
            key.CurrentState?.FocusNode.RequestFocus();
        }
    }

    private RenderCupertinoSlidingSegmentedControl<T>? CurrentRenderObject =>
        _renderKey.CurrentContext?.FindRenderObject() as RenderCupertinoSlidingSegmentedControl<T>;

    private static SpringSimulation CreateSpringSimulation() => new(
        ThumbSpring,
        start: 0.0,
        end: 1.0,
        velocity: 0.0);

    private static bool ValuesEqual(T? first, T? second) =>
        EqualityComparer<T?>.Default.Equals(first, second);

    private static int? IndexOf(IReadOnlyList<KeyValuePair<T, Widget>> entries, T? value)
    {
        if (value is null)
        {
            return null;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            if (ValuesEqual(entries[index].Key, value))
            {
                return index;
            }
        }

        return null;
    }

    private sealed record SlidingRenderKey(Guid Id) : GlobalKey;
}

internal enum SlidingSegmentLocation
{
    Leftmost,
    Rightmost,
    InBetween,
}

internal sealed class SlidingSegment<T> : StatefulWidget where T : notnull
{
    public SlidingSegment(
        Widget child,
        bool pressed,
        bool highlighted,
        bool isDragging,
        bool enabled,
        SlidingSegmentLocation location,
        bool isMomentary,
        Key? key = null) : base(key)
    {
        Child = child;
        Pressed = pressed;
        Highlighted = highlighted;
        IsDragging = isDragging;
        Enabled = enabled;
        Location = location;
        IsMomentary = isMomentary;
    }

    public Widget Child { get; }

    public bool Pressed { get; }

    public bool Highlighted { get; }

    public bool IsDragging { get; }

    public bool Enabled { get; }

    public SlidingSegmentLocation Location { get; }

    public bool IsMomentary { get; }

    public override State CreateState() => new SlidingSegmentState<T>();
}

internal sealed class SlidingSegmentState<T> : State where T : notnull
{
    private static readonly Color DisabledContentColor = Color.FromArgb(115, 122, 122, 122);
    private AnimationController? _scaleController;
    private Animation<double>? _scaleAnimation;

    private SlidingSegment<T> Current => (SlidingSegment<T>)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _scaleController = new AnimationController(
            value: 0.0,
            duration: CupertinoSlidingSegmentedControlState<T>.SpringAnimationDuration,
            vsync: this);
        _scaleAnimation = new DoubleTween(begin: 1.0, end: 1.0).Animate(_scaleController);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldSegment = (SlidingSegment<T>)oldWidget;
        bool oldShouldScale = ShouldScale(oldSegment);
        bool shouldScale = ShouldScale(Current);
        if (oldShouldScale == shouldScale)
        {
            return;
        }

        double currentScale = _scaleAnimation!.Value;
        Animatable<double> tween;
        if (shouldScale && Current.IsMomentary)
        {
            tween = new TweenSequence<double>(
            [
                new TweenSequenceItem<double>(
                    new DoubleTween(begin: currentScale, end: 1.05),
                    weight: 50.0),
                new TweenSequenceItem<double>(
                    new DoubleTween(begin: 1.05, end: 1.0),
                    weight: 50.0),
            ]);
        }
        else
        {
            tween = new DoubleTween(
                begin: currentScale,
                end: shouldScale ? 0.95 : 1.0);
        }

        _scaleAnimation = tween.Animate(_scaleController!);
        _scaleController!.AnimateWith(CreateSpringSimulation());
    }

    public override Widget Build(BuildContext context)
    {
        bool shouldFade = Current.Pressed
                          && !Current.Highlighted
                          && Current.Enabled
                          && !Current.IsMomentary;
        bool shouldHighlight = Current.Highlighted && !Current.IsMomentary;
        TextStyle style = DefaultTextStyle.Of(context).Merge(new TextStyle(
            FontSize: 13.0,
            FontWeight: shouldHighlight ? FontWeight.DemiBold : FontWeight.Medium,
            Color: Current.Enabled ? null : DisabledContentColor));
        Alignment alignment = Current.Location switch
        {
            SlidingSegmentLocation.Leftmost => Alignment.CenterLeft,
            SlidingSegmentLocation.Rightmost => Alignment.CenterRight,
            _ => Alignment.Center,
        };
        Widget visible = new ScaleTransition(
            scale: _scaleAnimation!,
            alignment: alignment,
            child: Current.Child);
        visible = new AnimatedDefaultTextStyle(
            child: visible,
            style: style,
            duration: TimeSpan.FromMilliseconds(200.0),
            curve: Curves.Ease);
        visible = new AnimatedOpacity(
            opacity: shouldFade ? 0.2 : 1.0,
            duration: TimeSpan.FromMilliseconds(470.0),
            curve: Curves.Ease,
            child: visible);
        Widget sizing = DefaultTextStyle.Merge(
            child: Current.Child,
            style: new TextStyle(
                FontSize: 13.0,
                FontWeight: FontWeight.DemiBold));
        return new MetaData(
            behavior: HitTestBehavior.Opaque,
            child: new IndexedStack(
                index: 0,
                alignment: Alignment.Center,
                children: [visible, sizing]));
    }

    public override void Dispose()
    {
        _scaleController?.Dispose();
        _scaleController = null;
        base.Dispose();
    }

    private static bool ShouldScale(SlidingSegment<T> segment) =>
        segment.Pressed
        && segment.Enabled
        && ((segment.Highlighted && segment.IsDragging) || segment.IsMomentary);

    private static SpringSimulation CreateSpringSimulation() => new(
        new SpringDescription(mass: 1.0, stiffness: 503.551, damping: 44.8799),
        start: 0.0,
        end: 1.0,
        velocity: 0.0);
}

internal sealed class SlidingSegmentSeparator : StatefulWidget
{
    public SlidingSegmentSeparator(bool highlighted, Key? key = null) : base(key)
    {
        Highlighted = highlighted;
    }

    public bool Highlighted { get; }

    public override State CreateState() => new SlidingSegmentSeparatorState();
}

internal sealed class SlidingSegmentSeparatorState : State
{
    private static readonly Color SeparatorColor = Color.FromUInt32(0x4D8E8E93);
    private AnimationController? _opacityController;

    private SlidingSegmentSeparator Current => (SlidingSegmentSeparator)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _opacityController = new AnimationController(
            value: Current.Highlighted ? 0.0 : 1.0,
            duration: CupertinoSlidingSegmentedControlState<string>.SpringAnimationDuration,
            vsync: this);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (((SlidingSegmentSeparator)oldWidget).Highlighted == Current.Highlighted)
        {
            return;
        }

        _opacityController!.AnimateTo(
            Current.Highlighted ? 0.0 : 1.0,
            duration: TimeSpan.FromMilliseconds(412.0),
            curve: Curves.Ease);
    }

    public override Widget Build(BuildContext context)
    {
        return new AnimatedBuilder(
            animation: _opacityController!,
            builder: (_, _) => new Padding(
                EdgeInsetsGeometry.Symmetric(vertical: 5.0),
                new DecoratedBox(
                    decoration: new ShapeDecoration(
                        Shape: new RoundedRectangleBorder(
                            borderRadius: new BorderRadius(0.5)),
                        Color: WithOpacity(SeparatorColor, _opacityController!.Value)),
                    child: new SizedBox(width: 1.0))));
    }

    public override void Dispose()
    {
        _opacityController?.Dispose();
        _opacityController = null;
        base.Dispose();
    }

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)Math.Round(color.A * opacity), 0, byte.MaxValue),
        color.R,
        color.G,
        color.B);
}

internal sealed class CupertinoSlidingSegmentButton<T> : StatefulWidget where T : notnull
{
    public CupertinoSlidingSegmentButton(
        T value,
        bool enabled,
        Widget child,
        Key? key = null) : base(key)
    {
        Value = value;
        Enabled = enabled;
        Child = child;
    }

    public T Value { get; }

    public bool Enabled { get; }

    public Widget Child { get; }

    public override State CreateState() => new CupertinoSlidingSegmentButtonState<T>();
}

internal sealed class CupertinoSlidingSegmentButtonState<T> : State, RadioClient<T> where T : notnull
{
    private readonly FocusNode _focusNode = new();
    private RadioGroupRegistry<T>? _registry;

    private CupertinoSlidingSegmentButton<T> Current =>
        (CupertinoSlidingSegmentButton<T>)StateWidget;

    public bool Tristate => false;

    public T RadioValue => Current.Value;

    public bool Enabled => Current.Enabled;

    public FocusNode FocusNode => _focusNode;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        UpdateRegistry();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (((CupertinoSlidingSegmentButton<T>)oldWidget).Enabled != Current.Enabled)
        {
            UpdateRegistry();
        }
    }

    public override Widget Build(BuildContext context)
    {
        return new Focus(
            focusNode: _focusNode,
            canRequestFocus: Current.Enabled,
            onKeyEvent: (_, _) => KeyEventResult.Ignored,
            child: Current.Child);
    }

    public override void Dispose()
    {
        SetRegistry(null);
        _focusNode.Dispose();
        base.Dispose();
    }

    private void UpdateRegistry()
    {
        SetRegistry(Current.Enabled ? RadioGroup<T>.MaybeOf(Context) : null);
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

internal sealed class CupertinoSlidingSegmentedControlParentData : ContainerBoxParentData<RenderBox>
{
}

internal sealed class CupertinoSlidingSegmentedControlRenderWidget<T> : MultiChildRenderObjectWidget
    where T : notnull
{
    public CupertinoSlidingSegmentedControlRenderWidget(
        int? highlightedIndex,
        Color thumbColor,
        double thumbScale,
        bool proportionalWidth,
        CupertinoSlidingSegmentedControlState<T> state,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(children, key)
    {
        HighlightedIndex = highlightedIndex;
        ThumbColor = thumbColor;
        ThumbScale = thumbScale;
        ProportionalWidth = proportionalWidth;
        State = state;
    }

    public int? HighlightedIndex { get; }

    public Color ThumbColor { get; }

    public double ThumbScale { get; }

    public bool ProportionalWidth { get; }

    public CupertinoSlidingSegmentedControlState<T> State { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderCupertinoSlidingSegmentedControl<T>(
            highlightedIndex: HighlightedIndex,
            thumbColor: ThumbColor,
            thumbScale: ThumbScale,
            proportionalWidth: ProportionalWidth,
            state: State);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var control = (RenderCupertinoSlidingSegmentedControl<T>)renderObject;
        control.HighlightedIndex = HighlightedIndex;
        control.ThumbColor = ThumbColor;
        control.ThumbScale = ThumbScale;
        control.ProportionalWidth = ProportionalWidth;
        control.ControlState = State;
    }
}

internal sealed class RenderCupertinoSlidingSegmentedControl<T> : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, CupertinoSlidingSegmentedControlParentData>,
    IRenderObjectContainer
    where T : notnull
{
    private static readonly IReadOnlyList<BoxShadow> ThumbShadows =
    [
        new BoxShadow(
            color: Color.FromUInt32(0x1F000000),
            offset: new Point(0.0, 3.0),
            blurRadius: 8.0),
        new BoxShadow(
            color: Color.FromUInt32(0x0A000000),
            offset: new Point(0.0, 3.0),
            blurRadius: 1.0),
    ];

    private readonly RenderBoxContainerDefaultsMixin<RenderBox, CupertinoSlidingSegmentedControlParentData>
        _children;
    private int? _highlightedIndex;
    private Color _thumbColor;
    private double _thumbScale;
    private bool _proportionalWidth;
    private CupertinoSlidingSegmentedControlState<T> _state;
    private Animatable<Rect>? _thumbAnimatable;
    private Rect? _thumbAnimatableEnd;

    public RenderCupertinoSlidingSegmentedControl(
        int? highlightedIndex,
        Color thumbColor,
        double thumbScale,
        bool proportionalWidth,
        CupertinoSlidingSegmentedControlState<T> state)
    {
        _children = new RenderBoxContainerDefaultsMixin<RenderBox, CupertinoSlidingSegmentedControlParentData>(this);
        _highlightedIndex = highlightedIndex;
        _thumbColor = thumbColor;
        _thumbScale = thumbScale;
        _proportionalWidth = proportionalWidth;
        _state = state;
    }

    public int? HighlightedIndex
    {
        get => _highlightedIndex;
        set
        {
            if (_highlightedIndex == value)
            {
                return;
            }

            _highlightedIndex = value;
            MarkNeedsPaint();
        }
    }

    public Color ThumbColor
    {
        get => _thumbColor;
        set
        {
            if (_thumbColor == value)
            {
                return;
            }

            _thumbColor = value;
            MarkNeedsPaint();
        }
    }

    public double ThumbScale
    {
        get => _thumbScale;
        set
        {
            if (_thumbScale == value)
            {
                return;
            }

            _thumbScale = value;
            MarkNeedsPaint();
        }
    }

    public bool ProportionalWidth
    {
        get => _proportionalWidth;
        set
        {
            if (_proportionalWidth == value)
            {
                return;
            }

            _proportionalWidth = value;
            MarkNeedsLayout();
        }
    }

    public CupertinoSlidingSegmentedControlState<T> ControlState
    {
        get => _state;
        set => _state = value;
    }

    public Rect? CurrentThumbRect { get; private set; }

    public int ChildCount => _children.ChildCount;

    public RenderBox? FirstChild => _children.FirstChild;

    public RenderBox? LastChild => _children.LastChild;

    private int SegmentCount => (ChildCount / 2) + 1;

    protected override void OnAttach()
    {
        base.OnAttach();
        _state.ThumbController.Changed += HandleThumbAnimationTick;
    }

    protected override void OnDetach()
    {
        _state.ThumbController.Changed -= HandleThumbAnimationTick;
        base.OnDetach();
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not CupertinoSlidingSegmentedControlParentData)
        {
            child.parentData = new CupertinoSlidingSegmentedControlParentData();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        IntrinsicWidth(height, static (child, extent) => child.GetMinIntrinsicWidth(extent));

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        IntrinsicWidth(height, static (child, extent) => child.GetMaxIntrinsicWidth(extent));

    protected override double ComputeMinIntrinsicHeight(double width) =>
        IntrinsicHeight(width, static (child, extent) => child.GetMinIntrinsicHeight(extent));

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        IntrinsicHeight(width, static (child, extent) => child.GetMaxIntrinsicHeight(extent));

    protected override Size ComputeDryLayout(BoxConstraints constraints) => ComputeOverallSize(constraints);

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        IReadOnlyList<double> widths = ComputeSegmentWidths(constraints);
        double height = GetMaxChildHeight(constraints.MaxWidth);
        double? result = null;
        int segmentIndex = 0;
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex % 2 != 0)
            {
                continue;
            }

            BoxConstraints childConstraints = BoxConstraints.TightFor(
                width: widths[segmentIndex++],
                height: height);
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
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex % 2 != 0)
            {
                continue;
            }

            double? childBaseline = child.GetDistanceToBaseline(baseline, onlyReal: true);
            if (childBaseline.HasValue)
            {
                result = !result.HasValue ? childBaseline : Math.Min(result.Value, childBaseline.Value);
            }
        }

        return result;
    }

    protected override void PerformLayout()
    {
        IReadOnlyList<double> widths = ComputeSegmentWidths(Constraints);
        double childHeight = GetMaxChildHeight(double.PositiveInfinity);
        BoxConstraints separatorConstraints = BoxConstraints.TightFor(height: childHeight);
        double start = 0.0;
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            bool isSegment = childIndex % 2 == 0;
            child.Layout(
                isSegment
                    ? BoxConstraints.TightFor(width: widths[childIndex / 2], height: childHeight)
                    : separatorConstraints,
                parentUsesSize: true);
            var data = (CupertinoSlidingSegmentedControlParentData)child.parentData!;
            data.offset = new Point(start, 0.0);
            start += child.Size.Width;
        }

        Size = ComputeOverallSize(Constraints);
        if (HighlightedIndex.HasValue)
        {
            Rect? target = SegmentRect(HighlightedIndex.Value);
            if (target.HasValue)
            {
                if (CurrentThumbRect is null || !_state.ThumbController.IsAnimating)
                {
                    CurrentThumbRect = MoveThumbRectInBounds(InflateHorizontally(target.Value, 1.0));
                    _thumbAnimatable = null;
                    _thumbAnimatableEnd = null;
                }
                else
                {
                    CurrentThumbRect = MoveThumbRectInBounds(CurrentThumbRect.Value);
                }
            }
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        PaintChildrenByParity(context, offset, even: false);
        PaintThumb(context, offset);
        PaintChildrenByParity(context, offset, even: true);
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        DebugHandleEvent(@event, entry);
        if (@event is PointerDownEvent down && !_state.IsThumbDragging)
        {
            _state.AddPointer(down);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (RenderBox? child = LastChild; child is not null; child = ChildBefore(child))
        {
            var data = (CupertinoSlidingSegmentedControlParentData)child.parentData!;
            Rect childRect = new(data.offset, child.Size);
            if (!childRect.Contains(position))
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

    public int GetClosestSegmentIndex(double dx)
    {
        int segmentIndex = 0;
        int childIndex = 0;
        RenderBox? lastSegment = null;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex % 2 != 0)
            {
                continue;
            }

            lastSegment = child;
            var data = (CupertinoSlidingSegmentedControlParentData)child.parentData!;
            if (dx <= data.offset.X + child.Size.Width)
            {
                return segmentIndex;
            }

            segmentIndex++;
        }

        return lastSegment is null ? 0 : Math.Max(0, SegmentCount - 1);
    }

    public void ClearThumbAnimatable()
    {
        _thumbAnimatable = null;
        _thumbAnimatableEnd = null;
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

    private double IntrinsicWidth(double height, Func<RenderBox, double, double> getter)
    {
        double maximum = 0.0;
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex % 2 == 0)
            {
                maximum = Math.Max(maximum, getter(child, height));
            }
        }

        return ((maximum + 20.0) * SegmentCount) + TotalSeparatorWidth;
    }

    private double IntrinsicHeight(double width, Func<RenderBox, double, double> getter)
    {
        double maximum = 28.0;
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex % 2 == 0)
            {
                maximum = Math.Max(maximum, getter(child, width));
            }
        }

        return maximum;
    }

    private IReadOnlyList<double> ComputeSegmentWidths(BoxConstraints constraints)
    {
        int count = SegmentCount;
        if (count <= 0)
        {
            return [];
        }

        double availableMin = Math.Max(0.0, constraints.MinWidth - TotalSeparatorWidth);
        double availableMax = double.IsPositiveInfinity(constraints.MaxWidth)
            ? double.PositiveInfinity
            : Math.Max(0.0, constraints.MaxWidth - TotalSeparatorWidth);
        var rawWidths = new List<double>(count);
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex % 2 == 0)
            {
                rawWidths.Add(child.GetMaxIntrinsicWidth(double.PositiveInfinity) + 20.0);
            }
        }

        if (ProportionalWidth)
        {
            double total = rawWidths.Sum();
            double target = Math.Clamp(total, availableMin, availableMax);
            double scale = total <= 0.0 ? 1.0 : target / total;
            return rawWidths.Select(width => width * scale).ToList();
        }

        double childWidth = availableMin / count;
        foreach (double width in rawWidths)
        {
            childWidth = Math.Max(childWidth, width);
        }

        childWidth = Math.Min(childWidth, availableMax / count);
        childWidth = Math.Max(0.0, childWidth);
        return Enumerable.Repeat(childWidth, count).ToList();
    }

    private double GetMaxChildHeight(double childWidth)
    {
        double height = 28.0;
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex % 2 == 0)
            {
                height = Math.Max(height, child.GetMaxIntrinsicHeight(childWidth));
            }
        }

        return height;
    }

    private Size ComputeOverallSize(BoxConstraints constraints)
    {
        IReadOnlyList<double> widths = ComputeSegmentWidths(constraints);
        double height = GetMaxChildHeight(constraints.MaxWidth);
        return constraints.Constrain(new Size(widths.Sum() + TotalSeparatorWidth, height));
    }

    private double TotalSeparatorWidth => ChildCount / 2;

    private void PaintChildrenByParity(PaintingContext context, Point offset, bool even)
    {
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if ((childIndex % 2 == 0) != even)
            {
                continue;
            }

            var data = (CupertinoSlidingSegmentedControlParentData)child.parentData!;
            context.PaintChild(child, offset + (Vector)data.offset);
        }
    }

    private void PaintThumb(PaintingContext context, Point offset)
    {
        if (!HighlightedIndex.HasValue)
        {
            CurrentThumbRect = null;
            return;
        }

        Rect? target = SegmentRect(HighlightedIndex.Value);
        if (!target.HasValue)
        {
            CurrentThumbRect = null;
            return;
        }

        Rect boundedTarget = MoveThumbRectInBounds(InflateHorizontally(target.Value, 1.0));
        AnimationController controller = _state.ThumbController;
        UpdateThumbAnimation(boundedTarget, controller);

        Rect unscaled = MoveThumbRectInBounds(
            _thumbAnimatable?.Transform(controller.Value) ?? boundedTarget);
        CurrentThumbRect = unscaled;
        Rect thumbRect = ScaleThumbRect(unscaled, HighlightedIndex.Value, ThumbScale);
        RSuperellipse shape = RSuperellipse.FromRectAndRadius(
            new Rect(thumbRect.Position + (Vector)offset, thumbRect.Size),
            Radius.Circular(7.0));
        foreach (BoxShadow shadow in ThumbShadows)
        {
            context.Canvas.DrawRSuperellipseShadow(shape, shadow);
        }

        context.Canvas.DrawRSuperellipse(
            shape.Inflate(0.5),
            new SolidColorBrush(Color.FromUInt32(0x0A000000)),
            null);
        context.Canvas.DrawRSuperellipse(shape, new SolidColorBrush(ThumbColor), null);
    }

    private void HandleThumbAnimationTick()
    {
        if (HighlightedIndex.HasValue)
        {
            Rect? target = SegmentRect(HighlightedIndex.Value);
            if (target.HasValue)
            {
                Rect boundedTarget = MoveThumbRectInBounds(InflateHorizontally(target.Value, 1.0));
                UpdateThumbAnimation(boundedTarget, _state.ThumbController);
                CurrentThumbRect = MoveThumbRectInBounds(
                    _thumbAnimatable?.Transform(_state.ThumbController.Value) ?? boundedTarget);
            }
        }

        MarkNeedsPaint();
    }

    private void UpdateThumbAnimation(Rect boundedTarget, AnimationController controller)
    {
        if (controller.IsAnimating && _thumbAnimatable is null)
        {
            Rect begin = MoveThumbRectInBounds(CurrentThumbRect ?? boundedTarget);
            _thumbAnimatable = new RectTween(begin: begin, end: boundedTarget);
            _thumbAnimatableEnd = boundedTarget;
        }
        else if (controller.IsAnimating && _thumbAnimatableEnd != boundedTarget)
        {
            Rect begin = MoveThumbRectInBounds(CurrentThumbRect ?? boundedTarget);
            _thumbAnimatable = new RectTween(begin: begin, end: boundedTarget)
                .Chain(new CurveTween(Curves.Interval(controller.Value, 1.0)));
            _thumbAnimatableEnd = boundedTarget;
        }
        else if (!controller.IsAnimating)
        {
            _thumbAnimatable = null;
            _thumbAnimatableEnd = null;
        }
    }

    private Rect? SegmentRect(int segmentIndex)
    {
        int targetChildIndex = segmentIndex * 2;
        int childIndex = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child), childIndex++)
        {
            if (childIndex != targetChildIndex)
            {
                continue;
            }

            var data = (CupertinoSlidingSegmentedControlParentData)child.parentData!;
            return new Rect(data.offset, child.Size);
        }

        return null;
    }

    private Rect MoveThumbRectInBounds(Rect rect)
    {
        Rect? first = SegmentRect(0);
        Rect? last = SegmentRect(Math.Max(0, SegmentCount - 1));
        if (!first.HasValue || !last.HasValue)
        {
            return rect;
        }

        double minimumLeft = first.Value.Left - 1.0;
        double maximumRight = last.Value.Right + 1.0;
        double left = Math.Clamp(rect.Left, minimumLeft, maximumRight);
        double right = Math.Clamp(rect.Right, minimumLeft, maximumRight);
        return new Rect(
            left,
            first.Value.Top,
            Math.Max(0.0, right - left),
            first.Value.Height);
    }

    private Rect ScaleThumbRect(Rect rect, int highlightedIndex, double scale)
    {
        double width = rect.Width * scale;
        double height = rect.Height * scale;
        double left = highlightedIndex switch
        {
            0 => rect.Left,
            _ when highlightedIndex == SegmentCount - 1 => rect.Right - width,
            _ => rect.Center.X - (width / 2.0),
        };
        return new Rect(
            left,
            rect.Center.Y - (height / 2.0),
            width,
            height);
    }

    private static Rect InflateHorizontally(Rect rect, double amount) => new(
        rect.Left - amount,
        rect.Top,
        rect.Width + (amount * 2.0),
        rect.Height);
}
