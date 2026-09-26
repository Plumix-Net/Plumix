// Dart parity source: material_ui/lib/src/material.dart
// Mirrors material-ui-src/test/material_test.dart

using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;
using Path = Plumix.UI.Path;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDartParityTests : IDisposable
{
    private const string GoldenSkip =
        "matchesGoldenFile: Plumix has no golden-image comparison infrastructure; the border paint order it "
        + "pins is covered by MaterialCardTests.Material_ClipsAndPaintsShapeBorderAtConfiguredPaintOrder.";

    private readonly bool _previousDisableShadows = RenderingDebug.DisableShadows;

    public MaterialDartParityTests()
    {
        // flutter_test runs with debugDisableShadows == true.
        RenderingDebug.DisableShadows = true;
    }

    public void Dispose()
    {
        RenderingDebug.DisableShadows = _previousDisableShadows;
    }

    // Flutter: "MaterialApp.home nullable and update test"
    [Fact]
    public void MaterialAppHomeNullableAndUpdateTest()
    {
        using var tester = new FrameworkDartTester();
        // _WidgetsAppState._usesNavigator == true
        tester.PumpWidget(new MaterialApp(home: new SizedBox(width: 0.0, height: 0.0)));

        // _WidgetsAppState._usesNavigator == false
        tester.PumpWidget(new MaterialApp()); // Do not crash!

        // _WidgetsAppState._usesNavigator == true
        tester.PumpWidget(new MaterialApp(home: new SizedBox(width: 0.0, height: 0.0))); // Do not crash!

        Assert.Null(tester.TakeException());
    }

    // Flutter: "default Material debugFillProperties"
    [DebugOnlyFact]
    public void DefaultMaterialDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new MaterialWidget().DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();

        Assert.Equal(["type: canvas"], description);
    }

    // Flutter: "Material implements debugFillProperties"
    [DebugOnlyFact]
    public void MaterialImplementsDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new MaterialWidget(
            color: new Color(0xFFFFFFFF),
            shadowColor: new Color(0xffff0000),
            surfaceTintColor: new Color(0xff0000ff),
            textStyle: new TextStyle(Color: new Color(0xff00ff00)),
            borderRadius: BorderRadiusDirectional.Circular(10)).DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();

        Assert.Equal(
            [
                "type: canvas",
                $"color: {new Color(0xffffffff)}",
                $"shadowColor: {new Color(0xffff0000)}",
                $"surfaceTintColor: {new Color(0xff0000ff)}",
                "textStyle.inherit: true",
                $"textStyle.color: {new Color(0xff00ff00)}",
                "borderRadius: BorderRadiusDirectional.circular(10.0)",
            ],
            description);
    }

    // Flutter: "LayoutChangedNotification test"
    [Fact]
    public void LayoutChangedNotificationTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialWidget(child: new NotifyMaterial()));
        Assert.Null(tester.TakeException());
    }

    // Flutter: "ListView scroll does not repaint"
    [Fact]
    public void ListViewScrollDoesNotRepaint()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<Size>();

        tester.PumpWidget(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Column(
                    children:
                    [
                        new SizedBox(
                            width: 150.0,
                            height: 150.0,
                            child: new CustomPaint(painter: new PaintRecorder(log))),
                        new Expanded(
                            child: new MaterialWidget(
                                child: new Column(
                                    children:
                                    [
                                        new Expanded(
                                            child: new ListView(
                                                children:
                                                [
                                                    new Container(height: 2000.0, color: new Color(0xFF00FF00)),
                                                ])),
                                        new SizedBox(
                                            width: 100.0,
                                            height: 100.0,
                                            child: new CustomPaint(painter: new PaintRecorder(log))),
                                    ]))),
                    ])));

        // We paint twice because we have two CustomPaint widgets in the tree above
        // to test repainting both inside and outside the Material widget.
        Assert.Equal([new Size(150.0, 150.0), new Size(100.0, 100.0)], log);
        log.Clear();

        tester.Drag(tester.ElementOfType<ListView>(), new Vector(0.0, -300.0));
        tester.Pump();

        Assert.Empty(log);
    }

    // Flutter: "Shadow color defaults"
    [Fact]
    public void ShadowColorDefaults()
    {
        using var tester = new FrameworkDartTester();

        Widget BuildWithShadow(Color? shadowColor)
        {
            return new Center(
                child: new SizedBox(
                    width: 100.0,
                    height: 100.0,
                    child: new MaterialWidget(shadowColor: shadowColor, elevation: 10, shape: new CircleBorder())));
        }

        // Default M2 shadow color
        tester.PumpWidget(new Theme(data: new ThemeData(useMaterial3: false), child: BuildWithShadow(null)));
        tester.PumpAndSettle();
        Assert.Equal(new ThemeData().ShadowColor, GetModel(tester).ShadowColor);

        // Default M3 shadow color
        tester.PumpWidget(new Theme(data: new ThemeData(), child: BuildWithShadow(null)));
        tester.PumpAndSettle();
        Assert.Equal(new ThemeData().ColorScheme.Shadow, GetModel(tester).ShadowColor);

        // Drop shadow can be turned off with a transparent color.
        tester.PumpWidget(new Theme(data: new ThemeData(), child: BuildWithShadow(MaterialColors.Transparent)));
        tester.PumpAndSettle();
        Assert.Equal(MaterialColors.Transparent, GetModel(tester).ShadowColor);
    }

    // Flutter: "Shadows animate smoothly"
    [Fact]
    public void ShadowsAnimateSmoothly()
    {
        // This code verifies that the PhysicalModel's elevation animates over
        // a kThemeChangeDuration time interval.
        using var tester = new FrameworkDartTester();
        TimeSpan themeChangeDuration = MaterialConstants.ThemeAnimationDuration;

        tester.PumpWidget(BuildMaterial());
        RenderPhysicalShape modelA = GetModel(tester);
        Assert.Equal(0.0, modelA.Elevation);

        tester.PumpWidget(BuildMaterial(elevation: 9.0));
        RenderPhysicalShape modelB = GetModel(tester);
        Assert.Equal(0.0, modelB.Elevation);

        tester.Pump(TimeSpan.FromMilliseconds(1));
        RenderPhysicalShape modelC = GetModel(tester);
        Assert.Equal(0.0, modelC.Elevation, 0.001);

        tester.Pump(themeChangeDuration / 2);
        RenderPhysicalShape modelD = GetModel(tester);
        Assert.True(Math.Abs(modelD.Elevation) > 0.001);

        tester.Pump(themeChangeDuration);
        RenderPhysicalShape modelE = GetModel(tester);
        Assert.Equal(9.0, modelE.Elevation);
    }

    // Flutter: "Shadow colors animate smoothly"
    [Fact]
    public void ShadowColorsAnimateSmoothly()
    {
        // This code verifies that the PhysicalModel's shadowColor animates over
        // a kThemeChangeDuration time interval.
        using var tester = new FrameworkDartTester();
        TimeSpan themeChangeDuration = MaterialConstants.ThemeAnimationDuration;

        tester.PumpWidget(BuildMaterial());
        RenderPhysicalShape modelA = GetModel(tester);
        Assert.Equal(new Color(0xFF00FF00), modelA.ShadowColor);

        tester.PumpWidget(BuildMaterial(shadowColor: new Color(0xFFFF0000)));
        RenderPhysicalShape modelB = GetModel(tester);
        Assert.Equal(new Color(0xFF00FF00), modelB.ShadowColor);

        tester.Pump(TimeSpan.FromMilliseconds(1));
        RenderPhysicalShape modelC = GetModel(tester);
        Assert.True(MaxComponentColorDistance(modelC.ShadowColor, new Color(0xFF00FF00)) <= 1);

        tester.Pump(themeChangeDuration / 2);
        RenderPhysicalShape modelD = GetModel(tester);
        Assert.False(MaxComponentColorDistance(modelD.ShadowColor, new Color(0xFF00FF00)) <= 1);

        tester.Pump(themeChangeDuration);
        RenderPhysicalShape modelE = GetModel(tester);
        Assert.Equal(new Color(0xFFFF0000), modelE.ShadowColor);
    }

    // Flutter: "Transparent material widget does not absorb hit test"
    [Fact]
    public void TransparentMaterialWidgetDoesNotAbsorbHitTest()
    {
        // This is a regression test for https://github.com/flutter/flutter/issues/58665.
        using var tester = new FrameworkDartTester();
        bool pressed = false;
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    body: new Stack(
                        children:
                        [
                            new ElevatedButton(
                                onPressed: () => pressed = true,
                                // Dart passes `child: null`; the C# constructor takes a non-null child.
                                child: new SizedBox(width: 0.0, height: 0.0)),
                            new MaterialWidget(
                                type: MaterialType.Transparency,
                                child: new SizedBox(width: 400.0, height: 500.0)),
                        ]))));
        tester.Tap(tester.ElementOfType<ElevatedButton>());
        Assert.True(pressed);
    }

    // Flutter: "Material uses the dark SystemUIOverlayStyle when the background is light"
    [Fact]
    public void MaterialUsesTheDarkSystemUiOverlayStyleWhenTheBackgroundIsLight()
    {
        using var tester = new FrameworkDartTester();
        var lightTheme = new ThemeData();
        tester.PumpWidget(
            new MaterialApp(
                theme: lightTheme,
                home: new Scaffold(body: new Center(child: new Text("test")))));

        Assert.Equal(Brightness.Light, lightTheme.ColorScheme.Brightness);
        Assert.Equal(SystemUiOverlayStyle.Dark, SystemChrome.LatestStyle);
    }

    // Flutter: "Material uses the light SystemUIOverlayStyle when the background is dark"
    [Fact]
    public void MaterialUsesTheLightSystemUiOverlayStyleWhenTheBackgroundIsDark()
    {
        using var tester = new FrameworkDartTester();
        ThemeData darkTheme = ThemeData.Dark;
        tester.PumpWidget(
            new MaterialApp(
                theme: darkTheme,
                home: new Scaffold(body: new Center(child: new Text("test")))));

        Assert.Equal(Brightness.Dark, darkTheme.ColorScheme.Brightness);
        Assert.Equal(SystemUiOverlayStyle.Light, SystemChrome.LatestStyle);
    }

    // Flutter: "Surface Tint Overlay" / "applyElevationOverlayColor does not effect anything with
    // useMaterial3 set to true"
    [Fact]
    public void ApplyElevationOverlayColorDoesNotEffectAnythingWithUseMaterial3SetToTrue()
    {
        using var tester = new FrameworkDartTester();
        var surfaceColor = new Color(0xFF121212);
        tester.PumpWidget(
            new Theme(
                data: new ThemeData(
                    applyElevationOverlayColor: true,
                    colorScheme: ColorScheme.Dark().CopyWith(surface: surfaceColor)),
                child: BuildMaterial(color: surfaceColor, elevation: 8.0)));
        RenderPhysicalShape model = GetModel(tester);
        Assert.Equal(surfaceColor, model.Color);
    }

    // Flutter: "Surface Tint Overlay" / "surfaceTintColor is used to as an overlay to indicate elevation"
    [Fact]
    public void SurfaceTintColorIsUsedToAsAnOverlayToIndicateElevation()
    {
        using var tester = new FrameworkDartTester();
        var baseColor = new Color(0xFF121212);
        var surfaceTintColor = new Color(0xff44CCFF);

        // With no surfaceTintColor specified, it should not apply an overlay
        tester.PumpWidget(
            new Theme(
                data: new ThemeData(),
                child: BuildMaterial(color: baseColor, elevation: 12.0)));
        tester.PumpAndSettle();
        RenderPhysicalShape noTintModel = GetModel(tester);
        Assert.Equal(baseColor, noTintModel.Color);

        // With transparent surfaceTintColor, it should not apply an overlay
        tester.PumpWidget(
            new Theme(
                data: new ThemeData(),
                child: BuildMaterial(color: baseColor, surfaceTintColor: MaterialColors.Transparent, elevation: 12.0)));
        tester.PumpAndSettle();
        RenderPhysicalShape transparentTintModel = GetModel(tester);
        Assert.Equal(baseColor, transparentTintModel.Color);

        // With surfaceTintColor specified, it should not apply an overlay based
        // on the elevation.
        tester.PumpWidget(
            new Theme(
                data: new ThemeData(),
                child: BuildMaterial(color: baseColor, surfaceTintColor: surfaceTintColor, elevation: 12.0)));
        tester.PumpAndSettle();
        RenderPhysicalShape tintModel = GetModel(tester);

        // Final color should be the base with a tint of 0.14 opacity or 0xff192c33
        ColorMatchers.AssertSameColorAs(new Color(0xff192c33), tintModel.Color);
    }

    // Flutter: "Elevation Overlay M2" / "applyElevationOverlayColor set to false does not change surface color"
    [Fact]
    public void ApplyElevationOverlayColorSetToFalseDoesNotChangeSurfaceColor()
    {
        using var tester = new FrameworkDartTester();
        var surfaceColor = new Color(0xFF121212);
        tester.PumpWidget(
            new Theme(
                data: new ThemeData(
                    useMaterial3: false,
                    applyElevationOverlayColor: false,
                    colorScheme: ColorScheme.Dark().CopyWith(surface: surfaceColor)),
                child: BuildMaterial(color: surfaceColor, elevation: 8.0)));
        RenderPhysicalShape model = GetModel(tester);
        Assert.Equal(surfaceColor, model.Color);
    }

    // Flutter: "Elevation Overlay M2" / "applyElevationOverlayColor set to true applies a semi-transparent
    // onSurface color to the surface color"
    [Fact]
    public void ApplyElevationOverlayColorSetToTrueAppliesASemiTransparentOnSurfaceColorToTheSurfaceColor()
    {
        using var tester = new FrameworkDartTester();
        var surfaceColor = new Color(0xFF121212);
        Color onSurfaceColor = MaterialColors.GreenAccent;

        // The colors we should get with a base surface color of 0xFF121212 for
        // and a given elevation
        (double Elevation, Color Color)[] elevationColors =
        [
            (0.0, new Color(0xFF121212)),
            (1.0, new Color(0xFF161D19)),
            (2.0, new Color(0xFF18211D)),
            (3.0, new Color(0xFF19241E)),
            (4.0, new Color(0xFF1A2620)),
            (6.0, new Color(0xFF1B2922)),
            (8.0, new Color(0xFF1C2C24)),
            (12.0, new Color(0xFF1D3027)),
            (16.0, new Color(0xFF1E3329)),
            (24.0, new Color(0xFF20362B)),
        ];

        foreach ((double elevation, Color color) in elevationColors)
        {
            tester.PumpWidget(
                new Theme(
                    data: new ThemeData(
                        useMaterial3: false,
                        applyElevationOverlayColor: true,
                        colorScheme: ColorScheme.Dark().CopyWith(surface: surfaceColor, onSurface: onSurfaceColor)),
                    child: BuildMaterial(color: surfaceColor, elevation: elevation)));
            tester.PumpAndSettle(); // wait for the elevation animation to finish
            RenderPhysicalShape model = GetModel(tester);
            ColorMatchers.AssertSameColorAs(color, model.Color);
        }
    }

    // Flutter: "Elevation Overlay M2" / "overlay will not apply to materials using a non-surface color"
    [Fact]
    public void OverlayWillNotApplyToMaterialsUsingANonSurfaceColor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Theme(
                data: new ThemeData(
                    useMaterial3: false,
                    applyElevationOverlayColor: true,
                    colorScheme: ColorScheme.Dark()),
                child: BuildMaterial(color: MaterialColors.Cyan, elevation: 8.0)));
        RenderPhysicalShape model = GetModel(tester);
        // Shouldn't change, as it is not using a ColorScheme.surface color
        Assert.Equal<Color>(MaterialColors.Cyan, model.Color);
    }

    // Flutter: "Elevation Overlay M2" / "overlay will not apply to materials using a light theme"
    [Fact]
    public void OverlayWillNotApplyToMaterialsUsingALightTheme()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Theme(
                data: new ThemeData(
                    useMaterial3: false,
                    applyElevationOverlayColor: true,
                    colorScheme: ColorScheme.Light()),
                child: BuildMaterial(color: MaterialColors.Cyan, elevation: 8.0)));
        RenderPhysicalShape model = GetModel(tester);
        // Shouldn't change, as it was under a light color scheme.
        Assert.Equal<Color>(MaterialColors.Cyan, model.Color);
    }

    // Flutter: "Elevation Overlay M2" / "overlay will apply to materials with a non-opaque surface color"
    [Fact]
    public void OverlayWillApplyToMaterialsWithANonOpaqueSurfaceColor()
    {
        using var tester = new FrameworkDartTester();
        var surfaceColor = new Color(0xFF121212);
        var surfaceColorWithOverlay = new Color(0xC6353535);

        tester.PumpWidget(
            new Theme(
                data: new ThemeData(
                    useMaterial3: false,
                    applyElevationOverlayColor: true,
                    colorScheme: ColorScheme.Dark()),
                child: BuildMaterial(color: surfaceColor.WithOpacity(.75), elevation: 8.0)));

        RenderPhysicalShape model = GetModel(tester);
        ColorMatchers.AssertSameColorAs(surfaceColorWithOverlay, model.Color);
        Assert.False(ColorMatchers.IsSameColorAs(model.Color, surfaceColor));
    }

    // Flutter: "Elevation Overlay M2" / "Expected overlay color can be computed using colorWithOverlay"
    [Fact]
    public void ExpectedOverlayColorCanBeComputedUsingColorWithOverlay()
    {
        using var tester = new FrameworkDartTester();
        var surfaceColor = new Color(0xFF123456);
        var onSurfaceColor = new Color(0xFF654321);
        const double elevation = 8.0;

        Color surfaceColorWithOverlay = ElevationOverlay.ColorWithOverlay(surfaceColor, onSurfaceColor, elevation);

        tester.PumpWidget(
            new Theme(
                data: new ThemeData(
                    useMaterial3: false,
                    applyElevationOverlayColor: true,
                    colorScheme: ColorScheme.Dark(surface: surfaceColor, onSurface: onSurfaceColor)),
                child: BuildMaterial(color: surfaceColor, elevation: elevation)));

        RenderPhysicalShape model = GetModel(tester);
        Assert.Equal(surfaceColorWithOverlay, model.Color);
        Assert.NotEqual(surfaceColor, model.Color);
    }

    // Flutter: "Transparency clipping" / "No clip by default"
    [Fact]
    public void TransparencyClippingNoClipByDefault()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Transparency,
                child: new SizedBox(width: 100.0, height: 100.0)));

        RenderClipPath renderClip = AllRenderObjects(tester.RenderView).OfType<RenderClipPath>().First();
        Assert.Equal(Clip.None, renderClip.ClipBehavior);
    }

    // Flutter: "Transparency clipping" / "clips to bounding rect by default given Clip.antiAlias"
    [Fact]
    public void TransparencyClippingClipsToBoundingRectByDefaultGivenClipAntiAlias()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Transparency,
                clipBehavior: Clip.AntiAlias,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertClipsWithBoundingRect(RootRenderObject(tester, materialKey));
    }

    // Flutter: "Transparency clipping" / "clips to rounded rect when borderRadius provided given Clip.antiAlias"
    [Fact]
    public void TransparencyClippingClipsToRoundedRectWhenBorderRadiusProvidedGivenClipAntiAlias()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Transparency,
                borderRadius: BorderRadius.Circular(10.0),
                clipBehavior: Clip.AntiAlias,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertClipsWithBoundingRRect(RootRenderObject(tester, materialKey), BorderRadius.Circular(10.0));
    }

    // Flutter: "Transparency clipping" / "clips to shape when provided given Clip.antiAlias"
    [Fact]
    public void TransparencyClippingClipsToShapeWhenProvidedGivenClipAntiAlias()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Transparency,
                shape: new StadiumBorder(),
                clipBehavior: Clip.AntiAlias,
                child: new SizedBox(width: 100.0, height: 100.0)));

        RenderObject renderObject = RootRenderObject(tester, materialKey);
        var clipPath = Assert.IsType<RenderClipPath>(renderObject);
        var shapeClipper = Assert.IsType<ShapeBorderClipper>(clipPath.Clipper);
        Assert.Equal<ShapeBorder>(new StadiumBorder(), shapeClipper.Shape);
    }

    // Flutter: "Transparency clipping" / "supports directional clips"
    [Fact]
    public void TransparencyClippingSupportsDirectionalClips()
    {
        using var tester = new FrameworkDartTester();
        var logs = new List<string>();
        ShapeBorder shape = new TestBorder(logs.Add);

        Widget BuildMaterialWidget()
        {
            return new MaterialWidget(
                type: MaterialType.Transparency,
                shape: shape,
                clipBehavior: Clip.AntiAlias,
                child: new SizedBox(width: 100.0, height: 100.0));
        }

        Widget material = BuildMaterialWidget();
        // verify that a regular clip works as one would expect
        logs.Add("--0");
        tester.PumpWidget(material);
        // verify that pumping again doesn't recompute the clip
        // even though the widget itself is new (the shape doesn't change identity)
        logs.Add("--1");
        tester.PumpWidget(BuildMaterialWidget());
        // verify that Material passes the TextDirection on to its shape when it's transparent
        logs.Add("--2");
        tester.PumpWidget(new Directionality(textDirection: TextDirection.Ltr, child: material));
        // verify that changing the text direction from LTR to RTL has an effect
        // even though the widget itself is identical
        logs.Add("--3");
        tester.PumpWidget(new Directionality(textDirection: TextDirection.Rtl, child: material));
        // verify that pumping again with a text direction has no effect
        logs.Add("--4");
        tester.PumpWidget(new Directionality(textDirection: TextDirection.Rtl, child: BuildMaterialWidget()));
        logs.Add("--5");
        // verify that changing the text direction and the widget at the same time
        // works as expected
        tester.PumpWidget(new Directionality(textDirection: TextDirection.Ltr, child: material));
        Assert.Equal(
            [
                "--0",
                "getOuterPath Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) null",
                "paint Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) null",
                "--1",
                "--2",
                "getOuterPath Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) TextDirection.ltr",
                "paint Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) TextDirection.ltr",
                "--3",
                "getOuterPath Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) TextDirection.rtl",
                "paint Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) TextDirection.rtl",
                "--4",
                "--5",
                "getOuterPath Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) TextDirection.ltr",
                "paint Rect.fromLTRB(0.0, 0.0, 800.0, 600.0) TextDirection.ltr",
            ],
            logs);
    }

    // Flutter: "PhysicalModels" / "canvas"
    [Fact]
    public void PhysicalModelsCanvas()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new MaterialWidget(key: materialKey, child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalModel(
            RootRenderObject(tester, materialKey),
            shape: BoxShape.Rectangle,
            borderRadius: BorderRadius.Zero,
            elevation: 0.0);
    }

    // Flutter: "PhysicalModels" / "canvas with borderRadius and elevation"
    [Fact]
    public void PhysicalModelsCanvasWithBorderRadiusAndElevation()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                borderRadius: BorderRadius.Circular(5.0),
                elevation: 1.0,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalModel(
            RootRenderObject(tester, materialKey),
            shape: BoxShape.Rectangle,
            borderRadius: BorderRadius.Circular(5.0),
            elevation: 1.0);
    }

    // Flutter: "PhysicalModels" / "canvas with shape and elevation"
    [Fact]
    public void PhysicalModelsCanvasWithShapeAndElevation()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                shape: new StadiumBorder(),
                elevation: 1.0,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalShape(RootRenderObject(tester, materialKey), new StadiumBorder(), elevation: 1.0);
    }

    // Flutter: "PhysicalModels" / "card"
    [Fact]
    public void PhysicalModelsCard()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Card,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalModel(
            RootRenderObject(tester, materialKey),
            shape: BoxShape.Rectangle,
            borderRadius: BorderRadius.Circular(2.0),
            elevation: 0.0);
    }

    // Flutter: "PhysicalModels" / "card with borderRadius and elevation"
    [Fact]
    public void PhysicalModelsCardWithBorderRadiusAndElevation()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Card,
                borderRadius: BorderRadius.Circular(5.0),
                elevation: 5.0,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalModel(
            RootRenderObject(tester, materialKey),
            shape: BoxShape.Rectangle,
            borderRadius: BorderRadius.Circular(5.0),
            elevation: 5.0);
    }

    // Flutter: "PhysicalModels" / "card with shape and elevation"
    [Fact]
    public void PhysicalModelsCardWithShapeAndElevation()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Card,
                shape: new StadiumBorder(),
                elevation: 5.0,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalShape(RootRenderObject(tester, materialKey), new StadiumBorder(), elevation: 5.0);
    }

    // Flutter: "PhysicalModels" / "circle"
    [Fact]
    public void PhysicalModelsCircle()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Circle,
                color: new Color(0xFF0000FF),
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalModel(RootRenderObject(tester, materialKey), shape: BoxShape.Circle, elevation: 0.0);
    }

    // Flutter: "PhysicalModels" / "button"
    [Fact]
    public void PhysicalModelsButton()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Button,
                color: new Color(0xFF0000FF),
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalModel(
            RootRenderObject(tester, materialKey),
            shape: BoxShape.Rectangle,
            borderRadius: BorderRadius.Circular(2.0),
            elevation: 0.0);
    }

    // Flutter: "PhysicalModels" / "button with elevation and borderRadius"
    [Fact]
    public void PhysicalModelsButtonWithElevationAndBorderRadius()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Button,
                color: new Color(0xFF0000FF),
                borderRadius: BorderRadius.Circular(6.0),
                elevation: 4.0,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalModel(
            RootRenderObject(tester, materialKey),
            shape: BoxShape.Rectangle,
            borderRadius: BorderRadius.Circular(6.0),
            elevation: 4.0);
    }

    // Flutter: "PhysicalModels" / "button with elevation and shape"
    [Fact]
    public void PhysicalModelsButtonWithElevationAndShape()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Button,
                color: new Color(0xFF0000FF),
                shape: new StadiumBorder(),
                elevation: 4.0,
                child: new SizedBox(width: 100.0, height: 100.0)));

        AssertRendersOnPhysicalShape(RootRenderObject(tester, materialKey), new StadiumBorder(), elevation: 4.0);
    }

    // Flutter: "Border painting" / "border is painted on physical layers"
    [Fact]
    public void BorderIsPaintedOnPhysicalLayers()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Button,
                color: new Color(0xFF0000FF),
                shape: new CircleBorder(side: new BorderSide(width: 2.0, color: new Color(0xFF0000FF))),
                child: new SizedBox(width: 100.0, height: 100.0)));

        PaintAssert.Paints(RootRenderObject(tester, materialKey), PaintPattern.Paints.Circle());
    }

    // Flutter: "Border painting" / "border is painted for transparent material"
    [Fact]
    public void BorderIsPaintedForTransparentMaterial()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Transparency,
                shape: new CircleBorder(side: new BorderSide(width: 2.0, color: new Color(0xFF0000FF))),
                child: new SizedBox(width: 100.0, height: 100.0)));

        PaintAssert.Paints(RootRenderObject(tester, materialKey), PaintPattern.Paints.Circle());
    }

    // Flutter: "Border painting" / "border is not painted for when border side is none"
    [Fact]
    public void BorderIsNotPaintedForWhenBorderSideIsNone()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                type: MaterialType.Transparency,
                shape: new CircleBorder(),
                child: new SizedBox(width: 100.0, height: 100.0)));

        PaintAssert.DoesNotPaint(RootRenderObject(tester, materialKey), PaintPattern.Paints.Circle());
    }

    // Flutter: "Border painting" / "Material2 - border is painted above child by default"
    [Fact(Skip = GoldenSkip)]
    public void Material2BorderIsPaintedAboveChildByDefault()
    {
    }

    // Flutter: "Border painting" / "Material3 - border is painted above child by default"
    [Fact(Skip = GoldenSkip)]
    public void Material3BorderIsPaintedAboveChildByDefault()
    {
    }

    // Flutter: "Border painting" / "Material2 - border is painted below child when specified"
    [Fact(Skip = GoldenSkip)]
    public void Material2BorderIsPaintedBelowChildWhenSpecified()
    {
    }

    // Flutter: "Border painting" / "Material3 - border is painted below child when specified"
    [Fact(Skip = GoldenSkip)]
    public void Material3BorderIsPaintedBelowChildWhenSpecified()
    {
    }

    // Flutter: "InkFeature skips painting if intermediate node skips"
    [Fact]
    public void InkFeatureSkipsPaintingIfIntermediateNodeSkips()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey sizedBoxKey = new LabeledGlobalKey<State>(null);
        GlobalKey materialKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                child: new Offstage(child: new SizedBox(key: sizedBoxKey, width: 20, height: 20))));
        MaterialInkController controller = MaterialWidget.Of(sizedBoxKey.CurrentContext!);

        var tracker = new TrackPaintInkFeature(
            controller: controller,
            referenceBox: (RenderBox)sizedBoxKey.CurrentContext!.FindRenderObject()!);
        controller.AddInkFeature(tracker);
        Assert.Equal(0, tracker.PaintCount);

        var layer1 = new ContainerLayer();

        // Force a repaint. Since it's offstage, the ink feature should not get painted.
        materialKey.CurrentContext!.FindRenderObject()!.Paint(new PaintingContext(layer1, LargestRect), default);
        Assert.Equal(0, tracker.PaintCount);

        tester.PumpWidget(
            new MaterialWidget(
                key: materialKey,
                child: new Offstage(offstage: false, child: new SizedBox(key: sizedBoxKey, width: 20, height: 20))));
        // Gets a paint because the global keys have reused the elements and it is
        // now onstage.
        Assert.Equal(1, tracker.PaintCount);

        var layer2 = new ContainerLayer();

        // Force a repaint again. This time, it gets repainted because it is onstage.
        materialKey.CurrentContext!.FindRenderObject()!.Paint(new PaintingContext(layer2, LargestRect), default);
        Assert.Equal(2, tracker.PaintCount);

        tracker.Dispose();
        layer1.Dispose();
        layer2.Dispose();
    }

    // Flutter: "$InkFeature dispatches memory events"
    [Fact]
    public void InkFeatureDispatchesMemoryEvents()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialWidget(child: new SizedBox(width: 20, height: 20)));

        Element element = tester.ElementOfType<SizedBox>();
        MaterialInkController controller = MaterialWidget.Of(element);
        var referenceBox = (RenderBox)element.FindRenderObject()!;

        var events = new List<ObjectEvent>();
        FlutterMemoryAllocations.Instance.AddListener(events.Add);
        TestInkFeature feature;
        try
        {
            feature = new TestInkFeature(controller: controller, referenceBox: referenceBox);
            feature.Dispose();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(events.Add);
        }

        List<ObjectEvent> featureEvents = events.Where(@event => ReferenceEquals(@event.Object, feature)).ToList();
        if (FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            // areCreateAndDispose
            Assert.Collection(
                featureEvents,
                @event => Assert.IsType<ObjectCreated>(@event),
                @event => Assert.IsType<ObjectDisposed>(@event));
            var created = (ObjectCreated)featureEvents[0];
            Assert.Equal("package:flutter/material.dart", created.Library);
            Assert.Equal("InkFeature", created.ClassName);
        }
        else
        {
            Assert.Empty(featureEvents);
        }
    }

    // Flutter: "LookupBoundary" / "hides Material from Material.maybeOf"
    [Fact]
    public void LookupBoundaryHidesMaterialFromMaterialMaybeOf()
    {
        using var tester = new FrameworkDartTester();
        MaterialInkController? material = null;

        tester.PumpWidget(
            new MaterialWidget(
                child: new LookupBoundary(
                    child: new Builder(context =>
                    {
                        material = MaterialWidget.MaybeOf(context);
                        return new Container();
                    }))));

        Assert.Null(material);
    }

    // Flutter: "LookupBoundary" / "hides Material from Material.of"
    [DebugOnlyFact]
    public void LookupBoundaryHidesMaterialFromMaterialOf()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new LookupBoundary(
                    child: new Builder(context =>
                    {
                        MaterialWidget.Of(context);
                        return new Container();
                    }))));
        object? exception = tester.TakeException();
        FlutterError error = Assert.IsType<FlutterError>(exception);

        Assert.Equal(
            "FlutterError\n"
            + "   Material.of() was called with a context that does not have access\n"
            + "   to a Material widget.\n"
            + "   The context provided to Material.of() does have a Material widget\n"
            + "   ancestor, but it is hidden by a LookupBoundary. This can happen\n"
            + "   because you are using a widget that looks for a Material\n"
            + "   ancestor, but no such ancestor exists within the closest\n"
            + "   LookupBoundary.\n"
            + "   The context used was:\n"
            + "     Builder(dirty)\n",
            error.ToStringDeep());
    }

    // Flutter: "LookupBoundary" / "hides Material from debugCheckHasMaterial"
    [DebugOnlyFact]
    public void LookupBoundaryHidesMaterialFromDebugCheckHasMaterial()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new MaterialWidget(
                child: new LookupBoundary(
                    child: new Builder(context =>
                    {
                        MaterialDebug.DebugCheckHasMaterial(context);
                        return new Container();
                    }))));
        object? exception = tester.TakeException();
        FlutterError error = Assert.IsType<FlutterError>(exception);

        Assert.StartsWith(
            "FlutterError\n"
            + "   No Material widget found within the closest LookupBoundary.\n"
            + "   There is an ancestor Material widget, but it is hidden by a\n"
            + "   LookupBoundary.\n"
            + "   Builder widgets require a Material widget ancestor within the\n"
            + "   closest LookupBoundary.\n"
            + "   In Material Design, most widgets are conceptually \"printed\" on a\n"
            + "   sheet of material. In Flutter's material library, that material\n"
            + "   is represented by the Material widget. It is the Material widget\n"
            + "   that renders ink splashes, for instance. Because of this, many\n"
            + "   material library widgets require that there be a Material widget\n"
            + "   in the tree above them.\n"
            + "   To introduce a Material widget, you can either directly include\n"
            + "   one, or use a widget that contains Material itself, such as a\n"
            + "   Card, Dialog, Drawer, or Scaffold.\n"
            + "   The specific widget that could not find a Material ancestor was:\n"
            + "     Builder\n"
            + "   The ancestors of this widget were:\n"
            + "     LookupBoundary\n",
            error.ToStringDeep(),
            StringComparison.Ordinal);
    }

    // Flutter: "Material is not visible from sub-views"
    [Fact]
    public void MaterialIsNotVisibleFromSubViews()
    {
        using var tester = new FrameworkDartTester();
        MaterialInkController? outsideView = null;
        MaterialInkController? insideView = null;
        MaterialInkController? outsideViewAnchor = null;

        tester.PumpWidget(
            new MaterialWidget(
                child: new Builder(context =>
                {
                    outsideViewAnchor = MaterialWidget.MaybeOf(context);
                    return new ViewAnchor(
                        view: new Builder(innerContext =>
                        {
                            outsideView = MaterialWidget.MaybeOf(innerContext);
                            return new View(
                                FakeView,
                                new Builder(viewContext =>
                                {
                                    insideView = MaterialWidget.MaybeOf(viewContext);
                                    return new SizedBox();
                                }));
                        }),
                        child: new SizedBox());
                })));

        Assert.NotNull(outsideViewAnchor);
        Assert.Null(outsideView);
        Assert.Null(insideView);
    }

    // Flutter: "Material does not crash at zero area"
    [Fact]
    public void MaterialDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new Center(child: new SizedBox(child: new MaterialWidget()))));
        Assert.Equal(new Size(0, 0), tester.GetSize(tester.ElementOfType<MaterialWidget>()));
    }

    // A second view for "Material is not visible from sub-views"; flutter_test's `_FakeView` wraps
    // `tester.view` with view id 100.
    private static readonly FlutterView FakeView = new(new Size(800, 600), 1.0, 5100);

    private static readonly Rect LargestRect = new(-1e9, -1e9, 2e9, 2e9);

    private static Widget BuildMaterial(
        double elevation = 0.0,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? color = null)
    {
        return new Center(
            child: new SizedBox(
                width: 100.0,
                height: 100.0,
                child: new MaterialWidget(
                    color: color ?? new Color(0xFF0000FF),
                    shadowColor: shadowColor ?? new Color(0xFF00FF00),
                    surfaceTintColor: surfaceTintColor,
                    elevation: elevation,
                    shape: new CircleBorder())));
    }

    private static RenderPhysicalShape GetModel(FrameworkDartTester tester) =>
        (RenderPhysicalShape)tester.ElementOfType<PhysicalShape>().FindRenderObject()!;

    // flutter_test's `_maxComponentColorDistance`, the `within<Color>` distance.
    private static int MaxComponentColorDistance(Color a, Color b)
    {
        int delta = Math.Max(Math.Abs(a.Red - b.Red), Math.Abs(a.Green - b.Green));
        delta = Math.Max(delta, Math.Abs(a.Blue - b.Blue));
        return Math.Max(delta, Math.Abs(a.Alpha - b.Alpha));
    }

    // `find.byKey(key)` evaluated to its single element's render object (`nodes.single.renderObject`).
    private static RenderObject RootRenderObject(FrameworkDartTester tester, Key key) =>
        Assert.Single(tester.ElementsWithKey(key)).FindRenderObject()!;

    private static List<RenderObject> AllRenderObjects(RenderObject root)
    {
        var result = new List<RenderObject>();

        void Visit(RenderObject node)
        {
            result.Add(node);
            node.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }

    // flutter_test's `rendersOnPhysicalModel` (`_RendersOnPhysicalModel`).
    private static void AssertRendersOnPhysicalModel(
        RenderObject renderObject,
        BoxShape? shape = null,
        BorderRadius? borderRadius = null,
        double? elevation = null)
    {
        if (renderObject.GetType() == typeof(RenderPhysicalModel))
        {
            var model = (RenderPhysicalModel)renderObject;
            if (shape is not null)
            {
                Assert.Equal(shape, model.Shape);
            }

            if (borderRadius is not null)
            {
                Assert.Equal(borderRadius, model.BorderRadius);
            }

            if (elevation is not null)
            {
                Assert.Equal(elevation.Value, model.Elevation);
            }

            return;
        }

        var physicalShape = Assert.IsType<RenderPhysicalShape>(renderObject);
        var shapeClipper = Assert.IsType<ShapeBorderClipper>(physicalShape.Clipper);
        if (borderRadius is not null)
        {
            AssertRoundedRectangle(shapeClipper, borderRadius.Value);
        }

        if (borderRadius is null && shape == BoxShape.Rectangle)
        {
            AssertRoundedRectangle(shapeClipper, BorderRadius.Zero);
        }

        if (borderRadius is null && shape == BoxShape.Circle)
        {
            Assert.IsType<CircleBorder>(shapeClipper.Shape);
        }

        if (elevation is not null)
        {
            Assert.Equal(elevation.Value, physicalShape.Elevation);
        }
    }

    private static void AssertRoundedRectangle(ShapeBorderClipper shapeClipper, BorderRadius borderRadius)
    {
        var border = Assert.IsType<RoundedRectangleBorder>(shapeClipper.Shape);
        Assert.Equal<BorderRadiusGeometry>(borderRadius, border.BorderRadius);
    }

    // flutter_test's `rendersOnPhysicalShape` (`_RendersOnPhysicalShape`).
    private static void AssertRendersOnPhysicalShape(RenderObject renderObject, ShapeBorder shape, double? elevation)
    {
        var physicalShape = Assert.IsType<RenderPhysicalShape>(renderObject);
        var shapeClipper = Assert.IsType<ShapeBorderClipper>(physicalShape.Clipper);
        Assert.Equal(shape, shapeClipper.Shape);
        if (elevation is not null)
        {
            Assert.Equal(elevation.Value, physicalShape.Elevation);
        }
    }

    // flutter_test's `clipsWithBoundingRect` (`_ClipsWithBoundingRect`).
    private static void AssertClipsWithBoundingRect(RenderObject renderObject)
    {
        if (renderObject.GetType() == typeof(RenderClipRect))
        {
            Assert.Null(((RenderClipRect)renderObject).Clipper);
            return;
        }

        var clipPath = Assert.IsType<RenderClipPath>(renderObject);
        var shapeClipper = Assert.IsType<ShapeBorderClipper>(clipPath.Clipper);
        var border = Assert.IsType<RoundedRectangleBorder>(shapeClipper.Shape);
        Assert.Equal<BorderRadiusGeometry>(BorderRadius.Zero, border.BorderRadius);
    }

    // flutter_test's `clipsWithBoundingRRect` (`_ClipsWithBoundingRRect`).
    private static void AssertClipsWithBoundingRRect(RenderObject renderObject, BorderRadius borderRadius)
    {
        if (renderObject.GetType() == typeof(RenderClipRRect))
        {
            var clipRRect = (RenderClipRRect)renderObject;
            Assert.Null(clipRRect.Clipper);
            Assert.Equal<BorderRadiusGeometry>(borderRadius, clipRRect.BorderRadius);
            return;
        }

        var clipPath = Assert.IsType<RenderClipPath>(renderObject);
        var shapeClipper = Assert.IsType<ShapeBorderClipper>(clipPath.Clipper);
        var border = Assert.IsType<RoundedRectangleBorder>(shapeClipper.Shape);
        Assert.Equal<BorderRadiusGeometry>(borderRadius, border.BorderRadius);
    }

    // Dart's `Rect.toString`.
    private static string DescribeRect(Rect rect) => string.Format(
        CultureInfo.InvariantCulture,
        "Rect.fromLTRB({0:0.0}, {1:0.0}, {2:0.0}, {3:0.0})",
        rect.Left,
        rect.Top,
        rect.Right,
        rect.Bottom);

    private static string DescribeTextDirection(TextDirection? textDirection) => textDirection switch
    {
        TextDirection.Ltr => "TextDirection.ltr",
        TextDirection.Rtl => "TextDirection.rtl",
        _ => "null",
    };

    private sealed class NotifyMaterial : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            new LayoutChangedNotification().Dispatch(context);
            return new Container();
        }
    }

    private sealed class PaintRecorder(List<Size> log) : CustomPainter
    {
        public override void Paint(PaintingContext context, Size size)
        {
            log.Add(size);
            context.Canvas.DrawRect(new Rect(new Point(0, 0), size), new Paint { Color = new Color(0xFF0000FF) });
        }

        public override bool ShouldRepaint(CustomPainter oldDelegate) => false;
    }

    // material-ui-src/test/test_border.dart: a ShapeBorder that logs its method calls.
    private sealed record TestBorder(Action<string> OnLog) : ShapeBorder
    {
        public override EdgeInsetsGeometry Dimensions => EdgeInsetsGeometry.DirectionalOnly(start: 1.0);

        public override ShapeBorder Scale(double t) => new TestBorder(OnLog);

        public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
        {
            OnLog($"getInnerPath {DescribeRect(rect)} {DescribeTextDirection(textDirection)}");
            return new Path();
        }

        public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
        {
            OnLog($"getOuterPath {DescribeRect(rect)} {DescribeTextDirection(textDirection)}");
            return new Path();
        }

        public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
        {
            OnLog($"paint {DescribeRect(rect)} {DescribeTextDirection(textDirection)}");
        }
    }

    private sealed class TrackPaintInkFeature(MaterialInkController controller, RenderBox referenceBox)
        : InkFeature(controller, referenceBox)
    {
        public int PaintCount { get; private set; }

        protected override void PaintFeature(Canvas canvas, Matrix4 transform)
        {
            PaintCount += 1;
        }
    }

    private sealed class TestInkFeature : InkFeature
    {
        public TestInkFeature(MaterialInkController controller, RenderBox referenceBox)
            : base(controller, referenceBox)
        {
            controller.AddInkFeature(this);
        }

        protected override void PaintFeature(Canvas canvas, Matrix4 transform)
        {
        }
    }
}
