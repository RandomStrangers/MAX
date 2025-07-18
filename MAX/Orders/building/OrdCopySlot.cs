﻿/*
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
using MAX.Drawing;
using System;
using System.Collections.Generic;

namespace MAX.Orders.Building
{
    public class OrdCopySlot : Order
    {
        public override string Name { get { return "CopySlot"; } }
        public override string Shortcut { get { return "cs"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0)
            {
                OutputCopySlots(p);
            }
            else if (message.CaselessEq("random"))
            {
                SetRandomCopySlot(p);
            }
            else
            {
                int i = 0;
                if (!OrderParser.GetInt(p, message, "Slot number", ref i, 1, p.group.CopySlots)) return;

                SetCopySlot(p, i);
            }
        }

        public static void OutputCopySlots(Player p)
        {
            List<CopyState> copySlots = p.CopySlots;
            int used = 0;

            for (int i = 0; i < copySlots.Count; i++)
            {
                if (copySlots[i] == null) continue;
                p.Message("  #{0}: {1}", i + 1, copySlots[i].Summary);
                used++;
            }

            p.Message("Using {0} of {1} slots, with slot #{2} selected.",
                      used, p.group.CopySlots, p.CurrentCopySlot + 1);
        }

        public static void SetRandomCopySlot(Player p)
        {
            List<CopyState> copySlots = p.CopySlots;
            List<int> slots = new List<int>();

            for (int i = 0; i < copySlots.Count; i++)
            {
                if (copySlots[i] == null) continue;
                slots.Add(i);
            }

            if (slots.Count == 0)
            {
                p.Message("&WCannot randomly select when all copy slots are unused/empty");
                return;
            }

            int idx = new Random().Next(slots.Count);
            SetCopySlot(p, slots[idx] + 1);
        }

        public static void SetCopySlot(Player p, int i)
        {
            p.CurrentCopySlot = i - 1;
            if (p.CurrentCopy == null)
            {
                p.Message("Selected copy slot {0} (unused)", i);
            }
            else
            {
                p.Message("Selected copy slot {0}: {1}", i, p.CurrentCopy.Summary);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/CopySlot random");
            p.Message("&HSelects a random slot to &T/copy &Hand &T/paste &Hfrom");
            p.Message("&T/CopySlot [number]");
            p.Message("&HSelects the slot to &T/copy &Hand &T/paste &Hfrom");
            p.Message("&HMaxmimum number of copy slots is determined by your rank");
            p.Message("&T/CopySlot");
            p.Message("&HLists details about any copies stored in any slots");
        }
    }
}