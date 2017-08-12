/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GamePipeLib.Utils;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Text;

namespace GamePipeLib.Model.Steam
{
    //TODO currently, a network copy of a game from a compressed archive will be inflated before going across the network...
    public class SteamArchive : SteamLibrary
    {
        private const string COMPRESSION_EXTENSION = ".gpdeflate";
        public SteamArchive(string libraryDirectory) : base(libraryDirectory, true)
        {
            try
            {
                InitializeArchiveOptions();
            }
            catch (Exception ex)
            {
                Utils.Logging.Logger.Warn($"Error initializing archive options for {libraryDirectory}", ex);
            }
        }

        private bool _CompressNewGames;
        public bool CompressNewGames
        {
            get
            {
                return _CompressNewGames;
            }
            set
            {
                _CompressNewGames = value;
                SaveArchiveOptions();
                NotifyPropertyChanged("CompressNewGames");
            }
        }

        private bool _CopyInOut;
        public bool CopyInOut
        {
            get
            {
                return _CopyInOut;
            }
            set
            {
                _CopyInOut = value;
                SaveArchiveOptions();
                NotifyPropertyChanged("CopyInOut");
            }
        }

        public override IEnumerable<Tuple<string, long>> GetFilesForApp(string appId, bool acceptCompressedFiles)
        {
            if (acceptCompressedFiles)
            {
                return base.GetFilesForApp(appId, acceptCompressedFiles);
            }
            else
            {
                return base.GetFilesForApp(appId, acceptCompressedFiles).Select(pair => new Tuple<string, long>(StripFileCompressionExtension(pair.Item1), pair.Item2));//This is a little innefficient, but not common and only affects the setup of the transfer.
            }
        }

        public override Stream GetReadFileStream(string appId, string file, bool acceptCompressedFiles, bool validation, int bufferSize)
        {
            var game = GetGameById(appId);
            if (game == null) throw new ArgumentException(string.Format("App ID {0} not found in {1}", appId, SteamDirectory));

            var fullPath = Path.GetFullPath(Path.Combine(game.GameDir, file));

            //If the requestor doesn't want compressed files, then we'll open up a stream the inflates the compressed files
            if (acceptCompressedFiles == false)
            {
                var compressedPath = fullPath + COMPRESSION_EXTENSION;
                if (File.Exists(compressedPath))
                {
                    return FileUtils.OpenCompressedReadStream(compressedPath, validation, bufferSize);
                }
            }

            return FileUtils.OpenReadStream(fullPath, validation, bufferSize);
        }


        public override Stream GetWriteFileStream(string file, bool validation, int bufferSize)
        {
            //if the incoming file doesn't end with COMPRESSION_EXTENSION, and we're compressing files, then open as a deflate stream.
            if (CompressNewGames && !file.EndsWith(COMPRESSION_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                var fullPath = Path.GetFullPath(Path.Combine(SteamDirectory, file + COMPRESSION_EXTENSION));
                return FileUtils.OpenCompressedWriteStream(fullPath, validation, bufferSize);
            }
            else
            {
                return base.GetWriteFileStream(file, validation, bufferSize);
            }
        }

        private string StripFileCompressionExtension(string path)
        {
            if (path.EndsWith(COMPRESSION_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(0, (path.Length - COMPRESSION_EXTENSION.Length));
            }
            return path;
        }

        private void InitializeArchiveOptions()
        {
            var optionFilePath = Path.Combine(SteamDirectory, "archiveOptions.txt");
            var searchString = new Regex(@"\s*(?'property'\w+)\s*=\s*(?'value'\w+)\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (File.Exists(optionFilePath))
            {
                var contents = File.ReadAllText(optionFilePath);
                foreach (Match match in searchString.Matches(contents))
                {
                    try
                    {
                        var prop = match.Groups["property"].Value.ToLower();
                        var value = match.Groups["value"].Value.ToLower();
                        if (prop == "compressnewgames")
                        {
                            bool.TryParse(value, out _CompressNewGames);
                        }
                        else if (prop == "copyinout")
                        {
                            bool.TryParse(value, out _CopyInOut);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Error($"Exception parsing {optionFilePath} match '{match.Value}' into properties:", ex);
                    }
                }
            }
        }


        private void SaveArchiveOptions()
        {
            var optionFilePath = Path.Combine(SteamDirectory, "archiveOptions.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"CompressNewGames={CompressNewGames}");
            sb.AppendLine($"CopyInOut={CopyInOut}");
            try
            {
                File.WriteAllText(optionFilePath, sb.ToString());
            }
            catch (Exception ex)
            {
                Logging.Logger.Error($"Exception writing archive options for {optionFilePath}:", ex);
            }
        }
    }

}
