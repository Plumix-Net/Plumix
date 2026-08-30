using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/container.dart

namespace Plumix.Widgets;

/// <summary>A widget that paints a <see cref="Painting.Decoration"/> either before or after it paints
/// its child.</summary>
public sealed class DecoratedBox : SingleChildRenderObjectWidget
{
    public DecoratedBox(
        Decoration decoration,
        DecorationPosition position = DecorationPosition.Background,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        Position = position;
    }

    /// <summary>What decoration to paint.</summary>
    public Decoration Decoration { get; }

    /// <summary>Whether to paint the box decoration behind or in front of the child.</summary>
    public DecorationPosition Position { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderDecoratedBox(
            Decoration,
            position: Position,
            configuration: ImageConfigurationUtils.CreateLocalImageConfiguration(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var decoratedBox = (RenderDecoratedBox)renderObject;
        decoratedBox.DecorationValue = Decoration;
        decoratedBox.Configuration = ImageConfigurationUtils.CreateLocalImageConfiguration(context);
        decoratedBox.Position = Position;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        string label = Position switch
        {
            DecorationPosition.Background => "bg",
            DecorationPosition.Foreground => "fg",
            _ => throw new ArgumentOutOfRangeException(nameof(Position)),
        };
        properties.Add(new EnumProperty<DecorationPosition>(
            "position",
            Position,
            level: DiagnosticLevel.Hidden));
        properties.Add(new DiagnosticsProperty<Decoration>(label, Decoration));
    }
}

/// <summary>A convenience widget that combines common painting, positioning, and sizing widgets.
/// </summary>
public sealed class Container : StatelessWidget
{
    public Container(
        Widget? child = null,
        Color? color = null,
        Decoration? decoration = null,
        AlignmentGeometry? alignment = null,
        EdgeInsetsGeometry? margin = null,
        BoxConstraints? constraints = null,
        Matrix4? transform = null,
        EdgeInsetsGeometry? padding = null,
        double? width = null,
        double? height = null,
        Key? key = null,
        Decoration? foregroundDecoration = null,
        AlignmentGeometry? transformAlignment = null,
        bool isAntiAlias = true,
        Clip clipBehavior = Clip.None) : base(key)
    {
        if (Constants.KDebugMode)
        {
            if (margin is { } marginValue && !marginValue.IsNonNegative)
            {
                throw new AssertionError("margin must be non-negative.");
            }

            if (padding is { } paddingValue && !paddingValue.IsNonNegative)
            {
                throw new AssertionError("padding must be non-negative.");
            }

            if (decoration is null && clipBehavior != Clip.None)
            {
                throw new AssertionError("Clipping a Container requires a decoration.");
            }

            if (color is not null && decoration is not null)
            {
                throw new AssertionError(
                    "Cannot provide both a color and a decoration.\n"
                    + "The color argument is just a shorthand for \"decoration: BoxDecoration(color: color)\".\n"
                    + "To use both a color and other decoration properties, set the color in the "
                    + "BoxDecoration instead.");
            }
        }

        Child = child;
        Color = color;
        Decoration = decoration;
        ForegroundDecoration = foregroundDecoration;
        Alignment = alignment;
        Margin = margin;
        Transform = transform;
        TransformAlignment = transformAlignment;
        PaddingInsets = padding;
        IsAntiAlias = isAntiAlias;
        ClipBehavior = clipBehavior;
        Constraints = width is not null || height is not null
            ? constraints?.Tighten(width: width, height: height)
              ?? BoxConstraints.TightFor(width: width, height: height)
            : constraints;
    }

    /// <summary>The child contained by the container.</summary>
    public Widget? Child { get; }

    /// <summary>Align the child within the container.</summary>
    public AlignmentGeometry? Alignment { get; }

    /// <summary>Empty space to inscribe inside the <see cref="Decoration"/>.</summary>
    /// <remarks>Dart's `Container.padding`; C# members may not repeat their declaring type's name.
    /// </remarks>
    public EdgeInsetsGeometry? PaddingInsets { get; }

    /// <summary>The color to paint behind the child.</summary>
    public Color? Color { get; }

    /// <summary>Whether the <see cref="ColoredBox"/> the color shorthand builds is anti-aliased.
    /// </summary>
    public bool IsAntiAlias { get; }

    /// <summary>The decoration to paint behind the child.</summary>
    public Decoration? Decoration { get; }

    /// <summary>The decoration to paint in front of the child.</summary>
    public Decoration? ForegroundDecoration { get; }

    /// <summary>Additional constraints to apply to the child.</summary>
    public BoxConstraints? Constraints { get; }

    /// <summary>Empty space to surround the <see cref="Decoration"/> and child.</summary>
    public EdgeInsetsGeometry? Margin { get; }

    /// <summary>The transformation matrix to apply before painting the container.</summary>
    public Matrix4? Transform { get; }

    /// <summary>The alignment of the origin, relative to the size of the container, if
    /// <see cref="Transform"/> is specified.</summary>
    public AlignmentGeometry? TransformAlignment { get; }

    /// <summary>The clip behavior when <see cref="Decoration"/> is not null. Defaults to
    /// <see cref="Clip.None"/>.</summary>
    public Clip ClipBehavior { get; }

    private EdgeInsetsGeometry? PaddingIncludingDecoration => (PaddingInsets, Decoration?.Padding) switch
    {
        (null, { } decorationPadding) => decorationPadding,
        ({ } padding, null) => padding,
        (null, null) => null,
        ({ } padding, { } decorationPadding) => padding.Add(decorationPadding),
    };

    public override Widget Build(BuildContext context)
    {
        Widget? current = Child;

        if (Child is null && (Constraints is null || !Constraints.Value.IsTight))
        {
            current = new LimitedBox(
                maxWidth: 0.0,
                maxHeight: 0.0,
                child: new ConstrainedBox(BoxConstraints.Expand()));
        }
        else if (Alignment is { } alignment)
        {
            current = new Align(alignment: alignment, child: current);
        }

        if (PaddingIncludingDecoration is { } effectivePadding)
        {
            current = new Padding(effectivePadding, current);
        }

        if (Color is { } color)
        {
            current = new ColoredBox(color, isAntiAlias: IsAntiAlias, child: current);
        }

        if (ClipBehavior != Clip.None)
        {
            current = new ClipPath(
                clipper: new DecorationClipper(Decoration!, Directionality.MaybeOf(context)),
                clipBehavior: ClipBehavior,
                child: current);
        }

        if (Decoration is { } decoration)
        {
            current = new DecoratedBox(decoration, child: current);
        }

        if (ForegroundDecoration is { } foregroundDecoration)
        {
            current = new DecoratedBox(
                foregroundDecoration,
                position: DecorationPosition.Foreground,
                child: current);
        }

        if (Constraints is { } constraints)
        {
            current = new ConstrainedBox(constraints, current);
        }

        if (Margin is { } margin)
        {
            current = new Padding(margin, current);
        }

        if (Transform is { } transform)
        {
            current = new Transform(transform, current, alignment: TransformAlignment);
        }

        return current!;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>(
            "alignment",
            Alignment,
            showName: false,
            defaultValue: null));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>(
            "padding",
            PaddingInsets,
            defaultValue: null));
        properties.Add(new DiagnosticsProperty<Clip>("clipBehavior", ClipBehavior, defaultValue: Clip.None));
        if (Color is { } color)
        {
            properties.Add(new DiagnosticsProperty<Color>("bg", color));
        }
        else
        {
            properties.Add(new DiagnosticsProperty<Decoration?>("bg", Decoration, defaultValue: null));
        }

        properties.Add(new DiagnosticsProperty<Decoration?>("fg", ForegroundDecoration, defaultValue: null));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>(
            "constraints",
            Constraints,
            defaultValue: null));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>("margin", Margin, defaultValue: null));
        properties.Add(ObjectFlagProperty<Matrix4>.Has("transform", Transform));
    }
}
