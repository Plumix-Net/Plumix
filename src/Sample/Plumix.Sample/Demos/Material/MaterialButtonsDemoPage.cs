using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/material_buttons_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class MaterialButtonsDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new MaterialButtonsDemoPageState();
    }
}

internal sealed class MaterialButtonsDemoPageState : State
{
    private bool _enabled = true;
    private int _textButtonTaps;
    private int _elevatedButtonTaps;
    private int _outlinedButtonTaps;
    private int _filledButtonTaps;
    private int _filledTonalButtonTaps;
    private int _iconButtonTaps;
    private int _filledIconButtonTaps;
    private int _filledTonalIconButtonTaps;
    private int _outlinedIconButtonTaps;
    private int _materialButtonTaps;
    private int _rawMaterialButtonTaps;
    private bool _iconButtonSelected;
    private bool _useMaterial3 = true;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Material buttons baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "TextButton / ElevatedButton / OutlinedButton / FilledButton (+ tonal) / IconButton with enabled/disabled and theme-aware defaults.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _enabled ? "Enabled" : "Disabled",
                            onTap: ToggleEnabled,
                            width: 108,
                            background: Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            label: "Reset",
                            onTap: ResetCounters,
                            width: 88,
                            background: Color.Parse("#FFF3E8D8")),
                        BuildControlButton(
                            label: _useMaterial3 ? "Icons M3" : "Icons M2",
                            onTap: ToggleIconMaterialVersion,
                            width: 96,
                            background: Color.Parse("#FFE8F5E9")),
                    ]),
                new Text(
                    $"enabled={(_enabled ? "true" : "false")}, iconsM3={(_useMaterial3 ? "true" : "false")}, "
                    + $"text={_textButtonTaps}, elevated={_elevatedButtonTaps}, outlined={_outlinedButtonTaps}, "
                    + $"filled={_filledButtonTaps}, tonal={_filledTonalButtonTaps}, "
                    + $"material={_materialButtonTaps}, raw={_rawMaterialButtonTaps}, icon={_iconButtonTaps}, "
                    + $"filledIcon={_filledIconButtonTaps}, tonalIcon={_filledTonalIconButtonTaps}, "
                    + $"outlinedIcon={_outlinedIconButtonTaps}, "
                    + $"iconSelected={(_iconButtonSelected ? "true" : "false")}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new SizedBox(
                    width: 240,
                    child: new TextButton(
                        onPressed: _enabled ? OnTextButtonTap : null,
                        child: new Text($"TextButton taps: {_textButtonTaps}"))),
                BuildTextButtonSchemeProbe(context),
                new SizedBox(
                    width: 240,
                    child: new ElevatedButton(
                        onPressed: _enabled ? OnElevatedButtonTap : null,
                        child: new Text($"ElevatedButton taps: {_elevatedButtonTaps}"))),
                BuildElevatedButtonSchemeProbe(context),
                new SizedBox(
                    width: 240,
                    child: new OutlinedButton(
                        onPressed: _enabled ? OnOutlinedButtonTap : null,
                        child: new Text($"OutlinedButton taps: {_outlinedButtonTaps}"))),
                new SizedBox(
                    width: 240,
                    child: new FilledButton(
                        onPressed: _enabled ? OnFilledButtonTap : null,
                        child: new Text($"FilledButton taps: {_filledButtonTaps}"))),
                new SizedBox(
                    width: 240,
                    child: FilledButton.Tonal(
                        onPressed: _enabled ? OnFilledTonalButtonTap : null,
                        child: new Text($"FilledButton.tonal taps: {_filledTonalButtonTaps}"))),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new MaterialButton(
                                onPressed: _enabled ? OnMaterialButtonTap : null,
                                color: Color.Parse("#FFE0E0E0"),
                                child: new Text($"Material: {_materialButtonTaps}"))),
                        new Expanded(
                            child: new RawMaterialButton(
                                onPressed: _enabled ? OnRawMaterialButtonTap : null,
                                fillColor: Color.Parse("#FFDDEBF7"),
                                hoverColor: Color.Parse("#1F005E7A"),
                                highlightColor: Color.Parse("#33005E7A"),
                                splashColor: Color.Parse("#33005E7A"),
                                shape: BorderRadius.Circular(6),
                                child: new Text($"Raw: {_rawMaterialButtonTaps}"))),
                    ]),
                BuildIconButtonProbe(context),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new ElevatedButton(
                                onPressed: _enabled ? OnElevatedButtonTap : null,
                                backgroundColor: Color.Parse("#FF6A994E"),
                                foregroundColor: Colors.White,
                                child: new Text("Custom elevated"))),
                        new Expanded(
                            child: new OutlinedButton(
                                onPressed: _enabled ? OnOutlinedButtonTap : null,
                                borderColor: Color.Parse("#FF7B2CBF"),
                                foregroundColor: Color.Parse("#FF7B2CBF"),
                                child: new Text("Custom outlined"))),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new FilledButton(
                                onPressed: _enabled ? OnFilledButtonTap : null,
                                foregroundColor: Colors.White,
                                backgroundColor: Color.Parse("#FF005E7A"),
                                child: new Text("Custom filled"))),
                        new Expanded(
                            child: FilledButton.Tonal(
                                onPressed: _enabled ? OnFilledTonalButtonTap : null,
                                foregroundColor: Color.Parse("#FF42275A"),
                                backgroundColor: Color.Parse("#FFD8CFF8"),
                                child: new Text("Custom tonal"))),
                    ]),
            ]);
    }

    private Widget BuildTextButtonSchemeProbe(BuildContext context)
    {
        ThemeData inherited = Theme.Of(context);
        ColorScheme scheme = inherited.ColorScheme.CopyWith(
            primary: Color.Parse("#FF006A6A"),
            onSurface: Color.Parse("#FF4D2A6A"));
        ThemeData probeTheme = inherited with
        {
            PrimaryColor = Colors.OrangeRed,
            ColorScheme = scheme
        };

        return new Theme(
            data: probeTheme,
            child: new Row(
                spacing: 8,
                children:
                [
                    new Expanded(
                        child: new TextButton(
                            onPressed: _enabled ? OnTextButtonTap : null,
                            child: new Text("Scheme primary"))),
                    new Expanded(
                        child: new TextButton(
                            onPressed: null,
                            child: new Text("Scheme disabled"))),
                ]));
    }

    private Widget BuildElevatedButtonSchemeProbe(BuildContext context)
    {
        ThemeData inherited = Theme.Of(context);
        ColorScheme scheme = inherited.ColorScheme.CopyWith(
            primary: Color.Parse("#FF425F2D"),
            onPrimary: Color.Parse("#FFFFFFFF"),
            surfaceContainerLow: Color.Parse("#FFE8F2DD"),
            onSurface: Color.Parse("#FF392E21"),
            shadow: Color.Parse("#FF2F3B26"));
        ThemeData probeTheme = inherited with
        {
            PrimaryColor = Colors.OrangeRed,
            SurfaceContainerLowColor = Colors.Bisque,
            ColorScheme = scheme
        };

        return new Theme(
            data: probeTheme,
            child: new Row(
                spacing: 8,
                children:
                [
                    new Expanded(
                        child: new ElevatedButton(
                            onPressed: _enabled ? OnElevatedButtonTap : null,
                            child: new Text("Scheme elevated"))),
                    new Expanded(
                        child: new ElevatedButton(
                            onPressed: null,
                            child: new Text("Scheme elevated off"))),
                ]));
    }

    private Widget BuildControlButton(
        string label,
        Action onTap,
        double width,
        Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onTap,
                backgroundColor: background,
                foregroundColor: Colors.Black,
                minHeight: 36,
                padding: new Thickness(10, 8),
                borderRadius: BorderRadius.Circular(8),
                child: new Text(
                    label,
                    fontSize: 12)));
    }

    private Widget BuildIconButtonProbe(BuildContext context)
    {
        Widget tonalButton = new IconButtonTheme(
            data: new IconButtonThemeData(
                style: IconButton.StyleFrom(
                    foregroundColor: Color.Parse("#FF6A1B9A"))),
            child: new SizedBox(
                width: 56,
                height: 56,
                child: IconButton.FilledTonal(
                    icon: new Icon(Icons.Star),
                    visualDensity: VisualDensity.Compact,
                    tooltip: "Compact tonal favorite",
                    onPressed: _enabled ? OnFilledTonalIconButtonTap : null)));

        return new Theme(
            data: Theme.Of(context) with { UseMaterial3 = _useMaterial3 },
            child: new Row(
                spacing: 8,
                children:
                [
                    new SizedBox(
                        width: 56,
                        height: 56,
                        child: new IconButton(
                            icon: new Icon(Icons.StarOutline),
                            selectedIcon: new Icon(Icons.Star),
                            isSelected: _iconButtonSelected,
                            tooltip: "Toggle favorite",
                            onPressed: _enabled ? OnIconButtonTap : null)),
                    new SizedBox(
                        width: 56,
                        height: 56,
                        child: IconButton.Filled(
                            icon: new Icon(Icons.Add),
                            tooltip: "Add",
                            onPressed: _enabled ? OnFilledIconButtonTap : null)),
                    tonalButton,
                    new SizedBox(
                        width: 56,
                        height: 56,
                        child: IconButton.Outlined(
                            icon: new Icon(Icons.InfoOutline),
                            tooltip: "Info",
                            onPressed: _enabled ? OnOutlinedIconButtonTap : null)),
                ]));
    }

    private void ToggleEnabled()
    {
        SetState(() => _enabled = !_enabled);
    }

    private void ToggleIconMaterialVersion()
    {
        SetState(() => _useMaterial3 = !_useMaterial3);
    }

    private void ResetCounters()
    {
        SetState(() =>
        {
            _textButtonTaps = 0;
            _elevatedButtonTaps = 0;
            _outlinedButtonTaps = 0;
            _filledButtonTaps = 0;
            _filledTonalButtonTaps = 0;
            _iconButtonTaps = 0;
            _filledIconButtonTaps = 0;
            _filledTonalIconButtonTaps = 0;
            _outlinedIconButtonTaps = 0;
            _materialButtonTaps = 0;
            _rawMaterialButtonTaps = 0;
            _iconButtonSelected = false;
            _enabled = true;
            _useMaterial3 = true;
        });
    }

    private void OnTextButtonTap()
    {
        SetState(() => _textButtonTaps += 1);
    }

    private void OnElevatedButtonTap()
    {
        SetState(() => _elevatedButtonTaps += 1);
    }

    private void OnOutlinedButtonTap()
    {
        SetState(() => _outlinedButtonTaps += 1);
    }

    private void OnFilledButtonTap()
    {
        SetState(() => _filledButtonTaps += 1);
    }

    private void OnFilledTonalButtonTap()
    {
        SetState(() => _filledTonalButtonTaps += 1);
    }

    private void OnMaterialButtonTap()
    {
        SetState(() => _materialButtonTaps += 1);
    }

    private void OnRawMaterialButtonTap()
    {
        SetState(() => _rawMaterialButtonTaps += 1);
    }

    private void OnIconButtonTap()
    {
        SetState(() =>
        {
            _iconButtonTaps += 1;
            _iconButtonSelected = !_iconButtonSelected;
        });
    }

    private void OnFilledIconButtonTap()
    {
        SetState(() => _filledIconButtonTaps += 1);
    }

    private void OnFilledTonalIconButtonTap()
    {
        SetState(() => _filledTonalIconButtonTaps += 1);
    }

    private void OnOutlinedIconButtonTap()
    {
        SetState(() => _outlinedIconButtonTaps += 1);
    }
}
