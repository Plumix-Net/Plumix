namespace Plumix.Rendering;

// Dart parity source: dart:ui ImageFilter and TileMode.

public enum TileMode
{
    Clamp,
    Repeated,
    Mirror,
    Decal,
}

public abstract record ImageFilter
{
    public sealed record Blur : ImageFilter
    {
        public Blur(
            double sigmaX = 0.0,
            double sigmaY = 0.0,
            TileMode tileMode = TileMode.Clamp)
        {
            if (!double.IsFinite(sigmaX) || sigmaX < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(sigmaX));
            }

            if (!double.IsFinite(sigmaY) || sigmaY < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(sigmaY));
            }

            SigmaX = sigmaX;
            SigmaY = sigmaY;
            TileMode = tileMode;
        }

        public double SigmaX { get; }

        public double SigmaY { get; }

        public TileMode TileMode { get; }
    }

    public sealed record Matrix : ImageFilter
    {
        public Matrix(
            IReadOnlyList<double> values,
            FilterQuality filterQuality = FilterQuality.Low)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Count != 16)
            {
                throw new ArgumentException("An image-filter matrix must contain 16 values.", nameof(values));
            }

            if (values.Any(static value => !double.IsFinite(value)))
            {
                throw new ArgumentException("Image-filter matrix values must be finite.", nameof(values));
            }

            Values = values.ToArray();
            FilterQuality = filterQuality;
        }

        public Matrix(
            FilterQuality filterQuality = FilterQuality.Low,
            params double[] values) : this((IReadOnlyList<double>)values, filterQuality)
        {
        }

        public IReadOnlyList<double> Values { get; }

        public FilterQuality FilterQuality { get; }
    }

    public sealed record Dilate : ImageFilter
    {
        public Dilate(double radiusX = 0.0, double radiusY = 0.0)
        {
            ValidateRadius(radiusX, nameof(radiusX));
            ValidateRadius(radiusY, nameof(radiusY));
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        public double RadiusX { get; }

        public double RadiusY { get; }
    }

    public sealed record Erode : ImageFilter
    {
        public Erode(double radiusX = 0.0, double radiusY = 0.0)
        {
            ValidateRadius(radiusX, nameof(radiusX));
            ValidateRadius(radiusY, nameof(radiusY));
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        public double RadiusX { get; }

        public double RadiusY { get; }
    }

    public sealed record Compose : ImageFilter
    {
        public Compose(ImageFilter outer, ImageFilter inner)
        {
            Outer = outer ?? throw new ArgumentNullException(nameof(outer));
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public ImageFilter Outer { get; }

        public ImageFilter Inner { get; }
    }

    private static void ValidateRadius(double radius, string parameterName)
    {
        if (!double.IsFinite(radius) || radius < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
