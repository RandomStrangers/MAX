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
using System;
using System.Collections.Generic;

namespace MAX.Orders
{

    /// <summary>
    /// Represents the name, behavior, and help text for a suborder. Used with SubOrderGroup to offer a variety of suborders to run based on user input.
    /// </summary>
    public class SubOrder
    {
        public delegate void Behavior(Player p, string[] args);
        public delegate void BehaviorOneArg(Player p, string arg);

        public string Name;
        public int ArgCount;
        public Behavior behavior;
        public string[] Help;
        public bool MapOnly;
        public string[] Aliases;

        /// <summary>
        /// When mapOnly is true, the suborder can only be used when the player is the realm owner.
        /// Args passed to behavior through SubOrderGroup.Use are guaranteed to be the length specified by argCount
        /// </summary>
        public SubOrder(string name, int argCount, Behavior behavior, string[] help, bool mapOnly = true, string[] aliases = null)
        {
            if (argCount < 1) { throw new ArgumentException("argCount must be greater than or equal to 1."); }
            Name = name;
            ArgCount = argCount;
            this.behavior = behavior;
            Help = help;
            MapOnly = mapOnly;
            Aliases = aliases;
        }
        public SubOrder(string name, BehaviorOneArg simpleBehavior, string[] help, bool mapOnly = true, string[] aliases = null) :
            this(name, 1, (p, args) => { simpleBehavior(p, args[0]); }, help, mapOnly, aliases)
        { }

        public bool Match(string ord)
        {
            if (Aliases != null)
            {
                foreach (string alias in Aliases)
                {
                    if (alias.CaselessEq(ord)) { return true; }
                }
            }
            return Name.CaselessEq(ord);
        }
        public bool AnyMatchingAlias(SubOrder other)
        {
            if (Aliases != null)
            {
                foreach (string alias in Aliases)
                {
                    if (other.Match(alias)) { return true; }
                }
            }
            return other.Match(Name);
        }
        public bool Allowed(Player p, string parentOrderName)
        {
            if (MapOnly && !LevelInfo.IsRealmOwner(p.level, p.name))
            {
                p.Message("You may only use &T/{0} {1}&S after you join your map.", parentOrderName, Name.ToLower());
                return false;
            }
            return true;
        }
        public void DisplayHelp(Player p)
        {
            if (Help == null || Help.Length == 0)
            {
                p.Message("No help is available for {0}", Name);
                return;
            }
            p.MessageLines(Help);
        }
    }

    /// <summary>
    /// Represents a group of SubOrders that can be called from a given parent order. SubOrders can be added or removed using Register and Unregister.
    /// </summary>
    public class SubOrderGroup
    {
        public enum UsageResult { NoneFound, Success, Disallowed }

        public string parentOrderName;
        public List<SubOrder> subOrders;

        public SubOrderGroup(string parentOrd, List<SubOrder> initialOrds)
        {
            parentOrderName = parentOrd;
            subOrders = initialOrds;
        }

        public void Register(SubOrder subOrd)
        {
            foreach (SubOrder sub in subOrders)
            {
                if (subOrd.AnyMatchingAlias(sub))
                {
                    throw new ArgumentException(
                        string.Format("One or more designations of the existing suborder \"{0}\" conflicts with the suborder \"{1}\" that is being registered.",
                        sub.Name, subOrd.Name));
                }
            }
            subOrders.Add(subOrd);
        }

        public void Unregister(SubOrder subOrd)
        {
            subOrders.Remove(subOrd);
        }

        public UsageResult Use(Player p, string message, bool alertNoneFound = true)
        {
            string[] args = message.SplitSpaces(2);
            string ord = args[0];

            foreach (SubOrder subOrd in subOrders)
            {
                if (!subOrd.Match(ord)) { continue; }
                if (!subOrd.Allowed(p, parentOrderName)) { return UsageResult.Disallowed; }

                string[] bArgs = new string[subOrd.ArgCount];
                string[] ordArgs = args.Length > 1 ? args[1].SplitSpaces(subOrd.ArgCount) : new string[] { "" };

                for (int i = 0; i < bArgs.Length; i++)
                {
                    if (i < ordArgs.Length) { bArgs[i] = ordArgs[i]; }
                    else { bArgs[i] = ""; }
                }

                subOrd.behavior(p, bArgs);
                return UsageResult.Success;
            }
            if (alertNoneFound)
            {
                p.Message("There is no {0} order \"{1}\".", parentOrderName, message);
                p.Message("See &T/help {0}&S for all {0} orders.", parentOrderName);
            }
            return UsageResult.NoneFound;
        }

        public void DisplayAvailable(Player p)
        {
            p.Message("&HOrders: &S{0}", subOrders.Join(grp => grp.Name));
            p.Message("&HUse &T/Help {0} [order] &Hfor more details", parentOrderName);
        }

        public void DisplayHelpFor(Player p, string subOrdName)
        {
            foreach (SubOrder subOrd in subOrders)
            {
                if (!subOrd.Match(subOrdName)) { continue; }
                subOrd.DisplayHelp(p);
                return;
            }
            p.Message("There is no {0} order {1} to display help for.", parentOrderName, subOrdName);
        }
    }
}