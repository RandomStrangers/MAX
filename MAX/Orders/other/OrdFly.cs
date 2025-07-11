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
using MAX.Maths;
using MAX.Tasks;
using System;
using System.Collections.Generic;

namespace MAX.Orders.Misc
{
    public class OrdFly : Order
    {
        public override string Name { get { return "Fly"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (!Hacks.CanUseFly(p))
            {
                p.Message("You cannot use &T/Fly &Son this map.");
                p.isFlying = false; return;
            }

            p.isFlying = !p.isFlying;
            if (!p.isFlying) return;

            p.Message("You are now flying. &cJump!");

            FlyState state = new FlyState
            {
                player = p
            };
            SchedulerTask task = new SchedulerTask(FlyCallback, state, TimeSpan.Zero, true);
            p.CriticalTasks.Add(task);
        }

        public class FlyState
        {
            public Player player;
            public Position oldPos = default;
            public List<Vec3U16> lastGlass = new List<Vec3U16>();
            public List<Vec3U16> glassCoords = new List<Vec3U16>();
        }

        public static void FlyCallback(SchedulerTask task)
        {
            FlyState state = (FlyState)task.State;
            Player p = state.player;
            if (state.player.isFlying) { DoFly(state); return; }

            foreach (Vec3U16 pos in state.lastGlass)
            {
                p.SendBlockchange(pos.X, pos.Y, pos.Z, Block.Air);
            }
            p.Message("Stopped flying");
            task.Repeating = false;
        }

        public static void DoFly(FlyState state)
        {
            Player p = state.player;
            if (p.Pos == state.oldPos) return;

            int x = p.Pos.BlockX, z = p.Pos.BlockZ;
            int y = (p.Pos.Y - 60) / 32;

            for (int yy = y - 1; yy <= y; yy++)
                for (int zz = z - 2; zz <= z + 2; zz++)
                    for (int xx = x - 2; xx <= x + 2; xx++)
                    {
                        Vec3U16 pos;
                        pos.X = (ushort)xx; pos.Y = (ushort)yy; pos.Z = (ushort)zz;
                        if (p.level.IsAirAt(pos.X, pos.Y, pos.Z)) state.glassCoords.Add(pos);
                    }

            foreach (Vec3U16 P in state.glassCoords)
            {
                if (state.lastGlass.Contains(P)) continue;
                state.lastGlass.Add(P);
                p.SendBlockchange(P.X, P.Y, P.Z, Block.Glass);
            }

            for (int i = 0; i < state.lastGlass.Count; i++)
            {
                Vec3U16 P = state.lastGlass[i];
                if (state.glassCoords.Contains(P)) continue;

                p.RevertBlock(P.X, P.Y, P.Z);
                state.lastGlass.RemoveAt(i); i--;
            }

            state.glassCoords.Clear();
            state.oldPos = p.Pos;
        }

        public override void Help(Player p)
        {
            string name = Group.GetColoredName(LevelPermission.Operator);
            p.Message("&T/Fly");
            p.Message("&HCreates a glass platform underneath you that moves with you.");
            p.Message("&H  May not work if you have high latency.");
            p.Message("&H  Cannot be used on maps which have -hax in their motd. " +
                           "(unless you are {0}&H+ and the motd has +ophax)", name);
        }
    }
}