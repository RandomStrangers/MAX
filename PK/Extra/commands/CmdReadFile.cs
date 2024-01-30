using System;
using System.IO;
using Flames.Network;
namespace Flames.Commands
{
    public sealed class CmdReadFile : Command2 {
    public override string name { get { return "ReadFile"; } }
	public override string shortcut { get { return "read"; } }
	public override string type { get { return "information"; } }
	public override LevelPermission defaultRank { get { return LevelPermission.Flames; } }
	public override bool SuperUseable { get { return true; } }

		public override void Use(Player p, string message, CommandData data) {
        bool Empty = string.IsNullOrEmpty(message);
        if (Empty){
        	Help(p);
        return;
        }
			string file = message;
            if (!File.Exists(file)){
            p.Message("File " + message + " does not exist!");
            return;
            }
			string contents = File.ReadAllText(file);
					p.Message("Contents of  " + message + ":");
                    p.Message("");
                    p.Message(contents);
                    return;
        }
        	public override void Help(Player p) {
            p.Message("%T/ReadFile %H- Read a file.");
		}
                    }
                }