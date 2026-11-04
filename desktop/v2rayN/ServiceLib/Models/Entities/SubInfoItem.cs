namespace ServiceLib.Models.Entities;

/// <summary>
/// Provider metadata of a subscription (RockBlack UI 3.0 info bar), parsed from the
/// subscription response headers (<c>subscription-userinfo</c>, <c>profile-title</c>,
/// <c>support-url</c>, <c>profile-web-page-url</c>, <c>announce</c>,
/// <c>profile-update-interval</c>) or the equivalent <c>#key: value</c> body lines.
/// </summary>
public class SubInfoItem
{
    /// <summary>Subscription id (<see cref="SubItem.Id"/>).</summary>
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public long Upload { get; set; }
    public long Download { get; set; }

    /// <summary>Traffic limit in bytes; 0 = unlimited.</summary>
    public long Total { get; set; }

    /// <summary>Expiry as unix time (seconds); 0 = never.</summary>
    public long Expire { get; set; }

    public string? Title { get; set; }
    public string? SupportUrl { get; set; }
    public string? WebPageUrl { get; set; }
    public string? Announce { get; set; }

    /// <summary>Raw <c>routing</c> value (e.g. <c>happ://routing/add/{base64}</c>).</summary>
    public string? Routing { get; set; }

    /// <summary>Update interval suggested by the provider, in hours (0 = not provided).</summary>
    public int UpdateIntervalHours { get; set; }

    /// <summary>True when the provider sent a <c>subscription-userinfo</c> value.</summary>
    public bool HasUserInfo { get; set; }

    /// <summary>Last time the info was refreshed (unix seconds).</summary>
    public long UpdateTime { get; set; }

    [Ignore]
    public long Used => Upload + Download;

    [Ignore]
    public bool IsEmpty =>
        !HasUserInfo
        && Title.IsNullOrEmpty()
        && SupportUrl.IsNullOrEmpty()
        && WebPageUrl.IsNullOrEmpty()
        && Announce.IsNullOrEmpty()
        && UpdateIntervalHours <= 0;
}
