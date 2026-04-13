using System.Linq;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialFloatingActionButtonTests
{
    [Fact]
    public void FloatingActionButton_DefaultM3_UsesThemePrimaryContainerAndOnPrimaryContainerColors()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var theme = ThemeData.Light with
        {
            PrimaryContainerColor = Colors.Moccasin,
            OnPrimaryContainerColor = Colors.MediumBlue,
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FloatingActionButton(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(Colors.Moccasin, decorated!.Decoration.Color);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(Colors.MediumBlue, capturedIconTheme!.Color);
        Assert.Equal(24, capturedIconTheme.Size);
    }

    [Fact]
    public void FloatingActionButton_DefaultM3_UsesRegular56ConstraintAndRounded16Shape()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var constrainedBox = FindDescendant<RenderConstrainedBox>(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(constrainedBox);
        Assert.Equal(56, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MaxHeight);

        Assert.NotNull(decorated);
        Assert.Equal(BorderRadius.Circular(16), decorated!.Decoration.BorderRadius);
    }

    [Fact]
    public void FloatingActionButton_DefaultClipBehavior_DoesNotInsertClipRRect()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.Null(FindDescendant<RenderClipRRect>(renderRoot));
    }

    [Fact]
    public void FloatingActionButton_ClipBehaviorHardEdge_InsertsClipRRect()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: () => { },
                    clipBehavior: Clip.HardEdge)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.NotNull(FindDescendant<RenderClipRRect>(renderRoot));
    }

    [Fact]
    public void FloatingActionButton_StoresHeroTagMouseCursorAndEnableFeedback()
    {
        var heroTag = new object();
        var cursor = SystemMouseCursors.Click;
        var fab = new FloatingActionButton(
            child: new Icon(Icons.Add),
            onPressed: () => { },
            heroTag: heroTag,
            mouseCursor: cursor,
            enableFeedback: false);

        Assert.Same(heroTag, fab.HeroTag);
        Assert.Same(cursor, fab.MouseCursor);
        Assert.False(fab.EnableFeedback);
    }

    [Fact]
    public void FloatingActionButton_HeroTag_WrapsBuiltResultWithHero()
    {
        var owner = new BuildOwner();
        Widget? capturedBuiltWidget = null;
        var heroTag = new object();

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new CaptureFloatingActionButtonBuild(
                    floatingActionButton: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        onPressed: () => { },
                        heroTag: heroTag),
                    onBuilt: widget => capturedBuiltWidget = widget)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var hero = Assert.IsType<Hero>(capturedBuiltWidget);
        Assert.Same(heroTag, hero.Tag);
    }

    [Fact]
    public void FloatingActionButton_DefaultMouseCursor_UsesClickOnHover()
    {
        MouseCursorManager.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        onPressed: () => { })));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.Equal(SystemMouseCursors.Basic, MouseCursorManager.CurrentCursor);

            var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
            Assert.NotNull(hoverListener);
            hoverListener!.HandleEvent(
                new PointerEnterEvent(
                    pointer: 702,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 8),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(hoverListener, new Point(10, 8)));
            owner.FlushBuild();

            Assert.Equal(SystemMouseCursors.Click, MouseCursorManager.CurrentCursor);

            hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
            Assert.NotNull(hoverListener);
            hoverListener!.HandleEvent(
                new PointerExitEvent(
                    pointer: 702,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(150, 8),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(hoverListener, new Point(150, 8)));
            owner.FlushBuild();

            Assert.Equal(SystemMouseCursors.Basic, MouseCursorManager.CurrentCursor);
        }
        finally
        {
            MouseCursorManager.ResetForTests();
        }
    }

    [Fact]
    public void FloatingActionButton_ThemeMouseCursor_UsedWhenWidgetMouseCursorIsNull()
    {
        MouseCursorManager.ResetForTests();
        try
        {
            var themeCursor = new SystemMouseCursor("themeCursor");
            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light with
                    {
                        FloatingActionButtonTheme = new FloatingActionButtonThemeData(
                            MouseCursor: themeCursor),
                    },
                    child: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        onPressed: () => { })));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var hoverListener = FindHoverPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
            Assert.NotNull(hoverListener);
            hoverListener!.HandleEvent(
                new PointerEnterEvent(
                    pointer: 703,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 8),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(hoverListener, new Point(10, 8)));
            owner.FlushBuild();

            Assert.Equal(themeCursor, MouseCursorManager.CurrentCursor);
        }
        finally
        {
            MouseCursorManager.ResetForTests();
        }
    }

    [Fact]
    public void FloatingActionButton_DefaultEnableFeedback_EmitsTapFeedbackOnKeyboardActivation()
    {
        FocusManager.Instance.ResetForTests();
        Feedback.ResetForTests();
        try
        {
            var pressedCount = 0;
            var feedbackEvents = new List<FeedbackType>();
            Feedback.FeedbackTriggered += feedbackEvents.Add;

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        onPressed: () => pressedCount += 1)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
            Assert.NotNull(focusListener);
            focusListener!.HandleEvent(
                new PointerDownEvent(
                    pointer: 704,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 8),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(focusListener, new Point(10, 8)));
            owner.FlushBuild();

            var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(1, pressedCount);
            Assert.Single(feedbackEvents);
            Assert.Equal(FeedbackType.Tap, feedbackEvents[0]);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
            Feedback.ResetForTests();
        }
    }

    [Fact]
    public void FloatingActionButton_EnableFeedbackFalse_SuppressesTapFeedback()
    {
        FocusManager.Instance.ResetForTests();
        Feedback.ResetForTests();
        try
        {
            var pressedCount = 0;
            var feedbackEvents = new List<FeedbackType>();
            Feedback.FeedbackTriggered += feedbackEvents.Add;

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        onPressed: () => pressedCount += 1,
                        enableFeedback: false)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
            Assert.NotNull(focusListener);
            focusListener!.HandleEvent(
                new PointerDownEvent(
                    pointer: 705,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 8),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(focusListener, new Point(10, 8)));
            owner.FlushBuild();

            var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(1, pressedCount);
            Assert.Empty(feedbackEvents);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
            Feedback.ResetForTests();
        }
    }

    [Fact]
    public void FloatingActionButton_ThemeEnableFeedbackFalse_SuppressesTapFeedback()
    {
        FocusManager.Instance.ResetForTests();
        Feedback.ResetForTests();
        try
        {
            var pressedCount = 0;
            var feedbackEvents = new List<FeedbackType>();
            Feedback.FeedbackTriggered += feedbackEvents.Add;

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light with
                    {
                        FloatingActionButtonTheme = new FloatingActionButtonThemeData(
                            EnableFeedback: false),
                    },
                    child: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        onPressed: () => pressedCount += 1)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var focusListener = FindFocusPointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
            Assert.NotNull(focusListener);
            focusListener!.HandleEvent(
                new PointerDownEvent(
                    pointer: 706,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 8),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(focusListener, new Point(10, 8)));
            owner.FlushBuild();

            var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(1, pressedCount);
            Assert.Empty(feedbackEvents);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
            Feedback.ResetForTests();
        }
    }

    [Fact]
    public void FloatingActionButton_Small_Uses40Constraint()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Small(
                    child: new Icon(Icons.Add),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(40, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void FloatingActionButton_Large_Uses96ConstraintAndIconSize36()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Large(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(96, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(96, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(96, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(96, constrainedBox.AdditionalConstraints.MaxHeight);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(36, capturedIconTheme!.Size);
    }

    [Fact]
    public void FloatingActionButton_Extended_WithIcon_UsesHeight56AndDirectionalPadding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Extended(
                    label: new Text("Create"),
                    icon: new Icon(Icons.Add),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var constrainedBox = FindDescendant<RenderConstrainedBox>(renderRoot);
        var paddings = FindDescendants<RenderPadding>(renderRoot);
        var hasExpectedPadding = paddings.Any(p => p.Padding == new Thickness(16, 0, 20, 0));
        var label = FindDescendants<RenderParagraph>(renderRoot).FirstOrDefault(p => p.Text == "Create");

        Assert.NotNull(constrainedBox);
        Assert.Equal(56, constrainedBox!.AdditionalConstraints.MinHeight);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MaxHeight);
        Assert.True(hasExpectedPadding);
        Assert.NotNull(label);
    }

    [Fact]
    public void FloatingActionButton_Extended_WithoutIcon_UsesStart20End20Padding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Extended(
                    label: new Text("Create"),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paddings = FindDescendants<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        var hasExpectedPadding = paddings.Any(p => p.Padding == new Thickness(20, 0, 20, 0));
        Assert.True(hasExpectedPadding);
    }

    [Fact]
    public void FloatingActionButton_ThemeDefaults_ApplyWhenWidgetOverridesMissing()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var theme = ThemeData.Light with
        {
            FloatingActionButtonTheme = new FloatingActionButtonThemeData(
                ForegroundColor: Colors.White,
                BackgroundColor: Colors.Orange,
                SizeConstraints: TightConstraints(60, 60)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FloatingActionButton(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var constrainedBox = FindDescendant<RenderConstrainedBox>(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(constrainedBox);
        Assert.Equal(60, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(60, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Orange, decorated!.Decoration.Color);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(Colors.White, capturedIconTheme!.Color);
    }

    [Fact]
    public void FloatingActionButton_WidgetColors_OverrideThemeDefaults()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var theme = ThemeData.Light with
        {
            FloatingActionButtonTheme = new FloatingActionButtonThemeData(
                ForegroundColor: Colors.White,
                BackgroundColor: Colors.Green),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FloatingActionButton(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { },
                    foregroundColor: Colors.Yellow,
                    backgroundColor: Colors.Purple)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Purple, decorated!.Decoration.Color);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(Colors.Yellow, capturedIconTheme!.Color);
    }

    [Fact]
    public void FloatingActionButton_HoverAndPressed_UseConfiguredElevations()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { ShadowColor = Colors.Black },
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: () => { },
                    elevation: 2,
                    hoverElevation: 4,
                    highlightElevation: 7)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var defaultDecorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        Assert.NotNull(defaultDecorated);
        Assert.Equal(2, RequirePrimaryShadow(defaultDecorated!).OffsetY);

        var hoverListener = FindHoverPointerListener(renderRoot);
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 700,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));
        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(4, RequirePrimaryShadow(hoveredDecorated!).OffsetY);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 700,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 8)));
        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(7, RequirePrimaryShadow(pressedDecorated!).OffsetY);
    }

    [Fact]
    public void FloatingActionButton_Disabled_FallsBackToBaseElevationWhenDisabledElevationMissing()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { ShadowColor = Colors.Black },
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: null,
                    elevation: 5)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(5, RequirePrimaryShadow(decorated!).OffsetY);
    }

    [Fact]
    public void FloatingActionButton_Tooltip_ShowsOnPointerEnter_AndHidesOnPointerExit()
    {
        Scheduler.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        tooltip: "Create item",
                        onPressed: () => { })));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
            Assert.Null(FindParagraphByText(renderRoot, "Create item"));

            var tooltipListener = FindTooltipHoverPointerListener(renderRoot);
            Assert.NotNull(tooltipListener);
            tooltipListener!.HandleEvent(
                new PointerEnterEvent(
                    pointer: 701,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 8),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(tooltipListener, new Point(10, 8)));
            owner.FlushBuild();

            renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
            Assert.NotNull(FindParagraphByText(renderRoot, "Create item"));

            tooltipListener = FindTooltipHoverPointerListener(renderRoot);
            Assert.NotNull(tooltipListener);
            tooltipListener!.HandleEvent(
                new PointerExitEvent(
                    pointer: 701,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(180, 8),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(tooltipListener, new Point(180, 8)));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.50));
            owner.FlushBuild();

            renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
            Assert.Null(FindParagraphByText(renderRoot, "Create item"));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    private static BoxConstraints TightConstraints(double width, double height)
    {
        return new BoxConstraints(
            MinWidth: width,
            MaxWidth: width,
            MinHeight: height,
            MaxHeight: height);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        var renderObject = element!.RenderObject;
        return Assert.IsAssignableFrom<T>(renderObject);
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T target)
        {
            return target;
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

        if (root is T target)
        {
            results.Add(target);
        }

        root.VisitChildren(child => CollectDescendants(child, results));
    }

    private static RenderPointerListener? FindInteractivePointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerDown != null
            && listener.OnPointerUp != null
            && listener.OnPointerCancel != null)
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

    private static RenderPointerListener? FindTooltipHoverPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerEnter != null
            && listener.OnPointerExit != null
            && listener.OnPointerDown != null)
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

            result = FindTooltipHoverPointerListener(child);
        });
        return result;
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderParagraph paragraph && paragraph.Text == text)
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

    private static BoxShadow RequirePrimaryShadow(RenderDecoratedBox decorated)
    {
        Assert.True(decorated.Decoration.BoxShadows.HasValue);
        var shadows = decorated.Decoration.BoxShadows!.Value;
        Assert.True(shadows.Count > 0);
        return shadows[0];
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
            return new SizedBox(width: 12, height: 12);
        }
    }

    private sealed class CaptureFloatingActionButtonBuild : StatelessWidget
    {
        private readonly FloatingActionButton _floatingActionButton;
        private readonly Action<Widget> _onBuilt;

        public CaptureFloatingActionButtonBuild(
            FloatingActionButton floatingActionButton,
            Action<Widget> onBuilt)
        {
            _floatingActionButton = floatingActionButton;
            _onBuilt = onBuilt;
        }

        public override Widget Build(BuildContext context)
        {
            var built = _floatingActionButton.Build(context);
            _onBuilt(built);
            return built;
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
