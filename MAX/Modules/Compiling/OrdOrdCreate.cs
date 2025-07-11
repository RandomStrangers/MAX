/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified for use with MCForge)
 
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

namespace MAX.Compiling
{
    public class OrdOrdCreate : OrdCompile
    {
        public override string Name { get { return "OrdCreate"; } }
        public override string Shortcut { get { return ""; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("ACreate", "addon") }; }
        }

        public override void CompileOrder(Player p, string[] paths, ICompiler compiler)
        {
            foreach (string ord in paths)
            {
                CompilerOperations.CreateOrder(p, ord, compiler);
            }
        }

        public override void CompileAddon(Player p, string[] paths, ICompiler compiler)
        {
            foreach (string ord in paths)
            {
                CompilerOperations.CreateAddon(p, ord, compiler);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/OrdCreate [name]");
            p.Message("&HCreates an example C# order named Ord[name]");
            p.Message("&H  This can be used as the basis for creating a new order");
            p.Message("&T/OrdCreate addon [name]");
            p.Message("&HCreates an example C# addon named [name]");
        }
    }
}
