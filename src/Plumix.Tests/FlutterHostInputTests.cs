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
        var keyDownCount = 0;
        var keyUpCount = 0;
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

    private sealed class TestPlumixHost : PlumixHost
    {
        public bool DispatchKeyDown(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            var args = new KeyEventArgs
            {
                Key = key,
                KeyModifiers = modifiers
            };

            OnKeyDown(args);
            return args.Handled;
        }

        public bool DispatchKeyUp(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            var args = new KeyEventArgs
            {
                Key = key,
                KeyModifiers = modifiers
            };

            OnKeyUp(args);
            return args.Handled;
        }
    }
}
