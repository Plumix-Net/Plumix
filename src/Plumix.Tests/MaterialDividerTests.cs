using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialDividerTests
{
    [Fact]
    public void Divider_Constructors_MatchFlutterNonNegativeGuards()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(height: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(thickness: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(endIndent: -0.1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalDivider(width: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalDivider(thickness: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalDivider(endIndent: -0.1));

        Assert.Equal(double.PositiveInfinity, new Divider(indent: double.PositiveInfinity).Indent);
        Assert.Equal(double.PositiveInfinity, new VerticalDivider(indent: double.PositiveInfinity).Indent);
    }

    [Fact]
    public void DividerThemeData_DefaultCopyEqualityAndLerpMatchFlutter()
    {
        var empty = new DividerThemeData();
        Assert.Null(empty.Color);
        Assert.Null(empty.Space);
        Assert.Null(empty.Thickness);
        Assert.Null(empty.Indent);
        Assert.Null(empty.EndIndent);
        Assert.Null(empty.Radius);
        Assert.Equal(empty, empty.CopyWith());
        Assert.Equal(empty.GetHashCode(), empty.CopyWith().GetHashCode());

        var source = new DividerThemeData(
            Color: Colors.Orange,
            Space: 5.0,
            Thickness: 4.0,
            Indent: 3.0,
            EndIndent: 2.0,
            Radius: BorderRadius.Only(1.0, 2.0, 4.0, 3.0));
        DividerThemeData copy = source.CopyWith(color: Colors.Purple, thickness: 8.0);
        Assert.Equal(Colors.Purple, copy.Color);
        Assert.Equal(5.0, copy.Space);
        Assert.Equal(8.0, copy.Thickness);
        Assert.Equal(source.Radius, copy.Radius);

        DividerThemeData midpoint = DividerThemeData.Lerp(source, copy, 0.5);
        Assert.Equal(6.0, midpoint.Thickness);
        Assert.Equal(source.Radius, midpoint.Radius);
    }

    [Fact]
    public void DividerTheme_IsInheritedThemeAndWrapsItsData()
    {
        var data = new DividerThemeData(Color: Colors.Teal);
        var theme = new DividerTheme(data, new SizedBox());
        var child = new Text("wrapped");

        Assert.IsAssignableFrom<InheritedTheme>(theme);
        var wrapped = Assert.IsType<DividerTheme>(theme.Wrap(default, child));
        Assert.Same(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);
    }

    [Fact]
    public void Divider_CreateBorderSide_HandlesNullContext()
    {
        BorderSide defaults = Divider.CreateBorderSide(null);
        BorderSide explicitSide = Divider.CreateBorderSide(null, Colors.Orange, 5.0);

        Assert.Equal(Colors.Black, defaults.Color);
        Assert.Equal(0.0, defaults.Width);
        Assert.Equal(Colors.Orange, explicitSide.Color);
        Assert.Equal(5.0, explicitSide.Width);
    }

    [Fact]
    public void Divider_DefaultM3_UsesDirectOutlineVariantTokens()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(outlineVariant: Colors.CadetBlue),
            DividerColor = Colors.Crimson,
        };
        using var harness = new WidgetRenderHarness(
            Root(
                theme,
                new SizedBox(
                    width: 200,
                    height: 48,
                    child: new Divider())));

        harness.Pump(new Size(220, 80));

        RenderDecoratedBox line = FindDividerBox(harness.RenderView, Axis.Horizontal);
        BorderSide side = ((Plumix.Rendering.Border)line.Decoration.Border!).Bottom;
        Assert.Equal(Colors.CadetBlue, side.Color);
        Assert.Equal(1.0, side.Width, 3);
        Assert.Equal(200.0, line.Size.Width, 3);
        Assert.Equal(1.0, line.Size.Height, 3);
        Assert.NotNull(FindConstrainedBox(
            harness.RenderView,
            constraints => Math.Abs(constraints.MinHeight - 16.0) < 0.001
                           && Math.Abs(constraints.MaxHeight - 16.0) < 0.001));
    }

    [Fact]
    public void Divider_DefaultM2_UsesDividerColorAndHairlineWidth()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            DividerColor = Colors.CadetBlue,
        };
        using var harness = new WidgetRenderHarness(
            Root(
                theme,
                new SizedBox(
                    width: 200,
                    height: 48,
                    child: new Divider())));

        harness.Pump(new Size(220, 80));

        RenderDecoratedBox line = FindDividerBox(harness.RenderView, Axis.Horizontal);
        BorderSide side = ((Plumix.Rendering.Border)line.Decoration.Border!).Bottom;
        Assert.Equal(Colors.CadetBlue, side.Color);
        Assert.Equal(0.0, side.Width, 3);
        Assert.Equal(0.0, line.Size.Height, 3);
    }

    [Fact]
    public void VerticalDivider_DefaultsSplitBetweenMaterial3AndMaterial2()
    {
        var m3Theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(outlineVariant: Colors.DarkCyan),
        };
        var m2Theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            DividerColor = Colors.DarkGoldenrod,
        };
        using var m3Harness = new WidgetRenderHarness(
            Root(m3Theme, new SizedBox(width: 48, height: 100, child: new VerticalDivider())));
        using var m2Harness = new WidgetRenderHarness(
            Root(m2Theme, new SizedBox(width: 48, height: 100, child: new VerticalDivider())));

        m3Harness.Pump(new Size(80, 120));
        m2Harness.Pump(new Size(80, 120));

        RenderDecoratedBox m3Line = FindDividerBox(m3Harness.RenderView, Axis.Vertical);
        RenderDecoratedBox m2Line = FindDividerBox(m2Harness.RenderView, Axis.Vertical);
        BorderSide m3Side = ((Plumix.Rendering.Border)m3Line.Decoration.Border!).Left;
        BorderSide m2Side = ((Plumix.Rendering.Border)m2Line.Decoration.Border!).Left;
        Assert.Equal(Colors.DarkCyan, m3Side.Color);
        Assert.Equal(1.0, m3Side.Width);
        Assert.Equal(1.0, m3Line.Size.Width);
        Assert.Equal(Colors.DarkGoldenrod, m2Side.Color);
        Assert.Equal(0.0, m2Side.Width);
        Assert.Equal(0.0, m2Line.Size.Width);
    }

    [Fact]
    public void Divider_ResolvesWidgetLocalThemeAndGlobalThemePrecedence()
    {
        var rootTheme = ThemeData.Light with
        {
            DividerTheme = new DividerThemeData(
                Color: Colors.DarkGreen,
                Space: 30,
                Thickness: 3,
                Indent: 11,
                EndIndent: 13),
        };
        using var themeHarness = new WidgetRenderHarness(
            Root(
                rootTheme,
                new SizedBox(
                    width: 220,
                    height: 64,
                    child: new Divider())));

        themeHarness.Pump(new Size(260, 90));

        RenderDecoratedBox themedLine = FindDividerBox(themeHarness.RenderView, Axis.Horizontal);
        BorderSide themedSide = ((Plumix.Rendering.Border)themedLine.Decoration.Border!).Bottom;
        Assert.Equal(Colors.DarkGreen, themedSide.Color);
        Assert.Equal(3.0, themedSide.Width, 3);
        Assert.Equal(196.0, themedLine.Size.Width, 3);

        var localTheme = new DividerThemeData(
            Color: Colors.Orange,
            Space: 32,
            Thickness: 4,
            Indent: 10,
            EndIndent: 12);
        var widgetRadius = BorderRadius.Only(1.0, 2.0, 4.0, 3.0);
        using var widgetHarness = new WidgetRenderHarness(
            Root(
                rootTheme,
                new SizedBox(
                    width: 220,
                    height: 64,
                    child: new DividerTheme(
                        localTheme,
                        new Divider(
                            height: 36,
                            thickness: 5,
                            indent: 7,
                            endIndent: 9,
                            color: Colors.Crimson,
                            radius: widgetRadius)))));

        widgetHarness.Pump(new Size(260, 90));

        RenderDecoratedBox widgetLine = FindDividerBox(widgetHarness.RenderView, Axis.Horizontal);
        BorderSide widgetSide = ((Plumix.Rendering.Border)widgetLine.Decoration.Border!).Bottom;
        Assert.Equal(Colors.Crimson, widgetSide.Color);
        Assert.Equal(5.0, widgetSide.Width, 3);
        Assert.Equal(204.0, widgetLine.Size.Width, 3);
        Assert.Equal(widgetRadius, widgetLine.Decoration.BorderRadius);
        Assert.NotNull(FindConstrainedBox(
            widgetHarness.RenderView,
            constraints => Math.Abs(constraints.MinHeight - 36.0) < 0.001
                           && Math.Abs(constraints.MaxHeight - 36.0) < 0.001));
    }

    [Fact]
    public void VerticalDivider_UsesThemeSpaceThicknessIndentsAndRadius()
    {
        var theme = ThemeData.Light with
        {
            DividerTheme = new DividerThemeData(
                Color: Colors.Purple,
                Space: 24,
                Thickness: 2,
                Indent: 6,
                EndIndent: 10,
                Radius: BorderRadius.Only(1.0, 2.0, 4.0, 3.0)),
        };
        using var harness = new WidgetRenderHarness(
            Root(
                theme,
                new SizedBox(
                    width: 80,
                    height: 120,
                    child: new VerticalDivider())));

        harness.Pump(new Size(120, 160));

        RenderDecoratedBox line = FindDividerBox(harness.RenderView, Axis.Vertical);
        BorderSide side = ((Plumix.Rendering.Border)line.Decoration.Border!).Left;
        Assert.Equal(Colors.Purple, side.Color);
        Assert.Equal(2.0, side.Width, 3);
        Assert.Equal(2.0, line.Size.Width, 3);
        Assert.Equal(104.0, line.Size.Height, 3);
        Assert.Equal(
            theme.DividerTheme.Radius!.Value.Resolve(TextDirection.Ltr),
            line.Decoration.BorderRadius);
        Assert.NotNull(FindConstrainedBox(
            harness.RenderView,
            constraints => Math.Abs(constraints.MinWidth - 24.0) < 0.001
                           && Math.Abs(constraints.MaxWidth - 24.0) < 0.001));
    }

    [Fact]
    public void Divider_ResolvesDirectionalIndentsInRtl()
    {
        using var harness = new WidgetRenderHarness(
            Root(
                ThemeData.Light,
                new SizedBox(
                    width: 100,
                    height: 30,
                    child: new Divider(
                        thickness: 2,
                        indent: 10,
                        endIndent: 20,
                        radius: BorderRadiusDirectional.Only(
                            topStart: 1,
                            topEnd: 4,
                            bottomEnd: 2,
                            bottomStart: 6))),
                TextDirection.Rtl));

        harness.Pump(new Size(120, 50));

        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(20.0, 0.0, 10.0, 0.0));
        RenderDecoratedBox line = FindDividerBox(harness.RenderView, Axis.Horizontal);
        Assert.Equal(70.0, line.Size.Width, 3);
        Assert.Equal(BorderRadius.Only(4, 1, 6, 2), line.Decoration.BorderRadius);
    }

    [Fact]
    public void Divider_AndVerticalDivider_DoNotCrashAtZeroArea()
    {
        using var horizontal = new WidgetRenderHarness(
            Root(ThemeData.Light, new SizedBox(width: 0.0, height: 0.0, child: new Divider())));
        using var vertical = new WidgetRenderHarness(
            Root(ThemeData.Light, new SizedBox(width: 0.0, height: 0.0, child: new VerticalDivider())));

        horizontal.Pump(new Size(100, 100));
        vertical.Pump(new Size(100, 100));

        Assert.Equal(default, horizontal.RenderView.Child!.Size);
        Assert.Equal(default, vertical.RenderView.Child!.Size);
    }

    private static Widget Root(
        ThemeData theme,
        Widget child,
        TextDirection textDirection = TextDirection.Ltr)
    {
        return new Directionality(
            textDirection,
            new Theme(data: theme, child: child));
    }

    private static RenderDecoratedBox FindDividerBox(RenderObject? root, Axis axis)
    {
        return Assert.Single(
            FindDescendants<RenderDecoratedBox>(root),
            box => axis == Axis.Horizontal
                ? box.Decoration.Border is Plumix.Rendering.Border { Bottom.Style: BorderStyle.Solid }
                : box.Decoration.Border is Plumix.Rendering.Border { Left.Style: BorderStyle.Solid });
    }

    private static RenderConstrainedBox? FindConstrainedBox(
        RenderObject? root,
        Func<BoxConstraints, bool> predicate)
    {
        return FindDescendants<RenderConstrainedBox>(root)
            .FirstOrDefault(box => predicate(box.AdditionalConstraints));
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
