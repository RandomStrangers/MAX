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
using MAX.Blocks.Extended;
using MAX.Blocks.Physics;
using MAX.Maths;
using System;

namespace MAX.Blocks
{

    public static class DeleteBehaviour
    {

        public static ChangeResult RocketStart(Player p, ushort _, ushort x, ushort y, ushort z)
        {
            if (p.level.Physics < 2 || p.level.Physics == 5) return ChangeResult.Unchanged;

            DirUtils.EightYaw(p.Rot.RotY, out int dx, out int dz);
            DirUtils.Pitch(p.Rot.HeadX, out int dy);

            // Looking straight up or down
            byte pitch = p.Rot.HeadX;
            if (pitch >= 192 && pitch <= 196 || pitch >= 60 && pitch <= 64) { dx = 0; dz = 0; }
            Vec3U16 head = new Vec3U16((ushort)(x + dx * 2), (ushort)(y + dy * 2), (ushort)(z + dz * 2));
            Vec3U16 tail = new Vec3U16((ushort)(x + dx), (ushort)(y + dy), (ushort)(z + dz));

            bool headFree = p.level.IsAirAt(head.X, head.Y, head.Z) && p.level.CheckClear(head.X, head.Y, head.Z);
            bool tailFree = p.level.IsAirAt(tail.X, tail.Y, tail.Z) && p.level.CheckClear(tail.X, tail.Y, tail.Z);
            if (headFree && tailFree)
            {
                p.level.Blockchange(head.X, head.Y, head.Z, Block.RocketHead);
                p.level.Blockchange(tail.X, tail.Y, tail.Z, Block.LavaFire);
            }
            return ChangeResult.Unchanged;
        }

        public static ChangeResult Firework(Player p, ushort _, ushort x, ushort y, ushort z)
        {
            if (p.level.Physics == 0 || p.level.Physics == 5) return ChangeResult.Unchanged;

            Random rand = new Random();
            // Offset the firework randomly
            Vec3U16 pos = new Vec3U16(0, 0, 0)
            {
                X = (ushort)(x + rand.Next(0, 2) - 1),
                Z = (ushort)(z + rand.Next(0, 2) - 1)
            };
            ushort headY = (ushort)(y + 2), tailY = (ushort)(y + 1);

            bool headFree = p.level.IsAirAt(pos.X, headY, pos.Z) && p.level.CheckClear(pos.X, headY, pos.Z);
            bool tailFree = p.level.IsAirAt(pos.X, tailY, pos.Z) && p.level.CheckClear(pos.X, tailY, pos.Z);
            if (headFree && tailFree)
            {
                p.level.Blockchange(pos.X, headY, pos.Z, Block.Fireworks);

                PhysicsArgs args = default;
                args.Type1 = PhysicsArgs.Wait; args.Value1 = 1;
                args.Type2 = PhysicsArgs.Dissipate; args.Value2 = 100;
                p.level.Blockchange(pos.X, tailY, pos.Z, Block.StillLava, false, args);
            }
            return ChangeResult.Unchanged;
        }

        public static ChangeResult C4Det(Player p, ushort _, ushort x, ushort y, ushort z)
        {
            int index = p.level.PosToInt(x, y, z);
            C4Physics.BlowUp(index, p.level);
            return p.ChangeBlock(x, y, z, Block.Air);
        }

        public static ChangeResult RevertDoor(Player _, ushort __, ushort ___, ushort ____, ushort _____)
        {
            return ChangeResult.Unchanged;
        }

        public static ChangeResult Door(Player p, ushort old, ushort x, ushort y, ushort z)
        {
            if (p.level.Physics == 0) return p.ChangeBlock(x, y, z, Block.Air);

            PhysicsArgs args = ActivateablePhysics.GetDoorArgs(old, out ushort physForm);
            p.level.Blockchange(x, y, z, physForm, false, args);
            return ChangeResult.Modified;
        }

        public static ChangeResult ODoor(Player p, ushort old, ushort x, ushort y, ushort z)
        {
            if (old == Block.oDoor_Green || old == Block.oDoor_Green_air)
            {
                ushort oDoorOpposite = p.level.Props[old].oDoorBlock;
                p.level.Blockchange(x, y, z, oDoorOpposite);
                return ChangeResult.Modified;
            }
            return ChangeResult.Unchanged;
        }

        public static ChangeResult DoPortal(Player p, ushort _, ushort x, ushort y, ushort z)
        {
            if (!Portal.Handle(p, x, y, z))
            {
                return p.ChangeBlock(x, y, z, Block.Air);
            }
            return ChangeResult.Unchanged;
        }

        public static ChangeResult DoMessageBlock(Player p, ushort _, ushort x, ushort y, ushort z)
        {
            if (!MessageBlock.Handle(p, x, y, z, true))
            {
                return p.ChangeBlock(x, y, z, Block.Air);
            }
            return ChangeResult.Unchanged;
        }
    }
}