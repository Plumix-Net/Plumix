using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/rendering/box_constraints_test.dart

namespace Plumix.Tests;

/// <summary>
/// Ports <c>box_constraints_test.dart</c>, plus the <c>BoxConstraints</c> members of
/// <c>rendering/box.dart</c> that test does not reach (Euclidean <c>%</c>, <c>clampDouble</c>'s NaN
/// rule, the applied-constraint checks and the <c>NOT NORMALIZED</c> annotation).
/// </summary>
public sealed class BoxConstraintsTests
{
    private static readonly BoxConstraints Constraints = new(
        MinWidth: 3.0,
        MaxWidth: 7.0,
        MinHeight: 11.0,
        MaxHeight: 17.0);

    [Fact]
    public void BoxConstraints_ToString()
    {
        Assert.Contains("biggest", BoxConstraints.Expand().ToString(), StringComparison.Ordinal);
        Assert.Contains("unconstrained", BoxConstraints.Unbounded.ToString(), StringComparison.Ordinal);
        Assert.Contains("w=50", BoxConstraints.TightFor(width: 50.0).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BoxConstraints_ToString_SpellsEveryShapeTheWayDartDoes()
    {
        Assert.Equal("BoxConstraints(w=800.0, h=600.0)", BoxConstraints.Tight(new Size(800, 600)).ToString());
        Assert.Equal(
            "BoxConstraints(0.0<=w<=Infinity, h=100.0)",
            BoxConstraints.Unbounded.Tighten(height: 100.0).ToString());
        Assert.Equal("BoxConstraints(biggest)", BoxConstraints.Expand().ToString());
        Assert.Equal("BoxConstraints(unconstrained)", BoxConstraints.Unbounded.ToString());
        Assert.Equal(
            "BoxConstraints(3.0<=w<=2.0, 11.0<=h<=18.0; NOT NORMALIZED)",
            new BoxConstraints(3.0, 2.0, 11.0, 18.0).ToString());
    }

    [Fact]
    public void BoxConstraints_CopyWith()
    {
        BoxConstraints copy = Constraints.CopyWith();
        Assert.Equal(Constraints, copy);
        copy = Constraints.CopyWith(minWidth: 13.0, maxWidth: 17.0, minHeight: 111.0, maxHeight: 117.0);
        Assert.Equal(13.0, copy.MinWidth);
        Assert.Equal(17.0, copy.MaxWidth);
        Assert.Equal(111.0, copy.MinHeight);
        Assert.Equal(117.0, copy.MaxHeight);
        Assert.NotEqual(Constraints, copy);
        Assert.NotEqual(Constraints.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void BoxConstraints_Operators()
    {
        BoxConstraints copy = Constraints * 2.0;
        Assert.Equal(6.0, copy.MinWidth);
        Assert.Equal(14.0, copy.MaxWidth);
        Assert.Equal(22.0, copy.MinHeight);
        Assert.Equal(34.0, copy.MaxHeight);
        Assert.Equal(Constraints, copy / 2.0);
        copy = Constraints.TruncatingDivide(2.0);
        Assert.Equal(1.0, copy.MinWidth);
        Assert.Equal(3.0, copy.MaxWidth);
        Assert.Equal(5.0, copy.MinHeight);
        Assert.Equal(8.0, copy.MaxHeight);
        copy = Constraints % 3.0;
        Assert.Equal(0.0, copy.MinWidth);
        Assert.Equal(1.0, copy.MaxWidth);
        Assert.Equal(2.0, copy.MinHeight);
        Assert.Equal(2.0, copy.MaxHeight);
    }

    [Fact]
    public void BoxConstraints_Operators_FollowDartDoubleSemantics()
    {
        // Dart's `%` is Euclidean, unlike C#'s truncating remainder.
        BoxConstraints negative = new BoxConstraints(-1.0, 5.0, -4.0, 7.0) % 3.0;
        Assert.Equal(2.0, negative.MinWidth);
        Assert.Equal(2.0, negative.MaxWidth);
        Assert.Equal(2.0, negative.MinHeight);
        Assert.Equal(1.0, negative.MaxHeight);
        Assert.True(double.IsNaN((BoxConstraints.Unbounded % 3.0).MaxWidth));

        // `double ~/ double` truncates toward zero and throws on an infinite quotient.
        Assert.Equal(-1.0, new BoxConstraints(0.0, 3.0, 0.0, 3.0).TruncatingDivide(-2.0).MaxWidth);
        Assert.Throws<NotSupportedException>(() => BoxConstraints.Unbounded.TruncatingDivide(2.0));
    }

    [Fact]
    public void BoxConstraints_Lerp()
    {
        Assert.Null(BoxConstraints.Lerp(null, null, 0.5));
        BoxConstraints copy = BoxConstraints.Lerp(null, Constraints, 0.5)!.Value;
        AssertConstraints(copy, 1.5, 3.5, 5.5, 8.5);
        copy = BoxConstraints.Lerp(Constraints, null, 0.5)!.Value;
        AssertConstraints(copy, 1.5, 3.5, 5.5, 8.5);
        copy = BoxConstraints.Lerp(
            new BoxConstraints(MinWidth: 13.0, MaxWidth: 17.0, MinHeight: 111.0, MaxHeight: 117.0),
            Constraints,
            0.2)!.Value;
        AssertConstraints(copy, 11.0, 15.0, 91.0, 97.0);
    }

    [Fact]
    public void BoxConstraints_Lerp_IdenticalAB()
    {
        Assert.Null(BoxConstraints.Lerp(null, null, 0));
        BoxConstraints constraints = BoxConstraints.Unbounded;
        // Dart checks `identical(a, b)`; a value type has no identity, so equal operands short-cut.
        Assert.Equal(constraints, BoxConstraints.Lerp(constraints, constraints, 0.5));
    }

    [Fact]
    public void BoxConstraints_Lerp_WithUnboundedWidth()
    {
        var constraints1 = new BoxConstraints(MinWidth: double.PositiveInfinity, MinHeight: 10.0, MaxHeight: 20.0);
        var constraints2 = new BoxConstraints(MinWidth: double.PositiveInfinity, MinHeight: 20.0, MaxHeight: 30.0);
        var constraints3 = new BoxConstraints(MinWidth: double.PositiveInfinity, MinHeight: 15.0, MaxHeight: 25.0);
        Assert.Equal(constraints3, BoxConstraints.Lerp(constraints1, constraints2, 0.5));
    }

    [Fact]
    public void BoxConstraints_Lerp_WithUnboundedHeight()
    {
        var constraints1 = new BoxConstraints(MinWidth: 10.0, MaxWidth: 20.0, MinHeight: double.PositiveInfinity);
        var constraints2 = new BoxConstraints(MinWidth: 20.0, MaxWidth: 30.0, MinHeight: double.PositiveInfinity);
        var constraints3 = new BoxConstraints(MinWidth: 15.0, MaxWidth: 25.0, MinHeight: double.PositiveInfinity);
        Assert.Equal(constraints3, BoxConstraints.Lerp(constraints1, constraints2, 0.5));
    }

    [DebugOnlyFact]
    public void BoxConstraints_Lerp_FromBoundedToUnbounded()
    {
        var constraints1 = new BoxConstraints(
            MinWidth: double.PositiveInfinity,
            MinHeight: double.PositiveInfinity);
        var constraints2 = new BoxConstraints(MinWidth: 20.0, MaxWidth: 30.0, MinHeight: double.PositiveInfinity);
        var constraints3 = new BoxConstraints(MinWidth: double.PositiveInfinity, MinHeight: 20.0, MaxHeight: 30.0);
        AssertionError error = Assert.Throws<AssertionError>(
            () => BoxConstraints.Lerp(constraints1, constraints2, 0.5));
        Assert.Equal(
            "Cannot interpolate between finite constraints and unbounded constraints.",
            error.MessageObject);
        Assert.Throws<AssertionError>(() => BoxConstraints.Lerp(constraints1, constraints3, 0.5));
        Assert.Throws<AssertionError>(() => BoxConstraints.Lerp(constraints2, constraints3, 0.5));
    }

    [Fact]
    public void BoxConstraints_Normalize()
    {
        var constraints = new BoxConstraints(MinWidth: 3.0, MaxWidth: 2.0, MinHeight: 11.0, MaxHeight: 18.0);
        BoxConstraints copy = constraints.Normalize();
        AssertConstraints(copy, 3.0, 3.0, 11.0, 18.0);

        // A negative (or NaN) minimum becomes zero; an already normalized value is returned as is.
        AssertConstraints(new BoxConstraints(-5.0, -10.0, double.NaN, 4.0).Normalize(), 0.0, 0.0, 0.0, 4.0);
        Assert.Equal(Constraints, Constraints.Normalize());
    }

    [Fact]
    public void BoxConstraints_FromViewConstraints()
    {
        BoxConstraints unconstrained = BoxConstraints.FromViewConstraints(ViewConstraints.Unbounded);
        Assert.Equal(BoxConstraints.Unbounded, unconstrained);

        BoxConstraints constraints = BoxConstraints.FromViewConstraints(
            new ViewConstraints(MinWidth: 1, MaxWidth: 2, MinHeight: 3, MaxHeight: 4));
        Assert.Equal(new BoxConstraints(MinWidth: 1, MaxWidth: 2, MinHeight: 3, MaxHeight: 4), constraints);
    }

    [Fact]
    public void BoxConstraints_ConstrainSizeAndAttemptToPreserveAspectRatio_CanHandleEmptySize()
    {
        var constraints = new BoxConstraints(MinWidth: 10.0, MaxWidth: 20.0, MinHeight: 10.0, MaxHeight: 20.0);
        var unconstrainedSize = new Size(15.0, 0.0);
        Size constrainedSize = constraints.ConstrainSizeAndAttemptToPreserveAspectRatio(unconstrainedSize);
        Assert.Equal(new Size(15.0, 10.0), constrainedSize);
    }

    [Fact]
    public void BoxConstraints_ClampLikeDartClampDouble()
    {
        // `clampDouble` sends NaN to the maximum, where `Math.Clamp` would keep it.
        Assert.Equal(7.0, Constraints.ConstrainWidth(double.NaN));
        Assert.Equal(new Size(7.0, 11.0), Constraints.ConstrainDimensions(100.0, 0.0));
        Assert.Equal(
            new BoxConstraints(5.0, 5.0, 11.0, 17.0),
            Constraints.Enforce(new BoxConstraints(5.0, 5.0, 0.0, 100.0)));
    }

    [Fact]
    public void BoxConstraints_BoundedAndInfinitePredicates_MatchDartComparisons()
    {
        // Dart compares with `< double.infinity`, so a negative infinity counts as bounded.
        var odd = new BoxConstraints(0.0, double.NegativeInfinity, 0.0, double.NegativeInfinity);
        Assert.True(odd.HasBoundedWidth);
        Assert.True(odd.HasBoundedHeight);
        Assert.False(BoxConstraints.Unbounded.HasBoundedWidth);
        Assert.True(BoxConstraints.Expand().HasInfiniteWidth);
        Assert.True(BoxConstraints.Expand().HasInfiniteHeight);

        // `tightForFinite` only treats positive infinity as "not given".
        Assert.Equal(BoxConstraints.TightFor(height: 5.0), BoxConstraints.TightForFinite(height: 5.0));
    }

    [Fact]
    public void BoxConstraints_Deflate_KeepsTheMaximumAtLeastTheMinimum()
    {
        BoxConstraints deflated = new BoxConstraints(10.0, 30.0, 5.0, double.PositiveInfinity)
            .Deflate(new EdgeInsets(10.0, 3.0, 15.0, 4.0));
        AssertConstraints(deflated, 0.0, 5.0, 0.0, double.PositiveInfinity);
        Assert.Equal(new BoxConstraints(0.0, 7.0, 0.0, 17.0), Constraints.Loosen());
    }

    [DebugOnlyFact]
    public void BoxConstraints_DebugAssertIsValid_ReportsAppliedInfiniteMinimums()
    {
        FlutterError both = Assert.Throws<FlutterError>(() =>
            BoxConstraints.Expand().DebugAssertIsValid(isAppliedConstraint: true));
        Assert.StartsWith("BoxConstraints forces an infinite width and infinite height.", both.Message);
        Assert.Contains("The offending constraints were:", both.ToStringDeep(), StringComparison.Ordinal);

        FlutterError height = Assert.Throws<FlutterError>(() =>
            BoxConstraints.Expand(width: 10).DebugAssertIsValid(isAppliedConstraint: true));
        Assert.StartsWith("BoxConstraints forces an infinite height.", height.Message);

        // Not applied: infinite minimums are only non-normalized when the maximum is smaller.
        Assert.True(BoxConstraints.Expand().DebugAssertIsValid());
    }

    [DebugOnlyFact]
    public void BoxConstraints_EqualityAssertsBothOperandsAreValid()
    {
        var invalid = new BoxConstraints(MinWidth: -1.0);
        Assert.Throws<FlutterError>(() => invalid.Equals(Constraints));
        Assert.Throws<FlutterError>(() => Constraints.Equals(invalid));
        Assert.Throws<FlutterError>(() => invalid.GetHashCode());
    }

    private static void AssertConstraints(
        BoxConstraints constraints,
        double minWidth,
        double maxWidth,
        double minHeight,
        double maxHeight)
    {
        Assert.Equal(minWidth, constraints.MinWidth, 10);
        Assert.Equal(maxWidth, constraints.MaxWidth, 10);
        Assert.Equal(minHeight, constraints.MinHeight, 10);
        Assert.Equal(maxHeight, constraints.MaxHeight, 10);
    }
}
