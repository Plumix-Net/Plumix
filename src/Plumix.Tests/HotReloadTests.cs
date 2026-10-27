using Avalonia.Input;
using Key = Avalonia.Input.Key;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources: flutter/packages/flutter/lib/src/widgets/framework.dart (Element.reassemble,
// StatefulElement.reassemble); flutter/packages/flutter/lib/src/foundation/binding.dart
// (reassembleApplication, performReassemble); flutter/packages/flutter/test/foundation/reassemble_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class HotReloadTests : IDisposable
{
    // The warm-up frame of a reassemble is two Timer.run turns; they run on a fake clock the test drives.
    private readonly FakeGestureTimers _timers = new();

    public HotReloadTests()
    {
        SchedulerBinding.ResetForTests();
    }

    public void Dispose()
    {
        _timers.Dispose();
        SchedulerBinding.ResetForTests();
    }

    [Fact]
    public void Reassemble_RebuildsEntireTree_AndPreservesState()
    {
        ReassembleTracker.Reset();

        var owner = TestBuildOwner.Create();
        var root = new TestRootElement(new ReassembleHost());
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.PumpFrame();

        var initialState = ReassembleTracker.CurrentState;
        Assert.NotNull(initialState);
        Assert.Equal(1, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(1, ReassembleTracker.StatelessBuildCount);
        Assert.Equal(0, ReassembleTracker.StateReassembleCount);

        owner.Reassemble(root);
        owner.PumpFrame();

        Assert.Same(initialState, ReassembleTracker.CurrentState);
        Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(2, ReassembleTracker.StatelessBuildCount);
        Assert.Equal(1, ReassembleTracker.StateReassembleCount);

        root.UnmountRoot();
    }

    [Fact]
    public void Reassemble_CallsStateReassemble_BeforeMarkingSubtreeDirty()
    {
        ReassembleTracker.Reset();

        var owner = TestBuildOwner.Create();
        var root = new TestRootElement(new ReassembleHost());
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.PumpFrame();

        ReassembleTracker.Events.Clear();
        owner.Reassemble(root);
        owner.PumpFrame();

        Assert.Equal(["state-reassemble", "stateful-build", "stateless-build"], ReassembleTracker.Events);

        root.UnmountRoot();
    }

    [Fact]
    public async Task BindingBase_ReassembleApplication_RebuildsTheTreeUnderLockedEvents_PreservingState()
    {
        ReassembleTracker.Reset();
        var host = new WidgetHost
        {
            RootWidget = new ReassembleHost(),
        };
        try
        {
            ReassembleHostState? initialState = ReassembleTracker.CurrentState;
            Assert.NotNull(initialState);
            Assert.Equal(1, ReassembleTracker.StatefulBuildCount);

            Task reassembled = BindingBase.ReassembleApplication();

            // reassemble_test.dart: events are locked while performReassemble runs, and the state
            // was reassembled synchronously inside it.
            Assert.Equal([true], ReassembleTracker.LockedDuringReassemble);
            Assert.True(SchedulerBinding.Instance.Locked);
            Assert.False(reassembled.IsCompleted);

            // RendererBinding.performReassemble waits for the warm-up frame that shows the result.
            await DrainWarmUpFrame(reassembled);

            Assert.False(SchedulerBinding.Instance.Locked);
            Assert.Same(initialState, ReassembleTracker.CurrentState);
            Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
            Assert.Equal(2, ReassembleTracker.StatelessBuildCount);
            Assert.Equal(1, ReassembleTracker.StateReassembleCount);
        }
        finally
        {
            host.RootWidget = null;
        }
    }

    [Fact]
    public async Task BindingBase_ReassembleApplication_ResetsTheErrorCount()
    {
        // BindingBase.performReassemble: the first error after a hot reload prints in full again.
        List<string?> log = [];
        DebugPrintCallback previousPrint = Print.DebugPrint;
        Print.DebugPrint = (message, _) => log.Add(message);
        try
        {
            FlutterError.ResetErrorCount();
            FlutterError.DumpErrorToConsole(new FlutterErrorDetails(exception: new InvalidOperationException("one")));
            FlutterError.DumpErrorToConsole(new FlutterErrorDetails(exception: new InvalidOperationException("two")));
            Assert.StartsWith("Another exception was thrown:", log[^1]);

            await DrainWarmUpFrame(BindingBase.ReassembleApplication());
            log.Clear();
            FlutterError.DumpErrorToConsole(new FlutterErrorDetails(exception: new InvalidOperationException("three")));

            Assert.DoesNotContain(
                log,
                static line => line?.StartsWith("Another exception", StringComparison.Ordinal) == true);
        }
        finally
        {
            Print.DebugPrint = previousPrint;
            FlutterError.ResetErrorCount();
        }
    }

    [Fact]
    public async Task HotReloadManager_ReassembleApplication_RunsTheBindingChain()
    {
        ReassembleTracker.Reset();
        var host = new WidgetHost
        {
            RootWidget = new ReassembleHost(),
        };
        try
        {
            Assert.Equal(1, ReassembleTracker.StatefulBuildCount);

            await DrainWarmUpFrame(HotReloadManager.ReassembleApplication());

            Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
            Assert.Equal(1, ReassembleTracker.StateReassembleCount);
        }
        finally
        {
            host.RootWidget = null;
        }
    }

    [Fact]
    public void ManualReassembleShortcut_ReassemblesTheApplication_WhenHotReloadAvailable()
    {
        ReassembleTracker.Reset();
        var widgetHost = new WidgetHost
        {
            RootWidget = new ReassembleHost(),
        };
        var probeHost = new KeyProbeHost();

        Assert.Equal(1, ReassembleTracker.StatefulBuildCount);

        bool wasAvailable = HotReloadManager.IsManualReassembleAvailable;
        try
        {
            HotReloadManager.IsManualReassembleAvailable = false;
            Assert.False(probeHost.DispatchKeyDown(Key.R, KeyModifiers.Control | KeyModifiers.Shift));

            HotReloadManager.IsManualReassembleAvailable = true;
            Assert.True(probeHost.DispatchKeyDown(Key.R, KeyModifiers.Control | KeyModifiers.Shift));
            for (int turn = 0; turn < 10 && SchedulerBinding.Instance.Locked; turn++)
            {
                RunTimers();
                EventLoop.FlushMicrotasks();
                WaitForUnlock();
            }
        }
        finally
        {
            HotReloadManager.IsManualReassembleAvailable = wasAvailable;
            widgetHost.RootWidget = null;
        }

        Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(1, ReassembleTracker.StateReassembleCount);
    }

    [DebugOnlyFact]
    public async Task ReassembleServiceExtension_ReassemblesTheApplication()
    {
        ReassembleTracker.Reset();
        var host = new WidgetHost
        {
            RootWidget = new ReassembleHost(),
        };
        try
        {
            // flutter_test's harness calls the registered callback directly, bypassing the VM wrapper.
            Task<Dictionary<string, object?>> response =
                BindingBase.RegisteredCallbacks[FoundationServiceExtensions.Reassemble.DartName()](
                    new Dictionary<string, string>());
            await DrainWarmUpFrame(response);

            Assert.Empty(response.Result);
            Assert.Equal(1, ReassembleTracker.StateReassembleCount);
        }
        finally
        {
            host.RootWidget = null;
        }
    }

    /// <summary>Runs the warm-up frame <c>performReassemble</c> waits for, then the unlock microtask.</summary>
    private async Task DrainWarmUpFrame(Task reassembled)
    {
        for (int turn = 0; turn < 10 && !reassembled.IsCompleted; turn++)
        {
            RunTimers();
            EventLoop.FlushMicrotasks();
        }

        await reassembled.WaitAsync(TimeSpan.FromSeconds(5));
        WaitForUnlock();
    }

    /// <summary>
    /// lockEvents releases its lock in a microtask queued by the locked task's continuation, which may
    /// complete on another thread; drain microtasks until that release ran.
    /// </summary>
    internal static void WaitForUnlock()
    {
        for (int attempt = 0; attempt < 400; attempt++)
        {
            EventLoop.FlushMicrotasks();
            if (!SchedulerBinding.Instance.Locked)
            {
                return;
            }

            Thread.Sleep(5);
        }
    }

    private void RunTimers() => _timers.RunDueTimers();

    private sealed class KeyProbeHost : PlumixHost
    {
        public bool DispatchKeyDown(Key key, KeyModifiers modifiers)
        {
            var args = new KeyEventArgs
            {
                Key = key,
                KeyModifiers = modifiers
            };

            OnKeyDown(args);
            return args.Handled;
        }
    }

    private static class ReassembleTracker
    {
        public static int StatefulBuildCount;
        public static int StatelessBuildCount;
        public static int StateReassembleCount;
        public static ReassembleHostState? CurrentState;
        public static List<string> Events = [];
        public static List<bool> LockedDuringReassemble = [];

        public static void Reset()
        {
            StatefulBuildCount = 0;
            StatelessBuildCount = 0;
            StateReassembleCount = 0;
            CurrentState = null;
            Events = [];
            LockedDuringReassemble = [];
        }
    }

    private sealed class ReassembleHost : StatefulWidget
    {
        public override State CreateState() => new ReassembleHostState();
    }

    private sealed class ReassembleHostState : State
    {
        public override void InitState()
        {
            base.InitState();
            ReassembleTracker.CurrentState = this;
        }

        public override void Reassemble()
        {
            base.Reassemble();
            ReassembleTracker.StateReassembleCount += 1;
            ReassembleTracker.Events.Add("state-reassemble");
            ReassembleTracker.LockedDuringReassemble.Add(SchedulerBinding.Instance.Locked);
        }

        public override Widget Build(BuildContext context)
        {
            ReassembleTracker.StatefulBuildCount += 1;
            ReassembleTracker.Events.Add("stateful-build");
            return new ReassembleLeaf();
        }
    }

    private sealed class ReassembleLeaf : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            ReassembleTracker.StatelessBuildCount += 1;
            ReassembleTracker.Events.Add("stateless-build");
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

    }
}
