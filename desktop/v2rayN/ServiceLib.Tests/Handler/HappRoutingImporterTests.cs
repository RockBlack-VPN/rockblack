namespace ServiceLib.Tests.Handler;

public class HappRoutingImporterTests
{
    private const string ProfileJson = """
        {
          "Name": "Provider RU",
          "GlobalProxy": "true",
          "DomainStrategy": "IPIfNonMatch",
          "DirectSites": ["geosite:category-ru", "domain:ru"],
          "DirectIp": ["geoip:ru"],
          "ProxySites": ["geosite:youtube"],
          "ProxyIp": [],
          "BlockSites": ["geosite:category-ads-all"],
          "BlockIp": ["1.2.3.4/32"],
          "RemoteDNSIP": "1.1.1.1"
        }
        """;

    private static string Encode(string json, bool urlSafe = false)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return urlSafe ? b64.Replace('+', '-').Replace('/', '_').TrimEnd('=') : b64;
    }

    [Test]
    [Arguments("add", HappRoutingImporter.EAction.Add)]
    [Arguments("onadd", HappRoutingImporter.EAction.OnAdd)]
    [Arguments("ONADD", HappRoutingImporter.EAction.OnAdd)]
    public async Task ParseLink_ShouldDecodeAddAndOnAdd(string verb, HappRoutingImporter.EAction expected)
    {
        var (action, json) = HappRoutingImporter.ParseLink($"happ://routing/{verb}/{Encode(ProfileJson)}");

        await action.Should().BeEqualTo(expected);
        await json.Should().NotBeNull();
        await json!.Contains("Provider RU").Should().BeTrue();
    }

    [Test]
    public async Task ParseLink_ShouldAcceptUrlSafeBase64()
    {
        var (action, json) = HappRoutingImporter.ParseLink($"happ://routing/onadd/{Encode(ProfileJson, true)}");

        await action.Should().BeEqualTo(HappRoutingImporter.EAction.OnAdd);
        await json!.Contains("geosite:category-ads-all").Should().BeTrue();
    }

    [Test]
    public async Task ParseLink_ShouldRecogniseOffAndIgnoreOtherValues()
    {
        await HappRoutingImporter.ParseLink("happ://routing/off").Action.Should().BeEqualTo(HappRoutingImporter.EAction.Off);
        await HappRoutingImporter.ParseLink("https://example.com/routing").Action.Should().BeEqualTo(HappRoutingImporter.EAction.None);
        await HappRoutingImporter.ParseLink("happ://routing/unknown/abc").Action.Should().BeEqualTo(HappRoutingImporter.EAction.None);
        await HappRoutingImporter.ParseLink(null).Action.Should().BeEqualTo(HappRoutingImporter.EAction.None);
    }

    [Test]
    public async Task BuildRoutingItem_ShouldMapRulesInAndroidOrder()
    {
        var item = HappRoutingImporter.BuildRoutingItem("sub1", ProfileJson, out var error);

        await error.Should().BeNull();
        await item.Should().NotBeNull();
        await item!.Id.Should().BeEqualTo("rb-happ-sub1");
        await item.Remarks.Should().BeEqualTo("Provider RU");
        await item.DomainStrategy.Should().BeEqualTo("IPIfNonMatch");

        var rules = JsonUtils.Deserialize<List<RulesItem>>(item.RuleSet)!;
        await item.RuleNum.Should().BeEqualTo(rules.Count);
        var order = rules.Select(r => r.OutboundTag).ToList();
        // block sites, block ip, private ip, private domains, proxy sites, direct sites, direct ip, final (ProxyIp empty → skipped)
        await order.SequenceEqual(new[] { "block", "block", "direct", "direct", "proxy", "direct", "direct", "proxy" }).Should().BeTrue();
        await rules[0].Domain!.Contains("geosite:category-ads-all").Should().BeTrue();
        await rules[1].Ip!.Contains("1.2.3.4/32").Should().BeTrue();
        await rules[2].Ip!.Contains("geoip:private").Should().BeTrue();
        await rules[4].Domain!.Contains("geosite:youtube").Should().BeTrue();
        await rules[5].Domain!.SequenceEqual(new[] { "geosite:category-ru", "domain:ru" }).Should().BeTrue();
        await rules[6].Ip!.Contains("geoip:ru").Should().BeTrue();
        await rules[^1].Port.Should().BeEqualTo("0-65535");
    }

    [Test]
    [Arguments("false")]
    [Arguments("\"false\"")]
    public async Task BuildRoutingItem_GlobalProxyFalse_ShouldEndDirect(string value)
    {
        var json = $$"""{ "Name": "Direct by default", "GlobalProxy": {{value}}, "ProxySites": ["geosite:google"] }""";

        var item = HappRoutingImporter.BuildRoutingItem("s", json, out _);
        var rules = JsonUtils.Deserialize<List<RulesItem>>(item!.RuleSet)!;

        await rules[^1].OutboundTag.Should().BeEqualTo("direct");
        await rules.Any(r => r.OutboundTag == "proxy" && (r.Domain ?? []).Contains("geosite:google")).Should().BeTrue();
    }

    [Test]
    public async Task BuildRoutingItem_ShouldIgnoreUnknownDomainStrategyAndDefaultName()
    {
        var item = HappRoutingImporter.BuildRoutingItem("s", """{ "DomainStrategy": "Bogus" }""", out _);

        await item!.DomainStrategy.Should().BeEqualTo(string.Empty);
        await item.Remarks.Should().BeEqualTo("Happ routing");
        // no lists: private direct (2 rules) + final proxy (GlobalProxy defaults to true)
        var rules = JsonUtils.Deserialize<List<RulesItem>>(item.RuleSet)!;
        await rules.Count.Should().BeEqualTo(3);
        await rules[^1].OutboundTag.Should().BeEqualTo("proxy");
    }

    [Test]
    public async Task BuildRoutingItem_InvalidJson_ShouldFail()
    {
        var item = HappRoutingImporter.BuildRoutingItem("s", "not json", out var error);

        await item.Should().BeNull();
        await error.Should().NotBeNull();
    }

    [Test]
    public async Task SubscriptionInfo_ShouldCarryRoutingFromHeaderOrBody()
    {
        var link = $"happ://routing/onadd/{Encode(ProfileJson)}";

        var fromHeader = SubscriptionInfoParser.Parse("s", new Dictionary<string, string> { ["routing"] = link }, null);
        var fromBody = SubscriptionInfoParser.Parse("s", null, $"routing: {link}\nvless://a@b.example:443#B\n");
        var fromComment = SubscriptionInfoParser.Parse("s", null, $"#routing: {link}\n");

        await fromHeader.Routing.Should().BeEqualTo(link);
        await fromBody.Routing.Should().BeEqualTo(link);
        await fromComment.Routing.Should().BeEqualTo(link);
    }

    [Test]
    public async Task HappProfiles_ShouldBeRecognisedAndDescribed()
    {
        await HappRoutingImporter.IsHappRouting("rb-happ-abc").Should().BeTrue();
        await HappRoutingImporter.IsHappRouting(RoutingPresets.RussiaId).Should().BeFalse();
        await RoutingPresets.GetDescription(new RoutingItem { Id = "rb-happ-abc" }).Should().BeEqualTo(ResUI.RbRoutingFromSubscription);
        await RoutingPresets.IsLegacyUpstreamBuiltin(new RoutingItem { Id = "rb-happ-abc", Remarks = "Provider RU" }).Should().BeFalse();
    }
}
