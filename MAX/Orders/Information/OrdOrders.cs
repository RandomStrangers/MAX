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
using System.Collections.Generic;

namespace MAX.Orders.Info 
{
    public sealed class OrdOrders : Order2 
    {
        public override string name { get { return "Orders"; } }
        public override string shortcut { get { return "Ords"; } }
        public override string type { get { return OrderTypes.Information; } }
        public override bool UseableWhenFrozen { get { return true; } }
        public override OrderDesignation[] Designations {
            get { return new[] { new OrderDesignation("OrdList") }; }
        }

        public override void Use(Player p, string message, OrderData data) {
            if (!ListOrders(p, message)) Help(p);
        }

        public static bool ListOrders(Player p, string message) {
            string[] args = message.SplitSpaces();
            string sort = args.Length > 1 ? args[1].ToLower() : "";
            string modifier = args.Length > 2 ? args[2] : sort;
            
            // if user only provided name/names/rank/ranks, don't treat that as the modifier
            if (args.Length == 2) {
                if (modifier == "name" || modifier == "names" || modifier == "rank" || modifier == "ranks") {
                    modifier = "";
                } else {
                    sort = "";
                }
            }

            string type = args[0].ToLower();
            if (type == "short" || type == "shortcut" || type == "shortcuts") {
                 PrintShortcuts(p, modifier);
            } else if (type == "old" || type == "oldmenu" || type == "" || type == "order") {
                 PrintRankOrders(p, sort, modifier, p.group, true);
            } else if (type == "all" || type == "orderall" || type == "ordersall") {
                 PrintAllOrders(p, sort, modifier);
            } else {
                bool any = PrintCategoryOrders(p, sort, modifier, type);
                if (any) return true;
                
                // list orders a rank can use 
                Group grp = Group.Find(type);
                if (grp == null) return false;
                PrintRankOrders(p, sort, modifier, grp, false);
            }
            return true;
        }

        public static void PrintShortcuts(Player p, string modifier) {
            List<Order> shortcuts = new List<Order>();
            foreach (Order ord in allOrds) {
                if (ord.shortcut.Length == 0) continue;
                if (!p.CanUse(ord)) continue;
                shortcuts.Add(ord);
            }
            
            Paginator.Output(p, shortcuts,
                             (ord) => "&b" + ord.shortcut + " &S[" + ord.name + "]",
                             "Orders shortcuts", "shortcuts", modifier);
        }

        public static void PrintRankOrders(Player p, string sort, string modifier, Group group, bool own) {
            List<Order> ords = new List<Order>();
            foreach (Order c in allOrds) 
            {
                if (c.Permissions.UsableBy(group.Permission)) ords.Add(c);
            }   
            
            if (ords.Count == 0) {
                p.Message("{0} &Scannot use any orders.", group.ColoredName); return;
            }            
            SortOrders(ords, sort);
            if (own)
                p.Message("Available orders:");
            else
                p.Message("Orders available to " + group.ColoredName + " &Srank:");
            
            string type = "Ords " + group.Name;
            if (sort.Length > 0) type += " " + sort;
            Paginator.Output(p, ords, GetColoredName,
                             type, "orders", modifier);
            p.Message("Type &T/Help <order> &Sfor more help on a order.");
        }

        public static void PrintAllOrders(Player p, string sort, string modifier) {
            List<Order> ords = CopyAll();
            SortOrders(ords, sort);
            p.Message("All orders:");
            
            string type = "Orders all";
            if (sort.Length > 0) type += " " + sort;
            Paginator.Output(p, ords, GetColoredName,
                             type, "orders", modifier);            
            p.Message("Type &T/Help <order> &Sfor more help on a order.");
        }

        public static bool PrintCategoryOrders(Player p, string sort, string modifier, string type) {
            List<Order> ords = new List<Order>();
            bool foundAny = false;
            
            // common shortcuts people tend to use
            type = MapCategory(type);
            if (type.CaselessEq("eco")) type = OrderTypes.Economy;

            foreach (Order c in allOrds) 
            {
                string category = MapCategory(c.type);
                if (!type.CaselessEq(category)) continue;
                
                if (p.CanUse(c)) ords.Add(c);
                foundAny = true;
            }
            if (!foundAny) return false;
            
            if (ords.Count == 0) {
                p.Message("You cannot use any of the {0} orders.", type.Capitalize()); return true;
            }            
            SortOrders(ords, sort);
            p.Message(type.Capitalize() + " orders you may use:");

            type = "Orders " + type;
            if (sort.Length > 0) type += " " + sort;
            Paginator.Output(p, ords, GetColoredName,
                             type, "orders", modifier);
            
            p.Message("Type &T/Help <order> &Sfor more help on a order.");
            return true;
        }

        public static void SortOrders(List<Order> ords, string sort) {
            if (sort == "name" || sort == "names") {
                ords.Sort((a, b) => a.name
                          .CompareTo(b.name));
            }
            if (sort == "rank" || sort == "ranks") {
                ords.Sort((a, b) => a.Permissions.MinRank
                          .CompareTo(b.Permissions.MinRank));
            }
        }

        public static string MapCategory(string type) {
            // convert old category/type names
            if (type == "Build")   return OrderTypes.Building;
            if (type == "chat")    return OrderTypes.Chat;
            if (type == "economy") return OrderTypes.Economy;
            if (type == "game")    return OrderTypes.Games;
            if (type == "mod")     return OrderTypes.Moderation;
            if (type == "other")   return OrderTypes.Other;
            if (type == "world")   return OrderTypes.World;

            if (type == "information") return OrderTypes.Information;
            return type;
        }

        public static string GetCategories() {
            Dictionary<string, bool> categories = new Dictionary<string, bool>();
            foreach (Order ord in allOrds)
            {
            	categories[MapCategory(ord.type)] = true;
            }
            
            List<string> list = new List<string>(categories.Keys);
            list.Sort();
            return list.Join(" ");
        }

        public override void Help(Player p) {
            p.Message("&T/Orders [category] <sort by>");
            p.Message("  &HIf no category is given, outputs all orders you can use.");
            p.Message("  &HIf category is \"shortcuts\", outputs all order shortcuts.");
            p.Message("  &HIf category is \"all\", outputs all orders.");
            p.Message("  &HIf category is a rank name, outputs what that rank can use.");
            p.Message("&HOther order categories:");
            p.Message("  &H{0}", GetCategories());
            p.Message("&HSort By is optional, and can be either \"name\" or \"rank\"");
        }
    }
}