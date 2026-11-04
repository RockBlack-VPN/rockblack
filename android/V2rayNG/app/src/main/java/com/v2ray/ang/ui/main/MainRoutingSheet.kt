package com.v2ray.ang.ui.main

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.v2ray.ang.R
import com.v2ray.ang.handler.RoutingProfileManager
import com.v2ray.ang.ui.compose.RockBlackTheme

/** Localized name of a routing profile. */
@Composable
internal fun routingProfileLabel(id: String, profiles: List<RoutingProfileUi>): String =
    when (RoutingProfileManager.Preset.fromId(id)) {
        RoutingProfileManager.Preset.GLOBAL -> stringResource(R.string.rb_routing_global)
        RoutingProfileManager.Preset.RUSSIA -> stringResource(R.string.rb_routing_russia)
        RoutingProfileManager.Preset.BLOCK_ADS -> stringResource(R.string.rb_routing_block_ads)
        RoutingProfileManager.Preset.DIRECT -> stringResource(R.string.rb_routing_direct)
        null -> profiles.firstOrNull { it.id == id }?.name ?: stringResource(R.string.rb_routing_custom)
    }

@Composable
private fun routingProfileDescription(profile: RoutingProfileUi): String =
    when (RoutingProfileManager.Preset.fromId(profile.id)) {
        RoutingProfileManager.Preset.GLOBAL -> stringResource(R.string.rb_routing_global_desc)
        RoutingProfileManager.Preset.RUSSIA -> stringResource(R.string.rb_routing_russia_desc)
        RoutingProfileManager.Preset.BLOCK_ADS -> stringResource(R.string.rb_routing_block_ads_desc)
        RoutingProfileManager.Preset.DIRECT -> stringResource(R.string.rb_routing_direct_desc)
        null -> stringResource(if (profile.imported) R.string.rb_routing_imported_desc else R.string.rb_routing_custom_desc)
    }

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun RoutingProfileSheet(
    profiles: List<RoutingProfileUi>,
    activeId: String,
    onSelect: (String) -> Unit,
    onDelete: (String) -> Unit,
    onEditRules: () -> Unit,
    onDismiss: () -> Unit,
) {
    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true),
        containerColor = RockBlackTheme.colors.surface,
    ) {
        RoutingProfileList(
            profiles = profiles,
            activeId = activeId,
            onSelect = onSelect,
            onDelete = onDelete,
            onEditRules = onEditRules,
            modifier = Modifier.navigationBarsPadding()
        )
    }
}

/** Content of the routing sheet (also rendered directly by the screenshot test). */
@Composable
internal fun RoutingProfileList(
    profiles: List<RoutingProfileUi>,
    activeId: String,
    onSelect: (String) -> Unit,
    onDelete: (String) -> Unit,
    onEditRules: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val c = RockBlackTheme.colors
    Column(
        modifier
            .fillMaxWidth()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 16.dp)
            .padding(bottom = 16.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text(
            stringResource(R.string.rb_routing_title),
            style = MaterialTheme.typography.titleLarge,
            color = c.text,
            modifier = Modifier.padding(start = 4.dp, bottom = 4.dp)
        )
        profiles.forEach { profile ->
            // "Custom" only appears while it is the active state (rules edited by hand).
            if (profile.id == RoutingProfileManager.PROFILE_CUSTOM && activeId != RoutingProfileManager.PROFILE_CUSTOM) return@forEach
            val selected = profile.id == activeId
            Row(
                Modifier
                    .fillMaxWidth()
                    .heightIn(min = 64.dp)
                    .clip(MaterialTheme.shapes.small)
                    .background(if (selected) c.primarySoft else c.surface2)
                    .border(1.dp, if (selected) c.primary.copy(alpha = 0.6f) else Color.Transparent, MaterialTheme.shapes.small)
                    .clickable(role = Role.RadioButton) { onSelect(profile.id) }
                    .padding(horizontal = 14.dp, vertical = 10.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Box(
                    Modifier
                        .size(22.dp)
                        .clip(CircleShape)
                        .border(2.dp, if (selected) c.primary else c.textMuted, CircleShape),
                    contentAlignment = Alignment.Center
                ) {
                    if (selected) {
                        Box(Modifier.size(12.dp).clip(CircleShape).background(c.primary))
                    }
                }
                Spacer(Modifier.width(14.dp))
                Column(Modifier.weight(1f)) {
                    Text(
                        routingProfileLabel(profile.id, profiles),
                        style = MaterialTheme.typography.bodyLarge,
                        fontWeight = FontWeight.SemiBold,
                        color = c.text,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    Text(
                        routingProfileDescription(profile),
                        style = MaterialTheme.typography.bodySmall,
                        color = c.textMuted
                    )
                }
                if (profile.imported) {
                    IconButton(onClick = { onDelete(profile.id) }, modifier = Modifier.size(36.dp)) {
                        Icon(
                            painterResource(R.drawable.ic_delete_24dp),
                            stringResource(R.string.rb_routing_delete_profile),
                            Modifier.size(20.dp),
                            tint = c.textMuted
                        )
                    }
                }
            }
        }
        Spacer(Modifier.height(4.dp))
        Row(
            Modifier
                .fillMaxWidth()
                .height(48.dp)
                .clip(MaterialTheme.shapes.small)
                .border(1.dp, c.outline, MaterialTheme.shapes.small)
                .clickable(role = Role.Button, onClick = onEditRules),
            horizontalArrangement = Arrangement.Center,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(painterResource(R.drawable.ic_routing_24dp), null, Modifier.size(18.dp), tint = c.primary)
            Spacer(Modifier.width(8.dp))
            Text(stringResource(R.string.rb_routing_edit_rules), style = MaterialTheme.typography.labelLarge, color = c.primary)
        }
    }
}
