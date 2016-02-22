/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamePipeLib.Interfaces;
using System.IO;

namespace GamePipeLib.Model
{
    public class LocalCopy : TransferBase
    {
        public LocalCopy(IAppProvider source, ITransferTarget target, ISteamApplication app) : base(source, target, app) { }
        public override string TransferType { get { return "Local Copy"; } }
        public override bool CanPauseMidStream() { return true; }

        protected override void DoAbortProcess()
        {
        }

        protected override void DoPostProcess()
        {
            string appId = Application.AppId;
            _target.WriteAcfFile(appId, _source.GetAcfFileContent(appId));
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
