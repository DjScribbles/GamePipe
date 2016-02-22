/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace GamePipeLib.Model.Steam
{
    public static class SteamBase
    {
        private static System.Windows.Threading.Dispatcher _UiDispatcher;
        public static System.Windows.Threading.Dispatcher UiDispatcher
        {
            get { return _UiDispatcher; }
            set { _UiDispatcher = value; }
        }

    }
}
