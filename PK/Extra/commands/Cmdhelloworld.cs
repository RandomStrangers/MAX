using System;
using Flames;
public class CmdHelloworld : Command
{
	public override string name { get { return "HelloWorld"; } }
	public override string shortcut { get { return "hw"; } }
	public override string type { get { return "other"; } }
	public override bool museumUsable { get { return true; } }
	public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
	public override void Use(Player p, string message)
	{
		p.Message("Hello World!");
	}

	public override void Help(Player p)
	{
		p.Message("/Helloworldtest - Does stuff. Example command.");
	}
}