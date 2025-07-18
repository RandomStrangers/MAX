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
using MAX.Generator.Classic;
using MAX.Generator.fCraft;
using MAX.Generator.Realistic;
using MAX.Orders;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MAX.Generator
{
    public delegate bool MapGenFunc(Player p, Level lvl, MapGenArgs args);
    public delegate bool MapGenArgSelector(string arg);
    public enum GenType { Simple, fCraft, Advanced };

    public class MapGenArgs
    {
        public string Args;
        public int Seed;
        public MapGenBiomeName Biome = Server.Config.DefaultMapGenBiome;
        public bool RandomDefault = true;

        public MapGenArgSelector ArgFilter = (Args) => false;
        public MapGenArgSelector ArgParser = null;

        public bool ParseArgs(Player p)
        {
            bool gotSeed = false;
            foreach (string arg in Args.SplitSpaces())
            {
                if (arg.Length == 0) continue;

                if (ArgFilter(arg))
                {
                    if (!ArgParser(arg)) return false;
                }
                else if (int.TryParse(arg, out Seed))
                {
                    gotSeed = true;
                }
                else
                {
                    if (!OrderParser.GetEnum(p, arg, "Seed", ref Biome)) return false;
                }
            }

            if (!gotSeed) Seed = RandomDefault ? new Random().Next() : -1;
            return true;
        }
    }

    /// <summary> Map generators initialise the blocks in a level. </summary>
    /// <remarks> e.g. flatgrass generator, mountains theme generator, etc </remarks>
    public class MapGen
    {
        public string Theme, Desc;
        public GenType Type;
        public MapGenFunc GenFunc;

        /// <summary> Applies this map generator to the given level. </summary>
        /// <returns> Whether generation was actually successful. </returns>
        public bool Generate(Player p, Level lvl, string seed)
        {
            lvl.Config.Theme = Theme;
            lvl.Config.Seed = seed;

            MapGenArgs args = new MapGenArgs
            {
                Args = seed
            };

            bool success = GenFunc(p, lvl, args);
            MapGenBiome.Get(args.Biome).ApplyEnv(lvl.Config);
            return success;
        }


        /// <summary> Creates an RNG initialised with the given seed. </summary>
        public static Random MakeRng(string seed)
        {
            if (seed.Length == 0) return new Random();

            if (!int.TryParse(seed, out int value)) value = seed.GetHashCode();
            return new Random(value);
        } // TODO move to OrdMaze


        public static List<MapGen> Generators = new List<MapGen>();
        public static MapGen Find(string theme)
        {
            foreach (MapGen gen in Generators)
            {
                if (gen.Theme.CaselessEq(theme)) return gen;
            }
            return null;
        }

        public static string FilterThemes(GenType type)
        {
            return Generators.Join(g => g.Type == type ? g.Theme : null);
        }
        public static void PrintThemes(Player p)
        {
            p.Message("&HStandard themes: &f" + FilterThemes(GenType.Simple));
            p.Message("&HfCraft themes: &f" + FilterThemes(GenType.fCraft));
            p.Message("&HAdvanced themes: &f" + FilterThemes(GenType.Advanced));
        }


        public const string DEFAULT_HELP = "&HSeed affects how terrain is generated. If seed is the same, the generated level will be the same.";

        /// <summary> Adds a new map generator to the list of generators. </summary>
        public static void Register(string theme, GenType type, MapGenFunc func, string desc)
        {
            MapGen gen = new MapGen() { Theme = theme, GenFunc = func, Desc = desc, Type = type };
            Generators.Add(gen);
        }

        static MapGen()
        {
            RealisticMapGen.RegisterGenerators();
            SimpleGen.RegisterGenerators();
            FCraftMapGen.RegisterGenerators();
            AdvNoiseGen.RegisterGenerators();
            ClassicGenerator.RegisterGenerators();
            Register("Heightmap", GenType.Advanced, HeightmapGen.Generate,
                     "&HSeed specifies the URL of the heightmap image");
        }


        public static Level Generate(Player p, MapGen gen, string name,
                                     ushort x, ushort y, ushort z, string seed)
        {
            if (gen == null) { PrintThemes(p); return null; }
            if (!Formatter.ValidMapName(p, name)) return null;

            if (LevelInfo.MapExists(name))
            {
                p.Message("&WLevel \"{0}\" already exists", name); return null;
            }

            if (Interlocked.CompareExchange(ref p.GeneratingMap, 1, 0) == 1)
            {
                p.Message("You are already generating a map, please wait until that map has finished generating first.");
                return null;
            }

            Level lvl;
            try
            {
                p.Message("Generating map \"{0}\"..", name);
                lvl = new Level(name, x, y, z);

                DateTime start = DateTime.UtcNow;
                if (!gen.Generate(p, lvl, seed)) { lvl.Dispose(); return null; }
                Logger.Log(LogType.SystemActivity, "Generation completed in {0:F3} seconds",
                           (DateTime.UtcNow - start).TotalSeconds);

                string msg = seed.Length > 0 ? "λNICK&S created level {0}&S with seed \"{1}\"" : "λNICK&S created level {0}";
                Chat.MessageFrom(p, string.Format(msg, lvl.ColoredName, seed));
            }
            finally
            {
                Interlocked.Exchange(ref p.GeneratingMap, 0);
                Server.DoGC();
            }
            return lvl;
        }

        public static bool GetDimensions(Player p, string[] args, int i,
                                         ref ushort x, ref ushort y, ref ushort z, bool checkVolume = true)
        {
            return
                CheckMapAxis(p, args[i], "Width", ref x) &&
                CheckMapAxis(p, args[i + 1], "Height", ref y) &&
                CheckMapAxis(p, args[i + 2], "Length", ref z) &&
                (!checkVolume || CheckMapVolume(p, x, y, z));
        }

        public static bool CheckMapAxis(Player p, string input, string type, ref ushort len)
        {
            return OrderParser.GetUShort(p, input, type, ref len, 1, 16384);
        }

        public static bool CheckMapVolume(Player p, int x, int y, int z)
        {
            int limit = p.group.GenVolume;
            if ((long)x * y * z <= limit) return true;

            string text = "&WYou cannot create a map with over ";
            if (limit > 1000 * 1000) text += (limit / (1000 * 1000)) + " million blocks";
            else if (limit > 1000) text += (limit / 1000) + " thousand blocks";
            else text += limit + " blocks";
            p.Message(text);
            return false;
        }

        /// <summary> Sets default permissions for a newly generated realm map. </summary>
        public static void SetRealmPerms(Player p, Level lvl)
        {
            lvl.Config.RealmOwner = p.name;
            const LevelPermission rank = LevelPermission.MAX;
            lvl.BuildAccess.Whitelist(Player.MAX, rank, lvl, p.name);
            lvl.VisitAccess.Whitelist(Player.MAX, rank, lvl, p.name);

            Group grp = Group.Find(Server.Config.OSPerbuildDefault);
            if (grp == null) return;

            lvl.BuildAccess.SetMin(Player.MAX, rank, lvl, grp);
        }
    }
}