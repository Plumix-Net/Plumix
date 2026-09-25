using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/box.dart

namespace Plumix.Rendering;

/// <summary>A render object in a 2D Cartesian coordinate system.</summary>
/// <remarks>
/// Flutter's <c>RenderBox</c>. Two debug mechanisms take a different shape here (see
/// <c>docs/ai/DIVERGENCES.md</c>): Dart's <c>_DebugSize</c> subclass of <c>Size</c> becomes a flag
/// captured next to the size, because Avalonia's <see cref="Size"/> is a sealed value type.
/// </remarks>
public abstract class RenderBox : RenderObject
{
    /// <summary>The "contact support" hint the protocol assertions end with.</summary>
    private const string SupportHint =
        "If you are not writing your own RenderBox subclass, then this is not\nyour fault. "
        + "Contact support: https://github.com/Plumix-Net/Plumix/issues/new";

    private const string BaselineCallingConventions =
        "Please see the documentation for computeDistanceToActualBaseline for the required calling "
        + "conventions of this method.";

    // Dart's debug statics are per-isolate; a render tree here belongs to one thread, and test hosts
    // lay trees out on several threads at once, so they are per-thread (like `_debugActiveLayout`).

    /// <remarks>
    /// Flutter's <c>RenderBox._debugDryLayoutCalculationValid</c>; always reset to <c>true</c> before it
    /// is read.
    /// </remarks>
    [ThreadStatic]
    private static bool _debugDryLayoutCalculationValid;

    /// <remarks>Flutter's <c>RenderBox._debugDoingBaseline</c>.</remarks>
    [ThreadStatic]
    private static bool _debugDoingBaseline;

    /// <remarks>Flutter's <c>RenderBox._debugIntrinsicsDepth</c>.</remarks>
    [ThreadStatic]
    private static int _debugIntrinsicsDepth;

    // C#-only test hook: like Dart, a computation that throws leaves the depth raised; a Dart test
    // file gets a fresh isolate, a Plumix test the same thread.
    internal static void DebugResetIntrinsicsDepthForTests() => _debugIntrinsicsDepth = 0;

    private readonly LayoutCacheStorage _layoutCacheStorage = new();

    private bool _computingThisDryLayout;
    private bool _computingThisDryBaseline;
    private Size? _size;

    /// <summary>
    /// The <c>_canBeUsedByParent</c> flag of Dart's <c>_DebugSize</c>; null while <see cref="_size"/> is
    /// not a debug size (release builds, or before the first layout).
    /// </summary>
    private bool? _debugSizeCanBeUsedByParent;

    /// <remarks>Flutter's <c>RenderBox._debugActivePointers</c>.</remarks>
    private int _debugActivePointers;

    /// <inheritdoc />
    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    private TOutput ComputeIntrinsics<TInput, TOutput>(
        ICachedLayoutCalculation<TInput, TOutput> type,
        TInput input,
        Func<TInput, TOutput> computer)
        where TInput : notnull
    {
        // performResize should not depend on anything except the incoming constraints.
        DebugAssert(DebugCheckingIntrinsics || !DebugDoingThisResize);

        // The debug-mode intrinsic checks must not affect who gets marked dirty, so they bypass the
        // caches entirely.
        bool shouldCache = !(Constants.KDebugMode && DebugCheckingIntrinsics);
        return shouldCache ? ComputeWithTimeline(type, input, computer) : computer(input);
    }

    /// <remarks>Flutter's <c>RenderBox._computeWithTimeline</c>.</remarks>
    private TOutput ComputeWithTimeline<TInput, TOutput>(
        ICachedLayoutCalculation<TInput, TOutput> type,
        TInput input,
        Func<TInput, TOutput> computer)
        where TInput : notnull
    {
        Dictionary<string, object?>? debugTimelineArguments = null;
        if (Constants.KDebugMode)
        {
            Dictionary<string, object?> arguments = RenderingDebug.EnhanceLayoutTimelineArguments
                ? FlutterTimeline.ToTimelineArguments(ToDiagnosticsNode().ToTimelineArguments())!
                : [];
            debugTimelineArguments = type.DebugFillTimelineArguments(arguments, input);
        }

        if (!Constants.KReleaseMode)
        {
            if (RenderingDebug.ProfileLayoutsEnabled || _debugIntrinsicsDepth == 0)
            {
                FlutterTimeline.StartSync(type.EventLabel(this), arguments: debugTimelineArguments);
            }

            _debugIntrinsicsDepth += 1;
        }

        TOutput result = type.Memoize(_layoutCacheStorage, input, computer);
        if (!Constants.KReleaseMode)
        {
            _debugIntrinsicsDepth -= 1;
            if (RenderingDebug.ProfileLayoutsEnabled || _debugIntrinsicsDepth == 0)
            {
                FlutterTimeline.FinishSync();
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the minimum width that this box could be without failing to correctly paint its
    /// contents within itself, without clipping.
    /// </summary>
    public double GetMinIntrinsicWidth(double height)
    {
        DebugCheckIntrinsicArgument(height, "height", "getMinIntrinsicWidth");
        return ComputeIntrinsics(IntrinsicDimension.MinWidth, height, ComputeMinIntrinsicWidth);
    }

    /// <summary>Computes the value returned by <see cref="GetMinIntrinsicWidth"/>.</summary>
    protected virtual double ComputeMinIntrinsicWidth(double height) => 0.0;

    /// <summary>
    /// Returns the smallest width beyond which increasing the width never decreases the preferred
    /// height.
    /// </summary>
    public double GetMaxIntrinsicWidth(double height)
    {
        DebugCheckIntrinsicArgument(height, "height", "getMaxIntrinsicWidth");
        return ComputeIntrinsics(IntrinsicDimension.MaxWidth, height, ComputeMaxIntrinsicWidth);
    }

    /// <summary>Computes the value returned by <see cref="GetMaxIntrinsicWidth"/>.</summary>
    protected virtual double ComputeMaxIntrinsicWidth(double height) => 0.0;

    /// <summary>
    /// Returns the minimum height that this box could be without failing to correctly paint its
    /// contents within itself, without clipping.
    /// </summary>
    public double GetMinIntrinsicHeight(double width)
    {
        DebugCheckIntrinsicArgument(width, "width", "getMinIntrinsicHeight");
        return ComputeIntrinsics(IntrinsicDimension.MinHeight, width, ComputeMinIntrinsicHeight);
    }

    /// <summary>Computes the value returned by <see cref="GetMinIntrinsicHeight"/>.</summary>
    protected virtual double ComputeMinIntrinsicHeight(double width) => 0.0;

    /// <summary>
    /// Returns the smallest height beyond which increasing the height never decreases the preferred
    /// width.
    /// </summary>
    public double GetMaxIntrinsicHeight(double width)
    {
        DebugCheckIntrinsicArgument(width, "width", "getMaxIntrinsicHeight");
        return ComputeIntrinsics(IntrinsicDimension.MaxHeight, width, ComputeMaxIntrinsicHeight);
    }

    /// <summary>Computes the value returned by <see cref="GetMaxIntrinsicHeight"/>.</summary>
    protected virtual double ComputeMaxIntrinsicHeight(double width) => 0.0;

    /// <summary>
    /// Returns the <see cref="Size"/> that this <see cref="RenderBox"/> would like to be given the
    /// provided <see cref="BoxConstraints"/>.
    /// </summary>
    /// <remarks>
    /// The size returned by this method is guaranteed to be the same size that this
    /// <see cref="RenderBox"/> computes for itself during layout given the same constraints.
    /// </remarks>
    public Size GetDryLayout(BoxConstraints constraints)
    {
        return ComputeIntrinsics(DryLayout.Instance, constraints, ComputeDryLayoutGuarded);
    }

    private Size ComputeDryLayoutGuarded(BoxConstraints constraints)
    {
        if (Constants.KDebugMode)
        {
            DebugAssert(!_computingThisDryLayout);
            _computingThisDryLayout = true;
        }

        Size result = ComputeDryLayout(constraints);
        if (Constants.KDebugMode)
        {
            DebugAssert(_computingThisDryLayout);
            _computingThisDryLayout = false;
        }

        return result;
    }

    /// <summary>Computes the value returned by <see cref="GetDryLayout"/>.</summary>
    /// <remarks>
    /// Do not call this function directly, instead, call <see cref="GetDryLayout"/>. Subclasses that
    /// cannot compute a dry layout call <see cref="DebugCannotComputeDryLayout"/> from their override.
    /// </remarks>
    protected virtual Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Constants.KDebugMode)
        {
            DebugCannotComputeDryLayout(error: new FlutterError(
            [
                new ErrorSummary(
                    $"The {Diagnostics.ObjectRuntimeType(this, "RenderBox")} class does not implement "
                    + "\"computeDryLayout\"."),
                new ErrorHint(SupportHint),
            ]));
        }

        return default;
    }

    /// <summary>
    /// Returns the distance from the top of the box to the first baseline of the box's contents for
    /// the given <paramref name="constraints"/>, or null if this box does not have any baselines.
    /// </summary>
    public double? GetDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        double? baselineOffset = ComputeIntrinsics(
            Baseline.Instance,
            (Constraints: constraints, Baseline: baseline),
            ComputeDryBaselineGuarded).Offset;
        // This assert makes sure computeDryBaseline always gets called in debug mode, in case the
        // computeDryBaseline implementation invokes debugCannotComputeDryLayout.
        if (Constants.KDebugMode && !DebugCheckingIntrinsics)
        {
            DebugAssert(baselineOffset == ComputeDryBaseline(constraints, baseline));
        }
        return baselineOffset;
    }

    private BaselineOffset ComputeDryBaselineGuarded((BoxConstraints Constraints, TextBaseline Baseline) pair)
    {
        if (Constants.KDebugMode)
        {
            DebugAssert(!_computingThisDryBaseline);
            _computingThisDryBaseline = true;
        }

        var result = new BaselineOffset(ComputeDryBaseline(pair.Constraints, pair.Baseline));
        if (Constants.KDebugMode)
        {
            DebugAssert(_computingThisDryBaseline);
            _computingThisDryBaseline = false;
        }

        return result;
    }

    /// <summary>Computes the value returned by <see cref="GetDryBaseline"/>.</summary>
    protected virtual double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Constants.KDebugMode)
        {
            DebugCannotComputeDryLayout(error: new FlutterError(
            [
                new ErrorSummary(
                    $"The {Diagnostics.ObjectRuntimeType(this, "RenderBox")} class does not implement "
                    + "\"computeDryBaseline\"."),
                new ErrorHint(SupportHint),
            ]));
        }

        return null;
    }

    /// <summary>
    /// Called from <see cref="ComputeDryLayout"/> or <see cref="ComputeDryBaseline"/> within an
    /// assert if the given <see cref="RenderBox"/> subclass does not support calculating a dry
    /// layout.
    /// </summary>
    /// <remarks>
    /// When asserts are enabled and <see cref="RenderObject.DebugCheckingIntrinsics"/> is not true,
    /// this method throws an exception describing <paramref name="reason"/> (or
    /// <paramref name="error"/>). Otherwise it records that the dry calculation currently in flight is
    /// not valid, so the debug checks skip comparing its result. Exactly one of the two must be given.
    /// </remarks>
    protected bool DebugCannotComputeDryLayout(string? reason = null, FlutterError? error = null)
    {
        DebugAssert((reason is null) != (error is null));
        if (!Constants.KDebugMode)
        {
            return true;
        }

        if (!DebugCheckingIntrinsics)
        {
            if (reason is not null)
            {
                List<DiagnosticsNode> information =
                [
                    new ErrorSummary(
                        $"The {Diagnostics.ObjectRuntimeType(this, "RenderBox")} class does not support dry "
                        + "layout."),
                ];
                if (reason.Length > 0)
                {
                    information.Add(new ErrorDescription(reason));
                }

                throw new FlutterError(information);
            }

            throw error!;
        }

        _debugDryLayoutCalculationValid = false;
        return true;
    }

    /// <summary>Whether this render object has undergone layout and has a <see cref="Size"/>.</summary>
    public virtual bool HasSize => _size != null;

    /// <summary>The size of this render box computed during layout.</summary>
    /// <remarks>
    /// In debug builds, reading the size checks the access rules Dart's <c>_DebugSize</c> enforces: a
    /// render box may always read its own size, but during layout only its parent may read it, and only
    /// if it passed <c>parentUsesSize: true</c>; and no box may read it from a dry layout or dry
    /// baseline computation.
    /// </remarks>
    public Size Size
    {
        get
        {
            if (Constants.KDebugMode)
            {
                if (!HasSize)
                {
                    throw new AssertionError($"RenderBox was not laid out: {this}");
                }

                if (_debugSizeCanBeUsedByParent is bool canBeUsedByParent)
                {
                    DebugCheckSizeAccess(canBeUsedByParent);
                }
            }

            return _size ?? throw new InvalidOperationException(
                $"RenderBox was not laid out: {GetType().Name}#{Diagnostics.ShortHash(this)}");
        }

        protected set
        {
            if (Constants.KDebugMode)
            {
                DebugAssert(!(DebugDoingThisResize && DebugDoingThisLayout));
                DebugAssert(SizedByParent || !DebugDoingThisResize);
                DebugCheckSizeSetterPhase();
                value = DebugAdoptSize(value);
                _debugSizeCanBeUsedByParent = DebugCanParentUseSize;
            }

            _size = value;
            if (Constants.KDebugMode)
            {
                DebugAssertDoesMeetConstraints();
            }
        }
    }

    private void DebugCheckSizeAccess(bool canBeUsedByParent)
    {
        RenderObject? parent = Parent;
        // Whether the size getter is accessed during layout (but not in a layout callback).
        bool doingRegularLayout = !(DebugActiveLayout?.DebugDoingThisLayoutWithCallback ?? true);
        bool sizeAccessAllowed = !doingRegularLayout
                                 || DebugDoingThisResize
                                 || DebugDoingThisLayout
                                 || _debugDoingBaseline
                                 || (DebugActiveLayout == parent && canBeUsedByParent);
        if (!sizeAccessAllowed)
        {
            throw new AssertionError(
                "RenderBox.size accessed beyond the scope of resize, layout, or permitted parent access. "
                + "RenderBox can always access its own size, otherwise, the only object that is allowed to "
                + "read RenderBox.size is its parent, if they have said they will. If you hit this assert "
                + "trying to access a child's size, pass \"parentUsesSize: true\" to that child's layout() in "
                + $"{Diagnostics.ObjectRuntimeType(this, "RenderBox")}.performLayout.");
        }

        RenderBox? renderBoxDoingDryLayout = _computingThisDryLayout
            ? this
            : parent is RenderBox { _computingThisDryLayout: true } dryLayoutParent ? dryLayoutParent : null;
        if (renderBoxDoingDryLayout != null)
        {
            throw new AssertionError(
                $"RenderBox.size accessed in "
                + $"{Diagnostics.ObjectRuntimeType(renderBoxDoingDryLayout, "RenderBox")}.computeDryLayout. "
                + "The computeDryLayout method must not access the RenderBox's own size, or the size of its "
                + "child, because it's established in performLayout or performResize using different "
                + "BoxConstraints.");
        }

        RenderBox? renderBoxDoingDryBaseline = _computingThisDryBaseline
            ? this
            : parent is RenderBox { _computingThisDryBaseline: true } dryBaselineParent ? dryBaselineParent : null;
        if (renderBoxDoingDryBaseline != null)
        {
            throw new AssertionError(
                $"RenderBox.size accessed in "
                + $"{Diagnostics.ObjectRuntimeType(renderBoxDoingDryBaseline, "RenderBox")}.computeDryBaseline. "
                + "The computeDryBaseline method must not access the RenderBox's own size, or the size of its "
                + "child, because it's established in performLayout or performResize using different "
                + "BoxConstraints.");
        }
    }

    /// <summary>
    /// Ports Dart's <c>RenderBox.size</c> setter assertions: the size may only be written by the
    /// object itself, from <see cref="PerformResize"/> when <see cref="SizedByParent"/> is
    /// <c>true</c>, and from <see cref="PerformLayout"/> when it is <c>false</c>.
    /// </summary>
    private void DebugCheckSizeSetterPhase()
    {
        if ((SizedByParent && DebugDoingThisResize) || (!SizedByParent && DebugDoingThisLayout))
        {
            return;
        }

        DebugAssert(!DebugDoingThisResize);
        List<DiagnosticsNode> information = [new ErrorSummary("RenderBox size setter called incorrectly.")];
        if (DebugDoingThisLayout)
        {
            DebugAssert(SizedByParent);
            information.Add(new ErrorDescription("It appears that the size setter was called from performLayout()."));
        }
        else
        {
            information.Add(new ErrorDescription(
                "The size setter was called from outside layout (neither performResize() nor performLayout() "
                + "were being run for this object)."));
            if (Owner is { DebugDoingLayout: true })
            {
                information.Add(new ErrorDescription(
                    "Only the object itself can set its size. It is a contract violation for other objects to "
                    + "set it."));
            }
        }

        information.Add(new ErrorDescription(SizedByParent
            ? "Because this RenderBox has sizedByParent set to true, it must set its size in performResize()."
            : "Because this RenderBox has sizedByParent set to false, it must set its size in performLayout()."));
        throw new FlutterError(information);
    }

    /// <summary>Claims ownership of the given <see cref="Avalonia.Size"/>.</summary>
    /// <remarks>
    /// Flutter's <c>RenderBox.debugAdoptSize</c>. In Dart a size read from a child carries its owner, so
    /// adopting it can report a size taken from a non-child or from a child laid out without
    /// <c>parentUsesSize</c>. Avalonia's <see cref="Avalonia.Size"/> is a sealed value type that carries
    /// no owner, so those two checks have no counterpart and this returns <paramref name="value"/>
    /// unchanged; the parent-access rule is still enforced when the child's size is read.
    /// </remarks>
    public Size DebugAdoptSize(Size value) => value;

    /// <inheritdoc />
    protected override Rect SemanticBounds => new(default, Size);

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderBox.debugResetSize</c>: re-adopts the current size.</remarks>
    protected override void DebugResetSize()
    {
        // `size = size`: re-wraps the size with the current `debugCanParentUseSize`.
        Size = Size;
    }

    /// <summary>
    /// Returns the distance from the y-coordinate of the position of the box to the y-coordinate of
    /// the first given baseline in the box's contents.
    /// </summary>
    /// <remarks>
    /// If there is no baseline, this function returns the height of the box unless
    /// <paramref name="onlyReal"/> is true, in which case it returns null. Only call this from the
    /// parent of this box during that parent's <see cref="PerformLayout"/> or <c>Paint</c>.
    /// </remarks>
    public double? GetDistanceToBaseline(TextBaseline baseline, bool onlyReal = false)
    {
        if (Constants.KDebugMode)
        {
            DebugAssert(!_debugDoingBaseline, BaselineCallingConventions);
            DebugAssert(!DebugNeedsLayout || DebugCheckingIntrinsics);
            DebugAssert(DebugCheckingIntrinsics || DebugIsBaselineCallerAllowed());
            _debugDoingBaseline = true;
        }

        double? result;
        try
        {
            result = GetDistanceToActualBaseline(baseline);
        }
        finally
        {
            if (Constants.KDebugMode)
            {
                _debugDoingBaseline = false;
            }
        }

        if (result == null && !onlyReal)
        {
            return Size.Height;
        }

        return result;
    }

    /// <summary>
    /// Dart's <c>switch (owner!)</c> in <c>getDistanceToBaseline</c>: during layout only the parent
    /// may ask, during paint the parent or the box itself.
    /// </summary>
    /// <remarks>
    /// A detached tree (no owner) — which Dart only reaches by crashing on <c>owner!</c> — has no
    /// pipeline phase to check against, so the check passes.
    /// </remarks>
    private bool DebugIsBaselineCallerAllowed()
    {
        RenderObject? parent = Parent;
        return Owner switch
        {
            null => true,
            { DebugDoingLayout: true } => DebugActiveLayout == parent && parent!.DebugDoingThisLayout,
            { DebugDoingPaint: true } => (DebugActivePaint == parent && parent!.DebugDoingThisPaint)
                                         || (DebugActivePaint == this && DebugDoingThisPaint),
            _ => false,
        };
    }

    /// <summary>Calls <see cref="ComputeDistanceToActualBaseline"/> and caches the result.</summary>
    /// <remarks>
    /// This function must only be called from <see cref="GetDistanceToBaseline"/> and
    /// <see cref="ComputeDistanceToActualBaseline"/>. Dart's <c>@protected</c> member; C# has no
    /// protection level that a sibling render box (a parent forwarding to its child) could call, so it
    /// is public.
    /// </remarks>
    public double? GetDistanceToActualBaseline(TextBaseline baseline)
    {
        DebugAssert(_debugDoingBaseline, BaselineCallingConventions);
        return ComputeIntrinsics(
            Baseline.Instance,
            (Constraints: Constraints, Baseline: baseline),
            pair => new BaselineOffset(ComputeDistanceToActualBaseline(pair.Baseline))).Offset;
    }

    /// <summary>
    /// Returns the distance from the y-coordinate of the position of the box to the y-coordinate of
    /// the first given baseline in the box's contents, if any, or null otherwise.
    /// </summary>
    /// <remarks>
    /// Do not call this function directly. If you need to know the baseline of a child from an
    /// invocation of <see cref="PerformLayout"/> or <c>Paint</c>, call
    /// <see cref="GetDistanceToBaseline"/>; from an override of this method, call
    /// <see cref="GetDistanceToActualBaseline"/> on the child.
    /// </remarks>
    protected virtual double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        DebugAssert(_debugDoingBaseline, BaselineCallingConventions);
        return null;
    }

    /// <inheritdoc />
    public new virtual BoxConstraints Constraints => (BoxConstraints)base.Constraints;

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderBox.debugAssertDoesMeetConstraints</c>.</remarks>
    protected override void DebugAssertDoesMeetConstraints()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (!HasSize)
        {
            throw new FlutterError(
            [
                new ErrorSummary("RenderBox did not set its size during layout."),
                new ErrorDescription(SizedByParent
                    ? "Because this RenderBox has sizedByParent set to true, it must set its size in performResize()."
                    : "Because this RenderBox has sizedByParent set to false, it must set its size in "
                      + "performLayout()."),
                new ErrorDescription(
                    "It appears that this did not happen; layout completed, but the size property is still null."),
                new DiagnosticsProperty<RenderBox>(
                    "The RenderBox in question is", this, style: DiagnosticsTreeStyle.ErrorProperty),
            ]);
        }

        Size size = _size!.Value;
        // Verify that the size is not infinite.
        if (!double.IsFinite(size.Width) || !double.IsFinite(size.Height))
        {
            List<DiagnosticsNode> information =
            [
                new ErrorSummary($"{GetType().Name} object was given an infinite size during layout."),
                new ErrorDescription(
                    "This probably means that it is a render object that tries to be as big as possible, but it "
                    + "was put inside another render object that allows its children to pick their own size."),
            ];
            if (!Constraints.HasBoundedWidth)
            {
                RenderBox node = this;
                while (!node.Constraints.HasBoundedWidth && node.Parent is RenderBox parentBox)
                {
                    node = parentBox;
                }

                information.Add(
                    node.DescribeForError("The nearest ancestor providing an unbounded width constraint is"));
            }

            if (!Constraints.HasBoundedHeight)
            {
                RenderBox node = this;
                while (!node.Constraints.HasBoundedHeight && node.Parent is RenderBox parentBox)
                {
                    node = parentBox;
                }

                information.Add(
                    node.DescribeForError("The nearest ancestor providing an unbounded height constraint is"));
            }

            throw new FlutterError(
            [
                .. information,
                new DiagnosticsProperty<BoxConstraints>(
                    $"The constraints that applied to the {GetType().Name} were",
                    Constraints,
                    style: DiagnosticsTreeStyle.ErrorProperty),
                new DiagnosticsProperty<Size>(
                    "The exact size it was given was", size, style: DiagnosticsTreeStyle.ErrorProperty),
                new ErrorHint("See https://flutter.dev/to/unbounded-constraints for more information."),
            ]);
        }

        // Verify that the size is within the constraints.
        if (!Constraints.IsSatisfiedBy(size))
        {
            throw new FlutterError(
            [
                new ErrorSummary($"{GetType().Name} does not meet its constraints."),
                new DiagnosticsProperty<BoxConstraints>(
                    "Constraints", Constraints, style: DiagnosticsTreeStyle.ErrorProperty),
                new DiagnosticsProperty<Size>("Size", size, style: DiagnosticsTreeStyle.ErrorProperty),
                new ErrorHint(SupportHint.Replace("\n", " ", StringComparison.Ordinal)),
            ]);
        }

        if (RenderingDebug.CheckIntrinsicSizes)
        {
            DebugCheckIntrinsicSizes();
        }
    }

    /// <summary>The <c>debugCheckIntrinsicSizes</c> branch of Dart's <c>debugAssertDoesMeetConstraints</c>.</summary>
    private void DebugCheckIntrinsicSizes()
    {
        // Verify that the intrinsics are sane.
        DebugAssert(!DebugCheckingIntrinsics);
        DebugCheckingIntrinsics = true;
        var failures = new List<DiagnosticsNode>();

        double TestIntrinsic(Func<double, double> function, string name, double constraint)
        {
            double result = function(constraint);
            if (result < 0)
            {
                failures.Add(new ErrorDescription(
                    $" * {name}({DartFormat.Number(constraint)}) returned a negative value: "
                    + DartFormat.Number(result)));
            }

            if (!double.IsFinite(result))
            {
                failures.Add(new ErrorDescription(
                    $" * {name}({DartFormat.Number(constraint)}) returned a non-finite value: "
                    + DartFormat.Number(result)));
            }

            return result;
        }

        void TestIntrinsicsForValues(
            Func<double, double> getMin,
            Func<double, double> getMax,
            string name,
            double constraint)
        {
            double min = TestIntrinsic(getMin, $"getMinIntrinsic{name}", constraint);
            double max = TestIntrinsic(getMax, $"getMaxIntrinsic{name}", constraint);
            if (min > max)
            {
                failures.Add(new ErrorDescription(
                    $" * getMinIntrinsic{name}({DartFormat.Number(constraint)}) returned a larger value "
                    + $"({DartFormat.Number(min)}) than getMaxIntrinsic{name}({DartFormat.Number(constraint)}) "
                    + $"({DartFormat.Number(max)})"));
            }
        }

        try
        {
            TestIntrinsicsForValues(GetMinIntrinsicWidth, GetMaxIntrinsicWidth, "Width", double.PositiveInfinity);
            TestIntrinsicsForValues(GetMinIntrinsicHeight, GetMaxIntrinsicHeight, "Height", double.PositiveInfinity);
            // Dart pairs the width queries with the height bound and vice versa, literally.
            if (Constraints.HasBoundedWidth)
            {
                TestIntrinsicsForValues(GetMinIntrinsicWidth, GetMaxIntrinsicWidth, "Width", Constraints.MaxHeight);
            }

            if (Constraints.HasBoundedHeight)
            {
                TestIntrinsicsForValues(GetMinIntrinsicHeight, GetMaxIntrinsicHeight, "Height", Constraints.MaxWidth);
            }
        }
        finally
        {
            DebugCheckingIntrinsics = false;
        }

        if (failures.Count > 0)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    $"The intrinsic dimension methods of the {GetType().Name} class returned values that "
                    + "violate the intrinsic protocol contract."),
                new ErrorDescription($"The following {(failures.Count > 1 ? "failures" : "failure")} was detected:"),
                .. failures,
                new ErrorHint(SupportHint),
            ]);
        }

        // Checking that GetDryLayout computes the same size.
        _debugDryLayoutCalculationValid = true;
        DebugCheckingIntrinsics = true;
        Size dryLayoutSize;
        try
        {
            dryLayoutSize = GetDryLayout(Constraints);
        }
        finally
        {
            DebugCheckingIntrinsics = false;
        }

        if (_debugDryLayoutCalculationValid && dryLayoutSize != _size)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    $"The size given to the {Diagnostics.ObjectRuntimeType(this, "RenderBox")} class differs from "
                    + "the size computed by computeDryLayout."),
                new ErrorDescription(
                    $"The size computed in {(SizedByParent ? "performResize" : "performLayout")} is "
                    + $"{DartFormat.SizeOf(Size)}, which is different from {DartFormat.SizeOf(dryLayoutSize)}, "
                    + "which was computed by computeDryLayout."),
                new ErrorDescription($"The constraints used were {Constraints}."),
                new ErrorHint(SupportHint),
            ]);
        }
    }

    /// <remarks>Flutter's <c>RenderBox._debugVerifyDryBaselines</c>.</remarks>
    private void DebugVerifyDryBaselines()
    {
        List<DiagnosticsNode> messages =
        [
            new ErrorDescription($"The constraints used were {Constraints}."),
            new ErrorHint(SupportHint),
        ];

        foreach (TextBaseline baseline in Enum.GetValues<TextBaseline>())
        {
            DebugAssert(!DebugCheckingIntrinsics);
            DebugCheckingIntrinsics = true;
            _debugDryLayoutCalculationValid = true;
            double? dryBaseline;
            double? realBaseline;
            try
            {
                dryBaseline = GetDryBaseline(Constraints, baseline);
                realBaseline = GetDistanceToBaseline(baseline, onlyReal: true);
            }
            finally
            {
                DebugCheckingIntrinsics = false;
            }

            if (!_debugDryLayoutCalculationValid || dryBaseline == realBaseline)
            {
                continue;
            }

            string runtimeType = Diagnostics.ObjectRuntimeType(this, "RenderBox");
            string baselineName = DartFormat.Enum(baseline);
            string summary =
                $"The {baselineName} location returned by {runtimeType}.computeDistanceToActualBaseline "
                + "differs from the baseline location computed by computeDryBaseline.";
            if ((dryBaseline == null) != (realBaseline == null))
            {
                (string methodReturnedNull, string methodReturnedNonNull) = dryBaseline == null
                    ? ("computeDryBaseline", "computeDistanceToActualBaseline")
                    : ("computeDistanceToActualBaseline", "computeDryBaseline");
                throw new FlutterError(
                [
                    new ErrorSummary(summary),
                    new ErrorDescription(
                        $"The {methodReturnedNull} method returned null while the {methodReturnedNonNull} returned "
                        + $"a non-null {baselineName} of {DartFormat.Number((dryBaseline ?? realBaseline)!.Value)}. "
                        + $"Did you forget to implement {methodReturnedNull} for {runtimeType}?"),
                    .. messages,
                ]);
            }

            throw new FlutterError(
            [
                new ErrorSummary(summary),
                new DiagnosticsProperty<RenderObject>("The RenderBox was", this),
                new ErrorDescription(
                    $"The computeDryBaseline method returned {DartFormat.Number(dryBaseline!.Value)},\n"
                    + "while the computeDistanceToActualBaseline method returned "
                    + $"{DartFormat.Number(realBaseline!.Value)}.\n"
                    + "Consider checking the implementations of the following methods on the "
                    + $"{runtimeType} class and make sure they are consistent:\n"
                    + " * computeDistanceToActualBaseline\n * computeDryBaseline\n * performLayout\n"),
                .. messages,
            ]);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderBox.markNeedsLayout</c>: clears the intrinsic, dry layout and baseline caches;
    /// if any of them held data, the parent may have used it, so the parent is marked instead.
    /// </remarks>
    public override void MarkNeedsLayout()
    {
        if (_layoutCacheStorage.Clear() && Parent != null)
        {
            MarkParentNeedsLayout();
            return;
        }

        base.MarkNeedsLayout();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderBox.performResize</c>: sets <see cref="Size"/> to the result of
    /// <see cref="ComputeDryLayout"/> for the current constraints. Override
    /// <see cref="ComputeDryLayout"/> rather than this method.
    /// </remarks>
    protected override void PerformResize()
    {
        // Default behavior for subclasses that have sizedByParent = true.
        Size = ComputeDryLayout(Constraints);
        DebugAssert(double.IsFinite(Size.Width) && double.IsFinite(Size.Height));
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        if (Constants.KDebugMode && !SizedByParent)
        {
            throw new FlutterError(
            [
                new ErrorSummary($"{GetType().Name} did not implement performLayout()."),
                new ErrorHint(
                    "RenderBox subclasses need to either override performLayout() to set a size and lay out "
                    + "any children, or, set sizedByParent to true so that performResize() sizes the render "
                    + "object."),
            ]);
        }
    }

    /// <summary>
    /// Determines the set of render objects located at the given position.
    /// </summary>
    /// <remarks>
    /// Returns true, and adds any render objects that contain the point to the given hit test result,
    /// if this render object or one of its descendants absorbs the hit (preventing objects below this
    /// one from being hit). The caller is responsible for transforming <paramref name="position"/> from
    /// global coordinates to its location relative to the origin of this <see cref="RenderBox"/>.
    /// </remarks>
    public virtual bool HitTest(BoxHitTestResult result, Point position)
    {
        if (Constants.KDebugMode && !HasSize)
        {
            if (DebugNeedsLayout)
            {
                throw new FlutterError(
                [
                    new ErrorSummary("Cannot hit test a render box that has never been laid out."),
                    DescribeForError("The hitTest() method was called on this RenderBox"),
                    new ErrorDescription(
                        "Unfortunately, this object's geometry is not known at this time, probably because it has "
                        + "never been laid out. This means it cannot be accurately hit-tested."),
                    new ErrorHint(
                        "If you are trying to perform a hit test during the layout phase itself, make sure you only "
                        + "hit test nodes that have completed layout (e.g. the node's children, after their layout() "
                        + "method has been called)."),
                ]);
            }

            throw new FlutterError(
            [
                new ErrorSummary("Cannot hit test a render box with no size."),
                DescribeForError("The hitTest() method was called on this RenderBox"),
                new ErrorDescription("Although this node is not marked as needing layout, its size is not set."),
                new ErrorHint(
                    "A RenderBox object must have an explicit size before it can be hit-tested. Make sure that "
                    + "the RenderBox in question sets its size during layout."),
            ]);
        }

        Size size = _size ?? throw new InvalidOperationException(
            $"RenderBox was not laid out: {GetType().Name}#{Diagnostics.ShortHash(this)}");
        // Dart's `Size.contains`: the right and bottom edges are outside the box.
        if (position.X >= 0.0 && position.X < size.Width && position.Y >= 0.0 && position.Y < size.Height)
        {
            if (HitTestChildren(result, position) || HitTestSelf(position))
            {
                result.Add(new BoxHitTestEntry(this, position));
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Override this method if this render object can be hit even if its children were not hit.
    /// </summary>
    protected virtual bool HitTestSelf(Point position) => false;

    /// <summary>Override this method to check whether any children are located at the given position.</summary>
    protected virtual bool HitTestChildren(BoxHitTestResult result, Point position) => false;

    /// <inheritdoc />
    /// <remarks>
    /// Multiply the transform from the parent's coordinate system to this box's coordinate system into
    /// the given transform. The default assumes every child uses <see cref="BoxParentData"/>.
    /// </remarks>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        DebugAssert(child.Parent == this);
        if (Constants.KDebugMode && child.parentData is not BoxParentData)
        {
            string runtimeType = GetType().Name;
            throw new FlutterError(
            [
                new ErrorSummary($"{runtimeType} does not implement applyPaintTransform."),
                DescribeForError($"The following {runtimeType} object"),
                child.DescribeForError(
                    "...did not use a BoxParentData class for the parentData field of the following child"),
                new ErrorDescription($"The {runtimeType} class inherits from RenderBox."),
                new ErrorHint(
                    "The default applyPaintTransform implementation provided by RenderBox assumes that the "
                    + "children all use BoxParentData objects for their parentData field. Since "
                    + $"{runtimeType} does not in fact use that ParentData class for its children, it must "
                    + "provide an implementation of applyPaintTransform that supports the specific ParentData "
                    + $"subclass used by its children (which apparently is {child.parentData?.GetType().Name})."),
            ]);
        }

        var childParentData = (BoxParentData)child.parentData!;
        Point offset = childParentData.offset;
        transform.TranslateByDouble(offset.X, offset.Y, 0, 1);
    }

    /// <summary>
    /// Convert the given point from the global coordinate system in logical pixels to the local
    /// coordinate system for this box.
    /// </summary>
    /// <remarks>
    /// An unprojection rather than a plain inverse point transform, so a perspective transform maps
    /// back onto the z = 0 plane the way it was drawn from. If the transform from global coordinates to
    /// local coordinates is degenerate, this function returns <c>Offset.zero</c>.
    /// </remarks>
    public Point GlobalToLocal(Point point, RenderObject? ancestor = null)
    {
        // We want to find point (p) that corresponds to a given point on the screen (s), but that also
        // physically resides on the local render plane, so that it is useful for visually accurate
        // gesture processing in the local space.
        Matrix4 transform = GetTransformTo(ancestor);
        double det = transform.Invert();
        if (det == 0.0)
        {
            return default;
        }

        Vector3 n = new(0.0, 0.0, 1.0);
        Vector3 i = transform.PerspectiveTransform(new Vector3(0.0, 0.0, 0.0));
        Vector3 d = transform.PerspectiveTransform(n) - i;
        if (d.Z == 0.0)
        {
            return default;
        }

        Vector3 s = transform.PerspectiveTransform(new Vector3(point.X, point.Y, 0.0));
        Vector3 p = s - (d * (s.Z / d.Z));
        return new Point(p.X, p.Y);
    }

    /// <summary>
    /// Convert the given point from the local coordinate system for this box to the global coordinate
    /// system in logical pixels.
    /// </summary>
    public Point LocalToGlobal(Point point, RenderObject? ancestor = null)
    {
        return MatrixUtils.TransformPoint(GetTransformTo(ancestor), point);
    }

    /// <summary>A rectangle that contains all the pixels painted by this box.</summary>
    public override Rect PaintBounds => new(default, Size);

    /// <summary>Implements the <see cref="RenderingDebug.PaintPointersEnabled"/> debugging feature.</summary>
    /// <remarks>
    /// Flutter's <c>RenderBox.debugHandleEvent</c>. <see cref="RenderBox"/> subclasses that implement
    /// <see cref="RenderObject.HandleEvent"/> should call this from it. If it is called for a
    /// <see cref="PointerDownEvent"/>, it must also be called for the corresponding
    /// <see cref="PointerUpEvent"/> or <see cref="PointerCancelEvent"/>.
    /// </remarks>
    public bool DebugHandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (!Constants.KDebugMode)
        {
            return true;
        }

        if (RenderingDebug.PaintPointersEnabled)
        {
            if (@event is PointerDownEvent)
            {
                _debugActivePointers += 1;
            }
            else if (@event is PointerUpEvent or PointerCancelEvent)
            {
                _debugActivePointers -= 1;
            }

            MarkNeedsPaint();
        }

        return true;
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderBox.debugPaint</c>.</remarks>
    protected override void DebugPaint(PaintingContext context, Point offset)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        // Only perform the baseline checks after the pipeline owner's layout flush completes: a
        // baseline may depend on the layout of a child, so the safest point to compare the dry and
        // the real implementation is once the entire tree has finished laying out.
        if (RenderingDebug.CheckIntrinsicSizes)
        {
            DebugVerifyDryBaselines();
        }

        if (RenderingDebug.PaintSizeEnabled)
        {
            DebugPaintSize(context, offset);
        }

        if (RenderingDebug.PaintBaselinesEnabled)
        {
            DebugPaintBaselines(context, offset);
        }

        if (RenderingDebug.PaintPointersEnabled)
        {
            DebugPaintPointers(context, offset);
        }
    }

    /// <summary>In debug mode, paints a border around this render box.</summary>
    /// <remarks>
    /// Flutter's <c>RenderBox.debugPaintSize</c>, called for every <see cref="RenderBox"/> when
    /// <see cref="RenderingDebug.PaintSizeEnabled"/> is set.
    /// </remarks>
    protected internal virtual void DebugPaintSize(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFF00FFFF)), 1.0);
        context.Canvas.DrawGeometry(null, pen, new RectangleGeometry(Deflate(new Rect(offset, Size), 0.5)));
    }

    /// <summary>In debug mode, paints a line for each baseline.</summary>
    /// <remarks>
    /// Flutter's <c>RenderBox.debugPaintBaselines</c>, called for every <see cref="RenderBox"/> when
    /// <see cref="RenderingDebug.PaintBaselinesEnabled"/> is set.
    /// </remarks>
    protected virtual void DebugPaintBaselines(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Ideographic baseline.
        double? baselineI = GetDistanceToBaseline(TextBaseline.Ideographic, onlyReal: true);
        if (baselineI is { } ideographic)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFFFFD000)), 0.25);
            context.Canvas.DrawLine(
                pen,
                new Point(offset.X, offset.Y + ideographic),
                new Point(offset.X + Size.Width, offset.Y + ideographic));
        }

        // Alphabetic baseline.
        double? baselineA = GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true);
        if (baselineA is { } alphabetic)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFF00FF00)), 0.25);
            context.Canvas.DrawLine(
                pen,
                new Point(offset.X, offset.Y + alphabetic),
                new Point(offset.X + Size.Width, offset.Y + alphabetic));
        }
    }

    /// <summary>
    /// In debug mode, paints a rectangle if this render box has counted more pointer downs than
    /// pointer up events.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderBox.debugPaintPointers</c>, called for every <see cref="RenderBox"/> when
    /// <see cref="RenderingDebug.PaintPointersEnabled"/> is set. Events are only counted for classes
    /// that call <see cref="DebugHandleEvent"/>.
    /// </remarks>
    protected virtual void DebugPaintPointers(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_debugActivePointers <= 0)
        {
            return;
        }

        var color = Color.FromUInt32(0x00BBBBu | (uint)(0x04000000 * Depth & 0xFF000000));
        context.Canvas.DrawRectangle(new SolidColorBrush(color), null, new Rect(offset, Size));
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Size?>("size", _size, missingIfNull: true));
    }

    private void DebugCheckIntrinsicArgument(double argument, string argumentName, string methodName)
    {
        if (Constants.KDebugMode && argument < 0.0)
        {
            throw new FlutterError(
            [
                new ErrorSummary($"The {argumentName} argument to {methodName} was negative."),
                new ErrorDescription($"The argument to {methodName} must not be negative or null."),
                new ErrorHint(
                    $"If you perform computations on another {argumentName} before passing it to {methodName}, "
                    + "consider using math.max() or double.clamp() to force the value into the valid range."),
            ]);
        }
    }

    /// <summary>Dart's <c>assert(condition, message)</c>, as a catchable error.</summary>
    private static void DebugAssert(bool condition, string? message = null)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }

    /// <remarks>Dart's <c>Rect.deflate</c>.</remarks>
    private static Rect Deflate(Rect rect, double delta)
    {
        return new Rect(
            rect.X + delta,
            rect.Y + delta,
            Math.Max(0.0, rect.Width - (delta * 2.0)),
            Math.Max(0.0, rect.Height - (delta * 2.0)));
    }

    /// <remarks>
    /// Flutter's <c>_CachedLayoutCalculation</c>: how one kind of cached layout query is memoized and
    /// reported to the timeline.
    /// </remarks>
    private interface ICachedLayoutCalculation<TInput, TOutput>
        where TInput : notnull
    {
        TOutput Memoize(LayoutCacheStorage cacheStorage, TInput input, Func<TInput, TOutput> computer);

        // Debug information that will be used to generate the Timeline event for this type of
        // calculation.
        Dictionary<string, object?> DebugFillTimelineArguments(
            Dictionary<string, object?> timelineArguments,
            TInput input);

        string EventLabel(RenderBox renderBox);
    }

    /// <remarks>Flutter's <c>_DryLayout</c>.</remarks>
    private sealed class DryLayout : ICachedLayoutCalculation<BoxConstraints, Size>
    {
        public static readonly DryLayout Instance = new();

        public Size Memoize(LayoutCacheStorage cacheStorage, BoxConstraints input, Func<BoxConstraints, Size> computer)
        {
            Dictionary<BoxConstraints, Size> cache = cacheStorage.CachedDryLayoutSizes ??= [];
            return PutIfAbsent(cache, input, () => computer(input));
        }

        public Dictionary<string, object?> DebugFillTimelineArguments(
            Dictionary<string, object?> timelineArguments,
            BoxConstraints input)
        {
            timelineArguments["getDryLayout constraints"] = input.ToString();
            return timelineArguments;
        }

        public string EventLabel(RenderBox renderBox) =>
            $"{Diagnostics.DescribeType(renderBox.GetType())}.getDryLayout";
    }

    /// <remarks>
    /// Flutter's <c>_Baseline</c>: one map per baseline kind, keyed by the constraints only, and shared
    /// between the dry and the real baseline.
    /// </remarks>
    private sealed class Baseline
        : ICachedLayoutCalculation<(BoxConstraints Constraints, TextBaseline Baseline), BaselineOffset>
    {
        public static readonly Baseline Instance = new();

        public BaselineOffset Memoize(
            LayoutCacheStorage cacheStorage,
            (BoxConstraints Constraints, TextBaseline Baseline) input,
            Func<(BoxConstraints Constraints, TextBaseline Baseline), BaselineOffset> computer)
        {
            Dictionary<BoxConstraints, BaselineOffset> cache = input.Baseline switch
            {
                TextBaseline.Alphabetic => cacheStorage.CachedAlphabeticBaseline ??= [],
                TextBaseline.Ideographic => cacheStorage.CachedIdeoBaseline ??= [],
                _ => throw new ArgumentOutOfRangeException(nameof(input), input.Baseline, null),
            };
            return PutIfAbsent(cache, input.Constraints, () => computer(input));
        }

        public Dictionary<string, object?> DebugFillTimelineArguments(
            Dictionary<string, object?> timelineArguments,
            (BoxConstraints Constraints, TextBaseline Baseline) input)
        {
            timelineArguments["baseline type"] = input.Baseline == TextBaseline.Alphabetic
                ? "TextBaseline.alphabetic"
                : "TextBaseline.ideographic";
            timelineArguments["constraints"] = input.Constraints.ToString();
            return timelineArguments;
        }

        public string EventLabel(RenderBox renderBox) =>
            $"{Diagnostics.DescribeType(renderBox.GetType())}.getDryBaseline";
    }

    /// <summary>Dart's <c>Map.putIfAbsent</c>.</summary>
    private static TValue PutIfAbsent<TKey, TValue>(Dictionary<TKey, TValue> map, TKey key, Func<TValue> ifAbsent)
        where TKey : notnull
    {
        if (map.TryGetValue(key, out TValue? existing))
        {
            return existing;
        }

        TValue value = ifAbsent();
        map[key] = value;
        return value;
    }

    /// <remarks>Flutter's <c>_IntrinsicDimension</c>.</remarks>
    private enum IntrinsicDimensionKind
    {
        MinWidth,
        MaxWidth,
        MinHeight,
        MaxHeight,
    }

    /// <remarks>
    /// Flutter's <c>_IntrinsicDimension</c> values, each carrying its <c>memoize</c> implementation.
    /// </remarks>
    private sealed class IntrinsicDimension(IntrinsicDimensionKind kind) : ICachedLayoutCalculation<double, double>
    {
        public static readonly IntrinsicDimension MinWidth = new(IntrinsicDimensionKind.MinWidth);
        public static readonly IntrinsicDimension MaxWidth = new(IntrinsicDimensionKind.MaxWidth);
        public static readonly IntrinsicDimension MinHeight = new(IntrinsicDimensionKind.MinHeight);
        public static readonly IntrinsicDimension MaxHeight = new(IntrinsicDimensionKind.MaxHeight);

        public double Memoize(LayoutCacheStorage cacheStorage, double input, Func<double, double> computer)
        {
            Dictionary<(IntrinsicDimensionKind, double), double> cache = cacheStorage.CachedIntrinsicDimensions ??= [];
            return PutIfAbsent(cache, (kind, input), () => computer(input));
        }

        public Dictionary<string, object?> DebugFillTimelineArguments(
            Dictionary<string, object?> timelineArguments,
            double input)
        {
            timelineArguments["intrinsics dimension"] = kind switch
            {
                IntrinsicDimensionKind.MinWidth => "minWidth",
                IntrinsicDimensionKind.MaxWidth => "maxWidth",
                IntrinsicDimensionKind.MinHeight => "minHeight",
                _ => "maxHeight",
            };
            timelineArguments["intrinsics argument"] = BindingBase.DartDoubleToString(input);
            return timelineArguments;
        }

        public string EventLabel(RenderBox renderBox) => $"{Diagnostics.DescribeType(renderBox.GetType())} intrinsics";
    }

    /// <remarks>Flutter's <c>_LayoutCacheStorage</c>.</remarks>
    private sealed class LayoutCacheStorage
    {
        public Dictionary<(IntrinsicDimensionKind, double), double>? CachedIntrinsicDimensions;
        public Dictionary<BoxConstraints, Size>? CachedDryLayoutSizes;
        public Dictionary<BoxConstraints, BaselineOffset>? CachedAlphabeticBaseline;
        public Dictionary<BoxConstraints, BaselineOffset>? CachedIdeoBaseline;

        /// <summary>
        /// Returns a boolean indicating whether the cache storage has cached intrinsics / dry layout data
        /// in it, and clears it.
        /// </summary>
        public bool Clear()
        {
            bool hasCache = (CachedDryLayoutSizes?.Count > 0)
                            || (CachedIntrinsicDimensions?.Count > 0)
                            || (CachedAlphabeticBaseline?.Count > 0)
                            || (CachedIdeoBaseline?.Count > 0);
            if (hasCache)
            {
                CachedDryLayoutSizes?.Clear();
                CachedIntrinsicDimensions?.Clear();
                CachedAlphabeticBaseline?.Clear();
                CachedIdeoBaseline?.Clear();
            }

            return hasCache;
        }
    }
}
