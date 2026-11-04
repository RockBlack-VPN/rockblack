package com.v2ray.ang.handler

/**
 * RockBlack UI 3.0 preference keys (see branding/DESIGN.md). Stored in the multi-process MMKV
 * settings storage like the upstream preferences.
 */
object RockBlackPrefs {
    /** Ping method: [PING_METHOD_PROXY_GET] (real delay through the core) or [PING_METHOD_TCP]. */
    const val PREF_PING_METHOD = "pref_rockblack_ping_method"
    const val PING_METHOD_PROXY_GET = "proxy_get"
    const val PING_METHOD_TCP = "tcp"

    /** Ping display: [PING_DISPLAY_TIME] ("42 ms") or [PING_DISPLAY_ICON] (status icon). */
    const val PREF_PING_DISPLAY = "pref_rockblack_ping_display"
    const val PING_DISPLAY_TIME = "time"
    const val PING_DISPLAY_ICON = "icon"

    /** Test every server once when the main screen opens. */
    const val PREF_PING_ON_LAUNCH = "pref_rockblack_ping_on_launch"

    /** Ids of the subscription cards the user collapsed on the main screen. */
    const val PREF_COLLAPSED_GROUPS = "pref_rockblack_collapsed_groups"

    /** Prefix of the per-group display order (`default`, `ping`, `name`). */
    const val PREF_GROUP_SORT_PREFIX = "pref_rockblack_group_sort_"
    const val SORT_DEFAULT = "default"
    const val SORT_PING = "ping"
    const val SORT_NAME = "name"

    /** Epoch millis of the last successful core start; written by the daemon process. */
    const val PREF_CONNECTED_SINCE = "pref_rockblack_connected_since"

    fun pingMethod(): String =
        MmkvManager.decodeSettingsString(PREF_PING_METHOD, PING_METHOD_PROXY_GET) ?: PING_METHOD_PROXY_GET

    fun pingDisplay(): String =
        MmkvManager.decodeSettingsString(PREF_PING_DISPLAY, PING_DISPLAY_TIME) ?: PING_DISPLAY_TIME

    fun pingOnLaunch(): Boolean = MmkvManager.decodeSettingsBool(PREF_PING_ON_LAUNCH, false)

    fun collapsedGroups(): Set<String> =
        MmkvManager.decodeSettingsStringSet(PREF_COLLAPSED_GROUPS)?.toSet().orEmpty()

    fun setCollapsedGroups(ids: Set<String>) {
        MmkvManager.encodeSettings(PREF_COLLAPSED_GROUPS, ids.toMutableSet())
    }

    fun groupSort(groupId: String): String =
        MmkvManager.decodeSettingsString(PREF_GROUP_SORT_PREFIX + groupId, SORT_DEFAULT) ?: SORT_DEFAULT

    fun setGroupSort(groupId: String, sort: String) {
        MmkvManager.encodeSettings(PREF_GROUP_SORT_PREFIX + groupId, sort)
    }

    fun connectedSince(): Long = MmkvManager.decodeSettingsString(PREF_CONNECTED_SINCE)?.toLongOrNull() ?: 0L

    fun markConnected(nowMillis: Long = System.currentTimeMillis()) {
        MmkvManager.encodeSettings(PREF_CONNECTED_SINCE, nowMillis.toString())
    }
}
