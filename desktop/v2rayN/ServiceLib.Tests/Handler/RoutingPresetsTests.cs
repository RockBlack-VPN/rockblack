namespace ServiceLib.Tests.Handler;

public class RoutingPresetsTests
{
    private static readonly string[] ChinaMarkers =
    [
        "geosite:cn", "geoip:cn", "geolocation-cn", "223.5.5.5", "223.6.6.6", "119.29.29.29", "alidns", "doh.pub", "dnspod", "114.114.114.114"
    ];

    [Test]
    public async Task Builtins_ShouldBeGlobalRussiaAdsDirect_WithRussiaDefault()
    {
        await RoutingPresets.Builtins.Select(t => t.Id).SequenceEqual(
            [RoutingPresets.GlobalId, RoutingPresets.RussiaId, RoutingPresets.BlockAdsId, RoutingPresets.DirectId]).Should().BeTrue();
        await RoutingPresets.DefaultId.Should().BeEqualTo(RoutingPresets.RussiaId);
    }

    [Test]
    public async Task AllBuiltins_ShouldParse_AndContainNoChinaRules()
    {
        foreach (var preset in RoutingPresets.Builtins)
        {
            var json = RoutingPresets.GetRulesJson(preset);
            foreach (var marker in ChinaMarkers)
            {
                await json.Contains(marker, StringComparison.OrdinalIgnoreCase).Should().BeFalse();
            }

            var item = RoutingPresets.CreateItem(preset);
            await item.Id.Should().BeEqualTo(preset.Id);
            await (item.RuleNum > 0).Should().BeTrue();
            var rules = JsonUtils.Deserialize<List<RulesItem>>(item.RuleSet);
            await rules.Should().NotBeNull();
            await rules!.Count.Should().BeEqualTo(item.RuleNum);
            await rules.All(r => r.Id.IsNotEmpty()).Should().BeTrue();
        }
    }

    [Test]
    public async Task Russia_ShouldSendRussianTrafficDirect_AndBlockAds()
    {
        var rules = RoutingPresets.GetRules(RoutingPresets.Builtins.Single(t => t.Id == RoutingPresets.RussiaId));

        var directDomains = rules.Where(r => r.OutboundTag == Global.DirectTag).SelectMany(r => r.Domain ?? []).ToList();
        var directIps = rules.Where(r => r.OutboundTag == Global.DirectTag).SelectMany(r => r.Ip ?? []).ToList();
        var blocked = rules.Where(r => r.OutboundTag == Global.BlockTag).SelectMany(r => r.Domain ?? []).ToList();

        foreach (var domain in new[] { "geosite:category-ru", "domain:ru", "domain:su", "domain:xn--p1ai", "geosite:private" })
        {
            await directDomains.Contains(domain).Should().BeTrue();
        }
        await directIps.Contains("geoip:ru").Should().BeTrue();
        await directIps.Contains("geoip:private").Should().BeTrue();
        await blocked.Contains("geosite:category-ads-all").Should().BeTrue();
        await rules.Last().OutboundTag.Should().BeEqualTo(Global.ProxyTag);
        await rules.Last().Port.Should().BeEqualTo("0-65535");
    }

    [Test]
    public async Task Global_ShouldProxyEverythingExceptPrivate()
    {
        var rules = RoutingPresets.GetRules(RoutingPresets.Builtins.Single(t => t.Id == RoutingPresets.GlobalId));

        await rules.Any(r => r.OutboundTag == Global.BlockTag).Should().BeFalse();
        await rules.Where(r => r.OutboundTag == Global.DirectTag).SelectMany(r => (r.Ip ?? []).Concat(r.Domain ?? []))
            .All(v => v is "geoip:private" or "geosite:private").Should().BeTrue();
        await rules.Last().OutboundTag.Should().BeEqualTo(Global.ProxyTag);
    }

    [Test]
    public async Task BlockAds_ShouldBlockAdsAndProxyTheRest()
    {
        var rules = RoutingPresets.GetRules(RoutingPresets.Builtins.Single(t => t.Id == RoutingPresets.BlockAdsId));

        await rules.First().OutboundTag.Should().BeEqualTo(Global.BlockTag);
        await (rules.First().Domain ?? []).Contains("geosite:category-ads-all").Should().BeTrue();
        await rules.Last().OutboundTag.Should().BeEqualTo(Global.ProxyTag);
    }

    [Test]
    public async Task Direct_ShouldSendEverythingDirect()
    {
        var rules = RoutingPresets.GetRules(RoutingPresets.Builtins.Single(t => t.Id == RoutingPresets.DirectId));

        await rules.Count.Should().BeEqualTo(1);
        await rules[0].OutboundTag.Should().BeEqualTo(Global.DirectTag);
        await rules[0].Port.Should().BeEqualTo("0-65535");
    }

    [Test]
    [Arguments("V4-绕过大陆(Whitelist)", true)]
    [Arguments("V4-黑名单(Blacklist)", true)]
    [Arguments("V4-全局(Global)", true)]
    [Arguments("V3-绕过大陆(Whitelist)", true)]
    [Arguments("V4-Whitelist", true)]
    [Arguments("my custom", false)]
    [Arguments("runetfreedom-Russia", false)]
    public async Task IsLegacyUpstreamBuiltin_ShouldMatchOnlyUpstreamTemplates(string remarks, bool expected)
    {
        var item = new RoutingItem { Id = "x", Remarks = remarks };

        await RoutingPresets.IsLegacyUpstreamBuiltin(item).Should().BeEqualTo(expected);
    }

    [Test]
    public async Task IsLegacyUpstreamBuiltin_ShouldNeverMatchRockBlackBuiltins()
    {
        foreach (var preset in RoutingPresets.Builtins)
        {
            await RoutingPresets.IsLegacyUpstreamBuiltin(RoutingPresets.CreateItem(preset)).Should().BeFalse();
        }
    }

    [Test]
    public async Task BuiltinsSortBeforeCustomProfiles()
    {
        foreach (var preset in RoutingPresets.Builtins)
        {
            await (RoutingPresets.SortOf(preset) < 0).Should().BeTrue();
        }
    }

    [Test]
    public async Task DnsDefaults_ShouldNotBeChinese()
    {
        var lists = Global.DomainDirectDNSAddress
            .Concat(Global.DomainPureIPDNSAddress)
            .Concat(Global.DomainDNS4PreResolveAddress)
            .Concat(Global.DomainRemoteDNSAddress)
            .ToList();
        foreach (var marker in ChinaMarkers)
        {
            await lists.Any(t => t.Contains(marker, StringComparison.OrdinalIgnoreCase)).Should().BeFalse();
        }
        await Global.DomainDirectDNSAddress[0].Should().BeEqualTo("77.88.8.8");
        await ConfigHandler.InitBuiltinSimpleDNS().DirectDNS.Should().BeEqualTo("77.88.8.8");
        await ConfigHandler.InitBuiltinSimpleDNS().BootstrapDNS.Should().BeEqualTo("77.88.8.8");
        await Global.ExpectedIPs.Contains("geoip:cn").Should().BeFalse();

        foreach (var sample in new[] { Global.DNSV2rayNormalFileName, Global.DNSSingboxNormalFileName, Global.TunSingboxDNSFileName, Global.ClashMixinYaml })
        {
            var text = EmbedUtils.GetEmbedText(sample);
            foreach (var marker in ChinaMarkers)
            {
                await text.Contains(marker, StringComparison.OrdinalIgnoreCase).Should().BeFalse();
            }
        }
    }
}
