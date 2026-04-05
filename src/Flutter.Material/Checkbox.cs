using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/checkbox.dart (approximate baseline for current framework scope)

public sealed class Checkbox : StatelessWidget
{
    public const double Width = 18.0;

    public Checkbox(
        bool? value,
        Action<bool?>? onChanged,
        bool tristate = false,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        BorderRadius? shape = null,
        BorderSide? side = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Key? key = null) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException("Checkbox value cannot be null when tristate is false.", nameof(value));
        }

        Value = value;
        OnChanged = onChanged;
        Tristate = tristate;
        ActiveColor = activeColor;
        FillColor = fillColor;
        CheckColor = checkColor;
        OverlayColor = overlayColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        MaterialTapTargetSize = materialTapTargetSize;
        Shape = shape;
        Side = side;
        FocusNode = focusNode;
        Autofocus = autofocus;
    }

    public bool? Value { get; }

    public Action<bool?>? OnChanged { get; }

    public bool Tristate { get; }

    public Color? ActiveColor { get; }

    public MaterialStateProperty<Color?>? FillColor { get; }

    public Color? CheckColor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public BorderRadius? Shape { get; }

    public BorderSide? Side { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var enabled = OnChanged is not null;
        var isSelected = Value ?? true;
        var baseStates = BuildStates(enabled, isSelected);
        var effectiveShape = Shape ?? Flutter.Rendering.BorderRadius.Circular(theme.UseMaterial3 ? 2 : 1);
        var effectiveTapTargetSize = MaterialTapTargetSize ?? theme.MaterialTapTargetSize;
        var resolvedCheckColor = ResolveCheckColor(theme, baseStates);

        var style = new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveCheckColor(theme, states)),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveFillColor(theme, states)),
            ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: ResolveOverlayColor(theme),
            SplashColor: null,
            Elevation: MaterialStateProperty<double?>.All(0),
            IconColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveCheckColor(theme, states)),
            IconSize: MaterialStateProperty<double?>.All(14),
            Side: MaterialStateProperty<BorderSide?>.ResolveWith(states => ResolveSide(theme, states)),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(0)),
            Shape: MaterialStateProperty<BorderRadius?>.All(effectiveShape),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
            FixedSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
            MaximumSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
            Alignment: Alignment.Center,
            TapTargetSize: effectiveTapTargetSize);

        Widget indicator = Value switch
        {
            true => new Icon(Icons.Check, size: 14, color: resolvedCheckColor),
            null => new Container(width: 10, height: 2, color: resolvedCheckColor),
            _ => new SizedBox()
        };

        return new MaterialButtonCore(
            child: new SizedBox(
                width: Width,
                height: Width,
                child: new Center(child: indicator)),
            onPressed: enabled ? HandleTap : null,
            style: style,
            focusNode: FocusNode,
            isSelected: isSelected,
            autofocus: Autofocus);
    }

    private void HandleTap()
    {
        OnChanged?.Invoke(NextValue());
    }

    private bool? NextValue()
    {
        if (!Tristate)
        {
            return !(Value ?? false);
        }

        return Value switch
        {
            false => true,
            true => null,
            _ => false
        };
    }

    private MaterialStateProperty<Color?> ResolveOverlayColor(ThemeData theme)
    {
        if (OverlayColor is not null)
        {
            return OverlayColor;
        }

        var overlayBaseColor = ActiveColor ?? theme.PrimaryColor;
        var pressedFocusedOpacity = theme.UseMaterial3 ? 0.10 : 0.12;
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                return MaterialButtonCore.ApplyOpacity(overlayBaseColor, pressedFocusedOpacity);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return HoverColor ?? MaterialButtonCore.ApplyOpacity(overlayBaseColor, 0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return FocusColor ?? MaterialButtonCore.ApplyOpacity(overlayBaseColor, pressedFocusedOpacity);
            }

            return null;
        });
    }

    private Color ResolveFillColor(ThemeData theme, MaterialState states)
    {
        var resolved = FillColor?.Resolve(states);
        if (resolved.HasValue)
        {
            return resolved.Value;
        }

        if (states.HasFlag(MaterialState.Disabled))
        {
            return states.HasFlag(MaterialState.Selected)
                ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                : Colors.Transparent;
        }

        if (states.HasFlag(MaterialState.Selected))
        {
            return ActiveColor ?? theme.PrimaryColor;
        }

        return Colors.Transparent;
    }

    private Color ResolveCheckColor(ThemeData theme, MaterialState states)
    {
        if (states.HasFlag(MaterialState.Disabled))
        {
            return states.HasFlag(MaterialState.Selected)
                ? (theme.UseMaterial3 ? theme.CanvasColor : Colors.White)
                : Colors.Transparent;
        }

        if (states.HasFlag(MaterialState.Selected))
        {
            return CheckColor ?? theme.OnPrimaryColor;
        }

        return Colors.Transparent;
    }

    private BorderSide? ResolveSide(ThemeData theme, MaterialState states)
    {
        if (Side.HasValue)
        {
            return states.HasFlag(MaterialState.Selected) ? null : Side.Value;
        }

        if (states.HasFlag(MaterialState.Disabled))
        {
            if (states.HasFlag(MaterialState.Selected))
            {
                return new BorderSide(Colors.Transparent, theme.UseMaterial3 ? 0 : 2);
            }

            return new BorderSide(
                MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38),
                2);
        }

        if (states.HasFlag(MaterialState.Selected))
        {
            return new BorderSide(Colors.Transparent, theme.UseMaterial3 ? 0 : 2);
        }

        if (theme.UseMaterial3)
        {
            if (states.HasFlag(MaterialState.Pressed)
                || states.HasFlag(MaterialState.Hovered)
                || states.HasFlag(MaterialState.Focused))
            {
                return new BorderSide(theme.OnSurfaceColor, 2);
            }

            return new BorderSide(theme.OnSurfaceVariantColor, 2);
        }

        return new BorderSide(
            MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.60),
            2);
    }

    private static MaterialState BuildStates(bool enabled, bool selected)
    {
        if (!enabled)
        {
            return selected
                ? MaterialState.Disabled | MaterialState.Selected
                : MaterialState.Disabled;
        }

        return selected ? MaterialState.Selected : MaterialState.None;
    }
}
