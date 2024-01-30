using System;
using Flames;
public class CmdMessage : Command
{
	public override string name { get { return "MessageMe"; } }
	public override string shortcut { get { return "msgme"; } }
	public override string type { get { return "other"; } }
	public override bool museumUsable { get { return true; } }
	public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
    public override void Use(Player p, string message)
	{
    bool MessageEmpty = string.IsNullOrEmpty(message);
    if (MessageEmpty) {
   p.Message("/msgme [message] - Sends a message to yourself."); 
   return; }
 	else {
 		p.Message(message);
 	}
 }
	public override void Help(Player p)
	{
   	p.Message("/msgme [message] - Sends a message to yourself."); 
	}
}