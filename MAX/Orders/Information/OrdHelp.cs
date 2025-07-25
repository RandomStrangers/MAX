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
using MAX.Blocks;
using MAX.Orders.CPE;
using System.Collections.Generic;


namespace MAX.Orders.Info
{
    public class OrdHelp : Order
    {
        public override string Name { get { return "Help"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("OrdHelp"), new OrderDesignation("Ranks", "ranks"),
                    new OrderDesignation("Colors", "colors"), new OrderDesignation("Emotes", "emotes") };
            }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0)
            {
                PrintHelpMenu(p);
            }
            else if (message.CaselessEq("ranks"))
            {
                PrintRanks(p);
            }
            else if (message.CaselessEq("colors") || message.CaselessEq("colours"))
            {
                PrintColors(p);
            }
            else if (message.CaselessEq("emotes") || message.CaselessStarts("emotes "))
            {
                PrintEmotes(p, message);
            }
            else
            {
                if (OrdOrders.ListOrders(p, message)) return;
                if (ParseOrder(p, message) || ParseBlock(p, message) || ParseAddon(p, message)) return;
                p.Message("Could not find order, addon or block specified.");
            }
        }

        public static void PrintHelpMenu(Player p)
        {
            p.Message("&HOrder Categories:");
            p.Message("  &T{0}", OrdOrders.GetCategories());
            p.Message("&HOther Categories:");
            p.Message("  &TRanks Colors Emotes Shortcuts Orders");
            p.Message("&HTo view help for a category, type &T/Help CategoryName");
            p.Message("&HTo see detailed help for a order, type &T/Help OrderName");
            p.Message("&HTo see your stats, type &T/Info");
            p.Message("&HTo see loaded maps, type &T/Maps");
            p.Message("&HTo view your personal world options, use &T/Realm");
            p.Message("&HTo join a map, type &T/Goto WorldName");
            p.Message("&HTo send private messages, type &T@PlayerName Message");
        }

        public static void PrintRanks(Player p)
        {
            foreach (Group grp in Group.GroupList)
            {
                p.Message("{0} &S- Draw: {1}, Perm: {2}, max realms: {3}",
                          grp.ColoredName, grp.DrawLimit, (int)grp.Permission, grp.OverseerMaps);
            }
        }

        public static void PrintColors(Player p)
        {
            p.Message("&fTo use a color, put a '%' and then put the color code.");
            p.Message("Colors Available:");

            p.Message("0 - &0{0} &S| 1 - &1{1} &S| 2 - &2{2} &S| 3 - &3{3}",
                      Colors.Name('0'), Colors.Name('1'), Colors.Name('2'), Colors.Name('3'));
            p.Message("4 - &4{0} &S| 5 - &5{1} &S| 6 - &6{2} &S| 7 - &7{3}",
                      Colors.Name('4'), Colors.Name('5'), Colors.Name('6'), Colors.Name('7'));

            p.Message("8 - &8{0} &S| 9 - &9{1} &S| a - &a{2} &S| b - &b{3}",
                      Colors.Name('8'), Colors.Name('9'), Colors.Name('a'), Colors.Name('b'));
            p.Message("c - &c{0} &S| d - &d{1} &S| e - &e{2} &S| f - &f{3}",
                      Colors.Name('c'), Colors.Name('d'), Colors.Name('e'), Colors.Name('f'));

            foreach (ColorDesc color in Colors.List)
            {
                if (color.Undefined || Colors.IsStandard(color.Code)) continue;
                OrdCustomColors.PrintColor(p, color);
            }
        }

        public static void PrintEmotes(Player p, string message)
        {
            char[] emotes = EmotesHandler.ControlCharReplacements.ToCharArray();
            emotes[0] = EmotesHandler.ExtendedCharReplacements[0]; // replace NULL with house

            string[] args = message.SplitSpaces(2);
            string modifier = args.Length > 1 ? args[1] : "";
            Paginator.Output(p, emotes, PrintEmote,
                             "Help emotes", "emotes", modifier);
        }

        public static void PrintEmote(Player p, char emote)
        {
            List<string> keywords = new List<string>();
            foreach (KeyValuePair<string, char> kvp in EmotesHandler.Keywords)
            {
                if (kvp.Value == emote) keywords.Add("(&S" + kvp.Key + ")");
            }
            p.Message("&f{0} &S- {1}", emote, keywords.Join());
        }

        public bool ParseOrder(Player p, string message)
        {
            string[] args = message.SplitSpaces(2);
            string ordName = args[0], ordArgs = "";
            Search(ref ordName, ref ordArgs);

            Order ord = Find(ordName);
            if (ord == null) return false;

            if (args.Length == 1)
            {
                ord.Help(p);
                Formatter.PrintOrderInfo(p, ord);
            }
            else
            {
                ord.Help(p, args[1]);
            }
            return true;
        }

        public bool ParseBlock(Player p, string message)
        {
            ushort block = Block.Parse(p, message);
            if (block == Block.Invalid) return false;

            p.Message("Block \"{0}\" appears as &b{1}",
                      message, Block.GetName(p, Block.Convert(block)));
            BlockPerms.Find(block).MessageCannotUse(p, "use");

            DescribePhysics(p, block);
            return true;
        }

        public void DescribePhysics(Player p, ushort b)
        {
            BlockProps props = p.IsSuper ? Block.Props[b] : p.level.Props[b];

            if (props.IsDoor)
            {
                p.Message("Door can be used as an 'openable' block if physics are enabled, will automatically toggle back to closed after a few seconds. " +
                               "door_green toggles to red instead of air - also see, odoor and tdoor.");
            }
            if (props.oDoorBlock != Block.Invalid)
            {
                p.Message("Odoor behaves like a user togglable door, does not auto close. " +
                               "Needs to be opened with a normal /door of any type and touched by other physics blocks, such as air_door to work.");
            }
            if (props.IsTDoor)
            {
                p.Message("Tdoor behaves like a regular /door, but allows physics blocks, e.g. active_water to flow through when opened.");
            }
            if (b == Block.Door_AirActivatable)
            {
                p.Message("Air_switch can be placed in front of doors to act as an automatic door opener when the player walks into the air_switch block.");
            }
            if (b == Block.Fire || b == Block.LavaFire)
            {
                p.Message("Fire blocks burn through wood and temporarily leaves coal and obsidian behind.");
            }
            if (b == Block.Deadly_Air)
            {
                p.Message("Nerve gas is an invisible, killer, static block.");
            }
            if (b == Block.Train)
            {
                p.Message("Place a train on {0} wool and it will move with physics on. Can ride with /ride.", Block.GetName(p, Block.Red));
            }
            if (b == Block.Snake || b == Block.SnakeTail)
            {
                p.Message("Snake crawls along the ground and kills players it touches if physics are on.");
            }
            if (b == Block.ZombieBody)
            {
                p.Message("Place a zombie on the map. Moves with physics and kills players on touch");
            }
            if (b == Block.Creeper)
            {
                p.Message("Place a creeper on the map. Moves with physics and kills players on touch, also explodes like tnt.");
            }
            if (b == Block.Fireworks)
            {
                p.Message("Place a firework. Left click to send a firework into the sky, which explodes into different colored wool.");
            }
            if (b == Block.RocketStart)
            {
                p.Message("Place a rocket starter. Left click to fire, explodes like tnt.");
            }
            if (b == Block.FiniteFaucet)
            {
                p.Message("Place a faucet block which spews out and places water on the map a few blocks at a time.");
            }
            if (b == Block.WaterFaucet || b == Block.LavaFaucet)
            {
                p.Message("Place a faucet block which water/lava will come out of. Works like waterfall/lavafall but water/lava disappears and is redropped periodically.");
            }
            if (b == Block.WaterDown || b == Block.LavaDown)
            {
                p.Message("Waterfall and lavafall flow straight down, catch them at the bottom, or they will flood the map like regular active_water/lava.");
            }
            if (b == Block.FiniteWater || b == Block.FiniteLava)
            {
                p.Message("Finite water and lava flow like active_water/lava, but never create more blocks than you place.");
            }
            if (b == Block.Deadly_Water || b == Block.Deadly_Lava)
            {
                p.Message("Hot lava and cold water are nonmoving killer blocks which kill players on touch.");
            }
            if (b == Block.Water || b == Block.Geyser || b == Block.Deadly_ActiveWater)
            {
                p.Message("Active_water flows horizontally through the map, active_cold_water and geyser kill players, geyser flows upwards.");
            }
            if (b == Block.Lava || b == Block.Magma || b == Block.Deadly_ActiveLava || b == Block.FastLava || b == Block.Deadly_FastLava)
            {
                p.Message("Active_lava and its fast counterparts flow horizontally through the map, active_hot_lava and magma kill players, " +
                               "magma flows upwards slowly if it is placed in a spot where it cannot flow then broken out.");
            }

            AnimalAI ai = props.AnimalAI;
            if (ai == AnimalAI.KillerAir || ai == AnimalAI.Fly || ai == AnimalAI.FleeAir)
            {
                p.Message("The bird blocks are different colored blocks that fly through the air if physics is on. Killer_phoenix kills players it touches");
            }
            if (ai == AnimalAI.FleeLava || ai == AnimalAI.FleeWater || ai == AnimalAI.KillerLava || ai == AnimalAI.KillerWater)
            {
                p.Message("The fish blocks are different colored blocks that swim around in active_water (lava_shark in active_lava), " +
                               "sharks and lava sharks eat players they touch.");
            }
        }

        public bool ParseAddon(Player p, string message)
        {
            Addon ad = Addon.FindCustom(message);
            if (ad == null) return false;

            ad.Help(p);
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("...really? Wow. Just...wow.");
        }
    }
}