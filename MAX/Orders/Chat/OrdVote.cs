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
using MAX.Tasks;
using System;

namespace MAX.Orders.Chatting
{
    public class OrdVote : Order
    {
        public override string Name { get { return "Vote"; } }
        public override string Shortcut { get { return "vo"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            if (!MessageOrd.CanSpeak(p, Name)) return;

            if (Server.voting)
            {
                p.Message("A vote is in progress!"); return;
            }
            Server.voting = true;
            Server.NoVotes = 0; Server.YesVotes = 0;
            Chat.MessageGlobal("&2 VOTE: &S{0} &S(type &2Yes &Sor &cNo &Sin chat)", message);
            Server.MainScheduler.QueueOnce(VoteCallback, null, TimeSpan.FromSeconds(15));
        }

        public void VoteCallback(SchedulerTask task)
        {
            Server.voting = false;
            Chat.MessageGlobal("The votes are in! &2Y: {0} &cN: {1}", Server.YesVotes, Server.NoVotes);
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) pl.voted = false;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Vote [message]");
            p.Message("&HStarts a vote for 15 seconds.");
            p.Message("&HType &TY &Hor &TN &Hinto chat to vote.");
        }
    }
}