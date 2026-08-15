using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_state.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/raw_radio.dart
public enum WidgetState
{
    Hovered,
    Focused,
    Pressed,
    Disabled,
    Selected,
    Dragged,
    ScrolledUnder,
    Error,
}

public abstract class WidgetStateProperty<T>
{
    public abstract T Resolve(IReadOnlySet<WidgetState> states);

    public static T ResolveAs(object value, IReadOnlySet<WidgetState> states)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value is WidgetStateProperty<T> property
            ? property.Resolve(states)
            : (T)value;
    }

    public static WidgetStateProperty<T> All(T value)
    {
        return new WidgetStatePropertyAll<T>(value);
    }

    public static WidgetStateProperty<T> ResolveWith(Func<IReadOnlySet<WidgetState>, T> resolver)
    {
        return new WidgetStatePropertyResolver<T>(resolver);
    }

    public static WidgetStateProperty<T>? Lerp(
        WidgetStateProperty<T>? a,
        WidgetStateProperty<T>? b,
        double t,
        Func<T, T, double, T> lerpFunction)
    {
        ArgumentNullException.ThrowIfNull(lerpFunction);
        if (a is null && b is null)
        {
            return null;
        }

        return ResolveWith(states => lerpFunction(
            a is null ? default! : a.Resolve(states),
            b is null ? default! : b.Resolve(states),
            t));
    }
}

/// <summary>
/// A color whose value can depend on the current widget states.
/// </summary>
public sealed class WidgetStateColor : WidgetStateProperty<Color>
{
    private readonly Func<IReadOnlySet<WidgetState>, Color> _resolver;

    public WidgetStateColor(Color defaultValue)
        : this(defaultValue, _ => defaultValue)
    {
        IsConstantColor = true;
    }

    public WidgetStateColor(
        Color defaultValue,
        Func<IReadOnlySet<WidgetState>, Color> resolver)
    {
        DefaultValue = defaultValue;
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public Color DefaultValue { get; }

    /// True when this value stands in for a plain <see cref="Color"/> — the implicit conversion and
    /// the single-argument constructor set it. Dart distinguishes the same two cases with
    /// `value is WidgetStateColor`, which C# cannot express because `Color` is a sealed value type.
    public bool IsConstantColor { get; }

    public override Color Resolve(IReadOnlySet<WidgetState> states)
    {
        return _resolver(states);
    }

    public static WidgetStateColor ResolveWith(
        Color defaultValue,
        Func<IReadOnlySet<WidgetState>, Color> resolver)
    {
        return new WidgetStateColor(defaultValue, resolver);
    }

    public new static WidgetStateColor ResolveWith(Func<IReadOnlySet<WidgetState>, Color> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return new WidgetStateColor(resolver(new HashSet<WidgetState>()), resolver);
    }

    public static implicit operator WidgetStateColor(Color color)
    {
        return new WidgetStateColor(color);
    }

    public static implicit operator Color(WidgetStateColor color)
    {
        ArgumentNullException.ThrowIfNull(color);
        return color.DefaultValue;
    }
}

public sealed class WidgetStatePropertyAll<T> : WidgetStateProperty<T>
{
    public WidgetStatePropertyAll(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public override T Resolve(IReadOnlySet<WidgetState> states)
    {
        return Value;
    }
}

internal sealed class WidgetStatePropertyResolver<T> : WidgetStateProperty<T>
{
    private readonly Func<IReadOnlySet<WidgetState>, T> _resolver;

    public WidgetStatePropertyResolver(Func<IReadOnlySet<WidgetState>, T> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public override T Resolve(IReadOnlySet<WidgetState> states)
    {
        return _resolver(states);
    }
}

public delegate Widget RadioBuilder<T>(BuildContext context, RawRadioState<T> state);

public sealed class RawRadio<T> : StatefulWidget
{
    public RawRadio(
        T value,
        WidgetStateProperty<MouseCursor> mouseCursor,
        bool toggleable,
        FocusNode focusNode,
        bool autofocus,
        RadioGroupRegistry<T>? groupRegistry,
        bool enabled,
        RadioBuilder<T> builder,
        Key? key = null) : base(key)
    {
        if (enabled && groupRegistry is null)
        {
            throw new ArgumentException("An enabled RawRadio must have a group registry.", nameof(groupRegistry));
        }

        Value = value;
        MouseCursor = mouseCursor ?? throw new ArgumentNullException(nameof(mouseCursor));
        Toggleable = toggleable;
        FocusNode = focusNode ?? throw new ArgumentNullException(nameof(focusNode));
        Autofocus = autofocus;
        GroupRegistry = groupRegistry;
        Enabled = enabled;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public T Value { get; }

    public WidgetStateProperty<MouseCursor> MouseCursor { get; }

    public bool Toggleable { get; }

    public FocusNode FocusNode { get; }

    public bool Autofocus { get; }

    public RadioGroupRegistry<T>? GroupRegistry { get; }

    public bool Enabled { get; }

    public RadioBuilder<T> Builder { get; }

    public override State CreateState()
    {
        return new RawRadioState<T>();
    }
}

public sealed class RawRadioState<T> : ToggleableState, RadioClient<T>
{
    private RadioGroupRegistry<T>? _registry;

    private RawRadio<T> CurrentWidget => (RawRadio<T>)StateWidget;

    protected override bool IsInteractive => CurrentWidget.Enabled && _registry is not null;

    protected override bool IsValueSelected => Selected;

    public IReadOnlySet<WidgetState> States => CurrentWidgetStates;

    public new double Position => base.Position.Value;

    public new double Reaction => base.Reaction.Value;

    public double HoverFade => ReactionHoverFade.Value;

    public double FocusFade => ReactionFocusFade.Value;

    public Point? PressPosition => DownPosition;

    internal Animation<double> PositionAnimation => base.Position;

    internal Animation<double> ReactionAnimation => base.Reaction;

    internal Animation<double> ReactionHoverFadeAnimation => ReactionHoverFade;

    internal Animation<double> ReactionFocusFadeAnimation => ReactionFocusFade;

    public bool Selected => EqualityComparer<T?>.Default.Equals(
        CurrentWidget.Value,
        _registry is null ? default : _registry.GroupValue);

    public bool Hovered => States.Contains(WidgetState.Hovered);

    public bool Focused => States.Contains(WidgetState.Focused);

    public bool Pressed => States.Contains(WidgetState.Pressed);

    public bool Tristate => CurrentWidget.Toggleable;

    public T RadioValue => CurrentWidget.Value;

    public bool Enabled => CurrentWidget.Enabled;

    public FocusNode FocusNode => CurrentWidget.FocusNode;

    public override void InitState()
    {
        SetRegistry(CurrentWidget.GroupRegistry);
        base.InitState();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldRadio = (RawRadio<T>)oldWidget;
        SetRegistry(CurrentWidget.GroupRegistry);
        base.DidUpdateWidget(oldWidget);
        AnimateToValue();
    }

    public override Widget Build(BuildContext context)
    {
        Widget result = CurrentWidget.Builder(context, this);
        MouseCursor mouseCursor = CurrentWidget.MouseCursor.Resolve(CurrentWidgetStates);
        result = BuildToggleableChild(
            child: result,
            mouseCursor: mouseCursor,
            onTap: HandleTap,
            focusNode: FocusNode,
            autofocus: CurrentWidget.Autofocus);

        SemanticsFlags flags = SemanticsFlags.IsInMutuallyExclusiveGroup;
        if (IsInteractive)
        {
            flags |= SemanticsFlags.IsEnabled;
        }

        bool applePlatform = PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS;
        string? hint = applePlatform && !Selected
            ? Localizations.MaybeOf<WidgetsLocalizations>(context)?.RadioButtonUnselectedLabel
            : null;
        return new Semantics(
            child: result,
            flags: flags,
            hint: hint,
            onTap: IsInteractive ? HandleTap : null,
            @checked: Selected,
            selected: applePlatform ? Selected : null);
    }

    public override void Dispose()
    {
        SetRegistry(null);
        base.Dispose();
    }

    public void AnimateToValue()
    {
        AnimateToValue(Selected, CurrentWidget.Toggleable);
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

    private void HandleTap()
    {
        if (!IsInteractive || _registry is null)
        {
            return;
        }

        if (!Selected)
        {
            _registry.OnChanged(CurrentWidget.Value);
            return;
        }

        if (CurrentWidget.Toggleable)
        {
            _registry.OnChanged(default);
        }
        SemanticsService.SendEvent(
            new TapSemanticEvent(Context.FindRenderObject()?.SemanticsNodeId));
    }
}
