using System.Collections.Generic;

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
// already does for Avalonia's sealed `Color`. The `BorderSide` equivalent lives in
// `Plumix.Material` (`CheckboxTheme.cs`), where it was first needed.

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
