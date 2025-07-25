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
using System.Threading;

namespace MAX.Orders.Misc
{
    public class OrdDelay : Order
    {
        public override string Name { get { return "Delay"; } }
        public override string Type { get { return OrderTypes.Other; } }

        public override void Use(Player p, string message, OrderData data)
        {
            TimeSpan duration = TimeSpan.Zero;
            if (!OrderParser.GetTimespan(p, message, ref duration, "wait for", "ms")) return;

            if (duration.TotalSeconds > 60)
            {
                p.Message("&WCan only wait for a minute at most."); return;
            }

            if (data.Context != OrderContext.MessageBlock)
            {
                p.Message("&WThis order can only be used in message blocks."); return;
            }
            Thread.Sleep((int)duration.TotalMilliseconds);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Delay [timespan]");
            p.Message("&HWaits for a certain amount of time.");
            p.Message("&HUse to run a order after a certain delay in a &T/MB");
            p.Message("&H  e.g. &T/MB air /Delay 1000ms |/Help Me &Hruns &T/Help Me " +
                           "&H1000 milliseconds (1 second) after the MB is clicked");
        }
    }
}