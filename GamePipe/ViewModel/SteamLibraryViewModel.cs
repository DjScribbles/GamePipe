/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GamePipeLib.Model.Steam;
using GamePipeLib.Utils;
using System.Collections.Specialized;

namespace GamePipe.ViewModel
{
    public class SteamLibraryViewModel : ViewModelBase
    {
        private string _listFilter = "";
        public readonly bool _isArchive;

        private SteamLibrary _model;
        public SteamLibraryViewModel(SteamLibrary model)
        {
            _model = model;
            UpdateDriveInfo();
            model.Games.CollectionChanged += Games_CollectionChanged;
        }

        private void Games_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("FilteredGames");
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

        public double DriveLetter { get; private set; }
        public double DrivePercentFull { get; private set; }
        public string DriveAvailableSpace { get; private set; }
        public string DriveTotalSpace { get; private set; }

        public string SteamDirectoryShortened
        {
            get
            {
                if (Drive != null)
                    return SteamDirectory.Replace(Drive.RootDirectory.ToString(), "").Replace("SteamApps", "");
                return SteamDirectory.Replace("SteamApps", "");
            }
        }

        public IEnumerable<GameViewModel> FilteredGames
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_listFilter))
                {
                    return _model.Games.Select(x => new GameViewModel(x));
                }

                return (from game in _model.Games
                        where game.GameName.ToLower().Contains(_listFilter.ToLower())
                        select new GameViewModel(game));
            }
        }

        public void Refresh()
        {
            _model.Refresh();
            NotifyPropertyChanged("FilteredGames");
        }

        public void UpdateFilter(string filter)
        {
            _listFilter = filter;
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
        #endregion //Commands
    }
}
