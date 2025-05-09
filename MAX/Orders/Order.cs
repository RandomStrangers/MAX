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
using System.Reflection;
using MAX.Orders;
using MAX.Maths;
using MAX.Scripting;

namespace MAX 
{
    public abstract partial class Order 
    {
        /// <summary> The full name of this order (e.g. 'Copy') </summary>
        public abstract string name { get; }
        /// <summary> The shortcut/short name of this order (e.g. `"c"`) </summary>
        public virtual string shortcut { get { return ""; } }
        /// <summary> The type/group of this order (see `OrderTypes` class) </summary>
        public abstract string type { get; }
        /// <summary> Whether this order can be used in museums </summary>
        /// <remarks> Level altering (e.g. places a block) order should return false </remarks>
        public virtual bool museumUsable { get { return true; } }
        /// <summary> The default minimum rank that is required to use this order </summary>
        public virtual LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        
        public abstract void Use(Player p, string message);
        public virtual void Use(Player p, string message, OrderData data) { Use(p, message); }
        public abstract void Help(Player p);
        public virtual void Help(Player p, string message) { Help(p); Formatter.PrintOrderInfo(p, this); }
        
        public virtual OrderPerm[] ExtraPerms { get { return null; } }
        public virtual OrderDesignation[] Designations { get { return null; } }

        /// <summary> Whether this order is usable by 'super' players (MAX, IRC, etc) </summary>
        public virtual bool SuperUseable { get { return true; } }
        public virtual bool MessageBlockRestricted { get { return type.CaselessContains("mod"); } }
        /// <summary> Whether this order can be used when a player is jailed </summary>
        /// <remarks> Only informational order should override this to return true </remarks>
        public virtual bool UseableWhenFrozen { get { return false; } }

        /// <summary> Whether using this order is logged to server logs </summary>
        /// <remarks> return false to prevent this order showing in logs (e.g. /pass) </remarks>
        public virtual bool LogUsage { get { return true; } }
        /// <summary> Whether this order updates the 'most recent order used' by players </summary>
        /// <remarks> return false to prevent this order showing in /last (e.g. /pass, /hide) </remarks>
        public virtual bool UpdatesLastOrd { get { return true; } }
        
        public virtual OrderParallelism Parallelism { 
            get { return type.CaselessEq(OrderTypes.Information) ? OrderParallelism.NoAndWarn : OrderParallelism.Yes; }
        }
        public OrderPerms Permissions;
        
        public static List<Order> allOrds  = new List<Order>();
        public static bool IsCore(Order ord) { 
            return ord.GetType().Assembly == Assembly.GetExecutingAssembly(); // TODO common method
        }

        public static List<Order> CopyAll() { return new List<Order>(allOrds); }
        
        
        public static void InitAll() {
            allOrds.Clear();
            Designation.coreDesignations.Clear();

            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++) 
            {
                Type type = types[i];
                if (!type.IsSubclassOf(typeof(Order)) || type.IsAbstract || !type.IsPublic) continue;
                
                Order ord = (Order)Activator.CreateInstance(type);
                if (Server.Config.DisabledOrders.CaselessContains(ord.name)) continue;
                Register(ord);
            }
            
            IScripting.AutoloadOrders();
        }
        public static void Register(params Order[] ords)
        {
            foreach (Order ord in ords)
            {
                Register(ord);
            }
        }
        public static void Register(Order ord) {
            allOrds.Add(ord);            
            ord.Permissions = OrderPerms.GetOrAdd(ord.name, ord.defaultRank);
            
            OrderPerm[] extra = ord.ExtraPerms;
            if (extra != null) {
                for (int i = 0; i < extra.Length; i++) 
                {
                    OrderExtraPerms exPerms = OrderExtraPerms.GetOrAdd(ord.name, i + 1, extra[i].Perm);
                    exPerms.Desc = extra[i].Description;
                }
            }           
            Designation.RegisterDefaults(ord);
        }

        public static void TryRegister(bool announce, params Order[] orders)
        {
            foreach (Order ord in orders)
            {
                if (Find(ord.name) != null) continue;

                Register(ord);
                if (announce) Logger.Log(LogType.SystemActivity, "Order /{0} loaded", ord.name);
            }
        }
        public static bool Unregister(Order ord) {
            bool removed = allOrds.Remove(ord);
            
            // typical usage: Order.Unregister(Order.Find("xyz"))
            // So don't throw exception if Order.Find returned null
            if (ord != null) Designation.UnregisterDefaults(ord);
            return removed;
        }
        
        public static void Unregister(params Order[] orders) {
            foreach (Order ord in orders) Unregister(ord);
        }
        
        
        public static string GetColoredName(Order ord) {
            LevelPermission perm = ord.Permissions.MinRank;
            return Group.GetColor(perm) + ord.name;
        }
        
        public static Order Find(string name) {
            foreach (Order ord in allOrds) 
            {
                if (ord.name.CaselessEq(name)) return ord;
            }
            return null;
        }
        
        public static void Search(ref string ordName, ref string ordArgs) {
            if (ordName.Length == 0) return;
            Designation designation = Designation.Find(ordName);
            
            // Designations override built in order shortcuts
            if (designation == null) {
                foreach (Order ord in allOrds) 
                {
                    if (!ord.shortcut.CaselessEq(ordName)) continue;
                    ordName = ord.name; return;
                }
                return;
            }
            
            ordName = designation.Target;
            string format = designation.Format;
            if (format == null) return;
            
            if (format.Contains("{args}")) {
                ordArgs = format.Replace("{args}", ordArgs);
            } else {
                ordArgs = format + " " + ordArgs;
            }
            ordArgs = ordArgs.Trim();
        }
    }
    
    public enum OrderContext : byte 
    {
        Normal, Static, SendOrd, Purchase, MessageBlock
    }
    
    public struct OrderData 
    {
        public LevelPermission Rank;
        public OrderContext Context;
        public Vec3S32 MBCoords;
    }
    
    // Clunky design, but needed to stay backwards compatible with custom orders
    public abstract class Order2 : Order 
    {
        public override void Use(Player p, string message) {
            Use(p, message, p.DefaultOrdData);
        }
    }

    public enum OrderParallelism
    {
        NoAndSilent, NoAndWarn, Yes
    }
}

namespace MAX.Orders 
{
    public struct OrderPerm 
    {
        public LevelPermission Perm;
        public string Description;
        
        public OrderPerm(LevelPermission perm, string desc) {
            Perm = perm; Description = desc;
        }
    }
    
    public struct OrderDesignation 
    {
        public string Trigger, Format;
        
        public OrderDesignation(string ord, string format = null) {
            Trigger = ord; Format = format;
        }
    }
}