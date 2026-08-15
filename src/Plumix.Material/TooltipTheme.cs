using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/tooltip_theme.dart

public sealed partial record TooltipThemeData
{
    public TooltipThemeData(
        double? Height = null,
        BoxConstraints? Constraints = null,
        EdgeInsetsGeometry? Padding = null,
        EdgeInsetsGeometry? Margin = null,
        double? VerticalOffset = null,
        bool? PreferBelow = null,
        bool? ExcludeFromSemantics = null,
        Decoration? Decoration = null,
        TextStyle? TextStyle = null,
        TextAlign? TextAlign = null,
        TimeSpan? WaitDuration = null,
        TimeSpan? ShowDuration = null,
        TimeSpan? ExitDuration = null,
        TooltipTriggerMode? TriggerMode = null,
        bool? EnableFeedback = null)
    {
        if (Height.HasValue && Constraints.HasValue)
        {
            throw new ArgumentException("Only one of height and constraints may be specified.");
        }

        this.Height = Height;
        this.Constraints = Constraints;
        this.Padding = Padding;
        this.Margin = Margin;
        this.VerticalOffset = VerticalOffset;
        this.PreferBelow = PreferBelow;
        this.ExcludeFromSemantics = ExcludeFromSemantics;
        this.Decoration = Decoration;
        this.TextStyle = TextStyle;
        this.TextAlign = TextAlign;
        this.WaitDuration = WaitDuration;
        this.ShowDuration = ShowDuration;
        this.ExitDuration = ExitDuration;
        this.TriggerMode = TriggerMode;
        this.EnableFeedback = EnableFeedback;
    }

    public double? Height { get; init; }

    public BoxConstraints? Constraints { get; init; }

    public EdgeInsetsGeometry? Padding { get; init; }

    public EdgeInsetsGeometry? Margin { get; init; }

    public double? VerticalOffset { get; init; }

    public bool? PreferBelow { get; init; }

    public bool? ExcludeFromSemantics { get; init; }

    public Decoration? Decoration { get; init; }

    public TextStyle? TextStyle { get; init; }

    public TextAlign? TextAlign { get; init; }

    public TimeSpan? WaitDuration { get; init; }

    public TimeSpan? ShowDuration { get; init; }

    public TimeSpan? ExitDuration { get; init; }

    public TooltipTriggerMode? TriggerMode { get; init; }

    public bool? EnableFeedback { get; init; }

    public TooltipThemeData CopyWith(
        double? height = null,
        BoxConstraints? constraints = null,
        EdgeInsetsGeometry? padding = null,
        EdgeInsetsGeometry? margin = null,
        double? verticalOffset = null,
        bool? preferBelow = null,
        bool? excludeFromSemantics = null,
        Decoration? decoration = null,
        TextStyle? textStyle = null,
        TextAlign? textAlign = null,
        TimeSpan? waitDuration = null,
        TimeSpan? showDuration = null,
        TimeSpan? exitDuration = null,
        TooltipTriggerMode? triggerMode = null,
        bool? enableFeedback = null)
    {
        // Flutter 3.44's constructor call intentionally omits exitDuration here.
        return new TooltipThemeData(
            Height: height ?? Height,
            Constraints: constraints ?? Constraints,
            Padding: padding ?? Padding,
            Margin: margin ?? Margin,
            VerticalOffset: verticalOffset ?? VerticalOffset,
            PreferBelow: preferBelow ?? PreferBelow,
            ExcludeFromSemantics: excludeFromSemantics ?? ExcludeFromSemantics,
            Decoration: decoration ?? Decoration,
            TextStyle: textStyle ?? TextStyle,
            TextAlign: textAlign ?? TextAlign,
            WaitDuration: waitDuration ?? WaitDuration,
            ShowDuration: showDuration ?? ShowDuration,
            TriggerMode: triggerMode ?? TriggerMode,
            EnableFeedback: enableFeedback ?? EnableFeedback);
    }

    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new DoubleProperty("height", Height, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>("constraints", Constraints, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>("padding", Padding, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>("margin", Margin, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("vertical offset", VerticalOffset, defaultValue: nullDefault));
        properties.Add(new FlagProperty("position", PreferBelow, "below", "above", showName: true));
        properties.Add(new FlagProperty("semantics", ExcludeFromSemantics, "excluded", showName: true));
        properties.Add(new DiagnosticsProperty<Decoration?>("decoration", Decoration, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle?>("textStyle", TextStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextAlign?>("textAlign", TextAlign, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TimeSpan?>("wait duration", WaitDuration, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TimeSpan?>("show duration", ShowDuration, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TimeSpan?>("exit duration", ExitDuration, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TooltipTriggerMode?>(
            "triggerMode",
            TriggerMode,
            defaultValue: nullDefault));
        properties.Add(new FlagProperty("enableFeedback", EnableFeedback, "true", showName: true));
    }
}

public sealed class TooltipTheme : InheritedTheme
{
    public TooltipTheme(TooltipThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TooltipThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new TooltipTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((TooltipTheme)oldWidget).Data, Data);
    }

    public static TooltipThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<TooltipTheme>()?.Data ?? Theme.Of(context).TooltipTheme;
    }
}
