using System.Globalization;

namespace RedactorApi.Analyzer.Faker;

public class Faker
{
    private enum Gender
    {
        Male,
        Female
    }

    private readonly Dictionary<string, string> _entities = new()
    {
        { "CREDIT_CARD", "A credit card number is between 12 to 19 digits." },
        { "CRYPTO", "A Crypto wallet number. Currently only Bitcoin address is supported" },
        { "DATE_TIME", "Absolute or relative dates or periods or times smaller than a day." },
        {
            "EMAIL_ADDRESS",
            "An email address identifies an email box to which email messages are delivered"
        },
        {
            "IBAN_CODE",
            "The International Bank Account Number (IBAN) is an internationally agreed system of identifying bank accounts across national borders to facilitate the communication and processing of cross border transactions with a reduced risk of transcription errors."
        },
        { "IP_ADDRESS", "An Internet Protocol (IP) address (either IPv4 or IPv6)." },
        { "NRP", "A person's Nationality, religious or political group." },
        {
            "LOCATION",
            "Name of politically or geographically defined location (cities, provinces, countries, international regions, bodies of water, mountains"
        },
        {
            "PERSON",
            "A full person name, which can include first names, middle names or initials, and last names."
        },
        { "PHONE_NUMBER", "A telephone number	Custom logic," },
        { "MEDICAL_LICENSE", "Common medical license numbers." },
        {
            "URL",
            "A URL (Uniform Resource Locator), unique identifier used to locate a resource on the Internet"
        },
        { "US_BANK_NUMBER", "A US bank account number is between 8 to 17 digits." },
        { "US_DRIVER_LICENSE", "A US driver license according to" },
        {
            "US_ITIN",
            "US Individual Taxpayer Identification Number (ITIN). Nine digits that start with a '9' and contain a '7' or '8' as the 4 digit."
        },
        { "US_PASSPORT", "A US passport number with 9 digits." },
        { "US_SSN", "A US Social Security Number (SSN) with 9 digits." },
        { "UK_NHS", "A UK NHS number is 10 digits." },
        {
            "UK_NINO",
            "UK National Insurance Number is a unique identifier used in the administration of National Insurance and tax."
        },
        {"UK_POST_CODE", "UK Post Code is a unique identifier used in identifying addresses."},
        { "ES_NIF", "A spanish NIF number (Personal tax ID)." },
        { "ES_NIE", "A spanish NIE number (Foreigners ID card)." },
        { "IT_FISCAL_CODE", "An Italian personal identification code." },
        { "IT_DRIVER_LICENSE", "An Italian driver license number." },
        { "IT_VAT_CODE", "An Italian VAT code number" },
        { "IT_PASSPORT", "An Italian passport number." },
        { "IT_IDENTITY_CARD", "An Italian identity card number." },
        { "PL_PESEL", "Polish PESEL number" },
        { "SG_NRIC_FIN", "A National Registration Identification Card" },
        {
            "SG_UEN",
            "A Unique Entity Number (UEN) is a standard identification number for entities registered in Singapore."
        },
        {
            "AU_ABN",
            "The Australian Business Number (ABN) is a unique 11 digit identifier issued to all entities registered in the Australian Business Register (ABR)."
        },
        {
            "AU_ACN",
            "An Australian Company Number is a unique nine-digit number issued by the Australian Securities and Investments Commission to every company registered under the Commonwealth Corporations Act 2001 as an identifier."
        },
        {
            "AU_TFN",
            "The tax file number (TFN) is a unique identifier issued by the Australian Taxation Office to each taxpaying entity"
        },
        {
            "AU_MEDICARE",
            "Medicare number is a unique identifier issued by Australian Government that enables the cardholder to receive a rebates of medical expenses under Australia's Medicare system"
        },
        {
            "IN_PAN",
            "The Indian Permanent Account Number (PAN) is a unique 12 character alphanumeric identifier issued to all business and individual entities registered as Tax Payers."
        },
        { "IN_AADHAAR", "Indian government issued unique 12 digit individual identity number" },
        {
            "IN_VEHICLE_REGISTRATION",
            "Indian government issued transport (govt, personal, diplomatic, defence) vehicle registration number"
        },
        {
            "IN_VOTER",
            "Indian Election Commission issued 10 digit alpha numeric voter id for all indian citizens (age 18 or above)"
        },
        { "IN_PASSPORT", "Indian Passport Number" },
        {
            "FI_PERSONAL_IDENTITY_CODE",
            "The Finnish Personal Identity Code (Henkilötunnus) is a unique 11 character individual identity number."
        },
    };

    public Faker()
    {
        //Randomizer.Seed = new Random(3897234);
    }

    public string GetDescription(string entityType)
    {
        return _entities[entityType];
    }

    public string GetFakeData(string entityType) => entityType switch
    {
        "CREDIT_CARD" => new Bogus.DataSets.Finance().CreditCardNumber(),
        // "CRYPTO" => new Bogus.DataSets.Crypto().BitcoinAddress(),
        "DATE_TIME" => new Bogus.DataSets.Date().Past().ToString(CultureInfo.InvariantCulture),
        "EMAIL_ADDRESS" => new Bogus.DataSets.Internet().Email(),
        "IBAN_CODE" => new Bogus.DataSets.Finance().Iban(),
        "IP_ADDRESS" => new Bogus.DataSets.Internet().Ip(),
        "NRP" => new Bogus.DataSets.Name().JobTitle(),
        "LOCATION" => new Bogus.DataSets.Address().City(),
        "PERSON" => new Bogus.DataSets.Name().FullName(),
        "PHONE_NUMBER" => new Bogus.DataSets.PhoneNumbers().PhoneNumber(),
        "MEDICAL_LICENSE" => new Bogus.DataSets.Lorem().Word(),
        "URL" => new Bogus.DataSets.Internet().Url(),
        "US_BANK_NUMBER" => new Bogus.DataSets.Finance().Account(),
        "US_DRIVER_LICENSE" => new Bogus.DataSets.Lorem().Word(),
        "US_ITIN" => new Bogus.DataSets.Lorem().Word(),
        "US_PASSPORT" => new Bogus.DataSets.Lorem().Word(),
        "US_SSN" => new Bogus.DataSets.Lorem().Word(),
        "UK_NHS" => new Bogus.DataSets.Lorem().Word(),
        "UK_NINO" => new Bogus.DataSets.Lorem().Word(),
        "UK_POST_CODE" => new Bogus.DataSets.Lorem().Word(),
        "ES_NIF" => new Bogus.DataSets.Lorem().Word(),
        "ES_NIE" => new Bogus.DataSets.Lorem().Word(),
        "IT_FISCAL_CODE" => new Bogus.DataSets.Lorem().Word(),
        "IT_DRIVER_LICENSE" => new Bogus.DataSets.Lorem().Word(),
        "IT_VAT_CODE" => new Bogus.DataSets.Lorem().Word(),
        "IT_PASSPORT" => new Bogus.DataSets.Lorem().Word(),
        "IT_IDENTITY_CARD" => new Bogus.DataSets.Lorem().Word(),
        "PL_PESEL" => new Bogus.DataSets.Lorem().Word(),
        "SG_NRIC_FIN" => new Bogus.DataSets.Lorem().Word(),
        "SG_UEN" => new Bogus.DataSets.Lorem().Word(),
        "AU_ABN" => new Bogus.DataSets.Lorem().Word(),
        "AU_ACN" => new Bogus.DataSets.Lorem().Word(),
        "AU_TFN" => new Bogus.DataSets.Lorem().Word(),
        "AU_MEDICARE" => new Bogus.DataSets.Lorem().Word(),
        "IN_PAN" => new Bogus.DataSets.Lorem().Word(),
        "IN_AADHAAR" => new Bogus.DataSets.Lorem().Word(),
        "IN_VEHICLE_REGISTRATION" => new Bogus.DataSets.Lorem().Word(),
        "IN_VOTER" => new Bogus.DataSets.Lorem().Word(),
        "IN_PASSPORT" => new Bogus.DataSets.Lorem().Word(),
        "FI_PERSONAL_IDENTITY_CODE" => new Bogus.DataSets.Lorem().Word(),
        _ => throw new NotImplementedException()
    };
}
