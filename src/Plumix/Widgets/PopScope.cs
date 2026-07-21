using Plumix.Foundation;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/pop_scope.dart
// flutter/packages/flutter/lib/src/widgets/navigator_pop_handler.dart

namespace Plumix.Widgets;

public sealed class PopScope<T> : StatefulWidget
{
    public PopScope(
        Widget child,
        bool canPop = true,
        PopInvokedWithResultCallback<T>? onPopInvokedWithResult = null,
        PopInvokedCallback? onPopInvoked = null,
        Key? key = null) : base(key)
    {
        if (onPopInvokedWithResult != null && onPopInvoked != null)
        {
            throw new ArgumentException(
                "onPopInvoked and onPopInvokedWithResult cannot both be provided.",
                nameof(onPopInvoked));
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
        CanPop = canPop;
        OnPopInvokedWithResult = onPopInvokedWithResult;
#pragma warning disable CS0618
        OnPopInvoked = onPopInvoked;
#pragma warning restore CS0618
    }

    public Widget Child { get; }

    public bool CanPop { get; }

    public PopInvokedWithResultCallback<T>? OnPopInvokedWithResult { get; }

    [Obsolete("Use OnPopInvokedWithResult instead.")]
    public PopInvokedCallback? OnPopInvoked { get; }

    internal void CallPopInvoked(bool didPop, T? result)
    {
        if (OnPopInvokedWithResult != null)
        {
            OnPopInvokedWithResult(didPop, result);
            return;
        }

#pragma warning disable CS0618
        OnPopInvoked?.Invoke(didPop);
#pragma warning restore CS0618
    }

    public override State CreateState()
    {
        return new PopScopeState<T>();
    }
}

internal sealed class PopScopeState<T> : State, PopEntry
{
    private ModalRoute? _route;
    private ValueNotifier<bool> _canPopNotifier = null!;

    private PopScope<T> CurrentWidget => (PopScope<T>)StateWidget;

    public IValueListenable<bool> CanPopNotifier => _canPopNotifier;

    public override void InitState()
    {
        base.InitState();
        _canPopNotifier = new ValueNotifier<bool>(CurrentWidget.CanPop);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        var nextRoute = ModalRoute.MaybeOf(Context);
        if (ReferenceEquals(nextRoute, _route))
        {
            return;
        }

        _route?.UnregisterPopEntry(this);
        _route = nextRoute;
        _route?.RegisterPopEntry(this);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        _canPopNotifier.Value = CurrentWidget.CanPop;
    }

    public override void Dispose()
    {
        _route?.UnregisterPopEntry(this);
        _route = null;
        _canPopNotifier.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return CurrentWidget.Child;
    }

    public void OnPopInvokedWithResult(bool didPop, object? result)
    {
        T? typedResult = result switch
        {
            null => default,
            T value => value,
            _ => throw new InvalidCastException(
                $"The pop result of type '{result.GetType().Name}' cannot be passed to PopScope<{typeof(T).Name}>."),
        };
        CurrentWidget.CallPopInvoked(didPop, typedResult);
    }
}

public sealed class NavigatorPopHandler<T> : StatefulWidget
{
    public NavigatorPopHandler(
        Widget child,
        Action? onPop = null,
        PopResultCallback<T>? onPopWithResult = null,
        bool enabled = true,
        Key? key = null) : base(key)
    {
        if (onPop != null && onPopWithResult != null)
        {
            throw new ArgumentException(
                "onPop and onPopWithResult cannot both be provided.",
                nameof(onPop));
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
#pragma warning disable CS0618
        OnPop = onPop;
#pragma warning restore CS0618
        OnPopWithResult = onPopWithResult;
        Enabled = enabled;
    }

    public Widget Child { get; }

    public bool Enabled { get; }

    [Obsolete("Use OnPopWithResult instead.")]
    public Action? OnPop { get; }

    public PopResultCallback<T>? OnPopWithResult { get; }

    public override State CreateState()
    {
        return new NavigatorPopHandlerState<T>();
    }
}

internal sealed class NavigatorPopHandlerState<T> : State
{
    private bool _canPop = true;

    private NavigatorPopHandler<T> CurrentWidget => (NavigatorPopHandler<T>)StateWidget;

    public override Widget Build(BuildContext context)
    {
        return new PopScope<T>(
            canPop: !CurrentWidget.Enabled || _canPop,
            onPopInvokedWithResult: HandlePopInvoked,
            child: new NotificationListener<NavigationNotification>(
                onNotification: HandleNavigationNotification,
                child: CurrentWidget.Child));
    }

    private void HandlePopInvoked(bool didPop, T? result)
    {
        if (didPop)
        {
            return;
        }

#pragma warning disable CS0618
        CurrentWidget.OnPop?.Invoke();
#pragma warning restore CS0618
        CurrentWidget.OnPopWithResult?.Invoke(result);
    }

    private bool HandleNavigationNotification(NavigationNotification notification)
    {
        bool nextCanPop = !notification.CanHandlePop;
        if (nextCanPop != _canPop)
        {
            SetState(() => _canPop = nextCanPop);
        }

        return false;
    }
}

public sealed class NavigationNotification : Notification
{
    public NavigationNotification(bool canHandlePop)
    {
        CanHandlePop = canHandlePop;
    }

    public bool CanHandlePop { get; }

    public override string ToString()
    {
        return $"NavigationNotification canHandlePop: {CanHandlePop}";
    }
}
