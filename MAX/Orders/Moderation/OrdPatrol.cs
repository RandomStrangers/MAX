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
using System;
using System.Collections.Generic;

namespace MAX.Orders.Moderation
{
    public class OrdPatrol : Order
    {
        public override string Name { get { return "Patrol"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Builder, "are not patrolled") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length > 0) { Help(p); return; }

            List<Player> candidates = GetPatrolCandidates(p, data);
            if (candidates.Count == 0)
            {
                p.Message("&WNo players to patrol.");
            }
            else
            {
                Player target = candidates[new Random().Next(candidates.Count)];
                target.LastPatrol = DateTime.UtcNow;

                Find("TP").Use(p, target.name, data);
                p.Message("Now visiting {0}&S.", p.FormatNick(target));
            }
        }

        public List<Player> GetPatrolCandidates(Player p, OrderData data)
        {
            List<Player> candidates = new List<Player>();
            ItemPerms except = OrderExtraPerms.Find(Name, 1);
            Player[] players = PlayerInfo.Online.Items;
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-15);

            foreach (Player target in players)
            {
                if (except.UsableBy(target) || !p.CanSee(target, data.Rank)) continue;
                if (target == p || target.LastPatrol > cutoff) continue;
                candidates.Add(target);
            }
            return candidates;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Patrol");
            ItemPerms except = OrderExtraPerms.Find(Name, 1);
            p.Message("&HTeleports you to a random player. {0} &Hare not patrolled", except.Describe());
            p.Message("&HPlayers patrolled within the last 15 seconds are ignored");
        }
    }
}