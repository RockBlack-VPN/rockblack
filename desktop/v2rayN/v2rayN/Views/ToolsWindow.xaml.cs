using v2rayN.Base;

namespace v2rayN.Views;

/// <summary>
/// RockBlack UI 3.0: the upstream views that no longer fit on the main screen
/// (full server table with every bulk action, logs, Clash proxies / connections).
/// Opened from the settings menu; closing only hides it so the log keeps collecting.
/// </summary>
public partial class ToolsWindow
{
    public ToolsWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Title = $"RockBlack - {ResUI.RbAdvancedServers}";

        ViewHost.Show(tabServers, viewModel.ProfilesViewModel);
        ViewHost.Show(tabLogs, viewModel.MsgViewModel);
        ViewHost.Show(tabClashProxies, viewModel.ClashProxiesViewModel);
        ViewHost.Show(tabClashConnections, viewModel.ClashConnectionsViewModel);

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ShowClashUI, v => v.tabClashProxies.Visibility).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.ShowClashUI, v => v.tabClashConnections.Visibility).DisposeWith(disposables);
        });

        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    public void SelectTab(int index)
    {
        if (index >= 0 && index < tabTools.Items.Count)
        {
            tabTools.SelectedIndex = index;
        }
    }
}
