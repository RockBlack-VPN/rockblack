using System.Globalization;

namespace ServiceLib.ViewModels;

/// <summary>
/// A subscription card of the RockBlack UI 3.0 main screen (or the manual "Server list" card,
/// <see cref="IsManual"/>), shared by the WPF and Avalonia views.
/// </summary>
public partial class ServerGroupViewModel : ReactiveObject
{
    private readonly HomeViewModel _owner;

    /// <summary>Subscription id; "" for the manual servers group.</summary>
    public string SubId { get; }

    public bool IsManual => SubId.IsNullOrEmpty();

    public ObservableCollection<ServerItemViewModel> Servers { get; } = [];

    [Reactive] public partial string Name { get; set; }
    [Reactive] public partial string UpdatedText { get; set; }
    [Reactive] public partial string AutoUpdateText { get; set; }
    [Reactive] public partial string SubtitleText { get; set; }
    [Reactive] public partial bool HasSubtitle { get; set; }
    [Reactive] public partial bool IsExpanded { get; set; }
    [Reactive] public partial bool IsPinging { get; set; }
    [Reactive] public partial bool IsRefreshing { get; set; }
    [Reactive] public partial bool CanRefresh { get; set; }
    [Reactive] public partial int ServerCount { get; set; }
    [Reactive] public partial EServerSortMode SortMode { get; set; }

    // Info bar
    [Reactive] public partial bool HasInfo { get; set; }
    [Reactive] public partial string TrafficText { get; set; }
    [Reactive] public partial double TrafficPercent { get; set; }
    [Reactive] public partial bool HasTraffic { get; set; }
    [Reactive] public partial bool HasTrafficLimit { get; set; }
    [Reactive] public partial string ExpiryText { get; set; }
    [Reactive] public partial bool HasExpiry { get; set; }

    // Expiry banner
    [Reactive] public partial bool ShowExpiryBanner { get; set; }
    [Reactive] public partial string ExpiryBannerText { get; set; }
    [Reactive] public partial int? DaysLeft { get; set; }

    // Provider links
    [Reactive] public partial string? SupportUrl { get; set; }
    [Reactive] public partial bool HasSupportUrl { get; set; }
    [Reactive] public partial bool IsTelegramSupport { get; set; }
    [Reactive] public partial string? WebPageUrl { get; set; }
    [Reactive] public partial bool HasWebPageUrl { get; set; }
    [Reactive] public partial string? Announce { get; set; }
    [Reactive] public partial bool HasAnnounce { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> ToggleExpandCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RefreshCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PingCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SortByPingCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SortByNameCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SortDefaultCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SelectFastestCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> OpenSupportCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> OpenWebPageCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> EditCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> DeleteCmd { get; }

    public ServerGroupViewModel(HomeViewModel owner, string subId, bool isExpanded = true)
    {
        _owner = owner;
        SubId = subId ?? string.Empty;
        Name = UpdatedText = AutoUpdateText = SubtitleText = TrafficText = ExpiryText = ExpiryBannerText = string.Empty;
        IsExpanded = isExpanded;

        ToggleExpandCmd = ReactiveCommand.Create(() => { IsExpanded = !IsExpanded; });
        RefreshCmd = ReactiveCommand.CreateFromTask(() => _owner.RefreshSubscriptionAsync(this));
        PingCmd = ReactiveCommand.CreateFromTask(() => _owner.PingServersAsync(Servers.ToList(), this));
        SortByPingCmd = ReactiveCommand.Create(() => _owner.SetSortMode(this, EServerSortMode.Ping));
        SortByNameCmd = ReactiveCommand.Create(() => _owner.SetSortMode(this, EServerSortMode.Name));
        SortDefaultCmd = ReactiveCommand.Create(() => _owner.SetSortMode(this, EServerSortMode.Default));
        SelectFastestCmd = ReactiveCommand.CreateFromTask(() => _owner.SelectFastestAsync(this));
        OpenSupportCmd = ReactiveCommand.Create(() => HomeViewModel.OpenUrl(SupportUrl));
        OpenWebPageCmd = ReactiveCommand.Create(() => HomeViewModel.OpenUrl(WebPageUrl));
        EditCmd = ReactiveCommand.CreateFromTask(() => _owner.EditSubscriptionAsync(this));
        DeleteCmd = ReactiveCommand.CreateFromTask(() => _owner.DeleteSubscriptionAsync(this));

        // Persist the collapsed state (the owner ignores unchanged values).
        this.WhenAnyValue(x => x.IsExpanded)
            .Subscribe(expanded => _owner.OnGroupExpandedChanged(this, expanded));
    }

    /// <summary>Applies subscription settings and provider metadata.</summary>
    public void ApplyInfo(SubItem? sub, SubInfoItem? info, DateTime now)
    {
        Name = GetGroupName(sub, info);
        CanRefresh = sub?.Url.IsNotEmpty() == true;

        var updated = info?.UpdateTime > 0 ? info.UpdateTime : sub?.UpdateTime ?? 0;
        UpdatedText = updated > 0 ? FormatDateTime(DateTimeOffset.FromUnixTimeSeconds(updated).LocalDateTime) : string.Empty;
        AutoUpdateText = FormatAutoUpdate(sub?.AutoUpdateInterval ?? 0);
        SubtitleText = string.Join(" · ", new[] { UpdatedText, AutoUpdateText }.Where(s => s.IsNotEmpty()));
        HasSubtitle = SubtitleText.IsNotEmpty();

        var hasUserInfo = info?.HasUserInfo == true;
        HasTraffic = hasUserInfo;
        HasTrafficLimit = hasUserInfo && info!.Total > 0;
        TrafficText = hasUserInfo ? FormatTraffic(info!.Used, info.Total) : string.Empty;
        TrafficPercent = HasTrafficLimit ? Math.Clamp(info!.Used * 100.0 / info.Total, 0, 100) : 0;

        var expire = hasUserInfo && info!.Expire > 0 ? DateTimeOffset.FromUnixTimeSeconds(info.Expire).LocalDateTime : (DateTime?)null;
        HasExpiry = expire != null;
        ExpiryText = expire != null ? string.Format(ResUI.RbExpires, FormatDate(expire.Value)) : string.Empty;
        (ShowExpiryBanner, ExpiryBannerText, DaysLeft) = GetExpiryBanner(expire, now);

        SupportUrl = info?.SupportUrl;
        HasSupportUrl = SupportUrl.IsNotEmpty();
        IsTelegramSupport = IsTelegramUrl(SupportUrl);
        WebPageUrl = info?.WebPageUrl;
        HasWebPageUrl = WebPageUrl.IsNotEmpty();
        Announce = info?.Announce;
        HasAnnounce = Announce.IsNotEmpty();

        HasInfo = HasTraffic || HasExpiry || HasSupportUrl || HasWebPageUrl;
    }

    #region Formatting (static, unit-tested)

    public static string GetGroupName(SubItem? sub, SubInfoItem? info)
    {
        if (sub == null)
        {
            return ResUI.RbManualServers;
        }
        if (sub.Remarks.IsNotEmpty() && sub.Remarks != "import_sub")
        {
            return sub.Remarks;
        }
        if (info?.Title.IsNotEmpty() == true)
        {
            return info.Title;
        }
        return Utils.TryUri(sub.Url ?? string.Empty)?.Host ?? sub.Remarks ?? string.Empty;
    }

    public static string FormatDate(DateTime value)
    {
        return value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    }

    public static string FormatDateTime(DateTime value)
    {
        return value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Auto-update interval in minutes → "auto 12 h" / "auto 30 min" ("" when off).</summary>
    public static string FormatAutoUpdate(int minutes)
    {
        if (minutes <= 0)
        {
            return string.Empty;
        }
        return minutes % 60 == 0
            ? string.Format(ResUI.RbAutoUpdateHours, minutes / 60)
            : string.Format(ResUI.RbAutoUpdateMinutes, minutes);
    }

    /// <summary>"3.1 GB / 50 GB", "3 GB / ∞" (total = 0 means unlimited).</summary>
    public static string FormatTraffic(long used, long total)
    {
        return $"{FormatBytes(used)} / {(total > 0 ? FormatBytes(total) : "∞")}";
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            bytes = 0;
        }
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        var number = size < 10 && unit > 0
            ? Math.Round(size, 1).ToString("0.#", CultureInfo.InvariantCulture)
            : Math.Round(size).ToString("0", CultureInfo.InvariantCulture);
        return $"{number} {units[unit]}";
    }

    /// <summary>Banner shown when the subscription expires within 3 days or has expired.</summary>
    public static (bool Show, string Text, int? DaysLeft) GetExpiryBanner(DateTime? expire, DateTime now)
    {
        if (expire == null)
        {
            return (false, string.Empty, null);
        }
        var remaining = expire.Value - now;
        if (remaining <= TimeSpan.Zero)
        {
            return (true, ResUI.RbExpired, 0);
        }
        // Whole days left ("in 2 days" for 2 d 10 min), like Happ.
        var days = (int)Math.Floor(remaining.TotalDays);
        if (days > 3)
        {
            return (false, string.Empty, days);
        }
        if (expire.Value.Date == now.Date)
        {
            return (true, ResUI.RbExpiresToday, 0);
        }
        days = Math.Max(1, days);
        return (true, string.Format(ResUI.RbExpiresIn, FormatDays(days)), days);
    }

    /// <summary>"1 day", "3 days" (Russian: день / дня / дней).</summary>
    public static string FormatDays(int days)
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string format;
        if (lang == "ru")
        {
            var mod10 = days % 10;
            var mod100 = days % 100;
            format = mod10 == 1 && mod100 != 11
                ? ResUI.RbDaysOne
                : mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14 ? ResUI.RbDaysFew : ResUI.RbDaysMany;
        }
        else
        {
            format = days == 1 ? ResUI.RbDaysOne : ResUI.RbDaysMany;
        }
        return string.Format(format, days);
    }

    public static bool IsTelegramUrl(string? url)
    {
        if (url.IsNullOrEmpty())
        {
            return false;
        }
        if (url.StartsWith("tg://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        var host = Utils.TryUri(url)?.Host?.ToLowerInvariant() ?? string.Empty;
        return host is "t.me" or "telegram.me" or "telegram.dog" or "www.t.me";
    }

    #endregion Formatting (static, unit-tested)
}
