﻿/*
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
using System;


namespace MAX.Blocks.Physics
{

    public static class ExtLiquidPhysics
    {

        public static void DoMagma(Level lvl, ref PhysInfo C)
        {
            C.Data.Data++;
            if (C.Data.Data < 3) return;

            ushort x = C.X, y = C.Y, z = C.Z;
            ushort below = lvl.GetBlock(x, (ushort)(y - 1), z, out int index);

            if (below == Block.Air)
            {
                lvl.AddUpdate(index, Block.Magma, default(PhysicsArgs));
            }
            else if (below != Block.Magma)
            {
                ushort block = C.Block;
                LiquidPhysics.PhysLava(lvl, (ushort)(x + 1), y, z, block);
                LiquidPhysics.PhysLava(lvl, (ushort)(x - 1), y, z, block);
                LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z + 1), block);
                LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z - 1), block);
            }

            if (lvl.Physics <= 1 || C.Data.Data <= 10) return;
            C.Data.Data = 0;
            bool flowUp = false;

            MagmaFlow(lvl, x - 1, y, z, ref flowUp);
            MagmaFlow(lvl, x + 1, y, z, ref flowUp);
            MagmaFlow(lvl, x, y - 1, z, ref flowUp);
            MagmaFlow(lvl, x, y, z - 1, ref flowUp);
            MagmaFlow(lvl, x, y, z + 1, ref flowUp);
            if (flowUp)
                MagmaFlow(lvl, x, y + 1, z, ref flowUp);
        }

        public static void MagmaFlow(Level lvl, int x, int y, int z, ref bool flowUp)
        {
            ushort block = lvl.GetBlock((ushort)x, (ushort)y, (ushort)z, out int index);

            if (lvl.Props[block].LavaKills)
            {
                lvl.AddUpdate(index, Block.Magma, default(PhysicsArgs));
                flowUp = true;
            }
        }

        public static void DoGeyser(Level lvl, ref PhysInfo C)
        {
            C.Data.Data++;

            ushort x = C.X, y = C.Y, z = C.Z;
            ushort below = lvl.GetBlock(x, (ushort)(y - 1), z, out int index);

            if (below == Block.Air)
            {
                lvl.AddUpdate(index, Block.Geyser, default(PhysicsArgs));
            }
            else if (below != Block.Geyser)
            {
                ushort block = C.Block;
                LiquidPhysics.PhysWater(lvl, (ushort)(x + 1), y, z, block);
                LiquidPhysics.PhysWater(lvl, (ushort)(x - 1), y, z, block);
                LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z + 1), block);
                LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z - 1), block);
            }

            if (lvl.Physics <= 1 || C.Data.Data <= 10) return;
            C.Data.Data = 0;
            bool flowUp = false;

            GeyserFlow(lvl, x - 1, y, z, ref flowUp);
            GeyserFlow(lvl, x + 1, y, z, ref flowUp);
            GeyserFlow(lvl, x, y - 1, z, ref flowUp);
            GeyserFlow(lvl, x, y, z - 1, ref flowUp);
            GeyserFlow(lvl, x, y, z + 1, ref flowUp);
            if (flowUp)
                GeyserFlow(lvl, x, y + 1, z, ref flowUp);
        }

        public static void GeyserFlow(Level lvl, int x, int y, int z, ref bool flowUp)
        {
            ushort block = lvl.GetBlock((ushort)x, (ushort)y, (ushort)z, out int index);

            if (lvl.Props[block].WaterKills)
            {
                lvl.AddUpdate(index, Block.Geyser, default(PhysicsArgs));
                flowUp = true;
            }
        }

        public static void DoWaterfall(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;
            ushort below = lvl.GetBlock(x, (ushort)(y - 1), z, out int index);

            switch (below)
            {
                case Block.Air:
                    lvl.AddUpdate(index, Block.WaterDown, default(PhysicsArgs));
                    if (!C.Data.HasWait) C.Data.Data = PhysicsArgs.RemoveFromChecks;
                    break;
                case Block.Air_FloodDown:
                case Block.StillLava:
                case Block.StillWater:
                case Block.WaterDown:
                    break;

                default:
                    ushort block = C.Block;
                    LiquidPhysics.PhysWater(lvl, (ushort)(x + 1), y, z, block);
                    LiquidPhysics.PhysWater(lvl, (ushort)(x - 1), y, z, block);
                    LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z + 1), block);
                    LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z - 1), block);
                    if (!C.Data.HasWait) C.Data.Data = PhysicsArgs.RemoveFromChecks;
                    break;
            }
        }

        public static void DoLavafall(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;
            ushort below = lvl.GetBlock(x, (ushort)(y - 1), z, out int index);

            switch (below)
            {
                case Block.Air:
                    lvl.AddUpdate(index, Block.LavaDown, default(PhysicsArgs));
                    if (!C.Data.HasWait) C.Data.Data = PhysicsArgs.RemoveFromChecks;
                    break;
                case Block.Air_FloodDown:
                case Block.StillLava:
                case Block.StillWater:
                case Block.LavaDown:
                    break;
                default:
                    ushort block = C.Block;
                    LiquidPhysics.PhysLava(lvl, (ushort)(x + 1), y, z, block);
                    LiquidPhysics.PhysLava(lvl, (ushort)(x - 1), y, z, block);
                    LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z + 1), block);
                    LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z - 1), block);
                    if (!C.Data.HasWait) C.Data.Data = PhysicsArgs.RemoveFromChecks;
                    break;
            }
        }

        public static void DoFaucet(Level lvl, ref PhysInfo C, ushort target)
        {
            C.Data.Data++;
            if (C.Data.Data < 2) return;
            C.Data.Data = 0;

            Random rand = lvl.physRandom;
            ushort below = lvl.GetBlock(C.X, (ushort)(C.Y - 1), C.Z, out int index);

            if (below == Block.Air || below == target)
            {
                if (rand.Next(1, 10) > 7)
                {
                    lvl.AddUpdate(index, Block.Air_FloodDown, default(PhysicsArgs));
                }
            }
            else if (below == Block.Air_FloodDown)
            {
                if (rand.Next(1, 10) > 4)
                {
                    lvl.AddUpdate(index, target);
                }
            }
        }
    }
}