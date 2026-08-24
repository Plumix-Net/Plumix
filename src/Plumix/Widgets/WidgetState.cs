using System.Collections.Generic;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_state.dart
//
// `WidgetState`, `WidgetStateProperty<T>`, `WidgetStateColor` and the resolver/all helpers live in
// `RawRadio.cs` next to the first widget that needed them; this file carries the remaining
// state-resolving value types.
//
// Flutter declares `WidgetStateTextStyle extends TextStyle`, so a state-resolving style can be
// stored in a plain `TextStyle?` field. Plumix's `TextStyle` is a sealed record and cannot be
// subclassed, so this type wraps the style and converts implicitly, exactly as `WidgetStateColor`
// already does for Avalonia's sealed `Color` and `WidgetStateBorderSide` below for the readonly
// record struct `BorderSide`. Material call sites keep their `[Flags] MaterialState` signature
// through extension bridges in `Plumix.Material` (`CheckboxTheme.cs`).

/// A text style whose value can depend on the current widget states.
public sealed class WidgetStateTextStyle : WidgetStateProperty<TextStyle>
{
    private readonly Func<IReadOnlySet<WidgetState>, TextStyle> _resolver;

    public WidgetStateTextStyle(TextStyle defaultValue)
        : this(defaultValue, _ => defaultValue)
    {
        IsConstantTextStyle = true;
    }

    public WidgetStateTextStyle(
        TextStyle defaultValue,
        Func<IReadOnlySet<WidgetState>, TextStyle> resolver)
    {
        DefaultValue = defaultValue ?? throw new ArgumentNullException(nameof(defaultValue));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public TextStyle DefaultValue { get; }

    /// True when this value stands in for a plain <see cref="TextStyle"/> — the implicit conversion
    /// and the single-argument constructor set it. Dart distinguishes the same two cases with
    /// `value is WidgetStateTextStyle`, which C# cannot express because `TextStyle` is a sealed record.
    public bool IsConstantTextStyle { get; }

    public override TextStyle Resolve(IReadOnlySet<WidgetState> states) => _resolver(states);

    public static WidgetStateTextStyle ResolveWith(
        TextStyle defaultValue,
        Func<IReadOnlySet<WidgetState>, TextStyle> resolver) => new(defaultValue, resolver);

    public new static WidgetStateTextStyle ResolveWith(Func<IReadOnlySet<WidgetState>, TextStyle> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return new WidgetStateTextStyle(resolver(new HashSet<WidgetState>()), resolver);
    }

    public static implicit operator WidgetStateTextStyle?(TextStyle? style) =>
        style is null ? null : new WidgetStateTextStyle(style);

    public static implicit operator TextStyle?(WidgetStateTextStyle? style) => style?.DefaultValue;
}

/// A border side whose value can depend on the current widget states. Flutter declares
/// `WidgetStateBorderSide extends BorderSide`, so a state-resolving side can be stored in a plain
/// `BorderSide?` field; Plumix's `BorderSide` is a readonly record struct, so this wrapper stands in
/// for it. A plain side converts implicitly and keeps Flutter's backwards-compatible rule of only
/// rendering when the widget is not selected; <see cref="IsStateful"/> mirrors Dart's
/// `side is WidgetStateBorderSide` check.
public abstract class WidgetStateBorderSide
{
    public abstract BorderSide? Resolve(IReadOnlySet<WidgetState> states);

    public abstract bool IsStateful { get; }

    public static WidgetStateBorderSide All(BorderSide? side)
    {
        return new StatefulBorderSide(_ => side);
    }

    public static WidgetStateBorderSide ResolveWith(Func<IReadOnlySet<WidgetState>, BorderSide?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return new StatefulBorderSide(resolver);
    }

    public static implicit operator WidgetStateBorderSide(BorderSide side)
    {
        return new FixedBorderSide(side);
    }

    public static WidgetStateBorderSide? Lerp(
        WidgetStateBorderSide? a,
        WidgetStateBorderSide? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        BorderSide? start = a?.Resolve(new HashSet<WidgetState>());
        BorderSide? end = b?.Resolve(new HashSet<WidgetState>());
        BorderSide? side = LerpNullable(start, end, t);
        return side.HasValue ? new FixedBorderSide(side.Value) : null;
    }

    // Dart's `BorderSide.lerp` treats a null endpoint as a zero-width transparent side of the
    // other endpoint's color, so a side can fade in or out.
    private static BorderSide? LerpNullable(BorderSide? a, BorderSide? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        BorderSide from = a ?? new BorderSide(
            Avalonia.Media.Color.FromArgb(0, b!.Value.Color.R, b.Value.Color.G, b.Value.Color.B),
            0.0,
            b.Value.Style);
        BorderSide to = b ?? new BorderSide(
            Avalonia.Media.Color.FromArgb(0, a!.Value.Color.R, a.Value.Color.G, a.Value.Color.B),
            0.0,
            a.Value.Style);
        return BorderSide.Lerp(from, to, t);
    }

    private sealed class FixedBorderSide(BorderSide side) : WidgetStateBorderSide
    {
        public override BorderSide? Resolve(IReadOnlySet<WidgetState> states)
        {
            return states.Contains(WidgetState.Selected) ? null : side;
        }

        public override bool IsStateful => false;
    }

    private sealed class StatefulBorderSide(
        Func<IReadOnlySet<WidgetState>, BorderSide?> resolver) : WidgetStateBorderSide
    {
        public override BorderSide? Resolve(IReadOnlySet<WidgetState> states) => resolver(states);

        public override bool IsStateful => true;
    }
}

/// A mouse cursor whose value can depend on the current widget states, as Flutter's
/// `WidgetStateMouseCursor` — a `MouseCursor` that also implements `WidgetStateProperty`.
public abstract record WidgetStateMouseCursor : MouseCursor
{
    public abstract MouseCursor? Resolve(IReadOnlySet<WidgetState> states);

    public static WidgetStateMouseCursor ResolveWith(
        Func<IReadOnlySet<WidgetState>, MouseCursor?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return new ResolverWidgetStateMouseCursor(resolver);
    }

    /// <summary>
    /// Dart's `WidgetStateMouseCursor.clickable` (`widgets/widget_state.dart`): the click cursor
    /// unless the widget is disabled, in which case the basic cursor.
    /// </summary>
    public static WidgetStateMouseCursor Clickable { get; } = ResolveWith(states =>
        states.Contains(WidgetState.Disabled) ? SystemMouseCursors.Basic : SystemMouseCursors.Click);

    /// <summary>
    /// Dart's `WidgetStateMouseCursor.adaptiveClickable` (`widgets/widget_state.dart`): the click
    /// cursor on web only, and the basic cursor when disabled or on any other platform.
    /// </summary>
    public static WidgetStateMouseCursor AdaptiveClickable { get; } = ResolveWith(states =>
        states.Contains(WidgetState.Disabled) || !PlatformDefaults.IsWeb
            ? SystemMouseCursors.Basic
            : SystemMouseCursors.Click);

    private sealed record ResolverWidgetStateMouseCursor(
        Func<IReadOnlySet<WidgetState>, MouseCursor?> Resolver) : WidgetStateMouseCursor
    {
        public override MouseCursor? Resolve(IReadOnlySet<WidgetState> states) => Resolver(states);
    }
}
