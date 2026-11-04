package com.v2ray.ang.ui.screenshot

import android.app.Application
import android.graphics.Bitmap
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.rememberDrawerState
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.asAndroidBitmap
import androidx.compose.ui.test.captureToImage
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import androidx.compose.ui.unit.dp
import com.v2ray.ang.AppConfig
import com.v2ray.ang.dto.GroupMapItem
import com.v2ray.ang.dto.entities.ProfileItem
import com.v2ray.ang.dto.entities.SubscriptionUsageInfo
import com.v2ray.ang.enums.EConfigType
import com.v2ray.ang.handler.RockBlackPrefs
import com.v2ray.ang.handler.RoutingProfileManager
import com.v2ray.ang.ui.compose.AppTopBar
import com.v2ray.ang.ui.compose.CollapsiblePreferenceGroupHeader
import com.v2ray.ang.ui.compose.RockBlackTheme
import com.v2ray.ang.ui.compose.SettingsEditItem
import com.v2ray.ang.ui.compose.SettingsListItem
import com.v2ray.ang.ui.compose.SettingsSwitchItem
import com.v2ray.ang.ui.main.GroupCardActions
import com.v2ray.ang.ui.main.GroupCardUi
import com.v2ray.ang.ui.main.MainDrawerContent
import com.v2ray.ang.ui.main.MainScreenContent
import com.v2ray.ang.ui.main.PowerSectionUi
import com.v2ray.ang.ui.main.PowerState
import com.v2ray.ang.ui.main.RoutingProfileList
import com.v2ray.ang.ui.main.RoutingProfileUi
import com.v2ray.ang.ui.main.ServerRowUiModel
import com.v2ray.ang.ui.main.buildGroupCard
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * Renders the RockBlack UI 3.0 main screen (DESIGN.md) to PNG files so the design can be
 * reviewed without a device. Output: build/outputs/screenshots/ (uploaded by CI as the
 * "android-screenshots" artifact). These tests only record images, they do not diff.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = "w393dp-h852dp-xhdpi", application = Application::class)
class RockBlackScreenshotTest {

    @get:Rule
    val composeRule = createComposeRule()

    private val outDir: File =
        File(System.getProperty("rockblack.screenshots.dir") ?: "build/outputs/screenshots").apply { mkdirs() }

    private val now = 1_793_800_000_000L // fixed clock (Nov 2026)
    private val day = 86_400_000L

    private fun row(
        guid: String,
        name: String,
        type: EConfigType,
        delay: Long,
        network: String? = null,
        security: String? = null,
    ): ServerRowUiModel {
        val profile = ProfileItem(configType = type, remarks = name).apply {
            this.network = network
            this.security = security
        }
        return ServerRowUiModel(
            guid = guid,
            profile = profile,
            remarks = name,
            statistics = "",
            typeDescription = type.name,
            testDelayMillis = delay,
            subscriptionBadge = "",
        )
    }

    private fun subscriptionRows(withPing: Boolean) = listOf(
        row("1", "🇩🇪 Germany", EConfigType.VLESS, if (withPing) 42 else 0, "tcp", "reality"),
        row("2", "Netherlands · Amsterdam", EConfigType.VLESS, if (withPing) 386 else 0, "xhttp", "reality"),
        row("3", "🇱🇻 Latvia", EConfigType.TROJAN, if (withPing) -1 else 0, "tcp", "tls"),
        row("4", "Moldova", EConfigType.VMESS, if (withPing) 1255 else 0, "ws", "tls"),
        row("5", "Finland FI-02", EConfigType.HYSTERIA2, if (withPing) 97 else 0, null, "tls"),
    )

    private val manualRows = listOf(
        row("m1", "🇺🇸 USA - Seattle", EConfigType.VMESS, 0, "ws", "tls"),
        row("m2", "Home router", EConfigType.SHADOWSOCKS, 0),
    )

    private fun cards(withPing: Boolean, expiresInDays: Long = 300, announce: String? = null, collapsedManual: Boolean = false): List<GroupCardUi> {
        val subscription = GroupMapItem(
            id = "sub",
            remarks = "RockBlack",
            usageInfo = SubscriptionUsageInfo(
                upload = 400_000_000,
                download = 2_900_000_000,
                total = 50_000_000_000,
                expire = (now + expiresInDays * day) / 1000,
                title = "My subscription",
                supportUrl = "https://t.me/rockblack_vpn",
                webPageUrl = "https://my.rockblack.tech",
                announce = announce,
            ),
            lastUpdated = now - 3 * 3_600_000L,
            autoUpdateMinutes = 720,
            hasUrl = true,
        )
        val manual = GroupMapItem(id = AppConfig.DEFAULT_SUBSCRIPTION_ID, remarks = "Default")
        val auto = { h: Int -> "auto $h h" }
        return listOf(
            buildGroupCard(subscription, subscriptionRows(withPing), false, "Server-List", false, false, RockBlackPrefs.SORT_DEFAULT, now, auto),
            buildGroupCard(manual, manualRows, true, "Server-List", collapsedManual, false, RockBlackPrefs.SORT_DEFAULT, now, auto),
        )
    }

    private fun capture(name: String, content: @Composable () -> Unit) {
        composeRule.setContent(content)
        composeRule.waitForIdle()
        val bitmap = composeRule.onRoot().captureToImage().asAndroidBitmap()
        File(outDir, "$name.png").outputStream().use { bitmap.compress(Bitmap.CompressFormat.PNG, 100, it) }
    }

    @Composable
    private fun Main(
        dark: Boolean,
        power: PowerSectionUi,
        cards: List<GroupCardUi>,
        routing: String = "Russia",
        pingIcons: Boolean = false,
    ) {
        RockBlackTheme(darkTheme = dark) {
            MainScreenContent(
                power = power,
                routingLabel = routing,
                vpnMode = true,
                cards = cards,
                selectedGuid = "1",
                pingingGuids = emptySet(),
                pingDisplayIcon = pingIcons,
                isLoading = false,
                showSearch = false,
                searchQuery = "",
                onSearchQueryChange = {},
                onSearchClose = {},
                onSettingsClick = {},
                onAction = {},
                onToggle = {},
                onTest = {},
                onRoutingClick = {},
                onModeSelected = {},
                cardActions = GroupCardActions(),
                applyWindowInsets = false,
            )
        }
    }

    private val connected = PowerSectionUi(
        state = PowerState.Connected,
        timer = "00:16:28",
        downSpeed = 1_250_000,
        upSpeed = 143_000,
    )

    @Test
    fun main_dark_disconnected() = capture("main_dark_disconnected") {
        Main(dark = true, power = PowerSectionUi(PowerState.Disconnected), cards = cards(withPing = false))
    }

    @Test
    fun main_dark_connected() = capture("main_dark_connected") {
        Main(dark = true, power = connected, cards = cards(withPing = true))
    }

    @Test
    fun main_dark_connecting() = capture("main_dark_connecting") {
        Main(dark = true, power = PowerSectionUi(PowerState.Connecting), cards = cards(withPing = true), pingIcons = true)
    }

    @Test
    fun main_dark_expiry_banner() = capture("main_dark_expiry_banner") {
        Main(
            dark = true,
            power = connected,
            cards = cards(withPing = true, expiresInDays = 3, announce = "Maintenance on Sunday 03:00–04:00 MSK"),
        )
    }

    @Test
    @Config(qualifiers = "w393dp-h1300dp-xhdpi")
    fun main_dark_full_list() = capture("main_dark_full_list") {
        Main(dark = true, power = connected, cards = cards(withPing = true))
    }

    @Test
    fun main_light_connected() = capture("main_light_connected") {
        Main(dark = false, power = connected, cards = cards(withPing = true))
    }

    @Test
    fun main_light_disconnected() = capture("main_light_disconnected") {
        Main(dark = false, power = PowerSectionUi(PowerState.Disconnected), cards = cards(withPing = false, expiresInDays = -1))
    }

    private val routingProfiles = RoutingProfileManager.Preset.entries.map { RoutingProfileUi(it.id) } +
            RoutingProfileUi(RoutingProfileManager.IMPORTED_PREFIX + "x", "Panel profile", imported = true) +
            RoutingProfileUi(RoutingProfileManager.PROFILE_CUSTOM)

    private fun routingSheet(name: String, dark: Boolean) = capture(name) {
        RockBlackTheme(darkTheme = dark) {
            Box(
                Modifier
                    .fillMaxWidth()
                    .background(RockBlackTheme.colors.surface)
                    .padding(top = 16.dp)
            ) {
                RoutingProfileList(
                    profiles = routingProfiles,
                    activeId = RoutingProfileManager.Preset.RUSSIA.id,
                    onSelect = {},
                    onDelete = {},
                    onEditRules = {},
                )
            }
        }
    }

    @Test
    fun routing_sheet_dark() = routingSheet("routing_sheet_dark", dark = true)

    @Test
    fun routing_sheet_light() = routingSheet("routing_sheet_light", dark = false)

    private fun drawer(name: String, dark: Boolean) = capture(name) {
        RockBlackTheme(darkTheme = dark) {
            Box(
                Modifier
                    .fillMaxSize()
                    .background(RockBlackTheme.colors.bg)
            ) {
                MainDrawerContent(
                    drawerState = rememberDrawerState(DrawerValue.Open),
                    onNavigate = {},
                    onTool = {},
                )
            }
        }
    }

    @Test
    fun drawer_dark() = drawer("drawer_dark", dark = true)

    @Test
    fun drawer_light() = drawer("drawer_light", dark = false)

    private fun settings(name: String, dark: Boolean) = capture(name) {
        RockBlackTheme(darkTheme = dark) {
            Column(
                Modifier
                    .fillMaxSize()
                    .background(RockBlackTheme.colors.bg)
            ) {
                AppTopBar(title = "Settings", onBackClick = {})
                CollapsiblePreferenceGroupHeader(title = "Ping", expanded = true, onExpandedChange = {})
                SettingsListItem(
                    title = "Ping method",
                    entries = listOf("Via proxy (GET)", "TCP"),
                    values = listOf("proxy_get", "tcp"),
                    selectedValue = "proxy_get",
                    onSelected = {}
                )
                SettingsListItem(
                    title = "Ping display",
                    entries = listOf("Time (42 ms)", "Status icon"),
                    values = listOf("time", "icon"),
                    selectedValue = "time",
                    onSelected = {}
                )
                SettingsSwitchItem(
                    title = "Ping on launch",
                    summary = "Test all servers when the app opens",
                    checked = true,
                    onCheckedChange = {}
                )
                SettingsEditItem(title = "Test URL", value = "https://www.gstatic.com/generate_204", onValueChanged = {})
                CollapsiblePreferenceGroupHeader(title = "UI settings", expanded = false, onExpandedChange = {})
            }
        }
    }

    @Test
    fun settings_dark() = settings("settings_dark", dark = true)

    @Test
    fun settings_light() = settings("settings_light", dark = false)
}
