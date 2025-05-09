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
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX.Orders 
{    
    /// <summary> Represents additional permissions required to perform a special action in a order. </summary>
    /// <remarks> For example, /color order has an extra permission for changing the color of other players. </remarks>
    public class OrderExtraPerms : ItemPerms 
    {
        public string OrdName, Desc = "";
        public int Num;
        public override string ItemName { get { return OrdName + ":" + Num; } }

        public static List<OrderExtraPerms> list = new List<OrderExtraPerms>();
        
        
        public OrderExtraPerms(string ord, int num, string desc, LevelPermission min) : base(min) {
            OrdName = ord; Num = num; Desc = desc;
        }
        
        public OrderExtraPerms Copy() {
            OrderExtraPerms copy = new OrderExtraPerms(OrdName, Num, Desc, 0);
            CopyPermissionsTo(copy); return copy;
        }
        
        
        public static OrderExtraPerms Find(string ord, int num) {
            foreach (OrderExtraPerms perms in list) 
            {
                if (perms.OrdName.CaselessEq(ord) && perms.Num == num) return perms;
            }
            return null;
        }
        
        public static List<OrderExtraPerms> FindAll(string ord) {
            List<OrderExtraPerms> all = new List<OrderExtraPerms>();
            foreach (OrderExtraPerms perms in list) 
            {
                if (perms.OrdName.CaselessEq(ord) && perms.Desc.Length > 0) all.Add(perms);
            }
            return all;
        }
        
        
        /// <summary> Gets or adds the nth extra permission for the given order. </summary>
        public static OrderExtraPerms GetOrAdd(string ord, int num, LevelPermission min) {
            OrderExtraPerms perms = Find(ord, num);
            if (perms != null) return perms;
            
            perms = new OrderExtraPerms(ord, num, "", min);
            list.Add(perms);
            return perms;
        }
              
        public void MessageCannotUse(Player p) {
            p.Message("Only {0} {1}", Describe(), Desc);
        }


        public static object ioLock = new object();      
        /// <summary> Saves list of extra permissions to disc. </summary>
        public static void Save() {
            try {
                lock (ioLock) SaveCore();
            } catch (Exception ex) {
                Logger.LogError("Error saving " + Paths.OrdExtraPermsFile, ex);
            }
        }

        public static void SaveCore() {
            using (StreamWriter w = new StreamWriter(Paths.OrdExtraPermsFile)) {
                WriteHeader(w, "extra order permissions", "extra permissions in some orders",
                            "OrderName:ExtraPermissionNumber", "countdown:1");
                
                foreach (OrderExtraPerms perms in list) {
                    w.WriteLine(perms.Serialise());
                }
            }
        }
        

        /// <summary> Loads list of extra permissions to disc. </summary>
        public static void Load() {
            lock (ioLock) {
                if (!File.Exists(Paths.OrdExtraPermsFile)) Save();
                
                using (StreamReader r = new StreamReader(Paths.OrdExtraPermsFile)) {
                    ProcessLines(r);
                }
            }
        }

        public static void ProcessLines(StreamReader r) {
            string[] args = new string[5];
            OrderExtraPerms perms;
            string line;
            
            while ((line = r.ReadLine()) != null) {
                if (line.IsCommentLine() || line.IndexOf(':') == -1) continue;
                // Format - Name:Num : Lowest : Disallow : Allow
                line.Replace(" ", "").FixedSplit(args, ':');
                
                try {
                    LevelPermission min;
                    List<LevelPermission> allowed, disallowed;
                    
                    // Old format - Name:Num : Lowest : Description
                    if (IsDescription(args[3])) {
                        min = (LevelPermission)int.Parse(args[2]);
                        allowed = null; disallowed = null;
                    } else {
                        Deserialise(args, 2, out min, out allowed, out disallowed);
                    }
                    
                    perms = GetOrAdd(args[0], int.Parse(args[1]), min);
                    perms.Init(min, allowed, disallowed);
                } catch (Exception ex) {
                    Logger.Log(LogType.Warning, "Hit an error on the extra order perms " + line);
                    Logger.LogError(ex);
                }
            }
        }

        public static bool IsDescription(string arg) {
            foreach (char c in arg) 
            {
                if (c >= 'a' && c <= 'z') return true;
            }
            return false;
        }
    }
}