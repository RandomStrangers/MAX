﻿/*
   Copyright 2015-2024 MCGalaxy

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
using System.Collections.Generic;
using System.IO;
using MAX.Maths;

namespace MAX.Levels.IO
{
    /// <summary> Reads/Loads block data (and potentially metadata) encoded in a particular format. </summary>
    public abstract class IMapImporter
    {
        public abstract string Extension { get; }
        public abstract string Description { get; }

        public virtual Level Read(string path, string name, bool metadata)
        {
            using (FileStream fs = File.OpenRead(path))
                return Read(fs, name, metadata);
        }

        public abstract Level Read(Stream src, string name, bool metadata);

        public virtual Vec3U16 ReadDimensions(string path)
        {
            using (FileStream fs = File.OpenRead(path))
                return ReadDimensions(fs);
        }

        public abstract Vec3U16 ReadDimensions(Stream src);


        public static void ConvertCustom(Level lvl)
        {
            byte[] blocks = lvl.blocks; // local var to avoid JIT bounds check
            for (int i = 0; i < blocks.Length; i++)
            {
                byte raw = blocks[i];
                if (raw <= Block.CPE_MAX_BLOCK) continue;

                blocks[i] = Block.custom_block;
                lvl.IntToPos(i, out ushort x, out ushort y, out ushort z);
                lvl.FastSetExtTile(x, y, z, raw);
            }
        }

        /// <summary> Reads the given number of bytes from the given stream </summary>
        /// <remarks> Throws EndOfStreamException if unable to read sufficient bytes </remarks>
        public static void ReadFully(Stream s, byte[] data, int count)
        {
            int offset = 0;
            while (count > 0)
            {
                int read = s.Read(data, offset, count);

                if (read == 0) throw new EndOfStreamException("End of stream reading data");
                offset += read; 
                count -= read;
            }
        }


        /// <summary> List of all level format importers </summary>
        public static List<IMapImporter> Formats = new List<IMapImporter>() 
        {
            new LvlImporter(), new CwImporter(), new FcmImporter(), new McfImporter(),
            new DatImporter(), new McLevelImporter(), new MapImporter(),
        };
        public static IMapImporter defaultImporter = new LvlImporter();
        /// <summary> Returns an IMapImporter capable of decoding the given level file </summary>
        /// <remarks> Determines importer suitability by comparing file extensions </remarks>
        /// <remarks> A suitable IMapImporter, or null if no suitable importer is found </remarks>
        public static IMapImporter GetFor(string path)
        {
            string p = path.Replace(".prev", "")
                .Replace(".backup", "");
            foreach (IMapImporter imp in Formats)
            {
                if (p.CaselessEnds(imp.Extension))
                {
                    return imp;
                }
            }
            return null;
        }
        /// <summary> Decodes the given level file into a Level instance </summary>
        public static Level Decode(string path, string name, bool metadata)
        {
            IMapImporter imp = GetFor(path);
            if (imp == null)
            {
                Logger.Log(LogType.Warning, "No importer found for {0}, cannot import level!", path);
                return null;
            }
            else
            {
                return imp.Read(path, name, metadata);
            }
        }
    }

    /// <summary> Writes/Saves block data (and potentially metadata) encoded in a particular format. </summary>
    public abstract class IMapExporter
    {
        public abstract string Extension { get; }

        public void Write(string path, Level lvl)
        {
            using (FileStream fs = File.Create(path))
            {
                Write(fs, lvl);
            }
        }

        public abstract void Write(Stream dst, Level lvl);

        public static List<IMapExporter> Formats = new List<IMapExporter>() 
        {
            new LvlExporter(), new McfExporter(), new CwExporter()
        };
        public static IMapExporter defaultExporter = new LvlExporter();

        /// <summary> Returns an IMapExporter capable of encoding the given level file </summary>
        /// <remarks> Determines exporter suitability by comparing file extensions </remarks>
        /// <remarks> A suitable IMapExporter, or null if no suitable exporter is found </remarks>
        public static IMapExporter GetFor(string path)
        {
            string p = path.Replace(".prev", "")
                .Replace(".backup", "");
            foreach (IMapExporter exp in Formats)
            {
                if (p.CaselessEnds(exp.Extension))
                {
                    return exp;
                }
            }
            return null;
        }
        public static void Encode(string path, Level lvl)
        {
            IMapExporter exp = GetFor(path);
            if (exp == null)
            {
                Logger.Log(LogType.Warning, "No exporter found for {0}, cannot save level!", path);
                return;
            }
            else
            {
                exp.Write(path, lvl);
            }
        }
    }
}
