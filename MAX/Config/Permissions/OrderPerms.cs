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
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX.Orders
{
    /// <summary> Represents which ranks are allowed (and which are disallowed) to use an order. </summary>
    public class OrderPerms : ItemPerms
    {
        public string OrdName;
        public override string ItemName { get { return OrdName; } }

        public static List<OrderPerms> List = new List<OrderPerms>();


        public OrderPerms(string ord, LevelPermission min) : base(min)
        {
            OrdName = ord;
        }

        public OrderPerms Copy()
        {
            OrderPerms copy = new OrderPerms(OrdName, 0);
            CopyPermissionsTo(copy); return copy;
        }


        /// <summary> Find the permissions for the given order. (case insensitive) </summary>
        public static OrderPerms Find(string ord)
        {
            foreach (OrderPerms perms in List)
            {
                if (perms.OrdName.CaselessEq(ord)) return perms;
            }
            return null;
        }


        /// <summary> Gets or adds permissions for the given order. </summary>
        public static OrderPerms GetOrAdd(string ord, LevelPermission min)
        {
            OrderPerms perms = Find(ord);
            if (perms != null) return perms;

            perms = new OrderPerms(ord, min);
            List.Add(perms);
            return perms;
        }

        public void MessageCannotUse(Player p)
        {
            p.Message("Only {0} can use &T/{1}", Describe(), OrdName);
        }


        public static object ioLock = new object();
        /// <summary> Saves list of order permissions to disc. </summary>
        public static void Save()
        {
            try
            {
                lock (ioLock) SaveCore();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving " + Paths.OrdPermsFile, ex);
            }
        }

        public static void SaveCore()
        {
            using (StreamWriter w = new StreamWriter(Paths.OrdPermsFile))
            {
                WriteHeader(w, "order", "each order", "OrderName", "gun");

                foreach (OrderPerms perms in List)
                {
                    w.WriteLine(perms.Serialise());
                }
            }
        }


        /// <summary> Applies new order permissions to server state. </summary>
        public static void ApplyChanges()
        {
            // does nothing... for now anyways 
            //  (may be required if p.CanUse is changed to instead
            //   use a list of usable orders as a field instead)
        }


        /// <summary> Loads list of order permissions from disc. </summary>
        public static void Load()
        {
            lock (ioLock) LoadCore();
            ApplyChanges();
        }

        public static void LoadCore()
        {
            if (!File.Exists(Paths.OrdPermsFile)) { Save(); return; }

            using (StreamReader r = new StreamReader(Paths.OrdPermsFile))
            {
                ProcessLines(r);
            }
        }

        public static void ProcessLines(StreamReader r)
        {
            string[] args = new string[4];
            OrderPerms perms;
            string line;

            while ((line = r.ReadLine()) != null)
            {
                if (line.IsCommentLine()) continue;
                // Format - Name : Lowest : Disallow : Allow
                line.Replace(" ", "").FixedSplit(args, ':');

                try
                {

                    Deserialise(args, 1, out LevelPermission min, out List<LevelPermission> allowed, out List<LevelPermission> disallowed);
                    perms = GetOrAdd(args[0], min);
                    perms.Init(min, allowed, disallowed);
                }
                catch
                {
                    Logger.Log(LogType.Warning, "Hit an error on the order " + line); continue;
                }
            }
        }
    }
}