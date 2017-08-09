/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace GamePipeLib.Utils
{
    static public class SteamDirParsingUtils
    {
        //file for categories: C:\Program Files (x86)\Steam\userdata\33810821\7\remote\sharedconfig.vdf
        private static string _SteamDirectory;
        public static string SteamDirectory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_SteamDirectory))
                {
                    try
                    {
                        if (Environment.Is64BitOperatingSystem)
                        {
                            _SteamDirectory = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam\", "InstallPath", @"C:\Program Files (x86)\Steam")?.ToString();
                        }
                        else
                        {
                            _SteamDirectory = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam\", "InstallPath", @"C:\Program Files (x86)\Steam")?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Fatal("Failed to load Steam registry keys due to exception:", ex);
                    }
                    //If the registry keys failed, try Steams usual location
                    if (string.IsNullOrWhiteSpace(_SteamDirectory) || Directory.Exists(_SteamDirectory) == false)
                        _SteamDirectory = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Steam");

                    //If that also failed, try loading from our application settings
                    if (string.IsNullOrWhiteSpace(_SteamDirectory) || Directory.Exists(_SteamDirectory) == false || File.Exists(Path.Combine(_SteamDirectory, "steam.exe")) == false)
                        _SteamDirectory = GamePipeLib.Properties.Settings.Default.ManualSteamDir;

                    //If there was no application setting, then make one
                    if (string.IsNullOrWhiteSpace(_SteamDirectory))
                    {
                        System.Windows.MessageBox.Show("Unable to find Steam, please manually select the Steam executable.", "Can't find Steam", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);

                        var dialog = new System.Windows.Forms.OpenFileDialog()
                        {
                            InitialDirectory = Environment.ExpandEnvironmentVariables("%ProgramFiles%"),
                            CheckFileExists = true,
                            DereferenceLinks = true,
                            AutoUpgradeEnabled = true,
                            Filter = "Executables (*.exe;*.lnk)|*.exe;*.lnk|All files (*.*)|*.*",
                            Title = "Find Steam.exe"
                        };
                        try
                        {
                            dialog.ShowDialog();
                            if (!string.IsNullOrWhiteSpace(dialog.FileName))
                                _SteamDirectory = Path.GetDirectoryName(dialog.FileName);
                        }
                        catch { _SteamDirectory = null; }
                        finally
                        {
                            dialog.Dispose();
                        }

                        if (string.IsNullOrWhiteSpace(_SteamDirectory) || Directory.Exists(_SteamDirectory) == false || File.Exists(Path.Combine(_SteamDirectory, "steam.exe")) == false)
                        {
                            System.Windows.MessageBox.Show("The Steam desktop client is required for Game Pipe to function.\n\nGame Pipe will open the download page for Steam, please restart Game Pipe once Steam is installed.");
                            System.Diagnostics.Process.Start(@"http://store.steampowered.com/about/");
                            System.Windows.Application.Current.Shutdown();
                            //_SteamDirectory = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Steam");  //If the user goofed, just set the directory to something to prevent issues
                        }
                        else
                        {
                            GamePipeLib.Properties.Settings.Default.ManualSteamDir = _SteamDirectory;
                            GamePipeLib.Properties.Settings.Default.Save();
                        }
                    }
                    if (_SteamDirectory != null)
                        Utils.Logging.Logger.Info($"Steam located at {_SteamDirectory}");
                }
                return _SteamDirectory;
            }
            set { _SteamDirectory = value; }
        }

        public static bool IsSteamOpen()
        {
            try
            {
                var value = (int)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam\ActiveProcess", "pid", null);
                return (value != 0);
            }
            catch
            {
                return System.Diagnostics.Process.GetProcessesByName("SteamService").Any();
            }

        }

        public static void CloseSteam()
        {
            var steampath = Path.Combine(SteamDirectory, "steam.exe");
            System.Diagnostics.Process.Start(steampath, "-shutdown");
        }


        //TODO: This would probably be easier to just get a proper JSON parser, I think that is what is used. But for now this is functional, we don't need a lot anyway.
        public static IEnumerable<Tuple<string, string>> ParseStringPairs(string chunkString)
        {
            Regex pairParser = new System.Text.RegularExpressions.Regex(@"^\s*""(?'tag'.*?)""\s*""(?'value'.*?)""[ \t]*$", RegexOptions.Multiline);
            var matches = pairParser.Matches(chunkString);

            return from Match match in matches
                   select new Tuple<string, string>(match.Groups["tag"].Value, match.Groups["value"].Value);
        }

        public static string GetDirectoryNameFromAcf(string acfFile)
        {
            string installDir = null;
            string[] splitString = { @"\\" };
            foreach (var pair in ParseStringPairs(acfFile))
            {
                if (pair.Item1.ToLower() == "installdir")
                {
                    installDir = pair.Item2;
                }
            }
            if (installDir.Contains(@"\\"))
            {
                return installDir.Split(splitString, StringSplitOptions.RemoveEmptyEntries).Last();
            }
            else
            {
                return installDir;
            }

        }

        public static string GetAndReplaceDirectoryNameFromAcf(ref string acfFile)
        {
            string installDir = null;
            string[] splitString = { @"\\" };
            foreach (var pair in ParseStringPairs(acfFile))
            {
                if (pair.Item1.ToLower() == "installdir")
                {
                    installDir = pair.Item2;
                }
            }
            if (installDir.Contains(@"\\"))
            {
                var newDir = installDir.Split(splitString, StringSplitOptions.RemoveEmptyEntries).Last();
                acfFile = acfFile.Replace(installDir, newDir);
                return newDir;
            }
            else
            {
                return installDir;
            }

        }

        public static void RemoveSteamLibrary(string path)
        {
            string libraryVdfPath = Path.Combine(SteamDirectory, @"steamapps\libraryfolders.vdf");
            if (File.Exists(libraryVdfPath))
            {
                var lines = File.ReadAllLines(libraryVdfPath).ToList();
                string searchString = string.Format("\"{0}\"", path).Replace(@"\", @"\\");
                foreach (string line in lines.ToArray())
                {
                    if (line.TrimEnd().EndsWith(searchString, StringComparison.OrdinalIgnoreCase))
                        lines.Remove(line);
                }
                File.WriteAllLines(libraryVdfPath, lines);
            }
        }

        public static void SetupNewSteamLibrary(string path)
        {
            //C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf may not exist
            string libraryVdfPath = Path.Combine(SteamDirectory, @"steamapps\libraryfolders.vdf");
            if (File.Exists(libraryVdfPath))
            {
                var content = File.ReadAllText(libraryVdfPath);
                //Determine the next count to prevent reparses of the libraryfolders.vdf, which deletes any directories that don't exist (such as external drives)
                int count = 1;
                while (content.Contains(string.Format("\"{0}\"", count.ToString())))
                    count++;

                var formattedPath = path.Replace(@"\", @"\\");
                string newLine = "\t\"" + count.ToString() + "\"\t\t\"" + formattedPath + "\"\n}";
                //Now splice in our new line
                content = content.Replace("}", newLine);
                File.WriteAllText(libraryVdfPath, content);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("\"LibraryFolders\"");
                sb.AppendLine("{");
                sb.AppendLine("\t\"TimeNextStatsReport\"\t\"0\"");
                sb.AppendLine("\t\"ContentStatsID\"\t\"0\"");
                sb.AppendLine(string.Format("\t\"1\"\t\"{0}\"", path.Replace(@"\", @"\\")));
                sb.AppendLine("}");
                File.WriteAllText(libraryVdfPath, sb.ToString());
            }


            string sourceSteamDll = Path.Combine(SteamDirectory, "steam.dll");
            string destSteamDll = Path.Combine(path, "steam.dll");

            File.Copy(sourceSteamDll, destSteamDll, true);
            //C:\Program Files (x86)\Steam\config\config.vdf should always exist. NOTE: After testing edits to the above, there isn't a need to edit this file, it will automatically update when libraryFolders.vdf is updated.
            //string configVdfPath = Path.Combine(SteamDirectory,".\\config\\config.vdf");
            //using (Stream stream = new FileStream(configVdfPath, FileMode.Open, FileAccess.ReadWrite))
            //{

            //}
        }
        //public static IEnumerable<Tuple<string, IEnumerable<string>>> SearchSteamDirForPattern(string searchPattern, string directory = "C:\\Program Files (x86)\\Steam\\", IEnumerable<string> filterList = null)
        //{
        //    Regex regexPattern = new Regex(searchPattern, RegexOptions.Multiline);
        //    return SearchSteamDirForPattern(regexPattern, directory, filterList);
        //}

        //public static IEnumerable<Tuple<string, IEnumerable<string>>> SearchSteamDirForPattern(Regex searchPattern, string searchDirectory = "C:\\Program Files (x86)\\Steam\\", IEnumerable<string> filterList = null)
        //{

        //    //Get a list of file names from the following filters:
        //    string[] filters = {
        //        "*.xaml",
        //        "*.vb",
        //        "*.xml",
        //        "*.resx"
        //    };
        //    IEnumerable<FileInfo> files = default(IEnumerable<FileInfo>);
        //    if (filterList != null)
        //    {
        //        files = (from filter in filterList
        //                 let filePaths = Directory.GetFiles(searchDirectory, filter, SearchOption.AllDirectories)
        //                 from path in filePaths
        //                 where path.ToLower().Contains("\\steamapps\\common\\") == false
        //                 select new FileInfo(path));
        //    }
        //    else
        //    {
        //        var temp = Directory.GetFiles(searchDirectory, "*", SearchOption.AllDirectories);
        //        files = (from path in temp where path.ToLower().Contains("\\steamapps\\common\\") == false select new FileInfo(path));
        //    }

        //    //These variables are used for the summary portion of the log file

        //    var startTime = DateTime.Now;


        //    //Parallel.ForEach routine
        //    Action<FileInfo> act = new Action<FileInfo>((FileInfo file) =>
        //    {
        //        string[] contents = null;
        //        try
        //        {
        //            contents = File.ReadAllLines(file._path);
        //            file._matchedStrings = (from line in contents where searchPattern.IsMatch(line) select line).AsParallel().ToArray();

        //        }
        //        catch
        //        {
        //            System.Diagnostics.Debugger.Break();
        //        }
        //    });

        //    Parallel.ForEach<FileInfo>(files, act);


        //    return (from file in files where (file._matchedStrings != null) && file._matchedStrings.Count() > 0 select new Tuple<string, IEnumerable<string>>(file._path, file._matchedStrings)).ToArray();
        //}

        //private class FileInfo
        //{
        //    public string _path;

        //    public IEnumerable<string> _matchedStrings;
        //    public FileInfo(string filePath)
        //    {
        //        _path = filePath;
        //    }
        //}

    }
}
