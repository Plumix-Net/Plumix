using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/circle_avatar.dart

public sealed class CircleAvatar : StatelessWidget
{
    private const double DefaultRadius = 20.0;
    private const double DefaultMinRadius = 0.0;
    private const double DefaultMaxRadius = double.PositiveInfinity;
    private static readonly TimeSpan ThemeChangeDuration = TimeSpan.FromMilliseconds(200);

    public CircleAvatar(
        Widget? child = null,
        Color? backgroundColor = null,
        ImageProvider? backgroundImage = null,
        ImageProvider? foregroundImage = null,
        ImageErrorListener? onBackgroundImageError = null,
        ImageErrorListener? onForegroundImageError = null,
        Color? foregroundColor = null,
        double? radius = null,
        double? minRadius = null,
        double? maxRadius = null,
        Key? key = null) : base(key)
    {
        if (radius.HasValue && (minRadius.HasValue || maxRadius.HasValue))
        {
            throw new ArgumentException("radius cannot be combined with minRadius or maxRadius.");
        }
        if (backgroundImage is null && onBackgroundImageError is not null)
        {
            throw new ArgumentException("onBackgroundImageError requires backgroundImage.", nameof(onBackgroundImageError));
        }
        if (foregroundImage is null && onForegroundImageError is not null)
        {
            throw new ArgumentException("onForegroundImageError requires foregroundImage.", nameof(onForegroundImageError));
        }

        ValidateRadius(radius, nameof(radius), allowInfinity: false);
        ValidateRadius(minRadius, nameof(minRadius), allowInfinity: false);
        ValidateRadius(maxRadius, nameof(maxRadius), allowInfinity: true);
        if (minRadius.HasValue && maxRadius.HasValue && minRadius.Value > maxRadius.Value)
        {
            throw new ArgumentException("minRadius cannot exceed maxRadius.");
        }

        Child = child;
        BackgroundColor = backgroundColor;
        BackgroundImage = backgroundImage;
        ForegroundImage = foregroundImage;
        OnBackgroundImageError = onBackgroundImageError;
        OnForegroundImageError = onForegroundImageError;
        ForegroundColor = foregroundColor;
        Radius = radius;
        MinRadius = minRadius;
        MaxRadius = maxRadius;
    }

    public Widget? Child { get; }
    public Color? BackgroundColor { get; }
    public ImageProvider? BackgroundImage { get; }
    public ImageProvider? ForegroundImage { get; }
    public ImageErrorListener? OnBackgroundImageError { get; }
    public ImageErrorListener? OnForegroundImageError { get; }
    public Color? ForegroundColor { get; }
    public double? Radius { get; }
    public double? MinRadius { get; }
    public double? MaxRadius { get; }

    internal double MinDiameter => Radius is null && MinRadius is null && MaxRadius is null
        ? DefaultRadius * 2.0
        : 2.0 * (Radius ?? MinRadius ?? DefaultMinRadius);

    internal double MaxDiameter => Radius is null && MinRadius is null && MaxRadius is null
        ? DefaultRadius * 2.0
        : 2.0 * (Radius ?? MaxRadius ?? DefaultMaxRadius);

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var effectiveForegroundColor = ForegroundColor
                                       ?? (theme.UseMaterial3 ? theme.ColorScheme.OnPrimaryContainer : null);
        var effectiveTextStyle = theme.UseMaterial3
            ? theme.TextTheme.TitleMedium
            : theme.PrimaryTextTheme.TitleMedium;
        var textStyle = effectiveTextStyle.CopyWith(color: effectiveForegroundColor);
        var effectiveBackgroundColor = BackgroundColor
                                       ?? (theme.UseMaterial3 ? theme.ColorScheme.PrimaryContainer : null);

        if (!effectiveBackgroundColor.HasValue)
        {
            effectiveBackgroundColor = ThemeData.EstimateBrightnessForColor(textStyle.Color ?? Colors.Black) switch
            {
                Brightness.Dark => theme.PrimaryColorLight,
                _ => theme.PrimaryColorDark,
            };
        }
        else if (!effectiveForegroundColor.HasValue)
        {
            textStyle = textStyle.CopyWith(
                color: ThemeData.EstimateBrightnessForColor(BackgroundColor!.Value) switch
                {
                    Brightness.Dark => theme.PrimaryColorLight,
                    _ => theme.PrimaryColorDark,
                });
        }

        Widget? avatarChild = null;
        if (Child is not null)
        {
            avatarChild = new Center(
                child: MediaQuery.WithNoTextScaling(
                    context,
                    new IconTheme(
                        data: theme.IconTheme.CopyWith(color: textStyle.Color),
                        child: new DefaultTextStyle(
                            style: textStyle,
                            child: Child))));
        }

        return new AnimatedContainer(
            duration: ThemeChangeDuration,
            constraints: new BoxConstraints(
                MinWidth: MinDiameter,
                MaxWidth: MaxDiameter,
                MinHeight: MinDiameter,
                MaxHeight: MaxDiameter),
            decoration: new BoxDecoration(
                Color: effectiveBackgroundColor,
                Image: BackgroundImage is null
                    ? null
                    : new DecorationImage(
                        BackgroundImage,
                        onError: OnBackgroundImageError,
                        fit: BoxFit.Cover),
                Shape: BoxShape.Circle),
            foregroundDecoration: ForegroundImage is null
                ? null
                : new BoxDecoration(
                    Image: new DecorationImage(
                        ForegroundImage,
                        onError: OnForegroundImageError,
                        fit: BoxFit.Cover),
                    Shape: BoxShape.Circle),
            child: avatarChild);
    }

    private static void ValidateRadius(double? value, string parameterName, bool allowInfinity)
    {
        if (!value.HasValue) return;
        if (double.IsNaN(value.Value)
            || value.Value < 0
            || (!allowInfinity && double.IsInfinity(value.Value)))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Radius must be non-negative.");
        }
    }
}
