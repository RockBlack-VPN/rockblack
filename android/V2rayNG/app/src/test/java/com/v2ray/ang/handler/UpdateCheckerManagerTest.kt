package com.v2ray.ang.handler

import com.v2ray.ang.dto.GitHubRelease
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test

class UpdateCheckerManagerTest {
    private fun asset(name: String) = GitHubRelease.Asset(name, "https://example.invalid/$name")

    private fun release(
        tag: String,
        prerelease: Boolean = false,
        draft: Boolean = false,
        assets: List<String> = emptyList()
    ) = GitHubRelease(
        tagName = tag,
        body = "notes",
        assets = assets.map(::asset),
        prerelease = prerelease,
        draft = draft
    )

    @Test
    fun selectsNewestAndroidReleaseIgnoringDesktopAndLatestMirror() {
        val releases = listOf(
            release("desktop-v9.9.9"),
            release("latest"),
            release("android-v2.3.15"),
            release("android-v2.3.16"),
            release("android-v2.3.9")
        )
        assertEquals("android-v2.3.16", UpdateCheckerManager.selectLatestRelease(releases, false)?.tagName)
    }

    @Test
    fun skipsDraftsAndPreReleasesUnlessRequested() {
        val releases = listOf(
            release("android-v2.3.15"),
            release("android-v2.4.0", prerelease = true),
            release("android-v2.5.0", draft = true)
        )
        assertEquals("android-v2.3.15", UpdateCheckerManager.selectLatestRelease(releases, false)?.tagName)
        assertEquals("android-v2.4.0", UpdateCheckerManager.selectLatestRelease(releases, true)?.tagName)
    }

    @Test
    fun noAndroidReleaseGivesNull() {
        assertNull(UpdateCheckerManager.selectLatestRelease(emptyList(), true))
        assertNull(UpdateCheckerManager.selectLatestRelease(listOf(release("desktop-v7.0.0")), true))
    }

    @Test
    fun versionStripsTagPrefix() {
        assertEquals("2.3.15", UpdateCheckerManager.versionOf(release("android-v2.3.15")))
    }

    @Test
    fun prefersAbiApkThenUniversal() {
        val full = release(
            "android-v2.3.15",
            assets = listOf("RockBlack.apk", "RockBlack-arm64-v8a.apk", "RockBlack-armeabi-v7a.apk", "SHA256SUMS")
        )
        assertEquals("RockBlack-arm64-v8a.apk", UpdateCheckerManager.selectApkAsset(full, "arm64-v8a", false)?.name)
        assertEquals("RockBlack.apk", UpdateCheckerManager.selectApkAsset(full, "x86_64", false)?.name)
        assertNull(UpdateCheckerManager.selectApkAsset(full, "arm64-v8a", true))
        assertNull(UpdateCheckerManager.selectApkAsset(release("android-v1.0.0"), "arm64-v8a", false))
    }

    @Test
    fun comparesVersionsNumerically() {
        assertTrue(UpdateCheckerManager.compareVersions("2.3.16", "2.3.15") > 0)
        assertTrue(UpdateCheckerManager.compareVersions("2.10.0", "2.9.9") > 0)
        assertEquals(0, UpdateCheckerManager.compareVersions("2.3", "2.3.0"))
        assertTrue(UpdateCheckerManager.compareVersions("2.3.15", "2.3.16-beta") < 0)
    }
}
