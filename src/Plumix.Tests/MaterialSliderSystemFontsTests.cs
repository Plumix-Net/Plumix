using System.Reflection;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/painting/system_fonts_test.dart
// Slider/range-slider regression contracts, including their overlay value indicators.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSliderSystemFontsTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SystemChannelDefersRelayoutAndRefreshesLabelsAndTheOverlay(bool range, bool showLabels)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        Widget slider = range
            ? new RangeSlider(values: new RangeValues(0.2, 0.8), onChanged: _ => { }, divisions: 5,
                labels: showLabels ? new RangeLabels("20", "80") : null)
            : new Slider(value: 0.2, onChanged: _ => { }, divisions: 5, label: showLabels ? "20" : null);
        tester.PumpWidget(new MaterialApp(home: new Material.Material(child: new Center(child: slider))));
        RenderBox render = Descendants(tester.RenderView)
            .OfType<RenderBox>().Single(box => range ? box is RenderRangeSlider : box is RenderSlider);

        TestGesture gesture = tester.StartGesture(
            render.LocalToGlobal(new Point(render.Size.Width / 2, render.Size.Height / 2)), PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromMilliseconds(200));
        RenderObject? indicator = Descendants(tester.RenderView)
            .SingleOrDefault(box => box.GetType().Name == "RenderValueIndicator");
        if (showLabels) Assert.NotNull(indicator);
        string[] painterFields = range ? ["_startLabelPainter", "_endLabelPainter"] : ["_labelPainter"];
        TextPainter[] painters = showLabels
            ? painterFields.Select(field => (TextPainter)GetField(render, field)).ToArray()
            : [];
        object[] cachedLayouts = painters.Select(painter => GetField(painter, "_layoutCache")).ToArray();

        await NotifyFonts();
        await NotifyFonts();
        if (Constants.KDebugMode) Assert.False(render.DebugNeedsLayout);
        for (int i = 0; i < painters.Length; i++)
        {
            Assert.Same(cachedLayouts[i], GetField(painters[i], "_layoutCache"));
            Assert.True(painters[i].Width > 0); // Geometry remains valid during input handling.
        }

        bool? needsLayout = null;
        bool? indicatorNeedsLayout = null;
        SchedulerBinding.Instance.ScheduleFrameCallback(_ =>
        {
            needsLayout = render.DebugNeedsLayout;
            indicatorNeedsLayout = indicator?.DebugNeedsLayout;
        });
        tester.Pump();
        if (Constants.KDebugMode)
        {
            Assert.True(needsLayout);
            if (indicator is not null) Assert.True(indicatorNeedsLayout);
        }

        for (int i = 0; i < painters.Length; i++)
        {
            Assert.NotSame(cachedLayouts[i], GetField(painters[i], "_layoutCache"));
        }

        gesture.Up();
        tester.PumpAndSettle();
        tester.PumpWidget(new SizedBox());
        await NotifyFonts();
        tester.Pump(); // Removed and disposed label painters must no longer receive notifications.
        Assert.Null(tester.TakeException());
    }

    private static Task NotifyFonts() => ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
        SystemChannels.System.Name,
        SystemChannels.System.Codec.EncodeMessage(new Dictionary<string, object?> { ["type"] = "fontsChange" }),
        callback: null);

    private static object GetField(object target, string field) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private static IEnumerable<RenderObject> Descendants(RenderObject root)
    {
        yield return root;
        var children = new List<RenderObject>();
        root.VisitChildren(children.Add);
        foreach (RenderObject child in children)
        foreach (RenderObject descendant in Descendants(child))
            yield return descendant;
    }
}
