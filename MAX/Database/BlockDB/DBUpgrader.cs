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
using MAX.Events.PlayerEvents;
using MAX.SQL;
using System;
using System.Collections.Generic;

namespace MAX.DB
{
    public static class DBUpgrader
    {
        public static bool Upgrading = false;
        public static string[] CompactMessages = new string[] {
            " If you are using SQLite, It is recommended that you compact the database by either:",
            "   a) doing VACUUM on the database (note that this will create a temp file as big as MAX.db)",
            "   b) doing /server backup litedb, shutting down the server, deleting MAX.db, then finally running /server import SQL",
        };

        public static void Lock()
        {
            Upgrading = true;
            Logger.Log(LogType.SystemActivity, "Kicking players and unloading levels..");
            OnPlayerStartConnectingEvent.Register(ConnectingHandler, Priority.System_Level);

            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players)
            {
                p.Leave("Upgrading BlockDB. Check back later!");
            }

            Level[] levels = LevelInfo.Loaded.Items;
            foreach (Level lvl in levels)
            {
                lvl.Unload();
            }
            Logger.Log(LogType.SystemActivity, "Kicked all players and unloaded levels.");
        }

        public static void Unlock()
        {
            OnPlayerStartConnectingEvent.Unregister(ConnectingHandler);
            Player.MAX.MessageLines(CompactMessages);
            Logger.Log(LogType.SystemActivity, "&aUpgrade finished!");
            Upgrading = false;
        }

        public static void ConnectingHandler(Player p, string mppass)
        {
            p.Leave("Upgrading BlockDB (" + Progress + "). Check back later!");
            p.cancelconnecting = true;
        }


        public static int current, count;
        public static string Progress { get { return current + " / " + count; } }

        public static void Upgrade()
        {
            List<string> tables = Database.Backend.AllTables();
            List<string> blockDBTables = new List<string>(tables.Count);
            current = 0; count = 0;

            foreach (string table in tables)
            {
                if (!table.CaselessStarts("block")) continue;
                blockDBTables.Add(table);
            }

            current = 0;
            count = blockDBTables.Count;
            Logger.Log(LogType.SystemActivity, "Upgrading {0} tables. This may take several hours.", count);

            BlockDBTableDumper dumper = new BlockDBTableDumper();
            foreach (string table in blockDBTables)
            {
                current++;
                try
                {
                    dumper.DumpTable(table);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error dumping BlockDB table " + table, ex);
                }
            }
        }
    }
}