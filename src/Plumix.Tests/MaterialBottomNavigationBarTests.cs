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
using Transform = Plumix.Widgets.Transform;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialBottomNavigationBarTests
{
    // ---------- Constructor asserts (Dart's initializer list) ----------

    [Fact]
    public void BottomNavigationBar_RequiresAtLeastTwoItems()
    {
        var error = Assert.Throws<ArgumentException>(() => new BottomNavigationBar(
            items:
            [
                new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "Only one"),
            ]));

        Assert.Contains("at least two", error.Message);
    }

    [Fact]
    public void BottomNavigationBar_ItemLabelMustNotBeNull()
    {
        var error = Assert.Throws<ArgumentException>(() => new BottomNavigationBar(
            items:
            [
                new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "One"),
                new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline)),
            ]));

        Assert.Contains("non-null label", error.Message);
    }

    [Fact]
    public void BottomNavigationBar_InvalidCurrentIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomNavigationBar(
            items: TwoItems(),
            currentIndex: 2));
    }

    [Fact]
    public void BottomNavigationBar_SelectedItemColorAndFixedColorTogether_Throws()
    {
        var error = Assert.Throws<ArgumentException>(() => new BottomNavigationBar(
            items: TwoItems(),
            selectedItemColor: Colors.Red,
            fixedColor: Colors.Blue));

        Assert.Contains("but not both", error.Message);
    }

    [Fact]
    public void BottomNavigationBar_NegativeMeasurements_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomNavigationBar(
            items: TwoItems(),
            elevation: -1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomNavigationBar(
            items: TwoItems(),
            iconSize: -1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomNavigationBar(
            items: TwoItems(),
            selectedFontSize: -1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomNavigationBar(
            items: TwoItems(),
            unselectedFontSize: -1.0));
    }

    [Fact]
    public void BottomNavigationBar_FixedColor_IsAnAliasOfSelectedItemColor()
    {
        var bar = new BottomNavigationBar(items: TwoItems(), fixedColor: Colors.OrangeRed);
        Assert.Equal(Colors.OrangeRed, bar.SelectedItemColor);
        Assert.Equal(Colors.OrangeRed, bar.FixedColor);

        var explicitBar = new BottomNavigationBar(items: TwoItems(), selectedItemColor: Colors.SeaGreen);
        Assert.Equal(Colors.SeaGreen, explicitBar.FixedColor);
    }

    // ---------- Defaults ----------

    [Fact]
    public void BottomNavigationBar_FixedDefaults_UseColorSchemeRolesAndElevationEight()
    {
        ThemeData theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with { Primary = Colors.OrangeRed },
            UnselectedWidgetColor = Colors.CadetBlue,
        };

        using var scope = MountBar(theme, new BottomNavigationBar(currentIndex: 1, items: TwoItems()));

        Assert.Equal(Colors.OrangeRed, LabelColor(scope, "Second"));
        Assert.Equal(Colors.CadetBlue, LabelColor(scope, "First"));

        // Both labels render at selectedFontSize; the unselected one is scaled down instead.
        Assert.Equal(14.0, LabelParagraph(scope, "Second").FontSize);
        Assert.Equal(14.0, LabelParagraph(scope, "First").FontSize);
        Assert.Equal(12.0 / 14.0, LabelScale(scope, "First"), 6);
        Assert.Equal(1.0, LabelScale(scope, "Second"), 6);

        // A fixed bar shows both label sets by default, so no fade/visibility wrapper appears.
        Assert.Empty(InLabels<FadeTransition>(scope));
        Assert.Empty(InLabels<Visibility>(scope));

        Assert.Equal(8.0, BarMaterial(scope).Elevation);
    }

    [Fact]
    public void BottomNavigationBar_DarkFixedDefault_UsesColorSchemeSecondary()
    {
        ThemeData theme = ThemeData.Dark with
        {
            ColorScheme = ThemeData.Dark.ColorScheme with { Secondary = Colors.Gold },
        };

        using var scope = MountBar(theme, new BottomNavigationBar(currentIndex: 0, items: TwoItems()));

        Assert.Equal(Colors.Gold, LabelColor(scope, "First"));
    }

    [Fact]
    public void BottomNavigationBar_ShiftingDefaults_UseColorSchemeSurfaceAndHideUnselectedLabels()
    {
        ThemeData theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with { Surface = Colors.MediumPurple },
        };

        using var scope = MountBar(
            theme,
            new BottomNavigationBar(
                type: BottomNavigationBarType.Shifting,
                currentIndex: 0,
                items: TwoItems()));

        Assert.Equal(Colors.MediumPurple, LabelColor(scope, "First"));
        Assert.Equal(14.0, LabelParagraph(scope, "First").FontSize);

        // showUnselectedLabels defaults to false for shifting, so labels fade with selection.
        List<FadeTransition> fades = InLabels<FadeTransition>(scope);
        Assert.Equal(2, fades.Count);
        Assert.Equal(1.0, fades[0].Opacity.Value, 6);
        Assert.Equal(0.0, fades[1].Opacity.Value, 6);

        Assert.Equal(8.0, BarMaterial(scope).Elevation);
    }

    [Fact]
    public void BottomNavigationBar_AutomaticType_IsFixedUpToThreeItemsAndShiftingBeyond()
    {
        using (var three = MountBar(ThemeData.Light, new BottomNavigationBar(items: Items(3))))
        {
            Assert.Empty(InLabels<FadeTransition>(three));
        }

        using var four = MountBar(ThemeData.Light, new BottomNavigationBar(items: Items(4)));
        Assert.Equal(4, InLabels<FadeTransition>(four).Count);
    }

    [Fact]
    public void BottomNavigationBar_ThemeTypeOverridesAutomaticType()
    {
        var bottomTheme = new BottomNavigationBarThemeData(Type: BottomNavigationBarType.Shifting);

        using var scope = MountBar(
            ThemeData.Light with { BottomNavigationBarTheme = bottomTheme },
            new BottomNavigationBar(items: TwoItems()));

        // Two items resolve to `fixed` automatically; the theme forces `shifting`, which hides
        // unselected labels by default.
        Assert.Equal(2, InLabels<FadeTransition>(scope).Count);
    }

    // ---------- Label styles and font sizes ----------

    [Fact]
    public void BottomNavigationBar_CustomLabelStyles_ApplyAndDriveTheUnselectedScale()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                selectedLabelStyle: new TextStyle(FontSize: 18.0, FontWeight: FontWeight.Bold),
                unselectedLabelStyle: new TextStyle(FontSize: 12.0, FontWeight: FontWeight.Normal)));

        Assert.Equal(18.0, LabelParagraph(scope, "First").FontSize);
        Assert.Equal(18.0, LabelParagraph(scope, "Second").FontSize);
        Assert.Equal(FontWeight.Bold, LabelParagraph(scope, "First").FontWeight);
        Assert.Equal(12.0 / 18.0, LabelScale(scope, "Second"), 6);
    }

    [Fact]
    public void BottomNavigationBar_LabelStyleFontSize_OverridesFontSizeParameters()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                selectedFontSize: 17.0,
                selectedLabelStyle: new TextStyle(FontSize: 18.0)));

        Assert.Equal(18.0, LabelParagraph(scope, "First").FontSize);
        Assert.Equal(12.0 / 18.0, LabelScale(scope, "Second"), 6);
    }

    [Fact]
    public void BottomNavigationBar_LabelStyleColors_OverrideItemColors_WhenLegacyColorSchemeIsOff()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                useLegacyColorScheme: false,
                selectedItemColor: Colors.OrangeRed,
                unselectedItemColor: Colors.CadetBlue,
                selectedLabelStyle: new TextStyle(Color: Colors.IndianRed),
                unselectedLabelStyle: new TextStyle(Color: Colors.SeaGreen)));

        Assert.Equal(Colors.IndianRed, LabelColor(scope, "First"));
        Assert.Equal(Colors.SeaGreen, LabelColor(scope, "Second"));
    }

    [Fact]
    public void BottomNavigationBar_LegacyColorScheme_KeepsItemColorsOnLabels()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                selectedItemColor: Colors.OrangeRed,
                unselectedItemColor: Colors.CadetBlue,
                selectedLabelStyle: new TextStyle(Color: Colors.IndianRed),
                unselectedLabelStyle: new TextStyle(Color: Colors.SeaGreen)));

        // `useLegacyColorScheme` defaults to true, so the shared colorTween wins over label styles.
        Assert.Equal(Colors.OrangeRed, LabelColor(scope, "First"));
        Assert.Equal(Colors.CadetBlue, LabelColor(scope, "Second"));
    }

    // ---------- Icon themes ----------

    [Fact]
    public void BottomNavigationBar_CustomIconThemes_DriveIconSizeAndColor()
    {
        IconThemeData? selectedIconTheme = null;
        IconThemeData? unselectedIconTheme = null;

        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items:
                [
                    new BottomNavigationBarItem(
                        icon: new CaptureIconThemeWidget(data => selectedIconTheme = data),
                        label: "First"),
                    new BottomNavigationBarItem(
                        icon: new CaptureIconThemeWidget(data => unselectedIconTheme = data),
                        label: "Second"),
                ],
                selectedIconTheme: new IconThemeData(Color: Colors.OrangeRed, Size: 36.0),
                unselectedIconTheme: new IconThemeData(Color: Colors.CadetBlue, Size: 18.0)));

        Assert.NotNull(selectedIconTheme);
        Assert.NotNull(unselectedIconTheme);
        Assert.Equal(36.0, selectedIconTheme!.Size);
        Assert.Equal(Colors.OrangeRed, selectedIconTheme.Color);
        Assert.Equal(18.0, unselectedIconTheme!.Size);
        Assert.Equal(Colors.CadetBlue, unselectedIconTheme.Color);
    }

    [Fact]
    public void BottomNavigationBar_IconTheme_MayBeSuppliedForOneStateOnly()
    {
        IconThemeData? selectedIconTheme = null;

        // Dart has no "both or neither" assert — a lone selectedIconTheme is legal.
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items:
                [
                    new BottomNavigationBarItem(
                        icon: new CaptureIconThemeWidget(data => selectedIconTheme = data),
                        label: "First"),
                    new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                ],
                selectedIconTheme: new IconThemeData(Color: Colors.Red, Size: 20.0)));

        Assert.NotNull(selectedIconTheme);
        Assert.Equal(20.0, selectedIconTheme!.Size);
    }

    [Fact]
    public void BottomNavigationBar_IconSize_FeedsTheAmbientIconTheme()
    {
        IconThemeData? captured = null;

        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                iconSize: 12.0,
                items:
                [
                    new BottomNavigationBarItem(
                        icon: new CaptureIconThemeWidget(data => captured = data),
                        label: "First"),
                    new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                ]));

        Assert.NotNull(captured);
        Assert.Equal(12.0, captured!.Size);
    }

    // ---------- Tile padding math ----------

    [Fact]
    public void BottomNavigationBar_TilePadding_AllLabels()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                selectedFontSize: 16.0,
                selectedIconTheme: new IconThemeData(Size: 36.0),
                unselectedIconTheme: new IconThemeData(Size: 20.0)));

        List<Thickness> insets = TilePaddings(scope);
        Assert.Equal(8.0, insets[0].Top, 6);
        Assert.Equal(8.0, insets[0].Bottom, 6);
        Assert.Equal(16.0, insets[1].Top, 6);
        Assert.Equal(16.0, insets[1].Bottom, 6);
    }

    [Fact]
    public void BottomNavigationBar_TilePadding_SelectedLabelsOnly()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                showUnselectedLabels: false,
                selectedFontSize: 16.0,
                selectedIconTheme: new IconThemeData(Size: 36.0),
                unselectedIconTheme: new IconThemeData(Size: 20.0)));

        List<Thickness> insets = TilePaddings(scope);
        Assert.Equal(8.0, insets[0].Top, 6);
        Assert.Equal(8.0, insets[0].Bottom, 6);
        Assert.Equal(24.0, insets[1].Top, 6);
        Assert.Equal(8.0, insets[1].Bottom, 6);
    }

    [Fact]
    public void BottomNavigationBar_TilePadding_NoLabels()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                showSelectedLabels: false,
                showUnselectedLabels: false,
                selectedFontSize: 16.0,
                selectedIconTheme: new IconThemeData(Size: 36.0),
                unselectedIconTheme: new IconThemeData(Size: 20.0)));

        List<Thickness> insets = TilePaddings(scope);
        Assert.Equal(16.0, insets[0].Top, 6);
        Assert.Equal(0.0, insets[0].Bottom, 6);
        Assert.Equal(24.0, insets[1].Top, 6);
        Assert.Equal(8.0, insets[1].Bottom, 6);
    }

    // ---------- Label visibility ----------

    [Fact]
    public void BottomNavigationBar_HidingBothLabels_UsesMaintainedVisibility()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                items: TwoItems(),
                showSelectedLabels: false,
                showUnselectedLabels: false));

        List<Visibility> hidden = InLabels<Visibility>(scope);
        Assert.Equal(2, hidden.Count);
        Assert.All(hidden, visibility => Assert.False(visibility.Visible));

        // Visibility.maintain keeps the labels in the tree.
        Assert.NotNull(FindParagraphByText(scope.RenderRoot, "First"));
        Assert.NotNull(FindParagraphByText(scope.RenderRoot, "Second"));
    }

    [Fact]
    public void BottomNavigationBar_HidingSelectedLabels_FadesTheSelectedTileOut()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                showSelectedLabels: false,
                showUnselectedLabels: true));

        List<FadeTransition> fades = InLabels<FadeTransition>(scope);
        Assert.Equal(2, fades.Count);
        Assert.Equal(0.0, fades[0].Opacity.Value, 6);
        Assert.Equal(1.0, fades[1].Opacity.Value, 6);
    }

    [Fact]
    public void BottomNavigationBar_HidingUnselectedLabels_FadesTheUnselectedTileOut()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                currentIndex: 0,
                items: TwoItems(),
                showSelectedLabels: true,
                showUnselectedLabels: false));

        List<FadeTransition> fades = InLabels<FadeTransition>(scope);
        Assert.Equal(2, fades.Count);
        Assert.Equal(1.0, fades[0].Opacity.Value, 6);
        Assert.Equal(0.0, fades[1].Opacity.Value, 6);
    }

    // ---------- Background and elevation ----------

    [Fact]
    public void BottomNavigationBar_FixedBackgroundColor_ReachesTheBarMaterial()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(items: TwoItems(), backgroundColor: Colors.DarkSlateBlue));

        Assert.Equal(Colors.DarkSlateBlue, BarMaterial(scope).Color);
    }

    [Fact]
    public void BottomNavigationBar_ShiftingBackground_IsOverriddenByTheSelectedItemColor()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                type: BottomNavigationBarType.Shifting,
                currentIndex: 0,
                backgroundColor: Colors.Blue,
                items:
                [
                    new BottomNavigationBarItem(
                        icon: new Icon(Icons.Menu),
                        label: "First",
                        backgroundColor: Colors.Yellow),
                    new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                ]));

        Assert.Equal(Colors.Yellow, BarMaterial(scope).Color);
    }

    [Fact]
    public void BottomNavigationBar_Elevation_ReachesTheBarMaterial()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(items: TwoItems(), elevation: 3.0));

        Assert.Equal(3.0, BarMaterial(scope).Elevation);
    }

    [Fact]
    public void BottomNavigationBar_Height_IsBarHeightPlusBottomViewPadding()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(items: TwoItems(), selectedFontSize: 8.0),
                viewPadding: new Thickness(0, 0, 0, 40)));

        harness.Pump(new Size(800, 600));
        Assert.Equal(96.0, harness.RenderView.Child!.Size.Height, 3);
    }

    [Fact]
    public void BottomNavigationBar_Height_IgnoresViewInsets()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(items: TwoItems(), selectedFontSize: 8.0),
                viewPadding: new Thickness(0, 0, 0, 40),
                viewInsets: new Thickness(0, 0, 0, 336)));

        harness.Pump(new Size(800, 600));
        Assert.Equal(96.0, harness.RenderView.Child!.Size.Height, 3);
    }

    // ---------- Interaction ----------

    [Fact]
    public void BottomNavigationBar_OnTap_InvokesCallbackWithTappedIndex()
    {
        var taps = new List<int>();

        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(items: Items(3), onTap: taps.Add));

        List<InkResponse> responses = FindWidgets<InkResponse>(scope.Root);
        Assert.Equal(3, responses.Count);
        responses[2].OnTap!.Invoke();

        Assert.Equal([2], taps);
    }

    [Fact]
    public void BottomNavigationBar_SelectedItem_UsesActiveIcon()
    {
        var idleIcon = new Icon(Icons.InfoOutline);
        var activeIcon = new Icon(Icons.Menu);

        var items = (IReadOnlyList<BottomNavigationBarItem>)
        [
            new BottomNavigationBarItem(icon: idleIcon, activeIcon: activeIcon, label: "First"),
            new BottomNavigationBarItem(icon: new Icon(Icons.Check), label: "Second"),
        ];

        using (var selected = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(items: items, currentIndex: 0)))
        {
            List<Icon> icons = FindWidgets<Icon>(selected.Root);
            Assert.Contains(activeIcon, icons);
            Assert.DoesNotContain(idleIcon, icons);
        }

        using var unselected = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(items: items, currentIndex: 1));
        List<Icon> unselectedIcons = FindWidgets<Icon>(unselected.Root);
        Assert.Contains(idleIcon, unselectedIcons);
        Assert.DoesNotContain(activeIcon, unselectedIcons);
    }

    [Fact]
    public void BottomNavigationBar_ShiftingSelectionChange_AnimatesTileWidths()
    {
        Scheduler.ResetForTests();
        try
        {
            Widget Build(int currentIndex) => WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    type: BottomNavigationBarType.Shifting,
                    currentIndex: currentIndex,
                    items: TwoItems()),
                size: new Size(800, 600));

            using var harness = new WidgetRenderHarness(Build(0));
            harness.Pump(new Size(800, 600));

            // Shifting flex is 1.5 for the selected tile and 1.0 for the others.
            List<double> initial = GetBottomNavigationTileWidths(harness.RenderView.Child, 2);
            Assert.Equal(480.0, initial[0], 3);
            Assert.Equal(320.0, initial[1], 3);

            harness.UpdateRootWidget(Build(1));
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.60));
            harness.Pump(new Size(800, 600));

            List<double> settled = GetBottomNavigationTileWidths(harness.RenderView.Child, 2);
            Assert.Equal(320.0, settled[0], 3);
            Assert.Equal(480.0, settled[1], 3);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void BottomNavigationBar_ItemCountChange_RebuildsWithoutStaleTiles()
    {
        Widget Build(int count) => WrapWithThemeAndMediaQuery(
            ThemeData.Light,
            new BottomNavigationBar(items: Items(count)),
            size: new Size(800, 600));

        using var harness = new WidgetRenderHarness(Build(3));
        harness.Pump(new Size(800, 600));
        Assert.NotNull(FindParagraphByText(harness.RenderView.Child, "Item 3"));

        harness.UpdateRootWidget(Build(4));
        harness.Pump(new Size(800, 600));
        Assert.NotNull(FindParagraphByText(harness.RenderView.Child, "Item 4"));

        harness.UpdateRootWidget(Build(2));
        harness.Pump(new Size(800, 600));
        Assert.NotNull(FindParagraphByText(harness.RenderView.Child, "Item 2"));
        Assert.Null(FindParagraphByText(harness.RenderView.Child, "Item 3"));
        Assert.Null(FindParagraphByText(harness.RenderView.Child, "Item 4"));
    }

    // ---------- Mouse cursor ----------

    [Fact]
    public void BottomNavigationBar_MouseCursor_DefaultsToClickable()
    {
        using var scope = MountBar(ThemeData.Light, new BottomNavigationBar(items: TwoItems()));

        Assert.All(
            FindWidgets<InkResponse>(scope.Root),
            response => Assert.Equal(SystemMouseCursors.Click, response.MouseCursor));
    }

    [Fact]
    public void BottomNavigationBar_MouseCursor_UsesTheWidgetValue()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(items: TwoItems(), mouseCursor: SystemMouseCursors.Text));

        Assert.All(
            FindWidgets<InkResponse>(scope.Root),
            response => Assert.Equal(SystemMouseCursors.Text, response.MouseCursor));
    }

    [Fact]
    public void BottomNavigationBar_MouseCursor_ResolvesAStatefulWidgetCursor()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                items: TwoItems(),
                currentIndex: 0,
                mouseCursor: WidgetStateMouseCursor.ResolveWith(states =>
                    states.Contains(WidgetState.Selected)
                        ? SystemMouseCursors.Grab
                        : SystemMouseCursors.Forbidden)));

        List<InkResponse> responses = FindWidgets<InkResponse>(scope.Root);
        Assert.Equal(SystemMouseCursors.Grab, responses[0].MouseCursor);
        Assert.Equal(SystemMouseCursors.Forbidden, responses[1].MouseCursor);
    }

    [Fact]
    public void BottomNavigationBar_MouseCursor_ResolvesTheThemeStateProperty()
    {
        var bottomTheme = new BottomNavigationBarThemeData(
            MouseCursor: WidgetStateProperty<MouseCursor?>.ResolveWith(states =>
                states.Contains(WidgetState.Selected)
                    ? SystemMouseCursors.Grab
                    : SystemMouseCursors.Grabbing));

        using var scope = MountBar(
            ThemeData.Light with { BottomNavigationBarTheme = bottomTheme },
            new BottomNavigationBar(items: TwoItems(), currentIndex: 0));

        List<InkResponse> responses = FindWidgets<InkResponse>(scope.Root);
        Assert.Equal(SystemMouseCursors.Grab, responses[0].MouseCursor);
        Assert.Equal(SystemMouseCursors.Grabbing, responses[1].MouseCursor);
    }

    // ---------- Feedback ----------

    [Fact]
    public void BottomNavigationBar_EnableFeedback_DefaultsToTrue()
    {
        using var scope = MountBar(ThemeData.Light, new BottomNavigationBar(items: TwoItems()));

        Assert.All(FindWidgets<InkResponse>(scope.Root), response => Assert.True(response.EnableFeedback));
    }

    [Fact]
    public void BottomNavigationBar_EnableFeedback_ComesFromTheThemeWhenUnset()
    {
        var bottomTheme = new BottomNavigationBarThemeData(EnableFeedback: false);

        using var scope = MountBar(
            ThemeData.Light with { BottomNavigationBarTheme = bottomTheme },
            new BottomNavigationBar(items: TwoItems()));

        Assert.All(FindWidgets<InkResponse>(scope.Root), response => Assert.False(response.EnableFeedback));
    }

    [Fact]
    public void BottomNavigationBar_EnableFeedback_OverridesTheThemeValue()
    {
        var bottomTheme = new BottomNavigationBarThemeData(EnableFeedback: false);

        using var scope = MountBar(
            ThemeData.Light with { BottomNavigationBarTheme = bottomTheme },
            new BottomNavigationBar(items: TwoItems(), enableFeedback: true));

        Assert.All(FindWidgets<InkResponse>(scope.Root), response => Assert.True(response.EnableFeedback));
    }

    // ---------- Tooltips ----------

    [Fact]
    public void BottomNavigationBar_Tooltip_IsOmittedForNullAndEmptyMessages()
    {
        using var scope = MountBar(
            ThemeData.Light,
            new BottomNavigationBar(
                items:
                [
                    new BottomNavigationBarItem(
                        icon: new Icon(Icons.Menu),
                        label: "First",
                        tooltip: "A tooltip"),
                    new BottomNavigationBarItem(
                        icon: new Icon(Icons.InfoOutline),
                        label: "Second",
                        tooltip: string.Empty),
                    new BottomNavigationBarItem(icon: new Icon(Icons.Check), label: "Third"),
                ]));

        List<Tooltip> tooltips = FindWidgets<Tooltip>(scope.Root);
        Assert.Single(tooltips);
        Assert.Equal("A tooltip", tooltips[0].Message);
        Assert.False(tooltips[0].PreferBelow);
        Assert.Equal(24.0 + 14.0, tooltips[0].VerticalOffset);
    }

    // ---------- Landscape layouts ----------

    [Fact]
    public void BottomNavigationBar_SpreadLandscapeLayout_UsesTheWholeWidth()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(items: TwoItems()),
                size: new Size(800, 600)));

        harness.Pump(new Size(800, 600));

        Assert.Equal(800.0, FindBottomNavigationRow(harness.RenderView.Child, 2)!.Size.Width, 3);
    }

    [Fact]
    public void BottomNavigationBar_CenteredLandscapeLayout_ConstrainsTheBarToThePortraitWidth()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    items: TwoItems(),
                    landscapeLayout: BottomNavigationBarLandscapeLayout.Centered),
                size: new Size(800, 600)));

        harness.Pump(new Size(800, 600));

        // `centered` constrains the row to the portrait width, which is the view's height.
        Assert.Equal(600.0, FindBottomNavigationRow(harness.RenderView.Child, 2)!.Size.Width, 3);
    }

    [Fact]
    public void BottomNavigationBar_LinearLandscapeLayout_PutsIconAndLabelInARow()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    items: TwoItems(),
                    landscapeLayout: BottomNavigationBarLandscapeLayout.Linear),
                size: new Size(800, 600)));

        harness.Pump(new Size(800, 600));

        // The bar row plus one row per tile; the default column layout produces only the bar row.
        Assert.Equal(3, CountHorizontalFlexes(harness.RenderView.Child));
    }

    [Fact]
    public void BottomNavigationBar_LandscapeLayout_IsIgnoredInPortrait()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    items: TwoItems(),
                    landscapeLayout: BottomNavigationBarLandscapeLayout.Linear),
                size: new Size(600, 800)));

        harness.Pump(new Size(600, 800));

        Assert.Equal(1, CountHorizontalFlexes(harness.RenderView.Child));
    }

    [Fact]
    public void BottomNavigationBar_LandscapeLayout_ComesFromTheThemeWhenUnset()
    {
        var bottomTheme = new BottomNavigationBarThemeData(
            LandscapeLayout: BottomNavigationBarLandscapeLayout.Centered);

        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light with { BottomNavigationBarTheme = bottomTheme },
                new BottomNavigationBar(items: TwoItems()),
                size: new Size(800, 600)));

        harness.Pump(new Size(800, 600));

        Assert.Equal(600.0, FindBottomNavigationRow(harness.RenderView.Child, 2)!.Size.Width, 3);
    }

    // ---------- Semantics ----------

    [Fact]
    public void BottomNavigationBar_Semantics_ExposeButtonSelectionAndIndexLabel()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(items: Items(3), currentIndex: 0, onTap: _ => { }),
                size: new Size(800, 600)));

        SemanticsNode? root = harness.PumpAndGetSemantics(new Size(800, 600));
        Assert.NotNull(root);

        SemanticsNode? first = FindFirstSemanticsNode(root!, node => HasLabelPart(node, "Item 1"));
        Assert.NotNull(first);
        Assert.True(HasLabelPart(first!, "Tab 1 of 3"));
        Assert.True(first!.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(first.Flags.HasFlag(SemanticsFlags.IsSelected));

        SemanticsNode? second = FindFirstSemanticsNode(root!, node => HasLabelPart(node, "Item 2"));
        Assert.NotNull(second);
        Assert.True(HasLabelPart(second!, "Tab 2 of 3"));
        Assert.False(second!.Flags.HasFlag(SemanticsFlags.IsSelected));
    }

    [Fact]
    public void BottomNavigationBar_HiddenLabels_StillCarrySemantics()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    items: TwoItems(),
                    currentIndex: 0,
                    showSelectedLabels: false,
                    showUnselectedLabels: false),
                size: new Size(800, 600)));

        SemanticsNode? root = harness.PumpAndGetSemantics(new Size(800, 600));
        Assert.NotNull(root);
        Assert.NotNull(FindFirstSemanticsNode(root!, node => HasLabelPart(node, "First")));
        Assert.NotNull(FindFirstSemanticsNode(root!, node => HasLabelPart(node, "Second")));
    }

    [Fact]
    public void BottomNavigationBar_ItemSemanticsLabel_ReplacesTheVisibleLabel()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 0,
                    items:
                    [
                        new BottomNavigationBarItem(
                            icon: new Icon(Icons.Menu),
                            label: "First",
                            semanticsLabel: "Custom A label"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                    ]),
                size: new Size(800, 600)));

        SemanticsNode? root = harness.PumpAndGetSemantics(new Size(800, 600));
        Assert.NotNull(root);
        Assert.NotNull(FindFirstSemanticsNode(root!, node => HasLabelPart(node, "Custom A label")));
        Assert.Null(FindFirstSemanticsNode(root!, node => HasLabelPart(node, "First")));
    }

    [Fact]
    public void BottomNavigationBar_SemanticsIndexLabel_UsesMaterialLocalizationsOverride()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(items: TwoItems(), currentIndex: 0),
                localizations: new TestMaterialLocalizations(),
                size: new Size(800, 600)));

        SemanticsNode? root = harness.PumpAndGetSemantics(new Size(800, 600));
        Assert.NotNull(root);
        Assert.NotNull(FindFirstSemanticsNode(root!, node => HasLabelPart(node, "Section 1 / 2")));
    }

    // ---------- Theme data ----------

    [Fact]
    public void BottomNavigationBarThemeData_CopyWith_CarriesEveryField()
    {
        WidgetStateProperty<MouseCursor?> cursor =
            WidgetStateProperty<MouseCursor?>.All(SystemMouseCursors.Text);
        var source = new BottomNavigationBarThemeData();

        BottomNavigationBarThemeData copy = source.CopyWith(
            backgroundColor: Colors.Red,
            elevation: 10.0,
            selectedIconTheme: new IconThemeData(Size: 1.0),
            unselectedIconTheme: new IconThemeData(Size: 2.0),
            selectedItemColor: Colors.Green,
            unselectedItemColor: Colors.Blue,
            selectedLabelStyle: new TextStyle(FontSize: 3.0),
            unselectedLabelStyle: new TextStyle(FontSize: 4.0),
            showSelectedLabels: true,
            showUnselectedLabels: true,
            type: BottomNavigationBarType.Fixed,
            enableFeedback: false,
            landscapeLayout: BottomNavigationBarLandscapeLayout.Linear,
            mouseCursor: cursor);

        Assert.Equal(Colors.Red, copy.BackgroundColor);
        Assert.Equal(10.0, copy.Elevation);
        Assert.Equal(1.0, copy.SelectedIconTheme!.Size);
        Assert.Equal(2.0, copy.UnselectedIconTheme!.Size);
        Assert.Equal(Colors.Green, copy.SelectedItemColor);
        Assert.Equal(Colors.Blue, copy.UnselectedItemColor);
        Assert.Equal(3.0, copy.SelectedLabelStyle!.FontSize);
        Assert.Equal(4.0, copy.UnselectedLabelStyle!.FontSize);
        Assert.True(copy.ShowSelectedLabels);
        Assert.True(copy.ShowUnselectedLabels);
        Assert.Equal(BottomNavigationBarType.Fixed, copy.Type);
        Assert.False(copy.EnableFeedback);
        Assert.Equal(BottomNavigationBarLandscapeLayout.Linear, copy.LandscapeLayout);
        Assert.Same(cursor, copy.MouseCursor);

        // An all-null copyWith leaves the source untouched and equal.
        Assert.Equal(source, source.CopyWith());
        Assert.Equal(source.GetHashCode(), source.CopyWith().GetHashCode());
    }

    [Fact]
    public void BottomNavigationBarThemeData_Lerp_SnapsDiscreteFieldsAtTheHalfwayPoint()
    {
        var a = new BottomNavigationBarThemeData(
            Elevation: 0.0,
            Type: BottomNavigationBarType.Fixed,
            EnableFeedback: true,
            LandscapeLayout: BottomNavigationBarLandscapeLayout.Spread);
        var b = new BottomNavigationBarThemeData(
            Elevation: 10.0,
            Type: BottomNavigationBarType.Shifting,
            EnableFeedback: false,
            LandscapeLayout: BottomNavigationBarLandscapeLayout.Linear);

        BottomNavigationBarThemeData quarter = BottomNavigationBarThemeData.Lerp(a, b, 0.25);
        Assert.Equal(2.5, quarter.Elevation!.Value, 6);
        Assert.Equal(BottomNavigationBarType.Fixed, quarter.Type);
        Assert.True(quarter.EnableFeedback);
        Assert.Equal(BottomNavigationBarLandscapeLayout.Spread, quarter.LandscapeLayout);

        BottomNavigationBarThemeData most = BottomNavigationBarThemeData.Lerp(a, b, 0.75);
        Assert.Equal(BottomNavigationBarType.Shifting, most.Type);
        Assert.False(most.EnableFeedback);
        Assert.Equal(BottomNavigationBarLandscapeLayout.Linear, most.LandscapeLayout);

        // Dart's identity short-circuit.
        Assert.Same(a, BottomNavigationBarThemeData.Lerp(a, a, 0.5));
    }

    [DebugOnlyFact]
    public void BottomNavigationBarThemeData_DebugFillProperties_ListsEveryField()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new BottomNavigationBarThemeData(
            BackgroundColor: Colors.Red,
            Elevation: 10.0,
            SelectedIconTheme: new IconThemeData(Size: 1.0),
            UnselectedIconTheme: new IconThemeData(Size: 2.0),
            SelectedItemColor: Colors.Green,
            UnselectedItemColor: Colors.Blue,
            SelectedLabelStyle: new TextStyle(FontSize: 3.0),
            UnselectedLabelStyle: new TextStyle(FontSize: 4.0),
            ShowSelectedLabels: true,
            ShowUnselectedLabels: true,
            Type: BottomNavigationBarType.Fixed,
            EnableFeedback: false,
            LandscapeLayout: BottomNavigationBarLandscapeLayout.Linear,
            MouseCursor: WidgetStateProperty<MouseCursor?>.All(SystemMouseCursors.Text))
            .DebugFillProperties(properties);

        Assert.Equal(
            [
                "backgroundColor",
                "elevation",
                "selectedIconTheme",
                "unselectedIconTheme",
                "selectedItemColor",
                "unselectedItemColor",
                "selectedLabelStyle",
                "unselectedLabelStyle",
                "showSelectedLabels",
                "showUnselectedLabels",
                "type",
                "enableFeedback",
                "landscapeLayout",
                "mouseCursor",
            ],
            properties.Properties.Select(property => property.Name).ToList());
    }

    [Fact]
    public void BottomNavigationBarThemeData_DefaultInstance_HasNoNonNullProperties()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new BottomNavigationBarThemeData().DebugFillProperties(properties);

        Assert.Empty(properties.Properties.Where(property => property.Value is not null));
    }

    [Fact]
    public void BottomNavigationBarTheme_Of_PrefersTheNearestThemeOverThemeData()
    {
        var ambient = new BottomNavigationBarThemeData(Elevation: 4.0);
        var local = new BottomNavigationBarThemeData(Elevation: 9.0);
        BottomNavigationBarThemeData? seenAmbient = null;
        BottomNavigationBarThemeData? seenLocal = null;

        using var scope = Mount(
            new MediaQuery(
                data: new MediaQueryData(Size: new Size(800, 600)),
                child: new Theme(
                    data: ThemeData.Light with { BottomNavigationBarTheme = ambient },
                    child: new Column(children:
                    [
                        new Builder(context =>
                        {
                            seenAmbient = BottomNavigationBarTheme.Of(context);
                            return new SizedBox();
                        }),
                        new BottomNavigationBarTheme(
                            data: local,
                            child: new Builder(context =>
                            {
                                seenLocal = BottomNavigationBarTheme.Of(context);
                                return new SizedBox();
                            })),
                    ]))));

        Assert.Equal(ambient, seenAmbient);
        Assert.Equal(local, seenLocal);
    }

    [Fact]
    public void BottomNavigationBar_ThemeDataDefaults_AreUsed_WhenWidgetValuesAreNull()
    {
        var bottomTheme = new BottomNavigationBarThemeData(
            BackgroundColor: Colors.DarkSlateBlue,
            Elevation: 9.0,
            SelectedItemColor: Colors.OrangeRed,
            UnselectedItemColor: Colors.CadetBlue,
            ShowSelectedLabels: true,
            ShowUnselectedLabels: true,
            Type: BottomNavigationBarType.Fixed);

        using var scope = MountBar(
            ThemeData.Light with { BottomNavigationBarTheme = bottomTheme },
            new BottomNavigationBar(items: TwoItems(), currentIndex: 0));

        Assert.Equal(Colors.DarkSlateBlue, BarMaterial(scope).Color);
        Assert.Equal(9.0, BarMaterial(scope).Elevation);
        Assert.Equal(Colors.OrangeRed, LabelColor(scope, "First"));
        Assert.Equal(Colors.CadetBlue, LabelColor(scope, "Second"));
    }

    [Fact]
    public void BottomNavigationBar_WidgetValues_OverrideThemeDefaults()
    {
        var bottomTheme = new BottomNavigationBarThemeData(
            BackgroundColor: Colors.DarkSlateBlue,
            Elevation: 9.0,
            SelectedItemColor: Colors.OrangeRed,
            UnselectedItemColor: Colors.CadetBlue,
            Type: BottomNavigationBarType.Shifting);

        using var scope = MountBar(
            ThemeData.Light with { BottomNavigationBarTheme = bottomTheme },
            new BottomNavigationBar(
                items: TwoItems(),
                currentIndex: 0,
                backgroundColor: Colors.Pink,
                elevation: 7.0,
                selectedItemColor: Colors.Purple,
                unselectedItemColor: Colors.Teal,
                type: BottomNavigationBarType.Fixed));

        Assert.Equal(Colors.Pink, BarMaterial(scope).Color);
        Assert.Equal(7.0, BarMaterial(scope).Elevation);
        Assert.Equal(Colors.Purple, LabelColor(scope, "First"));
        Assert.Equal(Colors.Teal, LabelColor(scope, "Second"));
    }

    // ---------- Fixtures and lookups ----------

    private static IReadOnlyList<BottomNavigationBarItem> TwoItems() =>
    [
        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
    ];

    private static IReadOnlyList<BottomNavigationBarItem> Items(int count)
    {
        var items = new List<BottomNavigationBarItem>(count);
        for (int index = 0; index < count; index++)
        {
            items.Add(new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: $"Item {index + 1}"));
        }

        return items;
    }

    private static MountedScope MountBar(ThemeData theme, BottomNavigationBar bar) =>
        Mount(WrapWithThemeAndMediaQuery(theme, bar));

    private static MountedScope Mount(Widget widget)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(TextDirection.Ltr, child: widget));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        return new MountedScope(root);
    }

    private static MaterialWidget BarMaterial(MountedScope scope) =>
        FindWidgets<MaterialWidget>(scope.Root).First(material => material.Type != MaterialType.Transparency);

    private static RenderParagraph LabelParagraph(MountedScope scope, string text)
    {
        RenderParagraph? paragraph = FindParagraphByText(scope.RenderRoot, text);
        Assert.NotNull(paragraph);
        return paragraph!;
    }

    private static Color LabelColor(MountedScope scope, string text) =>
        Assert.IsType<SolidColorBrush>(LabelParagraph(scope, text).Foreground).Color;

    /// <summary>The uniform scale of the `Transform` that Dart's `_Label` puts around its `Text`.</summary>
    private static double LabelScale(MountedScope scope, string text)
    {
        Element? labelElement = FindElement(
            scope.Root,
            element => element.Widget is Text label && label.Data == text);
        Assert.NotNull(labelElement);

        for (Element? current = labelElement; current is not null; current = ParentOf(scope.Root, current))
        {
            if (current.Widget is Transform transform)
            {
                return transform.Matrix[0];
            }
        }

        throw new InvalidOperationException($"No Transform above the label '{text}'.");
    }

    /// <summary>Every tile's animated icon/label padding, in item order.</summary>
    private static List<Thickness> TilePaddings(MountedScope scope)
    {
        var insets = new List<Thickness>();
        foreach (Element tile in FindElements(scope.Root, element => element.Widget is BottomNavigationTile))
        {
            Element? padding = FindElement(tile, element => element.Widget is Padding);
            Assert.NotNull(padding);
            insets.Add(((Padding)padding!.Widget).Insets.Resolve(TextDirection.Ltr));
        }

        return insets;
    }

    /// <summary>Widgets of type <typeparamref name="T"/> inside each tile's label, in item order.</summary>
    private static List<T> InLabels<T>(MountedScope scope) where T : class
    {
        var found = new List<T>();
        foreach (Element label in FindElements(scope.Root, element => element.Widget is BottomNavigationTileLabel))
        {
            found.AddRange(FindWidgets<T>(label));
        }

        return found;
    }

    private static int CountHorizontalFlexes(RenderObject? root)
    {
        if (root is null)
        {
            return 0;
        }

        int count = root is RenderFlex { Direction: Axis.Horizontal } ? 1 : 0;
        root.VisitChildren(child => count += CountHorizontalFlexes(child));
        return count;
    }

    private static List<T> FindWidgets<T>(Element root) where T : class
    {
        var found = new List<T>();
        void Walk(Element element)
        {
            if (element.Widget is T match)
            {
                found.Add(match);
            }

            element.VisitChildren(Walk);
        }

        Walk(root);
        return found;
    }

    private static List<Element> FindElements(Element root, Func<Element, bool> predicate)
    {
        var found = new List<Element>();
        void Walk(Element element)
        {
            if (predicate(element))
            {
                found.Add(element);
            }

            element.VisitChildren(Walk);
        }

        Walk(root);
        return found;
    }

    private static Element? FindElement(Element root, Func<Element, bool> predicate)
    {
        Element? found = null;
        void Walk(Element element)
        {
            if (found is not null)
            {
                return;
            }

            if (predicate(element))
            {
                found = element;
                return;
            }

            element.VisitChildren(Walk);
        }

        Walk(root);
        return found;
    }

    private static Element? ParentOf(Element root, Element target)
    {
        Element? parent = null;
        void Walk(Element element)
        {
            if (parent is not null)
            {
                return;
            }

            element.VisitChildren(child =>
            {
                if (parent is not null)
                {
                    return;
                }

                if (ReferenceEquals(child, target))
                {
                    parent = element;
                    return;
                }

                Walk(child);
            });
        }

        Walk(root);
        return parent;
    }

    private sealed class MountedScope : IDisposable
    {
        internal MountedScope(TestRootElement root)
        {
            Root = root;
        }

        internal TestRootElement Root { get; }

        internal RenderObject RenderRoot => RequireRenderObject<RenderObject>(Root.ChildElement);

        public void Dispose() => Root.Unmount();
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsAssignableFrom<T>(element.RenderObject);
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderParagraph paragraph && paragraph.PlainText == text)
        {
            return paragraph;
        }

        RenderParagraph? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindParagraphByText(child, text);
        });

        return result;
    }

    private static RenderExclusiveMouseRegion? FindTooltipHoverPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderExclusiveMouseRegion listener)
        {
            return listener;
        }

        RenderExclusiveMouseRegion? result = null;
        root.VisitChildren(child =>
        {
            if (result is null)
            {
                result = FindTooltipHoverPointerListener(child);
            }
        });
        return result;
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

    private static List<double> GetBottomNavigationTileWidths(RenderObject? root, int childCount)
    {
        var row = FindBottomNavigationRow(root, childCount)
                  ?? throw new InvalidOperationException("BottomNavigationBar row not found in render tree.");
        var widths = new List<double>(childCount);
        for (RenderBox? child = row.FirstChild; child != null; child = row.ChildAfter(child))
        {
            widths.Add(child.Size.Width);
        }

        return widths;
    }

    private static RenderFlex? FindBottomNavigationRow(RenderObject? root, int childCount)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderFlex flex
            && flex.Direction == Axis.Horizontal
            && flex.ChildCount == childCount)
        {
            return flex;
        }

        RenderFlex? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindBottomNavigationRow(child, childCount);
        });

        return result;
    }

    private static Widget WrapWithThemeAndMediaQuery(
        ThemeData theme,
        Widget child,
        MaterialLocalizations? localizations = null,
        Size? size = null,
        Thickness viewPadding = default,
        Thickness viewInsets = default)
    {
        Widget themedContent = new Theme(data: theme, child: child);
        if (localizations is not null)
        {
            themedContent = new MaterialLocalizationsScope(localizations: localizations, child: themedContent);
        }

        return new MediaQuery(
            data: new MediaQueryData(
                Size: size ?? new Size(390, 844),
                ViewPadding: viewPadding,
                ViewInsets: viewInsets),
            child: themedContent);
    }

    private sealed class TestMaterialLocalizations : DefaultMaterialLocalizations
    {
        public override string TabLabel(int tabIndex, int tabCount)
        {
            return $"Section {tabIndex} / {tabCount}";
        }
    }

    private sealed class CaptureIconThemeWidget : StatelessWidget
    {
        private readonly Action<IconThemeData> _capture;

        public CaptureIconThemeWidget(Action<IconThemeData> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(IconTheme.Of(context));
            return new SizedBox();
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

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
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
            if (_child != null)
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

            _rootElement = new HarnessRootElement(
                RenderView,
                new Directionality(TextDirection.Ltr, child: rootWidget));
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

        public void UpdateRootWidget(Widget rootWidget)
        {
            _rootElement.Update(new Directionality(TextDirection.Ltr, child: rootWidget));
            _owner.FlushBuild();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
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

            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild(force: true);
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
                if (_child != null)
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
                if (_child != null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }

    /// <summary>
    /// Whether one of the node's merged label parts is <paramref name="part"/>. A merged node joins
    /// the labels it absorbed with a newline, exactly like Flutter's <c>_concatAttributedString</c>.
    /// </summary>
    private static bool HasLabelPart(SemanticsNode node, string part) =>
        node.Label?.Split('\n').Contains(part) == true;
}

