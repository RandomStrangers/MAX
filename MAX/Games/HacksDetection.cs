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
using System;
using System.Collections.Generic;

namespace MAX.Games
{

    public class HacksDetector
    {
        public List<DateTime> log = new List<DateTime>(5);
        public DateTime lastWarn;
        public Player player;

        public HacksDetector(Player p) { player = p; }

        public void Warn(string action)
        {
            DateTime now = DateTime.UtcNow;
            if (now < lastWarn) return;

            player.Message("&4Do not {0} &W- ops have been warned.", action);
            Chat.MessageFromOps(player, "λNICK &4appears to be " + action + "ing");
            Logger.Log(LogType.SuspiciousActivity, "{0} appears to be {1}ing", player.name, action);
            lastWarn = now.AddSeconds(5);
        }

        public static TimeSpan interval = TimeSpan.FromSeconds(5);
    }

    public class SpeedhackDetector : HacksDetector
    {

        public SpeedhackDetector(Player p) : base(p) { }

        public bool Detect(Position newPos, float moveDist)
        {
            Player p = player;
            if (p.Game.Referee || Hacks.CanUseSpeed(p)) return false;
            int dx = Math.Abs(p.Pos.X - newPos.X), dz = Math.Abs(p.Pos.Z - newPos.Z);

            int maxMove = (int)(moveDist * 32);
            bool speeding = dx >= maxMove || dz >= maxMove;
            if (!speeding || log.AddSpamEntry(5, interval)) return false;

            Warn("speedhack");
            p.SendPosition(p.Pos, p.Rot);
            return true;
        }
    }

    public class NoclipDetector : HacksDetector
    {

        public NoclipDetector(Player p) : base(p) { }

        public bool Detect()
        {
            Player p = player;
            if (p.Game.Referee || Hacks.CanUseNoclip(p)) return false;
            if (!p.IsLikelyInsideBlock() || log.AddSpamEntry(5, interval)) return false;

            Warn("noclip");
            return false;
        }
    }
}