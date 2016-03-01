/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamePipeLib.Model.Steam;
using GamePipeLib.Interfaces;
using GamePipe.Properties;
using System.ServiceModel;

namespace GamePipe.ViewModel
{
    public class FriendViewModel : ViewModelBase
    {
        public readonly IAppProvider _provider = null;
        //private Service.GameGetterClient _client;

        public FriendViewModel(string ip, ushort port)
        {
            Port = port;
            Ip = ip;
            Uri baseUri = new Uri(string.Format("net.tcp://{0}:{1}/gamepipe", ip, port));
            //Uri fileSenderUri = new Uri(string.Format("net.tcp://{0}:{1}/gamepipe/file", ip.ToString(), port));
            //Uri infoProviderUri = new Uri(string.Format("net.tcp://{0}:{1}/gamepipe/apps", ip.ToString(), port));
            var myBinding = new NetTcpBinding("StreamedBinding");
            var myEndpoint = new EndpointAddress(baseUri);
            var myChannelFactory = new ChannelFactory<IAppProvider>(myBinding, myEndpoint);

            try
            {
                _provider = myChannelFactory.CreateChannel();
            }
            catch
            {
                if (_provider != null)
                {
                    ((ICommunicationObject)_provider).Abort();
                }
                throw new ArgumentException(string.Format("Unable to connect to {0}:{1}, ensure the address is correct and that they are hosting in GamePipe.", ip.ToString(), port));
            }
        }

        public string Ip { get; private set; }
        public ushort Port { get; private set; }
        public string FriendName { get { return string.Format("{0}:{1}", Ip, Port); } }

        public IEnumerable<RemoteSteamApp> FilteredGames
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_listFilter))
                {
                    return AvailableApplications;

                }
                else
                {
                    return AvailableApplications?.Where(x => x.GameName.ToLower().Contains(_listFilter.ToLower())).ToArray();
                }
            }
        }
        public bool Remembered
        {
            get { return (Settings.Default.Friends != null) && Settings.Default.Friends.Contains(FriendName); }
            set
            {
                if (Remembered != value)
                {
                    if (Settings.Default.Friends == null) Settings.Default.Friends = new System.Collections.Specialized.StringCollection();
                    if (value)
                    {
                        if (!Settings.Default.Friends.Contains(FriendName))
                            Settings.Default.Friends.Add(FriendName);
                    }
                    else
                    {
                        Settings.Default.Friends.Remove(FriendName);
                    }
                    Settings.Default.Save();
                    NotifyPropertyChanged("Remembered");
                }
            }
        }
        private string _listFilter = "";

        private bool _filterUpdateQueued = false;
        public void UpdateFilter(string filter)
        {
            _listFilter = filter;
            if (_filterUpdateQueued == false && SteamBase.UiDispatcher != null)
            {
                _filterUpdateQueued = true;
                SteamBase.UiDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                {
                    _filterUpdateQueued = false;
                    NotifyPropertyChanged("FilteredGames");
                }));
            }
        }

        private IEnumerable<RemoteSteamApp> _AvailableApplications;
        public IEnumerable<RemoteSteamApp> AvailableApplications
        {
            get
            {
                return _AvailableApplications;
            }
            set
            {
                SteamBase.UiDispatcher.Invoke(() =>
                {
                    _AvailableApplications = value;
                    NotifyPropertyChanged("AvailableApplications");
                    NotifyPropertyChanged("FilteredGames");
                });
            }
        }
        #region "RefreshCommand"
        private RelayCommand _RefreshCommand = null;
        public RelayCommand RefreshCommand
        {
            get
            {
                if (_RefreshCommand == null)
                {
                    _RefreshCommand = new RelayCommand(x => Refresh());

                }
                return _RefreshCommand;
            }
        }

        public void Refresh()
        {
            //if (_client._provider.CanCopy("31419"))
            //    Console.WriteLine("Success");
            //AvailableApplications = _client._provider.GetAvailableIds().Select(x => new RemoteSteamApp(x, _client._provider));

            Task.Run(() =>
            {

                try
                {

                    var available = _provider.GetAvailableIds();
                    var results = available.Select(x => new RemoteSteamApp(x, _provider));
                    AvailableApplications = results;
                }
                catch (Exception ex)
                {
                    GamePipeLib.Utils.Logging.Logger.Error(string.Format("Refresh exception on {0}", FriendName), ex);
                    System.Windows.MessageBox.Show("Refresh exception:\n" + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);

                }

            });
        }

        #endregion //RefreshCommand

        //RefreshAppsCommand
        //CopyTo(app,drive) via drag and drop

    }
    public class RemoteSteamApp : ISteamApplication
    {

        private ISteamApplication _appInfo;
        private IAppProvider _provider;
        public RemoteSteamApp(ISteamApplication receivedData, IAppProvider provider)
        {
            _appInfo = receivedData;
            _provider = provider;
        }



        public string InstallDir { get { return _appInfo.InstallDir; } }
        public string AppId
        {
            get
            {
                return _appInfo.AppId;
            }
        }

        public long DiskSize
        {
            get
            {
                return _appInfo.DiskSize;
            }
        }

        public string GameName
        {
            get
            {
                return _appInfo.GameName;
            }
        }
        public string ImageUrl
        {
            get
            {
                return _appInfo.GetSteamImageUrl();
            }
        }
        public bool CanCopy()
        {
            return _provider.CanCopy(AppId);
        }

        public string ReadableDiskSize { get { return string.Format("~{0}", GamePipeLib.Utils.FileUtils.GetReadableFileSize(DiskSize)); } }
    }
}
