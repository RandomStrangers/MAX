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
using MAX.DB;
using MAX.Drawing.Ops;
using MAX.Maths;
using MAX.Orders.World;
using MAX.Undo;
using System;

namespace MAX.Orders.Building
{
    public class OrdUndo : Order
    {
        public override string Name { get { return "Undo"; } }
        public override string Shortcut { get { return "u"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Operator, "can undo physics") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { UndoLastDrawOp(p); return; }
            string[] parts = message.SplitSpaces();
            bool undoPhysics = parts[0].CaselessEq("physics");

            TimeSpan delta = GetDelta(p, p.name, parts, undoPhysics ? 1 : 0);
            if (delta == TimeSpan.MinValue || (!undoPhysics && parts.Length > 1))
            {
                p.Message("If you are trying to undo another player, use &T/UndoPlayer");
                return;
            }

            if (undoPhysics) { UndoPhysics(p, data, delta); }
            else { UndoSelf(p, delta); }
        }

        public void UndoLastDrawOp(Player p)
        {
            UndoDrawOpEntry[] entries = p.DrawOps.Items;
            if (entries.Length == 0)
            {
                p.Message("You have no draw operations to undo.");
                p.Message("Try using &T/Undo [timespan] &Sinstead.");
                return;
            }

            for (int i = entries.Length - 1; i >= 0; i--)
            {
                UndoDrawOpEntry entry = entries[i];
                if (entry.DrawOpName == "UndoSelf") continue;
                p.DrawOps.Remove(entry);

                UndoSelfDrawOp op = new UndoSelfDrawOp
                {
                    who = p.name,
                    ids = NameConverter.FindIds(p.name),

                    Start = entry.Start,
                    End = entry.End
                };
                DrawOpPerformer.Do(op, null, p, new Vec3S32[] { Vec3U16.MinVal, Vec3U16.MaxVal });
                p.Message("Undo performed.");
                return;
            }

            p.Message("Unable to undo any draw operations, as all of the " +
                               "past 50 draw operations are &T/Undo &Sor &T/Undo [timespan]");
            p.Message("Try using &T/Undo [timespan] &Sinstead");
        }

        public void UndoPhysics(Player p, OrderData data, TimeSpan delta)
        {
            if (!CheckExtraPerm(p, data, 1)) return;
            if (!p.CanUse("Physics"))
            {
                p.Message("&WYou can only undo physics if you can use &T/Physics"); return;
            }

            OrdPhysics.SetPhysics(p.level, 0);
            UndoPhysicsDrawOp op = new UndoPhysicsDrawOp
            {
                Start = DateTime.UtcNow.Subtract(delta)
            };
            DrawOpPerformer.Do(op, null, p, new Vec3S32[] { Vec3U16.MinVal, Vec3U16.MaxVal });

            p.level.Message("Physics were undone &b" + delta.Shorten());
            Logger.Log(LogType.UserActivity, "Physics were undone &b" + delta.Shorten());
            p.level.Save(true);
        }

        public void UndoSelf(Player p, TimeSpan delta)
        {
            UndoDrawOp op = new UndoSelfDrawOp
            {
                Start = DateTime.UtcNow.Subtract(delta),
                who = p.name,
                ids = NameConverter.FindIds(p.name)
            };

            DrawOpPerformer.Do(op, null, p, new Vec3S32[] { Vec3U16.MinVal, Vec3U16.MaxVal });
            if (op.found)
            {
                p.Message("Undid your changes for the past &b{0}", delta.Shorten(true));
                Logger.Log(LogType.UserActivity, "{0} undid their own actions for the past {1}",
                           p.name, delta.Shorten(true));
            }
            else
            {
                p.Message("No changes found by you in the past &b{0}", delta.Shorten(true));
            }
        }


        public static TimeSpan GetDelta(Player p, string name, string[] parts, int offset)
        {
            TimeSpan delta = TimeSpan.Zero;
            string timespan = parts.Length > offset ? parts[parts.Length - 1] : "30m";
            bool self = p.name.CaselessEq(name);

            if (timespan.CaselessEq("all"))
            {
                return self ? TimeSpan.FromSeconds(int.MaxValue) : p.group.MaxUndo;
            }
            else if (!OrderParser.GetTimespan(p, timespan, ref delta, "undo the past", "s"))
            {
                return TimeSpan.MinValue;
            }

            if (delta.TotalSeconds == 0)
                delta = TimeSpan.FromMinutes(90);
            if (!self && delta > p.group.MaxUndo)
            {
                p.Message("{0}&Ss may only undo up to {1}",
                          p.group.ColoredName, p.group.MaxUndo.Shorten(true, true));
                return p.group.MaxUndo;
            }
            return delta;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Undo &H- Undoes your last draw operation");
            p.Message("&T/Undo [timespan]");
            p.Message("&HUndoes your blockchanges in the past [timespan]");
            p.Message("&T/Undo physics [timespan] &H- Undoes physics on current map");
        }
    }
}