using SkiaSharp;

namespace Plumix.UI;

// C#-only host infrastructure for CanvasPlayback. The INativeShader transport it implements is declared
// beside SkiaShaders (SkiaShaders.cs).

// A paint's solid color as a native shader, so shapes Avalonia's geometry would approximate (conic
// corners, superellipses) reach Skia as the exact path Dart's canvas hands it. The paint's alpha is
// applied by the native draw, so the shader carries the opaque color.
internal sealed class SolidColorNativeShader(Color color) : INativeShader
{
    public SKShader? CreateNativeShader() => SKShader.CreateColor(new SKColor(color.WithAlpha(255).ToARGB32()));
}
