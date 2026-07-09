using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/drawer_header.dart
// flutter/packages/flutter/lib/src/material/user_accounts_drawer_header.dart

public sealed class DrawerHeader : StatelessWidget
{
    private const double DrawerHeaderHeight = 161.0;

    public DrawerHeader(
        Widget? child,
        BoxDecoration? decoration = null,
        Thickness? margin = null,
        Thickness? padding = null,
        TimeSpan? duration = null,
        Curve? curve = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Decoration = decoration;
        Margin = margin ?? new Thickness(0, 0, 0, 8);
        Padding = padding ?? new Thickness(16, 16, 16, 8);
        Duration = duration ?? TimeSpan.FromMilliseconds(250);
        Curve = curve ?? Curves.FastOutSlowIn;

        if (Duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }

    public Widget? Child { get; }
    public BoxDecoration? Decoration { get; }
    public Thickness? Margin { get; }
    public Thickness Padding { get; }
    public TimeSpan Duration { get; }
    public Curve Curve { get; }

    public override Widget Build(BuildContext context)
    {
        double statusBarHeight = MediaQuery.PaddingOf(context).Top;
        var divider = Divider.CreateBorderSide(context);
        Widget? child = Child;
        if (child is not null)
        {
            child = new DefaultTextStyle(
                style: Theme.Of(context).TextTheme.BodyLarge,
                child: MediaQuery.RemovePadding(context, child, removeTop: true));
        }

        var animated = new AnimatedContainer(
            duration: Duration,
            curve: Curve,
            padding: new Thickness(
                Padding.Left,
                Padding.Top + statusBarHeight,
                Padding.Right,
                Padding.Bottom),
            decoration: Decoration,
            child: child);

        return new Container(
            height: statusBarHeight + DrawerHeaderHeight,
            margin: Margin,
            child: new Column(
                children:
                [
                    new Expanded(child: animated),
                    new Divider(height: 1, thickness: divider.Width, color: divider.Color),
                ]));
    }
}

public sealed class UserAccountsDrawerHeader : StatefulWidget
{
    public UserAccountsDrawerHeader(
        Widget? accountName,
        Widget? accountEmail,
        BoxDecoration? decoration = null,
        Thickness? margin = null,
        Widget? currentAccountPicture = null,
        IReadOnlyList<Widget>? otherAccountsPictures = null,
        Size? currentAccountPictureSize = null,
        Size? otherAccountsPicturesSize = null,
        Action? onDetailsPressed = null,
        Color? arrowColor = null,
        Key? key = null) : base(key)
    {
        AccountName = accountName;
        AccountEmail = accountEmail;
        Decoration = decoration;
        Margin = margin ?? new Thickness(0, 0, 0, 8);
        CurrentAccountPicture = currentAccountPicture;
        OtherAccountsPictures = otherAccountsPictures;
        CurrentAccountPictureSize = currentAccountPictureSize ?? new Size(72, 72);
        OtherAccountsPicturesSize = otherAccountsPicturesSize ?? new Size(40, 40);
        OnDetailsPressed = onDetailsPressed;
        ArrowColor = arrowColor ?? Colors.White;
        ValidateSize(CurrentAccountPictureSize, nameof(currentAccountPictureSize));
        ValidateSize(OtherAccountsPicturesSize, nameof(otherAccountsPicturesSize));
    }

    public BoxDecoration? Decoration { get; }
    public Thickness? Margin { get; }
    public Widget? CurrentAccountPicture { get; }
    public IReadOnlyList<Widget>? OtherAccountsPictures { get; }
    public Size CurrentAccountPictureSize { get; }
    public Size OtherAccountsPicturesSize { get; }
    public Widget? AccountName { get; }
    public Widget? AccountEmail { get; }
    public Action? OnDetailsPressed { get; }
    public Color ArrowColor { get; }

    public override State CreateState() => new UserAccountsDrawerHeaderState();

    private static void ValidateSize(Size size, string parameterName)
    {
        if (!double.IsFinite(size.Width)
            || !double.IsFinite(size.Height)
            || size.Width < 0
            || size.Height < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Account picture sizes must be finite and non-negative.");
        }
    }

    private sealed class UserAccountsDrawerHeaderState : State
    {
        private bool _isOpen;
        private UserAccountsDrawerHeader CurrentWidget => (UserAccountsDrawerHeader)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var localizations = MaterialLocalizations.Of(context);
            var direction = Directionality.Of(context);
            var decoration = widget.Decoration ?? new BoxDecoration(Color: theme.PrimaryColor);

            return new Semantics(
                container: true,
                label: localizations.SignedInLabel,
                child: new DrawerHeader(
                    decoration: decoration,
                    margin: widget.Margin,
                    padding: ResolveDirectionalThickness(direction, start: 16, top: 16),
                    child: new SafeArea(
                        bottom: false,
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                new Expanded(
                                    child: new Padding(
                                        insets: ResolveDirectionalThickness(direction, end: 16),
                                        child: BuildAccountPictures(widget, direction))),
                                new AccountDetails(
                                    accountName: widget.AccountName,
                                    accountEmail: widget.AccountEmail,
                                    isOpen: _isOpen,
                                    onTap: widget.OnDetailsPressed is null ? null : HandleDetailsPressed,
                                    arrowColor: widget.ArrowColor),
                            ]))));
        }

        private void HandleDetailsPressed()
        {
            SetState(() => _isOpen = !_isOpen);
            CurrentWidget.OnDetailsPressed?.Invoke();
        }

        private static Widget BuildAccountPictures(UserAccountsDrawerHeader widget, TextDirection direction)
        {
            var children = new List<Widget>();
            if (widget.CurrentAccountPicture is not null)
            {
                children.Add(new Positioned(
                    top: 0,
                    left: direction == TextDirection.Ltr ? 0 : null,
                    right: direction == TextDirection.Rtl ? 0 : null,
                    child: new Semantics(
                        explicitChildNodes: true,
                        child: new SizedBox(
                            width: widget.CurrentAccountPictureSize.Width,
                            height: widget.CurrentAccountPictureSize.Height,
                            child: widget.CurrentAccountPicture))));
            }

            var otherPictures = widget.OtherAccountsPictures?.Take(3).ToList() ?? [];
            if (otherPictures.Count > 0)
            {
                var rowChildren = otherPictures
                    .Select(picture => (Widget)new Padding(
                        insets: ResolveDirectionalThickness(direction, start: 8, bottom: 8),
                        child: new Semantics(
                            container: true,
                            child: new SizedBox(
                                width: widget.OtherAccountsPicturesSize.Width,
                                height: widget.OtherAccountsPicturesSize.Height,
                                child: picture))))
                    .ToList();
                children.Add(new Positioned(
                    top: 0,
                    left: direction == TextDirection.Rtl ? 0 : null,
                    right: direction == TextDirection.Ltr ? 0 : null,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        textDirection: direction,
                        children: rowChildren)));
            }

            return new Stack(children: children);
        }
    }

    private sealed class AccountDetails : StatefulWidget
    {
        public AccountDetails(
            Widget? accountName,
            Widget? accountEmail,
            bool isOpen,
            Action? onTap,
            Color arrowColor)
        {
            AccountName = accountName;
            AccountEmail = accountEmail;
            IsOpen = isOpen;
            OnTap = onTap;
            ArrowColor = arrowColor;
        }

        public Widget? AccountName { get; }
        public Widget? AccountEmail { get; }
        public bool IsOpen { get; }
        public Action? OnTap { get; }
        public Color ArrowColor { get; }
        public override State CreateState() => new AccountDetailsState();
    }

    private sealed class AccountDetailsState : State
    {
        private AccountDetails CurrentWidget => (AccountDetails)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var direction = Directionality.Of(context);
            var textChildren = new List<Widget>();
            if (widget.AccountName is not null)
            {
                textChildren.Add(new DefaultTextStyle(
                    style: theme.PrimaryTextTheme.BodyLarge,
                    overflow: TextOverflow.Ellipsis,
                    child: widget.AccountName));
            }
            if (widget.AccountEmail is not null)
            {
                textChildren.Add(new DefaultTextStyle(
                    style: theme.PrimaryTextTheme.BodyMedium,
                    overflow: TextOverflow.Ellipsis,
                    child: widget.AccountEmail));
            }

            var children = new List<Widget>
            {
                new Expanded(
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        mainAxisAlignment: MainAxisAlignment.Center,
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        textDirection: direction,
                        spacing: 4,
                        children: textChildren)),
            };

            if (widget.OnTap is not null)
            {
                double angle = widget.IsOpen ? Math.PI : 0;
                var transform = RotationAroundCenter(angle, 12);
                string semanticLabel = widget.IsOpen
                    ? MaterialLocalizations.Of(context).HideAccountsLabel
                    : MaterialLocalizations.Of(context).ShowAccountsLabel;
                children.Add(new SizedBox(
                    width: 56,
                    height: 56,
                    child: new Center(
                        child: new AnimatedContainer(
                            duration: TimeSpan.FromMilliseconds(200),
                            curve: Curves.FastOutSlowIn,
                            transform: transform,
                            child: new Semantics(
                                label: semanticLabel,
                                child: new Icon(
                                    Icons.ArrowDropDown,
                                    color: widget.ArrowColor,
                                    semanticLabel: semanticLabel))))));
            }

            Widget result = new SizedBox(
                height: 56,
                child: new Row(textDirection: direction, children: children));
            if (widget.OnTap is not null)
            {
                result = new Semantics(
                    container: true,
                    flags: SemanticsFlags.IsButton | SemanticsFlags.IsEnabled,
                    onTap: widget.OnTap,
                    child: new GestureDetector(
                        behavior: HitTestBehavior.Opaque,
                        onTap: widget.OnTap,
                        child: result));
            }
            return result;
        }

        private static Matrix RotationAroundCenter(double angle, double center)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Matrix(
                cos,
                sin,
                -sin,
                cos,
                center - (center * cos) + (center * sin),
                center - (center * sin) - (center * cos));
        }
    }

    private static Thickness ResolveDirectionalThickness(
        TextDirection direction,
        double start = 0,
        double top = 0,
        double end = 0,
        double bottom = 0)
    {
        return direction == TextDirection.Ltr
            ? new Thickness(start, top, end, bottom)
            : new Thickness(end, top, start, bottom);
    }
}
