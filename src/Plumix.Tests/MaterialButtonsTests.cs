using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialButtonsTests
{
    public MaterialButtonsTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void ThemeData_Light_UsesPlumixMaterial3ColorDefaults()
    {
        var theme = ThemeData.Light;

        Assert.Equal(Color.Parse("#FFFEF7FF"), theme.ScaffoldBackgroundColor);
        Assert.Equal(Color.Parse("#FFFEF7FF"), theme.CanvasColor);
        Assert.Equal(Color.Parse("#FF6750A4"), theme.PrimaryColor);
        Assert.Equal(Color.Parse("#FF1D1B20"), theme.ColorScheme.OnSurface);
        Assert.Equal(Color.Parse("#FF4A4458"), theme.ColorScheme.OnSecondaryContainer);
        Assert.Equal(Colors.Black, theme.ShadowColor);
    }

    [Fact]
    public void ThemeData_Light_DefaultMaterialTapTargetSize_IsPadded()
    {
        Assert.Equal(MaterialTapTargetSize.Padded, ThemeData.Light.MaterialTapTargetSize);
    }

    [Fact]
    public void TextButton_M3DefaultForeground_ReadsColorSchemePrimaryDirectly()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.DarkCyan)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Tap me"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_M2DefaultForeground_ReadsColorSchemePrimaryDirectly()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.DarkCyan)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Tap me"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TextButton_DisabledDefaultForeground_ReadsColorSchemeOnSurfaceDirectly(bool useMaterial3)
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = useMaterial3,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.MidnightBlue)
        };
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: null,
                    child: new Text("Disabled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(
            ApplyOpacity(theme.ColorScheme.OnSurface, 0.38),
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_DefaultIconTheme_UsesForegroundAndIconSizeDefaults()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.DarkCyan)
        };
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkCyan, capturedTheme!.Color);
        Assert.Equal(18, capturedTheme.Size);
    }

    [Fact]
    public void TextButton_ConstructorsExposeCallbacksStateAndSemanticSurface()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        bool longPressed = false;
        Action<bool> hover = _ => { };
        Action<bool> focusChange = _ => { };
        var button = new TextButton(
            child: new Text("Hold"),
            onPressed: null,
            onLongPress: () => longPressed = true,
            onHover: hover,
            onFocusChange: focusChange,
            focusNode: focusNode,
            autofocus: true,
            clipBehavior: Clip.AntiAlias,
            statesController: statesController);

        Assert.NotNull(button.OnLongPress);
        Assert.Same(hover, button.OnHover);
        Assert.Same(focusChange, button.OnFocusChange);
        Assert.Same(focusNode, button.FocusNode);
        Assert.True(button.Autofocus);
        Assert.Equal(Clip.AntiAlias, button.ClipBehavior);
        Assert.Same(statesController, button.StatesController);
        // Dart's ElevatedButton/OutlinedButton do not expose isSemanticButton; the base default holds.
        Assert.True(button.IsSemanticButton);

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(TextDirection.Ltr, button)));
        var semantics = harness.PumpAndGetSemantics(new Size(120, 80));
        var actionNode = FindSemantics(
            semantics,
            node => node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.NotNull(actionNode);
        Assert.True(actionNode!.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(actionNode.PerformAction(SemanticsActions.LongPress));
        Assert.True(longPressed);
    }

    [Fact]
    public void TextButtonTheme_WrapPreservesThemeData()
    {
        var data = new TextButtonThemeData(
            style: TextButton.StyleFrom(foregroundColor: Colors.DarkCyan));
        Widget child = new Text("Captured");
        var theme = new TextButtonTheme(data, child);

        var wrapped = Assert.IsType<TextButtonTheme>(theme.Wrap(default, child));

        Assert.Same(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);
    }

    [Fact]
    public void ElevatedButton_ConstructorsExposeCallbacksStateAndSemanticSurface()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        bool longPressed = false;
        Action<bool> hover = _ => { };
        Action<bool> focusChange = _ => { };
        var button = new ElevatedButton(
            child: new Text("Hold"),
            onPressed: null,
            onLongPress: () => longPressed = true,
            onHover: hover,
            onFocusChange: focusChange,
            focusNode: focusNode,
            autofocus: true,
            clipBehavior: Clip.AntiAlias,
            statesController: statesController);

        Assert.NotNull(button.OnLongPress);
        Assert.Same(hover, button.OnHover);
        Assert.Same(focusChange, button.OnFocusChange);
        Assert.Same(focusNode, button.FocusNode);
        Assert.True(button.Autofocus);
        Assert.Equal(Clip.AntiAlias, button.ClipBehavior);
        Assert.Same(statesController, button.StatesController);
        // Dart's ElevatedButton/OutlinedButton do not expose isSemanticButton; the base default holds.
        Assert.True(button.IsSemanticButton);

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(TextDirection.Ltr, button)));
        var semantics = harness.PumpAndGetSemantics(new Size(120, 80));
        var actionNode = FindSemantics(
            semantics,
            node => node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.NotNull(actionNode);
        Assert.True(actionNode!.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(actionNode.PerformAction(SemanticsActions.LongPress));
        Assert.True(longPressed);
    }

    [Fact]
    public void ElevatedButtonTheme_WrapPreservesThemeData()
    {
        var data = new ElevatedButtonThemeData(
            style: ElevatedButton.StyleFrom(foregroundColor: Colors.DarkCyan));
        Widget child = new Text("Captured");
        var theme = new ElevatedButtonTheme(data, child);

        var wrapped = Assert.IsType<ElevatedButtonTheme>(theme.Wrap(default, child));

        Assert.Same(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);
    }

    [Fact]
    public void OutlinedButton_ConstructorsExposeCallbacksStateAndSemanticSurface()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        bool longPressed = false;
        Action<bool> hover = _ => { };
        Action<bool> focusChange = _ => { };
        var button = new OutlinedButton(
            child: new Text("Hold"),
            onPressed: null,
            onLongPress: () => longPressed = true,
            onHover: hover,
            onFocusChange: focusChange,
            focusNode: focusNode,
            autofocus: true,
            clipBehavior: Clip.AntiAlias,
            statesController: statesController);

        Assert.NotNull(button.OnLongPress);
        Assert.Same(hover, button.OnHover);
        Assert.Same(focusChange, button.OnFocusChange);
        Assert.Same(focusNode, button.FocusNode);
        Assert.True(button.Autofocus);
        Assert.Equal(Clip.AntiAlias, button.ClipBehavior);
        Assert.Same(statesController, button.StatesController);
        // Dart's ElevatedButton/OutlinedButton do not expose isSemanticButton; the base default holds.
        Assert.True(button.IsSemanticButton);

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(TextDirection.Ltr, button)));
        var semantics = harness.PumpAndGetSemantics(new Size(120, 80));
        var actionNode = FindSemantics(
            semantics,
            node => node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.NotNull(actionNode);
        Assert.True(actionNode!.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(actionNode.PerformAction(SemanticsActions.LongPress));
        Assert.True(longPressed);
    }

    [Fact]
    public void OutlinedButtonTheme_WrapPreservesThemeData()
    {
        var data = new OutlinedButtonThemeData(
            style: OutlinedButton.StyleFrom(foregroundColor: Colors.DarkCyan));
        Widget child = new Text("Captured");
        var theme = new OutlinedButtonTheme(data, child);

        var wrapped = Assert.IsType<OutlinedButtonTheme>(theme.Wrap(default, child));

        Assert.Same(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FilledButton_DefaultsReadColorSchemeRolesDirectly(bool useMaterial3)
    {
        var owner = new BuildOwner();
        var colorScheme = ThemeData.Light.ColorScheme.CopyWith(
            primary: Colors.DarkCyan,
            onPrimary: Colors.AliceBlue,
            onSurface: Colors.MidnightBlue,
            shadow: Colors.DarkGreen);
        var theme = ThemeData.Light with
        {
            UseMaterial3 = useMaterial3,
            PrimaryColor = Colors.OrangeRed,
            ShadowColor = Colors.Black,
            ColorScheme = colorScheme
        };
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: () => { },
                    child: new Text("Filled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(colorScheme.Primary, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(colorScheme.OnPrimary, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButton_DefaultStyleExposesGeneratedNonNullContract()
    {
        var owner = new BuildOwner();
        ButtonStyle? captured = null;
        var button = new FilledButton(
            onPressed: () => { },
            child: new Text("Defaults"));
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Builder(context =>
                {
                    captured = button.DefaultStyleOf(context);
                    return button;
                })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        ButtonStyle style = Assert.IsType<ButtonStyle>(captured);
        Assert.NotNull(style.TextStyle);
        Assert.NotNull(style.BackgroundColor);
        Assert.NotNull(style.ForegroundColor);
        Assert.NotNull(style.OverlayColor);
        Assert.NotNull(style.ShadowColor);
        Assert.NotNull(style.SurfaceTintColor);
        Assert.NotNull(style.Elevation);
        Assert.NotNull(style.Padding);
        Assert.NotNull(style.MinimumSize);
        Assert.NotNull(style.MaximumSize);
        Assert.NotNull(style.IconColor);
        Assert.NotNull(style.IconSize);
        Assert.NotNull(style.Shape);
        Assert.NotNull(style.MouseCursor);
        Assert.NotNull(style.VisualDensity);
        Assert.NotNull(style.TapTargetSize);
        Assert.NotNull(style.AnimationDuration);
        Assert.NotNull(style.EnableFeedback);
        Assert.NotNull(style.Alignment);
        Assert.NotNull(style.SplashFactory);
        Assert.Null(style.FixedSize);
        Assert.Null(style.Side);
        Assert.Null(style.BackgroundBuilder);
        Assert.Null(style.ForegroundBuilder);
    }

    [Fact]
    public void FilledButtonTonal_DefaultsReadColorSchemeRolesDirectly()
    {
        var owner = new BuildOwner();
        var colorScheme = ThemeData.Light.ColorScheme.CopyWith(
            secondaryContainer: Colors.Bisque,
            onSecondaryContainer: Colors.DarkSlateBlue);
        var theme = ThemeData.Light with
        {
            ColorScheme = colorScheme
        };
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: FilledButton.Tonal(
                    onPressed: () => { },
                    child: new Text("Filled tonal"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(colorScheme.SecondaryContainer, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(
            colorScheme.OnSecondaryContainer,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButton_ConstructorsExposeCallbacksClipAndStateSurface()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        bool longPressed = false;
        Action longPress = () => longPressed = true;
        Action<bool> hover = _ => { };
        Action<bool> focusChange = _ => { };
        FilledButton[] buttons =
        [
            new FilledButton(
                child: new Text("Filled"),
                onPressed: null,
                onLongPress: longPress,
                onHover: hover,
                onFocusChange: focusChange,
                focusNode: focusNode,
                autofocus: true,
                clipBehavior: Clip.AntiAlias,
                statesController: statesController),
            FilledButton.Tonal(
                child: new Text("Tonal"),
                onPressed: null,
                onLongPress: longPress,
                onHover: hover,
                onFocusChange: focusChange,
                clipBehavior: Clip.AntiAlias,
                statesController: statesController),
            FilledButton.Icon(
                label: new Text("Icon"),
                icon: new Icon(Icons.Star),
                onPressed: null,
                onLongPress: longPress,
                onHover: hover,
                onFocusChange: focusChange,
                clipBehavior: Clip.AntiAlias,
                statesController: statesController),
            FilledButton.TonalIcon(
                label: new Text("Tonal icon"),
                icon: new Icon(Icons.Star),
                onPressed: null,
                onLongPress: longPress,
                onHover: hover,
                onFocusChange: focusChange,
                clipBehavior: Clip.AntiAlias,
                statesController: statesController)
        ];

        Assert.All(buttons, button =>
        {
            Assert.Same(longPress, button.OnLongPress);
            Assert.Same(hover, button.OnHover);
            Assert.Same(focusChange, button.OnFocusChange);
            Assert.Equal(Clip.AntiAlias, button.ClipBehavior);
            Assert.Same(statesController, button.StatesController);
        });
        Assert.Same(focusNode, buttons[0].FocusNode);
        Assert.True(buttons[0].Autofocus);

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(TextDirection.Ltr, buttons[0])));
        var semantics = harness.PumpAndGetSemantics(new Size(120, 80));
        var buttonNode = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(buttonNode);
        Assert.True(buttonNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));

        var actionNode = FindSemantics(
            semantics,
            node => node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.NotNull(actionNode);
        Assert.Same(buttonNode, actionNode);
        Assert.True(actionNode!.PerformAction(SemanticsActions.LongPress));
        Assert.True(longPressed);
    }

    [Fact]
    public void ButtonStyleButton_SemanticsAbsorbFocusInkAndLabelIntoTheTapTargetNode()
    {
        var style = new ButtonStyle(
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(88, 36)),
            TapTargetSize: MaterialTapTargetSize.Padded);
        Func<Action, Action, ButtonStyleButton>[] factories =
        [
            (onPressed, onLongPress) => new ElevatedButton(
                onPressed: onPressed,
                onLongPress: onLongPress,
                style: style,
                child: new Text("ABC")),
            (onPressed, onLongPress) => new OutlinedButton(
                onPressed: onPressed,
                onLongPress: onLongPress,
                style: style,
                child: new Text("ABC")),
            (onPressed, onLongPress) => new TextButton(
                onPressed: onPressed,
                onLongPress: onLongPress,
                style: style,
                child: new Text("ABC")),
            (onPressed, onLongPress) => new FilledButton(
                onPressed: onPressed,
                onLongPress: onLongPress,
                style: style,
                child: new Text("ABC")),
        ];

        foreach (Func<Action, Action, ButtonStyleButton> factory in factories)
        {
            int tapCount = 0;
            int longPressCount = 0;
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Directionality(
                        TextDirection.Ltr,
                        new Align(
                            alignment: Alignment.Center,
                            child: factory(() => tapCount++, () => longPressCount++)))));

            SemanticsNode root = harness.PumpAndGetSemantics(new Size(800, 600))!;
            SemanticsNode buttonNode = Assert.Single(root.Children);

            Assert.Empty(buttonNode.Children);
            Assert.Equal("ABC", buttonNode.Label);
            Assert.Equal(new Rect(0, 0, 88, 48), buttonNode.Rect);
            Assert.Equal(new Rect(356, 276, 88, 48), buttonNode.GlobalRect);
            Assert.True(buttonNode.Flags.HasFlag(SemanticsFlags.HasEnabledState));
            Assert.True(buttonNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
            Assert.True(buttonNode.Flags.HasFlag(SemanticsFlags.IsButton));
            Assert.True(buttonNode.Flags.HasFlag(SemanticsFlags.IsFocusable));
            Assert.True(buttonNode.Actions.HasFlag(SemanticsActions.Tap));
            Assert.True(buttonNode.Actions.HasFlag(SemanticsActions.LongPress));
            Assert.True(buttonNode.Actions.HasFlag(SemanticsActions.Focus));
            Assert.True(buttonNode.PerformAction(SemanticsActions.Tap));
            Assert.True(buttonNode.PerformAction(SemanticsActions.LongPress));
            Assert.True(buttonNode.PerformAction(SemanticsActions.Focus));
            Scheduler.FlushMicrotasks();
            Assert.Equal(1, tapCount);
            Assert.Equal(1, longPressCount);
        }
    }

    [Fact]
    public void FilledButton_DisabledWhileHoveredReportsExitAndClearsHoveredState()
    {
        var owner = new BuildOwner();
        var statesController = new MaterialStatesController();
        var hoverChanges = new List<bool>();
        Widget BuildButton(Action? onPressed)
        {
            return new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: onPressed,
                    onHover: hoverChanges.Add,
                    statesController: statesController,
                    child: new Text("Hover lifecycle")));
        }

        var root = new TestRootElement(BuildButton(() => { }));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 212,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));
        owner.FlushBuild();

        root.Update(BuildButton(onPressed: null));
        owner.FlushBuild();
        Assert.Equal([true], hoverChanges);
        Assert.True(statesController.Value.HasFlag(MaterialState.Hovered));
        Assert.True(statesController.Value.HasFlag(MaterialState.Disabled));

        hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerExitEvent(
                pointer: 212,
                kind: PointerDeviceKind.Mouse,
                position: new Point(130, 90),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(130, 90)));
        owner.FlushBuild();

        Assert.Equal([true, false], hoverChanges);
        Assert.False(statesController.Value.HasFlag(MaterialState.Hovered));
        Assert.True(statesController.Value.HasFlag(MaterialState.Disabled));
    }

    [Fact]
    public void FilledButton_DisablingPressedButtonAddsDisabledBeforeRemovingPressed()
    {
        var owner = new BuildOwner();
        var statesController = new MaterialStatesController();
        var values = new List<MaterialState>();
        statesController.AddListener(() => values.Add(statesController.Value));
        Widget BuildButton(Action? onPressed)
        {
            return new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: onPressed,
                    statesController: statesController,
                    child: new Text("Pressed lifecycle")));
        }

        var root = new TestRootElement(BuildButton(() => { }));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        statesController.Update(MaterialState.Pressed, true);
        owner.FlushBuild();
        Assert.True(statesController.Value.HasFlag(MaterialState.Pressed));

        root.Update(BuildButton(onPressed: null));
        owner.FlushBuild();

        Assert.False(statesController.Value.HasFlag(MaterialState.Pressed));
        Assert.True(statesController.Value.HasFlag(MaterialState.Disabled));
        // Dart adds `disabled` first and only then clears `pressed`, so the intermediate value
        // carries both.
        Assert.True(values.Count >= 3);
        Assert.True(values[^2].HasFlag(MaterialState.Pressed));
        Assert.True(values[^2].HasFlag(MaterialState.Disabled));
        Assert.True(values[^1].HasFlag(MaterialState.Disabled));
        Assert.False(values[^1].HasFlag(MaterialState.Pressed));
    }

    [Fact]
    public void FilledButtonTheme_WrapPreservesThemeData()
    {
        var data = new FilledButtonThemeData(
            style: FilledButton.StyleFrom(foregroundColor: Colors.DarkCyan));
        Widget child = new Text("Captured");
        var theme = new FilledButtonTheme(data, child);

        var wrapped = Assert.IsType<FilledButtonTheme>(theme.Wrap(default, child));

        Assert.Same(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);
    }

    [Fact]
    public void FilledButton_StyleFromCarriesCursorDensityTimingFeedbackAndBuilders()
    {
        var enabledCursor = new SystemMouseCursor("filled-enabled");
        var disabledCursor = new SystemMouseCursor("filled-disabled");
        var density = new VisualDensity(horizontal: -2, vertical: 1);
        TimeSpan duration = TimeSpan.FromMilliseconds(350);
        ButtonLayerBuilder backgroundBuilder = (_, _, child) => child!;
        ButtonLayerBuilder foregroundBuilder = (_, _, child) => child!;
        ButtonStyle style = FilledButton.StyleFrom(
            enabledMouseCursor: enabledCursor,
            disabledMouseCursor: disabledCursor,
            visualDensity: density,
            animationDuration: duration,
            enableFeedback: false,
            backgroundBuilder: backgroundBuilder,
            foregroundBuilder: foregroundBuilder);

        Assert.Same(enabledCursor, style.MouseCursor!.Resolve(MaterialState.None));
        Assert.Same(disabledCursor, style.MouseCursor.Resolve(MaterialState.Disabled));
        Assert.Equal(density, style.VisualDensity);
        Assert.Equal(duration, style.AnimationDuration);
        Assert.False(style.EnableFeedback);
        Assert.Same(backgroundBuilder, style.BackgroundBuilder);
        Assert.Same(foregroundBuilder, style.ForegroundBuilder);
    }

    [Fact]
    public void FilledButton_IconUsesWidgetForegroundBeforeGeneratedDefaultIconColor()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FilledButton.Icon(
                    onPressed: () => { },
                    style: FilledButton.StyleFrom(foregroundColor: Colors.OrangeRed),
                    icon: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    label: new Text("Foreground icon"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedIconTheme);
        Assert.Equal(Colors.OrangeRed, capturedIconTheme!.Color);
    }

    [Fact]
    public void FilledButton_LayerBuildersReceiveStatesAndBackgroundCanDropForeground()
    {
        var owner = new BuildOwner();
        var statesController = new MaterialStatesController(MaterialState.Focused);
        MaterialState foregroundStates = MaterialState.None;
        MaterialState backgroundStates = MaterialState.None;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: () => { },
                    statesController: statesController,
                    style: FilledButton.StyleFrom(
                        foregroundBuilder: (_, states, _) =>
                        {
                            foregroundStates = states;
                            return new Text("Foreground replacement");
                        },
                        backgroundBuilder: (_, states, _) =>
                        {
                            backgroundStates = states;
                            return new SizedBox(width: 12, height: 12);
                        }),
                    child: new Text("Original"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(foregroundStates.HasFlag(MaterialState.Focused));
        Assert.True(backgroundStates.HasFlag(MaterialState.Focused));
        Assert.Null(FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement)));
    }

    [Fact]
    public void FilledButton_TextAndIconColorsAnimateOverConfiguredDuration()
    {
        var owner = new BuildOwner();
        var statesController = new MaterialStatesController();
        IconThemeData? capturedIconTheme = null;
        MaterialStateProperty<Color?> color = MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Focused) ? Colors.White : Colors.Black);
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: () => { },
                    statesController: statesController,
                    style: new ButtonStyle(
                        ForegroundColor: color,
                        IconColor: color,
                        AnimationDuration: TimeSpan.FromMilliseconds(200)),
                    child: new Row(
                        children:
                        [
                            new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                            new Text("Animated")
                        ]))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(Colors.Black, capturedIconTheme!.Color);
        double now = Scheduler.CurrentSeconds;
        statesController.Update(MaterialState.Focused, true);
        owner.FlushBuild();
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.1));
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        var animatedTextColor = Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color;
        Assert.InRange(animatedTextColor.R, (byte)1, (byte)254);
        // Dart animates the text through `Material.animationDuration` and the icon through its
        // own theme animation, so both are mid-flight here without being frame-locked together.
        Assert.InRange(capturedIconTheme!.Color!.Value.R, (byte)1, (byte)254);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        owner.FlushBuild();
        paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
        Assert.Equal(Colors.White, capturedIconTheme!.Color);
        root.Unmount();
    }

    [Fact]
    public void OutlinedButton_StyleFromCarriesCursorDensityTimingAndFeedback()
    {
        var enabledCursor = new SystemMouseCursor("outlined-enabled");
        var disabledCursor = new SystemMouseCursor("outlined-disabled");
        var density = new VisualDensity(horizontal: -2, vertical: 1);
        TimeSpan duration = TimeSpan.FromMilliseconds(350);
        ButtonStyle style = OutlinedButton.StyleFrom(
            enabledMouseCursor: enabledCursor,
            disabledMouseCursor: disabledCursor,
            visualDensity: density,
            animationDuration: duration,
            enableFeedback: false);

        Assert.Same(enabledCursor, style.MouseCursor!.Resolve(MaterialState.None));
        Assert.Same(disabledCursor, style.MouseCursor.Resolve(MaterialState.Disabled));
        Assert.Equal(density, style.VisualDensity);
        Assert.Equal(duration, style.AnimationDuration);
        Assert.False(style.EnableFeedback);
    }

    [Fact]
    public void TextButton_StyleFrom_IconColorAndSizeOverrideDefaults()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        iconColor: Colors.Gold,
                        iconSize: 26),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Gold, capturedTheme!.Color);
        Assert.Equal(26, capturedTheme.Size);
    }

    [Fact]
    public void TextButton_StyleFrom_DisabledIconColorOverridesForegroundFallback()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: null,
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        iconColor: Colors.Gold,
                        disabledIconColor: Colors.Gray),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Gray, capturedTheme!.Color);
        Assert.Equal(18, capturedTheme.Size);
    }

    [Fact]
    public void TextButton_StyleFrom_IconColorWithoutDisabledIcon_UsesIconColorWhenDisabled()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: null,
                    style: TextButton.StyleFrom(
                        iconColor: Colors.Gold),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Gold, capturedTheme!.Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_IconColorWithoutDisabledIcon_FallsBackToDefaultDisabledIcon()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: null,
                    style: ElevatedButton.StyleFrom(iconColor: Colors.Gold),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        // Dart's `ElevatedButton.styleFrom` builds `iconColor` with `defaultColor`, which resolves null when
        // disabled and no `disabledIconColor` was given, so the default disabled colour wins.
        Assert.NotNull(capturedTheme);
        Assert.Equal(
            ApplyOpacity(ThemeData.Light.ColorScheme.OnSurface, 0.38),
            capturedTheme!.Color);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_IconColorWithoutDisabledIcon_FallsBackToDefaultDisabledIcon()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: null,
                    style: OutlinedButton.StyleFrom(iconColor: Colors.Gold),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        // Dart's `OutlinedButton.styleFrom` builds `iconColor` with `defaultColor`, which resolves null when
        // disabled and no `disabledIconColor` was given, so the default disabled colour wins.
        Assert.NotNull(capturedTheme);
        Assert.Equal(
            ApplyOpacity(ThemeData.Light.ColorScheme.OnSurface, 0.38),
            capturedTheme!.Color);
    }

    [Fact]
    public void FilledButton_StyleFrom_IconColorWithoutDisabledIcon_FallsBackToDefaultDisabledIcon()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.DarkSlateBlue)
                },
                child: new FilledButton(
                    onPressed: null,
                    style: FilledButton.StyleFrom(iconColor: Colors.Gold),
                    child: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(ApplyOpacity(Colors.DarkSlateBlue, 0.38), capturedTheme!.Color);
    }

    [Fact]
    public void TextButton_DefaultMinSize_UsesMaterialBaseline64x40()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Min size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void TextButton_DefaultMinSize_UseMaterial3Disabled_UsesMaterialBaseline64x36()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Min size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(36, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void TextButton_DefaultPadding_UseMaterial3Disabled_UsesAll8()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(8), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void TextButton_Icon_DefaultPadding_UsesStart12TopBottom8End16()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: TextButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 8, 16, 8), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void TextButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesAll8()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: TextButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(8), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void TextButton_DefaultPadding_TextScaleFactor2_UsesHorizontal8AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(8, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void TextButton_Icon_DefaultPadding_TextScaleFactor2_UsesHorizontal4AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: TextButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(4, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void TextButton_Icon_DefaultPadding_Rtl_UsesDirectionalStartEnd()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Theme(
                    data: ThemeData.Light,
                    child: TextButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 8, 12, 8), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void TextButton_Icon_DefaultSpacing_Uses8()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: TextButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Spacing"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var row = FindDescendant<RenderFlex>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(row);
        Assert.Equal(8.0, row!.Spacing, 3);
    }

    [Fact]
    public void TextButton_Icon_TextScaleFactor15_UsesInterpolatedSpacing6()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 1.5),
                child: new Theme(
                    data: ThemeData.Light,
                    child: TextButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Spacing")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var row = FindDescendant<RenderFlex>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(row);
        Assert.Equal(6.0, row!.Spacing, 3);
    }

    [Fact]
    public void TextButton_Icon_TextScaleFactor3_ClampsSpacingTo4()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 3.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: TextButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Spacing")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var row = FindDescendant<RenderFlex>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(row);
        Assert.Equal(4.0, row!.Spacing, 3);
    }

    [Fact]
    public void TextButton_Icon_StyleTextSize28_UsesClampedSpacing4()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: TextButton.Icon(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        textStyle: new TextStyle(FontSize: 28)),
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Spacing"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var row = FindDescendant<RenderFlex>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(row);
        Assert.Equal(4.0, row!.Spacing, 3);
    }

    [Fact]
    public void TextButton_Icon_IconAlignmentEnd_PlacesLabelBeforeIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: TextButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Icon alignment"),
                    iconAlignment: IconAlignment.End)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void TextButton_Icon_StyleFromIconAlignmentEnd_PlacesLabelBeforeIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: TextButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Icon alignment"),
                    style: TextButton.StyleFrom(iconAlignment: IconAlignment.End))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void TextButton_Icon_ThemeIconAlignmentEnd_PlacesLabelBeforeIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButtonTheme(
                    data: new TextButtonThemeData(
                        style: new ButtonStyle(IconAlignment: IconAlignment.End)),
                    child: TextButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Icon alignment")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void TextButton_Icon_IconAlignmentParameter_OverridesStyleFromIconAlignment()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: TextButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Icon alignment"),
                    style: TextButton.StyleFrom(iconAlignment: IconAlignment.Start),
                    iconAlignment: IconAlignment.End)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void TextButton_Icon_IconAlignmentStart_Rtl_KeepsIconFirstInTheRow()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Theme(
                    data: ThemeData.Light,
                    child: TextButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Icon alignment"),
                        iconAlignment: IconAlignment.Start))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            // Dart puts the icon first in the `Row` for `IconAlignment.start` in both
            // directions and lets the row's own direction do the RTL mirroring.
            iconFirst: true);
    }

    [Fact]
    public void TextButton_Icon_IconAlignmentEnd_Rtl_KeepsLabelFirstInTheRow()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Theme(
                    data: ThemeData.Light,
                    child: TextButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Icon alignment"),
                        iconAlignment: IconAlignment.End))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void TextButton_TapTargetPadding_RedirectsHitTestInPaddedAreaToChildCenter()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    TextDirection.Ltr,
                    new SizedBox(
                        width: 120,
                        child: new TextButton(
                            onPressed: () => { },
                            child: new Text("Tap target"))))));

        harness.Pump(new Size(220, 120));

        var padding = FindDescendant<RenderInputPadding>(harness.RenderView);
        Assert.NotNull(padding);

        // Dart's `_RenderInputPadding.hitTest` re-dispatches a miss inside the 48px tap target to
        // the child's exact centre, so the ink response is still hit at y == 1.
        var hitResult = new BoxHitTestResult();
        Assert.True(harness.RenderView.HitTest(hitResult, new Point(60, 1)));
        Assert.Contains(hitResult.Path, entry => entry.Target is RenderInkResponsePaint);

        var missResult = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(missResult, new Point(60, 90)));
    }

    [Fact]
    public void TextButton_ThemeMaterialTapTargetSizeShrinkWrap_DoesNotExpandTapTarget()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap },
                child: new SizedBox(
                    width: 120,
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Tap target")))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void TextButton_StyleFrom_BackgroundOnly_AppliesBackgroundWhenDisabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: null,
                    style: TextButton.StyleFrom(
                        backgroundColor: Colors.Gold),
                    child: new Text("Disabled text background"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_StyleFromTapTargetSize_OverridesThemeTapTargetSize()
    {
        using (var paddedHarness = new WidgetRenderHarness(
                   new Theme(
                       data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.Padded },
                       child: new SizedBox(
                           width: 120,
                           child: new TextButton(
                               onPressed: () => { },
                               child: new Text("Tap target"))))))
        {
            paddedHarness.Pump(new Size(220, 120));
            var paddedHitResult = new BoxHitTestResult();
            Assert.True(paddedHarness.RenderView.HitTest(paddedHitResult, new Point(60, 46)));
        }

        using var overrideHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.Padded },
                child: new SizedBox(
                    width: 120,
                    child: new TextButton(
                        onPressed: () => { },
                        style: TextButton.StyleFrom(tapTargetSize: MaterialTapTargetSize.ShrinkWrap),
                        child: new Text("Tap target")))));

        overrideHarness.Pump(new Size(220, 120));

        var overrideHitResult = new BoxHitTestResult();
        Assert.False(overrideHarness.RenderView.HitTest(overrideHitResult, new Point(60, 46)));
    }

    [Fact]
    public void ElevatedButton_DefaultPadding_UsesHorizontal24AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(24, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void ElevatedButton_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void ElevatedButton_Icon_DefaultPadding_UsesStart16AndEnd24()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: ElevatedButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0, 24, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void ElevatedButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesStart12AndEnd16()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: ElevatedButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0, 16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void ElevatedButton_DefaultPadding_TextScaleFactor2_UsesHorizontal12()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: new ElevatedButton(
                        onPressed: () => { },
                        child: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void ElevatedButton_Icon_DefaultPadding_TextScaleFactor2_UsesStart8End12()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: ElevatedButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(8, 0, 12, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void ElevatedButton_Icon_DefaultPadding_TextScaleFactor2_Rtl_UsesDirectionalStartEnd()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new MediaQuery(
                    data: new MediaQueryData(TextScaleFactor: 2.0),
                    child: new Theme(
                        data: ThemeData.Light,
                        child: ElevatedButton.Icon(
                            onPressed: () => { },
                            icon: new SizedBox(width: 12, height: 12),
                            label: new Text("Padding"))))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0, 8, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void ElevatedButton_Icon_ThemeIconAlignmentEnd_PlacesLabelBeforeIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButtonTheme(
                    data: new ElevatedButtonThemeData(
                        style: new ButtonStyle(IconAlignment: IconAlignment.End)),
                    child: ElevatedButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Icon alignment")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void ElevatedButton_DefaultMinSize_UseMaterial3Disabled_UsesMaterialBaseline64x36()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Min size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(36, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void OutlinedButton_DefaultPadding_UsesHorizontal24AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(24, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void OutlinedButton_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void OutlinedButton_Icon_DefaultPadding_UsesStart16AndEnd24()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: OutlinedButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0, 24, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void OutlinedButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: OutlinedButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void OutlinedButton_DefaultPadding_TextScaleFactor2_UsesHorizontal12()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: new OutlinedButton(
                        onPressed: () => { },
                        child: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void OutlinedButton_Icon_DefaultPadding_TextScaleFactor2_UsesStart8End12()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: OutlinedButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(8, 0, 12, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void OutlinedButton_Icon_DefaultPadding_TextScaleFactor2_UseMaterial3Disabled_UsesHorizontal8()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light with { UseMaterial3 = false },
                    child: OutlinedButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(8, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void OutlinedButton_Icon_ThemeIconAlignmentEnd_PlacesLabelBeforeIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButtonTheme(
                    data: new OutlinedButtonThemeData(
                        style: new ButtonStyle(IconAlignment: IconAlignment.End)),
                    child: OutlinedButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Icon alignment")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void OutlinedButton_DefaultMinSize_UseMaterial3Disabled_UsesMaterialBaseline64x36()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Min size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(36, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void FilledButton_DefaultPadding_UsesHorizontal24AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(24, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButton_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new FilledButton(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButtonTonal_DefaultPadding_UseMaterial3Disabled_UsesHorizontal16AndZeroVertical()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: FilledButton.Tonal(
                    onPressed: () => { },
                    child: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButton_Icon_DefaultPadding_UsesStart16AndEnd24()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FilledButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(16, 0, 24, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButton_Icon_DefaultPadding_UseMaterial3Disabled_UsesStart12AndEnd16()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: FilledButton.Icon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0, 16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButtonTonal_Icon_DefaultPadding_UseMaterial3Disabled_UsesStart12AndEnd16()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: FilledButton.TonalIcon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Padding"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0, 16, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButton_DefaultPadding_TextScaleFactor2_UsesHorizontal12()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: new FilledButton(
                        onPressed: () => { },
                        child: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButton_Icon_DefaultPadding_TextScaleFactor2_UsesStart8End12()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(TextScaleFactor: 2.0),
                child: new Theme(
                    data: ThemeData.Light,
                    child: FilledButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Padding")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(8, 0, 12, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButton_Icon_DefaultPadding_TextScaleFactor2_Rtl_UsesDirectionalStartEnd()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new MediaQuery(
                    data: new MediaQueryData(TextScaleFactor: 2.0),
                    child: new Theme(
                        data: ThemeData.Light,
                        child: FilledButton.Icon(
                            onPressed: () => { },
                            icon: new SizedBox(width: 12, height: 12),
                            label: new Text("Padding"))))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var padding = FindDescendant<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(padding);
        Assert.Equal(new Thickness(12, 0, 8, 0), padding!.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr));
    }

    [Fact]
    public void FilledButtonTonal_Icon_IconAlignmentEnd_PlacesLabelBeforeIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FilledButton.TonalIcon(
                    onPressed: () => { },
                    icon: new SizedBox(width: 12, height: 12),
                    label: new Text("Icon alignment"),
                    iconAlignment: IconAlignment.End)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void FilledButton_Icon_ThemeIconAlignmentEnd_PlacesLabelBeforeIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FilledButtonTheme(
                    data: new FilledButtonThemeData(
                        style: new ButtonStyle(IconAlignment: IconAlignment.End)),
                    child: FilledButton.Icon(
                        onPressed: () => { },
                        icon: new SizedBox(width: 12, height: 12),
                        label: new Text("Icon alignment")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        AssertIconRowOrder(
            RequireRenderObject<RenderObject>(root.ChildElement),
            iconFirst: false);
    }

    [Fact]
    public void TextButton_DefaultTextStyle_UsesLabelLargeTypography()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Typography"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(14, paragraph!.FontSize);
        Assert.Equal(FontWeight.Medium, paragraph.FontWeight);
        Assert.Equal(1.43, paragraph.Height);
        Assert.Equal(0.1, paragraph.LetterSpacing);
    }

    [Fact]
    public void TextButton_TextStyleColor_DoesNotOverrideForegroundColor()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        textStyle: new TextStyle(Color: Colors.Crimson)),
                    child: new Text("Foreground precedence"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_M3Defaults_ReadColorSchemeRolesDirectly()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                primary: Colors.DarkSlateBlue,
                surfaceContainerLow: Colors.LightCyan,
                shadow: Colors.DarkGreen)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Primary"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(theme.ColorScheme.SurfaceContainerLow, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(theme.ColorScheme.Primary, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
        Assert.Equal(ApplyOpacity(Colors.DarkGreen, 0.20), RequirePrimaryShadow(decorated).Color);
    }

    [Fact]
    public void ElevatedButton_M2Defaults_ReadColorSchemeRolesDirectly()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                primary: Colors.DarkSlateBlue,
                onPrimary: Colors.LightCyan)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Primary"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(theme.ColorScheme.Primary, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(theme.ColorScheme.OnPrimary, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_SurfaceTintColor_TintsBackgroundByElevation()
    {
        var owner = new BuildOwner();
        var baseBackground = Colors.White;
        var surfaceTint = Colors.Red;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: ElevatedButton.StyleFrom(
                        backgroundColor: baseBackground,
                        surfaceTintColor: surfaceTint,
                        elevation: 3),
                    child: new Text("Surface tint"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(ApplySurfaceTint(baseBackground, surfaceTint, 3), decorated!.Decoration.Color);
    }

    [Fact]
    public void ElevatedButton_ThemeStyleSurfaceTintColor_TintsDefaultBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ElevatedButtonTheme = new ElevatedButtonThemeData(
                style: new ButtonStyle(
                    SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Red)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Theme surface tint"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(
            ApplySurfaceTint(theme.ColorScheme.SurfaceContainerLow, Colors.Red, 1),
            decorated!.Decoration.Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_SurfaceTintColor_DoesNotTintBackground_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var baseBackground = Colors.White;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: ElevatedButton.StyleFrom(
                        backgroundColor: baseBackground,
                        surfaceTintColor: Colors.Red,
                        elevation: 3),
                    child: new Text("Surface tint off"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(baseBackground, decorated!.Decoration.Color);
    }

    [Fact]
    public void ElevatedButton_ThemeStyleSurfaceTintColor_DoesNotTintBackground_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ElevatedButtonTheme = new ElevatedButtonThemeData(
                style: new ButtonStyle(
                    SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Red)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: ElevatedButton.StyleFrom(
                        backgroundColor: Colors.White,
                        elevation: 3),
                    child: new Text("Theme surface tint off"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.White, decorated!.Decoration.Color);
    }

    [Fact]
    public void ElevatedButton_DefaultShadow_IsAppliedWhenEnabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Shadow"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        IReadOnlyList<Plumix.Rendering.BoxShadow>? shadows = decorated!.Decoration.BoxShadows;
        Assert.NotNull(shadows);
        Assert.True(shadows.Count > 0);
    }

    [Fact]
    public void ElevatedButton_DisabledState_DoesNotApplyShadow()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: null,
                    child: new Text("Disabled shadow"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Null(decorated!.Decoration.BoxShadows);
    }

    [Fact]
    public void FilledButton_DefaultElevation_Hovered_UsesOneAndDefaultUsesZero()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: () => { },
                    style: new ButtonStyle(AnimationDuration: TimeSpan.Zero),
                    child: new Text("Filled elevation"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var defaultDecorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        Assert.NotNull(defaultDecorated);
        Assert.Null(defaultDecorated!.Decoration.BoxShadows);

        var hoverListener = FindHoverPointerListener(renderRoot);
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 104,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));
        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        var hoveredShadow = RequirePrimaryShadow(hoveredDecorated!);
        Assert.Equal(1, hoveredShadow.Offset.Y);
    }

    [Fact]
    public void ElevatedButton_DefaultElevation_UseMaterial3Disabled_UsesMaterial2StateMap()
    {
        // Dart's `ElevatedButton.styleFrom` turns a single elevation into disabled 0, pressed +6,
        // hovered/focused +2 and the plain value otherwise; the Material 2 default feeds it 2.
        ButtonStyle style = ElevatedButton.StyleFrom(elevation: 2);

        Assert.Equal(0.0, style.Elevation!.Resolve(MaterialState.Disabled));
        Assert.Equal(8.0, style.Elevation.Resolve(MaterialState.Pressed));
        Assert.Equal(4.0, style.Elevation.Resolve(MaterialState.Hovered));
        Assert.Equal(4.0, style.Elevation.Resolve(MaterialState.Focused));
        Assert.Equal(2.0, style.Elevation.Resolve(MaterialState.None));
    }

    [Fact]
    public void TextButton_StyleFrom_ElevationAndShadowColor_AppliesShadow()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        backgroundColor: Colors.White,
                        shadowColor: Colors.Black,
                        elevation: 3),
                    child: new Text("Text shadow"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.NotNull(decorated!.Decoration.BoxShadows);
        Assert.True(decorated.Decoration.BoxShadows!.Count > 0);
    }

    [Fact]
    public void TextButton_StyleFrom_ElevationWithoutShadowColor_DoesNotApplyShadowInMaterial3()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { ShadowColor = Colors.Black },
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        backgroundColor: Colors.White,
                        elevation: 2),
                    child: new Text("Text shadow fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Null(decorated!.Decoration.BoxShadows);
    }

    [Fact]
    public void TextButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallbackInMaterial2()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    UseMaterial3 = false,
                    ShadowColor = Colors.Black
                },
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        backgroundColor: Colors.White,
                        elevation: 2),
                    child: new Text("Text shadow fallback M2"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.NotNull(decorated!.Decoration.BoxShadows);
        Assert.True(decorated.Decoration.BoxShadows!.Count > 0);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_ElevationAndShadowColor_AppliesShadow()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: OutlinedButton.StyleFrom(
                        backgroundColor: Colors.White,
                        shadowColor: Colors.Black,
                        elevation: 2),
                    child: new Text("Outlined shadow"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.NotNull(decorated!.Decoration.BoxShadows);
        Assert.True(decorated.Decoration.BoxShadows!.Count > 0);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_ElevationWithoutShadowColor_DoesNotApplyShadowInMaterial3()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { ShadowColor = Colors.Black },
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: OutlinedButton.StyleFrom(
                        backgroundColor: Colors.White,
                        elevation: 2),
                    child: new Text("Outlined shadow fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Null(decorated!.Decoration.BoxShadows);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallbackInMaterial2()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    UseMaterial3 = false,
                    ShadowColor = Colors.Black
                },
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: OutlinedButton.StyleFrom(
                        backgroundColor: Colors.White,
                        elevation: 2),
                    child: new Text("Outlined shadow fallback M2"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.NotNull(decorated!.Decoration.BoxShadows);
        Assert.True(decorated.Decoration.BoxShadows!.Count > 0);
    }

    [Fact]
    public void FilledButton_StyleFrom_ElevationAndShadowColor_AppliesShadow()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FilledButton(
                    onPressed: () => { },
                    style: FilledButton.StyleFrom(
                        shadowColor: Colors.Black,
                        elevation: 2),
                    child: new Text("Filled shadow"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.NotNull(decorated!.Decoration.BoxShadows);
        Assert.True(decorated.Decoration.BoxShadows!.Count > 0);
    }

    [Fact]
    public void FilledButton_StyleFrom_ElevationWithoutShadowColor_UsesThemeShadowColorFallback()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { ShadowColor = Colors.Black },
                child: new FilledButton(
                    onPressed: () => { },
                    style: FilledButton.StyleFrom(elevation: 2),
                    child: new Text("Filled shadow fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.NotNull(decorated!.Decoration.BoxShadows);
        Assert.True(decorated.Decoration.BoxShadows!.Count > 0);
    }

    [Fact]
    public void OutlinedButton_M3Defaults_ReadColorSchemeRolesDirectly()
    {
        FocusManager.Instance.ResetForTests();
        var owner = new BuildOwner();
        var focusNode = new FocusNode();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                primary: Colors.MediumVioletRed,
                outline: Colors.CadetBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    style: new ButtonStyle(AnimationDuration: TimeSpan.Zero),
                    child: new Text("Outline"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(MaterialColors.Transparent, decorated!.Decoration.Color);
        Assert.Equal(Plumix.Rendering.Border.FromBorderSide(
            new BorderSide(theme.ColorScheme.Outline, 1)), decorated.Decoration.Border);
        Assert.NotNull(paragraph);
        Assert.Equal(
            theme.ColorScheme.Primary,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);

        focusNode.RequestFocus();
        owner.FlushBuild();

        decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Plumix.Rendering.Border.FromBorderSide(
            new BorderSide(theme.ColorScheme.Primary, 1)), decorated!.Decoration.Border);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void OutlinedButton_DefaultBorder_UseMaterial3Disabled_UsesOnSurfaceOpacity()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.MidnightBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Outline m2 border"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(
            Plumix.Rendering.Border.FromBorderSide(new BorderSide(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), 1)),
            decorated!.Decoration.Border);
    }

    [Fact]
    public void OutlinedButton_FocusedBorder_UseMaterial3Disabled_StaysOnSurfaceOpacity()
    {
        var owner = new BuildOwner();
        var focusNode = new FocusNode();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                primary: Colors.MediumPurple,
                onSurface: Colors.MidnightBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    style: new ButtonStyle(AnimationDuration: TimeSpan.Zero),
                    child: new Text("Outline m2 focus border"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        focusNode.RequestFocus();
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(
            Plumix.Rendering.Border.FromBorderSide(new BorderSide(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), 1)),
            decorated!.Decoration.Border);
    }

    [Fact]
    public void OutlinedButton_DefaultForegroundUsesColorSchemePrimary()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.MediumVioletRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.DarkCyan)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Outline fg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(
            theme.ColorScheme.Primary,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void OutlinedButton_M2Defaults_ReadColorSchemePrimaryDirectly()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.MediumVioletRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.DarkCyan)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Outlined"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(
            theme.ColorScheme.Primary,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_BackgroundOnly_AppliesBackgroundWhenDisabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: null,
                    style: OutlinedButton.StyleFrom(
                        backgroundColor: Colors.Gold),
                    child: new Text("Disabled outlined background"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_BackgroundOnly_OverridesThemeDisabledBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OutlinedButtonTheme = new OutlinedButtonThemeData(
                new ButtonStyle(
                    BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Disabled) ? Colors.IndianRed : MaterialColors.Transparent)))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: null,
                    style: OutlinedButton.StyleFrom(backgroundColor: Colors.Gold),
                    child: new Text("Disabled outlined background override"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void FilledButton_UsesThemePrimaryAndOnPrimaryColorsByDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkSlateBlue,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                primary: Colors.DarkSlateBlue,
                onPrimary: Colors.AliceBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: () => { },
                    child: new Text("Filled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(theme.ColorScheme.Primary, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(
            theme.ColorScheme.OnPrimary,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButtonTonal_UsesThemeSecondaryContainerAndOnSecondaryContainerColorsByDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                secondaryContainer: Colors.Bisque,
                onSecondaryContainer: Colors.DarkSlateBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: FilledButton.Tonal(
                    onPressed: () => { },
                    child: new Text("Filled tonal"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(theme.ColorScheme.SecondaryContainer, decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(
            theme.ColorScheme.OnSecondaryContainer,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButton_DisabledStateUsesThemeOnSurfaceTones()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.DarkOliveGreen)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: null,
                    child: new Text("Disabled filled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(
            ApplyOpacity(theme.ColorScheme.OnSurface, 0.38),
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButton_StyleFrom_BackgroundOnly_DisabledFallsBackToThemeDisabledBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.DarkSlateGray)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: null,
                    style: FilledButton.StyleFrom(backgroundColor: Colors.SeaGreen),
                    child: new Text("Disabled filled background fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_ButtonStyleForegroundOverridesDefault()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen)),
                    child: new Text("Styled foreground"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.ForestGreen, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_ButtonStyleAlignmentOverridesDefaultCenter()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        Alignment: Alignment.TopLeft),
                    child: new Text("Aligned"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var align = FindDescendant<RenderPositionedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(align);
        Assert.Equal(Alignment.TopLeft, align!.Alignment);
    }

    [Fact]
    public void TextButton_ThemeStyleAlignmentOverridesDefaultCenter()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    Alignment: Alignment.BottomRight))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Theme aligned"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var align = FindDescendant<RenderPositionedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(align);
        Assert.Equal(Alignment.BottomRight, align!.Alignment);
    }

    [Fact]
    public void TextButton_WidgetStyleAlignmentOverridesThemeStyleAlignment()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    Alignment: Alignment.BottomRight))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(alignment: Alignment.CenterLeft),
                    child: new Text("Widget aligned"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var align = FindDescendant<RenderPositionedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(align);
        Assert.Equal(Alignment.CenterLeft, align!.Alignment);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleMinimumSizeOverridesDefault()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MinimumSize: MaterialStateProperty<Size?>.All(new Size(180, 56))),
                    child: new Text("Styled size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(180, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleMinimumSize_AllowsZero()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, 0))),
                    child: new Text("Zero min size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(0, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(0, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleMaximumSizeClampsDefaultInfinityMax()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MaximumSize: MaterialStateProperty<Size?>.All(new Size(120, 48))),
                    child: new Text("Max size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(120, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(48, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleFixedSizeSetsTightConstraints_WithinMaximum()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        MinimumSize: MaterialStateProperty<Size?>.All(new Size(64, 40)),
                        MaximumSize: MaterialStateProperty<Size?>.All(new Size(120, 48)),
                        FixedSize: MaterialStateProperty<Size?>.All(new Size(200, 80))),
                    child: new Text("Fixed size"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(120, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(120, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(48, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(48, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void TextButton_ButtonStyleFixedSizeInfiniteWidth_OnlyTightensFiniteAxis()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        FixedSize: MaterialStateProperty<Size?>.All(new Size(double.PositiveInfinity, 44))),
                    child: new Text("Fixed height only"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(64, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.True(double.IsPositiveInfinity(constrainedBox.AdditionalConstraints.MaxWidth));
        Assert.Equal(44, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(44, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleSideOverridesDefault()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Goldenrod, 2))),
                    child: new Text("Styled side"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Plumix.Rendering.Border.FromBorderSide(
            new BorderSide(Colors.Goldenrod, 2)), decorated!.Decoration.Border);
    }

    [Fact]
    public void TextButton_ThemeStyleForegroundOverridesDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
            TextButtonTheme = new TextButtonThemeData(style: new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Theme fg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_WidgetStyleForegroundOverridesThemeStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(style: new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen)),
                    child: new Text("Widget fg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.ForestGreen, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_ThemeStyleBackgroundOverridesDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(surfaceContainerLow: Colors.Bisque),
            ElevatedButtonTheme = new ElevatedButtonThemeData(style: new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Theme bg"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.MediumPurple, decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_ThemeStyleSideOverridesDefault()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(outline: Colors.CadetBlue),
            OutlinedButtonTheme = new OutlinedButtonThemeData(style: new ButtonStyle(
                Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Goldenrod, 3)))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Theme side"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Plumix.Rendering.Border.FromBorderSide(
            new BorderSide(Colors.Goldenrod, 3)), decorated!.Decoration.Border);
    }

    [Fact]
    public void TextButton_LocalThemeStyleForegroundOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
            TextButtonTheme = new TextButtonThemeData(style: new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButtonTheme(
                    data: new TextButtonThemeData(
                        style: new ButtonStyle(
                            ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen))),
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Local theme fg")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.ForestGreen, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_WidgetStyleForegroundOverridesLocalThemeStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(style: new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButtonTheme(
                    data: new TextButtonThemeData(
                        style: new ButtonStyle(
                            ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen))),
                    child: new TextButton(
                        onPressed: () => { },
                        style: new ButtonStyle(
                            ForegroundColor: MaterialStateProperty<Color?>.All(Colors.OrangeRed)),
                        child: new Text("Widget over local")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_LocalThemeNullStyle_DoesNotFallbackToThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
            TextButtonTheme = new TextButtonThemeData(style: new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkCyan))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButtonTheme(
                    data: new TextButtonThemeData(),
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Local clears theme")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_LocalThemeStyleBackgroundOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(surfaceContainerLow: Colors.Bisque),
            ElevatedButtonTheme = new ElevatedButtonThemeData(style: new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButtonTheme(
                    data: new ElevatedButtonThemeData(
                        style: new ButtonStyle(
                            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Gold))),
                    child: new ElevatedButton(
                        onPressed: () => { },
                        child: new Text("Local elevated bg")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_LocalThemeStyleSideOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(outline: Colors.CadetBlue),
            OutlinedButtonTheme = new OutlinedButtonThemeData(style: new ButtonStyle(
                Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Goldenrod, 3)))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButtonTheme(
                    data: new OutlinedButtonThemeData(
                        style: new ButtonStyle(
                            Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Colors.Crimson, 4)))),
                    child: new OutlinedButton(
                        onPressed: () => { },
                        child: new Text("Local outlined side")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Plumix.Rendering.Border.FromBorderSide(
            new BorderSide(Colors.Crimson, 4)), decorated!.Decoration.Border);
    }

    [Fact]
    public void FilledButton_LocalThemeStyleBackgroundOverridesThemeDataStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            FilledButtonTheme = new FilledButtonThemeData(style: new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
        )};

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButtonTheme(
                    data: new FilledButtonThemeData(
                        style: new ButtonStyle(
                            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Gold))),
                    child: new FilledButton(
                        onPressed: () => { },
                        child: new Text("Local filled bg")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Gold, decorated!.Decoration.Color);
    }

    [Fact]
    public void ButtonStyle_Merge_FillsNullFields_FromArgument_WithoutOverridingExisting()
    {
        var owner = new BuildOwner();
        var mergedStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.Crimson))
            .Merge(new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.DarkGreen),
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.LightGoldenrodYellow)));

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: mergedStyle,
                    child: new Text("Merge semantics"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Crimson, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
        Assert.NotNull(decorated);
        Assert.Equal(Colors.LightGoldenrodYellow, decorated!.Decoration.Color);
    }

    [Fact]
    public void ButtonStyle_MergeAndLerpPreserveLayerBuilders()
    {
        ButtonLayerBuilder firstBackground = (_, _, child) => child!;
        ButtonLayerBuilder firstForeground = (_, _, child) => child!;
        ButtonLayerBuilder secondBackground = (_, _, child) => child!;
        ButtonLayerBuilder secondForeground = (_, _, child) => child!;
        var first = new ButtonStyle(
            BackgroundBuilder: firstBackground,
            ForegroundBuilder: firstForeground);
        var second = new ButtonStyle(
            BackgroundBuilder: secondBackground,
            ForegroundBuilder: secondForeground);

        ButtonStyle merged = first.Merge(second);
        ButtonStyle lerpedBeforeMidpoint = Assert.IsType<ButtonStyle>(ButtonStyle.Lerp(first, second, 0.49));
        ButtonStyle lerpedAtMidpoint = Assert.IsType<ButtonStyle>(ButtonStyle.Lerp(first, second, 0.5));

        Assert.Same(firstBackground, merged.BackgroundBuilder);
        Assert.Same(firstForeground, merged.ForegroundBuilder);
        Assert.Same(firstBackground, lerpedBeforeMidpoint.BackgroundBuilder);
        Assert.Same(firstForeground, lerpedBeforeMidpoint.ForegroundBuilder);
        Assert.Same(secondBackground, lerpedAtMidpoint.BackgroundBuilder);
        Assert.Same(secondForeground, lerpedAtMidpoint.ForegroundBuilder);
    }

    [Fact]
    public void TextButton_StyleFrom_AppliesForegroundAndTextStyle()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(
                        foregroundColor: Colors.DarkCyan,
                        textStyle: new TextStyle(
                            FontSize: 18,
                            FontWeight: FontWeight.Bold)),
                    child: new Text("StyleFrom"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.DarkCyan, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
        Assert.Equal(18, paragraph.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
    }

    [Fact]
    public void TextButton_WidgetTextStyleStateResolver_NullDisabled_FallsBackToThemeTextStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    TextStyle: MaterialStateProperty<TextStyle?>.All(
                        new TextStyle(FontWeight: FontWeight.Bold))))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: null,
                    style: new ButtonStyle(
                        TextStyle: MaterialStateProperty<TextStyle?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? null
                                : new TextStyle(FontSize: 18))),
                    child: new Text("Disabled text-style fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(14, paragraph!.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
    }

    [Fact]
    public void TextButton_WidgetTextStyleStateResolver_Enabled_OverridesThemeTextStyle()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            TextButtonTheme = new TextButtonThemeData(
                style: new ButtonStyle(
                    TextStyle: MaterialStateProperty<TextStyle?>.All(
                        new TextStyle(FontWeight: FontWeight.Bold))))
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        TextStyle: MaterialStateProperty<TextStyle?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? null
                                : new TextStyle(FontSize: 18))),
                    child: new Text("Enabled text-style"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(18, paragraph!.FontSize);
        // Dart resolves the first non-null layer and hands it to `Material.textStyle` whole; it
        // never merges the default's weight underneath.
        Assert.Equal(FontWeight.Normal, paragraph.FontWeight);
    }

    [Fact]
    public void TextButton_StyleFrom_ForegroundColor_DerivesOverlayAndSplash()
    {
        Color styleColor = Colors.DarkCyan;
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                style: TextButton.StyleFrom(foregroundColor: styleColor),
                child: new Text("StyleFrom states")));

        // Dart derives the overlay table from `overlayColor ?? foregroundColor`: pressed 0.1,
        // hovered 0.08, focused 0.1, and nothing at rest.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(styleColor, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), InkHighlight(harness));

        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), splash!.SplashColor);
    }

    [Fact]
    public void TextButton_StyleFrom_TransparentOverlay_DisablesVisualHighlights()
    {
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                style: TextButton.StyleFrom(overlayColor: MaterialColors.Transparent),
                child: new Text("Transparent overlay")));

        // Dart's `styleFrom` passes a fully transparent overlay through verbatim, which defeats
        // every highlight instead of deriving the 0.1/0.08/0.1 table from it.
        HoverButton(harness, enter: true);
        Assert.Equal(MaterialColors.Transparent, InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(MaterialColors.Transparent, InkHighlight(harness));
        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(MaterialColors.Transparent, splash!.SplashColor);
    }

    [Fact]
    public void TextButton_StyleFrom_OverlayColor_UsesStateOpacitiesAndSplashFallback()
    {
        Color styleColor = Colors.DarkCyan;
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                style: TextButton.StyleFrom(overlayColor: styleColor),
                child: new Text("StyleFrom states")));

        // Dart derives the overlay table from `overlayColor ?? foregroundColor`: pressed 0.1,
        // hovered 0.08, focused 0.1, and nothing at rest.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(styleColor, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), InkHighlight(harness));

        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), splash!.SplashColor);
    }

    [Fact]
    public void TextButton_ButtonStyleOverlayAll_DoesNotTintAtRest_ButAppliesOnHover()
    {
        var owner = new BuildOwner();
        var overlayColor = Colors.HotPink;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.All(overlayColor)),
                    child: new Text("Overlay all"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.HighlightColor);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 25,
                kind: PointerDeviceKind.Mouse,
                position: new Point(11, 10),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(11, 10)));

        owner.FlushBuild();

        var hoveredDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(overlayColor, hoveredDecorated!.HighlightColor);
    }

    [Fact]
    public void TextButton_SplashKeepsOverlayTint_AfterPointerUp()
    {
        Color overlayColor = Colors.Crimson;
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                style: new ButtonStyle(OverlayColor: MaterialStateProperty<Color?>.All(overlayColor)),
                child: new Text("Splash tint")));

        PressButton(harness);
        RenderInkResponsePaint? pressed = FindInkPaint(harness.RenderView);
        Assert.NotNull(pressed);
        Assert.Equal(overlayColor, pressed!.SplashColor);

        // The splash keeps its colour while it fades out after the pointer is released.
        ReleaseButton(harness);
        RenderInkResponsePaint? released = FindInkPaint(harness.RenderView);
        Assert.NotNull(released);
        Assert.Equal(overlayColor, released!.SplashColor);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_UsesDisabledColorOverrides()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new ElevatedButton(
                    onPressed: null,
                    style: ElevatedButton.StyleFrom(
                        foregroundColor: Colors.White,
                        backgroundColor: Colors.SeaGreen,
                        disabledForegroundColor: Colors.SlateGray,
                        disabledBackgroundColor: Colors.SaddleBrown),
                    child: new Text("Disabled styleFrom"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);
        Assert.NotNull(decorated);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.SaddleBrown, decorated!.Decoration.Color);
        Assert.Equal(Colors.SlateGray, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void TextButton_StyleFrom_ForegroundOnly_DisabledFallsBackToThemeDisabledForeground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.MidnightBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: null,
                    style: TextButton.StyleFrom(foregroundColor: Colors.LimeGreen),
                    child: new Text("Disabled foreground fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(
            ApplyOpacity(theme.ColorScheme.OnSurface, 0.38),
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_BackgroundOnly_DisabledFallsBackToThemeDisabledBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.MidnightBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: null,
                    style: ElevatedButton.StyleFrom(backgroundColor: Colors.SeaGreen),
                    child: new Text("Disabled background fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_StyleFrom_DisabledForegroundOnly_PreservesEnabledThemeForeground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkCyan,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.Crimson)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: TextButton.StyleFrom(disabledForegroundColor: Colors.DimGray),
                    child: new Text("Enabled foreground"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(theme.ColorScheme.Primary, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_StyleFrom_DisabledBackgroundOnly_PreservesEnabledThemeBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(surfaceContainerLow: Colors.LightCyan)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: ElevatedButton.StyleFrom(disabledBackgroundColor: Colors.SaddleBrown),
                    child: new Text("Enabled background"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(theme.ColorScheme.SurfaceContainerLow, decorated!.Decoration.Color);
    }

    [Fact]
    public void TextButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkCyan,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.MediumPurple)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? Colors.Gray : null)),
                    child: new Text("Resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(theme.ColorScheme.Primary, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.MediumPurple)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? Colors.Gray : null)),
                    child: new Text("Elevated fg fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(theme.ColorScheme.Primary, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.MediumPurple)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? Colors.Gray : null)),
                    child: new Text("Outlined fg fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(
            theme.ColorScheme.Primary,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void FilledButton_ButtonStyleForegroundResolverNullForEnabled_FallsBackToDefaultEnabledColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onPrimary: Colors.DarkGoldenrod)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FilledButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? Colors.Gray : null)),
                    child: new Text("Filled fg fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(
            theme.ColorScheme.OnPrimary,
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleBackgroundResolverNullForDisabled_FallsBackToDefaultDisabledBackground()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.MidnightBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: null,
                    style: new ButtonStyle(
                        BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled) ? null : Colors.SeaGreen)),
                    child: new Text("Background resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), decorated!.Decoration.Color);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleSideResolverNullForEnabled_FallsBackToDefaultEnabledSide()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(outline: Colors.CadetBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    style: new ButtonStyle(
                        Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? new BorderSide(Colors.DarkGray, 3)
                                : null)),
                    child: new Text("Side resolver fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Plumix.Rendering.Border.FromBorderSide(
            new BorderSide(theme.ColorScheme.Outline, 1)), decorated!.Decoration.Border);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleSideResolverNullForDisabled_FallsBackToDefaultDisabledSide()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.DarkOliveGreen)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: null,
                    style: new ButtonStyle(
                        Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Disabled)
                                ? null
                                : new BorderSide(Colors.Goldenrod, 2))),
                    child: new Text("Disabled side fallback"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(
            Plumix.Rendering.Border.FromBorderSide(new BorderSide(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), 1)),
            decorated!.Decoration.Border);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        Color pressedOverlay = Colors.YellowGreen;
        ThemeData theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
        };
        using WidgetRenderHarness harness = PumpButton(
            new ElevatedButton(
                onPressed: () => { },
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                child: new Text("Overlay resolver fallback")),
            theme);

        // Dart resolves each layer's `WidgetStateProperty` first and only then falls through, so a
        // resolver that yields null for the hovered state still gets the default hover overlay.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.Primary, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void OutlinedButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        Color pressedOverlay = Colors.YellowGreen;
        ThemeData theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
        };
        using WidgetRenderHarness harness = PumpButton(
            new OutlinedButton(
                onPressed: () => { },
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                child: new Text("Overlay resolver fallback")),
            theme);

        // Dart resolves each layer's `WidgetStateProperty` first and only then falls through, so a
        // resolver that yields null for the hovered state still gets the default hover overlay.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.Primary, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void ElevatedButton_ThemeStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        Color pressedOverlay = Colors.YellowGreen;
        ThemeData theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
            ElevatedButtonTheme = new ElevatedButtonThemeData(
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null))),
        };
        using WidgetRenderHarness harness = PumpButton(
            new ElevatedButton(
                onPressed: () => { },
                child: new Text("Overlay resolver fallback")),
            theme);

        // Dart resolves each layer's `WidgetStateProperty` first and only then falls through, so a
        // resolver that yields null for the hovered state still gets the default hover overlay.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.Primary, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void TextButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        Color pressedOverlay = Colors.YellowGreen;
        ThemeData theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
        };
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                child: new Text("Overlay resolver fallback")),
            theme);

        // Dart resolves each layer's `WidgetStateProperty` first and only then falls through, so a
        // resolver that yields null for the hovered state still gets the default hover overlay.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.Primary, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void FilledButton_ButtonStyleOverlayResolverNullForHover_FallsBackToDefaultOverlay()
    {
        Color pressedOverlay = Colors.YellowGreen;
        ThemeData theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.OrangeRed),
        };
        using WidgetRenderHarness harness = PumpButton(
            new FilledButton(
                onPressed: () => { },
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                child: new Text("Overlay resolver fallback")),
            theme);

        // Dart resolves each layer's `WidgetStateProperty` first and only then falls through, so a
        // resolver that yields null for the hovered state still gets the default hover overlay.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.OnPrimary, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void TextButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash()
    {
        Color overlayColor = Colors.Teal;
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                style: new ButtonStyle(OverlayColor: MaterialStateProperty<Color?>.All(overlayColor)),
                child: new Text("Overlay splash fallback")));

        PressButton(harness);

        // Dart's `InkResponse` takes the splash colour from `overlayColor` resolved for the current
        // states before falling back to `ThemeData.splashColor`.
        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(overlayColor, splash!.SplashColor);
    }

    [Fact]
    public void ElevatedButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash()
    {
        Color overlayColor = Colors.Teal;
        using WidgetRenderHarness harness = PumpButton(
            new ElevatedButton(
                onPressed: () => { },
                style: new ButtonStyle(OverlayColor: MaterialStateProperty<Color?>.All(overlayColor)),
                child: new Text("Overlay splash fallback")));

        PressButton(harness);

        // Dart's `InkResponse` takes the splash colour from `overlayColor` resolved for the current
        // states before falling back to `ThemeData.splashColor`.
        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(overlayColor, splash!.SplashColor);
    }

    [Fact]
    public void OutlinedButton_ButtonStyleOverlayWithoutSplash_UsesOverlayForSplash()
    {
        Color overlayColor = Colors.Teal;
        using WidgetRenderHarness harness = PumpButton(
            new OutlinedButton(
                onPressed: () => { },
                style: new ButtonStyle(OverlayColor: MaterialStateProperty<Color?>.All(overlayColor)),
                child: new Text("Overlay splash fallback")));

        PressButton(harness);

        // Dart's `InkResponse` takes the splash colour from `overlayColor` resolved for the current
        // states before falling back to `ThemeData.splashColor`.
        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(overlayColor, splash!.SplashColor);
    }

    [Fact]
    public void ElevatedButton_DisabledStateUsesThemeOnSurfaceTones()
    {
        var owner = new BuildOwner();
        var background = Colors.DarkGreen;
        var foreground = Colors.White;
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.MidnightBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new ElevatedButton(
                    onPressed: null,
                    style: ElevatedButton.StyleFrom(
                        backgroundColor: background,
                        foregroundColor: foreground,
                        disabledBackgroundColor: ApplyOpacity(theme.ColorScheme.OnSurface, 0.12),
                        disabledForegroundColor: ApplyOpacity(theme.ColorScheme.OnSurface, 0.38)),
                    child: new Text("Disabled"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.OnSurface, 0.12), decorated!.Decoration.Color);
        Assert.NotNull(paragraph);
        Assert.Equal(
            ApplyOpacity(theme.ColorScheme.OnSurface, 0.38),
            Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void ElevatedButton_PressedStateKeepsBackground_AndPaintsPressedOverlay()
    {
        Color background = Colors.SteelBlue;
        using WidgetRenderHarness harness = PumpButton(
            new ElevatedButton(
                onPressed: () => { },
                style: ElevatedButton.StyleFrom(backgroundColor: background) with
                {
                    AnimationDuration = TimeSpan.Zero,
                },
                child: new Text("Press")));

        RenderDecoratedBox? material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(background, material!.Decoration.Color);

        // Dart leaves the Material colour alone while pressed; the tint is the ink overlay.
        PressButton(harness);
        Assert.Equal(background, FindDescendant<RenderDecoratedBox>(harness.RenderView)!.Decoration.Color);
        Assert.NotNull(InkHighlight(harness));

        ReleaseButton(harness);
        Assert.Equal(background, FindDescendant<RenderDecoratedBox>(harness.RenderView)!.Decoration.Color);
    }

    [Fact]
    public void TextButton_HoverStateAppliesOverlayUntilExit()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.IndianRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.IndianRed)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Hover"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.HighlightColor);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 1,
                kind: PointerDeviceKind.Mouse,
                position: new Point(8, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(8, 8)));

        owner.FlushBuild();

        var hoveredDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), hoveredDecorated!.HighlightColor);

        hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerExitEvent(
                pointer: 1,
                kind: PointerDeviceKind.Mouse,
                position: new Point(120, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(120, 8)));

        owner.FlushBuild();

        var exitedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(exitedDecorated);
        Assert.Null(exitedDecorated!.HighlightColor);
    }

    [Fact]
    public void TextButton_PressedOverlayTakesPriorityOverHoverOverlay()
    {
        Color otherOverlay = Colors.CornflowerBlue;
        Color pressedOverlay = Colors.Gold;
        var focusNode = new FocusNode();
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                focusNode: focusNode,
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed)
                            ? pressedOverlay
                            : states.HasFlag(MaterialState.Hovered)
                                ? otherOverlay
                                : null)),
                child: new Text("Priority")));

        HoverButton(harness, enter: true);
        Assert.Equal(otherOverlay, InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void TextButton_HoverOverlayTakesPriorityOverFocusOverlay()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.MediumSeaGreen,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.MediumSeaGreen)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Focus hover"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 19,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(12, 9)));

        owner.FlushBuild();

        var focusedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.10), focusedDecorated!.HighlightColor);

        var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 19,
                kind: PointerDeviceKind.Mouse,
                position: new Point(12, 9),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(12, 9)));

        owner.FlushBuild();

        var focusedHoveredDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedHoveredDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.08), focusedHoveredDecorated!.HighlightColor);

        hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerExitEvent(
                pointer: 19,
                kind: PointerDeviceKind.Mouse,
                position: new Point(120, 9),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(120, 9)));

        owner.FlushBuild();

        var focusOnlyDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusOnlyDecorated);
        Assert.Equal(ApplyOpacity(theme.PrimaryColor, 0.10), focusOnlyDecorated!.HighlightColor);
    }

    [Fact]
    public void TextButton_KeyboardActivation_UsesPressedOverlay_AndInvokesOnPressedOnKeyDownOnly()
    {
        var owner = new BuildOwner();
        var focusedOverlay = Colors.SeaGreen;
        var pressedOverlay = Colors.OrangeRed;
        int pressedCount = 0;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => pressedCount += 1,
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        {
                            if (states.HasFlag(MaterialState.Pressed))
                            {
                                return pressedOverlay;
                            }

                            if (states.HasFlag(MaterialState.Focused))
                            {
                                return focusedOverlay;
                            }

                            return null;
                        })),
                    child: new Text("Keyboard pressed overlay"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 41,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(10, 8)));

        owner.FlushBuild();

        var focusedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(focusedOverlay, focusedDecorated!.HighlightColor);

        bool handledDown = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space));
        Assert.True(handledDown);
        owner.FlushBuild();

        var pressedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(pressedOverlay, pressedDecorated!.HighlightColor);
        Assert.Equal(1, pressedCount);

        bool handledUp = FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.Space));
        Assert.True(handledUp);
        owner.FlushBuild();
        Assert.Equal(1, pressedCount);
    }

    [Fact]
    public void TextButton_KeyboardActivation_NumPadEnter_InvokesOnPressed()
    {
        var owner = new BuildOwner();
        int pressedCount = 0;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => pressedCount += 1,
                    child: new Text("NumPad Enter"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 42,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(10, 8)));

        owner.FlushBuild();

        bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.NumpadEnter));
        Assert.True(handled);
        owner.FlushBuild();

        Assert.Equal(1, pressedCount);
    }

    [Fact]
    public void TextButton_KeyboardActivation_IgnoresModifiedSpaceChord()
    {
        var owner = new BuildOwner();
        var focusedOverlay = Colors.SeaGreen;
        var pressedOverlay = Colors.OrangeRed;
        int pressedCount = 0;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => pressedCount += 1,
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        {
                            if (states.HasFlag(MaterialState.Pressed))
                            {
                                return pressedOverlay;
                            }

                            if (states.HasFlag(MaterialState.Focused))
                            {
                                return focusedOverlay;
                            }

                            return null;
                        })),
                    child: new Text("Ctrl+Space ignored"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusListener);
        focusListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 43,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(focusListener, new Point(10, 8)));

        owner.FlushBuild();

        var focusedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(focusedOverlay, focusedDecorated!.HighlightColor);

        bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space, control: true));
        Assert.False(handled);
        owner.FlushBuild();

        var stillFocusedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(stillFocusedDecorated);
        Assert.Equal(focusedOverlay, stillFocusedDecorated!.HighlightColor);
        Assert.NotEqual(pressedOverlay, stillFocusedDecorated.HighlightColor);
        Assert.Equal(0, pressedCount);
    }

    [Fact]
    public void TextButton_PressedOverlayTakesPriorityOverFocusOverlay()
    {
        Color otherOverlay = Colors.CornflowerBlue;
        Color pressedOverlay = Colors.Gold;
        var focusNode = new FocusNode();
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                focusNode: focusNode,
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed)
                            ? pressedOverlay
                            : states.HasFlag(MaterialState.Focused)
                                ? otherOverlay
                                : null)),
                child: new Text("Priority")));

        focusNode.RequestFocus();
        harness.Pump(ButtonHarnessSize);
        Assert.Equal(otherOverlay, InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void TextButton_M2DefaultOverlay_UsesPressedAndFocusedOpacity010()
    {
        FocusManager.Instance.ResetForTests();
        ThemeData theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.DeepPink),
        };
        var focusNode = new FocusNode();

        using (WidgetRenderHarness harness = PumpButton(
            new TextButton(onPressed: () => { }, focusNode: focusNode, child: new Text("M2 overlay")),
            theme))
        {
            focusNode.RequestFocus();
            harness.Pump(ButtonHarnessSize);
            Assert.Equal(ApplyOpacity(theme.ColorScheme.Primary, 0.10), InkHighlight(harness));

            PressButton(harness);
            Assert.Equal(ApplyOpacity(theme.ColorScheme.Primary, 0.10), InkHighlight(harness));
        }

        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void ElevatedButton_DefaultFocusedOverlay_UseMaterial3Disabled_UsesOnPrimaryOpacity010()
    {
        FocusManager.Instance.ResetForTests();
        ThemeData theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onPrimary: Colors.AliceBlue),
        };
        var focusNode = new FocusNode();

        using (WidgetRenderHarness harness = PumpButton(
            new ElevatedButton(onPressed: () => { }, focusNode: focusNode, child: new Text("M2 focus")),
            theme))
        {
            focusNode.RequestFocus();
            harness.Pump(ButtonHarnessSize);

            // `ElevatedButton.styleFrom` derives the Material 2 overlay from `onPrimary` at
            // pressed 0.1 / hovered 0.08 / focused 0.1 — the doc comment's 0.12 is not what runs.
            Assert.Equal(ApplyOpacity(theme.ColorScheme.OnPrimary, 0.10), InkHighlight(harness));
        }

        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void OutlinedButton_DefaultFocusedOverlay_UseMaterial3Disabled_UsesPrimaryOpacity010()
    {
        FocusManager.Instance.ResetForTests();

        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.MediumSlateBlue)
        };
        var focusNode = new FocusNode();

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new OutlinedButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    style: new ButtonStyle(AnimationDuration: TimeSpan.Zero),
                    child: new Text("M2 outlined overlay"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        focusNode.RequestFocus();
        owner.FlushBuild();

        var focusedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(ApplyOpacity(theme.ColorScheme.Primary, 0.10), focusedDecorated!.HighlightColor);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void ElevatedButton_StyleFrom_OverlayColor_UsesHoverOpacityAndPressedPriority()
    {
        Color styleColor = Colors.DarkCyan;
        using WidgetRenderHarness harness = PumpButton(
            new ElevatedButton(
                onPressed: () => { },
                style: ElevatedButton.StyleFrom(overlayColor: styleColor, animationDuration: TimeSpan.Zero),
                child: new Text("StyleFrom states")));

        // Dart derives the overlay table from `overlayColor ?? foregroundColor`: pressed 0.1,
        // hovered 0.08, focused 0.1, and nothing at rest.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(styleColor, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), InkHighlight(harness));

        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), splash!.SplashColor);
    }

    [Fact]
    public void FilledButton_StyleFrom_OverlayColor_UsesHoverOpacityAndPressedPriority()
    {
        Color styleColor = Colors.DarkCyan;
        using WidgetRenderHarness harness = PumpButton(
            new FilledButton(
                onPressed: () => { },
                style: FilledButton.StyleFrom(overlayColor: styleColor, animationDuration: TimeSpan.Zero),
                child: new Text("StyleFrom states")));

        // Dart derives the overlay table from `overlayColor ?? foregroundColor`: pressed 0.1,
        // hovered 0.08, focused 0.1, and nothing at rest.
        HoverButton(harness, enter: true);
        Assert.Equal(ApplyOpacity(styleColor, 0.08), InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), InkHighlight(harness));

        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(ApplyOpacity(styleColor, 0.10), splash!.SplashColor);
    }

    [Fact]
    public void FilledButton_StyleFrom_TransparentOverlay_KeepsBaseBackground_AndNoSplash()
    {
        using WidgetRenderHarness harness = PumpButton(
            new FilledButton(
                onPressed: () => { },
                style: FilledButton.StyleFrom(overlayColor: MaterialColors.Transparent),
                child: new Text("Transparent overlay")));

        // Dart's `styleFrom` passes a fully transparent overlay through verbatim, which defeats
        // every highlight instead of deriving the 0.1/0.08/0.1 table from it.
        HoverButton(harness, enter: true);
        Assert.Equal(MaterialColors.Transparent, InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(MaterialColors.Transparent, InkHighlight(harness));
        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(MaterialColors.Transparent, splash!.SplashColor);
    }

    [Fact]
    public void OutlinedButton_StyleFrom_TransparentOverlay_HasNoIdleTint_AndNoSplash()
    {
        using WidgetRenderHarness harness = PumpButton(
            new OutlinedButton(
                onPressed: () => { },
                style: OutlinedButton.StyleFrom(overlayColor: MaterialColors.Transparent),
                child: new Text("Transparent overlay")));

        // Dart's `styleFrom` passes a fully transparent overlay through verbatim, which defeats
        // every highlight instead of deriving the 0.1/0.08/0.1 table from it.
        HoverButton(harness, enter: true);
        Assert.Equal(MaterialColors.Transparent, InkHighlight(harness));

        PressButton(harness);
        Assert.Equal(MaterialColors.Transparent, InkHighlight(harness));
        RenderInkResponsePaint? splash = FindInkPaint(harness.RenderView);
        Assert.NotNull(splash);
        Assert.Equal(MaterialColors.Transparent, splash!.SplashColor);
    }

    [Fact]
    public void TextButton_PointerDownStartsInkSplashRender()
    {
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(onPressed: () => { }, child: new Text("Splash")));

        RenderInkResponsePaint? initialSplash = FindInkPaint(harness.RenderView);
        Assert.NotNull(initialSplash);
        Assert.Equal(0, initialSplash!.SplashProgress);

        PressButton(harness);

        RenderInkResponsePaint? activeSplash = FindInkPaint(harness.RenderView);
        Assert.NotNull(activeSplash);
        Assert.NotNull(activeSplash!.SplashColor);
    }

    [Fact]
    public void TextButton_HasNoClipByDefault_ButInkRemainsContained()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    child: new Text("Rounded splash"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var clip = FindDescendant<RenderClipRRect>(renderRoot);
        var splash = FindInkPaint(renderRoot);

        Assert.Null(clip);
        Assert.NotNull(splash);
        Assert.True(splash!.ContainedInkWell);
    }

    [Fact]
    public void TextButton_M2DefaultShape_UsesRoundedRectangleRadius4()
    {
        ThemeData theme = ThemeData.Light with { UseMaterial3 = false };
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                clipBehavior: Clip.AntiAlias,
                child: new Text("Rounded splash")),
            theme);

        ShapeDecoration decoration = MaterialShapeDecoration(harness);
        var shape = Assert.IsType<RoundedRectangleBorder>(decoration.Shape);
        Assert.Equal(BorderRadius.Circular(4), shape.BorderRadius);
    }

    [Fact]
    public void ElevatedButton_HasNoClipByDefault_WhenUseMaterial3Disabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new ElevatedButton(
                    onPressed: () => { },
                    child: new Text("Rounded splash"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var clip = FindDescendant<RenderClipRRect>(RequireRenderObject<RenderObject>(root.ChildElement));

        Assert.Null(clip);
    }

    [Fact]
    public void OutlinedButton_HasNoClipByDefault_WhenUseMaterial3Disabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new OutlinedButton(
                    onPressed: () => { },
                    child: new Text("Rounded splash"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var clip = FindDescendant<RenderClipRRect>(RequireRenderObject<RenderObject>(root.ChildElement));

        Assert.Null(clip);
    }

    [Fact]
    public void TextButton_PressedOverlay_ClearsAfterPointerUp()
    {
        Color pressedOverlay = Colors.Gold;
        using WidgetRenderHarness harness = PumpButton(
            new TextButton(
                onPressed: () => { },
                style: new ButtonStyle(
                    OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Pressed) ? pressedOverlay : null)),
                child: new Text("Click")));

        PressButton(harness);
        Assert.Equal(pressedOverlay, InkHighlight(harness));

        ReleaseButton(harness);
        Assert.NotEqual(pressedOverlay, InkHighlight(harness));
    }

    [Fact]
    public void TextButton_ExternalFocusNode_RequestFocus_AppliesFocusedOverlay()
    {
        FocusManager.Instance.ResetForTests();

        var owner = new BuildOwner();
        var focusNode = new FocusNode();
        var focusedOverlay = Colors.OrangeRed;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    style: new ButtonStyle(
                        OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                            states.HasFlag(MaterialState.Focused) ? focusedOverlay : null)),
                    child: new Text("External focus node"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(initialDecorated);
        Assert.Null(initialDecorated!.HighlightColor);

        focusNode.RequestFocus();
        owner.FlushBuild();

        var focusedDecorated = FindInkPaint(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(focusedDecorated);
        Assert.Equal(focusedOverlay, focusedDecorated!.HighlightColor);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void TextButton_Autofocus_RequestsProvidedFocusNodeOnMount()
    {
        FocusManager.Instance.ResetForTests();

        var owner = new BuildOwner();
        var focusNode = new FocusNode();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    autofocus: true,
                    child: new Text("Autofocus mount"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(focusNode.HasFocus);
        Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void TextButton_Autofocus_RequestIsAppliedWhenToggledFromFalseToTrue()
    {
        FocusManager.Instance.ResetForTests();

        var owner = new BuildOwner();
        var focusNode = new FocusNode();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    autofocus: false,
                    child: new Text("Autofocus update"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.False(focusNode.HasFocus);

        root.Update(
            new Theme(
                data: ThemeData.Light,
                child: new TextButton(
                    onPressed: () => { },
                    focusNode: focusNode,
                    autofocus: true,
                    child: new Text("Autofocus update"))));
        owner.FlushBuild();

        Assert.True(focusNode.HasFocus);
        Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);

        root.Unmount();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void TextButton_TightWidth_ExpandsInkSplashToFullButtonBounds()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 240,
                    child: new TextButton(
                        onPressed: () => { },
                        child: new Text("Wide button")))));

        harness.Pump(new Size(300, 120));

        var renderRoot = harness.RenderView.Child;
        var splash = FindInkPaint(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(splash);
        Assert.NotNull(decorated);
        Assert.Equal(240, decorated!.Size.Width, 3);
        Assert.Equal(240, splash!.Size.Width, 3);
        Assert.Equal(decorated.Size.Height, splash.Size.Height, 3);
    }

    [Fact]
    public void IconButton_DefaultIconTheme_UsesOnSurfaceVariantAndSize24()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                onSurfaceVariant: Colors.MediumAquamarine)
        };
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new IconButton(
                    icon: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.MediumAquamarine, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
    }

    [Fact]
    public void IconButton_Material3VariantsUseDirectColorSchemeRoles()
    {
        var scheme = ThemeData.Light.ColorScheme.CopyWith(
            primary: Colors.DarkGreen,
            onPrimary: Colors.Gold,
            secondaryContainer: Colors.Navy,
            onSecondaryContainer: Colors.Orange,
            surfaceContainerHighest: Colors.CadetBlue,
            onSurfaceVariant: Colors.Purple,
            inverseSurface: Colors.White,
            onInverseSurface: Colors.Black,
            outline: Colors.Brown);
        var theme = ThemeData.Light with
        {
            ColorScheme = scheme,
            PrimaryColor = Colors.Red,
        };

        AssertVariant(
            icon => new IconButton(icon: icon, onPressed: () => { }),
            Colors.Purple,
            MaterialColors.Transparent,
            null);
        AssertVariant(
            icon => IconButton.Filled(icon: icon, onPressed: () => { }),
            Colors.Gold,
            Colors.DarkGreen,
            null);
        AssertVariant(
            icon => IconButton.Filled(icon: icon, isSelected: false, onPressed: () => { }),
            Colors.DarkGreen,
            Colors.CadetBlue,
            null);
        AssertVariant(
            icon => IconButton.FilledTonal(icon: icon, onPressed: () => { }),
            Colors.Orange,
            Colors.Navy,
            null);
        AssertVariant(
            icon => IconButton.Outlined(icon: icon, isSelected: true, onPressed: () => { }),
            Colors.Black,
            Colors.White,
            null);
        AssertVariant(
            icon => IconButton.Outlined(icon: icon, isSelected: false, onPressed: () => { }),
            Colors.Purple,
            MaterialColors.Transparent,
            new BorderSide(Colors.Brown));

        void AssertVariant(
            Func<Widget, IconButton> factory,
            Color expectedForeground,
            Color expectedBackground,
            BorderSide? expectedBorder)
        {
            var owner = new BuildOwner();
            IconThemeData? capturedTheme = null;
            var root = new TestRootElement(
                new Theme(
                    data: theme,
                    child: factory(
                        new CaptureIconThemeWidget(
                            iconTheme => capturedTheme = iconTheme))));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.NotNull(capturedTheme);
            Assert.Equal(expectedForeground, capturedTheme!.Color);
            var decorated = FindDescendant<RenderDecoratedBox>(
                RequireRenderObject<RenderObject>(root.ChildElement));
            Assert.NotNull(decorated);
            Assert.Equal(expectedBackground, decorated!.Decoration.Color);
            Assert.Equal(
                expectedBorder is { } expectedSide ? Plumix.Rendering.Border.FromBorderSide(expectedSide) : null,
                decorated.Decoration.Border);
            root.Unmount();
        }
    }

    [Fact]
    public void Theme_InstallsConfiguredIconThemeForWidgetDescendants()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;
        var expected = new IconThemeData(
            Color: Colors.DarkOrange,
            Size: 29,
            Opacity: 0.5);
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { IconTheme = expected },
                child: new CaptureIconThemeWidget(
                    iconTheme => capturedTheme = iconTheme)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(
            expected.CopyWith(
                fill: IconThemeData.Fallback.Fill,
                weight: IconThemeData.Fallback.Weight,
                grade: IconThemeData.Fallback.Grade,
                opticalSize: IconThemeData.Fallback.OpticalSize,
                applyTextScaling: IconThemeData.Fallback.ApplyTextScaling),
            capturedTheme);
    }

    [Fact]
    public void IconButton_DefaultMinSize_UsesMaterial3Baseline40x40()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new IconButton(
                    icon: new SizedBox(width: 20, height: 20),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(40, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void IconButton_DefaultMinSize_UseMaterial3Disabled_UsesMaterial2Baseline48x48()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new IconButton(
                    icon: new SizedBox(width: 20, height: 20),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(48, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(48, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void IconButton_StyleFrom_ForegroundAndIconSizeOverrideDefaults()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new IconButton(
                    icon: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme),
                    onPressed: () => { },
                    style: IconButton.StyleFrom(
                        foregroundColor: Colors.OrangeRed,
                        iconSize: 30))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.OrangeRed, capturedTheme!.Color);
        Assert.Equal(30, capturedTheme.Size);
    }

    [Fact]
    public void IconButton_ThemeStyle_OverridesAmbientIconThemeDefaults()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    IconButtonTheme = new IconButtonThemeData(
                        style: IconButton.StyleFrom(
                            foregroundColor: Colors.ForestGreen,
                            iconSize: 22))
                },
                child: new Plumix.Widgets.IconTheme(
                    data: new IconThemeData(Color: Colors.CadetBlue, Size: 18),
                    child: new IconButton(
                        icon: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme),
                        onPressed: () => { }))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.ForestGreen, capturedTheme!.Color);
        Assert.Equal(22, capturedTheme.Size);
    }

    [Fact]
    public void IconButton_WidgetStyle_OverridesIconButtonThemeStyle()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    IconButtonTheme = new IconButtonThemeData(
                        style: IconButton.StyleFrom(foregroundColor: Colors.ForestGreen))
                },
                child: new IconButton(
                    icon: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme),
                    onPressed: () => { },
                    style: IconButton.StyleFrom(foregroundColor: Colors.OrangeRed))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.OrangeRed, capturedTheme!.Color);
    }

    [Fact]
    public void IconButton_IsSelectedTrue_UsesSelectedIcon()
    {
        var owner = new BuildOwner();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new IconButton(
                    icon: new Text("unselected"),
                    selectedIcon: new Text("selected"),
                    isSelected: true,
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal("selected", paragraph!.PlainText);
    }

    [Fact]
    public void IconButton_Outlined_SelectedState_DropsOutlineBorder()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: IconButton.Outlined(
                    icon: new SizedBox(width: 20, height: 20),
                    isSelected: false,
                    style: IconButton.StyleFrom(animationDuration: TimeSpan.Zero),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var unselectedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(unselectedDecorated);
        Assert.Equal(Plumix.Rendering.Border.FromBorderSide(
            new BorderSide(ThemeData.Light.ColorScheme.Outline, 1)), unselectedDecorated!.Decoration.Border);

        root.Update(
            new Theme(
                data: ThemeData.Light,
                child: IconButton.Outlined(
                    icon: new SizedBox(width: 20, height: 20),
                    isSelected: true,
                    style: IconButton.StyleFrom(animationDuration: TimeSpan.Zero),
                    onPressed: () => { })));
        owner.FlushBuild();

        var selectedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(selectedDecorated);
        Assert.Null(selectedDecorated!.Decoration.Border);
    }

    [Fact]
    public void IconButton_StyleFromTapTargetSize_OverridesThemeTapTargetSize()
    {
        using (var paddedHarness = new WidgetRenderHarness(
                   new Theme(
                       data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.Padded },
                       child: new SizedBox(
                           width: 120,
                           child: new IconButton(
                               icon: new SizedBox(width: 20, height: 20),
                               onPressed: () => { })))))
        {
            paddedHarness.Pump(new Size(220, 120));
            var paddedHitResult = new BoxHitTestResult();
            Assert.True(paddedHarness.RenderView.HitTest(paddedHitResult, new Point(60, 46)));
        }

        using var overrideHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.Padded },
                child: new SizedBox(
                    width: 120,
                    child: new IconButton(
                        icon: new SizedBox(width: 20, height: 20),
                        onPressed: () => { },
                        style: IconButton.StyleFrom(tapTargetSize: MaterialTapTargetSize.ShrinkWrap)))));

        overrideHarness.Pump(new Size(220, 120));

        var overrideHitResult = new BoxHitTestResult();
        Assert.False(overrideHarness.RenderView.HitTest(overrideHitResult, new Point(60, 46)));
    }

    [Fact]
    public void IconButton_ConstructorsExposeCompleteDartApiSurface()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        Widget selectedIcon = new Icon(Icons.Star);

        IconButton button = IconButton.FilledTonal(
            icon: new Icon(Icons.StarOutline),
            onPressed: () => { },
            iconSize: 28,
            visualDensity: VisualDensity.Compact,
            padding: new Thickness(6),
            alignment: Alignment.BottomRight,
            splashRadius: 31,
            tooltip: "Favorite",
            enableFeedback: false,
            mouseCursor: SystemMouseCursors.Grab,
            focusNode: focusNode,
            autofocus: true,
            constraints: new BoxConstraints(MinWidth: 44, MinHeight: 42),
            isSelected: true,
            selectedIcon: selectedIcon,
            statesController: statesController);

        Assert.Equal(28, button.IconSize);
        Assert.Equal(VisualDensity.Compact, button.VisualDensity);
        Assert.Equal(new Thickness(6), button.Padding);
        Assert.Equal(Alignment.BottomRight, button.Alignment);
        Assert.Equal(31, button.SplashRadius);
        Assert.Equal("Favorite", button.Tooltip);
        Assert.False(button.EnableFeedback);
        Assert.Equal(SystemMouseCursors.Grab, button.MouseCursor);
        Assert.Same(focusNode, button.FocusNode);
        Assert.True(button.Autofocus);
        Assert.True(button.IsSelected);
        Assert.Same(selectedIcon, button.SelectedIcon);
        Assert.Same(statesController, button.StatesController);
    }

    [Fact]
    public void IconButton_StyleFromMapsCompleteButtonStyleSurface()
    {
        TimeSpan animationDuration = TimeSpan.FromMilliseconds(275);
        ButtonStyle style = IconButton.StyleFrom(
            enabledMouseCursor: SystemMouseCursors.Click,
            disabledMouseCursor: SystemMouseCursors.Basic,
            visualDensity: VisualDensity.Comfortable,
            animationDuration: animationDuration,
            enableFeedback: false);

        Assert.Equal(
            SystemMouseCursors.Click,
            style.MouseCursor!.Resolve(MaterialState.None));
        Assert.Equal(
            SystemMouseCursors.Basic,
            style.MouseCursor.Resolve(MaterialState.Disabled));
        Assert.Equal(VisualDensity.Comfortable, style.VisualDensity);
        Assert.Equal(animationDuration, style.AnimationDuration);
        Assert.False(style.EnableFeedback);
    }

    [Fact]
    public void IconButton_Material2UsesLegacyBranchAndIgnoresToggleIcon()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            DisabledColor = Colors.DarkOrange,
            IconButtonTheme = new IconButtonThemeData(
                IconButton.StyleFrom(foregroundColor: Colors.ForestGreen)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new IconButton(
                    icon: new CaptureIconThemeWidget(iconTheme => capturedTheme = iconTheme),
                    selectedIcon: new Text("selected"),
                    isSelected: true,
                    visualDensity: VisualDensity.Compact,
                    onPressed: null)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkOrange, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
        Assert.Null(FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement)));

        var constrainedBox = FindDescendant<RenderConstrainedBox>(
            RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(40, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
    }

    [Fact]
    public void IconButton_ExternalStatesControllerTracksSelectedAndDisabled()
    {
        var owner = new BuildOwner();
        var statesController = new MaterialStatesController();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new IconButton(
                    icon: new Icon(Icons.Star),
                    isSelected: true,
                    statesController: statesController,
                    onPressed: null)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(statesController.Value.HasFlag(MaterialState.Selected));
        Assert.True(statesController.Value.HasFlag(MaterialState.Disabled));

        root.Update(
            new Theme(
                data: ThemeData.Light,
                child: new IconButton(
                    icon: new Icon(Icons.Star),
                    isSelected: false,
                    statesController: statesController,
                    onPressed: () => { })));
        owner.FlushBuild();

        Assert.False(statesController.Value.HasFlag(MaterialState.Selected));
        Assert.False(statesController.Value.HasFlag(MaterialState.Disabled));
    }

    [Fact]
    public void IconButton_LocalThemeOverridesGlobalThemeAndAmbientIconTheme()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedTheme = null;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    IconButtonTheme = new IconButtonThemeData(
                        IconButton.StyleFrom(foregroundColor: Colors.Red)),
                },
                child: new Plumix.Widgets.IconTheme(
                    data: new IconThemeData(Color: Colors.Blue),
                    child: new IconButtonTheme(
                        data: new IconButtonThemeData(
                            IconButton.StyleFrom(foregroundColor: Colors.ForestGreen)),
                        child: new IconButton(
                            icon: new CaptureIconThemeWidget(
                                iconTheme => capturedTheme = iconTheme),
                            onPressed: () => { })))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.ForestGreen, capturedTheme!.Color);
    }

    [Fact]
    public void IconButtonThemeData_CopyAndLerpFollowButtonStyle()
    {
        var a = new IconButtonThemeData(
            IconButton.StyleFrom(
                foregroundColor: Colors.Black,
                iconSize: 20,
                visualDensity: VisualDensity.Compact,
                animationDuration: TimeSpan.FromMilliseconds(100),
                enableFeedback: false));
        IconButtonThemeData b = a with
        {
            Style = IconButton.StyleFrom(
                foregroundColor: Colors.White,
                iconSize: 28,
                visualDensity: VisualDensity.Standard,
                animationDuration: TimeSpan.FromMilliseconds(300),
                enableFeedback: true),
        };

        IconButtonThemeData midpoint = Assert.IsType<IconButtonThemeData>(
            IconButtonThemeData.Lerp(a, b, 0.5));

        Assert.Equal(
            new ColorTween().Evaluate(0.5, Colors.Black, Colors.White),
            midpoint.Style!.ForegroundColor!.Resolve(MaterialState.None));
        Assert.Equal(24, midpoint.Style.IconSize!.Resolve(MaterialState.None));
        Assert.Equal(VisualDensity.Standard, midpoint.Style.VisualDensity);
        Assert.Equal(TimeSpan.FromMilliseconds(300), midpoint.Style.AnimationDuration);
        Assert.True(midpoint.Style.EnableFeedback);
    }

    [Fact]
    public void IconButton_TooltipWrapsMaterial2AndMaterial3Branches()
    {
        using var material3Harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new IconButton(
                    icon: new Icon(Icons.InfoOutline),
                    tooltip: "Material 3 info",
                    onPressed: () => { })));
        material3Harness.Pump(new Size(120, 80));

        Assert.NotNull(material3Harness.FindState<TooltipState>());

        using var material2Harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { UseMaterial3 = false },
                child: new IconButton(
                    icon: new Icon(Icons.InfoOutline),
                    tooltip: "Material 2 info",
                    onPressed: () => { })));
        material2Harness.Pump(new Size(120, 80));

        Assert.NotNull(material2Harness.FindState<TooltipState>());
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

    private static readonly Size ButtonHarnessSize = new(200, 100);

    /// Builds a button under a `Theme`/`Directionality` and pumps one frame, the shape every
    /// interaction test needs now that the button composes `Material` + `InkWell`.
    private static WidgetRenderHarness PumpButton(Widget button, ThemeData? theme = null)
    {
        var harness = new WidgetRenderHarness(
            new Theme(
                data: theme ?? ThemeData.Light,
                child: new Directionality(TextDirection.Ltr, button)));
        harness.Pump(ButtonHarnessSize);
        return harness;
    }

    private static Color? InkHighlight(WidgetRenderHarness harness)
    {
        RenderInkResponsePaint? paint = FindInkPaint(harness.RenderView);
        Assert.NotNull(paint);
        return paint!.HighlightColor;
    }

    private static void HoverButton(WidgetRenderHarness harness, bool enter, int pointer = 91)
    {
        RenderPointerListener? listener = FindHoverPointerListener(harness.RenderView);
        Assert.NotNull(listener);
        var position = enter ? new Point(20, 20) : new Point(400, 400);
        PointerEvent hover = enter
            ? new PointerEnterEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, DateTime.UtcNow)
            : new PointerExitEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, DateTime.UtcNow);
        listener!.HandleEvent(hover, new BoxHitTestEntry(listener, position));
        harness.Pump(ButtonHarnessSize);
    }

    private static void PressButton(WidgetRenderHarness harness, int pointer = 91)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                new Point(20, 20),
                PointerButtons.Primary,
                DateTime.UtcNow));
        harness.Pump(ButtonHarnessSize);
    }

    private static void ReleaseButton(WidgetRenderHarness harness, int pointer = 91)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                new Point(20, 20),
                PointerButtons.None,
                DateTime.UtcNow));
        harness.Pump(ButtonHarnessSize);
    }

    /// The `ShapeDecoration` the button's `Material` paints, skipping any plain `BoxDecoration`
    /// above it in the tree.
    private static ShapeDecoration MaterialShapeDecoration(WidgetRenderHarness harness)
    {
        ShapeDecoration? found = null;
        void Visit(RenderObject node)
        {
            if (found is not null)
            {
                return;
            }

            if (node is RenderDecoratedBox box && box.DecorationValue is ShapeDecoration shape)
            {
                found = shape;
                return;
            }

            node.VisitChildren(Visit);
        }

        Visit(harness.RenderView);
        Assert.NotNull(found);
        return found!;
    }

    private static RenderInkResponsePaint? FindInkPaint(RenderObject? root)
    {
        return FindDescendant<RenderInkResponsePaint>(root);
    }

    private static RenderPointerListener? FindInteractivePointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerDown != null
            && listener.OnPointerUp != null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindInteractivePointerListener(child);
        });

        return result;
    }

    private static RenderPointerListener? FindHoverPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerEnter != null
            && listener.OnPointerExit != null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindHoverPointerListener(child);
        });

        return result;
    }

    private static RenderPointerListener? FindFocusPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerDown != null
            && listener.OnPointerUp == null
            && listener.OnPointerCancel == null
            && listener.OnPointerEnter == null
            && listener.OnPointerExit == null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindFocusPointerListener(child);
        });

        return result;
    }

    private static Plumix.Rendering.BoxShadow RequirePrimaryShadow(RenderDecoratedBox decorated)
    {
        IReadOnlyList<Plumix.Rendering.BoxShadow>? shadows = decorated.Decoration.BoxShadows;
        Assert.NotNull(shadows);
        Assert.True(shadows.Count > 0);
        return shadows[0];
    }

    private static void AssertIconRowOrder(RenderObject root, bool iconFirst)
    {
        var row = FindDescendant<RenderFlex>(root);
        Assert.NotNull(row);
        Assert.Equal(Axis.Horizontal, row!.Direction);

        var first = Assert.IsAssignableFrom<RenderBox>(row.FirstChild);
        var second = Assert.IsAssignableFrom<RenderBox>(row.ChildAfter(first));

        if (iconFirst)
        {
            Assert.IsType<RenderConstrainedBox>(first);
            Assert.IsType<RenderParagraph>(second);
        }
        else
        {
            Assert.IsType<RenderParagraph>(first);
            Assert.IsType<RenderConstrainedBox>(second);
        }
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

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public T FindState<T>() where T : State
        {
            T? result = null;
            void Visit(Element element)
            {
                if (result is not null)
                {
                    return;
                }

                if (element is StatefulElement stateful && stateful.State is T match)
                {
                    result = match;
                    return;
                }

                element.VisitChildren(Visit);
            }

            Visit(_rootElement);
            return result
                   ?? throw new InvalidOperationException(
                       $"State {typeof(T).Name} was not found.");
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
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
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

            public override void Unmount()
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

    private static SemanticsNode? FindSemantics(
        SemanticsNode? node,
        Func<SemanticsNode, bool> predicate)
    {
        if (node is null)
        {
            return null;
        }

        if (predicate(node))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? match = FindSemantics(child, predicate);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Color BlendColorOverlay(Color baseColor, Color overlayColor)
    {
        static byte Blend(byte from, byte to, double t)
        {
            return (byte)Math.Clamp((int)(from + ((to - from) * t)), 0, 255);
        }

        double clampedOpacity = Math.Clamp(overlayColor.A / 255.0, 0, 1);
        return Color.FromArgb(
            baseColor.A,
            Blend(baseColor.R, overlayColor.R, clampedOpacity),
            Blend(baseColor.G, overlayColor.G, clampedOpacity),
            Blend(baseColor.B, overlayColor.B, clampedOpacity));
    }

    private static Color ApplySurfaceTint(Color color, Color surfaceTint, double elevation)
    {
        return ElevationOverlay.ApplySurfaceTint(color, surfaceTint, elevation);
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

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
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
