/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GamePipeLib.Utils;
using GamePipeLib.Model.Steam;
using GamePipeLib.Interfaces;
using System.ServiceModel;

// http://garfoot.com/blog/2008/06/transferring-large-files-using-wcf/
//https://msdn.microsoft.com/en-us/library/ms751515%28v=vs.110%29.aspx
namespace GamePipeService
{

    public class GameProviderService : IAppProvider
    {
        private static Dictionary<string, uint> _validationCache = new Dictionary<string, uint>();


        public GameProviderService()
        {
        }

        const int fileBufferSize = 16384;
        public Stream GetFileStream(string appId, string file, bool acceptCompressedFiles, bool validation, int bufferSize)
        {
            var library = SteamRoot.Instance.GetLibraryForGame(appId);
            if (library == null)
                throw new ArgumentException("library for appId not found");

            var stream = library.GetReadFileStream(appId, file, acceptCompressedFiles, validation, bufferSize);

            if (stream != null && stream is CrcStream)
            {
                var crcStream = (CrcStream)stream;
                OperationContext clientContext = OperationContext.Current;
                clientContext.OperationCompleted += new EventHandler(delegate (object sender, EventArgs args)
                {
                    var key = GetKey(appId, file);
                    _validationCache[key] = crcStream.ReadCrc;
                    crcStream.Dispose();
                });
            }

            return stream;
        }


        public uint GetTransferredCrc(string appId, string file)
        {
            var key = GetKey(appId, file);
            return _validationCache[key];
        }

        private string GetKey(string appId, string file)
        {
            return string.Format("{0}_{1}", appId.ToLower(), file.ToLower());
        }

        public string GetAcfFileContent(string appId)
        {
            var library = SteamRoot.Instance.GetLibraryForGame(appId);
            if (library == null)
                throw new ArgumentException("library for appId not found");

            return library.GetAcfFileContent(appId);
        }

        public IEnumerable<BasicSteamApp> GetAvailableIds()
        {
            IEnumerable<BasicSteamApp> originalList = SteamRoot.Instance.GetAllGames().Select(x => new BasicSteamApp(x)).OrderBy(x => x.GameName);
            //IEnumerable<SteamApp> filteredList = originalList.GroupBy(x => x.AppId).Select(group => group.First());
            return originalList.ToArray();
        }

        public IEnumerable<string> GetDirectoriesForApp(string appId)
        {
            var library = SteamRoot.Instance.GetLibraryForGame(appId);
            if (library == null)
                throw new ArgumentException("library for appId not found");

            return library.GetDirectoriesForApp(appId);
        }

        public IEnumerable<Tuple<string, long>> GetFilesForApp(string appId, bool acceptCompressedFiles)
        {
            var library = SteamRoot.Instance.GetLibraryForGame(appId);
            if (library == null)
                throw new ArgumentException("library for appId not found");

            return library.GetFilesForApp(appId, acceptCompressedFiles);
        }

        public bool CanCopy(string appId)
        {
            var gameInfo = SteamRoot.Instance.GetGame(appId);
            if (gameInfo == null)
                return false;

            return gameInfo.CanCopy();
        }

        public bool CanCopyIfForced(string appId)
        {
            var gameInfo = SteamRoot.Instance.GetGame(appId);
            if (gameInfo == null)
                return false;

            return gameInfo.CanCopyIfForced();
        }

        public long GetMeasuredGameSize(string appId)
        {
            var gameInfo = SteamRoot.Instance.GetGame(appId);
            if (gameInfo == null)
                throw new ArgumentException("appId not found");
            gameInfo.MeasureDiskSize();
            return gameInfo.DiskSize;
        }
    }
}
