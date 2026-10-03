using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports the supportsAnnounce cases of flutter/packages/flutter/test/widgets/media_query_test.dart
// ('MediaQuery.supportsAnnounce' and the supportsAnnounceOf/maybeSupportsAnnounceOf variants of
// 'MediaQuery partial dependencies') and text_test.dart's 'Text respects media query', the framework's
// MediaQuery.withClampedTextScaling test.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MediaQueryAnnounceAndTextScalingDartParityTests
{
    // Flutter: 'media_query_test.dart: MediaQuery.supportsAnnounce'
    [Fact]
    public void MediaQuerySupportsAnnounce()
    {
        using var tester = new FrameworkDartTester();
        bool? outsideSupportsAnnounce = null;
        bool? insideSupportsAnnounce = null;

        tester.PlatformDispatcher.AccessibilityFeaturesTestValue = new FakeAccessibilityFeatures();

        tester.PumpWidget(new Builder(context =>
        {
            outsideSupportsAnnounce = MediaQuery.SupportsAnnounceOf(context);
            return new MediaQuery(
                data: new MediaQueryData(SupportsAnnounce: true),
                child: new Builder(innerContext =>
                {
                    insideSupportsAnnounce = MediaQuery.SupportsAnnounceOf(innerContext);
                    return new Container();
                }));
        }));

        Assert.False(outsideSupportsAnnounce);
        Assert.True(insideSupportsAnnounce);
    }

    // Flutter: 'media_query_test.dart: MediaQuery partial dependencies' for the
    // MediaQuery.supportsAnnounceOf and MediaQuery.maybeSupportsAnnounceOf variants.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MediaQueryPartialDependenciesSupportsAnnounce(bool maybe)
    {
        using var tester = new FrameworkDartTester();
        var data = new MediaQueryData();
        int buildCount = 0;
        StateSetter? setPageState = null;

        Widget builder = new Builder(context =>
        {
            if (maybe)
            {
                MediaQuery.MaybeSupportsAnnounceOf(context);
            }
            else
            {
                MediaQuery.SupportsAnnounceOf(context);
            }

            buildCount++;
            return new SizedBox(width: 0, height: 0);
        });

        Widget page = new StatefulBuilder((_, setState) =>
        {
            setPageState = setState;
            return new MediaQuery(data: data, child: builder);
        });

        tester.PumpWidget(page);
        Assert.Equal(1, buildCount);

        // 'Copy data'.
        setPageState!(() => data = data.CopyWith());
        tester.PumpAndSettle();
        Assert.Equal(1, buildCount);

        // 'Change data'.
        setPageState!(() => data = new MediaQueryData(SupportsAnnounce: true));
        tester.PumpAndSettle();
        Assert.Equal(2, buildCount);

        // 'Copy data'.
        setPageState!(() => data = data.CopyWith());
        tester.PumpAndSettle();
        Assert.Equal(2, buildCount);
    }

    // C#-only companion of the partial-dependency test: a change to another field does not rebuild a
    // MaybeSupportsAnnounceOf dependent, and the lookup reads the nearest ancestor's value.
    [Fact]
    public void MaybeSupportsAnnounceOfDependsOnlyOnSupportsAnnounce()
    {
        using var tester = new FrameworkDartTester();
        var data = new MediaQueryData(SupportsAnnounce: true);
        int buildCount = 0;
        bool? observed = null;
        StateSetter? setPageState = null;

        Widget builder = new Builder(context =>
        {
            observed = MediaQuery.MaybeSupportsAnnounceOf(context);
            buildCount++;
            return new SizedBox(width: 0, height: 0);
        });

        tester.PumpWidget(new StatefulBuilder((_, setState) =>
        {
            setPageState = setState;
            return new MediaQuery(data: data, child: builder);
        }));
        Assert.Equal(1, buildCount);
        Assert.True(observed);

        setPageState!(() => data = data.CopyWith(size: new Avalonia.Size(1, 1)));
        tester.PumpAndSettle();
        Assert.Equal(1, buildCount);
    }

    // Flutter: 'text_test.dart: Text respects media query'
    [Fact]
    public void TextRespectsMediaQuery()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(MediaQuery.WithClampedTextScaling(
            minScaleFactor: 1.3,
            maxScaleFactor: 1.3,
            child: new Center(child: new Text("Hello", textDirection: TextDirection.Ltr))));

        RichText text = FirstRichText(tester);
        Assert.NotNull(text);
        Assert.Equal(TextScaler.Linear(1.3), text.TextScaler);

        tester.PumpWidget(new Center(child: new Text("Hello", textDirection: TextDirection.Ltr)));

        text = FirstRichText(tester);
        Assert.NotNull(text);
        // isSystemTextScaler(withScaleFactor: 1.0).
        Assert.Equal(1.0, text.TextScaler.TextScaleFactor);
    }

    // C#-only: the Builder that withClampedTextScaling returns carries the key and clamps the ambient
    // scaler read from its own context, not from the caller's.
    [Fact]
    public void WithClampedTextScalingClampsTheAmbientScalerBelowTheBuilder()
    {
        using var tester = new FrameworkDartTester();
        var key = new UniqueKey();
        TextScaler? inner = null;

        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(TextScaler: TextScaler.Linear(3.0)),
            child: MediaQuery.WithClampedTextScaling(
                key: key,
                maxScaleFactor: 2.0,
                child: new Builder(context =>
                {
                    inner = MediaQuery.TextScalerOf(context);
                    return new SizedBox(width: 0, height: 0);
                }))));

        Assert.IsType<Builder>(tester.ElementsWithKey(key).Single().Widget);
        Assert.NotNull(inner);
        Assert.Equal(TextScaler.Linear(3.0).Clamp(maxScaleFactor: 2.0), inner);
        Assert.Equal(20.0, inner!.Scale(10.0));
    }

    // Flutter: media_query.dart's withClampedTextScaling asserts.
    [DebugOnlyFact]
    public void WithClampedTextScalingAssertsItsArguments()
    {
        Widget child = new SizedBox(width: 0, height: 0);

        AssertionError error = Assert.Throws<AssertionError>(
            () => MediaQuery.WithClampedTextScaling(minScaleFactor: 2.0, maxScaleFactor: 1.0, child: child));
        Assert.Contains("maxScaleFactor >= minScaleFactor", error.Message);

        error = Assert.Throws<AssertionError>(
            () => MediaQuery.WithClampedTextScaling(maxScaleFactor: double.NaN, child: child));
        Assert.Contains("maxScaleFactor >= minScaleFactor", error.Message);

        error = Assert.Throws<AssertionError>(() => MediaQuery.WithClampedTextScaling(
            minScaleFactor: double.PositiveInfinity,
            maxScaleFactor: double.PositiveInfinity,
            child: child));
        Assert.Contains("minScaleFactor.isFinite", error.Message);

        error = Assert.Throws<AssertionError>(
            () => MediaQuery.WithClampedTextScaling(minScaleFactor: -1.0, child: child));
        Assert.Contains("minScaleFactor >= 0", error.Message);
    }

    private static RichText FirstRichText(FrameworkDartTester tester) =>
        (RichText)tester.ElementsOfType<RichText>().First().Widget;
}
