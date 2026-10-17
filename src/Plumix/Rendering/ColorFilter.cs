using Avalonia.Media;

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
            FlutterBlendMode = blendMode;
            BlendMode = ToBitmapBlendingMode(blendMode);
        }

        public Mode(Color color, Avalonia.Media.Imaging.BitmapBlendingMode blendMode)
        {
            Color = color;
            BlendMode = blendMode;
            FlutterBlendMode = FromBitmapBlendingMode(blendMode);
        }

        public Color Color { get; }

        public BlendMode FlutterBlendMode { get; }

        public Avalonia.Media.Imaging.BitmapBlendingMode BlendMode { get; }

        /// <summary>Dart's <c>ColorFilter.toString</c> for a mode filter.</summary>
        public override string ToString() =>
            $"ColorFilter.mode({(Plumix.UI.Color)Color}, {Plumix.UI.Paint.BlendModeName(FlutterBlendMode)})";

        private static Avalonia.Media.Imaging.BitmapBlendingMode ToBitmapBlendingMode(BlendMode blendMode)
        {
            return blendMode switch
            {
                Rendering.BlendMode.Clear => Avalonia.Media.Imaging.BitmapBlendingMode.Source,
                Rendering.BlendMode.Source => Avalonia.Media.Imaging.BitmapBlendingMode.Source,
                Rendering.BlendMode.Destination => Avalonia.Media.Imaging.BitmapBlendingMode.Destination,
                Rendering.BlendMode.SourceOver => Avalonia.Media.Imaging.BitmapBlendingMode.SourceOver,
                Rendering.BlendMode.DestinationOver => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOver,
                Rendering.BlendMode.SourceIn => Avalonia.Media.Imaging.BitmapBlendingMode.SourceIn,
                Rendering.BlendMode.DestinationIn => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationIn,
                Rendering.BlendMode.SourceOut => Avalonia.Media.Imaging.BitmapBlendingMode.SourceOut,
                Rendering.BlendMode.DestinationOut => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOut,
                Rendering.BlendMode.SourceAtop => Avalonia.Media.Imaging.BitmapBlendingMode.SourceAtop,
                Rendering.BlendMode.DestinationAtop => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationAtop,
                Rendering.BlendMode.Xor => Avalonia.Media.Imaging.BitmapBlendingMode.Xor,
                Rendering.BlendMode.Plus => Avalonia.Media.Imaging.BitmapBlendingMode.Plus,
                Rendering.BlendMode.Modulate => Avalonia.Media.Imaging.BitmapBlendingMode.Multiply,
                Rendering.BlendMode.Screen => Avalonia.Media.Imaging.BitmapBlendingMode.Screen,
                Rendering.BlendMode.Overlay => Avalonia.Media.Imaging.BitmapBlendingMode.Overlay,
                Rendering.BlendMode.Darken => Avalonia.Media.Imaging.BitmapBlendingMode.Darken,
                Rendering.BlendMode.Lighten => Avalonia.Media.Imaging.BitmapBlendingMode.Lighten,
                Rendering.BlendMode.ColorDodge => Avalonia.Media.Imaging.BitmapBlendingMode.ColorDodge,
                Rendering.BlendMode.ColorBurn => Avalonia.Media.Imaging.BitmapBlendingMode.ColorBurn,
                Rendering.BlendMode.HardLight => Avalonia.Media.Imaging.BitmapBlendingMode.HardLight,
                Rendering.BlendMode.SoftLight => Avalonia.Media.Imaging.BitmapBlendingMode.SoftLight,
                Rendering.BlendMode.Difference => Avalonia.Media.Imaging.BitmapBlendingMode.Difference,
                Rendering.BlendMode.Exclusion => Avalonia.Media.Imaging.BitmapBlendingMode.Exclusion,
                Rendering.BlendMode.Multiply => Avalonia.Media.Imaging.BitmapBlendingMode.Multiply,
                Rendering.BlendMode.Hue => Avalonia.Media.Imaging.BitmapBlendingMode.Hue,
                Rendering.BlendMode.Saturation => Avalonia.Media.Imaging.BitmapBlendingMode.Saturation,
                Rendering.BlendMode.Color => Avalonia.Media.Imaging.BitmapBlendingMode.Color,
                Rendering.BlendMode.Luminosity => Avalonia.Media.Imaging.BitmapBlendingMode.Luminosity,
                _ => Avalonia.Media.Imaging.BitmapBlendingMode.SourceOver,
            };
        }

        private static BlendMode FromBitmapBlendingMode(Avalonia.Media.Imaging.BitmapBlendingMode blendMode)
        {
            return blendMode switch
            {
                Avalonia.Media.Imaging.BitmapBlendingMode.Source => Rendering.BlendMode.Source,
                Avalonia.Media.Imaging.BitmapBlendingMode.Destination => Rendering.BlendMode.Destination,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOver => Rendering.BlendMode.DestinationOver,
                Avalonia.Media.Imaging.BitmapBlendingMode.SourceIn => Rendering.BlendMode.SourceIn,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationIn => Rendering.BlendMode.DestinationIn,
                Avalonia.Media.Imaging.BitmapBlendingMode.SourceOut => Rendering.BlendMode.SourceOut,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOut => Rendering.BlendMode.DestinationOut,
                Avalonia.Media.Imaging.BitmapBlendingMode.SourceAtop => Rendering.BlendMode.SourceAtop,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationAtop => Rendering.BlendMode.DestinationAtop,
                Avalonia.Media.Imaging.BitmapBlendingMode.Xor => Rendering.BlendMode.Xor,
                Avalonia.Media.Imaging.BitmapBlendingMode.Plus => Rendering.BlendMode.Plus,
                Avalonia.Media.Imaging.BitmapBlendingMode.Screen => Rendering.BlendMode.Screen,
                Avalonia.Media.Imaging.BitmapBlendingMode.Overlay => Rendering.BlendMode.Overlay,
                Avalonia.Media.Imaging.BitmapBlendingMode.Darken => Rendering.BlendMode.Darken,
                Avalonia.Media.Imaging.BitmapBlendingMode.Lighten => Rendering.BlendMode.Lighten,
                Avalonia.Media.Imaging.BitmapBlendingMode.ColorDodge => Rendering.BlendMode.ColorDodge,
                Avalonia.Media.Imaging.BitmapBlendingMode.ColorBurn => Rendering.BlendMode.ColorBurn,
                Avalonia.Media.Imaging.BitmapBlendingMode.HardLight => Rendering.BlendMode.HardLight,
                Avalonia.Media.Imaging.BitmapBlendingMode.SoftLight => Rendering.BlendMode.SoftLight,
                Avalonia.Media.Imaging.BitmapBlendingMode.Difference => Rendering.BlendMode.Difference,
                Avalonia.Media.Imaging.BitmapBlendingMode.Exclusion => Rendering.BlendMode.Exclusion,
                Avalonia.Media.Imaging.BitmapBlendingMode.Multiply => Rendering.BlendMode.Multiply,
                Avalonia.Media.Imaging.BitmapBlendingMode.Hue => Rendering.BlendMode.Hue,
                Avalonia.Media.Imaging.BitmapBlendingMode.Saturation => Rendering.BlendMode.Saturation,
                Avalonia.Media.Imaging.BitmapBlendingMode.Color => Rendering.BlendMode.Color,
                Avalonia.Media.Imaging.BitmapBlendingMode.Luminosity => Rendering.BlendMode.Luminosity,
                _ => Rendering.BlendMode.SourceOver,
            };
        }
    }
}
