using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/box.dart (approximate)

namespace Plumix.Rendering;

/// <summary>
/// A render object in a 2D Cartesian coordinate system.
/// </summary>
public abstract class RenderBox : RenderObject
{
    [ThreadStatic]
    private static RenderBox? _activeDryCalculation;

    private readonly LayoutCacheStorage _layoutCacheStorage = new();
    private Size? _size;

    internal static bool DebugCheckingIntrinsics { get; set; }

    protected override Rect SemanticBounds => new(new Point(0, 0), HasSize ? Size : new Size());

    public Size Size
    {
        get
        {
            if (_activeDryCalculation != null)
            {
                throw new InvalidOperationException(
                    $"RenderBox.Size was accessed while {_activeDryCalculation.GetType().Name} was computing "
                    + "a dry layout result.");
            }

            return _size ??
                   throw new InvalidOperationException(
                       $"RenderBox was not laid out: {GetType()}#{Diagnostics.ShortHash(this)}");
        }

        protected set => _size = value;
    }

    public bool HasSize => _size != null;

    /// <summary>An estimate of the bounds within which this render object will paint.</summary>
    public override Rect PaintBounds => new(default, Size);

    public new virtual BoxConstraints Constraints => (BoxConstraints)base.Constraints;

    public double GetMinIntrinsicWidth(double height)
    {
        return GetIntrinsicDimension(IntrinsicDimension.MinWidth, height, ComputeMinIntrinsicWidth);
    }

    public double GetMaxIntrinsicWidth(double height)
    {
        return GetIntrinsicDimension(IntrinsicDimension.MaxWidth, height, ComputeMaxIntrinsicWidth);
    }

    public double GetMinIntrinsicHeight(double width)
    {
        return GetIntrinsicDimension(IntrinsicDimension.MinHeight, width, ComputeMinIntrinsicHeight);
    }

    public double GetMaxIntrinsicHeight(double width)
    {
        return GetIntrinsicDimension(IntrinsicDimension.MaxHeight, width, ComputeMaxIntrinsicHeight);
    }

    public Size GetDryLayout(BoxConstraints constraints)
    {
        if (!constraints.IsNormalized)
        {
            throw new InvalidOperationException("RenderBox.GetDryLayout requires normalized constraints.");
        }

        if (!DebugCheckingIntrinsics
            && _layoutCacheStorage.DryLayouts.TryGetValue(constraints, out Size cached))
        {
            return cached;
        }

        Size result = constraints.Constrain(RunDryCalculation(() => ComputeDryLayout(constraints)));
        if (!DebugCheckingIntrinsics)
        {
            _layoutCacheStorage.DryLayouts[constraints] = result;
        }

        return result;
    }

    public double? GetDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (!constraints.IsNormalized)
        {
            throw new InvalidOperationException("RenderBox.GetDryBaseline requires normalized constraints.");
        }

        double? result = GetCachedBaseline(
            constraints,
            baseline,
            () => RunDryCalculation(() => ComputeDryBaseline(constraints, baseline)));

#if DEBUG
        double? verification = RunDryCalculation(() => ComputeDryBaseline(constraints, baseline));
        if (verification != result)
        {
            throw new InvalidOperationException(
                $"{GetType().Name}.ComputeDryBaseline returned inconsistent results for {constraints}.");
        }
#endif

        return result;
    }

    protected virtual double ComputeMinIntrinsicWidth(double height) => 0.0;

    protected virtual double ComputeMaxIntrinsicWidth(double height) => ComputeMinIntrinsicWidth(height);

    protected virtual double ComputeMinIntrinsicHeight(double width) => 0.0;

    protected virtual double ComputeMaxIntrinsicHeight(double width) => ComputeMinIntrinsicHeight(width);

    protected virtual Size ComputeDryLayout(BoxConstraints constraints) => constraints.Smallest;

    protected virtual double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => null;

    protected void DebugCannotComputeDryLayout(string? reason = null)
    {
        throw new InvalidOperationException(
            reason ?? $"{GetType().Name} cannot compute a dry layout result for the supplied constraints.");
    }

    public override void MarkNeedsLayout()
    {
        bool hadCachedLayoutData = _layoutCacheStorage.Clear();
        if (hadCachedLayoutData && Parent != null)
        {
            MarkParentNeedsLayout();
            return;
        }

        base.MarkNeedsLayout();
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (child.parentData is BoxParentData childParentData)
        {
            Point offset = childParentData.offset;
            transform.TranslateByDouble(offset.X, offset.Y, 0, 1);
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (!HasSize)
        {
            return false;
        }

        if (position.X < 0 || position.Y < 0 || position.X > Size.Width || position.Y > Size.Height)
        {
            return false;
        }

        if (HitTestChildren(result, position) || HitTestSelf(position))
        {
            result.Add(new BoxHitTestEntry(this, position));
            return true;
        }

        return false;
    }

    protected virtual bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return false;
    }

    protected virtual bool HitTestSelf(Point position)
    {
        return false;
    }


    /// Returns the distance from the y-coordinate of the position of the box to
    /// the y-coordinate of the first given baseline in the box's contents.
    ///
    /// Used by certain layout models to align adjacent boxes on a common
    /// baseline, regardless of padding, font size differences, etc. If there is
    /// no baseline, this function returns the distance from the y-coordinate of
    /// the position of the box to the y-coordinate of the bottom of the box
    /// (i.e., the height of the box) unless the caller passes true
    /// for `onlyReal`, in which case the function returns null.
    ///
    /// Only call this function after calling [layout] on this box. You
    /// are only allowed to call this from the parent of this box during
    /// that parent's [performLayout] or [paint] functions.
    ///
    /// When implementing a [RenderBox] subclass, to override the baseline
    /// computation, override [computeDistanceToActualBaseline].
    ///
    /// See also:
    ///
    ///  * [getDryBaseline], which returns the baseline location of this
    ///    [RenderBox] at a certain [BoxConstraints].
    public double? GetDistanceToBaseline(TextBaseline baseline, bool onlyReal = false)
    {
        // Debug.Assert(
        //     !_debugDoingBaseline,
        //     "Please see the documentation for computeDistanceToActualBaseline for the required calling conventions of this method.",
        // );
        // Debug.Assert(!debugNeedsLayout || RenderObject.debugCheckingIntrinsics);
        // Debug.Assert(
        //     RenderObject.debugCheckingIntrinsics ||
        //     Owner! switch 
        // {
        //     PipelineOwner(debugDoingLayout: true) =>
        //     RenderObject.debugActiveLayout == parent && parent!.debugDoingThisLayout,
        //     PipelineOwner(debugDoingPaint: true) =>
        //     RenderObject.debugActivePaint == parent && parent!.debugDoingThisPaint ||
        //         (RenderObject.debugActivePaint == this && debugDoingThisPaint),
        //     PipelineOwner () => false,
        // }
        // );

        //Debug.Assert(_debugSetDoingBaseline(true));

        double? result;

        try
        {
            result = GetCachedBaseline(
                Constraints,
                baseline,
                () => ComputeDistanceToActualBaseline(baseline));
        }
        finally
        {
            //Debug.Assert(_debugSetDoingBaseline(false));
        }

        if (result == null && !onlyReal)
        {
            return Size.Height;
        }

        return result;
    }

    /// Calls [computeDistanceToActualBaseline] and caches the result.
    ///
    /// This function must only be called from [getDistanceToBaseline] and
    /// [computeDistanceToActualBaseline]. Do not call this function directly from
    /// outside those two methods.
    protected virtual double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        // Debug.Assert(
        //     _debugDoingBaseline,
        //     'Please see the documentation for computeDistanceToActualBaseline for the required calling conventions of this method.',
        // );

        // return _computeIntrinsics(
        //     _CachedLayoutCalculation.baseline,
        //     (Constraints, baseline),
        //     ((BoxConstraints, TextBaseline) pair) =>
        //         BaselineOffset(computeDistanceToActualBaseline(pair.$2))
        // ).offset;

        return null;
    }

    private double GetIntrinsicDimension(
        IntrinsicDimension dimension,
        double argument,
        Func<double, double> computer)
    {
        if (double.IsNaN(argument) || argument < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(argument), "Intrinsic dimensions must be non-negative.");
        }

        var key = (dimension, argument);
        if (!DebugCheckingIntrinsics
            && _layoutCacheStorage.IntrinsicDimensions.TryGetValue(key, out double cached))
        {
            return cached;
        }

        double result = RunDryCalculation(() => computer(argument));
        if (!DebugCheckingIntrinsics)
        {
            _layoutCacheStorage.IntrinsicDimensions[key] = result;
        }

        return result;
    }

    private double? GetCachedBaseline(
        BoxConstraints constraints,
        TextBaseline baseline,
        Func<double?> computer)
    {
        Dictionary<BoxConstraints, BaselineOffset> cache = baseline switch
        {
            TextBaseline.Alphabetic => _layoutCacheStorage.AlphabeticBaselines,
            TextBaseline.Ideographic => _layoutCacheStorage.IdeographicBaselines,
            _ => throw new ArgumentOutOfRangeException(nameof(baseline), baseline, null)
        };

        if (!DebugCheckingIntrinsics && cache.TryGetValue(constraints, out BaselineOffset cached))
        {
            return cached.Offset;
        }

        double? result = computer();
        if (!DebugCheckingIntrinsics)
        {
            cache[constraints] = new BaselineOffset(result);
        }

        return result;
    }

    private T RunDryCalculation<T>(Func<T> computer)
    {
        RenderBox? previous = _activeDryCalculation;
        _activeDryCalculation = this;
        try
        {
            return computer();
        }
        finally
        {
            _activeDryCalculation = previous;
        }
    }

    private enum IntrinsicDimension
    {
        MinWidth,
        MaxWidth,
        MinHeight,
        MaxHeight
    }

    private readonly record struct BaselineOffset(double? Offset);

    private sealed class LayoutCacheStorage
    {
        public Dictionary<(IntrinsicDimension Dimension, double Argument), double> IntrinsicDimensions { get; } = [];

        public Dictionary<BoxConstraints, Size> DryLayouts { get; } = [];

        public Dictionary<BoxConstraints, BaselineOffset> AlphabeticBaselines { get; } = [];

        public Dictionary<BoxConstraints, BaselineOffset> IdeographicBaselines { get; } = [];

        public bool Clear()
        {
            bool hadCachedLayoutData = IntrinsicDimensions.Count > 0
                                       || DryLayouts.Count > 0
                                       || AlphabeticBaselines.Count > 0
                                       || IdeographicBaselines.Count > 0;
            IntrinsicDimensions.Clear();
            DryLayouts.Clear();
            AlphabeticBaselines.Clear();
            IdeographicBaselines.Clear();
            return hadCachedLayoutData;
        }
    }

    // private TOutput _computeIntrinsics<TInput, TOutput>(
    //     _CachedLayoutCalculation<TInput, TOutput> type,
    //     TInput input,
    //     Func<TInput, TOutput> computer
    // )
    //     where TInput : class
    // {
    //     // Debug.Assert(
    //     //     RenderObject.debugCheckingIntrinsics || !debugDoingThisResize,
    //     // ); // performResize should not depend on anything except the incoming constraints
    //
    //     bool shouldCache = true;
    //     Func<bool> a = () =>
    //     {
    //         // we don't want the debug-mode intrinsic tests to affect
    //         // who gets marked dirty, etc.
    //         //shouldCache = !RenderObject.debugCheckingIntrinsics;
    //         return true;
    //     };
    //
    //     Debug.Assert(a());
    //
    //     return shouldCache ? _computeWithTimeline(type, input, computer) : computer(input);
    // }

    // private class _LayoutCacheStorage
    // {
    //     Dictionary<(_IntrinsicDimension, double), double>? _cachedIntrinsicDimensions;
    //     Dictionary<BoxConstraints, Size>? _cachedDryLayoutSizes;
    //     Dictionary<BoxConstraints, BaselineOffset>? _cachedAlphabeticBaseline;
    //     Dictionary<BoxConstraints, BaselineOffset>? _cachedIdeoBaseline;
    //
    //     // Returns a boolean indicating whether the cache storage has cached
    //     // intrinsics / dry layout data in it.
    //     bool clear()
    //     {
    //         bool hasCache =
    //             (_cachedDryLayoutSizes?.isNotEmpty ?? false) ||
    //             (_cachedIntrinsicDimensions?.isNotEmpty ?? false) ||
    //             (_cachedAlphabeticBaseline?.isNotEmpty ?? false) ||
    //             (_cachedIdeoBaseline?.isNotEmpty ?? false);
    //
    //         if (hasCache)
    //         {
    //             _cachedDryLayoutSizes?.Clear();
    //             _cachedIntrinsicDimensions?.Clear();
    //             _cachedAlphabeticBaseline?.Clear();
    //             _cachedIdeoBaseline?.Clear();
    //         }
    //
    //         return hasCache;
    //     }
    // }

    // private struct BaselineOffset(double? offset)
    // {
    //     /// A value that indicates that the associated `RenderBox` does not have any
    //     /// baselines.
    //     ///
    //     /// [BaselineOffset.noBaseline] is an identity element in most binary
    //     /// operations involving two [BaselineOffset]s (such as [minOf]), for render
    //     /// objects with no baselines typically do not contribute to the baseline
    //     /// offset of their parents.
    //     static const BaselineOffset noBaseline = BaselineOffset(null);
    //
    //     /// Returns a new baseline location that is `offset` pixels further away from
    //     /// the origin than `this`, or unchanged if `this` is [noBaseline].
    //     BaselineOffset operator +(double offset)
    //     {
    //          double? value = this.offset;
    //         return BaselineOffset(value == null ? null : value + offset);
    //     }
    //
    //     /// Compares this [BaselineOffset] and `other`, and returns whichever is closer
    //     /// to the origin.
    //     ///
    //     /// When both `this` and `other` are [noBaseline], this method returns
    //     /// [noBaseline]. When one of them is [noBaseline], this method returns the
    //     /// other operand that's not [noBaseline].
    //     BaselineOffset minOf(BaselineOffset other)
    //     {
    //         return switch ((this, other))
    //         {
    //             (final double lhs ?, final double rhs ?) => lhs >= rhs ? other : this,
    //             (final double lhs ?, null) => BaselineOffset(lhs),
    //             (null, final BaselineOffset rhs) => rhs,
    //         }
    //
    //         ;
    //     }
    // }
    //
    // private abstract class _CachedLayoutCalculation<TInput, TOutput>
    // {
    //     public static _DryLayout dryLayout = new _DryLayout();
    //     public static _Baseline baseline = new _Baseline();
    //
    //     public abstract TOutput memoize(_LayoutCacheStorage cacheStorage, TInput input, Func<TInput, TOutput> computer);
    //
    //     // Debug information that will be used to generate the Timeline event for this type of calculation.
    //     public abstract Dictionary<String, String> debugFillTimelineArguments(
    //         Dictionary<String, String> timelineArguments,
    //         TInput input
    //     );
    //
    //     public abstract String eventLabel(RenderBox renderBox);
    // }
    //
    // private class _DryLayout : _CachedLayoutCalculation<BoxConstraints, Size>
    // {
    //     public override Size memoize(_LayoutCacheStorage cacheStorage, BoxConstraints input,
    //         Func<BoxConstraints, Size> computer)
    //     {
    //         return (cacheStorage._cachedDryLayoutSizes ??=  <BoxConstraints, Size >{
    //         }).putIfAbsent(
    //             input,
    //             () => computer(input)
    //         );
    //     }
    //
    //     public override Dictionary<string, string> debugFillTimelineArguments(
    //         Dictionary<string, string> timelineArguments, BoxConstraints input)
    //     {
    //         return timelineArguments.. ['getDryLayout constraints'] = '$input';
    //     }
    //
    //     public override string eventLabel(RenderBox renderBox)
    //         => '${renderBox.runtimeType}.getDryLayout';
    // }
    //
    // private class _Baseline : _CachedLayoutCalculation<(BoxConstraints, TextBaseline), BaselineOffset>
    // {
    //     public _Baseline()
    //     {
    //     }
    //
    //     public override BaselineOffset memoize(_LayoutCacheStorage cacheStorage, (BoxConstraints, TextBaseline) input,
    //         Func<(BoxConstraints, TextBaseline), BaselineOffset> computer)
    //     {
    //         Dictionary<BoxConstraints, BaselineOffset> cache = input.$2 switch ()
    //         {
    //             TextBaseline.Alphabetic =>
    //             cacheStorage._cachedAlphabeticBaseline ??=  <BoxConstraints, BaselineOffset >{
    //         },
    //             TextBaseline.Ideographic =>
    //             cacheStorage._cachedIdeoBaseline ??=  <BoxConstraints, BaselineOffset >{
    //         }
    //         }
    //
    //         ;
    //         BaselineOffset ifAbsent() => computer(input);
    //         return cache.putIfAbsent(input.$1, ifAbsent);
    //     }
    //
    //     public override Dictionary<string, string> debugFillTimelineArguments(
    //         Dictionary<string, string> timelineArguments, (BoxConstraints, TextBaseline) input)
    //     {
    //         return timelineArguments
    //             .. ['baseline type'] = '${input.$2}'
    //             .. ['constraints'] = '${input.$1}';
    //     }
    //
    //     public override string eventLabel(RenderBox renderBox)
    //         => '${renderBox.runtimeType}.getDryBaseline';
    // }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Size?>("size", _size, missingIfNull: true));
    }
}
