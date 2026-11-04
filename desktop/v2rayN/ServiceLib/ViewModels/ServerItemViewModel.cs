namespace ServiceLib.ViewModels;

/// <summary>
/// One server row of the RockBlack UI 3.0 main screen (shared by the WPF and Avalonia views).
/// </summary>
public partial class ServerItemViewModel : ReactiveObject
{
    private readonly HomeViewModel _owner;

    public string IndexId { get; }
    public string SubId { get; }
    public EConfigType ConfigType { get; private set; }

    /// <summary>Order delivered by the subscription / manual order (ProfileEx sort).</summary>
    public int DefaultSort { get; set; }

    [Reactive] public partial string Name { get; set; }

    /// <summary>Flag emoji ("" when unknown). Windows cannot render flag emoji — prefer <see cref="CountryCode"/>.</summary>
    [Reactive] public partial string Flag { get; set; }

    /// <summary>ISO 3166-1 alpha-2, lowercase ("" when unknown → neutral globe icon).</summary>
    [Reactive] public partial string CountryCode { get; set; }

    [Reactive] public partial bool HasCountry { get; set; }
    [Reactive] public partial string Subtitle { get; set; }
    [Reactive] public partial string Address { get; set; }

    /// <summary>Last measured delay: 0 = never tested, -1 = timeout, &gt;0 = ms.</summary>
    [Reactive] public partial int Delay { get; set; }

    [Reactive] public partial string DelayText { get; set; }
    [Reactive] public partial EDelayState DelayState { get; set; }
    [Reactive] public partial bool IsSelected { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> SelectCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PingCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> EditCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ShareCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> CopyLinkCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> DeleteCmd { get; }

    public ServerItemViewModel(HomeViewModel owner, string indexId, string subId)
    {
        _owner = owner;
        IndexId = indexId;
        SubId = subId ?? string.Empty;
        Name = Flag = CountryCode = Subtitle = Address = DelayText = string.Empty;

        SelectCmd = ReactiveCommand.CreateFromTask(() => _owner.SelectServerAsync(this));
        PingCmd = ReactiveCommand.CreateFromTask(() => _owner.PingServersAsync([this]));
        EditCmd = ReactiveCommand.CreateFromTask(() => _owner.EditServerAsync(this));
        ShareCmd = ReactiveCommand.CreateFromTask(() => _owner.ShareServerAsync(this));
        CopyLinkCmd = ReactiveCommand.CreateFromTask(() => _owner.CopyServerLinkAsync(this));
        DeleteCmd = ReactiveCommand.CreateFromTask(() => _owner.DeleteServerAsync(this));
    }

    /// <summary>Updates the row from a database model (keeps the instance so the view does not flicker).</summary>
    public void Update(ProfileItemModel model)
    {
        ConfigType = model.ConfigType;
        var (flag, flagCode, name) = CountryExtension.SplitLeadingFlag(model.Remarks);
        var code = flagCode.IsNotEmpty() ? flagCode : CountryExtension.DetectCountryCode(model.Remarks);
        if (name.IsNullOrEmpty())
        {
            name = model.Address ?? string.Empty;
        }
        Name = name;
        CountryCode = code;
        HasCountry = code.IsNotEmpty();
        Flag = flag.IsNotEmpty() ? flag : CountryExtension.CodeToFlagEmoji(code);
        Subtitle = BuildSubtitle(model.ConfigType, model.StreamSecurity, model.Network);
        Address = model.ConfigType.IsComplexType() ? string.Empty : $"{model.Address}:{model.Port}";
    }

    /// <summary>Sets the latency result (0 = unknown, -1 = timeout).</summary>
    public void SetDelay(int delay)
    {
        Delay = delay;
        DelayState = GetDelayState(delay, false);
        DelayText = FormatDelay(delay, false);
    }

    public void SetTesting()
    {
        DelayState = EDelayState.Testing;
        DelayText = FormatDelay(Delay, true);
    }

    #region Formatting (static, unit-tested)

    public static EDelayState GetDelayState(int delay, bool testing)
    {
        if (testing)
        {
            return EDelayState.Testing;
        }
        return delay switch
        {
            0 => EDelayState.Unknown,
            < 0 => EDelayState.Timeout,
            < 300 => EDelayState.Good,
            <= 800 => EDelayState.Warning,
            _ => EDelayState.Bad,
        };
    }

    public static string FormatDelay(int delay, bool testing)
    {
        if (testing)
        {
            return "…";
        }
        return delay switch
        {
            0 => ResUI.RbNotAvailable,
            < 0 => ResUI.RbTimeout,
            _ => string.Format(ResUI.RbMs, delay),
        };
    }

    /// <summary>"VLESS · Reality", "VMess · TLS · ws", "Shadowsocks".</summary>
    public static string BuildSubtitle(EConfigType type, string? streamSecurity, string? network)
    {
        var parts = new List<string> { ProtocolName(type) };
        if (type.IsComplexType())
        {
            return parts[0];
        }
        switch (streamSecurity?.ToLowerInvariant())
        {
            case Global.StreamSecurityReality:
                parts.Add("Reality");
                break;

            case Global.StreamSecurity:
                parts.Add("TLS");
                break;
        }
        if (type is EConfigType.VMess or EConfigType.VLESS or EConfigType.Trojan
            && network.IsNotEmpty()
            && network is not Global.DefaultNetwork and not Global.RawNetworkAlias)
        {
            parts.Add(network);
        }
        return string.Join(" · ", parts);
    }

    public static string ProtocolName(EConfigType type)
    {
        return type switch
        {
            EConfigType.VMess => "VMess",
            EConfigType.VLESS => "VLESS",
            EConfigType.Shadowsocks => "Shadowsocks",
            EConfigType.SOCKS => "SOCKS",
            EConfigType.HTTP => "HTTP",
            EConfigType.Trojan => "Trojan",
            EConfigType.Hysteria2 => "Hysteria2",
            EConfigType.TUIC => "TUIC",
            EConfigType.WireGuard => "WireGuard",
            EConfigType.Anytls => "AnyTLS",
            EConfigType.Naive => "Naive",
            _ => type.ToString(),
        };
    }

    #endregion Formatting (static, unit-tested)
}
