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
using MAX.Maths;
using MAX.SQL;
using System;
using System.Collections.Generic;

namespace MAX.Blocks.Extended
{
    public static class MessageBlock
    {

        public static bool Handle(Player p, ushort x, ushort y, ushort z, bool alwaysRepeat)
        {
            if (!p.level.hasMessageBlocks) return false;

            string message = Get(p.level.MapName, x, y, z);
            if (message == null) return false;
            message = message.Replace("@p", p.name);

            if (message != p.prevMsg || alwaysRepeat || Server.Config.RepeatMBs)
            {
                Execute(p, message, new Vec3S32(x, y, z));
            }
            return true;
        }

        public static void Execute(Player p, string message, Vec3S32 mbCoords)
        {
            List<string> ords = GetParts(message, out string text);
            if (text != null) p.Message(text);

            OrderData data = p.DefaultOrdData;
            data.Context = OrderContext.MessageBlock;
            data.MBCoords = mbCoords;

            if (ords.Count == 1)
            {
                string[] parts = ords[0].SplitSpaces(2);
                string args = parts.Length > 1 ? parts[1] : "";
                p.HandleOrder(parts[0], args, data);
            }
            else if (ords.Count > 0)
            {
                p.HandleOrders(ords, data);
            }
            p.prevMsg = message;
        }

        public static bool Validate(Player p, string message, bool allOrds)
        {
            List<string> ords = GetParts(message, out string _);
            foreach (string ord in ords)
            {
                if (!CheckOrder(p, ord, allOrds)) return false;
            }
            return true;
        }

        public static bool CheckOrder(Player p, string message, bool allOrds)
        {
            string[] parts = message.SplitSpaces(2);
            string ordName = parts[0], ordArgs = "";
            Order.Search(ref ordName, ref ordArgs);

            Order ord = Order.Find(ordName);
            if (ord == null) return true;

            if (p.CanUse(ord) && (allOrds || !ord.MessageBlockRestricted)) return true;
            p.Message("You cannot use &T/{0} &Sin a messageblock.", ord.Name);
            return false;
        }

        public static string[] sep = new string[] { " |/" };
        public const StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries;
        public static List<string> empty = new List<string>();
        public static List<string> GetParts(string message, out string text)
        {
            if (message.IndexOf('|') == -1) return ParseSingle(message, out text);

            string[] parts = message.Split(sep, opts);
            List<string> ords = ParseSingle(parts[0], out text);
            if (parts.Length == 1) return ords;

            if (text != null) ords = new List<string>();
            for (int i = 1; i < parts.Length; i++)
                ords.Add(parts[i]);
            return ords;
        }

        public static List<string> ParseSingle(string message, out string text)
        {
            message = Chat.ParseInput(message, out bool isOrder);

            if (isOrder)
            {
                text = null; return new List<string>() { message };
            }
            else
            {
                text = message; return empty;
            }
        }


        /// <summary> Returns whether a Messages table for the given map exists in the DB. </summary>
        public static bool ExistsInDB(string map) { return Database.TableExists("Messages" + map); }

        /// <summary> Returns the coordinates for all message blocks in the given map. </summary>
        public static List<Vec3U16> GetAllCoords(string map)
        {
            List<Vec3U16> coords = new List<Vec3U16>();
            if (!ExistsInDB(map)) return coords;

            Database.ReadRows("Messages" + map, "X,Y,Z",
                                record => coords.Add(Portal.ParseCoords(record)));
            return coords;
        }

        /// <summary> Deletes all message blocks for the given map. </summary>
        public static void DeleteAll(string map)
        {
            Database.DeleteTable("Messages" + map);
        }

        /// <summary> Copies all message blocks from the given map to another map. </summary>
        public static void CopyAll(string src, string dst)
        {
            if (!ExistsInDB(src)) return;
            Database.CreateTable("Messages" + dst, LevelDB.createMessages);
            Database.CopyAllRows("Messages" + src, "Messages" + dst);
        }

        /// <summary> Moves all message blocks from the given map to another map. </summary>
        public static void MoveAll(string src, string dst)
        {
            if (!ExistsInDB(src)) return;
            Database.RenameTable("Messages" + src, "Messages" + dst);
        }


        /// <summary> Returns the text for the given message block in the given map. </summary>
        public static string Get(string map, ushort x, ushort y, ushort z)
        {
            string msg = Database.ReadString("Messages" + map, "Message",
                                             "WHERE X=@0 AND Y=@1 AND Z=@2", x, y, z);
            if (msg == null) return null;

            msg = msg.Trim().Replace("\\'", "\'");
            msg = msg.Cp437ToUnicode();
            return msg;
        }

        /// <summary> Deletes the given message block from the given map. </summary>
        public static void Delete(string map, ushort x, ushort y, ushort z)
        {
            Database.DeleteRows("Messages" + map,
                                "WHERE X=@0 AND Y=@1 AND Z=@2", x, y, z);
        }

        /// <summary> Creates or updates the given message block in the given map. </summary>
        public static void Set(string map, ushort x, ushort y, ushort z, string contents)
        {
            contents = contents.Replace("'", "\\'");
            contents = Colors.Escape(contents);
            contents = contents.UnicodeToCp437();

            Database.CreateTable("Messages" + map, LevelDB.createMessages);
            object[] args = new object[] { x, y, z, contents };

            int changed = Database.UpdateRows("Messages" + map, "Message=@3",
                                             "WHERE X=@0 AND Y=@1 AND Z=@2", args);
            if (changed == 0)
            {
                Database.AddRow("Messages" + map, "X,Y,Z, Message", args);
            }

            Level lvl = LevelInfo.FindExact(map);
            if (lvl != null) lvl.hasMessageBlocks = true;
        }
    }
}