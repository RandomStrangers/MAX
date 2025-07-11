/*
    Copyright 2015-2024 MCGalaxy
        
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
namespace MAX.Compiling
{
    public class CompilerAddon : Addon
    {
        public override string Name { get { return "Compiler"; } }
        public Order ordCreate = new OrdOrdCreate();
        public Order ordCompile = new OrdCompile();
        public Order ordCompLoad = new OrdCompLoad();
        public override void Load(bool startup)
        {
            Server.EnsureDirectoryExists(ICompiler.ORDERS_SOURCE_DIR);
            Order.Register(ordCreate, ordCompile, ordCompLoad);
        }
        public override void Unload(bool shutdown)
        {
            Order.Unregister(ordCreate, ordCompile, ordCompLoad);
        }
    }
}