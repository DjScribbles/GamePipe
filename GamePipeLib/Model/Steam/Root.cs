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

        private ObservableCollection<string> _ExternalLibraries = null;

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

            if (_ExternalLibraries != null) _ExternalLibraries.CollectionChanged -= OnExternalLibrariesChanged;
            if (File.Exists("externalLibraries.txt"))
            {
                var lines = File.ReadAllLines("externalLibraries.txt");
                _ExternalLibraries = new ObservableCollection<string>(lines);
                _ExternalLibraries.CollectionChanged += OnExternalLibrariesChanged;
                foreach (var line in lines)
                {
                    if (Directory.Exists(line))
                    {
                        result.Add(new SteamArchive(line));
                    }

                }

            }
            else
            {
                _ExternalLibraries = new ObservableCollection<string>();
                _ExternalLibraries.CollectionChanged += OnExternalLibrariesChanged;
            }
            return result;
        }



        private void OnExternalLibrariesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var sb = new System.Text.StringBuilder();
            foreach (string line in _ExternalLibraries)
            {
                sb.AppendLine(line);
            }
            File.WriteAllText("externalLibraries.txt", sb.ToString());
        }

        public SteamApp GetGame(string appId)
        {
            foreach (var lib in Libraries)
            {
                var game = lib.Games.Where(x => x.AppId == appId).FirstOrDefault();
                if ((game != null) && (game.AppId == appId))
                    return game;
            }
            return null;
        }

        public SteamLibrary GetLibraryForGame(string appId)
        {
            foreach (var lib in Libraries)
            {
                var game = lib.Games.Where(x => x.AppId == appId).FirstOrDefault();
                if ((game != null) && (game.AppId == appId))
                    return lib;
            }
            return null;
        }

        public IEnumerable<SteamApp> GetAllGames()
        {
            return Libraries.SelectMany(x => x.Games);
        }

        public void AddArchive(string path)
        {
            if (Directory.Exists(path) && !_ExternalLibraries.Contains(path))
            {
                _ExternalLibraries.Add(path);

                _Libraries.Add(new SteamArchive(path));
                NotifyPropertyChanged("Libraries");
            }
        }

        public void AddLibrary(string path)
        {
            if (Directory.Exists(path) && !_ExternalLibraries.Contains(path))
            {
                GamePipeLib.Utils.SteamDirParsingUtils.SetupNewSteamLibrary(path);
                _Libraries.Add(new SteamLibrary(Path.Combine(path, "steamapps")));
                NotifyPropertyChanged("Libraries");
            }
        }

        public void ScanWithDefender(string gameDir, string appId)
        {
            if (gameDir == null)
                throw new ArgumentNullException("gameDir");
            if (!Directory.Exists(gameDir))
                throw new ArgumentException("gameDir doesn't exist: " + gameDir);
            string tempFile;
            int i = 0;
            do
            {
                tempFile = Environment.ExpandEnvironmentVariables(string.Format("%TEMP%\\scan_{0}_{1}.bat", appId, (i == 0 ? "" : i.ToString())));
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
    }
}
