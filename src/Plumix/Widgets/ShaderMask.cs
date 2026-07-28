using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (ShaderMask)

public sealed class ShaderMask : SingleChildRenderObjectWidget
{
    public ShaderMask(
        ShaderCallback shaderCallback,
        Widget? child = null,
        BlendMode blendMode = BlendMode.Modulate,
        Key? key = null) : base(child, key)
    {
        ShaderCallback = shaderCallback ?? throw new ArgumentNullException(nameof(shaderCallback));
        BlendMode = blendMode;
    }

    public ShaderCallback ShaderCallback { get; }

    public BlendMode BlendMode { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderShaderMask(
            shaderCallback: ShaderCallback,
            blendMode: BlendMode);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var shaderMask = (RenderShaderMask)renderObject;
        shaderMask.ShaderCallback = ShaderCallback;
        shaderMask.BlendMode = BlendMode;
    }
}
