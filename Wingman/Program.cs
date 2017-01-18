using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using WingmanLib;
using System.ServiceModel;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.IO;

namespace Wingman
{
    class Program
    {
        const int SYSTEM_IDLE_SECONDS = 300;
        const int TICKS_PER_SECOND = 1000;
        const int IDLE_TICKS = SYSTEM_IDLE_SECONDS * TICKS_PER_SECOND;

        //https://msdn.microsoft.com/en-us/library/bb546102%28v=vs.110%29.aspx
        public static bool IsSteamRunning()
        {
            try
            {
                var value = (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam\ActiveProcess", "pid", null);
                return (value != 0);
            }
            catch
            {
                return Process.GetProcessesByName("SteamService").Any();
            }
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

        public static void CloseSteam()
        {
            var steampath = System.IO.Path.Combine(SteamDirectory, "steam.exe");
            System.Diagnostics.Process.Start(steampath, "-shutdown");
        }

        public static void OpenSteam()
        {
            var steampath = System.IO.Path.Combine(SteamDirectory, "steam.exe");
            System.Diagnostics.Process.Start(steampath);
        }

        #region "Idle Checking"

        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime; //Time is in msec
        }

        [DllImport("User32.dll")]
        private static extern bool
        GetLastInputInfo(ref LASTINPUTINFO plii);
        public static uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            return ((uint)Environment.TickCount - lastInPut.dwTime);
        }
        #endregion

        public static bool IsSystemIdle()
        {
            return GetIdleTime() > IDLE_TICKS;
        }



        public static bool IsAnySteamAppRunning()
        {
            //This call is pretty expensive, only run every 10-30 seconds or so, and only when all other conditions are met
            using (var steamAppsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\Apps"))
            {
                var appKeys = steamAppsKey.GetSubKeyNames();
                foreach (var keyName in appKeys)
                {

                    using (var appKey = steamAppsKey.OpenSubKey(keyName))
                    {
                        try
                        {
                            var value = appKey.GetValue("Running");

                            if (value != null && ((int)value) != 0)
                            {
                                return true;
                            }
                        }
                        catch
                        { }
                    }
                }
            }
            return false;
        }


        private static bool _lastAnyAppRunning = true;
        private static DateTime _nextAppRunningCheck = DateTime.MinValue;
        public static bool Cached_IsAnySteamAppRunning()
        {
            var now = DateTime.UtcNow;
            if (now > _nextAppRunningCheck)
            {
                _nextAppRunningCheck = now.AddSeconds(15);
                _lastAnyAppRunning = IsAnySteamAppRunning();
            }
            return _lastAnyAppRunning;
        }

        static void Main(string[] args)
        {
            var minimumEndTime = DateTime.UtcNow.AddSeconds(3);
            using (ServiceHost host = new ServiceHost(typeof(WingmanService)))
            {
                host.Open();

                Console.WriteLine("Service up and running at:");
                foreach (var ea in host.Description.Endpoints)
                {
                    Console.WriteLine(ea.Address);
                }

                while (true)
                {
                    if (WingmanService.HasItems())
                    {
                        if (!IsSteamRunning())
                        {
                            WingmanService.ProcessHitList();
                            if (WingmanService.GetRestartRequested())
                            {
                                Thread.Sleep(3000);
                                OpenSteam();
                                WingmanService.ClearRestartRequest();
                            }
                        }
                        else
                        {
                            //Monitor system utilization, try to restart steam when nobody is looking... o.O
                            if (IsSystemIdle() && !Cached_IsAnySteamAppRunning())
                            {
                                CloseSteam();
                                var timeout = DateTime.UtcNow.AddMinutes(3);
                                while (IsSteamRunning() && timeout > DateTime.UtcNow)
                                {
                                    Thread.Sleep(100);
                                }
                                if (!IsSteamRunning())
                                {
                                    WingmanService.ProcessHitList();
                                    Thread.Sleep(3000);
                                    OpenSteam();
                                }
                            }
                        }
                    }
#if !STEAM
                    else if (DateTime.UtcNow > minimumEndTime && !IsGamePipeRunning())
                    {
                        return;
                    }
#endif
                    Thread.Sleep(500);
                }
            }
        }


        private static string _SteamDirectory;
        public static string SteamDirectory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_SteamDirectory))
                {
                    try
                    {
                        if (Environment.Is64BitOperatingSystem)
                        {
                            _SteamDirectory = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam\", "InstallPath", @"C:\Program Files (x86)\Steam")?.ToString();
                        }
                        else
                        {
                            _SteamDirectory = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam\", "InstallPath", @"C:\Program Files (x86)\Steam")?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    //If the registry keys failed, try Steams usual location
                    if (string.IsNullOrWhiteSpace(_SteamDirectory) || Directory.Exists(_SteamDirectory) == false)
                        _SteamDirectory = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Steam");

                }
                return _SteamDirectory;
            }
            set { _SteamDirectory = value; }
        }

    }
}
