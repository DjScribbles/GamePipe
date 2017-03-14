/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Windows;

using System.Net.NetworkInformation;

namespace GamePipe
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Version _version;
        public App() : base()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.Exit += App_Exit;
            this.Startup += App_Startup;
            _version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            GamePipeLib.Utils.Logging.Logger.InfoFormat("-----------------------GamePipe started {0}-----------", _version.ToString());

        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            CheckForUpdates(_version);
            var a = new Action(() => GamePipeLib.Utils.WingmanServer.KickoffWingmanProcess());
            a.BeginInvoke(a.EndInvoke, null);
            //GamePipeLib.Model.Steam.Cleanup.CleanupRoot.StartupScan();
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




        public async void CheckForUpdates(Version currentVersion)
        {
            try
            {
                var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("GamePipe"));
                var releases = client.Repository.Release.GetAll("DjScribbles", "GamePipe");
                Version latestVersion = null;
                Octokit.Release latestRelease = null;
                foreach (var item in await releases)
                {
                    try
                    {
                        var thisVersion = new Version(item.TagName);
                        if (thisVersion.CompareTo(latestVersion) > 0)
                        {
                            latestVersion = thisVersion;
                            latestRelease = item;
                        }
                    }
                    catch
                    {
                    }
                }

                if ((currentVersion.CompareTo(latestVersion) < 0) && (latestRelease != null))
                {
                    var mw = (MainWindow as MainWindow);
                    if (mw != null)
                    {
                        mw.UpdateVm.IsNewVersionAvailable = true;
                        mw.UpdateVm.NewVersionUrl = latestRelease.HtmlUrl;
                    }
                    //var result = MessageBox.Show("A new version of Game Pipe is available, do you want to take a look?", "New Version Available", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    //if (result == MessageBoxResult.Yes)
                    //{
                    //    System.Diagnostics.Process.Start(latestRelease.HtmlUrl);
                    //}
                }
            }
            catch (Exception ex)
            {
                GamePipeLib.Utils.Logging.Logger.Error("Failed to check for updates due to exception:", ex);
            }
        }
    }
}
