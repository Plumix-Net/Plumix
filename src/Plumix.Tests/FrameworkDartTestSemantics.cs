using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure: the tree matcher from widgets/semantics_tester.dart.
// Carries the properties exercised by the scroll and slider suites; geometry/id ignore options
// are explicit at each call site, as in Flutter's hasSemantics.

namespace Plumix.Tests;

internal sealed class TestSemantics(
    int? id = null,
    SemanticsFlags flags = SemanticsFlags.None,
    SemanticsActions actions = SemanticsActions.None,
    string label = "",
    string value = "",
    string increasedValue = "",
    string decreasedValue = "",
    string hint = "",
    string tooltip = "",
    TextDirection? textDirection = null,
    Rect? rect = null,
    Matrix4? transform = null,
    TextSelection? textSelection = null,
    int? scrollIndex = null,
    int? scrollChildren = null,
    int? headingLevel = null,
    SemanticsRole role = SemanticsRole.None,
    SemanticsValidationResult validationResult = SemanticsValidationResult.None,
    SemanticsInputType inputType = SemanticsInputType.None,
    int? maxValueLength = null,
    int? currentValueLength = null,
    Uri? linkUrl = null,
    IReadOnlyList<TestSemantics>? children = null)
{
    public static readonly Rect FullScreen = new(0, 0, 800, 600);

    public static TestSemantics Root(IReadOnlyList<TestSemantics> children) =>
        new(id: 0, rect: new Rect(0, 0, 2400, 1800), children: children);

    public static TestSemantics RootChild(IReadOnlyList<TestSemantics> children) =>
        new(transform: Matrix4.Diagonal3Values(3, 3, 1), children: children);

    internal string? Match(
        SemanticsNode node,
        string path,
        bool ignoreRect,
        bool ignoreTransform,
        bool ignoreId,
        DebugSemanticsDumpOrder childOrder)
    {
        SemanticsData data = node.GetSemanticsData();
        string? Compare<T>(string name, T expected, T actual) => EqualityComparer<T>.Default.Equals(expected, actual)
            ? null
            : $"Node #{node.Id} at {path}: {name}: expected {expected}, found {actual}";

        string? failure = ignoreId ? null : Compare("id", id, (int?)node.Id);
        failure ??= Compare("flags", flags, data.Flags);
        failure ??= Compare("actions", actions, data.Actions);
        failure ??= Compare("label", label, data.Label);
        failure ??= Compare("value", value, data.Value);
        failure ??= Compare("increasedValue", increasedValue, data.IncreasedValue);
        failure ??= Compare("decreasedValue", decreasedValue, data.DecreasedValue);
        failure ??= Compare("hint", hint, data.Hint);
        failure ??= Compare("tooltip", tooltip, data.Tooltip);
        if (textDirection is not null)
        {
            failure ??= Compare("textDirection", textDirection, data.TextDirection);
        }

        if ((data.Label.Length > 0 || data.Value.Length > 0 || data.Hint.Length > 0
             || data.IncreasedValue.Length > 0 || data.DecreasedValue.Length > 0)
            && data.TextDirection is null)
        {
            failure ??= $"Node #{node.Id} at {path}: text with no textDirection";
        }

        if (!ignoreRect)
        {
            failure ??= Compare("rect", rect, (Rect?)node.Rect);
        }

        if (!ignoreTransform)
        {
            failure ??= Compare("transform", transform, node.Transform);
        }

        failure ??= Compare("selection base", textSelection?.BaseOffset, data.TextSelection?.BaseOffset);
        failure ??= Compare("selection extent", textSelection?.ExtentOffset, data.TextSelection?.ExtentOffset);
        if (scrollIndex is not null)
        {
            failure ??= Compare("scrollIndex", scrollIndex, data.ScrollIndex);
        }

        if (scrollChildren is not null)
        {
            failure ??= Compare("scrollChildren", scrollChildren, data.ScrollChildCount);
        }

        if (headingLevel is not null)
        {
            failure ??= Compare("headingLevel", headingLevel, (int?)data.HeadingLevel);
        }

        failure ??= Compare("role", role, data.Role);
        failure ??= Compare("validationResult", validationResult, data.ValidationResult);
        failure ??= Compare("inputType", inputType, data.InputType);
        failure ??= Compare("linkUrl", linkUrl?.ToString(), data.LinkUrl?.ToString());
        failure ??= Compare("maxValueLength", maxValueLength, data.MaxValueLength);
        failure ??= Compare("currentValueLength", currentValueLength, data.CurrentValueLength);
        IReadOnlyList<TestSemantics> expectedChildren = children ?? [];
        IReadOnlyList<SemanticsNode> actualChildren = node.MergeAllDescendantsIntoThisNode
            ? []
            : node.DebugListChildrenInOrder(childOrder);
        failure ??= Compare("child count", expectedChildren.Count, actualChildren.Count);
        if (failure is not null)
        {
            return failure;
        }

        for (int i = 0; i < expectedChildren.Count; i++)
        {
            failure = expectedChildren[i].Match(
                actualChildren[i], $"{path}/{i}", ignoreRect, ignoreTransform, ignoreId, childOrder);
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }
}
