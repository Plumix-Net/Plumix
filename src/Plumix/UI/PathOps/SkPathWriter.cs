namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkPathWriter.h and SkPathWriter.cpp at chrome/m119 (the Skia
// inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double types
// are kept. SkPath is Plumix.UI.Path, edited through the SkPath helpers of SkPath.cs; SkPath's value semantics are
// kept by copying where Skia copies (fPartials.push_back(fCurrent), partial = reverse). fDefer[2] is an array of
// two references, fEndPtTs a List that assemble rewrites in place as Skia writes through runs. DistanceLessThan is a
// lambda over the distances. DEBUG_PATH_CONSTRUCTION and DEBUG_ASSEMBLE output is omitted.

// Construct the path one contour at a time.
// If the contour is closed, copy it to the final output.
// Otherwise, keep the partial contour for later assembly.
internal sealed class SkPathWriter
{
    private readonly Path _current = new Path(); // contour under construction
    private readonly List<Path> _partials = new List<Path>(); // contours with mismatched starts and ends
    private readonly List<SkOpPtT> _endPtTs = new List<SkOpPtT>(); // possible pt values for partial starts and ends
    private readonly Path _pathPtr; // closed contours are written here
    private readonly SkOpPtT?[] _defer = new SkOpPtT?[2]; // [0] deferred move, [1] deferred line
    private SkOpPtT? _firstPtT; // first in current contour

    // wrap path to keep track of whether the contour is initialized and non-empty
    public SkPathWriter(Path path)
    {
        _pathPtr = path;
        Init();
    }

    public bool HasMove()
    {
        return _firstPtT == null;
    }

    public Path NativePath()
    {
        return _pathPtr;
    }

    private List<SkOpPtT> EndPtTs()
    {
        return _endPtTs;
    }

    private List<Path> Partials()
    {
        return _partials;
    }

    // SkPathWriter.cpp

    private void Close()
    {
        if (SkPathOps.SkPathIsEmpty(_current))
        {
            return;
        }

        _current.Close();
        SkPathOps.SkPathAddPath(_pathPtr, _current, extend: false);
        _current.Reset();
        Init();
    }

    public void ConicTo(PathPoint pt1, SkOpPtT pt2, float weight)
    {
        PathPoint pt2pt = Update(pt2);
        SkPathOps.SkPathConicTo(_current, pt1, pt2pt, weight);
    }

    public void CubicTo(PathPoint pt1, PathPoint pt2, SkOpPtT pt3)
    {
        PathPoint pt3pt = Update(pt3);
        SkPathOps.SkPathCubicTo(_current, pt1, pt2, pt3pt);
    }

    public bool DeferredLine(SkOpPtT pt)
    {
        if (_defer[0] == pt)
        {
            // FIXME: why we're adding a degenerate line? Caller should have preflighted this.
            return true;
        }

        if (pt.Contains(_defer[0]!))
        {
            // FIXME: why we're adding a degenerate line?
            return true;
        }

        if (MatchedLast(pt))
        {
            return false;
        }

        if (_defer[1] != null && ChangedSlopes(pt))
        {
            LineTo();
            _defer[0] = _defer[1];
        }

        _defer[1] = pt;
        return true;
    }

    public void DeferredMove(SkOpPtT pt)
    {
        if (_defer[1] == null)
        {
            _firstPtT = _defer[0] = pt;
            return;
        }

        if (!MatchedLast(pt))
        {
            FinishContour();
            _firstPtT = _defer[0] = pt;
        }
    }

    public void FinishContour()
    {
        if (!MatchedLast(_defer[0]))
        {
            if (_defer[1] == null)
            {
                return;
            }

            LineTo();
        }

        if (SkPathOps.SkPathIsEmpty(_current))
        {
            return;
        }

        if (IsClosed())
        {
            Close();
        }
        else
        {
            _endPtTs.Add(_firstPtT!);
            _endPtTs.Add(_defer[1]!);
            _partials.Add(new Path(_current));
            Init();
        }
    }

    public void Init()
    {
        _current.Reset();
        _firstPtT = _defer[0] = _defer[1] = null;
    }

    public bool IsClosed()
    {
        return MatchedLast(_firstPtT);
    }

    private void LineTo()
    {
        if (SkPathOps.SkPathIsEmpty(_current))
        {
            MoveTo();
        }

        SkPathOps.SkPathLineTo(_current, _defer[1]!.Pt);
    }

    private bool MatchedLast(SkOpPtT? test)
    {
        if (test == _defer[1])
        {
            return true;
        }

        if (test == null)
        {
            return false;
        }

        if (_defer[1] == null)
        {
            return false;
        }

        return test.Contains(_defer[1]!);
    }

    private void MoveTo()
    {
        SkPathOps.SkPathMoveTo(_current, _firstPtT!.Pt);
    }

    public void QuadTo(PathPoint pt1, SkOpPtT pt2)
    {
        PathPoint pt2pt = Update(pt2);
        SkPathOps.SkPathQuadTo(_current, pt1, pt2pt);
    }

    // if last point to be written matches the current path's first point, alter the
    // last to avoid writing a degenerate lineTo when the path is closed
    private PathPoint Update(SkOpPtT pt)
    {
        if (_defer[1] == null)
        {
            MoveTo();
        }
        else if (!MatchedLast(_defer[0]))
        {
            LineTo();
        }

        PathPoint result = pt.Pt;
        if (_firstPtT != null && result != _firstPtT.Pt && _firstPtT.Contains(pt))
        {
            result = _firstPtT.Pt;
        }

        _defer[0] = _defer[1] = pt; // set both to know that there is not a pending deferred line
        return result;
    }

    private bool SomeAssemblyRequired()
    {
        FinishContour();
        return _endPtTs.Count != 0;
    }

    private bool ChangedSlopes(SkOpPtT ptT)
    {
        if (MatchedLast(_defer[0]))
        {
            return false;
        }

        PathPoint deferDxdy = _defer[1]!.Pt - _defer[0]!.Pt;
        PathPoint lineDxdy = ptT.Pt - _defer[1]!.Pt;
        return deferDxdy.X * lineDxdy.Y != deferDxdy.Y * lineDxdy.X;
    }

    /*
        check start and end of each contour
        if not the same, record them
        match them up
        connect closest
        reassemble contour pieces into new path
    */
    public void Assemble()
    {
        if (!SomeAssemblyRequired())
        {
            return;
        }

        List<SkOpPtT> runs = _endPtTs; // starts, ends of partial contours
        int endCount = _endPtTs.Count; // all starts and ends
        // lengthen any partial contour adjacent to a simple segment
        for (int pIndex = 0; pIndex < endCount; pIndex++)
        {
            SkOpPtT opPtT = runs[pIndex];
            Path p = new Path();
            SkPathWriter partWriter = new SkPathWriter(p);
            do
            {
                if (!SkPathOps.ZeroOrOne(opPtT.T))
                {
                    break;
                }

                SkOpSpanBase opSpanBase = opPtT.Span();
                SkOpSpanBase start = opPtT.T != 0 ? opSpanBase.Prev()! : opSpanBase.UpCast().Next();
                int step = opPtT.T != 0 ? 1 : -1;
                SkOpSegment opSegment = opSpanBase.Segment();
                SkOpSegment? nextSegment = opSegment.IsSimple(ref start, ref step);
                if (nextSegment == null)
                {
                    break;
                }

                SkOpSpanBase opSpanEnd = start.T() != 0 ? start.Prev()! : start.UpCast().Next();
                if (start.Starter(opSpanEnd).AlreadyAdded())
                {
                    break;
                }

                _ = nextSegment.AddCurveTo(start, opSpanEnd, partWriter);
                opPtT = opSpanEnd.PtT();
                runs[pIndex] = opPtT;
            }
            while (true);

            partWriter.FinishContour();
            List<Path> partPartials = partWriter.Partials();
            if (partPartials.Count == 0)
            {
                continue;
            }

            // if pIndex is even, reverse and prepend to fPartials; otherwise, append
            Path partial = _partials[pIndex >> 1];
            Path part = partPartials[0];
            if ((pIndex & 1) != 0)
            {
                SkPathOps.SkPathAddPath(partial, part, extend: true);
            }
            else
            {
                Path reverse = new Path();
                SkPathOps.SkPathReverseAddPath(reverse, part);
                SkPathOps.SkPathAddPath(reverse, partial, extend: true);
                _partials[pIndex >> 1] = reverse;
            }
        }

        int linkCount = endCount / 2; // number of partial contours
        int[] sLink = new int[linkCount];
        int[] eLink = new int[linkCount];
        int rIndex, iIndex;
        for (rIndex = 0; rIndex < linkCount; ++rIndex)
        {
            sLink[rIndex] = eLink[rIndex] = SkPathOps.SkMaxS32;
        }

        int entries = endCount * (endCount - 1) / 2; // folded triangle
        double[] distances = new double[entries];
        int[] sortedDist = new int[entries];
        int[] distLookup = new int[entries];
        int rRow = 0;
        int dIndex = 0;
        for (rIndex = 0; rIndex < endCount - 1; ++rIndex)
        {
            SkOpPtT oPtT = runs[rIndex];
            for (iIndex = rIndex + 1; iIndex < endCount; ++iIndex)
            {
                SkOpPtT iPtT = runs[iIndex];
                double dx = iPtT.Pt.X - oPtT.Pt.X;
                double dy = iPtT.Pt.Y - oPtT.Pt.Y;
                double dist = dx * dx + dy * dy;
                distLookup[dIndex] = rRow + iIndex;
                distances[dIndex] = dist; // oStart distance from iStart
                sortedDist[dIndex] = dIndex;
                dIndex++;
            }

            rRow += endCount;
        }

        SkPathOps.SkTQSort<int>(sortedDist, (int one, int two) => distances[one] < distances[two]);
        int remaining = linkCount; // number of start/end pairs
        for (rIndex = 0; rIndex < entries; ++rIndex)
        {
            int pair = sortedDist[rIndex];
            pair = distLookup[pair];
            int row = pair / endCount;
            int col = pair - row * endCount;
            int ndxOne = row >> 1;
            bool endOne = (row & 1) != 0;
            int[] linkOne = endOne ? eLink : sLink;
            if (linkOne[ndxOne] != SkPathOps.SkMaxS32)
            {
                continue;
            }

            int ndxTwo = col >> 1;
            bool endTwo = (col & 1) != 0;
            int[] linkTwo = endTwo ? eLink : sLink;
            if (linkTwo[ndxTwo] != SkPathOps.SkMaxS32)
            {
                continue;
            }

            bool flip = endOne == endTwo;
            linkOne[ndxOne] = flip ? ~ndxTwo : ndxTwo;
            linkTwo[ndxTwo] = flip ? ~ndxOne : ndxOne;
            if (--remaining == 0)
            {
                break;
            }
        }

        rIndex = 0;
        do
        {
            bool forward = true;
            bool first = true;
            int sIndex = sLink[rIndex];
            sLink[rIndex] = SkPathOps.SkMaxS32;
            int eIndex;
            if (sIndex < 0)
            {
                eIndex = sLink[~sIndex];
                sLink[~sIndex] = SkPathOps.SkMaxS32;
            }
            else
            {
                eIndex = eLink[sIndex];
                eLink[sIndex] = SkPathOps.SkMaxS32;
            }

            do
            {
                Path contour = _partials[rIndex];
                if (!first)
                {
                    PathPoint next;
                    if (!SkPathOps.SkPathGetLastPt(_pathPtr, out PathPoint prior))
                    {
                        return;
                    }

                    if (forward)
                    {
                        next = SkPathOps.SkPathGetPoint(contour, 0);
                    }
                    else
                    {
                        _ = SkPathOps.SkPathGetLastPt(contour, out next);
                    }

                    if (prior != next)
                    {
                        /* TODO: if there is a gap between open path written so far and path to come,
                           connect by following segments from one to the other, rather than introducing
                           a diagonal to connect the two.
                         */
                    }
                }

                if (forward)
                {
                    SkPathOps.SkPathAddPath(_pathPtr, contour, extend: !first);
                }
                else
                {
                    SkPathOps.SkPathReversePathTo(_pathPtr, contour);
                }

                if (first)
                {
                    first = false;
                }

                if (sIndex == (((rIndex != eIndex) ^ forward) ? eIndex : ~eIndex))
                {
                    _pathPtr.Close();
                    break;
                }

                if (forward)
                {
                    eIndex = eLink[rIndex];
                    eLink[rIndex] = SkPathOps.SkMaxS32;
                    if (eIndex >= 0)
                    {
                        sLink[eIndex] = SkPathOps.SkMaxS32;
                    }
                    else
                    {
                        eLink[~eIndex] = SkPathOps.SkMaxS32;
                    }
                }
                else
                {
                    eIndex = sLink[rIndex];
                    sLink[rIndex] = SkPathOps.SkMaxS32;
                    if (eIndex >= 0)
                    {
                        eLink[eIndex] = SkPathOps.SkMaxS32;
                    }
                    else
                    {
                        sLink[~eIndex] = SkPathOps.SkMaxS32;
                    }
                }

                rIndex = eIndex;
                if (rIndex < 0)
                {
                    forward ^= true;
                    rIndex = ~rIndex;
                }
            }
            while (true);

            for (rIndex = 0; rIndex < linkCount; ++rIndex)
            {
                if (sLink[rIndex] != SkPathOps.SkMaxS32)
                {
                    break;
                }
            }
        }
        while (rIndex < linkCount);
    }
}
