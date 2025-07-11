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
using MAX.Blocks;
using MAX.Blocks.Extended;
using MAX.Maths;
using MAX.Util;
using System.Collections.Generic;


namespace MAX.Orders.Building
{
    public class OrdPortal : Order
    {
        public override string Name { get { return "Portal"; } }
        public override string Shortcut { get { return "o"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            PortalArgs pArgs = new PortalArgs
            {
                Multi = false
            };
            string[] args = message.SplitSpaces();
            string block = message.Length == 0 ? "" : args[0].ToLower();

            if (args.Length >= 2 && args[1].CaselessEq("multi"))
            {
                pArgs.Multi = true;
            }
            else if (args.Length >= 2)
            {
                Help(p); return;
            }

            pArgs.Block = GetBlock(p, block);
            if (pArgs.Block == Block.Invalid) return;
            if (!OrderParser.IsBlockAllowed(p, "place a portal of", pArgs.Block)) return;
            pArgs.Entries = new List<PortalPos>();

            p.Message("Place an &aEntry block &Sfor the portal");
            p.ClearBlockchange();
            p.blockchangeObject = pArgs;
            p.Blockchange += EntryChange;
        }

        public ushort GetBlock(Player p, string name)
        {
            if (name.CaselessEq("show")) { ShowPortals(p); return Block.Invalid; }
            ushort block = Block.Parse(p, name);
            if (block != Block.Invalid && p.level.Props[block].IsPortal) return block;

            // Hardcoded designations for backwards compatibility
            block = Block.Invalid;
            if (name.Length == 0) block = Block.Portal_Blue;
            if (name.CaselessEq("blue")) block = Block.Portal_Blue;
            if (name.CaselessEq("orange")) block = Block.Portal_Orange;
            if (name.CaselessEq("air")) block = Block.Portal_Air;
            if (name.CaselessEq("water")) block = Block.Portal_Water;
            if (name.CaselessEq("lava")) block = Block.Portal_Lava;

            if (p.level.Props[block].IsPortal) return block;
            Help(p); return Block.Invalid;
        }

        public void EntryChange(Player p, ushort x, ushort y, ushort z, ushort block)
        {
            PortalArgs args = (PortalArgs)p.blockchangeObject;
            ushort old = p.level.GetBlock(x, y, z);
            if (!p.level.CheckAffect(p, x, y, z, old, args.Block))
            {
                p.RevertBlock(x, y, z); return;
            }
            p.ClearBlockchange();

            if (args.Multi && block == Block.Red && args.Entries.Count > 0)
            {
                ExitChange(p, x, y, z, block); return;
            }

            p.level.UpdateBlock(p, x, y, z, args.Block);
            p.SendBlockchange(x, y, z, Block.Green);
            PortalPos Port;

            Port.Map = p.level.name;
            Port.x = x; Port.y = y; Port.z = z;
            args.Entries.Add(Port);
            p.blockchangeObject = args;

            if (!args.Multi)
            {
                p.Blockchange += ExitChange;
                p.Message("&aEntry block placed");
            }
            else
            {
                p.Blockchange += EntryChange;
                p.Message("&aEntry block placed. &c{0} block for exit",
                              Block.GetName(p, Block.Red));
            }
        }

        public void ExitChange(Player p, ushort x, ushort y, ushort z, ushort block)
        {
            p.ClearBlockchange();
            p.RevertBlock(x, y, z);

            PortalArgs args = (PortalArgs)p.blockchangeObject;
            string exitMap = p.level.name;

            foreach (PortalPos P in args.Entries)
            {
                string map = P.Map;
                if (map == p.level.name) p.RevertBlock(P.x, P.y, P.z);
                object locker = ThreadSafeCache.DBCache.GetLocker(map);

                lock (locker)
                {
                    Portal.Set(map, P.x, P.y, P.z, x, y, z, exitMap);
                }
            }

            p.Message("&3Exit &Sblock placed");
            if (!p.staticOrders) return;
            args.Entries.Clear();
            p.blockchangeObject = args;
            p.Blockchange += EntryChange;
        }

        public class PortalArgs { public List<PortalPos> Entries; public ushort Block; public bool Multi; }
        public struct PortalPos { public ushort x, y, z; public string Map; }


        public static void ShowPortals(Player p)
        {
            p.showPortals = !p.showPortals;
            List<Vec3U16> coords = Portal.GetAllCoords(p.level.MapName);

            foreach (Vec3U16 pos in coords)
            {
                if (p.showPortals)
                {
                    p.SendBlockchange(pos.X, pos.Y, pos.Z, Block.Green);
                }
                else
                {
                    p.RevertBlock(pos.X, pos.Y, pos.Z);
                }
            }

            List<PortalExit> exits = Portal.GetAllExits(p.level.MapName);
            foreach (PortalExit exit in exits)
            {
                if (exit.Map != p.level.MapName) continue;

                if (p.showPortals)
                {
                    p.SendBlockchange(exit.X, exit.Y, exit.Z, Block.Red);
                }
                else
                {
                    p.RevertBlock(exit.X, exit.Y, exit.Z);
                }
            }

            p.Message("Now {0} &Sportals.",
                           p.showPortals ? "showing &a" + coords.Count : "hiding");
        }


        public static string Format(ushort block, Player p, BlockProps[] props)
        {
            if (!props[block].IsPortal) return null;

            // We want to use the simple designations if possible
            if (block == Block.Portal_Orange) return "orange";
            if (block == Block.Portal_Blue) return "blue";
            if (block == Block.Portal_Air) return "air";
            if (block == Block.Portal_Lava) return "lava";
            if (block == Block.Portal_Water) return "water";
            return Block.GetName(p, block);
        }

        public static List<string> SupportedBlocks(Player p)
        {
            List<string> names = new List<string>();
            BlockProps[] props = p.IsSuper ? Block.Props : p.level.Props;

            for (int i = 0; i < props.Length; i++)
            {
                string name = Format((ushort)i, p, props);
                if (name != null) names.Add(name);
            }
            return names;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Portal [block]");
            p.Message("&HPlace a block for the entry, then another block for exit.");
            p.Message("&T/Portal [block] multi");
            p.Message("&HPlace multiple blocks for entries, then a {0} block for exit.", Block.GetName(p, Block.Red));
            p.Message("&H  Note: The exit can be on a different level.");
            List<string> names = SupportedBlocks(p);
            p.Message("&H  Supported blocks: &S{0}", names.Join());
            p.Message("&T/Portal show &H- Shows portals (green = entry, red = exit)");
        }
    }
}