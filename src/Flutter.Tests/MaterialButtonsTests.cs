using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

public sealed class MaterialButtonsTests
{
    [Fact]
    public void ThemeData_Light_UsesFlutterMaterial3ColorDefaults()
    {
        var theme = ThemeData.Light;

        Assert.Equal(Color.Parse("#FFFEF7FF"), theme.ScaffoldBackgroundColor);
        Assert.Equal(Color.Parse("#FFFEF7FF"), theme.CanvasColor);
        Assert.Equal(Color.Parse("#FF6750A4"), theme.PrimaryColor);
        Assert.Equal(Color.Parse("#FF1D1B20"), theme.OnSurfaceColor);
        Assert.Equal(Color.Parse("#FF4A4458"), theme.OnSecondaryContainerColor);
        Assert.Equal(Colors.Black, theme.ShadowColor);
    }

    [Fact]
    public void ThemeData_Light_DefaultMaterialTapTargetSize_IsPadded()
    {
        Assert.Equal(MaterialTapTargetSize.Padded, ThemeData.Light.MaterialTapTargetSize);
    }

    [Fact]
    public void TextButton_UsesThemePrimaryColorAsDefaultForeground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Tap me"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_DefaultIconTheme_UsesForegroundAndIconSizeDefaults()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed
        };
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.OrangeRed, capturedTheme!.Color);
        Assert.Equal(18, capturedTheme.Size);
    }

    [Fact]
    public void TextButton_StyleFrom_IconColorAndSizeOverrideDefaults()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        iconColor: Colors.Gold,
                        iconSize: 26),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Gold, capturedTheme!.Color);
        Assert.Equal(26, capturedTheme.Size);
    }

    [Fact]
    public void TextButton_StyleFrom_DisabledIconColorOverridesForegroundFallback()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: null,
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        iconColor: Colors.Gold,
                        disabledIconColor: Colors.Gray),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Gray, capturedTheme!.Color);
        Assert.Equal(18, capturedTheme.Size);
    }

    [Fact]
    public void TextButton_DefaultMinSize_UsesMaterialBaseline64x40()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Min size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void TextButton_TapTargetPadding_RedirectsHitTestInPaddedAreaToChildCenter()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 120,
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Tap target")))));

        harness.Pump(new Size(220, 120));

        var renderRoot = harness.RenderView;
        var probePosition = new Point(60, 1);
        var hitResult = new BoxHitTestResult();
        Assert.True(renderRoot.HitTest(hitResult, probePosition));

        var interactiveListener = FindInteractivePointerListener(harness.RenderView.Child);
        Assert.NotNull(interactiveListener);

        BoxHitTestEntry? interactiveEntry = null;
        foreach (var entry in hitResult.Path)
        {
            if (entry is BoxHitTestEntry boxEntry && ReferenceEquals(boxEntry.Target, interactiveListener))
            {
                interactiveEntry = boxEntry;
                break;
            }
        }

        Assert.NotNull(interactiveEntry);
        Assert.True(interactiveEntry!.LocalPosition.Y > 10);

        var missResult = new BoxHitTestResult();
        Assert.False(renderRoot.HitTest(missResult, new Point(60, 90)));
    }

    [Fact]
    public void TextButton_ThemeMaterialTapTargetSizeShrinkWrap_DoesNotExpandTapTarget()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap },
                child: new SizedBox(
                    width: 120,
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Tap target")))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void TextButton_StyleFromTapTargetSize_OverridesThemeTapTargetSize()
    {
        using (var paddedHarness = new WidgetRenderHarness(
                   new Theme(
                       data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.Padded },
                       child: new SizedBox(
                           width: 120,
                           child: new TextButton(
                               onPressed: () => { },
                               child: new Text("Tap target"))))))
        {
            paddedHarness.Pump(new Size(220, 120));
            var paddedHitResult = new BoxHitTestResult();
            Assert.True(paddedHarness.RenderView.HitTest(paddedHitResult, new Point(60, 46)));
        }

        using var overrideHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.Padded },
                child: new SizedBox(
                    width: 120,
                    child: new TextButton(
                        onPressed: () => { },
                        style: TextButton.StyleFrom(tapTargetSize: MaterialTapTargetSize.ShrinkWrap),
                        child: new Text("Tap target")))));

        overrideHarness.Pump(new Size(220, 120));

        var overrideHitResult = new BoxHitTestResult();
        Assert.False(overrideHarness.RenderView.HitTest(overrideHitResult, new Point(60, 46)));
    }

    [Fact]
    public void ElevatedButton_DefaultPadding_UsesHorizontal24AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(24, 0), padding!.Padding);
    }

    [Fact]
    public void OutlinedButton_DefaultPadding_UsesHorizontal24AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(24, 0), padding!.Padding);
    }

    [Fact]
    public void FilledButton_DefaultPadding_UsesHorizontal24AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(24, 0), padding!.Padding);
    }

    [Fact]
    public void TextButton_DefaultTextStyle_UsesLabelLargeTypography()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Typography"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(14, paragraph!.FontSize);
        Assert.Equal(FontWeight.Medium, paragraph.FontWeight);
        Assert.Equal(1.43, paragraph.Height);
        Assert.Equal(0.1, paragraph.LetterSpacing);
    }

    [Fact]
    public void TextButton_TextStyleColor_DoesNotOverrideForegroundColor()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        textStyle: new TextStyle(Color: Colors.Crimson)),
                    child: new Text("Foreground precedence"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_UsesThemeSurfaceContainerAndPrimaryColorsByDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkSlateBlue,
            SurfaceContainerLowColor = Colors.Bisque
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Primary"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(Colors.Bisque, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkSlateBlue, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_DefaultShadow_IsAppliedWhenEnabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Shadow"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        var shadows = decorated!.Decoration.BoxShadows;
        Assert.True(shadows.HasValue);
        Assert.True(shadows.Value.Count > 0);
    }

    [Fact]
    public void ElevatedButton_DisabledState_DoesNotApplyShadow()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: null,
                    child: new Text("Disabled shadow"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.False(decorated!.Decoration.BoxShadows.HasValue);
    }

    [Fact]
    public void OutlinedButton_UsesThemeOutlineColorForBorderByDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OutlineColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Outline"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(new BorderSide(Colors.CadetBlue, 1), decorated!.Decoration.Border);
    }

    [Fact]
    public void OutlinedButton_DefaultForegroundUsesThemePrimaryColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.MediumVioletRed,
            OutlineColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Outline fg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.MediumVioletRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButton_UsesThemePrimaryAndOnPrimaryColorsByDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkSlateBlue,
            OnPrimaryColor = Colors.AliceBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: () => { },
                    child: new Text("Filled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(theme.PrimaryColor, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(theme.OnPrimaryColor, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButtonTonal_UsesThemeSecondaryContainerAndOnSecondaryContainerColorsByDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            SecondaryContainerColor = Colors.Bisque,
            OnSecondaryContainerColor = Colors.DarkSlateBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: FilledButton.Tonal(
                    onPressed: () => { },
                    child: new Text("Filled tonal"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(theme.SecondaryContainerColor, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(theme.OnSecondaryContainerColor, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButton_DisabledStateUsesThemeOnSurfaceTones()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.DarkOliveGreen
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: null,
                    child: new Text("Disabled filled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.OnSurfaceColor, 0.12), decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(ApplyOpacity(theme.OnSurfaceColor, 0.38), Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_ButtonStyleForegroundOverridesDefault()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen)),
                    child: new Text("Styled foreground"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.ForestGreen, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_ButtonStyleAlignmentOverridesDefaultCenter()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        Alignment: Alignment.TopLeft),
                    child: new Text("Aligned"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var align = FindDescendant<RenderAlign>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(align);
        Assert.Equal(Alignment.TopLeft, align!.Alignment);
    }

    [Fact]
    public void TextButton_ThemeStyleAlignmentOverridesDefaultCenter()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    Alignment: Alignment.BottomRight))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Theme aligned"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var align = FindDescendant<RenderAlign>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(align);
        Assert.Equal(Alignment.BottomRight, align!.Alignment);
    }

    [Fact]
    public void TextButton_WidgetStyleAlignmentOverridesThemeStyleAlignment()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    Alignment: Alignment.BottomRight))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(alignment: Alignment.CenterLeft),
                    child: new Text("Widget aligned"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var align = FindDescendant<RenderAlign>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(align);
        Assert.Equal(Alignment.CenterLeft, align!.Alignment);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleMinimumSizeOverridesDefault()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MinimumSize: MaterialStateProperty<Size?>.All(new Size(180, 56))),
                    child: new Text("Styled size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(180, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleMinimumSize_AllowsZero()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, 0))),
                    child: new Text("Zero min size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(0, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(0, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleMinimumSize_Negative_Throws()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MinimumSize: MaterialStateProperty<Size?>.All(new Size(-1, 10))),
                    child: new Text("Invalid min size"))));

        root.Attach(owner);
        Assert.Throws<ArgumentOutOfRangeException>(() => root.Mount(parent: null, newSlot: null));
    }

    [Fact]
    public void TextButton_ButtonStyleMaximumSizeClampsDefaultInfinityMax()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MaximumSize: MaterialStateProperty<Size?>.All(new Size(120, 48))),
                    child: new Text("Max size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(120, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(48, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleFixedSizeSetsTightConstraints_WithinMaximum()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MinimumSize: MaterialStateProperty<Size?>.All(new Size(64, 40)),
                        MaximumSize: MaterialStateProperty<Size?>.All(new Size(120, 48)),
                        FixedSize: MaterialStateProperty<Size?>.All(new Size(200, 80))),
                    child: new Text("Fixed size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(120, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(120, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(48, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(48, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleFixedSizeInfiniteWidth_OnlyTightensFiniteAxis()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        FixedSize: MaterialStateProperty<Size?>.All(new Size(double.PositiveInfinity, 44))),
                    child: new Text("Fixed height only"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.True(double.IsPositiveInfinity(constrainedBox.AdditionalConstraints.MaxWidth));
        Assert.Equal(44, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(44, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleSideOverridesDefault()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Goldenrod, 2))),
                    child: new Text("Styled side"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(new BorderSide(Colors.Goldenrod, 2), decorated!.Decoration.Border);
    }

    [Fact]
    public void TextButton_ThemeStyleForegroundOverridesDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            TextButtonStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Theme fg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_WidgetStyleForegroundOverridesThemeStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen)),
                    child: new Text("Widget fg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.ForestGreen, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_LegacyForeground_OverridesWidgetAndThemeStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    foregroundColor: Colors.OrangeRed,
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen)),
                    child: new Text("Legacy fg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_ThemeStyleBackgroundOverridesDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            SurfaceContainerLowColor = Colors.Bisque,
            ElevatedButtonStyle = new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Theme bg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.MediumPurple, decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_ThemeStyleSideOverridesDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OutlineColor = Colors.CadetBlue,
            OutlinedButtonStyle = new ButtonStyle(
                Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Goldenrod, 3)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Theme side"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(new BorderSide(Colors.Goldenrod, 3), decorated!.Decoration.Border);
    }

    [Fact]
    public void TextButton_ThemeDataButtonTheme_OverridesLegacyThemeStyleProperty()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan)),
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("ThemeData text button theme"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.ForestGreen, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_ThemeDataButtonTheme_OverridesLegacyThemeStyleProperty()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ElevatedButtonStyle = new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple)),
            ElevatedButtonTheme = new ElevatedButtonThemeData(
                style: new ButtonStyle(
                    BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Gold)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("ThemeData elevated theme"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_ThemeDataButtonTheme_OverridesLegacyThemeStyleProperty()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OutlinedButtonStyle = new ButtonStyle(
                Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Goldenrod, 3))),
            OutlinedButtonTheme = new OutlinedButtonThemeData(
                style: new ButtonStyle(
                    Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Crimson, 4))))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("ThemeData outlined theme"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(new BorderSide(Colors.Crimson, 4), decorated!.Decoration.Border);
    }

    [Fact]
    public void FilledButton_ThemeDataButtonTheme_OverridesLegacyThemeStyleProperty()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            FilledButtonStyle = new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple)),
            FilledButtonTheme = new FilledButtonThemeData(
                style: new ButtonStyle(
                    BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Gold)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: () => { },
                    child: new Text("ThemeData filled theme"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_LocalThemeStyleForegroundOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            TextButtonStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButtonTheme(
                    data: new TextButtonThemeData(
                        style: new ButtonStyle(
                            ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen))),
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Local theme fg")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.ForestGreen, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_WidgetStyleForegroundOverridesLocalThemeStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButtonTheme(
                    data: new TextButtonThemeData(
                        style: new ButtonStyle(
                            ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen))),
                    child: new TextButton(
                        onPressed: () => { },
                        style: new ButtonStyle(
                            ForegroundColor: MaterialStateProperty<Color?>.All(Colors.OrangeRed)),
                        child: new Text("Widget over local")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_LocalThemeNullStyle_DoesNotFallbackToThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            TextButtonStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButtonTheme(
                    data: new TextButtonThemeData(),
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Local clears theme")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_LocalThemeStyleBackgroundOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            SurfaceContainerLowColor = Colors.Bisque,
            ElevatedButtonStyle = new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButtonTheme(
                    data: new ElevatedButtonThemeData(
                        style: new ButtonStyle(
                            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Gold))),
                    child: new ElevatedButton(
                        onPressed: () => { },
                        child: new Text("Local elevated bg")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_LocalThemeStyleSideOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OutlineColor = Colors.CadetBlue,
            OutlinedButtonStyle = new ButtonStyle(
                Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Goldenrod, 3)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButtonTheme(
                    data: new OutlinedButtonThemeData(
                        style: new ButtonStyle(
                            Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Crimson, 4)))),
                    child: new OutlinedButton(
                        onPressed: () => { },
                        child: new Text("Local outlined side")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(new BorderSide(Colors.Crimson, 4), decorated!.Decoration.Border);
    }

    [Fact]
    public void FilledButton_LocalThemeStyleBackgroundOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            FilledButtonStyle = new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButtonTheme(
                    data: new FilledButtonThemeData(
                        style: new ButtonStyle(
                            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Gold))),
                    child: new FilledButton(
                        onPressed: () => { },
                        child: new Text("Local filled bg")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void ButtonStyle_Merge_FillsNullFields_FromArgument_WithoutOverridingExisting()
    {
        var owner = new BuildOwner();
        var mergedStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.Crimson))
            .Merge(new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkGreen),
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.LightGoldenrodYellow)));

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: mergedStyle,
                    child: new Text("Merge semantics"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Crimson, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
        Assert.NotNull(decorated);
        Assert.Equal(Colors.LightGoldenrodYellow, decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_StyleFrom_AppliesForegroundAndTextStyle()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        textStyle: new TextStyle(
                            FontSize: 18,
                            FontWeight: FontWeight.Bold)),
                    child: new Text("StyleFrom"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
        Assert.Equal(18, paragraph.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
    }

    [Fact]
    public void TextButton_WidgetTextStyleStateResolver_NullDisabled_FallsBackToThemeTextStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    TextStyle: MaterialStateProperty<TextStyle?>.All(
                        new TextStyle(FontWeight: FontWeight.Bold))))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: null,
                    style: new ButtonStyle(
                        TextStyle: MaterialStateProperty<TextStyle?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? null
                                : new TextStyle(FontSize: 18))),
                    child: new Text("Disabled text-style fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(14, paragraph!.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
    }

    [Fact]
    public void TextButton_WidgetTextStyleStateResolver_Enabled_OverridesThemeTextStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    TextStyle: MaterialStateProperty<TextStyle?>.All(
                        new TextStyle(FontWeight: FontWeight.Bold))))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        TextStyle: MaterialStateProperty<TextStyle?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? null
                                : new TextStyle(FontSize: 18))),
                    child: new Text("Enabled text-style"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(18, paragraph!.FontSize);
        Assert.Equal(FontWeight.Medium, paragraph.FontWeight);
    }

    [Fact]
    public void TextButton_StyleFrom_ForegroundColor_DerivesOverlayAndSplash()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed
        };

        var styleColor = Colors.DarkCyan;
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(foregroundColor: styleColor),
                    child: new Text("StyleFrom states"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 21,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(ApplyOpacity(styleColor, 0.08), hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 21,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 8)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), pressedDecorated!.Decoration.Color);

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), splash!.SplashColor);
    }

    [Fact]
    public void TextButton_StyleFrom_TransparentOverlay_DisablesVisualHighlights()
    {
        var owner = new BuildOwner();
        var style = TextButton.StyleFrom(overlayColor: Colors.Transparent);
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: style,
                    child: new Text("Transparent overlay"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.Decoration.Color);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 22,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(12, 9)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(Colors.Transparent, hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 22,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 9)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(Colors.Transparent, pressedDecorated!.Decoration.Color);

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Null(splash!.SplashColor);
    }

    [Fact]
    public void TextButton_StyleFrom_OverlayColor_UsesStateOpacitiesAndSplashFallback()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.DarkMagenta;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    PrimaryColor = Colors.CadetBlue
                },
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(overlayColor: overlayColor),
                    child: new Text("Overlay styleFrom"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.Decoration.Color);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 24,
                kind: PointerDeviceKind.Mouse,
                position: new Point(11, 9),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(11, 9)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(ApplyOpacity(overlayColor, 0.08), hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 24,
                kind: PointerDeviceKind.Mouse,
                position: new Point(11, 9),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(11, 9)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(ApplyOpacity(overlayColor, 0.10), pressedDecorated!.Decoration.Color);

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Equal(ApplyOpacity(overlayColor, 0.10), splash!.SplashColor);
    }

    [Fact]
    public void TextButton_ButtonStyleOverlayAll_DoesNotTintAtRest_ButAppliesOnHover()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.HotPink;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.All(overlayColor)),
                    child: new Text("Overlay all"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.Decoration.Color);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 25,
                kind: PointerDeviceKind.Mouse,
                position: new Point(11, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(11, 10)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(overlayColor, hoveredDecorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_SplashColor_RemainsActivationTint_AfterPointerUp()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.DarkOrange;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(overlayColor: overlayColor),
                    child: new Text("Stable splash tint"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 26,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(12, 10)));

        owner.FlushBuild();

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 26,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 10)));

        owner.FlushBuild();

        var pressedSplash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedSplash);
        Assert.Equal(ApplyOpacity(overlayColor, 0.10), pressedSplash!.SplashColor);

        interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerUpEvent(
                pointer: 26,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 10)));

        owner.FlushBuild();

        var releasedSplash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(releasedSplash);
        Assert.Equal(ApplyOpacity(overlayColor, 0.10), releasedSplash!.SplashColor);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_UsesDisabledColorOverrides()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: null,
                    style: ElevatedButton.StyleFrom(
                        foregroundColor: Colors.White,
                        backgroundColor: Colors.SeaGreen,
                        disabledForegroundColor: Colors.SlateGray,
                        disabledBackgroundColor: Colors.SaddleBrown),
                    child: new Text("Disabled styleFrom"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);
        Assert.NotNull(decorated);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.SaddleBrown, decorated!.Decoration.Color);
        Assert.Equal(Colors.SlateGray, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_StyleFrom_ForegroundOnly_DisabledFallsBackToThemeDisabledForeground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.MidnightBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: null,
                    style: TextButton.StyleFrom(foregroundColor: Colors.LimeGreen),
                    child: new Text("Disabled foreground fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(ApplyOpacity(theme.OnSurfaceColor, 0.38), Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_BackgroundOnly_DisabledFallsBackToThemeDisabledBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.DarkSlateGray
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: null,
                    style: ElevatedButton.StyleFrom(backgroundColor: Colors.SeaGreen),
                    child: new Text("Disabled background fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.OnSurfaceColor, 0.12), decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_StyleFrom_DisabledForegroundOnly_PreservesEnabledThemeForeground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(disabledForegroundColor: Colors.DimGray),
                    child: new Text("Enabled foreground"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(theme.PrimaryColor, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_DisabledBackgroundOnly_PreservesEnabledThemeBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            SurfaceContainerLowColor = Colors.BurlyWood
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: ElevatedButton.StyleFrom(disabledBackgroundColor: Colors.SaddleBrown),
                    child: new Text("Enabled background"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(theme.SurfaceContainerLowColor, decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.MediumPurple
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? Colors.Gray : null)),
                    child: new Text("Resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(theme.PrimaryColor, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.MediumPurple
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? Colors.Gray : null)),
                    child: new Text("Elevated fg fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(theme.PrimaryColor, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.MediumPurple
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? Colors.Gray : null)),
                    child: new Text("Outlined fg fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(theme.PrimaryColor, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleBackgroundResolverNullForDisabled_FallsBackToDefaultDisabledBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.DarkSlateGray
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: null,
                    style: new ButtonStyle(
                        BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? null : Colors.SeaGreen)),
                    child: new Text("Background resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.OnSurfaceColor, 0.12), decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleSideResolverNullForEnabled_FallsBackToDefaultEnabledSide()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OutlineColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? new BorderSide(Colors.DarkGray, 3)
                                : null)),
                    child: new Text("Side resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(new BorderSide(theme.OutlineColor, 1), decorated!.Decoration.Border);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleSideResolverNullForDisabled_FallsBackToDefaultDisabledSide()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.DarkOliveGreen
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: null,
                    style: new ButtonStyle(
                        Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? null
                                : new BorderSide(Colors.Goldenrod, 2))),
                    child: new Text("Disabled side fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(new BorderSide(ApplyOpacity(theme.OnSurfaceColor, 0.12), 1), decorated!.Decoration.Border);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed
        };

        var pressedOverlay = Colors.YellowGreen;
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                    child: new Text("Elevated overlay resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 44,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        var expectedHoverOverlay = ApplyOpacity(theme.PrimaryColor, 0.08);
        Assert.Equal(BlendColorOverlay(theme.SurfaceContainerLowColor, expectedHoverOverlay), hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 44,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 8)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(BlendColorOverlay(theme.SurfaceContainerLowColor, pressedOverlay), pressedDecorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed
        };

        var pressedOverlay = Colors.YellowGreen;
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                    child: new Text("Outlined overlay resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 45,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 45,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 8)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(pressedOverlay, pressedDecorated!.Decoration.Color);
    }

    [Fact]
    public void ElevatedButton_ThemeStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            SurfaceContainerLowColor = Colors.Bisque,
            ElevatedButtonStyle = new ButtonStyle(
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Pressed) ? Colors.YellowGreen : null))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Theme overlay fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 46,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        var expectedHoverOverlay = ApplyOpacity(theme.PrimaryColor, 0.08);
        Assert.Equal(BlendColorOverlay(theme.SurfaceContainerLowColor, expectedHoverOverlay), hoveredDecorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed
        };

        var pressedOverlay = Colors.YellowGreen;
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                    child: new Text("Overlay resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 33,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 33,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 8)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(pressedOverlay, pressedDecorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.Teal;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.All(overlayColor),
                        SplashColor: null),
                    child: new Text("Overlay splash fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 34,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 10)));

        owner.FlushBuild();

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Equal(overlayColor, splash!.SplashColor);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.Orange;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Pressed) ? overlayColor : null),
                        SplashColor: null),
                    child: new Text("Elevated overlay splash"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 35,
                kind: PointerDeviceKind.Mouse,
                position: new Point(13, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(13, 10)));

        owner.FlushBuild();

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Equal(overlayColor, splash!.SplashColor);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.CadetBlue;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Pressed) ? overlayColor : null),
                        SplashColor: null),
                    child: new Text("Outlined overlay splash"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 36,
                kind: PointerDeviceKind.Mouse,
                position: new Point(14, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(14, 10)));

        owner.FlushBuild();

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Equal(overlayColor, splash!.SplashColor);
    }

    [Fact]
    public void TextButton_LegacyForeground_OverridesStyleFromForeground()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    foregroundColor: Colors.OrangeRed,
                    style: TextButton.StyleFrom(foregroundColor: Colors.RoyalBlue),
                    child: new Text("Legacy wins"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_DisabledStateUsesThemeOnSurfaceTones()
    {
        var owner = new BuildOwner();
        var background = Colors.DarkGreen;
        var foreground = Colors.White;
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.Black
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: null,
                    backgroundColor: background,
                    foregroundColor: foreground,
                    child: new Text("Disabled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.OnSurfaceColor, 0.12), decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(ApplyOpacity(theme.OnSurfaceColor, 0.38), Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_PointerPressedStateDarkensBackgroundUntilPointerUp()
    {
        var owner = new BuildOwner();
        var background = Colors.SteelBlue;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    backgroundColor: background,
                    child: new Text("Press"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var pointerListener = FindInteractivePointerListener(renderRoot);
        Assert.NotNull(pointerListener);
        pointerListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 1,
                kind: PointerDeviceKind.Mouse,
                position: new Point(8, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(pointerListener, new Point(8, 8)));

        owner.FlushBuild();

        var pressedRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var pressedDecoration = FindDescendant<RenderDecoratedBox>(pressedRoot);
        Assert.NotNull(pressedDecoration);
        Assert.NotEqual(background, pressedDecoration!.Decoration.Color);

        pointerListener = FindInteractivePointerListener(pressedRoot);
        Assert.NotNull(pointerListener);
        pointerListener!.HandleEvent(
            new PointerUpEvent(
                pointer: 1,
                kind: PointerDeviceKind.Mouse,
                position: new Point(8, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(pointerListener, new Point(8, 8)));

        owner.FlushBuild();

        var releasedDecoration = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(releasedDecoration);
        Assert.Equal(background, releasedDecoration!.Decoration.Color);
    }

    [Fact]
    public void TextButton_HoverStateAppliesOverlayUntilExit()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.IndianRed
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Hover"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.Decoration.Color);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 1,
                kind: PointerDeviceKind.Mouse,
                position: new Point(8, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(8, 8)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), hoveredDecorated!.Decoration.Color);

        hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerExitEvent(
                pointer: 1,
                kind: PointerDeviceKind.Mouse,
                position: new Point(120, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(120, 8)));

        owner.FlushBuild();

        var exitedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(exitedDecorated);
        Assert.Null(exitedDecorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_PressedOverlayTakesPriorityOverHoverOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.CornflowerBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Priority"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 11,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 10)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 11,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 10)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.10), pressedDecorated!.Decoration.Color);

        interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerUpEvent(
                pointer: 11,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 10)));

        owner.FlushBuild();

        var releasedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(releasedDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), releasedDecorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_HoverOverlayTakesPriorityOverFocusOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.MediumSeaGreen
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Focus hover"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 19,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(12, 9)));

        owner.FlushBuild();

        var focusedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.10), focusedDecorated!.Decoration.Color);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 19,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(12, 9)));

        owner.FlushBuild();

        var focusedHoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedHoveredDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), focusedHoveredDecorated!.Decoration.Color);

        hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerExitEvent(
                pointer: 19,
                kind: PointerDeviceKind.Mouse,
                position: new Point(120, 9),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(120, 9)));

        owner.FlushBuild();

        var focusOnlyDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusOnlyDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.10), focusOnlyDecorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_KeyboardActivation_UsesPressedOverlay_AndInvokesOnPressedOnKeyDownOnly()
    {
        var owner = new BuildOwner();
        var focusedOverlay = Colors.SeaGreen;
        var pressedOverlay = Colors.OrangeRed;
        var pressedCount = 0;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => pressedCount += 1,
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        {
                            if (states.HasFlag(MaterialState.Pressed))
                            {
                                return pressedOverlay;
                            }

                            if (states.HasFlag(MaterialState.Focused))
                            {
                                return focusedOverlay;
                            }

                            return null;
                        })),
                    child: new Text("Keyboard pressed overlay"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 41,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(10, 8)));

        owner.FlushBuild();

        var focusedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(focusedOverlay, focusedDecorated!.Decoration.Color);

        var handledDown = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
        Assert.True(handledDown);
        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(pressedOverlay, pressedDecorated!.Decoration.Color);
        Assert.Equal(1, pressedCount);

        var handledUp = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: false));
        Assert.True(handledUp);
        owner.FlushBuild();
        Assert.Equal(1, pressedCount);
    }

    [Fact]
    public void TextButton_KeyboardActivation_NumPadEnter_InvokesOnPressed()
    {
        var owner = new BuildOwner();
        var pressedCount = 0;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => pressedCount += 1,
                    child: new Text("NumPad Enter"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 42,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(10, 8)));

        owner.FlushBuild();

        var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "NumPadEnter", isDown: true));
        Assert.True(handled);
        owner.FlushBuild();

        Assert.Equal(1, pressedCount);
    }

    [Fact]
    public void TextButton_KeyboardActivation_IgnoresModifiedSpaceChord()
    {
        var owner = new BuildOwner();
        var focusedOverlay = Colors.SeaGreen;
        var pressedOverlay = Colors.OrangeRed;
        var pressedCount = 0;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => pressedCount += 1,
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        {
                            if (states.HasFlag(MaterialState.Pressed))
                            {
                                return pressedOverlay;
                            }

                            if (states.HasFlag(MaterialState.Focused))
                            {
                                return focusedOverlay;
                            }

                            return null;
                        })),
                    child: new Text("Ctrl+Space ignored"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 43,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(10, 8)));

        owner.FlushBuild();

        var focusedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(focusedOverlay, focusedDecorated!.Decoration.Color);

        var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(
            key: "Space",
            isDown: true,
            isControlPressed: true));
        Assert.False(handled);
        owner.FlushBuild();

        var stillFocusedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(stillFocusedDecorated);
        Assert.Equal(focusedOverlay, stillFocusedDecorated!.Decoration.Color);
        Assert.NotEqual(pressedOverlay, stillFocusedDecorated.Decoration.Color);
        Assert.Equal(0, pressedCount);
    }

    [Fact]
    public void TextButton_PressedOverlayTakesPriorityOverFocusOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DeepPink
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Focus pressed"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 37,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(12, 9)));

        owner.FlushBuild();

        var focusedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.10), focusedDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 37,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 9)));

        owner.FlushBuild();

        var focusedPressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedPressedDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.10), focusedPressedDecorated!.Decoration.Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_OverlayColor_UsesHoverOpacityAndPressedPriority()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.DarkOliveGreen;
        var theme = ThemeData.Light;
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: ElevatedButton.StyleFrom(overlayColor: overlayColor),
                    child: new Text("Elevated overlay"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 31,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(12, 10)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        var expectedHoverTint = ApplyOpacity(overlayColor, 0.08);
        Assert.Equal(BlendColorOverlay(theme.SurfaceContainerLowColor, expectedHoverTint), hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 31,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 10)));

        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        var expectedPressedTint = ApplyOpacity(overlayColor, 0.10);
        Assert.Equal(BlendColorOverlay(theme.SurfaceContainerLowColor, expectedPressedTint), pressedDecorated!.Decoration.Color);

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Equal(expectedPressedTint, splash!.SplashColor);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_TransparentOverlay_HasNoIdleTint_AndNoSplash()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: OutlinedButton.StyleFrom(overlayColor: Colors.Transparent),
                    child: new Text("Outlined transparent"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.Decoration.Color);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 32,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 9),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 9)));

        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(Colors.Transparent, hoveredDecorated!.Decoration.Color);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 32,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 9),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 9)));

        owner.FlushBuild();

        var splash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(splash);
        Assert.Null(splash!.SplashColor);
    }

    [Fact]
    public void TextButton_PointerDownStartsInkSplashRender()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.Teal
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Splash"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialSplash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialSplash);
        Assert.Null(initialSplash!.SplashColor);
        Assert.Equal(0, initialSplash.SplashProgress);

        var pointerListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pointerListener);
        pointerListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 13,
                kind: PointerDeviceKind.Mouse,
                position: new Point(16, 12),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(pointerListener, new Point(16, 12)));

        owner.FlushBuild();

        var activeSplash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(activeSplash);
        Assert.NotNull(activeSplash!.SplashColor);
        Assert.Equal(new Point(16, 12), activeSplash.SplashOrigin);
    }

    [Fact]
    public void TextButton_UsesRoundedClipForInkSplash()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Rounded splash"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var clip = FindDescendant<RenderClipRRect>(renderRoot);
        var splash = FindDescendant<RenderInkSplash>(renderRoot);

        Assert.NotNull(clip);
        Assert.Equal(BorderRadius.Circular(20), clip!.BorderRadius);
        Assert.NotNull(splash);
        Assert.False(splash!.ClipToBounds);
    }

    [Fact]
    public void TextButton_PointerClick_DoesNotKeepFocusOverlayAfterPointerUp()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.Coral
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Pointer focus"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var interactiveListener = FindInteractivePointerListener(renderRoot);
        var focusListener = FindFocusPointerListener(renderRoot);
        Assert.NotNull(interactiveListener);
        Assert.NotNull(focusListener);

        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 17,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 10)));

        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 17,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(12, 10)));

        owner.FlushBuild();

        interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerUpEvent(
                pointer: 17,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(12, 10)));

        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Null(decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_ExternalFocusNode_RequestFocus_AppliesFocusedOverlay()
    {
        FocusManager.Instance.ResetForTests();

        var owner = new BuildOwner();
        var focusNode = new FocusNode();
        var focusedOverlay = Colors.OrangeRed;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Focused) ? focusedOverlay : null)),
                    child: new Text("External focus node"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.Decoration.Color);

        Assert.True(focusNode.RequestFocus());
        owner.FlushBuild();

        var focusedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(focusedOverlay, focusedDecorated!.Decoration.Color);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void TextButton_Autofocus_RequestsProvidedFocusNodeOnMount()
    {
        FocusManager.Instance.ResetForTests();

        var owner = new BuildOwner();
        var focusNode = new FocusNode();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    autofocus: true,
                    child: new Text("Autofocus mount"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(focusNode.HasFocus);
        Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void TextButton_Autofocus_RequestIsAppliedWhenToggledFromFalseToTrue()
    {
        FocusManager.Instance.ResetForTests();

        var owner = new BuildOwner();
        var focusNode = new FocusNode();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    autofocus: false,
                    child: new Text("Autofocus update"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.False(focusNode.HasFocus);

        root.Update(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    autofocus: true,
                    child: new Text("Autofocus update"))));
        owner.FlushBuild();

        Assert.True(focusNode.HasFocus);
        Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void TextButton_TightWidth_ExpandsInkSplashToFullButtonBounds()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 240,
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Wide button")))));

        harness.Pump(new Size(300, 120));

        var renderRoot = harness.RenderView.Child;
        var splash = FindDescendant<RenderInkSplash>(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(splash);
        Assert.NotNull(decorated);
        Assert.Equal(240, decorated!.Size.Width, 3);
        Assert.Equal(240, splash!.Size.Width, 3);
        Assert.Equal(decorated.Size.Height, splash.Size.Height, 3);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsAssignableFrom<T>(element.RenderObject);
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindDescendant<T>(child);
        });

        return result;
    }

    private static RenderPointerListener? FindInteractivePointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerDown != null
            && listener.OnPointerUp != null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindInteractivePointerListener(child);
        });

        return result;
    }

    private static RenderPointerListener? FindHoverPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerEnter != null
            && listener.OnPointerExit != null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindHoverPointerListener(child);
        });

        return result;
    }

    private static RenderPointerListener? FindFocusPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerDown != null
            && listener.OnPointerUp == null
            && listener.OnPointerCancel == null
            && listener.OnPointerEnter == null
            && listener.OnPointerExit == null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindFocusPointerListener(child);
        });

        return result;
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
            return new SizedBox(width: 8, height: 8);
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

            internal override void Unmount()
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

    private static Color ApplyOpacity(Color color, double opacity)
    {
        var alpha = (byte)Math.Clamp((int)(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Color BlendColorOverlay(Color baseColor, Color overlayColor)
    {
        static byte Blend(byte from, byte to, double t)
        {
            return (byte)Math.Clamp((int)(from + ((to - from) * t)), 0, 255);
        }

        var clampedOpacity = Math.Clamp(overlayColor.A / 255.0, 0, 1);
        return Color.FromArgb(
            baseColor.A,
            Blend(baseColor.R, overlayColor.R, clampedOpacity),
            Blend(baseColor.G, overlayColor.G, clampedOpacity),
            Blend(baseColor.B, overlayColor.B, clampedOpacity));
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
            if (_child != null)
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
}
