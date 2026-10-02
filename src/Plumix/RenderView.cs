using System.Diagnostics;
using Avalonia;
using Plumix.Rendering;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/view.dart

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
/// Flutter's <c>RenderView</c>, a bare <see cref="RenderObject"/> with one <see cref="RenderBox"/>
/// child (Dart's <c>RenderObjectWithChildMixin&lt;RenderBox&gt;</c>, folded in here). It is
/// constructed for a <see cref="FlutterView"/>, laid out against its <see cref="Configuration"/> once
/// <see cref="PrepareInitialFrame"/> bootstrapped layout and paint, and is always a repaint
/// boundary whose root layer is a <see cref="TransformLayer"/> carrying the device pixel ratio.
/// </para>
/// <para>
/// <see cref="CompositeFrame"/> hands that root layer to <see cref="Widgets.FlutterView.Render"/>
/// instead of an engine scene; see <c>docs/ai/DIVERGENCES.md</c>.
/// </para>
/// </remarks>
public class RenderView : RenderObject, IRenderObjectSingleChildContainer
{
    private static readonly List<DebugPaintCallback> DebugPaintCallbacks = [];

    private RenderBox? _child;
    private Size _size;
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

    /// <summary>The current layout size of the view.</summary>
    /// <remarks>Flutter's <c>RenderView.size</c>.</remarks>
    public Size Size => _size;

    /// <summary>
    /// The constraints and pixel density used for the root layout.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderView.configuration</c>. Until <see cref="PrepareInitialFrame"/> has run the
    /// value is only stored; afterwards a change relays the view out and, when
    /// <see cref="ViewConfiguration.ShouldUpdateMatrix"/> says so, replaces the root layer.
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
                // [prepareInitialFrame] has not been called yet, nothing more to do for now.
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

    /// <summary>The constraints the view lays its child out under: the configuration's logical ones.</summary>
    /// <remarks>
    /// Flutter's <c>RenderView.constraints</c>. The view is never laid out by a parent, so its
    /// constraints come from <see cref="Configuration"/>; reading them before a configuration is set
    /// throws Dart's <c>StateError</c> (an <see cref="InvalidOperationException"/>).
    /// </remarks>
    public new BoxConstraints Constraints
    {
        get
        {
            if (!HasConfiguration)
            {
                // Dart's `StateError`.
                throw new InvalidOperationException(
                    "Constraints are not available because RenderView has not been given a configuration yet.");
            }

            return Configuration.LogicalConstraints;
        }
    }

    /// <summary>The platform view this render view renders into.</summary>
    /// <remarks>Flutter's <c>RenderView.flutterView</c>.</remarks>
    public FlutterView FlutterView { get; }

    /// <summary>
    /// Whether Flutter should automatically compute the desired system UI overlay style from the
    /// painted <see cref="SystemUiOverlayStyle"/> annotations after each composited frame.
    /// </summary>
    /// <remarks>Flutter's <c>RenderView.automaticSystemUiAdjustment</c>.</remarks>
    public bool AutomaticSystemUiAdjustment { get; set; } = true;

    /// <summary>The render object's only child.</summary>
    /// <remarks>
    /// Dart's <c>RenderObjectWithChildMixin.child</c>: drops the old child and adopts the new one;
    /// adoption and dropping mark this object dirty.
    /// </remarks>
    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;

            if (_child != null)
            {
                AdoptChild(_child);
            }
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
    private TransformLayer UpdateMatricesAndCreateNewRootLayer()
    {
        Debug.Assert(HasConfiguration);
        _rootTransform = Configuration.ToMatrix();
        var rootLayer = new TransformLayer(transform: _rootTransform);
        rootLayer.Attach(this);
        Debug.Assert(_rootTransform is not null);
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

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderView.debugAssertDoesMeetConstraints</c>. The view is never laid out by
    /// <see cref="RenderObject.Layout"/> (it is laid out through <see cref="RenderObject.ScheduleInitialLayout"/>),
    /// so this is never checked.
    /// </remarks>
    protected override void DebugAssertDoesMeetConstraints()
    {
        Debug.Assert(false);
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderView.performResize</c>, which is never called.</remarks>
    protected override void PerformResize()
    {
        Debug.Assert(false);
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
        BoxConstraints constraints = Constraints;
        bool sizedByChild = !constraints.IsTight;
        _child?.Layout(constraints, parentUsesSize: sizedByChild);
        _size = sizedByChild && _child is not null ? _child.Size : constraints.Smallest;
        Debug.Assert(double.IsFinite(_size.Width) && double.IsFinite(_size.Height));
        Debug.Assert(constraints.IsSatisfiedBy(_size));
    }

    /// <summary>
    /// Determines the set of render objects located at the given position.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderView.hitTest</c>: the child is tested under a wrapping
    /// <see cref="BoxHitTestResult"/> and the view always adds itself, so it returns <c>true</c>
    /// even when the position lies outside the view. <paramref name="position"/> is in logical
    /// pixels; the device pixel ratio is not applied.
    /// </remarks>
    public bool HitTest(HitTestResult result, Point position)
    {
        ArgumentNullException.ThrowIfNull(result);
        _child?.HitTest(BoxHitTestResult.Wrap(result), position: position);
        result.Add(new HitTestEntry(this));
        return true;
    }

    /// <inheritdoc />
    public override bool IsRepaintBoundary => true;

    /// <inheritdoc />
    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            ctx.PaintChild(_child, offset);
        }

        if (Constants.KDebugMode)
        {
            foreach (DebugPaintCallback paintCallback in DebugPaintCallbacks.ToArray())
            {
                if (DebugPaintCallbacks.Contains(paintCallback))
                {
                    paintCallback(ctx, offset, this);
                }
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderView.applyPaintTransform</c>: the child is painted under the root
    /// transform, which scales logical pixels to physical ones.
    /// </remarks>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        Debug.Assert(_rootTransform is not null);
        transform.Multiply(_rootTransform!);
        base.ApplyPaintTransform(child, transform);
    }

    /// <summary>
    /// Uploads the composited layer tree to the view.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderView.compositeFrame</c>, which <c>RendererBinding.DrawFrame</c> calls for every
    /// registered view. The root layer goes to <see cref="Widgets.FlutterView.Render"/>; a host renders
    /// it into its Avalonia drawing context, and the system chrome is updated from the painted
    /// annotations once the scene is built. A view that has not painted its first frame yet has no
    /// root layer and is skipped, where Dart's <c>layer!</c> would throw.
    /// </remarks>
    public void CompositeFrame()
    {
        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.StartSync("COMPOSITING");
        }

        try
        {
            if (_layer is not OffsetLayer rootLayer)
            {
                return;
            }

            bool hostRendered = FlutterView.HasRenderer;
            FlutterView.Render(rootLayer);
            if (!hostRendered)
            {
                // Dart runs this between `buildScene` and `render`: the scene has to be built first,
                // because a follower layer's `find` reads the transform the build computed. Without a
                // host, `Render` has just built the scene; a host builds it in its own render pass
                // and updates the system chrome there (PlumixHost.Render).
                if (AutomaticSystemUiAdjustment)
                {
                    UpdateSystemChrome();
                }

                // A host advances the repaint rainbow when it draws the layer (PipelineOwner.CompositeFrame).
                RenderingDebug.AdvanceRepaintColorForFrame();
            }
        }
        finally
        {
            if (!Constants.KReleaseMode)
            {
                FlutterTimeline.FinishSync();
            }
        }
    }

    /// <summary>
    /// Takes the overlay style from the places where the system status bar and the system
    /// navigation bar are drawn, and sends it to <see cref="SystemChrome"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's private <c>RenderView._updateSystemChrome</c>. The horizontal center of the screen
    /// and the vertical centers of the status bar (top padding) and the navigation bar (bottom
    /// padding) are sampled in physical pixels, which the root <see cref="TransformLayer"/>'s
    /// <c>find</c> maps back to logical ones; only Android has a customizable navigation bar. A host
    /// builds the scene in its own render pass and calls this right after (<c>docs/ai/DIVERGENCES.md</c>,
    /// <c>FlutterHost</c> frame row).
    /// </remarks>
    internal void UpdateSystemChrome()
    {
        if (_layer is not ContainerLayer rootLayer)
        {
            return;
        }

        // Take overlay style from the place where a system status bar and system navigation bar are
        // placed to update system style overlay.
        Rect bounds = PaintBounds;
        // Center of the status bar.
        var top = new Point(
            // Horizontal center of the screen.
            bounds.Center.X,
            // The vertical center of the system status bar. The system status bar height is kept as
            // top window padding.
            FlutterView.Padding.Top / 2.0);
        // Center of the navigation bar.
        var bottom = new Point(
            // Horizontal center of the screen.
            bounds.Center.X,
            // Vertical center of the system navigation bar. The system navigation bar height is kept
            // as bottom window padding. The "1" needs to be subtracted from the bottom because
            // available pixels are in (0..bottom) range.
            bounds.Bottom - 1.0 - FlutterView.Padding.Bottom / 2.0);
        SystemUiOverlayStyle? upperOverlayStyle = rootLayer.Find<SystemUiOverlayStyle>(top);
        // Only android has a customizable system navigation bar.
        SystemUiOverlayStyle? lowerOverlayStyle = null;
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.Android:
                lowerOverlayStyle = rootLayer.Find<SystemUiOverlayStyle>(bottom);
                break;
            case TargetPlatform.Fuchsia:
            case TargetPlatform.IOS:
            case TargetPlatform.Linux:
            case TargetPlatform.MacOS:
            case TargetPlatform.Windows:
                break;
        }

        // If there are no overlay style in the UI don't bother updating.
        if (upperOverlayStyle is null && lowerOverlayStyle is null)
        {
            return;
        }

        // If both are not null, the upper provides the status bar properties and the lower provides
        // the system navigation bar properties.
        if (upperOverlayStyle is not null && lowerOverlayStyle is not null)
        {
            SystemChrome.SetSystemUIOverlayStyle(new SystemUiOverlayStyle(
                statusBarBrightness: upperOverlayStyle.StatusBarBrightness,
                statusBarIconBrightness: upperOverlayStyle.StatusBarIconBrightness,
                statusBarColor: upperOverlayStyle.StatusBarColor,
                systemStatusBarContrastEnforced: upperOverlayStyle.SystemStatusBarContrastEnforced,
                systemNavigationBarColor: lowerOverlayStyle.SystemNavigationBarColor,
                systemNavigationBarDividerColor: lowerOverlayStyle.SystemNavigationBarDividerColor,
                systemNavigationBarIconBrightness: lowerOverlayStyle.SystemNavigationBarIconBrightness,
                systemNavigationBarContrastEnforced: lowerOverlayStyle.SystemNavigationBarContrastEnforced));
            return;
        }

        // If only one of the upper or the lower overlay style is not null, it provides all properties.
        bool isAndroid = PlatformDefaults.TargetPlatform == TargetPlatform.Android;
        SystemUiOverlayStyle definedOverlayStyle = (upperOverlayStyle ?? lowerOverlayStyle)!;
        SystemChrome.SetSystemUIOverlayStyle(new SystemUiOverlayStyle(
            statusBarBrightness: definedOverlayStyle.StatusBarBrightness,
            statusBarIconBrightness: definedOverlayStyle.StatusBarIconBrightness,
            statusBarColor: definedOverlayStyle.StatusBarColor,
            systemStatusBarContrastEnforced: definedOverlayStyle.SystemStatusBarContrastEnforced,
            systemNavigationBarColor: isAndroid ? definedOverlayStyle.SystemNavigationBarColor : null,
            systemNavigationBarDividerColor:
                isAndroid ? definedOverlayStyle.SystemNavigationBarDividerColor : null,
            systemNavigationBarIconBrightness:
                isAndroid ? definedOverlayStyle.SystemNavigationBarIconBrightness : null,
            systemNavigationBarContrastEnforced:
                isAndroid ? definedOverlayStyle.SystemNavigationBarContrastEnforced : null));
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderView.paintBounds</c>: the view's size in physical pixels.</remarks>
    public override Rect PaintBounds
    {
        get
        {
            double devicePixelRatio = Configuration.DevicePixelRatio;
            return new Rect(0.0, 0.0, _size.Width * devicePixelRatio, _size.Height * devicePixelRatio);
        }
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderView.semanticBounds</c>: the view's box under the root transform.</remarks>
    protected override Rect SemanticBounds
    {
        get
        {
            Debug.Assert(_rootTransform is not null);
            return MatrixUtils.TransformRect(_rootTransform!, new Rect(_size));
        }
    }

    /// <summary>
    /// Sends <paramref name="update"/> to the view.
    /// </summary>
    /// <remarks>Flutter's <c>RenderView.updateSemantics</c>.</remarks>
    public void UpdateSemantics(SemanticsUpdate update)
    {
        FlutterView.UpdateSemantics(update);
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
