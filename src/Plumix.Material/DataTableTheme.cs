using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/data_table_theme.dart

public sealed partial record DataTableThemeData
{
    public DataTableThemeData(
        Decoration? decoration = null,
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
        if (dataRowMinHeight > dataRowMaxHeight)
        {
            throw new ArgumentException("Maximum row height must be at least the minimum row height.");
        }

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

    public Decoration? Decoration { get; init; }
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

    public DataTableThemeData CopyWith(
        Decoration? decoration = null,
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
        {
            throw new ArgumentException("dataRowHeight cannot be combined with min/max row heights.");
        }
        dataRowMinHeight ??= dataRowHeight;
        dataRowMaxHeight ??= dataRowHeight;
        return new DataTableThemeData(
            decoration: decoration ?? Decoration,
            dataRowColor: dataRowColor ?? DataRowColor,
            dataRowMinHeight: dataRowMinHeight ?? DataRowMinHeight,
            dataRowMaxHeight: dataRowMaxHeight ?? DataRowMaxHeight,
            dataTextStyle: dataTextStyle ?? DataTextStyle,
            headingRowColor: headingRowColor ?? HeadingRowColor,
            headingRowHeight: headingRowHeight ?? HeadingRowHeight,
            headingTextStyle: headingTextStyle ?? HeadingTextStyle,
            horizontalMargin: horizontalMargin ?? HorizontalMargin,
            columnSpacing: columnSpacing ?? ColumnSpacing,
            dividerThickness: dividerThickness ?? DividerThickness,
            checkboxHorizontalMargin: checkboxHorizontalMargin ?? CheckboxHorizontalMargin,
            headingCellCursor: headingCellCursor ?? HeadingCellCursor,
            dataRowCursor: dataRowCursor ?? DataRowCursor,
            headingRowAlignment: headingRowAlignment ?? HeadingRowAlignment);
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
