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
using MAX.DB;
using MAX.Drawing.Brushes;
using MAX.Maths;
using System;


namespace MAX.Drawing.Ops
{
    public class HighlightDrawOp : DrawOp
    {
        public override string Name { get { return "Highlight"; } }

        // Some servers like to set custom default highlight blocks due to using custom blocks
        public static ushort DefaultPlaceHighlight = Block.Green;
        public static ushort DefaultDeleteHighlight = Block.Red;

        /// <summary> Point in time that the /highlight should go backwards up to. </summary>
        public DateTime Start = DateTime.MinValue;

        /// <summary> Block to highlight placements with. </summary>
        public ushort PlaceHighlight = DefaultPlaceHighlight;

        /// <summary> Block to highlight deletions with. </summary>
        public ushort DeleteHighlight = DefaultDeleteHighlight;


        public string who;
        public int[] ids;
        public int totalChanges = 0;

        public HighlightDrawOp()
        {
            Flags = 0;
            Undoable = false;
            AffectedByTransform = false;
        }

        public override int BlocksAffected(Level lvl, Vec3S32[] marks) { return -1; }

        public override void Perform(Vec3S32[] marks, Brush brush, DrawOpOutput output)
        {
            this.output = output;
            PerformHighlight();
            this.output = null;
        }

        public void PerformHighlight()
        {
            if (ids.Length == 0) return;

            // can't use "using" as it creates a local var, and read lock reference may be changed by DrawOpPerformer class
            try
            {
                BlockDBReadLock = Level.BlockDB.Locker.AccquireRead();
                if (Level.BlockDB.FindChangesBy(ids, Start, DateTime.MaxValue, out dims, HighlightBlock)) return;
            }
            finally
            {
                BlockDBReadLock?.Dispose();
            }
        }

        public DrawOpOutput output;
        public Vec3U16 dims;

        public void HighlightBlock(BlockDBEntry e)
        {
            ushort oldBlock = e.OldBlock;
            if (oldBlock == Block.Invalid) return; // Exported BlockDB SQL table entries don't have previous block
            ushort newBlock = e.NewBlock;

            ushort highlight = (newBlock == Block.Air
                                  || Block.Convert(oldBlock) == Block.Water || oldBlock == Block.StillWater
                                  || Block.Convert(oldBlock) == Block.Lava || oldBlock == Block.StillLava)
                ? DeleteHighlight : PlaceHighlight;

            int x = e.Index % dims.X;
            int y = e.Index / dims.X / dims.Z;
            int z = e.Index / dims.X % dims.Z;

            if (x < Min.X || y < Min.Y || z < Min.Z) return;
            if (x > Max.X || y > Max.Y || z > Max.Z) return;
            output(Place((ushort)x, (ushort)y, (ushort)z, highlight));
            totalChanges++;
        }
    }
}