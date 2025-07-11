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
namespace MAX.Orders.Building
{
    public class OrdStatic : Order
    {
        public override string Name { get { return "Static"; } }
        public override string Shortcut { get { return "t"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("zz", "cuboid") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            p.staticOrders = !p.staticOrders;
            p.ClearBlockchange();

            p.Message("Static mode: &a" + p.staticOrders);
            if (message.Length == 0 || !p.staticOrders) return;
            data.Context = OrderContext.Static;

            string[] parts = message.SplitSpaces(2);
            string ord = parts[0], args = parts.Length > 1 ? parts[1] : "";
            p.HandleOrder(ord, args, data);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Static [order]");
            p.Message("&HMakes every order a toggle.");
            p.Message("&HIf [order] is given, then that order is used");
        }
    }
}