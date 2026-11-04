package com.v2ray.ang.ui.main

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.asPaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBars
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListState
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalNavigationDrawer
import androidx.compose.material3.Text
import androidx.compose.material3.rememberDrawerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.v2ray.ang.AppConfig
import com.v2ray.ang.R
import com.v2ray.ang.handler.RockBlackPrefs
import com.v2ray.ang.ui.compose.DeleteConfirmDialog
import com.v2ray.ang.ui.compose.QRCodeDialog
import com.v2ray.ang.ui.compose.RockBlackTheme
import com.v2ray.ang.ui.compose.verticalScrollbar
import kotlinx.coroutines.launch

@Composable
fun MainScreen(
    mainViewModel: MainViewModel,
    onAction: (MainAction) -> Unit,
    onNavigate: (MainDestination) -> Unit,
) {
    val uiState by mainViewModel.uiState.collectAsStateWithLifecycle()
    val groupStates by mainViewModel.allGroupStates.collectAsStateWithLifecycle()
    val isLoading by mainViewModel.isLoading.collectAsStateWithLifecycle()
    val isRunning = uiState.isRunning
    val confirmRemove = uiState.confirmRemove
    val shareQRCodeBitmap = uiState.shareQRCodeBitmap

    val drawerState = rememberDrawerState(DrawerValue.Closed)
    val scope = rememberCoroutineScope()
    var showSearch by remember { mutableStateOf(false) }
    var searchQuery by remember { mutableStateOf("") }
    var showRoutingSheet by remember { mutableStateOf(false) }
    var showDelAllConfirm by remember { mutableStateOf(false) }
    var showDelDuplicateConfirm by remember { mutableStateOf(false) }
    var showDelInvalidConfirm by remember { mutableStateOf(false) }
    var deleteSubscriptionTarget by remember { mutableStateOf<String?>(null) }
    var deleteServersTarget by remember { mutableStateOf<String?>(null) }
    var showRemoveConfirm by rememberSaveable(stateSaver = ServerDeleteTarget.Saver) {
        mutableStateOf<ServerDeleteTarget?>(null)
    }
    val removeServer: (String, String) -> Unit = { guid, profileName ->
        if (confirmRemove) {
            showRemoveConfirm = ServerDeleteTarget(guid, profileName)
        } else {
            onAction(MainAction.RemoveServer(guid))
        }
    }

    // ---------- Live session ----------
    val live = rememberLiveSession(isRunning) { RockBlackPrefs.connectedSince() }
    val statusText = when (val status = uiState.status) {
        is MainStatus.ConnectionTest -> mainViewModel.formatStatus(status)
        MainStatus.Testing -> if (uiState.pingingGroupId == null) mainViewModel.formatStatus(status) else null
        else -> null
    }
    val power = PowerSectionUi(
        state = when {
            isRunning -> PowerState.Connected
            uiState.isConnecting -> PowerState.Connecting
            else -> PowerState.Disconnected
        },
        timer = live.timer,
        downSpeed = live.down,
        upSpeed = live.up,
        statusText = statusText,
    )

    // ---------- Cards ----------
    val serverListTitle = stringResource(R.string.rb_server_list)
    val autoTemplate = stringResource(R.string.rb_auto_update_hours, 999)
    val now = remember(uiState.groups) { System.currentTimeMillis() }
    val cards = uiState.groups.mapNotNull { group ->
        val rows = groupStates[group.id]?.rows.orEmpty()
        val isServerList = group.id == AppConfig.DEFAULT_SUBSCRIPTION_ID
        // The manual list only shows up when it has servers (or is the only group).
        if (isServerList && rows.isEmpty() && uiState.groups.size > 1) return@mapNotNull null
        if (searchQuery.isNotBlank() && rows.isEmpty()) return@mapNotNull null
        buildGroupCard(
            group = group,
            rows = rows,
            isServerList = isServerList,
            serverListTitle = serverListTitle,
            collapsed = group.id in uiState.collapsedGroups && searchQuery.isBlank(),
            pinging = uiState.pingingGroupId == group.id || uiState.pingingGroupId == PING_ALL_GROUPS ||
                    (uiState.pingingGroupId != null && rows.any { it.guid in uiState.pingingGuids }),
            sort = uiState.groupSorts[group.id] ?: RockBlackPrefs.SORT_DEFAULT,
            nowMillis = now,
            autoText = { hours -> autoTemplate.replace("999", hours.toString()) },
        )
    }

    val cardActions = remember(onAction, confirmRemove) {
        GroupCardActions(
            onToggleCollapsed = { onAction(MainAction.ToggleGroupCollapsed(it)) },
            onRefresh = { onAction(MainAction.UpdateGroupSubscription(it)) },
            onPing = { onAction(MainAction.PingGroup(it)) },
            onSort = { id, sort -> onAction(MainAction.SetGroupSort(id, sort)) },
            onSelectFastest = { onAction(MainAction.SelectFastest(it)) },
            onEditSubscription = { onAction(MainAction.EditSubscription(it)) },
            onDeleteSubscription = { deleteSubscriptionTarget = it },
            onDeleteServers = { deleteServersTarget = it },
            onOpenLink = { onAction(MainAction.OpenSubscriptionLink(it)) },
            onGroupTool = { id, tool ->
                // Upstream bulk tools act on the "selected group".
                onAction(MainAction.SelectGroup(id))
                when (tool) {
                    MainMoreMenuAction.ExportAll -> onAction(MainAction.ExportAll)
                    MainMoreMenuAction.DeleteDuplicate -> showDelDuplicateConfirm = true
                    MainMoreMenuAction.DeleteInvalid -> showDelInvalidConfirm = true
                    MainMoreMenuAction.DeleteAll -> showDelAllConfirm = true
                    else -> Unit
                }
            },
            onSelectServer = { onAction(MainAction.SelectServer(it)) },
            onServerAction = { action, row ->
                when (action) {
                    ServerRowAction.Edit -> {
                        onAction(MainAction.SelectGroup(row.profile.subscriptionId))
                        onAction(MainAction.EditServer(row.guid, row.profile))
                    }
                    ServerRowAction.ShareQr -> onAction(MainAction.ShareQRCode(row.guid))
                    ServerRowAction.CopyLink -> onAction(MainAction.ShareClipboard(row.guid))
                    ServerRowAction.CopyFull -> onAction(MainAction.ShareFullContent(row.guid))
                    ServerRowAction.Ping -> onAction(MainAction.PingServer(row.guid))
                    ServerRowAction.Delete -> removeServer(row.guid, row.remarks)
                }
            },
        )
    }

    // ---------- Dialogs ----------
    MainDialogs(
        showDelAllConfirm = showDelAllConfirm,
        onDismissDelAll = { showDelAllConfirm = false },
        onConfirmDelAll = { showDelAllConfirm = false; onAction(MainAction.RemoveAllServers) },
        showDelDuplicateConfirm = showDelDuplicateConfirm,
        onDismissDelDuplicate = { showDelDuplicateConfirm = false },
        onConfirmDelDuplicate = { showDelDuplicateConfirm = false; onAction(MainAction.RemoveDuplicateServers) },
        showDelInvalidConfirm = showDelInvalidConfirm,
        onDismissDelInvalid = { showDelInvalidConfirm = false },
        onConfirmDelInvalid = { showDelInvalidConfirm = false; onAction(MainAction.RemoveInvalidServers) },
        showRemoveConfirm = showRemoveConfirm,
        onDismissRemove = { showRemoveConfirm = null },
        onConfirmRemove = { guid -> showRemoveConfirm = null; onAction(MainAction.RemoveServer(guid)) }
    )
    deleteSubscriptionTarget?.let { id ->
        DeleteConfirmDialog(
            message = stringResource(R.string.rb_delete_subscription_confirm),
            itemName = uiState.groups.firstOrNull { it.id == id }?.remarks,
            onConfirm = { deleteSubscriptionTarget = null; onAction(MainAction.DeleteSubscription(id)) },
            onDismiss = { deleteSubscriptionTarget = null }
        )
    }
    deleteServersTarget?.let { id ->
        DeleteConfirmDialog(
            message = stringResource(R.string.confirm_delete_visible_profiles),
            onConfirm = { deleteServersTarget = null; onAction(MainAction.DeleteGroupServers(id)) },
            onDismiss = { deleteServersTarget = null }
        )
    }
    if (shareQRCodeBitmap != null) {
        QRCodeDialog(bitmap = shareQRCodeBitmap, onDismiss = { onAction(MainAction.DismissQRCodeDialog) })
    }
    if (showRoutingSheet) {
        RoutingProfileSheet(
            profiles = uiState.routingProfiles,
            activeId = uiState.routingProfileId,
            onSelect = { id ->
                showRoutingSheet = false
                onAction(MainAction.SelectRoutingProfile(id))
            },
            onDelete = { id -> onAction(MainAction.DeleteRoutingProfile(id)) },
            onEditRules = {
                showRoutingSheet = false
                onAction(MainAction.OpenRoutingEditor)
            },
            onDismiss = { showRoutingSheet = false }
        )
    }

    val routingLabel = routingProfileLabel(uiState.routingProfileId, uiState.routingProfiles)
    // New manual imports go to the "Server-List" card, not to whichever group was used last.
    val importAction: (MainAction) -> Unit = { action ->
        when (action) {
            MainAction.ImportQRcode, MainAction.ImportClipboard, MainAction.ImportConfigLocal, is MainAction.ImportManually ->
                onAction(MainAction.SelectGroup(AppConfig.DEFAULT_SUBSCRIPTION_ID))
            else -> Unit
        }
        onAction(action)
    }

    BackHandler(enabled = showSearch && !drawerState.isOpen) {
        searchQuery = ""
        onAction(MainAction.Search(""))
        showSearch = false
    }
    BackHandler(enabled = drawerState.isOpen) { scope.launch { drawerState.close() } }

    ModalNavigationDrawer(
        drawerState = drawerState,
        drawerContent = {
            MainDrawerContent(
                drawerState = drawerState,
                onNavigate = { route ->
                    scope.launch { drawerState.close() }
                    onNavigate(route)
                },
                onTool = { tool ->
                    scope.launch { drawerState.close() }
                    when (tool) {
                        MainDrawerTool.Search -> showSearch = true
                        MainDrawerTool.UpdateSubscriptions -> onAction(MainAction.UpdateGroupSubscription(""))
                        MainDrawerTool.PingAll -> onAction(MainAction.PingGroup(PING_ALL_GROUPS))
                        MainDrawerTool.RestartService -> onAction(MainAction.RestartService)
                    }
                }
            )
        }
    ) {
        MainScreenContent(
            power = power,
            routingLabel = routingLabel,
            vpnMode = uiState.vpnMode,
            cards = cards,
            selectedGuid = uiState.selectedGuid,
            pingingGuids = uiState.pingingGuids,
            pingDisplayIcon = uiState.pingDisplayIcon,
            isLoading = isLoading,
            showSearch = showSearch,
            searchQuery = searchQuery,
            onSearchQueryChange = { query ->
                searchQuery = query
                onAction(MainAction.Search(query))
            },
            onSearchClose = {
                searchQuery = ""
                onAction(MainAction.Search(""))
                showSearch = false
            },
            onSettingsClick = { scope.launch { drawerState.open() } },
            onAction = importAction,
            onToggle = { onAction(MainAction.ToggleService) },
            onTest = { onAction(MainAction.TestCurrentServer) },
            onRoutingClick = { showRoutingSheet = true },
            onModeSelected = { vpn -> onAction(MainAction.SetVpnMode(vpn)) },
            cardActions = cardActions,
        )
    }
}

/** Stateless main screen (also rendered by RockBlackScreenshotTest). */
@Composable
internal fun MainScreenContent(
    power: PowerSectionUi,
    routingLabel: String,
    vpnMode: Boolean,
    cards: List<GroupCardUi>,
    selectedGuid: String?,
    pingingGuids: Set<String>,
    pingDisplayIcon: Boolean,
    isLoading: Boolean,
    showSearch: Boolean,
    searchQuery: String,
    onSearchQueryChange: (String) -> Unit,
    onSearchClose: () -> Unit,
    onSettingsClick: () -> Unit,
    onAction: (MainAction) -> Unit,
    onToggle: () -> Unit,
    onTest: () -> Unit,
    onRoutingClick: () -> Unit,
    onModeSelected: (Boolean) -> Unit,
    cardActions: GroupCardActions,
    listState: LazyListState = rememberLazyListState(),
    applyWindowInsets: Boolean = true,
) {
    val c = RockBlackTheme.colors
    val bottomInset = if (applyWindowInsets) WindowInsets.navigationBars.asPaddingValues().calculateBottomPadding() else 0.dp
    Box(
        Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(c.bgTop, c.bgBottom)))
    ) {
        Column(Modifier.fillMaxSize()) {
            MainTopBar(
                isLoading = isLoading,
                showSearch = showSearch,
                searchQuery = searchQuery,
                onSearchQueryChange = onSearchQueryChange,
                onSearchClose = onSearchClose,
                onSettingsClick = onSettingsClick,
                onAction = onAction,
                applyStatusBarInsets = applyWindowInsets,
            )
            LazyColumn(
                state = listState,
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScrollbar(listState),
                contentPadding = PaddingValues(bottom = 16.dp + bottomInset)
            ) {
                if (!showSearch) {
                    item(key = "power", contentType = "power") {
                        MainPowerSection(ui = power, onToggle = onToggle, onTest = onTest)
                    }
                    item(key = "chips", contentType = "chips") {
                        MainChipsRow(
                            routingLabel = routingLabel,
                            vpnMode = vpnMode,
                            onRoutingClick = onRoutingClick,
                            onModeSelected = onModeSelected,
                            modifier = Modifier.padding(top = 12.dp, bottom = 20.dp)
                        )
                    }
                } else {
                    item(key = "search-gap") { Spacer(Modifier.height(8.dp)) }
                }
                cards.forEach { card ->
                    groupCardItems(
                        card = card,
                        selectedGuid = selectedGuid,
                        pingingGuids = pingingGuids,
                        pingDisplayIcon = pingDisplayIcon,
                        actions = cardActions,
                    )
                }
                if (cards.isEmpty() && !showSearch) {
                    item(key = "empty") {
                        Text(
                            text = stringResource(R.string.rb_no_servers),
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 32.dp, vertical = 16.dp),
                            style = MaterialTheme.typography.bodyMedium,
                            color = c.textMuted,
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }
        }
    }
}
