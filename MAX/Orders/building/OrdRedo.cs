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
using MAX.Drawing.Ops;
using MAX.Maths;
using MAX.Undo;

namespace MAX.Orders.Building
{
    public class OrdRedo : Order
    {
        public override string Name { get { return "Redo"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length > 0) { Help(p); return; }
            PerformRedo(p);
        }

        public static void PerformRedo(Player p)
        {
            UndoDrawOpEntry[] entries = p.DrawOps.Items;
            if (entries.Length == 0)
            {
                p.Message("You have no &T/Undo &Sor &T/Undo [seconds] &Sto redo."); return;
            }

            for (int i = entries.Length - 1; i >= 0; i--)
            {
                UndoDrawOpEntry entry = entries[i];
                if (entry.DrawOpName != "UndoSelf") continue;
                p.DrawOps.Remove(entry);

                RedoSelfDrawOp op = new RedoSelfDrawOp
                {
                    Start = entry.Start,
                    End = entry.End
                };
                DrawOpPerformer.Do(op, null, p, new Vec3S32[] { Vec3U16.MinVal, Vec3U16.MaxVal });
                p.Message("Redo performed.");
                return;
            }
            p.Message("No &T/Undo &Sor &T/Undo [timespan] &Scalls were " +
                               "found in the last 200 draw operations.");
        }

        public override void Help(Player p)
        {
            p.Message("&T/Redo");
            p.Message("&HRedoes last &T/Undo &Hor &T/Undo [timespan] &Hyou performed");
        }
    }
}