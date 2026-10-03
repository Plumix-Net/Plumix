using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/platform_view.dart

namespace Plumix.Widgets;

/// <summary>Signature for a callback that receives the HTML element created by an
/// <see cref="HtmlElementView"/>.</summary>
/// <remarks>Flutter's <c>ElementCreatedCallback</c>.</remarks>
public delegate void ElementCreatedCallback(object element);

/// <summary>Embeds an Android view in the Widget hierarchy.</summary>
/// <remarks>
/// Flutter's <c>AndroidView</c>. Requires Android API level 23 or greater. Embedding Android views is
/// an expensive operation and should be avoided when a Flutter equivalent is possible. The embedded
/// Android view is painted just like any other Flutter widget and transformations apply to it as well.
/// The widget fills all available space, the parent of this object must provide bounded layout
/// constraints. The widget participates in Flutter's gesture arenas, and dispatches touch events to
/// the platform view iff it won the arena. Specific gestures that should be dispatched to the platform
/// view can be specified in the <see cref="GestureRecognizers"/> constructor parameter. If the set of
/// gesture recognizers is empty, a gesture will be dispatched to the platform view iff it was not
/// claimed by any other gesture recognizer.
/// </remarks>
public class AndroidView : StatefulWidget
{
    /// <summary>Creates a widget that embeds an Android view.</summary>
    /// <remarks>If <paramref name="creationParams"/> is not null then
    /// <paramref name="creationParamsCodec"/> must not be null.</remarks>
    public AndroidView(
        string viewType,
        PlatformViewCreatedCallback? onPlatformViewCreated = null,
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        TextDirection? layoutDirection = null,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? gestureRecognizers = null,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null)
        : base(key)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);
        ViewType = viewType;
        OnPlatformViewCreated = onPlatformViewCreated;
        HitTestBehavior = hitTestBehavior;
        LayoutDirection = layoutDirection;
        GestureRecognizers = gestureRecognizers;
        CreationParams = creationParams;
        CreationParamsCodec = creationParamsCodec;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The unique identifier for Android view type to be embedded by this widget.</summary>
    /// <remarks>A <c>PlatformViewFactory</c> for this type must have been registered.</remarks>
    public string ViewType { get; }

    /// <summary>Callback to invoke after the platform view has been created.</summary>
    public PlatformViewCreatedCallback? OnPlatformViewCreated { get; }

    /// <summary>How this widget should behave during hit testing.</summary>
    /// <remarks>This defaults to <see cref="PlatformViewHitTestBehavior.Opaque"/>.</remarks>
    public PlatformViewHitTestBehavior HitTestBehavior { get; }

    /// <summary>The text direction to use for the embedded view.</summary>
    /// <remarks>If this is null, the ambient <see cref="Directionality"/> is used instead.</remarks>
    public TextDirection? LayoutDirection { get; }

    /// <summary>Which gestures should be forwarded to the Android view.</summary>
    /// <remarks>
    /// The gesture recognizers built by factories in this set participate in the gesture arena for
    /// each pointer that was put down on the widget. If any of these recognizers win the gesture arena,
    /// the entire pointer event sequence starting from the pointer down event will be dispatched to the
    /// Android view. When null, an empty set of gesture recognizer factories is used, in which case a
    /// pointer event sequence will only be dispatched to the Android view if no other member of the
    /// arena claimed it.
    /// </remarks>
    public IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? GestureRecognizers { get; }

    /// <summary>Passed as the args argument of <c>PlatformViewFactory#create</c>.</summary>
    /// <remarks>This can be used by plugins to pass constructor parameters to the embedded Android
    /// view.</remarks>
    public object? CreationParams { get; }

    /// <summary>The codec used to encode <see cref="CreationParams"/> before sending it to the platform
    /// side.</summary>
    /// <remarks>This is typically one of: <see cref="StandardMessageCodec"/>,
    /// <see cref="JsonMessageCodec"/>, <see cref="StringCodec"/>, or <see cref="BinaryCodec"/>.</remarks>
    public IMessageCodec? CreationParamsCodec { get; }

    /// <summary>The content will be clipped (or not) according to this option.</summary>
    /// <remarks>Defaults to <see cref="Clip.HardEdge"/>.</remarks>
    public Clip ClipBehavior { get; }

    public override State CreateState() => new AndroidViewState();
}

/// <summary>Common superclass for iOS and macOS platform views.</summary>
/// <remarks>
/// Dart's private <c>_DarwinView</c>. C# forbids a public class deriving from an internal one, so the
/// base is public with an internal constructor (<c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
public abstract class DarwinView : StatefulWidget
{
    private protected DarwinView(
        string viewType,
        PlatformViewCreatedCallback? onPlatformViewCreated = null,
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        TextDirection? layoutDirection = null,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? gestureRecognizers = null,
        Key? key = null)
        : base(key)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);
        ViewType = viewType;
        OnPlatformViewCreated = onPlatformViewCreated;
        HitTestBehavior = hitTestBehavior;
        LayoutDirection = layoutDirection;
        CreationParams = creationParams;
        CreationParamsCodec = creationParamsCodec;
        GestureRecognizers = gestureRecognizers;
    }

    /// <summary>The unique identifier for iOS view type to be embedded by this widget.</summary>
    /// <remarks>A PlatformViewFactory for this type must have been registered.</remarks>
    public string ViewType { get; }

    /// <summary>Callback to invoke after the platform view has been created.</summary>
    public PlatformViewCreatedCallback? OnPlatformViewCreated { get; }

    /// <summary>How this widget should behave during hit testing.</summary>
    /// <remarks>This defaults to <see cref="PlatformViewHitTestBehavior.Opaque"/>.</remarks>
    public PlatformViewHitTestBehavior HitTestBehavior { get; }

    /// <summary>The text direction to use for the embedded view.</summary>
    /// <remarks>If this is null, the ambient <see cref="Directionality"/> is used instead.</remarks>
    public TextDirection? LayoutDirection { get; }

    /// <summary>Passed as the `arguments` argument of
    /// <c>-[FlutterPlatformViewFactory createWithFrame:viewIdentifier:arguments:]</c>.</summary>
    public object? CreationParams { get; }

    /// <summary>The codec used to encode <see cref="CreationParams"/> before sending it to the platform
    /// side.</summary>
    public IMessageCodec? CreationParamsCodec { get; }

    /// <summary>Which gestures should be forwarded to the UIKit view.</summary>
    /// <remarks>
    /// The gesture recognizers built by factories in this set participate in the gesture arena for
    /// each pointer that was put down on the widget. If any of these recognizers win the gesture arena,
    /// the entire pointer event sequence starting from the pointer down event will be dispatched to the
    /// UIKit view. When null, an empty set of gesture recognizer factories is used.
    /// </remarks>
    public IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? GestureRecognizers { get; }
}

/// <summary>Widget that contains a UIView from the iOS platform.</summary>
/// <remarks>Flutter's <c>UiKitView</c>. See <see cref="AndroidView"/> for the gesture and layout
/// contract.</remarks>
public class UiKitView : DarwinView
{
    /// <summary>Creates a widget that embeds an iOS view.</summary>
    /// <remarks>If <paramref name="creationParams"/> is not null then
    /// <paramref name="creationParamsCodec"/> must not be null.</remarks>
    public UiKitView(
        string viewType,
        UiKitViewGestureBlockingPolicy gestureBlockingPolicy =
            UiKitViewGestureBlockingPolicy.FallbackToPluginDefault,
        PlatformViewCreatedCallback? onPlatformViewCreated = null,
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        TextDirection? layoutDirection = null,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? gestureRecognizers = null,
        Key? key = null)
        : base(
            viewType,
            onPlatformViewCreated,
            hitTestBehavior,
            layoutDirection,
            creationParams,
            creationParamsCodec,
            gestureRecognizers,
            key)
    {
        Debug.Assert(creationParams is null || creationParamsCodec is not null);
        GestureBlockingPolicy = gestureBlockingPolicy;
    }

    /// <summary>The policy that determines how the embedded UIView blocks gestures.</summary>
    public UiKitViewGestureBlockingPolicy GestureBlockingPolicy { get; }

    public override State CreateState() => new UiKitViewState();
}

/// <summary>Widget that contains an NSView from the macOS platform.</summary>
/// <remarks>Flutter's <c>AppKitView</c>.</remarks>
public class AppKitView : DarwinView
{
    /// <summary>Creates a widget that embeds a macOS AppKit NSView.</summary>
    public AppKitView(
        string viewType,
        PlatformViewCreatedCallback? onPlatformViewCreated = null,
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        TextDirection? layoutDirection = null,
        object? creationParams = null,
        IMessageCodec? creationParamsCodec = null,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>>? gestureRecognizers = null,
        Key? key = null)
        : base(
            viewType,
            onPlatformViewCreated,
            hitTestBehavior,
            layoutDirection,
            creationParams,
            creationParamsCodec,
            gestureRecognizers,
            key)
    {
    }

    public override State CreateState() => new AppKitViewState();
}

/// <summary>Embeds an HTML element in the Widget hierarchy in Flutter web.</summary>
/// <remarks>
/// Flutter's <c>HtmlElementView</c>. Plumix has no Flutter Web engine, so it takes Dart's non-web
/// (<c>_html_element_view_io.dart</c>) branch: the widget can be constructed, but building it throws
/// <see cref="NotImplementedException"/> (Dart's <c>UnimplementedError</c>).
/// </remarks>
public class HtmlElementView : StatelessWidget
{
    private const string OnlyOnWeb = "HtmlElementView is only available on Flutter Web";

    /// <summary>Creates a platform view for Flutter web.</summary>
    /// <remarks><paramref name="viewType"/> identifies the type of platform view to create.</remarks>
    public HtmlElementView(
        string viewType,
        PlatformViewCreatedCallback? onPlatformViewCreated = null,
        object? creationParams = null,
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        Key? key = null)
        : base(key)
    {
        ViewType = viewType;
        OnPlatformViewCreated = onPlatformViewCreated;
        CreationParams = creationParams;
        HitTestBehavior = hitTestBehavior;
    }

    /// <summary>Creates a platform view that creates a DOM element specified by
    /// <paramref name="tagName"/>.</summary>
    /// <remarks>Throws off the web, as Dart's <c>_html_element_view_io.dart</c> does.</remarks>
    public static HtmlElementView FromTagName(
        string tagName,
        bool isVisible = true,
        ElementCreatedCallback? onElementCreated = null,
        PlatformViewHitTestBehavior hitTestBehavior = PlatformViewHitTestBehavior.Opaque,
        Key? key = null)
    {
        throw new NotImplementedException(OnlyOnWeb);
    }

    /// <summary>The unique identifier for the HTML view type to be embedded by this widget.</summary>
    public string ViewType { get; }

    /// <summary>Callback to invoke after the platform view has been created.</summary>
    public PlatformViewCreatedCallback? OnPlatformViewCreated { get; }

    /// <summary>Passed as the 2nd argument (i.e. <c>params</c>) of the registered view factory.</summary>
    public object? CreationParams { get; }

    /// <summary>How this widget should behave during hit testing.</summary>
    public PlatformViewHitTestBehavior HitTestBehavior { get; }

    public override Widget Build(BuildContext context) => throw new NotImplementedException(OnlyOnWeb);
}

internal sealed class AndroidViewState : State<AndroidView>
{
    private static readonly IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> EmptyRecognizersSet =
        new HashSet<IFactory<OneSequenceGestureRecognizer>>();

    private int? _id;
    private AndroidViewController? _controller;
    private TextDirection? _layoutDirection;
    private bool _initialized;

    internal FocusNode? FocusNode { get; private set; }

    internal AndroidViewController Controller => _controller!;

    public override Widget Build(BuildContext context)
    {
        return new Focus(
            focusNode: FocusNode,
            onFocusChange: OnFocusChange,
            child: new AndroidPlatformView(
                controller: _controller!,
                hitTestBehavior: Widget.HitTestBehavior,
                gestureRecognizers: Widget.GestureRecognizers ?? EmptyRecognizersSet,
                clipBehavior: Widget.ClipBehavior));
    }

    private void InitializeOnce()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        CreateNewAndroidView();
        FocusNode = new FocusNode(debugLabel: $"AndroidView(id: {_id})");
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        TextDirection newLayoutDirection = FindLayoutDirection();
        bool didChangeLayoutDirection = _layoutDirection != newLayoutDirection;
        _layoutDirection = newLayoutDirection;

        InitializeOnce();
        if (didChangeLayoutDirection)
        {
            // The native view will update asynchronously, in the meantime we don't want
            // to block the framework. (so this is intentionally not awaiting).
            _ = _controller!.SetLayoutDirection(_layoutDirection.Value);
        }
    }

    public override void DidUpdateWidget(AndroidView oldWidget)
    {
        base.DidUpdateWidget(oldWidget);

        TextDirection newLayoutDirection = FindLayoutDirection();
        bool didChangeLayoutDirection = _layoutDirection != newLayoutDirection;
        _layoutDirection = newLayoutDirection;

        if (Widget.ViewType != oldWidget.ViewType)
        {
            _controller!.DisposePostFrame();
            CreateNewAndroidView();
            return;
        }

        if (didChangeLayoutDirection)
        {
            _ = _controller!.SetLayoutDirection(_layoutDirection.Value);
        }
    }

    private TextDirection FindLayoutDirection()
    {
        Debug.Assert(Widget.LayoutDirection is not null || WidgetsDebug.DebugCheckHasDirectionality(Context));
        return Widget.LayoutDirection ?? Directionality.Of(Context);
    }

    public override void Dispose()
    {
        _ = _controller!.Dispose();
        FocusNode?.Dispose();
        FocusNode = null;
        base.Dispose();
    }

    private void CreateNewAndroidView()
    {
        _id = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _controller = PlatformViewsService.InitAndroidView(
            id: _id.Value,
            viewType: Widget.ViewType,
            layoutDirection: _layoutDirection!.Value,
            creationParams: Widget.CreationParams,
            creationParamsCodec: Widget.CreationParamsCodec,
            onFocus: () => FocusNode!.RequestFocus());
        if (Widget.OnPlatformViewCreated is not null)
        {
            _controller.AddOnPlatformViewCreatedListener(Widget.OnPlatformViewCreated);
        }
    }

    private void OnFocusChange(bool isFocused)
    {
        if (!_controller!.IsCreated)
        {
            return;
        }

        if (!isFocused)
        {
            _ = PlatformViewErrors.CatchError(
                _controller.ClearFocus(),
                swallowMissingPlugin: true,
                library: "widgets library",
                context: "while clearing the platform view focus");
            return;
        }

        _ = PlatformViewErrors.CatchError(
            SystemChannels.TextInput.InvokeMethod<object>(
                "TextInput.setPlatformViewClient",
                new Dictionary<string, object?> { ["platformViewId"] = _id }),
            swallowMissingPlugin: true,
            library: "widgets library",
            context: "while setting the platform view client");
    }
}

internal abstract class DarwinViewState<TPlatformView, TController, TRender, TView> : State<TPlatformView>
    where TPlatformView : DarwinView
    where TController : DarwinPlatformViewController
    where TRender : RenderDarwinPlatformView<TController>
    where TView : DarwinPlatformView<TController, TRender>
{
    protected static readonly IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> EmptyRecognizersSet =
        new HashSet<IFactory<OneSequenceGestureRecognizer>>();

    private TController? _controller;
    private TextDirection? _layoutDirection;
    private bool _initialized;

    /// <summary>Dart's <c>@visibleForTesting focusNode</c>.</summary>
    internal FocusNode? FocusNode { get; set; }

    protected TController? Controller => _controller;

    protected TextDirection? LayoutDirectionValue => _layoutDirection;

    public override Widget Build(BuildContext context)
    {
        TController? controller = _controller;
        if (controller is null)
        {
            return SizedBox.Expand();
        }

        return new Focus(
            focusNode: FocusNode,
            onFocusChange: isFocused => OnFocusChange(isFocused, controller),
            child: ChildPlatformView());
    }

    protected abstract TView ChildPlatformView();

    private void InitializeOnce()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _ = CreateNewUiKitView();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        TextDirection newLayoutDirection = FindLayoutDirection();
        bool didChangeLayoutDirection = _layoutDirection != newLayoutDirection;
        _layoutDirection = newLayoutDirection;

        InitializeOnce();
        if (didChangeLayoutDirection)
        {
            // The native view will update asynchronously, in the meantime we don't want
            // to block the framework. (so this is intentionally not awaiting).
            _ = _controller?.SetLayoutDirection(_layoutDirection.Value);
        }
    }

    public override void DidUpdateWidget(TPlatformView oldWidget)
    {
        base.DidUpdateWidget(oldWidget);

        TextDirection newLayoutDirection = FindLayoutDirection();
        bool didChangeLayoutDirection = _layoutDirection != newLayoutDirection;
        _layoutDirection = newLayoutDirection;

        if (Widget.ViewType != oldWidget.ViewType)
        {
            _ = _controller?.Dispose();
            _controller = null;
            FocusNode?.Dispose();
            FocusNode = null;
            _ = CreateNewUiKitView();
            return;
        }

        if (didChangeLayoutDirection)
        {
            _ = _controller?.SetLayoutDirection(_layoutDirection.Value);
        }
    }

    private TextDirection FindLayoutDirection()
    {
        Debug.Assert(Widget.LayoutDirection is not null || WidgetsDebug.DebugCheckHasDirectionality(Context));
        return Widget.LayoutDirection ?? Directionality.Of(Context);
    }

    public override void Dispose()
    {
        _ = _controller?.Dispose();
        _controller = null;
        FocusNode?.Dispose();
        FocusNode = null;
        base.Dispose();
    }

    private async Task CreateNewUiKitView()
    {
        try
        {
            int id = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
            TController controller = await CreateNewViewController(id);
            if (!Mounted)
            {
                _ = controller.Dispose();
                return;
            }

            Widget.OnPlatformViewCreated?.Invoke(id);
            SetState(() =>
            {
                _controller = controller;
                FocusNode = new FocusNode(debugLabel: $"UiKitView(id: {id})");
            });
        }
        catch (Exception error)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: error,
                stack: error.StackTrace,
                library: "widgets",
                context: new ErrorDescription("while creating a Darwin platform view")));
        }
    }

    protected abstract Task<TController> CreateNewViewController(int id);

    private void OnFocusChange(bool isFocused, TController controller)
    {
        if (!isFocused)
        {
            // Unlike Android, we do not need to send "clearFocus" channel message
            // to the engine, because focusing on another view will automatically
            // cancel the focus on the previously focused platform view.
            return;
        }

        _ = PlatformViewErrors.CatchError(
            SystemChannels.TextInput.InvokeMethod<object>(
                "TextInput.setPlatformViewClient",
                new Dictionary<string, object?> { ["platformViewId"] = controller.Id }),
            swallowMissingPlugin: false,
            library: "widgets library",
            context: "while setting the platform view client");
    }
}

internal sealed class UiKitViewState
    : DarwinViewState<UiKitView, UiKitViewController, RenderUiKitView, UiKitPlatformView>
{
    protected override UiKitPlatformView ChildPlatformView()
    {
        return new UiKitPlatformView(
            controller: Controller!,
            hitTestBehavior: Widget.HitTestBehavior,
            gestureRecognizers: Widget.GestureRecognizers ?? EmptyRecognizersSet);
    }

    protected override Task<UiKitViewController> CreateNewViewController(int id)
    {
        return PlatformViewsService.InitUiKitView(
            id: id,
            viewType: Widget.ViewType,
            gestureBlockingPolicy: Widget.GestureBlockingPolicy,
            layoutDirection: LayoutDirectionValue!.Value,
            creationParams: Widget.CreationParams,
            creationParamsCodec: Widget.CreationParamsCodec,
            onFocus: () => FocusNode?.RequestFocus());
    }
}

internal sealed class AppKitViewState
    : DarwinViewState<AppKitView, AppKitViewController, RenderAppKitView, AppKitPlatformView>
{
    protected override AppKitPlatformView ChildPlatformView()
    {
        return new AppKitPlatformView(
            controller: Controller!,
            hitTestBehavior: Widget.HitTestBehavior,
            gestureRecognizers: Widget.GestureRecognizers ?? EmptyRecognizersSet);
    }

    protected override Task<AppKitViewController> CreateNewViewController(int id)
    {
        return PlatformViewsService.InitAppKitView(
            id: id,
            viewType: Widget.ViewType,
            layoutDirection: LayoutDirectionValue!.Value,
            creationParams: Widget.CreationParams,
            creationParamsCodec: Widget.CreationParamsCodec,
            onFocus: () => FocusNode?.RequestFocus());
    }
}

internal sealed class AndroidPlatformView : LeafRenderObjectWidget
{
    public AndroidPlatformView(
        AndroidViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers,
        Clip clipBehavior = Clip.HardEdge)
    {
        Controller = controller;
        HitTestBehavior = hitTestBehavior;
        GestureRecognizers = gestureRecognizers;
        ClipBehavior = clipBehavior;
    }

    public AndroidViewController Controller { get; }

    public PlatformViewHitTestBehavior HitTestBehavior { get; }

    public IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> GestureRecognizers { get; }

    public Clip ClipBehavior { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderAndroidView(
            viewController: Controller,
            hitTestBehavior: HitTestBehavior,
            gestureRecognizers: GestureRecognizers,
            clipBehavior: ClipBehavior);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var renderAndroidView = (RenderAndroidView)renderObject;
        renderAndroidView.Controller = Controller;
        renderAndroidView.HitTestBehavior = HitTestBehavior;
        renderAndroidView.UpdateGestureRecognizers(GestureRecognizers);
        renderAndroidView.ClipBehavior = ClipBehavior;
    }
}

internal abstract class DarwinPlatformView<TController, TRender> : LeafRenderObjectWidget
    where TController : DarwinPlatformViewController
    where TRender : RenderDarwinPlatformView<TController>
{
    protected DarwinPlatformView(
        TController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
    {
        Controller = controller;
        HitTestBehavior = hitTestBehavior;
        GestureRecognizers = gestureRecognizers;
    }

    public TController Controller { get; }

    public PlatformViewHitTestBehavior HitTestBehavior { get; }

    public IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> GestureRecognizers { get; }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var render = (TRender)renderObject;
        render.ViewController = Controller;
        render.HitTestBehavior = HitTestBehavior;
        render.UpdateGestureRecognizers(GestureRecognizers);
    }
}

internal sealed class UiKitPlatformView : DarwinPlatformView<UiKitViewController, RenderUiKitView>
{
    public UiKitPlatformView(
        UiKitViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
        : base(controller, hitTestBehavior, gestureRecognizers)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderUiKitView(
            viewController: Controller,
            hitTestBehavior: HitTestBehavior,
            gestureRecognizers: GestureRecognizers);
}

internal sealed class AppKitPlatformView : DarwinPlatformView<AppKitViewController, RenderAppKitView>
{
    public AppKitPlatformView(
        AppKitViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
        : base(controller, hitTestBehavior, gestureRecognizers)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderAppKitView(
            viewController: Controller,
            hitTestBehavior: HitTestBehavior,
            gestureRecognizers: GestureRecognizers);
}

/// <summary>The parameters used to create a <see cref="PlatformViewController"/>.</summary>
/// <remarks>Flutter's <c>PlatformViewCreationParams</c>.</remarks>
public class PlatformViewCreationParams
{
    internal PlatformViewCreationParams(
        int id,
        string viewType,
        PlatformViewCreatedCallback onPlatformViewCreated,
        Action<bool> onFocusChanged)
    {
        Id = id;
        ViewType = viewType;
        OnPlatformViewCreated = onPlatformViewCreated;
        OnFocusChanged = onFocusChanged;
    }

    /// <summary>The unique identifier for the new platform view.</summary>
    /// <remarks><see cref="PlatformViewController.ViewId"/> should match this id.</remarks>
    public int Id { get; }

    /// <summary>The unique identifier for the type of platform view to be embedded.</summary>
    public string ViewType { get; }

    /// <summary>Callback invoked after the platform view has been created.</summary>
    public PlatformViewCreatedCallback OnPlatformViewCreated { get; }

    /// <summary>Callback invoked when the platform view's focus is changed on the platform side.</summary>
    /// <remarks>The value is true when the platform view gains focus and false when it loses
    /// focus.</remarks>
    public Action<bool> OnFocusChanged { get; }
}

/// <summary>A factory for a surface presenting a platform view as part of the widget hierarchy.</summary>
/// <remarks>Flutter's <c>PlatformViewSurfaceFactory</c>.</remarks>
public delegate Widget PlatformViewSurfaceFactory(BuildContext context, PlatformViewController controller);

/// <summary>Constructs a <see cref="PlatformViewController"/>.</summary>
/// <remarks>
/// Flutter's <c>CreatePlatformViewCallback</c>. The <see cref="PlatformViewController.ViewId"/> field
/// of the created controller must match the value of the params <c>Id</c> field.
/// </remarks>
public delegate PlatformViewController CreatePlatformViewCallback(PlatformViewCreationParams parameters);

/// <summary>Links a platform view with the Flutter framework.</summary>
/// <remarks>
/// Flutter's <c>PlatformViewLink</c>. Provides the necessary hooks for a platform view to be
/// integrated with Flutter's framework: <c>onCreatePlatformView</c> constructs the
/// <see cref="PlatformViewController"/> once the widget is first built, and <c>surfaceFactory</c>
/// builds the widget presenting the platform view once it was created (typically a
/// <see cref="PlatformViewSurface"/>). Until then a placeholder fills the available space and asks
/// the controller to create the view when it has a non-empty size.
/// </remarks>
public class PlatformViewLink : StatefulWidget
{
    /// <summary>Construct a <see cref="PlatformViewLink"/> widget.</summary>
    public PlatformViewLink(
        PlatformViewSurfaceFactory surfaceFactory,
        CreatePlatformViewCallback onCreatePlatformView,
        string viewType,
        Key? key = null)
        : base(key)
    {
        SurfaceFactory = surfaceFactory;
        OnCreatePlatformView = onCreatePlatformView;
        ViewType = viewType;
    }

    internal PlatformViewSurfaceFactory SurfaceFactory { get; }

    internal CreatePlatformViewCallback OnCreatePlatformView { get; }

    /// <summary>The unique identifier for the view type to be embedded.</summary>
    /// <remarks>Typically, this viewType has already been registered on the platform side.</remarks>
    public string ViewType { get; }

    public override State CreateState() => new PlatformViewLinkState();
}

internal sealed class PlatformViewLinkState : State<PlatformViewLink>
{
    private int? _id;
    private PlatformViewController? _controller;
    private bool _platformViewCreated;
    private Widget? _surface;
    private FocusNode? _focusNode;

    public override Widget Build(BuildContext context)
    {
        PlatformViewController? controller = _controller;
        if (controller is null)
        {
            return SizedBox.Expand();
        }

        if (!_platformViewCreated)
        {
            // Depending on the implementation, the first non-empty size can be used
            // to size the platform view.
            return new PlatformViewPlaceHolder(
                onLayout: (size, position) =>
                {
                    if (controller.AwaitingCreation && !size.IsEmpty)
                    {
                        _ = controller.Create(size: size, position: position);
                    }
                });
        }

        _surface ??= Widget.SurfaceFactory(context, controller);
        return new Focus(
            focusNode: _focusNode,
            onFocusChange: HandleFrameworkFocusChanged,
            child: _surface);
    }

    public override void InitState()
    {
        _focusNode = new FocusNode(debugLabel: $"PlatformView(id: {DartIntString(_id)})");
        Initialize();
        base.InitState();
    }

    public override void DidUpdateWidget(PlatformViewLink oldWidget)
    {
        base.DidUpdateWidget(oldWidget);

        if (Widget.ViewType != oldWidget.ViewType)
        {
            _controller?.DisposePostFrame();
            // The _surface has to be recreated as its controller is disposed.
            // Setting _surface to null will trigger its creation in build().
            _surface = null;
            Initialize();
        }
    }

    private void Initialize()
    {
        _id = PlatformViewsRegistry.Instance.GetNextPlatformViewId();
        _controller = Widget.OnCreatePlatformView(new PlatformViewCreationParams(
            id: _id.Value,
            viewType: Widget.ViewType,
            onPlatformViewCreated: OnPlatformViewCreated,
            onFocusChanged: HandlePlatformFocusChanged));
    }

    private void OnPlatformViewCreated(int id)
    {
        if (Mounted)
        {
            SetState(() => _platformViewCreated = true);
        }
    }

    private void HandleFrameworkFocusChanged(bool isFocused)
    {
        if (!isFocused)
        {
            _ = _controller?.ClearFocus();
        }

        _ = PlatformViewErrors.CatchError(
            SystemChannels.TextInput.InvokeMethod<object>(
                "TextInput.setPlatformViewClient",
                new Dictionary<string, object?> { ["platformViewId"] = _id }),
            swallowMissingPlugin: false,
            library: "widget library",
            context: "while handling framework focus changed on platform view");
    }

    private void HandlePlatformFocusChanged(bool isFocused)
    {
        if (isFocused)
        {
            _focusNode!.RequestFocus();
        }
    }

    public override void Dispose()
    {
        _ = _controller?.Dispose();
        _controller = null;
        _focusNode?.Dispose();
        _focusNode = null;
        base.Dispose();
    }

    private static string DartIntString(int? value) => value?.ToString() ?? "null";
}

/// <summary>Integrates a platform view with Flutter's compositor, touch, and semantics
/// subsystems.</summary>
/// <remarks>
/// Flutter's <c>PlatformViewSurface</c>. The compositor integration is done by adding a
/// <see cref="PlatformViewLayer"/> to the layer tree. The widget fills all available space, the parent
/// of this object must provide bounded layout constraints. If the associated platform view is not
/// created the <see cref="PlatformViewSurface"/> does not paint any contents.
/// </remarks>
public class PlatformViewSurface : LeafRenderObjectWidget
{
    /// <summary>Construct a <see cref="PlatformViewSurface"/>.</summary>
    public PlatformViewSurface(
        PlatformViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers,
        Key? key = null)
        : base(key)
    {
        Controller = controller;
        HitTestBehavior = hitTestBehavior;
        GestureRecognizers = gestureRecognizers;
    }

    /// <summary>The controller for the platform view integrated by this
    /// <see cref="PlatformViewSurface"/>.</summary>
    /// <remarks><see cref="PlatformViewController"/> is used for dispatching touch events to the
    /// platform view.</remarks>
    public PlatformViewController Controller { get; }

    /// <summary>Which gestures should be forwarded to the platform view.</summary>
    /// <remarks>See <see cref="AndroidView.GestureRecognizers"/>.</remarks>
    public IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> GestureRecognizers { get; }

    /// <summary>How this widget should behave during hit testing.</summary>
    /// <remarks>This defaults to <see cref="PlatformViewHitTestBehavior.Opaque"/>.</remarks>
    public PlatformViewHitTestBehavior HitTestBehavior { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new PlatformViewRenderBox(
            controller: Controller,
            gestureRecognizers: GestureRecognizers,
            hitTestBehavior: HitTestBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var renderBox = (PlatformViewRenderBox)renderObject;
        renderBox.Controller = Controller;
        renderBox.HitTestBehavior = HitTestBehavior;
        renderBox.UpdateGestureRecognizers(GestureRecognizers);
    }
}

/// <summary>Integrates an Android view with Flutter's compositor, touch, and semantics
/// subsystems.</summary>
/// <remarks>
/// Flutter's <c>AndroidViewSurface</c>. The compositor integration is done by a
/// <see cref="PlatformViewLayer"/> when the controller requires view composition, and a
/// <see cref="TextureLayer"/> otherwise. The widget fills all available space, the parent of this
/// object must provide bounded layout constraints.
/// </remarks>
public class AndroidViewSurface : StatefulWidget
{
    /// <summary>Construct an <see cref="AndroidViewSurface"/>.</summary>
    public AndroidViewSurface(
        AndroidViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers,
        Key? key = null)
        : base(key)
    {
        Controller = controller;
        HitTestBehavior = hitTestBehavior;
        GestureRecognizers = gestureRecognizers;
    }

    /// <summary>The controller for the platform view integrated by this
    /// <see cref="AndroidViewSurface"/>.</summary>
    public AndroidViewController Controller { get; }

    /// <summary>Which gestures should be forwarded to the platform view.</summary>
    public IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> GestureRecognizers { get; }

    /// <summary>How this widget should behave during hit testing.</summary>
    public PlatformViewHitTestBehavior HitTestBehavior { get; }

    public override State CreateState() => new AndroidViewSurfaceState();
}

internal sealed class AndroidViewSurfaceState : State<AndroidViewSurface>
{
    public override void InitState()
    {
        base.InitState();
        if (!Widget.Controller.IsCreated)
        {
            // Schedule a rebuild once creation is complete and the final display
            // type is known.
            Widget.Controller.AddOnPlatformViewCreatedListener(OnPlatformViewCreated);
        }
    }

    public override void Dispose()
    {
        Widget.Controller.RemoveOnPlatformViewCreatedListener(OnPlatformViewCreated);
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        if (Widget.Controller.RequiresViewComposition)
        {
            return new PlatformLayerBasedAndroidViewSurface(
                controller: Widget.Controller,
                hitTestBehavior: Widget.HitTestBehavior,
                gestureRecognizers: Widget.GestureRecognizers);
        }

        return new TextureBasedAndroidViewSurface(
            controller: Widget.Controller,
            hitTestBehavior: Widget.HitTestBehavior,
            gestureRecognizers: Widget.GestureRecognizers);
    }

    private void OnPlatformViewCreated(int _)
    {
        // Trigger a re-build based on the current controller state.
        SetState(() => { });
    }
}

// Displays an Android platform view via TextureLayer.
internal sealed class TextureBasedAndroidViewSurface : PlatformViewSurface
{
    public TextureBasedAndroidViewSurface(
        AndroidViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
        : base(controller, hitTestBehavior, gestureRecognizers)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        var viewController = (AndroidViewController)Controller;
        // Use GL texture based composition.
        // App should use GL texture unless they require to embed a SurfaceView.
        var renderBox = new RenderAndroidView(
            viewController: viewController,
            gestureRecognizers: GestureRecognizers,
            hitTestBehavior: HitTestBehavior);
        viewController.PointTransformer = position => renderBox.GlobalToLocal(position);
        return renderBox;
    }
}

// Displays an Android platform view via PlatformViewLayer.
internal sealed class PlatformLayerBasedAndroidViewSurface : PlatformViewSurface
{
    public PlatformLayerBasedAndroidViewSurface(
        AndroidViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
        : base(controller, hitTestBehavior, gestureRecognizers)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        var viewController = (AndroidViewController)Controller;
        var renderBox = (PlatformViewRenderBox)base.CreateRenderObject(context);
        viewController.PointTransformer = position => renderBox.GlobalToLocal(position);
        return renderBox;
    }
}

/// <summary>A callback used to notify the size of the platform view placeholder.</summary>
/// <remarks>Dart's private <c>_OnLayoutCallback</c>.</remarks>
internal delegate void PlatformViewOnLayoutCallback(Size size, Point position);

/// <summary>A <see cref="RenderBox"/> that notifies its size to the owner after a layout.</summary>
internal sealed class PlatformViewPlaceholderBox : RenderConstrainedBox
{
    public PlatformViewPlaceholderBox(PlatformViewOnLayoutCallback onLayout)
        : base(additionalConstraints: BoxConstraints.TightFor(
            width: double.PositiveInfinity,
            height: double.PositiveInfinity))
    {
        OnLayout = onLayout;
    }

    public PlatformViewOnLayoutCallback OnLayout { get; set; }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        // A call to `localToGlobal` requires waiting for a frame to render first.
        Scheduler.AddPostFrameCallback(
            _ =>
            {
                if (!Attached)
                {
                    return;
                }

                OnLayout(Size, LocalToGlobal(default));
            },
            debugLabel: "PlatformViewPlaceholderBox.onLayout");
    }
}

/// <summary>When a platform view is in the widget hierarchy, this helper is used to observe the size
/// of the platform view.</summary>
internal sealed class PlatformViewPlaceHolder : SingleChildRenderObjectWidget
{
    public PlatformViewPlaceHolder(PlatformViewOnLayoutCallback onLayout)
    {
        OnLayout = onLayout;
    }

    public PlatformViewOnLayoutCallback OnLayout { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new PlatformViewPlaceholderBox(onLayout: OnLayout);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((PlatformViewPlaceholderBox)renderObject).OnLayout = OnLayout;
    }
}

internal static class PlatformViewControllerDisposal
{
    /// <summary>Disposes the controller in a post-frame callback, to allow other widgets to remove
    /// their listeners before the controller is disposed.</summary>
    internal static void DisposePostFrame(this PlatformViewController controller)
    {
        Scheduler.AddPostFrameCallback(
            timeStamp => { _ = controller.Dispose(); },
            debugLabel: "PlatformViewController.dispose");
    }
}

/// <summary>The <c>.catchError</c> handlers platform_view.dart attaches to its channel futures.</summary>
internal static class PlatformViewErrors
{
    internal static async Task CatchError(
        Task task,
        bool swallowMissingPlugin,
        string library,
        string context)
    {
        try
        {
            await task;
        }
        catch (MissingPluginException) when (swallowMissingPlugin)
        {
            // We knowingly ignore errors arising from missing plugins.
        }
        catch (Exception error)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: error,
                stack: error.StackTrace,
                library: library,
                context: new ErrorDescription(context)));
        }
    }
}
