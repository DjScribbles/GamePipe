using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePipeLib.Interfaces
{
    public interface ILocalSteamApplication : ISteamApplication
    {
        string GameDir { get; }
        bool SizeIsMeasured { get; }
        void MeasureDiskSize();
        void DeleteGameData();
        bool CanCopy();
        void RefreshFromAcf();
    }
}
