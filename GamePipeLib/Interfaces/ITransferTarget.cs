/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System.Collections.Generic;
using GamePipeLib.Utils;
using System.IO;

namespace GamePipeLib.Interfaces
{
    public enum BackupDisposalProcedure
    {
        BackupThenDelete,
        BackupThenMerge,
        BackupThenOpen
    }

    public interface ITransferTarget
    {
        Stream GetFileStream(string installDir, string file, bool validation);
        void CreateDirectories(IEnumerable<string> directories);
        void WriteAcfFile(string appId, string contents);
        bool HasApp(string appId);

        void DisposeOfBackup(string installDir, BackupDisposalProcedure procedure);
        bool HasGameDir(string installDir);
        void BackupExistingDir(string installDir);
        void RestoreBackupToDir(string installDir);
        long GetFreeSpace();
    }

    //public interface ITransferSource : IAppProvider
    //{
    //    string Name { get; }
    //    bool IsBusy { get; }
    //    bool IsReadOnly { get; }
    //    void DeleteApp(string appId);
    //}
    //public class NetworkTransferSource : ITransferSource
    //{
    //    private IAppProvider _provider;
    //    NetworkTransferSource(IAppProvider provider)
    //    {
    //        _provider = provider;
    //    }

    //    public bool IsBusy
    //    {
    //        get
    //        {
    //            return false;
    //        }
    //    }

    //    public bool IsReadOnly
    //    {
    //        get
    //        {
    //            return true;
    //        }
    //    }

    //    public string Name
    //    {
    //        get
    //        {
    //            return "Not Implemented";
    //            //throw new NotImplementedException();
    //        }
    //    }

    //    public bool CanCopy(string appId)
    //    {
    //        return _provider.CanCopy(appId);
    //    }

    //    public void DeleteApp(string appId)
    //    {
    //        throw new NotImplementedException("Cannot delete from network source");
    //    }

    //    public string GetAcfFileContent(string appId)
    //    {
    //        return _provider.GetAcfFileContent(appId);
    //    }

    //    public IEnumerable<BasicSteamApp> GetAvailableIds()
    //    {
    //        return _provider.GetAvailableIds();
    //    }

    //    public IEnumerable<string> GetDirectoriesForApp(string appId)
    //    {
    //        return _provider.GetDirectoriesForApp(appId);
    //    }

    //    public IEnumerable<string> GetFilesForApp(string appId)
    //    {
    //        return _provider.GetFilesForApp(appId);
    //    }

    //    public System.IO.Stream GetFileStream(string appId, string file)
    //    {
    //        return _provider.GetFileStream(appId, file);
    //    }

    //    public uint GetTransferredCrc(string appId, string file)
    //    {
    //        return _provider.GetTransferredCrc(appId, file);
    //    }
    //}
}
