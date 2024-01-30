using Flames.Commands;
using Flames;
using System;
using System.IO;
using System.Threading;
namespace Flames.Commands.Misc
{
    public class CmdCreateFile : Command
    {
        public override string name { get { return "CreateFile"; } }
        public override string shortcut { get { return "cf"; } }
        public override string type { get { return CommandTypes.Other; } }
		public override CommandAlias[] Aliases { get { return new[] { new CommandAlias("create") }; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Flames; } }
        public override void Use(Player p, string message)
        {
        		bool messageEmpty = string.IsNullOrEmpty(message);
			if (!messageEmpty){
            if (File.Exists(message))
            {
                p.Message(message + " already exists!");
                return;
            }
            if (!File.Exists(message))
            {
            File.Create(message);
                p.Message(message + " created");
                return;
            }
            return;
        }
        else 
        {
        p.Message("File name required! :P");
        }
	}
        public override void Help(Player p)
        {
            p.Message("Create a file.");
        }
    }
public class CmdCreateDir : Command
{
    public override string name { get { return "CreateDirectory"; } }
    public override string shortcut { get { return "cdir"; } }
    public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Flames; } }
    public override CommandAlias[] Aliases { get { return new[] { new CommandAlias("createdir") }; } }
    public override void Use(Player p, string message)
    {
            		bool messageEmpty = string.IsNullOrEmpty(message);
		if (!messageEmpty) {
        if (Directory.Exists(message)) {
            p.Message(message + " directory already exists!.");
            return;
        }
        if (!Directory.Exists(message))
        {
        	Directory.CreateDirectory(message);
            p.Message(message + " directory created!");
            return;
        }
		else 
        {
        p.Message("Directory name required! :P");
        }
    }
    }
    public override void Help(Player p)
    {
        p.Message("Create a directory.");
    }
}
}