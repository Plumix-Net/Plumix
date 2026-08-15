using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/autofill.dart

/// <summary>
/// A collection of commonly used autofill hint strings on different platforms.
/// </summary>
/// <remarks>
/// Each hint is pre-defined on at least one supported platform. The framework does not translate
/// them; the host is expected to map a hint onto the platform's own content-type constant
/// (Android's <c>HintConstants</c>, iOS's <c>UITextContentType</c>, the web's <c>autocomplete</c>),
/// and to use the hint string as-is otherwise. iOS accounts only for the first hint, the web uses
/// only the first hint, and Android uses all of them.
/// </remarks>
public static class AutofillHints
{
    /// <summary>The city name of an address.</summary>
    public const string AddressCity = "addressCity";

    /// <summary>The city and state of an address.</summary>
    public const string AddressCityAndState = "addressCityAndState";

    /// <summary>The state of an address.</summary>
    public const string AddressState = "addressState";

    /// <summary>A person's birth date.</summary>
    public const string Birthday = "birthday";

    /// <summary>The day component of a birth date.</summary>
    public const string BirthdayDay = "birthdayDay";

    /// <summary>The month component of a birth date.</summary>
    public const string BirthdayMonth = "birthdayMonth";

    /// <summary>The year component of a birth date.</summary>
    public const string BirthdayYear = "birthdayYear";

    /// <summary>The country code of an address.</summary>
    public const string CountryCode = "countryCode";

    /// <summary>The country name of an address.</summary>
    public const string CountryName = "countryName";

    /// <summary>A credit card expiration date.</summary>
    public const string CreditCardExpirationDate = "creditCardExpirationDate";

    /// <summary>The day component of a credit card expiration date.</summary>
    public const string CreditCardExpirationDay = "creditCardExpirationDay";

    /// <summary>The month component of a credit card expiration date.</summary>
    public const string CreditCardExpirationMonth = "creditCardExpirationMonth";

    /// <summary>The year component of a credit card expiration date.</summary>
    public const string CreditCardExpirationYear = "creditCardExpirationYear";

    /// <summary>The family name of a credit card holder.</summary>
    public const string CreditCardFamilyName = "creditCardFamilyName";

    /// <summary>The given name of a credit card holder.</summary>
    public const string CreditCardGivenName = "creditCardGivenName";

    /// <summary>The middle name of a credit card holder.</summary>
    public const string CreditCardMiddleName = "creditCardMiddleName";

    /// <summary>The full name of a credit card holder.</summary>
    public const string CreditCardName = "creditCardName";

    /// <summary>A credit card number.</summary>
    public const string CreditCardNumber = "creditCardNumber";

    /// <summary>A credit card security code.</summary>
    public const string CreditCardSecurityCode = "creditCardSecurityCode";

    /// <summary>The type of a credit card.</summary>
    public const string CreditCardType = "creditCardType";

    /// <summary>An email address.</summary>
    public const string Email = "email";

    /// <summary>A person's family name.</summary>
    public const string FamilyName = "familyName";

    /// <summary>A street address, in full.</summary>
    public const string FullStreetAddress = "fullStreetAddress";

    /// <summary>A person's gender.</summary>
    public const string Gender = "gender";

    /// <summary>A person's given name.</summary>
    public const string GivenName = "givenName";

    /// <summary>An instant messaging protocol endpoint.</summary>
    public const string Impp = "impp";

    /// <summary>A job title.</summary>
    public const string JobTitle = "jobTitle";

    /// <summary>A language name or code.</summary>
    public const string Language = "language";

    /// <summary>A location, such as a set of coordinates.</summary>
    public const string Location = "location";

    /// <summary>A person's middle initial.</summary>
    public const string MiddleInitial = "middleInitial";

    /// <summary>A person's middle name.</summary>
    public const string MiddleName = "middleName";

    /// <summary>A person's full name.</summary>
    public const string Name = "name";

    /// <summary>A prefix or title, such as "Mr.".</summary>
    public const string NamePrefix = "namePrefix";

    /// <summary>A suffix, such as "Jr.".</summary>
    public const string NameSuffix = "nameSuffix";

    /// <summary>A newly created password, for a sign-up or password-change form.</summary>
    public const string NewPassword = "newPassword";

    /// <summary>A newly created username, for a sign-up form.</summary>
    public const string NewUsername = "newUsername";

    /// <summary>A nickname.</summary>
    public const string Nickname = "nickname";

    /// <summary>A one-time code, typically delivered out of band.</summary>
    public const string OneTimeCode = "oneTimeCode";

    /// <summary>An organization name.</summary>
    public const string OrganizationName = "organizationName";

    /// <summary>A password.</summary>
    public const string Password = "password";

    /// <summary>A photograph.</summary>
    public const string Photo = "photo";

    /// <summary>A postal address.</summary>
    public const string PostalAddress = "postalAddress";

    /// <summary>An extended postal address.</summary>
    public const string PostalAddressExtended = "postalAddressExtended";

    /// <summary>The postal code of an extended postal address.</summary>
    public const string PostalAddressExtendedPostalCode = "postalAddressExtendedPostalCode";

    /// <summary>A postal code.</summary>
    public const string PostalCode = "postalCode";

    /// <summary>The first administrative level of an address.</summary>
    public const string StreetAddressLevel1 = "streetAddressLevel1";

    /// <summary>The second administrative level of an address.</summary>
    public const string StreetAddressLevel2 = "streetAddressLevel2";

    /// <summary>The third administrative level of an address.</summary>
    public const string StreetAddressLevel3 = "streetAddressLevel3";

    /// <summary>The fourth administrative level of an address.</summary>
    public const string StreetAddressLevel4 = "streetAddressLevel4";

    /// <summary>The first line of a street address.</summary>
    public const string StreetAddressLine1 = "streetAddressLine1";

    /// <summary>The second line of a street address.</summary>
    public const string StreetAddressLine2 = "streetAddressLine2";

    /// <summary>The third line of a street address.</summary>
    public const string StreetAddressLine3 = "streetAddressLine3";

    /// <summary>A sublocality of an address.</summary>
    public const string Sublocality = "sublocality";

    /// <summary>A telephone number.</summary>
    public const string TelephoneNumber = "telephoneNumber";

    /// <summary>The area code of a telephone number.</summary>
    public const string TelephoneNumberAreaCode = "telephoneNumberAreaCode";

    /// <summary>The country code of a telephone number.</summary>
    public const string TelephoneNumberCountryCode = "telephoneNumberCountryCode";

    /// <summary>A device-specific telephone number.</summary>
    public const string TelephoneNumberDevice = "telephoneNumberDevice";

    /// <summary>The extension of a telephone number.</summary>
    public const string TelephoneNumberExtension = "telephoneNumberExtension";

    /// <summary>The local part of a telephone number.</summary>
    public const string TelephoneNumberLocal = "telephoneNumberLocal";

    /// <summary>The local prefix of a telephone number.</summary>
    public const string TelephoneNumberLocalPrefix = "telephoneNumberLocalPrefix";

    /// <summary>The local suffix of a telephone number.</summary>
    public const string TelephoneNumberLocalSuffix = "telephoneNumberLocalSuffix";

    /// <summary>The national part of a telephone number.</summary>
    public const string TelephoneNumberNational = "telephoneNumberNational";

    /// <summary>A transaction amount.</summary>
    public const string TransactionAmount = "transactionAmount";

    /// <summary>A transaction currency.</summary>
    public const string TransactionCurrency = "transactionCurrency";

    /// <summary>A URL.</summary>
    public const string Url = "url";

    /// <summary>A username.</summary>
    public const string Username = "username";
}

/// <summary>
/// A collection of autofill related information that represents an
/// <see cref="IAutofillClient"/>.
/// </summary>
/// <remarks>
/// Typically used in a <see cref="TextInputConfiguration"/> to request autofill for a text field.
/// </remarks>
public sealed class AutofillConfiguration : IEquatable<AutofillConfiguration>
{
    /// <summary>Creates an enabled autofill configuration.</summary>
    public AutofillConfiguration(
        string uniqueIdentifier,
        IReadOnlyList<string> autofillHints,
        TextEditingValue currentEditingValue,
        string? hintText = null)
        : this(true, uniqueIdentifier, autofillHints, currentEditingValue, hintText)
    {
    }

    private AutofillConfiguration(
        bool enabled,
        string uniqueIdentifier,
        IReadOnlyList<string>? autofillHints,
        TextEditingValue currentEditingValue,
        string? hintText)
    {
        Enabled = enabled;
        UniqueIdentifier = uniqueIdentifier;
        AutofillHints = autofillHints ?? [];
        CurrentEditingValue = currentEditingValue;
        HintText = hintText;
    }

    /// <summary>An <see cref="AutofillConfiguration"/> that indicates the field is not autofillable.
    /// </summary>
    public static AutofillConfiguration Disabled { get; } =
        new(false, string.Empty, [], new TextEditingValue(), null);

    /// <summary>Whether autofill should be enabled for the client.</summary>
    public bool Enabled { get; }

    /// <summary>A string that uniquely identifies the client within its autofill scope.</summary>
    public string UniqueIdentifier { get; }

    /// <summary>The hints a host can use to decide what value to fill the client with.</summary>
    public IReadOnlyList<string> AutofillHints { get; }

    /// <summary>The current editing value of the client.</summary>
    public TextEditingValue CurrentEditingValue { get; }

    /// <summary>The optional hint text placed on the view that typically suggests what sort of input
    /// the field wants.</summary>
    public string? HintText { get; }

    /// <summary>The JSON payload sent to the host, or <c>null</c> when autofill is disabled.</summary>
    public Dictionary<string, object?>? ToJson()
    {
        if (!Enabled)
        {
            return null;
        }

        var json = new Dictionary<string, object?>
        {
            ["uniqueIdentifier"] = UniqueIdentifier,
            ["hints"] = AutofillHints,
            ["editingValue"] = CurrentEditingValue.ToJson(),
        };
        if (HintText != null)
        {
            json["hintText"] = HintText;
        }

        return json;
    }

    /// <inheritdoc/>
    public bool Equals(AutofillConfiguration? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && other.Enabled == Enabled
               && other.UniqueIdentifier == UniqueIdentifier
               && other.AutofillHints.SequenceEqual(AutofillHints)
               && other.CurrentEditingValue.Equals(CurrentEditingValue)
               && other.HintText == HintText;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as AutofillConfiguration);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Enabled);
        hash.Add(UniqueIdentifier);
        foreach (string hint in AutofillHints)
        {
            hash.Add(hint);
        }

        hash.Add(CurrentEditingValue);
        hash.Add(HintText);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        string hints = $"[{string.Join(", ", AutofillHints)}]";
        string text = $"AutofillConfiguration(enabled: {Enabled}, uniqueIdentifier: {UniqueIdentifier}, "
                      + $"autofillHints: {hints}, currentEditingValue: {CurrentEditingValue}";
        return HintText is null ? text + ")" : text + $", hintText: {HintText})";
    }
}

/// <summary>
/// An object that represents an autofillable input field in the autofill workflow.
/// </summary>
public interface IAutofillClient
{
    /// <summary>The unique identifier of this <see cref="IAutofillClient"/>. Must not change.</summary>
    string AutofillId { get; }

    /// <summary>The <see cref="TextInputConfiguration"/> that describes this client.</summary>
    TextInputConfiguration TextInputConfiguration { get; }

    /// <summary>Requests this client to update its editing value to the given one.</summary>
    void Autofill(TextEditingValue newEditingValue);
}

/// <summary>
/// A collection of autofill clients that are cross-referenced to each other.
/// </summary>
/// <remarks>
/// C# has no mixins, so Dart's <c>AutofillScopeMixin</c> body lives on the
/// <see cref="AutofillScopeMixin"/> companion: implementers forward
/// <see cref="Attach"/> to <see cref="AutofillScopeMixin.Attach"/>.
/// </remarks>
public interface IAutofillScope
{
    /// <summary>Gets the <see cref="IAutofillClient"/> with the given id, or <c>null</c>.</summary>
    IAutofillClient? GetAutofillClient(string autofillId);

    /// <summary>The collection of clients in this scope, in registration order.</summary>
    IEnumerable<IAutofillClient> AutofillClients { get; }

    /// <summary>Allows a <see cref="ITextInputClient"/> to attach to this scope.</summary>
    TextInputConnection Attach(ITextInputClient trigger, TextInputConfiguration configuration);
}

/// <summary>Carries the body of Dart's <c>AutofillScopeMixin</c>.</summary>
public static class AutofillScopeMixin
{
    /// <summary>Attaches <paramref name="trigger"/> to the platform, exposing every client in
    /// <paramref name="scope"/> as an autofillable field.</summary>
    public static TextInputConnection Attach(
        IAutofillScope scope,
        ITextInputClient trigger,
        TextInputConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.AutofillClients.Any(client => !client.TextInputConfiguration.AutofillConfiguration.Enabled))
        {
            throw new InvalidOperationException(
                "Every client in AutofillScope.autofillClients must enable autofill");
        }

        TextInputConfiguration inputConfiguration = new AutofillScopeTextInputConfiguration(
            scope.AutofillClients.Select(client => client.TextInputConfiguration),
            configuration);
        return TextInput.Attach(trigger, inputConfiguration);
    }
}

/// <summary>
/// The <see cref="TextInputConfiguration"/> an <see cref="IAutofillScope"/> sends to the host:
/// the triggering client's own configuration plus a <c>fields</c> entry describing every client.
/// </summary>
internal sealed class AutofillScopeTextInputConfiguration : TextInputConfiguration
{
    internal AutofillScopeTextInputConfiguration(
        IEnumerable<TextInputConfiguration> allConfigurations,
        TextInputConfiguration currentClientConfiguration)
        : base(
            viewId: currentClientConfiguration.ViewId,
            keyboardType: currentClientConfiguration.KeyboardType,
            obscureText: currentClientConfiguration.ObscureText,
            autocorrect: currentClientConfiguration.Autocorrect,
            smartDashesType: currentClientConfiguration.SmartDashesType,
            smartQuotesType: currentClientConfiguration.SmartQuotesType,
            enableSuggestions: currentClientConfiguration.EnableSuggestions,
            inputAction: currentClientConfiguration.InputAction,
            textCapitalization: currentClientConfiguration.TextCapitalization,
            actionLabel: currentClientConfiguration.ActionLabel,
            autofillConfiguration: currentClientConfiguration.AutofillConfiguration,
            multiline: currentClientConfiguration.Multiline)
    {
        AllConfigurations = allConfigurations;
    }

    internal IEnumerable<TextInputConfiguration> AllConfigurations { get; }

    /// <inheritdoc/>
    public override Dictionary<string, object?> ToJson()
    {
        Dictionary<string, object?> result = base.ToJson();
        result["fields"] = AllConfigurations
            .Select(configuration => configuration.ToJson())
            .ToList();
        return result;
    }
}
