/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

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
