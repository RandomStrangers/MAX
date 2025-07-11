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
using System.IO;

namespace MAX.Orders.CPE
{
    public class OrdEnvironment : Order
    {
        public override string Name { get { return "Environment"; } }
        public override string Shortcut { get { return "Env"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.CaselessEq("preset"))
            {
                MessagePresets(p); return;
            }

            Level lvl = null;
            EnvConfig cfg = null;
            string area = "server";

            if (message.CaselessStarts("global "))
            {
                message = message.Substring("global ".Length);
                cfg = Server.Config;
            }
            else if (message.CaselessStarts("level "))
            {
                message = message.Substring("level ".Length);
            }

            // Work on current level by default
            if (cfg == null)
            {
                if (p.IsSuper) { p.Message("&WWhen using &T/Env &Wfrom {0}, only &T/Env Global &Wis supported", p.SuperName); return; }

                lvl = p.level; cfg = lvl.Config;
                area = lvl.ColoredName;
                if (!LevelInfo.Check(p, data.Rank, lvl, "set env settings of this level")) return;
            }

            string[] args = message.SplitSpaces();
            string opt = args[0], value = args.Length > 1 ? args[1] : "";
            if (!Handle(p, lvl, opt, value, cfg, area)) { Help(p); }
        }

        public static bool Handle(Player p, Level lvl, string type, string value, EnvConfig cfg, string area)
        {
            if (type.CaselessEq("preset"))
            {
                EnvPreset preset = FindPreset(value);
                if (preset == null) { MessagePresets(p); return false; }

                cfg.SkyColor = preset.Sky;
                cfg.CloudColor = preset.Clouds;
                cfg.FogColor = preset.Fog;
                cfg.ShadowColor = preset.Shadow;
                cfg.LightColor = preset.Sun;
            }
            else if (type.CaselessEq("normal"))
            {
                cfg.ResetEnv();
                p.Message("Reset environment for {0} &Sto normal", area);
            }
            else
            {
                EnvOption opt = EnvOptions.Find(type);
                if (opt == null) return false;
                opt.SetFunc(p, area, cfg, value);
            }

            if (lvl == null)
            {
                Player[] players = PlayerInfo.Online.Items;
                foreach (Player pl in players)
                {
                    pl.SendCurrentEnv();
                }
                SrvProperties.Save();
            }
            else
            {
                SendEnv(lvl);
                lvl.SaveSettings();
            }
            return true;
        }

        public static void SendEnv(Level lvl)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level != lvl) continue;
                pl.SendCurrentEnv();
            }
        }

        public static EnvPreset FindPreset(string value)
        {
            EnvPreset preset = EnvPreset.Find(value);
            if (preset != null) return preset;

            if (File.Exists("presets/" + value.ToLower() + ".env"))
            {
                string text = File.ReadAllText("presets/" + value.ToLower() + ".env");
                return new EnvPreset(text);
            }
            return null;
        }

        public static void MessagePresets(Player p)
        {
            p.Message("&T/Env preset [type] &H- Applies an env preset on the map");
            p.Message("&HPresets: &f{0}", EnvPreset.Presets.Join(pr => pr.Key));

            string[] files = FileIO.TryGetFiles("presets", "*.env");
            if (files == null) return;

            string all = files.Join(f => Path.GetFileNameWithoutExtension(f));
            if (all.Length > 0) p.Message("&HCustom presets: &f" + all);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Environment global/level [variable] [value]");
            p.Message("&HChanges server default or current level's environment.");
            p.Message("&HSee &T/Help env variables &Hfor list of variables");
            p.Message("&T/Environment global/level normal");
            p.Message("&HResets all environment variables to default");
        }

        public override void Help(Player p, string message)
        {
            if (message.CaselessEq("variable") || message.CaselessEq("variables"))
            {
                p.Message("&HVariables: &f{0}", EnvOptions.Options.Join(o => o.Name));
                p.Message("&HUse &T/Help env [variable] &Hto see details for that variable");
                return;
            }
            else if (message.CaselessEq("presets"))
            {
                MessagePresets(p); return;
            }

            EnvOption opt = EnvOptions.Find(message);
            if (opt != null)
            {
                p.Message("&T/Env {0} [value]", opt.Name);
                p.Message(opt.Help);
                p.Message("&HUse 'normal' for [value] to reset to default");
            }
            else
            {
                p.Message("&WUnrecognised property \"{0}\"", message);
            }
        }
    }
}