/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using GamePipeLib.Interfaces;

//Helpful info about ACF files: https://wiki.singul4rity.com/steam:filestructures:acf
namespace GamePipeLib.Model.Steam
{
    public enum AppStateFlags
    {
        StateInvalid = 0,
        StateUninstalled = 0x01,
        StateUpdateRequired = 0x02,
        StateFullyInstalled = 0x04,
        StateEncrypted = 0x08,
        StateLocked = 0x10,
        StateFilesMissing = 0x20,
        StateAppRunning = 0x40,
        StateFilesCorrupt = 0x80,
        StateUpdateRunning = 0x0100,
        StateUpdatePaused = 0x0200,
        StateUpdateStarted = 0x0400,
        StateUninstalling = 0x0800,
        StateBackupRunning = 0x1000,
        StateReconfiguring = 0x010000,
        StateValidating = 0x020000,
        StateAddingFiles = 0x040000,
        StatePreallocating = 0x080000,
        StateDownloading = 0x100000,
        StateStaging = 0x200000,
        StateCommitting = 0x400000,
        StateUpdateStopping = 0x800000
    }

    public class SteamApp : GamePipeLib.Model.NotifyPropertyChangedBase, ILocalSteamApplication
    {
        private System.IO.FileSystemWatcher _watcher;
        private long _acfDiskSize;
        public SteamApp(string acfFilePath)
        {
            _AcfFile = acfFilePath;
            try
            {
                InitializeFromAcf();
            }
            catch (Exception ex)
            {
                Utils.Logging.Logger.Error($"Failed to initialize new SteamApp object from ACF File: {acfFilePath}", ex);
            }
            //_watcher = new FileSystemWatcher(_AcfFile);
            //_watcher.Changed += _watcher_Changed;
            //_watcher.Deleted += _watcher_Deleted;
        }

        private bool _isMeasured = false;
        public bool SizeIsMeasured { get { return _isMeasured; } }
        public void MeasureDiskSize()
        {
            if (!_isMeasured)
            {
                if (Directory.Exists(GameDir))
                {
                    var info = new DirectoryInfo(GameDir);
                    DiskSize = info.EnumerateFiles("*", SearchOption.AllDirectories).Sum(x => x.Length);
                }
                else
                    DiskSize = 0;
                _isMeasured = true;
            }
        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (_watcher == sender)
            {
                _watcher.Changed -= _watcher_Changed;
                _watcher.Deleted -= _watcher_Deleted;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            RefreshFromAcf();
            _isMeasured = false;
        }

        private readonly string _AcfFile;
        public string AcfFile { get { return _AcfFile; } }

        public string GameName { get; private set; }
        public string AppId { get; private set; }
        public string GameDir { get; private set; }
        public string InstallDir { get; private set; }

        private AppStateFlags _AppState;
        public AppStateFlags AppState
        {
            get { return _AppState; }
            private set
            {
                if (_AppState != value)
                {
                    _AppState = value;
                    NotifyPropertyChanged("AppState");
                }
            }
        }

        private long _DiskSize;
        public long DiskSize
        {
            get { return _DiskSize; }
            private set
            {
                if (_DiskSize != value)
                {
                    _DiskSize = value;
                    NotifyPropertyChanged("DiskSize");
                    NotifyPropertyChanged("ReadableDiskSize");

                }
            }
        }



        public string ReadableDiskSize { get { return Utils.FileUtils.GetReadableFileSize(DiskSize); } }
        public string ImageUrl { get { return this.GetSteamImageUrl(); } }

        public bool CanCopy()
        {
            var ignores = (AppStateFlags.StateFullyInstalled | AppStateFlags.StateEncrypted | AppStateFlags.StateBackupRunning | AppStateFlags.StateCommitting | AppStateFlags.StateUpdateRequired);
            if ((AppState & ~ignores) == 0)
                return true;

            if (!Utils.SteamDirParsingUtils.IsSteamOpen())
                ignores |= AppStateFlags.StateAppRunning | AppStateFlags.StateFilesCorrupt | AppStateFlags.StateFilesMissing | AppStateFlags.StateValidating;

            return (0 == (AppState & ~ignores));
        }

        public bool CanCopyIfForced()
        {
            var ignores = (AppStateFlags.StateFullyInstalled | AppStateFlags.StateEncrypted | AppStateFlags.StateBackupRunning | AppStateFlags.StateCommitting | AppStateFlags.StateUpdateRequired);
            ignores |= AppStateFlags.StateAppRunning | AppStateFlags.StateFilesCorrupt | AppStateFlags.StateFilesMissing | AppStateFlags.StateValidating;

            return ((0 == (AppState & ~ignores)) || !Utils.SteamDirParsingUtils.IsSteamOpen());
        }

        public void InitializeFromAcf()
        {
            var contents = File.ReadAllText(AcfFile);
            var pairs = GamePipeLib.Utils.SteamDirParsingUtils.ParseStringPairs(contents);
            int count = 0;
            foreach (var pair in pairs)
            {
                if (count >= 5)
                {
                    if (!Directory.Exists(GameDir))
                        DiskSize = 0;
                    return;  //return once we've got what we're looking for
                }
                switch (pair.Item1.ToLower())   //Compare in lower case
                {
                    case "appid":
                        AppId = pair.Item2;
                        count++;
                        break;
                    case "stateflags":
                        AppState = (AppStateFlags)long.Parse(pair.Item2);
                        count++;
                        break;
                    case "sizeondisk":
                        DiskSize = long.Parse(pair.Item2);
                        _acfDiskSize = DiskSize;
                        count++;
                        break;
                    case "name":
                        GameName = pair.Item2;
                        count++;
                        break;
                    case "installdir":
                        var subFolders = new string[] { "common", "music" };
                        var folders = subFolders.Select(f => Path.Combine(Path.GetDirectoryName(AcfFile), f)).ToArray();
                        var commonFolder = folders.First();
                        var installDir = pair.Item2.Replace("\\\\", "\\").Trim();//Convert "\\" to "\", double slashes for escape charcters

                        string[] splitString = { "\\" };
                        InstallDir = installDir.Split(splitString, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                        if (string.IsNullOrWhiteSpace(InstallDir))
                        {
                            Utils.Logging.Logger.Error($"Failed to identify Game Directory for ACF File: {AcfFile} {GameName}");
                        }
                        else
                        {
                            GameDir = folders.Select(f => Path.Combine(f, InstallDir)).Where(f => Directory.Exists(f)).FirstOrDefault() ?? Path.Combine(commonFolder, InstallDir);
                        }
                        count++;
                        break;

                }
            }


        }
        public void RefreshFromAcf()
        {
            string contents = null;
            try
            {
                contents = File.ReadAllText(AcfFile);
            }
            catch (Exception) { return; }
            var pairs = GamePipeLib.Utils.SteamDirParsingUtils.ParseStringPairs(contents);
            int count = 0;
            foreach (var pair in pairs)
            {
                if (count >= 2) return;  //return once we've got what we're looking for
                switch (pair.Item1.ToLower())   //Compare in lower case
                {
                    case "stateflags":
                        AppState = (AppStateFlags)long.Parse(pair.Item2);
                        count++;
                        break;
                    case "sizeondisk":
                        var newDiskSize = long.Parse(pair.Item2);
                        if (newDiskSize != _acfDiskSize)
                        {
                            DiskSize = newDiskSize;
                            _acfDiskSize = DiskSize;
                            _isMeasured = false;
                        }
                        count++;
                        break;
                }
            }
        }
        public void DeleteGameData()
        {

            File.Delete(AcfFile);
            Utils.WingmanServer.SendSignal_AddAcfFileToHitList(AcfFile);
            SteamRoot.Instance.SteamRestartRequired = true;
            try
            {
                DeleteDirectoryTree(GameDir, true);
            }
            catch (Exception ex)
            {
                Utils.Logging.Logger.Error(string.Format("Error Deleting tree: {0}", GameDir), ex);
                Task.Run(() =>
                {
                    System.Windows.MessageBox.Show("Cleanup failed. You need to manually delete the directory.\n\nPress Ok to open this directory in explorer.", "Cleanup failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                    try
                    {
                        System.Diagnostics.Process.Start(GameDir);
                    }
                    catch (Exception) { }
                });
            }
        }

        public void DeleteManifest()
        {
            File.Delete(AcfFile);
            SteamRoot.Instance.SteamRestartRequired = true;
        }

        private void DeleteDirectoryTree(string dirPath, bool deleteFiles = false)
        {
            if (Directory.Exists(dirPath))
            {
                var files = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories).ToArray();
                if (deleteFiles == false && files.Any())
                {
                    throw new IOException(string.Format("Directory is not empty: {0}", dirPath));
                }
                else if (deleteFiles == true)
                {
                    foreach (var filePath in files)
                    {
                        File.Delete(filePath);
                    }
                }
                Directory.Delete(dirPath, deleteFiles);
            }
        }
    }
}
