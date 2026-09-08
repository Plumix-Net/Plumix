using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/binding.dart; flutter/packages/flutter/lib/src/rendering/binding.dart (host integration, adapted)

namespace Plumix;

public sealed class WidgetHost : PlumixHost, IRootRenderObjectHost
{
    private readonly BuildOwner _owner = new();
    private RootElement? _rootElement;
    private Widget? _rootWidget;
    private MediaQueryData? _lastMediaQueryData;
    private FlutterView? _view;

    public WidgetHost()
    {
        _owner.OnBuildScheduled = ScheduleVisualUpdate;
    }

    public Widget? RootWidget
    {
        get => _rootWidget;
        set
        {
            if (ReferenceEquals(_rootWidget, value))
            {
                return;
            }

            _rootWidget = value;
            InitializeOrUpdate();
        }
    }

    private void InitializeOrUpdate()
    {
        if (_rootWidget == null)
        {
            if (_rootElement != null)
            {
                _rootElement.Unmount();
                _rootElement = null;
            }

            SetRootChild(null);
            _lastMediaQueryData = null;
            return;
        }

        AttachRootWidget(_rootWidget);
    }

    /// <summary>
    /// Wraps <paramref name="rootWidget"/> in a <see cref="Widgets.RootWidget"/> and attaches it to
    /// <see cref="BuildOwner"/>, creating the root element the first time.
    /// </summary>
    /// <remarks>Dart's <c>WidgetsBinding.attachRootWidget</c>.</remarks>
    private void AttachRootWidget(Widget rootWidget)
    {
        AttachToBuildOwner(new RootWidget(
            child: BuildRootWidget(rootWidget),
            debugShortDescription: "[root]",
            renderObjectHost: this));
    }

    /// <summary>Dart's <c>WidgetsBinding.attachToBuildOwner</c>.</summary>
    private void AttachToBuildOwner(RootWidget widget)
    {
        bool isBootstrapFrame = _rootElement is null;
        _rootElement = widget.Attach(_owner, _rootElement);
        if (isBootstrapFrame)
        {
            ScheduleVisualUpdate();
        }
    }

    /// <inheritdoc />
    void IRootRenderObjectHost.AttachRootRenderObject(RenderObject? child)
    {
        switch (child)
        {
            case null:
                SetRootChild(null);
                return;
            case RenderBox renderBox:
                SetRootChild(renderBox);
                return;
            default:
                throw new InvalidOperationException("RootElement can host only RenderBox.");
        }
    }

    protected override void OnDrawFrame(TimeSpan timestamp)
    {
        _owner.BuildScope();
    }

    protected override void PerformReassemble()
    {
        if (_rootElement != null)
        {
            _owner.Reassemble(_rootElement);
        }

        base.PerformReassemble();
    }

    protected override void OnMetricsChanged()
    {
        base.OnMetricsChanged();

        if (_rootWidget == null || _rootElement == null)
        {
            return;
        }

        var nextData = GetMediaQueryData();
        if (_lastMediaQueryData == nextData)
        {
            return;
        }

        _lastMediaQueryData = nextData;
        AttachRootWidget(_rootWidget);
    }

    private Widget BuildRootWidget(Widget rootWidget)
    {
        var data = GetMediaQueryData();
        _lastMediaQueryData = data;
        _view ??= new FlutterView(data.PhysicalSize, data.DevicePixelRatio, data.ViewId);
        _view.UpdateMetrics(data.PhysicalSize, data.DevicePixelRatio, data.ViewId);
        RootFlutterView = _view;
        return new View(
            view: _view,
            child: new MediaQuery(
                data: data,
                child: rootWidget));
    }
}
