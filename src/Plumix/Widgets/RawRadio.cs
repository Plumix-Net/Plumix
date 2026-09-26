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

/// <summary>
/// The interface side of Dart's <c>WidgetStateProperty&lt;T&gt;</c>.
/// </summary>
/// <remarks>
/// C#-only shape: Dart's value types that resolve per state (<c>WidgetStateColor extends Color
/// implements WidgetStateProperty&lt;Color&gt;</c>) implement the property as an interface while
/// extending the value's own class. C# has single inheritance, so <see cref="WidgetStateProperty{T}"/>
/// is the base class for ordinary properties and this covariant interface is what
/// <see cref="WidgetStateProperty{T}.ResolveAs"/> and the <c>is</c> tests check.
/// </remarks>
public interface IWidgetStateProperty<out T>
{
    /// <summary>Returns a value of type <typeparamref name="T"/> that depends on <paramref name="states"/>.</summary>
    T Resolve(IReadOnlySet<WidgetState> states);
}

public abstract class WidgetStateProperty<T> : IWidgetStateProperty<T>
{
    public abstract T Resolve(IReadOnlySet<WidgetState> states);

    /// <summary>
    /// Resolves the value for the given set of states if <paramref name="value"/> is a
    /// <see cref="IWidgetStateProperty{T}"/>, otherwise returns the value itself.
    /// </summary>
    public static T ResolveAs(object? value, IReadOnlySet<WidgetState> states)
    {
        return value is IWidgetStateProperty<T> property
            ? property.Resolve(states)
            : (T)value!;
    }

    public static WidgetStateProperty<T> All(T value)
    {
        return new WidgetStatePropertyAll<T>(value);
    }

    public static WidgetStateProperty<T> ResolveWith(Func<IReadOnlySet<WidgetState>, T> resolver)
    {
        return new WidgetStatePropertyResolver<T>(resolver);
    }

    /// <summary>
    /// Resolves by returning the value of the first entry whose constraint the states satisfy.
    /// </summary>
    /// <remarks>
    /// Ports Dart's <c>WidgetStateProperty.fromMap</c>. Dart keys an insertion-ordered <c>Map</c>;
    /// C# dictionaries carry no order guarantee, so the entries are passed as an ordered list.
    /// </remarks>
    public static WidgetStateProperty<T> FromMap(
        IReadOnlyList<KeyValuePair<WidgetStatesConstraint, T>> map)
    {
        return new WidgetStateMapper<T>(map);
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
/// Defines a <see cref="Color"/> that is also a <see cref="IWidgetStateProperty{T}"/>.
/// </summary>
/// <remarks>
/// Like Dart's <c>WidgetStateColor extends Color implements WidgetStateProperty&lt;Color&gt;</c>,
/// it can be stored in any <see cref="Color"/> slot; the widgets that document support resolve it
/// with <see cref="WidgetStateProperty{T}.ResolveAs"/>. Its own components are the color it resolves
/// to with no states.
/// </remarks>
public abstract class WidgetStateColor : Color, IWidgetStateProperty<Color>
{
    /// <summary>
    /// Abstract const constructor, for subclasses; <paramref name="defaultValue"/> is the ARGB value.
    /// </summary>
    protected WidgetStateColor(uint defaultValue)
        : base(defaultValue)
    {
    }

    /// <summary>
    /// Creates a <see cref="WidgetStateColor"/> from a callback that resolves the color for a set of
    /// states.
    /// </summary>
    public static WidgetStateColor ResolveWith(Func<IReadOnlySet<WidgetState>, Color> callback) =>
        new ResolvingWidgetStateColor(callback);

    /// <summary>
    /// Creates a <see cref="WidgetStateColor"/> from a map of state constraints to colors.
    /// </summary>
    /// <remarks>Dart's insertion-ordered map is an ordered list of entries, as in
    /// <see cref="WidgetStateProperty{T}.FromMap"/>.</remarks>
    public static WidgetStateColor FromMap(IReadOnlyList<KeyValuePair<WidgetStatesConstraint, Color>> map) =>
        new WidgetStateColorMapper(map);

    /// <summary>Returns a <see cref="Color"/> that depends on <paramref name="states"/>.</summary>
    public abstract Color Resolve(IReadOnlySet<WidgetState> states);

    /// <summary>A constant whose value is transparent for all states.</summary>
    public static WidgetStateColor Transparent { get; } = new WidgetStateColorTransparent();
}

// Dart's `_WidgetStateColor`.
internal sealed class ResolvingWidgetStateColor : WidgetStateColor
{
    private static readonly IReadOnlySet<WidgetState> DefaultStates = new HashSet<WidgetState>();

    private readonly Func<IReadOnlySet<WidgetState>, Color> _resolve;

    public ResolvingWidgetStateColor(Func<IReadOnlySet<WidgetState>, Color> resolve)
        : base((resolve ?? throw new ArgumentNullException(nameof(resolve)))(DefaultStates).Value)
    {
        _resolve = resolve;
    }

    public override Color Resolve(IReadOnlySet<WidgetState> states) => _resolve(states);
}

// Dart's `_WidgetStateColorTransparent`.
internal sealed class WidgetStateColorTransparent : WidgetStateColor
{
    public WidgetStateColorTransparent()
        : base(0x00000000)
    {
    }

    public override Color Resolve(IReadOnlySet<WidgetState> states) => new(0x00000000);
}

// Dart's `_WidgetStateColorMapper extends WidgetStateMapper<Color> implements WidgetStateColor`. C#
// cannot inherit both, so it derives from WidgetStateColor and delegates to a WidgetStateMapper; like
// Dart's `noSuchMethod`, every Color member throws.
internal sealed class WidgetStateColorMapper : WidgetStateColor
{
    private readonly WidgetStateMapper<Color> _mapper;

    public WidgetStateColorMapper(IReadOnlyList<KeyValuePair<WidgetStatesConstraint, Color>> map)
        : base(0)
    {
        _mapper = new WidgetStateMapper<Color>(map);
    }

    public override Color Resolve(IReadOnlySet<WidgetState> states) => _mapper.Resolve(states);

    public override double A => throw NoSuchMember(nameof(A));

    public override double R => throw NoSuchMember(nameof(R));

    public override double G => throw NoSuchMember(nameof(G));

    public override double B => throw NoSuchMember(nameof(B));

    public override ColorSpace ColorSpace => throw NoSuchMember(nameof(ColorSpace));

    public override uint Value => throw NoSuchMember(nameof(Value));

    public override uint ToARGB32() => throw NoSuchMember(nameof(ToARGB32));

    public override bool Equals(object? obj) => obj is WidgetStateColorMapper other && other._mapper.Equals(_mapper);

    public override int GetHashCode() => _mapper.GetHashCode();

    public override string ToString() => _mapper.ToString();

    private FlutterError NoSuchMember(string memberName) =>
        new(
            $"There was an attempt to access the \"{memberName}\" field of a WidgetStateMapper<Color> object.\n"
            + $"{this}\n"
            + "WidgetStateProperty objects should only be used in places that document their support.\n"
            + "Double-check whether the map was used in a place that documents support for "
            + "WidgetStateProperty objects. If so, please file a bug report. (The https://pub.dev/ page "
            + "for a package contains a link to \"View/report issues\".)");
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

    // Dart's `WidgetStatePropertyAll` compares by runtime type and value, so two independently
    // constructed `WidgetStatePropertyAll(3.0)` are equal — theme equality depends on it.
    public override bool Equals(object? obj)
    {
        return obj is WidgetStatePropertyAll<T> other
               && other.GetType() == GetType()
               && EqualityComparer<T>.Default.Equals(other.Value, Value);
    }

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    /// <summary>Dart's <c>WidgetStatePropertyAll.toString</c>.</summary>
    public override string ToString() => Value is double number
        ? $"WidgetStatePropertyAll({DoubleProperty.FormatDouble(number)})"
        : $"WidgetStatePropertyAll({Diagnostics.DescribeValue(Value)})";
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

internal sealed class WidgetStateMapper<T> : WidgetStateProperty<T>
{
    private readonly IReadOnlyList<KeyValuePair<WidgetStatesConstraint, T>> _map;

    public WidgetStateMapper(IReadOnlyList<KeyValuePair<WidgetStatesConstraint, T>> map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
    }

    public override T Resolve(IReadOnlySet<WidgetState> states)
    {
        for (int index = 0; index < _map.Count; index++)
        {
            if (_map[index].Key.IsSatisfiedBy(states))
            {
                return _map[index].Value;
            }
        }

        if (default(T) is null)
        {
            return default!;
        }

        throw new ArgumentException(
            $"The current set of widget states ({string.Join(", ", states)}) is not supported by this map.");
    }

    // Dart's `==`: another mapper with an equal map (`mapEquals`, order-insensitive).
    public override bool Equals(object? obj)
    {
        if (obj is not WidgetStateMapper<T> other || other._map.Count != _map.Count)
        {
            return false;
        }

        foreach (KeyValuePair<WidgetStatesConstraint, T> entry in _map)
        {
            bool matched = false;
            foreach (KeyValuePair<WidgetStatesConstraint, T> candidate in other._map)
            {
                if (candidate.Key.Equals(entry.Key))
                {
                    matched = EqualityComparer<T>.Default.Equals(candidate.Value, entry.Value);
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    // Dart's `MapEquality().hash(_map)`: order-insensitive over the entries.
    public override int GetHashCode()
    {
        int hash = 0;
        foreach (KeyValuePair<WidgetStatesConstraint, T> entry in _map)
        {
            hash ^= HashCode.Combine(entry.Key, entry.Value);
        }

        return hash;
    }

    public override string ToString() =>
        $"WidgetStateMapper<{Foundation.Diagnostics.DescribeType(typeof(T))}>({_map.Count} entries)";
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

public sealed class RawRadioState<T> : ToggleableState<RawRadio<T>>, RadioClient<T>
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

    internal new Animation<double> PositionAnimation => base.Position;

    internal Animation<double> ReactionAnimation => base.Reaction;

    internal Animation<double> ReactionHoverFadeAnimation => ReactionHoverFade;

    internal Animation<double> ReactionFocusFadeAnimation => ReactionFocusFade;

    public bool Selected => EqualityComparer<T?>.Default.Equals(
        CurrentWidget.Value,
        _registry is null ? default : _registry.GroupValue);

    public bool Hovered => States.Contains(WidgetState.Hovered);

    public bool Focused => States.Contains(WidgetState.Focused);

    public bool Pressed => DownPosition.HasValue;

    public bool Tristate => CurrentWidget.Toggleable;

    public T RadioValue => CurrentWidget.Value;

    public bool Enabled => CurrentWidget.Enabled;

    public FocusNode FocusNode => CurrentWidget.FocusNode;

    public override void InitState()
    {
        SetRegistry(CurrentWidget.GroupRegistry);
        base.InitState();
    }

    public override void DidUpdateWidget(RawRadio<T> oldWidget)
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

        bool applePlatform = PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS;
        string? hint = applePlatform && !Selected
            ? Localizations.MaybeOf<WidgetsLocalizations>(context)?.RadioButtonUnselectedLabel
            : null;
        return new Semantics(
            child: result,
            inMutuallyExclusiveGroup: true,
            enabled: IsInteractive ? true : null,
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
        Context.FindRenderObject()?.SendSemanticsEvent(new TapSemanticEvent());
    }
}
