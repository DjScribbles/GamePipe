/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GamePipeLib.Interfaces;
using GamePipeLib.Utils;
using System.Collections.ObjectModel;

namespace GamePipeLib.Model.Steam
{
    //    public abstract class LibraryBase : NotifyPropertyChangedBase, IAppProvider
    //    {
    //        public bool CanCopy(string appId)
    //        {
    //            return true;
    //        }

    //        public abstract string GetAcfFileContent(string appId);

    //        public IEnumerable<BasicSteamApp> GetAvailableIds()
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public IEnumerable<string> GetDirectoriesForApp(string appId)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public IEnumerable<string> GetFilesForApp(string appId, bool acceptCompressedFiles)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public Stream GetFileStream(string appId, string file, bool acceptCompressedFiles)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        public long GetMeasuredGameSize(string appId)
    //        {

    //        }

    //        public uint GetTransferredCrc(string appId, string file)
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    public class SteamLibrary : NotifyPropertyChangedBase, IAppProvider, ITransferTarget
    {
        private System.IO.FileSystemWatcher _watcher;
        public readonly bool _isArchive;

        public SteamLibrary(string libraryDirectory)
        {
            _LibraryDirectory = libraryDirectory;
            _isArchive = false;
            if (Directory.Exists(_LibraryDirectory))
            {
                _watcher = new FileSystemWatcher(libraryDirectory, "*.acf") { EnableRaisingEvents = true, IncludeSubdirectories = false };
                _watcher.Created += _watcher_Created;
                _watcher.Deleted += _watcher_Deleted;
                _watcher.Changed += _watcher_Changed;
            }
        }

        protected SteamLibrary(string libraryDirectory, bool isArchive)
        {
            _LibraryDirectory = libraryDirectory;
            _isArchive = isArchive;
            if (Directory.Exists(_LibraryDirectory))
            {
                _watcher = new FileSystemWatcher(libraryDirectory, "*.acf") { EnableRaisingEvents = true, IncludeSubdirectories = false };
                _watcher.Created += _watcher_Created;
                _watcher.Deleted += _watcher_Deleted;
                _watcher.Changed += _watcher_Changed;
            }
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Action onChange = new Action(() =>
            {
                var appId = e.Name.ToLower().Replace(".acf", "").Replace("appmanifest_", "");
                var game = GetGameById(appId);
                if (game != null)
                {
                    game.RefreshFromAcf();
                }
                else
                {
                    try
                    {
                        game = new SteamApp(e.FullPath);

                    }
                    catch (Exception)
                    {
                        return;
                    }
                    if (GetGameById(game.AppId) == null)
                    {
                        _Games.Insert(0, game);
                    }
                }
            });

            if (SteamBase.UiDispatcher != null)
            {
                SteamBase.UiDispatcher.BeginInvoke(onChange);
            }
            else
            {
                onChange();
            }
        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Action onChange = new Action(() =>
            {

                var appId = e.Name.ToLower().Replace(".acf", "").Replace("appmanifest_", "");
                var game = GetGameById(appId);
                _Games.Remove(game);
            });
            if (SteamBase.UiDispatcher != null)
            {
                SteamBase.UiDispatcher.BeginInvoke(onChange);
            }
            else
            {
                onChange();
            }
        }

        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            Action onChange = new Action(() =>
            {
                SteamApp game;
                try
                {
                    game = new SteamApp(e.FullPath);

                }
                catch (Exception)
                {
                    return;
                }
                if (GetGameById(game.AppId) == null)
                {
                    _Games.Insert(0, game);
                }
            });
            if (SteamBase.UiDispatcher != null)
            {
                SteamBase.UiDispatcher.BeginInvoke(onChange);
            }
            else
            {
                onChange();
            }
        }

        private readonly string _LibraryDirectory;
        public string SteamDirectory
        {
            get { return _LibraryDirectory; }
        }

        private ObservableCollection<SteamApp> _Games;
        public ObservableCollection<SteamApp> Games
        {
            get
            {
                if ((_Games == null))
                {
                    _Games = new ObservableCollection<SteamApp>(GenerateGames());
                }
                return _Games;
            }
        }

        private IEnumerable<SteamApp> GenerateGames()
        {
            if (string.IsNullOrWhiteSpace(SteamDirectory) || Directory.Exists(SteamDirectory) == false)
            {
                return null;
            }
            var files = Directory.EnumerateFiles(SteamDirectory, "*.acf");
            return (from file in files
                    let game = new SteamApp(file)
                    where string.IsNullOrWhiteSpace(game.GameName) == false
                    where string.IsNullOrWhiteSpace(game.AppId) == false
                    orderby game.GameName
                    select game).ToList();
        }

        public void Refresh()
        {
            _Games = null;
            NotifyPropertyChanged("Games");
        }

        public SteamApp GetGameById(string id)
        {
            return Games.Where(x => x.AppId == id).FirstOrDefault();
        }

        public IEnumerable<BasicSteamApp> GetAvailableIds()
        {
            return Games.Select(x => new BasicSteamApp(x));
        }

        public virtual IEnumerable<string> GetFilesForApp(string appId, bool acceptCompressedFiles)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));

            string baseDir = game.GameDir + "\\";
            return Directory.EnumerateFiles(game.GameDir, "*", SearchOption.AllDirectories).OrderByDescending(x => (new FileInfo(x)).Length)
                .Select(path => path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase)
                                ? path.Substring(baseDir.Length)
                                : path);

        }

        public virtual IEnumerable<string> GetDirectoriesForApp(string appId)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));

            string baseDir = SteamDirectory + "\\";
            return Directory.EnumerateDirectories(game.GameDir, "*", SearchOption.AllDirectories).DefaultIfEmpty(game.GameDir)
                .Select(path => path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase)
                                ? path.Substring(baseDir.Length)
                                : path);
        }

        public bool CanCopy(string appId)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));

            return game.CanCopy();
        }

        Stream IAppProvider.GetFileStream(string appId, string file, bool acceptCompressedFiles)
        {
            return GetReadFileStream(appId, file, acceptCompressedFiles);
        }


        public uint GetTransferredCrc(string appId, string file)
        {
            throw new NotSupportedException("Cast the read stream to a CrcStream and get its crc instead.");
        }

        public string GetAcfFileContent(string appId)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));

            return File.ReadAllText(game.AcfFile);
        }

        CrcStream ITransferTarget.GetFileStream(string installDir, string file)
        {
            string path = Path.Combine(SteamDirectory, "common", installDir, file);
            return GetWriteFileStream(path);
        }

        public void WriteAcfFile(string appId, string contents)
        {
            string acfFileName = string.Format("appmanifest_{0}.acf", appId);
            string acfPath = Path.Combine(SteamDirectory, acfFileName);

            SteamDirParsingUtils.GetAndReplaceDirectoryNameFromAcf(ref contents);   //This will strip any absolute pathing on the install dir, absolute paths cause issues when moving things around, and are uneccessary.

            TransferManager.Instance.AcfFileWatchList.Remove(acfPath.ToLower());

            File.WriteAllText(acfPath, contents);
        }

        public bool HasApp(string appId)
        {
            var game = GetGameById(appId);
            return (game != null);
        }

        public bool HasGameDir(string installDir)
        {
            string path = Path.Combine(SteamDirectory, "common", installDir);
            return (Directory.Exists(path));
        }

        public void OpenGameDir(string installDir, bool openInstallDir = true, bool openBackupDirToo = false)
        {
            if (openInstallDir)
            {
                string path = Path.Combine(SteamDirectory, "common", installDir);
                try
                {
                    if (Directory.Exists(path)) System.Diagnostics.Process.Start(path);
                }
                catch (Exception) { }
            }
            if (openBackupDirToo)
            {
                string backupDir = string.Format("_gpbackup_{0}", installDir);
                string backupPath = Path.Combine(SteamDirectory, "common", backupDir);
                try
                {
                    if (Directory.Exists(backupPath)) System.Diagnostics.Process.Start(backupPath);
                }
                catch (Exception) { }
            }
        }

        public virtual Stream GetReadFileStream(string appId, string file, bool acceptCompressedFiles)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));
            var gameDir = Path.GetFullPath(game.GameDir);
            var fullPath = Path.GetFullPath(Path.Combine(gameDir, file));

            if (fullPath.StartsWith(gameDir, StringComparison.OrdinalIgnoreCase) == false)
                throw new ArgumentException(string.Format("The file request is outside the game directory for {0}. File: {1}", game.GameName, file));

            return FileUtils.OpenReadStream(fullPath);
        }

        public virtual CrcStream GetWriteFileStream(string file)
        {
            var fullPath = Path.GetFullPath(Path.Combine(SteamDirectory, file));

            return FileUtils.OpenWriteStream(fullPath);
        }

        public void CreateDirectories(IEnumerable<string> directories)
        {
            var fullPaths = directories.Select(dir => Path.GetFullPath(Path.Combine(SteamDirectory, dir)));

            foreach (string path in fullPaths)
            {
                Directory.CreateDirectory(path);
            }

            if (_watcher == null && Directory.Exists(_LibraryDirectory))
            {
                _watcher = new FileSystemWatcher(_LibraryDirectory, "*.acf") { EnableRaisingEvents = true, IncludeSubdirectories = false };
                _watcher.Created += _watcher_Created;
                _watcher.Deleted += _watcher_Deleted;
                _watcher.Changed += _watcher_Changed;
            }
        }


        public void BackupExistingDir(string installDir)
        {
            string path = Path.Combine(SteamDirectory, "common", installDir);
            if (Directory.Exists(path))
            {
                string backupDir = string.Format("_gpbackup_{0}", installDir);
                string backupPath = Path.Combine(SteamDirectory, "common", backupDir);

                if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, true);
                Directory.Move(path, backupPath);
                //Let any exceptions bubble up to the caller
                Logging.Logger.InfoFormat("Backed up already existing files from game directory {0} to backup directory {1}.", installDir, backupDir);
            }
        }

        public void RestoreBackupToDir(string installDir)
        {
            string path = Path.Combine(SteamDirectory, "common", installDir);
            string backupDir = string.Format("_gpbackup_{0}", installDir);
            string backupPath = Path.Combine(SteamDirectory, "common", backupDir);
            if (Directory.Exists(backupPath))
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                    Directory.Move(backupPath, path);
                    Logging.Logger.InfoFormat("Restored previously existing files from backup directory {0} to game directory {1}.", backupDir, installDir);
                }
                catch (Exception ex)
                {
                    Task.Run(() =>
                    {
                        Logging.Logger.Error(string.Format("Failed to restore previously existing files from backup directory {0} to game directory {1}.", backupDir, installDir), ex);
                        System.Windows.MessageBox.Show(string.Format("Failed to restore previously existing files from backup directory {0} to game directory {1}.\nTo manually restore the backup:\nDelete the directory {1} if it still exists.\nRename the directory {0} to {1}.\n\nPress ok to open game directories now.", backupDir, installDir), "Restore Backup failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                        try
                        {
                            System.Diagnostics.Process.Start(backupPath);
                            if (Directory.Exists(path))
                                System.Diagnostics.Process.Start(path);
                        }
                        catch (Exception) { }
                    });
                }
            }
        }


        public void DisposeOfBackup(string installDir, BackupDisposalProcedure procedure)
        {
            string backupDir = string.Format("_gpbackup_{0}", installDir);
            SteamRoot.Instance.SteamRestartRequired = true;
            switch (procedure)
            {
                case BackupDisposalProcedure.BackupThenDelete:
                    try
                    {
                        DeleteBackupDir(installDir);
                        Logging.Logger.InfoFormat("Deleted backup directory {0} after a successful transfer.", backupDir, installDir);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Error(string.Format("Failed to delete the backup of old files after a successful transfer.\nPlease manually delete the {0} directory.", backupDir), ex);
                        System.Windows.MessageBox.Show(string.Format("Failed to delete the backup of old files after a successful transfer.\nPlease manually delete the {0} directory.\n\nPress ok to the backup directory now.", backupDir), "Delete Backup failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                        OpenGameDir(installDir, false, true);
                    }
                    break;
                case BackupDisposalProcedure.BackupThenMerge:
                    throw new NotImplementedException("Not yet implemented");
                    break;
                case BackupDisposalProcedure.BackupThenOpen:
                    string backupPath = Path.Combine(SteamDirectory, "common", backupDir);
                    if (Directory.Exists(backupPath))
                        OpenGameDir(installDir, true, true);
                    break;
            }
        }

        public void DeleteBackupDir(string installDir)
        {
            string backupDir = string.Format("_gpbackup_{0}", installDir);
            string backupPath = Path.Combine(SteamDirectory, "common", backupDir);
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, true);

        }


        //I'm not sure that there is right answer for how to do this. 
        //public void MergeBackupDir(string installDir)
        //{
        //    string backupDir = string.Format("_gpbackup_{0}", installDir);
        //    string backupPath = Path.Combine(SteamDirectory, "common", backupDir);
        //    if (Directory.Exists(backupPath))
        //    {
        //        string path = Path.Combine(SteamDirectory, "common", installDir);
        //        var filesInBackupDir = Directory.EnumerateFiles(backupPath, "*", SearchOption.AllDirectories).Select(p => p.Replace(backupPath, ".\\").ToLower());
        //        var filesInInstallDir = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Select(p => p.Replace(path, ".\\").ToLower());
        //        var filesToCopyFromBackup = filesInBackupDir.Except(filesInInstallDir);            
        //        foreach (string file in filesToCopyFromBackup)
        //        {
        //            try
        //            {
        //                string fileSource = Path.Combine(backupPath, file);
        //                string fileDest = Path.Combine(path, file);
        //                File.Move(fileSource, fileDest);
        //            }
        //            catch (Exception ex)
        //            {
        //                // log it
        //            }
        //        }
        //        //Could also use file timestamps, but that just seems jankity
        //        //var filesToDeleteFromBackup = filesInBackupDir.Union(filesInInstallDir);
        //        //foreach (string file in filesToDeleteFromBackup)
        //        //{
        //        //}
        //    }
        //}


        private DriveInfo _Drive;
        public DriveInfo Drive
        {
            get
            {
                if (_Drive == null)
                    _Drive = DriveInfo.GetDrives().Where(x => Path.GetFullPath(SteamDirectory).StartsWith(x.Name)).FirstOrDefault();
                return _Drive;
            }
        }

        public void DeleteGameContent(string appId)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));
            game.DeleteGameData();
        }

        public long GetMeasuredGameSize(string appId)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));

            game.MeasureDiskSize();
            return game.DiskSize;
        }

        public long GetFreeSpace()
        {
            return Drive.AvailableFreeSpace;
        }

        internal void ScanWithDefender(string installDir, string appId)
        {
            string path = Path.Combine(SteamDirectory, "common", installDir);
            SteamRoot.Instance.ScanWithDefender(path, appId);
        }
    }
}
