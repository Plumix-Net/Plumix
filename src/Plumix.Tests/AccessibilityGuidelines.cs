using System.Text;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit.Sdk;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter_test/lib/src/accessibility.dart (Evaluation,
// AccessibilityGuideline, MinimumTapTargetGuideline, androidTapTargetGuideline, iOSTapTargetGuideline,
// meetsGuideline)

/// <summary>flutter_test's <c>Evaluation</c>: the result of evaluating a semantics node by a guideline.</summary>
internal sealed class Evaluation
{
    private Evaluation(bool passed, string? reason)
    {
        Passed = passed;
        Reason = reason;
    }

    /// <summary>Dart's <c>Evaluation.pass()</c>.</summary>
    public static Evaluation Pass() => new(true, null);

    /// <summary>Dart's <c>Evaluation.fail([reason])</c>.</summary>
    public static Evaluation Fail(string? reason = null) => new(false, reason);

    public bool Passed { get; }

    public string? Reason { get; }

    public static Evaluation operator +(Evaluation self, Evaluation? other)
    {
        if (other is null)
        {
            return self;
        }

        var buffer = new StringBuilder();
        if (!string.IsNullOrEmpty(self.Reason))
        {
            buffer.Append(self.Reason);
            buffer.Append('\n');
        }

        if (!string.IsNullOrEmpty(other.Reason))
        {
            buffer.Append(other.Reason);
        }

        return new Evaluation(self.Passed && other.Passed, buffer.Length == 0 ? null : buffer.ToString());
    }
}

/// <summary>flutter_test's <c>AccessibilityGuideline</c>.</summary>
internal abstract class AccessibilityGuideline
{
    public abstract Evaluation Evaluate(FrameworkDartTester tester);

    public abstract string Description { get; }

    /// <summary>Dart's <c>androidTapTargetGuideline</c>.</summary>
    public static readonly AccessibilityGuideline AndroidTapTargetGuideline = new MinimumTapTargetGuideline(
        size: new Size(48.0, 48.0),
        link: "https://support.google.com/accessibility/android/answer/7101858?hl=en");

    /// <summary>Dart's <c>iOSTapTargetGuideline</c>.</summary>
    public static readonly AccessibilityGuideline IOSTapTargetGuideline = new MinimumTapTargetGuideline(
        size: new Size(44.0, 44.0),
        link: "https://developer.apple.com/design/human-interface-guidelines/ios/visual-design/"
            + "adaptivity-and-layout/");

    /// <summary>Dart's <c>expect(tester, meetsGuideline(guideline))</c>.</summary>
    public static void ExpectMeetsGuideline(FrameworkDartTester tester, AccessibilityGuideline guideline)
    {
        Evaluation result = guideline.Evaluate(tester);
        if (!result.Passed)
        {
            throw new XunitException(result.Reason);
        }
    }
}

/// <summary>flutter_test's <c>MinimumTapTargetGuideline</c>.</summary>
internal sealed class MinimumTapTargetGuideline(Size size, string link) : AccessibilityGuideline
{
    // The gap between targets to their parent scrollables to be consider as valid tap targets.
    private const double MinimumGapToBoundary = 0.001;

    /// <summary>The minimum allowed size of a tappable node.</summary>
    public Size Size { get; } = size;

    /// <summary>A link describing the tap target guidelines for a platform.</summary>
    public string Link { get; } = link;

    public override string Description => $"Tappable objects should be at least {Size}";

    public override Evaluation Evaluate(FrameworkDartTester tester)
    {
        Evaluation result = Evaluation.Pass();
        SemanticsNode? root = tester.SemanticsRootNode;
        if (root is not null)
        {
            result += Traverse(tester.View, root);
        }

        return result;
    }

    private Evaluation Traverse(TestFlutterView view, SemanticsNode node)
    {
        Evaluation result = Evaluation.Pass();
        foreach (SemanticsNode child in node.Children)
        {
            result += Traverse(view, child);
        }

        if (node.IsMergedIntoParent)
        {
            return result;
        }

        if (ShouldSkipNode(node))
        {
            return result;
        }

        Rect paintBounds = node.Rect;
        SemanticsNode? current = node;
        while (current is not null)
        {
            Matrix4? transform = current.Transform;
            if (transform is { } value)
            {
                paintBounds = MatrixUtils.TransformRect(value, paintBounds);
            }

            // Skip node if it is touching the edge scrollable, since it might be partially scrolled
            // offscreen.
            if (current.Flags.HasFlag(SemanticsFlags.HasImplicitScrolling) && IsAtBoundary(paintBounds, current.Rect))
            {
                return result;
            }

            current = current.Parent;
        }

        var viewRect = new Rect(new Point(0, 0), view.PhysicalSize);
        if (IsAtBoundary(paintBounds, viewRect))
        {
            return result;
        }

        // Shrink by device pixel ratio.
        var candidateSize = new Size(
            paintBounds.Width / view.DevicePixelRatio,
            paintBounds.Height / view.DevicePixelRatio);
        if (candidateSize.Width < Size.Width - Constants.PrecisionErrorTolerance
            || candidateSize.Height < Size.Height - Constants.PrecisionErrorTolerance)
        {
            result += Evaluation.Fail(
                $"{node}: expected tap target size of at least {Size}, but found {candidateSize}\n"
                + $"See also: {Link}");
        }

        return result;
    }

    private static bool IsAtBoundary(Rect child, Rect parent)
    {
        if (child.Left - parent.Left > MinimumGapToBoundary
            && parent.Right - child.Right > MinimumGapToBoundary
            && child.Top - parent.Top > MinimumGapToBoundary
            && parent.Bottom - child.Bottom > MinimumGapToBoundary)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns whether <paramref name="node"/> should be skipped for the minimum tap target guideline:
    /// links, hidden nodes and nodes without a tap or long-press action.
    /// </summary>
    public static bool ShouldSkipNode(SemanticsNode node)
    {
        SemanticsData data = node.GetSemanticsData();
        // Skip node if it has no actions, or is marked as hidden.
        if ((!data.HasAction(SemanticsActions.LongPress) && !data.HasAction(SemanticsActions.Tap))
            || data.Flags.HasFlag(SemanticsFlags.IsHidden))
        {
            return true;
        }

        // Skip links https://www.w3.org/WAI/WCAG21/Understanding/target-size.html
        if (data.Flags.HasFlag(SemanticsFlags.IsLink))
        {
            return true;
        }

        return false;
    }
}
