using System.Windows.Controls;
using v2rayN.Manager;

namespace v2rayN.Views;

public partial class StatusBarView
{
    private static Config _config;

    public StatusBarView()
    {
        InitializeComponent();
        _config = AppManager.Instance.Config;

        menuExit.Click += menuExit_Click;
        txtRunningServerDisplay.PreviewMouseDown += txtRunningInfoDisplay_MouseDoubleClick;
        txtRunningInfoDisplay.PreviewMouseDown += txtRunningInfoDisplay_MouseDoubleClick;
        txtServerName.PreviewMouseDown += txtRunningInfoDisplay_MouseDoubleClick;
        btnConnect.Click += BtnConnect_Click;

        this.WhenActivated(disposables =>
        {
            //system proxy
            this.OneWayBind(ViewModel, vm => vm.BlSystemProxyClear, v => v.menuSystemProxyClear2.Visibility, conversionHint: BooleanToVisibilityHints.UseHidden, converter: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlSystemProxySet, v => v.menuSystemProxySet2.Visibility, conversionHint: BooleanToVisibilityHints.UseHidden, converter: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlSystemProxyNothing, v => v.menuSystemProxyNothing2.Visibility, conversionHint: BooleanToVisibilityHints.UseHidden, converter: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlSystemProxyPac, v => v.menuSystemProxyPac2.Visibility, conversionHint: BooleanToVisibilityHints.UseHidden, converter: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxyClearCmd, v => v.menuSystemProxyClear).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxySetCmd, v => v.menuSystemProxySet).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxyNothingCmd, v => v.menuSystemProxyNothing).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxyPacCmd, v => v.menuSystemProxyPac).DisposeWith(disposables);

            //routings and servers
            this.OneWayBind(ViewModel, vm => vm.RoutingItems, v => v.cmbRoutings.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRouting, v => v.cmbRoutings.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlRouting, v => v.menuRoutings.Visibility).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlRouting, v => v.sepRoutings.Visibility).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Servers, v => v.cmbServers.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedServer, v => v.cmbServers.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlServers, v => v.cmbServers.Visibility).DisposeWith(disposables);

            //tray menu
            this.BindCommand(ViewModel, vm => vm.AddServerViaClipboardCmd, v => v.menuAddServerViaClipboard2).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddServerViaScanCmd, v => v.menuAddServerViaScan2).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateCmd, v => v.menuSubUpdate2).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateViaProxyCmd, v => v.menuSubUpdateViaProxy2).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CopyProxyCmdToClipboardCmd, v => v.menuCopyProxyCmdToClipboard).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RunningServerToolTipText, v => v.tbNotify.ToolTipText).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.NotifyLeftClickCmd, v => v.tbNotify.LeftClickCommand).DisposeWith(disposables);

            //status bar
            this.OneWayBind(ViewModel, vm => vm.InboundDisplay, v => v.txtInboundDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.InboundLanDisplay, v => v.txtInboundLanDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RunningServerDisplay, v => v.txtRunningServerDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RunningInfoDisplay, v => v.txtRunningInfoDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.SpeedProxyDisplay, v => v.txtSpeedProxyDisplay.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.SpeedDirectDisplay, v => v.txtSpeedDirectDisplay.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableTun, v => v.togEnableTun.IsChecked).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SystemProxySelected, v => v.cmbSystemProxy.SelectedIndex).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RoutingItems, v => v.cmbRoutings2.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRouting, v => v.cmbRoutings2.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlRouting, v => v.cmbRoutings2.Visibility).DisposeWith(disposables);

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
                .Subscribe(s => chipSpeedProxy.Visibility = s.IsNullOrEmpty() ? Visibility.Collapsed : Visibility.Visible)
                .DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel.SpeedDirectDisplay)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(s => chipSpeedDirect.Visibility = s.IsNullOrEmpty() ? Visibility.Collapsed : Visibility.Visible)
                .DisposeWith(disposables);

            ViewModel.SetClipboardDataInteraction.RegisterHandler(interaction =>
            {
                var strData = interaction.Input;
                WindowsUtils.SetClipboardData(strData);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.DispatcherRefreshIconInteraction.RegisterHandler(interaction =>
            {
                Application.Current?.Dispatcher.Invoke(async () => await RefreshIcon(), DispatcherPriority.Normal);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);
        });

        _ = RefreshIcon();
    }

    private async Task RefreshIcon()
    {
        tbNotify.Icon = await WindowsManager.Instance.GetNotifyIcon(_config);
        Application.Current.MainWindow?.Icon = WindowsManager.Instance.GetAppIcon(_config);
    }

    private async void menuExit_Click(object sender, RoutedEventArgs e)
    {
        tbNotify.Dispose();
        await AppManager.Instance.AppExitAsync(true);
    }

    private void txtRunningInfoDisplay_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.TestServerAvailability();
    }

    #region RockBlack status card

    /// <summary>
    /// The connect button toggles the existing system proxy commands:
    /// "set system proxy" when it is off, "clear system proxy" when it is on (set or PAC).
    /// </summary>
    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }
        var connected = ViewModel.BlSystemProxySet || ViewModel.BlSystemProxyPac;
        ICommand command = connected ? ViewModel.SystemProxyClearCmd : ViewModel.SystemProxySetCmd;
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private void UpdateConnectState(bool connected)
    {
        btnConnect.Tag = connected;
        txtConnectState.Text = connected ? ResUI.RbConnected : ResUI.RbNotConnected;
        txtConnectState.SetResourceReference(TextBlock.ForegroundProperty, connected ? "RB.Primary" : "RB.TextMuted");
    }

    /// <summary>Splits the upstream summary "[VLESS] name(addr:port)" into chip, title and caption.</summary>
    private void UpdateServerCard(string? summary)
    {
        txtRunningServerDisplay.Text = summary ?? string.Empty;
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
        chipProtocol.Visibility = protocol.IsNullOrEmpty() ? Visibility.Collapsed : Visibility.Visible;
        txtServerName.Text = text.IsNullOrEmpty() ? address : text;
        txtServerAddress.Text = text.IsNullOrEmpty() ? string.Empty : address;
        txtServerAddress.Visibility = txtServerAddress.Text.IsNullOrEmpty() ? Visibility.Collapsed : Visibility.Visible;
    }

    #endregion RockBlack status card
}
