
namespace Plumix.Rendering;

// Dart parity source: dart:ui FilterQuality, BlendMode and ColorFilter.

public enum FilterQuality
{
    None,
    Low,
    Medium,
    High,
}

public enum BlendMode
{
    Clear,
    Source,
    Destination,
    SourceOver,
    DestinationOver,
    SourceIn,
    DestinationIn,
    SourceOut,
    DestinationOut,
    SourceAtop,
    DestinationAtop,
    Xor,
    Plus,
    Modulate,
    Screen,
    Overlay,
    Darken,
    Lighten,
    ColorDodge,
    ColorBurn,
    HardLight,
    SoftLight,
    Difference,
    Exclusion,
    Multiply,
    Hue,
    Saturation,
    Color,
    Luminosity,
}

public abstract record ColorFilter
{
    public sealed record Matrix : ColorFilter
    {
        public Matrix(IReadOnlyList<double> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Count != 20)
            {
                throw new ArgumentException("A color-filter matrix must contain 20 values.", nameof(values));
            }

            Values = values.ToArray();
        }

        public Matrix(params double[] values) : this((IReadOnlyList<double>)values) { }

        public IReadOnlyList<double> Values { get; }

        /// <summary>Dart's <c>ColorFilter ==</c>: matrices compare element by element.</summary>
        public bool Equals(Matrix? other) =>
            other is not null && (ReferenceEquals(this, other) || Values.SequenceEqual(other.Values));

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (double value in Values)
            {
                hash.Add(value);
            }

            return hash.ToHashCode();
        }

        /// <summary>Dart's <c>ColorFilter.toString</c> for a matrix filter.</summary>
        public override string ToString() =>
            $"ColorFilter.matrix([{string.Join(", ", Values.Select(DartFormat.Number))}])";
    }

    public sealed record Mode : ColorFilter
    {
        public Mode(Color color, BlendMode blendMode)
        {
            Color = color;
            BlendMode = blendMode;
        }

        public Color Color { get; }

        public BlendMode BlendMode { get; }

        /// <summary>Dart's <c>ColorFilter.toString</c> for a mode filter.</summary>
        public override string ToString() =>
            $"ColorFilter.mode({(Plumix.UI.Color)Color}, {Plumix.UI.Paint.BlendModeName(BlendMode)})";
    }
}
