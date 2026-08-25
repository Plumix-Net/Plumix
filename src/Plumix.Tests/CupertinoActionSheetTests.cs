using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class CupertinoActionSheetTests : IDisposable
{
    private static readonly Size PhoneSize = new(800.0, 600.0);

    public CupertinoActionSheetTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructor_RequiresTitleMessageActionsOrCancelButton()
    {
        Assert.Throws<ArgumentException>(() => new CupertinoActionSheet());
        _ = new CupertinoActionSheet(actions: []);
        _ = new CupertinoActionSheet(title: new Text("Title"));
        _ = new CupertinoActionSheet(message: new Text("Message"));
        _ = new CupertinoActionSheet(cancelButton: new Text("Cancel"));
    }

    [Fact]
    public void ButtonFontSize_FollowsTheNativeTable()
    {
        double[] bodySizes = [14.0, 15.0, 16.0, 17.0, 19.0, 21.0, 23.0, 28.0, 33.0, 40.0, 47.0, 53.0];
        double[] buttonSizes = [21.0, 21.0, 21.0, 21.0, 23.0, 24.0, 24.0, 28.0, 33.0, 40.0, 47.0, 53.0];
        for (int index = 0; index < bodySizes.Length; index++)
        {
            Assert.Equal(
                buttonSizes[index],
                ActionSheetActionContent.ButtonFontSize(bodySizes[index]),
                precision: 3);
        }

        // The piecewise segments interpolate rather than step.
        Assert.Equal(22.0, ActionSheetActionContent.ButtonFontSize(18.0), precision: 3);
        Assert.Equal(23.5, ActionSheetActionContent.ButtonFontSize(20.0), precision: 3);
    }

    [Fact]
    public void ActionLabel_IsRenderedAtTheTableFontSize()
    {
        double[] bodySizes = [14.0, 17.0, 19.0, 21.0, 23.0, 53.0];
        double[] buttonSizes = [21.0, 21.0, 23.0, 24.0, 24.0, 53.0];
        for (int index = 0; index < bodySizes.Length; index++)
        {
            using var harness = new CupertinoThemeTestHarness(Wrap(
                Sheet(actions: [Action("One")]),
                textScaleFactor: bodySizes[index] / 17.0));
            harness.Pump(PhoneSize);

            RenderParagraph label = Paragraph(harness, "One");
            Assert.Equal(buttonSizes[index], label.TextScaler.Scale(label.FontSize), precision: 3);
        }
    }

    [Fact]
    public void ActionStyles_CoverDefaultDestructiveAndDarkMode()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(Sheet(actions:
        [
            Action("Plain"),
            new CupertinoActionSheetAction(new Text("Default"), () => { }, isDefaultAction: true),
            new CupertinoActionSheetAction(new Text("Destroy"), () => { }, isDestructiveAction: true),
        ])));
        light.Pump(PhoneSize);

        Assert.Equal(Color.FromUInt32(0xFF007AFF), ParagraphColor(Paragraph(light, "Plain")));
        Assert.Equal(FontWeight.Normal, Paragraph(light, "Plain").FontWeight);
        Assert.Equal(FontWeight.SemiBold, Paragraph(light, "Default").FontWeight);
        Assert.Equal(Color.FromUInt32(0xFFFF3B30), ParagraphColor(Paragraph(light, "Destroy")));

        using var dark = new CupertinoThemeTestHarness(Wrap(
            Sheet(actions: [Action("Plain"), Action("Destroy", destructive: true)]),
            brightness: PlatformBrightness.Dark));
        dark.Pump(PhoneSize);

        Assert.Equal(Color.FromUInt32(0xFF0A84FF), ParagraphColor(Paragraph(dark, "Plain")));
        Assert.Equal(Color.FromUInt32(0xFFFF453A), ParagraphColor(Paragraph(dark, "Destroy")));
    }

    [Fact]
    public void ContentStyles_BoldTheSectionThatStandsAlone()
    {
        using var both = new CupertinoThemeTestHarness(Wrap(
            Sheet(title: new Text("Title"), message: new Text("Message"), actions: [Action("One")])));
        both.Pump(PhoneSize);
        Assert.Equal(FontWeight.SemiBold, Paragraph(both, "Title").FontWeight);
        Assert.Equal(FontWeight.Normal, Paragraph(both, "Message").FontWeight);
        Assert.Equal(13.0, Paragraph(both, "Title").FontSize);
        Assert.Equal(Color.FromUInt32(0x851D1D1D), ParagraphColor(Paragraph(both, "Message")));

        using var titleOnly = new CupertinoThemeTestHarness(Wrap(
            Sheet(title: new Text("Title"), actions: [Action("One")])));
        titleOnly.Pump(PhoneSize);
        Assert.Equal(FontWeight.Normal, Paragraph(titleOnly, "Title").FontWeight);

        using var messageOnly = new CupertinoThemeTestHarness(Wrap(
            Sheet(message: new Text("Message"), actions: [Action("One")])));
        messageOnly.Pump(PhoneSize);
        Assert.Equal(FontWeight.SemiBold, Paragraph(messageOnly, "Message").FontWeight);

        using var dark = new CupertinoThemeTestHarness(Wrap(
            Sheet(title: new Text("Title"), actions: [Action("One")]),
            brightness: PlatformBrightness.Dark));
        dark.Pump(PhoneSize);
        Assert.Equal(Color.FromUInt32(0x96F1F1F1), ParagraphColor(Paragraph(dark, "Title")));
    }

    [Fact]
    public void ContentSection_InsertsFourLogicalPixelsBetweenTitleAndMessage()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            Sheet(title: new Text("Title"), message: new Text("Message"), actions: [Action("One")])));
        harness.Pump(PhoneSize);

        double titleBottom = Paragraph(harness, "Title").GetPaintOffsetToRoot().Y
                             + Paragraph(harness, "Title").Size.Height;
        double messageTop = Paragraph(harness, "Message").GetPaintOffsetToRoot().Y;
        // Title padding bottom 0 + 4.0 spacer + message padding top 0.
        Assert.Equal(4.0, messageTop - titleBottom, precision: 3);
    }

    [Fact]
    public void ActionsSection_IsOneButtonTallForOneActionAndCappedAtEightyFour()
    {
        Assert.Equal(57.17, ActionsSectionHeight(1), precision: 2);
        Assert.Equal(84.0, ActionsSectionHeight(2), precision: 2);
        Assert.Equal(84.0, ActionsSectionHeight(3), precision: 2);
        Assert.Equal(84.0, ActionsSectionHeight(5), precision: 2);
    }

    [Fact]
    public void Layout_WithCancelButton_MatchesTheNativeGeometry()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(
            title: new Text("The title"),
            message: new Text("The message"),
            actions: [Action("One"), Action("Two")],
            cancelButton: Action("Cancel"))));
        harness.Pump(PhoneSize);

        Assert.Equal(592.0, ActionBottom(harness, "Cancel"), precision: 2);
        Assert.Equal(469.36, ActionBottom(harness, "One"), precision: 2);
        Assert.Equal(526.83, ActionBottom(harness, "Two"), precision: 2);
    }

    [Fact]
    public void Width_UsesTheShorterViewDimensionInLandscape()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new Row(children: [Sheet(actions: [Action("One"), Action("Two")])])));
        harness.Pump(PhoneSize);

        // 800x600 is landscape, so the sheet is as wide as the view is tall.
        Assert.Equal(600.0, SheetBox(harness).Size.Width, precision: 3);
    }

    [Fact]
    public void Height_ShrinkWrapsToTheButtonsAndTheEdgePadding()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new Column(children: [Sheet(actions: [Action("One"), Action("Two")])])));
        harness.Pump(PhoneSize);

        // 57.17 * 2 + 0.3 divider + 8 top edge padding + 8 SafeArea minimum.
        Assert.Equal(130.64, SheetBox(harness).Size.Height, precision: 2);
    }

    [Fact]
    public void JustCancelButton_IsOneButtonPlusBothEdgePaddings()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(cancelButton: Action("Cancel"))));
        harness.Pump(PhoneSize);

        Assert.Equal(57.17 + 8.0 + 8.0, SheetBox(harness).Size.Height, precision: 2);
        Assert.Equal(600.0, SheetBox(harness).Size.Width, precision: 3);
    }

    [Theory]
    // iPhone SE gen 3: 667 - 20 top view padding - 20 top widget padding - 8 bottom edge padding.
    [InlineData(375.0, 667.0, 0.0, 20.0, 0.0, 359.0, 619.0)]
    // iPhone 13 Pro: 844 - 47 - 47 - 34 bottom view padding.
    [InlineData(390.0, 844.0, 0.0, 47.0, 34.0, 374.0, 716.0)]
    // iPhone 15 Plus: the capsule ratio turns a 59 view padding into a 54 widget padding.
    [InlineData(430.0, 932.0, 0.0, 59.0, 34.0, 414.0, 785.0)]
    // iPhone 13 Pro landscape: the width follows the view height, the top padding is a flat 8.
    [InlineData(844.0, 390.0, 47.0, 0.0, 21.0, 374.0, 361.0)]
    public void MaximumSize_MatchesTheMeasuredDevices(
        double viewWidth,
        double viewHeight,
        double horizontalPadding,
        double topPadding,
        double bottomPadding,
        double expectedWidth,
        double expectedHeight)
    {
        var viewSize = new Size(viewWidth, viewHeight);
        using var harness = new CupertinoThemeTestHarness(Wrap(
            Sheet(actions: Enumerable.Range(0, 20).Select(index => Action($"Button {index}")).ToArray()),
            size: viewSize,
            viewPadding: new Thickness(horizontalPadding, topPadding, horizontalPadding, bottomPadding)));
        harness.Pump(viewSize);

        RenderBox mainSheet = Descendants<RenderStack>(harness.RenderView)[0];
        Assert.Equal(expectedWidth, mainSheet.Size.Width, precision: 2);
        Assert.Equal(expectedHeight, mainSheet.Size.Height, precision: 2);
    }

    [Fact]
    public void ContentAndActions_ScrollWithSeparateControllers()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(
            title: new Text("The title"),
            message: new Text("The message"),
            actions: [Action("One"), Action("Two"), Action("Three")])));
        harness.Pump(PhoneSize);

        IReadOnlyList<CupertinoScrollbar> scrollbars = harness.FindWidgets<CupertinoScrollbar>();
        Assert.Equal(2, scrollbars.Count);
        Assert.NotNull(scrollbars[0].Controller);
        Assert.NotSame(scrollbars[0].Controller, scrollbars[1].Controller);
    }

    [Fact]
    public void PressedAction_PaintsThePressedFillAndHidesTheAdjacentDividers()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(actions:
        [
            Action("One"), Action("Two"), Action("Three"),
        ])));
        harness.Pump(PhoneSize);

        Color background = Color.FromUInt32(0xC8FCFCFC);
        Color divider = Color.FromUInt32(0xD4C9C9C9);
        int backgroundBoxes = Descendants<RenderColoredBox>(harness.RenderView)
            .Count(box => box.Color == background);
        Assert.Equal(2, Descendants<RenderColoredBox>(harness.RenderView).Count(box => box.Color == divider));

        PointerDown(harness, CenterOf(Paragraph(harness, "Two")));
        harness.Pump(PhoneSize);

        // The pressed fill shows on the very next frame, and both neighbouring hairlines turn into
        // the background color so the highlight is not cut in half.
        Assert.Contains(
            Descendants<RenderColoredBox>(harness.RenderView),
            box => box.Color == Color.FromUInt32(0xCAE0E0E0));
        Assert.DoesNotContain(Descendants<RenderColoredBox>(harness.RenderView), box => box.Color == divider);
        // The pressed button stops painting the idle fill; both hidden dividers start painting it.
        Assert.Equal(
            backgroundBoxes + 1,
            Descendants<RenderColoredBox>(harness.RenderView).Count(box => box.Color == background));
    }

    [Fact]
    public void SlidingTap_SlidesBetweenActionsAndConfirmsOnRelease()
    {
        string? pressed = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(actions:
        [
            new CupertinoActionSheetAction(new Text("One"), () => pressed = "One"),
            new CupertinoActionSheetAction(new Text("Two"), () => pressed = "Two"),
        ])));
        harness.Pump(PhoneSize);

        PointerDown(harness, CenterOf(Paragraph(harness, "Two")));
        PointerMove(harness, CenterOf(Paragraph(harness, "One")));
        harness.Pump(PhoneSize);
        PointerUp(harness, CenterOf(Paragraph(harness, "One")));

        Assert.Equal("One", pressed);
    }

    [Fact]
    public void SlidingTap_CanStartOnTheContentAndEndOnTheCancelButton()
    {
        bool cancelled = false;
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(
            title: new Text("The title"),
            actions: [Action("One")],
            cancelButton: new CupertinoActionSheetAction(new Text("Cancel"), () => cancelled = true))));
        harness.Pump(PhoneSize);

        PointerDown(harness, CenterOf(Paragraph(harness, "The title")));
        PointerMove(harness, CenterOf(Paragraph(harness, "Cancel")));
        harness.Pump(PhoneSize);
        PointerUp(harness, CenterOf(Paragraph(harness, "Cancel")));

        Assert.True(cancelled);
    }

    [Fact]
    public void CancelButton_PaintsItsOwnIdleAndPressedColors()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(
            actions: [Action("One")],
            cancelButton: Action("Cancel"))));
        harness.Pump(PhoneSize);

        Assert.Contains(DecorationColors(harness), color => color == Color.FromUInt32(0xFFFFFFFF));

        PointerDown(harness, CenterOf(Paragraph(harness, "Cancel")));
        harness.Pump(PhoneSize);
        Assert.Contains(DecorationColors(harness), color => color == Color.FromUInt32(0xFFECECEC));

        PointerUp(harness, CenterOf(Paragraph(harness, "Cancel")));
        harness.Pump(PhoneSize);
        Assert.Contains(DecorationColors(harness), color => color == Color.FromUInt32(0xFFFFFFFF));
    }

    [Fact]
    public void CancelButton_IsSeparatedByEightPixelsOnlyWhenSomethingSitsAboveIt()
    {
        using var alone = new CupertinoThemeTestHarness(Wrap(Sheet(cancelButton: Action("Cancel"))));
        alone.Pump(PhoneSize);
        // Only the two edge paddings; no cancel padding above.
        Assert.Equal(57.17 + 16.0, SheetBox(alone).Size.Height, precision: 2);

        using var withActions = new CupertinoThemeTestHarness(Wrap(
            new Column(children:
            [
                Sheet(actions: [Action("One")], cancelButton: Action("Cancel")),
            ])));
        withActions.Pump(PhoneSize);
        Assert.Equal((57.17 * 2) + 8.0 + 16.0, SheetBox(withActions).Size.Height, precision: 2);
    }

    [Fact]
    public void Haptics_FireOnSlidingIntoAButtonButNotOnPointerDown()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
            using var platform = new MockMethodCallHandler(SystemChannels.Platform);
            using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(actions:
            [
                Action("One"), Action("Two"),
            ])));
            harness.Pump(PhoneSize);

            PointerDown(harness, CenterOf(Paragraph(harness, "One")));
            harness.Pump(PhoneSize);
            Assert.Empty(platform.Log);

            PointerMove(harness, CenterOf(Paragraph(harness, "Two")));
            harness.Pump(PhoneSize);
            Assert.Equal(["HapticFeedback.vibrate"], platform.Methods);
            Assert.Equal("HapticFeedbackType.selectionClick", platform.Log[0].Arguments);

            PointerUp(harness, CenterOf(Paragraph(harness, "Two")));
            Assert.Single(platform.Log);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void Haptics_StaySilentOnDesktopPlatforms()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        try
        {
            PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
            using var platform = new MockMethodCallHandler(SystemChannels.Platform);
            using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(actions:
            [
                Action("One"), Action("Two"),
            ])));
            harness.Pump(PhoneSize);

            PointerDown(harness, CenterOf(Paragraph(harness, "One")));
            PointerMove(harness, CenterOf(Paragraph(harness, "Two")));
            harness.Pump(PhoneSize);

            Assert.Empty(platform.Log);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void FocusHighlight_TintsTheActionBackground()
    {
        using var light = new CupertinoThemeTestHarness(Wrap(Sheet(actions: [Action("One")])));
        light.Pump(PhoneSize);
        Assert.DoesNotContain(DecorationColors(light), color => color == TintedFocus(0xFF007AFF, 0.12));

        Assert.Single(light.FindWidgets<FocusableActionDetector>()).OnShowFocusHighlight!(true);
        light.Pump(PhoneSize);
        Assert.Contains(DecorationColors(light), color => color == TintedFocus(0xFF007AFF, 0.12));

        using var dark = new CupertinoThemeTestHarness(Wrap(
            Sheet(actions: [Action("One")]),
            brightness: PlatformBrightness.Dark));
        dark.Pump(PhoneSize);
        Assert.Single(dark.FindWidgets<FocusableActionDetector>()).OnShowFocusHighlight!(true);
        dark.Pump(PhoneSize);
        Assert.Contains(DecorationColors(dark), color => color == TintedFocus(0xFF007AFF, 0.26));

        using var custom = new CupertinoThemeTestHarness(Wrap(
            Sheet(actions:
            [
                new CupertinoActionSheetAction(
                    new Text("One"), () => { }, focusColor: Color.FromUInt32(0xFFFFAAAA)),
            ]),
            brightness: PlatformBrightness.Dark));
        custom.Pump(PhoneSize);
        Assert.Single(custom.FindWidgets<FocusableActionDetector>()).OnShowFocusHighlight!(true);
        custom.Pump(PhoneSize);
        Assert.Contains(DecorationColors(custom), color => color == TintedFocus(0xFFFFAAAA, 0.26));
    }

    [Fact]
    public void ActivateIntent_InvokesOnPressedAndSendsATapSemanticsEvent()
    {
        int presses = 0;
        var events = new List<SemanticsEvent>();
        void Recorder(SemanticsEvent semanticsEvent) => events.Add(semanticsEvent);
        SemanticsService.SemanticsEventRequested += Recorder;
        try
        {
            using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(actions:
            [
                new CupertinoActionSheetAction(new Text("One"), () => presses++),
            ])));
            harness.Pump(PhoneSize);

            FocusableActionDetector detector = Assert.Single(harness.FindWidgets<FocusableActionDetector>());
            FlutterAction action = detector.Actions![typeof(ActivateIntent)];
            Assert.IsType<CallbackAction<ActivateIntent>>(action).Invoke(new ActivateIntent());

            Assert.Equal(1, presses);
            Assert.Single(events, semanticsEvent => semanticsEvent is TapSemanticEvent);
        }
        finally
        {
            SemanticsService.SemanticsEventRequested -= Recorder;
        }
    }

    [Fact]
    public void MouseCursor_DefersByDefaultAndHonoursAnOverride()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(actions:
        [
            Action("One"),
            new CupertinoActionSheetAction(
                new Text("Two"), () => { }, mouseCursor: SystemMouseCursors.Grab),
        ])));
        harness.Pump(PhoneSize);

        IReadOnlyList<MouseRegion> regions = harness.FindWidgets<MouseRegion>();
        Assert.Contains(regions, region => region.Cursor == SystemMouseCursors.Grab);
        Assert.Contains(
            regions,
            region => region.Cursor == (PlatformDefaults.IsWeb ? SystemMouseCursors.Click : MouseCursor.Defer));
    }

    [Fact]
    public void Semantics_ExposeTheDialogRouteScrollableSectionsAndButtons()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(
            title: new Text("The title"),
            message: new Text("The message"),
            actions: [Action("One"), Action("Two")],
            cancelButton: Action("Cancel"))));
        SemanticsNode? root = harness.PumpAndGetSemantics(PhoneSize);

        SemanticsNode alert = FindSemantics(root, node =>
            node.Label == "Alert"
            && node.Role == SemanticsRole.Dialog
            && node.Flags.HasFlag(SemanticsFlags.ScopesRoute)
            && node.Flags.HasFlag(SemanticsFlags.NamesRoute))!;
        Assert.NotNull(alert);

        foreach (string label in new[] { "One", "Two", "Cancel" })
        {
            SemanticsNode? button = FindSemantics(root, node =>
                node.Label == label && node.Flags.HasFlag(SemanticsFlags.IsButton));
            Assert.NotNull(button);
        }
    }

    private static double ActionsSectionHeight(int actionCount)
    {
        Widget[] actions = Enumerable.Range(0, actionCount)
            .Select(index => Action($"Button {index}"))
            .ToArray();
        using var harness = new CupertinoThemeTestHarness(Wrap(Sheet(
            title: new Text("The title"),
            message: new Text(string.Concat(Enumerable.Repeat("Very long content", 200))),
            actions: actions,
            cancelButton: Action("Cancel"))));
        harness.Pump(PhoneSize);

        RenderPriorityColumn column = Descendants<RenderPriorityColumn>(harness.RenderView)[0];
        RenderBox? bottom = null;
        RenderBox? previous = null;
        column.VisitChildren(child =>
        {
            bottom = previous is null ? null : (RenderBox)child;
            previous ??= (RenderBox)child;
        });
        // The bottom slot is the section divider plus the scrollable actions viewport.
        return bottom!.Size.Height - 0.3;
    }

    private static CupertinoActionSheet Sheet(
        Widget? title = null,
        Widget? message = null,
        IReadOnlyList<Widget>? actions = null,
        Widget? cancelButton = null)
    {
        return new CupertinoActionSheet(
            title: title,
            message: message,
            actions: actions,
            cancelButton: cancelButton);
    }

    private static CupertinoActionSheetAction Action(string label, bool destructive = false) =>
        new(new Text(label), () => { }, isDestructiveAction: destructive);

    private static Widget Wrap(
        Widget child,
        double textScaleFactor = 1.0,
        Size? size = null,
        Thickness viewPadding = default,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(
                    Size: size ?? PhoneSize,
                    TextScaleFactor: textScaleFactor,
                    Padding: viewPadding,
                    ViewPadding: viewPadding,
                    PlatformBrightness: brightness),
                new CupertinoTheme(
                    new CupertinoThemeData(brightness: brightness),
                    new Localizations(
                        locale: new Locale("en", "US"),
                        delegates:
                        [
                            DefaultWidgetsLocalizations.Delegate,
                            DefaultCupertinoLocalizations.Delegate,
                        ],
                        child: new Align(alignment: Alignment.BottomCenter, child: child)))));
    }

    private static Color TintedFocus(uint baseColor, double opacity)
    {
        Color color = Color.FromUInt32(baseColor);
        byte alpha = (byte)Math.Round(byte.MaxValue * opacity);
        return HSLColor.FromColor(Color.FromArgb(alpha, color.R, color.G, color.B)).ToColor();
    }

    private static List<Color> DecorationColors(CupertinoThemeTestHarness harness)
    {
        return Descendants<RenderDecoratedBox>(harness.RenderView)
            .Select(box => box.Decoration)
            .OfType<BoxDecoration>()
            .Where(decoration => decoration.Color is not null)
            .Select(decoration => decoration.Color!.Value)
            .ToList();
    }

    private static RenderBox SheetBox(CupertinoThemeTestHarness harness) =>
        (RenderBox)harness.FindRenderObject<CupertinoActionSheet>();

    private static double ActionBottom(CupertinoThemeTestHarness harness, string label)
    {
        RenderObject? node = Paragraph(harness, label);
        while (node is not null)
        {
            if (node is RenderMetaData metaData && metaData.MetaData is CupertinoActionSheetActionState)
            {
                return metaData.GetPaintOffsetToRoot().Y + metaData.Size.Height;
            }

            node = node.Parent;
        }

        throw new InvalidOperationException($"No action sheet action renders the label '{label}'.");
    }

    private static void PointerDown(CupertinoThemeTestHarness harness, Point position) =>
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: position,
            buttons: PointerButtons.Primary, timestampUtc: DateTime.UtcNow));

    private static void PointerMove(CupertinoThemeTestHarness harness, Point position) =>
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: position,
            buttons: PointerButtons.Primary, down: true, timestampUtc: DateTime.UtcNow));

    private static void PointerUp(CupertinoThemeTestHarness harness, Point position) =>
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: position,
            buttons: PointerButtons.None, timestampUtc: DateTime.UtcNow));

    private static Point CenterOf(RenderBox box)
    {
        Point origin = box.GetPaintOffsetToRoot();
        return new Point(origin.X + (box.Size.Width / 2), origin.Y + (box.Size.Height / 2));
    }

    private static Color ParagraphColor(RenderParagraph paragraph) =>
        ((ISolidColorBrush)paragraph.Foreground).Color;

    private static RenderParagraph Paragraph(CupertinoThemeTestHarness harness, string text) =>
        Descendants<RenderParagraph>(harness.RenderView).First(paragraph => paragraph.PlainText == text);

    private static List<T> Descendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T target)
        {
            result.Add(target);
        }

        root.VisitChildren(child => result.AddRange(Descendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null || predicate(node))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? result = FindSemantics(child, predicate);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
