using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;
using MaterialSurface = Plumix.Material.Material;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/elevation_overlay.dart
// flutter/packages/flutter/lib/src/material/material.dart
// flutter/packages/flutter/test/material/elevation_overlay_test.dart
// flutter/packages/flutter/test/material/material_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialElevationOverlayTests : IDisposable
{
    public MaterialElevationOverlayTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ApplySurfaceTint_NullAndTransparentTintReturnOriginalColor()
    {
        Color color = Color.Parse("#FF888888");

        Assert.Equal(color, ElevationOverlay.ApplySurfaceTint(color, null, 42.0));
        Assert.Equal(color, ElevationOverlay.ApplySurfaceTint(color, Colors.Transparent, 42.0));
    }

    [Theory]
    [InlineData(-42.0, "#FF888888")]
    [InlineData(0.0, "#FF888888")]
    [InlineData(1.0, "#FF858B8E")]
    [InlineData(3.0, "#FF838D91")]
    [InlineData(6.0, "#FF818F95")]
    [InlineData(8.0, "#FF809096")]
    [InlineData(12.0, "#FF7E9299")]
    [InlineData(42.0, "#FF7E9299")]
    [InlineData(9.2, "#FF7F9197")]
    [InlineData(10.0, "#FF7F9197")]
    [InlineData(10.4, "#FF7F9198")]
    public void ApplySurfaceTint_ClampsExactLevelsAndInterpolatesBetweenThem(
        double elevation,
        string expected)
    {
        Color result = ElevationOverlay.ApplySurfaceTint(
            Color.Parse("#FF888888"),
            Color.Parse("#FF44CCFF"),
            elevation);

        Assert.Equal(Color.Parse(expected), result);
    }

    [Fact]
    public void ApplySurfaceTint_ReplacesAProvidedTintAlphaLikeFlutterWithOpacity()
    {
        Color baseColor = Color.Parse("#FF121212");
        Color opaqueTint = Color.Parse("#FF44CCFF");
        Color translucentTint = Color.Parse("#8044CCFF");

        Assert.Equal(
            ElevationOverlay.ApplySurfaceTint(baseColor, opaqueTint, 12.0),
            ElevationOverlay.ApplySurfaceTint(baseColor, translucentTint, 12.0));
    }

    [Fact]
    public void OverlayColorAndApplyOverlay_UseAmbientDarkThemeSurfacePolicy()
    {
        Color surface = Color.Parse("#FF121212");
        Color onSurface = Color.Parse("#FF69F0AE");
        Color? overlayColor = null;
        Color? appliedColor = null;
        var theme = new ThemeData(
            brightness: Brightness.Dark,
            useMaterial3: false,
            applyElevationOverlayColor: true,
            surfaceColor: surface,
            onSurfaceColor: onSurface);
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                theme,
                new Builder(context =>
                {
                    overlayColor = ElevationOverlay.OverlayColor(context, 8.0);
                    appliedColor = ElevationOverlay.ApplyOverlay(context, surface, 8.0);
                    return new SizedBox();
                })));

        MountAndFlush(root, owner);

        Assert.Equal(Color.Parse("#1E69F0AE"), overlayColor);
        Assert.Equal(Color.Parse("#FF1C2C24"), appliedColor);
        root.Unmount();
    }

    [Fact]
    public void ApplyOverlay_RequiresPositiveElevationEnabledDarkThemeAndSurfaceRgb()
    {
        Color surface = Color.Parse("#FF121212");
        Color overlay = Color.Parse("#FF69F0AE");
        var enabled = new ThemeData(
            brightness: Brightness.Dark,
            useMaterial3: false,
            applyElevationOverlayColor: true,
            surfaceColor: surface,
            onSurfaceColor: overlay);

        Assert.Equal(surface, ElevationOverlay.ApplyOverlay(enabled, surface, 0.0));
        Assert.Equal(
            Colors.Cyan,
            ElevationOverlay.ApplyOverlay(enabled, Colors.Cyan, 8.0));
        Assert.Equal(
            surface,
            ElevationOverlay.ApplyOverlay(
                enabled with { ApplyElevationOverlayColor = false },
                surface,
                8.0));
        Assert.Equal(
            surface,
            ElevationOverlay.ApplyOverlay(
                enabled with { Brightness = Brightness.Light },
                surface,
                8.0));

        Color translucentSurface = Color.FromArgb(0xBF, surface.R, surface.G, surface.B);
        Color result = ElevationOverlay.ApplyOverlay(enabled, translucentSurface, 8.0);
        Assert.NotEqual(translucentSurface, result);
    }

    [Fact]
    public void ThemeData_DefaultsOverlayPolicyFromBrightnessAndLerpsItDiscretely()
    {
        var light = new ThemeData(brightness: Brightness.Light);
        var dark = new ThemeData(brightness: Brightness.Dark);
        var m2Dark = new ThemeData(
            brightness: Brightness.Dark,
            useMaterial3: false);
        var disabledDark = new ThemeData(
            brightness: Brightness.Dark,
            applyElevationOverlayColor: false);

        Assert.False(light.ApplyElevationOverlayColor);
        Assert.True(dark.ApplyElevationOverlayColor);
        Assert.False(m2Dark.ApplyElevationOverlayColor);
        Assert.False(disabledDark.ApplyElevationOverlayColor);
        Assert.False(ThemeData.Lerp(light, dark, 0.49).ApplyElevationOverlayColor);
        Assert.True(ThemeData.Lerp(light, dark, 0.5).ApplyElevationOverlayColor);
    }

    [Fact]
    public void Material_UsesM2OverlayOrM3SurfaceTintAccordingToThemeMode()
    {
        Color surface = Color.Parse("#FF121212");
        Color onSurface = Color.Parse("#FF69F0AE");
        Color tint = Color.Parse("#FF44CCFF");
        var m2Theme = new ThemeData(
            brightness: Brightness.Dark,
            useMaterial3: false,
            applyElevationOverlayColor: true,
            canvasColor: surface,
            surfaceColor: surface,
            onSurfaceColor: onSurface);
        var m3Theme = m2Theme with { UseMaterial3 = true };

        Assert.Equal(
            Color.Parse("#FF1C2C24"),
            ResolveMaterialColor(
                m2Theme,
                new MaterialSurface(
                    color: surface,
                    elevation: 8.0,
                    child: new SizedBox())));
        Assert.Equal(
            surface,
            ResolveMaterialColor(
                m3Theme,
                new MaterialSurface(
                    color: surface,
                    elevation: 8.0,
                    child: new SizedBox())));
        Assert.Equal(
            Color.Parse("#FF192C33"),
            ResolveMaterialColor(
                m3Theme,
                new MaterialSurface(
                    color: surface,
                    surfaceTintColor: tint,
                    elevation: 12.0,
                    child: new SizedBox())));
    }

    private static Color ResolveMaterialColor(ThemeData theme, MaterialSurface material)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Theme(theme, material));
        MountAndFlush(root, owner);
        var decorations = new List<DecoratedBox>();
        CollectWidgets(root, decorations);
        DecoratedBox decoration = Assert.Single(decorations);
        BoxDecoration boxDecoration = Assert.IsType<BoxDecoration>(decoration.Decoration);
        root.Unmount();
        return boxDecoration.Color
               ?? throw new InvalidOperationException("Material did not resolve a surface color.");
    }

    private static void CollectWidgets<T>(Element element, List<T> results) where T : Widget
    {
        if (element.Widget is T widget)
        {
            results.Add(widget);
        }

        element.VisitChildren(child => CollectWidgets(child, results));
    }

    private static void MountAndFlush(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }

        internal override void Unmount()
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
