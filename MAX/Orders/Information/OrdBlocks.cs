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
using MAX.Orders.World;
using System;
using System.Collections.Generic;


namespace MAX.Orders.Info
{
    public class OrdBlocks : Order
    {
        public override string Name { get { return "Blocks"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("Materials") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces();
            string modifier = args.Length > 1 ? args[1] : "";
            string type = args[0];
            ushort block;

            if (type.Length == 0 || type.CaselessEq("basic"))
            {
                p.Message("Basic blocks: ");
                OutputBlocks(p, "basic", modifier,
                             b => !Block.IsPhysicsType(b));
            }
            else if (type.CaselessEq("all") || type.CaselessEq("complex"))
            {
                p.Message("Complex blocks: ");
                OutputBlocks(p, "complex", modifier,
                             b => Block.IsPhysicsType(b));
            }
            else if ((block = Block.Parse(p, type)) != Block.Invalid)
            {
                OutputBlockInfo(p, block);
            }
            else if (Group.Find(type) != null)
            {
                Group grp = Group.Find(type);
                p.Message("Blocks which {0} &Scan place: ", grp.ColoredName);
                OutputBlocks(p, type, modifier,
                             b => grp.Blocks[b]);
            }
            else if (args.Length > 1)
            {
                Help(p);
            }
            else
            {
                p.Message("Unable to find block or rank");
            }
        }

        public static void OutputBlocks(Player p, string type, string modifier, Predicate<ushort> selector)
        {
            List<ushort> blocks = new List<ushort>(Block.SUPPORTED_COUNT);
            for (ushort b = 0; b < Block.SUPPORTED_COUNT; b++)
            {
                if (Block.ExistsFor(p, b) && selector(b)) blocks.Add(b);
            }

            Paginator.Output(p, blocks, b => Block.GetColoredName(p, b),
                             "Blocks " + type, "blocks", modifier);
        }

        public static void OutputBlockInfo(Player p, ushort block)
        {
            string name = Block.GetName(p, block);
            BlockProps[] scope = p.IsSuper ? Block.Props : p.level.Props;
            OrdBlockProperties.Detail(p, scope, block);

            if (Block.IsPhysicsType(block))
            {
                p.Message("&bComplex information for \"{0}\":", name);
                OutputPhysicsInfo(p, scope, block); return;
            }

            string msg = "";
            for (ushort b = Block.CPE_COUNT; b < Block.CORE_COUNT; b++)
            {
                if (Block.Convert(b) != block) continue;
                msg += Block.GetColoredName(p, b) + ", ";
            }

            if (msg.Length > 0)
            {
                p.Message("Blocks which look like \"{0}\":", name);
                p.Message(msg.Remove(msg.Length - 2));
            }
            else
            {
                p.Message("No complex blocks look like \"{0}\"", name);
            }
        }

        public static void OutputPhysicsInfo(Player p, BlockProps[] scope, ushort b)
        {
            ushort conv = Block.Convert(b);
            p.Message("&c  Appears as a \"{0}\" block", Block.GetName(p, conv));

            // TODO: Use scope[b] instead of hardcoded global
            if (Block.LightPass(b)) p.Message("  Allows light through");
            if (Block.NeedRestart(b)) p.Message("  The block's physics will auto-start");

            if (Physics(scope, b))
            {
                p.Message("  Affects physics in some way");
            }
            else
            {
                p.Message("  Does not affect physics in any way");
            }

            if (Block.AllowBreak(b)) p.Message("  Anybody can activate this block");
            if (Block.Walkthrough(conv)) p.Message("  Can be walked through");
            if (Mover(scope, conv)) p.Message("  Can be activated by walking through it");
        }

        public static bool Mover(BlockProps[] scope, ushort conv)
        {
            bool nonSolid = Block.Walkthrough(conv);
            return BlockBehaviour.GetWalkthroughHandler(conv, scope, nonSolid) != null;
        }

        public static bool Physics(BlockProps[] scope, ushort b)
        {
            if (scope[b].IsMessageBlock || scope[b].IsPortal) return false;
            if (scope[b].IsDoor || scope[b].IsTDoor) return false;
            if (scope[b].OPBlock) return false;

            return BlockBehaviour.GetPhysicsHandler(b, Block.Props) != null;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Blocks &H- Lists all basic blocks");
            p.Message("&T/Blocks complex &H- Lists all complex blocks");
            p.Message("&T/Blocks [block] &H- Lists information about that block");
            p.Message("&T/Blocks [rank] &H- Lists all blocks [rank] can use");
            p.Message("&HTo see available ranks, type &T/ViewRanks");
        }
    }
}