﻿/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
    
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
using MAX.SQL;
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX
{
    public partial class Server
    {
        public static ColumnDesc[] playersTable = new ColumnDesc[] {
            new ColumnDesc("ID", ColumnType.Integer, priKey: true, autoInc: true, notNull: true),
            new ColumnDesc("Name", ColumnType.VarChar, 17),
            new ColumnDesc("IP", ColumnType.Char, 15),
            new ColumnDesc("FirstLogin", ColumnType.DateTime),
            new ColumnDesc("LastLogin", ColumnType.DateTime),
            new ColumnDesc("totalLogin", ColumnType.Int24),
            new ColumnDesc("Title", ColumnType.Char, 20),
            new ColumnDesc("TotalDeaths", ColumnType.Int16),
            new ColumnDesc("Money", ColumnType.UInt24),
            new ColumnDesc("totalBlocks", ColumnType.Int32),
            new ColumnDesc("totalCuboided", ColumnType.Int32),
            new ColumnDesc("totalKicked", ColumnType.Int24),
            new ColumnDesc("TimeSpent", ColumnType.VarChar, 20),
            new ColumnDesc("color", ColumnType.VarChar, 6),
            new ColumnDesc("title_color", ColumnType.VarChar, 6),
            new ColumnDesc("Messages", ColumnType.UInt24),
        };

        public static ColumnDesc[] opstatsTable = new ColumnDesc[] {
            new ColumnDesc("ID", ColumnType.Integer, priKey: true, autoInc: true, notNull: true),
            new ColumnDesc("Time", ColumnType.DateTime),
            new ColumnDesc("Name", ColumnType.VarChar, 17),
            new ColumnDesc("Ord", ColumnType.VarChar, 40),
            new ColumnDesc("Ordmsg", ColumnType.VarChar, 40),
        };

        public static void InitDatabase()
        {
            if (!Directory.Exists("blockdb")) Directory.CreateDirectory("blockdb");

            try
            {
                Database.Backend.CreateDatabase();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                Logger.Log(LogType.Warning, "MySQL settings have not been set! Please Setup using the properties window.");
                return;
            }

            Database.CreateTable("Opstats", opstatsTable);
            Database.CreateTable("Players", playersTable);

            //since MCForge 5.5.11 we are cleaning up the table Playerords
            //if Playerords exists copy-filter to Opstats and remove Playerords
            if (Database.TableExists("Playerords"))
            {
                const string sql = "INSERT INTO Opstats (Time, Name, Ord, Ordmsg) SELECT Time, Name, Ord, Ordmsg FROM Playerords WHERE {0};";
                foreach (string ord in Opstats)
                    Database.Execute(string.Format(sql, "ord = '" + ord + "'"));
                Database.Execute(string.Format(sql, "ord = 'review' AND ordmsg = 'next'"));
                Database.DeleteTable("Playerords");
            }

            List<string> columns = Database.Backend.ColumnNames("Players");
            if (columns.Count == 0) return;

            if (!columns.CaselessContains("Color"))
            {
                Database.AddColumn("Players", new ColumnDesc("color", ColumnType.VarChar, 6), "totalKicked");
            }
            if (!columns.CaselessContains("Title_Color"))
            {
                Database.AddColumn("Players", new ColumnDesc("title_color", ColumnType.VarChar, 6), "color");
            }
            if (!columns.CaselessContains("TimeSpent"))
            {
                Database.AddColumn("Players", new ColumnDesc("TimeSpent", ColumnType.VarChar, 20), "totalKicked");
            }
            if (!columns.CaselessContains("TotalCuboided"))
            {
                Database.AddColumn("Players", new ColumnDesc("totalCuboided", ColumnType.Int32), "totalBlocks");
            }
            if (!columns.CaselessContains("Messages"))
            {
                Database.AddColumn("Players", new ColumnDesc("Messages", ColumnType.UInt24), "title_color");
            }
        }
    }
}