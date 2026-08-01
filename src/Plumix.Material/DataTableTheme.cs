using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/data_table_theme.dart

public sealed partial record DataTableThemeData
{
    public DataTableThemeData(
        BoxDecoration? decoration = null,
        MaterialStateProperty<Color?>? dataRowColor = null,
        double? dataRowHeight = null,
        double? dataRowMinHeight = null,
        double? dataRowMaxHeight = null,
        TextStyle? dataTextStyle = null,
        MaterialStateProperty<Color?>? headingRowColor = null,
        double? headingRowHeight = null,
        TextStyle? headingTextStyle = null,
        double? horizontalMargin = null,
        double? columnSpacing = null,
        double? dividerThickness = null,
        double? checkboxHorizontalMargin = null,
        MaterialStateProperty<MouseCursor?>? headingCellCursor = null,
        MaterialStateProperty<MouseCursor?>? dataRowCursor = null,
        MainAxisAlignment? headingRowAlignment = null)
    {
        if (dataRowHeight.HasValue && (dataRowMinHeight.HasValue || dataRowMaxHeight.HasValue))
            throw new ArgumentException("dataRowHeight cannot be combined with min/max row heights.");
        dataRowMinHeight ??= dataRowHeight;
        dataRowMaxHeight ??= dataRowHeight;
        ValidateNonNegative(dataRowMinHeight, nameof(dataRowMinHeight));
        ValidateNonNegative(dataRowMaxHeight, nameof(dataRowMaxHeight));
        if (dataRowMinHeight > dataRowMaxHeight) throw new ArgumentException("Maximum row height must be at least the minimum row height.");
        ValidateNonNegative(headingRowHeight, nameof(headingRowHeight));
        ValidateNonNegative(horizontalMargin, nameof(horizontalMargin));
        ValidateNonNegative(columnSpacing, nameof(columnSpacing));
        ValidateNonNegative(dividerThickness, nameof(dividerThickness));
        ValidateNonNegative(checkboxHorizontalMargin, nameof(checkboxHorizontalMargin));

        Decoration = decoration;
        DataRowColor = dataRowColor;
        DataRowMinHeight = dataRowMinHeight;
        DataRowMaxHeight = dataRowMaxHeight;
        DataTextStyle = dataTextStyle;
        HeadingRowColor = headingRowColor;
        HeadingRowHeight = headingRowHeight;
        HeadingTextStyle = headingTextStyle;
        HorizontalMargin = horizontalMargin;
        ColumnSpacing = columnSpacing;
        DividerThickness = dividerThickness;
        CheckboxHorizontalMargin = checkboxHorizontalMargin;
        HeadingCellCursor = headingCellCursor;
        DataRowCursor = dataRowCursor;
        HeadingRowAlignment = headingRowAlignment;
    }

    public BoxDecoration? Decoration { get; init; }
    public MaterialStateProperty<Color?>? DataRowColor { get; init; }
    public double? DataRowMinHeight { get; init; }
    public double? DataRowMaxHeight { get; init; }
    public double? DataRowHeight => DataRowMinHeight == DataRowMaxHeight ? DataRowMinHeight : null;
    public TextStyle? DataTextStyle { get; init; }
    public MaterialStateProperty<Color?>? HeadingRowColor { get; init; }
    public double? HeadingRowHeight { get; init; }
    public TextStyle? HeadingTextStyle { get; init; }
    public double? HorizontalMargin { get; init; }
    public double? ColumnSpacing { get; init; }
    public double? DividerThickness { get; init; }
    public double? CheckboxHorizontalMargin { get; init; }
    public MaterialStateProperty<MouseCursor?>? HeadingCellCursor { get; init; }
    public MaterialStateProperty<MouseCursor?>? DataRowCursor { get; init; }
    public MainAxisAlignment? HeadingRowAlignment { get; init; }

    private static void ValidateNonNegative(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(name);
    }
}

public sealed class DataTableTheme : InheritedWidget
{
    public DataTableTheme(DataTableThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DataTableThemeData Data { get; }
    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;
    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => !Equals(((DataTableTheme)oldWidget).Data, Data);

    public static DataTableThemeData Of(BuildContext context) =>
        context.DependOnInherited<DataTableTheme>()?.Data ?? Theme.Of(context).DataTableTheme;
}
