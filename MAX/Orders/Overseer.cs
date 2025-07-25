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
using MAX.Generator;
using MAX.Orders.CPE;
using System.Collections.Generic;

namespace MAX.Orders.World
{

    /// <summary>
    /// Implements and manages behavior that can be called from OrdOverseer
    /// </summary>
    public static class Overseer
    {

        public static string orderShortcut = "os";
        public static void RegisterSubOrder(SubOrder subOrd)
        {
            subOrderGroup.Register(subOrd);
        }

        public static void UnregisterSubOrder(SubOrder subOrd)
        {
            subOrderGroup.Unregister(subOrd);
        }

        public static void UseOrder(Player p, string ord, string args)
        {
            OrderData data = default;
            data.Rank = LevelPermission.Owner;
            Order.Find(ord).Use(p, args, data);
        }

        public static string GetLevelName(Player p, int i)
        {
            string name = p.name.ToLower();
            return i == 1 ? name : name + i;
        }

        public static string NextLevel(Player p)
        {
            int realms = p.group.OverseerMaps;

            for (int i = 1; realms > 0; i++)
            {
                string map = GetLevelName(p, i);
                if (!LevelInfo.MapExists(map)) return map;

                if (LevelInfo.IsRealmOwner(p.name, map)) realms--;
            }
            p.Message("You have reached the limit for your overseer maps."); return null;
        }

        #region Help messages

        public static string[] blockPropsHelp = new string[] {
            "&T/os blockprops [id] [action] <args> &H- Changes properties of blocks in your map.",
            "&H  See &T/Help blockprops &Hfor how to use this order.",
            "&H  Remember to substitute /blockprops for /os blockprops when using the order based on the help",
        };

        public static string[] envHelp = new string[] {
            "&T/os env [fog/cloud/sky/shadow/sun] [hex color] &H- Changes env colors of your map.",
            "&T/os env level [height] &H- Sets the water height of your map.",
            "&T/os env cloudheight [height] &H-Sets cloud height of your map.",
            "&T/os env maxfog &H- Sets the max fog distance in your map.",
            "&T/os env horizon &H- Sets the \"ocean\" block outside your map.",
            "&T/os env border &H- Sets the \"bedrock\" block outside your map.",
            "&T/os env weather [sun/rain/snow] &H- Sets weather of your map.",
            " Note: If no hex or block is given, the default will be used.",
        };

        public static string[] gotoHelp = new string[] {
            "&T/os go &H- Teleports you to your first map.",
            "&T/os go [num] &H- Teleports you to your nth map.",
        };

        public static string[] kickHelp = new string[] {
            "&T/os kick [name] &H- Removes that player from your map.",
        };

        public static string[] kickAllHelp = new string[] {
            "&T/os kickall &H- Removes all other players from your map.",
        };

        public static string[] levelBlockHelp = new string[] {
            "&T/os lb [action] <args> &H- Manages custom blocks on your map.",
            "&H  See &T/Help lb &Hfor how to use this order.",
            "&H  Remember to substitute /lb for /os lb when using the order based on the help",
        };

        public static string[] mapHelp = new string[] {
            "&T/os map add [type - default is flat] &H- Creates your map (128x128x128)",
            "&T/os map add [width] [height] [length] [theme]",
            "&H  See &T/Help newlvl themes &Hfor a list of map themes.",
            "&T/os map physics [level] &H- Sets the physics on your map.",
            "&T/os map delete &H- Deletes your map",
            "&T/os map restore [num] &H- Restores backup [num] of your map",
            "&T/os map resize [width] [height] [length] &H- Resizes your map",
            "&T/os map save &H- Saves your map",
            "&T/os map pervisit [rank] &H- Sets the pervisit of you map",
            "&T/os map perbuild [rank] &H- Sets the perbuild of you map",
            "&T/os map texture [url] &H- Sets terrain.png for your map",
            "&T/os map texturepack [url] &H- Sets texture pack .zip for your map",
            "&T/os map [option] <value> &H- Toggles that map option.",
            "&H  See &T/Help map &Hfor a list of map options",
        };

        public static string[] presetHelp = new string[] {
            "&T/os preset [name] &H- Sets the env settings of your map to that preset's.",
        };

        public static string[] spawnHelp = new string[] {
            "&T/os setspawn &H- Sets the map's spawn point to your current position.",
        };

        public static string[] zoneHelp = new string[] {
            "&T/os zone add [name] &H- Allows them to Build in your map.",
            "&T/os zone del all &H- Deletes all zones in your map.",
            "&T/os zone del [name] &H- Prevents them from building in your map.",
            "&T/os zone list &H- Shows zones affecting a particular block.",
            "&T/os zone block [name] &H- Prevents them from joining your map.",
            "&T/os zone unblock [name] &H- Allows them to join your map.",
            "&T/os zone blacklist &H- Shows currently blacklisted players.",
        };

        public static string[] zonesHelp = new string[] {
            "&T/os zones [ord] [args]",
            "&HManages zones in your map. See &T/Help zone",
        };
        #endregion

        public static SubOrderGroup subOrderGroup = new SubOrderGroup(orderShortcut,
                new List<SubOrder>() {
                new SubOrder("BlockProps",    HandleBlockProps, blockPropsHelp, true, new string[] { "BlockProperties" }),
                new SubOrder("Env",        2, HandleEnv,        envHelp),
                new SubOrder("Go",            HandleGoto,       gotoHelp, false),
                new SubOrder("Kick",          HandleKick,       kickHelp),
                new SubOrder("KickAll",       HandleKickAll,    kickAllHelp),
                new SubOrder("LB",            HandleLevelBlock, levelBlockHelp, true, new string[] {"LevelBlock" }),
                new SubOrder("Map",        2, HandleMap,        mapHelp, false),
                new SubOrder("Preset",        HandlePreset,     presetHelp),
                new SubOrder("SetSpawn",      HandleSpawn,      spawnHelp, true, new string[] { "Spawn" }),
                new SubOrder("Zone",       2, HandleZone,       zoneHelp),
                new SubOrder("Zones",      2, HandleZones,      zonesHelp), }
            );

        public static void HandleBlockProps(Player p, string message)
        {
            if (message.Length == 0) { p.MessageLines(blockPropsHelp); return; }
            UseOrder(p, "BlockProperties", "level " + message);
        }

        public static void HandleEnv(Player p, string[] args)
        {
            Level lvl = p.level;
            if (OrdEnvironment.Handle(p, lvl, args[0], args[1], lvl.Config, lvl.ColoredName)) return;
            p.MessageLines(envHelp);
        }

        public static void HandleGoto(Player p, string map)
        {
            if (map.Length == 0) map = "1";

            if (!byte.TryParse(map, out byte mapNum))
            {
                p.MessageLines(gotoHelp); return;
            }
            map = GetLevelName(p, mapNum);

            if (LevelInfo.FindExact(map) == null)
                LevelActions.Load(p, map, !Server.Config.AutoLoadMaps);
            if (LevelInfo.FindExact(map) != null)
                PlayerActions.ChangeMap(p, map);
        }

        public static void HandleKick(Player p, string name)
        {
            if (name.Length == 0) { p.Message("You must specify a player to kick."); return; }
            Player pl = PlayerInfo.FindMatches(p, name);
            if (pl == null) return;

            if (pl.level == p.level)
            {
                PlayerActions.ChangeMap(pl, Server.mainLevel);
            }
            else
            {
                p.Message("Player is not on your level!");
            }
        }

        public static void HandleKickAll(Player p, string unused)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (pl.level == p.level && pl != p)
                    PlayerActions.ChangeMap(pl, Server.mainLevel);
            }
        }

        public static void HandleLevelBlock(Player p, string lbArgs)
        {
            CustomBlockOrder.Execute(p, lbArgs, p.DefaultOrdData, false, "/os lb");
        }

        public static void HandleMap(Player p, string[] args)
        {
            string ord = args[0];
            string value = args[1];
            string message = (ord + " " + value).Trim();

            if (message.Length == 0)
            {
                p.MessageLines(mapHelp);
                return;
            }

            SubOrderGroup.UsageResult result = mapSubOrderGroup.Use(p, message, false);
            if (result != SubOrderGroup.UsageResult.NoneFound) { return; }

            LevelOption opt = LevelOptions.Find(ord);
            if (opt == null)
            {
                p.Message("Could not find map order or map option \"{0}\".", ord);
                p.Message("See &T/help {0} map &Sto display every order.", orderShortcut);
                return;
            }
            if (DisallowedMapOption(opt.Name))
            {
                p.Message("&WYou cannot change the {0} map option via /{1} map.", opt.Name, orderShortcut);
                return;
            }
            if (!LevelInfo.IsRealmOwner(p.level, p.name))
            {
                p.Message("You may only use &T/{0} map {1}&S after you join your map.", orderShortcut, opt.Name);
                return;
            }
            opt.SetFunc(p, p.level, value);
            p.level.SaveSettings();
        }
        public static bool DisallowedMapOption(string opt)
        {
            return
                opt == LevelOptions.Speed || opt == LevelOptions.Overload || opt == LevelOptions.RealmOwner ||
                opt == LevelOptions.Goto || opt == LevelOptions.Unload;
        }

        public static SubOrderGroup mapSubOrderGroup = new SubOrderGroup(orderShortcut + " map",
                new List<SubOrder>() {
                new SubOrder("Physics",  HandleMapPhysics, null),
                new SubOrder("Add",      HandleMapAdd,     null, false, new string[] { "create", "new" } ),
                new SubOrder("Delete",   HandleMapDelete,  null, true , new string[] { "del", "remove" } ),
                new SubOrder("Save",     (p, unused)  => { UseOrder(p, "Save", ""); }, null),
                new SubOrder("Restore",  (p, arg) => { UseOrder(p, "Restore", arg); }, null),
                new SubOrder("Resize",   HandleMapResize,   null),
                new SubOrder("PerVisit", HandleMapPerVisit, null),
                new SubOrder("PerBuild", HandleMapPerBuild, null),
                new SubOrder("Texture",  HandleMapTexture,  null, true, new string[] { "texturezip", "texturepack" } ), }
            );

        public static void HandleMapPhysics(Player p, string message)
        {
            if (message == "0" || message == "1" || message == "2" || message == "3" || message == "4" || message == "5")
            {
                OrdPhysics.SetPhysics(p.level, int.Parse(message));
            }
            else
            {
                p.Message("Accepted numbers are: 0, 1, 2, 3, 4 or 5");
            }
        }
        public static void HandleMapAdd(Player p, string message)
        {
            if (p.group.OverseerMaps == 0)
            {
                p.Message("Your rank is not allowed to create any /{0} maps.", orderShortcut); return;
            }

            string level = NextLevel(p);
            if (level == null) return;
            string[] bits = message.SplitSpaces();

            if (message.Length == 0) message = "128 128 128";
            else if (bits.Length < 3) message = "128 128 128 " + message;
            string[] genArgs = (level + " " + message.TrimEnd()).SplitSpaces(6);

            OrdNewLvl newLvl = (OrdNewLvl)Order.Find("NewLvl"); // TODO: this is a nasty hack, find a better way
            Level lvl = newLvl.GenerateMap(p, genArgs, p.DefaultOrdData);
            if (lvl == null) return;

            MapGen.SetRealmPerms(p, lvl);
            p.Message("Use &T/{0} zone add [name] &Sto allow other players to Build in the map.", orderShortcut);

            try
            {
                lvl.Save(true);
            }
            finally
            {
                lvl.Dispose();
                Server.DoGC();
            }
        }
        public static void HandleMapDelete(Player p, string message)
        {
            if (message.Length > 0)
            {
                p.Message("To delete your current map, type &T/{0} map delete", orderShortcut);
                return;
            }
            UseOrder(p, "DeleteLvl", p.level.name);
        }
        public static void HandleMapResize(Player p, string message)
        {
            message = p.level.name + " " + message;
            string[] args = message.SplitSpaces();
            if (args.Length < 4)
            {
                p.Message("Not enough args provided! Usage:");
                p.Message("&T/{0} map resize [width] [height] [length]", orderShortcut);
                return;
            }

            if (OrdResizeLvl.DoResize(p, args, p.DefaultOrdData, out bool needConfirm)) return;

            if (!needConfirm) return;
            p.Message("Type &T/{0} map resize {1} {2} {3} confirm &Sif you're sure.",
                      orderShortcut, args[1], args[2], args[3]);
        }
        public static void HandleMapPerVisit(Player p, string message)
        {
            // Older realm maps didn't put you on visit whitelist, so make sure we put the owner here
            AccessController access = p.level.VisitAccess;
            if (!access.Whitelisted.CaselessContains(p.name))
            {
                access.Whitelist(Player.MAX, LevelPermission.MAX, p.level, p.name);
            }
            if (message.Length == 0)
            {
                p.Message("See &T/help pervisit &Sfor how to use this order, but don't include [level].");
                return;
            }
            message = p.level.name + " " + message;
            UseOrder(p, "PerVisit", message);
        }
        public static void HandleMapPerBuild(Player p, string message)
        {
            if (message.Length == 0)
            {
                p.Message("See &T/help perbuild &Sfor how to use this order, but don't include [level].");
                return;
            }
            message = p.level.name + " " + message;
            UseOrder(p, "PerBuild", message);
        }
        public static void HandleMapTexture(Player p, string message)
        {
            if (message.Length == 0) { message = "normal"; }
            UseOrder(p, "Texture", "levelzip " + message);
        }

        public static void HandlePreset(Player p, string preset)
        {
            HandleEnv(p, new string[] { "preset", preset });
        }

        public static void HandleSpawn(Player p, string unused)
        {
            UseOrder(p, "SetSpawn", "");
        }

        public static void HandleZone(Player p, string[] args)
        {
            string ord = args[0];
            string name = args[1];

            ord = ord.ToUpper();
            if (ord == "LIST")
            {
                UseOrder(p, "ZoneList", "");
            }
            else if (ord == "ADD")
            {
                UseOrder(p, "PerBuild", "+" + name);
            }
            else if (Order.IsDeleteOrder(ord))
            {
                UseOrder(p, "PerBuild", "-" + name);
            }
            else if (ord == "BLOCK")
            {
                UseOrder(p, "PerVisit", "-" + name);
            }
            else if (ord == "UNBLOCK")
            {
                UseOrder(p, "PerVisit", "+" + name);
            }
            else if (ord == "BLACKLIST")
            {
                List<string> blacklist = p.level.VisitAccess.Blacklisted;
                if (blacklist.Count > 0)
                {
                    p.Message("Blacklisted players: " + blacklist.Join());
                }
                else
                {
                    p.Message("No players are blacklisted from visiting this map.");
                }
            }
            else
            {
                p.MessageLines(zoneHelp);
            }
        }

        public static void HandleZones(Player p, string[] args)
        {
            if (args[1].Length == 0)
            {
                p.Message("Arguments required. See &T/Help zone");
            }
            else
            {
                UseOrder(p, "Zone", args[0] + " " + args[1]);
            }
        }
    }
}