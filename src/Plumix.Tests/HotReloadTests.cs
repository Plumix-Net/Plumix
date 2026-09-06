using Avalonia.Input;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (Element.reassemble, StatefulElement.reassemble); flutter/packages/flutter/lib/src/foundation/binding.dart (reassembleApplication, adapted to .NET MetadataUpdateHandler)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class HotReloadTests
{
    [Fact]
    public void Reassemble_RebuildsEntireTree_AndPreservesState()
    {
        ReassembleTracker.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(new ReassembleHost());
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialState = ReassembleTracker.CurrentState;
        Assert.NotNull(initialState);
        Assert.Equal(1, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(1, ReassembleTracker.StatelessBuildCount);
        Assert.Equal(0, ReassembleTracker.StateReassembleCount);

        owner.Reassemble(root);
        owner.FlushBuild();

        Assert.Same(initialState, ReassembleTracker.CurrentState);
        Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(2, ReassembleTracker.StatelessBuildCount);
        Assert.Equal(1, ReassembleTracker.StateReassembleCount);

        root.Unmount();
    }

    [Fact]
    public void Reassemble_CallsStateReassemble_BeforeMarkingSubtreeDirty()
    {
        ReassembleTracker.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(new ReassembleHost());
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        ReassembleTracker.Events.Clear();
        owner.Reassemble(root);
        owner.FlushBuild();

        Assert.Equal(["state-reassemble", "stateful-build", "stateless-build"], ReassembleTracker.Events);

        root.Unmount();
    }

    [Fact]
    public void WidgetHost_ReassembleApplication_RebuildsWidgetTree_PreservingState()
    {
        ReassembleTracker.Reset();

        var host = new WidgetHost
        {
            RootWidget = new ReassembleHost()
        };

        var initialState = ReassembleTracker.CurrentState;
        Assert.NotNull(initialState);
        Assert.Equal(1, ReassembleTracker.StatefulBuildCount);

        host.ReassembleApplication();
        Scheduler.PumpFrameForTests();

        Assert.Same(initialState, ReassembleTracker.CurrentState);
        Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(2, ReassembleTracker.StatelessBuildCount);
        Assert.Equal(1, ReassembleTracker.StateReassembleCount);

        host.RootWidget = null;
    }

    [Fact]
    public void HotReloadManager_ReassembleApplication_ReassemblesLiveHosts()
    {
        ReassembleTracker.Reset();
        HotReloadManager.ResetForTests();

        var host = new WidgetHost
        {
            RootWidget = new ReassembleHost()
        };

        Assert.Equal(1, ReassembleTracker.StatefulBuildCount);

        HotReloadManager.ReassembleApplication();
        Scheduler.PumpFrameForTests();

        Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(1, ReassembleTracker.StateReassembleCount);

        host.RootWidget = null;
    }

    [Fact]
    public void ManualReassembleShortcut_ReassemblesLiveHosts_WhenHotReloadAvailable()
    {
        ReassembleTracker.Reset();
        HotReloadManager.ResetForTests();

        var widgetHost = new WidgetHost
        {
            RootWidget = new ReassembleHost()
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
            Scheduler.PumpFrameForTests();
        }
        finally
        {
            HotReloadManager.IsManualReassembleAvailable = wasAvailable;
        }

        Assert.Equal(2, ReassembleTracker.StatefulBuildCount);
        Assert.Equal(1, ReassembleTracker.StateReassembleCount);

        widgetHost.RootWidget = null;
    }

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

        public static void Reset()
        {
            StatefulBuildCount = 0;
            StatelessBuildCount = 0;
            StateReassembleCount = 0;
            CurrentState = null;
            Events = [];
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

        public override void Unmount()
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
