using Avalonia;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/_window.dart

namespace Plumix.Widgets;

/// <summary>
/// The composition every window widget shares: Dart builds
/// <c>ListenableBuilder(WindowScope(View(controller.rootView, child)))</c> in each of them.
/// </summary>
internal static class WindowWidgetBuilder
{
    public static Widget Build(BaseWindowController controller, Widget child)
    {
        return new ListenableBuilder(
            listenable: controller,
            builder: (_, _) => new WindowScope(
                controller: controller,
                child: new View(controller.RootView, child)));
    }
}

/// <summary>A widget that creates a regular window.</summary>
/// <remarks>
/// Dart's <c>RegularWindow</c>. The <see cref="Controller"/>'s <see cref="BaseWindowController.RootView"/>
/// hosts <see cref="Child"/>, under a <see cref="WindowScope"/> that exposes the controller. Removing this
/// widget from the tree does not destroy the window; call <see cref="BaseWindowController.Destroy"/>.
/// It must be placed where a view may be: the root of the tree (<c>RunWidget</c>), a
/// <see cref="ViewCollection"/> or a <see cref="ViewAnchor"/>'s view slot.
/// </remarks>
public sealed class RegularWindow : StatelessWidget
{
    /// <summary>Creates a regular window widget.</summary>
    public RegularWindow(RegularWindowController controller, Widget child, Key? key = null) : base(key)
    {
        Windowing.CheckEnabled();
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>Controller for this widget.</summary>
    public RegularWindowController Controller { get; }

    /// <summary>The content rendered into this window.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => WindowWidgetBuilder.Build(Controller, Child);
}

/// <summary>A widget that creates a dialog window.</summary>
/// <remarks>Dart's <c>DialogWindow</c>; see <see cref="RegularWindow"/> for placement and lifetime.</remarks>
public sealed class DialogWindow : StatelessWidget
{
    /// <summary>Creates a dialog window widget.</summary>
    public DialogWindow(DialogWindowController controller, Widget child, Key? key = null) : base(key)
    {
        Windowing.CheckEnabled();
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>Controller for this widget.</summary>
    public DialogWindowController Controller { get; }

    /// <summary>The content rendered into this window.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => WindowWidgetBuilder.Build(Controller, Child);
}

/// <summary>A widget that creates a tooltip window.</summary>
/// <remarks>Dart's <c>TooltipWindow</c>; see <see cref="RegularWindow"/> for placement and lifetime.</remarks>
public sealed class TooltipWindow : StatelessWidget
{
    /// <summary>Creates a tooltip window widget.</summary>
    public TooltipWindow(TooltipWindowController controller, Widget child, Key? key = null) : base(key)
    {
        Windowing.CheckEnabled();
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>Controller for this widget.</summary>
    public TooltipWindowController Controller { get; }

    /// <summary>The content rendered into this window.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => WindowWidgetBuilder.Build(Controller, Child);
}

/// <summary>A widget that creates a popup window.</summary>
/// <remarks>Dart's <c>PopupWindow</c>; see <see cref="RegularWindow"/> for placement and lifetime.</remarks>
public sealed class PopupWindow : StatelessWidget
{
    /// <summary>Creates a popup window widget.</summary>
    public PopupWindow(PopupWindowController controller, Widget child, Key? key = null) : base(key)
    {
        Windowing.CheckEnabled();
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>Controller for this widget.</summary>
    public PopupWindowController Controller { get; }

    /// <summary>The content rendered into this window.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => WindowWidgetBuilder.Build(Controller, Child);
}

/// <summary>A widget that creates a satellite window.</summary>
/// <remarks>Dart's <c>SatelliteWindow</c>; see <see cref="RegularWindow"/> for placement and lifetime.</remarks>
public sealed class SatelliteWindow : StatelessWidget
{
    /// <summary>Creates a satellite window widget.</summary>
    public SatelliteWindow(SatelliteWindowController controller, Widget child, Key? key = null) : base(key)
    {
        Windowing.CheckEnabled();
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>Controller for this widget.</summary>
    public SatelliteWindowController Controller { get; }

    /// <summary>The content rendered into this window.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => WindowWidgetBuilder.Build(Controller, Child);
}

/// <summary>
/// Provides descendants with access to the <see cref="BaseWindowController"/> of the window they render in.
/// </summary>
/// <remarks>
/// Dart's <c>WindowScope</c>, an <c>InheritedModel&lt;_WindowControllerAspect&gt;</c>. A public C# model
/// cannot be generic over a private aspect type, so it is an <c>InheritedModel&lt;object&gt;</c> whose
/// aspects are the private <see cref="WindowControllerAspect"/> values, as <see cref="MediaQuery"/> does.
/// </remarks>
public sealed class WindowScope : InheritedModel<object>
{
    /// <summary>Creates a new <see cref="WindowScope"/>.</summary>
    public WindowScope(BaseWindowController controller, Widget child, Key? key = null) : base(child, key)
    {
        Windowing.CheckEnabled();
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    /// <summary>The controller associated with this window.</summary>
    public BaseWindowController Controller { get; }

    /// <summary>Dart's private <c>_WindowControllerAspect</c>.</summary>
    private enum WindowControllerAspect
    {
        ContentSize,
        Title,
        Activated,
        Maximized,
        Minimized,
        Fullscreen,
    }

    /// <summary>The <see cref="BaseWindowController"/> of the closest <see cref="WindowScope"/>.</summary>
    /// <remarks>Throws when there is no window scope ancestor.</remarks>
    public static BaseWindowController Of(BuildContext context) => OfCore(context);

    /// <summary>
    /// The <see cref="BaseWindowController"/> of the closest <see cref="WindowScope"/>, or
    /// <see langword="null"/>.
    /// </summary>
    public static BaseWindowController? MaybeOf(BuildContext context) => MaybeOfCore(context);

    /// <summary>The window's content size; rebuilds the context only when it changes.</summary>
    public static Size ContentSizeOf(BuildContext context) =>
        OfCore(context, WindowControllerAspect.ContentSize).ContentSize;

    /// <summary>The window's content size, or <see langword="null"/> without a window scope.</summary>
    public static Size? MaybeContentSizeOf(BuildContext context) =>
        MaybeOfCore(context, WindowControllerAspect.ContentSize)?.ContentSize;

    /// <summary>The window's title; empty for tooltip and popup windows.</summary>
    public static string TitleOf(BuildContext context) =>
        TitleOfController(OfCore(context, WindowControllerAspect.Title));

    /// <summary>The window's title, or <see langword="null"/> without a window scope.</summary>
    public static string? MaybeTitleOf(BuildContext context) =>
        MaybeOfCore(context, WindowControllerAspect.Title) is { } controller ? TitleOfController(controller) : null;

    /// <summary>Whether the window is activated; always <see langword="false"/> for tooltips.</summary>
    public static bool IsActivatedOf(BuildContext context) =>
        IsActivatedOfController(OfCore(context, WindowControllerAspect.Activated));

    /// <summary>Whether the window is activated, or <see langword="null"/> without a window scope.</summary>
    public static bool? MaybeIsActivatedOf(BuildContext context) =>
        MaybeOfCore(context, WindowControllerAspect.Activated) is { } controller
            ? IsActivatedOfController(controller)
            : null;

    /// <summary>Whether the window is minimized; only regular and dialog windows can be.</summary>
    public static bool IsMinimizedOf(BuildContext context) =>
        IsMinimizedOfController(OfCore(context, WindowControllerAspect.Minimized));

    /// <summary>Whether the window is minimized, or <see langword="null"/> without a window scope.</summary>
    public static bool? MaybeIsMinimizedOf(BuildContext context) =>
        MaybeOfCore(context, WindowControllerAspect.Minimized) is { } controller
            ? IsMinimizedOfController(controller)
            : null;

    /// <summary>Whether the window is maximized; only regular windows can be.</summary>
    public static bool IsMaximizedOf(BuildContext context) =>
        IsMaximizedOfController(OfCore(context, WindowControllerAspect.Maximized));

    /// <summary>Whether the window is maximized, or <see langword="null"/> without a window scope.</summary>
    public static bool? MaybeIsMaximizedOf(BuildContext context) =>
        MaybeOfCore(context, WindowControllerAspect.Maximized) is { } controller
            ? IsMaximizedOfController(controller)
            : null;

    /// <summary>Whether the window is fullscreen; only regular windows can be.</summary>
    public static bool IsFullscreenOf(BuildContext context) =>
        IsFullscreenOfController(OfCore(context, WindowControllerAspect.Fullscreen));

    /// <summary>Whether the window is fullscreen, or <see langword="null"/> without a window scope.</summary>
    public static bool? MaybeIsFullscreenOf(BuildContext context) =>
        MaybeOfCore(context, WindowControllerAspect.Fullscreen) is { } controller
            ? IsFullscreenOfController(controller)
            : null;

    private static string TitleOfController(BaseWindowController controller)
    {
        return controller switch
        {
            RegularWindowController regular => regular.Title,
            DialogWindowController dialog => dialog.Title,
            TooltipWindowController => string.Empty,
            PopupWindowController => string.Empty,
            SatelliteWindowController satellite => satellite.Title,
            _ => throw new InvalidOperationException($"Unexpected controller {controller}"),
        };
    }

    private static bool IsActivatedOfController(BaseWindowController controller)
    {
        return controller switch
        {
            RegularWindowController regular => regular.IsActivated,
            DialogWindowController dialog => dialog.IsActivated,
            TooltipWindowController => false,
            PopupWindowController popup => popup.IsActivated,
            SatelliteWindowController satellite => satellite.IsActivated,
            _ => throw new InvalidOperationException($"Unexpected controller {controller}"),
        };
    }

    private static bool IsMinimizedOfController(BaseWindowController controller)
    {
        return controller switch
        {
            RegularWindowController regular => regular.IsMinimized,
            DialogWindowController dialog => dialog.IsMinimized,
            _ => false,
        };
    }

    private static bool IsMaximizedOfController(BaseWindowController controller) =>
        controller is RegularWindowController regular && regular.IsMaximized;

    private static bool IsFullscreenOfController(BaseWindowController controller) =>
        controller is RegularWindowController regular && regular.IsFullscreen;

    private static BaseWindowController OfCore(BuildContext context, WindowControllerAspect? aspect = null)
    {
        Windowing.CheckEnabled();
        DebugCheckHasWindowController(context);
        WindowScope? scope = InheritFrom<WindowScope>(context, aspect);
        // Dart's `!` on a missing scope in a release build.
        return scope?.Controller ?? throw new InvalidOperationException("Null check operator used on a null value");
    }

    private static BaseWindowController? MaybeOfCore(BuildContext context, WindowControllerAspect? aspect = null)
    {
        Windowing.CheckEnabled();
        return InheritFrom<WindowScope>(context, aspect)?.Controller;
    }

    /// <summary>Dart's private <c>_debugCheckHasWindowController</c>.</summary>
    private static void DebugCheckHasWindowController(BuildContext context)
    {
        if (!Constants.KDebugMode || context.DependOnInheritedWidgetOfExactType<WindowScope>() is not null)
        {
            return;
        }

        throw new FlutterError(
        [
            new ErrorSummary("No WindowScope found in context."),
            new ErrorDescription(
                $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require a WindowScope widget "
                + "ancestor."),
            context.DescribeWidget("The specific widget that could not find a WindowScope ancestor was"),
            context.DescribeOwnershipChain("The ownership chain for the affected widget is"),
            new ErrorHint(
                "No WindowScope ancestor could be found starting from the context "
                + "that was passed to WindowScope.of(). This can happen because the "
                + "context used is not a descendant of a RegularWindow widget, which introduces "
                + "a WindowScope."),
        ]);
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(Controller, ((WindowScope)oldWidget).Controller);

    protected override bool UpdateShouldNotifyDependent(
        InheritedModel<object> oldWidget,
        IReadOnlySet<object> dependencies)
    {
        BaseWindowController oldController = ((WindowScope)oldWidget).Controller;
        return dependencies.Any(dependency => dependency is WindowControllerAspect aspect && aspect switch
        {
            WindowControllerAspect.ContentSize => Controller.ContentSize != oldController.ContentSize,
            WindowControllerAspect.Title => Controller switch
            {
                RegularWindowController regular => regular.Title != ((RegularWindowController)oldController).Title,
                DialogWindowController dialog => dialog.Title != ((DialogWindowController)oldController).Title,
                SatelliteWindowController satellite =>
                    satellite.Title != ((SatelliteWindowController)oldController).Title,
                _ => false,
            },
            WindowControllerAspect.Activated => Controller switch
            {
                RegularWindowController regular =>
                    regular.IsActivated != ((RegularWindowController)oldController).IsActivated,
                DialogWindowController dialog =>
                    dialog.IsActivated != ((DialogWindowController)oldController).IsActivated,
                PopupWindowController popup =>
                    popup.IsActivated != ((PopupWindowController)oldController).IsActivated,
                SatelliteWindowController satellite =>
                    satellite.IsActivated != ((SatelliteWindowController)oldController).IsActivated,
                _ => false,
            },
            WindowControllerAspect.Maximized => Controller is RegularWindowController regular
                                               && regular.IsMaximized
                                               != ((RegularWindowController)oldController).IsMaximized,
            WindowControllerAspect.Minimized => Controller switch
            {
                RegularWindowController regular =>
                    regular.IsMinimized != ((RegularWindowController)oldController).IsMinimized,
                DialogWindowController dialog =>
                    dialog.IsMinimized != ((DialogWindowController)oldController).IsMinimized,
                _ => false,
            },
            WindowControllerAspect.Fullscreen => Controller is RegularWindowController regular
                                                && regular.IsFullscreen
                                                != ((RegularWindowController)oldController).IsFullscreen,
            _ => false,
        });
    }
}

/// <summary>
/// The registry of windows created through <see cref="WindowEntry"/>s, which a
/// <see cref="WindowManager"/> renders.
/// </summary>
/// <remarks>Dart's <c>WindowRegistry</c>.</remarks>
public sealed class WindowRegistry : ChangeNotifier
{
    private readonly List<WindowEntry> _windows = [];

    /// <summary>Creates an empty registry.</summary>
    public WindowRegistry()
    {
        Windowing.CheckEnabled();
    }

    /// <summary>The registered windows, in registration order (a fresh unmodifiable copy).</summary>
    public IReadOnlyList<WindowEntry> Windows => _windows.ToArray();

    /// <summary>Registers <paramref name="entry"/> and notifies the listeners.</summary>
    public void Register(WindowEntry entry)
    {
        Windowing.CheckEnabled();
        _windows.Add(entry);
        NotifyListeners();
    }

    /// <summary>Unregisters <paramref name="entry"/> and notifies the listeners, even if it was absent.</summary>
    public void Unregister(WindowEntry entry)
    {
        Windowing.CheckEnabled();
        _windows.Remove(entry);
        NotifyListeners();
    }

    /// <summary>The registry of the closest <see cref="WindowManager"/>, or <see langword="null"/>.</summary>
    public static WindowRegistry? MaybeOf(BuildContext context) =>
        context.DependOnInheritedWidgetOfExactType<WindowRegistryScope>()?.Registry;

    /// <summary>The registry of the closest <see cref="WindowManager"/>.</summary>
    public static WindowRegistry Of(BuildContext context)
    {
        WindowRegistry? registry = MaybeOf(context);
        if (Constants.KDebugMode && registry is null)
        {
            throw new FlutterError(
            [
                new ErrorSummary("No WindowRegistry found in context."),
                new ErrorDescription(
                    $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require a WindowRegistry "
                    + "widget ancestor."),
                context.DescribeWidget("The specific widget that could not find a WindowRegistry ancestor was"),
                context.DescribeOwnershipChain("The ownership chain for the affected widget is"),
                new ErrorHint(
                    "No WindowRegistry ancestor could be found starting from the context "
                    + "that was passed to WindowRegistry.of(). This can happen because the "
                    + "context used is not a descendant of a WindowManager widget, which introduces "
                    + "a WindowRegistry."),
            ]);
        }

        return registry ?? throw new InvalidOperationException("Null check operator used on a null value");
    }
}

/// <summary>Dart's private <c>_WindowRegistryScope</c>.</summary>
internal sealed class WindowRegistryScope : InheritedWidget
{
    public WindowRegistryScope(WindowRegistry registry, Widget child) : base(child)
    {
        Windowing.CheckEnabled();
        Registry = registry;
    }

    public WindowRegistry Registry { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(Registry, ((WindowRegistryScope)oldWidget).Registry);
}

/// <summary>A window a <see cref="WindowRegistry"/> holds: its controller and its content builder.</summary>
/// <remarks>Dart's <c>WindowEntry</c>; entries compare by identity.</remarks>
public sealed class WindowEntry
{
    /// <summary>Creates a window entry.</summary>
    public WindowEntry(BaseWindowController controller, WidgetBuilder builder)
    {
        Windowing.CheckEnabled();
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    /// <summary>The window's controller.</summary>
    public BaseWindowController Controller { get; }

    /// <summary>Builds the window's content.</summary>
    public WidgetBuilder Builder { get; }
}

/// <summary>
/// Introduces a <see cref="WindowRegistry"/> and renders every window registered in it.
/// </summary>
/// <remarks>
/// Dart's <c>WindowManager</c>, which <see cref="WidgetsApp"/> inserts when
/// <see cref="Features.IsWindowingEnabled"/>. Inside a view the registered windows are the view slot of a
/// <see cref="ViewAnchor"/> over <see cref="Child"/>; outside any view they form a
/// <see cref="ViewCollection"/> and <see cref="Child"/> is not rendered.
/// </remarks>
public sealed class WindowManager : StatefulWidget
{
    /// <summary>Creates a window manager over <paramref name="child"/>.</summary>
    public WindowManager(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>The widget below this one in the tree.</summary>
    public Widget Child { get; }

    public override State CreateState() => new WindowManagerState();

    private sealed class WindowManagerState : State<WindowManager>
    {
        // Dart's field initializer: creating the state throws while windowing is disabled.
        private readonly WindowRegistry _registry = new();

        public override Widget Build(BuildContext context)
        {
            if (!Features.IsWindowingEnabled)
            {
                return Widget.Child;
            }

            return new WindowRegistryScope(
                registry: _registry,
                child: new ListenableBuilder(
                    listenable: _registry,
                    child: Widget.Child,
                    builder: (builderContext, child) =>
                    {
                        List<Widget> subViews = _registry.Windows
                            .Select(entry => BuildWindow(entry, builderContext))
                            .ToList();
                        FlutterView? view = View.MaybeOf(builderContext);
                        if (view is null)
                        {
                            return new ViewCollection(subViews);
                        }

                        return new ViewAnchor(
                            view: subViews.Count > 0 ? new ViewCollection(subViews) : null,
                            child: child!);
                    }));
        }

        private static Widget BuildWindow(WindowEntry entry, BuildContext context)
        {
            return entry.Controller switch
            {
                DialogWindowController dialog => new DialogWindow(dialog, entry.Builder(context)),
                RegularWindowController regular => new RegularWindow(regular, entry.Builder(context)),
                TooltipWindowController tooltip => new TooltipWindow(tooltip, entry.Builder(context)),
                PopupWindowController popup => new PopupWindow(popup, entry.Builder(context)),
                SatelliteWindowController satellite => new SatelliteWindow(satellite, entry.Builder(context)),
                _ => throw new InvalidOperationException($"Unexpected controller {entry.Controller}"),
            };
        }
    }
}
