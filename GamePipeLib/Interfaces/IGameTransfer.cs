/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

namespace GamePipeLib.Interfaces
{
    public enum TransferStatus
    {
        Queued,
        Preparing,
        TransferingFiles,
        Cleanup,
        Paused,
        WaitingToFinish,
        Finished,
        Aborting,
        Aborted,
        Blocked
    }

    public interface IGameTransfer
    {
        string TransferType { get; }
        TransferStatus Status { get; }
        double Progress { get; }
        ISteamApplication Application { get; }
        //string SourceName { get; }
        //string DestinationName { get; }
    }

}
