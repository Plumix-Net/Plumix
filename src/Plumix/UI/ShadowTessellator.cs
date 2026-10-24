using SkiaSharp;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Canvas.drawShadow)
// Engine-side port: Skia's src/utils/SkShadowTessellator.cpp, src/core/SkDrawShadowInfo.{h,cpp} and the
// SkInsetConvexPolygon/SkComputeRadialSteps helpers of src/utils/SkPolyUtils.cpp, at the Skia revision the
// pinned Flutter engine DEPS names (8df24be6). Curves are flattened the way a Ganesh-enabled build does
// (GrPathUtils + Wang's formula), which is how the Flutter engine compiles Skia. SkiaSharp does not bind
// SkShadowUtils, so the tessellation runs here and only the mesh rasterization stays native.

/// <summary>The float geometry Skia's shadow code is written in (SkPoint/SkVector).</summary>
internal readonly record struct ShadowPoint(float X, float Y)
{
    public static ShadowPoint operator +(ShadowPoint a, ShadowPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static ShadowPoint operator -(ShadowPoint a, ShadowPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static ShadowPoint operator -(ShadowPoint a) => new(-a.X, -a.Y);
    public static ShadowPoint operator *(ShadowPoint a, float s) => new(a.X * s, a.Y * s);

    public float Cross(ShadowPoint b) => (X * b.Y) - (Y * b.X);
    public float Dot(ShadowPoint b) => (X * b.X) + (Y * b.Y);
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);

    public static implicit operator SKPoint(ShadowPoint p) => new(p.X, p.Y);
    public static implicit operator ShadowPoint(SKPoint p) => new(p.X, p.Y);

    /// <summary>SkPoint::setLength: rescales in double precision, failing on a zero or non-finite result.</summary>
    public bool TrySetLength(float length, out ShadowPoint result)
    {
        double magnitude = Math.Sqrt(((double)X * X) + ((double)Y * Y));
        double scale = length / magnitude;
        float x = (float)(X * scale);
        float y = (float)(Y * scale);
        if (!float.IsFinite(x) || !float.IsFinite(y) || (x == 0.0f && y == 0.0f))
        {
            result = default;
            return false;
        }

        result = new ShadowPoint(x, y);
        return true;
    }

    public static float DistanceSquared(ShadowPoint a, ShadowPoint b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>SkPointPriv::DistanceToLineSegmentBetweenSqd.</summary>
    public static float DistanceToLineSegmentSquared(ShadowPoint point, ShadowPoint a, ShadowPoint b)
    {
        ShadowPoint u = b - a;
        ShadowPoint v = point - a;
        float uLengthSquared = u.Dot(u);
        float uDotV = u.Dot(v);
        if (uDotV <= 0.0f)
        {
            return v.Dot(v);
        }

        if (uDotV > uLengthSquared)
        {
            return DistanceSquared(b, point);
        }

        float det = u.Cross(v);
        float temp = det / uLengthSquared;
        temp *= det;
        return float.IsFinite(temp) ? temp : v.Dot(v);
    }
}

/// <summary>SkPoint3.</summary>
internal readonly record struct ShadowPoint3(float X, float Y, float Z)
{
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);
}

/// <summary>A triangle mesh whose vertex alpha is the shadow's coverage before the Gaussian falloff.</summary>
internal sealed record ShadowVertices(SKPoint[] Positions, SKColor[] Colors, ushort[] Indices);

/// <summary>SkDrawShadowMetrics.</summary>
internal static class ShadowMetrics
{
    public const float NearlyZero = 1.0f / (1 << 12);
    private const float AmbientHeightFactor = 1.0f / 128.0f;
    private const float AmbientGeomFactor = 64.0f;
    private const float MaxAmbientRadius = 300 * AmbientHeightFactor * AmbientGeomFactor;

    public static float AmbientBlurRadius(float height) =>
        Math.Min(height * AmbientHeightFactor * AmbientGeomFactor, MaxAmbientRadius);

    public static float AmbientRecipAlpha(float height) => 1.0f + Math.Max(height * AmbientHeightFactor, 0.0f);

    public static float SpotBlurRadius(float occluderZ, float lightZ, float lightRadius) =>
        lightRadius * DivideAndPin(occluderZ, lightZ - occluderZ, 0.0f, 0.95f);

    public static void GetSpotParams(
        float occluderZ,
        float lightX,
        float lightY,
        float lightZ,
        float lightRadius,
        out float blurRadius,
        out float scale,
        out ShadowPoint translate)
    {
        float zRatio = DivideAndPin(occluderZ, lightZ - occluderZ, 0.0f, 0.95f);
        blurRadius = lightRadius * zRatio;
        scale = DivideAndPin(lightZ, lightZ - occluderZ, 1.0f, 1.95f);
        translate = new ShadowPoint(-zRatio * lightX, -zRatio * lightY);
    }

    public static void GetDirectionalParams(
        float occluderZ,
        float lightX,
        float lightY,
        float lightZ,
        float lightRadius,
        out float blurRadius,
        out float scale,
        out ShadowPoint translate)
    {
        blurRadius = lightRadius * occluderZ;
        scale = 1.0f;
        // Max z-ratio is "max expected elevation"/"min allowable z".
        const float maxZRatio = 64 / NearlyZero;
        float zRatio = DivideAndPin(occluderZ, lightZ, 0.0f, maxZRatio);
        translate = new ShadowPoint(-zRatio * lightX, -zRatio * lightY);
    }

    /// <summary>
    /// SkDrawShadowMetrics::GetSpotShadowTransform for an affine CTM (the only kind a Plumix canvas
    /// carries): the spot shadow is the occluder scaled and translated away from the light.
    /// </summary>
    public static bool GetSpotShadowTransform(
        ShadowPoint3 lightPos,
        float lightRadius,
        SKMatrix ctm,
        ShadowPoint3 zPlaneParams,
        SKRect pathBounds,
        bool directional,
        out SKMatrix shadowTransform,
        out float radius)
    {
        float occluderHeight = (zPlaneParams.X * pathBounds.MidX) + (zPlaneParams.Y * pathBounds.MidY)
            + zPlaneParams.Z;
        float scale;
        ShadowPoint translate;
        if (directional)
        {
            GetDirectionalParams(occluderHeight, lightPos.X, lightPos.Y, lightPos.Z, lightRadius,
                out radius, out scale, out translate);
        }
        else
        {
            GetSpotParams(occluderHeight, lightPos.X, lightPos.Y, lightPos.Z, lightRadius,
                out radius, out scale, out translate);
        }

        var scaleTranslate = new SKMatrix(scale, 0, translate.X, 0, scale, translate.Y, 0, 0, 1);
        shadowTransform = SKMatrix.Concat(scaleTranslate, ctm);
        return true;
    }

    private static float DivideAndPin(float numer, float denom, float min, float max) =>
        Pin(numer / denom, min, max);

    /// <summary>SkTPin: a NaN collapses to <paramref name="min"/>.</summary>
    public static float Pin(float value, float min, float max)
    {
        float clamped = max < value ? max : value;
        return min < clamped ? clamped : min;
    }
}

/// <summary>SkShadowTessellator::MakeAmbient / MakeSpot.</summary>
internal sealed class ShadowTessellator
{
    private const float QuadTolerance = 0.2f;
    private const float CubicTolerance = 0.2f;
    private const float QuadToleranceSquared = QuadTolerance * QuadTolerance;
    private const float CubicToleranceSquared = CubicTolerance * CubicTolerance;
    private const float ConicTolerance = 0.25f;
    private const int MaxConicToQuadPow2 = 5;
    private const uint PenumbraColor = 0x00000000;
    private const uint UmbraColor = 0xFF000000;

    private readonly ShadowPoint3 _zPlaneParams;
    private readonly SKRect _pathBounds;

    private readonly List<ShadowPoint> _positions = [];
    private readonly List<uint> _colors = [];
    private readonly List<ushort> _indices = [];
    private readonly List<ShadowPoint> _pathPolygon = [];
    private readonly List<ShadowPoint> _clipPolygon = [];
    private readonly List<ShadowPoint> _clipVectors = [];

    private ShadowPoint _centroid;
    private float _area;
    private float _lastArea;
    private float _lastCross;

    private int _firstVertexIndex = -1;
    private ShadowPoint _firstOutset;
    private ShadowPoint _firstPoint;

    private bool _succeeded;
    private bool _transparent;
    private bool _isConvex = true;
    private bool _validUmbra = true;

    private float _direction = 1.0f;
    private int _prevUmbraIndex = -1;
    private int _currUmbraIndex;
    private int _currClipIndex;
    private bool _prevUmbraOutside;
    private bool _firstUmbraOutside;
    private ShadowPoint _prevOutset;
    private ShadowPoint _prevPoint;

    private ShadowTessellator(ShadowPoint3 zPlaneParams, SKRect bounds, bool transparent)
    {
        _zPlaneParams = zPlaneParams;
        _pathBounds = bounds;
        _transparent = transparent;
    }

    /// <summary>
    /// The ambient (all-around) shadow mesh in device space, or null where Skia falls back to a blurred
    /// path draw (a concave outline, which Skia insets with SkOffsetSimplePolygon).
    /// </summary>
    public static ShadowVertices? MakeAmbient(SKPath path, SKMatrix ctm, ShadowPoint3 zPlane, bool transparent)
    {
        if (!IsFinite(ctm.MapRect(path.Bounds)) || !zPlane.IsFinite)
        {
            return null;
        }

        var tessellator = new ShadowTessellator(zPlane, path.Bounds, transparent);
        tessellator.TessellateAmbient(path, ctm);
        return tessellator.ReleaseVertices();
    }

    /// <summary>The spot (light-offset) shadow mesh in device space; null as for <see cref="MakeAmbient"/>.</summary>
    public static ShadowVertices? MakeSpot(
        SKPath path,
        SKMatrix ctm,
        ShadowPoint3 zPlane,
        ShadowPoint3 lightPos,
        float lightRadius,
        bool transparent,
        bool directional)
    {
        if (!IsFinite(ctm.MapRect(path.Bounds)) || !zPlane.IsFinite || !lightPos.IsFinite
            || !(lightPos.Z >= ShadowMetrics.NearlyZero) || !float.IsFinite(lightRadius)
            || !(lightRadius >= ShadowMetrics.NearlyZero))
        {
            return null;
        }

        var tessellator = new ShadowTessellator(zPlane, path.Bounds, transparent);
        tessellator.TessellateSpot(path, ctm, lightPos, lightRadius, directional);
        return tessellator.ReleaseVertices();
    }

    private static bool IsFinite(SKRect rect) =>
        float.IsFinite(rect.Left) && float.IsFinite(rect.Top)
        && float.IsFinite(rect.Right) && float.IsFinite(rect.Bottom);

    private ShadowVertices? ReleaseVertices()
    {
        if (!_succeeded)
        {
            return null;
        }

        return new ShadowVertices(
            _positions.Select(point => (SKPoint)point).ToArray(),
            _colors.Select(color => new SKColor(color)).ToArray(),
            _indices.ToArray());
    }

    private float HeightFunc(float x, float y) => (_zPlaneParams.X * x) + (_zPlaneParams.Y * y) + _zPlaneParams.Z;

    // SkAmbientShadowTessellator.
    private void TessellateAmbient(SKPath path, SKMatrix ctm)
    {
        // umbraColor is the interior value, penumbraColor the exterior value.
        float baseZ = HeightFunc(_pathBounds.MidX, _pathBounds.MidY);
        float outset = ShadowMetrics.AmbientBlurRadius(baseZ);
        float inset = (outset * ShadowMetrics.AmbientRecipAlpha(baseZ)) - outset;

        if (!ComputePathPolygon(path, ctm))
        {
            return;
        }

        if (_pathPolygon.Count < 3 || !float.IsFinite(_area))
        {
            // Skia does not blur these cases: it returns an empty mesh instead.
            _succeeded = true;
            return;
        }

        _succeeded = _isConvex && ComputeConvexShadow(inset, outset, doClip: false);
    }

    // SkSpotShadowTessellator.
    private void TessellateSpot(SKPath path, SKMatrix ctm, ShadowPoint3 lightPos, float lightRadius, bool directional)
    {
        // Compute the blur radius, scale and translation for the spot shadow.
        if (!ShadowMetrics.GetSpotShadowTransform(lightPos, lightRadius, ctm, _zPlaneParams, path.Bounds,
                directional, out SKMatrix shadowTransform, out float outset))
        {
            return;
        }

        float inset = outset;

        // Compute rough clip bounds for umbra, plus offset polygon, plus centroid.
        if (!ComputeClipAndPathPolygons(path, ctm, shadowTransform))
        {
            return;
        }

        if (_clipPolygon.Count < 3 || _pathPolygon.Count < 3 || !float.IsFinite(_area))
        {
            _succeeded = true;
            return;
        }

        _succeeded = _isConvex && ComputeConvexShadow(inset, outset, doClip: true);
    }

    private bool ComputePathPolygon(SKPath path, SKMatrix ctm)
    {
        // Walk around the path, tessellate and generate the outer ring.
        using SKPath.Iterator iterator = path.CreateIterator(forceClose: true);
        var points = new SKPoint[4];
        bool verbSeen = false;
        bool closeSeen = false;
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            if (closeSeen)
            {
                return false;
            }

            switch (verb)
            {
                case SKPathVerb.Line:
                    HandleLine(ctm.MapPoint(points[1]));
                    break;
                case SKPathVerb.Quad:
                    HandleQuad(MapPoints(ctm, points, 3));
                    break;
                case SKPathVerb.Cubic:
                    HandleCubic(MapPoints(ctm, points, 4));
                    break;
                case SKPathVerb.Conic:
                    HandleConic(MapPoints(ctm, points, 3), iterator.ConicWeight());
                    break;
                case SKPathVerb.Move:
                    if (verbSeen)
                    {
                        return false;
                    }

                    break;
                case SKPathVerb.Close:
                    closeSeen = true;
                    break;
            }

            verbSeen = true;
        }

        FinishPathPolygon();
        return true;
    }

    private bool ComputeClipAndPathPolygons(SKPath path, SKMatrix ctm, SKMatrix shadowTransform)
    {
        // Coefficients to compute a cubic Bezier at t = 5/16.
        const float a = 0.32495117187f;
        const float b = 0.44311523437f;
        const float c = 0.20141601562f;
        const float d = 0.03051757812f;

        using SKPath.Iterator iterator = path.CreateIterator(forceClose: true);
        var points = new SKPoint[4];
        bool closeSeen = false;
        bool verbSeen = false;
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            if (closeSeen)
            {
                return false;
            }

            ShadowPoint[] clip;
            switch (verb)
            {
                case SKPathVerb.Line:
                    AddToClip(ctm.MapPoint(points[1]));
                    HandleLine(shadowTransform.MapPoint(points[1]));
                    break;
                case SKPathVerb.Quad:
                    clip = MapPoints(ctm, points, 3);
                    // Point at t = 1/2.
                    AddToClip(new ShadowPoint(
                        (0.25f * clip[0].X) + (0.5f * clip[1].X) + (0.25f * clip[2].X),
                        (0.25f * clip[0].Y) + (0.5f * clip[1].Y) + (0.25f * clip[2].Y)));
                    AddToClip(clip[2]);
                    HandleQuad(MapPoints(shadowTransform, points, 3));
                    break;
                case SKPathVerb.Conic:
                {
                    clip = MapPoints(ctm, points, 3);
                    float w = iterator.ConicWeight();
                    // Point at t = 1/2.
                    var curvePoint = new ShadowPoint(
                        (0.25f * clip[0].X) + (w * 0.5f * clip[1].X) + (0.25f * clip[2].X),
                        (0.25f * clip[0].Y) + (w * 0.5f * clip[1].Y) + (0.25f * clip[2].Y));
                    AddToClip(curvePoint * (1.0f / (0.5f + (0.5f * w))));
                    AddToClip(clip[2]);
                    HandleConic(MapPoints(shadowTransform, points, 3), w);
                    break;
                }
                case SKPathVerb.Cubic:
                    clip = MapPoints(ctm, points, 4);
                    // Point at t = 5/16.
                    AddToClip(new ShadowPoint(
                        (a * clip[0].X) + (b * clip[1].X) + (c * clip[2].X) + (d * clip[3].X),
                        (a * clip[0].Y) + (b * clip[1].Y) + (c * clip[2].Y) + (d * clip[3].Y)));
                    // Point at t = 11/16.
                    AddToClip(new ShadowPoint(
                        (d * clip[0].X) + (c * clip[1].X) + (b * clip[2].X) + (a * clip[3].X),
                        (d * clip[0].Y) + (c * clip[1].Y) + (b * clip[2].Y) + (a * clip[3].Y)));
                    AddToClip(clip[3]);
                    HandleCubic(MapPoints(shadowTransform, points, 4));
                    break;
                case SKPathVerb.Move:
                    if (verbSeen)
                    {
                        return false;
                    }

                    break;
                case SKPathVerb.Close:
                    closeSeen = true;
                    break;
            }

            verbSeen = true;
        }

        FinishPathPolygon();
        return true;
    }

    private void AddToClip(ShadowPoint point)
    {
        if (_clipPolygon.Count == 0 || !DuplicatePoint(point, _clipPolygon[^1]))
        {
            _clipPolygon.Add(point);
        }
    }

    private static ShadowPoint[] MapPoints(SKMatrix matrix, SKPoint[] points, int count)
    {
        var mapped = new ShadowPoint[count];
        for (int index = 0; index < count; index++)
        {
            mapped[index] = matrix.MapPoint(points[index]);
        }

        return mapped;
    }

    // ---- Polygon accumulation -------------------------------------------------------------------

    private static bool DuplicatePoint(ShadowPoint p0, ShadowPoint p1)
    {
        const float close = 1.0f / 16;
        const float closeSquared = close * close;
        return ShadowPoint.DistanceSquared(p0, p1) < closeSquared;
    }

    private static float PerpDot(ShadowPoint p0, ShadowPoint p1, ShadowPoint p2) => (p1 - p0).Cross(p2 - p1);

    private static bool NearlyZero(float value, float tolerance = ShadowMetrics.NearlyZero) =>
        Math.Abs(value) <= tolerance;

    private static bool ComputeNormal(ShadowPoint p0, ShadowPoint p1, float direction, out ShadowPoint normal)
    {
        // Compute the perpendicular.
        var perpendicular = new ShadowPoint((p0.Y - p1.Y) * direction, (p1.X - p0.X) * direction);
        return perpendicular.TrySetLength(1.0f, out normal);
    }

    private bool AccumulateCentroid(ShadowPoint current, ShadowPoint next)
    {
        if (DuplicatePoint(current, next))
        {
            return false;
        }

        ShadowPoint v0 = current - _pathPolygon[0];
        ShadowPoint v1 = next - _pathPolygon[0];
        float quadArea = v0.Cross(v1);
        _centroid = new ShadowPoint(
            _centroid.X + ((v0.X + v1.X) * quadArea),
            _centroid.Y + ((v0.Y + v1.Y) * quadArea));
        _area += quadArea;
        // Convexity check.
        if (quadArea * _lastArea < 0)
        {
            _isConvex = false;
        }

        if (quadArea != 0)
        {
            _lastArea = quadArea;
        }

        return true;
    }

    private bool CheckConvexity(ShadowPoint p0, ShadowPoint p1, ShadowPoint p2)
    {
        float cross = PerpDot(p0, p1, p2);
        // Skip collinear points.
        if (NearlyZero(cross))
        {
            return false;
        }

        if (_lastCross * cross < 0)
        {
            _isConvex = false;
        }

        if (cross != 0)
        {
            _lastCross = cross;
        }

        return true;
    }

    private void FinishPathPolygon()
    {
        if (_pathPolygon.Count > 1 && !AccumulateCentroid(_pathPolygon[^1], _pathPolygon[0]))
        {
            // Remove the coincident point.
            _pathPolygon.RemoveAt(_pathPolygon.Count - 1);
        }

        if (_pathPolygon.Count > 2)
        {
            // Do this before the final convexity check, so the correct _pathPolygon[0] is used.
            _centroid *= 1.0f / (3 * _area);
            _centroid += _pathPolygon[0];
            if (!CheckConvexity(_pathPolygon[^2], _pathPolygon[^1], _pathPolygon[0]))
            {
                // Remove the collinear point.
                _pathPolygon[0] = _pathPolygon[^1];
                _pathPolygon.RemoveAt(_pathPolygon.Count - 1);
            }
        }

        // If the area is positive, the winding is counter-clockwise.
        _direction = _area > 0 ? -1 : 1;
    }

    // Clamps the point to the nearest 16th of a pixel.
    private static ShadowPoint SanitizePoint(ShadowPoint point) =>
        new(MathF.Floor((16.0f * point.X) + 0.5f) * 0.0625f, MathF.Floor((16.0f * point.Y) + 0.5f) * 0.0625f);

    private void HandleLine(ShadowPoint point)
    {
        ShadowPoint sanitized = SanitizePoint(point);
        if (_pathPolygon.Count > 0 && !AccumulateCentroid(_pathPolygon[^1], sanitized))
        {
            // Skip the coincident point.
            return;
        }

        if (_pathPolygon.Count > 1 && !CheckConvexity(_pathPolygon[^2], _pathPolygon[^1], sanitized))
        {
            // Remove the collinear point.
            _pathPolygon.RemoveAt(_pathPolygon.Count - 1);
            // The previous point may now be coincident with the new one.
            if (DuplicatePoint(_pathPolygon[^1], sanitized))
            {
                _pathPolygon.RemoveAt(_pathPolygon.Count - 1);
            }
        }

        _pathPolygon.Add(sanitized);
    }

    // GrPathUtils::generateQuadraticPoints over a curve already in device space.
    private void HandleQuad(ShadowPoint[] points)
    {
        // Check for degeneracy.
        ShadowPoint v0 = points[1] - points[0];
        ShadowPoint v1 = points[2] - points[0];
        if (NearlyZero(v0.Cross(v1)))
        {
            return;
        }

        uint maxCount = MaxBezierVertices(QuadraticLog2(1.0f / QuadTolerance, points));
        var flattened = new List<ShadowPoint>((int)maxCount);
        GenerateQuadraticPoints(points[0], points[1], points[2], QuadToleranceSquared, flattened, maxCount);
        foreach (ShadowPoint point in flattened)
        {
            HandleLine(point);
        }
    }

    private void HandleCubic(ShadowPoint[] points)
    {
        uint maxCount = MaxBezierVertices(CubicLog2(1.0f / CubicTolerance, points));
        var flattened = new List<ShadowPoint>((int)maxCount);
        GenerateCubicPoints(points[0], points[1], points[2], points[3], CubicToleranceSquared, flattened, maxCount);
        foreach (ShadowPoint point in flattened)
        {
            HandleLine(point);
        }
    }

    // SkAutoConicToQuads over the device-space conic, then each quad as above.
    private void HandleConic(ShadowPoint[] points, float weight)
    {
        int pow2 = ComputeQuadPow2(points, weight, ConicTolerance);
        int count = SKPath.ConvertConicToQuads(points[0], points[1], points[2], weight, out SKPoint[] quads, pow2);
        ShadowPoint lastPoint = quads[0];
        int cursor = 1;
        for (int index = 0; index < count; index++)
        {
            ShadowPoint end = index == count - 1 ? points[2] : quads[cursor + 1];
            HandleQuad([lastPoint, quads[cursor], end]);
            lastPoint = end;
            cursor += 2;
        }
    }

    // SkConic::computeQuadPOW2.
    private static int ComputeQuadPow2(ShadowPoint[] points, float weight, float tolerance)
    {
        if (!points[0].IsFinite || !points[1].IsFinite || !points[2].IsFinite
            || !(weight > 0) || !float.IsFinite(weight))
        {
            return 0;
        }

        float a = weight - 1;
        float k = a / (4 * (2 + a));
        float x = k * (points[0].X - (2 * points[1].X) + points[2].X);
        float y = k * (points[0].Y - (2 * points[1].Y) + points[2].Y);
        float error = MathF.Sqrt((x * x) + (y * y));
        int pow2;
        for (pow2 = 0; pow2 < MaxConicToQuadPow2; ++pow2)
        {
            if (error <= tolerance)
            {
                break;
            }

            error *= 0.25f;
        }

        return pow2;
    }

    private static uint MaxBezierVertices(int chopCount)
    {
        const int maxChopsPerCurve = 10;
        return 1u << Math.Min(chopCount, maxChopsPerCurve);
    }

    // skgpu::wangs_formula::nextlog2: ceil(log2(x)) for finite x > 1, otherwise 0.
    private static int NextLog2(float x)
    {
        if (x <= 1)
        {
            return 0;
        }

        uint bits = BitConverter.SingleToUInt32Bits(x);
        const int digitsAfterBinaryPoint = 23;
        bits += (1u << digitsAfterBinaryPoint) - 1u;
        int exponent = (int)((bits >> digitsAfterBinaryPoint) & 0b1111_1111) - 127;
        return exponent > 0 ? exponent : 0;
    }

    private static int NextLog16(float x) => (NextLog2(x) + 3) >> 2;

    private static int QuadraticLog2(float precision, ShadowPoint[] points)
    {
        float vx = (-2 * points[1].X) + points[0].X + points[2].X;
        float vy = (-2 * points[1].Y) + points[0].Y + points[2].Y;
        float lengthTermP2 = (2 * 2 * (1 * 1) / 64.0f) * (precision * precision);
        return NextLog16(((vx * vx) + (vy * vy)) * lengthTermP2);
    }

    private static int CubicLog2(float precision, ShadowPoint[] points)
    {
        float v0x = (-2 * points[1].X) + points[0].X + points[2].X;
        float v0y = (-2 * points[1].Y) + points[0].Y + points[2].Y;
        float v1x = (-2 * points[2].X) + points[1].X + points[3].X;
        float v1y = (-2 * points[2].Y) + points[1].Y + points[3].Y;
        float lengthTermP2 = (3 * 3 * (2 * 2) / 64.0f) * (precision * precision);
        return NextLog16(Math.Max((v0x * v0x) + (v0y * v0y), (v1x * v1x) + (v1y * v1y)) * lengthTermP2);
    }

    private static float Midpoint(float a, float b) => (float)(((double)a + b) * 0.5);

    private static ShadowPoint Midpoint(ShadowPoint a, ShadowPoint b) => new(Midpoint(a.X, b.X), Midpoint(a.Y, b.Y));

    private static void GenerateQuadraticPoints(
        ShadowPoint p0,
        ShadowPoint p1,
        ShadowPoint p2,
        float toleranceSquared,
        List<ShadowPoint> points,
        uint pointsLeft)
    {
        if (pointsLeft < 2 || ShadowPoint.DistanceToLineSegmentSquared(p1, p0, p2) < toleranceSquared)
        {
            points.Add(p2);
            return;
        }

        ShadowPoint q0 = Midpoint(p0, p1);
        ShadowPoint q1 = Midpoint(p1, p2);
        ShadowPoint r = Midpoint(q0, q1);
        pointsLeft >>= 1;
        GenerateQuadraticPoints(p0, q0, r, toleranceSquared, points, pointsLeft);
        GenerateQuadraticPoints(r, q1, p2, toleranceSquared, points, pointsLeft);
    }

    private static void GenerateCubicPoints(
        ShadowPoint p0,
        ShadowPoint p1,
        ShadowPoint p2,
        ShadowPoint p3,
        float toleranceSquared,
        List<ShadowPoint> points,
        uint pointsLeft)
    {
        if (pointsLeft < 2
            || (ShadowPoint.DistanceToLineSegmentSquared(p1, p0, p3) < toleranceSquared
                && ShadowPoint.DistanceToLineSegmentSquared(p2, p0, p3) < toleranceSquared))
        {
            points.Add(p3);
            return;
        }

        ShadowPoint q0 = Midpoint(p0, p1);
        ShadowPoint q1 = Midpoint(p1, p2);
        ShadowPoint q2 = Midpoint(p2, p3);
        ShadowPoint r0 = Midpoint(q0, q1);
        ShadowPoint r1 = Midpoint(q1, q2);
        ShadowPoint s = Midpoint(r0, r1);
        pointsLeft >>= 1;
        GenerateCubicPoints(p0, q0, r0, s, toleranceSquared, points, pointsLeft);
        GenerateCubicPoints(s, r1, q2, p3, toleranceSquared, points, pointsLeft);
    }

    // ---- Convex shadow --------------------------------------------------------------------------

    private bool ComputeConvexShadow(float inset, float outset, bool doClip)
    {
        if (doClip)
        {
            ComputeClipVectorsAndTestCentroid();
        }

        // Adjust the inset distance and umbra color if necessary.
        uint umbraColor = UmbraColor;
        float minDistanceSquared = ShadowPoint.DistanceToLineSegmentSquared(
            _centroid, _pathPolygon[0], _pathPolygon[1]);
        for (int i = 1; i < _pathPolygon.Count; ++i)
        {
            int j = i == _pathPolygon.Count - 1 ? 0 : i + 1;
            float distanceSquared = ShadowPoint.DistanceToLineSegmentSquared(
                _centroid, _pathPolygon[i], _pathPolygon[j]);
            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
            }
        }

        List<ShadowPoint>? insetPolygon = null;
        if (inset > ShadowMetrics.NearlyZero)
        {
            const float tolerance = 1.0e-2f;
            if (minDistanceSquared < (inset + tolerance) * (inset + tolerance))
            {
                // If the umbra would collapse, back off a bit on the inner blur and adjust the alpha.
                float newInset = MathF.Sqrt(minDistanceSquared) - tolerance;
                float ratio = 128 * ((newInset / inset) + 1);
                // They aren't premultiplied colors, but the interpolation (SkPMLerp) is the same.
                umbraColor = Lerp256(UmbraColor, PenumbraColor, ratio > 0 ? (uint)ratio : 0u);
                inset = newInset;
            }

            // Generate the inner ring.
            insetPolygon = [];
            if (!InsetConvexPolygon(_pathPolygon, inset, insetPolygon))
            {
                // Not ideal, but in this case inset using the centroid.
                _validUmbra = false;
            }
        }

        List<ShadowPoint> umbraPolygon = inset > ShadowMetrics.NearlyZero ? insetPolygon! : _pathPolygon;

        // Walk around the path polygon, generate the outer ring and connect it to the inner ring.
        if (_transparent)
        {
            AddVertex(_centroid, umbraColor);
        }

        _currUmbraIndex = 0;

        // Initial setup: add the first quad.
        int polyCount = _pathPolygon.Count;
        if (!ComputeNormal(_pathPolygon[polyCount - 1], _pathPolygon[0], _direction, out _firstOutset))
        {
            // The polygon should be sanitized by this point, so this is unrecoverable.
            return false;
        }

        _firstOutset *= outset;
        _firstPoint = _pathPolygon[polyCount - 1];
        _firstVertexIndex = _positions.Count;
        _prevOutset = _firstOutset;
        _prevPoint = _firstPoint;
        _prevUmbraIndex = -1;

        AddInnerPoint(_firstPoint, umbraColor, umbraPolygon, out _prevUmbraIndex);

        if (!_transparent && doClip)
        {
            bool isOutside = ClipUmbraPoint(_positions[_firstVertexIndex], _centroid, out ShadowPoint clipPoint);
            if (isOutside)
            {
                AddVertex(clipPoint, umbraColor);
            }

            _prevUmbraOutside = isOutside;
            _firstUmbraOutside = isOutside;
        }

        AddVertex(_firstPoint + _firstOutset, PenumbraColor);
        AddEdge(_pathPolygon[0], _firstOutset, umbraColor, umbraPolygon, lastEdge: false, doClip);

        for (int i = 1; i < polyCount; ++i)
        {
            if (!ComputeNormal(_prevPoint, _pathPolygon[i], _direction, out ShadowPoint normal))
            {
                return false;
            }

            normal *= outset;
            AddArc(normal, outset, finishArc: true);
            AddEdge(_pathPolygon[i], normal, umbraColor, umbraPolygon, i == polyCount - 1, doClip);
        }

        // Final fan.
        if (AddArc(_firstOutset, outset, finishArc: false))
        {
            AppendTriangle(_firstVertexIndex, _positions.Count - 1, _firstVertexIndex + (_firstUmbraOutside ? 2 : 1));
        }
        else
        {
            // No arc added: fix up by setting the first penumbra point's position to the last one.
            _positions[_firstVertexIndex + (_firstUmbraOutside ? 2 : 1)] = _positions[^1];
        }

        return true;
    }

    private static uint Lerp256(uint source, uint destination, uint scale)
    {
        scale = Math.Min(scale, 256u);
        uint result = 0;
        for (int shift = 0; shift < 32; shift += 8)
        {
            uint s = (source >> shift) & 0xFF;
            uint d = (destination >> shift) & 0xFF;
            result |= (((s * scale) + (d * (256 - scale))) >> 8 & 0xFF) << shift;
        }

        return result;
    }

    private void AddVertex(ShadowPoint position, uint color)
    {
        _positions.Add(position);
        _colors.Add(color);
    }

    private void ComputeClipVectorsAndTestCentroid()
    {
        _currClipIndex = _clipPolygon.Count - 1;

        // Init the clip vectors.
        ShadowPoint v0 = _clipPolygon[1] - _clipPolygon[0];
        _clipVectors.Add(v0);

        // Init the centroid check.
        bool hiddenCentroid = true;
        ShadowPoint v1 = _centroid - _clipPolygon[0];
        float initCross = v0.Cross(v1);

        for (int p = 1; p < _clipPolygon.Count; ++p)
        {
            // Add to the clip vectors.
            v0 = _clipPolygon[(p + 1) % _clipPolygon.Count] - _clipPolygon[p];
            _clipVectors.Add(v0);
            // Determine whether the transformed centroid is inside the clip polygon.
            v1 = _centroid - _clipPolygon[p];
            if (initCross * v0.Cross(v1) <= 0)
            {
                hiddenCentroid = false;
            }
        }

        _transparent = _transparent || !hiddenCentroid;
    }

    private void AddEdge(
        ShadowPoint nextPoint,
        ShadowPoint nextNormal,
        uint umbraColor,
        List<ShadowPoint> umbraPolygon,
        bool lastEdge,
        bool doClip)
    {
        // Add the next umbra point.
        int currUmbraIndex;
        bool duplicate;
        if (lastEdge)
        {
            duplicate = false;
            currUmbraIndex = _firstVertexIndex;
            _prevPoint = nextPoint;
        }
        else
        {
            duplicate = AddInnerPoint(nextPoint, umbraColor, umbraPolygon, out currUmbraIndex);
        }

        int prevPenumbraIndex = duplicate || currUmbraIndex == _firstVertexIndex
            ? _positions.Count - 1
            : _positions.Count - 2;
        if (!duplicate)
        {
            // Add to the center fan if transparent or the centroid is showing.
            if (_transparent)
            {
                AppendTriangle(0, _prevUmbraIndex, currUmbraIndex);
            }
            else if (doClip)
            {
                // Otherwise add to the clip ring.
                ShadowPoint clipPoint = default;
                bool isOutside = lastEdge
                    ? _firstUmbraOutside
                    : ClipUmbraPoint(_positions[currUmbraIndex], _centroid, out clipPoint);
                if (isOutside)
                {
                    if (!lastEdge)
                    {
                        AddVertex(clipPoint, umbraColor);
                    }

                    AppendTriangle(_prevUmbraIndex, currUmbraIndex, currUmbraIndex + 1);
                    if (_prevUmbraOutside)
                    {
                        // Fill out the quad.
                        AppendTriangle(_prevUmbraIndex, currUmbraIndex + 1, _prevUmbraIndex + 1);
                    }
                }
                else if (_prevUmbraOutside)
                {
                    // Add a triangle.
                    AppendTriangle(_prevUmbraIndex, currUmbraIndex, _prevUmbraIndex + 1);
                }

                _prevUmbraOutside = isOutside;
            }
        }

        // Add the next penumbra point and quad.
        AddVertex(nextPoint + nextNormal, PenumbraColor);

        if (!duplicate)
        {
            AppendTriangle(_prevUmbraIndex, prevPenumbraIndex, currUmbraIndex);
        }

        AppendTriangle(prevPenumbraIndex, _positions.Count - 1, currUmbraIndex);

        _prevUmbraIndex = currUmbraIndex;
        _prevOutset = nextNormal;
    }

    private bool ClipUmbraPoint(ShadowPoint umbraPoint, ShadowPoint centroid, out ShadowPoint clipPoint)
    {
        ShadowPoint segmentVector = centroid - umbraPoint;

        int startClipPoint = _currClipIndex;
        do
        {
            ShadowPoint dp = umbraPoint - _clipPolygon[_currClipIndex];
            float denom = _clipVectors[_currClipIndex].Cross(segmentVector);
            float tNumer = dp.Cross(segmentVector);
            // If the line segments are nearly parallel...
            if (NearlyZero(denom))
            {
                // ...and collinear.
                if (NearlyZero(tNumer))
                {
                    clipPoint = default;
                    return false;
                }

                // Otherwise they are separate: try the next polygon segment.
            }
            else if (tNumer >= 0 && tNumer <= denom)
            {
                // The crossing lies within the polygon segment.
                float sNumer = dp.Cross(_clipVectors[_currClipIndex]);
                // If the umbra point is inside the clip polygon.
                if (sNumer >= 0 && sNumer <= denom)
                {
                    segmentVector *= sNumer / denom;
                    clipPoint = umbraPoint + segmentVector;
                    return true;
                }
            }

            _currClipIndex = (_currClipIndex + 1) % _clipPolygon.Count;
        }
        while (_currClipIndex != startClipPoint);

        clipPoint = default;
        return false;
    }

    private bool AddInnerPoint(
        ShadowPoint pathPoint,
        uint umbraColor,
        List<ShadowPoint> umbraPolygon,
        out int currUmbraIndex)
    {
        ShadowPoint umbraPoint;
        if (!_validUmbra)
        {
            ShadowPoint v = (_centroid - pathPoint) * 0.95f;
            umbraPoint = pathPoint + v;
        }
        else
        {
            umbraPoint = umbraPolygon[GetClosestUmbraIndex(pathPoint, umbraPolygon)];
        }

        _prevPoint = pathPoint;

        // Merge "close" points.
        if (_prevUmbraIndex == -1 || !DuplicatePoint(umbraPoint, _positions[_prevUmbraIndex]))
        {
            // If we've wrapped around, don't add a new point.
            if (_prevUmbraIndex >= 0 && DuplicatePoint(umbraPoint, _positions[_firstVertexIndex]))
            {
                currUmbraIndex = _firstVertexIndex;
            }
            else
            {
                currUmbraIndex = _positions.Count;
                AddVertex(umbraPoint, umbraColor);
            }

            return false;
        }

        currUmbraIndex = _prevUmbraIndex;
        return true;
    }

    private int GetClosestUmbraIndex(ShadowPoint point, List<ShadowPoint> umbraPolygon)
    {
        float minDistance = ShadowPoint.DistanceSquared(point, umbraPolygon[_currUmbraIndex]);
        int index = _currUmbraIndex;
        int direction = 1;
        int next = (index + direction) % umbraPolygon.Count;

        // Init the travel direction.
        float distance = ShadowPoint.DistanceSquared(point, umbraPolygon[next]);
        if (distance < minDistance)
        {
            index = next;
            minDistance = distance;
        }
        else
        {
            direction = umbraPolygon.Count - 1;
        }

        // Iterate until a point increases the distance.
        next = (index + direction) % umbraPolygon.Count;
        distance = ShadowPoint.DistanceSquared(point, umbraPolygon[next]);
        while (distance < minDistance)
        {
            index = next;
            minDistance = distance;
            next = (index + direction) % umbraPolygon.Count;
            distance = ShadowPoint.DistanceSquared(point, umbraPolygon[next]);
        }

        _currUmbraIndex = index;
        return index;
    }

    private bool AddArc(ShadowPoint nextNormal, float offset, bool finishArc)
    {
        // Fill in the fan from the previous quad.
        if (!ComputeRadialSteps(_prevOutset, nextNormal, offset, out float rotSin, out float rotCos, out int steps))
        {
            // Recover as best we can.
            steps = 0;
        }

        ShadowPoint prevNormal = _prevOutset;
        for (int i = 0; i < steps - 1; ++i)
        {
            var currNormal = new ShadowPoint(
                (prevNormal.X * rotCos) - (prevNormal.Y * rotSin),
                (prevNormal.Y * rotCos) + (prevNormal.X * rotSin));
            AddVertex(_prevPoint + currNormal, PenumbraColor);
            AppendTriangle(_prevUmbraIndex, _positions.Count - 1, _positions.Count - 2);
            prevNormal = currNormal;
        }

        if (finishArc && steps > 0)
        {
            AddVertex(_prevPoint + nextNormal, PenumbraColor);
            AppendTriangle(_prevUmbraIndex, _positions.Count - 1, _positions.Count - 2);
        }

        _prevOutset = nextNormal;
        return steps > 0;
    }

    private void AppendTriangle(int index0, int index1, int index2)
    {
        _indices.Add((ushort)index0);
        _indices.Add((ushort)index1);
        _indices.Add((ushort)index2);
    }

    // ---- SkPolyUtils ----------------------------------------------------------------------------

    private const float CrossTolerance = ShadowMetrics.NearlyZero * ShadowMetrics.NearlyZero;

    // SkComputeRadialSteps: the number of points needed for a circular join when offsetting a vertex.
    private static bool ComputeRadialSteps(
        ShadowPoint v1,
        ShadowPoint v2,
        float offset,
        out float rotSin,
        out float rotCos,
        out int steps)
    {
        const float recipPixelsPerArcSegment = 0.25f;
        rotSin = 0;
        rotCos = 0;
        steps = 0;

        float rCos = v1.Dot(v2);
        float rSin = v1.Cross(v2);
        if (!float.IsFinite(rCos) || !float.IsFinite(rSin))
        {
            return false;
        }

        float theta = MathF.Atan2(rSin, rCos);
        float floatSteps = MathF.Abs(offset * theta * recipPixelsPerArcSegment);
        // Limit the number of steps to what a 16-bit index can address.
        if (floatSteps >= ushort.MaxValue)
        {
            return false;
        }

        int count = (int)MathF.Floor(floatSteps + 0.5f);
        float dTheta = count > 0 ? theta / count : 0;
        rotSin = MathF.Sin(dTheta);
        rotCos = MathF.Cos(dTheta);
        // A large offset can leave a tiny dTheta, losing precision in rotSin and rotCos.
        if (count > 0 && (rotSin == 0 || rotCos == 1))
        {
            return false;
        }

        steps = count;
        return true;
    }

    // Computes perpDot for point p against the segment with origin p0 and vector v: positive is left.
    private static int ComputeSide(ShadowPoint p0, ShadowPoint v, ShadowPoint p)
    {
        float perpDot = v.Cross(p - p0);
        return NearlyZero(perpDot, CrossTolerance) ? 0 : perpDot > 0 ? 1 : -1;
    }

    // SkGetPolygonWinding: 1 for counter-clockwise, -1 for clockwise, 0 for a zero signed area.
    private static int GetPolygonWinding(List<ShadowPoint> polygon)
    {
        if (polygon.Count < 3)
        {
            return 0;
        }

        float quadArea = 0;
        ShadowPoint v0 = polygon[1] - polygon[0];
        for (int current = 2; current < polygon.Count; ++current)
        {
            ShadowPoint v1 = polygon[current] - polygon[0];
            quadArea += v0.Cross(v1);
            v0 = v1;
        }

        return NearlyZero(quadArea, CrossTolerance) ? 0 : quadArea > 0 ? 1 : -1;
    }

    private static bool OutsideInterval(float numer, float denom, bool denomPositive) =>
        (denomPositive && (numer < 0 || numer > denom)) || (!denomPositive && (numer > 0 || numer < denom));

    private static bool ZeroLength(ShadowPoint v, float vDotV) => !(v.IsFinite && vDotV != 0);

    private static bool CanNormalize(ShadowPoint v) => v.IsFinite && (v.X != 0 || v.Y != 0);

    // compute_intersection: the intersection of segments s0 and s1 with their parametric values s and t.
    private static bool ComputeIntersection(
        ShadowPoint s0Origin,
        ShadowPoint v0,
        ShadowPoint s1Origin,
        ShadowPoint v1,
        out ShadowPoint point,
        out float s,
        out float t)
    {
        point = default;
        s = 0;
        t = 0;
        ShadowPoint w = s1Origin - s0Origin;
        float denom = v0.Cross(v1);
        bool denomPositive = denom > 0;
        float sNumer;
        float tNumer;
        if (NearlyZero(denom, CrossTolerance))
        {
            // The segments are parallel, but not collinear.
            if (!NearlyZero(w.Cross(v0), CrossTolerance) || !NearlyZero(w.Cross(v1), CrossTolerance))
            {
                return false;
            }

            // Check for zero-length segments.
            float v0DotV0 = v0.Dot(v0);
            if (ZeroLength(v0, v0DotV0))
            {
                // Both are zero-length.
                float v1DotV1 = v1.Dot(v1);
                if (ZeroLength(v1, v1DotV1))
                {
                    // Check whether they're the same point.
                    if (!CanNormalize(w))
                    {
                        point = s0Origin;
                        return true;
                    }

                    // Otherwise the intersection is indeterminate.
                    return false;
                }

                // Otherwise project segment0's origin onto segment1.
                tNumer = v1.Dot(-w);
                denom = v1DotV1;
                if (OutsideInterval(tNumer, denom, true))
                {
                    return false;
                }

                sNumer = 0;
            }
            else
            {
                // Project segment1's endpoints onto segment0.
                sNumer = v0.Dot(w);
                denom = v0DotV0;
                tNumer = 0;
                if (OutsideInterval(sNumer, denom, true))
                {
                    // The first endpoint doesn't lie on segment0; a degenerate segment1 can't collide.
                    float v1DotV1 = v1.Dot(v1);
                    if (ZeroLength(v1, v1DotV1))
                    {
                        return false;
                    }

                    // Otherwise try the other one.
                    float oldSNumer = sNumer;
                    sNumer = v0.Dot(w + v1);
                    tNumer = denom;
                    if (OutsideInterval(sNumer, denom, true))
                    {
                        // Segment1's interval may surround segment0; not if the params share a sign.
                        if (sNumer * oldSNumer > 0)
                        {
                            return false;
                        }

                        // Otherwise project segment0's endpoint onto segment1 instead.
                        sNumer = 0;
                        tNumer = v1.Dot(-w);
                        denom = v1DotV1;
                    }
                }
            }
        }
        else
        {
            sNumer = w.Cross(v1);
            if (OutsideInterval(sNumer, denom, denomPositive))
            {
                return false;
            }

            tNumer = w.Cross(v0);
            if (OutsideInterval(tNumer, denom, denomPositive))
            {
                return false;
            }
        }

        float localS = sNumer / denom;
        float localT = tNumer / denom;
        point = s0Origin + (v0 * localS);
        s = localS;
        t = localT;
        return true;
    }

    private static bool EqualsWithinTolerance(ShadowPoint a, ShadowPoint b, float tolerance) =>
        NearlyZero(a.X - b.X, tolerance) && NearlyZero(a.Y - b.Y, tolerance);

    // SkIsConvexPolygon.
    private static bool IsConvexPolygon(List<ShadowPoint> polygon)
    {
        if (polygon.Count < 3)
        {
            return false;
        }

        float lastPerpDot = 0;
        int xSignChangeCount = 0;
        int ySignChangeCount = 0;

        int currIndex = 0;
        int nextIndex = 1;
        ShadowPoint v0 = polygon[currIndex] - polygon[^1];
        float lastVx = v0.X;
        float lastVy = v0.Y;
        ShadowPoint v1 = polygon[nextIndex] - polygon[currIndex];
        for (int i = 0; i < polygon.Count; ++i)
        {
            if (!polygon[i].IsFinite)
            {
                return false;
            }

            // Check that the winding direction is always the same (otherwise there is a reflex vertex).
            float perpDot = v0.Cross(v1);
            if (lastPerpDot * perpDot < 0)
            {
                return false;
            }

            if (perpDot != 0)
            {
                lastPerpDot = perpDot;
            }

            // Check that the signs of the edge vectors don't change more than twice per coordinate.
            if (lastVx * v1.X < 0)
            {
                xSignChangeCount++;
            }

            if (lastVy * v1.Y < 0)
            {
                ySignChangeCount++;
            }

            if (xSignChangeCount > 2 || ySignChangeCount > 2)
            {
                return false;
            }

            currIndex = nextIndex;
            nextIndex = (currIndex + 1) % polygon.Count;
            if (v1.X != 0)
            {
                lastVx = v1.X;
            }

            if (v1.Y != 0)
            {
                lastVy = v1.Y;
            }

            v0 = v1;
            v1 = polygon[nextIndex] - polygon[currIndex];
        }

        return true;
    }

    // SkInsetConvexPolygon: insets every edge and drops the inset edges that turn the wrong way.
    private static bool InsetConvexPolygon(List<ShadowPoint> input, float inset, List<ShadowPoint> insetPolygon)
    {
        int size = input.Count;
        if (size < 3 || size > ushort.MaxValue)
        {
            return false;
        }

        // Can't inset by a negative or non-finite amount.
        if (inset < -ShadowMetrics.NearlyZero || !float.IsFinite(inset))
        {
            return false;
        }

        // Insetting close to zero just returns the original polygon.
        if (inset <= ShadowMetrics.NearlyZero)
        {
            insetPolygon.AddRange(input);
            return true;
        }

        int winding = GetPolygonWinding(input);
        if (winding == 0)
        {
            return false;
        }

        var edges = new OffsetEdge[size];
        int prev = size - 1;
        for (int curr = 0; curr < size; prev = curr, ++curr)
        {
            int next = (curr + 1) % size;
            if (!input[curr].IsFinite)
            {
                return false;
            }

            // Check for convexity just to be sure.
            if (ComputeSide(input[prev], input[curr] - input[prev], input[next]) * winding < 0)
            {
                return false;
            }

            ShadowPoint v = input[next] - input[curr];
            new ShadowPoint(-v.Y, v.X).TrySetLength(inset * winding, out ShadowPoint perpendicular);
            ShadowPoint origin = input[curr] + perpendicular;
            edges[curr] = new OffsetEdge
            {
                Prev = prev,
                Next = next,
                Origin = origin,
                Vector = v,
                Intersection = origin,
                TValue = float.MinValue,
            };
        }

        int head = 0;
        int currEdge = head;
        int prevEdge = edges[currEdge].Prev;
        int insetVertexCount = size;
        int iterations = 0;
        int maxIterations = size * size;
        while (head >= 0 && prevEdge != currEdge)
        {
            ++iterations;
            // Each edge should be checked against each other edge at most once.
            if (iterations > maxIterations)
            {
                return false;
            }

            if (ComputeIntersection(edges[prevEdge].Origin, edges[prevEdge].Vector,
                    edges[currEdge].Origin, edges[currEdge].Vector,
                    out ShadowPoint intersection, out float s, out float t))
            {
                if (s < edges[prevEdge].TValue)
                {
                    // The new intersection is further back on the previous inset than the prior one.
                    RemoveNode(edges, prevEdge, ref head);
                    --insetVertexCount;
                    prevEdge = edges[prevEdge].Prev;
                }
                else if (edges[currEdge].TValue > float.MinValue
                    && EqualsWithinTolerance(intersection, edges[currEdge].Intersection, 1.0e-6f))
                {
                    // This intersection was already considered: done.
                    break;
                }
                else
                {
                    // Add the intersection and go to the next segment.
                    edges[currEdge].Intersection = intersection;
                    edges[currEdge].TValue = t;
                    prevEdge = currEdge;
                    currEdge = edges[currEdge].Next;
                }
            }
            else
            {
                // If prev is to the right side of curr...
                int side = winding * ComputeSide(edges[currEdge].Origin, edges[currEdge].Vector,
                    edges[prevEdge].Origin);
                if (side < 0 && side == winding * ComputeSide(edges[currEdge].Origin, edges[currEdge].Vector,
                        edges[prevEdge].Origin + edges[prevEdge].Vector))
                {
                    RemoveNode(edges, prevEdge, ref head);
                    --insetVertexCount;
                    prevEdge = edges[prevEdge].Prev;
                }
                else
                {
                    // Move to the next segment.
                    RemoveNode(edges, currEdge, ref head);
                    --insetVertexCount;
                    currEdge = edges[currEdge].Next;
                }
            }
        }

        // Store all the valid intersections that aren't nearly coincident.
        insetPolygon.Clear();
        if (head < 0)
        {
            return false;
        }

        const float cleanupTolerance = 0.01f;
        int currIndex = 0;
        insetPolygon.Add(edges[head].Intersection);
        currEdge = edges[head].Next;
        while (currEdge != head)
        {
            if (!EqualsWithinTolerance(edges[currEdge].Intersection, insetPolygon[currIndex], cleanupTolerance))
            {
                insetPolygon.Add(edges[currEdge].Intersection);
                currIndex++;
            }

            currEdge = edges[currEdge].Next;
        }

        // Make sure the first and last points aren't coincident.
        if (currIndex >= 1 && EqualsWithinTolerance(insetPolygon[0], insetPolygon[currIndex], cleanupTolerance))
        {
            insetPolygon.RemoveAt(insetPolygon.Count - 1);
        }

        return IsConvexPolygon(insetPolygon);
    }

    private static void RemoveNode(OffsetEdge[] edges, int node, ref int head)
    {
        edges[edges[node].Prev].Next = edges[node].Next;
        edges[edges[node].Next].Prev = edges[node].Prev;
        if (node == head)
        {
            head = edges[node].Next == node ? -1 : edges[node].Next;
        }
    }

    private struct OffsetEdge
    {
        public int Prev;
        public int Next;
        public ShadowPoint Origin;
        public ShadowPoint Vector;
        public ShadowPoint Intersection;
        public float TValue;
    }
}
