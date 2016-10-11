/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System;
using System.IO;
using System.Collections.ObjectModel;
using GamePipeLib.Model.Steam;
using System.Collections.Specialized;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace GamePipe.ViewModel
{
    public class RootSteamViewModel : ViewModelBase
    {

        private static bool _hosting = false;
        private SteamRoot _root;

        public RootSteamViewModel()
        {
            _root = SteamRoot.Instance;
            NewFriendIp = "192.168.1.101";
            NewFriendPort = 41650;
            _root.Libraries.CollectionChanged += Libraries_CollectionChanged;

            if (GamePipe.Properties.Settings.Default.Friends != null)
            {
                foreach (var friend in GamePipe.Properties.Settings.Default.Friends)
                {
                    try
                    {
                        AddFriend(friend);

                    }
                    catch (Exception ex)
                    {
                        GamePipeLib.Utils.Logging.Logger.Error("Failed to restore friend: " + friend, ex);

                    }
                }
            }
        }

        private void Libraries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (SteamLibrary addition in e.NewItems)
                {
                    var newLib = (addition is SteamArchive)
                                ? new SteamArchiveViewModel(addition as SteamArchive)
                                : new SteamLibraryViewModel(addition);
                    newLib.UpdateFilter(LocalListFilter);
                    Libraries.Add(newLib);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SteamLibrary removal in e.OldItems)
                {
                    SteamLibraryViewModel itemToRemove = null;
                    foreach (var lib in Libraries)
                    {
                        if (lib.Model == removal)
                        {
                            itemToRemove = lib;
                            break;
                        }
                    }
                    if (itemToRemove != null)
                        Libraries.Remove(itemToRemove);
                }
            }
        }
        public SteamRoot Model { get { return _root; } }


        public string NewFriendIp { get; set; }
        public ushort NewFriendPort { get; set; }

        private static Thread _hostThread = new Thread(StartHosting) { IsBackground = true };
        private static object _hostLock = new object();
        private static void StartHosting()
        {
            lock (_hostLock)
            {
                Uri baseAddress = new Uri("net.tcp://localhost:41650/gamepipe");
                _Host = new ServiceHost(typeof(GamePipeService.GameProviderService), baseAddress);
            }
        }
        private static ServiceHost _Host = null;
        private ServiceHost Host
        {
            get
            {
                bool waitForHost = false;
                lock (_hostLock)
                {
                    if (_Host == null)
                    {
                        _hostThread.Start();
                        waitForHost = true;
                    }
                }
                while (waitForHost && _Host == null && _hostThread.IsAlive)
                {
                    Thread.Sleep(10);
                }

                return _Host;
            }
        }

        public ObservableCollection<FriendViewModel> _Friends = new ObservableCollection<FriendViewModel>();
        public ObservableCollection<FriendViewModel> Friends { get { return _Friends; } }

        private ObservableCollection<SteamLibraryViewModel> _Libraries = null;
        public ObservableCollection<SteamLibraryViewModel> Libraries
        {
            get
            {
                if (_Libraries == null)
                {
                    _Libraries = new ObservableCollection<SteamLibraryViewModel>(_root.Libraries.Select(x => (x is SteamArchive)
                                                                                                              ? new SteamArchiveViewModel(x as SteamArchive)
                                                                                                              : new SteamLibraryViewModel(x)));
                }
                return _Libraries;
            }
        }

        private string _LocalListFilter;
        public string LocalListFilter
        {
            get { return _LocalListFilter; }
            set
            {
                _LocalListFilter = value;
                foreach (var item in Libraries)
                {
                    item.UpdateFilter(value);
                }
                NotifyPropertyChanged("LocalListFilter");
            }
        }
        private GameSortMode _LocalSortMode = GameSortMode.AtoZ;
        public GameSortMode LocalSortMode
        {
            get { return _LocalSortMode; }
            set
            {
                if (_LocalSortMode != value)
                {
                    _LocalSortMode = value;
                    foreach (var item in Libraries)
                    {
                        item.UpdateSortMode(value);
                    }
                }
                NotifyPropertyChanged("LocalSortMode");
            }
        }

        private string _LanListFilter;
        public string LanListFilter
        {
            get { return _LanListFilter; }
            set
            {
                _LanListFilter = value;
                foreach (var item in Friends)
                {
                    item.UpdateFilter(value);
                }
                NotifyPropertyChanged("LanListFilter");
            }
        }


        #region "Commands"
        #region "AddArchiveCommand"
        private RelayCommand _AddArchiveCommand = null;
        public RelayCommand AddArchiveCommand
        {
            get
            {
                if (_AddArchiveCommand == null)
                {
                    _AddArchiveCommand = new RelayCommand(x => AddArchive());

                }
                return _AddArchiveCommand;
            }
        }

        public void AddArchive()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.SelectedPath);
                try
                {
                    _root.AddArchive(path);
                }
                catch (Exception ex)
                {
                    GamePipeLib.Utils.Logging.Logger.Error("Archive Addition failed due to exception:", ex);
                    System.Windows.MessageBox.Show("Archive Addition failed due to exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                }
            }
        }


        #endregion //AddArchiveCommand
        #region "AddLibraryCommand"
        private RelayCommand _AddLibraryCommand = null;
        public RelayCommand AddLibraryCommand
        {
            get
            {
                if (_AddLibraryCommand == null)
                {
                    _AddLibraryCommand = new RelayCommand(x => AddLibrary());

                }
                return _AddLibraryCommand;
            }
        }

        public void AddLibrary()
        {
            while (GamePipeLib.Utils.SteamDirParsingUtils.IsSteamOpen())
            {
                var msgBoxResult = System.Windows.MessageBox.Show("Please close Steam to continue.", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                if (msgBoxResult != System.Windows.MessageBoxResult.OK)
                    return;
            }

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.SelectedPath);
                try
                {
                    _root.AddLibrary(path);
                }
                catch (Exception ex)
                {
                    GamePipeLib.Utils.Logging.Logger.Error("Library Addition failed due to exception:", ex);
                    System.Windows.MessageBox.Show("Library Addition failed due to exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                }
            }
        }
        #endregion //AddLibraryCommand

        #region "AddFriendCommand"
        private RelayCommand _AddFriendCommand = null;
        public RelayCommand AddFriendCommand
        {
            get
            {
                if (_AddFriendCommand == null)
                {
                    _AddFriendCommand = new RelayCommand(x => AddFriend());

                }
                return _AddFriendCommand;
            }
        }

        public void AddFriend()
        {
            AddFriend(NewFriendIp, NewFriendPort);
        }


        #endregion //AddFriendCommand


        #region "OpenHostCommand"
        private RelayCommand _OpenHostCommand = null;
        public RelayCommand OpenHostCommand
        {
            get
            {
                if (_OpenHostCommand == null)
                {
                    _OpenHostCommand = new RelayCommand(x => OpenHost(), x => !_hosting);

                }
                return _OpenHostCommand;
            }
        }

        public void OpenHost()
        {

            if (_hosting == false)
            {
                try
                {
                    Host.Open();
                    _hosting = true;
                }
                catch (Exception ex)
                {
                    GamePipeLib.Utils.Logging.Logger.Error("Open Host failed due to exception:", ex);
                    System.Windows.MessageBox.Show("Open Host failed due to exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                }
            }
        }
        #endregion //OpenHostCommand

        #region "CloseHostCommand"
        private RelayCommand _CloseHostCommand = null;
        public RelayCommand CloseHostCommand
        {
            get
            {
                if (_CloseHostCommand == null)
                {
                    _CloseHostCommand = new RelayCommand(x => CloseHost(), x => _hosting);

                }
                return _CloseHostCommand;
            }
        }

        public void CloseHost()
        {
            if (_hosting == true)
            {
                try
                {
                    Host.Close();
                    _Host = null;
                    _hostThread.Abort();
                    _hostThread = new Thread(StartHosting) { IsBackground = true };
                    _hosting = false;
                }
                catch (Exception ex)
                {
                    GamePipeLib.Utils.Logging.Logger.Error("Close Host failed due to exception:", ex);
                    System.Windows.MessageBox.Show("Close Host failed due to exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);

                }
            }
        }
        #endregion //CloseHostCommand
        #region "SetSortModeCommand"
        private RelayCommand _SetSortModeCommand = null;
        public RelayCommand SetSortModeCommand
        {
            get
            {
                if (_SetSortModeCommand == null)
                {
                    _SetSortModeCommand = new RelayCommand(x => SetSortMode(x));

                }
                return _SetSortModeCommand;
            }
        }

        public void SetSortMode(object param)
        {
            try
            {
                LocalSortMode = (GameSortMode)param;
            }
            catch (InvalidCastException) { return; }
        }


        #endregion //SetSortModeCommand
        #endregion //Commands


        public void AddFriend(string ipAndPort)
        {
            try
            {
                char[] splits = { ':' };
                var parts = ipAndPort.Split(splits);
                ushort port = Convert.ToUInt16(parts[1]);
                string ip = parts[0];
                AddFriend(ip, port);
            }
            catch (Exception ex)
            {
                GamePipeLib.Utils.Logging.Logger.Error(string.Format("Error adding friend at {0}", ipAndPort), ex);
            }
        }

        public void AddFriend(string ip, ushort port)
        {
            FriendViewModel friend = null;
            try
            {
                friend = new FriendViewModel(ip, port);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format("Failed to connect new friend :(\n\n{0}\n{1}", ex.Message, ex.StackTrace), "Failed To Connect");
            }

            if (friend != null)
            {
                friend.UpdateFilter(LanListFilter);
                Friends.Add(friend);
                friend.Remembered = true;
            }
        }




        //public string SteamDirectory { get { return SteamBase.SteamDirectory; } }


        //private ObservableCollection<SteamLibraryViewModel> _Libraries = null;
        //public IEnumerable<SteamLibraryViewModel> Libraries
        //{
        //    get
        //    {
        //        if (_Libraries == null)
        //        {
        //            _Libraries = new ObservableCollection<SteamLibraryViewModel>(DiscoverLibraries());
        //        }
        //        return _Libraries;
        //    }
        //}
        //private ObservableCollection<string> _ExternalLibraries = null;

        //private IEnumerable<SteamLibraryViewModel> DiscoverLibraries()
        //{

        //    var steamApps = Path.Combine(SteamDirectory, "SteamApps");
        //    Regex libraryRegex = new Regex("^\\s*\"\\d+\"\\s*\"(?'path'.*)\"\\s*$", RegexOptions.Multiline);
        //    var libraryFile = Path.Combine(steamApps, "libraryfolders.vdf");
        //    List<SteamLibraryViewModel> result = new List<SteamLibraryViewModel>();

        //    if (Directory.Exists(steamApps) == false)
        //    {
        //        return result;
        //    }
        //    result.Add(new SteamLibraryViewModel(steamApps));

        //    if (File.Exists(libraryFile))
        //    {
        //        var contents = File.ReadAllText(libraryFile);
        //        var matches = libraryRegex.Matches(contents);
        //        foreach (Match match in matches)
        //        {
        //            dynamic path = Path.Combine(match.Groups["path"].Value.Replace("\\\\", "\\"), "SteamApps");
        //            if (Directory.Exists(path))
        //            {
        //                result.Add(new SteamLibraryViewModel(path));
        //            }
        //        }
        //    }
        //    if (_ExternalLibraries != null) _ExternalLibraries.CollectionChanged -= OnExternalLibrariesChanged;
        //    if (File.Exists("externalLibraries.txt"))
        //    {
        //        var lines = File.ReadAllLines("externalLibraries.txt");
        //        _ExternalLibraries = new ObservableCollection<string>(lines);
        //        _ExternalLibraries.CollectionChanged += OnExternalLibrariesChanged;
        //        foreach (var line in lines)
        //        {
        //            if (Directory.Exists(line))
        //            {
        //                result.Add(new SteamLibraryViewModel(line, true));
        //            }

        //        }

        //    }
        //    else
        //    {
        //        _ExternalLibraries = new ObservableCollection<string>();
        //        _ExternalLibraries.CollectionChanged += OnExternalLibrariesChanged;
        //    }
        //    return result;
        //}

        //private void OnExternalLibrariesChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    var sb = new System.Text.StringBuilder();
        //    foreach (string line in _ExternalLibraries)
        //    {
        //        sb.AppendLine(line);
        //    }
        //    File.WriteAllText("externalLibraries.txt", sb.ToString());
        //}

        //public SteamApp GetGame(string appId)
        //{
        //    foreach (var lib in Libraries)
        //    {
        //        var game = lib.Games.Find(x => x.AppId == appId);
        //        if ((game != null) && (game.AppId == appId))
        //            return game;
        //    }
        //    return null;
        //}
        //public IEnumerable<SteamApp> GetAllGames()
        //{
        //    return Libraries.SelectMany(x => x.Games);
        //}

    }

}
