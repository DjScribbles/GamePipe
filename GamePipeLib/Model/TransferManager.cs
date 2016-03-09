/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;

namespace GamePipeLib.Model
{


    public class TransferManager : NotifyPropertyChangedBase
    {
        private readonly System.Threading.Thread _workThread;
        private readonly object _transferLock = new object();
        private volatile bool _shutdownRequested = false;
        private TransferManager()
        {
            _Transfers = new ObservableCollection<TransferBase>();
            _workThread = new Thread(WorkThreadWrapper);
            _workThread.Start();
        }

        private static TransferManager _Instance = null;
        public static TransferManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new TransferManager();
                return _Instance;
            }
        }

        public void RequestShutdown()
        {
            _shutdownRequested = true;
        }

        private ObservableCollection<TransferBase> _Transfers;
        public ObservableCollection<TransferBase> Transfers
        {
            get { return _Transfers; }
            private set
            {
                if (_Transfers != value)
                {
                    lock (_transferLock)
                    {
                        _Transfers = value;
                    }
                    Steam.SteamBase.UiDispatcher.BeginInvoke((Action)(() => NotifyPropertyChanged("Transfers")));
                }
            }
        }

        private volatile bool _IsPaused = false;
        public bool IsPaused
        {
            get { return _IsPaused; }
            set { _IsPaused = value; }
        }

        private List<string> _AcfFileWatchList = new List<string>();
        public List<string> AcfFileWatchList { get { return _AcfFileWatchList; } }

        public void AddTransfer(TransferBase transfer)
        {
            lock (_transferLock)
            {
                Steam.SteamBase.UiDispatcher.Invoke((Action)(() => Transfers.Add(transfer)));
            }
        }

        public void AbortAllTransfers()
        {
            lock (_transferLock)
            {
                foreach (var transfer in Transfers.ToArray())   //Get an array copy as we'll be modifying the collection
                    transfer.RequestAbort();
            }
        }

        public void AbortTransfer(TransferBase transfer)
        {
            lock (_transferLock)
            {
                Steam.SteamBase.UiDispatcher.Invoke((Action)(() => Transfers.Remove(transfer)));
            }
        }

        public void ShuffleTransfers(IEnumerable<TransferBase> newOrder)
        {
            lock (_transferLock)
            {
                var newSet = newOrder.Union(Transfers);
                if (Transfers.Count() != newSet.Count())
                    throw new ArgumentException("newOrder does not include all of the existing transfers");
                Transfers = new ObservableCollection<TransferBase>(newSet);
            }
        }

        private void WorkThreadWrapper()
        {
            while (_shutdownRequested == false)
            {
                if (_IsPaused == false)
                {

                    bool workToDo = false;
                    lock (_transferLock)    //Acquire the transfer lock
                    {
                        workToDo = Transfers.Any(x => (x.Status != Interfaces.TransferStatus.Blocked) || (x.CanCopy));
                    }
                    if (workToDo)
                    {
                        DoWork();
                    }
                    else
                    {
                        Thread.Sleep(3000);
                    }
                }
                else
                {
                    Thread.Sleep(3000);
                }
            }
        }


        private void DoWork()
        {
            while (_shutdownRequested == false)
            {

                if (_IsPaused)
                {
                    return;
                }
                else
                {
                    //TODO instead of rearranging transfers, maybe just don't always use first transfer?

                    TransferBase nextTransfer = null;
                    lock (_transferLock)    //Acquire the transfer lock
                    {
                        var readyTransfers = Transfers.Where(x => (x.Status != Interfaces.TransferStatus.Blocked) || (x.CanCopy));
                        //If there are no transfers to do, then return
                        if (readyTransfers.Any() == false)
                            return;

                        nextTransfer = readyTransfers.FirstOrDefault();
                    }

                    if (nextTransfer != null && nextTransfer.CanCopy == false)
                    {
                        nextTransfer.Status = Interfaces.TransferStatus.Blocked;
                        nextTransfer = null;
                    }

                    if (nextTransfer != null)
                        TransferFiles(nextTransfer);
                }
            }
        }

        //TODO Look into more efficient methods https://designingefficientsoftware.wordpress.com/2011/03/03/efficient-file-io-from-csharp/
        private void TransferFiles(TransferBase transfer, long bufferSize = 65535)
        {
            string file = null;
            Stream sourceStream = null;
            Stream destStream = null;
            Action<string, Stream, Stream> finishMethod = null;
            Action<long> updateMethod = null;
            long totalBytesRead = 0;
            int readLength = 0;
            byte[] ActiveBuffer = new byte[bufferSize];
            byte[] BackBuffer = new byte[bufferSize];
            bool readStarted = false;
            //var transfer = _activeTransfer;
            IAsyncResult readState = null;
            do
            {
                if (!readStarted) transfer.GetNextFile(out file, out sourceStream, out destStream, out finishMethod, out updateMethod, out totalBytesRead);
                if (file != null)
                {
                    var thisFile = file;
                    var thisSourceStream = sourceStream;
                    var thisDestStream = destStream;
                    var thisFinishMethod = finishMethod;
                    var thisUpdateMethod = updateMethod;

                    transfer.Status = Interfaces.TransferStatus.TransferingFiles;
                    long fileLength = 0;
                    if (sourceStream.CanSeek) fileLength = sourceStream.Length;
                    bool endOfFile = false;
                    if (!readStarted)
                    {
                        //start reading asynchronously
                        readState = sourceStream.BeginRead(ActiveBuffer, 0, ActiveBuffer.Length, null, null);
                        readStarted = true;
                    }
                    do
                    {
                        readLength = sourceStream.EndRead(readState);
                        readStarted = false;
                        totalBytesRead += readLength;

                        //If we haven't reached the end of the file...
                        if (readLength != 0)
                        {
                            var thisTotalBytesRead = totalBytesRead;

                            bool pauseStream = false;
                            bool abortStream = false;
                            //Kick off another read asynchronously while we write. 
                            try
                            {
                                if (fileLength > 0 && sourceStream.Position >= fileLength)
                                {
                                    endOfFile = true;
                                }
                                else if (ShouldAbortTransfer(transfer))
                                {
                                    abortStream = true;
                                }
                                //If the streams can be paused, and the manager is being paused or this transfer isn't first anymore, then pause the streams and return.
                                else if (transfer.CanPauseMidStream() && (_IsPaused || (transfer != GetFirstTransfer(preferCurrentTransfer:!transfer.CanCopy))))    //If this transfer became locked, mark it preferred because we should probably close the streams before pausing
                                {
                                    //Flag the stream for pausing
                                    pauseStream = true;
                                }
                                else
                                {
                                    readState = sourceStream.BeginRead(BackBuffer, 0, BackBuffer.Length, null, null);
                                    readStarted = true;
                                }
                            }
                            catch (IOException)
                            {
                                endOfFile = true;
                            }
                            if (endOfFile && !abortStream && !pauseStream)
                            {
                                transfer.GetNextFile(out file, out sourceStream, out destStream, out finishMethod, out updateMethod, out totalBytesRead);
                                if (file != null)
                                {
                                    readState = sourceStream.BeginRead(BackBuffer, 0, BackBuffer.Length, null, null);
                                    readStarted = true;
                                }
                                else
                                {
                                    file = thisFile;
                                    sourceStream = thisSourceStream;
                                    destStream = thisDestStream;
                                }
                            }
                            //Write the active buffer to the destination, update progress, and do a buffer swap. 
                            thisDestStream.Write(ActiveBuffer, 0, readLength);
                            thisUpdateMethod(thisTotalBytesRead);
                            BackBuffer = Interlocked.Exchange(ref ActiveBuffer, BackBuffer);
                            if (endOfFile == false)
                            {
                                //If there is an abort request, dispose of our active streams and bail out. The fact that we dispose here and nowhere else is a little dirty...
                                if (abortStream)
                                {
                                    sourceStream.Dispose();
                                    destStream.Dispose();
                                    return;
                                }
                                else if (pauseStream)
                                {
                                    transfer.PauseStreaming(thisFile, sourceStream, destStream, thisTotalBytesRead);
                                    return;
                                }
                            }
                        }
                    }
                    while ((readLength != 0) && (endOfFile == false));    //Break out once there is no more to read

                    if (thisFinishMethod != null)
                        thisFinishMethod(thisFile, thisSourceStream, thisDestStream); //Callback to the finish method,
                }

                if (_IsPaused || (transfer != GetFirstTransfer()))
                {
                    transfer.Pause();
                    return;
                }

            } while (file != null); //If file is null, we've reached the end
            Steam.SteamBase.UiDispatcher.Invoke((Action)(() =>
            {
                lock (_transferLock)    //Acquire the transfer lock
                {
                    Transfers.Remove(transfer);
                }
            }));
            Steam.SteamBase.UiDispatcher.BeginInvoke((Action)(() => transfer.RunPostProcessing()));

        }

        private TransferBase GetFirstTransfer(bool preferCurrentTransfer = false)
        {
            lock (_transferLock)    //Acquire the transfer lock
            {
                if (preferCurrentTransfer)
                    return Transfers.Where(x => (x.Status != Interfaces.TransferStatus.Blocked)).FirstOrDefault();
                else
                    return Transfers.Where(x => (x.Status != Interfaces.TransferStatus.Blocked) || (x.CanCopy)).FirstOrDefault();
            }
        }
        private bool ShouldAbortTransfer(TransferBase transfer)
        {
            lock (_transferLock)    //Acquire the transfer lock
            {
                return !Transfers.Contains(transfer);
            }
        }
    }
}

// Look into more efficient methods https://designingefficientsoftware.wordpress.com/2011/03/03/efficient-file-io-from-csharp/
//Total time writing< 1MB with File.WriteAllLines                           = 00:00:00.0050003
//Total time writing< 1MB with File.TestWriteAllText                        = 00:00:00.0040002
//Total time writing< 1MB with File.WriteAllBytes                           = 00:00:00.3560204 
//Total time writing< 1MB with BinaryWriter.Write                           = 00:00:00.0010001
//Total time writing< 1MB with StreamWriter1.Write                          = 00:00:00.0030002
//Total time writing< 1MB with FileStream1.Write no parsing                 = 00:00:00.0010001
//Total time writing< 1MB with FileStream2.Write no parsing                 = 00:00:00.0070004
//Total time writing< 1MB with FileStream3.Write no parsing                 = 00:00:00.0100006
//Total time writing< 1MB with WFIO1.Write No Parsing                       = 00:00:00.0020001
//Total time writing< 1MB with WFIO2.WriteBlocks No Parsing                 = 00:00:00.0010001

//Total time writing 10MB with File.WriteAllLines                           = 00:00:00.0350020
//Total time writing 10MB with File.TestWriteAllText                        = 00:00:00.0270016
//Total time writing 10MB with File.WriteAllBytes                           = 00:00:00.3390194
//Total time writing 10MB with BinaryWriter.Write                           = 00:00:00.0050003
//Total time writing 10MB with StreamWriter1.Write                          = 00:00:00.0230013
//Total time writing 10MB with FileStream1.Write no parsing                 = 00:00:00.0050003
//Total time writing 10MB with FileStream2.Write no parsing                 = 00:00:00.1060061
//Total time writing 10MB with FileStream3.Write no parsing                 = 00:00:00.1150066
//Total time writing 10MB with WFIO1.Write No Parsing                       = 00:00:00.0050003
//Total time writing 10MB with WFIO2.WriteBlocks No Parsing                 = 00:00:00.0060003

//Total time writing 50MB with File.WriteAllLines                           = 00:00:00.1620093
//Total time writing 50MB with File.TestWriteAllText                        = 00:00:00.1440082
//Total time writing 50MB with File.WriteAllBytes                           = 00:00:00.3530202
//Total time writing 50MB with BinaryWriter.Write                           = 00:00:00.3040174
//Total time writing 50MB with StreamWriter1.Write                          = 00:00:00.1140065
//Total time writing 50MB with FileStream1.Write no parsing                 = 00:00:00.3670210
//Total time writing 50MB with FileStream2.Write no parsing                 = 00:00:00.5000286
//Total time writing 50MB with FileStream3.Write no parsing                 = 00:00:00.5840334
//Total time writing 50MB with WFIO1.Write No Parsing                       = 00:00:00.3530202
//Total time writing 50MB with WFIO2.WriteBlocks No Parsing                 = 00:00:00.0260015





//Total time reading< 1MB with File.ReadAllLines                            = 00:00:00.0030002
//Total time reading< 1MB with File.ReadAllText                             = 00:00:00.0040002
//Total time reading< 1MB with File.ReadAllBytes                            = 00:00:00
//Total time reading< 1MB with BinaryReader.Read                            = 00:00:00.0020001
//Total time reading< 1MB with StreamReader1.Read                           = 00:00:00.0010001
//Total time reading< 1MB with StreamReader2.Read(large buf)                = 00:00:00.0010001
//Total time reading< 1MB with StreamReader3.ReadBlock                      = 00:00:00.0010001
//Total time reading< 1MB with StreamReader4.ReadToEnd                      = 00:00:00.0020001
//Total time reading< 1MB with mult StreamReader5.Read                      = 00:00:00.0020001
//Total time reading< 1MB with StreamReader6.ReadLine                       = 00:00:00.0020002
//Total time reading< 1MB with FileStream1.Read no parsing                  = 00:00:00.0080005
//Total time reading< 1MB with FileStream3.Read(Rand) no parsing            = 00:00:00
//Total time reading< 1MB with FileStream4.BeginRead no parsing             = 0:00:00.0020001
//Total time reading< 1MB with FileStream7.BeginRead                        = 00:00:00
//Total time reading< 1MB with WFIO1.Read No Parsing                        = 00:00:00.0020001
//Total time reading< 1MB with WFIO2.ReadUntilEOF No Parsing                = 00:00:00.0010001
//Total time reading< 1MB with WFIO3.ReadBlocks API No Parsing              = 00:00:00.0010001
//Total time reading< 1MB with BinaryReader.Read                            = 00:00:00.0010001
//Total time reading< 1MB with StreamReader2.Read(large buf)                = 00:00:00.0010001
//Total time reading< 1MB with FileStream1.Read no parsing                  = 00:00:00.0010000
//Total time reading< 1MB with WFIO.Read No Open/Close                      = 00:00:00.0010001

//Total time reading 10MB with File.ReadAllLines                            = 00:00:00.0640037
//Total time reading 10MB with File.ReadAllText                             = 00:00:00.0360020
//Total time reading 10MB with File.ReadAllBytes                            = 00:00:00.0050003
//Total time reading 10MB with BinaryReader.Read                            = 00:00:00.0270016
//Total time reading 10MB with StreamReader1.Read                           = 00:00:00.0200011
//Total time reading 10MB with StreamReader2.Read(large buf)                = 00:00:00.0160009
//Total time reading 10MB with StreamReader3.ReadBlock                      = 00:00:00.0150008
//Total time reading 10MB with StreamReader4.ReadToEnd                      = 00:00:00.0320018
//Total time reading 10MB with mult StreamReader5.Read                      = 00:00:00.0430025
//Total time reading 10MB with StreamReader6.ReadLine                       = 00:00:00.0310017
//Total time reading 10MB with FileStream1.Read no parsing                  = 00:00:00.0040002
//Total time reading 10MB with FileStream3.Read(Rand) no parsing            = 00:00:00.0030002
//Total time reading 10MB with FileStream4.BeginRead no parsing             = 00:00:00.0040002
//Total time reading 10MB with FileStream7.BeginRead                        = 00:00:00.0050003
//Total time reading 10MB with WFIO1.Read No Parsing                        = 00:00:00.0020001
//Total time reading 10MB with WFIO2.ReadUntilEOF No Parsing                = 00:00:00.0030001
//Total time reading 10MB with WFIO3.ReadBlocks API No Parsing              = 00:00:00.0030002
//Total time reading 10MB with BinaryReader.Read                            = 00:00:00.0220012
//Total time reading 10MB with StreamReader2.Read(large buf)                = 00:00:00.0150008
//Total time reading 10MB with FileStream1.Read no parsing                  = 00:00:00.0030002
//Total time reading 10MB with WFIO.Read No Open/Close                      = 00:00:00.0030001

//Total time reading 50MB with File.ReadAllLines                            = 00:00:00.3540202
//Total time reading 50MB with File.ReadAllText                             = 00:00:00.1630093
//Total time reading 50MB with File.ReadAllBytes                            = 00:00:00.0260015
//Total time reading 50MB with BinaryReader.Read                            = 00:00:00.1260072
//Total time reading 50MB with StreamReader1.Read                           = 00:00:00.0960055
//Total time reading 50MB with StreamReader2.Read(large buf)                = 00:00:00.0750043
//Total time reading 50MB with StreamReader3.ReadBlock                      = 00:00:00.0750043
//Total time reading 50MB with StreamReader4.ReadToEnd                      = 00:00:00.1720099
//Total time reading 50MB with mult StreamReader5.Read                      = 00:00:00.0850048
//Total time reading 50MB with StreamReader6.ReadLine                       = 00:00:00.1510087
//Total time reading 50MB with FileStream1.Read no parsing                  = 00:00:00.0190011
//Total time reading 50MB with FileStream3.Read(Rand) no parsing            = 00:00:00.0170009
//Total time reading 50MB with FileStream4.BeginRead no parsing             = 00:00:00.0180011
//Total time reading 50MB with FileStream7.BeginRead                        = 00:00:00.0240014
//Total time reading 50MB with WFIO1.Read No Parsing                        = 00:00:00.0120007
//Total time reading 50MB with WFIO2.ReadUntilEOF No Parsing                = 00:00:00.0140008
//Total time reading 50MB with WFIO3.ReadBlocks API No Parsing              = 00:00:00.0130008
//Total time reading 50MB with BinaryReader.Read                            = 00:00:00.1080062
//Total time reading 50MB with StreamReader2.Read(large buf)                = 00:00:00.0690040
//Total time reading 50MB with FileStream1.Read no parsing                  = 00:00:00.0130008
//Total time reading 50MB with WFIO.Read No Open/Close                      = 00:00:00.0130008