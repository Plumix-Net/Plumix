// Dart parity source: material_ui/lib/src/stepper.dart
// Mirrors material-ui-src/test/stepper_test.dart, plus C#-only checks of the composition the
// sample screen comparison depends on.

using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialStepperTests : IDisposable
{
    private readonly bool _previousDisableShadows = RenderingDebug.DisableShadows;

    public MaterialStepperTests()
    {
        // flutter_test runs with debugDisableShadows == true.
        RenderingDebug.DisableShadows = true;
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        RenderingDebug.DisableShadows = _previousDisableShadows;
    }

    // Flutter: "Material3 has sentence case labels"
    [Fact]
    public void Material3HasSentenceCaseLabels()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                onStepTapped: _ => { },
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0)),
                    new Step(new Text("Step 2"), new SizedBox(width: 100.0, height: 100.0)),
                ]))));
        Assert.True(tester.Any(Find.Text("Continue")));
        Assert.True(tester.Any(Find.Text("Cancel")));
    }

    // Flutter: "Stepper tap callback test"
    [Fact]
    public void StepperTapCallbackTest()
    {
        int index = 0;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                onStepTapped: i => index = i,
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0)),
                    new Step(new Text("Step 2"), new SizedBox(width: 100.0, height: 100.0)),
                ])))));
        tester.Tap(Find.Text("Step 2"));
        Assert.Equal(1, index);
    }

    // Flutter: "Stepper expansion test"
    [Fact]
    public void StepperExpansionTest()
    {
        static Widget Build(int currentStep) => new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                currentStep: currentStep,
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0)),
                    new Step(new Text("Step 2"), new SizedBox(width: 200.0, height: 200.0)),
                ]))));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Build(0));
        Assert.Equal(332.0, tester.GetSize(Find.ByType<Stepper>()).Height);

        tester.PumpWidget(Build(1));
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.True(tester.GetSize(Find.ByType<Stepper>()).Height > 332.0);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(432.0, tester.GetSize(Find.ByType<Stepper>()).Height);
    }

    // Flutter: "Stepper horizontal size test" (the first one)
    [Fact]
    public void StepperHorizontalSizeTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                type: StepperType.Horizontal,
                steps: [new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0))])))));
        Assert.Equal(600.0, tester.GetSize(Find.ByType<Stepper>()).Height);
    }

    // Flutter: "Stepper visibility test"
    [Fact]
    public void StepperVisibilityTest()
    {
        static Widget Build(int currentStep) => new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                type: StepperType.Horizontal,
                currentStep: currentStep,
                steps:
                [
                    new Step(new Text("Step 1"), new Text("A")),
                    new Step(new Text("Step 2"), new Text("B")),
                ]))));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Build(0));
        Assert.True(tester.Any(Find.Text("A")));
        Assert.False(tester.Any(Find.Text("B")));

        tester.PumpWidget(Build(1));
        Assert.False(tester.Any(Find.Text("A")));
        Assert.True(tester.Any(Find.Text("B")));
    }

    // Flutter: "Material2 - Stepper button test" and "Material3 - Stepper button test"
    [Theory]
    [InlineData(false, "CONTINUE", "CANCEL")]
    [InlineData(true, "Continue", "Cancel")]
    public void StepperButtonTest(bool useMaterial3, string continueLabel, string cancelLabel)
    {
        bool continuePressed = false;
        bool cancelPressed = false;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: useMaterial3),
            home: new Center(child: new MaterialWidget(child: new Stepper(
                type: StepperType.Horizontal,
                onStepContinue: () => continuePressed = true,
                onStepCancel: () => cancelPressed = true,
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0)),
                    new Step(new Text("Step 2"), new SizedBox(width: 200.0, height: 200.0)),
                ])))));

        tester.Tap(Find.Text(continueLabel));
        tester.Tap(Find.Text(cancelLabel));
        Assert.True(continuePressed);
        Assert.True(cancelPressed);
    }

    // Flutter: "Stepper disabled step test"
    [Fact]
    public void StepperDisabledStepTest()
    {
        int index = 0;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                onStepTapped: i => index = i,
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0)),
                    new Step(
                        new Text("Step 2"),
                        new SizedBox(width: 100.0, height: 100.0),
                        state: StepState.Disabled),
                ])))));
        tester.Tap(Find.Text("Step 2"));
        Assert.Equal(0, index);
    }

    // Flutter: "Stepper scroll test"
    [Fact]
    public void StepperScrollTest()
    {
        static Widget Build(int currentStep) => new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                currentStep: currentStep,
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 300.0)),
                    new Step(new Text("Step 2"), new SizedBox(width: 100.0, height: 300.0)),
                    new Step(new Text("Step 3"), new SizedBox(width: 100.0, height: 100.0)),
                ]))));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Build(0));
        ScrollableState scrollableState = tester.FirstState<ScrollableState>(Find.ByType<Scrollable>());
        Assert.Equal(0.0, scrollableState.Position.Pixels);

        tester.Tap(Find.Text("Step 3"));
        tester.PumpWidget(Build(2));
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.True(scrollableState.Position.Pixels > 0.0);
    }

    // Flutter: "Stepper index test"
    [Fact]
    public void StepperIndexTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                steps:
                [
                    new Step(
                        new Text("Step 1"),
                        new SizedBox(width: 100.0, height: 100.0),
                        state: StepState.Complete),
                    new Step(new Text("Step 2"), new SizedBox(width: 100.0, height: 100.0)),
                ])))));
        Assert.False(tester.Any(Find.Text("1")));
        Assert.True(tester.Any(Find.Text("2")));
    }

    // Flutter: "Stepper custom controls test"
    [Fact]
    public void StepperCustomControlsTest()
    {
        bool continuePressed = false;
        bool canceledPressed = false;
        Widget Builder(BuildContext context, ControlsDetails details) => new Container(
            margin: new Thickness(0, 16, 0, 0),
            child: new ConstrainedBox(
                constraints: BoxConstraints.TightFor(height: 48.0),
                child: new Row(children:
                [
                    new TextButton(onPressed: details.OnStepContinue, child: new Text("Let us continue!")),
                    new Container(
                        margin: EdgeInsetsDirectional.Only(start: 8.0),
                        child: new TextButton(
                            onPressed: details.OnStepCancel,
                            child: new Text("Cancel This!"))),
                ])));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                controlsBuilder: Builder,
                onStepContinue: () => continuePressed = true,
                onStepCancel: () => canceledPressed = true,
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0)),
                    new Step(new Text("Step 2"), new SizedBox(width: 200.0, height: 200.0)),
                ])))));

        // 2 because stepper creates a set of controls for each step
        Assert.Equal(2, Find.Text("Let us continue!").Count);
        Assert.Equal(2, Find.Text("Cancel This!").Count);

        tester.Tap(Find.Text("Cancel This!").First);
        tester.Pump();
        tester.Tap(Find.Text("Let us continue!").First);
        tester.Pump();
        Assert.True(canceledPressed);
        Assert.True(continuePressed);
    }

    // Flutter: "Stepper custom indexed controls test"
    [Fact]
    public void StepperCustomIndexedControlsTest()
    {
        int currentStep = 0;
        Widget Builder(BuildContext context, ControlsDetails details) => new Container(
            margin: new Thickness(0, 16, 0, 0),
            child: new ConstrainedBox(
                constraints: BoxConstraints.TightFor(height: 48.0),
                child: new Row(children:
                [
                    new TextButton(
                        onPressed: details.OnStepCancel,
                        child: new Text($"Return to {details.StepIndex - 1}")),
                    new Container(
                        margin: EdgeInsetsDirectional.Only(start: 8.0),
                        child: new TextButton(
                            onPressed: details.OnStepContinue,
                            child: new Text($"Continue to {details.StepIndex + 1}"))),
                ])));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new StatefulBuilder((_, setState) => new Stepper(
                controlsBuilder: Builder,
                currentStep: currentStep,
                onStepContinue: () => setState(() => currentStep++),
                onStepCancel: () => setState(() => currentStep--),
                steps:
                [
                    new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 100.0)),
                    new Step(new Text("Step 2"), new SizedBox(width: 200.0, height: 200.0)),
                ]))))));

        tester.Tap(Find.Text("Continue to 1").First);
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.Text("Return to 0")));
        Assert.True(tester.Any(Find.Text("Continue to 2")));
        Assert.Equal(1, currentStep);

        tester.Tap(Find.Text("Return to 0").First);
        tester.PumpAndSettle();
        Assert.Equal(0, currentStep);
    }

    // Flutter: "Stepper error test"
    [Fact]
    public void StepperErrorTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(child: new MaterialWidget(child: new Stepper(
                steps:
                [
                    new Step(
                        new Text("Step 1"),
                        new SizedBox(width: 100.0, height: 100.0),
                        state: StepState.Error),
                ])))));
        Assert.Equal(1, Find.Text("!").Count);
    }

    // Flutter: "Nested stepper error test"
    [DebugOnlyFact]
    public void NestedStepperErrorTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                type: StepperType.Horizontal,
                steps:
                [
                    new Step(
                        new Text("Step 2"),
                        new Stepper(steps: [new Step(new Text("Nested step 1"), new Text("A"))])),
                    new Step(new Text("Step 1"), new Text("A")),
                ]))));
        var error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal("Steppers must not be nested.", error.Diagnostics[0].ToString());
        Assert.Contains(
            "https://material.io/archive/guidelines/components/steppers.html#steppers-usage",
            error.ToString());
    }

    // Flutter: "Stepper icons size test"
    [Fact]
    public void StepperIconsSizeTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                steps:
                [
                    new Step(new Text("A"), new SizedBox(), state: StepState.Editing),
                    new Step(new Text("B"), new SizedBox(), state: StepState.Complete),
                ]))));

        Assert.Equal(new Size(18.0, 18.0), tester.GetSize(Find.ByIcon(Icons.Edit)));
        Assert.Equal(new Size(18.0, 18.0), tester.GetSize(Find.ByIcon(Icons.Check)));
    }

    // Flutter: "Vertical Stepper can't be focused when disabled." and the horizontal twin.
    [Theory]
    [InlineData(StepperType.Vertical)]
    [InlineData(StepperType.Horizontal)]
    public void StepperCantBeFocusedWhenDisabled(StepperType type)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                type: type,
                steps:
                [
                    new Step(
                        new Text("Step 0"),
                        new Text("Text 0"),
                        state: StepState.Disabled),
                ]))));
        tester.Pump();

        FocusNode disabledNode = Focus.Of(tester.Element(Find.Text("Step 0")), scopeOk: true);
        disabledNode.RequestFocus();
        tester.Pump();
        Assert.False(disabledNode.HasPrimaryFocus);
    }

    // Flutter: "Stepper header title should not overflow" and "... subtitle should not overflow"
    [Fact]
    public void StepperHeaderTextShouldNotOverflow()
    {
        string longText = string.Concat(Enumerable.Repeat("A", 100));
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                steps:
                [
                    new Step(new Text(longText, maxLines: 1), new SizedBox()),
                    new Step(
                        new Text("Regular title"),
                        new SizedBox(),
                        subtitle: new Text(longText, maxLines: 1)),
                ]))));
        Assert.Null(tester.TakeException());
    }

    // Flutter: "Material2 - Stepper enabled button styles"
    [Fact]
    public void Material2StepperEnabledButtonStyles()
    {
        using var tester = new FrameworkDartTester();
        var buttonShape = new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(2)));
        var continueButtonRect = new Rect(new Point(24.0, 212.0), new Point(168.0, 260.0));
        var cancelButtonRect = new Rect(new Point(176.0, 212.0), new Point(292.0, 260.0));

        tester.PumpWidget(BuildButtonFrame(new ThemeData(useMaterial3: false)));
        MaterialWidget continueMaterial = ButtonMaterial(tester, "CONTINUE");
        Assert.Equal(0xff2196f3u, continueMaterial.Color!.Value);
        Assert.Equal(new Color(0xffffffff), continueMaterial.TextStyle!.Color);
        Assert.Equal(buttonShape, continueMaterial.Shape);
        Assert.Equal(continueButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("CONTINUE")));

        MaterialWidget cancelMaterial = ButtonMaterial(tester, "CANCEL");
        Assert.Equal(0u, cancelMaterial.Color!.Value);
        Assert.Equal(new Color(0x8a000000), cancelMaterial.TextStyle!.Color);
        Assert.Equal(buttonShape, cancelMaterial.Shape);
        Assert.Equal(cancelButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("CANCEL")));

        tester.PumpWidget(BuildButtonFrame(new ThemeData(useMaterial3: false, brightness: Brightness.Dark)));
        tester.PumpAndSettle(); // Complete the theme animation.

        Assert.Equal(0u, ButtonMaterial(tester, "CONTINUE").Color!.Value);
        Assert.Equal(new Color(0xffffffff), ButtonMaterial(tester, "CONTINUE").TextStyle!.Color);
        Assert.Equal(continueButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("CONTINUE")));
        Assert.Equal(0u, ButtonMaterial(tester, "CANCEL").Color!.Value);
        Assert.Equal(new Color(0xb3ffffff), ButtonMaterial(tester, "CANCEL").TextStyle!.Color);
        Assert.Equal(cancelButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("CANCEL")));
    }

    // Flutter: "Material3 - Stepper enabled button styles"
    [Fact]
    public void Material3StepperEnabledButtonStyles()
    {
        using var tester = new FrameworkDartTester();
        var buttonShape = new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(2)));
        var continueButtonRect = new Rect(new Point(24.0, 212.0), new Point(168.8, 260.0));
        var cancelButtonRect = new Rect(new Point(176.8, 212.0), new Point(293.4, 260.0));

        var themeLight = new ThemeData();
        tester.PumpWidget(BuildButtonFrame(themeLight));
        Assert.Equal(themeLight.ColorScheme.Primary, ButtonMaterial(tester, "Continue").Color);
        Assert.Equal(new Color(0xffffffff), ButtonMaterial(tester, "Continue").TextStyle!.Color);
        Assert.Equal(buttonShape, ButtonMaterial(tester, "Continue").Shape);
        AssertRectMoreOrLessEquals(continueButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("Continue")));
        Assert.Equal(0u, ButtonMaterial(tester, "Cancel").Color!.Value);
        Assert.Equal(new Color(0x8a000000), ButtonMaterial(tester, "Cancel").TextStyle!.Color);
        Assert.Equal(buttonShape, ButtonMaterial(tester, "Cancel").Shape);
        AssertRectMoreOrLessEquals(cancelButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("Cancel")));

        ThemeData themeDark = ThemeData.Dark;
        tester.PumpWidget(BuildButtonFrame(themeDark));
        tester.PumpAndSettle(); // Complete the theme animation.
        Assert.Equal(0u, ButtonMaterial(tester, "Continue").Color!.Value);
        Assert.Equal(themeDark.ColorScheme.OnSurface, ButtonMaterial(tester, "Continue").TextStyle!.Color);
        AssertRectMoreOrLessEquals(continueButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("Continue")));
        Assert.Equal(0u, ButtonMaterial(tester, "Cancel").Color!.Value);
        Assert.Equal(new Color(0xb3ffffff), ButtonMaterial(tester, "Cancel").TextStyle!.Color);
        AssertRectMoreOrLessEquals(cancelButtonRect, tester.GetRect(Find.WidgetWithText<TextButton>("Cancel")));
    }

    // Flutter: "Material2 - Stepper disabled button styles" and "Material3 - ..."
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void StepperDisabledButtonStyles(bool useMaterial3, bool dark)
    {
        var theme = new ThemeData(
            useMaterial3: useMaterial3,
            brightness: dark ? Brightness.Dark : Brightness.Light);
        string continueLabel = useMaterial3 ? "Continue" : "CONTINUE";
        string cancelLabel = useMaterial3 ? "Cancel" : "CANCEL";
        Color disabledText = useMaterial3
            ? theme.ColorScheme.OnSurface.WithOpacity(0.38)
            : dark ? new Color(0x61ffffff) : new Color(0x61000000);

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            theme: theme,
            home: new MaterialWidget(child: new Stepper(
                type: StepperType.Horizontal,
                steps: [new Step(new Text("step1"), new SizedBox(width: 100, height: 100))]))));
        tester.PumpAndSettle();

        Assert.Equal(0u, ButtonMaterial(tester, continueLabel).Color!.Value);
        Assert.Equal(disabledText, ButtonMaterial(tester, continueLabel).TextStyle!.Color);
        Assert.Equal(0u, ButtonMaterial(tester, cancelLabel).Color!.Value);
        Assert.Equal(disabledText, ButtonMaterial(tester, cancelLabel).TextStyle!.Color);
    }

    // Flutter: "Vertical and Horizontal Stepper physics test" and
    // "ScrollController is passed to the stepper listview"
    [Theory]
    [InlineData(StepperType.Vertical)]
    [InlineData(StepperType.Horizontal)]
    public void StepperPassesPhysicsAndControllerToListView(StepperType type)
    {
        var physics = new NeverScrollableScrollPhysics();
        var controller = new ScrollController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                type: type,
                physics: physics,
                controller: controller,
                steps: [new Step(new Text("Step 1"), new Text("Text 1"))]))));

        ListView listView = tester.Widget<ListView>(
            Find.Descendant(of: Find.ByType<Stepper>(), matching: Find.ByType<ListView>()));
        Assert.Same(physics, listView.Physics);
        Assert.Same(controller, listView.Controller);
    }

    // Flutter: "Stepper horizontal size test" (the second one, flutter/flutter#77732)
    [Fact]
    public void StepperCircleFillColorReadsColorScheme()
    {
        Widget BuildFrame(bool isActive, Brightness brightness) => new MaterialApp(
            theme: brightness == Brightness.Dark ? ThemeData.Dark : new ThemeData(),
            home: new Scaffold(body: new Center(child: new Stepper(
                type: StepperType.Horizontal,
                steps: [new Step(new Text("step"), new Text("content"), isActive: isActive)]))));

        using var tester = new FrameworkDartTester();
        Color? CircleFillColor() =>
            (tester.Widget<AnimatedContainer>(Find.WidgetWithText<AnimatedContainer>("1")).Decoration
                as BoxDecoration)?.Color;

        ColorScheme light = new ThemeData().ColorScheme;
        tester.PumpWidget(BuildFrame(true, Brightness.Light));
        Assert.Equal(light.Primary, CircleFillColor());
        tester.PumpWidget(BuildFrame(false, Brightness.Light));
        tester.PumpAndSettle();
        Assert.Equal(light.OnSurface.WithOpacity(0.38), CircleFillColor());

        ColorScheme dark = ThemeData.Dark.ColorScheme;
        tester.PumpWidget(BuildFrame(true, Brightness.Dark));
        tester.PumpAndSettle();
        Assert.Equal(dark.Secondary, CircleFillColor());
        tester.PumpWidget(BuildFrame(false, Brightness.Dark));
        tester.PumpAndSettle();
        Assert.Equal(dark.Background, CircleFillColor());
    }

    // Flutter: "Stepper custom elevation" and "Stepper with default elevation"
    [Theory]
    [InlineData(4.0, 4.0)]
    [InlineData(null, 2.0)]
    public void StepperElevation(double? elevation, double expected)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new SizedBox(
                width: 200,
                height: 75,
                child: new Stepper(
                    type: StepperType.Horizontal,
                    elevation: elevation,
                    steps:
                    [
                        new Step(new Text("Regular title"), new Text("Text content")),
                    ])))));

        MaterialWidget material = tester.FirstWidget<MaterialWidget>(
            Find.Descendant(of: Find.ByType<Stepper>(), matching: Find.ByType<MaterialWidget>()));
        Assert.Equal(expected, material.Elevation);
    }

    // Flutter: "Stepper horizontal preserves state"
    [Fact]
    public void StepperHorizontalPreservesState()
    {
        Color untappedColor = Colors.Blue;
        Color tappedColor = Colors.Red;
        int index = 0;
        Widget BuildFrame() => new MaterialApp(
            home: new Scaffold(body: new Center(child: new StatefulBuilder((_, setState) => new Stepper(
                type: StepperType.Horizontal,
                currentStep: index,
                onStepTapped: i => setState(() => index = i),
                steps:
                [
                    new Step(
                        new Text("Step 1"),
                        new TappableColorWidget(tappedColor: tappedColor, untappedColor: untappedColor)),
                    new Step(new Text("Step 2"), new SizedBox()),
                ])))));

        using var tester = new FrameworkDartTester();
        Color? ContainerColor() => tester.Widget<Container>(Find.ByKey(new ValueKey<string>("tap-me"))).Color;

        tester.PumpWidget(BuildFrame());
        Assert.Equal(untappedColor, ContainerColor());
        tester.Tap(Find.ByKey(new ValueKey<string>("tap-me")));
        tester.PumpAndSettle();
        Assert.Equal(tappedColor, ContainerColor());

        tester.Tap(Find.Text("Step 2"));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("Step 1"));
        tester.PumpAndSettle();
        Assert.Equal(tappedColor, ContainerColor());
    }

    // Flutter: "Stepper with Alternative Label"
    [Fact]
    public void StepperWithAlternativeLabel()
    {
        int index = 0;
        var labelStyle = new TextStyle(FontSize: 12.0, Color: Colors.Black);
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(body: new Center(child: new StatefulBuilder((_, setState) => new Stepper(
                type: StepperType.Horizontal,
                currentStep: index,
                onStepTapped: i => setState(() => index = i),
                steps:
                [
                    new Step(new Text("Title 1"), new Text("A"), label: new Text("Label 1", style: labelStyle)),
                    new Step(new Text("Title 2"), new Text("B"), label: new Text("Label 2", style: labelStyle)),
                ]))))));

        Assert.Same(labelStyle, tester.Widget<Text>(Find.Text("Label 1")).Style);
        Assert.Same(labelStyle, tester.Widget<Text>(Find.Text("Label 2")).Style);
        tester.Tap(Find.Text("Label 2"));
        tester.PumpAndSettle();
        Assert.Equal(1, index);
    }

    // Flutter: "Stepper Connector Style"
    [Fact]
    public void StepperConnectorStyle()
    {
        Color selectedColor = Colors.Black;
        Color disabledColor = Colors.White;
        int index = 0;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(body: new Center(child: new StatefulBuilder((_, setState) => new Stepper(
                type: StepperType.Horizontal,
                connectorColor: WidgetStateProperty<Color>.ResolveWith(states =>
                    states.Contains(WidgetState.Selected) ? selectedColor : disabledColor),
                onStepTapped: i => setState(() => index = i),
                currentStep: index,
                steps:
                [
                    new Step(new Text("step1"), new Text("step1 content"), isActive: index >= 0),
                    new Step(new Text("step2"), new Text("step2 content"), isActive: index >= 1),
                ]))))));

        Color? CircleColor(string circleText) =>
            (tester.Widget<AnimatedContainer>(Find.WidgetWithText<AnimatedContainer>(circleText)).Decoration
                as BoxDecoration)?.Color;
        Color LineColor() => tester.Widget<ColoredBox>(
            Find.Descendant(of: Find.ByType<Stepper>(), matching: Find.ByType<ColoredBox>())).Color;

        Assert.True(tester.Any(Find.Text("step1 content")));
        Assert.False(tester.Any(Find.Text("step2 content")));
        Assert.Equal(selectedColor, CircleColor("1"));
        Assert.Equal(disabledColor, CircleColor("2"));
        // in two steps case there will be single line
        Assert.Equal(selectedColor, LineColor());

        tester.Tap(Find.Text("step2"));
        tester.PumpAndSettle();
        Assert.False(tester.Any(Find.Text("step1 content")));
        Assert.True(tester.Any(Find.Text("step2 content")));
        Assert.Equal(selectedColor, CircleColor("1"));
        Assert.Equal(selectedColor, CircleColor("2"));
        Assert.Equal(selectedColor, LineColor());
    }

    // Flutter: "Stepper stepIconBuilder test"; a Text stands in for FlutterLogo, which Plumix lacks.
    [Fact]
    public void StepperStepIconBuilderTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                stepIconBuilder: (_, state) => state == StepState.Complete ? new Text("logo") : null,
                steps:
                [
                    new Step(new Text("A"), new SizedBox(), state: StepState.Complete),
                    new Step(new Text("B"), new SizedBox(), state: StepState.Editing),
                    new Step(new Text("C"), new SizedBox(), state: StepState.Error),
                ]))));

        // Check if the custom icon is shown for the complete step.
        Assert.True(tester.Any(Find.Text("logo")));
        // Check if the default icons are shown for the editing and error steps.
        Assert.True(tester.Any(Find.ByIcon(Icons.Edit)));
        Assert.True(tester.Any(Find.Text("!")));
    }

    // Flutter: "StepperProperties test", "Stepper custom margin" and "StepStyle test"
    [Fact]
    public void StepperAndStepStylePropertiesTest()
    {
        var stepper = new Stepper(
            stepIconHeight: 24,
            stepIconWidth: 24,
            stepIconMargin: EdgeInsets.All(8),
            margin: EdgeInsets.All(16),
            steps: [new Step(new Text("Regular title"), new Text("Text 1"))]);
        Assert.Equal(24, stepper.StepIconHeight);
        Assert.Equal(24, stepper.StepIconWidth);
        Assert.Equal(EdgeInsets.All(8), stepper.StepIconMargin);
        Assert.Equal((EdgeInsetsGeometry)EdgeInsets.All(16), stepper.Margin);

        var style = new StepStyle(
            Color: Colors.White,
            ErrorColor: Colors.Orange,
            ConnectorColor: Colors.Red,
            ConnectorThickness: 2,
            Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Colors.Blue)),
            Gradient: new LinearGradient([Colors.Red, Colors.Blue]),
            IndexStyle: new TextStyle(Color: Colors.Black));
        Assert.Equal(Colors.White, style.Color);
        Assert.Equal(Colors.Orange, style.ErrorColor);

        StepStyle copied = style.CopyWith(color: Colors.Black, errorColor: Colors.Red);
        Assert.Equal(Colors.Black, copied.Color);
        Assert.Equal(Colors.Red, copied.ErrorColor);

        StepStyle merged = style.Merge(new StepStyle(ConnectorColor: Colors.Blue, ConnectorThickness: 3));
        Assert.Equal(Colors.Blue, merged.ConnectorColor);
        Assert.Equal(3, merged.ConnectorThickness);
        Assert.Same(style, style.Merge(null));
    }

    [Fact]
    public void StepperConstructorChecksMatchFlutterAsserts()
    {
        IReadOnlyList<Step> steps = [new Step(new Text("One"), new Text("Body"))];
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper(steps, currentStep: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper(steps, stepIconWidth: 23));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper(steps, stepIconHeight: 81));
        Assert.Throws<ArgumentException>(() => new Stepper(steps, stepIconWidth: 32, stepIconHeight: 40));
        Assert.Equal(StepperType.Vertical, new Stepper(steps).Type);
        Assert.Equal(Clip.None, new Stepper(steps).ClipBehavior);
    }

    // Flutter: "Vertical Stepper does not draw connector on the last step"
    [Fact]
    public void VerticalStepperDoesNotDrawConnectorOnTheLastStep()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(body: new Center(child: new Stepper(
                currentStep: 1,
                steps:
                [
                    new Step(new Text("step1"), new Text("step1 content")),
                    new Step(new Text("step2"), new Text("step2 content")),
                ])))));

        var lastConnector = (SizedBox)tester.Widget<Center>(Find.Descendant(
            of: Find.ByType<PositionedDirectional>(),
            matching: Find.ByType<Center>().Last)).Child!;
        Assert.Equal(0.0, lastConnector.Width);
    }

    // Flutter: "Default Stepper clipBehavior" and "Stepper steps can be clipped"
    [Theory]
    [InlineData(StepperType.Vertical, null, Clip.None)]
    [InlineData(StepperType.Horizontal, null, Clip.None)]
    [InlineData(StepperType.Vertical, Clip.HardEdge, Clip.HardEdge)]
    [InlineData(StepperType.Horizontal, Clip.HardEdge, Clip.HardEdge)]
    public void StepperContentClipBehavior(StepperType type, Clip? clipBehavior, Clip expected)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(body: new Center(child: new Stepper(
                type: type,
                clipBehavior: clipBehavior ?? Clip.None,
                steps:
                [
                    new Step(new Text("step1"), new Text("step1 content")),
                    new Step(new Text("step2"), new Text("step2 content")),
                ])))));

        ClipRect clipRect = tester.FirstWidget<ClipRect>(
            Find.Ancestor(of: Find.Text("step1 content"), matching: Find.ByType<ClipRect>()));
        Assert.Equal(expected, clipRect.ClipBehavior);
    }

    // Flutter: "Vertical stepper border displays correctly" (flutter/flutter#160156)
    [Fact]
    public void VerticalStepperBorderDisplaysCorrectly()
    {
        int index = 0;
        var connectorColor = new Color(0xff00ffff);
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(body: new Center(child: new StatefulBuilder((_, setState) => new Stepper(
                currentStep: index,
                connectorColor: WidgetStateProperty<Color>.All(connectorColor),
                onStepTapped: value => setState(() => index = value),
                steps:
                [
                    new Step(new Text("step1"), new Text("step1 content")),
                    new Step(new Text("step2"), new Text("step2 content")),
                ]))))));

        Finder findConnector = Find.Descendant(
            of: Find.ByType<Stepper>(),
            matching: Find.Descendant(
                of: Find.ByType<PositionedDirectional>(),
                matching: Find.ByElementPredicate(element =>
                    element.Widget is ColoredBox box
                    && box.Color == connectorColor
                    && element.FindRenderObject() is RenderBox { Size: { Width: 1.0, Height: > 0 } })));

        void VerifyConnector()
        {
            Assert.Equal(1, findConnector.Count);
            if (!Constants.KDebugMode)
            {
                return; // Canvas call recording, which backs `paints`, only exists in debug builds.
            }
            Assert.Contains(
                tester.RecordPaint(findConnector),
                call => call.Method == "drawRect" && call.Color == connectorColor);
        }

        VerifyConnector();
        tester.Tap(Find.Text("step2"));

        const int checkCount = 5;
        TimeSpan duration = TimeSpan.FromTicks(MaterialConstants.ThemeAnimationDuration.Ticks / (checkCount + 1));
        for (int i = 0; i < checkCount; i++)
        {
            tester.Pump(duration);
            VerifyConnector();
        }
    }

    // Flutter: "Vertical stepper active step has fully colored connector line"
    [Fact]
    public void VerticalStepperActiveStepHasFullyColoredConnectorLine()
    {
        var activeColor = new Color(0xFF2196F3);
        var inactiveColor = new Color(0xFF9E9E9E);
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(body: new Center(child: new Stepper(
                controlsBuilder: (_, _) => SizedBox.Shrink(),
                connectorThickness: 3,
                connectorColor: WidgetStateProperty<Color>.ResolveWith(states =>
                    states.Contains(WidgetState.Selected) ? activeColor : inactiveColor),
                steps:
                [
                    new Step(new Text("step1"), new Text("step1 content"), isActive: true),
                    new Step(new Text("step2"), new Text("step2 content")),
                ])))));

        Finder connectorLines = Find.ByWidgetPredicate(widget =>
            widget is ColoredBox { Child: SizedBox { Width: 3.0 } });
        Assert.True(connectorLines.Count > 0);

        List<Color> colors = tester.WidgetList<ColoredBox>(connectorLines).Select(box => box.Color).ToList();
        // Both top and bottom box should be colored.
        Assert.Equal(2, colors.Count(color => color == activeColor));
        Assert.Equal(activeColor, colors[0]);
        Assert.Equal(activeColor, colors[^1]);
    }

    // Flutter: "Stepper does not crash at zero area"
    [Theory]
    [InlineData(StepperType.Vertical)]
    [InlineData(StepperType.Horizontal)]
    public void StepperDoesNotCrashAtZeroArea(StepperType type)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(body: new Center(child: SizedBox.Shrink(child: new Stepper(
                type: type,
                steps:
                [
                    new Step(new Text("X"), new Text("X")),
                    new Step(new Text("Y"), new Text("Y")),
                ]))))));
        Assert.Equal(new Size(0, 0), tester.GetSize(Find.ByType<Stepper>()));
    }

    // Flutter: "Stepper custom headerPadding for vertical stepper" and the horizontal twin.
    [Theory]
    [InlineData(StepperType.Vertical, 12.0, 12.0)]
    [InlineData(StepperType.Horizontal, 16.0, 8.0)]
    public void StepperCustomHeaderPadding(StepperType type, double horizontal, double expectedShift)
    {
        Widget Build(EdgeInsetsGeometry? headerPadding) => new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                type: type,
                headerPadding: headerPadding,
                steps: [new Step(new Text("Step 1"), new Text("Content 1"))])));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Build(null));
        Rect defaultTitleRect = tester.GetRect(Find.Text("Step 1"));
        tester.PumpWidget(Build(EdgeInsets.Symmetric(horizontal: horizontal, vertical: 8.0)));
        Rect customTitleRect = tester.GetRect(Find.Text("Step 1"));

        Assert.Equal(expectedShift, defaultTitleRect.Left - customTitleRect.Left, 6);
        Assert.Equal(24.0, defaultTitleRect.Top);
        Assert.Equal(32.0, customTitleRect.Top);
    }

    // Flutter: "Stepper custom contentPadding for horizontal stepper" and
    // "Stepper default contentPadding for horizontal stepper"
    [Theory]
    [InlineData(16.0, 16.0)]
    [InlineData(null, 24.0)]
    public void StepperHorizontalContentPadding(double? padding, double expected)
    {
        EdgeInsetsGeometry? customPadding = padding is { } value ? EdgeInsets.All(value) : null;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                type: StepperType.Horizontal,
                contentPadding: customPadding,
                steps: [new Step(new Text("Step 1"), new Text("Content 1"))]))));

        Assert.Equal(customPadding, tester.Widget<Stepper>(Find.ByType<Stepper>()).ContentPadding);
        ListView listView = tester.Widget<ListView>(
            Find.Descendant(of: Find.ByType<Stepper>(), matching: Find.ByType<ListView>()));
        Assert.Equal(new Thickness(expected), listView.Padding);
    }

    // Flutter: "Stepper custom contentPadding for vertical stepper",
    // "Stepper vertical contentPadding includes stepIconMargin start value",
    // "Stepper vertical default contentPadding includes stepIconMargin start value" and
    // "Stepper vertical contentPadding without stepIconMargin uses default"
    [Theory]
    [InlineData(false, false, 60.0, 24.0, 24.0)]
    [InlineData(true, false, 48.0, 16.0, 16.0)]
    [InlineData(false, true, 70.0, 24.0, 24.0)]
    [InlineData(true, true, 56.0, 20.0, 16.0)]
    public void StepperVerticalContentPadding(
        bool customContent,
        bool customMargin,
        double left,
        double right,
        double bottom)
    {
        EdgeInsetsGeometry? contentPadding = customContent
            ? customMargin
                ? EdgeInsetsDirectional.Only(start: 40.0, end: 20.0, bottom: 16.0)
                : EdgeInsetsDirectional.Only(start: 48.0, end: 16.0, bottom: 16.0)
            : null;
        EdgeInsets? iconMargin = customMargin
            ? customContent ? EdgeInsets.Only(left: 16.0, right: 8.0) : EdgeInsets.Only(left: 10.0)
            : null;

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                stepIconMargin: iconMargin,
                contentPadding: contentPadding,
                steps: [new Step(new Text("Step 1"), new Text("Content 1"))]))));

        Padding paddingWidget = tester.FirstWidget<Padding>(
            Find.Ancestor(of: Find.Text("Content 1"), matching: Find.ByType<Padding>()));
        Assert.Equal(new Thickness(left, 0, right, bottom), paddingWidget.Insets.Resolve(TextDirection.Ltr));
    }

    // C#-only: the default index digit merges _kStepStyle (fontSize 12, white) into the ambient
    // DefaultTextStyle, so it inherits the M3 bodyMedium line height and letter spacing.
    [Fact]
    public void StepperIndexTextMergesStepStyleIntoAmbientStyle()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                steps:
                [
                    new Step(new Text("Step 1"), new Text("Content 1")),
                    new Step(new Text("Step 2"), new Text("Content 2")),
                ]))));

        Text index = tester.Widget<Text>(Find.Text("2"));
        Assert.Same(Stepper.KStepStyle, index.Style);
        TextStyle ambient = DefaultTextStyle.Of(tester.Element(Find.Text("2"))).Style;
        TextStyle effective = tester.RenderObject<RenderParagraph>(Find.Text("2")).Text.Style!;
        Assert.Equal(12.0, effective.FontSize);
        Assert.Equal(Colors.White, effective.Color);
        Assert.NotNull(ambient.Height);
        Assert.Equal(ambient.Height, effective.Height);
        Assert.Equal(ambient.LetterSpacing, effective.LetterSpacing);
    }

    // C#-only: Dart's vertical body is Padding > Column(ClipRect(content), controls) with the default
    // center cross-axis alignment, so narrow content is centered at its natural width.
    [Fact]
    public void VerticalStepperCentersContentAtNaturalWidth()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                steps: [new Step(new Text("Step 1"), new SizedBox(width: 100.0, height: 40.0))]))));

        Rect content = tester.GetRect(Find.ByWidgetPredicate(widget =>
            widget is SizedBox { Width: 100.0, Height: 40.0 }));
        // 800 wide: padding start 60, end 24 leaves 716, so the content is centered at 60 + 358.
        Assert.Equal(100.0, content.Width);
        Assert.Equal(60.0 + ((716.0 - 100.0) / 2), content.Left);
        Assert.Equal(72.0, content.Top);
    }

    // C#-only: Dart's _buildIcon cross-fades whenever the state changed, handing stepIconBuilder the
    // old state on the side that is fading out.
    [Fact]
    public void StepperIconBuilderReceivesBothSidesOfErrorTransition()
    {
        var states = new List<StepState>();
        Widget? Builder(int index, StepState state)
        {
            states.Add(state);
            return state == StepState.Error ? new Text("Custom error") : null;
        }
        Widget Build(StepState state) => new MaterialApp(
            home: new MaterialWidget(child: new Stepper(
                stepIconBuilder: Builder,
                steps: [new Step(new Text("One"), new Text("Body"), state: state)])));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Build(StepState.Indexed));
        states.Clear();
        tester.PumpWidget(Build(StepState.Error));

        Assert.Contains(StepState.Indexed, states);
        Assert.Contains(StepState.Error, states);
        Assert.True(tester.Any(Find.WidgetWithText<AnimatedCrossFade>("Custom error")));
    }

    private static Widget BuildButtonFrame(ThemeData theme) => new MaterialApp(
        theme: theme,
        home: new MaterialWidget(child: new Stepper(
            type: StepperType.Horizontal,
            onStepCancel: () => { },
            onStepContinue: () => { },
            steps: [new Step(new Text("step1"), new SizedBox(width: 100, height: 100))])));

    private static MaterialWidget ButtonMaterial(FrameworkDartTester tester, string label) =>
        tester.Widget<MaterialWidget>(Find.Descendant(
            of: Find.WidgetWithText<TextButton>(label),
            matching: Find.ByType<MaterialWidget>()));

    private static void AssertRectMoreOrLessEquals(Rect expected, Rect actual)
    {
        Assert.Equal(expected.Left, actual.Left, 0.001);
        Assert.Equal(expected.Top, actual.Top, 0.001);
        Assert.Equal(expected.Right, actual.Right, 0.001);
        Assert.Equal(expected.Bottom, actual.Bottom, 0.001);
    }

    private sealed class TappableColorWidget(Color tappedColor, Color untappedColor) : StatefulWidget
    {
        public Color TappedColor { get; } = tappedColor;
        public Color UntappedColor { get; } = untappedColor;

        public override State CreateState() => new TappableColorWidgetState();
    }

    private sealed class TappableColorWidgetState : State<TappableColorWidget>
    {
        private Color? _color;

        public override void InitState()
        {
            base.InitState();
            _color = ((TappableColorWidget)StateWidget).UntappedColor;
        }

        public override Widget Build(BuildContext context) => new GestureDetector(
            onTap: () => SetState(() => _color = ((TappableColorWidget)StateWidget).TappedColor),
            child: new Container(key: new ValueKey<string>("tap-me"), height: 50, width: 50, color: _color));
    }
}
