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
using MAX.Levels.IO;
using MAX.Maths;
using MAX.SQL;
using MAX.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX.DB
{
    /// <summary> Exports a BlockDB table to the new binary format. </summary>
    public class BlockDBTableDumper
    {
        public string mapName;
        public Dictionary<string, int> nameCache = new Dictionary<string, int>();
        public Stream stream;
        public bool errorOccurred;
        public Vec3U16 dims;
        public BlockDBEntry entry;
        public FastList<BlockDBEntry> buffer = new FastList<BlockDBEntry>(4096);
        public uint entriesWritten;

        public void DumpTable(string table)
        {
            buffer.Count = 0;
            entriesWritten = 0;
            errorOccurred = false;
            mapName = table.Substring("Block".Length);

            try
            {
                Database.ReadRows(table, "*", DumpRow);
                WriteBuffer(true);
                AppendCbdbFile();
                SaveCbdbFile();
            }
            finally
            {
                stream?.Close();
                stream = null;
            }

            if (errorOccurred) return;
            Database.DeleteTable(table);
        }

        public void DumpRow(ISqlRecord record)
        {
            if (errorOccurred) return;

            try
            {
                if (stream == null)
                {
                    stream = File.Create(BlockDBFile.DumpPath(mapName));
                    string lvlPath = LevelInfo.MapPath(mapName);
                    dims = IMapImporter.GetFor(lvlPath).ReadDimensions(lvlPath);
                    BlockDBFile.WriteHeader(stream, dims);
                }

                // Only log maps which have a used BlockDB to avoid spam
                entriesWritten++;
                if (entriesWritten == 10)
                {
                    string progress = " (" + DBUpgrader.Progress + ")";
                    Logger.Log(LogType.SystemActivity, "Dumping BlockDB for " + mapName + progress);
                }

                UpdateBlock(record);
                UpdateCoords(record);
                UpdatePlayerID(record);
                UpdateTimestamp(record);

                buffer.Add(entry);
                WriteBuffer(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errorOccurred = true;
            }
        }

        public void WriteBuffer(bool force)
        {
            if (buffer.Count == 0) return;
            if (!force && buffer.Count < 4096) return;

            BlockDBFile.V1.WriteEntries(stream, buffer);
            buffer.Count = 0;
        }

        public void AppendCbdbFile()
        {
            string path = BlockDBFile.FilePath(mapName);
            if (!File.Exists(path) || stream == null) return;

            byte[] bulk = new byte[4096];
            using (Stream cbdb = File.OpenRead(path))
            {
                cbdb.Read(bulk, 0, BlockDBFile.EntrySize); // header
                int read = 0;
                while ((read = cbdb.Read(bulk, 0, 4096)) > 0)
                {
                    stream.Write(bulk, 0, read);
                }
            }
        }

        public void SaveCbdbFile()
        {
            if (stream == null) return;
            stream.Close();
            stream = null;

            string dumpPath = BlockDBFile.DumpPath(mapName);
            string filePath = BlockDBFile.FilePath(mapName);
            if (File.Exists(filePath)) FileIO.TryDelete(filePath);
            FileIO.TryMove(dumpPath, filePath);
        }


        public void UpdateBlock(ISqlRecord record)
        {
            entry.OldRaw = Block.Invalid;
            entry.NewRaw = (byte)record.GetInt32(5);
            byte blockFlags = (byte)record.GetInt32(6);
            entry.Flags = BlockDBFlags.ManualPlace;

            if ((blockFlags & 1) != 0)
            { // deleted block
                entry.NewRaw = Block.Air;
            }
            if ((blockFlags & 2) != 0)
            { // new block is custom
                entry.Flags |= BlockDBFlags.NewExtended;
            }
        }

        public void UpdateCoords(ISqlRecord record)
        {
            int x = record.GetInt32(2);
            int y = record.GetInt32(3);
            int z = record.GetInt32(4);
            entry.Index = x + dims.X * (z + dims.Z * y);
        }

        public void UpdatePlayerID(ISqlRecord record)
        {
            string user = record.GetString(0);
            if (!nameCache.TryGetValue(user, out int id))
            {
                int[] ids = NameConverter.FindIds(user);
                if (ids.Length > 0)
                {
                    nameCache[user] = ids[0];
                }
                else
                {
                    nameCache[user] = NameConverter.InvalidNameID(user);
                }
            }
            entry.PlayerID = id;
        }

        public void UpdateTimestamp(ISqlRecord record)
        {
            DateTime time = record.GetDateTime(1).ToUniversalTime();
            entry.TimeDelta = (int)time.Subtract(BlockDB.Epoch).TotalSeconds;
        }
    }
}