// C#-only infrastructure: no Dart parity source.
//
// `Plumix.Material.Colors` (the Material palette, ported from material_ui/lib/src/colors.dart) and
// `Avalonia.Media.Colors` (the CSS named colors) collide in every file that imports both. The test
// suite uses the CSS names as arbitrary probe colors, so `Colors` stays bound to Avalonia's set
// here; reach the Material palette through the `MaterialColors` alias instead.
global using Colors = Avalonia.Media.Colors;
global using MaterialColors = Plumix.Material.Colors;
