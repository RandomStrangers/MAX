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
using MAX.Authentication;
using MAX.DB;
using MAX.Drawing;
using MAX.Events.EconomyEvents;
using MAX.Events.EntityEvents;
using MAX.Events.PlayerDBEvents;
using MAX.Events.PlayerEvents;
using MAX.Games;
using MAX.Maths;
using MAX.Network;
using MAX.SQL;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;


namespace MAX
{
    public class MAX : Player
    {
        public MAX() : base("&S(&4MAX&S)")
        {
            group = Group.MAXRank;
            color = "&S";
            SuperName = "&S&4MAX&S";
        }

        public override string FullName
        {
            get { return "&S[&4MAX&S]"; }
        }

        public override void Message(string message)
        {
            Logger.Log(LogType.MAXMessage, message);
        }
    }
    public partial class Player : Entity, IDisposable
    {

        public static int sessionCounter;
        public static Player MAX = new MAX();
        //This is so that addon devs can declare a player without needing a socket..
        //They would still have to do p.Dispose()..
        public Player(string playername)
        {
            name = playername;
            truename = playername;
            DisplayName = playername;

            SetIP(IPAddress.Loopback);
            IsSuper = true;
        }

        public const int SESSION_ID_MASK = (1 << 20) - 1;
        public Player(INetSocket socket, IGameSession session)
        {
            Socket = socket;
            Session = session;
            SetIP(Socket.IP);

            spamChecker = new SpamChecker(this);
            partialLog = new List<DateTime>(20);
            session.ID = Interlocked.Increment(ref sessionCounter) & SESSION_ID_MASK;

            for (int b = 0; b < BlockBindings.Length; b++)
            {
                BlockBindings[b] = (ushort)b;
            }
        }

        public override byte EntityID { get { return id; } }
        public override Level Level { get { return level; } }
        public override bool RestrictsScale { get { return true; } }

        /// <summary> Whether this player can see the given player. </summary>
        public bool CanSee(Player target) { return CanSee(target, Rank); }
        /// <summary> Whether this player can see the given player, as if they were the given rank. </summary>
        public bool CanSee(Player target, LevelPermission plRank)
        {
            if (target == this || target == null) return true;

            bool canSee = !target.hidden || plRank >= target.hideRank;
            OnGettingCanSeeEvent.Call(this, plRank, ref canSee, target);
            return canSee;
        }

        public override bool CanSeeEntity(Entity target)
        {
            if (target == this) return true; // always see self

            bool canSee = CanSee(target as Player, Rank);
            OnGettingCanSeeEntityEvent.Call(this, ref canSee, target);
            return canSee;
        }

        public ushort GetHeldBlock()
        {
            if (ModeBlock != Block.Invalid) return ModeBlock;
            return BlockBindings[ClientHeldBlock];
        }

        public string GetMotd()
        {
            Zone zone = ZoneIn;
            string motd = zone == null ? "ignore" : zone.Config.MOTD;

            // fallback to level MOTD, then rank MOTD, then server MOTD            
            if (motd == "ignore") motd = level.Config.MOTD;
            if (motd == "ignore") motd = string.IsNullOrEmpty(group.MOTD) ? Server.Config.MOTD : group.MOTD;

            OnGettingMotdEvent.Call(this, ref motd);
            return motd;
        }

        public void SetPrefix()
        {
            List<string> prefixes = new List<string>(6)
            {
                Game.Referee ? "&2[Ref] " : "",
                GroupPrefix.Length > 0 ? GroupPrefix + color : ""
            };

            Team team = Game.Team;
            prefixes.Add(team == null ? "" : "<" + team.Color + team.Name + color + "> ");

            IGame game = IGame.GameOn(level);
            prefixes.Add(game == null ? "" : game.GetPrefix(this));

            bool devPrefix = Server.Config.SoftwareStaffPrefixes &&
                             Server.Devs.CaselessContains(truename);

            prefixes.Add(devPrefix ? MakeTitle("Dev", "&4") : "");
            prefixes.Add(title.Length > 0 ? MakeTitle(title, titlecolor) : "");

            OnSettingPrefixEvent.Call(this, prefixes);
            prefix = prefixes.Join("");
        }

        public string MakeTitle(string title, string titleCol)
        {
            return color + "[" + titleCol + title + color + "] ";
        }

        /// <summary> Raises OnSettingColorEvent then sets color. </summary>
        public void SetColor(string col)
        {
            OnSettingColorEvent.Call(this, ref col);
            color = col;
        }

        /// <summary> Calls SetColor, then updates state depending on color. 
        /// (e.g. entity name, tab list name, prefix, etc) </summary>
        public void UpdateColor(string col)
        {
            string prevCol = color;
            SetColor(col);

            if (prevCol == color) return;
            Entities.GlobalRespawn(this);
            SetPrefix();
        }

        public bool IsLikelyInsideBlock()
        {
            AABB bb = ModelBB.OffsetPosition(Pos);
            // client collision is very slightly more precise than server
            // so move slightly in to avoid false positives
            bb = bb.Expand(-1);
            return AABB.IntersectsSolidBlocks(bb, level);
        }

        public void SaveStats()
        {
            bool cancel = false;
            OnInfoSaveEvent.Call(this, ref cancel);
            if (cancel) return;

            // Player disconnected before SQL data was retrieved
            if (!gotSQLData) return;
            long blocks = PlayerData.Pack(TotalPlaced, TotalModified);
            long drawn = PlayerData.Pack(TotalDeleted, TotalDrawn);
            Database.UpdateRows("Players", "IP=@0, LastLogin=@1, totalLogin=@2, totalDeaths=@3, Money=@4, " +
                                "totalBlocks=@5, totalCuboided=@6, totalKicked=@7, TimeSpent=@8, Messages=@9", "WHERE Name=@10",
                                ip, LastLogin.ToString(Database.DateFormat),
                                TimesVisited, TimesDied, money, blocks,
                                drawn, TimesBeenKicked, (long)TotalTime.TotalSeconds, TotalMessagesSent, name);
        }

        public void SetIP(IPAddress addr)
        {
            IP = addr;
            ip = addr.ToString();
        }

        public bool CanUse(Order ord) { return ord.Permissions.UsableBy(this); }
        public bool CanUse(string ordName)
        {
            Order ord = Order.Find(ordName);
            return ord != null && CanUse(ord);
        }

        public bool MarkPossessed(string marker = "")
        {
            if (marker.Length > 0)
            {
                Player controller = PlayerInfo.FindExact(marker);
                if (controller == null) return false;
                marker = " (" + controller.color + controller.name + color + ")";
            }

            Entities.GlobalDespawn(this, true);
            Entities.GlobalSpawn(this, true, marker);
            return true;
        }

        #region == DISCONNECTING ==

        /// <summary> Disconnects the player from the server, 
        /// with their default logout message shown in chat. </summary>
        public void Disconnect() { LeaveServer(PlayerInfo.GetLogoutMessage(this), "disconnected", false); }

        /// <summary> Kicks the player from the server,
        /// with the given messages shown in chat and in the disconnect packet. </summary>
        public void Kick(string chatMsg, string discMsg, bool sync = false)
        {
            LeaveServer(chatMsg, discMsg, true, sync);
        }

        /// <summary> Kicks the player from the server,
        /// with the given message shown in both chat and in the disconnect packet. </summary>
        public void Kick(string discMsg) { Kick(discMsg, false); }
        public void Kick(string discMsg, bool sync = false)
        {
            string chatMsg = discMsg;
            if (chatMsg.Length > 0) chatMsg = "(" + chatMsg + ")"; // old format
            LeaveServer(chatMsg, discMsg, true, sync);
        }

        /// <summary> Disconnects the players from the server,
        /// with the given messages shown in chat and in the disconnect packet. </summary>
        public void Leave(string chatMsg, string discMsg, bool sync = false)
        {
            LeaveServer(chatMsg, discMsg, false, sync);
        }

        /// <summary> Disconnects the players from the server,
        /// with the same message shown in chat and in the disconnect packet. </summary>        
        public void Leave(string msg) { Leave(msg, false); }
        public void Leave(string msg, bool sync = false)
        {
            LeaveServer(msg, msg, false, sync);
        }

        public bool leftServer = false;
        public void LeaveServer(string chatMsg, string discMsg, bool isKick, bool sync = false)
        {
            if (leftServer || IsSuper) return;
            leftServer = true;
            CriticalTasks.Clear();
            ZoneIn = null;

            // Disconnected before sent handshake
            if (name == null)
            {
                Socket?.Close();
                Logger.Log(LogType.UserActivity, "{0} disconnected.", IP);
                return;
            }

            Server.reviewlist.Remove(name);
            try
            {
                if (Socket.Disconnected)
                {
                    PlayerInfo.Online.Remove(this);
                    return;
                }

                weapon?.Disable();
                if (chatMsg != null) chatMsg = Colors.Escape(chatMsg);
                discMsg = Colors.Escape(discMsg);

                string kickPacketMsg = ChatTokens.Apply(discMsg, this);
                Session.SendKick(kickPacketMsg, sync);
                Socket.Disconnected = true;
                ZoneIn = null;
                if (isKick) TimesBeenKicked++;

                if (!loggedIn)
                {
                    PlayerInfo.Online.Remove(this);
                    Logger.Log(LogType.UserActivity, "{0} ({1}) disconnected. ({2})", truename, IP, discMsg);
                    return;
                }

                Entities.DespawnEntities(this, false);
                ShowDisconnectInChat(chatMsg, isKick);
                SaveStats();
                PlayerInfo.Online.Remove(this);
                OnPlayerDisconnectEvent.Call(this, discMsg);
                level.AutoUnload();
                Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError("Error disconnecting player", e);
            }
            finally
            {
                Socket.Close();
            }
        }

        public void ShowDisconnectInChat(string chatMsg, bool isKick)
        {
            if (chatMsg == null) return;

            if (!isKick)
            {
                string leaveMsg = "&c- λFULL &S" + chatMsg;
                if (Server.Config.GuestLeavesNotify || Rank > LevelPermission.Guest)
                {
                    Chat.MessageFrom(ChatScope.All, this, leaveMsg, null, Chat.FilterVisible(this), !hidden);
                }
                Logger.Log(LogType.UserActivity, "{0} disconnected ({1}&S).", truename, chatMsg);
            }
            else
            {
                string leaveMsg = "&c- λFULL &Skicked &S" + chatMsg;
                Chat.MessageFrom(ChatScope.All, this, leaveMsg, null, null, true);
                Logger.Log(LogType.UserActivity, "{0} kicked ({1}&S).", truename, chatMsg);
            }
        }

        public void Dispose()
        {
            Extras.Clear();

            foreach (CopyState cState in CopySlots)
            {
                cState?.Clear();
            }
            CopySlots.Clear();

            DrawOps.Clear();
            spamChecker?.Clear();
            ClearSerialOrders();
        }

        #endregion
        #region == OTHER ==

        public const string USERNAME_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890._";

        public byte UserType() { return group.Blocks[Block.Bedrock] ? (byte)100 : (byte)0; }

        #endregion

        /// <summary> Returns whether the player is currently allowed to talk. </summary>
        public bool CanSpeak()
        {
            return IsMAX || (!muted && !Unverified && (voice || !Server.chatmod));
        }

        public bool CheckCanSpeak(string action)
        {
            if (IsMAX) return true;

            if (muted)
            {
                Message("Cannot {0} &Swhile muted", action); return false;
            }
            if (Server.chatmod && !voice)
            {
                Message("Cannot {0} &Swhile chat moderation is on without &T/Voice&S", action); return false;
            }
            if (Unverified)
            {
                PassAuthenticator.Current.RequiresVerification(this, action);
                return false;
            }
            return true;
        }

        /// <summary> Checks if player is this player requires additional verification </summary>
        public bool NeedsVerification()
        {
            if (verifiedPass) return false;
            Unverified = Server.Config.verifyadmins && Rank >= Server.Config.VerifyAdminsRank;
            return Unverified;
        }

        /// <summary> Checks if player is currently unverified, and if so, sends a message informing them </summary>
        public void CheckIsUnverified()
        {
            if (NeedsVerification()) PassAuthenticator.Current.NeedVerification(this);
        }


        /// <summary> Formats a player name for displaying in chat. </summary>
        public string FormatNick(string name)
        {
            Player target = PlayerInfo.FindExact(name);
            // TODO: select color from database?
            if (target != null && CanSee(target)) return FormatNick(target);

            return Group.GroupIn(name).Color + Server.ToRawUsername(name);
        }

        /// <summary> Formats a player's name for displaying in chat. </summary>        
        public string FormatNick(Player target)
        {
            if (Ignores.Nicks) return target.color + target.truename;
            return target.color + target.DisplayName;
        }

        /// <summary> Blocks calling thread until all 'new map loaded' packets have been sent. </summary>
        public void BlockUntilLoad(int sleep)
        {
            while (Loading)
                Thread.Sleep(sleep);
        }

        /// <summary> Sends a block change packet to the user containing the current block at the given coordinates. </summary>
        /// <remarks> Vanilla client always assumes block place/delete succeeds, so this is usually used to echo back the
        /// old block. (e.g. insufficient permission to change that block, used as mark for draw operations) </remarks>
        public void RevertBlock(ushort x, ushort y, ushort z)
        {
            SendBlockchange(x, y, z, level.GetBlock(x, y, z));
        }

        public void SetMoney(int amount)
        {
            money = amount;
            OnMoneyChangedEvent.Call(this);
        }

        public static bool CheckVote(string msg, Player p, string a, string b, ref int totalVotes)
        {
            if (!(msg.CaselessEq(a) || msg.CaselessEq(b))) return false;

            if (p.voted && !Server.Config.MultipleVotes)
            {
                p.Message("&cYou have already voted!");
            }
            else
            {
                if (p.voted)
                {
                    totalVotes--;
                }
                totalVotes++;
                p.Message("&aThanks for voting!");
                p.voted = true;
            }
            return true;
        }

        public void CheckForMessageSpam()
        {
            spamChecker?.CheckChatSpam();
        }

        public void SetBaseTotalModified(long modified)
        {
            long adjust = modified - TotalModified;
            TotalModified = modified;
            // adjust so that SessionModified is unaffected
            startModified += adjust;
        }

        public string selTitle;
        public object selLock = new object();
        public Vec3S32[] selMarks;
        public object selState;
        public SelectionHandler selCallback;
        public SelectionMarkHandler selMarkCallback;
        public int selIndex;

        public void MakeSelection(int marks, string title, object state,
                                  SelectionHandler callback, SelectionMarkHandler markCallback = null)
        {
            lock (selLock)
            {
                selMarks = new Vec3S32[marks];
                selTitle = title;
                selState = state;
                selCallback = callback;
                selMarkCallback = markCallback;
                selIndex = 0;
                Blockchange = SelectionBlockChange;

                if (title != null) InitSelectionHUD();
                else ResetSelectionHUD();
            }
        }

        public void MakeSelection(int marks, object state, SelectionHandler callback)
        {
            MakeSelection(marks, null, state, callback);
        }

        public void ClearSelection()
        {
            lock (selLock)
            {
                if (selTitle != null) ResetSelectionHUD();
                selTitle = null;
                selState = null;
                selCallback = null;
                selMarkCallback = null;
                Blockchange = null;
            }
        }

        public void SelectionBlockChange(Player p, ushort x, ushort y, ushort z, ushort block)
        {
            lock (selLock)
            {
                Blockchange = SelectionBlockChange;
                RevertBlock(x, y, z);

                selMarks[selIndex] = new Vec3S32(x, y, z);
                selMarkCallback?.Invoke(p, selMarks, selIndex, selState, block);
                // Mark callback cancelled selection
                if (selCallback == null) return;

                selIndex++;
                if (selIndex == 1 && selTitle != null)
                {
                    SendCpeMessage(CpeMessageType.BottomRight2, "Mark #1" + FormatSelectionMark(selMarks[0]));
                }
                else if (selIndex == 2 && selTitle != null)
                {
                    SendCpeMessage(CpeMessageType.BottomRight1, "Mark #2" + FormatSelectionMark(selMarks[1]));
                }
                if (selIndex != selMarks.Length) return;

                string title = selTitle;
                object state = selState;
                SelectionMarkHandler markCallback = selMarkCallback;
                SelectionHandler callback = selCallback;
                ClearSelection();

                block = p.BlockBindings[block];
                bool canRepeat = callback(this, selMarks, state, block);

                if (canRepeat && staticOrders)
                {
                    MakeSelection(selIndex, title, state, callback, markCallback);
                }
            }
        }

        public string FormatSelectionMark(Vec3S32 P)
        {
            return ": &S(" + P.X + ", " + P.Y + ", " + P.Z + ")";
        }

        public void InitSelectionHUD()
        {
            SendCpeMessage(CpeMessageType.BottomRight3, selTitle);
            SendCpeMessage(CpeMessageType.BottomRight2, "Mark #1: &S(Not yet set)");
            string mark2Msg = selMarks.Length >= 2 ? "Mark #2: &S(Not yet set)" : "";
            SendCpeMessage(CpeMessageType.BottomRight1, mark2Msg);
        }

        public void ResetSelectionHUD()
        {
            SendCpeMessage(CpeMessageType.BottomRight3, "");
            SendCpeMessage(CpeMessageType.BottomRight2, "");
            SendCpeMessage(CpeMessageType.BottomRight1, "");
        }
    }
}