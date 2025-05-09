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
using System.Reflection;
namespace MAX.Scripting
{
    /// <summary> Exception raised when attempting to load a new order/addon 
    /// that has the same name as an already loaded order/addon </summary>
    public sealed class AlreadyLoadedException : Exception
    {
        public AlreadyLoadedException(string msg) : base(msg) { }
    }
    
    /// <summary> Utility methods for loading assemblies, orders, and addons </summary>
    public static class IScripting
    {
        public const string ORDERS_DLL_DIR = "extra/orders/";
        public const string ADDONS_DLL_DIR  = "addons/";
        
        /// <summary> Returns the default .dll path for the custom order with the given name </summary>
        public static string OrderPath(string name) { return ORDERS_DLL_DIR + "Ord" + name + ".dll"; }
        /// <summary> Returns the default .dll path for the addon with the given name </summary>
        public static string AddonPath(string name)  { return ADDONS_DLL_DIR + name + ".dll"; }
        
        
        public static void Init() {
            Directory.CreateDirectory(ORDERS_DLL_DIR);
            Directory.CreateDirectory(ADDONS_DLL_DIR);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAddonAssembly;
        }
        public static class DotNetBackend
        {
            public static void Init() { }

            public static string GetExePath(string path)
            {
                return path;
            }

            public static Assembly ResolveAddonReference(string name)
            {
                return null;
            }
        }
        // only used for resolving addon DLLs depending on other addon DLLs
        static Assembly ResolveAddonAssembly(object sender, ResolveEventArgs args) {
            // This property only exists in .NET framework 4.0 and later
            Assembly requestingAssembly = args.RequestingAssembly;
            
            if (requestingAssembly == null)       return null;
            if (!IsAddonDLL(requestingAssembly)) return null;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assem in assemblies)
            {
                if (!IsAddonDLL(assem)) continue;

                if (args.Name == assem.FullName) return assem;
            }

            Assembly coreRef = DotNetBackend.ResolveAddonReference(args.Name);
            if (coreRef != null) return coreRef;

            Logger.Log(LogType.Warning, "Custom order/addon [{0}] tried to load [{1}], but it could not be found",
                       requestingAssembly.FullName, args.Name);
            return null;
        }

        static bool IsAddonDLL(Assembly a) { return string.IsNullOrEmpty(a.Location); }
        
        
        /// <summary> Constructs instances of all types which derive from T in the given assembly. </summary>
        /// <returns> The list of constructed instances. </returns>
        public static List<T> LoadTypes<T>(Assembly lib) {
            List<T> instances = new List<T>();
            
            foreach (Type t in lib.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface || !t.IsSubclassOf(typeof(T))) continue;
                object instance = Activator.CreateInstance(t);
                
                if (instance == null) {
                    Logger.Log(LogType.Warning, "{0} \"{1}\" could not be loaded", typeof(T).Name, t.Name);
                    throw new BadImageFormatException();
                }
                instances.Add((T)instance);
            }
            return instances;
        }
        
        /// <summary> Loads the given assembly from disc </summary>
        public static Assembly LoadAssembly(string path) {
            byte[] data  = File.ReadAllBytes(path);
            return Assembly.Load(data);
        }
        
        
        
        public static void AutoloadOrders() {
            string[] files = AtomicIO.TryGetFiles(ORDERS_DLL_DIR, "*.dll");
            if (files == null) return;
            
            foreach (string path in files) { AutoloadOrders(path); }
        }
        
        static void AutoloadOrders(string path) {
            List<Order> ords;
            
            try {
                ords = LoadOrder(path);
            } catch (Exception ex) {
                Logger.LogError("Error loading orders from " + path, ex);
                return;
            }
            
            Logger.Log(LogType.SystemActivity, "AUTOLOAD: Loaded {0} from {1}",
                       ords.Join(o => "/" + o.name), Path.GetFileName(path));
        }
        
        /// <summary> Loads and registers all the orders from the given .dll path </summary>
        public static List<Order> LoadOrder(string path) {
            Assembly lib = LoadAssembly(path);
            List<Order> orders = LoadTypes<Order>(lib);
            
            if (orders.Count == 0)
                throw new InvalidOperationException("No orders in " + path);
            
            foreach (Order ord in orders)
            {
                if (Order.Find(ord.name) != null)
                    throw new AlreadyLoadedException("/" + ord.name + " is already loaded");
                
                Order.Register(ord);
            }
            return orders;
        }
        
        public static string DescribeLoadError(string path, Exception ex) {
            string file = Path.GetFileName(path);
            
            if (ex is BadImageFormatException) {
                return "&W" + file + " is not a valid assembly, or has an invalid dependency. Details in the error log.";
            } else if (ex is FileLoadException) {
                return "&W" + file + " or one of its dependencies could not be loaded. Details in the error log.";
            }
            
            return "&WAn unknown error occured. Details in the error log.";
        }
        
        
        public static void AutoloadAddons() {
            string[] files = AtomicIO.TryGetFiles(ADDONS_DLL_DIR, "*.dll");
            if (files == null) return;
            
            // Ensure that addon files are loaded in a consistent order,
            //  in case addons have a dependency on other addons
            Array.Sort(files);
            
            foreach (string path in files)
            {
                try {
                    LoadAddon(path, true);
                } catch (Exception ex) {
                    Logger.LogError("Error loading addons from " + path, ex);
                }
            }
        }
        
        /// <summary> Loads all addons from the given .dll path. </summary>
        public static List<Addon> LoadAddon(string path, bool auto) {
            Assembly lib = LoadAssembly(path);
            List<Addon> addons = LoadTypes<Addon>(lib);
            
            foreach (Addon a in addons)
            {
                if (Addon.FindCustom(a.name) != null)
                    throw new AlreadyLoadedException("Addon " + a.name + " is already loaded");
                
                Addon.Load(a, auto);
            }
            return addons;
        }
    }
}
