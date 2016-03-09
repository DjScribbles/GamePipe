using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamePipeLib.Model.Steam;

namespace GamePipe.ViewModel
{
    public class SteamArchiveViewModel : SteamLibraryViewModel
    {
        private SteamArchive _model;
        public SteamArchiveViewModel(SteamArchive model) : base(model)
        {
            _model = model;
        }

        public bool CompressNewGames
        {
            get
            {
                return _model.CompressNewGames;
            }
            set
            {
                _model.CompressNewGames = value;
                NotifyPropertyChanged("CompressNewGames");
            }
        }
        public bool CopyInOut
        {
            get
            {
                return _model.CopyInOut;
            }
            set
            {
                _model.CopyInOut = value;
                NotifyPropertyChanged("CopyInOut");
            }
        }
    }
}
