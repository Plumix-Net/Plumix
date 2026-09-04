using Avalonia;
using Avalonia.Media;
using Plumix;
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
public sealed class MaterialSnackBarTests : IDisposable
{
    public MaterialSnackBarTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void SnackBar_ValidatesFlutterAsserts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Bar(elevation: -1));
        Assert.Throws<ArgumentException>(() => Bar(width: 200, margin: new Thickness(8)));
        foreach (double invalid in new[] { -1.0, -0.0001, 1.000001, 5.0 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Bar(actionOverflowThreshold: invalid));
        }

        // Dart drops the asserts Plumix used to add on its own: a zero width and negative insets
        // are accepted by the constructor.
        Assert.Equal(0, Bar(width: 0).Width);
        Assert.Equal(new Thickness(-1), Bar(margin: new Thickness(-1)).Margin!.Value.Resolve(TextDirection.Ltr));
    }

    [Fact]
    public void SnackBarThemeData_ValidatesFlutterAsserts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnackBarThemeData(elevation: -1));
        Assert.Throws<ArgumentException>(() => new SnackBarThemeData(width: 200));
        Assert.Throws<ArgumentException>(
            () => new SnackBarThemeData(width: 200, behavior: SnackBarBehavior.Fixed));
        Assert.Null(new SnackBarThemeData(width: 200, behavior: SnackBarBehavior.Floating).Behavior
            is SnackBarBehavior.Floating
            ? null
            : "width is allowed with floating behavior");
        foreach (double invalid in new[] { -1.0, -0.0001, 1.000001, 5.0 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SnackBarThemeData(actionOverflowThreshold: invalid));
        }

        // `disabledBackgroundColor must not be provided when background color is a WidgetStateColor`.
        Assert.Throws<ArgumentException>(() => new SnackBarThemeData(
            actionBackgroundColor: WidgetStateColor.ResolveWith(_ => Colors.Red),
            disabledActionBackgroundColor: Colors.Blue));
        Assert.Throws<ArgumentException>(() => new SnackBarAction(
            "UNDO",
            () => { },
            backgroundColor: WidgetStateColor.ResolveWith(_ => Colors.Red),
            disabledBackgroundColor: Colors.Blue));

        // A plain color converted implicitly is not a WidgetStateColor, so the pair is allowed.
        Assert.NotNull(new SnackBarThemeData(
            actionBackgroundColor: Colors.Red,
            disabledActionBackgroundColor: Colors.Blue));
    }

    [Fact]
    public void SnackBarThemeData_DefaultsAreNullAndCopyWithEqualityRoundTrip()
    {
        var empty = new SnackBarThemeData();
        Assert.Null(empty.BackgroundColor);
        Assert.Null(empty.ActionTextColor);
        Assert.Null(empty.DisabledActionTextColor);
        Assert.Null(empty.ContentTextStyle);
        Assert.Null(empty.Elevation);
        Assert.Null(empty.Shape);
        Assert.Null(empty.Behavior);
        Assert.Null(empty.Width);
        Assert.Null(empty.InsetPadding);
        Assert.Null(empty.ShowCloseIcon);
        Assert.Null(empty.CloseIconColor);
        Assert.Null(empty.ActionOverflowThreshold);
        Assert.Null(empty.ActionBackgroundColor);
        Assert.Null(empty.DisabledActionBackgroundColor);
        Assert.Null(empty.DismissDirection);

        Assert.Equal(empty, new SnackBarThemeData());
        Assert.Equal(empty.GetHashCode(), new SnackBarThemeData().GetHashCode());

        var populated = new SnackBarThemeData(
            backgroundColor: Colors.Purple,
            elevation: 3,
            behavior: SnackBarBehavior.Floating,
            width: 240,
            showCloseIcon: true,
            dismissDirection: DismissDirection.Up);
        Assert.Equal(populated, populated.CopyWith());
        Assert.NotEqual(populated, populated.CopyWith(elevation: 9));
    }

    [Fact]
    public void SnackBarThemeData_LerpMatchesDartIncludingItsShowCloseIconOmission()
    {
        Assert.Equal(new SnackBarThemeData(), SnackBarThemeData.Lerp(null, null, 0));

        var data = new SnackBarThemeData(elevation: 4, showCloseIcon: true);
        Assert.Same(data, SnackBarThemeData.Lerp(data, data, 0.5));

        var a = new SnackBarThemeData(elevation: 0, showCloseIcon: true, closeIconColor: Colors.Black);
        var b = new SnackBarThemeData(elevation: 10, showCloseIcon: true, closeIconColor: Colors.White);
        SnackBarThemeData lerped = SnackBarThemeData.Lerp(a, b, 0.5);
        Assert.Equal(5, lerped.Elevation);
        // Dart's `lerp` never passes `showCloseIcon` to the result it constructs.
        Assert.Null(lerped.ShowCloseIcon);
        Assert.NotNull(lerped.CloseIconColor);
    }

    [Fact]
    public void SnackBar_PersistDefaultsToHavingAnActionAndWithAnimationCopiesEveryField()
    {
        Assert.False(Bar().Persist);
        Assert.True(Bar(action: Action()).Persist);
        Assert.False(Bar(action: Action(), persist: false).Persist);

        using var controller = SnackBar.CreateAnimationController();
        Assert.Equal(TimeSpan.FromMilliseconds(250), controller.Duration);
        Assert.Null(controller.ReverseDuration);

        using var styled = SnackBar.CreateAnimationController(
            duration: TimeSpan.FromMilliseconds(400),
            reverseDuration: TimeSpan.FromMilliseconds(90));
        Assert.Equal(TimeSpan.FromMilliseconds(400), styled.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(90), styled.ReverseDuration);

        var action = Action();
        var original = new SnackBar(
            content: new Text("Message"),
            backgroundColor: Colors.Purple,
            elevation: 3,
            margin: new Thickness(4),
            width: null,
            shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(9)),
            hitTestBehavior: Plumix.Rendering.HitTestBehavior.Translucent,
            behavior: SnackBarBehavior.Floating,
            action: action,
            actionOverflowThreshold: 0.5,
            showCloseIcon: true,
            closeIconColor: Colors.Gold,
            duration: TimeSpan.FromSeconds(9),
            persist: true,
            onVisible: () => { },
            dismissDirection: DismissDirection.Up,
            clipBehavior: Clip.AntiAlias,
            key: new ValueKey<string>("snack"));
        SnackBar copy = original.WithAnimation(controller, new ValueKey<string>("fallback"));

        Assert.Same(controller, copy.Animation);
        Assert.Equal(original.Key, copy.Key);
        Assert.Same(original.Content, copy.Content);
        Assert.Equal(Colors.Purple, copy.BackgroundColor);
        Assert.Equal(3, copy.Elevation);
        Assert.Equal(new Thickness(4), copy.Margin!.Value.Resolve(TextDirection.Ltr));
        Assert.Equal(original.Shape, copy.Shape);
        Assert.Equal(Plumix.Rendering.HitTestBehavior.Translucent, copy.HitTestBehavior);
        Assert.Equal(SnackBarBehavior.Floating, copy.Behavior);
        Assert.Same(action, copy.Action);
        Assert.Equal(0.5, copy.ActionOverflowThreshold);
        Assert.True(copy.ShowCloseIcon);
        Assert.Equal(Colors.Gold, copy.CloseIconColor);
        Assert.Equal(TimeSpan.FromSeconds(9), copy.Duration);
        Assert.True(copy.Persist);
        Assert.Same(original.OnVisible, copy.OnVisible);
        Assert.Equal(DismissDirection.Up, copy.DismissDirection);
        Assert.Equal(Clip.AntiAlias, copy.ClipBehavior);

        // The fallback key is only used when the source snack bar has none.
        SnackBar keyless = Bar().WithAnimation(controller, new ValueKey<string>("fallback"));
        Assert.Equal(new ValueKey<string>("fallback"), keyless.Key);
    }

    [Fact]
    public void SnackBar_Material2BackgroundIsDartsAlphaBlendOfOnSurfaceOverSurface()
    {
        var light = ThemeData.Light with { UseMaterial3 = false };
        // Dart: Color.alphaBlend(colorScheme.onSurface.withOpacity(0.80), colorScheme.surface).
        Color expected = AlphaBlend(
            WithOpacity(light.ColorScheme.OnSurface, 0.80),
            light.ColorScheme.Surface);
        using var harness = Show(light, Bar());
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == expected);

        // Dark M2 takes `colorScheme.onSurface` straight through.
        var dark = ThemeData.Dark with { UseMaterial3 = false };
        using var darkHarness = Show(dark, Bar());
        Assert.Contains(FindDescendants<RenderDecoratedBox>(darkHarness.RenderView), box =>
            box.Decoration.Color == dark.ColorScheme.OnSurface);
    }

    private static Color WithOpacity(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Round(255 * opacity), color.R, color.G, color.B);

    private static Color AlphaBlend(Color foreground, Color background)
    {
        double alpha = foreground.A / 255.0;
        byte Blend(byte f, byte b) => (byte)Math.Round((f * alpha) + (b * (1 - alpha)));
        return Color.FromArgb(
            255,
            Blend(foreground.R, background.R),
            Blend(foreground.G, background.G),
            Blend(foreground.B, background.B));
    }

    [Fact]
    public void SnackBar_M3AndM2DefaultsUseOppositeSurfaceContrast()
    {
        using var material3 = Show(ThemeData.Light, Bar(action: Action()));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(material3.RenderView), box =>
            box.Decoration.Color == ThemeData.Light.ColorScheme.InverseSurface);
        Assert.Equal(
            ThemeData.Light.ColorScheme.OnInverseSurface,
            Assert.IsType<SolidColorBrush>(FindParagraph(material3.RenderView, "Message")!.Foreground).Color);
        Assert.Equal(
            ThemeData.Light.ColorScheme.InversePrimary,
            Assert.IsType<SolidColorBrush>(FindParagraph(material3.RenderView, "UNDO")!.Foreground).Color);

        var material2Dark = ThemeData.Dark with { UseMaterial3 = false };
        using var material2 = Show(material2Dark, Bar(action: Action()));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(material2.RenderView), box =>
            box.Decoration.Color == material2Dark.ColorScheme.OnSurface);
        // M2 dark inverts to a light theme, so the action takes `colorScheme.primary`.
        Assert.Equal(
            material2Dark.ColorScheme.Primary,
            Assert.IsType<SolidColorBrush>(FindParagraph(material2.RenderView, "UNDO")!.Foreground).Color);
    }

    [Fact]
    public void SnackBar_M3DarkActionUsesInversePrimary()
    {
        using var harness = Show(ThemeData.Dark, Bar(action: Action()));
        Assert.Equal(
            ThemeData.Dark.ColorScheme.InversePrimary,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "UNDO")!.Foreground).Color);
    }

    [Fact]
    public void SnackBar_FixedBehaviorGetsNoShapeWhileFloatingTakesTheDefaultRoundedRect()
    {
        using var fixedBar = Show(ThemeData.Light, Bar());
        Assert.DoesNotContain(FindWidgets<MaterialWidget>(fixedBar), material => material.Shape is not null);

        using var floating = Show(ThemeData.Light, Bar(behavior: SnackBarBehavior.Floating));
        MaterialWidget floatingMaterial = Assert.Single(
            FindWidgets<MaterialWidget>(floating),
            material => material.Shape is RoundedRectangleBorder);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(4.0)),
            floatingMaterial.Shape);
        Assert.Equal(6.0, floatingMaterial.Elevation);
    }

    [Fact]
    public void SnackBar_MaterialAppliesClipBehaviorAndWidgetOverridesBeatThemes()
    {
        using var byDefault = Show(ThemeData.Light, Bar());
        Assert.Contains(FindWidgets<MaterialWidget>(byDefault), material => material.ClipBehavior == Clip.HardEdge);

        using var antiAlias = Show(ThemeData.Light, Bar(clipBehavior: Clip.AntiAlias));
        Assert.Contains(FindWidgets<MaterialWidget>(antiAlias), material => material.ClipBehavior == Clip.AntiAlias);

        var themed = ThemeData.Light with
        {
            SnackBarTheme = new SnackBarThemeData(
                backgroundColor: Colors.Purple,
                actionTextColor: Colors.Gold,
                contentTextStyle: ThemeData.Light.TextTheme.BodyMedium.CopyWith(
                    color: Colors.Orange,
                    fontSize: 18),
                elevation: 0,
                behavior: SnackBarBehavior.Floating,
                insetPadding: new Thickness(7),
                shape: new RoundedRectangleBorder(
                    borderRadius: Plumix.Rendering.BorderRadius.Circular(9))),
        };

        using var fromTheme = Show(themed, Bar(action: Action()));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(fromTheme.RenderView), box =>
            box.Decoration.Color == Colors.Purple);
        RenderParagraph content = FindParagraph(fromTheme.RenderView, "Message")!;
        Assert.Equal(18, content.FontSize);
        Assert.Equal(Colors.Orange, Assert.IsType<SolidColorBrush>(content.Foreground).Color);
        Assert.Equal(
            Colors.Gold,
            Assert.IsType<SolidColorBrush>(FindParagraph(fromTheme.RenderView, "UNDO")!.Foreground).Color);
        Assert.Contains(FindWidgets<MaterialWidget>(fromTheme), material => material.Elevation == 0);

        using var widgetWins = Show(themed, Bar(backgroundColor: Colors.Green, elevation: 12));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(widgetWins.RenderView), box =>
            box.Decoration.Color == Colors.Green);
        Assert.Contains(FindWidgets<MaterialWidget>(widgetWins), material => material.Elevation == 12);
    }

    [Fact]
    public void SnackBar_LocalSnackBarThemeBeatsThemeDataAndIsCapturedByOf()
    {
        var themed = ThemeData.Light with
        {
            SnackBarTheme = new SnackBarThemeData(backgroundColor: Colors.Purple),
        };
        using var harness = Show(
            themed,
            Bar(),
            wrap: child => new SnackBarTheme(new SnackBarThemeData(backgroundColor: Colors.Teal), child));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Teal);
    }

    [Fact]
    public void SnackBar_ContentThemeDiffersFromTheAncestorOnlyInItsColorScheme()
    {
        var outer = ThemeData.Light with { UseMaterial3 = false };
        ThemeData? ambient = null;
        using var harness = Show(
            outer,
            Bar(),
            wrap: child => new Builder(context =>
            {
                // `Theme.Of` localizes, so the snack bar's ancestor value is captured here rather
                // than compared against the raw ThemeData handed to the Theme widget.
                ambient = Theme.Of(context);
                return child;
            }));
        Theme inner = Assert.Single(FindWidgets<Theme>(harness), theme => !ReferenceEquals(theme.Data, outer));

        Assert.NotNull(ambient);
        Assert.NotEqual(ambient!.ColorScheme, inner.Data.ColorScheme);
        Assert.Equal(ambient with { ColorScheme = inner.Data.ColorScheme }, inner.Data);
        // The inverted scheme swaps the on/base roles.
        Assert.Equal(outer.ColorScheme.OnSurface, inner.Data.ColorScheme.Surface);
        Assert.Equal(outer.ColorScheme.Surface, inner.Data.ColorScheme.OnSurface);
        Assert.Equal(Brightness.Dark, inner.Data.ColorScheme.Brightness);
    }

    [Fact]
    public void SnackBar_M3KeepsTheAmbientThemeBecauseItsTokensArePreInverted()
    {
        using var harness = Show(ThemeData.Light, Bar());
        Assert.DoesNotContain(
            FindWidgets<Theme>(harness),
            theme => !ReferenceEquals(theme.Data, ThemeData.Light) && theme.Data.ColorScheme
                != ThemeData.Light.ColorScheme);
    }

    [Fact]
    public void SnackBar_ActionOverflowMovesTheActionOntoItsOwnWrapRun()
    {
        // A wide label over a narrow bar exceeds the 0.25 threshold, so Dart adds a second row.
        using var narrow = Show(
            ThemeData.Light,
            Bar(action: Action("UNDO THIS VERY LONG ACTION LABEL")),
            surface: new Size(200, 220));
        Wrap narrowWrap = Assert.Single(FindWidgets<Wrap>(narrow));
        Assert.Equal(2, narrowWrap.Children.Count);

        using var wide = Show(ThemeData.Light, Bar(action: Action()), surface: new Size(900, 220));
        Wrap wideWrap = Assert.Single(FindWidgets<Wrap>(wide));
        Assert.Single(wideWrap.Children);

        // The same narrow bar with a short label overflows at the 0.25 default but not at 1.0.
        using var atDefault = Show(ThemeData.Light, Bar(action: Action()), surface: new Size(200, 220));
        Assert.Equal(2, Assert.Single(FindWidgets<Wrap>(atDefault)).Children.Count);

        using var never = Show(
            ThemeData.Light,
            Bar(action: Action(), actionOverflowThreshold: 1),
            surface: new Size(200, 220));
        Assert.Single(Assert.Single(FindWidgets<Wrap>(never)).Children);

        using var always = Show(
            ThemeData.Light,
            Bar(action: Action(), actionOverflowThreshold: 0),
            surface: new Size(900, 220));
        Assert.Equal(2, Assert.Single(FindWidgets<Wrap>(always)).Children.Count);
    }

    [Fact]
    public void SnackBarAction_CanOnlyBePressedOnceAndThenTakesTheDisabledColors()
    {
        int calls = 0;
        // Dart's SnackBarAction closes through the throwing `ScaffoldMessenger.of`, so the action
        // always needs a messenger ancestor even when probed on its own.
        using var harness = new WidgetRenderHarness(WrapChrome(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox(
                width: 200,
                height: 60,
                child: new SnackBarAction(
                    label: "UNDO",
                    onPressed: () => calls++,
                    textColor: Colors.Green,
                    disabledTextColor: Colors.Red))))));
        harness.Pump(new Size(200, 60));

        Assert.Equal(
            Colors.Green,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "UNDO")!.Foreground).Color);

        double now = Scheduler.CurrentSeconds;
        Tap(harness.RenderView, new Point(100, 30), 901);
        harness.Pump(new Size(200, 60));
        Tap(harness.RenderView, new Point(100, 30), 902);
        harness.Pump(new Size(200, 60));

        // The disabled colour arrives through `Material.animationDuration`, so let the default
        // 200 ms text-style animation finish before reading the paragraph.
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.5));
        harness.Pump(new Size(200, 60));

        Assert.Equal(1, calls);
        Assert.Equal(
            Colors.Red,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "UNDO")!.Foreground).Color);
    }

    [Fact]
    public void SnackBarAction_WidgetStateColorIsResolvedPerStateRatherThanFlattened()
    {
        // Dart's SnackBarAction closes through the throwing `ScaffoldMessenger.of`, so the action
        // always needs a messenger ancestor even when probed on its own.
        using var harness = new WidgetRenderHarness(WrapChrome(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox(
                width: 200,
                height: 60,
                child: new SnackBarAction(
                    label: "UNDO",
                    onPressed: () => { },
                    textColor: WidgetStateColor.ResolveWith(states =>
                        states.Contains(WidgetState.Disabled) ? Colors.Red : Colors.Green)))))));
        harness.Pump(new Size(200, 60));
        Assert.Equal(
            Colors.Green,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "UNDO")!.Foreground).Color);

        double now = Scheduler.CurrentSeconds;
        Tap(harness.RenderView, new Point(100, 30), 903);
        harness.Pump(new Size(200, 60));
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.5));
        harness.Pump(new Size(200, 60));
        Assert.Equal(
            Colors.Red,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "UNDO")!.Foreground).Color);
    }

    [Fact]
    public void ScaffoldMessenger_ThrowsWhenThereIsNoDescendantScaffold()
    {
        using var harness = new WidgetRenderHarness(WrapChrome(
            ThemeData.Light,
            new ScaffoldMessenger(new SizedBox())));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();
        Assert.Throws<InvalidOperationException>(() => messenger.ShowSnackBar(Bar()));
    }

    [Fact]
    public async Task ScaffoldMessenger_QueuesSnackBarsOnOneControllerAndReportsClosedReasons()
    {
        using var harness = NewMessenger();
        var messenger = harness.FindState<ScaffoldMessengerState>();

        var first = messenger.ShowSnackBar(Bar(content: "First"));
        var second = messenger.ShowSnackBar(Bar(content: "Second"));
        harness.Pump(new Size(360, 220));
        Assert.NotNull(FindParagraph(harness.RenderView, "First"));
        Assert.Null(FindParagraph(harness.RenderView, "Second"));

        // Both entries are driven by the same animation instance.
        Assert.Same(first.Feature.Animation, second.Feature.Animation);

        messenger.RemoveCurrentSnackBar(SnackBarClosedReason.Remove);
        harness.Pump(new Size(360, 220));
        Assert.Equal(SnackBarClosedReason.Remove, await first.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "First"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));

        second.Close();
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));
        await Task.Yield();
        Assert.Equal(SnackBarClosedReason.Hide, await second.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "Second"));
    }

    [Fact]
    public async Task ScaffoldMessenger_ClearSnackBarsKeepsTheVisibleBarAndDropsTheQueue()
    {
        using var harness = NewMessenger();
        var messenger = harness.FindState<ScaffoldMessengerState>();

        var first = messenger.ShowSnackBar(Bar(content: "First"));
        messenger.ShowSnackBar(Bar(content: "Second"));
        messenger.ShowSnackBar(Bar(content: "Third"));
        harness.Pump(new Size(360, 220));

        messenger.ClearSnackBars();
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));
        await Task.Yield();

        Assert.Equal(SnackBarClosedReason.Hide, await first.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "First"));
        Assert.Null(FindParagraph(harness.RenderView, "Second"));
        Assert.Null(FindParagraph(harness.RenderView, "Third"));
    }

    [Fact]
    public async Task SnackBarAction_ClosesTheBarWithTheActionReason()
    {
        int calls = 0;
        using var harness = NewMessenger();
        var messenger = harness.FindState<ScaffoldMessengerState>();
        var controller = messenger.ShowSnackBar(Bar(action: Action("UNDO", () => calls++)));
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));

        RenderParagraph label = FindParagraph(harness.RenderView, "UNDO")!;
        Point center = LocalCenter(label);
        Tap(harness.RenderView, center, 910);
        harness.Pump(new Size(360, 220));
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));
        await Task.Yield();

        Assert.Equal(1, calls);
        Assert.Equal(SnackBarClosedReason.Action, await controller.Closed);
    }

    [Fact]
    public async Task SnackBar_CloseIconHidesWithDismissAndCarriesTheLocalizedTooltip()
    {
        using var harness = NewMessenger();
        var messenger = harness.FindState<ScaffoldMessengerState>();
        var controller = messenger.ShowSnackBar(Bar(showCloseIcon: true, persist: true));
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));

        IconButton closeButton = Assert.Single(FindWidgets<IconButton>(harness));
        Assert.Equal("Close", closeButton.Tooltip);
        Assert.Equal(24.0, closeButton.IconSize);
        Assert.NotNull(FindParagraph(harness.RenderView, char.ConvertFromUtf32(Icons.Close.CodePoint)));

        closeButton.OnPressed!();
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));
        await Task.Yield();
        Assert.Equal(SnackBarClosedReason.Dismiss, await controller.Closed);
    }

    [Fact]
    public async Task SnackBar_SemanticsIsALiveRegionThatRemovesWithTheDismissReason()
    {
        using var harness = NewMessenger();
        var messenger = harness.FindState<ScaffoldMessengerState>();
        var controller = messenger.ShowSnackBar(Bar(persist: true));
        AnimationPump.Advance(0.4);
        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(360, 220));

        SemanticsNode? liveRegion = FindSemantics(semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.IsLiveRegion)
            && node.Actions.HasFlag(SemanticsActions.Dismiss));
        Assert.NotNull(liveRegion);
        Assert.True(liveRegion!.PerformAction(SemanticsActions.Dismiss));
        harness.Pump(new Size(360, 220));
        Assert.Equal(SnackBarClosedReason.Dismiss, await controller.Closed);
    }

    [Fact]
    public void SnackBar_ComposesTheSharedDismissibleWithTheResolvedDirection()
    {
        using var byDefault = Show(ThemeData.Light, Bar());
        Dismissible dismissible = Assert.Single(FindWidgets<Dismissible>(byDefault));
        Assert.Equal(DismissDirection.Down, dismissible.Direction);
        // A snack bar must not run Dismissible's resize phase after the swipe.
        Assert.Null(dismissible.ResizeDuration);
        Assert.Equal(Plumix.Rendering.HitTestBehavior.Opaque, dismissible.Behavior);

        using var fromWidget = Show(ThemeData.Light, Bar(dismissDirection: DismissDirection.Up));
        Assert.Equal(DismissDirection.Up, Assert.Single(FindWidgets<Dismissible>(fromWidget)).Direction);

        var themed = ThemeData.Light with
        {
            SnackBarTheme = new SnackBarThemeData(dismissDirection: DismissDirection.Horizontal),
        };
        using var fromTheme = Show(themed, Bar());
        Assert.Equal(
            DismissDirection.Horizontal,
            Assert.Single(FindWidgets<Dismissible>(fromTheme)).Direction);

        // The widget value wins over the theme value.
        using var widgetWins = Show(themed, Bar(dismissDirection: DismissDirection.StartToEnd));
        Assert.Equal(
            DismissDirection.StartToEnd,
            Assert.Single(FindWidgets<Dismissible>(widgetWins)).Direction);
    }

    [Fact]
    public void SnackBar_MarginAndThemeInsetPaddingMakeHitTestingDeferToTheChild()
    {
        using var opaque = Show(ThemeData.Light, Bar());
        Assert.Equal(
            Plumix.Rendering.HitTestBehavior.Opaque,
            Assert.Single(FindWidgets<Dismissible>(opaque)).Behavior);

        using var withMargin = Show(
            ThemeData.Light,
            Bar(margin: new Thickness(8), behavior: SnackBarBehavior.Floating));
        Assert.Equal(
            Plumix.Rendering.HitTestBehavior.DeferToChild,
            Assert.Single(FindWidgets<Dismissible>(withMargin)).Behavior);

        var themed = ThemeData.Light with
        {
            SnackBarTheme = new SnackBarThemeData(
                behavior: SnackBarBehavior.Floating,
                insetPadding: new Thickness(6)),
        };
        using var themedInset = Show(themed, Bar());
        Assert.Equal(
            Plumix.Rendering.HitTestBehavior.DeferToChild,
            Assert.Single(FindWidgets<Dismissible>(themedInset)).Behavior);

        using var explicitBehavior = Show(
            ThemeData.Light,
            Bar(margin: new Thickness(8), behavior: SnackBarBehavior.Floating,
                hitTestBehavior: Plumix.Rendering.HitTestBehavior.Opaque));
        Assert.Equal(
            Plumix.Rendering.HitTestBehavior.Opaque,
            Assert.Single(FindWidgets<Dismissible>(explicitBehavior)).Behavior);
    }

    [Fact]
    public void SnackBar_MarginAndWidthRejectFixedBehaviorWithDartsThreeMessages()
    {
        InvalidOperationException fromConstructor = Assert.Throws<InvalidOperationException>(
            () => Show(ThemeData.Light, Bar(margin: new Thickness(8), behavior: SnackBarBehavior.Fixed)));
        Assert.Contains("was set in the SnackBar constructor.", fromConstructor.Message, StringComparison.Ordinal);

        var themed = ThemeData.Light with
        {
            SnackBarTheme = new SnackBarThemeData(behavior: SnackBarBehavior.Fixed),
        };
        InvalidOperationException fromTheme = Assert.Throws<InvalidOperationException>(
            () => Show(themed, Bar(margin: new Thickness(8))));
        Assert.Contains(
            "was set by the inherited SnackBarThemeData.",
            fromTheme.Message,
            StringComparison.Ordinal);

        InvalidOperationException byDefault = Assert.Throws<InvalidOperationException>(
            () => Show(ThemeData.Light, Bar(margin: new Thickness(8))));
        Assert.Contains("was set by default.", byDefault.Message, StringComparison.Ordinal);
        Assert.Contains("Margin can only be used with floating behavior.", byDefault.Message, StringComparison.Ordinal);

        var widthTheme = ThemeData.Light with
        {
            SnackBarTheme = new SnackBarThemeData(behavior: SnackBarBehavior.Fixed),
        };
        InvalidOperationException width = Assert.Throws<InvalidOperationException>(
            () => Show(widthTheme, Bar(width: 200)));
        Assert.Contains("Width can only be used with floating behavior.", width.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SnackBar_FloatingWithAWidthCentersTheBarAndDropsHorizontalMargins()
    {
        using var harness = Show(
            ThemeData.Light,
            Bar(width: 200, behavior: SnackBarBehavior.Floating),
            surface: new Size(800, 220));
        SizedBox sized = Assert.Single(FindWidgets<SizedBox>(harness), box => box.Width == 200);
        Assert.Equal(200, sized.Width);

        // The surviving padding carries only the vertical inset padding.
        Assert.Contains(FindWidgets<Padding>(harness), padding =>
            padding.Insets.Resolve(TextDirection.Ltr) == new Thickness(0, 5, 0, 10));
    }

    [Fact]
    public async Task SnackBar_OnVisibleFiresOnceAndNeverForAQueuedBar()
    {
        int visible = 0;
        int queuedVisible = 0;
        using var harness = NewMessenger();
        var messenger = harness.FindState<ScaffoldMessengerState>();

        var first = messenger.ShowSnackBar(Bar(content: "First", onVisible: () => visible++));
        messenger.ShowSnackBar(Bar(content: "Second", onVisible: () => queuedVisible++));
        harness.Pump(new Size(360, 220));
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));

        Assert.Equal(1, visible);
        Assert.Equal(0, queuedVisible);

        // A further frame must not fire it again.
        AnimationPump.Advance(0.2);
        harness.Pump(new Size(360, 220));
        Assert.Equal(1, visible);

        messenger.RemoveCurrentSnackBar();
        harness.Pump(new Size(360, 220));
        await first.Closed;
    }

    [Fact]
    public void SnackBar_AccessibleNavigationDropsEveryTransitionWidget()
    {
        using var normal = Show(ThemeData.Light, Bar(behavior: SnackBarBehavior.Floating));
        Assert.NotEmpty(FindWidgets<FadeTransition>(normal));

        using var accessible = Show(
            ThemeData.Light,
            Bar(behavior: SnackBarBehavior.Floating),
            accessibleNavigation: true);
        Assert.Empty(FindWidgets<FadeTransition>(accessible));
        Assert.Empty(FindWidgets<ValueListenableBuilder<double>>(accessible));
    }

    [Fact]
    public void SnackBar_TransitionShapeFollowsBehaviorAndMaterialVersion()
    {
        // Fixed (M2 and M3) animates its height from the top start.
        using var fixedM3 = Show(ThemeData.Light, Bar());
        Assert.Single(FindWidgets<ValueListenableBuilder<double>>(fixedM3));

        // M2 additionally fades its content out from inside the Material.
        var m2 = ThemeData.Light with { UseMaterial3 = false };
        using var fixedM2 = Show(m2, Bar());
        Assert.Single(FindWidgets<ValueListenableBuilder<double>>(fixedM2));
        Assert.Single(FindWidgets<FadeTransition>(fixedM2));

        // M2 floating fades in and out but has no height animation.
        using var floatingM2 = Show(m2, Bar(behavior: SnackBarBehavior.Floating));
        Assert.Empty(FindWidgets<ValueListenableBuilder<double>>(floatingM2));
        Assert.Equal(2, FindWidgets<FadeTransition>(floatingM2).Count);

        // M3 floating fades in and animates height, with no fade-out layer.
        using var floatingM3 = Show(ThemeData.Light, Bar(behavior: SnackBarBehavior.Floating));
        Assert.Single(FindWidgets<ValueListenableBuilder<double>>(floatingM3));
        Assert.Single(FindWidgets<FadeTransition>(floatingM3));
    }

    [Fact]
    public async Task ScaffoldMessenger_TimeoutClosesWithItsOwnReasonAndPersistSurvivesIt()
    {
        // The timer itself hands back through the host dispatcher, which no unit-test harness
        // pumps; what is asserted here is the decision the timer callback makes, plus the reason
        // it reports. End-to-end expiry is verified at runtime through the sample.
        using var harness = NewMessenger();
        var messenger = harness.FindState<ScaffoldMessengerState>();

        var transient = messenger.ShowSnackBar(Bar(content: "Transient"));
        Assert.False(transient.Feature.Persist);
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));

        messenger.HideCurrentSnackBar(SnackBarClosedReason.Timeout);
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));
        await Task.Yield();
        Assert.Equal(SnackBarClosedReason.Timeout, await transient.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "Transient"));

        // A bar with an action persists by default, so an expiring timer leaves it alone.
        var persistent = messenger.ShowSnackBar(Bar(
            content: "Persistent",
            duration: TimeSpan.FromMilliseconds(20),
            action: Action()));
        Assert.True(persistent.Feature.Persist);
        AnimationPump.Advance(0.4);
        harness.Pump(new Size(360, 220));
        await Task.Delay(60);
        harness.Pump(new Size(360, 220));
        Assert.False(persistent.Closed.IsCompleted);
        Assert.NotNull(FindParagraph(harness.RenderView, "Persistent"));

        messenger.RemoveCurrentSnackBar();
        harness.Pump(new Size(360, 220));
        Assert.True(persistent.Closed.IsCompleted);
        Assert.Equal(SnackBarClosedReason.Remove, await persistent.Closed);
    }

    private WidgetRenderHarness NewMessenger()
    {
        var harness = new WidgetRenderHarness(WrapChrome(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox()))));
        harness.Pump(new Size(360, 220));
        return harness;
    }

    private static SnackBar Bar(
        string content = "Message",
        Color? backgroundColor = null,
        double? elevation = null,
        Thickness? margin = null,
        Thickness? padding = null,
        double? width = null,
        SnackBarBehavior? behavior = null,
        SnackBarAction? action = null,
        double? actionOverflowThreshold = null,
        bool? showCloseIcon = null,
        bool? persist = null,
        TimeSpan? duration = null,
        Action? onVisible = null,
        DismissDirection? dismissDirection = null,
        HitTestBehavior? hitTestBehavior = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) => new(
        content: new Text(content),
        backgroundColor: backgroundColor,
        elevation: elevation,
        margin: margin is null ? null : (EdgeInsetsGeometry)margin.Value,
        padding: padding is null ? null : (EdgeInsetsGeometry)padding.Value,
        width: width,
        behavior: behavior,
        action: action,
        actionOverflowThreshold: actionOverflowThreshold,
        showCloseIcon: showCloseIcon,
        persist: persist,
        duration: duration,
        onVisible: onVisible,
        dismissDirection: dismissDirection,
        hitTestBehavior: hitTestBehavior,
        clipBehavior: clipBehavior,
        key: key);

    private static SnackBarAction Action(string label = "UNDO", Action? onPressed = null) =>
        new(label, onPressed ?? (() => { }));

    /// Mounts a standalone snack bar with an animation already at its end, as the messenger would.
    private static SnackBarHarness Show(
        ThemeData theme,
        SnackBar snackBar,
        Size? surface = null,
        bool accessibleNavigation = false,
        Func<Widget, Widget>? wrap = null)
    {
        Size size = surface ?? new Size(600, 240);
        var controller = new AnimationController(value: 1.0, duration: SnackBar.TransitionDuration);
        Widget child = snackBar.WithAnimation(controller, new UniqueKey());
        if (wrap is not null)
        {
            child = wrap(child);
        }

        var harness = new SnackBarHarness(
            WrapChrome(theme, child, size, accessibleNavigation),
            controller);
        harness.Pump(size);
        return harness;
    }

    private static Widget WrapChrome(
        ThemeData theme,
        Widget child,
        Size? surface = null,
        bool accessibleNavigation = false,
        TextDirection direction = TextDirection.Ltr) =>
        new Directionality(
            direction,
            new MediaQuery(
                new MediaQueryData(
                    Size: surface ?? new Size(600, 240),
                    AccessibleNavigation: accessibleNavigation),
                new Theme(theme, child)));

    private static void Tap(RenderView renderView, Point position, int pointer)
    {
        GestureBinding binding = GestureBinding.Instance;
        DateTime timestamp = DateTime.UtcNow;
        binding.HandlePointerEvent(renderView, new PointerDownEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, timestamp));
        binding.HandlePointerEvent(renderView, new PointerUpEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, timestamp.AddMilliseconds(20)));
    }

    private static Point LocalCenter(RenderObject target)
    {
        Point offset = target.GetPaintOffsetToRoot();
        Size size = ((RenderBox)target).Size;
        return new Point(offset.X + (size.Width / 2), offset.Y + (size.Height / 2));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static List<T> FindWidgets<T>(WidgetRenderHarness harness) where T : Widget =>
        harness.FindWidgets<T>();

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null || predicate(node)) return node;
        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
    }

    private sealed class SnackBarHarness : WidgetRenderHarness
    {
        private readonly AnimationController _controller;

        public SnackBarHarness(Widget rootWidget, AnimationController controller) : base(rootWidget)
        {
            _controller = controller;
        }

        public override void Dispose()
        {
            base.Dispose();
            _controller.Dispose();
        }
    }

    private class WidgetRenderHarness : IDisposable
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

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return Assert.Single(states);
        }

        public List<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            CollectWidgets(_rootElement, widgets);
            return widgets;
        }

        public virtual void Dispose() => _rootElement.Unmount();

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state) states.Add(state);
            element.VisitChildren(child => CollectStates(child, states));
        }

        private static void CollectWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T widget) widgets.Add(widget);
            element.VisitChildren(child => CollectWidgets(child, widgets));
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;
            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            public override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            public override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
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
}
