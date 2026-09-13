using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

public class RenderIgnorePointer : RenderProxyBox
{
    private const string IgnoringSemanticsDeprecation =
        "Use ExcludeSemantics or create a custom ignore pointer widget instead. "
        + "This feature was deprecated after v3.8.0-12.0.pre.";

    private bool _ignoring;
    private bool? _ignoringSemantics;

    /// <remarks>
    /// Dart deprecates the <c>ignoringSemantics</c> parameter; C# cannot mark a parameter obsolete, so
    /// only <see cref="IgnoringSemantics"/> carries the deprecation.
    /// </remarks>
    public RenderIgnorePointer(
        RenderBox? child = null,
        bool ignoring = true,
        bool? ignoringSemantics = null) : base(child)
    {
        _ignoring = ignoring;
        _ignoringSemantics = ignoringSemantics;
    }

    public bool Ignoring
    {
        get => _ignoring;
        set
        {
            if (value == _ignoring)
            {
                return;
            }

            _ignoring = value;
            if (_ignoringSemantics == null)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    [Obsolete(IgnoringSemanticsDeprecation)]
    public bool? IgnoringSemantics
    {
        get => _ignoringSemantics;
        set
        {
            if (value == _ignoringSemantics)
            {
                return;
            }

            _ignoringSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !Ignoring && base.HitTest(result, position);
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_ignoringSemantics ?? false)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsBlockingUserActions = _ignoring && (_ignoringSemantics ?? true);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("ignoring", _ignoring));
        properties.Add(new DiagnosticsProperty<bool?>(
            "ignoringSemantics",
            _ignoringSemantics,
            description: _ignoringSemantics is { } value ? $"implicitly {(value ? "true" : "false")}" : null));
    }
}

public class RenderOffstage : RenderProxyBox
{
    private bool _offstage;

    public RenderOffstage(bool offstage = true, RenderBox? child = null) : base(child)
    {
        _offstage = offstage;
    }

    public bool Offstage
    {
        get => _offstage;
        set
        {
            if (value == _offstage)
            {
                return;
            }

            _offstage = value;
            MarkNeedsLayoutForSizedByParentChange();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        Offstage ? 0.0 : base.ComputeMinIntrinsicWidth(height);

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        Offstage ? 0.0 : base.ComputeMaxIntrinsicWidth(height);

    protected override double ComputeMinIntrinsicHeight(double width) =>
        Offstage ? 0.0 : base.ComputeMinIntrinsicHeight(width);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        Offstage ? 0.0 : base.ComputeMaxIntrinsicHeight(width);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) =>
        Offstage ? null : base.ComputeDistanceToActualBaseline(baseline);

    /// <inheritdoc />
    protected override bool SizedByParent => Offstage;

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) =>
        Offstage ? null : base.ComputeDryBaseline(constraints, baseline);

    protected override Size ComputeDryLayout(BoxConstraints constraints) =>
        Offstage ? constraints.Smallest : base.ComputeDryLayout(constraints);

    protected override void PerformResize()
    {
        Debug.Assert(Offstage);
        base.PerformResize();
    }

    protected override void PerformLayout()
    {
        if (Offstage)
        {
            Child?.Layout(Constraints);
        }
        else
        {
            base.PerformLayout();
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !Offstage && base.HitTest(result, position);
    }

    /// <inheritdoc />
    public override bool PaintsChild(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return !Offstage;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Offstage)
        {
            return;
        }

        base.Paint(context, offset);
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Offstage)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("offstage", Offstage));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        if (Child is null)
        {
            return [];
        }

        return
        [
            Child.ToDiagnosticsNode(
                name: "child",
                style: Offstage ? DiagnosticsTreeStyle.Offstage : DiagnosticsTreeStyle.Sparse),
        ];
    }
}

public class RenderAbsorbPointer : RenderProxyBox
{
    private const string IgnoringSemanticsDeprecation =
        "Use ExcludeSemantics or create a custom absorb pointer widget instead. "
        + "This feature was deprecated after v3.8.0-12.0.pre.";

    private bool _absorbing;
    private bool? _ignoringSemantics;

    /// <remarks>
    /// Dart deprecates the <c>ignoringSemantics</c> parameter; C# cannot mark a parameter obsolete, so
    /// only <see cref="IgnoringSemantics"/> carries the deprecation.
    /// </remarks>
    public RenderAbsorbPointer(
        RenderBox? child = null,
        bool absorbing = true,
        bool? ignoringSemantics = null) : base(child)
    {
        _absorbing = absorbing;
        _ignoringSemantics = ignoringSemantics;
    }

    public bool Absorbing
    {
        get => _absorbing;
        set
        {
            if (_absorbing == value)
            {
                return;
            }

            _absorbing = value;
            if (_ignoringSemantics == null)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    [Obsolete(IgnoringSemanticsDeprecation)]
    public bool? IgnoringSemantics
    {
        get => _ignoringSemantics;
        set
        {
            if (value == _ignoringSemantics)
            {
                return;
            }

            _ignoringSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderAbsorbPointer.hitTest</c> reports the hit without adding itself to the
    /// path, so the absorber swallows the event rather than receiving it.
    /// </remarks>
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return Absorbing
            ? Size.Contains(position)
            : base.HitTest(result, position);
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_ignoringSemantics ?? false)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsBlockingUserActions = Absorbing && (_ignoringSemantics ?? true);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("absorbing", Absorbing));
        properties.Add(new DiagnosticsProperty<bool?>(
            "ignoringSemantics",
            _ignoringSemantics,
            description: _ignoringSemantics is { } value ? $"implicitly {(value ? "true" : "false")}" : null));
    }
}

/// <remarks>Dart's <c>dynamic metaData</c> is <see cref="object"/>.</remarks>
public class RenderMetaData : RenderProxyBoxWithHitTestBehavior
{
    public RenderMetaData(
        object? metaData = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        RenderBox? child = null) : base(behavior: behavior, child: child)
    {
        MetaData = metaData;
    }

    public object? MetaData { get; set; }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<object>("metaData", MetaData));
    }
}

/// <summary>
/// Adds the <see cref="SemanticsProperties"/> it is given to the semantics of its box child.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderSemanticsAnnotations</c>. Dart applies <c>SemanticsAnnotationsMixin</c>; C# has no
/// mixins, so the class composes <see cref="SemanticsAnnotations"/> and forwards its members.
/// </remarks>
public class RenderSemanticsAnnotations : RenderProxyBox
{
    private readonly SemanticsAnnotations _annotations;

    public RenderSemanticsAnnotations(
        SemanticsProperties properties,
        RenderBox? child = null,
        bool container = false,
        bool explicitChildNodes = false,
        bool excludeSemantics = false,
        bool blockUserActions = false,
        Locale? localeForSubtree = null,
        TextDirection? textDirection = null) : base(child)
    {
        _annotations = new SemanticsAnnotations(
            MarkNeedsSemanticsUpdate,
            properties,
            container: container,
            explicitChildNodes: explicitChildNodes,
            excludeSemantics: excludeSemantics,
            blockUserActions: blockUserActions,
            textDirection: textDirection,
            localeForSubtree: localeForSubtree);
    }

    /// <summary>All the annotations this render object contributes.</summary>
    /// <remarks>
    /// Flutter's <c>SemanticsAnnotationsMixin.properties</c>. The setter compares by reference, so a
    /// widget hands it a freshly built value object on every update.
    /// </remarks>
    public SemanticsProperties Properties
    {
        get => _annotations.Properties;
        set => _annotations.Properties = value;
    }

    /// <summary>Whether this annotation introduces a semantics node of its own.</summary>
    public bool Container
    {
        get => _annotations.Container;
        set => _annotations.Container = value;
    }

    /// <summary>Whether the descendants must each produce their own semantics node.</summary>
    public bool ExplicitChildNodes
    {
        get => _annotations.ExplicitChildNodes;
        set => _annotations.ExplicitChildNodes = value;
    }

    /// <summary>Whether to drop all of the child's semantics.</summary>
    public bool ExcludeSemantics
    {
        get => _annotations.ExcludeSemantics;
        set => _annotations.ExcludeSemantics = value;
    }

    /// <summary>Whether the user actions of this subtree are blocked.</summary>
    public bool BlockUserActions
    {
        get => _annotations.BlockUserActions;
        set => _annotations.BlockUserActions = value;
    }

    /// <summary>The locale annotated onto this subtree's semantics nodes.</summary>
    public Locale? LocaleForSubtree
    {
        get => _annotations.LocaleForSubtree;
        set => _annotations.LocaleForSubtree = value;
    }

    /// <summary>The reading direction for this subtree's semantic strings.</summary>
    public TextDirection? TextDirection
    {
        get => _annotations.TextDirection;
        set => _annotations.TextDirection = value;
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.visitChildrenForSemantics</c>.</remarks>
    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_annotations.ExcludeSemantics)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    /// <inheritdoc />
    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        _annotations.DescribeSemanticsConfiguration(configuration);
    }
}

public class RenderBlockSemantics : RenderProxyBox
{
    private bool _blocking;

    public RenderBlockSemantics(RenderBox? child = null, bool blocking = true) : base(child)
    {
        _blocking = blocking;
    }

    public bool Blocking
    {
        get => _blocking;
        set
        {
            if (value == _blocking)
            {
                return;
            }

            _blocking = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsBlockingSemanticsOfPreviouslyPaintedNodes = Blocking;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("blocking", Blocking));
    }
}

public class RenderMergeSemantics : RenderProxyBox
{
    public RenderMergeSemantics(RenderBox? child = null) : base(child)
    {
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
        configuration.IsMergingSemanticsOfDescendants = true;
    }
}

public class RenderExcludeSemantics : RenderProxyBox
{
    private bool _excluding;

    public RenderExcludeSemantics(RenderBox? child = null, bool excluding = true) : base(child)
    {
        _excluding = excluding;
    }

    public bool Excluding
    {
        get => _excluding;
        set
        {
            if (value == _excluding)
            {
                return;
            }

            _excluding = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Excluding)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("excluding", Excluding));
    }
}

public class RenderIndexedSemantics : RenderProxyBox
{
    private int _index;

    public RenderIndexedSemantics(int index, RenderBox? child = null) : base(child)
    {
        _index = index;
    }

    public int Index
    {
        get => _index;
        set
        {
            if (value == Index)
            {
                return;
            }

            _index = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IndexInParent = Index;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<int>("index", Index));
    }
}
