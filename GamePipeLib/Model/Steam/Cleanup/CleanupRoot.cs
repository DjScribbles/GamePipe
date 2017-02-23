using GamePipeLib.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePipeLib.Model.Steam.Cleanup
{
    public class CleanupRoot
    {

        public static void StartupScan()
        {
            var libs = Steam.SteamRoot.Instance.Libraries;

            var duplicates = from lib in libs
                             from manifest in lib.Games
                             group manifest by manifest.AppId into manifestGroup
                             where manifestGroup.Count() > 1
                             select manifestGroup;

            bool duplicateAppsPresent = false;
            List<ILocalSteamApplication> effectedGames = new List<ILocalSteamApplication>();

            if (duplicates.Any())
            {
                List<ILocalSteamApplication> emptyAppRemovals = new List<ILocalSteamApplication>();
                foreach (var dupe in duplicates)
                {
                    bool gameAffected = false;
                    foreach (var manifest in dupe)
                    {
                        int nonEmptyApps = 0;

                        //Add all apps that have a missing or empty game directory to a list for quick removal
                        if (!Directory.Exists(manifest.GameDir))
                        {
                            emptyAppRemovals.Add(manifest);
                            if (gameAffected == false)
                            {
                                gameAffected = true;
                                effectedGames.Add(manifest);
                            }
                        }
                        else
                        {
                            manifest.MeasureDiskSize();
                            if (manifest.DiskSize == 0)
                            {
                                emptyAppRemovals.Add(manifest);
                                if (gameAffected == false)
                                {
                                    gameAffected = true;
                                    effectedGames.Add(manifest);
                                }
                            }
                            else
                            {
                                nonEmptyApps++;
                                if (nonEmptyApps > 1)
                                {
                                    duplicateAppsPresent = true;
                                }
                            }
                        }
                    }
                }

                if (emptyAppRemovals.Any())
                {

                    StringBuilder message = new StringBuilder();
                    if (effectedGames.Count > 1)
                    {
                        message.Append($"Game Pipe has detected {effectedGames.Count} duplicated appManifest files with no associated game data, which may prevent the games from running.");
                        if (effectedGames.Count > 5)
                        {
                            message.AppendLine("The games effected by this include:");
                            foreach (var game in effectedGames.Take(4))
                            {
                                message.AppendLine(game.GameName);
                            }
                            message.AppendLine($"And {effectedGames.Count - 4} more...");
                        }
                        else
                        {
                            message.AppendLine("The games effected by this are:");
                            foreach (var game in effectedGames)
                            {
                                message.AppendLine(game.GameName);
                            }
                        }
                        message.AppendLine("\nDo you want to remove the empty duplicates? (No game data will be removed)");
                    }
                    else
                    {
                        message.Append($"Game Pipe has detected that {effectedGames[0].GameName} has a duplicate appManifest files with no associated game data, which may prevent it from running.");
                        message.Append("\n\nDo you want to repair this problem? (No game data will be removed)");
                    }

                    var result = System.Windows.MessageBox.Show(message.ToString(), "Repair Steam Manifests", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.Yes);
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        foreach (var empty in emptyAppRemovals)
                        {
                            empty.DeleteManifest();
                        }
                    }
                }
            }


            if (duplicateAppsPresent)
            {
                System.Windows.MessageBox.Show("Game Pipe has detected multiple copies of some games in different libraries, you should visit the cleanup tab to resolve any issues this could cause.");
            }
        }




        /*
         * 
         * 
         * 
         * 
         * 
         * Dead Shortcut - Check the desktop for shortcuts to games that are no longer installed anywhere
         * 
         * Astranged ACF - An acf file in one library while the game dir is found in another - Action - Move ACF         
         * Empty ACF w/ Backup - ACF File related to a GPBackup directory, but no game dir. - Restore, Delete All.
         * GP Backup - A folder begins with gpBackup_{name} and an associated game dir is found - Action - Merge, Delete
         * Orphan Backup - Folder begins gpBackup_{name} and no associated {name} or acf file in any location - Action - Delete, Restore
         * Dangling Folder - A folder with no assciated ACF in any library - Action - Delete, Install                       
         * Empyt Games - ACF File with no associated game data
         * Duplicate Games (one or more games, with one or more manifests) Ask which to delete (show size difference between biggest and the rest), relocate acf if needed
         */




        public class CleanupAction
        {
            string ActionName;
            ILocalSteamApplication TargetApp;
            string TargetDirectory;
            Action<ILocalSteamApplication, string> CleanupTask;
        }
    }
}
