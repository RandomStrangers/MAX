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
using MAX.Orders.Chatting;

namespace MAX.Core
{

    public static class ChatHandler
    {

        public static void HandleOnChat(ChatScope scope, Player source, string msg,
                                          object arg, ref ChatMessageFilter filter, bool irc)
        {
            msg = msg.Replace("λFULL", source.name).Replace("λNICK", source.name);
            LogType logType = LogType.PlayerChat;

            if (scope == ChatScope.Perms)
            {
                logType = LogType.StaffChat;
            }
            else if (scope == ChatScope.Rank)
            {
                logType = LogType.RankChat;
            }

            if (scope != ChatScope.PM) Logger.Log(logType, msg);
        }

        public static void HandleOrder(Player p, string ord, string args, OrderData data)
        {
            if (!Server.Config.CoreSecretOrders) return;
            // DO NOT REMOVE THE TWO ORDERS BELOW, /PONY AND /RAINBOWDASHLIKESCOOLTHINGS. -EricKilla
            if (ord.ToLower() == "pony")
            {
                p.cancelorder = true;
                if (!MessageOrd.CanSpeak(p, ord)) return;
                int used = p.Extras.GetInt("MAX_PONY");

                if (used < 2)
                {
                    Chat.MessageFrom(p, "λNICK &Sjust so happens to be a proud brony! Everyone give λNICK &Sa brohoof!");
                    Logger.Log(LogType.OrderUsage, "{0} used /{1}", p.name, ord);
                }
                else
                {
                    p.Message("You have used this order 2 times. You cannot use it anymore! Sorry, Brony!");
                }

                p.Extras["MAX_PONY"] = used + 1;
            }
            else if (ord.ToLower() == "rainbowdashlikescoolthings")
            {
                p.cancelorder = true;
                if (!MessageOrd.CanSpeak(p, ord)) return;
                int used = p.Extras.GetInt("MAX_RD");

                if (used < 2)
                {
                    Chat.MessageGlobal("&4T&6H&eI&aS&3 S&9E&1R&4V&6E&eR &aJ&3U&9S&1T &4G&6O&eT &a2&30 &9P&1E&4R&6C&eE&aN&3T &9C&1O&4O&6L&eE&aR&3!");
                    Logger.Log(LogType.OrderUsage, "{0} used /{1}", p.name, ord);
                }
                else
                {
                    p.Message("You have used this order 2 times. You cannot use it anymore! Sorry, Brony!");
                }

                p.Extras["MAX_RD"] = used + 1;
            }
            if (!Server.Config.MCLawlSecretOrders) return;
            if (ord.ToLower() == "care")
            {
                p.cancelorder = true;
                int used = p.Extras.GetInt("MAX_CARE");

                if (used < 2)
                {
                    Chat.MessageFrom(p, "λNICK is now loved by Harmony with all her heart. :D");
                    p.Message("Harmony now loves you with all her heart. :D");
                    Logger.Log(LogType.OrderUsage, "{0} used /{1}", p.name, ord);
                }
                else
                {
                    p.Message("You have used this order 2 times. You cannot use it anymore!");
                }

                p.Extras["MAX_CARE"] = used + 1;
            }
            else if (ord.ToLower() == "facepalm")
            {
                p.cancelorder = true;
                int used = p.Extras.GetInt("MAX_FACEPALM");

                if (used < 2)
                {
                    p.Message("Harmony's bot army just simultaneously facepalm'd at your use of this order.");
                    Logger.Log(LogType.OrderUsage, "{0} used /{1}", p.name, ord);
                }
                else
                {
                    p.Message("You have used this order 2 times. You cannot use it anymore!");
                }

                p.Extras["MAX_FACEPALM"] = used + 1;
            }
        }
    }
}