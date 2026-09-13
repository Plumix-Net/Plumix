using Avalonia;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

/// <summary>Paints a <see cref="Rendering.Decoration"/> either before or after its child paints.</summary>
public class RenderDecoratedBox : RenderProxyBox
{
    private BoxPainter? _painter;
    private Decoration _decoration;
    private DecorationPosition _position;
    private ImageConfiguration _configuration;

    /// <summary>Creates a decorated box.</summary>
    /// <remarks>
    /// Dart's <c>configuration</c> defaults to <c>ImageConfiguration.empty</c>; C# cannot use a non-constant
    /// default, so <see langword="null"/> stands for <see cref="ImageConfiguration.Empty"/>.
    /// </remarks>
    public RenderDecoratedBox(
        Decoration decoration,
        DecorationPosition position = DecorationPosition.Background,
        ImageConfiguration? configuration = null,
        RenderBox? child = null) : base(child)
    {
        _decoration = decoration;
        _position = position;
        _configuration = configuration ?? ImageConfiguration.Empty;
    }

    /// <summary>What decoration to paint.</summary>
    public Decoration Decoration
    {
        get => _decoration;
        set
        {
            if (value == _decoration)
            {
                return;
            }

            _painter?.Dispose();
            _painter = null;
            _decoration = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>Whether to paint the box decoration behind or in front of the child.</summary>
    public DecorationPosition Position
    {
        get => _position;
        set
        {
            if (value == _position)
            {
                return;
            }

            _position = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>The settings to pass to the decoration when painting.</summary>
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
            MarkNeedsPaint();
        }
    }

    /// <inheritdoc />
    protected override void OnDetach()
    {
        _painter?.Dispose();
        _painter = null;
        base.OnDetach();

        // Since we're disposing of our painter, we won't receive change
        // notifications. We mark ourselves as needing paint so that we will
        // resubscribe to change notifications. If we didn't do this, then, for
        // example, animated GIFs would stop animating when a DecoratedBox gets
        // moved around the tree due to GlobalKey reparenting.
        MarkNeedsPaint();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _painter?.Dispose();
        base.Dispose();
    }

    /// <inheritdoc />
    protected override bool HitTestSelf(Point position)
    {
        return _decoration.HitTest(Size, position, textDirection: Configuration.TextDirection);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        _painter ??= _decoration.CreateBoxPainter(MarkNeedsPaint);
        ImageConfiguration filledConfiguration = Configuration.CopyWith(size: Size);
        if (Position == DecorationPosition.Background)
        {
            int? debugSaveCount = null;
            if (Constants.KDebugMode)
            {
                debugSaveCount = context.Canvas.GetSaveCount();
            }

            _painter.Paint(context, offset, filledConfiguration);
            if (Constants.KDebugMode)
            {
                if (debugSaveCount != context.Canvas.GetSaveCount())
                {
                    throw new FlutterError([
                        new ErrorSummary(
                            $"{_decoration.GetType().Name} painter had mismatching save and restore calls."),
                        new ErrorDescription(
                            $"Before painting the decoration, the canvas save count was {debugSaveCount}. "
                            + $"After painting it, the canvas save count was {context.Canvas.GetSaveCount()}. "
                            + "Every call to save() or saveLayer() must be matched by a call to restore()."),
                        new DiagnosticsProperty<Decoration>(
                            "The decoration was",
                            Decoration,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                        new DiagnosticsProperty<BoxPainter>(
                            "The painter was",
                            _painter,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                    ]);
                }
            }

            if (Decoration.IsComplex)
            {
                context.SetIsComplexHint();
            }
        }

        base.Paint(context, offset);
        if (Position == DecorationPosition.Foreground)
        {
            _painter.Paint(context, offset, filledConfiguration);
            if (Decoration.IsComplex)
            {
                context.SetIsComplexHint();
            }
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(_decoration.ToDiagnosticsNode(name: "decoration"));
        properties.Add(new DiagnosticsProperty<ImageConfiguration>("configuration", Configuration));
    }
}
