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
using MAX.Blocks;
using System.Collections.Generic;


namespace MAX.Orders.World
{
    public class OrdBlockProperties : Order
    {
        public override string Name { get { return "BlockProperties"; } }
        public override string Shortcut { get { return "BlockProps"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(4);
            if (args.Length < 2) { Help(p); return; }

            BlockProps[] scope = GetScope(p, data, args[0]);
            if (scope == null) return;
            if (IsListOrder(args[1]) && (args.Length == 2 || IsListModifier(args[2])))
            {
                ListProps(p, scope, args); return;
            }

            ushort block = GetBlock(p, scope, args[1]);
            if (block == Block.Invalid) return;
            if (args.Length < 3) { Help(p); return; }
            string opt = args[2];

            if (opt.CaselessEq("copy"))
            {
                CopyProps(p, scope, block, args);
            }
            else if (opt.CaselessEq("reset") || IsDeleteOrder(opt))
            {
                ResetProps(p, scope, block);
            }
            else
            {
                SetProps(p, scope, block, args);
            }
        }

        public static BlockProps[] GetScope(Player p, OrderData data, string scope)
        {
            if (scope.CaselessEq("core") || scope.CaselessEq("global")) return Block.Props;

            if (scope.CaselessEq("level"))
            {
                if (p.IsSuper) { p.Message("Cannot use level scope from {0}.", p.SuperName); return null; }
                if (!LevelInfo.Check(p, data.Rank, p.level, "change properties of blocks in this level")) return null;
                return p.level.Props;
            }

            p.Message("&WScope must be: global or level");
            return null;
        }

        public static ushort GetBlock(Player p, BlockProps[] scope, string str)
        {
            Player pScope = scope == Block.Props ? Player.MAX : p;
            ushort block = Block.Parse(pScope, str);

            if (block == Block.Invalid)
            {
                p.Message("&WThere is no block \"{0}\".", str);
            }
            return block;
        }

        public static void Detail(Player p, BlockProps[] scope, ushort block)
        {
            BlockProps props = scope[block];
            string name = BlockProps.ScopedName(scope, p, block);
            p.Message("&TProperties of {0}:", name);

            if (props.KillerBlock) p.Message("  Kills players who collide with this block");
            if (props.DeathMessage != null) p.Message("  Death message: &S" + props.DeathMessage);

            if (props.IsDoor) p.Message("  Is an ordinary door");
            if (props.IsTDoor) p.Message("  Is a tdoor (allows other blocks through when open)");
            if (props.oDoorBlock != Block.Invalid)
                p.Message("  Is an odoor (can be toggled by doors, and toggles other odoors)");

            if (props.IsPortal) p.Message("  Can be used as a &T/Portal");
            if (props.IsMessageBlock) p.Message("  Can be used as a &T/MessageBlock");

            if (props.WaterKills) p.Message("  Is destroyed by flooding water");
            if (props.LavaKills) p.Message("  Is destroyed by flooding lava");

            if (props.OPBlock) p.Message("  Is not affected by explosions");
            if (props.IsRails) p.Message("  Can be used as rails for &T/Train");

            if (props.AnimalAI != AnimalAI.None)
            {
                p.Message("  Has the {0} AI behaviour", props.AnimalAI);
            }
            if (props.StackBlock != Block.Air)
            {
                p.Message("  Stacks as {0} when placed on top of itself",
                          BlockProps.ScopedName(scope, p, props.StackBlock));
            }
            if (props.Drownable) p.Message("&H  Players can drown in this block");

            if (props.GrassBlock != Block.Invalid)
            {
                p.Message("  Grows into {0} when in sunlight",
                          BlockProps.ScopedName(scope, p, props.GrassBlock));
            }
            if (props.DirtBlock != Block.Invalid)
            {
                p.Message("  Decays into {0} when in shadow",
                          BlockProps.ScopedName(scope, p, props.DirtBlock));
            }
        }

        public static List<ushort> FilterProps(BlockProps[] scope)
        {
            int changed = BlockProps.ScopeId(scope);
            List<ushort> filtered = new List<ushort>();

            for (int b = 0; b < scope.Length; b++)
            {
                if ((scope[b].ChangedScope & changed) == 0) continue;
                filtered.Add((ushort)b);
            }
            return filtered;
        }

        public void ListProps(Player p, BlockProps[] scope, string[] args)
        {
            List<ushort> filtered = FilterProps(scope);
            string ord = "BlockProps " + args[0] + " list";
            string modifier = args.Length > 2 ? args[2] : "";

            Paginator.Output(p, filtered, b => BlockProps.ScopedName(scope, p, b),
                             ord, "modified blocks", modifier);
        }

        public void CopyProps(Player p, BlockProps[] scope, ushort block, string[] args)
        {
            if (args.Length < 4) { Help(p); return; }
            ushort dst = GetBlock(p, scope, args[3]);
            if (dst == Block.Invalid) return;

            scope[dst] = scope[block];
            scope[dst].ChangedScope |= BlockProps.ScopeId(scope);

            p.Message("Copied properties of {0} to {1}",
                      BlockProps.ScopedName(scope, p, block),
                      BlockProps.ScopedName(scope, p, dst));
            BlockProps.ApplyChanges(scope, p.level, block, true);
        }

        public void ResetProps(Player p, BlockProps[] scope, ushort block)
        {
            scope[block] = BlockProps.MakeDefault(scope, p.level, block);
            string name = BlockProps.ScopedName(scope, p, block);

            p.Message("Reset properties of {0} to default", name);
            BlockProps.ApplyChanges(scope, p.level, block, true);
        }

        public void SetProps(Player p, BlockProps[] scope, ushort block, string[] args)
        {
            BlockOption opt = BlockOptions.Find(args[2]);
            if (opt == null) { Help(p); return; }
            string value = args.Length > 3 ? args[3] : "";

            opt.SetFunc(p, scope, block, value);
            scope[block].ChangedScope |= BlockProps.ScopeId(scope);
            BlockProps.ApplyChanges(scope, p.level, block, true);
        }

        public override void Help(Player p)
        {
            p.Message("&T/BlockProps global/level list");
            p.Message("&HLists blocks which have non-default properties");
            p.Message("&T/BlockProps global/level [id/name] copy [new id]");
            p.Message("&HCopies properties of that block to another");
            p.Message("&T/BlockProps global/level [id/name] reset");
            p.Message("&HResets properties of that block to their default");
            p.Message("&T/BlockProps global/level [id/name] [property] <value>");
            p.Message("&HSets various properties of that block");
            p.Message("&H  Use &T/Help BlockProps props &Hfor a list of properties");
        }

        public override void Help(Player p, string message)
        {
            if (message.CaselessEq("props") || message.CaselessEq("properties"))
            {
                p.Message("&HProperties: &f{0}", BlockOptions.Options.Join(o => o.Name));
                p.Message("&HUse &T/Help BlockProps [property] &Hfor more details");
                return;
            }

            BlockOption opt = BlockOptions.Find(message);
            if (opt != null)
            {
                p.Message(opt.Help);
            }
            else
            {
                p.Message("&WUnrecognised property \"{0}\"", message);
            }
        }
    }
}