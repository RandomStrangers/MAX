﻿/*
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
using MAX.Eco;
using MAX.Events.EconomyEvents;

namespace MAX.Orders.Eco
{
    public abstract class MoneyOrd : Order
    {
        public override string Type { get { return OrderTypes.Economy; } }

        public bool ParseArgs(Player p, string message, ref bool all,
                                 out EcoTransaction data)
        {
            data = new EcoTransaction();
            string[] args = message.SplitSpaces(3);
            if (args.Length < 2) { Help(p); return false; }

            if (!Economy.CheckIsEnabled(p, this)) return false;

            data.TargetName = args[0];
            data.Reason = args.Length > 2 ? args[2] : null;
            data.Source = p;

            all = all && args[1].CaselessEq("all");
            return all || OrderParser.GetInt(p, args[1], "Amount", ref data.Amount, 1);
        }
    }
}