//reference System.dll
using System;
using System.IO;
using System.Text.RegularExpressions;
namespace Flames.Commands
{
    public sealed class CmdAllText : Command
    {
        public override string name { get { return "AllText"; } }
        public override string shortcut { get { return "Rant"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public override void Use(Player p, string message)
        {
		bool Empty = string.IsNullOrEmpty(message);
            if (!Directory.Exists("Rant/"))
                Directory.CreateDirectory("Rant");
            if (Empty)
            {
                Help(p);
                return;
            }
            try
            {
                    string filename = "Rant.txt";
                    string path = "Rant/" + filename;
                    string contents = message;
                    if (contents == "")
                    {
                        Help(p);
                        return;
                    }
                    if (!File.Exists(path))
                        contents = contents;
                    else
                        contents = System.Environment.NewLine + contents;

                    File.AppendAllText(path, contents);
                    p.Message("Added text to: " + filename);
            }
            catch { Help(p); }
        }
        public override void Help(Player p)
        {
            p.Message("/AllText [message] - Makes a file viewable by /vr");
            p.Message("The [message] is entered into the text file");
            p.Message("If the file already exists, text will be added to the end");
        }

        private string SanitizeFileName(string filename)
        {
            return Regex.Replace(filename, @"[^\d\w\-]", "");
        }
    }
    public sealed class CmdViewRant : Command2 
    {
        public override string name { get { return "ViewRant"; } }
        public override string type { get { return CommandTypes.Other; } }
		public override string shortcut { get { return "VR"; } }
        public override bool UseableWhenFrozen { get { return true; } }
        public override void Use(Player p, string message, CommandData data) {
            if (!Directory.Exists("Rant/")) 
                Directory.CreateDirectory("Rant");
                if (File.Exists("Rant/Rant.txt")) {
                    string[] lines = File.ReadAllLines("Rant/Rant.txt");
                    p.Message("Contents of the rant file:");
                    p.MessageLines(lines);
                } else {
                    p.Message("Rant file doesn't exist! Create it using /Rant!");
                }
            }
        public override void Help(Player p) {
            p.Message("&T/ViewRant &H- Views the contents of the /rant file");
        }
    }
}