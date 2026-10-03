using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// C#-only test infrastructure: flutter_test's `paints` matcher (flutter_test/lib/src/mock_canvas.dart)
// and `TestRecordingPaintingContext` (flutter_test/lib/src/recording_canvas.dart), over the debug call
// list Plumix's `Canvas` records. Only the predicates the ported tests use are implemented.

/// <summary>
/// flutter_test's <c>TestRecordingPaintingContext</c>: children and pushed layers paint inline into one
/// recording canvas, and clips go straight to that canvas.
/// </summary>
internal sealed class TestRecordingPaintingContext() : PaintingContext(new OffsetLayer(), default)
{
    private readonly Canvas _canvas = new(new PictureRecorder());

    public override Canvas Canvas => _canvas;

    /// <summary>The recorded calls, in order.</summary>
    public IReadOnlyList<CanvasCall> Calls => _canvas.DebugCalls;

    public override void PaintChild(RenderObject child, Point offset) => child.Paint(this, offset);

    public override void PushLayer(
        ContainerLayer childLayer,
        PaintingContextCallback painter,
        Point offset,
        Rect? childPaintBounds = null)
    {
        painter(this, offset);
    }

    public override ClipRectLayer? PushClipRect(
        bool needsCompositing,
        Point offset,
        Rect clipRect,
        PaintingContextCallback painter,
        Clip clipBehavior = Clip.HardEdge,
        ClipRectLayer? oldLayer = null)
    {
        Rect shifted = clipRect.Translate(new Vector(offset.X, offset.Y));
        ClipRectAndPaint(shifted, clipBehavior, shifted, () => painter(this, offset));
        return null;
    }
}

/// <summary>Records what a render object paints, the way flutter_test's <c>paints</c> matcher does.</summary>
internal static class PaintRecording
{
    /// <summary>Paints <paramref name="renderObject"/> at the origin into a recording context.</summary>
    public static IReadOnlyList<CanvasCall> Record(RenderObject renderObject)
    {
        ArgumentNullException.ThrowIfNull(renderObject);
        var context = new TestRecordingPaintingContext();
        renderObject.Paint(context, default);
        return context.Calls;
    }

    /// <summary>flutter_test's <c>paintsExactlyCountTimes(#method, count)</c> counterpart.</summary>
    public static int CountCalls(this IReadOnlyList<CanvasCall> calls, string method) =>
        calls.Count(call => call.Method == method);
}

/// <summary>
/// flutter_test's <c>PaintPattern</c>: each step matches the next recorded call of its kind, which must
/// then carry the step's arguments; calls of other kinds in between are skipped.
/// </summary>
internal sealed class PaintPattern
{
    private readonly List<Step> _steps = [];

    /// <summary>flutter_test's <c>paints</c>: a new, empty pattern.</summary>
    public static PaintPattern Paints => new();

    public PaintPattern Rect(
        Rect? rect = null,
        Color? color = null,
        PaintingStyle? style = null,
        double? strokeWidth = null,
        bool? hasMaskFilter = null) => Add(
        "drawRect",
        call => Check(call, color, style, strokeWidth)
                ?? (rect is { } expected && call.Rect != expected ? $"rect {call.Rect} != {expected}" : null)
                ?? (hasMaskFilter is { } mask && (call.MaskFilter is not null) != mask
                    ? $"hasMaskFilter {call.MaskFilter is not null} != {mask}"
                    : null));

    public PaintPattern RRect(
        RRect? rrect = null,
        Color? color = null,
        PaintingStyle? style = null,
        double? strokeWidth = null) => Add(
        "drawRRect",
        call => Check(call, color, style, strokeWidth)
                ?? (rrect is { } expected && !RRectNearlyEquals(call.RRect, expected)
                    ? $"rrect {call.RRect} != {expected}"
                    : null));

    // mock_canvas.dart `_RRectPaintPredicate`: edges and radii compared within 0.0001.
    private static bool RRectNearlyEquals(RRect? actual, RRect expected)
    {
        const double eps = .0001;
        if (actual is not { } a)
        {
            return false;
        }

        static bool Near(double x, double y) => Math.Abs(x - y) <= eps;
        return Near(a.Left, expected.Left) && Near(a.Right, expected.Right)
               && Near(a.Top, expected.Top) && Near(a.Bottom, expected.Bottom)
               && Near(a.BottomLeft.X, expected.BottomLeft.X) && Near(a.BottomLeft.Y, expected.BottomLeft.Y)
               && Near(a.BottomRight.X, expected.BottomRight.X) && Near(a.BottomRight.Y, expected.BottomRight.Y)
               && Near(a.TopLeft.X, expected.TopLeft.X) && Near(a.TopLeft.Y, expected.TopLeft.Y)
               && Near(a.TopRight.X, expected.TopRight.X) && Near(a.TopRight.Y, expected.TopRight.Y);
    }

    public PaintPattern Circle(
        double? x = null,
        double? y = null,
        double? radius = null,
        Color? color = null,
        PaintingStyle? style = null,
        double? strokeWidth = null) => Add(
        "drawCircle",
        call => Check(call, color, style, strokeWidth)
                ?? (x is { } ex && call.Center!.Value.X != ex ? $"x {call.Center!.Value.X} != {ex}" : null)
                ?? (y is { } ey && call.Center!.Value.Y != ey ? $"y {call.Center!.Value.Y} != {ey}" : null)
                ?? (radius is { } er && call.Radius != er ? $"radius {call.Radius} != {er}" : null));

    public PaintPattern Path(
        Color? color = null,
        IEnumerable<Point>? includes = null,
        IEnumerable<Point>? excludes = null,
        PaintingStyle? style = null,
        double? strokeWidth = null) => Add(
        "drawPath",
        call => Check(call, color, style, strokeWidth) ?? CheckPath(call.Path, includes, excludes));

    public PaintPattern Line(
        Point? p1 = null,
        Point? p2 = null,
        Color? color = null,
        double? strokeWidth = null) => Add(
        "drawLine",
        call => Check(call, color, null, strokeWidth)
                ?? (p1 is { } start && call.Offset != start ? $"p1 {call.Offset} != {start}" : null)
                ?? (p2 is { } end && call.EndOffset != end ? $"p2 {call.EndOffset} != {end}" : null));

    public PaintPattern Paragraph(Point? offset = null) => Add(
        "drawParagraph",
        call => offset is { } expected && call.Offset != expected ? $"offset {call.Offset} != {expected}" : null);

    public PaintPattern Shadow(Color? color = null, double? elevation = null) => Add(
        "drawShadow",
        call => (color is { } expected && !ColorsMatch(call.Color, expected)
                    ? $"color {call.Color} != {expected}"
                    : null)
                ?? (elevation is { } e && call.Elevation != e ? $"elevation {call.Elevation} != {e}" : null));

    public PaintPattern Save() => Add("save", _ => null);

    public PaintPattern Restore() => Add("restore", _ => null);

    public PaintPattern Translate(double? x = null, double? y = null) => Add(
        "translate",
        call => (x is { } ex && call.Dx != ex ? $"dx {call.Dx} != {ex}" : null)
                ?? (y is { } ey && call.Dy != ey ? $"dy {call.Dy} != {ey}" : null));

    public PaintPattern Scale(double? x = null, double? y = null) => Add(
        "scale",
        call => (x is { } ex && call.Dx != ex ? $"sx {call.Dx} != {ex}" : null)
                ?? (y is { } ey && call.Dy != ey ? $"sy {call.Dy} != {ey}" : null));

    public PaintPattern Rotate(double? angle = null) => Add(
        "rotate",
        call => angle is { } expected && call.Radius != expected ? $"angle {call.Radius} != {expected}" : null);

    public PaintPattern ClipRect(Rect? rect = null) => Add(
        "clipRect",
        call => rect is { } expected && call.Rect != expected ? $"rect {call.Rect} != {expected}" : null);

    public PaintPattern ClipRRect(RRect? rrect = null) => Add(
        "clipRRect",
        call => rrect is { } expected && call.RRect != expected ? $"rrect {call.RRect} != {expected}" : null);

    public PaintPattern ClipPath() => Add("clipPath", _ => null);

    /// <summary>
    /// <c>..clipPath(pathMatcher: coversSameAreaAs(expected, areaToCompare: ..., sampleSize: ...))</c>.
    /// </summary>
    public PaintPattern ClipPath(Plumix.UI.Path coversSameAreaAs, Rect areaToCompare, int sampleSize = 20) => Add(
        "clipPath",
        call => call.Path is null
            ? "the call recorded no path"
            : PathMatchers.CoversSameAreaAs(call.Path, coversSameAreaAs, areaToCompare, sampleSize));

    /// <summary>
    /// mock_canvas.dart's <c>..something(predicate)</c>: skips calls until <paramref name="predicate"/>
    /// returns true for one; a predicate that throws fails the match with its message.
    /// </summary>
    public PaintPattern Something(Func<CanvasCall, bool> predicate)
    {
        _steps.Add(new Step("something", null, (calls, index) =>
        {
            while (index < calls.Count)
            {
                CanvasCall call = calls[index];
                index++;
                string? failure = RunPredicate(predicate, call, out bool matched);
                if (failure is not null)
                {
                    return (index, $"a \"something\" step considered {call.Method} incorrect: {failure}");
                }

                if (matched)
                {
                    return (index, null);
                }
            }

            return (index, "none of the painted methods satisfied the \"something\" step");
        }));
        return this;
    }

    /// <summary>
    /// mock_canvas.dart's <c>..everything(predicate)</c>: every remaining call must satisfy
    /// <paramref name="predicate"/>.
    /// </summary>
    public PaintPattern Everything(Func<CanvasCall, bool> predicate)
    {
        _steps.Add(new Step("everything", null, (calls, index) =>
        {
            if (index >= calls.Count)
            {
                return (index, "no calls were left for the \"everything\" step");
            }

            while (index < calls.Count)
            {
                CanvasCall call = calls[index];
                index++;
                string? failure = RunPredicate(predicate, call, out bool matched);
                if (failure is not null || !matched)
                {
                    return (index, $"an \"everything\" step considered {call.Method} incorrect: {failure}");
                }
            }

            return (index, null);
        }));
        return this;
    }

    private static string? RunPredicate(Func<CanvasCall, bool> predicate, CanvasCall call, out bool matched)
    {
        try
        {
            matched = predicate(call);
            return null;
        }
        catch (Exception exception)
        {
            matched = false;
            return exception.Message;
        }
    }

    /// <summary>Whether <paramref name="calls"/> match; <c>isNot(paints..)</c> is its negation.</summary>
    public bool Matches(IReadOnlyList<CanvasCall> calls) => Describe(calls) is null;

    /// <summary>Why <paramref name="calls"/> do not match, or null when they do.</summary>
    public string? Describe(IReadOnlyList<CanvasCall> calls)
    {
        if (calls.Count == 0)
        {
            return "painted nothing";
        }

        int index = 0;
        for (int stepIndex = 0; stepIndex < _steps.Count; stepIndex++)
        {
            Step step = _steps[stepIndex];
            if (step.Match is not null)
            {
                (index, string? matchFailure) = step.Match(calls, index);
                if (matchFailure is not null)
                {
                    return $"step {stepIndex} ({step.Method}): {matchFailure}";
                }

                continue;
            }

            while (index < calls.Count && calls[index].Method != step.Method)
            {
                index++;
            }

            if (index >= calls.Count)
            {
                return $"step {stepIndex} ({step.Method}): no matching call; painted "
                       + string.Join(", ", calls.Select(call => call.Method));
            }

            if (step.Check!(calls[index]) is { } failure)
            {
                return $"step {stepIndex} ({step.Method}) at call {index}: {failure}";
            }

            index++;
        }

        return null;
    }

    private PaintPattern Add(string method, Func<CanvasCall, string?> check)
    {
        _steps.Add(new Step(method, check, null));
        return this;
    }

    // mock_canvas.dart's `_colorsMatch`: same colour space, every channel within 1/255.
    private static bool ColorsMatch(Color? actual, Color expected)
    {
        const double limit = 1.0 / 255.0;
        return actual is not null
               && actual.ColorSpace == expected.ColorSpace
               && Math.Abs(actual.A - expected.A) < limit
               && Math.Abs(actual.R - expected.R) < limit
               && Math.Abs(actual.G - expected.G) < limit
               && Math.Abs(actual.B - expected.B) < limit;
    }

    private static string? Check(CanvasCall call, Color? color, PaintingStyle? style, double? strokeWidth)
    {
        if (color is { } expectedColor && !ColorsMatch(call.Color, expectedColor))
        {
            return $"color {call.Color} != {expectedColor}";
        }

        if (style is { } expectedStyle && call.Style != expectedStyle)
        {
            return $"style {call.Style} != {expectedStyle}";
        }

        if (strokeWidth is { } expectedWidth && call.StrokeWidth != expectedWidth)
        {
            return $"strokeWidth {call.StrokeWidth} != {expectedWidth}";
        }

        return null;
    }

    private static string? CheckPath(Plumix.UI.Path? path, IEnumerable<Point>? includes, IEnumerable<Point>? excludes)
    {
        if (path is null)
        {
            return includes is null && excludes is null ? null : "the call recorded no path";
        }

        foreach (Point point in includes ?? [])
        {
            if (!path.Contains(point))
            {
                return $"path does not contain {point}";
            }
        }

        foreach (Point point in excludes ?? [])
        {
            if (path.Contains(point))
            {
                return $"path unexpectedly contains {point}";
            }
        }

        return null;
    }

    private readonly record struct Step(
        string Method,
        Func<CanvasCall, string?>? Check,
        Func<IReadOnlyList<CanvasCall>, int, (int Index, string? Failure)>? Match);
}

/// <summary>xUnit assertions over <see cref="PaintPattern"/>.</summary>
internal static class PaintAssert
{
    /// <summary><c>expect(renderObject, paints..)</c>.</summary>
    public static void Paints(RenderObject renderObject, PaintPattern pattern) =>
        Paints(PaintRecording.Record(renderObject), pattern);

    /// <summary>
    /// <c>expect(finder, paints..)</c>: what the single match's render object paints (live tester).
    /// </summary>
    public static void Paints(Finder finder, PaintPattern pattern) =>
        Paints(FrameworkDartTester.RequireCurrent().RecordPaint(finder), pattern);

    /// <summary><c>expect(finder, isNot(paints..))</c>.</summary>
    public static void DoesNotPaint(Finder finder, PaintPattern pattern) =>
        DoesNotPaint(FrameworkDartTester.RequireCurrent().RenderObject<RenderObject>(finder), pattern);

    public static void Paints(IReadOnlyList<CanvasCall> calls, PaintPattern pattern)
    {
        if (pattern.Describe(calls) is { } failure)
        {
            Assert.Fail(failure);
        }
    }

    /// <summary>
    /// <c>expect(renderObject, paintsNothing)</c>: nothing but <c>save</c>/<c>restore</c> was recorded.
    /// </summary>
    public static void PaintsNothing(RenderObject renderObject)
    {
        List<string> painting = PaintRecording.Record(renderObject)
            .Where(call => call.Method is not ("save" or "restore"))
            .Select(call => call.Method)
            .ToList();
        Assert.True(painting.Count == 0, "painted something: " + string.Join(", ", painting));
    }

    /// <summary><c>expect(renderObject, isNot(paints..))</c>.</summary>
    public static void DoesNotPaint(RenderObject renderObject, PaintPattern pattern) =>
        Assert.False(pattern.Matches(PaintRecording.Record(renderObject)), "the paint pattern unexpectedly matched");
}

/// <summary>flutter_test's path matchers (matchers.dart).</summary>
internal static class PathMatchers
{
    /// <summary>
    /// matchers.dart's <c>coversSameAreaAs</c>: samples a <paramref name="sampleSize"/>-square grid (and
    /// a jittered point next to each grid point) and requires both paths to agree on containment.
    /// </summary>
    /// <remarks>
    /// As in Dart, the grid starts at the origin and spans <paramref name="areaToCompare"/>'s size; the
    /// jitter comes from a seeded <see cref="Random"/>, not Dart's <c>Random(1)</c>.
    /// </remarks>
    public static string? CoversSameAreaAs(
        Plumix.UI.Path actual,
        Plumix.UI.Path expected,
        Rect areaToCompare,
        int sampleSize = 20)
    {
        double stepX = areaToCompare.Width / sampleSize;
        double stepY = areaToCompare.Height / sampleSize;
        var random = new Random(1);
        for (int i = 0; i < sampleSize; i += 1)
        {
            for (int j = 0; j < sampleSize; j += 1)
            {
                var offset = new Point(i * stepX, j * stepY);
                if (Sample(offset) is { } failure)
                {
                    return failure;
                }

                var noise = new Vector(stepX * random.NextDouble(), stepY * random.NextDouble());
                if (Sample(offset + noise) is { } noisyFailure)
                {
                    return noisyFailure;
                }
            }
        }

        return null;

        string? Sample(Point point) => expected.Contains(point) == actual.Contains(point)
            ? null
            : $"expected path {(expected.Contains(point) ? "contains" : "does not contain")} {point}, actual "
              + $"{(actual.Contains(point) ? "does" : "does not")}";
    }
}
