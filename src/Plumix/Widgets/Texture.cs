using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/texture.dart

namespace Plumix.Widgets;

/// <summary>A rectangle upon which a backend texture is mapped.</summary>
/// <remarks>
/// Flutter's <c>Texture</c>. Backend textures are images that can be applied (mapped) to an area of the
/// Flutter view. They are created, managed, and updated using a platform-specific texture registry
/// (<see cref="UI.TextureRegistry"/>). A texture widget fills all available space; when it is frozen, the
/// texture keeps showing the frame it last painted.
/// </remarks>
public class Texture : LeafRenderObjectWidget
{
    /// <summary>Creates a widget backed by the texture identified by <paramref name="textureId"/>.</summary>
    public Texture(
        int textureId,
        bool freeze = false,
        FilterQuality filterQuality = FilterQuality.Low,
        Plumix.Foundation.Key? key = null) : base(key)
    {
        TextureId = textureId;
        Freeze = freeze;
        FilterQuality = filterQuality;
    }

    /// <summary>The identity of the backend texture.</summary>
    public int TextureId { get; }

    /// <summary>When true the texture will not be updated with new frames.</summary>
    public bool Freeze { get; }

    /// <summary>The quality of sampling the texture and rendering it on screen.</summary>
    public FilterQuality FilterQuality { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new TextureBox(textureId: TextureId, freeze: Freeze, filterQuality: FilterQuality);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var textureBox = (TextureBox)renderObject;
        textureBox.TextureId = TextureId;
        textureBox.Freeze = Freeze;
        textureBox.FilterQuality = FilterQuality;
    }
}
