/*
    Copyright 2011 MCForge modified by headdetect

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
using MAX.Scripting;

namespace MAX.Compiling
{
    public class OrdCompLoad : OrdCompile
    {
        public override string name { get { return "CompLoad"; } }
        public override string shortcut { get { return "cml"; } }
        public override OrderDesignation[] Designations { get { return null; } }

        public override void CompileAddon(Player p, string[] paths, ICompiler compiler)
        {
            string dst = IScripting.AddonPath(paths[0]);

            UnloadAddon(p, paths[0]);
            base.CompileAddon(p, paths, compiler);
            ScriptingOperations.LoadAddons(p, dst);
        }

        public static void UnloadAddon(Player p, string name)
        {
            Addon addon = Addon.FindCustom(name);

            if (addon == null) return;
            ScriptingOperations.UnloadAddon(p, addon);
        }

        public override void CompileOrder(Player p, string[] paths, ICompiler compiler)
        {
            string ord = paths[0];
            string dst = IScripting.OrderPath(ord);

            UnloadOrder(p, ord);
            base.CompileOrder(p, paths, compiler);
            ScriptingOperations.LoadOrders(p, dst);
        }

        public static void UnloadOrder(Player p, string ordName)
        {
            string ordArgs = "";
            Search(ref ordName, ref ordArgs);
            Order ord = Find(ordName);

            if (ord == null) return;
            ScriptingOperations.UnloadOrder(p, ord);
        }

        public override void Help(Player p)
        {
            p.Message("&T/CompLoad [order]");
            p.Message("&HCompiles and loads (or reloads) a C# order into the server");
            p.Message("&T/CompLoad addon [addon]");
            p.Message("&HCompiles and loads (or reloads) a C# addon into the server");
        }
    }
}
