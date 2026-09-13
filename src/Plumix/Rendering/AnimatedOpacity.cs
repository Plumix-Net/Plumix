using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart
// (RenderAnimatedOpacityMixin, RenderAnimatedOpacity)

/// <summary>
/// The state and invalidation rules of Flutter's <c>RenderAnimatedOpacityMixin</c>.
/// </summary>
/// <remarks>
/// C# has no mixins, so the mixin body lives here and both
/// <see cref="RenderAnimatedOpacity"/> (box) and <c>RenderSliverAnimatedOpacity</c> (sliver) hold
/// one of these and forward to it, exactly as Dart's two render objects share the mixin.
/// </remarks>
internal sealed class RenderAnimatedOpacityMixin
{
    private readonly RenderObject _owner;
    private readonly Func<bool> _hasChild;
    private int? _alpha;
    private bool? _currentlyIsRepaintBoundary;
    private Animation<double>? _opacity;
    private bool? _alwaysIncludeSemantics;

    internal RenderAnimatedOpacityMixin(RenderObject owner, Func<bool> hasChild)
    {
        _owner = owner;
        _hasChild = hasChild;
    }

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin._alpha</c>.</remarks>
    internal int? Alpha => _alpha;

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin.isRepaintBoundary</c>.</remarks>
    internal bool IsRepaintBoundary => _hasChild() && _currentlyIsRepaintBoundary!.Value;

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin.updateCompositedLayer</c>; the cast fails for a
    /// non-<see cref="OpacityLayer"/> like Dart's covariant parameter.</remarks>
    internal OffsetLayer UpdateCompositedLayer(OffsetLayer? oldLayer)
    {
        OpacityLayer updatedLayer = (OpacityLayer?)oldLayer ?? new OpacityLayer();
        updatedLayer.Alpha = _alpha;
        return updatedLayer;
    }

    internal Animation<double> Opacity
    {
        get => _opacity!;
        set
        {
            if (ReferenceEquals(_opacity, value))
            {
                return;
            }

            if (_owner.Attached && _opacity is not null)
            {
                Opacity.RemoveListener(UpdateOpacity);
            }

            _opacity = value;
            if (_owner.Attached)
            {
                Opacity.AddListener(UpdateOpacity);
            }

            UpdateOpacity();
        }
    }

    internal bool AlwaysIncludeSemantics
    {
        get => _alwaysIncludeSemantics!.Value;
        set
        {
            if (value == _alwaysIncludeSemantics)
            {
                return;
            }

            _alwaysIncludeSemantics = value;
            _owner.MarkNeedsSemanticsUpdate();
        }
    }

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin.attach</c>, run after the child attached.</remarks>
    internal void OnAttach()
    {
        Opacity.AddListener(UpdateOpacity);
        UpdateOpacity(); // in case it changed while we weren't listening
    }

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin.detach</c>, run before the child detaches.</remarks>
    internal void OnDetach() => Opacity.RemoveListener(UpdateOpacity);

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin._updateOpacity</c>.</remarks>
    private void UpdateOpacity()
    {
        int? oldAlpha = _alpha;
        _alpha = ColorUtilities.GetAlphaFromOpacity(Opacity.Value);
        if (oldAlpha != _alpha)
        {
            bool? wasRepaintBoundary = _currentlyIsRepaintBoundary;
            _currentlyIsRepaintBoundary = _alpha!.Value > 0;
            if (_hasChild() && wasRepaintBoundary != _currentlyIsRepaintBoundary)
            {
                _owner.MarkNeedsCompositingBitsUpdate();
            }

            _owner.MarkNeedsCompositedLayerUpdate();
            if (oldAlpha == 0 || _alpha == 0)
            {
                _owner.MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin.paintsChild</c>: reads the animation, not the alpha.</remarks>
    internal bool PaintsChild(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, _owner));
        return Opacity.Value > 0;
    }

    /// <remarks>The condition of Flutter's <c>RenderAnimatedOpacityMixin.visitChildrenForSemantics</c>.</remarks>
    internal bool IncludesChildInSemantics() => _hasChild() && (_alpha != 0 || AlwaysIncludeSemantics);

    /// <remarks>Flutter's <c>RenderAnimatedOpacityMixin.debugFillProperties</c>.</remarks>
    internal void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        properties.Add(new DiagnosticsProperty<Animation<double>>("opacity", Opacity));
        properties.Add(new FlagProperty(
            "alwaysIncludeSemantics",
            value: AlwaysIncludeSemantics,
            ifTrue: "alwaysIncludeSemantics"));
    }
}

/// <summary>Makes its child partially transparent, driven by an animation.</summary>
/// <remarks>Flutter's <c>RenderAnimatedOpacity</c>.</remarks>
public class RenderAnimatedOpacity : RenderProxyBox
{
    private readonly RenderAnimatedOpacityMixin _animatedOpacity;

    public RenderAnimatedOpacity(
        Animation<double> opacity,
        bool alwaysIncludeSemantics = false,
        RenderBox? child = null) : base(child)
    {
        _animatedOpacity = new RenderAnimatedOpacityMixin(this, () => Child != null);
        Opacity = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    /// <inheritdoc />
    public override bool IsRepaintBoundary => _animatedOpacity.IsRepaintBoundary;

    /// <inheritdoc />
    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer) =>
        _animatedOpacity.UpdateCompositedLayer(oldLayer);

    /// <inheritdoc />
    protected override void UpdateCompositedLayer(OffsetLayer layer) =>
        _animatedOpacity.UpdateCompositedLayer(layer);

    /// <summary>The animation driving this render object's opacity.</summary>
    public Animation<double> Opacity
    {
        get => _animatedOpacity.Opacity;
        set => _animatedOpacity.Opacity = value;
    }

    /// <summary>Whether to preserve the semantics of the child when it is fully transparent.</summary>
    public bool AlwaysIncludeSemantics
    {
        get => _animatedOpacity.AlwaysIncludeSemantics;
        set => _animatedOpacity.AlwaysIncludeSemantics = value;
    }

    /// <inheritdoc />
    protected override void OnAttach()
    {
        base.OnAttach();
        _animatedOpacity.OnAttach();
    }

    /// <inheritdoc />
    protected override void OnDetach()
    {
        _animatedOpacity.OnDetach();
        base.OnDetach();
    }

    /// <inheritdoc />
    public override bool PaintsChild(RenderObject child) => _animatedOpacity.PaintsChild(child);

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (_animatedOpacity.Alpha == 0)
        {
            return;
        }

        base.Paint(context, offset);
    }

    /// <inheritdoc />
    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_animatedOpacity.IncludesChildInSemantics())
        {
            visitor(Child!);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        _animatedOpacity.DebugFillProperties(properties);
    }
}
