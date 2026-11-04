package com.v2ray.ang.ui.main

import com.v2ray.ang.dto.entities.ProfileItem
import com.v2ray.ang.enums.EConfigType
import java.util.Locale

/**
 * Server-row flags (DESIGN.md "Flags"): a leading flag emoji in the server name is used as the
 * row icon, otherwise the flag is derived from a country name or ISO code in the name,
 * otherwise the row shows a neutral globe.
 */
internal object ServerFlags {
    private const val REGIONAL_A = 0x1F1E6
    private const val REGIONAL_Z = 0x1F1FF

    /** Display name without the leading flag emoji, plus the flag (or null). */
    data class Parsed(val flag: String?, val name: String)

    fun parse(remarks: String): Parsed {
        val trimmed = remarks.trim()
        leadingFlag(trimmed)?.let { (flag, length) ->
            val rest = trimmed.substring(length).trimStart(' ', '\u00A0', '|', '-', '·', '•', '\uFE0F')
            return Parsed(flag, rest.ifEmpty { trimmed })
        }
        return Parsed(deriveFlag(trimmed), trimmed)
    }

    /** Leading regional-indicator pair (🇩🇪) and its length in UTF-16 chars. */
    private fun leadingFlag(text: String): Pair<String, Int>? {
        if (text.length < 4) return null
        val first = text.codePointAt(0)
        if (first !in REGIONAL_A..REGIONAL_Z) return null
        val secondIndex = Character.charCount(first)
        val second = text.codePointAt(secondIndex)
        if (second !in REGIONAL_A..REGIONAL_Z) return null
        val length = secondIndex + Character.charCount(second)
        return text.substring(0, length) to length
    }

    /** Converts an ISO 3166-1 alpha-2 code ("DE") to its flag emoji. */
    fun flagOf(iso: String): String? {
        val code = iso.uppercase(Locale.ROOT)
        if (code.length != 2 || code.any { it !in 'A'..'Z' }) return null
        val builder = StringBuilder()
        code.forEach { builder.appendCodePoint(REGIONAL_A + (it - 'A')) }
        return builder.toString()
    }

    fun deriveFlag(name: String): String? {
        val lower = name.lowercase(Locale.ROOT)
        val words = lower.split(WORD_SPLIT).filter { it.isNotEmpty() }
        // Country / city names (en, ru, de) first: "Germany", "Германия", "Frankfurt".
        for ((key, iso) in NAMES) {
            val match = if (key.contains(' ')) lower.contains(key) else words.any { it == key || (key.length >= 5 && it.startsWith(key)) }
            if (match) return flagOf(iso)
        }
        // Then ISO codes written in upper case as separate tokens: "NL-01", "[US]".
        name.split(WORD_SPLIT).forEach { token ->
            if (token.length == 2 && token.all { it in 'A'..'Z' } && token in ISO_CODES) return flagOf(token)
        }
        return null
    }

    /** "VLESS · Reality", "VMess · WS · TLS" — the protocol subtitle of a server row. */
    fun protocolSubtitle(profile: ProfileItem): String {
        val type = when (profile.configType) {
            EConfigType.VMESS -> "VMess"
            EConfigType.VLESS -> "VLESS"
            EConfigType.SHADOWSOCKS -> "Shadowsocks"
            EConfigType.TROJAN -> "Trojan"
            EConfigType.WIREGUARD -> "WireGuard"
            EConfigType.HYSTERIA2 -> "Hysteria2"
            EConfigType.SOCKS -> "SOCKS"
            EConfigType.HTTP -> "HTTP"
            EConfigType.CUSTOM -> "Custom"
            else -> profile.configType.name.lowercase(Locale.ROOT).replaceFirstChar { it.titlecase(Locale.ROOT) }
        }
        val parts = mutableListOf(type)
        profile.network?.takeIf { it.isNotBlank() && !it.equals("tcp", true) && !it.equals("raw", true) }?.let {
            parts += when (it.lowercase(Locale.ROOT)) {
                "ws" -> "WS"
                "grpc" -> "gRPC"
                "xhttp" -> "XHTTP"
                "httpupgrade" -> "HTTPUpgrade"
                "kcp" -> "mKCP"
                else -> it
            }
        }
        profile.security?.takeIf { it.isNotBlank() && !it.equals("none", true) && !it.equals("auto", true) }?.let {
            parts += when (it.lowercase(Locale.ROOT)) {
                "reality" -> "Reality"
                "tls" -> "TLS"
                else -> it
            }
        }
        return parts.joinToString(" · ")
    }

    private val WORD_SPLIT = Regex("[^\\p{L}\\p{N}]+")

    private val ISO_CODES = setOf(
        "AE", "AM", "AR", "AT", "AU", "AZ", "BE", "BG", "BR", "BY", "CA", "CH", "CL", "CN", "CY", "CZ",
        "DE", "DK", "EE", "ES", "FI", "FR", "GB", "GE", "GR", "HK", "HU", "ID", "IE", "IL", "IN", "IR",
        "IS", "IT", "JP", "KR", "KZ", "LT", "LU", "LV", "MD", "MX", "MY", "NL", "NO", "NZ", "PL", "PT",
        "RO", "RS", "RU", "SE", "SG", "SK", "TH", "TR", "TW", "UA", "UK", "US", "UZ", "VN", "ZA",
    )

    // Insertion order matters: multi-word names first, cities after countries.
    private val NAMES: List<Pair<String, String>> = listOf(
        "united states" to "US", "united kingdom" to "GB", "great britain" to "GB", "south korea" to "KR",
        "hong kong" to "HK", "new york" to "US", "los angeles" to "US", "san francisco" to "US",
        "south africa" to "ZA", "new zealand" to "NZ", "saudi arabia" to "SA", "czech republic" to "CZ",
        "usa" to "US", "america" to "US", "сша" to "US", "америка" to "US",
        "germany" to "DE", "deutschland" to "DE", "германия" to "DE",
        "netherlands" to "NL", "holland" to "NL", "niederlande" to "NL", "нидерланды" to "NL", "голландия" to "NL",
        "france" to "FR", "frankreich" to "FR", "франция" to "FR",
        "finland" to "FI", "finnland" to "FI", "финляндия" to "FI",
        "sweden" to "SE", "schweden" to "SE", "швеция" to "SE",
        "norway" to "NO", "norwegen" to "NO", "норвегия" to "NO",
        "denmark" to "DK", "dänemark" to "DK", "дания" to "DK",
        "poland" to "PL", "polen" to "PL", "польша" to "PL",
        "latvia" to "LV", "lettland" to "LV", "латвия" to "LV",
        "lithuania" to "LT", "litauen" to "LT", "литва" to "LT",
        "estonia" to "EE", "estland" to "EE", "эстония" to "EE",
        "moldova" to "MD", "moldau" to "MD", "молдова" to "MD", "молдавия" to "MD",
        "romania" to "RO", "rumänien" to "RO", "румыния" to "RO",
        "bulgaria" to "BG", "bulgarien" to "BG", "болгария" to "BG",
        "hungary" to "HU", "ungarn" to "HU", "венгрия" to "HU",
        "austria" to "AT", "österreich" to "AT", "австрия" to "AT",
        "switzerland" to "CH", "schweiz" to "CH", "швейцария" to "CH",
        "italy" to "IT", "italien" to "IT", "италия" to "IT",
        "spain" to "ES", "spanien" to "ES", "испания" to "ES",
        "portugal" to "PT", "португалия" to "PT",
        "belgium" to "BE", "belgien" to "BE", "бельгия" to "BE",
        "ireland" to "IE", "irland" to "IE", "ирландия" to "IE",
        "britain" to "GB", "england" to "GB", "великобритания" to "GB", "англия" to "GB",
        "czechia" to "CZ", "tschechien" to "CZ", "чехия" to "CZ",
        "slovakia" to "SK", "slowakei" to "SK", "словакия" to "SK",
        "serbia" to "RS", "serbien" to "RS", "сербия" to "RS",
        "greece" to "GR", "griechenland" to "GR", "греция" to "GR",
        "cyprus" to "CY", "zypern" to "CY", "кипр" to "CY",
        "turkey" to "TR", "türkiye" to "TR", "türkei" to "TR", "турция" to "TR",
        "russia" to "RU", "russland" to "RU", "россия" to "RU",
        "ukraine" to "UA", "украина" to "UA",
        "belarus" to "BY", "беларусь" to "BY",
        "kazakhstan" to "KZ", "kasachstan" to "KZ", "казахстан" to "KZ",
        "georgia" to "GE", "georgien" to "GE", "грузия" to "GE",
        "armenia" to "AM", "armenien" to "AM", "армения" to "AM",
        "azerbaijan" to "AZ", "aserbaidschan" to "AZ", "азербайджан" to "AZ",
        "uzbekistan" to "UZ", "usbekistan" to "UZ", "узбекистан" to "UZ",
        "israel" to "IL", "израиль" to "IL",
        "emirates" to "AE", "uae" to "AE", "dubai" to "AE", "оаэ" to "AE", "дубай" to "AE",
        "india" to "IN", "indien" to "IN", "индия" to "IN",
        "japan" to "JP", "япония" to "JP", "tokyo" to "JP", "токио" to "JP",
        "korea" to "KR", "корея" to "KR", "seoul" to "KR",
        "singapore" to "SG", "singapur" to "SG", "сингапур" to "SG",
        "taiwan" to "TW", "тайвань" to "TW",
        "china" to "CN", "китай" to "CN",
        "vietnam" to "VN", "вьетнам" to "VN",
        "thailand" to "TH", "таиланд" to "TH",
        "indonesia" to "ID", "indonesien" to "ID", "индонезия" to "ID",
        "malaysia" to "MY", "малайзия" to "MY",
        "australia" to "AU", "australien" to "AU", "австралия" to "AU",
        "canada" to "CA", "kanada" to "CA", "канада" to "CA",
        "mexico" to "MX", "mexiko" to "MX", "мексика" to "MX",
        "brazil" to "BR", "brasilien" to "BR", "бразилия" to "BR",
        "argentina" to "AR", "argentinien" to "AR", "аргентина" to "AR",
        "iceland" to "IS", "island" to "IS", "исландия" to "IS",
        "luxembourg" to "LU", "luxemburg" to "LU", "люксембург" to "LU",
        // Popular server cities.
        "amsterdam" to "NL", "амстердам" to "NL", "rotterdam" to "NL",
        "frankfurt" to "DE", "франкфурт" to "DE", "berlin" to "DE", "берлин" to "DE", "munich" to "DE", "münchen" to "DE",
        "nuremberg" to "DE", "nürnberg" to "DE", "falkenstein" to "DE",
        "paris" to "FR", "париж" to "FR",
        "london" to "GB", "лондон" to "GB",
        "helsinki" to "FI", "хельсинки" to "FI",
        "stockholm" to "SE", "стокгольм" to "SE",
        "warsaw" to "PL", "warschau" to "PL", "варшава" to "PL",
        "riga" to "LV", "рига" to "LV",
        "vilnius" to "LT", "вильнюс" to "LT",
        "tallinn" to "EE", "таллин" to "EE",
        "chisinau" to "MD", "кишинев" to "MD", "кишинёв" to "MD",
        "vienna" to "AT", "wien" to "AT", "вена" to "AT",
        "zurich" to "CH", "zürich" to "CH", "цюрих" to "CH",
        "milan" to "IT", "mailand" to "IT", "милан" to "IT",
        "madrid" to "ES", "мадрид" to "ES",
        "istanbul" to "TR", "стамбул" to "TR",
        "moscow" to "RU", "moskau" to "RU", "москва" to "RU",
        "seattle" to "US", "chicago" to "US", "dallas" to "US", "miami" to "US", "ashburn" to "US",
        "toronto" to "CA", "торонто" to "CA",
        "sydney" to "AU",
        "almaty" to "KZ", "алматы" to "KZ",
        "tbilisi" to "GE", "тбилиси" to "GE",
        "yerevan" to "AM", "ереван" to "AM",
    )
}
