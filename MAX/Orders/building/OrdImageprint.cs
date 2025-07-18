/*
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
using MAX.Drawing;
using MAX.Drawing.Ops;
using MAX.Maths;
using MAX.Network;
using MAX.Util;
using System;
using System.IO;
using System.Threading;


namespace MAX.Orders.Building
{
    public class OrdImageprint : Order
    {
        public override string Name { get { return "ImagePrint"; } }
        public override string Shortcut { get { return "Img"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("ImgPrint"), new OrderDesignation("PrintImg"),
                    new OrderDesignation("ImgDraw"), new OrderDesignation("DrawImg"),
                    new OrderDesignation("DrawImage"), new OrderDesignation("PrintImage") };
            }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (!Directory.Exists("extra/images/"))
                Directory.CreateDirectory("extra/images/");
            if (message.Length == 0) { Help(p); return; }
            string[] parts = message.SplitSpaces(5);

            DrawArgs dArgs = new DrawArgs
            {
                Pal = ImagePalette.Find("color")
            };
            if (dArgs.Pal == null) dArgs.Pal = ImagePalette.Palettes[0];

            if (parts.Length > 1)
            {
                dArgs.Pal = ImagePalette.Find(parts[1]);
                if (dArgs.Pal == null)
                {
                    p.Message("Palette {0} not found.", parts[1]); return;
                }

                if (dArgs.Pal.Entries == null || dArgs.Pal.Entries.Length == 0)
                {
                    p.Message("Palette {0} does not have any entries", dArgs.Pal.Name);
                    p.Message("Use &T/Palette &Sto add entries to it"); return;
                }
            }

            if (parts.Length > 2)
            {
                string mode = parts[2];
                if (!ParseMode(mode, dArgs)) { p.Message("&WUnknown print mode \"{0}\".", mode); return; }
            }

            if (parts.Length > 4)
            {
                if (!OrderParser.GetInt(p, parts[3], "Width", ref dArgs.Width, 1, 1024)) return;
                if (!OrderParser.GetInt(p, parts[4], "Height", ref dArgs.Height, 1, 1024)) return;
            }

            if (parts[0].IndexOf('.') != -1)
            {
                dArgs.Data = HttpUtil.DownloadImage(parts[0], p);
                if (dArgs.Data == null) return;
            }
            else
            {
                string path = "extra/images/" + parts[0] + ".bmp";
                if (!File.Exists(path)) { p.Message("{0} does not exist", path); return; }
                dArgs.Data = File.ReadAllBytes(path);
            }

            p.Message("Place or break two blocks to determine direction.");
            p.MakeSelection(2, "Selecting direction for &SImagePrint", dArgs, DoImage);
        }

        public bool ParseMode(string mode, DrawArgs args)
        {
            // Dithered and 2 layer mode are mutually exclusive because dithering is not visually effective when the (dark) sides of blocks are visible all over the image.
            if (mode.CaselessEq("wall"))
            {
                // default arguments are fine
            }
            else if (mode.CaselessEq("walldither"))
            {
                args.Dithered = true;
            }
            else if (mode.CaselessEq("wall2layer") || mode.CaselessEq("vertical2layer"))
            {
                args.TwoLayer = true;
            }
            else if (mode.CaselessEq("floor") || mode.CaselessEq("horizontal"))
            {
                args.Floor = true;
            }
            else if (mode.CaselessEq("floordither"))
            {
                args.Floor = true; args.Dithered = true;
            }
            else { return false; }

            return true;
        }

        public bool DoImage(Player p, Vec3S32[] m, object state, ushort block)
        {
            if (m[0].X == m[1].X && m[0].Z == m[1].Z) { p.Message("No direction was selected"); return false; }

            Thread thread = new Thread(() => DoDrawImage(p, m, (DrawArgs)state))
            {
                Name = "ImagePrint"
            };
            thread.Start();
            return false;
        }

        public void DoDrawImage(Player p, Vec3S32[] m, DrawArgs dArgs)
        {
            try
            {
                DoDrawImageCore(p, m, dArgs);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error drawing image", ex);
                // Do not want it taking down the whole server if error occurs
            }
        }

        public void DoDrawImageCore(Player p, Vec3S32[] marks, DrawArgs dArgs)
        {
            IBitmap2D bmp = ImageUtils.DecodeImage(dArgs.Data, p);
            if (bmp == null) return;

            ImagePrintDrawOp op = dArgs.Dithered ? new ImagePrintDitheredDrawOp() : new ImagePrintDrawOp();
            op.LayerMode = dArgs.Floor; op.DualLayer = dArgs.TwoLayer;
            op.CalcState(marks);

            int width = dArgs.Width == 0 ? bmp.Width : dArgs.Width;
            int height = dArgs.Height == 0 ? bmp.Height : dArgs.Height;
            Clamp(p, marks, op, ref width, ref height);

            if (width < bmp.Width || height < bmp.Height)
            {
                bmp.Resize(width, height, true);
            }

            op.Source = bmp; op.Palette = dArgs.Pal;
            DrawOpPerformer.Do(op, null, p, marks, false);
        }

        public void Clamp(Player p, Vec3S32[] m, ImagePrintDrawOp op, ref int width, ref int height)
        {
            Level lvl = p.level;
            Vec3S32 xEnd = m[0] + op.dx * (width - 1);
            Vec3S32 yEnd = m[0] + op.dy * (height - 1);
            if (lvl.IsValidPos(xEnd.X, xEnd.Y, xEnd.Z) && lvl.IsValidPos(yEnd.X, yEnd.Y, yEnd.Z)) return;

            int resizedWidth = width - LargestDelta(lvl, xEnd);
            int resizedHeight = height - LargestDelta(lvl, yEnd);

            // Preserve aspect ratio of image
            float ratio = Math.Min(resizedWidth / (float)width, resizedHeight / (float)height);
            resizedWidth = Math.Max(1, (int)(width * ratio));
            resizedHeight = Math.Max(1, (int)(height * ratio));

            p.Message("&WImage is too large ({0}x{1}), resizing to ({2}x{3})",
                      width, height, resizedWidth, resizedHeight);
            width = resizedWidth; height = resizedHeight;
        }

        public static int LargestDelta(Level lvl, Vec3S32 point)
        {
            Vec3S32 clamped = lvl.ClampPos(point);
            int dx = Math.Abs(point.X - clamped.X);
            int dy = Math.Abs(point.Y - clamped.Y);
            int dz = Math.Abs(point.Z - clamped.Z);
            return Math.Max(dx, Math.Max(dy, dz));
        }

        public override void Help(Player p)
        {
            p.Message("&T/ImagePrint [file/url] [palette] <mode> <width height>");
            p.Message("&HPrints image from given URL, or from a .bmp file in /extra/images/ folder");
            p.Message("&HPalettes: &f{0}", ImagePalette.Palettes.Join(pal => pal.Name));
            p.Message("&HModes: &fWall, WallDither, Wall2Layer, Floor, FloorDither");
            p.Message("&H  <width height> optionally resize the printed image");
        }

        public class DrawArgs
        {
            public bool Floor, TwoLayer, Dithered;
            public ImagePalette Pal;
            public byte[] Data;
            public int Width, Height;
        }
    }
}