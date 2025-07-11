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
using MAX.Maths;
using System;
using System.Collections.Generic;


namespace MAX.Orders.CPE
{
    public static class CustomBlockOrder
    {
        public class BlockDefinitionsArgs
        {
            public bool global;
            public Level level;
            public string ord;
            public string scope;
            public BlockDefinition[] defs;
            public BlockDefinition curDef;
        }

        public static void Execute(Player p, string message, OrderData data, bool global, string ord)
        {
            string[] parts = message.SplitSpaces(4);
            Level lvl = p.IsSuper ? null : p.level;

            BlockDefinitionsArgs args = new BlockDefinitionsArgs
            {
                global = global,
                level = lvl,
                ord = ord,
                scope = global ? "global" : "level",
                defs = global ? BlockDefinition.GlobalDefs : lvl.CustomBlockDefs,
                curDef = global ? p.gbBlock : p.lbBlock
            };

            for (int i = 0; i < Math.Min(parts.Length, 3); i++)
                parts[i] = parts[i].ToLower();

            if (message.Length == 0)
            {
                if (args.curDef != null)
                {
                    SendStepHelp(p, args);
                }
                else
                {
                    Help(p, ord);
                }
                return;
            }

            switch (parts[0])
            { // TODO IsCreateOrder
                case "add":
                case "create":
                    AddHandler(p, parts, args); break;
                case "copyall":
                case "copyfrom":
                    CopyAllHandler(p, parts, args, data); break;
                case "copy":
                case "clone":
                case "duplicate":
                    CopyHandler(p, parts, args, data); break;
                case "delete":
                case "remove":
                    RemoveHandler(p, parts, args); break;
                case "info":
                case "about":
                    InfoHandler(p, parts, args); break;
                case "list":
                case "ids":
                    ListHandler(p, parts, args); break;
                case "abort":
                    p.Message("Aborted the custom block creation process.");
                    SetBD(p, args, null); break;
                case "edit":
                    EditHandler(p, parts, args); break;
                default:
                    if (args.curDef != null)
                        DefineBlockStep(p, message, args);
                    else
                        Help(p, ord);
                    break;
            }
        }

        public static void AddHandler(Player p, string[] parts, BlockDefinitionsArgs args)
        {
            ushort target;
            if (parts.Length >= 2)
            {
                string id = parts[1];
                if (!CheckBlock(p, id, args, out target)) return;
                BlockDefinition old = args.defs[target];

                if (ExistsInScope(old, target, args))
                {
                    p.Message("There is already a custom block with the id {0}" +
                              ", you must either use a different id or use \"{1} remove {0}\"", id, args.ord);
                    return;
                }
            }
            else
            {
                target = GetFreeBlock(p, args);
                if (target == Block.Invalid) return;
            }

            BlockDefinition def = new BlockDefinition();
            def.SetBlock(target);
            SetBD(p, args, def);
            args.curDef = def;

            p.Message("  Use &T{0} abort &Sat any time to stop making the block.", args.ord);
            p.Message("  Use &T{0} revert &Sto go back a step", args.ord);
            p.Message("  Use &T{0} [input] &Sto provide input", args.ord);
            p.Message("&f----------------------------------------------------------");

            SetStep(p, args, 2);
            SendStepHelp(p, args);
        }

        public static BlockDefinition[] GetDefs(Player p, OrderData data, string map, ref string coloredMap)
        {
            map = Matcher.FindMaps(p, map);
            if (map == null) return null;

            LevelConfig cfg = LevelInfo.GetConfig(map);
            AccessController visit = new LevelAccessController(cfg, map, true);

            if (!visit.CheckDetailed(p, data.Rank))
            {
                p.Message("Hence, you cannot copy custom blocks from that level");
                return null;
            }

            coloredMap = cfg.Color + map;
            string path = Paths.MapBlockDefs(map);
            return BlockDefinition.Load(path);
        }


        public static void CopyAllHandler(Player p, string[] parts, BlockDefinitionsArgs args, OrderData data)
        {
            if (parts.Length < 2) { Help(p, args.ord); return; }
            string coloredMap = null;
            int copied = 0;

            BlockDefinition[] srcDefs = GetDefs(p, data, parts[1], ref coloredMap);
            if (srcDefs == null) return;

            for (int i = 0; i < srcDefs.Length; i++)
            {
                if (srcDefs[i] == null) continue;

                ushort b = (ushort)i;
                if (!DoCopy(p, args, true, srcDefs[i], b, b)) continue;
                copied++;

                p.Message("Copied the {0} custom block with id \"{1}\".", args.scope, Block.ToRaw(b));
            }

            p.Message("{0} custom blocks were copied from level {1}",
                      copied > 0 ? copied.ToString() : "No", coloredMap);
            if (copied > 0) BlockDefinition.Save(args.global, args.level);
        }

        public static void CopyHandler(Player p, string[] parts, BlockDefinitionsArgs args, OrderData data)
        {
            if (parts.Length < 2) { Help(p, args.ord); return; }
            BlockDefinition[] srcDefs = args.defs;

            ushort dst;
            if (!CheckRawBlocks(p, parts[1], args, out int min, out int max, true)) return;

            if (parts.Length > 2)
            {
                if (!CheckBlock(p, parts[2], args, out dst)) return;

                if (parts.Length > 3)
                {
                    string coloredMap = null;
                    srcDefs = GetDefs(p, data, parts[3], ref coloredMap);
                    if (srcDefs == null) return;
                }
            }
            else
            {
                dst = GetFreeBlock(p, args);
                if (dst == Block.Invalid) return;
            }
            bool changed = false;

            for (int i = min; i <= max && Block.ToRaw(dst) <= Block.MaxRaw; i++, dst++)
            {
                ushort src = Block.FromRaw((ushort)i);
                if (!DoCopy(p, args, false, srcDefs[src], src, dst)) continue;

                p.Message("Duplicated the {0} custom block with id \"{1}\" to \"{2}\".",
                          args.scope, i, Block.ToRaw(dst));
                changed = true;
            }
            if (changed) BlockDefinition.Save(args.global, args.level);
        }

        public static bool DoCopy(Player p, BlockDefinitionsArgs args, bool keepOrder,
                           BlockDefinition srcDef, ushort src, ushort dst)
        {
            if (srcDef == null && src < Block.CPE_COUNT)
            {
                srcDef = DefaultSet.MakeCustomBlock(src);
            }
            if (srcDef == null) { MessageNoBlock(p, src, args); return false; }

            BlockDefinition dstDef = args.defs[dst];
            if (ExistsInScope(dstDef, dst, args)) { MessageAlreadyBlock(p, dst, args); return false; }

            dstDef = srcDef.Copy();
            dstDef.SetBlock(dst);
            if (!keepOrder) dstDef.InventoryOrder = -1;

            UpdateBlock(p, args, dstDef);
            return true;
        }


        public static void InfoHandler(Player p, string[] parts, BlockDefinitionsArgs args)
        {
            if (parts.Length == 1) { Help(p, args.ord); return; }
            if (!CheckRawBlocks(p, parts[1], args, out int min, out int max)) return;

            for (int i = min; i <= max; i++)
            {
                DoInfo(p, args, Block.FromRaw((ushort)i));
            }
        }

        public static void DoInfo(Player p, BlockDefinitionsArgs args, ushort block)
        {
            BlockDefinition def = args.defs[block];
            if (def == null) { MessageNoBlock(p, block, args); return; }

            p.Message("About {0} ({1})", def.Name, def.RawID);
            p.Message("  Draw type: {0}, Blocks light: {1}, collide type: {2}",
                           def.BlockDraw, def.BlocksLight, def.CollideType);
            p.Message("  Fallback ID: {0}, Sound: {1}, Speed: {2}",
                           def.FallBack, def.WalkSound, def.Speed.ToString("F2"));

            if (def.FogDensity == 0)
            {
                p.Message("  Block does not use fog");
            }
            else
            {
                p.Message("  Fog density: {0}, color: {1}",
                               def.FogDensity, Utils.Hex(def.FogR, def.FogG, def.FogB));
            }

            bool tinted = (def.FogR != 0 || def.FogG != 0 || def.FogB != 0) && def.Name.IndexOf('#') >= 0;
            if (tinted)
            {
                p.Message("  Tint color: {0}", Utils.Hex(def.FogR, def.FogG, def.FogB));
            }

            if (def.Shape == 0)
            {
                p.Message("  Block is a sprite");
                p.Message("  Texture ID: {0}", def.RightTex);
            }
            else
            {
                p.Message("  Block is a cube from ({0}, {1}, {2}) to ({3}, {4}, {5})",
                               def.MinX, def.MinZ, def.MinY, def.MaxX, def.MaxZ, def.MaxY);
                p.Message("  Texture IDs (left: {0}, right: {1}, front: {2}, back: {3}, top: {4}, bottom: {5})",
                               def.LeftTex, def.RightTex, def.FrontTex, def.BackTex, def.TopTex, def.BottomTex);
            }

            if (def.InventoryOrder < 0)
            {
                p.Message("  Order: None");
            }
            else if (def.InventoryOrder == 0)
            {
                p.Message("  Order: Hidden from inventory");
            }
            else
            {
                p.Message("  Order: " + def.InventoryOrder);
            }
        }


        public static void ListHandler(Player p, string[] parts, BlockDefinitionsArgs args)
        {
            string modifier = parts.Length > 1 ? parts[1] : "";
            List<BlockDefinition> defsInScope = new List<BlockDefinition>();
            BlockDefinition[] defs = args.defs;

            for (int i = 0; i < defs.Length; i++)
            {
                BlockDefinition def = defs[i];
                if (def == null) continue;
                ushort block = def.GetBlock();

                if (!ExistsInScope(def, block, args)) continue;
                defsInScope.Add(def);
            }
            Paginator.Output(p, defsInScope, PrintBlock,
                             args.ord.Substring(1) + " list", "custom blocks", modifier);
        }

        public static void PrintBlock(Player p, BlockDefinition def)
        {
            p.Message("Custom block &T{0} &Shas name &T{1}", def.RawID, def.Name);
        }


        public static void RemoveHandler(Player p, string[] parts, BlockDefinitionsArgs args)
        {
            if (parts.Length <= 1) { Help(p, args.ord); return; }

            if (!CheckRawBlocks(p, parts[1], args, out int min, out int max)) return;
            bool changed = false;

            for (int i = min; i <= max; i++)
            {
                changed |= DoRemove(p, args, Block.FromRaw((ushort)i));
            }
            if (changed) BlockDefinition.Save(args.global, args.level);
        }

        public static bool DoRemove(Player p, BlockDefinitionsArgs args, ushort block)
        {
            BlockDefinition def = args.defs[block];
            if (!ExistsInScope(def, block, args)) { MessageNoBlock(p, block, args); return false; }

            BlockDefinition.Remove(def, args.defs, args.level);
            ResetProps(args, block);
            p.Message("Removed {0} custom block {1}({2})", args.scope, def.Name, def.RawID);

            BlockDefinition globalDef = BlockDefinition.GlobalDefs[block];
            if (!args.global && globalDef != null)
                BlockDefinition.Add(globalDef, args.defs, args.level);
            return true;
        }


        public static void DefineBlockStep(Player p, string value, BlockDefinitionsArgs args)
        {
            string opt = value.ToLower();
            int step = GetStep(p, args);
            BlockDefinition def = args.curDef;
            bool temp = false;

            if (opt.CaselessEq("revert") && step > 2)
            {
                if (step == 17 && def.FogDensity == 0) step -= 2;
                else if (step == 9 && def.Shape == 0) step -= 5;
                else step--;

                SetStep(p, args, step);
                SendStepHelp(p, args);
                return;
            }

            if (step == 2)
            {
                def.Name = value;
                step++;
            }
            else if (step == 3)
            {
                if (OrderParser.GetBool(p, value, ref temp))
                {
                    def.Shape = temp ? (byte)0 : (byte)16;
                    step++;
                }
            }
            else if (step == 4)
            {
                if (OrderParser.GetUShort(p, value, "Texture ID", ref def.TopTex))
                {
                    step += def.Shape == 0 ? 5 : 1; // skip other texture steps for sprites
                    if (def.Shape == 0) def.SetAllTex(def.TopTex);
                }
            }
            else if (step == 5)
            {
                if (OrderParser.GetUShort(p, value, "Texture ID", ref def.RightTex))
                {
                    def.SetSideTex(def.RightTex);
                    step++;
                }
            }
            else if (step == 6)
            {
                if (OrderParser.GetUShort(p, value, "Texture ID", ref def.BottomTex))
                    step++;
            }
            else if (step == 7)
            {
                if (ParseCoords(p, value, ref def.MinX, ref def.MinY, ref def.MinZ))
                    step++;
            }
            else if (step == 8)
            {
                if (ParseCoords(p, value, ref def.MaxX, ref def.MaxY, ref def.MaxZ))
                    step++;
                def.Shape = def.MaxY;
            }
            else if (step == 9)
            {
                if (OrderParser.GetByte(p, value, "Collide type", ref def.CollideType, 0, 7))
                    step++;
            }
            else if (step == 10)
            {
                if (OrderParser.GetReal(p, value, "Movement speed", ref def.Speed, 0.25f, 3.96f))
                    step++;
            }
            else if (step == 11)
            {
                if (OrderParser.GetBool(p, value, ref temp))
                {
                    def.BlocksLight = temp;
                    step++;
                }
            }
            else if (step == 12)
            {
                if (OrderParser.GetByte(p, value, "Walk sound", ref def.WalkSound, 0, 11))
                    step++;
            }
            else if (step == 13)
            {
                if (OrderParser.GetBool(p, value, ref def.FullBright))
                    step++;
            }
            else if (step == 14)
            {
                if (OrderParser.GetByte(p, value, "Block draw", ref def.BlockDraw, 0, 4))
                    step++;
            }
            else if (step == 15)
            {
                if (OrderParser.GetByte(p, value, "Fog density", ref def.FogDensity))
                {
                    step += def.FogDensity == 0 ? 2 : 1;
                }
            }
            else if (step == 16)
            {
                ColorDesc rgb = default;
                if (OrderParser.GetHex(p, value, ref rgb))
                {
                    def.FogR = rgb.R; def.FogG = rgb.G; def.FogB = rgb.B;
                    step++;
                }
            }
            else if (step == 17)
            {
                byte fallback = GetFallback(p, value);
                if (fallback == Block.Invalid) { SendStepHelp(p, args); return; }
                def.FallBack = fallback;

                if (!AddBlock(p, args, def)) return;
                BlockDefinition.Save(args.global, args.level);

                SetBD(p, args, null);
                SetStep(p, args, 0);
                return;
            }

            SetStep(p, args, step);
            SendStepHelp(p, args);
        }

        public static bool DoEdit(Player p, string[] parts, BlockDefinitionsArgs args, ushort block)
        {
            BlockDefinition def = args.defs[block], globalDef = BlockDefinition.GlobalDefs[block];

            if (def == null && block < Block.CPE_COUNT)
            {
                def = DefaultSet.MakeCustomBlock(block);
                UpdateBlock(p, args, def);
            }
            if (def != null && !args.global && def == globalDef)
            {
                def = globalDef.Copy();
                UpdateBlock(p, args, def);
            }
            if (!ExistsInScope(def, block, args)) { MessageNoBlock(p, block, args); return false; }

            string value = parts[3], blockName = def.Name;
            bool temp = false, changedFallback = false;

            string arg = MapPropertyName(parts[2].ToLower());
            switch (arg)
            {
                case "name":
                    def.Name = value; break;
                case "collide":
                    if (!EditByte(p, value, "Collide type", ref def.CollideType, arg)) return false;
                    break;
                case "speed":
                    if (!OrderParser.GetReal(p, value, "Movement speed", ref def.Speed, 0.25f, 3.96f))
                    {
                        SendEditHelp(p, arg); return false;
                    }
                    break;
                case "toptex":
                    if (!EditUShort(p, value, "Top texture", ref def.TopTex, arg)) return false;
                    break;
                case "alltex":
                    if (!EditUShort(p, value, "All textures", ref def.RightTex, arg)) return false;
                    def.SetAllTex(def.RightTex);
                    break;
                case "sidetex":
                    if (!EditUShort(p, value, "Side texture", ref def.RightTex, arg)) return false;
                    def.SetSideTex(def.RightTex);
                    break;
                case "lefttex":
                    if (!EditUShort(p, value, "Left texture", ref def.LeftTex, arg)) return false;
                    break;
                case "righttex":
                    if (!EditUShort(p, value, "Right texture", ref def.RightTex, arg)) return false;
                    break;
                case "fronttex":
                    if (!EditUShort(p, value, "Front texture", ref def.FrontTex, arg)) return false;
                    break;
                case "backtex":
                    if (!EditUShort(p, value, "Back texture", ref def.BackTex, arg)) return false;
                    break;
                case "bottomtex":
                    if (!EditUShort(p, value, "Bottom texture", ref def.BottomTex, arg)) return false;
                    break;

                case "blockslight":
                    if (!OrderParser.GetBool(p, value, ref temp))
                    {
                        SendEditHelp(p, arg); return false;
                    }
                    def.BlocksLight = temp;
                    break;
                case "sound":
                    if (!EditByte(p, value, "Walk sound", ref def.WalkSound, arg)) return false;
                    break;
                case "fullbright":
                    if (!OrderParser.GetBool(p, value, ref temp))
                    {
                        SendEditHelp(p, arg); return false;
                    }
                    def.FullBright = temp;
                    break;

                case "shape":
                    if (!OrderParser.GetBool(p, value, ref temp))
                    {
                        SendEditHelp(p, arg); return false;
                    }
                    def.Shape = temp ? (byte)0 : def.MaxZ;
                    break;
                case "blockdraw":
                    if (!EditByte(p, value, "Block draw", ref def.BlockDraw, arg)) return false;
                    break;
                case "min":
                    if (!ParseCoords(p, value, ref def.MinX, ref def.MinY, ref def.MinZ))
                    {
                        SendEditHelp(p, arg); return false;
                    }
                    break;
                case "max":
                    if (!ParseCoords(p, value, ref def.MaxX, ref def.MaxY, ref def.MaxZ))
                    {
                        SendEditHelp(p, arg); return false;
                    }
                    break;

                case "fogdensity":
                    if (!EditByte(p, value, "Fog density", ref def.FogDensity, arg)) return false;
                    break;
                case "fogcolor":
                    ColorDesc rgb = default;
                    if (!OrderParser.GetHex(p, value, ref rgb)) return false;
                    def.FogR = rgb.R; def.FogG = rgb.G; def.FogB = rgb.B;
                    break;
                case "fallback":
                    byte fallback = GetFallback(p, value);
                    if (fallback == Block.Invalid) return false;
                    changedFallback = true;

                    value = Block.GetName(p, fallback);
                    def.FallBack = fallback; break;

                case "order":
                    int order = 0;
                    if (!OrderParser.GetInt(p, value, "Inventory order", ref order, 0, Block.MaxRaw))
                    {
                        SendEditHelp(p, arg); return false;
                    }
                    BlockDefinition[] defs = args.defs;

                    // Don't let multiple blocks be assigned to same order
                    // TODO move out of CustomBlockOrder class
                    if (order != def.RawID && order != 0)
                    {
                        for (int i = 0; i < defs.Length; i++)
                        {
                            if (defs[i] == null || defs[i].InventoryOrder != order) continue;
                            p.Message("Block {0} already had order {1}", defs[i].Name, order);
                            return false;
                        }
                    }

                    def.InventoryOrder = order == def.RawID ? -1 : order;
                    BlockDefinition.UpdateOrder(def, args.global, args.level);
                    p.Message("Set inventory order for {0} to {1}", blockName,
                                   order == def.RawID ? "default" : order.ToString());
                    return true;
                default:
                    p.Message("Unrecognised property: " + arg); return false;
            }

            p.Message("Set {0} for {1} to {2}", arg, blockName, value);
            BlockDefinition.Add(def, args.defs, args.level);
            if (changedFallback)
            {
                BlockDefinition.UpdateFallback(args.global, def.GetBlock(), args.level);
            }
            return true;
        }

        public static void EditHandler(Player p, string[] parts, BlockDefinitionsArgs args)
        {
            if (parts.Length <= 3)
            {
                if (parts.Length == 1)
                {
                    p.Message("Valid properties: " + helpSections.Keys.Join());
                }
                else if (parts.Length == 3)
                {
                    Help(p, args.ord, "edit " + parts[2]);
                }
                else
                {
                    Help(p, args.ord);
                }
                return;
            }

            if (!CheckRawBlocks(p, parts[1], args, out int min, out int max)) return;
            bool changed = false;

            for (int i = min; i <= max; i++)
            {
                changed |= DoEdit(p, parts, args, Block.FromRaw((ushort)i));
            }
            if (changed) BlockDefinition.Save(args.global, args.level); // TODO SaveChanged(bool changed, bool global, Level lvl) func
        }


        public static void UpdateBlock(Player p, BlockDefinitionsArgs args, BlockDefinition def)
        {
            p.Message("Created a new {0} custom block {1}({2})", args.scope, def.Name, def.RawID);

            ushort block = def.GetBlock();
            BlockDefinition.Add(def, args.defs, args.level);
            ResetProps(args, block);
        }

        public static bool AddBlock(Player p, BlockDefinitionsArgs args, BlockDefinition def)
        {
            ushort block = def.GetBlock();
            BlockDefinition old = args.defs[block];
            if (!args.global && old == BlockDefinition.GlobalDefs[block]) old = null; // TODO ExistsInScope

            // in case the list is modified before we finish the order.
            if (old != null)
            {
                block = GetFreeBlock(p, args);
                if (block == Block.Invalid)
                {
                    if (!args.global) p.Message("You may also manually specify the same existing id of a global custom block.");
                    return false;
                }
                def.SetBlock(block);
            }

            UpdateBlock(p, args, def);
            return true;
        }

        public static byte GetFallback(Player p, string value)
        {
            if (!OrderParser.GetBlock(p, value, out ushort block)) return Block.Invalid;

            if (block >= Block.Extended)
            {
                p.Message("&WCustom blocks cannot be used as fallback blocks.");
                return Block.Invalid;
            }
            if (Block.IsPhysicsType(block))
            {
                p.Message("&WPhysics block cannot be used as fallback blocks.");
                return Block.Invalid;
            }
            return (byte)block;
        }

        public static ushort GetFreeBlock(Player p, BlockDefinitionsArgs args)
        {
            BlockDefinition[] defs = args.defs;

            // Start from opposite ends to avoid overlap
            if (args.global)
            {
                for (ushort b = Block.CPE_COUNT; b <= Block.MaxRaw; b++)
                {
                    ushort block = Block.FromRaw(b);
                    if (defs[block] == null) return block;
                }
            }
            else
            {
                for (ushort b = Block.MaxRaw; b >= Block.CPE_COUNT; b--)
                {
                    ushort block = Block.FromRaw(b);
                    if (defs[block] == null) return block;
                }
            }

            p.Message("&WThere are no custom block ids left, you must &T{0} remove &Wa custom block first.", args.ord);
            return Block.Invalid;
        }


        public static void MessageNoBlock(Player p, ushort block, BlockDefinitionsArgs args)
        {
            p.Message("&WThere is no {1} custom block with the id \"{0}\".", Block.ToRaw(block), args.scope);
            p.Message("Type &T{0} list &Sto see a list of {1} custom blocks.", args.ord, args.scope);
        }

        public static void MessageAlreadyBlock(Player p, ushort block, BlockDefinitionsArgs args)
        {
            p.Message("&WThere is already a {1} custom block with the id \"{0}\".", Block.ToRaw(block), args.scope);
            p.Message("Type &T{0} list &Sto see a list of {1} custom blocks.", args.ord, args.scope);
        }

        public static bool EditByte(Player p, string value, string propName, ref byte target, string help)
        {
            if (!OrderParser.GetByte(p, value, propName, ref target))
            {
                SendEditHelp(p, help); return false;
            }
            return true;
        }

        public static bool EditUShort(Player p, string value, string propName, ref ushort target, string help)
        {
            if (!OrderParser.GetUShort(p, value, propName, ref target))
            {
                SendEditHelp(p, help); return false;
            }
            return true;
        }

        public static bool ParseCoords(Player p, string parts, ref byte x, ref byte y, ref byte z)
        {
            string[] coords = parts.SplitSpaces();
            if (coords.Length != 3) return false;

            // TODO: Having to cast to sbyte here is yucky. blockdefs code should be fixed instead
            Vec3S32 P = new Vec3S32((sbyte)x, (sbyte)z, (sbyte)y); // blockdef files have z being height, we use y being height
            if (!OrderParser.GetCoords(p, coords, 0, ref P)) return false;

            if (!OrderParser.CheckRange(p, P.X, "X", -127, 127)) return false;
            if (!OrderParser.CheckRange(p, P.Y, "Y", -127, 127)) return false;
            if (!OrderParser.CheckRange(p, P.Z, "Z", -127, 127)) return false;

            // TODO: Improve output message with relative coords (currently shows "Set max for Stone to ~ ~8 ~")
            x = (byte)P.X; z = (byte)P.Y; y = (byte)P.Z; // blockdef files have z being height, we use y being height
            return true;
        }

        public static bool CheckRaw(Player p, string arg, BlockDefinitionsArgs args,
                             out int raw, bool air = false)
        {
            int min = air ? 0 : 1;
            int max = Block.MaxRaw;

            // Check for block names (can't use standard parsing behaviour)
            if (!int.TryParse(arg, out raw))
            {
                BlockDefinition def = BlockDefinition.ParseName(arg, args.defs);

                if (def == null)
                {
                    p.Message("&W{0} is not a valid block {1} custom block name", arg, args.scope);
                    return false;
                }
                raw = def.RawID;
                return true;
            }

            return OrderParser.GetInt(p, arg, "Block ID", ref raw, min, max);
        }

        public static bool CheckRawBlocks(Player p, string arg, BlockDefinitionsArgs args,
                                   out int min, out int max, bool air = false)
        {
            bool success;

            // Either "[id]" or "[min]-[max]"
            if (OrderParser.IsRawBlockRange(arg, out string[] bits))
            {
                success = CheckRaw(p, bits[0], args, out min, air)
                        & CheckRaw(p, bits[1], args, out max, air);
            }
            else
            {
                success = CheckRaw(p, arg, args, out min, air);
                max = min;
            }
            return success;
        }

        public static bool CheckBlock(Player p, string arg, BlockDefinitionsArgs args,
                               out ushort block, bool air = false)
        {
            bool success = CheckRaw(p, arg, args, out int raw, air);

            block = Block.FromRaw((ushort)raw);
            return success;
        }

        public static void ResetProps(BlockDefinitionsArgs args, ushort block)
        {
            Level lvl = args.level;
            BlockProps[] scope = args.global ? Block.Props : lvl.Props;
            int changed = scope[block].ChangedScope & BlockProps.ScopeId(scope);
            if (changed != 0) return;

            // properties not manually modified, revert (e.g. make grass die in shadow again)
            scope[block] = BlockProps.MakeDefault(scope, lvl, block);
            BlockProps.ApplyChanges(scope, lvl, block, false);
        }


        public static int GetStep(Player p, BlockDefinitionsArgs args)
        {
            return args.global ? p.gbStep : p.lbStep;
        }

        public static void SetBD(Player p, BlockDefinitionsArgs args, BlockDefinition def)
        {
            if (args.global) p.gbBlock = def;
            else p.lbBlock = def;
        }

        public static void SetStep(Player p, BlockDefinitionsArgs args, int step)
        {
            if (args.global) p.gbStep = step;
            else p.lbStep = step;
        }

        public static bool ExistsInScope(BlockDefinition def, ushort block, BlockDefinitionsArgs args)
        {
            return def != null && (args.global || def != BlockDefinition.GlobalDefs[block]);
        }


        public static void SendStepHelp(Player p, BlockDefinitionsArgs args)
        {
            int step = GetStep(p, args);
            string[] help = helpSections[stepsHelp[step]];
            BlockDefinition def = args.curDef;

            for (int i = 0; i < help.Length; i++)
            {
                string msg = help[i];
                // TODO: Ugly hardcoding, but not really worth doing properly
                if (step == 4 && def.Shape == 0) msg = msg.Replace("top texture", "texture");

                p.Message(msg);
            }
            if (step == 2) p.Message("Use &T{0} [answer] &Sto type your answers", args.ord);
            p.Message("&f--------------------------");
        }

        public static void SendEditHelp(Player p, string section)
        {
            string[] help = helpSections[section];
            for (int i = 0; i < help.Length; i++)
                p.Message(help[i].Replace("Type", "Use"));
        }

        public static string MapPropertyName(string prop)
        {
            if (prop.CaselessEq("side") || prop.CaselessEq("all") || prop.CaselessEq("top") || prop.CaselessEq("bottom")
                || prop.CaselessEq("left") || prop.CaselessEq("right") || prop.CaselessEq("front") || prop.CaselessEq("back")) return prop + "tex";

            if (prop.CaselessEq("sides") || prop.CaselessEq("sidestex")) return "sidetex";
            if (prop.CaselessEq("light")) return "blockslight";
            if (prop.CaselessEq("bright")) return "fullbright";
            if (prop.CaselessEq("walksound")) return "sound";
            if (prop.CaselessEq("draw")) return "blockdraw";
            if (prop.CaselessEq("mincoords")) return "min";
            if (prop.CaselessEq("maxcoords")) return "max";
            if (prop.CaselessEq("density")) return "fogdensity";
            if (prop.CaselessEq("col") || prop.CaselessEq("fogcol")) return "fogcolor";
            if (prop.CaselessEq("fogcolour")) return "fogcolor";
            if (prop.CaselessEq("fallbackid") || prop.CaselessEq("fallbackblock")) return "fallback";

            return prop;
        }


        public static string[] stepsHelp = new string[] {
            null, null, "name", "shape", "toptex", "sidetex", "bottomtex", "min", "max", "collide",
            "speed", "blockslight", "sound", "fullbright", "blockdraw", "fogdensity", "fogcolor", "fallback" };

        public const string texLine = "Press F10 to see the numbers for each texture in terrain.png";
        public static Dictionary<string, string[]> helpSections = new Dictionary<string, string[]>() {
            { "name", new string[]  { "Type the name for the block." } },
            { "shape", new string[] { "Type '0' if the block is a cube, '1' if a sprite (e.g roses)." } },
            { "blockslight", new string[] { "Type 'yes' if the block casts a shadow, 'no' if it doesn't." } },
            { "fullbright", new string[]  { "Type 'yes' if the block is fully lit (e.g. lava), 'no' if not." } },

            { "alltex", new string[]    { "Type a number for all textures.", texLine } },
            { "sidetex", new string[]   { "Type a number for sides texture.", texLine } },
            { "lefttex", new string[]   { "Type a number for the left side texture.", texLine } },
            { "righttex", new string[]  { "Type a number for the right side texture.", texLine } },
            { "fronttex", new string[]  { "Type a number for the front side texture.", texLine } },
            { "backtex", new string[]   { "Type a number for the back side texture.", texLine } },
            { "toptex", new string[]    { "Type a number for the top texture.", texLine } },
            { "bottomtex", new string[] { "Type a number for the bottom texture.", texLine } },

            { "min", new string[] { "Enter the three minimum coordinates of the cube in units (separated by spaces). 1 block = 16 units.",
                    "Minimum coordinates for a normal block are &40 &20 &10." } },
            { "max", new string[] { "Enter the three maximum coordinates of the cube in units (separated by spaces). 1 block = 16 units.",
                    "Maximum coordinates for a normal block are &416 &216 &116." } },
            { "collide", new string[] { "Type a number between '0' and '7' for collision type.",
                    "0 - block is walk-through (e.g. air).", "1 - block is swim-through/climbable (e.g. rope).",
                    "2 - block is solid (e.g. dirt).", "3 - block is solid, but slippery like ice",
                    "4 - block is solid, but even slipperier than ice", "5 - block is swim-through like water",
                    "6 - block is swim-through like lava", "7 - block is climbable like rope" } },
            { "speed", new string[] { "Type a number between '0.25' (25% speed) and '3.96' (396% speed).",
                    "This speed is used when inside or walking on the block. Default speed is 1" }
            },
            { "sound", new string[] { "Type a number between '0' and '9' for the sound played when walking on it and breaking.",
                    "0 = None, 1 = Wood, 2 = Gravel, 3 = Grass, 4 = Stone",
                    "5 = Metal, 6 = Glass, 7 = Cloth, 8 = Sand, 9 = Snow" }
            },
            { "blockdraw", new string[] { "Enter the block's draw method.", "0 = Opaque, 1 = Transparent (Like glass)",
                    "2 = Transparent (Like leaves), 3 = Translucent (Like ice), 4 = Gas (Like air)" }
            },
            { "fogdensity", new string[] { "Enter the fog density for the block. 0 = No fog at all",
                    "1 - 255 = Increasing density (e.g. water has 12, lava 255)" }
            },
            { "fogcolor", new string[] { "Enter the fog color (hex color)" } },
            { "fallback", new string[] { "Enter the fallback block (Block shown to players who can't see custom blocks).",
                    "You can use any block name or block ID from the normal blocks." }
            },
            { "order", new string[] { "Enter the position/order of this block in the inventory.",
                    "The default position of a block is its ID.",
                    "A position of 0 hides the block from the inventory." }
            },
        };


        public static void Help(Player p, string ord)
        {
            p.Message("&H{0} help page 1:", ord.Substring(1));
            p.Message("&T{0} add [id] &H- begins creating a new custom block", ord);
            p.Message("&T{0} copy [id] <new id> &H- clones an existing custom block", ord);
            p.Message("&T{0} edit [id] [property] [value] &H- edits that custom block", ord);
            p.Message("&T{0} remove [id] &H- removes that custom block", ord);
            p.Message("&HTo see the list of editable properties, type &T{0} edit", ord);
            p.Message("&HTo read help page 2, type &T/help {0} 2", ord.Substring(1));
        }

        public static void Help(Player p, string ord, string args)
        {
            if (args.CaselessEq("2"))
            {
                p.Message("&H{0} help page 2:", ord.Substring(1));
                p.Message("&T{0} copyall [level] &H- clones all custom blocks from [level]", ord);
                p.Message("&T{0} list <offset> &H- lists all custom blocks", ord);
                p.Message("&T{0} info [id] &H- shows info about that custom block", ord);
                p.Message("&HYou may edit, remove or see info for multiple IDs at once.");
                p.Message("&HUse &T/help {0} 3 &Hfor multi explanation.", ord.Substring(1));
                return;
            }
            else if (args.CaselessEq("3"))
            {
                p.Message("&H{0} help page 3:", ord.Substring(1));
                p.Message("&HTo work with multiple block IDs at once,");
                p.Message("&Huse a start and end range seperated by a dash.");
                p.Message("&HFor example, &T{0} remove 21-24", ord);
                p.Message("&Hwould remove blocks with ID 21, 22, 23, and 24.", ord);
                p.Message("&HMulti editing only works with &T{0} edit, remove, or info", ord);
                return;
            }
            if (!args.CaselessStarts("edit ")) { Help(p, ord); return; }
            string prop = args.Substring(args.IndexOf(' ') + 1);
            prop = MapPropertyName(prop.ToLower());

            if (!helpSections.ContainsKey(prop))
            {
                p.Message("Valid properties: " + helpSections.Keys.Join());
            }
            else
            {
                SendEditHelp(p, prop);
            }
        }
    }

    public class OrdGlobalBlock : Order
    {
        public override string Name { get { return "GlobalBlock"; } }
        public override string Shortcut { get { return "gb"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message, OrderData data)
        {
            CustomBlockOrder.Execute(p, message, data, true, "/gb");
        }

        public override void Help(Player p)
        {
            CustomBlockOrder.Help(p, "/gb");
        }

        public override void Help(Player p, string message)
        {
            CustomBlockOrder.Help(p, "/gb", message);
        }
    }

    public class OrdLevelBlock : Order
    {
        public override string Name { get { return "LevelBlock"; } }
        public override string Shortcut { get { return "lb"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            CustomBlockOrder.Execute(p, message, data, false, "/lb");
        }

        public override void Help(Player p)
        {
            CustomBlockOrder.Help(p, "/lb");
        }

        public override void Help(Player p, string message)
        {
            CustomBlockOrder.Help(p, "/lb", message);
        }
    }
}