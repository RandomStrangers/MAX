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
using MAX.Bots;

namespace MAX.Orders.Chatting
{
    public class OrdNick : EntityPropertyOrd
    {
        public override string Name { get { return "Nick"; } }
        public override string Shortcut { get { return "Nickname"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override OrderPerm[] ExtraPerms
        {
            get
            {
                return new[] { new OrderPerm(LevelPermission.Operator, "can change the nick of others"),
                    new OrderPerm(LevelPermission.Operator, "can change the nick of bots") };
            }
        }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("xnick", "-own") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            UseBotOrPlayer(p, data, message, "nick");
        }

        public override void SetBotData(Player p, PlayerBot bot, string nick)
        {
            if (!MessageOrd.CanSpeak(p, Name)) return;

            if (nick.Length == 0)
            {
                bot.DisplayName = bot.name;
                p.level.Message("Bot " + bot.ColoredName + " &Sreverted to their original name.");
            }
            else
            {
                string nameTag = nick.CaselessEq("empty") ? "" : nick;
                if (Colors.StripUsed(nick).Length > 62) { p.Message("Name must be 62 or fewer letters."); return; }

                p.Message("You changed the name of bot " + bot.ColoredName + " &Sto &c" + nameTag);
                bot.DisplayName = Colors.Escape(nick);
            }

            bot.GlobalDespawn();
            bot.GlobalSpawn();
            BotsFile.Save(p.level);
        }

        public override void SetPlayerData(Player p, string target, string nick)
        {
            PlayerOperations.SetNick(p, target, nick);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Nick [player] [nick]");
            p.Message("&HSets the nick of that player.");
            p.Message("&H  If [nick] is not given, reverts [player]'s nick to their account name.");
            p.Message("&T/Nick bot [bot] [name]");
            p.Message("&HSets the name shown above that bot in game.");
            p.Message("&H  If [name] is \"empty\", the bot will not have a name shown.");
        }
    }
}