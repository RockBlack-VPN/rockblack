package com.v2ray.ang.ui.main

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.pluralStringResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextDirection
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.v2ray.ang.R
import com.v2ray.ang.dto.GroupMapItem
import com.v2ray.ang.extension.isComplexType
import com.v2ray.ang.extension.toTrafficString
import com.v2ray.ang.handler.RockBlackPrefs
import com.v2ray.ang.handler.SubscriptionInfoParser
import com.v2ray.ang.ui.compose.RockBlackTheme
import com.v2ray.ang.ui.compose.TabularNumbers
import com.v2ray.ang.ui.compose.latencyColor
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import kotlin.math.ceil

private val CARD_RADIUS = 18.dp
private val ROW_HEIGHT = 64.dp
private const val DAY_MILLIS = 86_400_000L
private const val EXPIRY_WARNING_DAYS = 3

/** Everything a subscription card renders (DESIGN.md "Main screen"). */
internal data class GroupCardUi(
    val id: String,
    val title: String,
    /** Card of the servers without a subscription. */
    val isServerList: Boolean,
    /** "09.11.2026 14:06 · auto 12 h", null when never updated. */
    val updatedText: String?,
    val canRefresh: Boolean,
    val info: SubscriptionInfoUiModel?,
    val announce: String?,
    /** null: no banner; 0: expires today; < 0: expired; otherwise days left. */
    val expiryDaysLeft: Int?,
    val renewUrl: String?,
    val collapsed: Boolean,
    val pinging: Boolean,
    val sort: String,
    val rows: List<ServerRowUiModel>,
)

internal enum class ServerRowAction { Edit, ShareQr, CopyLink, CopyFull, Ping, Delete }

internal class GroupCardActions(
    val onToggleCollapsed: (String) -> Unit = {},
    val onRefresh: (String) -> Unit = {},
    val onPing: (String) -> Unit = {},
    val onSort: (String, String) -> Unit = { _, _ -> },
    val onSelectFastest: (String) -> Unit = {},
    val onEditSubscription: (String) -> Unit = {},
    val onDeleteSubscription: (String) -> Unit = {},
    val onDeleteServers: (String) -> Unit = {},
    val onOpenLink: (String) -> Unit = {},
    val onGroupTool: (String, MainMoreMenuAction) -> Unit = { _, _ -> },
    val onSelectServer: (String) -> Unit = {},
    val onServerAction: (ServerRowAction, ServerRowUiModel) -> Unit = { _, _ -> },
)

// ---------- Model building ----------

internal fun formatUpdated(lastUpdated: Long, autoUpdateMinutes: Long?, autoText: (Int) -> String): String? {
    if (lastUpdated <= 0L) return autoUpdateMinutes?.let { autoText(((it + 59) / 60).toInt().coerceAtLeast(1)) }
    val date = SimpleDateFormat("dd.MM.yyyy HH:mm", Locale.ROOT).format(Date(lastUpdated))
    return if (autoUpdateMinutes != null) "$date · ${autoText(((autoUpdateMinutes + 59) / 60).toInt().coerceAtLeast(1))}" else date
}

/** Days until expiry for the danger banner, null when no banner is shown. */
internal fun expiryBannerDays(expireMillis: Long?, nowMillis: Long): Int? {
    if (expireMillis == null) return null
    val left = expireMillis - nowMillis
    if (left <= 0L) return -1
    val days = ceil(left.toDouble() / DAY_MILLIS).toInt()
    return if (days <= EXPIRY_WARNING_DAYS) (if (left < DAY_MILLIS) 0 else days) else null
}

internal fun sortRows(rows: List<ServerRowUiModel>, sort: String): List<ServerRowUiModel> = when (sort) {
    RockBlackPrefs.SORT_PING -> rows.sortedBy {
        when {
            it.testDelayMillis > 0L -> it.testDelayMillis
            it.testDelayMillis < 0L -> Long.MAX_VALUE - 1
            else -> Long.MAX_VALUE
        }
    }

    RockBlackPrefs.SORT_NAME -> rows.sortedBy { it.displayName.lowercase(Locale.ROOT) }
    else -> rows
}

internal fun buildGroupCard(
    group: GroupMapItem,
    rows: List<ServerRowUiModel>,
    isServerList: Boolean,
    serverListTitle: String,
    collapsed: Boolean,
    pinging: Boolean,
    sort: String,
    nowMillis: Long,
    autoText: (Int) -> String,
): GroupCardUi {
    val info = group.usageInfo?.let { buildSubscriptionInfoUiModel(it, nowMillis) }
    val hasFigures = group.usageInfo?.let { it.total > 0 || it.used > 0 || it.expire > 0 } == true
    return GroupCardUi(
        id = group.id,
        title = if (isServerList) serverListTitle else (info?.title ?: group.remarks),
        isServerList = isServerList,
        updatedText = if (group.hasUrl) formatUpdated(group.lastUpdated, group.autoUpdateMinutes, autoText) else null,
        canRefresh = group.hasUrl,
        info = info?.takeIf { hasFigures || it.supportUrl != null },
        announce = group.usageInfo?.announce,
        expiryDaysLeft = expiryBannerDays(info?.expireMillis, nowMillis),
        renewUrl = info?.webPageUrl ?: info?.supportUrl,
        collapsed = collapsed,
        pinging = pinging,
        sort = sort,
        rows = sortRows(rows, sort),
    )
}

// ---------- Lazy list items ----------

private fun segmentShape(first: Boolean, last: Boolean): Shape = RoundedCornerShape(
    topStart = if (first) CARD_RADIUS else 0.dp,
    topEnd = if (first) CARD_RADIUS else 0.dp,
    bottomStart = if (last) CARD_RADIUS else 0.dp,
    bottomEnd = if (last) CARD_RADIUS else 0.dp,
)

@Composable
private fun CardSegment(
    first: Boolean,
    last: Boolean,
    color: Color = RockBlackTheme.colors.cardSurface,
    content: @Composable () -> Unit,
) {
    val shape = segmentShape(first, last)
    Box(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp)
            .clip(shape)
            .background(color)
    ) { content() }
}

/** Emits one subscription card as several lazy items so long server lists stay lazy. */
internal fun LazyListScope.groupCardItems(
    card: GroupCardUi,
    selectedGuid: String?,
    pingingGuids: Set<String>,
    pingDisplayIcon: Boolean,
    actions: GroupCardActions,
) {
    val showBody = !card.collapsed
    val hasInfo = showBody && card.info != null
    val hasAnnounce = showBody && !card.announce.isNullOrBlank()
    val hasBanner = showBody && card.expiryDaysLeft != null
    val rows = if (showBody) card.rows else emptyList()
    val lastIsHeader = !hasInfo && !hasAnnounce && !hasBanner && rows.isEmpty()

    item(key = "header-${card.id}", contentType = "groupHeader") {
        CardSegment(first = true, last = lastIsHeader) { GroupHeader(card, actions) }
    }
    if (hasInfo) {
        item(key = "info-${card.id}", contentType = "groupInfo") {
            CardSegment(first = false, last = !hasAnnounce && !hasBanner && rows.isEmpty(), color = RockBlackTheme.colors.surface2) {
                GroupInfoBar(card.info!!, expiringSoon = card.expiryDaysLeft != null, onOpenLink = actions.onOpenLink)
            }
        }
    }
    if (hasAnnounce) {
        item(key = "announce-${card.id}", contentType = "groupAnnounce") {
            CardSegment(first = false, last = !hasBanner && rows.isEmpty(), color = RockBlackTheme.colors.surface2) {
                AnnounceLine(card.announce!!)
            }
        }
    }
    if (hasBanner) {
        item(key = "banner-${card.id}", contentType = "groupBanner") {
            CardSegment(first = false, last = rows.isEmpty()) {
                ExpiryBanner(card.expiryDaysLeft!!, card.renewUrl, actions.onOpenLink)
            }
        }
    }
    rows.forEachIndexed { index, row ->
        item(key = "row-${card.id}-${row.guid}", contentType = "serverRow") {
            CardSegment(first = false, last = index == rows.lastIndex) {
                Column {
                    HorizontalDivider(thickness = 1.dp, color = RockBlackTheme.colors.outline.copy(alpha = 0.7f))
                    ServerRow(
                        row = row,
                        selected = row.guid == selectedGuid,
                        pinging = card.pinging && row.guid in pingingGuids && row.testDelayMillis == 0L,
                        pingDisplayIcon = pingDisplayIcon,
                        onSelect = { actions.onSelectServer(row.guid) },
                        onAction = { action -> actions.onServerAction(action, row) },
                    )
                }
            }
        }
    }
    item(key = "gap-${card.id}", contentType = "groupGap") { Spacer(Modifier.height(12.dp)) }
}

// ---------- Header ----------

@Composable
private fun GroupHeader(card: GroupCardUi, actions: GroupCardActions) {
    val c = RockBlackTheme.colors
    val expandLabel = stringResource(if (card.collapsed) R.string.rb_expand_group else R.string.rb_collapse_group)
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .heightIn(min = 60.dp)
            .clickable(role = Role.Button, onClickLabel = expandLabel) { actions.onToggleCollapsed(card.id) }
            .padding(start = 8.dp, end = 4.dp, top = 6.dp, bottom = 6.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            painter = painterResource(R.drawable.ic_expand_more_24dp),
            contentDescription = null,
            tint = c.text,
            modifier = Modifier
                .padding(horizontal = 6.dp)
                .size(24.dp)
                .rotate(if (card.collapsed) 0f else 180f)
        )
        Column(Modifier.weight(1f).padding(start = 2.dp)) {
            Text(
                text = card.title,
                style = MaterialTheme.typography.titleMedium.copy(fontSize = 18.sp, lineHeight = 22.sp),
                fontWeight = FontWeight.Bold,
                color = c.text,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            card.updatedText?.let {
                Text(
                    text = it,
                    style = MaterialTheme.typography.labelSmall.merge(TabularNumbers),
                    color = c.textMuted,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
        if (card.canRefresh) {
            HeaderIcon(R.drawable.ic_refresh_24dp, stringResource(R.string.rb_group_update)) { actions.onRefresh(card.id) }
        }
        if (card.pinging) {
            Box(Modifier.size(40.dp), contentAlignment = Alignment.Center) {
                CircularProgressIndicator(Modifier.size(20.dp), color = c.primary, strokeWidth = 2.dp)
            }
        } else {
            HeaderIcon(R.drawable.rb_ic_speed, stringResource(R.string.rb_group_ping)) { actions.onPing(card.id) }
        }
        Box {
            var expanded by remember { mutableStateOf(false) }
            HeaderIcon(R.drawable.rb_ic_more_horiz, stringResource(R.string.acc_more)) { expanded = true }
            GroupMenu(card, expanded, onDismiss = { expanded = false }, actions = actions)
        }
    }
}

@Composable
private fun HeaderIcon(iconRes: Int, description: String, onClick: () -> Unit) {
    IconButton(onClick = onClick, modifier = Modifier.size(40.dp)) {
        Icon(painterResource(iconRes), description, Modifier.size(22.dp), tint = RockBlackTheme.colors.text)
    }
}

@Composable
private fun GroupMenu(card: GroupCardUi, expanded: Boolean, onDismiss: () -> Unit, actions: GroupCardActions) {
    val c = RockBlackTheme.colors
    DropdownMenu(expanded = expanded, onDismissRequest = onDismiss, containerColor = MaterialTheme.colorScheme.surface) {
        listOf(
            RockBlackPrefs.SORT_PING to R.string.rb_sort_ping,
            RockBlackPrefs.SORT_NAME to R.string.rb_sort_name,
            RockBlackPrefs.SORT_DEFAULT to R.string.rb_sort_default,
        ).forEach { (sort, label) ->
            DropdownMenuItem(
                text = { Text(stringResource(label)) },
                leadingIcon = { Icon(painterResource(R.drawable.rb_ic_sort), null, Modifier.size(20.dp)) },
                trailingIcon = if (card.sort == sort) {
                    { Icon(painterResource(R.drawable.rb_ic_check), null, Modifier.size(18.dp), tint = c.primary) }
                } else null,
                onClick = { onDismiss(); actions.onSort(card.id, sort) }
            )
        }
        HorizontalDivider(color = c.outline)
        DropdownMenuItem(
            text = { Text(stringResource(R.string.rb_select_fastest)) },
            leadingIcon = { Icon(painterResource(R.drawable.rb_ic_bolt), null, Modifier.size(20.dp)) },
            onClick = { onDismiss(); actions.onSelectFastest(card.id) }
        )
        DropdownMenuItem(
            text = { Text(stringResource(R.string.rb_group_ping)) },
            leadingIcon = { Icon(painterResource(R.drawable.rb_ic_speed), null, Modifier.size(20.dp)) },
            onClick = { onDismiss(); actions.onPing(card.id) }
        )
        if (card.canRefresh) {
            DropdownMenuItem(
                text = { Text(stringResource(R.string.rb_group_update)) },
                leadingIcon = { Icon(painterResource(R.drawable.ic_refresh_24dp), null, Modifier.size(20.dp)) },
                onClick = { onDismiss(); actions.onRefresh(card.id) }
            )
        }
        HorizontalDivider(color = c.outline)
        // Upstream bulk tools, scoped to this group.
        listOf(
            MainMoreMenuAction.ExportAll to R.drawable.ic_share_24dp,
            MainMoreMenuAction.DeleteDuplicate to R.drawable.ic_delete_24dp,
            MainMoreMenuAction.DeleteInvalid to R.drawable.ic_delete_24dp,
        ).forEach { (tool, icon) ->
            DropdownMenuItem(
                text = { Text(stringResource(tool.labelRes)) },
                leadingIcon = { Icon(painterResource(icon), null, Modifier.size(20.dp)) },
                onClick = { onDismiss(); actions.onGroupTool(card.id, tool) }
            )
        }
        if (!card.isServerList && card.id.isNotEmpty()) {
            DropdownMenuItem(
                text = { Text(stringResource(R.string.rb_edit_subscription)) },
                leadingIcon = { Icon(painterResource(R.drawable.ic_edit_24dp), null, Modifier.size(20.dp)) },
                onClick = { onDismiss(); actions.onEditSubscription(card.id) }
            )
            DropdownMenuItem(
                text = { Text(stringResource(R.string.rb_delete_subscription), color = c.danger) },
                leadingIcon = { Icon(painterResource(R.drawable.ic_delete_24dp), null, Modifier.size(20.dp), tint = c.danger) },
                onClick = { onDismiss(); actions.onDeleteSubscription(card.id) }
            )
        } else if (card.isServerList) {
            DropdownMenuItem(
                text = { Text(stringResource(R.string.rb_delete_group_servers), color = c.danger) },
                leadingIcon = { Icon(painterResource(R.drawable.ic_delete_24dp), null, Modifier.size(20.dp), tint = c.danger) },
                onClick = { onDismiss(); actions.onDeleteServers(card.id) }
            )
        }
    }
}

// ---------- Info bar, announcement, expiry banner ----------

@Composable
private fun GroupInfoBar(info: SubscriptionInfoUiModel, expiringSoon: Boolean, onOpenLink: (String) -> Unit) {
    val c = RockBlackTheme.colors
    val figures = MaterialTheme.typography.labelSmall.copy(textDirection = TextDirection.Ltr).merge(TabularNumbers)
    val totalText = if (info.totalBytes > 0L) info.totalBytes.toTrafficString() else "\u221E"
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height(40.dp)
            .padding(horizontal = 4.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        val pageUrl = info.webPageUrl
        IconButton(onClick = { pageUrl?.let(onOpenLink) }, enabled = pageUrl != null, modifier = Modifier.size(36.dp)) {
            Icon(painterResource(R.drawable.rb_ic_info), stringResource(R.string.sub_info_open_page), Modifier.size(20.dp), tint = c.primary)
        }
        // Traffic progress pill with the figures inside.
        val fraction = info.trafficFraction ?: 0f
        val barColor = if (info.trafficLow) c.danger else c.primary
        Box(
            modifier = Modifier
                .weight(1f)
                .height(22.dp)
                .clip(CircleShape)
                .background(c.cardSurface)
                .border(1.dp, c.outline, CircleShape),
            contentAlignment = Alignment.Center
        ) {
            if (fraction > 0f) {
                Box(
                    Modifier
                        .align(Alignment.CenterStart)
                        .fillMaxHeight()
                        .fillMaxWidth(fraction)
                        .background(barColor.copy(alpha = 0.35f))
                )
            }
            Text(
                text = "${info.usedBytes.toTrafficString()} / $totalText",
                style = figures,
                fontWeight = FontWeight.SemiBold,
                color = c.text,
                maxLines = 1
            )
        }
        Spacer(Modifier.width(10.dp))
        Text(
            text = info.expireMillis?.let {
                stringResource(R.string.rb_expires, SimpleDateFormat("dd.MM.yyyy", Locale.ROOT).format(Date(it)))
            } ?: stringResource(R.string.rb_expires, "\u221E"),
            style = figures,
            fontWeight = FontWeight.Medium,
            color = if (info.expired || expiringSoon) c.danger else c.text,
            maxLines = 1
        )
        val support = info.supportUrl
        if (support != null) {
            IconButton(onClick = { onOpenLink(support) }, modifier = Modifier.size(36.dp)) {
                Icon(
                    painterResource(if (SubscriptionInfoParser.isTelegramLink(support)) R.drawable.ic_telegram_24dp else R.drawable.rb_ic_link),
                    stringResource(R.string.sub_info_open_support),
                    Modifier.size(20.dp),
                    tint = c.primary
                )
            }
        } else {
            Spacer(Modifier.width(10.dp))
        }
    }
}

@Composable
private fun AnnounceLine(text: String) {
    val c = RockBlackTheme.colors
    Column {
        HorizontalDivider(thickness = 1.dp, color = c.outline.copy(alpha = 0.7f))
        Text(
            text = text,
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 8.dp),
            style = MaterialTheme.typography.bodySmall,
            fontWeight = FontWeight.Medium,
            color = c.text,
            textAlign = TextAlign.Center,
            maxLines = 4,
            overflow = TextOverflow.Ellipsis
        )
    }
}

@Composable
private fun ExpiryBanner(daysLeft: Int, renewUrl: String?, onOpenLink: (String) -> Unit) {
    val c = RockBlackTheme.colors
    val text = when {
        daysLeft < 0 -> stringResource(R.string.rb_sub_expired)
        daysLeft == 0 -> stringResource(R.string.rb_sub_expires_today)
        else -> pluralStringResource(R.plurals.rb_sub_expires_in_days, daysLeft, daysLeft)
    }
    Column(
        Modifier
            .fillMaxWidth()
            .background(c.danger.copy(alpha = 0.14f))
            .padding(horizontal = 12.dp, vertical = 10.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = text,
            style = MaterialTheme.typography.titleSmall,
            color = c.danger,
            textAlign = TextAlign.Center
        )
        if (renewUrl != null) {
            Spacer(Modifier.height(8.dp))
            Box(
                Modifier
                    .fillMaxWidth()
                    .height(36.dp)
                    .clip(MaterialTheme.shapes.small)
                    .background(c.danger)
                    .clickable(role = Role.Button) { onOpenLink(renewUrl) },
                contentAlignment = Alignment.Center
            ) {
                Text(
                    stringResource(R.string.rb_renew),
                    style = MaterialTheme.typography.labelLarge,
                    color = Color.White
                )
            }
        }
    }
}

// ---------- Server row ----------

@OptIn(ExperimentalFoundationApi::class)
@Composable
internal fun ServerRow(
    row: ServerRowUiModel,
    selected: Boolean,
    pinging: Boolean,
    pingDisplayIcon: Boolean,
    onSelect: () -> Unit,
    onAction: (ServerRowAction) -> Unit,
) {
    val c = RockBlackTheme.colors
    var menu by remember { mutableStateOf(false) }
    val selectedDescription = if (selected) stringResource(R.string.acc_selected_server) else null
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height(ROW_HEIGHT)
            .background(if (selected) c.primarySoft else Color.Transparent)
            .semantics { if (selectedDescription != null) stateDescription = selectedDescription }
            .combinedClickable(onClick = onSelect, onLongClick = { menu = true }),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            Modifier
                .width(3.dp)
                .fillMaxHeight()
                .background(if (selected) c.primary else Color.Transparent)
        )
        Spacer(Modifier.width(13.dp))
        FlagIcon(row.flag)
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Text(
                text = row.displayName,
                style = MaterialTheme.typography.bodyLarge.copy(fontSize = 16.sp, lineHeight = 20.sp),
                fontWeight = FontWeight.SemiBold,
                color = c.text,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
            Text(
                text = row.subtitle,
                style = MaterialTheme.typography.bodySmall.copy(fontSize = 13.sp, lineHeight = 17.sp),
                color = c.textMuted,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
        Spacer(Modifier.width(8.dp))
        PingValue(row.testDelayMillis, pinging, pingDisplayIcon)
        Box {
            IconButton(onClick = { menu = true }, modifier = Modifier.size(40.dp)) {
                Icon(
                    painterResource(R.drawable.rb_ic_chevron_right),
                    stringResource(R.string.rb_server_actions),
                    Modifier.size(22.dp),
                    tint = c.textMuted
                )
            }
            ServerActionsMenu(row, menu, onDismiss = { menu = false }, onAction = onAction)
        }
        Spacer(Modifier.width(4.dp))
    }
}

@Composable
private fun FlagIcon(flag: String?) {
    val c = RockBlackTheme.colors
    Box(
        Modifier
            .size(28.dp)
            .clip(RoundedCornerShape(6.dp))
            .background(c.surface2),
        contentAlignment = Alignment.Center
    ) {
        if (flag != null) {
            Text(flag, fontSize = 20.sp, lineHeight = 24.sp, maxLines = 1)
        } else {
            Icon(painterResource(R.drawable.ic_website_24dp), null, Modifier.size(18.dp), tint = c.textMuted)
        }
    }
}

@Composable
internal fun PingValue(delayMillis: Long, pinging: Boolean, iconMode: Boolean) {
    val c = RockBlackTheme.colors
    if (pinging) {
        CircularProgressIndicator(Modifier.size(14.dp), color = c.textMuted, strokeWidth = 1.5.dp)
        return
    }
    val color = when {
        delayMillis == 0L -> c.textMuted
        else -> c.latencyColor(delayMillis)
    }
    if (iconMode) {
        SignalBars(
            level = when {
                delayMillis == 0L -> 0
                delayMillis < 0L -> -1
                delayMillis < 300L -> 3
                delayMillis <= 800L -> 2
                else -> 1
            },
            color = color,
        )
        return
    }
    Text(
        text = when {
            delayMillis == 0L -> stringResource(R.string.rb_ping_na)
            delayMillis < 0L -> stringResource(R.string.rb_ping_timeout)
            else -> stringResource(R.string.rb_ping_ms, delayMillis.toInt())
        },
        style = MaterialTheme.typography.labelMedium.merge(TabularNumbers),
        color = color,
        maxLines = 1
    )
}

/** Status icon variant of the ping: three bars, filled by quality; -1 = failed. */
@Composable
private fun SignalBars(level: Int, color: Color, barWidth: Dp = 4.dp) {
    val c = RockBlackTheme.colors
    Row(verticalAlignment = Alignment.Bottom, horizontalArrangement = Arrangement.spacedBy(2.dp)) {
        listOf(6.dp, 10.dp, 14.dp).forEachIndexed { index, height ->
            val active = level > index || level < 0
            Box(
                Modifier
                    .width(barWidth)
                    .height(height)
                    .clip(RoundedCornerShape(1.dp))
                    .background(if (active) color else c.outline)
            )
        }
    }
}

@Composable
private fun ServerActionsMenu(
    row: ServerRowUiModel,
    expanded: Boolean,
    onDismiss: () -> Unit,
    onAction: (ServerRowAction) -> Unit,
) {
    val c = RockBlackTheme.colors
    val complex = row.profile.configType.isComplexType()
    DropdownMenu(expanded = expanded, onDismissRequest = onDismiss, containerColor = MaterialTheme.colorScheme.surface) {
        @Composable
        fun entry(action: ServerRowAction, label: Int, icon: Int, danger: Boolean = false) {
            DropdownMenuItem(
                text = { Text(stringResource(label), color = if (danger) c.danger else Color.Unspecified) },
                leadingIcon = { Icon(painterResource(icon), null, Modifier.size(20.dp), tint = if (danger) c.danger else c.textMuted) },
                onClick = { onDismiss(); onAction(action) }
            )
        }
        entry(ServerRowAction.Edit, R.string.action_edit, R.drawable.ic_edit_24dp)
        if (!complex) {
            entry(ServerRowAction.ShareQr, R.string.share_method_qrcode, R.drawable.ic_qu_scan_24dp)
            entry(ServerRowAction.CopyLink, R.string.share_method_clipboard, R.drawable.ic_copy)
        }
        entry(ServerRowAction.CopyFull, R.string.share_method_full_content, R.drawable.ic_share_24dp)
        entry(ServerRowAction.Ping, R.string.rb_server_ping, R.drawable.rb_ic_speed)
        entry(ServerRowAction.Delete, R.string.action_delete, R.drawable.ic_delete_24dp, danger = true)
    }
}
