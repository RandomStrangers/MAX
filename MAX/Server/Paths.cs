﻿/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */

using System.IO;

namespace MAX
{
    /// <summary> Provides a centralised list of files and paths used. </summary>
    public static class Paths
    {
        public const string CustomColorsFile = "text/customcolors.txt";
        public const string TempRanksFile = "text/tempranks.txt";
        public const string TempBansFile = "text/tempbans.txt";
        public const string CustomTokensFile = "text/custom$s.txt";
        public const string BadWordsFile = "text/badwords.txt";
        public const string EatMessagesFile = "text/eatmessages.txt";
        public const string RulesFile = "text/rules.txt";
        public const string OprulesFile = "text/oprules.txt";
        public const string FaqFile = "text/faq.txt";
        public const string AnnouncementsFile = "text/messages.txt";
        public const string DesignationsFile = "text/designations.txt";
        public const string NewsFile = "text/news.txt";
        public const string WelcomeFile = "text/Welcome.txt";
        public const string JokerFile = "text/joker.txt";
        public const string EightBallFile = "text/8ball.txt";

        public const string BlockPermsFile = "props/block.properties";
        public const string OrdPermsFile = "props/order.properties";
        public const string OrdExtraPermsFile = "props/ExtraOrderPermissions.properties";
        public const string EconomyPropsFile = "props/economy.properties";
        public const string ServerPropsFile = "props/server.properties";
        public const string RankPropsFile = "props/ranks.properties";
        public const string AuthServicesFile = "props/authservices.properties";
        public const string CPEDisabledFile = "props/cpe.properties";

        public const string ImportsDir = "extra/import/";
        public const string WaypointsDir = "extra/Waypoints/";

        /// <summary> Relative path of the file containing a map's bots. </summary>
        public static string BotsPath(string map)
        {
            return "extra/bots/" + map + ".json";
        }
        /// <summary> Relative path of the file containing a map's block definitions. </summary>
        public static string MapBlockDefs(string map)
        {
            return "blockdefs/lvl_" + map + ".json";
        }
        /// <summary> Relative path of a deleted level's map file. </summary>
        public static string DeletedMapFile(string map)
        {
            bool mcf = File.Exists("levels/deleted/" + map + ".mcf");
            bool mapFile = File.Exists("levels/deleted/" + map + ".map");
            bool pklvl = File.Exists("levels/deleted/" + map + ".pklvl");
            bool flvl = File.Exists("levels/deleted/" + map + ".flvl");
            //bool cw = File.Exists("levels/deleted/" + map + ".cw");
            if (mcf)
            {
                return "levels/deleted/" + map + ".mcf";
            }
            else if (mapFile)
            {
                return "levels/deleted/" + map + ".map";
            }
            else if (pklvl)
            {
                return "levels/deleted/" + map + ".pklvl";
            }
            else if (flvl)
            {
                return "levels/deleted/" + map + ".flvl";
            }
            /*else if (cw)
            {
                return "levels/deleted/" + map + ".cw";
            }*/
            else
            {
                return "levels/deleted/" + map + ".lvl";
            }
        }
        /// <summary> Relative path of a level's previous save map file. </summary>
        public static string PrevMapFile(string map)
        {
            bool mcf = File.Exists("levels/" + map.ToLower() + ".mcf");
            bool mapFile = File.Exists("levels/" + map.ToLower() + ".map");
            bool pklvl = File.Exists("levels/" + map.ToLower() + ".pklvl");
            bool flvl = File.Exists("levels/" + map.ToLower() + ".flvl");
            //bool cw = File.Exists("levels/" + map.ToLower() + ".cw");
            if (mcf)
            {
                return "levels/prev/" + map.ToLower() + ".mcf.prev";
            }
            else if (mapFile)
            {
                return "levels/prev/" + map.ToLower() + ".map.prev";
            }
            else if (pklvl)
            {
                return "levels/prev/" + map.ToLower() + ".pklvl.prev";
            }
            else if (flvl)
            {
                return "levels/prev/" + map.ToLower() + ".flvl.prev";
            }
            /*else if (cw)
            {
                return "levels/prev/" + map.ToLower() + ".cw.prev";
            }*/
            else
            {
                return "levels/prev/" + map.ToLower() + ".lvl.prev";
            }
        }
        /// <summary> Relative path of a block properties file. </summary>     
        public static string BlockPropsPath(string group)
        {
            return "blockprops/" + group + ".txt";
        }
    }
}