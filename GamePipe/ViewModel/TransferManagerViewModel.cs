/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using GamePipeLib.Model;
using System;
using System.Timers;
using System.Windows.Shell;

namespace GamePipe.ViewModel
{
    public class TransferManagerViewModel : ViewModelBase
    {
        private Timer _updateTimer = new Timer(500) { AutoReset = true };
        public TransferManagerViewModel()
        {
            _updateTimer.Elapsed += _updateTimer_Elapsed;
            _updateTimer.Enabled = true;
        }


        private void _updateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateProgress();
        }

        public TransferManager Manager { get { return TransferManager.Instance; } }

        #region "Commands"
        #region "PauseCommand"
        private RelayCommand _PauseCommand = null;
        public RelayCommand PauseCommand
        {
            get
            {
                if (_PauseCommand == null)
                {
                    _PauseCommand = new RelayCommand(x => Pause(), x => !Manager.IsPaused);

                }
                return _PauseCommand;
            }
        }

        public void Pause()
        {
            Manager.IsPaused = true;
        }


        #endregion //PauseCommand
        #region "ResumeCommand"
        private RelayCommand _ResumeCommand = null;
        public RelayCommand ResumeCommand
        {
            get
            {
                if (_ResumeCommand == null)
                {
                    _ResumeCommand = new RelayCommand(x => Resume(), x => Manager.IsPaused);
                }
                return _ResumeCommand;
            }
        }

        public void Resume()
        {
            Manager.IsPaused = false;
        }


        #endregion //ResumeCommand
        #endregion //Commands

        private double _OverallProgress;
        public double OverallProgress
        {
            get { return _OverallProgress; }
            private set
            {
                if (_OverallProgress != value)
                {
                    _OverallProgress = value;
                    GamePipeLib.Model.Steam.SteamBase.UiDispatcher.BeginInvoke((Action)(() => NotifyPropertyChanged("OverallProgress")));
                }
            }
        }
        private TaskbarItemProgressState _State = TaskbarItemProgressState.None;
        public TaskbarItemProgressState State
        {
            get { return _State; }
            set
            {
                if (_State != value)
                {
                    var prevValue = _State;
                    _State = value;
                    GamePipeLib.Model.Steam.SteamBase.UiDispatcher.BeginInvoke((Action)(() =>
                    {
                        NotifyPropertyChanged("State");
                        if (prevValue == TaskbarItemProgressState.Normal && _State == TaskbarItemProgressState.None)
                        {
                            OnTransfersComplete();
                        }
                    }
                    ));
                }
            }
        }

        private void OnTransfersComplete()
        {
            System.Media.SystemSounds.Hand.Play();

            //Originally planned on using windows toasting, but after looking deeper it seems that that may cause compatability issues on win7, as it involves bringing in winrt libs.
            //ToastContent content = new ToastContent()
            //{
            //    Launch = "na",

            //    Visual = new ToastVisual()
            //    {
            //        TitleText = new ToastText()
            //        {
            //            Text = "Game Pipe"
            //        },

            //        BodyTextLine1 = new ToastText()
            //        {
            //            Text = "All transfers completed"
            //        },

            //        AppLogoOverride = new ToastAppLogo()
            //        {
            //            Source = new ToastImageSource("pack://application:,,,/Resources/GamePipe.ico")
            //        }
            //    },


            //    Audio = new ToastAudio()
            //    {
            //        Src = new Uri("ms-winsoundevent:Notification.Default")
            //    }
            //};

            //string doc = content.GetContent();


            //// Generate WinRT notification
            //new ToastNotification(doc);
        }

        private void UpdateProgress()
        {
            OverallProgress = Manager.GetOverallProgress();
            if (Manager.Transfers.Count > 0)
            {
                if (Manager.IsPaused)
                {
                    State = TaskbarItemProgressState.Paused;
                }
                else
                {
                    State = TaskbarItemProgressState.Normal;
                }
            }
            else
            {
                State = TaskbarItemProgressState.None;
            }
        }
    }
}
