using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/banner.dart

public enum BannerLocation
{
    TopStart,
    TopEnd,
    BottomStart,
    BottomEnd,
}

public sealed class BannerPainter : CustomPainter
{
    public const double Offset = 40.0;
    public const double Height = 12.0;
    public static double BottomOffset => Offset + (Math.Sqrt(0.5) * Height);
    public static Rect BannerRect { get; } = new(-Offset, Offset - Height, Offset * 2.0, Height);
    public static Color DefaultColor { get; } = Color.FromArgb(0xA0, 0xB7, 0x1C, 0x1C);
    public static TextStyle DefaultTextStyle { get; } = new(
        FontSize: Height * 0.85,
        Color: Colors.White,
        FontWeight: FontWeight.Black,
        Height: 1.0);
    public static BoxShadow DefaultShadow { get; } = new()
    {
        Color = Color.FromArgb(0x7F, 0, 0, 0),
        Blur = 6.0,
        Spread = 0,
        OffsetX = 0,
        OffsetY = 0,
        IsInset = false,
    };

    private TextLayout? _textLayout;
    private bool _disposed;

    public BannerPainter(
        string message,
        TextDirection textDirection,
        BannerLocation location,
        TextDirection layoutDirection,
        Color? color = null,
        TextStyle? textStyle = null,
        BoxShadow? shadow = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        TextDirection = textDirection;
        Location = location;
        LayoutDirection = layoutDirection;
        Color = color ?? DefaultColor;
        TextStyle = textStyle ?? DefaultTextStyle;
        Shadow = shadow ?? DefaultShadow;
    }

    public string Message { get; }
    public TextDirection TextDirection { get; }
    public BannerLocation Location { get; }
    public TextDirection LayoutDirection { get; }
    public Color Color { get; }
    public TextStyle TextStyle { get; }
    public BoxShadow Shadow { get; }

    public double TranslationX(double width)
    {
        return (LayoutDirection, Location) switch
        {
            (TextDirection.Rtl, BannerLocation.TopStart) => width,
            (TextDirection.Ltr, BannerLocation.TopStart) => 0,
            (TextDirection.Rtl, BannerLocation.TopEnd) => 0,
            (TextDirection.Ltr, BannerLocation.TopEnd) => width,
            (TextDirection.Rtl, BannerLocation.BottomStart) => width - BottomOffset,
            (TextDirection.Ltr, BannerLocation.BottomStart) => BottomOffset,
            (TextDirection.Rtl, BannerLocation.BottomEnd) => BottomOffset,
            (TextDirection.Ltr, BannerLocation.BottomEnd) => width - BottomOffset,
            _ => 0,
        };
    }

    public double TranslationY(double height)
    {
        return Location is BannerLocation.BottomStart or BannerLocation.BottomEnd
            ? height - BottomOffset
            : 0;
    }

    public double Rotation => Math.PI / 4.0 * ((LayoutDirection, Location) switch
    {
        (TextDirection.Rtl, BannerLocation.TopStart or BannerLocation.BottomEnd) => 1,
        (TextDirection.Ltr, BannerLocation.TopStart or BannerLocation.BottomEnd) => -1,
        (TextDirection.Rtl, BannerLocation.BottomStart or BannerLocation.TopEnd) => -1,
        (TextDirection.Ltr, BannerLocation.BottomStart or BannerLocation.TopEnd) => 1,
        _ => 0,
    });

    public override void Paint(PaintingContext context, Size size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var translation = Matrix.CreateTranslation(
            TranslationX(size.Width),
            TranslationY(size.Height));
        context.PushTransform(translation, translatedContext =>
        {
            translatedContext.PushTransform(CreateRotationMatrix(Rotation), rotatedContext =>
            {
                rotatedContext.DrawRectangle(
                    Brushes.Transparent,
                    null,
                    BannerRect,
                    boxShadows: new BoxShadows(Shadow));
                rotatedContext.DrawRectangle(new SolidColorBrush(Color), null, BannerRect);
                PaintText(rotatedContext);
            });
        });
    }

    public bool ShouldRepaint(BannerPainter oldDelegate)
    {
        return Message != oldDelegate.Message
               || Location != oldDelegate.Location
               || Color != oldDelegate.Color
               || !Equals(TextStyle, oldDelegate.TextStyle);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) =>
        oldDelegate is not BannerPainter bannerPainter || ShouldRepaint(bannerPainter);

    public override bool? HitTest(Point position) => false;

    public override void Dispose()
    {
        _disposed = true;
        _textLayout = null;
    }

    private void PaintText(PaintingContext context)
    {
        try
        {
            _textLayout ??= new TextLayout(
                text: Message,
                typeface: new Typeface(
                    TextStyle.FontFamily ?? FontFamily.Default,
                    TextStyle.FontStyle ?? FontStyle.Normal,
                    TextStyle.FontWeight ?? FontWeight.Normal,
                    FontStretch.Normal),
                fontSize: TextStyle.FontSize ?? DefaultTextStyle.FontSize!.Value,
                foreground: new SolidColorBrush(TextStyle.Color ?? Colors.White),
                textAlignment: TextAlignment.Center,
                textWrapping: TextWrapping.NoWrap,
                flowDirection: TextDirection == TextDirection.Rtl
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight,
                maxWidth: Offset * 2.0,
                maxHeight: double.PositiveInfinity,
                lineHeight: (TextStyle.FontSize ?? DefaultTextStyle.FontSize!.Value) * (TextStyle.Height ?? 1.0),
                letterSpacing: TextStyle.LetterSpacing ?? 0);

            context.DrawTextLayout(
                _textLayout,
                BannerRect.TopLeft + new Vector(0, (BannerRect.Height - _textLayout.Height) / 2.0));
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            // Font services are absent in host-less render tests.
        }
    }

    private static Matrix CreateRotationMatrix(double angle)
    {
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        return new Matrix(cosine, sine, -sine, cosine, 0, 0);
    }
}

public sealed class Banner : StatefulWidget
{
    public Banner(
        string message,
        BannerLocation location,
        Widget? child = null,
        TextDirection? textDirection = null,
        TextDirection? layoutDirection = null,
        Color? color = null,
        TextStyle? textStyle = null,
        BoxShadow? shadow = null,
        Key? key = null) : base(key)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Location = location;
        Child = child;
        TextDirection = textDirection;
        LayoutDirection = layoutDirection;
        Color = color ?? BannerPainter.DefaultColor;
        TextStyle = textStyle ?? BannerPainter.DefaultTextStyle;
        Shadow = shadow ?? BannerPainter.DefaultShadow;
    }

    public Widget? Child { get; }
    public string Message { get; }
    public TextDirection? TextDirection { get; }
    public BannerLocation Location { get; }
    public TextDirection? LayoutDirection { get; }
    public Color Color { get; }
    public TextStyle TextStyle { get; }
    public BoxShadow Shadow { get; }

    public override State CreateState() => new BannerState();

    private sealed class BannerState : State
    {
        private BannerPainter? _painter;

        private Banner CurrentWidget => (Banner)Element.Widget;

        public override Widget Build(BuildContext context)
        {
            _painter?.Dispose();
            Plumix.UI.TextDirection? directionality = null;
            if (CurrentWidget.TextDirection is null || CurrentWidget.LayoutDirection is null)
            {
                directionality = Directionality.Of(context);
            }
            _painter = new BannerPainter(
                message: CurrentWidget.Message,
                textDirection: CurrentWidget.TextDirection ?? directionality!.Value,
                location: CurrentWidget.Location,
                layoutDirection: CurrentWidget.LayoutDirection ?? directionality!.Value,
                color: CurrentWidget.Color,
                textStyle: CurrentWidget.TextStyle,
                shadow: CurrentWidget.Shadow);
            return new CustomPaint(foregroundPainter: _painter, child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _painter?.Dispose();
            base.Dispose();
        }
    }

}

public sealed class CheckedModeBanner : StatelessWidget
{
    public CheckedModeBanner(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
#if DEBUG
        return new Banner(
            message: "DEBUG",
            textDirection: TextDirection.Ltr,
            location: BannerLocation.TopEnd,
            child: Child);
#else
        return Child;
#endif
    }
}
