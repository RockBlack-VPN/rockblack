using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MaterialDesignThemes.Wpf;

namespace v2rayN.Views;

/// <summary>
/// RockBlack UI 3.0 main screen. Layout only: all state and actions come from the shared
/// ServiceLib HomeViewModel (also used by the Avalonia desktop client).
/// Narrow windows: one centred column (power on top, cards below).
/// Wide windows: two panes (power/status left, subscription cards right).
/// </summary>
public partial class HomeView
{
    private const double TwoPaneMinWidth = 960;
    private bool? _twoPane;

    public HomeView()
    {
        InitializeComponent();

        SizeChanged += (_, e) => UpdateLayoutMode(e.NewSize.Width);
        this.WhenAnyValue(v => v.ViewModel).Subscribe(vm => DataContext = vm);

        this.WhenActivated(disposables =>
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.CopyToClipboardInteraction.RegisterHandler(interaction =>
            {
                WindowsUtils.SetClipboardData(interaction.Input);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.ShowQrCodeInteraction.RegisterHandler(async interaction =>
            {
                await ShowQrCode(interaction.Input);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.ConfirmInteraction.RegisterHandler(interaction =>
            {
                interaction.SetOutput(UI.ShowYesNo(interaction.Input) == MessageBoxResult.Yes);
            }).DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel.ConnectionState)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(UpdatePowerVisual)
                .DisposeWith(disposables);
        });

        btnPower.Loaded += (_, _) => UpdatePowerVisual(ViewModel?.ConnectionState ?? EConnectionState.Disconnected);

#if DEBUG
        StartUiPreview();
#endif
    }

    /// <summary>
    /// Power button fill / glow (200 ms cross-fade): disconnected = surface2, connecting
    /// (first connect or auto-reconnect after a server / routing switch) = half-lit while the
    /// rings pulse, connected = primary + glow. Driven here so every transition animates from
    /// the current value instead of jumping.
    /// </summary>
    private void UpdatePowerVisual(EConnectionState state)
    {
        if (btnPower.Template?.FindName("discOn", btnPower) is not UIElement disc
            || btnPower.Template.FindName("glow", btnPower) is not UIElement glow)
        {
            return;
        }
        var (discTo, glowTo) = state switch
        {
            EConnectionState.Connected => (1.0, 0.35),
            EConnectionState.Connecting => (0.55, 0.12),
            _ => (0.0, 0.0),
        };
        var duration = new Duration(TimeSpan.FromMilliseconds(200));
        disc.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(discTo, duration));
        glow.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(glowTo, duration));
    }

#if DEBUG

    /// <summary>
    /// Debug builds only: RB_UI_PREVIEW=connected|connecting renders that power-button state without
    /// starting the core or touching the system proxy (used for screenshots on developer machines).
    /// </summary>
    private void StartUiPreview()
    {
        var mode = Environment.GetEnvironmentVariable("RB_UI_PREVIEW");
        if (mode is not ("connected" or "connecting"))
        {
            return;
        }
        var started = DateTime.Now.AddSeconds(-988);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        var stopped = false;
        timer.Tick += (_, _) =>
        {
            if (ViewModel == null)
            {
                return;
            }
            if (!stopped)
            {
                ViewModel.Stop(); // freeze the real connection clock so it does not overwrite the preview
                stopped = true;
            }
            var connected = mode == "connected";
            ViewModel.ConnectionState = connected ? EConnectionState.Connected : EConnectionState.Connecting;
            ViewModel.IsConnected = connected;
            ViewModel.IsConnecting = !connected;
            ViewModel.ConnectionStateText = connected ? ResUI.RbConnected : ResUI.RbConnecting;
            ViewModel.SessionTimeText = connected ? HomeViewModel.FormatSessionTime(DateTime.Now - started) : string.Empty;
            ViewModel.ShowSpeed = connected;
            ViewModel.SpeedDownText = HomeViewModel.FormatSpeed(1240 + (DateTime.Now.Second * 7));
            ViewModel.SpeedUpText = HomeViewModel.FormatSpeed(140);
        };
        timer.Start();
    }

#endif

    private MainWindowViewModel? HostViewModel => (Window.GetWindow(this) as MainWindow)?.ViewModel;

    #region Layout

    private void UpdateLayoutMode(double width)
    {
        var twoPane = width >= TwoPaneMinWidth;
        if (_twoPane == twoPane)
        {
            return;
        }
        _twoPane = twoPane;

        if (twoPane)
        {
            colLeft.Width = new GridLength(440);
            colRight.Width = new GridLength(1, GridUnitType.Star);
            rowTop.Height = new GridLength(1, GridUnitType.Star);
            Grid.SetRow(powerPane, 0);
            Grid.SetRowSpan(powerPane, 2);
            Grid.SetColumn(powerPane, 0);
            powerPane.Margin = new Thickness(16, 0, 8, 48);
            Grid.SetRow(listPane, 0);
            Grid.SetRowSpan(listPane, 2);
            Grid.SetColumn(listPane, 1);
            listColumn.Margin = new Thickness(8, 4, 24, 16);
            listColumn.MaxWidth = 640;
        }
        else
        {
            colLeft.Width = new GridLength(1, GridUnitType.Star);
            colRight.Width = new GridLength(0);
            rowTop.Height = GridLength.Auto;
            Grid.SetRow(powerPane, 0);
            Grid.SetRowSpan(powerPane, 1);
            Grid.SetColumn(powerPane, 0);
            powerPane.Margin = new Thickness(16, 0, 16, 8);
            Grid.SetRow(listPane, 1);
            Grid.SetRowSpan(listPane, 1);
            Grid.SetColumn(listPane, 0);
            listColumn.Margin = new Thickness(16, 8, 16, 16);
            listColumn.MaxWidth = 560;
        }
    }

    #endregion Layout

    #region Menus and popups

    private static void OpenOwnMenu(object sender)
    {
        if (sender is FrameworkElement { ContextMenu: { } menu } element)
        {
            menu.DataContext = element.DataContext;
            menu.PlacementTarget = element;
            // the buttons sit at the right edge of the card: open below, right edges aligned
            menu.Placement = PlacementMode.Custom;
            menu.CustomPopupPlacementCallback = (popupSize, targetSize, _) =>
            [
                new CustomPopupPlacement(new System.Windows.Point(targetSize.Width - popupSize.Width, targetSize.Height + 2), PopupPrimaryAxis.Horizontal),
                new CustomPopupPlacement(new System.Windows.Point(targetSize.Width - popupSize.Width, -popupSize.Height - 2), PopupPrimaryAxis.Horizontal),
            ];
            menu.IsOpen = true;
        }
    }

    private void RowMenuButton_Click(object sender, RoutedEventArgs e)
    {
        OpenOwnMenu(sender);
        e.Handled = true;
    }

    private void GroupMenuButton_Click(object sender, RoutedEventArgs e)
    {
        OpenOwnMenu(sender);
        e.Handled = true;
    }

    private void BtnRouting_Click(object sender, RoutedEventArgs e)
    {
        routingPopup.IsOpen = !routingPopup.IsOpen;
    }

    private void RoutingItem_Click(object sender, RoutedEventArgs e)
    {
        routingPopup.IsOpen = false;
    }

    private void BtnRoutingSettings_Click(object sender, RoutedEventArgs e)
    {
        routingPopup.IsOpen = false;
        Execute(HostViewModel?.RoutingSettingCmd);
    }


    private void BtnEmptyClipboard_Click(object sender, RoutedEventArgs e)
    {
        Execute(HostViewModel?.AddServerViaClipboardCmd);
    }

    private static void Execute(ICommand? command)
    {
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }

    private static async Task ShowQrCode(string url)
    {
        if (url.IsNullOrEmpty())
        {
            return;
        }
        var dialog = new QrcodeView()
        {
            imgQrcode = { Source = QRCodeWindowsUtils.GetQRCode(url) },
            txtContent = { Text = url },
        };
        await DialogHost.Show(dialog, "RootDialog");
    }

    #endregion Menus and popups
}
