/*
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
using MAX.DB;
using System;

namespace MAX.Orders.Info
{
    public class OrdBanInfo : Order
    {
        public override string Name { get { return "BanInfo"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override bool MessageBlockRestricted { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (CheckSuper(p, message, "player name")) return;
            if (message.Length == 0) message = p.name;

            string target = PlayerInfo.FindMatchesPreferOnline(p, message);
            if (target == null) return;
            string nick = p.FormatNick(target);

            string tempData = Server.tempBans.Get(target);
            string tempBanner = null, tempReason = null;
            DateTime tempExpiry = DateTime.MinValue;
            if (tempData != null)
            {
                Ban.UnpackTempBanData(tempData, out tempReason, out tempBanner, out tempExpiry);
            }

            bool permaBanned = Group.BannedRank.Players.Contains(target);
            bool isBanned = permaBanned || tempExpiry >= DateTime.UtcNow;
            string msg = nick;
            string ip = PlayerDB.FindIP(target);
            bool ipBanned = ip != null && Server.bannedIP.Contains(ip);

            if (!ipBanned && isBanned) msg += " &Sis &cBANNED";
            else if (!ipBanned && !isBanned) msg += " &Sis not banned";
            else if (ipBanned && isBanned) msg += " &Sand their IP are &cBANNED";
            else msg += " &Sis not banned, but their IP is &cBANNED";

            Ban.GetBanData(target, out string banner, out string reason, out DateTime time, out string prevRank);
            if (banner != null && permaBanned)
            {
                string grpName = Group.GetColoredName(prevRank);
                msg += " &S(Former rank: " + grpName + "&S)";
            }
            p.Message(msg);

            if (tempExpiry >= DateTime.UtcNow)
            {
                TimeSpan delta = tempExpiry - DateTime.UtcNow;
                p.Message("Temp-banned &S by {1} &Sfor another {0}",
                          delta.Shorten(), p.FormatNick(tempBanner));
                if (tempReason.Length > 0)
                {
                    p.Message("Reason: {0}", tempReason);
                }
            }

            if (banner != null)
            {
                DisplayDetails(p, banner, reason, time, permaBanned ? "Banned" : "Last banned");
            }
            else
            {
                p.Message("No previous bans recorded for {0}&S.", nick);
            }
            Ban.GetUnbanData(target, out banner, out reason, out time);
            DisplayDetails(p, banner, reason, time, permaBanned ? "Last unbanned" : "Unbanned");
        }

        public static void DisplayDetails(Player p, string banner, string reason, DateTime time, string type)
        {
            if (banner == null) return;

            TimeSpan delta = DateTime.UtcNow - time;
            p.Message("{0} {1} ago by {2}",
                          type, delta.Shorten(), p.FormatNick(banner));
            p.Message("Reason: {0}", reason);
        }

        public override void Help(Player p)
        {
            p.Message("&T/BanInfo [player]");
            p.Message("&HOutputs information about current and/or previous ban/unban for that player.");
        }
    }
}