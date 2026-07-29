using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/theme.dart
// flutter/packages/flutter/lib/src/material/theme_data.dart
// flutter/packages/flutter/lib/src/material/page.dart
// flutter/packages/flutter/lib/src/material/page_transitions_theme.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialThemeAnimationTests : IDisposable
{
    public MaterialThemeAnimationTests()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesColorsTypographyIconsAndDensity_AndSnapsDiscreteValues()
    {
        var begin = new ThemeData(
            brightness: Brightness.Light,
            primaryColor: Color.FromArgb(255, 0, 20, 40),
            textTheme: new MaterialTextTheme(
                bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(fontSize: 10)),
            iconTheme: new IconThemeData(Color: Colors.Black, Size: 16),
            visualDensity: new VisualDensity(-2, 0),
            useMaterial3: false);
        var end = new ThemeData(
            brightness: Brightness.Dark,
            primaryColor: Color.FromArgb(255, 100, 120, 140),
            textTheme: new MaterialTextTheme(
                bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(fontSize: 20)),
            iconTheme: new IconThemeData(Color: Colors.White, Size: 24),
            visualDensity: new VisualDensity(2, 4),
            useMaterial3: true);

        ThemeData firstHalf = ThemeData.Lerp(begin, end, 0.25);
        ThemeData midpoint = ThemeData.Lerp(begin, end, 0.5);

        Assert.Same(begin, ThemeData.Lerp(begin, end, 0.0));
        Assert.Same(end, ThemeData.Lerp(begin, end, 1.0));
        Assert.Equal(Color.FromArgb(255, 50, 70, 90), midpoint.PrimaryColor);
        Assert.Equal(15, midpoint.TextTheme.BodyMedium.FontSize);
        Assert.Equal(20, midpoint.IconTheme.Size);
        Assert.Equal(new VisualDensity(0, 2), midpoint.VisualDensity);
        Assert.Equal(Brightness.Light, firstHalf.Brightness);
        Assert.False(firstHalf.UseMaterial3);
        Assert.Equal(Brightness.Dark, midpoint.Brightness);
        Assert.True(midpoint.UseMaterial3);
    }

    [Fact]
    public void AnimatedTheme_InterpolatesFromCurrentThemeAcrossInterruptedUpdates_AndCallsOnEnd()
    {
        var owner = new BuildOwner();
        ThemeData? observedTheme = null;
        int completed = 0;
        var probe = new ThemeProbe(theme => observedTheme = theme);
        var begin = ThemeData.Light with { PrimaryColor = Color.FromRgb(0, 0, 0) };
        var firstTarget = ThemeData.Light with { PrimaryColor = Color.FromRgb(200, 100, 50) };
        var secondTarget = ThemeData.Light with { PrimaryColor = Color.FromRgb(20, 220, 120) };
        var root = new TestRootElement(new AnimatedTheme(
            data: begin,
            duration: TimeSpan.FromMilliseconds(200),
            child: probe,
            onEnd: () => completed++));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.Equal(begin.PrimaryColor, observedTheme!.PrimaryColor);

        root.Update(new AnimatedTheme(
            data: firstTarget,
            duration: TimeSpan.FromMilliseconds(200),
            child: probe,
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.1));
        owner.FlushBuild();
        Color interruptedColor = observedTheme!.PrimaryColor;
        Assert.NotEqual(begin.PrimaryColor, interruptedColor);
        Assert.NotEqual(firstTarget.PrimaryColor, interruptedColor);

        root.Update(new AnimatedTheme(
            data: secondTarget,
            duration: TimeSpan.FromMilliseconds(200),
            child: probe,
            onEnd: () => completed++));
        owner.FlushBuild();
        Assert.Equal(interruptedColor, observedTheme!.PrimaryColor);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        Assert.Equal(secondTarget.PrimaryColor, observedTheme.PrimaryColor);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedTheme_ExposesFlutterDefaultsAndValidatesDuration()
    {
        var animatedTheme = new AnimatedTheme(
            data: ThemeData.Light,
            child: new SizedBox());

        Assert.Equal(TimeSpan.FromMilliseconds(200), animatedTheme.Duration);
        Assert.Equal(Curves.Linear(0.3), animatedTheme.Curve(0.3));
        Assert.Null(animatedTheme.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedTheme(
            data: ThemeData.Light,
            child: new SizedBox(),
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void MaterialPageRoute_UsesPlatformBuilderDurationsAndTransitionComposition()
    {
        var builder = new RecordingPageTransitionsBuilder();
        var pageTransitions = new PageTransitionsTheme(
            new Dictionary<TargetPlatform, PageTransitionsBuilder>
            {
                [TargetPlatform.Windows] = builder,
            });
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.Windows,
            PageTransitionsTheme = pageTransitions,
        };
        NavigatorState? navigator = null;
        var route = new MaterialPageRoute(
            builder: context =>
            {
                navigator ??= Navigator.Of(context);
                return new SizedBox(width: 10, height: 10);
            });
        var root = new TestRootElement(
            new Theme(
                theme,
                new Directionality(
                    Plumix.UI.TextDirection.Ltr,
                    new Navigator(route))));
        var owner = new BuildOwner();

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigator);
        Assert.Equal(TimeSpan.FromMilliseconds(120), route.TransitionDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(80), route.ReverseTransitionDuration);
        Assert.True(builder.BuildCount > 0);

        root.Unmount();
    }

    private sealed class ThemeProbe : StatelessWidget
    {
        private readonly Action<ThemeData> _onBuild;

        public ThemeProbe(Action<ThemeData> onBuild)
        {
            _onBuild = onBuild;
        }

        public override Widget Build(BuildContext context)
        {
            _onBuild(Theme.Of(context));
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class RecordingPageTransitionsBuilder : PageTransitionsBuilder
    {
        public int BuildCount { get; private set; }

        public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(120);

        public override TimeSpan ReverseTransitionDuration => TimeSpan.FromMilliseconds(80);

        public override Widget BuildTransitions(
            PageRoute route,
            BuildContext context,
            Animation<double> animation,
            Animation<double> secondaryAnimation,
            Widget child)
        {
            BuildCount += 1;
            return child;
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

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
            if (_child is not null)
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
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
