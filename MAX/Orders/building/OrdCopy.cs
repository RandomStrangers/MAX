/*
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
using MAX.Drawing;
using MAX.Drawing.Brushes;
using MAX.Drawing.Ops;
using MAX.Maths;
using System.IO;
using System.IO.Compression;


namespace MAX.Orders.Building
{
    public class OrdCopy : Order
    {
        public override string Name { get { return "Copy"; } }
        public override string Shortcut { get { return "c"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("Cut", "cut") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            int offsetIndex = message.IndexOf('@');
            if (offsetIndex != -1)
                message = message.Replace("@ ", "").Replace("@", "");

            string[] parts = message.SplitSpaces();
            string opt = parts[0].ToLower();

            if (opt == "save")
            {
                if (parts.Length != 2) { Help(p); return; }
                if (!Formatter.ValidFilename(p, parts[1])) return;

                SaveCopy(p, parts[1]);
            }
            else if (opt == "load")
            {
                if (parts.Length != 2) { Help(p); return; }
                if (!Formatter.ValidFilename(p, parts[1])) return;

                LoadCopy(p, parts[1]);
            }
            else if (IsDeleteOrder(opt))
            {
                if (parts.Length != 2) { Help(p); return; }
                if (!Formatter.ValidFilename(p, parts[1])) return;

                string path = FindCopy(p.name, parts[1]);
                if (path == null) { p.Message("No such copy exists."); return; }
                FileIO.TryDelete(path);
                p.Message("Deleted copy " + parts[1]);
            }
            else if (IsListOrder(opt))
            {
                string dir = "extra/savecopy/" + p.name;
                if (!Directory.Exists(dir))
                {
                    p.Message("You have no saved copies"); return;
                }

                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    p.Message(Path.GetFileNameWithoutExtension(files[i]));
                }
            }
            else
            {
                HandleOther(p, parts, offsetIndex);
            }
        }

        public void HandleOther(Player p, string[] parts, int offsetIndex)
        {
            CopyArgs cArgs = new CopyArgs
            {
                offsetIndex = offsetIndex
            };

            for (int i = 0; i < parts.Length; i++)
            {
                string opt = parts[i];
                if (opt.CaselessEq("cut"))
                {
                    cArgs.cut = true;
                }
                else if (opt.CaselessEq("air"))
                {
                    cArgs.air = true;
                }
                else if (opt.Length > 0)
                {
                    Help(p); return;
                }
            }

            p.Message("Place or break two blocks to determine the edges.");
            int marks = cArgs.offsetIndex != -1 ? 3 : 2;
            p.MakeSelection(marks, "Selecting region for &SCopy", cArgs, DoCopy, DoCopyMark);
        }

        public void CompleteCopy(Player p, Vec3S32[] m, CopyArgs cArgs)
        {
            if (!cArgs.cut) return;
            DrawOp op = new CuboidDrawOp
            {
                Flags = BlockDBFlags.Cut,
                AffectedByTransform = false
            };
            Brush brush = new SolidBrush(Block.Air);
            DrawOpPerformer.Do(op, brush, p, new Vec3S32[] { m[0], m[1] }, false);
        }

        public void DoCopyMark(Player p, Vec3S32[] m, int i, object state, ushort block)
        {
            CopyArgs cArgs = (CopyArgs)state;
            if (i == 2)
            {
                CopyState copy = p.CurrentCopy;
                copy.Offset.X = copy.OriginX - m[i].X;
                copy.Offset.Y = copy.OriginY - m[i].Y;
                copy.Offset.Z = copy.OriginZ - m[i].Z;

                p.Message("Set offset of where to paste from.");
                CompleteCopy(p, m, cArgs);
                return;
            }
            if (i != 1) return;

            Vec3S32 min = Vec3S32.Min(m[0], m[1]), max = Vec3S32.Max(m[0], m[1]);
            ushort minX = (ushort)min.X, minY = (ushort)min.Y, minZ = (ushort)min.Z;
            ushort maxX = (ushort)max.X, maxY = (ushort)max.Y, maxZ = (ushort)max.Z;

            CopyState cState = new CopyState(minX, minY, minZ, maxX - minX + 1,
                                             maxY - minY + 1, maxZ - minZ + 1)
            {
                OriginX = m[0].X,
                OriginY = m[0].Y,
                OriginZ = m[0].Z
            };

            int index = 0; cState.UsedBlocks = 0;
            cState.PasteAir = cArgs.air;

            for (ushort y = minY; y <= maxY; ++y)
                for (ushort z = minZ; z <= maxZ; ++z)
                    for (ushort x = minX; x <= maxX; ++x)
                    {
                        block = p.level.GetBlock(x, y, z);
                        if (!p.group.Blocks[block]) { index++; continue; }

                        if (block != Block.Air || cState.PasteAir) cState.UsedBlocks++;
                        cState.Set(block, index);
                        index++;
                    }

            if (cState.UsedBlocks > p.group.DrawLimit)
            {
                p.Message("You tried to copy {0} blocks. You cannot copy more than {1} blocks.",
                          cState.UsedBlocks, p.group.DrawLimit);
                cState.Clear();
                p.ClearSelection();
                return;
            }

            cState.CopySource = "level " + p.level.name;
            p.CurrentCopy = cState;

            p.Message("Copied &a{0} &Sblocks, origin at ({1}, {2}, {3}) corner", cState.UsedBlocks,
                      cState.OriginX == cState.X ? "Min" : "MAX",
                      cState.OriginY == cState.Y ? "Min" : "MAX",
                      cState.OriginZ == cState.Z ? "Min" : "MAX");
            if (!cState.PasteAir) p.Message("To also copy air blocks, use &T/Copy Air");

            if (cArgs.offsetIndex != -1)
            {
                p.Message("Place a block to determine where to paste from");
            }
            else
            {
                CompleteCopy(p, m, cArgs);
            }
        }

        public bool DoCopy(Player p, Vec3S32[] m, object state, ushort block) { return false; }
        public class CopyArgs { public int offsetIndex; public bool cut, air; }

        public void SaveCopy(Player p, string file)
        {
            if (!Directory.Exists("extra/savecopy"))
                Directory.CreateDirectory("extra/savecopy");
            if (!Directory.Exists("extra/savecopy/" + p.name))
                Directory.CreateDirectory("extra/savecopy/" + p.name);
            if (Directory.GetFiles("extra/savecopy/" + p.name).Length > 15)
            {
                p.Message("You can only save a maxmium of 15 copies. /copy delete some.");
                return;
            }

            CopyState cState = p.CurrentCopy;
            if (cState == null)
            {
                p.Message("You haven't copied anything yet"); return;
            }

            string path = "extra/savecopy/" + p.name + "/" + file + ".cpb";
            using (FileStream fs = File.Create(path))
            using (GZipStream gs = new GZipStream(fs, CompressionMode.Compress))
            {
                cState.SaveTo(gs);
            }
            p.Message("Saved copy as " + file);
        }

        public void LoadCopy(Player p, string file)
        {
            string path = FindCopy(p.name, file);
            if (path == null) { p.Message("No such copy exists"); return; }

            using (FileStream fs = File.OpenRead(path))
            using (GZipStream gs = new GZipStream(fs, CompressionMode.Decompress))
            {
                CopyState state = new CopyState(0, 0, 0, 0, 0, 0);
                if (path.CaselessEnds(".cpb"))
                {
                    state.LoadFrom(gs);
                }
                else
                {
                    state.LoadFromOld(fs);
                }

                state.CopySource = "file " + file;
                p.CurrentCopy = state;
            }
            p.Message("Loaded copy from " + file);
        }

        public static string FindCopy(string player, string file)
        {
            string path = "extra/savecopy/" + player + "/" + file;
            bool existsNew = File.Exists(path + ".cpb");
            bool existsOld = File.Exists(path + ".cpy");

            if (!existsNew && !existsOld) return null;
            string ext = existsNew ? ".cpb" : ".cpy";
            return path + ext;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Copy &H- Copies the blocks in an area.");
            p.Message("&T/Copy save [name] &H- Saves what you have copied.");
            p.Message("&T/Copy load [name] &H- Loads what you have saved.");
            p.Message("&T/Copy delete [name] &H- Deletes the specified copy.");
            p.Message("&T/Copy list &H- Lists all saved copies you have");
            p.Message("&T/Copy cut &H- Copies the blocks in an area, then removes them.");
            p.Message("&T/Copy air &H- Copies the blocks in an area, including air.");
            p.Message("/Copy @ - @ toggle for all the above, gives you a third click after copying that determines where to paste from");
        }
    }
}