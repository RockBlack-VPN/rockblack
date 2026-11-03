using DialogHostAvalonia;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class StatusBarView : ReactiveUserControl<StatusBarViewModel>
{
    private static Config _config;

    public StatusBarView()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        txtRunningServerDisplay.Tapped += TxtRunningServerDisplay_Tapped;
        txtRunningInfoDisplay.Tapped += TxtRunningServerDisplay_Tapped;
        txtServerName.Tapped += TxtRunningServerDisplay_Tapped;
        btnConnect.Click += BtnConnect_Click;

        this.WhenActivated(disposables =>
        {
            //status bar
            this.OneWayBind(ViewModel, vm => vm.InboundDisplay, v => v.txtInboundDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.InboundLanDisplay, v => v.txtInboundLanDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RunningServerDisplay, v => v.txtRunningServerDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RunningInfoDisplay, v => v.txtRunningInfoDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.SpeedProxyDisplay, v => v.txtSpeedProxyDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.SpeedDirectDisplay, v => v.txtSpeedDirectDisplay.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableTun, v => v.togEnableTun.IsChecked).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SystemProxySelected, v => v.cmbSystemProxy.SelectedIndex).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRouting, v => v.cmbRoutings2.SelectedItem).DisposeWith(disposables);

            //RockBlack status card (view only)
            this.WhenAnyValue(v => v.ViewModel.BlSystemProxySet, v => v.ViewModel.BlSystemProxyPac, (set, pac) => set || pac)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(UpdateConnectState)
                .DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel.RunningServerDisplay)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(UpdateServerCard)
                .DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel.SpeedProxyDisplay)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(s => chipSpeedProxy.IsVisible = s.IsNotEmpty())
                .DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel.SpeedDirectDisplay)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(s => chipSpeedDirect.IsVisible = s.IsNotEmpty())
                .DisposeWith(disposables);

            ViewModel.SetClipboardDataInteraction.RegisterHandler(async interaction =>
            {
                var strData = interaction.Input;
                await AvaUtils.SetClipboardData(this, strData);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.PasswordInputInteraction.RegisterHandler(async interaction =>
            {
                var result = await PasswordInputAsync();
                interaction.SetOutput(result);
            }).DisposeWith(disposables);

            ViewModel.DispatcherRefreshIconInteraction.RegisterHandler(interaction =>
            {
                Dispatcher.UIThread.Post(RefreshIcon, DispatcherPriority.Default);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);
        });

        //spEnableTun.IsVisible = (Utils.IsWindows() || AppHandler.Instance.IsAdministrator);

        if (Utils.IsNonWindows() && cmbSystemProxy.Items.IsReadOnly == false)
        {
            cmbSystemProxy.Items.RemoveAt(cmbSystemProxy.Items.Count - 1);
        }

        // Because this view has not yet been initialized when DispatcherRefreshIconInteraction is first called.
        RefreshIcon();
    }

    private void RefreshIcon()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.Icon = AvaUtils.GetAppIcon(_config.SystemProxyItem.SysProxyType);
            var iconslist = TrayIcon.GetIcons(Application.Current);
            iconslist[0].Icon = desktop.MainWindow.Icon;
            TrayIcon.SetIcons(Application.Current, iconslist);
        }
    }

    private async Task<string?> PasswordInputAsync()
    {
        var dialog = new SudoPasswordInputView();
        var obj = await DialogHost.Show(dialog);

        var password = obj?.ToString();
        if (password.IsNullOrEmpty())
        {
            togEnableTun.IsChecked = false;
            return password;
        }

        AppManager.Instance.LinuxSudoPwd = password;
        return password;
    }

    private void TxtRunningServerDisplay_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        ViewModel?.TestServerAvailability();
    }

    #region RockBlack status card

    /// <summary>The connect button toggles the existing "set / clear system proxy" commands.</summary>
    private void BtnConnect_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }
        var connected = ViewModel.BlSystemProxySet || ViewModel.BlSystemProxyPac;
        System.Windows.Input.ICommand command = connected ? ViewModel.SystemProxyClearCmd : ViewModel.SystemProxySetCmd;
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private void UpdateConnectState(bool connected)
    {
        btnConnect.Classes.Set("connected", connected);
        txtConnectState.Text = connected ? ResUI.RbConnected : ResUI.RbNotConnected;
        txtConnectState.Foreground = this.TryFindResource(connected ? "RB.Primary" : "RB.TextMuted", ActualThemeVariant, out var brush)
            ? brush as IBrush
            : null;
    }

    /// <summary>Splits the upstream summary "[VLESS] name(addr:port)" into chip, title and caption.</summary>
    private void UpdateServerCard(string? summary)
    {
        var text = (summary ?? string.Empty).Trim();
        var protocol = string.Empty;
        if (text.StartsWith('[') && text.IndexOf(']') > 0)
        {
            var end = text.IndexOf(']');
            protocol = text[1..end];
            text = text[(end + 1)..].Trim();
        }

        var address = string.Empty;
        if (text.EndsWith(')') && text.LastIndexOf('(') > 0)
        {
            var start = text.LastIndexOf('(');
            address = text[(start + 1)..^1];
            text = text[..start].Trim();
        }

        txtProtocol.Text = protocol;
        chipProtocol.IsVisible = protocol.IsNotEmpty();
        txtServerName.Text = text.IsNullOrEmpty() ? address : text;
        txtServerAddress.Text = text.IsNullOrEmpty() ? string.Empty : address;
        txtServerAddress.IsVisible = txtServerAddress.Text.IsNotEmpty();
    }

    #endregion RockBlack status card
}
