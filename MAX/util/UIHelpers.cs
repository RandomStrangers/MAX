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
using System;
using System.Threading;

namespace MAX.UI 
{
    /// <summary> Common functionality for a TerminalLineInterface or GUI server console </summary>
    public static class UIHelpers 
    {
        public static string lastORD = "";
        public static void HandleChat(string text) {
            if (text != null) text = text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            
            Player p = Player.MAX;
            if (ChatModes.Handle(p, text)) return;
            
            Chat.MessageChat(ChatScope.Global, p, "λFULL: &f" + text, null, null, true);
        }
        
        public static void RepeatOrder() {
            if (lastORD.Length == 0) {
                Logger.Log(LogType.Debug, "(MAX): Cannot repeat order - no orders issued yet.");
                return;
            }
            Logger.Log(LogType.Debug, "Repeating &T/" + lastORD);
            HandleOrder(lastORD);
        }
        
        public static void HandleOrder(string text) {
            if (text != null) text = text.Trim();
            if (string.IsNullOrEmpty(text)) {
                Logger.Log(LogType.Debug, "(MAX): Whitespace orders are not allowed."); 
                return;
            }
            if (text[0] == '/' && text.Length > 1)
                text = text.Substring(1);
            
            lastORD = text;
            text.Separate(' ', out string name, out string args);

            Order.Search(ref name, ref args);
            if (Server.Check(name, args)) { Server.cancelorder = false; return; }
            Order ord = Order.Find(name);
            
            if (ord == null) { 
                Logger.Log(LogType.Debug, "(MAX): Unknown order \"{0}\"", name); return; 
            }
            if (!ord.SuperUseable) { 
                Logger.Log(LogType.Debug, "(MAX): /{0} can only be used in-game.", ord.name); return; 
            }

            Thread thread = new Thread(
                () =>
                {
                    try
                    {
                        ord.Use(Player.MAX, args);
                        if (args.Length == 0)
                        {
                            Logger.Log(LogType.Debug, "(MAX) used /" + ord.name);
                        }
                        else
                        {
                            Logger.Log(LogType.Debug, "(MAX) used /" + ord.name + " " + args);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                        Logger.Log(LogType.Debug, "(MAX): FAILED ORDER");
                    }
                })
            {
                Name = "MAXORD_" + name,
                IsBackground = false
            };
            thread.Start();
        }
        
        public static string Format(string message) {
            message = message.Replace("%S", "&f"); // We want %S to be treated specially when displayed in UI
            message = Colors.Escape(message);      // Need to Replace first, otherwise it's mapped by Colors.Escape
            return message;
        }
        
        public static string OutputPart(ref char nextCol, ref int start, string message) {
            int next = NextPart(start, message);
            string part;
            if (next == -1) {
                part = message.Substring(start);
                start = message.Length;
            } else {
                part = message.Substring(start, next - start);
                start = next + 2;
                nextCol = message[next + 1];
            }
            return part;
        }

        public static int NextPart(int start, string message) {
            for (int i = start; i < message.Length; i++) {
                if (message[i] != '&') continue;
                // No colour code follows this
                if (i == message.Length - 1) return -1;
                
                // Check following character is an actual colour code
                char col = Colors.Lookup(message[i + 1]);
                if (col != '\0') return i;
            }
            return -1;
        }
    }
}