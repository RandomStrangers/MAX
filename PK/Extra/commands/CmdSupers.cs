using System;
using Flames.Util;
using System.Threading;
using Flames.DB;
namespace Flames.Commands { 
		public sealed class CmdSuper : Command2 {
        public override string name { get { return "Super"; } }
		public override bool MessageBlockRestricted { get { return true; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		public override string shortcut { get { return ""; } }
            public override bool UpdatesLastCmd { get { return false; } }
        public override void Use(Player p, string message, CommandData data) {
        p.IsSuper = true;
            p.Message("&cWarning: &5You can no longer be kicked and will remain connected until you use /UnSuper!");
        }
        public override void Help(Player p) 
        {
            if (p.IsFire){
			p.Message("Does nothing."); }
            else if (p.IsSuper){
			p.Message("Does nothing."); }
            else {
         string msg = PlayerDB.GetLogoutMessage(p.name);
         p.Leave(msg);
        }
        }
	}
		public sealed class CmdUnSuper : Command2 {
        public override string name { get { return "UnSuper"; } }
		public override bool MessageBlockRestricted { get { return true; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		public override string shortcut { get { return ""; } }
            public override bool UpdatesLastCmd { get { return false; } }
        public override void Use(Player p, string message, CommandData data) {
            p.Message("&cWarning: &5You will be disconnected upon closing the client now.");
        p.IsSuper = false;
        }
        public override void Help(Player p) 
        {
            if (p.IsFire){
			p.Message("Does nothing."); }
            else if (p.IsSuper){
			p.Message("Does nothing."); }
            else {
         string msg = PlayerDB.GetLogoutMessage(p.name);
         p.Leave(msg);
        }
        }
	}
}