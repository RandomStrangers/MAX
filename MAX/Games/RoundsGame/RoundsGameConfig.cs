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
using MAX.Config;
using MAX.Events.GameEvents;
using System.Collections.Generic;
using System.IO;

namespace MAX.Games
{
    /// <summary> Stores map-specific game configuration state. </summary>
    public class RoundsGameMapConfig
    {
        public void LoadFrom(ConfigElement[] cfg, string propsDir, string map)
        {
            string path = propsDir + map + ".properties";
            ConfigElement.ParseFile(cfg, path, this);
        }

        public void SaveTo(ConfigElement[] cfg, string propsDir, string map)
        {
            string path = propsDir + map + ".properties";
            if (!Directory.Exists(propsDir)) Directory.CreateDirectory(propsDir);
            ConfigElement.SerialiseSimple(cfg, path, this);
        }

        /// <summary> Saves this configuration to disc. </summary>
        public virtual void Save(string map)
        {
        }
        /// <summary> Loads this configuration from disc. </summary>
        public virtual void Load(string map)
        {
        }
        /// <summary> Applies default values for config fields which differ per map. </summary>
        /// <remarks> e.g. spawn positions, zones </remarks>
        public virtual void SetDefaults(Level lvl)
        {
        }
    }

    /// <summary> Stores overall game configuration state. </summary>
    public class RoundsGameConfig
    {
        [ConfigBool("start-on-server-start", "General", false)]
        public bool StartImmediately;
        [ConfigBool("set-main-level", "General", false)]
        public bool SetMainLevel;
        [ConfigBool("map-in-heartbeat", "General", false)]
        public bool MapInHeartbeat;
        [ConfigStringList("maps", "General")]
        public List<string> Maps = new List<string>();

        /// <summary> Whether users are allowed to auto-join maps used by this game. </summary>
        /// <remarks> If false, users can only join these maps when manually /load ed. </remarks>
        public bool AllowAutoload { get; }
        public string GameName { get; }
        public string Path;

        public ConfigElement[] cfg;
        public virtual void Save()
        {
            if (cfg == null) cfg = ConfigElement.GetAll(GetType());

            using (StreamWriter w = new StreamWriter(Path))
            {
                w.WriteLine("#" + GameName + " configuration");
                ConfigElement.Serialise(cfg, w, this);
            }
        }

        public virtual void Load()
        {
            if (cfg == null) cfg = ConfigElement.GetAll(GetType());
            ConfigElement.ParseFile(cfg, Path, this);
        }


        public static void AddMap(Player p, string map, LevelConfig lvlCfg, RoundsGame game)
        {
            RoundsGameConfig cfg = game.GetConfig();
            string coloredName = lvlCfg.Color + map;

            if (cfg.Maps.CaselessContains(map))
            {
                p.Message("{0} &Sis already in the list of {1} maps", coloredName, game.GameName);
            }
            else
            {
                p.Message("Added {0} &Sto the list of {1} maps", coloredName, game.GameName);
                cfg.Maps.Add(map);
                if (!cfg.AllowAutoload) lvlCfg.LoadOnGoto = false;

                cfg.Save();
                lvlCfg.SaveFor(map);
                OnMapsChangedEvent.Call(game);
            }
        }

        public static void RemoveMap(Player p, string map, LevelConfig lvlCfg, RoundsGame game)
        {
            RoundsGameConfig cfg = game.GetConfig();
            string coloredName = lvlCfg.Color + map;

            if (!cfg.Maps.CaselessRemove(map))
            {
                p.Message("{0} &Swas not in the list of {1} maps", coloredName, game.GameName);
            }
            else
            {
                p.Message("Removed {0} &Sfrom the list of {1} maps", coloredName, game.GameName);
                lvlCfg.AutoUnload = true;
                if (!cfg.AllowAutoload) lvlCfg.LoadOnGoto = true;

                cfg.Save();
                lvlCfg.SaveFor(map);
                OnMapsChangedEvent.Call(game);
            }
        }
    }
}