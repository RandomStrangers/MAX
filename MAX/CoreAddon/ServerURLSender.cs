using MAX.Tasks;
using System;
using System.IO;

namespace MAX.Core
{
    public class ServerURLSender : Addon
    {
        public override string Name { get { return "SayURL"; } }
        public override string Creator { get { return "HarmonyNetwork"; } }
        public override string MAX_Version { get { return Server.InternalVersion; } }
        public override void Load(bool startup)
        {
            bool SendURL = Server.Config.SendURL;
            bool SayHi = Server.Config.SayHello;
            if (SendURL)
            {
                Server.MainScheduler.QueueOnce(SayURL, null, TimeSpan.FromSeconds(12));
            }
            if (SayHi)
            {
                Server.Background.QueueOnce(SayHello, null, TimeSpan.FromSeconds(10));
            }
        }
        public void SayURL(SchedulerTask task)
        {
            string file = "./text/externalurl.txt";
            string contents = File.ReadAllText(file);
            string msg = "Server URL: " + contents;
            Order.Find("say").Use(Player.MAX, msg);
            Logger.Log(LogType.SystemActivity, "Server URL sent to chat!");
        }
        public static void SayHello(SchedulerTask task)
        {
            Order.Find("say").Use(Player.MAX, "Hello, World!");
            Logger.Log(LogType.SystemActivity, "Hello, World!");
        }
        public override void Unload(bool shutdown)
        {
        }
        public override void Help(Player p)
        {
            p.Message("");
        }
    }
}