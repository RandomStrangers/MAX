using System;
using Flames.Tasks;

namespace Flames
{
	public class NoAFK : Plugin
	{
		public override string name { get { return "Plugin_Simple"; } }
		public override string creator { get { return "Random Strangers"; } }
		public override void Load(bool startup) {
	 Server.MainScheduler.QueueOnce(UnAFK, null, TimeSpan.FromSeconds(0));
     Server.MainScheduler.QueueRepeat(UnAFK, null, TimeSpan.FromSeconds(660));
		}
    static string CmdName = "sendcmd";
    static string PlayerName = "AurumStellae";
    static string SentCmd = "afk";
  void UnAFK(SchedulerTask task) {
    Logger.Log(LogType.SystemActivity, "(Schedule) used /SendCmd AurumStellae AFK");
    Command.Find(CmdName).Use(Player.Flame, PlayerName + " " + SentCmd);
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