using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports the text spacing / bold text override cases of
// flutter/packages/flutter/test/widgets/media_query_test.dart ('MediaQueryData.applyDisplayCornerRadii
// applies specified display corner radii', 'MediaQuery.applyTextStyleOverrides applies the specified
// text style overrides', 'MediaQuery.boldTextOf', the matching 'MediaQuery partial dependencies'
// variants) and text_test.dart ('Text respects ...Override...', 'Text.rich respects ...Override...',
// 'RichText ignores ...Override...'), plus the dart:ui / flutter_test dispatcher side of the overrides.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TextSpacingOverridesDartParityTests
{
    private static readonly MediaQueryData OverrideData = new(
        LineHeightScaleFactorOverride: 2.0,
        LetterSpacingOverride: 2.0,
        WordSpacingOverride: 2.0);

    // Flutter: 'media_query_test.dart: MediaQueryData.applyDisplayCornerRadii applies specified display
    // corner radii'
    [Fact]
    public void ApplyDisplayCornerRadiiAppliesSpecifiedDisplayCornerRadii()
    {
        using var tester = new FrameworkDartTester();
        var data = new MediaQueryData(DisplayCornerRadii: BorderRadius.All(Radius.Circular(33)));
        MediaQueryData? updatedData = null;
        BorderRadius? radii = BorderRadius.All(Radius.Circular(99));

        Widget Build() => new MediaQuery(
            data: data,
            child: new Builder(context => new MediaQuery(
                data: MediaQuery.Of(context).ApplyDisplayCornerRadii(radii),
                child: new Builder(innerContext =>
                {
                    updatedData = MediaQuery.Of(innerContext);
                    return new Placeholder();
                }))));

        tester.PumpWidget(Build());
        AssertSameExceptCornerRadii(data, updatedData!);
        Assert.Equal(BorderRadius.All(Radius.Circular(99)), updatedData!.DisplayCornerRadii);

        radii = null;
        tester.PumpWidget(Build());
        AssertSameExceptCornerRadii(data, updatedData!);
        Assert.Null(updatedData!.DisplayCornerRadii);
    }

    private static void AssertSameExceptCornerRadii(MediaQueryData data, MediaQueryData updatedData)
    {
        Assert.Equal(data.Size, updatedData.Size);
        Assert.Equal(data.DevicePixelRatio, updatedData.DevicePixelRatio);
        Assert.Equal(data.TextScaler, updatedData.TextScaler);
        Assert.Equal(data.Padding, updatedData.Padding);
        Assert.Equal(data.ViewPadding, updatedData.ViewPadding);
        Assert.Equal(data.ViewInsets, updatedData.ViewInsets);
        Assert.Equal(data.SystemGestureInsets, updatedData.SystemGestureInsets);
        Assert.Equal(data.AlwaysUse24HourFormat, updatedData.AlwaysUse24HourFormat);
        Assert.Equal(data.AccessibleNavigation, updatedData.AccessibleNavigation);
        Assert.Equal(data.InvertColors, updatedData.InvertColors);
        Assert.Equal(data.HighContrast, updatedData.HighContrast);
        Assert.Equal(data.OnOffSwitchLabels, updatedData.OnOffSwitchLabels);
        Assert.Equal(data.DisableAnimations, updatedData.DisableAnimations);
        Assert.Equal(data.BoldText, updatedData.BoldText);
        Assert.Equal(data.SupportsAnnounce, updatedData.SupportsAnnounce);
        Assert.Equal(data.NavigationMode, updatedData.NavigationMode);
        Assert.Equal(data.GestureSettings, updatedData.GestureSettings);
        Assert.Equal(data.DisplayFeatures, updatedData.DisplayFeatures);
        Assert.Equal(data.SupportsShowingSystemContextMenu, updatedData.SupportsShowingSystemContextMenu);
        Assert.Equal(data.LineHeightScaleFactorOverride, updatedData.LineHeightScaleFactorOverride);
        Assert.Equal(data.LetterSpacingOverride, updatedData.LetterSpacingOverride);
        Assert.Equal(data.WordSpacingOverride, updatedData.WordSpacingOverride);
        Assert.Equal(data.ParagraphSpacingOverride, updatedData.ParagraphSpacingOverride);
    }

    // Flutter: 'media_query_test.dart: MediaQuery.applyTextStyleOverrides applies the specified text
    // style overrides'
    [Fact]
    public void ApplyTextStyleOverridesAppliesTheSpecifiedTextStyleOverrides()
    {
        using var tester = new FrameworkDartTester();
        MediaQueryData? insideData = null;
        double? value = 2.0;

        Widget Build() => new MediaQuery(
            data: new MediaQueryData(
                LineHeightScaleFactorOverride: 9.0,
                LetterSpacingOverride: 9.0,
                WordSpacingOverride: 9.0,
                ParagraphSpacingOverride: 9.0),
            child: MediaQuery.ApplyTextStyleOverrides(
                lineHeightScaleFactorOverride: value,
                letterSpacingOverride: value,
                wordSpacingOverride: value,
                paragraphSpacingOverride: value,
                child: new Builder(context =>
                {
                    insideData = MediaQuery.Of(context);
                    return new Placeholder();
                })));

        tester.PumpWidget(Build());
        Assert.Equal(2.0, insideData!.LineHeightScaleFactorOverride);
        Assert.Equal(2.0, insideData.LetterSpacingOverride);
        Assert.Equal(2.0, insideData.WordSpacingOverride);
        Assert.Equal(2.0, insideData.ParagraphSpacingOverride);

        tester.PumpAndSettle();
        value = null;
        tester.PumpWidget(Build());
        // A null argument clears the override; it does not inherit the outer 9.0.
        Assert.Null(insideData!.LineHeightScaleFactorOverride);
        Assert.Null(insideData.LetterSpacingOverride);
        Assert.Null(insideData.WordSpacingOverride);
        Assert.Null(insideData.ParagraphSpacingOverride);
    }

    // Flutter: 'media_query_test.dart: MediaQuery.boldTextOf'
    [Fact]
    public void MediaQueryBoldTextOf()
    {
        using var tester = new FrameworkDartTester();
        bool? outsideBoldText = null;
        bool? insideBoldText = null;

        tester.PumpWidget(new Builder(context =>
        {
            outsideBoldText = MediaQuery.BoldTextOf(context);
            return new MediaQuery(
                data: new MediaQueryData(BoldText: true),
                child: new Builder(innerContext =>
                {
                    insideBoldText = MediaQuery.BoldTextOf(innerContext);
                    return new Placeholder();
                }));
        }));

        Assert.False(outsideBoldText);
        Assert.True(insideBoldText);
    }

    // Flutter: 'media_query_test.dart: MediaQuery partial dependencies', the boldText and text spacing
    // override variants: a dependent rebuilds when its aspect changes, and not for another field.
    [Theory]
    [InlineData("boldText")]
    [InlineData("maybeBoldText")]
    [InlineData("lineHeightScaleFactorOverride")]
    [InlineData("letterSpacingOverride")]
    [InlineData("wordSpacingOverride")]
    [InlineData("paragraphSpacingOverride")]
    public void MediaQueryPartialDependencies(string aspect)
    {
        using var tester = new FrameworkDartTester();
        var data = new MediaQueryData();
        int buildCount = 0;
        StateSetter? setPageState = null;

        Widget builder = new Builder(context =>
        {
            _ = aspect switch
            {
                "boldText" => (object)MediaQuery.BoldTextOf(context),
                "maybeBoldText" => MediaQuery.MaybeBoldTextOf(context),
                "lineHeightScaleFactorOverride" => MediaQuery.MaybeLineHeightScaleFactorOverrideOf(context),
                "letterSpacingOverride" => MediaQuery.MaybeLetterSpacingOverrideOf(context),
                "wordSpacingOverride" => MediaQuery.MaybeWordSpacingOverrideOf(context),
                _ => MediaQuery.MaybeParagraphSpacingOverrideOf(context),
            };
            buildCount++;
            return new Placeholder();
        });

        tester.PumpWidget(new StatefulBuilder((_, setState) =>
        {
            setPageState = setState;
            return new MediaQuery(data: data, child: builder);
        }));
        Assert.Equal(1, buildCount);

        // Another field changes: no rebuild.
        setPageState!(() => data = data.CopyWith(highContrast: true));
        tester.Pump();
        Assert.Equal(1, buildCount);

        // The aspect changes: rebuild.
        setPageState(() => data = aspect switch
        {
            "boldText" or "maybeBoldText" => data.CopyWith(boldText: true),
            _ => data.ApplyTextStyleOverrides(
                lineHeightScaleFactorOverride: aspect == "lineHeightScaleFactorOverride" ? 2.0 : null,
                letterSpacingOverride: aspect == "letterSpacingOverride" ? 2.0 : null,
                wordSpacingOverride: aspect == "wordSpacingOverride" ? 2.0 : null,
                paragraphSpacingOverride: aspect == "paragraphSpacingOverride" ? 2.0 : null),
        });
        tester.Pump();
        Assert.Equal(2, buildCount);
    }

    [Fact]
    public void MediaQueryDataCarriesTheOverridesThroughCopiesAndEquality()
    {
        var data = new MediaQueryData(
            LineHeightScaleFactorOverride: 1.5,
            LetterSpacingOverride: 1.0,
            WordSpacingOverride: 3.0,
            ParagraphSpacingOverride: 12.0);

        // Dart's copyWith has no override parameters and passes them through, as do the remove* copies.
        foreach (MediaQueryData copy in new[]
                 {
                     data.CopyWith(boldText: true),
                     data.RemovePadding(removeTop: true),
                     data.RemoveViewInsets(removeTop: true),
                     data.RemoveViewPadding(removeTop: true),
                 })
        {
            Assert.Equal(1.5, copy.LineHeightScaleFactorOverride);
            Assert.Equal(1.0, copy.LetterSpacingOverride);
            Assert.Equal(3.0, copy.WordSpacingOverride);
            Assert.Equal(12.0, copy.ParagraphSpacingOverride);
        }

        Assert.Equal(data, new MediaQueryData(
            LineHeightScaleFactorOverride: 1.5,
            LetterSpacingOverride: 1.0,
            WordSpacingOverride: 3.0,
            ParagraphSpacingOverride: 12.0));
        Assert.NotEqual(data, data.ApplyTextStyleOverrides(1.5, 1.0, 3.0, paragraphSpacingOverride: null));
    }

    // dart:ui: the overrides change through onMetricsChanged; flutter_test's test values replace them
    // and notify; MediaQueryData.fromView reads them unless the parent data carries its own.
    [Fact]
    public void FromViewReadsTheDispatcherOverridesAndUpdatesOnMetricsChange()
    {
        using var tester = new FrameworkDartTester();
        TestPlatformDispatcher dispatcher = tester.PlatformDispatcher;
        double? letterSpacing = -1.0;
        double? lineHeight = -1.0;

        tester.PumpWidget(new Builder(context =>
        {
            letterSpacing = MediaQuery.MaybeLetterSpacingOverrideOf(context);
            lineHeight = MediaQuery.MaybeLineHeightScaleFactorOverrideOf(context);
            return new Placeholder();
        }));
        Assert.Null(letterSpacing);
        Assert.Null(lineHeight);

        int metricsChanges = 0;
        Action? previous = dispatcher.OnMetricsChanged;
        dispatcher.OnMetricsChanged = () =>
        {
            metricsChanges++;
            previous?.Invoke();
        };
        try
        {
            dispatcher.LetterSpacingOverrideTestValue = 4.0;
            dispatcher.LineHeightScaleFactorOverrideTestValue = 1.5;
            Assert.Equal(2, metricsChanges);
            tester.Pump();
            Assert.Equal(4.0, letterSpacing);
            Assert.Equal(1.5, lineHeight);

            MediaQueryData parent = new MediaQueryData(LetterSpacingOverride: 7.0);
            Assert.Equal(7.0, MediaQueryData.FromView(tester.View, platformData: parent).LetterSpacingOverride);
            // A parent without the override falls back to the dispatcher.
            Assert.Equal(1.5, MediaQueryData.FromView(tester.View, platformData: parent).LineHeightScaleFactorOverride);

            dispatcher.ClearLetterSpacingOverrideTestValue();
            dispatcher.ClearLineHeightScaleFactorOverrideTestValue();
            Assert.Equal(4, metricsChanges);
            tester.Pump();
            Assert.Null(letterSpacing);
            Assert.Null(lineHeight);
        }
        finally
        {
            dispatcher.OnMetricsChanged = previous;
        }
    }

    [Fact]
    public void TestValuesForceNullAndTheEngineUpdateNotifiesOnlyOnChange()
    {
        using var tester = new FrameworkDartTester();
        TestPlatformDispatcher dispatcher = tester.PlatformDispatcher;
        PlatformDispatcher real = PlatformDispatcher.Instance;
        int metricsChanges = 0;
        Action? previous = real.OnMetricsChanged;
        real.OnMetricsChanged = () => metricsChanges++;
        try
        {
            real.UpdateTextSpacingOverrides(2.0, 1.0, 4.0, 10.0);
            Assert.Equal(1, metricsChanges);
            real.UpdateTextSpacingOverrides(2.0, 1.0, 4.0, 10.0);
            Assert.Equal(1, metricsChanges);
            Assert.Equal(2.0, real.LineHeightScaleFactorOverride);
            Assert.Equal(1.0, real.LetterSpacingOverride);
            Assert.Equal(4.0, real.WordSpacingOverride);
            Assert.Equal(10.0, real.ParagraphSpacingOverride);

            // A null test value forces the override to read null over the platform's value.
            dispatcher.WordSpacingOverrideTestValue = null;
            Assert.Null(dispatcher.WordSpacingOverride);
            dispatcher.ParagraphSpacingOverrideTestValue = 3.0;
            Assert.Equal(3.0, dispatcher.ParagraphSpacingOverride);
            dispatcher.ClearAllTestValues();
            Assert.Equal(4.0, dispatcher.WordSpacingOverride);
            Assert.Equal(10.0, dispatcher.ParagraphSpacingOverride);
        }
        finally
        {
            real.UpdateTextSpacingOverrides(null, null, null, null);
            real.OnMetricsChanged = previous;
        }
    }

    // Flutter: 'text_test.dart: Text respects MediaQueryData.lineHeightScaleFactorOverride,
    // MediaQueryData.letterSpacingOverride, and MediaQueryData.wordSpacingOverride'
    [Fact]
    public void TextRespectsTheTextSpacingOverrides()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MediaQuery(
            data: OverrideData,
            child: new Center(child: new Text("Hello", textDirection: TextDirection.Ltr))));

        RichText text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Equal(2.0, text.Text.Style?.Height);
        Assert.Equal(2.0, text.Text.Style?.LetterSpacing);
        Assert.Equal(2.0, text.Text.Style?.WordSpacing);

        tester.PumpWidget(new Center(child: new Text("Hello", textDirection: TextDirection.Ltr)));
        text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Null(text.Text.Style?.Height);
        Assert.Null(text.Text.Style?.LetterSpacing);
        Assert.Null(text.Text.Style?.WordSpacing);
    }

    // Flutter: 'text_test.dart: Text.rich respects MediaQueryData.lineHeightScaleFactorOverride,
    // MediaQueryData.letterSpacingOverride, and MediaQueryData.wordSpacingOverride'
    [Fact]
    public void TextRichRespectsTheTextSpacingOverrides()
    {
        using var tester = new FrameworkDartTester();
        Widget Rich() => Text.Rich(
            new TextSpan(text: "Hello"),
            textDirection: TextDirection.Ltr,
            strutStyle: new StrutStyle(Height: 0.9));

        tester.PumpWidget(new MediaQuery(data: OverrideData, child: new Center(child: Rich())));
        RichText text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Equal(2.0, text.Text.Style?.Height);
        Assert.Equal(2.0, text.Text.Style?.LetterSpacing);
        Assert.Equal(2.0, text.Text.Style?.WordSpacing);
        Assert.Equal(2.0, text.StrutStyle?.Height);

        // The nested span is a plain TextSpan, so it gets the override style too.
        var child = (TextSpan)((TextSpan)text.Text).Children![0];
        Assert.Equal(2.0, child.Style?.Height);

        tester.PumpWidget(new Center(child: Rich()));
        text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Null(text.Text.Style?.Height);
        Assert.Null(text.Text.Style?.LetterSpacing);
        Assert.Null(text.Text.Style?.WordSpacing);
        Assert.Equal(0.9, text.StrutStyle?.Height);
    }

    // Flutter: 'text_test.dart: RichText ignores MediaQueryData.lineHeightScaleFactorOverride,
    // MediaQueryData.letterSpacingOverride, and MediaQueryData.wordSpacingOverride'
    [Fact]
    public void RichTextIgnoresTheTextSpacingOverrides()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MediaQuery(
            data: OverrideData,
            child: new Center(child: new RichText(
                text: new TextSpan(text: "Hello"),
                textDirection: TextDirection.Ltr,
                strutStyle: new StrutStyle(Height: 0.9)))));

        RichText text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Null(text.Text.Style?.Height);
        Assert.Null(text.Text.Style?.LetterSpacing);
        Assert.Null(text.Text.Style?.WordSpacing);
        Assert.Equal(0.9, text.StrutStyle?.Height);
    }

    [Fact]
    public void TextOverridesSkipWidgetSpansAndTextSpanSubclasses()
    {
        using var tester = new FrameworkDartTester();
        var subclassSpan = new SubclassSpan("sub");
        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(LetterSpacingOverride: 3.0),
            child: new Center(child: Text.Rich(
                new TextSpan(children:
                [
                    subclassSpan,
                    new WidgetSpan(child: new SizedBox(width: 10, height: 10)),
                ]),
                textDirection: TextDirection.Ltr))));

        RichText text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        var outer = (TextSpan)text.Text;
        var userSpan = (TextSpan)outer.Children![0];
        Assert.Equal(3.0, outer.Style?.LetterSpacing);
        Assert.Equal(3.0, userSpan.Style?.LetterSpacing);
        // Only spans whose runtime type is exactly TextSpan are rebuilt.
        Assert.Same(subclassSpan, userSpan.Children![0]);
        Assert.IsType<WidgetSpan>(userSpan.Children[1]);
    }

    [Fact]
    public void TextAppliesBoldTextEvenWhenItsStyleDoesNotInherit()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(BoldText: true),
            child: new Center(child: new Text(
                "Hello",
                textDirection: TextDirection.Ltr,
                style: new TextStyle(Inherit: false, FontSize: 12.0, FontWeight: FontWeight.Light)))));

        RichText text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Equal(FontWeight.Bold, text.Text.Style?.FontWeight);
        Assert.Equal(12.0, text.Text.Style?.FontSize);

        // RichText itself does not apply bold text.
        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(BoldText: true),
            child: new Center(child: new RichText(
                text: new TextSpan(text: "Hello", style: new TextStyle(FontWeight: FontWeight.Light)),
                textDirection: TextDirection.Ltr))));
        text = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Equal(FontWeight.Light, text.Text.Style?.FontWeight);
    }

    private sealed class SubclassSpan(string text) : TextSpan(text: text);
}
