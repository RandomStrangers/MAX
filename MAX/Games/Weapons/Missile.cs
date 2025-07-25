﻿/*
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
using MAX.Drawing.Ops;
using MAX.Maths;
using MAX.Tasks;
using System;
using System.Collections.Generic;


namespace MAX.Games
{
    /// <summary> Represents a missile that adjusts the direction it is
    /// travelling in based on the player's current orientation. </remarks>
    public class Missile : Weapon
    {
        public override string Name { get { return "Missile"; } }
        public WeaponType type;

        public override void OnDisabled(Player p) { }

        public override void OnActivated(Vec3F32 dir, ushort block)
        {
            MissileData args = new MissileData
            {
                block = block,
                type = type,
                pos = (Vec3U16)p.Pos.BlockCoords
            };

            SchedulerTask task = new SchedulerTask(MissileCallback, args,
                                                   TimeSpan.FromMilliseconds(100), true);
            p.CriticalTasks.Add(task);
            Disable();
        }

        public class MissileData : AmmunitionData
        {
            public WeaponType type;
            public Vec3U16 pos;
            public List<Vec3S32> buffer = new List<Vec3S32>();
        }

        /// <summary> Called when a missile has collided with a block. </summary>
        /// <returns> true if this block stops the missile, false if it should continue moving. </returns>
        public virtual bool OnHitBlock(MissileData args, Vec3U16 pos, ushort block)
        {
            return true;
        }

        /// <summary> Called when a missile has collided with a player. </summary>
        public virtual void OnHitPlayer(MissileData args, Player pl)
        {
            pl.HandleDeath(Block.Cobblestone, "@p &Swas hit by a missile from " + p.ColoredName);
        }

        public void MissileCallback(SchedulerTask task)
        {
            MissileData args = (MissileData)task.State;
            if (args.moving) { PerformMove(args); return; }

            if (args.visible.Count > 0)
            {
                Vec3U16 pos = args.visible[0];
                args.visible.RemoveAt(0);
                p.level.Blockchange(pos.X, pos.Y, pos.Z, Block.Air, true);
            }
            task.Repeating = args.visible.Count > 0;
        }

        public void PerformMove(MissileData args)
        {
            while (true)
            {
                args.iterations++;
                Vec3U16 target = MissileTarget(args);
                FindNext(target, ref args.pos, args.buffer);

                if (args.iterations <= 3) continue;
                args.moving = MoveMissile(args, args.pos, target);
                return;
            }
        }

        public Vec3U16 MissileTarget(MissileData args)
        {
            args.start = (Vec3U16)p.Pos.BlockCoords;
            args.dir = DirUtils.GetDirVector(p.Rot.RotY, p.Rot.HeadX);
            int i;

            for (i = 1; ; i++)
            {
                Vec3U16 target = args.PosAt(i);
                ushort block = p.level.GetBlock(target.X, target.Y, target.Z);

                if (block == Block.Invalid) break;
                if (block != Block.Air && !args.all.Contains(target)) break;

                Player hit = PlayerAt(p, target, true);
                if (hit != null) return (Vec3U16)hit.Pos.BlockCoords;
            }
            return args.PosAt(i - 1);
        }

        public bool MoveMissile(MissileData args, Vec3U16 pos, Vec3U16 target)
        {
            ushort block = p.level.GetBlock(pos.X, pos.Y, pos.Z);
            if (block != Block.Air && !args.all.Contains(pos) && OnHitBlock(args, pos, block))
                return false;

            p.level.Blockchange(pos.X, pos.Y, pos.Z, args.block);
            args.visible.Add(pos);
            args.all.Add(pos);
            if (HitsPlayer(args, pos)) return false;

            if (pos == target && p.level.Physics >= 3 && args.type >= WeaponType.Explode)
            {
                p.level.MakeExplosion(target.X, target.Y, target.Z, 2);
                return false;
            }

            if (args.visible.Count > 12)
            {
                pos = args.visible[0];
                p.level.Blockchange(pos.X, pos.Y, pos.Z, Block.Air, true);
                args.visible.RemoveAt(0);
            }
            return true;
        }

        public bool HitsPlayer(MissileData args, Vec3U16 pos)
        {
            Player pl = PlayerAt(p, pos, true);
            if (pl == null) return false;

            OnHitPlayer(args, pl);
            return true;
        }

        public void FindNext(Vec3U16 lookedAt, ref Vec3U16 pos, List<Vec3S32> buffer)
        {
            LineDrawOp.DrawLine(pos.X, pos.Y, pos.Z, 2, lookedAt.X, lookedAt.Y, lookedAt.Z, buffer);
            Vec3U16 end = (Vec3U16)buffer[buffer.Count - 1];
            pos.X = end.X; pos.Y = end.Y; pos.Z = end.Z;
            buffer.Clear();
        }
    }

    public class PenetrativeMissile : Missile
    {
        public override string Name { get { return "Penetrative missile"; } }

        public override bool OnHitBlock(MissileData args, Vec3U16 pos, ushort block)
        {
            if (p.level.Physics < 2) return true;

            if (!p.level.Props[block].LavaKills) return true;
            // Penetrative missile goes through blocks lava can go through
            p.level.Blockchange(pos.X, pos.Y, pos.Z, Block.Air);
            return false;
        }
    }

    public class ExplosiveMissile : Missile
    {
        public override string Name { get { return "Explosive missile"; } }

        public override void OnHitPlayer(MissileData args, Player pl)
        {
            if (pl.level.Physics >= 3)
            {
                pl.HandleDeath(Block.Cobblestone, "@p &Swas blown up by " + p.ColoredName, true);
            }
            else
            {
                base.OnHitPlayer(args, pl);
            }
        }

        public override bool OnHitBlock(MissileData args, Vec3U16 pos, ushort block)
        {
            if (p.level.Physics >= 3) p.level.MakeExplosion(pos.X, pos.Y, pos.Z, 1);
            return true;
        }
    }

    public class TeleportMissile : Missile
    {
        public override string Name { get { return "Teleporter missile"; } }

        public override void OnHitPlayer(MissileData args, Player pl)
        {
            args.DoTeleport(p);
        }

        public override bool OnHitBlock(MissileData args, Vec3U16 pos, ushort block)
        {
            args.DoTeleport(p);
            return true;
        }
    }
}