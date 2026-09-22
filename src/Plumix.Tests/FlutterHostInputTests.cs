using Avalonia.Input;
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
    }

    public void Dispose()
    {
        FrameworkFocusManager.Instance.ResetForTests();
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
        Scheduler.FlushMicrotasks();

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
        Scheduler.FlushMicrotasks();

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
