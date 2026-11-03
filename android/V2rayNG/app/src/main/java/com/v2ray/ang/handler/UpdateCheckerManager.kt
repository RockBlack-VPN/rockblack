package com.v2ray.ang.handler

import android.os.Build
import com.v2ray.ang.AppConfig
import com.v2ray.ang.BuildConfig
import com.v2ray.ang.dto.CheckUpdateResult
import com.v2ray.ang.dto.GitHubRelease
import com.v2ray.ang.dto.UrlContentRequest
import com.v2ray.ang.util.HttpUtil
import com.v2ray.ang.util.JsonUtil
import com.v2ray.ang.util.LogUtil
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

/**
 * RockBlack: the monorepo RockBlack-VPN/rockblack publishes desktop and Android
 * releases side by side, so `releases/latest` may point to a desktop release.
 * The checker lists releases and only considers tags `android-vX.Y.Z` with the
 * assets `RockBlack-<abi>.apk` / `RockBlack.apk` (see branding/README.md).
 */
object UpdateCheckerManager {
    private const val RELEASES_PAGE_SIZE = 50

    suspend fun checkForUpdate(includePreRelease: Boolean = false): CheckUpdateResult = withContext(Dispatchers.IO) {
        val url = "${AppConfig.APP_API_URL}?per_page=$RELEASES_PAGE_SIZE"

        val proxyUsername = SettingsManager.getSocksUsername()
        val proxyPassword = SettingsManager.getSocksPassword()

        var response = HttpUtil.getUrlContent(
            UrlContentRequest(
                url = url,
                timeout = 5000
            )
        )
        if (response.isNullOrEmpty()) {
            val httpPort = SettingsManager.getHttpPort()
            response = HttpUtil.getUrlContent(
                UrlContentRequest(
                    url = url,
                    timeout = 5000,
                    httpPort = httpPort,
                    proxyUsername = proxyUsername,
                    proxyPassword = proxyPassword
                )
            )
                ?: throw IllegalStateException("Failed to get response")
        }

        val releases = JsonUtil.fromJsonSafe(response, Array<GitHubRelease>::class.java)?.toList()
            ?: throw IllegalStateException("Failed to parse releases")
        val latestRelease = selectLatestRelease(releases, includePreRelease)
            ?: return@withContext CheckUpdateResult(hasUpdate = false)

        val latestVersion = versionOf(latestRelease)
        LogUtil.i(
            AppConfig.TAG,
            "Found latest version: $latestVersion (current: ${BuildConfig.VERSION_NAME})"
        )

        return@withContext if (compareVersions(latestVersion, BuildConfig.VERSION_NAME) > 0) {
            val isFdroid = BuildConfig.APPLICATION_ID.contains("fdroid", ignoreCase = true)
            val downloadUrl = selectApkAsset(latestRelease, Build.SUPPORTED_ABIS[0], isFdroid)
                ?.browserDownloadUrl
                ?: throw IllegalStateException("No compatible APK found")
            CheckUpdateResult(
                hasUpdate = true,
                latestVersion = latestVersion,
                releaseNotes = latestRelease.body,
                downloadUrl = downloadUrl,
                isPreRelease = latestRelease.prerelease
            )
        } else {
            CheckUpdateResult(hasUpdate = false)
        }
    }

    /** Version string of an `android-vX.Y.Z` release. */
    internal fun versionOf(release: GitHubRelease): String =
        release.tagName.removePrefix(AppConfig.APP_RELEASE_TAG_PREFIX)

    /** Newest non-draft Android release; pre-releases only when [includePreRelease]. */
    internal fun selectLatestRelease(
        releases: List<GitHubRelease>,
        includePreRelease: Boolean
    ): GitHubRelease? = releases
        .asSequence()
        .filter { it.tagName.startsWith(AppConfig.APP_RELEASE_TAG_PREFIX) }
        .filter { !it.draft }
        .filter { includePreRelease || !it.prerelease }
        .maxWithOrNull { a, b -> compareVersions(versionOf(a), versionOf(b)) }

    /**
     * Picks `RockBlack-<abi>.apk`, falling back to the universal `RockBlack.apk`.
     * F-Droid builds use a different applicationId and only accept `RockBlack-fdroid-*` assets.
     */
    internal fun selectApkAsset(release: GitHubRelease, abi: String, isFdroid: Boolean): GitHubRelease.Asset? {
        val base = if (isFdroid) "${AppConfig.APP_APK_NAME}-fdroid" else AppConfig.APP_APK_NAME
        val candidates = listOf("$base-$abi.apk", if (isFdroid) "$base-universal.apk" else "$base.apk")
        return candidates.firstNotNullOfOrNull { name ->
            release.assets.firstOrNull { it.name.equals(name, ignoreCase = true) }
        }
    }

    internal fun compareVersions(version1: String, version2: String): Int {
        val v1 = version1.substringBefore('-').split(".")
        val v2 = version2.substringBefore('-').split(".")

        for (i in 0 until maxOf(v1.size, v2.size)) {
            val num1 = v1.getOrNull(i)?.toIntOrNull() ?: 0
            val num2 = v2.getOrNull(i)?.toIntOrNull() ?: 0
            if (num1 != num2) return num1 - num2
        }
        return 0
    }
}
