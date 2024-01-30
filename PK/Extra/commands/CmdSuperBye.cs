namespace Flames.Commands.Misc
{
    public class CmdSuperQuit : Command
    {
        public override string name { get { return "SuperBye"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Nobody; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool UseableWhenFrozen { get { return true; } }
        public override void Use(Player p, string message)
        {
			p.Leave(message);
        }
        public override void Help(Player p)
        {
			p.Leave("");
        }
    }
}