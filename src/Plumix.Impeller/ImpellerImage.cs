using NImpeller;
using Plumix.UI;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. A decoded image of the Impeller prototype: premultiplied
// RGBA8888 pixels the host's decoder produced, uploaded to an Impeller texture the first time a picture draws
// it (textures need the render context, which exists only once a surface does). The pixels stay in managed
// memory for Image.toByteData, which Impeller cannot read back from a texture.

/// <summary>Decoded pixels and their lazily created Impeller texture.</summary>
internal sealed class ImpellerImage
{
    private ImpellerTexture? _texture;

    private ImpellerImage(byte[] rgba, int width, int height)
    {
        Rgba = rgba;
        Width = width;
        Height = height;
    }

    /// <summary>Premultiplied RGBA8888 rows, top to bottom.</summary>
    public byte[] Rgba { get; }

    public int Width { get; }

    public int Height { get; }

    /// <summary>A framework image over <paramref name="rgba"/>; its logical size is its pixel size.</summary>
    public static RasterImage Wrap(byte[] rgba, int width, int height) =>
        new(new ImpellerImage(rgba, width, height), width, height, new Size(width, height));

    /// <summary>The texture, uploaded on first use; null while no render context exists.</summary>
    public ImpellerTexture? Texture
    {
        get
        {
            if (_texture is null && ImpellerBackend.Context is not null)
            {
                // GLES uploads run on the context's thread (Impeller's reactor reacts only there).
                _texture = ImpellerBackend.OnContextThread(() =>
                {
                    using var contents = new ImpellerUnmanagedMemory(Rgba);
                    return ImpellerBackend.Context?.TextureCreateWithContentsNew(
                        new ImpellerTextureDescriptor
                        {
                            Pixel_format = ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
                            Size = new ImpellerISize(Width, Height),
                            Mip_count = 1,
                        },
                        contents);
                });
            }

            return _texture;
        }
    }
}
