namespace ServiceLib.ViewModels;

/// <summary>
/// RockBlack UI 3.0 main screen (branding/DESIGN.md): power button with connection state,
/// session timer and live speed, routing / mode chips and subscription cards with servers.
/// Shared by the WPF (Windows) and Avalonia (macOS / Linux) views; created and wired by
/// <see cref="MainWindowViewModel"/> (property <c>HomeViewModel</c>).
/// </summary>
public partial class HomeViewModel : MyReactiveObject
{
    #region Interactions (registered by the views)

    /// <summary>Copies text (share link) to the clipboard.</summary>
    public Interaction<string, RxVoid> CopyToClipboardInteraction { get; } = new();

    /// <summary>Shows a QR code for the share URL.</summary>
    public Interaction<string, RxVoid> ShowQrCodeInteraction { get; } = new();

    /// <summary>Asks a yes/no question; returns true for yes.</summary>
    public Interaction<string, bool> ConfirmInteraction { get; } = new();

    #endregion Interactions (registered by the views)

    #region Hooks (set by MainWindowViewModel)

    /// <summary>Reloads the core (restarts it when connected, stops it when disconnected).</summary>
    public Func<Task>? ReloadFunc { get; set; }

    /// <summary>Refreshes every server list (tray menu, advanced list, this screen).</summary>
    public Func<Task>? RefreshAllFunc { get; set; }

    /// <summary>Updates the subscription with the id ("" = all); bool = via proxy.</summary>
    public Func<string, bool, Task>? UpdateSubscriptionFunc { get; set; }

    #endregion Hooks (set by MainWindowViewModel)

    private readonly Dictionary<string, ServerItemViewModel> _rows = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _reloading;
    private DateTime? _connectedSince;
    private bool _disposed;

    public ObservableCollection<ServerGroupViewModel> Groups { get; } = [];
    public ObservableCollection<RoutingProfileViewModel> RoutingProfiles { get; } = [];

    [Reactive] public partial bool HasGroups { get; set; }
    [Reactive] public partial bool IsEmpty { get; set; }

    #region Connection

    [Reactive] public partial EConnectionState ConnectionState { get; set; }
    [Reactive] public partial bool IsConnected { get; set; }
    [Reactive] public partial bool IsConnecting { get; set; }
    [Reactive] public partial string ConnectionStateText { get; set; }
    [Reactive] public partial string SessionTimeText { get; set; }
    [Reactive] public partial bool ShowSpeed { get; set; }
    [Reactive] public partial string SpeedDownText { get; set; }
    [Reactive] public partial string SpeedUpText { get; set; }

    /// <summary>Name of the active server (shown under the power button on wide layouts).</summary>
    [Reactive] public partial string ActiveServerName { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> ToggleConnectCmd { get; }

    #endregion Connection

    #region Mode and routing

    [Reactive] public partial bool IsTunMode { get; set; }
    [Reactive] public partial bool IsProxyMode { get; set; }
    [Reactive] public partial string RoutingName { get; set; }
    [Reactive] public partial string RoutingChipText { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> SetProxyModeCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SetTunModeCmd { get; }
    public ReactiveCommand<RoutingProfileViewModel, RxVoid> SelectRoutingCmd { get; }

    #endregion Mode and routing

    #region Ping and subscriptions

    /// <summary>Show a status dot instead of the latency text (Settings → Ping → display).</summary>
    [Reactive] public partial bool PingShowIcon { get; set; }

    [Reactive] public partial bool IsPingingAll { get; set; }
    [Reactive] public partial bool IsRefreshingAll { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> PingAllCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RefreshAllCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> AddSubscriptionCmd { get; }

    #endregion Ping and subscriptions

    public HomeViewModel()
    {
        _config = AppManager.Instance.Config;
        ConnectionStateText = ResUI.RbNotConnected;
        SessionTimeText = SpeedDownText = SpeedUpText = RoutingName = RoutingChipText = ActiveServerName = string.Empty;
        PingShowIcon = _config.SpeedTestItem.PingShowIcon;

        // Upgrades from UI 2.0 (connect = system proxy on) keep their state.
        _config.UiItem.RbConnected ??= _config.SystemProxyItem.SysProxyType is ESysProxyType.ForcedChange or ESysProxyType.Pac
                                        || _config.TunModeItem.EnableTun;

        ToggleConnectCmd = ReactiveCommand.CreateFromTask(ToggleConnectAsync);
        SetProxyModeCmd = ReactiveCommand.CreateFromTask(() => SetModeAsync(false));
        SetTunModeCmd = ReactiveCommand.CreateFromTask(() => SetModeAsync(true));
        SelectRoutingCmd = ReactiveCommand.Create<RoutingProfileViewModel>(SelectRouting);
        PingAllCmd = ReactiveCommand.CreateFromTask(PingAllAsync);
        RefreshAllCmd = ReactiveCommand.CreateFromTask(RefreshAllSubscriptionsAsync);
        AddSubscriptionCmd = ReactiveCommand.CreateFromTask(AddSubscriptionAsync);

        var statusBar = StatusBarViewModel.Instance;
        statusBar.WhenAnyValue(x => x.EnableTun)
            .Subscribe(tun =>
            {
                IsTunMode = tun;
                IsProxyMode = !tun;
            });
        statusBar.WhenAnyValue(x => x.SelectedRouting)
            .Subscribe(_ => RebuildRoutingProfiles());
        statusBar.RoutingItems.CollectionChanged += (_, _) => RebuildRoutingProfiles();

        AppEvents.DispatcherStatisticsRequested
            .AsObservable()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(UpdateSpeed);

        UpdateConnectionState();
        _ = RunClockAsync();
    }

    #region Connection

    /// <summary>Whether the user switched the tunnel on (persisted).</summary>
    public bool WantConnected => _config.UiItem.RbConnected == true;

    /// <summary>Called by MainWindowViewModel when a core reload starts / ends (any thread).</summary>
    public void SetReloading(bool reloading)
    {
        RxSchedulers.MainThreadScheduler.Schedule(() =>
        {
            _reloading = reloading;
            UpdateConnectionState();
        });
    }

    private async Task ToggleConnectAsync()
    {
        if (IsConnecting)
        {
            return;
        }
        var connect = ConnectionState == EConnectionState.Disconnected;
        _config.UiItem.RbConnected = connect;

        var proxyType = _config.SystemProxyItem.SysProxyType;
        if (connect)
        {
            if (!_config.TunModeItem.EnableTun && proxyType is not (ESysProxyType.ForcedChange or ESysProxyType.Pac))
            {
                AppEvents.SysProxyChangeRequested.Publish(ESysProxyType.ForcedChange);
            }
        }
        else if (proxyType is ESysProxyType.ForcedChange or ESysProxyType.Pac)
        {
            AppEvents.SysProxyChangeRequested.Publish(ESysProxyType.ForcedClear);
        }
        await ConfigHandler.SaveConfig(_config);

        _reloading = connect;
        UpdateConnectionState();
        if (ReloadFunc != null)
        {
            await ReloadFunc();
        }
    }

    private async Task SetModeAsync(bool tun)
    {
        var statusBar = StatusBarViewModel.Instance;
        if (statusBar.EnableTun == tun)
        {
            return;
        }
        if (WantConnected)
        {
            // Proxy and TUN are exclusive on the main screen.
            AppEvents.SysProxyChangeRequested.Publish(tun ? ESysProxyType.ForcedClear : ESysProxyType.ForcedChange);
        }
        // StatusBarViewModel handles elevation (restart as admin / sudo password) and reloads the core.
        statusBar.EnableTun = tun;
        await Task.CompletedTask;
    }

    private void UpdateConnectionState()
    {
        var running = CoreManager.Instance.IsCoreRunning;
        var state = !WantConnected
            ? EConnectionState.Disconnected
            : _reloading ? EConnectionState.Connecting
            : running ? EConnectionState.Connected
            : EConnectionState.Disconnected;

        if (state == EConnectionState.Connected)
        {
            _connectedSince ??= DateTime.Now;
        }
        else if (state == EConnectionState.Disconnected)
        {
            _connectedSince = null;
        }

        ConnectionState = state;
        IsConnected = state == EConnectionState.Connected;
        IsConnecting = state == EConnectionState.Connecting;
        ConnectionStateText = state switch
        {
            EConnectionState.Connected => ResUI.RbConnected,
            EConnectionState.Connecting => ResUI.RbConnecting,
            _ => ResUI.RbNotConnected,
        };
        SessionTimeText = _connectedSince is { } since && IsConnected ? FormatSessionTime(DateTime.Now - since) : string.Empty;
        ShowSpeed = IsConnected && _config.GuiItem.DisplayRealTimeSpeed;
        if (!IsConnected)
        {
            SpeedDownText = SpeedUpText = string.Empty;
        }
    }

    private async Task RunClockAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!_disposed && await timer.WaitForNextTickAsync())
        {
            RxSchedulers.MainThreadScheduler.Schedule(UpdateConnectionState);
        }
    }

    private void UpdateSpeed(ServerSpeedItem update)
    {
        if (!IsConnected)
        {
            return;
        }
        // Statistics arrive in KB/s (proxy + direct = everything that went through the core).
        SpeedDownText = FormatSpeed(update.ProxyDown + update.DirectDown);
        SpeedUpText = FormatSpeed(update.ProxyUp + update.DirectUp);
        ShowSpeed = _config.GuiItem.DisplayRealTimeSpeed;
    }

    public static string FormatSessionTime(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }
        return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    /// <summary>Formats a speed given in KB/s: "1.2 MB/s", "140 KB/s".</summary>
    public static string FormatSpeed(long kbPerSecond)
    {
        return $"{ServerGroupViewModel.FormatBytes(Math.Max(0, kbPerSecond) * 1024)}/s";
    }

    #endregion Connection

    #region Routing

    private void RebuildRoutingProfiles()
    {
        var statusBar = StatusBarViewModel.Instance;
        var activeId = statusBar.SelectedRouting?.Id;
        var items = statusBar.RoutingItems.Where(t => t.Id.IsNotEmpty()).ToList();
        if (activeId.IsNullOrEmpty())
        {
            activeId = items.FirstOrDefault(t => t.IsActive)?.Id;
        }

        RoutingProfiles.Clear();
        foreach (var item in items)
        {
            RoutingProfiles.Add(new RoutingProfileViewModel(item, item.Id == activeId));
        }
        var active = items.FirstOrDefault(t => t.Id == activeId);
        RoutingName = RoutingPresets.GetDisplayName(active);
        RoutingChipText = active == null ? ResUI.RbRoutingProfiles : string.Format(ResUI.RbRoutingChip, RoutingName);
    }

    private void SelectRouting(RoutingProfileViewModel? profile)
    {
        if (profile == null)
        {
            return;
        }
        var statusBar = StatusBarViewModel.Instance;
        var item = statusBar.RoutingItems.FirstOrDefault(t => t.Id == profile.Id);
        if (item == null || item.Id == statusBar.SelectedRouting?.Id)
        {
            return;
        }
        // StatusBarViewModel activates the profile and requests a core reload (reconnects when connected).
        statusBar.SelectedRouting = item;
    }

    #endregion Routing

    #region Groups and servers

    /// <summary>Rebuilds the subscription cards from the database (keeps existing view models).</summary>
    public async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var subs = await AppManager.Instance.SubItems() ?? [];
            var infos = (await AppManager.Instance.SubInfoItems()).GroupBy(t => t.Id).ToDictionary(t => t.Key, t => t.First());
            var models = await AppManager.Instance.ProfileModels("", "") ?? [];
            var exs = (await ProfileExManager.Instance.GetProfileExs())
                .GroupBy(t => t.IndexId).ToDictionary(t => t.Key, t => t.First());
            await ConfigHandler.SetDefaultServer(_config, models);

            var now = DateTime.Now;
            var subIds = subs.Select(t => t.Id).ToHashSet();
            var existing = Groups.ToDictionary(t => t.SubId);
            var groups = new List<ServerGroupViewModel>();
            var seenRows = new HashSet<string>();

            foreach (var sub in subs)
            {
                var group = existing.GetValueOrDefault(sub.Id) ?? CreateGroup(sub.Id);
                group.ApplyInfo(sub, infos.GetValueOrDefault(sub.Id), now);
                SyncServers(group, models.Where(t => t.Subid == sub.Id), exs, seenRows);
                groups.Add(group);
            }

            var manual = models.Where(t => t.Subid.IsNullOrEmpty() || !subIds.Contains(t.Subid)).ToList();
            if (manual.Count > 0)
            {
                var group = existing.GetValueOrDefault(string.Empty) ?? CreateGroup(string.Empty);
                group.ApplyInfo(null, null, now);
                SyncServers(group, manual, exs, seenRows);
                groups.Add(group);
            }

            foreach (var id in _rows.Keys.Where(id => !seenRows.Contains(id)).ToList())
            {
                _rows.Remove(id);
            }

            if (!groups.SequenceEqual(Groups))
            {
                Groups.Clear();
                foreach (var group in groups)
                {
                    Groups.Add(group);
                }
            }

            HasGroups = Groups.Count > 0;
            IsEmpty = models.Count == 0;
            ActiveServerName = _rows.GetValueOrDefault(_config.IndexId ?? string.Empty)?.Name ?? string.Empty;
            PingShowIcon = _config.SpeedTestItem.PingShowIcon;
        }
        catch (Exception ex)
        {
            Logging.SaveLog("HomeViewModel.RefreshAsync", ex);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private ServerGroupViewModel CreateGroup(string subId)
    {
        var collapsed = _config.UiItem.CollapsedGroups?.Contains(subId) == true;
        var group = new ServerGroupViewModel(this, subId, !collapsed);
        if (_config.UiItem.GroupSortModes?.TryGetValue(subId, out var mode) == true && Enum.IsDefined(typeof(EServerSortMode), mode))
        {
            group.SortMode = (EServerSortMode)mode;
        }
        return group;
    }

    private void SyncServers(ServerGroupViewModel group, IEnumerable<ProfileItemModel> models,
        Dictionary<string, ProfileExItem> exs, HashSet<string> seenRows)
    {
        var rows = new List<ServerItemViewModel>();
        foreach (var model in models)
        {
            if (model.IndexId.IsNullOrEmpty() || !seenRows.Add(model.IndexId))
            {
                continue;
            }
            if (!_rows.TryGetValue(model.IndexId, out var row) || row.SubId != (model.Subid ?? string.Empty) && !group.IsManual)
            {
                row = new ServerItemViewModel(this, model.IndexId, model.Subid ?? string.Empty);
                _rows[model.IndexId] = row;
            }
            row.Update(model);
            var ex = exs.GetValueOrDefault(model.IndexId);
            row.DefaultSort = ex?.Sort ?? 0;
            if (row.DelayState != EDelayState.Testing)
            {
                row.SetDelay(ex?.Delay ?? 0);
            }
            row.IsSelected = row.IndexId == _config.IndexId;
            rows.Add(row);
        }
        ApplyOrder(group, rows);
    }

    private static void ApplyOrder(ServerGroupViewModel group, List<ServerItemViewModel> rows)
    {
        var ordered = SortRows(rows, group.SortMode);
        if (!ordered.SequenceEqual(group.Servers))
        {
            group.Servers.Clear();
            foreach (var row in ordered)
            {
                group.Servers.Add(row);
            }
        }
        group.ServerCount = group.Servers.Count;
    }

    /// <summary>Display order of a group (unit-tested).</summary>
    public static List<ServerItemViewModel> SortRows(IEnumerable<ServerItemViewModel> rows, EServerSortMode mode)
    {
        return mode switch
        {
            EServerSortMode.Ping => rows
                .OrderBy(t => t.Delay > 0 ? 0 : t.Delay == 0 ? 1 : 2)
                .ThenBy(t => t.Delay)
                .ThenBy(t => t.DefaultSort)
                .ToList(),
            EServerSortMode.Name => rows
                .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(t => t.DefaultSort)
                .ToList(),
            _ => rows.OrderBy(t => t.DefaultSort).ToList(),
        };
    }

    internal void OnGroupExpandedChanged(ServerGroupViewModel group, bool expanded)
    {
        var list = _config.UiItem.CollapsedGroups ??= [];
        var collapsed = list.Contains(group.SubId);
        if (collapsed == !expanded)
        {
            return;
        }
        if (expanded)
        {
            list.Remove(group.SubId);
        }
        else
        {
            list.Add(group.SubId);
        }
        _ = ConfigHandler.SaveConfig(_config);
    }

    internal void SetSortMode(ServerGroupViewModel group, EServerSortMode mode)
    {
        group.SortMode = mode;
        var modes = _config.UiItem.GroupSortModes ??= new();
        if (mode == EServerSortMode.Default)
        {
            modes.Remove(group.SubId);
        }
        else
        {
            modes[group.SubId] = (int)mode;
        }
        _ = ConfigHandler.SaveConfig(_config);
        ApplyOrder(group, group.Servers.ToList());
    }

    internal async Task SelectServerAsync(ServerItemViewModel row)
    {
        if (row.IndexId == _config.IndexId)
        {
            return;
        }
        var item = await AppManager.Instance.GetProfileItem(row.IndexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return;
        }
        if (await ConfigHandler.SetDefaultServerIndex(_config, row.IndexId) != 0)
        {
            return;
        }
        foreach (var r in _rows.Values)
        {
            r.IsSelected = r.IndexId == row.IndexId;
        }
        ActiveServerName = row.Name;

        if (RefreshAllFunc != null)
        {
            await RefreshAllFunc();
        }
        // Reconnects automatically when the tunnel is on (the reload is a no-op otherwise).
        if (ReloadFunc != null)
        {
            await ReloadFunc();
        }
    }

    internal async Task SelectFastestAsync(ServerGroupViewModel group)
    {
        if (group.Servers.All(t => t.Delay <= 0))
        {
            await PingServersAsync(group.Servers.ToList(), group);
        }
        var fastest = group.Servers.Where(t => t.Delay > 0).MinBy(t => t.Delay);
        if (fastest != null)
        {
            await SelectServerAsync(fastest);
        }
    }

    #endregion Groups and servers

    #region Ping

    public async Task PingAllAsync()
    {
        if (IsPingingAll)
        {
            return;
        }
        IsPingingAll = true;
        try
        {
            var groups = Groups.Where(t => !t.IsPinging).ToList();
            foreach (var group in groups)
            {
                group.IsPinging = true;
            }
            try
            {
                await PingServersAsync(groups.SelectMany(t => t.Servers).ToList());
            }
            finally
            {
                foreach (var group in groups)
                {
                    group.IsPinging = false;
                    if (group.SortMode == EServerSortMode.Ping)
                    {
                        ApplyOrder(group, group.Servers.ToList());
                    }
                }
            }
        }
        finally
        {
            IsPingingAll = false;
        }
    }

    /// <summary>Pings the rows in parallel (method from Settings → Ping); results appear as they arrive.</summary>
    internal async Task PingServersAsync(IReadOnlyList<ServerItemViewModel> rows, ServerGroupViewModel? group = null)
    {
        if (rows.Count == 0 || group?.IsPinging == true)
        {
            return;
        }
        if (group != null)
        {
            group.IsPinging = true;
        }
        try
        {
            var profiles = await AppManager.Instance.GetProfileItemsOrderedByIndexIds(rows.Select(t => t.IndexId));
            if (profiles.Count == 0)
            {
                return;
            }
            foreach (var row in rows)
            {
                row.SetTesting();
            }
            var action = _config.SpeedTestItem.PingMethod == EPingMethod.Tcp ? ESpeedActionType.Tcping : ESpeedActionType.Realping;
            var service = new SpeedtestService(_config, OnSpeedTestResult);
            await Task.Run(() => service.RunLoop(action, profiles));
        }
        catch (Exception ex)
        {
            Logging.SaveLog("HomeViewModel.Ping", ex);
        }
        finally
        {
            foreach (var row in rows.Where(t => t.DelayState == EDelayState.Testing))
            {
                row.SetDelay(row.Delay);
            }
            if (group != null)
            {
                group.IsPinging = false;
                if (group.SortMode == EServerSortMode.Ping)
                {
                    ApplyOrder(group, group.Servers.ToList());
                }
            }
        }
    }

    private Task OnSpeedTestResult(SpeedTestResult result)
    {
        RxSchedulers.MainThreadScheduler.Schedule(() => SetDelayResult(result.IndexId, result.Delay));
        return Task.CompletedTask;
    }

    /// <summary>Applies a latency result (also used for the availability check after connecting).</summary>
    public void SetDelayResult(string? indexId, string? delay)
    {
        if (indexId.IsNullOrEmpty() || delay.IsNullOrEmpty() || !_rows.TryGetValue(indexId, out var row))
        {
            return;
        }
        if (int.TryParse(delay, out var value))
        {
            row.SetDelay(value == 0 ? -1 : value);
        }
        else if (delay == ResUI.Speedtesting)
        {
            row.SetTesting();
        }
    }

    #endregion Ping

    #region Server actions

    internal async Task EditServerAsync(ServerItemViewModel row)
    {
        var item = await AppManager.Instance.GetProfileItem(row.IndexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return;
        }

        bool? ret;
        if (item.ConfigType is EConfigType.Custom or EConfigType.Outbound)
        {
            ret = await AppManager.Instance.WindowDialog.ShowDialogAsync(new AddServer2ViewModel(item));
        }
        else if (item.ConfigType.IsGroupType())
        {
            ret = await AppManager.Instance.WindowDialog.ShowDialogAsync(new AddGroupServerViewModel(item));
        }
        else
        {
            ret = await AppManager.Instance.WindowDialog.ShowDialogAsync(new AddServerViewModel(item));
        }
        if (ret != true)
        {
            return;
        }
        await RefreshEverythingAsync();
        if (item.IndexId == _config.IndexId && ReloadFunc != null)
        {
            await ReloadFunc();
        }
    }

    internal async Task ShareServerAsync(ServerItemViewModel row)
    {
        var url = await GetShareUrlAsync(row);
        if (url.IsNotEmpty())
        {
            await ShowQrCodeInteraction.HandleSafe(url);
        }
    }

    internal async Task CopyServerLinkAsync(ServerItemViewModel row)
    {
        var url = await GetShareUrlAsync(row);
        if (url.IsNotEmpty())
        {
            await CopyToClipboardInteraction.HandleSafe(url);
            NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
        }
    }

    private static async Task<string?> GetShareUrlAsync(ServerItemViewModel row)
    {
        var item = await AppManager.Instance.GetProfileItem(row.IndexId);
        if (item is null)
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
            return null;
        }
        var url = FmtHandler.GetShareUri(item);
        if (url.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
        }
        return url;
    }

    internal async Task DeleteServerAsync(ServerItemViewModel row)
    {
        var item = await AppManager.Instance.GetProfileItem(row.IndexId);
        if (item is null)
        {
            return;
        }
        if (await ConfirmInteraction.HandleSafe(string.Format(ResUI.RbDeleteServerConfirm, row.Name), false) != true)
        {
            return;
        }
        var wasActive = item.IndexId == _config.IndexId;
        await ConfigHandler.RemoveServers(_config, [item]);
        await RefreshEverythingAsync();
        if (wasActive && ReloadFunc != null)
        {
            await ReloadFunc();
        }
    }

    #endregion Server actions

    #region Subscription actions

    internal async Task RefreshSubscriptionAsync(ServerGroupViewModel group)
    {
        if (!group.CanRefresh || group.IsRefreshing || UpdateSubscriptionFunc == null)
        {
            return;
        }
        group.IsRefreshing = true;
        try
        {
            await UpdateSubscriptionFunc(group.SubId, IsConnected);
        }
        finally
        {
            group.IsRefreshing = false;
        }
        await RefreshAsync();
    }

    private async Task RefreshAllSubscriptionsAsync()
    {
        if (IsRefreshingAll || UpdateSubscriptionFunc == null)
        {
            return;
        }
        IsRefreshingAll = true;
        var groups = Groups.Where(t => t.CanRefresh).ToList();
        foreach (var group in groups)
        {
            group.IsRefreshing = true;
        }
        try
        {
            await UpdateSubscriptionFunc(string.Empty, IsConnected);
        }
        finally
        {
            foreach (var group in groups)
            {
                group.IsRefreshing = false;
            }
            IsRefreshingAll = false;
        }
        await RefreshAsync();
    }

    private async Task AddSubscriptionAsync()
    {
        var item = new SubItem();
        if (await AppManager.Instance.WindowDialog.ShowDialogAsync(new SubEditViewModel(item)) != true)
        {
            return;
        }
        await RefreshEverythingAsync();
        var subs = await AppManager.Instance.SubItems() ?? [];
        var added = item.Id.IsNotEmpty() ? subs.FirstOrDefault(t => t.Id == item.Id) : subs.LastOrDefault();
        var group = added == null ? null : Groups.FirstOrDefault(t => t.SubId == added.Id);
        if (group != null)
        {
            await RefreshSubscriptionAsync(group);
        }
    }

    internal async Task EditSubscriptionAsync(ServerGroupViewModel group)
    {
        if (group.IsManual)
        {
            return;
        }
        var item = await AppManager.Instance.GetSubItem(group.SubId);
        if (item is null)
        {
            return;
        }
        if (await AppManager.Instance.WindowDialog.ShowDialogAsync(new SubEditViewModel(item)) == true)
        {
            await RefreshEverythingAsync();
        }
    }

    internal async Task DeleteSubscriptionAsync(ServerGroupViewModel group)
    {
        if (group.IsManual)
        {
            return;
        }
        if (await ConfirmInteraction.HandleSafe(string.Format(ResUI.RbDeleteSubscriptionConfirm, group.Name), false) != true)
        {
            return;
        }
        var wasActive = group.Servers.Any(t => t.IndexId == _config.IndexId);
        await ConfigHandler.DeleteSubItem(_config, group.SubId);
        await RefreshEverythingAsync();
        if (wasActive && ReloadFunc != null)
        {
            await ReloadFunc();
        }
    }

    private async Task RefreshEverythingAsync()
    {
        if (RefreshAllFunc != null)
        {
            await RefreshAllFunc();
        }
        else
        {
            await RefreshAsync();
        }
    }

    /// <summary>Opens a provider link (http/https/tg) in the system browser.</summary>
    public static void OpenUrl(string? url)
    {
        if (url.IsNullOrEmpty())
        {
            return;
        }
        if (url.StartsWith(Global.HttpsProtocol, StringComparison.OrdinalIgnoreCase)
            || url.StartsWith(Global.HttpProtocol, StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("tg://", StringComparison.OrdinalIgnoreCase))
        {
            ProcUtils.ProcessStart(url);
        }
    }

    #endregion Subscription actions

    /// <summary>Re-reads settings that affect the main screen (after Settings was closed).</summary>
    public void ReloadSettings()
    {
        PingShowIcon = _config.SpeedTestItem.PingShowIcon;
        UpdateConnectionState();
    }

    /// <summary>Pings all servers when "ping on launch" is enabled.</summary>
    public async Task OnStartedAsync()
    {
        if (_config.SpeedTestItem.PingOnLaunch)
        {
            await PingAllAsync();
        }
    }

    public void Stop()
    {
        _disposed = true;
    }
}
