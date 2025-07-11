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
using MAX.Orders;
using System.Collections.Generic;

namespace MAX.Eco
{
    /// <summary> An abstract object that can be bought in the economy. (e.g. a rank, title, levels, etc) </summary>
    public class Item
    {
        /// <summary> Simple name for this item. </summary>
        public virtual string Name { get; }

        /// <summary> The minimum permission/rank required to purchase this item. </summary>
        public LevelPermission PurchaseRank = LevelPermission.Guest;

        /// <summary> Simple name displayed in /shop, defaults to item name. </summary>
        public virtual string ShopName { get { return Name; } }

        /// <summary> Other common names for this item. </summary>
        public string[] Aliases;

        /// <summary> Whether this item can currently be bought in the economy. </summary>
        public bool Enabled;


        public void LoadConfig(string line)
        {
            line.Separate(':', out string prop, out string value);

            if (prop.CaselessEq("enabled"))
            {
                Enabled = value.CaselessEq("true");
            }
            else if (prop.CaselessEq("purchaserank"))
            {
                PurchaseRank = (LevelPermission)int.Parse(value);
            }
            else
            {
                Parse(prop, value);
            }
        }

        /// <summary> Parses item-specific properties from the given configuration </remarks>
        public virtual void Parse(string prop, string value)
        {
        }

        public void SaveConfig(List<string> cfg)
        {
            cfg.Add("enabled:" + Enabled);
            cfg.Add("purchaserank:" + (int)PurchaseRank);
            Serialise(cfg);
        }

        /// <summary> Saves item-specific properties to the given configuration </summary>
        public virtual void Serialise(List<string> cfg)
        {
        }


        /// <summary> Called when a player is attempting to purchase this item </summary>
        /// <remarks> Usually called when a player does /buy [item name] &lt;args&gt; </remarks>
        public virtual void OnPurchase(Player p, string args)
        {
        }

        /// <summary> Called when the player does /eco [item name] [option] &lt;value&gt; </summary>
        public virtual void OnSetup(Player p, string[] args)
        {
        }

        /// <summary> Called when the player does /eco help [item name] </summary>
        public virtual void OnSetupHelp(Player p)
        {
            p.Message("&T/Eco {0} enable/disable", Name.ToLower());
            p.Message("&HEnables/disables purchasing this item.");
            p.Message("&T/Eco {0} purchaserank [rank]", Name.ToLower());
            p.Message("&HSets the lowest rank which can purchase this item.");
        }

        /// <summary> Called when the player does /store </summary>
        public virtual void OnStoreOverview(Player p)
        {
        }

        /// <summary> Outputs detailed information about how to purchase this item to the given player </summary>
        /// <remarks> Usually called when the player does /store [item name] </remarks>
        public virtual void OnStoreOrder(Player p)
        {
        }


        public void Setup(Player p, string[] args)
        {
            string ord = args[1];
            if (ord.CaselessEq("enable"))
            {
                p.Message("&aThe {0} item is now enabled.", Name);
                Enabled = true;
            }
            else if (ord.CaselessEq("disable"))
            {
                p.Message("&aThe {0} item is now disabled.", Name);
                Enabled = false;
            }
            else if (ord.CaselessStarts("purchaserank"))
            {
                if (args.Length == 2) { p.Message("You need to provide a rank name."); return; }
                Group grp = Matcher.FindRanks(p, args[2]);
                if (grp == null) return;

                PurchaseRank = grp.Permission;
                p.Message("Min purchase rank for {0} item set to {1}&S.", Name, grp.ColoredName);
            }
            else
            {
                OnSetup(p, args);
            }
        }

        public static bool CheckPrice(Player p, int price, string item)
        {
            if (p.money < price)
            {
                p.Message("&WYou don't have enough &3{1} &Wto buy {0}.", item, Server.Config.Currency);
                return false;
            }
            return true;
        }
    }

    /// <summary> Simple item, in that it only has one cost value. </summary>
    public class SimpleItem : Item
    {
        /// <summary> How much this item costs to purchase. </summary>
        public int Price = 100;

        public override void Parse(string prop, string value)
        {
            if (prop.CaselessEq("price"))
                Price = int.Parse(value);
        }

        public override void Serialise(List<string> cfg)
        {
            cfg.Add("price:" + Price);
        }

        public bool CheckPrice(Player p) { return CheckPrice(p, Price, "a " + Name); }

        public override void OnSetup(Player p, string[] args)
        {
            if (args[1].CaselessEq("price"))
            {
                int cost = 0;
                if (!OrderParser.GetInt(p, args[2], "Price", ref cost)) return;

                p.Message("Changed price of {0} item to &f{1} &3{2}", Name, cost, Server.Config.Currency);
                Price = cost;
            }
            else
            {
                p.Message("Supported actions: enable, disable, price [cost]");
            }
        }

        public override void OnSetupHelp(Player p)
        {
            base.OnSetupHelp(p);
            p.Message("&T/Eco {0} price [amount]", Name.ToLower());
            p.Message("&HSets how many &3{0} &Hthis item costs.", Server.Config.Currency);
        }

        public override void OnStoreOverview(Player p)
        {
            if (p.Rank >= PurchaseRank)
            {
                p.Message("&6{0} &S- &a{1} &S{2}", Name, Price, Server.Config.Currency);
            }
            else
            {
                string grpName = Group.GetColoredName(PurchaseRank);
                p.Message("&6{0} &S({3}&S+) - &a{1} &S{2}", Name, Price, Server.Config.Currency, grpName);
            }
        }

        public override void OnStoreOrder(Player p)
        {
            p.Message("&T/Buy {0} [value]", Name);
            OutputItemInfo(p);
        }

        public void OutputItemInfo(Player p)
        {
            p.Message("&HCosts &a{0} {1} &Heach time the item is bought.", Price, Server.Config.Currency);
            List<string> shortcuts = new List<string>();
            foreach (Designation a in Designation.designations)
            {
                if (!a.Target.CaselessEq("buy") || a.Format == null) continue;

                // Find if there are any custom designations for this item
                bool matchFound = false;
                foreach (string alias in Aliases)
                {
                    if (!a.Format.CaselessEq(alias)) continue;
                    matchFound = true; break;
                }

                if (!matchFound) continue;
                shortcuts.Add("/" + a.Trigger);
            }

            if (shortcuts.Count == 0) return;
            p.Message("Shortcuts: &T{0}", shortcuts.Join());
        }
    }
}