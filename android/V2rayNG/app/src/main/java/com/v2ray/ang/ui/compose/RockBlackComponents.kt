package com.v2ray.ang.ui.compose

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.v2ray.ang.R

private const val TRANSITION_MS = 200

/** Logo + "RockBlack" wordmark used in the main top bar and the drawer header. */
@Composable
fun BrandWordmark(modifier: Modifier = Modifier, logoSize: Dp = 28.dp) {
    Row(modifier = modifier, verticalAlignment = Alignment.CenterVertically) {
        Image(
            painter = painterResource(R.drawable.ic_brand_logo),
            contentDescription = null,
            modifier = Modifier.size(logoSize)
        )
        Spacer(Modifier.width(10.dp))
        Text(
            text = stringResource(R.string.app_name),
            style = MaterialTheme.typography.titleLarge,
            color = RockBlackTheme.colors.text,
            maxLines = 1
        )
    }
}

/**
 * The large round connect button (DESIGN.md "The connect button").
 * Disconnected: surface2 fill, outline ring, muted power icon.
 * Connected: primary fill, onPrimary icon and a soft primary glow.
 */
@Composable
fun ConnectButton(
    isRunning: Boolean,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    diameter: Dp = 160.dp,
    glowRadius: Dp = 24.dp,
) {
    val c = RockBlackTheme.colors
    val fill by animateColorAsState(if (isRunning) c.primary else c.surface2, tween(TRANSITION_MS), label = "connectFill")
    val ring by animateColorAsState(if (isRunning) c.primary else c.outline, tween(TRANSITION_MS), label = "connectRing")
    val iconTint by animateColorAsState(if (isRunning) c.onPrimary else c.textMuted, tween(TRANSITION_MS), label = "connectIcon")
    val glow by animateFloatAsState(if (isRunning) 1f else 0f, tween(TRANSITION_MS), label = "connectGlow")
    val description = stringResource(if (isRunning) R.string.acc_stop else R.string.acc_start)
    val ringWidth = if (diameter >= 96.dp) 3.dp else 2.dp

    Box(
        modifier = modifier
            .size(diameter + glowRadius * 2)
            .drawBehind {
                if (glow > 0f) {
                    val radius = diameter.toPx() / 2f + glowRadius.toPx()
                    drawCircle(
                        brush = Brush.radialGradient(
                            0.0f to c.primary.copy(alpha = 0.35f * glow),
                            (diameter.toPx() / 2f / radius) to c.primary.copy(alpha = 0.35f * glow),
                            1.0f to Color.Transparent,
                            center = center,
                            radius = radius
                        ),
                        radius = radius
                    )
                }
            },
        contentAlignment = Alignment.Center
    ) {
        Box(
            modifier = Modifier
                .size(diameter)
                .clip(CircleShape)
                .background(fill)
                .border(ringWidth, ring, CircleShape)
                .clickable(role = Role.Button, onClick = onClick)
                .semantics { contentDescription = description },
            contentAlignment = Alignment.Center
        ) {
            Icon(
                painter = painterResource(R.drawable.ic_power_24dp),
                contentDescription = null,
                tint = iconTint,
                modifier = Modifier.size(diameter * 0.36f)
            )
        }
    }
}

/** Pill-shaped tag, e.g. the protocol of a server. */
@Composable
fun ProtocolChip(
    text: String,
    modifier: Modifier = Modifier,
    highlighted: Boolean = false,
) {
    val c = RockBlackTheme.colors
    Text(
        text = text,
        modifier = modifier
            .clip(CircleShape)
            .background(if (highlighted) c.primarySoft else c.surface2)
            .padding(horizontal = 10.dp, vertical = 3.dp),
        style = MaterialTheme.typography.labelSmall,
        fontWeight = FontWeight.Medium,
        color = if (highlighted) c.primary else c.textMuted,
        maxLines = 1,
        overflow = TextOverflow.Ellipsis
    )
}

/** Flat card: surface fill, 1 dp outline, radius 16, no shadow. */
@Composable
fun RockBlackCard(
    modifier: Modifier = Modifier,
    shape: Shape = MaterialTheme.shapes.medium,
    color: Color = RockBlackTheme.colors.surface,
    borderColor: Color = RockBlackTheme.colors.outline,
    content: @Composable () -> Unit
) {
    Surface(
        modifier = modifier,
        shape = shape,
        color = color,
        border = BorderStroke(1.dp, borderColor),
        content = content
    )
}
