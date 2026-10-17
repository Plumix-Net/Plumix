using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text.dart

public sealed class DefaultTextStyle : InheritedTheme
{
    public DefaultTextStyle(
        TextStyle style,
        Widget child,
        Key? key = null,
        TextAlign? textAlign = null,
        bool softWrap = true,
        TextOverflow overflow = TextOverflow.Clip,
        int? maxLines = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null) : base(child, key)
    {
        ArgumentNullException.ThrowIfNull(style);
        if (maxLines is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines must be greater than zero.");
        }

        Style = style;
        TextAlign = textAlign;
        SoftWrap = softWrap;
        Overflow = overflow;
        MaxLines = maxLines;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
    }

    private DefaultTextStyle(Key? key = null) : this(new TextStyle(), new FallbackNullWidget(), key)
    {
    }

    public static DefaultTextStyle Fallback(Key? key = null) => new(key);

    public static Widget Merge(
        Widget child,
        TextStyle? style = null,
        TextAlign? textAlign = null,
        bool? softWrap = null,
        TextOverflow? overflow = null,
        int? maxLines = null,
        TextWidthBasis? textWidthBasis = null,
        TextHeightBehavior? textHeightBehavior = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new Builder(context =>
        {
            DefaultTextStyle parent = Of(context);
            return new DefaultTextStyle(
                style: parent.Style.Merge(style),
                child: child,
                key: key,
                textAlign: textAlign ?? parent.TextAlign,
                softWrap: softWrap ?? parent.SoftWrap,
                overflow: overflow ?? parent.Overflow,
                maxLines: maxLines ?? parent.MaxLines,
                textWidthBasis: textWidthBasis ?? parent.TextWidthBasis,
                textHeightBehavior: textHeightBehavior ?? parent.TextHeightBehavior);
        });
    }

    public TextStyle Style { get; }

    public TextAlign? TextAlign { get; }

    public bool SoftWrap { get; }

    public TextOverflow Overflow { get; }

    public int? MaxLines { get; }

    public TextWidthBasis TextWidthBasis { get; }

    public TextHeightBehavior? TextHeightBehavior { get; }

    public static DefaultTextStyle Of(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<DefaultTextStyle>() ?? Fallback();
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldStyle = (DefaultTextStyle)oldWidget;
        return Style.CompareTo(oldStyle.Style) != RenderComparison.Identical
               || oldStyle.TextAlign != TextAlign
               || oldStyle.SoftWrap != SoftWrap
               || oldStyle.Overflow != Overflow
               || oldStyle.MaxLines != MaxLines
               || oldStyle.TextWidthBasis != TextWidthBasis
               || oldStyle.TextHeightBehavior != TextHeightBehavior;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new DefaultTextStyle(
            style: Style,
            child: child,
            textAlign: TextAlign,
            softWrap: SoftWrap,
            overflow: Overflow,
            maxLines: MaxLines,
            textWidthBasis: TextWidthBasis,
            textHeightBehavior: TextHeightBehavior);
    }

    internal static DefaultTextStyle? MaybeOf(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<DefaultTextStyle>();
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        Style.DebugFillProperties(properties);
        properties.Add(new EnumProperty<TextAlign>("textAlign", TextAlign, defaultValue: null));
        properties.Add(new FlagProperty(
            "softWrap",
            SoftWrap,
            ifTrue: "wrapping at box width",
            ifFalse: "no wrapping except at line break characters",
            showName: true));
        properties.Add(new EnumProperty<TextOverflow>("overflow", Overflow, defaultValue: null));
        properties.Add(new IntProperty("maxLines", MaxLines, defaultValue: null));
        properties.Add(new EnumProperty<TextWidthBasis>(
            "textWidthBasis",
            TextWidthBasis,
            defaultValue: UI.TextWidthBasis.Parent));
        properties.Add(new DiagnosticsProperty<TextHeightBehavior?>(
            "textHeightBehavior",
            TextHeightBehavior,
            defaultValue: null));
    }

    private sealed class FallbackNullWidget : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            throw new FlutterError(
                "A DefaultTextStyle constructed with DefaultTextStyle.fallback cannot be incorporated into the "
                + "widget tree, it is meant only to provide a fallback value returned by DefaultTextStyle.of() "
                + "when no enclosing default text style is present in a BuildContext.");
        }
    }
}

/// The [TextHeightBehavior] that will apply to descendant [Text] and [EditableText]
/// widgets which have not explicitly set [Text.TextHeightBehavior].
///
/// If there is a [DefaultTextStyle] with a non-null [DefaultTextStyle.TextHeightBehavior]
/// below this widget, the [DefaultTextStyle.TextHeightBehavior] will be used
/// over this widget's [TextHeightBehavior].
/// Dart's <c>DefaultTextHeightBehavior</c>.
public sealed class DefaultTextHeightBehavior : InheritedTheme
{
    /// Creates a default text height behavior for the given subtree.
    public DefaultTextHeightBehavior(TextHeightBehavior textHeightBehavior, Widget child, Key? key = null)
        : base(child, key)
    {
        ArgumentNullException.ThrowIfNull(textHeightBehavior);
        TextHeightBehavior = textHeightBehavior;
    }

    /// {@macro dart.ui.textHeightBehavior}
    public TextHeightBehavior TextHeightBehavior { get; }

    /// The closest instance of [DefaultTextHeightBehavior] that encloses the
    /// given context, or null if none is found.
    public static TextHeightBehavior? MaybeOf(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<DefaultTextHeightBehavior>()?.TextHeightBehavior;
    }

    /// The closest instance of [DefaultTextHeightBehavior] that encloses the
    /// given context; throws when none is found.
    public static TextHeightBehavior Of(BuildContext context)
    {
        if (MaybeOf(context) is { } behavior)
        {
            return behavior;
        }

        throw new FlutterError(
            "DefaultTextHeightBehavior.of() was called with a context that does not contain a "
            + "DefaultTextHeightBehavior widget.\n"
            + "No DefaultTextHeightBehavior widget ancestor could be found starting from the context "
            + "that was passed to DefaultTextHeightBehavior.of(). This can happen because you are using "
            + "a widget that looks for a DefaultTextHeightBehavior ancestor, but no such ancestor "
            + "exists.\n"
            + $"The context used was:\n  {context}");
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(TextHeightBehavior, ((DefaultTextHeightBehavior)oldWidget).TextHeightBehavior);
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new DefaultTextHeightBehavior(textHeightBehavior: TextHeightBehavior, child: child);
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<TextHeightBehavior>(
            "textHeightBehavior",
            TextHeightBehavior,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}
