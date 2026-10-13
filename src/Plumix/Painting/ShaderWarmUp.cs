using Avalonia;
using Plumix.Developer;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/painting/shader_warm_up.dart

namespace Plumix.Painting;

/// <summary>Draws an offscreen picture during binding initialization to warm the drawing backend.</summary>
public abstract class ShaderWarmUp
{
    public virtual Size Size => new(100.0, 100.0);

    protected abstract Task WarmUpOnCanvas(Canvas canvas);

    public async Task Execute()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        await WarmUpOnCanvas(canvas);
        Picture picture = recorder.EndRecording();
        if (Constants.KDebugMode && !PaintingDebug.DebugCaptureShaderWarmUpPicture(picture))
        {
            throw new AssertionError("debugCaptureShaderWarmUpPicture returned false.");
        }

        TimelineTask? debugShaderWarmUpTask = null;
        if (!Constants.KReleaseMode)
        {
            debugShaderWarmUpTask = new TimelineTask();
            debugShaderWarmUpTask.Start("Warm-up shader");
        }

        try
        {
            var image = await picture.ToImage((int)Math.Ceiling(Size.Width), (int)Math.Ceiling(Size.Height));
            if (Constants.KDebugMode && !PaintingDebug.DebugCaptureShaderWarmUpImage(image))
            {
                throw new AssertionError("debugCaptureShaderWarmUpImage returned false.");
            }

            image.Dispose();
        }
        finally
        {
            if (!Constants.KReleaseMode)
            {
                debugShaderWarmUpTask!.Finish();
            }
        }

        picture.Dispose();
    }
}
