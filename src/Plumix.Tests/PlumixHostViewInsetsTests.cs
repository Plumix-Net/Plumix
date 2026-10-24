using Avalonia;
using Avalonia.Controls.Platform;
using Xunit;

namespace Plumix.Tests;

// C#-only infrastructure: the host's `viewInsets.bottom`, which the engine computes from the part of
// the view the keyboard frame covers, measured from the view's bottom.
public sealed class PlumixHostViewInsetsTests
{
    private static readonly Rect Screen = new(0, 0, 402, 874);

    [Fact]
    public void OpenKeyboard_InsetsTheCoveredBottom()
    {
        // iOS: the frame includes the home-indicator strip, so the inset is the whole frame.
        Assert.Equal(new Thickness(0, 0, 0, 336),
            PlumixHost.ResolveViewInsets(InputPaneState.Open, new Rect(0, 538, 402, 336), Screen));
        // Android: Avalonia reports the pane above the 24 dp navigation bar; the inset reaches the bottom,
        // so the content no longer slides under the keyboard by the bar's height.
        Assert.Equal(new Thickness(0, 0, 0, 300),
            PlumixHost.ResolveViewInsets(InputPaneState.Open, new Rect(0, 574, 402, 276), Screen));
    }

    [Fact]
    public void HiddenKeyboard_KeepsItsFrameBelowTheScreen_AndInsetsNothing()
    {
        // UIKit's will-hide notification leaves the full-height end frame off screen; the inset used
        // to stay at its height after a route with a text field was popped.
        var below = new Rect(0, 874, 402, 336);

        Assert.Equal(default, PlumixHost.ResolveViewInsets(InputPaneState.Closed, below, Screen));
        Assert.Equal(default, PlumixHost.ResolveViewInsets(InputPaneState.Open, below, Screen));
        Assert.Equal(default, PlumixHost.ResolveViewInsets(InputPaneState.Closed, new Rect(0, 538, 402, 336), Screen));
    }

    [Fact]
    public void HardwareKeyboard_InsetsOnlyTheVisibleShortcutBar()
    {
        // With a hardware keyboard iOS "opens" a full-height frame of which only the bar is on screen.
        Assert.Equal(new Thickness(0, 0, 0, 55),
            PlumixHost.ResolveViewInsets(InputPaneState.Open, new Rect(0, 819, 402, 336), Screen));
    }

    [Fact]
    public void ViewOffsetInTheTopLevel_MeasuresFromItsOwnBottom()
    {
        var view = new Rect(0, 100, 402, 500);

        Assert.Equal(new Thickness(0, 0, 0, 62),
            PlumixHost.ResolveViewInsets(InputPaneState.Open, new Rect(0, 538, 402, 336), view));
        Assert.Equal(default, PlumixHost.ResolveViewInsets(InputPaneState.Open, new Rect(0, 700, 402, 174), view));
    }
}
