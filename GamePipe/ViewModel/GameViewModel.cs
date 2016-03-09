/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System;
using System.Diagnostics;
using GamePipeLib.Model.Steam;
using GamePipeLib.Interfaces;

namespace GamePipe.ViewModel
{
    public class BundleViewModel : GameViewModel
    {
        //TODO implement this class to provide a unique view for bundles, showing all app names and app ids.
        public BundleViewModel(ILocalSteamApplication model) : base(model)
        {
        }
    }
    public class GameViewModel : ViewModelBase, ISteamApplication
    {
        private ILocalSteamApplication _model;
        public ILocalSteamApplication Model { get { return _model; } }
        public GameViewModel(ILocalSteamApplication model)
        {
            _model = model;
        }

        //public string AcfFile { get { return _model.AcfFile; } }
        public string AppId { get { return _model.AppId; } }
        //public AppStateFlags AppState { get { return _model.AppState; } }
        public string GameName { get { return _model.GameName; } }
        public string GameDir { get { return _model.GameDir; } }

        public long DiskSize
        {
            get
            {
                return ((ISteamApplication)Model).DiskSize;
            }
        }

        public string ReadableDiskSize
        {
            get
            {
                return ((ISteamApplication)Model).ReadableDiskSize;
            }
        }

        public string InstallDir
        {
            get
            {
                return Model.InstallDir;
            }
        }
        public string ImageUrl { get { return Model.ImageUrl; } }
        public string ReadableFileSize
        {
            get
            {
                return string.Format("~{0}", GamePipeLib.Utils.FileUtils.GetReadableFileSize(DiskSize));
            }
        }

        #region "Commands"
        #region "OpenGameDirCommand"
        private RelayCommand _OpenGameDirCommand = null;
        public RelayCommand OpenGameDirCommand
        {
            get
            {
                if (_OpenGameDirCommand == null)
                    _OpenGameDirCommand = new RelayCommand(x => OpenGameDir());
                return _OpenGameDirCommand;
            }
        }


        public void OpenGameDir()
        {
            try
            {
                System.Diagnostics.Process.Start(GameDir);
            }
            catch (Exception) { }
        }


        #endregion //OpenGameDirCommand
        #region "DeleteGameCommand"
        private RelayCommand _DeleteGameCommand = null;
        public RelayCommand DeleteGameCommand
        {
            get
            {
                if (_DeleteGameCommand == null)
                    _DeleteGameCommand = new RelayCommand(x => DeleteGame());
                return _DeleteGameCommand;
            }
        }


        public void DeleteGame()
        {
            var result = System.Windows.MessageBox.Show(string.Format("Are you sure you want to delete {0}?", GameName), "Delete Game?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Model.DeleteGameData();
            }
        }


        #endregion //DeleteGameCommand
        #region "VerifyGameCommand"
        private RelayCommand _VerifyGameCommand = null;
        public RelayCommand VerifyGameCommand
        {
            get
            {
                if (_VerifyGameCommand == null)
                    _VerifyGameCommand = new RelayCommand(x => VerifyGame());
                return _VerifyGameCommand;
            }
        }


        public void VerifyGame()
        {
            Process.Start(string.Format("steam://validate/{0}", AppId));
        }
        #endregion //VerifyGameCommand
        #region "OpenStorePageCommand"
        private RelayCommand _OpenStorePageCommand = null;
        public RelayCommand OpenStorePageCommand
        {
            get
            {
                if (_OpenStorePageCommand == null)
                    _OpenStorePageCommand = new RelayCommand(x => OpenStorePage());
                return _OpenStorePageCommand;
            }
        }

        public void OpenStorePage()
        {
            Process.Start(string.Format("http://store.steampowered.com/app/{0}/", AppId));
        }
        #endregion //OpenStorePageCommand
        #region "RunGameCommand"
        private RelayCommand _RunGameCommand = null;
        public RelayCommand RunGameCommand
        {
            get
            {
                if (_RunGameCommand == null)
                    _RunGameCommand = new RelayCommand(x => RunGame());
                return _RunGameCommand;
            }
        }

        public void RunGame()
        {
            Process.Start(string.Format("steam://run/{0}", AppId));
        }
        #endregion //RunGameCommand
        #region "ScanWithDefenderCommand"
        private RelayCommand _ScanWithDefenderCommand = null;
        public RelayCommand ScanWithDefenderCommand
        {
            get
            {
                if (_ScanWithDefenderCommand == null)
                    _ScanWithDefenderCommand = new RelayCommand(x => ScanWithDefender(),x=> SteamRoot.Instance.IsDefenderPresent);
                return _ScanWithDefenderCommand;
            }
        }

        public void ScanWithDefender()
        {
            SteamRoot.Instance.ScanWithDefender(GameDir, AppId);
        }
        #endregion //ScanWithDefenderCommand
        #endregion //Commands
    }
}
