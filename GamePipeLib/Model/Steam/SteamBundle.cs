/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GamePipeLib.Interfaces;

namespace GamePipeLib.Model.Steam
{
    /// <summary>
    /// This class is designed to handle games which have multiple appIds which lead to the same install directory
    /// A few examples would be Arma 2 (33910 & 33900), Torchlight (41500 & 41520), and Medal Of Honor (47790 & 47830)
    /// </summary>
    //TODO update the contents of the bundle when items are removed? Not a common situation at all, and probably harmless?
    public class SteamBundle : NotifyPropertyChangedBase, ILocalSteamApplication
    {
        private List<string> _removedAppIds = new List<string>();

        public SteamBundle(IEnumerable<ILocalSteamApplication> apps)
        {
            var includedBundles = apps.OfType<SteamBundle>();
            var nonBundles = apps.Except(includedBundles);
            var appsToBundle = nonBundles.Concat(includedBundles.SelectMany(x => x.AppsInBundle));
            try
            {
                _AppsInBundle = appsToBundle.OrderBy(x => Convert.ToInt32(x.AppId)).ToArray();
            }
            catch (Exception)
            {
                _AppsInBundle = appsToBundle.OrderBy(x => x.AppId).ToArray();
            }
            if (_AppsInBundle.Any(x => x.InstallDir.ToLower() != InstallDir.ToLower()))
                throw new ArgumentException("Not all apps have the same install dir");

            UpdateAppId();
        }

        private ILocalSteamApplication[] _AppsInBundle;
        public IEnumerable<ILocalSteamApplication> AppsInBundle
        {
            get
            {
                return _AppsInBundle;
            }
        }

        private string _AppId;
        public string AppId
        {
            get
            {
                return _AppId;
            }
        }

        public long DiskSize
        {
            get
            {
                if (SizeIsMeasured)
                    return _AppsInBundle[0].DiskSize;
                else
                    return _AppsInBundle.Sum(x => x.DiskSize);
            }
        }

        public string GameName
        {
            get
            {
                return _AppsInBundle[0].GameName;
            }
        }

        public string ImageUrl
        {
            get
            {
                return _AppsInBundle[0].ImageUrl;
            }
        }

        public string InstallDir
        {
            get
            {
                return _AppsInBundle[0].InstallDir;
            }
        }

        public string ReadableDiskSize
        {
            get
            {
                return Utils.FileUtils.GetReadableFileSize(DiskSize);
            }
        }

        public string GameDir
        {
            get
            {
                return _AppsInBundle[0].GameDir;
            }
        }

        public bool SizeIsMeasured { get { return _AppsInBundle[0].SizeIsMeasured; } }

        public void MeasureDiskSize()
        {
            _AppsInBundle[0].MeasureDiskSize();
            NotifyPropertyChanged("DiskSize");
            NotifyPropertyChanged("ReadableDiskSize");
        }

        public void DeleteGameData()
        {
            foreach (var app in _AppsInBundle)
                app.DeleteGameData();
        }

        public void DeleteManifest()
        {
            foreach (var app in _AppsInBundle)
                app.DeleteManifest();
        }
        public bool CanCopy()
        {
            return _AppsInBundle.All(x => x.CanCopy());
        }

        public bool CanCopyIfForced()
        {
            return _AppsInBundle.All(x => x.CanCopyIfForced());
        }

        public void RefreshFromAcf()
        {
            foreach (var app in _AppsInBundle)
                app.RefreshFromAcf();

        }

        public bool ShouldRemove()
        {
            return AppsInBundle.All(x => _removedAppIds.Contains(x.AppId));
        }

        internal void MarkAppIdRemoved(string appId)
        {
            _removedAppIds.Add(appId);
            UpdateAppId();
        }


        private void UpdateAppId()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var app in _AppsInBundle.Where(x => !_removedAppIds.Contains(x.AppId)))
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }
                sb.Append(app.AppId);
            }
            _AppId = sb.ToString();
            NotifyPropertyChanged("AppId");
        }
    }
}
