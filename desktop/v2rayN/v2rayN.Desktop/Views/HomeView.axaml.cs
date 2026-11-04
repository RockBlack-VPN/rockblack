using DialogHostAvalonia;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

/// <summary>
/// RockBlack UI 3.0 main screen (branding/DESIGN.md): power button, routing / mode chips and the
/// subscription cards. A centred column (max 560 px) on narrow windows, two panes (status left,
/// cards right) on wide windows.
/// </summary>
public partial class HomeView : ReactiveUserControl<HomeViewModel>
{
    /// <summary>Width from which the two-pane layout is used.</summary>
    public const double WideLayoutWidth = 900;

    private bool? _wide;

    /// <summary>"Routing settings…" in the routing sheet; handled by the main window.</summary>
    public event EventHandler? RoutingSettingsRequested;

    public HomeView()
    {
        InitializeComponent();

        SizeChanged += (_, e) => UpdateLayoutMode(e.NewSize.Width);

        this.WhenActivated(disposables =>
        {
            ViewModel?.CopyToClipboardInteraction.RegisterHandler(async interaction =>
            {
                await AvaUtils.SetClipboardData(this, interaction.Input);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel?.ShowQrCodeInteraction.RegisterHandler(async interaction =>
            {
                if (interaction.Input.IsNotEmpty())
                {
                    await DialogHost.Show(new QrcodeView(interaction.Input));
                }
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel?.ConfirmInteraction.RegisterHandler(async interaction =>
            {
                var result = await UI.ShowYesNo(interaction.Input);
                interaction.SetOutput(result == ButtonResult.Yes);
            }).DisposeWith(disposables);
        });
    }

    private void UpdateLayoutMode(double width)
    {
        var wide = width >= WideLayoutWidth;
        if (_wide == wide)
        {
            return;
        }
        _wide = wide;

        Detach(statusPanel);
        Detach(groupsPanel);
        if (wide)
        {
            wideLeft.Child = statusPanel;
            wideRight.Children.Add(groupsPanel);
        }
        else
        {
            narrowHost.Children.Add(statusPanel);
            narrowHost.Children.Add(groupsPanel);
        }
        narrowScroll.IsVisible = !wide;
        wideGrid.IsVisible = wide;
    }

    private static void Detach(Control control)
    {
        switch (control.Parent)
        {
            case Panel panel:
                panel.Children.Remove(control);
                break;

            case Decorator decorator:
                decorator.Child = null;
                break;
        }
    }

    private void RoutingItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RoutingProfileViewModel profile } && ViewModel != null)
        {
            System.Windows.Input.ICommand command = ViewModel.SelectRoutingCmd;
            if (command.CanExecute(profile))
            {
                command.Execute(profile);
            }
        }
        btnRouting.Flyout?.Hide();
    }

    private void RoutingSettings_Click(object? sender, RoutedEventArgs e)
    {
        btnRouting.Flyout?.Hide();
        RoutingSettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
