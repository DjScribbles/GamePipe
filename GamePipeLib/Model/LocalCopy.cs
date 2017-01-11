/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using GamePipeLib.Interfaces;
using System.IO;
using System;

namespace GamePipeLib.Model
{
    public class LocalCopy : TransferBase
    {
        public LocalCopy(IAppProvider source, ITransferTarget target, ISteamApplication app) : base(source, target, app) { }
        public override string TransferType { get { return "Local Copy"; } }
        public override bool CanPauseMidStream() { return true; }

        public override bool GetIsValidated()
        {
            return false;
        }

        protected override void DoAbortProcess()
        {
        }

        protected override void DoPostProcess()
        {
            string appId = Application.AppId;
            if (appId.Contains(","))
            {
                var ids = appId.Split(new char[1] { ',' });
                foreach (string id in ids)
                {
                    _target.WriteAcfFile(id, _source.GetAcfFileContent(id));
                }
            }
            else
            {
                _target.WriteAcfFile(appId, _source.GetAcfFileContent(appId));
            }
        }

        protected override void DoPreProcess()
        {
            //Nothing to do
        }

        protected override bool ValidateFile(string file, Stream source, Stream target)
        {
            if (target is Utils.CrcStream && source is Utils.CrcStream)
            {
                var sourceCrc = ((Utils.CrcStream)source).ReadCrc;
                var targetCrc = ((Utils.CrcStream)target).WriteCrc;
                return (targetCrc == sourceCrc);
            }
            else
                return true; //If the target or source isn't a crc stream then just return true
        }
    }
}
