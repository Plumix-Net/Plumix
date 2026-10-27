using System.Globalization;
using Avalonia;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/preferred_size.dart

/// <summary>
/// The size a <see cref="IPreferredSizeWidget"/> would prefer: the dart:ui <c>Size</c> that
/// <c>PreferredSizeWidget.preferredSize</c> returns.
/// </summary>
/// <remarks>
/// Dart widgets subclass <c>Size</c> to attach metadata to their preferred size (Material's private
/// <c>_PreferredAppBarSize</c> carries the app bar's toolbar and bottom heights). Avalonia's
/// <see cref="Size"/> is a sealed value type, so the preferred size is this non-sealed class instead.
/// It converts implicitly to and from <see cref="Size"/>, and, like Dart's <c>Size</c>, compares by
/// width and height only, whatever its runtime type.
/// </remarks>
public class PreferredSizeValue : IEquatable<PreferredSizeValue>
{
    public PreferredSizeValue(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public PreferredSizeValue(Size size) : this(size.Width, size.Height)
    {
    }

    /// <summary>Dart's <c>Size.fromHeight</c>: an unbounded width with the given height.</summary>
    public static PreferredSizeValue FromHeight(double height) => new(double.PositiveInfinity, height);

    /// <summary>Dart's <c>Size.fromWidth</c>: the given width with an unbounded height.</summary>
    public static PreferredSizeValue FromWidth(double width) => new(width, double.PositiveInfinity);

    public double Width { get; }

    public double Height { get; }

    /// <summary>The plain <see cref="Size"/> with this width and height.</summary>
    public Size ToSize() => new(Width, Height);

    public static implicit operator Size(PreferredSizeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ToSize();
    }

    public static implicit operator PreferredSizeValue(Size size) => new(size);

    public static bool operator ==(PreferredSizeValue? left, PreferredSizeValue? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PreferredSizeValue? left, PreferredSizeValue? right) => !(left == right);

    public bool Equals(PreferredSizeValue? other) =>
        other is not null && other.Width.Equals(Width) && other.Height.Equals(Height);

    public override bool Equals(object? obj) => obj is PreferredSizeValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Width, Height);

    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture, $"Size({Width:0.0}, {Height:0.0})");
}

/// <summary>
/// An interface for widgets that can return the size they would prefer if they were otherwise
/// unconstrained: Dart's <c>PreferredSizeWidget</c>.
/// </summary>
public interface IPreferredSizeWidget
{
    PreferredSizeValue PreferredSize { get; }
}

/// <summary>A widget with a preferred size: Dart's <c>PreferredSize</c>.</summary>
public sealed class PreferredSize : StatelessWidget, IPreferredSizeWidget
{
    public PreferredSize(PreferredSizeValue preferredSize, Widget child, Key? key = null) : base(key)
    {
        Size = preferredSize ?? throw new ArgumentNullException(nameof(preferredSize));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>Dart's <c>preferredSize</c>; a member cannot share its type's name in C#.</summary>
    public PreferredSizeValue Size { get; }

    public Widget Child { get; }

    PreferredSizeValue IPreferredSizeWidget.PreferredSize => Size;

    public override Widget Build(BuildContext context) => Child;
}
