using System.Reflection;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/painting/system_fonts_test.dart
//
// The RenderParagraph, RenderEditable, Banner, CupertinoDatePicker/CupertinoTimerPicker and non-idle
// phase cases. The slider, range slider and time picker cases live in MaterialSliderSystemFontsTests
// and MaterialTimePickerRestorationTests; 'Safe to query a RelayoutWhenSystemFontsChangeMixin ...'
// is SystemFontsRelayoutTests (Plumix has no WidgetToRenderBoxAdapter). The Cupertino picker cases
// assert the rebuild only: the C# pickers measure their columns on every build instead of caching
// `estimatedColumnWidths` / `numberLabelWidth`, so there is no cache to inspect.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SystemFontsDartParityTests
{
    // 'RenderParagraph relayout upon system fonts changes'
    [Fact]
    public async Task RenderParagraphRelayoutUponSystemFontsChanges()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new Text("text widget")));
        RenderParagraph renderObject = tester.RenderObject<RenderParagraph>(Find.Text("text widget"));
        object layoutCache = GetField(renderObject.TextPainter, "_layoutCache");

        await VerifyMarkedNeedsLayoutDuringTransientCallbacksPhase(tester, renderObject);

        // `systemFontsDidChange` also invalidated the text painter, so the relayout reshaped the text.
        Assert.NotSame(layoutCache, GetField(renderObject.TextPainter, "_layoutCache"));
        Assert.Null(tester.TakeException());
    }

    // 'RenderEditable relayout upon system fonts changes'
    [Fact]
    public async Task RenderEditableRelayoutUponSystemFontsChanges()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new SelectableText("text widget")));
        RenderEditable renderEditable = tester.AllRenderObjects.OfType<RenderEditable>().Single();

        await VerifyMarkedNeedsLayoutDuringTransientCallbacksPhase(tester, renderEditable);
        Assert.Null(tester.TakeException());
    }

    // 'Banner repaint upon system fonts changes'
    [Fact]
    public async Task BannerRepaintUponSystemFontsChanges()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Banner(
            message: "message",
            location: BannerLocation.TopStart,
            textDirection: TextDirection.Ltr,
            layoutDirection: TextDirection.Ltr));
        RenderObject renderObject = tester.RenderObject<RenderObject>(Find.ByType<Banner>());
        if (Constants.KDebugMode) Assert.False(renderObject.DebugNeedsPaint);

        await NotifyFonts();

        if (Constants.KDebugMode) Assert.True(renderObject.DebugNeedsPaint);
    }

    // 'CupertinoDatePicker reset cache upon system fonts change - date time mode' / '- date mode', plus
    // the monthYear and time modes, which Dart wires the same way.
    [Theory]
    [InlineData(CupertinoDatePickerMode.DateAndTime)]
    [InlineData(CupertinoDatePickerMode.Date)]
    [InlineData(CupertinoDatePickerMode.MonthYear)]
    [InlineData(CupertinoDatePickerMode.Time)]
    public async Task CupertinoDatePickerRebuildsUponSystemFontsChange(CupertinoDatePickerMode mode)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new CupertinoApp(home: new CupertinoDatePicker(onDateTimeChanged: _ => { }, mode: mode)));
        Element element = tester.Element(Find.ByType<CupertinoDatePicker>());
        Assert.False(element.Dirty);

        await NotifyFonts();

        Assert.True(element.Dirty);
        tester.PumpWidget(new SizedBox());
        await NotifyFonts(); // The disposed state no longer listens.
        Assert.Null(tester.TakeException());
    }

    // 'CupertinoDatePicker reset cache upon system fonts change - time mode'
    [Fact]
    public async Task CupertinoTimerPickerRebuildsUponSystemFontsChange()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new CupertinoApp(home: new CupertinoTimerPicker(onTimerDurationChanged: _ => { })));
        Element element = tester.Element(Find.ByType<CupertinoTimerPicker>());
        Assert.False(element.Dirty);

        await NotifyFonts();

        Assert.True(element.Dirty);
        tester.PumpWidget(new SizedBox());
        await NotifyFonts();
        Assert.Null(tester.TakeException());
    }

    // 'System fonts update during non-idle scheduler phase does not assert'
    [Fact]
    public void SystemFontsUpdateDuringNonIdleSchedulerPhaseDoesNotAssert()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new Text("Hello")));

        SchedulerBinding.Instance.ScheduleFrameCallback(timeStamp =>
        {
            Assert.Equal(SchedulerPhase.TransientCallbacks, SchedulerBinding.Instance.SchedulerPhase);
            _ = NotifyFonts();
        });

        tester.Pump();
        tester.Pump();

        Assert.Null(tester.TakeException());
        Assert.True(tester.Any(Find.Text("Hello")));
    }

    // The host hook sends the engine's message shape on the system channel.
    [Fact]
    public async Task HostNotificationReachesSystemFontsListeners()
    {
        int notifications = 0;
        void Listener() => notifications++;
        PaintingBinding.Instance.SystemFonts.AddListener(Listener);
        try
        {
            await HostSystemFonts.NotifySystemFontsChanged();
            Assert.Equal(1, notifications);
        }
        finally
        {
            PaintingBinding.Instance.SystemFonts.RemoveListener(Listener);
        }
    }

    // system_fonts_test.dart's `verifyMarkedNeedsLayoutDuringTransientCallbacksPhase`.
    private static async Task VerifyMarkedNeedsLayoutDuringTransientCallbacksPhase(
        FrameworkDartTester tester,
        RenderObject renderObject)
    {
        if (Constants.KDebugMode) Assert.False(renderObject.DebugNeedsLayout);

        await NotifyFonts();

        bool? needsLayout = null;
        SchedulerBinding.Instance.ScheduleFrameCallback(_ => needsLayout = renderObject.DebugNeedsLayout);

        // The fonts change does not mark the render object as needing layout immediately.
        if (Constants.KDebugMode) Assert.False(renderObject.DebugNeedsLayout);
        tester.Pump();
        Assert.NotNull(needsLayout);
        if (Constants.KDebugMode) Assert.True(needsLayout);
    }

    private static Task NotifyFonts() => ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
        SystemChannels.System.Name,
        SystemChannels.System.Codec.EncodeMessage(new Dictionary<string, object?> { ["type"] = "fontsChange" }),
        callback: null);

    private static object GetField(object target, string field) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
}
