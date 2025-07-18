﻿/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified for use with MCForge)
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
using System.Threading;

namespace MAX.Orders.Misc
{
    public class OrdRagequit : Order
    {
        public override string Name { get { return "RageQuit"; } }
        public override string Shortcut { get { return "rq"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            p.Leave("RAGEQUIT!!");
        }

        public override void Help(Player p)
        {
            p.Message("&T/RageQuit");
            p.Message("&HMakes you ragequit");
        }
    }

    public class OrdQuit : Order
    {
        public override string Name { get { return "Quit"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            string msg = message.Length > 0 ? "Left the game: " + message : "Left the game.";
            if (p.muted) msg = "Left the game.";
            p.Leave(msg);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Quit <reason>");
            p.Message("&HLeave the server.");
        }
    }

    public class OrdCrashServer : Order
    {
        public override string Name { get { return "CrashServer"; } }
        public override string Shortcut { get { return "Crash"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length > 0) { Help(p); return; }
            int code = new Random().Next(int.MinValue, int.MaxValue);

            p.Leave("Server crash! Error code 0x" + code.ToString("X8").TrimStart('0'));
        }

        public override void Help(Player p)
        {
            p.Message("&T/CrashServer");
            p.Message("&HCrash the server with a generic error");
        }
    }

    public class OrdHacks : Order
    {
        public override string Name { get { return "Hacks"; } }
        public override string Shortcut { get { return "Hax"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length > 0)
            {
                p.Message("&WIncorrect syntax. Abuse detected.");
                Thread.Sleep(3000);
            }

            const string msg = "Your IP has been backtraced + reported to FBI Cyber Crimes Unit.";
            p.Leave("kicked (" + msg + ")", msg, false);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Hacks");
            p.Message("&HPerforms various server hacks. OPERATORS ONLY!!!");
        }
    }
}