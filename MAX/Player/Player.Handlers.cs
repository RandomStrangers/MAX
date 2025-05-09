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
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using MAX.Authentication;
using MAX.Blocks;
using MAX.Blocks.Physics;
using MAX.Orders;
using MAX.Orders.Chatting;
using MAX.DB;
using MAX.Events.PlayerEvents;
using MAX.Games;
using MAX.Maths;
using MAX.Network;
using MAX.SQL;
using MAX.Util;
using BlockID = System.UInt16;

namespace MAX
{
    public partial class Player : IDisposable
    {
        public const string mustAgreeMsg = "You must read /rules then agree to them with /agree!";

        public object blockchangeLock = new object();
        public bool HasBlockChange() { return Blockchange != null; }

        public bool DoBlockchangeCallback(ushort x, ushort y, ushort z, BlockID block) {
            lock (blockchangeLock) {
                lastClick.X = x; lastClick.Y = y; lastClick.Z = z;
                if (Blockchange == null) return false;
                
                Blockchange(this, x, y, z, block);
                return true;
            }
        }

        public void HandleManualChange(ushort x, ushort y, ushort z, bool placing,
                                       BlockID block, bool checkPlaceDist)
        {
            BlockID old = level.GetBlock(x, y, z);
            if (old == Block.Invalid) return;

            if (jailed || possessed) { RevertBlock(x, y, z); return; }
            if (!agreed)
            {
                Message(mustAgreeMsg);
                RevertBlock(x, y, z); return;
            }

            if (level.IsMuseum && Blockchange == null) return;
            bool deletingBlock = !painting && !placing;

            if (Unverified)
            {
                PassAuthenticator.Current.RequiresVerification(this, "modify blocks");
                RevertBlock(x, y, z); return;
            }

            IGame game = IGame.GameOn(level);
            if (game != null && game.HandlesBlockchange(this, x, y, z)) return;

            if (ClickToMark && DoBlockchangeCallback(x, y, z, block)) return;

            bool cancel = false;
            OnBlockChangingEvent.Call(this, x, y, z, block, placing, ref cancel);
            if (cancel) return;

            if (old >= Block.Air_Flood && old <= Block.Door_Air_air)
            {
                Message("Block is active, you cannot disturb it.");
                RevertBlock(x, y, z); return;
            }

            if (!deletingBlock)
            {
                PhysicsArgs args = level.foundInfo(x, y, z);
                if (args.HasWait) return;
            }

            if (Rank == LevelPermission.Banned) return;
            if (checkPlaceDist)
            {
                int dx = Pos.BlockX - x, dy = Pos.BlockY - y, dz = Pos.BlockZ - z;
                int diff = (int)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (diff > ReachDistance + 4)
                {
                    Logger.Log(LogType.Warning, "{0} attempted to Build with a {1} distance offset", name, diff);
                    Message("You can't Build that far away.");
                    RevertBlock(x, y, z); return;
                }
            }

            if (!CheckManualChange(old, deletingBlock))
            {
                RevertBlock(x, y, z); return;
            }

            BlockID raw = placing ? block : Block.Air;
            block = BlockBindings[block];
            if (ModeBlock != Block.Invalid) block = ModeBlock;

            BlockID newB = deletingBlock ? Block.Air : block;
            ChangeResult result;

            if (old == newB)
            {
                // Ignores updating blocks that are the same and revert block back only to the player
                result = ChangeResult.Unchanged;
            }
            else if (deletingBlock)
            {
                result = DeleteBlock(old, x, y, z);
            }
            else if (!OrderParser.IsBlockAllowed(this, "place", block))
            {
                // Not allowed to place new block
                result = ChangeResult.Unchanged;
            }
            else
            {
                result = PlaceBlock(old, x, y, z, block);
            }

            if (result != ChangeResult.Modified)
            {
                // Client always assumes that the place/delete succeeds
                // So if actually didn't, need to revert to the actual block
                if (!Block.VisuallyEquals(raw, old)) RevertBlock(x, y, z);
            }
            OnBlockChangedEvent.Call(this, x, y, z, result);
        }

        public bool CheckManualChange(BlockID old, bool deleteMode)
        {
            if (!group.Blocks[old] && !level.BuildIn(old) && !Block.AllowBreak(old))
            {
                string action = deleteMode ? "delete" : "replace";
                BlockPerms.Find(old).MessageCannotUse(this, action);
                return false;
            }
            return true;
        }

        public ChangeResult DeleteBlock(BlockID old, ushort x, ushort y, ushort z) {
            if (deleteMode) return ChangeBlock(x, y, z, Block.Air);

            HandleDelete handler = level.DeleteHandlers[old];
            if (handler != null) return handler(this, old, x, y, z);
            return ChangeBlock(x, y, z, Block.Air);
        }

        public ChangeResult PlaceBlock(BlockID old, ushort x, ushort y, ushort z, BlockID block) {
            HandlePlace handler = level.PlaceHandlers[block];
            if (handler != null) return handler(this, block, x, y, z);
            return ChangeBlock(x, y, z, block);
        }
        
        /// <summary> Updates the block at the given position, mainly intended for manual changes by the player. </summary>
        /// <remarks> Adds to the BlockDB. Also turns block below to grass/dirt depending on light. </remarks>
        /// <returns> Return code from DoBlockchange </returns>
        public ChangeResult ChangeBlock(ushort x, ushort y, ushort z, BlockID block) {
            BlockID old = level.GetBlock(x, y, z);
            ChangeResult result = level.TryChangeBlock(this, x, y, z, block);
            
            if (result == ChangeResult.Unchanged) return result;
            if (result == ChangeResult.Modified)  level.BroadcastChange(x, y, z, block);
            
            ushort flags = BlockDBFlags.ManualPlace;
            if (painting && CollideType.IsSolid(level.CollideType(old))) {
                flags = BlockDBFlags.Painted;
            }
            
            level.BlockDB.Cache.Add(this, x, y, z, flags, old, block);
            y--; // check for growth at block below
            
            bool grow = level.Config.GrassGrow && (level.physics == 0 || level.physics == 5);
            if (!grow || level.CanAffect(this, x, y, z) != null) return result;
            BlockID below = level.GetBlock(x, y, z);
            
            BlockID grass = level.Props[below].GrassBlock;
            if (grass != Block.Invalid && block == Block.Air) {
                level.Blockchange(this, x, y, z, grass);
            }
            
            BlockID dirt = level.Props[below].DirtBlock;
            if (dirt != Block.Invalid && !level.LightPasses(block)) {
                level.Blockchange(this, x, y, z, dirt);
            }
            return result;
        }

        public void ProcessBlockchange(ushort x, ushort y, ushort z, byte action, BlockID held) {
            try {
                if (spamChecker.CheckBlockSpam()) return;

                LastAction = DateTime.UtcNow;
                if (IsAfk) OrdAfk.ToggleAfk(this, "");
                ClientHeldBlock = held;
                
                if ((action == 0 || held == Block.Air) && !level.Config.Deletable) {
                    // otherwise if you're holding air and try to place a block, this message would show
                    if (!level.IsAirAt(x, y, z)) Message("Deleting blocks is disabled in this level.");
                    
                    RevertBlock(x, y, z); return;
                } else if (action == 1 && !level.Config.Buildable) {
                    Message("Placing blocks is disabled in this level.");
                    RevertBlock(x, y, z); return;
                }
                
                if (held >= Block.Extended) {
                    if (!Session.hasBlockDefs || level.CustomBlockDefs[held] == null) {
                        Message("Invalid block type: " + Block.ToRaw(held));
                        RevertBlock(x, y, z); return;
                    }
                }
                HandleManualChange(x, y, z, action != 0, held, true);
            } catch (Exception e) {
                // Don't ya just love it when the server tattles?
                Chat.MessageOps(DisplayName + " has triggered a block change error");
                Chat.MessageOps(e.GetType().ToString() + ": " + e.Message);
                Logger.LogError(e);
            }
        }
        
        public void ProcessMovement(int x, int y, int z, byte yaw, byte pitch, int held) {
            if (held >= 0) ClientHeldBlock = (BlockID)held;

            if (trainGrab || following.Length > 0) { CheckBlocks(Pos, Pos); return; }
            Position next = new Position(x, y, z);
            CheckBlocks(Pos, next);

            bool cancel = false;
            OnPlayerMoveEvent.Call(this, next, yaw, pitch, ref cancel);
            if (cancel) { cancel = false; return; }
            
            Pos = next;
            SetYawPitch(yaw, pitch);
            CheckZones(next);
            
            if (!Moved() || Loading) return;
            if (DateTime.UtcNow < AFKCooldown) return;
            
            LastAction = DateTime.UtcNow;
            if (IsAfk) OrdAfk.ToggleAfk(this, "");
        }

        public void CheckZones(Position pos) {
            Vec3S32 P = pos.BlockCoords;
            Zone zone = ZoneIn;
            
            // player hasn't moved from current zone
            if (zone != null && zone.Contains(P.X, P.Y, P.Z)) return;
            Zone[] zones = level.Zones.Items;
            if (zones.Length == 0) return;
            
            for (int i = 0; i < zones.Length; i++) {
                if (!zones[i].Contains(P.X, P.Y, P.Z)) continue;
                
                ZoneIn = zones[i];
                OnChangedZoneEvent.Call(this);
                return;
            }
            
            ZoneIn = null;
            if (zone != null) OnChangedZoneEvent.Call(this);
        }

        public int CurrentEnvProp(EnvProp i, Zone zone) {
            int value    = Server.Config.GetEnvProp(i);
            bool block   = i == EnvProp.SidesBlock || i == EnvProp.EdgeBlock;
            int default_ = block ? Block.Invalid : EnvConfig.ENV_USE_DEFAULT;
            
            if (level.Config.GetEnvProp(i) != default_) {
                value = level.Config.GetEnvProp(i);
            }
            if (zone != null && zone.Config.GetEnvProp(i) != default_) {
                value = zone.Config.GetEnvProp(i);
            }
            
            if (value == default_) value = EnvConfig.DefaultEnvProp(i, level.Height);
            if (block)             value = Session.ConvertBlock((BlockID)value);
            return value;
        }
        
        public void SendCurrentEnv() {
            Zone zone = ZoneIn;
            
            for (int i = 0; i <= 5; i++) {
                string col = Server.Config.GetColor(i);
                if (level.Config.GetColor(i) != "") {
                    col = level.Config.GetColor(i);
                }
                if (zone != null && zone.Config.GetColor(i) != "") {
                    col = zone.Config.GetColor(i);
                }
                Session.SendSetEnvColor((byte)i, col);
            }
            
            if (Supports(CpeExt.EnvMapAspect) || Supports(CpeExt.EnvMapAspect, 2)) {
                for (EnvProp i = 0; i < EnvProp.Max; i++) {
                    int value = CurrentEnvProp(i, zone);
                    Send(Packet.EnvMapProperty(i, value));
                }
            }
            
            int weather = CurrentEnvProp(EnvProp.Weather, zone);
            Session.SendSetWeather((byte)weather);
        }

        public void CheckBlocks(Position prev, Position next) {
            try {
                Vec3U16 P = (Vec3U16)prev.BlockCoords;
                AABB bb = ModelBB.OffsetPosition(next);
                int index = level.PosToInt(P.X, P.Y, P.Z);
                
                if (level.Config.SurvivalDeath) {
                    bool movingDown = next.Y < prev.Y;
                    PlayerPhysics.Drown(this, bb);
                    PlayerPhysics.Fall(this,  bb, movingDown);
                }
                lastFallY = bb.Min.Y;
                
                PlayerPhysics.Walkthrough(this, bb);
                oldIndex = index;
            } catch (Exception ex) {
                Logger.LogError(ex);
            }
        }

        public bool Moved() { return _lastRot.RotY != Rot.RotY || _lastRot.HeadX != Rot.HeadX; }
        
        public void AnnounceDeath(string msg) {
            //Chat.MessageFrom(ChatScope.Level, this, msg.Replace("@p", "λNICK"), level, Chat.FilterVisible(this));
            if (hidden) {
                // Don't show usual death message to avoid confusion about whether others see your death
                Message(msg.Replace("@p", "You").Replace("was", "were"));
            } else {
                Chat.MessageFromLevel(this, msg.Replace("@p", "λNICK"));
            }
        }
        
        public bool HandleDeath(BlockID block, string customMsg = "", bool explode = false, bool immediate = false) {
            if (!immediate && DateTime.UtcNow < deathCooldown) return false;
            if (invincible) return false;
            
            bool cancel = false;
            OnPlayerDyingEvent.Call(this, block, ref cancel);
            if (cancel) { cancel = false; return false; }

            onTrain = false; trainInvincible = false; trainGrab = false;
            ushort x = (ushort)Pos.BlockX, y = (ushort)Pos.BlockY, z = (ushort)Pos.BlockZ;
            
            string deathMsg = level.Props[block].DeathMessage;
            if (deathMsg != null) AnnounceDeath(deathMsg);
            
            if (block == Block.RocketHead) level.MakeExplosion(x, y, z, 0);
            if (block == Block.Creeper) level.MakeExplosion(x, y, z, 1);
            
            if (block == Block.Stone || block == Block.Cobblestone) {
                if (explode) level.MakeExplosion(x, y, z, 1);
                if (block == Block.Stone) {
                    Chat.MessageFrom(this, customMsg.Replace("@p", "λNICK"));
                } else {
                    AnnounceDeath(customMsg);
                }
            }
            
            TimeSpan cooldown = Server.Config.DeathCooldown;
            OnPlayerDiedEvent.Call(this, block, ref cooldown);
            PlayerActions.Respawn(this);
            
            TimesDied++;
            // NOTE: If deaths column is ever increased past 16 bits, remove this clamp
            if (TimesDied > short.MaxValue && Database.Backend.EnforcesIntegerLimits)
                TimesDied = short.MaxValue;

            if (Server.Config.AnnounceDeathCount && (TimesDied > 0 && TimesDied % 10 == 0)) {
                AnnounceDeath("@p &Shas died &3" + TimesDied + " times");
            }
            deathCooldown = DateTime.UtcNow.Add(cooldown);
            return true;
        }
        
        public void ProcessChat(string text, bool continued) {
            LastAction = DateTime.UtcNow;
            if (FilterChat(ref text, continued)) return;

            if (text != "/afk" && IsAfk)
                OrdAfk.ToggleAfk(this, "");
            
            bool isOrder;
            text = Chat.ParseInput(text, out isOrder);
            if (isOrder) { DoOrder(text); return; }

            // People who are muted can't speak or vote
            if (muted) { Message("You are muted."); return; } //Muted: Only allow orders

            if (Server.voting) {
                if (CheckVote(text, this, "y", "yes", ref Server.YesVotes) ||
                    CheckVote(text, this, "n", "no", ref Server.NoVotes)) return;
            }

            IGame game = IGame.GameOn(level);
            if (game != null && game.HandlesChatMessage(this, text)) return;
            
            // Put this after vote collection so that people can vote even when chat is moderated
            if (!CheckCanSpeak("speak")) return;

            if (ChatModes.Handle(this, text)) return;
            text = HandleJoker(text);

            OnPlayerChatEvent.Call(this, text);
            if (cancelchat) { cancelchat = false; return; }
            Chat.MessageChat(this, "λFULL: &f" + text, null, true);
        }

        public bool FilterChat(ref string text, bool continued) {
            // Handle /womid [version] which informs the server of the WoM client version
            if (text.StartsWith("/womid")) {
                UsingWom = true;
                return true;
            }
            
            if (continued) {
                if (text.Length < NetUtils.StringSize) text += " ";
                partialMessage += text;
                
                LimitPartialMessage();
                return true;
            }

            if (text.CaselessContains("^detail.user=")) {
                Message("&WYou cannot use WoM detail strings in a chat message.");
                return true;
            }

            if (IsPartialSpaced(text)) {
                AppendPartialMessage(text.Substring(0, text.Length - 2) + " ");
                return true;
            } else if (IsPartialJoined(text)) {
                AppendPartialMessage(text.Substring(0, text.Length - 2));
                return true;
            } else if (partialMessage.Length > 0) {
                text = partialMessage + text;
                partialMessage = "";
            }

            text = Regex.Replace(text, "  +", " ");
            return text.Length == 0;
        }

        public static bool IsPartialSpaced(string text) {
            return text.EndsWith(" >") || text.EndsWith(" /");
        }

        public static bool IsPartialJoined(string text) {
            return text.EndsWith(" <") || text.EndsWith(" \\");
        }

        public void LimitPartialMessage() {
            if (partialMessage.Length < 100 * 64) return;
            partialMessage = "";
            Message("&WPartial message cleared due to exceeding 100 lines");
        }

        public void AppendPartialMessage(string part) {
            if (!partialLog.AddSpamEntry(20, TimeSpan.FromSeconds(1))) {
                Message("&WTried to add over 20 partial message in one second, slow down");
                return;
            }

            partialMessage += part;
            SendRawMessage("&3Partial message: &f" + partialMessage);
            LimitPartialMessage();
        }


        public void DoOrder(string text) {
            // Typing / repeats last order executed
            if (text.Length == 0) {
                text = lastORD;
                if (text.Length == 0) {
                    Message("Cannot repeat order - no orders used yet."); return;
                }
                Message("Repeating &T/" + text);
            }
            
            string ord, args;
            text.Separate(' ', out ord, out args);
            HandleOrder(ord, args, DefaultOrdData);
        }

        public string HandleJoker(string text) {
            if (!joker) return text;
            Logger.Log(LogType.PlayerChat, "<JOKER>: {0}: {1}", name, text);
            Chat.MessageFromOps(this, "&S<&aJ&bO&cK&5E&9R&S>: λNICK:&f " + text);

            TextFile jokerFile = TextFile.Files["Joker"];
            jokerFile.EnsureExists();
            
            string[] lines = jokerFile.GetText();
            Random rnd = new Random();
            return lines.Length > 0 ? lines[rnd.Next(lines.Length)] : text;
        }
        
        public void HandleOrder(string ord, string args, OrderData data) {
            ord = ord.ToLower();
            if (!Server.Config.OrdSpamCheck && !CheckMBRecursion(data)) return;
            
            try {
                Order order = GetOrder(ref ord, ref args, data);
                if (order == null) return;
                
                bool parallel = order.Parallelism == OrderParallelism.Yes
                                    || data.Context == OrderContext.MessageBlock;
                if (!parallel && !EnqueueSerialOrder(order, args, data)) return;
                
                ThreadStart callback;
                if (parallel) {
                    callback = () => UseOrder(order, args, data);
                } else {
                    callback = ExecuteSerialOrders;
                }
                
                Thread thread = new Thread(callback);
                try { thread.Name = "CMD_" + ord; } catch { }
                thread.IsBackground = true;
                thread.Start();
            } catch (Exception e) {
                Logger.LogError(e);
                Message("&WOrder failed");
            }
        }
        
        public void HandleOrders(List<string> ords, OrderData data) {
            List<string> messages = new List<string>(ords.Count);
            List<Order> orders = new List<Order>(ords.Count);
            if (!Server.Config.OrdSpamCheck && !CheckMBRecursion(data)) return;
            
            try {
                foreach (string raw in ords) {
                    string[] parts = raw.SplitSpaces(2);
                    string ord = parts[0].ToLower();
                    string args = parts.Length > 1 ? parts[1] : "";
                    
                    Order order = GetOrder(ref ord, ref args, data);
                    if (order == null) return;
                    
                    messages.Add(args); orders.Add(order);
                }

                Thread thread = new Thread(() => UseOrders(orders, messages, data));
                thread.Name = "CMDS_";
                thread.IsBackground = true;
                thread.Start();
            } catch (Exception e) {
                Logger.LogError(e);
                Message("&WOrder failed.");
            }
        }

        public bool CheckMBRecursion(OrderData data) {
            if (data.Context == OrderContext.MessageBlock) {
                mbRecursion++;
                // failsafe for when server has turned off order spam checking
                if (mbRecursion >= 100) {
                    mbRecursion = 0;
                    Message("&WInfinite message block loop detected, aborting");
                    return false;
                }
            } else if (data.Context == OrderContext.Normal) {
                mbRecursion = 0;
            }
            return true;
        }

        public bool CheckOrder(string ord) {
            if (ord.Length == 0) { Message("No order entered."); return false; }
            if (Server.Config.AgreeToRulesOnEntry && !agreed && !(ord == "agree" || ord == "rules" || ord == "disagree" || ord == "pass" || ord == "setpass")) {
                Message(mustAgreeMsg); return false;
            }
            if (jailed) {
                Message("You cannot use any orders while jailed."); return false;
            }
            if (Unverified && !(ord == "pass" || ord == "setpass")) {
                PassAuthenticator.Current.RequiresVerification(this, "use /" + ord);
                return false;
            }
            
            TimeSpan delta = ordUnblocked - DateTime.UtcNow;
            if (delta.TotalSeconds > 0) {
                int secs = (int)Math.Ceiling(delta.TotalSeconds);
                Message("Blocked from using orders for another " + secs + " seconds"); return false;
            }
            return true;
        }

        public Order GetOrder(ref string ordName, ref string ordArgs, OrderData data) {
            if (!CheckOrder(ordName)) return null;
            
            string bound;
            byte bindIndex;
            if (OrdBindings.TryGetValue(ordName, out bound)) {
                // user defined order shortcuts take priority
                bound.Separate(' ', out ordName, out ordArgs);
            } else if (byte.TryParse(ordName, out bindIndex) && bindIndex < 10) {
                // backwards compatibility for old /ordbind behaviour
                Message("No order is bound to: &T/" + ordName);
                return null;
            }
            
            Order.Search(ref ordName, ref ordArgs);
            OnPlayerOrderEvent.Call(this, ordName, ordArgs, data);
            if (cancelorder) { cancelorder = false; return null; }
            
            Order order = Order.Find(ordName);
            if (order == null) {
                if (Block.Parse(this, ordName) != Block.Invalid) {
                    ordArgs = ordName; ordName = "mode";
                    order = Order.Find("Mode");
                } else {
                    Logger.Log(LogType.OrderUsage, "{0} tried to use unknown order: /{1} {2}", name, ordName, ordArgs);
                    Message("Unknown order \"{0}\".", ordName); return null;
                }
            }

            if (!CanUse(order)) {
                order.Permissions.MessageCannotUse(this);
                return null;
            }
            
            if (level != null && level.IsMuseum && !order.museumUsable) {
                Message("Cannot use &T/{0} &Swhile in a museum.", order.name); return null;
            }
            if (jailed && !order.UseableWhenFrozen) {
                Message("Cannot use &T/{0} &Swhile jailed.", order.name); return null;
            }
            return order;
        }

        public bool UseOrder(Order order, string args, OrderData data) {
            string ord = order.name;
            if (order.UpdatesLastOrd) {
                lastORD = args.Length == 0 ? ord : ord + " " + args;
                lastOrdTime = DateTime.UtcNow;
            }
            if (order.LogUsage) Logger.Log(LogType.OrderUsage, "{0} used /{1} {2}", name, ord, args);
            
            try { //opstats patch (since MCForge 5.5.11)
                if (Server.Opstats.CaselessContains(ord) || (ord.CaselessEq("review") && args.CaselessEq("next") && Server.reviewlist.Count > 0)) {
                    Database.AddRow("Opstats", "Time, Name, Ord, Ordmsg",
                                    DateTime.Now.ToString(Database.DateFormat), name, ord, args);
                }
            } catch { }
            
            try {
                order.Use(this, args, data);
            } catch (Exception e) {
                Logger.LogError(e);
                Message("&WAn error occured when using the order!");
                Message(e.GetType() + ": " + e.Message);
                return false;
            }
            if (spamChecker != null && spamChecker.CheckOrderSpam()) return false;
            return true;
        }

        public bool UseOrders(List<Order> orders, List<string> messages, OrderData data) {
            for (int i = 0; i < messages.Count; i++) {
                if (!UseOrder(orders[i], messages[i], data)) return false;
                
                // No point running orders after disconnected
                if (leftServer) return false;
            }
            return true;
        }


        public bool EnqueueSerialOrder(Order ord, string args, OrderData data) {
            SerialOrder head = default;
            SerialOrder sord;
            
            sord.ord  = ord;
            sord.args = args;
            sord.data = data;
            
            lock (serialOrdsLock) {
                if (serialOrds.Count > 0)
                    head = serialOrds.Peek();
                
                serialOrds.Enqueue(sord);
            }
            if (head.ord == null) return true;
            
            if (ord.Parallelism == OrderParallelism.NoAndWarn) {
                Message("Waiting for &T/{0} {1} &Sto finish first before running &T/{2} {3}",
                        head.ord.name, head.args, ord.name, args);
            }
            
            // Overly punish triggering forced serial execution of orders
            spamChecker.CheckOrderSpam();
            return false;
        }

        public void ExecuteSerialOrders() {
            for (;;) 
            {
                SerialOrder sord;
                
                lock (serialOrdsLock) {
                    if (serialOrds.Count == 0) return;
                    sord = serialOrds.Peek();
                }
                UseOrder(sord.ord, sord.args, sord.data);
                
                // only dequeue AFTER finished (for long running orders)
                lock (serialOrdsLock) {
                    if (serialOrds.Count == 0) return;
                    serialOrds.Dequeue();
                }
            }
        }

        public void ClearSerialOrders() {
            lock (serialOrdsLock) { serialOrds.Clear(); }
        }
    }
}