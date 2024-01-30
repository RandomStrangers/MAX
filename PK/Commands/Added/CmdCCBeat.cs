﻿using System;
using System.IO;
using PattyKaki.Network;
namespace PattyKaki.Commands
{
    public class CmdCCHeartbeat : Command
    {
        public override string name { get { return "ccheartbeat"; } }
        public override string shortcut { get { return "ccbeat"; } }
        public override string type { get { return "moderation"; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }

        public override void Use(Player p, string message)
        {
            try
            {
                Heartbeat.Heartbeats[0].Pump();
                p.Message("Heartbeat pump sent.");
                p.Message("Server URL: " + ((ClassiCubeBeat)Heartbeat.Heartbeats[0]).LastResponse);

            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error with ClassiCube pump.", e);
                p.Message("Error with ClassiCube pump: " + e + ".");
            }
        }
        public override void Help(Player p)
        {
            p.Message("/ccheartbeat - Forces a pump for the ClassiCube heartbeat.  DEBUG PURPOSES ONLY.");
        }
    }
    public sealed class CmdUrl : Command2
    {
        public override string name { get { return "ServerUrl"; } }
        public override string shortcut { get { return "url"; } }
        public override string type { get { return "information"; } }
        public override bool SuperUseable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }


        public override void Use(Player p, string message, CommandData data)
        {
                string file = "./text/externalurl.txt";
                string contents = File.ReadAllText(file);
                p.Message("Server URL: " + contents);
                return;
        }
        public override void Help(Player p)
        {
            p.Message("%T/ServerUrl %H- Shows the server's ClassiCube URL.");
        }
    }
}