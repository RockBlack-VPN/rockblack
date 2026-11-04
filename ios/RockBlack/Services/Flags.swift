import Foundation

/// Flag for a server row: a leading flag emoji in the name, else a country
/// derived from a name or ISO code in the name, else nil (globe icon).
enum Flags {
    static func flag(for name: String) -> String? {
        if let emoji = leadingFlag(name) { return emoji }
        if let code = countryCode(in: name) { return emoji(for: code) }
        return nil
    }

    /// Name without a leading flag emoji (the flag is shown as the row icon).
    static func strippedName(_ name: String) -> String {
        guard leadingFlag(name) != nil else { return name }
        let rest = name.unicodeScalars.dropFirst(2)
        let text = String(String.UnicodeScalarView(rest)).trimmingCharacters(in: .whitespaces)
        return text.isEmpty ? name : text
    }

    static func leadingFlag(_ name: String) -> String? {
        let scalars = Array(name.trimmingCharacters(in: .whitespaces).unicodeScalars.prefix(2))
        guard scalars.count == 2, scalars.allSatisfy({ (0x1F1E6...0x1F1FF).contains($0.value) }) else { return nil }
        var view = String.UnicodeScalarView()
        view.append(contentsOf: scalars)
        return String(view)
    }

    static func emoji(for code: String) -> String? {
        let upper = code.uppercased()
        guard upper.count == 2, upper.unicodeScalars.allSatisfy({ (65...90).contains($0.value) }) else { return nil }
        var view = String.UnicodeScalarView()
        for scalar in upper.unicodeScalars {
            if let flag = Unicode.Scalar(0x1F1E6 + scalar.value - 65) { view.append(flag) }
        }
        return String(view)
    }

    static func countryCode(in name: String) -> String? {
        let lower = name.lowercased()
        for (code, words) in names {
            if words.contains(where: { lower.contains($0) }) { return code }
        }
        // Standalone ISO codes such as "DE-1", "[NL] Amsterdam", "us 02".
        let tokens = name.components(separatedBy: CharacterSet.alphanumerics.inverted).filter { $0.count == 2 }
        for token in tokens where token == token.uppercased() {
            if isoCodes.contains(token) { return token }
        }
        return nil
    }

    static let isoCodes: Set<String> = Set(names.map { $0.0 })

    /// English / Russian / German country names (lower-case substrings).
    static let names: [(String, [String])] = [
        ("DE", ["germany", "германия", "deutschland", "frankfurt", "франкфурт", "berlin"]),
        ("NL", ["netherlands", "нидерланды", "голландия", "niederlande", "amsterdam", "амстердам"]),
        ("RU", ["russia", "россия", "russland", "moscow", "москва"]),
        ("US", ["united states", "usa", "сша", "america", "new york", "los angeles", "seattle"]),
        ("GB", ["united kingdom", "великобритания", "england", "london", "лондон", "großbritannien"]),
        ("FR", ["france", "франция", "frankreich", "paris", "париж"]),
        ("FI", ["finland", "финляндия", "finnland", "helsinki"]),
        ("SE", ["sweden", "швеция", "schweden", "stockholm"]),
        ("NO", ["norway", "норвегия", "norwegen"]),
        ("PL", ["poland", "польша", "polen", "warsaw"]),
        ("LV", ["latvia", "латвия", "lettland", "riga"]),
        ("LT", ["lithuania", "литва", "litauen", "vilnius"]),
        ("EE", ["estonia", "эстония", "estland", "tallinn"]),
        ("MD", ["moldova", "молдова", "moldau", "chisinau"]),
        ("KZ", ["kazakhstan", "казахстан", "kasachstan", "almaty"]),
        ("TR", ["turkey", "турция", "türkei", "istanbul", "стамбул"]),
        ("AE", ["emirates", "эмираты", "dubai", "дубай", "uae", "оаэ"]),
        ("JP", ["japan", "япония", "tokyo", "токио"]),
        ("SG", ["singapore", "сингапур", "singapur"]),
        ("HK", ["hong kong", "гонконг", "hongkong"]),
        ("CH", ["switzerland", "швейцария", "schweiz", "zurich", "zürich"]),
        ("AT", ["austria", "австрия", "österreich", "vienna", "wien"]),
        ("IT", ["italy", "италия", "italien", "milan", "milano"]),
        ("ES", ["spain", "испания", "spanien", "madrid"]),
        ("CZ", ["czech", "чехия", "tschechien", "prague", "prag"]),
        ("CA", ["canada", "канада", "kanada", "toronto"]),
        ("UA", ["ukraine", "украина"]),
        ("BY", ["belarus", "беларусь", "weißrussland"]),
        ("GE", ["georgia", "грузия", "georgien", "tbilisi"]),
        ("AM", ["armenia", "армения", "armenien", "yerevan"]),
        ("RS", ["serbia", "сербия", "serbien", "belgrade"]),
        ("BG", ["bulgaria", "болгария", "bulgarien", "sofia"]),
        ("RO", ["romania", "румыния", "rumänien", "bucharest"]),
        ("HU", ["hungary", "венгрия", "ungarn", "budapest"]),
        ("IN", ["india", "индия", "indien", "mumbai"]),
        ("KR", ["korea", "корея", "seoul"]),
        ("BR", ["brazil", "бразилия", "brasilien", "são paulo", "sao paulo"]),
        ("AU", ["australia", "австралия", "australien", "sydney"]),
        ("IL", ["israel", "израиль"]),
        ("IE", ["ireland", "ирландия", "irland", "dublin"]),
        ("BE", ["belgium", "бельгия", "belgien", "brussels"]),
        ("DK", ["denmark", "дания", "dänemark", "copenhagen"]),
        ("PT", ["portugal", "португалия", "lisbon"]),
        ("GR", ["greece", "греция", "griechenland", "athens"]),
        ("CY", ["cyprus", "кипр", "zypern"]),
    ]
}
