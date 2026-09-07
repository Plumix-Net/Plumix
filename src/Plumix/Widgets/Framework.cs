using System.Runtime.CompilerServices;
using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

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

    public override string ToString()
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
/// Reparenting an [Element] using a global key is relatively expensive, as
/// this operation will trigger a call to [State.deactivate] on the associated
/// [State] and all of its descendants; then force all widgets that depends
/// on an [InheritedWidget] to rebuild.
///
/// If you don't need any of the features listed above, consider using a [Key],
/// [ValueKey], [ObjectKey], or [UniqueKey] instead.
///
/// You cannot simultaneously include two widgets in the tree with the same
/// global key. Attempting to do so will assert at runtime.
///
/// ## Pitfalls
///
/// GlobalKeys should not be re-created on every build. They should usually be
/// long-lived objects owned by a [State] object, for example.
///
/// Creating a new GlobalKey on every build will throw away the state of the
/// subtree associated with the old key and create a new fresh subtree for the
/// new key. Besides harming performance, this can also cause unexpected
/// behavior in widgets in the subtree. For example, a [GestureDetector] in the
/// subtree will be unable to track ongoing gestures since it will be recreated
/// on each build.
///
/// Instead, a good practice is to let a State object own the GlobalKey, and
/// instantiate it outside the build method, such as in [State.initState].
///
/// See also:
///
///  * The discussion at [Widget.key] for more information about how widgets use
/// keys.
/// </summary>
public abstract record GlobalKey : Key
{
    private sealed class CurrentElementHolder(Element element)
    {
        public Element Element { get; } = element;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GlobalKey, CurrentElementHolder>
        CurrentElements = new();

    internal Element? CurrentElement => CurrentElements.TryGetValue(this, out var holder) ? holder.Element : null;

    public BuildContext? CurrentContext => CurrentElement;
    public Widget? CurrentWidget => CurrentElement?.Widget;

    internal void AttachElement(Element element)
    {
        CurrentElements.Remove(this);
        CurrentElements.Add(this, new CurrentElementHolder(element));
    }

    internal void DetachElement(Element element)
    {
        if (CurrentElements.TryGetValue(this, out var current) && ReferenceEquals(current.Element, element))
            CurrentElements.Remove(this);
    }
}

public abstract record GlobalKey<T> : GlobalKey where T : State
{
    public T? CurrentState => (CurrentElement as StatefulElement)?.State as T;

    //   /// Creates a [LabeledGlobalKey], which is a [GlobalKey] with a label used for
//   /// debugging.
//   ///
//   /// The label is purely for debugging and not used for comparing the identity
//   /// of the key.
//   factory GlobalKey({String? debugLabel}) => LabeledGlobalKey<T>(debugLabel);
//
//   /// Creates a global key without a label.
//   ///
//   /// Used by subclasses because the factory constructor shadows the implicit
//   /// constructor.
//   const GlobalKey.constructor() : super.empty();
//
//   Element? get _currentElement => WidgetsBinding.instance.buildOwner!._globalKeyRegistry[this];
//
//   /// The build context in which the widget with this key builds.
//   ///
//   /// The current context is null if there is no widget in the tree that matches
//   /// this global key.
//   BuildContext? get currentContext => _currentElement;
//
//   /// The widget in the tree that currently has this global key.
//   ///
//   /// The current widget is null if there is no widget in the tree that matches
//   /// this global key.
//   Widget? get currentWidget => _currentElement?.widget;
//
//   /// The [State] for the widget in the tree that currently has this global key.
//   ///
//   /// The current state is null if (1) there is no widget in the tree that
//   /// matches this global key, (2) that widget is not a [StatefulWidget], or the
//   /// associated [State] object is not a subtype of `T`.
//   T? get currentState => switch (_currentElement) {
//     StatefulElement(:final T state) => state,
//     _ => null,
//   };
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
    /// Ports Dart's <c>LabeledGlobalKey.toString</c>: <c>[GlobalKey#hash label]</c> for the class
    /// itself, <c>[describeIdentity(this) label]</c> for a subclass. The record-generated printer
    /// would render the debug label as a property bag instead.
    /// </summary>
    public override string ToString()
    {
        string label = DebugLabel is null ? string.Empty : $" {DebugLabel}";
        return GetType() == typeof(LabeledGlobalKey<T>)
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
///
/// If the object is not private, then it is possible that collisions will occur
/// where independent widgets will reuse the same object as their
/// [GlobalObjectKey] value in a different part of the tree, leading to a global
/// key conflict. To avoid this problem, create a private [GlobalObjectKey]
/// subclass, as in:
///
/// ```dart
/// class _MyKey extends GlobalObjectKey {
///   const _MyKey(super.value);
/// }
/// ```
///
/// Since the [runtimeType] of the key is part of its identity, this will
/// prevent clashes with other [GlobalObjectKey]s even if they have the same
/// value.
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
    public override string ToString()
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
