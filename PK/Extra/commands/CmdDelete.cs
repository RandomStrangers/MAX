using Flames.Commands;
using Flames;
using System;
using System.IO;
using System.Threading;
namespace Flames.Commands.Misc
{
    public class CmdDeleteFile : Command
    {
        public override string name { get { return "DeleteFile"; } }
        public override string shortcut { get { return "df"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Flames; } }
        public override CommandAlias[] Aliases { get { return new[] { new CommandAlias("delete1") }; } }
        public override void Use(Player p, string message)
        {
        		bool messageEmpty = string.IsNullOrEmpty(message);
			if (!messageEmpty){
            if (File.Exists(message))
            {
                AtomicIO.TryDelete(message);
                p.Message(message + " deleted.");
                return;
            }
            if (!File.Exists(message))
            {
                p.Message(message + " not found!");
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
            p.Message("Delete a file.");
        }
    }
}
public class CmdDeleteDir : Command
{
    public override string name { get { return "DeleteDirectory"; } }
    public override string shortcut { get { return "dd"; } }
    public override string type { get { return CommandTypes.Other; } }
    public override LevelPermission defaultRank { get { return LevelPermission.Flames; } }
    public override CommandAlias[] Aliases { get { return new[] { new CommandAlias("deletedir") }; } }
    public override void Use(Player p, string message)
    {
            		bool messageEmpty = string.IsNullOrEmpty(message);
		if (!messageEmpty) {
        if (Directory.Exists(message)) {
            Delete.Delete1(message);
            p.Message(message + " directory deleted.");
            return;
        }
        if (!Directory.Exists(message))
        {
            p.Message(message + " directory not found!");
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
        p.Message("Delete a directory.");
    }
}
public static class Delete
{

    public static bool Delete1(string path)
    {
        try
        {
            Directory.Delete(path, false);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }
}