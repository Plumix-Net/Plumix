using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using FrameworkFocusManager = Plumix.Widgets.FocusManager;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/focus_manager.dart; flutter/packages/flutter/lib/src/widgets/binding.dart (host keyboard dispatch regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PlumixHostInputTests : IDisposable
{
    public PlumixHostInputTests()
    {
        FrameworkFocusManager.Instance.ResetForTests();
        PlumixHost.ResetPlatformPluginForTests();
    }

    public void Dispose()
    {
        FrameworkFocusManager.Instance.ResetForTests();
        PlumixHost.ResetPlatformPluginForTests();
    }

    [Fact]
    public void PlumixHost_HitTestsItsWholeBounds_WhateverTheSceneDraws()
    {
        // The engine hands every pointer event inside a FlutterView to the framework. Avalonia would
        // otherwise hit-test the drawn content, which misses native draws such as exact solid paths,
        // so wheel and press events over them fell through to the controls behind the host.
        var host = new PlumixHost();
        host.Measure(new Size(100, 50).ToAvalonia());
        host.Arrange(Rect.FromLTWH(0, 0, 100, 50).ToAvalonia());
        ICustomHitTest hitTest = Assert.IsAssignableFrom<ICustomHitTest>(host);
        Assert.True(hitTest.HitTest(new Offset(0, 0).ToAvalonia()));
        Assert.True(hitTest.HitTest(new Offset(50, 25).ToAvalonia()));
        Assert.False(hitTest.HitTest(new Offset(101, 25).ToAvalonia()));
        Assert.False(hitTest.HitTest(new Offset(50, -1).ToAvalonia()));
    }

    [Fact]
    public void PlumixHost_KeyDownAndKeyUp_AreDispatchedToPrimaryFocusNode()
    {
        int keyDownCount = 0;
        int keyUpCount = 0;
        var focusNode = new FocusNode
        {
            OnKeyEvent = (_, @event) =>
            {
                if (!@event.LogicalKey.Equals(Plumix.UI.LogicalKeyboardKey.Space))
                {
                    return KeyEventResult.Ignored;
                }

                if (@event is Plumix.UI.KeyDownEvent)
                {
                    keyDownCount += 1;
                }
                else
                {
                    keyUpCount += 1;
                }

                return KeyEventResult.Handled;
            }
        };

        FrameworkFocusManager.Instance.RegisterNode(focusNode);
        FrameworkFocusManager.Instance.RequestFocus(focusNode);
        EventLoop.FlushMicrotasks();

        var host = new TestPlumixHost();
        Assert.True(host.DispatchKeyDown(Key.Space));
        Assert.True(host.DispatchKeyUp(Key.Space));
        Assert.Equal(1, keyDownCount);
        Assert.Equal(1, keyUpCount);
    }

    [Fact]
    public void PlumixHost_KeyUp_Ignored_WhenNoPrimaryFocus()
    {
        var host = new TestPlumixHost();
        Assert.False(host.DispatchKeyUp(Key.Space));
    }

    [Fact]
    public void PlumixHost_KeyDown_ForwardsCharacterAndDetectsRepeat()
    {
        var events = new List<Plumix.UI.KeyEvent>();
        var focusNode = new FocusNode
        {
            OnKeyEvent = (_, @event) =>
            {
                events.Add(@event);
                return KeyEventResult.Handled;
            }
        };
        FrameworkFocusManager.Instance.RegisterNode(focusNode);
        FrameworkFocusManager.Instance.RequestFocus(focusNode);
        EventLoop.FlushMicrotasks();

        var host = new TestPlumixHost();
        Assert.True(host.DispatchKeyDown(Key.OemQuestion, keySymbol: "?", physicalKey: PhysicalKey.Slash));
        Assert.True(host.DispatchKeyDown(Key.OemQuestion, keySymbol: "?", physicalKey: PhysicalKey.Slash));
        Assert.True(host.DispatchKeyUp(Key.OemQuestion, keySymbol: "?", physicalKey: PhysicalKey.Slash));

        Assert.Equal("?", events[0].Character);
        Assert.IsType<Plumix.UI.KeyDownEvent>(events[0]);
        Assert.IsType<Plumix.UI.KeyRepeatEvent>(events[1]);
        Assert.IsType<Plumix.UI.KeyUpEvent>(events[2]);
        Assert.Equal(Plumix.UI.LogicalKeyboardKey.Slash, events[0].LogicalKey);
        Assert.Equal(Plumix.UI.PhysicalKeyboardKey.Slash, events[0].PhysicalKey);
    }

    [Fact]
    public void PlumixHost_WindowsKeyDataPreservesTextAndSynchronizesMissedModifiers()
    {
        if (!OperatingSystem.IsWindows()) return;
        var events = new List<Plumix.UI.KeyEvent>();
        using var focus = new FocusNode
        {
            OnKeyEvent = (_, value) =>
            {
                Assert.True(EventLoop.IsFrameworkThread);
                events.Add(value);
                return KeyEventResult.Handled;
            },
        };
        FrameworkFocusManager.Instance.RegisterNode(focus);
        focus.RequestFocus();
        EventLoop.FlushMicrotasks();
        var host = new TestPlumixHost();
        Assert.True(host.DispatchKeyDown(
            Key.A, KeyModifiers.Shift, keySymbol: "A", physicalKey: PhysicalKey.A));
        Assert.True(HardwareKeyboard.Instance.IsShiftPressed);
        var down = Assert.Single(events.OfType<Plumix.UI.KeyDownEvent>(), value => !value.Synthesized);
        Assert.Equal("A", down.Character);
        Assert.Equal(LogicalKeyboardKey.KeyA, down.LogicalKey);
        Assert.Contains(events, value => value.Synthesized && value.LogicalKey == LogicalKeyboardKey.ShiftLeft);
        Assert.True(host.DispatchKeyUp(Key.A, keySymbol: "a", physicalKey: PhysicalKey.A));
        Assert.False(HardwareKeyboard.Instance.IsShiftPressed);
        Assert.Empty(HardwareKeyboard.Instance.PhysicalKeysPressed);
        events.Clear();
        Assert.True(host.DispatchKeyDown(Key.A, physicalKey: PhysicalKey.A));
        Assert.Equal("a", Assert.Single(events).Character);
        Assert.True(host.DispatchKeyUp(Key.A, physicalKey: PhysicalKey.A));
    }

    [Theory]
    [InlineData(NavigationMethod.Tab, KeyModifiers.None, ViewFocusDirection.Forward)]
    [InlineData(NavigationMethod.Tab, KeyModifiers.Shift, ViewFocusDirection.Backward)]
    [InlineData(NavigationMethod.Pointer, KeyModifiers.None, ViewFocusDirection.Undefined)]
    public void PlumixHost_ReportsViewFocusEntryDirection(
        NavigationMethod method,
        KeyModifiers modifiers,
        ViewFocusDirection expectedDirection)
    {
        var observer = new ViewFocusObserver();
        WidgetsBinding.Instance.AddObserver(observer);
        try
        {
            var host = new TestPlumixHost();
            host.DispatchGotFocus(method, modifiers);
            host.DispatchGotFocus(method, modifiers);
            host.DispatchLostFocus();
            host.DispatchLostFocus();

            Assert.Collection(observer.Events,
                gained =>
                {
                    Assert.Equal(host.RootFlutterView.ViewId, gained.ViewId);
                    Assert.Equal(ViewFocusState.Focused, gained.State);
                    Assert.Equal(expectedDirection, gained.Direction);
                },
                lost =>
                {
                    Assert.Equal(host.RootFlutterView.ViewId, lost.ViewId);
                    Assert.Equal(ViewFocusState.Unfocused, lost.State);
                    Assert.Equal(ViewFocusDirection.Undefined, lost.Direction);
                });
        }
        finally
        {
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    [Fact]
    public void PlumixHost_WindowActivationRestoresFocusedViewOnce()
    {
        var observer = new ViewFocusObserver();
        WidgetsBinding.Instance.AddObserver(observer);
        try
        {
            var host = new TestPlumixHost();
            host.DispatchGotFocus(NavigationMethod.Pointer, KeyModifiers.None);
            host.UpdateViewFocusForWindowActivation(active: false);
            host.UpdateViewFocusForWindowActivation(active: false);
            host.UpdateViewFocusForWindowActivation(active: true);
            host.UpdateViewFocusForWindowActivation(active: true);

            Assert.Collection(observer.Events,
                gained => Assert.Equal(ViewFocusState.Focused, gained.State),
                lost => Assert.Equal(ViewFocusState.Unfocused, lost.State),
                regained =>
                {
                    Assert.Equal(ViewFocusState.Focused, regained.State);
                    Assert.Equal(ViewFocusDirection.Undefined, regained.Direction);
                });
        }
        finally
        {
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    [Fact]
    public void PlumixHost_Backspace_IsNotABackButton()
    {
        var observer = new PopRouteObserver { Result = Task.FromResult(true) };
        WidgetsBinding.Instance.AddObserver(observer);
        try
        {
            var host = new TestPlumixHost();

            // Avalonia's `Key.Back` is Backspace (Android's KEYCODE_DEL); nobody handles it here.
            Assert.False(host.DispatchKeyDown(Key.Back, physicalKey: PhysicalKey.Backspace));
            host.DispatchKeyUp(Key.Back, physicalKey: PhysicalKey.Backspace);
            Assert.Equal(0, observer.Calls);

            // Android's KEYCODE_BACK arrives as Escape.
            Assert.True(host.DispatchKeyDown(Key.Escape, physicalKey: PhysicalKey.Escape));
            host.DispatchKeyUp(Key.Escape, physicalKey: PhysicalKey.Escape);
            Assert.Equal(1, observer.Calls);
        }
        finally
        {
            WidgetsBinding.Instance.RemoveObserver(observer);
        }
    }

    private sealed class PopRouteObserver : WidgetsBindingObserver
    {
        public Task<bool> Result { get; set; } = Task.FromResult(false);

        public int Calls { get; private set; }

        public Task<bool> DidPopRoute()
        {
            Calls += 1;
            return Result;
        }
    }

    private sealed class ViewFocusObserver : WidgetsBindingObserver
    {
        public List<ViewFocusEvent> Events { get; } = [];

        public void DidChangeViewFocus(ViewFocusEvent @event)
        {
            Events.Add(@event);
        }
    }

    private sealed class TestPlumixHost : PlumixHost
    {
        public new void AttachPlatformChannelHandler() => base.AttachPlatformChannelHandler();

        public new void DetachPlatformChannelHandler() => base.DetachPlatformChannelHandler();

        public void DispatchGotFocus(NavigationMethod method, KeyModifiers modifiers)
        {
            RaiseEvent(new FocusChangedEventArgs(InputElement.GotFocusEvent)
            {
                Source = this,
                NavigationMethod = method,
                KeyModifiers = modifiers
            });
        }

        public void DispatchLostFocus()
        {
            RaiseEvent(new FocusChangedEventArgs(InputElement.LostFocusEvent) { Source = this });
        }

        public bool DispatchKeyDown(
            Key key,
            KeyModifiers modifiers = KeyModifiers.None,
            string? keySymbol = null,
            PhysicalKey physicalKey = PhysicalKey.None)
        {
            var args = new KeyEventArgs
            {
                Key = key,
                PhysicalKey = physicalKey,
                KeyModifiers = modifiers,
                KeySymbol = keySymbol
            };

            OnKeyDown(args);
            return args.Handled;
        }

        public bool DispatchKeyUp(
            Key key,
            KeyModifiers modifiers = KeyModifiers.None,
            string? keySymbol = null,
            PhysicalKey physicalKey = PhysicalKey.None)
        {
            var args = new KeyEventArgs
            {
                Key = key,
                PhysicalKey = physicalKey,
                KeyModifiers = modifiers,
                KeySymbol = keySymbol
            };

            OnKeyUp(args);
            return args.Handled;
        }
    }
}
