/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified for use with MCForge)
 
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
using MAX.Scripting;

namespace MAX.Orders.Scripting
{
    public class OrdOrdUnload : Order2
    {
        public override string name { get { return "OrdUnload"; } }
        public override string type { get { return OrderTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }
        public override bool MessageBlockRestricted { get { return true; } }

        public override void Use(Player p, string ordName, OrderData data)
        {
            if (ordName.Length == 0) 
            { 
                Help(p);
                return; 
            }

            string ordArgs = "";
            Search(ref ordName, ref ordArgs);
            Order ord = Find(ordName);

            if (ord == null)
            {
                p.Message("\"{0}\" is not a valid or loaded order.", ordName); 
                return;
            }
            ScriptingOperations.UnloadOrder(p, ord);
        }

        public override void Help(Player p)
        {
            p.Message("&T/OrdUnload [order]");
            p.Message("&HUnloads a order from the server.");
        }
    }
}
