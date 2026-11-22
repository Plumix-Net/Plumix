namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkOpEdgeBuilder.h and SkOpEdgeBuilder.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. SkPath is Plumix.UI.Path; preFetch walks its raw verbs, points and conic weights exactly as
// SkPathPriv::Iterate does (pts is the verb's own point for kMove and the point before it otherwise; nothing is
// iterated for a non-finite path, which preFetch rejects first). fPathVerbs is a List<byte> holding SkPath::Verb
// values plus kDone_Verb (6), which PathVerb lacks; fPathPts and fWeights are Lists indexed where Skia walks
// pointers. Splitsville is a small class array. debugValidate is omitted.

internal sealed class SkOpEdgeBuilder
{
    private const byte KDoneVerb = 6; // SkPath::kDone_Verb

    private readonly SkOpGlobalState _globalState;
    private Path _path;
    private readonly List<PathPoint> _pathPts = new List<PathPoint>();
    private readonly List<float> _weights = new List<float>();
    private readonly List<byte> _pathVerbs = new List<byte>();
    private readonly SkOpContourBuilder _contourBuilder;
    private readonly SkOpContourHead _contoursHead;
    private readonly SkPathOpsMask[] _xorMask = new SkPathOpsMask[2];
    private int _secondHalf;
    private bool _operand;
    private readonly bool _allowOpenContours;
    private bool _unparseable;

    public SkOpEdgeBuilder(SkPathWriter path, SkOpContourHead contours2, SkOpGlobalState globalState)
    {
        _globalState = globalState;
        _path = path.NativePath();
        _contourBuilder = new SkOpContourBuilder(contours2);
        _contoursHead = contours2;
        _allowOpenContours = true;
        Init();
    }

    public SkOpEdgeBuilder(Path path, SkOpContourHead contours2, SkOpGlobalState globalState)
    {
        _globalState = globalState;
        _path = path;
        _contourBuilder = new SkOpContourBuilder(contours2);
        _contoursHead = contours2;
        _allowOpenContours = false;
        Init();
    }

    public void Complete()
    {
        _contourBuilder.Flush();
        SkOpContour? contour = _contourBuilder.Contour();
        if (contour != null && contour.Count() != 0)
        {
            contour.Complete();
            _contourBuilder.SetContour(null);
        }
    }

    public SkOpContour Head()
    {
        return _contoursHead;
    }

    public bool Unparseable()
    {
        return _unparseable;
    }

    public SkPathOpsMask XorMask()
    {
        return _xorMask[_operand ? 1 : 0];
    }

    // SkOpEdgeBuilder.cpp

    public void Init()
    {
        _operand = false;
        _xorMask[0] = _xorMask[1] = ((int)_path.FillType & 1) != 0 ? SkPathOpsMask.EvenOdd : SkPathOpsMask.Winding;
        _unparseable = false;
        _secondHalf = PreFetch();
    }

    public void AddOperand(Path path)
    {
        _pathVerbs.RemoveAt(_pathVerbs.Count - 1);
        _path = path;
        _xorMask[1] = ((int)_path.FillType & 1) != 0 ? SkPathOpsMask.EvenOdd : SkPathOpsMask.Winding;
        _ = PreFetch();
    }

    public bool Finish()
    {
        _operand = false;
        if (_unparseable || !Walk())
        {
            return false;
        }

        Complete();
        SkOpContour? contour = _contourBuilder.Contour();
        if (contour != null && contour.Count() == 0)
        {
            _contoursHead.Remove(contour);
        }

        return true;
    }

    private void CloseContour(PathPoint curveEnd, PathPoint curveStart)
    {
        if (!SkDPoint.ApproximatelyEqual(curveEnd, curveStart))
        {
            _pathVerbs.Add((byte)PathVerb.Line);
            _pathPts.Add(curveStart);
        }
        else
        {
            int verbCount = _pathVerbs.Count;
            int ptsCount = _pathPts.Count;
            if ((byte)PathVerb.Line == _pathVerbs[verbCount - 1]
                && _pathPts[ptsCount - 2] == curveStart)
            {
                _pathVerbs.RemoveAt(_pathVerbs.Count - 1);
                _pathPts.RemoveAt(_pathPts.Count - 1);
            }
            else
            {
                _pathPts[ptsCount - 1] = curveStart;
            }
        }

        _pathVerbs.Add((byte)PathVerb.Close);
    }

    private int PreFetch()
    {
        if (!SkPathOps.SkPathIsFinite(_path))
        {
            _unparseable = true;
            return 0;
        }

        PathPoint curveStart = default;
        Span<PathPoint> curve = stackalloc PathPoint[4];
        bool lastCurve = false;
        IReadOnlyList<PathVerb> pathVerbs = _path.Verbs;
        IReadOnlyList<PathPoint> pathPoints = _path.Points;
        IReadOnlyList<float> pathWeights = _path.ConicWeights;
        int pointIndex = 0;
        int weightIndex = 0;
        for (int verbIndex = 0; verbIndex < pathVerbs.Count; ++verbIndex)
        {
            PathVerb pathVerb = pathVerbs[verbIndex];
            // SkPathPriv::Iterate: pts backs up one point for every verb but kMove
            int pts = pathVerb == PathVerb.Move ? pointIndex : pointIndex - 1;
            int w = weightIndex;
            pointIndex += SkPathOps.SkPathPtsInVerb(pathVerb);
            if (pathVerb == PathVerb.Conic)
            {
                ++weightIndex;
            }

            PathVerb verb = pathVerb;
            switch (verb)
            {
                case PathVerb.Move:
                    if (!_allowOpenContours && lastCurve)
                    {
                        CloseContour(curve[0], curveStart);
                    }

                    _pathVerbs.Add((byte)verb);
                    curve[0] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts]);
                    _pathPts.Add(curve[0]);
                    curveStart = curve[0];
                    lastCurve = false;
                    continue;
                case PathVerb.Line:
                    curve[1] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 1]);
                    if (SkDPoint.ApproximatelyEqual(curve[0], curve[1]))
                    {
                        byte lastVerb = _pathVerbs[^1];
                        if (lastVerb != (byte)PathVerb.Line && lastVerb != (byte)PathVerb.Move)
                        {
                            _pathPts[^1] = curve[0] = curve[1];
                        }

                        continue; // skip degenerate points
                    }

                    break;
                case PathVerb.Quad:
                    curve[1] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 1]);
                    curve[2] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 2]);
                    verb = SkReduceOrder.Quad(curve, curve);
                    if (verb == PathVerb.Move)
                    {
                        continue; // skip degenerate points
                    }

                    break;
                case PathVerb.Conic:
                    curve[1] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 1]);
                    curve[2] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 2]);
                    verb = SkReduceOrder.Quad(curve, curve);
                    if (PathVerb.Quad == verb && 1 != pathWeights[w])
                    {
                        verb = PathVerb.Conic;
                    }
                    else if (verb == PathVerb.Move)
                    {
                        continue; // skip degenerate points
                    }

                    break;
                case PathVerb.Cubic:
                    curve[1] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 1]);
                    curve[2] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 2]);
                    curve[3] = SkOpEdgeBuilderStatics.ForceSmallToZero(pathPoints[pts + 3]);
                    verb = SkReduceOrder.Cubic(curve, curve);
                    if (verb == PathVerb.Move)
                    {
                        continue; // skip degenerate points
                    }

                    break;
                case PathVerb.Close:
                    CloseContour(curve[0], curveStart);
                    lastCurve = false;
                    continue;
            }

            _pathVerbs.Add((byte)verb);
            int ptCount = SkPathOps.SkPathOpsVerbToPoints(verb);
            for (int index = 1; index <= ptCount; ++index)
            {
                _pathPts.Add(curve[index]);
            }

            if (verb == PathVerb.Conic)
            {
                _weights.Add(pathWeights[w]);
            }

            curve[0] = curve[ptCount];
            lastCurve = true;
        }

        if (!_allowOpenContours && lastCurve)
        {
            CloseContour(curve[0], curveStart);
        }

        _pathVerbs.Add(KDoneVerb);
        return _pathVerbs.Count - 1;
    }

    private bool Close()
    {
        Complete();
        return true;
    }

    private bool Walk()
    {
        int verbPtr = 0;
        int endOfFirstHalf = _secondHalf;
        int pointsPtr = 0;
        int weightPtr = 0;
        byte verb;
        SkOpContour? contour = _contourBuilder.Contour();
        int moveToPtrBump = 0;
        PathPoint[] pathPts = _pathPts.ToArray();
        Span<PathPoint> pair = stackalloc PathPoint[5];
        Span<PathPoint> cStorage0 = stackalloc PathPoint[3];
        Span<PathPoint> cStorage1 = stackalloc PathPoint[3];
        Span<PathPoint> conicPair = stackalloc PathPoint[5];
        Span<PathPoint> pair1Pts = stackalloc PathPoint[3];
        Span<float> splitT = stackalloc float[3];
        SkOpEdgeBuilderSplitsville[] splits =
        {
            new SkOpEdgeBuilderSplitsville(), new SkOpEdgeBuilderSplitsville(),
            new SkOpEdgeBuilderSplitsville(), new SkOpEdgeBuilderSplitsville(),
        };
        while ((verb = _pathVerbs[verbPtr]) != KDoneVerb)
        {
            if (verbPtr == endOfFirstHalf)
            {
                _operand = true;
            }

            verbPtr++;
            switch ((PathVerb)verb)
            {
                case PathVerb.Move:
                    if (contour != null && contour.Count() != 0)
                    {
                        if (_allowOpenContours)
                        {
                            Complete();
                        }
                        else if (!Close())
                        {
                            return false;
                        }
                    }

                    if (contour == null)
                    {
                        _contourBuilder.SetContour(contour = _contoursHead.AppendContour());
                    }

                    contour.Init(_globalState, _operand,
                        _xorMask[_operand ? 1 : 0] == SkPathOpsMask.EvenOdd);
                    pointsPtr += moveToPtrBump;
                    moveToPtrBump = 1;
                    continue;
                case PathVerb.Line:
                    _contourBuilder.AddLine(pathPts.AsSpan(pointsPtr));
                    break;
                case PathVerb.Quad:
                {
                    PathPoint vec1 = pathPts[pointsPtr + 1] - pathPts[pointsPtr];
                    PathPoint vec2 = pathPts[pointsPtr + 2] - pathPts[pointsPtr + 1];
                    if (vec1.X * vec2.X + vec1.Y * vec2.Y < 0)
                    {
                        if (SkPathOps.SkChopQuadAtMaxCurvature(pathPts.AsSpan(pointsPtr), pair) == 1)
                        {
                            goto addOneQuad;
                        }

                        if (!SkPathOps.SkScalarsAreFinite(
                                System.Runtime.InteropServices.MemoryMarshal.Cast<PathPoint, float>(pair),
                                pair.Length * 2))
                        {
                            return false;
                        }

                        for (int index = 0; index < pair.Length; ++index)
                        {
                            pair[index] = SkOpEdgeBuilderStatics.ForceSmallToZero(pair[index]);
                        }

                        PathVerb v1 = SkReduceOrder.Quad(pair, cStorage0);
                        PathVerb v2 = SkReduceOrder.Quad(pair[2..], cStorage1);
                        Span<PathPoint> curve1 = v1 != PathVerb.Line ? pair : cStorage0;
                        Span<PathPoint> curve2 = v2 != PathVerb.Line ? pair[2..] : cStorage1;
                        if (SkOpEdgeBuilderStatics.CanAddCurve(v1, curve1)
                            && SkOpEdgeBuilderStatics.CanAddCurve(v2, curve2))
                        {
                            _contourBuilder.AddCurve(v1, curve1);
                            _contourBuilder.AddCurve(v2, curve2);
                            break;
                        }
                    }
                }

                addOneQuad:
                    _contourBuilder.AddQuad(pathPts.AsSpan(pointsPtr));
                    break;
                case PathVerb.Conic:
                {
                    PathPoint vec1 = pathPts[pointsPtr + 1] - pathPts[pointsPtr];
                    PathPoint vec2 = pathPts[pointsPtr + 2] - pathPts[pointsPtr + 1];
                    float weight = _weights[weightPtr++];
                    if (vec1.X * vec2.X + vec1.Y * vec2.Y < 0)
                    {
                        // FIXME: max curvature for conics hasn't been implemented; use placeholder
                        float maxCurvature = SkPathOps.SkFindQuadMaxCurvature(pathPts.AsSpan(pointsPtr));
                        if (0 < maxCurvature && maxCurvature < 1)
                        {
                            // conicPair holds SkConic pair[2]: pair[0].fPts = [0..3), pair[1].fPts = [2..5)
                            if (!SkPathOps.SkConicChopAt(pathPts.AsSpan(pointsPtr), weight, maxCurvature,
                                    conicPair, out float pair0W, out float pair1W))
                            {
                                // if result can't be computed, use original
                                _contourBuilder.AddConic(pathPts.AsSpan(pointsPtr), weight);
                                break;
                            }

                            // SkConic pair[1] has its own copy of the shared point
                            Span<PathPoint> pair0Pts = conicPair[..3];
                            conicPair[2..5].CopyTo(pair1Pts);
                            PathVerb v1 = SkReduceOrder.Conic(pair0Pts, pair0W, cStorage0);
                            PathVerb v2 = SkReduceOrder.Conic(pair1Pts, pair1W, cStorage1);
                            Span<PathPoint> curve1 = v1 != PathVerb.Line ? pair0Pts : cStorage0;
                            Span<PathPoint> curve2 = v2 != PathVerb.Line ? pair1Pts : cStorage1;
                            if (SkOpEdgeBuilderStatics.CanAddCurve(v1, curve1)
                                && SkOpEdgeBuilderStatics.CanAddCurve(v2, curve2))
                            {
                                _contourBuilder.AddCurve(v1, curve1, pair0W);
                                _contourBuilder.AddCurve(v2, curve2, pair1W);
                                break;
                            }
                        }
                    }

                    _contourBuilder.AddConic(pathPts.AsSpan(pointsPtr), weight);
                }

                    break;
                case PathVerb.Cubic:
                {
                    // Split complex cubics (such as self-intersecting curves or
                    // ones with difficult curvature) in two before proceeding.
                    // This can be required for intersection to succeed.
                    int breaks = SkDCubic.ComplexBreak(pathPts.AsSpan(pointsPtr), splitT);
                    if (breaks == 0)
                    {
                        _contourBuilder.AddCubic(pathPts.AsSpan(pointsPtr));
                        break;
                    }

                    SkPathOps.SkTQSort(splitT[..breaks], static (float a, float b) => a < b);
                    for (int index = 0; index <= breaks; ++index)
                    {
                        SkOpEdgeBuilderSplitsville split = splits[index];
                        split.T[0] = index != 0 ? splitT[index - 1] : 0;
                        split.T[1] = index < breaks ? splitT[index] : 1;
                        SkDCubic part = SkDCubic.SubDivide(pathPts.AsSpan(pointsPtr), split.T[0], split.T[1]);
                        if (!part.ToFloatPoints(split.Pts))
                        {
                            return false;
                        }

                        split.Verb = SkReduceOrder.Cubic(split.Pts, split.Reduced);
                        PathPoint[] curve = PathVerb.Cubic == split.Verb ? split.Pts : split.Reduced;
                        split.CanAdd = SkOpEdgeBuilderStatics.CanAddCurve(split.Verb, curve);
                    }

                    for (int index = 0; index <= breaks; ++index)
                    {
                        SkOpEdgeBuilderSplitsville split = splits[index];
                        if (!split.CanAdd)
                        {
                            continue;
                        }

                        int prior = index;
                        while (prior > 0 && !splits[prior - 1].CanAdd)
                        {
                            --prior;
                        }

                        if (prior < index)
                        {
                            split.T[0] = splits[prior].T[0];
                            split.Pts[0] = splits[prior].Pts[0];
                        }

                        int next = index;
                        int breakLimit = SkPathOps.StdMin(breaks, splits.Length - 1);
                        while (next < breakLimit && !splits[next + 1].CanAdd)
                        {
                            ++next;
                        }

                        if (next > index)
                        {
                            split.T[1] = splits[next].T[1];
                            split.Pts[3] = splits[next].Pts[3];
                        }

                        if (prior < index || next > index)
                        {
                            split.Verb = SkReduceOrder.Cubic(split.Pts, split.Reduced);
                        }

                        PathPoint[] curve = PathVerb.Cubic == split.Verb ? split.Pts : split.Reduced;
                        if (!SkOpEdgeBuilderStatics.CanAddCurve(split.Verb, curve))
                        {
                            return false;
                        }

                        _contourBuilder.AddCurve(split.Verb, curve);
                    }
                }

                    break;
                case PathVerb.Close:
                    if (!Close())
                    {
                        return false;
                    }

                    contour = null;
                    continue;
                default:
                    return false;
            }

            pointsPtr += SkPathOps.SkPathOpsVerbToPoints((PathVerb)verb);
        }

        _contourBuilder.Flush();
        if (contour != null && contour.Count() != 0 && !_allowOpenContours && !Close())
        {
            return false;
        }

        return true;
    }
}

// Skia's local struct Splitsville in SkOpEdgeBuilder::walk.
file sealed class SkOpEdgeBuilderSplitsville
{
    public readonly double[] T = new double[2];
    public readonly PathPoint[] Pts = new PathPoint[4];
    public readonly PathPoint[] Reduced = new PathPoint[4];
    public PathVerb Verb;
    public bool CanAdd;
}

file static class SkOpEdgeBuilderStatics
{
    // very tiny points cause numerical instability : don't allow them
    public static PathPoint ForceSmallToZero(PathPoint pt)
    {
        PathPoint ret = pt;
        if (SkPathOps.SkScalarAbs(ret.X) < SkPathOps.FltEpsilonOrderableErr)
        {
            ret = ret with { X = 0 };
        }

        if (SkPathOps.SkScalarAbs(ret.Y) < SkPathOps.FltEpsilonOrderableErr)
        {
            ret = ret with { Y = 0 };
        }

        return ret;
    }

    public static bool CanAddCurve(PathVerb verb, Span<PathPoint> curve)
    {
        if (PathVerb.Move == verb)
        {
            return false;
        }

        for (int index = 0; index <= SkPathOps.SkPathOpsVerbToPoints(verb); ++index)
        {
            curve[index] = ForceSmallToZero(curve[index]);
        }

        return PathVerb.Line != verb || !SkDPoint.ApproximatelyEqual(curve[0], curve[1]);
    }
}
