using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

public sealed class MaterialCardTests
{
    [Fact]
    public void Card_Throws_WhenElevationIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Card(elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CardThemeData(Elevation: -1));
    }

    [Fact]
    public void Card_DefaultM3Elevated_UsesSurfaceContainerLowRadius12MarginAndShadow()
    {
        var theme = ThemeData.Light;
        using var harness = new WidgetRenderHarness(
            BuildThemedCard(new Card(child: new SizedBox(width: 100, height: 40)), theme));

        harness.Pump(new Size(220, 140));

        var material = FindMaterialDecoration(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(theme.SurfaceContainerLowColor, material!.Decoration.Color);
        Assert.Equal(12, material.Decoration.EffectiveBorderRadius.Radius);
        Assert.True(material.Decoration.BoxShadows.HasValue);
        Assert.Equal(172, material.Size.Width, 3);
        Assert.Equal(88, material.Size.Height, 3);

        var margin = FindDescendant<RenderPadding>(harness.RenderView);
        Assert.NotNull(margin);
        Assert.Equal(new Thickness(4), margin!.Padding);
    }

    [Fact]
    public void Card_FilledAndOutlinedM3_UseVariantDefaults()
    {
        var theme = ThemeData.Light;
        using var filledHarness = new WidgetRenderHarness(
            BuildThemedCard(Card.Filled(child: new SizedBox(width: 80, height: 32)), theme));
        filledHarness.Pump(new Size(220, 140));

        var filled = FindMaterialDecoration(filledHarness.RenderView);
        Assert.NotNull(filled);
        Assert.Equal(theme.SurfaceContainerHighestColor, filled!.Decoration.Color);
        Assert.False(filled.Decoration.BoxShadows.HasValue);

        using var outlinedHarness = new WidgetRenderHarness(
            BuildThemedCard(Card.Outlined(child: new SizedBox(width: 80, height: 32)), theme));
        outlinedHarness.Pump(new Size(220, 140));

        var outlinedBackground = FindMaterialDecoration(outlinedHarness.RenderView);
        var outlinedBorder = FindDescendants<RenderDecoratedBox>(outlinedHarness.RenderView)
            .FirstOrDefault(box => box.Decoration.Border.HasValue);

        Assert.NotNull(outlinedBackground);
        Assert.Equal(theme.SurfaceColor, outlinedBackground!.Decoration.Color);
        Assert.False(outlinedBackground.Decoration.BoxShadows.HasValue);
        Assert.NotNull(outlinedBorder);
        Assert.Equal(theme.OutlineVariantColor, outlinedBorder!.Decoration.Border!.Value.Color);
        Assert.Equal(12, outlinedBorder.Decoration.EffectiveBorderRadius.Radius);
    }

    [Fact]
    public void Card_M2Variants_FallBackToElevatedM2Defaults()
    {
        var cardColor = Color.Parse("#FFFAFAFA");
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            CardColor = cardColor,
            ShadowColor = Colors.DarkSlateGray
        };

        using var harness = new WidgetRenderHarness(
            BuildThemedCard(Card.Outlined(child: new SizedBox(width: 96, height: 36)), theme));

        harness.Pump(new Size(220, 140));

        var material = FindMaterialDecoration(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(cardColor, material!.Decoration.Color);
        Assert.Equal(4, material.Decoration.EffectiveBorderRadius.Radius);
        Assert.False(material.Decoration.Border.HasValue);
        Assert.True(material.Decoration.BoxShadows.HasValue);
    }

    [Fact]
    public void Card_ThemeDefaults_AreUsed_WhenWidgetValuesAreNull()
    {
        var themeColor = Color.Parse("#FFEAF6FF");
        var theme = ThemeData.Light with
        {
            CardTheme = new CardThemeData(
                Color: themeColor,
                ShadowColor: Colors.DarkGreen,
                Elevation: 3,
                Margin: new Thickness(9),
                Shape: ShapeBorder.RoundedRectangle(18),
                ClipBehavior: Clip.AntiAlias)
        };

        using var harness = new WidgetRenderHarness(
            BuildThemedCard(new Card(child: new SizedBox(width: 90, height: 38)), theme));

        harness.Pump(new Size(240, 160));

        var material = FindMaterialDecoration(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(themeColor, material!.Decoration.Color);
        Assert.Equal(18, material.Decoration.EffectiveBorderRadius.Radius);
        Assert.True(material.Decoration.BoxShadows.HasValue);

        var margin = FindDescendant<RenderPadding>(harness.RenderView);
        Assert.NotNull(margin);
        Assert.Equal(new Thickness(9), margin!.Padding);
        Assert.NotNull(FindDescendant<RenderClipRRect>(harness.RenderView));
    }

    [Fact]
    public void Card_WidgetValues_OverrideCardThemes()
    {
        var themeColor = Color.Parse("#FFEAF6FF");
        var localThemeColor = Color.Parse("#FFFFF1D9");
        var widgetColor = Color.Parse("#FFE8F5E9");
        var theme = ThemeData.Light with
        {
            CardTheme = new CardThemeData(Color: themeColor)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new CardTheme(
                    data: new CardThemeData(
                        Color: localThemeColor,
                        Shape: ShapeBorder.RoundedRectangle(22)),
                    child: new SizedBox(
                        width: 180,
                        height: 96,
                        child: new Card(
                            color: widgetColor,
                            shape: ShapeBorder.RoundedRectangle(6),
                            child: new SizedBox())))));

        harness.Pump(new Size(240, 160));

        var material = FindMaterialDecoration(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(widgetColor, material!.Decoration.Color);
        Assert.Equal(6, material.Decoration.EffectiveBorderRadius.Radius);
    }

    [Fact]
    public void Card_SurfaceTintColor_TintsBackgroundByElevation()
    {
        var baseColor = Color.Parse("#FFF7F2FA");
        var tint = Colors.Red;
        using var harness = new WidgetRenderHarness(
            BuildThemedCard(new Card(
                color: baseColor,
                surfaceTintColor: tint,
                elevation: 3,
                child: new SizedBox(width: 80, height: 32))));

        harness.Pump(new Size(220, 140));

        var material = FindMaterialDecoration(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(ApplySurfaceTint(baseColor, tint, 3), material!.Decoration.Color);
    }

    [Fact]
    public void Card_ClipBehavior_InsertsClipRRectOnlyWhenRequested()
    {
        using var defaultHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(child: new SizedBox(width: 80, height: 32))));
        defaultHarness.Pump(new Size(220, 140));

        Assert.Null(FindDescendant<RenderClipRRect>(defaultHarness.RenderView));

        using var clippedHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(
                clipBehavior: Clip.AntiAlias,
                shape: ShapeBorder.RoundedRectangle(20),
                child: new SizedBox(width: 80, height: 32))));
        clippedHarness.Pump(new Size(220, 140));

        var clip = FindDescendant<RenderClipRRect>(clippedHarness.RenderView);
        Assert.NotNull(clip);
        Assert.Equal(20, clip!.BorderRadius.Radius);
    }

    [Fact]
    public void Card_SemanticContainer_DefaultsToContainerAndCanBeDisabled()
    {
        using var defaultHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(child: new SizedBox(width: 80, height: 32))));
        defaultHarness.Pump(new Size(220, 140));

        var defaultSemantics = FindDescendant<RenderSemanticsAnnotations>(defaultHarness.RenderView);
        Assert.NotNull(defaultSemantics);
        Assert.True(defaultSemantics!.Container);
        Assert.False(defaultSemantics.ExplicitChildNodes);

        using var explicitHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(
                semanticContainer: false,
                child: new SizedBox(width: 80, height: 32))));
        explicitHarness.Pump(new Size(220, 140));

        var explicitSemantics = FindDescendant<RenderSemanticsAnnotations>(explicitHarness.RenderView);
        Assert.NotNull(explicitSemantics);
        Assert.False(explicitSemantics!.Container);
        Assert.True(explicitSemantics.ExplicitChildNodes);
    }

    private static Widget BuildThemedCard(Widget card, ThemeData? theme = null)
    {
        return new Theme(
            data: theme ?? ThemeData.Light,
            child: new SizedBox(
                width: 180,
                height: 96,
                child: card));
    }

    private static RenderDecoratedBox? FindMaterialDecoration(RenderObject? root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .FirstOrDefault(box => box.Decoration.Color.HasValue);
    }

    private static IEnumerable<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
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

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        return FindDescendants<T>(root).FirstOrDefault();
    }

    private static Color ApplySurfaceTint(Color color, Color surfaceTint, double elevation)
    {
        if (surfaceTint.A == 0 || elevation <= 0)
        {
            return color;
        }

        var opacity = ResolveSurfaceTintOpacityForElevation(elevation);
        if (opacity <= 0)
        {
            return color;
        }

        var overlay = Color.FromArgb(
            (byte)Math.Clamp((int)(opacity * 255), 0, 255),
            surfaceTint.R,
            surfaceTint.G,
            surfaceTint.B);

        return BlendColorOverlay(color, overlay);
    }

    private static double ResolveSurfaceTintOpacityForElevation(double elevation)
    {
        ReadOnlySpan<(double Elevation, double Opacity)> stops =
        [
            (0.0, 0.0),
            (1.0, 0.05),
            (3.0, 0.08),
            (6.0, 0.11),
            (8.0, 0.12),
            (12.0, 0.14)
        ];

        if (elevation <= stops[0].Elevation)
        {
            return stops[0].Opacity;
        }

        for (var i = 1; i < stops.Length; i++)
        {
            var current = stops[i];
            if (Math.Abs(elevation - current.Elevation) < 0.0001)
            {
                return current.Opacity;
            }

            if (elevation < current.Elevation)
            {
                var lower = stops[i - 1];
                var t = (elevation - lower.Elevation) / (current.Elevation - lower.Elevation);
                return lower.Opacity + (t * (current.Opacity - lower.Opacity));
            }
        }

        return stops[^1].Opacity;
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
}
