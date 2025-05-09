﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MAX.DB;
using MAX.Events.ServerEvents;
using MAX.Events;

namespace MAX.Relay
{
    public abstract class BotControllersOrd : Order2
    {
        public override string type { get { return OrderTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public abstract RelayBot Bot { get; }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] parts = message.SplitSpaces();
            RelayBot bot = Bot;

            string ord = parts[0].ToLower();
            string arg = parts.Length > 1 ? parts[1] : "";

            switch (ord)
            {
                case "reload":
                    bot.LoadControllers();
                    p.Message("{0} controllers reloaded!", bot.RelayName);
                    break;

                case "add":
                    if (arg.Length == 0) { p.Message("You need to provide a name to add."); return; }

                    if (!bot.Controllers.Add(arg))
                    {
                        p.Message("{0} is already in the {1} controllers list.", arg, bot.RelayName);
                    }
                    else
                    {
                        bot.Controllers.Save();
                        p.Message("{0} added to the {1} controllers list.", arg, bot.RelayName);
                    }
                    break;

                case "remove":
                    if (arg.Length == 0) { p.Message("You need to provide a name to remove."); return; }

                    if (!bot.Controllers.Remove(arg))
                    {
                        p.Message("{0} is not in the {1} controllers list.", arg, bot.RelayName);
                    }
                    else
                    {
                        bot.Controllers.Save();
                        p.Message("{0} removed from the {1} controllers list.", arg, bot.RelayName);
                    }
                    break;

                case "list":
                    bot.Controllers.OutputPlain(p, bot.RelayName + " controllers",
                                                name + " list", arg);
                    break;

                case "rank":
                    if (arg.Length == 0)
                    {
                        p.Message("{0} controllers have the rank {1}", bot.RelayName,
                                  Group.GetColoredName(Server.Config.IRCControllerRank));
                        return;
                    }

                    Group grp = Matcher.FindRanks(p, arg);
                    if (grp == null) return;
                    if (Server.Config.IRCControllerRank > data.Rank)
                    {
                        p.Message("Cannot change the {0} controllers rank, " +
                                  "as it is currently a rank higher than yours.", bot.RelayName); return;
                    }
                    if (grp.Permission > data.Rank)
                    {
                        p.Message("Cannot set the {0} controllers rank to a rank higher than yours.", bot.RelayName); return;
                    }

                    Server.Config.IRCControllerRank = grp.Permission;
                    SrvProperties.Save();
                    p.Message("Set {0} controller rank to {1}&S.", bot.RelayName, grp.ColoredName);
                    break;

                default:
                    Help(p); break;
            }
        }

        public override void Help(Player p)
        {
            string ord = name;
            string relay = Bot.RelayName;

            p.Message("&T/{0} add/remove [name]", ord);
            p.Message("&HAdds or removes [name] from list of {0} controllers", relay);
            p.Message("&T/{0} reload/list", ord);
            p.Message("&HReloads or outputs list of {0} controllers", relay);
            p.Message("&T/{0} rank [rank]", ord);
            p.Message("&HSets which rank {0} controllers are treated as having", relay);
        }
    }
    public class RelayUser
    {
        public string ID, Nick;

        public virtual string GetMessagePrefix() { return ""; }
    }

    public delegate void OnDirectMessage(RelayBot bot, string channel, RelayUser user, string message, ref bool cancel);
    /// <summary> Called when an external communication service user sends a message directly to the relay bot </summary>
    public class OnDirectMessageEvent : IEvent<OnDirectMessage>
    {
        public static void Call(RelayBot bot, string channel, RelayUser user, string message, ref bool cancel)
        {
            IEvent<OnDirectMessage>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try
                {
                    items[i].method(bot, channel, user, message, ref cancel);
                }
                catch (Exception ex)
                {
                    LogHandlerException(ex, items[i]);
                }
            }
        }
    }

    public delegate void OnChannelMessage(RelayBot bot, string channel, RelayUser user, string message, ref bool cancel);
    /// <summary> Called when an external communication service user sends a message to the given channel </summary>
    public class OnChannelMessageEvent : IEvent<OnChannelMessage>
    {
        public static void Call(RelayBot bot, string channel, RelayUser user, string message, ref bool cancel)
        {
            IEvent<OnChannelMessage>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try
                {
                    items[i].method(bot, channel, user, message, ref cancel);
                }
                catch (Exception ex)
                {
                    LogHandlerException(ex, items[i]);
                }
            }
        }
    }

    /// <summary> Manages a connection to an external communication service </summary>
    public abstract class RelayBot
    {
        /// <summary> List of orders that cannot be used by relay bot controllers. </summary>
        public List<string> BannedOrders;

        /// <summary> List of channels to send public chat messages to </summary>
        public string[] Channels;

        /// <summary> List of channels to send staff only messages to </summary>
        public string[] OpChannels;

        /// <summary> List of user IDs that all chat from is ignored </summary>
        public string[] IgnoredUsers;

        public Player fakeGuest = new Player("RelayBot");
        public Player fakeStaff = new Player("RelayBot");
        public DateTime lastWho, lastOpWho, lastWarn;

        public bool canReconnect;
        public byte retries;
        public volatile Thread worker;
        /// <summary> Whether this relay bot can automatically reconnect </summary>
        public abstract bool CanReconnect { get; }


        /// <summary> The name of the service this relay bot communicates with </summary>
        /// <example> IRC, Discord </example>
        public abstract string RelayName { get; }

        /// <summary> Whether this relay bot is currently enabled </summary>
        public abstract bool Enabled { get; }

        /// <summary> Whether this relay bot is connected to the external communication service </summary>
        public bool Connected { get { return worker != null; } }

        /// <summary> List of users allowed to run in-game orders from the external communication service </summary>
        public PlayerList Controllers;

        /// <summary> The ID of the user associated with this relay bot </summary>
        /// <remarks> Do not cache this ID as it can change </remarks>
        public abstract string UserID { get; }


        /// <summary> Sends a message to all channels setup for general public chat </summary>
        public void SendPublicMessage(string message)
        {
            foreach (string chan in Channels)
            {
                SendMessage(chan, message);
            }
        }

        /// <summary> Sends a message to all channels setup for staff chat only </summary>
        public void SendStaffMessage(string message)
        {
            foreach (string chan in OpChannels)
            {
                SendMessage(chan, message);
            }
        }

        /// <summary> Sends a message to the given channel </summary>
        /// <remarks> Channels can specify either group chat or direct messages </remarks>
        public void SendMessage(string channel, string message)
        {
            if (!Enabled || !Connected) return;
            DoSendMessage(channel, message);
        }

        public abstract void DoSendMessage(string channel, string message);

        public string ConvertMessageCommon(string message)
        {
            message = EmotesHandler.Replace(message);
            message = ChatTokens.ApplyCustom(message);
            return message;
        }


        /// <summary> Attempts to connect to the external communication service </summary>
        /// <returns> null if connecting succeeded, otherwise the reason why connecting failed </returns>
        /// <remarks> e.g. is not enabled, is already connected, server shutting down </remarks>
        public string Connect()
        {
            if (!Enabled) return "is not enabled";
            if (Connected) return "is already connected";
            if (Server.shuttingDown) return "cannot connect as server shutting down";
            canReconnect = true;
            retries = 0;

            try
            {
                UpdateConfig();
                RunAsync();
            }
            catch (Exception e)
            {
                Logger.Log(LogType.RelayActivity, "Failed to connect to {0}!", RelayName);
                Logger.LogError(e);
                return "failed to connect - " + e.Message;
            }
            return null;
        }

        /// <summary> Forcefully disconnects from the external communication service </summary>
        /// <remarks> Does nothing if not connected </remarks>
        public void Disconnect(string reason)
        {
            if (!Connected) return;
            canReconnect = false;

            // silent, as otherwise it'll duplicate disconnect messages with IOThread
            try { DoDisconnect(reason); } catch { }
            // wait for worker to completely finish
            try { worker.Join(); } catch { }
        }

        /// <summary> Disconnects from the external communication service and then connects again </summary>
        public void Reset()
        {
            Disconnect(RelayName + " Bot resetting...");
            Connect();
        }

        public void OnReady()
        {
            Logger.Log(LogType.RelayActivity, "Connected to {0}!", RelayName);
            retries = 0;
        }


       public void IOThreadCore()
        {
            OnStart();

            while (CanReconnect && retries < 3)
            {
                try
                {
                    Logger.Log(LogType.RelayActivity, "Connecting to {0}...", RelayName);
                    DoConnect();
                    DoReadLoop();
                }
                catch (SocketException ex)
                {
                    Logger.Log(LogType.Warning, "Disconnected from {0} ({1}), retrying in {2} seconds..",
                               RelayName, ex.Message, 30);

                    // SocketException is usually due to complete connection dropout
                    retries = 0;
                    Thread.Sleep(30 * 1000);
                }
                catch (IOException ex)
                {
                    // IOException is an expected error, so don't log full details
                    Logger.Log(LogType.Warning, "{0} read error ({1})", RelayName, ex.Message);
                }
                catch (ObjectDisposedException ex)
                {
                    // ObjectDisposedException is an expected error, so don't log full details
                    Logger.Log(LogType.Warning, "{0} read error ({1})", RelayName, ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.LogError(RelayName + " relay error", ex);
                }
                retries++;

                try
                {
                    DoDisconnect("Reconnecting");
                }
                catch (Exception ex)
                {
                    Logger.LogError("Disconnecting from " + RelayName, ex);
                }
                Logger.Log(LogType.RelayActivity, "Disconnected from {0}!", RelayName);
            }
            OnStop();
        }

        public void IOThread()
        {
            try
            {
                IOThreadCore();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            worker = null;
        }

        /// <summary> Starts the read loop in a background thread </summary>
        public void RunAsync()
        {
            worker = new Thread(IOThread)
            {
                Name = RelayName + "_RelayBot",
                IsBackground = true
            };
            worker.Start();
        }

        public abstract void DoConnect();
        public abstract void DoReadLoop();
        public abstract void DoDisconnect(string reason);


        /// <summary> Loads the list of controller users from disc </summary>
        public abstract void LoadControllers();

        /// <summary> Reloads all configuration (including controllers list) </summary>
        public virtual void ReloadConfig()
        {
            UpdateConfig();
            LoadControllers();
        }

        public abstract void UpdateConfig();

        public void LoadBannedOrders()
        {
            BannedOrders = new List<string>() {""};

            if (!File.Exists("text/ircordblacklist.txt"))
            {
                File.WriteAllLines("text/ircordblacklist.txt", new string[] {
                                       "# Here you can put orders that cannot be used from the IRC bot.",
                                       "# Lines starting with \"#\" are ignored." ,
                                        "IRCBot", "DiscordBot", "OpRules", "IRCControllers", "DiscordControllers"
                });
            }

            foreach (string line in File.ReadAllLines("text/ircordblacklist.txt"))
            {
                if (!line.IsCommentLine()) BannedOrders.Add(line);
            }
        }


        public virtual void OnStart()
        {
            OnChatEvent.Register(OnChat, Priority.Low);
            OnChatSysEvent.Register(OnChatSys, Priority.Low);
            OnChatFromEvent.Register(OnChatFrom, Priority.Low);
            OnShuttingDownEvent.Register(OnShutdown, Priority.Low);
        }

        public virtual void OnStop()
        {
            OnChatEvent.Unregister(OnChat);
            OnChatSysEvent.Unregister(OnChatSys);
            OnChatFromEvent.Unregister(OnChatFrom);
            OnShuttingDownEvent.Unregister(OnShutdown);
        }


        public static bool FilterIRC(Player pl, object arg)
        {
            return !pl.Ignores.IRC && !pl.Ignores.IRCNicks.Contains((string)arg);
        }
        public static ChatMessageFilter filterIRC = FilterIRC;

        public static void MessageInGame(string srcNick, string message)
        {
            Chat.Message(ChatScope.Global, message, srcNick, filterIRC);
        }

        public string Unescape(Player p, string msg)
        {
            return msg
                .Replace("λFULL", UnescapeFull(p))
                .Replace("λNICK", UnescapeNick(p));
        }

        public virtual string UnescapeFull(Player p)
        {
            return Server.Config.IRCShowPlayerTitles ? p.FullName : p.group.Prefix + p.ColoredName;
        }

        public virtual string UnescapeNick(Player p) { return p.ColoredName; }
        public virtual string PrepareMessage(string msg) { return msg; }


        public void MessageToRelay(ChatScope scope, string msg, object arg, ChatMessageFilter filter)
        {
            ChatMessageFilter scopeFilter = Chat.scopeFilters[(int)scope];
            fakeGuest.group = Group.DefaultRank;

            if (scopeFilter(fakeGuest, arg) && (filter == null || filter(fakeGuest, arg)))
            {
                SendPublicMessage(msg); return;
            }

            fakeStaff.group = GetControllerRank();
            if (scopeFilter(fakeStaff, arg) && (filter == null || filter(fakeStaff, arg)))
            {
                SendStaffMessage(msg);
            }
        }

        public void OnChatSys(ChatScope scope, string msg, object arg,
                           ref ChatMessageFilter filter, bool relay)
        {
            if (!relay) return;

            msg = PrepareMessage(msg);
            MessageToRelay(scope, msg, arg, filter);
        }

        public void OnChatFrom(ChatScope scope, Player source, string msg,
                            object arg, ref ChatMessageFilter filter, bool relay)
        {
            if (!relay) return;

            msg = PrepareMessage(msg);
            MessageToRelay(scope, Unescape(source, msg), arg, filter);
        }

        public void OnChat(ChatScope scope, Player source, string msg,
                        object arg, ref ChatMessageFilter filter, bool relay)
        {
            if (!relay) return;

            msg = PrepareMessage(msg);
            MessageToRelay(scope, Unescape(source, msg), arg, filter);
        }

        public void OnShutdown(bool restarting, string message)
        {
            Disconnect(restarting ? "Server is restarting" : "Server is shutting down");
        }


        /// <summary> Simplifies some fancy characters (e.g. simplifies ” to ") </summary>
        public void SimplifyCharacters(StringBuilder sb)
        {
            // simplify fancy quotes
            sb.Replace("“", "\"");
            sb.Replace("”", "\"");
            sb.Replace("‘", "'");
            sb.Replace("’", "'");
        }
        public abstract string ParseMessage(string message);

        /// <summary> Handles a direct message written by the given user </summary>
        public void HandleDirectMessage(RelayUser user, string channel, string message)
        {
            if (IgnoredUsers.CaselessContains(user.ID)) return;
            message = ParseMessage(message).TrimEnd();
            if (message.Length == 0) return;

            bool cancel = false;
            OnDirectMessageEvent.Call(this, channel, user, message, ref cancel);
            if (cancel) return;

            string[] parts = message.SplitSpaces(2);
            string ordName = parts[0].ToLower();
            string ordArgs = parts.Length > 1 ? parts[1] : "";

            if (HandleListPlayers(user, channel, ordName, false)) return;
            Order.Search(ref ordName, ref ordArgs);
            if (HandleURL(user, channel, ordName)) return;
            Order.Search(ref ordName, ref ordArgs);
            if (!CanUseOrder(user, ordName, out string error))
            {
                if (error != null) SendMessage(channel, error);
                return;
            }

            ExecuteOrder(user, channel, ordName, ordArgs);
        }

        /// <summary> Handles a message written by the given user on the given channel </summary>
        public void HandleChannelMessage(RelayUser user, string channel, string message)
        {
            if (IgnoredUsers.CaselessContains(user.ID)) return;
            message = ParseMessage(message).TrimEnd();
            if (message.Length == 0) return;

            bool cancel = false;
            OnChannelMessageEvent.Call(this, channel, user, message, ref cancel);
            if (cancel) return;

            string[] parts = message.SplitSpaces(3);
            string rawOrd = parts[0].ToLower();
            bool chat = Channels.CaselessContains(channel);
            bool opchat = OpChannels.CaselessContains(channel);

            // Only reply to .who on channels configured to listen on
            if ((chat || opchat) && HandleListPlayers(user, channel, rawOrd, opchat)) return;
            if ((chat || opchat) && HandleURL(user, channel, rawOrd)) return;

            if (rawOrd.CaselessEq(Server.Config.IRCOrderPrefix))
            {
                if (!HandleOrder(user, channel, message, parts)) return;
            }
            string msg = user.GetMessagePrefix() + message;

            if (opchat)
            {
                Logger.Log(LogType.RelayChat, "(OPs): ({0}) {1}: {2}", RelayName, user.Nick, msg);
                Chat.MessageOps(string.Format("To Ops &f-&I({0}) {1}&f- {2}", RelayName, user.Nick,
                                              Server.Config.ProfanityFiltering ? ProfanityFilter.Parse(msg) : msg));
            }
            else if (chat)
            {
                Logger.Log(LogType.RelayChat, "({0}) {1}: {2}", RelayName, user.Nick, msg);
                MessageInGame(user.Nick, string.Format("&I({0}) {1}: &f{2}", RelayName, user.Nick,
                                                       Server.Config.ProfanityFiltering ? ProfanityFilter.Parse(msg) : msg));
            }
        }

        public bool HandleListPlayers(RelayUser user, string channel, string ord, bool opchat)
        {
            bool isWho = ord == ".who" || ord == ".players" || ord == "!players";
            DateTime last = opchat ? lastOpWho : lastWho;
            if (!isWho || (DateTime.UtcNow - last).TotalSeconds <= 5) return false;

            try
            {
                RelayPlayer p = new RelayPlayer(channel, user, this)
                {
                    group = Group.DefaultRank
                };
                MessagePlayers(p);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            if (opchat) lastOpWho = DateTime.UtcNow;
            else lastWho = DateTime.UtcNow;
            return true;
        }
        public bool HandleURL(RelayUser user, string channel, string ord)
        {
            bool isURL = ord.ToLower() == ".serverurl" || ord.ToLower() == ".url";
            if (!isURL) return false;
            try
            {
                RelayPlayer p = new RelayPlayer(channel, user, this)
                {
                    group = Group.DefaultRank
                };
                MessageURL(p);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return true;
        }

        /// <summary> Outputs the list of online players to the given user </summary>
        public virtual void MessagePlayers(RelayPlayer p)
        {
            Order.Find("Players").Use(p, "", p.DefaultOrdData);
        }
        public virtual void MessageURL(RelayPlayer p)
        {
            Order.Find("ServerUrl").Use(p, "", p.DefaultOrdData);
        }

        public bool HandleOrder(RelayUser user, string channel, string message, string[] parts)
        {
            string ordName = parts.Length > 1 ? parts[1].ToLower() : "";
            string ordArgs = parts.Length > 2 ? parts[2].Trim() : "";
            Order.Search(ref ordName, ref ordArgs);

            if (!CanUseOrder(user, ordName, out string error))
            {
                if (error != null) SendMessage(channel, error);
                return false;
            }

            return ExecuteOrder(user, channel, ordName, ordArgs);
        }

        public bool ExecuteOrder(RelayUser user, string channel, string ordName, string ordArgs)
        {
            Order ord = Order.Find(ordName);
            Player p = new RelayPlayer(channel, user, this);
            if (ord == null) { p.Message("Unknown order \"{0}\"", ordName); return false; }

            string logOrd = ordArgs.Length == 0 ? ordName : ordName + " " + ordArgs;
            Logger.Log(LogType.OrderUsage, "/{0} (by {1} from {2})", logOrd, user.Nick, RelayName);

            try
            {
                if (!p.CanUse(ord))
                {
                    ord.Permissions.MessageCannotUse(p);
                    return false;
                }
                if (!ord.SuperUseable)
                {
                    p.Message(ord.name + " can only be used in-game.");
                    return false;
                }
                ord.Use(p, ordArgs);
            }
            catch (Exception ex)
            {
                p.Message("CMD Error: " + ex);
                Logger.LogError(ex);
            }
            return true;
        }

        /// <summary> Returns whether the given relay user is allowed to execute the given order </summary>
        public bool CanUseOrder(RelayUser user, string ord, out string error)
        {
            error = null;

            if (!Controllers.Contains(user.ID))
            {
                // Intentionally show no message to non-controller users to avoid spam
                if ((DateTime.UtcNow - lastWarn).TotalSeconds <= 60) return false;

                lastWarn = DateTime.UtcNow;
                error = "Only " + RelayName + " Controllers are allowed to use in-game orders from " + RelayName;
                return false;
            }

            // Make sure controller is actually allowed to execute orders right now
            if (!CheckController(user.ID, ref error)) return false;

            if (BannedOrders.CaselessContains(ord))
            {
                error = "You are not allowed to use this order from " + RelayName + ".";
                return false;
            }
            return true;
        }

        /// <summary> Returns whether the given controller is currently allowed to execute orders </summary>
        /// <remarks> e.g. a user may have to login before they are allowed to execute orders </remarks>
        public abstract bool CheckController(string userID, ref string error);

        public Group GetControllerRank()
        {
            LevelPermission perm = Server.Config.IRCControllerRank;

            // find highest rank <= IRC controller rank
            for (int i = Group.GroupList.Count - 1; i >= 0; i--)
            {
                Group grp = Group.GroupList[i];
                if (grp.Permission <= perm) return grp;
            }
            return Group.DefaultRank;
        }

        public class RelayPlayer : Player
        {
            public string ChannelID;
            public RelayUser User;
            public RelayBot Bot;

            public RelayPlayer(string channel, RelayUser user, RelayBot bot) : base(bot.RelayName)
            {
                group = bot.GetControllerRank();

                ChannelID = channel;
                User = user;
                color = "&a";
                Bot = bot;

                if (user != null)
                {
                    string nick = "(" + bot.RelayName + " " + user.Nick + ")";
                    DatabaseID = NameConverter.InvalidNameID(nick);
                }
                SuperName = bot.RelayName;
            }

            public override void Message(string message)
            {
                Bot.SendMessage(ChannelID, message);
            }
        }
    }

    public abstract class RelayBotOrd : Order2
    {
        public override string type { get { return OrderTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public abstract RelayBot Bot { get; }

        public override void Use(Player p, string message, OrderData data)
        {
            RelayBot bot = Bot;

            if (message.CaselessEq("reset") || message.CaselessEq("reconnect"))
            {
                if (!bot.Enabled) { p.Message("{0} is not enabled", bot.RelayName); }
                bot.Reset();
            }
            else if (message.CaselessEq("connect"))
            {
                string err = bot.Connect();
                if (err != null) p.Message("{0} bot {1}", bot.RelayName, err);
            }
            else if (message.CaselessEq("disconnect"))
            {
                bot.Disconnect("Disconnecting IRC bot");
            }
            else
            {
                Help(p);
            }
        }

        public override void Help(Player p)
        {
            string ord = name;
            string relay = Bot.RelayName;

            p.Message("&T/{0} connect", ord);
            p.Message("&HCauses the {0} bot to connect to {0}.", relay);
            p.Message("&T/{0} disconnect", ord);
            p.Message("&HCauses the {0} bot to disconnect from {0}.", relay);
            p.Message("&T/{0} reset", ord);
            p.Message("&HCauses the {0} bot to disconnect then reconnect.", relay);
        }
    }
}