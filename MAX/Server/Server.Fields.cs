﻿/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
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
using MAX.Network;
using MAX.Tasks;
using System;
using System.Collections.Generic;

namespace MAX
{
    public partial class Server
    {
        public delegate void OnMAXOrder(string ord, string message);
        public static event OnMAXOrder MAXOrder;
        public delegate void MessageEventHandler(string message);
        public delegate void VoidHandler();
        public static event MessageEventHandler OnURLChange;
        public static event VoidHandler OnSettingsUpdate;
        public static ServerConfig Config = new ServerConfig();
        public static DateTime StartTime;
        public static PlayerMetaList RankInfo = new PlayerMetaList("text/rankinfo.txt"),
            Notes = new PlayerMetaList("text/notes.txt");
        /// <summary> *** DO NOT USE THIS! *** Use VersionString, as this field is a constant and is inlined if used. </summary>
        public const string InternalVersion = "0.0.3.2", 
            SoftwareNameConst = "&4MAX";
        public static string fullName, RestartPath, Version = InternalVersion, SoftwareName = SoftwareNameConst;
        public const string NameVersioned = SoftwareNameConst + " " + InternalVersion;
        public static string SoftwareNameVersioned
        {
            // By default, if SoftwareName gets externally changed, that is reflected in SoftwareNameVersioned too
            get { return fullName ?? SoftwareName + " " + Version; }
            set { fullName = value; }
        }
        public static INetListen Listener = new TcpListen();
        public static bool SetupFinished, TLIMode, chatmod, 
            flipHead, shuttingDown, voting, cancelorder;
        public static PlayerList whiteList, invalidIds, ignored, 
            hidden, agreed, vip, 
            noEmotes, lockdown, reviewlist = new PlayerList();
        public static PlayerExtList models, skins, reach, rotations, modelScales,
            bannedIP, jailed, muted, tempBans, tempRanks, AutoloadMaps;
        public static readonly List<string> Devs = new List<string>()
        {
            "DarkBurningFlame", "BurningFlame", "SuperNova", "DeadNova",
            "HyperNova", "RandomStranger05", "GoldenSparks", "AurumStellae",
            "sethbatman05", "sethbatman2005", "jackstage1", "Pattykaki45",
            "jaketheidiot", "RandomStrangers", "ArgenteaeLunae", "Argenteae",
            "HarmonyNetwork" , "krowteNynomraH"
        }, Opstats = new List<string>() 
        { 
            "ban", "tempban", "xban", "banip", 
            "kick", "warn", "mute", "jail", "setrank" 
        };
        public static Level mainLevel;
        public static string[] announcements = new string[0];
        public static ExtrasCollection Extras = new ExtrasCollection();
        public static int YesVotes, NoVotes;
        public const int MAX_PLAYERS = int.MaxValue;
        public static Scheduler MainScheduler = new Scheduler("MAX_MainScheduler"),
            Background = new Scheduler("MAX_BackgroundScheduler"),
            Critical = new Scheduler("MAX_CriticalScheduler"),
            Heartbeats = new Scheduler("MAX_HeartbeatsScheduler");
        public const byte VERSION_0016 = 3, // classic 0.0.16
                    VERSION_0017 = 4, // classic 0.0.17 / 0.0.18
                    VERSION_0019 = 5, // classic 0.0.19
                    VERSION_0020 = 6, // classic 0.0.20 / 0.0.21 / 0.0.23
                    VERSION_0030 = 7; // classic 0.30 (final)
    }
}