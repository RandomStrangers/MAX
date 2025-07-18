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
using MAX.DB;
using MAX.Events.LevelEvents;
using MAX.SQL;
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX
{

    public static class LevelInfo
    {

        /// <summary> Array of all current loaded levels. </summary>
        /// <remarks> Note this field is highly volatile, you should cache references to the items array. </remarks>
        public static VolatileArray<Level> Loaded = new VolatileArray<Level>();

        public static Level FindExact(string name)
        {
            Level[] loaded = Loaded.Items;
            foreach (Level lvl in loaded)
            {
                if (lvl.name.CaselessEq(name)) return lvl;
            }
            return null;
        }

        public static void Add(Level lvl)
        {
            Loaded.Add(lvl);
            OnLevelAddedEvent.Call(lvl);
        }

        public static void Remove(Level lvl)
        {
            Loaded.Remove(lvl);
            OnLevelRemovedEvent.Call(lvl);
        }


        // TODO: support loading other map files eventually
        public static string[] AllMapFiles()
        {
            List<string> Files = new List<string>();
            Files.AddRange(FileIO.TryGetFiles("levels", "*.lvl"));
            Files.AddRange(FileIO.TryGetFiles("levels", "*.mcf"));
            Files.AddRange(FileIO.TryGetFiles("levels", "*.map"));
            Files.AddRange(FileIO.TryGetFiles("levels", "*.pklvl"));
            Files.AddRange(FileIO.TryGetFiles("levels", "*.flvl"));
            //Files.AddRange(FileIO.TryGetFiles("levels", "*.cw"));
            return Files.ToArray();
        }

        public static string[] AllMapNames()
        {
            string[] files = AllMapFiles();
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return files;
        }

        public static bool MapExists(string name)
        {
            return File.Exists(MapPath(name));
        }
        public static string Name(string name)
        {
            bool mcf = File.Exists("levels/" + name.ToLower() + ".mcf");
            bool map = File.Exists("levels/" + name.ToLower() + ".map");
            bool pklvl = File.Exists("levels/" + name.ToLower() + ".pklvl");
            bool flvl = File.Exists("levels/" + name.ToLower() + ".flvl");
            //bool cw = File.Exists("levels/" + name.ToLower() + ".cw");
            if (mcf)
            {
                return name.ToLower() + ".mcf";
            }
            else if (map)
            {
                return name.ToLower() + ".map";
            }
            else if (pklvl)
            {
                return name.ToLower() + ".pklvl";
            }
            else if (flvl)
            {
                return name.ToLower() + ".flvl";
            }
            /*else if (cw)
            {
                return name.ToLower() + ".cw";
            }*/
            else
            {
                return name.ToLower() + ".lvl";
            }
        }
        /// <summary> Relative path of a level's map file </summary>
        public static string MapPath(string name)
        {
            return "levels/" + Name(name);
        }


        /// <summary> Relative path of a level's backup folder </summary>
        public static string BackupBasePath(string name)
        {
            return Server.Config.BackupDirectory + "/" + name;
        }

        /// <summary> Relative path of a level's backup map directory </summary>
        public static string BackupDirPath(string name, string backup)
        {
            return BackupBasePath(name) + "/" + backup;
        }

        /// <summary> Relative path of a level's backup map file </summary>
        public static string BackupFilePath(string name, string backup)
        {
            bool mcf = File.Exists("levels/" + name.ToLower() + ".mcf");
            bool map = File.Exists("levels/" + name.ToLower() + ".map");
            bool pklvl = File.Exists("levels/" + name.ToLower() + ".pklvl");
            bool flvl = File.Exists("levels/" + name.ToLower() + ".flvl");
            //bool cw = File.Exists("levels/" + name.ToLower() + ".cw");
            if (mcf)
            {
                return BackupDirPath(name, backup) + "/" + name + ".mcf";
            }
            else if (map)
            {
                return BackupDirPath(name, backup) + "/" + name + ".map";
            }
            if (pklvl)
            {
                return BackupDirPath(name, backup) + "/" + name + ".pklvl";
            }
            if (flvl)
            {
                return BackupDirPath(name, backup) + "/" + name + ".flvl";
            }
            /*else if (cw)
            {
                return BackupDirPath(name, backup) + "/" + name + ".cw";
            }*/
            else
            {
                return BackupDirPath(name, backup) + "/" + name + ".lvl";
            }
        }

        public static string BackupNameFrom(string path)
        {
            return path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
        }

        public static int LatestBackup(string map)
        {
            string root = BackupBasePath(map);
            string[] backups = Directory.GetDirectories(root);
            int latest = 0;

            foreach (string path in backups)
            {
                string backupName = BackupNameFrom(path);

                if (!int.TryParse(backupName, out int num)) continue;
                latest = Math.Max(num, latest);
            }
            return latest;
        }

        public static string NextBackup(string map)
        {
            string root = BackupBasePath(map);
            Directory.CreateDirectory(root);

            return (LatestBackup(map) + 1).ToString();
        }

        /// <summary> Relative path of a level's property file </summary>
        public static string PropsPath(string name)
        {
            return "levels/level properties/" + Name(name) + ".properties";
        }

        public static LevelConfig GetConfig(string map)
        {
            return GetConfig(map, out Level _);
        }

        public static LevelConfig GetConfig(string map, out Level lvl)
        {
            lvl = FindExact(map);
            if (lvl != null) return lvl.Config;

            string propsPath = PropsPath(map);
            LevelConfig cfg = new LevelConfig();
            cfg.Load(propsPath);
            return cfg;
        }

        public static bool Check(Player p, LevelPermission plRank, string map, string action, out LevelConfig cfg)
        {
            cfg = GetConfig(map, out Level lvl);
            if (p.IsMAX) return true;
            if (lvl != null) return Check(p, plRank, lvl, action);

            AccessController visit = new LevelAccessController(cfg, map, true);
            AccessController build = new LevelAccessController(cfg, map, false);
            if (!visit.CheckDetailed(p, plRank) || !build.CheckDetailed(p, plRank))
            {
                p.Message("Hence, you cannot {0}.", action);
                return false;
            }
            return true;
        }

        public static bool Check(Player p, LevelPermission plRank, string map, string action)
        {
            return Check(p, plRank, map, action, out LevelConfig _);
        }

        public static bool Check(Player p, LevelPermission plRank, Level lvl, string action)
        {
            if (p.IsMAX) return true;
            if (!lvl.VisitAccess.CheckDetailed(p, plRank) || !lvl.BuildAccess.CheckDetailed(p, plRank))
            {
                p.Message("Hence, you cannot {0}.", action);
                return false;
            }
            return true;
        }

        public static bool ValidName(string map)
        {
            foreach (char c in map)
            {
                if (!Database.ValidNameChar(c)) return false;
            }
            return true;
        }


        public static bool IsRealmOwner(string name, string map)
        {
            LevelConfig cfg = GetConfig(map);
            return IsRealmOwner(map, cfg, name);
        }

        public static bool IsRealmOwner(Level lvl, string name)
        {
            return IsRealmOwner(lvl.name, lvl.Config, name);
        }

        public static bool IsRealmOwner(string map, LevelConfig cfg, string name)
        {
            string[] owners = cfg.RealmOwner.SplitComma();
            if (owners.Length > 0)
            {
                foreach (string owner in owners)
                {
                    if (owner.CaselessEq(name)) return true;
                }
                return false;
            }

            // For backwards compatibility, treat name+XYZ map names as belonging to name+
            // If no + though, don't use because otherwise people can register accounts and claim maps
            return Server.Config.ClassicubeAccountPlus && map.CaselessStarts(name);
        }

        public static string DefaultRealmOwner(string map)
        {
            bool plus = Server.Config.ClassicubeAccountPlus;
            // Early out when either
            //  1) accounts aren't using +
            //  2) map name doesn't include +
            if (!plus || map.IndexOf('+') == -1) return null;

            // Convert username+23 to username+
            while (map.Length > 0 && char.IsNumber(map[map.Length - 1]))
            {
                map = map.Substring(0, map.Length - 1);
            }

            // Match the backwards compatibilty case of IsRealmOwner
            return PlayerDB.FindName(map);
        }
    }
}