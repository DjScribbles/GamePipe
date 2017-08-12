/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/


using GamePipeLib.Interfaces;

namespace GamePipeLib.Model
{
    public class LocalMove : LocalCopy
    {
        public LocalMove(IAppProvider source, ITransferTarget target, ISteamApplication app) : base(source, target, app) { }
        public override string TransferType { get { return "Local Move"; } }

        protected override void DoPostProcess()
        {
            base.DoPostProcess();
            (_source as Steam.SteamLibrary)?.DeleteGameContent(Application.AppId);
        }
    }

}
