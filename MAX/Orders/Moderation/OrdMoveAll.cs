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
namespace MAX.Orders.Moderation
{
    public class OrdMoveAll : Order
    {
        public override string Name { get { return "MoveAll"; } }
        public override string Shortcut { get { return "ma"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            Level level = Matcher.FindLevels(p, message);
            if (level == null) return;

            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.Rank < data.Rank)
                    PlayerActions.ChangeMap(pl, level.name);
                else
                    p.Message("You cannot move {0} &Sbecause {1} {2} of equal or higher rank", p.FormatNick(pl), pl.Pronouns.Subject, pl.Pronouns.PresentVerb);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/MoveAll [level]");
            p.Message("&HMoves all players to that level.");
        }
    }
}