using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Diagnostics;
using System.IO;
using GamePipeLib.WingmanService;
using GamePipeLib.Model;

namespace GamePipeLib.Utils
{


    public static class WingmanServer
    {
        private const string CLIENT_NAME = "GamePipe_Helper.exe";
        private const string CLIENT_PROCESS = "GamePipe_Helper";

        public static void SendSignal_AddAcfFileToHitList(string filePath)
        {
            try
            {
                GetService().AddAcfFileToHitList(filePath);
            }
            catch (Exception)
            {
                filePath = Path.GetFullPath(filePath).ToLower();
                TransferManager.Instance.AcfFileWatchList.Add(filePath);
            }
        }

        public static void SendSignal_RestartSteamWhenDone(bool restartRequested)
        {
            try
            {
                GetService().SetRestartSteamOnExit(restartRequested);
            }
            catch (Exception) { }
        }

        public static void SendSignal_RemoveAcfFileFromHitList(string filePath)
        {
            try
            {
                GetService().RemoveAcfFileFromHitList(filePath);
                filePath = Path.GetFullPath(filePath).ToLower();
                TransferManager.Instance.AcfFileWatchList.Remove(filePath);
            }
            catch (Exception)
            {
                filePath = Path.GetFullPath(filePath).ToLower();
                TransferManager.Instance.AcfFileWatchList.Remove(filePath);
            }
        }

        private static IWingmanService _wingmanService;
        private static IWingmanService GetService()
        {
            if (!WingmanIsRunning())
            {
                //TODO just warn the user that they could see duplicat e
                try
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
                    System.Threading.Thread.Sleep(50);
                    if (!WingmanIsRunning())
                        System.Threading.Thread.Sleep(200);

                }
                catch (Exception ex)
                {
                    Utils.Logging.Logger.Error($"Exception in spawning the {CLIENT_PROCESS} process:", ex);
                }
            }

            if (_wingmanService == null)
            {
                try
                {
                    _wingmanService = new WingmanServiceClient();
                }
                catch (Exception ex)
                {
                    Utils.Logging.Logger.Error($"Exception in connecting to the {CLIENT_PROCESS} client:", ex);
                }
            }
            return _wingmanService;
        }

        private static bool WingmanIsRunning()
        {
            return System.Diagnostics.Process.GetProcessesByName(CLIENT_PROCESS).Any();
        }
    }
}
