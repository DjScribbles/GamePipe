using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.IO;
using System.Threading;

namespace WingmanLib
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class WingmanService : IWingmanService
    {
        private static object _hitListLock = new object();
        private static List<string> _hitList = new List<string>();
        private static bool _restartRequested = false;
        private static int _itemCount = 0;
        public static bool HasItems()
        {
            lock (_hitListLock)
            {
                return _hitList.Any();
            }
        }

        public static bool GetRestartRequested()
        {
            return _restartRequested;
        }

        public void SetRestartSteamOnExit(bool restartRequested)
        {
            _restartRequested = restartRequested;
        }
        public static void ClearRestartRequest()
        {
            _restartRequested = false;
        }


        public bool HitListHasItems()
        {
            return _itemCount > 0;
        }
        public static void ProcessHitList()
        {
            Console.WriteLine("Processing hit list...");
            string[] tempList;
            lock (_hitListLock)
            {
                tempList = _hitList.ToArray();
            }

            foreach (var file in tempList)
            {
                KillAcfFile(file);
            }
        }

        public void AddAcfFileToHitList(string filePath)
        {
            Console.WriteLine($"Adding file to hit list: {filePath}");

            bool added = false;
            if (ValidateFilePath(filePath))
            {
                filePath = Path.GetFullPath(filePath).ToLower();
                lock (_hitListLock)
                {
                    if (!_hitList.Contains(filePath))
                    {
                        _hitList.Add(filePath);
                        added = true;
                    }
                    _itemCount = _hitList.Count;
                }
            }
            if (added)
                Console.WriteLine($"Added file to hit list: {filePath}");
            else
                Console.WriteLine($"Failed to add file to hit list: {filePath}");

        }

        public void RemoveAcfFileFromHitList(string filePath)
        {
            Console.WriteLine($"Removing file from hit list: {filePath}");
            bool removed = false;
            filePath = Path.GetFullPath(filePath).ToLower();
            lock (_hitListLock)
            {
                removed = _hitList.Remove(filePath);
                _itemCount = _hitList.Count;
            }
            if (removed)
                Console.WriteLine($"Removed file from hit list: {filePath}");
        }

        private static void KillAcfFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                lock (_hitListLock)
                {
                    if (!_hitList.Contains(filePath))
                        return;

                    _hitList.Remove(filePath);
                    _itemCount = _hitList.Count;
                }
                File.Delete(filePath);
                Console.WriteLine($"Deleted file: {filePath}");
            }
            else
            {
                lock (_hitListLock)
                {
                    _hitList.Remove(filePath);
                    _itemCount = _hitList.Count;
                }
                Console.WriteLine($"File not found, removing from list: {filePath}");
            }
        }

        private static bool ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;
            //TODO check file not exists, directory does exist, and steam is running before adding to list
            // Or just make sure we find Steam.dll in the right relative location, and delete file if steam isn't running only have to be careful of deleting a game that was just moved back to it's original location
            var fileName = Path.GetFileName(filePath);

            if (!fileName.StartsWith("appmanifest_", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!fileName.EndsWith(".acf", StringComparison.OrdinalIgnoreCase))
                return false;

            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                return false;

            var steamDllPath = Path.Combine(directory, "..\\steam.dll");
            if (!File.Exists(steamDllPath))
                return false;


            return true;
        }


        //private static StreamWriter _Console;
        //public static StreamWriter Console
        //{
        //    get
        //    {
        //        if (_Console == null)
        //        {
        //            _Console = new StreamWriter(Path.GetFullPath(@"E:\Users\Joe\Documents\GamePipe\GamePipe\bin\Debug\logs\wingman.txt"), true);
        //        }
        //        return _Console;
        //    }
        //}
    }
}
