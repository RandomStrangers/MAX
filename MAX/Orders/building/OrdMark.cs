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
using MAX.Blocks;
using MAX.Maths;


namespace MAX.Orders.Building
{
    public class OrdMark : Order
    {
        public override string Name { get { return "Mark"; } }
        public override string Shortcut { get { return "click"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("m"), new OrderDesignation("x"),
                    new OrderDesignation("MarkAll", "all"), new OrderDesignation("ma", "all") };
            }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.CaselessEq("all"))
            {
                if (!DoMark(p, 0, 0, 0))
                {
                    p.Message("Cannot mark, no selection in progress.");
                }
                else
                {
                    Level lvl = p.level;
                    DoMark(p, lvl.MaxX, lvl.MaxY, lvl.MaxZ);
                }
                return;
            }


            Vec3S32 P = p.Pos.BlockCoords;
            P.Y = (p.Pos.Y - 32) / 32;
            if (message.Length > 0 && !ParseCoords(message, p, ref P)) return;

            P = p.level.ClampPos(P);
            if (DoMark(p, P.X, P.Y, P.Z)) return;

            Vec3U16 mark = (Vec3U16)P;
            // We only want to activate blocks in the world
            ushort old = p.level.GetBlock(mark.X, mark.Y, mark.Z);
            if (!p.CheckManualChange(old, true)) return;

            HandleDelete handler = p.level.DeleteHandlers[old];
            if (handler != null)
            {
                handler(p, old, mark.X, mark.Y, mark.Z);
            }
            else
            {
                p.Message("Cannot mark, no selection in progress, " +
                               "nor could the existing block at the coordinates be activated."); return;
            }
        }

        public bool ParseCoords(string message, Player p, ref Vec3S32 P)
        {
            string[] args = message.SplitSpaces();
            // Expand /mark ~4 into /mark ~4 ~4 ~4
            if (args.Length == 1)
            {
                args = new string[] { message, message, message };
            }
            if (args.Length != 3) { Help(p); return false; }

            AdjustArg(ref args[0], ref P.X, "X", p.lastClick.X);
            AdjustArg(ref args[1], ref P.Y, "Y", p.lastClick.Y);
            AdjustArg(ref args[2], ref P.Z, "Z", p.lastClick.Z);

            return OrderParser.GetCoords(p, args, 0, ref P);
        }

        public void AdjustArg(ref string arg, ref int value, string axis, int last)
        {
            if (!arg.CaselessStarts(axis)) return;

            if (arg.Length == 1)
            {
                // just 'X' changes input to coordinate of last click
                arg = last.ToString();
            }
            else
            {
                // assume wanting input relative to last click, e.g. 'X~3'
                arg = arg.Substring(1);
                value = last;
            }
        }

        public static bool DoMark(Player p, int x, int y, int z)
        {
            if (!p.HasBlockChange()) return false;
            if (!p.Ignores.DrawOutput)
            {
                p.Message("Mark placed at &b({0}, {1}, {2})", x, y, z);
            }

            ushort block = p.GetHeldBlock();
            p.DoBlockchangeCallback((ushort)x, (ushort)y, (ushort)z, block);
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Mark <x y z> &H- Places a marker for selections, e.g for &T/z");
            p.Message("&HUse ~ before a coordinate to mark relative to current position");
            p.Message("&HIf no coordinates are given, marks at where you are standing");
            p.Message("&HIf only x coordinate is given, it is used for y and z too");
            p.Message("  &He.g. /mark 30 y 20 will mark at (30, last y, 20)");
            p.Message("&T/Mark all &H- Places markers at min and max corners of the map");
            p.Message("&HActivates the block (e.g. door) if no selection is in progress");
        }
    }
}