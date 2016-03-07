/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
#define PARSE_ELEGANT

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace GamePipeLib.Utils
{
    static class SteamWebUtils
    {

        public static IEnumerable<string> ScrapeAllAppIdsFromUserIdPage(string steamId)
        {
            var url = string.Format("http://steamcommunity.com/profiles/{0}/games/?tab=all", steamId);
            //var url = string.Format("http://steamcommunity.com/id/{0}/games/?tab=all", steamCommunityId);
            var request = System.Net.HttpWebRequest.Create(url);

            const string APP_ID_TAG = "appid";
#if PARSE_ELEGANT
            const string NAME_TAG = "name";
            var chunkParser = new System.Text.RegularExpressions.Regex("{.*?}");
            var pairParser = new System.Text.RegularExpressions.Regex("\"(?'tag'\\w+)\":(?'value'[^,]+)");
#else
            var crudeParser = new Regex("\"" + "{0}" + "\":(?'value'[^,]+)");
#endif
            string gameLine;
            using (var response = request.GetResponse())
            {
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    var fileName = String.Format("webResponse_{0}.txt", DateTime.Now.ToString("yyyMMdd-HH-mm-ss"));
                    File.WriteAllText(fileName, reader.ReadToEnd());
                    gameLine = File.ReadAllLines(fileName).Where(line => line.TrimStart().StartsWith("var rgGames =")).FirstOrDefault();

#if PARSE_ELEGANT
                    var outputList = new List<string>();
                    foreach (Match chunk in chunkParser.Matches(gameLine))
                    {
                        string appId = null, name = null;
                        foreach (Match pair in pairParser.Matches(chunk.Value))
                        {
                            string tag = null, value = null;
                            try
                            {
                                tag = pair.Groups["tag"].Value;
                                value = pair.Groups["value"].Value;
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            if (tag == APP_ID_TAG)
                            {
                                appId = value;
                            }
                            else if (tag == NAME_TAG)
                            {
                                name = value;
                            }
                            if (name != null && appId != null)
                            {
                                outputList.Add(string.Format("{0}, {1}", appId, name));
                                break;
                            }
                        }
                    }
                    return outputList;
                }
            }

#else
                    return from Match match in crudeParser.Matches(gameLine)
                           select match.Groups["value"].Value;
#endif

            //{"appid":8930,"name":"Sid Meier's Civilization V",
            //"logo":"http:\/\/cdn.akamai.steamstatic.com\/steamcommunity\/public\/images\/apps\/8930\/2203f62bd1bdc75c286c13534e50f22e3bd5bb58.jpg",
            //"availStatLinks":{"achievements":true,"global_achievements":true,"stats":false,"leaderboards":false,"global_leaderboards":false},
            //"friendlyURL":"CivV","hours_forever":"384","last_played":1433811474}
        }
    }
}
