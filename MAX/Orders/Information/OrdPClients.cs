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
using System.Collections.Generic;
using System.Text;

namespace MAX.Orders.Info
{
    public class OrdPClients : Order
    {
        public override string Name { get { return "PClients"; } }
        public override string Shortcut { get { return "Clients"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            Dictionary<string, List<Player>> clients = new Dictionary<string, List<Player>>();
            Player[] online = PlayerInfo.Online.Items;

            foreach (Player pl in online)
            {
                if (!p.CanSee(pl, data.Rank)) continue;
                string appName = pl.Session.ClientName();

                if (!clients.TryGetValue(appName, out List<Player> usingClient))
                {
                    usingClient = new List<Player>();
                    clients[appName] = usingClient;
                }
                usingClient.Add(pl);
            }

            p.Message("Players using:");
            foreach (KeyValuePair<string, List<Player>> kvp in clients)
            {
                StringBuilder builder = new StringBuilder();
                List<Player> players = kvp.Value;

                for (int i = 0; i < players.Count; i++)
                {
                    string nick = Colors.StripUsed(p.FormatNick(players[i]));
                    builder.Append(nick);
                    if (i < players.Count - 1) builder.Append(", ");
                }
                p.Message("  {0}: &f{1}", kvp.Key, builder.ToString());
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/PClients");
            p.Message("&HLists the clients players are using, and who uses which client.");
        }
    }
}