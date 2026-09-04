using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>Mirrors Flutter's `material_ui/test/text_selection_theme_test.dart`.</summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialTextSelectionThemeTests : IDisposable
{
    public MaterialTextSelectionThemeTests() => FocusManager.Instance.ResetForTests();

    public void Dispose() => FocusManager.Instance.ResetForTests();

    [DebugOnlyFact]
    public void TextSelectionThemeData_DebugFillProperties_MatchesFlutter()
    {
        // Flutter's `text_selection_theme_test.dart`: three `ColorProperty` entries, and a default
        // instance elides all of them.
        var defaults = new DiagnosticPropertiesBuilder();
        new TextSelectionThemeData().DebugFillProperties(defaults);
        Assert.Equal(
            ["cursorColor", "selectionColor", "selectionHandleColor"],
            defaults.Properties.Select(property => property.Name).ToList());
        Assert.Empty(defaults.Properties.Where(property => property.Value is not null));

        var filled = new DiagnosticPropertiesBuilder();
        new TextSelectionThemeData(
            CursorColor: Color.FromUInt32(0xffeeffaa),
            SelectionColor: Color.FromUInt32(0x88888888),
            SelectionHandleColor: Color.FromUInt32(0xaabbccdd)).DebugFillProperties(filled);
        Assert.All(filled.Properties, property => Assert.IsType<ColorProperty>(property));
        Assert.Equal(
            "Color(alpha: 1.0000, red: 0.9333, green: 1.0000, blue: 0.6667, colorSpace: ColorSpace.sRGB)",
            ((ColorProperty)filled.Properties[0]).ValueToString());
    }

    [Fact]
    public void TextSelectionThemeData_CopyWithEqualsAndHashCodeBasics()
    {
        var data = new TextSelectionThemeData();

        Assert.Equal(data, data.CopyWith());
        Assert.Equal(data.GetHashCode(), data.CopyWith().GetHashCode());

        var filled = new TextSelectionThemeData(
            CursorColor: Color.FromUInt32(0xffeeffaa),
            SelectionColor: Color.FromUInt32(0x88888888),
            SelectionHandleColor: Color.FromUInt32(0xaabbccdd));

        Assert.Equal(filled, filled.CopyWith());
        Assert.Equal(
            new TextSelectionThemeData(
                CursorColor: Colors.Red,
                SelectionColor: filled.SelectionColor,
                SelectionHandleColor: filled.SelectionHandleColor),
            filled.CopyWith(cursorColor: Colors.Red));
    }

    [Fact]
    public void TextSelectionThemeData_LerpSpecialCases()
    {
        Assert.Null(TextSelectionThemeData.Lerp(null, null, 0));

        var data = new TextSelectionThemeData();
        Assert.Same(data, TextSelectionThemeData.Lerp(data, data, 0.5));

        var begin = new TextSelectionThemeData(CursorColor: Color.FromRgb(0, 0, 0));
        var end = new TextSelectionThemeData(CursorColor: Color.FromRgb(0, 0, 100));
        Assert.Equal(Color.FromRgb(0, 0, 50), TextSelectionThemeData.Lerp(begin, end, 0.5)!.CursorColor);
    }

    [Fact]
    public void TextSelectionThemeData_NullFieldsByDefault()
    {
        var data = new TextSelectionThemeData();

        Assert.Null(data.CursorColor);
        Assert.Null(data.SelectionColor);
        Assert.Null(data.SelectionHandleColor);
    }

    [Fact]
    public void Material2_EmptyTextSelectionThemeUsesDefaults()
    {
        var theme = new ThemeData(useMaterial3: false);

        // The values Flutter's own test hard-codes: `Colors.blue[500]` from the M2
        // `ColorScheme.fromSwatch` default, and the same color at 40% opacity.
        Color defaultCursorColor = Color.FromUInt32(0xFF2196F3);
        Color defaultSelectionColor = Color.FromUInt32(0x662196F3);
        Assert.Equal(defaultCursorColor, theme.ColorScheme.Primary);

        (Color cursorColor, Color selectionColor) = ResolveFieldColors(theme);
        Assert.Equal(defaultCursorColor, cursorColor);
        Assert.Equal(defaultSelectionColor, selectionColor);
        Assert.Equal(defaultCursorColor, ResolveHandleColor(theme));
    }

    [Fact]
    public void Material3_EmptyTextSelectionThemeUsesDefaults()
    {
        var theme = new ThemeData();
        Color primary = theme.ColorScheme.Primary;

        (Color cursorColor, Color selectionColor) = ResolveFieldColors(theme);
        Assert.Equal(primary, cursorColor);
        Assert.Equal(WithOpacity(primary, 0.40), selectionColor);
        Assert.Equal(primary, ResolveHandleColor(theme));
    }

    [Fact]
    public void ThemeDataTextSelectionTheme_IsUsedWhenProvided()
    {
        var selectionTheme = new TextSelectionThemeData(
            CursorColor: Color.FromUInt32(0xffaabbcc),
            SelectionColor: Color.FromUInt32(0x88888888),
            SelectionHandleColor: Color.FromUInt32(0x00ccbbaa));
        ThemeData theme = ThemeData.Light with { TextSelectionTheme = selectionTheme };

        (Color cursorColor, Color selectionColor) = ResolveFieldColors(theme);
        Assert.Equal(selectionTheme.CursorColor, cursorColor);
        Assert.Equal(selectionTheme.SelectionColor, selectionColor);
        Assert.Equal(selectionTheme.SelectionHandleColor, ResolveHandleColor(theme));
    }

    [Fact]
    public void TextSelectionThemeWidget_OverridesThemeData()
    {
        ThemeData theme = ThemeData.Light with
        {
            TextSelectionTheme = new TextSelectionThemeData(
                CursorColor: Color.FromUInt32(0xffaabbcc),
                SelectionColor: Color.FromUInt32(0x88888888),
                SelectionHandleColor: Color.FromUInt32(0x00ccbbaa)),
        };
        var widgetTheme = new TextSelectionThemeData(
            CursorColor: Color.FromUInt32(0xffddeeff),
            SelectionColor: Color.FromUInt32(0x44444444),
            SelectionHandleColor: Color.FromUInt32(0x00ffeedd));

        (Color cursorColor, Color selectionColor) = ResolveFieldColors(
            theme,
            field => new TextSelectionTheme(widgetTheme, field));
        Assert.Equal(widgetTheme.CursorColor, cursorColor);
        Assert.Equal(widgetTheme.SelectionColor, selectionColor);
        Assert.Equal(
            widgetTheme.SelectionHandleColor,
            ResolveHandleColor(theme, handle => new TextSelectionTheme(widgetTheme, handle)));
    }

    [Fact]
    public void TextFieldAndSelectableTextParameters_OverrideThemeSettings()
    {
        ThemeData theme = ThemeData.Light with
        {
            TextSelectionTheme = new TextSelectionThemeData(
                CursorColor: Color.FromUInt32(0xffaabbcc),
                SelectionHandleColor: Color.FromUInt32(0x00ccbbaa)),
        };
        var widgetTheme = new TextSelectionThemeData(
            CursorColor: Color.FromUInt32(0xffddeeff),
            SelectionHandleColor: Color.FromUInt32(0x00ffeedd));
        Color cursorColor = Color.FromUInt32(0x88888888);

        RenderEditable field = RenderField(
            theme,
            new TextSelectionTheme(widgetTheme, new TextField(cursorColor: cursorColor)));
        Assert.Equal(cursorColor, field.CursorColor);

        RenderEditable selectable = RenderField(
            theme,
            new TextSelectionTheme(
                widgetTheme,
                new SelectableText("foobar", cursorColor: cursorColor, showCursor: true)));
        Assert.Equal(cursorColor, selectable.CursorColor);
    }

    [Fact]
    public void TextSelectionTheme_OverridesDefaultSelectionStyleForDescendants()
    {
        Color themeSelectionColor = Color.FromUInt32(0xffaabbcc);
        Color themeCursorColor = Color.FromUInt32(0x00ccbbaa);
        Color defaultSelectionColor = Color.FromUInt32(0xffaa1111);
        Color defaultCursorColor = Color.FromUInt32(0x00cc2222);
        DefaultSelectionStyle? aboveStyle = null;
        DefaultSelectionStyle? belowStyle = null;

        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new DefaultSelectionStyle(
                selectionColor: defaultSelectionColor,
                cursorColor: defaultCursorColor,
                child: new Builder(context =>
                {
                    aboveStyle = DefaultSelectionStyle.Of(context);
                    return new TextSelectionTheme(
                        new TextSelectionThemeData(
                            CursorColor: themeCursorColor,
                            SelectionColor: themeSelectionColor),
                        new Builder(inner =>
                        {
                            belowStyle = DefaultSelectionStyle.Of(inner);
                            return new SizedBox();
                        }));
                }))));
        harness.Pump(new Size(320, 120));

        Assert.Equal(defaultSelectionColor, aboveStyle!.SelectionColor);
        Assert.Equal(defaultCursorColor, aboveStyle.CursorColor);
        Assert.Equal(themeSelectionColor, belowStyle!.SelectionColor);
        Assert.Equal(themeCursorColor, belowStyle.CursorColor);
    }

    [Fact]
    public void TextField_CursorUsesTheErrorColorWhileTheFieldIsInError()
    {
        var theme = new ThemeData();

        RenderEditable errored = RenderField(
            theme,
            new TextField(decoration: new InputDecoration(errorText: "nope")));
        Assert.Equal(theme.ColorScheme.Error, errored.CursorColor);

        Color cursorErrorColor = Color.FromUInt32(0xff00ff00);
        RenderEditable overridden = RenderField(
            theme,
            new TextField(
                cursorColor: Colors.Blue,
                cursorErrorColor: cursorErrorColor,
                decoration: new InputDecoration(errorText: "nope")));
        Assert.Equal(cursorErrorColor, overridden.CursorColor);

        // An intrinsic (over `maxLength`) error drives the same branch.
        var controller = new TextEditingController("abcd");
        RenderEditable intrinsic = RenderField(
            theme,
            new TextField(controller: controller, maxLength: 2));
        Assert.Equal(theme.ColorScheme.Error, intrinsic.CursorColor);

        RenderEditable valid = RenderField(theme, new TextField(cursorColor: Colors.Blue));
        Assert.Equal(Colors.Blue, valid.CursorColor);
    }

    [Fact]
    public void TextField_ResolvesTheCupertinoPrimaryColorOnApplePlatforms()
    {
        ThemeData theme = ThemeData.Light with { Platform = TargetPlatform.IOS };

        // Without a `CupertinoTheme` override Dart's `MaterialBasedCupertinoThemeData` defers to the
        // Material color scheme, so the resolved color is the same as on the other platforms.
        (Color cursorColor, Color selectionColor) = ResolveFieldColors(theme);
        Assert.Equal(theme.ColorScheme.Primary, cursorColor);
        Assert.Equal(WithOpacity(theme.ColorScheme.Primary, 0.40), selectionColor);

        Color cupertinoPrimary = Color.FromUInt32(0xff00aa77);
        RenderEditable overridden = RenderField(
            theme,
            new CupertinoTheme(
                new CupertinoThemeData(primaryColor: cupertinoPrimary),
                new TextField()));
        Assert.Equal(cupertinoPrimary, overridden.CursorColor);
        Assert.Equal(WithOpacity(cupertinoPrimary, 0.40), overridden.SelectionColor);
    }

    private static (Color CursorColor, Color SelectionColor) ResolveFieldColors(
        ThemeData theme,
        Func<Widget, Widget>? wrap = null)
    {
        Widget field = new TextField();
        RenderEditable editable = RenderField(theme, wrap is null ? field : wrap(field));
        return (editable.CursorColor, editable.SelectionColor);
    }

    private static RenderEditable RenderField(ThemeData theme, Widget field)
    {
        using var harness = new WidgetRenderHarness(Wrap(theme, field));
        harness.Pump(new Size(360, 160));
        return Assert.Single(FindDescendants<RenderEditable>(harness.RenderView));
    }

    private static Color ResolveHandleColor(ThemeData theme, Func<Widget, Widget>? wrap = null)
    {
        Widget handle = new Builder(context => MaterialTextSelectionControls.Instance.BuildHandle(
            context,
            TextSelectionHandleType.Right,
            10.0));
        using var harness = new WidgetRenderHarness(Wrap(theme, wrap is null ? handle : wrap(handle)));
        harness.Pump(new Size(60, 60));

        RenderCustomPaint paint = Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView));
        return Assert.IsType<TextSelectionHandlePainter>(paint.Painter).Color;
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb((byte)Math.Round(color.A * opacity), color.R, color.G, color.B);
    }

    private static Widget Wrap(ThemeData theme, Widget child)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(new MediaQueryData(Size: new Size(360, 640)), new Theme(theme, child)));
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T value) result.Add(value);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
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

        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public RootElement(RenderView view, Widget widget) : base(widget) => _view = view;

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
                if (ReferenceEquals(_child, child)) _child = null;
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null) visitor(_child);
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _view.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_view.Child, child)) _view.Child = null;
            }

            public override void Unmount()
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
