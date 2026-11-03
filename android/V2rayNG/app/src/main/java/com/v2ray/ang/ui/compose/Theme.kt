package com.v2ray.ang.ui.compose

import android.app.Activity
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.ColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.SideEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.compositionLocalOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.compositeOver
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.view.WindowCompat
import com.v2ray.ang.AppConfig
import com.v2ray.ang.handler.MmkvManager
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

/*
 * RockBlack UI 2.0 design tokens (see branding/DESIGN.md in the monorepo root).
 * Dark graphite is the default; teal is the only accent. Dynamic color is
 * intentionally not used so the brand palette is always applied.
 */

/** Design-system palette; read it in composables via [RockBlackTheme.colors]. */
@Immutable
data class RockBlackPalette(
    val isDark: Boolean,
    val bg: Color,
    val surface: Color,
    val surface2: Color,
    val outline: Color,
    val primary: Color,
    val onPrimary: Color,
    val primarySoft: Color,
    val text: Color,
    val textMuted: Color,
    val danger: Color,
    val warning: Color,
    val graphite: Color = Color(0xFF5F6062),
)

val DarkPalette = RockBlackPalette(
    isDark = true,
    bg = Color(0xFF0F1011),
    surface = Color(0xFF18191B),
    surface2 = Color(0xFF212326),
    outline = Color(0xFF2D3034),
    primary = Color(0xFF26D6B0),
    onPrimary = Color(0xFF04231C),
    primarySoft = Color(0xFF26D6B0).copy(alpha = 0.14f),
    text = Color(0xFFF1F3F4),
    textMuted = Color(0xFF8E959C),
    danger = Color(0xFFFF5C6C),
    warning = Color(0xFFFFB547),
)

val LightPalette = RockBlackPalette(
    isDark = false,
    bg = Color(0xFFF3F5F6),
    surface = Color(0xFFFFFFFF),
    surface2 = Color(0xFFEEF1F2),
    outline = Color(0xFFDCE1E4),
    primary = Color(0xFF0FA383),
    onPrimary = Color(0xFFFFFFFF),
    primarySoft = Color(0xFF0FA383).copy(alpha = 0.12f),
    text = Color(0xFF14171A),
    textMuted = Color(0xFF5E666D),
    danger = Color(0xFFD93848),
    warning = Color(0xFFB87400),
)

private fun RockBlackPalette.toColorScheme(): ColorScheme {
    val primaryContainer = primarySoft.compositeOver(surface)
    val errorContainer = danger.copy(alpha = 0.16f).compositeOver(surface)
    return if (isDark) {
        darkColorScheme(
            primary = primary,
            onPrimary = onPrimary,
            primaryContainer = primaryContainer,
            onPrimaryContainer = text,
            inversePrimary = Color(0xFF0FA383),
            secondary = primary,
            onSecondary = onPrimary,
            secondaryContainer = primaryContainer,
            onSecondaryContainer = text,
            tertiary = warning,
            onTertiary = Color(0xFF2B1A00),
            tertiaryContainer = warning.copy(alpha = 0.16f).compositeOver(surface),
            onTertiaryContainer = text,
            error = danger,
            onError = Color(0xFF2B0006),
            errorContainer = errorContainer,
            onErrorContainer = text,
            background = bg,
            onBackground = text,
            surface = surface,
            onSurface = text,
            surfaceVariant = surface2,
            onSurfaceVariant = textMuted,
            surfaceTint = Color.Transparent,
            inverseSurface = text,
            inverseOnSurface = bg,
            outline = outline,
            outlineVariant = outline,
            scrim = Color(0xFF000000),
            surfaceBright = surface2,
            surfaceDim = bg,
            surfaceContainerLowest = bg,
            surfaceContainerLow = Color(0xFF141517),
            surfaceContainer = surface,
            surfaceContainerHigh = surface2,
            surfaceContainerHighest = Color(0xFF292B2F),
        )
    } else {
        lightColorScheme(
            primary = primary,
            onPrimary = onPrimary,
            primaryContainer = primaryContainer,
            onPrimaryContainer = text,
            inversePrimary = Color(0xFF26D6B0),
            secondary = primary,
            onSecondary = onPrimary,
            secondaryContainer = primaryContainer,
            onSecondaryContainer = text,
            tertiary = warning,
            onTertiary = Color.White,
            tertiaryContainer = warning.copy(alpha = 0.14f).compositeOver(surface),
            onTertiaryContainer = text,
            error = danger,
            onError = Color.White,
            errorContainer = errorContainer,
            onErrorContainer = text,
            background = bg,
            onBackground = text,
            surface = surface,
            onSurface = text,
            surfaceVariant = surface2,
            onSurfaceVariant = textMuted,
            surfaceTint = Color.Transparent,
            inverseSurface = Color(0xFF26292C),
            inverseOnSurface = Color(0xFFF1F3F4),
            outline = outline,
            outlineVariant = outline,
            scrim = Color(0xFF000000),
            surfaceBright = surface,
            surfaceDim = bg,
            surfaceContainerLowest = surface,
            surfaceContainerLow = Color(0xFFF8F9FA),
            surfaceContainer = surface,
            surfaceContainerHigh = surface2,
            surfaceContainerHighest = Color(0xFFE5E9EB),
        )
    }
}

/** Radii: chips/pills via CircleShape, buttons & inputs 12, cards 16, sheets/dialogs 24. */
val RockBlackShapes = Shapes(
    extraSmall = RoundedCornerShape(8.dp),
    small = RoundedCornerShape(12.dp),
    medium = RoundedCornerShape(16.dp),
    large = RoundedCornerShape(20.dp),
    extraLarge = RoundedCornerShape(24.dp),
)

private val BaseTypography = Typography()

val RockBlackTypography = BaseTypography.copy(
    headlineSmall = BaseTypography.headlineSmall.copy(fontWeight = FontWeight.SemiBold),
    titleLarge = BaseTypography.titleLarge.copy(fontSize = 22.sp, lineHeight = 28.sp, fontWeight = FontWeight.SemiBold),
    titleMedium = BaseTypography.titleMedium.copy(fontSize = 16.sp, fontWeight = FontWeight.SemiBold),
    titleSmall = BaseTypography.titleSmall.copy(fontSize = 15.sp, lineHeight = 20.sp, fontWeight = FontWeight.SemiBold),
    bodyLarge = BaseTypography.bodyLarge.copy(fontSize = 15.sp, lineHeight = 21.sp),
    bodyMedium = BaseTypography.bodyMedium.copy(fontSize = 14.sp, lineHeight = 20.sp),
    bodySmall = BaseTypography.bodySmall.copy(fontSize = 12.sp, lineHeight = 16.sp),
    labelLarge = BaseTypography.labelLarge.copy(fontWeight = FontWeight.SemiBold),
)

/** Numbers (latency, speed, traffic) use tabular figures. */
val TabularNumbers = TextStyle(fontFeatureSettings = "tnum")

// Semantic colors kept for existing call sites; prefer RockBlackTheme.colors in new code.
val colorPing = Color(0xFF26D6B0)
val colorPingRed = Color(0xFFFF5C6C)
val colorConfigType = Color(0xFF1FB896)
val colorFabActive = Color(0xFF26D6B0)
val colorFabInactiveLight = Color(0xFFEEF1F2)
val colorFabInactiveDark = Color(0xFF212326)
val dividerColorLight = LightPalette.outline
val dividerColorDark = DarkPalette.outline

// Toast colors (85 %)
val toastNormalBgLight = Color(0xD926292C)
val toastNormalBgDark = Color(0xD9292B2F)
val toastSuccessBg = Color(0xD90E8C71)
val toastErrorBg = Color(0xD9C42E3E)
val toastInfoBg = Color(0xD93A4048)
val toastIconCircleBg = Color(0x33FFFFFF)
val toastTextColor = Color.White

/** Latency colors: < 300 ms primary, 300–800 ms warning, > 800 ms or failure danger. */
fun RockBlackPalette.latencyColor(delayMillis: Long): Color = when {
    delayMillis < 0L -> danger
    delayMillis < 300L -> primary
    delayMillis <= 800L -> warning
    else -> danger
}

object ThemeManager {
    /** RockBlack: dark graphite unless the user picked another mode ("0" system, "1" light, "2" dark). */
    const val DEFAULT_THEME_MODE = "2"

    private val _themeMode = MutableStateFlow(
        MmkvManager.decodeSettingsString(AppConfig.PREF_UI_MODE_NIGHT, DEFAULT_THEME_MODE) ?: DEFAULT_THEME_MODE
    )
    val themeMode: StateFlow<String> = _themeMode.asStateFlow()

    private val _dynamicColorEnabled = MutableStateFlow(
        MmkvManager.decodeSettingsBool(AppConfig.PREF_DYNAMIC_COLOR, true)
    )

    /** Stored for compatibility only; the RockBlack palette ignores dynamic color. */
    val dynamicColorEnabled: StateFlow<Boolean> = _dynamicColorEnabled.asStateFlow()

    fun setThemeMode(mode: String) {
        MmkvManager.encodeSettings(AppConfig.PREF_UI_MODE_NIGHT, mode)
        _themeMode.value = mode
    }

    fun setDynamicColorEnabled(enabled: Boolean) {
        MmkvManager.encodeSettings(AppConfig.PREF_DYNAMIC_COLOR, enabled)
        _dynamicColorEnabled.value = enabled
    }

    fun refresh() {
        _themeMode.value =
            MmkvManager.decodeSettingsString(AppConfig.PREF_UI_MODE_NIGHT, DEFAULT_THEME_MODE) ?: DEFAULT_THEME_MODE
        _dynamicColorEnabled.value =
            MmkvManager.decodeSettingsBool(AppConfig.PREF_DYNAMIC_COLOR, true)
    }
}

@Composable
fun resolveDarkTheme(): Boolean {
    val mode by ThemeManager.themeMode.collectAsState()
    return when (mode) {
        "1" -> false
        "2" -> true
        else -> isSystemInDarkTheme()
    }
}

val LocalDarkTheme = compositionLocalOf { false }

val LocalRockBlackPalette = staticCompositionLocalOf { DarkPalette }

object RockBlackTheme {
    val colors: RockBlackPalette
        @Composable get() = LocalRockBlackPalette.current
}

/**
 * Pure brand theme (no settings access) used by [AppTheme] and by screenshot tests.
 */
@Composable
fun RockBlackTheme(
    darkTheme: Boolean,
    content: @Composable () -> Unit
) {
    val palette = if (darkTheme) DarkPalette else LightPalette
    CompositionLocalProvider(
        LocalDarkTheme provides darkTheme,
        LocalRockBlackPalette provides palette
    ) {
        MaterialTheme(
            colorScheme = palette.toColorScheme(),
            shapes = RockBlackShapes,
            typography = RockBlackTypography,
            content = content
        )
    }
}

@Composable
fun AppTheme(
    darkTheme: Boolean = resolveDarkTheme(),
    content: @Composable () -> Unit
) {
    val snackbarController = rememberAppSnackbarController()

    val view = LocalView.current
    if (!view.isInEditMode) {
        SideEffect {
            val activity = view.context as? Activity ?: return@SideEffect
            val window = activity.window
            WindowCompat.getInsetsController(window, view).apply {
                isAppearanceLightStatusBars = !darkTheme
                isAppearanceLightNavigationBars = !darkTheme
            }
        }
    }

    RockBlackTheme(darkTheme = darkTheme) {
        CompositionLocalProvider(LocalAppSnackbar provides snackbarController) {
            Box(modifier = Modifier.fillMaxSize()) {
                AppSnackbarBridge(controller = snackbarController)
                content()
                AppSnackbarHost(hostState = snackbarController.hostState)
            }
        }
    }
}
