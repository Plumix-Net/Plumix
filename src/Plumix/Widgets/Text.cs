using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/text.dart

namespace Plumix.Widgets;

/// A run of text with a single style.
///
/// The [Text] widget displays a string of text with single style. The string
/// might break across multiple lines or might all be displayed on the same line
/// depending on the layout constraints.
///
/// The [Style] argument is optional. When omitted, the text will use the style
/// from the closest enclosing [DefaultTextStyle]. If the given style's
/// [TextStyle.Inherit] property is true (the default), the given style will
/// be merged with the closest enclosing [DefaultTextStyle].
///
/// Using the [Text.Rich] constructor, the [Text] widget can display a paragraph
/// with differently styled [TextSpan]s and inline [WidgetSpan]s.
public sealed class Text : StatelessWidget
{
    /// Creates a text widget.
    ///
    /// If the [Style] argument is null, the text will use the style from the
    /// closest enclosing [DefaultTextStyle].
    public Text(
        string data,
        Key? key = null,
        TextStyle? style = null,
        StrutStyle? strutStyle = null,
        TextAlign? textAlign = null,
        TextDirection? textDirection = null,
        Locale? locale = null,
        bool? softWrap = null,
        TextOverflow? overflow = null,
        double? textScaleFactor = null,
        TextScaler? textScaler = null,
        int? maxLines = null,
        string? semanticsLabel = null,
        string? semanticsIdentifier = null,
        TextWidthBasis? textWidthBasis = null,
        TextHeightBehavior? textHeightBehavior = null,
        Color? selectionColor = null)
        : this(
            data ?? throw new ArgumentNullException(nameof(data)),
            null,
            key,
            style,
            strutStyle,
            textAlign,
            textDirection,
            locale,
            softWrap,
            overflow,
            textScaleFactor,
            textScaler,
            maxLines,
            semanticsLabel,
            semanticsIdentifier,
            textWidthBasis,
            textHeightBehavior,
            selectionColor)
    {
    }

    private Text(
        string? data,
        InlineSpan? textSpan,
        Key? key,
        TextStyle? style,
        StrutStyle? strutStyle,
        TextAlign? textAlign,
        TextDirection? textDirection,
        Locale? locale,
        bool? softWrap,
        TextOverflow? overflow,
        double? textScaleFactor,
        TextScaler? textScaler,
        int? maxLines,
        string? semanticsLabel,
        string? semanticsIdentifier,
        TextWidthBasis? textWidthBasis,
        TextHeightBehavior? textHeightBehavior,
        Color? selectionColor) : base(key)
    {
        if (textScaler is not null && textScaleFactor is not null)
        {
            throw new ArgumentException(
                "textScaleFactor is deprecated and cannot be specified when textScaler is specified.",
                nameof(textScaleFactor));
        }

        Data = data;
        TextSpan = textSpan;
        Style = style;
        StrutStyle = strutStyle;
        TextAlign = textAlign;
        TextDirection = textDirection;
        Locale = locale;
        SoftWrap = softWrap;
        Overflow = overflow;
        TextScaleFactor = textScaleFactor;
        TextScaler = textScaler;
        MaxLines = maxLines;
        SemanticsLabel = semanticsLabel;
        SemanticsIdentifier = semanticsIdentifier;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
        SelectionColor = selectionColor;
    }

    /// Creates a text widget with an [InlineSpan].
    ///
    /// The following subclasses of [InlineSpan] may be used to build rich text:
    ///
    /// * [TextSpan]s define text and children [InlineSpan]s.
    /// * [WidgetSpan]s define embedded inline widgets.
    public static Text Rich(
        InlineSpan textSpan,
        Key? key = null,
        TextStyle? style = null,
        StrutStyle? strutStyle = null,
        TextAlign? textAlign = null,
        TextDirection? textDirection = null,
        Locale? locale = null,
        bool? softWrap = null,
        TextOverflow? overflow = null,
        double? textScaleFactor = null,
        TextScaler? textScaler = null,
        int? maxLines = null,
        string? semanticsLabel = null,
        string? semanticsIdentifier = null,
        TextWidthBasis? textWidthBasis = null,
        TextHeightBehavior? textHeightBehavior = null,
        Color? selectionColor = null)
    {
        ArgumentNullException.ThrowIfNull(textSpan);
        return new Text(
            null,
            textSpan,
            key,
            style,
            strutStyle,
            textAlign,
            textDirection,
            locale,
            softWrap,
            overflow,
            textScaleFactor,
            textScaler,
            maxLines,
            semanticsLabel,
            semanticsIdentifier,
            textWidthBasis,
            textHeightBehavior,
            selectionColor);
    }

    /// The text to display.
    ///
    /// This will be null if a [TextSpan] is provided instead.
    public string? Data { get; }

    /// The text to display as a [InlineSpan].
    ///
    /// This will be null if [Data] is provided instead.
    public InlineSpan? TextSpan { get; }

    /// If non-null, the style to use for this text.
    ///
    /// If the style's "inherit" property is true, the style will be merged with
    /// the closest enclosing [DefaultTextStyle]. Otherwise, the style will
    /// replace the closest enclosing [DefaultTextStyle].
    ///
    /// The user or platform may override this [Style]'s [TextStyle.FontWeight],
    /// [TextStyle.Height], [TextStyle.LetterSpacing], and [TextStyle.WordSpacing]
    /// via a [MediaQuery] ancestor's [MediaQueryData.BoldText],
    /// [MediaQueryData.LineHeightScaleFactorOverride],
    /// [MediaQueryData.LetterSpacingOverride], and [MediaQueryData.WordSpacingOverride]
    /// regardless of its [TextStyle.Inherit] value.
    public TextStyle? Style { get; }

    /// The strut style to use. Strut style defines the strut, which sets minimum vertical layout metrics.
    ///
    /// The user or platform may override this [StrutStyle]'s [StrutStyle.Height]
    /// via a [MediaQuery] ancestor's [MediaQueryData.LineHeightScaleFactorOverride].
    public StrutStyle? StrutStyle { get; }

    /// How the text should be aligned horizontally.
    public TextAlign? TextAlign { get; }

    /// The directionality of the text.
    ///
    /// Defaults to the ambient [Directionality], if any.
    public TextDirection? TextDirection { get; }

    /// Used to select a font when the same Unicode character can be rendered
    /// differently, depending on the locale.
    ///
    /// It's rarely necessary to set this property. By default its value
    /// is inherited from the enclosing app with `Localizations.localeOf(context)`.
    public Locale? Locale { get; }

    /// Whether the text should break at soft line breaks.
    ///
    /// If false, the glyphs in the text will be positioned as if there was unlimited horizontal space.
    public bool? SoftWrap { get; }

    /// How visual overflow should be handled.
    ///
    /// If this is null [TextStyle.Overflow] will be used, otherwise the value
    /// from the nearest [DefaultTextStyle] ancestor will be used.
    public TextOverflow? Overflow { get; }

    /// Deprecated. Will be removed in a future version of Flutter. Use
    /// [TextScaler] instead.
    public double? TextScaleFactor { get; }

    /// The font scaling strategy to use when laying the text out.
    public TextScaler? TextScaler { get; }

    /// An optional maximum number of lines for the text to span, wrapping if necessary.
    /// If the text exceeds the given number of lines, it will be truncated according
    /// to [Overflow].
    public int? MaxLines { get; }

    /// An alternative semantics label for this text.
    public string? SemanticsLabel { get; }

    /// A unique identifier for the semantics node for this widget.
    public string? SemanticsIdentifier { get; }

    /// Defines how to measure the width of the rendered text.
    public TextWidthBasis? TextWidthBasis { get; }

    /// Defines how to apply [TextStyle.Height] over and under text.
    public TextHeightBehavior? TextHeightBehavior { get; }

    /// The color to use when painting the selection.
    ///
    /// This is ignored if [SelectionContainer.MaybeOf] returns null
    /// in the [BuildContext] of the [Text] widget.
    ///
    /// If null, the ambient [DefaultSelectionStyle] is used (if any); failing
    /// that, the selection color defaults to [DefaultSelectionStyle.DefaultColor].
    public Color? SelectionColor { get; }

    public override Widget Build(BuildContext context)
    {
        DefaultTextStyle defaultTextStyle = DefaultTextStyle.Of(context);
        TextStyle? effectiveTextStyle = Style;
        if (Style is null || Style.Inherit)
        {
            effectiveTextStyle = defaultTextStyle.Style.Merge(Style);
        }

        if (MediaQuery.BoldTextOf(context))
        {
            effectiveTextStyle = effectiveTextStyle!.Merge(new TextStyle(FontWeight: FontWeight.Bold));
        }

        // TODO(Renzo-Olivares): Investigate ways the framework can automatically
        // apply MediaQueryData.paragraphSpacingOverride to its own text components.
        // See: https://github.com/flutter/flutter/issues/177953 and https://github.com/flutter/flutter/issues/177408.
        double? lineHeightScaleFactor = MediaQuery.MaybeLineHeightScaleFactorOverrideOf(context);
        double? letterSpacing = MediaQuery.MaybeLetterSpacingOverrideOf(context);
        double? wordSpacing = MediaQuery.MaybeWordSpacingOverrideOf(context);
        TextSpan effectiveTextSpan = OverridingTextStyleTextSpanUtils.ApplyTextSpacingOverrides(
            lineHeightScaleFactor: lineHeightScaleFactor,
            letterSpacing: letterSpacing,
            wordSpacing: wordSpacing,
            textSpan: new TextSpan(
                style: effectiveTextStyle,
                text: Data,
                locale: Locale?.ToLanguageTag(),
                children: TextSpan is not null ? [TextSpan] : null));
        StrutStyle? effectiveStrutStyle = StrutStyle?.Merge(new StrutStyle(Height: lineHeightScaleFactor));
        ISelectionRegistrar? registrar = SelectionContainer.MaybeOf(context);
        TextScaler textScaler = (TextScaler, TextScaleFactor) switch
        {
            ({ } scaler, _) => scaler,
            // For unmigrated apps, fall back to textScaleFactor.
            (null, { } textScaleFactor) => Painting.TextScaler.Linear(textScaleFactor),
            (null, null) => MediaQuery.TextScalerOf(context),
        };
        Widget result;
        if (registrar is not null)
        {
            result = new MouseRegion(
                cursor: DefaultSelectionStyle.Of(context).MouseCursor ?? SystemMouseCursors.Text,
                child: new SelectableTextContainer(
                    textAlign: TextAlign ?? defaultTextStyle.TextAlign ?? UI.TextAlign.Start,
                    // RichText uses Directionality.of to obtain a default if this is null.
                    textDirection: TextDirection,
                    // RichText uses Localizations.localeOf to obtain a default if this is null
                    locale: Locale,
                    softWrap: SoftWrap ?? defaultTextStyle.SoftWrap,
                    overflow: Overflow ?? effectiveTextStyle?.Overflow ?? defaultTextStyle.Overflow,
                    textScaler: textScaler,
                    maxLines: MaxLines ?? defaultTextStyle.MaxLines,
                    strutStyle: effectiveStrutStyle,
                    textWidthBasis: TextWidthBasis ?? defaultTextStyle.TextWidthBasis,
                    textHeightBehavior: TextHeightBehavior
                                        ?? defaultTextStyle.TextHeightBehavior
                                        ?? DefaultTextHeightBehavior.MaybeOf(context),
                    selectionColor: SelectionColor
                                    ?? DefaultSelectionStyle.Of(context).SelectionColor
                                    ?? DefaultSelectionStyle.DefaultColor,
                    text: effectiveTextSpan));
        }
        else
        {
            result = new RichText(
                textAlign: TextAlign ?? defaultTextStyle.TextAlign ?? UI.TextAlign.Start,
                // RichText uses Directionality.of to obtain a default if this is null.
                textDirection: TextDirection,
                // RichText uses Localizations.localeOf to obtain a default if this is null
                locale: Locale,
                softWrap: SoftWrap ?? defaultTextStyle.SoftWrap,
                overflow: Overflow ?? effectiveTextStyle?.Overflow ?? defaultTextStyle.Overflow,
                textScaler: textScaler,
                maxLines: MaxLines ?? defaultTextStyle.MaxLines,
                strutStyle: effectiveStrutStyle,
                textWidthBasis: TextWidthBasis ?? defaultTextStyle.TextWidthBasis,
                textHeightBehavior: TextHeightBehavior
                                    ?? defaultTextStyle.TextHeightBehavior
                                    ?? DefaultTextHeightBehavior.MaybeOf(context),
                selectionColor: SelectionColor
                                ?? DefaultSelectionStyle.Of(context).SelectionColor
                                ?? DefaultSelectionStyle.DefaultColor,
                text: effectiveTextSpan);
        }

        if (SemanticsLabel is not null || SemanticsIdentifier is not null)
        {
            result = new Semantics(
                textDirection: TextDirection,
                label: SemanticsLabel,
                identifier: SemanticsIdentifier,
                child: new ExcludeSemantics(excluding: SemanticsLabel is not null, child: result));
        }

        return result;
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>Text.debugFillProperties</c>.</remarks>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new StringProperty("data", Data, showName: false));
        if (TextSpan is not null)
        {
            properties.Add(TextSpan.ToDiagnosticsNode(name: "textSpan", style: DiagnosticsTreeStyle.Transition));
        }

        Style?.DebugFillProperties(properties);
        properties.Add(new EnumProperty<TextAlign>(
            "textAlign",
            TextAlign,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<TextDirection>(
            "textDirection",
            TextDirection,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<Locale>("locale", Locale, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new FlagProperty(
            "softWrap",
            value: SoftWrap,
            ifTrue: "wrapping at box width",
            ifFalse: "no wrapping except at line break characters",
            showName: true));
        properties.Add(new EnumProperty<TextOverflow>(
            "overflow",
            Overflow,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty(
            "textScaleFactor",
            TextScaleFactor,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty("maxLines", MaxLines, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<TextWidthBasis>(
            "textWidthBasis",
            TextWidthBasis,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<TextHeightBehavior?>(
            "textHeightBehavior",
            TextHeightBehavior,
            defaultValue: DiagnosticsDefaults.NullValue));
        if (SemanticsLabel is not null)
        {
            properties.Add(new StringProperty("semanticsLabel", SemanticsLabel));
        }

        if (SemanticsIdentifier is not null)
        {
            properties.Add(new StringProperty("semanticsIdentifier", SemanticsIdentifier));
        }
    }

    /// Dart's `_SelectableTextContainer`.
    private sealed class SelectableTextContainer : StatefulWidget
    {
        public SelectableTextContainer(
            TextSpan text,
            TextAlign textAlign,
            TextDirection? textDirection,
            bool softWrap,
            TextOverflow overflow,
            TextScaler textScaler,
            int? maxLines,
            Locale? locale,
            StrutStyle? strutStyle,
            TextWidthBasis textWidthBasis,
            TextHeightBehavior? textHeightBehavior,
            Color selectionColor)
        {
            Text = text;
            TextAlign = textAlign;
            TextDirection = textDirection;
            SoftWrap = softWrap;
            Overflow = overflow;
            TextScaler = textScaler;
            MaxLines = maxLines;
            Locale = locale;
            StrutStyle = strutStyle;
            TextWidthBasis = textWidthBasis;
            TextHeightBehavior = textHeightBehavior;
            SelectionColor = selectionColor;
        }

        public TextSpan Text { get; }

        public TextAlign TextAlign { get; }

        public TextDirection? TextDirection { get; }

        public bool SoftWrap { get; }

        public TextOverflow Overflow { get; }

        public TextScaler TextScaler { get; }

        public int? MaxLines { get; }

        public Locale? Locale { get; }

        public StrutStyle? StrutStyle { get; }

        public TextWidthBasis TextWidthBasis { get; }

        public TextHeightBehavior? TextHeightBehavior { get; }

        public Color SelectionColor { get; }

        public override State CreateState() => new SelectableTextContainerState();
    }

    /// Dart's `_SelectableTextContainerState`.
    private sealed class SelectableTextContainerState : State<SelectableTextContainer>
    {
        private SelectableTextContainerDelegate _selectionDelegate = null!;
        private readonly GlobalKey _textKey = new LabeledGlobalKey<State>(null);

        public override void InitState()
        {
            base.InitState();
            _selectionDelegate = new SelectableTextContainerDelegate(_textKey);
        }

        public override void Dispose()
        {
            _selectionDelegate.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            SelectableTextContainer widget = (SelectableTextContainer)StateWidget;
            return new SelectionContainer(
                @delegate: _selectionDelegate,
                // Use [_RichText] wrapper so the underlying [RenderParagraph] can register
                // its [Selectable]s to the [SelectionContainer] created by this widget.
                child: new ContainerRichText(
                    textKey: _textKey,
                    textAlign: widget.TextAlign,
                    textDirection: widget.TextDirection,
                    locale: widget.Locale,
                    softWrap: widget.SoftWrap,
                    overflow: widget.Overflow,
                    textScaler: widget.TextScaler,
                    maxLines: widget.MaxLines,
                    strutStyle: widget.StrutStyle,
                    textWidthBasis: widget.TextWidthBasis,
                    textHeightBehavior: widget.TextHeightBehavior,
                    selectionColor: widget.SelectionColor,
                    text: widget.Text));
        }
    }

    /// Dart's `_RichText`: reads the registrar of the <see cref="SelectionContainer"/> above it.
    private sealed class ContainerRichText : StatelessWidget
    {
        public ContainerRichText(
            GlobalKey? textKey,
            InlineSpan text,
            TextAlign textAlign,
            TextDirection? textDirection,
            bool softWrap,
            TextOverflow overflow,
            TextScaler textScaler,
            int? maxLines,
            Locale? locale,
            StrutStyle? strutStyle,
            TextWidthBasis textWidthBasis,
            TextHeightBehavior? textHeightBehavior,
            Color selectionColor)
        {
            TextKey = textKey;
            Text = text;
            TextAlign = textAlign;
            TextDirection = textDirection;
            SoftWrap = softWrap;
            Overflow = overflow;
            TextScaler = textScaler;
            MaxLines = maxLines;
            Locale = locale;
            StrutStyle = strutStyle;
            TextWidthBasis = textWidthBasis;
            TextHeightBehavior = textHeightBehavior;
            SelectionColor = selectionColor;
        }

        public GlobalKey? TextKey { get; }

        public InlineSpan Text { get; }

        public TextAlign TextAlign { get; }

        public TextDirection? TextDirection { get; }

        public bool SoftWrap { get; }

        public TextOverflow Overflow { get; }

        public TextScaler TextScaler { get; }

        public int? MaxLines { get; }

        public Locale? Locale { get; }

        public StrutStyle? StrutStyle { get; }

        public TextWidthBasis TextWidthBasis { get; }

        public TextHeightBehavior? TextHeightBehavior { get; }

        public Color SelectionColor { get; }

        public override Widget Build(BuildContext context)
        {
            ISelectionRegistrar? registrar = SelectionContainer.MaybeOf(context);
            return new RichText(
                key: TextKey,
                textAlign: TextAlign,
                textDirection: TextDirection,
                locale: Locale,
                softWrap: SoftWrap,
                overflow: Overflow,
                textScaler: TextScaler,
                maxLines: MaxLines,
                strutStyle: StrutStyle,
                textWidthBasis: TextWidthBasis,
                textHeightBehavior: TextHeightBehavior,
                selectionRegistrar: registrar,
                selectionColor: SelectionColor,
                text: Text);
        }
    }

    /// Dart's `_SelectableTextContainerDelegate`: a delegate that manages updating multiple
    /// [ISelectable] children where the [ISelectable]s do not change or move around frequently.
    ///
    /// This delegate keeps track of the [ISelectable]s that received start or end
    /// [SelectionEvent]s and the global locations of those events to accurately
    /// synthesize [SelectionEvent]s for children [ISelectable]s when needed.
    ///
    /// When a new [SelectionEdgeUpdateEvent] is dispatched to a [ISelectable], this
    /// delegate checks whether the [ISelectable] has already received a selection
    /// update for each edge that currently exists, and synthesizes an event for the
    /// edges that have not yet received an update. This synthesized event is dispatched
    /// before dispatching the new event.
    ///
    /// For example, if we have an existing start edge for this delegate and a [ISelectable]
    /// receives an end [SelectionEdgeUpdateEvent] and the [ISelectable] did not receive
    /// the start [SelectionEdgeUpdateEvent], we synthesize a start [SelectionEdgeUpdateEvent]
    /// for the [ISelectable] and dispatch it before dispatching the original end
    /// [SelectionEdgeUpdateEvent].
    private sealed class SelectableTextContainerDelegate : StaticSelectionContainerDelegate
    {
        // In practice some selectables like widgetspan shift several pixels. So when
        // the vertical position diff is within the threshold, compare the horizontal
        // position to make the compareScreenOrder function more robust.
        private const double KSelectableVerticalComparingThreshold = 3.0;

        private readonly GlobalKey _textKey;

        public SelectableTextContainerDelegate(GlobalKey textKey)
        {
            _textKey = textKey;
        }

        private RenderParagraph Paragraph => (RenderParagraph)_textKey.CurrentContext!.FindRenderObject()!;

        protected override SelectionResult HandleSelectParagraph(SelectParagraphSelectionEvent @event)
        {
            SelectionResult result = HandleSelectParagraphCore(@event);
            DidReceiveSelectionBoundaryEvents();
            return result;
        }

        // Dart's `_handleSelectParagraph`.
        private SelectionResult HandleSelectParagraphCore(SelectParagraphSelectionEvent @event)
        {
            if (@event.Absorb)
            {
                for (int index = 0; index < Selectables.Count; index += 1)
                {
                    DispatchSelectionEventToChild(Selectables[index], @event);
                }

                CurrentSelectionStartIndex = 0;
                CurrentSelectionEndIndex = Selectables.Count - 1;
                return SelectionResult.Next;
            }

            // First pass, if the position is on a placeholder then dispatch the selection
            // event to the [Selectable] at the location and terminate.
            for (int index = 0; index < Selectables.Count; index += 1)
            {
                bool selectableIsPlaceholder = !Paragraph.SelectableBelongsToParagraph(Selectables[index]);
                if (selectableIsPlaceholder && Selectables[index].BoundingBoxes.Count > 0)
                {
                    foreach (Rect rect in Selectables[index].BoundingBoxes)
                    {
                        Rect globalRect = MatrixUtils.TransformRect(Selectables[index].GetTransformTo(null), rect);
                        if (SelectionUtils.RectContains(globalRect, @event.GlobalPosition))
                        {
                            CurrentSelectionStartIndex = CurrentSelectionEndIndex = index;
                            return DispatchSelectionEventToChild(Selectables[index], @event);
                        }
                    }
                }
            }

            SelectionResult? lastSelectionResult = null;
            bool foundStart = false;
            int? lastNextIndex = null;
            for (int index = 0; index < Selectables.Count; index += 1)
            {
                if (!Paragraph.SelectableBelongsToParagraph(Selectables[index]))
                {
                    if (foundStart)
                    {
                        SelectionEvent synthesizedEvent = new SelectParagraphSelectionEvent(
                            globalPosition: @event.GlobalPosition,
                            absorb: true);
                        SelectionResult result = DispatchSelectionEventToChild(Selectables[index], synthesizedEvent);
                        if (Selectables.Count - 1 == index)
                        {
                            CurrentSelectionEndIndex = index;
                            FlushInactiveSelections();
                            return result;
                        }
                    }

                    continue;
                }

                SelectionGeometry existingGeometry = Selectables[index].Value;
                lastSelectionResult = DispatchSelectionEventToChild(Selectables[index], @event);
                if (index == Selectables.Count - 1 && lastSelectionResult == SelectionResult.Next)
                {
                    if (foundStart)
                    {
                        CurrentSelectionEndIndex = index;
                    }
                    else
                    {
                        CurrentSelectionStartIndex = CurrentSelectionEndIndex = index;
                    }

                    return SelectionResult.Next;
                }

                if (lastSelectionResult == SelectionResult.Next)
                {
                    if (Equals(Selectables[index].Value, existingGeometry) && !foundStart)
                    {
                        lastNextIndex = index;
                    }

                    if (!Equals(Selectables[index].Value, existingGeometry) && !foundStart)
                    {
                        Debug.Assert(Selectables[index].BoundingBoxes.Count > 0);
                        Debug.Assert(Selectables[index].Value.SelectionRects.Count > 0);
                        bool selectionAtStartOfSelectable = RectOverlaps(
                            Selectables[index].BoundingBoxes[0],
                            Selectables[index].Value.SelectionRects[0]);
                        int startIndex;
                        if (lastNextIndex is { } nextIndex && selectionAtStartOfSelectable)
                        {
                            startIndex = nextIndex + 1;
                        }
                        else
                        {
                            startIndex = lastNextIndex is null && selectionAtStartOfSelectable ? 0 : index;
                        }

                        for (int i = startIndex; i < index; i += 1)
                        {
                            SelectionEvent synthesizedEvent = new SelectParagraphSelectionEvent(
                                globalPosition: @event.GlobalPosition,
                                absorb: true);
                            DispatchSelectionEventToChild(Selectables[i], synthesizedEvent);
                        }

                        CurrentSelectionStartIndex = startIndex;
                        foundStart = true;
                    }

                    continue;
                }

                if (index == 0 && lastSelectionResult == SelectionResult.Previous)
                {
                    return SelectionResult.Previous;
                }

                if (!Equals(Selectables[index].Value, existingGeometry))
                {
                    if (!foundStart && lastNextIndex is null)
                    {
                        CurrentSelectionStartIndex = 0;
                        for (int i = 0; i < index; i += 1)
                        {
                            SelectionEvent synthesizedEvent = new SelectParagraphSelectionEvent(
                                globalPosition: @event.GlobalPosition,
                                absorb: true);
                            DispatchSelectionEventToChild(Selectables[i], synthesizedEvent);
                        }
                    }

                    CurrentSelectionEndIndex = index;
                    // Geometry has changed as a result of select paragraph, need to clear the
                    // selection of other selectables to keep selection in sync.
                    FlushInactiveSelections();
                }

                return SelectionResult.End;
            }

            Debug.Assert(lastSelectionResult is null);
            return SelectionResult.End;
        }

        /// Initializes the selection of the selectable children.
        ///
        /// The goal is to find the selectable child that contains the selection edge.
        /// Returns [SelectionResult.End] if the selection edge ends on any of the
        /// children. Otherwise, it returns [SelectionResult.Previous] if the selection
        /// does not reach any of its children. Returns [SelectionResult.Next]
        /// if the selection reaches the end of its children.
        ///
        /// Ideally, this method should only be called twice at the beginning of the
        /// drag selection, once for start edge update event, once for end edge update
        /// event.
        private SelectionResult AdjustSelection(SelectionEdgeUpdateEvent @event, bool isEnd)
        {
            if (isEnd)
            {
                Debug.Assert(CurrentSelectionEndIndex < Selectables.Count && CurrentSelectionEndIndex >= 0);
            }
            else
            {
                Debug.Assert(CurrentSelectionStartIndex < Selectables.Count && CurrentSelectionStartIndex >= 0);
            }

            SelectionResult? finalResult = null;
            bool isCurrentEdgeWithinViewport = isEnd
                ? Value.EndSelectionPoint is not null
                : Value.StartSelectionPoint is not null;
            bool isOppositeEdgeWithinViewport = isEnd
                ? Value.StartSelectionPoint is not null
                : Value.EndSelectionPoint is not null;
            int newIndex = (isEnd, isCurrentEdgeWithinViewport, isOppositeEdgeWithinViewport) switch
            {
                (true, true, true) => CurrentSelectionEndIndex,
                (true, true, false) => CurrentSelectionEndIndex,
                (true, false, true) => CurrentSelectionStartIndex,
                (true, false, false) => 0,
                (false, true, true) => CurrentSelectionStartIndex,
                (false, true, false) => CurrentSelectionStartIndex,
                (false, false, true) => CurrentSelectionEndIndex,
                (false, false, false) => 0,
            };
            bool? forward = null;
            while (newIndex < Selectables.Count && newIndex >= 0 && finalResult is null)
            {
                SelectionResult currentSelectableResult = DispatchSelectionEventToChild(Selectables[newIndex], @event);
                switch (currentSelectableResult)
                {
                    case SelectionResult.End:
                    case SelectionResult.Pending:
                    case SelectionResult.None:
                        finalResult = currentSelectableResult;
                        break;
                    case SelectionResult.Next:
                        if (forward == false)
                        {
                            newIndex += 1;
                            finalResult = SelectionResult.End;
                        }
                        else if (newIndex == Selectables.Count - 1)
                        {
                            finalResult = currentSelectableResult;
                        }
                        else
                        {
                            forward = true;
                            newIndex += 1;
                        }

                        break;
                    case SelectionResult.Previous:
                        if (forward ?? false)
                        {
                            newIndex -= 1;
                            finalResult = SelectionResult.End;
                        }
                        else if (newIndex == 0)
                        {
                            finalResult = currentSelectableResult;
                        }
                        else
                        {
                            forward = false;
                            newIndex -= 1;
                        }

                        break;
                }
            }

            if (isEnd)
            {
                bool forwardSelection = CurrentSelectionEndIndex >= CurrentSelectionStartIndex;
                if (forward is { } isForward
                    && ((!forwardSelection && isForward && newIndex >= CurrentSelectionStartIndex)
                        || (forwardSelection && !isForward && newIndex <= CurrentSelectionStartIndex)))
                {
                    CurrentSelectionStartIndex = CurrentSelectionEndIndex;
                }

                CurrentSelectionEndIndex = newIndex;
            }
            else
            {
                bool forwardSelection = CurrentSelectionEndIndex >= CurrentSelectionStartIndex;
                if (forward is { } isForward
                    && ((!forwardSelection && !isForward && newIndex <= CurrentSelectionEndIndex)
                        || (forwardSelection && isForward && newIndex >= CurrentSelectionEndIndex)))
                {
                    CurrentSelectionEndIndex = CurrentSelectionStartIndex;
                }

                CurrentSelectionStartIndex = newIndex;
            }

            FlushInactiveSelections();
            return finalResult!.Value;
        }

        /// The compare function this delegate used for determining the selection
        /// order of the [ISelectable]s.
        ///
        /// Sorts the [ISelectable]s by their top left [Rect].
        protected override Comparison<ISelectable> CompareOrder => CompareScreenOrder;

        private static int CompareScreenOrder(ISelectable a, ISelectable b)
        {
            Rect rectA = MatrixUtils.TransformRect(a.GetTransformTo(null), a.BoundingBoxes[0]);
            Rect rectB = MatrixUtils.TransformRect(b.GetTransformTo(null), b.BoundingBoxes[0]);
            int result = CompareVertically(rectA, rectB);
            if (result != 0)
            {
                return result;
            }

            return CompareHorizontally(rectA, rectB);
        }

        /// Compares two rectangles in the screen order solely by their vertical
        /// positions.
        ///
        /// Returns positive if a is lower, negative if a is higher, 0 if their
        /// order can't be determine solely by their vertical position.
        private static int CompareVertically(Rect a, Rect b)
        {
            // The rectangles overlap so defer to horizontal comparison.
            if ((a.Top - b.Top < KSelectableVerticalComparingThreshold
                 && a.Bottom - b.Bottom > -KSelectableVerticalComparingThreshold)
                || (b.Top - a.Top < KSelectableVerticalComparingThreshold
                    && b.Bottom - a.Bottom > -KSelectableVerticalComparingThreshold))
            {
                return 0;
            }

            if (Math.Abs(a.Top - b.Top) > KSelectableVerticalComparingThreshold)
            {
                return a.Top > b.Top ? 1 : -1;
            }

            return a.Bottom > b.Bottom ? 1 : -1;
        }

        /// Compares two rectangles in the screen order by their horizontal positions
        /// assuming one of the rectangles enclose the other rect vertically.
        ///
        /// Returns positive if a is lower, negative if a is higher.
        private static int CompareHorizontally(Rect a, Rect b)
        {
            // a encloses b.
            if (a.Left - b.Left < Constants.PrecisionErrorTolerance
                && a.Right - b.Right > -Constants.PrecisionErrorTolerance)
            {
                return -1;
            }

            // b encloses a.
            if (b.Left - a.Left < Constants.PrecisionErrorTolerance
                && b.Right - a.Right > -Constants.PrecisionErrorTolerance)
            {
                return 1;
            }

            if (Math.Abs(a.Left - b.Left) > Constants.PrecisionErrorTolerance)
            {
                return a.Left > b.Left ? 1 : -1;
            }

            return a.Right > b.Right ? 1 : -1;
        }

        /// This method calculates a local [SelectedContentRange] based on the list
        /// of [selections] that are accumulated from the [ISelectable] children under this
        /// delegate. This calculation takes into account the accumulated content
        /// length before the active selection, and returns null when either selection
        /// edge has not been set.
        private SelectedContentRange? CalculateLocalRange(
            IReadOnlyList<(int ContentLength, SelectedContentRange? Range)> selections)
        {
            if (CurrentSelectionStartIndex == -1 || CurrentSelectionEndIndex == -1)
            {
                return null;
            }

            int startOffset = 0;
            int endOffset = 0;
            bool foundStart = false;
            bool forwardSelection = CurrentSelectionEndIndex >= CurrentSelectionStartIndex;
            if (CurrentSelectionEndIndex == CurrentSelectionStartIndex)
            {
                SelectedContentRange rangeAtSelectableInSelection =
                    Selectables[CurrentSelectionStartIndex].GetSelection()!;
                forwardSelection = rangeAtSelectableInSelection.EndOffset >= rangeAtSelectableInSelection.StartOffset;
            }

            for (int index = 0; index < selections.Count; index++)
            {
                (int contentLength, SelectedContentRange? range) = selections[index];
                if (range is null)
                {
                    if (foundStart)
                    {
                        return new SelectedContentRange(
                            startOffset: forwardSelection ? startOffset : endOffset,
                            endOffset: forwardSelection ? endOffset : startOffset);
                    }

                    startOffset += contentLength;
                    endOffset = startOffset;
                    continue;
                }

                int selectionStartNormalized = Math.Min(range.StartOffset, range.EndOffset);
                int selectionEndNormalized = Math.Max(range.StartOffset, range.EndOffset);
                if (!foundStart)
                {
                    // Because a RenderParagraph may split its content into multiple selectables
                    // we have to consider at what offset a selectable starts at relative
                    // to the RenderParagraph, when the selectable is not the start of the content.
                    bool shouldConsiderContentStart =
                        index > 0 && Paragraph.SelectableBelongsToParagraph(Selectables[index]);
                    int contentStart = 0;
                    if (shouldConsiderContentStart)
                    {
                        Rect firstBox = Selectables[index].BoundingBoxes[0];
                        contentStart = Paragraph
                            .GetPositionForOffset(new Point(firstBox.Left, firstBox.Top + firstBox.Height / 2.0))
                            .Offset;
                    }

                    startOffset += Math.Abs(selectionStartNormalized - contentStart);
                    endOffset = startOffset + Math.Abs(selectionEndNormalized - selectionStartNormalized);
                    foundStart = true;
                }
                else
                {
                    endOffset += Math.Abs(selectionEndNormalized - selectionStartNormalized);
                }
            }

            Debug.Assert(
                foundStart,
                "The start of the selection has not been found despite this selection delegate having an "
                + "existing currentSelectionStartIndex and currentSelectionEndIndex.");
            return new SelectedContentRange(
                startOffset: forwardSelection ? startOffset : endOffset,
                endOffset: forwardSelection ? endOffset : startOffset);
        }

        /// Returns a [SelectedContentRange] considering the [SelectedContentRange]
        /// from each [ISelectable] child managed under this delegate.
        ///
        /// When nothing is selected or either selection edge has not been set,
        /// this method will return `null`.
        public override SelectedContentRange? GetSelection()
        {
            var selections = new List<(int ContentLength, SelectedContentRange? Range)>();
            foreach (ISelectable selectable in Selectables)
            {
                selections.Add((selectable.ContentLength, selectable.GetSelection()));
            }

            return CalculateLocalRange(selections);
        }

        /// Clears the selection on all selectables not in the range of
        /// currentSelectionStartIndex..currentSelectionEndIndex.
        ///
        /// If one of the edges does not exist, then this method will clear the selection
        /// in all selectables except the existing edge.
        ///
        /// If neither of the edges exist this method immediately returns.
        private void FlushInactiveSelections()
        {
            if (CurrentSelectionStartIndex == -1 && CurrentSelectionEndIndex == -1)
            {
                return;
            }

            if (CurrentSelectionStartIndex == -1 || CurrentSelectionEndIndex == -1)
            {
                int skipIndex = CurrentSelectionStartIndex == -1
                    ? CurrentSelectionEndIndex
                    : CurrentSelectionStartIndex;
                for (int i = 0; i < Selectables.Count; i++)
                {
                    if (i == skipIndex)
                    {
                        continue;
                    }

                    DispatchSelectionEventToChild(Selectables[i], new ClearSelectionEvent());
                }

                return;
            }

            int skipStart = Math.Min(CurrentSelectionStartIndex, CurrentSelectionEndIndex);
            int skipEnd = Math.Max(CurrentSelectionStartIndex, CurrentSelectionEndIndex);
            for (int index = 0; index < Selectables.Count; index += 1)
            {
                if (index >= skipStart && index <= skipEnd)
                {
                    continue;
                }

                DispatchSelectionEventToChild(Selectables[index], new ClearSelectionEvent());
            }
        }

        protected override SelectionResult HandleSelectionEdgeUpdate(SelectionEdgeUpdateEvent @event)
        {
            if (@event.Granularity != TextGranularity.Paragraph)
            {
                return base.HandleSelectionEdgeUpdate(@event);
            }

            UpdateLastSelectionEdgeLocation(
                globalSelectionEdgeLocation: @event.GlobalPosition,
                forEnd: @event.Type == SelectionEventType.EndEdgeUpdate);
            if (@event.Type == SelectionEventType.EndEdgeUpdate)
            {
                return CurrentSelectionEndIndex == -1
                    ? base.HandleSelectionEdgeUpdate(@event)
                    : AdjustSelection(@event, isEnd: true);
            }

            return CurrentSelectionStartIndex == -1
                ? base.HandleSelectionEdgeUpdate(@event)
                : AdjustSelection(@event, isEnd: false);
        }

        // dart:ui `Rect.overlaps`.
        private static bool RectOverlaps(Rect a, Rect b)
        {
            if (a.Right <= b.Left || b.Right <= a.Left)
            {
                return false;
            }

            if (a.Bottom <= b.Top || b.Bottom <= a.Top)
            {
                return false;
            }

            return true;
        }
    }
}

/// A utility class for overriding the text styles of a [TextSpan] tree.
/// Dart's `_OverridingTextStyleTextSpanUtils`, which text.dart and editable_text.dart each declare
/// identically; C# shares one internal copy.
// TODO(Renzo-Olivares): Remove after investigating a solution for overriding all
// styles for children in an [InlineSpan] tree, see: https://github.com/flutter/flutter/issues/177952.
internal static class OverridingTextStyleTextSpanUtils
{
    public static TextSpan ApplyTextSpacingOverrides(
        double? lineHeightScaleFactor,
        double? letterSpacing,
        double? wordSpacing,
        TextSpan textSpan)
    {
        if (lineHeightScaleFactor is null && letterSpacing is null && wordSpacing is null)
        {
            return textSpan;
        }

        return ApplyTextStyleOverrides(
            new TextStyle(
                Height: lineHeightScaleFactor,
                LetterSpacing: letterSpacing,
                WordSpacing: wordSpacing),
            textSpan);
    }

    private static TextSpan ApplyTextStyleOverrides(TextStyle overrideTextStyle, TextSpan textSpan)
    {
        return new TextSpan(
            text: textSpan.Text,
            children: textSpan.Children?.Select(child =>
            {
                if (child is TextSpan childSpan && child.GetType() == typeof(TextSpan))
                {
                    return ApplyTextStyleOverrides(overrideTextStyle, childSpan);
                }

                return child;
            }).ToList(),
            style: textSpan.Style?.Merge(overrideTextStyle) ?? overrideTextStyle,
            recognizer: textSpan.Recognizer,
            mouseCursor: textSpan.MouseCursor,
            onEnter: textSpan.OnEnter,
            onExit: textSpan.OnExit,
            semanticsLabel: textSpan.SemanticsLabel,
            semanticsIdentifier: textSpan.SemanticsIdentifier,
            locale: textSpan.Locale,
            spellOut: textSpan.SpellOut);
    }
}
