using System.Globalization;

namespace ServiceLib.Handler;

/// <summary>
/// Parses provider metadata of a subscription (the de-facto standard used by Happ, Hiddify,
/// Clash and Marzban/Remnawave panels):
/// <list type="bullet">
/// <item><c>subscription-userinfo: upload=1; download=2; total=3; expire=1700000000</c></item>
/// <item><c>profile-title</c> (plain or <c>base64:</c> prefixed), <c>support-url</c>,
/// <c>profile-web-page-url</c>, <c>announce</c>, <c>profile-update-interval</c> (hours),
/// <c>routing</c></item>
/// </list>
/// Values are read from the HTTP response headers first; <c>#key: value</c> lines at the top of
/// the (optionally base64 encoded) body fill in anything the headers did not provide.
/// </summary>
public static class SubscriptionInfoParser
{
    public const string HeaderUserInfo = "subscription-userinfo";
    public const string HeaderTitle = "profile-title";
    public const string HeaderSupportUrl = "support-url";
    public const string HeaderWebPageUrl = "profile-web-page-url";
    public const string HeaderAnnounce = "announce";
    public const string HeaderUpdateInterval = "profile-update-interval";
    public const string HeaderRouting = "routing";

    private static readonly string[] _knownKeys =
    [
        HeaderUserInfo, HeaderTitle, HeaderSupportUrl, HeaderWebPageUrl, HeaderAnnounce, HeaderUpdateInterval, HeaderRouting
    ];

    /// <summary>
    /// Builds a <see cref="SubInfoItem"/> from response headers and body.
    /// </summary>
    /// <param name="subId">Subscription id stored in the result.</param>
    /// <param name="headers">Response headers (any casing); may be null.</param>
    /// <param name="body">Raw subscription body (plain or base64); may be null.</param>
    public static SubInfoItem Parse(string subId, IEnumerable<KeyValuePair<string, string>>? headers, string? body)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                if (key.IsNullOrEmpty() || value.IsNullOrEmpty() || !IsKnownKey(key.Trim()))
                {
                    continue;
                }
                values.TryAdd(key.Trim(), value.Trim());
            }
        }

        foreach (var (key, value) in ParseBodyLines(body))
        {
            values.TryAdd(key, value);
        }

        var item = new SubInfoItem { Id = subId ?? string.Empty };
        if (values.TryGetValue(HeaderUserInfo, out var userInfo))
        {
            ApplyUserInfo(item, userInfo);
        }
        if (values.TryGetValue(HeaderTitle, out var title))
        {
            item.Title = DecodeValue(title).NullIfEmpty();
        }
        if (values.TryGetValue(HeaderSupportUrl, out var support))
        {
            item.SupportUrl = DecodeValue(support).NullIfEmpty();
        }
        if (values.TryGetValue(HeaderWebPageUrl, out var web))
        {
            item.WebPageUrl = DecodeValue(web).NullIfEmpty();
        }
        if (values.TryGetValue(HeaderAnnounce, out var announce))
        {
            item.Announce = DecodeValue(announce).NullIfEmpty();
        }
        if (values.TryGetValue(HeaderUpdateInterval, out var interval)
            && int.TryParse(DecodeValue(interval).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)
            && hours > 0)
        {
            item.UpdateIntervalHours = hours;
        }
        if (values.TryGetValue(HeaderRouting, out var routing))
        {
            item.Routing = routing.Trim().NullIfEmpty();
        }

        return item;
    }

    /// <summary>
    /// Parses <c>upload=..; download=..; total=..; expire=..</c>. Unknown parts are ignored.
    /// </summary>
    public static void ApplyUserInfo(SubInfoItem item, string? userInfo)
    {
        if (userInfo.IsNullOrEmpty())
        {
            return;
        }

        var any = false;
        foreach (var part in userInfo.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }
            var key = part[..idx].Trim().ToLowerInvariant();
            var number = ParseNumber(part[(idx + 1)..]);
            if (number == null)
            {
                continue;
            }

            switch (key)
            {
                case "upload":
                    item.Upload = number.Value;
                    any = true;
                    break;

                case "download":
                    item.Download = number.Value;
                    any = true;
                    break;

                case "total":
                    item.Total = number.Value;
                    any = true;
                    break;

                case "expire":
                    item.Expire = number.Value;
                    any = true;
                    break;
            }
        }
        item.HasUserInfo = any;
    }

    /// <summary>
    /// Returns the <c>#key: value</c> (or <c>key: value</c> before the first link) metadata lines of a
    /// subscription body. The body may be base64 encoded.
    /// </summary>
    public static List<KeyValuePair<string, string>> ParseBodyLines(string? body)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (body.IsNullOrEmpty())
        {
            return result;
        }

        var text = body;
        if (Utils.IsBase64String(body.Trim()))
        {
            var decoded = Utils.Base64Decode(body.Trim());
            if (decoded.IsNotEmpty())
            {
                text = decoded;
            }
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.IsNullOrEmpty())
            {
                continue;
            }

            var commented = line.StartsWith('#');
            if (commented)
            {
                line = line.TrimStart('#').Trim();
            }
            else if (line.Contains("://") && !line.StartsWith(HeaderRouting, StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith(HeaderSupportUrl, StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith(HeaderWebPageUrl, StringComparison.OrdinalIgnoreCase))
            {
                // A share link: metadata is only expected before / between links as comments.
                continue;
            }

            var idx = line.IndexOf(':');
            if (idx <= 0)
            {
                continue;
            }
            var key = line[..idx].Trim();
            if (!IsKnownKey(key))
            {
                continue;
            }
            var value = line[(idx + 1)..].Trim();
            if (value.IsNotEmpty())
            {
                result.Add(new(key.ToLowerInvariant(), value));
            }
        }

        return result;
    }

    /// <summary>Decodes <c>base64:...</c> values; returns other values unchanged.</summary>
    public static string DecodeValue(string? value)
    {
        if (value.IsNullOrEmpty())
        {
            return string.Empty;
        }
        var v = value.Trim();
        if (v.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
        {
            var decoded = Utils.Base64Decode(v[7..].Trim());
            return decoded.IsNullOrEmpty() ? v : decoded.Trim();
        }
        return v;
    }

    /// <summary>
    /// Copies metadata from a fresh parse onto the stored info, keeping previously known values
    /// that the provider did not send again (e.g. a provider sends <c>profile-title</c> only in headers).
    /// </summary>
    public static SubInfoItem Merge(SubInfoItem? stored, SubInfoItem fresh)
    {
        if (stored == null)
        {
            return fresh;
        }
        stored.Id = fresh.Id;
        if (fresh.HasUserInfo)
        {
            stored.Upload = fresh.Upload;
            stored.Download = fresh.Download;
            stored.Total = fresh.Total;
            stored.Expire = fresh.Expire;
            stored.HasUserInfo = true;
        }
        stored.Title = fresh.Title ?? stored.Title;
        stored.SupportUrl = fresh.SupportUrl ?? stored.SupportUrl;
        stored.WebPageUrl = fresh.WebPageUrl ?? stored.WebPageUrl;
        // An announcement disappears when the provider stops sending it.
        stored.Announce = fresh.Announce;
        stored.UpdateIntervalHours = fresh.UpdateIntervalHours > 0 ? fresh.UpdateIntervalHours : stored.UpdateIntervalHours;
        stored.Routing = fresh.Routing ?? stored.Routing;
        return stored;
    }

    private static bool IsKnownKey(string key)
    {
        return _knownKeys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static long? ParseNumber(string text)
    {
        var t = text.Trim();
        if (long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
        {
            return l;
        }
        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && !double.IsNaN(d))
        {
            return (long)Math.Round(d);
        }
        return null;
    }
}
