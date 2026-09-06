using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDrawerHeaderTests
{
    [Fact]
    public void DrawerHeader_UsesStatusBarHeightPaddingAndBottomDivider()
    {
        var theme = ThemeData.Light with
        {
            TextTheme = new MaterialTextTheme(
                bodyLarge: MaterialTextTheme.DefaultBodyLarge.CopyWith(fontSize: 19)),
            DividerColor = Colors.Crimson,
            UseMaterial3 = false,
        };
        using var harness = new WidgetRenderHarness(Root(
            new DrawerHeader(child: new Text("Header")),
            theme,
            padding: new Thickness(0, 24, 0, 0)));

        harness.Pump(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(height: 185));
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(16, 40, 16, 8));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Border is Plumix.Rendering.Border sideBottom
                   && sideBottom.Bottom.Color == Colors.Crimson);
        Assert.Equal(19, FindParagraph(harness.RenderView, "Header")!.FontSize);
    }

    [Fact]
    public void DrawerHeader_CustomDecorationMarginAndAnimationContractAreWired()
    {
        Decoration decoration = new BoxDecoration(Color: Colors.DarkSlateBlue);
        var header = new DrawerHeader(
            child: new Text("Custom"),
            decoration: decoration,
            margin: new Thickness(3, 4, 5, 6),
            padding: new Thickness(7, 8, 9, 10),
            duration: TimeSpan.FromMilliseconds(400),
            curve: Curves.EaseOut);
        Assert.Equal(TimeSpan.FromMilliseconds(400), header.Duration);
        Assert.Equal(Curves.EaseOut(0.3), header.Curve(0.3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawerHeader(
            child: null,
            duration: TimeSpan.FromMilliseconds(-1)));

        using var harness = new WidgetRenderHarness(Root(header, ThemeData.Light));
        harness.Pump(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration == decoration);
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(3, 4, 5, 6));
    }

    [Fact]
    public void DrawerHeader_DefaultCurveUsesFlutterFastOutSlowInCubic()
    {
        var header = new DrawerHeader(child: null);

        Assert.Equal(EdgeInsetsGeometry.Only(bottom: 8.0), header.Margin);
        Assert.Equal(EdgeInsetsGeometry.FromLTRB(16.0, 16.0, 16.0, 8.0), header.Padding);
        Assert.Equal(TimeSpan.FromMilliseconds(250), header.Duration);
        Assert.Equal(0, header.Curve(0));
        Assert.InRange(header.Curve(0.5), 0.77, 0.78);
        Assert.Equal(1, header.Curve(1));
    }

    [Fact]
    public void UserAccountsDrawerHeader_ApiDefaultsAndCustomPictureSizesMatchFlutter()
    {
        var defaults = new UserAccountsDrawerHeader(accountName: null, accountEmail: null);
        Assert.Equal(EdgeInsetsGeometry.Only(bottom: 8.0), defaults.Margin);
        Assert.Equal(new Size(72.0, 72.0), defaults.CurrentAccountPictureSize);
        Assert.Equal(new Size(40.0, 40.0), defaults.OtherAccountsPicturesSize);
        Assert.Equal(Colors.White, defaults.ArrowColor);

        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: null,
                accountEmail: null,
                currentAccountPicture: new Text("current"),
                otherAccountsPictures: [new Text("other")],
                currentAccountPictureSize: new Size(60.0, 60.0),
                otherAccountsPicturesSize: new Size(30.0, 30.0),
                arrowColor: Colors.Crimson),
            ThemeData.Light));

        harness.Pump(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 60.0, height: 60.0));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 30.0, height: 30.0));
    }

    [Fact]
    public void UserAccountsDrawerHeader_DefaultsPicturesTextAndSemanticsMatchFlutter()
    {
        Color schemePrimary = Colors.DarkBlue;
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkGreen,
            ColorScheme = ThemeData.Light.ColorScheme with { Primary = schemePrimary },
            PrimaryTextTheme = new MaterialTextTheme(
                bodyLarge: MaterialTextTheme.DefaultBodyLarge.CopyWith(fontSize: 17, color: Colors.White),
                bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(fontSize: 13, color: Colors.White)),
        };
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Ada"),
                accountEmail: new Text("ada@example.test"),
                currentAccountPicture: new Text("current"),
                otherAccountsPictures:
                [
                    new Text("other-1"),
                    new Text("other-2"),
                    new Text("other-3"),
                    new Text("other-4"),
                ]),
            theme));

        var semantics = harness.PumpAndGetSemantics(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == schemePrimary);
        Assert.DoesNotContain(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == theme.PrimaryColor);
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 72, height: 72));
        Assert.Equal(
            3,
            FindDescendants<RenderConstrainedBox>(harness.RenderView)
                .Count(box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 40, height: 40)));
        Assert.Null(FindParagraph(harness.RenderView, "other-4"));
        Assert.Equal(17, FindParagraph(harness.RenderView, "Ada")!.FontSize);
        Assert.Equal(13, FindParagraph(harness.RenderView, "ada@example.test")!.FontSize);
        Assert.NotNull(FindSemantics(
            semantics,
            node => node.Label?.Contains("Signed in", StringComparison.Ordinal) == true));
        foreach (string label in new[] { "other-1", "other-2", "other-3" })
        {
            SemanticsNode? account = FindSemantics(
                semantics,
                node => node.Label == label);
            Assert.True(account is not null, harness.SemanticsDump);
            Assert.True(account!.Rect.Size == new Size(48.0, 48.0), harness.SemanticsDump);
        }
    }

    [Fact]
    public void UserAccountsDrawerHeader_DetailsToggleInvokesCallbackAndUpdatesArrowSemantics()
    {
        int detailsPressed = 0;
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Ada"),
                accountEmail: new Text("ada@example.test"),
                onDetailsPressed: () => detailsPressed++),
            ThemeData.Light));

        var semantics = harness.PumpAndGetSemantics(new Size(304, 260));
        Assert.NotNull(FindSemantics(semantics, node => node.Label?.Contains("Show accounts", StringComparison.Ordinal) == true));
        var button = FindSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(button);
        Assert.True(button!.PerformAction(SemanticsActions.Tap));

        semantics = harness.PumpAndGetSemantics(new Size(304, 260));
        Assert.Equal(1, detailsPressed);
        Assert.NotNull(FindSemantics(
            semantics,
            node => node.Label?.Contains("Hide accounts", StringComparison.Ordinal) == true));

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(304, 260));
        RenderTransform transform = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.InRange(transform.Transform[0], -1.000001, -0.999999);

        semantics = harness.PumpAndGetSemantics(new Size(304, 260));
        button = FindSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(button!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(304, 260));
        now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(304, 260));
        transform = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.InRange(transform.Transform[0], 0.999999, 1.000001);
        Assert.Equal(2, detailsPressed);
    }

    [Fact]
    public void UserAccountsDrawerHeader_RtlPlacesCurrentAccountAtVisualStartAndOthersAtEnd()
    {
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Name"),
                accountEmail: new Text("Mail"),
                currentAccountPicture: new Text("current"),
                otherAccountsPictures: [new Text("other")]),
            ThemeData.Light,
            direction: TextDirection.Rtl));

        harness.Pump(new Size(304, 260));

        var pictures = FindDescendants<RenderStack>(harness.RenderView)
            .Single(stack => stack.ChildCount == 2);
        var others = Assert.IsAssignableFrom<RenderBox>(pictures.FirstChild);
        var current = Assert.IsAssignableFrom<RenderBox>(pictures.ChildAfter(others));
        Assert.True(
            ((StackParentData)current.parentData!).offset.X > ((StackParentData)others.parentData!).offset.X);
    }

    [Theory]
    [InlineData(TextDirection.Ltr)]
    [InlineData(TextDirection.Rtl)]
    public void UserAccountsDrawerHeader_AccountDetailsUseFlutterCustomLayout(TextDirection direction)
    {
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Name"),
                accountEmail: new Text("Email"),
                onDetailsPressed: () => { }),
            ThemeData.Light,
            direction: direction));

        harness.Pump(new Size(304, 260));

        RenderCustomMultiChildLayoutBox layout = Assert.Single(
            FindDescendants<RenderCustomMultiChildLayoutBox>(harness.RenderView));
        RenderBox name = FindLayoutChild(layout, "accountName");
        RenderBox email = FindLayoutChild(layout, "accountEmail");
        RenderBox icon = FindLayoutChild(layout, "dropdownIcon");
        Point nameOffset = ((MultiChildLayoutParentData)name.parentData!).offset;
        Point emailOffset = ((MultiChildLayoutParentData)email.parentData!).offset;
        Point iconOffset = ((MultiChildLayoutParentData)icon.parentData!).offset;

        Assert.Equal(emailOffset.Y - name.Size.Height, nameOffset.Y, precision: 6);
        Assert.Equal(
            layout.Size.Height - (icon.Size.Height / 2.0) - (email.Size.Height / 2.0),
            emailOffset.Y,
            precision: 6);
        if (direction == TextDirection.Ltr)
        {
            Assert.Equal(0.0, nameOffset.X);
            Assert.Equal(0.0, emailOffset.X);
            Assert.Equal(layout.Size.Width - icon.Size.Width, iconOffset.X, precision: 6);
        }
        else
        {
            Assert.Equal(layout.Size.Width - name.Size.Width, nameOffset.X, precision: 6);
            Assert.Equal(layout.Size.Width - email.Size.Width, emailOffset.X, precision: 6);
            Assert.Equal(0.0, iconOffset.X);
        }
    }

    [Fact]
    public void UserAccountsDrawerHeader_UnrelatedRebuildDoesNotRotateArrow()
    {
        Widget BuildHeader() => Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Name"),
                accountEmail: new Text("Email"),
                onDetailsPressed: () => { }),
            ThemeData.Light);

        using var harness = new WidgetRenderHarness(BuildHeader());
        harness.Pump(new Size(304, 260));
        harness.Update(BuildHeader());

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        harness.Pump(new Size(304, 260));

        RenderTransform transform = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.Equal(Matrix4.Identity(), transform.Transform);
    }

    [Fact]
    public void UserAccountsDrawerHeader_RapidDetailsTogglesReverseCurrentAnimation()
    {
        int detailsPressed = 0;
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Name"),
                accountEmail: new Text("Email"),
                onDetailsPressed: () => detailsPressed++),
            ThemeData.Light));

        TapDetails(harness);
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.06));
        harness.Pump(new Size(304, 260));
        double openingM11 = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView)).Transform[0];
        Assert.InRange(openingM11, -0.999, 0.999);

        TapDetails(harness);
        TapDetails(harness);
        TapDetails(harness);
        now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
        harness.Pump(new Size(304, 260));

        Assert.Equal(4, detailsPressed);
        RenderTransform transform = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.InRange(transform.Transform[0], 0.999999, 1.000001);
    }

    [Theory]
    [InlineData(TextDirection.Ltr)]
    [InlineData(TextDirection.Rtl)]
    public void UserAccountsDrawerHeader_NullDetailsAndZeroAreaAreSafe(TextDirection direction)
    {
        using var harness = new WidgetRenderHarness(Root(
            new SizedBox(
                width: 0.0,
                height: 0.0,
                child: new UserAccountsDrawerHeader(accountName: null, accountEmail: null)),
            ThemeData.Light,
            direction: direction));

        harness.Pump(new Size(304, 260));

        RenderCustomMultiChildLayoutBox details = Assert.Single(
            FindDescendants<RenderCustomMultiChildLayoutBox>(harness.RenderView));
        Assert.Equal(0, details.ChildCount);
        Assert.Empty(FindDescendants<RenderTransform>(harness.RenderView));
        RenderConstrainedBox zeroBox = Assert.Single(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 0.0, height: 0.0));
        Assert.Equal(default, zeroBox.Size);
    }

    [Fact]
    public void DrawerHeader_DirectionalInsetsResolveAgainstTextDirection()
    {
        var header = new DrawerHeader(
            child: new Text("Directional"),
            margin: EdgeInsetsGeometry.DirectionalOnly(start: 3.0, end: 5.0),
            padding: EdgeInsetsGeometry.DirectionalOnly(start: 7.0, top: 11.0, end: 13.0, bottom: 17.0));
        using var harness = new WidgetRenderHarness(Root(
            header,
            ThemeData.Light,
            padding: new Thickness(0.0, 19.0, 0.0, 0.0),
            direction: TextDirection.Rtl));

        harness.Pump(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(13.0, 30.0, 7.0, 17.0));
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(5.0, 0.0, 3.0, 0.0));
    }

    [Fact]
    public void UserAccountsDrawerHeader_ValidatesPictureSizes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserAccountsDrawerHeader(
            accountName: null,
            accountEmail: null,
            currentAccountPictureSize: new Size(-1, 72)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserAccountsDrawerHeader(
            accountName: null,
            accountEmail: null,
            otherAccountsPicturesSize: new Size(double.PositiveInfinity, 40)));
    }

    private static Widget Root(
        Widget child,
        ThemeData theme,
        Thickness padding = default,
        TextDirection direction = TextDirection.Ltr)
    {
        return new MediaQuery(
            data: new MediaQueryData(Size: new Size(304, 260), Padding: padding, ViewPadding: padding),
            child: new Directionality(direction, new Theme(theme, child)));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static RenderBox FindLayoutChild(RenderCustomMultiChildLayoutBox layout, string id)
    {
        for (RenderBox? child = layout.FirstChild; child is not null; child = layout.ChildAfter(child))
        {
            if (Equals(((MultiChildLayoutParentData)child.parentData!).Id, id))
            {
                return child;
            }
        }

        throw new Xunit.Sdk.XunitException($"Missing custom-layout child '{id}'.");
    }

    private static void TapDetails(WidgetRenderHarness harness)
    {
        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(304, 260));
        SemanticsNode? button = FindSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(button);
        Assert.True(button!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(304, 260));
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null) return null;
        if (predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
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

        public string SemanticsDump => _pipeline.SemanticsOwner!.DebugDumpTree();

        public void Update(Widget rootWidget)
        {
            _rootElement.UpdateRoot(rootWidget);
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;
            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            public override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }
            public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(force: true); }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            public void UpdateRoot(Widget widget) => Update(widget);
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
