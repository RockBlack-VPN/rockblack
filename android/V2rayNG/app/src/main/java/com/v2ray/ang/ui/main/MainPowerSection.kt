package com.v2ray.ang.ui.main

import android.net.TrafficStats
import android.os.Process
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.v2ray.ang.R
import com.v2ray.ang.extension.toSpeedString
import com.v2ray.ang.ui.compose.RockBlackTheme
import com.v2ray.ang.ui.compose.TabularNumbers
import kotlinx.coroutines.delay
import java.util.Locale

/** Connection state of the power button (DESIGN.md "The power button"). */
internal enum class PowerState { Disconnected, Connecting, Connected }

/** View-only data of the power section; live values are sampled by [rememberLiveSession]. */
internal data class PowerSectionUi(
    val state: PowerState,
    /** "00:16:28", connected only. */
    val timer: String? = null,
    /** Bytes per second; null hides the speed line. */
    val downSpeed: Long? = null,
    val upSpeed: Long? = null,
    /** Upstream connection-test line (tap the caption to run it), null when idle. */
    val statusText: String? = null,
)

private const val TRANSITION_MS = 200
private val BUTTON_DIAMETER = 168.dp
private val RING_STEP = 28.dp
private val GLOW_BLUR = 32.dp

@Composable
internal fun MainPowerSection(
    ui: PowerSectionUi,
    onToggle: () -> Unit,
    onTest: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val c = RockBlackTheme.colors
    Column(
        modifier = modifier.fillMaxWidth(),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        PowerButton(state = ui.state, onClick = onToggle)
        Spacer(Modifier.height(6.dp))
        val captionModifier = if (ui.state == PowerState.Connected) {
            Modifier
                .clip(MaterialTheme.shapes.small)
                .clickable(role = Role.Button, onClickLabel = stringResource(R.string.rb_tap_to_test), onClick = onTest)
                .padding(horizontal = 12.dp, vertical = 2.dp)
        } else {
            Modifier.padding(horizontal = 12.dp, vertical = 2.dp)
        }
        Column(captionModifier, horizontalAlignment = Alignment.CenterHorizontally) {
            Text(
                text = stringResource(
                    when (ui.state) {
                        PowerState.Disconnected -> R.string.rb_state_not_connected
                        PowerState.Connecting -> R.string.rb_state_connecting
                        PowerState.Connected -> R.string.rb_state_connected
                    }
                ).uppercase(Locale.getDefault()),
                style = MaterialTheme.typography.labelMedium,
                letterSpacing = 1.6.sp,
                fontWeight = FontWeight.SemiBold,
                color = if (ui.state == PowerState.Connected) c.primary else c.textMuted
            )
            // Fixed height so the list below does not jump when connecting.
            Box(Modifier.heightIn(min = 30.dp), contentAlignment = Alignment.Center) {
                if (ui.state == PowerState.Connected && ui.timer != null) {
                    Text(
                        text = ui.timer,
                        style = MaterialTheme.typography.headlineSmall.merge(TabularNumbers),
                        fontWeight = FontWeight.SemiBold,
                        color = c.text
                    )
                }
            }
        }
        Box(Modifier.heightIn(min = 18.dp), contentAlignment = Alignment.Center) {
            if (ui.state == PowerState.Connected && ui.downSpeed != null && ui.upSpeed != null) {
                SpeedLine(ui.downSpeed, ui.upSpeed)
            }
        }
        if (ui.statusText != null) {
            Spacer(Modifier.height(4.dp))
            Text(
                text = ui.statusText,
                modifier = Modifier
                    .widthIn(max = 340.dp)
                    .padding(horizontal = 24.dp),
                style = MaterialTheme.typography.bodySmall.merge(TabularNumbers),
                color = c.textMuted,
                textAlign = TextAlign.Center,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

@Composable
private fun SpeedLine(down: Long, up: Long) {
    val c = RockBlackTheme.colors
    val style = MaterialTheme.typography.bodySmall.merge(TabularNumbers)
    Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.Center) {
        Text("↓", style = style, color = c.primary, fontWeight = FontWeight.Bold)
        Spacer(Modifier.width(4.dp))
        Text(down.toSpeedString(), style = style, color = c.textMuted)
        Spacer(Modifier.width(16.dp))
        Text("↑", style = style, color = c.primary, fontWeight = FontWeight.Bold)
        Spacer(Modifier.width(4.dp))
        Text(up.toSpeedString(), style = style, color = c.textMuted)
    }
}

/**
 * The round power button: 168 dp disc with two 1 dp concentric rings (+28 / +56 dp), a soft
 * primary glow when connected and pulsing rings while connecting.
 */
@Composable
internal fun PowerButton(
    state: PowerState,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    diameter: Dp = BUTTON_DIAMETER,
) {
    val c = RockBlackTheme.colors
    val connected = state == PowerState.Connected
    val fill by animateColorAsState(if (connected) c.primary else c.surface2, tween(TRANSITION_MS), label = "powerFill")
    val icon by animateColorAsState(
        when (state) {
            PowerState.Connected -> c.onPrimary
            PowerState.Connecting -> c.primary
            PowerState.Disconnected -> c.textMuted
        },
        tween(TRANSITION_MS), label = "powerIcon"
    )
    val glow by animateFloatAsState(if (connected) 1f else 0f, tween(TRANSITION_MS), label = "powerGlow")
    val pulse = if (state == PowerState.Connecting) {
        val transition = rememberInfiniteTransition(label = "powerPulse")
        val value by transition.animateFloat(
            initialValue = 0.2f,
            targetValue = 0.6f,
            animationSpec = infiniteRepeatable(tween(900, easing = FastOutSlowInEasing), RepeatMode.Reverse),
            label = "powerPulseAlpha"
        )
        value
    } else {
        null
    }
    val ringColor = if (state == PowerState.Disconnected) c.outline else c.primary
    val innerRingAlpha = pulse ?: if (connected) 0.25f else 1f
    val outerRingAlpha = pulse?.let { it * 0.5f } ?: if (connected) 0.12f else 0.6f
    val description = stringResource(if (state == PowerState.Disconnected) R.string.acc_start else R.string.acc_stop)
    val total = diameter + RING_STEP * 2

    Box(
        modifier = modifier
            .size(total)
            .drawBehind {
                val center = Offset(size.width / 2f, size.height / 2f)
                val radius = diameter.toPx() / 2f
                if (glow > 0f) {
                    val glowRadius = radius + GLOW_BLUR.toPx()
                    drawCircle(
                        brush = Brush.radialGradient(
                            0f to c.primary.copy(alpha = 0.35f * glow),
                            (radius / glowRadius) to c.primary.copy(alpha = 0.35f * glow),
                            1f to Color.Transparent,
                            center = center,
                            radius = glowRadius
                        ),
                        radius = glowRadius,
                        center = center
                    )
                }
                val stroke = Stroke(width = 1.dp.toPx())
                drawCircle(
                    color = ringColor.copy(alpha = innerRingAlpha),
                    radius = radius + RING_STEP.toPx() / 2f,
                    center = center,
                    style = stroke
                )
                drawCircle(
                    color = ringColor.copy(alpha = outerRingAlpha),
                    radius = radius + RING_STEP.toPx(),
                    center = center,
                    style = stroke
                )
            },
        contentAlignment = Alignment.Center
    ) {
        Box(
            modifier = Modifier
                .size(diameter)
                .clip(CircleShape)
                .background(fill)
                .border(1.dp, if (connected) Color.White.copy(alpha = 0.18f) else c.outline, CircleShape)
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = androidx.compose.material3.ripple(),
                    role = Role.Button,
                    onClick = onClick
                )
                .semantics { contentDescription = description },
            contentAlignment = Alignment.Center
        ) {
            // Inner disc gives the button some depth (lighter at the top).
            Box(
                Modifier
                    .size(diameter * 0.74f)
                    .clip(CircleShape)
                    .background(
                        Brush.verticalGradient(
                            if (connected) {
                                listOf(Color.White.copy(alpha = 0.16f), Color.Black.copy(alpha = 0.06f))
                            } else {
                                listOf(Color.White.copy(alpha = if (c.isDark) 0.05f else 0.7f), Color.Black.copy(alpha = if (c.isDark) 0.12f else 0.03f))
                            }
                        )
                    )
            )
            Icon(
                painter = painterResource(R.drawable.ic_power_24dp),
                contentDescription = null,
                tint = icon,
                modifier = Modifier.size(diameter * 0.30f)
            )
        }
    }
}

// ---------- Chips ----------

@Composable
internal fun MainChipsRow(
    routingLabel: String,
    vpnMode: Boolean,
    onRoutingClick: () -> Unit,
    onModeSelected: (Boolean) -> Unit,
    modifier: Modifier = Modifier,
) {
    Row(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp, Alignment.CenterHorizontally),
        verticalAlignment = Alignment.CenterVertically
    ) {
        PillChip(
            iconRes = R.drawable.ic_routing_24dp,
            text = stringResource(R.string.rb_routing_chip, routingLabel),
            trailingArrow = true,
            onClick = onRoutingClick,
            modifier = Modifier.weight(1f, fill = false)
        )
        Box {
            var expanded by remember { mutableStateOf(false) }
            PillChip(
                iconRes = R.drawable.rb_ic_shield,
                text = stringResource(if (vpnMode) R.string.rb_mode_vpn else R.string.rb_mode_proxy),
                trailingArrow = true,
                onClick = { expanded = true },
            )
            DropdownMenu(
                expanded = expanded,
                onDismissRequest = { expanded = false },
                containerColor = MaterialTheme.colorScheme.surface
            ) {
                listOf(true, false).forEach { vpn ->
                    DropdownMenuItem(
                        text = { Text(stringResource(if (vpn) R.string.rb_mode_vpn else R.string.rb_mode_proxy)) },
                        trailingIcon = if (vpn == vpnMode) {
                            { Icon(painterResource(R.drawable.rb_ic_check), null, Modifier.size(18.dp), tint = RockBlackTheme.colors.primary) }
                        } else null,
                        onClick = {
                            expanded = false
                            if (vpn != vpnMode) onModeSelected(vpn)
                        }
                    )
                }
            }
        }
    }
}

@Composable
internal fun PillChip(
    iconRes: Int,
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    trailingArrow: Boolean = false,
) {
    val c = RockBlackTheme.colors
    Row(
        modifier = modifier
            .height(36.dp)
            .clip(CircleShape)
            .background(c.surface2)
            .border(1.dp, c.outline, CircleShape)
            .clickable(role = Role.Button, onClick = onClick)
            .padding(start = 12.dp, end = if (trailingArrow) 6.dp else 14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(painterResource(iconRes), contentDescription = null, tint = c.primary, modifier = Modifier.size(16.dp))
        Spacer(Modifier.width(8.dp))
        Text(
            text = text,
            modifier = Modifier.weight(1f, fill = false),
            style = MaterialTheme.typography.labelLarge,
            color = c.text,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
        if (trailingArrow) {
            Icon(painterResource(R.drawable.rb_ic_arrow_drop_down), contentDescription = null, tint = c.textMuted, modifier = Modifier.size(20.dp))
        }
    }
}

// ---------- Live session values ----------

internal data class LiveSession(val timer: String?, val down: Long?, val up: Long?)

internal fun formatSessionTimer(elapsedMillis: Long): String {
    val total = (elapsedMillis / 1000L).coerceAtLeast(0L)
    return String.format(Locale.ROOT, "%02d:%02d:%02d", total / 3600, (total % 3600) / 60, total % 60)
}

/**
 * Samples the session timer (start time written by the core process) and the live speed of the
 * app's own traffic (TrafficStats of this uid = the core's proxied traffic) once per second.
 */
@Composable
internal fun rememberLiveSession(running: Boolean, connectedSince: () -> Long): LiveSession {
    var timer by remember { mutableStateOf<String?>(null) }
    var down by remember { mutableLongStateOf(-1L) }
    var up by remember { mutableLongStateOf(-1L) }
    LaunchedEffect(running) {
        if (!running) {
            timer = null; down = -1L; up = -1L
            return@LaunchedEffect
        }
        val uid = Process.myUid()
        var lastRx = TrafficStats.getUidRxBytes(uid)
        var lastTx = TrafficStats.getUidTxBytes(uid)
        var lastTime = System.currentTimeMillis()
        var since = connectedSince().takeIf { it > 0L } ?: lastTime
        while (true) {
            val now = System.currentTimeMillis()
            if (since > now) since = now
            timer = formatSessionTimer(now - since)
            delay(1000L)
            val rx = TrafficStats.getUidRxBytes(uid)
            val tx = TrafficStats.getUidTxBytes(uid)
            val t = System.currentTimeMillis()
            val seconds = ((t - lastTime) / 1000.0).coerceAtLeast(0.5)
            if (rx >= 0 && lastRx >= 0) down = ((rx - lastRx).coerceAtLeast(0) / seconds).toLong()
            if (tx >= 0 && lastTx >= 0) up = ((tx - lastTx).coerceAtLeast(0) / seconds).toLong()
            lastRx = rx; lastTx = tx; lastTime = t
            connectedSince().takeIf { it > 0L }?.let { since = it }
        }
    }
    return LiveSession(timer, down.takeIf { it >= 0 }, up.takeIf { it >= 0 })
}
