using Avalonia.Input;
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
                if (!string.Equals(@event.Key, "Space", StringComparison.Ordinal))
                {
                    return KeyEventResult.Ignored;
                }

                if (@event.IsDown)
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

        var host = new TestPlumixHost();
        Assert.True(host.DispatchKeyDown(Key.OemQuestion, keySymbol: "?"));
        Assert.True(host.DispatchKeyDown(Key.OemQuestion, keySymbol: "?"));
        Assert.True(host.DispatchKeyUp(Key.OemQuestion, keySymbol: "?"));

        Assert.Equal("?", events[0].Character);
        Assert.False(events[0].IsRepeat);
        Assert.True(events[1].IsRepeat);
        Assert.False(events[2].IsRepeat);
    }

    private sealed class TestPlumixHost : PlumixHost
    {
        public bool DispatchKeyDown(
            Key key,
            KeyModifiers modifiers = KeyModifiers.None,
            string? keySymbol = null)
        {
            var args = new KeyEventArgs
            {
                Key = key,
                KeyModifiers = modifiers,
                KeySymbol = keySymbol
            };

            OnKeyDown(args);
            return args.Handled;
        }

        public bool DispatchKeyUp(
            Key key,
            KeyModifiers modifiers = KeyModifiers.None,
            string? keySymbol = null)
        {
            var args = new KeyEventArgs
            {
                Key = key,
                KeyModifiers = modifiers,
                KeySymbol = keySymbol
            };

            OnKeyUp(args);
            return args.Handled;
        }
    }
}
