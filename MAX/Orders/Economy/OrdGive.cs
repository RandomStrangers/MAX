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
using MAX.Eco;
using MAX.Events.EconomyEvents;

namespace MAX.Orders.Eco
{
    public class OrdGive : MoneyOrd
    {
        public override string Name { get { return "Give"; } }
        public override string Shortcut { get { return "Gib"; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message, OrderData data)
        {
            bool all = false;
            if (!ParseArgs(p, message, ref all, out EcoTransaction trans)) return;

            Player who = PlayerInfo.FindMatches(p, trans.TargetName, out int matches);
            if (matches > 1) return;
            int money;
            if (who == null)
            {
                trans.TargetName = Economy.FindMatches(p, trans.TargetName, out money);
                if (trans.TargetName == null) return;

                if (ReachedMax(p, money, trans.Amount)) return;
                money += trans.Amount;
                Economy.UpdateMoney(trans.TargetName, money);
            }
            else
            {
                trans.TargetName = who.name;
                money = who.money;

                if (ReachedMax(p, money, trans.Amount)) return;
                who.SetMoney(who.money + trans.Amount);
            }

            trans.TargetFormatted = p.FormatNick(trans.TargetName);
            trans.Type = EcoTransactionType.Give;
            OnEcoTransactionEvent.Call(trans);
        }

        public static bool ReachedMax(Player p, int current, int amount)
        {
            if (current + amount > int.MaxValue)
            {
                p.Message("&WPlayers cannot have over &3" + int.MaxValue + " &3" + Server.Config.Currency); return true;
            }
            return false;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Give [player] [amount] <reason>");
            p.Message("&HGives [player] [amount] &3" + Server.Config.Currency);
        }
    }
}