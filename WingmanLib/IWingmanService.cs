using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WingmanLib
{
    [ServiceContract]
    public interface IWingmanService
    {
        [OperationContract]
        void AddAcfFileToHitList(string filePath);

        [OperationContract]
        void RemoveAcfFileFromHitList(string filePath);

        [OperationContract]
        void SetRestartSteamOnExit(bool restartRequested);

        [OperationContract]
        bool HitListHasItems();
    }
}
