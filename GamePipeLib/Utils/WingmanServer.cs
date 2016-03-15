using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Diagnostics;
using System.IO;
using GamePipeLib.WingmanService;

namespace GamePipeLib.Utils
{


    public static class WingmanServer
    {
        private const string CLIENT_NAME = "Wingman.exe";
        private const string CLIENT_PROCESS = "Wingman";

        public static void SendSignal_AddAcfFileToHitList(string filePath)
        {
            GetService().AddAcfFileToHitList(filePath);
        }

        public static void SendSignal_RestartSteamWhenDone(bool restartRequested)
        {
            GetService().SetRestartSteamOnExit(restartRequested);
        }

        public static void SendSignal_RemoveAcfFileFromHitList(string filePath)
        {
            GetService().RemoveAcfFileFromHitList(filePath);
        }

        private static IWingmanService _wingmanService;
        private static IWingmanService GetService()
        {
            if (!WingmanIsRunning())
            {
                //var wingmanPath = Path.GetFullPath(CLIENT_NAME);
                //var tempFile = Environment.ExpandEnvironmentVariables("%TEMP%\\LaunchWingman.bat");
                //var command = $"start start start start start start start start start start start start {wingmanPath}";
                //File.WriteAllText(tempFile, command);
                //Process.Start(tempFile);
                //using (Process wingmanProcess = new Process())
                //{
                //    wingmanProcess.StartInfo.FileName = CLIENT_NAME;
                //    wingmanProcess.StartInfo.UseShellExecute = true;
                //    wingmanProcess.StartInfo.CreateNoWindow = true;                   
                //    wingmanProcess.Start();
                //}
                Process.Start(CLIENT_NAME);
                _wingmanService = null;
                while (!WingmanIsRunning())
                    System.Threading.Thread.Sleep(10);
            }
            if (_wingmanService == null)
            {
                _wingmanService = new WingmanServiceClient();
            }
            return _wingmanService;
        }

        private static bool WingmanIsRunning()
        {
            return System.Diagnostics.Process.GetProcessesByName(CLIENT_PROCESS).Any();
        }
    }
}
