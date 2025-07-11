/*
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
using MAX.Maths;
using System;


namespace MAX.Network
{
    /// <summary> Abstracts a network session with a client supporting a particular game protocol </summary>
    /// <remarks> By default only the Minecraft Classic game protocol is supported </remarks>
    /// <remarks> Generally, you should manipulate a session through wrapper methods in the Player class instead </remarks>
    public class IGameSession : INetProtocol
    {
        public byte ProtocolVersion;
        public byte[] fallback = new byte[256]; // fallback for classic+CPE block IDs
        public ushort MaxRawBlock = Block.CLASSIC_MAX_BLOCK;
        public bool hasCpe;
        public string appName;

        // these are checked very frequently, so avoid overhead of .Supports(
        public bool hasCustomBlocks, hasExtBlocks, hasBlockDefs, hasBulkBlockUpdate;
        public INetSocket socket;
        public Player player;
        /// <summary> Temporary unique ID for this network session </summary>
        public int ID;

        public PingList Ping = new PingList();

        public int ProcessReceived(byte[] buffer, int bufferLen)
        {
            int read = 0;
            try
            {
                while (read < bufferLen)
                {
                    int packetLen = HandlePacket(buffer, read, bufferLen - read);
                    // Partial packet received
                    if (packetLen == 0) break;

                    // Packet processed, onto next
                    read += packetLen;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return read;
        }

        public void Disconnect() { player.Disconnect(); }


        /// <summary> Sends raw data to the client </summary>
        public void Send(byte[] data) { socket.Send(data, SendFlags.None); }
        /// <summary> Whether the client supports the given CPE extension </summary>
        public virtual bool Supports(string extName, int version)
        {
            return false;
        }
        /// <summary> Attempts to process the next packet received from the client </summary>
        /// <returns> 0 if insufficient data left to fully process the next packet,
        /// otherwise returns the number of bytes processed </returns>
        public virtual int HandlePacket(byte[] buffer, int offset, int left)
        {
            return 0;
        }

        /// <summary> Sends a ping packet to the client </summary>
        public virtual void SendPing()
        {
        }
        public virtual void SendMotd(string motd)
        {
        }
        /// <summary> Sends chat to the client </summary>
        /// <remarks> Performs line wrapping if chat message is too long to fit in a single packet </remarks>
        public virtual void SendChat(string message)
        {
        }
        /// <summary> Sends a message packet to the client </summary>
        public virtual void SendMessage(CpeMessageType type, string message)
        {
        }
        /// <summary> Sends a kick/disconnect packet with the given reason </summary>
        public virtual void SendKick(string reason, bool sync)
        {
        }
        public virtual bool SendSetUserType(byte type)
        {
            return false;
        }
        /// <summary> Sends an entity teleport (absolute location update) packet to the client </summary>
        public virtual void SendTeleport(byte id, Position pos, Orientation rot)
        {
        }
        /// <summary> Sends an ext entity teleport with more control over behavior </summary>
        public virtual bool SendTeleport(byte id, Position pos, Orientation rot,
                                         Packet.TeleportMoveMode moveMode, bool usePos = true, bool interpolateOri = false, bool useOri = true)
        { return false; }
        /// <summary> Sends a spawn/add entity packet to the client </summary>
        public virtual void SendSpawnEntity(byte id, string name, string skin, Position pos, Orientation rot)
        {
        }
        /// <summary> Sends a despawn/remove entity to the client </summary>
        public virtual void SendRemoveEntity(byte id)
        {
        }
        public virtual void SendSetSpawnpoint(Position pos, Orientation rot)
        {
        }

        public virtual void SendAddTabEntry(byte id, string name, string nick, string group, byte groupRank)
        { 
        }
        public virtual void SendRemoveTabEntry(byte id) 
        { 
        }
        /// <summary> Sends a set reach/click distance packet to the client </summary>
        public virtual bool SendSetReach(float reach) 
        { 
            return false; 
        }
        /// <summary> Sends a set held block packet to the client </summary>
        public virtual bool SendHoldThis(ushort block, bool locked) 
        { 
            return false; 
        }
        /// <summary> Sends an update environment color packet to the client </summary>
        public virtual bool SendSetEnvColor(byte type, string hex) 
        { 
            return false; 
        }
        public virtual void SendChangeModel(byte id, string model) 
        { 
        }
        /// <summary> Sends an update weather packet </summary>
        public virtual bool SendSetWeather(byte weather) 
        { 
            return false; 
        }
        /// <summary> Sends an update text color code packet to the client </summary>
        public virtual bool SendSetTextColor(ColorDesc color) 
        {
            return false; 
        }
        /// <summary> Sends a define custom block packet to the client </summary>
        public virtual bool SendDefineBlock(BlockDefinition def) 
        { 
            return false; 
        }
        /// <summary> Sends an undefine custom block packet to the client </summary>
        public virtual bool SendUndefineBlock(BlockDefinition def) 
        { 
            return false; 
        }
        public virtual bool SendAddSelection(byte id, string label, Vec3U16 p1, Vec3U16 p2, ColorDesc color) 
        { 
            return false; 
        }
        public virtual bool SendRemoveSelection(byte id) 
        { 
            return false; 
        }

        /// <summary> Sends a level to the client </summary>
        public virtual void SendLevel(Level prev, Level level) 
        { 
        }
        /// <summary> Sends a block change/update packet to the client </summary>
        public virtual void SendBlockchange(ushort x, ushort y, ushort z, ushort block) 
        { 
        }

        public virtual byte[] MakeBulkBlockchange(BufferedBlockSender buffer)
        { 
            return new byte[0]; 
        }
        /// <summary> Gets the name of the software the client is using </summary>
        /// <example> ClassiCube, Classic 0.0.16, etc </example>
        public virtual string ClientName() 
        {
            return "Classic v0.28-v0.30"; 
        }
        public virtual void UpdatePlayerPositions()
        {
        }

        /// <summary> Converts the given block ID into a raw block ID that the client supports </summary>
        public virtual ushort ConvertBlock(ushort block)
        {
            ushort raw;
            Player p = player;

            if (block >= Block.Extended)
            {
                raw = Block.ToRaw(block);
            }
            else
            {
                raw = Block.Convert(block);
                // show invalid physics blocks as Orange
                if (raw >= Block.CPE_COUNT) raw = Block.Orange;
            }
            if (raw > MaxRawBlock) raw = p.level.GetFallback(block);

            // Check if a custom block replaced a core block
            //  If so, assume fallback is the better block to display
            if (!hasBlockDefs && raw < Block.CPE_COUNT)
            {
                BlockDefinition def = p.level.CustomBlockDefs[raw];
                if (def != null) raw = def.FallBack;
            }

            if (!hasCustomBlocks) raw = fallback[(byte)raw];
            return raw;
        }
    }
}