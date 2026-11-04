package com.v2ray.ang.enums

import androidx.core.os.LocaleListCompat
import java.util.Locale

/**
 * In-app languages. RockBlack ships English (default resources), Russian and German only;
 * any other device or stored locale falls back to [AUTO], which resolves to English resources
 * unless the system language is Russian or German.
 */
enum class Language(val code: String, val languageTag: String) {
    AUTO("auto", ""),
    ENGLISH("en", "en"),
    RUSSIAN("ru", "ru"),
    GERMAN("de", "de");

    val locale: Locale?
        get() = languageTag.takeIf { it.isNotEmpty() }?.let(Locale::forLanguageTag)

    fun toLocaleList(): LocaleListCompat =
        if (languageTag.isEmpty()) {
            LocaleListCompat.getEmptyLocaleList()
        } else {
            LocaleListCompat.forLanguageTags(languageTag)
        }

    companion object {
        /** Maps a stored preference value; legacy values (zh-rCN, fa, vi, ...) become [AUTO]. */
        fun fromCode(code: String): Language {
            return entries.find { it.code == code } ?: AUTO
        }

        /** Maps a BCP 47 tag; unsupported languages become [AUTO]. */
        fun fromLanguageTag(languageTag: String?): Language {
            if (languageTag.isNullOrBlank()) return AUTO

            return when (Locale.forLanguageTag(languageTag).language) {
                "en" -> ENGLISH
                "ru" -> RUSSIAN
                "de" -> GERMAN
                else -> AUTO
            }
        }

        /** True when [languageTag] is empty (follow system) or one of the shipped languages. */
        fun isSupportedTag(languageTag: String?): Boolean =
            languageTag.isNullOrBlank() || fromLanguageTag(languageTag) != AUTO
    }
}
