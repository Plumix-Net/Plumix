using Avalonia;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/dialog.dart

namespace Plumix.Widgets;

/// <summary>Signature for a function that creates a dialog route.</summary>
/// <remarks>Dart's <c>RawDialogRouteBuilder&lt;T&gt;</c>.</remarks>
public delegate Route RawDialogRouteBuilder(BuildContext context, WidgetBuilder builder);

/// <summary>The top-level functions of Dart's <c>widgets/dialog.dart</c>.</summary>
public static class RawDialogs
{
    /// <summary>
    /// Displays a dialog above the current contents of the app: in a separate dialog window when the
    /// windowing API is enabled and a <see cref="WindowRegistry"/> is available, in a route otherwise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dart's <c>showRawDialog</c>. With windowing, the dialog is a <see cref="DialogWindowController"/>
    /// window parented to the context's <see cref="WindowScope"/> controller, sized to its content or, for
    /// a <paramref name="fullscreenDialog"/>, to the parent's content size; <paramref name="routeBuilder"/>
    /// is ignored there. If the platform does not support windowing, the error is reported and the dialog
    /// falls back to a route.
    /// </para>
    /// <para>
    /// The fallback route is <paramref name="routeBuilder"/>'s, or a <see cref="RawDialogRoute{T}"/> whose
    /// page is <paramref name="builder"/>. The task completes with the value the dialog was popped with.
    /// </para>
    /// </remarks>
    public static Task<T?> ShowRawDialog<T>(
        BuildContext context,
        WidgetBuilder builder,
        RawDialogRouteBuilder? routeBuilder = null,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null,
        bool fullscreenDialog = false)
    {
        WidgetsDebug.DebugCheckHasWidgetsLocalizations(context);

        NavigatorState navigator = Navigator.Of(context, rootNavigator: useRootNavigator);

        WindowRegistry? windowRegistry = WindowRegistry.MaybeOf(context);
        if (windowRegistry is not null && Features.IsWindowingEnabled)
        {
            try
            {
                Size? parentSize = WindowScope.MaybeContentSizeOf(context);
                var windowRoute = new DialogWindowRoute<T>(
                    builder: builder,
                    parentController: WindowScope.MaybeOf(context),
                    context: context,
                    settings: routeSettings,
                    size: fullscreenDialog ? parentSize : null);
                navigator.Push(windowRoute);
                return ResultOf<T>(windowRoute);
            }
            catch (NotSupportedException error)
            {
                // Fallback to normal dialog route if windowing is not supported.
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: error,
                    library: "widgets library",
                    stack: error.StackTrace));
            }
        }

        Route route = routeBuilder?.Invoke(context, builder)
                      ?? new RawDialogRoute<T>(
                          pageBuilder: (pageContext, _, _) => builder(pageContext),
                          settings: routeSettings,
                          fullscreenDialog: fullscreenDialog);

        navigator.Push(route);
        return route is RawDialogRoute<T> rawDialogRoute ? rawDialogRoute.Popped : ResultOf<T>(route);
    }

    /// <summary>
    /// Dart's <c>navigator.push&lt;T&gt;</c> future: the route's pop result as a <typeparamref name="T"/>.
    /// </summary>
    private static async Task<T?> ResultOf<T>(Route route)
    {
        object? result = await route.Popped.ConfigureAwait(true);
        return result is T typed ? typed : default;
    }
}

/// <summary>Dart's private <c>_DialogWindowDelegate</c>: a close request pops the dialog's route.</summary>
internal sealed class DialogWindowDelegate : DialogWindowControllerDelegate
{
    private readonly Route _route;

    public DialogWindowDelegate(Route route)
    {
        _route = route;
    }

    public override void OnWindowCloseRequested(DialogWindowController controller)
    {
        _route.Navigator?.Pop();
    }
}

/// <summary>
/// Dart's private <c>_DialogWindowRoute&lt;T&gt;</c>: a route whose content renders in a dialog window
/// registered with the closest <see cref="WindowRegistry"/>, not in the navigator's overlay.
/// </summary>
internal sealed class DialogWindowRoute<T> : Route
{
    private readonly WindowRegistry? _registry;
    private DialogWindowController? _controller;
    private WindowEntry? _entry;
    private IReadOnlyList<OverlayEntry>? _overlayEntries;

    public DialogWindowRoute(
        WidgetBuilder builder,
        BaseWindowController? parentController,
        BuildContext context,
        RouteSettings? settings = null,
        Size? size = null) : base(settings)
    {
        Builder = builder;
        ParentController = parentController;
        _registry = WindowRegistry.MaybeOf(context);
        _controller = size is { } dialogSize
            ? DialogWindowController.Create(
                parent: parentController,
                title: "Dialog",
                @delegate: new DialogWindowDelegate(this),
                size: dialogSize)
            : DialogWindowController.SizedToContent(
                parent: parentController,
                title: "Dialog",
                @delegate: new DialogWindowDelegate(this));
    }

    public WidgetBuilder Builder { get; }

    public BaseWindowController? ParentController { get; }

    /// <summary>The dialog's window controller, until the route is disposed.</summary>
    internal DialogWindowController? Controller => _controller;

    public override IReadOnlyList<OverlayEntry> OverlayEntries =>
        _overlayEntries ?? throw new InvalidOperationException("Field '_overlayEntries' has not been initialized.");

    protected override void Install()
    {
        base.Install();

        // Create a minimal transparent overlay entry to satisfy Navigator requirements.
        // The actual dialog content is rendered through ViewAnchor, not through this overlay.
        _overlayEntries = [new OverlayEntry(builder: _ => SizedBox.Shrink())];

        NavigatorState? nav = Navigator;
        BuildContext? routeContext = nav?.Context;
        if (routeContext is not null && nav is not null)
        {
            _entry = new WindowEntry(controller: _controller!, builder: Builder);
            _registry?.Register(_entry);
        }
    }

    public override bool DidPop(object? result)
    {
        if (_entry is not null)
        {
            _registry?.Unregister(_entry);
        }

        _controller?.Destroy();
        return base.DidPop(result);
    }

    public override void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
        base.Dispose();
    }
}
