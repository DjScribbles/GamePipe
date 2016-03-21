/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System.Collections.Generic;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.IO;

namespace GamePipeLib.Interfaces
{

    [ServiceContract]
    public interface IAppProvider
    {
        [OperationContract]
        IEnumerable<BasicSteamApp> GetAvailableIds();
        [OperationContract]
        IEnumerable<string> GetFilesForApp(string appId, bool acceptCompressedFiles);
        [OperationContract]
        IEnumerable<string> GetDirectoriesForApp(string appId);
        [OperationContract]
        bool CanCopy(string appId);
        [OperationContract]
        bool CanCopyIfForced(string appId);


        [OperationContract]
        Stream GetFileStream(string appId, string file, bool acceptCompressedFiles);

        [OperationContract]
        uint GetTransferredCrc(string appId, string file);

        [OperationContract]
        string GetAcfFileContent(string appId);
        [OperationContract]
        long GetMeasuredGameSize(string appId);
    }

    
    [DataContract]
    public class BasicSteamApp : ISteamApplication
    {
        public BasicSteamApp()
        {

        }
        public BasicSteamApp(ISteamApplication source)
        {
            GameName = source.GameName;
            AppId = source.AppId;
            DiskSize = source.DiskSize;
            InstallDir = source.InstallDir;
        }

        [DataMember]
        public string InstallDir { get; set; }
        [DataMember]
        public string GameName { get; set; }
        [DataMember]
        public string AppId { get; set; }
        [DataMember]
        public long DiskSize { get; set; }

        public  string ReadableDiskSize { get {return GamePipeLib.Utils.FileUtils.GetReadableFileSize(DiskSize); } }
        public string ImageUrl { get { return this.GetSteamImageUrl(); } }
    }
}
