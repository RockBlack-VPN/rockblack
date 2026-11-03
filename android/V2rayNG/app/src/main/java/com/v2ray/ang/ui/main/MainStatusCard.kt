package com.v2ray.ang.ui.main

import androidx.compose.animation.animateContentSize
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.v2ray.ang.R
import com.v2ray.ang.ui.compose.ConnectButton
import com.v2ray.ang.ui.compose.ProtocolChip
import com.v2ray.ang.ui.compose.RockBlackCard
import com.v2ray.ang.ui.compose.RockBlackTheme
import com.v2ray.ang.ui.compose.TabularNumbers

/** View-only data of the connection status card. */
internal data class MainStatusCardUi(
    val isRunning: Boolean,
    /** Upstream status line: connection state, test progress or connection-test result. */
    val statusText: String,
    /** Whether [statusText] carries more than the plain "not connected" state. */
    val showStatusLine: Boolean,
    val serverName: String?,
    val protocol: String?,
)

/**
 * Status card at the top of the main screen. It replaces upstream's floating start/stop
 * button and bottom status bar: the round button triggers the same toggle action, and the
 * status line triggers the same connection test as the old bottom bar.
 */
@Composable
internal fun MainStatusCard(
    state: MainStatusCardUi,
    compact: Boolean,
    onToggle: () -> Unit,
    onTest: () -> Unit,
    modifier: Modifier = Modifier,
) {
    RockBlackCard(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp)
            .animateContentSize(tween(200))
    ) {
        if (compact) {
            CompactStatus(state, onToggle, onTest)
        } else {
            ExpandedStatus(state, onToggle, onTest)
        }
    }
}

@Composable
private fun stateCaption(isRunning: Boolean): String =
    stringResource(if (isRunning) R.string.status_connected else R.string.connection_not_connected)

@Composable
private fun ExpandedStatus(state: MainStatusCardUi, onToggle: () -> Unit, onTest: () -> Unit) {
    val c = RockBlackTheme.colors
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(start = 16.dp, end = 16.dp, top = 4.dp, bottom = 16.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        ConnectButton(isRunning = state.isRunning, onClick = onToggle)
        Text(
            text = stateCaption(state.isRunning),
            style = MaterialTheme.typography.titleMedium,
            color = if (state.isRunning) c.primary else c.textMuted
        )
        Spacer(Modifier.height(10.dp))
        ServerLine(state, centered = true)
        if (state.showStatusLine) {
            Spacer(Modifier.height(12.dp))
            StatusLine(state.statusText, onTest)
        }
    }
}

@Composable
private fun CompactStatus(state: MainStatusCardUi, onToggle: () -> Unit, onTest: () -> Unit) {
    val c = RockBlackTheme.colors
    Column(Modifier.fillMaxWidth().padding(12.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            ConnectButton(
                isRunning = state.isRunning,
                onClick = onToggle,
                diameter = 52.dp,
                glowRadius = 6.dp
            )
            Spacer(Modifier.width(8.dp))
            Column(Modifier.weight(1f)) {
                Text(
                    text = stateCaption(state.isRunning),
                    style = MaterialTheme.typography.titleSmall,
                    color = if (state.isRunning) c.primary else c.textMuted
                )
                Spacer(Modifier.height(2.dp))
                ServerLine(state, centered = false)
            }
        }
        if (state.showStatusLine) {
            Spacer(Modifier.height(8.dp))
            StatusLine(state.statusText, onTest)
        }
    }
}

@Composable
private fun ServerLine(state: MainStatusCardUi, centered: Boolean) {
    val c = RockBlackTheme.colors
    Row(
        modifier = if (centered) Modifier.widthIn(max = 360.dp) else Modifier.fillMaxWidth(),
        horizontalArrangement = if (centered) Arrangement.Center else Arrangement.Start,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = state.serverName ?: stringResource(R.string.status_no_server),
            modifier = Modifier.weight(1f, fill = false),
            style = MaterialTheme.typography.bodyLarge,
            fontWeight = FontWeight.SemiBold,
            color = if (state.serverName != null) c.text else c.textMuted,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
            textAlign = if (centered) TextAlign.Center else TextAlign.Start
        )
        if (!state.protocol.isNullOrBlank()) {
            Spacer(Modifier.width(8.dp))
            ProtocolChip(text = state.protocol, highlighted = state.isRunning)
        }
    }
}

/** Upstream's bottom status text; tapping it runs the current-server connection test. */
@Composable
private fun StatusLine(text: String, onTest: () -> Unit) {
    val c = RockBlackTheme.colors
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(MaterialTheme.shapes.small)
            .background(c.surface2)
            .clickable(role = Role.Button, onClick = onTest)
            .padding(horizontal = 12.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            painter = painterResource(R.drawable.ic_flash_on_24dp),
            contentDescription = null,
            tint = c.textMuted,
            modifier = Modifier.size(18.dp)
        )
        Spacer(Modifier.width(8.dp))
        Text(
            text = text,
            style = MaterialTheme.typography.bodySmall.merge(TabularNumbers),
            color = c.text,
            maxLines = 3,
            overflow = TextOverflow.Ellipsis
        )
    }
}
