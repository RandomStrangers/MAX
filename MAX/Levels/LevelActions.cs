﻿/*
    Copyright 2011 MCForge
        
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
using MAX.Blocks.Extended;
using MAX.Bots;
using MAX.DB;
using MAX.Events.LevelEvents;
using MAX.Levels.IO;
using MAX.SQL;
using MAX.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX
{
    public static class LevelActions
    {
        public static string BlockPropsLvlPath(string map)
        {
            return Paths.BlockPropsPath("_" + map);
        }
        public static string BlockPropsOldPath(string map)
        {
            return Paths.BlockPropsPath("lvl_" + map);
        }

        public static bool Backup(string map, string backupName)
        {
            string basePath = LevelInfo.BackupBasePath(map);
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            string path = Path.Combine(basePath, backupName);
            Directory.CreateDirectory(path);
            string ext = Path.GetExtension(path);
            bool lvl = DoAction(LevelInfo.MapPath(map), Path.Combine(path, map + ext), action_copy);
            bool props = DoAction(LevelInfo.PropsPath(map), Path.Combine(path, "map.properties"), action_copy);
            bool defs = DoAction(Paths.MapBlockDefs(map), Path.Combine(path, "blockdefs.json"), action_copy);
            bool blkOld = DoAction(BlockPropsOldPath(map), Path.Combine(path, "blockprops.txt"), action_copy);
            bool blkCur = DoAction(BlockPropsLvlPath(map), Path.Combine(path, "blockprops.txt"), action_copy);
            bool bots = DoAction(Paths.BotsPath(map), Path.Combine(path, "bots.json"), action_copy);

            return lvl && props && defs && blkOld && blkCur && bots;
        }


        /// <summary> Renames the given level and its associated metadata. Does not unload. </summary>
        /// <remarks> Backups are NOT renamed. </remarks>
        public static bool Rename(Player p, string src, string dst)
        {
            if (LevelInfo.MapExists(dst))
            {
                p.Message("&WLevel \"{0}\" already exists.", dst);
                return false;
            }

            Level lvl = LevelInfo.FindExact(src);
            if (lvl == Server.mainLevel)
            {
                p.Message("Cannot rename the main level.");
                return false;
            }

            List<Player> players = null;
            if (lvl != null) players = lvl.GetPlayers();

            if (lvl != null && !lvl.Unload())
            {
                p.Message("Unable to rename the level, because it could not be unloaded. " +
                          "A game may currently be running on it.");
                return false;
            }

            FileIO.TryMove(LevelInfo.MapPath(src), LevelInfo.MapPath(dst));
            DoAll(src, dst, action_move);

            // TODO: Should we move backups still
            try
            {
                //MoveBackups(src, dst);
            }
            catch
            {
            }

            RenameDatabaseTables(p, src, dst);
            BlockDBFile.MoveBackingFile(src, dst);
            OnLevelRenamedEvent.Call(src, dst);
            if (players == null) return true;

            // Move all the old players to the renamed map
            Load(p, dst, false);
            foreach (Player pl in players)
                PlayerActions.ChangeMap(pl, dst);
            return true;
        }

        public static void RenameDatabaseTables(Player p, string src, string dst)
        {
            if (Database.TableExists("Block" + src))
            {
                Database.RenameTable("Block" + src, "Block" + dst);
            }
            object srcLocker = ThreadSafeCache.DBCache.GetLocker(src);
            object dstLocker = ThreadSafeCache.DBCache.GetLocker(dst);

            lock (srcLocker)
                lock (dstLocker)
                {
                    Portal.MoveAll(src, dst);
                    MessageBlock.MoveAll(src, dst);

                    if (Database.TableExists("Zone" + src))
                    {
                        Database.RenameTable("Zone" + src, "Zone" + dst);
                    }
                }

            p.Message("Updating portals that go to {0}..", src);
            List<string> tables = Database.Backend.AllTables();
            foreach (string table in tables)
            {
                if (!table.StartsWith("Portals")) continue;

                Database.UpdateRows(table, "ExitMap=@1",
                                    "WHERE ExitMap=@0", src, dst);
            }
        }

        public static void MoveBackups(string src, string dst) 
        {
            string srcBase = LevelInfo.BackupBasePath(src);
            string dstBase = LevelInfo.BackupBasePath(dst);
            if (!Directory.Exists(srcBase)) return;
            Directory.CreateDirectory(dstBase);
            
            string[] backups = Directory.GetDirectories(srcBase);
            for (int i = 0; i < backups.Length; i++) 
            {
                string name = LevelInfo.BackupNameFrom(backups[i]);
                string srcFile = LevelInfo.BackupFilePath(src, name);
                string dstFile = LevelInfo.BackupFilePath(dst, name);
                string dstDir = LevelInfo.BackupDirPath(dst, name);
                
                Directory.CreateDirectory(dstDir);
                FileIO.TryMove(srcFile, dstFile);
                Directory.Delete(backups[i]);
            }
            Directory.Delete(srcBase);
        }


        /// <summary> Deletes a level and its associated metadata. </summary>
        public static bool Delete(Player p, string map)
        {
            Level lvl = LevelInfo.FindExact(map);
            if (lvl == Server.mainLevel)
            {
                p.Message("Cannot delete the main level.");
                return false;
            }

            if (lvl != null && !lvl.Unload())
            {
                p.Message("Unable to delete the level, because it could not be unloaded. " +
                          "A game may currently be running on it.");
                return false;
            }

            p.Message("Created backup.");
            if (!Directory.Exists("levels/deleted"))
                Directory.CreateDirectory("levels/deleted");

            if (File.Exists(Paths.DeletedMapFile(map)))
            {
                int num = 0;
                while (File.Exists(Paths.DeletedMapFile(map + num))) num++;

                FileIO.TryMove(LevelInfo.MapPath(map), Paths.DeletedMapFile(map + num));
            }
            else
            {
                FileIO.TryMove(LevelInfo.MapPath(map), Paths.DeletedMapFile(map));
            }

            DoAll(map, "", action_delete);
            DeleteDatabaseTables(map);
            BlockDBFile.DeleteBackingFile(map);
            OnLevelDeletedEvent.Call(map);
            return true;
        }

        public static void DeleteDatabaseTables(string map)
        {
            Database.DeleteTable("Block" + map);

            object locker = ThreadSafeCache.DBCache.GetLocker(map);
            lock (locker)
            {
                Portal.DeleteAll(map);
                MessageBlock.DeleteAll(map);
                Database.DeleteTable("Zone" + map);
            }
        }


        public static void Replace(Level old, Level lvl)
        {
            old.SaveBlockDBChanges();
            LevelInfo.Remove(old);
            LevelInfo.Add(lvl);

            old.SetPhysics(0);
            old.ClearPhysics();
            lvl.StartPhysics();

            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level != old) continue;
                pl.level = lvl;
                PlayerActions.ReloadMap(pl);
            }

            old.Unload(true, false);
            if (old == Server.mainLevel)
                Server.mainLevel = lvl;
        }


        /// <summary> Copies a level and its related metadata. </summary>
        /// <remarks> Backups and BlockDB are NOT copied. </remarks>
        public static bool Copy(Player p, string src, string dst)
        {
            if (LevelInfo.MapExists(dst))
            {
                p.Message("&WLevel \"{0}\" already exists.", dst);
                return false;
            }

            // Make sure any changes to live map are saved first
            Level lvl = LevelInfo.FindExact(src);
            if (lvl != null && !lvl.Save(true))
            {
                p.Message("&WUnable to save {0}! Some recent block changes may not be copied.", src);
            }

            File.Copy(LevelInfo.MapPath(src), LevelInfo.MapPath(dst));
            DoAll(src, dst, action_copy);
            CopyDatabaseTables(src, dst);
            OnLevelCopiedEvent.Call(src, dst);
            return true;
        }

        public static void CopyDatabaseTables(string src, string dst)
        {
            object srcLocker = ThreadSafeCache.DBCache.GetLocker(src);
            object dstLocker = ThreadSafeCache.DBCache.GetLocker(dst);

            lock (srcLocker)
                lock (dstLocker)
                {
                    Portal.CopyAll(src, dst);
                    MessageBlock.CopyAll(src, dst);
                }
        }


        /// <summary> Reloads the given level for all players in that level </summary>
        /// <param name="src"> The player that triggered the reload </param>
        /// <param name="announce"> Whether to show a message in chat </param>
        public static void ReloadAll(Level lvl, Player src, bool announce)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players)
            {
                if (p.level != lvl) continue;
                PlayerActions.ReloadMap(p);
                if (!announce) continue;

                if (src == null || !p.CanSee(src))
                {
                    p.Message("&bMap reloaded");
                }
                else
                {
                    p.Message("&bMap reloaded by " + p.FormatNick(src));
                }
                if (src.CanSee(p))
                {
                    src.Message("&4Finished reloading for " + src.FormatNick(p));
                }
            }
        }

        public const byte action_delete = 0;
        public const byte action_move = 1;
        public const byte action_copy = 2;

        public static void DoAll(string src, string dst, byte action)
        {
            DoAction(LevelInfo.MapPath(src) + ".backup",
                     LevelInfo.MapPath(dst) + ".backup", action);
            DoAction(LevelInfo.PropsPath(src),
                     LevelInfo.PropsPath(dst), action);
            DoAction("levels/level properties/" + src,
                     LevelInfo.PropsPath(dst), action);
            DoAction(Paths.MapBlockDefs(src),
                     Paths.MapBlockDefs(dst), action);
            DoAction(BlockPropsOldPath(src),
                     BlockPropsOldPath(dst), action);
            DoAction(BlockPropsLvlPath(src),
                     BlockPropsLvlPath(dst), action);
            DoAction(Paths.BotsPath(src),
                     Paths.BotsPath(dst), action);
        }

        public static bool DoAction(string src, string dst, byte action)
        {
            if (!File.Exists(src)) return true;
            try
            {
                if (action == action_delete)
                {
                    FileIO.TryDelete(src);
                }
                else if (action == action_move)
                {
                    FileIO.TryMove(src, dst);
                }
                else if (action == action_copy)
                {
                    File.Copy(src, dst, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
        }


        public static Level Load(Player p, string map, bool announce)
        {
            map = map.ToLower();
            Level cur = LevelInfo.FindExact(map);
            if (cur != null)
            {
                p.Message("&WLevel {0} &Wis already loaded.", cur.ColoredName);
                return null;
            }

            try
            {
                Level lvl = ReadLevel(p, map);
                if (lvl == null || !lvl.CanJoin(p)) return null;

                cur = LevelInfo.FindExact(map);
                if (cur != null)
                {
                    p.Message("&WLevel {0} &Wis already loaded.", cur.ColoredName);
                    return null;
                }

                LevelInfo.Add(lvl);
                if (!announce) return lvl;

                string autoloadMsg = "Level " + lvl.ColoredName + " &Sloaded.";
                Chat.Message(ChatScope.All, autoloadMsg, null, Chat.FilterVisible(p));
                return lvl;
            }
            finally
            {
                Server.DoGC();
            }
        }

        public static Level ReadBackup(Player p, string map, string path, string type)
        {
            Logger.Log(LogType.Warning, "Attempting to load {1} for {0}", map, type);
            Level lvl = Level.Load(map, path);

            if (lvl != null) return lvl;
            p.Message("&WLoading {1} of {0} failed.", map, type);
            return null;
        }

        public static Level ReadLevel(Player p, string map)
        {
            Level lvl = Level.Load(map);
            if (lvl != null) return lvl;

            string path = LevelInfo.MapPath(map) + ".backup";
            if (!File.Exists(path))
            {
                p.Message("Level \"{0}\" does not exist", map);
                return lvl;
            }
            lvl = ReadBackup(p, map, path, "backup copy");
            if (lvl != null) return lvl;

            path = Paths.PrevMapFile(map);
            lvl = ReadBackup(p, map, path, "previous save");
            if (lvl != null) return lvl;

            string backupDir = LevelInfo.BackupBasePath(map);
            if (Directory.Exists(backupDir))
            {
                int latest = LevelInfo.LatestBackup(map);
                path = LevelInfo.BackupFilePath(map, latest.ToString());
                lvl = ReadBackup(p, map, path, "latest backup");
            }
            else
            {
                p.Message("&WLatest backup of {0} does not exist.", map);
            }
            return lvl;
        }


        public static Level LoadMuseum(string name, string mapName, string path)
        {
            Level lvl = GetMuseum(name, path);
            lvl.MapName = mapName;
            lvl.IsMuseum = true;

            Level.LoadMetadata(lvl);
            lvl.BuildAccess.Min = LevelPermission.Terminal;
            lvl.Config.Physics = 0;
            return lvl;
        }

        public static Level GetMuseum(string name, string path)
        {
            Player[] players = PlayerInfo.Online.Items;
            // Since museums are essentially readonly anyways, try to reuse
            //  blocks/CustomBlocks from existing museum to reduce memory usage
            foreach (Player pl in players)
            {
                Level lvl = pl.level;
                if (!lvl.IsMuseum || lvl.name != name) continue;

                Level clone = new Level
                {
                    blocks = lvl.blocks,
                    CustomBlocks = lvl.CustomBlocks
                };

                // Just in case museum was unloaded a split second before
                if (clone.blocks == null || clone.CustomBlocks == null) break;

                clone.Init(name, lvl.Width, lvl.Height, lvl.Length);
                return clone;
            }

            return IMapImporter.Decode(path, name, false);
        }


        public static void Resize(ref Level lvl, int width, int height, int length)
        {
            Level res = new Level(lvl.name, (ushort)width, (ushort)height, (ushort)length)
            {
                hasPortals = lvl.hasPortals,
                hasMessageBlocks = lvl.hasMessageBlocks
            };
            byte[] src = lvl.blocks, dst = res.blocks;

            // Copy blocks in bulk
            width = Math.Min(lvl.Width, res.Width);
            height = Math.Min(lvl.Height, res.Height);
            length = Math.Min(lvl.Length, res.Length);
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < length; z++)
                {
                    int srcI = lvl.Width * (z + y * lvl.Length);
                    int dstI = res.Width * (z + y * res.Length);
                    Buffer.BlockCopy(src, srcI, dst, dstI, width);
                }
            }

            // Copy extended blocks in bulk
            width = Math.Min(lvl.ChunksX, res.ChunksX);
            height = Math.Min(lvl.ChunksY, res.ChunksY);
            length = Math.Min(lvl.ChunksZ, res.ChunksZ);
            for (int cy = 0; cy < height; cy++)
                for (int cz = 0; cz < length; cz++)
                    for (int cx = 0; cx < width; cx++)
                    {
                        src = lvl.CustomBlocks[(cy * lvl.ChunksZ + cz) * lvl.ChunksX + cx];
                        if (src == null) continue;

                        dst = new byte[16 * 16 * 16];
                        res.CustomBlocks[(cy * res.ChunksZ + cz) * res.ChunksX + cx] = dst;
                        Buffer.BlockCopy(src, 0, dst, 0, 16 * 16 * 16);
                    }

            // TODO: This copying is really ugly and probably not 100% right
            res.spawnx = lvl.spawnx;
            res.spawny = lvl.spawny;
            res.spawnz = lvl.spawnz;
            res.rotx = lvl.rotx;
            res.roty = lvl.roty;

            lock (lvl.saveLock)
            {
                lvl.Backup(true);

                // Make sure zones are kept
                res.Zones = lvl.Zones;
                lvl.Zones = new VolatileArray<Zone>();

                IMapExporter.Encode(LevelInfo.MapPath(lvl.name), res);
                lvl.SaveChanges = false;
            }

            res.ChangedSinceBackup = false;
            Level.LoadMetadata(res);
            BotsFile.Load(res);

            Replace(lvl, res);
            lvl = res;
        }
    }
}
