using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialLegacyButtonTests
{
    public MaterialLegacyButtonTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void RawMaterialButton_Defaults_MatchFlutter()
    {
        var button = new RawMaterialButton(onPressed: null);

        Assert.False(button.Enabled);
        Assert.Equal(2, button.Elevation);
        Assert.Equal(4, button.FocusElevation);
        Assert.Equal(4, button.HoverElevation);
        Assert.Equal(8, button.HighlightElevation);
        Assert.Equal(0, button.DisabledElevation);
        Assert.Equal(default, button.Padding);
        Assert.Equal(VisualDensity.Standard, button.VisualDensity);
        Assert.Equal(new BoxConstraints(MinWidth: 88, MinHeight: 36), button.Constraints);
        Assert.Equal(new RoundedRectangleBorder(), button.Shape);
        Assert.Equal(TimeSpan.FromMilliseconds(200), button.AnimationDuration);
        Assert.Equal(Clip.None, button.ClipBehavior);
        Assert.Equal(MaterialTapTargetSize.Padded, button.MaterialTapTargetSize);
        Assert.False(button.Autofocus);
        Assert.True(button.EnableFeedback);
    }

    [Fact]
    public void ButtonConstructors_RejectNegativeElevationsAndExtents()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RawMaterialButton(onPressed: null, elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RawMaterialButton(onPressed: null, highlightElevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MaterialButton(onPressed: null, disabledElevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MaterialButton(onPressed: null, minWidth: -1));
        Assert.Throws<ArgumentException>(() =>
            new RawMaterialButton(
                onPressed: null,
                constraints: new BoxConstraints(MinWidth: 10, MaxWidth: 5)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ButtonThemeData(MinWidth: -1));
    }

    [Fact]
    public void ButtonThemeData_PaddingGetterAndButtonTextThemeResolutionMatchSource()
    {
        var defaults = new ButtonThemeData();
        Assert.Equal(EdgeInsetsGeometry.Symmetric(horizontal: 16), defaults.Padding);
        var primary = new MaterialButton(
            onPressed: () => { },
            textTheme: ButtonTextTheme.Primary);
        Assert.Equal(EdgeInsetsGeometry.Symmetric(horizontal: 24), defaults.GetPadding(primary));

        var explicitPadding = new ButtonThemeData(
            Padding: EdgeInsetsGeometry.DirectionalOnly(start: 14, end: 6));
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 14, end: 6),
            explicitPadding.GetPadding(primary));
    }

    [Fact]
    public void MaterialButton_Defaults_AreResolvedThroughRawMaterialButton()
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new MaterialButton(
                onPressed: () => { },
                child: new Text("Legacy"))));

        var raw = tree.FindWidget<RawMaterialButton>();
        var tapTarget = tree.FindWidget<InputPadding>();

        Assert.NotNull(raw);
        Assert.True(raw!.Enabled);
        Assert.Equal(EdgeInsetsGeometry.Symmetric(horizontal: 16), raw.Padding);
        Assert.Equal(new BoxConstraints(MinWidth: 88, MinHeight: 36), raw.Constraints);
        Assert.Equal(2, ShapeBorderGeometry.ResolveRadius(raw.Shape).TopLeft);
        Assert.Equal(2, raw.Elevation);
        Assert.Equal(4, raw.FocusElevation);
        Assert.Equal(4, raw.HoverElevation);
        Assert.Equal(8, raw.HighlightElevation);
        Assert.Equal(0, raw.DisabledElevation);
        Assert.Equal(Color.FromArgb(0xDE, 0, 0, 0), raw.TextStyle?.Color);
        Assert.Equal(ThemeData.Light.HighlightColor, raw.HighlightColor);
        Assert.Equal(ThemeData.Light.SplashColor, raw.SplashColor);
        Assert.NotNull(tapTarget);
        Assert.Equal(new Size(48, 48), tapTarget!.MinSize);
    }

    [Fact]
    public void MaterialButton_WidgetAndButtonThemePrecedence_MatchesFlutter()
    {
        var localTheme = new ButtonThemeData(
            TextTheme: ButtonTextTheme.Accent,
            MinWidth: 104,
            Height: 44,
            Padding: new Thickness(19, 3),
            Shape: BorderRadius.Circular(9),
            FocusColor: Colors.DarkOrange,
            HoverColor: Colors.DarkCyan);

        using var tree = Build(new Theme(
            data: ThemeData.Light with
            {
                MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap,
            },
            child: new ButtonTheme(
                data: localTheme,
                child: new MaterialButton(
                    onPressed: () => { },
                    minWidth: 120,
                    height: 52,
                    textColor: Colors.Crimson,
                    hoverColor: Colors.HotPink,
                    child: new Text("Overrides")))));

        var raw = Assert.IsType<RawMaterialButton>(tree.FindWidget<RawMaterialButton>());
        Assert.Equal(120, raw.Constraints.MinWidth);
        Assert.Equal(52, raw.Constraints.MinHeight);
        Assert.Equal(EdgeInsetsGeometry.Symmetric(horizontal: 19, vertical: 3), raw.Padding);
        Assert.Equal(9, ShapeBorderGeometry.ResolveRadius(raw.Shape).TopLeft);
        Assert.Equal(Colors.Crimson, raw.TextStyle?.Color);
        Assert.Equal(Colors.DarkOrange, raw.FocusColor);
        Assert.Equal(Colors.HotPink, raw.HoverColor);
        Assert.Equal(MaterialTapTargetSize.ShrinkWrap, raw.MaterialTapTargetSize);
    }

    [Theory]
    [InlineData(TextDirection.Ltr, 14, 6)]
    [InlineData(TextDirection.Rtl, 6, 14)]
    public void MaterialButton_DirectionalThemePaddingResolvesAtTheFinalPaddingLayer(
        TextDirection textDirection,
        double expectedLeft,
        double expectedRight)
    {
        using var tree = Build(new Directionality(
            textDirection,
            new Theme(
                data: ThemeData.Light,
                child: new ButtonTheme(
                    data: new ButtonThemeData(
                        Padding: EdgeInsetsGeometry.DirectionalOnly(start: 14, end: 6)),
                    child: new MaterialButton(
                        onPressed: () => { },
                        child: new Text("Directional"))))));

        var raw = Assert.IsType<RawMaterialButton>(tree.FindWidget<RawMaterialButton>());
        Assert.Equal(EdgeInsetsGeometry.DirectionalOnly(start: 14, end: 6), raw.Padding);
        Assert.Equal(
            new Thickness(expectedLeft, 0, expectedRight, 0),
            FindButtonContentPadding(tree.RenderRoot).Resolve(textDirection));
    }

    [Fact]
    public void MaterialButton_DisabledWidgetColors_TakePrecedence()
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new MaterialButton(
                onPressed: null,
                disabledTextColor: Colors.SlateBlue,
                disabledColor: Colors.Beige,
                child: new Text("Disabled"))));

        var raw = Assert.IsType<RawMaterialButton>(tree.FindWidget<RawMaterialButton>());
        Assert.False(raw.Enabled);
        Assert.Equal(Colors.SlateBlue, raw.TextStyle?.Color);
        Assert.Equal(Colors.Beige, raw.FillColor);
        Assert.Equal(0, raw.DisabledElevation);
    }

    [Fact]
    public void MaterialButton_TextColorAlsoWinsForDisabledButtonLikeFlutterWidgetStateColorPath()
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new MaterialButton(
                onPressed: null,
                textColor: Colors.Crimson,
                disabledTextColor: Colors.SlateBlue,
                child: new Text("Disabled"))));

        var raw = Assert.IsType<RawMaterialButton>(tree.FindWidget<RawMaterialButton>());
        Assert.Equal(Colors.Crimson, raw.TextStyle?.Color);
    }

    [Fact]
    public void RawMaterialButton_ResolvesStateElevationOverlayAndDensity()
    {
        var textStyle = new TextStyle(Color: Colors.Navy, FontSize: 15);
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new RawMaterialButton(
                onPressed: () => { },
                textStyle: textStyle,
                fillColor: Colors.Beige,
                focusColor: Colors.Gold,
                hoverColor: Colors.Green,
                highlightColor: Colors.Crimson,
                splashColor: Colors.Blue,
                padding: new Thickness(10, 8),
                visualDensity: VisualDensity.Compact,
                constraints: new BoxConstraints(MinWidth: 88, MinHeight: 36),
                shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(7)),
                child: new Text("Raw"))));

        var material = Assert.IsType<Plumix.Material.Material>(tree.FindWidget<Plumix.Material.Material>());
        var ink = Assert.IsType<InkWell>(tree.FindWidget<InkWell>());
        var constrained = Assert.IsType<ConstrainedBox>(tree.FindWidget<ConstrainedBox>());
        var tapTarget = Assert.IsType<InputPadding>(tree.FindWidget<InputPadding>());

        Assert.Equal(2, material.Elevation);
        Assert.Equal(Colors.Beige, material.Color);
        Assert.Equal(MaterialType.Button, material.Type);
        Assert.Equal(Colors.Navy, material.TextStyle?.Color);
        Assert.Equal(7, ShapeBorderGeometry.ResolveRadius(material.Shape).TopLeft);
        Assert.Equal(Colors.Gold, ink.FocusColor);
        Assert.Equal(Colors.Green, ink.HoverColor);
        Assert.Equal(Colors.Crimson, ink.HighlightColor);
        Assert.Equal(Colors.Blue, ink.SplashColor);
        Assert.Same(material.Shape, ink.CustomBorder);
        Assert.Equal(new Thickness(2, 0), FindButtonContentPadding(tree.RenderRoot));
        Assert.Equal(80, constrained.Constraints.MinWidth);
        Assert.Equal(28, constrained.Constraints.MinHeight);
        Assert.Equal(new Size(40, 40), tapTarget.MinSize);
    }

    [Theory]
    [InlineData(true, 8.0)]
    [InlineData(false, 2.0)]
    public void RawMaterialButton_EffectiveElevationFollowsThePressedState(bool pressed, double expected)
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new RawMaterialButton(
                onPressed: () => { },
                fillColor: Colors.Beige,
                child: new Text("Press"))));

        if (pressed)
        {
            PressAndHold(tree, pointer: 91, DateTime.UtcNow);
        }
        else
        {
            tree.Pump(HarnessSize);
        }

        var material = Assert.IsType<Plumix.Material.Material>(tree.FindWidget<Plumix.Material.Material>());
        Assert.Equal(expected, material.Elevation);
    }

    [Fact]
    public void RawMaterialButton_DisabledUsesDisabledElevationAndTransparentMaterial()
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new RawMaterialButton(
                onPressed: null,
                disabledElevation: 3,
                child: new Text("Off"))));

        var material = Assert.IsType<Plumix.Material.Material>(tree.FindWidget<Plumix.Material.Material>());
        var ink = Assert.IsType<InkWell>(tree.FindWidget<InkWell>());
        Assert.Equal(3, material.Elevation);
        Assert.Equal(MaterialType.Transparency, material.Type);
        Assert.Null(material.Color);
        Assert.False(ink.CanRequestFocus);
        Assert.Null(ink.OnTap);
    }

    [Fact]
    public void RawMaterialButton_ShrinkWrapTapTargetRemovesTheInputPadding()
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new RawMaterialButton(
                onPressed: () => { },
                materialTapTargetSize: MaterialTapTargetSize.ShrinkWrap,
                child: new Text("Tight"))));

        var tapTarget = Assert.IsType<InputPadding>(tree.FindWidget<InputPadding>());
        Assert.Equal(default, tapTarget.MinSize);
    }

    [Fact]
    public void RawMaterialButton_LongPressOnly_RemainsEnabledAndInteractive()
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new RawMaterialButton(
                onPressed: null,
                onLongPress: () => { },
                child: new Text("Hold"))));

        var raw = Assert.IsType<RawMaterialButton>(tree.FindWidget<RawMaterialButton>());
        var ink = Assert.IsType<InkWell>(tree.FindWidget<InkWell>());
        Assert.True(raw.Enabled);
        Assert.True(ink.CanRequestFocus);
        Assert.Null(ink.OnTap);
        Assert.NotNull(ink.OnLongPress);
        Assert.NotNull(FindInteractiveGestureListener(tree.RenderRoot));
    }

    [Fact]
    public void RawMaterialButton_ReportsHighlightTransitions()
    {
        var highlights = new List<bool>();
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new RawMaterialButton(
                onPressed: () => { },
                onHighlightChanged: highlights.Add,
                highlightColor: Colors.DarkOrange,
                child: new Text("Press"))));

        DateTime now = DateTime.UtcNow;
        PressAndHold(tree, pointer: 71, now);

        var pressedMaterial = Assert.IsType<Plumix.Material.Material>(tree.FindWidget<Plumix.Material.Material>());
        var pressedInk = Assert.IsType<InkWell>(tree.FindWidget<InkWell>());
        Assert.Equal(Colors.DarkOrange, pressedInk.HighlightColor);
        Assert.Equal(8, pressedMaterial.Elevation);

        Release(tree, pointer: 71, now.AddMilliseconds(20));

        Assert.Equal([true, false], highlights);
        // The mouse is still over the button after the release, so Dart's precedence order lands on
        // `hoverElevation`, not on the resting `elevation`.
        var releasedMaterial = Assert.IsType<Plumix.Material.Material>(tree.FindWidget<Plumix.Material.Material>());
        Assert.Equal(4, releasedMaterial.Elevation);
    }

    /// The resolved insets of the `Padding` Dart's `RawMaterialButton` puts between its `InkWell`
    /// and its child — the deepest `RenderPadding` under the button's material.
    private static EdgeInsetsGeometry FindButtonContentPadding(RenderObject? root)
    {
        RenderPadding? deepest = null;
        Collect(root);
        Assert.NotNull(deepest);
        return deepest!.Padding;

        void Collect(RenderObject? node)
        {
            if (node is null)
            {
                return;
            }

            if (node is RenderPadding padding)
            {
                deepest = padding;
            }

            node.VisitChildren(Collect);
        }
    }

    private static readonly Size HarnessSize = new(200, 100);

    private static void PressAndHold(WidgetTree tree, int pointer, DateTime now)
    {
        tree.Pump(HarnessSize);
        GestureBinding.Instance.HandlePointerEvent(
            tree.RenderView,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                new Point(60, 40),
                PointerButtons.Primary,
                now));
        tree.Pump(HarnessSize);
    }

    private static void Release(WidgetTree tree, int pointer, DateTime now)
    {
        GestureBinding.Instance.HandlePointerEvent(
            tree.RenderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                new Point(60, 40),
                PointerButtons.None,
                now));
        tree.Pump(HarnessSize);
    }

    private static WidgetTree Build(Widget widget) => new(widget);

    /// The pointer surface Dart's `InkWell` installs through its `GestureDetector`.
    private static RenderPointerListener? FindInteractiveGestureListener(RenderObject? root)
    {
        if (root is null) return null;
        if (root is RenderPointerListener listener && listener.OnPointerDown is not null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child => result ??= FindInteractiveGestureListener(child));
        return result;
    }

    private sealed class WidgetTree : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetTree(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public RenderObject? RenderRoot => _root.Child?.RenderObject;

        public T? FindWidget<T>() where T : Widget => FindWidget<T>(_root.Child);

        public void FlushBuild() => _owner.FlushBuild();

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _root.Unmount();

        private static T? FindWidget<T>(Element? element) where T : Widget
        {
            if (element is null) return null;
            if (element.Widget is T match) return match;

            T? result = null;
            element.VisitChildren(child => result ??= FindWidget<T>(child));
            return result;
        }
    }

    private sealed class RootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;

        public RootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;

        public Element? Child { get; private set; }
        public override RenderObject? RenderObject => Child?.RenderObject;
        internal override Element? RenderObjectAttachingChild => Child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            Child = UpdateChild(Child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (Child is not null) visitor(Child);
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(Child, child)) Child = null;
        }

        internal override void Unmount()
        {
            if (Child is not null)
            {
                UnmountChild(Child);
                Child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot) =>
            _renderView.Child = (RenderBox)child;

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child))
            {
                _renderView.Child = null;
            }
        }
    }
}
