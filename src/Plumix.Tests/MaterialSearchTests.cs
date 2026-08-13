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

    private static SuggestionsBuilder Sync(Func<SearchController, IReadOnlyList<Widget>> builder) =>
        (_, controller) => new ValueTask<IReadOnlyList<Widget>>(builder(controller));

    [Fact]
    public void SearchBar_DefaultsConstructorMetadataAndControllerAsserts()
    {
        var bar = new SearchBar();
        Assert.True(bar.Enabled);
        Assert.False(bar.AutoFocus);
        Assert.False(bar.ReadOnly);
        Assert.Equal(new Thickness(20), bar.ScrollPadding);
        Assert.Null(bar.Controller);
        Assert.Null(bar.FocusNode);
        Assert.Null(bar.HintText);

        var anchor = SearchAnchor.Bar(suggestionsBuilder: Sync(_ => Array.Empty<Widget>()));
        Assert.True(anchor.Enabled);
        Assert.Null(anchor.IsFullScreen);
        Assert.NotNull(anchor.Builder);
        Assert.NotNull(anchor.SuggestionsBuilder);

        var detached = new SearchController();
        Assert.False(detached.IsAttached);
        Assert.Throws<InvalidOperationException>(() => detached.OpenView());
        Assert.Throws<InvalidOperationException>(() => detached.CloseView(null));
        Assert.Throws<InvalidOperationException>(() => detached.IsOpen);
    }

    [Fact]
    public void SearchBar_DefaultsUseM3MaterialSurfaceAndCollapsedField()
    {
        var theme = ThemeData.Light;
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new SearchBar(
                hintText: "Search mail",
                leading: new Icon(Icons.Search),
                trailing: [new Icon(Icons.MoreVert)])));
        harness.Pump(new Size(900, 120));

        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 360
                   && box.AdditionalConstraints.MaxWidth == 800
                   && box.AdditionalConstraints.MinHeight == 56);

        Plumix.Material.Material surface = Assert.Single(harness.FindWidgets<Plumix.Material.Material>());
        Assert.Equal(6.0, surface.Elevation);
        Assert.Equal(theme.SurfaceContainerHighColor, surface.Color);
        Assert.Equal(theme.ShadowColor, surface.ShadowColor);
        Assert.Equal(Colors.Transparent, surface.SurfaceTintColor);
        Assert.IsType<StadiumBorder>(surface.Shape);

        // The resolved padding is applied twice: around the Row and around the inner text field.
        Assert.True(harness.FindWidgets<Padding>()
            .Count(padding => padding.InsetsGeometry == EdgeInsetsGeometry.Symmetric(horizontal: 8.0)) >= 2);

        TextField field = Assert.Single(harness.FindWidgets<TextField>());
        Assert.Equal(InputBorder.None, field.Decoration?.Border);
        Assert.Equal(InputBorder.None, field.Decoration?.EnabledBorder);
        Assert.Equal(InputBorder.None, field.Decoration?.FocusedBorder);
        Assert.Equal(EdgeInsetsGeometry.Zero, field.Decoration?.ContentPadding);
        Assert.True(field.Decoration?.IsDense);
        Assert.NotEqual(true, field.Decoration?.IsCollapsed);
        Assert.Equal(theme.OnSurfaceVariantColor, field.Decoration?.HintStyle?.Color);
        Assert.Equal(theme.OnSurfaceColor, field.Style?.Color);
        Assert.Contains(
            harness.FindWidgets<Semantics>(),
            semantics => semantics.InputType == SemanticsInputType.Search);
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
                Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                    Plumix.Rendering.BorderRadius.Circular(12))),
                Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(
                    EdgeInsetsGeometry.Symmetric(horizontal: 4)),
                Constraints: new BoxConstraints(MinWidth: 240, MaxWidth: 300, MinHeight: 48))
        };

        using var themed = new WidgetRenderHarness(Wrap(theme, new SearchBar(hintText: "Themed")));
        themed.Pump(new Size(500, 100));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(themed.RenderView),
            box => box.AdditionalConstraints.MinWidth == 240
                   && box.AdditionalConstraints.MaxWidth == 300
                   && box.AdditionalConstraints.MinHeight == 48);
        Plumix.Material.Material themedSurface = Assert.Single(themed.FindWidgets<Plumix.Material.Material>());
        Assert.Equal(Colors.LightBlue, themedSurface.Color);
        Assert.Equal(2.0, themedSurface.Elevation);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12)),
            themedSurface.Shape);
        Assert.Contains(themed.FindWidgets<Padding>(),
            padding => padding.InsetsGeometry == EdgeInsetsGeometry.Symmetric(horizontal: 4));

        using var widgetOverride = new WidgetRenderHarness(Wrap(
            theme,
            new SearchBar(
                backgroundColor: MaterialStateProperty<Color?>.All(Colors.Orange),
                shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                    Plumix.Rendering.BorderRadius.Circular(20))))));
        widgetOverride.Pump(new Size(500, 100));
        Plumix.Material.Material overriddenSurface = Assert.Single(
            widgetOverride.FindWidgets<Plumix.Material.Material>());
        Assert.Equal(Colors.Orange, overriddenSurface.Color);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(20)),
            overriddenSurface.Shape);
    }

    [Fact]
    public void SearchBar_HintStyleFallsBackToTextStyleAndOverridesIt()
    {
        using var fallback = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SearchBar(
                hintText: "hint",
                textStyle: MaterialStateProperty<TextStyle?>.All(new TextStyle(Color: Colors.Purple)))));
        fallback.Pump(new Size(900, 120));
        TextField fallbackField = Assert.Single(fallback.FindWidgets<TextField>());
        Assert.Equal(Colors.Purple, fallbackField.Decoration?.HintStyle?.Color);

        using var overridden = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SearchBar(
                hintText: "hint",
                textStyle: MaterialStateProperty<TextStyle?>.All(new TextStyle(Color: Colors.Purple)),
                hintStyle: MaterialStateProperty<TextStyle?>.All(new TextStyle(Color: Colors.Green)))));
        overridden.Pump(new Size(900, 120));
        TextField overriddenField = Assert.Single(overridden.FindWidgets<TextField>());
        Assert.Equal(Colors.Green, overriddenField.Decoration?.HintStyle?.Color);
        Assert.Equal(Colors.Purple, overriddenField.Style?.Color);
    }

    [Fact]
    public void SearchBar_ForwardsInputConfigurationToEditableText()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SearchBar(
                keyboardType: TextInputType.EmailAddress,
                textInputAction: TextInputAction.Done,
                textCapitalization: TextCapitalization.Characters,
                smartDashesType: SmartDashesType.Disabled,
                smartQuotesType: SmartQuotesType.Disabled,
                scrollPadding: new Thickness(42))));
        harness.Pump(new Size(900, 120));

        TextField field = Assert.Single(harness.FindWidgets<TextField>());
        Assert.Equal(TextInputType.EmailAddress, field.KeyboardType);
        Assert.Equal(TextInputAction.Done, field.TextInputAction);
        Assert.Equal(TextCapitalization.Characters, field.TextCapitalization);
        Assert.Equal(SmartDashesType.Disabled, field.SmartDashesType);
        Assert.Equal(SmartQuotesType.Disabled, field.SmartQuotesType);
        Assert.Equal(new Thickness(42), field.ScrollPadding);

        EditableText editable = Assert.Single(harness.FindWidgets<EditableText>());
        Assert.Equal(TextCapitalization.Characters, editable.TextCapitalization);
        Assert.Equal(SmartDashesType.Disabled, editable.SmartDashesType);
        Assert.Equal(SmartQuotesType.Disabled, editable.SmartQuotesType);
        Assert.Equal(new Thickness(42), editable.ScrollPadding);
    }

    [Fact]
    public void SearchAnchor_ControllerOpensRouteBuildsSuggestionsAndClosesWithSelectedText()
    {
        var controller = new SearchController();
        int opened = 0;
        int closed = 0;
        var changed = new List<string>();
        var size = new Size(640, 420);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => SearchAnchor.Bar(
                searchController: controller,
                barHintText: "Find item",
                onOpen: () => opened++,
                onClose: () => closed++,
                onChanged: changed.Add,
                suggestionsBuilder: Sync(searchController => new Widget[]
                {
                    new ListTile(
                        title: new Text($"Result for {searchController.Text}"),
                        onTap: () => searchController.CloseView("selected")),
                    new Text("Secondary suggestion"),
                }))))));

        harness.Pump(size);
        Assert.True(controller.IsAttached);
        Assert.False(controller.IsOpen);

        controller.OpenView();
        Settle(harness, size);
        Assert.True(controller.IsOpen);
        Assert.Equal(1, opened);
        Assert.NotNull(FindParagraph(harness.RenderView, "Find item"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Result for "));
        Assert.NotNull(FindParagraph(harness.RenderView, "Secondary suggestion"));

        // The anchor fades out while the view is open.
        Assert.Contains(harness.FindWidgets<AnimatedOpacity>(), widget => widget.Opacity == 0.0);

        controller.Text = "ap";
        Settle(harness, size);
        Assert.NotNull(FindParagraph(harness.RenderView, "Result for ap"));

        controller.CloseView("selected");
        Settle(harness, size);
        Assert.False(controller.IsOpen);
        Assert.Equal("selected", controller.Text);
        Assert.Equal(1, closed);
        Assert.Contains(harness.FindWidgets<AnimatedOpacity>(), widget => widget.Opacity == 1.0);
    }

    [Fact]
    public void SearchAnchor_OpensAnchoredViewWithM3DefaultsAndGeometry()
    {
        var controller = new SearchController();
        var size = new Size(700, 500);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => SearchAnchor.Bar(
                searchController: controller,
                suggestionsBuilder: Sync(_ => new Widget[] { new Text("suggestion") }))))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);

        Plumix.Material.Material view = Assert.Single(
            harness.FindWidgets<Plumix.Material.Material>(),
            material => material.ClipBehavior == Clip.AntiAlias);
        Assert.Equal(6.0, view.Elevation);
        Assert.Equal(ThemeData.Light.SurfaceContainerHighColor, view.Color);
        Assert.Equal(Colors.Transparent, view.SurfaceTintColor);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(28.0)),
            view.Shape);

        // Docked geometry: width clamps the anchor width, height is 2/3 of the navigator height.
        Assert.Contains(harness.FindWidgets<ConstrainedBox>(),
            box => box.Constraints.MaxWidth == 700
                   && box.Constraints.MaxHeight == 500 * 2.0 / 3.0
                   && box.Constraints.MinWidth == 360
                   && box.Constraints.MinHeight == 240);
        Assert.Contains(harness.FindWidgets<Widgets.Transform>(),
            transform => transform.Matrix == Matrix.CreateTranslation(0, 0));
        Assert.Contains(harness.FindWidgets<OverflowBox>(),
            box => box.Fit == OverflowBoxFit.DeferToChild && box.MaxWidth == 700);
    }

    [Fact]
    public void SearchAnchor_ViewGeometryClampsToNavigator_LtrRtl()
    {
        var size = new Size(700, 500);
        foreach ((TextDirection direction, double expectedX) in new[]
                 {
                     (TextDirection.Ltr, 340.0),
                     (TextDirection.Rtl, 0.0),
                 })
        {
            var controller = new SearchController();
            Alignment anchorAlignment = direction == TextDirection.Ltr
                ? Alignment.BottomRight
                : Alignment.BottomLeft;
            using var harness = new WidgetRenderHarness(Wrap(
                ThemeData.Light,
                new Navigator(new BuilderPageRoute(_ => new Align(
                    alignment: anchorAlignment,
                    child: new SearchAnchor(
                        searchController: controller,
                        builder: (_, _) => new SizedBox(width: 24, height: 24),
                        suggestionsBuilder: Sync(_ => Array.Empty<Widget>()))))),
                direction));
            harness.Pump(size);

            controller.OpenView();
            Settle(harness, size);

            double expectedY = 500 - 500 * 2.0 / 3.0;
            Assert.Contains(harness.FindWidgets<Widgets.Transform>(),
                transform => transform.Matrix == Matrix.CreateTranslation(expectedX, expectedY));
            Assert.Contains(harness.FindWidgets<ConstrainedBox>(),
                box => box.Constraints.MaxWidth == 360 && box.Constraints.MaxHeight == 500 * 2.0 / 3.0);
        }
    }

    [Fact]
    public void SearchAnchor_FullScreenFillsNavigatorAndIgnoresViewPadding()
    {
        var controller = new SearchController();
        var size = new Size(700, 500);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                searchController: controller,
                isFullScreen: true,
                viewPadding: EdgeInsetsGeometry.All(16),
                builder: (_, _) => new SizedBox(width: 24, height: 24),
                suggestionsBuilder: Sync(_ => Array.Empty<Widget>()))))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);

        Plumix.Material.Material view = Assert.Single(
            harness.FindWidgets<Plumix.Material.Material>(),
            material => material.ClipBehavior == Clip.AntiAlias);
        Assert.Equal(new RoundedRectangleBorder(), view.Shape);
        Assert.Contains(harness.FindWidgets<ConstrainedBox>(),
            box => box.Constraints.MaxWidth == 700 && box.Constraints.MaxHeight == 500);
        // viewPadding is ignored in the full-screen branch.
        Assert.DoesNotContain(harness.FindWidgets<Padding>(),
            padding => padding.InsetsGeometry == EdgeInsetsGeometry.All(16));
        // The full-screen header bar keeps the 72dp minimum.
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 72);
    }

    [Fact]
    public void SearchAnchor_DockedViewPopsOnMetricsChangeWhileFullScreenStays()
    {
        foreach (bool fullScreen in new[] { false, true })
        {
            var controller = new SearchController();
            var host = new MediaSizeHost(
                new Size(640, 420),
                new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                    searchController: controller,
                    isFullScreen: fullScreen,
                    builder: (_, _) => new SizedBox(width: 24, height: 24),
                    suggestionsBuilder: Sync(_ => Array.Empty<Widget>())))));
            using var harness = new WidgetRenderHarness(new Directionality(
                TextDirection.Ltr,
                host));
            harness.Pump(new Size(640, 420));

            controller.OpenView();
            Settle(harness, new Size(640, 420));
            Assert.True(controller.IsOpen);

            harness.FindState<MediaSizeHostState>().SetSize(new Size(500, 300));
            Settle(harness, new Size(500, 300));

            Assert.Equal(fullScreen, controller.IsOpen);
        }
    }

    [Fact]
    public void SearchAnchor_CapturedInheritedThemesReachTheViewRoute()
    {
        var controller = new SearchController();
        var size = new Size(640, 420);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new SearchViewTheme(
                new SearchViewThemeData(BackgroundColor: Colors.LightGreen),
                new SearchAnchor(
                    searchController: controller,
                    builder: (_, _) => new SizedBox(width: 24, height: 24),
                    suggestionsBuilder: Sync(_ => Array.Empty<Widget>())))))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);

        Plumix.Material.Material view = Assert.Single(
            harness.FindWidgets<Plumix.Material.Material>(),
            material => material.ClipBehavior == Clip.AntiAlias);
        Assert.Equal(Colors.LightGreen, view.Color);
    }

    [Fact]
    public void SearchAnchor_AsyncSuggestionsResolveOnceAndRefreshOnTextChange()
    {
        var controller = new SearchController();
        int builderCalls = 0;
        var completion = new TaskCompletionSource<IReadOnlyList<Widget>>();
        var size = new Size(640, 420);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                searchController: controller,
                builder: (_, _) => new SizedBox(width: 24, height: 24),
                suggestionsBuilder: async (_, _) =>
                {
                    builderCalls++;
                    return await completion.Task;
                })))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);
        Assert.Equal(1, builderCalls);
        Assert.Null(FindParagraph(harness.RenderView, "Async result"));

        // Extra frames with an unchanged query must not re-run the builder.
        Settle(harness, size);
        Assert.Equal(1, builderCalls);

        completion.SetResult([new Text("Async result")]);
        Settle(harness, size);
        Assert.NotNull(FindParagraph(harness.RenderView, "Async result"));

        completion = new TaskCompletionSource<IReadOnlyList<Widget>>();
        completion.SetResult([new Text("Typed result")]);
        controller.Text = "ty";
        Settle(harness, size);
        Assert.Equal(2, builderCalls);
        Assert.NotNull(FindParagraph(harness.RenderView, "Typed result"));
    }

    [Fact]
    public void SearchAnchor_DefaultViewLeadingTrailingAndClearButton()
    {
        var controller = new SearchController();
        var size = new Size(640, 420);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                searchController: controller,
                builder: (_, _) => new SizedBox(width: 24, height: 24),
                suggestionsBuilder: Sync(_ => Array.Empty<Widget>()))))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);

        string clearTooltip = DefaultMaterialLocalizations.Instance.ClearButtonTooltip;
        Assert.NotEmpty(harness.FindWidgets<BackButton>());
        Assert.DoesNotContain(harness.FindWidgets<IconButton>(), button => button.Tooltip == clearTooltip);

        controller.Text = "query";
        Settle(harness, size);
        Assert.Single(harness.FindWidgets<IconButton>(), button => button.Tooltip == clearTooltip);

        controller.Clear();
        Settle(harness, size);
        Assert.DoesNotContain(harness.FindWidgets<IconButton>(), button => button.Tooltip == clearTooltip);
    }

    [Fact]
    public void SearchAnchor_ViewForwardsInputConfigurationAndCallbacks()
    {
        var controller = new SearchController();
        var changed = new List<string>();
        var submitted = new List<string>();
        var size = new Size(640, 420);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                searchController: controller,
                textCapitalization: TextCapitalization.Sentences,
                textInputAction: TextInputAction.Send,
                keyboardType: TextInputType.Url,
                smartDashesType: SmartDashesType.Disabled,
                smartQuotesType: SmartQuotesType.Disabled,
                viewOnChanged: changed.Add,
                viewOnSubmitted: submitted.Add,
                builder: (_, _) => new SizedBox(width: 24, height: 24),
                suggestionsBuilder: Sync(_ => Array.Empty<Widget>()))))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);

        TextField viewField = Assert.Single(harness.FindWidgets<TextField>());
        Assert.True(viewField.Autofocus);
        Assert.Equal(TextCapitalization.Sentences, viewField.TextCapitalization);
        Assert.Equal(TextInputAction.Send, viewField.TextInputAction);
        Assert.Equal(TextInputType.Url, viewField.KeyboardType);
        Assert.Equal(SmartDashesType.Disabled, viewField.SmartDashesType);
        Assert.Equal(SmartQuotesType.Disabled, viewField.SmartQuotesType);

        viewField.OnChanged?.Invoke("abc");
        Assert.Equal("abc", Assert.Single(changed));
        viewField.OnSubmitted?.Invoke("abc");
        Assert.Equal("abc", Assert.Single(submitted));
    }

    [Fact]
    public void SearchAnchor_AttachDetachAndExternalControllerSurvivesDispose()
    {
        var first = new SearchController();
        var second = new SearchController();
        var host = new ControllerSwapHost(first, second);
        var size = new Size(640, 420);
        var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => host))));
        harness.Pump(size);
        Assert.True(first.IsAttached);
        Assert.False(second.IsAttached);

        harness.FindState<ControllerSwapHostState>().UseSecond();
        harness.Pump(size);
        Assert.False(first.IsAttached);
        Assert.True(second.IsAttached);

        second.OpenView();
        Settle(harness, size);
        Assert.True(second.IsOpen);

        harness.Dispose();
        Assert.False(second.IsAttached);
        second.Text = "still usable";
        Assert.Equal("still usable", second.Text);
    }

    [Fact]
    public void SearchAnchor_DisabledStatesUseOpacityAndBlockOpening()
    {
        var controller = new SearchController();
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => SearchAnchor.Bar(
                searchController: controller,
                enabled: false,
                suggestionsBuilder: Sync(_ => Array.Empty<Widget>()))))));
        harness.Pump(new Size(640, 420));

        Assert.Contains(harness.FindWidgets<AnimatedOpacity>(), widget => widget.Opacity == 0.38);
        Assert.Contains(harness.FindWidgets<Opacity>(), widget => widget.Value == 0.38);
        Assert.Contains(harness.FindWidgets<IgnorePointer>(), widget => widget.Ignoring);
        TextField field = Assert.Single(harness.FindWidgets<TextField>());
        Assert.False(field.Enabled ?? true);
    }

    [Fact]
    public void SearchAnchor_ShrinkWrapOmitsDividerUntilSuggestionsExist()
    {
        var controller = new SearchController();
        var suggestions = new List<Widget>();
        var size = new Size(640, 420);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                searchController: controller,
                shrinkWrap: true,
                viewConstraints: new BoxConstraints(),
                builder: (_, _) => new SizedBox(width: 24, height: 24),
                suggestionsBuilder: Sync(_ => suggestions.ToList()))))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);
        Assert.Empty(harness.FindWidgets<Divider>());

        suggestions.Add(new Text("One suggestion"));
        controller.Text = "o";
        Settle(harness, size);
        Assert.NotEmpty(harness.FindWidgets<Divider>());
    }

    [Fact]
    public void SearchThemes_LerpFollowsSourceQuirks()
    {
        var barTheme = new SearchBarThemeData(Elevation: MaterialStateProperty<double?>.All(3));
        Assert.Same(barTheme, SearchBarThemeData.Lerp(barTheme, barTheme, 0.3));

        var viewTheme = new SearchViewThemeData(Elevation: 1);
        Assert.Same(viewTheme, SearchViewThemeData.Lerp(viewTheme, viewTheme, 0.6));

        var a = new SearchViewThemeData(
            HeaderTextStyle: new TextStyle(Color: Colors.Blue),
            HeaderHintStyle: new TextStyle(Color: Colors.Red),
            Side: new BorderSide(Colors.Black, 4.0));
        var b = new SearchViewThemeData(
            HeaderTextStyle: new TextStyle(Color: Colors.Blue),
            HeaderHintStyle: new TextStyle(Color: Colors.Yellow));
        SearchViewThemeData mid = SearchViewThemeData.Lerp(a, b, 0.5)!;

        // Upstream quirk: headerHintStyle lerps the headerTextStyle inputs.
        Assert.Equal(mid.HeaderTextStyle, mid.HeaderHintStyle);
        // A null side lerps against a transparent zero-width side.
        Assert.Equal(2.0, mid.Side!.Value.Width);
    }

    [Fact]
    public void SearchAnchor_ZeroAreaDoesNotCrash()
    {
        var controller = new SearchController();
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => SearchAnchor.Bar(
                searchController: controller,
                suggestionsBuilder: Sync(_ => Array.Empty<Widget>()))))));
        harness.Pump(new Size(0, 0));
        harness.Pump(new Size(0, 0));
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
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(18)),
                HeaderHeight: 64,
                BarPadding: EdgeInsetsGeometry.Symmetric(horizontal: 10),
                DividerColor: Colors.Red,
                Constraints: new BoxConstraints(MinWidth: 420, MinHeight: 260))
        };
        var size = new Size(700, 500);

        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new Navigator(new BuilderPageRoute(_ => new SearchAnchor(
                searchController: controller,
                builder: (_, searchController) => new SearchBar(controller: searchController, hintText: "Anchor"),
                suggestionsBuilder: Sync(_ => new Widget[] { new Text("Themed suggestion") }),
                isFullScreen: false)))));
        harness.Pump(size);

        controller.OpenView();
        Settle(harness, size);

        Plumix.Material.Material view = Assert.Single(
            harness.FindWidgets<Plumix.Material.Material>(),
            material => material.ClipBehavior == Clip.AntiAlias);
        Assert.Equal(Colors.LightGreen, view.Color);
        Assert.Equal(0.0, view.Elevation);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(18)),
            view.Shape);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 64
                   && box.AdditionalConstraints.MaxHeight == 64);
        Divider divider = Assert.Single(harness.FindWidgets<Divider>());
        Assert.Equal(1.0, divider.Height!.Value);
        Assert.Contains(harness.FindWidgets<DividerTheme>(), widget => widget.Data.Color == Colors.Red);
        Assert.NotNull(FindParagraph(harness.RenderView, "Themed suggestion"));
    }

    private static void Settle(WidgetRenderHarness harness, Size size)
    {
        for (int i = 0; i < 3; i++)
        {
            harness.Pump(size);
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.7));
        }

        harness.Pump(size);
    }

    private static Widget Wrap(ThemeData theme, Widget child, TextDirection direction = TextDirection.Ltr) =>
        new Directionality(
            direction,
            new MediaQuery(
                new MediaQueryData(Size: new Size(700, 500)),
                new Theme(theme, child)));

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

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

    private sealed class MediaSizeHost : StatefulWidget
    {
        public MediaSizeHost(Size initialSize, Widget child)
        {
            InitialSize = initialSize;
            Child = child;
        }

        public Size InitialSize { get; }

        public Widget Child { get; }

        public override State CreateState() => new MediaSizeHostState();
    }

    private sealed class MediaSizeHostState : State
    {
        private Size? _size;

        private MediaSizeHost Current => (MediaSizeHost)StateWidget;

        public void SetSize(Size size) => SetState(() => _size = size);

        public override Widget Build(BuildContext context) => new MediaQuery(
            new MediaQueryData(Size: _size ?? Current.InitialSize),
            Current.Child);
    }

    private sealed class ControllerSwapHost : StatefulWidget
    {
        public ControllerSwapHost(SearchController first, SearchController second)
        {
            First = first;
            Second = second;
        }

        public SearchController First { get; }

        public SearchController Second { get; }

        public override State CreateState() => new ControllerSwapHostState();
    }

    private sealed class ControllerSwapHostState : State
    {
        private bool _useSecond;

        private ControllerSwapHost Current => (ControllerSwapHost)StateWidget;

        public void UseSecond() => SetState(() => _useSecond = true);

        public override Widget Build(BuildContext context) => new SearchAnchor(
            searchController: _useSecond ? Current.Second : Current.First,
            builder: (_, _) => new SizedBox(width: 24, height: 24),
            suggestionsBuilder: (_, _) => new ValueTask<IReadOnlyList<Widget>>([]));
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
