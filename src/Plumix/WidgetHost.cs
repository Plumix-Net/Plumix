using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/binding.dart; flutter/packages/flutter/lib/src/rendering/binding.dart (host integration, adapted)

namespace Plumix;

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
            }

            SetRootChild(null);
            _lastMediaQueryData = null;
            return;
        }

        var effectiveRootWidget = BuildRootWidget(_rootWidget);

        if (_rootElement == null)
        {
            _rootElement = new RootElement(this, effectiveRootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
        }
        else
        {
            _rootElement.Update(effectiveRootWidget);
        }
    }

    protected override void OnDrawFrame(TimeSpan timestamp)
    {
        _owner.BuildScope();
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
        _rootElement.Update(new MediaQuery(data: nextData, child: _rootWidget));
    }

    private Widget BuildRootWidget(Widget rootWidget)
    {
        var data = GetMediaQueryData();
        _lastMediaQueryData = data;
        return new MediaQuery(data: data, child: rootWidget);
    }

    private sealed class RootElement : Element, IRenderObjectHost
    {
        private readonly WidgetHost _host;
        private Element? _child;

        public RootElement(WidgetHost host, Widget widget) : base(widget)
        {
            _host = host;
        }

        public override RenderObject? RenderObject => _child?.RenderObject;

        internal override Element? RenderObjectAttachingChild => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(child, _child))
            {
                _child = null;
            }
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("RootElement expects null slot.");
            }

            if (child is RenderBox renderBox)
            {
                _host.SetRootChild(renderBox);
                return;
            }

            throw new InvalidOperationException("RootElement can host only RenderBox.");
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("RootElement does not support non-null slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("RootElement expects null slot.");
            }

            if (child is RenderBox renderBox && ReferenceEquals(_host.RootChild, renderBox))
            {
                _host.SetRootChild(null);
            }
        }

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }
}
