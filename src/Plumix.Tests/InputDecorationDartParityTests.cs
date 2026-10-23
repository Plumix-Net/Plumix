using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: material_ui/lib/src/input_decorator.dart
// Regressions from material-ui-src/test/input_decorator_test.dart.
[Collection(SchedulerTestCollection.Name)]
public sealed class InputDecorationDartParityTests
{
    [Fact]
    public void CopyWith_RetainsNullableFieldsAndCopiesSlotsStylesAndGeometry()
    {
        var label = new Text("label");
        var helper = new Text("helper");
        var hint = new Text("hint");
        var error = new Text("error");
        var prefix = new Text("prefix");
        var suffix = new Text("suffix");
        var counter = new Text("counter");
        var icon = new Icon(Icons.Email);
        var prefixIcon = new Icon(Icons.Lock);
        var suffixIcon = new Icon(Icons.Visibility);
        var style = new TextStyle(FontSize: 23.0, Color: Colors.Teal);
        var border = new OutlineInputBorder();
        var constraints = new BoxConstraints(MaxWidth: 200.0);
        var original = new InputDecoration(
            icon: icon, iconColor: Colors.Red,
            label: label, labelStyle: style, floatingLabelStyle: style,
            helper: helper, helperStyle: style, helperMaxLines: 2,
            hint: hint, hintStyle: style, hintTextDirection: TextDirection.Rtl, hintMaxLines: 3,
            hintFadeDuration: TimeSpan.FromMilliseconds(120),
            maintainHintHeight: false, maintainHintSize: false, maintainLabelSize: true,
            error: error, errorStyle: style, errorMaxLines: 4,
            floatingLabelBehavior: FloatingLabelBehavior.Always,
            floatingLabelAlignment: FloatingLabelAlignment.Center,
            isCollapsed: false, isDense: true, contentPadding: EdgeInsetsGeometry.All(5.0),
            prefixIcon: prefixIcon, prefixIconConstraints: constraints,
            prefix: prefix, prefixStyle: style, prefixIconColor: Colors.Green,
            suffixIcon: suffixIcon, suffixIconConstraints: constraints,
            suffix: suffix, suffixStyle: style, suffixIconColor: Colors.Blue,
            counter: counter, counterText: "count", counterStyle: style,
            filled: true, fillColor: Colors.Orange, focusColor: Colors.Pink, hoverColor: Colors.Purple,
            errorBorder: border, focusedBorder: border, focusedErrorBorder: border,
            disabledBorder: border, enabledBorder: border, border: border,
            enabled: false, semanticCounterText: "remaining", alignLabelWithHint: true,
            constraints: constraints, visualDensity: VisualDensity.Compact);

        InputDecoration retained = original.CopyWith(semanticsService: null);
        Assert.NotSame(original, retained);
        Assert.Equal(original, retained);
        Assert.Equal(original.GetHashCode(), retained.GetHashCode());
        Assert.Same(label, retained.Label);
        Assert.Same(style, retained.HintStyle);
        Assert.Same(border, retained.Border);

        var replacement = new Text("replacement");
        InputDecoration changed = original.CopyWith(
            icon: replacement, iconColor: Colors.Black,
            label: replacement, labelStyle: new TextStyle(FontSize: 25.0),
            helper: replacement, hint: replacement, error: replacement,
            prefix: replacement, prefixIcon: replacement, suffix: replacement, suffixIcon: replacement,
            counter: replacement, counterText: "new count",
            floatingLabelBehavior: FloatingLabelBehavior.Never,
            floatingLabelAlignment: FloatingLabelAlignment.Start,
            maintainHintHeight: true, maintainHintSize: true, maintainLabelSize: false,
            enabled: true, isDense: false, filled: false,
            constraints: new BoxConstraints(MaxWidth: 400.0));
        Assert.Same(replacement, changed.Icon);
        Assert.Equal(Colors.Black, changed.IconColor);
        Assert.Same(replacement, changed.Label);
        Assert.Equal(25.0, changed.LabelStyle!.FontSize);
        Assert.Same(replacement, changed.Helper);
        Assert.Same(replacement, changed.Hint);
        Assert.Same(replacement, changed.Error);
        Assert.Same(replacement, changed.Prefix);
        Assert.Same(replacement, changed.PrefixIcon);
        Assert.Same(replacement, changed.Suffix);
        Assert.Same(replacement, changed.SuffixIcon);
        Assert.Same(replacement, changed.Counter);
        Assert.Equal("new count", changed.CounterText);
        Assert.Equal(FloatingLabelBehavior.Never, changed.FloatingLabelBehavior);
        Assert.Equal(FloatingLabelAlignment.Start, changed.FloatingLabelAlignment);
        Assert.True(changed.MaintainHintSize);
        Assert.False(changed.MaintainLabelSize);
        Assert.True(changed.Enabled);
        Assert.False(changed.IsDense);
        Assert.False(changed.Filled);
        Assert.Equal(400.0, changed.Constraints!.Value.MaxWidth);
        Assert.False(original.Enabled);
    }

    [Fact]
    public void CopyWith_CopiesTextSlotsAndDoesNotClearThemWithNull()
    {
        var original = new InputDecoration(
            labelText: "label", helperText: "helper", hintText: "hint", errorText: "error",
            prefixText: "prefix", suffixText: "suffix", counterText: "counter");
        Assert.Equal(original, original.CopyWith(
            labelText: null, helperText: null, hintText: null, errorText: null,
            prefixText: null, suffixText: null, counterText: null));
        Assert.Equal(
            new InputDecoration(
                labelText: "L", helperText: "H", hintText: "I", errorText: "E",
                prefixText: "P", suffixText: "S", counterText: "C"),
            original.CopyWith(
                labelText: "L", helperText: "H", hintText: "I", errorText: "E",
                prefixText: "P", suffixText: "S", counterText: "C"));
        Assert.Throws<ArgumentException>(() => original.CopyWith(label: new Text("conflict")));
    }

    [Fact]
    public void Collapsed_PreservesExplicitNullsAndLegacyArguments()
    {
        InputDecoration defaults = InputDecoration.Collapsed(null);
        Assert.Same(InputBorder.None, defaults.Border);
        Assert.False(defaults.Filled);
        Assert.False(defaults.MaintainLabelSize);
        var border = new OutlineInputBorder();
        InputDecoration collapsed = InputDecoration.Collapsed(
            "hint", border: border, filled: null,
            floatingLabelBehavior: FloatingLabelBehavior.Always,
            floatingLabelAlignment: FloatingLabelAlignment.Center,
            maintainHintHeight: false, maintainHintSize: false, maintainLabelSize: true);
        Assert.Same(border, collapsed.Border);
        Assert.Null(collapsed.Filled);
        Assert.True(collapsed.IsCollapsed);
        Assert.False(collapsed.IsDense);
        Assert.False(collapsed.AlignLabelWithHint);
        Assert.Equal(EdgeInsetsGeometry.Zero, collapsed.ContentPadding);
        Assert.Equal(FloatingLabelBehavior.Always, collapsed.FloatingLabelBehavior);
        Assert.Equal(FloatingLabelAlignment.Center, collapsed.FloatingLabelAlignment);
        Assert.False(collapsed.MaintainHintSize);
        Assert.True(collapsed.MaintainLabelSize);
        Assert.Equal(collapsed, collapsed.CopyWith());
        Assert.Null(InputDecoration.Collapsed("hint", border: null).Border);
    }

    [Fact]
    public void Collapsed_DefersSlotConflictsUntilCopyOrDefaultApplication()
    {
        var hint = new Text("custom");
        InputDecoration collapsed = InputDecoration.Collapsed("text", hint: hint);
        Assert.Same(hint, collapsed.Hint);
        Assert.Equal("text", collapsed.HintText);
        Assert.Throws<ArgumentException>(() => collapsed.CopyWith());
        Assert.Throws<ArgumentException>(() => collapsed.ApplyDefaults(new InputDecorationThemeData()));
    }

    [Fact]
    public void LineLimits_AreMetadataUntilTheirTextWidgetIsBuilt()
    {
        var decoration = new InputDecoration(helperMaxLines: 0, hintMaxLines: -1, errorMaxLines: 0);
        Assert.Equal(decoration, decoration.CopyWith());
        Assert.Equal(-1, InputDecoration.Collapsed("hint", hintMaxLines: -1).HintMaxLines);
        using var harness = new DecoratorHarness(new InputDecorator(decoration));
        harness.Pump();
    }

#pragma warning disable CS0618 // The deprecated field remains independent from maintainHintSize in pinned Dart.
    [Fact]
    public void MaintainHintHeight_IsCopiedComparedAndPrintedButNeverChangesGeometry()
    {
        var decoration = new InputDecoration(hint: new SizedBox(width: 100.0, height: 80.0));
        Assert.True(decoration.MaintainHintHeight);
        InputDecoration legacy = decoration.CopyWith(maintainHintHeight: false);
        Assert.False(legacy.MaintainHintHeight);
        Assert.True(legacy.MaintainHintSize);
        Assert.NotEqual(decoration, legacy);
        Assert.Contains("maintainHintHeight: false", legacy.ToString());
        using var harness = new DecoratorHarness(new InputDecorator(decoration, child: new SizedBox(height: 10.0)));
        harness.Pump();
        Size before = harness.Decorator.Size;
        harness.Update(new InputDecorator(legacy, child: new SizedBox(height: 10.0)));
        Assert.Equal(before, harness.Decorator.Size);
        Assert.Equal(80.0, harness.Decorator.HintBox!.Size.Height);
        Assert.False(InputDecoration.Collapsed(null, maintainHintHeight: false).MaintainHintHeight);
    }
#pragma warning restore CS0618

    [Fact]
    public void ToString_ElidesDefaultsAndUsesDartFieldOrder()
    {
        Assert.Equal("InputDecoration()", new InputDecoration().ToString());
        Assert.Equal("InputDecoration()", new InputDecoration(
            labelStyle: new TextStyle(FontSize: 30.0), helperStyle: new TextStyle(),
            hintStyle: new TextStyle(), hintTextDirection: TextDirection.Rtl,
            isDense: false, isCollapsed: false, filled: false).ToString());
        Assert.Equal(
            "InputDecoration(labelText: \"Label\", helperText: \"Help\", hintText: \"Hint\", "
            + "hintFadeDuration: \"0:00:00.120000\", maintainHintHeight: false, maintainHintSize: false, "
            + "maintainLabelSize: true, floatingLabelBehavior: FloatingLabelBehavior.always, "
            + "floatingLabelAlignment: FloatingLabelAlignment.center, isDense: true, filled: true, "
            + "enabled: false, alignLabelWithHint: false)",
            new InputDecoration(
                labelText: "Label", helperText: "Help", hintText: "Hint",
                hintFadeDuration: TimeSpan.FromMilliseconds(120), maintainHintHeight: false,
                maintainHintSize: false, maintainLabelSize: true,
                floatingLabelBehavior: FloatingLabelBehavior.Always,
                floatingLabelAlignment: FloatingLabelAlignment.Center,
                isDense: true, filled: true, enabled: false, alignLabelWithHint: false).ToString());
    }

    [DebugOnlyFact]
    public void InputDecorator_DiagnosticsFollowDartPropertiesAndDefaultElision()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new InputDecorator(new InputDecoration()).DebugFillProperties(properties);
        Assert.Equal(
            ["decoration", "baseStyle", "isFocused", "expands", "isEmpty"],
            properties.Properties.Select(property => property.Name));
        Assert.Equal(
            ["decoration", "isFocused", "isEmpty"],
            properties.Properties.Where(property => property.Level != DiagnosticLevel.Fine)
                .Select(property => property.Name));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FocusedMaterial2Border_UsesColorSchemeRatherThanPrimaryColor(bool outlined)
    {
        var theme = ThemeData.Light with { UseMaterial3 = false, PrimaryColor = Colors.Red };
        using var harness = new DecoratorHarness(new InputDecorator(
            new InputDecoration(border: outlined ? new OutlineInputBorder() : new UnderlineInputBorder()),
            isFocused: true, child: new Text("value")), theme);
        harness.Pump();
        RenderCustomPaint paint = DecoratorHarness.Find<RenderCustomPaint>(harness.Decorator)!;
        var painter = Assert.IsType<InputBorderPainter>(paint.ForegroundPainter);
        Assert.Equal(theme.ColorScheme.Primary, painter.Border.BorderSide.Color);
        Assert.Equal(2.0, painter.Border.BorderSide.Width);
        Assert.NotEqual(theme.PrimaryColor, painter.Border.BorderSide.Color);
    }

    [Theory]
    [InlineData(TextDirection.Ltr)]
    [InlineData(TextDirection.Rtl)]
    public void FloatingLabel_GainsUnscaledWidthBeyondItsContainer(TextDirection direction)
    {
        using var harness = new DecoratorHarness(new InputDecorator(
            new InputDecoration(label: new SizedBox(width: 2000.0, height: 20.0), filled: true),
            child: new SizedBox(height: 20.0)), width: 300.0, textDirection: direction);
        harness.Pump();
        double logicalWidth = (300.0 - 2.0 * (12.0 + 4.0)) / 0.75;
        Assert.Equal(logicalWidth, harness.Decorator.LabelBox!.Size.Width, 6);
        Assert.True(harness.Decorator.LabelBox.Size.Width > harness.Decorator.Size.Width);
    }

    [Fact]
    public void CustomHint_UsesItsAmbientStyleAndPreservesItsWidget()
    {
        using var tester = new FrameworkDartTester();
        var hint = new Text("custom hint");
        tester.PumpWidget(Frame(new DefaultTextStyle(
            style: new TextStyle(FontSize: 31.0, Color: Colors.Green),
            child: new InputDecorator(new InputDecoration(
                hint: hint, hintStyle: new TextStyle(FontSize: 12.0, Color: Colors.Red)), isEmpty: true))));
        Element element = tester.ElementOfWidget(hint);
        Assert.Equal(31.0, DefaultTextStyle.Of(element).Style.FontSize);
        Assert.Equal(Colors.Green, DefaultTextStyle.Of(element).Style.Color);
    }

    [Theory]
    [InlineData(TextOverflow.Visible)]
    [InlineData(TextOverflow.Clip)]
    [InlineData(TextOverflow.Fade)]
    public void HintStyleOverflow_PrecedesTheMaxLinesFallback(TextOverflow overflow)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Frame(new InputDecorator(new InputDecoration(
            hintText: "hint", hintMaxLines: 2, hintStyle: new TextStyle(Overflow: overflow)), isEmpty: true)));
        Text hint = Assert.Single(tester.ElementsWithText("hint").Select(element => element.Widget).OfType<Text>());
        Assert.Equal(overflow, hint.Overflow);
        Assert.Equal(2, hint.MaxLines);
    }

    [Theory]
    [InlineData(TextDirection.Ltr)]
    [InlineData(TextDirection.Rtl)]
    public void NonRetainedHint_UsesCurvedFadeAndDirectionalTopStartLayout(TextDirection direction)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Frame(new InputDecorator(new InputDecoration(
            hintText: "hint", maintainHintSize: false, hintFadeDuration: TimeSpan.FromMilliseconds(120)),
            isEmpty: true), direction: direction));
        var switcher = (AnimatedSwitcher)tester.ElementOfType<AnimatedSwitcher>().Widget;
        Assert.Equal(TimeSpan.FromMilliseconds(120), switcher.Duration);
        var animation = new AlwaysStoppedAnimation<double>(0.25);
        var child = new SizedBox(width: 20.0, height: 10.0);
        var transition = Assert.IsType<FadeTransition>(switcher.TransitionBuilder(child, animation));
        Assert.Equal(Curves.FastOutSlowIn.Transform(0.25), transition.Opacity.Value, 8);
        Assert.Same(child, transition.Child);
        var previous = new SizedBox(width: 40.0, height: 20.0);
        var stack = Assert.IsType<Stack>(switcher.LayoutBuilder(child, [previous]));
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopStart, stack.Alignment);
        Assert.Equal([previous, child], stack.Children);
        var empty = Assert.IsType<Stack>(switcher.LayoutBuilder(null, [previous]));
        Assert.Equal([previous], empty.Children);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HintFade_RetainsOrReleasesHeightAndRestoresTheHint(bool maintainHintSize)
    {
        using var tester = new FrameworkDartTester();
        var hint = new Container(width: 200.0, height: 100.0);
        Widget Build(bool empty) => Frame(new InputDecorator(
            new InputDecoration(
                hint: hint, maintainHintSize: maintainHintSize,
                hintFadeDuration: TimeSpan.FromMilliseconds(120)),
            isEmpty: empty, child: new SizedBox(height: 20.0)));
        tester.PumpWidget(Build(true));
        RenderDecoration render = DecoratorHarness.Find<RenderDecoration>(tester.RenderView)!;
        double shownHeight = render.Size.Height;
        tester.PumpWidget(Build(false));
        tester.Pump(TimeSpan.FromMilliseconds(60));
        if (!maintainHintSize)
        {
            FadeTransition fadingHint = tester.AllElements().Select(element => element.Widget)
                .OfType<FadeTransition>().Single(fade => ReferenceEquals(fade.Child, hint));
            Assert.Equal(Curves.FastOutSlowIn.Transform(0.5), fadingHint.Opacity.Value, 6);
        }

        tester.Pump(TimeSpan.FromMilliseconds(61));
        if (maintainHintSize) Assert.Equal(shownHeight, render.Size.Height);
        else Assert.True(render.Size.Height < shownHeight);

        tester.PumpWidget(Build(true));
        tester.Pump(TimeSpan.FromMilliseconds(121));
        Assert.Equal(shownHeight, render.Size.Height);
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void IconSlots_MergeAmbientOpacityForExternalAndAffixIcons()
    {
        using var tester = new FrameworkDartTester();
        var opacities = new List<double?>();
        Widget Probe() => new Builder(context =>
        {
            opacities.Add(IconTheme.Of(context).Opacity);
            return new Icon(Icons.Email);
        });
        tester.PumpWidget(Frame(new IconTheme(
            new IconThemeData(Opacity: 0.35),
            new InputDecorator(new InputDecoration(icon: Probe(), prefixIcon: Probe(), suffixIcon: Probe())))));
        Assert.Equal(3, opacities.Count);
        Assert.All(opacities, opacity => Assert.Equal(0.35, opacity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AffixIconButtonTheme_OverridesColorAndSizeAndKeepsOtherAmbientSlots(bool dense)
    {
        using var tester = new FrameworkDartTester();
        var ambient = new ButtonStyle(
            ForegroundColor: WidgetStateProperty<Color?>.All(Colors.Purple),
            BackgroundColor: WidgetStateProperty<Color?>.All(Colors.Orange),
            IconSize: WidgetStateProperty<double?>.All(50.0),
            Elevation: WidgetStateProperty<double?>.All(7.0));
        var resolved = new List<IconButtonThemeData>();
        Widget Probe() => new Builder(context =>
        {
            resolved.Add(IconButtonTheme.Of(context));
            return new Icon(Icons.Email);
        });
        tester.PumpWidget(Frame(new IconButtonTheme(
            new IconButtonThemeData(ambient),
            new InputDecorator(new InputDecoration(
                prefixIcon: Probe(), suffixIcon: Probe(), isDense: dense,
                prefixIconColor: Colors.Teal, suffixIconColor: Colors.Blue)))));
        Assert.Equal(2, resolved.Count);
        IReadOnlySet<WidgetState> states = new HashSet<WidgetState>();
        Assert.Equal(Colors.Teal, resolved[0].Style!.ForegroundColor!.Resolve(states));
        Assert.Equal(Colors.Blue, resolved[1].Style!.ForegroundColor!.Resolve(states));
        foreach (IconButtonThemeData theme in resolved)
        {
            Assert.Equal(dense ? 18.0 : 24.0, theme.Style!.IconSize!.Resolve(states));
            Assert.Same(ambient.BackgroundColor, theme.Style.BackgroundColor);
            Assert.Same(ambient.Elevation, theme.Style.Elevation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Counter_UsesSemanticReplacementAndFocusLiveRegion(bool focused)
    {
        using var harness = new DecoratorHarness(new InputDecorator(
            new InputDecoration(counterText: "3/10", semanticCounterText: "7 remaining"), isFocused: focused));
        SemanticsNode root = harness.PumpSemantics();
        List<SemanticsNode> nodes = Descendants(root).Where(node => node.Label.Contains("remaining")).ToList();
        SemanticsNode counter = Assert.Single(nodes);
        Assert.Equal("7 remaining", counter.Label);
        Assert.Equal(focused, counter.Flags.HasFlag(SemanticsFlags.IsLiveRegion));
        Assert.DoesNotContain(Descendants(root), node => node.Label.Contains("3/10"));
    }

    [Fact]
    public void Counter_LeavesExplicitNewlinesUnrestricted()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Frame(new InputDecorator(new InputDecoration(counterText: "first\nsecond"))));
        Text counter = tester.ElementsWithText("first\nsecond")
            .Select(element => element.Widget).OfType<Text>().Single();
        Assert.Null(counter.MaxLines);
        Assert.Equal(TextOverflow.Ellipsis, counter.Overflow);
    }

    [Fact]
    public void ContainerOf_FindsNearestAncestorAndReturnsNullOutside()
    {
        using var tester = new FrameworkDartTester();
        BuildContext? outside = null;
        BuildContext? inside = null;
        tester.PumpWidget(Frame(new Builder(context =>
        {
            outside = context;
            return new InputDecorator(new InputDecoration(), child: new Builder(childContext =>
            {
                inside = childContext;
                return new SizedBox(height: 20.0);
            }));
        })));
        RenderDecoration decorator = DecoratorHarness.Find<RenderDecoration>(tester.RenderView)!;
        Assert.Null(InputDecorator.ContainerOf(outside!));
        Assert.Same(decorator.ContainerBox, InputDecorator.ContainerOf(inside!));
        Assert.Equal(800.0, InputDecorator.ContainerOf(inside!)!.Size.Width);
    }

    private static Widget Frame(Widget child, ThemeData? theme = null, TextDirection direction = TextDirection.Ltr) =>
        new Directionality(direction, new MediaQuery(
            new MediaQueryData(Size: new Size(800.0, 600.0)),
            new Theme(theme ?? ThemeData.Light, new Align(alignment: Alignment.TopLeft, child: child))));

    private static IEnumerable<SemanticsNode> Descendants(SemanticsNode root)
    {
        yield return root;
        foreach (SemanticsNode child in root.Children)
        {
            foreach (SemanticsNode descendant in Descendants(child)) yield return descendant;
        }
    }
}
