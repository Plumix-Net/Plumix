using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Path = Plumix.UI.Path;

// Dart parity source: engine/src/flutter/testing/dart/canvas_test.dart (getTransform, clip bounds,
// restoreToCount)

namespace Plumix.Tests;

public sealed class CanvasDartParityTests
{
    private static Canvas NewCanvas() => new(new PictureRecorder());

    private static void AssertCloseToTransform(double[] expected, double[] actual)
    {
        Assert.Equal(16, actual.Length);
        for (int index = 0; index < 16; index++)
        {
            Assert.True(Math.Abs(expected[index] - actual[index]) < 1e-10, $"entry {index}: {actual[index]}");
        }
    }

    private static bool IsCloseToTransform(double[] expected, double[] actual) =>
        expected.Zip(actual).All(pair => Math.Abs(pair.First - pair.Second) < 1e-10);

    private static void AssertCloseToRect(Rect expected, Rect actual)
    {
        Assert.True(IsCloseToRect(expected, actual), $"expected {expected}, got {actual}");
    }

    private static bool IsCloseToRect(Rect expected, Rect actual) =>
        Math.Abs(expected.Left - actual.Left) < 1e-6
        && Math.Abs(expected.Top - actual.Top) < 1e-6
        && Math.Abs(expected.Right - actual.Right) < 1e-6
        && Math.Abs(expected.Bottom - actual.Bottom) < 1e-6;

    [Fact]
    public void Translate_AffectsGetTransform()
    {
        Canvas canvas = NewCanvas();
        canvas.Translate(12, 14.5);
        double[] matrix = Matrix4.TranslationValues(12, 14.5, 0).Storage;
        double[] curMatrix = canvas.GetTransform();
        AssertCloseToTransform(matrix, curMatrix);
        canvas.Translate(10, 10);
        double[] newCurMatrix = canvas.GetTransform();
        Assert.False(IsCloseToTransform(matrix, newCurMatrix));
        AssertCloseToTransform(matrix, curMatrix);
    }

    [Fact]
    public void Scale_AffectsGetTransform()
    {
        Canvas canvas = NewCanvas();
        canvas.Scale(12, 14.5);
        double[] matrix = Matrix4.Diagonal3Values(12, 14.5, 1).Storage;
        double[] curMatrix = canvas.GetTransform();
        AssertCloseToTransform(matrix, curMatrix);
        canvas.Scale(10, 10);
        Assert.False(IsCloseToTransform(matrix, canvas.GetTransform()));
        AssertCloseToTransform(matrix, curMatrix);
    }

    [Fact]
    public void Rotate_AffectsGetTransform()
    {
        Canvas canvas = NewCanvas();
        canvas.Rotate(Math.PI);
        double[] matrix = Matrix4.RotationZ(Math.PI).Storage;
        double[] curMatrix = canvas.GetTransform();
        AssertCloseToTransform(matrix, curMatrix);
        canvas.Rotate(Math.PI / 2);
        Assert.False(IsCloseToTransform(matrix, canvas.GetTransform()));
        AssertCloseToTransform(matrix, curMatrix);
    }

    [Fact]
    public void Skew_AffectsGetTransform()
    {
        Canvas canvas = NewCanvas();
        canvas.Skew(12, 14.5);
        Matrix4 expected = Matrix4.Identity();
        expected.SetEntry(0, 1, 12);
        expected.SetEntry(1, 0, 14.5);
        double[] curMatrix = canvas.GetTransform();
        AssertCloseToTransform(expected.Storage, curMatrix);
        canvas.Skew(10, 10);
        Assert.False(IsCloseToTransform(expected.Storage, canvas.GetTransform()));
        AssertCloseToTransform(expected.Storage, curMatrix);
    }

    [Fact]
    public void Transform_AffectsGetTransform()
    {
        Canvas canvas = NewCanvas();
        Matrix4 matrix = Matrix4.TranslationValues(12.0, 14.5, 0.0);
        matrix.Multiply(Matrix4.Diagonal3Values(12.0, 14.5, 1.0));
        canvas.Transform(matrix);
        double[] curMatrix = canvas.GetTransform();
        AssertCloseToTransform(matrix.Storage, curMatrix);
        canvas.Translate(10, 10);
        Assert.False(IsCloseToTransform(matrix.Storage, canvas.GetTransform()));
        AssertCloseToTransform(matrix.Storage, curMatrix);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClipRect_AffectsGetClipBounds(bool doAntiAlias)
    {
        // The LR/TB-swapped variants of the Dart test cannot be expressed: an Avalonia Rect is always sorted.
        var clipRect = new Rect(new Point(10.2, 11.3), new Point(20.4, 25.7));
        Canvas canvas = NewCanvas();
        canvas.ClipRect(clipRect, doAntiAlias: doAntiAlias);
        Rect clipExpandedBounds = doAntiAlias
            ? new Rect(new Point(10, 11), new Point(21, 26))
            : clipRect;

        AssertClipBoundsLifecycle(canvas, clipExpandedBounds);
    }

    [Fact]
    public void ClipRect_WithMatrix_AffectsGetClipBounds()
    {
        Canvas canvas = NewCanvas();
        var clipBounds1 = new Rect(0, 0, 10, 10);
        var clipBounds2 = new Rect(10, 10, 10, 10);

        canvas.Save();
        canvas.ClipRect(clipBounds1, doAntiAlias: false);
        canvas.Translate(0, 10.0);
        canvas.ClipRect(clipBounds1, doAntiAlias: false);
        Assert.True(IsEmpty(canvas.GetDestinationClipBounds()));
        canvas.Restore();

        canvas.Save();
        canvas.ClipRect(clipBounds1, doAntiAlias: false);
        canvas.Translate(-10.0, -10.0);
        canvas.ClipRect(clipBounds2, doAntiAlias: false);
        Assert.Equal(clipBounds1, canvas.GetDestinationClipBounds());
        canvas.Restore();
    }

    [Fact]
    public void ClipRRect_AntiAliased_AffectsGetClipBounds()
    {
        Canvas canvas = NewCanvas();
        var clipBounds = new Rect(new Point(10.2, 11.3), new Point(20.4, 25.7));
        canvas.ClipRRect(RRect.FromRectAndRadius(clipBounds, Radius.Circular(3)));

        AssertClipBoundsLifecycle(canvas, new Rect(new Point(10, 11), new Point(21, 26)));
    }

    [Fact]
    public void ClipRRect_Aliased_AffectsGetClipBounds()
    {
        Canvas canvas = NewCanvas();
        var clipBounds = new Rect(new Point(10.2, 11.3), new Point(20.4, 25.7));
        canvas.ClipRRect(RRect.FromRectAndRadius(clipBounds, Radius.Circular(3)), doAntiAlias: false);

        AssertClipBoundsLifecycle(canvas, clipBounds, doAntiAliasInnerClip: false);
    }

    [Fact]
    public void ClipRRect_WithMatrix_AffectsGetClipBounds()
    {
        Canvas canvas = NewCanvas();
        var clipBounds1 = new Rect(0, 0, 10, 10);
        var clipBounds2 = new Rect(10, 10, 10, 10);
        RRect clip1 = RRect.FromRectAndRadius(clipBounds1, Radius.Circular(3));
        RRect clip2 = RRect.FromRectAndRadius(clipBounds2, Radius.Circular(3));

        canvas.Save();
        canvas.ClipRRect(clip1, doAntiAlias: false);
        canvas.Translate(0, 10.0);
        canvas.ClipRRect(clip1, doAntiAlias: false);
        Assert.True(IsEmpty(canvas.GetDestinationClipBounds()));
        canvas.Restore();

        canvas.Save();
        canvas.ClipRRect(clip1, doAntiAlias: false);
        canvas.Translate(-10.0, -10.0);
        canvas.ClipRRect(clip2, doAntiAlias: false);
        Assert.Equal(clipBounds1, canvas.GetDestinationClipBounds());
        canvas.Restore();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClipPath_AffectsGetClipBounds(bool doAntiAlias)
    {
        Canvas canvas = NewCanvas();
        var clipBounds = new Rect(new Point(10.2, 11.3), new Point(20.4, 25.7));
        var clip = new Path();
        clip.AddRect(clipBounds);
        clip.AddOval(clipBounds);
        canvas.ClipPath(clip, doAntiAlias: doAntiAlias);

        AssertClipBoundsLifecycle(
            canvas,
            doAntiAlias ? new Rect(new Point(10, 11), new Point(21, 26)) : clipBounds,
            doAntiAliasInnerClip: doAntiAlias);
    }

    [Fact]
    public void ClipPath_WithMatrix_AffectsGetClipBounds()
    {
        Canvas canvas = NewCanvas();
        var clipBounds1 = new Rect(0, 0, 10, 10);
        var clipBounds2 = new Rect(10, 10, 10, 10);
        var clip1 = new Path();
        clip1.AddRect(clipBounds1);
        clip1.AddOval(clipBounds1);
        var clip2 = new Path();
        clip2.AddRect(clipBounds2);
        clip2.AddOval(clipBounds2);

        canvas.Save();
        canvas.ClipPath(clip1, doAntiAlias: false);
        canvas.Translate(0, 10.0);
        canvas.ClipPath(clip1, doAntiAlias: false);
        Assert.True(IsEmpty(canvas.GetDestinationClipBounds()));
        canvas.Restore();

        canvas.Save();
        canvas.ClipPath(clip1, doAntiAlias: false);
        canvas.Translate(-10.0, -10.0);
        canvas.ClipPath(clip2, doAntiAlias: false);
        Assert.Equal(clipBounds1, canvas.GetDestinationClipBounds());
        canvas.Restore();
    }

    [Fact]
    public void ClipRectDifference_DoesNotAffectGetClipBounds()
    {
        Canvas canvas = NewCanvas();
        var clipBounds = new Rect(new Point(10.2, 11.3), new Point(20.4, 25.7));
        canvas.ClipRect(clipBounds, doAntiAlias: false);

        Rect initialLocalBounds = canvas.GetLocalClipBounds();
        Rect initialDestinationBounds = canvas.GetDestinationClipBounds();
        AssertCloseToRect(clipBounds, initialLocalBounds);
        AssertCloseToRect(clipBounds, initialDestinationBounds);

        canvas.ClipRect(new Rect(0, 0, 15, 15), doAntiAlias: false, clipOp: ClipOp.Difference);
        Assert.Equal(initialLocalBounds, canvas.GetLocalClipBounds());
        Assert.Equal(initialDestinationBounds, canvas.GetDestinationClipBounds());
    }

    [Fact]
    public void RestoreToCount_CanWork()
    {
        Canvas canvas = NewCanvas();
        for (int i = 0; i < 5; i++)
        {
            canvas.Save();
        }

        Assert.Equal(6, canvas.GetSaveCount());
        canvas.RestoreToCount(2);
        Assert.Equal(2, canvas.GetSaveCount());
        canvas.Restore();
        Assert.Equal(1, canvas.GetSaveCount());
    }

    [Fact]
    public void RestoreToCount_LessThanOne_ResetsTheStack()
    {
        Canvas canvas = NewCanvas();
        for (int i = 0; i < 5; i++)
        {
            canvas.Save();
        }

        Assert.Equal(6, canvas.GetSaveCount());
        canvas.RestoreToCount(0);
        Assert.Equal(1, canvas.GetSaveCount());
    }

    [Fact]
    public void RestoreToCount_GreaterThanSaveCount_DoesNothing()
    {
        Canvas canvas = NewCanvas();
        for (int i = 0; i < 5; i++)
        {
            canvas.Save();
        }

        Assert.Equal(6, canvas.GetSaveCount());
        canvas.RestoreToCount(canvas.GetSaveCount() + 1);
        Assert.Equal(6, canvas.GetSaveCount());
    }

    [Fact]
    public void SaveLayer_RestoresTheTransformAndClip()
    {
        Canvas canvas = NewCanvas();
        canvas.ClipRect(new Rect(0, 0, 50, 50), doAntiAlias: false);
        canvas.SaveLayer(null, new Paint());
        canvas.Translate(5, 5);
        canvas.ClipRect(new Rect(0, 0, 10, 10), doAntiAlias: false);
        Assert.Equal(new Rect(5, 5, 10, 10), canvas.GetDestinationClipBounds());
        Assert.Equal(2, canvas.GetSaveCount());

        canvas.Restore();

        Assert.Equal(new Rect(0, 0, 50, 50), canvas.GetDestinationClipBounds());
        AssertCloseToTransform(Matrix4.Identity().Storage, canvas.GetTransform());
    }

    [Fact]
    public void CullRect_IsTheInitialClip()
    {
        var canvas = new Canvas(new PictureRecorder(), new Rect(0, 0, 100, 50));
        Assert.Equal(new Rect(0, 0, 100, 50), canvas.GetDestinationClipBounds());

        Canvas unbounded = NewCanvas();
        Assert.Equal(Canvas.LargestRect, unbounded.GetDestinationClipBounds());
    }

    // The shared body of the engine's clip-bounds tests: the clip set before this call reports
    // `clipExpandedBounds`, an inner clip changes both bounds until restored, and a scale changes only the
    // local bounds.
    private static void AssertClipBoundsLifecycle(
        Canvas canvas,
        Rect clipExpandedBounds,
        bool doAntiAliasInnerClip = true)
    {
        Rect initialLocalBounds = canvas.GetLocalClipBounds();
        Rect initialDestinationBounds = canvas.GetDestinationClipBounds();
        AssertCloseToRect(clipExpandedBounds, initialLocalBounds);
        AssertCloseToRect(clipExpandedBounds, initialDestinationBounds);

        canvas.Save();
        canvas.ClipRect(new Rect(0, 0, 15, 15), doAntiAlias: doAntiAliasInnerClip);
        Assert.False(IsCloseToRect(clipExpandedBounds, canvas.GetLocalClipBounds()));
        Assert.False(IsCloseToRect(clipExpandedBounds, canvas.GetDestinationClipBounds()));
        AssertCloseToRect(clipExpandedBounds, initialLocalBounds);
        AssertCloseToRect(clipExpandedBounds, initialDestinationBounds);
        canvas.Restore();

        Assert.Equal(initialLocalBounds, canvas.GetLocalClipBounds());
        Assert.Equal(initialDestinationBounds, canvas.GetDestinationClipBounds());

        canvas.Save();
        canvas.Scale(2, 2);
        var scaledExpandedBounds = new Rect(
            new Point(clipExpandedBounds.Left / 2.0, clipExpandedBounds.Top / 2.0),
            new Point(clipExpandedBounds.Right / 2.0, clipExpandedBounds.Bottom / 2.0));
        AssertCloseToRect(scaledExpandedBounds, canvas.GetLocalClipBounds());
        AssertCloseToRect(clipExpandedBounds, canvas.GetDestinationClipBounds());
        canvas.Restore();

        Assert.Equal(initialLocalBounds, canvas.GetLocalClipBounds());
        Assert.Equal(initialDestinationBounds, canvas.GetDestinationClipBounds());
    }

    private static bool IsEmpty(Rect rect) => rect.Width <= 0.0 || rect.Height <= 0.0;
}
