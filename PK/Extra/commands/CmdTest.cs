//	Auto-generated command skeleton class
//	Use this as a basis for custom Flames commands
//	Naming should be kept consistent (e.g. /update command should have a class name of 'CmdUpdate' and a filename of 'CmdUpdate.cs')
// As a note, Flames is designed for .NET 4.8
// To reference other assemblies, put a "//reference [assembly filename]" at the top of the file
//   e.g. to reference the System.Data assembly, put "//reference System.Data.dll"
// Add any other using statements you need after this
using System;
using Flames;
public class CmdTest : Command
{
	// The command's name (what you put after a slash to use this command)
	public override string name { get { return "Test"; } }
	// Command's shortcut, can be left blank (e.g. "/Copy" has a shortcut of "c")
	public override string shortcut { get { return ""; } }
	// Which submenu this command displays in under /Help
	public override string type { get { return "other"; } }
	// Whether or not this command can be used in a museum. Block/map altering commands should return false to avoid errors.
	public override bool museumUsable { get { return true; } }
	// The default rank required to use this command. Valid values are:
	//   LevelPermission.Guest, LevelPermission.Builder, LevelPermission.AdvBuilder,
	//   LevelPermission.Operator, LevelPermission.Admin, LevelPermission.Owner
	public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
	// This is for when a player executes this command by doing /Test
	//   p is the player object for the player executing the command. 
	//   message is the arguments given to the command. (e.g. for '/Test this', message is "this")
	public override void Use(Player p, string message)
	{
		p.Message("Hello World!");
	}
	// This is for when a player does /Help Test
	public override void Help(Player p)
	{
		p.Message("/Test - Does stuff. Example command.");
	}
}