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

namespace MAX.Orders.Misc
{
    public class OrdFakeRank : Order
    {
        public override string Name { get { return "FakeRank"; } }
        public override string Shortcut { get { return "frk"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces();
            if (message.Length == 0 || args.Length < 2) { Help(p); return; }
            Player who = PlayerInfo.FindMatches(p, args[0]);
            Group newRank = Matcher.FindRanks(p, args[1]);
            if (who == null || newRank == null) return;

            if (!CheckRank(p, data, who, "fakerank", true)) return;
            DoFakerank(who, newRank);
        }

        public static void DoFakerank(Player who, Group newRank)
        {
            if (newRank.Permission == LevelPermission.Banned)
            {
                Chat.MessageGlobal("{0} &Swas &8banned&S.", who.ColoredName);
            }
            else
            {
                string reason = newRank.Permission >= who.Rank ? Server.Config.DefaultPromoteMessage : Server.Config.DefaultDemoteMessage;
                string direction = newRank.Permission >= who.Rank ? " &Swas promoted to " : " &Swas demoted to ";
                string rankMsg = who.ColoredName + direction + newRank.ColoredName + "&S. (" + reason + "&S)";

                Chat.MessageGlobal(rankMsg);
                who.Message("You are now ranked {0}&S, type /Help for your new set of orders.", newRank.ColoredName);
            }
            who.UpdateColor(newRank.Color);
        }

        public override void Help(Player p)
        {
            p.Message("&T/FakeRank [player] [rank]");
            p.Message("&HGives [player] the appearance of being ranked to [rank].");
        }
    }
}