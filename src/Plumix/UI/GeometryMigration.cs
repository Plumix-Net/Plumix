namespace Plumix;

// C#-only infrastructure: no Dart parity source.
// Transitional spellings left over from the Avalonia geometry carriers; removed by the migration.
internal static class GeometryMigrationExtensions
{
    extension(Offset offset)
    {
        public Rect WithSize(Size size) => offset & size;
        public Offset Modulo(double divisor) => offset % divisor;
    }

    extension(Size size)
    {
        public Size Add(Offset offset) => size + offset;
        public Size Subtract(Offset offset) => size - offset;
        public Offset Difference(Size other) => size - other;
        public Size Modulo(double divisor) => size % divisor;
    }
}
