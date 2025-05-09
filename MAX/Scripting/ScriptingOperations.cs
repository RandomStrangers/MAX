/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified by MCGalaxy)

    Edited for use with MCGalaxy
 
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
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX.Scripting
{
    public static class ScriptingOperations
    {
        public static bool LoadOrders(Player p, string path) {
            if (!File.Exists(path)) {
                p.Message("File &9{0} &Snot found.", path);
                return false;
            }
            
            try {
                List<Order> ords = IScripting.LoadOrder(path);
                
                p.Message("Successfully loaded &T{0}",
                          ords.Join(o => "/" + o.name));
                return true;
            } catch (AlreadyLoadedException ex) {
                p.Message(ex.Message);
                return false;
            } catch (Exception ex) {
                p.Message(IScripting.DescribeLoadError(path, ex));
                Logger.LogError("Error loading orders from " + path, ex);
                return false;
            }
        }

        public static bool LoadAddons(Player p, string path) {
            if (!File.Exists(path)) {
                p.Message("File &9{0} &Snot found.", path);
                return false;
            }
            
            try {
                List<Addon> addons = IScripting.LoadAddon(path, false);
                
                p.Message("Addon {0} loaded successfully",
                          addons.Join(a => a.name));
                return true;
            } catch (AlreadyLoadedException ex) {
                p.Message(ex.Message);
                return false;
            } catch (Exception ex) {
                p.Message(IScripting.DescribeLoadError(path, ex));
                Logger.LogError("Error loading addons from " + path, ex);
                return false;
            }
        }
        
        
        public static bool UnloadOrder(Player p, Order ord) {          
            if (Order.IsCore(ord)) {
                p.Message("&T/{0} &Sis a core order, you cannot unload it.", ord.name); 
                return false;
            }
   
            Order.Unregister(ord);
            p.Message("Order &T/{0} &Sunloaded successfully", ord.name);
            return true;
        }
        
        public static bool UnloadAddon(Player p, Addon addon) {
            if (!Addon.Unload(addon)) {
                p.Message("&WError unloading addon. See error logs for more information.");
                return false;
            }
            
            p.Message("Addon {0} &Sunloaded successfully", addon.name);
            return true;
        }
    }
}
