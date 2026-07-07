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
        Assert.Equal(BorderRadius.Zero, button.Shape);
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
    public void MaterialButton_Defaults_AreResolvedThroughRawMaterialButton()
    {
        using var tree = Build(new Theme(
            data: ThemeData.Light,
            child: new MaterialButton(
                onPressed: () => { },
                child: new Text("Legacy"))));

        var raw = tree.FindWidget<RawMaterialButton>();
        var core = tree.FindWidget<MaterialButtonCore>();

        Assert.NotNull(raw);
        Assert.True(raw!.Enabled);
        Assert.Equal(new Thickness(16, 0), raw.Padding);
        Assert.Equal(new BoxConstraints(MinWidth: 88, MinHeight: 36), raw.Constraints);
        Assert.Equal(2, raw.Shape.Radius);
        Assert.Equal(2, raw.Elevation);
        Assert.Equal(4, raw.FocusElevation);
        Assert.Equal(4, raw.HoverElevation);
        Assert.Equal(8, raw.HighlightElevation);
        Assert.Equal(0, raw.DisabledElevation);
        Assert.Equal(Color.FromArgb(0xDE, 0, 0, 0), raw.TextStyle?.Color);
        Assert.Equal(ThemeData.Light.HighlightColor, raw.HighlightColor);
        Assert.Equal(ThemeData.Light.SplashColor, raw.SplashColor);
        Assert.NotNull(core);
        Assert.Equal(new Size(48, 48), core!.TapTargetMinimumSize);
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
                SecondaryColor = Colors.Gold,
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
        Assert.Equal(new Thickness(19, 3), raw.Padding);
        Assert.Equal(9, raw.Shape.Radius);
        Assert.Equal(Colors.Crimson, raw.TextStyle?.Color);
        Assert.Equal(Colors.DarkOrange, raw.FocusColor);
        Assert.Equal(Colors.HotPink, raw.HoverColor);
        Assert.Equal(MaterialTapTargetSize.ShrinkWrap, raw.MaterialTapTargetSize);
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
                shape: BorderRadius.Circular(7),
                child: new Text("Raw"))));

        var core = Assert.IsType<MaterialButtonCore>(tree.FindWidget<MaterialButtonCore>());
        Assert.Equal(2, core.Style.ResolveElevation(MaterialState.None));
        Assert.Equal(4, core.Style.ResolveElevation(MaterialState.Focused));
        Assert.Equal(4, core.Style.ResolveElevation(MaterialState.Hovered));
        Assert.Equal(8, core.Style.ResolveElevation(MaterialState.Pressed));
        Assert.Equal(0, core.Style.ResolveElevation(MaterialState.Disabled));
        Assert.Equal(Colors.Gold, core.Style.ResolveOverlayColor(MaterialState.Focused));
        Assert.Equal(Colors.Green, core.Style.ResolveOverlayColor(MaterialState.Hovered));
        Assert.Equal(Colors.Crimson, core.Style.ResolveOverlayColor(MaterialState.Pressed));
        Assert.Equal(Colors.Blue, core.Style.ResolveSplashColor(MaterialState.Pressed));
        Assert.Equal(new Thickness(2, 0), core.Style.ResolvePadding(MaterialState.None));
        Assert.Equal(new Size(80, 28), EffectiveMinimum(core));
        Assert.Equal(new Size(40, 40), core.TapTargetMinimumSize);
        Assert.Equal(7, core.Style.ResolveShape(MaterialState.None)?.Radius);
        Assert.Equal(Colors.Navy, core.Style.ResolveForegroundColor(MaterialState.None));
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
        var core = Assert.IsType<MaterialButtonCore>(tree.FindWidget<MaterialButtonCore>());
        Assert.True(raw.Enabled);
        Assert.True(core.Enabled);
        Assert.Null(core.OnPressed);
        Assert.NotNull(core.OnLongPress);
        Assert.NotNull(FindInteractivePointerListener(tree.RenderRoot));
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

        var listener = Assert.IsType<RenderPointerListener>(
            FindInteractivePointerListener(tree.RenderRoot));
        var entry = new BoxHitTestEntry(listener, new Point(12, 10));
        var now = DateTime.UtcNow;
        listener.HandleEvent(new PointerDownEvent(
            pointer: 71,
            kind: PointerDeviceKind.Mouse,
            position: new Point(12, 10),
            buttons: PointerButtons.Primary,
            timestampUtc: now), entry);
        tree.FlushBuild();

        var core = Assert.IsType<MaterialButtonCore>(tree.FindWidget<MaterialButtonCore>());
        Assert.Equal(Colors.DarkOrange, core.Style.ResolveOverlayColor(MaterialState.Pressed));

        listener.HandleEvent(new PointerUpEvent(
            pointer: 71,
            kind: PointerDeviceKind.Mouse,
            position: new Point(12, 10),
            buttons: PointerButtons.None,
            timestampUtc: now.AddMilliseconds(20)), entry);
        tree.FlushBuild();

        Assert.Equal([true, false], highlights);
    }

    private static Size EffectiveMinimum(MaterialButtonCore core)
    {
        var minimum = core.Style.ResolveMinimumSize(MaterialState.None)!.Value;
        var adjustment = core.Style.VisualDensity!.Value.BaseSizeAdjustment;
        return new Size(
            Math.Max(0, minimum.Width + adjustment.X),
            Math.Max(0, minimum.Height + adjustment.Y));
    }

    private static WidgetTree Build(Widget widget) => new(widget);

    private static RenderPointerListener? FindInteractivePointerListener(RenderObject? root)
    {
        if (root is null) return null;
        if (root is RenderPointerListener listener
            && listener.OnPointerDown is not null
            && listener.OnPointerUp is not null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child => result ??= FindInteractivePointerListener(child));
        return result;
    }

    private sealed class WidgetTree : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;

        public WidgetTree(Widget widget)
        {
            _root = new RootElement(widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderObject? RenderRoot => _root.Child?.RenderObject;

        public T? FindWidget<T>() where T : Widget => FindWidget<T>(_root.Child);

        public void FlushBuild() => _owner.FlushBuild();

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
        public RootElement(Widget widget) : base(widget) { }

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

        public void InsertRenderObjectChild(RenderObject child, object? slot) { }
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
        public void RemoveRenderObjectChild(RenderObject child, object? slot) { }
    }
}
