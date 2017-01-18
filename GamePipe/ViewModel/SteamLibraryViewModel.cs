/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GamePipeLib.Model.Steam;
using GamePipeLib.Utils;
using System.Collections.Specialized;

namespace GamePipe.ViewModel
{
    public class SteamLibraryViewModel : ViewModelBase
    {
        private string _listFilter = "";
        private GameSortMode _sortMode = GameSortMode.AtoZ;

        private SteamLibrary _model;
        public SteamLibraryViewModel(SteamLibrary model)
        {
            _model = model;
            UpdateDriveInfo();
            model.Games.CollectionChanged += Games_CollectionChanged;
            Refresh();
        }

        private void Games_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Refresh();
            UpdateDriveInfo();
        }

        public string SteamDirectory { get { return _model.SteamDirectory; } }
        public SteamLibrary Model { get { return _model; } }

        public DriveInfo Drive { get { return Model.Drive; } }

        private void UpdateDriveInfo()
        {
            if (Drive != null)
            {
                DrivePercentFull = 100.0 * Convert.ToDouble(Drive.TotalSize - Drive.AvailableFreeSpace) / Convert.ToDouble(Drive.TotalSize);
                NotifyPropertyChanged("DrivePercentFull");
                DriveAvailableSpace = FileUtils.GetReadableFileSize(Drive.AvailableFreeSpace);
                NotifyPropertyChanged("DriveAvailableSpace");
                DriveTotalSpace = FileUtils.GetReadableFileSize(Drive.TotalSize);
                NotifyPropertyChanged("DriveTotalSpace");
            }
        }

        public double DrivePercentFull { get; private set; }
        public string DriveAvailableSpace { get; private set; }
        public string DriveTotalSpace { get; private set; }
        public bool IsArchive { get { return Model is SteamArchive; } }


        public string SteamDirectoryShortened
        {
            get
            {
                var shortPath = SteamDirectory;
                if (Drive != null)
                {
                    shortPath = shortPath.Substring(Drive.RootDirectory.ToString().Length);
                }
                if (shortPath.EndsWith("SteamApps", StringComparison.OrdinalIgnoreCase))
                {
                    shortPath = shortPath.Substring(0, shortPath.Length - "SteamApps".Length);
                }
                return shortPath;
            }
        }

        public IEnumerable<GameViewModel> FilteredGames
        {
            get
            {
                IEnumerable<GameViewModel> result;
                bool reverse = (_sortMode == GameSortMode.ZtoA) || (_sortMode == GameSortMode.BiggestToSmallest);
                switch (_sortMode)
                {
                    default:
                        result = _gamesByName;
                        break;

                    case GameSortMode.BiggestToSmallest:
                    case GameSortMode.SmallestToBiggest:
                        result = _gamesBySize;
                        break;
                }

                if (!string.IsNullOrWhiteSpace(_listFilter))
                {
                    result = result.Where(x => x.GameName.ToLower().Contains(_listFilter.ToLower()));
                }

                return (reverse ? result.Reverse() : result);
            }
        }

        private GameViewModel[] _gamesByName;
        private GameViewModel[] _gamesBySize;
        public void Refresh()
        {
            _gamesByName = _model.Games.OrderBy(x => x.GameName).Select(x =>
            {
                if (x is SteamBundle)
                    return new BundleViewModel((SteamBundle)x);
                else
                    return new GameViewModel((SteamApp)x);
            }
            ).ToArray();
            _gamesBySize = _gamesByName.OrderBy(x => x.DiskSize).ToArray();
            NotifyPropertyChanged("FilteredGames");
        }

        private bool _filterUpdateQueued = false;
        public void UpdateFilter(string filter)
        {
            _listFilter = filter;
            if (_filterUpdateQueued == false && SteamBase.UiDispatcher != null)
            {
                _filterUpdateQueued = true;
                SteamBase.UiDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                {
                    _filterUpdateQueued = false;
                    NotifyPropertyChanged("FilteredGames");
                }));
            }
        }

        public void UpdateSortMode(GameSortMode mode)
        {
            _sortMode = mode;
            NotifyPropertyChanged("FilteredGames");
        }

        #region "Commands"
        #region "OpenLibraryCommand"
        private RelayCommand _OpenLibraryCommand = null;
        public RelayCommand OpenLibraryCommand
        {
            get
            {
                if (_OpenLibraryCommand == null)
                    _OpenLibraryCommand = new RelayCommand(x => OpenLibrary());
                return _OpenLibraryCommand;
            }
        }

        public void OpenLibrary()
        {
            try
            {
                System.Diagnostics.Process.Start(SteamDirectory);
            }
            catch (Exception) { }
        }


        #endregion //OpenLibraryCommand
        #region "DeleteSelectedGamesCommand"
        private RelayCommand _DeleteSelectedGamesCommand = null;
        public RelayCommand DeleteSelectedGamesCommand
        {
            get
            {
                if (_DeleteSelectedGamesCommand == null)
                    _DeleteSelectedGamesCommand = new RelayCommand(DeleteSelectedGames, CanDeleteSelectedGames);
                return _DeleteSelectedGamesCommand;
            }
        }

        public void DeleteSelectedGames(object param)
        {
            if (!CanDeleteSelectedGames(param)) return;
            var list = param as System.Collections.IList;
            if (list != null)
            {
                //var items = list.OfType<System.Windows.Controls.ListViewItem>().Select(x => x.DataContext as GameViewModel).Where(x => x != null).ToArray();
                var items = list.OfType<GameViewModel>();
                var sb = new System.Text.StringBuilder();
                var count = items.Count();
                sb.AppendLine($"Are you sure you want to delete the following {count} games:");
                int i = 0;
                foreach (var game in items)
                {
                    if (i < 10 || count < 15)
                        sb.AppendLine(game.GameName);
                    else if (i == 10)
                        sb.AppendLine($"and {count - 9} more...");
                    i++;
                }
                var result = System.Windows.MessageBox.Show(sb.ToString(), $"Delete {count} Games?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    foreach (var game in items)
                    {
                        game.Model.DeleteGameData();
                        GamePipeLib.Utils.Logging.Logger.Info($"Game \"{ game.DisplayName}\" ({game.ReadableDiskSize}) deleted by user request.");
                    }
                }
            }
        }

        public bool CanDeleteSelectedGames(object param)
        {
            var list = param as System.Collections.IList;
            if (list != null)
            {
                var items = list.OfType<GameViewModel>();
                return items.Count() > 1;
            }
            return false;
        }
        #endregion //DeleteSelectedGamesCommand
        #region "RemoveCommand"
        private RelayCommand _RemoveCommand = null;
        public RelayCommand RemoveCommand
        {
            get
            {
                if (_RemoveCommand == null)
                    _RemoveCommand = new RelayCommand(x => Remove());
                return _RemoveCommand;
            }
        }

        public void Remove()
        {
            try
            {
                if (Model is SteamArchive)
                {
                    var result = System.Windows.MessageBox.Show("Are you sure you want to remove this archive from Game Pipe?\n\nNo files will be deleted, and it may be readded at any time.", "Remove Library?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No);
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        SteamRoot.Instance.RemoveArchive(Model as SteamArchive);
                    }
                }
                else
                {
                    var result = System.Windows.MessageBox.Show("Are you sure you want to remove this library from Game Pipe and Steam?\n\nNo files will be deleted, but any games it contains will no longer be playable.", "Remove Library?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No);
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        SteamRoot.Instance.RemoveLibrary(Model);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Error("Library removal failed due to exception:", ex);
                System.Windows.MessageBox.Show("Library removal failed due to exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
            }
        }


        #endregion //RemoveCommand
        #endregion //Commands
    }
}
