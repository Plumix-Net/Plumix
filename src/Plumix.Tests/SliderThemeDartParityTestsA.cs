// Dart parity source: material_ui/lib/src/slider_theme.dart
// Mirrors material-ui-src/test/slider_theme_test.dart (lines 1-1768)

using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SliderThemeDartParityTestsA : IDisposable
{
    private readonly bool _previousDisableShadows;

    public SliderThemeDartParityTestsA()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test: defaultTargetPlatform == android, debugDisableShadows == true.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        _previousDisableShadows = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true;
    }

    public void Dispose()
    {
        RenderingDebug.DisableShadows = _previousDisableShadows;
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    // Flutter: "SliderThemeData copyWith, ==, hashCode basics"
    [Fact]
    public void SliderThemeDataCopyWithEqualsHashCodeBasics()
    {
        Assert.Equal(new SliderThemeData(), new SliderThemeData().CopyWith());
        Assert.Equal(new SliderThemeData().GetHashCode(), new SliderThemeData().CopyWith().GetHashCode());
    }

    // Flutter: "SliderThemeData lerp special cases"
    [Fact]
    public void SliderThemeDataLerpSpecialCases()
    {
        var data = new SliderThemeData();
        Assert.Same(data, SliderThemeData.Lerp(data, data, 0.5));
    }

    // Flutter: "Default SliderThemeData debugFillProperties"
    [Fact]
    public void DefaultSliderThemeDataDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new SliderThemeData().DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();

        Assert.Empty(description);
    }

    // Flutter: "SliderThemeData implements debugFillProperties"
    [Fact]
    public void SliderThemeDataImplementsDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new SliderThemeData(
            TrackHeight: 7.0,
            ActiveTrackColor: new Color(0xFF000001),
            InactiveTrackColor: new Color(0xFF000002),
            SecondaryActiveTrackColor: new Color(0xFF000003),
            DisabledActiveTrackColor: new Color(0xFF000004),
            DisabledInactiveTrackColor: new Color(0xFF000005),
            DisabledSecondaryActiveTrackColor: new Color(0xFF000006),
            ActiveTickMarkColor: new Color(0xFF000007),
            InactiveTickMarkColor: new Color(0xFF000008),
            DisabledActiveTickMarkColor: new Color(0xFF000009),
            DisabledInactiveTickMarkColor: new Color(0xFF000010),
            ThumbColor: new Color(0xFF000011),
            OverlappingShapeStrokeColor: new Color(0xFF000012),
            DisabledThumbColor: new Color(0xFF000013),
            OverlayColor: new Color(0xFF000014),
            ValueIndicatorColor: new Color(0xFF000015),
            ValueIndicatorStrokeColor: new Color(0xFF000015),
            OverlayShape: new RoundSliderOverlayShape(),
            TickMarkShape: new RoundSliderTickMarkShape(),
            ThumbShape: new RoundSliderThumbShape(),
            TrackShape: new RoundedRectSliderTrackShape(),
            ValueIndicatorShape: new PaddleSliderValueIndicatorShape(),
            RangeTickMarkShape: new RoundRangeSliderTickMarkShape(),
            RangeThumbShape: new RoundRangeSliderThumbShape(),
            RangeTrackShape: new RoundedRectRangeSliderTrackShape(),
            RangeValueIndicatorShape: new PaddleRangeSliderValueIndicatorShape(),
#pragma warning disable CS0618 // Dart's test uses the deprecated ShowValueIndicator.always.
            ShowValueIndicator: ShowValueIndicator.Always,
#pragma warning restore CS0618
            ValueIndicatorTextStyle: new TextStyle(Color: MaterialColors.Black),
            MouseCursor: new ClickableMouseCursorProperty(),
            AllowedInteraction: SliderInteraction.TapOnly,
            Padding: EdgeInsetsGeometry.All(1.0),
            ThumbSize: new WidgetStatePropertyAll<Size?>(new Size(20, 20)),
            TrackGap: 10.0,
            Year2023: false).DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();

        Assert.Equal(
            new[]
            {
                "trackHeight: 7.0",
                $"activeTrackColor: {new Color(0xff000001)}",
                $"inactiveTrackColor: {new Color(0xff000002)}",
                $"secondaryActiveTrackColor: {new Color(0xff000003)}",
                $"disabledActiveTrackColor: {new Color(0xff000004)}",
                $"disabledInactiveTrackColor: {new Color(0xff000005)}",
                $"disabledSecondaryActiveTrackColor: {new Color(0xff000006)}",
                $"activeTickMarkColor: {new Color(0xff000007)}",
                $"inactiveTickMarkColor: {new Color(0xff000008)}",
                $"disabledActiveTickMarkColor: {new Color(0xff000009)}",
                $"disabledInactiveTickMarkColor: {new Color(0xff000010)}",
                $"thumbColor: {new Color(0xff000011)}",
                $"overlappingShapeStrokeColor: {new Color(0xff000012)}",
                $"disabledThumbColor: {new Color(0xff000013)}",
                $"overlayColor: {new Color(0xff000014)}",
                $"valueIndicatorColor: {new Color(0xff000015)}",
                $"valueIndicatorStrokeColor: {new Color(0xff000015)}",
                "overlayShape: Instance of 'RoundSliderOverlayShape'",
                "tickMarkShape: Instance of 'RoundSliderTickMarkShape'",
                "thumbShape: Instance of 'RoundSliderThumbShape'",
                "trackShape: Instance of 'RoundedRectSliderTrackShape'",
                "valueIndicatorShape: Instance of 'PaddleSliderValueIndicatorShape'",
                "rangeTickMarkShape: Instance of 'RoundRangeSliderTickMarkShape'",
                "rangeThumbShape: Instance of 'RoundRangeSliderThumbShape'",
                "rangeTrackShape: Instance of 'RoundedRectRangeSliderTrackShape'",
                "rangeValueIndicatorShape: Instance of 'PaddleRangeSliderValueIndicatorShape'",
                "showValueIndicator: always",
                $"valueIndicatorTextStyle: TextStyle(inherit: true, color: {new Color(0xff000000)})",
                "mouseCursor: WidgetStateMouseCursor(clickable)",
                "allowedInteraction: tapOnly",
                "padding: EdgeInsets.all(1.0)",
                "thumbSize: WidgetStatePropertyAll(Size(20.0, 20.0))",
                "trackGap: 10.0",
                "year2023: false",
            },
            description);
    }

    // Flutter: "Slider defaults"
    [Fact]
    public void SliderDefaults()
    {
        RenderingDebug.DisableShadows = false;
        var theme = new ThemeData();
        ColorScheme colorScheme = theme.ColorScheme;
        const double trackHeight = 4.0;
        var activeTrackColor = new Color(colorScheme.Primary.Value);
        Color inactiveTrackColor = colorScheme.SurfaceContainerHighest;
        Color secondaryActiveTrackColor = colorScheme.Primary.WithOpacity(0.54);
        Color disabledActiveTrackColor = colorScheme.OnSurface.WithOpacity(0.38);
        Color disabledInactiveTrackColor = colorScheme.OnSurface.WithOpacity(0.12);
        Color disabledSecondaryActiveTrackColor = colorScheme.OnSurface.WithOpacity(0.12);
        Color shadowColor = colorScheme.Shadow;
        var thumbColor = new Color(colorScheme.Primary.Value);
        Color disabledThumbColor = Color.AlphaBlend(
            colorScheme.OnSurface.WithOpacity(0.38),
            colorScheme.Surface);
        Color activeTickMarkColor = colorScheme.OnPrimary.WithOpacity(0.38);
        Color inactiveTickMarkColor = colorScheme.OnSurfaceVariant.WithOpacity(0.38);
        Color disabledActiveTickMarkColor = colorScheme.OnSurface.WithOpacity(0.38);
        Color disabledInactiveTickMarkColor = colorScheme.OnSurface.WithOpacity(0.38);

        try
        {
            double value = 0.45;
            Widget BuildApp(int? divisions = null, bool enabled = true)
            {
                Action<double>? onChanged = !enabled ? null : d => value = d;
                return new MaterialApp(
                    home: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new MaterialWidget(
                            child: new Center(
                                child: new Theme(
                                    data: theme,
                                    child: new Slider(
                                        value: value,
                                        secondaryTrackValue: 0.75,
                                        label: DartDouble(value),
                                        divisions: divisions,
                                        onChanged: onChanged))))));
            }

            using var tester = new FrameworkDartTester();
            tester.PumpWidget(BuildApp());

            RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

            // Test default track height.
            Radius radius = Radius.Circular(trackHeight / 2);
            Radius activatedRadius = Radius.Circular((trackHeight + 2) / 2);
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    // Inactive track.
                    .RRect(
                        rrect: RRect.FromLTRBR(360.4, 298.0, 776.0, 302.0, radius),
                        color: inactiveTrackColor)
                    // Active track.
                    .RRect(
                        rrect: RRect.FromLTRBR(24.0, 297.0, 364.4, 303.0, activatedRadius),
                        color: activeTrackColor));

            // Test default colors for enabled slider.
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: inactiveTrackColor)
                    .RRect(color: activeTrackColor)
                    .RRect(color: secondaryActiveTrackColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Shadow(color: shadowColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: thumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.RRect(color: disabledSecondaryActiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: activeTickMarkColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: inactiveTickMarkColor));

            // Test defaults colors for discrete slider.
            tester.PumpWidget(BuildApp(divisions: 3));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: inactiveTrackColor)
                    .RRect(color: activeTrackColor)
                    .RRect(color: secondaryActiveTrackColor));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .Circle(color: activeTickMarkColor)
                    .Circle(color: activeTickMarkColor)
                    .Circle(color: inactiveTickMarkColor)
                    .Circle(color: inactiveTickMarkColor)
                    .Shadow(color: MaterialColors.Black)
                    .Circle(color: thumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledActiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: disabledInactiveTrackColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.RRect(color: disabledSecondaryActiveTrackColor));

            // Test defaults colors for disabled slider.
            tester.PumpWidget(BuildApp(enabled: false));
            tester.PumpAndSettle();
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: disabledInactiveTrackColor)
                    .RRect(color: disabledActiveTrackColor)
                    .RRect(color: disabledSecondaryActiveTrackColor));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .Shadow(color: shadowColor)
                    .Circle(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: thumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: secondaryActiveTrackColor));

            // Test defaults colors for disabled discrete slider.
            tester.PumpWidget(BuildApp(divisions: 3, enabled: false));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .Circle(color: disabledActiveTickMarkColor)
                    .Circle(color: disabledActiveTickMarkColor)
                    .Circle(color: disabledInactiveTickMarkColor)
                    .Circle(color: disabledInactiveTickMarkColor)
                    .Shadow(color: shadowColor)
                    .Circle(color: disabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: thumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: activeTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: inactiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: secondaryActiveTrackColor));
        }
        finally
        {
            RenderingDebug.DisableShadows = true;
        }
    }

    // Flutter: "Slider uses the right theme colors for the right components"
    // Every expectation up to the last one passes; the last one expects the overlay to start with the
    // M2 canvas Material's `drawRRect(0xfffafafa)`.
    [Fact]
    public void SliderUsesTheRightThemeColorsForTheRightComponents()
    {
        RenderingDebug.DisableShadows = false;
        try
        {
            var customColor1 = new Color(0xcafefeed);
            var customColor2 = new Color(0xdeadbeef);
            var customColor3 = new Color(0xdecaface);
            var theme = new ThemeData(
                useMaterial3: false,
                platform: TargetPlatform.Android,
                primarySwatch: MaterialColors.Blue,
                sliderTheme: new SliderThemeData(
                    DisabledThumbColor: new Color(0xff000001),
                    DisabledActiveTickMarkColor: new Color(0xff000002),
                    DisabledActiveTrackColor: new Color(0xff000003),
                    DisabledInactiveTickMarkColor: new Color(0xff000004),
                    DisabledInactiveTrackColor: new Color(0xff000005),
                    ActiveTrackColor: new Color(0xff000006),
                    ActiveTickMarkColor: new Color(0xff000007),
                    InactiveTrackColor: new Color(0xff000008),
                    InactiveTickMarkColor: new Color(0xff000009),
                    OverlayColor: new Color(0xff000010),
                    ThumbColor: new Color(0xff000011),
                    ValueIndicatorColor: new Color(0xff000012),
                    DisabledSecondaryActiveTrackColor: new Color(0xff000013),
                    SecondaryActiveTrackColor: new Color(0xff000014)));
            SliderThemeData sliderTheme = theme.SliderTheme;
            double value = 0.45;
            Widget BuildApp(
                Color? activeColor = null,
                Color? inactiveColor = null,
                Color? secondaryActiveColor = null,
                int? divisions = null,
                bool enabled = true)
            {
                Action<double>? onChanged = !enabled ? null : d => value = d;
                return new MaterialApp(
                    theme: theme,
                    home: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new MaterialWidget(
                            child: new Center(
                                child: new Theme(
                                    data: theme,
                                    child: new Slider(
                                        value: value,
                                        secondaryTrackValue: 0.75,
                                        label: DartDouble(value),
                                        divisions: divisions,
                                        activeColor: activeColor,
                                        inactiveColor: inactiveColor,
                                        secondaryActiveColor: secondaryActiveColor,
                                        onChanged: onChanged))))));
            }

            using var tester = new FrameworkDartTester();
            tester.PumpWidget(BuildApp());

            RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
            RenderObject valueIndicatorBox = tester.ElementOfType<Overlay>().FindRenderObject()!;

            // Check default theme for enabled widget.
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: sliderTheme.InactiveTrackColor)
                    .RRect(color: sliderTheme.ActiveTrackColor)
                    .RRect(color: sliderTheme.SecondaryActiveTrackColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Shadow(color: new Color(0xff000000)));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            AssertNoDisabledColors(material, sliderTheme);
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ActiveTickMarkColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.Circle(color: sliderTheme.InactiveTickMarkColor));

            // Test setting only the activeColor.
            tester.PumpWidget(BuildApp(activeColor: customColor1));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: sliderTheme.InactiveTrackColor)
                    .RRect(color: customColor1)
                    .RRect(color: sliderTheme.SecondaryActiveTrackColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Shadow(color: MaterialColors.Black));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: customColor1));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            AssertNoDisabledColors(material, sliderTheme);

            // Test setting only the inactiveColor.
            tester.PumpWidget(BuildApp(inactiveColor: customColor1));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: customColor1)
                    .RRect(color: sliderTheme.ActiveTrackColor)
                    .RRect(color: sliderTheme.SecondaryActiveTrackColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Shadow(color: MaterialColors.Black));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            AssertNoDisabledColors(material, sliderTheme);

            // Test setting only the secondaryActiveColor.
            tester.PumpWidget(BuildApp(secondaryActiveColor: customColor1));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: sliderTheme.InactiveTrackColor)
                    .RRect(color: sliderTheme.ActiveTrackColor)
                    .RRect(color: customColor1));
            PaintAssert.Paints(material, PaintPattern.Paints.Shadow(color: MaterialColors.Black));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            AssertNoDisabledColors(material, sliderTheme);

            // Test setting both activeColor, inactiveColor, and secondaryActiveColor.
            tester.PumpWidget(BuildApp(
                activeColor: customColor1,
                inactiveColor: customColor2,
                secondaryActiveColor: customColor3));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: customColor2)
                    .RRect(color: customColor1)
                    .RRect(color: customColor3));
            PaintAssert.Paints(material, PaintPattern.Paints.Shadow(color: MaterialColors.Black));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: customColor1));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            AssertNoDisabledColors(material, sliderTheme);

            // Test colors for discrete slider.
            tester.PumpWidget(BuildApp(divisions: 3));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: sliderTheme.InactiveTrackColor)
                    .RRect(color: sliderTheme.ActiveTrackColor)
                    .RRect(color: sliderTheme.SecondaryActiveTrackColor));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .Circle(color: sliderTheme.ActiveTickMarkColor)
                    .Circle(color: sliderTheme.ActiveTickMarkColor)
                    .Circle(color: sliderTheme.InactiveTickMarkColor)
                    .Circle(color: sliderTheme.InactiveTickMarkColor)
                    .Shadow(color: MaterialColors.Black)
                    .Circle(color: sliderTheme.ThumbColor));
            AssertNoDisabledColors(material, sliderTheme);

            // Test colors for discrete slider with inactiveColor and activeColor set.
            tester.PumpWidget(BuildApp(
                activeColor: customColor1,
                inactiveColor: customColor2,
                secondaryActiveColor: customColor3,
                divisions: 3));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: customColor2)
                    .RRect(color: customColor1)
                    .RRect(color: customColor3));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .Circle(color: customColor2)
                    .Circle(color: customColor2)
                    .Circle(color: customColor1)
                    .Circle(color: customColor1)
                    .Shadow(color: MaterialColors.Black)
                    .Circle(color: customColor1));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            AssertNoDisabledColors(material, sliderTheme);
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ActiveTickMarkColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.Circle(color: sliderTheme.InactiveTickMarkColor));

            // Test default theme for disabled widget.
            tester.PumpWidget(BuildApp(enabled: false));
            tester.PumpAndSettle();
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: sliderTheme.DisabledInactiveTrackColor)
                    .RRect(color: sliderTheme.DisabledActiveTrackColor)
                    .RRect(color: sliderTheme.DisabledSecondaryActiveTrackColor));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .Shadow(color: MaterialColors.Black)
                    .Circle(color: sliderTheme.DisabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            // These 2 colors are too close to distinguish.
            // expect(material, isNot(paints..rrect(color: sliderTheme.activeTrackColor)));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: sliderTheme.InactiveTrackColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.RRect(color: sliderTheme.SecondaryActiveTrackColor));

            // Test default theme for disabled discrete widget.
            tester.PumpWidget(BuildApp(divisions: 3, enabled: false));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .Circle(color: sliderTheme.DisabledActiveTickMarkColor)
                    .Circle(color: sliderTheme.DisabledActiveTickMarkColor)
                    .Circle(color: sliderTheme.DisabledInactiveTickMarkColor)
                    .Circle(color: sliderTheme.DisabledInactiveTickMarkColor)
                    .Shadow(color: MaterialColors.Black)
                    .Circle(color: sliderTheme.DisabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            // These 2 colors are too close to distinguish.
            // expect(material, isNot(paints..rrect(color: sliderTheme.activeTrackColor)));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: sliderTheme.InactiveTrackColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.RRect(color: sliderTheme.SecondaryActiveTrackColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ActiveTickMarkColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.Circle(color: sliderTheme.InactiveTickMarkColor));

            // Test setting the activeColor, inactiveColor and secondaryActiveColor for disabled widget.
            tester.PumpWidget(BuildApp(
                activeColor: customColor1,
                inactiveColor: customColor2,
                secondaryActiveColor: customColor3,
                enabled: false));
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: sliderTheme.DisabledInactiveTrackColor)
                    .RRect(color: sliderTheme.DisabledActiveTrackColor)
                    .RRect(color: sliderTheme.DisabledSecondaryActiveTrackColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
            // These colors are too close to distinguish.
            // expect(material, isNot(paints..rrect(color: sliderTheme.activeTrackColor)));
            PaintAssert.DoesNotPaint(material, PaintPattern.Paints.RRect(color: sliderTheme.InactiveTrackColor));
            PaintAssert.DoesNotPaint(
                material,
                PaintPattern.Paints.RRect(color: sliderTheme.SecondaryActiveTrackColor));

            // Test that the default value indicator has the right colors.
            tester.PumpWidget(BuildApp(divisions: 3));
            Point center = tester.GetCenter(tester.ElementOfType<Slider>());
            TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            // Wait for value indicator animation to finish.
            tester.PumpAndSettle();
            Assert.Equal(2.0 / 3.0, value);
            PaintAssert.Paints(
                valueIndicatorBox,
                PaintPattern.Paints
                    .Path(color: sliderTheme.ValueIndicatorColor)
                    .Paragraph());
            gesture.Up();
            // Wait for value indicator animation to finish.
            tester.PumpAndSettle();

            // Testing the custom colors are used for the indicator.
            tester.PumpWidget(BuildApp(divisions: 3, activeColor: customColor1, inactiveColor: customColor2));
            center = tester.GetCenter(tester.ElementOfType<Slider>());
            gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
            // Wait for value indicator animation to finish.
            tester.PumpAndSettle();
            Assert.Equal(2.0 / 3.0, value);
            PaintAssert.Paints(
                valueIndicatorBox,
                PaintPattern.Paints
                    .RRect(color: new Color(0xfffafafa))
                    .RRect(color: customColor2) // Inactive track
                    .RRect(color: customColor1) // Active track
                    .Circle(color: customColor1.WithOpacity(0.12)) // overlay
                    .Circle(color: customColor2) // 1st tick mark
                    .Circle(color: customColor2) // 2nd tick mark
                    .Circle(color: customColor2) // 3rd tick mark
                    .Circle(color: customColor1) // 4th tick mark
                    .Shadow(color: MaterialColors.Black)
                    .Circle(color: customColor1) // thumb
                    .Path(color: sliderTheme.ValueIndicatorColor)); // indicator
            gesture.Up();
        }
        finally
        {
            RenderingDebug.DisableShadows = true;
        }
    }

    // Flutter: "Slider parameters overrides theme properties"
    [Fact]
    public void SliderParametersOverridesThemeProperties()
    {
        RenderingDebug.DisableShadows = false;
        var activeTrackColor = new Color(0xffff0001);
        var inactiveTrackColor = new Color(0xffff0002);
        var secondaryActiveTrackColor = new Color(0xffff0003);
        var thumbColor = new Color(0xffff0004);

        var theme = new ThemeData(
            platform: TargetPlatform.Android,
            primarySwatch: MaterialColors.Blue,
            sliderTheme: new SliderThemeData(
                ActiveTrackColor: new Color(0xff000001),
                InactiveTickMarkColor: new Color(0xff000002),
                SecondaryActiveTrackColor: new Color(0xff000003),
                ThumbColor: new Color(0xff000004)));
        try
        {
            const double value = 0.45;
            Widget BuildApp(bool enabled = true)
            {
                return new MaterialApp(
                    theme: theme,
                    home: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new MaterialWidget(
                            child: new Center(
                                child: new Slider(
                                    activeColor: activeTrackColor,
                                    inactiveColor: inactiveTrackColor,
                                    secondaryActiveColor: secondaryActiveTrackColor,
                                    thumbColor: thumbColor,
                                    value: value,
                                    secondaryTrackValue: 0.75,
                                    label: DartDouble(value),
                                    onChanged: _ => { })))));
            }

            using var tester = new FrameworkDartTester();
            tester.PumpWidget(BuildApp());

            RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

            // Test Slider parameters.
            PaintAssert.Paints(
                material,
                PaintPattern.Paints
                    .RRect(color: inactiveTrackColor)
                    .RRect(color: activeTrackColor)
                    .RRect(color: secondaryActiveTrackColor));
            PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: thumbColor));
        }
        finally
        {
            RenderingDebug.DisableShadows = true;
        }
    }

    // Flutter: "Slider uses ThemeData slider theme if present"
    [Fact]
    public void SliderUsesThemeDataSliderThemeIfPresent()
    {
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Red);
        SliderThemeData sliderTheme = theme.SliderTheme;
        SliderThemeData customTheme = sliderTheme.CopyWith(
            activeTrackColor: MaterialColors.Purple,
            inactiveTrackColor: MaterialColors.Purple.WithAlpha(0x3d),
            secondaryActiveTrackColor: MaterialColors.Purple.WithAlpha(0x8a));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.5, secondaryTrackValue: 0.75, enabled: false));
        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: customTheme.DisabledActiveTrackColor)
                .RRect(color: customTheme.DisabledInactiveTrackColor)
                .RRect(color: customTheme.DisabledSecondaryActiveTrackColor));
    }

    // Flutter: "Slider overrides ThemeData theme if SliderTheme present"
    [Fact]
    public void SliderOverridesThemeDataThemeIfSliderThemePresent()
    {
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Red);
        SliderThemeData sliderTheme = theme.SliderTheme;
        SliderThemeData customTheme = sliderTheme.CopyWith(
            activeTrackColor: MaterialColors.Purple,
            inactiveTrackColor: MaterialColors.Purple.WithAlpha(0x3d),
            secondaryActiveTrackColor: MaterialColors.Purple.WithAlpha(0x8a));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.5, secondaryTrackValue: 0.75, enabled: false));
        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .RRect(color: customTheme.DisabledActiveTrackColor)
                .RRect(color: customTheme.DisabledInactiveTrackColor)
                .RRect(color: customTheme.DisabledSecondaryActiveTrackColor));
    }

    // Flutter: "SliderThemeData generates correct opacities for fromPrimaryColors"
    [Fact]
    public void SliderThemeDataGeneratesCorrectOpacitiesForFromPrimaryColors()
    {
        var customColor1 = new Color(0xcafefeed);
        var customColor2 = new Color(0xdeadbeef);
        var customColor3 = new Color(0xdecaface);
        var customColor4 = new Color(0xfeedcafe);

        SliderThemeData sliderTheme = SliderThemeData.FromPrimaryColors(
            primaryColor: customColor1,
            primaryColorDark: customColor2,
            primaryColorLight: customColor3,
            valueIndicatorTextStyle: ThemeData.Fallback.TextTheme.BodyLarge!.CopyWith(color: customColor4));

        Assert.Equal(customColor1.WithAlpha(0xff), sliderTheme.ActiveTrackColor);
        Assert.Equal(customColor1.WithAlpha(0x3d), sliderTheme.InactiveTrackColor);
        Assert.Equal(customColor1.WithAlpha(0x8a), sliderTheme.SecondaryActiveTrackColor);
        Assert.Equal(customColor2.WithAlpha(0x52), sliderTheme.DisabledActiveTrackColor);
        Assert.Equal(customColor2.WithAlpha(0x1f), sliderTheme.DisabledInactiveTrackColor);
        Assert.Equal(customColor2.WithAlpha(0x1f), sliderTheme.DisabledSecondaryActiveTrackColor);
        Assert.Equal(customColor3.WithAlpha(0x8a), sliderTheme.ActiveTickMarkColor);
        Assert.Equal(customColor1.WithAlpha(0x8a), sliderTheme.InactiveTickMarkColor);
        Assert.Equal(customColor3.WithAlpha(0x1f), sliderTheme.DisabledActiveTickMarkColor);
        Assert.Equal(customColor2.WithAlpha(0x1f), sliderTheme.DisabledInactiveTickMarkColor);
        Assert.Equal(customColor1.WithAlpha(0xff), sliderTheme.ThumbColor);
        Assert.Equal(customColor2.WithAlpha(0x52), sliderTheme.DisabledThumbColor);
        Assert.Equal(customColor1.WithAlpha(0x1f), sliderTheme.OverlayColor);
        Assert.Equal(customColor1.WithAlpha(0xff), sliderTheme.ValueIndicatorColor);
        Assert.Equal(customColor1.WithAlpha(0xff), sliderTheme.ValueIndicatorStrokeColor);
        Assert.Equal(customColor4, sliderTheme.ValueIndicatorTextStyle!.Color);
    }

    // Flutter: "SliderThemeData generates correct shapes for fromPrimaryColors"
    // Dart compares the const shapes by identity; C# shapes are not canonicalized, so the runtime
    // type (and the const constructor's default fields) is what is checked.
    [Fact]
    public void SliderThemeDataGeneratesCorrectShapesForFromPrimaryColors()
    {
        var customColor1 = new Color(0xcafefeed);
        var customColor2 = new Color(0xdeadbeef);
        var customColor3 = new Color(0xdecaface);
        var customColor4 = new Color(0xfeedcafe);

        SliderThemeData sliderTheme = SliderThemeData.FromPrimaryColors(
            primaryColor: customColor1,
            primaryColorDark: customColor2,
            primaryColorLight: customColor3,
            valueIndicatorTextStyle: ThemeData.Fallback.TextTheme.BodyLarge!.CopyWith(color: customColor4));

        Assert.IsType<RoundSliderOverlayShape>(sliderTheme.OverlayShape);
        Assert.IsType<RoundSliderTickMarkShape>(sliderTheme.TickMarkShape);
        Assert.IsType<RoundSliderThumbShape>(sliderTheme.ThumbShape);
        Assert.IsType<RoundedRectSliderTrackShape>(sliderTheme.TrackShape);
        Assert.IsType<PaddleSliderValueIndicatorShape>(sliderTheme.ValueIndicatorShape);
        Assert.IsType<RoundRangeSliderTickMarkShape>(sliderTheme.RangeTickMarkShape);
        Assert.IsType<RoundRangeSliderThumbShape>(sliderTheme.RangeThumbShape);
        Assert.IsType<RoundedRectRangeSliderTrackShape>(sliderTheme.RangeTrackShape);
        Assert.IsType<PaddleRangeSliderValueIndicatorShape>(sliderTheme.RangeValueIndicatorShape);
    }

    // Flutter: "SliderThemeData lerps correctly"
    [Fact]
    public void SliderThemeDataLerpsCorrectly()
    {
        SliderThemeData sliderThemeBlack = SliderThemeData.FromPrimaryColors(
            primaryColor: MaterialColors.Black,
            primaryColorDark: MaterialColors.Black,
            primaryColorLight: MaterialColors.Black,
            valueIndicatorTextStyle: ThemeData.Fallback.TextTheme.BodyLarge!.CopyWith(color: MaterialColors.Black))
            .CopyWith(trackHeight: 2.0);
        SliderThemeData sliderThemeWhite = SliderThemeData.FromPrimaryColors(
            primaryColor: MaterialColors.White,
            primaryColorDark: MaterialColors.White,
            primaryColorLight: MaterialColors.White,
            valueIndicatorTextStyle: ThemeData.Fallback.TextTheme.BodyLarge!.CopyWith(color: MaterialColors.White))
            .CopyWith(trackHeight: 6.0);
        SliderThemeData lerp = SliderThemeData.Lerp(sliderThemeBlack, sliderThemeWhite, 0.5);
        var middleGrey = new Color(0xff7f7f7f);

        Assert.Equal(4.0, lerp.TrackHeight);
        AssertSameColorAs(middleGrey.WithAlpha(0xff), lerp.ActiveTrackColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x3d), lerp.InactiveTrackColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x8a), lerp.SecondaryActiveTrackColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x52), lerp.DisabledActiveTrackColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x1f), lerp.DisabledInactiveTrackColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x1f), lerp.DisabledSecondaryActiveTrackColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x8a), lerp.ActiveTickMarkColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x8a), lerp.InactiveTickMarkColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x1f), lerp.DisabledActiveTickMarkColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x1f), lerp.DisabledInactiveTickMarkColor);
        AssertSameColorAs(middleGrey.WithAlpha(0xff), lerp.ThumbColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x52), lerp.DisabledThumbColor);
        AssertSameColorAs(middleGrey.WithAlpha(0x1f), lerp.OverlayColor);
        AssertSameColorAs(middleGrey.WithAlpha(0xff), lerp.ValueIndicatorColor);
        AssertSameColorAs(middleGrey.WithAlpha(0xff), lerp.ValueIndicatorStrokeColor);
        AssertSameColorAs(middleGrey.WithAlpha(0xff), lerp.ValueIndicatorTextStyle!.Color);
    }

    // Flutter: "Default slider track draws correctly"
    [Fact]
    public void DefaultSliderTrackDrawsCorrectly()
    {
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
        SliderThemeData sliderTheme = theme.SliderTheme.CopyWith(thumbColor: MaterialColors.Red.Shade500);

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25, secondaryTrackValue: 0.5));
        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        Radius radius = Radius.Circular(2);
        Radius activatedRadius = Radius.Circular(3);

        // The enabled slider thumb has track segments that extend to and from
        // the center of the thumb.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(
                    rrect: RRect.FromLTRBR(210.0, 298.0, 776.0, 302.0, radius),
                    color: sliderTheme.InactiveTrackColor)
                // Active track.
                .RRect(
                    rrect: RRect.FromLTRBR(24.0, 297.0, 214.0, 303.0, activatedRadius),
                    color: sliderTheme.ActiveTrackColor)
                .RRect(
                    rrect: RRect.FromLTRBAndCorners(
                        212.0,
                        298.0,
                        400.0,
                        302.0,
                        topRight: radius,
                        bottomRight: radius),
                    color: sliderTheme.SecondaryActiveTrackColor));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25, secondaryTrackValue: 0.5, enabled: false));
        tester.PumpAndSettle(); // wait for disable animation

        // The disabled slider thumb is the same size as the enabled thumb.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(
                    rrect: RRect.FromLTRBR(210.0, 298.0, 776.0, 302.0, radius),
                    color: sliderTheme.DisabledInactiveTrackColor)
                // Active track.
                .RRect(
                    rrect: RRect.FromLTRBR(24.0, 297.0, 214.0, 303.0, activatedRadius),
                    color: sliderTheme.DisabledActiveTrackColor)
                .RRect(
                    rrect: RRect.FromLTRBAndCorners(
                        212.0,
                        298.0,
                        400.0,
                        302.0,
                        topRight: radius,
                        bottomRight: radius),
                    color: sliderTheme.DisabledSecondaryActiveTrackColor));
    }

    // Flutter: "Default slider overlay draws correctly"
    [Fact]
    public void DefaultSliderOverlayDrawsCorrectly()
    {
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
        SliderThemeData sliderTheme = theme.SliderTheme.CopyWith(thumbColor: MaterialColors.Red.Shade500);

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25));
        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // With no touch, paints only the thumb.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor, x: 212.0, y: 300.0, radius: 10.0));

        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        // Wait for overlay animation to finish.
        tester.PumpAndSettle();

        // After touch, paints thumb and overlay.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(color: sliderTheme.OverlayColor, x: 212.0, y: 300.0, radius: 24.0)
                .Circle(color: sliderTheme.ThumbColor, x: 212.0, y: 300.0, radius: 10.0));

        gesture.Up();
        tester.PumpAndSettle();

        // After the gesture is up and complete, it again paints only the thumb.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor, x: 212.0, y: 300.0, radius: 10.0));
    }

    // Flutter: "Slider can use theme overlay with material states"
    [Fact]
    public void SliderCanUseThemeOverlayWithMaterialStates()
    {
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
        SliderThemeData sliderTheme = theme.SliderTheme.CopyWith(
            overlayColor: WidgetStateColor.ResolveWith(states =>
            {
                if (states.Contains(WidgetState.Focused))
                {
                    return MaterialColors.Brown[500]!;
                }

                return MaterialColors.Transparent;
            }));
        using var focusNode = new FocusNode(debugLabel: "Slider");
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        double value = 0.5;

        Widget BuildLocalApp(bool enabled = true)
        {
            return new MaterialApp(
                theme: new ThemeData(sliderTheme: sliderTheme),
                home: new MaterialWidget(
                    child: new Center(
                        child: new StatefulBuilder(
                            builder: (context, setState) => new Slider(
                                value: value,
                                onChanged: enabled
                                    ? newValue => setState(() => value = newValue)
                                    : null,
                                autofocus: true,
                                focusNode: focusNode)))));
        }

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildLocalApp());

        // Check that the overlay shows when focused.
        tester.PumpAndSettle();
        Assert.True(focusNode.HasPrimaryFocus);
        PaintAssert.Paints(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: MaterialColors.Brown[500]));

        // Check that the overlay does not show when focused and disabled.
        tester.PumpWidget(BuildLocalApp(enabled: false));
        tester.PumpAndSettle();
        Assert.False(focusNode.HasPrimaryFocus);
        PaintAssert.DoesNotPaint(
            MaterialOf(tester.ElementOfType<Slider>()),
            PaintPattern.Paints.Circle(color: MaterialColors.Brown[500]));
    }

    // Flutter: "Default slider ticker and thumb shape draw correctly"
    [Fact]
    public void DefaultSliderTickerAndThumbShapeDrawCorrectly()
    {
        var theme = new ThemeData(platform: TargetPlatform.Android, primarySwatch: MaterialColors.Blue);
        SliderThemeData sliderTheme = theme.SliderTheme.CopyWith(thumbColor: MaterialColors.Red.Shade500);

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.45));
        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        PaintAssert.Paints(material, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor, radius: 10.0));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.45, enabled: false));
        tester.PumpAndSettle(); // wait for disable animation

        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor, radius: 10.0));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.45, divisions: 3));
        tester.PumpAndSettle(); // wait for enable animation

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(color: sliderTheme.ActiveTickMarkColor)
                .Circle(color: sliderTheme.ActiveTickMarkColor)
                .Circle(color: sliderTheme.InactiveTickMarkColor)
                .Circle(color: sliderTheme.InactiveTickMarkColor)
                .Circle(color: sliderTheme.ThumbColor, radius: 10.0));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.45, divisions: 3, enabled: false));
        tester.PumpAndSettle(); // wait for disable animation

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(color: sliderTheme.DisabledActiveTickMarkColor)
                .Circle(color: sliderTheme.DisabledInactiveTickMarkColor)
                .Circle(color: sliderTheme.DisabledInactiveTickMarkColor)
                .Circle(color: sliderTheme.DisabledInactiveTickMarkColor)
                .Circle(color: sliderTheme.DisabledThumbColor, radius: 10.0));
    }

    // Flutter: "Default paddle slider value indicator shape draws correctly" (line 1023)
    [Fact]
    public void DefaultPaddleSliderValueIndicatorShapeDrawsCorrectly() => PaddleValueIndicatorDrawsCorrectly();

    // Flutter: "Default paddle slider value indicator shape draws correctly" (line 1208, a verbatim
    // duplicate of the test at line 1023)
    [Fact]
    public void DefaultPaddleSliderValueIndicatorShapeDrawsCorrectlyDuplicate() =>
        PaddleValueIndicatorDrawsCorrectly();

    // Flutter: "The slider track height can be overridden"
    [Fact]
    public void TheSliderTrackHeightCanBeOverridden()
    {
        SliderThemeData sliderTheme = new ThemeData().SliderTheme.CopyWith(trackHeight: 16);
        Radius radius = Radius.Circular(8);
        Radius activatedRadius = Radius.Circular(9);

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // Top and bottom are centerY (300) + and - trackRadius (8).
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(
                    rrect: RRect.FromLTRBR(204.0, 292.0, 776.0, 308.0, radius),
                    color: sliderTheme.InactiveTrackColor)
                // Active track.
                .RRect(
                    rrect: RRect.FromLTRBR(24.0, 291.0, 220.0, 309.0, activatedRadius),
                    color: sliderTheme.ActiveTrackColor));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25, enabled: false));
        tester.PumpAndSettle(); // wait for disable animation

        // The disabled thumb is smaller so the active track has to paint longer to
        // get to the edge.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track.
                .RRect(
                    rrect: RRect.FromLTRBR(204.0, 292.0, 776.0, 308.0, radius),
                    color: sliderTheme.DisabledInactiveTrackColor)
                // Active track.
                .RRect(
                    rrect: RRect.FromLTRBR(24.0, 291.0, 220.0, 309.0, activatedRadius),
                    color: sliderTheme.DisabledActiveTrackColor));
    }

    // Flutter: "The default slider thumb shape sizes can be overridden"
    [Fact]
    public void TheDefaultSliderThumbShapeSizesCanBeOverridden()
    {
        SliderThemeData sliderTheme = new ThemeData().SliderTheme.CopyWith(
            thumbShape: new RoundSliderThumbShape(enabledThumbRadius: 7, disabledThumbRadius: 11));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25));
        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: 212, y: 300, radius: 7, color: sliderTheme.ThumbColor));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25, enabled: false));
        tester.PumpAndSettle(); // wait for disable animation

        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: 212, y: 300, radius: 11, color: sliderTheme.DisabledThumbColor));
    }

    // Flutter: "The default slider thumb shape disabled size can be inferred from the enabled size"
    [Fact]
    public void TheDefaultSliderThumbShapeDisabledSizeCanBeInferredFromTheEnabledSize()
    {
        SliderThemeData sliderTheme = new ThemeData().SliderTheme.CopyWith(
            thumbShape: new RoundSliderThumbShape(enabledThumbRadius: 9));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25));
        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: 212, y: 300, radius: 9, color: sliderTheme.ThumbColor));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.25, enabled: false));
        tester.PumpAndSettle(); // wait for disable animation
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(x: 212, y: 300, radius: 9, color: sliderTheme.DisabledThumbColor));
    }

    // Flutter: "The default slider tick mark shape size can be overridden"
    [Fact]
    public void TheDefaultSliderTickMarkShapeSizeCanBeOverridden()
    {
        SliderThemeData sliderTheme = new ThemeData().SliderTheme.CopyWith(
            tickMarkShape: new RoundSliderTickMarkShape(tickMarkRadius: 5),
            activeTickMarkColor: new Color(0xfadedead),
            inactiveTickMarkColor: new Color(0xfadebeef),
            disabledActiveTickMarkColor: new Color(0xfadecafe),
            disabledInactiveTickMarkColor: new Color(0xfadeface));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.5, divisions: 2));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(x: 26, y: 300, radius: 5, color: sliderTheme.ActiveTickMarkColor)
                .Circle(x: 400, y: 300, radius: 5, color: sliderTheme.ActiveTickMarkColor)
                .Circle(x: 774, y: 300, radius: 5, color: sliderTheme.InactiveTickMarkColor));

        tester.PumpWidget(BuildApp(sliderTheme, value: 0.5, divisions: 2, enabled: false));
        tester.PumpAndSettle();

        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                .Circle(x: 26, y: 300, radius: 5, color: sliderTheme.DisabledActiveTickMarkColor)
                .Circle(x: 400, y: 300, radius: 5, color: sliderTheme.DisabledActiveTickMarkColor)
                .Circle(x: 774, y: 300, radius: 5, color: sliderTheme.DisabledInactiveTickMarkColor));
    }

    // Flutter: "The default slider overlay shape size can be overridden"
    [Fact]
    public void TheDefaultSliderOverlayShapeSizeCanBeOverridden()
    {
        const double uniqueOverlayRadius = 23;
        SliderThemeData sliderTheme = new ThemeData().SliderTheme.CopyWith(
            overlayShape: new RoundSliderOverlayShape(overlayRadius: uniqueOverlayRadius));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.5));
        // Tap center and wait for animation.
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());
        PaintAssert.Paints(
            material,
            PaintPattern.Paints.Circle(
                x: center.X,
                y: center.Y,
                radius: uniqueOverlayRadius,
                color: sliderTheme.OverlayColor));

        // Finish gesture to release resources.
        gesture.Up();
        tester.PumpAndSettle();
    }

    // Regression test for https://github.com/flutter/flutter/issues/74503
    // Flutter: "The slider track layout correctly when the overlay size is smaller than the thumb size"
    [Fact]
    public void TheSliderTrackLayoutCorrectlyWhenTheOverlaySizeIsSmallerThanTheThumbSize()
    {
        SliderThemeData sliderTheme = new ThemeData().SliderTheme.CopyWith(
            overlayShape: SliderComponentShape.NoOverlay);

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildApp(sliderTheme, value: 0.5));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // The track rectangle begins at 10 pixels from the left of the screen and ends 10 pixels from the right
        // (790 pixels from the left). The main check here it that the track itself should be centered on
        // the 800 pixel-wide screen.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // Inactive track RRect. Ends 10 pixels from right of screen.
                .RRect(rrect: RRect.FromLTRBR(398.0, 298.0, 790.0, 302.0, Radius.Circular(2.0)))
                // Active track RRect. Starts 10 pixels from left of screen.
                .RRect(rrect: RRect.FromLTRBR(10.0, 297.0, 402.0, 303.0, Radius.Circular(3.0)))
                // The thumb.
                .Circle(x: 400.0, y: 300.0, radius: 10.0));
    }

    // Regression test for https://github.com/flutter/flutter/issues/125467
    // Flutter: "The RangeSlider track layout correctly when the overlay size is smaller than the thumb size"
    [Fact]
    public void TheRangeSliderTrackLayoutCorrectlyWhenTheOverlaySizeIsSmallerThanTheThumbSize()
    {
        SliderThemeData sliderTheme = new ThemeData().SliderTheme.CopyWith(
            overlayShape: SliderComponentShape.NoOverlay);

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(BuildRangeApp(sliderTheme, values: new RangeValues(0.0, 1.0)));

        RenderObject material = MaterialOf(tester.ElementOfType<RangeSlider>());

        // The track rectangle begins at 10 pixels from the left of the screen and ends 10 pixels from the right
        // (790 pixels from the left). The main check here it that the track itself should be centered on
        // the 800 pixel-wide screen.
        PaintAssert.Paints(
            material,
            PaintPattern.Paints
                // active track RRect. Starts 10 pixels from left of screen.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    10.0,
                    298.0,
                    10.0,
                    302.0,
                    topLeft: Radius.Circular(2.0),
                    bottomLeft: Radius.Circular(2.0)))
                // inactive track RRect. Ends 10 pixels from right of screen.
                .RRect(rrect: RRect.FromLTRBAndCorners(
                    790.0,
                    298.0,
                    790.0,
                    302.0,
                    topRight: Radius.Circular(2.0),
                    bottomRight: Radius.Circular(2.0)))
                // active track RRect Start 10 pixels from left screen.
                .RRect(rrect: RRect.FromLTRBR(8.0, 297.0, 792.0, 303.0, Radius.Circular(2.0)))
                // The thumb Left.
                .Circle(x: 10.0, y: 300.0, radius: 10.0)
                // The thumb Right.
                .Circle(x: 790.0, y: 300.0, radius: 10.0));
    }

    // Only the thumb, overlay, and tick mark have special shortcuts to provide
    // no-op or empty shapes.
    //
    // The track can also be skipped by providing 0 height.
    //
    // The value indicator can be skipped by passing the appropriate
    // [ShowValueIndicator].
    // Flutter: "The slider can skip all of its component painting"
    [Fact]
    public void TheSliderCanSkipAllOfItsComponentPainting()
    {
        using var tester = new FrameworkDartTester();
        // Pump a slider with all shapes skipped.
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                overlayShape: SliderComponentShape.NoOverlay,
                thumbShape: SliderComponentShape.NoThumb,
                tickMarkShape: SliderTickMarkShape.NoTickMark,
                showValueIndicator: ShowValueIndicator.Never),
            value: 0.5,
            divisions: 4));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        IReadOnlyList<CanvasCall> calls = PaintRecording.Record(material);
        Assert.Equal(0, calls.CountCalls("drawRect"));
        Assert.Equal(0, calls.CountCalls("drawCircle"));
        Assert.Equal(0, calls.CountCalls("drawPath"));
    }

    // Flutter: "The slider can skip all component painting except the track"
    [Fact]
    public void TheSliderCanSkipAllComponentPaintingExceptTheTrack()
    {
        using var tester = new FrameworkDartTester();
        // Pump a slider with just a track.
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                overlayShape: SliderComponentShape.NoOverlay,
                thumbShape: SliderComponentShape.NoThumb,
                tickMarkShape: SliderTickMarkShape.NoTickMark,
                showValueIndicator: ShowValueIndicator.Never),
            value: 0.5,
            divisions: 4));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // Only 2 track segments.
        IReadOnlyList<CanvasCall> calls = PaintRecording.Record(material);
        Assert.Equal(2, calls.CountCalls("drawRRect"));
        Assert.Equal(0, calls.CountCalls("drawCircle"));
        Assert.Equal(0, calls.CountCalls("drawPath"));
    }

    // Flutter: "The slider can skip all component painting except the tick marks"
    [Fact]
    public void TheSliderCanSkipAllComponentPaintingExceptTheTickMarks()
    {
        using var tester = new FrameworkDartTester();
        // Pump a slider with just tick marks.
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                overlayShape: SliderComponentShape.NoOverlay,
                thumbShape: SliderComponentShape.NoThumb,
                showValueIndicator: ShowValueIndicator.Never,
                // When the track is hidden to 0 height, a tick mark radius
                // must be provided to get a non-zero radius.
                tickMarkShape: new RoundSliderTickMarkShape(tickMarkRadius: 1)),
            value: 0.5,
            divisions: 4));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // Only 5 tick marks.
        IReadOnlyList<CanvasCall> calls = PaintRecording.Record(material);
        Assert.Equal(0, calls.CountCalls("drawRect"));
        Assert.Equal(5, calls.CountCalls("drawCircle"));
        Assert.Equal(0, calls.CountCalls("drawPath"));
    }

    // Flutter: "The slider can skip all component painting except the thumb"
    [Fact]
    public void TheSliderCanSkipAllComponentPaintingExceptTheThumb()
    {
        RenderingDebug.DisableShadows = false;
        try
        {
            using var tester = new FrameworkDartTester();
            // Pump a slider with just a thumb.
            tester.PumpWidget(BuildApp(
                new ThemeData().SliderTheme.CopyWith(
                    trackHeight: 0,
                    overlayShape: SliderComponentShape.NoOverlay,
                    tickMarkShape: SliderTickMarkShape.NoTickMark,
                    showValueIndicator: ShowValueIndicator.Never),
                value: 0.5,
                divisions: 4));

            RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

            // Only 1 thumb.
            IReadOnlyList<CanvasCall> calls = PaintRecording.Record(material);
            Assert.Equal(0, calls.CountCalls("drawRect"));
            Assert.Equal(1, calls.CountCalls("drawCircle"));
            Assert.Equal(0, calls.CountCalls("drawPath"));
        }
        finally
        {
            RenderingDebug.DisableShadows = true;
        }
    }

    // Flutter: "The slider can skip all component painting except the overlay"
    [Fact]
    public void TheSliderCanSkipAllComponentPaintingExceptTheOverlay()
    {
        using var tester = new FrameworkDartTester();
        // Pump a slider with just an overlay.
        tester.PumpWidget(BuildApp(
            new ThemeData().SliderTheme.CopyWith(
                trackHeight: 0,
                thumbShape: SliderComponentShape.NoThumb,
                tickMarkShape: SliderTickMarkShape.NoTickMark,
                showValueIndicator: ShowValueIndicator.Never),
            value: 0.5,
            divisions: 4));

        RenderObject material = MaterialOf(tester.ElementOfType<Slider>());

        // Tap the center of the track and wait for animations to finish.
        Point center = tester.GetCenter(tester.ElementOfType<Slider>());
        TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        // Only 1 overlay.
        IReadOnlyList<CanvasCall> calls = PaintRecording.Record(material);
        Assert.Equal(0, calls.CountCalls("drawRect"));
        Assert.Equal(1, calls.CountCalls("drawCircle"));
        Assert.Equal(0, calls.CountCalls("drawPath"));

        gesture.Up();
    }

    // The body shared by the two identical "Default paddle slider value indicator shape draws
    // correctly" tests (lines 1023 and 1208).
    private static void PaddleValueIndicatorDrawsCorrectly()
    {
        RenderingDebug.DisableShadows = false;
        try
        {
            var theme = new ThemeData(
                useMaterial3: false,
                platform: TargetPlatform.Android,
                primarySwatch: MaterialColors.Blue);
#pragma warning disable CS0618 // Dart's test uses the deprecated ShowValueIndicator.always.
            SliderThemeData sliderTheme = theme.SliderTheme.CopyWith(
                thumbColor: MaterialColors.Red.Shade500,
                showValueIndicator: ShowValueIndicator.Always,
                valueIndicatorShape: new PaddleSliderValueIndicatorShape());
#pragma warning restore CS0618
            Widget BuildLocalApp(string value, double sliderValue = 0.5, TextScaler? textScaler = null)
            {
                return new MaterialApp(
                    theme: theme,
                    home: new Directionality(
                        textDirection: TextDirection.Ltr,
                        child: new MediaQuery(
                            data: new MediaQueryData(TextScaler: textScaler ?? TextScaler.NoScaling),
                            child: new MaterialWidget(
                                child: new Row(
                                    children:
                                    [
                                        new Expanded(
                                            child: new SliderTheme(
                                                data: sliderTheme,
                                                child: new Slider(
                                                    value: sliderValue,
                                                    label: value,
                                                    divisions: 3,
                                                    onChanged: _ => { }))),
                                    ])))));
            }

            using var tester = new FrameworkDartTester();
            tester.PumpWidget(BuildLocalApp("1"));

            RenderObject valueIndicatorBox = tester.ElementOfType<Overlay>().FindRenderObject()!;

            void PressAndExpect(Point[] includes, Point[] excludes)
            {
                Point center = tester.GetCenter(tester.ElementOfType<Slider>());
                TestGesture gesture = tester.StartGesture(center, PointerDeviceKind.Touch);
                // Wait for value indicator animation to finish.
                tester.PumpAndSettle();
                PaintAssert.Paints(
                    valueIndicatorBox,
                    PaintPattern.Paints.Path(
                        color: sliderTheme.ValueIndicatorColor,
                        includes: includes,
                        excludes: excludes));
                gesture.Up();
            }

            PressAndExpect(
                [new Point(0.0, -40.0), new Point(15.9, -40.0), new Point(-15.9, -40.0)],
                [new Point(16.1, -40.0), new Point(-16.1, -40.0)]);

            // Test that it expands with a larger label.
            tester.PumpWidget(BuildLocalApp("1000"));
            PressAndExpect(
                [new Point(0.0, -40.0), new Point(35.9, -40.0), new Point(-35.9, -40.0)],
                [new Point(36.1, -40.0), new Point(-36.1, -40.0)]);

            // Test that it avoids the left edge of the screen.
            tester.PumpWidget(BuildLocalApp("1000000", sliderValue: 0.0));
            PressAndExpect(
                [new Point(0.0, -40.0), new Point(92.0, -40.0), new Point(-16.0, -40.0)],
                [new Point(98.1, -40.0), new Point(-20.1, -40.0)]);

            // Test that it avoids the right edge of the screen.
            tester.PumpWidget(BuildLocalApp("1000000", sliderValue: 1.0));
            PressAndExpect(
                [new Point(0.0, -40.0), new Point(16.0, -40.0), new Point(-92.0, -40.0)],
                [new Point(20.1, -40.0), new Point(-98.1, -40.0)]);

            // Test that the neck stretches when the text scale gets smaller.
            tester.PumpWidget(BuildLocalApp("1000000", sliderValue: 0.0, textScaler: TextScaler.Linear(0.5)));
            PressAndExpect(
                [new Point(0.0, -49.0), new Point(68.0, -49.0), new Point(-24.0, -49.0)],
                [
                    new Point(98.0, -32.0), // inside full size, outside small
                    new Point(-40.0, -32.0), // inside full size, outside small
                    new Point(90.1, -49.0),
                    new Point(-40.1, -49.0),
                ]);

            // Test that the neck shrinks when the text scale gets larger.
            tester.PumpWidget(BuildLocalApp("1000000", sliderValue: 0.0, textScaler: TextScaler.Linear(2.5)));
            PressAndExpect(
                [
                    new Point(0.0, -38.8),
                    new Point(92.0, -38.8),
                    new Point(8.0, -23.0), // Inside large, outside scale=1.0
                    new Point(-2.0, -23.0), // Inside large, outside scale=1.0
                ],
                [new Point(98.5, -38.8), new Point(-16.1, -38.8)]);
        }
        finally
        {
            RenderingDebug.DisableShadows = true;
        }
    }

    // The repeated "no disabled colours" block of "Slider uses the right theme colors for the right
    // components".
    private static void AssertNoDisabledColors(RenderObject material, SliderThemeData sliderTheme)
    {
        PaintAssert.DoesNotPaint(material, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints.RRect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints.RRect(color: sliderTheme.DisabledInactiveTrackColor));
        PaintAssert.DoesNotPaint(
            material,
            PaintPattern.Paints.RRect(color: sliderTheme.DisabledSecondaryActiveTrackColor));
    }

    /// <summary>Dart's <c>Material.of(context)</c> as the render object the <c>paints</c> matcher records.</summary>
    private static RenderObject MaterialOf(Element element) =>
        LookupBoundary.FindAncestorRenderObjectOfType<RenderInkFeatures>(element)!;

    /// <summary>flutter_test's <c>isSameColorAs</c> (default threshold <c>colorEpsilon</c> = 0.004).</summary>
    private static void AssertSameColorAs(Color expected, Color? actual)
    {
        const double threshold = 0.004;
        Assert.NotNull(actual);
        Assert.True(
            actual!.ColorSpace == expected.ColorSpace
            && Math.Abs(actual.A - expected.A) <= threshold
            && Math.Abs(actual.R - expected.R) <= threshold
            && Math.Abs(actual.G - expected.G) <= threshold
            && Math.Abs(actual.B - expected.B) <= threshold,
            $"{actual} is not the same color as {expected}");
    }

    /// <summary>Dart's <c>double.toString()</c>, as used by the <c>'$value'</c> labels.</summary>
    private static string DartDouble(double value)
    {
        string text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.Contains('.', StringComparison.Ordinal) || text.Contains('E', StringComparison.Ordinal)
            ? text
            : text + ".0";
    }

    /// <summary>The file-level <c>_buildApp</c> helper.</summary>
    private static Widget BuildApp(
        SliderThemeData sliderTheme,
        double value = 0.0,
        double? secondaryTrackValue = null,
        bool enabled = true,
        int? divisions = null,
        FocusNode? focusNode = null)
    {
        Action<double>? onChanged = enabled ? d => value = d : null;
        return new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: new SliderTheme(
                        data: sliderTheme,
                        child: new Slider(
                            value: value,
                            secondaryTrackValue: secondaryTrackValue,
                            label: DartDouble(value),
                            onChanged: onChanged,
                            divisions: divisions,
                            focusNode: focusNode)))));
    }

    /// <summary>The file-level <c>_buildRangeApp</c> helper.</summary>
    private static Widget BuildRangeApp(
        SliderThemeData sliderTheme,
        RangeValues? values = null,
        bool enabled = true,
        int? divisions = null)
    {
        values ??= new RangeValues(0, 0);
        Action<RangeValues>? onChanged = enabled ? d => values = d : null;
        return new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: new SliderTheme(
                        data: sliderTheme,
                        child: new RangeSlider(
                            values: values,
                            labels: new RangeLabels(DartDouble(values.Start), DartDouble(values.End)),
                            onChanged: onChanged,
                            divisions: divisions)))));
    }

    /// <summary>
    /// Dart passes <c>WidgetStateMouseCursor.clickable</c>, which implements
    /// <c>WidgetStateProperty&lt;MouseCursor&gt;</c>; the C# <see cref="WidgetStateMouseCursor"/> derives
    /// from <see cref="MouseCursor"/> only (recorded divergence), so this forwards to it.
    /// </summary>
    private sealed class ClickableMouseCursorProperty : WidgetStateProperty<MouseCursor?>
    {
        public override MouseCursor? Resolve(IReadOnlySet<WidgetState> states) =>
            WidgetStateMouseCursor.Clickable.Resolve(states);

        public override string ToString() => WidgetStateMouseCursor.Clickable.ToString();
    }
}
