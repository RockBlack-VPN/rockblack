package com.v2ray.ang.ui.main

import com.v2ray.ang.dto.entities.ProfileItem
import com.v2ray.ang.enums.EConfigType
import com.v2ray.ang.handler.RockBlackPrefs
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Test

class ServerFlagsTest {

    @Test
    fun leadingFlagEmojiIsSplitOff() {
        val parsed = ServerFlags.parse("🇩🇪 Germany - 01")
        assertEquals("🇩🇪", parsed.flag)
        assertEquals("Germany - 01", parsed.name)
    }

    @Test
    fun flagIsDerivedFromCountryCityOrIsoCode() {
        assertEquals("🇳🇱", ServerFlags.parse("Netherlands").flag)
        assertEquals("🇩🇪", ServerFlags.parse("Германия #2").flag)
        assertEquals("🇫🇮", ServerFlags.parse("Helsinki fast").flag)
        assertEquals("🇺🇸", ServerFlags.parse("Node US-03").flag)
        assertEquals("🇬🇧", ServerFlags.parse("United Kingdom").flag)
    }

    @Test
    fun unknownNamesGetNoFlag() {
        assertNull(ServerFlags.parse("Home router").flag)
        assertNull(ServerFlags.parse("VLESS TLS WS").flag)
    }

    @Test
    fun protocolSubtitle() {
        val reality = ProfileItem(configType = EConfigType.VLESS).apply { network = "tcp"; security = "reality" }
        assertEquals("VLESS · Reality", ServerFlags.protocolSubtitle(reality))
        val ws = ProfileItem(configType = EConfigType.VMESS).apply { network = "ws"; security = "tls" }
        assertEquals("VMess · WS · TLS", ServerFlags.protocolSubtitle(ws))
    }

    @Test
    fun expiryBannerOnlyWithinThreeDays() {
        val now = 1_000_000_000_000L
        val day = 86_400_000L
        assertNull(expiryBannerDays(null, now))
        assertNull(expiryBannerDays(now + 10 * day, now))
        assertEquals(3, expiryBannerDays(now + 3 * day, now))
        assertEquals(0, expiryBannerDays(now + day / 2, now))
        assertEquals(-1, expiryBannerDays(now - 1, now))
    }

    private fun row(guid: String, name: String, delay: Long) = ServerRowUiModel(
        guid = guid,
        profile = ProfileItem(configType = EConfigType.VLESS, remarks = name),
        remarks = name,
        statistics = "",
        typeDescription = "VLESS",
        testDelayMillis = delay,
        subscriptionBadge = "",
    )

    @Test
    fun sortByPingPutsUntestedAndTimeoutsLast() {
        val rows = listOf(row("a", "A", 0), row("b", "B", 300), row("c", "C", -1), row("d", "D", 40))
        assertEquals(listOf("d", "b", "c", "a"), sortRows(rows, RockBlackPrefs.SORT_PING).map { it.guid })
        assertEquals(listOf("a", "b", "c", "d"), sortRows(rows.reversed(), RockBlackPrefs.SORT_NAME).map { it.guid })
        assertEquals(rows, sortRows(rows, RockBlackPrefs.SORT_DEFAULT))
    }

    @Test
    fun sessionTimerFormat() {
        assertEquals("00:16:28", formatSessionTimer(988_000L))
        assertEquals("27:00:01", formatSessionTimer(97_201_000L))
    }
}
