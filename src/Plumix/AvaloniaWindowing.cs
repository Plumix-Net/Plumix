using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only infrastructure: the Avalonia counterpart of the platform windowing owners Flutter's engine
// embedders back (`widgets/_window_macos.dart`, `_window_win32.dart`, `_window_linux.dart`). Those drive
// native windows through FFI; here every window is an Avalonia `Window` whose content is a `PlumixHost`
// view, and the controllers follow the framework-observable contract the three owners share.

namespace Plumix;

/// <summary>
/// The default <see cref="WindowingOwner"/> on Windows, Linux and macOS: creates each window as an
/// Avalonia <see cref="Window"/> hosting one <see cref="FlutterView"/>.
/// </summary>
/// <remarks>
/// <para>
/// Like the engine's owners, a controller is created synchronously with its window and its view, which is
/// in <see cref="PlatformDispatcher.Views"/> before the controller returns. Platform events (resizing,
/// activation, state and title changes) notify the controller's listeners; the setters only ask for the
/// change and notify through the events they cause, except <c>SetTitle</c> and <c>SetConstraints</c>,
/// which notify directly as the Win32 owner does.
/// </para>
/// <para>
/// A user close calls the delegate's <c>OnWindowCloseRequested</c> instead of closing; destroying the
/// window removes its view and calls <c>OnWindowDestroyed</c>. Satellite windows are not implemented, as
/// on every Flutter platform.
/// </para>
/// </remarks>
public sealed class AvaloniaWindowingOwner : WindowingOwner
{
    /// <summary>Creates the owner. Throws <see cref="NotSupportedException"/> unless windowing is enabled.</summary>
    public AvaloniaWindowingOwner()
    {
        Windowing.CheckEnabled();
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            throw new NotSupportedException("Only available on the Windows, Linux and macOS platforms");
        }
    }

    public override RegularWindowController CreateRegularWindowController(
        RegularWindowControllerDelegate @delegate,
        bool resizable,
        Size? size = null,
        BoxConstraints? constraints = null,
        string? title = null)
    {
        return new AvaloniaRegularWindowController(@delegate, resizable, size, constraints, title);
    }

    public override DialogWindowController CreateDialogWindowController(
        DialogWindowControllerDelegate @delegate,
        bool resizable,
        Size? size = null,
        BoxConstraints? constraints = null,
        BaseWindowController? parent = null,
        string? title = null)
    {
        return new AvaloniaDialogWindowController(@delegate, resizable, size, constraints, parent, title);
    }

    public override TooltipWindowController CreateTooltipWindowController(
        TooltipWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent)
    {
        return new AvaloniaTooltipWindowController(@delegate, constraints, anchorRect, positioner, parent);
    }

    public override PopupWindowController CreatePopupWindowController(
        PopupWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent)
    {
        return new AvaloniaPopupWindowController(@delegate, constraints, anchorRect, positioner, parent);
    }

    public override SatelliteWindowController CreateSatelliteWindowController(
        SatelliteWindowControllerDelegate @delegate,
        BaseWindowController parent,
        WindowPositioner initialPositioner,
        bool resizable,
        Rect? initialAnchorRect = null,
        Size? size = null,
        BoxConstraints? constraints = null,
        string? title = null)
    {
        throw new NotImplementedException("Satellite windows are not yet implemented on Avalonia.");
    }

    /// <summary>The Avalonia window that shows <paramref name="controller"/>'s view, if this owner made it.</summary>
    internal static Window? WindowOf(BaseWindowController? controller) =>
        (controller as IAvaloniaWindowController)?.NativeWindow;

    /// <summary>The window of the platform's implicit view, which Flutter's macOS owner parents to view 0.</summary>
    internal static Window? ImplicitViewWindow()
    {
        if (Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }
}

/// <summary>An owner-made controller's native window.</summary>
internal interface IAvaloniaWindowController
{
    Window NativeWindow { get; }
}

/// <summary>
/// The <see cref="PlumixHost"/> that renders a window's view: no implicit view, no render tree of its own
/// (the window widget's <see cref="View"/> creates it), and no say over the window's close requests.
/// </summary>
internal sealed class WindowViewHost : PlumixHost
{
    private Size _renderedLogicalSize;
    private bool _hasRendered;
    private bool _isAttached;
    private double _pendingDevicePixelRatio = 1.0;

    public WindowViewHost(bool sizedToContent)
    {
        IsSizedToContent = sizedToContent;
    }

    /// <summary>Whether the window takes its size from the view's content.</summary>
    public bool IsSizedToContent { get; }

    /// <summary>The constraints the content of a sized-to-content window lays out under, in logical pixels.</summary>
    public BoxConstraints ContentConstraints { get; set; } = BoxConstraints.Unbounded;

    /// <summary>Raised once a frame of a new logical size was rendered.</summary>
    public event Action<Size>? ContentSizeRendered;

    private protected override bool IsWindowingView => true;

    /// <summary>
    /// Reports the view's metrics again, after the constraints changed or before the window is shown.
    /// </summary>
    /// <remarks>
    /// A window sized to its content is shown on its view's first frame, before the host is in a visual
    /// tree that could report metrics; until then the view gets the expected device pixel ratio and the
    /// loose content constraints, so the frame lays the content out at its own size.
    /// </remarks>
    public void ReportMetrics(double devicePixelRatio)
    {
        _pendingDevicePixelRatio = devicePixelRatio > 0.0 && double.IsFinite(devicePixelRatio)
            ? devicePixelRatio
            : 1.0;
        if (_isAttached)
        {
            OnMetricsChanged();
            return;
        }

        RootFlutterView.UpdateMetrics(
            physicalSize: new Size(0, 0),
            devicePixelRatio: _pendingDevicePixelRatio,
            physicalConstraints: IsSizedToContent ? ToPhysical(ContentConstraints, _pendingDevicePixelRatio) : null);
        PlatformDispatcher.Instance.OnMetricsChanged?.Invoke();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _isAttached = true;
        base.OnAttachedToVisualTree(e);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        base.OnDetachedFromVisualTree(e);
    }

    private protected override BoxConstraints? GetViewPhysicalConstraints(MediaQueryData data)
    {
        return IsSizedToContent ? ToPhysical(ContentConstraints, data.DevicePixelRatio) : null;
    }

    private static BoxConstraints ToPhysical(BoxConstraints logical, double ratio)
    {
        return new BoxConstraints(
            MinWidth: logical.MinWidth * ratio,
            MaxWidth: logical.MaxWidth * ratio,
            MinHeight: logical.MinHeight * ratio,
            MaxHeight: logical.MaxHeight * ratio);
    }

    private protected override void OnViewRendered(Size physicalSize)
    {
        double ratio = RootFlutterView.DevicePixelRatio;
        var logical = new Size(physicalSize.Width / ratio, physicalSize.Height / ratio);
        // `FlutterView.render` runs inside the frame; resizing or showing the window reports new metrics,
        // which the engine does after the frame, never during it.
        Dispatcher.UIThread.Post(() => ApplyRenderedSize(logical));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return IsSizedToContent ? _renderedLogicalSize : base.MeasureOverride(availableSize);
    }

    private void ApplyRenderedSize(Size logical)
    {
        if (_hasRendered && logical == _renderedLogicalSize)
        {
            return;
        }

        _hasRendered = true;
        _renderedLogicalSize = logical;
        if (IsSizedToContent)
        {
            InvalidateMeasure();
        }

        ContentSizeRendered?.Invoke(logical);
    }
}

/// <summary>
/// The window, view and destruction bookkeeping the owner's controllers share: the engine side of a
/// Flutter window.
/// </summary>
internal sealed class AvaloniaNativeWindow
{
    private readonly Action _notifyListeners;
    private readonly Action _onCloseRequested;
    private readonly Action _onDestroyed;
    private bool _destroying;
    private bool _shown;

    public AvaloniaNativeWindow(
        bool sizedToContent,
        Action notifyListeners,
        Action onCloseRequested,
        Action onDestroyed)
    {
        _notifyListeners = notifyListeners;
        _onCloseRequested = onCloseRequested;
        _onDestroyed = onDestroyed;
        Host = new WindowViewHost(sizedToContent);
        Window = new Window
        {
            Content = Host,
            SizeToContent = sizedToContent ? SizeToContent.WidthAndHeight : SizeToContent.Manual,
        };
        Window.Resized += (_, _) => NotifyIfAlive();
        Window.Activated += (_, _) => NotifyIfAlive();
        Window.Deactivated += (_, _) => NotifyIfAlive();
        Window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Window.WindowStateProperty || e.Property == Window.TitleProperty)
            {
                NotifyIfAlive();
            }
        };
        Window.Closing += HandleClosing;
        Window.Closed += HandleClosed;
        // The engine adds the view to `PlatformDispatcher.views` when it creates the window.
        PlatformDispatcher.Instance.AddView(Host.RootFlutterView);
        Host.ReportMetrics(ExpectedDevicePixelRatio());
    }

    /// <summary>The scaling the window will render at: its screen's, or the implicit view window's.</summary>
    private double ExpectedDevicePixelRatio()
    {
        Window? reference = AvaloniaWindowingOwner.ImplicitViewWindow();
        if (reference is { IsVisible: true })
        {
            return reference.RenderScaling;
        }

        return Window.Screens.Primary?.Scaling ?? 1.0;
    }

    public Window Window { get; }

    public WindowViewHost Host { get; }

    public bool IsDestroyed { get; private set; }

    /// <summary>The window's client size in logical pixels.</summary>
    public Size ContentSize => Window.ClientSize;

    /// <summary>
    /// Shows the window: at once for a window with a size, on its view's first frame for one sized to its
    /// content (as the Linux owner presents it), owned by <paramref name="owner"/> and modal to it when
    /// <paramref name="modal"/>.
    /// </summary>
    public void Show(Window? owner, bool modal, bool showActivated = true)
    {
        Window.ShowActivated = showActivated;
        if (!Host.IsSizedToContent)
        {
            ShowNow(owner, modal);
            return;
        }

        Host.ContentSizeRendered += OnFirstFrame;

        void OnFirstFrame(Size _)
        {
            Host.ContentSizeRendered -= OnFirstFrame;
            ShowNow(owner, modal);
        }
    }

    public void ThrowIfDestroyed()
    {
        if (IsDestroyed)
        {
            throw new InvalidOperationException("Window has been destroyed.");
        }
    }

    public void SetConstraints(BoxConstraints constraints)
    {
        Window.MinWidth = constraints.MinWidth;
        Window.MinHeight = constraints.MinHeight;
        Window.MaxWidth = constraints.MaxWidth;
        Window.MaxHeight = constraints.MaxHeight;
        Host.ContentConstraints = constraints;
        Host.ReportMetrics(ExpectedDevicePixelRatio());
    }

    public void SetSize(Size size)
    {
        Window.Width = size.Width;
        Window.Height = size.Height;
    }

    public void Destroy()
    {
        if (IsDestroyed || _destroying)
        {
            return;
        }

        _destroying = true;
        if (_shown)
        {
            Window.Close();
        }
        else
        {
            HandleClosed(Window, EventArgs.Empty);
        }
    }

    private void ShowNow(Window? owner, bool modal)
    {
        if (IsDestroyed || _destroying || _shown)
        {
            return;
        }

        _shown = true;
        if (owner is not null && modal)
        {
            _ = Window.ShowDialog(owner);
        }
        else if (owner is not null)
        {
            Window.Show(owner);
        }
        else
        {
            Window.Show();
        }
    }

    private void NotifyIfAlive()
    {
        if (!IsDestroyed)
        {
            _notifyListeners();
        }
    }

    private void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_destroying)
        {
            return;
        }

        // The owners suppress the platform's default close and ask the delegate instead.
        e.Cancel = true;
        _onCloseRequested();
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        if (IsDestroyed)
        {
            return;
        }

        IsDestroyed = true;
        PlatformDispatcher.Instance.RemoveView(Host.RootFlutterView);
        _onDestroyed();
    }
}

/// <summary>The owner's <see cref="RegularWindowController"/>.</summary>
internal sealed class AvaloniaRegularWindowController : RegularWindowController, IAvaloniaWindowController
{
    private readonly RegularWindowControllerDelegate _delegate;
    private readonly AvaloniaNativeWindow _native;

    public AvaloniaRegularWindowController(
        RegularWindowControllerDelegate @delegate,
        bool resizable,
        Size? size,
        BoxConstraints? constraints,
        string? title)
    {
        _delegate = @delegate;
        _native = new AvaloniaNativeWindow(
            sizedToContent: size is null,
            notifyListeners: NotifyListeners,
            onCloseRequested: () => _delegate.OnWindowCloseRequested(this),
            onDestroyed: () => _delegate.OnWindowDestroyed());
        RootView = _native.Host.RootFlutterView;
        _native.Window.CanResize = resizable;
        if (constraints is { } value)
        {
            _native.SetConstraints(value);
        }

        if (size is { } initialSize)
        {
            _native.SetSize(initialSize);
        }

        if (title is not null)
        {
            SetTitle(title);
        }

        _native.Show(owner: null, modal: false);
    }

    public Window NativeWindow => _native.Window;

    public override Size ContentSize
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.ContentSize;
        }
    }

    public override string Title
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.Window.Title ?? string.Empty;
        }
    }

    public override bool IsActivated
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.Window.IsActive;
        }
    }

    public override bool IsMaximized => State() == WindowState.Maximized;

    public override bool IsMinimized => State() == WindowState.Minimized;

    public override bool IsFullscreen => State() == WindowState.FullScreen;

    public override void SetSize(Size size)
    {
        _native.ThrowIfDestroyed();
        _native.SetSize(size);
    }

    public override void SetConstraints(BoxConstraints constraints)
    {
        _native.ThrowIfDestroyed();
        _native.SetConstraints(constraints);
        NotifyListeners();
    }

    public override void SetTitle(string title)
    {
        _native.ThrowIfDestroyed();
        _native.Window.Title = title;
        NotifyListeners();
    }

    public override void Activate()
    {
        _native.ThrowIfDestroyed();
        if (_native.Window.WindowState == WindowState.Minimized)
        {
            _native.Window.WindowState = WindowState.Normal;
        }

        _native.Window.Activate();
    }

    public override void SetMaximized(bool maximized)
    {
        _native.ThrowIfDestroyed();
        _native.Window.WindowState = maximized ? WindowState.Maximized : WindowState.Normal;
    }

    public override void SetMinimized(bool minimized)
    {
        _native.ThrowIfDestroyed();
        _native.Window.WindowState = minimized ? WindowState.Minimized : WindowState.Normal;
    }

    public override void SetFullscreen(bool fullscreen, Display? display = null)
    {
        _native.ThrowIfDestroyed();
        _native.Window.WindowState = fullscreen ? WindowState.FullScreen : WindowState.Normal;
    }

    public override void Destroy() => _native.Destroy();

    private WindowState State()
    {
        _native.ThrowIfDestroyed();
        return _native.Window.WindowState;
    }
}

/// <summary>The owner's <see cref="DialogWindowController"/>.</summary>
internal sealed class AvaloniaDialogWindowController : DialogWindowController, IAvaloniaWindowController
{
    private readonly DialogWindowControllerDelegate _delegate;
    private readonly AvaloniaNativeWindow _native;

    public AvaloniaDialogWindowController(
        DialogWindowControllerDelegate @delegate,
        bool resizable,
        Size? size,
        BoxConstraints? constraints,
        BaseWindowController? parent,
        string? title)
    {
        _delegate = @delegate;
        Parent = parent;
        _native = new AvaloniaNativeWindow(
            sizedToContent: size is null,
            notifyListeners: NotifyListeners,
            onCloseRequested: () => _delegate.OnWindowCloseRequested(this),
            onDestroyed: () => _delegate.OnWindowDestroyed());
        RootView = _native.Host.RootFlutterView;
        _native.Window.CanResize = resizable;
        _native.Window.ShowInTaskbar = false;
        _native.Window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (constraints is { } value)
        {
            _native.SetConstraints(value);
        }

        if (size is { } initialSize)
        {
            _native.SetSize(initialSize);
        }

        if (title is not null)
        {
            SetTitle(title);
        }

        // A dialog with a parent is modal to it; one without is owned by the implicit view's window, as
        // the macOS owner parents it to view 0, but stays modeless.
        Window? parentWindow = AvaloniaWindowingOwner.WindowOf(parent);
        _native.Show(
            owner: parentWindow ?? AvaloniaWindowingOwner.ImplicitViewWindow(),
            modal: parentWindow is not null);
    }

    public Window NativeWindow => _native.Window;

    public override BaseWindowController? Parent { get; }

    public override Size ContentSize
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.ContentSize;
        }
    }

    public override string Title
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.Window.Title ?? string.Empty;
        }
    }

    public override bool IsActivated
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.Window.IsActive;
        }
    }

    public override bool IsMinimized
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.Window.WindowState == WindowState.Minimized;
        }
    }

    public override void SetSize(Size size)
    {
        _native.ThrowIfDestroyed();
        _native.SetSize(size);
    }

    public override void SetConstraints(BoxConstraints constraints)
    {
        _native.ThrowIfDestroyed();
        _native.SetConstraints(constraints);
        NotifyListeners();
    }

    public override void SetTitle(string title)
    {
        _native.ThrowIfDestroyed();
        _native.Window.Title = title;
        NotifyListeners();
    }

    public override void Activate()
    {
        _native.ThrowIfDestroyed();
        if (_native.Window.WindowState == WindowState.Minimized)
        {
            _native.Window.WindowState = WindowState.Normal;
        }

        _native.Window.Activate();
    }

    public override void SetMinimized(bool minimized)
    {
        _native.ThrowIfDestroyed();
        // The Win32 owner does not minimize a modal dialog.
        if (Parent is not null)
        {
            return;
        }

        _native.Window.WindowState = minimized ? WindowState.Minimized : WindowState.Normal;
    }

    public override void Destroy() => _native.Destroy();
}

/// <summary>
/// The anchoring the owner's tooltip and popup controllers share: placement through
/// <see cref="WindowPositioner.PlaceWindow"/> in physical screen pixels, as the Win32 owner answers the
/// engine's position query, and destruction when the parent moves on.
/// </summary>
internal sealed class AvaloniaAnchoredWindow
{
    private readonly AvaloniaNativeWindow _native;
    private readonly Window _parentWindow;
    private readonly BaseWindowController _parent;
    private readonly bool _isPopup;
    private Rect _anchorRect;
    private WindowPositioner _positioner;

    public AvaloniaAnchoredWindow(
        AvaloniaNativeWindow native,
        BaseWindowController parent,
        Rect anchorRect,
        WindowPositioner positioner,
        BoxConstraints constraints,
        bool isPopup)
    {
        _native = native;
        _parent = parent;
        _isPopup = isPopup;
        _anchorRect = anchorRect;
        _positioner = positioner;
        _parentWindow = AvaloniaWindowingOwner.WindowOf(parent)
                        ?? throw new InvalidOperationException(
                            $"Failed to find {(isPopup ? "popup" : "tooltip")} parent window");
        Window window = native.Window;
        window.WindowDecorations = WindowDecorations.None;
        window.CanResize = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        native.SetConstraints(constraints);
        native.Host.ContentSizeRendered += _ => Place();
        _parentWindow.Resized += HandleParentResized;
        _parentWindow.PositionChanged += HandleParentResized;
        _parentWindow.Deactivated += HandleParentDeactivated;
        window.Closed += (_, _) =>
        {
            _parentWindow.Resized -= HandleParentResized;
            _parentWindow.PositionChanged -= HandleParentResized;
            _parentWindow.Deactivated -= HandleParentDeactivated;
        };
        native.Show(owner: _parentWindow, modal: false, showActivated: isPopup);
    }

    public BaseWindowController Parent => _parent;

    /// <summary>The window's top-left corner in the parent's client area, in logical pixels.</summary>
    public Point OffsetFromParent
    {
        get
        {
            PixelPoint parentOrigin = _parentWindow.PointToScreen(new Point(0, 0));
            PixelPoint position = _native.Window.Position;
            double ratio = _parentWindow.RenderScaling;
            return new Point((position.X - parentOrigin.X) / ratio, (position.Y - parentOrigin.Y) / ratio);
        }
    }

    public void UpdatePosition(Rect? anchorRect, WindowPositioner? positioner)
    {
        _anchorRect = anchorRect ?? _anchorRect;
        _positioner = positioner ?? _positioner;
        Place();
    }

    /// <summary>
    /// Positions the window as the Win32 owner's <c>onGetWindowPosition</c> answer does: the anchor
    /// rectangle and offset scaled to physical pixels, relative to the parent's client area, inside the
    /// parent screen's working area. Like the macOS owner, only the position is applied.
    /// </summary>
    private void Place()
    {
        if (_native.IsDestroyed)
        {
            return;
        }

        double ratio = _parentWindow.RenderScaling;
        PixelPoint parentOrigin = _parentWindow.PointToScreen(new Point(0, 0));
        Size parentClient = _parentWindow.ClientSize;
        var parentRect = new Rect(
            parentOrigin.X,
            parentOrigin.Y,
            parentClient.Width * ratio,
            parentClient.Height * ratio);
        Screen? screen = _parentWindow.Screens.ScreenFromWindow(_parentWindow);
        Rect displayRect = screen is null
            ? new Rect(-1e9, -1e9, 2e9, 2e9)
            : new Rect(
                screen.WorkingArea.X,
                screen.WorkingArea.Y,
                screen.WorkingArea.Width,
                screen.WorkingArea.Height);
        var anchorRect = new Rect(
            parentRect.Left + _anchorRect.Left * ratio,
            parentRect.Top + _anchorRect.Top * ratio,
            _anchorRect.Width * ratio,
            _anchorRect.Height * ratio);
        WindowPositioner positioner = _positioner.CopyWith(
            offset: new Point(_positioner.Offset.X * ratio, _positioner.Offset.Y * ratio));
        Size childClient = _native.ContentSize;
        var childSize = new Size(childClient.Width * ratio, childClient.Height * ratio);
        Rect placed = positioner.PlaceWindow(
            childSize: childSize,
            anchorRect: anchorRect,
            parentRect: parentRect,
            displayRect: displayRect);
        _native.Window.Position = new PixelPoint((int)placed.Left, (int)placed.Top);
    }

    private void HandleParentResized(object? sender, EventArgs e)
    {
        // The Win32 owner closes anchored windows when the parent is resized.
        EventLoop.ScheduleMicrotask(_native.Destroy);
    }

    private void HandleParentDeactivated(object? sender, EventArgs e)
    {
        if (!_isPopup)
        {
            EventLoop.ScheduleMicrotask(_native.Destroy);
            return;
        }

        // A popup survives focus moving into itself.
        Dispatcher.UIThread.Post(() =>
        {
            if (!_native.Window.IsActive && !_parentWindow.IsActive)
            {
                _native.Destroy();
            }
        });
    }
}

/// <summary>The owner's <see cref="TooltipWindowController"/>.</summary>
internal sealed class AvaloniaTooltipWindowController : TooltipWindowController, IAvaloniaWindowController
{
    private readonly AvaloniaNativeWindow _native;
    private readonly AvaloniaAnchoredWindow _anchored;

    public AvaloniaTooltipWindowController(
        TooltipWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent)
    {
        _native = new AvaloniaNativeWindow(
            sizedToContent: true,
            notifyListeners: NotifyListeners,
            onCloseRequested: Destroy,
            onDestroyed: @delegate.OnWindowDestroyed);
        RootView = _native.Host.RootFlutterView;
        _anchored = new AvaloniaAnchoredWindow(_native, parent, anchorRect, positioner, constraints, isPopup: false);
    }

    public Window NativeWindow => _native.Window;

    public override BaseWindowController Parent => _anchored.Parent;

    public override Size ContentSize
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.ContentSize;
        }
    }

    // The Win32 owner ignores a tooltip's constraints after creation.
    public override void SetConstraints(BoxConstraints constraints) => _native.ThrowIfDestroyed();

    public override void UpdatePosition(Rect? anchorRect = null, WindowPositioner? positioner = null)
    {
        _native.ThrowIfDestroyed();
        _anchored.UpdatePosition(anchorRect, positioner);
    }

    public override void Destroy() => _native.Destroy();
}

/// <summary>The owner's <see cref="PopupWindowController"/>.</summary>
internal sealed class AvaloniaPopupWindowController : PopupWindowController, IAvaloniaWindowController
{
    private readonly AvaloniaNativeWindow _native;
    private readonly AvaloniaAnchoredWindow _anchored;

    public AvaloniaPopupWindowController(
        PopupWindowControllerDelegate @delegate,
        BoxConstraints constraints,
        Rect anchorRect,
        WindowPositioner positioner,
        BaseWindowController parent)
    {
        _native = new AvaloniaNativeWindow(
            sizedToContent: true,
            notifyListeners: NotifyListeners,
            onCloseRequested: Destroy,
            onDestroyed: @delegate.OnWindowDestroyed);
        RootView = _native.Host.RootFlutterView;
        _anchored = new AvaloniaAnchoredWindow(_native, parent, anchorRect, positioner, constraints, isPopup: true);
    }

    public Window NativeWindow => _native.Window;

    public override BaseWindowController Parent => _anchored.Parent;

    public override Size ContentSize
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.ContentSize;
        }
    }

    public override Point OffsetFromParent
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _anchored.OffsetFromParent;
        }
    }

    // Like the Win32 owner's popup, the popup window is shown activated and is activated itself, in
    // place of the base class's walk to the closest regular or dialog ancestor.
    public override bool IsActivated
    {
        get
        {
            _native.ThrowIfDestroyed();
            return _native.Window.IsActive;
        }
    }

    public override void Activate()
    {
        _native.ThrowIfDestroyed();
        _native.Window.Activate();
    }

    // The Win32 owner ignores a popup's constraints after creation.
    public override void SetConstraints(BoxConstraints constraints) => _native.ThrowIfDestroyed();

    public override void UpdatePosition(Rect? anchorRect = null, WindowPositioner? positioner = null)
    {
        _native.ThrowIfDestroyed();
        _anchored.UpdatePosition(anchorRect, positioner);
    }

    public override void Destroy() => _native.Destroy();
}
