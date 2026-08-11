using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialSurface = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDrawerTests
{
    [Fact]
    public void DrawerTheme_CopyLerpAndWrap_AreSourceShaped()
    {
        var startShape = new ShapeBorder(BorderRadius.Only(topRight: 4.0));
        var endShape = new ShapeBorder(BorderRadius.Only(topLeft: 8.0));
        var data = new DrawerThemeData(
            BackgroundColor: Colors.Red,
            ScrimColor: Colors.Green,
            Elevation: 2.0,
            ShadowColor: Colors.Blue,
            Width: 240.0,
            SurfaceTintColor: Colors.Gold,
            Shape: startShape,
            EndShape: endShape,
            ClipBehavior: Clip.HardEdge);

        DrawerThemeData copied = data.CopyWith(elevation: 6.0, width: 280.0);

        Assert.Equal(Colors.Red, copied.BackgroundColor);
        Assert.Equal(Colors.Green, copied.ScrimColor);
        Assert.Equal(6.0, copied.Elevation);
        Assert.Equal(Colors.Blue, copied.ShadowColor);
        Assert.Equal(280.0, copied.Width);
        Assert.Equal(Colors.Gold, copied.SurfaceTintColor);
        Assert.Same(startShape, copied.Shape);
        Assert.Same(endShape, copied.EndShape);
        Assert.Equal(Clip.HardEdge, copied.ClipBehavior);

        Assert.Null(DrawerThemeData.Lerp(null, null, 0.5));
        Assert.Same(data, DrawerThemeData.Lerp(data, data, 0.5));
        DrawerThemeData midpoint = DrawerThemeData.Lerp(
            new DrawerThemeData(
                Elevation: 2.0,
                Width: 200.0,
                Shape: ShapeBorder.RoundedRectangle(4.0),
                ClipBehavior: Clip.None),
            new DrawerThemeData(
                Elevation: 6.0,
                Width: 280.0,
                Shape: ShapeBorder.RoundedRectangle(12.0),
                ClipBehavior: Clip.AntiAlias),
            0.5)!;
        Assert.Equal(4.0, midpoint.Elevation);
        Assert.Equal(240.0, midpoint.Width);
        Assert.Equal(8.0, midpoint.Shape!.BorderRadius.Radius);
        Assert.Equal(Clip.AntiAlias, midpoint.ClipBehavior);

        var child = new Text("wrapped");
        var drawerTheme = new DrawerTheme(data, new SizedBox());
        Assert.IsAssignableFrom<InheritedTheme>(drawerTheme);
        var wrapped = Assert.IsType<DrawerTheme>(drawerTheme.Wrap(default, child));
        Assert.Same(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);
    }

    [Fact]
    public void Drawer_M2AndM3Defaults_MatchFlutterTokens()
    {
        var m2Theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ShadowColor = Colors.Purple,
        };
        using (var m2Harness = new WidgetRenderHarness(Root(m2Theme, new Drawer())))
        {
            MaterialSurface material = Assert.Single(m2Harness.FindWidgets<MaterialSurface>());
            var constrained = Assert.Single(m2Harness.FindWidgets<ConstrainedBox>());

            Assert.Null(material.Color);
            Assert.Equal(16.0, material.Elevation);
            Assert.Equal(Colors.Purple, material.ShadowColor);
            Assert.Null(material.SurfaceTintColor);
            Assert.Null(material.Shape);
            Assert.Equal(Clip.None, material.ClipBehavior);
            Assert.Equal(304.0, constrained.Constraints.MinWidth);
            Assert.Equal(304.0, constrained.Constraints.MaxWidth);
        }

        Color directSurface = Color.Parse("#FF123456");
        var m3Theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(surfaceContainerLow: directSurface),
            SurfaceContainerLowColor = Colors.Red,
        };
        using var m3Harness = new WidgetRenderHarness(Root(m3Theme, new Drawer()));
        MaterialSurface m3Material = Assert.Single(m3Harness.FindWidgets<MaterialSurface>());

        Assert.Equal(directSurface, m3Material.Color);
        Assert.Equal(1.0, m3Material.Elevation);
        Assert.Equal(Colors.Transparent, m3Material.ShadowColor);
        Assert.Equal(Colors.Transparent, m3Material.SurfaceTintColor);
        Assert.Equal(BorderRadius.Only(topRight: 16.0, bottomRight: 16.0), m3Material.Shape!.BorderRadius);
        Assert.Equal(Clip.HardEdge, m3Material.ClipBehavior);
    }

    [Theory]
    [InlineData(TextDirection.Ltr, DrawerAlignment.Start, false)]
    [InlineData(TextDirection.Ltr, DrawerAlignment.End, true)]
    [InlineData(TextDirection.Rtl, DrawerAlignment.Start, true)]
    [InlineData(TextDirection.Rtl, DrawerAlignment.End, false)]
    public void Drawer_M3DefaultShape_ResolvesAlignmentAndDirection(
        TextDirection direction,
        DrawerAlignment alignment,
        bool roundsLeft)
    {
        Widget drawer = new DrawerController(
            alignment: alignment,
            isDrawerOpen: true,
            child: new Drawer());
        using var harness = new WidgetRenderHarness(Root(ThemeData.Light, drawer, direction));

        MaterialSurface material = Assert.Single(harness.FindWidgets<MaterialSurface>());
        BorderRadius expected = roundsLeft
            ? BorderRadius.Only(topLeft: 16.0, bottomLeft: 16.0)
            : BorderRadius.Only(topRight: 16.0, bottomRight: 16.0);

        Assert.Equal(expected, material.Shape!.BorderRadius);
        Assert.Equal(Clip.HardEdge, material.ClipBehavior);
    }

    [Fact]
    public void Drawer_WidgetLocalAndGlobalThemes_FollowSourcePrecedence()
    {
        var globalData = new DrawerThemeData(
            BackgroundColor: Colors.Red,
            Elevation: 2.0,
            Width: 220.0);
        var localData = new DrawerThemeData(
            BackgroundColor: Colors.Green,
            Elevation: 4.0,
            Width: 240.0);
        var theme = ThemeData.Light with { DrawerTheme = globalData };
        using (var localHarness = new WidgetRenderHarness(Root(
                   theme,
                   new DrawerTheme(localData, new Drawer()))))
        {
            MaterialSurface material = Assert.Single(localHarness.FindWidgets<MaterialSurface>());
            var constrained = Assert.Single(localHarness.FindWidgets<ConstrainedBox>());
            Assert.Equal(Colors.Green, material.Color);
            Assert.Equal(4.0, material.Elevation);
            Assert.Equal(240.0, constrained.Constraints.MinWidth);
        }

        using var widgetHarness = new WidgetRenderHarness(Root(
            theme,
            new DrawerTheme(
                localData,
                new Drawer(
                    backgroundColor: Colors.Blue,
                    elevation: 8.0,
                    width: 280.0))));
        MaterialSurface widgetMaterial = Assert.Single(widgetHarness.FindWidgets<MaterialSurface>());
        var widgetConstrained = Assert.Single(widgetHarness.FindWidgets<ConstrainedBox>());
        Assert.Equal(Colors.Blue, widgetMaterial.Color);
        Assert.Equal(8.0, widgetMaterial.Elevation);
        Assert.Equal(280.0, widgetConstrained.Constraints.MinWidth);
    }

    [Fact]
    public void Drawer_SemanticLabel_UsesHostPlatformNotThemePlatform()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
            var androidTheme = ThemeData.Light with { Platform = TargetPlatform.IOS };
            using (var androidHarness = new WidgetRenderHarness(Root(androidTheme, new Drawer())))
            {
                Semantics semantics = Assert.Single(androidHarness.FindWidgets<Semantics>());
                Assert.Equal(DefaultMaterialLocalizations.Instance.DrawerLabel, semantics.Label);
                Assert.True(semantics.ScopesRoute);
                Assert.True(semantics.NamesRoute);
                Assert.True(semantics.ExplicitChildNodes);
            }

            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
            var iosTheme = ThemeData.Light with { Platform = TargetPlatform.Android };
            using var iosHarness = new WidgetRenderHarness(Root(iosTheme, new Drawer()));
            Assert.Null(Assert.Single(iosHarness.FindWidgets<Semantics>()).Label);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void Drawer_AllowsZeroArea()
    {
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new SizedBox(width: 0.0, height: 0.0, child: new Drawer())));

        harness.Pump(new Size(320.0, 240.0));

        RenderConstrainedBox drawerBox = Assert.Single(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 304.0);
        Assert.Equal(new Size(0.0, 0.0), drawerBox.Size);
    }

    private static Widget Root(
        ThemeData theme,
        Widget child,
        TextDirection direction = TextDirection.Ltr)
    {
        return new Directionality(
            textDirection: direction,
            child: new Theme(data: theme, child: child));
    }

    private static IReadOnlyList<T> FindDescendants<T>(RenderObject root) where T : RenderObject
    {
        var matches = new List<T>();
        CollectRenderObjects(root, matches);
        return matches;
    }

    private static void CollectRenderObjects<T>(RenderObject root, ICollection<T> matches) where T : RenderObject
    {
        if (root is T typed)
        {
            matches.Add(typed);
        }

        root.VisitChildren(child => CollectRenderObjects(child, matches));
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
            var widgets = new List<T>();
            CollectWidgets(_rootElement, widgets);
            return widgets;
        }

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

        private static void CollectWidgets<T>(Element element, ICollection<T> widgets) where T : Widget
        {
            if (element.Widget is T typed)
            {
                widgets.Add(typed);
            }

            element.VisitChildren(child => CollectWidgets(child, widgets));
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
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot is not null || child is not RenderBox renderBox)
                {
                    throw new InvalidOperationException("Harness root only accepts a RenderBox at the null slot.");
                }

                _renderView.Child = renderBox;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
                if (!Equals(oldSlot, newSlot))
                {
                    throw new InvalidOperationException("Harness root does not support slot moves.");
                }
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot is not null)
                {
                    throw new InvalidOperationException("Harness root expects a null slot.");
                }

                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
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
}
