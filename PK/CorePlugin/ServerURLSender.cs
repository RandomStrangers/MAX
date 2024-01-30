﻿using System;
using System.IO;
using PattyKaki.Tasks;

namespace PattyKaki.Core
{
    public class ServerURLSender : Plugin
    {
        public override string name { get { return "Say URL"; } }
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override void Load(bool startup)
        {
            bool IsPublic = Server.Config.Public;
            if (IsPublic)
            {
                {
                    Server.MainScheduler.QueueOnce(SayURL, null, TimeSpan.FromSeconds(12));
                }
            }
            else
            {
                Logger.Log(LogType.SystemActivity, "Server is not public! Cannot send URL to chat!");
                return;
            }
        }

        public void SayURL(SchedulerTask task)
        {
            string file = "./text/externalurl.txt";
            string contents = File.ReadAllText(file);
            string msg = "Server URL: " + contents;
            Command.Find("say").Use(Player.PK, msg);
            Logger.Log(LogType.SystemActivity, "Server URL sent to chat!");
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