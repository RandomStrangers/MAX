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
using System;
using System.Net;
using System.Net.Sockets;

namespace MAX.Network
{
    /// <summary> Abstracts listening on network socket </summary>
    public class INetListen
    {
        /// <summary> The IP address this network socket is listening on </summary>
        public IPAddress IP;
        /// <summary> The port this network socket is listening on </summary>
        public int Port;
        /// <summary> Whether connections are currently being accepted </summary>
        public bool Listening;

        /// <summary> Begins listening for connections on the given IP and port </summary>
        /// <remarks> Client connections are asynchronously accepted </remarks>
        public virtual void Listen(IPAddress ip, int port)
        {
        }

        /// <summary> Closes this network listener </summary>
        public virtual void Close()
        {
        }
    }

    /// <summary> Abstracts listening on a TCP socket </summary>
    public class TcpListen : INetListen
    {
        public Socket socket;

        public void DisableIPV6OnlyListener()
        {
            if (socket.AddressFamily != AddressFamily.InterNetworkV6) return;
            // TODO: Make windows only?

            // NOTE: SocketOptionName.IPv6Only is not defined in Mono, but apparently
            //  macOS and Linux default to dual stack by default already
            const SocketOptionName ipv6Only = (SocketOptionName)27;
            try
            {
                socket.SetSocketOption(SocketOptionLevel.IPv6, ipv6Only, false);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to disable IPv6 only listener setting", ex);
            }
        }

        public void EnableAddressReuse()
        {
            // This fixes when on certain environments, if the server is restarted while there are still some
            // sockets in the TIME_WAIT state, the listener in the new server process will fail with EADDRINUSE
            //   https://stackoverflow.com/questions/3229860/what-is-the-meaning-of-so-reuseaddr-setsockopt-option-linux
            //   https://superuser.com/questions/173535/what-are-close-wait-and-time-wait-states
            //   https://stackoverflow.com/questions/14388706/how-do-so-reuseaddr-and-so-reuseport-differ
            // SO_REUSEADDR behaves differently on Windows though, so don't enable it there
            //  (note that this code is required for WINE, therefore just check if running in mono)
            //  (see WS_SO_REUSEADDR case handling in WS_setsockopt in WINE/dlls/ws2_32/socket.c)
            if (!Server.RunningOnMono()) return;

            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            }
            catch
            {
                // not really a critical issue if this fails to work
            }
        }

        public override void Listen(IPAddress ip, int port)
        {
            if (IP == ip && Port == port) return;
            Close();
            IP = ip; Port = port;

            try
            {
                socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                DisableIPV6OnlyListener();
                EnableAddressReuse();

                socket.Bind(new IPEndPoint(ip, port));
                socket.Listen((int)SocketOptionName.MaxConnections);
                AcceptNextAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Logger.Log(LogType.Warning, "Failed to start listening on port {0} ({1})", port, ex.Message);

                string msg = string.Format("Failed to start listening. Is another server or instance of {0} already running on port {1}?",
                                           Server.SoftwareName, port);
                Server.UpdateUrl(msg);
                socket = null; return;
            }
            Listening = true;
            Logger.Log(LogType.SystemActivity, "Started listening on port {0}... ", port);
        }

        public void AcceptNextAsync()
        {
            // retry, because if we don't call BeginAccept, no one can connect anymore
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    socket.BeginAccept(acceptCallback, this); return;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        public static AsyncCallback acceptCallback = new AsyncCallback(AcceptCallback);
        public static void AcceptCallback(IAsyncResult result)
        {
            if (Server.shuttingDown) return;
            TcpListen listen = (TcpListen)result.AsyncState;
            INetSocket s = null;

            try
            {
                Socket raw = listen.socket.EndAccept(result);
                bool cancel = false, announce = true;

                OnConnectionReceivedEvent.Call(raw, ref cancel, ref announce);
                if (cancel)
                {
                    // intentionally non-clean connection close
                    try { raw.Close(); } catch { }
                }
                else
                {
                    s = new TcpSocket(raw);

                    if (announce) Logger.Log(LogType.UserActivity, s.IP + " connected to the server.");
                    s.Init();
                }
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException)) Logger.LogError(ex);
                s?.Close();
            }
            listen.AcceptNextAsync();
        }

        public override void Close()
        {
            try
            {
                Listening = false;
                socket?.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}