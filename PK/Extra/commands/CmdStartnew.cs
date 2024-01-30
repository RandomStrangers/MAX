//reference System.dll
using System;
using System.Collections.Generic;
using System.IO;
using Flames.DB;
using Flames.SQL;
using System.Threading;
using System.Diagnostics;
namespace Flames.Commands.Misc {
    public sealed class CmdStartNew : Command
    {
        public override string name { get { return "ServerStart"; } }
		public override bool MessageBlockRestricted { get { return true; } }
        public override string shortcut { get { return "Start"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Flames; } }

        public override void Use(Player p, string message)
        {
            Repeat1(p);
        }
        static void Repeat1(Player p)
        {
            if (!CheckPerms1(p))
            {
                p.Message("Only the Flames or the Server Owner can use this command!"); return;
           }
           System.Diagnostics.Process.Start("FlamesCLI.exe");
        }
        static bool CheckPerms1(Player p)
        {
            if (p.IsFire) return true;
			if (p.IsSuper && !p.IsFire) return false;
            if (Server.Config.OwnerName.CaselessEq("Notch")) return false;
            return p.name.CaselessEq(Server.Config.OwnerName);
        }
        public override void Help(Player p)
        {
        		p.Message("");
        }
    }
}