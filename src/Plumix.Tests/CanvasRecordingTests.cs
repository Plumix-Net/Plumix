using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Path = Plumix.UI.Path;

// Dart parity source (reference): dart:ui Canvas/Picture/PictureRecorder and
// flutter/packages/flutter/lib/src/painting/clip.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class CanvasRecordingTests
{
    private static Paint RedStroke() =>
        new() { Color = new Color(0xFFFF0000), Style = PaintingStyle.Stroke, StrokeWidth = 1.0 };

    [Fact]
    public void PathCopy_PreservesOpenContoursAndFillTypeAfterSourceMutation()
    {
        var source = new Path { FillType = PathFillType.EvenOdd };
        source.AddOval(Rect.FromLTWH(0, 0, 20, 20));
        source.MoveTo(30, 0);
        source.LineTo(30, 20);
        var copy = new Path(source);
        source.LineTo(50, 20);
        source.Reset();
        Assert.Equal(PathFillType.EvenOdd, copy.FillType);
        Assert.Equal(Rect.FromLTWH(0, 0, 30, 20), copy.GetBounds());
        Assert.True(copy.Contains(new Offset(10, 10)));
        Assert.False(copy.Contains(new Offset(1, 1)));
        copy.LineTo(40, 20);
        Assert.Equal(Rect.FromLTWH(0, 0, 40, 20), copy.GetBounds());
        Assert.Equal(default, source.GetBounds());
    }

    [Fact]
    public void PathCopy_PreservesCombinedGeometryAfterOperandsAreReset()
    {
        var outer = new Path();
        outer.AddRect(Rect.FromLTWH(0, 0, 20, 20));
        var inner = new Path();
        inner.AddRect(Rect.FromLTWH(5, 5, 10, 10));
        var source = Path.Combine(PathOperation.Difference, outer, inner);
        var copy = new Path(source);
        outer.Reset();
        inner.Reset();
        source.Reset();
        Assert.True(copy.Contains(new Offset(2, 2)));
        Assert.False(copy.Contains(new Offset(10, 10)));
        Assert.Equal(Rect.FromLTWH(0, 0, 20, 20), copy.GetBounds());
    }

    [Fact]
    public void Canvas_StartsWithOneSaveLevel()
    {
        var canvas = new Canvas(new PictureRecorder());

        Assert.Equal(1, canvas.GetSaveCount());
    }

    [Fact]
    public void Canvas_SaveAndRestore_TrackTheSaveCount()
    {
        var canvas = new Canvas(new PictureRecorder());

        canvas.Save();
        canvas.Save();
        Assert.Equal(3, canvas.GetSaveCount());

        canvas.Restore();
        Assert.Equal(2, canvas.GetSaveCount());
    }

    [Fact]
    public void Canvas_Restore_IsANoOpOnAnEmptyStack()
    {
        var canvas = new Canvas(new PictureRecorder());

        canvas.Restore();
        canvas.Restore();

        Assert.Equal(1, canvas.GetSaveCount());
    }

    [Fact]
    public void Canvas_RestoreToCount_UnwindsToTheGivenLevel()
    {
        var canvas = new Canvas(new PictureRecorder());
        canvas.Save();
        int mark = canvas.GetSaveCount();
        canvas.Save();
        canvas.Save();

        canvas.RestoreToCount(mark);

        Assert.Equal(mark, canvas.GetSaveCount());
    }

    [Fact]
    public void Canvas_SaveLayer_OpensOneGroup()
    {
        var canvas = new Canvas(new PictureRecorder());

        canvas.SaveLayer(Rect.FromLTWH(0, 0, 10, 10), new Paint());

        Assert.Equal(2, canvas.GetSaveCount());
    }

    [Fact]
    public void PictureRecorder_RecordsWhileACanvasIsAttached()
    {
        var recorder = new PictureRecorder();
        Assert.False(recorder.IsRecording);

        var canvas = new Canvas(recorder);
        Assert.True(recorder.IsRecording);
        canvas.DrawLine(new Offset(0, 0), new Offset(1, 1), RedStroke());

        Picture picture = recorder.EndRecording();

        Assert.False(recorder.IsRecording);
        Assert.Equal(1, picture.CommandCount);
        Assert.Equal(1, picture.DrawCommandCount);
    }

    [Fact]
    public void PictureRecorder_EndRecording_ThrowsWhenCalledTwice()
    {
        var recorder = new PictureRecorder();
        _ = new Canvas(recorder);
        recorder.EndRecording();

        Assert.Throws<InvalidOperationException>(() => recorder.EndRecording());
    }

    [Fact]
    public void Picture_SeparatesDrawCommandsFromClipAndTransformCommands()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);

        canvas.Save();
        canvas.ClipRect(Rect.FromLTWH(0, 0, 5, 5));
        canvas.DrawRect(Rect.FromLTWH(0, 0, 4, 4), new Paint { Color = new Color(0xFFFF0000) });
        canvas.Restore();

        Picture picture = recorder.EndRecording();

        Assert.Equal(1, picture.DrawCommandCount);
        Assert.True(picture.CommandCount > picture.DrawCommandCount);
    }

    [Fact]
    public void Picture_Empty_IsEmptyAndRecordsNothing()
    {
        Assert.True(Picture.Empty.IsEmpty);
        Assert.Equal(0, Picture.Empty.CommandCount);
        Assert.Equal(0, Picture.Empty.DrawCommandCount);
    }

    [Fact]
    public void ClipContext_ClipNone_LeavesTheSaveCountBalancedAndClipsNothing()
    {
        var context = new PaintingContext(new ContainerLayer());
        int inside = 0;

        context.ClipRectAndPaint(
            Rect.FromLTWH(0, 0, 10, 10),
            Clip.None,
            Rect.FromLTWH(0, 0, 10, 10),
            () => inside = context.Canvas.GetSaveCount());

        // Dart still brackets the painter with save/restore, it just skips the clip call.
        Assert.Equal(2, inside);
        Assert.Equal(1, context.Canvas.GetSaveCount());
    }

    [Fact]
    public void ClipContext_AntiAliasWithSaveLayer_OpensASecondGroupAroundThePainter()
    {
        var context = new PaintingContext(new ContainerLayer());
        int inside = 0;

        context.ClipRectAndPaint(
            Rect.FromLTWH(0, 0, 10, 10),
            Clip.AntiAliasWithSaveLayer,
            Rect.FromLTWH(0, 0, 10, 10),
            () => inside = context.Canvas.GetSaveCount());

        Assert.Equal(3, inside);
        Assert.Equal(1, context.Canvas.GetSaveCount());
    }

    [Theory]
    [InlineData(Clip.HardEdge)]
    [InlineData(Clip.AntiAlias)]
    public void ClipContext_RestoresTheCanvasAfterEveryClipShape(Clip clipBehavior)
    {
        var context = new PaintingContext(new ContainerLayer());
        var bounds = Rect.FromLTWH(0, 0, 10, 10);
        var path = new Path();
        path.AddOval(bounds);

        context.ClipRectAndPaint(bounds, clipBehavior, bounds, () => { });
        context.ClipRRectAndPaint(RRect.FromRectAndRadius(bounds, 2.0), clipBehavior, bounds, () => { });
        context.ClipRSuperellipseAndPaint(
            RSuperellipse.FromRectAndRadius(bounds, new Radius(2.0, 2.0)),
            clipBehavior,
            bounds,
            () => { });
        context.ClipPathAndPaint(path, clipBehavior, bounds, () => { });

        Assert.Equal(1, context.Canvas.GetSaveCount());
    }

    [Fact]
    public void PaintingContext_Canvas_AppendsExactlyOnePictureLayerPerRecording()
    {
        var root = new ContainerLayer();
        var context = new PaintingContext(root);

        context.Canvas.DrawLine(new Offset(0, 0), new Offset(1, 1), RedStroke());
        context.Canvas.DrawLine(new Offset(1, 1), new Offset(2, 2), RedStroke());

        var picture = Assert.IsType<PictureLayer>(Assert.Single(root.Children));
        Assert.Null(picture.Picture);

        context.DebugStopRecordingIfNeeded();

        Assert.NotNull(picture.Picture);
        Assert.Equal(2, picture.Picture!.DrawCommandCount);
    }

    [Fact]
    public void PaintingContext_AddLayer_StopsTheRecordingFirst()
    {
        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        context.Canvas.DrawLine(new Offset(0, 0), new Offset(1, 1), RedStroke());

        var added = new ContainerLayer();
        context.AddLayer(added);

        Assert.Equal(2, root.Children.Count);
        var picture = Assert.IsType<PictureLayer>(root.Children[0]);
        Assert.NotNull(picture.Picture);
        Assert.Same(added, root.Children[1]);
    }

    [Fact]
    public void PaintingContext_SetIsComplexHint_StartsARecordingAndMarksTheLayer()
    {
        var root = new ContainerLayer();
        var context = new PaintingContext(root);

        context.SetIsComplexHint();
        context.SetWillChangeHint();

        var picture = Assert.IsType<PictureLayer>(Assert.Single(root.Children));
        Assert.True(picture.IsComplexHint);
        Assert.True(picture.WillChangeHint);
    }

    [Fact]
    public void PaintingContext_PictureLayer_CarriesTheEstimatedBoundsAsItsCanvasBounds()
    {
        var root = new ContainerLayer();
        var bounds = Rect.FromLTWH(0, 0, 40, 20);
        var context = new PaintingContext(root, bounds);

        context.Canvas.DrawLine(new Offset(0, 0), new Offset(1, 1), RedStroke());

        var picture = Assert.IsType<PictureLayer>(Assert.Single(root.Children));
        Assert.Equal(bounds, picture.CanvasBounds);
    }

    [Fact]
    public void PushTransform_BakesTheOffsetIntoTheEffectiveTransform()
    {
        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        var offset = new Offset(10, 20);

        TransformLayer? layer = context.PushTransform(
            needsCompositing: true,
            offset,
            Matrix4.Diagonal3Values(2.0, 2.0, 1.0),
            (_, _) => { });

        Assert.NotNull(layer);

        // translate(offset) * scale(2) * translate(-offset): the offset is a fixed point.
        Offset mappedOffset = MatrixUtils.TransformPoint(layer!.Transform, offset);
        Assert.Equal(offset.Dx, mappedOffset.Dx, 6);
        Assert.Equal(offset.Dy, mappedOffset.Dy, 6);

        Offset mappedOrigin = MatrixUtils.TransformPoint(layer.Transform, new Offset(0, 0));
        Assert.Equal(-10.0, mappedOrigin.Dx, 6);
        Assert.Equal(-20.0, mappedOrigin.Dy, 6);
    }

    [Fact]
    public void PushLayer_ClearsTheChildrenOfAReusedLayer()
    {
        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        var reused = new ContainerLayer();
        reused.Append(new PictureLayer());

        context.PushLayer(reused, (childContext, _) => childContext.SetIsComplexHint(), new Offset(0, 0));

        Assert.Single(reused.Children);
    }

    [Fact]
    public void PushClipRect_PassesTheOffsetThroughToThePainter()
    {
        var context = new PaintingContext(new ContainerLayer());
        Offset seen = default;

        context.PushClipRect(
            needsCompositing: false,
            new Offset(6, 7),
            Rect.FromLTWH(0, 0, 10, 10),
            (_, offset) => seen = offset);

        Assert.Equal(new Offset(6, 7), seen);
    }

    [Fact]
    public void PushClipPath_ShiftsTheClipPathByTheOffset()
    {
        var context = new PaintingContext(new ContainerLayer());
        var path = new Path();
        path.AddRect(Rect.FromLTWH(0, 0, 10, 10));

        ClipPathLayer? layer = context.PushClipPath(
            needsCompositing: true,
            new Offset(4, 5),
            Rect.FromLTWH(0, 0, 10, 10),
            path,
            (_, _) => { });

        Assert.NotNull(layer);
        Assert.Equal(Rect.FromLTWH(4, 5, 10, 10), layer!.ClipPath.GetBounds());

        // The caller's path is not mutated.
        Assert.Equal(Rect.FromLTWH(0, 0, 10, 10), path.GetBounds());
    }

    [Fact]
    public void PushClipRSuperellipse_ShiftsTheShapeByTheOffset()
    {
        var context = new PaintingContext(new ContainerLayer());

        ClipRSuperellipseLayer? layer = context.PushClipRSuperellipse(
            needsCompositing: true,
            new Offset(2, 3),
            Rect.FromLTWH(0, 0, 10, 10),
            RSuperellipse.FromRectAndRadius(Rect.FromLTWH(0, 0, 10, 10), new Radius(4.0, 4.0)),
            (_, _) => { });

        Assert.NotNull(layer);
        Assert.Equal(Rect.FromLTWH(2, 3, 10, 10), layer!.ClipRSuperellipse.Rect);
    }

    [Theory]
    [InlineData(Clip.None)]
    public void EveryClipPush_WithClipNone_ReturnsNullAndPaintsDirectly(Clip clipBehavior)
    {
        var context = new PaintingContext(new ContainerLayer());
        var bounds = Rect.FromLTWH(0, 0, 10, 10);
        var path = new Path();
        path.AddRect(bounds);
        int painted = 0;

        Assert.Null(context.PushClipRect(true, default, bounds, (_, _) => painted++, clipBehavior));
        Assert.Null(context.PushClipRRect(
            true,
            default,
            bounds,
            RRect.FromRectAndRadius(bounds, 1.0),
            (_, _) => painted++,
            clipBehavior));
        Assert.Null(context.PushClipPath(true, default, bounds, path, (_, _) => painted++, clipBehavior));
        Assert.Equal(3, painted);
    }
}
