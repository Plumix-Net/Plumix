using Avalonia.Media;
using SkiaSharp;

namespace Plumix.UI;

// C#-only transport for engine shaders through the published Avalonia IBrush slots.
internal interface INativeShader
{
    SKShader? CreateNativeShader();
    IBrush Snapshot();
}

// A paint's solid color as a native shader, so shapes Avalonia's geometry would approximate (conic
// corners, superellipses) reach Skia as the exact path Dart's canvas hands it. The paint's alpha is
// applied by the native draw, so the shader carries the opaque color.
internal sealed class SolidColorNativeShader(Color color) : INativeShader
{
    public SKShader? CreateNativeShader() => SKShader.CreateColor(new SKColor(color.WithAlpha(255).ToARGB32()));

    public IBrush Snapshot() => new SolidColorBrush(color);
}
