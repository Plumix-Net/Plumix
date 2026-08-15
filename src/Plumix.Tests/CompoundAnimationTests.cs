using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Coverage for the compound-animation primitives ported from
/// `flutter/packages/flutter/lib/src/animation/animations.dart`.
/// </summary>
public sealed class CompoundAnimationTests
{
    [Fact]
    public void AnimationMinAndMax_TrackTheirChildrenAndPreferAnAnimatingStatus()
    {
        using var first = new AnimationController(duration: TimeSpan.FromSeconds(1));
        using var next = new AnimationController(duration: TimeSpan.FromSeconds(1));
        var min = new AnimationMin<double>(first, next);
        var max = new AnimationMax<double>(first, next);
        var mean = new AnimationMean(first, next);

        first.SetValue(0.25);
        next.SetValue(0.75);

        Assert.Equal(0.25, min.Value);
        Assert.Equal(0.75, max.Value);
        Assert.Equal(0.5, mean.Value);

        // A mid-range value reports the controller's own direction, so bring both children back to a
        // bound before checking that the status is next's when next is animating, and first's otherwise.
        first.SetValue(0.0);
        next.SetValue(0.0);
        Assert.Equal(AnimationStatus.Dismissed, min.Status);
        next.Forward();
        Assert.Equal(AnimationStatus.Forward, min.Status);
    }

    [Fact]
    public void CompoundAnimation_NotifiesOnlyWhenTheCombinedValueChanges()
    {
        using var first = new AnimationController(duration: TimeSpan.FromSeconds(1));
        using var next = new AnimationController(duration: TimeSpan.FromSeconds(1));
        var min = new AnimationMin<double>(first, next);

        first.SetValue(0.2);
        next.SetValue(0.8);

        int notifications = 0;
        min.AddListener(() => notifications++);

        next.SetValue(0.1);
        Assert.Equal(1, notifications);

        next.SetValue(0.05);
        Assert.Equal(2, notifications);

        // The minimum is still 0.05, so no notification is sent.
        first.SetValue(0.9);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void TrainHoppingAnimation_HopsWhenTheNextTrainCatchesTheCurrentOne()
    {
        using var current = new AnimationController(duration: TimeSpan.FromSeconds(1));
        using var next = new AnimationController(duration: TimeSpan.FromSeconds(1));
        current.SetValue(0.8);
        next.SetValue(0.2);

        bool switched = false;
        using var hopping = new TrainHoppingAnimation(current, next, () => switched = true);
        Assert.Same(current, hopping.CurrentTrain);
        Assert.Equal(0.8, hopping.Value);

        // Mode is "maximize" because the current train started above the next one.
        next.SetValue(0.5);
        Assert.False(switched);
        Assert.Same(current, hopping.CurrentTrain);

        next.SetValue(0.9);
        Assert.True(switched);
        Assert.Same(next, hopping.CurrentTrain);
        Assert.Equal(0.9, hopping.Value);
    }

    [Fact]
    public void TrainHoppingAnimation_WithEqualValues_UsesTheNextTrainImmediately()
    {
        using var current = new AnimationController(duration: TimeSpan.FromSeconds(1));
        using var next = new AnimationController(duration: TimeSpan.FromSeconds(1));
        current.SetValue(0.4);
        next.SetValue(0.4);

        bool switched = false;
        using var hopping = new TrainHoppingAnimation(current, next, () => switched = true);

        Assert.Same(next, hopping.CurrentTrain);
        Assert.False(switched);
    }

    [Fact]
    public void TrainHoppingAnimation_WithoutANextTrain_ProxiesForever()
    {
        using var current = new AnimationController(duration: TimeSpan.FromSeconds(1));
        using var hopping = new TrainHoppingAnimation(current, null);

        int notifications = 0;
        hopping.AddListener(() => notifications++);

        current.SetValue(0.3);
        Assert.Equal(0.3, hopping.Value);
        Assert.Same(current, hopping.CurrentTrain);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void AnimatableChainAndDrive_ComposeInSourceOrder()
    {
        using var parent = new AnimationController(duration: TimeSpan.FromSeconds(1));
        Animatable<double> chained = new DoubleTween(begin: 0.875, end: 1.0)
            .Chain(new CurveTween(Curves.EaseIn));
        Animation<double> driven = parent.Drive(chained);

        parent.SetValue(0.0);
        Assert.Equal(0.875, driven.Value, 6);

        parent.SetValue(1.0);
        Assert.Equal(1.0, driven.Value, 6);

        parent.SetValue(0.5);
        double expected = 0.875 + (0.125 * Curves.EaseIn(0.5));
        Assert.Equal(expected, driven.Value, 6);
    }
}
