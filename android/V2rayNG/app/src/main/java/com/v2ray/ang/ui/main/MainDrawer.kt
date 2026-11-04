package com.v2ray.ang.ui.main

import androidx.annotation.DrawableRes
import androidx.annotation.StringRes
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.DrawerState
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalDrawerSheet
import androidx.compose.material3.NavigationDrawerItem
import androidx.compose.material3.NavigationDrawerItemDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.v2ray.ang.R
import com.v2ray.ang.ui.compose.AppDivider
import com.v2ray.ang.ui.compose.RockBlackTheme
import com.v2ray.ang.ui.compose.verticalScrollbar

enum class MainDestination(@DrawableRes val iconRes: Int, @StringRes val labelRes: Int) {
    Subscriptions(R.drawable.ic_subscriptions_24dp, R.string.title_sub_setting),
    PerAppProxy(R.drawable.ic_per_apps_24dp, R.string.per_app_proxy_settings),
    Routing(R.drawable.ic_routing_24dp, R.string.routing_settings_title),
    UserAssets(R.drawable.ic_file_24dp, R.string.title_user_asset_setting),
    Settings(R.drawable.ic_settings_24dp, R.string.title_settings),
    // RockBlack: upstream "Promotion" entry now opens the RockBlack website.
    Promotion(R.drawable.ic_website_24dp, R.string.title_website),
    Logcat(R.drawable.ic_logcat_24dp, R.string.title_logcat),
    CheckUpdate(R.drawable.ic_check_update_24dp, R.string.update_check_for_update),
    BackupRestore(R.drawable.ic_restore_24dp, R.string.title_configuration_backup_restore),
    About(R.drawable.ic_about_24dp, R.string.title_about)
}

private val primaryDrawerItems = listOf(
    MainDestination.Subscriptions,
    MainDestination.PerAppProxy,
    MainDestination.Routing,
    MainDestination.UserAssets,
    MainDestination.Settings
)

private val drawerItems = primaryDrawerItems + listOf(
    MainDestination.Promotion,
    MainDestination.Logcat,
    MainDestination.CheckUpdate,
    MainDestination.BackupRestore,
    MainDestination.About
)

/** RockBlack UI 3.0: global tools that used to live in the top bar overflow menu. */
enum class MainDrawerTool(@DrawableRes val iconRes: Int, @StringRes val labelRes: Int) {
    Search(R.drawable.ic_search_24dp, R.string.menu_item_search),
    UpdateSubscriptions(R.drawable.ic_refresh_24dp, R.string.title_sub_update),
    PingAll(R.drawable.rb_ic_speed, R.string.rb_group_ping),
    RestartService(R.drawable.ic_restore_24dp, R.string.title_service_restart),
}

@Composable
fun MainDrawerContent(
    drawerState: DrawerState,
    onNavigate: (MainDestination) -> Unit,
    onTool: ((MainDrawerTool) -> Unit)? = null,
) {
    val drawerScrollState = rememberScrollState()
    val c = RockBlackTheme.colors

    ModalDrawerSheet(
        drawerState = drawerState,
        modifier = Modifier.fillMaxWidth(0.78f),
        drawerContainerColor = c.surface,
        drawerShape = RoundedCornerShape(topEnd = 24.dp, bottomEnd = 24.dp)
    ) {
        Column(
            modifier = Modifier
                .verticalScroll(drawerScrollState)
                .verticalScrollbar(drawerScrollState)
        ) {
            // RockBlack header: logo + wordmark on the card surface.
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(start = 24.dp, end = 16.dp, top = 28.dp, bottom = 20.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Image(
                    painter = painterResource(R.drawable.ic_brand_logo),
                    contentDescription = null,
                    modifier = Modifier.size(52.dp)
                )
                Spacer(Modifier.width(14.dp))
                Text(
                    text = stringResource(R.string.app_name),
                    style = MaterialTheme.typography.titleLarge,
                    color = c.text
                )
            }
            AppDivider(Modifier.padding(horizontal = 16.dp))
            Spacer(Modifier.height(8.dp))
            val itemColors = NavigationDrawerItemDefaults.colors(
                unselectedContainerColor = Color.Transparent,
                selectedContainerColor = c.primarySoft,
                unselectedIconColor = c.textMuted,
                selectedIconColor = c.primary,
                unselectedTextColor = c.text,
                selectedTextColor = c.primary
            )
            drawerItems.forEachIndexed { index, item ->
                if (index == primaryDrawerItems.size) {
                    AppDivider(Modifier.padding(horizontal = 16.dp, vertical = 8.dp))
                }
                NavigationDrawerItem(
                    label = { Text(stringResource(item.labelRes), style = MaterialTheme.typography.bodyLarge) },
                    selected = false,
                    onClick = { onNavigate(item) },
                    icon = { Icon(painterResource(item.iconRes), contentDescription = null, modifier = Modifier.size(22.dp)) },
                    shape = MaterialTheme.shapes.small,
                    colors = itemColors,
                    modifier = Modifier
                        .padding(NavigationDrawerItemDefaults.ItemPadding)
                        .height(48.dp)
                )
            }
            if (onTool != null) {
                AppDivider(Modifier.padding(horizontal = 16.dp, vertical = 8.dp))
                Text(
                    text = stringResource(R.string.rb_tools),
                    style = MaterialTheme.typography.labelMedium,
                    color = c.textMuted,
                    modifier = Modifier.padding(start = 28.dp, top = 4.dp, bottom = 4.dp)
                )
                MainDrawerTool.entries.forEach { tool ->
                    NavigationDrawerItem(
                        label = { Text(stringResource(tool.labelRes), style = MaterialTheme.typography.bodyLarge) },
                        selected = false,
                        onClick = { onTool(tool) },
                        icon = { Icon(painterResource(tool.iconRes), contentDescription = null, modifier = Modifier.size(22.dp)) },
                        shape = MaterialTheme.shapes.small,
                        colors = itemColors,
                        modifier = Modifier
                            .padding(NavigationDrawerItemDefaults.ItemPadding)
                            .height(48.dp)
                    )
                }
            }
            Spacer(Modifier.height(16.dp))
        }
    }
}
