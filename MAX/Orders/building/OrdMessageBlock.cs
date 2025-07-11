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
    public class OrdMessageBlock : Order
    {
        public override string Name { get { return "MB"; } }
        public override string Shortcut { get { return "MessageBlock"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Admin, "can use moderation orders in MBs") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }

            bool allMessage = false;
            MBArgs mbArgs = new MBArgs();
            string[] args = message.SplitSpaces(2);
            string block = args[0].ToLower();

            mbArgs.Block = GetBlock(p, block, ref allMessage);
            if (mbArgs.Block == Block.Invalid) return;
            if (!OrderParser.IsBlockAllowed(p, "place a message block of", mbArgs.Block)) return;

            if (allMessage)
            {
                mbArgs.Message = message;
            }
            else if (args.Length == 1)
            {
                p.Message("You need to provide text to put in the messageblock."); return;
            }
            else
            {
                mbArgs.Message = args[1];
            }

            bool allOrds = HasExtraPerm(data.Rank, 1);
            if (!MessageBlock.Validate(p, mbArgs.Message, allOrds)) return;

            p.Message("Place where you wish the message block to go.");
            p.MakeSelection(1, mbArgs, PlacedMark);
        }

        public ushort GetBlock(Player p, string name, ref bool allMessage)
        {
            if (name == "show") { ShowMessageBlocks(p); return Block.Invalid; }
            ushort block = Block.Parse(p, name);
            if (block != Block.Invalid && p.level.Props[block].IsMessageBlock)
                return block;

            // Hardcoded designations for backwards compatibility
            block = Block.MB_White;
            if (name.CaselessEq("white")) block = Block.MB_White;
            if (name.CaselessEq("black")) block = Block.MB_Black;
            if (name.CaselessEq("air")) block = Block.MB_Air;
            if (name.CaselessEq("water")) block = Block.MB_Water;
            if (name.CaselessEq("lava")) block = Block.MB_Lava;

            allMessage = block == Block.MB_White && name != "white";
            if (p.level.Props[block].IsMessageBlock) return block;

            Help(p); return Block.Invalid;
        }

        public bool PlacedMark(Player p, Vec3S32[] marks, object state, ushort block)
        {
            ushort x = (ushort)marks[0].X, y = (ushort)marks[0].Y, z = (ushort)marks[0].Z;
            MBArgs args = (MBArgs)state;

            ushort old = p.level.GetBlock(x, y, z);
            if (p.level.CheckAffect(p, x, y, z, old, args.Block))
            {
                p.level.UpdateBlock(p, x, y, z, args.Block);
                UpdateDatabase(p, args, x, y, z);
                p.Message("Message block created.");
            }
            else
            {
                p.Message("Failed to create a message block.");
            }
            return true;
        }

        public void UpdateDatabase(Player p, MBArgs args, ushort x, ushort y, ushort z)
        {
            string map = p.level.name;
            object locker = ThreadSafeCache.DBCache.GetLocker(map);

            lock (locker)
            {
                MessageBlock.Set(map, x, y, z, args.Message);
            }
        }

        public class MBArgs { public string Message; public ushort Block; }


        public void ShowMessageBlocks(Player p)
        {
            p.showMBs = !p.showMBs;
            List<Vec3U16> coords = MessageBlock.GetAllCoords(p.level.MapName);

            foreach (Vec3U16 pos in coords)
            {
                if (p.showMBs)
                {
                    p.SendBlockchange(pos.X, pos.Y, pos.Z, Block.Green);
                }
                else
                {
                    p.RevertBlock(pos.X, pos.Y, pos.Z);
                }
            }

            p.Message("Now {0} &SMBs.",
                           p.showMBs ? "showing &a" + coords.Count : "hiding");
        }

        public static string Format(ushort block, Player p, BlockProps[] props)
        {
            if (!props[block].IsMessageBlock) return null;

            // We want to use the simple designations if possible
            if (block == Block.MB_Black) return "black";
            if (block == Block.MB_White) return "white";
            if (block == Block.MB_Air) return "air";
            if (block == Block.MB_Lava) return "lava";
            if (block == Block.MB_Water) return "water";
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
            p.Message("&T/MB [block] [message]");
            p.Message("&HPlaces a message in your next block.");
            List<string> names = SupportedBlocks(p);
            p.Message("&H  Supported blocks: &S{0}", names.Join());
            p.Message("&H  Use | to separate orders, e.g. /say 1 |/say 2");
            p.Message("&H  Note: \"@p\" is a placeholder for player who clicked.");
            p.Message("&T/MB show &H- Shows or hides message blocks");
        }
    }
}