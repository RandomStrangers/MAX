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
using MAX.Tasks;
using MAX.Util;
using System;
using System.Text;

namespace MAX.Orders.Chatting
{
    public class Ord8Ball : Order
    {
        public override string Name { get { return "8ball"; } }
        public override string Shortcut { get { return ""; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UseableWhenJailed { get { return true; } }

        public static DateTime nextUse;
        public static TimeSpan delay = TimeSpan.FromSeconds(2);

        public override void Use(Player p, string question, OrderData data)
        {
            if (!MessageOrd.CanSpeak(p, Name)) return;
            if (question.Length == 0) { Help(p); return; }

            TimeSpan delta = nextUse - DateTime.UtcNow;
            if (delta.TotalSeconds > 0)
            {
                p.Message("The 8-ball is still recharging, wait another {0} seconds.",
                               (int)Math.Ceiling(delta.TotalSeconds));
                return;
            }
            nextUse = DateTime.UtcNow.AddSeconds(10 + 2);

            StringBuilder builder = new StringBuilder(question.Length);
            foreach (char c in question)
            {
                if (char.IsLetterOrDigit(c)) builder.Append(c);
            }

            string msg = p.ColoredName + " &Sasked the &b8-Ball: &f" + question;
            Chat.Message(ChatScope.Global, msg, null, Filter8Ball);

            string final = builder.ToString();
            Server.MainScheduler.QueueOnce(EightBallCallback, final, delay);
        }

        public static void EightBallCallback(SchedulerTask task)
        {
            string final = (string)task.State;
            Random random = new Random(final.ToLower().GetHashCode());

            TextFile file = TextFile.Files["8ball"];
            file.EnsureExists();
            string[] messages = file.GetText();

            string msg = "The &b8-Ball &Ssays: &f" + messages[random.Next(messages.Length)];
            Chat.Message(ChatScope.Global, msg, null, Filter8Ball);
        }

        public static bool Filter8Ball(Player p, object arg) { return !p.Ignores.EightBall; }
        public override void Help(Player p)
        {
            p.Message("&T/8ball [yes or no question]");
            p.Message("&HGet an answer from the all-knowing 8-Ball!");
        }
    }
}