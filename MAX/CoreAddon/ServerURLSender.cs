using System;
using System.IO;
using MAX.Tasks;

namespace MAX.Core
{
    public class ServerURLSender : Addon_Simple
    {
        public override string name { get { return "Say URL"; } }
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string MAX_Version {  get {  return Server.Version; } }
        public override void Load(bool startup)
        {
            bool SendURL = Server.Config.SendURL;
            bool IsPublic = Server.Config.Public;
            bool SayHi = Server.Config.SayHello;
            if (SendURL)
            {
                if (!IsPublic)
                {
                    Logger.Log(LogType.SystemActivity, "Server is not public! Cannot send URL to chat!");
                    return;
                }
                else
                {
                    Server.MainScheduler.QueueOnce(SayURL, null, TimeSpan.FromSeconds(12));
                }
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
        static void SayHello(SchedulerTask task)
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