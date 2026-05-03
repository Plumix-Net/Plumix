using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialDividerTests
{
    [Fact]
    public void Divider_Constructors_Throw_OnInvalidNumericValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(height: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(thickness: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(indent: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(endIndent: -0.1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalDivider(width: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalDivider(thickness: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalDivider(indent: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalDivider(endIndent: -0.1));
    }

    [Fact]
    public void Divider_DefaultM3_UsesOutlineVariantSpaceAndThickness()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 200,
                    height: 48,
                    child: new Divider())));

        harness.Pump(new Size(220, 80));

        var line = FindDescendantByTypeName(harness.RenderView, "RenderDividerLine");
        Assert.NotNull(line);

        Assert.Equal(Axis.Horizontal, ReadProperty<Axis>(line!, "Axis"));
        Assert.Equal(theme.OutlineVariantColor, ReadProperty<Color>(line!, "Color"));
        Assert.Equal(1.0, ReadProperty<double>(line!, "Thickness"), 3);

        var lineBox = Assert.IsAssignableFrom<RenderBox>(line);
        Assert.Equal(1.0, lineBox.Size.Height, 3);

        var space = FindConstrainedBox(
            harness.RenderView,
            constraints => Math.Abs(constraints.MinHeight - 16.0) < 0.001
                           && Math.Abs(constraints.MaxHeight - 16.0) < 0.001);
        Assert.NotNull(space);
    }

    [Fact]
    public void Divider_DefaultM2_UsesDividerColorAndZeroLogicalThickness()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            DividerColor = Colors.CadetBlue
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 200,
                    height: 48,
                    child: new Divider())));

        harness.Pump(new Size(220, 80));

        var line = FindDescendantByTypeName(harness.RenderView, "RenderDividerLine");
        Assert.NotNull(line);

        Assert.Equal(Axis.Horizontal, ReadProperty<Axis>(line!, "Axis"));
        Assert.Equal(Colors.CadetBlue, ReadProperty<Color>(line!, "Color"));
        Assert.Equal(0.0, ReadProperty<double>(line!, "Thickness"), 3);

        var lineBox = Assert.IsAssignableFrom<RenderBox>(line);
        Assert.Equal(0.0, lineBox.Size.Height, 3);
    }

    [Fact]
    public void Divider_ResolvesPrecedence_WidgetOverThemeAndThemeOverDefaults()
    {
        var rootTheme = ThemeData.Light with
        {
            DividerTheme = new DividerThemeData(
                Color: Colors.DarkGreen,
                Space: 30,
                Thickness: 3,
                Indent: 11,
                EndIndent: 13)
        };

        using var themeHarness = new WidgetRenderHarness(
            new Theme(
                data: rootTheme,
                child: new SizedBox(
                    width: 220,
                    height: 64,
                    child: new Divider())));

        themeHarness.Pump(new Size(260, 90));

        var themedLine = FindDescendantByTypeName(themeHarness.RenderView, "RenderDividerLine");
        Assert.NotNull(themedLine);

        Assert.Equal(Colors.DarkGreen, ReadProperty<Color>(themedLine!, "Color"));
        Assert.Equal(3.0, ReadProperty<double>(themedLine!, "Thickness"), 3);
        Assert.Equal(11.0, ReadProperty<double>(themedLine!, "Indent"), 3);
        Assert.Equal(13.0, ReadProperty<double>(themedLine!, "EndIndent"), 3);

        var themedSpace = FindConstrainedBox(
            themeHarness.RenderView,
            constraints => Math.Abs(constraints.MinHeight - 30.0) < 0.001
                           && Math.Abs(constraints.MaxHeight - 30.0) < 0.001);
        Assert.NotNull(themedSpace);

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: rootTheme,
                child: new SizedBox(
                    width: 220,
                    height: 64,
                    child: new Divider(
                        height: 36,
                        thickness: 5,
                        indent: 7,
                        endIndent: 9,
                        color: Colors.Crimson))));

        widgetHarness.Pump(new Size(260, 90));

        var widgetLine = FindDescendantByTypeName(widgetHarness.RenderView, "RenderDividerLine");
        Assert.NotNull(widgetLine);

        Assert.Equal(Colors.Crimson, ReadProperty<Color>(widgetLine!, "Color"));
        Assert.Equal(5.0, ReadProperty<double>(widgetLine!, "Thickness"), 3);
        Assert.Equal(7.0, ReadProperty<double>(widgetLine!, "Indent"), 3);
        Assert.Equal(9.0, ReadProperty<double>(widgetLine!, "EndIndent"), 3);

        var widgetSpace = FindConstrainedBox(
            widgetHarness.RenderView,
            constraints => Math.Abs(constraints.MinHeight - 36.0) < 0.001
                           && Math.Abs(constraints.MaxHeight - 36.0) < 0.001);
        Assert.NotNull(widgetSpace);
    }

    [Fact]
    public void VerticalDivider_UsesThemeSpaceThicknessAndIndents()
    {
        var theme = ThemeData.Light with
        {
            DividerTheme = new DividerThemeData(
                Color: Colors.Purple,
                Space: 24,
                Thickness: 2,
                Indent: 6,
                EndIndent: 10)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 80,
                    height: 120,
                    child: new VerticalDivider())));

        harness.Pump(new Size(120, 160));

        var line = FindDescendantByTypeName(harness.RenderView, "RenderDividerLine");
        Assert.NotNull(line);

        Assert.Equal(Axis.Vertical, ReadProperty<Axis>(line!, "Axis"));
        Assert.Equal(Colors.Purple, ReadProperty<Color>(line!, "Color"));
        Assert.Equal(2.0, ReadProperty<double>(line!, "Thickness"), 3);
        Assert.Equal(6.0, ReadProperty<double>(line!, "Indent"), 3);
        Assert.Equal(10.0, ReadProperty<double>(line!, "EndIndent"), 3);

        var lineBox = Assert.IsAssignableFrom<RenderBox>(line);
        Assert.Equal(2.0, lineBox.Size.Width, 3);

        var space = FindConstrainedBox(
            harness.RenderView,
            constraints => Math.Abs(constraints.MinWidth - 24.0) < 0.001
                           && Math.Abs(constraints.MaxWidth - 24.0) < 0.001);
        Assert.NotNull(space);
    }

    private static T ReadProperty<T>(RenderObject target, string propertyName)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        var value = property!.GetValue(target);
        Assert.NotNull(value);
        return (T)value!;
    }

    private static RenderObject? FindDescendantByTypeName(RenderObject? root, string typeName)
    {
        if (root is null)
        {
            return null;
        }

        if (root.GetType().Name == typeName)
        {
            return root;
        }

        RenderObject? match = null;
        root.VisitChildren(child =>
        {
            if (match is not null)
            {
                return;
            }

            match = FindDescendantByTypeName(child, typeName);
        });

        return match;
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
