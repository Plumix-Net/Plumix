// C#-only infrastructure: no Dart parity source.
//
// `Plumix.Material.Colors` (the Material palette, ported from material_ui/lib/src/colors.dart) and
// `Avalonia.Media.Colors` (the CSS named colors) collide in every file that imports both. Demo
// pages use the CSS names for illustrative swatches, so `Colors` stays bound to the CSS set
// here; reach the Material palette through the `MaterialColors` alias instead.
global using Color = Plumix.UI.Color;
global using Colors = Plumix.Sample.CssColors;
global using MaterialColors = Plumix.Material.Colors;
