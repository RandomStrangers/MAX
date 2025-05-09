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
using System.IO;

namespace MAX.Orders 
{  
    public class Designation 
    {
        public static List<Designation> coreDesignations = new List<Designation>();
        public static List<Designation> designations = new List<Designation>();
        public string Trigger, Target, Format;

        public Designation(string trigger, string target) {
            Trigger = trigger;
            target = target.Trim();
            int space = target.IndexOf(' ');
            
            if (space < 0) {
                Target = target;
            } else {
                Target = target.Substring(0, space);
                Format = target.Substring(space + 1);
            }
        }
        
        public Designation(string trigger, string target, string format) {
            Trigger = trigger; Target = target; Format = format;
        }

        public static void LoadCustom() {
            designations.Clear();
            
            if (!File.Exists(Paths.DesignationsFile)) { SaveCustom(); return; }
            PropertiesFile.Read(Paths.DesignationsFile, LineProcessor, ':');
        }

        public static void LineProcessor(string key, string value) {
            designations.Add(new Designation(key, value));
        }

        public static void SaveCustom() {
            using (StreamWriter sw = new StreamWriter(Paths.DesignationsFile)) {
                sw.WriteLine("# Designations can be in one of three formats:");
                sw.WriteLine("# trigger : order");
                sw.WriteLine("#    e.g. \"xyz : help\" means /xyz is treated as /help <args given by user>");
                sw.WriteLine("# trigger : order [prefix]");
                sw.WriteLine("#    e.g. \"xyz : help me\" means /xyz is treated as /help me <args given by user>");
                sw.WriteLine("# trigger : order <prefix> {args} <suffix>");
                sw.WriteLine("#    e.g. \"mod : setrank {args} mod\" means /mod is treated as /setrank <args given by user> mod");
                
                foreach (Designation d in designations) 
                {
                    if (d.Format == null) {
                        sw.WriteLine(d.Trigger + " : " + d.Target);
                    } else {
                        sw.WriteLine(d.Trigger + " : " + d.Target + " " + d.Format);
                    }
                }
            }
        }

        public static Designation Find(string ord) {
            foreach (Designation designation in designations) 
            {
                if (designation.Trigger.CaselessEq(ord)) return designation;
            }
            foreach (Designation designation in coreDesignations) 
            {
                if (designation.Trigger.CaselessEq(ord)) return designation;
            }
            return null;
        }

        /// <summary> Registers default designations specified by an order. </summary>
        public static void RegisterDefaults(Order ord) {
            OrderDesignation[] designations = ord.Designations;
            if (designations == null) return;
            
            foreach (OrderDesignation d in designations) 
            {
                Designation designation = new Designation(d.Trigger, ord.name, d.Format);
                coreDesignations.Add(designation);
            }
        }

        public static void UnregisterDefaults(Order ord) {
            if (ord.Designations == null) return;
            coreDesignations.RemoveAll(d => d.Target == ord.name);
        }
    }
}