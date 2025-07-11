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

namespace MAX.Orders.Building
{
    public class OrdOrdBind : Order
    {
        public override string Name { get { return "OrdBind"; } }
        public override string Shortcut { get { return "ob"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Builder; } }
        public override bool SuperUseable { get { return false; } }
        public override bool MessageBlockRestricted { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0)
            {
                bool anyBinds = false;
                foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in p.OrdBindings)
                {
                    p.Message("&T/{0} &Sbound to &T/{1}", kvp.Key, kvp.Value);
                    anyBinds = true;
                }

                if (!anyBinds) p.Message("You currently have no orders bound.");
                return;
            }

            string[] parts = message.SplitSpaces(2);
            string trigger = parts[0];

            if (parts.Length == 1)
            {
                if (!p.OrdBindings.TryGetValue(trigger, out string value))
                {
                    p.Message("No order bound for &T/{0}", trigger);
                }
                else
                {
                    p.Message("&T/{0} &Sbound to &T/{1}", trigger, value);
                }
            }
            else
            {
                p.OrdBindings[trigger] = parts[1];
                p.Message("Bound &T/{1} &Sto &T/{0}", trigger, parts[1]);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/OrdBind [shortcut] [order]");
            p.Message("&HBinds [shortcut] to [order]");
            p.Message("&H  Use with \"&T/[shortcut]&H\" &f(example: &T/2&f)");
            p.Message("&T/OrdBind [shortcut]");
            p.Message("&HLists the order currently bound to [shortcut]");
            p.Message("&T/OrdBind &H");
            p.Message("&HLists all currently bound orders");
        }
    }
}