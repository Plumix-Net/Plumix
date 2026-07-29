using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialSurface = Plumix.Material.Material;

namespace Plumix.Tests;

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
    public void Card_Material3DefaultsReadColorSchemeRolesDirectly()
    {
        Color elevated = Color.Parse("#FF102030");
        Color filled = Color.Parse("#FF203040");
        Color outlined = Color.Parse("#FF304050");
        Color outline = Color.Parse("#FF405060");
        Color shadow = Color.Parse("#FF506070");
        var scheme = ThemeData.Light.ColorScheme.CopyWith(
            surfaceContainerLow: elevated,
            surfaceContainerHighest: filled,
            surface: outlined,
            outlineVariant: outline,
            shadow: shadow);
        var theme = ThemeData.Light with
        {
            ColorScheme = scheme,
            SurfaceContainerLowColor = Colors.Red,
            SurfaceContainerHighestColor = Colors.Green,
            SurfaceColor = Colors.Blue,
            OutlineVariantColor = Colors.Yellow,
            ShadowColor = Colors.Purple,
        };

        using var elevatedHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(child: new SizedBox(width: 80, height: 32)), theme));
        elevatedHarness.Pump(new Size(220, 140));
        var elevatedSurface = FindMaterialDecoration(elevatedHarness.RenderView);
        Assert.NotNull(elevatedSurface);
        Assert.Equal(elevated, elevatedSurface!.Decoration.Color);
        AssertShadowUsesColor(elevatedSurface, shadow);

        using var filledHarness = new WidgetRenderHarness(
            BuildThemedCard(Card.Filled(child: new SizedBox(width: 80, height: 32)), theme));
        filledHarness.Pump(new Size(220, 140));
        Assert.Equal(filled, FindMaterialDecoration(filledHarness.RenderView)!.Decoration.Color);

        using var outlinedHarness = new WidgetRenderHarness(
            BuildThemedCard(Card.Outlined(child: new SizedBox(width: 80, height: 32)), theme));
        outlinedHarness.Pump(new Size(220, 140));
        var outlinedSurface = FindMaterialDecoration(outlinedHarness.RenderView);
        var outlinedBorder = FindDescendants<RenderDecoratedBox>(outlinedHarness.RenderView)
            .Single(box => box.Decoration.Border.HasValue);
        Assert.Equal(outlined, outlinedSurface!.Decoration.Color);
        Assert.Equal(outline, outlinedBorder.Decoration.Border!.Value.Color);
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
    public void CardTheme_RejectsDataCombinedWithLegacyProperties()
    {
        Assert.Throws<ArgumentException>(() => new CardTheme(
            data: new CardThemeData(),
            color: Colors.Red));
    }

    [Fact]
    public void CardThemeData_CopyAndLerpFollowFlutterContracts()
    {
        var a = new CardThemeData(
            ClipBehavior: Clip.HardEdge,
            Color: Colors.Red,
            ShadowColor: Colors.Black,
            SurfaceTintColor: Colors.Blue,
            Elevation: 2,
            Margin: new Thickness(2, 4, 6, 8),
            Shape: ShapeBorder.RoundedRectangle(4, new BorderSide(Colors.Red, 1)));
        CardThemeData copy = a.CopyWith(color: Colors.Green, elevation: 6);
        Assert.Equal(Colors.Green, copy.Color);
        Assert.Equal(6, copy.Elevation);
        Assert.Equal(a.Margin, copy.Margin);

        var b = new CardThemeData(
            ClipBehavior: Clip.AntiAlias,
            Color: Colors.Blue,
            ShadowColor: Colors.White,
            SurfaceTintColor: Colors.Green,
            Elevation: 10,
            Margin: new Thickness(10, 12, 14, 16),
            Shape: ShapeBorder.RoundedRectangle(12, new BorderSide(Colors.Blue, 3)));
        CardThemeData midpoint = Assert.IsType<CardThemeData>(CardThemeData.Lerp(a, b, 0.5));

        Assert.Equal(Clip.AntiAlias, midpoint.ClipBehavior);
        Assert.Equal(6, midpoint.Elevation);
        Assert.Equal(new Thickness(6, 8, 10, 12), midpoint.Margin);
        Assert.Equal(8, midpoint.Shape!.BorderRadius.Radius);
        Assert.Equal(2, midpoint.Shape.Side!.Value.Width);
        Assert.Equal(new ColorTween().Evaluate(0.5, Colors.Red, Colors.Blue), midpoint.Color);

        CardThemeData scaled = Assert.IsType<CardThemeData>(
            CardThemeData.Lerp(null, new CardThemeData(
                Elevation: 8,
                Shape: ShapeBorder.RoundedRectangle(12, new BorderSide(Colors.Blue, 2))), 0.25));
        Assert.Equal(2, scaled.Elevation);
        Assert.Equal(3, scaled.Shape!.BorderRadius.Radius);
        Assert.Equal(0.5, scaled.Shape.Side!.Value.Width);
        Assert.Equal(new CardThemeData(), CardThemeData.Lerp(null, null, 0.5));

        var themeA = new CardTheme(data: a);
        CardTheme copiedTheme = themeA.CopyWith(color: Colors.Green, elevation: 6);
        Assert.Equal(Colors.Green, copiedTheme.Color);
        Assert.Equal(6, copiedTheme.Elevation);
        CardTheme lerpedTheme = CardTheme.Lerp(themeA, new CardTheme(data: b), 0.5);
        Assert.Equal(midpoint, lerpedTheme.Data);
    }

    [Fact]
    public void ThemeDataLerp_InterpolatesCardTheme()
    {
        var a = ThemeData.Light with
        {
            CardTheme = new CardThemeData(
                Color: Colors.Red,
                Elevation: 2,
                Margin: new Thickness(2)),
        };
        var b = ThemeData.Dark with
        {
            CardTheme = new CardThemeData(
                Color: Colors.Blue,
                Elevation: 10,
                Margin: new Thickness(10)),
        };

        ThemeData midpoint = ThemeData.Lerp(a, b, 0.5);

        Assert.Equal(new ColorTween().Evaluate(0.5, Colors.Red, Colors.Blue), midpoint.CardTheme.Color);
        Assert.Equal(6, midpoint.CardTheme.Elevation);
        Assert.Equal(new Thickness(6), midpoint.CardTheme.Margin);
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
    public void Card_BorderOnForegroundControlsMaterialPaintOrder()
    {
        ShapeBorder shape = ShapeBorder.RoundedRectangle(9, new BorderSide(Colors.DarkGreen, 2));
        using var foregroundHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(
                shape: shape,
                borderOnForeground: true,
                child: new SizedBox(width: 80, height: 32))));
        foregroundHarness.Pump(new Size(220, 140));

        Assert.NotNull(FindDescendant<RenderStack>(foregroundHarness.RenderView));
        var foregroundBorders = FindDescendants<RenderDecoratedBox>(foregroundHarness.RenderView)
            .Where(box => box.Decoration.Border.HasValue)
            .ToArray();
        Assert.Single(foregroundBorders);

        using var backgroundHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(
                shape: shape,
                borderOnForeground: false,
                child: new SizedBox(width: 80, height: 32))));
        backgroundHarness.Pump(new Size(220, 140));

        Assert.Null(FindDescendant<RenderStack>(backgroundHarness.RenderView));
        var backgroundBorders = FindDescendants<RenderDecoratedBox>(backgroundHarness.RenderView)
            .Where(box => box.Decoration.Border.HasValue)
            .ToArray();
        Assert.Single(backgroundBorders);
    }

    [Fact]
    public void Card_AllowsNullChild()
    {
        using var harness = new WidgetRenderHarness(BuildThemedCard(new Card()));

        harness.Pump(new Size(220, 140));

        Assert.NotNull(FindMaterialDecoration(harness.RenderView));
    }

    [Fact]
    public void Material_ValidatesShapeCircleAndElevationArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaterialSurface(elevation: -0.1));
        Assert.Throws<ArgumentException>(() => new MaterialSurface(
            shape: ShapeBorder.RoundedRectangle(4),
            borderRadius: BorderRadius.Circular(4)));
        Assert.Throws<ArgumentException>(() => new MaterialSurface(
            type: MaterialType.Circle,
            borderRadius: BorderRadius.Circular(4)));
    }

    [Fact]
    public void Material_ResolvesCanvasAndCardDefaultsAndAppliesSurfaceTint()
    {
        var theme = ThemeData.Light with
        {
            CanvasColor = Color.Parse("#FFF7F2FA"),
            CardColor = Color.Parse("#FFEAF6FF"),
        };

        using var canvasHarness = new WidgetRenderHarness(
            BuildThemedCard(new MaterialSurface(child: new SizedBox(width: 80, height: 32)), theme));
        canvasHarness.Pump(new Size(220, 140));

        var canvas = FindMaterialDecoration(canvasHarness.RenderView);
        Assert.NotNull(canvas);
        Assert.Equal(theme.CanvasColor, canvas!.Decoration.Color);
        Assert.Equal(0, canvas.Decoration.EffectiveBorderRadius.Radius);

        using var cardHarness = new WidgetRenderHarness(
            BuildThemedCard(new MaterialSurface(
                type: MaterialType.Card,
                elevation: 3,
                surfaceTintColor: Colors.Red,
                child: new SizedBox(width: 80, height: 32)), theme));
        cardHarness.Pump(new Size(220, 140));

        var card = FindMaterialDecoration(cardHarness.RenderView);
        Assert.NotNull(card);
        Assert.Equal(2, card!.Decoration.EffectiveBorderRadius.Radius);
        Assert.True(card.Decoration.BoxShadows.HasValue);
        Assert.Equal(ApplySurfaceTint(theme.CardColor, Colors.Red, 3), card.Decoration.Color);
    }

    [Fact]
    public void Material_ClipsAndPaintsShapeBorderAtConfiguredPaintOrder()
    {
        var side = new BorderSide(Colors.DarkGreen, 2);
        using var foregroundHarness = new WidgetRenderHarness(
            BuildThemedCard(new MaterialSurface(
                borderOnForeground: true,
                clipBehavior: Clip.AntiAlias,
                shape: ShapeBorder.RoundedRectangle(9, side),
                child: new SizedBox(width: 80, height: 32))));
        foregroundHarness.Pump(new Size(220, 140));

        Assert.NotNull(FindDescendant<RenderClipRRect>(foregroundHarness.RenderView));
        Assert.Single(FindDescendants<RenderDecoratedBox>(foregroundHarness.RenderView)
            .Where(box => box.Decoration.Border.HasValue)
            .ToArray());
        Assert.NotNull(FindDescendant<RenderStack>(foregroundHarness.RenderView));

        using var backgroundHarness = new WidgetRenderHarness(
            BuildThemedCard(new MaterialSurface(
                borderOnForeground: false,
                shape: ShapeBorder.RoundedRectangle(9, side),
                child: new SizedBox(width: 80, height: 32))));
        backgroundHarness.Pump(new Size(220, 140));

        var backgroundBorders = FindDescendants<RenderDecoratedBox>(backgroundHarness.RenderView)
            .Where(box => box.Decoration.Border.HasValue)
            .ToArray();
        Assert.Single(backgroundBorders);
    }

    [Fact]
    public void MergeableMaterial_ComposesCardMaterialSurfaceForEachConnectedSliceGroup()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 180,
                    child: new MergeableMaterial(
                        elevation: 2,
                        hasDividers: true,
                        children:
                        [
                            new MaterialSlice(
                                new ValueKey<string>("first"),
                                new SizedBox(width: 120, height: 24),
                                color: Colors.LightBlue),
                            new MaterialGap(new ValueKey<string>("gap"), 12),
                            new MaterialSlice(
                                new ValueKey<string>("second"),
                                new SizedBox(width: 120, height: 24),
                                color: Colors.LightGreen),
                        ]))));
        harness.Pump(new Size(220, 140));

        var surfaces = FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Where(box => box.Decoration.BoxShadows.HasValue)
            .ToArray();
        Assert.Equal(2, surfaces.Length);
        Assert.All(surfaces, surface => Assert.Equal(2, surface.Decoration.EffectiveBorderRadius.Radius));
    }

    [Fact]
    public void Card_SemanticContainer_DefaultsToContainerAndCanBeDisabled()
    {
        using var defaultHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(child: new SizedBox(width: 80, height: 32))));
        defaultHarness.Pump(new Size(220, 140));

        var defaultSemantics = FindDescendants<RenderSemanticsAnnotations>(defaultHarness.RenderView).ToArray();
        Assert.Equal(2, defaultSemantics.Length);
        Assert.Contains(defaultSemantics, semantics => semantics.Container);
        Assert.Contains(defaultSemantics, semantics => semantics.ExplicitChildNodes == false);

        using var explicitHarness = new WidgetRenderHarness(
            BuildThemedCard(new Card(
                semanticContainer: false,
                child: new SizedBox(width: 80, height: 32))));
        explicitHarness.Pump(new Size(220, 140));

        var explicitSemantics = FindDescendants<RenderSemanticsAnnotations>(explicitHarness.RenderView).ToArray();
        Assert.Equal(2, explicitSemantics.Length);
        Assert.All(explicitSemantics, semantics => Assert.False(semantics.Container));
        Assert.Contains(explicitSemantics, semantics => semantics.ExplicitChildNodes);
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
        return ElevationOverlay.ApplySurfaceTint(color, surfaceTint, elevation);
    }

    private static void AssertShadowUsesColor(RenderDecoratedBox surface, Color shadowColor)
    {
        Assert.True(surface.Decoration.BoxShadows.HasValue);
        var shadows = surface.Decoration.BoxShadows!.Value;
        Assert.Equal(shadowColor.R, shadows[0].Color.R);
        Assert.Equal(shadowColor.G, shadows[0].Color.G);
        Assert.Equal(shadowColor.B, shadows[0].Color.B);
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
