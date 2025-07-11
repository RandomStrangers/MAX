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
using MAX.SQL;
using System;
using System.Collections.Generic;

namespace MAX.Orders.Chatting
{
    public class OrdInbox : Order
    {
        public override string Name { get { return "Inbox"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override OrderParallelism Parallelism { get { return OrderParallelism.NoAndWarn; } }

        public const int i_text = 0, i_sent = 1, i_from = 2;

        public override void Use(Player p, string message, OrderData data)
        {
            if (!Database.TableExists("Inbox" + p.name))
            {
                p.Message("Your inbox is empty."); return;
            }

            List<string[]> entries = Database.GetRows("Inbox" + p.name, "Contents,TimeSent,PlayerFrom",
                                                      "ORDER BY TimeSent");
            if (entries.Count == 0)
            {
                p.Message("Your inbox is empty."); return;
            }

            string[] args = message.SplitSpaces(2);
            if (message.Length == 0)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Output(p, i + 1, entries[i]);
                }
            }
            else if (IsDeleteOrder(args[0]))
            {
                if (args.Length == 1)
                {
                    p.Message("You need to provide either \"all\" or a number."); return;
                }
                else if (args[1].CaselessEq("all"))
                {
                    int count = Database.DeleteRows("Inbox" + p.name, "");
                    p.Message("Deleted all {0} messages.", count);
                }
                else
                {
                    DeleteByID(p, args[1], entries);
                }
            }
            else
            {
                OutputByID(p, message, entries);
            }
        }

        public static void DeleteByID(Player p, string value, List<string[]> entries)
        {
            int num = 1;
            if (!OrderParser.GetInt(p, value, "Message number", ref num, 1)) return;

            if (num > entries.Count)
            {
                p.Message("Message #{0} does not exist.", num);
            }
            else
            {
                string[] entry = entries[num - 1];
                Database.DeleteRows("Inbox" + p.name,
                                    "WHERE PlayerFrom=@0 AND TimeSent=@1", entry[i_from], entry[i_sent]);
                p.Message("Deleted message #{0}", num);
            }
        }

        public static void OutputByID(Player p, string value, List<string[]> entries)
        {
            int num = 1;
            if (!OrderParser.GetInt(p, value, "Message number", ref num, 1)) return;

            if (num > entries.Count)
            {
                p.Message("Message #{0} does not exist.", num);
            }
            else
            {
                Output(p, num, entries[num - 1]);
            }
        }

        public static void Output(Player p, int num, string[] entry)
        {
            DateTime time = Database.ParseDBDate(entry[i_sent]);
            TimeSpan delta = DateTime.Now - time;
            string sender = p.FormatNick(entry[i_from]);

            p.Message("{0}) From {1} &a{2} ago:", num, sender, delta.Shorten());
            p.Message("  {0}", entry[i_text]);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Inbox");
            p.Message("&HDisplays all your messages.");
            p.Message("&T/Inbox [num]");
            p.Message("&HDisplays the message at [num]");
            p.Message("&T/Inbox del [num]/all");
            p.Message("&HDeletes the message at [num], deletes all messages if \"all\"");
            p.Message("  &HUse &T/Send &Hto reply to a message");
        }
    }
}