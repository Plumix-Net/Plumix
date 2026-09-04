using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSwitchTests
{    // ---- Defaults: Material 3 ----

    [Fact]
    public void Switch_DefaultM3_Selected_UsesPrimaryTrackAndOnPrimaryThumb()
    {
        ThemeData baseTheme = ThemeData.Light;
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with
            {
                Primary = Colors.Coral,
                OnPrimary = Colors.WhiteSmoke
            }
        };

        SwitchPainter painter = MountAndFindPainter(theme, new Switch(true, _ => { }));

        Assert.Equal(Colors.Coral, painter.ActiveTrackColor);
        Assert.Equal(Colors.WhiteSmoke, painter.ActiveColor);
        Assert.Equal(Plumix.Material.Colors.Transparent, painter.ActiveTrackOutlineColor);
    }

    [Fact]
    public void Switch_DefaultM3_Unselected_UsesSurfaceContainerHighestTrackAndOutlineThumb()
    {
        ThemeData baseTheme = ThemeData.Light;
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with
            {
                SurfaceContainerHighest = Colors.PowderBlue,
                Outline = Colors.CadetBlue
            }
        };

        SwitchPainter painter = MountAndFindPainter(theme, new Switch(false, _ => { }));

        Assert.Equal(Colors.PowderBlue, painter.InactiveTrackColor);
        Assert.Equal(Colors.CadetBlue, painter.InactiveTrackOutlineColor);
        Assert.Equal(2.0, painter.InactiveTrackOutlineWidth);
        Assert.Equal(Colors.CadetBlue, painter.InactiveColor);
    }

    [Fact]
    public void Switch_DefaultM3_DisabledSelected_UsesOnSurfaceOpacityTrackAndSurfaceThumb()
    {
        ThemeData baseTheme = ThemeData.Light;
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with { OnSurface = Colors.Brown }
        };

        SwitchPainter painter = MountAndFindPainter(theme, new Switch(true, onChanged: null));

        Assert.Equal(ApplyOpacity(Colors.Brown, 0.12), painter.ActiveTrackColor);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.Surface, 1.0), painter.ActiveColor);
    }

    [Fact]
    public void Switch_DefaultM3_DisabledInactive_AlphaBlendsThumbAgainstSurface()
    {
        ThemeData baseTheme = ThemeData.Light;
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with
            {
                OnSurface = Colors.Brown,
                SurfaceContainerHighest = Colors.PowderBlue,
                Surface = Colors.Ivory
            }
        };

        SwitchPainter painter = MountAndFindPainter(theme, new Switch(false, onChanged: null));

        Assert.Equal(ApplyOpacity(Colors.PowderBlue, 0.12), painter.InactiveTrackColor);
        Assert.Equal(ApplyOpacity(Colors.Brown, 0.12), painter.InactiveTrackOutlineColor);
        Assert.Equal(ApplyOpacity(Colors.Brown, 0.38), painter.InactiveColor);
        Assert.Equal(Colors.Ivory, painter.SurfaceColor);
    }

    // ---- Defaults: Material 2 ----

    [Fact]
    public void Switch_DefaultM2_UsesSecondaryAndLegacyGreyPalette()
    {
        var baseTheme = new ThemeData(useMaterial3: false);
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with { Secondary = Colors.DarkOrange }
        };

        SwitchPainter painter = MountAndFindPainter(theme, new Switch(true, _ => { }));

        Assert.Equal(Colors.DarkOrange, painter.ActiveColor);
        Assert.Equal(
            Color.FromArgb(0x80, Colors.DarkOrange.R, Colors.DarkOrange.G, Colors.DarkOrange.B),
            painter.ActiveTrackColor);
        Assert.Equal(Plumix.Material.Colors.Grey.Shade50, painter.InactiveColor);
        Assert.Equal(Color.FromArgb(0x52, 0x00, 0x00, 0x00), painter.InactiveTrackColor);
        Assert.Equal(Plumix.Material.Colors.Transparent, painter.InactiveTrackOutlineColor);
        Assert.Null(painter.InactiveTrackOutlineWidth);
        Assert.Equal(10.0, painter.ActiveThumbRadius);
        Assert.Equal(10.0, painter.InactiveThumbRadius);
        Assert.Equal(33.0, painter.TrackWidth);
        Assert.Equal(14.0, painter.TrackHeight);
    }

    [Fact]
    public void Switch_DefaultM2_Disabled_UsesBlack12TrackAndGrey400Thumb()
    {
        var theme = new ThemeData(useMaterial3: false);

        SwitchPainter selected = MountAndFindPainter(theme, new Switch(true, onChanged: null));
        SwitchPainter unselected = MountAndFindPainter(theme, new Switch(false, onChanged: null));

        Assert.Equal(Color.FromArgb(0x1F, 0x00, 0x00, 0x00), selected.ActiveTrackColor);
        Assert.Equal(Plumix.Material.Colors.Grey.Shade400, selected.ActiveColor);
        Assert.Equal(Color.FromArgb(0x1F, 0x00, 0x00, 0x00), unselected.InactiveTrackColor);
        Assert.Equal(Plumix.Material.Colors.Grey.Shade400, unselected.InactiveColor);
    }

    // ---- Geometry ----

    [Fact]
    public void Switch_Size_TracksTapTargetSizeForBothDesigns()
    {
        Assert.Equal(
            new Size(60.0, 48.0),
            MountAndFindCustomPaint(ThemeData.Light, new Switch(false, _ => { })).Size);
        Assert.Equal(
            new Size(60.0, 40.0),
            MountAndFindCustomPaint(
                ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap },
                new Switch(false, _ => { })).Size);

        var m2 = new ThemeData(useMaterial3: false);
        Assert.Equal(
            new Size(59.0, 48.0),
            MountAndFindCustomPaint(m2, new Switch(false, _ => { })).Size);
        Assert.Equal(
            new Size(59.0, 40.0),
            MountAndFindCustomPaint(
                m2 with { MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap },
                new Switch(false, _ => { })).Size);
    }

    [Fact]
    public void Switch_Padding_IsRespectedFromWidgetAndTheme()
    {
        Assert.Equal(
            new Size(52.0, 48.0),
            MountAndFindCustomPaint(
                ThemeData.Light,
                new Switch(false, _ => { }, padding: new Thickness(0.0))).Size);
        Assert.Equal(
            new Size(60.0, 56.0),
            MountAndFindCustomPaint(
                ThemeData.Light,
                new Switch(false, _ => { }, padding: new Thickness(4.0))).Size);
        Assert.Equal(
            new Size(52.0, 48.0),
            MountAndFindCustomPaint(
                ThemeData.Light,
                new SwitchTheme(
                    data: new SwitchThemeData(Padding: new Thickness(0.0)),
                    child: new Switch(false, _ => { }))).Size);
    }

    [Fact]
    public void Switch_M3Geometry_UsesTokenTrackAndThumbRadii()
    {
        SwitchPainter painter = MountAndFindPainter(ThemeData.Light, new Switch(false, _ => { }));

        Assert.Equal(52.0, painter.TrackWidth);
        Assert.Equal(32.0, painter.TrackHeight);
        Assert.Equal(12.0, painter.ActiveThumbRadius);
        Assert.Equal(8.0, painter.InactiveThumbRadius);
        Assert.Equal(14.0, painter.PressedThumbRadius);
        Assert.False(painter.IsCupertino);
        Assert.Empty(painter.ThumbShadow!);
    }

    // ---- Property precedence ----

    [Fact]
    public void Switch_WidgetThumbColor_PrecedesActiveThumbColorAndThemeThumbColor()
    {
        SwitchPainter painter = MountAndFindPainter(
            ThemeData.Light,
            new SwitchTheme(
                data: new SwitchThemeData(
                    ThumbColor: MaterialStateProperty<Color?>.All(Colors.Olive)),
                child: new Switch(
                    value: true,
                    onChanged: _ => { },
                    activeThumbColor: Colors.Teal,
                    thumbColor: MaterialStateProperty<Color?>.All(Colors.Crimson))));

        Assert.Equal(Colors.Crimson, painter.ActiveColor);
    }

    [Fact]
    public void Switch_ThemeThumbColor_Applies_WhenWidgetThumbColorIsMissing()
    {
        SwitchPainter painter = MountAndFindPainter(
            ThemeData.Light,
            new SwitchTheme(
                data: new SwitchThemeData(
                    ThumbColor: MaterialStateProperty<Color?>.All(Colors.Olive)),
                child: new Switch(true, _ => { })));

        Assert.Equal(Colors.Olive, painter.ActiveColor);
    }

    [Fact]
    public void Switch_WidgetTrackColor_PrecedesSwitchThemeTrackColor()
    {
        SwitchPainter painter = MountAndFindPainter(
            ThemeData.Light,
            new SwitchTheme(
                data: new SwitchThemeData(
                    TrackColor: MaterialStateProperty<Color?>.All(Colors.Olive)),
                child: new Switch(
                    value: true,
                    onChanged: _ => { },
                    trackColor: MaterialStateProperty<Color?>.All(Colors.Crimson))));

        Assert.Equal(Colors.Crimson, painter.ActiveTrackColor);
    }

    [Fact]
    public void Switch_ActiveThumbColor_SuppliesFallbackActiveTrackAtHalfAlpha()
    {
        SwitchPainter painter = MountAndFindPainter(
            ThemeData.Light,
            new Switch(value: true, onChanged: _ => { }, activeThumbColor: Colors.Crimson));

        Assert.Equal(Colors.Crimson, painter.ActiveColor);
        Assert.Equal(
            Color.FromArgb(0x80, Colors.Crimson.R, Colors.Crimson.G, Colors.Crimson.B),
            painter.ActiveTrackColor);
    }

    [Fact]
    public void Switch_TrackOutline_ResolvesColorAndWidthFromThemeAndWidget()
    {
        SwitchPainter themed = MountAndFindPainter(
            ThemeData.Light,
            new SwitchTheme(
                data: new SwitchThemeData(
                    TrackOutlineColor: MaterialStateProperty<Color?>.ResolveWith(
                        states => states.HasFlag(MaterialState.Selected)
                            ? Colors.Indigo
                            : Colors.Maroon),
                    TrackOutlineWidth: MaterialStateProperty<double?>.ResolveWith(
                        states => states.HasFlag(MaterialState.Selected) ? 1.0 : 3.0)),
                child: new Switch(false, _ => { })));

        Assert.Equal(Colors.Maroon, themed.InactiveTrackOutlineColor);
        Assert.Equal(3.0, themed.InactiveTrackOutlineWidth);
        Assert.Equal(Colors.Indigo, themed.ActiveTrackOutlineColor);
        Assert.Equal(1.0, themed.ActiveTrackOutlineWidth);

        SwitchPainter overridden = MountAndFindPainter(
            ThemeData.Light,
            new SwitchTheme(
                data: new SwitchThemeData(
                    TrackOutlineColor: MaterialStateProperty<Color?>.All(Colors.Maroon),
                    TrackOutlineWidth: MaterialStateProperty<double?>.All(3.0)),
                child: new Switch(
                    value: false,
                    onChanged: _ => { },
                    trackOutlineColor: MaterialStateProperty<Color?>.All(Colors.Lime),
                    trackOutlineWidth: MaterialStateProperty<double?>.All(6.0))));

        Assert.Equal(Colors.Lime, overridden.InactiveTrackOutlineColor);
        Assert.Equal(6.0, overridden.InactiveTrackOutlineWidth);
    }

    [Fact]
    public void Switch_PressedOverlay_DefaultsToThumbColorAtRadialReactionAlpha()
    {
        SwitchPainter painter = MountAndFindPainter(
            ThemeData.Light,
            new Switch(
                value: true,
                onChanged: _ => { },
                activeThumbColor: Colors.Crimson,
                inactiveThumbColor: Colors.Teal));

        Assert.Equal(
            Color.FromArgb(0x1F, Colors.Crimson.R, Colors.Crimson.G, Colors.Crimson.B),
            painter.ReactionColor);
        Assert.Equal(
            Color.FromArgb(0x1F, Colors.Teal.R, Colors.Teal.G, Colors.Teal.B),
            painter.InactiveReactionColor);
    }

    [Fact]
    public void Switch_M3PressedColors_UsePrimaryContainerAndTwentyEightPixelThumb()
    {
        ThemeData baseTheme = ThemeData.Light;
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with
            {
                PrimaryContainer = Colors.MediumPurple,
                OnSurfaceVariant = Colors.SlateGray
            }
        };

        SwitchPainter painter = MountAndFindPainter(theme, new Switch(true, _ => { }));

        Assert.Equal(Colors.MediumPurple, painter.ActivePressedColor);
        Assert.Equal(Colors.SlateGray, painter.InactivePressedColor);
        Assert.Equal(14.0, painter.PressedThumbRadius);
    }

    [Fact]
    public void Switch_OverlayColor_ResolvesFocusAndHoverForBothDesigns()
    {
        ThemeData baseTheme = ThemeData.Light;
        ThemeData m3 = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with { Primary = Colors.Coral }
        };
        SwitchPainter m3Painter = MountAndFindPainter(m3, new Switch(true, _ => { }));
        Assert.Equal(ApplyOpacity(Colors.Coral, 0.1), m3Painter.FocusColor);
        Assert.Equal(ApplyOpacity(Colors.Coral, 0.08), m3Painter.HoverColor);
        Assert.Equal(20.0, m3Painter.SplashRadius);

        var m2 = new ThemeData(useMaterial3: false);
        SwitchPainter m2Painter = MountAndFindPainter(m2, new Switch(false, _ => { }));
        Assert.Equal(m2.FocusColor, m2Painter.FocusColor);
        Assert.Equal(m2.HoverColor, m2Painter.HoverColor);

        SwitchPainter overridden = MountAndFindPainter(
            ThemeData.Light,
            new Switch(
                value: false,
                onChanged: _ => { },
                focusColor: Colors.Fuchsia,
                hoverColor: Colors.Aquamarine,
                splashRadius: 30.0));
        Assert.Equal(Colors.Fuchsia, overridden.FocusColor);
        Assert.Equal(Colors.Aquamarine, overridden.HoverColor);
        Assert.Equal(30.0, overridden.SplashRadius);
    }

    [Fact]
    public void Switch_ThumbIcon_ResolvesPerStateWithConfigIconColor()
    {
        var activeIcon = new Icon(Icons.Check);
        var inactiveIcon = new Icon(Icons.Close);
        ThemeData baseTheme = ThemeData.Light;
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with
            {
                OnPrimaryContainer = Colors.DarkGreen,
                SurfaceContainerHighest = Colors.PowderBlue
            }
        };

        SwitchPainter painter = MountAndFindPainter(
            theme,
            new Switch(
                value: true,
                onChanged: _ => { },
                thumbIcon: MaterialStateProperty<Icon?>.ResolveWith(
                    states => states.HasFlag(MaterialState.Selected) ? activeIcon : inactiveIcon)));

        Assert.Same(activeIcon, painter.ActiveIcon);
        Assert.Same(inactiveIcon, painter.InactiveIcon);
        Assert.Equal(Colors.DarkGreen, painter.ActiveIconColor);
        Assert.Equal(Colors.PowderBlue, painter.InactiveIconColor);
        // An icon forces the thumb to the with-icon radius on both sides.
        Assert.Equal(12.0, painter.ActiveThumbRadius);
        Assert.Equal(12.0, painter.InactiveThumbRadius);
    }

    [Fact]
    public void Switch_ThumbImage_IsHandedToThePainterWithItsErrorListener()
    {
        var activeImage = new MemoryImage([1]);
        var inactiveImage = new MemoryImage([2]);

        SwitchPainter painter = MountAndFindPainter(
            ThemeData.Light,
            new Switch(
                value: true,
                onChanged: _ => { },
                activeThumbImage: activeImage,
                onActiveThumbImageError: (_, _) => { },
                inactiveThumbImage: inactiveImage,
                onInactiveThumbImageError: (_, _) => { }));

        Assert.Same(activeImage, painter.ActiveThumbImage);
        Assert.Same(inactiveImage, painter.InactiveThumbImage);
        // An inactive thumb image also forces the with-icon radius, exactly like an icon does.
        Assert.Equal(12.0, painter.InactiveThumbRadius);
    }

    [Fact]
    public void Switch_PublicApi_ExposesSourceImageCursorDragAndAdaptiveFields()
    {
        var activeImage = new MemoryImage([1]);
        var inactiveImage = new MemoryImage([2]);
        ImageErrorListener activeError = (_, _) => { };
        ImageErrorListener inactiveError = (_, _) => { };
        var cursor = new SystemMouseCursor("switch");

        var materialSwitch = new Switch(
            value: true,
            onChanged: _ => { },
            activeThumbImage: activeImage,
            onActiveThumbImageError: activeError,
            inactiveThumbImage: inactiveImage,
            onInactiveThumbImageError: inactiveError,
            dragStartBehavior: DragStartBehavior.Down,
            mouseCursor: cursor);
        Switch adaptiveSwitch = Switch.Adaptive(
            value: false,
            onChanged: _ => { },
            applyCupertinoTheme: true);

        Assert.Same(activeImage, materialSwitch.ActiveThumbImage);
        Assert.Same(activeError, materialSwitch.OnActiveThumbImageError);
        Assert.Same(inactiveImage, materialSwitch.InactiveThumbImage);
        Assert.Same(inactiveError, materialSwitch.OnInactiveThumbImageError);
        Assert.Equal(DragStartBehavior.Down, materialSwitch.DragStartBehavior);
        Assert.Equal(cursor, materialSwitch.MouseCursor);
        Assert.True(adaptiveSwitch.ApplyCupertinoTheme);
        Assert.Null(typeof(Switch).GetProperty("SemanticLabel"));
        Assert.DoesNotContain(
            typeof(Switch).GetConstructors().SelectMany(constructor => constructor.GetParameters()),
            parameter => parameter.Name == "semanticLabel");
        Assert.DoesNotContain(
            typeof(Switch).GetMethod(nameof(Switch.Adaptive))!.GetParameters(),
            parameter => parameter.Name == "semanticLabel");
        Assert.Throws<ArgumentException>(() => new Switch(
            value: false,
            onChanged: _ => { },
            onActiveThumbImageError: activeError));
        Assert.Throws<ArgumentException>(() => Switch.Adaptive(
            value: false,
            onChanged: _ => { },
            onInactiveThumbImageError: inactiveError));
    }

    // ---- Switch.adaptive ----

    [Fact]
    public void Switch_AdaptiveApple_UsesCupertinoConfigWithoutBuildingACupertinoSwitch()
    {
        var theme = new ThemeData(platform: TargetPlatform.IOS);
        TestRootElement root = Mount(theme, Switch.Adaptive(false, _ => { }));

        Assert.Null(FindWidget<CupertinoSwitch>(root.ChildElement));
        SwitchPainter painter = FindSwitchPainter(root.ChildElement);
        Assert.True(painter.IsCupertino);
        Assert.Equal(51.0, painter.TrackWidth);
        Assert.Equal(31.0, painter.TrackHeight);
        Assert.Equal(14.0, painter.ActiveThumbRadius);
        Assert.Equal(14.0, painter.InactiveThumbRadius);
        Assert.Equal(Colors.White, painter.ActiveColor);
        Assert.Equal(Colors.White, painter.InactiveColor);
        Assert.Equal(Color.FromArgb(255, 52, 199, 89), painter.ActiveTrackColor);
        Assert.Equal(Color.FromArgb(40, 120, 120, 128), painter.InactiveTrackColor);
        Assert.Equal(Plumix.Material.Colors.Transparent, painter.InactiveTrackOutlineColor);
        Assert.Equal(0.0, painter.SplashRadius);
        // The widget is still sized by the Material config, not the Cupertino one.
        Assert.Equal(new Size(60.0, 48.0), FindWidget<CustomPaint>(root.ChildElement)!.Size);
    }

    [Fact]
    public void Switch_AdaptiveApple_IgnoresAmbientSwitchThemeButNonAppleDoesNot()
    {
        var themeData = new SwitchThemeData(
            ThumbColor: MaterialStateProperty<Color?>.All(Colors.Brown),
            TrackColor: MaterialStateProperty<Color?>.All(Colors.Yellow));

        SwitchPainter apple = MountAndFindPainter(
            new ThemeData(platform: TargetPlatform.IOS),
            new SwitchTheme(data: themeData, child: Switch.Adaptive(true, _ => { })));
        Assert.Equal(Colors.White, apple.ActiveColor);
        Assert.Equal(Color.FromArgb(255, 52, 199, 89), apple.ActiveTrackColor);

        SwitchPainter android = MountAndFindPainter(
            new ThemeData(platform: TargetPlatform.Android),
            new SwitchTheme(data: themeData, child: Switch.Adaptive(true, _ => { })));
        Assert.Equal(Colors.Brown, android.ActiveColor);
        Assert.Equal(Colors.Yellow, android.ActiveTrackColor);
    }

    [Fact]
    public void Switch_AdaptiveApple_HonorsACustomSwitchThemeDataAdaptation()
    {
        var localTheme = new SwitchThemeData(
            ThumbColor: MaterialStateProperty<Color?>.All(Colors.Brown),
            TrackColor: MaterialStateProperty<Color?>.All(Colors.Yellow));

        SwitchPainter apple = MountAndFindPainter(
            new ThemeData(
                platform: TargetPlatform.IOS,
                adaptations: [new PurpleSwitchAdaptation()]),
            new SwitchTheme(data: localTheme, child: Switch.Adaptive(true, _ => { })));
        Assert.Equal(Plumix.Material.Colors.LightGreen.Shade500, apple.ActiveColor);
        Assert.Equal(Plumix.Material.Colors.DeepPurple.Shade500, apple.ActiveTrackColor);

        SwitchPainter android = MountAndFindPainter(
            new ThemeData(
                platform: TargetPlatform.Android,
                adaptations: [new PurpleSwitchAdaptation()]),
            new SwitchTheme(data: localTheme, child: Switch.Adaptive(true, _ => { })));
        Assert.Equal(Colors.Brown, android.ActiveColor);
        Assert.Equal(Colors.Yellow, android.ActiveTrackColor);
    }

    [Fact]
    public void Switch_AdaptiveApple_MapsDeprecatedActiveColorToTheTrack()
    {
#pragma warning disable CS0618
        SwitchPainter apple = MountAndFindPainter(
            new ThemeData(platform: TargetPlatform.IOS),
            Switch.Adaptive(value: true, onChanged: _ => { }, activeColor: Colors.Crimson));
        Assert.Equal(Colors.Crimson, apple.ActiveTrackColor);
        Assert.Equal(Colors.White, apple.ActiveColor);

        SwitchPainter android = MountAndFindPainter(
            new ThemeData(platform: TargetPlatform.Android),
            Switch.Adaptive(value: true, onChanged: _ => { }, activeColor: Colors.Crimson));
        Assert.Equal(Colors.Crimson, android.ActiveColor);
#pragma warning restore CS0618
    }

    [Fact]
    public void Switch_AdaptiveApple_FocusUsesTheCupertinoGreenDerivedRing()
    {
        SwitchPainter painter = MountAndFindPainter(
            new ThemeData(platform: TargetPlatform.MacOS),
            Switch.Adaptive(true, _ => { }));

        Assert.Equal(Color.FromUInt32(0xCC6EF28F), painter.FocusColor);
    }

    [Fact]
    public void Switch_AdaptiveApple_Disabled_HalvesOpacity()
    {
        TestRootElement enabled = Mount(
            new ThemeData(platform: TargetPlatform.IOS),
            Switch.Adaptive(true, _ => { }));
        TestRootElement disabled = Mount(
            new ThemeData(platform: TargetPlatform.IOS),
            Switch.Adaptive(true, onChanged: null));

        Assert.Equal(1.0, FindWidget<Opacity>(enabled.ChildElement)!.Value);
        Assert.Equal(0.5, FindWidget<Opacity>(disabled.ChildElement)!.Value);

        TestRootElement material = Mount(ThemeData.Light, new Switch(true, onChanged: null));
        Assert.Equal(1.0, FindWidget<Opacity>(material.ChildElement)!.Value);
    }

    [Fact]
    public void Switch_AdaptiveApple_ApplyCupertinoTheme_UsesThePrimaryColorForTheTrack()
    {
        ThemeData baseTheme = new ThemeData(platform: TargetPlatform.IOS);
        ThemeData theme = baseTheme with
        {
            ColorScheme = baseTheme.ColorScheme with { Primary = Colors.Coral }
        };

        SwitchPainter painter = MountAndFindPainter(
            theme,
            Switch.Adaptive(value: true, onChanged: _ => { }, applyCupertinoTheme: true));

        Assert.Equal(Colors.Coral, painter.ActiveTrackColor);
    }

    // ---- Interaction ----

    [Fact]
    public void Switch_Tap_TogglesTheValue()
    {
        bool? next = null;
        using var harness = new WidgetRenderHarness(
            new Theme(data: ThemeData.Light, child: new Switch(false, value => next = value)));
        harness.Pump(new Size(200.0, 200.0));

        GestureBinding binding = GestureBinding.Instance;
        DispatchPointerDown(binding, harness.RenderView, 900, new Point(30.0, 24.0));
        DispatchPointerUp(binding, harness.RenderView, 900, new Point(30.0, 24.0));
        harness.Pump(new Size(200.0, 200.0));

        Assert.True(next);
    }

    [Fact]
    public void Switch_KeyboardActivation_TogglesFalseToTrue()
    {
        try
        {
            var owner = new BuildOwner();
            var focusNode = new FocusNode();
            bool nextValue = false;
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new Switch(
                        value: false,
                        focusNode: focusNode,
                        onChanged: value => nextValue = value)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            focusNode.RequestFocus();
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.True(nextValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Switch_Drag_CommitsOnlyWhenItCrossesTheMidpoint()
    {
        bool? reported = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Switch(false, next => reported = next)));
        harness.Pump(new Size(200.0, 200.0));

        GestureBinding binding = GestureBinding.Instance;
        // Dragging an "off" switch further left never crosses the midpoint, so nothing is reported.
        DispatchPointerDown(binding, harness.RenderView, 910, new Point(60.0, 24.0));
        // The first move only carries the drag past the mouse hit slop and wins the arena; with
        // `DragStartBehavior.Start` the drag starts there and updates begin with the next move.
        DispatchPointerMove(binding, harness.RenderView, 910, new Point(56.0, 24.0));
        DispatchPointerMove(binding, harness.RenderView, 910, new Point(20.0, 24.0));
        DispatchPointerUp(binding, harness.RenderView, 910, new Point(20.0, 24.0));
        harness.Pump(new Size(200.0, 200.0));
        Assert.Null(reported);

        // Dragging right past the midpoint reports the flipped value.
        DispatchPointerDown(binding, harness.RenderView, 911, new Point(20.0, 24.0));
        DispatchPointerMove(binding, harness.RenderView, 911, new Point(24.0, 24.0));
        DispatchPointerMove(binding, harness.RenderView, 911, new Point(60.0, 24.0));
        DispatchPointerUp(binding, harness.RenderView, 911, new Point(60.0, 24.0));
        harness.Pump(new Size(200.0, 200.0));
        Assert.True(reported);
    }

    [Fact]
    public void Switch_Semantics_ExposesToggledStateTapActionAndExternalLabel()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Semantics(
                    label: "Wi-Fi",
                    child: new Switch(value: true, onChanged: _ => { }))));
        SemanticsNode? rootNode = harness.PumpAndGetSemantics(new Size(200.0, 200.0));

        Assert.NotNull(rootNode);
        SemanticsNode? toggled = FindFirstSemanticsNode(
            rootNode!,
            node => node.Flags.HasFlag(SemanticsFlags.HasToggledState));
        Assert.NotNull(toggled);
        Assert.True(toggled!.Flags.HasFlag(SemanticsFlags.IsToggled));

        SemanticsNode? tappable = FindFirstSemanticsNode(
            rootNode!,
            node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(tappable);

        SemanticsNode? labelled = FindFirstSemanticsNode(
            rootNode!,
            node => node.Label == "Wi-Fi");
        Assert.NotNull(labelled);
    }

    [Fact]
    public void Switch_MergeSemantics_EmitsTapEventFromTheLabelledParent()
    {
        SemanticsEvent? received = null;
        void HandleEvent(SemanticsEvent semanticsEvent) => received = semanticsEvent;
        SemanticsService.SemanticsEventRequested += HandleEvent;
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new MergeSemantics(
                        child: new Semantics(
                            label: "Wi-Fi",
                            child: new Switch(value: false, onChanged: _ => { })))));
            SemanticsNode? rootNode = harness.PumpAndGetSemantics(new Size(200.0, 200.0));
            SemanticsNode? merged = FindFirstSemanticsNode(rootNode!, node => node.Label == "Wi-Fi");

            Assert.NotNull(merged);
            Assert.True(merged!.Actions.HasFlag(SemanticsActions.Tap));

            GestureBinding binding = GestureBinding.Instance;
            DispatchPointerDown(binding, harness.RenderView, 912, new Point(30.0, 24.0));
            DispatchPointerUp(binding, harness.RenderView, 912, new Point(30.0, 24.0));

            TapSemanticEvent tapEvent = Assert.IsType<TapSemanticEvent>(received);
            Assert.Equal(merged.Id, tapEvent.NodeId);
        }
        finally
        {
            SemanticsService.SemanticsEventRequested -= HandleEvent;
        }
    }

    [Fact]
    public void Switch_Disabled_HasNoTapSemanticsAction()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(data: ThemeData.Light, child: new Switch(true, onChanged: null)));
        SemanticsNode? rootNode = harness.PumpAndGetSemantics(new Size(200.0, 200.0));

        Assert.NotNull(rootNode);
        Assert.Null(FindFirstSemanticsNode(
            rootNode!,
            node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void Switch_ToggleDuration_MatchesTheDesignConfig()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(300),
            MountAndFindPainter(ThemeData.Light, new Switch(false, _ => { }))
                .PositionController.Duration);
        Assert.Equal(
            TimeSpan.FromMilliseconds(200),
            MountAndFindPainter(new ThemeData(useMaterial3: false), new Switch(false, _ => { }))
                .PositionController.Duration);
        Assert.Equal(
            TimeSpan.FromMilliseconds(140),
            MountAndFindPainter(
                    new ThemeData(platform: TargetPlatform.IOS),
                    Switch.Adaptive(false, _ => { }))
                .PositionController.Duration);
    }

    [Fact]
    public void Switch_PositionController_StartsAtTheCurrentValue()
    {
        Assert.Equal(
            0.0,
            MountAndFindPainter(ThemeData.Light, new Switch(false, _ => { }))
                .PositionController.Value);
        Assert.Equal(
            1.0,
            MountAndFindPainter(ThemeData.Light, new Switch(true, _ => { }))
                .PositionController.Value);
    }

    [Fact]
    public void Switch_Paint_RunsForEveryDesignAndPlatformFlavour()
    {
        Widget[] cases =
        [
            new Theme(data: ThemeData.Light, child: new Switch(true, _ => { })),
            new Theme(
                data: new ThemeData(useMaterial3: false),
                child: new Switch(false, _ => { })),
            new Theme(
                data: new ThemeData(platform: TargetPlatform.IOS),
                child: Switch.Adaptive(true, _ => { })),
            new Theme(
                data: ThemeData.Light,
                child: new Switch(
                    value: true,
                    onChanged: _ => { },
                    thumbIcon: MaterialStateProperty<Icon?>.All(new Icon(Icons.Check)),
                    trackOutlineColor: MaterialStateProperty<Color?>.All(Colors.Indigo),
                    trackOutlineWidth: MaterialStateProperty<double?>.All(3.0))),
        ];

        foreach (Widget widget in cases)
        {
            using var harness = new WidgetRenderHarness(widget);
            harness.Pump(new Size(200.0, 200.0));
            Assert.NotNull(FindDescendant<RenderCustomPaint>(harness.RenderView));
        }
    }

    private static SwitchPainter MountAndFindPainter(ThemeData theme, Widget child)
    {
        return FindSwitchPainter(Mount(theme, child).ChildElement);
    }

    private static CustomPaint MountAndFindCustomPaint(ThemeData theme, Widget child)
    {
        CustomPaint? paint = FindWidget<CustomPaint>(Mount(theme, child).ChildElement);
        Assert.NotNull(paint);
        return paint!;
    }

    private static TestRootElement Mount(ThemeData theme, Widget child)
    {
        var root = new TestRootElement(new Theme(data: theme, child: child));
        var owner = new BuildOwner();
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        return root;
    }

    private static SwitchPainter FindSwitchPainter(Element? root)
    {
        CustomPaint? customPaint = FindWidget<CustomPaint>(root);
        return Assert.IsType<SwitchPainter>(customPaint?.Painter);
    }

    private sealed class PurpleSwitchAdaptation : Adaptation<SwitchThemeData>
    {
        public override SwitchThemeData Adapt(ThemeData theme, SwitchThemeData defaultValue)
        {
            return theme.Platform switch
            {
                TargetPlatform.IOS or TargetPlatform.MacOS => new SwitchThemeData(
                    ThumbColor: MaterialStateProperty<Color?>.All(Plumix.Material.Colors.LightGreen.Shade500),
                    TrackColor: MaterialStateProperty<Color?>.All(Plumix.Material.Colors.DeepPurple.Shade500)),
                _ => defaultValue,
            };
        }
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsAssignableFrom<T>(element.RenderObject);
    }

    private static TestRootElement MountSwitch(
        ThemeData theme,
        bool value,
        Action<bool>? onChanged,
        Color? activeThumbColor = null,
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        bool adaptive = false)
    {
        Widget child = adaptive
            ? Switch.Adaptive(
                value: value,
                onChanged: onChanged,
                activeThumbColor: activeThumbColor,
                activeThumbImage: activeThumbImage,
                onActiveThumbImageError: onActiveThumbImageError,
                inactiveThumbImage: inactiveThumbImage,
                onInactiveThumbImageError: onInactiveThumbImageError)
            : new Switch(
                value: value,
                onChanged: onChanged,
                activeThumbColor: activeThumbColor,
                activeThumbImage: activeThumbImage,
                onActiveThumbImageError: onActiveThumbImageError,
                inactiveThumbImage: inactiveThumbImage,
                onInactiveThumbImageError: onInactiveThumbImageError);
        var root = new TestRootElement(new Theme(data: theme, child: child));
        var owner = new BuildOwner();
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        return root;
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

    private static CupertinoSwitchPainter FindCupertinoPainter(Element? root)
    {
        CustomPaint? customPaint = FindWidget<CustomPaint>(root);
        return Assert.IsType<CupertinoSwitchPainter>(customPaint?.Painter);
    }

    private static T? FindWidget<T>(Element? root) where T : Widget
    {
        if (root is null)
        {
            return null;
        }
        if (root.Widget is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindWidget<T>(child));
        return result;
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var results = new List<T>();
        CollectDescendants(root, results);
        return results;
    }

    private static void CollectDescendants<T>(RenderObject? root, List<T> results) where T : RenderObject
    {
        if (root is null)
        {
            return;
        }

        if (root is T typed)
        {
            results.Add(typed);
        }

        root.VisitChildren(child => CollectDescendants(child, results));
    }

    private static RenderDecoratedBox? FindTrackDecoration(RenderObject root)
    {
        var boxes = FindDescendants<RenderDecoratedBox>(root);
        return boxes.FirstOrDefault(box => box.Decoration.Border is not null)
               ?? boxes.FirstOrDefault(box =>
                   box.Decoration.Color.HasValue
                   && box.Decoration.Color.Value.A > 0);
    }

    private static RenderDecoratedBox? FindThumbDecoration(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .LastOrDefault(box =>
                box.Decoration.Color.HasValue
                && box.Decoration.Color.Value.A > 0
                && box.Decoration.Border is null);
    }

    private static RenderDecoratedBox? FindDecoratedBoxBySize(
        RenderObject root,
        double width,
        double height,
        double tolerance = 0.01)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .FirstOrDefault(box =>
                Math.Abs(box.Size.Width - width) <= tolerance
                && Math.Abs(box.Size.Height - height) <= tolerance);
    }

    private static bool IsSize(Size actual, double width, double height, double tolerance = 0.01)
    {
        return Math.Abs(actual.Width - width) <= tolerance
               && Math.Abs(actual.Height - height) <= tolerance;
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

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Color AlphaBlend(Color foreground, Color background)
    {
        double foregroundAlpha = foreground.A / 255.0;
        double backgroundAlpha = background.A / 255.0;
        double outputAlpha = foregroundAlpha + (backgroundAlpha * (1.0 - foregroundAlpha));
        byte BlendChannel(byte foregroundChannel, byte backgroundChannel)
        {
            double numerator = (foregroundChannel * foregroundAlpha)
                               + (backgroundChannel * backgroundAlpha * (1.0 - foregroundAlpha));
            return (byte)Math.Round(numerator / outputAlpha);
        }

        return Color.FromArgb(
            (byte)Math.Round(outputAlpha * 255.0),
            BlendChannel(foreground.R, background.R),
            BlendChannel(foreground.G, background.G),
            BlendChannel(foreground.B, background.B));
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

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var result = new List<T>();
            Visit(_rootElement, result);
            return result;
        }

        public void Update(Widget widget)
        {
            _rootElement.Update(widget);
        }

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

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private static void Visit<T>(Element element, List<T> result) where T : Widget
        {
            if (element.Widget is T widget)
            {
                result.Add(widget);
            }
            element.VisitChildren(child => Visit(child, result));
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
}
