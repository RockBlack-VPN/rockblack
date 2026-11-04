namespace ServiceLib.Handler;

/// <summary>
/// RockBlack built-in routing profiles (branding/DESIGN.md → Routing):
/// Global, Russia (default), Block ads only, Direct. They replace the upstream China-oriented
/// "Whitelist / Blacklist / Global" templates. Built-ins use fixed ids so they can be recognised,
/// localised in the UI and re-created when missing.
/// </summary>
public static class RoutingPresets
{
    public const string GlobalId = "rb-global";
    public const string RussiaId = "rb-russia";
    public const string BlockAdsId = "rb-ads";
    public const string DirectId = "rb-direct";

    /// <summary>The profile activated on first start and after migrating an upstream config.</summary>
    public const string DefaultId = RussiaId;

    /// <summary>Bump when the embedded rule sets change; built-ins are then rewritten once.</summary>
    public const int Version = 1;

    public sealed record Preset(string Id, string Remarks, string Resource);

    public static readonly IReadOnlyList<Preset> Builtins =
    [
        new(GlobalId, "Global", "global"),
        new(RussiaId, "Russia", "russia"),
        new(BlockAdsId, "Block ads only", "ads"),
        new(DirectId, "Direct", "direct"),
    ];

    public static bool IsBuiltin(string? id)
    {
        return id.IsNotEmpty() && Builtins.Any(t => t.Id == id);
    }

    /// <summary>
    /// Upstream v2rayN built-in templates ("V4-绕过大陆(Whitelist)", "V4-黑名单(Blacklist)", "V4-全局(Global)", older V3-…).
    /// Profiles imported from template URLs carry a URL-based version prefix and are not matched.
    /// </summary>
    public static bool IsLegacyUpstreamBuiltin(RoutingItem item)
    {
        if (item == null || IsBuiltin(item.Id) || item.Remarks.IsNullOrEmpty())
        {
            return false;
        }
        var remarks = item.Remarks;
        return remarks.Contains("绕过大陆")
               || remarks.Contains("黑名单")
               || remarks.Contains("全局(Global)")
               || Regex.IsMatch(remarks, @"^V\d+-(Whitelist|Blacklist|Global)$");
    }

    /// <summary>Returns the embedded rule set JSON of a preset.</summary>
    public static string GetRulesJson(Preset preset)
    {
        return EmbedUtils.GetEmbedText(Global.CustomRoutingFileName + preset.Resource);
    }

    /// <summary>Parses the embedded rules of a preset (fresh ids each call).</summary>
    public static List<RulesItem> GetRules(Preset preset)
    {
        var rules = JsonUtils.Deserialize<List<RulesItem>>(GetRulesJson(preset))
                    ?? throw new InvalidDataException($"The built-in routing rules '{preset.Resource}' are invalid.");
        foreach (var rule in rules)
        {
            rule.Id = Utils.GetGuid(false);
        }
        return rules;
    }

    /// <summary>Creates a routing item for a preset (not saved).</summary>
    public static RoutingItem CreateItem(Preset preset)
    {
        var rules = GetRules(preset);
        return new RoutingItem
        {
            Id = preset.Id,
            Remarks = preset.Remarks,
            Url = string.Empty,
            RuleNum = rules.Count,
            RuleSet = JsonUtils.Serialize(rules, false),
            Enabled = true,
            Locked = false,
            Sort = SortOf(preset),
        };
    }

    /// <summary>Built-ins sort before user profiles in the order of <see cref="Builtins"/>.</summary>
    public static int SortOf(Preset preset)
    {
        return Builtins.ToList().IndexOf(preset) - Builtins.Count;
    }

    /// <summary>Localised display name of a routing profile.</summary>
    public static string GetDisplayName(RoutingItem? item)
    {
        if (item == null)
        {
            return string.Empty;
        }
        return item.Id switch
        {
            GlobalId => ResUI.RbRoutingGlobal,
            RussiaId => ResUI.RbRoutingRussia,
            BlockAdsId => ResUI.RbRoutingBlockAds,
            DirectId => ResUI.RbRoutingDirect,
            _ => item.Remarks ?? string.Empty,
        };
    }

    /// <summary>Localised one-line description of a built-in profile ("" for custom profiles).</summary>
    public static string GetDescription(RoutingItem? item)
    {
        return item?.Id switch
        {
            GlobalId => ResUI.RbRoutingGlobalDesc,
            RussiaId => ResUI.RbRoutingRussiaDesc,
            BlockAdsId => ResUI.RbRoutingBlockAdsDesc,
            DirectId => ResUI.RbRoutingDirectDesc,
            _ when HappRoutingImporter.IsHappRouting(item?.Id) => ResUI.RbRoutingFromSubscription,
            _ => string.Empty,
        };
    }
}
