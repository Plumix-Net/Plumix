using System.Collections.Generic;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_state.dart

/// <summary>
/// A condition over the current set of <see cref="WidgetState"/> values, used as the key type of
/// <see cref="WidgetStateProperty{T}.FromMap"/>.
/// </summary>
/// <remarks>
/// Ports Dart's <c>WidgetStatesConstraint</c>. Dart lets the <c>WidgetState</c> enum implement the
/// interface directly and defines <c>&amp;</c>/<c>|</c>/<c>~</c> on it; a C# enum can do neither, so a
/// single state converts implicitly into a <see cref="WidgetStatesConstraint"/> and the operators
/// live here.
/// </remarks>
public abstract class WidgetStatesConstraint
{
    /// <summary>Dart's <c>WidgetState.any</c>: satisfied by every set of states.</summary>
    public static WidgetStatesConstraint Any { get; } = new AnyWidgetStatesConstraint();

    public static implicit operator WidgetStatesConstraint(WidgetState state) =>
        new SingleWidgetStateConstraint(state);

    public static WidgetStatesConstraint operator &(
        WidgetStatesConstraint left,
        WidgetStatesConstraint right) => new WidgetStateOperator(left, right, WidgetStateOperation.And);

    public static WidgetStatesConstraint operator |(
        WidgetStatesConstraint left,
        WidgetStatesConstraint right) => new WidgetStateOperator(left, right, WidgetStateOperation.Or);

    public static WidgetStatesConstraint operator ~(WidgetStatesConstraint value) =>
        new WidgetStateOperator(value, null, WidgetStateOperation.Not);

    /// <summary>Whether <paramref name="states"/> satisfies this constraint.</summary>
    public abstract bool IsSatisfiedBy(IReadOnlySet<WidgetState> states);
}

internal enum WidgetStateOperation
{
    And,
    Or,
    Not,
}

internal sealed class AnyWidgetStatesConstraint : WidgetStatesConstraint
{
    public override bool IsSatisfiedBy(IReadOnlySet<WidgetState> states) => true;
}

internal sealed class SingleWidgetStateConstraint : WidgetStatesConstraint
{
    private readonly WidgetState _state;

    public SingleWidgetStateConstraint(WidgetState state)
    {
        _state = state;
    }

    public override bool IsSatisfiedBy(IReadOnlySet<WidgetState> states)
    {
        ArgumentNullException.ThrowIfNull(states);
        return states.Contains(_state);
    }
}

internal sealed class WidgetStateOperator : WidgetStatesConstraint
{
    private readonly WidgetStatesConstraint _first;
    private readonly WidgetStatesConstraint? _second;
    private readonly WidgetStateOperation _operation;

    public WidgetStateOperator(
        WidgetStatesConstraint first,
        WidgetStatesConstraint? second,
        WidgetStateOperation operation)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second;
        _operation = operation;
    }

    public override bool IsSatisfiedBy(IReadOnlySet<WidgetState> states) => _operation switch
    {
        WidgetStateOperation.And => _first.IsSatisfiedBy(states) && _second!.IsSatisfiedBy(states),
        WidgetStateOperation.Or => _first.IsSatisfiedBy(states) || _second!.IsSatisfiedBy(states),
        _ => !_first.IsSatisfiedBy(states),
    };
}

/// <summary>
/// A <see cref="ValueNotifier{T}"/> over the set of states a widget is in, so several parts of one
/// widget can share and observe the same interaction state.
/// </summary>
/// <remarks>Ports Dart's <c>WidgetStatesController</c>.</remarks>
public class WidgetStatesController : ValueNotifier<IReadOnlySet<WidgetState>>
{
    private readonly HashSet<WidgetState> _states;

    public WidgetStatesController(IEnumerable<WidgetState>? value = null)
        : this(value is null ? [] : [.. value])
    {
    }

    private WidgetStatesController(HashSet<WidgetState> states) : base(states)
    {
        _states = states;
    }

    /// <summary>Adds or removes <paramref name="state"/>, notifying listeners when it changed.</summary>
    public void Update(WidgetState state, bool add)
    {
        bool valueChanged = add ? _states.Add(state) : _states.Remove(state);
        if (valueChanged)
        {
            NotifyListeners();
        }
    }
}
