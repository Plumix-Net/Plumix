using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/binding.dart; flutter/packages/flutter/lib/src/rendering/binding.dart (host integration, adapted)

namespace Plumix;

/// <summary>
/// A host that runs a widget tree: Plumix's <c>WidgetsBinding</c> half of the binding, per host.
/// </summary>
/// <remarks>
/// <see cref="RootWidget"/> is attached the way <c>runApp</c> does it: wrapped in the implicit
/// <see cref="View"/> over the host's own pipeline owner and render view
/// (<c>WidgetsBinding.wrapWithDefaultView</c>), then in a <see cref="Widgets.RootWidget"/>
/// (<c>attachRootWidget</c>). The platform-level <see cref="MediaQueryData"/> the host computes
/// sits above the view, where Flutter's <c>MediaQuery.fromView</c> picks it up as the platform
/// data; the view-level data comes from <see cref="PlumixHost.RootFlutterView"/>.
/// </remarks>
public sealed class WidgetHost : PlumixHost
{
    private readonly BuildOwner _owner = new();
    private RootElement? _rootElement;
    private Widget? _rootWidget;
    private MediaQueryData? _lastMediaQueryData;

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
                Pipeline.RootLayer.RemoveAllChildren();
                ScheduleVisualUpdate();
            }

            _lastMediaQueryData = null;
            return;
        }

        AttachRootWidget(_rootWidget);
    }

    /// <summary>
    /// Wraps <paramref name="rootWidget"/> in the implicit view and a <see cref="Widgets.RootWidget"/>
    /// and attaches it to the <see cref="BuildOwner"/>, creating the root element the first time.
    /// </summary>
    /// <remarks>Dart's <c>WidgetsBinding.attachRootWidget</c> over <c>wrapWithDefaultView</c>.</remarks>
    private void AttachRootWidget(Widget rootWidget)
    {
        AttachToBuildOwner(new RootWidget(
            child: WrapWithDefaultView(rootWidget),
            debugShortDescription: "[root]"));
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

    /// <summary>Dart's <c>WidgetsBinding.wrapWithDefaultView</c>, over this host's implicit view.</summary>
    private Widget WrapWithDefaultView(Widget rootWidget)
    {
        var data = GetMediaQueryData();
        _lastMediaQueryData = data;
        return new MediaQuery(
            data: data,
            child: new View(
                view: RootFlutterView,
                child: rootWidget,
                deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner: Pipeline,
                deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView: RootRenderView));
    }
}
