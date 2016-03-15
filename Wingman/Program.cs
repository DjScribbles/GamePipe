using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using WingmanLib;
using System.ServiceModel;
using System.Diagnostics;

namespace Wingman
{
    class Program
    {
        //https://msdn.microsoft.com/en-us/library/bb546102%28v=vs.110%29.aspx
        //private static PipeStream _pipeClient;
        //private static DateTime _restartSteamFlagTime = DateTime.MinValue;
        public static bool IsSteamRunning()
        {
            return Process.GetProcessesByName("SteamService").Any();
        }

        public static bool IsGamePipeRunning()
        {
            var isRunning = Process.GetProcessesByName("GamePipe").Any();
#if DEBUG
            if (isRunning == false)
            {
                isRunning = Process.GetProcessesByName("GamePipe.vshost").Any();
            }
#endif
            return isRunning;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            var minimumEndTime = DateTime.UtcNow.AddSeconds(30);
            using (ServiceHost host = new ServiceHost(typeof(WingmanService)))
            {
                host.Open();

                Console.WriteLine("Service up and running at:");
                foreach (var ea in host.Description.Endpoints)
                {
                    Console.WriteLine(ea.Address);
                }

                while (IsGamePipeRunning() || WingmanService.HasItems() || DateTime.UtcNow < minimumEndTime)
                {
                    if (!IsSteamRunning() && WingmanService.HasItems())
                    {
                        WingmanService.ProcessHitList();
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }
            }

            if (WingmanService.GetRestartRequested())
                Process.Start(@"steam://open/games");
        }

    }
}
