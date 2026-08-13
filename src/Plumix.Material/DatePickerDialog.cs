using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/date_picker.dart

public delegate Widget DatePickerTransitionBuilder(BuildContext context, Widget child);

public sealed class DatePickerDialog : StatefulWidget
{
    public DatePickerDialog(
        DateTime firstDate,
        DateTime lastDate,
        DateTime? initialDate = null,
        DateTime? currentDate = null,
        DatePickerEntryMode initialEntryMode = DatePickerEntryMode.Calendar,
        SelectableDayPredicate? selectableDayPredicate = null,
        string? cancelText = null,
        string? confirmText = null,
        string? helpText = null,
        DatePickerMode initialCalendarMode = DatePickerMode.Day,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? fieldHintText = null,
        string? fieldLabelText = null,
        string? restorationId = null,
        Action<DatePickerEntryMode>? onDatePickerModeChange = null,
        Widget? switchToInputEntryModeIcon = null,
        Widget? switchToCalendarEntryModeIcon = null,
        Thickness? insetPadding = null,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        InitialDate = initialDate.HasValue ? CalendarDelegate.DateOnly(initialDate.Value) : null;
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        CurrentDate = CalendarDelegate.DateOnly(currentDate ?? CalendarDelegate.Now());
        CalendarDatePicker.ValidateDates(InitialDate, FirstDate, LastDate, selectableDayPredicate);
        ValidateInsets(insetPadding, nameof(insetPadding));
        InitialEntryMode = initialEntryMode;
        SelectableDayPredicate = selectableDayPredicate;
        CancelText = cancelText;
        ConfirmText = confirmText;
        HelpText = helpText;
        InitialCalendarMode = initialCalendarMode;
        ErrorFormatText = errorFormatText;
        ErrorInvalidText = errorInvalidText;
        FieldHintText = fieldHintText;
        FieldLabelText = fieldLabelText;
        RestorationId = restorationId;
        OnDatePickerModeChange = onDatePickerModeChange;
        SwitchToInputEntryModeIcon = switchToInputEntryModeIcon;
        SwitchToCalendarEntryModeIcon = switchToCalendarEntryModeIcon;
        InsetPadding = insetPadding ?? new Thickness(16, 24);
    }

    public DateTime? InitialDate { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public DateTime CurrentDate { get; }
    public DatePickerEntryMode InitialEntryMode { get; }
    public SelectableDayPredicate? SelectableDayPredicate { get; }
    public string? CancelText { get; }
    public string? ConfirmText { get; }
    public string? HelpText { get; }
    public DatePickerMode InitialCalendarMode { get; }
    public string? ErrorFormatText { get; }
    public string? ErrorInvalidText { get; }
    public string? FieldHintText { get; }
    public string? FieldLabelText { get; }
    public string? RestorationId { get; }
    public Action<DatePickerEntryMode>? OnDatePickerModeChange { get; }
    public Widget? SwitchToInputEntryModeIcon { get; }
    public Widget? SwitchToCalendarEntryModeIcon { get; }
    public Thickness InsetPadding { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new DatePickerDialogState();

    private static void ValidateInsets(Thickness? value, string name)
    {
        if (!value.HasValue) return;
        var insets = value.Value;
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top)
            || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

internal sealed class DatePickerDialogState : State
{
    private const double InputFormPortraitHeight = 98;
    private const double InputFormLandscapeHeight = 108;
    private static readonly Size CalendarPortraitDialogSizeM2 = new(330, 518);
    private static readonly Size CalendarPortraitDialogSizeM3 = new(360, 568);
    private static readonly Size CalendarLandscapeDialogSize = new(496, 346);
    private static readonly Size InputPortraitDialogSizeM2 = new(330, 270);
    private static readonly Size InputPortraitDialogSizeM3 = new(328, 270);
    private static readonly Size InputLandscapeDialogSize = new(496, 160);

    private readonly LabeledGlobalKey<FormState> _formKey = new("date-picker-form");
    private readonly ValueKey<string> _calendarPickerKey = new("date-picker-calendar");
    private DateTime? _selectedDate;
    private DatePickerEntryMode _entryMode;
    private AutovalidateMode _autovalidateMode = AutovalidateMode.Disabled;

    private DatePickerDialog Current => (DatePickerDialog)StateWidget;

    public override void InitState()
    {
        _selectedDate = Current.InitialDate;
        _entryMode = Current.InitialEntryMode;
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        var localizations = MaterialLocalizations.Of(context);
        var datePickerTheme = DatePickerTheme.Of(context);
        var defaults = DatePickerTheme.Defaults(context);
        var dialogTheme = DialogTheme.Of(context);
        var media = MediaQuery.MaybeOf(context) ?? new MediaQueryData(Size: new Size(360, 640));
        bool landscape = media.Size.Width > media.Size.Height;
        bool calendarMode = _entryMode is DatePickerEntryMode.Calendar or DatePickerEntryMode.CalendarOnly;
        var baseSize = ResolveDialogSize(useMaterial3, calendarMode, landscape);
        double scale = Math.Clamp(media.TextScaleFactor, 0, 3);
        var dialogSize = new Size(baseSize.Width * scale, baseSize.Height * scale);
        var headerForeground = datePickerTheme.HeaderForegroundColor ?? defaults.HeaderForegroundColor;
        var headlineStyle = ResolveHeadlineStyle(theme, datePickerTheme, defaults, landscape)
            .CopyWith(color: headerForeground);

        Widget picker;
        Widget? entryModeButton;
        switch (_entryMode)
        {
            case DatePickerEntryMode.Calendar:
                picker = BuildCalendarPicker();
                entryModeButton = BuildEntryModeButton(
                    Current.SwitchToInputEntryModeIcon
                    ?? new Icon(useMaterial3 ? Icons.EditOutlined : Icons.Edit),
                    localizations.InputDateModeButtonLabel,
                    headerForeground);
                break;
            case DatePickerEntryMode.CalendarOnly:
                picker = BuildCalendarPicker();
                entryModeButton = null;
                break;
            case DatePickerEntryMode.Input:
                picker = BuildInputPicker(landscape);
                entryModeButton = BuildEntryModeButton(
                    Current.SwitchToCalendarEntryModeIcon ?? new Icon(Icons.CalendarToday),
                    localizations.CalendarModeButtonLabel,
                    headerForeground);
                break;
            default:
                picker = BuildInputPicker(landscape);
                entryModeButton = null;
                break;
        }

        string helpText = Current.HelpText
                          ?? (useMaterial3 ? localizations.DatePickerHelpText : localizations.DatePickerHelpText.ToUpperInvariant());
        string titleText = _selectedDate is { } selected
            ? Current.CalendarDelegate.FormatMediumDate(selected, localizations)
            : string.Empty;
        var header = BuildHeader(
            helpText,
            titleText,
            headlineStyle,
            landscape,
            entryModeButton,
            datePickerTheme,
            defaults);
        var actions = BuildActions(localizations, useMaterial3, landscape, datePickerTheme, defaults);

        Widget content = landscape
            ? new Row(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    header,
                    useMaterial3
                        ? new VerticalDivider(width: 0, color: datePickerTheme.DividerColor)
                        : new SizedBox(),
                    new Flexible(
                        child: new Column(
                            mainAxisSize: MainAxisSize.Min,
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                new Expanded(child: picker),
                                actions,
                            ])),
                ])
            : new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    header,
                    useMaterial3
                        ? new Divider(height: 0, color: datePickerTheme.DividerColor)
                        : new SizedBox(),
                    new Expanded(child: picker),
                    actions,
                ]);
        content = MediaQuery.WithClampedTextScaling(context, content, maxScaleFactor: 3);
        content = new AnimatedContainer(
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.EaseIn,
            width: dialogSize.Width,
            height: dialogSize.Height,
            child: content);

        return new Dialog(
            backgroundColor: datePickerTheme.BackgroundColor ?? defaults.BackgroundColor,
            elevation: datePickerTheme.Elevation
                       ?? (useMaterial3 ? defaults.Elevation : dialogTheme.Elevation ?? defaults.Elevation),
            shadowColor: datePickerTheme.ShadowColor ?? defaults.ShadowColor,
            surfaceTintColor: datePickerTheme.SurfaceTintColor ?? defaults.SurfaceTintColor,
            shape: datePickerTheme.Shape
                   ?? (useMaterial3 ? defaults.Shape : dialogTheme.Shape ?? defaults.Shape),
            insetPadding: Current.InsetPadding,
            clipBehavior: Clip.AntiAlias,
            child: content);
    }

    private Widget BuildCalendarPicker() => new CalendarDatePicker(
        key: _calendarPickerKey,
        calendarDelegate: Current.CalendarDelegate,
        initialDate: _selectedDate,
        firstDate: Current.FirstDate,
        lastDate: Current.LastDate,
        currentDate: Current.CurrentDate,
        onDateChanged: date => SetState(() => _selectedDate = date),
        selectableDayPredicate: Current.SelectableDayPredicate,
        initialCalendarMode: Current.InitialCalendarMode);

    private Widget BuildInputPicker(bool landscape) => new Form(
        key: _formKey,
        autovalidateMode: _autovalidateMode,
        child: new SizedBox(
            height: landscape ? InputFormLandscapeHeight : InputFormPortraitHeight,
            child: new Padding(
                new Thickness(24, 0),
                new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    children:
                    [
                        new Flexible(
                            child: new InputDatePickerFormField(
                                calendarDelegate: Current.CalendarDelegate,
                                initialDate: _selectedDate,
                                firstDate: Current.FirstDate,
                                lastDate: Current.LastDate,
                                onDateSubmitted: HandleDateChanged,
                                onDateSaved: HandleDateChanged,
                                selectableDayPredicate: Current.SelectableDayPredicate,
                                errorFormatText: Current.ErrorFormatText,
                                errorInvalidText: Current.ErrorInvalidText,
                                fieldHintText: Current.FieldHintText,
                                fieldLabelText: Current.FieldLabelText,
                                autofocus: true)),
                    ]))));

    private Widget BuildEntryModeButton(Widget icon, string tooltip, Color? color) => new Tooltip(
        message: tooltip,
        excludeFromSemantics: true,
        child: new Semantics(
            label: tooltip,
            flags: SemanticsFlags.IsButton | SemanticsFlags.IsEnabled,
            onTap: HandleEntryModeToggle,
            child: new IconButton(icon: icon, color: color, onPressed: HandleEntryModeToggle)));

    private Widget BuildActions(
        MaterialLocalizations localizations,
        bool useMaterial3,
        bool landscape,
        DatePickerThemeData theme,
        DatePickerThemeData defaults)
    {
        Widget actions = new Padding(
            new Thickness(8, 0),
            new Align(
                alignment: Alignment.CenterRight,
                child: new OverflowBar(
                    spacing: 8,
                    alignment: MainAxisAlignment.End,
                    children:
                    [
                        new TextButton(
                            style: theme.CancelButtonStyle ?? defaults.CancelButtonStyle,
                            onPressed: HandleCancel,
                            child: new Text(Current.CancelText
                                            ?? (useMaterial3
                                                ? localizations.CancelButtonLabel
                                                : localizations.CancelButtonLabel.ToUpperInvariant()))),
                        new TextButton(
                            style: theme.ConfirmButtonStyle ?? defaults.ConfirmButtonStyle,
                            onPressed: HandleOk,
                            child: new Text(Current.ConfirmText ?? localizations.OkButtonLabel)),
                    ])));
        actions = MediaQuery.WithClampedTextScaling(
            Context, actions, maxScaleFactor: landscape ? 1.6 : 3);
        return new ConstrainedBox(new BoxConstraints(MinHeight: 52), actions);
    }

    private Widget BuildHeader(
        string helpText,
        string titleText,
        TextStyle titleStyle,
        bool landscape,
        Widget? entryModeButton,
        DatePickerThemeData theme,
        DatePickerThemeData defaults)
    {
        var background = theme.HeaderBackgroundColor ?? defaults.HeaderBackgroundColor ?? Colors.Transparent;
        var foreground = theme.HeaderForegroundColor ?? defaults.HeaderForegroundColor;
        var helpStyle = (theme.HeaderHelpStyle ?? defaults.HeaderHelpStyle ?? Theme.Of(Context).TextTheme.LabelLarge)
            .CopyWith(color: foreground);
        Widget help = new DefaultTextStyle(
            helpStyle,
            new Text(helpText, maxLines: 1, overflow: TextOverflow.Ellipsis));
        Widget title = new DefaultTextStyle(
            titleStyle,
            new Text(
                titleText,
                maxLines: landscape ? 2 : 1,
                overflow: TextOverflow.Ellipsis));

        Widget child;
        if (landscape)
        {
            child = new SizedBox(
                width: 152,
                child: new ColoredBox(
                    background,
                    new Column(
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        children:
                        [
                            new SizedBox(height: 16),
                            new Padding(new Thickness(16, 0), help),
                            new SizedBox(height: _entryMode is DatePickerEntryMode.Input or DatePickerEntryMode.InputOnly ? 16 : 56),
                            new Expanded(child: new Padding(new Thickness(16, 0), title)),
                            entryModeButton is null
                                ? new SizedBox()
                                : new Padding(new Thickness(8, 0, 4, 6), entryModeButton),
                        ])));
        }
        else
        {
            var titleChildren = new List<Widget> { new Expanded(child: title) };
            if (entryModeButton is not null) titleChildren.Add(entryModeButton);
            child = new SizedBox(
                height: 120,
                child: new ColoredBox(
                    background,
                    new Padding(
                        new Thickness(24, 0, 12, 12),
                        new Column(
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            children:
                            [
                                new SizedBox(height: 16),
                                help,
                                new Flexible(child: new SizedBox(height: 38)),
                                new Row(children: titleChildren),
                            ]))));
        }

        return new Semantics(container: true, explicitChildNodes: true, child: child);
    }

    private void HandleOk()
    {
        if (_entryMode is DatePickerEntryMode.Input or DatePickerEntryMode.InputOnly)
        {
            var form = _formKey.CurrentState;
            if (form is null || !form.Validate())
            {
                SetState(() => _autovalidateMode = AutovalidateMode.Always);
                return;
            }
            form.Save();
        }
        Navigator.Of(Context).MaybePop(_selectedDate);
    }

    private void HandleCancel() => Navigator.Of(Context).MaybePop();

    private void HandleEntryModeToggle()
    {
        SetState(() =>
        {
            switch (_entryMode)
            {
                case DatePickerEntryMode.Calendar:
                    _autovalidateMode = AutovalidateMode.Disabled;
                    _entryMode = DatePickerEntryMode.Input;
                    break;
                case DatePickerEntryMode.Input:
                    _formKey.CurrentState?.Save();
                    _entryMode = DatePickerEntryMode.Calendar;
                    break;
                default:
                    throw new InvalidOperationException($"Cannot change entry mode from {_entryMode}.");
            }
            Current.OnDatePickerModeChange?.Invoke(_entryMode);
        });
    }

    private void HandleDateChanged(DateTime date) => SetState(() => _selectedDate = date);

    private static Size ResolveDialogSize(bool material3, bool calendar, bool landscape)
    {
        if (landscape) return calendar ? CalendarLandscapeDialogSize : InputLandscapeDialogSize;
        if (calendar) return material3 ? CalendarPortraitDialogSizeM3 : CalendarPortraitDialogSizeM2;
        return material3 ? InputPortraitDialogSizeM3 : InputPortraitDialogSizeM2;
    }

    private static TextStyle ResolveHeadlineStyle(
        ThemeData theme,
        DatePickerThemeData pickerTheme,
        DatePickerThemeData defaults,
        bool landscape)
    {
        var style = pickerTheme.HeaderHeadlineStyle
                    ?? defaults.HeaderHeadlineStyle
                    ?? theme.TextTheme.HeadlineSmall;
        return landscape ? theme.TextTheme.HeadlineSmall : style;
    }
}

public static partial class MaterialDatePickers
{
    public static Task<DateTime?> ShowDatePicker(
        BuildContext context,
        DateTime firstDate,
        DateTime lastDate,
        DateTime? initialDate = null,
        DateTime? currentDate = null,
        DatePickerEntryMode initialEntryMode = DatePickerEntryMode.Calendar,
        SelectableDayPredicate? selectableDayPredicate = null,
        string? helpText = null,
        string? cancelText = null,
        string? confirmText = null,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null,
        Locale? locale = null,
        TextDirection? textDirection = null,
        DatePickerTransitionBuilder? builder = null,
        DatePickerMode initialDatePickerMode = DatePickerMode.Day,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? fieldHintText = null,
        string? fieldLabelText = null,
        Action<DatePickerEntryMode>? onDatePickerModeChange = null,
        Widget? switchToInputEntryModeIcon = null,
        Widget? switchToCalendarEntryModeIcon = null,
        CalendarDelegate<DateTime>? calendarDelegate = null)
    {
        var dialog = (Widget)new DatePickerDialog(
            initialDate: initialDate,
            firstDate: firstDate,
            lastDate: lastDate,
            currentDate: currentDate,
            initialEntryMode: initialEntryMode,
            selectableDayPredicate: selectableDayPredicate,
            helpText: helpText,
            cancelText: cancelText,
            confirmText: confirmText,
            initialCalendarMode: initialDatePickerMode,
            errorFormatText: errorFormatText,
            errorInvalidText: errorInvalidText,
            fieldHintText: fieldHintText,
            fieldLabelText: fieldLabelText,
            onDatePickerModeChange: onDatePickerModeChange,
            switchToInputEntryModeIcon: switchToInputEntryModeIcon,
            switchToCalendarEntryModeIcon: switchToCalendarEntryModeIcon,
            calendarDelegate: calendarDelegate);
        if (textDirection.HasValue) dialog = new Directionality(textDirection.Value, dialog);
        Locale? effectiveLocale = locale ?? DatePickerTheme.Of(context).Locale;
        if (effectiveLocale is not null)
        {
            dialog = Localizations.Override(context, dialog, locale: effectiveLocale);
        }
        var capturedDialog = dialog;
        return MaterialDialogs.ShowDialog<DateTime?>(
            context,
            routeContext => builder?.Invoke(routeContext, capturedDialog) ?? capturedDialog,
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            useRootNavigator: useRootNavigator,
            routeSettings: routeSettings);
    }
}
