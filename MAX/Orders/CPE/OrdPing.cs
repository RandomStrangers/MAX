/*
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
using MAX.Network;

namespace MAX.Orders.Chatting
{
    public class OrdPing : Order
    {
        public override string Name { get { return "Ping"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Operator, "can see ping of other players") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (!message.CaselessEq("all"))
            {
                if (message.Length == 0) message = p.name;

                Player who = PlayerInfo.FindMatches(p, message);
                if (who == null) return;

                if (p != who && !CheckExtraPerm(p, data, 1)) return;
                PingList ping = who.Session.Ping;

                if (!who.Supports(CpeExt.TwoWayPing))
                {
                    p.Message("{0} client does not support measuring ping",
                              p == who ? "Your" : p.FormatNick(who) + "&S's");
                }
                else if (ping.Measures() == 0)
                {
                    p.Message("No ping measurements yet. Try again in a bit.");
                }
                else
                {
                    p.Message(p.FormatNick(who) + " &S- " + ping.Format());
                }
            }
            else
            {
                if (!CheckExtraPerm(p, data, 1)) return;
                Player[] players = PlayerInfo.Online.Items;
                p.Message("Ping/latency list of online players: (&aLo&S:&7Avg&S:&cHi&S)ms");

                foreach (Player target in players)
                {
                    if (!p.CanSee(target, data.Rank)) continue;
                    PingList ping = target.Session.Ping;

                    if (ping.Measures() == 0) continue;
                    p.Message(ping.FormatAll() + " &S- " + p.FormatNick(target));
                }
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Ping &H- Outputs details about your ping to the server.");
            p.Message("&T/Ping [player] &H- Outputs ping details for a player.");
            p.Message("&T/Ping all &H- Outputs ping details for all players.");
            p.Message("&cNOTE: &HNot all clients support measuring ping.");
        }
    }
}