namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of the SkPath members of skia/src/core/SkPath.cpp, include/core/SkPath.h and
// src/core/SkPathPriv.h that pathops calls (the edge builder, SkPathWriter, Op and Simplify), at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float types
// are kept. SkPath is Plumix.UI.Path: these read its raw Verbs/Points/ConicWeights (the SkPathRef arrays) and write
// through its public builders, which narrow doubles to float exactly and port SkPath's moveTo/lineTo/quadTo/conicTo/
// cubicTo/close (including injectMoveToIfNeeded). addPath in kAppend_AddPathMode with the identity matrix copies
// the verbs, points and weights unchanged in Skia; replaying them through the builders yields the same arrays and the
// same fLastMoveToIndex, because every path pathops appends starts with a moveTo. fLastMoveToIndex itself is private
// to Path; SkPathLastMoveToIndex rebuilds it from the verbs exactly as moveTo and close maintain it. Path keeps no
// convexity or first-direction cache, so isConvex always computes (computeConvexity with getFirstDirection() ==
// kUnknown); the cache only matters for a path whose points are colinear off-axis with non-empty bounds, which
// addRect/addOval/addRRect never produce.

internal enum SkPathConvexity
{
    Convex,
    Concave,
    Unknown,
}

internal enum SkPathFirstDirection
{
    CW, // == SkPathDirection::kCW
    CCW, // == SkPathDirection::kCCW
    Unknown,
}

internal static partial class SkPathOps
{
    // SkPathPriv::PtsInVerb.
    public static int SkPathPtsInVerb(PathVerb verb)
    {
        switch (verb)
        {
            case PathVerb.Move:
                return 1;
            case PathVerb.Line:
                return 1;
            case PathVerb.Quad:
                return 2;
            case PathVerb.Conic:
                return 2;
            case PathVerb.Cubic:
                return 3;
            default:
                return 0;
        }
    }

    // SkPoint::isFinite.
    public static bool SkPointIsFinite(PathPoint pt)
    {
        return SkScalarIsFinite(pt.X) && SkScalarIsFinite(pt.Y);
    }

    // SkPath::isEmpty.
    public static bool SkPathIsEmpty(Path path)
    {
        return 0 == path.Verbs.Count;
    }

    // SkPath::isFinite (SkPathRef::isFinite: the points' bounds are finite).
    public static bool SkPathIsFinite(Path path)
    {
        SkPathOpsBounds bounds = default;
        return SkPathComputePtBounds(path, ref bounds);
    }

    // SkPathRef's compute_pt_bounds.
    private static bool SkPathComputePtBounds(Path path, ref SkPathOpsBounds bounds)
    {
        int count = path.Points.Count;
        PathPoint[] points = new PathPoint[count];
        for (int index = 0; index < count; ++index)
        {
            points[index] = path.Points[index];
        }

        return bounds.SetBoundsCheck(points, count);
    }

    // SkPath::getPoint.
    public static PathPoint SkPathGetPoint(Path path, int index)
    {
        if ((uint)index < (uint)path.Points.Count)
        {
            return path.Points[index];
        }

        return new PathPoint(0, 0);
    }

    // SkPath::getLastPt.
    public static bool SkPathGetLastPt(Path path, out PathPoint lastPt)
    {
        int count = path.Points.Count;
        if (count > 0)
        {
            lastPt = path.Points[count - 1];
            return true;
        }

        lastPt = new PathPoint(0, 0);
        return false;
    }

    // SkPath::fLastMoveToIndex, as SkPath::moveTo (the index of the move's point) and SkPath::close (~index once
    // the contour is closed) maintain it; ~0 for a path without verbs.
    public static int SkPathLastMoveToIndex(Path path)
    {
        int lastMoveToIndex = ~0;
        int pointIndex = 0;
        foreach (PathVerb verb in path.Verbs)
        {
            if (verb == PathVerb.Move)
            {
                lastMoveToIndex = pointIndex;
            }
            else if (verb == PathVerb.Close && lastMoveToIndex >= 0)
            {
                lastMoveToIndex = ~lastMoveToIndex;
            }

            pointIndex += SkPathPtsInVerb(verb);
        }

        return lastMoveToIndex;
    }

    public static void SkPathMoveTo(Path path, PathPoint pt)
    {
        path.MoveTo(pt.X, pt.Y);
    }

    public static void SkPathLineTo(Path path, PathPoint pt)
    {
        path.LineTo(pt.X, pt.Y);
    }

    public static void SkPathQuadTo(Path path, PathPoint pt1, PathPoint pt2)
    {
        path.QuadraticBezierTo(pt1.X, pt1.Y, pt2.X, pt2.Y);
    }

    public static void SkPathConicTo(Path path, PathPoint pt1, PathPoint pt2, float weight)
    {
        path.ConicTo(pt1.X, pt1.Y, pt2.X, pt2.Y, weight);
    }

    public static void SkPathCubicTo(Path path, PathPoint pt1, PathPoint pt2, PathPoint pt3)
    {
        path.CubicTo(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y);
    }

    // SkPath::addPath(srcPath, SkMatrix::I(), mode): kAppend_AddPathMode when extend is false,
    // kExtend_AddPathMode when it is true.
    public static void SkPathAddPath(Path dst, Path srcPath, bool extend)
    {
        if (SkPathIsEmpty(srcPath))
        {
            return;
        }

        // Detect if we're trying to add ourself
        Path src = ReferenceEquals(dst, srcPath) ? new Path(srcPath) : srcPath;
        bool firstVerb = true;
        int point = 0;
        int weight = 0;
        foreach (PathVerb verb in src.Verbs)
        {
            switch (verb)
            {
                case PathVerb.Move:
                {
                    PathPoint mappedPt = src.Points[point];
                    if (firstVerb && extend && !SkPathIsEmpty(dst))
                    {
                        SkPathInjectMoveToIfNeeded(dst); // In case last contour is closed
                        // don't add lineTo if it is degenerate
                        if (!SkPathGetLastPt(dst, out PathPoint lastPt) || lastPt != mappedPt)
                        {
                            SkPathLineTo(dst, mappedPt);
                        }
                    }
                    else
                    {
                        SkPathMoveTo(dst, mappedPt);
                    }

                    break;
                }

                case PathVerb.Line:
                    SkPathLineTo(dst, src.Points[point]);
                    break;
                case PathVerb.Quad:
                    SkPathQuadTo(dst, src.Points[point], src.Points[point + 1]);
                    break;
                case PathVerb.Conic:
                    SkPathConicTo(dst, src.Points[point], src.Points[point + 1], src.ConicWeights[weight++]);
                    break;
                case PathVerb.Cubic:
                    SkPathCubicTo(dst, src.Points[point], src.Points[point + 1], src.Points[point + 2]);
                    break;
                case PathVerb.Close:
                    dst.Close();
                    break;
            }

            point += SkPathPtsInVerb(verb);
            firstVerb = false;
        }
    }

    // SkPath::injectMoveToIfNeeded: the builders inject the move themselves before a lineTo; this makes the
    // injection happen first so the getLastPt that follows it sees the injected point, as in Skia.
    private static void SkPathInjectMoveToIfNeeded(Path path)
    {
        int lastMoveToIndex = SkPathLastMoveToIndex(path);
        if (lastMoveToIndex < 0)
        {
            PathPoint pt;
            if (path.Verbs.Count == 0)
            {
                pt = new PathPoint(0, 0);
            }
            else
            {
                pt = path.Points[~lastMoveToIndex];
            }

            SkPathMoveTo(path, pt);
        }
    }

    // ignore the last point of the 1st contour
    // SkPath::reversePathTo.
    public static void SkPathReversePathTo(Path dst, Path path)
    {
        if (path.Verbs.Count == 0)
        {
            return;
        }

        int verbs = path.Verbs.Count;
        int verbsBegin = 0;
        int pts = path.Points.Count - 1;
        int conicWeights = path.ConicWeights.Count;

        while (verbs > verbsBegin)
        {
            PathVerb v = path.Verbs[--verbs];
            pts -= SkPathPtsInVerb(v);
            switch (v)
            {
                case PathVerb.Move:
                    // if the path has multiple contours, stop after reversing the last
                    return;
                case PathVerb.Line:
                    SkPathLineTo(dst, path.Points[pts]);
                    break;
                case PathVerb.Quad:
                    SkPathQuadTo(dst, path.Points[pts + 1], path.Points[pts]);
                    break;
                case PathVerb.Conic:
                    SkPathConicTo(dst, path.Points[pts + 1], path.Points[pts], path.ConicWeights[--conicWeights]);
                    break;
                case PathVerb.Cubic:
                    SkPathCubicTo(dst, path.Points[pts + 2], path.Points[pts + 1], path.Points[pts]);
                    break;
                case PathVerb.Close:
                    break;
            }
        }
    }

    // SkPath::reverseAddPath.
    public static void SkPathReverseAddPath(Path dst, Path srcPath)
    {
        // Detect if we're trying to add ourself
        Path src = ReferenceEquals(dst, srcPath) ? new Path(srcPath) : srcPath;

        int verbsBegin = 0;
        int verbs = src.Verbs.Count;
        int pts = src.Points.Count;
        int conicWeights = src.ConicWeights.Count;

        bool needMove = true;
        bool needClose = false;
        while (verbs > verbsBegin)
        {
            PathVerb v = src.Verbs[--verbs];
            int n = SkPathPtsInVerb(v);

            if (needMove)
            {
                --pts;
                SkPathMoveTo(dst, src.Points[pts]);
                needMove = false;
            }

            pts -= n;
            switch (v)
            {
                case PathVerb.Move:
                    if (needClose)
                    {
                        dst.Close();
                        needClose = false;
                    }

                    needMove = true;
                    pts += 1; // so we see the point in "if (needMove)" above
                    break;
                case PathVerb.Line:
                    SkPathLineTo(dst, src.Points[pts]);
                    break;
                case PathVerb.Quad:
                    SkPathQuadTo(dst, src.Points[pts + 1], src.Points[pts]);
                    break;
                case PathVerb.Conic:
                    SkPathConicTo(dst, src.Points[pts + 1], src.Points[pts], src.ConicWeights[--conicWeights]);
                    break;
                case PathVerb.Cubic:
                    SkPathCubicTo(dst, src.Points[pts + 2], src.Points[pts + 1], src.Points[pts]);
                    break;
                case PathVerb.Close:
                    needClose = true;
                    break;
            }
        }
    }

    // SkPath::isRect(rect, nullptr, nullptr).
    public static bool SkPathIsRect(Path path, out SkRect rect)
    {
        int currVerb = 0;
        int pts = 0;
        return SkPathIsRectContour(path, false, ref currVerb, ref pts, out _, out _, out rect);
    }

    // SkPath.cpp's rect_make_dir.
    private static int SkPathRectMakeDir(float dx, float dy)
    {
        return (0 != dx ? 1 : 0) << 0 | (dx > 0 || dy > 0 ? 1 : 0) << 1;
    }

    // SkPathPriv::IsRectContour; the point pointers are indices into path.Points (-1 for nullptr) and direction
    // is 0 for kCW, 1 for kCCW.
    public static bool SkPathIsRectContour(
        Path path,
        bool allowPartial,
        ref int currVerb,
        ref int ptsPtr,
        out bool isClosed,
        out int direction,
        out SkRect rect)
    {
        isClosed = false;
        direction = 0;
        rect = default;
        int corners = 0;
        PathPoint closeXY; // used to determine if final line falls on a diagonal
        PathPoint lineStart; // used to construct line from previous point
        int firstPt = -1; // first point in the rect (last of first moves)
        int lastPt = -1; // last point in the rect (last of lines or first if closed)
        PathPoint firstCorner = default;
        PathPoint thirdCorner = default;
        int pts = ptsPtr;
        int savePts = -1; // used to allow caller to iterate through a pair of rects
        lineStart = new PathPoint(0, 0);
        Span<sbyte> directions = stackalloc sbyte[] { -1, -1, -1, -1, -1 }; // -1 to 3; -1 is uninitialized
        bool closedOrMoved = false;
        bool autoClose = false;
        bool insertClose = false;
        int verbCnt = path.Verbs.Count;
        while (currVerb < verbCnt && (!allowPartial || !autoClose))
        {
            PathVerb verb = insertClose ? PathVerb.Close : path.Verbs[currVerb];
            switch (verb)
            {
                case PathVerb.Close:
                case PathVerb.Line:
                {
                    if (verb == PathVerb.Close)
                    {
                        savePts = pts;
                        autoClose = true;
                        insertClose = false;
                    }

                    if (PathVerb.Close != verb)
                    {
                        lastPt = pts;
                    }

                    PathPoint lineEnd = PathVerb.Close == verb ? path.Points[firstPt] : path.Points[pts++];
                    PathPoint lineDelta = lineEnd - lineStart;
                    if (lineDelta.X != 0 && lineDelta.Y != 0)
                    {
                        return false; // diagonal
                    }

                    if (!SkPointIsFinite(lineDelta))
                    {
                        return false; // path contains infinity or NaN
                    }

                    if (lineStart == lineEnd)
                    {
                        break; // single point on side OK
                    }

                    int nextDirection = SkPathRectMakeDir(lineDelta.X, lineDelta.Y); // 0 to 3
                    if (0 == corners)
                    {
                        directions[0] = (sbyte)nextDirection;
                        corners = 1;
                        closedOrMoved = false;
                        lineStart = lineEnd;
                        break;
                    }

                    if (closedOrMoved)
                    {
                        return false; // closed followed by a line
                    }

                    if (autoClose && nextDirection == directions[0])
                    {
                        break; // colinear with first
                    }

                    closedOrMoved = autoClose;
                    if (directions[corners - 1] == nextDirection)
                    {
                        if (3 == corners && PathVerb.Line == verb)
                        {
                            thirdCorner = lineEnd;
                        }

                        lineStart = lineEnd;
                        break; // colinear segment
                    }

                    directions[corners++] = (sbyte)nextDirection;
                    // opposite lines must point in opposite directions; xoring them should equal 2
                    switch (corners)
                    {
                        case 2:
                            firstCorner = lineStart;
                            break;
                        case 3:
                            if ((directions[0] ^ directions[2]) != 2)
                            {
                                return false;
                            }

                            thirdCorner = lineEnd;
                            break;
                        case 4:
                            if ((directions[1] ^ directions[3]) != 2)
                            {
                                return false;
                            }

                            break;
                        default:
                            return false; // too many direction changes
                    }

                    lineStart = lineEnd;
                    break;
                }

                case PathVerb.Quad:
                case PathVerb.Conic:
                case PathVerb.Cubic:
                    return false; // quadratic, cubic not allowed
                case PathVerb.Move:
                    if (allowPartial && !autoClose && directions[0] >= 0)
                    {
                        insertClose = true;
                        currVerb -= 1; // try move again afterwards
                        goto addMissingClose;
                    }

                    if (corners == 0)
                    {
                        firstPt = pts;
                    }
                    else
                    {
                        closeXY = path.Points[firstPt] - path.Points[lastPt];
                        if (closeXY.X != 0 && closeXY.Y != 0)
                        {
                            return false; // we're diagonal, abort
                        }
                    }

                    lineStart = path.Points[pts++];
                    closedOrMoved = true;
                    break;
                default:
                    break;
            }

            currVerb += 1;
        addMissingClose:
            ;
        }

        // Success if 4 corners and first point equals last
        if (corners < 3 || corners > 4)
        {
            return false;
        }

        if (savePts >= 0)
        {
            ptsPtr = savePts;
        }

        // check if close generates diagonal
        closeXY = path.Points[firstPt] - path.Points[lastPt];
        if (closeXY.X != 0 && closeXY.Y != 0)
        {
            return false;
        }

        // SkRect::set(firstCorner, thirdCorner)
        rect = new SkRect(
            StdMin(firstCorner.X, thirdCorner.X),
            StdMin(firstCorner.Y, thirdCorner.Y),
            StdMax(firstCorner.X, thirdCorner.X),
            StdMax(firstCorner.Y, thirdCorner.Y));
        isClosed = autoClose;
        direction = directions[0] == ((directions[1] + 1) & 3) ? 0 : 1;
        return true;
    }

    // SkPath::isConvex: getConvexity() with no cached convexity, so computeConvexity().
    public static bool SkPathIsConvex(Path path)
    {
        return SkPathConvexity.Convex == SkPathComputeConvexity(path);
    }

    // SkPathPriv::LeadingMoveToCount.
    private static int SkPathLeadingMoveToCount(Path path)
    {
        int verbCount = path.Verbs.Count;
        for (int i = 0; i < verbCount; i++)
        {
            if (path.Verbs[i] != PathVerb.Move)
            {
                return i;
            }
        }

        return verbCount; // path is all move verbs
    }

    // SkPath::computeConvexity (the results it caches with setConvexity are returned instead).
    public static SkPathConvexity SkPathComputeConvexity(Path path)
    {
        SkPathOpsBounds bounds = default;
        if (!SkPathComputePtBounds(path, ref bounds))
        {
            return SkPathConvexity.Concave;
        }

        // pointCount potentially includes a block of leading moveTos and trailing moveTos. Convexity
        // only cares about the last of the initial moveTos and the verbs before the final moveTos.
        int pointCount = path.Points.Count;
        int skipCount = SkPathLeadingMoveToCount(path) - 1;

        int lastMoveToIndex = SkPathLastMoveToIndex(path);
        if (lastMoveToIndex >= 0)
        {
            if (lastMoveToIndex == pointCount - 1)
            {
                // Find the last real verb that affects convexity
                int verbs = path.Verbs.Count - 1;
                while (verbs > 0 && path.Verbs[verbs] == PathVerb.Move)
                {
                    verbs--;
                    pointCount--;
                }
            }
            else if (lastMoveToIndex != skipCount)
            {
                // There's an additional moveTo between two blocks of other verbs, so the path must have
                // more than one contour and cannot be convex.
                return SkPathConvexity.Concave;
            } // else no trailing or intermediate moveTos to worry about
        }

        int points = 0;
        if (skipCount > 0)
        {
            points += skipCount;
            pointCount -= skipCount;
        }

        // Check to see if path changes direction more than three times as quick concave test
        SkPathConvexity convexity = SkPathConvexicator.BySign(path, points, pointCount);
        if (SkPathConvexity.Convex != convexity)
        {
            return SkPathConvexity.Concave;
        }

        int contourCount = 0;
        bool needsClose = false;
        SkPathConvexicator state = new SkPathConvexicator();

        int pointIndex = 0;
        foreach (PathVerb verb in path.Verbs)
        {
            // Iterate's pts: the verb's own point for a move, the point before it otherwise.
            int pts = verb == PathVerb.Move ? pointIndex : pointIndex - 1;
            pointIndex += SkPathPtsInVerb(verb);
            // Looking for the last moveTo before non-move verbs start
            if (contourCount == 0)
            {
                if (verb == PathVerb.Move)
                {
                    state.SetMovePt(path.Points[pts]);
                }
                else
                {
                    // Starting the actual contour, fall through to c=1 to add the points
                    contourCount++;
                    needsClose = true;
                }
            }

            // Accumulating points into the Convexicator until we hit a close or another move
            if (contourCount == 1)
            {
                if (verb == PathVerb.Close || verb == PathVerb.Move)
                {
                    if (!state.Close())
                    {
                        return SkPathConvexity.Concave;
                    }

                    needsClose = false;
                    contourCount++;
                }
                else
                {
                    // lines add 1 point, cubics add 3, conics and quads add 2
                    int count = SkPathPtsInVerb(verb);
                    for (int i = 1; i <= count; ++i)
                    {
                        if (!state.AddPt(path.Points[pts + i]))
                        {
                            return SkPathConvexity.Concave;
                        }
                    }
                }
            }
            else
            {
                // The first contour has closed and anything other than spurious trailing moves means
                // there's multiple contours and the path can't be convex
                if (verb != PathVerb.Move)
                {
                    return SkPathConvexity.Concave;
                }
            }
        }

        // If the path isn't explicitly closed do so implicitly
        if (needsClose && !state.Close())
        {
            return SkPathConvexity.Concave;
        }

        // this->getFirstDirection() is always kUnknown here (Path caches no first direction)
        if (state.GetFirstDirection() == SkPathFirstDirection.Unknown
            && bounds.Left < bounds.Right && bounds.Top < bounds.Bottom)
        {
            return state.Reversals() < 3 ? SkPathConvexity.Convex : SkPathConvexity.Concave;
        }

        return SkPathConvexity.Convex;
    }
}

// SkPath.cpp's Convexicator: only valid for a single contour.
internal sealed class SkPathConvexicator
{
    private const int KValueNeverReturnedBySign = 2;

    private enum DirChange
    {
        Unknown,
        Left,
        Right,
        Straight,
        Backwards, // if double back, allow simple lines to be convex
        Invalid,
    }

    private PathPoint _firstPt = new PathPoint(0, 0); // The first point of the contour, e.g. moveTo(x,y)
    private PathPoint _firstVec = new PathPoint(0, 0); // The direction leaving fFirstPt to the next vertex

    private PathPoint _lastPt = new PathPoint(0, 0); // The last point passed to addPt()
    private PathPoint _lastVec = new PathPoint(0, 0); // The direction that brought the path to fLastPt

    private DirChange _expectedDir = DirChange.Invalid;
    private SkPathFirstDirection _firstDirection = SkPathFirstDirection.Unknown;
    private int _reversals;
    private bool _isFinite = true;

    /** The direction returned is only valid if the path is determined convex */
    public SkPathFirstDirection GetFirstDirection()
    {
        return _firstDirection;
    }

    public void SetMovePt(PathPoint pt)
    {
        _firstPt = _lastPt = pt;
        _expectedDir = DirChange.Invalid;
    }

    public bool AddPt(PathPoint pt)
    {
        if (_lastPt == pt)
        {
            return true;
        }

        // should only be true for first non-zero vector after setMovePt was called.
        if (_firstPt == _lastPt && _expectedDir == DirChange.Invalid)
        {
            _lastVec = pt - _lastPt;
            _firstVec = _lastVec;
        }
        else if (!AddVec(pt - _lastPt))
        {
            return false;
        }

        _lastPt = pt;
        return true;
    }

    // Convexicator::BySign over path.Points[points..points + count].
    public static SkPathConvexity BySign(Path path, int points, int count)
    {
        if (count <= 3)
        {
            // point, line, or triangle are always convex
            return SkPathConvexity.Convex;
        }

        int last = points + count;
        PathPoint currPt = path.Points[points++];
        PathPoint firstPt = currPt;
        int dxes = 0;
        int dyes = 0;
        int lastSx = KValueNeverReturnedBySign;
        int lastSy = KValueNeverReturnedBySign;
        for (int outerLoop = 0; outerLoop < 2; ++outerLoop)
        {
            // the second pass reads only firstPt (points = &firstPt), closing the contour
            while (outerLoop != 0 || points != last)
            {
                PathPoint vec = (outerLoop != 0 ? firstPt : path.Points[points]) - currPt;
                if (!(vec.X == 0 && vec.Y == 0))
                {
                    // give up if vector construction failed
                    if (!SkPathOps.SkPointIsFinite(vec))
                    {
                        return SkPathConvexity.Unknown;
                    }

                    int sx = Sign(vec.X);
                    int sy = Sign(vec.Y);
                    dxes += sx != lastSx ? 1 : 0;
                    dyes += sy != lastSy ? 1 : 0;
                    if (dxes > 3 || dyes > 3)
                    {
                        return SkPathConvexity.Concave;
                    }

                    lastSx = sx;
                    lastSy = sy;
                }

                currPt = outerLoop != 0 ? firstPt : path.Points[points];
                points++;
                if (outerLoop != 0)
                {
                    break;
                }
            }
        }

        return SkPathConvexity.Convex; // that is, it may be convex, don't know yet
    }

    public bool Close()
    {
        // If this was an explicit close, there was already a lineTo to fFirstPoint, so this
        // addPt() is a no-op. Otherwise, the addPt implicitly closes the contour. In either case,
        // we have to check the direction change along the first vector in case it is concave.
        return AddPt(_firstPt) && AddVec(_firstVec);
    }

    public bool IsFinite()
    {
        return _isFinite;
    }

    public int Reversals()
    {
        return _reversals;
    }

    // SkPath.cpp's sign: 1 for negative, 0 otherwise.
    private static int Sign(float x)
    {
        return x < 0 ? 1 : 0;
    }

    private DirChange DirectionChange(PathPoint curVec)
    {
        // SkPoint::CrossProduct
        float cross = _lastVec.X * curVec.Y - _lastVec.Y * curVec.X;
        if (!SkPathOps.SkScalarIsFinite(cross))
        {
            return DirChange.Unknown;
        }

        if (cross == 0)
        {
            return _lastVec.X * curVec.X + _lastVec.Y * curVec.Y < 0 ? DirChange.Backwards : DirChange.Straight;
        }

        // SkScalarSignAsInt
        return 1 == (cross < 0 ? -1 : cross > 0 ? 1 : 0) ? DirChange.Right : DirChange.Left;
    }

    private bool AddVec(PathPoint curVec)
    {
        DirChange dir = DirectionChange(curVec);
        switch (dir)
        {
            case DirChange.Left: // fall through
            case DirChange.Right:
                if (DirChange.Invalid == _expectedDir)
                {
                    _expectedDir = dir;
                    _firstDirection = (DirChange.Right == dir) ? SkPathFirstDirection.CW : SkPathFirstDirection.CCW;
                }
                else if (dir != _expectedDir)
                {
                    _firstDirection = SkPathFirstDirection.Unknown;
                    return false;
                }

                _lastVec = curVec;
                break;
            case DirChange.Straight:
                break;
            case DirChange.Backwards:
                //  allow path to reverse direction twice
                //    Given path.moveTo(0, 0); path.lineTo(1, 1);
                //    - 1st reversal: direction change formed by line (0,0 1,1), line (1,1 0,0)
                //    - 2nd reversal: direction change formed by line (1,1 0,0), line (0,0 1,1)
                _lastVec = curVec;
                return ++_reversals < 3;
            case DirChange.Unknown:
                return _isFinite = false;
            case DirChange.Invalid:
                throw new InvalidOperationException("Use of invalid direction change flag");
        }

        return true;
    }
}
