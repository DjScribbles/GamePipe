/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.ObjectModel;
using GamePipeLib.Model.Steam;
using System.Collections.Specialized;
using System.Linq;

namespace GamePipe.ViewModel
{
    public class RootSteamViewModel : ViewModelBase
    {

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
                    AddFriend(friend);
                }
            }
        }

        private void Libraries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (SteamLibrary addition in e.NewItems)
                {
                    var newLib = new SteamLibraryViewModel(addition);
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

        public string NewFriendIp { get; set; }
        public ushort NewFriendPort { get; set; }

        public ObservableCollection<FriendViewModel> _Friends = new ObservableCollection<FriendViewModel>();
        public ObservableCollection<FriendViewModel> Friends { get { return _Friends; } }

        private ObservableCollection<SteamLibraryViewModel> _Libraries = null;
        public ObservableCollection<SteamLibraryViewModel> Libraries
        {
            get
            {
                if (_Libraries == null)
                {
                    _Libraries = new ObservableCollection<SteamLibraryViewModel>(_root.Libraries.Select(x => new SteamLibraryViewModel(x)));
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
                _root.AddArchive(path);
                //if (Directory.Exists(path) && !_ExternalLibraries.Contains(path))
                //{
                //    _ExternalLibraries.Add(path);

                //    _Libraries.Add(new SteamLibraryViewModel(path, true));
                //    NotifyPropertyChanged("Libraries");
                //}
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
                    System.Windows.MessageBox.Show("Library Addition failed due to exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
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
    }

}
