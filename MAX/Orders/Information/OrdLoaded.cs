/*
    Copyright 2012 MCForge
    
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

namespace MAX.Orders.Info
{
    public class OrdLoaded : Order
    {
        public override string Name { get { return "Loaded"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            Level[] loaded = LevelInfo.Loaded.Items;
            p.Message("Loaded levels [physics level] (&c[no] &Sif not visitable): ");
            Paginator.Output(p, loaded, (lvl) => FormatMap(p, lvl),
                             "Levels", "levels", message);
            p.Message("Use &T/Levels &Sfor all levels.");
        }

        public static string FormatMap(Player p, Level lvl)
        {
            bool canVisit = p.IsSuper || lvl.VisitAccess.CheckAllowed(p);
            string physics = " [" + lvl.Physics + "]";
            string visit = canVisit ? "" : " &c[no]";
            return lvl.ColoredName + physics + visit;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Loaded");
            p.Message("&HLists loaded levels and their physics levels.");
        }
    }
}