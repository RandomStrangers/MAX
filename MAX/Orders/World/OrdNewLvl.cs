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
using MAX.Generator;

namespace MAX.Orders.World
{
    public class OrdNewLvl : Order
    {
        public override string Name { get { return "NewLvl"; } }
        public override string Shortcut { get { return "Gen"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Admin, "can generate maps with advanced themes") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces(6);
            if (args.Length < 4) { Help(p); return; }

            Level lvl = null;
            try
            {
                lvl = GenerateMap(p, args, data);
                if (lvl == null) return;

                lvl.Save(true);
            }
            finally
            {
                lvl?.Dispose();
                Server.DoGC();
            }
        }

        public Level GenerateMap(Player p, string[] args, OrderData data)
        {
            if (args.Length < 4) return null;
            string theme = args.Length > 4 ? args[4] : Server.Config.DefaultMapGenTheme;
            string seed = args.Length > 5 ? args[5] : "";

            MapGen gen = MapGen.Find(theme);
            ushort x = 0, y = 0, z = 0;
            if (!MapGen.GetDimensions(p, args, 1, ref x, ref y, ref z)) return null;

            if (gen != null && gen.Type == GenType.Advanced && !CheckExtraPerm(p, data, 1)) return null;
            return MapGen.Generate(p, gen, args[0], x, y, z, seed);
        }

        public override void Help(Player p)
        {
            p.Message("&T/NewLvl [name] [width] [height] [length] [theme] <seed>");
            p.Message("&HCreates/generates a new level.");
            p.Message("  &HSizes must be between 1 and 16384");
            p.Message("  &HSeed is optional, and controls how the level is generated");
            p.Message("&HUse &T/Help NewLvl themes &Hfor a list of themes.");
            p.Message("&HUse &T/Help NewLvl [theme] &Hfor details on how seeds affect levels generated with that theme.");
        }

        public override void Help(Player p, string message)
        {
            MapGen gen = MapGen.Find(message);

            if (message.CaselessEq("theme") || message.CaselessEq("themes"))
            {
                MapGen.PrintThemes(p);
            }
            else if (gen == null)
            {
                p.Message("No theme found with name \"{0}\".", message);
                p.Message("&HUse &T/Help NewLvl themes &Hfor a list of themes.");
            }
            else
            {
                p.Message(gen.Desc);
            }
        }
    }
}