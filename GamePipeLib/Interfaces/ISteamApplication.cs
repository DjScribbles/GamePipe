/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace GamePipeLib.Interfaces
{
    public interface ISteamApplication
    {
        string GameName { get; }
        string AppId { get; }
        //string GameDir { get; }
        //string AcfFile { get; }
        long DiskSize { get; }
        string ReadableDiskSize { get; }
        string ImageUrl { get; }
        string InstallDir { get; }
        //bool CanCopy { get; }
        //bool CanCopy();
    }

    public static class ISteamApplicationExtension
    {
        public static string GetSteamImageUrl(this ISteamApplication app)
        {
            return string.Format("https://steamcdn-a.akamaihd.net/steam/apps/{0}/capsule_{1}.jpg?{2}", app.AppId, "sm_120", DateTime.Now.ToString("yyyy-MM-dd"));
        }
    }
}
