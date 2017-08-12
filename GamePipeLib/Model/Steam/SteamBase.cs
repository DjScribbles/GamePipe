/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

namespace GamePipeLib.Model.Steam
{
    public static class SteamBase
    {
        private static System.Windows.Threading.Dispatcher _UiDispatcher;
        public static System.Windows.Threading.Dispatcher UiDispatcher
        {
            get
            {
                if (_UiDispatcher == null)
                {
                    _UiDispatcher = System.Windows.Application.Current?.Dispatcher;
                }
                return _UiDispatcher;
            }
        }

    }
}
