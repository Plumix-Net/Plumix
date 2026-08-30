using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Color = Avalonia.Media.Color;
using Image = Plumix.Widgets.Image;
using Path = Plumix.UI.Path;

// Regression coverage for the Flutter 3.44.0 -> 3.47.0 upstream deltas absorbed into the existing
// ports.
//
// Dart parity sources:
// flutter/packages/flutter/lib/src/animation/animation_style.dart
// flutter/packages/flutter/lib/src/painting/borders.dart
// flutter/packages/flutter/lib/src/painting/image_stream.dart
// flutter/packages/flutter/lib/src/scheduler/binding.dart
// flutter/packages/flutter/lib/src/widgets/animated_cross_fade.dart
// flutter/packages/flutter/lib/src/widgets/image_icon.dart
// flutter/packages/flutter/lib/src/widgets/indexed_stack.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class UpstreamDelta347Tests : IDisposable
{
    private static readonly Color Green = Color.FromRgb(0, 0xFF, 0);

    public UpstreamDelta347Tests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void AnimationStyle_NoAnimationCopyWithAndMergeFollowTheSource()
    {
        // Dart's `AnimationStyle.noAnimation` only zeroes the durations; both curves stay null.
        Assert.Equal(TimeSpan.Zero, AnimationStyle.NoAnimation.Duration);
        Assert.Equal(TimeSpan.Zero, AnimationStyle.NoAnimation.ReverseDuration);
        Assert.Null(AnimationStyle.NoAnimation.Curve);
        Assert.Null(AnimationStyle.NoAnimation.ReverseCurve);
        Assert.Equal(
            AnimationStyle.NoAnimation,
            new AnimationStyle(Duration: TimeSpan.Zero, ReverseDuration: TimeSpan.Zero));

        var style = new AnimationStyle(
            Duration: TimeSpan.FromMilliseconds(100),
            Curve: Curves.EaseIn);
        AnimationStyle copied = style.CopyWith(reverseDuration: TimeSpan.FromMilliseconds(50));
        Assert.Equal(TimeSpan.FromMilliseconds(100), copied.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(50), copied.ReverseDuration);
        Assert.Equal<Curve?>(Curves.EaseIn, copied.Curve);

        // Merge takes the other style's non-null properties and keeps the rest.
        Assert.Same(style, style.Merge(null));
        AnimationStyle merged = style.Merge(new AnimationStyle(Duration: TimeSpan.FromSeconds(1)));
        Assert.Equal(TimeSpan.FromSeconds(1), merged.Duration);
        Assert.Equal<Curve?>(Curves.EaseIn, merged.Curve);
    }

    [Fact]
    public void AnimationStyle_LerpInterpolatesDurationsAndBlendsCurves()
    {
        var a = new AnimationStyle(Duration: TimeSpan.FromMilliseconds(100), Curve: Curves.Linear);
        var b = new AnimationStyle(Duration: TimeSpan.FromMilliseconds(300), Curve: Curves.EaseIn);

        Assert.Same(a, AnimationStyle.Lerp(a, a, 0.5));
        Assert.Equal(a.Duration, AnimationStyle.Lerp(a, b, 0.0)!.Duration);
        Assert.Equal(b.Duration, AnimationStyle.Lerp(a, b, 1.0)!.Duration);

        AnimationStyle? half = AnimationStyle.Lerp(a, b, 0.5);
        Assert.NotNull(half);
        // Durations interpolate in microseconds rather than snapping at t == 0.5.
        Assert.Equal(TimeSpan.FromMilliseconds(200), half!.Duration);
        // A null duration on one side counts as zero.
        Assert.Equal(
            TimeSpan.FromMilliseconds(150),
            AnimationStyle.Lerp(a, new AnimationStyle(Duration: TimeSpan.FromMilliseconds(200)), 0.5)!
                .Duration);

        // Curves blend as the weighted average of the two transforms, not a t < 0.5 snap.
        Curve blended = half.Curve!;
        Assert.Equal((Curves.Linear(0.25) * 0.5) + (Curves.EaseIn(0.25) * 0.5), blended(0.25), 10);

        // A null curve stands in as Curves.linear.
        Curve againstNull = AnimationStyle.Lerp(new AnimationStyle(), b, 0.5)!.Curve!;
        Assert.Equal((Curves.Linear(0.4) * 0.5) + (Curves.EaseIn(0.4) * 0.5), againstNull(0.4), 10);
    }

    [Fact]
    public void ShapeBorderLerp_FallsBackToTheReversedTimelineBeforeSnapping()
    {
        // `a` refuses both directions and `b` only lerps in the reversed direction, so only the
        // `b.LerpTo(a, 1 - t)` fallback can produce a result.
        var a = new UnlerpableBorder();
        var b = new ReverseOnlyBorder();

        var reversed = Assert.IsType<ReverseOnlyBorder>(ShapeBorder.Lerp(a, b, 0.25));
        Assert.Equal(0.75, reversed.T, 10);

        // OutlinedBorder.Lerp shares the fallback chain.
        var outlined = Assert.IsType<ReverseOnlyBorder>(OutlinedBorder.Lerp(a, b, 0.25));
        Assert.Equal(0.75, outlined.T, 10);

        // Without any fallback the t < 0.5 snap is still the last resort.
        Assert.Same(a, ShapeBorder.Lerp(a, new UnlerpableBorder(), 0.25));
    }

    [Fact]
    public void ImageStreamCompleter_SuppressesUnhandledErrorsAfterANonReportingListener()
    {
        Assert.True(new ImageStreamListener(OnImage: (_, _) => { }).ReportErrors);

        var reported = new List<Exception>();
        void OnUnhandled(Exception exception, System.Diagnostics.StackTrace? stack) =>
            reported.Add(exception);

        ImageStreamCompleter.UnhandledError += OnUnhandled;
        try
        {
            var failure = new InvalidOperationException("boom");

            // With no listener at all the error is reported.
            new TestImageStreamCompleter().Fail(failure);
            Assert.Single(reported);

            // An error listener handles the error, so nothing is reported.
            reported.Clear();
            var handledCompleter = new TestImageStreamCompleter();
            int handled = 0;
            handledCompleter.AddListener(new ImageStreamListener(
                OnImage: (_, _) => { },
                OnError: (_, _) => handled++));
            handledCompleter.Fail(failure);
            Assert.Equal(1, handled);
            Assert.Empty(reported);

            // A listener that opted out of error reporting suppresses the report even after it is
            // removed, because that listener was the one meant to handle the error.
            reported.Clear();
            var suppressed = new TestImageStreamCompleter();
            var listener = new ImageStreamListener(OnImage: (_, _) => { }, ReportErrors: false);
            suppressed.AddListener(listener);
            suppressed.RemoveListener(listener);
            suppressed.Fail(failure);
            Assert.Empty(reported);
        }
        finally
        {
            ImageStreamCompleter.UnhandledError -= OnUnhandled;
        }
    }

    [Fact]
    public void ImageIcon_UseOriginalColorsDropsTheTintAndRejectsAnExplicitColor()
    {
        var provider = new TestImageProvider("icon");
        Assert.False(new ImageIcon(provider).UseOriginalColors);
        Assert.Throws<ArgumentException>(
            () => new ImageIcon(provider, color: Green, useOriginalColors: true));

        var owner = new BuildOwner();
        BuildContext context = default;
        var root = new TestRootElement(new Builder(builderContext =>
        {
            context = builderContext;
            return new SizedBox();
        }));
        Mount(root, owner);

        Assert.Equal(Green, ImageOf(new ImageIcon(provider, color: Green).Build(context)).Color);
        Assert.Null(ImageOf(new ImageIcon(provider, useOriginalColors: true).Build(context)).Color);
        root.Unmount();

        static Image ImageOf(Widget built) =>
            Assert.IsType<Image>(Assert.IsType<Semantics>(built).Child);
    }

    [Fact]
    public void AnimatedCrossFade_ClipBehaviorDefaultsToHardEdge()
    {
        var crossFade = new AnimatedCrossFade(
            firstChild: new SizedBox(),
            secondChild: new SizedBox(),
            crossFadeState: CrossFadeState.ShowFirst,
            duration: TimeSpan.FromMilliseconds(100));
        Assert.Equal(Clip.HardEdge, crossFade.ClipBehavior);

        var clipped = new AnimatedCrossFade(
            firstChild: new SizedBox(),
            secondChild: new SizedBox(),
            crossFadeState: CrossFadeState.ShowFirst,
            duration: TimeSpan.FromMilliseconds(100),
            clipBehavior: Clip.None);
        Assert.Equal(Clip.None, clipped.ClipBehavior);
    }

    [Fact]
    public void IndexedStack_WrapsEveryChildAndExcludesUnselectedChildrenFromFocus()
    {
        var first = new SizedBox(width: 1);
        var second = new SizedBox(width: 2);

        var raw = Assert.IsType<RawIndexedStack>(
            new IndexedStack(children: [first, second], index: 1).Build(default));
        Assert.Equal(2, raw.Children.Count);

        var firstScope = Assert.IsType<VisibilityScope>(raw.Children[0]);
        Assert.False(firstScope.IsVisible);
        var firstExclude = Assert.IsType<ExcludeFocus>(firstScope.Child);
        Assert.True(firstExclude.Excluding);
        Assert.Same(first, firstExclude.Child);

        var secondScope = Assert.IsType<VisibilityScope>(raw.Children[1]);
        Assert.True(secondScope.IsVisible);
        var secondExclude = Assert.IsType<ExcludeFocus>(secondScope.Child);
        Assert.False(secondExclude.Excluding);
        Assert.Same(second, secondExclude.Child);
    }

    [Fact]
    public void Scheduler_ScheduleFrameCallbackRunsOnceAndCanBeCancelled()
    {
        var order = new List<string>();
        int cancelled = Scheduler.ScheduleFrameCallback(_ => order.Add("cancelled"));
        Scheduler.ScheduleFrameCallback(_ => order.Add("first"));
        Scheduler.CancelFrameCallbackWithId(cancelled);

        Assert.Equal(1, Scheduler.TransientCallbackCount);
        Assert.True(Scheduler.HasScheduledFrame);

        Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(16));
        Assert.Equal(["first"], order);
        Assert.Equal(0, Scheduler.TransientCallbackCount);

        // A transient callback is one-shot: a later frame does not run it again.
        Scheduler.ScheduleFrameCallback(_ => order.Add("second"));
        Scheduler.PumpFrameForTests(TimeSpan.FromMilliseconds(32));
        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public void TickerMode_GetValuesNotifierFallsBackOnAnUnmountedContext()
    {
        // Dart guards the inherited-widget lookup with `context.mounted`, so an animation
        // controller that is a late field first touched in State.dispose() still gets a value.
        Assert.False(default(BuildContext).Mounted);
        Assert.Same(
            TickerMode.GetValuesNotifier(default),
            TickerMode.GetValuesNotifier(default));
        Assert.Equal(
            TickerModeData.Fallback,
            TickerMode.GetValuesNotifier(default).Value);

        BuildContext context = default;
        var owner = new BuildOwner();
        var root = new TestRootElement(new Builder(builderContext =>
        {
            context = builderContext;
            return new SizedBox();
        }));
        Mount(root, owner);
        Assert.True(context.Mounted);
        root.Unmount();
        Assert.False(context.Mounted);
        Assert.Equal(TickerModeData.Fallback, TickerMode.GetValuesNotifier(context).Value);
    }

    [Fact]
    public void RichText_PassesTheAmbientDevicePixelRatioToTheParagraph()
    {
        var paragraph = new RenderParagraph(new TextSpan("hi"));
        Assert.Equal(1.0, paragraph.DevicePixelRatio);

        paragraph.DevicePixelRatio = 3.0;
        if (Constants.KDebugMode)
        {
            // `debugFillProperties` is an assert-only body in Dart, so it fills nothing outside a
            // debug build; the ambient-value plumbing below holds in every build.
            var properties = new DiagnosticPropertiesBuilder();
            paragraph.DebugFillProperties(properties);
            Assert.Equal(
                3.0,
                Assert.Single(properties.Properties, p => p.Name == "devicePixelRatio").Value);
        }

        BuildContext context = default;
        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            new MediaQueryData(DevicePixelRatio: 2.5),
            new Directionality(
                TextDirection.Ltr,
                new Builder(builderContext =>
                {
                    context = builderContext;
                    return new SizedBox();
                }))));
        Mount(root, owner);

        var richText = new RichText(new TextSpan("hi"));
        var created = (RenderParagraph)richText.CreateRenderObject(context);
        Assert.Equal(2.5, created.DevicePixelRatio);
        root.Unmount();
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TestImageStreamCompleter : ImageStreamCompleter
    {
        public void Fail(Exception exception) => ReportError(exception);
    }

    private sealed class TestImageProvider : ImageProvider<string>
    {
        private readonly string _key;

        public TestImageProvider(string key) => _key = key;

        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) =>
            ValueTask.FromResult(_key);

        protected override ImageStreamCompleter LoadImage(string key) =>
            new OneFrameImageStreamCompleter(
                Task.FromResult(new ImageInfo(new FakeImage(new Size(10, 10)), debugLabel: key)));
    }

    private sealed class FakeImage : Avalonia.Media.IImage
    {
        public FakeImage(Size size) => Size = size;

        public Size Size { get; }

        public void Draw(Avalonia.Media.DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }

    private sealed record UnlerpableBorder : OutlinedBorder
    {
        public override ShapeBorder Scale(double t) => this;

        public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
        {
            var path = new Path();
            path.AddRect(rect);
            return path;
        }

        public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null) =>
            GetInnerPath(rect, textDirection);

        public override void Paint(
            PaintingContext context,
            Rect rect,
            TextDirection? textDirection = null)
        {
        }

        public override OutlinedBorder CopyWith(BorderSide? side = null) => this;
    }

    private sealed record ReverseOnlyBorder : OutlinedBorder
    {
        public ReverseOnlyBorder(double t = 0.0)
        {
            T = t;
        }

        public double T { get; }

        public override ShapeBorder Scale(double t) => this;

        // Only the reversed timeline (`b.LerpTo(a, 1 - t)`) produces a value; the forward
        // LerpFrom/LerpTo pair declines.
        public override ShapeBorder? LerpFrom(ShapeBorder? a, double t) => null;

        public override ShapeBorder? LerpTo(ShapeBorder? b, double t) =>
            b is UnlerpableBorder ? new ReverseOnlyBorder(t) : null;

        public override Path GetInnerPath(Rect rect, TextDirection? textDirection = null)
        {
            var path = new Path();
            path.AddRect(rect);
            return path;
        }

        public override Path GetOuterPath(Rect rect, TextDirection? textDirection = null) =>
            GetInnerPath(rect, textDirection);

        public override void Paint(
            PaintingContext context,
            Rect rect,
            TextDirection? textDirection = null)
        {
        }

        public override OutlinedBorder CopyWith(BorderSide? side = null) => this;
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

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void VisitChildren(System.Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
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
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
