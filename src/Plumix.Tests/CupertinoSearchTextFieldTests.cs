using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/search_field_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoSearchTextFieldTests : IDisposable
{
    private static readonly Size ViewSize = new(320.0, 80.0);

    public CupertinoSearchTextFieldTests() => FocusManager.Instance.ResetForTests();

    public void Dispose() => FocusManager.Instance.ResetForTests();

    [Fact]
    public void Constructor_ExposesFlutterDefaultsAndGuardsDecorationShorthands()
    {
        var field = new CupertinoSearchTextField();

        Assert.Null(field.Controller);
        Assert.Null(field.Placeholder);
        Assert.Null(field.Decoration);
        Assert.Null(field.BackgroundColor);
        Assert.Null(field.BorderRadius);
        Assert.Equal(TextInputType.Text, field.KeyboardType);
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 5.5, top: 8.0, end: 5.5, bottom: 8.0),
            field.Padding);
        Assert.Same(CupertinoColors.SecondaryLabel, field.ItemColor);
        Assert.Equal(20.0, field.ItemSize);
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 6.0, top: 8.0, bottom: 8.0),
            field.PrefixInsets);
        Assert.Equal(CupertinoIcons.Search, Assert.IsType<Icon>(field.PrefixIcon).IconData);
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(top: 8.0, end: 5.0, bottom: 8.0),
            field.SuffixInsets);
        Assert.Equal(CupertinoIcons.XmarkCircleFill, field.SuffixIcon.IconData);
        Assert.Equal(OverlayVisibilityMode.Editing, field.SuffixMode);
        Assert.True(field.EnableIMEPersonalizedLearning);
        Assert.False(field.Autofocus);
        Assert.True(field.Autocorrect);
        Assert.Null(field.Enabled);
        Assert.Equal(2.0, field.CursorWidth);
        Assert.Null(field.CursorHeight);
        Assert.Equal(Radius.Circular(2.0), field.CursorRadius);
        Assert.True(field.CursorOpacityAnimates);

        var decoration = new BoxDecoration();
        Assert.Throws<ArgumentException>(() => new CupertinoSearchTextField(
            decoration: decoration,
            backgroundColor: CupertinoColors.SystemRed));
        Assert.Throws<ArgumentException>(() => new CupertinoSearchTextField(
            decoration: decoration,
            borderRadius: BorderRadius.Zero));
    }

    [Theory]
    [InlineData(PlatformBrightness.Light, 0x1E767680u, 0x993C3C43u)]
    [InlineData(PlatformBrightness.Dark, 0x3D767680u, 0x99EBEBF5u)]
    public void Build_ResolvesDefaultDecorationPlaceholderAndIconColors(
        PlatformBrightness brightness,
        uint background,
        uint secondaryLabel)
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSearchTextField(suffixMode: OverlayVisibilityMode.Always),
            brightness));

        harness.Pump(ViewSize);

        CupertinoTextField inner = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        BoxDecoration decoration = Assert.IsType<BoxDecoration>(inner.Decoration);
        Assert.Equal(Color.FromUInt32(background), decoration.Color);
        Assert.Equal(BorderRadius.Circular(9.0), decoration.BorderRadius);
        Assert.Equal(TextInputActionType.Search, inner.TextInputAction);

        Text placeholder = Assert.Single(harness.FindWidgets<Text>(), text => text.Data == "Search");
        Assert.Equal(Color.FromUInt32(secondaryLabel), placeholder.Style?.Color);

        IconTheme prefixTheme = Assert.Single(harness.FindWidgets<IconTheme>(), theme =>
            theme.Child is Icon icon && icon.IconData == CupertinoIcons.Search);
        IconTheme suffixTheme = Assert.Single(harness.FindWidgets<IconTheme>(), theme =>
            theme.Child is Icon icon && icon.IconData == CupertinoIcons.XmarkCircleFill);
        Assert.Equal(Color.FromUInt32(secondaryLabel), prefixTheme.Data.Color);
        Assert.Equal(Color.FromUInt32(secondaryLabel), suffixTheme.Data.Color);
        Assert.Equal(20.0, prefixTheme.Data.Size);
        Assert.Equal(20.0, suffixTheme.Data.Size);
    }

    [Fact]
    public void Build_ForwardsEditableConfigurationAndCustomStyles()
    {
        var controller = new TextEditingController("query");
        using var focusNode = new FocusNode();
        int tapped = 0;
        int submitted = 0;
        var style = new TextStyle(Color: Color.FromUInt32(0xFF123456), FontWeight: FontWeight.Light);
        var placeholderStyle = new TextStyle(
            Color: Color.FromUInt32(0xAA654321),
            FontWeight: FontWeight.Bold);
        var cursorColor = CupertinoDynamicColor.WithBrightness(Colors.Red, Colors.Blue);
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            controller: controller,
            focusNode: focusNode,
            keyboardType: TextInputType.Number,
            style: style,
            placeholder: "Find",
            placeholderStyle: placeholderStyle,
            onSubmitted: _ => submitted++,
            onTap: () => tapped++,
            autocorrect: false,
            enabled: false,
            autofocus: true,
            smartQuotesType: SmartQuotesType.Disabled,
            smartDashesType: SmartDashesType.Disabled,
            enableIMEPersonalizedLearning: false,
            cursorWidth: 1.0,
            cursorHeight: 10.0,
            cursorRadius: Radius.Circular(1.0),
            cursorOpacityAnimates: false,
            cursorColor: cursorColor)));

        harness.Pump(ViewSize);

        CupertinoTextField inner = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        Assert.Same(controller, inner.Controller);
        Assert.Same(focusNode, inner.FocusNode);
        Assert.Equal(TextInputType.Number, inner.KeyboardType);
        Assert.Same(style, inner.Style);
        Assert.Equal(placeholderStyle, inner.PlaceholderStyle);
        Assert.NotNull(inner.OnSubmitted);
        inner.OnSubmitted!("query");
        Assert.Equal(1, submitted);
        Assert.NotNull(inner.OnTap);
        inner.OnTap!();
        Assert.Equal(1, tapped);
        Assert.False(inner.Autocorrect);
        Assert.False(inner.Enabled);
        Assert.True(inner.Autofocus);
        Assert.Equal(SmartQuotesType.Disabled, inner.SmartQuotesType);
        Assert.Equal(SmartDashesType.Disabled, inner.SmartDashesType);
        Assert.False(inner.EnableIMEPersonalizedLearning);
        Assert.Equal(1.0, inner.CursorWidth);
        Assert.Equal(10.0, inner.CursorHeight);
        Assert.Equal(Radius.Circular(1.0), inner.CursorRadius);
        Assert.False(inner.CursorOpacityAnimates);
        Assert.Same(cursorColor, inner.CursorColor);
        Assert.False(focusNode.HasFocus);
    }

    [Fact]
    public void DecorationAndInsets_UseOverridesAndRemainDirectional()
    {
        var decoration = new BoxDecoration(
            Color: Color.FromUInt32(0xFF123456),
            BorderRadius: BorderRadius.Zero);
        EdgeInsetsGeometry prefixInsets = EdgeInsetsGeometry.DirectionalOnly(start: 1.0, end: 2.0);
        EdgeInsetsGeometry suffixInsets = EdgeInsetsGeometry.DirectionalOnly(start: 3.0, end: 4.0);
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            controller: new TextEditingController("query"),
            decoration: decoration,
            prefixInsets: prefixInsets,
            suffixInsets: suffixInsets,
            suffixMode: OverlayVisibilityMode.Always)));

        harness.Pump(ViewSize);

        CupertinoTextField inner = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        Assert.Same(decoration, inner.Decoration);
        Padding prefix = Assert.Single(harness.FindWidgets<Padding>(), padding =>
            padding.Child is IconTheme theme
            && theme.Child is Icon icon
            && icon.IconData == CupertinoIcons.Search);
        Padding suffix = Assert.Single(harness.FindWidgets<Padding>(), padding =>
            padding.Child is CupertinoButton);
        Assert.Equal(prefixInsets, prefix.InsetsGeometry);
        Assert.Equal(suffixInsets, suffix.InsetsGeometry);

        using var shorthand = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            backgroundColor: CupertinoColors.SystemRed,
            borderRadius: BorderRadius.Circular(3.0))));
        shorthand.Pump(ViewSize);
        BoxDecoration shorthandDecoration = Assert.IsType<BoxDecoration>(
            Assert.Single(shorthand.FindWidgets<CupertinoTextField>()).Decoration);
        Assert.Equal(CupertinoColors.SystemRed.Color, shorthandDecoration.Color);
        Assert.Equal(BorderRadius.Circular(3.0), shorthandDecoration.BorderRadius);
    }

    [Fact]
    public void Attachments_RespectVisibilityAndDefaultClearBehavior()
    {
        var controller = new TextEditingController();
        var changes = new List<string>();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            controller: controller,
            onChanged: changes.Add)));

        harness.Pump(ViewSize);
        Assert.Single(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.Search);
        Assert.DoesNotContain(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.XmarkCircleFill);

        controller.Text = "query";
        harness.Pump(ViewSize);
        Assert.Single(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.Search);
        Assert.Single(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.XmarkCircleFill);

        CupertinoButton suffix = Assert.Single(harness.FindWidgets<CupertinoButton>());
        suffix.OnPressed!();
        harness.Pump(ViewSize);
        Assert.Equal(string.Empty, controller.Text);
        Assert.Equal([string.Empty], changes);
        Assert.DoesNotContain(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.XmarkCircleFill);

        suffix.OnPressed!();
        Assert.Equal([string.Empty], changes);
    }

    [Fact]
    public void SuffixModesAndCustomTap_MatchFlutterVisibilityRules()
    {
        var controller = new TextEditingController();
        int taps = 0;
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            controller: controller,
            suffixMode: OverlayVisibilityMode.NotEditing,
            onSuffixTap: () => taps++)));

        harness.Pump(ViewSize);
        Assert.Single(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.XmarkCircleFill);
        Assert.Single(harness.FindWidgets<CupertinoButton>()).OnPressed!();
        Assert.Equal(1, taps);

        controller.Text = "query";
        harness.Pump(ViewSize);
        Assert.DoesNotContain(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.XmarkCircleFill);
        Assert.Equal("query", controller.Text);
    }

    [Fact]
    public void ControllerAndFocusNode_HandoffPreservesStateAndAccessibilityPrefixRule()
    {
        using var externalFocusNode = new FocusNode();
        var externalController = new TextEditingController("external");
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            controller: externalController,
            focusNode: externalFocusNode,
            itemSize: 10.0),
            textScaleFactor: 3.0));

        harness.Pump(ViewSize);
        IconTheme prefix = PrefixTheme(harness);
        Assert.Equal(30.0, prefix.Data.Size);

        externalFocusNode.RequestFocus();
        harness.Pump(ViewSize);
        Assert.Equal(0.0, PrefixTheme(harness).Data.Size);

        harness.PumpWidget(Wrap(new CupertinoSearchTextField(itemSize: 10.0), textScaleFactor: 2.9));
        harness.Pump(ViewSize);
        CupertinoTextField inner = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        Assert.Equal("external", inner.Controller!.Text);
        Assert.Equal(29.0, PrefixTheme(harness).Data.Size);

        harness.PumpWidget(Wrap(new CupertinoSearchTextField(controller: externalController)));
        harness.Pump(ViewSize);
        Assert.Same(externalController, Assert.Single(harness.FindWidgets<CupertinoTextField>()).Controller);
    }

    [Fact]
    public void Autofocus_RequestsFocusThroughTheInnerTextField()
    {
        using var focusNode = new FocusNode();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            focusNode: focusNode,
            autofocus: true)));

        harness.Pump(ViewSize);

        Assert.True(focusNode.HasFocus);
    }

    [Fact]
    public void ScrollResize_FadesIconsAndPlaceholderAndHalvesTopInsets()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            suffixMode: OverlayVisibilityMode.Always)));

        harness.Pump(new Size(320.0, 36.0));
        CupertinoSearchTextFieldState state = harness.FindState<CupertinoSearchTextFieldState>();
        Assert.Equal(1.0, PrefixOpacity(harness).Value);
        Assert.Equal(8.0, Assert.Single(harness.FindWidgets<CupertinoTextField>()).Padding.Top);

        harness.Layout(new Size(320.0, 32.0));
        DispatchScrollUpdate(state.Context);
        harness.Pump(new Size(320.0, 32.0));
        Assert.InRange(PrefixOpacity(harness).Value, 0.01, 0.99);
        Assert.InRange(Assert.Single(harness.FindWidgets<CupertinoTextField>()).Padding.Top, 4.01, 7.99);

        harness.Layout(new Size(320.0, 20.0));
        DispatchScrollUpdate(state.Context);
        harness.Pump(new Size(320.0, 20.0));

        Assert.Equal(0.0, PrefixOpacity(harness).Value);
        Assert.Equal(4.0, Assert.Single(harness.FindWidgets<CupertinoTextField>()).Padding.Top);
        Text placeholder = Assert.Single(harness.FindWidgets<Text>(), text => text.Data == "Search");
        Assert.Equal((byte)0, placeholder.Style!.Color!.Value.A);
    }

    [Theory]
    [InlineData(TextDirection.Ltr, false)]
    [InlineData(TextDirection.Rtl, true)]
    public void Layout_MirrorsPrefixAndSuffix(TextDirection direction, bool prefixAfterSuffix)
    {
        var controller = new TextEditingController("query");
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoSearchTextField(controller: controller),
            direction: direction));

        harness.Pump(ViewSize);

        string prefixGlyph = char.ConvertFromUtf32(CupertinoIcons.Search.CodePoint);
        string suffixGlyph = char.ConvertFromUtf32(CupertinoIcons.XmarkCircleFill.CodePoint);
        RenderParagraph prefix = Assert.Single(FindAll<RenderParagraph>(harness.RenderView), paragraph =>
            paragraph.PlainText == prefixGlyph);
        RenderParagraph suffix = Assert.Single(FindAll<RenderParagraph>(harness.RenderView), paragraph =>
            paragraph.PlainText == suffixGlyph);
        Assert.Equal(prefixAfterSuffix, prefix.LocalToGlobal(default).X > suffix.LocalToGlobal(default).X);
    }

    [Fact]
    public void ZeroArea_RemainsStableWhenSelectionChanges()
    {
        var controller = new TextEditingController("X");
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoSearchTextField(
            controller: controller)));

        harness.Pump(default);
        controller.Selection = TextSelection.Collapsed(0);
        harness.Pump(default);

        Assert.Equal(default(Size), harness.RenderView.Child?.Size);
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        TextDirection direction = TextDirection.Ltr,
        double textScaleFactor = 1.0)
    {
        return new MediaQuery(
            data: new MediaQueryData(
                PlatformBrightness: brightness,
                DevicePixelRatio: 1.0,
                TextScaler: TextScaler.Linear(textScaleFactor)),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    direction,
                    new CupertinoTheme(
                        new CupertinoThemeData(brightness: brightness),
                        new ScrollNotificationObserver(new Center(child: child))))));
    }

    private static IconTheme PrefixTheme(CupertinoThemeTestHarness harness)
    {
        return Assert.Single(harness.FindWidgets<IconTheme>(), theme =>
            theme.Child is Icon icon && icon.IconData == CupertinoIcons.Search);
    }

    private static Opacity PrefixOpacity(CupertinoThemeTestHarness harness)
    {
        return Assert.Single(harness.FindWidgets<Opacity>(), opacity =>
            opacity.Child is Padding { Child: IconTheme theme }
            && theme.Child is Icon icon
            && icon.IconData == CupertinoIcons.Search);
    }

    private static void DispatchScrollUpdate(BuildContext context)
    {
        var metrics = new FixedScrollMetrics(
            minScrollExtent: 0.0,
            maxScrollExtent: 100.0,
            pixels: 1.0,
            viewportDimension: 20.0,
            axisDirection: AxisDirection.Down,
            devicePixelRatio: 1.0);
        new ScrollUpdateNotification(metrics).Dispatch(context);
    }

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }
        if (root is T typed)
        {
            result.Add(typed);
        }
        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }
}
