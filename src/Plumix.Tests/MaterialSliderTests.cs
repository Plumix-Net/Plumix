using System.Globalization;
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
public sealed class MaterialSliderTests
{
    [Fact]
    public void SliderThemeData_FromPrimaryColorsAndLerp_CoverSourceFields()
    {
        var textStyle = new TextStyle(FontSize: 13.0);
        SliderThemeData source = SliderThemeData.FromPrimaryColors(
            Colors.CadetBlue,
            Colors.DarkSlateBlue,
            Colors.PowderBlue,
            textStyle);

        Assert.Equal(2.0, source.TrackHeight);
        Assert.Equal(ApplyOpacity(Colors.CadetBlue, 0x3d / 255.0), source.InactiveTrackColor);
        Assert.IsType<RoundedRectSliderTrackShape>(source.TrackShape);
        Assert.IsType<PaddleRangeSliderValueIndicatorShape>(source.RangeValueIndicatorShape);
        Assert.Same(textStyle, source.ValueIndicatorTextStyle);

        var firstTrack = new RecordingSliderTrackShape();
        var secondTrack = new RecordingSliderTrackShape();
        RangeThumbSelector firstSelector = (_, _, _, _, _, _) => Thumb.Start;
        RangeThumbSelector secondSelector = (_, _, _, _, _, _) => Thumb.End;
        var first = new SliderThemeData(
            TrackHeight: 2.0,
            TrackShape: firstTrack,
            ThumbSelector: firstSelector,
            Padding: EdgeInsetsGeometry.DirectionalOnly(start: 4.0));
        var second = new SliderThemeData(
            TrackHeight: 6.0,
            TrackShape: secondTrack,
            ThumbSelector: secondSelector,
            Padding: EdgeInsetsGeometry.DirectionalOnly(start: 12.0));

        SliderThemeData beforeMidpoint = SliderThemeData.Lerp(first, second, 0.25);
        SliderThemeData afterMidpoint = SliderThemeData.Lerp(first, second, 0.75);
        Assert.Equal(3.0, beforeMidpoint.TrackHeight);
        Assert.Same(firstTrack, beforeMidpoint.TrackShape);
        Assert.Same(firstSelector, beforeMidpoint.ThumbSelector);
        Assert.Equal(new Thickness(6.0, 0.0, 0.0, 0.0), beforeMidpoint.Padding!.Value.Resolve(TextDirection.Ltr));
        Assert.Same(secondTrack, afterMidpoint.TrackShape);
        Assert.Same(secondSelector, afterMidpoint.ThumbSelector);
    }

    [Fact]
    public void Slider_CustomShapeHierarchy_IsUsedForLayoutAndPaint()
    {
        var track = new RecordingSliderTrackShape();
        var overlay = new RecordingSliderComponentShape(new Size(36.0, 36.0));
        var tick = new RecordingSliderTickMarkShape();
        var thumb = new RecordingSliderComponentShape(new Size(20.0, 20.0));
        var indicator = new RecordingSliderComponentShape(new Size(32.0, 32.0));
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                TrackShape: track,
                OverlayShape: overlay,
                TickMarkShape: tick,
                ThumbShape: thumb,
                ValueIndicatorShape: indicator,
                ShowValueIndicator: ShowValueIndicator.AlwaysVisible),
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220.0,
                    child: new Slider(
                        value: 0.5,
                        divisions: 4,
                        label: "50",
                        onChanged: _ => { }))));

        harness.Pump(new Size(260.0, 120.0));

        Assert.True(track.PreferredRectCalls > 0);
        Assert.True(track.PaintCalls > 0);
        Assert.True(tick.PaintCalls > 0);
        Assert.True(thumb.PaintCalls > 0);
        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        SliderThemeData effectiveTheme = ReadProperty<SliderThemeData>(render!, "SliderTheme");
        Assert.Same(indicator, effectiveTheme.ValueIndicatorShape);
    }

    [Fact]
    public void Slider_RenderObject_IsSizedByParent_SoItsDryLayoutMatchesItsSize()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 220.0,
                    child: new Slider(value: 0.5, onChanged: _ => { }))));
        harness.Pump(new Size(260.0, 120.0));

        var render = (RenderBox)FindDescendantByTypeName(harness.RenderView, "RenderSlider")!;

        // A sized-by-parent box sizes itself in PerformResize from ComputeDryLayout alone, so the
        // dry layout for the constraints it was laid out with has to reproduce its size exactly.
        Assert.Equal(render.Size, render.GetDryLayout(render.Constraints));
    }

    [Fact]
    public void Slider_Adaptive_UsesCupertinoOnlyOnApplePlatforms()
    {
        using var iosHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { Platform = TargetPlatform.IOS },
                child: Slider.Adaptive(value: 0.5, onChanged: _ => { })));
        iosHarness.Pump(new Size(220.0, 80.0));
        Assert.NotNull(FindDescendantByTypeName(iosHarness.RenderView, "RenderCupertinoSlider"));
        Assert.Null(FindDescendantByTypeName(iosHarness.RenderView, "RenderSlider"));

        using var androidHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { Platform = TargetPlatform.Android },
                child: Slider.Adaptive(value: 0.5, onChanged: _ => { })));
        androidHarness.Pump(new Size(220.0, 80.0));
        Assert.NotNull(FindDescendantByTypeName(androidHarness.RenderView, "RenderSlider"));
    }

    [Fact]
    public void Slider_Constructor_Throws_OnInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new Slider(value: 0.5, min: 1, max: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: -0.1, min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: 0.5, min: 0, max: 1, divisions: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: double.NaN, min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: 0.5, min: 0, max: 1, secondaryTrackValue: 1.1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: 0.5, min: 0, max: 1, secondaryTrackValue: double.NaN, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(
            value: 0.5,
            onChanged: _ => { },
            padding: new Thickness(-1, 0, 0, 0)));
    }

    [Fact]
    public void Slider_ExtendedApi_StoresFlutterParityValues()
    {
        var cursor = new SystemMouseCursor("slider");
        var slider = new Slider(
            value: 0.5,
            onChanged: _ => { },
            divisions: 4,
            label: "50",
            mouseCursor: cursor,
            allowedInteraction: SliderInteraction.SlideThumb,
            padding: new Thickness(12, 6),
            showValueIndicator: ShowValueIndicator.AlwaysVisible,
            year2023: false);

        Assert.Equal("50", slider.Label);
        Assert.Same(cursor, slider.MouseCursor);
        Assert.Equal(SliderInteraction.SlideThumb, slider.AllowedInteraction);
        Assert.Equal(new Thickness(12, 6), slider.Padding);
        Assert.Equal(ShowValueIndicator.AlwaysVisible, slider.ShowValueIndicator);
        Assert.False(slider.Year2023);
    }

    [Fact]
    public void Slider_ExtendedThemeTokensReachRenderObject()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                ActiveTickMarkColor: Colors.Gold,
                InactiveTickMarkColor: Colors.DarkSlateBlue,
                TickMarkRadius: 3,
                ValueIndicatorColor: Colors.OrangeRed,
                ShowValueIndicator: ShowValueIndicator.AlwaysVisible,
                Padding: new Thickness(14, 5),
                AllowedInteraction: SliderInteraction.TapOnly,
                TrackGap: 7,
                Year2023: false)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.5,
                        divisions: 4,
                        label: "50",
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));
        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Gold, ReadProperty<Color>(render!, "ActiveTickMarkColor"));
        Assert.Equal(Colors.DarkSlateBlue, ReadProperty<Color>(render, "InactiveTickMarkColor"));
        Assert.Equal(3, ReadProperty<double>(render, "TickMarkRadius"));
        Assert.Equal(Colors.OrangeRed, ReadProperty<Color>(render, "ValueIndicatorColor"));
        Assert.Equal(ShowValueIndicator.AlwaysVisible, ReadProperty<ShowValueIndicator>(render, "ShowValueIndicator"));
        Assert.Equal(new Thickness(14, 5), ReadProperty<Thickness>(render, "Padding"));
        Assert.Equal(SliderInteraction.TapOnly, ReadProperty<SliderInteraction>(render, "AllowedInteraction"));
        Assert.Equal(7, ReadProperty<double>(render, "TrackGap"));
        Assert.Equal(new Size(4, 44), ReadProperty<Size>(render, "ThumbSize"));
        Assert.Equal(16, ReadProperty<double>(render, "TrackHeight"));
        Assert.Equal(theme.ColorScheme.SecondaryContainer, ReadProperty<Color>(render, "InactiveTrackColor"));
    }

    [Fact]
    public void Slider_DefaultM3_UsesPrimaryAndSurfaceContainerHighestColors()
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
                    child: new Slider(
                        value: 0.4,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.PowderBlue, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void Slider_DefaultM2_UsesPrimaryTrackWithOpacityForInactive()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.CadetBlue,
            ColorScheme = ThemeData.Light.ColorScheme with { Primary = Colors.CadetBlue },
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.4,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.CadetBlue, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(ApplyOpacity(Colors.CadetBlue, 0.24), ReadProperty<Color>(render, "InactiveTrackColor"));
    }

    [Fact]
    public void Slider_SecondaryTrack_DefaultAndNormalizationFollowFlutterParity()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            PrimaryColor = Colors.CadetBlue,
            ColorScheme = ThemeData.Light.ColorScheme with { Primary = Colors.CadetBlue },
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 15,
                        min: 0,
                        max: 20,
                        secondaryTrackValue: 18,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(0.9, ReadProperty<double>(render!, "SecondaryTrackValueNormalized"), 3);
        Assert.Equal(ApplyOpacity(Colors.CadetBlue, 0.54), ReadProperty<Color>(render, "SecondaryActiveTrackColor"));
    }

    [Fact]
    public void Slider_SecondaryTrack_ThemeAndWidgetColorsFollowPrecedence()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                SecondaryActiveTrackColor: Colors.OrangeRed)
        };

        using var themeHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.3,
                        secondaryTrackValue: 0.8,
                        onChanged: _ => { }))));

        themeHarness.Pump(new Size(260, 120));
        object? themeRender = FindDescendantByTypeName(themeHarness.RenderView, "RenderSlider");
        Assert.NotNull(themeRender);
        Assert.Equal(Colors.OrangeRed, ReadProperty<Color>(themeRender!, "SecondaryActiveTrackColor"));

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.3,
                        secondaryTrackValue: 0.8,
                        secondaryActiveColor: Colors.MediumPurple,
                        onChanged: _ => { }))));

        widgetHarness.Pump(new Size(260, 120));
        object? widgetRender = FindDescendantByTypeName(widgetHarness.RenderView, "RenderSlider");
        Assert.NotNull(widgetRender);
        Assert.Equal(Colors.MediumPurple, ReadProperty<Color>(widgetRender!, "SecondaryActiveTrackColor"));
    }

    [Fact]
    public void Slider_SecondaryTrack_DisabledUsesDisabledThemeColor()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                DisabledSecondaryActiveTrackColor: Colors.Gainsboro)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.4,
                        secondaryTrackValue: 0.9,
                        onChanged: null))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Gainsboro, ReadProperty<Color>(render!, "SecondaryActiveTrackColor"));
    }

    [Fact]
    public void Slider_ThemeColors_Apply_WhenWidgetColorsAreMissing()
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
                    child: new Slider(
                        value: 0.3,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkGreen, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.LightGreen, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Gold, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void Slider_WidgetColors_OverrideThemeColors()
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
                    child: new Slider(
                        value: 0.3,
                        activeColor: Colors.DarkRed,
                        inactiveColor: Colors.MistyRose,
                        thumbColor: Colors.DarkMagenta,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.MistyRose, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.DarkMagenta, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void Slider_Drag_InvokesOnChangeStartOnChangedAndOnChangeEnd_WithDiscreteSnapping()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            double? start = null;
            double? end = null;
            double changed = 0;

            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220,
                            child: new Slider(
                                value: 0.2,
                                divisions: 5,
                                onChangeStart: value => start = value,
                                onChanged: value => changed = value,
                                onChangeEnd: value => end = value)))));

            harness.Pump(new Size(280, 120));

            DispatchPointerDown(binding, harness.RenderView, pointer: 700, position: new Point(20, 24));
            DispatchPointerMove(binding, harness.RenderView, pointer: 700, position: new Point(214, 24));
            DispatchPointerUp(binding, harness.RenderView, pointer: 700, position: new Point(214, 24));
            harness.Pump(new Size(280, 120));

            Assert.NotNull(start);
            Assert.NotNull(end);
            Assert.Equal(0.2, start!.Value, 3);
            Assert.Equal(1.0, end!.Value, 3);
            Assert.Equal(1.0, changed, 3);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Slider_TapOnly_UpdatesOnDownAndIgnoresPointerMove()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            var changed = new List<double>();
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220,
                            child: new Slider(
                                value: 0.2,
                                allowedInteraction: SliderInteraction.TapOnly,
                                onChanged: changed.Add)))));

            harness.Pump(new Size(280, 120));
            DispatchPointerDown(binding, harness.RenderView, pointer: 709, position: new Point(110, 24));
            int countAfterDown = changed.Count;
            DispatchPointerMove(binding, harness.RenderView, pointer: 709, position: new Point(210, 24));
            DispatchPointerUp(binding, harness.RenderView, pointer: 709, position: new Point(210, 24));

            Assert.True(countAfterDown > 0);
            Assert.Equal(countAfterDown, changed.Count);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Slider_KeyboardArrowRight_IncrementsValueInLtr()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var focusNode = new FocusNode();
            double next = 0.0;

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.MacOS),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(
                            value: 0.40,
                            focusNode: focusNode,
                            onChanged: value => next = value))));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            focusNode.RequestFocus();
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(0.5, next, 3);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_KeyboardArrowLeft_InRtl_IncrementsValue()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var focusNode = new FocusNode();
            double next = 0.0;

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.MacOS),
                    child: new Directionality(
                        textDirection: TextDirection.Rtl,
                        child: new SizedBox(
                            width: 220,
                            child: new Slider(
                                value: 0.40,
                                focusNode: focusNode,
                                onChanged: value => next = value)))));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            focusNode.RequestFocus();
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(0.5, next, 3);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_Semantics_MatchFlutterEnabledAndDisabledNodes()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            SemanticsNode Build(Action<double>? onChanged)
            {
                using var harness = new WidgetRenderHarness(
                    new Theme(
                        data: new ThemeData(platform: TargetPlatform.Android),
                        child: new SizedBox(width: 220, child: new Slider(value: 0.5, onChanged: onChanged))));

                SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
                return Assert.IsType<SemanticsNode>(
                    FindFirstSemanticsNode(root, static node => node.Flags.HasFlag(SemanticsFlags.IsSlider)));
            }

            SemanticsNode enabled = Build(_ => { });
            Assert.True(enabled.Flags.HasFlag(SemanticsFlags.IsSlider));
            Assert.True(enabled.Flags.HasFlag(SemanticsFlags.IsEnabled));
            Assert.True(enabled.Flags.HasFlag(SemanticsFlags.HasEnabledState));
            Assert.True(enabled.Flags.HasFlag(SemanticsFlags.IsFocusable));
            Assert.False(enabled.Flags.HasFlag(SemanticsFlags.IsFocused));
            Assert.Equal(
                SemanticsActions.Increase | SemanticsActions.Decrease | SemanticsActions.Focus,
                enabled.Actions);
            Assert.Equal("50%", enabled.Value);
            Assert.Equal("55%", enabled.IncreasedValue);
            Assert.Equal("45%", enabled.DecreasedValue);
            Assert.Equal(TextDirection.Ltr, enabled.TextDirection);

            // Flutter keeps the three value strings on a disabled slider and drops every action.
            SemanticsNode disabled = Build(null);
            Assert.True(disabled.Flags.HasFlag(SemanticsFlags.IsSlider));
            Assert.True(disabled.Flags.HasFlag(SemanticsFlags.HasEnabledState));
            Assert.True(disabled.Flags.HasFlag(SemanticsFlags.IsFocusable));
            Assert.False(disabled.Flags.HasFlag(SemanticsFlags.IsEnabled));
            Assert.Equal(SemanticsActions.None, disabled.Actions);
            Assert.Equal("50%", disabled.Value);
            Assert.Equal("55%", disabled.IncreasedValue);
            Assert.Equal("45%", disabled.DecreasedValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    /// <remarks>
    /// Flutter's <c>slider_test.dart</c> "Slider gains keyboard focus when it gains semantics focus
    /// on Windows": the Windows-only <c>didGainAccessibilityFocus</c> handler is declared, and
    /// performing the action pulls keyboard focus onto the slider.
    /// </remarks>
    [Fact]
    public void Slider_DidGainAccessibilityFocus_IsWindowsOnlyAndTakesKeyboardFocus()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var focusNode = new FocusNode();
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.Windows),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(value: 0.5, focusNode: focusNode, onChanged: _ => { }))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            Assert.Equal(
                SemanticsActions.Increase
                | SemanticsActions.Decrease
                | SemanticsActions.Focus
                | SemanticsActions.DidGainAccessibilityFocus,
                node.Actions);

            Assert.False(focusNode.HasFocus);
            Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.DidGainAccessibilityFocus));
            harness.PumpAndGetSemantics(new Size(260, 120));
            Assert.True(focusNode.HasFocus);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    /// <remarks>Dart registers no accessibility-focus handler on any non-Windows platform.</remarks>
    [Fact]
    public void Slider_DidGainAccessibilityFocus_IsAbsentOffWindows()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.MacOS),
                    child: new SizedBox(width: 220, child: new Slider(value: 0.5, onChanged: _ => { }))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            Assert.False(node.Actions.HasFlag(SemanticsActions.DidGainAccessibilityFocus));
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_Semantics_StepByTenPercentOnApplePlatforms()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.MacOS),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(value: 100.0, max: 200.0, onChanged: _ => { }))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            Assert.Equal("50%", node.Value);
            Assert.Equal("60%", node.IncreasedValue);
            Assert.Equal("40%", node.DecreasedValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_SemanticFormatterCallback_FormatsValueIncreasedAndDecreased()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.Android),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(
                            value: 40.0,
                            max: 200.0,
                            divisions: 10,
                            onChanged: _ => { },
                            semanticFormatterCallback: value => Math.Round(value, MidpointRounding.AwayFromZero)
                                .ToString("0", CultureInfo.InvariantCulture)))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            // One division of 0..200 is 20, so the formatter sees 40, 60 and 20.
            Assert.Equal("40", node.Value);
            Assert.Equal("60", node.IncreasedValue);
            Assert.Equal("20", node.DecreasedValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_Label_DoesNotOverwriteTheSemanticValue()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.Android),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(
                            value: 40.0,
                            max: 200.0,
                            divisions: 10,
                            label: "Bingo",
                            onChanged: _ => { },
                            semanticFormatterCallback: value => Math.Round(value, MidpointRounding.AwayFromZero)
                                .ToString("0", CultureInfo.InvariantCulture)))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            Assert.Equal("Bingo", node.Label);
            Assert.Equal("40", node.Value);
            Assert.Equal("60", node.IncreasedValue);
            Assert.Equal("20", node.DecreasedValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_SemanticIncreaseAndDecrease_FireStartChangedAndEndInOrder()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var calls = new List<string>();
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.Android),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(
                            value: 0.5,
                            onChanged: value => calls.Add(FormattableString.Invariant($"changed {value:0.00}")),
                            onChangeStart: value => calls.Add(FormattableString.Invariant($"start {value:0.00}")),
                            onChangeEnd: value => calls.Add(FormattableString.Invariant($"end {value:0.00}"))))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            Assert.True(node.PerformAction(SemanticsActions.Increase));
            Assert.Equal(["start 0.50", "changed 0.55", "end 0.55"], calls);

            calls.Clear();
            Assert.True(node.PerformAction(SemanticsActions.Decrease));
            Assert.Equal(["start 0.50", "changed 0.45", "end 0.45"], calls);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_SemanticFocusAction_RequestsKeyboardFocus()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var focusNode = new FocusNode();
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.Android),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(value: 0.5, focusNode: focusNode, onChanged: _ => { }))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            Assert.False(focusNode.HasFocus);
            Assert.True(node.PerformAction(SemanticsActions.Focus));
            Scheduler.FlushMicrotasks();
            Assert.True(focusNode.HasFocus);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_SemanticsNode_IsAFortyEightSquareCenteredOnTheThumb()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.Android),
                    child: new SizedBox(width: 220, child: new Slider(value: 0.5, onChanged: _ => { }))));

            SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(new Size(260, 120)));
            SemanticsNode node = Assert.IsType<SemanticsNode>(
                FindFirstSemanticsNode(root, static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsSlider)));

            Assert.Equal(48.0, node.Rect.Width, 3);
            Assert.Equal(48.0, node.Rect.Height, 3);

            // The slider is 220 wide, so a mid-value thumb sits on the track's horizontal centre.
            Assert.Equal(110.0, node.Rect.Left + (node.Rect.Width / 2.0), 0);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
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
        byte alpha = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
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

    private sealed class RecordingSliderTrackShape : SliderTrackShape
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
            Point thumbCenter,
            Point? secondaryOffset,
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

    private sealed class RecordingSliderTickMarkShape : SliderTickMarkShape
    {
        public int PaintCalls { get; private set; }

        public override Size GetPreferredSize(SliderThemeData sliderTheme, bool isEnabled) => new(2.0, 2.0);

        public override void Paint(
            PaintingContext context,
            Point center,
            Point thumbCenter,
            Animation<double> enableAnimation,
            SliderThemeData sliderTheme,
            TextDirection textDirection)
        {
            PaintCalls++;
        }
    }

    private sealed class RecordingSliderComponentShape : SliderComponentShape
    {
        private readonly Size _preferredSize;

        public RecordingSliderComponentShape(Size preferredSize)
        {
            _preferredSize = preferredSize;
        }

        public int PaintCalls { get; private set; }

        public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => _preferredSize;

        public override void Paint(
            PaintingContext context,
            Point center,
            Animation<double> activationAnimation,
            Animation<double> enableAnimation,
            bool isDiscrete,
            TextLayout? labelLayout,
            RenderBox parentBox,
            SliderThemeData sliderTheme,
            TextDirection textDirection,
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
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public bool PerformSemanticsAction(int nodeId, SemanticsActions action)
        {
            return _pipeline.SemanticsOwner!.PerformAction(nodeId, action);
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

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            public override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
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

            public override void Unmount()
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

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
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
