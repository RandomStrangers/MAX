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
using MAX.Events.PlayerEvents;
using MAX.Maths;
using MAX.Orders.Building;
using MAX.Orders.CPE;
using MAX.Orders.World;
using System;


namespace MAX.Orders.Moderation
{
    public class OrdZone : Order
    {
        public override string Name { get { return "Zone"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MuseumUsable { get { return false; } }
        public override bool SuperUseable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("ZRemove", "del"), new OrderDesignation("ZDelete", "del"),
                    new OrderDesignation("ZAdd"), new OrderDesignation("ZEdit", "perbuild") };
            }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces(4);
            if (message.Length == 0) { Help(p); return; }
            string opt = args[0];

            if (IsCreateOrder(opt))
            {
                if (args.Length == 1) { Help(p); return; }
                CreateZone(p, args, data, 1);
            }
            else if (IsDeleteOrder(opt))
            {
                if (args.Length == 1) { Help(p); return; }
                DeleteZone(p, args, data);
            }
            else if (opt.CaselessEq("perbuild") || opt.CaselessEq("set"))
            {
                if (args.Length <= 2) { Help(p); return; }
                Zone zone = Matcher.FindZones(p, p.level, args[1]);
                if (zone == null) return;

                if (!zone.Access.CheckDetailed(p, data.Rank))
                {
                    p.Message("Hence, you cannot edit this zone."); return;
                }
                else if (opt.CaselessEq("perbuild"))
                {
                    EditZone(p, args, data, zone);
                }
                else
                {
                    SetZoneProp(p, args, zone);
                }
            }
            else
            {
                CreateZone(p, args, data, 0);
            }
        }

        public void CreateZone(Player p, string[] args, OrderData data, int offset)
        {
            if (p.level.FindZoneExact(args[offset]) != null)
            {
                p.Message("A zone with that name already exists. Use &T/zedit &Sto change it.");
                return;
            }
            if (!LevelInfo.Check(p, data.Rank, p.level, "create zones in this level")) return;

            Zone z = new Zone();
            z.Access.Min = p.level.BuildAccess.Min;
            z.Access.Max = p.level.BuildAccess.Max;
            // TODO readd once performance issues with massive zone Build blacklists are fixed
            //z.Access.CloneAccess(p.level.BuildAccess);

            z.Config.Name = args[offset];
            if (!PermissionOrd.Do(p, args, offset + 1, false, z.Access, data, p.level)) return;

            p.Message("Creating zone " + z.ColoredName);
            p.Message("Place or break two blocks to determine the edges.");
            p.MakeSelection(2, "Selecting region for &SNew zone", z, AddZone);
        }

        public bool AddZone(Player p, Vec3S32[] marks, object state, ushort block)
        {
            Zone zone = (Zone)state;
            zone.MinX = (ushort)Math.Min(marks[0].X, marks[1].X);
            zone.MinY = (ushort)Math.Min(marks[0].Y, marks[1].Y);
            zone.MinZ = (ushort)Math.Min(marks[0].Z, marks[1].Z);
            zone.MaxX = (ushort)Math.Max(marks[0].X, marks[1].X);
            zone.MaxY = (ushort)Math.Max(marks[0].Y, marks[1].Y);
            zone.MaxZ = (ushort)Math.Max(marks[0].Z, marks[1].Z);

            zone.AddTo(p.level);
            p.level.Save(true);
            p.Message("Created zone " + zone.ColoredName);
            return false;
        }

        public void DeleteZone(Player p, string[] args, OrderData data)
        {
            Level lvl = p.level;
            Zone zone = Matcher.FindZones(p, lvl, args[1]);
            if (zone == null) return;
            if (!zone.Access.CheckDetailed(p, data.Rank))
            {
                p.Message("Hence, you cannot delete this zone."); return;
            }

            zone.RemoveFrom(lvl);
            p.Message("Zone {0} &Sdeleted", zone.ColoredName);
            lvl.Save(true);
        }

        public void EditZone(Player p, string[] args, OrderData data, Zone zone)
        {
            PermissionOrd.Do(p, args, 2, false, zone.Access, data, p.level);
        }

        public void SetZoneProp(Player p, string[] args, Zone zone)
        {
            ColorDesc desc = default;
            if (args.Length < 4)
            {
                p.Message("No value provided. See &T/Help zone properties");
                return;
            }

            string opt = args[2], value = args[3];
            if (opt.CaselessEq("alpha"))
            {
                float alpha = 0;
                if (!OrderParser.GetReal(p, value, "Alpha", ref alpha, 0, 1)) return;

                zone.UnshowAll(p.level);
                zone.Config.ShowAlpha = (byte)(alpha * 255);
                zone.ShowAll(p.level);
            }
            else if (opt.CaselessEq("col") || opt.CaselessEq("color") || opt.CaselessEq("colour"))
            {
                if (!OrderParser.GetHex(p, value, ref desc)) return;

                zone.Config.ShowColor = value;
                zone.ShowAll(p.level);
            }
            else if (opt.CaselessEq("motd"))
            {
                zone.Config.MOTD = value;
                OnChangedZone(zone);
            }
            else if (OrdEnvironment.Handle(p, p.level, opt, value, zone.Config, "zone " + zone.ColoredName))
            {
                OnChangedZone(zone);
            }
            else
            {
                Help(p, "properties"); return;
            }
            p.level.Save(true);
        }

        public void OnChangedZone(Zone zone)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.ZoneIn == zone) OnChangedZoneEvent.Call(pl);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Zone add [name] <permissions>");
            p.Message("&HCreates a new zone, optionally also sets Build permissions");
            p.Message("&T/Zone del [name]");
            p.Message("&HDeletes the given zone");
            p.Message("&T/Zone perbuild [name] [permissions]");
            p.Message("&HSets Build permissions for the given zone");
            p.Message("&H  For syntax of permissions, see &T/Help PerBuild");
            p.Message("&T/Zone set [name] [property] [value]");
            p.Message("&HSets a property of this zone. See &T/Help zone properties");
        }

        public override void Help(Player p, string message)
        {
            if (message.CaselessEq("properties"))
            {
                p.Message("&T/Zone set [name] alpha [value]");
                p.Message("&HSets how solid the box shown around the zone is");
                p.Message("&H0 - not shown at all, 0.5 - half solid, 1 - fully solid");
                p.Message("&T/Zone set [name] color [hex color]");
                p.Message("&HSets the color of the box shown around the zone");
                p.Message("&T/Zone set [name] motd [value]");
                p.Message("&HSets the MOTD applied when in the zone. See &T/Help map motd");
                p.Message("&T/Zone set [name] [env property] [value]");
                p.Message("&HSets an env setting applied when in the zone. See &T/Help env");
            }
            else
            {
                base.Help(p, message);
            }
        }
    }

    public class OrdZoneTest : Order
    {
        public override string Name { get { return "ZoneTest"; } }
        public override string Shortcut { get { return "ZTest"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            p.Message("Place or delete a block where you would like to check for zones.");
            p.MakeSelection(1, "Selecting point for &SZone check", data, TestZone);
        }

        public bool TestZone(Player p, Vec3S32[] marks, object state, ushort block)
        {
            Vec3S32 P = marks[0];
            Level lvl = p.level;
            bool found = false;
            OrderData data = (OrderData)state;

            Zone[] zones = lvl.Zones.Items;
            for (int i = 0; i < zones.Length; i++)
            {
                Zone z = zones[i];
                if (!z.Contains(P.X, P.Y, P.Z)) continue;
                found = true;

                AccessResult status = z.Access.Check(p.name, data.Rank);
                bool allowed = z.Access.CheckAllowed(p);
                p.Message("  Zone {0} &S- {1}{2}", z.ColoredName, allowed ? "&a" : "&c", status);
            }

            if (!found) { p.Message("No zones affect this block."); }
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("&T/ZoneTest &H- Lists all zones affecting a block");
        }
    }

    public class OrdZoneList : Order
    {
        public override string Name { get { return "ZoneList"; } }
        public override string Shortcut { get { return "Zones"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            Zone[] zones = p.level.Zones.Items;
            Paginator.Output(p, zones, PrintZone,
                             "ZoneList", "zones", message);
        }

        public static void PrintZone(Player p, Zone zone)
        {
            p.Message("{0} &b- ({1}, {2}, {3}) to ({4}, {5}, {6})",
                      zone.ColoredName,
                      zone.MinX, zone.MinY, zone.MinZ,
                      zone.MaxX, zone.MaxY, zone.MaxZ);
        }

        public override void Help(Player p)
        {
            p.Message("&T/ZoneList &H- Lists all zones in current level");
        }
    }

    public class OrdZoneMark : Order
    {
        public override string Name { get { return "ZoneMark"; } }
        public override string Shortcut { get { return "ZMark"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("zm") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            Zone z;

            if (message.Length == 0)
            {
                z = p.ZoneIn;
                if (z == null) { p.Message("&STo use &T/ZoneMark &Swithout providing a zone name, you must be standing in a zone"); return; }
            }
            else
            {
                z = Matcher.FindZones(p, p.level, message);
                if (z == null) return;
            }

            if (!OrdMark.DoMark(p, z.MinX, z.MinY, z.MinZ))
            {
                p.Message("Cannot mark, no selection in progress.");
            }
            else
            {
                OrdMark.DoMark(p, z.MaxX, z.MaxY, z.MaxZ);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/ZoneMark [name]");
            p.Message("&HUses corners of the given zone as a &T/Mark &Hfor selections");
            p.Message("&T/ZoneMark");
            p.Message("&HUses corners of the zone you are currently standing in");
        }
    }
}