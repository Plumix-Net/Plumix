using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/view.dart (approximate)

namespace Plumix;

/// <summary>
/// The constraints and pixel density the root of the render tree is laid out against.
/// </summary>
/// <remarks>Flutter's <c>ViewConfiguration</c>.</remarks>
public class ViewConfiguration : IEquatable<ViewConfiguration>
{
    /// <summary>Creates a view configuration.</summary>
    public ViewConfiguration(
        BoxConstraints? physicalConstraints = null,
        BoxConstraints? logicalConstraints = null,
        double devicePixelRatio = 1.0)
    {
        PhysicalConstraints = physicalConstraints
            ?? new BoxConstraints(MaxWidth: 0, MaxHeight: 0);
        LogicalConstraints = logicalConstraints
            ?? new BoxConstraints(MaxWidth: 0, MaxHeight: 0);
        DevicePixelRatio = devicePixelRatio;
    }

    /// <summary>Creates a view configuration for <paramref name="view"/>.</summary>
    /// <remarks>Flutter's <c>ViewConfiguration.fromView</c>.</remarks>
    public static ViewConfiguration FromView(FlutterView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        BoxConstraints physicalConstraints = BoxConstraints.Tight(view.PhysicalSize);
        double devicePixelRatio = view.DevicePixelRatio;
        return new ViewConfiguration(
            physicalConstraints: physicalConstraints,
            logicalConstraints: physicalConstraints / devicePixelRatio,
            devicePixelRatio: devicePixelRatio);
    }

    /// <summary>The constraints of the output surface in logical pixels.</summary>
    public BoxConstraints LogicalConstraints { get; }

    /// <summary>The constraints of the output surface in physical pixels.</summary>
    public BoxConstraints PhysicalConstraints { get; }

    /// <summary>The pixel density of the output surface.</summary>
    public double DevicePixelRatio { get; }

    /// <summary>Creates a transformation matrix that applies the <see cref="DevicePixelRatio"/>.</summary>
    public virtual Matrix4 ToMatrix()
    {
        return Matrix4.Diagonal3Values(DevicePixelRatio, DevicePixelRatio, 1.0);
    }

    /// <summary>
    /// Whether <see cref="ToMatrix"/> would return a different value for this configuration than it
    /// would for <paramref name="oldConfiguration"/>.
    /// </summary>
    public virtual bool ShouldUpdateMatrix(ViewConfiguration oldConfiguration)
    {
        ArgumentNullException.ThrowIfNull(oldConfiguration);
        if (oldConfiguration.GetType() != GetType())
        {
            // New configuration could have different logic, so we don't know whether it will need a
            // new transform. Return a conservative result.
            return true;
        }

        return oldConfiguration.DevicePixelRatio != DevicePixelRatio;
    }

    /// <summary>Transforms <paramref name="logicalSize"/> from logical pixels to physical pixels.</summary>
    public virtual Size ToPhysicalSize(Size logicalSize)
    {
        return PhysicalConstraints.Constrain(
            new Size(logicalSize.Width * DevicePixelRatio, logicalSize.Height * DevicePixelRatio));
    }

    /// <inheritdoc />
    public bool Equals(ViewConfiguration? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return other.LogicalConstraints == LogicalConstraints
               && other.PhysicalConstraints == PhysicalConstraints
               && other.DevicePixelRatio == DevicePixelRatio;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ViewConfiguration);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(LogicalConstraints, PhysicalConstraints, DevicePixelRatio);

    /// <inheritdoc />
    public override string ToString()
        => $"{LogicalConstraints} at {DoubleProperty.FormatDouble(DevicePixelRatio)}x";
}

public sealed class RenderView : RenderBox, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;
    private ViewConfiguration? _configuration;

    public override bool IsRepaintBoundary => true;

    /// <summary>
    /// The constraints and pixel density used for the root layout.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderView.configuration</c>. Plumix's hosts drive the frame themselves (see the
    /// <c>PipelineOwner.RequestLayout</c> row in <c>docs/ai/DIVERGENCES.md</c>), so the owner keeps
    /// this in step with the size it is asked to lay the view out under.
    /// </remarks>
    public ViewConfiguration Configuration
    {
        get => _configuration
               ?? throw new InvalidOperationException(
                   "Configuration is not available because RenderView has not been given one yet.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Equals(_configuration, value))
            {
                return;
            }

            _configuration = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Whether a <see cref="Configuration"/> has been set.</summary>
    public bool HasConfiguration => _configuration is not null;

    /// <summary>The platform view this render view renders into, when a host has supplied one.</summary>
    /// <remarks>Flutter's <c>RenderView.flutterView</c>, which is required rather than optional there.</remarks>
    public FlutterView? FlutterView { get; set; }

    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;

            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderBox?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    protected override void PerformLayout()
    {
        if (_child != null)
        {
            _child.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(_child.Size);
            ((BoxParentData)_child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = Constraints.Constrain(new Size());
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Mirrors <see cref="PerformLayout"/> so that
    /// <see cref="Rendering.RenderingDebug.CheckIntrinsicSizes"/> does not report the view itself.
    /// </remarks>
    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return _child is null
            ? constraints.Constrain(new Size())
            : constraints.Constrain(_child.GetDryLayout(constraints));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            ctx.PaintChild(_child, offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null)
        {
            return false;
        }

        return _child.HitTest(result, position);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        configuration.IsSemanticBoundary = true;
    }

    internal void ScheduleInitialPaint(OffsetLayer rootLayer)
    {
        if (!rootLayer.Attached)
        {
            rootLayer.Attach(this);
        }

        _layer = rootLayer;
    }

    internal void ReplaceRootLayer(OffsetLayer rootLayer)
    {
        if (ReferenceEquals(_layer, rootLayer))
        {
            return;
        }

        if (_layer is Layer oldRootLayer && oldRootLayer.Attached)
        {
            oldRootLayer.Detach();
        }

        if (!rootLayer.Attached)
        {
            rootLayer.Attach(this);
        }

        _layer = rootLayer;
        MarkNeedsPaint();
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderView.debugFillProperties</c>. The call to the base implementation is
    /// omitted there too, because the root superclasses carry nothing interesting for this class.
    /// </remarks>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        if (Constants.KDebugMode)
        {
            properties.Add(DiagnosticsNode.Message(
                $"debug mode enabled - {System.Runtime.InteropServices.RuntimeInformation.OSDescription}"));
        }

        properties.Add(new DiagnosticsProperty<Size?>(
            "view size",
            FlutterView?.PhysicalSize,
            tooltip: "in physical pixels"));
        properties.Add(new DoubleProperty(
            "device pixel ratio",
            FlutterView?.DevicePixelRatio,
            tooltip: "physical pixels per logical pixel"));
        properties.Add(new DiagnosticsProperty<ViewConfiguration>(
            "configuration",
            _configuration,
            tooltip: "in logical pixels"));
        if (Owner?.SemanticsOwner is not null)
        {
            properties.Add(DiagnosticsNode.Message("semantics enabled"));
        }
    }
}
