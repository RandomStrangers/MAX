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
using System;
using System.Collections.Generic;


namespace MAX.Events.PlayerEvents
{
    public enum PlayerAction { Me, Referee, UnReferee, AFK, UnAFK, Hide, Unhide };
    public enum MouseButton { Left, Right, Middle }
    public enum MouseAction { Pressed, Released }
    public enum TargetBlockFace { AwayX, TowardsX, AwayY, TowardsY, AwayZ, TowardsZ, None }

    public delegate void OnPlayerChat(Player p, string message);
    /// <summary> Called whenever a player sends chat to the server </summary>
    /// <remarks> You must cancel this event to prevent the message being sent to the user (and others). </remarks>
    public class OnPlayerChatEvent : IEvent<OnPlayerChat>
    {
        public static void Call(Player p, string message)
        {
            IEvent<OnPlayerChat>[] items = handlers.Items;
            // Don't use CallCommon, because this event is called very frequently
            // and want to avoid lots of pointless temp mem allocations
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, message); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerMove(Player p, Position next, byte yaw, byte pitch, ref bool cancel);
    /// <summary> Called whenever a player moves </summary>
    public class OnPlayerMoveEvent : IEvent<OnPlayerMove>
    {
        public static void Call(Player p, Position next, byte yaw, byte pitch, ref bool cancel)
        {
            IEvent<OnPlayerMove>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, next, yaw, pitch, ref cancel); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerOrder(Player p, string ord, string args, OrderData data);
    /// <summary> Called whenever a player uses a order </summary>
    /// <remarks> You must cancel this event to prevent "Unknown order!" being shown. </remarks>
    public class OnPlayerOrderEvent : IEvent<OnPlayerOrder>
    {
        public static void Call(Player p, string ord, string args, OrderData data)
        {
            IEvent<OnPlayerOrder>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, ord, args, data); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerConnect(Player p);
    /// <summary> Called whenever a player connects to the server </summary>
    public class OnPlayerConnectEvent : IEvent<OnPlayerConnect>
    {
        public static void Call(Player p)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p));
        }
    }

    public delegate void OnPlayerStartConnecting(Player p, string mppass);
    /// <summary> Called whenever a player tries connecting to the server </summary>
    /// <remarks> Called just after Handshake received, but before CPE handshake is performed. </remarks>
    public class OnPlayerStartConnectingEvent : IEvent<OnPlayerStartConnecting>
    {
        public static void Call(Player p, string mppass)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p, mppass));
        }
    }

    public delegate void OnPlayerFinishConnecting(Player p);
    /// <summary> Called whenever a player tries connecting to the server </summary>
    /// <remarks> Called after CPE handshake has been performed, and just before spawn map is sent. </remarks>
    public class OnPlayerFinishConnectingEvent : IEvent<OnPlayerFinishConnecting>
    {
        public static void Call(Player p)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p));
        }
    }

    public delegate void OnPlayerDying(Player p, ushort cause, ref bool cancel);
    /// <summary> Called whenever a player is about to die </summary>
    /// <remarks> Can be caused by e.g. walking into a deadly block like nerve_gas </remarks>
    public class OnPlayerDyingEvent : IEvent<OnPlayerDying>
    {
        public static void Call(Player p, ushort block, ref bool cancel)
        {
            IEvent<OnPlayerDying>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, block, ref cancel); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerDied(Player p, ushort cause, ref TimeSpan cooldown);
    /// <summary> Called whenever a player has died </summary>
    /// <remarks> Can be caused by e.g. walking into a deadly block like nerve_gas </remarks>
    public class OnPlayerDiedEvent : IEvent<OnPlayerDied>
    {
        public static void Call(Player p, ushort block, ref TimeSpan cooldown)
        {
            IEvent<OnPlayerDied>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, block, ref cooldown); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerDisconnect(Player p, string reason);
    /// <summary> Called whenever a player disconnects from the server. </summary>
    public class OnPlayerDisconnectEvent : IEvent<OnPlayerDisconnect>
    {
        public static void Call(Player p, string reason)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p, reason));
        }
    }

    public delegate void SelectionBlockChange(Player p, ushort x, ushort y, ushort z, ushort block);
    public delegate void OnBlockChanging(Player p, ushort x, ushort y, ushort z, ushort block, bool placing, ref bool cancel);
    /// <summary> Called whenever a player is manually placing or deleting a block </summary>
    /// <remarks> The client always assumes a block change succeeds. 
    /// So if you cancel this event, make sure you have sent a block change or reverted it using p.RevertBlock </remarks>
    public class OnBlockChangingEvent : IEvent<OnBlockChanging>
    {
        public static void Call(Player p, ushort x, ushort y, ushort z, ushort block, bool placing, ref bool cancel)
        {
            IEvent<OnBlockChanging>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, x, y, z, block, placing, ref cancel); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnBlockChanged(Player p, ushort x, ushort y, ushort z, ChangeResult result);
    /// <summary> Called whenever a player has manually placed or deleted a block </summary>
    public class OnBlockChangedEvent : IEvent<OnBlockChanged>
    {
        public static void Call(Player p, ushort x, ushort y, ushort z, ChangeResult result)
        {
            IEvent<OnBlockChanged>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, x, y, z, result); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerClick(Player p, MouseButton button, MouseAction action,
                                       ushort yaw, ushort pitch, byte entity,
                                       ushort x, ushort y, ushort z, TargetBlockFace face);
    /// <summary> Called whenever a player clicks their mouse </summary>
    public class OnPlayerClickEvent : IEvent<OnPlayerClick>
    {
        public static void Call(Player p, MouseButton btn, MouseAction action,
                                ushort yaw, ushort pitch, byte entityID,
                                ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            IEvent<OnPlayerClick>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, btn, action, yaw, pitch, entityID, x, y, z, face); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnMessageReceived(Player p, ref string message, ref bool cancel);
    /// <summary> Called whenever a player recieves a message from the server or from another player </summary>
    public class OnMessageRecievedEvent : IEvent<OnMessageReceived>
    {
        public static void Call(Player p, ref string message, ref bool cancel)
        {
            IEvent<OnMessageReceived>[] items = handlers.Items;
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, ref message, ref cancel); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnSentMap(Player p, Level prevLevel, Level level);
    /// <summary> Called when a player has been sent a new map </summary>
    public class OnSentMapEvent : IEvent<OnSentMap>
    {
        public static void Call(Player p, Level prevLevl, Level level)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p, prevLevl, level));
        }
    }


    public delegate void OnJoiningLevel(Player p, Level lvl, ref bool canJoin);
    /// <summary> Called when player intends to join a map </summary>
    /// <remarks> canJoin decides whether player will be allowed into the map </remarks>
    public class OnJoiningLevelEvent : IEvent<OnJoiningLevel>
    {
        public static void Call(Player p, Level lvl, ref bool canJoin)
        {
            IEvent<OnJoiningLevel>[] items = handlers.Items;
            // Can't use CallCommon because we need to pass arguments by ref
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, lvl, ref canJoin); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce);
    /// <summary> Called when a player has been sent a new map, and has been spawned in that map. </summary>
    public class OnJoinedLevelEvent : IEvent<OnJoinedLevel>
    {
        public static void Call(Player p, Level prevLevel, Level level, ref bool announce)
        {
            IEvent<OnJoinedLevel>[] items = handlers.Items;
            // Can't use CallCommon because we need to pass arguments by ref
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, prevLevel, level, ref announce); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerAction(Player p, PlayerAction action,
                                        string message, bool stealth);
    /// <summary> Called when a player performs an action. </summary>
    public class OnPlayerActionEvent : IEvent<OnPlayerAction>
    {
        public static void Call(Player p, PlayerAction action,
                                string message = null, bool stealth = false)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p, action, message, stealth));
        }
    }

    public delegate void OnSettingPrefix(Player p, List<string> prefixes);
    /// <summary> Called when prefix is being updated for a player </summary>
    public class OnSettingPrefixEvent : IEvent<OnSettingPrefix>
    {
        public static void Call(Player p, List<string> prefixes)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p, prefixes));
        }
    }

    public delegate void OnSettingColor(Player p, ref string color);
    /// <summary> Called when color is being updated for a player. </summary>
    /// <example> You can use this to ensure player's color remains fixed to red while in a game. </example>
    public class OnSettingColorEvent : IEvent<OnSettingColor>
    {
        public static void Call(Player p, ref string color)
        {
            IEvent<OnSettingColor>[] items = handlers.Items;
            // Can't use CallCommon because we need to pass arguments by ref
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, ref color); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnGettingMotd(Player p, ref string motd);
    /// <summary> Called when MOTD is being retrieved for a player </summary>
    /// <remarks> e.g. You can use this event to make one player always have +hax motd. </remarks>
    public class OnGettingMotdEvent : IEvent<OnGettingMotd>
    {
        public static void Call(Player p, ref string motd)
        {
            IEvent<OnGettingMotd>[] items = handlers.Items;
            // Can't use CallCommon because we need to pass arguments by ref
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, ref motd); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnSendingMotd(Player p, ref string motd);
    /// <summary> Called when MOTD is being sent to a player </summary>
    /// <remarks> To change MOTD for a player in general (e.g. for /fly), use OnGettingMotdEvent instead. </remarks>
    public class OnSendingMotdEvent : IEvent<OnSendingMotd>
    {
        public static void Call(Player p, ref string motd)
        {
            IEvent<OnSendingMotd>[] items = handlers.Items;
            // Can't use CallCommon because we need to pass arguments by ref
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, ref motd); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnPlayerSpawning(Player p, ref Position pos, ref byte yaw, ref byte pitch, bool respawning);
    /// <summary> Called when a player is initially spawning in a map, or is respawning (e.g. killed by deadly lava) </summary>
    public class OnPlayerSpawningEvent : IEvent<OnPlayerSpawning>
    {
        public static void Call(Player p, ref Position pos, ref byte yaw, ref byte pitch, bool respawning)
        {
            IEvent<OnPlayerSpawning>[] items = handlers.Items;
            // Can't use CallCommon because we need to pass arguments by ref
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, ref pos, ref yaw, ref pitch, respawning); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }

    public delegate void OnChangedZone(Player p);
    /// <summary> Called when player has moved into a different zone </summary>
    /// <remarks> The 'zone' the player moves into may be null. </remarks>
    public class OnChangedZoneEvent : IEvent<OnChangedZone>
    {
        public static void Call(Player p)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(p));
        }
    }

    public delegate void OnGettingCanSee(Player p, LevelPermission plRank, ref bool canSee, Player target);
    /// <summary> Called when code is checking if this player can see the given player </summary>
    public class OnGettingCanSeeEvent : IEvent<OnGettingCanSee>
    {
        public static void Call(Player p, LevelPermission plRank, ref bool canSee, Player target)
        {
            IEvent<OnGettingCanSee>[] items = handlers.Items;
            // Can't use CallCommon because we need to pass arguments by ref
            for (int i = 0; i < items.Length; i++)
            {
                try { items[i].method(p, plRank, ref canSee, target); }
                catch (Exception ex) { LogHandlerException(ex, items[i]); }
            }
        }
    }
}