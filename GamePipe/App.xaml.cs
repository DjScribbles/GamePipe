/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using log4net.Config;

namespace GamePipe
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public App() : base()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.Exit += App_Exit;
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            GamePipeLib.Utils.Logging.Logger.InfoFormat("-----------------------GamePipe started {0}-----------", version.ToString());
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            GamePipeLib.Utils.Logging.Logger.Info("----------------------GamePipe shut down--------------------------");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GamePipeLib.Utils.Logging.Logger.Error("An unhandled exception occurred:", e.ExceptionObject as Exception);
        }


        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            GamePipeLib.Utils.Logging.Logger.Error("An unhandled dispatcher exception occurred:", e.Exception);
        }
    }
}
