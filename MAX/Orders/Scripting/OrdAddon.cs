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
using MAX.Scripting;

namespace MAX.Orders.Scripting
{
    public class OrdAddon : Order
    {
        public override string Name { get { return "Addon"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Owner; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("ALoad", "load"), new OrderDesignation("AUnload", "unload"),
                    new OrderDesignation("Addons", "list") };
            }
        }
        public override bool MessageBlockRestricted { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces(2);
            if (IsListOrder(args[0]))
            {
                string modifier = args.Length > 1 ? args[1] : "";

                p.Message("Loaded addons:");
                Paginator.Output(p, Addon.custom, ad => ad.Name,
                                 "Addons", "addons", modifier);
                return;
            }
            if (args.Length == 1) { Help(p); return; }

            string ord = args[0], name = args[1];
            if (!Formatter.ValidFilename(p, name)) return;

            if (ord.CaselessEq("load"))
            {
                string path = IScripting.AddonPath(name);
                ScriptingOperations.LoadAddons(p, path);
            }
            else if (ord.CaselessEq("unload"))
            {
                UnloadAddon(p, name);
            }
            else if (ord.CaselessEq("create"))
            {
                Find("OrdCreate").Use(p, "addon " + name);
            }
            else if (ord.CaselessEq("compile"))
            {
                Find("Compile").Use(p, "addon " + name);
            }
            else
            {
                Help(p);
            }
        }

        public static void UnloadAddon(Player p, string name)
        {
            Addon addon = Matcher.Find(p, name, out int matches, Addon.custom,
                                         null, add => add.Name, "addons");

            if (addon == null) return;
            ScriptingOperations.UnloadAddon(p, addon);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Addon load [filename]");
            p.Message("&HLoad a compiled addon from the &faddons &Hfolder");
            p.Message("&T/Addon unload [name]");
            p.Message("&HUnloads a currently loaded addon");
            p.Message("&T/Addon list");
            p.Message("&HLists all loaded addons");
        }
    }
}
