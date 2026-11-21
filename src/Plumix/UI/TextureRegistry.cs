namespace Plumix.UI;

// C#-only infrastructure: the engine side of `SceneBuilder.addTexture` — a port of the engine's
// `common/graphics/texture.{h,cc}` (`flutter::Texture`, `flutter::TextureRegistry`). A host or plugin
// registers a texture under an id, a `Texture` widget (`TextureBox`/`TextureLayer`) shows it, and the
// host's scene rasterizer (Plumix.Avalonia's FlowLayerPainter.PaintTextureFrame) paints whatever frame
// the texture hands it. No Dart source maps to this file.

/// <summary>A backend texture a <see cref="TextureRegistry"/> serves to texture layers.</summary>
/// <remarks>The engine's <c>flutter::Texture</c>.</remarks>
public abstract class EngineTexture
{
    private protected EngineTexture(long id)
    {
        Id = id;
    }

    /// <summary>The id texture layers refer to this texture by: <c>Texture::Id</c>.</summary>
    public long Id { get; }

    /// <summary>
    /// The frame a paint shows, or null for none; a <paramref name="freeze"/>d texture keeps the frame it
    /// last handed out.
    /// </summary>
    /// <remarks>The frame-selection half of the engine's <c>Texture::Paint</c>; the host draws the frame.</remarks>
    internal abstract RasterImage? FrameToPaint(bool freeze);

    /// <summary>Called when the producer has a new frame: <c>Texture::MarkNewFrameAvailable</c>.</summary>
    public abstract void MarkNewFrameAvailable();

    /// <summary>Called when the texture leaves its registry: <c>Texture::OnTextureUnregistered</c>.</summary>
    public abstract void OnTextureUnregistered();
}

/// <summary>An <see cref="EngineTexture"/> whose frames are images pushed by the producer.</summary>
/// <remarks>
/// Plumix-only: the engine's platform textures (<c>EmbedderExternalTextureGL</c>, the Android/iOS
/// external textures) wrap a GPU texture; this one wraps the latest <see cref="RasterImage"/> a producer set.
/// </remarks>
public sealed class ImageTexture : EngineTexture
{
    private RasterImage? _pending;
    private RasterImage? _current;

    public ImageTexture(long id) : base(id)
    {
    }

    /// <summary>Sets the image the next unfrozen paint shows, then marks a new frame available.</summary>
    public void SetImage(RasterImage? image)
    {
        _pending = image;
        TextureRegistry.Instance.MarkTextureFrameAvailable(Id);
    }

    /// <inheritdoc />
    internal override RasterImage? FrameToPaint(bool freeze)
    {
        if (!freeze)
        {
            _current = _pending;
        }

        return _current;
    }

    /// <inheritdoc />
    public override void MarkNewFrameAvailable()
    {
    }

    /// <inheritdoc />
    public override void OnTextureUnregistered()
    {
        _pending = null;
        _current = null;
    }
}

/// <summary>The textures the rasterizer can paint, by id.</summary>
/// <remarks>
/// The engine's <c>flutter::TextureRegistry</c>. Plumix has one engine per process, so there is one
/// registry; <see cref="TextureFrameAvailable"/> is the engine's <c>MarkTextureFrameAvailable</c> hook,
/// which a host turns into a new frame.
/// </remarks>
public sealed class TextureRegistry
{
    private readonly Dictionary<long, EngineTexture> _mapping = [];

    private TextureRegistry()
    {
    }

    /// <summary>The engine's registry.</summary>
    public static TextureRegistry Instance { get; } = new();

    /// <summary>Raised with a texture id when its producer has a new frame.</summary>
    public event Action<long>? TextureFrameAvailable;

    /// <summary>Registers <paramref name="texture"/> under its id: <c>RegisterTexture</c>.</summary>
    public void RegisterTexture(EngineTexture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        lock (_mapping)
        {
            _mapping[texture.Id] = texture;
        }
    }

    /// <summary>Removes the texture registered under <paramref name="id"/>: <c>UnregisterTexture</c>.</summary>
    public void UnregisterTexture(long id)
    {
        EngineTexture? texture;
        lock (_mapping)
        {
            if (!_mapping.Remove(id, out texture))
            {
                return;
            }
        }

        texture.OnTextureUnregistered();
    }

    /// <summary>The texture registered under <paramref name="id"/>, or null: <c>GetTexture</c>.</summary>
    public EngineTexture? GetTexture(long id)
    {
        lock (_mapping)
        {
            return _mapping.GetValueOrDefault(id);
        }
    }

    /// <summary>
    /// Tells the engine the texture under <paramref name="id"/> has a new frame: the engine's
    /// <c>Shell::OnPlatformViewMarkTextureFrameAvailable</c>.
    /// </summary>
    public void MarkTextureFrameAvailable(long id)
    {
        GetTexture(id)?.MarkNewFrameAvailable();
        TextureFrameAvailable?.Invoke(id);
    }
}
