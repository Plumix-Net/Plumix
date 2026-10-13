using Avalonia.Media.Imaging;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/painting/debug.dart

namespace Plumix.Painting;

public delegate bool ShaderWarmUpPictureCallback(Picture picture);
public delegate bool ShaderWarmUpImageCallback(Bitmap image);

/// <summary>Debug capture hooks used when warming the drawing backend.</summary>
public static class PaintingDebug
{
    public static ShaderWarmUpPictureCallback DebugCaptureShaderWarmUpPicture { get; set; } = _ => true;

    public static ShaderWarmUpImageCallback DebugCaptureShaderWarmUpImage { get; set; } = _ => true;
}
