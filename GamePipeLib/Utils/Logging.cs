/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]
namespace GamePipeLib.Utils
{
    public static class Logging
    {
        private static ILog _Logger;
        public static ILog Logger
        {
            get
            {
                if (_Logger == null)
                    _Logger = LogManager.GetLogger("RollingFileAppender");

                return _Logger;
            }
        }
    }
}
