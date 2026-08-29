using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Foundation;
using Plumix.UI;
using Xunit;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart
// (RenderObject.applyPaintTransform/getTransformTo/localToGlobal/globalToLocal);
// flutter/packages/flutter/lib/src/rendering/box.dart (RenderBox.applyPaintTransform)

namespace Plumix.Tests;

public sealed class PaintTransformTests
{
    [Fact]
    public void ApplyPaintTransform_RenderBox_TranslatesByTheChildParentDataOffset()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var padded = new RenderPadding(new Thickness(12, 7, 0, 0), child);
        var renderView = new RenderView { Child = padded };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 100));

        Matrix4 transform = Matrix4.Identity();
        padded.ApplyPaintTransform(child, transform);

        Assert.Equal(new Point(12, 7), MatrixUtils.TransformPoint(transform, default));
        Assert.Equal(new Point(12, 7), child.LocalToGlobal(default));
        Assert.Equal(new Point(12, 7), child.GetPaintOffsetToRoot());
    }

    [Fact]
    public void LocalToGlobal_ComposesAncestorTransformsAndOffsets()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var scaled = new RenderTransform(Matrix4.Diagonal3Values(2.0, 3.0, 1.0)) { Child = child };
        var padded = new RenderPadding(new Thickness(5, 9, 0, 0), scaled);
        var renderView = new RenderView { Child = padded };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(200, 200));

        // The scale applies in the transform's own space, then the padding offset shifts the result.
        Assert.Equal(new Point(5, 9), child.LocalToGlobal(default));
        Assert.Equal(new Point(25, 39), child.LocalToGlobal(new Point(10, 10)));
        Assert.Equal(new Point(10, 10), child.GlobalToLocal(new Point(25, 39)));

        // Relative to an ancestor the padding offset drops out again.
        Assert.Equal(new Point(20, 30), child.LocalToGlobal(new Point(10, 10), scaled));
    }

    [Fact]
    public void GetTransformTo_ResolvesInsideSubtreesHiddenFromSemantics()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var padded = new RenderPadding(new Thickness(4, 6, 0, 0), child);
        var excluded = new RenderExcludeSemantics(child: padded);
        var renderView = new RenderView { Child = excluded };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 100));

        // The transform chain walks the parent chain, so it no longer depends on the semantics walk.
        Assert.Equal(new Point(4, 6), child.LocalToGlobal(default));
    }

    [Fact]
    public void GetTransformTo_ResolvesASiblingThroughTheCommonAncestor()
    {
        var first = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var second = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var row = new RenderFlex(
            children: [first, second],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        var renderView = new RenderView { Child = row };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 100));

        // Flutter's `getTransformTo` accepts any render object in the same tree, not just an
        // ancestor: it walks both chains to their common ancestor and inverts the target's half. The
        // row places `second` 20 logical pixels to the right of `first`, so `first`'s origin sits at
        // x = -20 in `second`'s coordinate space.
        Assert.Equal(new Point(-20, 0), first.LocalToGlobal(default, second));

        // A render object in a different tree is still an error.
        var orphan = new RenderConstrainedBox(BoxConstraints.Tight(new Size(5, 5)));
        var orphanView = new RenderView { Child = orphan };
        var orphanPipeline = new PipelineOwner(orphanView);
        orphanPipeline.Attach(orphanView);
        orphanPipeline.FlushLayout(new Size(50, 50));
        Assert.Throws<FlutterError>(() => first.GetTransformTo(orphan));
    }

    [Fact]
    public void ApplyPaintTransform_FittedBox_CollapsesWhenEitherSideIsEmpty()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(0, 0)));
        var fitted = new RenderFittedBox(BoxFit.Contain) { Child = child };
        var renderView = new RenderView { Child = fitted };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 100));

        Matrix4 transform = Matrix4.Identity();
        fitted.ApplyPaintTransform(child, transform);

        // Flutter zeroes the matrix for a child it does not paint, and `paintsChild` reports it.
        Assert.Equal(Matrix4.Zero(), transform);
        Assert.False(fitted.PaintsChild(child));
    }

    [Fact]
    public void ApplyPaintTransform_FractionalTranslation_ShiftsByAFractionOfItsSize()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(40, 20)));
        var translated = new RenderFractionalTranslation(new Vector(0.5, -1.0)) { Child = child };
        var renderView = new RenderView { Child = translated };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(new Point(20, -20), child.LocalToGlobal(default));
    }

    [Fact]
    public void PathCombine_DifferenceRemovesTheSecondPath()
    {
        var outer = new Path();
        outer.AddRect(new Rect(0, 0, 100, 50));
        var gap = new Path();
        gap.AddRect(new Rect(20, -1, 30, 2));

        Path combined = Path.Combine(PathOperation.Difference, outer, gap);

        Assert.True(combined.Contains(new Point(10, 0.5)));
        Assert.False(combined.Contains(new Point(30, 0.5)));
        Assert.True(combined.Contains(new Point(30, 10)));
        Assert.Equal(new Rect(0, 0, 100, 50), combined.GetBounds());
        Assert.Throws<InvalidOperationException>(() => combined.LineTo(0, 0));
    }

    [Fact]
    public void PathCombine_IntersectUnionAndXorFollowTheirSetOperations()
    {
        var left = new Path();
        left.AddRect(new Rect(0, 0, 40, 40));
        var right = new Path();
        right.AddRect(new Rect(20, 0, 40, 40));

        // Painting defers to Avalonia's combined geometry with the matching mode.
        var excluded = (CombinedGeometry)Path.Combine(PathOperation.Difference, left, right).ToGeometry();
        Assert.Equal(GeometryCombineMode.Exclude, excluded.GeometryCombineMode);
        Assert.Equal(
            GeometryCombineMode.Intersect,
            ((CombinedGeometry)Path.Combine(PathOperation.Intersect, left, right).ToGeometry()).GeometryCombineMode);

        Assert.True(Path.Combine(PathOperation.Intersect, left, right).Contains(new Point(30, 20)));
        Assert.False(Path.Combine(PathOperation.Intersect, left, right).Contains(new Point(10, 20)));
        Assert.True(Path.Combine(PathOperation.Union, left, right).Contains(new Point(50, 20)));
        Assert.False(Path.Combine(PathOperation.Xor, left, right).Contains(new Point(30, 20)));
        Assert.True(Path.Combine(PathOperation.ReverseDifference, left, right).Contains(new Point(50, 20)));
        Assert.False(Path.Combine(PathOperation.ReverseDifference, left, right).Contains(new Point(30, 20)));
    }
}
