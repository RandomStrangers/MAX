﻿/*
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
using MAX.Authentication;
using MAX.Network;
using System;

namespace MAX.Core
{
    public static class ConnectingHandler
    {

        public static void HandleConnecting(Player p, string mppass)
        {
            if (p.cancelconnecting) return;
            bool success = HandleConnectingCore(p, mppass);
            if (!success) p.cancelconnecting = true;
        }

        public static bool HandleConnectingCore(Player p, string mppass)
        {
            if (!LoginAuthenticator.VerifyLogin(p, mppass))
            {
                p.Leave(null, "Login failed! Close the game and sign in again.", true); return false;
            }
            if (!CheckTempban(p)) return false;

            if (Server.Config.WhitelistedOnly && !Server.whiteList.Contains(p.name))
            {
                p.Leave(null, Server.Config.DefaultWhitelistMessage, true);
                return false;
            }

            p.group = Group.GroupIn(p.name);
            if (!CheckBanned(p)) return false;
            if (!CheckPlayersCount(p)) return false;
            return true;
        }

        public static bool CheckTempban(Player p)
        {
            try
            {
                string data = Server.tempBans.Get(p.name);
                if (data == null) return true;

                Ban.UnpackTempBanData(data, out string reason, out string banner, out DateTime expiry);

                if (expiry < DateTime.UtcNow)
                {
                    Server.tempBans.Remove(p.name);
                    Server.tempBans.Save();
                }
                else
                {
                    reason = reason.Length == 0 ? "" : " (" + reason + ")";
                    string delta = (expiry - DateTime.UtcNow).Shorten(true);

                    p.Kick(null, "Banned by " + banner + " for another " + delta + reason, true);
                    return false;
                }
            }
            catch { } // TODO log error
            return true;
        }

        public static bool CheckPlayersCount(Player p)
        {
            if (Server.vip.Contains(p.name)) return true;

            Player[] online = PlayerInfo.Online.Items;
            if ((uint)online.Length >= Server.Config.MaxPlayers && !IPUtil.IsPrivate(p.IP))
            {
                p.Leave(null, "Server full!", true); return false;
            }
            if (p.Rank > LevelPermission.Guest) return true;

            online = PlayerInfo.Online.Items;
            uint guests = 0;
            foreach (Player pl in online)
            {
                if (pl.Rank <= LevelPermission.Guest) guests++;
            }
            if (guests < Server.Config.MaxGuests) return true;

            if (Server.Config.GuestLimitNotify) Chat.MessageOps("Guest " + p.truename + " couldn't log in - too many guests.");
            Logger.Log(LogType.Warning, "Guest {0} couldn't log in - too many guests.", p.truename);
            p.Leave(null, "Server has reached max number of guests", true);
            return false;
        }

        public static bool CheckBanned(Player p)
        {
            string ipban = Server.bannedIP.Get(p.ip);
            if (ipban != null)
            {
                ipban = ipban.Length > 0 ? ipban : Server.Config.DefaultBanMessage;
                p.Kick(null, ipban, true);
                return false;
            }
            if (p.Rank != LevelPermission.Banned) return true;

            Ban.GetBanData(p.name, out string banner, out string reason, out DateTime _, out string _);

            if (banner != null)
            {
                p.Kick(null, "Banned by " + banner + ": " + reason, true);
            }
            else
            {
                p.Kick(null, Server.Config.DefaultBanMessage, true);
            }
            return false;
        }
    }
}