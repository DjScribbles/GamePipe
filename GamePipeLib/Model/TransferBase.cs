/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamePipeLib.Interfaces;
using System.IO;
using GamePipeLib.Model.Steam;

namespace GamePipeLib.Model
{
    public abstract class TransferBase : GamePipeLib.Model.NotifyPropertyChangedBase, IGameTransfer
    {

        protected IAppProvider _source;
        protected ITransferTarget _target;
        private bool _preProcessComplete = false;
        private bool _postProcessComplete = false;
        private long _actualDiskSize;
        private Queue<string> _workQueue;
        private BackupDisposalProcedure _desiredBackupBehavior = BackupDisposalProcedure.BackupThenOpen;
        private DateTime _startTime = DateTime.MinValue;
        private TimeSpan _timeSoFar = TimeSpan.Zero;
        private long _fileCount = 0;
        protected TransferBase(IAppProvider source, ITransferTarget target, ISteamApplication app)
        {
            _source = source;
            _target = target;
            _Application = app;
            _Status = TransferStatus.Queued;
        }

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
            else if (_target.HasGameDir(Application.InstallDir))
            {
                Task.Run(() =>
                {
                    //Don't show the dialog immediately after the mouse button gets released, that's just asking for trouble.
                    System.Threading.Thread.Sleep(1000);    //TODO this is a little lazy, replace this if I get a proper scheduler. 
                    var result = System.Windows.MessageBox.Show(string.Format("A directory already exists at the transfer destination {0}.\nHow would you like to handle it?\n\nYes = Backup the directory, then delete it upon successful copy.\nNo = Backup the directory, then open it upon a successful copy, and I will decide what to do.\nCancel = Just abort the transfer.\n\n", Application.InstallDir),
                                                                "Transfer failed", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.Cancel);
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

        //string SourceName { get { return _source.Name; } }
        //string DestinationName { get { return _target.Name; } }
        #endregion //IGameTransfer Implementation


        public void RunPreProcessing()
        {
            if (_preProcessComplete == false)
            {
                _startTime = DateTime.UtcNow;

                Utils.Logging.Logger.InfoFormat("Beginning {1} of {0}.", Application.GameName, this.TransferType.ToLower());
                string appId = Application.AppId;
                (Application as SteamApp)?.MeasureDiskSize();
                //Create the directory structure
                _target.BackupExistingDir(Application.InstallDir);
                _target.CreateDirectories(_source.GetDirectoriesForApp(appId));
                bool acceptCompressedFiles = ((_target is SteamArchive) && ((SteamArchive)_target).CompressNewGames);
                _workQueue = new Queue<string>(_source.GetFilesForApp(appId, acceptCompressedFiles));
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
        protected abstract void DoPostProcess();
        protected abstract void DoAbortProcess();
        protected abstract void DoPreProcess();
        protected abstract bool ValidateFile(string file, Stream source, Stream target);

        private long _validatedBytesTransferred = 0;
        private long _lastReportedBytesTransferred = 0;
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

            if (result == false)
            {
                _workQueue.Enqueue(file);
                IterrimUpdateMethod(0);
            }
            else
            {
                //Latch the copied bytes into the validated bytes
                _validatedBytesTransferred = _lastReportedBytesTransferred;
            }
        }

        public abstract bool CanPauseMidStream();


        private Stream _pausedSourceStream;
        private Stream _pausedTargetStream;
        private string _pausedFile;
        private long _pausedBytesRead;
        /// <summary>
        /// This only tracks if the transfer is paused mid-file, all other state control is handled by the TransferManager.
        /// </summary>
        private bool _isPausedMidStream = false;

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
        public void GetNextFile(out string file, out Stream source, out Stream target, out Action<string, Stream, Stream> finishMethod, out Action<long> updateMethod, out long totalBytesRead)
        {
            file = null;
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
                    Stream srcStrm = null;
                    Stream dstStrm = null;
                    string appId = Application.AppId;
                    file = _workQueue.Dequeue();
                    try
                    {
                        bool acceptCompressedFiles = ((_target is SteamArchive) && ((SteamArchive)_target).CompressNewGames);
                        srcStrm = _source.GetFileStream(appId, file, acceptCompressedFiles);

                        dstStrm = _target.GetFileStream(Application.InstallDir, file);
                        source = srcStrm;
                        target = dstStrm;
                    }
                    catch (IOException ex)
                    {
                        Utils.Logging.Logger.Error(string.Format("Error open file streams for {0}, retry queue, moving on to another file.", file), ex);
                        //try again later
                        _workQueue.Enqueue(file);
                        file = null;
                        srcStrm?.Dispose();
                        dstStrm?.Dispose();
                        source = null;
                        target = null;
                    }
                }
            }
        }

        private void IterrimUpdateMethod(long bytesCopied)
        {
            _lastReportedBytesTransferred = _validatedBytesTransferred + bytesCopied;
            Progress = Convert.ToDouble(_lastReportedBytesTransferred) / Convert.ToDouble(_actualDiskSize);
        }
    }
}
