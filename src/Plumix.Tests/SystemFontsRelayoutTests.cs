using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SystemFontsRelayoutTests : IDisposable
{
    public SystemFontsRelayoutTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public async Task FontChangesCoalesceUntilTheTransientPhaseAndStopAfterDetach()
    {
        var box = new FontAwareBox();
        var owner = new PipelineOwner();
        box.Attach(owner);
        box.Layout(BoxConstraints.Tight(new Size(100, 40)));

        await NotifyFonts();
        await NotifyFonts();
        Assert.Equal(0, box.Updates);
        if (Constants.KDebugMode) Assert.False(box.DebugNeedsLayout);

        Scheduler.HandleFrame(TimeSpan.FromMilliseconds(16));
        Assert.Equal(1, box.Updates);
        Assert.Equal(SchedulerPhase.TransientCallbacks, box.UpdatePhase);
        if (Constants.KDebugMode) Assert.True(box.DebugNeedsLayout);
        box.Layout(BoxConstraints.Tight(new Size(100, 40)));

        box.Detach();
        await NotifyFonts();
        Scheduler.HandleFrame(TimeSpan.FromMilliseconds(32));
        Assert.Equal(1, box.Updates);

        box.Attach(owner);
        await NotifyFonts();
        Scheduler.HandleFrame(TimeSpan.FromMilliseconds(48));
        Assert.Equal(2, box.Updates);
        box.Detach();
        box.Dispose();
        await NotifyFonts();
        Scheduler.HandleFrame(TimeSpan.FromMilliseconds(64));
        Assert.Equal(2, box.Updates);
    }

    [Fact]
    public void MidFrameFontChangeWaitsForTheNextFrame()
    {
        var box = new FontAwareBox();
        box.Attach(new PipelineOwner());
        Scheduler.ScheduleFrameCallback(_ => Scheduler.ScheduleMicrotask(() =>
        {
            Assert.Equal(SchedulerPhase.MidFrameMicrotasks, Scheduler.Phase);
            NotifyFonts().GetAwaiter().GetResult();
        }));

        Scheduler.HandleFrame(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, box.Updates);
        Scheduler.HandleFrame(TimeSpan.FromMilliseconds(32));
        Assert.Equal(1, box.Updates);
        Assert.Equal(SchedulerPhase.TransientCallbacks, box.UpdatePhase);
        box.Detach();
        box.Dispose();
    }

    [Fact]
    public async Task DefaultHookInvalidatesLayout()
    {
        var box = new FontAwareBox(useDefaultHook: true);
        box.Attach(new PipelineOwner());
        box.Layout(BoxConstraints.Tight(new Size(100, 40)));
        await NotifyFonts();
        box.Layout(BoxConstraints.Tight(new Size(100, 40)));
        Assert.Equal(1, box.Layouts);
        Scheduler.HandleFrame(TimeSpan.FromMilliseconds(16));
        box.Layout(BoxConstraints.Tight(new Size(100, 40)));
        Assert.Equal(2, box.Layouts);
        box.Detach();
        box.Dispose();
    }

    private static Task NotifyFonts() => PaintingBinding.Instance.HandleSystemMessage(
        new Dictionary<string, object?> { ["type"] = "fontsChange" });

    private sealed class FontAwareBox : RenderBox
    {
        private readonly RelayoutWhenSystemFontsChangeMixin _fonts;

        public FontAwareBox(bool useDefaultHook = false)
        {
            _fonts = new RelayoutWhenSystemFontsChangeMixin(this, useDefaultHook ? null : OnSystemFontsDidChange);
        }

        public int Updates { get; private set; }
        public int Layouts { get; private set; }
        public SchedulerPhase? UpdatePhase { get; private set; }

        private void OnSystemFontsDidChange()
        {
            _fonts.SystemFontsDidChange();
            Updates++;
            UpdatePhase = Scheduler.Phase;
        }

        protected override void OnAttach()
        {
            base.OnAttach();
            _fonts.OnAttach();
        }

        protected override void OnDetach()
        {
            _fonts.OnDetach();
            base.OnDetach();
        }

        protected override void PerformLayout()
        {
            Layouts++;
            Size = Constraints.Smallest;
        }

        public override void Paint(PaintingContext context, Point offset) { }
    }
}
