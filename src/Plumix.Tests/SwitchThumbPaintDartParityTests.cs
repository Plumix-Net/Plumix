// Dart parity source: material_ui/lib/src/switch.dart
// Mirrors the thumb-shadow paint expectations of material-ui-src/test/switch_test.dart and
// cupertino-ui-src/test/switch_test.dart: the thumb shadows are drawn through BoxShadow.toPaint
// (Material: the thumb's ShapeDecoration; Cupertino: one drawRRect per shadow) before the thumb.
using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Xunit.Sdk;
using MaterialWidget = Plumix.Material.Material;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SwitchThumbPaintDartParityTests : IDisposable
{
    public SwitchThumbPaintDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    // flutter_test runs every test with `debugDefaultTargetPlatformOverride == TargetPlatform.android`.
    private static FrameworkDartTester CreateTester(TargetPlatform platform = TargetPlatform.Android)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        SemanticsNode.DebugResetSemanticsIdCounter();
        return new FrameworkDartTester(fakeGestureTimers: true);
    }

    // `Material.of(context)` as a paint target: the Material's ink-feature render object.
    private static RenderObject MaterialOf(Element element) =>
        LookupBoundary.FindAncestorRenderObjectOfType<RenderInkFeatures>(element)
        ?? throw new XunitException("no Material ancestor");

    // Flutter (material switch_test.dart): "Material2 - Switch has default colors when enabled"
    [Fact]
    public void Material2SwitchHasDefaultColorsWhenEnabled()
    {
        using FrameworkDartTester tester = CreateTester();
        bool value = false;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: false),
            home: new Directionality(
                textDirection: TextDirection.Rtl,
                child: new StatefulBuilder((context, setState) => new MaterialWidget(
                    child: new Center(
                        child: new Switch(
                            value,
                            newValue => setState(() => value = newValue),
                            dragStartBehavior: DragStartBehavior.Down)))))));

        PaintAssert.Paints(
            MaterialOf(tester.ElementOfType<Switch>()),
            PaintPattern.Paints
                .RRect(
                    color: new Color(0x52000000), // Black with 32% opacity
                    rrect: RRect.FromLTRBR(13.0, 17.0, 46.0, 31.0, Radius.Circular(7.0)))
                .RRect(color: new Color(0x00000000))
                .RRect(color: new Color(0x33000000))
                .RRect(color: new Color(0x24000000))
                .RRect(color: new Color(0x1f000000))
                .RRect(color: Plumix.Material.Colors.Grey.Shade50));

        tester.Drag(Find.ByType<Switch>(), new Vector(-30.0, 0.0));
        tester.Pump();

        PaintAssert.Paints(
            MaterialOf(tester.ElementOfType<Switch>()),
            PaintPattern.Paints
                .RRect(
                    color: new Color(0x802196f3),
                    rrect: RRect.FromLTRBR(13.0, 17.0, 46.0, 31.0, Radius.Circular(7.0)))
                .RRect(color: new Color(0x00000000))
                .RRect(color: new Color(0x33000000))
                .RRect(color: new Color(0x24000000))
                .RRect(color: new Color(0x1f000000))
                .RRect(color: new Color(0xff2196f3)));
    }

    // Flutter (material switch_test.dart): "Switch.adaptive default thumb/track color and size(Cupertino)"
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void SwitchAdaptiveDefaultThumbTrackColorAndSizeCupertino(TargetPlatform platform)
    {
        using FrameworkDartTester tester = CreateTester(platform);
        Color thumbColor = Plumix.Material.Colors.White;
        Color inactiveTrackColor = Color.FromARGB(40, 120, 120, 128);
        Color activeTrackColor = Color.FromARGB(255, 52, 199, 89);
        // Switches have same sizes on both platform but they are more compact on macOS.
        RRect trackRRect = platform == TargetPlatform.IOS
            ? RRect.FromLTRBR(4.5, 8.5, 55.5, 39.5, Radius.Circular(15.5))
            : RRect.FromLTRBR(4.5, 4.5, 55.5, 35.5, Radius.Circular(15.5));
        RRect inactiveThumbRRect = platform == TargetPlatform.IOS
            ? RRect.FromLTRBR(6.0, 10.0, 34.0, 38.0, Radius.Circular(14.0))
            : RRect.FromLTRBR(6.0, 6.0, 34.0, 34.0, Radius.Circular(14.0));
        RRect activeThumbRRect = platform == TargetPlatform.IOS
            ? RRect.FromLTRBR(26.0, 10.0, 54.0, 38.0, Radius.Circular(14.0))
            : RRect.FromLTRBR(26.0, 6.0, 54.0, 34.0, Radius.Circular(14.0));

        void ExpectThumb(Color trackColor, RRect thumbRRect, double opacity)
        {
            PaintAssert.Paints(
                MaterialOf(tester.ElementOfType<Switch>()),
                PaintPattern.Paints
                    .RRect(color: trackColor, rrect: trackRRect)
                    .RRect(color: new Color(0x00000000))
                    .RRect(color: new Color(0x26000000))
                    .RRect(color: new Color(0x0f000000))
                    .RRect(color: new Color(0x0a000000)) // Thumb border color(only cupertino)
                    .RRect(color: thumbColor, rrect: thumbRRect));
            Assert.Equal(opacity, tester.Widget<Opacity>(Find.ByType<Opacity>()).Value);
        }

        tester.PumpWidget(new Container());
        tester.PumpWidget(BuildAdaptiveSwitch(platform, value: false));
        ExpectThumb(inactiveTrackColor, inactiveThumbRRect, 1.0);

        tester.PumpWidget(new Container());
        tester.PumpWidget(BuildAdaptiveSwitch(platform));
        tester.Pump();
        ExpectThumb(activeTrackColor, activeThumbRRect, 1.0);

        // Test disabled switch.
        tester.PumpWidget(new Container());
        tester.PumpWidget(BuildAdaptiveSwitch(platform, enabled: false, value: false));
        tester.Pump();
        ExpectThumb(inactiveTrackColor, inactiveThumbRRect, 0.5);

        tester.PumpWidget(new Container());
        tester.PumpWidget(BuildAdaptiveSwitch(platform, enabled: false));
        tester.Pump();
        ExpectThumb(activeTrackColor, activeThumbRRect, 0.5);
    }

    // Flutter (cupertino switch_test.dart): "CupertinoSwitch is focusable and has correct focus color"
    [Fact]
    public void CupertinoSwitchIsFocusableAndHasCorrectFocusColor()
    {
        using FrameworkDartTester tester = CreateTester();
        var focusNode = new FocusNode(debugLabel: "CupertinoSwitch");
        try
        {
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            bool value = true;
            var focusColor = new Color(0xffff0000);

            Widget BuildApp(bool enabled = true)
            {
                return new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new StatefulBuilder((context, setState) => new Center(
                        child: new CupertinoSwitch(
                            value: value,
                            onChanged: enabled ? newValue => setState(() => value = newValue) : null,
                            focusColor: focusColor,
                            focusNode: focusNode,
                            autofocus: true))));
            }

            tester.PumpWidget(BuildApp());
            tester.PumpAndSettle();

            Assert.True(focusNode.HasPrimaryFocus);
            PaintAssert.Paints(
                Find.ByType<CupertinoSwitch>(),
                PaintPattern.Paints
                    .RRect(color: new Color(0xff34c759))
                    .RRect(color: focusColor)
                    .ClipRRect()
                    .RRect(color: new Color(0x26000000))
                    .RRect(color: new Color(0x0f000000))
                    .RRect(color: new Color(0x0a000000))
                    .RRect(color: new Color(0xffffffff)));

            // Check the false value.
            value = false;
            tester.PumpWidget(BuildApp());
            tester.PumpAndSettle();

            Assert.True(focusNode.HasPrimaryFocus);
            PaintAssert.Paints(
                Find.ByType<CupertinoSwitch>(),
                PaintPattern.Paints
                    .RRect(color: new Color(0x28787880))
                    .RRect(color: focusColor)
                    .ClipRRect()
                    .RRect(color: new Color(0x26000000))
                    .RRect(color: new Color(0x0f000000))
                    .RRect(color: new Color(0x0a000000))
                    .RRect(color: new Color(0xffffffff)));

            // Check what happens when disabled.
            value = false;
            tester.PumpWidget(BuildApp(enabled: false));
            tester.PumpAndSettle();

            Assert.False(focusNode.HasPrimaryFocus);
            PaintAssert.Paints(
                Find.ByType<CupertinoSwitch>(),
                PaintPattern.Paints
                    .RRect(color: new Color(0x28787880))
                    .ClipRRect()
                    .RRect(color: new Color(0x26000000))
                    .RRect(color: new Color(0x0f000000))
                    .RRect(color: new Color(0x0a000000))
                    .RRect(color: new Color(0xffffffff)));
        }
        finally
        {
            focusNode.Dispose();
        }
    }

    // switch_test.dart `buildAdaptiveSwitch`.
    private static Widget BuildAdaptiveSwitch(TargetPlatform platform, bool enabled = true, bool value = true)
    {
        Switch adaptiveSwitch = Switch.Adaptive(value, enabled ? _ => { } : null);
        return new MaterialApp(
            theme: new ThemeData(platform: platform),
            home: new StatefulBuilder((context, setState) => new MaterialWidget(
                child: new Center(child: adaptiveSwitch))));
    }
}
