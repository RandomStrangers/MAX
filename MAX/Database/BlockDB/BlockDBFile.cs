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
using MAX.Maths;
using MAX.Util;
using System;
using System.IO;

namespace MAX.DB
{
    public unsafe class BlockDBFile
    {
        public const byte Version = 1;
        public const int EntrySize = 16;
        public const int HeaderEntries = 1;
        public const int BulkEntries = 4096;

        public static BlockDBFile V1 = new BlockDBFile_V1();

        public static string FilePath(string map) { return "blockdb/" + map + ".cbdb"; }
        public static string DumpPath(string map) { return "blockdb/" + map + ".dump"; }
        public static string TempPath(string map) { return "blockdb/" + map + ".temp"; }

        public static void WriteHeader(Stream s, Vec3U16 dims)
        {
            byte[] header = new byte[EntrySize * HeaderEntries * 4];
            NetUtils.Write("CBDB_MAX", header, 0, false);
            WriteU16(Version, header, 8);
            WriteU16(dims.X, header, 10);
            WriteU16(dims.Y, header, 12);
            WriteU16(dims.Z, header, 14);
            s.Write(header, 0, EntrySize);
        }

        public static BlockDBFile ReadHeader(Stream s, out Vec3U16 dims)
        {
            dims = default;
            byte[] header = new byte[EntrySize * HeaderEntries];
            ReadFully(s, header, 0, header.Length);

            // Check constants are expected
            // TODO: check 8 byte string identifier
            ushort fileVersion = ReadU16(header, 8);
            if (fileVersion != Version)
                throw new NotSupportedException("only version 1 is supported");

            dims.X = ReadU16(header, 10);
            dims.Y = ReadU16(header, 12);
            dims.Z = ReadU16(header, 14);
            return V1;
        }


        public virtual void WriteEntries(Stream s, FastList<BlockDBEntry> entries)
        {
        }

        public virtual void WriteEntries(Stream s, BlockDBCache cache)
        {
        }

        /// <summary> Reads a block of BlockDB entries, in a forward streaming manner. </summary>
        /// <returns> The number of entries read. </returns>
        public virtual int ReadForward(Stream s, byte[] bulk, BlockDBEntry* entryPtr)
        {
            return 0;
        }

        /// <summary> Reads a block of BlockDB entries, in a backward streaming manner. </summary>
        /// <returns> The number of entries read. </returns>
        public virtual int ReadBackward(Stream s, byte[] bulk, BlockDBEntry* entryPtr)
        {
            return 0;
        }

        /// <summary> Returns number of entries in the backing file. </summary>
        public virtual long CountEntries(Stream s)
        {
            return 0;
        }

        /// <summary> Deletes the backing file on disc if it exists. </summary>
        public static void DeleteBackingFile(string map)
        {
            string path = FilePath(map);
            if (!File.Exists(path)) return;
            FileIO.TryDelete(path);
        }

        /// <summary> Moves the backing file on disc if it exists. </summary>
        public static void MoveBackingFile(string srcMap, string dstMap)
        {
            string srcPath = FilePath(srcMap), dstPath = FilePath(dstMap);
            if (!File.Exists(srcPath)) return;
            if (File.Exists(dstPath)) FileIO.TryDelete(dstPath);
            FileIO.TryMove(srcPath, dstPath);
        }

        public static void ResizeBackingFile(BlockDB db)
        {
            Logger.Log(LogType.BackgroundActivity, "Resizing BlockDB for " + db.MapName);
            string filePath = FilePath(db.MapName);
            string tempPath = TempPath(db.MapName);

            using (Stream src = File.OpenRead(filePath), dst = File.Create(tempPath))
            {
                ReadHeader(src, out Vec3U16 dims);
                WriteHeader(dst, db.Dims);
                int width = db.Dims.X, length = db.Dims.Z;
                byte[] bulk = new byte[BulkEntries * EntrySize];

                fixed (byte* ptr = bulk)
                {
                    BlockDBEntry* entryPtr = (BlockDBEntry*)ptr;
                    while (true)
                    {
                        int count = V1.ReadForward(src, bulk, entryPtr);
                        if (count == 0) break;

                        for (int i = 0; i < count; i++)
                        {
                            int index = entryPtr[i].Index;
                            int x = index % dims.X;
                            int y = index / dims.X / dims.Z;
                            int z = index / dims.X % dims.Z;
                            entryPtr[i].Index = (y * length + z) * width + x;
                        }
                        dst.Write(bulk, 0, count * EntrySize);
                    }
                }
            }

            FileIO.TryDelete(filePath);
            FileIO.TryMove(tempPath, filePath);
        }


        /// <summary> Returns number of entries in the backing file on disc if it exists. </summary>
        public static long CountEntries(string map)
        {
            string path = FilePath(map);
            if (!File.Exists(path)) return 0;

            using (Stream src = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                BlockDBFile file = ReadHeader(src, out Vec3U16 dims);
                return file.CountEntries(src);
            }
        }

        /// <summary> Iterates from the very oldest to newest entry in the BlockDB. </summary>
        public void FindChangesAt(Stream s, int index, Action<BlockDBEntry> output)
        {
            byte[] bulk = new byte[BulkEntries * EntrySize];
            fixed (byte* ptr = bulk)
            {
                while (true)
                {
                    BlockDBEntry* entryPtr = (BlockDBEntry*)ptr;
                    int count = ReadForward(s, bulk, entryPtr);
                    if (count == 0) return;

                    for (int i = 0; i < count; i++)
                    {
                        if (entryPtr->Index == index) { output(*entryPtr); }
                        entryPtr++;
                    }
                }
            }
        }

        /// <summary> Iterates from the very newest to oldest entry in the BlockDB. </summary>
        /// <returns> whether an entry before start time was reached. </returns>
        public bool FindChangesBy(Stream s, int[] ids, int start, int end, Action<BlockDBEntry> output)
        {
            byte[] bulk = new byte[BulkEntries * EntrySize];
            s.Position = s.Length;
            fixed (byte* ptr = bulk)
            {
                while (true)
                {
                    BlockDBEntry* entryPtr = (BlockDBEntry*)ptr;
                    int count = ReadBackward(s, bulk, entryPtr);
                    if (count == 0) break;
                    entryPtr += count - 1;

                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (entryPtr->TimeDelta < start) return true;
                        if (entryPtr->TimeDelta <= end)
                        {
                            for (int j = 0; j < ids.Length; j++)
                            {
                                if (entryPtr->PlayerID != ids[j]) continue;
                                output(*entryPtr); break;
                            }
                        }
                        entryPtr--;
                    }
                }
            }
            return false;
        }


        public static ushort ReadU16(byte[] array, int offset)
        {
            return (ushort)(array[offset] | array[offset + 1] << 8);
        }

        public static void WriteU16(ushort value, byte[] array, int index)
        {
            array[index++] = (byte)value;
            array[index++] = (byte)(value >> 8);
        }

        public static void ReadFully(Stream stream, byte[] dst, int offset, int count)
        {
            int total = 0;
            do
            {
                int read = stream.Read(dst, offset + total, count - total);
                if (read == 0) throw new EndOfStreamException();
                total += read;
            } while (total < count);
        }
    }
}