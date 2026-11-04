package com.v2ray.ang.dto

import com.v2ray.ang.dto.entities.SubscriptionUsageInfo

data class GroupMapItem(
    var id: String,
    var remarks: String,
    val usageInfo: SubscriptionUsageInfo? = null,
    /** RockBlack UI 3.0: epoch millis of the last successful update, -1 when never updated. */
    val lastUpdated: Long = -1,
    /** Auto-update interval in minutes, null when auto-update is off. */
    val autoUpdateMinutes: Long? = null,
    /** True when the group has a subscription URL (it can be refreshed). */
    val hasUrl: Boolean = false,
)
