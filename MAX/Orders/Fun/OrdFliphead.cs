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
namespace MAX.Orders.Fun
{
    public class OrdFlipHead : Order
    {
        public override string Name { get { return "FlipHead"; } }
        public override string Type { get { return OrderTypes.Other; } }

        public override void Use(Player p, string message, OrderData data)
        {
            p.flipHead = !p.flipHead;
            p.Message("Your head was {0}&S!", p.flipHead ? "&cbroken" : "&ahealed");
        }

        public override void Help(Player p)
        {
            p.Message("&T/FlipHead");
            p.Message("&HMakes your head appear upside down to other players");
        }
    }
}