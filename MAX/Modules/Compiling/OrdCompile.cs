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
using MAX.Orders;
using MAX.Scripting;

namespace MAX.Compiling
{
    public class OrdCompile : Order
    {
        public override string Name { get { return "Compile"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Owner; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("ACompile", "addon") }; }
        }
        public override bool MessageBlockRestricted { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces();
            bool addon = args[0].CaselessEq("addon");
            string name, lang;

            if (addon)
            {
                // compile addon [name] <language>
                name = args.Length > 1 ? args[1] : "";
                lang = args.Length > 2 ? args[2] : "";
            }
            else
            {
                // compile [name] <language>
                name = args[0];
                lang = args.Length > 1 ? args[1] : "";
            }

            if (name.Length == 0) { Help(p); return; }
            if (!Formatter.ValidFilename(p, name)) return;

            ICompiler compiler = CompilerOperations.GetCompiler(p, lang);
            if (compiler == null) return;

            // either "source" or "source1,source2,source3"
            string[] paths = name.SplitComma();

            if (addon)
            {
                CompileAddon(p, paths, compiler);
            }
            else
            {
                CompileOrder(p, paths, compiler);
            }
        }

        public virtual void CompileAddon(Player p, string[] paths, ICompiler compiler)
        {
            string dstPath = IScripting.AddonPath(paths[0]);

            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = compiler.AddonPath(paths[i]);
            }
            CompilerOperations.Compile(p, compiler, "Addon", paths, dstPath);
        }

        public virtual void CompileOrder(Player p, string[] paths, ICompiler compiler)
        {
            string dstPath = IScripting.OrderPath(paths[0]);

            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = compiler.OrderPath(paths[i]);
            }
            CompilerOperations.Compile(p, compiler, "Order", paths, dstPath);
        }

        public override void Help(Player p)
        {
            ICompiler compiler = ICompiler.Compilers[0];
            p.Message("&T/Compile [order name]");
            p.Message("&HCompiles a .cs file containing a C# order into a DLL");
            p.Message("&H  Compiles from &f{0}", compiler.OrderPath("&H<name>&f"));
            p.Message("&T/Compile addon [addon name]");
            p.Message("&HCompiles a .cs file containing a C# addon into a DLL");
            p.Message("&H  Compiles from &f{0}", compiler.AddonPath("&H<name>&f"));
        }
    }
}