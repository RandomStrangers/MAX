// This time I properly credited GoldenSparks
using System;
using System.Threading;
namespace Flames.Commands.Misc {
    public class CmdOX : Command
{
        public override string name { get { return "Oxidation"; } }
        public override string shortcut { get { return "ox"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
    public override void Use(Player p, string message)
    {
        bool messageEmpty = string.IsNullOrEmpty(message);
        Command.Find("say").Use(Player.Flame, "&9THIS CALLS FOR OXIDATION AND DREAM MONSTERS!!!");
        Command.Find("say").Use(Player.Flame, "&fhttps://www.youtube.com/watch?v=5vnHaVjP33M");
        Command.Find("say").Use(Player.Flame, "(from " + p.truename + ")");
        if (!messageEmpty) {
            Command.Find("say").Use(Player.Flame, "&7-------------------");
            Command.Find("say").Use(Player.Flame, "&7" + p.truename + " says: " + message);
        }
    }
    public override void Help(Player p)
    {
        p.Message("/Oxidation (message, optional) - &9THIS CALLS FOR A Oxidation and Dream Monsters | GHOST Reupload!!!");
        p.Message("&7-- cmd made by koy (Lexi) with the assistance of GoldenSparks --");
    }
}
}