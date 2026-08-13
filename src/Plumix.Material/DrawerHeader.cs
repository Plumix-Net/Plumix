using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/drawer_header.dart
// material_ui/lib/src/user_accounts_drawer_header.dart

public sealed class DrawerHeader : StatelessWidget
{
    private const double DrawerHeaderHeight = 161.0;

    public DrawerHeader(
        Widget? child,
        Decoration? decoration = null,
        EdgeInsetsGeometry? margin = null,
        EdgeInsetsGeometry? padding = null,
        TimeSpan? duration = null,
        Curve? curve = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Decoration = decoration;
        Margin = margin ?? EdgeInsetsGeometry.Only(bottom: 8.0);
        Padding = padding ?? EdgeInsetsGeometry.FromLTRB(16.0, 16.0, 16.0, 8.0);
        Duration = duration ?? TimeSpan.FromMilliseconds(250);
        Curve = curve ?? Curves.FastOutSlowIn;

        if (Duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }

    public Widget? Child { get; }
    public Decoration? Decoration { get; }
    public EdgeInsetsGeometry? Margin { get; }
    public EdgeInsetsGeometry Padding { get; }
    public TimeSpan Duration { get; }
    public Curve Curve { get; }

    public override Widget Build(BuildContext context)
    {
        double statusBarHeight = MediaQuery.PaddingOf(context).Top;
        TextDirection direction = Directionality.Of(context);
        var divider = Divider.CreateBorderSide(context);
        Thickness resolvedPadding = Padding.Resolve(direction);
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
                resolvedPadding.Left,
                resolvedPadding.Top + statusBarHeight,
                resolvedPadding.Right,
                resolvedPadding.Bottom),
            decoration: Decoration,
            child: child);

        return new Container(
            height: statusBarHeight + DrawerHeaderHeight,
            margin: Margin?.Resolve(direction),
            decoration: new BoxDecoration(
                Border: new Plumix.Rendering.Border(bottom: divider)),
            child: animated);
    }
}

public sealed class UserAccountsDrawerHeader : StatefulWidget
{
    private const string AccountNameId = "accountName";
    private const string AccountEmailId = "accountEmail";
    private const string DropdownIconId = "dropdownIcon";

    public UserAccountsDrawerHeader(
        Widget? accountName,
        Widget? accountEmail,
        Decoration? decoration = null,
        EdgeInsetsGeometry? margin = null,
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
        Margin = margin ?? EdgeInsetsGeometry.Only(bottom: 8.0);
        CurrentAccountPicture = currentAccountPicture;
        OtherAccountsPictures = otherAccountsPictures;
        CurrentAccountPictureSize = currentAccountPictureSize ?? new Size(72, 72);
        OtherAccountsPicturesSize = otherAccountsPicturesSize ?? new Size(40, 40);
        OnDetailsPressed = onDetailsPressed;
        ArrowColor = arrowColor ?? Colors.White;
        ValidateSize(CurrentAccountPictureSize, nameof(currentAccountPictureSize));
        ValidateSize(OtherAccountsPicturesSize, nameof(otherAccountsPicturesSize));
    }

    public Decoration? Decoration { get; }
    public EdgeInsetsGeometry? Margin { get; }
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
            Decoration decoration = widget.Decoration ?? new BoxDecoration(Color: theme.ColorScheme.Primary);

            return new Semantics(
                container: true,
                label: localizations.SignedInLabel,
                child: new DrawerHeader(
                    decoration: decoration,
                    margin: widget.Margin,
                    padding: EdgeInsetsGeometry.DirectionalOnly(start: 16.0, top: 16.0),
                    child: new SafeArea(
                        bottom: false,
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                new Expanded(
                                    child: new Padding(
                                        insets: EdgeInsetsGeometry.DirectionalOnly(end: 16.0),
                                        child: BuildAccountPictures(widget))),
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

        private static Widget BuildAccountPictures(UserAccountsDrawerHeader widget)
        {
            var children = new List<Widget>();
            var otherPictures = widget.OtherAccountsPictures?.Take(3).ToList() ?? [];
            var rowChildren = otherPictures
                .Select(picture => (Widget)new Padding(
                    insets: EdgeInsetsGeometry.DirectionalOnly(start: 8.0),
                    child: new Semantics(
                        container: true,
                        child: new Padding(
                            insets: EdgeInsetsGeometry.Only(left: 8.0, bottom: 8.0),
                            child: new SizedBox(
                                width: widget.OtherAccountsPicturesSize.Width,
                                height: widget.OtherAccountsPicturesSize.Height,
                                child: picture)))))
                .ToList();
            children.Add(new PositionedDirectional(
                top: 0.0,
                end: 0.0,
                child: new Row(children: rowChildren)));
            children.Add(new Positioned(
                top: 0.0,
                child: new Semantics(
                    explicitChildNodes: true,
                    child: new SizedBox(
                        width: widget.CurrentAccountPictureSize.Width,
                        height: widget.CurrentAccountPictureSize.Height,
                        child: widget.CurrentAccountPicture))));

            return new Stack(
                alignment: AlignmentDirectional.TopStart,
                children: children);
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
        private AnimationController? _controller;
        private CurvedAnimation? _animation;

        private AccountDetails CurrentWidget => (AccountDetails)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(TimeSpan.FromMilliseconds(200), this);
            _controller.SetValue(CurrentWidget.IsOpen ? 1.0 : 0.0);
            _animation = new CurvedAnimation(
                parent: _controller,
                curve: Curves.FastOutSlowIn,
                reverseCurve: Curves.Flipped(Curves.FastOutSlowIn));
            _animation.AddListener(HandleAnimationChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldDetails = (AccountDetails)oldWidget;
            if (oldDetails.IsOpen == CurrentWidget.IsOpen)
            {
                return;
            }

            if (CurrentWidget.IsOpen)
            {
                _controller!.Forward();
            }
            else
            {
                _controller!.Reverse();
            }
        }

        public override void Dispose()
        {
            _animation!.RemoveListener(HandleAnimationChanged);
            _animation.Dispose();
            _controller!.Dispose();
            _animation = null;
            _controller = null;
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var direction = Directionality.Of(context);
            var children = new List<Widget>();
            if (widget.AccountName is not null)
            {
                children.Add(new LayoutId(
                    id: AccountNameId,
                    child: new Padding(
                        insets: EdgeInsetsGeometry.Symmetric(vertical: 2.0),
                        child: new DefaultTextStyle(
                            style: theme.PrimaryTextTheme.BodyLarge,
                            overflow: TextOverflow.Ellipsis,
                            child: widget.AccountName))));
            }
            if (widget.AccountEmail is not null)
            {
                children.Add(new LayoutId(
                    id: AccountEmailId,
                    child: new Padding(
                        insets: EdgeInsetsGeometry.Symmetric(vertical: 2.0),
                        child: new DefaultTextStyle(
                            style: theme.PrimaryTextTheme.BodyMedium,
                            overflow: TextOverflow.Ellipsis,
                            child: widget.AccountEmail))));
            }

            if (widget.OnTap is not null)
            {
                string semanticLabel = widget.IsOpen
                    ? MaterialLocalizations.Of(context).HideAccountsLabel
                    : MaterialLocalizations.Of(context).ShowAccountsLabel;
                children.Add(new LayoutId(
                    id: DropdownIconId,
                    child: new Semantics(
                        container: true,
                        flags: SemanticsFlags.IsButton,
                        onTap: widget.OnTap,
                        child: new SizedBox(
                            width: 56.0,
                            height: 56.0,
                            child: new Center(
                                child: new Plumix.Widgets.Transform(
                                    transform: Matrix.CreateRotation(_animation!.Value * Math.PI),
                                    alignment: Alignment.Center,
                                    child: new Icon(
                                        Icons.ArrowDropDown,
                                        color: widget.ArrowColor,
                                        semanticLabel: semanticLabel)))))));
            }

            Widget result = new SizedBox(
                height: 56.0,
                child: new CustomMultiChildLayout(
                    @delegate: new AccountDetailsLayout(direction),
                    children: children));
            if (widget.OnTap is not null)
            {
                result = new InkWell(
                    onTap: widget.OnTap,
                    excludeFromSemantics: true,
                    child: result);
            }
            return result;
        }

        private void HandleAnimationChanged() => SetState(() => { });
    }

    private sealed class AccountDetailsLayout(TextDirection textDirection) : MultiChildLayoutDelegate
    {
        public override void PerformLayout(Size size)
        {
            Size? iconSize = null;
            if (HasChild(DropdownIconId))
            {
                iconSize = LayoutChild(DropdownIconId, BoxConstraints.Loose(size));
                PositionChild(
                    DropdownIconId,
                    OffsetForIcon(size, iconSize.Value));
            }

            string? bottomLine = HasChild(AccountEmailId)
                ? AccountEmailId
                : HasChild(AccountNameId)
                    ? AccountNameId
                    : null;
            if (bottomLine is null)
            {
                return;
            }

            Size constraintSize = iconSize is null
                ? size
                : new Size(size.Width - iconSize.Value.Width, size.Height);
            iconSize ??= new Size(56.0, 56.0);
            Size bottomLineSize = LayoutChild(bottomLine, BoxConstraints.Loose(constraintSize));
            Point bottomLineOffset = OffsetForBottomLine(size, iconSize.Value, bottomLineSize);
            PositionChild(bottomLine, bottomLineOffset);

            if (bottomLine == AccountEmailId
                && HasChild(AccountNameId))
            {
                Size nameSize = LayoutChild(
                    AccountNameId,
                    BoxConstraints.Loose(constraintSize));
                PositionChild(
                    AccountNameId,
                    OffsetForName(size, nameSize, bottomLineOffset));
            }
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => true;

        private Point OffsetForIcon(Size size, Size iconSize)
        {
            return textDirection == TextDirection.Ltr
                ? new Point(size.Width - iconSize.Width, size.Height - iconSize.Height)
                : new Point(0.0, size.Height - iconSize.Height);
        }

        private Point OffsetForBottomLine(Size size, Size iconSize, Size bottomLineSize)
        {
            double y = size.Height - (0.5 * iconSize.Height) - (0.5 * bottomLineSize.Height);
            return textDirection == TextDirection.Ltr
                ? new Point(0.0, y)
                : new Point(size.Width - bottomLineSize.Width, y);
        }

        private Point OffsetForName(Size size, Size nameSize, Point bottomLineOffset)
        {
            double y = bottomLineOffset.Y - nameSize.Height;
            return textDirection == TextDirection.Ltr
                ? new Point(0.0, y)
                : new Point(size.Width - nameSize.Width, y);
        }
    }
}
