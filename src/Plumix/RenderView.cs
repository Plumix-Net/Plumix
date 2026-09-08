using System.Diagnostics;
using Avalonia;
using Plumix.Rendering;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/view.dart (approximate)
// RenderView is a RenderBox and the host composites the owner's root layer; see docs/ai/DIVERGENCES.md.

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
        BoxConstraints physicalConstraints = view.PhysicalConstraints;
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

/// <summary>A callback <see cref="RenderView.DebugAddPaintCallback"/> registers; runs after the view painted.</summary>
/// <remarks>Flutter's <c>DebugPaintCallback</c>.</remarks>
public delegate void DebugPaintCallback(PaintingContext context, Point offset, RenderView renderView);

/// <summary>
/// The root of the render tree: the object that bridges the render tree and a platform view.
/// </summary>
/// <remarks>
/// <para>
/// Flutter's <c>RenderView</c>. It is constructed for a <see cref="FlutterView"/>, laid out against
/// its <see cref="Configuration"/> once <see cref="PrepareInitialFrame"/> bootstrapped layout and
/// paint, and is always a repaint boundary.
/// </para>
/// <para>
/// Plumix's view is a <see cref="RenderBox"/> rather than a bare <c>RenderObject</c>, so the box
/// hit-test protocol reaches it directly; its root layer is a plain <see cref="OffsetLayer"/>
/// because the render tree is kept in logical pixels and the host applies the device pixel ratio
/// when it composites the owner's layer — see <c>docs/ai/DIVERGENCES.md</c>.
/// </para>
/// </remarks>
public class RenderView : RenderBox, IRenderObjectSingleChildContainer
{
    private static readonly List<DebugPaintCallback> DebugPaintCallbacks = [];

    private RenderBox? _child;
    private ViewConfiguration? _configuration;
    private Matrix4? _rootTransform;

    /// <summary>Creates the root of the render tree for <paramref name="view"/>.</summary>
    /// <remarks>Flutter's <c>RenderView</c> constructor.</remarks>
    public RenderView(FlutterView view, RenderBox? child = null, ViewConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        FlutterView = view;
        if (configuration is not null)
        {
            Configuration = configuration;
        }

        Child = child;
    }

    public override bool IsRepaintBoundary => true;

    /// <summary>
    /// The constraints and pixel density used for the root layout.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderView.configuration</c>. Until <see cref="PrepareInitialFrame"/> has run the
    /// value is only stored; afterwards a change relays the view out and, when
    /// <see cref="ViewConfiguration.ShouldUpdateMatrix"/> says so, replaces the root layer. Plumix's
    /// hosts drive the frame themselves (see the <c>PipelineOwner.RequestLayout</c> row in
    /// <c>docs/ai/DIVERGENCES.md</c>), so the owner keeps this in step with the size it is asked to
    /// lay the view out under.
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

            ViewConfiguration? oldConfiguration = _configuration;
            _configuration = value;
            if (_rootTransform is null)
            {
                // [prepareInitialFrame] has not been called yet, nothing to do for now.
                return;
            }

            if (oldConfiguration is null || value.ShouldUpdateMatrix(oldConfiguration))
            {
                ReplaceRootLayer(UpdateMatricesAndCreateNewRootLayer());
            }

            Debug.Assert(_rootTransform is not null);
            MarkNeedsLayout();
        }
    }

    /// <summary>Whether a <see cref="Configuration"/> has been set.</summary>
    /// <remarks>Flutter's <c>RenderView.hasConfiguration</c>.</remarks>
    public bool HasConfiguration => _configuration is not null;

    /// <summary>The platform view this render view renders into.</summary>
    /// <remarks>Flutter's <c>RenderView.flutterView</c>.</remarks>
    public FlutterView FlutterView { get; }

    /// <summary>
    /// Whether Flutter should automatically compute the desired system UI overlay style from the
    /// painted <see cref="SystemUiOverlayStyle"/> annotations after each frame.
    /// </summary>
    /// <remarks>Flutter's <c>RenderView.automaticSystemUiAdjustment</c>.</remarks>
    public bool AutomaticSystemUiAdjustment { get; set; } = true;

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

    /// <summary>
    /// Bootstraps the render pipeline by preparing the first frame: schedules the initial layout
    /// and creates the root layer, which is then scheduled for its initial paint.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderView.prepareInitialFrame</c>. Must be called once, after the view is
    /// attached to a <see cref="PipelineOwner"/> and given a <see cref="Configuration"/>.
    /// </remarks>
    public virtual void PrepareInitialFrame()
    {
        if (Owner is null)
        {
            throw new AssertionError("attach the RenderView to a PipelineOwner before calling prepareInitialFrame");
        }

        if (_rootTransform is not null)
        {
            throw new AssertionError("prepareInitialFrame must only be called once");
        }

        if (!HasConfiguration)
        {
            throw new AssertionError("set a configuration before calling prepareInitialFrame");
        }

        ScheduleInitialLayout();
        ScheduleInitialPaint(UpdateMatricesAndCreateNewRootLayer());
        Debug.Assert(_rootTransform is not null);
    }

    /// <summary>Flutter's <c>RenderView._updateMatricesAndCreateNewRootLayer</c>.</summary>
    private OffsetLayer UpdateMatricesAndCreateNewRootLayer()
    {
        Debug.Assert(HasConfiguration);
        _rootTransform = Configuration.ToMatrix();
        var rootLayer = new OffsetLayer();
        rootLayer.Attach(this);
        return rootLayer;
    }

    /// <summary>Flutter's <c>RenderObject.scheduleInitialPaint</c>, which only the root of a tree calls.</summary>
    internal void ScheduleInitialPaint(OffsetLayer rootLayer)
    {
        Debug.Assert(rootLayer.Attached);
        Debug.Assert(Attached);
        Debug.Assert(Parent is null);
        Debug.Assert(Owner?.DebugDoingPaint != true);
        Debug.Assert(IsRepaintBoundary);
        Debug.Assert(_layer is null);
        _layer = rootLayer;
        Owner!.RootLayer = rootLayer;
        Owner.RequestPaintFor(this);
    }

    /// <summary>Flutter's <c>RenderObject.replaceRootLayer</c>, which only the root of a tree calls.</summary>
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
        if (Owner is not null)
        {
            Owner.RootLayer = rootLayer;
        }

        MarkNeedsPaint();
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

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderView.performLayout</c>: with tight constraints the view has that size and
    /// the child cannot influence it; with loose constraints the view takes the child's size, or
    /// the smallest allowed size when there is no child.
    /// </remarks>
    protected override void PerformLayout()
    {
        Debug.Assert(_rootTransform is not null);
        bool sizedByChild = !Constraints.IsTight;
        _child?.Layout(Constraints, parentUsesSize: sizedByChild);
        Size = sizedByChild && _child is not null ? _child.Size : Constraints.Smallest;
        if (_child is not null)
        {
            ((BoxParentData)_child.parentData!).offset = new Point(0, 0);
        }

        Debug.Assert(double.IsFinite(Size.Width) && double.IsFinite(Size.Height));
        Debug.Assert(Constraints.IsSatisfiedBy(Size));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Mirrors <see cref="PerformLayout"/> so that
    /// <see cref="Rendering.RenderingDebug.CheckIntrinsicSizes"/> does not report the view itself.
    /// </remarks>
    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        bool sizedByChild = !constraints.IsTight;
        return sizedByChild && _child is not null
            ? _child.GetDryLayout(constraints)
            : constraints.Smallest;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            ctx.PaintChild(_child, offset);
        }

        if (Constants.KDebugMode && DebugPaintCallbacks.Count > 0)
        {
            foreach (DebugPaintCallback callback in DebugPaintCallbacks.ToArray())
            {
                if (DebugPaintCallbacks.Contains(callback))
                {
                    callback(ctx, offset, this);
                }
            }
        }
    }

    /// <summary>
    /// Registers a callback that paints on top of every <see cref="RenderView"/>, for debugging
    /// aids such as the widget inspector.
    /// </summary>
    /// <remarks>Flutter's <c>RenderView.debugAddPaintCallback</c>; a no-op outside debug mode.</remarks>
    public static void DebugAddPaintCallback(DebugPaintCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (Constants.KDebugMode)
        {
            DebugPaintCallbacks.Add(callback);
        }
    }

    /// <summary>Removes a callback registered with <see cref="DebugAddPaintCallback"/>.</summary>
    /// <remarks>Flutter's <c>RenderView.debugRemovePaintCallback</c>; a no-op outside debug mode.</remarks>
    public static void DebugRemovePaintCallback(DebugPaintCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (Constants.KDebugMode)
        {
            DebugPaintCallbacks.Remove(callback);
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
            FlutterView.PhysicalSize,
            tooltip: "in physical pixels"));
        properties.Add(new DoubleProperty(
            "device pixel ratio",
            FlutterView.DevicePixelRatio,
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
