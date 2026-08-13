using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class TextWidgetTests
{
    [Fact]
    public void TextWidget_CreatesAndUpdatesRenderParagraph_WithTextLayoutOptions()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Text(
                "alpha",
                fontSize: 16,
                color: Colors.Red,
                fontWeight: FontWeight.Bold,
                fontStyle: FontStyle.Italic,
                height: 1.4,
                letterSpacing: 1.5,
                textAlign: TextAlign.Center,
                softWrap: false,
                maxLines: 1,
                overflow: TextOverflow.Ellipsis,
                textDirection: TextDirection.Rtl));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.Equal("alpha", paragraph.PlainText);
        Assert.Equal(16, paragraph.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
        Assert.Equal(FontStyle.Italic, paragraph.FontStyle);
        Assert.Equal(1.4, paragraph.Height);
        Assert.Equal(1.5, paragraph.LetterSpacing);
        Assert.Equal(TextAlign.Center, paragraph.TextAlign);
        Assert.False(paragraph.SoftWrap);
        Assert.Equal(1, paragraph.MaxLines);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
        Assert.Equal(TextDirection.Rtl, paragraph.TextDirection);

        root.Update(new Text(
            "beta",
            fontSize: 12,
            color: Colors.Blue,
            fontWeight: FontWeight.Normal,
            fontStyle: FontStyle.Normal,
            height: 1.1,
            letterSpacing: 0.25,
            textAlign: TextAlign.End,
            softWrap: true,
            maxLines: 3,
            overflow: TextOverflow.Clip,
            textDirection: TextDirection.Ltr));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.Same(paragraph, updated);
        Assert.Equal("beta", updated.PlainText);
        Assert.Equal(12, updated.FontSize);
        Assert.Equal(FontWeight.Normal, updated.FontWeight);
        Assert.Equal(FontStyle.Normal, updated.FontStyle);
        Assert.Equal(1.1, updated.Height);
        Assert.Equal(0.25, updated.LetterSpacing);
        Assert.Equal(TextAlign.End, updated.TextAlign);
        Assert.True(updated.SoftWrap);
        Assert.Equal(3, updated.MaxLines);
        Assert.Equal(TextOverflow.Clip, updated.Overflow);
        Assert.Equal(TextDirection.Ltr, updated.TextDirection);
    }

    [Fact]
    public void RenderParagraph_UnboundedLayout_DoesNotClampWidthToArbitraryConstant()
    {
        var paragraph = new RenderParagraph(new string('W', 400))
        {
            FontSize = 14,
            SoftWrap = true
        };

        paragraph.Layout(new BoxConstraints(
            MinWidth: 0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: 0,
            MaxHeight: double.PositiveInfinity));

        Assert.True(
            paragraph.Size.Width > 1000,
            $"Expected unbounded text width above 1000, got {paragraph.Size.Width:0.##}.");
    }

    [Fact]
    public void RenderParagraph_ReportsARealTextBaseline()
    {
        var paragraph = new RenderParagraph("Baseline")
        {
            FontSize = 20
        };

        paragraph.Layout(new BoxConstraints(MaxWidth: 200, MaxHeight: 100));

        double? baseline = paragraph.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true);
        Assert.NotNull(baseline);
        Assert.InRange(baseline.Value, 0.01, paragraph.Size.Height);
    }

    [Fact]
    public void TextWidget_InheritsAndOverrides_DefaultTextStyle()
    {
        var owner = new BuildOwner();
        var style1 = new TextStyle(
            FontFamily: new FontFamily("Arial"),
            FontSize: 15,
            Color: Colors.DarkSlateBlue,
            FontWeight: FontWeight.SemiBold,
            FontStyle: FontStyle.Normal,
            Height: 1.4,
            LetterSpacing: 0.4);

        var root = new TestRootElement(
            new DefaultTextStyle(
                style: style1,
                textAlign: TextAlign.Center,
                softWrap: false,
                overflow: TextOverflow.Ellipsis,
                maxLines: 2,
                textWidthBasis: TextWidthBasis.LongestLine,
                textHeightBehavior: new TextHeightBehavior(false, false),
                child: new Text("alpha")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.Equal(style1.FontFamily, paragraph.FontFamily);
        Assert.Equal(15, paragraph.FontSize);
        Assert.Equal(FontWeight.SemiBold, paragraph.FontWeight);
        Assert.Equal(FontStyle.Normal, paragraph.FontStyle);
        Assert.Equal(1.4, paragraph.Height);
        Assert.Equal(0.4, paragraph.LetterSpacing);
        Assert.Equal(style1.Color, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
        Assert.Equal(TextAlign.Center, paragraph.TextAlign);
        Assert.False(paragraph.SoftWrap);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
        Assert.Equal(2, paragraph.MaxLines);
        Assert.Equal(TextWidthBasis.LongestLine, paragraph.TextWidthBasis);
        Assert.Equal(new TextHeightBehavior(false, false), paragraph.TextHeightBehavior);

        var style2 = new TextStyle(
            FontFamily: new FontFamily("Times New Roman"),
            FontSize: 18,
            Color: Colors.DarkGreen,
            FontWeight: FontWeight.Bold,
            FontStyle: FontStyle.Italic,
            Height: 1.6,
            LetterSpacing: 1.2);

        root.Update(
            new DefaultTextStyle(
                style: style2,
                child: new Text(
                    "alpha",
                    color: Colors.Blue,
                    letterSpacing: 0,
                    textAlign: TextAlign.Right,
                    softWrap: true,
                    maxLines: 4,
                    overflow: TextOverflow.Fade,
                    textWidthBasis: TextWidthBasis.Parent,
                    textHeightBehavior: new TextHeightBehavior(true, false))));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.Same(paragraph, updated);
        Assert.Equal(style2.FontFamily, updated.FontFamily);
        Assert.Equal(18, updated.FontSize);
        Assert.Equal(FontWeight.Bold, updated.FontWeight);
        Assert.Equal(FontStyle.Italic, updated.FontStyle);
        Assert.Equal(1.6, updated.Height);
        Assert.Equal(0, updated.LetterSpacing);
        Assert.Equal(Colors.Blue, Assert.IsType<SolidColorBrush>(updated.Foreground).Color);
        Assert.Equal(TextAlign.Right, updated.TextAlign);
        Assert.True(updated.SoftWrap);
        Assert.Equal(TextOverflow.Fade, updated.Overflow);
        Assert.Equal(4, updated.MaxLines);
        Assert.Equal(TextWidthBasis.Parent, updated.TextWidthBasis);
        Assert.Equal(new TextHeightBehavior(true, false), updated.TextHeightBehavior);
    }

    [Fact]
    public void TextWidget_InheritsMaterialTheme_BodyMediumStyle()
    {
        var owner = new BuildOwner();
        var themedStyle = new TextStyle(
            FontFamily: new FontFamily("Arial"),
            FontSize: 17,
            Color: Colors.OrangeRed,
            FontWeight: FontWeight.SemiBold,
            FontStyle: FontStyle.Italic,
            Height: 1.5,
            LetterSpacing: 0.6);

        var theme = ThemeData.Light with
        {
            TextTheme = new MaterialTextTheme(bodyMedium: themedStyle)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Text("alpha")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.Equal(themedStyle.FontFamily, paragraph.FontFamily);
        Assert.Equal(17, paragraph.FontSize);
        Assert.Equal(FontWeight.SemiBold, paragraph.FontWeight);
        Assert.Equal(FontStyle.Italic, paragraph.FontStyle);
        Assert.Equal(1.5, paragraph.Height);
        Assert.Equal(0.6, paragraph.LetterSpacing);
        Assert.Equal(themedStyle.Color, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
    }

    [Fact]
    public void TextWidget_PreservesTheExactAmbientTextScaler()
    {
        var owner = new BuildOwner();
        var scaler = new SquareTextScaler();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(TextScaler: scaler),
            child: new Text("scaled", fontSize: 12)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(root.ChildElement!.RenderObject);
        Assert.NotNull(paragraph);
        Assert.Same(scaler, paragraph!.TextScaler);
        Assert.Equal(144, paragraph.TextScaler.Scale(12));
    }

    [Fact]
    public void TextAndRichText_KeepLegacyScaleFactorCompatibility()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Text("scaled", fontSize: 12, textScaleFactor: 1.5));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.Equal(TextScaler.Linear(1.5), paragraph.TextScaler);

        var richText = new RichText(new TextSpan(text: "rich"), textScaleFactor: 2.0);
        Assert.Equal(TextScaler.Linear(2.0), richText.TextScaler);
        Assert.Equal(2.0, richText.TextScaleFactor);
        Assert.Throws<ArgumentException>(() => new Text(
            "invalid",
            textScaleFactor: 2.0,
            textScaler: TextScaler.NoScaling));
        Assert.Throws<ArgumentException>(() => new RichText(
            new TextSpan(text: "invalid"),
            textScaler: TextScaler.Linear(1.5),
            textScaleFactor: 2.0));
    }

    [Fact]
    public void IconWidget_UsesIconThemeDefaults_WhenArgumentsAreOmitted()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new IconTheme(
                data: new IconThemeData(Color: Colors.DarkOrange, Size: 28),
                child: new Icon(icon: Plumix.Material.Icons.Add)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(root.ChildElement!.RenderObject);
        Assert.NotNull(paragraph);
        Assert.Equal(char.ConvertFromUtf32(0xe047), paragraph!.PlainText);
        Assert.Equal(28, paragraph.FontSize);
        Assert.Equal(Colors.DarkOrange, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
        Assert.Equal(
            new FontFamily("avares://Plumix.Material/Assets/Fonts/MaterialIcons-Regular.otf#Material Icons"),
            paragraph.FontFamily);
    }

    [Fact]
    public void IconWidget_ExplicitColorAndSize_OverrideIconTheme()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new IconTheme(
                data: new IconThemeData(Color: Colors.DarkOrange, Size: 28),
                child: new Icon(
                    icon: Plumix.Material.Icons.Add,
                    size: 32,
                    color: Colors.MediumPurple)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(root.ChildElement!.RenderObject);
        Assert.NotNull(paragraph);
        Assert.Equal(32, paragraph!.FontSize);
        Assert.Equal(Colors.MediumPurple, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
    }

    [Fact]
    public void IconWidget_IconThemeOpacityAppliesToExplicitColor()
    {
        var owner = new BuildOwner();
        var explicitColor = Color.Parse("#FF663399");
        var root = new TestRootElement(
            new IconTheme(
                data: new IconThemeData(Color: Colors.DarkOrange, Size: 28, Opacity: 0.5),
                child: new Icon(
                    icon: Plumix.Material.Icons.Add,
                    color: explicitColor)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(root.ChildElement!.RenderObject);
        Assert.NotNull(paragraph);
        Assert.Equal(
            Color.FromArgb(128, explicitColor.R, explicitColor.G, explicitColor.B),
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void IconWidget_NullIcon_RendersSquareByResolvedSize()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(new Icon(icon: null, size: 18));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(root.ChildElement!.RenderObject);
        Assert.NotNull(constrainedBox);
        Assert.Equal(18, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(18, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(18, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(18, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void IconWidget_MatchTextDirection_Rtl_MirrorsGlyphWithTransform()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Icon(
                    icon: Plumix.Material.Icons.Add with { MatchTextDirection = true },
                    size: 24)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var transform = FindDescendant<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.NotNull(transform);
        Assert.Equal(Matrix.CreateTranslation(24, 0) * new Matrix(-1, 0, 0, 1, 0, 0), transform!.Transform);
    }

    [Fact]
    public void MaterialArrowBackIcon_UsesPlumixCodePointAndMatchTextDirection()
    {
        var icon = Plumix.Material.Icons.ArrowBack;

        Assert.Equal(0xe092, icon.CodePoint);
        Assert.True(icon.MatchTextDirection);
        Assert.Equal(
            "avares://Plumix.Material/Assets/Fonts/MaterialIcons-Regular.otf#Material Icons",
            icon.FontFamily);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
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

    private sealed record SquareTextScaler : TextScaler
    {
        public override double Scale(double fontSize) => fontSize * fontSize;

        public override double TextScaleFactor => 1.0;
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
