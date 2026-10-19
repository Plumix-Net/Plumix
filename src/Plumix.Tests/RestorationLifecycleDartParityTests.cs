using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/root_restoration_scope_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/restoration_mixin_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/restoration_scopes_moving_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationLifecycleDartParityTests : IDisposable
{
    public RestorationLifecycleDartParityTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    [Fact]
    public void RootScope_HoldsBackFirstRenderedFrameUntilNativeDataArrives()
    {
        using var tester = new FrameworkDartTester();
        RendererBinding.Instance.ResetFirstFrameSent();
        var messenger = new PlatformBinaryMessenger();
        var host = new AndroidRestorationChannel(true, messenger);
        using var manager = new RestorationManager(messenger);
        using var ambient = new ManagerOverride(manager);
        bool built = false;
        tester.PumpWidget(new RootRestorationScope("app", new Builder(_ =>
        {
            built = true;
            return new SizedBox(width: 20, height: 30);
        })));

        Assert.False(built);
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        tester.Pump();
        Assert.False(RendererBinding.Instance.SendFramesToEngine);

        host.SetRestorationData(null);
        EventLoop.FlushMicrotasks();
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
        tester.Pump();
        Assert.True(built);
        tester.PumpWidget(new SizedBox());
    }

    [Fact]
    public void RootScope_DisposedWhileLoadingStillBalancesItsFrameDeferral()
    {
        using var tester = new FrameworkDartTester();
        RendererBinding.Instance.ResetFirstFrameSent();
        var messenger = new PlatformBinaryMessenger();
        var host = new IOSRestorationPlugin(true, messenger);
        using var manager = new RestorationManager(messenger);
        using var ambient = new ManagerOverride(manager);
        tester.PumpWidget(new RootRestorationScope("app", new SizedBox()));
        Assert.False(RendererBinding.Instance.SendFramesToEngine);

        tester.PumpWidget(new SizedBox());
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        host.MarkRestorationComplete();
        EventLoop.FlushMicrotasks();
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void RootScope_EnablingAfterFirstBuildKeepsTheExistingChildWhileLoading()
    {
        using var tester = new FrameworkDartTester();
        using var manager = new TestRestorationManager { AnswerSynchronously = false };
        using var ambient = new ManagerOverride(manager);
        var key = new LabeledGlobalKey<RestorableWidgetState>("restorable");
        var property = new TestRestorableProperty(10);
        Widget Build(string? id) => new RootRestorationScope(id, new RestorableWidget("child", property, key: key));
        tester.PumpWidget(Build(null));
        RestorableWidgetState original = key.CurrentState!;
        property.Value = 42;
        Assert.False(manager.EngineQueried);

        tester.PumpWidget(Build("app"));
        Assert.Same(original, key.CurrentState);
        Assert.Equal(42, property.Value);
        Assert.True(manager.EngineQueried);
        manager.RespondWith(true, null);
        EventLoop.FlushMicrotasks();
        tester.Pump();

        Assert.Same(original, key.CurrentState);
        Assert.NotNull(original.Bucket);
        // This is enabling an existing scope, not replacing its manager's root: preserve current value.
        Assert.Equal(42, property.Value);
        tester.PumpWidget(new SizedBox());
    }

    [Fact]
    public void RootScope_MovingOutOfAnAncestorRetainsChildDuringPendingLoadAndReparentsBack()
    {
        using var tester = new FrameworkDartTester();
        using var manager = new TestRestorationManager { AnswerSynchronously = false };
        using var ambient = new ManagerOverride(manager);
        using var ancestorManager = new MockRestorationManager();
        var ancestor = RestorationBucket.Root(ancestorManager, null);
        var key = new LabeledGlobalKey<RestorableWidgetState>("restorable");
        var property = new TestRestorableProperty(10);
        var scopeKey = new LabeledGlobalKey<State>("scope");
        Widget Build(bool inside) => new UnmanagedRestorationScope(
            bucket: inside ? ancestor : null,
            child: new RootRestorationScope("app", new RestorableWidget("child", property, key: key), scopeKey));
        tester.PumpWidget(Build(true));
        RestorableWidgetState state = key.CurrentState!;
        RestorationBucket bucket = state.Bucket!;
        property.Value = 42;

        tester.PumpWidget(Build(false));
        Assert.Same(state, key.CurrentState);
        Assert.Null(state.Bucket);
        Assert.Equal(42, property.Value);
        Assert.True(manager.EngineQueried);
        manager.RespondWith(true, null);
        EventLoop.FlushMicrotasks();
        tester.Pump();

        Assert.Same(state, key.CurrentState);
        Assert.NotNull(state.Bucket);
        Assert.NotSame(bucket, state.Bucket);
        Assert.Equal(42, property.Value);
        RestorationBucket newBucket = state.Bucket;
        tester.PumpWidget(Build(true));
        Assert.Same(newBucket, state.Bucket);
        Assert.Equal(42, property.Value);
        tester.PumpWidget(new SizedBox());
        ancestor.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeCache_RestoresFreshStateThroughCodecChannelAndRootScope(bool ios)
    {
        using var tester = new FrameworkDartTester();
        var firstMessenger = new PlatformBinaryMessenger();
        var android = ios ? null : new AndroidRestorationChannel(true, firstMessenger);
        var darwin = ios ? new IOSRestorationPlugin(true, firstMessenger) : null;
        android?.SetRestorationData(null);
        darwin?.MarkRestorationComplete();
        using var firstManager = new RestorationManager(firstMessenger);
        using var firstAmbient = new ManagerOverride(firstManager);
        var firstProperty = new TestRestorableProperty(10);
        var key = new LabeledGlobalKey<RestorableWidgetState>("fresh-state");
        tester.PumpWidget(new RootRestorationScope("app", new RestorableWidget("child", firstProperty, key: key)));
        tester.Pump();
        RestorableWidgetState oldState = key.CurrentState!;
        Assert.NotNull(oldState);
        firstProperty.Value = 42;
        tester.Pump();
        firstManager.FlushData();
        byte[] saved = (ios ? darwin!.RestorationData : android!.RestorationData)!.ToArray();

        tester.PumpWidget(new SizedBox());
        var secondMessenger = new PlatformBinaryMessenger();
        if (ios)
        {
            _ = new IOSRestorationPlugin(true, secondMessenger) { RestorationData = saved };
        }
        else
        {
            var restoredHost = new AndroidRestorationChannel(true, secondMessenger);
            restoredHost.SetRestorationData(saved);
        }

        using var secondManager = new RestorationManager(secondMessenger);
        using var secondAmbient = new ManagerOverride(secondManager);
        var secondProperty = new TestRestorableProperty(0);
        tester.PumpWidget(new RootRestorationScope("app", new RestorableWidget("child", secondProperty, key: key)));
        tester.Pump();

        Assert.NotSame(oldState, key.CurrentState);
        Assert.Equal(42, secondProperty.Value);
        Assert.Equal([true], key.CurrentState!.InitialRestoreLog);
        Assert.Equal(["fromPrimitives", "initWithValue"], secondProperty.Log);
        tester.PumpWidget(new SizedBox());
    }

    private sealed class ManagerOverride : IDisposable
    {
        private readonly RestorationManager _previous = RestorationManager.Instance;

        public ManagerOverride(RestorationManager manager) => RestorationManager.Instance = manager;

        public void Dispose() => RestorationManager.Instance = _previous;
    }
}
