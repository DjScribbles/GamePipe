/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamePipeLib.Interfaces;
using System.IO;
using GamePipeLib.Model.Steam;

namespace GamePipeLib.Model
{
    public abstract class TransferBase : GamePipeLib.Model.NotifyPropertyChangedBase, IGameTransfer
    {
        //const int MAX_BUFFER_SIZE = 16777216; //Max buffer of 16MB, this didn't really improve anything, so I'll stop at 8MB
        const int MAX_BUFFER_SIZE = 8388608; //Max buffer of 8MB
        const int FALLBACK_BUFFER_SIZE = 16384;

        protected IAppProvider _source;
        protected ITransferTarget _target;
        private bool _preProcessComplete = false;
        private bool _postProcessComplete = false;
        private long _actualDiskSize;
        private long _validatedBytesTransferred = 0;
        private long _lastReportedBytesTransferred = 0;


        private Queue<Tuple<string, long>> _workQueue;
        private Tuple<string, long> _currentWorkItem = null;
        private BackupDisposalProcedure _desiredBackupBehavior = BackupDisposalProcedure.BackupThenOpen;
        private DateTime _startTime = DateTime.MinValue;
        private TimeSpan _timeSoFar = TimeSpan.Zero;
        private long _fileCount = 0;
        private Stream _pausedSourceStream;
        private Stream _pausedTargetStream;
        private string _pausedFile;
        private long _pausedBytesRead;
        /// <summary>
        /// This only tracks if the transfer is paused mid-file, all other state control is handled by the TransferManager.
        /// </summary>
        private bool _isPausedMidStream = false;
        private List<string> _retriedList = new List<string>();
        private int _lastRetriedListLength = 0;

        protected TransferBase(IAppProvider source, ITransferTarget target, ISteamApplication app)
        {
            _source = source;
            _target = target;
            _Application = app;
            _Status = TransferStatus.Queued;
        }
        public long ActualDiskSize { get { return _actualDiskSize; } }
        public long BytesTransfered { get { return _lastReportedBytesTransferred; } }
        public bool ForceTransfer { get; set; }
        public bool CanCopy
        {
            get
            {
                return (ForceTransfer
                        ? _source.CanCopyIfForced(Application.AppId)
                        : _source.CanCopy(Application.AppId));
            }
        }

        public abstract bool GetIsValidated();

        private bool _lastCanCopyCheckResult = false;
        private DateTime _nextCanCopyCheck = DateTime.MinValue;
        public bool GetCanCopyCached()
        {
            var now = DateTime.UtcNow;
            if (now > _nextCanCopyCheck)
            {
                _nextCanCopyCheck = now.AddSeconds(1);
                _lastCanCopyCheckResult = CanCopy;
            }
            return _lastCanCopyCheckResult;
        }

        protected abstract void DoPostProcess();
        protected abstract void DoAbortProcess();
        protected abstract void DoPreProcess();
        protected abstract bool ValidateFile(string file, Stream source, Stream target);
        public abstract bool CanPauseMidStream();

        public void QueueTransfer()
        {
            _actualDiskSize = _source.GetMeasuredGameSize(Application.AppId);
            if (_target.GetFreeSpace() < _actualDiskSize)
            {
                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(1000);    //TODO this is a little lazy, replace this if I get a proper scheduler. 
                    string message = string.Format("The destination only has {0} free, {1} is {2}.\nThe transfer cannot be completed.",
                                                                Utils.FileUtils.GetReadableFileSize(_target.GetFreeSpace()),
                                                                Utils.FileUtils.GetReadableFileSize(_actualDiskSize),
                                                                Application.GameName);
                    Utils.Logging.Logger.Error(message);
                    System.Windows.MessageBox.Show(message, "Insufficient Disk Space", System.Windows.MessageBoxButton.OK,
                                                System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                });
            }
            //else if (_target.HasApp(Application.AppId))
            //{

            //}
            else if (_target.HasGameDir(Application.InstallDir) && _actualDiskSize > 0)
            {
                Task.Run(() =>
                {
                    //Don't show the dialog immediately after the mouse button gets released, that's just asking for trouble.
                    System.Threading.Thread.Sleep(1000);    //TODO this is a little lazy, replace this if I get a proper scheduler. 
                    var result = System.Windows.MessageBox.Show(string.Format("A directory already exists at the transfer destination {0}.\nHow would you like to handle it?\n\nYes = Backup the directory, then delete it upon successful copy.\nNo = Backup the directory, then open it upon a successful copy, and I will decide what to do.\nCancel = Just abort the transfer.\n\n", Application.InstallDir),
                                                                "Directory Already Exists", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.Cancel);
                    switch (result)
                    {
                        case System.Windows.MessageBoxResult.Yes:
                            _desiredBackupBehavior = BackupDisposalProcedure.BackupThenDelete;
                            TransferManager.Instance.AddTransfer(this);
                            break;
                        case System.Windows.MessageBoxResult.No:
                            _desiredBackupBehavior = BackupDisposalProcedure.BackupThenOpen;
                            TransferManager.Instance.AddTransfer(this);
                            break;
                        default:
                        case System.Windows.MessageBoxResult.Cancel:
                            (_target as Steam.SteamLibrary)?.OpenGameDir(Application.InstallDir, true, true);
                            break;
                    }
                });
            }
            else
            {
                TransferManager.Instance.AddTransfer(this);
            }
        }

        #region "IGameTransfer Implementation"
        private readonly ISteamApplication _Application;
        public ISteamApplication Application { get { return _Application; } }

        abstract public string TransferType { get; }

        private volatile TransferStatus _Status;
        public TransferStatus Status
        {
            get { return _Status; }
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    Steam.SteamBase.UiDispatcher.BeginInvoke((Action)(() =>
                    {
                        NotifyPropertyChanged("Status");
                        NotifyPropertyChanged("StatusString");
                    }));
                }
            }
        }

        private volatile bool _progressUpdateInvoked = false;
        private double _Progress;
        public double Progress
        {
            get { return _Progress; }
            private set
            {
                if (_Progress != value)
                {
                    double old = System.Threading.Interlocked.Exchange(ref _Progress, value);
                    if ((old != value) && (!_progressUpdateInvoked))
                    {
                        _progressUpdateInvoked = true;
                        Steam.SteamBase.UiDispatcher.BeginInvoke((Action)(() =>
                        {
                            _progressUpdateInvoked = false;
                            NotifyPropertyChanged("Progress");
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
            }
        }
        private long _TransferRateBytesPerSecond;
        public long TransferRateBytesPerSecond
        {
            get { return _TransferRateBytesPerSecond; }
            private set
            {
                if (_TransferRateBytesPerSecond != value)
                {
                    double old = System.Threading.Interlocked.Exchange(ref _TransferRateBytesPerSecond, value);
                    if ((old != value))
                    {
                        Steam.SteamBase.UiDispatcher.BeginInvoke((Action)(() => NotifyPropertyChanged("TransferRateBytesPerSecond")));
                    }
                }
            }
        }

        public long TotalTransferred
        {
            get { return _lastReportedBytesTransferred; }
        }
        //string SourceName { get { return _source.Name; } }
        //string DestinationName { get { return _target.Name; } }
        #endregion //IGameTransfer Implementation


        public void RunPreProcessing()
        {
            if (_preProcessComplete == false)
            {
                Status = Interfaces.TransferStatus.Preparing;
                _startTime = DateTime.UtcNow;

                Utils.Logging.Logger.InfoFormat("Beginning {1} of {0}.", Application.GameName, this.TransferType.ToLower());
                string appId = Application.AppId;
                (Application as SteamApp)?.MeasureDiskSize();
                //Create the directory structure
                try
                {
                    var directories = _source.GetDirectoriesForApp(appId);
                    bool acceptCompressedFiles = ((_target is SteamArchive) && ((SteamArchive)_target).CompressNewGames);
                    _workQueue = new Queue<Tuple<string, long>>(_source.GetFilesForApp(appId, acceptCompressedFiles));

                    if (_workQueue.Any())
                    {
                        _target.BackupExistingDir(Application.InstallDir);
                    }
                    _target.CreateDirectories(directories);
                }
                catch (System.IO.DirectoryNotFoundException)
                {
                    _workQueue = new Queue<Tuple<string, long>>();
                }    //If the directory isn't present then allow the acf to move

                _fileCount = _workQueue.Count;
                DoPreProcess();
                _preProcessComplete = true;
            }
        }
        public void RunPostProcessing()
        {
            if (_postProcessComplete == false)
            {
                if (Status == TransferStatus.Aborting)
                {
                    Utils.Logging.Logger.InfoFormat("Aborted {0} of {1}.",
                                                    this.TransferType.ToLower(),
                                                    Application.GameName);
                    _target.RestoreBackupToDir(Application.InstallDir);
                    DoAbortProcess();
                    Status = TransferStatus.Aborted;
                }
                else
                {
                    _target.DisposeOfBackup(Application.InstallDir, _desiredBackupBehavior);
                    DoPostProcess();
                    if (_startTime != DateTime.MinValue)
                    {
                        _timeSoFar = _timeSoFar.Add(DateTime.UtcNow.Subtract(_startTime));
                        _startTime = DateTime.MinValue;
                    }

                    long bytesPerSecond = Convert.ToInt64(Convert.ToDouble(_actualDiskSize) / _timeSoFar.TotalSeconds);
                    Utils.Logging.Logger.InfoFormat("Completed {5} of {0}. Transferring {1} files, {2} total, in {3} (averaging {4}/sec).",
                                                    Application.GameName,
                                                    _fileCount,
                                                    Utils.FileUtils.GetReadableFileSize(_actualDiskSize),
                                                    _timeSoFar.ToString(),
                                                    Utils.FileUtils.GetReadableFileSize(bytesPerSecond),
                                                    this.TransferType.ToLower());
                    Status = TransferStatus.Finished;
                }
                _postProcessComplete = true;
            }
        }

        public void RequestAbort()
        {
            TransferManager.Instance.AbortTransfer(this);
            Abort();
        }

        private void Abort()
        {
            _pausedSourceStream?.Dispose();
            _pausedTargetStream?.Dispose();

            Status = TransferStatus.Aborting;
            RunPostProcessing();
        }

        private void ValidateAndCloseFile(string file, Stream source, Stream target)
        {

            bool result = false;
            try
            {
                result = ValidateFile(file, source, target);
            }
            finally
            {
                source.Dispose();
                target.Dispose();
            }

            if (result == false && _currentWorkItem != null)
            {
                _workQueue.Enqueue(_currentWorkItem);
                IterrimUpdateMethod(0);
            }
            else
            {
                //Latch the copied bytes into the validated bytes
                _retriedList.RemoveAll(x => file == x);
                _validatedBytesTransferred = _lastReportedBytesTransferred;
            }
        }


        public void PauseStreaming(string file, Stream source, Stream target, long totalRead)
        {
            if (CanPauseMidStream() == false) throw new NotSupportedException(string.Format("Type {0} does not support pausing mid-file.", this.GetType().Name));
            _pausedFile = file;
            _pausedSourceStream = source;
            _pausedTargetStream = target;
            _pausedBytesRead = totalRead;
            _isPausedMidStream = true;
            Pause();
        }
        public void Pause()
        {
            if (_startTime != DateTime.MinValue)
            {
                _timeSoFar = _timeSoFar.Add(DateTime.UtcNow.Subtract(_startTime));
                _startTime = DateTime.MinValue;
            }
        }

        public void GetNextFile(out string file, out long fileSize, out Stream source, out Stream target, out Action<string, Stream, Stream> finishMethod, out Action<long> updateMethod, out long totalBytesRead)
        {
            file = null;
            fileSize = 0;
            source = null;
            target = null;
            totalBytesRead = 0;
            finishMethod = ValidateAndCloseFile;
            updateMethod = IterrimUpdateMethod;

            if (_startTime == DateTime.MinValue)
                _startTime = DateTime.UtcNow;

            if (_preProcessComplete == false)
            {
                try
                {
                    RunPreProcessing();
                }
                catch (Exception ex)
                {
                    Utils.Logging.Logger.Error("Error while preparing the destination directory.\nFile transfer aborted.", ex);
                    Status = TransferStatus.Aborting;
                    Task.Run(() =>
                    {
                        System.Windows.MessageBox.Show(string.Format("Error while preparing the destination directory.\nFile transfer aborted.", ex.Message), "Transfer failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                    });
                    return;
                }
            }

            if (_isPausedMidStream)
            {
                file = _pausedFile;
                source = _pausedSourceStream;
                target = _pausedTargetStream;
                totalBytesRead = _pausedBytesRead;
                _isPausedMidStream = false;
            }
            else if (_workQueue.Any())
            {
                while (source == null || target == null)
                {
                    file = null;
                    fileSize = 0;
                    Stream srcStrm = null;
                    Stream dstStrm = null;
                    string appId = Application.AppId;
                    if (_workQueue.Any() == false) return;
                    _currentWorkItem = _workQueue.Dequeue();
                    file = _currentWorkItem.Item1;
                    fileSize = _currentWorkItem.Item2;
                    var theFile = file;
                    try
                    {
                        bool acceptCompressedFiles = ((_target is SteamArchive) && ((SteamArchive)_target).CompressNewGames);
                        int bufferSize = Convert.ToInt32(Math.Min(fileSize, MAX_BUFFER_SIZE));
                        if (bufferSize <= 0)
                            bufferSize = FALLBACK_BUFFER_SIZE;

                        srcStrm = _source.GetFileStream(appId, file, acceptCompressedFiles, GetIsValidated(), bufferSize);
                        dstStrm = _target.GetFileStream(Application.InstallDir, file, GetIsValidated(), bufferSize);

                        source = srcStrm;
                        target = dstStrm;
                    }
                    catch (Exception ex)
                    {
                        Utils.Logging.Logger.Debug(string.Format("Error openning file streams for {0}.", file), ex);
                        var retry = RetryFile(_currentWorkItem);

                        file = null;
                        fileSize = 0;
                        srcStrm?.Dispose();
                        dstStrm?.Dispose();
                        source = null;
                        target = null;

                        //If the retry failed, then give up
                        if (!retry)
                            return;
                    }
                }
            }
        }

        public bool RetryFile(string filePath, long fileSize)
        {
            return RetryFile(new Tuple<string, long>(filePath, fileSize));
        }

        public bool RetryFile(Tuple<string, long> entry)
        {
            _workQueue.Enqueue(entry);
            var theFile = entry.Item1;

            //try again later
            if (_retriedList.Contains(theFile) == false)
            {
                _retriedList.Add(theFile);
            }
            else if (_retriedList.First() == theFile)
            {
                //If we get to the first file, and the list size is the same time as the last time we got here, clear the work queue, and give up
                if (_lastRetriedListLength == _retriedList.Count)
                {


                    if (Progress < 0.9f)
                    {
                        Utils.Logging.Logger.Error("Two times through the retry list and no progress, less than 90% complete, so just aborting.");
                        Status = TransferStatus.Aborting;
                        Task.Run(() =>
                        {
                            System.Windows.MessageBox.Show(string.Format("{0} files failed to transfer for {1}.\nFile transfer aborted.", _lastRetriedListLength, Application.GameName), "Transfer failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                        });
                    }
                    else
                    {
                        Utils.Logging.Logger.Error(string.Format("Two times through the retry list and no progress, giving up on the transfer, but almost done, so finishing normally with a warning.", theFile));
                        Utils.Logging.Logger.Warn($"The game files for {Application.GameName} shoule be validated, some files failed to transfer");
                        Task.Run(() =>
                        {
                            System.Windows.MessageBox.Show(string.Format("{0} files failed to transfer for {1}.\nYou will need to validate the game after restarting Steam.", _lastRetriedListLength, Application.GameName), "Transfer Incomplete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                        });
                    }

                    _workQueue.Clear();
                    return false;
                }

                _lastRetriedListLength = _retriedList.Count;
            }
            return true;
        }

        private DateTime _lastTransferUpdateTime = DateTime.MinValue;
        private DateTime _nextTransferUpdateTime = DateTime.MinValue;
        private long _lastTransferUpdateSize = 0;
        private void IterrimUpdateMethod(long bytesCopied)
        {
            var time = DateTime.UtcNow;
            _lastReportedBytesTransferred = _validatedBytesTransferred + bytesCopied;
            Progress = Convert.ToDouble(_lastReportedBytesTransferred) / Convert.ToDouble(_actualDiskSize);

            if (time > _nextTransferUpdateTime)
            {
                var duration = time.Subtract(_lastTransferUpdateTime).TotalSeconds;
                var bytesTransferred = Convert.ToDouble(_lastReportedBytesTransferred - _lastTransferUpdateSize);
                var transferRate = Convert.ToInt64(bytesTransferred / duration);
                TransferRateBytesPerSecond = transferRate;

                _lastTransferUpdateTime = time;
                _nextTransferUpdateTime = time.AddSeconds(1);
                _lastTransferUpdateSize = _lastReportedBytesTransferred;

            }
        }

    }
}
