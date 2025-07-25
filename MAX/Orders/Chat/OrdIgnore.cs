/*
    Written by Jack1312
  
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

namespace MAX.Orders.Chatting
{
    public class OrdIgnore : Order
    {
        public override string Name { get { return "Ignore"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override bool SuperUseable { get { return false; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("Deafen", "all") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();
            string action = args[0];

            if (action.CaselessEq("all"))
            {
                Toggle(p, ref p.Ignores.All, "{0} ignoring all chat"); return;
            }
            else if (action.CaselessEq("irc"))
            {
                if (args.Length > 1) { IgnoreIRCNick(p, args[1]); }
                else { Toggle(p, ref p.Ignores.IRC, "{0} ignoring IRC chat"); }
                return;
            }
            else if (action.CaselessEq("titles"))
            {
                Toggle(p, ref p.Ignores.Titles, "{1}Player titles {0} show before names in chat"); return;
            }
            else if (action.CaselessEq("nicks"))
            {
                Toggle(p, ref p.Ignores.Nicks, "{1}Custom player nicks {0} show in chat");
                TabList.Update(p, true); return;
            }
            else if (action.CaselessEq("8ball"))
            {
                Toggle(p, ref p.Ignores.EightBall, "{0} ignoring &T/8ball"); return;
            }
            else if (action.CaselessEq("drawoutput"))
            {
                Toggle(p, ref p.Ignores.DrawOutput, "{0} ignoring draw order output"); return;
            }
            else if (action.CaselessEq("worldchanges"))
            {
                Toggle(p, ref p.Ignores.WorldChanges, "{0} ignoring world changes"); return;
            }
            else if (IsListOrder(action))
            {
                p.Ignores.Output(p); return;
            }

            if (p.Ignores.Names.CaselessRemove(action))
            {
                p.Message("&aNo longer ignoring {0}", action);
            }
            else
            {
                Player target = PlayerInfo.FindMatches(p, action, out int matches);
                if (target == null)
                {
                    if (matches == 0) p.Message("You must use the full name when unignoring offline players.");
                    return;
                }

                if (p.Ignores.Names.CaselessRemove(target.name))
                {
                    p.Message("&aNo longer ignoring {0}", p.FormatNick(target));
                }
                else
                {
                    p.Ignores.Names.Add(target.name);
                    p.Message("&cNow ignoring {0}", p.FormatNick(target));
                }
            }
            p.Ignores.Save(p);
        }

        public static void Toggle(Player p, ref bool ignore, string format)
        {
            ignore = !ignore;
            if (format.StartsWith("{0}"))
            {
                p.Message(format, ignore ? "&cNow" : "&aNo longer");
            }
            else
            {
                p.Message(format, ignore ? "no longer" : "now", ignore ? "&c" : "&a");
            }
            p.Ignores.Save(p);
        }

        public static void IgnoreIRCNick(Player p, string nick)
        {
            if (p.Ignores.IRCNicks.CaselessRemove(nick))
            {
                p.Message("&aNo longer ignoring IRC nick: {0}", nick);
            }
            else
            {
                p.Ignores.IRCNicks.Add(nick);
                p.Message("&cNow ignoring IRC nick: {0}", nick);
            }
            p.Ignores.Save(p);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Ignore [name]");
            p.Message("&HSee &T/Help ignore special &Hfor special names when ignoring.");
            p.Message("&HOtherwise, all chat from the player with [name] is ignored.");
            p.Message("&HUsing the same [name] again will unignore.");
        }

        public override void Help(Player p, string message)
        {
            if (!message.CaselessEq("special")) { Help(p); return; }
            p.Message("&HSpecial names for &T/Ignore [name]");
            p.Message("&H all - all chat is ignored.");
            p.Message("&H irc - IRC chat is ignored.");
            p.Message("&H irc [nick] - IRC chat by that IRC nick ignored.");
            p.Message("&H titles - player titles before names are ignored.");
            p.Message("&H nicks - custom player nicks do not show in chat.");
            p.Message("&H 8ball - &T/8ball &His ignored.");
            p.Message("&H drawoutput - drawing order output is ignored.");
            p.Message("&H worldchanges - world change messages are ignored.");
        }
    }
}