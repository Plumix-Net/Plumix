namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/pathops/SkIntersections.h and SkIntersections.cpp at chrome/m119 (the
// Skia inside SkiaSharp 3.119), behind Path.Combine. Line for line: Skia's names, operation order and float/double
// types are kept. SkIntersections is partial: horizontal/vertical/intersect/intersectRay and the
// Horizontal/VerticalIntercept statics come with SkDLineIntersection.cpp, SkDQuadLineIntersection.cpp,
// SkDConicLineIntersection.cpp, SkDCubicLineIntersection.cpp and SkPathOpsTSect.cpp. Methods Skia declares but
// never defines (alignQuadPts, cleanUpCoincidence, cubicInsert, cubicCheckCoincidence, cubicExactEnd, cubicNearEnd,
// the tRange overloads of horizontal) are not ported.

internal sealed partial class SkIntersections
{
    // FIXME: since scans store points as SkPoint, this should also
    private readonly SkDPoint[] _pt = new SkDPoint[13];

    // used by nearly same to store alternate intersection point
    private readonly SkDPoint[] _pt2 = new SkDPoint[2];
    private readonly double[][] _t = { new double[13], new double[13] };

    // bit set for each curve's coincident T
    private readonly ushort[] _isCoincident = new ushort[2];

    // true if end points nearly match
    private readonly bool[] _nearlySame = new bool[2];
    private byte _used;
    private byte _max;
    private bool _allowNear;
    private bool _swap;

    public SkIntersections()
    {
        // The arrays start zeroed, as Skia's sk_bzero leaves them.
        _swap = false;
        Reset();
        _max = 0; // require that the caller set the max
    }

    // Skia's TArray operator[](int n): a read-only view of fT[n].
    public ReadOnlySpan<double> this[int n] => _t[n];

    public void AllowNear(bool nearAllowed)
    {
        _allowNear = nearAllowed;
    }

    public void ClearCoincidence(int index)
    {
        int bit = 1 << index;
        _isCoincident[0] = (ushort)(_isCoincident[0] & ~bit);
        _isCoincident[1] = (ushort)(_isCoincident[1] & ~bit);
    }

    public int ConicHorizontal(
        ReadOnlySpan<PathPoint> a,
        float weight,
        float left,
        float right,
        float y,
        bool flipped)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        _max = 2;
        return Horizontal(conic, left, right, y, flipped);
    }

    public int ConicVertical(
        ReadOnlySpan<PathPoint> a,
        float weight,
        float top,
        float bottom,
        float x,
        bool flipped)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        _max = 2;
        return Vertical(conic, top, bottom, x, flipped);
    }

    public int ConicLine(ReadOnlySpan<PathPoint> a, float weight, ReadOnlySpan<PathPoint> b)
    {
        SkDConic conic = default;
        conic.Set(a, weight);
        SkDLine line = default;
        line.Set(b);
        _max = 3; // 2;  permit small coincident segment + non-coincident intersection
        return Intersect(conic, line);
    }

    public int CubicHorizontal(ReadOnlySpan<PathPoint> a, float left, float right, float y, bool flipped)
    {
        SkDCubic cubic = default;
        cubic.Set(a);
        _max = 3;
        return Horizontal(cubic, left, right, y, flipped);
    }

    public int CubicVertical(ReadOnlySpan<PathPoint> a, float top, float bottom, float x, bool flipped)
    {
        SkDCubic cubic = default;
        cubic.Set(a);
        _max = 3;
        return Vertical(cubic, top, bottom, x, flipped);
    }

    public int CubicLine(ReadOnlySpan<PathPoint> a, ReadOnlySpan<PathPoint> b)
    {
        SkDCubic cubic = default;
        cubic.Set(a);
        SkDLine line = default;
        line.Set(b);
        _max = 3;
        return Intersect(cubic, line);
    }

    public bool HasT(double t)
    {
        return _used > 0 && (t == 0 ? _t[0][0] == 0 : _t[0][_used - 1] == 1);
    }

    public bool HasOppT(double t)
    {
        return _used > 0 && (_t[1][0] == t || _t[1][_used - 1] == t);
    }

    public int InsertSwap(double one, double two, in SkDPoint pt)
    {
        if (_swap)
        {
            return Insert(two, one, pt);
        }
        else
        {
            return Insert(one, two, pt);
        }
    }

    public bool IsCoincident(int index)
    {
        return (_isCoincident[0] & 1 << index) != 0;
    }

    public int LineHorizontal(ReadOnlySpan<PathPoint> a, float left, float right, float y, bool flipped)
    {
        SkDLine line = default;
        line.Set(a);
        _max = 2;
        return Horizontal(line, left, right, y, flipped);
    }

    public int LineVertical(ReadOnlySpan<PathPoint> a, float top, float bottom, float x, bool flipped)
    {
        SkDLine line = default;
        line.Set(a);
        _max = 2;
        return Vertical(line, top, bottom, x, flipped);
    }

    public int LineLine(ReadOnlySpan<PathPoint> a, ReadOnlySpan<PathPoint> b)
    {
        SkDLine aLine = default;
        SkDLine bLine = default;
        aLine.Set(a);
        bLine.Set(b);
        _max = 2;
        return Intersect(aLine, bLine);
    }

    public bool NearlySame(int index)
    {
        return _nearlySame[index];
    }

    public ref readonly SkDPoint Pt(int index)
    {
        return ref _pt[index];
    }

    public ref readonly SkDPoint Pt2(int index)
    {
        return ref _pt2[index];
    }

    public int QuadHorizontal(ReadOnlySpan<PathPoint> a, float left, float right, float y, bool flipped)
    {
        SkDQuad quad = default;
        quad.Set(a);
        _max = 2;
        return Horizontal(quad, left, right, y, flipped);
    }

    public int QuadVertical(ReadOnlySpan<PathPoint> a, float top, float bottom, float x, bool flipped)
    {
        SkDQuad quad = default;
        quad.Set(a);
        _max = 2;
        return Vertical(quad, top, bottom, x, flipped);
    }

    public int QuadLine(ReadOnlySpan<PathPoint> a, ReadOnlySpan<PathPoint> b)
    {
        SkDQuad quad = default;
        quad.Set(a);
        SkDLine line = default;
        line.Set(b);
        return Intersect(quad, line);
    }

    // leaves swap, max alone
    public void Reset()
    {
        _allowNear = true;
        _used = 0;
        _isCoincident[0] = 0;
        _isCoincident[1] = 0;
    }

    public void Set(bool swap, int tIndex, double t)
    {
        _t[swap ? 1 : 0][tIndex] = t;
    }

    public void SetMax(int max)
    {
        _max = (byte)max;
    }

    public void Swap()
    {
        _swap ^= true;
    }

    public bool Swapped()
    {
        return _swap;
    }

    public int Used()
    {
        return _used;
    }

    // Debug-only depth tracking in Skia (SkASSERT(--fDepth >= 0)); empty in release.
    public void DownDepth()
    {
    }

    public bool UnBumpT(int index)
    {
        _t[0][index] = _t[0][index] * (1 + SkPathOps.BumpEpsilon * 2) - SkPathOps.BumpEpsilon;
        if (!SkPathOps.Between(0, _t[0][index], 1))
        {
            _used = 0;
            return false;
        }

        return true;
    }

    // Debug-only depth tracking in Skia (SkASSERT(++fDepth < 16)); empty in release.
    public void UpDepth()
    {
    }

    public int IntersectRay(SkTCurve tCurve, in SkDLine line)
    {
        return tCurve.IntersectRay(this, line);
    }

    // Skia returns fDepth only in debug builds.
    public int Depth()
    {
        return 0;
    }

    // SkIntersections.cpp

    public int ClosestTo(double rangeStart, double rangeEnd, in SkDPoint testPt, out double closestDist)
    {
        int closest = -1;
        closestDist = SkPathOps.SkScalarMax;
        for (int index = 0; index < _used; ++index)
        {
            if (!SkPathOps.Between(rangeStart, _t[0][index], rangeEnd))
            {
                continue;
            }

            ref readonly SkDPoint iPt = ref _pt[index];
            double dist = testPt.DistanceSquared(iPt);
            if (closestDist > dist)
            {
                closestDist = dist;
                closest = index;
            }
        }

        return closest;
    }

    public void Flip()
    {
        for (int index = 0; index < _used; ++index)
        {
            _t[1][index] = 1 - _t[1][index];
        }
    }

    // FIXME : does not respect swap
    public int Insert(double one, double two, in SkDPoint pt)
    {
        if (_isCoincident[0] == 3 && SkPathOps.Between(_t[0][0], one, _t[0][1]))
        {
            // For now, don't allow a mix of coincident and non-coincident intersections
            return -1;
        }

        int index;
        for (index = 0; index < _used; ++index)
        {
            double oldOne = _t[0][index];
            double oldTwo = _t[1][index];
            if (one == oldOne && two == oldTwo)
            {
                return -1;
            }

            if (SkPathOps.MoreRoughlyEqual(oldOne, one) && SkPathOps.MoreRoughlyEqual(oldTwo, two))
            {
                if ((!SkPathOps.PreciselyZero(one) || SkPathOps.PreciselyZero(oldOne))
                    && (!SkPathOps.PreciselyEqual(one, 1) || SkPathOps.PreciselyEqual(oldOne, 1))
                    && (!SkPathOps.PreciselyZero(two) || SkPathOps.PreciselyZero(oldTwo))
                    && (!SkPathOps.PreciselyEqual(two, 1) || SkPathOps.PreciselyEqual(oldTwo, 1)))
                {
                    return -1;
                }

                // remove this and reinsert below in case replacing would make list unsorted
                int remaining = _used - index - 1;
                Array.Copy(_pt, index + 1, _pt, index, remaining);
                Array.Copy(_t[0], index + 1, _t[0], index, remaining);
                Array.Copy(_t[1], index + 1, _t[1], index, remaining);
                int clearMask = ~((1 << index) - 1);
                _isCoincident[0] = (ushort)(_isCoincident[0] - ((_isCoincident[0] >> 1) & clearMask));
                _isCoincident[1] = (ushort)(_isCoincident[1] - ((_isCoincident[1] >> 1) & clearMask));
                --_used;
                break;
            }
        }

        for (index = 0; index < _used; ++index)
        {
            if (_t[0][index] > one)
            {
                break;
            }
        }

        if (_used >= _max)
        {
            // FIXME : this error, if it is to be handled at runtime in release, must
            // be propagated all the way back down to the caller, and return failure.
            _used = 0;
            return 0;
        }

        int remaining2 = _used - index; // Skia's second `remaining`
        if (remaining2 > 0)
        {
            Array.Copy(_pt, index, _pt, index + 1, remaining2);
            Array.Copy(_t[0], index, _t[0], index + 1, remaining2);
            Array.Copy(_t[1], index, _t[1], index + 1, remaining2);
            int clearMask = ~((1 << index) - 1);
            _isCoincident[0] = (ushort)(_isCoincident[0] + (_isCoincident[0] & clearMask));
            _isCoincident[1] = (ushort)(_isCoincident[1] + (_isCoincident[1] & clearMask));
        }

        _pt[index] = pt;
        if (one < 0 || one > 1)
        {
            return -1;
        }

        if (two < 0 || two > 1)
        {
            return -1;
        }

        _t[0][index] = one;
        _t[1][index] = two;
        ++_used;
        return index;
    }

    public void InsertNear(double one, double two, in SkDPoint pt1, in SkDPoint pt2)
    {
        _nearlySame[one != 0 ? 1 : 0] = true;
        _ = Insert(one, two, pt1);
        _pt2[one != 0 ? 1 : 0] = pt2;
    }

    // start if index == 0 : end if index == 1
    public int InsertCoincident(double one, double two, in SkDPoint pt)
    {
        int index = InsertSwap(one, two, pt);
        if (index >= 0)
        {
            SetCoincident(index);
        }

        return index;
    }

    public void SetCoincident(int index)
    {
        int bit = 1 << index;
        _isCoincident[0] = (ushort)(_isCoincident[0] | bit);
        _isCoincident[1] = (ushort)(_isCoincident[1] | bit);
    }

    public void Merge(SkIntersections a, int aIndex, SkIntersections b, int bIndex)
    {
        Reset();
        _t[0][0] = a._t[0][aIndex];
        _t[1][0] = b._t[0][bIndex];
        _pt[0] = a._pt[aIndex];
        _pt2[0] = b._pt[bIndex];
        _used = 1;
    }

    public int MostOutside(double rangeStart, double rangeEnd, in SkDPoint origin)
    {
        int result = -1;
        for (int index = 0; index < _used; ++index)
        {
            if (!SkPathOps.Between(rangeStart, _t[0][index], rangeEnd))
            {
                continue;
            }

            if (result < 0)
            {
                result = index;
                continue;
            }

            SkDVector best = _pt[result] - origin;
            SkDVector test = _pt[index] - origin;
            if (test.CrossCheck(best) < 0)
            {
                result = index;
            }
        }

        return result;
    }

    public void RemoveOne(int index)
    {
        int remaining = --_used - index;
        if (remaining <= 0)
        {
            return;
        }

        Array.Copy(_pt, index + 1, _pt, index, remaining);
        Array.Copy(_t[0], index + 1, _t[0], index, remaining);
        Array.Copy(_t[1], index + 1, _t[1], index, remaining);
        int coBit = _isCoincident[0] & (1 << index);
        _isCoincident[0] = (ushort)(_isCoincident[0] - (((_isCoincident[0] >> 1) & ~((1 << index) - 1)) + coBit));
        _isCoincident[1] = (ushort)(_isCoincident[1] - (((_isCoincident[1] >> 1) & ~((1 << index) - 1)) + coBit));
    }
}
