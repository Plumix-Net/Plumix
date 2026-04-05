using System.Linq;
using Avalonia;
using Avalonia.Media;
using Flutter;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

public sealed class MaterialSwitchTests
{
    [Fact]
    public void Switch_DefaultM3_Selected_UsesPrimaryTrackAndOnPrimaryThumb()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.Coral,
            OnPrimaryColor = Colors.WhiteSmoke
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Switch(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var track = FindTrackDecoration(renderRoot);
        var thumb = FindThumbDecoration(renderRoot);

        Assert.NotNull(track);
        Assert.NotNull(thumb);
        Assert.Equal(Colors.Coral, track!.Decoration.Color);
        Assert.Equal(Colors.WhiteSmoke, thumb!.Decoration.Color);
    }

    [Fact]
    public void Switch_DefaultM3_Unselected_UsesSurfaceContainerHighestTrackAndOutlineThumb()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            SurfaceContainerHighestColor = Colors.PowderBlue,
            OutlineColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Switch(
                    value: false,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var track = FindTrackDecoration(renderRoot);
        var thumb = FindThumbDecoration(renderRoot);

        Assert.NotNull(track);
        Assert.NotNull(thumb);
        Assert.Equal(Colors.PowderBlue, track!.Decoration.Color);
        Assert.True(track.Decoration.Border.HasValue);
        Assert.Equal(Colors.CadetBlue, track.Decoration.Border!.Value.Color);
        Assert.Equal(2, track.Decoration.Border.Value.Width);
        Assert.Equal(Colors.CadetBlue, thumb!.Decoration.Color);
    }

    [Fact]
    public void Switch_DefaultM3_DisabledSelected_UsesOnSurfaceOpacityTrackColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.Brown
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Switch(
                    value: true,
                    onChanged: null)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var track = FindTrackDecoration(renderRoot);
        Assert.NotNull(track);
        Assert.Equal(ApplyOpacity(Colors.Brown, 0.12), track!.Decoration.Color);
    }

    [Fact]
    public void Switch_WidgetThumbColor_PrecedesActiveThumbColorAndThemeThumbColor()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    SwitchTheme = new SwitchThemeData(
                        ThumbColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
                },
                child: new Switch(
                    value: true,
                    onChanged: _ => { },
                    activeThumbColor: Colors.Orange,
                    thumbColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    {
                        return states.HasFlag(MaterialState.Selected)
                            ? Colors.ForestGreen
                            : Colors.SlateGray;
                    }))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var thumb = FindThumbDecoration(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(thumb);
        Assert.Equal(Colors.ForestGreen, thumb!.Decoration.Color);
    }

    [Fact]
    public void Switch_ThemeThumbColor_Applies_WhenWidgetThumbColorIsMissing()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    SwitchTheme = new SwitchThemeData(
                        ThumbColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
                },
                child: new Switch(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var thumb = FindThumbDecoration(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(thumb);
        Assert.Equal(Colors.MediumPurple, thumb!.Decoration.Color);
    }

    [Fact]
    public void Switch_WidgetTrackColor_PrecedesSwitchThemeTrackColor()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    SwitchTheme = new SwitchThemeData(
                        TrackColor: MaterialStateProperty<Color?>.All(Colors.PaleVioletRed))
                },
                child: new Switch(
                    value: true,
                    onChanged: _ => { },
                    trackColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    {
                        return states.HasFlag(MaterialState.Selected)
                            ? Colors.Teal
                            : Colors.Gainsboro;
                    }))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var track = FindTrackDecoration(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(track);
        Assert.Equal(Colors.Teal, track!.Decoration.Color);
    }

    [Fact]
    public void Switch_SwitchThemeTrackOutline_ResolvesColorAndWidth()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    SwitchTheme = new SwitchThemeData(
                        TrackOutlineColor: MaterialStateProperty<Color?>.All(Colors.Indigo),
                        TrackOutlineWidth: MaterialStateProperty<double?>.All(3))
                },
                child: new Switch(
                    value: false,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var track = FindTrackDecoration(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(track);
        Assert.True(track!.Decoration.Border.HasValue);
        Assert.Equal(Colors.Indigo, track.Decoration.Border!.Value.Color);
        Assert.Equal(3, track.Decoration.Border.Value.Width);
    }

    [Fact]
    public void Switch_ThumbIcon_SelectedState_RendersSelectedIcon()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Switch(
                    value: true,
                    onChanged: _ => { },
                    thumbIcon: MaterialStateProperty<Icon?>.ResolveWith(states =>
                    {
                        return states.HasFlag(MaterialState.Selected)
                            ? new Icon(Icons.Check, size: 14)
                            : new Icon(Icons.Close, size: 14);
                    }))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var iconGlyph = char.ConvertFromUtf32(Icons.Check.CodePoint);
        var iconParagraph = FindDescendants<RenderParagraph>(renderRoot)
            .FirstOrDefault(paragraph => paragraph.Text == iconGlyph);
        Assert.NotNull(iconParagraph);
    }

    [Fact]
    public void Switch_DefaultTapTarget_Padded_ExpandsHitArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 120,
                    child: new Switch(
                        value: false,
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.True(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void Switch_ThemeTapTarget_ShrinkWrap_DoesNotExpandHitArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap },
                child: new SizedBox(
                    width: 120,
                    child: new Switch(
                        value: false,
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void Switch_KeyboardActivation_TogglesFalseToTrue()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            var focusNode = new FocusNode();
            var nextValue = false;
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

            Assert.True(focusNode.RequestFocus());
            owner.FlushBuild();

            var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.True(nextValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
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
        return boxes.FirstOrDefault(box => box.Decoration.Border.HasValue)
               ?? boxes.FirstOrDefault(box =>
                   box.Decoration.Color.HasValue
                   && box.Decoration.Color.Value.A > 0);
    }

    private static RenderDecoratedBox? FindThumbDecoration(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .FirstOrDefault(box =>
                box.Decoration.Color.HasValue
                && box.Decoration.Color.Value.A > 0
                && !box.Decoration.Border.HasValue);
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        var alpha = (byte)Math.Clamp((int)(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
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
