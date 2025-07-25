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
    public class OrdTake : MoneyOrd
    {
        public override string Name { get { return "Take"; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message, OrderData data)
        {
            bool all = true;
            if (!ParseArgs(p, message, ref all, out EcoTransaction trans)) return;

            Player who = PlayerInfo.FindMatches(p, trans.TargetName, out int matches);
            if (matches > 1) return;

            int money = 0;
            if (who == null)
            {
                trans.TargetName = Economy.FindMatches(p, trans.TargetName, out money);
                if (trans.TargetName == null) return;

                Take(ref money, all, trans);
                Economy.UpdateMoney(trans.TargetName, money);
            }
            else
            {
                trans.TargetName = who.name;
                money = who.money;

                Take(ref money, all, trans);
                who.SetMoney(money);
            }

            trans.TargetFormatted = p.FormatNick(trans.TargetName);
            trans.Type = EcoTransactionType.Take;
            OnEcoTransactionEvent.Call(trans);
        }

        public static void Take(ref int money, bool all, EcoTransaction data)
        {
            if (all || money < data.Amount)
            {
                data.Amount = money;
                money = 0;
            }
            else
            {
                money -= data.Amount;
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Take [player] [amount] <reason>");
            p.Message("&HTakes [amount] of &3" + Server.Config.Currency + " &Sfrom [player]");
            p.Message("&T/Take [player] all <reason>");
            p.Message("&HTakes all the &3" + Server.Config.Currency + " &Sfrom [player]");
        }
    }
}