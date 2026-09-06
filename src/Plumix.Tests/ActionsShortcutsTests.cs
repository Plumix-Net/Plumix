using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/actions.dart
// flutter/packages/flutter/lib/src/widgets/shortcuts.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ActionsShortcutsTests : IDisposable
{
    public ActionsShortcutsTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Activators_ExposeSourceDefaultsAndExactModifierRepeatLockBehavior()
    {
        var plain = new SingleActivator(LogicalKeyboardKey.KeyA);
        Assert.False(plain.Control);
        Assert.False(plain.Shift);
        Assert.False(plain.Alt);
        Assert.False(plain.Meta);
        Assert.True(plain.IncludeRepeats);
        Assert.Equal(LockState.Ignored, plain.NumLock);
        Assert.Equal("Key A", plain.DebugDescribeKeys());
        Assert.True(plain.Accepts(KeySim.Down(LogicalKeyboardKey.KeyA), HardwareKeyboard.Instance));
        Assert.False(plain.Accepts(
            KeySim.Down(LogicalKeyboardKey.KeyA, control: true),
            HardwareKeyboard.Instance));
        Assert.False(plain.Accepts(KeySim.Up(LogicalKeyboardKey.KeyA), HardwareKeyboard.Instance));

        var modified = new SingleActivator(
            LogicalKeyboardKey.KeyK,
            control: true,
            shift: true,
            includeRepeats: false,
            numLock: LockState.Locked);
        Assert.Equal("Control + Shift + Key K", modified.DebugDescribeKeys());
        Assert.True(modified.Accepts(
            KeySim.Down(LogicalKeyboardKey.KeyK, control: true, shift: true, numLock: true),
            HardwareKeyboard.Instance));
        Assert.False(modified.Accepts(
            KeySim.Repeat(LogicalKeyboardKey.KeyK, control: true, shift: true),
            HardwareKeyboard.Instance));
        Assert.False(modified.Accepts(
            KeySim.Down(LogicalKeyboardKey.KeyK, control: true, shift: true),
            HardwareKeyboard.Instance));

        Assert.Equal(
            new SingleActivator(LogicalKeyboardKey.KeyK, control: true),
            new SingleActivator(LogicalKeyboardKey.KeyK, control: true));
        Assert.NotEqual(
            new SingleActivator(LogicalKeyboardKey.KeyK, control: true),
            new SingleActivator(LogicalKeyboardKey.KeyK, meta: true));
    }

    [Fact]
    public void LogicalAndCharacterActivators_MatchSourceKeySetAndCharacterContracts()
    {
        var logical = new LogicalKeySet(LogicalKeyboardKey.Control, LogicalKeyboardKey.KeyC);
        var equivalent = new LogicalKeySet(
            new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.KeyC, LogicalKeyboardKey.Control });
        Assert.Equal(logical, equivalent);
        Assert.Equal(logical.GetHashCode(), equivalent.GetHashCode());
        // A `LogicalKeySet` matches against the whole pressed set, so the trigger has to be
        // recorded first — which is what Flutter's `simulateKeyDownEvent` does before dispatch.
        KeyEvent controlC = KeySim.Down(LogicalKeyboardKey.KeyC, control: true);
        HardwareKeyboard.Instance.HandleKeyEvent(controlC);
        Assert.True(logical.Accepts(controlC, HardwareKeyboard.Instance));
        HardwareKeyboard.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.KeyC, control: true));

        KeyEvent controlShiftC = KeySim.Down(LogicalKeyboardKey.KeyC, control: true, shift: true);
        HardwareKeyboard.Instance.HandleKeyEvent(controlShiftC);
        Assert.False(logical.Accepts(controlShiftC, HardwareKeyboard.Instance));
        HardwareKeyboard.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.KeyC));

        var character = new CharacterActivator("?", alt: true, includeRepeats: false);
        Assert.True(character.Accepts(
            KeySim.Down(LogicalKeyboardKey.Slash, alt: true, character: "?"),
            HardwareKeyboard.Instance));
        Assert.False(character.Accepts(
            KeySim.Repeat(LogicalKeyboardKey.Slash, alt: true, character: "?"),
            HardwareKeyboard.Instance));
        Assert.False(character.Accepts(
            KeySim.Down(LogicalKeyboardKey.Slash, character: "?"),
            HardwareKeyboard.Instance));

        Assert.Throws<ArgumentException>(() => new LogicalKeySet(LogicalKeyboardKey.KeyA, LogicalKeyboardKey.KeyA));
        Assert.Throws<ArgumentException>(() => new LogicalKeySet(new HashSet<LogicalKeyboardKey>()));
        Assert.Equal(string.Empty, new CharacterActivator(string.Empty).Character);
        Assert.Throws<ArgumentNullException>(() => new CharacterActivator(null!));
        Assert.Throws<ArgumentException>(() => new SingleActivator(LogicalKeyboardKey.Control));
    }

    [Fact]
    public void Actions_FindInvokeHandlerAndDispatcher_UseNearestTypedMapping()
    {
        int outerInvocations = 0;
        int innerInvocations = 0;
        var dispatcher = new TrackingDispatcher();
        BuildContext capturedContext = null!;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(IncrementIntent)] = new CallbackAction<IncrementIntent>(
                        intent =>
                        {
                            outerInvocations += intent.Amount;
                            return "outer";
                        })
                },
                dispatcher: dispatcher,
                child: new Actions(
                    actions: new Dictionary<Type, FlutterAction>
                    {
                        [typeof(IncrementIntent)] = new CallbackAction<IncrementIntent>(
                            intent =>
                            {
                                innerInvocations += intent.Amount;
                                return "inner";
                            })
                    },
                    child: new Builder(context =>
                    {
                        capturedContext = context;
                        return new SizedBox(width: 10, height: 10);
                    }))));

        Mount(root, owner);

        FlutterAction<IncrementIntent> found = Actions.Find<IncrementIntent>(capturedContext!);
        Assert.NotNull(found);
        Assert.Equal("inner", Actions.Invoke(capturedContext, new IncrementIntent(2)));
        Assert.Equal(2, innerInvocations);
        Assert.Equal(0, outerInvocations);
        Assert.Equal(1, dispatcher.InvokeCount);

        System.Action? handler = Actions.Handler(capturedContext, new IncrementIntent(3));
        Assert.NotNull(handler);
        handler!();
        Assert.Equal(5, innerInvocations);
        Assert.Equal(2, dispatcher.InvokeCount);
        Assert.Null(Actions.MaybeFind<DismissIntent>(capturedContext));
        Assert.Null(Actions.MaybeInvoke(capturedContext, new DismissIntent()));

        root.Unmount();
    }

    [Fact]
    public void Actions_DisabledMappingStopsAncestorSearchAndActionNotificationsRebuildDependents()
    {
        var action = new ToggleAction(enabled: false);
        int outerInvocations = 0;
        int dependentBuilds = 0;
        BuildContext capturedContext = null!;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(IncrementIntent)] = new CallbackAction<IncrementIntent>(
                        _ =>
                        {
                            outerInvocations++;
                            return null;
                        })
                },
                child: new Actions(
                    actions: new Dictionary<Type, FlutterAction>
                    {
                        [typeof(IncrementIntent)] = action
                    },
                    child: new Builder(context =>
                    {
                        capturedContext = context;
                        dependentBuilds++;
                        _ = Actions.MaybeFind<IncrementIntent>(context);
                        return new SizedBox(width: 10, height: 10);
                    }))));

        Mount(root, owner);
        Assert.Equal(1, dependentBuilds);
        Assert.Null(Actions.Handler(capturedContext!, new IncrementIntent(1)));
        Assert.Null(Actions.MaybeInvoke(capturedContext, new IncrementIntent(1)));
        Assert.Equal(0, outerInvocations);

        action.SetEnabled(true);
        owner.FlushBuild();
        Assert.Equal(2, dependentBuilds);
        Assert.Equal(4, Actions.MaybeInvoke(capturedContext, new IncrementIntent(4)));
        Assert.Equal(4, action.Total);

        root.Unmount();
    }

    [Fact]
    public void ActionListener_RebindsAndDetachesFromActions()
    {
        var first = new ToggleAction(enabled: true);
        var second = new ToggleAction(enabled: true);
        int notifications = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new ActionListener(
            action: first,
            listener: _ => notifications++,
            child: new SizedBox(width: 10, height: 10)));
        Mount(root, owner);

        first.SetEnabled(false);
        Assert.Equal(1, notifications);

        root.Update(new ActionListener(
            action: second,
            listener: _ => notifications++,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();
        first.SetEnabled(true);
        second.SetEnabled(false);
        Assert.Equal(2, notifications);

        root.Unmount();
        second.SetEnabled(true);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void OverridableAction_DelegatesToTheOverrideAndExposesItAsCallingAction()
    {
        // Dart's `Action.overridable`: the overridable action looks its override up from the
        // context it was created with, and hands the override the default action as
        // `callingAction` for the duration of the call.
        var defaultAction = new RecordingAction("default");
        var override1 = new RecordingAction("override");
        BuildContext innerContext = null!;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(OverridableIntent)] = override1
                },
                child: new Builder(context =>
                {
                    innerContext = context;
                    return new SizedBox(width: 10, height: 10);
                })));
        Mount(root, owner);

        var overridable = (ContextAction<OverridableIntent>)FlutterAction.Overridable(
            defaultAction,
            innerContext!);

        Assert.Equal("override", overridable.Invoke(new OverridableIntent(), innerContext));
        Assert.Equal(1, override1.Invocations);
        Assert.Equal(0, defaultAction.Invocations);

        // The override saw a calling action while it ran, and the field is cleared once the call
        // returns. A ContextAction default is handed over wrapped together with the invoking
        // context, so the override can reach it without being a ContextAction itself.
        Assert.NotNull(override1.CallingActionDuringInvoke);
        Assert.Null(override1.CallingAction);
        Assert.Equal("default", override1.CallingActionDuringInvoke!.Invoke(new OverridableIntent()));
        Assert.Equal(1, defaultAction.Invocations);

        // Enablement and key consumption are answered by the override, not the default.
        override1.Enabled = false;
        Assert.False(overridable.IsActionEnabled);
        Assert.False(overridable.IsEnabled(new OverridableIntent(), innerContext));
        override1.Enabled = true;
        override1.Consumes = false;
        Assert.False(overridable.ConsumesKey(new OverridableIntent()));

        root.Unmount();
    }

    [Fact]
    public void OverridableAction_FallsBackToTheDefaultActionWhenNoOverrideIsFound()
    {
        var defaultAction = new RecordingAction("default");
        BuildContext context = null!;
        var owner = new BuildOwner();
        var root = new TestRootElement(new Builder(builderContext =>
        {
            context = builderContext;
            return new SizedBox(width: 10, height: 10);
        }));
        Mount(root, owner);

        var overridable = (ContextAction<OverridableIntent>)FlutterAction.Overridable(
            defaultAction,
            context!);
        Assert.Equal("default", overridable.Invoke(new OverridableIntent(), context));
        Assert.Equal(1, defaultAction.Invocations);
        Assert.True(overridable.IsActionEnabled);

        defaultAction.Enabled = false;
        Assert.False(overridable.IsActionEnabled);

        root.Unmount();
    }

    [Fact]
    public void ContextAction_ReceivesFocusedContextThroughActionsAndDispatcherFallback()
    {
        var action = new ContextProbeAction();
        var focusNode = new FocusNode();
        BuildContext capturedContext = null!;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(ContextProbeIntent)] = action
                },
                child: new Builder(context =>
                {
                    capturedContext = context;
                    return new Focus(
                        focusNode: focusNode,
                        autofocus: true,
                        child: new SizedBox(width: 20, height: 20));
                })));
        Mount(root, owner);

        Assert.Equal("context", Actions.Invoke(capturedContext!, new ContextProbeIntent()));
        Assert.True(action.LastContext is not null);
        Assert.Same(capturedContext, action.LastContext);

        action.LastContext = null;
        Assert.Equal(
            "context",
            new ActionDispatcher().InvokeAction(action, new ContextProbeIntent()));
        Assert.True(action.LastContext is not null);
        Assert.Same(focusNode.AttachmentElement, action.LastContext);

        root.Unmount();
    }

    [Fact]
    public void Shortcuts_DispatchesFromFocusedDescendantAndHonorsConsumePolicy()
    {
        int consumedInvocations = 0;
        int propagatedInvocations = 0;
        var focusNode = new FocusNode();
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Shortcuts(
                shortcuts: new Dictionary<ShortcutActivator, Intent>
                {
                    [new SingleActivator(LogicalKeyboardKey.KeyA, control: true)] = new IncrementIntent(2),
                    [new SingleActivator(LogicalKeyboardKey.KeyB)] = new PropagatingIntent()
                },
                child: new Actions(
                    actions: new Dictionary<Type, FlutterAction>
                    {
                        [typeof(IncrementIntent)] = new CallbackAction<IncrementIntent>(
                            intent =>
                            {
                                consumedInvocations += intent.Amount;
                                return null;
                            }),
                        [typeof(PropagatingIntent)] = new PropagatingAction(
                            () => propagatedInvocations++)
                    },
                    child: new Focus(
                        focusNode: focusNode,
                        autofocus: true,
                        child: new SizedBox(width: 20, height: 20)))));
        Mount(root, owner);

        Assert.True(focusNode.HasFocus);
        Assert.True(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        Assert.Equal(2, consumedInvocations);

        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyB)));
        Assert.Equal(1, propagatedInvocations);
        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.KeyA)));

        root.Unmount();
    }

    [Fact]
    public void NestedShortcuts_UsesNearestMatchAndModalManagerStopsTraversal()
    {
        int outerInvocations = 0;
        int innerInvocations = 0;
        var focusNode = new FocusNode();
        var modalManager = new ShortcutManager(modal: true);
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Shortcuts(
                manager: modalManager,
                child: new Shortcuts(
                    shortcuts: new Dictionary<ShortcutActivator, Intent>
                    {
                        [new SingleActivator(LogicalKeyboardKey.Enter)] = new OuterIntent()
                    },
                    child: new Actions(
                        actions: new Dictionary<Type, FlutterAction>
                        {
                            [typeof(OuterIntent)] = new CallbackAction<OuterIntent>(
                                _ =>
                                {
                                    outerInvocations++;
                                    return null;
                                }),
                            [typeof(InnerIntent)] = new CallbackAction<InnerIntent>(
                                _ =>
                                {
                                    innerInvocations++;
                                    return null;
                                })
                        },
                        child: new Shortcuts(
                            shortcuts: new Dictionary<ShortcutActivator, Intent>
                            {
                                [new SingleActivator(LogicalKeyboardKey.Enter)] = new InnerIntent()
                            },
                            child: new Focus(
                                focusNode: focusNode,
                                autofocus: true,
                                child: new SizedBox(width: 20, height: 20)))))));
        Mount(root, owner);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Enter)));
        Assert.Equal(0, outerInvocations);
        Assert.Equal(1, innerInvocations);

        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab)));
        Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);

        root.Unmount();
    }

    [Fact]
    public void CallbackShortcuts_InvokesCallbackWithoutExplicitActionsWidget()
    {
        int invocationCount = 0;
        var focusNode = new FocusNode();
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new CallbackShortcuts(
                bindings: new Dictionary<ShortcutActivator, System.Action>
                {
                    [new CharacterActivator("+")] = () => invocationCount++
                },
                child: new Focus(
                    focusNode: focusNode,
                    autofocus: true,
                    child: new SizedBox(width: 20, height: 20))));
        Mount(root, owner);

        Assert.True(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.Equal, character: "+")));
        Assert.Equal(1, invocationCount);

        root.Unmount();
    }

    [Fact]
    public void ShortcutRegistrar_CombinesDeferredEntriesAndDispatchesRegisteredShortcut()
    {
        int invocationCount = 0;
        var focusNode = new FocusNode();
        ShortcutRegistryEntry? entry = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new ShortcutRegistrar(
                child: new Builder(context =>
                {
                    entry ??= ShortcutRegistry.Of(context).AddAll(
                        new Dictionary<ShortcutActivator, Intent>
                        {
                            [new SingleActivator(LogicalKeyboardKey.F2)] = new InnerIntent()
                        });
                    return new Actions(
                        actions: new Dictionary<Type, FlutterAction>
                        {
                            [typeof(InnerIntent)] = new CallbackAction<InnerIntent>(
                                _ =>
                                {
                                    invocationCount++;
                                    return null;
                                })
                        },
                        child: new Focus(
                            focusNode: focusNode,
                            autofocus: true,
                            child: new SizedBox(width: 20, height: 20)));
                })));
        Mount(root, owner);
        Scheduler.PumpFrameForTests();

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.F2)));
        Assert.Equal(1, invocationCount);

        entry!.ReplaceAll(
            new Dictionary<ShortcutActivator, Intent>
            {
                [new SingleActivator(LogicalKeyboardKey.F3)] = new InnerIntent()
            });
        Scheduler.PumpFrameForTests();
        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.F2)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.F3)));
        Assert.Equal(2, invocationCount);

        entry.Dispose();
        Scheduler.PumpFrameForTests();
        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.F3)));
        root.Unmount();
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class IncrementIntent : Intent
    {
        public IncrementIntent(int amount)
        {
            Amount = amount;
        }

        public int Amount { get; }
    }

    private sealed class OverridableIntent : Intent
    {
    }

    private sealed class RecordingAction : ContextAction<OverridableIntent>
    {
        private readonly string _result;

        public RecordingAction(string result)
        {
            _result = result;
        }

        public int Invocations { get; private set; }

        public bool Enabled { get; set; } = true;

        public bool Consumes { get; set; } = true;

        public FlutterAction<OverridableIntent>? CallingActionDuringInvoke { get; private set; }

        public override bool IsActionEnabled => Enabled;

        public override bool ConsumesKey(OverridableIntent intent) => Consumes;

        public override object? Invoke(OverridableIntent intent, BuildContext? context)
        {
            Invocations++;
            CallingActionDuringInvoke = CallingAction;
            return _result;
        }
    }

    private sealed class OuterIntent : Intent
    {
    }

    private sealed class InnerIntent : Intent
    {
    }

    private sealed class PropagatingIntent : Intent
    {
    }

    private sealed class ContextProbeIntent : Intent
    {
    }

    private sealed class ToggleAction : FlutterAction<IncrementIntent>
    {
        private bool _enabled;

        public ToggleAction(bool enabled)
        {
            _enabled = enabled;
        }

        public int Total { get; private set; }

        public override bool IsActionEnabled => _enabled;

        public void SetEnabled(bool enabled)
        {
            if (_enabled == enabled)
            {
                return;
            }

            _enabled = enabled;
            NotifyActionListeners();
        }

        public override object? Invoke(IncrementIntent intent)
        {
            Total += intent.Amount;
            return Total;
        }
    }

    private sealed class PropagatingAction : FlutterAction<PropagatingIntent>
    {
        private readonly System.Action _callback;

        public PropagatingAction(System.Action callback)
        {
            _callback = callback;
        }

        public override bool ConsumesKey(PropagatingIntent intent) => false;

        public override object? Invoke(PropagatingIntent intent)
        {
            _callback();
            return null;
        }
    }

    private sealed class ContextProbeAction : ContextAction<ContextProbeIntent>
    {
        public BuildContext? LastContext { get; set; }

        public override object? Invoke(ContextProbeIntent intent, BuildContext? context)
        {
            LastContext = context;
            return context is not null ? "context" : "missing";
        }
    }

    private sealed class TrackingDispatcher : ActionDispatcher
    {
        public int InvokeCount { get; private set; }

        public override object? InvokeAction(
            FlutterAction action,
            Intent intent,
            BuildContext? context = null)
        {
            InvokeCount++;
            return base.InvokeAction(action, intent, context);
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

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void VisitChildren(System.Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
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
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }
}
