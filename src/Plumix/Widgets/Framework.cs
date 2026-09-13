using System.Runtime.CompilerServices;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Widgets;

/// <summary>
/// Dart's private <c>_reportException</c> from <c>widgets/framework.dart</c>: builds the
/// <see cref="FlutterErrorDetails"/> for a failure inside the widgets library, reports it through
/// <see cref="FlutterError.ReportError"/>, and hands the details back so the caller can feed them to
/// <see cref="ErrorWidget.Builder"/>.
/// </summary>
internal static class FrameworkErrors
{
    public static FlutterErrorDetails ReportException(
        DiagnosticsNode context,
        Exception exception,
        InformationCollector? informationCollector = null)
    {
        var details = new FlutterErrorDetails(
            exception: exception,
            stack: exception.StackTrace,
            library: "widgets library",
            context: context,
            informationCollector: informationCollector);
        FlutterError.ReportError(details);
        return details;
    }
}

/// <summary>
/// C#-only: Dart's <c>assert(condition)</c> for the framework core. Checked in debug builds only, and
/// failing with an <see cref="AssertionError"/> that carries the optional message.
/// </summary>
internal static class DebugAssertions
{
    public static void Assert(bool condition, string? message = null)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }
}

/// <summary>
/// A key that takes its identity from the object used as its value.
///
/// Used to tie the identity of a widget to the identity of an object used to
/// generate that widget.
///
/// See also:
///
///  * [Key], the base class for all keys.
///  * The discussion at [Widget.key] for more information about how widgets use
///    keys.
/// </summary>
/// <param name="Value"></param>
public record ObjectKey(object? Value) : LocalKey
{
    // Dart compares with `identical(other.value, value)` and hashes `identityHashCode(value)`, so an
    // ObjectKey ties a widget to one *instance*. The record-generated members would instead use the
    // value's own Equals/GetHashCode, which makes two keys over equal-but-distinct objects
    // interchangeable and silently reuses the wrong element.
    public virtual bool Equals(ObjectKey? other)
    {
        return other is not null
            && other.GetType() == GetType()
            && ReferenceEquals(other.Value, Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), RuntimeHelpers.GetHashCode(Value));
    }

    // Sealed so a record subclass inherits Dart's printer instead of getting a synthesized one.
    public sealed override string ToString()
    {
        string identity = Diagnostics.DescribeIdentity(Value);
        return GetType() == typeof(ObjectKey)
            ? $"[{identity}]"
            : $"[{Diagnostics.ObjectRuntimeType(this, "ObjectKey")} {identity}]";
    }
}

/// <summary>
/// A key that is unique across the entire app.
///
/// Global keys uniquely identify elements. Global keys provide access to other
/// objects that are associated with those elements, such as [BuildContext].
/// For [StatefulWidget]s, global keys also provide access to [State].
///
/// Widgets that have global keys reparent their subtrees when they are moved
/// from one location in the tree to another location in the tree. In order to
/// reparent its subtree, a widget must arrive at its new location in the tree
/// in the same animation frame in which it was removed from its old location in
/// the tree.
///
/// You cannot simultaneously include two widgets in the tree with the same
/// global key. Attempting to do so will assert at runtime.
///
/// GlobalKeys should not be re-created on every build. They should usually be
/// long-lived objects owned by a [State] object, for example.
/// </summary>
public abstract record GlobalKey : Key
{
    /// <summary>
    /// Dart's <c>GlobalKey._currentElement</c>: the registry is keyed by <c>==</c>, so an equal key
    /// instance finds the element too.
    /// </summary>
    internal Element? CurrentElement => BuildOwner.LookupGlobalKey(this);

    /// <summary>The build context in which the widget with this key builds.</summary>
    public BuildContext? CurrentContext => CurrentElement;

    /// <summary>The widget in the tree that currently has this global key.</summary>
    public Widget? CurrentWidget => CurrentElement?.Widget;

    /// <summary>
    /// A key's identity is not its current element: the record printer must not walk
    /// <see cref="CurrentContext"/>, whose description prints this key again.
    /// </summary>
    protected override bool PrintMembers(System.Text.StringBuilder builder) => false;
}

/// <summary>A <see cref="GlobalKey"/> whose <see cref="CurrentState"/> is typed.</summary>
public abstract record GlobalKey<T> : GlobalKey where T : State
{
    /// <summary>
    /// The <see cref="State"/> for the widget in the tree that currently has this global key, or null
    /// when there is none or it is not a <typeparamref name="T"/>.
    /// </summary>
    public T? CurrentState => CurrentElement is StatefulElement { State: T state } ? state : null;
}

/// <summary>
/// A global key with a debugging label.
///
/// The debug label is useful for documentation and for debugging. The label
/// does not affect the key's identity.
/// </summary>
/// <param name="DebugLabel"></param>
/// <typeparam name="T"></typeparam>
public record LabeledGlobalKey<T>(string? DebugLabel) : GlobalKey<T> where T : State
{
    // Dart's LabeledGlobalKey is a class and inherits identity equality, so two keys built with the same
    // label stay distinct. The generated record equality would instead make them interchangeable, which
    // collides as soon as two instances of the same widget (two Scaffolds, two Drawers, ...) are mounted
    // in one tree.
    public virtual bool Equals(LabeledGlobalKey<T>? other) => ReferenceEquals(this, other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    /// <summary>
    /// Ports Dart's <c>LabeledGlobalKey.toString</c>: <c>[GlobalKey#hash label]</c> when the runtime
    /// type is <c>LabeledGlobalKey&lt;State&lt;StatefulWidget&gt;&gt;</c> (what <c>GlobalKey()</c>
    /// creates), <c>[describeIdentity(this) label]</c> for any other type argument or subclass.
    /// </summary>
    public sealed override string ToString()
    {
        string label = DebugLabel is null ? string.Empty : $" {DebugLabel}";
        return GetType() == typeof(LabeledGlobalKey<State>)
            ? $"[GlobalKey#{Diagnostics.ShortHash(this)}{label}]"
            : $"[{Diagnostics.DescribeIdentity(this)}{label}]";
    }
}

/// <summary>
/// A global key that takes its identity from the object used as its value.
///
/// Used to tie the identity of a widget to the identity of an object used to
/// generate that widget.
///
/// Any [GlobalObjectKey] created for the same object will match.
/// </summary>
/// <param name="Value"></param>
/// <typeparam name="T"></typeparam>
public record GlobalObjectKey<T>(object Value) : GlobalKey<T> where T : State
{
    // Dart compares with `identical(other.value, value)` and hashes `identityHashCode(value)`, so the
    // key follows one instance. The record-generated members would defer to the value's own
    // Equals/GetHashCode and make two keys over equal-but-distinct objects collide.
    public virtual bool Equals(GlobalObjectKey<T>? other)
    {
        return other is not null
            && other.GetType() == GetType()
            && ReferenceEquals(other.Value, Value);
    }

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(Value);

    /// <summary>
    /// Ports Dart's `GlobalObjectKey.toString`: the key's own type plus the identity of its value,
    /// never the value's own `toString`. The record-generated printer would render `Value` in full,
    /// which recurses without bound whenever the value is a <see cref="Foundation.IDiagnosticable"/>
    /// that dumps the widget tree the key is mounted in.
    /// </summary>
    public sealed override string ToString()
    {
        string selfType = Foundation.Diagnostics.ObjectRuntimeType(this, "GlobalObjectKey");
        const string suffix = "<State>";
        if (selfType.EndsWith(suffix, StringComparison.Ordinal))
        {
            selfType = selfType[..^suffix.Length];
        }

        return $"[{selfType} {Foundation.Diagnostics.DescribeIdentity(Value)}]";
    }
}
