using System.Text.RegularExpressions;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit.Sdk;

// C#-only test infrastructure: flutter_test's finders.dart (`find.*`, `Finder`, `.first`/`.last`/`.at`),
// the `findsOneWidget`/`findsNothing`/`findsNWidgets`/`findsWidgets` matchers (matchers.dart) and the
// finder-based `WidgetController` queries (`widget`, `widgetList`, `element`, `state`, `renderObject`,
// `getCenter`/`getTopLeft`/..., `tap`, `longPress`, `drag`, `fling`; controller.dart).

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    /// <summary>The tester finders evaluate against when none is given; fails when there is none.</summary>
    public static FrameworkDartTester RequireCurrent() =>
        Current ?? throw new InvalidOperationException("No FrameworkDartTester is alive to evaluate the finder.");

    /// <summary>Dart's <c>tester.allElements</c> (offstage ones included).</summary>
    public IEnumerable<Element> AllElementsIncludingOffstage => AllElements();

    /// <summary>Dart's <c>tester.allWidgets</c>.</summary>
    public IEnumerable<Widget> AllWidgets => AllElements().Select(element => element.Widget);

    /// <summary>
    /// Dart's <c>tester.allRenderObjects</c>: the render object of every element, in tree order, each once.
    /// </summary>
    public IEnumerable<RenderObject> AllRenderObjects => AllElements()
        .Select(element => element.RenderObject)
        .OfType<RenderObject>()
        .Distinct();

    /// <summary>Dart's <c>tester.any(finder)</c>.</summary>
    public bool Any(Finder finder) => finder.Evaluate(this).Count > 0;

    /// <summary>Dart's <c>tester.widget&lt;T&gt;(finder)</c>: the single match's widget.</summary>
    public T Widget<T>(Finder finder) where T : Widget => (T)SingleElement(finder, "widget").Widget;

    /// <summary>Dart's <c>tester.firstWidget&lt;T&gt;(finder)</c>.</summary>
    public T FirstWidget<T>(Finder finder) where T : Widget => (T)FirstElement(finder, "firstWidget").Widget;

    /// <summary>Dart's <c>tester.widgetList&lt;T&gt;(finder)</c>.</summary>
    public IReadOnlyList<T> WidgetList<T>(Finder finder) where T : Widget =>
        finder.Evaluate(this).Select(element => (T)element.Widget).ToList();

    /// <summary>Dart's <c>tester.element(finder)</c>: the single match.</summary>
    public Element Element(Finder finder) => SingleElement(finder, "element");

    /// <summary>Dart's <c>tester.firstElement(finder)</c>.</summary>
    public Element FirstElement(Finder finder) => FirstElement(finder, "firstElement");

    /// <summary>Dart's <c>tester.elementList(finder)</c>.</summary>
    public IReadOnlyList<Element> ElementList(Finder finder) => finder.Evaluate(this);

    /// <summary>Dart's <c>tester.state&lt;T&gt;(finder)</c>: the single match must be a stateful element.</summary>
    public TState State<TState>(Finder finder) where TState : State => StateOf<TState>(SingleElement(finder, "state"));

    /// <summary>Dart's <c>tester.firstState&lt;T&gt;(finder)</c>.</summary>
    public TState FirstState<TState>(Finder finder) where TState : State =>
        StateOf<TState>(FirstElement(finder, "firstState"));

    /// <summary>Dart's <c>tester.stateList&lt;T&gt;(finder)</c>.</summary>
    public IReadOnlyList<TState> StateList<TState>(Finder finder) where TState : State =>
        finder.Evaluate(this).Select(StateOf<TState>).ToList();

    /// <summary>Dart's <c>tester.renderObject&lt;T&gt;(finder)</c>: the single match's render object.</summary>
    public T RenderObject<T>(Finder finder) where T : RenderObject =>
        (T)(SingleElement(finder, "renderObject").RenderObject
            ?? throw new InvalidOperationException($"{finder} has no render object."));

    /// <summary>Dart's <c>tester.firstRenderObject&lt;T&gt;(finder)</c>.</summary>
    public T FirstRenderObject<T>(Finder finder) where T : RenderObject =>
        (T)(FirstElement(finder, "firstRenderObject").RenderObject
            ?? throw new InvalidOperationException($"{finder} has no render object."));

    /// <summary>Dart's <c>tester.renderObjectList&lt;T&gt;(finder)</c>.</summary>
    public IReadOnlyList<T> RenderObjectList<T>(Finder finder) where T : RenderObject =>
        finder.Evaluate(this).Select(element => (T)element.RenderObject!).ToList();

    /// <summary>
    /// What <paramref name="finder"/>'s render object paints, for <see cref="PaintAssert"/>
    /// (<c>expect(find.byType(X), paints..)</c> paints the single match's render object).
    /// </summary>
    public IReadOnlyList<CanvasCall> RecordPaint(Finder finder) =>
        PaintRecording.Record(RenderObject<RenderObject>(finder));

    /// <summary>Dart's <c>tester.getCenter(finder)</c>.</summary>
    public Point GetCenter(Finder finder) => GetCenter(SingleElement(finder, "getCenter"));

    /// <summary>Dart's <c>tester.getTopLeft(finder)</c>.</summary>
    public Point GetTopLeft(Finder finder) => GetTopLeft(SingleElement(finder, "getTopLeft"));

    /// <summary>Dart's <c>tester.getTopRight(finder)</c>.</summary>
    public Point GetTopRight(Finder finder) => GetTopRight(SingleElement(finder, "getTopRight"));

    /// <summary>Dart's <c>tester.getBottomLeft(finder)</c>.</summary>
    public Point GetBottomLeft(Finder finder) => GetBottomLeft(SingleElement(finder, "getBottomLeft"));

    /// <summary>Dart's <c>tester.getBottomRight(finder)</c>.</summary>
    public Point GetBottomRight(Finder finder) => GetBottomRight(SingleElement(finder, "getBottomRight"));

    /// <summary>Dart's <c>tester.getRect(finder)</c>.</summary>
    public Rect GetRect(Finder finder) => GetRect(SingleElement(finder, "getRect"));

    /// <summary>Dart's <c>tester.getSize(finder)</c>.</summary>
    public Size GetSize(Finder finder) => GetSize(SingleElement(finder, "getSize"));

    /// <summary>Dart's <c>tester.getTopRight</c>.</summary>
    public Point GetTopRight(Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Point(box.Size.Width, 0));
    }

    /// <summary>Dart's <c>tester.getBottomLeft</c>.</summary>
    public Point GetBottomLeft(Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Point(0, box.Size.Height));
    }

    /// <summary>
    /// Dart's <c>tester.tap(finder)</c>: a down and an up at the single match's center, no pump.
    /// </summary>
    public void Tap(
        Finder finder,
        PointerButtons buttons = PointerButtons.Primary,
        PointerDeviceKind kind = PointerDeviceKind.Touch) =>
        TapAt(GetCenter(SingleElement(finder, "tap")), buttons, kind);

    /// <summary>Dart's <c>tester.tapAt(location)</c>: a down and an up, no pump.</summary>
    public void TapAt(
        Point location,
        PointerButtons buttons = PointerButtons.Primary,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        TestGesture gesture = StartGesture(location, kind, buttons);
        gesture.Up();
    }

    /// <summary>
    /// Dart's <c>tester.longPress(finder)</c>: down, <c>pump(kLongPressTimeout + kPressTimeout)</c>, up.
    /// </summary>
    public void LongPress(
        Finder finder,
        PointerButtons buttons = PointerButtons.Primary,
        PointerDeviceKind kind = PointerDeviceKind.Touch) =>
        LongPressAt(GetCenter(SingleElement(finder, "longPress")), buttons, kind);

    /// <summary>Dart's <c>tester.longPressAt(location)</c>.</summary>
    public void LongPressAt(
        Point location,
        PointerButtons buttons = PointerButtons.Primary,
        PointerDeviceKind kind = PointerDeviceKind.Touch)
    {
        TestGesture gesture = StartGesture(location, kind, buttons);
        Pump(Gestures.GestureConstants.LongPressTimeout + Gestures.GestureConstants.PressTimeout);
        gesture.Up();
    }

    /// <summary>Dart's <c>tester.drag(finder, offset)</c>.</summary>
    public void Drag(
        Finder finder,
        Vector offset,
        PointerDeviceKind kind = PointerDeviceKind.Touch,
        double touchSlopX = DragSlopDefault,
        double touchSlopY = DragSlopDefault) =>
        Drag(SingleElement(finder, "drag"), offset, kind, touchSlopX, touchSlopY);

    /// <summary>Dart's <c>tester.timedDrag(finder, offset, duration)</c>.</summary>
    public void TimedDrag(Finder finder, Vector offset, TimeSpan duration, double frequency = 60.0) =>
        TimedDrag(SingleElement(finder, "timedDrag"), offset, duration, frequency);

    /// <summary>Dart's <c>tester.fling(finder, offset, speed)</c>.</summary>
    public void Fling(
        Finder finder,
        Vector offset,
        double speed,
        PointerButtons buttons = PointerButtons.Primary,
        TimeSpan? frameInterval = null,
        Vector initialOffset = default,
        TimeSpan? initialOffsetDelay = null) =>
        Fling(SingleElement(finder, "fling"), offset, speed, buttons, frameInterval, initialOffset, initialOffsetDelay);

    /// <summary>
    /// The single element <paramref name="finder"/> finds, failing with flutter_test's messages when it
    /// finds none or several.
    /// </summary>
    internal Element SingleElement(Finder finder, string callee)
    {
        IReadOnlyList<Element> elements = finder.Evaluate(this);
        if (elements.Count == 0)
        {
            throw new InvalidOperationException(
                $"The finder \"{finder}\" (used in a call to \"{callee}()\") could not find any matching widgets.");
        }

        if (elements.Count > 1)
        {
            throw new InvalidOperationException(
                $"The finder \"{finder}\" (used in a call to \"{callee}()\") ambiguously found multiple matching "
                + $"widgets ({elements.Count}). The \"{callee}()\" method needs a single target.");
        }

        return elements[0];
    }

    private Element FirstElement(Finder finder, string callee)
    {
        IReadOnlyList<Element> elements = finder.Evaluate(this);
        return elements.Count > 0
            ? elements[0]
            : throw new InvalidOperationException(
                $"The finder \"{finder}\" (used in a call to \"{callee}()\") could not find any matching widgets.");
    }

    private static TState StateOf<TState>(Element element) where TState : State =>
        element is StatefulElement { State: TState state }
            ? state
            : throw new InvalidOperationException(
                $"Widget of type {element.Widget.GetType().Name}, with no {typeof(TState).Name} state, "
                + "was found by a finder used with state<T>().");
}

/// <summary>
/// flutter_test's <c>Finder</c>: a lazy query over the element tree of the live
/// <see cref="FrameworkDartTester"/>, searching on-stage elements only unless <c>skipOffstage: false</c>.
/// Build them with <see cref="Find"/>; check them with <see cref="Finds"/>.
/// </summary>
internal abstract class Finder
{
    protected Finder(bool skipOffstage)
    {
        SkipOffstage = skipOffstage;
    }

    /// <summary>Whether offstage elements are skipped (Dart's default: true).</summary>
    public bool SkipOffstage { get; }

    /// <summary>A description of what is searched for, for failure messages.</summary>
    public abstract string Description { get; }

    /// <summary>Dart's <c>finder.first</c>.</summary>
    public Finder First => new IndexFinder(this, "first", elements => elements.Take(1));

    /// <summary>Dart's <c>finder.last</c>.</summary>
    public Finder Last => new IndexFinder(this, "last", elements => elements.TakeLast(1));

    /// <summary>Dart's <c>finder.at(index)</c>.</summary>
    public Finder At(int index) => new IndexFinder(this, $"at index {index}", elements => elements.Skip(index).Take(1));

    /// <summary>Dart's <c>finder.evaluate()</c> against <paramref name="tester"/> (the live one by default).</summary>
    public IReadOnlyList<Element> Evaluate(FrameworkDartTester? tester = null)
    {
        FrameworkDartTester target = tester ?? FrameworkDartTester.RequireCurrent();
        return FindInCandidates(AllCandidates(target)).ToList();
    }

    /// <summary>The number of matches.</summary>
    public int Count => Evaluate().Count;

    /// <summary>Dart's <c>finder.hasFound</c> after an evaluation: whether anything matches.</summary>
    public bool HasFound => Count > 0;

    /// <summary>Dart's <c>allCandidates</c>: every element (on-stage only by default) below the root.</summary>
    protected internal virtual IEnumerable<Element> AllCandidates(FrameworkDartTester tester) =>
        SkipOffstage ? tester.OnstageElements() : tester.AllElements();

    /// <summary>Dart's <c>findInCandidates</c>.</summary>
    protected internal abstract IEnumerable<Element> FindInCandidates(IEnumerable<Element> candidates);

    public override string ToString() => Description + (SkipOffstage ? " (ignoring offstage widgets)" : string.Empty);

    /// <summary>flutter_test's <c>_IndexFinder</c>/<c>first</c>/<c>last</c> over a parent finder.</summary>
    private sealed class IndexFinder(Finder parent, string which, Func<IEnumerable<Element>, IEnumerable<Element>> pick)
        : Finder(parent.SkipOffstage)
    {
        public override string Description => $"{parent.Description} ({which})";

        protected internal override IEnumerable<Element> AllCandidates(FrameworkDartTester tester) =>
            parent.AllCandidates(tester);

        protected internal override IEnumerable<Element> FindInCandidates(IEnumerable<Element> candidates) =>
            pick(parent.FindInCandidates(candidates).ToList());
    }
}

/// <summary>flutter_test's <c>MatchFinder</c>: keeps the candidates a predicate accepts.</summary>
internal sealed class MatchFinder(Func<Element, bool> matches, string description, bool skipOffstage = true)
    : Finder(skipOffstage)
{
    public override string Description { get; } = description;

    protected internal override IEnumerable<Element> FindInCandidates(IEnumerable<Element> candidates) =>
        candidates.Where(matches);

    /// <summary>Whether <paramref name="candidate"/> matches.</summary>
    public bool Matches(Element candidate) => matches(candidate);
}

/// <summary>flutter_test's <c>_DescendantFinder</c>.</summary>
internal sealed class DescendantFinder(Finder of, Finder matching, bool matchRoot, bool skipOffstage)
    : Finder(skipOffstage)
{
    public override string Description => matchRoot
        ? $"{matching.Description} in the subtree(s) beginning with {of.Description}"
        : $"{matching.Description} that has ancestor(s) with {of.Description}";

    protected internal override IEnumerable<Element> AllCandidates(FrameworkDartTester tester)
    {
        IReadOnlyList<Element> roots = of.Evaluate(tester);
        var seen = new HashSet<Element>();
        var candidates = new List<Element>();
        if (matchRoot)
        {
            candidates.AddRange(roots);
        }

        foreach (Element root in roots)
        {
            void Visit(Element element)
            {
                if (seen.Add(element))
                {
                    candidates.Add(element);
                }

                if (SkipOffstage)
                {
                    element.DebugVisitOnstageChildren(Visit);
                }
                else
                {
                    element.VisitChildren(Visit);
                }
            }

            if (SkipOffstage)
            {
                root.DebugVisitOnstageChildren(Visit);
            }
            else
            {
                root.VisitChildren(Visit);
            }
        }

        return candidates;
    }

    // flutter_test's `_DescendantFinderMixin.findInCandidates`: `matching` is evaluated as a whole, so a
    // nested finder (e.g. another `find.descendant`) keeps its own constraints.
    protected internal override IEnumerable<Element> FindInCandidates(IEnumerable<Element> candidates)
    {
        var descendants = new HashSet<Element>(matching.Evaluate());
        return candidates.Where(descendants.Contains);
    }
}

/// <summary>flutter_test's <c>_AncestorFinder</c>.</summary>
internal sealed class AncestorFinder(Finder of, Finder matching, bool matchRoot) : Finder(skipOffstage: false)
{
    public override string Description => matchRoot
        ? $"ancestor {matching.Description} beginning with {of.Description}"
        : $"{matching.Description} which is an ancestor of {of.Description}";

    protected internal override IEnumerable<Element> AllCandidates(FrameworkDartTester tester)
    {
        var candidates = new List<Element>();
        foreach (Element root in of.Evaluate(tester))
        {
            if (matchRoot)
            {
                candidates.Add(root);
            }

            root.VisitAncestorElements(element =>
            {
                candidates.Add(element);
                return true;
            });
        }

        return candidates;
    }

    // flutter_test's `_AncestorFinderMixin.findInCandidates`: `matching` is evaluated as a whole.
    protected internal override IEnumerable<Element> FindInCandidates(IEnumerable<Element> candidates)
    {
        var ancestors = new HashSet<Element>(matching.Evaluate());
        return candidates.Where(ancestors.Contains);
    }
}

/// <summary>
/// flutter_test's <c>find</c> (<c>CommonFinders</c>): <c>Find.Text("OK")</c>, <c>Find.ByType&lt;Icon&gt;()</c>,
/// <c>Find.Descendant(of: ..., matching: ...)</c>, ... Every finder skips offstage elements unless
/// <c>skipOffstage: false</c>.
/// </summary>
internal static class Find
{
    /// <summary>
    /// Dart's <c>find.text(text, findRichText:)</c>: <see cref="Text"/> widgets whose data (or span's plain
    /// text) equals <paramref name="text"/>, and <see cref="EditableText"/>s whose controller holds it. With
    /// <paramref name="findRichText"/>, <see cref="RichText"/> widgets are matched instead of <c>Text</c>.
    /// </summary>
    public static Finder Text(string text, bool findRichText = false, bool skipOffstage = true) =>
        new MatchFinder(
            element => MatchesText(element.Widget, findRichText, candidate => candidate == text),
            $"text \"{text}\"",
            skipOffstage);

    /// <summary>Dart's <c>find.textContaining(pattern)</c>.</summary>
    public static Finder TextContaining(string pattern, bool findRichText = false, bool skipOffstage = true) =>
        new MatchFinder(
            element => MatchesText(
                element.Widget,
                findRichText,
                candidate => candidate.Contains(pattern, StringComparison.Ordinal)),
            $"text containing \"{pattern}\"",
            skipOffstage);

    /// <summary>Dart's <c>find.textContaining(RegExp)</c>.</summary>
    public static Finder TextContaining(Regex pattern, bool findRichText = false, bool skipOffstage = true) =>
        new MatchFinder(
            element => MatchesText(element.Widget, findRichText, pattern.IsMatch),
            $"text containing /{pattern}/",
            skipOffstage);

    /// <summary>Dart's <c>find.byType(T)</c>: exact runtime type.</summary>
    public static Finder ByType<T>(bool skipOffstage = true) where T : Widget =>
        ByType(typeof(T), skipOffstage);

    /// <summary>Dart's <c>find.byType(type)</c>: exact runtime type.</summary>
    public static Finder ByType(Type type, bool skipOffstage = true) =>
        new MatchFinder(element => element.Widget.GetType() == type, $"type \"{type.Name}\"", skipOffstage);

    /// <summary>Dart's <c>find.bySubtype&lt;T&gt;()</c>.</summary>
    public static Finder BySubtype<T>(bool skipOffstage = true) where T : Widget =>
        new MatchFinder(element => element.Widget is T, $"type \"{typeof(T).Name}\" or subtype", skipOffstage);

    /// <summary>
    /// A widget whose runtime type is named <paramref name="typeName"/> — Dart's
    /// <c>find.byWidgetPredicate((w) =&gt; w.runtimeType.toString() == '_Private')</c> for private types.
    /// </summary>
    public static Finder ByTypeName(string typeName, bool skipOffstage = true) =>
        new MatchFinder(element => element.Widget.GetType().Name == typeName, $"type \"{typeName}\"", skipOffstage);

    /// <summary>Dart's <c>find.byKey(key)</c>.</summary>
    public static Finder ByKey(Key key, bool skipOffstage = true) =>
        new MatchFinder(element => Equals(element.Widget.Key, key), $"key {key}", skipOffstage);

    /// <summary>Dart's <c>find.byWidget(widget)</c>: the very instance.</summary>
    public static Finder ByWidget(Widget widget, bool skipOffstage = true) =>
        new MatchFinder(
            element => ReferenceEquals(element.Widget, widget),
            $"the given widget ({widget.GetType().Name})",
            skipOffstage);

    /// <summary>The very element (for passing an already found element where a finder is expected).</summary>
    public static Finder ByElement(Element element) =>
        new MatchFinder(candidate => ReferenceEquals(candidate, element), $"the given element ({element})", false);

    /// <summary>Dart's <c>find.byIcon(icon)</c>: an <see cref="Icon"/> showing <paramref name="icon"/>.</summary>
    public static Finder ByIcon(IconData icon, bool skipOffstage = true) =>
        new MatchFinder(
            element => element.Widget is Icon { IconData: { } data } && Equals(data, icon),
            $"icon \"{icon}\"",
            skipOffstage);

    /// <summary>Dart's <c>find.byWidgetPredicate(predicate)</c>.</summary>
    public static Finder ByWidgetPredicate(
        Func<Widget, bool> predicate,
        string? description = null,
        bool skipOffstage = true) =>
        new MatchFinder(element => predicate(element.Widget), description ?? "widget matching predicate", skipOffstage);

    /// <summary>Dart's <c>find.byElementPredicate(predicate)</c>.</summary>
    public static Finder ByElementPredicate(
        Func<Element, bool> predicate,
        string? description = null,
        bool skipOffstage = true) =>
        new MatchFinder(predicate, description ?? "element matching predicate", skipOffstage);

    /// <summary>
    /// Dart's <c>find.widgetWithText(type, text)</c>: <paramref name="widgetType"/> widgets that have a
    /// <c>find.text(text)</c> descendant.
    /// </summary>
    public static Finder WidgetWithText(Type widgetType, string text, bool skipOffstage = true) =>
        Ancestor(of: Text(text, skipOffstage: skipOffstage), matching: ByType(widgetType, skipOffstage));

    /// <summary>Dart's <c>find.widgetWithText(T, text)</c>.</summary>
    public static Finder WidgetWithText<T>(string text, bool skipOffstage = true) where T : Widget =>
        WidgetWithText(typeof(T), text, skipOffstage);

    /// <summary>Dart's <c>find.widgetWithIcon(type, icon)</c>.</summary>
    public static Finder WidgetWithIcon(Type widgetType, IconData icon, bool skipOffstage = true) =>
        Ancestor(of: ByIcon(icon, skipOffstage), matching: ByType(widgetType, skipOffstage));

    /// <summary>Dart's <c>find.widgetWithIcon(T, icon)</c>.</summary>
    public static Finder WidgetWithIcon<T>(IconData icon, bool skipOffstage = true) where T : Widget =>
        WidgetWithIcon(typeof(T), icon, skipOffstage);

    /// <summary>
    /// Dart's <c>find.descendant(of:, matching:, matchRoot:, skipOffstage:)</c>: the elements
    /// <paramref name="matching"/> accepts among the descendants of what <paramref name="of"/> finds.
    /// </summary>
    public static Finder Descendant(Finder of, Finder matching, bool matchRoot = false, bool skipOffstage = true) =>
        new DescendantFinder(of, matching, matchRoot, skipOffstage);

    /// <summary>
    /// Dart's <c>find.ancestor(of:, matching:, matchRoot:)</c>: the elements <paramref name="matching"/>
    /// accepts among the ancestors of what <paramref name="of"/> finds (nearest first).
    /// </summary>
    public static Finder Ancestor(Finder of, Finder matching, bool matchRoot = false) =>
        new AncestorFinder(of, matching, matchRoot);

    /// <summary>
    /// Dart's <c>find.bySemanticsLabel(label)</c>: render object elements whose semantics node carries the
    /// label. Needs semantics on (<see cref="FrameworkDartTester.EnsureSemantics"/>).
    /// </summary>
    public static Finder BySemanticsLabel(string label, bool skipOffstage = true) =>
        BySemanticsLabel(candidate => candidate == label, $"a semantics label named \"{label}\"", skipOffstage);

    /// <summary>Dart's <c>find.bySemanticsLabel(RegExp)</c>.</summary>
    public static Finder BySemanticsLabel(Regex label, bool skipOffstage = true) =>
        BySemanticsLabel(label.IsMatch, $"a semantics label matching the pattern \"{label}\"", skipOffstage);

    /// <summary>
    /// Dart's <c>find.byTooltip(message)</c>: a <c>RawTooltip</c> whose semantics tooltip is
    /// <paramref name="message"/>, or a <see cref="Tooltip"/> showing it that gives no semantics tooltip.
    /// </summary>
    public static Finder ByTooltip(string message, bool skipOffstage = true) =>
        ByWidgetPredicate(
            widget =>
            {
                if (widget is Tooltip tooltip)
                {
                    string tooltipMessage = tooltip.Message ?? tooltip.RichMessage!.ToPlainText();
                    if ((tooltip.ExcludeFromSemantics ?? false) || tooltipMessage.Length == 0)
                    {
                        return tooltipMessage == message;
                    }
                }

                return widget is RawTooltip rawTooltip && rawTooltip.SemanticsTooltip == message;
            },
            $"tooltip \"{message}\"",
            skipOffstage);

    private static Finder BySemanticsLabel(Func<string, bool> matches, string description, bool skipOffstage) =>
        new MatchFinder(
            element => element is RenderObjectElement { RenderObject: { } renderObject }
                       && renderObject.SemanticsNode is { } node
                       && matches(node.Label),
            description,
            skipOffstage);

    // finders.dart's `_MatchTextFinder.matches`.
    private static bool MatchesText(Widget widget, bool findRichText, Func<string, bool> matchesText)
    {
        if (widget is EditableText editable)
        {
            return matchesText(editable.Controller.Text);
        }

        if (!findRichText)
        {
            return widget is Plumix.Widgets.Text text
                   && matchesText(text.Data ?? text.TextSpan!.ToPlainText());
        }

        return widget is RichText richText && matchesText(richText.Text.ToPlainText());
    }
}

/// <summary>
/// flutter_test's finder matchers: <c>expect(finder, findsOneWidget)</c> is
/// <c>Finds.OneWidget(finder)</c>, and so on.
/// </summary>
internal static class Finds
{
    /// <summary><c>findsOneWidget</c> / <c>findsOne</c>.</summary>
    public static void OneWidget(Finder finder) => Check(finder, count => count == 1, "exactly one matching candidate");

    /// <summary><c>findsOne</c>.</summary>
    public static void One(Finder finder) => OneWidget(finder);

    /// <summary><c>findsNothing</c>.</summary>
    public static void Nothing(Finder finder) => Check(finder, count => count == 0, "no matching candidates");

    /// <summary><c>findsWidgets</c> / <c>findsAny</c>: at least one.</summary>
    public static void Widgets(Finder finder) => Check(finder, count => count > 0, "at least one matching candidate");

    /// <summary><c>findsNWidgets(n)</c> / <c>findsExactly(n)</c>.</summary>
    public static void NWidgets(Finder finder, int n) =>
        Check(finder, count => count == n, $"exactly {n} matching candidates");

    /// <summary><c>findsAtLeastNWidgets(n)</c> / <c>findsAtLeast(n)</c>.</summary>
    public static void AtLeastNWidgets(Finder finder, int n) =>
        Check(finder, count => count >= n, $"at least {n} matching candidates");

    private static void Check(Finder finder, Func<int, bool> accepts, string expected)
    {
        IReadOnlyList<Element> found = finder.Evaluate();
        if (!accepts(found.Count))
        {
            string widgets = string.Join(", ", found.Take(10).Select(element => element.Widget.GetType().Name));
            throw new XunitException(
                $"Expected: {expected}\nActual: {finder} found {found.Count} widget(s): [{widgets}]");
        }
    }
}
