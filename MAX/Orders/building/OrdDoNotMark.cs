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

namespace MAX.Orders.Building
{
    public class OrdDoNotMark : Order
    {
        public override string Name { get { return "DoNotMark"; } }
        public override string Shortcut { get { return "dnm"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations { get { return new[] { new OrderDesignation("dm") }; } }

        public override void Use(Player p, string message, OrderData data)
        {
            p.ClickToMark = !p.ClickToMark;
            p.Message("Click blocks to &T/mark&S: {0}", p.ClickToMark ? "&2ON" : "&4OFF");
        }

        public override void Help(Player p)
        {
            p.Message("&T/DoNotMark");
            p.Message("&HToggles whether clicking blocks adds a marker to a selection. (e.g. &T/cuboid&H)");
        }
    }
}