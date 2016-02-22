/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamePipeLib.Model;

namespace GamePipe.ViewModel
{
    public class TransferManagerViewModel : ViewModelBase
    {
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
    }
}
