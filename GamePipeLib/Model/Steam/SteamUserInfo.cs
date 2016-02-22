/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
namespace GamePipeLib.Model.Steam
{
    public class SteamUserInfo 
    {
        const string LOGINUSERS_VDF_ACCOUNT_NAME = "AccountName";
        const string LOGINUSERS_VDF_PERSONA_NAME = "PersonaName";

        public SteamUserInfo()
        {
            Initialize();
        }

        private SteamUser _PrimaryUser;
        public SteamUser PrimaryUser
        {
            get { return _PrimaryUser; }
        }

        private List<SteamUser> _OtherUsers = new List<SteamUser>();
        public IEnumerable<SteamUser> OtherUsers
        {
            get { return _OtherUsers; }

        }

        private void Initialize()
        {
            var loginUsersPath = GetFileRelativeToSteam("config\\loginusers.vdf");
            string primaryUserLogin = GetPrimaryUserLogin();
            var contents = File.ReadAllText(loginUsersPath);
            string userClusterParserPattern = "^\\s*\"(?'userId'\\d+)\"\\s*{(?'chunk'.*?)}";
            Regex userChunkParser = new Regex(userClusterParserPattern, RegexOptions.Multiline | RegexOptions.Singleline);
            _OtherUsers.Clear();
            _PrimaryUser = null;
            foreach (Match match in userChunkParser.Matches(contents))
            {
                var pairs = GamePipeLib.Utils.SteamDirParsingUtils.ParseStringPairs(match.Groups["chunk"].Value);
                string id = match.Groups["userId"].Value;
                string account = null, persona = null;

                foreach (var pair in pairs)
                {
                    if (pair.Item1 == LOGINUSERS_VDF_ACCOUNT_NAME)
                    {
                        account = pair.Item2;
                        if (persona != null) break;
                    }
                    else if (pair.Item1 == LOGINUSERS_VDF_PERSONA_NAME)
                    {
                        persona = pair.Item2;
                        if (account != null) break;
                    }
                }
                SteamUser user = new SteamUser(persona, id, (account == primaryUserLogin));
                if ((_PrimaryUser == null) && user.IsPrimary)
                {
                    _PrimaryUser = user;
                }
                else
                {
                    _OtherUsers.Add(user);
                }
            }

        }

        private string GetPrimaryUserLogin()
        {
            var steamAppDataPath = GetFileRelativeToSteam("config\\SteamAppData.vdf");
            var lines = File.ReadAllLines(steamAppDataPath);

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("\"AutoLoginUser\""))
                {
                    return line.Replace("\"", "").Replace("AutoLoginUser", "").Trim();
                }
            }
            return null;
        }

        public static string GetFileRelativeToSteam(string relativePath)
        {
            return System.IO.Path.Combine(GamePipeLib.Utils.SteamDirParsingUtils.SteamDirectory, relativePath);
        }
    }


    public class SteamUser
    {
        public SteamUser(string persona, string id, bool isPrimary)
        {
            if (persona == null) throw new ArgumentNullException("persona");
            if (id == null) throw new ArgumentNullException("id");
            Persona = persona;
            Id = id;
            IsPrimary = isPrimary;
        }

        public string Persona { get; set; }
        public string Id { get; set; }
        public bool IsPrimary { get; set; }

        public IEnumerable<string> GetAllUsersAppIds()
        {
            return GamePipeLib.Utils.SteamWebUtils.ScrapeAllAppIdsFromUserIdPage(Id);
        }
    }
}
