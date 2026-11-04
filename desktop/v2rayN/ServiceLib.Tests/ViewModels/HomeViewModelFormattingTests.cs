using System.Globalization;
using ServiceLib.ViewModels;

namespace ServiceLib.Tests.ViewModels;

public class HomeViewModelFormattingTests
{
    [Test]
    [Arguments("🇩🇪 Germany", "de", "Germany")]
    [Arguments("🇳🇱Netherlands - 1", "nl", "Netherlands - 1")]
    [Arguments("🇺🇸 | USA Seattle", "us", "USA Seattle")]
    [Arguments("Germany", "", "Germany")]
    public async Task SplitLeadingFlag_ShouldSeparateFlagAndName(string remarks, string code, string name)
    {
        var (flag, flagCode, rest) = CountryExtension.SplitLeadingFlag(remarks);

        await flagCode.Should().BeEqualTo(code);
        await rest.Should().BeEqualTo(name);
        await (flag.Length == (code.Length > 0 ? 4 : 0)).Should().BeTrue();
    }

    [Test]
    [Arguments("🇩🇪 Germany", "de")]
    [Arguments("Germany", "de")]
    [Arguments("Германия-2", "de")]
    [Arguments("Amsterdam - 1", "nl")]
    [Arguments("DE-01 Fast", "de")]
    [Arguments("[NL] Premium", "nl")]
    [Arguments("us2", "us")]
    [Arguments("Server 🇫🇮 Helsinki", "fi")]
    [Arguments("Latvia", "lv")]
    [Arguments("Moldova", "md")]
    [Arguments("My best server", "")]
    [Arguments("VLESS Reality", "")]
    public async Task DetectCountryCode_ShouldFindCountry(string remarks, string expected)
    {
        await CountryExtension.DetectCountryCode(remarks).Should().BeEqualTo(expected);
    }

    [Test]
    public async Task CodeToFlagEmoji_ShouldBuildRegionalIndicators()
    {
        await CountryExtension.CodeToFlagEmoji("de").Should().BeEqualTo("🇩🇪");
        await CountryExtension.CodeToFlagEmoji("x").Should().BeEqualTo(string.Empty);
    }

    [Test]
    [Arguments(0, EDelayState.Unknown)]
    [Arguments(-1, EDelayState.Timeout)]
    [Arguments(42, EDelayState.Good)]
    [Arguments(299, EDelayState.Good)]
    [Arguments(300, EDelayState.Warning)]
    [Arguments(800, EDelayState.Warning)]
    [Arguments(801, EDelayState.Bad)]
    public async Task GetDelayState_ShouldFollowDesignThresholds(int delay, EDelayState expected)
    {
        await ServerItemViewModel.GetDelayState(delay, false).Should().BeEqualTo(expected);
    }

    [Test]
    public async Task FormatDelay_ShouldUseLocalisedTexts()
    {
        var culture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            await ServerItemViewModel.FormatDelay(42, false).Should().BeEqualTo("42 ms");
            await ServerItemViewModel.FormatDelay(0, false).Should().BeEqualTo("n/a");
            await ServerItemViewModel.FormatDelay(-1, false).Should().BeEqualTo("Timeout");
            await ServerItemViewModel.FormatDelay(42, true).Should().BeEqualTo("…");
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }

    [Test]
    public async Task BuildSubtitle_ShouldDescribeProtocolAndSecurity()
    {
        await ServerItemViewModel.BuildSubtitle(EConfigType.VLESS, "reality", "raw").Should().BeEqualTo("VLESS · Reality");
        await ServerItemViewModel.BuildSubtitle(EConfigType.VMess, "tls", "ws").Should().BeEqualTo("VMess · TLS · ws");
        await ServerItemViewModel.BuildSubtitle(EConfigType.Shadowsocks, "", "").Should().BeEqualTo("Shadowsocks");
    }

    [Test]
    [Arguments(0L, "0 B")]
    [Arguments(3328599654L, "3.1 GB")]
    [Arguments(53687091200L, "50 GB")]
    [Arguments(1536L, "1.5 KB")]
    public async Task FormatBytes_ShouldBeCompact(long bytes, string expected)
    {
        await ServerGroupViewModel.FormatBytes(bytes).Should().BeEqualTo(expected);
    }

    [Test]
    public async Task FormatTraffic_ShouldShowInfinityForUnlimited()
    {
        await ServerGroupViewModel.FormatTraffic(3221225472L, 0).Should().BeEqualTo("3 GB / ∞");
        await ServerGroupViewModel.FormatTraffic(3328599654L, 53687091200L).Should().BeEqualTo("3.1 GB / 50 GB");
    }

    [Test]
    public async Task ExpiryBanner_ShouldAppearWithinThreeDays()
    {
        var culture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var now = new DateTime(2026, 11, 4, 12, 0, 0);

            var far = ServerGroupViewModel.GetExpiryBanner(now.AddDays(10), now);
            await far.Show.Should().BeFalse();

            var soon = ServerGroupViewModel.GetExpiryBanner(now.AddDays(3), now);
            await soon.Show.Should().BeTrue();
            await soon.Text.Should().BeEqualTo("Your subscription expires in 3 days");

            var today = ServerGroupViewModel.GetExpiryBanner(now.AddHours(3), now);
            await today.Text.Should().BeEqualTo("Your subscription expires today");

            var expired = ServerGroupViewModel.GetExpiryBanner(now.AddMinutes(-1), now);
            await expired.Show.Should().BeTrue();
            await expired.Text.Should().BeEqualTo("Subscription has expired");

            await ServerGroupViewModel.GetExpiryBanner(null, now).Show.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }

    [Test]
    public async Task FormatDays_ShouldUseRussianPlurals()
    {
        var culture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ru");
            await ServerGroupViewModel.FormatDays(1).Should().BeEqualTo("1 день");
            await ServerGroupViewModel.FormatDays(3).Should().BeEqualTo("3 дня");
            await ServerGroupViewModel.FormatDays(5).Should().BeEqualTo("5 дней");
            await ServerGroupViewModel.FormatDays(11).Should().BeEqualTo("11 дней");
            await ServerGroupViewModel.FormatDays(21).Should().BeEqualTo("21 день");
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }

    [Test]
    public async Task FormatAutoUpdate_ShouldUseHoursOrMinutes()
    {
        var culture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            await ServerGroupViewModel.FormatAutoUpdate(720).Should().BeEqualTo("auto 12 h");
            await ServerGroupViewModel.FormatAutoUpdate(30).Should().BeEqualTo("auto 30 min");
            await ServerGroupViewModel.FormatAutoUpdate(0).Should().BeEqualTo(string.Empty);
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }

    [Test]
    [Arguments("https://t.me/rockblack", true)]
    [Arguments("tg://resolve?domain=rockblack", true)]
    [Arguments("https://my.rockblack.tech/support", false)]
    [Arguments("", false)]
    public async Task IsTelegramUrl_ShouldDetectTelegram(string url, bool expected)
    {
        await ServerGroupViewModel.IsTelegramUrl(url).Should().BeEqualTo(expected);
    }

    [Test]
    public async Task FormatSessionTime_ShouldBeHoursMinutesSeconds()
    {
        await HomeViewModel.FormatSessionTime(new TimeSpan(0, 16, 28)).Should().BeEqualTo("00:16:28");
        await HomeViewModel.FormatSessionTime(new TimeSpan(1, 2, 3, 4)).Should().BeEqualTo("26:03:04");
        await HomeViewModel.FormatSpeed(1229).Should().BeEqualTo("1.2 MB/s");
        await HomeViewModel.FormatSpeed(140).Should().BeEqualTo("140 KB/s");
    }
}
