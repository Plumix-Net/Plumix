using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/localizations.dart

public abstract class CupertinoLocalizations
{
    public virtual string AlertDialogLabel => "Alert";

    public virtual string ModalBarrierDismissLabel => "Dismiss";

    public virtual string CutButtonLabel => "Cut";

    public virtual string CopyButtonLabel => "Copy";

    public virtual string PasteButtonLabel => "Paste";

    public virtual string NoSpellCheckReplacementsLabel => "No Replacements Found";

    public virtual string SelectAllButtonLabel => "Select All";

    public virtual string LookUpButtonLabel => "Look Up";

    public virtual string SearchWebButtonLabel => "Search Web";

    public virtual string ShareButtonLabel => "Share...";

    public static CupertinoLocalizations Of(BuildContext context)
    {
        return Localizations.MaybeOf<CupertinoLocalizations>(context)
               ?? DefaultCupertinoLocalizations.Instance;
    }
}

public sealed class DefaultCupertinoLocalizations : CupertinoLocalizations
{
    private DefaultCupertinoLocalizations()
    {
    }

    public static DefaultCupertinoLocalizations Instance { get; } = new();

    public static LocalizationsDelegate<CupertinoLocalizations> Delegate { get; } =
        new DefaultCupertinoLocalizationsDelegate();

    private sealed class DefaultCupertinoLocalizationsDelegate : LocalizationsDelegate<CupertinoLocalizations>
    {
        public override bool IsSupported(Locale locale) => true;

        public override CupertinoLocalizations LoadTyped(Locale locale)
        {
            return Instance;
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }
}
