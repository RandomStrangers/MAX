﻿/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified for use with MCForge)
 
   
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
using MAX.Drawing.Brushes;
using MAX.Maths;


namespace MAX.Drawing.Ops
{
    public class FixGrassDrawOp : CuboidDrawOp
    {
        public bool LightMode;
        public bool FixGrass, FixDirt;

        public FixGrassDrawOp()
        {
            Flags = BlockDBFlags.FixGrass;
            AffectedByTransform = false;
        }

        public override void Perform(Vec3S32[] marks, Brush brush, DrawOpOutput output)
        {
            if (LightMode)
            {
                FixLight(output);
            }
            else
            {
                Fix(output, FixGrass, FixDirt);
            }

            Player.Message("Fixed " + TotalModified + " blocks.");
        }

        public void Fix(DrawOpOutput output, bool fixGrass, bool fixDirt)
        {
            Level lvl = Level;
            int maxY = lvl.Height - 1, oneY = lvl.Width * lvl.Length;
            int index, width = lvl.Width, length = lvl.Length;
            ushort above, block;

            Vec3U16 p1 = Clamp(Min), p2 = Clamp(Max);
            for (ushort y = p1.Y; y <= p2.Y; y++)
                for (ushort z = p1.Z; z <= p2.Z; z++)
                    for (ushort x = p1.X; x <= p2.X; x++)
                    {
                        index = x + width * (z + y * length);
                        block = lvl.FastGetBlock(index);

                        if (fixGrass && lvl.Props[block].GrassBlock != Block.Invalid)
                        {
                            above = y == maxY ? Block.Air : lvl.FastGetBlock(index + oneY);
                            ushort grass = lvl.Props[block].GrassBlock;

                            if (lvl.LightPasses(above)) output(Place(x, y, z, grass));
                        }
                        else if (fixDirt && lvl.Props[block].DirtBlock != Block.Invalid)
                        {
                            above = y == maxY ? Block.Air : lvl.FastGetBlock(index + oneY);
                            ushort dirt = lvl.Props[block].DirtBlock;

                            if (!lvl.LightPasses(above)) output(Place(x, y, z, dirt));
                        }
                    }
        }

        public void FixLight(DrawOpOutput output)
        {
            Level lvl = Level;
            int oneY = lvl.Width * lvl.Length;
            int index, width = lvl.Width, length = lvl.Length;
            ushort above, block;

            Vec3U16 p1 = Clamp(Min), p2 = Clamp(Max);
            for (ushort y = p1.Y; y <= p2.Y; y++)
                for (ushort z = p1.Z; z <= p2.Z; z++)
                    for (ushort x = p1.X; x <= p2.X; x++)
                    {
                        index = x + width * (z + y * length);
                        block = lvl.FastGetBlock(index);
                        bool inShadow = false;

                        if (lvl.Props[block].GrassBlock != Block.Invalid)
                        {
                            for (int i = 1; i < (lvl.Height - y); i++)
                            {
                                above = lvl.FastGetBlock(index + oneY * i);
                                if (!lvl.LightPasses(above)) { inShadow = true; break; }
                            }

                            ushort grass = lvl.Props[block].GrassBlock;
                            if (!inShadow) output(Place(x, y, z, grass));
                        }
                        else if (lvl.Props[block].DirtBlock != Block.Invalid)
                        {
                            for (int i = 1; i < (lvl.Height - y); i++)
                            {
                                above = lvl.FastGetBlock(index + oneY * i);
                                if (!lvl.LightPasses(above)) { inShadow = true; break; }
                            }

                            ushort dirt = lvl.Props[block].DirtBlock;
                            if (inShadow) output(Place(x, y, z, dirt));
                        }
                    }
        }
    }
}