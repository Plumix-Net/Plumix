using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Engine contracts: flutter/engine/src/flutter/testing/dart/fragment_shader_test.dart.
public sealed class FragmentShaderBackendTests
{
    private const string Source = """
        uniform float opacity;
        uniform float3 rgb;
        half4 main(float2 coordinate) { return half4(rgb * opacity, opacity); }
        """;

    [Fact]
    public void Uniforms_AreZeroInitializedAndFollowDeclarationOrder()
    {
        RuntimeFragmentProgram program = RuntimeFragmentProgram.Compile(Source);
        Assert.Equal(4, program.FloatCount);
        using RuntimeFragmentShader shader = program.FragmentShader();
        using RenderTargetBitmap empty = Render(canvas => canvas.DrawPaint(new Paint { Shader = shader }));
        Assert.Equal(0, RasterBackend.PixelAt(empty, 10, 10).A);
        shader.SetFloat(0, 0.5);
        shader.SetFloat(1, 1.0);
        shader.SetFloat(2, 0.0);
        shader.SetFloat(3, 0.25);
        using RenderTargetBitmap filled = Render(canvas => canvas.DrawRect(
            new Rect(0, 0, 40, 40), new Paint { Shader = shader }));
        AssertPixel(filled, 10, 10, 128, 0, 32, 128);
    }

    [Fact]
    public void Recording_CopiesUniformsPerDrawAndSurvivesMutationAndDisposal()
    {
        RuntimeFragmentProgram program = RuntimeFragmentProgram.Compile(Source);
        RuntimeFragmentShader shader = program.FragmentShader();
        shader.SetFloat(0, 1.0);
        shader.SetFloat(1, 1.0);
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        canvas.DrawRect(new Rect(0, 0, 20, 40), new Paint { Shader = shader });
        shader.SetFloat(1, 0.0);
        shader.SetFloat(3, 1.0);
        canvas.ClipRect(new Rect(20, 0, 20, 40));
        canvas.DrawPaint(new Paint { Shader = shader });
        Picture picture = recorder.EndRecording();
        shader.Dispose();
        Assert.True(shader.DebugDisposed);
        using RenderTargetBitmap image = Replay(picture);
        AssertPixel(image, 10, 10, 255, 0, 0, 255);
        AssertPixel(image, 30, 10, 0, 0, 255, 255);
    }

    [Fact]
    public void ShaderInstances_HaveIndependentUniformBuffers()
    {
        RuntimeFragmentProgram program = RuntimeFragmentProgram.Compile(Source);
        using RuntimeFragmentShader first = program.FragmentShader();
        using RuntimeFragmentShader second = program.FragmentShader();
        first.SetFloat(0, 1.0);
        first.SetFloat(1, 1.0);
        using RenderTargetBitmap image = Render(canvas => canvas.DrawPaint(new Paint { Shader = second }));
        Assert.Equal(0, RasterBackend.PixelAt(image, 10, 10).A);
    }

    [Fact]
    public void DisposedShader_RejectsWritesRecordingAndRepeatedDisposal()
    {
        RuntimeFragmentShader shader = RuntimeFragmentProgram.Compile(Source).FragmentShader();
        Assert.Throws<IndexOutOfRangeException>(() => shader.SetFloat(-1, 0));
        Assert.Throws<IndexOutOfRangeException>(() => shader.SetFloat(4, 0));
        shader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => shader.SetFloat(0, 0));
        Assert.Throws<ObjectDisposedException>(() => shader.Snapshot());
        Assert.Throws<ObjectDisposedException>(() => shader.Dispose());
        var canvas = new Canvas(new PictureRecorder());
        Assert.Throws<ObjectDisposedException>(() => canvas.DrawPaint(new Paint { Shader = shader }));
    }

    [Fact]
    public void Coordinates_AreLocalUnderTransformsAndClips()
    {
        RuntimeFragmentProgram program = RuntimeFragmentProgram.Compile("""
            half4 main(float2 p) { return half4(p.x / 20.0, p.y / 20.0, 0, 1); }
            """);
        Assert.Equal(0, program.FloatCount);
        using RuntimeFragmentShader shader = program.FragmentShader();
        using RenderTargetBitmap image = Render(canvas =>
        {
            canvas.Translate(10, 10);
            canvas.Scale(2, 2);
            canvas.ClipRect(new Rect(0, 0, 10, 10));
            canvas.DrawPaint(new Paint { Shader = shader });
        });
        AssertPixel(image, 20, 20, 67, 67, 0, 255);
        Assert.Equal(0, RasterBackend.PixelAt(image, 5, 5).A);
        Assert.Equal(0, RasterBackend.PixelAt(image, 35, 35).A);
    }

    [Fact]
    public void AlphaAndMaskFilter_ApplyToTheNativeShader()
    {
        using RuntimeFragmentShader shader = RuntimeFragmentProgram.Compile(Source).FragmentShader();
        shader.SetFloat(0, 1.0);
        shader.SetFloat(1, 1.0);
        using RenderTargetBitmap image = Render(canvas => canvas.DrawCircle(new Point(20, 20), 12,
            new Paint
            {
                Shader = shader,
                Color = Color.From(alpha: 0.5, red: 0, green: 0, blue: 0),
                MaskFilter = MaskFilter.Blur(BlurStyle.Normal, 1.0),
            }));
        AssertPixel(image, 20, 20, 128, 0, 0, 128);
        Assert.InRange(RasterBackend.PixelAt(image, 32, 20).A, 1, 127);
    }

    [Fact]
    public void InvalidSource_ReportsCompilationDiagnostics()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            RuntimeFragmentProgram.Compile("half4 main(float2 p) { return missing_name; }"));
        Assert.Contains("missing_name", error.Message);
    }

    internal static RenderTargetBitmap Render(Action<Canvas> draw)
    {
        var recorder = new PictureRecorder();
        draw(new Canvas(recorder));
        Picture picture = recorder.EndRecording();
        return Replay(picture);
    }

    private static RenderTargetBitmap Replay(Picture picture)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(40, 40));
        using DrawingContext context = bitmap.CreateDrawingContext();
        picture.Playback(context, default);
        return bitmap;
    }

    private static void AssertPixel(Bitmap image, int x, int y, int r, int g, int b, int a)
    {
        var pixel = RasterBackend.PixelAt(image, x, y);
        Assert.InRange((int)pixel.R, r - 2, r + 2);
        Assert.InRange((int)pixel.G, g - 2, g + 2);
        Assert.InRange((int)pixel.B, b - 2, b + 2);
        Assert.InRange((int)pixel.A, a - 2, a + 2);
    }
}
