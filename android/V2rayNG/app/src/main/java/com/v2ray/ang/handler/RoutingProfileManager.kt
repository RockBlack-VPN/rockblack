package com.v2ray.ang.handler

import com.google.gson.JsonElement
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import com.v2ray.ang.AppConfig
import com.v2ray.ang.dto.entities.RulesetItem
import com.v2ray.ang.util.JsonUtil
import com.v2ray.ang.util.LogUtil
import java.util.Base64
import java.util.Locale
import java.util.UUID

/**
 * RockBlack routing profiles (branding/DESIGN.md "Routing").
 *
 * A profile is a complete v2rayNG routing ruleset list. Selecting a profile replaces the
 * upstream ruleset list (locked rules are kept, exactly like the upstream "import predefined
 * rulesets" action), so the core config builder and the upstream routing editor keep working
 * unchanged. Built-in presets live in code; profiles imported from a subscription
 * (`routing: happ://routing/add/{base64}`) are stored as named custom profiles.
 */
object RoutingProfileManager {
    const val PREF_ACTIVE_PROFILE = "pref_rockblack_routing_profile"
    const val PREF_ACTIVE_SIGNATURE = "pref_rockblack_routing_signature"
    const val PREF_CUSTOM_PROFILES = "pref_rockblack_routing_custom_profiles"

    /** The rules currently in the upstream routing editor that match no profile. */
    const val PROFILE_CUSTOM = "custom"
    const val IMPORTED_PREFIX = "imported:"

    private const val HAPP_ROUTING_PREFIX = "happ://routing/"
    private val DOMAIN_STRATEGIES = setOf("AsIs", "IPIfNonMatch", "IPOnDemand")

    // geosite/geoip tags below were verified to exist in the geosite.dat / geoip.dat bundled
    // with libv2ray.aar (category-ru, category-ads-all, private; geoip ru, private).
    const val GEOSITE_ADS = "geosite:category-ads-all"
    const val GEOSITE_RU = "geosite:category-ru"
    const val GEOIP_RU = "geoip:ru"

    enum class Preset(val id: String) {
        GLOBAL("global"),
        RUSSIA("russia"),
        BLOCK_ADS("block_ads"),
        DIRECT("direct");

        companion object {
            fun fromId(id: String?): Preset? = entries.firstOrNull { it.id == id }
        }
    }

    val DEFAULT_PRESET = Preset.RUSSIA

    /** A profile imported from a subscription or a `happ://routing/...` link. */
    data class CustomProfile(
        val id: String = "",
        val name: String = "",
        val rules: List<RulesetItem> = emptyList(),
        val domainStrategy: String? = null,
    )

    data class HappLink(val activate: Boolean, val json: String)

    // ---------- Presets ----------

    private fun rule(remarks: String, outbound: String, domain: List<String>? = null, ip: List<String>? = null, port: String? = null) =
        RulesetItem(remarks = remarks, outboundTag = outbound, domain = domain, ip = ip, port = port)

    private fun privateDirect() = listOf(
        rule("Direct LAN IP", AppConfig.TAG_DIRECT, ip = listOf(AppConfig.GEOIP_PRIVATE)),
        rule("Direct LAN domains", AppConfig.TAG_DIRECT, domain = listOf(AppConfig.GEOSITE_PRIVATE)),
    )

    private fun blockAds() = rule("Block ads", AppConfig.TAG_BLOCKED, domain = listOf(GEOSITE_ADS))

    private fun finalRule(outbound: String) =
        rule(if (outbound == AppConfig.TAG_DIRECT) "Final direct" else "Final proxy", outbound, port = "0-65535")

    fun presetRules(preset: Preset): List<RulesetItem> = when (preset) {
        Preset.GLOBAL -> privateDirect() + finalRule(AppConfig.TAG_PROXY)
        Preset.RUSSIA -> listOf(blockAds()) + privateDirect() + listOf(
            rule(
                "Direct Russian domains", AppConfig.TAG_DIRECT,
                domain = listOf(GEOSITE_RU, "domain:ru", "domain:su", "domain:xn--p1ai")
            ),
            rule("Direct Russian IPs", AppConfig.TAG_DIRECT, ip = listOf(GEOIP_RU)),
            finalRule(AppConfig.TAG_PROXY),
        )

        Preset.BLOCK_ADS -> listOf(blockAds()) + privateDirect() + finalRule(AppConfig.TAG_PROXY)
        Preset.DIRECT -> privateDirect() + finalRule(AppConfig.TAG_DIRECT)
    }

    // ---------- Signatures (detect edits made in the routing editor) ----------

    /** Identity of a ruleset list that ignores the ids the routing editor assigns lazily. */
    fun signature(rules: List<RulesetItem>): String =
        JsonUtil.toJson(rules.filter { it.locked != true }.map { it.copy(id = "", locked = false) })

    // ---------- Happ routing import ----------

    /** Finds a `happ://routing/(add|onadd)/{base64}` link in a header value or a subscription body. */
    fun findHappLink(text: String?): String? {
        if (text.isNullOrBlank()) return null
        return text.lineSequence()
            .map { it.trim().removePrefix("routing:").trim() }
            .firstOrNull { it.startsWith(HAPP_ROUTING_PREFIX, ignoreCase = true) }
    }

    fun parseHappLink(link: String?): HappLink? {
        val value = link?.trim() ?: return null
        if (!value.startsWith(HAPP_ROUTING_PREFIX, ignoreCase = true)) return null
        val rest = value.substring(HAPP_ROUTING_PREFIX.length)
        val slash = rest.indexOf('/')
        if (slash <= 0) return null
        val action = rest.substring(0, slash).lowercase(Locale.ROOT)
        if (action != "add" && action != "onadd") return null
        val payload = rest.substring(slash + 1).substringBefore('?').substringBefore('#')
        val json = decodeBase64(payload) ?: return null
        return HappLink(activate = action == "onadd", json = json)
    }

    private fun decodeBase64(raw: String): String? {
        val cleaned = java.net.URLDecoder.decode(raw, "UTF-8").trim()
            .replace('-', '+').replace('_', '/').replace("\n", "").replace("\r", "")
        if (cleaned.isEmpty()) return null
        val padded = cleaned + "=".repeat((4 - cleaned.length % 4) % 4)
        return runCatching { String(Base64.getDecoder().decode(padded), Charsets.UTF_8) }.getOrNull()
    }

    /** Maps a Happ routing JSON profile to v2rayNG rulesets (block → proxy → direct → final). */
    fun happJsonToProfile(json: String, fallbackName: String = "Imported"): CustomProfile? {
        val obj = runCatching { JsonParser.parseString(json) }.getOrNull()
            ?.takeIf { it.isJsonObject }?.asJsonObject ?: return null

        val blockSites = obj.stringList("BlockSites")
        val blockIp = obj.stringList("BlockIp")
        val proxySites = obj.stringList("ProxySites")
        val proxyIp = obj.stringList("ProxyIp")
        val directSites = obj.stringList("DirectSites")
        val directIp = obj.stringList("DirectIp")
        val globalProxy = obj.boolValue("GlobalProxy") ?: true

        if (listOf(blockSites, blockIp, proxySites, proxyIp, directSites, directIp).all { it.isEmpty() } &&
            !obj.has("GlobalProxy")
        ) return null

        val rules = mutableListOf<RulesetItem>()
        if (blockSites.isNotEmpty()) rules += rule("Block sites", AppConfig.TAG_BLOCKED, domain = blockSites)
        if (blockIp.isNotEmpty()) rules += rule("Block IPs", AppConfig.TAG_BLOCKED, ip = blockIp)
        rules += privateDirect()
        if (proxySites.isNotEmpty()) rules += rule("Proxy sites", AppConfig.TAG_PROXY, domain = proxySites)
        if (proxyIp.isNotEmpty()) rules += rule("Proxy IPs", AppConfig.TAG_PROXY, ip = proxyIp)
        if (directSites.isNotEmpty()) rules += rule("Direct sites", AppConfig.TAG_DIRECT, domain = directSites)
        if (directIp.isNotEmpty()) rules += rule("Direct IPs", AppConfig.TAG_DIRECT, ip = directIp)
        rules += finalRule(if (globalProxy) AppConfig.TAG_PROXY else AppConfig.TAG_DIRECT)

        val name = obj.stringValue("Name")?.trim()?.take(60)?.ifEmpty { null } ?: fallbackName
        val strategy = obj.stringValue("DomainStrategy")?.takeIf { it in DOMAIN_STRATEGIES }
        return CustomProfile(id = "", name = name, rules = rules, domainStrategy = strategy)
    }

    private fun JsonObject.stringList(key: String): List<String> {
        val element: JsonElement = get(key) ?: return emptyList()
        val values = when {
            element.isJsonArray -> element.asJsonArray.mapNotNull { e ->
                e.takeIf { it.isJsonPrimitive }?.asString
            }

            element.isJsonPrimitive -> element.asString.split(',', '\n')
            else -> emptyList()
        }
        return values.map { it.trim() }.filter { it.isNotEmpty() }.distinct()
    }

    private fun JsonObject.stringValue(key: String): String? =
        get(key)?.takeIf { it.isJsonPrimitive }?.asString

    private fun JsonObject.boolValue(key: String): Boolean? {
        val element = get(key)?.takeIf { it.isJsonPrimitive } ?: return null
        val primitive = element.asJsonPrimitive
        return if (primitive.isBoolean) primitive.asBoolean else primitive.asString.trim().equals("true", ignoreCase = true)
    }

    // ---------- Storage ----------

    fun customProfiles(): List<CustomProfile> {
        val raw = MmkvManager.decodeSettingsString(PREF_CUSTOM_PROFILES)
        if (raw.isNullOrBlank()) return emptyList()
        return JsonUtil.fromJsonSafe(raw, Array<CustomProfile>::class.java)?.toList().orEmpty()
    }

    private fun saveCustomProfiles(profiles: List<CustomProfile>) {
        MmkvManager.encodeSettings(PREF_CUSTOM_PROFILES, JsonUtil.toJson(profiles))
    }

    /** Id of the selected profile; [PROFILE_CUSTOM] when the rules were edited by hand afterwards. */
    fun activeProfileId(): String {
        val stored = MmkvManager.decodeSettingsString(PREF_ACTIVE_PROFILE) ?: return PROFILE_CUSTOM
        val storedSignature = MmkvManager.decodeSettingsString(PREF_ACTIVE_SIGNATURE)
        val current = signature(MmkvManager.decodeRoutingRulesets().orEmpty())
        return if (storedSignature == current) stored else PROFILE_CUSTOM
    }

    fun rulesFor(profileId: String): List<RulesetItem>? {
        Preset.fromId(profileId)?.let { return presetRules(it) }
        if (profileId.startsWith(IMPORTED_PREFIX)) {
            val id = profileId.removePrefix(IMPORTED_PREFIX)
            return customProfiles().firstOrNull { it.id == id }?.rules
        }
        return null
    }

    /** Replaces the routing rulesets with [profileId]; returns false for unknown profiles. */
    fun applyProfile(profileId: String): Boolean {
        val rules = rulesFor(profileId) ?: return false
        val locked = MmkvManager.decodeRoutingRulesets().orEmpty().filter { it.locked == true }
        val fresh = rules.map { it.copy(id = UUID.randomUUID().toString()) }
        val list = (locked + fresh).toMutableList()
        MmkvManager.encodeRoutingRulesets(list)
        if (profileId.startsWith(IMPORTED_PREFIX)) {
            customProfiles().firstOrNull { IMPORTED_PREFIX + it.id == profileId }?.domainStrategy?.let {
                MmkvManager.encodeSettings(AppConfig.PREF_ROUTING_DOMAIN_STRATEGY, it)
            }
        }
        MmkvManager.encodeSettings(PREF_ACTIVE_PROFILE, profileId)
        MmkvManager.encodeSettings(PREF_ACTIVE_SIGNATURE, signature(list))
        return true
    }

    /**
     * Stores the Happ profile of [link] (replacing a profile with the same name) and activates it
     * for `onadd` links or when [forceActivate] is set.
     * @return the stored profile, or null when the link is not a valid Happ routing profile.
     */
    fun importHappLink(
        link: String,
        forceActivate: Boolean = false,
        onRoutingChanged: () -> Unit = {},
    ): CustomProfile? {
        val parsed = parseHappLink(link) ?: return null
        val profile = happJsonToProfile(parsed.json) ?: return null
        return try {
            val existing = customProfiles()
            val previous = existing.firstOrNull { it.name == profile.name }
            val stored = profile.copy(id = previous?.id ?: UUID.randomUUID().toString())
            saveCustomProfiles(existing.filterNot { it.id == stored.id } + stored)
            val activeId = IMPORTED_PREFIX + stored.id
            if (parsed.activate || forceActivate || MmkvManager.decodeSettingsString(PREF_ACTIVE_PROFILE) == activeId) {
                val before = signature(MmkvManager.decodeRoutingRulesets().orEmpty())
                applyProfile(activeId)
                // Lets the caller reconnect a running tunnel, only when the rules really changed.
                if (signature(MmkvManager.decodeRoutingRulesets().orEmpty()) != before) onRoutingChanged()
            }
            stored
        } catch (e: Exception) {
            LogUtil.e(AppConfig.TAG, "Failed to import routing profile", e)
            null
        }
    }

    fun deleteCustomProfile(id: String) {
        saveCustomProfiles(customProfiles().filterNot { it.id == id })
    }

    /** True for the upstream China presets (geosite:cn / geoip:cn rules). */
    fun containsChinaRules(rules: List<RulesetItem>): Boolean = rules.any { rule ->
        rule.domain.orEmpty().any { it == AppConfig.GEOSITE_CN || it.startsWith("geosite:geolocation-cn") } ||
                rule.ip.orEmpty().any { it == AppConfig.GEOIP_CN }
    }

    /**
     * First start of UI 3.0: fresh installs and the upstream China presets get the Russia
     * profile; other hand-made rules are kept as the "Custom" profile.
     */
    fun ensureDefaultProfile() {
        if (MmkvManager.decodeSettingsString(PREF_ACTIVE_PROFILE) != null) return
        val current = MmkvManager.decodeRoutingRulesets().orEmpty()
        if (current.isEmpty() || containsChinaRules(current)) {
            applyProfile(DEFAULT_PRESET.id)
        } else {
            MmkvManager.encodeSettings(PREF_ACTIVE_PROFILE, PROFILE_CUSTOM)
        }
    }
}
