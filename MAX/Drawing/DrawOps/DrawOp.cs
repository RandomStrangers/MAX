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


namespace MAX
{
    public struct DrawOpBlock
    {
        public ushort X, Y, Z;
        public ushort Block;
    }
}

namespace MAX.Drawing.Ops
{
    /// <summary> Performs on action on a block output from a draw operation. </summary>
    public delegate void DrawOpOutput(DrawOpBlock block);

    public class DrawOp
    {
        //public long TotalAffected; // blocks affected by the draw operation
        public int TotalModified; // blocks actually modified (e.g. some may not be due to permissions)

        /// <summary> Minimum coordinates of the bounds of this draw operation </summary>
        public Vec3S32 Min;

        /// <summary> Maximum coordinates of the bounds of this draw operation </summary>
        public Vec3S32 Max;

        /// <summary> Coordinates of the first point selected by the player </summary>
        public Vec3S32 Origin;

        /// <summary> Coordinates of the current block being processed by this draw operation </summary>
        /// <remarks> Note: You should treat this as coordinates, it is a DrawOpBlock struct for performance reasons. </remarks>
        public DrawOpBlock Coords;

        /// <summary> Player that is executing this draw operation </summary>
        public Player Player;

        /// <summary> Level that this draw operation is being performed on </summary>
        public Level Level;

        /// <summary> BlockDB change flags for blocks affected by this draw operation </summary>
        public ushort Flags = BlockDBFlags.Drawn;

        /// <summary> Lock held on the associated level's BlockDB. Can be null and usually is null. </summary>
        public IDisposable BlockDBReadLock;

        /// <summary> Whether this draw operation can be undone. </summary>
        public bool Undoable = true;

        /// <summary> Whether this draw operation can be used on maps that have drawing disabled. </summary>
        public bool AlwaysUsable;


        public int SizeX { get { return Max.X - Min.X + 1; } }
        public int SizeY { get { return Max.Y - Min.Y + 1; } }
        public int SizeZ { get { return Max.Z - Min.Z + 1; } }


        /// <summary> Human friendly name of the draw operation. </summary>
        public virtual string Name { get; }

        /// <summary> Whether the output of this draw operation is affected by the player's current Transform. </summary>
        public bool AffectedByTransform = true;

        /// <summary> Estimates the total number of blocks that this draw operation may affect. </summary>
        /// <remarks> This estimate assumes that all potentially affected blocks will be changed by the draw operation </remarks>
        public virtual int BlocksAffected(Level lvl, Vec3S32[] marks)
        {
            return 0;
        }

        public virtual void Perform(Vec3S32[] marks, Brush brush, DrawOpOutput output)
        {
        }


        /// <summary> Sets the player and level associated with this draw operation, then called SetMarks </summary>
        public void Setup(Player p, Level lvl, Vec3S32[] marks)
        {
            Player = p;
            Level = lvl;
            clip = new Vec3S32(lvl.Width - 1, lvl.Height - 1, lvl.Length - 1);

            SetMarks(marks);
        }

        public virtual bool CanDraw(Vec3S32[] marks, Player p, long affected)
        {
            if (affected <= p.group.DrawLimit) return true;
            p.Message("You tried to draw " + affected + " blocks.");
            p.Message("You cannot draw more than " + p.group.DrawLimit + ".");
            return false;
        }

        public virtual void SetMarks(Vec3S32[] marks)
        {
            Origin = marks[0]; Min = marks[0]; Max = marks[0];
            for (int i = 1; i < marks.Length; i++)
            {
                Min = Vec3S32.Min(Min, marks[i]);
                Max = Vec3S32.Max(Max, marks[i]);
            }
        }


        public DrawOpBlock Place(ushort x, ushort y, ushort z, Brush brush)
        {
            Coords.X = x; Coords.Y = y; Coords.Z = z;
            Coords.Block = brush.NextBlock(this);
            return Coords;
        }

        public DrawOpBlock Place(ushort x, ushort y, ushort z, ushort block)
        {
            Coords.X = x; Coords.Y = y; Coords.Z = z;
            Coords.Block = block;
            return Coords;
        }

        public Vec3S32 clip = new Vec3S32(ushort.MaxValue);
        public Vec3U16 Clamp(Vec3S32 pos)
        {
            pos.X = Math.Max(0, Math.Min(pos.X, clip.X));
            pos.Y = Math.Max(0, Math.Min(pos.Y, clip.Y));
            pos.Z = Math.Max(0, Math.Min(pos.Z, clip.Z));
            return new Vec3U16((ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z);
        }
    }
}