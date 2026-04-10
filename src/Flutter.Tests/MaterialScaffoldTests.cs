using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

public sealed class MaterialScaffoldTests
{
    public MaterialScaffoldTests()
    {
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    [Fact]
    public void Scaffold_UsesThemeScaffoldBackgroundColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ScaffoldBackgroundColor = Colors.Beige
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var background = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.Beige, background.Color);
    }

    [Fact]
    public void Scaffold_UsesExplicitBackgroundColorOverride()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    ScaffoldBackgroundColor = Colors.White
                },
                child: new Scaffold(
                    backgroundColor: Colors.Crimson,
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var background = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.Crimson, background.Color);
    }

    [Fact]
    public void ThemeData_DefaultsUseMaterial3ToTrue()
    {
        Assert.True(ThemeData.Light.UseMaterial3);
    }

    [Fact]
    public void ThemeData_DefaultsBrightnessToLight()
    {
        Assert.Equal(Brightness.Light, ThemeData.Light.Brightness);
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            CanvasColor = Colors.DarkSlateBlue,
            PrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Demo"),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var scaffoldBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var contentColumn = Assert.IsType<RenderFlex>(scaffoldBackground.Child);
        var appBarBackground = Assert.IsType<RenderColoredBox>(contentColumn.FirstChild);

        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);
        Assert.NotNull(contentColumn.ChildAfter(appBarBackground));
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemePrimaryColorForAppBarBackground_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            CanvasColor = Colors.Goldenrod,
            PrimaryColor = Colors.DarkSlateBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Demo"),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var scaffoldBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var contentColumn = Assert.IsType<RenderFlex>(scaffoldBackground.Child);
        var appBarBackground = Assert.IsType<RenderColoredBox>(contentColumn.FirstChild);

        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);
        Assert.NotNull(contentColumn.ChildAfter(appBarBackground));
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsDisabledAndBrightnessDark()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            Brightness = Brightness.Dark,
            CanvasColor = Colors.DarkSlateBlue,
            PrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Demo"),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var scaffoldBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var contentColumn = Assert.IsType<RenderFlex>(scaffoldBackground.Child);
        var appBarBackground = Assert.IsType<RenderColoredBox>(contentColumn.FirstChild);

        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);
        Assert.NotNull(contentColumn.ChildAfter(appBarBackground));
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            CanvasColor = Colors.DarkSlateBlue,
            OnSurfaceColor = Colors.Bisque,
            OnPrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);

        var paragraph = FindDescendant<RenderParagraph>(appBarBackground);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesThemeOnPrimaryColor_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.DarkSlateBlue,
            OnSurfaceColor = Colors.Crimson,
            OnPrimaryColor = Colors.Bisque
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);

        var paragraph = FindDescendant<RenderParagraph>(appBarBackground);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsDisabledAndBrightnessDark()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            Brightness = Brightness.Dark,
            CanvasColor = Colors.DarkSlateBlue,
            PrimaryColor = Colors.Crimson,
            OnSurfaceColor = Colors.Bisque,
            OnPrimaryColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);

        var paragraph = FindDescendant<RenderParagraph>(appBarBackground);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesSingleLineEllipsisDefaults()
    {
        var owner = new BuildOwner();
        const string title = "Very long default app bar title";
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: title)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, title);
        Assert.NotNull(paragraph);
        Assert.False(paragraph!.SoftWrap);
        Assert.Equal(1, paragraph.MaxLines);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
    }

    [Fact]
    public void AppBar_DefaultTitle_EmptyString_IsRenderedAsText()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: string.Empty)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, string.Empty);
        Assert.NotNull(paragraph);
        Assert.False(paragraph!.SoftWrap);
        Assert.Equal(1, paragraph.MaxLines);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
    }

    [Fact]
    public void AppBar_BackgroundColor_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkSlateBlue,
            AppBarTheme = new AppBarThemeData(BackgroundColor: Colors.Crimson),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.Crimson, appBarBackground.Color);
    }

    [Fact]
    public void AppBar_BackgroundColor_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(BackgroundColor: Colors.Crimson),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Demo",
                    backgroundColor: Colors.DarkOliveGreen)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkOliveGreen, appBarBackground.Color);
    }

    [Fact]
    public void AppBar_ForegroundColor_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnPrimaryColor = Colors.Bisque,
            AppBarTheme = new AppBarThemeData(ForegroundColor: Colors.Goldenrod),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, "Demo");
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Goldenrod, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_ForegroundColor_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ForegroundColor: Colors.Goldenrod),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Demo",
                    foregroundColor: Colors.CadetBlue)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, "Demo");
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.CadetBlue, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_SystemOverlayStyle_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var themedStyle = new SystemUiOverlayStyle(
            StatusBarColor: Colors.Crimson,
            NavigationBarColor: Colors.DarkGreen,
            StatusBarIconBrightness: SystemUiIconBrightness.Light,
            NavigationBarIconBrightness: SystemUiIconBrightness.Light);
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(SystemOverlayStyle: themedStyle),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(themedStyle, SystemChrome.CurrentSystemUiOverlayStyle);
    }

    [Fact]
    public void AppBar_SystemOverlayStyle_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var themedStyle = new SystemUiOverlayStyle(
            StatusBarColor: Colors.Crimson,
            NavigationBarColor: Colors.DarkGreen,
            StatusBarIconBrightness: SystemUiIconBrightness.Light,
            NavigationBarIconBrightness: SystemUiIconBrightness.Light);
        var widgetStyle = new SystemUiOverlayStyle(
            StatusBarColor: Colors.Bisque,
            NavigationBarColor: Colors.CadetBlue,
            StatusBarIconBrightness: SystemUiIconBrightness.Dark,
            NavigationBarIconBrightness: SystemUiIconBrightness.Dark);
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(SystemOverlayStyle: themedStyle),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Demo",
                    systemOverlayStyle: widgetStyle)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(widgetStyle, SystemChrome.CurrentSystemUiOverlayStyle);
    }

    [Fact]
    public void AppBar_CenterTitleTrue_WrapsTitleInCenterAlign()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Centered",
                    centerTitle: true)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.NotNull(align);
        Assert.Equal(Alignment.Center, align!.Alignment);
    }

    [Fact]
    public void AppBar_CenterTitle_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.Android,
            AppBarTheme = new AppBarThemeData(CenterTitle: true),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Centered by theme")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.NotNull(align);
        Assert.Equal(Alignment.Center, align!.Alignment);
    }

    [Fact]
    public void AppBar_CenterTitle_ExplicitValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.MacOS,
            AppBarTheme = new AppBarThemeData(CenterTitle: true),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Not centered",
                    centerTitle: false)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.Null(align);
    }

    [Fact]
    public void AppBar_CenterTitle_DefaultsFromPlatform_MacOS_WhenActionsCountLessThanTwo()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.MacOS,
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Centered by platform",
                    actions:
                    [
                        new SizedBox(width: 8, height: 8),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.NotNull(align);
        Assert.Equal(Alignment.Center, align!.Alignment);
    }

    [Fact]
    public void AppBar_CenterTitle_DefaultsFromPlatform_MacOS_WithTwoActions_IsNotCentered()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.MacOS,
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Not centered by platform",
                    actions:
                    [
                        new SizedBox(width: 8, height: 8),
                        new SizedBox(width: 8, height: 8),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.Null(align);
    }

    [Fact]
    public void AppBar_LeadingWidth_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(LeadingWidth: 80),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new SizedBox(width: 12, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var leadingBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinWidth - 80) < 0.001
            && Math.Abs(constraints.MaxWidth - 80) < 0.001);

        Assert.NotNull(leadingBox);
    }

    [Fact]
    public void AppBar_LeadingWidth_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(LeadingWidth: 80),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new SizedBox(width: 12, height: 12),
                    leadingWidth: 64)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var leadingBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinWidth - 64) < 0.001
            && Math.Abs(constraints.MaxWidth - 64) < 0.001);

        Assert.NotNull(leadingBox);
    }

    [Fact]
    public void AppBar_LeadingSlot_IsConstrainedByLeadingWidthAndToolbarHeight()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    leading: new SizedBox(width: 12, height: 12),
                    leadingWidth: 64,
                    toolbarHeight: 72)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var leadingBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinWidth - 64) < 0.001
            && Math.Abs(constraints.MaxWidth - 64) < 0.001
            && Math.Abs(constraints.MinHeight - 72) < 0.001
            && Math.Abs(constraints.MaxHeight - 72) < 0.001);

        Assert.NotNull(leadingBox);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_ShowsBackIcon_OnNonRootRoute()
    {
        var owner = new BuildOwner();
        BuildContext? rootContext = null;

        static Widget BuildBody() => new SizedBox(width: 24, height: 12);

        Route? BuildRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        rootContext ??= context;
                        return new Scaffold(
                            appBar: new AppBar(titleText: "Root"),
                            body: BuildBody());
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(titleText: "Details"),
                        body: BuildBody()),
                    settings: settings),
                _ => null,
            };
        }

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Navigator(
                    onGenerateRoute: BuildRoute,
                    initialRouteName: "/")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(rootContext.HasValue);
        Navigator.PushNamed(rootContext!.Value, "/details");
        owner.FlushBuild();

        var arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
        var arrowBackParagraph = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.NotNull(arrowBackParagraph);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_False_HidesBackIcon_OnNonRootRoute()
    {
        var owner = new BuildOwner();
        BuildContext? rootContext = null;

        static Widget BuildBody() => new SizedBox(width: 24, height: 12);

        Route? BuildRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        rootContext ??= context;
                        return new Scaffold(
                            appBar: new AppBar(titleText: "Root"),
                            body: BuildBody());
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(
                            titleText: "Details",
                            automaticallyImplyLeading: false),
                        body: BuildBody()),
                    settings: settings),
                _ => null,
            };
        }

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Navigator(
                    onGenerateRoute: BuildRoute,
                    initialRouteName: "/")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(rootContext.HasValue);
        Navigator.PushNamed(rootContext!.Value, "/details");
        owner.FlushBuild();

        var arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
        var arrowBackParagraph = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.Null(arrowBackParagraph);
    }

    [Fact]
    public void AppBar_ActionsPadding_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ActionsPadding: new Thickness(13, 5, 19, 7)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 13) < 0.001
            && Math.Abs(padding.Top - 5) < 0.001
            && Math.Abs(padding.Right - 19) < 0.001
            && Math.Abs(padding.Bottom - 7) < 0.001);

        Assert.NotNull(actionsPadding);
    }

    [Fact]
    public void AppBar_ActionsPadding_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ActionsPadding: new Thickness(13, 5, 19, 7)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actionsPadding: new Thickness(4, 6, 8, 10),
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 4) < 0.001
            && Math.Abs(padding.Top - 6) < 0.001
            && Math.Abs(padding.Right - 8) < 0.001
            && Math.Abs(padding.Bottom - 10) < 0.001);

        Assert.NotNull(actionsPadding);
    }

    [Fact]
    public void AppBar_DefaultOuterPadding_IsZero()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var outerPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Right) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(outerPadding);
    }

    [Fact]
    public void AppBar_PrimaryTrue_AppliesMediaQueryTopPadding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new MediaQuery(
                    data: new MediaQueryData(
                        Padding: new Thickness(0, 24, 0, 0),
                        ViewPadding: new Thickness(0, 24, 0, 0)),
                    child: new AppBar(titleText: "Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var safeAreaPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left) < 0.001
            && Math.Abs(padding.Top - 24) < 0.001
            && Math.Abs(padding.Right) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(safeAreaPadding);
    }

    [Fact]
    public void AppBar_PrimaryFalse_DoesNotApplyMediaQueryTopPadding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new MediaQuery(
                    data: new MediaQueryData(
                        Padding: new Thickness(0, 24, 0, 0),
                        ViewPadding: new Thickness(0, 24, 0, 0)),
                    child: new AppBar(
                        titleText: "Title",
                        primary: false))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var safeAreaPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left) < 0.001
            && Math.Abs(padding.Top - 24) < 0.001
            && Math.Abs(padding.Right) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.Null(safeAreaPadding);
    }

    [Fact]
    public void AppBar_ActionsRow_DoesNotApplyExtraSpacing()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    actionsPadding: new Thickness(3, 4, 5, 6),
                    actions:
                    [
                        new Text("One"),
                        new Text("Two"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 3) < 0.001
            && Math.Abs(padding.Top - 4) < 0.001
            && Math.Abs(padding.Right - 5) < 0.001
            && Math.Abs(padding.Bottom - 6) < 0.001);
        Assert.NotNull(actionsPadding);

        var actionsRow = FindDescendant<RenderFlex>(actionsPadding);
        Assert.NotNull(actionsRow);
        Assert.Equal(Axis.Horizontal, actionsRow!.Direction);
        Assert.Equal(MainAxisSize.Min, actionsRow.MainAxisSize);
        Assert.Equal(CrossAxisAlignment.Center, actionsRow.CrossAxisAlignment);
        Assert.Equal(0, actionsRow.Spacing);
    }

    [Fact]
    public void AppBar_ActionsRow_UsesStretchCrossAxisAlignment_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false
        };
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actionsPadding: new Thickness(7, 8, 9, 10),
                    actions:
                    [
                        new Text("One"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 7) < 0.001
            && Math.Abs(padding.Top - 8) < 0.001
            && Math.Abs(padding.Right - 9) < 0.001
            && Math.Abs(padding.Bottom - 10) < 0.001);
        Assert.NotNull(actionsPadding);

        var actionsRow = FindDescendant<RenderFlex>(actionsPadding);
        Assert.NotNull(actionsRow);
        Assert.Equal(MainAxisSize.Min, actionsRow!.MainAxisSize);
        Assert.Equal(CrossAxisAlignment.Stretch, actionsRow.CrossAxisAlignment);
        Assert.Equal(0, actionsRow.Spacing);
    }

    [Fact]
    public void AppBar_IconTheme_DefaultsFromThemeAppBarTheme_ForLeading()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                IconTheme: new IconThemeData(
                    Color: Colors.Crimson,
                    Size: 19)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Crimson, capturedTheme!.Color);
        Assert.Equal(19, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_DefaultsToOnSurfaceAndSize24_ForLeading_WhenUseMaterial3IsEnabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.CadetBlue,
            OnPrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_DefaultsToOnPrimary_ForLeading_WhenUseMaterial3IsDisabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            OnSurfaceColor = Colors.Crimson,
            OnPrimaryColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
        Assert.Null(capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_WidgetValue_OverridesThemeAppBarTheme_ForLeading()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                IconTheme: new IconThemeData(
                    Color: Colors.Crimson,
                    Size: 19)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    iconTheme: new IconThemeData(
                        Color: Colors.CadetBlue,
                        Size: 21),
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
        Assert.Equal(21, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_WithNullColor_FallsBackToForeground_ForLeading()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    foregroundColor: Colors.DarkRed,
                    iconTheme: new IconThemeData(Size: 22),
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkRed, capturedTheme!.Color);
        Assert.Equal(22, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_DefaultsFromThemeAppBarTheme_ForActions()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ActionsIconTheme: new IconThemeData(
                    Color: Colors.Goldenrod,
                    Size: 17)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Goldenrod, capturedTheme!.Color);
        Assert.Equal(17, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_DefaultsToOnSurfaceVariant_WhenUseMaterial3IsEnabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.CadetBlue,
            OnSurfaceVariantColor = Colors.Goldenrod,
            OnPrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Goldenrod, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_DefaultsToOnPrimary_WhenUseMaterial3IsDisabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            OnSurfaceVariantColor = Colors.Goldenrod,
            OnPrimaryColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_WidgetValue_OverridesThemeAppBarTheme_ForActions()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ActionsIconTheme: new IconThemeData(
                    Color: Colors.Goldenrod,
                    Size: 17)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actionsIconTheme: new IconThemeData(
                        Color: Colors.LimeGreen,
                        Size: 23),
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.LimeGreen, capturedTheme!.Color);
        Assert.Equal(23, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_FallsBackToAppBarIconTheme_WhenActionsThemeMissing()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                IconTheme: new IconThemeData(
                    Color: Colors.DarkCyan,
                    Size: 14)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkCyan, capturedTheme!.Color);
        Assert.Equal(14, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_FallsBackToWidgetIconTheme_WhenActionsThemeMissing()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    iconTheme: new IconThemeData(
                        Color: Colors.DarkOrange,
                        Size: 11),
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkOrange, capturedTheme!.Color);
        Assert.Equal(11, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_WithNullColor_FallsBackToForeground_ForActions()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    foregroundColor: Colors.LimeGreen,
                    actionsIconTheme: new IconThemeData(Size: 24),
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.LimeGreen, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_Actions_ReceiveToolbarTextStyle_AndActionsIconTheme()
    {
        ActionContextSnapshot? snapshot = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    toolbarTextStyle: new TextStyle(
                        FontSize: 18,
                        Color: Colors.CadetBlue,
                        FontWeight: FontWeight.Bold),
                    actionsIconTheme: new IconThemeData(
                        Color: Colors.Goldenrod,
                        Size: 20),
                    actions:
                    [
                        new CaptureActionContextWidget(data => snapshot = data),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot!.TextStyle.Color);
        Assert.Equal(18, snapshot.TextStyle.FontSize);
        Assert.Equal(Colors.CadetBlue, snapshot.TextStyle.Color!.Value);
        Assert.Equal(FontWeight.Bold, snapshot.TextStyle.FontWeight);
        Assert.Equal(Colors.Goldenrod, snapshot.IconThemeData.Color);
        Assert.Equal(20, snapshot.IconThemeData.Size);
    }

    [Fact]
    public void AppBar_TitleSpacing_AppliesHorizontalPaddingToTitle()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    title: new SizedBox(width: 40, height: 12),
                    titleSpacing: 24)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titlePadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 24) < 0.001
            && Math.Abs(padding.Right - 24) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(titlePadding);
    }

    [Fact]
    public void AppBar_TitleSpacing_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(TitleSpacing: 22),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(title: new SizedBox(width: 40, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titlePadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 22) < 0.001
            && Math.Abs(padding.Right - 22) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(titlePadding);
    }

    [Fact]
    public void AppBar_TitleSpacing_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(TitleSpacing: 22),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    title: new SizedBox(width: 40, height: 12),
                    titleSpacing: 30)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titlePadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 30) < 0.001
            && Math.Abs(padding.Right - 30) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(titlePadding);
    }

    [Fact]
    public void AppBar_ToolbarHeight_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ToolbarHeight: 72),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 72) < 0.001
            && Math.Abs(constraints.MaxHeight - 72) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_ToolbarHeight_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ToolbarHeight: 72),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    toolbarHeight: 64)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 64) < 0.001
            && Math.Abs(constraints.MaxHeight - 64) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_ToolbarHeight_DefaultsTo56_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 56) < 0.001
            && Math.Abs(constraints.MaxHeight - 56) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_ToolbarHeight_DefaultsTo56_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false
        };
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 56) < 0.001
            && Math.Abs(constraints.MaxHeight - 56) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_TitleTextStyle_DefaultsFromTextThemeTitleLarge_WithThemeForegroundFallback()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.Bisque,
            OnPrimaryColor = Colors.Crimson,
            TextTheme = new MaterialTextTheme(
                titleLarge: new TextStyle(
                    FontSize: 29,
                    Color: Colors.Crimson,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titleParagraph = FindParagraphByText(appBarBackground, "Title");
        Assert.NotNull(titleParagraph);
        Assert.Equal(29, titleParagraph!.FontSize);
        Assert.Equal(FontWeight.Bold, titleParagraph.FontWeight);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(titleParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_TitleTextStyle_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                TitleTextStyle: new TextStyle(
                    FontSize: 26,
                    Color: Colors.Crimson,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titleParagraph = FindParagraphByText(appBarBackground, "Title");
        Assert.NotNull(titleParagraph);
        Assert.Equal(26, titleParagraph!.FontSize);
        Assert.Equal(FontWeight.Bold, titleParagraph.FontWeight);
        Assert.Equal(Colors.Crimson, Assert.IsType<SolidColorBrush>(titleParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_TitleTextStyle_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                TitleTextStyle: new TextStyle(
                    FontSize: 26,
                    Color: Colors.Crimson,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    titleTextStyle: new TextStyle(
                        FontSize: 18,
                        Color: Colors.LimeGreen,
                        FontWeight: FontWeight.Normal))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titleParagraph = FindParagraphByText(appBarBackground, "Title");
        Assert.NotNull(titleParagraph);
        Assert.Equal(18, titleParagraph!.FontSize);
        Assert.Equal(FontWeight.Normal, titleParagraph.FontWeight);
        Assert.Equal(Colors.LimeGreen, Assert.IsType<SolidColorBrush>(titleParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_ToolbarTextStyle_DefaultsFromThemeAppBarTheme_ForActionsText()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ToolbarTextStyle: new TextStyle(
                    FontSize: 17,
                    Color: Colors.Goldenrod,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionParagraph = FindParagraphByText(appBarBackground, "Action");
        Assert.NotNull(actionParagraph);
        Assert.Equal(17, actionParagraph!.FontSize);
        Assert.Equal(FontWeight.Bold, actionParagraph.FontWeight);
        Assert.Equal(Colors.Goldenrod, Assert.IsType<SolidColorBrush>(actionParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_ToolbarTextStyle_WidgetValue_OverridesThemeAppBarTheme_ForActionsText()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ToolbarTextStyle: new TextStyle(
                    FontSize: 17,
                    Color: Colors.Goldenrod,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    toolbarTextStyle: new TextStyle(
                        FontSize: 15,
                        Color: Colors.CadetBlue,
                        FontWeight: FontWeight.Normal),
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionParagraph = FindParagraphByText(appBarBackground, "Action");
        Assert.NotNull(actionParagraph);
        Assert.Equal(15, actionParagraph!.FontSize);
        Assert.Equal(FontWeight.Normal, actionParagraph.FontWeight);
        Assert.Equal(Colors.CadetBlue, Assert.IsType<SolidColorBrush>(actionParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_NegativeTitleSpacing_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AppBar(
            titleText: "Invalid",
            titleSpacing: -1));
    }

    [Fact]
    public void AppBar_NonPositiveThemeToolbarHeight_Throws()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(ToolbarHeight: 0),
                },
                child: new AppBar(titleText: "Invalid")));

        root.Attach(owner);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();
        });
    }

    [Fact]
    public void AppBar_NonPositiveThemeLeadingWidth_Throws()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(LeadingWidth: 0),
                },
                child: new AppBar(
                    titleText: "Invalid",
                    leading: new SizedBox(width: 8, height: 8))));

        root.Attach(owner);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();
        });
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

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderParagraph paragraph && string.Equals(paragraph.Text, text, StringComparison.Ordinal))
        {
            return paragraph;
        }

        RenderParagraph? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindParagraphByText(child, text);
        });

        return result;
    }

    private static RenderPadding? FindPadding(RenderObject? root, Predicate<Thickness> predicate)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPadding padding && predicate(padding.Padding))
        {
            return padding;
        }

        RenderPadding? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindPadding(child, predicate);
        });

        return result;
    }

    private static RenderConstrainedBox? FindConstrainedBox(RenderObject? root, Predicate<BoxConstraints> predicate)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderConstrainedBox constrainedBox && predicate(constrainedBox.AdditionalConstraints))
        {
            return constrainedBox;
        }

        RenderConstrainedBox? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindConstrainedBox(child, predicate);
        });

        return result;
    }

    private sealed class CaptureIconThemeWidget : StatelessWidget
    {
        private readonly Action<IconThemeData> _capture;

        public CaptureIconThemeWidget(Action<IconThemeData> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(IconTheme.Of(context));
            return new SizedBox(width: 8, height: 8);
        }
    }

    private sealed record ActionContextSnapshot(TextStyle TextStyle, IconThemeData IconThemeData);

    private sealed class CaptureActionContextWidget : StatelessWidget
    {
        private readonly Action<ActionContextSnapshot> _capture;

        public CaptureActionContextWidget(Action<ActionContextSnapshot> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(new ActionContextSnapshot(
                TextStyle: DefaultTextStyle.Of(context),
                IconThemeData: IconTheme.Of(context)));
            return new SizedBox(width: 8, height: 8);
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
