using System;
using Flames.Util;
using System.Threading;
namespace Flames.Commands { 
		public sealed class CmdSendCmd2 : Command2 {
        public override string name { get { return "SendCmd2"; } }
		public override bool MessageBlockRestricted { get { return true; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		public override string shortcut { get { return "sc"; } }
        public override void Use(Player p, string message, CommandData data) {
    	Find("bye").Use(Player.Flame, "disconnected");}
        public override void Help(Player p) 
        {        
		p.cancelcommand = true;		
        }
	}
}