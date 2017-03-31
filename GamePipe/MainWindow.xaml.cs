/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System.Linq;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System;

namespace GamePipe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            UpdateVm = new UpdateViewModel();
            InitializeComponent();
            CloseSteamButton.Click += CloseSteamButton_Click;
            ListGamesInAccountButton.Click += ListGamesInAccountButton_Click;
            PrintActiveProcessesButton.Click += PrintActiveProcessesButton_Click;
            this.Closing += MainWindow_Closing;
            NetworkTab.PreviewKeyDown += NetworkTab_PreviewKeyDown;
            LocalLibsTab.PreviewKeyDown += LocalLibsTab_PreviewKeyDown;
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Title = $"Game Pipe {version}";
        }

        private Key[] _ignoredKeys =
            { Key.LeftShift, Key.RightShift,
            Key.RightAlt, Key.LeftAlt,
            Key.LeftCtrl, Key.RightCtrl,
            Key.LWin, Key.RWin };

        private void LocalLibsTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;
            if (_ignoredKeys.Contains(e.Key)) return;

            LocalSearchTextBox.Focus();
        }

        private void NetworkTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;
            if (_ignoredKeys.Contains(e.Key)) return;
            if (!IP_TextBox.IsFocused && !Port_TextBox.IsFocused)
            {
                NetworkSearchTextBox.Focus();
            }
        }

        private static readonly ViewModel.RootSteamViewModel _rootSteamVm = new ViewModel.RootSteamViewModel();
        public static ViewModel.RootSteamViewModel SteamVM { get { return _rootSteamVm; } }
        private static readonly ViewModel.TransferManagerViewModel _transferVm = new ViewModel.TransferManagerViewModel();
        public static ViewModel.TransferManagerViewModel TransferVM { get { return _transferVm; } }

        public UpdateViewModel UpdateVm { get; private set; }


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var manager = GamePipeLib.Model.TransferManager.Instance;
            if (manager.Transfers.Any())
            {
                var result = System.Windows.MessageBox.Show("There are still some transfers in progress, they will be aborted if you close.", "Transfers In Progress", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.Cancel);
                if (result == MessageBoxResult.OK)
                {
                    manager.AbortAllTransfers();
                }
                else
                {
                    e.Cancel = true;
                }
            }

            if ((e.Cancel == false) && manager.AcfFileWatchList.Any())
            {
                if (GamePipeLib.Utils.SteamDirParsingUtils.IsSteamOpen())
                {
                    var result = System.Windows.MessageBox.Show("Due to an issue connecting with the GamePipe_Helper app, you must close Steam before closing Game Pipe.\n\nClose Steam now?", "Close Steam?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No);
                    if (result == MessageBoxResult.Yes)
                    {
                        GamePipeLib.Utils.SteamDirParsingUtils.CloseSteam();

                        while (GamePipeLib.Utils.SteamDirParsingUtils.IsSteamOpen())
                            System.Threading.Thread.Sleep(100);

                        foreach (var file in manager.AcfFileWatchList)
                        {
                            try
                            {
                                System.IO.File.Delete(file);
                            }
                            catch (Exception) { }
                        }
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            }
            else if ((e.Cancel == false) && GamePipeLib.Utils.WingmanServer.SteamRestartNeeded())
            {
                var result = System.Windows.MessageBox.Show("You moved (or deleted) some steam games, they won't be playable unless you restart Steam.\n\nRestart Steam now?", "Restart Steam?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    GamePipeLib.Utils.WingmanServer.SendSignal_RestartSteamWhenDone(true);
                    GamePipeLib.Utils.SteamDirParsingUtils.CloseSteam();
                    GamePipeLib.Utils.Logging.Logger.Info("Closing Steam to update app data...");
                }
                else
                {
                    GamePipeLib.Utils.WingmanServer.SendSignal_RestartSteamWhenDone(false);
                    GamePipeLib.Utils.Logging.Logger.Info("Skipped Steam restart.");
                }
            }

            if (e.Cancel == false)
            {
                manager.RequestShutdown();
            }
        }



        void CloseSteamButton_Click(object sender, RoutedEventArgs e)
        {
            //Process.Start(@"steam://nav/console");     
            GamePipeLib.Utils.SteamDirParsingUtils.CloseSteam();
            //useful console commands:
            //install_folder_list
            //install_folder_add
        }

        void ListGamesInAccountButton_Click(object sender, RoutedEventArgs e)
        {
            //var values = GamePipeLib.Utils.SteamWebUtils.ScrapeAllAppIdsFromUserIdPage("76561197994076549");
            var userInfo = new GamePipeLib.Model.Steam.SteamUserInfo();
            var values = userInfo.PrimaryUser.GetAllUsersAppIds();
            var sb = new StringBuilder();
            foreach (string value in values)
            {
                sb.AppendLine(value);
            }

            TestOutput.Text = sb.ToString();
        }
        void PrintActiveProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            //Looking for Steam or SteamService
            var procs = Process.GetProcesses().OrderBy(x => x.ProcessName);

            var sb = new StringBuilder();
            foreach (Process proc in procs)
            {
                sb.AppendLine(proc.ProcessName);
            }
            TestOutput.Text = sb.ToString();
        }

        private void NewVersionHyperlink_Click(object sender, RoutedEventArgs e)
        {
            var link = sender as System.Windows.Documents.Hyperlink;
            if (link != null && link.NavigateUri != null)
                Process.Start(link.NavigateUri.ToString());
        }

        void CloseUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateVm.IsNewVersionAvailable = false;
        }
    }

    public class UpdateViewModel : ViewModel.ViewModelBase
    {
        private bool _IsNewVersionAvailable;
        public bool IsNewVersionAvailable
        {
            get
            {
                return _IsNewVersionAvailable;
            }
            set
            {
                _IsNewVersionAvailable = value;
                NotifyPropertyChanged("IsNewVersionAvailable");
            }
        }
        private string _NewVersionUrl;
        public string NewVersionUrl
        {
            get
            {
                return _NewVersionUrl;
            }
            set
            {
                _NewVersionUrl = value;
                NotifyPropertyChanged("NewVersionUrl");
            }
        }
    }
}
