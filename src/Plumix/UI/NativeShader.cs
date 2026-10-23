using Avalonia.Media;
using SkiaSharp;

namespace Plumix.UI;

// C#-only transport for engine shaders through the published Avalonia IBrush slots.
internal interface INativeShader
{
    SKShader? CreateNativeShader();
    IBrush Snapshot();
}
