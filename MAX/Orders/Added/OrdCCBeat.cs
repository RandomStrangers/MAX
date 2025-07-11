using MAX.Network;
using System;
using System.IO;
namespace MAX.Orders
{
    public class OrdCCHeartbeat : Order
    {
        public override string Name { get { return "ccheartbeat"; } }
        public override string Shortcut { get { return "ccbeat"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MuseumUsable { get { return true; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Owner; } }

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
                p.Message("Error with ClassiCube pump: " + e + ".");
                if (!p.IsMAX)
                {
                    Logger.Log(LogType.Error, "Error with ClassiCube pump.", e);
                }
            }
        }
        public override void Help(Player p)
        {
            p.Message("&T/CCHeartbeat &H- Forces a pump for the ClassiCube heartbeat.");
        }
    }
    public class OrdURL : Order
    {
        public override string Name { get { return "ServerURL"; } }
        public override string Shortcut { get { return "URL"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool SuperUseable { get { return true; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Banned; } }


        public override void Use(Player p, string message)
        {
            string file = "./text/externalurl.txt";
            string contents = File.ReadAllText(file);
            p.Message("Server URL: " + contents);
            return;
        }
        public override void Help(Player p)
        {
            p.Message("&T/ServerUrl &H- Shows the server's ClassiCube URL.");
        }
    }
}