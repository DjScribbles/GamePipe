/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;

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
            var id = app.AppId;
            if (id.Contains(","))
                id = id.Split(new char[1] { ',' })[0];

            return string.Format("https://steamcdn-a.akamaihd.net/steam/apps/{0}/capsule_{1}.jpg?{2}", id, "sm_120", DateTime.Now.ToString("yyyy-MM-dd"));
        }
    }
}
