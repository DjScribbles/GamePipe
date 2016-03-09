/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System.ServiceModel;

namespace GamePipeService
{
    [ServiceContract]
    public interface IDiscoverFriends
    {
        [OperationContract]
        string RequestFriendship(string publicKey);
        [OperationContract]
        string StartSession(string publicKey);
        [OperationContract]
        string EndSession(string publicKey);
    }
}
