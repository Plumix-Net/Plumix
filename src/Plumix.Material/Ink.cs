using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_decoration.dart

/// <summary>
/// A convenience widget for drawing images and other decorations on <see cref="Material"/> widgets,
/// so that <see cref="InkWell"/> and <see cref="InkResponse"/> splashes will render over them.
/// </summary>
public class Ink : StatefulWidget
{
    /// <summary>Paints a decoration (which can be a simple color) on a <see cref="Material"/>.</summary>
    public Ink(
        EdgeInsetsGeometry? padding = null,
        Color? color = null,
        Decoration? decoration = null,
        double? width = null,
        double? height = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        DebugAssertions.Assert(padding is null || padding.Value.IsNonNegative);
        DebugAssertions.Assert(
            color is null || decoration is null,
            "Cannot provide both a color and a decoration\n"
            + "The color argument is just a shorthand for \"decoration: BoxDecoration(color: color)\".");
        Padding = padding;
        Decoration = decoration ?? (color is not null ? new BoxDecoration(Color: color) : null);
        Width = width;
        Height = height;
        Child = child;
    }

    private Ink(
        EdgeInsetsGeometry? padding,
        Decoration decoration,
        double? width,
        double? height,
        Widget? child,
        Key? key) : base(key)
    {
        DebugAssertions.Assert(padding is null || padding.Value.IsNonNegative);
        Padding = padding;
        Decoration = decoration;
        Width = width;
        Height = height;
        Child = child;
    }

    /// <summary>Creates a widget that shows an image (obtained from an <see cref="ImageProvider"/>) on a
    /// <see cref="Material"/>.</summary>
    /// <remarks>Dart's <c>Ink.image</c> named constructor.</remarks>
    public static Ink Image(
        ImageProvider image,
        EdgeInsetsGeometry? padding = null,
        ImageErrorListener? onImageError = null,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        Alignment? alignment = null,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        double? width = null,
        double? height = null,
        Widget? child = null,
        Key? key = null)
    {
        return new Ink(
            padding,
            new BoxDecoration(
                Image: new DecorationImage(
                    image: image,
                    onError: onImageError,
                    colorFilter: colorFilter,
                    fit: fit,
                    alignment: alignment ?? Alignment.Center,
                    centerSlice: centerSlice,
                    repeat: repeat,
                    matchTextDirection: matchTextDirection)),
            width,
            height,
            child,
            key);
    }

    /// <summary>The <see cref="Widget.Child"/> contained by the container.</summary>
    public Widget? Child { get; }

    /// <summary>Empty space to inscribe inside the <see cref="Decoration"/>.</summary>
    public EdgeInsetsGeometry? Padding { get; }

    /// <summary>The decoration to paint on the nearest ancestor <see cref="Material"/> widget.</summary>
    public Decoration? Decoration { get; }

    /// <summary>A width to apply to the <see cref="Decoration"/> and the <see cref="Child"/>.</summary>
    public double? Width { get; }

    /// <summary>A height to apply to the <see cref="Decoration"/> and the <see cref="Child"/>.</summary>
    public double? Height { get; }

    internal EdgeInsetsGeometry PaddingIncludingDecoration
    {
        get
        {
            EdgeInsetsGeometry? decorationPadding = Decoration?.Padding;
            return (Padding, decorationPadding) switch
            {
                (null, null) => EdgeInsets.Zero,
                (null, { } padding) => padding,
                ({ } padding, null) => padding,
                _ => Padding!.Value.Add(Decoration!.Padding),
            };
        }
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>("padding", Padding, defaultValue: null));
        properties.Add(new DiagnosticsProperty<Decoration>("bg", Decoration, defaultValue: null));
    }

    public override State CreateState() => new InkState();
}

// Dart's `_InkState`.
internal sealed class InkState : State<Ink>
{
    private readonly GlobalKey _boxKey = new LabeledGlobalKey<State>("ink box");
    private InkDecoration? _ink;

    private void HandleRemoved()
    {
        _ink = null;
    }

    public override void Deactivate()
    {
        _ink?.Dispose();
        DebugAssertions.Assert(_ink is null);
        base.Deactivate();
    }

    private Widget BuildInner(BuildContext context)
    {
        // By creating the InkDecoration from within a Builder widget, we can
        // use the RenderBox of the Padding widget.
        if (_ink is null)
        {
            _ink = new InkDecoration(
                decoration: Widget.Decoration,
                isVisible: Visibility.Of(context),
                configuration: ImageConfigurationUtils.CreateLocalImageConfiguration(context),
                controller: Material.Of(context),
                referenceBox: (RenderBox)_boxKey.CurrentContext!.FindRenderObject()!,
                onRemoved: HandleRemoved);
        }
        else
        {
            _ink.Decoration = Widget.Decoration;
            _ink.IsVisible = Visibility.Of(context);
            _ink.Configuration = ImageConfigurationUtils.CreateLocalImageConfiguration(context);
        }

        return Widget.Child ?? new ConstrainedBox(BoxConstraints.Expand());
    }

    public override Widget Build(BuildContext context)
    {
        DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterial(context));
        Widget result = new Padding(
            key: _boxKey,
            insets: Widget.PaddingIncludingDecoration,
            child: new Builder(BuildInner));
        if (Widget.Width is not null || Widget.Height is not null)
        {
            result = new SizedBox(width: Widget.Width, height: Widget.Height, child: result);
        }

        return result;
    }
}

/// <summary>
/// A decoration on a part of a <see cref="Material"/>: the <see cref="InkFeature"/> behind
/// <see cref="Ink"/>, painting its <see cref="Decoration"/> on the material below the children.
/// </summary>
public class InkDecoration : InkFeature
{
    private BoxPainter? _painter;
    private Decoration? _decoration;
    private bool _isVisible = true;
    private ImageConfiguration _configuration;

    /// <summary>Draws a decoration on a <see cref="Material"/>.</summary>
    public InkDecoration(
        Decoration? decoration,
        ImageConfiguration configuration,
        MaterialInkController controller,
        RenderBox referenceBox,
        bool isVisible = true,
        Action? onRemoved = null)
        : base(controller, referenceBox, onRemoved)
    {
        _configuration = configuration;
        Decoration = decoration;
        IsVisible = isVisible;
        controller.AddInkFeature(this);
    }

    /// <summary>What to paint on the <see cref="Material"/>.</summary>
    public Decoration? Decoration
    {
        get => _decoration;
        set
        {
            if (Equals(value, _decoration))
            {
                return;
            }

            _decoration = value;
            _painter?.Dispose();
            _painter = _decoration?.CreateBoxPainter(HandleChanged);
            Controller.MarkNeedsPaint();
        }
    }

    /// <summary>Whether the decoration should be painted.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (value == _isVisible)
            {
                return;
            }

            _isVisible = value;
            Controller.MarkNeedsPaint();
        }
    }

    /// <summary>The configuration to pass to the <see cref="BoxPainter"/> obtained from the
    /// <see cref="Decoration"/>, when painting.</summary>
    public ImageConfiguration Configuration
    {
        get => _configuration;
        set
        {
            if (value == _configuration)
            {
                return;
            }

            _configuration = value;
            Controller.MarkNeedsPaint();
        }
    }

    private void HandleChanged()
    {
        Controller.MarkNeedsPaint();
    }

    public override void Dispose()
    {
        _painter?.Dispose();
        base.Dispose();
    }

    protected override void PaintFeature(Canvas canvas, Matrix4 transform)
    {
        if (_painter is null || !IsVisible)
        {
            return;
        }

        Point? originOffset = MatrixUtils.GetAsTranslation(transform);
        ImageConfiguration sizedConfiguration = Configuration.CopyWith(size: ReferenceBox.Size);
        // C#-only: Plumix's BoxPainter paints into a PaintingContext (see CanvasPaintingContext).
        var context = new CanvasPaintingContext(canvas);
        if (originOffset is null)
        {
            canvas.Save();
            canvas.Transform(transform);
            _painter.Paint(context, default, sizedConfiguration);
            canvas.Restore();
        }
        else
        {
            _painter.Paint(context, originOffset.Value, sizedConfiguration);
        }
    }
}
