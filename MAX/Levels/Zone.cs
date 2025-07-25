﻿/*
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
using MAX.Config;
using MAX.Events.PlayerEvents;
using MAX.Maths;
using System.Collections.Generic;

namespace MAX
{

    public class ZoneConfig : AreaConfig
    {
        [ConfigString("Name", "General", "", true)]
        public string Name = "";
        [ConfigString("ShowColor", "General", "000000", true)]
        public string ShowColor = "000000";
        [ConfigInt("ShowAlpha", "General", 0, 0, 255)]
        public int ShowAlpha = 0;

        public string Color { get { return Group.GetColor(BuildMin); } }
    }

    /// <summary> Encapuslates build access permissions for a zone. </summary>
    public class ZoneAccessController : AccessController
    {
        public ZoneConfig cfg;

        public ZoneAccessController(ZoneConfig cfg)
        {
            this.cfg = cfg;
        }

        public override LevelPermission Min
        {
            get { return cfg.BuildMin; }
            set { cfg.BuildMin = value; }
        }

        public override LevelPermission Max
        {
            get { return cfg.BuildMax; }
            set { cfg.BuildMax = value; }
        }

        public override List<string> Whitelisted { get { return cfg.BuildWhitelist; } }
        public override List<string> Blacklisted { get { return cfg.BuildBlacklist; } }

        public override string ColoredName { get { return "zone " + cfg.Color + cfg.Name; } }
        public override string Action { get { return "build in"; } }
        public override string ActionIng { get { return "building in"; } }
        public override string Type { get { return "build"; } }
        public override string MaxOrd { get { return null; } }


        public override void ApplyChanges(Player p, Level lvl, string msg)
        {
            lvl.Save(true);
            msg += " &Sin " + ColoredName;
            Logger.Log(LogType.UserActivity, "{0} &Son {1}", msg, lvl.name);

            lvl.Message(msg);
            if (p.level != lvl) p.Message("{0} &Son {1}", msg, lvl.ColoredName);
        }
    }

    public class Zone
    {
        public ushort MinX, MinY, MinZ;
        public ushort MaxX, MaxY, MaxZ;
        public byte ID;

        public ZoneConfig Config;
        public ZoneAccessController Access;
        public string ColoredName { get { return Config.Color + Config.Name; } }

        public Zone()
        {
            Config = new ZoneConfig();
            Access = new ZoneAccessController(Config);
        }


        public bool Contains(int x, int y, int z)
        {
            return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY && z >= MinZ && z <= MaxZ;
        }

        public bool CoversMap(Level lvl)
        {
            return MinX == 0 && MinY == 0 && MinZ == 0 &&
                MaxX == lvl.Width - 1 && MaxY == lvl.Height - 1 && MaxZ == lvl.Length - 1;
        }

        public bool Shows { get { return Config.ShowAlpha != 0 && Config.ShowColor.Length > 0; } }
        public void Show(Player p)
        {
            if (!Shows) return;

            Colors.TryParseHex(Config.ShowColor, out ColorDesc color);
            color.A = (byte)Config.ShowAlpha;

            Vec3U16 min = new Vec3U16(MinX, MinY, MinZ);
            Vec3U16 max = new Vec3U16((ushort)(MaxX + 1), (ushort)(MaxY + 1), (ushort)(MaxZ + 1));
            p.AddVisibleSelection(Config.Name, min, max, color, this);
        }

        public void ShowAll(Level lvl)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players)
            {
                if (p.level == lvl) Show(p);
            }
        }

        public void Unshow(Player p)
        {
            if (Shows) p.RemoveVisibleSelection(this);
        }

        public void UnshowAll(Level lvl)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players)
            {
                if (p.level == lvl) Unshow(p);
            }
        }

        public void AddTo(Level level)
        {
            level.Zones.Add(this);
        }

        public void RemoveFrom(Level level)
        {
            lock (level.Zones.locker)
            {
                UnshowAll(level);
                level.Zones.Remove(this);
            }

            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.ZoneIn != this) continue;
                pl.ZoneIn = null;
                OnChangedZoneEvent.Call(pl);
            }
        }

        public unsafe byte NextFreeZoneId(Level level)
        {
            byte* used = stackalloc byte[256];
            for (int i = 0; i < 256; i++) used[i] = 0;

            Zone[] zones = level.Zones.Items;
            for (int i = 0; i < zones.Length; i++)
            {
                byte id = zones[i].ID;
                used[id] = 1;
            }

            for (byte i = 0; i < 255; i++)
            {
                if (used[i] == 0) return i;
            }
            return 255;
        }
    }
}