using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using RelativeRect = Plumix.Rendering.RelativeRect;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TransitionsTests : IDisposable
{
    public TransitionsTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void SlideTransition_ExposesFlutterDefaultsAndResolvesTextDirection()
    {
        var animation = new TestValueAnimation<Vector>(
            new Vector(0.25, -0.5),
            AnimationStatus.Forward);
        var child = new SizedBox(width: 40, height: 20);
        var transition = new SlideTransition(animation, child: child);

        Assert.Same(animation, transition.Position);
        Assert.Same(animation, transition.Listenable);
        Assert.True(transition.TransformHitTests);
        Assert.Null(transition.TextDirection);
        Assert.Same(child, transition.Child);
        Assert.Throws<ArgumentNullException>(() => new SlideTransition(null!));

        var owner = new BuildOwner();
        var root = new TestRootElement(new SlideTransition(
            position: animation,
            transformHitTests: false,
            textDirection: Plumix.UI.TextDirection.Rtl,
            child: child));
        Mount(root, owner);

        var translation = Assert.IsType<RenderFractionalTranslation>(root.ChildElement!.RenderObject);
        Assert.Equal(new Vector(-0.25, -0.5), translation.Translation);
        Assert.False(translation.TransformHitTests);
        Assert.Equal(1, animation.ListenerCount);

        animation.Set(new Vector(-0.4, 0.75), AnimationStatus.Reverse);
        owner.FlushBuild();

        translation = Assert.IsType<RenderFractionalTranslation>(root.ChildElement.RenderObject);
        Assert.Equal(new Vector(0.4, 0.75), translation.Translation);

        root.Unmount();
        Assert.Equal(0, animation.ListenerCount);
    }

    [Fact]
    public void SlideTransition_RebindsAnimationAndPreservesFractionalHitTestPolicy()
    {
        var first = new TestValueAnimation<Vector>(new Vector(0.2, 0.3), AnimationStatus.Forward);
        var second = new TestValueAnimation<Vector>(new Vector(-0.5, 0.1), AnimationStatus.Reverse);
        var owner = new BuildOwner();
        var root = new TestRootElement(new SlideTransition(
            position: first,
            transformHitTests: false,
            child: new SizedBox(width: 80, height: 40)));
        Mount(root, owner);

        var translation = Assert.IsType<RenderFractionalTranslation>(root.ChildElement!.RenderObject);
        translation.Layout(BoxConstraints.Tight(new Size(80, 40)));
        Assert.Equal(new Vector(0.2, 0.3), translation.Translation);
        Assert.False(translation.TransformHitTests);
        Assert.Equal(1, first.ListenerCount);

        root.Update(new SlideTransition(
            position: second,
            transformHitTests: false,
            child: new SizedBox(width: 80, height: 40)));
        owner.FlushBuild();

        translation = Assert.IsType<RenderFractionalTranslation>(root.ChildElement.RenderObject);
        Assert.Equal(new Vector(-0.5, 0.1), translation.Translation);
        Assert.False(translation.TransformHitTests);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);

        root.Unmount();
        Assert.Equal(0, second.ListenerCount);
    }

    [Fact]
    public void ThresholdCurveAndVectorTween_MatchFlutterEndpointAndInterpolationContracts()
    {
        Curve threshold = Curves.Threshold(0.25);

        Assert.Equal(0.0, threshold(0.0));
        Assert.Equal(0.0, threshold(0.249));
        Assert.Equal(1.0, threshold(0.25));
        Assert.Equal(1.0, threshold(1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Curves.Threshold(-0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => Curves.Threshold(1.01));

        var tween = new VectorTween(
            begin: new Vector(2.0, -4.0),
            end: new Vector(10.0, 8.0));
        Assert.Equal(new Vector(4.0, -1.0), tween.Evaluate(0.25));
        Assert.False(AnimationStatus.Forward.IsCompleted());
        Assert.True(AnimationStatus.Completed.IsCompleted());
    }

    [Fact]
    public void SizeTransition_ExposesFlutterContractsAndValidatesArguments()
    {
        var animation = new TestAnimation(0.5, AnimationStatus.Forward);
        var child = new SizedBox(width: 80, height: 40);
        var transition = new SizeTransition(
            sizeFactor: animation,
            axis: Axis.Horizontal,
            alignment: Alignment.BottomRight,
            fixedCrossAxisSizeFactor: 0.75,
            child: child);

        Assert.Same(animation, transition.SizeFactor);
        Assert.Same(animation, transition.Listenable);
        Assert.Equal(Axis.Horizontal, transition.Axis);
        Assert.Equal(
            Alignment.BottomRight,
            transition.Alignment!.Value.Resolve(Plumix.UI.TextDirection.Ltr));
        Assert.Equal(0.75, transition.FixedCrossAxisSizeFactor);
        Assert.Same(child, transition.Child);

        Assert.Throws<ArgumentNullException>(() => new SizeTransition(null!));
        Assert.Throws<ArgumentException>(() => new SizeTransition(
            animation,
            axisAlignment: 0.5,
            alignment: Alignment.Center));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SizeTransition(
            animation,
            fixedCrossAxisSizeFactor: -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SizeTransition(
            animation,
            fixedCrossAxisSizeFactor: double.NaN));

        var directional = new SizeTransition(
            animation,
            alignment: AlignmentDirectional.BottomStart);
        Assert.Equal(
            Alignment.BottomRight,
            directional.Alignment!.Value.Resolve(Plumix.UI.TextDirection.Rtl));
    }

    [Fact]
    public void SizeTransition_ClampsFactorClipsAndResolvesDirectionalAlignment()
    {
        var animation = new TestAnimation(-0.25, AnimationStatus.Forward);
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            textDirection: Plumix.UI.TextDirection.Ltr,
            child: new SizeTransition(
                sizeFactor: animation,
                axis: Axis.Vertical,
                axisAlignment: 1.0,
                fixedCrossAxisSizeFactor: 0.5,
                child: new SizedBox(width: 80, height: 40))));
        Mount(root, owner);

        var clip = Assert.IsType<RenderClipRect>(root.ChildElement!.RenderObject);
        var align = Assert.IsType<RenderAlign>(clip.Child);
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));

        Assert.Equal(Alignment.BottomLeft, align.Alignment);
        Assert.Equal(0.5, align.WidthFactor);
        Assert.Equal(0.0, align.HeightFactor);
        Assert.Equal(new Size(40, 0), clip.Size);
        Assert.Equal(new Point(0, -40), ((BoxParentData)align.Child!.parentData!).offset);

        animation.Set(0.5, AnimationStatus.Forward);
        owner.FlushBuild();
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));

        Assert.Equal(0.5, align.HeightFactor);
        Assert.Equal(new Size(40, 20), clip.Size);
        Assert.Equal(new Point(0, -20), ((BoxParentData)align.Child!.parentData!).offset);

        root.Unmount();
        Assert.Equal(0, animation.ListenerCount);
    }

    [Fact]
    public void SizeTransition_HorizontalDefaultUsesRtlAndRebindsAnimation()
    {
        var first = new TestAnimation(0.25, AnimationStatus.Forward);
        var second = new TestAnimation(0.75, AnimationStatus.Reverse);
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            textDirection: Plumix.UI.TextDirection.Rtl,
            child: new SizeTransition(
                sizeFactor: first,
                axis: Axis.Horizontal,
                axisAlignment: 0.5,
                child: new SizedBox(width: 80, height: 40))));
        Mount(root, owner);

        var clip = Assert.IsType<RenderClipRect>(root.ChildElement!.RenderObject);
        var align = Assert.IsType<RenderAlign>(clip.Child);
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));

        Assert.Equal(new Alignment(-0.5, -1.0), align.Alignment);
        Assert.Equal(0.25, align.WidthFactor);
        Assert.Null(align.HeightFactor);
        Assert.Equal(new Size(20, 200), clip.Size);
        Assert.Equal(new Point(-15, 0), ((BoxParentData)align.Child!.parentData!).offset);
        Assert.Equal(1, first.ListenerCount);

        root.Update(new Directionality(
            textDirection: Plumix.UI.TextDirection.Rtl,
            child: new SizeTransition(
                sizeFactor: second,
                axis: Axis.Horizontal,
                axisAlignment: 0.5,
                child: new SizedBox(width: 80, height: 40))));
        owner.FlushBuild();
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));

        Assert.Equal(0.75, align.WidthFactor);
        Assert.Equal(new Size(60, 200), clip.Size);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);

        root.Unmount();
        Assert.Equal(0, second.ListenerCount);
    }

    [Fact]
    public void ScaleAndRotationTransition_ExposeFlutterDefaultsAndSharedMatrixSurface()
    {
        var scaleAnimation = new TestAnimation(1.5, AnimationStatus.Completed);
        var scaleChild = new SizedBox(width: 20, height: 10);
        var scale = new ScaleTransition(scaleAnimation, child: scaleChild);

        Assert.Same(scaleAnimation, scale.Scale);
        Assert.Same(scaleAnimation, scale.Animation);
        Assert.Same(scaleAnimation, scale.Listenable);
        Assert.Equal(Alignment.Center, scale.Alignment);
        Assert.Null(scale.FilterQuality);
        Assert.Same(scaleChild, scale.Child);

        var turnsAnimation = new TestAnimation(0.25, AnimationStatus.Completed);
        var rotation = new RotationTransition(turnsAnimation);

        Assert.Same(turnsAnimation, rotation.Turns);
        Assert.Same(turnsAnimation, rotation.Animation);
        Assert.Equal(Alignment.Center, rotation.Alignment);
        Assert.Null(rotation.FilterQuality);
        Assert.Null(rotation.Child);

        Assert.Throws<ArgumentNullException>(() => new ScaleTransition(null!));
        Assert.Throws<ArgumentNullException>(() => new RotationTransition(null!));
        Assert.Throws<ArgumentNullException>(() => new MatrixTransition(
            scaleAnimation,
            onTransform: null!));
    }

    [Fact]
    public void MatrixTransition_UsesCallbackAlignmentAndAnimatedFilterQualityPolicy()
    {
        var animation = new TestAnimation(0.4, AnimationStatus.Dismissed);
        int callbackCount = 0;
        var transition = new MatrixTransition(
            animation: animation,
            onTransform: value =>
            {
                callbackCount++;
                return Matrix.CreateTranslation(value * 10.0, value * 20.0);
            },
            alignment: Alignment.BottomRight,
            filterQuality: FilterQuality.High,
            child: new SizedBox(width: 24, height: 16));
        var owner = new BuildOwner();
        var root = new TestRootElement(transition);
        Mount(root, owner);

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(Matrix.CreateTranslation(4.0, 8.0), renderTransform.Transform);
        Assert.Equal(Alignment.BottomRight, renderTransform.Alignment);
        Assert.Null(renderTransform.FilterQuality);
        Assert.Equal(1, callbackCount);

        animation.Set(0.6, AnimationStatus.Forward);
        owner.FlushBuild();

        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateTranslation(6.0, 12.0), renderTransform.Transform);
        Assert.Equal(FilterQuality.High, renderTransform.FilterQuality);
        Assert.Equal(2, callbackCount);

        animation.Set(1.0, AnimationStatus.Completed);
        owner.FlushBuild();

        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateTranslation(10.0, 20.0), renderTransform.Transform);
        Assert.Null(renderTransform.FilterQuality);
        Assert.Equal(3, callbackCount);

        root.Unmount();
    }

    [Fact]
    public void ScaleTransition_RebuildsFromAnimationAndRebindsWhenAnimationChanges()
    {
        var firstAnimation = new TestAnimation(0.5, AnimationStatus.Forward);
        var secondAnimation = new TestAnimation(1.25, AnimationStatus.Reverse);
        var owner = new BuildOwner();
        var root = new TestRootElement(new ScaleTransition(
            scale: firstAnimation,
            alignment: Alignment.TopLeft,
            filterQuality: FilterQuality.Low,
            child: new SizedBox(width: 30, height: 20)));
        Mount(root, owner);

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(Matrix.CreateScale(0.5, 0.5), renderTransform.Transform);
        Assert.Equal(Alignment.TopLeft, renderTransform.Alignment);
        Assert.Equal(FilterQuality.Low, renderTransform.FilterQuality);
        Assert.Equal(1, firstAnimation.ListenerCount);

        firstAnimation.Set(0.75, AnimationStatus.Forward);
        owner.FlushBuild();
        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateScale(0.75, 0.75), renderTransform.Transform);

        root.Update(new ScaleTransition(
            scale: secondAnimation,
            alignment: Alignment.TopLeft,
            filterQuality: FilterQuality.Low,
            child: new SizedBox(width: 30, height: 20)));
        owner.FlushBuild();

        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateScale(1.25, 1.25), renderTransform.Transform);
        Assert.Equal(0, firstAnimation.ListenerCount);
        Assert.Equal(1, secondAnimation.ListenerCount);

        firstAnimation.Set(2.0, AnimationStatus.Completed);
        owner.FlushBuild();
        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateScale(1.25, 1.25), renderTransform.Transform);

        root.Unmount();
        Assert.Equal(0, secondAnimation.ListenerCount);
    }

    [Fact]
    public void MatrixTransition_DropsFilterQualityOnAnimationControllerTerminalFrame()
    {
        using var animation = new AnimationController(TimeSpan.FromMilliseconds(100));
        var owner = new BuildOwner();
        var root = new TestRootElement(new ScaleTransition(
            scale: animation,
            filterQuality: FilterQuality.High,
            child: new SizedBox(width: 20, height: 20)));
        Mount(root, owner);

        animation.Forward(from: 0.0);
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(AnimationStatus.Completed, animation.Status);
        Assert.Equal(Matrix.Identity, renderTransform.Transform);
        Assert.Null(renderTransform.FilterQuality);

        root.Unmount();
    }

    [Theory]
    [InlineData(0.0, 1.0, 0.0, 0.0, 1.0)]
    [InlineData(0.25, 0.0, 1.0, -1.0, 0.0)]
    [InlineData(0.5, -1.0, 0.0, 0.0, -1.0)]
    [InlineData(0.75, 0.0, -1.0, 1.0, 0.0)]
    public void RotationTransition_ConvertsTurnsToExactCardinalMatrices(
        double turns,
        double m11,
        double m12,
        double m21,
        double m22)
    {
        var animation = new TestAnimation(turns, AnimationStatus.Completed);
        var transition = new RotationTransition(animation);
        var owner = new BuildOwner();
        var root = new TestRootElement(transition);
        Mount(root, owner);

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(new Matrix(m11, m12, m21, m22, 0, 0), renderTransform.Transform);

        root.Unmount();
    }

    [Fact]
    public void PositionedTransitions_ExposeFlutterContractsAndRelativeRectInterpolation()
    {
        var relativeRectAnimation = new TestValueAnimation<RelativeRect>(
            new RelativeRect(10, 12, 30, 32),
            AnimationStatus.Forward);
        var child = new SizedBox(width: 24, height: 16);
        var positioned = new PositionedTransition(relativeRectAnimation, child);

        Assert.Same(relativeRectAnimation, positioned.Rect);
        Assert.Same(relativeRectAnimation, positioned.Listenable);
        Assert.Same(child, positioned.Child);

        var rectAnimation = new TestValueAnimation<Rect?>(
            new Rect(20, 24, 40, 32),
            AnimationStatus.Forward);
        var relative = new RelativePositionedTransition(
            rect: rectAnimation,
            size: new Size(200, 120),
            child: child);

        Assert.Same(rectAnimation, relative.Rect);
        Assert.Equal(new Size(200, 120), relative.Size);
        Assert.Same(child, relative.Child);

        var tween = new RelativeRectTween(
            begin: new RelativeRect(0, 10, 20, 30),
            end: new RelativeRect(40, 30, 10, 0));
        Assert.Equal(new RelativeRect(10, 15, 17.5, 22.5), tween.Evaluate(0.25));
        Assert.Equal(new RelativeRect(60, 40, 5, -15), tween.Evaluate(1.5));
        Assert.Equal(RelativeRect.Fill, new RelativeRectTween().Evaluate(0.5));

        Assert.Throws<ArgumentNullException>(() => new PositionedTransition(null!, child));
        Assert.Throws<ArgumentNullException>(() => new PositionedTransition(relativeRectAnimation, null!));
        Assert.Throws<ArgumentNullException>(() => new RelativePositionedTransition(
            rect: null!,
            size: new Size(200, 120),
            child: child));
        Assert.Throws<ArgumentNullException>(() => new RelativePositionedTransition(
            rect: rectAnimation,
            size: new Size(200, 120),
            child: null!));
    }

    [Fact]
    public void PositionedTransition_RebuildsStackParentDataAndRebindsAnimation()
    {
        var firstAnimation = new TestValueAnimation<RelativeRect>(
            new RelativeRect(10, 12, 30, 32),
            AnimationStatus.Forward);
        var secondAnimation = new TestValueAnimation<RelativeRect>(
            new RelativeRect(7, 9, 11, 13),
            AnimationStatus.Reverse);
        var owner = new BuildOwner();
        var root = new TestRootElement(new Stack(
            children:
            [
                new PositionedTransition(
                    rect: firstAnimation,
                    child: new SizedBox(width: 24, height: 16)),
            ]));
        Mount(root, owner);

        var renderStack = Assert.IsType<RenderStack>(root.ChildElement!.RenderObject);
        renderStack.Layout(BoxConstraints.Tight(new Size(200, 120)));
        var parentData = Assert.IsType<StackParentData>(renderStack.FirstChild!.parentData);
        Assert.Equal(10, parentData.Left);
        Assert.Equal(12, parentData.Top);
        Assert.Equal(30, parentData.Right);
        Assert.Equal(32, parentData.Bottom);
        Assert.Equal(new Point(10, 12), parentData.offset);
        Assert.Equal(new Size(160, 76), renderStack.FirstChild.Size);
        Assert.Equal(1, firstAnimation.ListenerCount);

        firstAnimation.Set(new RelativeRect(20, 22, 40, 42), AnimationStatus.Forward);
        owner.FlushBuild();

        renderStack.Layout(BoxConstraints.Tight(new Size(200, 120)));
        parentData = Assert.IsType<StackParentData>(renderStack.FirstChild!.parentData);
        Assert.Equal(20, parentData.Left);
        Assert.Equal(22, parentData.Top);
        Assert.Equal(40, parentData.Right);
        Assert.Equal(42, parentData.Bottom);
        Assert.Equal(new Point(20, 22), parentData.offset);
        Assert.Equal(new Size(140, 56), renderStack.FirstChild.Size);

        root.Update(new Stack(
            children:
            [
                new PositionedTransition(
                    rect: secondAnimation,
                    child: new SizedBox(width: 24, height: 16)),
            ]));
        owner.FlushBuild();

        parentData = Assert.IsType<StackParentData>(renderStack.FirstChild!.parentData);
        Assert.Equal(7, parentData.Left);
        Assert.Equal(9, parentData.Top);
        Assert.Equal(11, parentData.Right);
        Assert.Equal(13, parentData.Bottom);
        Assert.Equal(0, firstAnimation.ListenerCount);
        Assert.Equal(1, secondAnimation.ListenerCount);

        root.Unmount();
        Assert.Equal(0, secondAnimation.ListenerCount);
    }

    [Fact]
    public void RelativePositionedTransition_ConvertsAnimatedRectAgainstDeclaredSize()
    {
        var animation = new TestValueAnimation<Rect?>(
            new Rect(20, 15, 50, 30),
            AnimationStatus.Forward);
        var owner = new BuildOwner();
        var root = new TestRootElement(new Stack(
            children:
            [
                new RelativePositionedTransition(
                    rect: animation,
                    size: new Size(200, 120),
                    child: new SizedBox()),
            ]));
        Mount(root, owner);

        var renderStack = Assert.IsType<RenderStack>(root.ChildElement!.RenderObject);
        renderStack.Layout(BoxConstraints.Tight(new Size(200, 120)));
        var parentData = Assert.IsType<StackParentData>(renderStack.FirstChild!.parentData);
        Assert.Equal(20, parentData.Left);
        Assert.Equal(15, parentData.Top);
        Assert.Equal(130, parentData.Right);
        Assert.Equal(75, parentData.Bottom);
        Assert.Equal(new Point(20, 15), parentData.offset);
        Assert.Equal(new Size(50, 30), renderStack.FirstChild.Size);

        animation.Set(null, AnimationStatus.Completed);
        owner.FlushBuild();

        renderStack.Layout(BoxConstraints.Tight(new Size(200, 120)));
        parentData = Assert.IsType<StackParentData>(renderStack.FirstChild!.parentData);
        Assert.Equal(0, parentData.Left);
        Assert.Equal(0, parentData.Top);
        Assert.Equal(200, parentData.Right);
        Assert.Equal(120, parentData.Bottom);
        Assert.Equal(new Point(), parentData.offset);
        Assert.Equal(new Size(), renderStack.FirstChild.Size);

        root.Unmount();
    }

    [Fact]
    public void AlignTransition_ExposesContractsResolvesDirectionAndRebindsAnimation()
    {
        var child = new SizedBox(width: 40, height: 20);
        var first = new TestValueAnimation<AlignmentGeometry>(
            AlignmentDirectional.TopStart,
            AnimationStatus.Forward);
        var second = new TestValueAnimation<AlignmentGeometry>(
            AlignmentDirectional.BottomEnd,
            AnimationStatus.Reverse);
        var transition = new AlignTransition(
            alignment: first,
            child: child,
            widthFactor: 2.0,
            heightFactor: 3.0);

        Assert.Same(first, transition.Alignment);
        Assert.Same(first, transition.Listenable);
        Assert.Same(child, transition.Child);
        Assert.Equal(2.0, transition.WidthFactor);
        Assert.Equal(3.0, transition.HeightFactor);
        Assert.Throws<ArgumentNullException>(() => new AlignTransition(null!, child));
        Assert.Throws<ArgumentNullException>(() => new AlignTransition(first, null!));

        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            textDirection: TextDirection.Rtl,
            child: transition));
        Mount(root, owner);

        var renderAlign = Assert.IsType<RenderAlign>(root.ChildElement!.RenderObject);
        renderAlign.Layout(BoxConstraints.Loose(new Size(200, 200)));
        Assert.Equal(Alignment.TopRight, renderAlign.Alignment);
        Assert.Equal(new Size(80, 60), renderAlign.Size);
        Assert.Equal(1, first.ListenerCount);

        root.Update(new Directionality(
            textDirection: TextDirection.Rtl,
            child: new AlignTransition(
                alignment: second,
                child: child,
                widthFactor: 2.0,
                heightFactor: 3.0)));
        owner.FlushBuild();

        renderAlign = Assert.IsType<RenderAlign>(root.ChildElement.RenderObject);
        Assert.Equal(Alignment.BottomLeft, renderAlign.Alignment);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);

        root.Unmount();
        Assert.Equal(0, second.ListenerCount);
    }

    [Fact]
    public void DefaultTextStyleTransition_AppliesAnimatedStyleAndImmediateTextOptions()
    {
        var firstStyle = new TextStyle(
            FontSize: 14,
            Color: Colors.DarkBlue,
            FontWeight: FontWeight.SemiBold,
            LetterSpacing: 0.5);
        var secondStyle = new TextStyle(
            FontSize: 22,
            Color: Colors.DarkRed,
            FontWeight: FontWeight.Bold,
            LetterSpacing: 1.5);
        var first = new TestValueAnimation<TextStyle>(firstStyle, AnimationStatus.Forward);
        var second = new TestValueAnimation<TextStyle>(secondStyle, AnimationStatus.Reverse);
        var child = new Text("animated style");
        var transition = new DefaultTextStyleTransition(first, child);

        Assert.Same(first, transition.Style);
        Assert.Same(first, transition.Listenable);
        Assert.Same(child, transition.Child);
        Assert.Null(transition.TextAlign);
        Assert.True(transition.SoftWrap);
        Assert.Equal(TextOverflow.Clip, transition.Overflow);
        Assert.Null(transition.MaxLines);
        Assert.Throws<ArgumentNullException>(() => new DefaultTextStyleTransition(null!, child));
        Assert.Throws<ArgumentNullException>(() => new DefaultTextStyleTransition(first, null!));

        var owner = new BuildOwner();
        var root = new TestRootElement(new DefaultTextStyleTransition(
            style: first,
            child: child,
            textAlign: TextAlign.Center,
            softWrap: false,
            overflow: TextOverflow.Ellipsis,
            maxLines: 2));
        Mount(root, owner);

        var paragraph = Assert.IsType<RenderParagraph>(root.ChildElement!.RenderObject);
        Assert.Equal(14, paragraph.FontSize);
        Assert.Equal(FontWeight.SemiBold, paragraph.FontWeight);
        Assert.Equal(0.5, paragraph.LetterSpacing);
        Assert.Equal(Colors.DarkBlue, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
        Assert.Equal(TextAlign.Center, paragraph.TextAlign);
        Assert.False(paragraph.SoftWrap);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
        Assert.Equal(2, paragraph.MaxLines);
        Assert.Equal(1, first.ListenerCount);

        root.Update(new DefaultTextStyleTransition(
            style: second,
            child: child,
            textAlign: TextAlign.End,
            overflow: TextOverflow.Fade,
            maxLines: 1));
        owner.FlushBuild();

        paragraph = Assert.IsType<RenderParagraph>(root.ChildElement.RenderObject);
        Assert.Equal(22, paragraph.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
        Assert.Equal(1.5, paragraph.LetterSpacing);
        Assert.Equal(Colors.DarkRed, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
        Assert.Equal(TextAlign.End, paragraph.TextAlign);
        Assert.True(paragraph.SoftWrap);
        Assert.Equal(TextOverflow.Fade, paragraph.Overflow);
        Assert.Equal(1, paragraph.MaxLines);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);

        root.Unmount();
        Assert.Equal(0, second.ListenerCount);
    }

    [Fact]
    public void DecorationTween_UsesDecorationPolymorphicLerpAndSupportsNullableEndpoints()
    {
        var begin = new BoxDecoration(
            Color: Color.Parse("#FF102030"),
            Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FF203040"), 2)),
            BorderRadius: BorderRadius.Circular(4));
        var end = new BoxDecoration(
            Color: Color.Parse("#FF90A0B0"),
            Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FFA0B0C0"), 6)),
            BorderRadius: BorderRadius.Circular(20));
        var tween = new DecorationTween(begin, end);

        Assert.Same(begin, tween.Begin);
        Assert.Same(end, tween.End);

        var midpoint = Assert.IsType<BoxDecoration>(tween.Evaluate(0.5));
        Assert.Equal(Color.Parse("#FF506070"), midpoint.Color);
        Assert.Equal(4, ((Plumix.Rendering.Border)midpoint.Border!).Top.Width);
        Assert.Equal(12, midpoint.BorderRadius!.Value.Radius);

        tween.Begin = null;
        var scaled = Assert.IsType<BoxDecoration>(tween.Evaluate(0.5));
        Assert.Equal(0x7F, scaled.Color!.Value.A);
        Assert.Equal(3, ((Plumix.Rendering.Border)scaled.Border!).Top.Width);

        tween.End = null;
        Assert.Throws<InvalidOperationException>(() => tween.Evaluate(0.5));
    }

    [Fact]
    public void DecoratedBoxTransition_ExposesDefaultsRebuildsAndRebindsAnimation()
    {
        var firstDecoration = new BoxDecoration(Color: Color.Parse("#FF123456"));
        var secondDecoration = new BoxDecoration(Color: Color.Parse("#FFABCDEF"));
        var replacementDecoration = new BoxDecoration(Color: Color.Parse("#FF654321"));
        var first = new TestValueAnimation<Decoration>(
            firstDecoration,
            AnimationStatus.Forward);
        var second = new TestValueAnimation<Decoration>(
            replacementDecoration,
            AnimationStatus.Reverse);
        var child = new SizedBox(width: 40, height: 20);
        var transition = new DecoratedBoxTransition(first, child);

        Assert.Same(first, transition.Decoration);
        Assert.Same(first, transition.Listenable);
        Assert.Equal(DecorationPosition.Background, transition.Position);
        Assert.Same(child, transition.Child);
        Assert.Throws<ArgumentNullException>(() => new DecoratedBoxTransition(null!, child));
        Assert.Throws<ArgumentNullException>(() => new DecoratedBoxTransition(first, null!));

        var owner = new BuildOwner();
        var root = new TestRootElement(new DecoratedBoxTransition(
            decoration: first,
            position: DecorationPosition.Foreground,
            child: child));
        Mount(root, owner);

        var render = Assert.IsType<RenderDecoratedBox>(root.ChildElement!.RenderObject);
        Assert.Same(firstDecoration, render.DecorationValue);
        Assert.Equal(DecorationPosition.Foreground, render.Position);
        Assert.Equal(1, first.ListenerCount);

        first.Set(secondDecoration, AnimationStatus.Completed);
        owner.FlushBuild();
        render = Assert.IsType<RenderDecoratedBox>(root.ChildElement.RenderObject);
        Assert.Same(secondDecoration, render.DecorationValue);

        root.Update(new DecoratedBoxTransition(
            decoration: second,
            position: DecorationPosition.Background,
            child: child));
        owner.FlushBuild();

        render = Assert.IsType<RenderDecoratedBox>(root.ChildElement.RenderObject);
        Assert.Same(replacementDecoration, render.DecorationValue);
        Assert.Equal(DecorationPosition.Background, render.Position);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);

        root.Unmount();
        Assert.Equal(0, second.ListenerCount);
    }

    [Fact]
    public void AnimatableAnimate_ForwardsParentLifecycleAndEvaluatesTween()
    {
        var parent = new TestAnimation(0.25, AnimationStatus.Forward);
        var tween = new DecorationTween(
            begin: new BoxDecoration(Color: Color.Parse("#FF000000")),
            end: new BoxDecoration(Color: Color.Parse("#FFFFFFFF")));
        Animation<Decoration> animation = tween.Animate(parent);
        int valueChanges = 0;
        AnimationStatus? status = null;
        animation.AddListener(() => valueChanges++);
        animation.AddStatusListener(value => status = value);

        Assert.Equal(AnimationStatus.Forward, animation.Status);
        Assert.Equal(Color.Parse("#FF3F3F3F"), Assert.IsType<BoxDecoration>(animation.Value).Color);
        Assert.Equal(1, parent.ListenerCount);

        parent.Set(0.75, AnimationStatus.Reverse);

        Assert.Equal(1, valueChanges);
        Assert.Equal(AnimationStatus.Reverse, status);
        Assert.Equal(Color.Parse("#FFBFBFBF"), Assert.IsType<BoxDecoration>(animation.Value).Color);
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TestAnimation : Animation<double>
    {
        private readonly List<Action> _listeners = [];
        private readonly List<Action<AnimationStatus>> _statusListeners = [];
        private double _value;
        private AnimationStatus _status;

        public TestAnimation(double value, AnimationStatus status)
        {
            _value = value;
            _status = status;
        }

        public override double Value => _value;

        public override AnimationStatus Status => _status;

        public int ListenerCount => _listeners.Count;

        public override void AddListener(Action listener) => _listeners.Add(listener);

        public override void RemoveListener(Action listener) => _listeners.Remove(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener) => _statusListeners.Add(listener);

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            _statusListeners.Remove(listener);
        }

        public void Set(double value, AnimationStatus status)
        {
            AnimationStatus previousStatus = _status;
            _value = value;
            _status = status;
            if (previousStatus != status)
            {
                foreach (var listener in _statusListeners.ToArray())
                {
                    listener(status);
                }
            }

            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
        }
    }

    private sealed class TestValueAnimation<T> : Animation<T>
    {
        private readonly List<Action> _listeners = [];
        private readonly List<Action<AnimationStatus>> _statusListeners = [];
        private T _value;
        private AnimationStatus _status;

        public TestValueAnimation(T value, AnimationStatus status)
        {
            _value = value;
            _status = status;
        }

        public override T Value => _value;

        public override AnimationStatus Status => _status;

        public int ListenerCount => _listeners.Count;

        public override void AddListener(Action listener) => _listeners.Add(listener);

        public override void RemoveListener(Action listener) => _listeners.Remove(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener) => _statusListeners.Add(listener);

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            _statusListeners.Remove(listener);
        }

        public void Set(T value, AnimationStatus status)
        {
            AnimationStatus previousStatus = _status;
            _value = value;
            _status = status;
            if (previousStatus != status)
            {
                foreach (var listener in _statusListeners.ToArray())
                {
                    listener(status);
                }
            }

            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
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

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
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
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
