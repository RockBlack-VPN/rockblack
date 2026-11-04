package com.v2ray.ang.handler

import com.v2ray.ang.AppConfig
import com.v2ray.ang.dto.entities.RulesetItem
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNotNull
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test
import java.util.Base64

class RoutingProfileManagerTest {

    private fun allValues(rules: List<RulesetItem>) = rules.flatMap { it.domain.orEmpty() + it.ip.orEmpty() }

    @Test
    fun presetsContainNoChinaRules() {
        RoutingProfileManager.Preset.entries.forEach { preset ->
            val rules = RoutingProfileManager.presetRules(preset)
            assertFalse(RoutingProfileManager.containsChinaRules(rules), preset.name)
            assertTrue(allValues(rules).none { it.endsWith(":cn") || it.contains("223.5.5.5") }, preset.name)
        }
    }

    @Test
    fun russiaPresetRoutesRussianTrafficDirectAndBlocksAds() {
        val rules = RoutingProfileManager.presetRules(RoutingProfileManager.Preset.RUSSIA)
        val direct = rules.filter { it.outboundTag == AppConfig.TAG_DIRECT }.let(::allValues)
        assertTrue(direct.containsAll(listOf("geosite:category-ru", "geoip:ru", "domain:ru", "domain:su", "domain:xn--p1ai")))
        assertTrue(direct.containsAll(listOf(AppConfig.GEOIP_PRIVATE, AppConfig.GEOSITE_PRIVATE)))
        val blocked = rules.filter { it.outboundTag == AppConfig.TAG_BLOCKED }.let(::allValues)
        assertEquals(listOf("geosite:category-ads-all"), blocked)
        assertEquals(AppConfig.TAG_PROXY, rules.last().outboundTag)
        assertEquals("0-65535", rules.last().port)
    }

    @Test
    fun directPresetEndsWithDirectRule() {
        val rules = RoutingProfileManager.presetRules(RoutingProfileManager.Preset.DIRECT)
        assertEquals(AppConfig.TAG_DIRECT, rules.last().outboundTag)
        assertTrue(rules.none { it.outboundTag == AppConfig.TAG_PROXY })
    }

    @Test
    fun signatureIgnoresIds() {
        val rules = RoutingProfileManager.presetRules(RoutingProfileManager.Preset.GLOBAL)
        val withIds = rules.mapIndexed { i, r -> r.copy(id = "id$i") }
        assertEquals(RoutingProfileManager.signature(rules), RoutingProfileManager.signature(withIds))
        val edited = withIds.toMutableList().apply { this[0] = this[0].copy(enabled = false) }
        assertFalse(RoutingProfileManager.signature(rules) == RoutingProfileManager.signature(edited))
    }

    @Test
    fun chinaRulesAreDetected() {
        val china = listOf(RulesetItem(outboundTag = "direct", domain = listOf("geosite:cn")))
        assertTrue(RoutingProfileManager.containsChinaRules(china))
    }

    private val happJson = """
        {"Name":"Panel RU","GlobalProxy":"true","DirectSites":["geosite:category-ru","domain:ya.ru"],
         "DirectIp":["geoip:ru"],"ProxySites":["domain:youtube.com"],"ProxyIp":[],
         "BlockSites":["geosite:category-ads-all"],"BlockIp":[],"DomainStrategy":"IPIfNonMatch"}
    """.trimIndent()

    @Test
    fun parsesHappLinksFromHeaderAndBody() {
        val b64 = Base64.getUrlEncoder().withoutPadding().encodeToString(happJson.toByteArray())
        val header = "happ://routing/onadd/$b64"
        val parsed = RoutingProfileManager.parseHappLink(header)
        assertNotNull(parsed)
        assertTrue(parsed!!.activate)
        assertEquals(happJson, parsed.json)

        val body = "vless://abc@host:443#A\nrouting: happ://routing/add/$b64\nvmess://xyz"
        val found = RoutingProfileManager.findHappLink(body)
        assertEquals("happ://routing/add/$b64", found)
        assertFalse(RoutingProfileManager.parseHappLink(found)!!.activate)

        assertNull(RoutingProfileManager.parseHappLink("happ://routing/remove/$b64"))
        assertNull(RoutingProfileManager.findHappLink("vless://abc@host:443"))
    }

    @Test
    fun mapsHappJsonToRulesets() {
        val profile = RoutingProfileManager.happJsonToProfile(happJson)
        assertNotNull(profile)
        profile!!
        assertEquals("Panel RU", profile.name)
        assertEquals("IPIfNonMatch", profile.domainStrategy)
        val tags = profile.rules.map { it.outboundTag }
        // block → private direct → proxy → direct → final proxy
        assertEquals(AppConfig.TAG_BLOCKED, tags.first())
        assertTrue(tags.indexOf(AppConfig.TAG_PROXY) < tags.lastIndexOf(AppConfig.TAG_DIRECT))
        assertEquals(AppConfig.TAG_PROXY, tags.last())
        assertTrue(allValues(profile.rules).containsAll(listOf("domain:ya.ru", "geoip:ru", "domain:youtube.com")))
    }

    @Test
    fun happNonGlobalProfileEndsDirect() {
        val profile = RoutingProfileManager.happJsonToProfile("""{"GlobalProxy":false,"ProxySites":["domain:x.com"]}""")
        assertEquals(AppConfig.TAG_DIRECT, profile!!.rules.last().outboundTag)
        assertNull(RoutingProfileManager.happJsonToProfile("not json"))
    }
}
