using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// C#-only sample infrastructure: the toggle button and status formatting shared by the
// CheckboxListTile / RadioListTile / SwitchListTile demo pages (the Dart sample repeats them
// per page, since Dart pages have no shared helper file).
internal static class ListTileControlDemos
{
    public static Widget ControlButton(string label, Action onPressed, double width, Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onPressed,
                child: new Text(label, fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }

    public static string Lower(bool value)
    {
        return value.ToString().ToLowerInvariant();
    }

    public static string FormatNullable(bool? value)
    {
        return value.HasValue ? Lower(value.Value) : "null";
    }
}
