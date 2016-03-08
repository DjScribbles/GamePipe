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
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using GamePipeLib.Interfaces;

namespace GamePipeLib.Model.Steam
{
    public class SteamRoot : NotifyPropertyChangedBase
    {
        private SteamRoot()
        {

        }

        private static SteamRoot _instance = null;
        public static SteamRoot Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SteamRoot();
                return _instance;
            }
        }


        public bool SteamRestartRequired { get; set; }


        public bool ScanAfterNetworkCopy
        {
            get
            {
                return Properties.Settings.Default.ScanAfterNetworkCopy;
            }
            set
            {
                if (Properties.Settings.Default.ScanAfterNetworkCopy != value)
                {
                    Properties.Settings.Default.ScanAfterNetworkCopy = value;
                    NotifyPropertyChanged("ScanAfterNetworkCopy");
                    Properties.Settings.Default.Save();
                }
            }
        }

        public bool OpenDirAfterNetworkCopy
        {
            get
            {
                return Properties.Settings.Default.OpenDirAfterNetworkCopy;
            }
            set
            {
                if (Properties.Settings.Default.OpenDirAfterNetworkCopy != value)
                {
                    Properties.Settings.Default.OpenDirAfterNetworkCopy = value;
                    NotifyPropertyChanged("OpenDirAfterNetworkCopy");
                    Properties.Settings.Default.Save();
                }
            }
        }

        private ObservableCollection<SteamLibrary> _Libraries = null;
        public ObservableCollection<SteamLibrary> Libraries
        {
            get
            {
                if (_Libraries == null)
                {
                    _Libraries = new ObservableCollection<SteamLibrary>(DiscoverLibraries());
                }
                return _Libraries;
            }
        }

        public string SteamDirectory { get { return Utils.SteamDirParsingUtils.SteamDirectory; } }

        private IEnumerable<SteamLibrary> DiscoverLibraries()
        {

            var steamApps = Path.Combine(SteamDirectory, "SteamApps");
            Regex libraryRegex = new Regex("^\\s*\"\\d+\"\\s*\"(?'path'.*)\"\\s*$", RegexOptions.Multiline);
            var libraryFile = Path.Combine(steamApps, "libraryfolders.vdf");
            List<SteamLibrary> result = new List<SteamLibrary>();

            if (Directory.Exists(steamApps) == false)
            {
                return result;
            }
            result.Add(new SteamLibrary(steamApps));

            if (File.Exists(libraryFile))
            {
                var contents = File.ReadAllText(libraryFile);
                var matches = libraryRegex.Matches(contents);
                foreach (Match match in matches)
                {
                    dynamic path = Path.Combine(match.Groups["path"].Value.Replace("\\\\", "\\"), "SteamApps");
                    if (Directory.Exists(path))
                    {
                        result.Add(new SteamLibrary(path));
                    }
                }
            }

            if (Properties.Settings.Default.Archives == null)
                Properties.Settings.Default.Archives = new StringCollection();

            foreach (var item in Properties.Settings.Default.Archives)
            {
                if (Directory.Exists(item))
                {
                    result.Add(new SteamArchive(item));
                }
            }

            return result;
        }


        public ILocalSteamApplication GetGame(string appId)
        {
            foreach (var lib in Libraries)
            {
                var game = lib.GetGameOrBundleById(appId);
                if (game != null)
                    return game;
            }
            return null;
        }

        public SteamLibrary GetLibraryForGame(string appId)
        {
            foreach (var lib in Libraries)
            {
                var game = lib.GetGameOrBundleById(appId);
                if (game != null)
                    return lib;
            }
            return null;
        }

        public IEnumerable<ILocalSteamApplication> GetAllGames()
        {
            return Libraries.SelectMany(x => x.Games);
        }

        public void AddArchive(string path)
        {

            if (Directory.Exists(path) && !Properties.Settings.Default.Archives.Contains(path))
            {
                _Libraries.Add(new SteamArchive(path));
                Properties.Settings.Default.Archives.Add(path);
                Properties.Settings.Default.Save();
            }
        }

        public void RemoveArchive(SteamArchive archive)
        {
            Properties.Settings.Default.Archives.Remove(archive.SteamDirectory);
            Properties.Settings.Default.Save();
            _Libraries.Remove(archive);
        }
        public void AddLibrary(string path)
        {
            if (Directory.Exists(path) && !Properties.Settings.Default.Archives.Contains(path))
            {
                GamePipeLib.Utils.SteamDirParsingUtils.SetupNewSteamLibrary(path);
                SteamRestartRequired = true;
                var libraryDirectory = Path.Combine(path, "SteamApps");
                if (!Directory.Exists(libraryDirectory)) Directory.CreateDirectory(libraryDirectory);
                _Libraries.Add(new SteamLibrary(libraryDirectory));
                NotifyPropertyChanged("Libraries");
            }
        }
        public void RemoveLibrary(SteamLibrary archive)
        {
            _Libraries.Remove(archive);
            var path = archive.SteamDirectory;
            if (path.EndsWith(@"\SteamApps", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - @"\SteamApps".Length);
            GamePipeLib.Utils.SteamDirParsingUtils.RemoveSteamLibrary(path);
        }

        public void ScanWithDefender(string gameDir, string appId)
        {
            if (appId.Contains(","))
                appId = appId.Split(new char[1] { ',' })[0];

            if (gameDir == null)
                throw new ArgumentNullException("gameDir");
            if (!Directory.Exists(gameDir))
                throw new ArgumentException("gameDir doesn't exist: " + gameDir);
            string tempFile;
            int i = 0;
            do
            {
                tempFile = Environment.ExpandEnvironmentVariables(string.Format("%TEMP%\\scan_{0}_{1}.bat", appId, (i == 0 ? "" : i.ToString())));
                i++;
            } while (File.Exists(tempFile));

            var command = Environment.ExpandEnvironmentVariables("%comspec%");
            var args = "/K " + Path.GetFullPath(tempFile) + " & del " + tempFile;
            try
            {
                File.WriteAllText(tempFile, "echo Press Ctrl+C to cancel the scan...\n" + Environment.ExpandEnvironmentVariables(Properties.Settings.Default.DefaultScanner).Replace("{game}", gameDir));

                var proc = System.Diagnostics.Process.Start(command, args);
            }
            catch (Exception ex)
            {
                Utils.Logging.Logger.Error(string.Format("Virus Scan failed due to exception: {0} {1}.", command, args), ex);
                System.Windows.MessageBox.Show("Virus Scan failed due to exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        private bool? _isDefenderPresent = null;
        public bool IsDefenderPresent()
        {
            if (_isDefenderPresent == null)
                _isDefenderPresent = File.Exists(Environment.ExpandEnvironmentVariables("%ProgramW6432%\\Windows Defender\\MpCmdRun.exe"));
            return (bool)_isDefenderPresent;
        }
    }
}
