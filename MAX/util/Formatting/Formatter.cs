/*
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
using System.Collections.Generic;
using System.Text;
using MAX.Authentication;
using MAX.Orders;

namespace MAX 
{
    public static class Formatter 
    { 
        public static void PrintOrderInfo(Player p, Order ord) {
            p.Message("Usable by: " + ord.Permissions.Describe());
            PrintAliases(p, ord);
            
            List<OrderExtraPerms> extraPerms = OrderExtraPerms.FindAll(ord.name);
            if (ord.ExtraPerms == null) extraPerms.Clear();
            if (extraPerms.Count == 0) return;
            
            p.Message("&TExtra permissions:");
            foreach (OrderExtraPerms extra in extraPerms) 
            {
                p.Message("{0}) {1} {2}", extra.Num, extra.Describe(), extra.Desc);
            }
        }

        public static void PrintAliases(Player p, Order ord) {
            StringBuilder dst = new StringBuilder("Shortcuts: &T");
            if (!string.IsNullOrEmpty(ord.shortcut)) {
                dst.Append('/').Append(ord.shortcut).Append(", ");
            }
            FindAliases(Designation.coreDesignations, ord, dst);
            FindAliases(Designation.designations, ord, dst);
            
            if (dst.Length == "Shortcuts: &T".Length) return;
            p.Message(dst.ToString(0, dst.Length - 2));
        }

        public static void FindAliases(List<Designation> aliases, Order ord, StringBuilder dst) {
            foreach (Designation a in aliases) 
            {
                if (!a.Target.CaselessEq(ord.name)) continue;
                
                dst.Append('/').Append(a.Trigger);
                if (a.Format == null) { dst.Append(", "); continue; }
                
                string name = string.IsNullOrEmpty(ord.shortcut) ? ord.name : ord.shortcut;
                if (name.Length > ord.name.Length) name = ord.name;
                string args = a.Format.Replace("{args}", "[args]");
                
                dst.Append(" for /").Append(name + " " + args);
                dst.Append(", ");
            }
        }
        
        public static void MessageNeedMinPerm(Player p, string action, LevelPermission perm) {
            p.Message("Only {0}&S{1}", Group.GetColoredName(perm), action);
        }
    	
        
        public static bool ValidName(Player p, string name, string type) {
            const string alphabet = Player.USERNAME_ALPHABET + "+"; // compatibility with ClassiCubeAccountPlus
            return IsValidName(p, name, type, alphabet);
        }
        
        public static bool ValidPlayerName(Player p, string name) {
            string alphabet = Player.USERNAME_ALPHABET + "+"; // compatibility with ClassiCubeAccountPlus
            
            foreach (AuthService service in AuthService.Services)
            {
                alphabet += service.Config.NameSuffix;
            }
            return IsValidName(p, name, "player", alphabet);
        }

        public static bool IsValidName(Player p, string name, string type, string alphabet) {
            if (name.Length > 0 && name.ContainsAllIn(alphabet)) return true;
            p.Message("\"{0}\" is not a valid {1} name.", name, type);
            return false;
        }
        
        public static bool ValidMapName(Player p, string name) {
            if (LevelInfo.ValidName(name)) return true;
            p.Message("\"{0}\" is not a valid level name.", name);
            return false;
        }

        public static char[] separators = { '/', '\\', ':' };
        public static char[] invalid    = { '<', '>', '|', '"', '*', '?' };
        /// <summary> Checks that the input is a valid filename (non-empty and no directory separator) </summary>
        /// <remarks> If the input is invalid, messages the player the reason why </remarks>
        public static bool ValidFilename(Player p, string name) {
            if (string.IsNullOrEmpty(name)) {
                p.Message("&WFilename cannot be empty"); 
                return false;
            }
            
            if (name.IndexOfAny(separators) >= 0) {
                p.Message("&W\"{0}\" includes a directory separator (/, : or \\), which is not allowed", name);
                return false;
            }

            if (name.IndexOfAny(invalid) >= 0) {
                p.Message("&W\"{0}\" includes a prohibited character (<, >, |, \", *, or ?)", name);
                return false;
            }

            if (name.ContainsAllIn(".")) {
                p.Message("&W\"{0}\" cannot consist entirely of dot characters", name);
                return false;
            }

            return true;
        }
    }
}