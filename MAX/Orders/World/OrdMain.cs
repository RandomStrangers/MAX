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
using MAX.Games;

namespace MAX.Orders.World
{
    public class OrdMain : Order
    {
        public override string Name { get { return "Main"; } }
        public override string Shortcut { get { return "h"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Admin, "can change the main level") }; }
        }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("WMain"), new OrderDesignation("WorldMain") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0)
            {
                if (p.IsSuper)
                {
                    p.Message("Main level is {0}", Server.mainLevel.ColoredName);
                }
                else if (p.level == Server.mainLevel)
                {
                    if (!IGame.CheckAllowed(p, "use &T/Main")) return;
                    PlayerActions.Respawn(p);
                }
                else
                {
                    PlayerActions.ChangeMap(p, Server.mainLevel);
                }
            }
            else
            {
                if (!CheckExtraPerm(p, data, 1)) return;
                if (!Formatter.ValidMapName(p, message)) return;
                if (!LevelInfo.Check(p, data.Rank, Server.mainLevel, "set main to another map")) return;

                string map = Matcher.FindMaps(p, message);
                if (map == null) return;
                if (!LevelInfo.Check(p, data.Rank, map, "set main to this map")) return;

                Server.SetMainLevel(map);
                Server.Config.MainLevel = map;
                SrvProperties.Save();

                p.Message("Set main level to {0}",
                          LevelInfo.GetConfig(map).Color + map);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Main");
            p.Message("&HSends you to the main level.");
            p.Message("&T/Main [level]");
            p.Message("&HSets the main level to that level.");
        }
    }
}