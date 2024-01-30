using Flames;
namespace Core {
	public sealed class CmdOverSeerAdventure : Command2 {
		public override string name { get { return "OverSeerAdventure"; } }
        public override string shortcut { get { return "osad"; } }
		public override string type { get { return "World"; } }
		
		public override void Use(Player p, string message, CommandData data) {
		    Command.Find("OverSeer").Use(p, "Map buildable");
		    Command.Find("OverSeer").Use(p, "Map deletable");
        }
		
		public override void Help(Player p) {
            p.Message("%T/OverSeerAdventure %H- Toggles adventure mode for a realm.");
		}
	}
}