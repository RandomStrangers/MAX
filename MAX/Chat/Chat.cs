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
using MAX.Events.ServerEvents;
using MAX.Orders;
using System.Collections.Generic;
using System.Text;

namespace MAX
{
    public enum ChatScope
    {
        /// <summary> Messages all players on the server </summary>
        All,
        /// <summary> Messages all players on levels which see server-wide chat </summary>
        /// <remarks> Excludes players who are ignoring all chat </remarks>
        Global,
        /// <summary> Messages all players on a particular level </summary>
        /// <remarks> Excludes players who are ignoring all chat </remarks>
        Level,

        /// <summary> Messages all players of a given rank </summary>
        Rank,
        /// <summary> Messages all players who can use an ItemPerms argument. </summary>
        Perms,
        /// <summary> Message to a specific player </summary>
        PM,
    }

    public delegate bool ChatMessageFilter(Player pl, object arg);
    public static class Chat
    {

        public static ItemPerms OpchatPerms
        {
            get
            {
                ItemPerms perms = OrderExtraPerms.Find("OpChat", 1);
                return perms ?? new ItemPerms(LevelPermission.Operator);
            }
        }

        public static ItemPerms AdminchatPerms
        {
            get
            {
                ItemPerms perms = OrderExtraPerms.Find("AdminChat", 1);
                return perms ?? new ItemPerms(LevelPermission.Admin);
            }
        }

        public static string Format(string message, Player dst, bool tokens = true, bool emotes = true)
        {
            message = Colors.Escape(message);
            StringBuilder sb = new StringBuilder(message);
            if (tokens) ChatTokens.Apply(sb, dst);
            if (!emotes) return sb.ToString();

            if (dst.parseEmotes)
            {
                sb.Replace(":)", "(darksmile)");
                sb.Replace(":D", "(smile)");
                sb.Replace("<3", "(heart)");
            }
            message = EmotesHandler.Replace(sb.ToString());
            return message;
        }

        /// <summary> Returns true if the target player is ignoring chat messags by source. </summary>
        public static bool Ignoring(Player target, Player source)
        {
            if (target.Ignores.All) return source != target; // don't ignore messages from self
            return source != null && target.Ignores.Names.CaselessContains(source.name);
        }


        public static bool FilterAll(Player pl, object arg) { return true; }
        public static bool FilterGlobal(Player pl, object arg)
        {
            return pl.IsSuper || (pl.level.SeesServerWideChat && !pl.Ignores.All);
        }

        public static bool FilterLevel(Player pl, object arg)
        {
            return pl.level == arg && !pl.Ignores.All;
        }

        public static bool FilterRank(Player pl, object arg) { return pl.Rank == (LevelPermission)arg; }
        public static bool FilterPerms(Player pl, object arg) { return ((ItemPerms)arg).UsableBy(pl); }
        public static bool FilterPM(Player pl, object arg) { return pl == arg; }

        public static ChatMessageFilter[] scopeFilters = new ChatMessageFilter[] {
            FilterAll, FilterGlobal, FilterLevel,
            FilterRank, FilterPerms, FilterPM,
        };

        /// <summary> Filters chat to only players that can see the source player. </summary>
        public static ChatMessageFilter FilterVisible(Player source)
        {
            return (pl, obj) => pl.CanSee(source);
        }


        public static void MessageAll(string msg) { Message(ChatScope.All, msg, null, null); }
        public static void MessageGlobal(string msg) { Message(ChatScope.Global, msg, null, null); }
        public static void MessageOps(string msg)
        {
            Message(ChatScope.Perms, msg, OpchatPerms, null);
        }

        public static void MessageGlobal(string message, object a0)
        {
            MessageGlobal(string.Format(message, a0));
        }

        public static void MessageGlobal(string message, object a0, object a1)
        {
            MessageGlobal(string.Format(message, a0, a1));
        }

        public static void Message(ChatScope scope, string msg, object arg,
                                   ChatMessageFilter filter, bool relay = false)
        {
            Player[] players = PlayerInfo.Online.Items;
            ChatMessageFilter scopeFilter = scopeFilters[(int)scope];

            OnChatSysEvent.Call(scope, msg, arg, ref filter, relay);
            foreach (Player pl in players)
            {
                if (!scopeFilter(pl, arg)) continue;
                if (filter != null && !filter(pl, arg)) continue;
                pl.Message(msg);
            }
        }


        public static void MessageFromLevel(Player source, string msg)
        {
            MessageFrom(ChatScope.Level, source, msg, source.level, null);
        }

        public static void MessageFromOps(Player source, string msg)
        {
            MessageFrom(ChatScope.Perms, source, msg, OpchatPerms, null);
        }

        public static void MessageFrom(Player source, string msg,
                                       ChatMessageFilter filter = null, bool relay = false)
        {
            // super players don't have a level
            if (source.level == null || source.level.SeesServerWideChat)
            {
                MessageFrom(ChatScope.Global, source, msg, null, filter, relay);
            }
            else
            {
                string prefix = Server.Config.ServerWideChat ? "<Local>" : "";
                MessageFrom(ChatScope.Level, source, prefix + msg, source.level, filter);
            }
        }

        /// <summary> Sends a message from the given player (e.g. message when requesting a review) </summary>
        /// <remarks> For player chat type messages, Chat.MessageChat is more appropriate to use. </remarks>
        /// <remarks> Only players not ignoring the given player will see this message. </remarks>
        public static void MessageFrom(ChatScope scope, Player source, string msg, object arg,
                                       ChatMessageFilter filter, bool relay = false)
        {
            Player[] players = PlayerInfo.Online.Items;
            ChatMessageFilter scopeFilter = scopeFilters[(int)scope];

            OnChatFromEvent.Call(scope, source, msg, arg, ref filter, relay);
            foreach (Player pl in players)
            {
                if (!scopeFilter(pl, arg)) continue;
                if (filter != null && !filter(pl, arg)) continue;

                if (Ignoring(pl, source)) continue;
                pl.Message(UnescapeMessage(pl, source, msg));
            }
        }


        public static void MessageChat(Player source, string msg,
                                       ChatMessageFilter filter = null, bool relay = false)
        {
            // super players don't have a level
            if (source.level == null || source.level.SeesServerWideChat)
            {
                MessageChat(ChatScope.Global, source, msg, null, filter, relay);
            }
            else
            {
                string prefix = Server.Config.ServerWideChat ? "<Local>" : "";
                MessageChat(ChatScope.Level, source, prefix + msg, source.level, filter);
            }
        }

        /// <summary> Sends a chat message from the given player (e.g. regular player chat or /me) </summary>
        /// <remarks> Chat messages will increase player's total messages sent in /info,
        /// and count towards triggering automute for chat spamming </remarks>
        /// <remarks> Only players not ignoring the given player will see this message. </remarks>
        public static void MessageChat(ChatScope scope, Player source, string msg, object arg,
                                       ChatMessageFilter filter, bool relay = false)
        {
            Player[] players = PlayerInfo.Online.Items;
            ChatMessageFilter scopeFilter = scopeFilters[(int)scope];
            bool counted = false;

            // Filter out bad words
            if (Server.Config.ProfanityFiltering) msg = ProfanityFilter.Parse(msg);

            OnChatEvent.Call(scope, source, msg, arg, ref filter, relay);
            foreach (Player pl in players)
            {
                if (Ignoring(pl, source)) continue;
                // Always show message to self too (unless ignoring self)

                if (pl != source)
                {
                    if (!scopeFilter(pl, arg)) continue;
                    if (filter != null && !filter(pl, arg)) continue;

                    if (!counted) { source.TotalMessagesSent++; counted = true; }
                }
                else
                {
                    // don't send PM back to self
                    if (scope == ChatScope.PM) continue;
                }

                pl.Message(UnescapeMessage(pl, source, msg));
            }
            source.CheckForMessageSpam();
        }


        public static string UnescapeMessage(Player pl, Player src, string msg)
        {
            string nick = pl.FormatNick(src);
            msg = msg.Replace("λNICK", nick);

            if (pl.Ignores.Titles)
            {
                return msg.Replace("λFULL", src.GroupPrefix + nick);
            }
            else if (pl.Ignores.Nicks)
            {
                return msg.Replace("λFULL", src.color + src.prefix + src.truename);
            }
            else
            {
                return msg.Replace("λFULL", src.FullName);
            }
        }

        public static string ParseInput(string text, out bool isOrder)
        {
            isOrder = false;
            // Typing //Order appears in chat as /order
            // Suggested by McMrCat
            if (text.StartsWith("//")) return text.Substring(1);
            if (text[0] != '/') return text;

            isOrder = true;
            return text.Substring(1);
        }
    }

    public class PersistentMessages
    {
        public class PersistentMessage
        {
            public string message;
            public PersistentMessagePriority priority;
        }

        public object locker = new object();
        public Dictionary<CpeMessageType, List<PersistentMessage>> persistentMsgs =
            new Dictionary<CpeMessageType, List<PersistentMessage>>();

        /// <returns> false if there is currently a higher priority persistent message set for the given type </returns>
        public bool Handle(CpeMessageType type, ref string message, PersistentMessagePriority priority)
        {
            if (!IsPersistent(type)) return true;

            lock (locker)
            {

                if (!persistentMsgs.TryGetValue(type, out List<PersistentMessage> field))
                {
                    field = new List<PersistentMessage>();
                    persistentMsgs[type] = field;
                }

                PersistentMessage curMsg = null;
                foreach (PersistentMessage msg in field)
                {
                    if (msg.priority == priority) { curMsg = msg; break; }
                }

                if (string.IsNullOrEmpty(message))
                {
                    field.Remove(curMsg);
                    PersistentMessage highestRemainingMsg = null;

                    foreach (PersistentMessage msg in field)
                    {
                        if (highestRemainingMsg == null || msg.priority > highestRemainingMsg.priority) highestRemainingMsg = msg;
                    }

                    // revert to the highest priority remaining message
                    if (highestRemainingMsg != null) message = highestRemainingMsg.message;
                }
                else
                {
                    if (curMsg == null)
                    {
                        curMsg = new PersistentMessage
                        {
                            priority = priority
                        };
                        field.Add(curMsg);
                    }
                    curMsg.message = message;
                }

                // don't send if there is a a higher priority message currently in this field
                foreach (PersistentMessage msg in field)
                {
                    if (msg.priority > priority) return false;
                }
            }
            return true;
        }

        public static bool IsPersistent(CpeMessageType type)
        {
            return
                type == CpeMessageType.Status1 || type == CpeMessageType.Status2 || type == CpeMessageType.Status3 ||
                type == CpeMessageType.BottomRight1 || type == CpeMessageType.BottomRight2 || type == CpeMessageType.BottomRight3;
        }
    }

    public enum PersistentMessagePriority : int
    {
        Lowest = 0,
        Low = 5,
        Normal = 10,
        High = 15,
        Highest = 20
    }
}