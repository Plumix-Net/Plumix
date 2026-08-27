using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialRangeSliderTests
{
    [Fact]
    public void RangeSlider_CustomShapesAndThumbSelector_AreUsed()
    {
        bool selectorCalled = false;
        var track = new RecordingRangeTrackShape();
        var tick = new RecordingRangeTickMarkShape();
        var thumb = new RecordingRangeThumbShape();
        var indicator = new RecordingRangeValueIndicatorShape();
        RangeThumbSelector selector = (_, _, _, _, _, _) =>
        {
            selectorCalled = true;
            return Thumb.End;
        };
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                RangeTrackShape: track,
                RangeTickMarkShape: tick,
                RangeThumbShape: thumb,
                RangeValueIndicatorShape: indicator,
                ThumbSelector: selector,
                ShowValueIndicator: ShowValueIndicator.AlwaysVisible),
        };
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: theme,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220.0,
                            child: new RangeSlider(
                                values: new RangeValues(0.2, 0.8),
                                divisions: 4,
                                labels: new RangeLabels("20", "80"),
                                onChanged: _ => { })))));

            harness.Pump(new Size(260.0, 120.0));
            DispatchPointerDown(binding, harness.RenderView, pointer: 799, position: new Point(40.0, 24.0));

            Assert.True(selectorCalled);
            Assert.True(track.PreferredRectCalls > 0);
            Assert.True(track.PaintCalls > 0);
            Assert.True(tick.PaintCalls > 0);
            Assert.True(thumb.PaintCalls >= 2);
            object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
            Assert.NotNull(render);
            SliderThemeData effectiveTheme = ReadProperty<SliderThemeData>(render!, "SliderTheme");
            Assert.Same(indicator, effectiveTheme.RangeValueIndicatorShape);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Constructor_Throws_OnInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new RangeSlider(values: new RangeValues(0.2, 0.8), min: 1, max: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentException>(() => new RangeSlider(values: new RangeValues(0.8, 0.2), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(-0.1, 0.2), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(0.2, 1.1), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(0.2, 0.7), min: 0, max: 1, divisions: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(double.NaN, 0.7), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(
            values: new RangeValues(0.2, 0.7),
            onChanged: _ => { },
            padding: new Thickness(0, -1, 0, 0)));
    }

    [Fact]
    public void RangeSlider_ExtendedApi_StoresFlutterParityValues()
    {
        var cursor = MaterialStateProperty<MouseCursor?>.All(new SystemMouseCursor("range-slider"));
        var slider = new RangeSlider(
            values: new RangeValues(0.2, 0.8),
            onChanged: _ => { },
            divisions: 5,
            labels: new RangeLabels("20", "80"),
            mouseCursor: cursor,
            padding: new Thickness(10, 4),
            year2023: false);

        Assert.Equal(new RangeLabels("20", "80"), slider.Labels);
        Assert.Same(cursor, slider.MouseCursor);
        Assert.Equal(new Thickness(10, 4), slider.Padding);
        Assert.False(slider.Year2023);
    }

    [Fact]
    public void RangeSlider_ExtendedThemeTokensReachRenderObject()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                ActiveTickMarkColor: Colors.Gold,
                InactiveTickMarkColor: Colors.DarkSlateBlue,
                TickMarkRadius: 2.5,
                ValueIndicatorColor: Colors.OrangeRed,
                ShowValueIndicator: ShowValueIndicator.AlwaysVisible,
                Padding: new Thickness(16, 6),
                TrackGap: 8,
                Year2023: false)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.8),
                        divisions: 5,
                        labels: new RangeLabels("20", "80"),
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));
        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Gold, ReadProperty<Color>(render!, "ActiveTickMarkColor"));
        Assert.Equal(Colors.DarkSlateBlue, ReadProperty<Color>(render, "InactiveTickMarkColor"));
        Assert.Equal(2.5, ReadProperty<double>(render, "TickMarkRadius"));
        Assert.Equal(Colors.OrangeRed, ReadProperty<Color>(render, "ValueIndicatorColor"));
        Assert.Equal(ShowValueIndicator.AlwaysVisible, ReadProperty<ShowValueIndicator>(render, "ShowValueIndicator"));
        Assert.Equal(new Thickness(16, 6), ReadProperty<Thickness>(render, "Padding"));
        Assert.Equal(8, ReadProperty<double>(render, "TrackGap"));
        Assert.Equal(new Size(4, 44), ReadProperty<Size>(render, "ThumbSize"));
        Assert.Equal(16, ReadProperty<double>(render, "TrackHeight"));
        Assert.Equal(theme.ColorScheme.SecondaryContainer, ReadProperty<Color>(render, "InactiveTrackColor"));
    }

    [Fact]
    public void RangeSlider_DefaultM3Year2023_UsesM2TrackColors()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            PrimaryColor = Colors.Coral,
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = Colors.Coral,
                SurfaceContainerHighest = Colors.PowderBlue,
            },
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.7),
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(ApplyOpacity(Colors.Coral, 0.24), ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void RangeSlider_ThemeColors_Apply_WhenWidgetColorsAreMissing()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                ActiveTrackColor: Colors.DarkGreen,
                InactiveTrackColor: Colors.LightGreen,
                ThumbColor: Colors.Gold)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.7),
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkGreen, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.LightGreen, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Gold, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void RangeSlider_WidgetColors_OverrideThemeColors()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                ActiveTrackColor: Colors.DarkGreen,
                InactiveTrackColor: Colors.LightGreen,
                ThumbColor: Colors.Gold)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.7),
                        activeColor: Colors.DarkRed,
                        inactiveColor: Colors.MistyRose,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.MistyRose, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void RangeSlider_DragStartThumb_InvokesLifecycleCallbacksAndUpdatesStartValue()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            RangeValues? start = null;
            RangeValues? changed = null;
            RangeValues? end = null;

            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220,
                            child: new RangeSlider(
                                values: new RangeValues(0.2, 0.7),
                                onChangeStart: values => start = values,
                                onChanged: values => changed = values,
                                onChangeEnd: values => end = values)))));

            harness.Pump(new Size(280, 120));

            DispatchPointerDown(binding, harness.RenderView, pointer: 700, position: new Point(50, 24));
            DispatchPointerMove(binding, harness.RenderView, pointer: 700, position: new Point(90, 24));
            DispatchPointerUp(binding, harness.RenderView, pointer: 700, position: new Point(90, 24));
            harness.Pump(new Size(280, 120));

            Assert.NotNull(start);
            Assert.NotNull(changed);
            Assert.NotNull(end);
            Assert.Equal(0.2, start!.Value.Start, 2);
            Assert.Equal(0.7, start.Value.End, 2);
            Assert.Equal(0.39, changed!.Value.Start, 2);
            Assert.Equal(0.7, changed.Value.End, 2);
            Assert.Equal(0.39, end!.Value.Start, 2);
            Assert.Equal(0.7, end.Value.End, 2);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_DiscreteDrag_SnapsToDivisions()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            RangeValues? changed = null;

            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220,
                            child: new RangeSlider(
                                values: new RangeValues(0.2, 0.6),
                                divisions: 5,
                                onChanged: values => changed = values)))));

            harness.Pump(new Size(280, 120));

            DispatchPointerDown(binding, harness.RenderView, pointer: 701, position: new Point(130, 24));
            DispatchPointerMove(binding, harness.RenderView, pointer: 701, position: new Point(150, 24));
            DispatchPointerUp(binding, harness.RenderView, pointer: 701, position: new Point(150, 24));
            harness.Pump(new Size(280, 120));

            Assert.NotNull(changed);
            Assert.Equal(0.2, changed!.Value.Start, 2);
            Assert.Equal(0.8, changed.Value.End, 2);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_ExposeOneNodePerThumbInStartThenEndOrder()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(BuildApp(new RangeValues(10.0, 30.0), max: 100.0));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
            IReadOnlyList<SemanticsNode> thumbs = FindThumbNodes(root);

            Assert.Equal(2, thumbs.Count);

            // Dart's `_createSemanticsConfiguration` puts everything readable on the two thumb nodes; the
            // parent carries only `isSemanticBoundary`.
            Assert.Equal("10%", thumbs[0].Value);
            Assert.Equal("15%", thumbs[0].IncreasedValue);
            Assert.Equal("5%", thumbs[0].DecreasedValue);
            Assert.Equal("30%", thumbs[1].Value);
            Assert.Equal("35%", thumbs[1].IncreasedValue);
            Assert.Equal("25%", thumbs[1].DecreasedValue);

            foreach (SemanticsNode thumb in thumbs)
            {
                Assert.True(thumb.Flags.HasFlag(SemanticsFlags.IsSlider));
                Assert.True(thumb.Flags.HasFlag(SemanticsFlags.IsEnabled));
                Assert.True(thumb.Flags.HasFlag(SemanticsFlags.HasEnabledState));
                Assert.True(thumb.Flags.HasFlag(SemanticsFlags.IsFocusable));
                Assert.False(thumb.Flags.HasFlag(SemanticsFlags.IsFocused));
                Assert.True(thumb.Actions.HasFlag(SemanticsActions.Increase));
                Assert.True(thumb.Actions.HasFlag(SemanticsActions.Decrease));
            }
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_SaturateTheAdjustmentThatWouldCrossTheOtherThumb()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            // The two thumbs are one adjustment unit apart, so Flutter reports the *current* value as the
            // increased start and the decreased end rather than clamping to the neighbour.
            using var harness = new WidgetRenderHarness(
                BuildApp(new RangeValues(10.0, 12.0), max: 100.0, labels: new RangeLabels("Begin", "End")));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
            IReadOnlyList<SemanticsNode> thumbs = FindThumbNodes(root);

            Assert.Equal(2, thumbs.Count);
            Assert.Equal("10%", thumbs[0].Value);
            Assert.Equal("10%", thumbs[0].IncreasedValue);
            Assert.Equal("5%", thumbs[0].DecreasedValue);
            Assert.Equal("12%", thumbs[1].Value);
            Assert.Equal("17%", thumbs[1].IncreasedValue);
            Assert.Equal("12%", thumbs[1].DecreasedValue);

            // `RangeSlider.labels` never reaches the semantics tree.
            Assert.True(string.IsNullOrEmpty(thumbs[0].Label));
            Assert.True(string.IsNullOrEmpty(thumbs[1].Label));
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_UseFortyEightSquareRectsCenteredOnEachThumb()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(BuildApp(new RangeValues(10.0, 30.0), max: 100.0));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
            IReadOnlyList<SemanticsNode> thumbs = FindThumbNodes(root);

            Assert.Equal(2, thumbs.Count);
            foreach (SemanticsNode thumb in thumbs)
            {
                Assert.Equal(48.0, thumb.Rect.Width, 3);
                Assert.Equal(48.0, thumb.Rect.Height, 3);
            }

            // 10% and 30% of the same track: the end thumb sits to the right of the start thumb.
            Assert.True(thumbs[0].Rect.Left < thumbs[1].Rect.Left);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_SwapTheTwoThumbRectsUnderRtl()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var ltr = new WidgetRenderHarness(BuildApp(new RangeValues(10.0, 30.0), max: 100.0));
            SemanticsNode ltrRoot = Assert.IsType<SemanticsNode>(ltr.PumpAndGetSemantics(ViewSize));
            IReadOnlyList<SemanticsNode> ltrThumbs = FindThumbNodes(ltrRoot);

            using var rtl = new WidgetRenderHarness(
                BuildApp(new RangeValues(10.0, 30.0), max: 100.0, textDirection: TextDirection.Rtl));
            SemanticsNode rtlRoot = Assert.IsType<SemanticsNode>(rtl.PumpAndGetSemantics(ViewSize));
            IReadOnlyList<SemanticsNode> rtlThumbs = FindThumbNodes(rtlRoot);

            Assert.Equal(2, rtlThumbs.Count);

            // Child order stays [start, end] and the value strings are unchanged...
            Assert.Equal("10%", rtlThumbs[0].Value);
            Assert.Equal("30%", rtlThumbs[1].Value);

            double ltrStart = Center(ltrThumbs[0]);
            double ltrEnd = Center(ltrThumbs[1]);
            double rtlStart = Center(rtlThumbs[0]);
            double rtlEnd = Center(rtlThumbs[1]);

            // ...but the rects are swapped: the start node takes the box where the *end* thumb paints, so
            // it still reads left-to-right on screen (Flutter's own RTL test has start at 526, end at 677).
            Assert.True(rtlStart < rtlEnd);

            // Each RTL node mirrors the *other* LTR node about the track centre; without the swap both
            // sums below would differ. This is the assertion that actually pins the swap down.
            Assert.Equal(ltrStart + rtlEnd, ltrEnd + rtlStart, 3);
            Assert.Equal(ltrThumbs[0].Rect.Width, rtlThumbs[0].Rect.Width, 3);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_ReportFocusOnTheThumbThatHoldsIt()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(BuildApp(new RangeValues(10.0, 30.0), max: 100.0));
            harness.Pump(ViewSize);

            RangeSlider.RangeSliderState state = StateOf(harness);
            Assert.True(state.StartFocusNode.RequestFocus());

            IReadOnlyList<SemanticsNode> thumbs = FindThumbNodes(
                Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize)));
            Assert.True(thumbs[0].Flags.HasFlag(SemanticsFlags.IsFocused));
            Assert.False(thumbs[1].Flags.HasFlag(SemanticsFlags.IsFocused));

            Assert.True(state.EndFocusNode.RequestFocus());

            thumbs = FindThumbNodes(Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize)));
            Assert.False(thumbs[0].Flags.HasFlag(SemanticsFlags.IsFocused));
            Assert.True(thumbs[1].Flags.HasFlag(SemanticsFlags.IsFocused));

            // Values and geometry are untouched by the focus move.
            Assert.Equal("10%", thumbs[0].Value);
            Assert.Equal("30%", thumbs[1].Value);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Tab_MovesFocusFromTheStartThumbToTheEndThumb()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(BuildApp(new RangeValues(10.0, 30.0), max: 100.0));
            harness.Pump(ViewSize);

            RangeSlider.RangeSliderState state = StateOf(harness);
            Assert.True(state.StartFocusNode.RequestFocus());
            harness.Pump(ViewSize);
            Assert.Same(state.StartFocusNode, FocusManager.Instance.PrimaryFocus);

            FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
            harness.Pump(ViewSize);

            Assert.Same(state.EndFocusNode, FocusManager.Instance.PrimaryFocus);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_SemanticIncreaseAndDecrease_ReportOnlyOnChanged()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var calls = new List<string>();
            using var harness = new WidgetRenderHarness(
                BuildApp(
                    new RangeValues(20.0, 60.0),
                    max: 100.0,
                    onChanged: values => calls.Add(
                        FormattableString.Invariant($"changed {values.Start:0}-{values.End:0}")),
                    onChangeStart: _ => calls.Add("start"),
                    onChangeEnd: _ => calls.Add("end")));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
            IReadOnlyList<SemanticsNode> thumbs = FindThumbNodes(root);

            // Unlike Slider, the range actions call onChanged only — never onChangeStart/onChangeEnd.
            Assert.True(thumbs[0].PerformAction(SemanticsActions.Increase));
            Assert.Equal(["changed 25-60"], calls);

            calls.Clear();
            Assert.True(thumbs[1].PerformAction(SemanticsActions.Decrease));
            Assert.Equal(["changed 20-55"], calls);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_DropTheAdjustmentActionsWhenDisabled()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                BuildApp(new RangeValues(10.0, 30.0), max: 100.0, omitOnChanged: true));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
            IReadOnlyList<SemanticsNode> thumbs = FindThumbNodes(root);

            Assert.Equal(2, thumbs.Count);
            foreach (SemanticsNode thumb in thumbs)
            {
                Assert.False(thumb.Flags.HasFlag(SemanticsFlags.IsEnabled));
                Assert.True(thumb.Flags.HasFlag(SemanticsFlags.HasEnabledState));

                // `isFocusable` is unconditional on a range thumb, even disabled.
                Assert.True(thumb.Flags.HasFlag(SemanticsFlags.IsFocusable));
                Assert.False(thumb.Actions.HasFlag(SemanticsActions.Increase));
                Assert.False(thumb.Actions.HasFlag(SemanticsActions.Decrease));
            }

            // The value strings survive being disabled.
            Assert.Equal("10%", thumbs[0].Value);
            Assert.Equal("30%", thumbs[1].Value);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_RebuildAfterTheNodesWereCleared()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(BuildApp(new RangeValues(10.0, 30.0), max: 100.0));
            Assert.Equal(2, FindThumbNodes(
                Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize))).Count);

            // Dart nulls both synthesized nodes in `clearSemantics` so a later pass rebuilds them instead
            // of reusing nodes the previous owner dropped.
            harness.RenderView.ClearSemantics();

            IReadOnlyList<SemanticsNode> rebuilt = FindThumbNodes(
                Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize)));
            Assert.Equal(2, rebuilt.Count);
            Assert.Equal("10%", rebuilt[0].Value);
            Assert.Equal("30%", rebuilt[1].Value);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_PointerDown_MovesFocusOntoTheThumbItPicked()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(BuildApp(new RangeValues(20.0, 80.0), max: 100.0));
            harness.Pump(ViewSize);
            RangeSlider.RangeSliderState state = StateOf(harness);

            DispatchPointerDown(binding, harness.RenderView, pointer: 811, position: new Point(60, 24));
            harness.Pump(ViewSize);
            Assert.True(state.StartFocusNode.HasFocus);
            DispatchPointerUp(binding, harness.RenderView, pointer: 811, position: new Point(60, 24));

            DispatchPointerDown(binding, harness.RenderView, pointer: 812, position: new Point(190, 24));
            harness.Pump(ViewSize);
            Assert.True(state.EndFocusNode.HasFocus);
            DispatchPointerUp(binding, harness.RenderView, pointer: 812, position: new Point(190, 24));
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
            binding.ResetForTests();
        }
    }

    private static readonly Size ViewSize = new(280, 120);

    private static Widget BuildApp(
        RangeValues values,
        double max,
        double min = 0.0,
        RangeLabels? labels = null,
        TextDirection textDirection = TextDirection.Ltr,
        Action<RangeValues>? onChanged = null,
        Action<RangeValues>? onChangeStart = null,
        Action<RangeValues>? onChangeEnd = null,
        bool omitOnChanged = false)
    {
        return new Theme(
            data: new ThemeData(platform: TargetPlatform.Android),
            child: new Directionality(
                textDirection: textDirection,
                child: new Align(
                    alignment: Alignment.TopLeft,
                    child: new SizedBox(
                        width: 220,
                        child: new RangeSlider(
                            values: values,
                            min: min,
                            max: max,
                            labels: labels,
                            onChanged: omitOnChanged ? null : onChanged ?? (_ => { }),
                            onChangeStart: onChangeStart,
                            onChangeEnd: onChangeEnd)))));
    }

    private static RangeSlider.RangeSliderState StateOf(WidgetRenderHarness harness)
    {
        RangeSlider.RangeSliderState? found = null;
        void Visit(Element element)
        {
            if (found is null && element is StatefulElement { State: RangeSlider.RangeSliderState state })
            {
                found = state;
            }

            element.VisitChildren(Visit);
        }

        harness.RootElement.VisitChildren(Visit);
        return Assert.IsType<RangeSlider.RangeSliderState>(found);
    }

    private static double Center(SemanticsNode node) => node.Rect.Left + (node.Rect.Width / 2.0);

    private static IReadOnlyList<SemanticsNode> FindThumbNodes(SemanticsNode root)
    {
        var thumbs = new List<SemanticsNode>();
        void Visit(SemanticsNode node)
        {
            if (node.Flags.HasFlag(SemanticsFlags.IsSlider))
            {
                thumbs.Add(node);
            }

            foreach (SemanticsNode child in node.Children)
            {
                Visit(child);
            }
        }

        Visit(root);
        return thumbs;
    }

    private static object? FindDescendantByTypeName(RenderObject? root, string typeName)
    {
        if (root is null)
        {
            return null;
        }

        if (root.GetType().Name == typeName)
        {
            return root;
        }

        object? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindDescendantByTypeName(child, typeName);
        });

        return result;
    }

    private static T ReadProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);

        object? value = property!.GetValue(target);
        Assert.NotNull(value);
        return (T)value!;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static void DispatchPointerDown(GestureBinding binding, RenderView renderView, int pointer, Point position)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerDownEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow));
    }

    private static void DispatchPointerMove(GestureBinding binding, RenderView renderView, int pointer, Point position)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerMoveEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.Primary,
                down: true,
                timestampUtc: DateTime.UtcNow));
    }

    private static void DispatchPointerUp(GestureBinding binding, RenderView renderView, int pointer, Point position)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerUpEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow));
    }

    private static SemanticsNode? FindFirstSemanticsNode(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindFirstSemanticsNode(child, predicate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private sealed class RecordingRangeTrackShape : RangeSliderTrackShape
    {
        public int PreferredRectCalls { get; private set; }
        public int PaintCalls { get; private set; }

        public override Rect GetPreferredRect(
            RenderBox parentBox,
            Point offset,
            SliderThemeData sliderTheme,
            bool isEnabled = false,
            bool isDiscrete = false)
        {
            PreferredRectCalls++;
            return base.GetPreferredRect(parentBox, offset, sliderTheme, isEnabled, isDiscrete);
        }

        public override void Paint(
            PaintingContext context,
            Point offset,
            Point startThumbCenter,
            Point endThumbCenter,
            Animation<double> enableAnimation,
            bool isDiscrete,
            bool isEnabled,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            TextDirection textDirection)
        {
            PaintCalls++;
        }
    }

    private sealed class RecordingRangeTickMarkShape : RangeSliderTickMarkShape
    {
        public int PaintCalls { get; private set; }

        public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled = false) => new(2.0, 2.0);

        public override void Paint(
            PaintingContext context,
            Point center,
            Point startThumbCenter,
            Point endThumbCenter,
            Animation<double> enableAnimation,
            SliderThemeData sliderTheme,
            TextDirection textDirection)
        {
            PaintCalls++;
        }
    }

    private sealed class RecordingRangeThumbShape : RangeSliderThumbShape
    {
        public int PaintCalls { get; private set; }

        public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => new(20.0, 20.0);

        public override void Paint(
            PaintingContext context,
            Point center,
            Animation<double> activationAnimation,
            Animation<double> enableAnimation,
            bool isDiscrete,
            bool isOnTop,
            bool isPressed,
            SliderThemeData sliderTheme,
            TextDirection textDirection,
            Thumb thumb)
        {
            PaintCalls++;
        }
    }

    private sealed class RecordingRangeValueIndicatorShape : RangeSliderValueIndicatorShape
    {
        public RecordingRangeValueIndicatorShape() : base(32.0, 16.0)
        {
        }

        public int PaintCalls { get; private set; }

        public override void Paint(
            PaintingContext context,
            Point center,
            Animation<double> activationAnimation,
            Animation<double> enableAnimation,
            bool isDiscrete,
            bool isOnTop,
            TextLayout labelLayout,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            TextDirection textDirection,
            Thumb thumb,
            double value,
            double textScaleFactor,
            Size sizeWithOverflow)
        {
            PaintCalls++;
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);

            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public Element RootElement => _rootElement;

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (child is not RenderBox renderBox)
                {
                    throw new InvalidOperationException("HarnessRootElement can host only RenderBox.");
                }

                _renderView.Child = renderBox;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
                if (!Equals(oldSlot, newSlot))
                {
                    throw new InvalidOperationException("HarnessRootElement does not support non-null slot moves.");
                }
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
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

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child is not null)
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
                throw new InvalidOperationException("TestRootElement expects null slot.");
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
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
