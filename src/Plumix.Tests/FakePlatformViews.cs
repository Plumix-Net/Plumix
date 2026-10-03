using System.Collections;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Flutter's test/services/fake_platform_views.dart: mocks of the platform side of
// `SystemChannels.PlatformViews` and fake controllers for the platform view tests.

/// <summary>Used in internal testing.</summary>
/// <remarks>Flutter's <c>FakePlatformViewController</c>.</remarks>
internal sealed class FakePlatformViewController(int viewId) : PlatformViewController
{
    public bool Disposed { get; private set; }

    public bool FocusCleared { get; private set; }

    /// <summary>Events dispatched to this platform view.</summary>
    public List<PointerEvent> DispatchedPointerEvents { get; } = [];

    public override int ViewId { get; } = viewId;

    public override Task DispatchPointerEvent(PointerEvent @event)
    {
        DispatchedPointerEvents.Add(@event);
        return Task.CompletedTask;
    }

    public void ClearTestingVariables()
    {
        DispatchedPointerEvents.Clear();
        Disposed = false;
        FocusCleared = false;
    }

    public override Task Dispose()
    {
        Disposed = true;
        return Task.CompletedTask;
    }

    public override Task ClearFocus()
    {
        FocusCleared = true;
        return Task.CompletedTask;
    }
}

/// <summary>Flutter's <c>FakeAndroidViewController</c>.</summary>
/// <remarks>Dart <c>implements AndroidViewController</c>; C# derives from it and overrides every
/// member, so no base behavior runs.</remarks>
internal sealed class FakeAndroidViewController : AndroidViewController
{
    private readonly List<PlatformViewCreatedCallback> _createdCallbacks = [];
    private bool _createCalledSuccessfully;
    private PointTransformer? _pointTransformer;

    public FakeAndroidViewController(int viewId, bool requiresSize = false, bool requiresViewComposition = false)
        : base(viewId, "fake", TextDirection.Ltr)
    {
        RequiresSize = requiresSize;
        RequiresViewCompositionValue = requiresViewComposition;
    }

    public bool Disposed { get; private set; }

    public bool FocusCleared { get; private set; }

    public bool Created { get; private set; }

    // If true, [create] won't be considered to have been called successfully
    // unless it includes a size.
    public bool RequiresSize { get; set; }

    // Offset passed to the last successful call to create.
    public Point? CreatePosition { get; private set; }

    /// <summary>Events dispatched to this platform view.</summary>
    public List<PointerEvent> DispatchedPointerEvents { get; } = [];

    public bool RequiresViewCompositionValue { get; set; }

    public bool PointTransformerIsSet => _pointTransformer is not null;

    public override PointTransformer PointTransformer
    {
        get => _pointTransformer
            ?? throw new InvalidOperationException("Field 'pointTransformer' has not been initialized.");
        set => _pointTransformer = value;
    }

    public override Task DispatchPointerEvent(PointerEvent @event)
    {
        DispatchedPointerEvents.Add(@event);
        return Task.CompletedTask;
    }

    public void ClearTestingVariables()
    {
        DispatchedPointerEvents.Clear();
        Disposed = false;
        FocusCleared = false;
    }

    public override Task Dispose()
    {
        Disposed = true;
        return Task.CompletedTask;
    }

    public override Task ClearFocus()
    {
        FocusCleared = true;
        return Task.CompletedTask;
    }

    public override Task<Size> SetSize(Size size) => Task.FromResult(size);

    public override Task SetOffset(Point off) => Task.CompletedTask;

    public override int? TextureId => 0;

    public override bool AwaitingCreation => !_createCalledSuccessfully;

    public override bool IsCreated => Created;

    public override void AddOnPlatformViewCreatedListener(PlatformViewCreatedCallback listener)
    {
        Created = true;
        _createdCallbacks.Add(listener);
    }

    public override void RemoveOnPlatformViewCreatedListener(PlatformViewCreatedCallback listener)
    {
        _createdCallbacks.Remove(listener);
    }

    public override Task SendMotionEvent(AndroidMotionEvent @event) => throw new NotImplementedException();

    public override Task SetLayoutDirection(TextDirection layoutDirection) =>
        throw new NotImplementedException();

    public override Task Create(Size? size = null, Point? position = null)
    {
        Assert.False(_createCalledSuccessfully);
        if (RequiresSize && size is not null)
        {
            Assert.False(size.Value.IsEmpty);
        }

        _createCalledSuccessfully = (size is not null && position is not null) || !RequiresSize;
        CreatePosition = position;
        return Task.CompletedTask;
    }

    public override IList<PlatformViewCreatedCallback> CreatedCallbacks => _createdCallbacks;

    public override bool RequiresViewComposition => RequiresViewCompositionValue;

    private protected override Task SendDisposeMessage() => throw new NotImplementedException();

    private protected override bool CreateRequiresSize => throw new NotImplementedException();

    private protected override Task SendCreateMessage(Size? size, Point? position = null) =>
        throw new NotImplementedException();

    private protected override Task<Size> SendResizeMessage(Size size) => throw new NotImplementedException();
}

/// <summary>The decoding helpers every fake handler shares.</summary>
internal static class FakePlatformViewArgs
{
    /// <summary>
    /// Resumes in the next microtask, the way every Dart <c>await</c> does. Plumix's mock messenger and
    /// <see cref="TaskCompletionSource"/> resume synchronously; fakes created with
    /// <c>dartAsyncReplies: true</c> hop through <see cref="Scheduler.ScheduleMicrotask"/> instead, so a
    /// reply lands after the frame that sent the call, as platform_view_test.dart expects.
    /// </summary>
    public static Task NextMicrotask()
    {
        var resumed = new TaskCompletionSource();
        Scheduler.ScheduleMicrotask(resumed.SetResult);
        return resumed.Task;
    }

    public static int Int(object? value) => Convert.ToInt32(value);

    public static double? Double(object? value) => value is null ? null : Convert.ToDouble(value);

    public static IDictionary Map(MethodCall call) => (IDictionary)call.Arguments!;

    /// <summary>flutter_test's <c>handlePlatformMessage</c> with a <c>viewFocused</c> call.</summary>
    public static void InvokeViewFocused(int viewId)
    {
        ByteData data = SystemChannels.PlatformViews.Codec.EncodeMethodCall(new MethodCall("viewFocused", viewId));
        _ = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.PlatformViews.Name,
            data,
            _ => { });
    }
}

/// <summary>Flutter's <c>FakeAndroidPlatformViewsController</c>.</summary>
internal sealed class FakeAndroidPlatformViewsController : IDisposable
{
    private readonly Dictionary<int, FakeAndroidPlatformView> _views = [];
    private readonly HashSet<string> _registeredViewTypes = [];
    private int _textureCounter;

    public FakeAndroidPlatformViewsController(bool dartAsyncReplies = false)
    {
        DartAsyncReplies = dartAsyncReplies;
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(OnMethodCall);
    }

    /// <summary>Whether awaits resume in a later microtask, as in Dart (C#-only).</summary>
    public bool DartAsyncReplies { get; }

    public IEnumerable<FakeAndroidPlatformView> Views => _views.Values;

    public Dictionary<int, List<FakeAndroidMotionEvent>> MotionEvents { get; } = [];

    public TaskCompletionSource? ResizeCompleter { get; set; }

    public TaskCompletionSource? CreateCompleter { get; set; }

    public int? LastClearedFocusViewId { get; set; }

    public Dictionary<int, Point> Offsets { get; } = [];

    /// <summary>True if Texture Layer Hybrid Composition mode should be enabled.</summary>
    /// <remarks>When false, <c>create</c> will simulate the engine's fallback mode.</remarks>
    public bool AllowTextureLayerMode { get; set; } = true;

    public void RegisterViewType(string viewType) => _registeredViewTypes.Add(viewType);

    public void InvokeViewFocused(int viewId) => FakePlatformViewArgs.InvokeViewFocused(viewId);

    public void Dispose() => SystemChannels.PlatformViews.SetPlatformMethodCallHandler(null);

    /// <summary>
    /// Holds every reply back until completed. Dart's mock messenger runs the handler synchronously but
    /// always delivers the reply in a later microtask; Plumix's delivers it synchronously, so tests that
    /// depend on Dart's interleaving (a <c>dispose</c> while <c>create</c> awaits) set this. C#-only.
    /// </summary>
    public TaskCompletionSource? ReplyDelay { get; set; }

    private async Task<object?> OnMethodCall(MethodCall call)
    {
        Task<object?> reply = Handle(call);
        if (ReplyDelay is not null)
        {
            await ReplyDelay.Task;
        }

        object? result = await reply;
        if (DartAsyncReplies)
        {
            await FakePlatformViewArgs.NextMicrotask();
        }

        return result;
    }

    private Task<object?> Handle(MethodCall call)
    {
        return call.Method switch
        {
            "create" => Create(call),
            "dispose" => DisposeView(call),
            "resize" => Resize(call),
            "touch" => Touch(call),
            "setDirection" => SetDirection(call),
            "clearFocus" => ClearFocus(call),
            "offset" => Offset(call),
            _ => Task.FromResult<object?>(null),
        };
    }

    private async Task<object?> Create(MethodCall call)
    {
        IDictionary args = FakePlatformViewArgs.Map(call);
        int id = FakePlatformViewArgs.Int(args["id"]);
        string viewType = (string)args["viewType"]!;
        double? width = FakePlatformViewArgs.Double(args.Contains("width") ? args["width"] : null);
        double? height = FakePlatformViewArgs.Double(args.Contains("height") ? args["height"] : null);
        int layoutDirection = FakePlatformViewArgs.Int(args["direction"]);
        bool? hybrid = args.Contains("hybrid") ? (bool?)args["hybrid"] : null;
        bool? hybridFallback = args.Contains("hybridFallback") ? (bool?)args["hybridFallback"] : null;
        byte[]? creationParams = args.Contains("params") ? (byte[]?)args["params"] : null;
        double? top = FakePlatformViewArgs.Double(args.Contains("top") ? args["top"] : null);
        double? left = FakePlatformViewArgs.Double(args.Contains("left") ? args["left"] : null);

        if (_views.ContainsKey(id))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to create an already created platform view, view id: {id}");
        }

        if (!_registeredViewTypes.Contains(viewType))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to create a platform view of unregistered type: {viewType}");
        }

        if (CreateCompleter is not null)
        {
            await CreateCompleter.Task;
            if (DartAsyncReplies)
            {
                await FakePlatformViewArgs.NextMicrotask();
            }
        }

        _views[id] = new FakeAndroidPlatformView(
            id,
            viewType,
            width is not null && height is not null ? new Size(width.Value, height.Value) : null,
            layoutDirection,
            hybrid: hybrid,
            hybridFallback: hybridFallback,
            creationParams: creationParams,
            position: left is not null && top is not null ? new Point(left.Value, top.Value) : null);

        // Return a hybrid result (null rather than a texture ID) if:
        bool hybridResult =
            // hybrid was explicitly requested, or
            (hybrid ?? false)
            // hybrid fallback was requested and simulated.
            || (!AllowTextureLayerMode && (hybridFallback ?? false));
        if (hybridResult)
        {
            return null;
        }

        return _textureCounter++;
    }

    private Task<object?> DisposeView(MethodCall call)
    {
        Assert.IsAssignableFrom<IDictionary>(call.Arguments);
        IDictionary args = FakePlatformViewArgs.Map(call);
        int id = FakePlatformViewArgs.Int(args["id"]);
        bool hybrid = (bool)args["hybrid"]!;

        if (hybrid && !_views[id].Hybrid!.Value)
        {
            throw new ArgumentException(
                "An AndroidViewController using hybrid composition must pass `hybrid: true`");
        }
        else if (!hybrid && (_views[id].Hybrid ?? false))
        {
            throw new ArgumentException(
                "An AndroidViewController not using hybrid composition must pass `hybrid: false`");
        }

        if (!_views.ContainsKey(id))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to dispose a platform view with unknown id: {id}");
        }

        _views.Remove(id);
        return Task.FromResult<object?>(null);
    }

    private async Task<object?> Resize(MethodCall call)
    {
        IDictionary args = FakePlatformViewArgs.Map(call);
        int id = FakePlatformViewArgs.Int(args["id"]);
        double width = FakePlatformViewArgs.Double(args["width"])!.Value;
        double height = FakePlatformViewArgs.Double(args["height"])!.Value;

        if (!_views.ContainsKey(id))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to resize a platform view with unknown id: {id}");
        }

        if (ResizeCompleter is not null)
        {
            await ResizeCompleter.Task;
            if (DartAsyncReplies)
            {
                await FakePlatformViewArgs.NextMicrotask();
            }
        }

        _views[id] = _views[id].CopyWith(size: new Size(width, height));

        return new Dictionary<string, object?> { ["width"] = width, ["height"] = height };
    }

    private Task<object?> Offset(MethodCall call)
    {
        IDictionary args = FakePlatformViewArgs.Map(call);
        int id = FakePlatformViewArgs.Int(args["id"]);
        double top = FakePlatformViewArgs.Double(args["top"])!.Value;
        double left = FakePlatformViewArgs.Double(args["left"])!.Value;
        Offsets[id] = new Point(left, top);
        return Task.FromResult<object?>(null);
    }

    private Task<object?> Touch(MethodCall call)
    {
        var args = (IList)call.Arguments!;
        int id = FakePlatformViewArgs.Int(args[0]);
        int action = FakePlatformViewArgs.Int(args[3]);
        var pointerProperties = ((IList)args[5]!).Cast<IList>().ToList();
        var pointerCoords = ((IList)args[6]!).Cast<IList>().ToList();
        var pointerIds = new List<int>();
        var pointerOffsets = new List<Point>();
        for (int i = 0; i < pointerCoords.Count; i++)
        {
            pointerIds.Add(FakePlatformViewArgs.Int(pointerProperties[i][0]));
            double x = FakePlatformViewArgs.Double(pointerCoords[i][7])!.Value;
            double y = FakePlatformViewArgs.Double(pointerCoords[i][8])!.Value;
            pointerOffsets.Add(new Point(x, y));
        }

        if (!MotionEvents.TryGetValue(id, out List<FakeAndroidMotionEvent>? events))
        {
            events = [];
            MotionEvents[id] = events;
        }

        events.Add(new FakeAndroidMotionEvent(action, pointerIds, pointerOffsets));
        return Task.FromResult<object?>(null);
    }

    private Task<object?> SetDirection(MethodCall call)
    {
        IDictionary args = FakePlatformViewArgs.Map(call);
        int id = FakePlatformViewArgs.Int(args["id"]);
        int layoutDirection = FakePlatformViewArgs.Int(args["direction"]);

        if (!_views.ContainsKey(id))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to resize a platform view with unknown id: {id}");
        }

        _views[id] = _views[id].CopyWith(layoutDirection: layoutDirection);

        return Task.FromResult<object?>(null);
    }

    private Task<object?> ClearFocus(MethodCall call)
    {
        int id = FakePlatformViewArgs.Int(call.Arguments);

        if (!_views.ContainsKey(id))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to clear the focus on a platform view with unknown id: {id}");
        }

        LastClearedFocusViewId = id;
        return Task.FromResult<object?>(null);
    }
}

/// <summary>Flutter's <c>FakeIosPlatformViewsController</c> and
/// <c>FakeMacosPlatformViewsController</c>, which differ only in the record they store.</summary>
internal abstract class FakeDarwinPlatformViewsController<TView> : IDisposable
{
    private readonly Dictionary<int, TView> _views = [];
    private readonly HashSet<string> _registeredViewTypes = [];

    protected FakeDarwinPlatformViewsController(bool dartAsyncReplies)
    {
        DartAsyncReplies = dartAsyncReplies;
        SystemChannels.PlatformViews.SetPlatformMethodCallHandler(OnMethodCall);
    }

    /// <summary>Whether awaits resume in a later microtask, as in Dart (C#-only).</summary>
    public bool DartAsyncReplies { get; }

    public IEnumerable<TView> Views => _views.Values;

    // When this completer is non null, the 'create' method channel call will be
    // delayed until it completes.
    public TaskCompletionSource? CreationDelay { get; set; }

    // Maps a view id to the number of gestures it accepted so far.
    public Dictionary<int, int> GesturesAccepted { get; } = [];

    // Maps a view id to the number of gestures it rejected so far.
    public Dictionary<int, int> GesturesRejected { get; } = [];

    public void RegisterViewType(string viewType) => _registeredViewTypes.Add(viewType);

    public void InvokeViewFocused(int viewId) => FakePlatformViewArgs.InvokeViewFocused(viewId);

    public void Dispose() => SystemChannels.PlatformViews.SetPlatformMethodCallHandler(null);

    protected abstract TView CreateView(int id, string viewType, byte[]? creationParams);

    private async Task<object?> OnMethodCall(MethodCall call)
    {
        object? result = await Handle(call);
        if (DartAsyncReplies)
        {
            await FakePlatformViewArgs.NextMicrotask();
        }

        return result;
    }

    private Task<object?> Handle(MethodCall call)
    {
        return call.Method switch
        {
            "create" => Create(call),
            "dispose" => DisposeView(call),
            "acceptGesture" => AcceptGesture(call),
            "rejectGesture" => RejectGesture(call),
            _ => Task.FromResult<object?>(null),
        };
    }

    private async Task<object?> Create(MethodCall call)
    {
        if (CreationDelay is not null)
        {
            await CreationDelay.Task;
            if (DartAsyncReplies)
            {
                await FakePlatformViewArgs.NextMicrotask();
            }
        }

        IDictionary args = FakePlatformViewArgs.Map(call);
        int id = FakePlatformViewArgs.Int(args["id"]);
        string viewType = (string)args["viewType"]!;
        byte[]? creationParams = args.Contains("params") ? (byte[]?)args["params"] : null;

        if (_views.ContainsKey(id))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to create an already created platform view, view id: {id}");
        }

        if (!_registeredViewTypes.Contains(viewType))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to create a platform view of unregistered type: {viewType}");
        }

        _views[id] = CreateView(id, viewType, creationParams);
        GesturesAccepted[id] = 0;
        GesturesRejected[id] = 0;
        return null;
    }

    private Task<object?> AcceptGesture(MethodCall call)
    {
        int id = FakePlatformViewArgs.Int(FakePlatformViewArgs.Map(call)["id"]);
        GesturesAccepted[id] = GesturesAccepted[id] + 1;
        return Task.FromResult<object?>(null);
    }

    private Task<object?> RejectGesture(MethodCall call)
    {
        int id = FakePlatformViewArgs.Int(FakePlatformViewArgs.Map(call)["id"]);
        GesturesRejected[id] = GesturesRejected[id] + 1;
        return Task.FromResult<object?>(null);
    }

    private Task<object?> DisposeView(MethodCall call)
    {
        int id = FakePlatformViewArgs.Int(call.Arguments);

        if (!_views.ContainsKey(id))
        {
            throw new PlatformException(
                code: "error",
                message: $"Trying to dispose a platform view with unknown id: {id}");
        }

        _views.Remove(id);
        return Task.FromResult<object?>(null);
    }
}

/// <summary>Flutter's <c>FakeIosPlatformViewsController</c>.</summary>
internal sealed class FakeIosPlatformViewsController(bool dartAsyncReplies = false)
    : FakeDarwinPlatformViewsController<FakeUiKitView>(dartAsyncReplies)
{
    protected override FakeUiKitView CreateView(int id, string viewType, byte[]? creationParams) =>
        new(id, viewType, creationParams);
}

/// <summary>Flutter's <c>FakeMacosPlatformViewsController</c>.</summary>
internal sealed class FakeMacosPlatformViewsController(bool dartAsyncReplies = false)
    : FakeDarwinPlatformViewsController<FakeAppKitView>(dartAsyncReplies)
{
    protected override FakeAppKitView CreateView(int id, string viewType, byte[]? creationParams) =>
        new(id, viewType, creationParams);
}

/// <summary>Flutter's <c>FakeAndroidPlatformView</c>.</summary>
internal sealed class FakeAndroidPlatformView(
    int id,
    string type,
    Size? size,
    int layoutDirection,
    bool? hybrid = null,
    bool? hybridFallback = null,
    byte[]? creationParams = null,
    Point? position = null)
{
    public int Id { get; } = id;

    public string Type { get; } = type;

    public byte[]? CreationParams { get; } = creationParams;

    public Size? Size { get; } = size;

    public int LayoutDirection { get; } = layoutDirection;

    public bool? Hybrid { get; } = hybrid;

    public bool? HybridFallback { get; } = hybridFallback;

    public Point? Position { get; } = position;

    public FakeAndroidPlatformView CopyWith(Size? size = null, int? layoutDirection = null) =>
        new(
            Id,
            Type,
            size ?? Size,
            layoutDirection ?? LayoutDirection,
            hybrid: Hybrid,
            hybridFallback: HybridFallback,
            creationParams: CreationParams,
            position: Position);

    public override bool Equals(object? obj)
    {
        return obj is FakeAndroidPlatformView other
               && other.Id == Id
               && other.Type == Type
               && ListEquals(other.CreationParams, CreationParams)
               && other.Size == Size
               && other.Hybrid == Hybrid
               && other.HybridFallback == HybridFallback
               && other.LayoutDirection == LayoutDirection
               && other.Position == Position;
    }

    public override int GetHashCode() =>
        HashCode.Combine(Id, Type, Size, LayoutDirection, Hybrid, HybridFallback, Position);

    public override string ToString() =>
        $"FakeAndroidPlatformView(id: {Id}, type: {Type}, size: {Size}, layoutDirection: {LayoutDirection}, "
        + $"hybrid: {Hybrid}, hybridFallback: {HybridFallback}, creationParams: {CreationParams}, "
        + $"position: {Position})";

    private static bool ListEquals(byte[]? a, byte[]? b) =>
        a is null ? b is null : b is not null && a.SequenceEqual(b);
}

/// <summary>Flutter's <c>FakeAndroidMotionEvent</c>.</summary>
internal sealed class FakeAndroidMotionEvent(int action, IReadOnlyList<int> pointerIds, IReadOnlyList<Point> pointers)
{
    public int Action { get; } = action;

    public IReadOnlyList<Point> Pointers { get; } = pointers;

    public IReadOnlyList<int> PointerIds { get; } = pointerIds;

    public override bool Equals(object? obj)
    {
        return obj is FakeAndroidMotionEvent other
               && other.PointerIds.SequenceEqual(PointerIds)
               && other.Action == Action
               && other.Pointers.SequenceEqual(Pointers);
    }

    public override int GetHashCode() => HashCode.Combine(Action, Pointers.Count, PointerIds.Count);

    public override string ToString() =>
        $"FakeAndroidMotionEvent(action: {Action}, pointerIds: [{string.Join(", ", PointerIds)}], "
        + $"pointers: [{string.Join(", ", Pointers)}])";
}

/// <summary>Flutter's <c>FakeUiKitView</c>.</summary>
internal sealed record FakeUiKitView(int Id, string Type, byte[]? CreationParams = null)
{
    public bool Equals(FakeUiKitView? other) =>
        other is not null && other.Id == Id && other.Type == Type
        && ReferenceEquals(other.CreationParams, CreationParams);

    public override int GetHashCode() => HashCode.Combine(Id, Type);
}

/// <summary>Flutter's <c>FakeAppKitView</c>.</summary>
internal sealed record FakeAppKitView(int Id, string Type, byte[]? CreationParams = null)
{
    public bool Equals(FakeAppKitView? other) =>
        other is not null && other.Id == Id && other.Type == Type
        && ReferenceEquals(other.CreationParams, CreationParams);

    public override int GetHashCode() => HashCode.Combine(Id, Type);
}
