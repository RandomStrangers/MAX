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
using MAX.Maths;
using MAX.Tasks;
using System;

namespace MAX.Orders.Misc
{
    public class OrdRide : Order
    {
        public override string Name { get { return "Ride"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override bool MuseumUsable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            p.onTrain = !p.onTrain;
            if (!p.onTrain) return;

            p.trainInvincible = true;
            p.Message("Stand near a train to mount it");

            SchedulerTask task = new SchedulerTask(RideCallback, p, TimeSpan.Zero, true);
            p.CriticalTasks.Add(task);
        }

        public static void RideCallback(SchedulerTask task)
        {
            Player p = (Player)task.State;
            if (!p.onTrain)
            {
                p.trainGrab = false;
                p.Message("Dismounted");

                Server.MainScheduler.QueueOnce(TrainInvincibleCallback, p,
                                               TimeSpan.FromSeconds(1));
                task.Repeating = false;
                return;
            }

            Vec3S32 P = p.Pos.FeetBlockCoords;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        ushort xx = (ushort)(P.X + dx), yy = (ushort)(P.Y + dy), zz = (ushort)(P.Z + dz);
                        if (p.level.GetBlock(xx, yy, zz) != Block.Train) continue;
                        p.trainGrab = true;

                        Vec3F32 dir = new Vec3F32(dx, 0, dz);
                        DirUtils.GetYawPitch(dir, out byte yaw, out byte pitch);

                        if (dy == 1) pitch = 240;
                        else if (dy == 0) pitch = 0;
                        else pitch = 8;

                        if (dx != 0 || dy != 0 || dz != 0)
                        {
                            Position pos = Position.FromFeetBlockCoords(P.X + dx, P.Y + dy, P.Z + dz);
                            p.SendPos(Entities.SelfID, pos, new Orientation(yaw, pitch));
                        }
                        return;
                    }
            p.trainGrab = false;
        }

        public static void TrainInvincibleCallback(SchedulerTask task)
        {
            Player p = (Player)task.State;
            p.trainInvincible = false;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Ride");
            p.Message("&HRides a nearby train.");
        }
    }
}