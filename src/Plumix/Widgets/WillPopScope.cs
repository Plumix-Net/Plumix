// Dart parity source: flutter/packages/flutter/lib/src/widgets/will_pop_scope.dart

using Plumix.Foundation;

namespace Plumix.Widgets;

#pragma warning disable CS0618 // The whole widget is Flutter's deprecated scoped will-pop surface.

/// <summary>
/// Registers a callback to veto attempts by the user to dismiss the enclosing <see cref="ModalRoute"/>.
/// </summary>
/// <remarks>
/// See also:
/// <list type="bullet">
///   <item><see cref="ModalRoute.AddScopedWillPopCallback"/> and
///   <see cref="ModalRoute.RemoveScopedWillPopCallback"/>, which this widget uses to register and
///   unregister <see cref="OnWillPop"/>.</item>
///   <item><see cref="Form"/>, which provides an <c>OnWillPop</c> callback that enables the form to
///   veto a pop initiated by the app's back button.</item>
/// </list>
/// </remarks>
[Obsolete(
    "Use PopScope instead. The Android predictive back feature will not work with WillPopScope. "
    + "Mirrors Flutter's deprecation after v3.12.0-1.0.pre.")]
public sealed class WillPopScope : StatefulWidget
{
    /// <summary>
    /// Creates a widget that registers a callback to veto attempts by the user to dismiss the
    /// enclosing <see cref="ModalRoute"/>.
    /// </summary>
    public WillPopScope(Widget child, WillPopCallback? onWillPop, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnWillPop = onWillPop;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>
    /// Called to veto attempts by the user to dismiss the enclosing <see cref="ModalRoute"/>. When
    /// the callback returns <see langword="false"/> the enclosing route is not popped.
    /// </summary>
    public WillPopCallback? OnWillPop { get; }

    /// <inheritdoc />
    public override State CreateState() => new WillPopScopeState();
}

/// <summary>Flutter's <c>_WillPopScopeState</c>.</summary>
internal sealed class WillPopScopeState : State
{
    private ModalRoute? _route;

    private WillPopScope Current => (WillPopScope)StateWidget;

    /// <inheritdoc />
    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        if (Current.OnWillPop is { } current)
        {
            _route?.RemoveScopedWillPopCallback(current);
        }

        _route = ModalRoute.MaybeOf(Context);
        if (Current.OnWillPop is { } next)
        {
            _route?.AddScopedWillPopCallback(next);
        }
    }

    /// <inheritdoc />
    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (WillPopScope)oldWidget;
        if (Current.OnWillPop != previous.OnWillPop && _route is not null)
        {
            if (previous.OnWillPop is { } removed)
            {
                _route.RemoveScopedWillPopCallback(removed);
            }

            if (Current.OnWillPop is { } added)
            {
                _route.AddScopedWillPopCallback(added);
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (Current.OnWillPop is { } callback)
        {
            _route?.RemoveScopedWillPopCallback(callback);
        }

        base.Dispose();
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => Current.Child;
}

#pragma warning restore CS0618
