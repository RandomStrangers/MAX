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
using MAX.DB;
using System.Collections.Generic;

namespace MAX.Orders.Info
{
    public class OrdTop : Order
    {
        public override string Name { get { return "Top"; } }
        public override string Shortcut { get { return "Most"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("TopTen", "10"), new OrderDesignation("TopFive", "5"),
                    new OrderDesignation("Top10", "10"), };
            }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces();
            if (args.Length < 2) { Help(p); return; }

            int maxResults = 0, offset = 0;
            if (!OrderParser.GetInt(p, args[0], "Max results", ref maxResults, 1, 15)) return;

            TopStat stat = TopStat.Find(args[1]);
            if (stat == null)
            {
                p.Message("&WNo stat found with name \"{0}\".", args[1]); return;
            }

            if (args.Length > 2)
            {
                if (!OrderParser.GetInt(p, args[2], "Offset", ref offset, 0)) return;
            }

            List<TopResult> results = stat.GetResults(maxResults, offset);
            p.Message("&a{0}:", stat.Title);

            for (int i = 0; i < results.Count; i++)
            {
                p.Message("{0}) {1} &S- {2}", offset + i + 1,
                          stat.FormatName(p, results[i].Name),
                          stat.Formatter(results[i].Value));
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Top [max results] [stat] <offset>");
            p.Message("&HPrints a list of players who have the " +
                       "most/top of a particular stat. Available stats:");
            TopStat.List(p);
        }
    }
}