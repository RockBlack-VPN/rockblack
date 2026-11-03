package com.v2ray.ang.ui.screenshot

import android.app.Application
import android.graphics.Bitmap
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
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
import com.v2ray.ang.dto.entities.ProfileItem
import com.v2ray.ang.enums.EConfigType
import com.v2ray.ang.ui.compose.AppTopBar
import com.v2ray.ang.ui.compose.CollapsiblePreferenceGroupHeader
import com.v2ray.ang.ui.compose.PreferenceGroupHeader
import com.v2ray.ang.ui.compose.RockBlackTheme
import com.v2ray.ang.ui.compose.SettingsListItem
import com.v2ray.ang.ui.compose.SettingsMenuItem
import com.v2ray.ang.ui.compose.SettingsSwitchItem
import com.v2ray.ang.ui.main.MainDrawerContent
import com.v2ray.ang.ui.main.MainStatusCard
import com.v2ray.ang.ui.main.MainStatusCardUi
import com.v2ray.ang.ui.main.MainTopBar
import com.v2ray.ang.ui.main.ServerCard
import com.v2ray.ang.ui.main.ServerRowUiModel
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode
import java.io.File

/**
 * Renders the RockBlack main-screen building blocks to PNG files (dark and light) so the
 * redesign can be reviewed without a device. Output: build/outputs/screenshots/ (uploaded by
 * CI as the "android-screenshots" artifact). These tests only record images, they do not diff.
 */
@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [35], qualifiers = "w393dp-h852dp-xhdpi", application = Application::class)
class RockBlackScreenshotTest {

    @get:Rule
    val composeRule = createComposeRule()

    private val outDir: File =
        File(System.getProperty("rockblack.screenshots.dir") ?: "build/outputs/screenshots").apply { mkdirs() }

    private fun row(
        guid: String,
        name: String,
        type: EConfigType,
        protocol: String,
        address: String,
        delay: Long,
        badge: String = "",
    ) = ServerRowUiModel(
        guid = guid,
        profile = ProfileItem(configType = type, remarks = name),
        remarks = name,
        statistics = address,
        typeDescription = protocol,
        testDelayMillis = delay,
        subscriptionBadge = badge,
    )

    private val rows = listOf(
        row("1", "Amsterdam · NL-01", EConfigType.VLESS, "VLESS / xhttp / reality", "nl1.example.net : 443", 86, "R"),
        row("2", "Frankfurt · DE-02", EConfigType.VLESS, "VLESS / reality", "de2.example.net : 443", 142, "R"),
        row("3", "Helsinki · FI-01", EConfigType.TROJAN, "TROJAN / tls", "fi1.example.net : 443", 412, "R"),
        row("4", "Tokyo · JP-01", EConfigType.SHADOWSOCKS, "SHADOWSOCKS", "jp1.example.net : 8388", 920),
        row("5", "New York · US-03", EConfigType.HYSTERIA2, "HYSTERIA2 / tls", "us3.example.net : 8443", -1),
        row("6", "Warsaw · PL-01", EConfigType.VMESS, "VMESS / ws / tls", "pl1.example.net : 443", 0),
    )

    private fun capture(name: String, content: @Composable () -> Unit) {
        composeRule.setContent(content)
        composeRule.waitForIdle()
        val bitmap = composeRule.onRoot().captureToImage().asAndroidBitmap()
        File(outDir, "$name.png").outputStream().use { bitmap.compress(Bitmap.CompressFormat.PNG, 100, it) }
    }

    @Composable
    private fun MainScreenMock(dark: Boolean, running: Boolean, compact: Boolean) {
        RockBlackTheme(darkTheme = dark) {
            Column(
                Modifier
                    .fillMaxSize()
                    .background(RockBlackTheme.colors.bg)
            ) {
                MainTopBar(
                    isLoading = false,
                    showSearch = false,
                    searchQuery = "",
                    onSearchQueryChange = {},
                    onSearchClose = {},
                    onSearchToggle = {},
                    onMenuClick = {},
                    onAction = {},
                    onMoreMenuAction = {}
                )
                MainStatusCard(
                    state = MainStatusCardUi(
                        isRunning = running,
                        statusText = if (running) "Connection succeeded in 86 ms\n(NL) 203.0.113.24" else "Not connected",
                        showStatusLine = running,
                        serverName = rows[0].remarks,
                        protocol = rows[0].typeDescription,
                    ),
                    compact = compact,
                    onToggle = {},
                    onTest = {}
                )
                Column(
                    Modifier.padding(PaddingValues(top = 4.dp)),
                    verticalArrangement = Arrangement.spacedBy(0.dp)
                ) {
                    rows.forEach { r ->
                        Box(Modifier.padding(horizontal = 16.dp, vertical = 4.dp)) {
                            ServerCard(
                                row = r,
                                isSelected = r.guid == "1",
                                doubleColumnDisplay = false,
                                onSelect = {}, onShare = {}, onEdit = {}, onRemove = {}, onMore = {}
                            )
                        }
                    }
                }
            }
        }
    }

    @Test
    fun mainScreen_dark_connected() = capture("main_dark_connected") {
        MainScreenMock(dark = true, running = true, compact = false)
    }

    @Test
    fun mainScreen_dark_disconnected() = capture("main_dark_disconnected") {
        MainScreenMock(dark = true, running = false, compact = false)
    }

    @Test
    fun mainScreen_dark_compact() = capture("main_dark_compact_connected") {
        MainScreenMock(dark = true, running = true, compact = true)
    }

    @Test
    fun mainScreen_light_connected() = capture("main_light_connected") {
        MainScreenMock(dark = false, running = true, compact = false)
    }

    @Test
    fun mainScreen_light_disconnected() = capture("main_light_disconnected") {
        MainScreenMock(dark = false, running = false, compact = false)
    }

    private fun serverCards(name: String, dark: Boolean) = capture(name) {
        RockBlackTheme(darkTheme = dark) {
            Column(
                Modifier
                    .fillMaxWidth()
                    .background(RockBlackTheme.colors.bg)
                    .padding(vertical = 8.dp)
            ) {
                rows.take(5).forEachIndexed { i, r ->
                    Box(Modifier.padding(horizontal = 16.dp, vertical = 4.dp)) {
                        ServerCard(
                            row = r,
                            isSelected = i == 1,
                            doubleColumnDisplay = i == 4,
                            onSelect = {}, onShare = {}, onEdit = {}, onRemove = {}, onMore = {}
                        )
                    }
                }
            }
        }
    }

    @Test
    fun serverCards_dark() = serverCards("server_cards_dark", dark = true)

    @Test
    fun serverCards_light() = serverCards("server_cards_light", dark = false)

    private fun drawer(name: String, dark: Boolean) = capture(name) {
        RockBlackTheme(darkTheme = dark) {
            Box(
                Modifier
                    .fillMaxSize()
                    .background(RockBlackTheme.colors.bg)
            ) {
                MainDrawerContent(
                    drawerState = rememberDrawerState(DrawerValue.Open),
                    onNavigate = {}
                )
            }
        }
    }

    private fun settings(name: String, dark: Boolean) = capture(name) {
        RockBlackTheme(darkTheme = dark) {
            Column(
                Modifier
                    .fillMaxSize()
                    .background(RockBlackTheme.colors.bg)
            ) {
                AppTopBar(title = "Settings", onBackClick = {})
                CollapsiblePreferenceGroupHeader(title = "UI settings", expanded = true, onExpandedChange = {})
                SettingsSwitchItem(
                    title = "Enable speed display",
                    summary = "Show current speed in the notification",
                    checked = true,
                    onCheckedChange = {}
                )
                SettingsSwitchItem(
                    title = "Confirm before deleting",
                    summary = "Ask before removing a config",
                    checked = false,
                    onCheckedChange = {}
                )
                SettingsListItem(
                    title = "Theme",
                    entries = listOf("Follow system", "Light", "Dark"),
                    values = listOf("0", "1", "2"),
                    selectedValue = "2",
                    onSelected = {}
                )
                CollapsiblePreferenceGroupHeader(title = "VPN settings", expanded = false, onExpandedChange = {})
                PreferenceGroupHeader(title = "About")
                SettingsMenuItem(title = "Source code", subtitle = "github.com/RockBlack-VPN/rockblack", onClick = {})
            }
        }
    }

    @Test
    fun settings_dark() = settings("settings_dark", dark = true)

    @Test
    fun settings_light() = settings("settings_light", dark = false)

    @Test
    fun drawer_dark() = drawer("drawer_dark", dark = true)

    @Test
    fun drawer_light() = drawer("drawer_light", dark = false)
}
