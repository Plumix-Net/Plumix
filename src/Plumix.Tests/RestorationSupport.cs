using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>Mirrors Flutter's <c>MockRestorationManager</c> from <c>test/services/restoration.dart</c>.</summary>
internal sealed class MockRestorationManager : RestorationManager
{
    public List<RestorationBucket> Scheduled { get; } = [];

    protected override void InitChannels()
    {
    }

    public override void ScheduleSerializationFor(RestorationBucket bucket) => Scheduled.Add(bucket);

    public override void UnscheduleSerializationFor(RestorationBucket bucket) => Scheduled.Remove(bucket);

    public void DoSerialization()
    {
        try
        {
            foreach (RestorationBucket bucket in Scheduled)
            {
                bucket.FinalizeBucket();
            }
        }
        finally
        {
            Scheduled.Clear();
        }
    }

    public override string ToString() => "MockManager";
}

/// <summary>Mirrors Flutter's <c>TestRestorationManager</c>: keeps the restoration data in memory.</summary>
internal sealed class TestRestorationManager : RestorationManager
{
    public bool AnswerSynchronously { get; set; } = true;

    public bool Enabled { get; set; } = true;

    public Dictionary<object, object?>? Data { get; set; }

    public int RootBucketAccessed { get; private set; }

    public bool EngineQueried { get; private set; }

    public List<Dictionary<object, object?>> SentToEngine { get; } = [];

    protected override void InitChannels()
    {
    }

    public override void GetRootBucket(Action<RestorationBucket?> callback)
    {
        RootBucketAccessed++;
        base.GetRootBucket(callback);
    }

    /// <summary>Answers a pending root-bucket request, or replaces the data of a running app.</summary>
    public void RespondWith(bool enabled, Dictionary<object, object?>? data)
    {
        Enabled = enabled;
        Data = data;
        HandleRestorationUpdateFromEngine(enabled, Encode(data));
    }

    protected override void GetRootBucketFromEngine()
    {
        EngineQueried = true;
        if (AnswerSynchronously)
        {
            HandleRestorationUpdateFromEngine(Enabled, Encode(Data));
        }
    }

    protected override void SendToEngine(byte[] encodedData)
    {
        SentToEngine.Add((Dictionary<object, object?>)DecodeRestorationData(encodedData)!);
    }

    private static byte[]? Encode(Dictionary<object, object?>? data) =>
        data is null ? null : EncodeRestorationData(data);
}

/// <summary>Helpers for building and inspecting the raw restoration data maps.</summary>
internal static class RawRestorationData
{
    public static Dictionary<object, object?> Build(
        Dictionary<object, object?>? values = null,
        Dictionary<object, object?>? children = null)
    {
        var data = new Dictionary<object, object?>();
        if (values is not null)
        {
            data["v"] = values;
        }

        if (children is not null)
        {
            data["c"] = children;
        }

        return data;
    }

    public static Dictionary<object, object?>? Values(IDictionary<object, object?> rawData)
    {
        return rawData.TryGetValue("v", out object? values) ? values as Dictionary<object, object?> : null;
    }

    public static Dictionary<object, object?>? Children(IDictionary<object, object?> rawData)
    {
        return rawData.TryGetValue("c", out object? children) ? children as Dictionary<object, object?> : null;
    }

    public static Dictionary<object, object?>? Child(IDictionary<object, object?> rawData, string id)
    {
        return Children(rawData)?.GetValueOrDefault(id) as Dictionary<object, object?>;
    }
}

/// <summary>Mirrors Flutter's <c>BucketSpy</c>: reports the bucket visible at its position.</summary>
internal sealed class BucketSpy : StatefulWidget
{
    public BucketSpy(Action<RestorationBucket?> onBucket, Widget? child = null, Key? key = null) : base(key)
    {
        OnBucket = onBucket;
        Child = child;
    }

    public Action<RestorationBucket?> OnBucket { get; }

    public Widget? Child { get; }

    public override State CreateState() => new BucketSpyState();

    private sealed class BucketSpyState : State
    {
        private BucketSpy CurrentWidget => (BucketSpy)StateWidget;

        public override Widget Build(BuildContext context)
        {
            CurrentWidget.OnBucket(RestorationScope.MaybeOf(context));
            return CurrentWidget.Child ?? new SizedBox(width: 0.0, height: 0.0);
        }
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that records every call made against it.</summary>
internal sealed class TestRestorableProperty : RestorableProperty<int>
{
    private readonly int _defaultValue;
    private bool _enabled = true;
    private int _value;

    public TestRestorableProperty(int defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public List<string> Log { get; } = [];

    public override bool Enabled => _enabled;

    public int Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            NotifyListeners();
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (_enabled == enabled)
        {
            return;
        }

        _enabled = enabled;
        NotifyListeners();
    }

    public override int CreateDefaultValue()
    {
        Log.Add("createDefaultValue");
        return _defaultValue;
    }

    public override int FromPrimitives(object? data)
    {
        Log.Add("fromPrimitives");
        return (int)data!;
    }

    public override void InitWithValue(int value)
    {
        Log.Add("initWithValue");
        _value = value;
    }

    public override object? ToPrimitives()
    {
        Log.Add("toPrimitives");
        return _value;
    }
}

/// <summary>A stateful widget whose state uses <see cref="RestorationState"/>.</summary>
internal sealed class RestorableWidget : StatefulWidget
{
    public RestorableWidget(
        string? restorationId,
        TestRestorableProperty property,
        Action<RestorableWidgetState>? onStateCreated = null,
        Key? key = null) : base(key)
    {
        RestorationId = restorationId;
        Property = property;
        OnStateCreated = onStateCreated;
    }

    public string? RestorationId { get; }

    public TestRestorableProperty Property { get; }

    public Action<RestorableWidgetState>? OnStateCreated { get; }

    public override State CreateState() => new RestorableWidgetState();
}

internal sealed class RestorableWidgetState : RestorationState
{
    public List<RestorationBucket?> RestoreStateLog { get; } = [];

    public List<bool> InitialRestoreLog { get; } = [];

    public List<RestorationBucket?> ToggleBucketLog { get; } = [];

    public RestorableWidget CurrentWidget => (RestorableWidget)StateWidget;

    protected override string? RestorationId => CurrentWidget.RestorationId;

    public void RegisterAdditional(RestorableProperty property, string id) =>
        RegisterForRestoration(property, id);

    public void UnregisterAdditional(RestorableProperty property) => UnregisterFromRestoration(property);

    public override void InitState()
    {
        base.InitState();
        CurrentWidget.OnStateCreated?.Invoke(this);
    }

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RestoreStateLog.Add(oldBucket);
        InitialRestoreLog.Add(initialRestore);
        RegisterForRestoration(CurrentWidget.Property, "foo");
    }

    protected override void DidToggleBucket(RestorationBucket? oldBucket)
    {
        base.DidToggleBucket(oldBucket);
        ToggleBucketLog.Add(oldBucket);
    }

    public override Widget Build(BuildContext context) => new SizedBox(width: 0.0, height: 0.0);
}

/// <summary>Minimal element host used by the restoration widget tests.</summary>
internal sealed class RestorationHarness : IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly HarnessRootElement _root;
    private readonly PipelineOwner _pipeline;

    public RestorationHarness(Widget widget)
    {
        RenderView = new RenderView();
        _pipeline = new PipelineOwner(RenderView);
        _pipeline.Attach(RenderView);
        _root = new HarnessRootElement(RenderView, widget);
        _root.Attach(_owner);
        _root.Mount(parent: null, newSlot: null);
        _owner.FlushBuild();
    }

    public RenderView RenderView { get; }

    public void FlushBuild() => _owner.FlushBuild();

    public void Update(Widget widget)
    {
        _root.UpdateWidget(widget);
        _owner.FlushBuild();
    }

    public void Pump(Size size)
    {
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(size);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
    }

    public T? FindWidget<T>() where T : Widget => FindWidget<T>(_root);

    public void Dispose()
    {
        _root.Unmount();
        Scheduler.PumpFrameForTests();
    }

    private static T? FindWidget<T>(Element element) where T : Widget
    {
        if (element.Widget is T widget)
        {
            return widget;
        }

        T? result = null;
        element.VisitChildren(child => result ??= FindWidget<T>(child));
        return result;
    }

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
        {
            _renderView = renderView;
        }

        public override RenderObject? RenderObject => _child?.RenderObject;

        public override Element? RenderObjectAttachingChild => _child;

        public void UpdateWidget(Widget widget) => Update(widget);

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            _renderView.Child = (RenderBox)child;
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child))
            {
                _renderView.Child = null;
            }
        }
    }
}
