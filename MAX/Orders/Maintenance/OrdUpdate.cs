/*
   Copyright 2011 MCForge

   Dual-licensed under the Educational Community License, Version 2.0 and
   the GNU General Public License, Version 3 (the "Licenses"); you may
   not use this file except in compliance with the Licenses. You may
   obtain a copy of the Licenses at

   http://www.opensource.org/licenses/ecl2.php
   http://www.gnu.org/licenses/gpl-3.0.html

   Unless required by applicable law or agreed to in writing,
   software distributed under the Licenses are distributed on an "AS IS"
   BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
   or implied. See the Licenses for the specific language governing
   permissions and limitations under the Licenses.
*/
namespace MAX.Orders.Maintenance
{
    public sealed class OrdUpdate : Order2
    {
        public override string name { get { return "Update"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return OrderTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Nobody; } }

        public override void Use(Player p, string message, OrderData data)
        {
            DoUpdate(p);
        }
        public static void DoUpdate(Player p)
        {
            if (!CheckPerms(p))
            {
                p.Message("Only MAX or the Server Owner can update the server."); return;
            }
            Updater.PerformUpdate();
        }

        public static bool CheckPerms(Player p)
        {
            if (p.IsMAX) return true;

            if (Server.Config.OwnerName.CaselessEq("Notch")) return false;
            return p.name.CaselessEq(Server.Config.OwnerName);
        }
        public override void Help(Player p)
        {
            p.Message("&T/Update &H- Force updates the server");
        }
    }
}