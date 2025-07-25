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
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX.Orders.World
{
    public class OrdGoto : Order
    {
        public override string Name { get { return "Goto"; } }
        public override string Shortcut { get { return "g"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("j"), new OrderDesignation("Join"), new OrderDesignation("gr", "-random"),
                    new OrderDesignation("GotoRandom", "-random"), new OrderDesignation("JoinRandom", "-random") };
            }
        }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }

            if (message.CaselessStarts("-random"))
            {
                string[] files = LevelInfo.AllMapFiles();
                string[] args = message.SplitSpaces(2);
                string map;

                // randomly only visit certain number of maps
                if (args.Length > 1)
                {
                    List<string> maps = Wildcard.Filter(files, args[1],
                                                        mapFile => Path.GetFileNameWithoutExtension(mapFile));
                    if (maps.Count == 0)
                    {
                        p.Message("No maps found containing \"{0}\"", args[1]);
                        return;
                    }
                    map = maps[new Random().Next(maps.Count)];
                }
                else
                {
                    map = files[new Random().Next(files.Length)];
                    map = Path.GetFileNameWithoutExtension(map);
                }

                PlayerActions.ChangeMap(p, map);
            }
            else if (Formatter.ValidMapName(p, message))
            {
                PlayerActions.ChangeMap(p, message);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Goto [map name]");
            p.Message("&HTeleports yourself to a different level.");
            p.Message("&T/Goto -random");
            p.Message("&HTeleports yourself to a random level.");
        }
    }
}