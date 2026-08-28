using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (ColoredBox)

namespace Plumix.Widgets;

public sealed class ColoredBox : SingleChildRenderObjectWidget
{
    public ColoredBox(
        Color color,
        bool isAntiAlias = true,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Color = color;
        IsAntiAlias = isAntiAlias;
    }

    public Color Color { get; }

    public bool IsAntiAlias { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderColoredBox(
            Color,
            isAntiAlias: IsAntiAlias);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var coloredBox = (RenderColoredBox)renderObject;
        coloredBox.Color = Color;
        coloredBox.IsAntiAlias = IsAntiAlias;
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new ColorProperty("color", Color));
        properties.Add(new DiagnosticsProperty<bool>("isAntiAlias", IsAntiAlias, defaultValue: true));
    }
}
