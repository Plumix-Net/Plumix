using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/texture.dart

namespace Plumix.Rendering;

/// <summary>A rectangle upon which a backend texture is mapped.</summary>
/// <remarks>
/// Flutter's <c>TextureBox</c>. Backend textures are images that can be applied (mapped) to an area of
/// the Flutter view. They are created, managed, and updated using a platform-specific texture registry
/// (<see cref="TextureRegistry"/>). The box paints a <see cref="TextureLayer"/> at its bounds, always
/// fills its constraints, and is always composited.
/// </remarks>
public class TextureBox : RenderBox
{
    private int _textureId;
    private bool _freeze;
    private FilterQuality _filterQuality;

    /// <summary>Creates a box backed by the texture identified by <paramref name="textureId"/>.</summary>
    public TextureBox(int textureId, bool freeze = false, FilterQuality filterQuality = FilterQuality.Low)
    {
        _textureId = textureId;
        _freeze = freeze;
        _filterQuality = filterQuality;
    }

    /// <summary>The identity of the backend texture.</summary>
    public int TextureId
    {
        get => _textureId;
        set
        {
            if (value != _textureId)
            {
                _textureId = value;
                MarkNeedsPaint();
            }
        }
    }

    /// <summary>When true the texture will not be updated with new frames.</summary>
    public bool Freeze
    {
        get => _freeze;
        set
        {
            if (value != _freeze)
            {
                _freeze = value;
                MarkNeedsPaint();
            }
        }
    }

    /// <summary>The quality of sampling the texture and rendering it on screen.</summary>
    public FilterQuality FilterQuality
    {
        get => _filterQuality;
        set
        {
            if (value != _filterQuality)
            {
                _filterQuality = value;
                MarkNeedsPaint();
            }
        }
    }

    protected override bool SizedByParent => true;

    public override bool AlwaysNeedsCompositing => true;

    public override bool IsRepaintBoundary => true;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Biggest;
    }

    protected override bool HitTestSelf(Point position) => true;

    public override void Paint(PaintingContext context, Point offset)
    {
        context.AddLayer(new TextureLayer(
            rect: new Rect(offset.X, offset.Y, Size.Width, Size.Height),
            textureId: _textureId,
            freeze: Freeze,
            filterQuality: _filterQuality));
    }
}
