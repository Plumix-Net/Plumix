using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/_window.dart

// Do not use these APIs in production applications or packages: like Flutter's `_window.dart`
// (`@internal`, not exported by `widgets.dart`), the windowing API is experimental, every member throws
// a `NotSupportedException` unless `Features.IsWindowingEnabled` is on, and it will change without notice.
// See: https://github.com/flutter/flutter/issues/30701.

namespace Plumix.Widgets;

/// <summary>The shared pieces of Dart's <c>_window.dart</c> that are top-level there.</summary>
public static class Windowing
{
    /// <summary>Dart's <c>_kWindowingDisabledErrorMessage</c>.</summary>
    internal const string WindowingDisabledErrorMessage =
        """
        Windowing APIs are not enabled.

        Windowing APIs are currently experimental. Do not use windowing APIs in
        production applications or plugins published to pub.dev.

        To try experimental windowing APIs:
        1. Switch to Flutter's main release channel.
        2. Turn on the windowing feature flag.

        See: https://github.com/flutter/flutter/issues/30701.

        """;

    /// <summary>
    /// Dart's FLAG CHECK: throws <see cref="NotSupportedException"/> (Dart's <c>UnsupportedError</c>)
    /// unless <see cref="Features.IsWindowingEnabled"/>.
    /// </summary>
    internal static void CheckEnabled()
    {
        if (!Features.IsWindowingEnabled)
        {
            throw new NotSupportedException(WindowingDisabledErrorMessage);
        }
    }

    /// <summary>
    /// Creates a default <see cref="WindowingOwner"/> for the current platform.
    /// </summary>
    /// <remarks>
    /// Dart's <c>createDefaultWindowingOwner</c>. Only supported on desktop platforms; any other platform,
    /// or windowing being disabled, gets an owner whose controller factories throw.
    /// </remarks>
    public static WindowingOwner CreateDefaultWindowingOwner()
    {
        if (!Features.IsWindowingEnabled)
        {
            return new WindowingOwnerUnsupported(errorMessage: WindowingDisabledErrorMessage);
        }

        WindowingOwner? owner = CreateDefaultOwner();
        if (owner is not null)
        {
            return owner;
        }

        return new WindowingOwnerUnsupported(errorMessage: "Windowing is unsupported on this platform.");
    }

    /// <summary>
    /// Dart's <c>_window_io.dart</c>/<c>_window_web.dart</c> <c>createDefaultOwner</c>: the platform owner
    /// for Windows, Linux and macOS, <see langword="null"/> anywhere else.
    /// </summary>
    /// <remarks>
    /// Flutter's desktop owners drive the engine's native windows through FFI. Plumix's engine role is
    /// played by Avalonia, so one owner, <see cref="AvaloniaWindowingOwner"/>, serves the three platforms.
    /// </remarks>
    internal static WindowingOwner? CreateDefaultOwner()
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return new AvaloniaWindowingOwner();
        }

        return null;
    }

    internal static void DebugAssertSatisfies(BoxConstraints? constraints, Size size)
    {
        if (Constants.KDebugMode && constraints is { } value && !value.IsSatisfiedBy(size))
        {
            throw new AssertionError("constraints.isSatisfiedBy(size)");
        }
    }
}

/// <summary>Base class for window controllers.</summary>
/// <remarks>
/// <para>
/// Dart's sealed <c>BaseWindowController</c>. A window controller must provide a <see cref="RootView"/>
/// as well as its current size, <see cref="ContentSize"/>. Subclasses are
/// <see cref="RegularWindowController"/>, <see cref="DialogWindowController"/>,
/// <see cref="TooltipWindowController"/>, <see cref="PopupWindowController"/> and
/// <see cref="SatelliteWindowController"/>; the constructor is assembly-private so the set stays closed.
/// </para>
/// <para>
/// This class does not interact with the widget tree. Instead, it is typically provided to a window
/// widget, such as <see cref="RegularWindow"/>, which listens to it and rebuilds when it notifies.
/// </para>
/// </remarks>
public abstract class BaseWindowController : ChangeNotifier
{
    private FlutterView? _view;

    private protected BaseWindowController()
    {
    }

    /// <summary>The current size of the drawable area of the window.</summary>
    /// <remarks>This might differ from the requested size.</remarks>
    public abstract Size ContentSize { get; }

    /// <summary>Destroys this window.</summary>
    /// <remarks>
    /// It is permissible to call this method multiple times. Destroying the window does not dispose of the
    /// controller; that remains the caller's job.
    /// </remarks>
    public abstract void Destroy();

    /// <summary>The root view associated to this window, which is unique to each window.</summary>
    /// <remarks>
    /// Dart's <c>late final _view</c>: reading it before a subclass set it throws, and so does setting it
    /// twice (both Dart's <c>LateInitializationError</c>).
    /// </remarks>
    public FlutterView RootView
    {
        get => _view ?? throw new InvalidOperationException("Field '_view' has not been initialized.");
        protected set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_view is not null)
            {
                throw new InvalidOperationException("Field '_view' has already been initialized.");
            }

            _view = value;
        }
    }
}

/// <summary>Delegate class for regular window controller.</summary>
/// <remarks>Dart's <c>RegularWindowControllerDelegate</c> mixin class.</remarks>
public class RegularWindowControllerDelegate
{
    /// <summary>Invoked when user attempts to close the window.</summary>
    /// <remarks>
    /// The default implementation destroys the window. Subclasses can override the behavior to delay or
    /// prevent the window from closing.
    /// </remarks>
    public virtual void OnWindowCloseRequested(RegularWindowController controller)
    {
        Windowing.CheckEnabled();
        controller.Destroy();
    }

    /// <summary>Invoked after the window is closed.</summary>
    /// <remarks>The default implementation does nothing.</remarks>
    public virtual void OnWindowDestroyed()
    {
        Windowing.CheckEnabled();
    }
}

/// <summary>
/// A controller for a regular window: a top-level window with a title bar, typically resizable.
/// </summary>
/// <remarks>
/// Dart's <c>RegularWindowController</c>. Its factory constructors are the static <see cref="Create"/>
/// and <see cref="SizedToContent"/>, which ask <see cref="WidgetsBinding.WindowingOwner"/> for the
/// platform's controller; the <c>empty</c> constructor is this type's protected constructor.
/// </remarks>
public abstract class RegularWindowController : BaseWindowController
{
    /// <summary>Dart's <c>RegularWindowController.empty</c>, for subclasses.</summary>
    protected RegularWindowController()
    {
    }

    /// <summary>Creates a <see cref="RegularWindowController"/> with the provided properties.</summary>
    /// <remarks>
    /// Dart's <c>RegularWindowController</c> factory. <paramref name="size"/> is the initial content size;
    /// <paramref name="constraints"/> must be satisfied by it.
    /// </remarks>
    public static RegularWindowController Create(
        Size size,
        BoxConstraints? constraints = null,
        string? title = null,
        RegularWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        Windowing.DebugAssertSatisfies(constraints, size);
        return WidgetsBinding.Instance.WindowingOwner.CreateRegularWindowController(
            @delegate: @delegate ?? new RegularWindowControllerDelegate(),
            size: size,
            constraints: constraints,
            title: title,
            resizable: true);
    }

    /// <summary>Creates a <see cref="RegularWindowController"/> that is sized to its content.</summary>
    /// <remarks>Dart's <c>RegularWindowController.sizedToContent</c> factory.</remarks>
    public static RegularWindowController SizedToContent(
        bool resizable = false,
        BoxConstraints? constraints = null,
        string? title = null,
        RegularWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        WidgetsFlutterBinding.EnsureInitialized();
        return WidgetsBinding.Instance.WindowingOwner.CreateRegularWindowController(
            @delegate: @delegate ?? new RegularWindowControllerDelegate(),
            constraints: constraints,
            resizable: resizable,
            title: title);
    }

    /// <summary>The current title of the window.</summary>
    public abstract string Title { get; }

    /// <summary>Whether this window is currently activated.</summary>
    public abstract bool IsActivated { get; }

    /// <summary>Whether the window is currently maximized.</summary>
    public abstract bool IsMaximized { get; }

    /// <summary>Whether the window is currently minimized.</summary>
    public abstract bool IsMinimized { get; }

    /// <summary>Whether this window is currently fullscreen.</summary>
    public abstract bool IsFullscreen { get; }

    /// <summary>Request change for the window's content size.</summary>
    /// <remarks>The system might not grant the request; <see cref="BaseWindowController.ContentSize"/>
    /// reports the result.</remarks>
    public abstract void SetSize(Size size);

    /// <summary>Request change for the window's content size constraints.</summary>
    public abstract void SetConstraints(BoxConstraints constraints);

    /// <summary>Request change for the window title.</summary>
    public abstract void SetTitle(string title);

    /// <summary>Requests that the window be displayed in its current size and position.</summary>
    /// <remarks>If the window is minimized or maximized, the window returns to the size and position that it
    /// had before that state was applied.</remarks>
    public abstract void Activate();

    /// <summary>Requests the window to be maximized (<see langword="true"/>) or restored.</summary>
    public abstract void SetMaximized(bool maximized);

    /// <summary>Requests the window to be minimized (<see langword="true"/>) or restored.</summary>
    public abstract void SetMinimized(bool minimized);

    /// <summary>Request change for the window to enter or exit fullscreen state.</summary>
    /// <remarks><paramref name="display"/> may name the display to go fullscreen on.</remarks>
    public abstract void SetFullscreen(bool fullscreen, Display? display = null);
}

/// <summary>Delegate class for dialog window controller.</summary>
/// <remarks>Dart's <c>DialogWindowControllerDelegate</c> mixin class.</remarks>
public class DialogWindowControllerDelegate
{
    /// <summary>Invoked when user attempts to close the window.</summary>
    /// <remarks>The default implementation destroys the window.</remarks>
    public virtual void OnWindowCloseRequested(DialogWindowController controller)
    {
        Windowing.CheckEnabled();
        controller.Destroy();
    }

    /// <summary>Invoked after the window is closed.</summary>
    /// <remarks>The default implementation does nothing.</remarks>
    public virtual void OnWindowDestroyed()
    {
        Windowing.CheckEnabled();
    }
}

/// <summary>A controller for a dialog window.</summary>
/// <remarks>
/// Dart's <c>DialogWindowController</c>. A dialog with a <see cref="Parent"/> is modal to it; one without
/// is modeless.
/// </remarks>
public abstract class DialogWindowController : BaseWindowController
{
    /// <summary>Dart's <c>DialogWindowController.empty</c>, for subclasses.</summary>
    protected DialogWindowController()
    {
    }

    /// <summary>Creates a <see cref="DialogWindowController"/> with the provided properties.</summary>
    /// <remarks>Dart's <c>DialogWindowController</c> factory.</remarks>
    public static DialogWindowController Create(
        Size size,
        BoxConstraints? constraints = null,
        BaseWindowController? parent = null,
        string? title = null,
        DialogWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        WidgetsFlutterBinding.EnsureInitialized();
        Windowing.DebugAssertSatisfies(constraints, size);
        return WidgetsBinding.Instance.WindowingOwner.CreateDialogWindowController(
            @delegate: @delegate ?? new DialogWindowControllerDelegate(),
            size: size,
            constraints: constraints,
            title: title,
            parent: parent,
            resizable: true);
    }

    /// <summary>Creates a <see cref="DialogWindowController"/> that is sized to its content.</summary>
    /// <remarks>Dart's <c>DialogWindowController.sizedToContent</c> factory.</remarks>
    public static DialogWindowController SizedToContent(
        bool resizable = false,
        BoxConstraints? constraints = null,
        BaseWindowController? parent = null,
        string? title = null,
        DialogWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        WidgetsFlutterBinding.EnsureInitialized();
        return WidgetsBinding.Instance.WindowingOwner.CreateDialogWindowController(
            @delegate: @delegate ?? new DialogWindowControllerDelegate(),
            constraints: constraints,
            resizable: resizable,
            title: title,
            parent: parent);
    }

    /// <summary>The parent controller of this dialog, if any.</summary>
    /// <remarks>If <see langword="null"/>, this dialog is modeless; otherwise it is modal to the parent.</remarks>
    public abstract BaseWindowController? Parent { get; }

    /// <summary>The current title of the window.</summary>
    public abstract string Title { get; }

    /// <summary>Whether this window is currently activated.</summary>
    public abstract bool IsActivated { get; }

    /// <summary>Whether the window is currently minimized.</summary>
    public abstract bool IsMinimized { get; }

    /// <summary>Request change for the window's content size.</summary>
    public abstract void SetSize(Size size);

    /// <summary>Request change for the window's content size constraints.</summary>
    public abstract void SetConstraints(BoxConstraints constraints);

    /// <summary>Request change for the window title.</summary>
    public abstract void SetTitle(string title);

    /// <summary>Requests that the window be displayed in its current size and position.</summary>
    public abstract void Activate();

    /// <summary>Requests the window to be minimized (<see langword="true"/>) or restored.</summary>
    /// <remarks>A modal dialog may not be minimized on every platform.</remarks>
    public abstract void SetMinimized(bool minimized);
}

/// <summary>Delegate class for tooltip window controller.</summary>
/// <remarks>Dart's <c>TooltipWindowControllerDelegate</c> mixin class.</remarks>
public class TooltipWindowControllerDelegate
{
    /// <summary>Invoked after the window is closed.</summary>
    /// <remarks>The default implementation does nothing.</remarks>
    public virtual void OnWindowDestroyed()
    {
        Windowing.CheckEnabled();
    }
}

/// <summary>A controller for a tooltip window: a non-interactive window anchored to a parent.</summary>
/// <remarks>Dart's <c>TooltipWindowController</c>.</remarks>
public abstract class TooltipWindowController : BaseWindowController
{
    /// <summary>Dart's <c>TooltipWindowController.empty</c>, for subclasses.</summary>
    protected TooltipWindowController()
    {
    }

    /// <summary>Creates a <see cref="TooltipWindowController"/> with the provided properties.</summary>
    /// <remarks>
    /// Dart's <c>TooltipWindowController</c> factory. <paramref name="anchorRect"/> is in the parent's
    /// logical coordinates; <paramref name="constraints"/> defaults to Dart's
    /// <c>const BoxConstraints()</c>.
    /// </remarks>
    public static TooltipWindowController Create(
        BaseWindowController parent,
        Rect anchorRect,
        WindowPositioner positioner,
        BoxConstraints? constraints = null,
        TooltipWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        WidgetsFlutterBinding.EnsureInitialized();
        return WidgetsBinding.Instance.WindowingOwner.CreateTooltipWindowController(
            parent: parent,
            constraints: constraints ?? BoxConstraints.Unbounded,
            @delegate: @delegate ?? new TooltipWindowControllerDelegate(),
            anchorRect: anchorRect,
            positioner: positioner);
    }

    /// <summary>The parent controller of this tooltip.</summary>
    public abstract BaseWindowController Parent { get; }

    /// <summary>Request change for the window's content size constraints.</summary>
    public abstract void SetConstraints(BoxConstraints constraints);

    /// <summary>Updates the position of the tooltip window.</summary>
    /// <remarks>A <see langword="null"/> argument keeps the current value.</remarks>
    public abstract void UpdatePosition(Rect? anchorRect = null, WindowPositioner? positioner = null);
}

/// <summary>Delegate class for popup window controller.</summary>
/// <remarks>Dart's <c>PopupWindowControllerDelegate</c> mixin class.</remarks>
public class PopupWindowControllerDelegate
{
    /// <summary>Invoked after the window is closed.</summary>
    /// <remarks>The default implementation does nothing.</remarks>
    public virtual void OnWindowDestroyed()
    {
        Windowing.CheckEnabled();
    }
}

/// <summary>A controller for a popup window: an interactive window anchored to a parent.</summary>
/// <remarks>Dart's <c>PopupWindowController</c>.</remarks>
public abstract class PopupWindowController : BaseWindowController
{
    /// <summary>Dart's <c>PopupWindowController.empty</c>, for subclasses.</summary>
    protected PopupWindowController()
    {
    }

    /// <summary>Creates a <see cref="PopupWindowController"/> with the provided properties.</summary>
    /// <remarks>Dart's <c>PopupWindowController</c> factory.</remarks>
    public static PopupWindowController Create(
        BaseWindowController parent,
        Rect anchorRect,
        WindowPositioner positioner,
        BoxConstraints? constraints = null,
        PopupWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        WidgetsFlutterBinding.EnsureInitialized();
        return WidgetsBinding.Instance.WindowingOwner.CreatePopupWindowController(
            parent: parent,
            constraints: constraints ?? BoxConstraints.Unbounded,
            @delegate: @delegate ?? new PopupWindowControllerDelegate(),
            anchorRect: anchorRect,
            positioner: positioner);
    }

    /// <summary>The parent controller of this popup.</summary>
    public abstract BaseWindowController Parent { get; }

    /// <summary>Request change for the window's content size constraints.</summary>
    public abstract void SetConstraints(BoxConstraints constraints);

    /// <summary>Updates the position of the popup window.</summary>
    /// <remarks>A <see langword="null"/> argument keeps the current value.</remarks>
    public abstract void UpdatePosition(Rect? anchorRect = null, WindowPositioner? positioner = null);

    /// <summary>The popup's top-left corner in the parent's client area, in logical coordinates.</summary>
    public abstract Point OffsetFromParent { get; }

    /// <summary>
    /// Activates the closest regular or dialog window ancestor, since a popup cannot be activated itself.
    /// </summary>
    public virtual void Activate()
    {
        BaseWindowController parent = Parent;
        while (true)
        {
            if (parent is RegularWindowController regular)
            {
                regular.Activate();
                break;
            }

            if (parent is DialogWindowController dialog)
            {
                dialog.Activate();
                break;
            }

            if (parent is PopupWindowController popup)
            {
                parent = popup.Parent;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected controller in hierarchy {parent}");
            }
        }
    }

    /// <summary>Whether the closest regular or dialog window ancestor is activated.</summary>
    public virtual bool IsActivated
    {
        get
        {
            BaseWindowController parent = Parent;
            while (true)
            {
                if (parent is RegularWindowController regular)
                {
                    return regular.IsActivated;
                }

                if (parent is DialogWindowController dialog)
                {
                    return dialog.IsActivated;
                }

                if (parent is PopupWindowController popup)
                {
                    parent = popup.Parent;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected controller in hierarchy {parent}");
                }
            }
        }
    }
}

/// <summary>Delegate class for satellite window controller.</summary>
/// <remarks>Dart's <c>SatelliteWindowControllerDelegate</c> mixin class.</remarks>
public class SatelliteWindowControllerDelegate
{
    /// <summary>Invoked when user attempts to close the window.</summary>
    /// <remarks>The default implementation destroys the window.</remarks>
    public virtual void OnWindowCloseRequested(SatelliteWindowController controller)
    {
        Windowing.CheckEnabled();
        controller.Destroy();
    }

    /// <summary>Invoked after the window is closed.</summary>
    /// <remarks>The default implementation does nothing.</remarks>
    public virtual void OnWindowDestroyed()
    {
        Windowing.CheckEnabled();
    }
}

/// <summary>
/// A controller for a satellite window: a window that floats beside its parent, such as a tool palette.
/// </summary>
/// <remarks>Dart's <c>SatelliteWindowController</c>.</remarks>
public abstract class SatelliteWindowController : BaseWindowController
{
    /// <summary>Dart's <c>SatelliteWindowController.empty</c>, for subclasses.</summary>
    protected SatelliteWindowController()
    {
    }

    /// <summary>Creates a <see cref="SatelliteWindowController"/> with the provided properties.</summary>
    /// <remarks>Dart's <c>SatelliteWindowController</c> factory.</remarks>
    public static SatelliteWindowController Create(
        BaseWindowController parent,
        WindowPositioner initialPositioner,
        Rect? initialAnchorRect = null,
        Size? size = null,
        BoxConstraints? constraints = null,
        string? title = null,
        SatelliteWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        if (size is { } initialSize)
        {
            Windowing.DebugAssertSatisfies(constraints, initialSize);
        }

        return WidgetsBinding.Instance.WindowingOwner.CreateSatelliteWindowController(
            @delegate: @delegate ?? new SatelliteWindowControllerDelegate(),
            parent: parent,
            initialAnchorRect: initialAnchorRect,
            initialPositioner: initialPositioner,
            size: size,
            constraints: constraints,
            title: title,
            resizable: true);
    }

    /// <summary>Creates a <see cref="SatelliteWindowController"/> that is sized to its content.</summary>
    /// <remarks>Dart's <c>SatelliteWindowController.sizedToContent</c> factory.</remarks>
    public static SatelliteWindowController SizedToContent(
        BaseWindowController parent,
        WindowPositioner initialPositioner,
        Rect? initialAnchorRect = null,
        bool resizable = false,
        BoxConstraints? constraints = null,
        string? title = null,
        SatelliteWindowControllerDelegate? @delegate = null)
    {
        Windowing.CheckEnabled();
        WidgetsFlutterBinding.EnsureInitialized();
        return WidgetsBinding.Instance.WindowingOwner.CreateSatelliteWindowController(
            @delegate: @delegate ?? new SatelliteWindowControllerDelegate(),
            parent: parent,
            initialAnchorRect: initialAnchorRect,
            initialPositioner: initialPositioner,
            constraints: constraints,
            resizable: resizable,
            title: title);
    }

    /// <summary>The parent controller of this satellite window.</summary>
    public abstract BaseWindowController Parent { get; }

    /// <summary>The current title of the window.</summary>
    public abstract string Title { get; }

    /// <summary>Whether this window is currently activated.</summary>
    public abstract bool IsActivated { get; }

    /// <summary>Changes the parent of this satellite window.</summary>
    public abstract void SetParent(BaseWindowController parent);

    /// <summary>Request change for the window's content size.</summary>
    public abstract void SetSize(Size size);

    /// <summary>Request change for the window's content size constraints.</summary>
    public abstract void SetConstraints(BoxConstraints constraints);

    /// <summary>Request change for the window title.</summary>
    public abstract void SetTitle(string title);

    /// <summary>Requests that the window be displayed in its current size and position.</summary>
    public abstract void Activate();
}

/// <summary>
/// A <see cref="WindowingOwner"/> is responsible for creating and managing window controllers.
/// </summary>
/// <remarks>
/// Dart's <c>WindowingOwner</c>. The default owner, from <see cref="Windowing.CreateDefaultWindowingOwner"/>,
/// supports macOS, Linux and Windows; a custom one can be installed through
/// <see cref="WidgetsBinding.WindowingOwner"/>.
/// </remarks>
public abstract class WindowingOwner
{
    /// <summary>Creates a <see cref="RegularWindowController"/> with the provided properties.</summary>
    /// <remarks>A <see langword="null"/> <paramref name="size"/> sizes the window to its content.</remarks>
    public abstract RegularWindowController CreateRegularWindowController(
        RegularWindowControllerDelegate @delegate,
        bool resizable,
        Size? size = null,
        BoxConstraints? constraints = null,
        string? title = null);

    /// <summary>Creates a <see cref="DialogWindowController"/> with the provided properties.</summary>
    /// <remarks>A <see langword="null"/> <paramref name="size"/> sizes the window to its content.</remarks>
    public abstract DialogWindowController CreateDialogWindowController(
        DialogWindowControllerDelegate @delegate,
        bool resizable,
        Size? size = null,
        BoxConstraints? constraints = null,
        BaseWindowController? parent = null,
        string? title = null);

    /// <summary>Creates a <see cref="TooltipWindowController"/> with the provided properties.</summary>
    public abstract TooltipWindowController CreateTooltipWindowController(
        TooltipWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent);

    /// <summary>Creates a <see cref="PopupWindowController"/> with the provided properties.</summary>
    public abstract PopupWindowController CreatePopupWindowController(
        PopupWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent);

    /// <summary>Creates a <see cref="SatelliteWindowController"/> with the provided properties.</summary>
    public abstract SatelliteWindowController CreateSatelliteWindowController(
        SatelliteWindowControllerDelegate @delegate,
        BaseWindowController parent,
        WindowPositioner initialPositioner,
        bool resizable,
        Rect? initialAnchorRect = null,
        Size? size = null,
        BoxConstraints? constraints = null,
        string? title = null);
}

/// <summary>
/// Dart's private <c>_WindowingOwnerUnsupported</c>: the owner installed when windowing is disabled or
/// the platform has no windowing support. Regular and dialog factories throw
/// <see cref="NotSupportedException"/>; the others throw <see cref="NotImplementedException"/>.
/// </summary>
internal sealed class WindowingOwnerUnsupported : WindowingOwner
{
    public WindowingOwnerUnsupported(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; }

    public override RegularWindowController CreateRegularWindowController(
        RegularWindowControllerDelegate @delegate,
        bool resizable = true,
        Size? size = null,
        BoxConstraints? constraints = null,
        string? title = null)
    {
        throw new NotSupportedException(ErrorMessage);
    }

    public override DialogWindowController CreateDialogWindowController(
        DialogWindowControllerDelegate @delegate,
        bool resizable = true,
        Size? size = null,
        BoxConstraints? constraints = null,
        BaseWindowController? parent = null,
        string? title = null)
    {
        throw new NotSupportedException(ErrorMessage);
    }

    public override TooltipWindowController CreateTooltipWindowController(
        TooltipWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent)
    {
        throw new NotImplementedException(ErrorMessage);
    }

    public override PopupWindowController CreatePopupWindowController(
        PopupWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent)
    {
        throw new NotImplementedException(ErrorMessage);
    }

    public override SatelliteWindowController CreateSatelliteWindowController(
        SatelliteWindowControllerDelegate @delegate,
        BaseWindowController parent,
        WindowPositioner initialPositioner,
        bool resizable = true,
        Rect? initialAnchorRect = null,
        Size? size = null,
        BoxConstraints? constraints = null,
        string? title = null)
    {
        throw new NotImplementedException(ErrorMessage);
    }
}
