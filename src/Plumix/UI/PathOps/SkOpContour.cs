namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkOpContour.h and SkOpContour.cpp at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types
// are kept. SkOpContour is a `partial` class: rayCheck and findSortableTop are defined in SkPathOpsWinding.cpp, whose
// part is in SkPathOpsWinding.cs. The contour owns its first segment (Skia embeds fHead by value) and links
// the rest through SkOpSegment::fNext/fPrev exactly as Skia does, so iteration order is unchanged; appendSegment's
// arena make<SkOpSegment>() is `new SkOpSegment()`. SkOpContourBuilder's arena point copies are plain arrays; its
// fContour is nullable, as SkOpEdgeBuilder::complete() sets it to nullptr.
// fFirstSorted and fDebugIndent are only touched by debug code and are omitted, as are debug* and dump*.

internal partial class SkOpContour
{
    protected SkOpGlobalState _state = null!;
    protected readonly SkOpSegment _head = new SkOpSegment();
    protected SkOpSegment? _tail;
    protected SkOpContour? _next;
    protected SkPathOpsBounds _bounds;
    protected int _ccw;
    protected int _count;
    protected bool _done; // set by find top segment
    protected bool _operand; // true for the second argument to a binary operator
    protected bool _reverse; // true if contour should be reverse written to path (used only by fix winding)
    protected bool _xor; // set if original path had even-odd fill
    protected bool _oppXor; // set if opposite path had even-odd fill

    public SkOpContour()
    {
        Reset();
    }

    // Skia's operator<.
    public static bool LessThan(SkOpContour lh, SkOpContour rh)
    {
        return lh._bounds.Top == rh._bounds.Top
            ? lh._bounds.Left < rh._bounds.Left
            : lh._bounds.Top < rh._bounds.Top;
    }

    public void AddConic(ReadOnlySpan<PathPoint> pts, float weight)
    {
        AppendSegment().AddConic(pts, weight, this);
    }

    public void AddCubic(ReadOnlySpan<PathPoint> pts)
    {
        AppendSegment().AddCubic(pts, this);
    }

    public SkOpSegment AddLine(ReadOnlySpan<PathPoint> pts)
    {
        return AppendSegment().AddLine(pts, this);
    }

    public void AddQuad(ReadOnlySpan<PathPoint> pts)
    {
        AppendSegment().AddQuad(pts, this);
    }

    public SkOpSegment AppendSegment()
    {
        SkOpSegment result = _count++ != 0 ? new SkOpSegment() : _head;
        result.SetPrev(_tail);
        if (_tail != null)
        {
            _tail.SetNext(result);
        }

        _tail = result;
        return result;
    }

    public ref readonly SkPathOpsBounds Bounds()
    {
        return ref _bounds;
    }

    public void CalcAngles()
    {
        SkOpSegment? segment = _head;
        do
        {
            segment.CalcAngles();
        }
        while ((segment = segment.Next()) != null);
    }

    public void Complete()
    {
        SetBounds();
    }

    public int Count()
    {
        return _count;
    }

    public SkOpGlobalState GlobalState()
    {
        return _state;
    }

    public bool Done()
    {
        return _done;
    }

    public PathPoint End()
    {
        return _tail!.Pts()[SkPathOps.SkPathOpsVerbToPoints(_tail.Verb())];
    }

    public SkOpSegment First()
    {
        return _head;
    }

    public void Init(SkOpGlobalState globalState, bool operand, bool isXor)
    {
        _state = globalState;
        _operand = operand;
        _xor = isXor;
    }

    public int IsCcw()
    {
        return _ccw;
    }

    public bool IsXor()
    {
        return _xor;
    }

    public void JoinSegments()
    {
        SkOpSegment? segment = _head;
        SkOpSegment? next;
        do
        {
            next = segment.Next();
            segment.JoinEnds(next != null ? next : _head);
        }
        while ((segment = next) != null);
    }

    public void MarkAllDone()
    {
        SkOpSegment? segment = _head;
        do
        {
            segment.MarkAllDone();
        }
        while ((segment = segment.Next()) != null);
    }

    // Please keep this aligned with debugMissingCoincidence()
    public bool MissingCoincidence()
    {
        SkOpSegment? segment = _head;
        bool result = false;
        do
        {
            if (segment.MissingCoincidence())
            {
                result = true;
            }

            segment = segment.Next();
        }
        while (segment != null);

        return result;
    }

    public bool MoveMultiples()
    {
        SkOpSegment? segment = _head;
        do
        {
            if (!segment.MoveMultiples())
            {
                return false;
            }
        }
        while ((segment = segment.Next()) != null);

        return true;
    }

    public bool MoveNearby()
    {
        SkOpSegment? segment = _head;
        do
        {
            if (!segment.MoveNearby())
            {
                return false;
            }
        }
        while ((segment = segment.Next()) != null);

        return true;
    }

    public SkOpContour? Next()
    {
        return _next;
    }

    public bool Operand()
    {
        return _operand;
    }

    public bool OppXor()
    {
        return _oppXor;
    }

    public void Reset()
    {
        _tail = null;
        _next = null;
        _count = 0;
        _done = false;
    }

    public void ResetReverse()
    {
        SkOpContour? next = this;
        do
        {
            if (next.Count() == 0)
            {
                continue;
            }

            next._ccw = -1;
            next._reverse = false;
        }
        while ((next = next.Next()) != null);
    }

    public bool Reversed()
    {
        return _reverse;
    }

    public void SetBounds()
    {
        SkOpSegment? segment = _head;
        _bounds = segment.Bounds();
        while ((segment = segment.Next()) != null)
        {
            _bounds.Add(segment.Bounds());
        }
    }

    public void SetCcw(int ccw)
    {
        _ccw = ccw;
    }

    public void SetGlobalState(SkOpGlobalState state)
    {
        _state = state;
    }

    public void SetNext(SkOpContour? contour)
    {
        //        SkASSERT(!fNext == !!contour);
        _next = contour;
    }

    public void SetOperand(bool isOp)
    {
        _operand = isOp;
    }

    public void SetOppXor(bool isOppXor)
    {
        _oppXor = isOppXor;
    }

    public void SetReverse()
    {
        _reverse = true;
    }

    public void SetXor(bool isXor)
    {
        _xor = isXor;
    }

    public bool SortAngles()
    {
        SkOpSegment? segment = _head;
        do
        {
            if (!segment.SortAngles())
            {
                return false;
            }
        }
        while ((segment = segment.Next()) != null);

        return true;
    }

    public PathPoint Start()
    {
        return _head.Pts()[0];
    }

    public void ToPartialBackward(SkPathWriter path)
    {
        SkOpSegment? segment = _tail;
        do
        {
            segment!.AddCurveTo(segment.Tail(), segment.Head(), path);
        }
        while ((segment = segment.Prev()) != null);
    }

    public void ToPartialForward(SkPathWriter path)
    {
        SkOpSegment? segment = _head;
        do
        {
            segment.AddCurveTo(segment.Head(), segment.Tail(), path);
        }
        while ((segment = segment.Next()) != null);
    }

    // SkOpContour.cpp

    public void ToPath(SkPathWriter path)
    {
        if (Count() == 0)
        {
            return;
        }

        SkOpSegment? segment = _head;
        do
        {
            segment.AddCurveTo(segment.Head(), segment.Tail(), path);
        }
        while ((segment = segment.Next()) != null);

        path.FinishContour();
        path.Assemble();
    }

    public void ToReversePath(SkPathWriter path)
    {
        SkOpSegment? segment = _tail;
        do
        {
            segment!.AddCurveTo(segment.Tail(), segment.Head(), path);
        }
        while ((segment = segment.Prev()) != null);

        path.FinishContour();
        path.Assemble();
    }

    public SkOpSpan? UndoneSpan()
    {
        SkOpSegment? testSegment = _head;
        do
        {
            if (testSegment.Done())
            {
                continue;
            }

            return testSegment.UndoneSpan();
        }
        while ((testSegment = testSegment.Next()) != null);

        _done = true;
        return null;
    }
}

internal sealed class SkOpContourHead : SkOpContour
{
    public SkOpContour AppendContour()
    {
        // Skia allocates an SkOpContour here, and SortContourList and the Op/Simplify entry points static_cast
        // plain contours to SkOpContourHead (which adds no state). A C# downcast would throw, so every appended
        // contour is created as the head type; its behaviour as an SkOpContour is unchanged.
        SkOpContour contour = new SkOpContourHead();
        contour.SetNext(null);
        SkOpContour prev = this;
        SkOpContour? next;
        while ((next = prev.Next()) != null)
        {
            prev = next;
        }

        prev.SetNext(contour);
        return contour;
    }

    public void JoinAllSegments()
    {
        SkOpContour? next = this;
        do
        {
            if (next.Count() == 0)
            {
                continue;
            }

            next.JoinSegments();
        }
        while ((next = next.Next()) != null);
    }

    public void Remove(SkOpContour contour)
    {
        if (contour == this)
        {
            return;
        }

        SkOpContour prev = this;
        SkOpContour? next;
        while ((next = prev.Next()) != contour)
        {
            prev = next!;
        }

        prev.SetNext(null);
    }
}

internal sealed class SkOpContourBuilder
{
    private SkOpContour? _contour;
    private readonly PathPoint[] _lastLine = new PathPoint[2];
    private bool _lastIsLine;

    public SkOpContourBuilder(SkOpContour? contour)
    {
        _contour = contour;
        _lastIsLine = false;
    }

    public SkOpContour? Contour()
    {
        return _contour;
    }

    public void SetContour(SkOpContour? contour)
    {
        Flush();
        _contour = contour;
    }

    // SkOpContour.cpp

    public void AddConic(ReadOnlySpan<PathPoint> pts, float weight)
    {
        Flush();
        _contour!.AddConic(pts, weight);
    }

    public void AddCubic(ReadOnlySpan<PathPoint> pts)
    {
        Flush();
        _contour!.AddCubic(pts);
    }

    public void AddCurve(PathVerb verb, ReadOnlySpan<PathPoint> pts, float weight = 1)
    {
        if (PathVerb.Line == verb)
        {
            AddLine(pts);
            return;
        }

        switch (verb)
        {
            case PathVerb.Quad:
            {
                PathPoint[] ptStorage = new PathPoint[3];
                pts[..3].CopyTo(ptStorage);
                AddQuad(ptStorage);
                break;
            }

            case PathVerb.Conic:
            {
                PathPoint[] ptStorage = new PathPoint[3];
                pts[..3].CopyTo(ptStorage);
                AddConic(ptStorage, weight);
                break;
            }

            case PathVerb.Cubic:
            {
                PathPoint[] ptStorage = new PathPoint[4];
                pts[..4].CopyTo(ptStorage);
                AddCubic(ptStorage);
                break;
            }

            default:
                break;
        }
    }

    public void AddLine(ReadOnlySpan<PathPoint> pts)
    {
        // if the previous line added is the exact opposite, eliminate both
        if (_lastIsLine)
        {
            if (_lastLine[0] == pts[1] && _lastLine[1] == pts[0])
            {
                _lastIsLine = false;
                return;
            }
            else
            {
                Flush();
            }
        }

        pts[..2].CopyTo(_lastLine);
        _lastIsLine = true;
    }

    public void AddQuad(ReadOnlySpan<PathPoint> pts)
    {
        Flush();
        _contour!.AddQuad(pts);
    }

    public void Flush()
    {
        if (!_lastIsLine)
        {
            return;
        }

        PathPoint[] ptStorage = new PathPoint[2];
        _lastLine.CopyTo(ptStorage, 0);
        _ = _contour!.AddLine(ptStorage);
        _lastIsLine = false;
    }
}
