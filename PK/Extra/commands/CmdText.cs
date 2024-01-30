//reference System.dll
using System;
using System.IO;
using System.Text.RegularExpressions;
namespace Flames.Commands
{
    public sealed class CmdText : Command
    {
        public override string name { get { return "Text"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Other; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Nobody; } }
        public override void Use(Player p, string message)
        {

            // Create the directory if it doesn't exist
            if (!Directory.Exists("extra/text/"))
                Directory.CreateDirectory("extra/text");

            // Show the help if the message doesn't contain enough parameters
            if (message.IndexOf(' ') == -1)
            {
                Help(p);
                return;
            }

            string[] param = message.Split(' ');

            try
            {
                if (param[0].ToLower() == "delete")
                {
                    string filename = SanitizeFileName(param[1]) + ".txt";
                    if (File.Exists("extra/text/" + filename))
                    {
                        File.Delete("extra/text/" + filename);
                        p.Message("Deleted file: " + filename);
                        return;
                    }
                    else
                    {
                        p.Message("Could not find file: " + filename);
                        return;
                    }
                }
                else
                {
                    string filename = SanitizeFileName(param[0]) + ".txt";
                    string path = "extra/text/" + filename;
                    message = message.Substring(message.IndexOf(' ') + 1);
                    string contents = message;
                    if (contents == "")
                    {
                        Help(p);
                        return;
                    }

                    if (!File.Exists(path))
                        contents = contents;
                    else
                        contents = " " + contents;

                    File.AppendAllText(path, contents);
                    p.Message("Added text to: " + filename);
                }
            }
            catch { Help(p); }
        }
        public override void Help(Player p)
        {
            p.Message("/text [file] [message] - Makes a /view-able text");
            p.Message("The [message] is entered into the text file");
            p.Message("If the file already exists, text will be added to the end");
        }

        private string SanitizeFileName(string filename)
        {
            return Regex.Replace(filename, @"[^\d\w\-]", "");
        }
    }
}