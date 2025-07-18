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

namespace MAX.Orders.Info
{
    public class OrdWhoNick : Order
    {
        public override string Name { get { return "WhoNick"; } }
        public override string Shortcut { get { return "RealName"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);

            if (args.Length > 1 && args[0].CaselessEq("bot"))
            {
                ForBot(p, args[1]);
                return;
            }
            ForPlayer(p, message);
        }

        public static void ForPlayer(Player p, string nick)
        {
            nick = Colors.Strip(nick);
            Player[] players = PlayerInfo.Online.Items;

            Player match = Matcher.Find(p, nick, out int matches, players, pl => p.CanSee(pl),
                                        pl => Colors.Strip(pl.DisplayName),
                                        pl => pl.ColoredName + " &S(" + pl.name + ")",
                                        "online player nicks");
            if (match == null) return;
            p.Message("The player nicknamed {0} &Sis named {1}", match.DisplayName, match.name);
        }

        public static void ForBot(Player p, string nick)
        {
            nick = Colors.Strip(nick);
            PlayerBot[] bots = p.level.Bots.Items;

            PlayerBot match = Matcher.Find(p, nick, out int matches, bots, bot => true,
                                           bot => Colors.Strip(bot.DisplayName),
                                           bot => bot.ColoredName + " &S(" + bot.name + ")",
                                           "bot nicknames");
            if (match == null) return;
            p.Message("The bot nicknamed {0} &Sis named {1}", match.DisplayName, match.name);
        }

        public override void Help(Player p)
        {
            p.Message("&T/WhoNick [nickname]");
            p.Message("&HDisplays the player's real username");
            p.Message("&T/WhoNick bot [nickname]");
            p.Message("&HDisplays the bot's real name");
        }
    }
}