using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSearchTests : IDisposable
{
    public MaterialSearchTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void SearchBar_ExposesFlutterDefaultsAndConstructorMetadata()
    {
        var bar = new SearchBar();
        Assert.True(bar.Enabled);
        Assert.False(bar.AutoFocus);
        Assert.False(bar.ReadOnly);
        Assert.Equal(new Thickness(20), bar.ScrollPadding);
        Assert.Null(bar.Controller);
        Assert.Null(bar.FocusNode);
        Assert.Null(bar.HintText);

        var anchor = SearchAnchor.Bar(
            suggestionsBuilder: (_, _) => []);
        Assert.True(anchor.Enabled);
        Assert.Null(anchor.IsFullScreen);
        Assert.NotNull(anchor.Builder);
        Assert.NotNull(anchor.SuggestionsBuilder);

        Assert.Throws<InvalidOperationException>(() => new SearchController().OpenView());
        Assert.Throws<ArgumentOutOfRangeException>(() => new SearchAnchor(
            builder: (_, _) => new SizedBox(),
            suggestionsBuilder: (_, _) => [],
            viewElevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SearchAnchor(
            builder: (_, _) => new SizedBox(),
            suggestionsBuilder: (_, _) => [],
            headerHeight: 0));
    }

    [Fact]
    public void SearchBar_DefaultsUseM3TokensAndCollapsedTextFieldComposition()
    {
        var theme = ThemeData.Light;
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new SearchBar(
                hintText: "Search mail",
                leading: new Icon(Icons.Search),
                trailing: [new Icon(Icons.MoreVert)])));
        harness.Pump(new Size(900, 120));

        var constraints = Assert.Single(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 360
                   && box.AdditionalConstraints.MaxWidth == 800
                   && box.AdditionalConstraints.MinHeight == 56);
        Assert.Equal(360, constraints.AdditionalConstraints.MinWidth);

        var surface = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == theme.SurfaceContainerHighColor
                   && box.Decoration.BorderRadius == BorderRadius.Circular(9999));
        Assert.NotNull(surface.Decoration.BoxShadows);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(8, 0));
        Assert.NotNull(FindParagraph(harness.RenderView, "Search mail"));
    }

    [Fact]
    public void SearchBar_ThemeAndWidgetStatePropertiesResolveByPrecedence()
    {
        var theme = ThemeData.Light with
        {
            SearchBarTheme = new SearchBarThemeData(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.LightBlue),
                Elevation: MaterialStateProperty<double?>.All(2),
                Shape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.RoundedRectangle(12)),
                Padding: MaterialStateProperty<Thickness?>.All(new Thickness(4, 0)),
                Constraints: new BoxConstraints(MinWidth: 240, MaxWidth: 300, MinHeight: 48))
        };

        using var themed = new WidgetRenderHarness(Wrap(theme, new SearchBar(hintText: "Themed")));
        themed.Pump(new Size(500, 100));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(themed.RenderView),
            box => box.AdditionalConstraints.MinWidth == 240
                   && box.AdditionalConstraints.MaxWidth == 300
                   && box.AdditionalConstraints.MinHeight == 48);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(themed.RenderView),
            box => box.Decoration.Color == Colors.LightBlue
                   && box.Decoration.BorderRadius == BorderRadius.Circular(12));
        Assert.Contains(FindDescendants<RenderPadding>(themed.RenderView), value => value.Padding == new Thickness(4, 0));

        using var widgetOverride = new WidgetRenderHarness(Wrap(
            theme,
            new SearchBar(
                backgroundColor: MaterialStateProperty<Color?>.All(Colors.Orange),
                shape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.RoundedRectangle(20)))));
        widgetOverride.Pump(new Size(500, 100));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(widgetOverride.RenderView),
            box => box.Decoration.Color == Colors.Orange
                   && box.Decoration.BorderRadius == BorderRadius.Circular(20));
    }

    [Fact]
    public void SearchAnchor_ControllerOpensRouteBuildsSuggestionsAndClosesWithSelectedText()
    {
        var controller = new SearchController();
        int opened = 0;
        int closed = 0;
        var changed = new List<string>();
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => SearchAnchor.Bar(
                searchController: controller,
                barHintText: "Find item",
                onOpen: () => opened++,
                onClose: () => closed++,
                onChanged: changed.Add,
                suggestionsBuilder: (context, searchController) =>
                [
                    new ListTile(
                        title: new Text($"Result for {searchController.Text}"),
                        onTap: () => searchController.CloseView("selected")),
                    new Text("Secondary suggestion"),
                ])))));

        harness.Pump(new Size(640, 420));
        Assert.True(controller.IsAttached);
        Assert.False(controller.IsOpen);

        controller.OpenView();
        harness.Pump(new Size(640, 420));
        Assert.True(controller.IsOpen);
        Assert.Equal(1, opened);
        Assert.NotNull(FindParagraph(harness.RenderView, "Find item"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Result for "));
        Assert.NotNull(FindParagraph(harness.RenderView, "Secondary suggestion"));

        controller.Text = "ap";
        harness.Pump(new Size(640, 420));
        Assert.NotNull(FindParagraph(harness.RenderView, "Result for ap"));

        controller.CloseView("selected");
        harness.Pump(new Size(640, 420));
        Assert.False(controller.IsOpen);
        Assert.Equal("selected", controller.Text);
        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task SearchDelegate_ShowSearchCoordinatesQueryBodiesTypedCloseAndDelegateReuse()
    {
        var searchDelegate = new TestSearchDelegate();
        Assert.Equal(AnimationStatus.Dismissed, searchDelegate.TransitionAnimation.Status);
        var host = new SearchLaunchHost(searchDelegate, "wi");
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => host))));
        var state = harness.FindState<SearchLaunchHostState>();
        state.Open();
        Assert.Equal(AnimationStatus.Forward, searchDelegate.TransitionAnimation.Status);
        harness.Pump(new Size(640, 420));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.4));
        harness.Pump(new Size(640, 420));

        Assert.Equal(AnimationStatus.Completed, searchDelegate.TransitionAnimation.Status);
        Assert.True(searchDelegate.IsActive);
        Assert.Equal("wi", searchDelegate.Query);
        Assert.NotNull(FindParagraph(harness.RenderView, "Suggestions: wi"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Leading"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Action"));

        searchDelegate.ShowResults();
        harness.Pump(new Size(640, 420));
        Assert.Equal(AnimationStatus.Completed, searchDelegate.TransitionAnimation.Status);
        Assert.NotNull(FindParagraph(harness.RenderView, "Results: wi"));

        searchDelegate.Query = "widget";
        harness.Pump(new Size(640, 420));
        Assert.NotNull(FindParagraph(harness.RenderView, "Results: widget"));

        searchDelegate.Close("Widget");
        Assert.Equal(AnimationStatus.Reverse, searchDelegate.TransitionAnimation.Status);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.4));
        harness.Pump(new Size(640, 420));

        Assert.Equal(AnimationStatus.Dismissed, searchDelegate.TransitionAnimation.Status);
        Assert.Equal("Widget", await state.Result);
        Assert.False(searchDelegate.IsActive);
    }

    [Fact]
    public void SearchDelegate_ForwardsInputConfigurationSwitcherAndSearchSemantics()
    {
        var searchDelegate = new TestSearchDelegate(
            keyboardType: TextInputType.EmailAddress,
            textInputAction: TextInputAction.Done,
            autocorrect: false,
            enableSuggestions: false);
        var host = new SearchLaunchHost(searchDelegate, "mail");
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => host))));

        harness.FindState<SearchLaunchHostState>().Open();
        harness.Pump(new Size(640, 420));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.4));
        harness.Pump(new Size(640, 420));

        TextField field = Assert.Single(harness.FindWidgets<TextField>());
        Assert.Equal(TextInputType.EmailAddress, field.KeyboardType);
        Assert.Equal(TextInputAction.Done, field.TextInputAction);
        Assert.False(field.Autocorrect);
        Assert.False(field.EnableSuggestions);
        FocusTextInputState inputState = Assert.IsType<FocusTextInputState>(
            FocusManager.Instance.ResolveTextInputState());
        Assert.Equal(TextInputKeyboardType.EmailAddress, inputState.Configuration?.KeyboardType);
        Assert.Equal(TextInputActionType.Done, inputState.Configuration?.InputAction);
        Assert.False(inputState.Configuration?.Autocorrect);
        Assert.False(inputState.Configuration?.EnableSuggestions);
        Assert.Contains(
            harness.FindWidgets<AnimatedSwitcher>(),
            switcher => switcher.Duration == TimeSpan.FromMilliseconds(300));
        Assert.Contains(
            harness.FindWidgets<Semantics>(),
            semantics => semantics.InputType == SemanticsInputType.Search);
        Assert.Contains(
            FlattenSemantics(harness.SemanticsRoot),
            node => node.InputType == SemanticsInputType.Search);
    }

    [Fact]
    public void SearchDelegate_QueryDefaultsThemeAndInputMetadataMatchFlutter()
    {
        var searchDelegate = new TestSearchDelegate();
        Assert.Null(searchDelegate.KeyboardType);
        Assert.Equal(TextInputAction.Search, searchDelegate.TextInputAction);
        Assert.True(searchDelegate.Autocorrect);
        Assert.True(searchDelegate.EnableSuggestions);

        searchDelegate.Query = "query";
        Assert.Equal(TextSelection.Collapsed(5), searchDelegate.QueryController.Selection);
        searchDelegate.Query = string.Empty;
        Assert.Equal(TextSelection.Collapsed(0), searchDelegate.QueryController.Selection);

        ThemeData? resolved = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with
            {
                PrimaryIconTheme = new IconThemeData(Color: Colors.Red, Size: 31),
            },
            new Builder(context =>
            {
                resolved = searchDelegate.AppBarTheme(context);
                return new SizedBox();
            })));
        harness.Pump(new Size(100, 100));

        Assert.NotNull(resolved);
        Assert.Equal(Colors.White, resolved.AppBarTheme.BackgroundColor);
        Assert.Equal(Colors.Gray, resolved.AppBarTheme.IconTheme?.Color);
        Assert.Equal(31, resolved.AppBarTheme.IconTheme?.Size);
        Assert.Equal(SystemUiIconBrightness.Dark, resolved.AppBarTheme.SystemOverlayStyle?.StatusBarIconBrightness);
        Assert.Equal(InputBorder.None, resolved.InputDecorationTheme.Border);
    }

    [Fact]
    public void SearchDelegate_ValidatesFieldStyleDecorationExclusivity()
    {
        Assert.Throws<ArgumentException>(() => new TestSearchDelegate(
            searchFieldStyle: new TextStyle(FontSize: 16),
            searchFieldDecorationTheme: new InputDecorationThemeData()));
    }

    [Fact]
    public void SearchViewTheme_ControlsRouteSurfaceAndHeaderDefaults()
    {
        var controller = new SearchController("seed");
        var theme = ThemeData.Light with
        {
            SearchViewTheme = new SearchViewThemeData(
                BackgroundColor: Colors.LightGreen,
                Elevation: 0,
                Shape: ShapeBorder.RoundedRectangle(18),
                HeaderHeight: 64,
                BarPadding: new Thickness(10, 0),
                DividerColor: Colors.Red,
                Constraints: new BoxConstraints(MinWidth: 420, MinHeight: 260))
        };

        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                searchController: controller,
                builder: (_, searchController) => new SearchBar(controller: searchController, hintText: "Anchor"),
                suggestionsBuilder: (_, _) => [new Text("Themed suggestion")],
                isFullScreen: false)))));
        harness.Pump(new Size(700, 500));

        controller.OpenView();
        harness.Pump(new Size(700, 500));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.LightGreen
                   && box.Decoration.BorderRadius == BorderRadius.Circular(18));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 420
                   && box.AdditionalConstraints.MinHeight == 260);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 64
                   && box.AdditionalConstraints.MaxHeight == 64);
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.BorderSides?.Bottom?.Color == Colors.Red);
        Assert.NotNull(FindParagraph(harness.RenderView, "Themed suggestion"));
    }

    private static Widget Wrap(ThemeData theme, Widget child) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(700, 500)),
                new Theme(theme, child)));

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static IEnumerable<SemanticsNode> FlattenSemantics(SemanticsNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        yield return node;
        foreach (SemanticsNode child in node.Children)
        {
            foreach (SemanticsNode descendant in FlattenSemantics(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class TestSearchDelegate : SearchDelegate<string>
    {
        public TestSearchDelegate(
            TextStyle? searchFieldStyle = null,
            InputDecorationThemeData? searchFieldDecorationTheme = null,
            TextInputType? keyboardType = null,
            TextInputAction textInputAction = TextInputAction.Search,
            bool autocorrect = true,
            bool enableSuggestions = true) : base(
            searchFieldLabel: "Find framework term",
            searchFieldStyle: searchFieldStyle,
            searchFieldDecorationTheme: searchFieldDecorationTheme,
            keyboardType: keyboardType,
            textInputAction: textInputAction,
            autocorrect: autocorrect,
            enableSuggestions: enableSuggestions)
        {
        }

        public override Widget BuildSuggestions(BuildContext context) => new Text($"Suggestions: {Query}");

        public override Widget BuildResults(BuildContext context) => new Text($"Results: {Query}");

        public override Widget? BuildLeading(BuildContext context) => new Text("Leading");

        public override IReadOnlyList<Widget>? BuildActions(BuildContext context) => [new Text("Action")];
    }

    private sealed class SearchLaunchHost : StatefulWidget
    {
        public SearchLaunchHost(TestSearchDelegate searchDelegate, string query) : base(key: null)
        {
            SearchDelegate = searchDelegate;
            Query = query;
        }

        public TestSearchDelegate SearchDelegate { get; }

        public string Query { get; }

        public override State CreateState() => new SearchLaunchHostState();
    }

    private sealed class SearchLaunchHostState : State
    {
        private SearchLaunchHost Current => (SearchLaunchHost)StateWidget;

        public Task<string?> Result { get; private set; } = Task.FromResult<string?>(null);

        public override Widget Build(BuildContext context) => new SizedBox();

        public void Open() => Result = MaterialSearch.ShowSearch(Context, Current.SearchDelegate, Current.Query);

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
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public SemanticsNode? SemanticsRoot => _pipeline.SemanticsOwner.RootNode;

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
            _pipeline.FlushSemantics();
        }

        public void Dispose() => _rootElement.Unmount();

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return Assert.Single(states);
        }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            CollectWidgets(_rootElement, widgets);
            return widgets;
        }

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state)
            {
                states.Add(state);
            }

            element.VisitChildren(child => CollectStates(child, states));
        }

        private static void CollectWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T widget)
            {
                widgets.Add(widget);
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
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
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
