namespace ServiceLib.Handler;

/// <summary>
/// Applies a Happ routing profile delivered by a subscription (<c>routing</c> header or
/// <c>#routing:</c> body line, see <see cref="SubscriptionInfoParser"/>), like the RockBlack
/// Android / iOS clients:
/// <list type="bullet">
/// <item><c>happ://routing/add/{base64 json}</c> — store / overwrite the profile;</item>
/// <item><c>happ://routing/onadd/{base64 json}</c> — store and activate it;</item>
/// <item><c>happ://routing/off</c> — remove the subscription's profile (Russia becomes active if it was active).</item>
/// </list>
/// Rule mapping: BlockSites/BlockIp → block, private ranges → direct, ProxySites/ProxyIp → proxy,
/// DirectSites/DirectIp → direct, final rule from GlobalProxy (true → proxy, false → direct);
/// DomainStrategy is applied to the routing item. The profile is stored as routing item
/// <c>rb-happ-{subscription id}</c> with the profile name as remarks.
/// </summary>
public static class HappRoutingImporter
{
    public const string IdPrefix = "rb-happ-";
    private const string Scheme = "happ://routing/";

    public enum EAction
    {
        None,
        Add,
        OnAdd,
        Off,
    }

    /// <summary>Result of <see cref="ApplyAsync"/>.</summary>
    public sealed record Result(bool Changed, bool ActiveChanged, string? Error = null);

    public static string GetId(string subId) => IdPrefix + subId;

    public static bool IsHappRouting(string? routingId) => routingId?.StartsWith(IdPrefix, StringComparison.Ordinal) == true;

    /// <summary>Splits a <c>happ://routing/…</c> link into action and decoded JSON payload.</summary>
    public static (EAction Action, string? Json) ParseLink(string? value)
    {
        var v = value?.Trim() ?? string.Empty;
        if (!v.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return (EAction.None, null);
        }
        var rest = v[Scheme.Length..];
        var slash = rest.IndexOf('/');
        var verb = (slash < 0 ? rest : rest[..slash]).Trim().ToLowerInvariant();
        var payload = slash < 0 ? string.Empty : Uri.UnescapeDataString(rest[(slash + 1)..].Trim());

        var action = verb switch
        {
            "add" => EAction.Add,
            "onadd" => EAction.OnAdd,
            "off" => EAction.Off,
            _ => EAction.None,
        };
        if (action is EAction.None or EAction.Off)
        {
            return (action, null);
        }

        var json = payload.TrimStart().StartsWith('{') ? payload : Utils.Base64Decode(payload);
        return json.IsNullOrEmpty() ? (EAction.None, null) : (action, json);
    }

    /// <summary>Builds the routing item from a Happ routing JSON profile (not saved).</summary>
    public static RoutingItem? BuildRoutingItem(string subId, string json, out string? error)
    {
        error = null;
        JsonObject? obj;
        try
        {
            obj = JsonNode.Parse(json) as JsonObject;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
        if (obj == null)
        {
            error = "Invalid Happ routing profile";
            return null;
        }

        var name = GetString(obj, "Name").NullIfEmpty() ?? "Happ routing";
        var globalProxy = GetBool(obj, "GlobalProxy") ?? true;

        var rules = new List<RulesItem>();
        AddRule(rules, "Block sites", Global.BlockTag, domains: GetList(obj, "BlockSites"));
        AddRule(rules, "Block IP", Global.BlockTag, ips: GetList(obj, "BlockIp"));
        AddRule(rules, "Private IP direct", Global.DirectTag, ips: ["geoip:private"]);
        AddRule(rules, "Private domains direct", Global.DirectTag, domains: ["geosite:private"]);
        AddRule(rules, "Proxy sites", Global.ProxyTag, domains: GetList(obj, "ProxySites"));
        AddRule(rules, "Proxy IP", Global.ProxyTag, ips: GetList(obj, "ProxyIp"));
        AddRule(rules, "Direct sites", Global.DirectTag, domains: GetList(obj, "DirectSites"));
        AddRule(rules, "Direct IP", Global.DirectTag, ips: GetList(obj, "DirectIp"));
        rules.Add(new RulesItem
        {
            Id = Utils.GetGuid(false),
            Remarks = globalProxy ? "Everything else via proxy" : "Everything else direct",
            Port = "0-65535",
            OutboundTag = globalProxy ? Global.ProxyTag : Global.DirectTag,
            Enabled = true,
        });

        var strategy = GetString(obj, "DomainStrategy");
        strategy = Global.DomainStrategies.FirstOrDefault(t => t.Equals(strategy, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        return new RoutingItem
        {
            Id = GetId(subId),
            Remarks = name,
            Url = string.Empty,
            RuleSet = JsonUtils.Serialize(rules, false),
            RuleNum = rules.Count,
            Enabled = true,
            Locked = false,
            DomainStrategy = strategy,
            Sort = 0,
        };
    }

    /// <summary>
    /// Applies the subscription's <c>routing</c> value. Returns whether the routing list changed and
    /// whether the active profile changed (the caller reloads the core when connected).
    /// </summary>
    public static async Task<Result> ApplyAsync(Config config, string subId, string? routingValue)
    {
        var (action, json) = ParseLink(routingValue);
        switch (action)
        {
            case EAction.None:
                return new(false, false);

            case EAction.Off:
            {
                var wasActive = await RemoveForSubscriptionAsync(config, subId);
                return new(wasActive != null, wasActive == true);
            }
        }

        var item = BuildRoutingItem(subId, json!, out var error);
        if (item == null)
        {
            Logging.SaveLog($"HappRoutingImporter: {error}");
            return new(false, false, error);
        }

        var items = await AppManager.Instance.RoutingItems() ?? [];
        // Same profile name from a subscription → overwrite; our own id → replace in place.
        var previous = items.Where(t => t.Id == item.Id || (IsHappRouting(t.Id) && t.Remarks == item.Remarks)).ToList();
        var wasActiveBefore = previous.Any(t => t.IsActive);
        foreach (var old in previous.Where(t => t.Id != item.Id))
        {
            await SQLiteHelper.Instance.DeleteAsync(old);
        }
        item.IsActive = wasActiveBefore;
        var own = previous.FirstOrDefault(t => t.Id == item.Id);
        var unchanged = own != null && previous.Count == 1
                        && own.Remarks == item.Remarks && own.DomainStrategy == item.DomainStrategy
                        && SameRules(own.RuleSet, item.RuleSet);
        if (own != null)
        {
            item.CustomIcon = own.CustomIcon;
            item.CustomRulesetPath4Singbox = own.CustomRulesetPath4Singbox;
            item.DomainStrategy4Singbox = own.DomainStrategy4Singbox;
        }
        await SQLiteHelper.Instance.ReplaceAsync(item);

        var activeChanged = false;
        if (action == EAction.OnAdd && !item.IsActive)
        {
            activeChanged = await ConfigHandler.SetDefaultRouting(config, item) == 0;
        }
        else if (wasActiveBefore && !unchanged)
        {
            // The active profile's rules changed: the running core must pick them up.
            activeChanged = true;
        }
        return new(!unchanged || activeChanged, activeChanged);
    }

    /// <summary>
    /// Deletes the routing profile imported for a subscription. Returns null if there was none,
    /// otherwise whether it was the active one (then the default preset is activated).
    /// </summary>
    public static async Task<bool?> RemoveForSubscriptionAsync(Config config, string subId)
    {
        var item = await AppManager.Instance.GetRoutingItem(GetId(subId));
        if (item == null)
        {
            return null;
        }
        await SQLiteHelper.Instance.DeleteAsync(item);
        if (!item.IsActive)
        {
            return false;
        }
        var fallback = await AppManager.Instance.GetRoutingItem(RoutingPresets.DefaultId)
                       ?? (await AppManager.Instance.RoutingItems())?.FirstOrDefault();
        if (fallback != null)
        {
            await ConfigHandler.SetDefaultRouting(config, fallback);
        }
        return true;
    }

    private static bool SameRules(string? a, string? b)
    {
        static string Normalize(string? json) =>
            string.Join("|", (JsonUtils.Deserialize<List<RulesItem>>(json) ?? [])
                .Select(r => $"{r.OutboundTag};{r.Port};{string.Join(",", r.Domain ?? [])};{string.Join(",", r.Ip ?? [])}"));
        return Normalize(a) == Normalize(b);
    }

    private static void AddRule(List<RulesItem> rules, string remarks, string outbound, List<string>? domains = null, List<string>? ips = null)
    {
        if ((domains?.Count ?? 0) == 0 && (ips?.Count ?? 0) == 0)
        {
            return;
        }
        rules.Add(new RulesItem
        {
            Id = Utils.GetGuid(false),
            Remarks = remarks,
            OutboundTag = outbound,
            Domain = domains is { Count: > 0 } ? domains : null,
            Ip = ips is { Count: > 0 } ? ips : null,
            Enabled = true,
        });
    }

    private static JsonNode? Get(JsonObject obj, string key)
    {
        foreach (var (k, v) in obj)
        {
            if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return v;
            }
        }
        return null;
    }

    private static string GetString(JsonObject obj, string key)
    {
        var node = Get(obj, key);
        return node is JsonValue value ? value.ToString().Trim() : string.Empty;
    }

    private static bool? GetBool(JsonObject obj, string key)
    {
        var node = Get(obj, key);
        if (node is not JsonValue value)
        {
            return null;
        }
        if (value.TryGetValue<bool>(out var b))
        {
            return b;
        }
        var s = value.ToString().Trim();
        return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1" ? true
            : s.Equals("false", StringComparison.OrdinalIgnoreCase) || s == "0" ? false
            : null;
    }

    private static List<string> GetList(JsonObject obj, string key)
    {
        var node = Get(obj, key);
        var list = new List<string>();
        switch (node)
        {
            case JsonArray array:
                list.AddRange(array.Select(t => t?.ToString().Trim() ?? string.Empty));
                break;

            case JsonValue value:
                list.AddRange(value.ToString().Split([',', '\n'], StringSplitOptions.TrimEntries));
                break;
        }
        return list.Where(t => t.IsNotEmpty()).Distinct().ToList();
    }
}
