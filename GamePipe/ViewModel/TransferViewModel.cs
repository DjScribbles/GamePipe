/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Linq;
using GamePipeLib.Model;
using GamePipeLib.Interfaces;

namespace GamePipe.ViewModel
{
    class TransferViewModel : ViewModelBase
    {
        private readonly TransferBase _model;
        public TransferBase Model { get { return _model; } }
        public TransferViewModel(TransferBase model)
        {
            if (model == null) throw new ArgumentNullException("model");
            _model = model;
            _model.PropertyChanged += _model_PropertyChanged;
        }

        private void _model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Progress")
            {
                NotifyPropertyChanged("Progress");
                NotifyPropertyChanged("TotalTransferred");
                NotifyPropertyChanged("ProgressText");
            }

            if (e.PropertyName == "TransferRateBytesPerSecond")
                NotifyPropertyChanged("TransferRate");

            if (e.PropertyName == "Status")
            {
                NotifyPropertyChanged("Status");
                NotifyPropertyChanged("IsBlocked");
            }
        }

        public TransferStatus Status { get { return _model.Status; } }
        public bool IsBlocked { get { return _model.Status == TransferStatus.Blocked; } }
        public bool IsForced { get { return _model.ForceTransfer; } }
        public bool IsNotForced { get { return !_model.ForceTransfer; } }//TODO This is really lazy...
        public double Progress { get { return _model.Progress; } }
        public string TotalTransferred { get { return GamePipeLib.Utils.FileUtils.GetReadableFileSizeFixed(_model.TotalTransferred); } }
        public string ActualSize { get { return GamePipeLib.Utils.FileUtils.GetReadableFileSize(_model.ActualDiskSize); } }
        public string ProgressText { get { return $"{TotalTransferred} / {ActualSize}"; } }
        public string TransferType { get { return _model.TransferType; } }
        public string ImageUrl { get { return _model.Application?.ImageUrl; } }
        public string GameName { get { return _model.Application?.GameName; } }
        public string TransferRate { get { return $"({GamePipeLib.Utils.FileUtils.GetReadableFileSizeFixed(_model.TransferRateBytesPerSecond)}/s)"; } }

        private TransferManager Manager { get { return TransferManager.Instance; } }


        #region "Commands"
        #region "AbortCommand"
        private RelayCommand _AbortCommand = null;
        public RelayCommand AbortCommand
        {
            get
            {
                if (_AbortCommand == null)
                {
                    _AbortCommand = new RelayCommand(x => Abort());

                }
                return _AbortCommand;
            }
        }

        public void Abort()
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to cancel this transfer?", "Cancel Transfer?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No);
            if (result == System.Windows.MessageBoxResult.Yes)
                Model.RequestAbort();
        }


        #endregion //AbortCommand
        #region "MoveUpCommand"
        private RelayCommand _MoveUpCommand = null;
        public RelayCommand MoveUpCommand
        {
            get
            {
                if (_MoveUpCommand == null)
                {
                    _MoveUpCommand = new RelayCommand(x => MoveUp());

                }
                return _MoveUpCommand;
            }
        }

        public void MoveUp()
        {
            var transfers = Manager.Transfers.ToList();
            var index = transfers.FindIndex(t => t == Model);
            if (index > 0)
            {
                transfers.RemoveAt(index);
                transfers.Insert(index - 1, Model);
            }
            Manager.ShuffleTransfers(transfers);
        }


        #endregion //MoveUpCommand
        #region "MoveDownCommand"
        private RelayCommand _MoveDownCommand = null;
        public RelayCommand MoveDownCommand
        {
            get
            {
                if (_MoveDownCommand == null)
                {
                    _MoveDownCommand = new RelayCommand(x => MoveDown());

                }
                return _MoveDownCommand;
            }
        }

        public void MoveDown()
        {
            var transfers = Manager.Transfers.ToList();
            var index = transfers.FindIndex(t => t == Model);
            if (index < transfers.Count - 1)
            {
                transfers.RemoveAt(index);
                transfers.Insert(index + 1, Model);
            }
            Manager.ShuffleTransfers(transfers);
        }
        #endregion //MoveDownCommand
        #region "ForceTransferCommand"
        private RelayCommand _ForceTransferCommand = null;
        public RelayCommand ForceTransferCommand
        {
            get
            {
                if (_ForceTransferCommand == null)
                {
                    _ForceTransferCommand = new RelayCommand(x => ForceTransfer());

                }
                return _ForceTransferCommand;
            }
        }

        public void ForceTransfer()
        {
            Model.ForceTransfer = true;
            NotifyPropertyChanged("IsNotForced");
            NotifyPropertyChanged("IsForced");
        }
        #endregion //ForceTransferCommand
        #endregion //Commands
    }
}
