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
    public class OrdPaint : Order
    {
        public override string Name { get { return "Paint"; } }
        public override string Shortcut { get { return "p"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length > 0) { Help(p); return; }
            p.painting = !p.painting;

            string type = p.painting ? "&aON" : "&cOFF";
            p.Message("Painting mode: " + type + "&S.");
        }

        public override void Help(Player p)
        {
            p.Message("&T/Paint");
            p.Message("&HTurns painting mode on/off.");
            p.Message("&HWhen paint mode is on, any block you delete is replaced by the block you are holding.");
        }
    }
}