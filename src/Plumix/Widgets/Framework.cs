using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

namespace Plumix.Widgets;

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
public record ObjectKey(object? Value) : LocalKey;

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
public abstract record GlobalKey : Key;

public abstract record GlobalKey<T> : GlobalKey where T : State
{
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
public record LabeledGlobalKey<T>(string? DebugLabel) : GlobalKey<T> where T : State;

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
public record GlobalObjectKey<T>(object Value) : GlobalKey<T> where T : State;
