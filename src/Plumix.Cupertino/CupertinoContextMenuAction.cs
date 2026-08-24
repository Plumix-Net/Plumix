using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/context_menu_action.dart

/// <summary>An iOS context-menu action row.</summary>
public sealed class CupertinoContextMenuAction : StatefulWidget
{
    public CupertinoContextMenuAction(
        Widget child,
        bool isDefaultAction = false,
        bool isDestructiveAction = false,
        Action? onPressed = null,
        IconData? trailingIcon = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        IsDefaultAction = isDefaultAction;
        IsDestructiveAction = isDestructiveAction;
        OnPressed = onPressed;
        TrailingIcon = trailingIcon;
    }

    public Widget Child { get; }

    public bool IsDefaultAction { get; }

    public bool IsDestructiveAction { get; }

    public Action? OnPressed { get; }

    public IconData? TrailingIcon { get; }

    public override State CreateState() => new CupertinoContextMenuActionState();

    private sealed class CupertinoContextMenuActionState : State
    {
        private static readonly CupertinoDynamicColor BackgroundColorPressed =
            CupertinoDynamicColor.WithBrightness(
                Color.FromUInt32(0xFFDDDDDD),
                Color.FromUInt32(0xFF3F3F40));

        private static readonly TextStyle ActionSheetActionStyle = new(
            FontFamily: new FontFamily("CupertinoSystemText"),
            FontSize: 16.0,
            FontWeight: FontWeight.Normal,
            Color: CupertinoColors.Black,
            Inherit: false,
            TextBaseline: TextBaseline.Alphabetic);

        private readonly GlobalKey _globalKey = new GlobalObjectKey<State>(new object());
        private bool _isPressed;

        private CupertinoContextMenuAction Current => (CupertinoContextMenuAction)StateWidget;

        public override Widget Build(BuildContext context)
        {
            TextStyle textStyle = ResolveTextStyle(context);
            var children = new List<Widget>
            {
                new Flexible(Current.Child),
            };
            if (Current.TrailingIcon is { } trailingIcon)
            {
                children.Add(new Icon(
                    trailingIcon,
                    color: textStyle.Color,
                    size: 21.0));
            }

            Widget content = new ConstrainedBox(
                constraints: new BoxConstraints(MinHeight: 43.0),
                child: new Semantics(
                    flags: SemanticsFlags.IsButton,
                    child: new ColoredBox(
                        color: CupertinoDynamicColor.Resolve(
                            _isPressed ? BackgroundColorPressed : CupertinoContextMenu.BackgroundColor,
                            context),
                        child: new Padding(
                            insets: new Thickness(15.5, 8.0, 17.5, 8.0),
                            child: new DefaultTextStyle(
                                style: textStyle,
                                child: new Row(
                                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                                    children: children))))));

            return new MouseRegion(
                cursor: Current.OnPressed is not null && PlatformDefaults.IsWeb
                    ? SystemMouseCursors.Click
                    : MouseCursor.Defer,
                child: new GestureDetector(
                    key: _globalKey,
                    behavior: HitTestBehavior.Opaque,
                    onTapDown: _ => SetPressed(true),
                    onTapUp: _ => SetPressed(false),
                    onTapCancel: () => SetPressed(false),
                    onTap: Current.OnPressed,
                    child: content));
        }

        private TextStyle ResolveTextStyle(BuildContext context)
        {
            if (Current.IsDefaultAction)
            {
                return ActionSheetActionStyle.CopyWith(
                    color: CupertinoDynamicColor.Resolve(CupertinoColors.Label, context),
                    fontWeight: FontWeight.SemiBold);
            }

            if (Current.IsDestructiveAction)
            {
                return ActionSheetActionStyle.CopyWith(color: CupertinoColors.DestructiveRed.Color);
            }

            return ActionSheetActionStyle.CopyWith(
                color: CupertinoDynamicColor.Resolve(CupertinoColors.Label, context));
        }

        private void SetPressed(bool value)
        {
            if (_isPressed == value)
            {
                return;
            }

            SetState(() => _isPressed = value);
        }
    }
}
