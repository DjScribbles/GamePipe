/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System.Linq;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;

namespace GamePipe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CloseSteamButton.Click += CloseSteamButton_Click;
            ListGamesInAccountButton.Click += ListGamesInAccountButton_Click;
            PrintActiveProcessesButton.Click += PrintActiveProcessesButton_Click;
            this.Closing += MainWindow_Closing;
            NetworkTab.PreviewKeyDown += NetworkTab_PreviewKeyDown;
            LocalLibsTab.PreviewKeyDown += LocalLibsTab_PreviewKeyDown;
            GamePipeLib.Model.Steam.SteamBase.UiDispatcher = this.Dispatcher;
        }

        private void LocalLibsTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            LocalSearchTextBox.Focus();
            e.Handled = false;
        }

        private void NetworkTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IP_TextBox.IsFocused && !Port_TextBox.IsFocused)
            {
                NetworkSearchTextBox.Focus();
            }
            e.Handled = false;
        }

        private static readonly ViewModel.RootSteamViewModel _rootSteamVm = new ViewModel.RootSteamViewModel();
        public static ViewModel.RootSteamViewModel SteamVM { get { return _rootSteamVm; } }
        private static readonly ViewModel.TransferManagerViewModel _transferVm = new ViewModel.TransferManagerViewModel();
        public static ViewModel.TransferManagerViewModel TransferVM { get { return _transferVm; } }



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

            //if ((e.Cancel == false) && (GamePipeLib.Model.Steam.SteamRoot.Instance.SteamRestartRequired))
            //{
            //    var result = System.Windows.MessageBox.Show("You moved some steam games, they won't be playable unless you restart Steam.\n\nRestart steam now?", "Steam needs to restart", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.Yes);
            //    switch (result)
            //    {
            //        case MessageBoxResult.Yes:
            //            GamePipeLib.Utils.SteamDirParsingUtils.CloseSteam();
            //            try
            //            {
            //                System.Diagnostics.Process.Start(@"DelayedStartSteam.bat"); //TODO replace this with something invisible.
            //            }
            //            catch (Exception) { }
            //            break;
            //        case MessageBoxResult.No:
            //            System.Windows.MessageBox.Show("Make sure you restart Steam soon.\nUntil then, any moved games won't be playable and if they are updated they may redownload to the old location.", "Steam needs to close", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
            //            break;
            //        default:
            //        case MessageBoxResult.Cancel:
            //            e.Cancel = true;
            //            break;
            //    }
            //}
            if ((e.Cancel == false) && (GamePipeLib.Model.Steam.SteamRoot.Instance.SteamRestartRequired))
            {
                var result = System.Windows.MessageBox.Show("You moved some steam games, you must restart Steam.\n\nRestart steam now?", "Steam needs to restart", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.Yes);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                GamePipeLib.Utils.SteamDirParsingUtils.CloseSteam();
                GamePipeLib.Utils.Logging.Logger.Info("Closing Steam to update app data...");
                while (GamePipeLib.Utils.SteamDirParsingUtils.IsSteamOpen())
                    System.Threading.Thread.Sleep(500);

                //Need to do this because steam is a jackass and writes these files without looking...
                foreach (string watchedAcf in manager.AcfFileWatchList)
                {
                    if (System.IO.File.Exists(watchedAcf) && watchedAcf.EndsWith(".acf"))
                    {
                        System.IO.File.Delete(watchedAcf);
                        GamePipeLib.Utils.Logging.Logger.InfoFormat("Re-Deleted {0} which was restored during Steam shutdown...", watchedAcf);
                    }
                }

                Process.Start(@"steam://open/games");

            }

            if (e.Cancel == false)
                manager.RequestShutdown();
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

    }
}

//C:\Program Files (x86)\Steam\userdata\33810821\7\remote\sharedconfig.vdf