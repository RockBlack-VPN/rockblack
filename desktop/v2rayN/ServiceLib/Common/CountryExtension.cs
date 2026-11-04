namespace ServiceLib.Common;

/// <summary>
/// Extension methods for country code utilities
/// </summary>
public static class CountryExtension
{
    /// <summary>
    /// Country code to emoji flag mapping for common countries
    /// </summary>
    private static readonly Dictionary<string, string> CountryEmojiMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Asia
        { "CN", "🇨🇳" }, // China
        { "HK", "🇭🇰" }, // Hong Kong
        { "TW", "🇹🇼" }, // Taiwan
        { "JP", "🇯🇵" }, // Japan
        { "SG", "🇸🇬" }, // Singapore
        { "KR", "🇰🇷" }, // South Korea
        { "TH", "🇹🇭" }, // Thailand
        { "VN", "🇻🇳" }, // Vietnam
        { "ID", "🇮🇩" }, // Indonesia
        { "PH", "🇵🇭" }, // Philippines
        { "MY", "🇲🇾" }, // Malaysia
        { "IN", "🇮🇳" }, // India
        { "PK", "🇵🇰" }, // Pakistan
        { "BD", "🇧🇩" }, // Bangladesh
        { "LK", "🇱🇰" }, // Sri Lanka
        { "KH", "🇰🇭" }, // Cambodia
        { "LA", "🇱🇦" }, // Laos
        { "MM", "🇲🇲" }, // Myanmar

        // Americas
        { "US", "🇺🇸" }, // United States
        { "CA", "🇨🇦" }, // Canada
        { "MX", "🇲🇽" }, // Mexico
        { "BR", "🇧🇷" }, // Brazil
        { "AR", "🇦🇷" }, // Argentina
        { "CL", "🇨🇱" }, // Chile
        { "CO", "🇨🇴" }, // Colombia

        // Europe
        { "GB", "🇬🇧" }, // United Kingdom
        { "DE", "🇩🇪" }, // Germany
        { "FR", "🇫🇷" }, // France
        { "IT", "🇮🇹" }, // Italy
        { "ES", "🇪🇸" }, // Spain
        { "RU", "🇷🇺" }, // Russia
        { "NL", "🇳🇱" }, // Netherlands
        { "CH", "🇨🇭" }, // Switzerland
        { "SE", "🇸🇪" }, // Sweden
        { "NO", "🇳🇴" }, // Norway
        { "DK", "🇩🇰" }, // Denmark
        { "FI", "🇫🇮" }, // Finland
        { "PL", "🇵🇱" }, // Poland
        { "CZ", "🇨🇿" }, // Czech Republic
        { "AT", "🇦🇹" }, // Austria
        { "GR", "🇬🇷" }, // Greece
        { "PT", "🇵🇹" }, // Portugal
        { "TR", "🇹🇷" }, // Turkey
        { "UA", "🇺🇦" }, // Ukraine
        { "RO", "🇷🇴" }, // Romania

        // Middle East & Central Asia
        { "AE", "🇦🇪" }, // United Arab Emirates
        { "SA", "🇸🇦" }, // Saudi Arabia
        { "IL", "🇮🇱" }, // Israel
        { "KZ", "🇰🇿" }, // Kazakhstan

        // Oceania
        { "AU", "🇦🇺" }, // Australia
        { "NZ", "🇳🇿" }, // New Zealand

        // Africa
        { "ZA", "🇿🇦" }, // South Africa
        { "EG", "🇪🇬" }, // Egypt
    };

    /// <summary>
    /// Converts country code to flag emoji using predefined mapping
    /// Example: "US" -> "🇺🇸", "CN" -> "🇨🇳"
    /// </summary>
    public static string? CountryToEmoji(this string? countryCode)
    {
        if (countryCode.IsNullOrEmpty())
        {
            return null;
        }

        return CountryEmojiMap.GetValueOrDefault(countryCode);
    }

    #region RockBlack server row flags

    private const int RegionalIndicatorA = 0x1F1E6;
    private const int RegionalIndicatorZ = 0x1F1FF;

    /// <summary>Builds the flag emoji of any ISO 3166-1 alpha-2 code ("de" -> 🇩🇪).</summary>
    public static string CodeToFlagEmoji(string? code)
    {
        if (code is not { Length: 2 } || !char.IsAsciiLetter(code[0]) || !char.IsAsciiLetter(code[1]))
        {
            return string.Empty;
        }
        var c = code.ToUpperInvariant();
        return char.ConvertFromUtf32(RegionalIndicatorA + (c[0] - 'A')) + char.ConvertFromUtf32(RegionalIndicatorA + (c[1] - 'A'));
    }

    /// <summary>
    /// Splits a server name into its leading flag emoji (if any), the ISO code of that flag and
    /// the remaining display name: "🇩🇪 Germany" -> ("🇩🇪", "de", "Germany").
    /// </summary>
    public static (string Flag, string Code, string Name) SplitLeadingFlag(string? text)
    {
        var s = (text ?? string.Empty).Trim();
        if (s.Length >= 4 && char.IsHighSurrogate(s[0]) && char.IsHighSurrogate(s[2]))
        {
            var first = char.ConvertToUtf32(s, 0);
            var second = char.ConvertToUtf32(s, 2);
            if (first is >= RegionalIndicatorA and <= RegionalIndicatorZ && second is >= RegionalIndicatorA and <= RegionalIndicatorZ)
            {
                var code = new string([(char)('a' + (first - RegionalIndicatorA)), (char)('a' + (second - RegionalIndicatorA))]);
                var rest = s[4..].TrimStart(' ', '|', '-', '_', '·', '•', ':', '\u00A0', '\uFE0F');
                return (s[..4], code, rest.IsNullOrEmpty() ? s[..4] : rest);
            }
        }
        return (string.Empty, string.Empty, s);
    }

    /// <summary>Country names (English / Russian / German) and well-known cities mapped to ISO codes.</summary>
    private static readonly Dictionary<string, string> CountryNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["germany"] = "de", ["deutschland"] = "de", ["германия"] = "de", ["frankfurt"] = "de", ["франкфурт"] = "de", ["berlin"] = "de", ["берлин"] = "de", ["nuremberg"] = "de", ["falkenstein"] = "de",
        ["netherlands"] = "nl", ["holland"] = "nl", ["niederlande"] = "nl", ["нидерланды"] = "nl", ["голландия"] = "nl", ["amsterdam"] = "nl", ["амстердам"] = "nl",
        ["finland"] = "fi", ["finnland"] = "fi", ["финляндия"] = "fi", ["helsinki"] = "fi", ["хельсинки"] = "fi",
        ["sweden"] = "se", ["schweden"] = "se", ["швеция"] = "se", ["stockholm"] = "se", ["стокгольм"] = "se",
        ["norway"] = "no", ["norwegen"] = "no", ["норвегия"] = "no", ["oslo"] = "no",
        ["denmark"] = "dk", ["dänemark"] = "dk", ["дания"] = "dk", ["copenhagen"] = "dk",
        ["poland"] = "pl", ["polen"] = "pl", ["польша"] = "pl", ["warsaw"] = "pl", ["варшава"] = "pl",
        ["latvia"] = "lv", ["lettland"] = "lv", ["латвия"] = "lv", ["riga"] = "lv", ["рига"] = "lv",
        ["lithuania"] = "lt", ["litauen"] = "lt", ["литва"] = "lt", ["vilnius"] = "lt", ["вильнюс"] = "lt",
        ["estonia"] = "ee", ["estland"] = "ee", ["эстония"] = "ee", ["tallinn"] = "ee", ["таллин"] = "ee",
        ["moldova"] = "md", ["moldau"] = "md", ["молдова"] = "md", ["молдавия"] = "md", ["chisinau"] = "md", ["кишинёв"] = "md", ["кишинев"] = "md",
        ["france"] = "fr", ["frankreich"] = "fr", ["франция"] = "fr", ["paris"] = "fr", ["париж"] = "fr",
        ["united kingdom"] = "gb", ["great britain"] = "gb", ["britain"] = "gb", ["england"] = "gb", ["uk"] = "gb", ["großbritannien"] = "gb", ["великобритания"] = "gb", ["англия"] = "gb", ["london"] = "gb", ["лондон"] = "gb",
        ["spain"] = "es", ["spanien"] = "es", ["испания"] = "es", ["madrid"] = "es",
        ["italy"] = "it", ["italien"] = "it", ["италия"] = "it", ["milan"] = "it", ["rome"] = "it",
        ["switzerland"] = "ch", ["schweiz"] = "ch", ["швейцария"] = "ch", ["zurich"] = "ch",
        ["austria"] = "at", ["österreich"] = "at", ["австрия"] = "at", ["vienna"] = "at", ["вена"] = "at",
        ["czechia"] = "cz", ["czech"] = "cz", ["tschechien"] = "cz", ["чехия"] = "cz", ["prague"] = "cz", ["прага"] = "cz",
        ["hungary"] = "hu", ["ungarn"] = "hu", ["венгрия"] = "hu", ["budapest"] = "hu",
        ["romania"] = "ro", ["rumänien"] = "ro", ["румыния"] = "ro", ["bucharest"] = "ro",
        ["bulgaria"] = "bg", ["bulgarien"] = "bg", ["болгария"] = "bg", ["sofia"] = "bg",
        ["serbia"] = "rs", ["serbien"] = "rs", ["сербия"] = "rs", ["belgrade"] = "rs",
        ["greece"] = "gr", ["griechenland"] = "gr", ["греция"] = "gr", ["athens"] = "gr",
        ["portugal"] = "pt", ["португалия"] = "pt", ["lisbon"] = "pt",
        ["belgium"] = "be", ["belgien"] = "be", ["бельгия"] = "be", ["brussels"] = "be",
        ["ireland"] = "ie", ["irland"] = "ie", ["ирландия"] = "ie", ["dublin"] = "ie",
        ["iceland"] = "is", ["island"] = "is", ["исландия"] = "is",
        ["luxembourg"] = "lu", ["luxemburg"] = "lu", ["люксембург"] = "lu",
        ["ukraine"] = "ua", ["украина"] = "ua", ["kyiv"] = "ua", ["kiev"] = "ua", ["киев"] = "ua",
        ["russia"] = "ru", ["russland"] = "ru", ["россия"] = "ru", ["moscow"] = "ru", ["москва"] = "ru", ["saint petersburg"] = "ru", ["санкт-петербург"] = "ru",
        ["belarus"] = "by", ["беларусь"] = "by", ["minsk"] = "by", ["минск"] = "by",
        ["kazakhstan"] = "kz", ["kasachstan"] = "kz", ["казахстан"] = "kz", ["almaty"] = "kz", ["алматы"] = "kz", ["astana"] = "kz",
        ["georgia"] = "ge", ["georgien"] = "ge", ["грузия"] = "ge", ["tbilisi"] = "ge",
        ["armenia"] = "am", ["armenien"] = "am", ["армения"] = "am", ["yerevan"] = "am",
        ["azerbaijan"] = "az", ["aserbaidschan"] = "az", ["азербайджан"] = "az", ["baku"] = "az",
        ["uzbekistan"] = "uz", ["узбекистан"] = "uz", ["kyrgyzstan"] = "kg", ["киргизия"] = "kg",
        ["turkey"] = "tr", ["türkiye"] = "tr", ["türkei"] = "tr", ["турция"] = "tr", ["istanbul"] = "tr", ["стамбул"] = "tr",
        ["israel"] = "il", ["израиль"] = "il", ["uae"] = "ae", ["emirates"] = "ae", ["оаэ"] = "ae", ["dubai"] = "ae", ["дубай"] = "ae",
        ["united states"] = "us", ["usa"] = "us", ["america"] = "us", ["сша"] = "us", ["new york"] = "us", ["los angeles"] = "us", ["seattle"] = "us", ["miami"] = "us", ["chicago"] = "us", ["dallas"] = "us",
        ["canada"] = "ca", ["kanada"] = "ca", ["канада"] = "ca", ["toronto"] = "ca",
        ["mexico"] = "mx", ["мексика"] = "mx", ["brazil"] = "br", ["brasilien"] = "br", ["бразилия"] = "br", ["argentina"] = "ar", ["аргентина"] = "ar",
        ["japan"] = "jp", ["япония"] = "jp", ["tokyo"] = "jp", ["токио"] = "jp", ["osaka"] = "jp",
        ["singapore"] = "sg", ["singapur"] = "sg", ["сингапур"] = "sg",
        ["hong kong"] = "hk", ["hongkong"] = "hk", ["гонконг"] = "hk",
        ["taiwan"] = "tw", ["тайвань"] = "tw", ["korea"] = "kr", ["south korea"] = "kr", ["корея"] = "kr", ["seoul"] = "kr",
        ["india"] = "in", ["indien"] = "in", ["индия"] = "in", ["mumbai"] = "in",
        ["thailand"] = "th", ["таиланд"] = "th", ["vietnam"] = "vn", ["вьетнам"] = "vn", ["indonesia"] = "id", ["индонезия"] = "id", ["malaysia"] = "my", ["малайзия"] = "my",
        ["australia"] = "au", ["australien"] = "au", ["австралия"] = "au", ["sydney"] = "au",
        ["south africa"] = "za", ["юар"] = "za", ["egypt"] = "eg", ["египет"] = "eg",
    };

    /// <summary>ISO codes accepted as a standalone token ("DE-01", "[NL] Fast", "us2").</summary>
    private static readonly HashSet<string> KnownCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "nl", "fi", "se", "no", "dk", "pl", "lv", "lt", "ee", "md", "fr", "gb", "uk", "es", "it", "ch", "at", "cz", "hu", "ro", "bg", "rs",
        "gr", "pt", "be", "ie", "lu", "ua", "ru", "by", "kz", "ge", "am", "tr", "il", "ae", "us", "ca", "mx", "br", "jp", "sg", "hk", "tw", "kr",
        "in", "th", "vn", "my", "au", "za",
    };

    private static readonly Regex CodeTokenRegex = new(@"(?<![\p{L}])([A-Za-z]{2})(?:[\s\-_|\]\)\.:#]|\d|$)", RegexOptions.Compiled);

    /// <summary>
    /// Detects the country of a server from its name: leading flag emoji, then a country/city name
    /// (en/ru/de), then a standalone ISO code token. Returns "" when unknown.
    /// </summary>
    public static string DetectCountryCode(string? name)
    {
        var (_, code, rest) = SplitLeadingFlag(name);
        if (code.IsNotEmpty())
        {
            return code;
        }
        if (rest.IsNullOrEmpty())
        {
            return string.Empty;
        }

        // Flag emoji anywhere in the name
        for (var i = 0; i + 3 < rest.Length; i++)
        {
            if (char.IsHighSurrogate(rest[i]) && char.IsHighSurrogate(rest[i + 2]))
            {
                var (_, inner, _) = SplitLeadingFlag(rest[i..]);
                if (inner.IsNotEmpty())
                {
                    return inner;
                }
            }
        }

        var lower = rest.ToLowerInvariant();
        string? best = null;
        var bestIndex = int.MaxValue;
        foreach (var (key, value) in CountryNameMap)
        {
            if (key.Length < 3)
            {
                continue;
            }
            var idx = lower.IndexOf(key, StringComparison.Ordinal);
            while (idx >= 0)
            {
                var startOk = idx == 0 || !char.IsLetter(lower[idx - 1]);
                var end = idx + key.Length;
                var endOk = end >= lower.Length || !char.IsLetter(lower[end]);
                if (startOk && endOk)
                {
                    if (idx < bestIndex)
                    {
                        best = value;
                        bestIndex = idx;
                    }
                    break;
                }
                idx = lower.IndexOf(key, idx + 1, StringComparison.Ordinal);
            }
        }
        if (best != null)
        {
            return best;
        }

        foreach (Match m in CodeTokenRegex.Matches(rest))
        {
            var token = m.Groups[1].Value;
            var next = m.Index + 2 < rest.Length ? rest[m.Index + 2] : ' ';
            // "DE-01", "[NL]" (upper case) or "us2" (lower case followed by a digit); not "My server".
            var plausible = token.All(char.IsUpper) || (token.All(char.IsLower) && char.IsDigit(next));
            if (plausible && KnownCodes.Contains(token))
            {
                return token.Equals("uk", StringComparison.OrdinalIgnoreCase) ? "gb" : token.ToLowerInvariant();
            }
        }
        if (CountryNameMap.TryGetValue(lower.Trim(), out var exact))
        {
            return exact;
        }
        return string.Empty;
    }

    #endregion RockBlack server row flags
}
