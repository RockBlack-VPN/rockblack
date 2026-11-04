package com.v2ray.ang.ui.main

import com.v2ray.ang.dto.ConnectionTestResult
import com.v2ray.ang.dto.GroupMapItem
import com.v2ray.ang.dto.LocateTarget

/** Locale-neutral state formatted only when it reaches the main UI. */
sealed interface MainStatus {
    data object Disconnected : MainStatus
    data object Connected : MainStatus
    data object Testing : MainStatus
    data class TestProgress(val progress: String) : MainStatus
    data class ConnectionTest(val result: ConnectionTestResult) : MainStatus
}

/**
 * Main UI state
 */
data class MainUiState(
    val groups: List<GroupMapItem> = emptyList(),
    val selectedGroupId: String = "",
    val selectedGuid: String? = null,
    val isRunning: Boolean = false,
    val isTesting: Boolean = false,
    val status: MainStatus = MainStatus.Disconnected,
    val locateTarget: LocateTarget? = null,
    val confirmRemove: Boolean = false,
    val doubleColumnDisplay: Boolean = false,
    val shareQRCodeBitmap: android.graphics.Bitmap? = null,
    // ---------- RockBlack UI 3.0 ----------
    /** Start requested, waiting for the service to report success or failure. */
    val isConnecting: Boolean = false,
    /** Group whose servers are being pinged ([PING_ALL_GROUPS] for every group), null when idle. */
    val pingingGroupId: String? = null,
    /** Servers of a single-server ping that has not reported yet. */
    val pingingGuids: Set<String> = emptySet(),
    val collapsedGroups: Set<String> = emptySet(),
    /** Display order per group id (RockBlackPrefs.SORT_*). */
    val groupSorts: Map<String, String> = emptyMap(),
    val pingDisplayIcon: Boolean = false,
    val routingProfileId: String = "",
    val routingProfiles: List<RoutingProfileUi> = emptyList(),
    val vpnMode: Boolean = true,
)

/** Entry of the routing profile sheet. */
data class RoutingProfileUi(
    val id: String,
    /** Display name for imported profiles; built-in names are resolved from resources. */
    val name: String? = null,
    val imported: Boolean = false,
)

const val PING_ALL_GROUPS = "\u0000all"

/**
 * All possible user interaction intents
 */
sealed interface MainAction {
    data object Initialize : MainAction
    data object RefreshGroups : MainAction
    data object ToggleService : MainAction
    data object TestCurrentServer : MainAction
    data object TestAllServers : MainAction
    data object TestRealAllServers : MainAction
    data object CancelTesting : MainAction
    data object RemoveAllServers : MainAction
    data object RemoveDuplicateServers : MainAction
    data object RemoveInvalidServers : MainAction
    data object SortByTestResults : MainAction
    data object UpdateSubscriptions : MainAction
    data class UpdateGroupSubscription(val groupId: String) : MainAction
    data object ExportAll : MainAction

    data object ImportQRcode : MainAction
    data object ImportClipboard : MainAction
    data object ImportConfigLocal : MainAction
    data class ImportManually(val type: Int) : MainAction
    data object RestartService : MainAction
    data object LocateSelectedServer : MainAction

    data class SelectGroup(val groupId: String) : MainAction
    data class SelectServer(val guid: String) : MainAction
    data class RemoveServer(val guid: String) : MainAction
    data class EditServer(val guid: String, val profile: com.v2ray.ang.dto.entities.ProfileItem) : MainAction
    data class Search(val query: String) : MainAction
    data class ShareQRCode(val guid: String) : MainAction
    data class ShareClipboard(val guid: String) : MainAction
    data class ShareFullContent(val guid: String) : MainAction
    data object DismissQRCodeDialog : MainAction
    data class OpenSubscriptionLink(val url: String) : MainAction

    data class ImportBatchConfig(val configText: String) : MainAction

    data object LocateHandled : MainAction

    // ---------- RockBlack UI 3.0 ----------
    data class PingGroup(val groupId: String) : MainAction
    data class PingServer(val guid: String) : MainAction
    data class ToggleGroupCollapsed(val groupId: String) : MainAction
    data class SetGroupSort(val groupId: String, val sort: String) : MainAction
    data class SelectFastest(val groupId: String) : MainAction
    data class EditSubscription(val groupId: String) : MainAction
    data class DeleteSubscription(val groupId: String) : MainAction
    data class DeleteGroupServers(val groupId: String) : MainAction
    data class SelectRoutingProfile(val profileId: String) : MainAction
    data class DeleteRoutingProfile(val profileId: String) : MainAction
    data object OpenRoutingEditor : MainAction
    data class SetVpnMode(val vpn: Boolean) : MainAction
}
