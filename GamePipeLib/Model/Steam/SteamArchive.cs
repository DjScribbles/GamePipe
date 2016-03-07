/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GamePipeLib.Utils;

namespace GamePipeLib.Model.Steam
{
    //TODO currently, a network copy of a game from a compressed archive will be inflated before going across the network...
    public class SteamArchive : SteamLibrary
    {
        private const string COMPRESSION_EXTENSION = ".gpdeflate";
        public SteamArchive(string libraryDirectory) : base(libraryDirectory, true)
        {
            CompressNewGames = true;
        }

        public bool CompressNewGames { get; set; }

        public override IEnumerable<string> GetFilesForApp(string appId, bool acceptCompressedFiles)
        {
            if (acceptCompressedFiles)
            {
                return base.GetFilesForApp(appId, acceptCompressedFiles);
            }
            else
            {
                return base.GetFilesForApp(appId, acceptCompressedFiles).Select(path => StripFileCompressionExtension(path));
            }
        }

        public override Stream GetReadFileStream(string appId, string file, bool acceptCompressedFiles)
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
                    return FileUtils.OpenCompressedReadStream(compressedPath);
                }
            }

            return FileUtils.OpenReadStream(fullPath);
        }


        public override CrcStream GetWriteFileStream(string file)
        {
            //if the incoming file doesn't end with COMPRESSION_EXTENSION, and we're compressing files, then open as a deflate stream.
            if (CompressNewGames && !file.EndsWith(COMPRESSION_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                var fullPath = Path.GetFullPath(Path.Combine(SteamDirectory, file + COMPRESSION_EXTENSION));
                return FileUtils.OpenCompressedWriteStream(fullPath);
            }
            else
            {
                return base.GetWriteFileStream(file);
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
    }

}
